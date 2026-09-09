param([string]$OutputDirectory = 'tests/fixtures/tracks', [string]$SourceTrackId = 'synthetic_flat_v1', [string]$TrackId = 'synthetic_stadium_v1')
$ErrorActionPreference = 'Stop'
# Derive lane ski metadata from the authoritative synthetic range segment.
$track = Get-Content (Join-Path 'tests/fixtures/tracks' ($SourceTrackId + '.json')) -Raw | ConvertFrom-Json
$track.id = $TrackId
$prefix = $TrackId -replace '_v1$', ''
$source = ($track.loops | Where-Object id -eq 'main_long').segments[-1]
$laneIds = @()
$entryPorts = @([ordered]@{id='range_main_entry';anchor=[ordered]@{loop_id='main_long';distance_m=800};guard_before_m=2;guard_after_m=2})
$junctions = Get-Content 'tests/fixtures/tracks/synthetic_flat_junctions_v1.json' -Raw | ConvertFrom-Json
$junctions.id = "${prefix}_junctions_v1"; $junctions.track_id = $track.id
# The directed seams all meet main900. main920 is a logical rejoin receipt, not
# another physical crossing. Preserve the older wide-guard stress fixture separately.
$junctions.junctions[0].ports = @($junctions.junctions[0].ports | Where-Object id -ne 'main_rejoin')
($junctions.junctions[0].ports | Where-Object id -eq 'main_entry').guard_after_m = 2
$connections = Get-Content 'tests/fixtures/tracks/synthetic_flat_connections_v1.json' -Raw | ConvertFrom-Json
$connections.id = "${prefix}_connections_v1"; $connections.junction_definition_id = $junctions.id
for ($i = 1; $i -le 30; $i++) {
    $id = "range_lane_$i"; $laneIds += $id
    $segment = $source | ConvertTo-Json -Depth 30 | ConvertFrom-Json
    $segment.id = "${id}_approach"; $segment.length_m = 100
    $track.loops += [ordered]@{id=$id;kind='race';closed=$false;segments=@($segment)}
    $entryPorts += [ordered]@{id="${id}_start";anchor=[ordered]@{loop_id=$id;distance_m=0};guard_before_m=0;guard_after_m=2}
    $junctions.junctions[0].ports += [ordered]@{id="${id}_end";anchor=[ordered]@{loop_id=$id;distance_m=100};guard_before_m=2;guard_after_m=0}
    $connections.connections += [ordered]@{id="${id}_enter";junction_id='range_access';from_port_id='range_main_entry';to_port_id="${id}_start";kind='coincident_seam'}
    $connections.connections += [ordered]@{id="${id}_exit";junction_id='penalty_access';from_port_id="${id}_end";to_port_id='main_entry';kind='coincident_seam'}
    $connections.connections += [ordered]@{id="${id}_penalty";junction_id='penalty_access';from_port_id="${id}_end";to_port_id='penalty_seam';kind='coincident_seam'}
}
$junctions.junctions += [ordered]@{id='range_access';ports=$entryPorts}
$lanes = [ordered]@{schema_version=1;id="${prefix}_lanes_v1";track_id=$track.id;main_loop_id='main_long';entry_distance_m=800;exit_distance_m=900;lane_loop_ids=$laneIds}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
foreach ($definition in @($track,$junctions,$connections,$lanes)) {
    $definition | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $OutputDirectory ($definition.id + '.json')) -Encoding utf8
}
