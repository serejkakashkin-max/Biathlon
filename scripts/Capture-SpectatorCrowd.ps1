param([string]$OutputDirectory='docs/quality/m12/wp161/captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$path=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
foreach($view in @(@{id='low';tier='low';time=12},@{id='balanced';tier='balanced';time=12},@{id='high';tier='high';time=12},@{id='motion';tier='high';time=14},@{id='rewind';tier='high';time=12},@{id='far';tier='high';time=12})) {
    $body='var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.SpectatorCrowdPreview>();p.Animate=false;p.FrozenTime='+$view.time+';p.Scene.Tier="'+$view.tier+'";'
    if($view.id -eq 'far'){$body+='p.Scene.View.transform.position=new UnityEngine.Vector3(-40,6,-5);p.Scene.View.transform.LookAt(new UnityEngine.Vector3(24,2,9));'}
    ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true}|Out-Null
    Start-Sleep -Seconds 1
    $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
    $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
    if(!$frame){throw 'Missing Game View image'}
    [IO.File]::WriteAllBytes((Join-Path $path ($view.id+'.png')),[Convert]::FromBase64String($frame.data))
    $body=@'
var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.SpectatorCrowdPreview>();var c=p.Crowd;
System.IO.File.WriteAllText("__PATH__/__ID__.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{schema_version=1,id="wp161_crowd_view",tier=p.Scene.Tier,time=p.FrozenTime,total=c.Total,visible=c.Visible,near=c.Near,far=c.Far,triangles=c.SubmittedTriangles},Newtonsoft.Json.Formatting.Indented));
'@
    ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__PATH__',$path).Replace('__ID__',$view.id);isMethodBody=$true}|Out-Null
}
$body=@'
int Difference(string a,string b){var x=new UnityEngine.Texture2D(2,2);var y=new UnityEngine.Texture2D(2,2);x.LoadImage(System.IO.File.ReadAllBytes("__PATH__/"+a+".png"));y.LoadImage(System.IO.File.ReadAllBytes("__PATH__/"+b+".png"));var u=x.GetPixels32();var v=y.GetPixels32();int count=0;for(int i=0;i<u.Length;i++)if(!u[i].Equals(v[i]))count++;UnityEngine.Object.Destroy(x);UnityEngine.Object.Destroy(y);return count;}
int motion=Difference("high","motion"),rewind=Difference("high","rewind");
System.IO.File.WriteAllText("__PATH__/pixel-comparison.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{schema_version=1,id="wp161_motion_rewind",motion_changed_pixels=motion,rewind_changed_pixels=rewind}));if(motion==0||rewind!=0)throw new System.InvalidOperationException("Crowd motion/rewind image check failed.");
var preview=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.SpectatorCrowdPreview>();var camera=preview.Scene.View;camera.transform.position=new UnityEngine.Vector3(13,2.4f,-1);camera.transform.LookAt(new UnityEngine.Vector3(24,2,9));var rows=new System.Collections.Generic.List<object>();
foreach(string tier in new[]{"low","balanced","high"}){for(int i=0;i<60;i++)preview.Crowd.Prepare(camera,tier,12);long bytes=System.GC.GetAllocatedBytesForCurrentThread(),start=System.Diagnostics.Stopwatch.GetTimestamp();for(int i=0;i<500;i++)preview.Crowd.Prepare(camera,tier,12);long end=System.Diagnostics.Stopwatch.GetTimestamp();bytes=System.GC.GetAllocatedBytesForCurrentThread()-bytes;rows.Add(new{tier,mean_cpu_ms=(end-start)*1000.0/System.Diagnostics.Stopwatch.Frequency/500,managed_bytes=bytes,visible=preview.Crowd.Visible,triangles=preview.Crowd.SubmittedTriangles});}
System.IO.File.WriteAllText("__PATH__/cpu-prepare.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{schema_version=1,id="wp161_culling_matrix_preparation",iterations=500,note="CPU preparation only; GPU and full-frame timings remain WP166",rows},Newtonsoft.Json.Formatting.Indented));
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__PATH__',$path);isMethodBody=$true}|Out-Null
