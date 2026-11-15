<#
.SYNOPSIS
  Zerlegt Fotos dicht bedruckter Buchseiten in überlappende Kacheln, damit jede Zeile groß genug gelesen wird.

.EXAMPLE
  pwsh -File kacheln.ps1 -Quelle "raw/france/Decouvertes/Serie bleue 2/IMG_1.jpg" -Ziel "raw/france/decouvertes-2-pugling/kacheln" -Zeilen 3

  Ein Bild oder ein Ordner (alle .jpg/.jpeg/.png nach Namen). Die Kacheln heißen
  <name>-<zeilen>x<spalten>-d<drehung>-r<zeile>c<spalte>.jpg.
  Liegt das Buch auf dem Kopf: -Drehen 180 (oder 90/270 im Uhrzeigersinn); -Zeilen 1 -Spalten 1 dreht nur.
  Das Ziel muss unter raw/ liegen (gitignored): Kacheln sind Ausschnitte des Buchs und kommen nie ins Repo.
#>
param(
    [Parameter(Mandatory)] [string] $Quelle,
    [Parameter(Mandatory)] [string] $Ziel,
    [int] $Zeilen = 2,
    [int] $Spalten = 1,
    # Overlap between neighbouring tiles, so a line cut at a border is whole in one of them.
    [double] $Ueberlappung = 0.08,
    # Longest edge of a tile; larger tiles are scaled down before the model sees them anyway.
    [int] $MaxKante = 1800,
    # Extra clockwise rotation after EXIF, for books photographed upside down or sideways (0, 90, 180, 270).
    [ValidateSet(0, 90, 180, 270)] [int] $Drehen = 0
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$files = if (Test-Path $Quelle -PathType Container) {
    Get-ChildItem $Quelle -File | Where-Object Extension -in '.jpg', '.jpeg', '.png' | Sort-Object Name
} else { Get-Item $Quelle }
New-Item -ItemType Directory -Force $Ziel | Out-Null

$jpeg = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object MimeType -eq 'image/jpeg'
$quality = New-Object System.Drawing.Imaging.EncoderParameters 1
$quality.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality, [long]85)

foreach ($file in $files) {
    $img = [System.Drawing.Image]::FromFile($file.FullName)
    try {
        # Phone photos carry their rotation as EXIF tag 0x0112; System.Drawing does not apply it on its own.
        if ($img.PropertyIdList -contains 0x0112) {
            $flip = switch ($img.GetPropertyItem(0x0112).Value[0]) {
                3 { 'Rotate180FlipNone' } 6 { 'Rotate90FlipNone' } 8 { 'Rotate270FlipNone' } default { $null }
            }
            if ($flip) { $img.RotateFlip([System.Drawing.RotateFlipType]::$flip) }
        }
        if ($Drehen -ne 0) { $img.RotateFlip([System.Drawing.RotateFlipType]::("Rotate{0}FlipNone" -f $Drehen)) }
        $tileW = [int]($img.Width / $Spalten); $tileH = [int]($img.Height / $Zeilen)
        $padW = [int]($tileW * $Ueberlappung); $padH = [int]($tileH * $Ueberlappung)
        for ($r = 0; $r -lt $Zeilen; $r++) {
            for ($c = 0; $c -lt $Spalten; $c++) {
                $x = [Math]::Max(0, $c * $tileW - $padW); $y = [Math]::Max(0, $r * $tileH - $padH)
                $w = [Math]::Min($img.Width - $x, $tileW + 2 * $padW); $h = [Math]::Min($img.Height - $y, $tileH + 2 * $padH)
                $scale = [Math]::Min(1.0, $MaxKante / [Math]::Max($w, $h))
                $tile = New-Object System.Drawing.Bitmap ([int]($w * $scale)), ([int]($h * $scale))
                $g = [System.Drawing.Graphics]::FromImage($tile)
                $g.InterpolationMode = 'HighQualityBicubic'
                $g.DrawImage($img, (New-Object System.Drawing.Rectangle 0, 0, $tile.Width, $tile.Height),
                    (New-Object System.Drawing.Rectangle $x, $y, $w, $h), 'Pixel')
                $g.Dispose()
                # Grid and rotation in the name, so a finer second pass never overwrites a coarser one.
                $out = Join-Path $Ziel ("{0}-{1}x{2}-d{3}-r{4}c{5}.jpg" -f $file.BaseName, $Zeilen, $Spalten, $Drehen, ($r + 1), ($c + 1))
                $tile.Save($out, $jpeg, $quality); $tile.Dispose()
                Write-Output $out
            }
        }
    } finally { $img.Dispose() }
}
