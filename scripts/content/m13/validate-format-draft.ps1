param(
    [string]$PythonPath = 'python',
    [string]$ProjectRoot = '',
    [string]$BaseRoot = '',
    [string]$OutputRoot = '',
    [string]$RecipePath = '',
    [string]$EnvironmentRecipePath = ''
)
$ErrorActionPreference = 'Stop'
$projectDirectory = if ($ProjectRoot) { [System.IO.Path]::GetFullPath($ProjectRoot) } else { [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..')) }
$baseDirectory = if ($BaseRoot) { [System.IO.Path]::GetFullPath($BaseRoot) } else { Join-Path $projectDirectory 'artifacts/wp169i/draft' }
$draftDirectory = if ($OutputRoot) { [System.IO.Path]::GetFullPath($OutputRoot) } else { Join-Path $projectDirectory 'artifacts/wp169j/formats-draft' }
$recipe = if ($RecipePath) { [System.IO.Path]::GetFullPath($RecipePath) } else { Join-Path $PSScriptRoot 'recipes/woodland_basin_formats_recipe_v1.json' }
$environmentRecipe = if ($EnvironmentRecipePath) { [System.IO.Path]::GetFullPath($EnvironmentRecipePath) } else { Join-Path $PSScriptRoot 'recipes/woodland_basin_environment_recipe_v1.json' }
if ($baseDirectory -eq $draftDirectory) { throw 'Use a distinct format output root to preserve the Mass checkpoint.' }
& $PythonPath (Join-Path $PSScriptRoot 'generate_format_branches.py') --project-root $projectDirectory --base-root $baseDirectory --output-root $draftDirectory --recipe $recipe
if ($LASTEXITCODE -ne 0) { throw 'Exact format branch generator failed.' }
& $PythonPath (Join-Path $PSScriptRoot 'verify_format_branches.py') --project-root $projectDirectory --base-root $baseDirectory --output-root $draftDirectory
if ($LASTEXITCODE -ne 0) { throw 'Independent serialized format checks failed.' }
& $PythonPath (Join-Path $PSScriptRoot 'generate_environment.py') --project-root $projectDirectory --output-root $draftDirectory --recipe $environmentRecipe
if ($LASTEXITCODE -ne 0) { throw 'Full-format environment generation failed.' }
& $PythonPath (Join-Path $PSScriptRoot 'verify_environment.py') --project-root $projectDirectory --output-root $draftDirectory
if ($LASTEXITCODE -ne 0) { throw 'Full-format environment serialized checks failed.' }
$buildDirectory = Join-Path $draftDirectory 'dotnet-validator'
dotnet build (Join-Path $projectDirectory 'dotnet/Biathlon.BalanceLab/Biathlon.BalanceLab.csproj') --artifacts-path $buildDirectory -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'Full-format validator build failed.' }
$validatorPath = Join-Path $buildDirectory 'bin/Biathlon.BalanceLab/debug/Biathlon.BalanceLab.dll'
$reportPath = Join-Path $draftDirectory 'static-report.json'
dotnet $validatorPath venue-validate (Join-Path $draftDirectory 'resources') 'Biathlon/Venues/woodland_basin_v1/manifest' 'Biathlon/Venues/production_venue_validation_v1' $reportPath
if ($LASTEXITCODE -ne 0) { throw 'Full-format production static policy failed.' }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if (-not $report.static_checks_pass -or $report.issues.Count -ne 0) { throw 'Expected a zero-diagnostic full-format static PASS.' }
$auditPath = Join-Path $draftDirectory 'formats-audit.json'
$audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
$audit.status = 'STATIC_PRODUCTION_POLICY_AND_SERIALIZED_FORMATS_PASS_RUNTIME_AND_VISUAL_PENDING'
$audit.catalog_binding = 'EXPLICIT_MANIFEST_CONTRACT_COLD_VALIDATION_PASS'
$audit | Add-Member -MemberType NoteProperty -Name static_report_sha256 -Value (Get-FileHash -LiteralPath $reportPath -Algorithm SHA256).Hash.ToLowerInvariant() -Force
$audit | Add-Member -MemberType NoteProperty -Name validator_dll_sha256 -Value (Get-FileHash -LiteralPath $validatorPath -Algorithm SHA256).Hash.ToLowerInvariant() -Force
[System.IO.File]::WriteAllText($auditPath, ($audit | ConvertTo-Json -Depth 30) + "`n", [System.Text.UTF8Encoding]::new($false))
Write-Output 'Full-format Venue01 static policy and serialized data PASS; playable/race/replay/visual acceptance remains pending.'
