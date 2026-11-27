---
tags: [typ/plan, bereich/api, bereich/app, rolle/student, lerntechnik/vokabeln]
aliases: [Turm-Spiel, Vokabel-Tower-Defense, Native Spiel-App]
---

# Vokabel-Türme: ein natives Tower-Defense-Spiel auf dem Pugling-Backend

Status: **Plan, Entscheidungen gegrillt** (2026-11-22). Backlog: [B-822](backlog/B-822-vokabel-tuerme-natives-spiel.md).
Arbeitstitel „Vokabel-Türme" — bewusst **nicht** nach dem Vorbild-Spiel benannt (siehe Abschnitt „Name").

## Die Idee in einem Absatz

Ein eigenständiges Handyspiel (Android zuerst, iPhone danach), das nur den Abwehr-Teil eines Kampfspiels
kennt: Monster laufen auf eine Stellung zu, **fest aufgebaute Türme** schießen sie ab, die Spielfigur
selbst schießt nie. Ein besiegtes Monster lässt **keine Münze, sondern eine Vokabel** fallen. Jeder Turm
trägt eine **Übersetzung**; die Figur sammelt die Vokabel ein, trägt sie zum passenden Turm und weist sie
dort zu — das ist die Munition des Turms. Munition ist knapp, also *muss* das Kind die Vokabeln richtig
zuordnen, sonst überrennen die Monster die Türme. Nach ~10 richtigen Zuweisungen ist das Level geschafft.
Das Backend bleibt unverändert die Quelle der Wahrheit: es liefert den Stoff, bewertet jede Zuweisung und
schreibt den Lernstand fort — das Spiel ist ein **weiterer Übungsbereich** der Student-Ebene.

## Spielablauf (Kern-Loop)

> **Seit 2026-11-27 ist ein Lauf endlos:** Die Gegner kommen in Wellen, kleine bringen Punkte, größere die Wörter,
> Türme steigen mit dem Wert ihrer Vokabeln in Stufen auf, und am Ende steht eine Punkte-Zusammenfassung. Die Regeln
> und die weiteren Etappen (Startbildschirm, Trophäen, Tagesaufgaben, Truhen) stehen in
> [turm-spiel-ausbau-plan.md](turm-spiel-ausbau-plan.md). Die Schritte unten beschreiben den Kern, der geblieben ist.

