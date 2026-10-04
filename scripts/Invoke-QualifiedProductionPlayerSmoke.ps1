param(
    [Parameter(Mandatory=$true)][string]$SprintRecord,
    [Parameter(Mandatory=$true)][string]$SprintReplay,
    [string]$RepositoryRoot='C:\game\Biathlon\Biathlon',
    [int]$TimeoutSeconds=300
)
$ErrorActionPreference='Stop'
if($TimeoutSeconds -lt 1 -or $TimeoutSeconds -gt 1800){throw 'TimeoutSeconds must be 1..1800.'}
$taskRoot=[IO.Path]::GetFullPath($RepositoryRoot)
$sourcePath=[IO.Path]::GetFullPath($SprintRecord)
$replayPath=[IO.Path]::GetFullPath($SprintReplay)
$source=Get-Content -Raw -LiteralPath $sourcePath | ConvertFrom-Json
$replay=Get-Content -Raw -LiteralPath $replayPath | ConvertFrom-Json
if($source.schema_version -ne 1 -or $source.id -ne 'production_venue_stream_record_report_v1' -or
   $source.status -ne 'RACE_COMPLETE_REPLAY_NOT_YET_PROVED' -or $source.stopped_reason -ne 'RACE_COMPLETE' -or
   $source.athletes -ne 40 -or $source.ticks -le 0 -or [string]::IsNullOrEmpty($source.result_json)) {
    throw 'A completed actual production Sprint40 streaming record is required.'
}
if($replay.schema_version -ne 1 -or $replay.id -ne 'production_venue_stream_replay_report_v1' -or
   $replay.status -ne 'PASS_COMPLETE_EXACT_REPLAY' -or !$replay.complete_race -or $replay.athletes -ne 40 -or
   $replay.ticks -ne $source.ticks -or $replay.tape_sha256 -ne $source.tape_sha256 -or
   $replay.context_sha256 -ne $source.context_sha256 -or $replay.result_json -cne $source.result_json) {
    throw 'The same actual Sprint40 must have passed its complete exact replay.'
}
$sprint=$source.result_json | ConvertFrom-Json
$profile=$sprint.competition_profile_json | ConvertFrom-Json
$currentProfile=Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'Assets/Resources/Biathlon/Venues/woodland_basin_v1/sprint_profile.json') | ConvertFrom-Json
if(@($sprint.rows).Count -ne 40 -or $sprint.tick_frequency -ne 20 -or $profile.id -cne $currentProfile.id -or
   @($sprint.rows | Where-Object {$_.status -ne 0}).Count -ne 0) {throw 'Exact completed current Sprint40 roster/clock/profile required.'}
