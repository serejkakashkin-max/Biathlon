[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& (Join-Path $PSScriptRoot 'Generate-LegacyV25Fixture.ps1')
$artifactRoot = Join-Path $projectRoot 'artifacts/m15-6/replay-compat'
$nativeSourceRoot = Join-Path $artifactRoot 'native-source'
$baselineSourceRoot = Join-Path $artifactRoot 'baseline/Assets/Scripts'
New-Item -ItemType Directory -Path $nativeSourceRoot -Force | Out-Null
foreach ($sourceFile in (Get-ChildItem -LiteralPath $baselineSourceRoot -Filter '*.cs' -Recurse)) {
    $relativeSource = $sourceFile.FullName.Substring($baselineSourceRoot.Length + 1)
    $destination = Join-Path $nativeSourceRoot $relativeSource
    New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
    # Namespace isolation is the sole source transformation. It prevents this
    # temporary evidence assembly from shadowing current Unity gameplay types.
    $sourceText = [System.IO.File]::ReadAllText($sourceFile.FullName).Replace('Biathlon.', 'OriginalV25.Biathlon.')
    [System.IO.File]::WriteAllText($destination, $sourceText, [System.Text.UTF8Encoding]::new($false))
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NativeLegacyV25FixtureGenerator.cs') -Destination (Join-Path $artifactRoot 'NativeGenerator.cs')
@'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>netstandard2.1</TargetFramework><LangVersion>9.0</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><TreatWarningsAsErrors>true</TreatWarningsAsErrors><AssemblyName>Biathlon.OriginalV25Fixture</AssemblyName></PropertyGroup><ItemGroup><Compile Include="native-source/**/*.cs"/><Compile Include="NativeGenerator.cs"/></ItemGroup></Project>
'@ | Set-Content -LiteralPath (Join-Path $artifactRoot 'NativeGenerator.csproj') -Encoding utf8NoBOM
$previousCliHome = $env:DOTNET_CLI_HOME
try {
    $env:DOTNET_CLI_HOME = Join-Path $projectRoot 'artifacts/dotnet-home'
    & dotnet build (Join-Path $artifactRoot 'NativeGenerator.csproj') -c Release --disable-build-servers -m:1 -p:UseSharedCompilation=false --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Original v25 evidence assembly failed to compile.' }
    Write-Output (Join-Path $artifactRoot 'bin/Release/netstandard2.1/Biathlon.OriginalV25Fixture.dll')
}
finally { $env:DOTNET_CLI_HOME = $previousCliHome }
