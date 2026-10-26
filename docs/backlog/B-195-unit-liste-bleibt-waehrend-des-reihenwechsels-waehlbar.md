---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [halbe Reparatur units.error, alte Units waehrend des Wechsels]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-01 zu B-172
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-172]
wartet_auf: ""
nachgeschaut: 2026-10-26
---

# B-195 · Beim Reihenwechsel bleiben die Units der alten Reihe wählbar

Die Reparatur aus B-172 ist an dieser Stelle **halb**: Sie deckt den Fehlerfall, nicht das Ladefenster.

## User Story

Als **Vater** möchte ich beim Wechsel der Reihe keine Unit angeboten bekommen, die zur vorigen Reihe
gehört, damit mein Speichern nicht mit einer Fehlermeldung endet.

## Ist-Stand am Code

`ChildMaterialSection.tsx:291` schreibt `units.error ? [] : units.data ?? []`. Das entschärft die
veraltete Liste im **Fehlerfall** — aber nicht während des laufenden Abrufs:

- `useAsync` lässt `data` über den Wechsel stehen,
- und setzt `error` beim Start jedes Laufs auf `null`.

Während des Reihenwechsels bietet das Feld also weiter die Units der **alten** Reihe an, **wählbar**.

**Der Schaden ist serverseitig gedeckelt**, und auch das ist nachgerechnet: `TextbooksController.cs:146-147`
weist eine Unit aus einem fremden Werk mit `400` ab. Es ist also ein **vermeidbarer Fehlschlag**, keine
Zerstörung — darum P3 und nicht höher.

**Dieselbe Wurzel wie [B-164](B-164-useasync-paart-frischen-zustand-mit-alten-daten.md)** („frischer
Zustand mit alten Daten"), das die gemessene Breite trägt (110 `useAsync`-Aufrufstellen, 67 mit
Abhängigkeiten). Diese Story ist der eine Fall, der in **abgenommener Arbeit** sitzt und darum die
Wirkungszahl des Bereichs betrifft — B-164 selbst trägt kein `entgangen_bei`.

## Offene Punkte

1. ~~Hier einzeln lösen oder auf B-164 warten?~~ → **Keins von beidem.** Beim Bauen zeigte sich, dass hier
   die *einfachere* der beiden Hausregeln greift — siehe Entscheidung 1.

## Entscheidungen

1. **Die Option filtert nach ihrem Elternteil, sie wartet nicht auf ein Signal.**
   Begründung: `SeriesUnitResponse` trägt seit jeher ein `SeriesId`
   ([TextbookSeriesDtos.cs:50](../../backend/Pugling.Contracts/Creator/TextbookSeriesDtos.cs)) — die Option
   **weiß also selbst**, zu welcher Reihe sie gehört. Damit gilt die im Startkontext des Frontends
   festgeschriebene Regel wörtlich: „Die Zeile muss ihr Elternteil mitbringen und bei Nichtpassung nicht
   rendern (`c.subjectId === subjectId`, B-170); ein `disabled` sperrt nur die Folge."
   ([frontend/CLAUDE.md](../../frontend/CLAUDE.md)). Ein Schlüsselvergleich wie in `VaterWizard` (B-169)
   ist die Antwort für Listen, deren Zeilen ihr Elternteil **nicht** tragen — dort ist er nötig
   ([B-167](B-167-katalogliste-zeigt-beim-filterwechsel-alte-treffer.md)), hier wäre er der schwerere
   Weg zum selben Ziel.
   **Kosten:** Zwei verschiedene Mechanismen im selben Haus für scheinbar dieselbe Frage. Was sie trennt,
   ist eine Eigenschaft der Daten (trägt die Zeile ihren Schlüssel?), nicht Geschmack — und sie steht als
   Unterscheidung in beiden Stories. Wer das übersieht, baut den schwereren Weg, wo der leichte reicht.
2. **Der Elternschlüssel wird `unitOptions` als Pflicht-Parameter mitgegeben**, nicht am Aufrufer
   vorgefiltert.
   Begründung: „mechanische Tore statt Disziplin" ([CLAUDE.md](../../CLAUDE.md)). Am Aufrufer gefiltert
   kann die **zweite** Aufrufstelle es vergessen — genau der Weg, auf dem B-172 seinen achten Fall verlor
   ([B-194](B-194-plandetail-kinderauswahl-ohne-rueckfall.md)). Als Parameter kann sie es nicht: der
   Compiler verlangt ihn.
   **Kosten:** Die Signatur wird breiter, und `unitOptions` hat heute nur **einen** Aufrufer — das Tor
   zahlt sich erst beim zweiten aus. Der Preis ist eine Zeile; die Alternative ist eine Regel, die genau
   an der Stelle greifen müsste, an der sie schon einmal nicht gegriffen hat.
3. **Der Fehlerzweig aus B-172 (`units.error ? []`) bleibt unangetastet.**
   Begründung: Der Filter deckt den Dep-Wechsel, nicht die Frage „was zeigen wir nach einem gescheiterten
   Abruf" — das hat B-172 entschieden, und diese Story hat keinen Anlass, es neu zu entscheiden.
   **Kosten:** Zwei Bedingungen an derselben Zeile, die auf den ersten Blick redundant wirken. Ein
   Kommentar sagt, warum jede für sich nötig ist.

## Akzeptanzkriterien

1. Wechselt die Reihe, bietet die Unit-Auswahl **keine** Unit der vorigen Reihe mehr an — auch nicht
   während der Abfrage.