1. **Level-Start**: Karte mit z. B. 5 Türmen. Jeder Turm zeigt eine Übersetzung (z. B. „laufen") und hat
   einen kleinen Start-Munitionsvorrat.
2. **Welle**: Monster laufen Pfade entlang auf die Türme zu. Türme schießen automatisch, solange sie
   Munition haben.
3. **Drop**: Ein besiegtes Monster lässt ein Vokabel-Token fallen (z. B. „run"). Es gibt mehr Vokabeln
   als Türme — auch **Ablenker**, die zu keinem Turm passen.
4. **Einsammeln**: Linker Daumen = virtueller Stick zum Laufen. Kommt die Figur in Reichweite mehrerer
   Tokens, erscheint rechts eine **Auswahlleiste** (z. B. 3 Vokabeln); der rechte Daumen wählt eine.
   Die Figur trägt **eine** Vokabel zur Zeit (sonst wird es Raten statt Wissen).
5. **Zuweisen**: In Reichweite eines Turms fragt ein Dialog „*run* zu *laufen* hinzufügen? Ja / Nein".
   Rechter Daumen bestätigt.
6. **Richtig** → Turm wird aufmunitioniert, bekommt eine **neue Übersetzung** (die passende Vokabel liegt
   schon auf dem Feld oder fällt bald). **Falsch** → Strafe (siehe Entscheidung E2).
7. **Level geschafft** nach N richtigen Zuweisungen (Start: 10); **verloren**, wenn Monster die Basis
   erreichen bzw. alle Türme leer und überrannt sind.
8. **Nächstes Level**: mehr Türme, schnellere oder zähere Monster, mehr Ablenker.
9. **Später**: Turm-Upgrades (Feuerrate, Schaden, Reichweite) mit im Spiel verdienter Währung.

### Steuerung

| Daumen | Aufgabe |
| --- | --- |
| Links | Virtueller Joystick: Figur bewegen |
| Rechts | Kontextknopf: Vokabel aus der Auswahlleiste wählen, „Zuweisen Ja/Nein", später Upgrade-Menü |

Der rechte Knopf ist **kontextabhängig** (ein großer Aktionsknopf + Auswahl), damit das Spiel im
Querformat mit zwei Daumen ohne Umgreifen läuft.

## Die Lern-Seite: was das Backend tun muss

### Grundsatz: Der Server bewertet, das Spiel spielt

Wie überall in Pugling gilt Selbstbetrug-Schutz serverseitig (CLAUDE.md → „Rolle & Selbstbetrug",
Lösungsfeld-Regel). Daraus folgt der zentrale Schnitt:

- Das Spiel bekommt pro Level eine **Runde**: Turm-Übersetzungen und Vokabel-Tokens mit **undurchsichtigen
  Ids** — aber **keine Zuordnung** Token→Turm. Ein Kind mit Proxy kann die Lösung nicht aus der Antwort
  lesen.
- Jede Zuweisung geht als `POST …/assign {tokenId, towerId}` an den Server; der antwortet mit
  `correct`, `expected` (Reveal nach der Antwort ist erlaubt) und dem **nächsten** Turm-Inhalt.
- Folge: das Spiel braucht für die Lernseite eine **Verbindung** (Offline-Modus siehe E5). Die Latenz ist
  unkritisch — die Zuweisung ist ein bewusster Bestätigungsklick, kein Frame-genaues Ereignis; das Spiel
  kann optimistisch animieren und bei Widerspruch korrigieren.

### Ist-Stand, an den das Spiel andocken kann

- **Lernstand je Vokabel**: `ItemProgressService.RecordAsync` schreibt Antwort-Historie und Leitner-Box
  je (Kind, Item) fort, inklusive Anti-Farming-Schalter `countsForMastery`
  ([ItemProgressService.cs:27](../backend/Pugling.Api/Services/Shared/ItemProgressService.cs:27)).
  Die Quelle ist ein Enum mit heute `Practice`/`Test`
  ([ItemProgressEntities.cs:4](../backend/Pugling.Api/Models/ItemProgressEntities.cs:4)) → hier kommt
  `Game` dazu.
- **Positions-Üben** (Leitner je Planposition, Heartbeat, `/next`, `/review`):
  [PositionPracticeController.cs:19](../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:19).
- **Bonus-Training** (Zusatz-Üben nach erfüllter Tagespflicht, mit Münz-Deckel gegen Farming):
  [ExtraTrainingService.cs:28](../backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:28),
  Endpunkte [ExtraTrainingController.cs:17](../backend/Pugling.Api/Controllers/Student/ExtraTrainingController.cs:17).
- **Typisierter Client**: `Pugling.Contracts` + `Pugling.Client` (beide `net10.0`) — wiederverwendbar,
  falls die Spiel-Engine C# spricht. Das Spiel nutzt ihn seit dem Umstieg auf GDScript nicht mehr (siehe
  Technik).

### Neue Backend-Oberfläche (API-First, Student-Ebene)

Route-Vorschlag unter `api/v1/student/children/{childId}/tower-game/…`, `[Authorize]` mit
Inline-Rollentrennung wie die übrigen Student-Endpunkte:

| Endpunkt | Zweck |
| --- | --- |
| `GET  tower-game` | Status: Restminuten heute (E6), höchstes Level, Upgrades, Spiel-Währung, Münzen heute/Deckel (E4) |
| `POST tower-game/runs` `{level}` | Neue Runde: Türme (Id + Übersetzung), Token-Pool (Id + Vokabel), Level-Parameter. Server wählt den Stoff |
| `POST tower-game/runs/{runId}/assign` `{tokenId, towerId}` | Bewertet eine Zuweisung, schreibt `ItemProgress` (Quelle `Game`), liefert `correct`, `expected`, neue Turm-Übersetzung, nachgereichte Tokens |
| `POST tower-game/runs/{runId}/end` `{outcome, stats}` | Abschluss: gewonnen/verloren, Spielzeit; Server vergibt Belohnung nach **seiner** Zählung, nicht nach `stats` |
| `POST tower-game/upgrades` *(Phase 3)* | Turm-Upgrade kaufen |

Neue Tabellen (Migration **anhängen**, nie falten): `TowerGameRun` (Kind, Level, Start/Ende, Ausgang),
`TowerGameRunItem` (Run, Item, Rolle Turm/Token/Ablenker, Zustand), später `TowerGameProfile`
(höchstes Level, Upgrades, Spiel-Währung). Der Stoff wird **eingefroren** wie der Cursor beim Ausspielen
(`Order`+`Cursor`), damit ein Neustart der App die Runde nicht neu würfelt.

### Stoffauswahl je Runde

- Quelle: Vokabeln aus den **aktiven Plänen** des Kindes (Fach wählbar), bevorzugt Leitner-fällige und
  schwache Items; dazu bereits gelernte als Ablenker. **Keine kalt neuen Wörter** — das Spiel festigt,
  es führt nicht ein (B-419 hat gezeigt, wie schlecht kalte Abfrage ankommt).
- Ablenker müssen **plausibel** sein (gleiche Unit, gleiche Wortart), sonst ist Zuordnen trivial.
- Richtung (Fremdsprache→Deutsch oder umgekehrt) je Level oder per Plan-Einstellung.

## Entscheidungen (gegrillt 2026-11-22)

> **Verfeinert in [B-822](backlog/B-822-vokabel-tuerme-natives-spiel.md) → „Entscheidungen".** Dort gilt
> insbesondere: Entscheidung 1 unten (Faktor auf die Trefferquote) ist ersetzt durch „N bestandene Level
> je Test" — ein Faktor auf die Quote hätte die Testschwelle nie erreicht.

1. **E1 · Das Spiel kann die Tagespflicht erfüllen – abgewertet.** Eine richtige Zuweisung zählt auf die
   Pflicht der Planposition, zu der die Vokabel gehört, aber nur mit einem **Anrechnungsfaktor** (Start
   0,5), den der Vater **je Position** einstellt (0 = zählt nicht). *Begründung:* Das Spiel soll ein
   vollwertiger Weg zur Pflicht sein, sonst spielt Friedrich es nur, wenn ohnehin alles erledigt ist.
   Zuordnen ist leichter als Tippen, darum der Abschlag. *Kosten:* Ein Run muss jede Vokabel ihrer
   Position zuordnen (`TowerGameRunItem.PlanPositionId`), die Pflichtzählung braucht einen gebrochenen
   Fortschritt bzw. einen gewichteten Zähler, und `PlanPosition` bekommt ein Feld für den Faktor
   (Migration, Vater-Web-Formular). Die Kür-Freischaltung „erst nach der Pflicht" entfällt.
2. **E2 · Falsche Zuweisung: Vokabel weg und 1 Schuss weniger.** Das Token verschwindet und fällt später
   erneut; der Turm verliert einen Schuss. *Begründung:* Raten (alle Türme ablaufen) muss doppelt kosten.
   *Kosten:* Der Server führt das Token als „zurückgelegt" und reicht es nach einer Verzögerung wieder aus.
3. **E3 · Leitner-Box gedämpft.** Jede Zuweisung landet in der Historie (`ItemReviewSource.Game`); die Box
   steigt nur beim **ersten Treffer je Vokabel und Tag** (`countsForMastery`), eine falsche Zuweisung setzt
   wie im Üben zurück. *Begründung:* Wiederholtes Zuordnen derselben Vokabel darf die Box nicht hochtreiben.
4. **E4 · Belohnung: Spielwährung plus wenige Münzen.** Eine eigene Währung nur für Turm-Upgrades; dazu ein
   kleiner Münzbetrag je gewonnenem Level mit **Tagesdeckel**. *Begründung:* Münzen bleiben der Hebel des
   Vaters und dürfen nicht im Spiel gefarmt werden. *Kosten:* Münzbuchung über den Wallet-Pfad
   (ConcurrencyStamp bumpen, wie jeder abbuchende/gutschreibende Pfad).
5. **E5 · Erst online, später offline.** Phase 1–4 brauchen den Server; Offline bleibt die optionale
   Phase 5. *Begründung:* Die Bewertung bleibt serverseitig, die Lösung verlässt den Server nicht.
6. **E6 · Der Vater begrenzt die Spielminuten pro Tag** je Kind, serverseitig erzwungen (Run-Start
   verweigert, laufender Run endet am Limit). *Zusammenspiel mit E1:* Ist das Limit erreicht, bleibt der
   Rest der Pflicht dem normalen Üben.
7. **Stoff: nur schon eingeführte Vokabeln.** Fällige und schwache Items aus den aktiven Plänen,
   Ablenker aus derselben Unit; das Spiel führt keine neuen Wörter ein.
8. **Technik: Godot 4**, vor dem Bau ein Technik-Spike aufs echte Handy (Phase 0). Gestartet in C#, seit
   2026-11-27 in GDScript (siehe „Technik der App“).
9. **Ablage: Ordner `game/` in diesem Repo**, neben `backend/` und `frontend/`.
10. **Name: „Vokabel-Türme".**

## Technik der App

> **Stand 2026-11-27: Das Spiel läuft in GDScript.** Der Umstieg von C# ist vollzogen, Ablauf und Abnahme stehen
> in [turm-spiel-gdscript-plan.md](turm-spiel-gdscript-plan.md). Die APK hat 34,5 MB statt 110 MB.

### Godot 4 mit GDScript

Godot exportiert **nativ** (kein WebView) und deckt „mindestens Android, gerne iPhone“ mit einer Codebasis ab.
iOS braucht zum Bauen einen Mac und ein Apple-Developer-Konto (99 $/Jahr). Android läuft per APK direkt auf dem
Handy der Söhne, ohne Store.

Gestartet ist das Spiel in C# (Technik-Spike [B-850](backlog/B-850-vokabel-tuerme-technik-spike.md),
2026-11-22). Die zwei Gründe dafür trugen nicht:

