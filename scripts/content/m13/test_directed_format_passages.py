"""Optional passage authority must preserve source geometry and legacy bytes."""
from pathlib import Path
import bisect
import json
import math
import subprocess
import sys
import tempfile
import unittest

PROJECT = Path(__file__).resolve().parents[3]
SCRIPT = Path(__file__).resolve().parent
PREFIX = Path('resources/Biathlon/Venues/woodland_basin_v1')


class DirectedFormatPassageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory(prefix='biathlon-directed-passages-')
        cls.root = Path(cls.temp.name)
        cls.base = cls.root / 'mass'
        result = subprocess.run([sys.executable, str(SCRIPT / 'generate_venue01.py'),
                                 '--project-root', str(PROJECT), '--output-root', str(cls.base)],
                                capture_output=True, text=True)
        if result.returncode:
            raise AssertionError(result.stdout + result.stderr)
        cls.recipe = json.loads((SCRIPT / 'recipes/woodland_basin_formats_recipe_v1.json').read_text(encoding='utf-8-sig'))
        cls.roots = {}
        for name, flag in (('omitted', None), ('exclusive', False), ('directed', True), ('repeat', True)):
            recipe = dict(cls.recipe)
            recipe.pop('directed_format_passages', None)
            if flag is not None:
                recipe['directed_format_passages'] = flag
            path = cls.root / (name + '.json')
            path.write_text(json.dumps(recipe), encoding='utf-8')
            output = cls.root / name
            result = cls.generate(path, output)
            if result.returncode:
                raise AssertionError(result.stdout + result.stderr)
            cls.roots[name] = output

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    @classmethod
    def generate(cls, recipe, output):
        return subprocess.run([sys.executable, str(SCRIPT / 'generate_format_branches.py'),
                               '--project-root', str(PROJECT), '--base-root', str(cls.base),
                               '--recipe', str(recipe), '--output-root', str(output)], capture_output=True, text=True)

    def files(self, name):
        root = self.roots[name] / 'resources'
        return {p.relative_to(root).as_posix(): p.read_bytes() for p in root.rglob('*.json')}

    def read(self, name, resource):
        return json.loads((self.roots[name] / PREFIX / (resource + '.json')).read_text(encoding='utf-8-sig'))

    def test_omitted_and_false_keep_exact_resource_bytes(self):
        self.assertEqual(self.files('omitted'), self.files('exclusive'))
        self.assertNotIn('pursuit_directed_passages.json', {Path(p).name for p in self.files('omitted')})

    def test_opt_in_adds_only_authority_and_pursuit_catalog_binding(self):
        old, new = self.files('exclusive'), self.files('directed')
        self.assertEqual(set(new) - set(old), {'Biathlon/Venues/woodland_basin_v1/pursuit_directed_passages.json'})
        self.assertEqual([p for p in old if old[p] != new[p]], ['Biathlon/Venues/woodland_basin_v1/format_bindings.json'])
        before, after = self.read('exclusive', 'format_bindings'), self.read('directed', 'format_bindings')
        pursuit = next(p for p in after['formats'] if p['id'] == 'pursuit')
        self.assertEqual(pursuit.pop('directed_passages_resource'), 'Biathlon/Venues/woodland_basin_v1/pursuit_directed_passages')
        self.assertEqual(before, after)
        self.assertEqual(self.files('directed'), self.files('repeat'))

    def test_every_corridor_retains_authoritative_knots_and_exact_world_chords(self):
        authority = self.read('directed', 'pursuit_directed_passages')
        visual = {p['loop_id']: p['knots'] for p in self.read('directed', 'visual')['loops']}
        for passage in authority['passages']:
            for name in ('source', 'target'):
                corridor = passage[name]
                original = visual[corridor['loop_id']]
                distances = [p['distance_m'] for p in original]
                knots = corridor['knots']
                self.assertEqual(knots[1:-1], [p for p in original if knots[0]['distance_m'] < p['distance_m'] < knots[-1]['distance_m']])
                for p in knots:
                    self.assertAlmostEqual(p['distance_m'] * 1000, round(p['distance_m'] * 1000), places=7)
                    i = min(bisect.bisect_right(distances, p['distance_m']) - 1, len(original) - 2)
                    a, b = original[i:i+2]
                    fraction = (p['distance_m'] - a['distance_m']) / (b['distance_m'] - a['distance_m'])
                    for axis in ('x', 'y', 'z'):
                        self.assertEqual(p[axis], a[axis] + (b[axis] - a[axis]) * fraction if p not in original else p[axis])
                for a, b in zip(knots, knots[1:]):
                    actual = math.dist(tuple(a[p] for p in ('x', 'y', 'z')), tuple(b[p] for p in ('x', 'y', 'z')))
                    self.assertAlmostEqual(actual, b['distance_m'] - a['distance_m'], places=8)
            self.assertEqual({axis: passage['source']['knots'][-1][axis] for axis in ('x', 'y', 'z')},
                             {axis: passage['target']['knots'][0][axis] for axis in ('x', 'y', 'z')})

    def test_windows_cover_guards_body_and_additional_authored_guard_extent(self):
        authority = self.read('directed', 'pursuit_directed_passages')
        connections = {p['id']: p for p in self.read('directed', 'connections')['connections']}
        regions = {p['id']: p for p in self.read('directed', 'junctions')['junctions']}
        body = json.loads((PROJECT / 'Assets/Resources/Biathlon/occupancy_provisional_v1.json').read_text(encoding='utf-8-sig'))
        half = (body['body_length_m'] + body['personal_clearance_m']) / 2
        self.assertEqual(authority['occupancy_definition_id'], body['id'])
        self.assertEqual(authority['validation_policy_resource'], self.read('directed', 'start_continuation')['validation_policy_resource'])
        self.assertEqual(len(authority['passages']), 2)
        for passage in authority['passages']:
            connection = connections[passage['connection_id']]
            region = regions[connection['junction_id']]
            self.assertEqual(len(region['ports']), 2)
            ports = {p['id']: p for p in region['ports']}
            incoming, outgoing = ports[connection['from_port_id']], ports[connection['to_port_id']]
            source, target = passage['source']['knots'], passage['target']['knots']
            self.assertEqual(source[-1]['distance_m'], incoming['anchor']['distance_m'])
            self.assertEqual(target[0]['distance_m'], outgoing['anchor']['distance_m'])
            self.assertLessEqual(source[0]['distance_m'], source[-1]['distance_m'] - 2 * incoming['guard_before_m'] - half)
            self.assertGreaterEqual(target[-1]['distance_m'], target[0]['distance_m'] + 2 * outgoing['guard_after_m'] + half)

    def test_malformed_flag_rejects_before_creating_or_mutating_output(self):
        for number, value in enumerate((None, 0, 1, 'true', [], {})):
            with self.subTest(value=value):
                recipe = self.root / ('invalid' + str(number) + '.json')
                recipe.write_text(json.dumps(dict(self.recipe, directed_format_passages=value)), encoding='utf-8')
                absent = self.root / ('absent' + str(number))
                self.assertNotEqual(self.generate(recipe, absent).returncode, 0)
                self.assertFalse(absent.exists())
                existing = self.root / ('existing' + str(number))
                existing.mkdir()
                sentinel = existing / 'accepted.bin'
                sentinel.write_bytes(b'accepted existing output')
                self.assertNotEqual(self.generate(recipe, existing).returncode, 0)
                self.assertEqual(list(existing.iterdir()), [sentinel])
                self.assertEqual(sentinel.read_bytes(), b'accepted existing output')


if __name__ == '__main__':
    unittest.main()
