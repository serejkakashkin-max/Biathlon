param(
 [ValidateSet('baseline','forest','forest_shadows','surface_shadows','athlete_shadows','crowd_shadows')][string]$Probe='baseline',
 [string]$Case='high-athlete',[switch]$SkipBuild,[string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
 if(!$SkipBuild){& "$PSScriptRoot/Invoke-PlayerBuildSmoke.ps1" | Out-Null}
 $definition=Get-Content -Raw Assets/Resources/Biathlon/benchmark_profile_v1.json | ConvertFrom-Json
 if($Case -notin @($definition.cases.id)){throw 'Unknown profiling case.'}
 if(!$OutputDirectory){$OutputDirectory='artifacts/profile-probes/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')+'-'+$Probe}
 New-Item -ItemType Directory -Path $OutputDirectory -ErrorAction Stop | Out-Null
 $run=(Resolve-Path $OutputDirectory).Path;$build=Join-Path $root 'Builds/WindowsDevelopment';$log=Join-Path $run 'Player.log'
 $playerArgs=@('-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-biathlonProfile','-biathlonProfileCase',$Case,'-biathlonProfileProbe',$Probe,'-biathlonProfileOutput',('"'+$run+'"'),'-logFile',('"'+$log+'"'))
 $process=Start-Process -FilePath (Join-Path $build 'Biathlon.exe') -WorkingDirectory $build -WindowStyle Hidden -PassThru -ArgumentList $playerArgs
 if(!$process.WaitForExit(60000)){Stop-Process -Id $process.Id -ErrorAction SilentlyContinue;throw 'Diagnostic probe exceeded60 seconds.'}
 $process.Refresh();$content=Get-Content -Raw -LiteralPath $log;$raw=Get-Content -Raw (Join-Path $run ($Case+'.json')) | ConvertFrom-Json
 $passed=$process.ExitCode -eq 0 -and $content.Contains('BIATHLON_PROFILE_PASS') -and !$content.Contains('BIATHLON_PROFILE_FAIL') -and $raw.diagnostic_only -and $raw.diagnostic_probe -eq $Probe -and $raw.samples.Count -eq $definition.sample_frames
 $files=@(Get-ChildItem -LiteralPath $build -Recurse -File | Where-Object {$_.FullName -notlike '*BackUpThisFolder*'} | Sort-Object FullName | ForEach-Object {@{path=[IO.Path]::GetRelativePath($build,$_.FullName);bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}})
 $files|ConvertTo-Json -Depth 4|Set-Content (Join-Path $run 'build-files.json')
 $evidence=[ordered]@{schema_version=1;id='visual_profile_probe_v1';diagnostic_only=$true;probe=$Probe;case_id=$Case;passed=$passed;exit_code=$process.ExitCode;git_commit=(& git rev-parse HEAD);build_manifest_sha256=(Get-FileHash (Join-Path $run 'build-files.json')).Hash.ToLowerInvariant();triangle_budget=$definition.triangle_budget;log_sha256=(Get-FileHash $log).Hash.ToLowerInvariant()}
 $evidence | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'execution.json')
 Copy-Item Assets/Resources/Biathlon/benchmark_profile_v1.json (Join-Path $run 'definition.json')
 if(!$passed){throw 'Diagnostic execution failed.'}
 & python "$PSScriptRoot/Analyze-VisualProfileProbe.py" $run
 if($LASTEXITCODE -ne 0){throw 'Diagnostic samples invalid.'}
}
finally {Pop-Location}
