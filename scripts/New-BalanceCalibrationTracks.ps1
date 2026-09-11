param([string]$OutputDirectory='tests/fixtures/tracks')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$inputDir=Join-Path $root ('artifacts/balance/track-input-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $inputDir | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'Assets/Resources/Biathlon/track_contract_fixture_v1.json') -Destination (Join-Path $inputDir 'track_contract_fixture_v1.json')
Copy-Item -LiteralPath (Join-Path $root 'tests/fixtures/balance/long-track-recipes-v1.json') -Destination (Join-Path $inputDir 'synthetic_track_recipes_v1.json')
$sdk=Join-Path $env:LOCALAPPDATA 'Biathlon/dotnet/dotnet.exe'
if(-not (Test-Path -LiteralPath $sdk)){$sdk='dotnet'}
& $sdk run --project (Join-Path $root 'dotnet/Biathlon.BalanceLab/Biathlon.BalanceLab.csproj') -c Release --no-restore -- generate-tracks $inputDir $OutputDirectory
if($LASTEXITCODE -ne 0){throw 'Track generation failed.'}
