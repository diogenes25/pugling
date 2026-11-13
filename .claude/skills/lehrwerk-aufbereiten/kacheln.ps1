<#
.SYNOPSIS
  Zerlegt Fotos dicht bedruckter Buchseiten in überlappende Kacheln, damit jede Zeile groß genug gelesen wird.
  Dazu zwei Schalter auf demselben geladenen Bild: -Uebersicht (Kontaktbogen) und -Ausschnitt (vergrößerter Bereich).

.EXAMPLE
  pwsh -File kacheln.ps1 -Quelle "raw/france/Decouvertes/Serie bleue 2/IMG_1.jpg" -Ziel "raw/france/decouvertes-2-pugling/kacheln" -Zeilen 3

  Ein Bild oder ein Ordner (alle .jpg/.jpeg/.png nach Namen). Die Kacheln heißen <name>-r<zeile>c<spalte>.jpg.
  Das Ziel muss unter raw/ liegen (gitignored): Kacheln, Bögen und Ausschnitte sind Ausschnitte des Buchs und kommen
  nie ins Repo.

.EXAMPLE
  pwsh -File kacheln.ps1 -Quelle "raw/<fach>/<werk>/IMG_7.jpg" -Ziel "raw/<fach>/<werk-slug>-pugling/kacheln" -Zeilen 3 -Drehung 180

  Dreht von Hand im Uhrzeigersinn (0|90|180|270), nach der EXIF-Drehung – für flach über dem Tisch fotografierte Seiten,
  denen das Handy keine EXIF-Drehung mitgibt.

.EXAMPLE
  pwsh -File kacheln.ps1 -Quelle "raw/<fach>/<werk>" -Ziel "raw/<fach>/<werk-slug>-pugling/uebersicht" -Uebersicht

  Kontaktbogen: 4×3 Miniaturen je Blatt, jede mit laufender Nummer und Dateiname; Blätter uebersicht-<n>.jpg.

.EXAMPLE
  pwsh -File kacheln.ps1 -Quelle "raw/<fach>/<werk>/IMG_9.jpg" -Ziel "raw/<fach>/<werk-slug>-pugling/kacheln" -Ausschnitt 0,0,1,0.15

  Bereich x,y,Breite,Höhe als Bruchteile 0–1 des (gedrehten) Bilds, zweifach hochskaliert (gekappt durch -MaxKante);
  Datei <name>-ausschnitt.jpg.
