param([string]$OutputDirectory='docs/quality/m12/wp164/gameplay')
$ErrorActionPreference='Stop'
# Enter an empty Play Mode scene first. The shooting helper creates a fresh two-athlete host.
& "$PSScriptRoot/Capture-M11Shooting.ps1" -OutputDirectory $OutputDirectory
$path=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
$body=@'
Biathlon.Unity.PlaytestDiagnostics.AdvanceTo(30000);
var host=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.PlayableSprintHost>();
var proxies=host.GetComponent<Biathlon.Unity.Presentation.AthleteProxyPresentation>();proxies.Render(1,3);
var target=proxies.GetProxy(host.Feed.Current.Get(host.PlayerIndex).AthleteId);
var camera=host.GetComponentInChildren<UnityEngine.Camera>();camera.transform.position=target.position+new UnityEngine.Vector3(5,1,-4);camera.transform.LookAt(target.position+UnityEngine.Vector3.up*.2f);
System.IO.File.WriteAllText("__OUTPUT__/finished.json",Biathlon.Unity.PlaytestDiagnostics.CaptureState().Replace("effort800_aim1000x1000_trigger_each_tick_v1","neutral_until_standing_first_closure_then_aim1000_v1"));
System.IO.File.WriteAllText("__OUTPUT__/result.json",host.Runner.Field.Result.ToJson());
'@
& "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool script-execute -Arguments @{csharpCode=$body.Replace('__OUTPUT__',$path);isMethodBody=$true}|Out-Null
Start-Sleep -Seconds 2
$cap=& "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool screenshot-game-view
$f=$cap.content|Where-Object type -eq image|Select-Object -First 1
if(!$f){throw 'Missing Game View image'}
[IO.File]::WriteAllBytes((Join-Path $path 'finished-detail.png'),[Convert]::FromBase64String($f.data))
