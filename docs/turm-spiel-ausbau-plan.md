---
tags: [bereich/turmspiel, typ/plan]
---

# Vokabel-Türme: Ausbau zum Endlos-Spiel mit Sammelanreizen

Stand 2026-11-27. Auftrag des Vaters: Das Turmspiel soll so lange fesseln wie ein gutes Handyspiel – mit dem Effekt,
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
**Nicht geprüft:** eine Runde auf dem Handy und das Spielgefühl der Balance – die Zahlen sind ein erster Wurf.

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
DB-Kopie. **Nicht geprüft:** das Aussehen auf dem Handy (nur am PC bei 1600×720 fotografiert).

Noch offen aus dieser Etappe: die Truhe lässt sich im Spiel nur ansehen, nicht öffnen; Trophäen fehlen in der
Kopfzeile (Etappe 3). Die Optik ist schlicht – Hintergrund, Symbole statt Wörtern („Münzen“, „Gems“) und
Animationen der Knöpfe sind ein eigener Schritt.

## Etappe 3 – Trophäen, Tagesaufgaben, Truhen (offen, Server zuerst)

- **Trophäen** als dritte, nicht ausgebbare Zählgröße: der Server vergibt sie am Laufende aus *seiner* Zählung
  (richtige Vokabeln nach Wert, dazu die gemeldete Welle mit Plausibilitätsgrenze aus der Spielzeit). Trophäen-Pfad
  mit Stufen und Belohnungen. Braucht eine Migration (Spalten am Lauf, Trophäen-Stand am Kind).
- **Tagesaufgaben im Spiel:** die vorhandenen Missionen des Kindes anzeigen und um Spiel-Aufgaben ergänzen
  („20 Vokabeln richtig im Turmspiel“, „Welle 5 erreichen“, „3 neue Wörter richtig“). Abrechnung über den
  bestehenden `GamificationService`.
- **Truhen:** die vorhandene Tagestruhe im Startbildschirm öffnen; dazu eine Spiel-Truhe, die sich mit richtigen
  Vokabeln füllt.
- **Serie:** Tage in Folge mit mindestens n richtigen Vokabeln, sichtbar im Startbildschirm.

## Etappe 4 – Dauerhafte Fortschritte und Abwechslung (offen)

- Helden-Stufe und freischaltbare Helden/Skins (Gems), dauerhafte Turm-Verbesserungen.
- Weitere Monster mit eigenem Verhalten (schnelle Läufer, Schildträger, Heiler), weitere Karten, Wochen-Ereignisse.
- Bestenliste in der Familie (Theodor gegen Friedrich) – nur wenn der Vater den Vergleich will.

## Offene Fragen an den Vater

1. **Spielzeit:** Bleibt das Tageslimit (Vorgabe 20 Minuten) trotz „so lange wie möglich“? Dieser Plan nimmt ja an.
2. ~~Anmeldung~~ – entschieden am 2026-11-27: die PIN wird auf dem Gerät gemerkt.
3. **Münzen:** Soll das Endlos-Spiel mehr zahlen als einmal 10 Münzen je Lauf – etwa je 10 richtige Vokabeln,
   weiter unter dem Tagesdeckel?
4. **Druckmittel:** Wie weit sollen die Bindungs-Mechaniken gehen? Serien, die verfallen, und Truhen mit
   Wartezeit wirken stark, auch gegen den Willen des Kindes. Dieser Plan sieht Serie und Tagestruhe vor, aber keine
   Mechanik, die bestraft, wenn das Kind einen Tag nicht spielt – die Pflicht samt Malus liegt im Lern-Teil.
