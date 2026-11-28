---
tags: [bereich/turmspiel, typ/plan]
---

# Vokabel-Türme: Ausbau zum Endlos-Spiel mit Sammelanreizen

Stand 2026-11-28 (Etappen 1 bis 4 gebaut, eine Handy-Runde mit der neuen APK gespielt). Auftrag des Vaters: Das Turmspiel soll so lange fesseln wie ein gutes Handyspiel – mit dem Effekt,
dass das Kind die Vokabeln seines Unterrichtsstoffs lernt. Dieser Plan teilt den Auftrag in Etappen. Technik und
Code-Struktur stehen in [turm-spiel-plan.md](turm-spiel-plan.md), die Portierung nach GDScript in
[turm-spiel-gdscript-plan.md](turm-spiel-gdscript-plan.md).

## Leitlinien

- **Lernen ist der einzige Weg zu Fortschritt.** Punkte, Turm-Stufen, Trophäen und Truhen entstehen aus richtig
  zugeordneten Vokabeln, und ein neues oder schwaches Wort zahlt mehr als ein gut gelerntes. Was nur Geschick
  belohnt (Monster treffen), zahlt wenig.
- **Der Server zählt, was Wert hat.** Münzen, Gems, Trophäen, Truhen und Tagesaufgaben bucht der Server nach
  seiner eigenen Zählung der richtigen Zuordnungen. Die Punkte eines Laufs zählt die App; sie sind Anzeige und
  Rekord, kein Zahlungsmittel.
- **Die Hebel des Vaters bleiben.** Das Tageslimit an Spielminuten und der Münzdeckel gelten weiter. „Endlos“ heißt:
  ein Lauf hat kein Ziel, an dem er endet – nicht: unbegrenzte Spielzeit.
- **Vorhandenes weiterverwenden.** Tagesaufgaben, Auszeichnungen, Truhe und Gems gibt es im Lern-Teil der App
  schon (`GamificationService`, `DailyBoxClaim`). Das Spiel zeigt und füttert sie, statt ein zweites System zu bauen.

## Etappe 1 – Endlos-Wellen, Turm-Stufen, Punkte (gebaut 2026-11-27)

| Wunsch | Umsetzung |
|---|---|
| Runden sind endlos, Level sind Angriffswellen | `core/wave_plan.gd`: Welle n bringt 5 + 2·(n−1) kleine Skelette (höchstens 30) und 3 + (n−1)/2 Wortträger (höchstens 9). Nach dem letzten Gegner 5 s Pause, dann die nächste Welle. Tempo, Takt und Lebenspunkte steigen je Welle. |
| Mehrere Monsterarten; kleine geben Punkte, große Vokabeln | Kleines Skelett: 10 Punkte, kein Wort. Krieger: 30 Punkte, 1 Wort. Riese am Ende jeder 5. Welle: 300 Punkte, 3 Wörter. Daten in `data/minion.tres`, `warrior.tres`, `boss.tres`. |
| Turm verbessert sich mit jeder Vokabel, nach Schwierigkeit | Der Server gibt jedem Turm den Wert seines Wortes mit (`TowerTarget.Value`): 3 = neu oder schwach (Box 1–2 oder nie gesehen), 2 = Box 3, 1 = Box 4–5. Der Wert ist die „Kraft“, die der Turm bekommt. |
| Turm steigt ab einem Schwellwert auf, Pfeile treffen härter | `core/tower_level.gd`: Stufe 2 ab 4 Kraft, Stufe 3 ab 12, Stufe 4 ab 24 … Jede Stufe gibt +1 Schaden je Schuss. Am Turmschild steht die Stufe mit einem Ring bis zur nächsten. |
| Mehr Vokabeln als bisher | Ein Lauf trägt bis zu 40 Wörter statt 12; sind alle durch, kommen sie erneut. Damit bei vielen Wörtern die passenden fallen, schickt der Server die Wörter grob in Turm-Reihenfolge, und die App lässt aus den vordersten acht fallen. |
| Zusammenfassung mit Gesamtpunkten | Ergebnisbildschirm: Welle, Punkte, Rekord der Karte, je eine Zeile für Wellen, Monster, Vokabeln und Turm-Aufstiege, dazu die beste Turm-Stufe. `core/run_score.gd` zählt. |
| Motivations-Sound und Animation, mindestens je Welle | Horn und Banner „Welle n“ beim Start; Fanfare, Banner „Welle n geschafft!“, Goldfunken an allen Türmen und jubelnder Ritter am Ende. Eigener Klang und Funken beim Turm-Aufstieg. „Neuer Rekord!“ am Laufende. |

