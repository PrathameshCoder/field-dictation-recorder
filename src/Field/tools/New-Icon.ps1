Add-Type -AssemblyName System.Drawing
[xml]$tokens = Get-Content -Raw "$PSScriptRoot\..\Design\Tokens.xaml"
$colors = @{}
foreach ($brush in $tokens.ResourceDictionary.SolidColorBrush) { $colors[$brush.GetAttribute('Key','http://schemas.microsoft.com/winfx/2006/xaml')] = [System.Drawing.ColorTranslator]::FromHtml($brush.Color) }
$frames = @()
foreach ($size in @(16,24,32,48,64,128,256)) {
 $bitmap = [System.Drawing.Bitmap]::new($size,$size)
 $g = [System.Drawing.Graphics]::FromImage($bitmap)
 $g.SmoothingMode = 'AntiAlias'
 $g.ScaleTransform($size/64.0,$size/64.0)
 $brushes = @{}
 foreach ($key in $colors.Keys) { $brushes[$key] = [System.Drawing.SolidBrush]::new($colors[$key]) }
 $g.FillRectangle($brushes.Control,2,2,60,60)
 $g.FillRectangle($brushes.Body,5,5,54,54)
 $g.FillRectangle($brushes.Instrument,10,11,44,25)
 $pen = [System.Drawing.Pen]::new($colors.Ink,2)
 $g.DrawLine($pen,32,32,20,18)
 $g.FillEllipse($brushes.Ink,29,29,6,6)
 $g.FillEllipse($brushes.Record,11,43,10,10)
 $g.FillRectangle($brushes.Control,28,43,10,10)
 $g.FillRectangle($brushes.Control,44,43,10,10)
 $stream = [System.IO.MemoryStream]::new()
 $bitmap.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
 $frames += ,@($size,$stream.ToArray())
 $stream.Dispose(); $pen.Dispose(); $g.Dispose(); $bitmap.Dispose()
 foreach ($brush in $brushes.Values) { $brush.Dispose() }
}
$output = [System.IO.BinaryWriter]::new([System.IO.File]::Create("$PSScriptRoot\..\Design\Field.ico"))
$output.Write([uint16]0); $output.Write([uint16]1); $output.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($frame in $frames) { $dimension = if ($frame[0] -eq 256) { 0 } else { $frame[0] }; $output.Write([byte]$dimension); $output.Write([byte]$dimension); $output.Write([byte]0); $output.Write([byte]0); $output.Write([uint16]1); $output.Write([uint16]32); $output.Write([uint32]$frame[1].Length); $output.Write([uint32]$offset); $offset += $frame[1].Length }
foreach ($frame in $frames) { $output.Write([byte[]]$frame[1]) }
$output.Dispose()
