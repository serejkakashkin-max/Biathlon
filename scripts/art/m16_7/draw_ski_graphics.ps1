param([Parameter(Mandatory=$true)][string]$Output,[Parameter(Mandatory=$true)][string]$FontPath)
Add-Type -AssemblyName System.Drawing
$bitmap=[System.Drawing.Bitmap]::new(512,4096)
$g=[System.Drawing.Graphics]::FromImage($bitmap)
$g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint=[System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.Clear([System.Drawing.Color]::FromArgb(13,20,26))
$red=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(197,21,44))
$white=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(238,244,244))
$grey=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(125,149,158))
$g.FillRectangle($red,0,0,512,1120);$g.FillRectangle($red,0,2900,512,1196)
$g.FillRectangle($white,28,0,8,4096);$g.FillRectangle($white,476,0,8,4096)
$fonts=[System.Drawing.Text.PrivateFontCollection]::new();$fonts.AddFontFile($FontPath)
function Mark([string]$Text,[float]$Size,[float]$X,[float]$Y,[System.Drawing.Brush]$Brush){
 $p=[System.Drawing.Drawing2D.GraphicsPath]::new([System.Drawing.Drawing2D.FillMode]::Winding)
 $p.AddString($Text,$fonts.Families[0],1,$Size,[System.Drawing.PointF]::new($X,$Y),[System.Drawing.StringFormat]::GenericTypographic)
 $g.FillPath($Brush,$p);$p.Dispose()
}
$g.TranslateTransform(80,1050);$g.RotateTransform(-90);Mark 'PULSE' 265 0 0 $white;$g.ResetTransform()
$g.TranslateTransform(110,3700);$g.RotateTransform(-90);Mark 'PULSE' 220 0 0 $white;$g.ResetTransform()
Mark '194' 145 120 3700 $white
$g.TranslateTransform(335,2700);$g.RotateTransform(-90);Mark 'NORDIC / CARBON' 82 0 0 $grey;$g.ResetTransform()
for($i=0;$i -lt 8;$i++){$g.FillRectangle($white,80,2800+$i*20,100+$i*32,7)}
$bitmap.Save($Output,[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$bitmap.Dispose();$red.Dispose();$white.Dispose();$grey.Dispose();$fonts.Dispose()
