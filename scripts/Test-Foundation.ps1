param([string]$Dotnet)
$ErrorActionPreference = 'Stop'
if (!$Dotnet) {
    $localSdk = Join-Path $env:LOCALAPPDATA 'Biathlon/dotnet/dotnet.exe'
    $Dotnet = if (Test-Path $localSdk) { $localSdk } else { 'dotnet' }
}
$projectRoot = Split-Path $PSScriptRoot -Parent
& $Dotnet test (Join-Path $projectRoot 'dotnet/Biathlon.Tests/Biathlon.Tests.csproj') --configuration Release --logger 'trx;LogFileName=core-tests.trx' --results-directory (Join-Path $projectRoot 'artifacts/foundation') --blame-hang-timeout 60s
if ($LASTEXITCODE -ne 0) { throw "Core tests failed with exit code $LASTEXITCODE" }
