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
  falls die Spiel-Engine C# spricht (siehe Technik).

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
8. **Technik: Godot 4 mit C#**, vor dem Bau ein Technik-Spike aufs echte Handy (Phase 0).
9. **Ablage: Ordner `game/` in diesem Repo**, neben `backend/` und `frontend/`.
10. **Name: „Vokabel-Türme".**

## Technik der App

### Empfehlung: Godot 4 mit C#

| Option | Für | Gegen |
| --- | --- | --- |
| **Godot 4 (C#)** | Frei/Open Source, sehr gutes 2D, Android+iOS-Export, **C#** → Team-Sprache; Spiellogik testbar mit xUnit | C#-Export für Mobil ist jünger als GDScript; .NET-Version der Engine hinkt `net10.0` hinterher → `Pugling.Contracts` ggf. multi-targeten oder DTOs generieren |
| Unity (C#) | Ausgereiftester Mobil-Export, riesiges Asset-Ökosystem | Lizenz-/Preisrisiko, schwerer Editor, netstandard2.1 → Contracts nicht direkt nutzbar |
| Flutter + Flame | Ein Code für beide Plattformen, gute UI für Menüs | Dart neu, Spiel-Tooling dünner, kein C#-Reuse |
| Kotlin (Android) + Swift (iOS) | Wirklich nativ | Spiel zweimal bauen — für ein Hobbyprojekt nicht tragbar |

Godot exportiert **nativ** (kein WebView) und deckt „mindestens Android, gerne iPhone" mit einer Codebasis
ab. iOS braucht zum Bauen einen Mac + Apple-Developer-Konto (99 $/Jahr); Android läuft per APK direkt auf
dem Handy der Söhne, ohne Store.

Der **Technik-Spike ist gelaufen** (2026-11-22, [B-850](backlog/B-850-vokabel-tuerme-technik-spike.md)):
Godot-4.7.2-.NET-APK auf einem echten Android-Handy, Login per `auth/child`, HTTPS aus der eingebetteten
Laufzeit — alles grün. Das Spiel zielt auf `net9.0` und kompiliert die Quellen von `Pugling.Contracts` mit.

### Client-Architektur

```
game/                     (Ordner in diesem Repo, Entscheidung 9)
  Scenes/                 Map, Tower, Monster, Player, Hud
  Scripts/Core/           reine Spiellogik (Wellen, Schaden, Munition) – engine-frei, unit-getestet
  Scripts/Net/            ApiClient: Login (PIN → JWT), Runs, Assign; Token sicher speichern
  Scripts/Input/          Zwei-Daumen-Steuerung (Joystick links, Kontextknopf rechts)
```

- **Spiellogik engine-frei halten** (Wellen, Schaden, Munitionszähler als reine C#-Klassen) → testbar
  ohne Godot und balancierbar über Tabellen.
- **Level als Daten** (JSON: Türme, Pfade, Wellen, Monster-HP/Tempo) statt Code — Schwierigkeit lässt
  sich drehen, ohne neu zu bauen.
- **Login**: PIN-Login des Kindes (`auth/child`) → JWT; Server-URL einstellbar (Heimnetz `:5200`, später
  öffentlich mit HTTPS). Der Login-Rate-Limiter gilt auch hier.
- **Grafik**: freie 2D-Assets (z. B. CC0-Packs) oder eigene; **keine** Figuren, Namen oder Optik des
  Vorbild-Spiels übernehmen.

### Netz & Betrieb

Heute läuft die echte App im Heimnetz (`scripts/heimnetz.ps1`, Port 5200). Für das Handy reicht das im
WLAN. Für unterwegs braucht es einen erreichbaren HTTPS-Endpunkt (Azure-Deploy ist stillgelegt,
[deployment-azure.md](deployment-azure.md)) — das ist eine eigene Entscheidung und blockt Phase 1 nicht.

## Etappen

| Phase | Inhalt | Ergebnis |
| --- | --- | --- |
| **0 · Spike** | Godot-C#-APK auf echtem Android-Handy, Login gegen Heimnetz-API | Technikweg belegt oder verworfen |
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
- **Technik**: C#-Mobil-Export in Godot — darum Phase 0 zuerst.
- **Aufwand**: Ein Spiel mit Balancing und Grafik ist deutlich größer als ein Pugling-Feature. Phase 2
  ist bewusst hässlich, aber spielbar — erst wenn die Söhne es freiwillig spielen, lohnt Phase 3/4.

## Name

Der Wunschname „Kingshot-Derivat (nur Action-Teil)" taugt als interne Beschreibung, nicht als Produktname:
ein fremder Spielname im Titel lädt zu Marken-Ärger ein, sobald das Spiel ein Store sieht. Entschieden:
**Vokabel-Türme** (Entscheidung 10).
