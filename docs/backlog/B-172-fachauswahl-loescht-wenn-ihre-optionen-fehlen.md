---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/creator]
aliases: [leeres Fach-Select loescht, subjects.data ?? [], Anzeige und Formularzustand driften]
status: ausformuliert
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-13 zu B-123
grund: ""
ersetzt_durch: []
entgangen_bei: [B-123, B-148]
---

# B-172 · Die Fachauswahl löscht das Fach, wenn ihre eigenen Optionen nicht geladen sind

`?? []` zieht „noch nicht geladen" und „das ist die vollständige Liste" zusammen. Vor B-123 war das
harmlos — das Fach-Auswahlfeld gab es nur beim **Anlegen**, und „keine Auswahl" hieß dort „ohne Fach
anlegen". B-123 und B-148 haben aus demselben Feld ein **löschendes** Bedienelement gemacht, dreimal.

## User Story

Als **Creator** möchte ich, dass ein Auswahlfeld mir nie einen Wert anzeigt, den ich nicht gewählt habe,
damit ich das Fach einer geteilten Reihe nicht in dem Moment zerstöre, in dem ich es zu reparieren
versuche.

## Ist-Stand am Code

Die Kette der Aufnahme war an vier Gliedern behauptet. **Drei stimmen, das vierte nicht** — und die
Wirklichkeit ist an einer Stelle harmloser und an zwei Stellen schlimmer als die Behauptung. Gemessen am
2026-08-31 mit einer Wegwerf-Probe gegen `SeriesForm` (fünf Fälle, danach gelöscht).

### Was stimmt

- `frontend/src/vater/VaterLehrwerke.tsx:164` und `:178` reichen `subjects.data ?? []` und
  `publishers.data ?? []` durch; die Abrufe stehen auf `:113`/`:114`.
- `useAsync` lässt `data` bei einem Fehler auf `null` und hat keinen erneuten Versuch
  (`frontend/src/lib/useAsync.ts:18`, `:29-36`).
- **Kein Fehler ist sichtbar.** Die Seite rendert `list.error` (`:157`) und `units.error` (`:458`) —
  `subjects.error` und `publishers.error` **nirgends**. Dasselbe in den beiden Schwesterseiten:
  `VaterKind.tsx` zeigt `child.error` (`:44`) und `interests.error` (`:83`), nicht `subjects.error`
  (`:35`); `VaterFachlehrer.tsx` zeigt nur `list.error` (`:60`), nicht `subjects.error`/`series.error`
  (`:38`/`:39`).
- Der Server räumt beim Schalter beide Spalten: `TextbookSeriesController.cs:236`,
  `TextbooksController.cs:113`, `CreatorProfilesController.cs:148`.

### Was nicht stimmt: das Feld steht nicht leer da, es behauptet einen Wert

Die Aufnahme nahm an, der `<select>` stehe bei fehlender Option leer. Er tut es nicht. Gemessen:

| Probe | Aufbau | Ergebnis |
|---|---|---|
| A | `subjects={[]}`, `series.subjectId = 1` | `value === ""`, `selectedIndex === 0`, angezeigt: **„– keine Angabe –"** |
| B | nur den Namen ändern, Fach-Feld nie anfassen | `{"name":"Access 2"}` — **kein** `clearSubject` |
| C | Liste ohne das eigene Fach; Nutzer probiert „Mathe" und geht zurück | **`{"clearSubject":true}`** |
| D | derselbe Umweg beim Verlag | **`{"clearPublisherId":true}`** |
| E | Liste kommt nach der Montage nach | Anzeige springt von selbst auf **„Englisch"** zurück |

Ein `<select>` mit Anzeigegröße 1 und ohne ausgewählte Option wählt die erste nicht gesperrte selbst —
Standardverhalten, nicht jsdom-Eigenart. Der React-Zustand `form.subjectId` bleibt dabei `"1"`.
**Anzeige und Formularzustand driften auseinander**, und der Diff in `seriesPatch` rechnet gegen den
Zustand, nicht gegen die Anzeige.

Drei Folgen, jede gemessen:

1. **Probe B entlastet den Normalfall.** Ein Speichern, das das Feld nicht anfasst, schickt kein
   `clearSubject`. Der in der Aufnahme behauptete Schaden („Gespeichert." — und die Reihe hat ihr Fach
   verloren) folgt so **nicht**.
