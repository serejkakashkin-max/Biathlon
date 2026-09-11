param([string]$SourceTrackId='synthetic_stadium_v1',[string]$TrackId='synthetic_mass_stadium_v1',[string]$OutputDirectory='tests/fixtures/tracks',[switch]$NarrowSector)
$ErrorActionPreference='Stop'
$sourcePrefix=$SourceTrackId -replace '_v1$',''
$prefix=$TrackId -replace '_v1$',''
$root='tests/fixtures/tracks'
$track=Get-Content (Join-Path $root "$SourceTrackId.json") -Raw | ConvertFrom-Json
$track.id=$TrackId
$source=($track.loops | Where-Object id -eq 'main_long').segments[-1]
$segment=$source | ConvertTo-Json -Depth 30 | ConvertFrom-Json
$segment.id='start_approach_flat';$segment.length_m=30
$track.loops += [ordered]@{id='start_approach';kind='race';closed=$false;segments=@($segment)}
if($NarrowSector) {
    $main=$track.loops | Where-Object id -eq 'main_long'
    if($main.segments.Count -ne 5 -or $main.segments[2].length_m -ne 200 -or $main.segments[3].length_m -ne 200){throw 'Narrow fixture requires the four200m flat segments.'}
    foreach($index in @(2,3)){$main.segments[$index].width_m=3;$main.segments[$index].overtake_allowed=$false}
}
$track | ConvertTo-Json -Depth 30 | Set-Content (Join-Path $OutputDirectory "$TrackId.json")
foreach($kind in @('junctions','connections','lanes')) {
    $json=Get-Content (Join-Path $root "${sourcePrefix}_${kind}_v1.json") -Raw
    $json=$json.Replace($SourceTrackId,$TrackId).Replace($sourcePrefix,$prefix)
    Set-Content (Join-Path $OutputDirectory "${prefix}_${kind}_v1.json") $json.TrimEnd()
}