2. Ohne gewählte Reihe bietet sie gar nichts an (der Beschriftungstext sagt „erst Reihe wählen").
3. Die Units der **richtigen** Reihe erscheinen unverändert vollständig.
4. `unitOptions` lässt sich ohne Elternschlüssel nicht mehr aufrufen.
5. Ein Test deckt jeden Fall ab; je Fall ist benannt, welche Änderung ihn rot macht.

## Schätzung

**Größe: XS** — ein Parameter, ein Filter, ein Aufrufer, eine neue Testdatei.

- **`migration: nein`** · **`vertragsbruch: nein`** · **`wo: frontend`**.
- **Risiko:** Der Rückfall-Wert der `ReferenceSelect` (die *gespeicherte* Unit) hängt an einer eigenen
  Bedingung (`eigeneUnit`) und darf nicht mitgefiltert werden — sonst verschwände die gespeicherte Unit
  aus ihrer eigenen Reihe. Akzeptanzkriterium 3 deckt das.
- **Angriffsplan:** (1) `unitOptions(units, seriesId)` mit Pflicht-Parameter; (2) Aufrufer nachziehen;
  (3) `frontend/src/lib/referenceOptions.test.ts` neu.
- **Testweg:** `frontend/src/lib/referenceOptions.test.ts` (neu) — die Regel ist eine reine Funktion und
  gehört auf diese Ebene. Kein E2E: das Ladefenster einer Unit-Liste ist zu kurz, um es ohne künstliche
  Verzögerung zu treffen, und der Wert läge unter den Kosten; der Fehlerzweig ist bereits von
  `e2e/optionsliste-faellt-aus.spec.ts` (B-172) gedeckt.

## Verlauf

- 2026-09-01 · Aufgenommen aus der **Nachschau** zu B-172 (Nachtlauf). Die Zeile stammt aus dem
  Abnahme-Commit `289b000`, darum `entgangen_bei: [B-172]`.
- **2026-09-03** — Nachtlauf: autonom gegrillt (Freigabe 1, `art: Defekt`) und geschätzt. Der offene Punkt
  löste sich beim Nachsehen anders auf als vermutet: Eine Unit **trägt ihre Reihe** (`SeriesUnitResponse.
  SeriesId`), also greift hier die einfachere der beiden Hausregeln — Filtern am Elternteil statt
  Schlüsselvergleich. Der Vorschlag der Ausformulierung („warten auf B-164") wäre der schwerere Weg zum
  selben Ziel gewesen.

## Was der Review noch gefunden hat

**Der Filter allein reichte nicht** — er hat die Optionen geräumt, aber zwei Aussagen daneben stehen
lassen, die dadurch erst falsch wurden:

1. Der Satz „Diese Reihe hat noch keine Units – unter „Lehrwerke" anlegen." las `units.data` weiterhin
   **roh**. Beim Wechsel von einer Reihe *ohne* Units zu einer *mit* Units behauptete er im Ladefenster
   das Gegenteil der Wahrheit und schickte den Vater zum Anlegen. Jetzt schweigt er, solange die Antwort
   nicht da ist. Bewusst **nicht** auf die gefilterte Liste umgestellt: die ist im Fenster immer leer, und
   der Satz erschiene dann bei *jedem* Reihenwechsel kurz.
2. Die jetzt korrekt geleerte Auswahlliste hatte **kein Ladesignal** — „– keine –" liest sich im Fenster
   als „diese Reihe hat keine Units". Das ist die B-116-Falle („stumm zu leeren wäre die andere Falle"),
   die `AddPosition` mit „(Suche läuft…)" vermeidet und die hier offen geblieben war. Jetzt „– lädt… –".

**Rote Probe:** Filter aus `unitOptions` entfernt — erwartet 2 Fälle, **gemessen 2**.

- **2026-09-03** — **abgenommen** (Nachtlauf, Sprint 1). Belege: `tsc -b` sauber, `vitest run`
  **357/357** grün, `playwright test` **52/52** grün. `frontend-reviewer` gelaufen, **kein Blocker**;
  seine Funde sind eingearbeitet (siehe unten). **Rollengang:** die vier Fälle in
  `e2e/planbau-ladefenster.spec.ts` fahren die echte Oberfläche im echten Browser gegen einen echten
  Server — das ist der Rollengang, und er wiederholt sich, statt einmal stattzufinden. Kein
  Browser-Handgang möglich (keine Chrome-Anbindung in dieser Sitzung), damit greift der dokumentierte
  Ersatz. Commit `a7e3da6`; Protokoll `docs/pm-sitzung-2026-09-03-nacht.md`.
- **2026-10-26** — Nachschau (sechster Nachtlauf, Sprint 11, [Protokoll](../pm-sitzung-2026-10-26-nacht.md)): AK 1/2/4 nachgerechnet: `referenceOptions.ts:42-48` (Pflicht-Parameter `seriesId`, Filter `u.seriesId === seriesId`) gegen `referenceOptions.test.ts:37-46` – ohne Filter fällt der Fall „andere Reihe“; der Aufruf `ChildMaterialSection.tsx:329` reicht den Schlüssel. Grenzfall „Satz ‚noch keine Units‘ im ersten Render nach dem Wechsel, bevor `useAsync` `loading` setzt“ (`:340`, liest `units.data` roh): bei einer diskreten Eingabe spült React die passiven Effekte vor dem Zeichnen, darum kein sichtbares Fenster. AK 3 (gespeicherte Unit) hängt an `eigeneUnit` (`:206`) und nicht am Filter. Kein Befund.
