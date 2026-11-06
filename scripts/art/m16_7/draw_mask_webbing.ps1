param([Parameter(Mandatory=$true)][string]$Output,[Parameter(Mandatory=$true)][string]$FontPath)
Add-Type -AssemblyName System.Drawing
$bitmap=[System.Drawing.Bitmap]::new(2048,256);$g=[System.Drawing.Graphics]::FromImage($bitmap)
$g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias;$g.Clear([System.Drawing.Color]::FromArgb(22,29,37))
$thread=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(32,40,48),1)
for($y=0;$y -lt 256;$y+=4){$g.DrawLine($thread,0,$y,2048,$y)}
for($x=0;$x -lt 2048;$x+=4){$g.DrawLine($thread,$x,0,$x,256)}
$red=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(171,18,39));$white=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(225,233,235))
$g.FillRectangle($red,0,10,2048,10);$g.FillRectangle($red,0,236,2048,10)
$fonts=[System.Drawing.Text.PrivateFontCollection]::new();$fonts.AddFontFile($FontPath)
foreach($x in @(215,1390)){
 $path=[System.Drawing.Drawing2D.GraphicsPath]::new([System.Drawing.Drawing2D.FillMode]::Winding)
 $path.AddString('PULSE',$fonts.Families[0],1,128,[System.Drawing.PointF]::new($x,48),[System.Drawing.StringFormat]::GenericTypographic);$g.FillPath($white,$path);$path.Dispose()
}
$bitmap.Save($Output,[System.Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$bitmap.Dispose();$thread.Dispose();$red.Dispose();$white.Dispose();$fonts.Dispose()
