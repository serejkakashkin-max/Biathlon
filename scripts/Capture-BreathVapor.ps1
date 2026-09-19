param([string]$OutputDirectory='docs/quality/m12/wp159/captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$outputPath=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
# Requires GreyboxVenue in Play Mode without a running host.
$body=@'
Biathlon.Unity.PlaytestDiagnostics.Begin();
var host=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.PlayableSprintHost>();
var proxy=host.GetComponent<Biathlon.Unity.Presentation.AthleteProxyPresentation>();
proxy.SetVisualBreathEnvironment(new Biathlon.Unity.Presentation.BreathEnvironment(-15,new UnityEngine.Vector2(.2f,0)));
for(int tick=2;tick<=1200;tick+=2)Biathlon.Unity.PlaytestDiagnostics.AdvanceTo(tick);
proxy.enabled=false;
var vapor=host.GetComponent<Biathlon.Unity.Presentation.BreathVaporPresentation>();
vapor.Clear();
var state=host.Feed.Current.Get(host.PlayerIndex);
vapor.Observe(state.AthleteId,proxy.GetRig(state.AthleteId),new Biathlon.Unity.Presentation.AthleteAnimationFrame(state,host.Feed.Current.Tick,host.Feed.Current.TickFrequency,1),0);
var camera=host.GetComponentInChildren<Biathlon.Unity.Presentation.AthleteChaseCamera>().GetComponent<UnityEngine.Camera>();
double frozenTime=host.Feed.Current.Tick/(double)host.Feed.Current.TickFrequency+.25;
// Freeze the real observed pose/load, then inspect the emitted cloud at age .25s.
// The callback removes itself when Play Mode destroys the captured component.
UnityEditor.EditorApplication.CallbackFunction draw=null;
draw=()=>{if(vapor==null){UnityEditor.EditorApplication.update-=draw;return;}vapor.Render(frozenTime,camera);};
UnityEditor.EditorApplication.update+=draw;
System.IO.File.WriteAllText("__OUTPUT__/race.json",Biathlon.Unity.PlaytestDiagnostics.CaptureState());
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__OUTPUT__',$outputPath);isMethodBody=$true}|Out-Null
Start-Sleep -Seconds 1
foreach($view in @('chase','close','disabled')) {
    if($view -eq 'close') {
        $body=@'
var h=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.PlayableSprintHost>();
var proxy=h.GetComponent<Biathlon.Unity.Presentation.AthleteProxyPresentation>();
var head=proxy.GetRig(h.Feed.Current.Get(h.PlayerIndex).AthleteId).Bone(Biathlon.Unity.Presentation.AthleteBone.Head);
var camera=h.GetComponentInChildren<Biathlon.Unity.Presentation.AthleteChaseCamera>().GetComponent<UnityEngine.Camera>();
camera.transform.position=head.position+head.right*.9f+head.forward*.65f+UnityEngine.Vector3.up*.1f;
camera.transform.LookAt(head.position+head.forward*.13f);camera.fieldOfView=45;
'@
        ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true}|Out-Null
    }
    if($view -eq 'disabled') {
        ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode='UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.AthleteProxyPresentation>().SetVisualBreathEnvironment(null);';isMethodBody=$true}|Out-Null
    }
    Start-Sleep -Seconds 1
    $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
    $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
    if(!$frame){throw 'Missing Game View image'}
    [IO.File]::WriteAllBytes((Join-Path $outputPath ($view+'.png')),[Convert]::FromBase64String($frame.data))
    if($view -eq 'close') {
        $body=@'
var h=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.PlayableSprintHost>();var vapor=h.GetComponent<Biathlon.Unity.Presentation.BreathVaporPresentation>();
System.IO.File.WriteAllText("__OUTPUT__/vapor.json","{\"schema_version\":1,\"id\":\"wp159_frozen_observed_pose_vapor\",\"cloud_age_s\":0.25,\"note\":\"Real host pose and load held stationary for isolated VFX inspection\",\"tick\":"+h.Feed.Current.Tick+",\"submitted\":"+vapor.SubmittedPuffs+",\"emitted\":"+vapor.Buffer.TotalEmitted+"}");
if(vapor.SubmittedPuffs==0)throw new System.InvalidOperationException("Expected visible vapor.");
'@
        ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__OUTPUT__',$outputPath);isMethodBody=$true}|Out-Null
    }
}
$body=@'
var a=new UnityEngine.Texture2D(2,2);var b=new UnityEngine.Texture2D(2,2);
a.LoadImage(System.IO.File.ReadAllBytes("__OUTPUT__/close.png"));b.LoadImage(System.IO.File.ReadAllBytes("__OUTPUT__/disabled.png"));
var pixels=a.GetPixels32();var baseline=b.GetPixels32();int changed=0,maxDelta=0;
for(int i=0;i<pixels.Length;i++){int delta=System.Math.Max(System.Math.Abs(pixels[i].r-baseline[i].r),System.Math.Max(System.Math.Abs(pixels[i].g-baseline[i].g),System.Math.Abs(pixels[i].b-baseline[i].b)));if(delta>1)changed++;maxDelta=System.Math.Max(maxDelta,delta);}
System.IO.File.WriteAllText("__OUTPUT__/pixel-comparison.json","{\"schema_version\":1,\"id\":\"wp159_vapor_on_off\",\"changed_pixels\":"+changed+",\"maximum_channel_delta\":"+maxDelta+"}");
UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);
if(changed<10)throw new System.InvalidOperationException("Vapor did not produce visible pixels.");
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__OUTPUT__',$outputPath);isMethodBody=$true}|Out-Null
