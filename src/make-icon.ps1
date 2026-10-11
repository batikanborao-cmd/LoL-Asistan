param([string]$OutIco, [string]$OutPng)
Add-Type -AssemblyName System.Drawing

function Hex([float]$cx, [float]$cy, [float]$r) {
    $pts = New-Object 'System.Drawing.PointF[]' 6
    for ($i = 0; $i -lt 6; $i++) {
        $a = [Math]::PI / 180 * (60 * $i - 90)
        $pts[$i] = New-Object System.Drawing.PointF(($cx + $r * [Math]::Cos($a)), ($cy + $r * [Math]::Sin($a)))
    }
    $pts
}

function Draw([int]$n) {
    $bmp = New-Object System.Drawing.Bitmap($n, $n, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $n / 256.0
    $c = 128 * $s

    $navyTop = [System.Drawing.Color]::FromArgb(255, 30, 45, 80)
    $navyBot = [System.Drawing.Color]::FromArgb(255, 8, 16, 34)
    $gold = [System.Drawing.Color]::FromArgb(255, 200, 170, 110)
    $goldLight = [System.Drawing.Color]::FromArgb(255, 240, 220, 160)
    $teal = [System.Drawing.Color]::FromArgb(255, 10, 200, 185)

    # Lacivert altigen zemin
    $outer = Hex $c $c (122 * $s)
    $rect = New-Object System.Drawing.RectangleF(0, 0, $n, $n)
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $navyTop, $navyBot, 90)
    $g.FillPolygon($bg, $outer)

    # Altin cerceve
    $goldBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $goldLight, $gold, 90)
    $pen = New-Object System.Drawing.Pen($goldBrush, [float][Math]::Max(1.5, 12 * $s))
    $pen.LineJoin = 'Round'
    $g.DrawPolygon($pen, (Hex $c $c (114 * $s)))
    if ($n -ge 48) {
        $thin = New-Object System.Drawing.Pen($gold, [float][Math]::Max(1, 3 * $s))
        $g.DrawPolygon($thin, (Hex $c $c (94 * $s)))
    }

    # Turkuaz "L" harfi (koseleri kesik, kalin)
    $raw = @(86,62, 140,62, 128,76, 128,148, 164,148, 178,134, 178,184, 86,184, 98,170, 98,76)
    $pts = New-Object 'System.Drawing.PointF[]' ($raw.Count / 2)
    for ($i = 0; $i -lt $pts.Length; $i++) { $pts[$i] = New-Object System.Drawing.PointF(($raw[2 * $i] * $s), ($raw[2 * $i + 1] * $s)) }
    if ($n -ge 48) {
        $glow = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70, 10, 200, 185), [float](14 * $s))
        $glow.LineJoin = 'Round'
        $g.DrawPolygon($glow, $pts)
    }
    $lRect = New-Object System.Drawing.RectangleF((86 * $s), (62 * $s), (92 * $s), (122 * $s))
    $lBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($lRect, [System.Drawing.Color]::FromArgb(255, 120, 240, 225), $teal, 90)
    $g.FillPolygon($lBrush, $pts)
    $g.Dispose()
    $bmp
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = @()
foreach ($sz in $sizes) {
    $b = Draw $sz
    $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , $ms.ToArray()
    if ($sz -eq 256 -and $OutPng) { $b.Save($OutPng, [System.Drawing.Imaging.ImageFormat]::Png) }
    $b.Dispose()
}

# ICO dosyasi (PNG gomulu girdiler)
$fs = [System.IO.File]::Create($OutIco)
$w = New-Object System.IO.BinaryWriter($fs)
$w.Write([UInt16]0); $w.Write([UInt16]1); $w.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]; $d = $pngs[$i]
    $dim = if ($sz -ge 256) { 0 } else { $sz }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([UInt16]1); $w.Write([UInt16]32); $w.Write([UInt32]$d.Length); $w.Write([UInt32]$offset)
    $offset += $d.Length
}
foreach ($d in $pngs) { $w.Write($d) }
$w.Close()
