# Installs the debug APK on the USB-connected phone and starts the app.
#
# The Xiaomi phone refuses `adb install` (INSTALL_FAILED_USER_RESTRICTED); pushing the file and installing it with
# the phone's own package manager works. With -ApiPort the phone's 127.0.0.1:5280 (the login's "Emulator" button) is
# routed over the cable to a throwaway API on this PC - never to the home server on :5200, which holds the real data.
#
#   .\tools\handy-installieren.ps1                 # install and start; the app talks to the home server by default
#   .\tools\handy-installieren.ps1 -ApiPort 5280   # also route the phone's :5280 to a throwaway API on this port
param(
    [string]$Serial = '8x5ho7euzlyhdajf',
    [int]$ApiPort = 0
)

$adb = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$apk = Join-Path $PSScriptRoot '..\build\pugling-tower.apk'
if (-not (Test-Path $apk)) { Write-Warning "Keine APK unter $apk - erst exportieren."; return }

& $adb -s $Serial push $apk /data/local/tmp/pt.apk
& $adb -s $Serial shell pm install -r /data/local/tmp/pt.apk
if ($LASTEXITCODE -ne 0) { Write-Warning 'Installation fehlgeschlagen.'; return }
if ($ApiPort -gt 0) { & $adb -s $Serial reverse tcp:5280 "tcp:$ApiPort" }
& $adb -s $Serial shell monkey -p de.pugling.tower -c android.intent.category.LAUNCHER 1 | Out-Null
'--- App gestartet. Log lesen:  adb shell run-as de.pugling.tower cat files/logs/godot.log ---'
