"""Place existing Woodland art beside paid routes; write a candidate outside Assets."""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path
import re


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def resource_path(project, resource, suffix):
    if not isinstance(resource, str) or resource.startswith('/') or any(p in ('', '.', '..') for p in resource.split('/')) or '\\' in resource:
        raise ValueError('Bounded extensionless resource required')
    path = (project / 'Assets/Resources' / (resource + suffix)).resolve()
    if not path.is_relative_to((project / 'Assets/Resources').resolve()) or not path.is_file():
        raise ValueError('Missing native resource: ' + resource)
    return path


def numbers(value, axes=('x', 'y', 'z')):
    result = tuple(float(re.search(r'\b' + axis + r':\s*([^,}]+)', value).group(1)) for axis in axes)
    if not all(map(math.isfinite, result)):
        raise ValueError('Finite native bounds required')
    return result


def mesh_bounds(path):
    text = path.read_text(encoding='utf-8-sig')
    match = re.search(r'm_LocalAABB:\s*\n\s*m_Center:\s*({[^}]+})\s*\n\s*m_Extent:\s*({[^}]+})', text)
    if match is None:
        raise ValueError('Explicit serialized native Mesh AABB required: ' + str(path))
    center, extent = (numbers(match.group(i)) for i in (1, 2))
    if any(v < 0 for v in extent):
        raise ValueError('Nonnegative native extents required')
    return tuple(center[i] - extent[i] for i in range(3)), tuple(center[i] + extent[i] for i in range(3))


def merged_bounds(bounds):
    return tuple(min(b[0][i] for b in bounds) for i in range(3)), tuple(max(b[1][i] for b in bounds) for i in range(3))


def rotate(x, z, yaw):
    angle = math.radians(yaw)
    return x * math.cos(angle) + z * math.sin(angle), -x * math.sin(angle) + z * math.cos(angle)


def footprint(bounds, position, yaw):
    lo, hi = bounds
    return [tuple(a + b for a, b in zip(rotate(x, z, yaw), (position[0], position[2])))
            for x, z in ((lo[0], lo[2]), (hi[0], lo[2]), (hi[0], hi[2]), (lo[0], hi[2]))]


def point_segment(point, a, b):
    dx, dz = b[0] - a[0], b[1] - a[1]
    denominator = dx * dx + dz * dz
    t = max(0., min(1., ((point[0] - a[0]) * dx + (point[1] - a[1]) * dz) / denominator)) if denominator else 0.
    return math.hypot(point[0] - a[0] - t * dx, point[1] - a[1] - t * dz)


def cross(a, b, p):
    return (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])


def inside(point, polygon):
    signs = [cross(polygon[i], polygon[(i + 1) % len(polygon)], point) for i in range(len(polygon))]
    return all(v >= -1e-9 for v in signs) or all(v <= 1e-9 for v in signs)


def intersects(a, b, c, d):
    if max(a[0], b[0]) < min(c[0], d[0]) or max(c[0], d[0]) < min(a[0], b[0]) or max(a[1], b[1]) < min(c[1], d[1]) or max(c[1], d[1]) < min(a[1], b[1]):
        return False
    return cross(a, b, c) * cross(a, b, d) <= 0 and cross(c, d, a) * cross(c, d, b) <= 0


def segment_polygon(a, b, polygon):
    if inside(a, polygon) or inside(b, polygon):
        return 0.
    distance = float('inf')
    for i, c in enumerate(polygon):
        d = polygon[(i + 1) % len(polygon)]
        if intersects(a, b, c, d):
            return 0.
        distance = min(distance, point_segment(a, c, d), point_segment(b, c, d), point_segment(c, a, b), point_segment(d, a, b))
    return distance


def point_polygon(point, polygon):
    return 0. if inside(point, polygon) else min(point_segment(point, p, polygon[(i + 1) % len(polygon)]) for i, p in enumerate(polygon))