Zusätzlich entschieden (im Sinne des Auftrags, zum Umstoßen):

- Eine Welle ohne Durchbruch **repariert die Burg um ein Leben**; mit Durchbruch ist sie nur „überstanden“.
- Münzen zahlt der Server unverändert: einmal je Lauf ab 10 richtigen Vokabeln, unter dem Tagesdeckel.
- Der Rekord je Karte liegt nur auf dem Gerät (`user://records.cfg`).

Geprüft: 36 Regeltests mit roter Probe, Level-Lauf mit 212 Prüfungen (auch im exportierten Paket), Server-Tests.
**Auf dem Handy** (2026-11-27, gegen die Wegwerf-API auf einer DB-Kopie): Welle 1 geschafft, Welle 2 begonnen,
Zusammenfassung mit Rekord. **Befund zur Balance:** Nach Welle 1 hatten die drei Türme noch 3, 2 und 0 Schuss, und
es waren nur drei Wörter gefallen – die Munition ist zu knapp für ein Spiel, das lange laufen soll. Die Zahlen sind
ein erster Wurf und brauchen Spielrunden der Kinder.

### Balance nach dem Handy-Befund (2026-11-28)

| Stellschraube | vorher | jetzt | Grund |
|---|---|---|---|
| Schuss je Turm am Start | 10 | 16 | Welle 1 kostet rund 25 Schuss; drei Türme halten sie jetzt sicher, die zweite ohne Wörter nicht mehr. |
| Schuss je richtigem Wort | 8 | 8 + 2 je Wertpunkt (10/12/14) | Ein neues oder schwaches Wort füllt am meisten – dieselbe Leitlinie wie bei der Turm-Kraft. |
| Wortträger in Welle 1 | 3 | 4 (höchstens 10 statt 9) | Die Wörter einer Welle decken die Schüsse der nächsten. |

Nachgewiesen als Rechnung in `tests/test_balance.gd` (kleinste Karte, drei Türme, vorsichtig gerechnet): mit jedem
gefallenen Wort bleiben die Türme 20 Wellen lang versorgt, ohne Wörter sind sie in Welle 2 leer – auch mit den
Spezialgegnern aus Etappe 4. Der Titel der Wörter-Karte bricht um, statt abgeschnitten zu werden. Offen bleibt, was
nur Spielrunden der Kinder zeigen: ob das Tempo beim Tragen der Wörter reicht.

## Etappe 2 – Startbildschirm, einmal anmelden, Einstellungen (gebaut 2026-11-27)

- **Einmal anmelden:** Entschieden vom Vater: die App merkt sich die PIN. Server-Adresse, Kind und PIN liegen in
  `user://settings.cfg`, verschlüsselt mit einem Schlüssel aus der Geräte-Kennung (`data/settings.gd`). Das schützt
  nicht vor jemandem, der das Gerät in der Hand hat, aber die PIN steht in keinem Dateimanager und in keiner
  Sicherung im Klartext. Beim Start meldet sich die App selbst an und öffnet die Startseite. Läuft die Sitzung ab,
  meldet sie sich einmal still neu an. Passt die gespeicherte PIN nicht mehr oder ist der Server weg, öffnen sich
  die Einstellungen mit dem Grund.
- **Einstellungen** (der frühere Login-Bildschirm): Server (Heimnetz, Emulator oder eigene Adresse), Kind-Nr., PIN,
  Ton an/aus, Abmelden (vergisst die PIN).
- **Startseite** (`screens/home.gd`): der Ritter in der Mitte, unten rechts der große Spielen-Knopf, daneben die
  Karte (ein Tipp wechselt sie) und die Wörter (öffnet die Liste). Oben Serie, Münzen, Gems und der Rekord. Links
  die Tagesaufgaben: die Lernziele des Tages mit den offenen Positionen und die Missionen mit Fortschritt und
  Belohnung. Rechts die Truhe, die Spielzeit und die Spiel-Münzen des Tages. Karte und Wörter bleiben gemerkt.
- Alles auf der Startseite kommt vom Server (`me/points`, `children/{id}/today`, `me/missions`, nur lesend). Die App
  hakt keine Aufgabe selbst ab.
