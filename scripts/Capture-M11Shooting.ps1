param([string]$OutputDirectory='docs/quality/m11/wp151/shooting')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode='Biathlon.Unity.PlaytestDiagnostics.Begin();';isMethodBody=$true} | Out-Null
foreach($stance in @('Prone','Standing')) {
    $body=@'
var host=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.PlayableSprintHost>();
var id=host.Runner.Field.GetAthlete(host.PlayerIndex).AthleteId;
bool found=false;
for(int i=0;i<20000&&!host.Runner.Field.IsComplete;i++) {
var t=host.Runner.Field.Tick.Next();
host.Step(new Biathlon.Domain.PlayerCommand(t,id,800,0),new Biathlon.Unity.Presentation.ShootingDeviceCommand(new Biathlon.Domain.WeaponAimCommand(t,id,0,0),true,false));
var a=host.Feed.Current.Get(host.PlayerIndex);
if(a.Stance==Biathlon.Domain.ShootingStance.__STANCE__&&a.ShootingPhase==Biathlon.Simulation.ShootingPhase.BoltCycle){found=true;break;}
}
if(!found)throw new System.InvalidOperationException("Requested actual stance was not reached.");
var proxies=host.GetComponent<Biathlon.Unity.Presentation.AthleteProxyPresentation>();proxies.enabled=false;proxies.Render(1);
var chase=host.GetComponentInChildren<Biathlon.Unity.Presentation.AthleteChaseCamera>();chase.enabled=false;chase.Render(1000);
System.IO.File.WriteAllText("__OUTPUT__/__STANCE__.json",Biathlon.Unity.PlaytestDiagnostics.CaptureState().Replace("effort800_aim1000x1000_trigger_each_tick_v1","effort800_neutral_aim_trigger_each_tick_v1"));
'@
    $body=$body.Replace('__STANCE__',$stance).Replace('__OUTPUT__',((Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)))
    ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true} | Out-Null
    Start-Sleep -Seconds 2 # Let newly requested Editor shader variants and the frozen Game View settle.
    $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
    $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
    if(!$frame){throw 'Missing Game View image'}
    [IO.File]::WriteAllBytes((Join-Path (Resolve-Path $OutputDirectory) "$stance-camera.png"),[Convert]::FromBase64String($frame.data))
}
