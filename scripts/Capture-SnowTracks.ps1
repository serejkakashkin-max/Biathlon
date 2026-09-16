param([string]$OutputDirectory='docs/quality/m12/wp154/captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$outputPath=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
# Requires GreyboxVenue in Play Mode, without a running host.
$body=@'
Biathlon.Unity.PlaytestDiagnostics.Begin();
var cold=UnityEngine.Resources.Load<UnityEngine.Material>("Biathlon/Snow/cold");
var snow=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Generated/VenueMaterials/snow.mat");
var trail=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Generated/VenueMaterials/trail.mat");
foreach(var r in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>(UnityEngine.FindObjectsSortMode.None))if(r.sharedMaterial==snow||r.sharedMaterial==trail)r.sharedMaterial=cold;
for(int tick=2;tick<=1200;tick+=2)Biathlon.Unity.PlaytestDiagnostics.AdvanceTo(tick);
var host=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.PlayableSprintHost>();
var marks=host.GetComponent<Biathlon.Unity.Presentation.SnowTrackPresentation>();
if(marks.Buffer.Count==0||marks.VisibleMarks==0)throw new System.InvalidOperationException("No visible real-contact marks.");
System.IO.File.WriteAllText("__OUTPUT__/race.json",Biathlon.Unity.PlaytestDiagnostics.CaptureState());
System.IO.File.WriteAllText("__OUTPUT__/marks.json","{\"schema_version\":1,\"id\":\"wp154_actual_contacts\",\"marks\":"+marks.Buffer.Count+",\"visible\":"+marks.VisibleMarks+",\"overwritten\":"+marks.Buffer.Overwritten+"}");
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__OUTPUT__',$outputPath);isMethodBody=$true} | Out-Null
foreach($view in @('chase','overhead')){
    if($view -eq 'overhead'){
        $body=@'
var host=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.PlayableSprintHost>();
var pivot=host.GetComponent<Biathlon.Unity.Presentation.AthleteProxyPresentation>().GetProxy(host.Feed.Current.Get(host.PlayerIndex).AthleteId);
var camera=host.GetComponentInChildren<Biathlon.Unity.Presentation.AthleteChaseCamera>().GetComponent<UnityEngine.Camera>();
camera.transform.position=pivot.TransformPoint(new UnityEngine.Vector3(-4,6,-8));camera.transform.LookAt(pivot.position-pivot.forward*3);
host.GetComponent<Biathlon.Unity.Presentation.SnowTrackPresentation>().Render(host.Feed.Current.Tick/(double)host.Feed.Current.TickFrequency,camera.transform.position);
'@
        ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true} | Out-Null
    }
    Start-Sleep -Seconds 2
    $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
    $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
    if(!$frame){throw 'Missing Game View image'}
    [IO.File]::WriteAllBytes((Join-Path $outputPath ($view+'.png')),[Convert]::FromBase64String($frame.data))
}
