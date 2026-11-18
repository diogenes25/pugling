#requires -Version 7
<#
.SYNOPSIS
    Gemeinsame Bausteine der Einmal-Skripte, die über die API Übungen und Lehrpläne anlegen.

.DESCRIPTION
    Wird per Dot-Sourcing geladen (`. "$PSScriptRoot\_plan-api.ps1"`), danach `Connect-PuglingApi`.
    - Invoke-Api:            ein Aufruf mit JSON-Body, Fehler mit der ProblemDetails-Meldung.
    - Get-ApiList:           eine Liste vollständig, seitenweise über skip/take und X-Total-Count.
    - Backup-RealDatabase:   sichert die echte pugling.db samt -wal, wenn das Ziel der Heimnetz-Server ist.
    - Get-OrNewExercise:     legt eine Übung nur an, wenn es in der Unit noch keine mit dem Titel gibt.
    - Assert-PlansResumable: bricht ab, wenn ein abgebrochener Lauf einen inaktiven Plan hinterlassen hat.
    - New-StudyPlan:         legt einen Plan inaktiv an, hängt die Positionen an und schaltet ihn erst dann aktiv.
                             Bis dahin bleibt der bisherige Plan des Fachs aktiv (B-539 schaltet erst beim Aktivieren um).
#>

$script:PuglingApi = $null
$script:PuglingHeaders = @{}
# Der Heimnetz-Server mit der echten DB der Söhne läuft auf diesem Port, die Entwicklungs-Server nie (.claude/launch.json).
$script:HeimnetzPort = 5200

function Invoke-Api([string]$Method, [string]$Path, $Body) {
    $request = @{ Method = $Method; Uri = "$script:PuglingApi/$Path"; Headers = $script:PuglingHeaders }
    if ($null -ne $Body) {
        $request.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 10))
        $request.ContentType = 'application/json; charset=utf-8'
    }
    try { Invoke-RestMethod @request }
    catch { throw "$Method $Path fehlgeschlagen: $($_.ErrorDetails.Message ?? $_.Exception.Message)" }
}

function Get-ApiList([string]$Path) {
    $take = 500
    $items = [System.Collections.Generic.List[object]]::new()
    $separator = if ($Path.Contains('?')) { '&' } else { '?' }
    while ($true) {
        $uri = "$script:PuglingApi/$Path${separator}skip=$($items.Count)&take=$take"
        try { $page = Invoke-RestMethod -Uri $uri -Headers $script:PuglingHeaders -ResponseHeadersVariable responseHeaders }
        catch { throw "GET $Path fehlgeschlagen: $($_.ErrorDetails.Message ?? $_.Exception.Message)" }
        foreach ($item in @($page)) { $items.Add($item) }
        $total = [int]($responseHeaders['X-Total-Count'] | Select-Object -First 1)
        if ($items.Count -ge $total -or @($page).Count -eq 0) { break }
    }
    $items.ToArray()
}

function Test-HeimnetzTarget([string]$BaseUrl) {
    ([Uri]$BaseUrl).Port -eq $script:HeimnetzPort
}

function Connect-PuglingApi([string]$BaseUrl, [string]$Pin, [int]$AdultId = 1) {
    $script:PuglingApi = "$($BaseUrl.TrimEnd('/'))/api/v1"
    if (-not $Pin) { $Pin = Read-Host -MaskInput 'PIN des Vater-Kontos (Papa)' }
    $script:PuglingHeaders = @{}
    $login = Invoke-Api POST 'auth/adult' @{ adultId = $AdultId; pin = $Pin }
    $script:PuglingHeaders = @{ Authorization = "Bearer $($login.token)" }
    Write-Host "Angemeldet als $($login.name)."
}

function Backup-RealDatabase([string]$BaseUrl, [string]$Tag) {
    if (-not (Test-HeimnetzTarget $BaseUrl)) { return }
    $db = Join-Path $PSScriptRoot '..\backend\Pugling.Api\pugling.db'
    $backup = "$db.bak-vor-$Tag"
    if (Test-Path $backup) { Write-Host "Sicherung gibt es schon: $backup"; return }
    # Der Server läuft und schreibt im WAL-Modus: was noch nicht zurückgeschrieben ist, steht nur in der -wal.
    # Ohne sie fehlt der Sicherung der jüngste Lernstand. Zurückspielen heißt: beide Dateien bei gestopptem Server.
    Copy-Item $db $backup
    if (Test-Path "$db-wal") { Copy-Item "$db-wal" "$backup-wal" }
    Write-Host "Sicherung: $backup (+ -wal)"
}

function Get-OrNewExercise([int]$SeriesId, [int]$UnitId, [string]$Route, [hashtable]$Body) {
    $existing = Get-ApiList "creator/exercises?seriesUnitId=$UnitId" | Where-Object title -EQ $Body.title | Select-Object -First 1
    if ($existing) {
        Write-Host "Übung $($existing.id) gibt es schon: $($existing.title)"
        return Invoke-Api GET "creator/exercises/$($existing.id)" $null
    }
    $e = Invoke-Api POST "creator/textbook-series/$SeriesId/units/$UnitId/$Route" $Body
    Write-Host "Übung $($e.id): $($e.title)"
    $e
}

# Ein Plan mit dem Titel, der aktiv ist, gilt als fertig angelegt und wird übersprungen. Ein inaktiver stammt aus
# einem abgebrochenen Lauf (New-StudyPlan aktiviert erst am Ende) – den löscht nur ein Mensch.
function Assert-PlansResumable([int[]]$ChildIds, [string[]]$Titles) {
    foreach ($childId in $ChildIds) {
        foreach ($p in (Get-ApiList "supervisor/study-plans?childId=$childId")) {
            if ($Titles -contains $p.title -and -not $p.active) {
                throw "Plan '$($p.title)' (Id $($p.id), Kind $childId) ist inaktiv – Rest eines abgebrochenen Laufs oder abgelöst. Prüfen und ggf. löschen (DELETE supervisor/study-plans/$($p.id)), dann neu starten."
            }
        }
    }
}

function Get-ActivePlanByTitle([int]$ChildId, [string]$Title) {
    Get-ApiList "supervisor/study-plans?childId=$ChildId" | Where-Object { $_.title -eq $Title -and $_.active } | Select-Object -First 1
}

function New-StudyPlan([hashtable]$Plan, [object[]]$Positions) {
    $existing = Get-ActivePlanByTitle $Plan.childId $Plan.title
    if ($existing) {
        Write-Host ''
        Write-Host "Plan $($existing.id) (Kind $($Plan.childId)) gibt es schon aktiv: $($existing.title) – übersprungen."
        return
    }
    $body = $Plan.Clone(); $body.active = $false
    $created = Invoke-Api POST 'supervisor/study-plans' $body
    Write-Host ''
    Write-Host "Plan $($created.id) (Kind $($Plan.childId)): $($created.title)  $($created.startDate) bis $($created.endDate)"
    $results = foreach ($pos in $Positions) {
        try { Invoke-Api POST "supervisor/study-plans/$($created.id)/positions" $pos }
        catch { throw "$_`nPlan $($created.id) bleibt inaktiv, der bisherige Plan des Fachs aktiv. Plan $($created.id) löschen und neu starten." }
    }
    $null = Invoke-Api PATCH "supervisor/study-plans/$($created.id)" @{ active = $true }
    $results
}
