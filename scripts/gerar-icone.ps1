param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\Assets")
)

Add-Type -AssemblyName System.Drawing

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class NativeIconMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
"@

function New-RoundedRectanglePath {
    param(
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius
    )

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $Radius * 2

    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()

    return $path
}

function New-MonitorHardwareBitmap {
    param([int]$Size)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $backgroundPath = New-RoundedRectanglePath -X ($Size * 0.06) -Y ($Size * 0.06) -Width ($Size * 0.88) -Height ($Size * 0.88) -Radius ($Size * 0.18)
    $backgroundBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.RectangleF]::new(0, 0, $Size, $Size),
        [System.Drawing.Color]::FromArgb(18, 22, 27),
        [System.Drawing.Color]::FromArgb(35, 43, 52),
        45)

    $graphics.FillPath($backgroundBrush, $backgroundPath)

    $borderPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(70, 210, 235), [Math]::Max(2, $Size * 0.035))
    $graphics.DrawPath($borderPen, $backgroundPath)

    $pulsePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(0, 220, 190), [Math]::Max(3, $Size * 0.05))
    $pulsePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pulsePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pulsePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $points = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($Size * 0.18, $Size * 0.55),
        [System.Drawing.PointF]::new($Size * 0.35, $Size * 0.55),
        [System.Drawing.PointF]::new($Size * 0.44, $Size * 0.35),
        [System.Drawing.PointF]::new($Size * 0.56, $Size * 0.70),
        [System.Drawing.PointF]::new($Size * 0.66, $Size * 0.47),
        [System.Drawing.PointF]::new($Size * 0.82, $Size * 0.47)
    )

    $graphics.DrawLines($pulsePen, $points)

    $fontSize = [Math]::Max(10, $Size * 0.18)
    $font = [System.Drawing.Font]::new("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(235, 248, 255))
    $textFormat = [System.Drawing.StringFormat]::new()
    $textFormat.Alignment = [System.Drawing.StringAlignment]::Center
    $textFormat.LineAlignment = [System.Drawing.StringAlignment]::Center

    $graphics.DrawString("MH", $font, $textBrush, [System.Drawing.RectangleF]::new(0, $Size * 0.64, $Size, $Size * 0.24), $textFormat)

    $graphics.Dispose()
    $backgroundPath.Dispose()
    $backgroundBrush.Dispose()
    $borderPen.Dispose()
    $pulsePen.Dispose()
    $font.Dispose()
    $textBrush.Dispose()
    $textFormat.Dispose()

    return $bitmap
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$pngPath = Join-Path $OutputDirectory "monitor-hardware.png"
$icoPath = Join-Path $OutputDirectory "monitor-hardware.ico"

$pngBitmap = New-MonitorHardwareBitmap -Size 256
$pngBitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBitmap.Dispose()

$iconBitmap = New-MonitorHardwareBitmap -Size 64
$iconHandle = $iconBitmap.GetHicon()

try {
    $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
    $stream = [System.IO.File]::Create($icoPath)

    try {
        $icon.Save($stream)
    }
    finally {
        $stream.Dispose()
        $icon.Dispose()
    }
}
finally {
    [NativeIconMethods]::DestroyIcon($iconHandle) | Out-Null
    $iconBitmap.Dispose()
}

Write-Host "Icone gerado em: $icoPath"
Write-Host "Logo gerado em: $pngPath"