- `Pugling.Contracts` wiederverwenden: Das Spiel braucht davon 6 Aufrufe mit etwa 10 Datentypen.
- xUnit für die Spielregeln: Das waren 18 Tests auf wenigen kleinen Klassen.

Dem stand die Größe gegenüber. Gemessen am 2026-11-27, jeweils arm64:

| Build | C# | GDScript |
| --- | --- | --- |
| Debug | 110,4 MB | 34,5 MB |
| Release | 108,6 MB | 32,7 MB |

Von den 110 MB waren etwa 70 MB .NET-Laufzeit. Den Rest füllt heute fast allein die Engine
(`libgodot_android.so`), die Spielinhalte machen etwa 6 MB aus.

**Werkzeug:** Installiert ist `Godot_v4.7.2-stable_mono_win64` (die .NET-Ausgabe des Editors). Sie exportiert ein
Projekt ohne C# mit ihren eigenen Vorlagen, und die .NET-Laufzeit bleibt dabei draußen. Ein zweiter Editor ohne
.NET ist nicht nötig.

**Server-Vertrag:** Das Spiel spricht die sechs Aufrufe direkt als JSON (`autoload/api.gd`). Der Server lehnt
unbekannte Felder mit `400 unknown_field` ab, die Feldnamen müssen also genau treffen. Das prüft
`tests/smoke_api.tscn` gegen eine Wegwerf-API.