- Die Kartenwahl als eigener Bildschirm ist entfallen; das Ergebnis führt zurück zur Startseite oder direkt in den
  nächsten Lauf.

Geprüft: Bildschirm-Lauf (Anmelden, Startseite, gemerkte Wahl, Neustart ohne Login, abgelaufene Sitzung,
unpassende PIN, Abmelden), auch im exportierten Paket; der Aufruf der Startseite gegen die Wegwerf-API auf einer
DB-Kopie. Auf dem Handy gesehen: Anmelden, Startseite mit den echten Lernzielen der Kopie, Spielen, Ergebnis,
Einstellungen, Abmelden (Fotos `handy-10-…` bis `handy-13-…`). Der Titel der Wörter-Karte wird bei langen Namen
abgeschnitten.

Noch offen aus dieser Etappe: die Truhe lässt sich im Spiel nur ansehen, nicht öffnen; Trophäen fehlen in der
Kopfzeile (Etappe 3). Die Optik ist schlicht – Hintergrund, Symbole statt Wörtern („Münzen“, „Gems“) und
Animationen der Knöpfe sind ein eigener Schritt.

## Etappe 3 – Trophäen, Tagesaufgaben, Truhen, Serie (gebaut 2026-11-28, Server zuerst)

Alles zählt der Server aus seinen eigenen Aufzeichnungen richtiger Zuordnungen; die App meldet nur die erreichte
Welle. Die Münz-Auszahlung des Spiels ist unverändert (einmal je Lauf ab 10 richtigen Vokabeln, unter dem
Tagesdeckel), das Tageslimit an Spielminuten ebenso. Nichts kostet etwas, wenn ein Tag ausfällt.

| Baustein | Umsetzung |
|---|---|
| Trophäen | Am Laufende: Wert jedes gelösten Wortes (3/2/1, am Lauf eingefroren) plus 1 je geschaffter Welle – höchstens so viele Wellen wie gelöste Wörter, und die gemeldete Welle zählt nur, soweit die gezählte Spielzeit sie trägt (eine Welle je 20 s). Wellen ohne Vokabeln bringen nichts. |
| Trophäen-Pfad | Stufen bei 20/60/120/250/500/1000/2000 Trophäen zahlen einmal 5/10/15/25/40/60/100 Gems (`Gamification:TowerGame:TrophyPath`). |
| Spiel-Aufgaben | Drei neue Missions-Metriken; jedes Kind bekommt drei Tagesaufgaben zu je 10 Gems: „20 Vokabeln richtig“, „Welle 5 erreichen“, „3 neue Wörter richtig“. Abgerechnet am Laufende über den `GamificationService`; der Vater kann sie im Vater-Web ändern oder löschen wie jede Mission. |
| Tagestruhe | Wird auf der Startseite geöffnet (vorhandener Endpunkt `daily-box/open`). |
| Spiel-Truhe | Füllt sich mit richtigen Zuordnungen des Tages, ab 15 einmal am Tag zu öffnen, 3–8 Gems. |
| Serien | Die Lern-Serie steht oben; die Spiel-Serie zählt Tage in Folge mit mindestens 10 richtigen Zuordnungen. Sonntag und Pause brechen sie nicht; heute zählt, sobald die 10 erreicht sind. |
| Anzeige | Startseite: Trophäen oben, rechts Truhen-Knöpfe, Weg zur nächsten Pfad-Stufe und Spiel-Serie. Ergebnis: Trophäen des Laufs, Gems einer erreichten Stufe. |

**Aussehen der Startseite:** gezeichnete Symbole statt der Wörter (Münze, Gem, Trophäe, Flamme, Truhe, Uhr,
Zahnrad, Buch, Karte, Stern) – auch statt der Zeichen ✔ ○ ▸ 🪙, die der Android-Schrift fehlen können. Der Ritter
steht vor Burg, zwei Türmen, Wald und Abendhimmel mit ziehenden Wolken. Knöpfe geben unter dem Daumen nach, der
Spielen-Knopf und eine offene Truhe pulsieren, eine Zahl, die sich ändert, hüpft. Eigenes Aussehen aus den
vorhandenen KayKit-Modellen, kein Nachbau eines bestimmten Spiels.

