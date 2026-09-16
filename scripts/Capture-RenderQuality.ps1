param([string]$OutputDirectory='docs/quality/m12/wp152/captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$outputPath=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
# Requires Play Mode in GreyboxVenue, without a running race host.
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode='Biathlon.Unity.PlaytestDiagnostics.Begin(); Biathlon.Unity.PlaytestDiagnostics.AdvanceTo(1200);';isMethodBody=$true} | Out-Null
foreach($tier in @('low','balanced','high')) {
    $body=@'
var host=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.PlayableSprintHost>();
var before=Biathlon.Unity.PlaytestDiagnostics.CaptureState();
var quality=host.GetComponent<Biathlon.Unity.Presentation.RenderQualityPresentation>();quality.Apply("__TIER__");
if(before!=Biathlon.Unity.PlaytestDiagnostics.CaptureState())throw new System.InvalidOperationException("Render switch mutated race state.");
System.IO.File.WriteAllText("__OUTPUT__/__TIER__-race.json",before);
var a=quality.Pipeline;
System.IO.File.WriteAllText("__OUTPUT__/__TIER__-pipeline.json",UnityEngine.JsonUtility.ToJson(a,true));
'@
    ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__TIER__',$tier).Replace('__OUTPUT__',$outputPath);isMethodBody=$true} | Out-Null
    # Let the new pipeline render; first-use Editor shader compilation may need longer.
    Start-Sleep -Seconds 2
    $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
    $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
    if(!$frame){throw 'Missing Game View image'}
    [IO.File]::WriteAllBytes((Join-Path $outputPath "$tier.png"),[Convert]::FromBase64String($frame.data))
}
$hashes=@(Get-ChildItem $outputPath '*-race.json' | Get-FileHash | Select-Object -ExpandProperty Hash -Unique)
if($hashes.Count -ne 1){throw 'Race snapshots changed between quality tiers'}
@{schema_version=1;id='wp152-quality-comparison';tick=1200;tiers=@('low','balanced','high');race_sha256=$hashes[0];note='Frozen actual Sprint; no authoritative advancement between profiles. Performance qualification remains WP166.'} | ConvertTo-Json | Set-Content (Join-Path $outputPath 'comparison.json')
