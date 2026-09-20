param([string]$OutputDirectory='docs/quality/m12/wp164/captures',[switch]$Matrix)
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$path=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
$cases=if($Matrix){foreach($tier in @('low','balanced','high')){foreach($weather in @('clear','overcast','snowfall')){foreach($shot in @('approach','athlete','targets')){@{id="$tier-$weather-$shot";tier=$tier;weather=$weather;lighting=$(if($weather -eq 'clear'){'clear_cold'}else{'overcast'});shot=$shot;time=8}}}}}else{@(@{id='approach';tier='balanced';weather='clear';lighting='clear_cold';shot='approach';time=8},@{id='athlete';tier='balanced';weather='clear';lighting='clear_cold';shot='athlete';time=8},@{id='dawn';tier='balanced';weather='clear';lighting='dawn';shot='athlete';time=8},@{id='targets';tier='balanced';weather='clear';lighting='late_day';shot='targets';time=8})}
foreach($case in $cases){
 $body='var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.VisualBenchmarkPresentation>();p.Animate=false;p.ShowControls=false;p.Tier="'+$case.tier+'";p.WeatherPreset="'+$case.weather+'";p.LightingPreset="'+$case.lighting+'";p.Shot="'+$case.shot+'";p.Seek('+$case.time+');'
 ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true}|Out-Null
 Start-Sleep -Seconds 1
 $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
 $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
 if(!$frame){throw 'Missing Game View image'}
 [IO.File]::WriteAllBytes((Join-Path $path ($case.id+'.png')),[Convert]::FromBase64String($frame.data))
}
$cases|ConvertTo-Json -Depth 5|Set-Content (Join-Path $path 'cases.json')
