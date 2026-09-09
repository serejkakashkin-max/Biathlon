param()
$ErrorActionPreference = 'Stop'
$directory = Join-Path $PSScriptRoot '../artifacts/physiology'
[xml]$baseline = Get-Content -LiteralPath (Join-Path $directory 'wp078-interrupted-unity-results.xml')
[xml]$rerun = Get-Content -LiteralPath (Join-Path $directory 'wp078-cooperative-unity-results.xml')
$original = @($baseline.SelectNodes('//test-case'))
$latest = @($rerun.SelectNodes('//test-case'))
if ($original.Count -ne 441 -or $latest.Count -ne 9) { throw 'Unexpected Unity test inventory.' }
if (@($latest | Where-Object result -ne Passed).Count) { throw 'Cooperative rerun did not pass completely.' }
$replacements = @{
    'Biathlon.Tests.AiRaceScenarioTests.ThirtyAthletesRepeatWholeRacesWithoutDeadlockOscillationOrOverlap("synthetic_flat_v1","expert",123UL)' = 'Biathlon.Tests.AiRaceScenarioTests.ThirtyAthletesFlatExpert'
    'Biathlon.Tests.AiRaceScenarioTests.ThirtyAthletesRepeatWholeRacesWithoutDeadlockOscillationOrOverlap("synthetic_rolling_v1","developing",456UL)' = 'Biathlon.Tests.AiRaceScenarioTests.ThirtyAthletesRollingDeveloping'
    'Biathlon.Tests.AiRaceScenarioTests.ThirtyAthletesRepeatWholeRacesWithoutDeadlockOscillationOrOverlap("synthetic_flat_v1","developing",789UL)' = 'Biathlon.Tests.AiRaceScenarioTests.ThirtyAthletesFlatDeveloping'
}
$verified = foreach ($test in $original) {
    $name = [string]$test.fullname
    if ($replacements.ContainsKey($name)) {
        if ($test.failure.message.InnerText -notlike 'Timeout value of 180000 ms*') { throw 'Unexpected original failure.' }
        $name = $replacements[$name]
    }
    $replacement = @($latest | Where-Object fullname -eq $name)
    $result = if ($replacement.Count -eq 1) { $replacement[0] } else { $test }
    if ($result.result -ne 'Passed') { throw "Unresolved test: $name" }
    [pscustomobject]@{ name = $name; status = 'Passed'; source = if ($replacement.Count) { 'cooperative_rerun' } else { 'full_run' } }
}
$expected = @(
    @('synthetic_flat_v1-expert-123','61758b3207d023b1','ed8473687eecee9a3a1c970c57ee9bdfffc1c638c6cce15ee5301c13bd334c27'),
    @('synthetic_rolling_v1-developing-456','79a899df81436769','767c76cc3192fa04893db68a7cc432a79e64fc521730412961b0300dfcee6a53'),
    @('synthetic_flat_v1-developing-789','161cdb0a5530b411','12b8608ce270d4a6233421e380c28c8da047a3a007c8406cee31ee2611ca2159')
)
foreach ($row in $expected) {
    $audit = Get-Content -LiteralPath (Join-Path $directory "ai-soak/$($row[0])-audit.json") -Raw | ConvertFrom-Json
    $hash = (Get-FileHash -LiteralPath (Join-Path $directory "ai-soak/$($row[0])-result.json") -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($audit.trace_digest_fnv64 -ne $row[1] -or $hash -ne $row[2]) { throw "Cross-runtime mismatch: $($row[0])" }
}
[pscustomobject]@{
    schema_version = 1; id = 'wp078_editmode_verification'; status = 'Passed'; total = $verified.Count
    verification = 'Full-run baseline plus cooperative rerun; not a claimed single green run.'
    sources = @('wp078-interrupted-unity-results.xml','wp078-cooperative-unity-results.xml','wp078-standalone.trx')
    cross_runtime_soak_cases = 3; tests = $verified
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $directory 'wp078-editmode-verification.json')
"Verified $($verified.Count) EditMode tests and three cross-runtime soak fingerprints."
