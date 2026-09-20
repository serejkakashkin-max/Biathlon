param([string]$BlenderPath='artifacts/tools/blender-4.5.9-windows-x64/blender.exe')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $blender=(Resolve-Path -LiteralPath $BlenderPath).Path
    New-Item -ItemType Directory -Force -Path artifacts/g2-snow | Out-Null
    $process=Start-Process -FilePath $blender -ArgumentList '--background --factory-startup --disable-autoexec --python-exit-code 1 --python scripts/art/Build-G2Snow.py' -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput artifacts/g2-snow/authoring.log -RedirectStandardError artifacts/g2-snow/authoring.err
    if(!$process.WaitForExit(60000)){throw "Snow authoring exceeded60 seconds; inspect process $($process.Id)."}
    if($process.ExitCode -ne 0){throw 'Snow authoring failed; inspect artifacts/g2-snow/authoring.err.'}
    & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool assets-refresh -Arguments @{} -TimeoutSec 60 | Out-Null
    & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool script-execute -Arguments @{csharpCode='Biathlon.Unity.G2SnowAuthoring.Generate();';isMethodBody=$true} -TimeoutSec 60 | Out-Null
    Write-Output 'CC0 snow detail and benchmark surface generated. Validate contact invariants and inspect captures.'
} finally { Pop-Location }
