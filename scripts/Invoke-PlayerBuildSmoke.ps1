param([switch]$SkipBuild, [switch]$VisualReview, [int]$TimeoutSeconds = 180)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    if (!$SkipBuild) {
        $buildMethod = if ($VisualReview) { 'BuildWindowsVisualReview' } else { 'BuildWindows' }
        & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool script-execute -Arguments @{
            csharpCode="Biathlon.Unity.PlayerBuildPipeline.$buildMethod();"; isMethodBody=$true
        } -TimeoutSec 1800 | Out-Null
    }
    $build = Join-Path $root $(if ($VisualReview) { 'Builds/VisualReviewWindows' } else { 'Builds/WindowsDevelopment' })
    $exe = Join-Path $build 'Biathlon.exe'
    $summary = Get-Content -Raw (Join-Path $build 'build-summary.json') | ConvertFrom-Json
    if ($summary.result -ne 'Succeeded' -or !(Test-Path -LiteralPath $exe)) { throw 'No successful Windows build.' }
    $run = Join-Path $root ('artifacts/player-smoke/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Path $run -ErrorAction Stop | Out-Null
    $log = Join-Path $run 'Player.log'
    $process = Start-Process -FilePath $exe -WorkingDirectory $build -WindowStyle Hidden -PassThru -ArgumentList @('-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-biathlonSmoke','-biathlonSmokeOutput',('"'+$run+'"'),'-logFile',('"'+$log+'"'))
    if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -Id $process.Id -ErrorAction SilentlyContinue
        throw "Player smoke timed out; log: $log"
    }
    $process.Refresh()
    $content = Get-Content -Raw -LiteralPath $log
    $rows = @([regex]::Matches($content, 'BIATHLON_SMOKE_SCENE (\{[^\r\n]+\})') | ForEach-Object { $_.Groups[1].Value | ConvertFrom-Json })
    $passed = $process.ExitCode -eq 0 -and $content.Contains('BIATHLON_SMOKE_PASS') -and !$content.Contains('BIATHLON_SMOKE_FAIL') -and
        @($rows | Where-Object { $_.scene -eq 'PlayableSprint' -and $_.athletes -eq 6 -and $_.tick -ge 400 }).Count -eq 1 -and
        @($rows | Where-Object { $_.scene -eq 'PlayableMassStart' -and $_.athletes -eq 30 -and $_.tick -ge 400 }).Count -eq 1
    $qualityChecks=@([regex]::Matches($content, 'BIATHLON_SMOKE_QUALITY (PlayableSprint|PlayableMassStart) (low|balanced|high)') | ForEach-Object {$_.Value} | Select-Object -Unique)
    $passed = $passed -and $qualityChecks.Count -eq 6
    $benchmarkChecks=@([regex]::Matches($content, 'BIATHLON_SMOKE_BENCHMARK (low|balanced|high)') | ForEach-Object {$_.Value} | Select-Object -Unique)
    $passed = $passed -and $benchmarkChecks.Count -eq 3
    $renderChecks=@([regex]::Matches($content,'BIATHLON_SMOKE_RENDER (PlayableSprint|PlayableMassStart|VisualBenchmark) (low|balanced|high) ([1-9][0-9]*)') | ForEach-Object {$_.Value})
    $passed = $passed -and $renderChecks.Count -eq 9
    $reviewStartup = $content.Contains('BIATHLON_SMOKE_STARTUP VisualBenchmark CAMERA_ACTIVE CONTROLS_VISIBLE')
    if ($VisualReview) { $passed = $passed -and $reviewStartup -and $summary.startup_scene -eq 'Assets/Scenes/VisualBenchmark.unity' }
    $files = @(Get-ChildItem -LiteralPath $build -File -Recurse | Where-Object { $_.FullName -notlike '*BackUpThisFolder*' } | Sort-Object FullName | ForEach-Object {
        [ordered]@{ path=[IO.Path]::GetRelativePath($build,$_.FullName); bytes=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    })
    $files | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $run 'build-files.json') -Encoding utf8
    $evidence = [ordered]@{ schema_version=1; id='biathlon_player_smoke_run_v1'; passed=$passed; exit_code=$process.ExitCode;
        git_commit=(& git rev-parse HEAD); working_tree_dirty=[bool](& git status --porcelain);
        exe_sha256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant();
        log_sha256=(Get-FileHash -LiteralPath $log -Algorithm SHA256).Hash.ToLowerInvariant();
        build_manifest_sha256=(Get-FileHash -LiteralPath (Join-Path $run 'build-files.json') -Algorithm SHA256).Hash.ToLowerInvariant();
        build=$summary; visual_review_startup=$reviewStartup; scenes=$rows; quality_checks=$qualityChecks; benchmark_checks=$benchmarkChecks; render_checks=$renderChecks; log=$log }
    $evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'verification.json') -Encoding utf8
    $evidence | ConvertTo-Json -Depth 8
    if (!$passed) { throw "Player smoke failed; log: $log" }
}
finally { Pop-Location }
