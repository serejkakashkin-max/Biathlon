"""Owner-directed presentation pruning; route geometry and tree identities stay intact."""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
RECIPE = Path(__file__).with_name('recipes') / 'woodland_playable_forest_v1.json'

def run():
    recipe = json.loads(RECIPE.read_text(encoding='utf-8'))
    assert recipe['schema_version'] == 1
    resources = ROOT / 'Assets/Resources'
    venue = resources / recipe['venue_resource']
    environment_path = venue / 'environment.json'
    environment = json.loads(environment_path.read_text(encoding='utf-8'))
    visual = json.loads((venue / 'visual.json').read_text(encoding='utf-8'))
    segments = [(a, b) for loop in visual['loops'] for a, b in zip(loop['knots'], loop['knots'][1:])]

    def distance(point):
        best = math.inf
        for a, b in segments:
            dx, dz = b['x']-a['x'], b['z']-a['z']
            length = dx*dx+dz*dz
            t = max(0, min(1, ((point['x']-a['x'])*dx+(point['z']-a['z'])*dz)/length)) if length else 0
            best = min(best, math.hypot(point['x']-a['x']-t*dx, point['z']-a['z']-t*dz))
        return best

    before = sum(g['count'] for g in environment['forest']['groups'])
    groups = []
    for group in environment['forest']['groups']:
        path = resources / (group['instances_resource']+'.json')
        forest = json.loads(path.read_text(encoding='utf-8'))
        retained = []
        for tree in forest['instances']:
            d = distance(tree['position'])
            if d <= recipe['dense_distance_from_route_m'] or (d <= recipe['silhouette_distance_from_route_m'] and tree['density_rank'] < recipe['silhouette_density_rank_maximum']):
                retained.append(tree)
        if retained:
            if len(retained) != len(forest['instances']):
                forest['instances'] = retained
                path.write_text(json.dumps(forest, ensure_ascii=False, separators=(',', ':'))+'\n', encoding='utf-8')
            group['count'] = len(retained)
            groups.append(group)
        # Empty source files stay archived on disk; they are no longer loaded by this venue.
    environment['forest']['groups'] = groups
    removed = [a['id'] for a in environment['mesh_assets'] if any(a['id'].startswith(p) for p in recipe['remove_mesh_prefixes'])]
    environment['mesh_assets'] = [a for a in environment['mesh_assets'] if a['id'] not in removed]
    for placement in recipe.get('grounded_mesh_placements', []):
        for asset in environment['mesh_assets']:
            if asset['id'].startswith(placement['prefix']):
                asset['position'] = placement['position'].copy()
    environment_path.write_text(json.dumps(environment, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(dict(schema_version=1, id=recipe['id'], trees_before=before, trees_after=sum(g['count'] for g in groups), loaded_groups=len(groups), removed_meshes=removed, authoritative_geometry_changed=False)))

if __name__ == '__main__':
    run()
