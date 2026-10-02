"""Prove Venue01 resources stay byte-identical for explicit checkpoint inputs.

Writes only fresh artifact roots. Two independent full generations are compared
with the supplied canonical checkpoint and with each other. Historical 203-file
checks require their pinned geometry/format recipes; current catalog checks can
select an optional environment recipe and an explicit expected resource count.
"""
from pathlib import Path
import argparse
import concurrent.futures
import hashlib
import json
import subprocess
import sys

SCRIPT = Path(__file__).resolve().parent
PREFIX = Path('resources/Biathlon/Venues/woodland_basin_v1')
SOURCES = ('generate_venue01.py', 'generate_format_branches.py', 'generate_environment.py',
           'verify_environment.py', 'exact_polyline.py', 'corridor_sampling.py',
           'environment_serialization.py', 'regress_venue01_defaults.py')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def resources(root):
    base = root / PREFIX
    return {str(path.relative_to(base)).replace('\\', '/'): sha(path)
            for path in sorted(base.rglob('*.json'))}


def generate(project, output, geometry_recipe, formats_recipe, environment_recipe=None):
    mass, full = output / 'mass', output / 'full'
    commands = [
        ['generate_venue01.py', '--project-root', str(project), '--output-root', str(mass),
         '--recipe', str(geometry_recipe)],
        ['generate_format_branches.py', '--project-root', str(project),
         '--base-root', str(mass), '--output-root', str(full), '--recipe', str(formats_recipe)],
        ['generate_environment.py', '--project-root', str(project), '--output-root', str(full)],
    ]
    if environment_recipe is not None:
        commands[-1].extend(['--recipe', str(environment_recipe)])
    output.mkdir(parents=True, exist_ok=True)
    for command in commands:
        result = subprocess.run([sys.executable, str(SCRIPT / command[0]), *command[1:]],
                                capture_output=True, text=True, timeout=600)
        (output / (command[0] + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
        if result.returncode:
            raise RuntimeError(command[0] + ' failed; inspect its artifact log.')
        print(output.name + ': ' + command[0] + ' PASS', flush=True)
    return resources(full)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-root', type=Path, default=SCRIPT.parents[2])
    parser.add_argument('--canonical-root', type=Path, required=True)
    parser.add_argument('--output-root', type=Path, required=True)
    parser.add_argument('--geometry-recipe', type=Path, default=SCRIPT / 'recipes/woodland_basin_geometry_recipe_v1.json')
    parser.add_argument('--formats-recipe', type=Path, default=SCRIPT / 'recipes/woodland_basin_formats_recipe_v1.json')
    parser.add_argument('--environment-recipe', type=Path, default=None)
    parser.add_argument('--expected-resource-count', type=int, default=None,
                        help='Optional exact checkpoint count (for example 203 or 205); never substitutes for byte comparison.')
    args = parser.parse_args()
    project, canonical, output = (path.resolve() for path in
                                  (args.project_root, args.canonical_root, args.output_root))
    if canonical.is_relative_to(output) or output.is_relative_to(canonical):
        parser.error('Canonical and output roots must not overlap in either direction.')
    if output.exists() and (not output.is_dir() or any(output.iterdir())):
        parser.error('Use a fresh or empty output root; stale generated resources must not mask omissions.')
    if args.expected_resource_count is not None and args.expected_resource_count < 1:
        parser.error('Expected resource count must be positive.')
    before = resources(canonical)
    if not before:
        parser.error('Canonical root contains no Venue01 resource JSON files.')
    if args.expected_resource_count is not None and len(before) != args.expected_resource_count:
        parser.error('Canonical resource count differs: expected ' + str(args.expected_resource_count) + ', actual ' + str(len(before)))
    recipes = [args.geometry_recipe.resolve(), args.formats_recipe.resolve()]
    environment_recipe = None if args.environment_recipe is None else args.environment_recipe.resolve()
    if environment_recipe is not None:
        recipes.append(environment_recipe)
    if any(path.is_relative_to(output) for path in [*(SCRIPT / name for name in SOURCES), *recipes]):
        parser.error('Output root must not contain any generator source or recipe input.')
    code_before = {name: sha(SCRIPT / name) for name in SOURCES}
    recipe_inputs = {str(path): sha(path) for path in recipes}
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        generated = list(pool.map(lambda name: generate(project, output / name,
                             recipes[0], recipes[1], environment_recipe), ('run-a', 'run-b')))
    differences = [{name: {'canonical': before.get(name), 'generated': result.get(name)}
                    for name in sorted(before.keys() | result.keys())
                    if before.get(name) != result.get(name)} for result in generated]
    canonical_unchanged = resources(canonical) == before
    def current_hash(path):
        return sha(path) if path.is_file() else None
    source_unchanged = code_before == {name: current_hash(SCRIPT / name) for name in SOURCES}
    recipes_unchanged = recipe_inputs == {str(path): current_hash(path) for path in recipes}
    generations_match = generated[0] == generated[1]
    passed = not any(differences) and canonical_unchanged and source_unchanged and recipes_unchanged and generations_match
    report_id = 'venue01_default_203_resource_regression_v1' if len(before) == 203 else 'venue01_resource_regression_v1'
    report = {'schema_version': 1, 'id': report_id,
              'status': 'PASS' if passed else 'FAIL', 'canonical_root': str(canonical),
              'expected_resource_count': args.expected_resource_count,
              'environment_recipe': None if environment_recipe is None else str(environment_recipe),
              'independent_generation_count': 2, 'canonical_resource_count': len(before),
              'independent_generations_match': generations_match,
              'generated_resource_counts': [len(result) for result in generated],
              'canonical_bytes_unchanged': canonical_unchanged,
              'generator_sources_unchanged_during_run': source_unchanged,
              'recipe_inputs_unchanged_during_run': recipes_unchanged,
              'recipe_input_sha256': recipe_inputs,
              'byte_differences': differences, 'generator_sha256': code_before,
              'canonical_resource_sha256': before}
    output.mkdir(parents=True, exist_ok=True)
    (output / 'regression.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({key: value for key, value in report.items()
                      if key not in ('canonical_resource_sha256', 'generator_sha256')}, indent=2))
    if not passed:
        raise SystemExit('Venue01 byte regression failed; inspect regression.json')


if __name__ == '__main__':
    main()
