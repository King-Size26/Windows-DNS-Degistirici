Add-Type -AssemblyName System.Drawing

$icoPath = "C:\Users\alim\Desktop\DNSDegistiriciApp\app.ico"
$pngPath = "C:\Users\alim\Desktop\DNSDegistiriciApp\icon256.png"

function Create-IconBitmap($size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::Transparent)

    # Background rounded rect with gradient
    $rect = New-Object System.Drawing.Rectangle 0,0,($size-1),($size-1)
    $radius = [int]($size * 0.22)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($rect.X, $rect.Y, $radius, $radius, 180, 90)
    $path.AddArc($rect.Right - $radius, $rect.Y, $radius, $radius, 270, 90)
    $path.AddArc($rect.Right - $radius, $rect.Bottom - $radius, $radius, $radius, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $radius, $radius, $radius, 90, 90)
    $path.CloseFigure()

    # Gradient from deep blue to lighter blue
    $c1 = [System.Drawing.Color]::FromArgb(13,71,161)
    $c2 = [System.Drawing.Color]::FromArgb(30,136,229)
    $br = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $c1, $c2, 90
    $g.FillPath($br, $path)
    $br.Dispose()

    # Inner highlight border
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(60,255,255,255), [int]($size*0.012))
    $g.DrawPath($pen, $path)
    $pen.Dispose()

    # Subtle shadow at bottom
    $shadowRect = New-Object System.Drawing.Rectangle ([int]($size*0.08)), ([int]($size*0.68)), ([int]($size*0.84)), ([int]($size*0.18))
    $shadowPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $shadowPath.AddEllipse($shadowRect)
    $shadowBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush $shadowPath
    $shadowBrush.CenterColor = [System.Drawing.Color]::FromArgb(35,0,0,0)
    $shadowBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0,0,0,0))
    $g.FillPath($shadowBrush, $shadowPath)
    $shadowBrush.Dispose()
    $shadowPath.Dispose()

    # Globe / network icon - central
    # Draw globe circles
    $globeSize = [int]($size * 0.38)
    $gx = [int](($size - $globeSize)/2)
    $gy = [int]($size * 0.22)
    $globeRect = New-Object System.Drawing.Rectangle $gx,$gy,$globeSize,$globeSize
    $globePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220,255,255,255), [int]($size*0.012))
    $globePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    # outer circle
    $g.DrawEllipse($globePen, $globeRect)
    # vertical ellipse
    $innerRect1 = New-Object System.Drawing.Rectangle ([int]($gx + $globeSize*0.28)), $gy, ([int]($globeSize*0.44)), $globeSize
    $g.DrawEllipse($globePen, $innerRect1)
    # horizontal line
    $hy = $gy + $globeSize/2
    $g.DrawLine($globePen, $gx, $hy, ($gx + $globeSize), $hy)
    # two latitude lines
    $y1 = $gy + $globeSize*0.30
    $y2 = $gy + $globeSize*0.70
    $g.DrawLine($globePen, ([int]($gx + $globeSize*0.15)), $y1, ([int]($gx + $globeSize*0.85)), $y1)
    $g.DrawLine($globePen, ([int]($gx + $globeSize*0.15)), $y2, ([int]($gx + $globeSize*0.85)), $y2)
    $globePen.Dispose()

    # DNS text
    $fontSize = [int]($size * 0.21)
    $font = New-Object System.Drawing.Font "Segoe UI", $fontSize, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $text = "DNS"
    $sz = $g.MeasureString($text, $font)
    $tx = ($size - $sz.Width)/2
    $ty = $gy + $globeSize + [int]($size*0.06)
    # text shadow
    $shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80,0,0,0))
    $g.DrawString($text, $font, $shadowBrush, ($tx+2), ($ty+2))
    $shadowBrush.Dispose()
    $g.DrawString($text, $font, [System.Drawing.Brushes]::White, $tx, $ty)
    $font.Dispose()

    # Three dots for providers (Quad9 purple, Google blue, Cloudflare orange)
    $dotR = [int]($size * 0.032)
    $dotY = $ty + $sz.Height + [int]($size*0.05)
    $dotX0 = [int]($size/2 - 2.5*$dotR - 6)
    $colors = @(
        [System.Drawing.Color]::FromArgb(106,27,154),
        [System.Drawing.Color]::FromArgb(25,118,210),
        [System.Drawing.Color]::FromArgb(239,108,0),
        [System.Drawing.Color]::FromArgb(46,125,50)
    )
    # white backing for dots
    for ($i=0; $i -lt 4; $i++) {
        $cx = $dotX0 + $i * ($dotR*2 + 8)
        $dotRect = New-Object System.Drawing.Rectangle ($cx -2), ($dotY -2), ($dotR*2+4), ($dotR*2+4)
        $g.FillEllipse([System.Drawing.Brushes]::White, $dotRect)
    }
    for ($i=0; $i -lt 4; $i++) {
        $cx = $dotX0 + $i * ($dotR*2 + 8)
        $dotRect = New-Object System.Drawing.Rectangle $cx, $dotY, ($dotR*2), ($dotR*2)
        $br2 = New-Object System.Drawing.SolidBrush $colors[$i]
        $g.FillEllipse($br2, $dotRect)
        $br2.Dispose()
    }

    $path.Dispose()
    $g.Dispose()
    return $bmp
}