def terrain_grid(project, environment):
    grid = {}; spacing = environment['terrain']['grid_spacing_m']
    for tile in environment['terrain']['tiles']:
        heights = read_json(resource_path(project, tile['height_resource'], '.json'))
        if heights['tile_id'] != tile['id'] or heights['venue_id'] != environment['venue_id'] or len(heights['heights_m']) != tile['columns'] * tile['rows']:
            raise ValueError('Exact matching terrain tile required')
        for iz in range(tile['rows']):
            for ix in range(tile['columns']):
                p = (tile['center']['x'] - tile['size']['x'] / 2 + ix * spacing, tile['center']['z'] - tile['size']['z'] / 2 + iz * spacing)
                value = heights['heights_m'][iz * tile['columns'] + ix]
                if p in grid and grid[p] != value:
                    raise ValueError('Terrain borders differ')
                grid[p] = value
    return grid, spacing


def ground(grid, spacing, x, z):
    x0 = math.floor(x / spacing) * spacing; z0 = math.floor(z / spacing) * spacing
    fx = (x - x0) / spacing; fz = (z - z0) / spacing
    a, b, c, d = (grid[p] for p in ((x0, z0), (x0, z0 + spacing), (x0 + spacing, z0), (x0 + spacing, z0 + spacing)))
    return a + (c - a) * fx + (b - a) * fz if fx + fz <= 1 else d + (b - d) * (1 - fx) + (c - d) * (1 - fz)


def clip_polygon(subject, clip):
    """Keep exact boundary intersections of two convex terrain footprints."""
    sign = 1 if cross(clip[0], clip[1], clip[2]) > 0 else -1
    polygon = list(subject)
    for i, a in enumerate(clip):
        if not polygon:
            break
        b = clip[(i + 1) % len(clip)]; result = []
        for j, point in enumerate(polygon):
            prior = polygon[j - 1]; p = sign * cross(a, b, prior); q = sign * cross(a, b, point)
            if (p >= -1e-9) != (q >= -1e-9):
                t = p / (p - q); result.append(tuple(prior[k] + t * (point[k] - prior[k]) for k in range(2)))
            if q >= -1e-9:
                result.append(point)
        polygon = result
    return polygon


def foundation_samples(grid, spacing, polygon):
    """Affine extrema over every native terrain triangle/footprint overlay."""
    samples = {}
    x0 = math.floor(min(p[0] for p in polygon) / spacing)
    x1 = math.floor(max(p[0] for p in polygon) / spacing)
    z0 = math.floor(min(p[1] for p in polygon) / spacing)
    z1 = math.floor(max(p[1] for p in polygon) / spacing)
    for iz in range(z0, z1 + 1):
        for ix in range(x0, x1 + 1):
            x, z = ix * spacing, iz * spacing
            triangles = [[(x, z), (x, z + spacing), (x + spacing, z)], [(x + spacing, z), (x, z + spacing), (x + spacing, z + spacing)]]
            for triangle in triangles:
                for point in clip_polygon(polygon, triangle):
                    samples[point] = ground(grid, spacing, *point)
    if not samples:
        raise ValueError('Native foundation overlay is empty')
    return sorted(samples.items())


def foundation_y(heights, minimum_native_y, template):
    """Rigid flat footings sit above snow; narrow vertical posts may embed."""
    mode = template.get('foundation_mode', 'rigid_no_burial')
    if mode == 'rigid_no_burial':
        return max(heights) - minimum_native_y
    if mode != 'embed_footing_into_snow':
        raise ValueError('Unknown authored foundation mode')
    cap = template['maximum_snow_embedding_m']
    if isinstance(cap, bool) or not isinstance(cap, (int, float)) or not math.isfinite(cap) or not 0 <= cap <= .5 or max(heights) - min(heights) > cap:
        raise ValueError('Banner snow embedding exceeds authored cap')
    return min(heights) - minimum_native_y


