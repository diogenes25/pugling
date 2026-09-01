---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [DerivedHint widerspricht dem Feld, abgeleitetes Fach ohne Option, ProfileForm anlegen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-172, Fund des frontend-reviewer
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-185 · Beim Anlegen behauptet der Hinweis ein übernommenes Fach, das das Feld nicht zeigt

Im **Anlege**-Modus von `ProfileForm` (`frontend/src/vater/VaterFachlehrer.tsx`) setzt `deriveFromSeries`
über `applySeriesChange` das Fach aus der gewählten Reihe, und `DerivedHint` meldet „aus dem Lehrwerk
übernommen". Scheitert dabei `GET creator/subjects`, während die Reihenliste geladen hat, gibt es zu diesem
Wert keine Option: der `<select>` fällt auf „– fachneutral –", der Hinweis daneben behauptet das Gegenteil,
und der POST trägt die `subjectId` trotzdem mit.

Kein Datenverlust — deshalb war das zu Recht nicht Teil von
[B-172](B-172-fachauswahl-loescht-wenn-ihre-optionen-fehlen.md). Aber es ist dieselbe Fehlerklasse: Anzeige
und Zustand driften auseinander, und der Nutzer entscheidet auf falscher Information. B-172s Schätzung hatte
die Anlege-Formulare mit „kein geladener Wert, kein Lösch-Schalter" ausgeklammert; das deckt die
*Zerstörung* ab, nicht die *Lüge*. Gefunden hat es der `frontend-reviewer`.

Das Bauteil `ReferenceSelect` hilft hier nicht von selbst: es garantiert Driftfreiheit nur für
`value === String(loadedId)`, und im Anlege-Modus gibt es kein `loadedId` — der Wert entsteht erst durch
die Ableitung.

Zu klären beim Ausformulieren: ob der Hinweis schweigen soll, wenn zum abgeleiteten Wert keine Option
existiert, oder ob die Ableitung in diesem Fall besser gar nicht greift. Und ob derselbe Fall auch die
Sprachfelder trifft, die `applySeriesChange` mit ableitet (dort sind die Optionen Konstanten — vermutlich
nicht, aber nachzusehen). Im **Bearbeiten**-Modus greift der `loaded`-Wächter von `applySeriesChange`; das
ist nachgeprüft, dort passiert nichts.

## Verlauf

- 2026-09-01 · Aufgenommen aus dem `frontend-reviewer`-Review zu B-172. Der Weg über
  `applySeriesChange` ist vom Reviewer am Code belegt, den Fall selbst habe ich nicht gefahren — darum
  `unverifiziert`.
