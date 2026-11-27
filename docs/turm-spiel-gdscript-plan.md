---
tags: [bereich/turmspiel, typ/plan]
---

# Vokabel-Türme: Umstieg von C# auf GDScript

Stand 2026-11-27. Entschieden vom Vater: Das Spiel wechselt von Godot/C# auf Godot/GDScript und bekommt dabei
gleich eine saubere, Godot-typische Struktur. Dieser Plan ist so geschrieben, dass eine **frische Sitzung ohne
Vorwissen** ihn ausführen kann.

> **Umgesetzt am 2026-11-27**, die Runde auf dem Handy eingeschlossen. Was nachgewiesen ist, was abweicht und was offen
> bleibt, steht unter [Paritätsliste](#paritätsliste-abnahme) und
> [Ergebnis der Umsetzung](#ergebnis-der-umsetzung-2026-11-27). Die Phasen unten beschreiben den Weg; wo dort
> `game-gd/` steht, ist heute `game/` gemeint.

## Auftrag für die ausführende Sitzung

> Setze `docs/turm-spiel-gdscript-plan.md` um: Portiere das Godot-Spiel „Vokabel-Türme“ (`game/`) von C# nach
> GDScript, mit der Zielstruktur aus dem Plan und bei gleichem Spielverhalten (Paritätsliste). Arbeite auf einem
> eigenen Branch und committe phasenweise; nicht pushen. Phase 0 (Größe messen) entscheidet, ob es weitergeht.
> Für Downloads (Godot ohne .NET, Export-Vorlagen) fragst du vorher. Am Ende: Paritätsliste abgehakt, APK-Größe
> vorher/nachher, Abschlussbericht.

## Warum

Die APK hat 110 MB. Davon sind etwa 70 MB .NET-Laufzeit, die nur wegen C# mitreist. Darunter sind 44 MB
statische Bibliotheken (`.a`), die Godots .NET-Export für Android einpackt, obwohl sie zur Laufzeit niemand
braucht. Die Spielinhalte selbst (Karten, Modelle, Texturen, Sounds) machen etwa 6 MB aus.

Die Gründe, aus denen damals C# gewählt wurde, tragen nicht mehr:

- Wiederverwendung von `Pugling.Contracts`: Das Spiel nutzt davon nur 5 Endpunkte mit etwa 10 Datentypen.
- xUnit für die Spielregeln: Das sind 18 Tests auf wenigen kleinen Klassen.

Erwartet werden mit GDScript 30–40 MB (Debug), weniger als Release-Build. Dazu kommen ein schnellerer Start,
weniger Speicher und ein reifer iOS-Export, falls später ein iPhone dazukommt.

## Ausgangslage (C#, Commit `99283817`)

```text
game/
  project.godot, export_presets.cfg ("Android" arm64, "Android-Emulator" x86_64), Main.tscn, icon.svg
  PuglingTower.csproj (net9.0, kompiliert backend/Pugling.Contracts mit), Scripts/ (~3.000 Zeilen C#), Tests/ (xUnit)
  assets/kaykit/{hex,chars}  KayKit-Modelle (CC0, glTF/glb, mit .import)
  assets/sfx/*.wav           13 selbst erzeugte Effekte (Erzeuger: game/tools/sfx.py)
  tools/                     sfx.py (Python, .gdignore)
```

Die Struktur und die Nahtstellen beschreibt `docs/turm-spiel-plan.md` → „Code-Struktur und Erweitern“ und
„Learning Dad, Sound, Katapult“. **Lies beide Abschnitte und den C#-Code, bevor du portierst.** Der C#-Code ist
die Referenz für das Verhalten; sie bleibt bis zur letzten Phase im Repo.

### Server-Vertrag (unverändert, JSON in camelCase)

Basis: `{server}/api/v1/`. Nach dem Login trägt jeder Aufruf `Authorization: Bearer {token}`.

| Aufruf | Rumpf | Antwort |
|---|---|---|
| `POST auth/child` | `{childId, pin}` | `{token, role, id, name, expiresAt}` |
| `GET student/children/{id}/tower-game` | – | `{positions:[{planPositionId, planTitle, subjectName?, exerciseTitle, wordCount}], playedSecondsToday, limitSeconds, coinsToday, dailyCoinCap}` |
| `POST …/tower-game/runs` | `{planPositionId}` | `{runId, planPositionId, goal, towerSlots, towers:[{towerId, prompt}], tokens:[{tokenId, word}]}` |
| `POST …/tower-game/assign` | `{runId, tokenId, towerId}` | `{correct, expected}` |
| `POST …/tower-game/runs/{runId}/heartbeat` | – | `{playedSecondsToday, limitSeconds, timeUp}` |
| `POST …/tower-game/runs/{runId}/end` | – | `{won, solved, goal, coinsAwarded, coinsToday, dailyCoinCap}` |

- **Unbekannte Felder lehnt der Server mit `400 unknown_field` ab.** Die Rümpfe müssen also genau diese
  Feldnamen tragen.
- `JSON.parse_string` liefert Zahlen als `float`. Ids und Zähler deshalb mit `int()` lesen.
- Fehlertexte für den Bildschirm (deutsch), wie in `Scripts/Net/PuglingApi.cs`:

  | Fall | Text |
  |---|---|
  | 401 | „PIN falsch oder Anmeldung abgelaufen.“ |
  | 404 | „Nicht gefunden – ist der Plan aktiv?“ |
  | 409 mit `tower_game_time_limit` | „Deine Spielzeit für heute ist um. Morgen geht's weiter!“ |
  | 409 mit `tower_game_run_closed` | „Das Level ist vorbei.“ (beendet das Level) |
  | 409 sonst | „Gerade kam etwas dazwischen – bitte nochmal.“ |
  | 429 | „Zu viele Versuche, kurz warten.“ |
  | Netzfehler | „Server nicht erreichbar (…)“ |

## Zielstruktur (GDScript, Godot-typisch)

Leitgedanken:

- Daten als Ressourcen.
- Regeln als reine Skripte ohne Szenenbaum, damit testbar.
- Darstellung als Szenen.
- Kommunikation über Signale.
- Durchgehend statisch typisiert.

```text
game/
  project.godot                ohne C#-Feature; config/quit_on_go_back=false; Datei-Log an (20 Dateien);
                               GDScript-Warnungen "untyped_declaration" u. a. als Fehler
  export_presets.cfg           "Android" (arm64) und "Android-Emulator" (x86_64), Paket de.pugling.tower
  autoload/
    api.gd                     PuglingApi: Login, Overview, Start, Assign, Heartbeat, End (HTTPRequest + await),
                               Fehlertexte wie oben; ein Fake für Tests/Headless (gleiche Methoden)
    log.gd                     Log.line("…") -> print("TURM hh:mm:ss …")
  data/                        Ressourcen (.tres) + ihre Klassen (class_name … extends Resource)
    tower_kind.gd   bolt_tower.tres, catapult.tres
    projectile_kind.gd   bolt.tres, fire_arrow.tres, boulder.tres, fire_boulder.tres, hero_bolt.tres
    enemy_kind.gd   minion.tres, warrior.tres
    level_def.gd    lichtung.tres, flussfurt.tres, burgberg.tres   (Pfade, Turmplätze, Fluss, Brücken, Berge …)
    balance.gd      alle Stellschrauben (heute Core/Balance.cs) als Konstanten
  core/                        reine Regeln, ohne Szenenbaum (extends RefCounted), unit-getestet
    tower_ammo.gd, run_tally.gd, spawn_schedule.gd, token_pool.gd, cooldown.gd
  screens/                     Bildschirmfluss (heute GameRoot.cs): login.tscn, positions.tscn, maps.tscn, result.tscn
    main.gd                    wechselt die Bildschirme, Android-Zurück-Taste (entprellt, 400 ms)
  level/
    level.tscn + level.gd      dünner Koordinator: Regeln in fester Reihenfolge, danach Signale auf `events`
    game_events.gd             Signale (enemy_spawned, shot_fired, tower_fired, projectile_hit, enemy_defeated,
                               castle_hit, word_appeared/expired/picked_up/laid_down/given/assigned,
                               server_message, time_up, pause_changed, level_ended)
    run_session.gd             Turm-Bedeutungen, assign, Heartbeat, Logzeilen
    feedback.gd                Funken + Rückmeldezeile (hört nur zu)
    sounds.gd                  Sound (hört nur zu): Schüsse räumlich, Wörter/Ende flach, Mindestabstand 0,09 s
  world/   map_builder.gd (MultiMesh-Hexfeld, Straßen, Fluss, Burg, Bäume), map_info.gd, models.gd, geo.gd, fx.gd
  combat/  towers.gd, enemies.gd, projectiles.gd (Bogen, Modell-Geschosse, Funkenschweif)
  heroes/  hero.gd (Position, Laufen, Tragen), knight_look.tscn + knight_look.gd (Darstellung, Schnittstelle
           wie IHeroLook: update(dt, moving, yaw), play(action))
  words/   word_books.gd (Abwurf, Ablegen = bleibt liegen, Verfall nach 30 s, freie Plätze mit Abstand 2,4)
  ui/      overlay.gd (2D-Zeichnen über der Szene), controls.gd (Aufheben-Liste, Geben, Ablegen, Pause-Knopf),
           pause_menu.tscn, touch_stick.gd, ui_kit.gd
  tests/   run_tests.gd (eigener kleiner Runner: `godot --headless --script res://tests/run_tests.gd`, Exit-Code
           0/1) und test_*.gd: die 18 Fälle aus Tests/RulesTests.cs
  assets/  unverändert übernehmen
  tools/   sfx.py (unverändert)
```

Ein eigener Test-Runner statt GUT, weil das ohne Download auskommt. Wenn du GUT willst, frag vorher.

## Phasen

Committe jede Phase einzeln, auf einem eigenen Branch und nicht pushen. **Der C#-Stand bleibt bis Phase 6 im
Repo.**

### Phase 0: Werkzeug und Größe messen (Abbruchpunkt)

1. Installiert ist nur `Godot_v4.7.2-stable_mono_win64`. Ein Projekt ganz ohne C# lässt sich damit
   wahrscheinlich exportieren, aber dann eventuell mit den .NET-Export-Vorlagen.
   - Prüfe das.
   - Wenn nötig, besorge Godot 4.7.2 **ohne .NET** samt Export-Vorlagen von godotengine.org bzw. GitHub
     Releases. **Vorher den Nutzer fragen** (Download).
   - Ablage neben der vorhandenen Version unter `%LOCALAPPDATA%\Programs\Godot\`.
2. Lege ein Wegwerf-Projekt mit den heutigen `assets/` und einer Szene an, die ein KayKit-Modell zeigt, und
   exportiere es als „Android“, einmal Debug, einmal Release. Notiere die Größen.
3. **Weiter nur, wenn die Debug-APK unter 50 MB liegt.** Sonst anhalten und dem Nutzer berichten, woran es
   liegt.

**Ergebnis (2026-11-27):** Kein Download nötig. Der installierte Editor `Godot_v4.7.2-stable_mono_win64`
exportiert ein Projekt ohne C# mit den vorhandenen `mono`-Vorlagen, und die .NET-Laufzeit bleibt dabei draußen:
In der APK liegen weder `libmonosgen*` noch `.dll`-Dateien. Gemessen am Wegwerf-Projekt mit allen heutigen
`assets/` und einer Szene mit dem Ritter:

| Build | C# (vorher) | ohne C# |
|---|---|---|
| Debug, arm64 | 110,4 MB | 34,4 MB |
| Release, arm64 | – | 32,5 MB |

Den Rest füllt fast allein `libgodot_android.so` (76 MB unkomprimiert). Die Schwelle von 50 MB ist klar
unterschritten, es geht weiter.

### Phase 1: Gerüst

- Neues GDScript-Projekt im Ordner `game-gd/` mit Ordnerstruktur, `project.godot` (Einstellungen siehe oben),
  `export_presets.cfg` (aus dem alten übernehmen, ohne .NET-Teile) und `Main.tscn`.
- Assets kopieren; die `.import`-Dateien neu erzeugen mit `--headless --import`.
- `autoload/log.gd`.
- Ein Durchstich: Login-Bildschirm mit Ziffernfeld gegen die Wegwerf-API (siehe unten).
- `game-gd/` bekommt erst in Phase 6 den Namen `game/`, damit beide Stände bis dahin nebeneinander vergleichbar
  bleiben.

### Phase 2: Regeln und Tests

- `core/*.gd` und `data/balance.gd` als 1:1-Port von `Scripts/Core/`.
- Die 18 xUnit-Fälle als GDScript-Tests portieren, dazu den Runner.
- Je Fall die rote Probe: Regel kurz verfälschen, Test wird rot, zurück. Den Wiederherstellungs-Fallstrick
  aus `CLAUDE.md` beachten.

### Phase 3: Netz und Bildschirme

- `autoload/api.gd` und ein Fake mit denselben Methoden.
- `screens/` vollständig:
  - Login mit Ziffernfeld, ohne Bildschirmtastatur (`virtual_keyboard_enabled = false`); das gewählte Feld ist
    hervorgehoben.
  - Server-Vorgabe: x86_64 = Emulator (`http://127.0.0.1:5280`), sonst Heimnetz
    (`http://192.168.178.55:5200`), beide als Knöpfe.
  - Wortwahl (Spielzeit und Münzen heute), „Abmelden“, „Spiel beenden“.
  - Kartenwahl mit Beschreibung, „Andere Wörter“.
  - Ergebnis:
    - Titel je Ende-Grund: geschafft / Burg gefallen / Zeit um / „Spiel beendet.“
    - Münzen.
    - „Ergebnis nochmal senden“, wenn `end` scheiterte.
    - „Noch einmal“ / „Andere Karte“ / „Andere Wörter“.
  - Zurück-Taste je Bildschirm.

### Phase 4: Welt und Level

- `world/`, `combat/`, `heroes/`, `words/`, `ui/`, `level/` portieren: zuerst die Welt mit allen drei Karten,
  dann Held und Steuerung, dann Gegner, Türme und Geschosse, dann Wörter und das Geben.
- Danach Ereignisse, `feedback`, `sounds`, Overlay und Pause.
- Die Zahlen kommen aus `data/` bzw. `balance.gd`, nicht verstreut.

### Phase 5: Parität prüfen

- Headless mit dem Fake-Server alle drei Karten durchspielen (das gab es für C# schon, siehe
  `docs/turm-spiel-plan.md` Zeile ~209). Die `TURM`-Logzeilen müssen gleich lauten.
- APK bauen und aufs Handy bringen, auf der **Wegwerf-API** mit einer DB-Kopie eine Runde spielen, dazu
  Bildschirmfotos.
- Größe vorher/nachher festhalten.

### Phase 6: Umschalten und Aufräumen

- C#-Stand löschen (`Scripts/`, `Tests/`, `*.csproj`, `*.sln`, `.godot/mono`); `game-gd/` wird zu `game/`.
- `docs/turm-spiel-plan.md` nachziehen: Technik-Abschnitt, Code-Struktur, „Wie ergänze ich …“.
- `CLAUDE.md`-Verweise prüfen. Das Root-`CLAUDE.md` erwähnt das Spiel nicht; die Test-Tor-Hooks gelten nur für
  `.cs` und berühren das Spiel dann nicht mehr.
- Abschlussbericht.

## Paritätsliste (Abnahme)

Stand 2026-11-27. Abgehakt ist, was nachgewiesen ist. Die Nachweise liegen unter `game/build/nachweis/` (nicht in
git): Logs der Läufe und Bildschirmfotos unter `fotos/`. „Level-Lauf“ heißt `tests/smoke_level.tscn` (183
Prüfungen), „Bildschirm-Lauf“ `tests/smoke_screens.tscn`, „API-Lauf“ `tests/smoke_api.tscn` gegen die
Wegwerf-API. Der Level- und der Bildschirm-Lauf sind auch im **exportierten** Stand grün
(`tests/smoke_export.sh`).

- [x] Login: Ziffernfeld ohne Tastatur, Kind-Nr. vorbelegt 3, Server-Vorgabe nach Gerät, Fehlertexte.
  - Bildschirm-Lauf: Ziffernfeld schreibt ins gewählte Feld, beide Felder ohne Bildschirmtastatur, Kind-Nr. 3,
    Vorgabe „Emulator“ auf x86_64. Foto `1-login.png`.
  - Handy-Foto `handy-01-start-gdscript.png`: Vorgabe „Heimnetz“ auf arm64.
  - API-Lauf: 401, 404, 409 Zeitlimit, 409 Lauf geschlossen, 429 und „Server nicht erreichbar“ lauten wie in der
    Tabelle oben. Nicht ausgelöst wurde nur der allgemeine 409-Text.
- [x] Wortwahl, Kartenwahl, Ergebnis mit allen Knöpfen; Zurück-Taste je Bildschirm (Login → App beenden).
  - Bildschirm-Lauf, Fotos `2-wortwahl.png`, `3-kartenwahl.png`, `5-ergebnis-ungespeichert.png`, `6-ergebnis.png`.
    Der Lauf endet erst mit Exit-Code 0, wenn die Zurück-Taste auf dem Login die App beendet.
- [x] Drei Karten: Lichtung, Flussfurt (Fluss, 3 Brücken), Burgberg (2 Wege).
  - Level-Lauf (Turmzahl je Karte, Fluss sperrt, Brücke trägt, beide Wege enden an der Burg), Fotos
    `level-start-*.png`. Die Karten entstehen mit demselben Seed und derselben Würfelfolge wie in C#.
- [x] Kamera folgt dem Helden (Abstand (0, 15, 9,5), Sichtwinkel 42). Joystick links frei aufsetzbar,
      Tastatur-Pfeile als Ersatz. Gleiten an Hindernissen.
  - Level-Lauf, Prüfungen „Laufen: …“ und „Kamera folgt …“, Foto `level-0-joystick.png`.
- [x] Ritter-Modell mit Animationen (Idle, Running_A, PickUp, Interact, Cheer, Death_A) und schwebendem Buch.
  - Level-Lauf, Prüfungen „Ritter: …“ (Clips vorhanden, Running_A beim Laufen, Idle im Stehen, PickUp beim
    Aufheben, Cheer am Ziel, Death_A bei gefallener Burg, Buch über dem Kopf).
- [x] Skelette: laufen die Wege, jedes vierte ist ein Krieger (4 HP, langsamer), Takt und Tempo steigen,
      10 Burg-Leben.
  - Level-Lauf (Krieger-Folge, Tempo, alle Wege, Burg fällt beim zehnten) und die Regeltests zu `SpawnSchedule`
    und `RunTally`.
- [x] Türme (Bolzenturm und Katapult reihum):
  - [x] je eine Bedeutung, Munition, Panel mit Balken und Zahl → Level-Lauf, Foto `level-0-geben.png`
  - [x] Randmarker außerhalb des Bildes, rot bei wenig Munition → Fotos `level-0-geben.png` und
        `level-randmarker.png`. Die Zeichnung selbst prüft kein Lauf, nur das Foto.
  - [x] stark ab mehr als 30 Schuss oder nach 5 Aufladungen (★, Feuerpfeile/Feuerkugeln) → Regeltests,
        Level-Lauf („Stark: …“), Foto `level-stark.png`
  - [x] Katapult wirft im Bogen, mit Mündungsfeuer, Rauch und Rückstoß → Level-Lauf („Katapult: …“), Foto
        `level-katapult.png`
- [x] Held schießt selbst (Reichweite 5,5, alle 1,0 s, 1 Schaden) → Level-Lauf („Heldenschuss …“).
- [x] Wörter: fallen neben den Weg, verfallen nach 30 s (blinken die letzten 5 s), abgelegte bleiben liegen
      (weißer Ring), Bücher halten Abstand 2,4.
  - Level-Lauf („Wörter: …“), Foto `level-aufheben-liste.png` (weiße Ringe).
- [x] Aufheben: rechte Liste mit bis zu 5 Wörtern in Reichweite. Ein neues Wort tauscht gegen das getragene.
      Knopf „„Wort“ ablegen“.
  - Level-Lauf („Aufheben-Liste …“, „Tauschen: …“, „Knopf … ablegen“), Foto `level-aufheben-liste.png`.
- [x] Geben: grüner Knopf am Turm; richtig +8 Schuss und nächste Bedeutung; falsch −3 Schuss, das Wort geht
      zurück in den Pool; Rückmeldezeile und Funken.
  - Level-Lauf auf allen drei Karten, je sechsmal. Die Funken prüft nur das Foto `level-katapult.png`.
- [x] Ziel erreicht → Ende. Heartbeat alle 15 s; Zeit um → Ende.
  - Level-Lauf („Ziel erreicht …“, „Heartbeat …“, „Zeit um: …“, „Lauf geschlossen: …“).
- [x] Pause: Knopf oben rechts und Zurück-Taste; „Weiter“ / „Aufhören“.
  - Level-Lauf und Bildschirm-Lauf, Fotos `4-pause.png`, `level-*-pause.png`.
- [x] Sound an denselben Ereignissen wie in `GameSounds.cs`.
  - Level-Lauf zählt jeden gestarteten Klang: alle zwölf angeschlossenen Effekte spielen. `tap.wav` war schon in
    C# an kein Ereignis gebunden.
- [x] Logdatei `user://logs/godot.log` mit Zeilen `TURM hh:mm:ss start/assign/abgelegt/pause/weiter/ende …`.
  - Am PC nachgewiesen (`logdatei-user-logs-godot.log`). Die Zeilenformen stimmen mit dem C#-Stand überein:
    `turm-vergleich.txt` (Referenzlauf am PC) und `turm-vergleich-handy.txt` (67 echte Zeilen vom Handy).
  - Auf dem Handy mit der GDScript-APK: `handy-gdscript-godot.log` mit `start`, `assign` (falsch und richtig),
    `pause`, `weiter` und `ende GaveUp …`. Nur `abgelegt` kam in dieser Runde nicht vor.
- [x] Die 18 Regeltests sind grün, jeder mit roter Probe.
  - `regeltests.log` (19 Fälle, einer neu) und `rote_probe.log` (19 von 19 werden am benannten Test rot).
- [x] Eine Runde auf dem Handy gegen die Wegwerf-API, mit Bildschirmfoto und Logzeilen.
  - Lauf 14 auf der Lichtung am 2026-11-27 um 20:08, per `adb` ferngesteuert, mit der korrigierten APK: Türme
    stehen, Wörter fallen, ein Wort falsch und eins richtig gegeben (+8 Schuss, nächste Bedeutung), Pause, Weiter,
    Aufhören, Ergebnis. Fotos `handy-02-…` bis `handy-08-…`, darunter Randmarker (auch rot bei 0 Schuss) und die
    Aufheben-Liste. Die Wegwerf-API hat Anmeldung, Start, beide Zuweisungen, zehn Heartbeats und das Ende mit
    200 beantwortet.

## Ergebnis der Umsetzung (2026-11-27)

Umgesetzt auf dem Branch `turmspiel-gdscript`, ein Commit je Phase. `game-gd/` heißt seit Phase 6 `game/`, der
C#-Stand ist aus dem Arbeitsbaum entfernt und bleibt in der Historie (letzter Stand: Commit `99283817`).

**Größe (arm64):** Debug 110,4 MB → 34,5 MB, Release 108,6 MB → 32,7 MB. In der APK liegt keine .NET-Laufzeit.

**Fund auf dem Handy:** Die erste GDScript-APK zeigte keine Türme, während alle Läufe am PC grün waren. Ursache:
Ein exportiertes Feld `@export var x: PackedVector2Array = []` kommt im exportierten Stand leer an (Turmplätze,
Fluss, Brücken, Berge, Turm-Modelle). Behoben, mit Wächtertest und einem Lauf gegen den exportierten Stand
(`tests/smoke_export.sh`). Einzelheiten stehen in `turm-spiel-plan.md` unter „Prüfen“.

**Abweichungen vom Plan:**

- **Kein Download.** Der installierte .NET-Editor exportiert das Projekt ohne C# mit seinen eigenen Vorlagen.
- **Wegwerf-API auf Port 5281.** Auf 5280 lief schon eine Instanz vom Vortag, die unberührt blieb. Das Handy
  erreicht 5281 über `adb reverse tcp:5280 tcp:5281`.
- **Mehr Dateien als in der Zielstruktur:** `autoload/tower_server.gd`, `api_result.gd`, `fake_api.gd`,
  `data/catalog.gd`, `tower_run.gd`, `tower_target.gd`, `tower_token.gd`, `level/level_end.gd`,
  `heroes/hero_look.gd`, `screens/page.tscn` sowie je eine eigene Datei für `Enemy`, `Tower`, `Projectile` und
  `WordBook`. Jede Klasse hat damit ihre Datei, wie in GDScript üblich.
- **Warnungen als Fehler:** fehlende Typen, unbenutzte Variablen und Parameter, verdeckte Namen und
  unerreichbarer Code. `missing_await` und `redundant_await` bleiben Warnungen: Knopf-Handler starten ihre
  Server-Aufrufe absichtlich ohne zu warten, und der Analyzer hält `await` auf die abstrakte Server-Klasse für
  überflüssig.
- **19 statt 18 Regeltests.** Der neue Fall hält den Export-Fallstrick.
- **Nähte für die Läufe:** `LevelSounds` meldet jeden gestarteten Klang als Signal, und `Level` zeigt seine
  Teilknoten (`hero`, `towers`, `words` …) offen. Im Spiel hört darauf niemand.
- **Export:** `tests/` bleibt aus der APK draußen. Das dritte Preset „Testpaket“ packt das Spiel samt Tests nur
  für den Paket-Lauf.
- **C#-Referenzlogs:** Die Logdateien des C#-Referenzlaufs am PC sind durch die neuen Läufe aus der Rotation
  gefallen (derselbe Ordner, höchstens 20 Dateien). Verglichen wurde mit den vorher mitgeschnittenen Zeilen und
  mit den C#-Logs vom Handy.
- **Phase 6 vor der Handy-Runde.** Das Handy war zwischendurch nicht verfügbar; die Runde ist am selben Abend
  nachgeholt worden.

**Offen:**

- **Die Handy-Runde war kurz und ferngesteuert:** zwei Zuweisungen, aufgehört nach 153 s. Ziel erreicht, Burg
  gefallen, Zeit um, Katapult und starke Türme sind auf dem Handy nicht gesehen worden, nur im Level-Lauf am PC
  und im exportierten Paket.
- **Rückweg:** Die letzte C#-APK liegt als `game/build/csharp-stand/pugling-tower-csharp-debug.apk` bereit, falls
  die GDScript-APK auf dem Handy weitere Überraschungen zeigt.
- **Emulator-Preset** (x86_64) ist übernommen, aber nicht gebaut und nicht geprüft: Der Emulator war tabu.
- Der allgemeine 409-Text („Gerade kam etwas dazwischen – bitte nochmal.“) ist nicht ausgelöst worden.

## Betrieb, Prüfen, Fallstricke

- **Echte Daten:** `backend/Pugling.Api/pugling.db` ist die echte DB der Söhne. Der Heimnetz-Server auf `:5200`
  läuft darauf.
  - Keinen zweiten Server auf dieser Datei starten.
  - Messen nur auf einer Kopie: `sqlite3 backend/Pugling.Api/pugling.db ".backup '<scratch>/kopie.db'"`.
- **Wegwerf-API zum Testen:** das Backend auf einer DB-Kopie auf Port 5280 starten.
  - Umgebungsvariablen: `ConnectionStrings__Default=Data Source=<kopie>`, `ASPNETCORE_URLS=http://0.0.0.0:5280`,
    `ASPNETCORE_ENVIRONMENT=Production`, `Jwt__Key=<beliebig, mindestens 32 Zeichen>`.
  - Wenn der Arbeitsbaum fremde, unfertige Backend-Änderungen trägt, aus einem sauberen `git archive HEAD backend`
    bauen.
  - Friedrich ist Kind 3. PINs liegen nur gehasht vor. Setze deshalb auf der **Kopie** eine eigene Test-PIN
    (`AccountService.SetPin` bzw. über die Vater-API), nie auf der echten DB.
  - Das Handy erreicht die API über das USB-Kabel mit `adb -s <serial> reverse tcp:5280 tcp:5280`.
- **Handy:** Xiaomi, adb-Serial `8x5ho7euzlyhdajf`.
  - `adb install` scheitert dort mit `INSTALL_FAILED_USER_RESTRICTED`. Zuverlässig ist:
    `adb push app.apk /data/local/tmp/pt.apk` und dann `adb shell pm install -r /data/local/tmp/pt.apk`.
    Unter Git-Bash vorher `MSYS_NO_PATHCONV=1` setzen.
  - Log lesen: `adb shell run-as de.pugling.tower cat files/logs/godot.log`.
  - Ist das Handy gesperrt, nicht fernsteuern, sondern den Nutzer bitten.
- **Emulator:** `emulator-5554` (AVD pl-test) ist eine fremde Arbeits-AVD und voll. Nicht benutzen, nicht
  aufräumen.
- **Signatur:** Paketname `de.pugling.tower` beibehalten. Signiert wird mit dem Debug-Keystore des Editors
  (`%APPDATA%\Godot\keystores\debug.keystore`). Teilt der Nicht-.NET-Editor ihn, installiert sich die neue
  Version über die alte. Sonst einmal deinstallieren, auf dem Gerät gehen dabei nur Logdateien verloren.
- **Export-Aufruf:**
  `"<godot>.exe" --headless --path game-gd --export-debug Android build/pugling-tower.apk`.
  - Mit `timeout 500` aufrufen; der Prozess hängt nach dem Export manchmal.
  - Die Zeit der APK prüfen. Meldungen „EditorSettings not instantiated“ sind harmlos.
- **Konventionen:**
  - Code-Doku englisch (`##`-Doc-Kommentare und `#`).
  - Oberflächentexte deutsch, wörtlich wie heute.
  - Eine Regel je Kommentar, das Warum statt des Was.
- **Grenzen:**
  - Nur `game/`, `game-gd/` und `docs/turm-spiel-*.md` ändern.
  - Backend, Frontend und Server-Vertrag bleiben unberührt.
  - Keine Migration.
