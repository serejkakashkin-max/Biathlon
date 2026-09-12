param([string]$Root=(Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference='Stop'
$folder=Join-Path $Root 'artifacts/balance/career-soak'
$summary=Get-Content (Join-Path $folder 'summary.json') -Raw | ConvertFrom-Json
if($summary.status -ne 'PASS' -or $summary.seasons -ne 15 -or $summary.races -ne 90){throw 'A complete passing soak is required'}
$rows=@(Get-Content (Join-Path $folder 'progress.json') -Raw | ConvertFrom-Json)
if($rows.Count -ne 15){throw 'Missing season metrics'}
$rows | Export-Csv (Join-Path $folder 'seasons.csv') -NoTypeInformation -Encoding utf8
$first=$rows[0];$last=$rows[-1]
$peak=$rows | Sort-Object player_mean_stat -Descending | Select-Object -First 1
$championAges=($rows | Measure-Object champion_age -Minimum -Maximum -Average)
$metrics=[ordered]@{schema_version=1;id='career_balance_report_v1';seasons=$rows.Count;mean_stat_first=$first.mean_stat;mean_stat_last=$last.mean_stat;player_stat_first=$first.player_mean_stat;player_stat_last=$last.player_mean_stat;player_observed_peak_age=$peak.player_age;player_observed_peak_stat=$peak.player_mean_stat;champion_age_min=$championAges.Minimum;champion_age_max=$championAges.Maximum;champion_age_mean=$championAges.Average;earned_minor=$summary.earned_minor;spent_minor=$summary.spent_minor;retired=$summary.retired;maximum_save_bytes=$summary.maximum_save_bytes;final_player_tier=$last.player_tier;final_world_money=$last.world_money;final_save_sha256=$summary.save_sha256}
$metrics | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $folder 'balance-summary.json') -Encoding utf8
$metrics | ConvertTo-Json -Depth 5
