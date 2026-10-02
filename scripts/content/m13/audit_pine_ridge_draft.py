"""Audit the isolated U34/compound Pine replacement; preserve historical proofs."""
from pathlib import Path
import argparse
import hashlib
import json

SCRIPT = Path(__file__).resolve().parent
VENUE = 'pine_ridge_v1'
GENERATOR_SOURCES = ('generate_venue01.py', 'generate_format_branches.py',
                     'generate_environment.py', 'verify_environment.py',
                     'verify_format_branches.py', 'exact_polyline.py',
                     'corridor_sampling.py', 'environment_serialization.py',
                     'regress_venue01_defaults.py')


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def inventory(root):
    return {path.relative_to(root).as_posix(): sha(path) for path in sorted(root.rglob('*.json'))}


def relative(project, path):
    return path.resolve().relative_to(project).as_posix()


def shared_range_contract(reference, pine):
    """Check the actual shared stadium, independently of recipe declarations."""
    tracks = [{loop['id']: loop for loop in read(root / 'track.json')['loops']}
              for root in (reference, pine)]
    visuals = [{loop['loop_id']: loop for loop in read(root / 'visual.json')['loops']}
               for root in (reference, pine)]
    shared = set(visuals[0]) - {'main', 'pursuit_shortcut', 'sprint_extension'}
    assert len(shared) == 33
    for loop_id in shared:
        assert tracks[0][loop_id] == tracks[1][loop_id], loop_id
        assert visuals[0][loop_id] == visuals[1][loop_id], loop_id
    paths = [read(root / 'paths.json') for root in (reference, pine)]
    assert len(paths[1]['lanes']) == 30 and paths[0]['lanes'] == paths[1]['lanes']
    path_lengths = [sum(round((part['end_m'] - part['start_m']) * 1000)
                        for part in lane['slices']) for lane in paths[1]['lanes']]
    assert set(path_lengths) == {269408}
    layouts = [read(root / 'manifest.json')['range_layout'] for root in (reference, pine)]
    assert layouts[0] == layouts[1] and layouts[1]['lane_pitch_m'] == 6
    assert sum(round(segment['length_m'] * 1000)
               for segment in tracks[1]['penalty_150']['segments']) == 150000
    return {'lane_count': 30, 'pitch_m': 6, 'identical_current_woodland_lane_paths': 30,
            'identical_current_woodland_physical_loops': 33, 'range_layout_equal': True,
            'private_path_length_m': 39.408, 'equal_full_personal_path_length_m': 269.408,
            'exit_aisle_length_m': 230, 'firing_distance_m': 50, 'penalty_length_m': 150}


