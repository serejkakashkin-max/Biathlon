param(
    [string]$PythonPath = 'python',
    [string]$OutputRoot = '',
    [string]$ProjectRoot = '',
    [string]$RecipePath = '',
    [string]$EnvironmentRecipePath = ''
)
$ErrorActionPreference = 'Stop'
$projectDirectory = if ($ProjectRoot) { [System.IO.Path]::GetFullPath($ProjectRoot) } else { [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..')) }
$draftDirectory = if ($OutputRoot) { [System.IO.Path]::GetFullPath($OutputRoot) } else { Join-Path $projectDirectory 'artifacts/wp169i/draft' }
$recipe = if ($RecipePath) { [System.IO.Path]::GetFullPath($RecipePath) } else { Join-Path $PSScriptRoot 'recipes/woodland_basin_geometry_recipe_v1.json' }
$environmentRecipe = if ($EnvironmentRecipePath) { [System.IO.Path]::GetFullPath($EnvironmentRecipePath) } else { Join-Path $PSScriptRoot 'recipes/woodland_basin_environment_recipe_v1.json' }
$buildDirectory = Join-Path $draftDirectory 'dotnet-validator'
$resourcesDirectory = Join-Path $draftDirectory 'resources'
$reportPath = Join-Path $draftDirectory 'static-report.json'

& $PythonPath (Join-Path $PSScriptRoot 'generate_venue01.py') --project-root $projectDirectory --output-root $draftDirectory --recipe $recipe
if ($LASTEXITCODE -ne 0) { throw 'Venue draft generator failed.' }
& $PythonPath (Join-Path $PSScriptRoot 'generate_environment.py') --project-root $projectDirectory --output-root $draftDirectory --recipe $environmentRecipe
if ($LASTEXITCODE -ne 0) { throw 'Venue environment draft generator failed.' }
& $PythonPath (Join-Path $PSScriptRoot 'verify_environment.py') --project-root $projectDirectory --output-root $draftDirectory
if ($LASTEXITCODE -ne 0) { throw 'Serialized venue environment checks failed.' }
dotnet build (Join-Path $projectDirectory 'dotnet/Biathlon.BalanceLab/Biathlon.BalanceLab.csproj') --artifacts-path $buildDirectory -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'BalanceLab draft validator build failed.' }
$validatorPath = Join-Path $buildDirectory 'bin/Biathlon.BalanceLab/debug/Biathlon.BalanceLab.dll'
dotnet $validatorPath venue-validate $resourcesDirectory 'Biathlon/Venues/woodland_basin_v1/manifest' 'Biathlon/Venues/production_venue_validation_v1' $reportPath
if ($LASTEXITCODE -ne 0) { throw 'Draft production static policy failed; inspect static-report.json.' }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if (-not $report.static_checks_pass -or $report.issues.Count -ne 0) { throw 'Expected an explicit zero-diagnostic static PASS.' }
$auditPath = Join-Path $draftDirectory 'audit.json'
$audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
$audit.status = 'STATIC_PRODUCTION_POLICY_PASS_RUNTIME_AND_VISUAL_PENDING'
$audit | Add-Member -MemberType NoteProperty -Name static_report_sha256 -Value (Get-FileHash -LiteralPath $reportPath -Algorithm SHA256).Hash.ToLowerInvariant() -Force
$audit | Add-Member -MemberType NoteProperty -Name validator_dll_sha256 -Value (Get-FileHash -LiteralPath $validatorPath -Algorithm SHA256).Hash.ToLowerInvariant() -Force
$audit | Add-Member -MemberType NoteProperty -Name generator_sha256 -Value (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'generate_venue01.py') -Algorithm SHA256).Hash.ToLowerInvariant() -Force
$audit | Add-Member -MemberType NoteProperty -Name recipe_sha256 -Value (Get-FileHash -LiteralPath $recipe -Algorithm SHA256).Hash.ToLowerInvariant() -Force
[System.IO.File]::WriteAllText($auditPath, ($audit | ConvertTo-Json -Depth 30) + "`n", [System.Text.UTF8Encoding]::new($false))
Write-Output 'Draft Venue01 static production policy PASS. Runtime, playable formats and visual acceptance remain pending.'