### Client-Architektur

- **Daten als Ressourcen:** Karten, Turm-, Geschoss- und Gegnertypen sind `.tres`-Dateien unter `data/`. Die
  Schwierigkeit lässt sich drehen, ohne Logik anzufassen.
- **Regeln als reine Skripte:** Munition, Zähler, Spawn-Takt und Wort-Pool liegen unter `core/`, ohne Szenenbaum.
  Sie sind einzeln getestet und an einer Stelle abstimmbar (`data/balance.gd`).
- **Darstellung als Szenen:** Bildschirme, Level, Pause-Menü und Held sind `.tscn`-Dateien mit je einem Skript.
- **Kommunikation über Signale:** Die Bildschirme melden an `screens/main.gd`, der Level meldet auf `GameEvents`.
- **Statisch typisiert:** Fehlende Typen, unbenutzte Variablen und verdeckte Namen sind in `project.godot` als
  Fehler eingestellt. Ein Skript mit einer solchen Warnung lädt nicht.
- **Login:** PIN-Login des Kindes (`auth/child`) → JWT. Die Server-URL ist einstellbar (Heimnetz `:5200`, später
  öffentlich mit HTTPS). Der Login-Rate-Limiter gilt auch hier.
- **Grafik:** freie Assets (CC0) oder eigene. **Keine** Figuren, Namen oder Optik des Vorbild-Spiels.

### 3D (seit 2026-11-26)