def previous_draft_contract(previous, pine):
    """The tail changes counted aisle slices; private geometry/guards stay exact."""
    old_visual, new_visual = [{loop['loop_id']: loop for loop in read(root / 'visual.json')['loops']}
                              for root in (previous, pine)]
    assert all(old_visual[f'mat_{index:02d}'] == new_visual[f'mat_{index:02d}']
               for index in range(1, 31))
    old_paths, new_paths = [read(root / 'paths.json') for root in (previous, pine)]
    for old, new in zip(old_paths['lanes'], new_paths['lanes']):
        assert old['id'] == new['id'] and old['mat_anchor'] == new['mat_anchor']
        assert old['slices'][1] == new['slices'][1]
        assert round(old['slices'][0]['start_m'] - new['slices'][0]['start_m'], 3) == 34
        assert round(new['slices'][2]['end_m'] - old['slices'][2]['end_m'], 3) == 34
    old_regions, new_regions = [{region['id']: region for region in read(root / 'junctions.json')['junctions']}
                               for root in (previous, pine)]
    old_ports, new_ports = [{port['id']: port for region in regions.values() for port in region['ports']}
                           for regions in (old_regions, new_regions)]
    assert old_ports.keys() == new_ports.keys()
    for port_id, old in old_ports.items():
        new = new_ports[port_id]
        assert all(old[key] == new[key] for key in ('guard_before_m', 'guard_after_m'))
        assert old['anchor']['loop_id'] == new['anchor']['loop_id']
    assert set(new_regions) == set(old_regions) - {'bypass_turn'}
    assert {port['id'] for port in new_regions['range_rejoin']['ports']} == {
        port['id'] for region_id in ('range_rejoin', 'bypass_turn')
        for port in old_regions[region_id]['ports']}
    assert read(previous / 'connections.json') == read(pine / 'connections.json')
    old_metrics, new_metrics = course_metrics(previous), course_metrics(pine)
    for key in ('sustained_climb_travel_m', 'sustained_climb_grade', 'sustained_climb_width_m',
                'sustained_climb_draft_allowed', 'main_ascent_m', 'main_descent_m',
                'main_peak_y_m', 'main_widths_m'):
        assert old_metrics[key] == new_metrics[key], key
    return {'private_physical_loops_unchanged': 30, 'mat_anchors_unchanged': 30,
            'private_slices_unchanged': 30, 'full_personal_path_length_change_m': 34,
            'entry_main_anchor_change_m': -34, 'exit_aisle_length_change_m': 34,
            'directed_connections_unchanged': True, 'all_port_ids_and_guard_extents_unchanged': True,
            'guard_port_count': len(new_ports), 'junction_count_before': len(old_regions),
            'junction_count_after': len(new_regions),
            'compound_range_rejoin_port_ids': [port['id'] for port in new_regions['range_rejoin']['ports']],
            'bypass_turn_region_removed': True,
            'draft_allowed_travel_change_m': round(new_metrics['draft_allowed_travel_m'] -
                                                   old_metrics['draft_allowed_travel_m'], 3)}


