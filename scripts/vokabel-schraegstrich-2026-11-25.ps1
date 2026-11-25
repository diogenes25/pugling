#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (B-626 AK 7): zwei Vokabeln, deren Schrägstrich mehrwortige Seiten trennt, werden zu zwei Bedeutungen.

.DESCRIPTION
    Ein Schrägstrich ohne Leerzeichen verbindet je ein Wort (B-626 Entscheidung 3). Bei zwei Einträgen stehen links und
    rechts mehrere Wörter – das lässt sich so nicht zerlegen. Mit Leerzeichen um den Schrägstrich sind es zwei Bedeutungen,
    jede zählt für sich (B-462):

      122  towards the station/Mr Bell   ->  towards the station / towards Mr Bell
      953  (to) pose a risk/a problem    ->  (to) pose a risk / (to) pose a problem

    Geändert wird nur das Wort, Übersetzung und Lernstand bleiben. Vorher wird geprüft, dass beide Einträge noch den
    alten Text tragen – sonst bricht das Skript ab, ohne etwas zu ändern. Die PIN wird verdeckt abgefragt und nirgends
    gespeichert.

.PARAMETER BaseUrl
    Vorgabe ist der Heimnetz-Server. Für eine Probe gegen eine Kopie: -BaseUrl http://127.0.0.1:<port>.

.PARAMETER Pin
    Nur für die Probe gegen eine Kopie mit Test-PIN. Am echten Server weglassen – dann fragt das Skript verdeckt.
#>
param(
    [string]$BaseUrl = 'http://127.0.0.1:5200',
    [string]$Pin
)

$ErrorActionPreference = 'Stop'
$api = "$BaseUrl/api/v1"
if (-not $Pin) { $Pin = Read-Host -MaskInput 'PIN des Vater-Kontos (Papa)' }

function Invoke-Api([string]$Method, [string]$Path, $Body) {
    $a = @{ Method = $Method; Uri = "$api/$Path"; Headers = $script:headers }
    if ($null -ne $Body) {
        $a.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 4))
        $a.ContentType = 'application/json; charset=utf-8'
    }
    try { $r = Invoke-RestMethod @a; $r }
    catch { throw "$Method $Path fehlgeschlagen: $($_.ErrorDetails.Message ?? $_.Exception.Message)" }
}

$script:headers = @{}
$login = Invoke-Api POST 'auth/adult' @{ adultId = 1; pin = $Pin }
$script:headers = @{ Authorization = "Bearer $($login.token)" }
Write-Host "Angemeldet als $($login.name)."

# String keys: an [ordered] dictionary indexes an int key by position, not by key.
$changes = [ordered]@{
    '122' = @{ Old = 'towards the station/Mr Bell'; New = 'towards the station / towards Mr Bell' }
    '953' = @{ Old = '(to) pose a risk/a problem'; New = '(to) pose a risk / (to) pose a problem' }
}

# Erst alles prüfen, dann schreiben.
foreach ($id in $changes.Keys) {
    $v = Invoke-Api GET "creator/vocabulary/$id" $null
    if ($v.word -cne $changes[$id].Old) {
        throw "Vokabel $id heißt '$($v.word)', erwartet '$($changes[$id].Old)' – Abbruch, nichts geändert."
    }
}

foreach ($id in $changes.Keys) {
    $v = Invoke-Api PATCH "creator/vocabulary/$id" @{ word = $changes[$id].New }
    Write-Host ("{0,4}  {1}  ->  {2}   ({3})" -f $id, $changes[$id].Old, $v.word, $v.translation)
}
Write-Host 'Fertig.'
