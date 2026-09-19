param([string]$OutputDirectory='docs/quality/m12/wp156/captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$directory=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
$views=@(
 @{id='chase-low';tier='low';position='0,2,-48';target='0,2,-10';time='12'},
 @{id='chase-balanced';tier='balanced';position='0,2,-48';target='0,2,-10';time='12'},
 @{id='chase-high';tier='high';position='0,2,-48';target='0,2,-10';time='12'},
 @{id='near';tier='balanced';position='1,2,-42';target='8,3,-34';time='12'},
 @{id='wind-later';tier='balanced';position='1,2,-42';target='8,3,-34';time='14'},
 @{id='wind-rewind';tier='balanced';position='1,2,-42';target='8,3,-34';time='12'},
 @{id='far';tier='balanced';position='18,18,-78';target='0,2,-10';time='12'}
)
foreach($v in $views){
 $body='var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.VegetationPreview>();p.Animate=false;p.FrozenTime='+$v.time+';p.Tier="'+$v.tier+'";p.View.transform.position=new UnityEngine.Vector3('+$v.position+');p.View.transform.LookAt(new UnityEngine.Vector3('+$v.target+'));'
 ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true} -TimeoutSec 30 | Out-Null
 Start-Sleep -Seconds 2
 $r=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view -TimeoutSec 30
 $frame=$r.content|Where-Object type -eq image|Select-Object -First 1
 if(!$frame){throw 'No vegetation screenshot'}
 [IO.File]::WriteAllBytes((Join-Path $directory ($v.id+'.png')),[Convert]::FromBase64String($frame.data))
 $body='var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.VegetationPreview>();var f=p.Forest;System.IO.File.WriteAllText("'+$directory+'/'+$v.id+'.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {schema_version=1,id="wp156-visibility",total=f.TotalInstances,visible=f.VisibleInstances,near=f.NearCount,mid=f.MidCount,far=f.FarCount,triangles=f.SubmittedTriangles,draw_calls=UnityEditor.UnityStats.drawCalls,scope="Editor frame counters include passes and editor overhead, not isolated GPU timing"}));'
 ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true} -TimeoutSec 30 | Out-Null
}
$body=@'
var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.VegetationPreview>();
p.View.transform.position=new UnityEngine.Vector3(0,2,-48);p.View.transform.LookAt(new UnityEngine.Vector3(0,2,-10));
var records=new System.Collections.Generic.List<object>();
foreach(var tier in new[]{"low","balanced","high"}){
 for(int i=0;i<50;i++)p.Forest.Prepare(p.View,tier,12);
 var clock=new System.Diagnostics.Stopwatch();long start=System.GC.GetAllocatedBytesForCurrentThread();clock.Start();
 for(int i=0;i<500;i++)p.Forest.Prepare(p.View,tier,12);clock.Stop();long bytes=System.GC.GetAllocatedBytesForCurrentThread()-start;
 records.Add(new {tier,iterations=500,cpu_prepare_ms=clock.Elapsed.TotalMilliseconds/500,managed_bytes=bytes,visible=p.Forest.VisibleInstances,near=p.Forest.NearCount,mid=p.Forest.MidCount,far=p.Forest.FarCount,triangles=p.Forest.SubmittedTriangles});
}
System.IO.File.WriteAllText("DIRECTORY/budget.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {schema_version=1,id="wp156-cpu-prepare",scope="Warmed CPU placement selection, frustum culling, matrix/property preparation only; excludes GPU and draw submission",records}));
var textures=new UnityEngine.Texture2D[3];string[] files={"near","wind-later","wind-rewind"};
try {
 for(int i=0;i<3;i++){textures[i]=new UnityEngine.Texture2D(2,2);UnityEngine.ImageConversion.LoadImage(textures[i],System.IO.File.ReadAllBytes("DIRECTORY/"+files[i]+".png"));}
 var a=textures[0].GetPixels32();var b=textures[1].GetPixels32();var c=textures[2].GetPixels32();int changed=0,rewind=0;
 for(int i=0;i<a.Length;i++){if(!a[i].Equals(b[i]))changed++;if(!a[i].Equals(c[i]))rewind++;}
 System.IO.File.WriteAllText("DIRECTORY/wind.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{schema_version=1,id="wp156-wind-pixels",changed,rewind}));
 if(changed<100||rewind!=0)throw new System.InvalidOperationException("Wind movement/rewind image regression.");
}finally{foreach(var t in textures)if(t!=null)UnityEngine.Object.Destroy(t);}
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('DIRECTORY',$directory);isMethodBody=$true} -TimeoutSec 60 | Out-Null
$views|ConvertTo-Json -Depth 4|Set-Content (Join-Path $directory 'views.json')