# Create 256
$bmp256 = Create-IconBitmap 256
$bmp256.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "PNG saved $pngPath"

# Also create smaller sizes for ICO
$sizes = @(16,32,48,64,128,256)
$bitmaps = @()
foreach ($s in $sizes) {
    $b = Create-IconBitmap $s
    $bitmaps += $b
}

# Build ICO file manually (ICO header)
# Use .NET Icon approach: create temp bitmaps and save via Icon
# Simplest: use 256 png as icon via conversion - create Icon from handle
$icoBmp = $bitmaps[-1] # 256
$hIcon = $icoBmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$fs = [System.IO.File]::Create($icoPath)
$icon.Save($fs)
$fs.Close()
# Cleanup handle
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class IconUtil { [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr hIcon); }
"@
[IconUtil]::DestroyIcon($hIcon) | Out-Null
Write-Host "ICO saved $icoPath (single 256)"

# Try to create proper multi-size ICO via binary writer (optional)
try {
    $icoPathMulti = "C:\Users\alim\Desktop\DNSDegistiriciApp\app_multi.ico"
    # Build multi-image ICO containing PNGs
    $images = @()
    foreach ($b in $bitmaps) {
        $ms = New-Object System.IO.MemoryStream
        $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $images += @{ Size = $b.Width; Data = $ms.ToArray() }
        $ms.Dispose()
    }
    $fs2 = [System.IO.File]::Create($icoPathMulti)
    $bw = New-Object System.IO.BinaryWriter $fs2
    $bw.Write([UInt16]0) # reserved
    $bw.Write([UInt16]1) # type 1 = icon
    $bw.Write([UInt16]$images.Count)
    $offset = 6 + 16 * $images.Count
    $headers = @()
    foreach ($img in $images) {
        $w = if ($img.Size -eq 256) {0} else {$img.Size}
        $bw.Write([byte]$w) # width
        $bw.Write([byte]$w) # height
        $bw.Write([byte]0) # colors
        $bw.Write([byte]0) # reserved
        $bw.Write([UInt16]1) # planes
        $bw.Write([UInt16]32) # bpp
        $bw.Write([UInt32]$img.Data.Length)
        $bw.Write([UInt32]$offset)
        $offset += $img.Data.Length
    }
    foreach ($img in $images) { $bw.Write($img.Data) }
    $bw.Close(); $fs2.Close()
    Write-Host "Multi ICO saved $icoPathMulti with $($images.Count) images"
    # Replace single with multi
    Copy-Item -LiteralPath $icoPathMulti -Destination $icoPath -Force
    Write-Host "Replaced with multi-size ICO"
} catch { Write-Host "Multi ICO failed: $_" }

foreach ($b in $bitmaps) { $b.Dispose() }
$bmp256.Dispose()

Write-Host "DONE"