Das Spiel läuft in **3D** mit schräg von oben schauender Kamera. Die Kamera folgt dem Helden und hält ihn immer
in der Bildmitte. Die Karte ist größer als der Bildschirm, und der Held dreht sich in Laufrichtung. Steuerung und
Kamera sind ein Genre-Prinzip, Figuren und Stil sind eigen.

- **Modelle:** KayKit „Medieval Hexagon“, „Adventurers“ und „Skeletons“, alle CC0. Die Auswahl liegt mit
  Lizenzdateien unter `game/assets/kaykit/`. Kenney-Pakete gibt kenney.nl nur über den Browser heraus.
- **Karte:** `world/map_builder.gd` baut sie aus einer `LevelDef`-Ressource. Hex-Gras und Fluss werden als
  `MultiMesh` gezeichnet. Die Wege sind eigene Bänder entlang der Monsterpfade. Hinter dem Rand der Karte steht
  dichter Wald, damit die mitlaufende Kamera nie ins Leere zeigt.
- **Lesbarkeit:** Alles, was am Handy lesbar sein muss, wird flach über die Szene gezeichnet (`ui/overlay.gd`).
  Das sind die Schilder der Türme mit Wort und Munitionsbalken, die Abzeichen am Rand für Türme außerhalb des
  Bildes, die Wörter auf den Büchern und das HUD.
- **Ablauf am Turm:** Kein Ja/Nein. Wer mit einem Wort nah an einem Turm steht, sieht unten rechts dauerhaft den
  Knopf „„dog“ geben“. Ein **falsches** Wort ist aus der Hand weg (es fällt später wieder) und kostet den Turm
  **3 Schuss**. Das ist eine Nutzer-Entscheidung vom 2026-11-26, vorher war es 1. Ein richtiges Wort bringt
  +8 Schuss.
- **Drei Karten:**
  - *Lichtung:* drei Türme, zum Eingewöhnen
  - *Flussfurt:* Fluss mit drei Brücken, fünf Türme
  - *Burgberg:* zwei Eingänge, sechs Türme

  Vor dem Start wählt man die Karte.
- **Renderer:** Es bleibt bei „Compatibility“, weil der Emulator damit läuft. Ob „Mobile“ am Handy schöner und
  flüssig ist, wird dort geprüft.

### Code-Struktur und Erweitern

```text
game/
  project.godot, export_presets.cfg, Main.tscn
  autoload/   log.gd (Log.line → „TURM hh:mm:ss …“), api.gd (die sechs Aufrufe), tower_server.gd (was das
              Spiel von einem Server verlangt), fake_api.gd (Attrappe für Läufe ohne Server), api_result.gd
  data/       balance.gd (Stellschrauben), catalog.gd (Listen: Karten, Turm-Mix, Gegner),
              level_def.gd + lichtung/flussfurt/burgberg.tres, tower_kind.gd + bolt_tower/catapult.tres,
              projectile_kind.gd + 5 .tres, enemy_kind.gd + minion/warrior.tres,
              tower_run.gd, tower_target.gd, tower_token.gd (was der Server zu einem Lauf schickt)
  core/       reine Regeln, einzeln getestet: tower_ammo, tower_level, run_tally, run_score, wave_plan,
              token_pool, cooldown
  screens/    main.gd (Bildschirmfluss, Zurück-Taste), page.tscn (Rahmen aller Menüs),
              login, positions, maps, result (je .tscn + .gd)
  level/      level.tscn + level.gd (dünner Koordinator), game_events.gd (Signale), run_session.gd (Bedeutungen,
              assign, Heartbeat), feedback.gd (Funken, Rückmeldezeile), sounds.gd, level_end.gd (Ende-Gründe)
  world/      map_builder.gd, map_info.gd (wo der Held gehen darf), models.gd (KayKit), geo.gd, fx.gd
  combat/     towers.gd + tower.gd, enemies.gd + enemy.gd, projectiles.gd + projectile.gd
  heroes/     hero.gd (Position, Laufen, Tragen), hero_look.gd (Schnittstelle der Darstellung),
              knight_look.tscn + knight_look.gd
  words/      word_books.gd + word_book.gd (Abwurf, Ablegen, Verfall, freie Plätze)
  ui/         overlay.gd (2D über der Szene), controls.gd (Knöpfe, Rückmeldezeile), pause_menu.tscn,
              touch_stick.gd, ui_kit.gd
  tests/      Regeltests, rote Probe, Headless-Läufe (siehe „Prüfen“)
  tools/      Einrichten, Installieren, sfx.py
  assets/     KayKit-Modelle und die erzeugten Sounds
```

