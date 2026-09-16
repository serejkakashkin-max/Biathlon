param([string]$OutputDirectory='docs/quality/m12/wp153/captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$views=@(
    @{id='overview';position='10,9,-14';target='0,.2f,0'},
    @{id='cold-close';position='-3.3f,1.1f,-5';target='-3.3f,.05f,-2'},
    @{id='wet-close';position='3.3f,1.1f,-5';target='3.3f,.05f,-2'},
    @{id='grazing';position='0,2,-10';target='0,.3f,2'}
)
foreach($v in $views){
    $body='var c=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.SnowMaterialPreview>().View; c.transform.position=new UnityEngine.Vector3('+ $v.position +'); c.transform.LookAt(new UnityEngine.Vector3('+ $v.target +'));'
    ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true} | Out-Null
    Start-Sleep -Seconds 2
    $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
    $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
    if(!$frame){throw 'Missing Game View image'}
    [IO.File]::WriteAllBytes((Join-Path (Resolve-Path $OutputDirectory) ($v.id+'.png')),[Convert]::FromBase64String($frame.data))
}
@{schema_version=1;id='wp153-snow-material-captures';views=$views;scene='SnowMaterialValidation';quality='balanced';source='snow_material_v1';note='Material fixture, not final venue. Original noise, no downloaded textures. No simulation changes.'}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $OutputDirectory 'state.json')
