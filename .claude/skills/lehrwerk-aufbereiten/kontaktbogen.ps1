<#
.SYNOPSIS
  Legt Vorschaubilder mehrerer Fotos auf einen Kontaktbogen, damit man vor dem Lesen Ausrichtung, Seitenart und
  Reihenfolge eines ganzen Fotoordners auf einen Blick sieht.

.EXAMPLE
  pwsh -File kontaktbogen.ps1 -Quelle "raw/latin" -Ziel "raw/latin/viva-1-pugling/kacheln/uebersicht"

  Je Bogen -ProBogen Fotos (Vorgabe 8) in 4 Spalten; unter jedem Bild der Dateiname. Nur EXIF-Drehung wird
  angewandt – wie ein Foto für die Kacheln gedreht werden muss, sieht man genau hier. Ziel muss unter raw/ liegen.
#>
param(
    [Parameter(Mandatory)] [string] $Quelle,
    [Parameter(Mandatory)] [string] $Ziel,
    [int] $ProBogen = 8,
    [int] $Spalten = 4,
    [int] $Kante = 700
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$files = Get-ChildItem $Quelle -File | Where-Object Extension -in '.jpg', '.jpeg', '.png' | Sort-Object Name
New-Item -ItemType Directory -Force $Ziel | Out-Null
$label = 28
$font = New-Object System.Drawing.Font 'Arial', 16
$rows = [Math]::Ceiling($ProBogen / $Spalten)

for ($start = 0; $start -lt $files.Count; $start += $ProBogen) {
    $chunk = $files[$start..([Math]::Min($files.Count, $start + $ProBogen) - 1)]
    $sheet = New-Object System.Drawing.Bitmap ($Spalten * $Kante), ($rows * ($Kante + $label))
    $g = [System.Drawing.Graphics]::FromImage($sheet)
    $g.Clear([System.Drawing.Color]::White)
    $g.InterpolationMode = 'HighQualityBicubic'
    for ($i = 0; $i -lt $chunk.Count; $i++) {
        $img = [System.Drawing.Image]::FromFile($chunk[$i].FullName)
        try {
            # Same EXIF handling as kacheln.ps1, so what you see here is what -Drehen 0 would tile.
            if ($img.PropertyIdList -contains 0x0112) {
                $flip = switch ($img.GetPropertyItem(0x0112).Value[0]) {
                    3 { 'Rotate180FlipNone' } 6 { 'Rotate90FlipNone' } 8 { 'Rotate270FlipNone' } default { $null }
                }
                if ($flip) { $img.RotateFlip([System.Drawing.RotateFlipType]::$flip) }
            }
            $scale = [Math]::Min($Kante / $img.Width, $Kante / $img.Height)
            $w = [int]($img.Width * $scale); $h = [int]($img.Height * $scale)
            $x = ($i % $Spalten) * $Kante + [int](($Kante - $w) / 2)
            $y = [Math]::Floor($i / $Spalten) * ($Kante + $label)
            $g.DrawImage($img, $x, $y, $w, $h)
            $g.DrawString($chunk[$i].Name, $font, [System.Drawing.Brushes]::Black, ($i % $Spalten) * $Kante + 4, $y + $Kante + 2)
        } finally { $img.Dispose() }
    }
    $g.Dispose()
    $out = Join-Path $Ziel ("kontaktbogen-{0:D2}.jpg" -f ([int]($start / $ProBogen) + 1))
    $sheet.Save($out, [System.Drawing.Imaging.ImageFormat]::Jpeg); $sheet.Dispose()
    Write-Output $out
}
