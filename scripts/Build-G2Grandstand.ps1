param([string]$BlenderPath='artifacts/tools/blender-4.5.9-windows-x64/blender.exe')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
 $blender=(Resolve-Path -LiteralPath $BlenderPath).Path
 New-Item -ItemType Directory -Force artifacts/g2-grandstand | Out-Null
 $process=Start-Process -FilePath $blender -ArgumentList '--background --factory-startup --disable-autoexec --python-exit-code 1 --python scripts/art/Build-G2Grandstand.py' -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput artifacts/g2-grandstand/authoring.log -RedirectStandardError artifacts/g2-grandstand/authoring.err
 if(!$process.WaitForExit(60000)){throw "Stand authoring exceeded60 seconds; inspect process $($process.Id)."}
 if($process.ExitCode -ne 0){throw 'Stand authoring failed; inspect artifacts/g2-grandstand/authoring.err.'}
 & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool assets-refresh -Arguments @{} -TimeoutSec 90 | Out-Null
 & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool script-execute -Arguments @{csharpCode='Biathlon.Unity.G2GrandstandAuthoring.Generate();Biathlon.Unity.VisualBenchmarkAuthoring.Generate();';isMethodBody=$true} -TimeoutSec 60 | Out-Null
 Write-Output 'Selected stand derivatives generated; validate seat fit, de-branding and benchmark.'
} finally {Pop-Location}
