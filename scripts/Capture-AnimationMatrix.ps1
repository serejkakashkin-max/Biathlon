param([string]$OutputDirectory='docs/quality/m11/wp151/matrix')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
foreach($set in @('Locomotion','ProneSequence','StandingSequence','ExertionAndCorners')) {
    $body=@'
var preview=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.AthleteAnimationPreview>();
preview.Frozen=true;preview.StateSet=Biathlon.Unity.Presentation.AthletePreviewSet.__SET__;
preview.RenderAt(.4);
var camera=UnityEngine.Camera.main;camera.orthographic=true;camera.orthographicSize=4.7f;
camera.transform.position=new UnityEngine.Vector3(3,5,14);camera.transform.LookAt(new UnityEngine.Vector3(0,.8f,0));
'@
    ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body.Replace('__SET__',$set);isMethodBody=$true} | Out-Null
    $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
    $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
    if(!$frame){throw 'Missing Game View image'}
    [IO.File]::WriteAllBytes((Join-Path (Resolve-Path $OutputDirectory) "$set.png"),[Convert]::FromBase64String($frame.data))
}
@{schema_version=1;id='wp151-animation-matrix';sample_time=.4;camera=@(3,5,14);look_at=@(0,.8,0);orthographic_size=4.7;rigs_per_set=7;shooting_progress=@(0,.2,.6,1,1,.6,0);bolt_fixture_sight_radians=@(.025,.012);simulated_race=$false;source='AthleteAnimationPreview.StateSet';note='Bolt fixture shows composed sight alignment, not a new bolt/finger animation; finish is a fixture while race entrants remain hidden at finish'} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $OutputDirectory 'state.json') -Encoding utf8