**Regel:** Die Spielregeln laufen in `level/level.gd` in fester Reihenfolge. Danach meldet der Level, was passiert
ist, als Signal auf `events`. Wer auf die Signale hört (Effekte, Sound), stellt nur dar und ändert keinen
Spielstand.

#### Wie ergänze ich …

- **… einen Helden** (z. B. Learning Dad als Billboard): eine neue Szene, deren Wurzel-Skript `HeroLook`
  erweitert (`heroes/hero_look.gd`).
  - `update(dt, moving, yaw)` schaltet zwischen Stehen und Laufen um. Ein flaches Bild spiegelt sich dabei
    höchstens nach `yaw`.
  - `play(action)` spielt die Momente `PICK_UP`, `LAY_DOWN`, `GIVE`, `SHOOT`, `CORRECT`, `WRONG`, `VICTORY` und
    `DEFEAT`. Für Learning Dad passen etwa thumbs-up zu `CORRECT`, facepalm zu `WRONG` und boxing-win zu
    `VICTORY`.
  - Danach in `level/level.gd` die Konstante `HERO_LOOK` auf die neue Szene zeigen lassen. Position, Tragen und
    Kamera bleiben in `heroes/hero.gd`.
- **… Sound:** eine Datei nach `assets/sfx/`, dann in `level/sounds.gd` einen Handler an das passende Signal
  hängen (`shot_fired`, `enemy_defeated`, `word_picked_up`, `word_assigned`, `castle_hit`, `level_ended` …).
  `shot_fired` liefert das Geschoss mit Typ und Schütze, daran entscheidet sich der Klang (`shot_sound`).
- **… einen Turmtyp** (Kanone): eine neue `.tres` neben `data/catapult.tres` und ein Eintrag in
  `Catalog.TOWER_MIX`.
  - Felder: Modell, Reichweite, Takt, Mündungshöhe, normales und starkes Geschoss (je eine
    `ProjectileKind`-Ressource).
  - `blast = true` gibt Mündungsfeuer, Rauch und Rückstoß (`level/feedback.gd`, Signal `tower_fired`).
  - Ein Geschoss mit Bogen ist `arc > 0`, ein eigenes Modell statt der Kugel steht in `model`.
  - Die Turmplätze einer Karte nehmen die Typen aus `TOWER_MIX` reihum. Für feste Typen je Platz braucht
    `LevelDef` ein Feld dafür.
- **… eine Karte:** eine neue `.tres` neben `data/lichtung.tres` und ein Eintrag in `Catalog.LEVELS`.
- **… einen Gegner:** eine neue `.tres` neben `data/minion.tres`, ein Eintrag in `Catalog` und eine Regel in
  `core/wave_plan.gd` (`troop`), wann er in einer Welle mitmarschiert. Die `.tres` sagt, was er aushält, wie
  viele Punkte er bringt (`points`) und wie viele Wörter er fallen lässt (`drops`). Für eine eigene Mischung je
  Karte bekommt `LevelDef` ein Feld, das `WavePlan` liest.
- **… ein UI-Element:** Gezeichnetes kommt als Block in `ui/overlay.gd` (`_draw`), Bedienbares in
  `ui/controls.gd` mit einem eigenen Signal.
- **… einen Bildschirm:** eine Szene, die `screens/page.tscn` erbt, mit Signalen für ihre Knöpfe.
  `screens/main.gd` zeigt sie und legt fest, wohin die Zurück-Taste führt.
- **… eine Stellschraube:** Regeln des ganzen Spiels stehen in `data/balance.gd`. Werte, die zu einem Typ gehören,
  stehen in dessen `.tres`.
- **… einen Server-Aufruf:** eine abstrakte Methode in `autoload/tower_server.gd`, die echte in `api.gd` und die
  der Attrappe in `fake_api.gd`. Dazu ein Fall in `tests/smoke_api.gd`.

### Prüfen

Alle Befehle laufen aus `game/`. `$GODOT` ist die Konsolen-Ausgabe des Editors
(`Godot_v4.7.2-stable_mono_win64_console.exe`).

