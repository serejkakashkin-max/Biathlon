param([switch]$SkipBuild,[switch]$VisualReview,[switch]$SingleThreadedDiagnostic,[string]$OutputDirectory,[int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
 if(!$SkipBuild){& "$PSScriptRoot/Invoke-PlayerBuildSmoke.ps1" -VisualReview:$VisualReview | Out-Null}
 if(!$OutputDirectory){$OutputDirectory='artifacts/visual-profile/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')}
 New-Item -ItemType Directory -Path $OutputDirectory -ErrorAction Stop | Out-Null
 $run=(Resolve-Path $OutputDirectory).Path
 $build=Join-Path $root $(if($VisualReview){'Builds/VisualReviewWindows'}else{'Builds/WindowsDevelopment'})
 $log=Join-Path $run 'Player.log'
 $playerArgs=@('-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-biathlonProfile','-biathlonProfileOutput',('"'+$run+'"'),'-logFile',('"'+$log+'"'))
 if($SingleThreadedDiagnostic){$playerArgs+='-force-gfx-direct'}
 $process=Start-Process -FilePath (Join-Path $build 'Biathlon.exe') -WorkingDirectory $build -WindowStyle Hidden -PassThru -ArgumentList $playerArgs
 if(!$process.WaitForExit($TimeoutSeconds*1000)){Stop-Process -Id $process.Id -ErrorAction SilentlyContinue;throw "Profile timed out: $log"}
 $process.Refresh();$content=Get-Content -Raw -LiteralPath $log
 $definition=Get-Content -Raw Assets/Resources/Biathlon/benchmark_profile_v1.json | ConvertFrom-Json
 $caseIds=@([regex]::Matches($content,'BIATHLON_PROFILE_CASE ([^\r\n]+)')|ForEach-Object {$_.Groups[1].Value})
 $passed=$process.ExitCode -eq 0 -and $content.Contains('BIATHLON_PROFILE_PASS') -and !$content.Contains('BIATHLON_PROFILE_FAIL') -and $caseIds.Count -eq $definition.cases.Count
 foreach($case in $definition.cases){if($case.id -notin $caseIds -or !(Test-Path (Join-Path $run ($case.id+'.json')))){$passed=$false}}
 $files=@(Get-ChildItem -LiteralPath $build -Recurse -File | Where-Object {$_.FullName -notlike '*BackUpThisFolder*'} | Sort-Object FullName | ForEach-Object {@{path=[IO.Path]::GetRelativePath($build,$_.FullName);bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}})
 $files|ConvertTo-Json -Depth 4|Set-Content (Join-Path $run 'build-files.json')
 Copy-Item Assets/Resources/Biathlon/benchmark_profile_v1.json (Join-Path $run 'definition.json')
 $evidence=[ordered]@{schema_version=1;id='visual_profile_execution_v1';passed=$passed;exit_code=$process.ExitCode;git_commit=(& git rev-parse HEAD);working_tree_dirty=[bool](& git status --porcelain);case_ids=$caseIds;build=(Get-Content -Raw (Join-Path $build 'build-summary.json')|ConvertFrom-Json);build_manifest_sha256=(Get-FileHash (Join-Path $run 'build-files.json')).Hash.ToLowerInvariant();log_sha256=(Get-FileHash $log).Hash.ToLowerInvariant();output_directory=$run}
 $evidence['single_threaded_diagnostic']=[bool]$SingleThreadedDiagnostic
 $evidence|ConvertTo-Json -Depth 7|Set-Content (Join-Path $run 'execution.json')
 $inputStream=[IO.File]::OpenRead($log);$outputStream=[IO.File]::Create((Join-Path $run 'Player.log.gz'));$zip=[IO.Compression.GZipStream]::new($outputStream,[IO.Compression.CompressionLevel]::Optimal)
 try{$inputStream.CopyTo($zip)}finally{$zip.Dispose();$outputStream.Dispose();$inputStream.Dispose()}
 $evidence|ConvertTo-Json -Depth 7
 if(!$passed){throw "Profile execution failed: $log"}
}
finally {Pop-Location}
