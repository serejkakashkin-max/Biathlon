param([string]$BlenderPath = 'artifacts/tools/blender-4.5.9-windows-x64/blender.exe', [string]$ImagePythonPath = "$env:USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe")
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $blender = (Resolve-Path -LiteralPath $BlenderPath).Path
    New-Item -ItemType Directory -Force -Path artifacts/g2-hero | Out-Null
    $export = @'
var rig=Biathlon.Unity.Presentation.OriginalAthleteRigBuilder.CreateDevelopment(null);
try {
    var entries=new System.Collections.Generic.List<string>();
    for(int i=0;i<(int)Biathlon.Unity.Presentation.AthleteBone.Count;i++) {
        var p=rig.transform.InverseTransformPoint(rig.Bone((Biathlon.Unity.Presentation.AthleteBone)i).position);
        entries.Add("{\"name\":\""+((Biathlon.Unity.Presentation.AthleteBone)i)+"\",\"position\":["+p.x.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+p.y.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+p.z.ToString(System.Globalization.CultureInfo.InvariantCulture)+"]}");
    }
    System.IO.File.WriteAllText("artifacts/g2-hero/m11-bones.json","["+string.Join(",",entries)+"]");
} finally { UnityEngine.Object.DestroyImmediate(rig.gameObject); }
'@
    & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool script-execute -Arguments @{csharpCode=$export;isMethodBody=$true} -TimeoutSec 60 | Out-Null
    $jobs = @(
        @{name='body'; arguments='--background --factory-startup --disable-autoexec --python-exit-code 1 --python scripts/art/Build-G2Hero.py -- --source external_downloads/g2_visual_remediation/01_characters/hero_base/blender_human_base_v1.4.1/extracted/human-base-meshes-bundle-v1.4.1/human_base_meshes_bundle.blend --bones artifacts/g2-hero/m11-bones.json --goggles external_downloads/g2_visual_remediation/03_equipment/goggles/blurryrender/extracted/source/cyberpunk_Ski_Goggles.blend --output artifacts/g2-hero'},
        @{name='materials'; arguments='--background --factory-startup --disable-autoexec --python-exit-code 1 --python scripts/art/Build-G2HeroMaterials.py'}
    )
    foreach ($job in $jobs) {
        $process = Start-Process -FilePath $blender -ArgumentList $job.arguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput "artifacts/g2-hero/$($job.name).log" -RedirectStandardError "artifacts/g2-hero/$($job.name).err"
        if (!$process.WaitForExit(60000)) { throw "Blender $($job.name) exceeded 60 seconds; inspect process $($process.Id) before retrying." }
        if ($process.ExitCode -ne 0) { throw "Blender $($job.name) failed: inspect artifacts/g2-hero/$($job.name).err" }
    }
    & $ImagePythonPath scripts/art/Build-G2BibAtlas.py
    if ($LASTEXITCODE -ne 0) { throw 'Bib atlas rasterization failed.' }
    & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool assets-refresh -Arguments @{} -TimeoutSec 60 | Out-Null
    & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool script-execute -Arguments @{csharpCode='Biathlon.Unity.G2HeroAuthoring.Generate();';isMethodBody=$true} -TimeoutSec 60 | Out-Null
    Write-Output 'Hero derivatives generated. Run G2HeroTests and inspect captures before accepting or publishing.'
} finally { Pop-Location }
