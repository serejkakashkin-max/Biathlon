param([string]$OutputDirectory='docs/quality/m12/wp163/captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$path=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
$views=@(@{id='sector';shot='sector';time=0},@{id='range';shot='range';time=0},@{id='finish';shot='finish';time=0},@{id='athlete';shot='athlete';time=0},@{id='rail-2';shot='rail';time=2},@{id='rail-6';shot='rail';time=6},@{id='rail-rewind';shot='rail';time=2},@{id='crowd-zoom';shot='crowd_zoom';time=0})
foreach($view in $views) {
    $body='var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.BroadcastBenchmarkPreview>();p.Shot="'+$view.shot+'";p.SampleTime='+$view.time+';'
    ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true}|Out-Null
    Start-Sleep -Seconds 1
    $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
    $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
    if(!$frame){throw 'Missing Game View image'}
    [IO.File]::WriteAllBytes((Join-Path $path ($view.id+'.png')),[Convert]::FromBase64String($frame.data))
}
$views | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $path 'sequence.json')
