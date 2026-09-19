param([string]$OutputDirectory='docs/quality/m12/wp160/captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$views=@(
    @{id='overview';position='40,16,-42';look='-3,2,13';fov=52},
    @{id='range-line';position='-7,1.7,-7';look='1,1.1,50';fov=55},
    @{id='target-detail';position='2.1,1.35,47.8';look='1.3,.95,50';fov=45},
    @{id='target-standing';position='1.3,.85,0';look='1.3,.85,50';fov=1.145877},
    @{id='target-prone';position='1.3,.85,0';look='1.3,.85,50';fov=1.145877;stance='Prone'},
    @{id='target-closed';position='1.3,.85,0';look='1.3,.85,50';fov=1.145877;mask=5},
    @{id='finish';position='-21,1.7,-34';look='-23,2.5,-22';fov=55},
    @{id='lodge';position='-17,2,-1';look='-24,2.4,15';fov=55},
    @{id='stand';position='13,2.4,-1';look='24,2,9';fov=55}
)
foreach($view in $views){
    $body='var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.StadiumArtPreview>();var c=p.View;c.transform.position=new UnityEngine.Vector3('+ $view.position+');c.transform.LookAt(new UnityEngine.Vector3('+$view.look+'));c.fieldOfView='+$view.fov+'f;'
    # Add f suffixes to literal fractional vector components for C# constructors.
    $body=$body -replace '(?<![\w.])(\.\d+|\d+\.\d+)(?![\w.])','$1f'
    $stance=if($view.stance){$view.stance}else{'Standing'}
    $mask=if($view.mask){$view.mask}else{0}
    $body+='var geometry=Biathlon.Domain.TargetGeometryDefinition.FromJson(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Biathlon/target_geometry_v1").text);foreach(var bank in UnityEngine.Object.FindObjectsByType<Biathlon.Unity.Presentation.TargetBankArt>(UnityEngine.FindObjectsSortMode.None))bank.Apply(geometry,Biathlon.Domain.ShootingStance.'+$stance+','+$mask+');'
    ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true}|Out-Null
    Start-Sleep -Seconds 1
    $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
    $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
    if(!$frame){throw 'Missing Game View image'}
    [IO.File]::WriteAllBytes((Join-Path (Resolve-Path $OutputDirectory) ($view.id+'.png')),[Convert]::FromBase64String($frame.data))
}
$views | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutputDirectory 'cameras.json')
