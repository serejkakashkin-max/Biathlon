[CmdletBinding()]
param([switch]$UpdateFixture)
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$artifactRoot = Join-Path $projectRoot 'artifacts/m15-6/replay-compat'
$baselineCommit = '9867b24bfcaed10dc354267477eb9d665fe50d14'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
$archivePath = Join-Path $artifactRoot 'baseline.zip'
& git -C $projectRoot archive --format=zip "--output=$archivePath" $baselineCommit Assets/Scripts/Domain Assets/Scripts/Simulation Assets/Scripts/AI Assets/Scripts/Career Assets/Scripts/Replay
if ($LASTEXITCODE -ne 0) { throw 'Could not read original Git blobs.' }
Expand-Archive -LiteralPath $archivePath -DestinationPath (Join-Path $artifactRoot 'baseline') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LegacyV25FixtureGenerator.cs') -Destination (Join-Path $artifactRoot 'Program.cs')
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>9.0</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
  <ItemGroup><Compile Include="baseline/Assets/Scripts/Domain/**/*.cs"/><Compile Include="baseline/Assets/Scripts/Simulation/**/*.cs"/><Compile Include="baseline/Assets/Scripts/AI/**/*.cs"/><Compile Include="baseline/Assets/Scripts/Career/**/*.cs"/><Compile Include="baseline/Assets/Scripts/Replay/**/*.cs"/><Compile Include="Program.cs"/></ItemGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $artifactRoot 'BaselineGenerator.csproj') -Encoding utf8NoBOM
$previousCliHome = $env:DOTNET_CLI_HOME
Push-Location -LiteralPath $projectRoot
try {
    $env:DOTNET_CLI_HOME = Join-Path $projectRoot 'artifacts/dotnet-home'
    & dotnet run --project (Join-Path $artifactRoot 'BaselineGenerator.csproj') -c Release --disable-build-servers -p:UseSharedCompilation=false | Set-Content -LiteralPath (Join-Path $artifactRoot 'generator-output.json') -Encoding utf8NoBOM
    if ($LASTEXITCODE -ne 0) { throw 'Original-source fixture generator failed.' }
    if ($UpdateFixture) {
        $fixtureRoot = Join-Path $projectRoot 'Assets/Tests/Fixtures/Replay'
        New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $artifactRoot 'v25-original.brp') -Destination $fixtureRoot
        Copy-Item -LiteralPath (Join-Path $artifactRoot 'v25-original.json') -Destination $fixtureRoot
    }
    Get-FileHash -LiteralPath (Join-Path $artifactRoot 'v25-original.brp'),(Join-Path $artifactRoot 'v25-original.json')
}
finally {
    $env:DOTNET_CLI_HOME = $previousCliHome
    Pop-Location
}