def course_metrics(root):
    track, visual = read(root / 'track.json'), read(root / 'visual.json')
    main = next(loop for loop in track['loops'] if loop['id'] == 'main')
    knots = next(loop for loop in visual['loops'] if loop['loop_id'] == 'main')['knots']
    sectors = []
    total = 0.
    for segment in main['segments']:
        key = tuple(segment[field] for field in
                    ('width_m', 'wind_exposure', 'draft_allowed', 'overtake_allowed', 'semantic_zone'))
        if not sectors or sectors[-1]['key'] != key:
            sectors.append({'key': key, 'start_m': total, 'end_m': total,
                            'minimum_grade': segment['start_grade'], 'maximum_grade': segment['start_grade']})
        total += segment['length_m']
        sectors[-1]['end_m'] = total
        sectors[-1]['minimum_grade'] = min(sectors[-1]['minimum_grade'], segment['start_grade'])
        sectors[-1]['maximum_grade'] = max(sectors[-1]['maximum_grade'], segment['start_grade'])
    for sector in sectors:
        sector.update(dict(zip(('width_m', 'wind_exposure', 'draft_allowed', 'overtake_allowed', 'semantic_zone'), sector.pop('key'))))
        sector['start_m'], sector['end_m'] = round(sector['start_m'], 3), round(sector['end_m'], 3)
        sector['length_m'] = round(sector['end_m'] - sector['start_m'], 3)
    climb = next(segment for segment in main['segments'] if segment['semantic_zone'] == 'climb_long')
    return {'main_length_m': round(total, 3), 'sustained_climb_travel_m': climb['length_m'],
            'sustained_climb_grade': climb['start_grade'], 'sustained_climb_width_m': climb['width_m'],
            'sustained_climb_draft_allowed': climb['draft_allowed'],
            'main_ascent_m': sum(max(0, b['y'] - a['y']) for a, b in zip(knots, knots[1:])),
            'main_descent_m': sum(max(0, a['y'] - b['y']) for a, b in zip(knots, knots[1:])),
            'main_peak_y_m': max(knot['y'] for knot in knots),
            'main_widths_m': sorted({segment['width_m'] for segment in main['segments']}),
            'draft_allowed_travel_m': round(sum(segment['length_m'] for segment in main['segments'] if segment['draft_allowed']), 3),
            'distance_weighted_wind_exposure': sum(segment['length_m'] * segment['wind_exposure'] for segment in main['segments']) / total,
            'sectors': sectors}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-root', type=Path, default=SCRIPT.parents[2])
    parser.add_argument('--output-root', type=Path, default=None)
    parser.add_argument('--repeat-root', type=Path, default=None)
    parser.add_argument('--regression-report', type=Path, default=None,
                        help='Existing current-source Venue01 byte proof; no Venue01 generation is performed.')
    parser.add_argument('--capture-inputs', action='store_true',
                        help='Snapshot dependencies and protected historical data before the independent Pine repeat.')
    args = parser.parse_args()
    project = args.project_root.resolve()
    output = (args.output_root or project / 'artifacts/wp170/u34-compound-draft').resolve()
    draft = output / 'formats-draft'
    pine = draft / ('resources/Biathlon/Venues/' + VENUE)
    previous = project / 'artifacts/wp170/formats-draft/resources/Biathlon/Venues/pine_ridge_v1'
    canonical = project / 'artifacts/wp169u/final-format-draft/resources/Biathlon/Venues/woodland_basin_v1'
    historical_proof = project / 'docs/quality/m13/wp170-pine-ridge-draft-verification.json'
    historical_addendum = project / 'docs/quality/m13/venue01-generic-generator-regression-addendum.json'
    regression_path = (args.regression_report or project / 'artifacts/wp169u/default-byte-reproduction/regression.json').resolve()
    for protected in (previous, canonical, SCRIPT):
        if output.is_relative_to(protected) or protected.is_relative_to(output):
            parser.error('Audit output must not overlap source, previous Pine or reference Woodland data.')
    dependencies = [*(SCRIPT / name for name in GENERATOR_SOURCES),
                    SCRIPT / 'audit_pine_ridge_draft.py', SCRIPT / 'test_pine_ridge_geometry.py',
                    *(SCRIPT / 'recipes').glob('pine_ridge_*.json'),
                    *sorted((project / 'Assets/Resources/Biathlon').glob('*.json'))]
    environment_audit = read(draft / 'environment-audit.json')
    for resource in environment_audit['native_resource_sha256']:
        dependencies.append(next(project / 'Assets/Resources' / (resource + extension)
                                 for extension in ('.json', '.prefab', '.mat', '.asset')
                                 if (project / 'Assets/Resources' / (resource + extension)).exists()))
    validator = project / 'artifacts/wp170/dotnet-validator/bin/Biathlon.BalanceLab/debug'
    dependencies.extend(sorted(validator.glob('Biathlon.*.dll')))
    dependencies.extend(project / ('Assets/Scripts/Unity/Presentation/' + name)
                        for name in ('TrackRibbonBuilder.cs', 'TerrainBlendMesh.cs'))
    fingerprints = {
        'dependency_sha256': {relative(project, path): sha(path) for path in sorted(set(dependencies))},
        'previous_pine_resource_sha256': inventory(previous),
        'reference_woodland_resource_sha256': inventory(canonical),
        'historical_proof_sha256': {relative(project, path): sha(path)
                                  for path in (historical_proof, historical_addendum, regression_path)}}
    snapshot_path = output / 'input-snapshot.json'
    if args.capture_inputs:
        assert not snapshot_path.exists(), 'Do not replace an existing input/protected-data snapshot.'
        snapshot_path.write_text(json.dumps({'schema_version': 1,
            'id': 'pine_ridge_u34_independent_repeat_input_snapshot_v1',
            'scope': 'Captured after the first draft generation and before the independent repeat; not a historical generation proof rerun.',
            **fingerprints}, indent=2) + '\n', encoding='utf-8')
        print('Pine inputs and protected historical/reference data snapshotted before independent repeat.')
        return
    snapshot = read(snapshot_path)
    assert all(snapshot[key] == value for key, value in fingerprints.items()), 'Source/input/protected data changed during repeat.'
    repeat = (args.repeat_root or output / 'repeat/formats-draft').resolve()
    first_resources = inventory(pine)
    repeat_resources = inventory(repeat / ('resources/Biathlon/Venues/' + VENUE))
    assert first_resources and first_resources == repeat_resources, 'Independent Pine repeat resources differ.'
    static, formats, environment = [read(draft / (name + '.json')) for name in
                                     ('static-report', 'formats-verification', 'environment-verification')]
    regression = read(regression_path)
    assert static['static_checks_pass'] and not static['issues']
    assert formats['maximum_world_edge_length_error_m'] < 1e-8
    assert formats['maximum_directed_seam_world_gap_m'] == 0
    assert environment['full_runtime_ribbon_triangle_clearance']['buried_ribbon_triangle_count'] == 0
    assert environment['full_runtime_ribbon_triangle_clearance']['minimum_ribbon_minus_terrain_m'] >= .025
    assert regression['status'] == 'PASS' and regression['canonical_bytes_unchanged']
    assert regression['independent_generations_match'] and regression['canonical_resource_count'] == 205
    for name, digest in regression['generator_sha256'].items():
        assert sha(SCRIPT / name) == digest, ('Referenced Venue01 byte proof uses different source', name)
    for name, digest in formats['source_hashes'].items():
        assert sha(pine / (name + '.json')) == digest
    assert sha(pine / 'environment.json') == environment['environment_sha256']
    range_contract = shared_range_contract(canonical, pine)
    deltas = previous_draft_contract(previous, pine)
    env = read(pine / 'environment.json')
    forest = env['forest']
    trees = [instance for group in forest['groups'] for instance in
             read(draft / 'resources' / (group['instances_resource'] + '.json'))['instances']]
    scales = [instance['scale'] for instance in trees]
    mass_audit = read(output / 'mass-draft/audit.json')
    previous_environment = read(project / 'artifacts/wp170/formats-draft/environment-verification.json')
    current_scripts = {relative(project, SCRIPT / name): sha(SCRIPT / name)
                       for name in (*GENERATOR_SOURCES, 'audit_pine_ridge_draft.py', 'test_pine_ridge_geometry.py')}
    old_addendum = read(historical_addendum)
    addendum = {'schema_version': 1, 'id': 'pine_ridge_u34_generator_source_addendum_v1',
                'status': 'CURRENT_SOURCE_MATCHES_RECORDED_U_DEFAULT_205_BYTE_PROOF_HISTORICAL_203_PRESERVED',
                'historical_203_addendum': relative(project, historical_addendum),
                'historical_203_addendum_sha256': sha(historical_addendum),
                'historical_203_rerun': False,
                'recorded_current_source_205_regression': relative(project, regression_path),
                'recorded_current_source_205_regression_sha256': sha(regression_path),
                'recorded_205_recipe_input_sha256': regression['recipe_input_sha256'],
                'recorded_205_regression_generator_sha256': regression['generator_sha256'],
                'current_sources_match_recorded_205': True,
                'changed_since_historical_203': {key: {'historical': value, 'current': current_scripts.get(key)}
                    for key, value in old_addendum['current_generator_sha256'].items()
                    if current_scripts.get(key) != value},
                'helper_dependency': {'path': 'scripts/content/m13/environment_serialization.py',
                                      'sha256': sha(SCRIPT / 'environment_serialization.py')},
                'scope': 'References existing U proof of the 205-file pre-compound O34/fog checkpoint with compound omitted; Pine explicitly opts into compound. No Venue01 generator/recipe/resource writes or new Venue01 generation in this Pine task.'}
    report = {'schema_version': 1, 'id': 'pine_ridge_wp170_updated_draft_acceptance_v2',
              'status': 'DRAFT_STATIC_DATA_PASS_RUNTIME_VISUAL_BALANCE_WEATHER_PERFORMANCE_PENDING',
              'venue_id': 'pine_ridge_v1', 'wp170_complete': False,
              'replacement_label': 'Replaces the initial isolated Pine content candidate only; historical acceptance proof remains unchanged.',
              'previous_draft_proof': relative(project, historical_proof),
              'previous_draft_proof_sha256': sha(historical_proof),
              'authority_boundary': 'Unimported draft resources only; no Assets/C#/catalog/calendar/core/state/Git mutation by this task.',
              'design_source': 'docs/proposals/M13-LAUNCH-VENUE-IDENTITY-RECIPES.md',
              'reference_woodland_root': relative(project, canonical),
              'geometry_comparison': {'woodland_basin_current_compound': course_metrics(canonical),
                                      'pine_ridge_previous': course_metrics(previous), 'pine_ridge_updated': course_metrics(pine)},
              'previous_draft_geometry_delta': deltas,
              'milestones_m': mass_audit['milestones_m'],
              'ridge_span_m': mass_audit['plateau_span_m'], 'ridge_actual_travel_m': mass_audit['plateau_actual_travel_m'],
              'shared_range': range_contract,
              'formats': formats['formats'], 'format_branches': read(draft / 'formats-audit.json')['branches'],
              'static_diagnostic_count': len(static['issues']), 'exact_chord_seam_proof': formats,
              'environment_proof': environment,
              'environment_delta': {'tile_count_change': environment['tile_count'] - previous_environment['tile_count'],
                                    'forest_group_count_change': environment['forest_group_count'] - previous_environment['forest_group_count'],
                                    'tree_count_change': environment['tree_count'] - previous_environment['tree_count'],
                                    'reason': 'The exit-tail extension translates the outer Main/branches and penalty/start by 34 m west, shortens recovery, and changes tile bounds and forest exclusions. Forest recipe identity is retained; instance bytes/counts are intentionally regenerated.'},
              'lighting': env['lighting'],
              'forest_identity': {**environment['original_venue_identity'], 'minimum_actual_scale': min(scales),
                                  'maximum_actual_scale': max(scales), 'mean_actual_scale': sum(scales) / len(scales),
                                  'environment_recipe': read(SCRIPT / 'recipes/pine_ridge_environment_recipe_v1.json')},
              'deterministic_pine_repeat': {'independent_generations': 2, 'resource_count': len(first_resources),
                                           'byte_difference_count': 0, 'repeat_root': relative(project, repeat),
                                           'input_snapshot_sha256': sha(snapshot_path),
                                           'dependencies_and_protected_data_unchanged_during_repeat': True,
                                           'snapshot_scope': snapshot['scope']},
              'venue01_byte_regression_reference': addendum,
              'source_code_sha256': current_scripts,
              'recipe_sha256': {path.name: sha(path) for path in sorted((SCRIPT / 'recipes').glob('pine_ridge_*.json'))},
              'draft_resource_sha256': first_resources,
              'proof_sha256': {path.name: sha(path) for path in [draft / 'static-report.json', draft / 'formats-verification.json',
                                                               draft / 'environment-verification.json', snapshot_path, regression_path]},
              'pending_acceptance': ['Cold Unity import and actual groomed closed-seam mesh/terrain test',
                                     'All format race admission, dense pack liveness, deterministic results and replay',
                                     'Dawn forest/ridge/stadium visibility and full snow-join camera review',
                                     'Worst-condition weather readability, LOD and performance',
                                     'Race-balance calibration of the longer .195608-grade climb'],
              'known_visual_risk': 'The independent float32 TrackRibbonBuilder reconstruction is not the final Unity-groomed mesh. Conservative capping creates up to %.6f m separation below the Main. Actual snow joins, dawn/fog and the steeper venue require Unity inspection; no visual PASS is claimed.' % environment['full_runtime_ribbon_triangle_clearance']['maximum_ribbon_minus_terrain_m']}
    for name, value in (('pine-ridge-updated-draft-verification.json', report), ('source-regression-addendum.json', addendum)):
        (output / name).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'status': report['status'], 'static_diagnostics': 0, 'shared_loops': 33,
                      'draft_resource_count': len(report['draft_resource_sha256']),
                      'full_triangle_clearance': environment['full_runtime_ribbon_triangle_clearance']['minimum_ribbon_minus_terrain_m']}, indent=2))


if __name__ == '__main__':
    main()
