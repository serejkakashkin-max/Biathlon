"""Collect qualified Sprint draw evidence; never changes source, profiles or state history."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs/quality/m16-2/sprint-draw'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(name, value):
    (OUT/name).write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def report():
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    tree = ET.parse(OUT/'sprint-draw-coreclr.trx')
    output = '\n'.join(x.text or '' for x in tree.findall('.//t:StdOut', ns))
    match = re.search(r'player_slot_counts=([\d,]+)', output)
    assert match, 'Missing fixed-seed draw distribution'
    counts = list(map(int, match[1].split(',')))
    assert len(counts) == 30 and sum(counts) == 6000 and all(130 <= x <= 270 for x in counts)
    write('distribution.json', dict(schema_version=1, id='m16_2_sprint_draw_distribution', draws=6000, entrants=30,
                                   draw_rng_seeds='0..5999, fixed SplitMix64; separate race namespace exercised by composition tests',
                                   counts_by_player_ordinal=counts, all_permutations_valid=True,
                                   minimum=min(counts), maximum=max(counts)))
    expected = {'draw-native.json': 7, 'broadcast-lifecycle-native.json': 2,
                'replay-viewer-native.json': 1, 'replay-metadata-native.json': 4,
                'gameplay-hud-native.json': 1}
    for name, total in expected.items():
        summary = read(OUT/name)['result']['structuredContent']['result']['Summary']
        assert summary['Status'] == 'Passed' and summary['PassedTests'] == total, (name, summary)
    for name, total in (('sprint-draw-coreclr.trx', 8), ('unchanged-start-formats-coreclr.trx', 19)):
        counters = ET.parse(OUT/name).find('.//t:Counters', ns).attrib
        assert int(counters['passed']) == total and int(counters['failed']) == 0, (name, counters)
    player = ROOT/'artifacts/m16-2/sprint-draw/fullscreen-ui'
    receipt = read(player/'review.json')
    assert receipt['success'] and receipt['owner_profiles_unchanged'] and not receipt['errors']
    assert receipt['player_start_ordinal'] == receipt['player_bib'] > 1
    assert receipt['start_wait_skipped'] and receipt['countdown_numbers'] == [3, 2, 1, 0]
    destination = OUT/'fullscreen'
    destination.mkdir(exist_ok=True)
    for name in ('review.json', '08a-drawn-start-wait.png', '08-countdown.png', '09-race.png'):
        shutil.copy2(player/name, destination/name)
    build = ROOT/'Builds/M16_2_Live_Shooting_Review'
    shutil.copy2(build/'build-summary.json', OUT/'build-summary.json')
    files = ['Biathlon.exe', 'Biathlon_Data/Managed/Biathlon.Unity.Presentation.dll',
             'Biathlon_Data/Managed/Biathlon.AI.dll', 'Biathlon_Data/Managed/Biathlon.Career.dll',
             'Biathlon_Data/Managed/Biathlon.Replay.dll', 'START_TEST.cmd']
    write('build-fingerprint.json', dict(schema_version=1, id='m16_2_sprint_draw_build',
                                        sha256={p: hashlib.sha256((build/p).read_bytes()).hexdigest() for p in files}))
    before = read(ROOT/'artifacts/m16-2/authority-before.json')
    changes = {p: {'before': value, 'after': hashlib.sha256((ROOT/p).read_bytes()).hexdigest()}
               for p, value in before.items() if hashlib.sha256((ROOT/p).read_bytes()).hexdigest() != value}
    expected_changes = {'Assets/Resources/Biathlon/Audio/audio_v1.json',
                        'Assets/Scripts/AI/CareerRaceProvider.cs',
                        'Assets/Scripts/Replay/RaceReplayLibrary.cs'}
    assert set(changes) == expected_changes, changes
    write('authority-guard.json', dict(schema_version=1, id='m16_2_sprint_draw_authority',
                                      starting_workspace_files=len(before), status='PASS', changes=changes,
                                      new_file='Assets/Scripts/Career/CareerSprintStartDraw.cs',
                                      simulation_domain_profiles_unchanged=True))
    hud_sources = sorted((ROOT/'artifacts/quality/gameplay-hud').glob('*'), key=lambda p: p.stat().st_mtime)
    hud_destination = OUT/'hud'
    hud_destination.mkdir(exist_ok=True)
    for source in hud_sources[-1].glob('*'):
        if source.is_file():
            shutil.copy2(source, hud_destination/source.name)
    baseline = subprocess.check_output(['git', 'show', 'ec143e91:docs/PROJECT_STATE.md'], cwd=ROOT)
    normalize = lambda x: x.decode('utf-8-sig').replace('\r\n', '\n')
    assert normalize((ROOT/'docs/PROJECT_STATE.md').read_bytes()).startswith(normalize(baseline))
    write('history-guard.json', dict(schema_version=1, id='m16_2_draw_state_history', status='PASS',
                                   baseline_commit='ec143e91', complete_prefix_preserved=True))
    print('PASS:15unique native cases,6000draws, delayed player/bib/skip/3-2-1, final build and complete state prefix.')


if __name__ == '__main__':
    report()