def anchor(visual, loop_id, distance):
    knots = next(l['knots'] for l in visual['loops'] if l['loop_id'] == loop_id)
    for a, b in zip(knots, knots[1:]):
        if a['distance_m'] <= distance <= b['distance_m']:
            t = (distance - a['distance_m']) / (b['distance_m'] - a['distance_m'])
            point = tuple(a[v] + t * (b[v] - a[v]) for v in ('x', 'y', 'z'))
            dx, dz = b['x'] - a['x'], b['z'] - a['z']; length = math.hypot(dx, dz)
            if length == 0:
                raise ValueError('Nonvertical paid source edge required')
            return point, (dz / length, -dx / length), math.degrees(math.atan2(dx, dz))
    raise ValueError('Anchor outside actual paid loop')


def viewing_strip(visual, item):
    """An explicit bounded audience sightline, anchored in the paid course frame."""
    point, normal, yaw = anchor(visual, item['loop_id'], item['distance_m'])
    forward = (-normal[1], normal[0])
    authored = item['polygon_forward_lateral_m']
    if len(authored) != 4 or any(len(p) != 2 or any(isinstance(v, bool) or not isinstance(v, (int, float)) or not math.isfinite(v) or abs(v) > 60 for v in p) for p in authored):
        raise ValueError('Four finite bounded viewing-strip corners required')
    turns = [cross(authored[i], authored[(i + 1) % 4], authored[(i + 2) % 4]) for i in range(4)]
    if not (all(t > 1e-6 for t in turns) or all(t < -1e-6 for t in turns)):
        raise ValueError('A strictly convex viewing strip required')
    if max(p[0] for p in authored) - min(p[0] for p in authored) > 50 or max(p[1] for p in authored) - min(p[1] for p in authored) > 12:
        raise ValueError('Audience strip cannot become a broad forest clearing')
    polygon = [(point[0] + forward[0] * p[0] + normal[0] * p[1], point[2] + forward[1] * p[0] + normal[1] * p[1]) for p in authored]
    return polygon


