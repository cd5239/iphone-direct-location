$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
# Draw a simple navigation marker at each native Windows icon size.
$frames=@()
foreach($size in @(16,20,24,32,40,48,64,128,256)) {
 $bitmap=New-Object System.Drawing.Bitmap($size,$size)
 $g=[System.Drawing.Graphics]::FromImage($bitmap)
 $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
 $g.ScaleTransform($size/64.0,$size/64.0)
 $blue=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(37,99,235))
 $g.FillEllipse($blue,1,1,62,62)
 $path=New-Object System.Drawing.Drawing2D.GraphicsPath
 $path.AddBezier(32,10,10,10,11,34,32,54)
 $path.AddBezier(32,54,53,34,54,10,32,10)
 $g.FillPath([System.Drawing.Brushes]::White,$path)
 $g.FillEllipse($blue,25,20,14,14)
 $stream=New-Object System.IO.MemoryStream
 $bitmap.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
 $frames+=,@{Size=$size;Bytes=$stream.ToArray()}
 $stream.Dispose();$path.Dispose();$blue.Dispose();$g.Dispose();$bitmap.Dispose()
}
$file=[System.IO.File]::Create((Join-Path $PSScriptRoot 'app.ico'))
$writer=New-Object System.IO.BinaryWriter($file)
$writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$frames.Count)
$offset=6+16*$frames.Count
foreach($frame in $frames) {
 $dimension=if($frame.Size -eq 256){0}else{$frame.Size}
 $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
 $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frame.Bytes.Length);$writer.Write([uint32]$offset)
 $offset+=$frame.Bytes.Length
}
foreach($frame in $frames){$writer.Write([byte[]]$frame.Bytes)}
$writer.Dispose();$file.Dispose()
