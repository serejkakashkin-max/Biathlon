"""Prove default Venue01 resources stay byte-identical after generic tooling changes.

Writes only fresh artifact roots. Two independent full generations are compared
with the supplied canonical 203-resource checkpoint and with each other.
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


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def resources(root):
    base = root / PREFIX
    return {str(path.relative_to(base)).replace('\\', '/'): sha(path)
            for path in sorted(base.rglob('*.json'))}


def generate(project, output):
    mass, full = output / 'mass', output / 'full'
    commands = [
        ['generate_venue01.py', '--project-root', str(project), '--output-root', str(mass)],
        ['generate_format_branches.py', '--project-root', str(project),
         '--base-root', str(mass), '--output-root', str(full)],
        ['generate_environment.py', '--project-root', str(project), '--output-root', str(full)],
    ]
    output.mkdir(parents=True, exist_ok=True)
    for command in commands:
        result = subprocess.run([sys.executable, str(SCRIPT / command[0]), *command[1:]],
                                check=True, capture_output=True, text=True)
        (output / (command[0] + '.log')).write_text(result.stdout, encoding='utf-8')
        print(output.name + ': ' + command[0] + ' PASS', flush=True)
    return resources(full)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-root', type=Path, default=SCRIPT.parents[2])
    parser.add_argument('--canonical-root', type=Path, required=True)
    parser.add_argument('--output-root', type=Path, required=True)
    args = parser.parse_args()
    project, canonical, output = (path.resolve() for path in
                                  (args.project_root, args.canonical_root, args.output_root))
    assert canonical != output and not canonical.is_relative_to(output)
    before = resources(canonical)
    assert len(before) == 203, ('Canonical resource count', len(before))
    scripts = ['generate_venue01.py', 'generate_format_branches.py', 'generate_environment.py',
               'verify_environment.py', 'exact_polyline.py', 'corridor_sampling.py',
               'regress_venue01_defaults.py']
    code_before = {name: sha(SCRIPT / name) for name in scripts}
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        generated = list(pool.map(lambda name: generate(project, output / name), ('run-a', 'run-b')))
    differences = [{name: {'canonical': before.get(name), 'generated': result.get(name)}
                    for name in sorted(before.keys() | result.keys())
                    if before.get(name) != result.get(name)} for result in generated]
    canonical_unchanged = resources(canonical) == before
    source_unchanged = code_before == {name: sha(SCRIPT / name) for name in scripts}
    passed = not any(differences) and canonical_unchanged and source_unchanged
    report = {'schema_version': 1, 'id': 'venue01_default_203_resource_regression_v1',
              'status': 'PASS' if passed else 'FAIL', 'canonical_root': str(canonical),
              'independent_generation_count': 2, 'canonical_resource_count': len(before),
              'generated_resource_counts': [len(result) for result in generated],
              'canonical_bytes_unchanged': canonical_unchanged,
              'generator_sources_unchanged_during_run': source_unchanged,
              'byte_differences': differences, 'generator_sha256': code_before,
              'canonical_resource_sha256': before}
    output.mkdir(parents=True, exist_ok=True)
    (output / 'regression.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({key: value for key, value in report.items()
                      if key not in ('canonical_resource_sha256', 'generator_sha256')}, indent=2))
    assert passed, 'Default Venue01 byte regression failed; inspect regression.json'


if __name__ == '__main__':
    main()
