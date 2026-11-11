---
tags: [typ/story, status/geschaetzt, bereich/training, rolle/student]
aliases: [Richtung both ist abwechselnd]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: "beides"
migration: nein
vertragsbruch: nein
quelle: B-463
grund: ""
ersetzt_durch: []
---

# B-472 · Jedes Wort einer Wortliste in beide Richtungen üben

Abgespalten aus [B-463](B-463-ki-lehrplan-deckt-ein-sechstel-der-wortliste.md) (Entscheidung 3). Die
Access-Wortlisten tragen `direction: both`. Im Server heißt das **abwechselnd**: Eintrag 1 wird Englisch →
Deutsch gefragt, Eintrag 2 Deutsch → Englisch, und so fort (`ExerciseContentProvider.cs:77–82`). Jedes Wort kommt
also nur in einer Richtung dran, und die Hälfte der Wörter schreibt das Kind nie auf Englisch, obwohl der
Vokabeltest der Klasse 7 genau das abfragt. Zu entscheiden: `both` umdeuten (jedes Wort zweimal, eigener
Lernstand je Richtung) oder die Richtung an die Position legen (zwei Positionen je Liste). Beides berührt
Lernstand und Leitner-Fortschritt je Eintrag.

## User Story

Als Vater möchte ich, dass jede Vokabel einer Wortliste in **beiden** Richtungen geübt und getestet wird, damit
mein Sohn im Vokabeltest das englische Wort auch richtig schreiben kann, wenn das deutsche gefragt ist.

## Ist-Stand am Code

Nachgesehen am 2026-11-11.

- Die Richtung steht in der Übung, nicht an der Position: `VocabularyConfig.Direction`, Werte
  `front-to-back | back-to-front | both` ([ExerciseConfigs.cs:24–25](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)).
- `both` dreht jede Karte mit **ungeradem** Index um, je Wort genau eine Richtung
  ([ExerciseContentProvider.cs:73–82](../../backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs)). Die
  Liste behält ihre Länge; ein Eintrag bleibt ein Index ([ExerciseContentResolver.cs:91–111](../../backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs)).
- Das Vater-Web beschriftet den Wert mit „beide“ ([exerciseConfig.tsx:462](../../frontend/src/vater/exerciseConfig.tsx),
  [VaterExerciseCreate.tsx:507](../../frontend/src/vater/VaterExerciseCreate.tsx)) – das verspricht mehr, als der Server tut.
- Der Lernstand hängt am **Index**: `PositionItemProgress` ist eindeutig je `(PlanPositionId, ItemIndex)`
  ([PuglingDbContext.cs:663](../../backend/Pugling.Api/Data/PuglingDbContext.cs)); Tagesdosis, Testportion und
  `ItemCount` zählen Indizes. Der plan-übergreifende Stand `ItemProgress` ist eindeutig je `(ChildId, ItemId)`
  ([PuglingDbContext.cs:673](../../backend/Pugling.Api/Data/PuglingDbContext.cs)) – ohne Richtung.
- Benutzt wird `both` nur von den vier Access-Wortlisten 13–16; sieben Vokabelübungen fragen `front-to-back`.
- Lernstand, der schon daran hängt: Plan 5, Stand heute 7 Zeilen `PositionItemProgress` (Übung 13 Index 0–2,
  Übung 14 Index 0–3) und 7 Zeilen `ItemProgress` für Kind 1.
- Die Tagesdosis des KI-Lehrplans rechnet ⌈Einträge / Tage⌉ (B-463, `PlanPlanner.BookListPosition`).

## Die echte Lücke

Es gibt keinen Weg, ein Wort in beiden Richtungen zu üben. `both` mischt nur die Richtungen über die Liste, und die
Beschriftung „beide“ lässt den Vater das Gegenteil annehmen.

## Offene Punkte

1. ~~**Wo sitzt „beide Richtungen“?**~~ (a) `both` bedeutet künftig: jedes Wort zweimal, je Richtung eine Karte in
   derselben Position. (b) Die Richtung wird an der Position einstellbar, und der Plan bekommt je Liste zwei
   Positionen. (c) Kein Code: die Access-Listen fragen nur noch Deutsch → Englisch (`back-to-front`).
   *Empfehlung:* (a). Die Beschriftung „beide“ verspricht genau das, im Plan bleiben es vier Pflichten statt acht.
   Kosten: Die Liste verdoppelt sich, Indizes verschieben sich.
