$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$source = [System.Drawing.Image]::FromFile((Join-Path $root 'Assets\AirPodsPro.png'))
$frames = @()
try {
    foreach ($size in @(16,24,32,48,64,128,256)) {
        $bitmap = [System.Drawing.Bitmap]::new($size,$size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $stream = [IO.MemoryStream]::new()
        try {
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            # Remove excess transparent margins so the artwork fills a standard tray slot.
            $graphics.DrawImage($source,[System.Drawing.Rectangle]::new(0,0,$size,$size),[System.Drawing.Rectangle]::new(41,26,430,430),[System.Drawing.GraphicsUnit]::Pixel)
            $bitmap.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
            $frames += [pscustomobject]@{ Size=$size; Bytes=$stream.ToArray() }
        } finally { $stream.Dispose(); $graphics.Dispose(); $bitmap.Dispose() }
    }
    $output = [IO.File]::Create((Join-Path $root 'Assets\PodGlide.ico'))
    $writer = [IO.BinaryWriter]::new($output)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
        $offset = 6 + 16 * $frames.Count
        foreach ($frame in $frames) {
            $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
            $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frame.Bytes.Length); $writer.Write([uint32]$offset)
            $offset += $frame.Bytes.Length
        }
        foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Bytes) }
    } finally { $writer.Dispose(); $output.Dispose() }
} finally { $source.Dispose() }
