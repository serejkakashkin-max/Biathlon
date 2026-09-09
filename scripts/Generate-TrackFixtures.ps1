param([string]$OutputDirectory = 'tests/fixtures/tracks', [string]$Dotnet)
$ErrorActionPreference = 'Stop'
if (!$Dotnet) { $Dotnet = Join-Path $env:LOCALAPPDATA 'Biathlon/dotnet/dotnet.exe' }
if (!(Test-Path -LiteralPath $Dotnet)) { throw 'Use the Foundation SDK path or pass -Dotnet.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
& $Dotnet run --project (Join-Path $projectRoot 'dotnet/Biathlon.BalanceLab/Biathlon.BalanceLab.csproj') -c Release -- generate-tracks (Join-Path $projectRoot 'Assets/Resources/Biathlon') $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw "Track fixture generation failed: $LASTEXITCODE" }
