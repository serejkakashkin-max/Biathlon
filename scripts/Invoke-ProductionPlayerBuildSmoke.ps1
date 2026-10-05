param([switch]$SkipBuild,[int]$TimeoutSeconds=300,[string]$RepositoryRoot=(Split-Path $PSScriptRoot -Parent),[string]$BuildDirectory='Builds/WoodlandReviewWindows',[string]$ReviewResource='Biathlon/woodland_review_v1')
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath($RepositoryRoot)
if($BuildDirectory -notmatch '^Builds/[A-Za-z0-9_-]+$' -or $ReviewResource -notmatch '^Biathlon/[A-Za-z0-9_-]+$'){throw 'Explicit bounded build directory and review resource required.'}
Push-Location $taskRoot
try {
    if(!$SkipBuild) {
        $buildCode='Biathlon.Unity.PlayerBuildPipeline.BuildWindowsProductionReview("'+$ReviewResource+'","'+$BuildDirectory+'");'
        & (Join-Path $taskRoot 'scripts/Invoke-UnityMcp.ps1') -Tool script-execute -Arguments @{csharpCode=$buildCode;isMethodBody=$true} -TimeoutSec 1800 | Out-Null
    }
    $build=Join-Path $taskRoot $BuildDirectory;$exe=Join-Path $build 'Biathlon.exe'
    $summary=Get-Content -Raw -LiteralPath (Join-Path $build 'build-summary.json') | ConvertFrom-Json
    $review=Get-Content -Raw -LiteralPath (Join-Path $build 'review-definition.json') | ConvertFrom-Json
    $startup=@($review.entries | Where-Object {$_.id -eq $review.initial_entry_id})
    if($summary.result -ne 'Succeeded' -or !(Test-Path -LiteralPath $exe) -or $startup.Count -ne 1 -or $summary.startup_scene -ne $startup[0].scene_path -or $summary.initial_quality -ne $review.initial_quality_tier) {throw 'No correctly bound successful production review build.'}
    $run=Join-Path $taskRoot ('artifacts/production-player-smoke/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Path $run | Out-Null;$log=Join-Path $run 'Player.log'
    $process=Start-Process -FilePath $exe -WorkingDirectory $build -WindowStyle Hidden -PassThru -ArgumentList @('-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-biathlonProductionSmoke','-biathlonProductionSmokeOutput',('"'+$run+'"'),'-logFile',('"'+$log+'"'))
    if(!$process.WaitForExit($TimeoutSeconds*1000)) {Stop-Process -Id $process.Id -ErrorAction SilentlyContinue;throw "Production content smoke timeout; retained log: $log"}
    $process.Refresh();$content=Get-Content -Raw -LiteralPath $log
    $rows=@([regex]::Matches($content,'BIATHLON_PRODUCTION_SMOKE_SCENE (\{[^\r\n]+\})') | ForEach-Object {$_.Groups[1].Value | ConvertFrom-Json})
    $passed=$process.ExitCode -eq 0 -and $content.Contains('BIATHLON_PRODUCTION_SMOKE_PASS') -and !$content.Contains('BIATHLON_PRODUCTION_SMOKE_FAIL') -and $rows.Count -eq 2
    foreach($entry in $review.entries) {
        $matches=@($rows | Where-Object {$_.entry -eq $entry.id -and $_.scene -eq $entry.scene_path -and $_.athletes -eq $entry.field_count -and ($_.tick-$_.start_tick) -eq 400 -and $_.initial_quality -eq $review.initial_quality_tier -and $_.terrain_tiles -gt 0 -and $_.forest_groups -gt 0 -and $_.trees -gt 0 -and $_.mats -eq 30 -and $_.banks -eq 30 -and $_.visible_loops -gt 0 -and ![string]::IsNullOrEmpty($_.route_id)})
        $passed=$passed -and $matches.Count -eq 1
        foreach($tier in @('high','balanced','low')) {
            $pattern='BIATHLON_PRODUCTION_SMOKE_RENDER '+[regex]::Escape($entry.id)+' '+$tier+' ([1-9][0-9]*) NORMAL_CAMERA draw_calls=(-1|[1-9][0-9]*)(?:\r?\n|$)'
            $suspended='BIATHLON_PRODUCTION_SMOKE_RENDER '+[regex]::Escape($entry.id)+' '+$tier+' 0 HIDDEN_WINDOW_SUSPENDED draw_calls=(-1|0)(?:\r?\n|$)'
            $rendered=[regex]::Matches($content,$pattern).Count -eq 1 -and (Test-Path -LiteralPath (Join-Path $run ($entry.id+'-'+$tier+'.png')))
            $passed=$passed -and ($rendered -or [regex]::Matches($content,$suspended).Count -eq 1)
        }
    }
    $passed=$passed -and [regex]::Matches($content,'BIATHLON_PRODUCTION_SMOKE_TRANSITION [A-Za-z0-9_-]+ [A-Za-z0-9_-]+ PENDING_REJECTED').Count -eq 1
    $manifest=@(Get-ChildItem -LiteralPath $build -Recurse -File | Where-Object {$_.FullName -notlike '*BackUpThisFolder*'} | Sort-Object FullName | ForEach-Object {[ordered]@{path=[IO.Path]::GetRelativePath($build,$_.FullName);bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $run 'build-files.json') -Encoding utf8
    $evidence=[ordered]@{schema_version=1;id='production_player_content_smoke_v1';passed=$passed;venue_id=$review.venue_id;scope='Initialized bound Sprint40/Mass30 content, resource-owned startup quality, paid400-tick controls and real scene transition. Hidden-window rendering may be suspended: zero frames then supplies no visual evidence; draw_calls=-1 is unavailable. Native normal Game View supplies separate visual review. No FPS/GPU or full-race claim.';window_rendering_suspended=$content.Contains('HIDDEN_WINDOW_SUSPENDED');exit_code=$process.ExitCode;build=$summary;review=$review;rows=$rows;exe_sha256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant();log_sha256=(Get-FileHash -LiteralPath $log -Algorithm SHA256).Hash.ToLowerInvariant();build_files_sha256=(Get-FileHash -LiteralPath (Join-Path $run 'build-files.json') -Algorithm SHA256).Hash.ToLowerInvariant();log=$log}
    $evidence | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $run 'verification.json') -Encoding utf8
    $evidence | ConvertTo-Json -Depth 9
    if(!$passed) {throw "Production content smoke failed; retained log: $log"}
}
finally {Pop-Location}
