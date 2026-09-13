param([int[]]$Ticks=@(0,400,2500),[string]$OutputDirectory='artifacts/playtest')
$ErrorActionPreference='Stop'
# Run in Play Mode with a venue loaded and no existing playable host. The harness leaves its
# host frozen for inspection; leaving Play Mode removes all transient harness objects.
$invoke=Join-Path $PSScriptRoot 'Invoke-UnityMcp.ps1'
$outputPath=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
function Call-Diagnostic([string]$method,[object[]]$parameters=@()) {
    $r=& $invoke -Tool reflection-method-call -Arguments @{filter=@{Namespace='Biathlon.Unity';TypeName='PlaytestDiagnostics';MethodName=$method;IsStatic=$true;IsPublic=$true;InputParameters=@($parameters|ForEach-Object {@{typeName=$_.typeName;name=$_.name}})};inputParameters=$parameters}
    $r.structuredContent.result.value
}
Call-Diagnostic 'Begin' | Out-Null
foreach($tick in $Ticks){
    $state=Call-Diagnostic 'AdvanceTo' @(@{typeName='System.Int32';name='targetTick';value=$tick})
    Start-Sleep -Milliseconds 250 # Allow frozen presentation to reach Game View.
    $before=$state|ConvertFrom-Json
    $r=& $invoke -Tool screenshot-game-view -Arguments @{}
    $image=$r.content|Where-Object type -eq 'image'|Select-Object -First 1
    if(!$image){throw 'MCP returned no image'}
    $after=Call-Diagnostic 'CaptureState'
    if($after -ne $state){throw 'State changed during screenshot capture'}
    $stem='tick-{0:D6}' -f $before.tick
    $png=Join-Path $outputPath ($stem+'.png');$json=Join-Path $outputPath ($stem+'.json')
    if((Test-Path $png) -or (Test-Path $json)){throw 'Evidence path already exists; use a fresh output directory'}
    [IO.File]::WriteAllBytes($png,[Convert]::FromBase64String($image.data))
    $before|Add-Member screenshot_sha256 (Get-FileHash -LiteralPath $png -Algorithm SHA256).Hash.ToLowerInvariant()
    $before|Add-Member git_commit ((git rev-parse HEAD).Trim())
    $before|Add-Member working_tree_dirty ([bool](git status --porcelain))
    $before|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $json
    Write-Output $json
}