2. ~~**Reihenfolge der zwei Karten eines Wortes:**~~ zusammen (Wort *i* als Index 2*i* und 2*i*+1) oder erst die ganze
   Liste vorwärts, dann rückwärts? *Empfehlung:* zusammen. Sonst kommt die Gegenrichtung der ersten Wörter erst nach
   Ablauf der ganzen Liste dran.
3. ~~**Was zählt die Tagesdosis:**~~ Karten oder Wörter? *Empfehlung:* Karten, wie heute. Der Agent verdoppelt die Dosis
   für `both`, damit die Liste weiter bis Planende durch ist (für Access Unit 1 rund 18 Karten bzw. 9 Wörter am Tag).
4. ~~**Plan-übergreifender Stand je Richtung?**~~ `ItemProgress` kennt keine Richtung, beide Karten eines Wortes
   schreiben in dieselbe Zeile. *Empfehlung:* vorerst so lassen (keine Migration); die schwache Richtung zieht
   die Beherrschung des Wortes herunter, und das ist für „schwache Wörter“ richtig.
5. ~~**Was passiert mit dem Lernstand von heute?**~~ *Empfehlung:* umschreiben statt verwerfen: alter Index *i* →
   neuer Index 2*i* (vorwärts) bzw. 2*i*+1 (wo die alte Karte umgedreht war). Es sind 7 Zeilen.

Alle fünf am 2026-11-11 vom Nutzer entschieden, jeweils die Empfehlung → Entscheidungen 1–5.

## Entscheidungen

1. **`both` heißt künftig: jedes Wort in beiden Richtungen, in derselben Position.** Eine Liste mit *n* Einträgen
   spielt 2*n* Karten. *Begründung:* Die Beschriftung „beide“ im Vater-Web verspricht genau das, und der Plan behält
   vier Pflichten statt acht. *Kosten:* Inhaltsliste, Kartenzahl (`ItemCount`) und alles, was am Index hängt,
   müssen die Verdopplung kennen; `front-to-back` und `back-to-front` bleiben unverändert.
2. **Die zwei Karten eines Wortes liegen zusammen:** Wort *i* ist Karte 2*i* (Lernsprache → Muttersprache) und
   2*i*+1 (umgekehrt). *Begründung:* Ein neues Wort kommt am selben Tag in beiden Richtungen dran, statt dass die
   Gegenrichtung erst nach Ablauf der ganzen Liste käme. *Kosten:* keine über Entscheidung 1 hinaus.
3. **Die Tagesdosis zählt Karten, wie bisher.** Der KI-Lehrplan rechnet die Dosis aus der Kartenzahl, für eine
   `both`-Liste also doppelt (Access Unit 1 bei 14 Tagen: 6 + 7 + 3 + 3 = 19 Karten, rund 9 Wörter am Tag).
   *Begründung:* Am Server ändert sich an der Dosis nichts. *Kosten:* Der Vater muss wissen, dass „neue Inhalte je
   Tag“ bei „beide“ Karten meint – das sagt die Feld-Erklärung.
4. **Der plan-übergreifende Lernstand bleibt je Wort**, beide Richtungen schreiben in dieselbe `ItemProgress`-Zeile.
   *Begründung:* keine Schemaänderung; die schwächere Richtung zieht die Beherrschung des Wortes herunter, und das
   ist für „schwache Wörter“ gewollt. *Kosten:* Welche Richtung hakt, zeigt nur der Positions-Bericht, nicht der
   Vokabel-Fortschritt.
