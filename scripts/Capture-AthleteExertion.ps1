param([string]$OutputDirectory = 'docs/quality/m11/wp149/comparison')
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$setup = @'
var preview=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.AthleteAnimationPreview>();
preview.Initialize();preview.enabled=false;
for(int i=0;i<preview.Count;i++)preview.Rig(i).gameObject.SetActive(i<3);
for(int i=0;i<3;i++){
    var rig=preview.Rig(i);rig.transform.localPosition=new UnityEngine.Vector3((i-1)*2,0,0);
    var driver=rig.GetComponent<Biathlon.Unity.Presentation.AthleteLocomotionDriver>();
    var motion=new Biathlon.Unity.Presentation.LocomotionInput(i==2?0:8,0,.9f,travelling:i!=2);
    var exertion=new Biathlon.Unity.Presentation.ExertionInput(i==0?0:1,i==0?1:0,i==0?60:190,finished:i==2);
    driver.Sample(rig,motion,10,exertion);driver.Sample(rig,motion,.4,exertion);
    rig.GetComponent<Biathlon.Unity.Presentation.AthleteRiflePresentation>().Sample(rig,0);
    rig.GetComponent<Biathlon.Unity.Presentation.AthleteGroundContact>().Sample(rig,0,driver.Cycle,driver.State,0);
}
var camera=UnityEngine.Camera.main;camera.transform.position=new UnityEngine.Vector3(4,2.8f,7);
camera.transform.LookAt(new UnityEngine.Vector3(0,.7f,0));camera.orthographicSize=2.5f;
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{ csharpCode=$setup; isMethodBody=$true } | Out-Null
$cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
$frame=$cap.content | Where-Object type -eq image | Select-Object -First 1
if (!$frame) { throw 'No Game View image returned.' }
[IO.File]::WriteAllBytes((Join-Path (Resolve-Path $OutputDirectory) 'exertion.png'),[Convert]::FromBase64String($frame.data))
@{schema_version=1; id='wp149-exertion'; states=@('fresh skiing','tired skiing','finish recovery fixture'); fatigue=@(0,1,1); reserve=@(1,0,0); hr_bpm=@(60,190,190); sample_time=.4; camera=@(4,2.8,7); look_at=@(0,.7,0); orthographic_size=2.5; simulated_race=$false; finish_visibility='Validation fixture; race presentation currently hides finished entrants'} | ConvertTo-Json | Set-Content (Join-Path $OutputDirectory 'state.json') -Encoding utf8
