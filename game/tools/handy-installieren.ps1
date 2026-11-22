# B-850 spike: installs the debug APK on the USB-connected phone, routes the phone's 127.0.0.1:5280 to the
# throwaway API on this PC (adb reverse, so neither the firewall nor the real instance on :5200 is involved),
# starts the app and follows its log. Stop with Ctrl-C.
$adb = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$apk = Join-Path $PSScriptRoot '..\build\pugling-tower.apk'

& $adb devices -l
& $adb install -r $apk
if ($LASTEXITCODE -ne 0) { Write-Warning 'Installation fehlgeschlagen.'; return }
& $adb reverse tcp:5280 tcp:5280
& $adb logcat -c
& $adb shell monkey -p de.pugling.tower -c android.intent.category.LAUNCHER 1 | Out-Null
'--- App gestartet; Log folgt (Strg+C beendet) ---'
& $adb logcat -s godot
