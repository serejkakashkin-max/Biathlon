param(
    [string]$Dotnet,
    [ValidatePattern('^[1-9]\d*(ms|s|m|h)$')]
    [string]$HangTimeout = '70m'
)
$ErrorActionPreference = 'Stop'
if (!$Dotnet) {
    $localSdk = Join-Path $env:LOCALAPPDATA 'Biathlon/dotnet/dotnet.exe'
    $Dotnet = if (Test-Path $localSdk) { $localSdk } else { 'dotnet' }
}
$projectRoot = Split-Path $PSScriptRoot -Parent
# The complete 16-event season already has a bounded 60-minute test limit.
# Let per-test assertions/limits report first; the outer guard handles a stuck host.
& $Dotnet test (Join-Path $projectRoot 'dotnet/Biathlon.Tests/Biathlon.Tests.csproj') --configuration Release --logger 'trx;LogFileName=core-tests.trx' --results-directory (Join-Path $projectRoot 'artifacts/foundation') --blame-hang-timeout $HangTimeout
if ($LASTEXITCODE -ne 0) { throw "Core tests failed with exit code $LASTEXITCODE" }
