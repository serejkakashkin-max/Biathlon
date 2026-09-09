param([string]$OutputDirectory = 'artifacts/shooting/balance')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$runtime = Join-Path $env:LOCALAPPDATA 'Biathlon/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $runtime)) { throw 'Run the Foundation SDK setup before the lab.' }
& $runtime run --project (Join-Path $root 'dotnet/Biathlon.BalanceLab/Biathlon.BalanceLab.csproj') --configuration Release -- shooting-balance (Join-Path $root 'Assets/Resources/Biathlon') (Join-Path $root $OutputDirectory)
if ($LASTEXITCODE -ne 0) { throw "Shooting balance lab failed with exit code $LASTEXITCODE" }
