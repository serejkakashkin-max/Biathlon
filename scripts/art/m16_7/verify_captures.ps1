param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$results=@()
$poseSamples=@{}
foreach($name in @('front','side','back','face','rifle','boots','pose1','pose2','pose3')) {
    $bitmap=[System.Drawing.Bitmap]::new((Join-Path $Folder ($name+'.png')))
    try {
        $samples=0; $visible=0; $raisedSleeves=0
        # Exclude the left UI rail. A visible menu alone cannot qualify the model.
        for($y=0; $y -lt $bitmap.Height; $y+=16) {
            for($x=[int]($bitmap.Width*.25); $x -lt $bitmap.Width; $x+=16) {
                $color=$bitmap.GetPixel($x,$y); $samples++
                if(([int]$color.R+[int]$color.G+[int]$color.B) -gt 90) {$visible++}
                if($name -in @('pose2','pose3') -and $y -lt $bitmap.Height*.31 -and [Math]::Abs($x-$bitmap.Width*.5) -gt $bitmap.Width*.04 -and $x -gt $bitmap.Width*.3 -and $x -lt $bitmap.Width*.7 -and $color.R -gt 55 -and $color.R -gt $color.G*1.5 -and $color.R -gt $color.B*1.4) {$raisedSleeves++}
            }
        }
        $fraction=$visible/[double]$samples
        $results+=@{name=$name;model_area_visible_fraction=$fraction;pass=$fraction -gt .01}
        if($name -in @('pose2','pose3')) {$poseSamples[$name]=$raisedSleeves}
    } finally {$bitmap.Dispose()}
}
$posesMatch=$poseSamples['pose2'] -ge 5 -and $poseSamples['pose3'] -le 1
$report=@{schema_version=1;id='m16_7_model_area_capture_check';success=(@($results|Where-Object {-not $_.pass}).Count -eq 0 -and $posesMatch);frames=$results;poses_match=$posesMatch;upper_sleeve_samples=$poseSamples}
$report|ConvertTo-Json -Depth 6
if(-not $report.success){exit 1}
