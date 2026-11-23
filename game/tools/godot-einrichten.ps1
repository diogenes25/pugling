# One-time setup of the Godot editor for the Android export of Vokabel-Türme: export templates, debug keystore and the
# Java/Android SDK paths in the editor settings. Without them `--export-debug` stops with "No export template found"
# and "A valid Java SDK path is required".
#
# The templates are taken from game/build/godot-setup (gitignored) when they are there; otherwise download
# Godot_v4.7.2-stable_mono_export_templates.tpz from godotengine.org and install them via the editor
# (Editor > Manage Export Templates).
param(
    [string]$JavaSdk = 'C:\Program Files\Android\openjdk\jdk-21.0.8',
    [string]$AndroidSdk = 'C:\Program Files (x86)\Android\android-sdk'
)

$ErrorActionPreference = 'Stop'
$godotData = Join-Path $env:APPDATA 'Godot'
$templates = Join-Path $godotData 'export_templates\4.7.2.stable.mono'
$keystore = Join-Path $godotData 'keystores\debug.keystore'
$settings = Join-Path $godotData 'editor_settings-4.7.tres'
$staged = Join-Path $PSScriptRoot '..\build\godot-setup'

foreach ($path in $JavaSdk, $AndroidSdk) {
    if (-not (Test-Path $path)) { throw "Nicht gefunden: $path – Pfad als Parameter angeben." }
}

# 1. Export templates (only the Android ones are needed).
New-Item -ItemType Directory -Force $templates | Out-Null
foreach ($file in 'android_debug.apk', 'android_release.apk', 'version.txt') {
    $target = Join-Path $templates $file
    if (Test-Path $target) { continue }
    $source = Join-Path $staged $file
    if (-not (Test-Path $source)) { throw "Exportvorlage fehlt: $file – siehe Kopf dieses Skripts." }
    Copy-Item $source $target
    "Vorlage kopiert: $file"
}

# 2. Debug keystore: the staged one, or a fresh one from the JDK's keytool.
if (-not (Test-Path $keystore)) {
    New-Item -ItemType Directory -Force (Split-Path $keystore) | Out-Null
    $stagedKey = Join-Path $staged 'debug.keystore'
    if (Test-Path $stagedKey) {
        Copy-Item $stagedKey $keystore
    } else {
        & (Join-Path $JavaSdk 'bin\keytool.exe') -genkeypair -keystore $keystore -storepass android -alias androiddebugkey `
            -keypass android -keyalg RSA -keysize 2048 -validity 10000 -dname 'CN=Android Debug,O=Android,C=US'
    }
    "Keystore angelegt: $keystore"
}

# 3. Editor settings: set or replace the five Android lines, keep everything else.
$wanted = [ordered]@{
    'export/android/java_sdk_path'        = $JavaSdk -replace '\\', '/'
    'export/android/android_sdk_path'     = $AndroidSdk -replace '\\', '/'
    'export/android/debug_keystore'       = $keystore -replace '\\', '/'
    'export/android/debug_keystore_user'  = 'androiddebugkey'
    'export/android/debug_keystore_pass'  = 'android'
}
$lines = @(if (Test-Path $settings) { Get-Content $settings } else { '[gd_resource type="EditorSettings" format=3]', '', '[resource]' })
if (-not ($lines -contains '[resource]')) { $lines += '', '[resource]' }
foreach ($key in $wanted.Keys) {
    $line = "$key = `"$($wanted[$key])`""
    $lines = @($lines | Where-Object { -not $_.StartsWith("$key = ") }) + $line
}
Set-Content -Path $settings -Value $lines -Encoding utf8
"Editor-Einstellungen gesetzt: $settings"
'Fertig. Jetzt: .\game\tools\emulator-installieren.ps1'
