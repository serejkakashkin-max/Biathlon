"""Fast exact-chord checks plus shared-range/identity integration for the draft."""
from pathlib import Path
import json
import math
import shutil
import tempfile
import unittest
from exact_polyline import exact_paired_chords, ridge_span
from audit_pine_ridge_draft import previous_draft_contract

PROJECT = Path(__file__).resolve().parents[3]
SCRIPT = Path(__file__).resolve().parent


class ExactRidgeTests(unittest.TestCase):
    def test_paired_chords_keep_fixed_controls_and_exact_total(self):
        controls = [(0., 0., 0.), (3., .2, 0.), (6., 0., 0.), (9., -.2, 0.), (12., 0., 0.)]
        points, lengths = exact_paired_chords(controls, 12028)
        self.assertEqual(points[::2], controls[::2])
        self.assertEqual(sum(lengths), 12028)
        self.assertTrue(all(isinstance(length, int) for length in lengths))
        self.assertLess(max(abs(math.dist(a, b) - length / 1000)
                            for a, b, length in zip(points, points[1:], lengths)), 1e-10)
        self.assertEqual(exact_paired_chords(controls, 12028), (points, lengths))

    def test_impossible_chord_budget_is_rejected(self):
        with self.assertRaises(AssertionError):
            exact_paired_chords([(0., 0., 0.), (3., 0., 0.), (6., 0., 0.)], 5999)

    def test_ridge_quantization_retains_rolling_controls_and_loop_budget(self):
        recipe = json.loads((SCRIPT / 'recipes/pine_ridge_geometry_recipe_v1.json').read_text())
        span, points, lengths = ridge_span(recipe['plateau_controls'], 1151.98, 157.415, -878.75, .03)
        self.assertAlmostEqual(span + sum(lengths) / 1000, 1151.98, places=9)
        self.assertEqual(points[0], (0., 157.415, -878.75))
        self.assertEqual(points[-1], (span, 157.415, -878.75))
        self.assertAlmostEqual(points[2][1], 164.415, places=9)
        self.assertAlmostEqual(points[6][1], 150.415, places=9)
        self.assertEqual(points[2][2], -902.75)
        self.assertEqual(points[6][2], -854.75)
        self.assertGreater(sum(lengths) / 1000, span + 9)

    def test_historical_draft_keeps_range_geometry_and_has_real_new_identity(self):
        pine = PROJECT / 'artifacts/wp170/formats-draft/resources/Biathlon/Venues/pine_ridge_v1'
        old = PROJECT / 'artifacts/wp169j/formats-draft/resources/Biathlon/Venues/woodland_basin_v1'
        read = lambda root, name: json.loads((root / (name + '.json')).read_text())
        tracks = [read(root, 'track') for root in (old, pine)]
        visuals = [read(root, 'visual') for root in (old, pine)]
        self.assertEqual(read(old, 'paths')['lanes'], read(pine, 'paths')['lanes'])
        self.assertEqual(read(old, 'manifest')['range_layout'], read(pine, 'manifest')['range_layout'])
        for collection, key in ((tracks, 'id'), (visuals, 'loop_id')):
            loops = [{loop[key]: loop for loop in source['loops']} for source in collection]
            shared = set(loops[0]) - {'main', 'pursuit_shortcut', 'sprint_extension'}
            self.assertEqual(len(shared), 33)
            for loop_id in shared:
                self.assertEqual(loops[0][loop_id], loops[1][loop_id], loop_id)
        main = next(loop for loop in tracks[1]['loops'] if loop['id'] == 'main')
        self.assertEqual(round(sum(segment['length_m'] for segment in main['segments']) * 1000), 3000000)
        climb = next(segment for segment in main['segments'] if segment['semantic_zone'] == 'climb_long')
        self.assertEqual(climb['length_m'], 820)
        self.assertGreater(climb['start_grade'], .19)
        self.assertFalse(climb['draft_allowed'])
        self.assertEqual({segment['width_m'] for segment in main['segments']}, {6, 7, 7.5, 8})
        self.assertEqual({segment['wind_exposure'] for segment in main['segments']},
                         {.1, .15, .22, .25, .35, .7, .8, .85, .95})
        self.assertTrue(read(PROJECT / 'artifacts/wp170/formats-draft', 'static-report')['static_checks_pass'])

    def test_updated_draft_keeps_private_geometry_and_exact_full_formats(self):
        root = PROJECT / 'artifacts/wp170/u34-compound-draft/formats-draft'
        pine = root / 'resources/Biathlon/Venues/pine_ridge_v1'
        previous = PROJECT / 'artifacts/wp170/formats-draft/resources/Biathlon/Venues/pine_ridge_v1'
        reference = PROJECT / 'artifacts/wp169u/final-format-draft/resources/Biathlon/Venues/woodland_basin_v1'
        read = lambda base, name: json.loads((base / (name + '.json')).read_text())
        private = lambda base: {loop['loop_id']: loop for loop in read(base, 'visual')['loops']
                                if loop['loop_id'].startswith('mat_')}
        self.assertEqual(len(private(pine)), 30)
        self.assertEqual(private(pine), private(previous))
        self.assertEqual(read(pine, 'paths')['lanes'], read(reference, 'paths')['lanes'])
        paths = read(pine, 'paths')['lanes']
        self.assertEqual({round(sum(section['end_m'] - section['start_m'] for section in path['slices']), 3)
                          for path in paths}, {269.408})
        self.assertEqual(read(pine, 'manifest')['range_layout'], read(reference, 'manifest')['range_layout'])
        track = read(pine, 'track')
        loops = {loop['id']: loop for loop in track['loops']}
        self.assertEqual(round(sum(segment['length_m'] for segment in loops['main']['segments']), 3), 3000)
        self.assertEqual(round(sum(segment['length_m'] for segment in loops['exit_aisle']['segments']), 3), 230)
        self.assertEqual(round(sum(segment['length_m'] for segment in loops['penalty_150']['segments']), 3), 150)
        climb = next(segment for segment in loops['main']['segments'] if segment['semantic_zone'] == 'climb_long')
        self.assertEqual((climb['length_m'], climb['width_m'], climb['wind_exposure'], climb['draft_allowed']),
                         (820, 7, .22, False))
        junctions = {region['id']: region for region in read(pine, 'junctions')['junctions']}
        self.assertNotIn('bypass_turn', junctions)
        self.assertEqual({port['id'] for port in junctions['range_rejoin']['ports']},
                         {'rejoin_exit', 'rejoin_main', 'rejoin_penalty', 'bypass_in', 'bypass_out'})
        self.assertEqual(read(pine, 'environment')['lighting']['fog_distance_scale'], 2.7)
        static = read(root, 'static-report')
        self.assertTrue(static['static_checks_pass'])
        self.assertEqual(static['issues'], [])
        formats = read(root, 'formats-verification')
        self.assertEqual({item['format']: item['distance_m'] for item in formats['formats']},
                         {'mass_start': 15000, 'sprint': 10000, 'pursuit': 12500})
        self.assertEqual(formats['maximum_directed_seam_world_gap_m'], 0)
        self.assertLess(formats['maximum_world_edge_length_error_m'], 1e-8)
        clearance = read(root, 'environment-verification')['full_runtime_ribbon_triangle_clearance']
        self.assertEqual(clearance['buried_ribbon_triangle_count'], 0)
        self.assertGreaterEqual(clearance['minimum_ribbon_minus_terrain_m'], .025)

    def test_replacement_audit_rejects_a_lost_private_body_guard(self):
        previous = PROJECT / 'artifacts/wp170/formats-draft/resources/Biathlon/Venues/pine_ridge_v1'
        current = PROJECT / 'artifacts/wp170/u34-compound-draft/formats-draft/resources/Biathlon/Venues/pine_ridge_v1'
        with tempfile.TemporaryDirectory() as directory:
            changed = Path(directory)
            for name in ('track', 'visual', 'paths', 'junctions', 'connections'):
                shutil.copyfile(current / (name + '.json'), changed / (name + '.json'))
            self.assertEqual(previous_draft_contract(previous, changed)['private_physical_loops_unchanged'], 30)
            path = changed / 'junctions.json'
            definition = json.loads(path.read_text())
            region = next(item for item in definition['junctions'] if item['id'] == 'merge_01')
            region['ports'][0]['guard_before_m'] = 0
            path.write_text(json.dumps(definition))
            with self.assertRaises(AssertionError):
                previous_draft_contract(previous, changed)


if __name__ == '__main__':
    unittest.main()
