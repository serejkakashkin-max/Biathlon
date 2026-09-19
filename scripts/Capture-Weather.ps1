param([string]$OutputDirectory='docs/quality/m12/wp158/captures')
$ErrorActionPreference='Stop';New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
$directory=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
$states=@(@{id='clear';preset='clear';tier='balanced';time=12},@{id='overcast';preset='overcast';tier='balanced';time=12},@{id='snow-low';preset='snowfall';tier='low';time=12},@{id='snow-balanced';preset='snowfall';tier='balanced';time=12},@{id='snow-high';preset='snowfall';tier='high';time=12},@{id='snow-later';preset='snowfall';tier='high';time=14},@{id='snow-rewind';preset='snowfall';tier='high';time=12})
foreach($s in $states){
 $body='var p=UnityEngine.Object.FindAnyObjectByType<Biathlon.Unity.Presentation.WeatherPreview>();p.Animate=false;p.Preset="'+$s.preset+'";p.Tier="'+$s.tier+'";p.FrozenTime='+$s.time+';'
 ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true} -TimeoutSec 30|Out-Null
 Start-Sleep -Seconds 2
 $r=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view -TimeoutSec 30;$im=$r.content|Where-Object type -eq image|Select-Object -First 1
 if(!$im){throw 'No weather screenshot'}
 [IO.File]::WriteAllBytes((Join-Path $directory ($s.id+'.png')),[Convert]::FromBase64String($im.data))
 $body='var p=UnityEngine.Object.FindAnyObjectByType<Biathlon.Unity.Presentation.WeatherPreview>();System.IO.File.WriteAllText("'+$directory+'/'+$s.id+'.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{schema_version=1,id="wp158-weather-state",preset=p.Preset,tier=p.Tier,time=p.FrozenTime,wind=new{x=p.Wind.x,z=p.Wind.y},flakes_submitted=p.Weather.SubmittedFlakes,trees=p.Forest.VisibleInstances,fog_start=UnityEngine.RenderSettings.fogStartDistance,fog_end=UnityEngine.RenderSettings.fogEndDistance,draw_calls=UnityEditor.UnityStats.drawCalls}));'
 ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true} -TimeoutSec 30|Out-Null
}
$body=@'
var p=UnityEngine.Object.FindAnyObjectByType<Biathlon.Unity.Presentation.WeatherPreview>();
var records=new System.Collections.Generic.List<object>();
foreach(var tier in new[]{"low","balanced","high"}){
 for(int i=0;i<30;i++)p.Weather.Render("snowfall",tier,12,p.Wind,0);
 var timer=new System.Diagnostics.Stopwatch();long start=System.GC.GetAllocatedBytesForCurrentThread();timer.Start();
 for(int i=0;i<300;i++)p.Weather.Render("snowfall",tier,12,p.Wind,0);timer.Stop();long bytes=System.GC.GetAllocatedBytesForCurrentThread()-start;
 records.Add(new{tier,iterations=300,cpu_ms=timer.Elapsed.TotalMilliseconds/300,managed_bytes=bytes,flakes_submitted=p.Weather.SubmittedFlakes});
}
System.IO.File.WriteAllText("DIRECTORY/budget.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{schema_version=1,id="wp158-cpu-weather",scope="Warmed CPU mesh/scene update and submission; not GPU frame timing",records}));
var textures=new UnityEngine.Texture2D[3];string[] names={"snow-high","snow-later","snow-rewind"};
try{for(int i=0;i<3;i++){textures[i]=new UnityEngine.Texture2D(2,2);UnityEngine.ImageConversion.LoadImage(textures[i],System.IO.File.ReadAllBytes("DIRECTORY/"+names[i]+".png"));}var a=textures[0].GetPixels32();var b=textures[1].GetPixels32();var c=textures[2].GetPixels32();int changed=0,rewind=0;for(int i=0;i<a.Length;i++){if(!a[i].Equals(b[i]))changed++;if(!a[i].Equals(c[i]))rewind++;}System.IO.File.WriteAllText("DIRECTORY/rewind.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{schema_version=1,id="wp158-rendered-rewind",changed,rewind}));if(changed<100||rewind!=0)throw new System.InvalidOperationException("Weather image rewind failed.");}finally{foreach(var t in textures)if(t!=null)UnityEngine.Object.Destroy(t);}
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('DIRECTORY',$directory);isMethodBody=$true} -TimeoutSec 60|Out-Null
$states|ConvertTo-Json -Depth 4|Set-Content (Join-Path $directory 'states.json')
