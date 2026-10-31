"""Qualify the format-cut snow fix without modifying gameplay or save data."""
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs/quality/m16-2/snow-seams'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(name, data):
    (OUT/name).write_text(json.dumps(data, indent=2)+'\n', encoding='utf-8')


def main():
    expected = {'format-final-native.json': 7, 'bank-contact-final-native.json': 16,
                'sprint-banks-final-native.json': 3, 'rim-final-native.json': 2,
                'coverage-final-native.json': 13, 'captures-final-native.json': 1}
    total = 0
    for name, count in expected.items():
        result = read(OUT/name)['result']['structuredContent']['result']['Summary']
        assert result['Status'] == 'Passed' and result['FailedTests'] == 0, (name, result)
        assert result['PassedTests'] == count, (name, result)
        total += count
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    counters = ET.parse(OUT/'ibu-scoring-coreclr.trx').find('.//t:Counters', ns).attrib
    assert int(counters['passed']) == 1 and int(counters['failed']) == 0
    spec = importlib.util.spec_from_file_location('navigation_report', ROOT/'scripts/quality/report_m162_navigation.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    before = read(OUT/'authority-before.json')
    after = module.authority()
    assert set(after) == set(before)
    changed = [name for name in before if after[name] != before[name]]
    assert changed == [], changed
    write('authority-guard.json', dict(schema_version=1, id='m16_2_snow_authority',
                                      status='PASS', starting_files=len(before), unchanged_files=len(before),
                                      all_core_and_data_bytes_unchanged=True))
    state = read(OUT/'state-before.json')
    current = (ROOT/'docs/PROJECT_STATE.md').read_bytes()
    assert hashlib.sha256(current[:state['bytes']]).hexdigest() == state['sha256']
    baseline = subprocess.check_output(['git', 'show', state['baseline_commit']+':docs/PROJECT_STATE.md'], cwd=ROOT)
    normalize = lambda b: b.decode('utf-8-sig').replace('\r\n', '\n')
    assert normalize(current).startswith(normalize(baseline))
    write('state-guard.json', dict(schema_version=1, id='m16_2_snow_state', status='PASS',
                                 starting_workspace_prefix_byte_exact=True,
                                 committed_history_prefix_preserved=True, baseline_commit=state['baseline_commit']))
    artifacts = ROOT/'artifacts/m16-2/snow-seams'
    for phase in ('before', 'after'):
        destination = OUT/phase
        destination.mkdir(exist_ok=True)
        names = ('sprint-sprint_extension-0.png', 'sprint-sprint_extension-249.png',
                 'pursuit-pursuit_shortcut-0.png', 'pursuit-pursuit_shortcut-367.png')
        # The first baseline covered Sprint's real cuts. Pursuit's actual branch
        # views were added later; never label an unrelated branch view as before.
        for name in names:
            source = artifacts/phase/name
            if source.exists():
                shutil.copy2(source, destination/name)
    for name in ('sprint-edges.json', 'pursuit-edges.json'):
        # Preserve every native diagnostic record without a 100k-line pretty-print
        # diff; these machine-readable arrays are evidence, not handwritten docs.
        (OUT/'after'/name).write_text(json.dumps(read(artifacts/'after'/name),separators=(',',':'))+'\n',encoding='utf-8')
    build = ROOT/'Builds/M16_2_Live_Shooting_Review'
    summary=read(build/'build-summary.json')
    assert summary['result']=='Succeeded' and summary['errors']==0, summary
    shutil.copy2(build/'build-summary.json', OUT/'build-summary.json')
    files = ('Biathlon.exe', 'Biathlon_Data/Managed/Biathlon.Unity.Presentation.dll', 'START_TEST.cmd')
    write('build-fingerprint.json', dict(schema_version=1, id='m16_2_snow_build',
                                         sha256={name: hashlib.sha256((build/name).read_bytes()).hexdigest() for name in files}))
    sources = ('Assets/Scripts/Unity/Presentation/ProductionCoursePresentation.cs',
               'Assets/Scripts/Unity/Presentation/SnowJoinCoverage.cs',
               'Assets/Scripts/Unity/Editor/PlayerBuildM16_2.cs',
               'Assets/Tests/PlayMode/FormatSnowCutGeometryTests.cs',
               'Assets/Tests/PlayMode/FormatSnowSeamReviewTests.cs',
               'Assets/Tests/PlayMode/ProductionTerrainContactTests.cs',
               'Assets/Tests/PlayMode/SnowJoinCoverageTests.cs')
    write('source-fingerprint.json', dict(schema_version=1, id='m16_2_snow_working_sources',
                                        sha256={name: hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in sources}))
    source = artifacts/'fullscreen-final'
    receipt = read(source/'review.json')
    assert receipt['success'] and receipt['owner_profiles_unchanged'] and not receipt['errors']
    assert receipt['fullscreen'] and receipt['fullscreen_mode']=='FullScreenWindow'
    assert (receipt['width'],receipt['height'])==(receipt['display_width'],receipt['display_height'])
    assert receipt['countdown_numbers']==[3,2,1,0]
    assert receipt['race_tick']>=receipt['player_start_tick']>0
    console=read(OUT/'console-final.json')
    assert console['errors']==0 and not console['compiling'], console
    destination = OUT/'fullscreen'
    destination.mkdir(exist_ok=True)
    for name in ('review.json', '08-countdown.png', '09-race.png'):
        shutil.copy2(source/name, destination/name)
    write('qualification.json', dict(schema_version=1, id='m16_2_snow_qualification', status='PASS',
                                     native_cases=total, scoring_coreclr_cases=1,
                                     player_success=True, authority_files=len(before)))
    print(f'PASS: {total} native / 1 CoreCLR; ordinary fullscreen player; protected core/data guard and complete history.')


if __name__ == '__main__':
    main()
