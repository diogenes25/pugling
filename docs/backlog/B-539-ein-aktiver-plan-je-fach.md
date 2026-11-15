---
tags: [typ/story, status/gegrillt, bereich/lehrplan, rolle/supervisor, rolle/student]
aliases: [Ein Plan je Fach, Fach Pflicht am Plan, Unit am Plan]
status: gegrillt
prio: P1
art: Wunsch
quelle: B-537 (Teil b, Entscheidungen 1–2)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-539 · Ein aktiver Plan je Fach statt je Kind

Teil b von [B-537](B-537-lehrplan-als-kapitel-pflicht-und-kuer.md). Ist-Stand am Code (Punkte 1 und 3) und
Begründungen stehen dort. **Nach [B-535](B-535-belohnungen-erreichen-friedrich-nicht.md) bauen** – beide ändern
Tagesbox und Bonus-Sperre.

## User Story

Als *Vater* möchte ich je Fach einen eigenen Lehrplan aktiv haben, damit mein Kind in jedem Fach täglich etwas tut
und jedes Fach in seinem eigenen Takt zum nächsten Kapitel wechselt. Als *Sohn* möchte ich alle Pflichten des
Tages an einer Stelle sehen, egal aus welchem Plan.

## Ist-Stand am Code

Siehe [B-537 → Ist-Stand](B-537-lehrplan-als-kapitel-pflicht-und-kuer.md#ist-stand-am-code), Punkte 1 und 3.

## Entscheidungen

Übernommen aus B-537: **1** (ein aktiver Plan je Fach), **2** (Fach Pflicht, Unit optional).

## Akzeptanzkriterien

1. Plan anlegen/ändern ohne Fach liefert `400`. Mit Unit wird das Fach aus der Reihe übernommen; hat die Reihe
   keins, muss es mitgegeben werden.
2. Ein Plan trägt optional eine Unit; Vertrag, Client und Vater-Web zeigen und setzen sie.
3. Migration (an die Kette angehängt): bestehende Pläne bekommen das Fach, das die Mehrheit ihrer Positionen
   trägt. Ist keines ableitbar, bleibt das Feld leer, bis der Plan das nächste Mal bearbeitet wird; solche Pläne
   zählen für Kriterium 4 als eine gemeinsame Gruppe. Die echte `pugling.db` startet danach, und die Pläne der
   Söhne sind unverändert spielbar.
4. Wird ein Plan aktiv, schaltet der Server nur die anderen aktiven Pläne **desselben Fachs** ab.
5. Tagespflicht, Tagesbox, Bonus-Sperre, Malus und Serie rechnen über **alle** aktiven, laufenden Pläne des
   Kindes (Regressionstest: zwei Fächer, eines erfüllt, eines offen ⇒ Pflicht offen).
6. Die Arcade zeigt die Pflichten aller aktiven Pläne; die Fach-Zugehörigkeit ist erkennbar.

## Verlauf

- 2026-11-15 · `gegrillt` angelegt durch Teilung von B-537 (Entscheidung 11); Entscheidungen dort.
