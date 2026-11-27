---
tags: [typ/story, status/verworfen, bereich/lehrplan, bereich/klassenarbeit, rolle/supervisor]
aliases: [Klassenarbeit ohne Übungen, Fachweite Klassenarbeit im Planer]
status: verworfen
prio: P2
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-27.md#sprint-197--ziel--umfang
grund: "Beantwortet am 2026-11-27: Kein Druck ohne Stoff (Option 1). Gebaut direkt im Anschluss an Sprint 197."
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-1034 · Darf eine Klassenarbeit ohne Übungen und Tags das ganze Fach unter Druck setzen?

Gefunden im Rollengang von Sprint 197 (B-680), bewusst nicht dort entschieden: Das Verhalten ist für den Vater sichtbar.

## Ist-Stand (gebaut mit B-680)

Eine Klassenarbeit mit Fach, aber **ohne** zugewiesene Übungen und Tags gilt für **jede** Position dieses Fachs (`Exam.Covers`
in `DailyPlanner`). Der Planer rechnet dann den ganzen Rest-Stoff aller Positionen bis zum Termin.

Beobachtet an der Kopie der echten DB, Friedrich: eine Klassenarbeit nur mit Fach in 6 Lerntagen, die Positionen des Fachs
tragen zusammen etwa 700 noch nie gezeigte Wörter. Der Planer meldet „120 neue Wörter pro Lerntag, der Deckel erlaubt 8 –
bis dahin fehlen etwa 672 Wörter“ und legt das Budget jeden Tag auf den Deckel. Im Schattenmodus wirkt das nichts; sobald
[B-736](B-736-pflicht-aus-der-tagesaufgabe.md) die Pflicht aus der Tagesaufgabe macht, steht diese Meldung täglich und das
Budget sitzt dauerhaft am Deckel.

## Die Frage an den Vater

Soll eine Klassenarbeit ohne Übungen und Tags
1. den Planer **nicht** unter Druck setzen (ohne Bündel ist der Stoff unbekannt; sie ordnet höchstens die Reihenfolge),
2. wie heute das ganze Fach unter Druck setzen, oder
3. verlangen, dass der Vater ein Bündel angibt, bevor der Planer sie liest?

Bis zur Entscheidung gilt Option 2 (wie gebaut).

## Antwort

**2026-11-27 (Nutzer):** Option 1 – eine Klassenarbeit ohne Übungen und Tags setzt den Planer nicht unter Druck: Sie ordnet ihre Positionen
höchstens nach ihrem Datum und steht in der Begründung, hebt aber kein Budget und löst keine Meldung aus. Gebaut als `ExamPresses` am
Kandidaten (`DailyPlanner`: nur eine Arbeit, die die Übung nennt, setzt das Tempo). Mutanten T1 bis T4 rot; Fälle
`Klassenarbeit_ohne_Uebungen_ordnet_nur_und_setzt_keinen_Druck` und `Klassenarbeit_ohne_Stoff_hebt_das_Budget_nicht_und_meldet_nichts`.
