param([string]$OutputDirectory = 'docs/quality/m11/wp150')
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$setup = @'
var preview=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.AthleteAnimationPreview>();
preview.enabled=false;preview.gameObject.SetActive(false);
foreach(var obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
if(obj.GetComponent<UnityEngine.MeshRenderer>()!=null){obj.transform.position=new UnityEngine.Vector3(0,-.05f,-5);obj.transform.localScale=new UnityEngine.Vector3(40,.1f,40);}
string Read(string n)=>UnityEngine.Resources.Load<UnityEngine.TextAsset>("Biathlon/"+System.IO.Path.GetFileNameWithoutExtension(n)).text;
string Fixture(string n)=>UnityEngine.Resources.Load<UnityEngine.TextAsset>("Biathlon/Tracks/"+System.IO.Path.GetFileNameWithoutExtension(n)).text;
var runner=Biathlon.AI.ChampionshipRaceFactory.Create(Read,Fixture,Biathlon.Domain.CompetitionFormat.MassStart,new Biathlon.AI.BalanceEventAudit(5),new Biathlon.AI.AiDecisionCounts(),count:30);
for(int i=0;i<400;i++)runner.Step();
var crowd=new UnityEngine.GameObject("Thirty athlete presentation benchmark").AddComponent<Biathlon.Unity.Presentation.AthleteCrowdValidation>();
crowd.Initialize(Biathlon.Simulation.RacePresentationSnapshot.Capture(runner.Field),UnityEngine.Camera.main);
crowd.Frozen=true;
var near=crowd.Measure(600,false);var far=crowd.Measure(600,true);
System.IO.File.WriteAllText("__OUTPUT__/pose-near.json",UnityEngine.JsonUtility.ToJson(near,true));
System.IO.File.WriteAllText("__OUTPUT__/pose-far.json",UnityEngine.JsonUtility.ToJson(far,true));
var camera=UnityEngine.Camera.main;camera.orthographic=false;camera.fieldOfView=55;
'@
$setup=$setup.Replace('__OUTPUT__',((Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)))
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$setup;isMethodBody=$true} | Out-Null
foreach($mode in @('near','far')) {
    $z=if($mode -eq 'near'){14}else{82}
    $body=@'
var camera=UnityEngine.Camera.main;camera.transform.position=new UnityEngine.Vector3(9,7,__Z__);
camera.transform.LookAt(new UnityEngine.Vector3(0,.8f,-5));
var crowd=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.AthleteCrowdValidation>();
crowd.RenderAt(10);crowd.RenderAt(5);
int visible=0;foreach(var skin in crowd.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>()){
var p=camera.WorldToViewportPoint(skin.transform.parent.position+UnityEngine.Vector3.up);
if(p.z>0&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1)visible++;
}
System.IO.File.WriteAllText("__OUTPUT__/__MODE__-view.txt","fixture=detached Mass Start tick400; fixed grid; animation time5; camera="+camera.transform.position+"; FOV55; athlete centers visible="+visible+"/"+crowd.Count+"; screen="+UnityEngine.Screen.width+"x"+UnityEngine.Screen.height);
'@
    $body=$body.Replace('__Z__',[string]$z).Replace('__OUTPUT__',((Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47))).Replace('__MODE__',$mode)
    ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true} | Out-Null
    $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
    $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
    if(!$frame){throw 'Missing Game View image'}
    [IO.File]::WriteAllBytes((Join-Path (Resolve-Path $OutputDirectory) "$mode.png"),[Convert]::FromBase64String($frame.data))
}
