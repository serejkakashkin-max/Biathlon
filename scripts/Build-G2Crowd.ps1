param([string]$BlenderPath='artifacts/tools/blender-4.5.9-windows-x64/blender.exe')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
 & git check-ignore --quiet Assets/Resources/Biathlon/RestrictedG2Crowd/Face0.png
 if($LASTEXITCODE -ne 0){throw 'Restricted crowd output must be ignored before import.'}
 $tracked=@(& git ls-files Assets/Resources/Biathlon/RestrictedG2Crowd)
 if($tracked.Count -ne 0){throw 'Restricted crowd derivatives are tracked; stop publication and review.'}
 $p=Start-Process -FilePath (Resolve-Path -LiteralPath $BlenderPath).Path -ArgumentList '--background --factory-startup --disable-autoexec --python-exit-code 1 --python scripts/art/Build-G2Crowd.py' -WindowStyle Hidden -PassThru -RedirectStandardOutput artifacts/g2-audit/crowd-build.log -RedirectStandardError artifacts/g2-audit/crowd-build.err
 if(!$p.WaitForExit(60000)){throw 'Crowd derivation timed out.'};if($p.ExitCode -ne 0){throw 'Crowd derivation failed.'}
 & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool assets-refresh -Arguments @{} -TimeoutSec 90 | Out-Null
 & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool script-execute -Arguments @{csharpCode='Biathlon.Unity.G2CrowdAuthoring.Generate();';isMethodBody=$true} -TimeoutSec 60 | Out-Null
} finally {Pop-Location}
