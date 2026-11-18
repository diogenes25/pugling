# Pulls everything a WebUntis login can see into one JSON file per login (outside the repo: personal data).
# Friedrich: Altes Gymnasium (defaults). Theodor: -Server avs.webuntis.com -School avs.
param(
    [string]$Server = 'altesgymnasium-flensburg.webuntis.com',
    [string]$School = 'altesgymnasium-flensburg',
    [string]$OutDir = (Join-Path $env:TEMP 'pugling-webuntis')
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutDir | Out-Null
$base = "https://$Server/WebUntis"

function Rpc($session, $method, $params) {
    $body = @{ id = 'dump'; method = $method; params = $params; jsonrpc = '2.0' } | ConvertTo-Json -Compress -Depth 10
    $r = Invoke-RestMethod -Uri "$base/jsonrpc.do?school=$School" -Method Post -Body $body `
        -ContentType 'application/json; charset=utf-8' -WebSession $session
    if ($r.error) { return @{ error = $r.error } }
    return $r.result
}
function Get-Json($session, $url, $token) {
    try {
        $h = @{}
        if ($token) { $h['Authorization'] = "Bearer $token" }
        $resp = Invoke-WebRequest -Uri $url -WebSession $session -Headers $h -UseBasicParsing
        $text = [Text.Encoding]::UTF8.GetString($resp.RawContentStream.ToArray())
        return ($text | ConvertFrom-Json)
    } catch { return @{ error = $_.Exception.Message; url = $url } }
}

while ($true) {
    $label = Read-Host 'Name fuer diese Datei (z.B. theodor, friedrich; leer = Ende)'
    if ([string]::IsNullOrWhiteSpace($label)) { break }
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $user = Read-Host 'WebUntis-Benutzername'
    $sec  = Read-Host 'WebUntis-Passwort' -AsSecureString
    $pass = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec))
    try { $auth = Rpc $session 'authenticate' @{ user = $user; password = $pass; client = 'pugling-dump' } }
    finally { $pass = $null }
    if ($auth.error) { Write-Host ("Login FEHLER {0} {1}" -f $auth.error.code, $auth.error.message); continue }
    Write-Host ("Login OK, personType={0}" -f $auth.personType)

    $out = [ordered]@{ label = $label; fetchedAt = (Get-Date).ToString('s'); personType = $auth.personType; klasseId = $auth.klasseId }
    try {
        $tokResp = Invoke-WebRequest -Uri "$base/api/token/new" -WebSession $session -UseBasicParsing
        $token = $tokResp.Content.Trim()
        $app = Get-Json $session "$base/api/rest/view/v1/app/data" $token
        $out.appUser = @{ roles = $app.user.roles; students = @($app.user.students | ForEach-Object { @{ id = $_.id; displayName = $_.displayName } }) }
        $out.currentSchoolYear = $app.currentSchoolYear
        $out.holidays = $app.holidays

        # Students this login may see: itself (personType 5) or the guardian's children.
        $students = @()
        if ($auth.personType -eq 5) { $students = @($auth.personId) } else { $students = @($app.user.students | ForEach-Object { $_.id }) }
        $out.studentIds = $students

        $sy = Rpc $session 'getCurrentSchoolyear' @{}
        $out.schoolYearRpc = $sy
        $start = if ($sy.startDate) { [string]$sy.startDate } else { (Get-Date).AddMonths(-4).ToString('yyyyMMdd') }
        $end = (Get-Date).AddDays(70).ToString('yyyyMMdd')
        $out.range = @{ start = $start; end = $end }

        $out.subjects = Rpc $session 'getSubjects' @{}
        $out.homework = Get-Json $session "$base/api/homeworks/lessons?startDate=$start&endDate=$end" $token

        $out.perStudent = @{}
        foreach ($sid in $students) {
            $s = [ordered]@{}
            # Timetable of the current and the next week, with lesson texts where the school releases them.
            $wkStart = (Get-Date).AddDays(-[int](Get-Date).DayOfWeek + 1).ToString('yyyyMMdd')
            $wkEnd = (Get-Date).AddDays(-[int](Get-Date).DayOfWeek + 12).ToString('yyyyMMdd')
            $s.timetable = Rpc $session 'getTimetable' @{ options = @{
                element = @{ id = $sid; type = 5 }; startDate = $wkStart; endDate = $wkEnd
                showLsText = $true; showStudentgroup = $true; showLsNumber = $false; showSubstText = $true; showInfo = $true; showBooking = $true
                subjectFields = @('id', 'name', 'longname'); teacherFields = @('id', 'name'); klasseFields = @('id', 'name') } }
            $s.exams = Get-Json $session "$base/api/exams?studentId=$sid&klasseId=-1&startDate=$start&endDate=$end&withGrades=true" $token
            $isoStart = "{0}-{1}-{2}T00:00:00" -f $wkStart.Substring(0,4), $wkStart.Substring(4,2), $wkStart.Substring(6,2)
            $isoEnd = "{0}-{1}-{2}T23:59:59" -f $wkEnd.Substring(0,4), $wkEnd.Substring(4,2), $wkEnd.Substring(6,2)
            $s.timetableRest = Get-Json $session "$base/api/rest/view/v1/timetable/entries?start=$isoStart&end=$isoEnd&format=4&resourceType=STUDENT&resources=$sid&timetableType=MY_TIMETABLE" $token
            $out.perStudent["$sid"] = $s
        }
    }
    catch { $out.fatal = $_.Exception.Message; Write-Host ("FEHLER {0}" -f $_.Exception.Message) }
    finally { $null = Rpc $session 'logout' @{} }

    $file = Join-Path $OutDir ("webuntis-{0}.json" -f $label)
    $json = $out | ConvertTo-Json -Depth 30
    [IO.File]::WriteAllText($file, $json, (New-Object Text.UTF8Encoding($false)))
    $hwCount = @($out.homework.data.homeworks).Count
    Write-Host ("Gespeichert: {0}  (Schueler: {1}, Hausaufgaben: {2})" -f $file, @($out.studentIds).Count, $hwCount)
}
Write-Host 'Fertig.'
