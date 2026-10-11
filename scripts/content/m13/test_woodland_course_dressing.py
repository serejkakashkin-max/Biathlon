"""Roadside authoring geometry and rejection tests; no Unity or Assets mutation."""
import copy
import importlib.util
import json
import math
from pathlib import Path
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
SPEC = importlib.util.spec_from_file_location('dressing', HERE / 'dress_woodland_course.py')
DRESS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(DRESS)
PROJECT = HERE.parents[2]


class WoodlandCourseDressingTests(unittest.TestCase):
    def test_sign_clearance_uses_full_native_bounds_and_all_paid_roads(self):
        import rebind_woodland_signs as signs
        environment = {'route_corridors': [{'knots': [{'x': 0., 'z': 0.}, {'x': 0., 'z': 20.}], 'widths_m': [8., 8.]}]}
        # A sign centre can be beside the tape while its wide footplate remains in
        # the ski lane. The complete rotated native footprint governs placement.
        bounds = ((-2., 0., -1.), (2., 4., 1.))
        self.assertAlmostEqual(signs.clearance(environment, DRESS.footprint(bounds, (8.3, 0., 10.), 0.)), 2.3)
        self.assertAlmostEqual(signs.clearance(environment, DRESS.footprint(bounds, (10.3, 0., 10.), 0.)), 4.3)
        environment['route_corridors'].append({'knots': [{'x': 10., 'z': 0.}, {'x': 10., 'z': 20.}], 'widths_m': [6., 6.]})
        self.assertLess(signs.clearance(environment, DRESS.footprint(bounds, (10.3, 0., 10.), 0.)), 0.)

    def test_saved_native_signs_clear_the_outer_fence_line(self):
        import rebind_woodland_signs as signs
        import author_short_course_dressing as author
        environment = DRESS.read_json(PROJECT / 'Assets/Resources/Biathlon/Venues/woodland_basin_v1/environment.json')
        policy = DRESS.read_json(HERE / 'woodland_sign_clearance_v1.json')
        checked = 0
        for placement in environment['assets']:
            if not placement['resource'].endswith(('/FlagStela', '/Billboard')):
                continue
            bounds = author.prefab_bounds(PROJECT, placement['resource']);p = placement['position']
            gap = signs.clearance(environment, DRESS.footprint(bounds, (p['x'], 0., p['z']), placement['yaw_deg']))
            self.assertGreaterEqual(gap + .00001, policy['minimum_course_edge_clearance_m'], placement['id'])
            checked += 1
        self.assertEqual(checked, 36)

    def test_saved_banner_full_footprint_retains_snow_contact_even_if_already_roadside(self):
        environment = DRESS.read_json(PROJECT / 'Assets/Resources/Biathlon/Venues/woodland_basin_v1/environment.json')
        grid, spacing = DRESS.terrain_grid(PROJECT, environment)
        groups = {}
        for p in environment['mesh_assets']:
            if p['id'].startswith('course_') and '_banner_' in p['id']:
                groups.setdefault(p['id'].rsplit('_', 1)[0], []).append(p)
        self.assertEqual(len(groups), 6)
        for identity, parts in groups.items():
            bounds = DRESS.merged_bounds([DRESS.mesh_bounds(DRESS.resource_path(PROJECT, p['mesh_resource'], '.asset')) for p in parts])
            p = parts[0];position = p['position']
            footprint = DRESS.footprint(bounds, (position['x'], 0., position['z']), p['yaw_deg'])
            heights = [y for _, y in DRESS.foundation_samples(grid, spacing, footprint)]
            self.assertAlmostEqual(position['y'] + bounds[0][1], min(heights), places=5, msg=identity)
            self.assertLessEqual(max(heights) - min(heights), .25, identity)

    def test_saved_standing_boot_contacts_reject_steep_terrain_before_placement(self):
        contacts = DRESS.standing_contact_geometry(PROJECT)
        feet, anchors = contacts
        self.assertEqual(len(anchors), 16)
        self.assertTrue(all(abs(p[1]) < 1e-6 for p in feet))
        self.assertGreater(max(p[0] for p in feet) - min(p[0] for p in feet), .27)
        template = {'maximum_foot_contact_gap_m': .03}
        flat = {(x, z): 0. for x in range(-16, 25, 8) for z in range(-16, 25, 8)}
        self.assertEqual(DRESS.require_standing_contact(flat, 8., (0., 0., 0.), 0., contacts, template), 0.)
        for yaw in (0., 90.):
            # Actual two-foot vertices, including fore/aft support, cross this slope.
            steep = {(x, z): .6 * (x + z) for x, z in flat}
            with self.subTest(yaw=yaw), self.assertRaisesRegex(ValueError, 'boot contacts'):
                DRESS.require_standing_contact(steep, 8., (0., 0., 0.), yaw, contacts, template)
        for limit in (None, True, float('nan'), float('inf'), .030001, 0.):
            with self.subTest(limit=limit), self.assertRaisesRegex(ValueError, 'foot-contact bound'):
                DRESS.require_standing_contact(flat, 8., (0., 0., 0.), 0., contacts, {'maximum_foot_contact_gap_m': limit})

    def test_crossing_segment_and_inside_endpoint_are_zero_clearance(self):
        polygon = [(0., 0.), (2., 0.), (2., 2.), (0., 2.)]
        self.assertEqual(DRESS.segment_polygon((-10., 1.), (10., 1.), polygon), 0.)
        self.assertEqual(DRESS.segment_polygon((1., 1.), (5., 5.), polygon), 0.)
        self.assertEqual(DRESS.segment_polygon((-1., -1.), (-1., -1.), polygon), math.sqrt(2))

    def test_endpoint_clearance_is_not_an_infinite_line_distance(self):
        polygon = [(3., -1.), (5., -1.), (5., 1.), (3., 1.)]
        self.assertEqual(DRESS.segment_polygon((0., 0.), (1., 0.), polygon), 2.)
        # Near a corner, the union of adjacent route capsules remains protected.
        self.assertEqual(DRESS.segment_polygon((0., 0.), (0., 5.), polygon), 3.)

    def test_native_offcentre_bound_is_rotated_in_unity_yaw_direction(self):
        bounds = ((-.5, 0., .08), (.5, 1., 31.29))
        polygon = DRESS.footprint(bounds, (100., 8., 200.), -90.)
        self.assertAlmostEqual(min(p[0] for p in polygon), 68.71)
        self.assertAlmostEqual(max(p[0] for p in polygon), 99.92)
        self.assertAlmostEqual(min(p[1] for p in polygon), 199.5)
        self.assertAlmostEqual(max(p[1] for p in polygon), 200.5)

    def test_ground_uses_actual_triangle_diagonal_not_bilinear_height(self):
        grid = {(0., 0.): 0., (0., 8.): 0., (8., 0.): 0., (8., 8.): 8.}
        self.assertEqual(DRESS.ground(grid, 8., 2., 2.), 0.)
        self.assertEqual(DRESS.ground(grid, 8., 6., 6.), 4.)

    def test_foundation_overlay_finds_peak_missed_by_corners_edges_and_centre(self):
        grid = {(x, z): 0. for x in range(0, 41, 8) for z in range(0, 41, 8)}
        grid[(8, 8)] = 6.
        polygon = [(0., 0.), (24., 0.), (24., 24.), (0., 24.)]
        sparse = polygon + [(12., 0.), (24., 12.), (12., 24.), (0., 12.), (12., 12.)]
        self.assertEqual(max(DRESS.ground(grid, 8., *p) for p in sparse), 0.)
        actual = DRESS.foundation_samples(grid, 8., polygon)
        self.assertEqual(max(y for _, y in actual), 6.)
        self.assertIn(((8., 8.), 6.), actual)

    def test_resource_traversal_and_assets_output_reject_before_writing(self):
        for value in ('../x', 'Biathlon/../x', '/absolute', 'Biathlon\\x'):
            with self.subTest(value=value), self.assertRaises(ValueError):
                DRESS.resource_path(PROJECT, value, '.asset')
        target = PROJECT / 'Assets/RejectedCourseDressing'
        self.assertFalse(target.exists())
        with self.assertRaises(ValueError):
            DRESS.run(PROJECT, HERE / 'recipes/woodland_basin_course_dressing_v1.json', target)
        self.assertFalse(target.exists())

    def test_viewing_strip_follows_actual_course_frame_without_broad_forest_clearance(self):
        visual = {'loops': [{'loop_id': 'main', 'knots': [
            {'distance_m': 0., 'x': 0., 'y': 10., 'z': 0.},
            {'distance_m': 100., 'x': -100., 'y': 10., 'z': 0.}]}]}
        item = {'loop_id': 'main', 'distance_m': 50., 'polygon_forward_lateral_m': [[-.090001, 8.325], [31.290001, 8.325], [9.76, 17.05032501], [-9.76, 17.05032501]]}
        polygon = DRESS.viewing_strip(visual, item)
        self.assertAlmostEqual(min(p[0] for p in polygon), -81.290001)
        self.assertAlmostEqual(max(p[0] for p in polygon), -40.24)
        self.assertAlmostEqual(min(p[1] for p in polygon), 8.325)
        self.assertAlmostEqual(max(p[1] for p in polygon), 17.05032501)
        self.assertEqual(DRESS.point_polygon((-50., 12.), polygon), 0.)
        # The skewed fence/stand edges must stay trapezoidal. A rectangle using
        # their union bounds would unnecessarily remove this outside tree.
        self.assertGreater(DRESS.point_polygon((-75., 16.), polygon), 4.)
        self.assertEqual(DRESS.point_polygon((-75., 9.), polygon), 0.)
        self.assertGreater(DRESS.point_polygon((-120., 12.), polygon), 30.)
        item['polygon_forward_lateral_m'][2] = [-.01, 8.326]
        with self.assertRaises(ValueError):
            DRESS.viewing_strip(visual, item)
        item['polygon_forward_lateral_m'][2] = [9.76, 17.05032501]
        item['polygon_forward_lateral_m'][1][0] = 60.
        with self.assertRaises(ValueError):
            DRESS.viewing_strip(visual, item)

    def test_slope_banner_embedding_removes_floating_base_without_altering_rigid_foundations(self):
        heights = [10., 10.1572, 10.08]
        self.assertEqual(DRESS.foundation_y(heights, 0., {}), 10.1572)
        template = {'foundation_mode': 'embed_footing_into_snow', 'maximum_snow_embedding_m': .25}
        y = DRESS.foundation_y(heights, 0., template)
        self.assertEqual(y, 10.)
        self.assertTrue(all(y <= ground for ground in heights), 'No sampled bottom point can float above the snow')
        self.assertLessEqual(max(heights) - y, template['maximum_snow_embedding_m'])
        with self.assertRaises(ValueError):
            DRESS.foundation_y([10., 10.25001], 0., template)
        with self.assertRaises(ValueError):
            DRESS.foundation_y(heights, 0., {'foundation_mode': 'unknown'})

    def test_nonfinite_clearance_rejects_before_candidate_mutation(self):
        recipe = DRESS.read_json(HERE / 'recipes/woodland_basin_course_dressing_v1.json')
        with tempfile.TemporaryDirectory(dir=PROJECT / 'artifacts') as directory:
            root = Path(directory); path = root / 'invalid.json'; output = root / 'candidate'
            for value in (float('nan'), float('inf'), -1, True):
                bad = copy.deepcopy(recipe); bad['minimum_course_edge_clearance_m'] = value
                path.write_text(json.dumps(bad), encoding='utf-8')
                with self.subTest(value=value), self.assertRaises(ValueError):
                    DRESS.run(PROJECT, path, output)
                self.assertFalse(output.exists())

    def test_detached_source_cannot_point_into_assets_or_outside_artifacts(self):
        with tempfile.TemporaryDirectory(dir=PROJECT / 'artifacts') as directory:
            output = Path(directory) / 'candidate'
            for source in (PROJECT / 'Assets/Resources', PROJECT, Path(directory) / 'missing'):
                with self.subTest(source=source), self.assertRaisesRegex(ValueError, 'Detached source'):
                    DRESS.run(PROJECT, HERE / 'recipes/woodland_basin_course_dressing_v1.json', output, source)
                self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
