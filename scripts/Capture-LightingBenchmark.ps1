param([string]$OutputDirectory='docs/quality/m12/wp162/captures')
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$path=(Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)
$views=@(@{id='sector';p='34,12,-36';look='-2,2,10';fov='52'},@{id='athlete';p='-3,1.65f,-12';look='0,1,-8';fov='55'},@{id='targets';p='1.3f,.85f,0';look='1.3f,.85f,50';fov='1.145877f'})
foreach($profile in @('clear_cold','overcast','dawn','late_day')) {
    foreach($view in $views) {
        $body='var p=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.StadiumArtPreview>();var lighting=Biathlon.Unity.Presentation.LightingVisualDefinition.Load().Get("'+$profile+'");p.LightingPreset=lighting.id;p.WeatherPreset=lighting.recommended_weather;p.View.transform.position=new UnityEngine.Vector3('+$view.p+');p.View.transform.LookAt(new UnityEngine.Vector3('+$view.look+'));p.View.fieldOfView='+$view.fov+';'
        ./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$body;isMethodBody=$true}|Out-Null
        Start-Sleep -Seconds 1
        $cap=./scripts/Invoke-UnityMcp.ps1 -Tool screenshot-game-view
        $frame=$cap.content|Where-Object type -eq image|Select-Object -First 1
        if(!$frame){throw 'Missing Game View image'}
        [IO.File]::WriteAllBytes((Join-Path $path ($profile+'-'+$view.id+'.png')),[Convert]::FromBase64String($frame.data))
    }
}
$views | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $path 'cameras.json')
