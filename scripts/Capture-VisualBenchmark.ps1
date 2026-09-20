param([string]$OutputDirectory='docs/quality/m12/wp164/captures',[switch]$Matrix)
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$path=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
$cases=if($Matrix){foreach($tier in @('low','balanced','high')){foreach($weather in @('clear','overcast','snowfall')){foreach($shot in @('approach','athlete','targets')){@{id="$tier-$weather-$shot";tier=$tier;weather=$weather;lighting=$(if($weather -eq 'clear'){'clear_cold'}else{'overcast'});shot=$shot;time=8}}}}}else{@(@{id='approach';tier='balanced';weather='clear';lighting='clear_cold';shot='approach';time=8},@{id='athlete';tier='balanced';weather='clear';lighting='clear_cold';shot='athlete';time=8},@{id='dawn';tier='balanced';weather='clear';lighting='dawn';shot='athlete';time=8},@{id='targets';tier='balanced';weather='clear';lighting='late_day';shot='targets';time=8})}
if($Matrix){$cases+=@(@{id='rewind-a';tier='balanced';weather='snowfall';lighting='overcast';shot='rail';time=2},@{id='rewind-away';tier='balanced';weather='snowfall';lighting='overcast';shot='rail';time=6},@{id='rewind-b';tier='balanced';weather='snowfall';lighting='overcast';shot='rail';time=2})}
foreach($case in $cases){
 $body='var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.VisualBenchmarkPresentation>();p.Animate=false;p.ShowControls=false;p.Tier="'+$case.tier+'";p.WeatherPreset="'+$case.weather+'";p.LightingPreset="'+$case.lighting+'";p.Shot="'+$case.shot+'";p.Seek('+$case.time+');'
 ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true}|Out-Null
 Start-Sleep -Seconds 2 # Let pipeline changes and Editor shader compilation settle.
 $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
 $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
 if(!$frame){throw 'Missing Game View image'}
 [IO.File]::WriteAllBytes((Join-Path $path ($case.id+'.png')),[Convert]::FromBase64String($frame.data))
 $meta=@'
var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.VisualBenchmarkPresentation>();var c=p.View;var at=c.transform.position;var r=c.transform.rotation;
var evidence=new{schema_version=1,id="__ID__",unity=UnityEngine.Application.unityVersion,width=c.pixelWidth,height=c.pixelHeight,graphics=UnityEngine.SystemInfo.graphicsDeviceName,api=UnityEngine.SystemInfo.graphicsDeviceType.ToString(),source_tick=p.Athletes.SourceTick,athletes=p.Athletes.Count,visible_trees=p.VisibleTrees,visible_spectators=p.VisibleSpectators,time=p.SampleTime,shot=p.Shot,tier=p.Tier,weather=p.WeatherPreset,lighting=p.LightingPreset,camera=new{x=at.x,y=at.y,z=at.z,qx=r.x,qy=r.y,qz=r.z,qw=r.w,fov=c.fieldOfView,near=c.nearClipPlane,far=c.farClipPlane}};
System.IO.File.WriteAllText("__PATH__/__ID__.json",Newtonsoft.Json.JsonConvert.SerializeObject(evidence,Newtonsoft.Json.Formatting.Indented));
'@
 ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$meta.Replace('__PATH__',$path).Replace('__ID__',$case.id);isMethodBody=$true}|Out-Null
}
$cases|ConvertTo-Json -Depth 5|Set-Content (Join-Path $path 'cases.json')
$inputs=@('Assets/Scenes/VisualBenchmark.unity')+@(Get-ChildItem Assets/Resources/Biathlon,Assets/Scripts/Unity/Presentation,Assets/Shaders -File -Recurse|Where-Object Extension -ne '.meta'|ForEach-Object {[IO.Path]::GetRelativePath($PWD,$_.FullName)})
$manifest=[ordered]@{schema_version=1;id='visual_benchmark_capture_manifest_v1';git_commit=(& git rev-parse HEAD);working_tree_dirty=[bool](& git status --porcelain);capture_count=$cases.Count;inputs=@($inputs|Sort-Object|ForEach-Object {@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});images=@($cases|ForEach-Object {@{id=$_.id;sha256=(Get-FileHash -LiteralPath (Join-Path $path ($_.id+'.png'))).Hash.ToLowerInvariant()}})}
$manifest|ConvertTo-Json -Depth 8|Set-Content (Join-Path $path 'manifest.json')
$cards=($cases|ForEach-Object {'<figure><a href="'+$_.id+'.png"><img loading="lazy" src="'+$_.id+'.png" alt="'+$_.id+'"></a><figcaption>'+ $_.id+' · '+$_.time+'s</figcaption></figure>'})-join "`n"
$html='<!doctype html><meta charset="utf-8"><title>BIATHLON — WP165 review matrix</title><style>body{margin:30px;background:#101a24;color:#e4ebf0;font:16px system-ui}h1{font-size:26px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(400px,1fr));gap:18px}figure{margin:0}img{width:100%}figcaption{padding:8px}p{max-width:1000px}</style><h1>WP165 · Fixed visual review matrix</h1><p>Technical regression evidence. Ten detached pose fixtures, not a live race. Final visual acceptance is OPEN. Click any image for full resolution. Case JSON and manifest record camera, weather, tier, source tick and hashes.</p><main>'+$cards+'</main>'
[IO.File]::WriteAllText((Join-Path $path 'index.html'),$html)
