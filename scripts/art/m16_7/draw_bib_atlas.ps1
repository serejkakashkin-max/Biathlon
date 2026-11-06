param([Parameter(Mandatory=$true)][string]$Definition,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$settings=(Get-Content -LiteralPath $Definition -Raw | ConvertFrom-Json).bib_print
$projectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$fonts=[Drawing.Text.PrivateFontCollection]::new();$fonts.AddFontFile((Join-Path $projectRoot $settings.font))
$bitmap=[Drawing.Bitmap]::new($settings.resolution,$settings.resolution,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics=[Drawing.Graphics]::FromImage($bitmap)
$layout=[Collections.Generic.List[object]]::new()
try{
    $graphics.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([Drawing.Color]::FromArgb(255,229,231,230))
    $brush=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,29,35,39))
    try{
        $pxPerMeterX=(.5-2*$settings.page_padding_uv)*$settings.resolution/(2*$settings.half_width_m)
        $pxPerMeterY=$settings.resolution/($settings.top_m-$settings.bottom_m)
        foreach($page in 0,1){foreach($key in 'number','footer','brand'){
            $path=[Drawing.Drawing2D.GraphicsPath]::new([Drawing.Drawing2D.FillMode]::Winding)
            try{
                $path.AddString([string]$settings.$key,$fonts.Families[0],[int][Drawing.FontStyle]::Regular,100,[Drawing.PointF]::new(0,0),[Drawing.StringFormat]::GenericTypographic)
                $bounds=$path.GetBounds();$sx=([double]$settings.($key+'_width_m')*$pxPerMeterX)/$bounds.Width;$sy=$sx*$pxPerMeterY/$pxPerMeterX
                $cx=($page*.5+.25)*$settings.resolution;$cy=($settings.top_m-[double]$settings.($key+'_z_m'))*$pxPerMeterY
                $matrix=[Drawing.Drawing2D.Matrix]::new([single]$sx,0,0,[single]$sy,[single]($cx-($bounds.X+$bounds.Width/2)*$sx),[single]($cy-($bounds.Y+$bounds.Height/2)*$sy))
                try{$path.Transform($matrix);$graphics.FillPath($brush,$path);$actual=$path.GetBounds();$layout.Add([pscustomobject]@{page=$page;text=$settings.$key;x=$actual.X;y=$actual.Y;width=$actual.Width;height=$actual.Height})}finally{$matrix.Dispose()}
            }finally{$path.Dispose()}
        }}
    }finally{$brush.Dispose()}
    $bitmap.Save($Output,[Drawing.Imaging.ImageFormat]::Png)
    [IO.File]::WriteAllText(($Output+'.layout.json'),($layout | ConvertTo-Json -Depth 3),[Text.UTF8Encoding]::new($false))
}finally{$graphics.Dispose();$bitmap.Dispose();$fonts.Dispose()}