Geprüft: Server – zwölf Testfälle in `TowerGameCollectTests` (zwei davon als Tabelle), der Migrationstest, Mutantenprobe 11/11 rot
am benannten Test, volle Suite 2729 grün; Vater-Web typgeprüft, 1530 Tests grün. Spiel – `core/trophy_path.gd` mit
Test und roter Probe, Bildschirm-Lauf (Truhen öffnen, Geldbörse zieht nach, Welle wird gemeldet) auch im
exportierten Paket, Vertrags-Lauf gegen die Wegwerf-API auf einer DB-Kopie.

**Migration `TowerGameCollect`** (nur angehängt, an einer DB-Kopie geprobt: 1340 Buchungen und 224 Lernstände
unverändert): zwei Tabellen (`TowerTrophyRewards`, `TowerGameChests`), Spalten `Wave` und `Trophies` am Lauf,
`Value` am Lauf-Wort (Bestand: 2), dazu die drei Spiel-Aufgaben für jedes vorhandene Kind.

Abweichungen vom Plan, mit Grund:

- **Kein Trophäen-Stand am Kind.** Der Stand ist die Summe über die Läufe, wie ein Kontostand die Summe des
  Buchs ist – eine zweite Zahl könnte davon abweichen.
- **Belohnungen in Gems, nicht in Münzen.** Münzen bleiben der Hebel des Vaters; Missionen und Boni zahlen in der
  App ohnehin Gems.
- **Schritte 2b und 3 des Auftrags in einem Commit:** die Startseite wurde für die neuen Anzeigen ohnehin neu
  aufgebaut, das gleich im neuen Aussehen.

## Etappe 4 – Dauerhafte Fortschritte und Abwechslung (gebaut 2026-11-28, soweit es trägt)

| Baustein | Umsetzung |
|---|---|
| Helden-Stufe | Folgt den Trophäen (Stufe 2 ab 30, 3 ab 90, 4 ab 180 …, `core/hero_level.gd`). Je Stufe schießt der Held 5 % schneller (bis 0,5 s), alle vier Stufen mit +1 Schaden. Er bleibt Plan B neben den Türmen. Startseite zeigt die Stufe, das Ergebnis meldet den Aufstieg. |
| Skins für Gems | Ritter (frei), Roter Ritter 150, Goldritter 400, Schattenritter 900 Gems. Preise und Besitz kennt der Server (`tower-game/skins`), Aussehen und Namen das Spiel; getragen wird, was das Gerät sich merkt. Keine neue Migration: der Besitz liegt in `Child.OwnedSkins` neben den Skins des Lern-Teils. |
| Läufer | Ab Welle 3 jeder dritte kleine Gegner: fast doppelt so schnell, ein Treffer genügt. Prüft, ob die Türme noch Schuss haben. |
| Schildträger | Ab Welle 4 jeder dritte Wortträger: die Rüstung nimmt 1 von jedem Treffer (mindestens 1 bleibt). Belohnt aufgestiegene Türme, trägt weiter ein Wort. |
| Heiler | Ab Welle 6 einer in der Mitte der Welle (höchstens drei): heilt alle 1,5 s Verletzte in seinem Kreis um 1, nie über deren Start-Lebenspunkte. |
| Karte | „Schlucht“: ein enger Zickzack-Weg mit vier Türmen. |

Geprüft: 55 Regeltests mit roter Probe (jede am benannten Fall rot), Level-Lauf mit den Szenen „Spezialgegner“ und
„Held Stufe 5 im roten Skin“, Bildschirm-Lauf mit Skin-Kauf und Helden-Aufstieg, beide auch im exportierten Paket;
Server-Test für den Skin-Kauf mit zwei Mutanten; Vertrags-Lauf gegen die Wegwerf-API.

Bewusst nicht gebaut (trägt noch nicht oder braucht eine Entscheidung):

- **Dauerhafte Turm-Verbesserungen:** würden die frisch eingestellte Balance verschieben; erst nach Spielrunden.
- **Wochen-Ereignisse:** brauchen einen Kalender am Server und Inhalte; eigener Schritt.
- **Bestenliste in der Familie:** nur wenn der Vater den Vergleich will (Frage unten).
- **Weitere Helden als eigene Figuren:** im Paket liegen nur Ritter und Skelette als Modelle; neue Figuren hieße
  etwas herunterladen. Die Skins färben darum den Ritter.

Die Handy-Runde mit der neuen APK gegen die Wegwerf-API steht im Abschnitt „Handy-Runde“ weiter unten.

## Handy-Runde zu Etappe 3 und 4 (2026-11-28)