#>
[CmdletBinding(DefaultParameterSetName = 'Kacheln')]
param(
    [Parameter(Mandatory)] [string] $Quelle,
    [Parameter(Mandatory)] [string] $Ziel,
    [Parameter(ParameterSetName = 'Kacheln')] [int] $Zeilen = 2,
    [Parameter(ParameterSetName = 'Kacheln')] [int] $Spalten = 1,
    # Overlap between neighbouring tiles, so a line cut at a border is whole in one of them.
    [Parameter(ParameterSetName = 'Kacheln')] [double] $Ueberlappung = 0.08,
    # Longest edge of a tile or crop; larger images are scaled down before the model sees them anyway.
    [int] $MaxKante = 1800,
    # Manual clockwise rotation after EXIF: a phone held flat over the table writes no EXIF rotation.
    [ValidateSet(0, 90, 180, 270)] [int] $Drehung = 0,
    # Contact sheet over all photos: deciding page kind and rotation needs a glance, not a full image per photo.
    [Parameter(Mandatory, ParameterSetName = 'Uebersicht')] [switch] $Uebersicht,
    # Fractions (not pixels) because the photo is only ever seen scaled down, where original pixels cannot be read.
    # Strings, parsed below: `pwsh -File` hands "0,0,1,0.15" over as ONE string, and a [double[]] binding would read
    # it through the current culture (German: "0,15" is a decimal).
    [Parameter(Mandatory, ParameterSetName = 'Ausschnitt')] [string[]] $Ausschnitt
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if ($PSCmdlet.ParameterSetName -eq 'Ausschnitt') {
    # A new variable: the parameter keeps its [string[]] constraint, and a number assigned to it would turn back into text.
    $bereich = [double[]]@(($Ausschnitt -join ',') -split ',' | ForEach-Object {
        [double]::Parse($_.Trim(), [System.Globalization.CultureInfo]::InvariantCulture) })
    if ($bereich.Count -ne 4 -or ($bereich | Where-Object { $_ -lt 0 -or $_ -gt 1 })) {
        throw "-Ausschnitt braucht x,y,Breite,Höhe als vier Bruchteile 0–1, z. B. 0,0,1,0.15."
    }
}

$files = if (Test-Path $Quelle -PathType Container) {
    Get-ChildItem $Quelle -File | Where-Object Extension -in '.jpg', '.jpeg', '.png' | Sort-Object Name
} else { Get-Item $Quelle }

# Tiles, sheets and crops are pieces of a copyrighted book: they may only land in the repo's gitignored raw/ folder.
$rawRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../raw')).TrimEnd('\', '/')
$zielVoll = [System.IO.Path]::GetFullPath($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Ziel)).TrimEnd('\', '/')
$unterRaw = $zielVoll.StartsWith($rawRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
if (-not $unterRaw) { throw "Ziel '$zielVoll' liegt nicht unter '$rawRoot' – Buchausschnitte gehören nur nach raw/ (gitignored)." }
New-Item -ItemType Directory -Force $Ziel | Out-Null

$jpeg = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object MimeType -eq 'image/jpeg'
$quality = New-Object System.Drawing.Imaging.EncoderParameters 1
$quality.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality, [long]85)

# Loads one photo upright: EXIF first, then the manual rotation. Shared by tiles, contact sheet and crop.
function Open-Upright([string] $path) {
    $img = [System.Drawing.Image]::FromFile($path)
    # Phone photos carry their rotation as EXIF tag 0x0112; System.Drawing does not apply it on its own.
    if ($img.PropertyIdList -contains 0x0112) {
        $flip = switch ($img.GetPropertyItem(0x0112).Value[0]) {
            3 { 'Rotate180FlipNone' } 6 { 'Rotate90FlipNone' } 8 { 'Rotate270FlipNone' } default { $null }
        }
        if ($flip) { $img.RotateFlip([System.Drawing.RotateFlipType]::$flip) }
    }
    if ($Drehung -ne 0) { $img.RotateFlip([System.Drawing.RotateFlipType]::"Rotate$($Drehung)FlipNone") }
    return $img
}

# Draws the source rectangle of $img into a new bitmap of the given size.
function New-Scaled($img, [int] $x, [int] $y, [int] $w, [int] $h, [int] $outW, [int] $outH) {
    $bmp = New-Object System.Drawing.Bitmap $outW, $outH
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.DrawImage($img, (New-Object System.Drawing.Rectangle 0, 0, $outW, $outH),
        (New-Object System.Drawing.Rectangle $x, $y, $w, $h), 'Pixel')
    $g.Dispose()
    return $bmp
}

if ($PSCmdlet.ParameterSetName -eq 'Uebersicht') {
    $cols = 4; $rows = 3; $cellW = 400; $cellH = 300; $labelH = 28
    $perSheet = $cols * $rows
    $font = New-Object System.Drawing.Font ([System.Drawing.FontFamily]::GenericSansSerif), 14
    $all = @($files)
    for ($sheet = 0; $sheet * $perSheet -lt $all.Count; $sheet++) {
        $bmp = New-Object System.Drawing.Bitmap ($cols * $cellW), ($rows * ($cellH + $labelH))
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.Clear([System.Drawing.Color]::White)
        $g.InterpolationMode = 'HighQualityBicubic'
        for ($i = 0; $i -lt $perSheet -and $sheet * $perSheet + $i -lt $all.Count; $i++) {
            $n = $sheet * $perSheet + $i
            $cx = ($i % $cols) * $cellW; $cy = [Math]::Floor($i / $cols) * ($cellH + $labelH)
            $img = Open-Upright $all[$n].FullName
            try {
                $scale = [Math]::Min(($cellW - 8) / $img.Width, ($cellH - 8) / $img.Height)
                $w = [int]($img.Width * $scale); $h = [int]($img.Height * $scale)
                $g.DrawImage($img, (New-Object System.Drawing.Rectangle ($cx + [int](($cellW - $w) / 2)), ($cy + 4), $w, $h))
            } finally { $img.Dispose() }
            $g.DrawString(("{0}: {1}" -f ($n + 1), $all[$n].Name), $font, [System.Drawing.Brushes]::Black,
                [single]($cx + 6), [single]($cy + $cellH + 2))
        }
        $g.Dispose()
        $out = Join-Path $Ziel ("uebersicht-{0}.jpg" -f ($sheet + 1))
        $bmp.Save($out, $jpeg, $quality); $bmp.Dispose()
        Write-Output $out
    }
    $font.Dispose()
    return
}

foreach ($file in $files) {
    $img = Open-Upright $file.FullName
    try {
        if ($PSCmdlet.ParameterSetName -eq 'Ausschnitt') {
            $x = [int]($bereich[0] * $img.Width); $y = [int]($bereich[1] * $img.Height)
            $w = [Math]::Min($img.Width - $x, [int]($bereich[2] * $img.Width))
            $h = [Math]::Min($img.Height - $y, [int]($bereich[3] * $img.Height))
            if ($w -le 0 -or $h -le 0) { throw "Ausschnitt $($bereich -join ',') ist leer." }
            # Twice the size: faint lines (flash glare at the top of a page) become legible only when enlarged.
            $scale = [Math]::Min(2.0, $MaxKante / [Math]::Max($w, $h))
            $crop = New-Scaled $img $x $y $w $h ([int]($w * $scale)) ([int]($h * $scale))
            $out = Join-Path $Ziel ("{0}-ausschnitt.jpg" -f $file.BaseName)
            $crop.Save($out, $jpeg, $quality); $crop.Dispose()
            Write-Output $out
            continue
        }
        $tileW = [int]($img.Width / $Spalten); $tileH = [int]($img.Height / $Zeilen)
        $padW = [int]($tileW * $Ueberlappung); $padH = [int]($tileH * $Ueberlappung)
        for ($r = 0; $r -lt $Zeilen; $r++) {
            for ($c = 0; $c -lt $Spalten; $c++) {
                $x = [Math]::Max(0, $c * $tileW - $padW); $y = [Math]::Max(0, $r * $tileH - $padH)
                $w = [Math]::Min($img.Width - $x, $tileW + 2 * $padW); $h = [Math]::Min($img.Height - $y, $tileH + 2 * $padH)
                $scale = [Math]::Min(1.0, $MaxKante / [Math]::Max($w, $h))
                $tile = New-Scaled $img $x $y $w $h ([int]($w * $scale)) ([int]($h * $scale))
                $out = Join-Path $Ziel ("{0}-r{1}c{2}.jpg" -f $file.BaseName, ($r + 1), ($c + 1))
                $tile.Save($out, $jpeg, $quality); $tile.Dispose()
                Write-Output $out
            }
        }
    } finally { $img.Dispose() }
}
