param(
    [string]$OutputDirectory = 'artifacts/balance/sensitivity',
    [string]$Dotnet = "$env:LOCALAPPDATA/Biathlon/dotnet/dotnet.exe"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
New-Item -ItemType Directory -Force $output | Out-Null
Set-Content -LiteralPath (Join-Path $output 'status.json') -Value '{"schema_version":1,"id":"balance_sensitivity_v1","status":"incomplete"}' -Encoding utf8
# The portable experiment tests are the single implementation of the locked marginal matrix.
# Isolated binaries allow a milestone regression to keep running in the normal output directory.
& $Dotnet test (Join-Path $root 'dotnet/Biathlon.Tests/Biathlon.Tests.csproj') -c Release --no-restore "-p:OutputPath=$output/bin/" --filter 'FullyQualifiedName~DraftCalibrationTests|FullyQualifiedName~StatEquipmentCalibrationTests|FullyQualifiedName~AiParityAuditTests' --logger 'trx;LogFileName=verification.trx' --results-directory $output
if ($LASTEXITCODE -ne 0) { throw 'Sensitivity/portability/parity regression failed; status remains incomplete.' }
$hashes = [ordered]@{}
foreach ($name in 'wp097.jsonl','wp098-ski.jsonl','wp098-shooting.jsonl','wp099.jsonl') {
    $source = Join-Path $root ('artifacts/balance/dotnet-' + $name)
    $destination = Join-Path $output $name
    Copy-Item -LiteralPath $source -Destination $destination -Force
    $rows = @(Get-Content -LiteralPath $source | ForEach-Object { $_ | ConvertFrom-Json })
    foreach ($row in $rows) {
        foreach ($property in $row.PSObject.Properties) {
            if ($property.Value -is [IFormattable]) { $property.Value = $property.Value.ToString($null, [Globalization.CultureInfo]::InvariantCulture) }
        }
    }
    $columns = @($rows | ForEach-Object { $_.PSObject.Properties.Name } | Sort-Object -Unique)
    $rows | Select-Object $columns | Export-Csv -LiteralPath (Join-Path $output ($name.Replace('.jsonl','.csv'))) -NoTypeInformation -Encoding utf8
    $hashes[$name] = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
}
[ordered]@{schema_version=1;id='balance_sensitivity_v1';status='PASS';hashes=$hashes} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'status.json') -Encoding utf8
@'
# Balance sensitivity — PASS

- Draft: eight seeded three-athlete shared-race counterfactuals, actual occupancy/range/AI.
- Ski stats: +10 to each of eight stats, baselines 40/60/80, four terrains, three laps, 60 s work / 20 s recovery.
- Shooting stats: +10 to five stats, both stances, 500 held-out seeds; accuracy and time separately.
- Equipment: two equipment alternatives, four snow profiles and four terrains; compare against +10 to all eight ski stats.
- AI parity: expert/developing on flat/rolling; complete commanded-field replay without AI.

JSONL and CSV contain all measured rows. The tests enforce the frozen cross-runtime digests and declared envelopes. This is a provisional matrix, not an estimate of a whole career population. See TASK-097–100 reports for scope and limitations.
'@ | Set-Content -LiteralPath (Join-Path $output 'report.md') -Encoding utf8