def run(project, recipe_path, output):
    project = project.resolve(); output = output.resolve()
    if not output.is_relative_to(project / 'artifacts') or output.exists():
        raise ValueError('Fresh candidate directory inside ignored artifacts required')
    recipe = read_json(recipe_path)
    if recipe.get('schema_version') != 1 or recipe.get('id') != 'woodland_basin_course_dressing_v1' or recipe.get('venue_id') != 'woodland_basin_v1':
        raise ValueError('Exact versioned Woodland recipe required')
    for field, low, high in (('minimum_course_edge_clearance_m', 2., 20.), ('canopy_landmark_clearance_m', 0., 10.)):
        value = recipe[field]
        if isinstance(value, bool) or not isinstance(value, (float, int)) or not math.isfinite(value) or not low <= value <= high:
            raise ValueError('Finite bounded presentation clearance required')
    prefix = 'Biathlon/Venues/woodland_basin_v1'; source = project / 'Assets/Resources' / prefix
    original = {str(p.relative_to(project / 'Assets/Resources')).replace('\\', '/'): p.read_bytes() for p in source.rglob('*.json')}
    documents = {p: json.loads(v.decode('utf-8-sig')) for p, v in original.items()}
    environment = documents[prefix + '/environment.json']; visual = documents[prefix + '/visual.json']
    grid, spacing = terrain_grid(project, environment)
    sources = {}; templates = {}
    guid_paths = {}
    for meta in (project / 'Assets/Resources/Biathlon/Stadium').rglob('*.asset.meta'):
        match = re.search(r'^guid:\s*(\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if match:
            guid_paths[match.group(1)] = meta.with_suffix('')
    def native_mesh(resource):
        p = resource_path(project, resource, '.asset'); sources[str(p.relative_to(project))] = sha(p)
        return mesh_bounds(p)
    for name, template in recipe['templates'].items():
        variation = template['maximum_foundation_variation_m']
        if isinstance(variation, bool) or not math.isfinite(variation) or not 0 <= variation <= .5:
            raise ValueError('Bounded rigid foundation variation required')
        if 'prefab_resource' in template:
            p = resource_path(project, template['prefab_resource'], '.prefab'); text = p.read_text(encoding='utf-8-sig'); sources[str(p.relative_to(project))] = sha(p)
            # This existing baked prefab has identity local transforms throughout.
            # Reject richer hierarchies instead of pretending raw mesh bounds compose.
            if any(numbers(s) != (0., 0., 0.) for s in re.findall(r'm_LocalPosition:\s*({[^}]+})', text)) or any(numbers(s) != (1., 1., 1.) for s in re.findall(r'm_LocalScale:\s*({[^}]+})', text)) or any(numbers(s, ('x', 'y', 'z', 'w')) != (0., 0., 0., 1.) for s in re.findall(r'm_LocalRotation:\s*({[^}]+})', text)):
                raise ValueError('Actual transformed prefab bounds require a native witness')
            guids = re.findall(r'm_Mesh:.*?guid:\s*(\w+)', text)
            bounds = []
            for guid in guids:
                mesh = guid_paths[guid]; sources[str(mesh.relative_to(project))] = sha(mesh); bounds.append(mesh_bounds(mesh))
            seating = re.search(r'anchors:\s*\n((?:\s*-\s*{[^}]+}\s*\n)+)', text)
            seats = len(re.findall(r'-\s*{', seating.group(1))) if seating else 0
            if seats < 1 or seats > 512 or 'Biathlon.Unity.Presentation.SpectatorSeating' not in text:
                raise ValueError('Existing real spectator anchors required')
            templates[name] = (merged_bounds(bounds), seats)
        else:
            bounds = []
            for part in template['parts']:
                bounds.append(native_mesh(part['mesh_resource']))
                material = resource_path(project, part['material_resource'], '.mat'); sources[str(material.relative_to(project))] = sha(material)
            templates[name] = (merged_bounds(bounds), 0)
    ids = {x['id'] for x in environment['assets'] + environment['mesh_assets']}; witnesses = []; footprints = []
    for item in recipe['landmarks']:
        identity = item['id']
        if not re.fullmatch(r'[a-z][a-z0-9_]{1,100}', identity) or identity in ids:
            raise ValueError('Unique stable landmark required')
        ids.add(identity); template = recipe['templates'][item['template']]; bounds, seats = templates[item['template']]
        for field, low, high in (('distance_m', 0., 100000.), ('lateral_offset_m', -200., 200.), ('yaw_offset_deg', -360., 360.)):
            value = item[field]
            if isinstance(value, bool) or not isinstance(value, (float, int)) or not math.isfinite(value) or not low <= value <= high:
                raise ValueError('Finite bounded landmark anchor required')
        point, normal, yaw = anchor(visual, item['loop_id'], item['distance_m']); yaw += item['yaw_offset_deg']
        position = (point[0] + normal[0] * item['lateral_offset_m'], 0., point[2] + normal[1] * item['lateral_offset_m'])
        polygon = footprint(bounds, position, yaw)
        # Corner/centre sampling could hide an interior terrain-grid peak. Check
        # exact affine extrema on every actual native triangle under the footprint.
        footing = foundation_samples(grid, spacing, polygon)
        samples = [p for p, _ in footing]; heights = [y for _, y in footing]
        if max(heights) - min(heights) > template['maximum_foundation_variation_m']:
            raise ValueError('Uneven native terrain foundation: ' + identity)
        position = (position[0], foundation_y(heights, bounds[0][1], template), position[2]); minimum_gap = float('inf'); nearest = None
        for corridor in environment['route_corridors']:
            for i, (a, b) in enumerate(zip(corridor['knots'], corridor['knots'][1:])):
                gap = segment_polygon((a['x'], a['z']), (b['x'], b['z']), polygon) - max(corridor['widths_m'][i:i + 2]) / 2
                if gap < minimum_gap:
                    minimum_gap = gap; nearest = {'loop_id': corridor['loop_id'], 'row_start_m': a['distance_m'], 'row_end_m': b['distance_m']}
        if minimum_gap < recipe['minimum_course_edge_clearance_m']:
            raise ValueError('Native footprint intrudes on paid corridor: ' + identity)
        placement = {'id': identity, 'position': dict(zip(('x', 'y', 'z'), position)), 'yaw_deg': yaw, 'scale': 1}
        if 'prefab_resource' in template:
            environment['assets'].append(dict(placement, resource=template['prefab_resource']))
        else:
            for i, part in enumerate(template['parts']):
                environment['mesh_assets'].append(dict(placement, id=identity + '_' + str(i + 1), **part))
        footprints.append(polygon)
        footing_bottom = position[1] + bounds[0][1]
        witnesses.append({'id': identity, 'anchor': item, 'native_local_bounds': bounds, 'world_footprint': polygon, 'world_position': placement['position'], 'yaw_deg': yaw, 'minimum_course_edge_clearance_m': minimum_gap, 'nearest_corridor': nearest, 'foundation_samples': [{'x': p[0], 'z': p[1], 'terrain_y': y} for p, y in zip(samples, heights)], 'foundation_variation_m': max(heights) - min(heights), 'foundation_mode': template.get('foundation_mode', 'rigid_no_burial'), 'maximum_bottom_above_snow_m': max(0., footing_bottom - min(heights)), 'maximum_snow_embedding_m': max(0., max(heights) - footing_bottom), 'authored_maximum_snow_embedding_m': template.get('maximum_snow_embedding_m', 0.), 'spectator_anchors': seats})
    strip_witnesses = []
    for strip in recipe.get('viewing_strips', []):
        if strip['id'] in ids or not re.fullmatch(r'[a-z][a-z0-9_]{1,100}', strip['id']):
            raise ValueError('Unique stable audience strip required')
        ids.add(strip['id'])
        spectator = next(w for w in witnesses if w['id'] == strip['spectator_landmark_id'])
        fence = next(w for w in witnesses if w['id'] == strip['fence_landmark_id'])
        if spectator['spectator_anchors'] == 0 or any(w['anchor']['loop_id'] != strip['loop_id'] or w['anchor']['distance_m'] != strip['distance_m'] for w in (spectator, fence)):
            raise ValueError('Viewing strip must bind its exact actual spectator/fence anchor')
        polygon = viewing_strip(visual, strip)
        if segment_polygon(polygon[0], polygon[1], fence['world_footprint']) > 1e-5 or segment_polygon(polygon[2], polygon[3], spectator['world_footprint']) > 1e-5:
            raise ValueError('Viewing strip must join the actual fence and spectator edges')
        gap = float('inf')
        for corridor in environment['route_corridors']:
            for i, (a, b) in enumerate(zip(corridor['knots'], corridor['knots'][1:])):
                gap = min(gap, segment_polygon((a['x'], a['z']), (b['x'], b['z']), polygon) - max(corridor['widths_m'][i:i + 2]) / 2)
        if gap < recipe['minimum_course_edge_clearance_m']:
            raise ValueError('Audience strip overlaps a paid route')
        strip_witnesses.append({'id': strip['id'], 'recipe': strip, 'world_polygon': polygon, 'minimum_course_edge_clearance_m': gap})
    pruning_zones = [{'id': w['id'], 'kind': 'landmark_footprint', 'polygon': w['world_footprint']} for w in witnesses]
    pruning_zones += [{'id': w['id'], 'kind': 'spectator_viewing_strip', 'polygon': w['world_polygon']} for w in strip_witnesses]
    pruned = []; total_before = sum(g['count'] for g in environment['forest']['groups']); radius = environment['forest']['conservative_all_species_canopy_radius_m']
    for group in environment['forest']['groups']:
        path = group['instances_resource'] + '.json'; forest = documents[path]; retained = []
        for tree in forest['instances']:
            gaps = [(point_polygon((tree['position']['x'], tree['position']['z']), zone['polygon']) - radius * tree['scale'], zone) for zone in pruning_zones]
            gap, zone = min(gaps, key=lambda pair: pair[0])
            if gap < recipe['canopy_landmark_clearance_m']:
                pruned.append({'id': tree['id'], 'group_id': group['id'], 'instance': copy.deepcopy(tree), 'canopy_gap_m': gap, 'nearest_pruning_zone_id': zone['id'], 'pruning_zone_kind': zone['kind']})
            else:
                retained.append(tree)
        if len(retained) != len(forest['instances']):
            forest['instances'] = retained; group['count'] = len(retained)
    if any(g['count'] == 0 for g in environment['forest']['groups']):
        raise ValueError('Empty forest group requires explicit authoring review')
    changed = {}; output_bytes = {}
    for path, prior in original.items():
        decoded = json.loads(prior.decode('utf-8-sig'))
        content = (json.dumps(documents[path], ensure_ascii=False, indent=2) + '\n').encode() if decoded != documents[path] else prior
        output_bytes[path] = content
        if content != prior:
            changed[path] = {'before_sha256': hashlib.sha256(prior).hexdigest(), 'after_sha256': hashlib.sha256(content).hexdigest()}
    allowed = {prefix + '/environment.json'} | {g['instances_resource'] + '.json' for g in environment['forest']['groups']}
    if not set(changed).issubset(allowed):
        raise ValueError('Presentation-only resource boundary violated')
    provenance = []
    for relative in recipe['provenance']:
        path = (project / relative).resolve()
        if not path.is_relative_to(project) or not path.is_file():
            raise ValueError('Existing local source attribution required')
        provenance.append({'path': relative, 'sha256': sha(path)})
    proof = {'schema_version': 1, 'id': 'woodland_course_dressing_candidate_proof_20261005_v1', 'status': 'CANDIDATE_STATIC_CHECKS_ONLY_NOT_IMPORTED_NATIVE_REVIEW_REQUIRED', 'recipe_sha256': sha(recipe_path), 'native_source_sha256': sources, 'license_provenance': provenance, 'source_venue_json_count': len(original), 'source_venue_sha256': {p: hashlib.sha256(b).hexdigest() for p, b in original.items()}, 'changed_resources': changed, 'landmark_witnesses': witnesses, 'viewing_strip_witnesses': strip_witnesses, 'forest_before': total_before, 'forest_after': total_before - len(pruned), 'pruned_tree_count': len(pruned), 'pruned_original_instances': pruned, 'actual_added_spectator_anchors': sum(w['spectator_anchors'] for w in witnesses), 'authoritative_geometry_changed': False, 'native_review_required': ['Camera-visible landmarks and facing', 'Actual prefab crowd and foliage after pruning', 'Actual rendered foundation contact', 'All weather readability']}
    output.mkdir(parents=True)
    for path, content in output_bytes.items():
        target = output / 'resources' / path; target.parent.mkdir(parents=True, exist_ok=True); target.write_bytes(content)
    (output / 'proof.json').write_text(json.dumps(proof, indent=2) + '\n', encoding='utf-8')
    return proof


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-root', type=Path, default=Path(__file__).resolve().parents[3])
    parser.add_argument('--recipe', type=Path, default=Path(__file__).with_name('recipes') / 'woodland_basin_course_dressing_v1.json')
    parser.add_argument('--output-root', type=Path, required=True)
    args = parser.parse_args()
    proof = run(args.project_root, args.recipe, args.output_root)
    print(json.dumps({k: proof[k] for k in ('status', 'pruned_tree_count', 'forest_after', 'actual_added_spectator_anchors', 'changed_resources')}))
