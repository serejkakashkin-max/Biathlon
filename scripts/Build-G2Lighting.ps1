param([string]$BlenderPath='artifacts/tools/blender-4.5.9-windows-x64/blender.exe')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
 New-Item -ItemType Directory -Force artifacts/g2-lighting | Out-Null
 $lightingProcess=Start-Process -FilePath (Resolve-Path -LiteralPath $BlenderPath).Path -ArgumentList '--background --factory-startup --disable-autoexec --python-exit-code 1 --python scripts/art/Build-G2Lighting.py' -WindowStyle Hidden -PassThru -RedirectStandardOutput artifacts/g2-lighting/blender.log -RedirectStandardError artifacts/g2-lighting/blender.err
 if(!$lightingProcess.WaitForExit(60000)){throw 'Lighting derivation timed out.'};if($lightingProcess.ExitCode -ne 0){throw 'Lighting derivation failed.'}
 & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool assets-refresh -Arguments @{} -TimeoutSec 90 | Out-Null
 & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool script-execute -Arguments @{csharpCode='Biathlon.Unity.G2LightingAuthoring.Generate();';isMethodBody=$true} -TimeoutSec 60 | Out-Null
} finally {Pop-Location}