| Was | Befehl | Grün heißt |
| --- | --- | --- |
| Regeltests (36 Fälle) | `$GODOT --headless --path . --script res://tests/run_tests.gd` | Exit-Code 0 |
| Rote Probe je Regeltest | `GODOT=… tests/rote_probe.sh` | jeder Fall wird am benannten Test rot |
| Bildschirmfluss mit Attrappe | `$GODOT --headless --path . res://tests/smoke_screens.tscn` | Exit-Code 0 |
| Level auf allen drei Karten | `$GODOT --headless --path . res://tests/smoke_level.tscn` | „0 Abweichungen“ |
| Dasselbe im exportierten Stand | `GODOT=… tests/smoke_export.sh` | Exit-Code 0 |
| Server-Vertrag (nur Wegwerf-API) | `$GODOT --headless --path . res://tests/smoke_api.tscn -- --server=… --child=3 --pin=…` | Exit-Code 0 |

Mit Fenster und `-- --shots=<Ordner>` speichern die beiden Smoke-Läufe Bildschirmfotos.

Der Lauf gegen den Server-Vertrag startet und beendet einen echten Lauf. Er gehört nur an eine Wegwerf-API auf
einer DB-Kopie, nie ans Heimnetz.

**APK bauen und aufs Handy:**

```text
$GODOT --headless --path . --export-debug Android build/pugling-tower.apk
.\tools\handy-installieren.ps1 -ApiPort 5280
```

Das Skript bringt die APK per `adb push` und `pm install` aufs Handy, weil `adb install` dort scheitert. Mit
`-ApiPort` leitet es den Knopf „Emulator“ des Logins über das Kabel an eine Wegwerf-API um.

**Fallstricke:**

- **Der exportierte Stand ist nicht der Editor-Stand.** Am 2026-11-27 fehlten auf dem Handy alle Türme, während
  jeder Lauf im Editor grün war. Ein exportiertes Feld `@export var x: PackedVector2Array = []` kommt im
  exportierten Stand leer an. Richtig ist `= PackedVector2Array()`. Das hält der Regeltest
  `test_export_pitfalls.gd`, und `tests/smoke_export.sh` spielt den exportierten Stand vor jeder Handy-Runde
  durch.
- **Der Export merkt sich umgewandelte Ressourcen.** Nach einer Änderung am Skript einer Ressource vorher
  `.godot/exported` löschen. `smoke_export.sh` tut das selbst.
- **Headless gibt es kein Fenster.** Touch-Ereignisse über `Input.parse_input_event` landen an falscher Stelle.
  Die Läufe speisen sie mit `get_viewport().push_input(event, true)` ein.
- **Die Logdateien teilen sich einen Ordner.** Auch die Smoke-Läufe schreiben nach `user://logs`, und Godot hält
  nur 20 Dateien. Wer ein Log als Beleg braucht, kopiert es gleich weg.

### Learning Dad, Sound, Katapult (2026-11-27)

Die drei Erweiterungen, jeweils an der vorgesehenen Nahtstelle:

- **Held bleibt der Ritter.**
  - Ausprobiert wurde Learning Dad als flache Figur in der 3D-Welt, die immer zur Kamera schaut. Die Bilder
    stammten aus den App-Clips.
  - Der Vater fand den Ritter besser. Es gab keinen Laufzyklus, und aus der Draufsicht wirkte die Figur flach.
  - Der Versuch ist darum wieder ausgebaut. Ein eigener, voll animierter 3D-Dad ist als
    [B-1035](backlog/B-1035-turmspiel-eigener-animierter-dad.md) im Backlog.
- **Sound** (`level/sounds.gd`) hängt an `GameEvents`.
  - Schüsse, Knochen und Burg klingen dort, wo sie passieren, und werden vom Kamerastandort aus gehört. Ein
    Turm außerhalb des Bildes ist deshalb leiser.
  - Klänge, die zu den Wörtern und zum Spielende gehören, laufen flach in voller Lautstärke.
  - Derselbe Klang spielt höchstens alle 0,09 s, sonst wird Dauerfeuer zu Lärm.
  - Die 13 Effekte sind selbst erzeugt (`game/tools/sfx.py`), ohne Download und ohne fremde Lizenz. Zwölf davon
    hängen an einem Ereignis. `tap.wav` ist für Knöpfe gedacht und noch nicht angeschlossen.
