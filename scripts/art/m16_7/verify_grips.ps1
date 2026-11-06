param(
    [Parameter(Mandatory=$true)][string]$BlenderPath,
    [string]$ReportDirectory='artifacts/m16-7-grip-checks'
)
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$false
$projectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$reportRoot=[IO.Path]::GetFullPath((Join-Path $projectRoot $ReportDirectory))
if(-not $reportRoot.StartsWith($projectRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Reports must stay inside the project.'}
if(-not (Test-Path -LiteralPath $BlenderPath -PathType Leaf)){throw 'Blender executable missing.'}
[IO.Directory]::CreateDirectory($reportRoot) | Out-Null
$assetRoot=Join-Path $projectRoot 'Assets/Biathlon/Characters/Biathlete_16_7'
$anatomy=Join-Path $assetRoot 'Models/Source~/MakeHuman/Natural/athletic_fitted.blend'
$current=Join-Path $assetRoot 'Models/Source~/Biathlete_16_7_Natural.blend'
$rows=[Collections.Generic.List[object]]::new()
function Check-Source([string]$name,[string]$script,[string[]]$arguments,[int]$expectedExit=0){
    $output=Join-Path $reportRoot ($name+'.json');$log=Join-Path $reportRoot ($name+'.log')
    if(Test-Path -LiteralPath $output){Remove-Item -LiteralPath $output -Force}
    & $BlenderPath --background --python-exit-code 2 --python (Join-Path $PSScriptRoot $script) -- @arguments --output $output *> $log
    $actualExit=$LASTEXITCODE
    if($actualExit -ne $expectedExit -or -not (Test-Path -LiteralPath $output)){throw "${name}: expected exit $expectedExit with a fresh receipt, got $actualExit. See $log"}
    $receipt=Get-Content -LiteralPath $output -Raw | ConvertFrom-Json
    if($receipt.pass -ne ($expectedExit -eq 0)){throw "${name}: receipt verdict differs from the expected result."}
    $rows.Add([pscustomobject]@{name=$name;exit_code=$actualExit;expected_exit_code=$expectedExit;qualified=$true;receipt=$output})
}
Push-Location $projectRoot
try{
    $common=@('--asset',$current,'--anatomy',$anatomy)
    Check-Source 'distal-pads' 'verify_distal_grip.py' $common
    Check-Source 'handle-and-straps' 'verify_hand_contacts.py' $common
    Check-Source 'finger-clearance' 'verify_finger_clearance.py' $common
    Check-Source 'thumb-side' 'verify_grip_pose.py' $common
    Check-Source 'surface-parity' 'verify_hand_surface.py' @('--anatomy',$anatomy)
    Check-Source 'anatomy' 'verify_anatomical_proportions.py' @('--body',$anatomy,'--source',(Join-Path $assetRoot 'Models/Source~/MakeHuman'))
    foreach($fixture in @('rejected_claw_grip','rejected_working_grip')){
        Check-Source $fixture 'verify_distal_grip.py' @('--asset',(Join-Path $assetRoot ('Models/Source~/Regression/'+$fixture+'.blend')),'--anatomy',$anatomy) 2
    }
    $summary=[pscustomobject]@{schema_version=1;id='m167_both_grips_source_regression';pass=$true;positive_checks=6;negative_checks=2;art_acceptance=$false;checks=$rows}
    [IO.File]::WriteAllText((Join-Path $reportRoot 'summary.json'),($summary | ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
    Write-Output 'Source regression PASS: 6 positive checks, 2 rejected fixtures correctly fail.'
}finally{Pop-Location}
