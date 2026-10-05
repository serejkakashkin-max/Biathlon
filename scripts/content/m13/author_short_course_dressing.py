"""Resolve a roadside schedule against real candidate terrain and all paid loops.

Writes an explicit versioned recipe outside Assets. The dressing generator and
native fixture independently check the resulting placements before delivery.
"""
import argparse
import json
import math
from pathlib import Path
import re

import dress_woodland_course as d


def prefab_bounds(project, resource, standing=False):
    text = d.resource_path(project, resource, '.prefab').read_text(encoding='utf-8-sig')
    guid_paths = {}
    for meta in (project / 'Assets/Resources/Biathlon').rglob('*.asset.meta'):
        match = re.search(r'^guid:\s*(\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if match: guid_paths[match.group(1)] = meta.with_suffix('')
    bounds = [d.mesh_bounds(guid_paths[g]) for g in re.findall(r'm_Mesh:.*?guid:\s*(\w+)', text)]
    if standing:
        body = d.mesh_bounds(d.resource_path(project, 'Biathlon/Crowd/StandingSpectatorLOD0', '.asset'))
        block = re.search(r'anchors:\s*\n((?:\s*-\s*{[^}]+}\s*\n)+)', text).group(1)
        for a in (d.numbers(p) for p in re.findall(r'-\s*({[^}]+})', block)):
            bounds.append((tuple(body[0][i] + a[i] for i in range(3)), tuple(body[1][i] + a[i] for i in range(3))))
    return d.merged_bounds(bounds)


def run(project, source_resources, output_recipe):
    project = project.resolve(); source_resources = source_resources.resolve(); output_recipe = output_recipe.resolve()
    if not source_resources.is_relative_to(project / 'artifacts') or not output_recipe.is_relative_to(project / 'artifacts'):
        raise ValueError('Detached authoring paths inside project artifacts required')
    prefix = 'Biathlon/Venues/woodland_basin_v1/'
    environment = d.read_json(source_resources / (prefix + 'environment.json'))
    visual = d.read_json(source_resources / (prefix + 'visual.json'))
    grid, spacing = d.terrain_grid(project, environment, source_resources)
    contacts = d.standing_contact_geometry(project)
    recipe = d.read_json(Path(__file__).with_name('recipes') / 'woodland_basin_course_dressing_v1.json')
    recipe['id'] = 'woodland_basin_course_dressing_v2'
    recipe['landmarks'] = []; recipe['viewing_strips'] = []
    recipe['templates'].pop('short_fence'); recipe['templates'].pop('spectator_pocket')
    for name, resource, variation, mode in (
            ('tape', 'RibbonTape2m', .4, 'embed_footing_into_snow'),
            ('flag', 'FlagStela', .25, 'rigid_no_burial'),
            ('billboard', 'Billboard', .25, 'rigid_no_burial'),
            ('standing', 'StandingCrowd16', 2., 'standing_ground_anchors')):
        value = {'prefab_resource': 'Biathlon/Roadside/' + resource,
                 'maximum_foundation_variation_m': variation, 'foundation_mode': mode}
        if mode == 'embed_footing_into_snow': value['maximum_snow_embedding_m'] = variation
        if mode == 'standing_ground_anchors': value['maximum_foot_contact_gap_m'] = .03
        recipe['templates'][name] = value
    bounds = {}
    for name, template in recipe['templates'].items():
        if 'prefab_resource' in template:
            bounds[name] = prefab_bounds(project, template['prefab_resource'], name == 'standing')
        else:
            bounds[name] = d.merged_bounds([d.mesh_bounds(d.resource_path(project, p['mesh_resource'], '.asset')) for p in template['parts']])
    rejected = []; counts = {}; solid_footprints = []

    def admit(identity, name, loop, station, lateral, yaw_offset):
        point, normal, yaw = d.anchor(visual, loop, station)
        position = (point[0] + normal[0] * lateral, 0., point[2] + normal[1] * lateral)
        polygon = d.footprint(bounds[name], position, yaw + yaw_offset)
        if name != 'tape' and any(any(d.segment_polygon(a, polygon[(i + 1) % 4], prior) < .15
                                             for i, a in enumerate(polygon)) for prior in solid_footprints):
            rejected.append({'id': identity, 'station_m': station, 'reason': 'other_roadside_footprint'})
            return False
        minimum_gap = min(d.segment_polygon((a['x'], a['z']), (b['x'], b['z']), polygon)
                          - max(c['widths_m'][i:i + 2]) / 2
                          for c in environment['route_corridors'] for i, (a, b) in enumerate(zip(c['knots'], c['knots'][1:])))
        if minimum_gap < recipe['minimum_course_edge_clearance_m']:
            rejected.append({'id': identity, 'station_m': station, 'reason': 'paid_corridor', 'minimum_gap_m': minimum_gap})
            return False
        heights = [y for _, y in d.foundation_samples(grid, spacing, polygon)]
        if name == 'standing':
            gap = d.standing_contact_gap(grid, spacing, position, yaw + yaw_offset, contacts)
            if gap > recipe['templates'][name]['maximum_foot_contact_gap_m'] - .0002:
                rejected.append({'id': identity, 'station_m': station, 'reason': 'native_boot_contact', 'maximum_gap_m': gap})
                return False
        if max(heights) - min(heights) > recipe['templates'][name]['maximum_foundation_variation_m']:
            rejected.append({'id': identity, 'station_m': station, 'reason': 'foundation_variation', 'variation_m': max(heights) - min(heights)})
            return False
        recipe['landmarks'].append({'id': identity, 'template': name, 'loop_id': loop,
                                    'distance_m': round(station, 3), 'lateral_offset_m': lateral, 'yaw_offset_deg': yaw_offset})
        if name != 'tape': solid_footprints.append(polygon)
        counts[name] = counts.get(name, 0) + 1
        return True

    def nearest_fit(identity, name, station, laterals, yaw=90):
        for delta in [0] + [v for n in range(1, 21) for v in (n, -n)]:
            for lateral in laterals:
                if admit(identity, name, 'main', station + delta, lateral, yaw if lateral > 0 else -yaw): return
        raise ValueError('No bounded native-ground placement for required landmark: ' + identity)

    schedule = {'schema_version': 1, 'id': 'woodland_short_roadside_schedule_v1',
                'main_tape_start_m': 12., 'main_tape_end_m': 970., 'tape_step_m': 2.2,
                'tape_laterals_m': [-6.8, 6.8], 'standing_step_m': 65.,
                'standing_laterals_m': [-9., 9.], 'optional_branch_tape_margin_m': 16.}
    recipe['resolved_schedule'] = schedule
    for identity, station in (
            ('course_climb_entry_banner', 120.), ('course_summit_banner', 310.),
            ('course_plateau_hut_banner', 470.), ('course_descent_entry_banner', 665.),
            ('course_descent_exit_banner', 835.), ('course_return_landmark_banner', 915.)):
        nearest_fit(identity, 'banner', station, [9., -9., 11., -11.])
    # A real level service building near the stadium exit, not a floating hut
    # balanced on the steep ridge. All paid routes still protect its full bounds.
    nearest_fit('course_plateau_support_hut', 'support_hut', 20., [-28., 28., -36., 36.], -90)
    nearest_fit('course_plateau_spectators', 'standing', 440., [9., -9., 12., -12.])
    nearest_fit('course_return_spectators', 'standing', 895., [9., -9., 12., -12.])
    for index, station in enumerate(range(45, 961, 65)):
        for side, lateral in enumerate(schedule['standing_laterals_m']):
            admit(f'roadside_crowd_{index:03d}_{side}', 'standing', 'main', float(station), lateral, -90 if lateral < 0 else 90)
    for index, station in enumerate((35., 135., 320., 390., 520., 635., 725., 875., 940.)):
        for side, lateral in enumerate((-8., 8.)):
            for name in ('flag', 'billboard'):
                nearest_fit(f'{name}_stela_{index:02d}_{side}', name, station + (8. if name == 'billboard' else 0), [lateral, lateral * 1.5])
    def tapes(loop, low, high):
        count = math.floor((high - low) / schedule['tape_step_m'])
        for index in range(count + 1):
            station = round(low + index * schedule['tape_step_m'], 3)
            for side, lateral in enumerate(schedule['tape_laterals_m']):
                admit(f'tape_{loop}_{index:04d}_{side}', 'tape', loop, station, lateral, 90)
    tapes('main', schedule['main_tape_start_m'], schedule['main_tape_end_m'])
    for loop in ('sprint_extension', 'pursuit_shortcut'):
        length = next(l['knots'][-1]['distance_m'] for l in visual['loops'] if l['loop_id'] == loop)
        margin = schedule['optional_branch_tape_margin_m']
        tapes(loop, margin, length - margin)
    recipe['status'] = 'RESOLVED_CANDIDATE_NATIVE_REVIEW_REQUIRED'
    recipe['provenance'].append('docs/licensing/g2_visual/roadside-original-derivation-20261005.json')
    output_recipe.parent.mkdir(parents=True, exist_ok=True)
    output_recipe.write_text(json.dumps(recipe, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    receipt = {'schema_version': 1, 'id': 'woodland_short_roadside_authoring_receipt_v1',
               'status': 'EXPLICIT_RECIPE_RESOLVED_NATIVE_REVIEW_PENDING', 'counts': counts,
               'recipe_sha256': d.sha(output_recipe), 'rejected_unsafe_candidates': rejected,
               'source_environment_sha256': d.sha(source_resources / (prefix + 'environment.json')),
               'source_visual_sha256': d.sha(source_resources / (prefix + 'visual.json'))}
    output_recipe.with_suffix('.receipt.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    return counts


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-root', type=Path, default=Path(__file__).resolve().parents[3])
    parser.add_argument('--source-resources', type=Path, required=True)
    parser.add_argument('--output-recipe', type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(run(args.project_root, args.source_resources, args.output_recipe)))
