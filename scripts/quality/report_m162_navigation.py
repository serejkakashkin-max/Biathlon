"""Read-only qualification of core/history; collects M16.2 navigation evidence."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs/quality/m16-2/history-cursor'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(name, data):
    (OUT/name).write_text(json.dumps(data, indent=2)+'\n', encoding='utf-8')


def authority():
    paths = []
    for name in ('Domain', 'Simulation', 'AI', 'Career', 'Replay'):
        paths.extend((ROOT/'Assets/Scripts'/name).rglob('*.cs'))
    paths.extend((ROOT/'Assets/Resources/Biathlon').rglob('*.json'))
    return {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(paths)}


def report():
    expected = {'history-model-native.json': 6, 'history-hud-native.json': 3,
                'thirty-athlete-table-native.json': 1, 'gameplay-hud-native.json': 1,
                'shooting-input-native.json': None, 'broadcast-lifecycle-native.json': 2,
                'replay-viewer-native.json': 1}
    total = 0
    for name, count in expected.items():
        summary = read(OUT/name)['result']['structuredContent']['result']['Summary']
        assert summary['Status'] == 'Passed' and summary['FailedTests'] == 0, (name, summary)
        assert summary['PassedTests'] > 0 and (count is None or count == summary['PassedTests']), (name, summary)
        total += summary['PassedTests']
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    counters = ET.parse(OUT/'history-coreclr.trx').find('.//t:Counters', ns).attrib
    assert int(counters['passed']) == 7 and int(counters['failed']) == 0
    source = ROOT/'artifacts/m16-2/history-cursor/fullscreen-final'
    receipt = read(source/'review.json')
    assert receipt['success'] and receipt['owner_profiles_unchanged'] and not receipt['errors']
    navigation = receipt['navigation']
    for key in ('gameplay_cursor_hidden', 'tab_cursor_hidden', 'focus_released', 'pause_cursor_visible',
                'resume_cursor_hidden', 'history_pinned', 'latest_restored', 'ui_did_not_advance_clock'):
        assert navigation[key], (key, navigation)
    assert navigation['roster'] == 30 and navigation['archived_rows'] > 0
    assert navigation['archived_ordinal'] < navigation['initial_latest'] < navigation['new_latest']
    assert navigation['restored_latest'] == navigation['new_latest']
    destination = OUT/'fullscreen'
    destination.mkdir(exist_ok=True)
    for name in ('review.json', '08b-pause-menu.png', '10-latest-split-ru.png',
                 '11-history-split-ru.png', '12-history-split-en-large.png'):
        shutil.copy2(source/name, destination/name)
    visual_runs = sorted((ROOT/'artifacts/quality/sport-visual').glob('*'), key=lambda p: p.stat().st_mtime)
    visual_out = OUT/'layout'
    visual_out.mkdir(exist_ok=True)
    for name in ('timing-ru.png', 'timing-en-large.png'):
        shutil.copy2(visual_runs[-1]/name, visual_out/name)
    build = ROOT/'Builds/M16_2_Live_Shooting_Review'
    shutil.copy2(build/'build-summary.json', OUT/'build-summary.json')
    files = ('Biathlon.exe', 'Biathlon_Data/Managed/Biathlon.Unity.Presentation.dll',
             'Biathlon_Data/Managed/Biathlon.Presentation.dll', 'START_TEST.cmd')
    write('build-fingerprint.json', dict(schema_version=1, id='m16_2_navigation_build',
                                        sha256={p: hashlib.sha256((build/p).read_bytes()).hexdigest() for p in files}))
    before = read(OUT/'authority-before.json')
    assert authority() == before, 'Authoritative sources or definitions changed'
    write('authority-guard.json', dict(schema_version=1, id='m16_2_navigation_authority',
                                      status='PASS', files=len(before), all_bytes_unchanged=True))
    baseline = subprocess.check_output(['git', 'show', '686bd931:docs/PROJECT_STATE.md'], cwd=ROOT)
    normalize = lambda b: b.decode('utf-8-sig').replace('\r\n', '\n')
    assert normalize((ROOT/'docs/PROJECT_STATE.md').read_bytes()).startswith(normalize(baseline))
    write('history-guard.json', dict(schema_version=1, id='m16_2_navigation_state',
                                   baseline_commit='686bd931', complete_prefix_preserved=True))
    write('qualification.json', dict(schema_version=1, id='m16_2_navigation_qualification', status='PASS',
                                   native_cases=total, coreclr_cases=7, player_exit=0))
    print(f'PASS: {total} native / 7 CoreCLR, actual cursor/pause/TAB and accepted history, core/history guards.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--baseline', action='store_true')
    args = parser.parse_args()
    if args.baseline:
        write('authority-before.json', authority())
        print('Captured core/data baseline only; no source files changed.')
    else:
        report()