Die APK vom 2026-11-28 (34,6 MB) per `adb push` + `pm install` auf dem Xiaomi, die Wegwerf-API auf der DB-Kopie
(Port 5281, am Handy über `adb reverse tcp:5280 tcp:5281`), angemeldet als Testkind 4 mit einer Test-PIN, per adb
ferngesteuert. Fotos und Log liegen unter `game/build/nachweis/` (`handy-20-…` bis `handy-24-…`,
`handy-etappe4-godot.log`).

- **Startseite:** neue Optik mit Burg und Symbolen, die Zahlen der Kopie (Trophäen 4, Münzen 3036, Gems 191),
  die drei Spiel-Aufgaben mit Fortschritt, Spiel-Truhe 2/15, „noch 16 bis +5 Gems“, Spiel-Serie 0 · heute 2/10. Der
  lange Titel der Wörter-Karte bricht um.
- **Lauf:** Lichtung, Welle 1 und 2 geschafft, Welle 3 begonnen, Burg 10/10. Ein Wort aufgehoben und einem Turm
  gegeben; der Server hat es als falsch bewertet und die richtige Lösung genannt.
- **Ergebnis:** Welle 3, 660 Punkte, neuer Rekord, „Trophäen gibt es für richtige Vokabeln. (gesamt 4)“. Der Server
  hat den Lauf mit 111 s Spielzeit, Welle 3 und 0 Trophäen gebucht – Wellen ohne Vokabeln zahlen nichts.

```text
TURM 08:11:37 start Lauf 3 Karte Lichtung, Ziel 10, Held Stufe 1 (Ritter)
TURM 08:12:23 welle 1 geschafft, Punkte 270
TURM 08:12:49 assign 'statistics (pl)' -> 'Erstens …': falsch, erwartet Firstly, …
TURM 08:13:09 welle 2 geschafft, Punkte 660
TURM 08:13:31 ende GaveUp 0/1 richtig, 99s, Burg 10/10, Heldenschüsse 31, Heldentreffer 9, Welle 3, Punkte 660
```

Danach abgemeldet, die Daten der App auf dem Handy geleert (Server steht wieder auf „Heimnetz“, keine PIN), die
Umleitung entfernt.

**Nicht am Handy gesehen:** eine richtige Zuordnung mit Trophäen, das Öffnen einer Truhe, der Skin-Kauf und die
Spezialgegner ab Welle 3 – die Fernsteuerung reichte für eine kurze Runde. Diese Teile sind durch die Läufe im
exportierten Paket und den Vertrags-Lauf gegen die Wegwerf-API belegt, nicht durch das Gerät.

**Befund zur Balance:** Die Türme verbrauchen ungleich. Der Turm, an dem die Gegner zuerst vorbeikommen, war in
Welle 1 leer, während die beiden anderen noch 13 und 15 Schuss hatten; der Lauf blieb trotzdem ohne Durchbruch. Ob
das als Aufgabe („verteile die Wörter klug“) gut ist oder der vordere Turm mehr braucht, zeigen Spielrunden.

## Offene Fragen an den Vater

1. **Spielzeit:** Bleibt das Tageslimit (Vorgabe 20 Minuten) trotz „so lange wie möglich“? Dieser Plan nimmt ja an.
2. ~~Anmeldung~~ – entschieden am 2026-11-27: die PIN wird auf dem Gerät gemerkt.
3. **Münzen:** Soll das Endlos-Spiel mehr zahlen als einmal 10 Münzen je Lauf – etwa je 10 richtige Vokabeln,
   weiter unter dem Tagesdeckel?
4. **Druckmittel:** Wie weit sollen die Bindungs-Mechaniken gehen? Serien, die verfallen, und Truhen mit
   Wartezeit wirken stark, auch gegen den Willen des Kindes. Dieser Plan sieht Serie und Tagestruhe vor, aber keine
   Mechanik, die bestraft, wenn das Kind einen Tag nicht spielt – die Pflicht samt Malus liegt im Lern-Teil.
5. **Gems aus dem Spiel:** Die Spiel-Aufgaben (3 × 10 Gems am Tag), die Spiel-Truhe (3–8) und der Trophäen-Pfad
   zahlen Gems; ein Skin kostet 150 bis 900. Passt die Größenordnung zu den Gems aus dem Lern-Teil (Combo rund 10)?
6. **Bestenliste:** Soll es den Vergleich Theodor gegen Friedrich geben?
