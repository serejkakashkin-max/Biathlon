param([string]$OutputDirectory = 'docs/quality/m11/wp148/contact')
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$setup = @'
var preview=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.AthleteAnimationPreview>();
preview.Initialize();preview.enabled=false;
foreach(var obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
    if(obj.GetComponent<UnityEngine.MeshRenderer>()!=null)obj.SetActive(false);
for(int i=0;i<preview.Count;i++)preview.Rig(i).gameObject.SetActive(i<3);
for(int i=0;i<3;i++){
    float grade=(i-1)*.12f;
    var rig=preview.Rig(i);rig.transform.localPosition=new UnityEngine.Vector3((i-1)*2.2f,0,0);
    var driver=rig.GetComponent<Biathlon.Unity.Presentation.AthleteLocomotionDriver>();
    driver.Sample(rig,new Biathlon.Unity.Presentation.LocomotionInput(8,grade,.9f),10);
    driver.Sample(rig,new Biathlon.Unity.Presentation.LocomotionInput(8,grade,.9f),.4);
    rig.GetComponent<Biathlon.Unity.Presentation.AthleteRiflePresentation>().Sample(rig,0);
    rig.GetComponent<Biathlon.Unity.Presentation.AthleteGroundContact>().Sample(rig,grade,.25,Biathlon.Unity.Presentation.SkiLocomotion.Flat,0);
    var floor=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
    UnityEngine.Object.Destroy(floor.GetComponent<UnityEngine.Collider>());
    floor.transform.rotation=UnityEngine.Quaternion.Euler(-UnityEngine.Mathf.Atan(grade)*UnityEngine.Mathf.Rad2Deg,0,0);
    floor.transform.position=rig.transform.position-floor.transform.up*.03f;
    floor.transform.localScale=new UnityEngine.Vector3(2,.06f,3.4f);
    floor.GetComponent<UnityEngine.Renderer>().sharedMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Generated/VenueMaterials/snow.mat");
}
var camera=UnityEngine.Camera.main;camera.transform.position=new UnityEngine.Vector3(5,3,7);
camera.transform.LookAt(new UnityEngine.Vector3(0,.55f,0));camera.orthographicSize=2.8f;
'@
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{ csharpCode=$setup; isMethodBody=$true } | Out-Null
$cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
$frame=$cap.content | Where-Object type -eq image | Select-Object -First 1
if (!$frame) { throw 'No Game View image returned.' }
[IO.File]::WriteAllBytes((Join-Path (Resolve-Path $OutputDirectory) 'slopes.png'),[Convert]::FromBase64String($frame.data))
@{schema_version=1; id='wp148-contact'; grades=@(-.12,0,.12); time_seconds=.4; pole_cycle=.25; camera=@(5,3,7); look_at=@(0,.55,0); orthographic_size=2.8; fixture='AthleteAnimationValidation'; simulated_race=$false} | ConvertTo-Json | Set-Content (Join-Path $OutputDirectory 'state.json') -Encoding utf8
