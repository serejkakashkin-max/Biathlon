param([string]$BlenderPath='artifacts/tools/blender-4.5.9-windows-x64/blender.exe')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $blender=(Resolve-Path -LiteralPath $BlenderPath).Path
    New-Item -ItemType Directory -Force -Path artifacts/g2-fir | Out-Null
    $process=Start-Process -FilePath $blender -ArgumentList '--background --factory-startup --disable-autoexec --python-exit-code 1 --python scripts/art/Build-G2Fir.py' -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput artifacts/g2-fir/authoring.log -RedirectStandardError artifacts/g2-fir/authoring.err
    if(!$process.WaitForExit(60000)){throw "Fir authoring exceeded 60 seconds; inspect process $($process.Id)."}
    if($process.ExitCode -ne 0){throw 'Fir authoring failed; inspect artifacts/g2-fir/authoring.err.'}
    & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool assets-refresh -Arguments @{} -TimeoutSec 60 | Out-Null
    & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool script-execute -Arguments @{csharpCode='Biathlon.Unity.G2FirAuthoring.Generate();';isMethodBody=$true} -TimeoutSec 60 | Out-Null
    Write-Output 'Audited fir derivatives generated. Validate alpha/shadows/LODs and benchmark before acceptance.'
} finally { Pop-Location }