2. **Probe C belastet den reparierenden Nutzer.** Der in der Aufnahme genannte Auslöser („wählt die
   einzige Option") feuert **kein** `change` — die Option ist ja schon gewählt. Der echte Auslöser ist ein
   **Umweg**: irgendeine andere Option, dann zurück auf „– keine Angabe –". Genau das tut jemand, dem das
   Feld gerade „kein Fach" anzeigt.
3. **Probe E schließt das Ladefenster aus.** Trifft die Liste später ein, springt die Anzeige von selbst
   auf den richtigen Wert. Der Schaden braucht eine Liste, die **leer bleibt** — also den Fehlerfall.

### Die Fläche ist sechs Felder, nicht eins

`subjectField.ts:6-9` begründet, warum nur das Fach eine geteilte Regel bekam: von sieben Schaltern seien
sechs heil, denn „kaputt wird es erst bei einem Feld, dessen **Ladezustand das Formular nicht abbilden
kann**, und das ist heute nur `subjectId`". **Der zweite Halbsatz ist widerlegt** (Probe D). Jedes
Referenzfeld, dessen Optionen aus einem eigenen, unabhängig scheiternden Abruf kommen, trägt denselben
Zustand:

| Formular | Felder | Optionen aus | Schalter |
|---|---|---|---|
| `SeriesForm` (`VaterLehrwerke.tsx:355-380`) | Verlag, Fach | `:113`/`:114` → `:164` | `clearPublisherId` (`seriesPatch.ts:79-82`), `clearSubject` (`:84`) |
| Lehrbuch am Kind (`ChildMaterialSection.tsx:225-246`) | Fach, Reihe | `VaterKind.tsx:69`, `ChildMaterialSection.tsx:86` | `clearSubject`, `clearSeries` (`textbookPatch.ts:65`, `:71-74`) |
| Fachlehrer-Profil (`VaterFachlehrer.tsx:305-315`) | Fach, Reihe | `:93`/`:114` | `clearSubject`, `clearSeries` (`profilePatch.ts:76`, `:78-81`) |

Beim Lehrbuch hängt ein Folgeschaden dran: die Reihe zu wechseln räumt zusätzlich die aktuelle Unit
(`ChildMaterialSection.tsx:163`), und `clearSeries` tut serverseitig dasselbe.

Gemessen ist `SeriesForm` (Proben A–E). Die beiden anderen sind **strukturell** belegt — gleiche
Feldform, gleicher `subjectPatch`, gleiches `?? []`, kein gerenderter Fehler — aber nicht gefahren.

### Warum kein Fach-Löschen den Zustand erzeugt

Nachgesehen, weil es die Größe entschieden hätte: `creator/subjects` liefert **alle** Fächer, ungefiltert
und ohne Paging (`SubjectsController.cs:36-37`). Und ein gelöschtes Fach setzt an allen drei Trägern nur
den Fremdschlüssel auf `null` (`PuglingDbContext.cs:198`, `:241`, `:272` — je `SetNull`), während
`SubjectName` als **gespeicherte Spalte** stehen bleibt: dann greift der Freitext-Zustand aus B-143, und
das Feld sagt die Wahrheit. **Es gibt also genau einen Weg in den Zustand — der gescheiterte Abruf.**

## Die echte Lücke

Nicht „ein leeres Feld löscht", sondern: **das Formular zeigt einen Wert an, den niemand gewählt hat, und
sagt nicht, dass es die Grundlage seiner Anzeige nie bekommen hat.** Der Nutzer trifft danach jede
Entscheidung auf falscher Information — und *beide* Auswege zerstören: eine andere Option überschreibt das
Fach, „– keine Angabe –" räumt Id **und** Rückfall-Namen.

Zwei Umstände machen daraus einen P1 statt eines Schönheitsfehlers:

- **Die Reihe ist geteilt.** Der Schaden trifft nicht den Verursacher, sondern jeden, der die Reihe nutzt:
  ohne Fach trägt sie keine Übungen mehr (`series_without_subject`, `VaterLehrwerke.tsx:397`).
- **Er ist nicht rekonstruierbar.** Mit dem Namen fällt der letzte Zeuge dessen, was dastand.

Die Zeile daneben weiß es besser: `VaterLehrwerke.tsx:207` zeigt den Fachnamen aus `series.subjectName`,
wenn die Liste ihn nicht kennt. Der Rückfall existiert also schon — er steht nur in der Liste und nicht im
Formular.

**Warum keine Testebene das sah:** `seriesPatch`/`subjectPatch` kennen die Options-Liste nicht, und
`SeriesForm.test.tsx:34` übergibt immer ein gefülltes `SUBJECTS`. Die Fälle in `subjectField.test.ts:39-76`
prüfen die Regel — und die Regel ist richtig. Falsch ist die Eingabe, die das Formular ihr liefert.

## Offene Punkte

1. ~~Trifft die Kette wirklich zu?~~ **Gemessen** (Proben A–E, siehe Ist-Stand): ja im Kern, nein im
   Auslöser. Der Schaden braucht einen Umweg über eine andere Option, nicht einen einzelnen Griff.
2. ~~Gilt dasselbe an weiteren Stellen?~~ **Gemessen**: drei Formulare, sechs Referenzfelder, und in
   keiner der drei Seiten wird der Fehler der Options-Liste gerendert.
3. **Sperren oder Fehler zeigen?** Empfehlung: **beides, aber die Sperre ist die tragende Hälfte** — ein
   Fehlerbanner allein lässt das löschende Feld bedienbar, und Probe C zeigt, dass der Nutzer es genau
   dann anfasst. Also: `subjects.error`/`publishers.error`/`series.error` als `banner err` rendern (Muster
   `VaterKatalog.tsx:29`) **und** das Feld sperren, solange die Liste nicht geladen ist. Kosten: der
   Ladezustand muss bis ins Formular durchgereicht werden — heute geht nur `data ?? []` hinein, also eine
   neue Prop je Formular.
4. **Wie unterscheidet das Formular „lädt noch" von „ist fehlgeschlagen"?** Beides kommt heute als `[]`
   an. Empfehlung: **`data` unverfälscht durchreichen** (`SubjectResponse[] | null`) statt `?? []` — dann
   trägt der Typ die Unterscheidung, und das `?? []` verschwindet dort, wo es schadet. Kosten:
   Signaturwechsel an sechs Übergabestellen. Alternative (billiger, schwächer): eine
   `subjectsLoading`-Prop, die den Unterschied nur behauptet.
5. **Reicht die Sperre, oder braucht das Feld einen Rückfall wie bei B-143?** Empfehlung: **Sperre
   genügt** — eine gesperrte Option „Englisch (nicht geladen)" wäre eine vierte Bedeutung im selben Feld,
   und im Unterschied zum Freitext ist dieser Zustand *vorübergehend*. Zu klären ist das trotzdem, weil
   das Feld dann während des ganzen Ausfalls unbedienbar ist.
6. **Eine Story oder drei?** Empfehlung: **eine** — es ist eine Fehlerklasse, ein Muster und dieselbe
   Behebung, und die drei Formulare teilen sich `subjectField.ts` schon. Die Alternative erzeugt drei
   Stories, die alle dieselbe Entscheidung 3 bräuchten.
7. **Zieht die Begründung in `subjectField.ts:6-9` nach?** Sie ist durch Probe D widerlegt und begründet
   heute das Gegenteil dessen, was gilt. Empfehlung: **mitkorrigieren, im selben Diff** — das ist genau
   die Fehlerklasse „ein Kommentar begründet eine Bedingung, ohne sie zu prüfen", die die Retrospektive
   vom 2026-08-14 als gemeinsame Wurzel benannt hat.

## Akzeptanzkriterien (Entwurf, bis `gegrillt` nicht verbindlich)

1. Scheitert `GET creator/subjects`, steht auf der Lehrwerk-Seite ein sichtbarer Fehler — und das
   Fach-Feld im Bearbeiten-Formular ist **nicht bedienbar**.
2. Dasselbe für Verlag, für das Lehrbuch am Kind (Fach + Reihe) und für das Fachlehrer-Profil
   (Fach + Reihe).
3. Ein Formular, dessen Options-Liste den geladenen Wert nicht enthält, zeigt **nie** „– keine Angabe –",
   ohne dass der Nutzer das gewählt hat.
4. Ein Regressionstest, der **vorher rot** ist: Probe C in Testform — leere/unvollständige Liste, Umweg
   über eine andere Option, Erwartung „kein `clearSubject` im Rumpf" bzw. „das Feld ist gesperrt".
5. Die Begründung in `subjectField.ts` sagt, was gilt.
6. Alles bisher Grüne bleibt grün (831 Backend, Vitest, E2E).

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-123. Bleibt `unverifiziert`: Die Fundorte sind
  benannt, aber ich habe die Kette nicht selbst gefahren — und eine Kette mit vier Gliedern ist genau die
  Sorte Behauptung, die man nicht abschreibt.
- 2026-08-31 · `idee → ausformuliert`. Kette mit einer Wegwerf-Probe gegen `SeriesForm` gefahren (fünf
  Fälle, danach gelöscht). **Die Prämisse fiel**: der `<select>` steht nicht leer, er zeigt
  „– keine Angabe –" an und driftet damit vom Formularzustand ab; der behauptete Auslöser feuert kein
  `change`, der echte ist ein Umweg; ein Speichern ohne Griff ins Feld ist harmlos. Dafür ist die Fläche
  **sechsmal so groß** wie behauptet (drei Formulare, sechs Referenzfelder), und die Begründung in
  `subjectField.ts:6-9` ist widerlegt. `prio` bleibt **P1**: der engere Auslöser wird durch die breitere
  Fläche und den unwiederbringlichen Schaden an einer geteilten Reihe aufgewogen.
