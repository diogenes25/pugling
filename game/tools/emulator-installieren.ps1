# Vokabel-Türme on the Android emulator (AVD pl-test, x86_64): exports the emulator build, installs it, routes the
# emulator's 127.0.0.1:5280 to the throwaway API on this PC (adb reverse) and starts the app. The emulator build is a
# separate preset because a two-architecture APK (~200 MB) does not fit into the AVD's storage as an update.
# Start the emulator first:  & "${env:ProgramFiles(x86)}\Android\android-sdk\emulator\emulator.exe" -avd pl-test
$adb = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$godot = Join-Path $env:LOCALAPPDATA 'Programs\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
$game = Resolve-Path (Join-Path $PSScriptRoot '..')
$apk = Join-Path $game 'build\pugling-tower-emulator.apk'

& $godot --headless --path $game --export-debug Android-Emulator $apk
# Uninstall first: an in-place update needs room for two copies, which the AVD does not have.
& $adb shell pm uninstall de.pugling.tower | Out-Null
& $adb install $apk
if ($LASTEXITCODE -ne 0) { Write-Warning 'Installation fehlgeschlagen.'; return }
& $adb reverse tcp:5280 tcp:5280
& $adb shell monkey -p de.pugling.tower -c android.intent.category.LAUNCHER 1 | Out-Null
'--- App gestartet; Log folgt (Strg+C beendet) ---'
& $adb logcat -s godot