5. **Der vorhandene Lernstand wird umgeschrieben, nicht verworfen:** alte Karte *i* → 2*i*, wo sie vorwärts gefragt
   war (*i* gerade), → 2*i*+1, wo sie umgedreht war (*i* ungerade). Betroffen sind alle gespeicherten Kartennummern
   von Positionen auf `both`-Übungen: `PositionItemProgress.ItemIndex`, `TestItemResult.ItemIndex` und die
   eingefrorenen Reihenfolgen `PracticeSessions.Order` und `TestAttempts.Order`. *Begründung:* Kein Lernstand geht
   verloren. *Kosten:* Die Umschreibung ist **nicht** idempotent – am Datenbestand ist nicht zu erkennen, ob eine
   Zeile schon umgerechnet ist. Sie läuft darum nicht bei jedem Start, sondern **einmal** als Skript bei
   angehaltenem Server, zusammen mit dem Ausrollen des neuen Codes (Abweichung von der Formulierung „beim Start“ in
   der Frage an den Nutzer).

## Akzeptanzkriterien

1. Eine Vokabelübung mit `both` und *n* Einträgen spielt 2*n* Karten: Karte 2*i* fragt Wort *i* vorwärts, Karte
   2*i*+1 rückwärts. `front-to-back` und `back-to-front` verhalten sich wie vorher.
2. Leitner-Stand, Tagesdosis, Testportion, Bericht und Vorschau zählen jede Richtung als eigene Karte; die
   Kartenzahl im Katalog (`itemCount`) ist 2*n*.
3. Bild, Audio und Artikel-Regel folgen der Richtung der jeweiligen Karte wie bisher.
4. Der KI-Lehrplan setzt die Dosis für eine `both`-Liste aus der Kartenzahl.
5. Ein Umschreib-Skript rechnet den Bestand einmal um; danach zeigt Plan 5 denselben Stand wie vorher (Box,
   Testergebnisse), und die Gegenrichtung dieser Wörter ist neu.
6. Das Vater-Web beschreibt „beide“ als „jedes Wort in beide Richtungen“, die Feld-Erklärung der Tagesdosis nennt
   die Karten.

## Schätzung

**Größe: M** — die Richtung wird an einer Stelle zur Verdopplung (Inhalts-Auflösung, `ItemCount`), dazu ein
Umschreib-Skript, die Dosis im Agenten und zwei Beschriftungen; Anker B-03 (M). `migration: nein` (keine
Schemaänderung, nur Daten), `vertragsbruch: nein` (dieselben Felder, `both` ändert seine Bedeutung).

**Risiken:** Stellen, die eine Kartennummer direkt als Zeile der Übung lesen statt über die aufgelöste
Inhaltsliste (etwa „anderes Bild“ je Karte, Vorschau, Bericht) – sie müssen beim Bau einzeln gefunden werden.
Ablenker im Multiple-Choice kämen sonst aus der falschen Sprache. Das Umschreib-Skript an der echten Datenbank
läuft nur einmal und nur mit Sicherung davor.

**Angriffsplan:** (1) Regressionstests rot: `both` liefert 2*n* Karten in der Paar-Reihenfolge, Kartenzahl im
Katalog. (2) Inhalts-Auflösung und `ItemCount` umbauen, Fundstellen mit Zeilen-Zugriff angleichen. (3) Agent:
Dosis aus der Kartenzahl. (4) Frontend: Beschriftung und Feld-Erklärung. (5) Umschreib-Skript, an einer Kopie der
Datenbank gemessen. (6) Ausrollen beim Nutzer: Server anhalten, sichern, Skript, Neustart.

**Testweg:** Integrationstests in `Pugling.Api.Tests` (Übungsrunde, Test, Dosis, Katalog, Vorschau auf einer
`both`-Übung), `CreatorAgentTests` für die Dosis, Vitest für die Beschriftung, volle Suite, `pugling-reviewer`
und `frontend-reviewer`, Messung des Skripts an einer DB-Kopie mit dem Stand von Plan 5, Rollengang als Sohn.

## Verlauf

- **2026-11-11** — angelegt (beim Ausformulieren von B-463).
- **2026-11-11** — ausformuliert: `both` heißt abwechselnd, das UI sagt „beide“; Lernstand hängt am Index; fünf Fragen
  an den Nutzer.
- **2026-11-11** — gegrillt im Dialog: Nutzer nimmt alle fünf Empfehlungen; geschätzt M, ohne Migration und ohne
  Vertragsbruch, mit einmaligem Umschreib-Skript für den Bestand.
