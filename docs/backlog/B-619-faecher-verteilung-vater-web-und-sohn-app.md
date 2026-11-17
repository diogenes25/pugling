---
tags: [typ/story, status/gegrillt, bereich/lehrplan, bereich/ui, rolle/supervisor, rolle/student]
aliases: [Fächer-Verteilung Frontend, Schieberegler Fächer, Regler im Vater-Web]
status: gegrillt
wartet_auf: "B-644 (Tagesplaner) – nicht bauen, bis B-644 gegrillt ist"
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-616
grund: ""
ersetzt_durch: []
---

# B-619 · Der Vater stellt die Fächer-Verteilung mit Reglern ein, und der Sohn sieht sie

Teil 2 von zwei aus [B-616](B-616-faecher-gewichtung-in-prozent.md) (dort gegrillt, beim Schätzen geteilt). Das Vater-Web bekommt am Kind das
Tagesbudget neuer Wörter und die Fächerliste mit Prozent-Reglern, die zusammen 100 halten; die Sohn-App zeigt
die Verteilung lesend. Voraussetzung: [B-618](B-618-faecher-verteilung-backend.md) ist gebaut, diese Story hängt an dessen API.

## User Story

Als *Vater* möchte ich die Verteilung der Fächer meines Kindes mit Reglern einstellen und sehen, wenn ein Anteil
ins Leere läuft, damit ich die Gewichtung ohne API-Aufrufe pflege. Als *Sohn* möchte ich sehen, wie meine Übung
auf die Fächer verteilt ist, damit ich verstehe, warum ich heute mehr Französisch als Englisch bekomme.

## Ist-Stand am Code

Gemessen am 2026-11-17 auf `main` (Stand `bf5fd841`), nur so weit, wie es die Teilung von B-616 brauchte. Die
Messung für die Schätzung steht noch aus.

- Im Frontend gibt es keinen Schieberegler: die Suche nach `type="range"` unter `frontend/src` findet nichts.
- Die Kind-Seite des Vaters ist `frontend/src/vater/VaterKind.tsx` (560 Zeilen); dort wird auch
  `BonusDailyCoinCap` gepflegt, das Vorbild für ein Feld, das nur der Vater setzt.
- Die Startseite des Sohns ist `frontend/src/sohn/SohnHome.tsx`, sein Lernstand `frontend/src/sohn/SohnProgress.tsx`.
- Die API, an der diese Story hängt, existiert noch nicht (B-618).

## Die echte Lücke

Es gibt keine Oberfläche für die Fächer-Verteilung, und kein Regler-Bauteil, auf dem sie aufsetzen könnte.

## Offene Punkte

Keine Produktfragen offen; sie sind in [B-616](B-616-faecher-gewichtung-in-prozent.md) entschieden. Für diese Story gelten daraus:

1. ~~**Wer setzt?**~~ → B-616 Entscheidung 3: nur der Vater, das Kind liest.
2. ~~**Summe?**~~ → B-616 Entscheidung 11: genau 100, die Regler halten die Summe selbst.
3. ~~**Leerer Anteil?**~~ → B-616 Entscheidung 7: verfällt, das Vater-Web warnt.
4. ~~**Positionen außerhalb der Liste?**~~ → B-616 Entscheidung 12: laufen wie heute, das Vater-Web zeigt sie an.

## Entscheidungen

Übernommen aus [B-616](B-616-faecher-gewichtung-in-prozent.md), gegrillt am 2026-11-17 mit dem Vater. Begründung und Kosten stehen dort und
wortgleich in [B-618](B-618-faecher-verteilung-backend.md); hier nur, was die Oberfläche bindet.

1. **Nur der Vater stellt ein, das Kind liest** (B-616 Entscheidung 3). *Begründung:* Das Kind soll ein
   ungeliebtes Fach nicht kleinrechnen. *Kosten:* keine Mitsprache des Kindes.
2. **Die Regler halten zusammen genau 100** (B-616 Entscheidung 11). *Begründung:* Das ganze Budget ist immer
   vergeben, und der Server weist jede andere Summe ab. *Kosten:* Ein Regler bewegt die anderen mit; die Liste
   wird immer als Ganzes gespeichert.
3. **Das Vater-Web warnt bei einem Anteil ohne aktiven Plan** (B-616 Entscheidung 7) **und zeigt Positionen
   außerhalb der Fächerliste an** (B-616 Entscheidung 12). *Begründung:* Beides ändert die Wirkung des Budgets,
   ohne dass der Vater es sonst bemerkt. *Kosten:* zwei Hinweise mehr auf der Kind-Seite.
4. **Der Verlauf der Stände ist in dieser Story nur lesbar, nicht als Diagramm** (B-616 Entscheidung 8: ein
   Stand je Änderung). *Begründung:* Die Stände sind treppenförmig und selten; eine Liste genügt. *Kosten:*
   kein Bild der Verschiebung über die Zeit.

## Akzeptanzkriterien

Final. AK 1 und 2 sind B-616 AK 13 und 14; AK 3 und 4 folgen aus den Entscheidungen 3 und 4. Je Fall die
Änderung, die seinen Test rot macht.

- **AK 1.** Das Vater-Web zeigt am Kind Budget und Verteilung mit Reglern, die zusammen 100 halten, speichert
  sie und zeigt die gespeicherten Werte nach dem Neuladen. *Rot, wenn die Seite die Werte nicht rendert.*
- **AK 2.** Die Sohn-App zeigt die Verteilung lesend. *Rot, wenn sie fehlt oder editierbar ist.*
- **AK 3.** Hat ein Fach einen Anteil, aber keinen aktiven Plan, zeigt die Kind-Seite des Vaters einen Hinweis
  mit dem Namen des Fachs; läuft eine Vokabel-Position außerhalb der Liste, nennt die Seite sie. *Rot, wenn der
  Hinweis fehlt, obwohl die API ihn meldet.*
- **AK 4.** Die Kind-Seite des Vaters listet die datierten Stände der Verteilung, den neuesten zuerst. *Rot,
  wenn nur der aktuelle Stand erscheint.*

## Verlauf

- **2026-11-17** — angelegt als Teil 2 der Teilung von B-616 (Schnitt vom Vater gewählt), Stufe `gegrillt`:
  Die Entscheidungen stammen aus der Grill-Runde zu B-616. Entscheidung 4 und die AKs 3 und 4 hat der Assistent
  aus den dortigen Entscheidungen 7, 8 und 12 abgeleitet; vom Vater nicht einzeln bestätigt. Die Schätzung steht
  aus, weil sie die Frontend-Messung und die fertige API aus B-618 braucht.
- **2026-11-17** — **Entscheidung des Vaters (Grill-Runde B-644, Entscheidung 6): angehalten.** Die Story wird nicht gebaut, bis [B-644](B-644-tagesplaner-statt-fester-lauf.md) gegrillt ist: der Tagesplaner verlegt die Pflicht von der Position an die Tagesaufgabe. Stufe und Entscheidungen bleiben bestehen; danach wird sie als Regel des Planers überführt oder verworfen.
