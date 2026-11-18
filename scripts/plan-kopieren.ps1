#requires -Version 7
<#
.SYNOPSIS
    Kopiert einen Lehrplan mit allen Positionen auf ein anderes Kind – etwa Friedrichs Plan auf den Testjungen,
    um ihn aus Sohn-Sicht durchzuspielen.

.DESCRIPTION
    Neuer Plan: gleicher Titel mit Zusatz, gleiches Fach, gleiche Unit, gleiche Laufzeit (Start und Ende wie das
    Original), aktiv. Jede Position wird mit allen Einstellungen übernommen (Stufe, Pflicht, Dosis, Leiter,
    Schwelle, Münzen …), in derselben Reihenfolge. Lernstand wird nicht kopiert – das Zielkind fängt bei null an.

    Ein aktiver Plan je Kind und Fach (B-539): Hat das Zielkind schon einen aktiven Plan im selben Fach, schaltet
    der neue ihn ab. Das Skript sagt vorher, welcher es wäre, und bricht ab, wenn der Titel schon existiert.
    Die PIN des Vater-Kontos wird verdeckt abgefragt und nirgends gespeichert.

.EXAMPLE
    ./scripts/plan-kopieren.ps1 -PlanId 15 -ChildId 4
#>
param(
    [Parameter(Mandatory)][int]$PlanId,
    [Parameter(Mandatory)][int]$ChildId,
    [string]$Suffix = ' (Test)',
    [string]$BaseUrl = 'http://127.0.0.1:5200',
    [string]$Pin
)

$ErrorActionPreference = 'Stop'
$api = "$BaseUrl/api/v1"
if (-not $Pin) { $Pin = Read-Host -MaskInput 'PIN des Vater-Kontos (Papa)' }

function Invoke-Api([string]$Method, [string]$Path, $Body) {
    $a = @{ Method = $Method; Uri = "$api/$Path"; Headers = $script:headers }
    if ($null -ne $Body) {
        $a.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 8))
        $a.ContentType = 'application/json; charset=utf-8'
    }
    try { Invoke-RestMethod @a }
    catch { throw "$Method $Path fehlgeschlagen: $($_.ErrorDetails.Message ?? $_.Exception.Message)" }
}

$script:headers = @{}
$login = Invoke-Api POST 'auth/adult' @{ adultId = 1; pin = $Pin }
$script:headers = @{ Authorization = "Bearer $($login.token)" }
Write-Host "Angemeldet als $($login.name)."

$source = Invoke-Api GET "supervisor/study-plans/$PlanId" $null
$positions = @(Invoke-Api GET "supervisor/study-plans/$PlanId/positions?pageSize=200" $null) | Sort-Object order
$title = "$($source.title)$Suffix"

$existing = @(Invoke-Api GET "supervisor/study-plans?childId=$ChildId&pageSize=200" $null)
if ($existing.title -contains $title) { throw "Kind $ChildId hat schon einen Plan '$title' – Abbruch, nichts angelegt." }
$replaced = $existing | Where-Object { $_.active -and $_.subjectId -eq $source.subjectId }
foreach ($p in $replaced) { Write-Host "Wird abgeschaltet (gleiches Fach): Plan $($p.id) $($p.title)" }

# Sicherung der echten Datenbank, bevor etwas geschrieben wird (nur am Heimnetz-Server).
if ($BaseUrl -match ':5200$') {
    $db = Join-Path $PSScriptRoot '..\backend\Pugling.Api\pugling.db'
    $backup = "$db.bak-vor-plankopie-$PlanId-kind$ChildId-$(Get-Date -Format yyyyMMdd)"
    if (-not (Test-Path $backup)) { Copy-Item $db $backup }
    Write-Host "Sicherung: $backup"
}

$days = ([datetime]$source.endDate - [datetime]$source.startDate).Days + 1
$plan = Invoke-Api POST 'supervisor/study-plans' @{
    childId = $ChildId; title = $title; subjectId = $source.subjectId; seriesUnitId = $source.seriesUnitId
    startDate = $source.startDate; durationDays = $days; description = $source.description; active = $true
}
Write-Host ''
Write-Host "Plan $($plan.id) (Kind $ChildId): $($plan.title)  $($plan.startDate) bis $($plan.endDate)"

# Alle Einstellungen, die eine Position beim Anlegen annimmt; die Antwort trägt dieselben Namen.
$fields = 'exerciseId', 'stage', 'itemCount', 'scope', 'cadence', 'orderStrategy', 'goalThreshold',
    'requireTypedTest', 'useLeitner', 'maxBox', 'boxIntervalDays', 'stageSchedule', 'pointsGoalMet', 'penaltyCoins',
    'newContentPoints', 'comboThreshold', 'comboBonusPoints', 'speedThresholdSeconds', 'speedBonusPoints',
    'timeSlots', 'newItemsPerDay', 'forgivingDemotion', 'remediation'
foreach ($src in $positions) {
    $body = [ordered]@{}
    foreach ($f in $fields) {
        $v = $src.$f
        if ($null -ne $v) { $body[$f] = $v }
    }
    $r = Invoke-Api POST "supervisor/study-plans/$($plan.id)/positions" $body
    $kind = if ($r.cadence -eq 'None') { 'Kür    ' } else { "Pflicht $($r.cadence)" }
    Write-Host ("  {0,4} <- {1,4}  {2,-15} {3}" -f $r.id, $src.id, $kind, $r.exerciseTitle)
}
