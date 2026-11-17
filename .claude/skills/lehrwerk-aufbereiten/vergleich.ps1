<#
Altes und neues Foto derselben Seite uebereinander legen (oben alt, unten neu, rote Trennlinie) – oder nur
einen Ausschnitt eines einzelnen Fotos vergroessern, um eine blasse Stelle zu lesen.

  pwsh -File vergleich.ps1 -Alt <alt.jpg> -Neu <neu.jpg> -Ziel <scratchpad>/s230.jpg
  pwsh -File vergleich.ps1 -Alt <alt.jpg> -Neu <neu.jpg> -DrehNeu 270 -Ziel ...        # neues Foto liegt quer
  pwsh -File vergleich.ps1 -Alt <foto.jpg> -X0 0.5 -Y0 0 -X1 1 -Y1 0.5 -Ziel ...       # nur rechts oben, ohne -Neu

Ausschnitt als Anteile (0..1) des gedrehten Fotos. Das Ziel liegt im Scratchpad oder unter raw/ (gitignored):
die Bilder sind Ausschnitte des Buchs und kommen nie ins Repo.
#>
param(
    [Parameter(Mandatory)] [string] $Alt,
    [string] $Neu,
    [ValidateSet(0, 90, 180, 270)] [int] $DrehAlt = 0,
    [ValidateSet(0, 90, 180, 270)] [int] $DrehNeu = 0,
    [Parameter(Mandatory)] [string] $Ziel,
    [int] $Breite = 1400,
    [double] $X0 = 0, [double] $Y0 = 0, [double] $X1 = 1, [double] $Y1 = 1
)
Add-Type -AssemblyName System.Drawing

function Load-Photo([string] $path, [int] $turn) {
    $img = [System.Drawing.Image]::FromFile((Resolve-Path $path))
    # Phone photos carry their rotation as EXIF tag 0x0112; System.Drawing does not apply it on its own.
    if ($img.PropertyIdList -contains 0x0112) {
        switch ($img.GetPropertyItem(0x0112).Value[0]) {
            3 { $img.RotateFlip('Rotate180FlipNone') } 6 { $img.RotateFlip('Rotate90FlipNone') } 8 { $img.RotateFlip('Rotate270FlipNone') }
        }
    }
    if ($turn -ne 0) { $img.RotateFlip([System.Drawing.RotateFlipType]::("Rotate{0}FlipNone" -f $turn)) }
    $rect = New-Object System.Drawing.Rectangle ([int]($img.Width * $X0)), ([int]($img.Height * $Y0)),
        ([int]($img.Width * ($X1 - $X0))), ([int]($img.Height * ($Y1 - $Y0)))
    $crop = ([System.Drawing.Bitmap] $img).Clone($rect, $img.PixelFormat)
    $img.Dispose()
    return $crop
}

$parts = @(Load-Photo $Alt $DrehAlt)
if ($Neu) { $parts += Load-Photo $Neu $DrehNeu }
$heights = $parts | ForEach-Object { [int]($_.Height * $Breite / $_.Width) }
$gap = if ($parts.Count -gt 1) { 10 } else { 0 }
$out = New-Object System.Drawing.Bitmap $Breite, (($heights | Measure-Object -Sum).Sum + $gap)
$g = [System.Drawing.Graphics]::FromImage($out)
$g.Clear([System.Drawing.Color]::Red)
$g.InterpolationMode = 'HighQualityBicubic'
$y = 0
for ($i = 0; $i -lt $parts.Count; $i++) { $g.DrawImage($parts[$i], 0, $y, $Breite, $heights[$i]); $y += $heights[$i] + $gap }
$jpeg = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object MimeType -eq 'image/jpeg'
$quality = New-Object System.Drawing.Imaging.EncoderParameters 1
$quality.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality, [long]85)
$out.Save($Ziel, $jpeg, $quality)
$g.Dispose(); $out.Dispose(); $parts | ForEach-Object { $_.Dispose() }
Write-Output $Ziel
