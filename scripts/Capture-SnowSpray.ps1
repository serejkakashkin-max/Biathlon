param([string]$OutputDirectory='docs/quality/m12/wp155/captures')
$ErrorActionPreference='Stop'
./scripts/Capture-SnowTracks.ps1 -OutputDirectory $OutputDirectory
$outputPath=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
$body=@'
var h=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.PlayableSprintHost>();
var pivot=h.GetComponent<Biathlon.Unity.Presentation.AthleteProxyPresentation>().GetProxy(h.Feed.Current.Get(h.PlayerIndex).AthleteId);
var camera=h.GetComponentInChildren<Biathlon.Unity.Presentation.AthleteChaseCamera>().GetComponent<UnityEngine.Camera>();
camera.transform.position=pivot.TransformPoint(new UnityEngine.Vector3(-1.5f,.4f,-2));camera.transform.LookAt(pivot.position-UnityEngine.Vector3.up*.65f);
var spray=h.GetComponent<Biathlon.Unity.Presentation.SnowSprayPresentation>();
double time=h.Feed.Current.Tick/(double)h.Feed.Current.TickFrequency+.06;
spray.Render(time,camera);
if(spray.VisibleParticles==0)throw new System.InvalidOperationException("No visible real-contact snow spray.");
System.IO.File.WriteAllText("__OUTPUT__/spray.json",System.FormattableString.Invariant($"{{\"schema_version\":1,\"id\":\"wp155_actual_spray\",\"tick\":{h.Feed.Current.Tick},\"visual_time\":{time},\"active\":{spray.Buffer.ActiveCount},\"visible\":{spray.VisibleParticles},\"emitted\":{spray.Buffer.TotalEmitted},\"overwritten\":{spray.Buffer.Overwritten}}}"));
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__OUTPUT__',$outputPath);isMethodBody=$true}|Out-Null
Start-Sleep -Seconds 2
$cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
$frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
if(!$frame){throw 'Missing Game View image'}
[IO.File]::WriteAllBytes((Join-Path $outputPath 'spray-close.png'),[Convert]::FromBase64String($frame.data))
$body=@'
var material=UnityEngine.Resources.Load<UnityEngine.Material>("Biathlon/Snow/Spray");
foreach(var r in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>(UnityEngine.FindObjectsSortMode.None))if(r.sharedMaterial==material)r.enabled=false;
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true}|Out-Null
Start-Sleep -Seconds 1
$cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
$frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
[IO.File]::WriteAllBytes((Join-Path $outputPath 'spray-disabled.png'),[Convert]::FromBase64String($frame.data))
$body=@'
var a=new UnityEngine.Texture2D(2,2);var b=new UnityEngine.Texture2D(2,2);
a.LoadImage(System.IO.File.ReadAllBytes("__OUTPUT__/spray-close.png"));b.LoadImage(System.IO.File.ReadAllBytes("__OUTPUT__/spray-disabled.png"));
if(a.width!=b.width||a.height!=b.height)throw new System.InvalidOperationException("Comparison size mismatch.");
var pixels=a.GetPixels32();var baseline=b.GetPixels32();int changed=0,maxDelta=0;
for(int i=0;i<pixels.Length;i++){int delta=System.Math.Max(System.Math.Abs(pixels[i].r-baseline[i].r),System.Math.Max(System.Math.Abs(pixels[i].g-baseline[i].g),System.Math.Abs(pixels[i].b-baseline[i].b)));if(delta>1)changed++;maxDelta=System.Math.Max(maxDelta,delta);}
System.IO.File.WriteAllText("__OUTPUT__/pixel-comparison.json","{\"schema_version\":1,\"id\":\"wp155_spray_on_off\",\"changed_pixels\":"+changed+",\"maximum_channel_delta\":"+maxDelta+"}");
UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);
if(changed<10)throw new System.InvalidOperationException("Spray did not produce visible pixels.");
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__OUTPUT__',$outputPath);isMethodBody=$true}|Out-Null
$body=@'
var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.SnowSprayPresentation>();p.Clear();
for(int a=0;a<40;a++)for(int channel=0;channel<4;channel++)p.Buffer.Sample("budget-"+a,channel,new UnityEngine.Vector3(a*.1f,0,channel*.2f),UnityEngine.Vector3.up,UnityEngine.Vector3.forward,0,true,10,0);
var go=new UnityEngine.GameObject("Spray CPU benchmark observer");var camera=go.AddComponent<UnityEngine.Camera>();camera.enabled=false;
for(int i=0;i<60;i++)p.Render(.05,camera);
long allocation=System.GC.GetAllocatedBytesForCurrentThread();long start=System.Diagnostics.Stopwatch.GetTimestamp();
for(int i=0;i<300;i++)p.Render(.05,camera);
long end=System.Diagnostics.Stopwatch.GetTimestamp();allocation=System.GC.GetAllocatedBytesForCurrentThread()-allocation;
double mean=(end-start)*1000.0/System.Diagnostics.Stopwatch.Frequency/300;
System.IO.File.WriteAllText("__OUTPUT__/budget-performance.json",System.FormattableString.Invariant($"{{\"schema_version\":1,\"id\":\"wp155_full_pool_render_budget\",\"active\":{p.Buffer.ActiveCount},\"visible\":{p.VisibleParticles},\"overwritten\":{p.Buffer.Overwritten},\"iterations\":300,\"mean_ms\":{mean},\"managed_bytes\":{allocation},\"note\":\"Synthetic full-pool billboard mesh rebuild; CPU only, no simulation or GPU qualification\"}}"));
UnityEngine.Object.Destroy(go);
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__OUTPUT__',$outputPath);isMethodBody=$true}|Out-Null