- **Katapult-Turm** (`data/catapult.tres`): Er wirft Steinbrocken im Bogen (`arc`, Modell
  `projectile_catapult`).
  - Werte: Reichweite 8,5, ein Wurf alle 1,5 s, Schaden 2.
  - Ein starker Turm wirft „Feuerkugeln“ (Schaden 3, mit Funkenschweif).
  - Beim Abschuss gibt es Mündungsfeuer, Rauch und einen Rückstoß des Turmmodells (`Fx.blast`, `Fx.recoil`,
    über das Signal `tower_fired`).
  - Die Karten nehmen `Catalog.TOWER_MIX` reihum: Bolzenturm, Katapult, Bolzenturm, …

### Netz & Betrieb

Heute läuft die echte App im Heimnetz (`scripts/heimnetz.ps1`, Port 5200). Für das Handy reicht das im
WLAN. Für unterwegs braucht es einen erreichbaren HTTPS-Endpunkt (Azure-Deploy ist stillgelegt,
[deployment-azure.md](deployment-azure.md)) — das ist eine eigene Entscheidung und blockt Phase 1 nicht.

## Etappen

| Phase | Inhalt | Ergebnis |
| --- | --- | --- |
| **0 · Spike** | Godot-APK auf echtem Android-Handy, Login gegen Heimnetz-API (damals in C#) | Technikweg belegt oder verworfen |
| **1 · Backend** | `ItemReviewSource.Game`, Tabellen + Migration, Endpunkte `runs`/`assign`/`end`, Stoffauswahl (nur Eingeführtes), abgewertete Pflicht-Anrechnung je Position (E1), Minutenlimit (E6), Münz-Tagesdeckel (E4), Vater-Felder im Vater-Web, Integrationstests (Ownership, keine Lösung im Run-Payload, Bewertung, Anti-Farming, Anrechnung, Limit), OpenAPI | Spiel ist per `curl` durchspielbar |
| **2 · Spielbarer Prototyp** | Eine Map, 5 Türme, eine Monsterart, Sammeln/Auswahl/Zuweisen, Level-Ende nach 10 Treffern, Platzhalter-Grafik | Friedrich/Theodor spielen ein Level — **Rollengang** |
| **3 · Progression** | Level-Kurve (mehr Türme, Tempo, HP, Ablenker), Turm-Upgrades, Spiel-Währung | Mehrere Level, Langzeitmotivation |
| **4 · Politur** | Sound, Effekte, echte Grafik, Tutorial-Level, iOS-Build | Store-fähig (optional) |
| **5 · Offline** *(optional)* | Vorab-Runden, Sync-Warteschlange | Spielen ohne Netz |

Phase 1 ist reine Pugling-Arbeit im bestehenden Muster und kann unabhängig vom Spiel gebaut werden.

## Risiken

- **Spiel frisst Lernzeit**: Zuordnen aus wenigen Türmen ist leichter als Tippen, und das Spiel darf die
  Pflicht erfüllen. Gegenmittel: Anrechnungsfaktor je Position (E1), gedämpfte Box (E3), Zeitlimit (E6),
  Ablenker aus derselben Unit. Nach den ersten Wochen messen, ob die Testergebnisse halten.
- **Raten statt Wissen**: Wer alle Türme abläuft, findet den richtigen durch Probieren. Gegenmittel:
  Falschzuweisung kostet (E2), eine Vokabel zur Zeit tragen.
- **Technik**: Der exportierte Stand kann sich vom Editor-Stand unterscheiden — darum vor jeder Handy-Runde
  `tests/smoke_export.sh` (siehe „Prüfen“).
- **Aufwand**: Ein Spiel mit Balancing und Grafik ist deutlich größer als ein Pugling-Feature. Phase 2
  ist bewusst hässlich, aber spielbar — erst wenn die Söhne es freiwillig spielen, lohnt Phase 3/4.

## Name

Der Wunschname „Kingshot-Derivat (nur Action-Teil)" taugt als interne Beschreibung, nicht als Produktname:
ein fremder Spielname im Titel lädt zu Marken-Ärger ein, sobald das Spiel ein Store sieht. Entschieden:
**Vokabel-Türme** (Entscheidung 10).