if($null -eq $source.resource_sha256 -or $null -eq $source.resource_sha256.PSObject.Properties['Biathlon/Venues/woodland_basin_v1/sprint_profile']) {
    throw 'Current production source resource binding is missing.'
}
foreach($resource in $source.resource_sha256.PSObject.Properties) {
    if($resource.Name -notmatch '^Biathlon/[A-Za-z0-9_/-]+$' -or $resource.Name.Contains('..')) {throw 'Invalid persisted resource ID.'}
    $path=Join-Path $taskRoot ('Assets/Resources/'+$resource.Name+'.json')
    if(!(Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $resource.Value) {
        throw ('Sprint authority resource differs from current build input: '+$resource.Name)
    }
}
$build=Join-Path $taskRoot 'Builds/WoodlandReviewWindows';$exe=Join-Path $build 'Biathlon.exe'
$summary=Get-Content -Raw -LiteralPath (Join-Path $build 'build-summary.json') | ConvertFrom-Json
if($summary.result -ne 'Succeeded' -or !(Test-Path -LiteralPath $exe)){throw 'No successful existing production review build. This script never builds.'}
$run=Join-Path $taskRoot ('artifacts/production-player-smoke/qualified-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $run | Out-Null
# Preserve the exact actual result file; do not replace it with generated qualification rows.
$boundSource=Join-Path $run 'actual-sprint40-record.json';Copy-Item -LiteralPath $sourcePath -Destination $boundSource
$sourceHash=(Get-FileHash -LiteralPath $boundSource -Algorithm SHA256).Hash.ToLowerInvariant()
$log=Join-Path $run 'Pursuit40-Player.log'
$process=Start-Process -FilePath $exe -WorkingDirectory $build -WindowStyle Hidden -PassThru -ArgumentList @(
    '-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080',
    '-biathlonQualifiedPursuitSmoke','-biathlonSprintRecord',('"'+$boundSource+'"'),
    '-biathlonQualifiedPursuitSmokeOutput',('"'+$run+'"'),'-logFile',('"'+$log+'"'))
if(!$process.WaitForExit($TimeoutSeconds*1000)) {
    Stop-Process -Id $process.Id -ErrorAction SilentlyContinue
    throw "Qualified Pursuit smoke timeout; retained log: $log"
}
$process.Refresh();$content=Get-Content -Raw -LiteralPath $log
$rows=@([regex]::Matches($content,'BIATHLON_QUALIFIED_PURSUIT_SMOKE_SCENE (\{[^\r\n]+\})') | ForEach-Object {$_.Groups[1].Value | ConvertFrom-Json})
$fatal='(?im)(^\s*(?:[\w.]*Exception|AssertionException):|Fatal error|Crash!!!|BIATHLON_QUALIFIED_PURSUIT_SMOKE_FAIL)'
$passed=$process.ExitCode -eq 0 -and $content.Contains('BIATHLON_QUALIFIED_PURSUIT_SMOKE_PASS') -and
    ![regex]::IsMatch($content,$fatal) -and $rows.Count -eq 1
if($rows.Count -eq 1) {
    $row=$rows[0]
    $passed=$passed -and $row.schema_version -eq 1 -and $row.venue -ceq 'woodland_basin_v1' -and
        $row.athletes -eq 40 -and $row.qualified_count -eq 40 -and $row.mats -eq 30 -and $row.banks -eq 30 -and
        ($row.tick-$row.start_tick) -eq 400 -and $row.source_result_id -ceq $sprint.id -and $row.source_sha256 -ceq $sourceHash -and
        $row.terrain_tiles -gt 0 -and $row.forest_groups -gt 0 -and $row.trees -gt 0 -and ![string]::IsNullOrEmpty($row.route_id)
}
foreach($tier in @('high','balanced','low')) {
    $pattern='BIATHLON_QUALIFIED_PURSUIT_SMOKE_RENDER '+$tier+' ([1-9][0-9]*) NORMAL_CAMERA draw_calls=(-1|[1-9][0-9]*)(?:\r?\n|$)'
    $png=Join-Path $run ('pursuit40-'+$tier+'.png')
    $suspended='BIATHLON_QUALIFIED_PURSUIT_SMOKE_RENDER '+$tier+' 0 HIDDEN_WINDOW_SUSPENDED draw_calls=(-1|0)(?:\r?\n|$)'
    $rendered=[regex]::Matches($content,$pattern).Count -eq 1 -and (Test-Path -LiteralPath $png)
    $passed=$passed -and ($rendered -or [regex]::Matches($content,$suspended).Count -eq 1)
}
$evidence=[ordered]@{schema_version=1;id='qualified_pursuit40_player_content_smoke_v1';passed=$passed;
    scope='Actual Sprint40 qualification identities and exact start gaps; initialized current Pursuit40 content; paid400-tick normal-player prefix. Hidden-window rendering may be suspended: zero frames supplies no visual evidence; draw_calls=-1 is unavailable. Native Game View supplies separate review. No full-race/FPS/GPU claim.';
    window_rendering_suspended=$content.Contains('HIDDEN_WINDOW_SUSPENDED');
    exit_code=$process.ExitCode;build=$summary;rows=$rows;source_record=$sourcePath;source_record_sha256=$sourceHash;
    source_replay=$replayPath;source_replay_sha256=(Get-FileHash -LiteralPath $replayPath -Algorithm SHA256).Hash.ToLowerInvariant();
    exe_sha256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant();
    log_sha256=(Get-FileHash -LiteralPath $log -Algorithm SHA256).Hash.ToLowerInvariant();log=$log}
$evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'verification.json') -Encoding utf8
$evidence | ConvertTo-Json -Depth 8
if(!$passed){throw "Qualified Pursuit content smoke failed; retained log: $log"}
