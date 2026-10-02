"""Focused cold CLI contract tests with isolated synthetic generator processes."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

PROJECT = Path(__file__).resolve().parents[3]
SOURCE = PROJECT / 'scripts/content/m13/regress_venue01_defaults.py'
PREFIX = Path('resources/Biathlon/Venues/woodland_basin_v1')

GENERATOR = '''from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('--project-root',type=Path);p.add_argument('--output-root',type=Path);p.add_argument('--base-root');p.add_argument('--recipe',type=Path);a=p.parse_args()
a.output_root.mkdir(parents=True,exist_ok=True)
if Path(__file__).name=='generate_environment.py':
 mode=json.loads(a.recipe.read_text()).get('mode') if a.recipe else None
 for source in sorted((a.project_root/'expected').glob('*.json')):
  target=a.output_root/'resources/Biathlon/Venues/woodland_basin_v1'/source.name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(source.read_bytes())
 if mode=='mismatch' or (mode=='divergent' and 'run-b' in a.output_root.parts):target.write_text('{"different":true}')
 if mode=='mutate_helper':(Path(__file__).parent/'environment_serialization.py').write_text('# changed during execution')
 if mode=='mutate_recipe':a.recipe.write_text(a.recipe.read_text()+' ')
'''

class RegressionCliTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix='biathlon-regression-cli-')
        self.root = Path(self.directory.name)
        self.scripts = self.root / 'scripts'
        self.scripts.mkdir()
        shutil.copyfile(SOURCE, self.scripts / SOURCE.name)
        for name in ['generate_venue01.py', 'generate_format_branches.py', 'generate_environment.py']:
            (self.scripts / name).write_text(GENERATOR)
        for name in ['verify_environment.py', 'exact_polyline.py', 'corridor_sampling.py', 'environment_serialization.py']:
            (self.scripts / name).write_text('# isolated dependency\n')
        self.geometry = self.root / 'geometry.json'
        self.formats = self.root / 'formats.json'
        self.environment = self.root / 'environment.json'
        for path in [self.geometry, self.formats, self.environment]:
            path.write_text('{}')
        self.canonical = self.root / 'canonical'
        self.output = self.root / 'output'
        self.expected = self.root / 'expected'
        self.expected.mkdir()
        self.canonical.joinpath(PREFIX).mkdir(parents=True)
        self.fixture(2)

    def tearDown(self):
        self.directory.cleanup()

    def fixture(self, count):
        for index in range(count):
            name = f'resource_{index:03}.json'
            content = json.dumps({'schema_version': 1, 'id': f'resource_{index}', 'value': index}) + '\n'
            (self.expected / name).write_text(content)
            (self.canonical / PREFIX / name).write_text(content)

    def run_cli(self, *extra, output=None):
        command = [sys.executable, '-B', str(self.scripts / SOURCE.name),
                   '--project-root', str(self.root), '--canonical-root', str(self.canonical),
                   '--output-root', str(output or self.output), '--geometry-recipe', str(self.geometry),
                   '--formats-recipe', str(self.formats), *map(str, extra)]
        return subprocess.run(command, capture_output=True, text=True, timeout=30)

    def report(self):
        return json.loads((self.output / 'regression.json').read_text())

    def test_optional_count_environment_and_all_dependency_hashes_pass(self):
        result = self.run_cli('--environment-recipe', self.environment)
        self.assertEqual(result.returncode, 0, result.stderr)
        report = self.report()
        self.assertEqual(report['status'], 'PASS')
        self.assertEqual(report['generated_resource_counts'], [2, 2])
        self.assertTrue(report['independent_generations_match'])
        self.assertTrue(report['recipe_inputs_unchanged_during_run'])
        helper = self.scripts / 'environment_serialization.py'
        self.assertEqual(report['generator_sha256'][helper.name], hashlib.sha256(helper.read_bytes()).hexdigest())
        self.assertIn(str(self.environment.resolve()), report['recipe_input_sha256'])

    def test_explicit_pinned_203_keeps_historical_report_contract(self):
        self.fixture(203)
        result = self.run_cli('--expected-resource-count', 203)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.report()['id'], 'venue01_default_203_resource_regression_v1')
        self.assertEqual(self.report()['generated_resource_counts'], [203, 203])
        self.assertEqual(self.report()['byte_differences'], [{}, {}])

    def test_byte_mismatch_fails_even_when_count_and_repeats_match(self):
        self.environment.write_text('{"mode":"mismatch"}')
        result = self.run_cli('--expected-resource-count', 2, '--environment-recipe', self.environment)
        self.assertNotEqual(result.returncode, 0)
        report = self.report()
        self.assertEqual(report['status'], 'FAIL')
        self.assertEqual(report['generated_resource_counts'], [2, 2])
        self.assertTrue(report['independent_generations_match'])
        self.assertTrue(all(report['byte_differences']))

    def test_changed_helper_or_recipe_cannot_claim_pass(self):
        for mode, field in [('mutate_helper', 'generator_sources_unchanged_during_run'),
                            ('mutate_recipe', 'recipe_inputs_unchanged_during_run')]:
            with self.subTest(mode=mode):
                self.environment.write_text(json.dumps({'mode': mode}))
                if self.output.exists():
                    shutil.rmtree(self.output)
                result = self.run_cli('--environment-recipe', self.environment)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(self.report()['status'], 'FAIL')
                self.assertFalse(self.report()[field])

    def test_two_generations_that_differ_cannot_claim_pass(self):
        self.environment.write_text('{"mode":"divergent"}')
        result = self.run_cli('--environment-recipe', self.environment)
        self.assertNotEqual(result.returncode, 0)
        report = self.report()
        self.assertEqual(report['status'], 'FAIL')
        self.assertFalse(report['independent_generations_match'])
        self.assertEqual(report['byte_differences'][0], {})
        self.assertTrue(report['byte_differences'][1])

    def test_both_overlap_directions_and_equal_roots_fail_before_writes(self):
        before = {path.relative_to(self.root): path.read_bytes() for path in self.root.rglob('*') if path.is_file()}
        for output in [self.canonical, self.canonical / 'nested', self.root]:
            result = self.run_cli(output=output)
            self.assertEqual(result.returncode, 2)
            self.assertIn('must not overlap', result.stderr)
        self.assertEqual(before, {path.relative_to(self.root): path.read_bytes() for path in self.root.rglob('*') if path.is_file()})

    def test_wrong_expected_count_and_stale_output_fail_before_generation(self):
        result = self.run_cli('--expected-resource-count', 205)
        self.assertEqual(result.returncode, 2)
        self.assertFalse(self.output.exists())
        self.output.mkdir()
        sentinel = self.output / 'stale.json'
        sentinel.write_text('original')
        result = self.run_cli()
        self.assertEqual(result.returncode, 2)
        self.assertEqual(sentinel.read_text(), 'original')
        self.assertEqual(list(self.output.iterdir()), [sentinel])

    def test_missing_helper_rejects_before_output_is_created(self):
        (self.scripts / 'environment_serialization.py').unlink()
        result = self.run_cli()
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(self.output.exists())

if __name__ == '__main__':
    unittest.main(verbosity=2)
