param([string]$OutputDirectory='docs/quality/m12/wp157/captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
$views=@(
 @{id='overview';position='11,9,-15';target='1,0,4'},
 @{id='packed';position='0,1,-14';target='1,0,-4'},
 @{id='edge';position='7,1,-4';target='5,0,1'},
 @{id='ice';position='-3,1,12';target='1,0,5'}
)
foreach($v in $views){
 $body='var p=UnityEngine.Object.FindAnyObjectByType<Biathlon.Unity.Presentation.SnowMaterialPreview>();p.View.transform.position=new UnityEngine.Vector3('+$v.position+');p.View.transform.LookAt(new UnityEngine.Vector3('+$v.target+'));'
 ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true} -TimeoutSec 30|Out-Null
 Start-Sleep -Seconds 2
 $r=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view -TimeoutSec 30;$im=$r.content|Where-Object type -eq image|Select-Object -First 1
 if(!$im){throw 'Missing terrain capture'}
 [IO.File]::WriteAllBytes((Join-Path (Resolve-Path $OutputDirectory) ($v.id+'.png')),[Convert]::FromBase64String($im.data))
}
@{schema_version=1;id='wp157-terrain-captures';scene='TerrainBlendBenchmark';quality='balanced';views=$views;note='Explicit visual ice patch; no authoritative condition or friction change.'}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $OutputDirectory 'state.json')
