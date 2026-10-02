param([string]$OutputDirectory='docs/quality/m13/venue01-captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
$taskOutput=(Resolve-Path -LiteralPath $OutputDirectory).Path.Replace('\','/')
if($taskOutput.Contains('"')){throw 'Capture directory contains an unsupported quote.'}
$taskCases=@(
    @{id='start-grid';mode='start';distance=0;offset='0,0,0';look='0,0,0';fov=62},
    @{id='stadium-overview';mode='world';distance=0;offset='110,35,-72';look='0,1,8';fov=64},
    @{id='side-entry';mode='world';distance=0;offset='110,5,-32';look='70,.8,-13';fov=58},
    @{id='climb-forest';mode='course';distance=600;offset='13,6,-9';look='0,1,6';fov=58},
    @{id='highland';mode='course';distance=1300;offset='14,7,-10';look='0,1,6';fov=62},
    @{id='snow-edge';mode='course';distance=568;offset='7,1.3,-3';look='0,.3,2';fov=55}
)
foreach($taskCase in $taskCases){
    $taskCode=@'
var h=UnityEngine.Object.FindAnyObjectByType<Biathlon.Unity.Presentation.ProductionVenueHost>();
if(h==null||h.Runner==null||h.Environment==null)throw new System.InvalidOperationException("Initialized production host required.");
h.enabled=false;var c=h.ChaseCamera;var follow=c.GetComponent<Biathlon.Unity.Presentation.AthleteChaseCamera>();if(follow!=null)follow.enabled=false;
h.GetComponent<Biathlon.Unity.Presentation.RenderQualityPresentation>().Apply("high");
h.Proxies.Render(1);UnityEngine.Vector3 position,look;
if("__MODE__"=="start")
{var actor=h.Proxies.GetProxy(h.Runner.Field.GetAthlete(h.PlayerIndex).AthleteId.Value);position=actor.position-actor.forward*9+UnityEngine.Vector3.up*3;look=actor.position+actor.forward*5+UnityEngine.Vector3.up;}
else if("__MODE__"=="course")
{var p=h.Venue.Visual.Get("main").Sample(__DISTANCE__);var center=new UnityEngine.Vector3((float)p.X,(float)p.Y,(float)p.Z);var forward=new UnityEngine.Vector3((float)p.ForwardX,0,(float)p.ForwardZ);var right=new UnityEngine.Vector3(forward.z,0,-forward.x);var offset=new UnityEngine.Vector3(__OFFSET__);var aim=new UnityEngine.Vector3(__LOOK__);position=center+right*offset.x+UnityEngine.Vector3.up*offset.y+forward*offset.z;look=center+right*aim.x+UnityEngine.Vector3.up*aim.y+forward*aim.z;}
else{position=new UnityEngine.Vector3(__OFFSET__);look=new UnityEngine.Vector3(__LOOK__);}
c.transform.SetPositionAndRotation(position,UnityEngine.Quaternion.LookRotation(look-position,UnityEngine.Vector3.up));c.fieldOfView=__FOV__;c.nearClipPlane=.05f;c.farClipPlane=700;
h.Environment.Render(c,"high",8);
var evidence=new{schema_version=1,id="__ID__",kind="frozen_simulation_actual_game_view",capture_tool="screenshot-game-view",venue=h.Venue.Manifest.Id,tick=h.Runner.Field.Tick.Value,athletes=h.Runner.Field.Count,banks=h.Environment.BankCount,trees=h.Environment.TreeCount,visible_trees=h.Environment.VisibleTrees,tiles=h.Environment.TerrainTileCount,ground_joined=h.Course.GroundJoined,maximum_edge_gap_m=h.Course.MaximumEdgeGapMeters,quality="high",unity=UnityEngine.Application.unityVersion,camera=new{x=position.x,y=position.y,z=position.z,look_x=look.x,look_y=look.y,look_z=look.z,fov=c.fieldOfView},width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,performance_qualification=false};
System.IO.File.WriteAllText("__OUTPUT__/__ID__.json",Newtonsoft.Json.JsonConvert.SerializeObject(evidence,Newtonsoft.Json.Formatting.Indented));
'@
    $taskCode=$taskCode.Replace('__MODE__',$taskCase.mode).Replace('__DISTANCE__',[string]$taskCase.distance).Replace('__OFFSET__',$taskCase.offset).Replace('__LOOK__',$taskCase.look).Replace('__FOV__',[string]$taskCase.fov).Replace('__OUTPUT__',$taskOutput).Replace('__ID__',$taskCase.id)
    $taskCode=$taskCode -replace '(?<![\w.])(\.\d+|\d+\.\d+)(?![\w.])','$1f'
    $null=.\scripts\Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$taskCode;isMethodBody=$true} -TimeoutSec 180
    Start-Sleep -Seconds 1
    # Read the regular frame: Camera.Render omits deferred RenderMeshInstanced submissions.
    $taskCapture=.\scripts\Invoke-UnityMcp.ps1 -Tool screenshot-game-view -Arguments @{} -TimeoutSec 180
    $taskImage=$taskCapture.content|Where-Object type -eq image|Select-Object -First 1
    if(!$taskImage){throw 'Missing camera image.'}
    [IO.File]::WriteAllBytes((Join-Path $taskOutput ($taskCase.id+'.png')),[Convert]::FromBase64String($taskImage.data))
    Write-Output ('Captured '+$taskCase.id)
}
$taskCases|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $taskOutput 'cases.json') -Encoding utf8
