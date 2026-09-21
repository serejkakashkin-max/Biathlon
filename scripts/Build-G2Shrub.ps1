param([string]$BlenderPath='artifacts/tools/blender-4.5.9-windows-x64/blender.exe')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
 $blender=(Resolve-Path -LiteralPath $BlenderPath).Path
 New-Item -ItemType Directory -Force artifacts/g2-shrub | Out-Null
 $process=Start-Process -FilePath $blender -ArgumentList '--background --factory-startup --disable-autoexec --python-exit-code 1 --python scripts/art/Build-G2Shrub.py' -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput artifacts/g2-shrub/authoring.log -RedirectStandardError artifacts/g2-shrub/authoring.err
 if(!$process.WaitForExit(60000)){throw "Shrub authoring exceeded60 seconds; inspect process $($process.Id)."}
 if($process.ExitCode -ne 0){throw 'Shrub authoring failed; inspect artifacts/g2-shrub/authoring.err.'}
 & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool assets-refresh -Arguments @{} -TimeoutSec 90 | Out-Null
 & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool script-execute -Arguments @{csharpCode='Biathlon.Unity.G2FirAuthoring.GenerateSpecies("Assets/Resources/Biathlon/Vegetation/G2Shrub", "shrub", "Shrub",singleAtlas:true);';isMethodBody=$true} -TimeoutSec 60 | Out-Null
 Write-Output 'Selected winter shrub derivatives generated; visual acceptance still required.'
} finally {Pop-Location}
