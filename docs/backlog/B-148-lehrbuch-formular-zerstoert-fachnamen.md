---
tags: [typ/story, status/in-arbeit, bereich/frontend, bereich/katalog, rolle/supervisor, rolle/creator]
aliases: [ChildMaterialSection clearSubject, Lehrbuch verliert Fachnamen, B-143 am Kind, Fachlehrer verliert Fachnamen, Freitext-Fach am Kind]
status: in-arbeit
prio: P2
art: Defekt
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer im Nachtlauf Sprint 3 (2026-08-10), Fund neben dem Diff von B-143
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-148 · Das Lehrbuch-Formular am Kind zerstört den Fachnamen bei jedem Speichern

Dieselbe Fehlerklasse wie [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md), eine Datei weiter
— und **ohne** den Schutz, der dort gratis abfiel. Vom `frontend-reviewer` gefunden, während er B-143
prüfte.

## User Story

Als **Supervisor** möchte ich am Lehrbuch meines Kindes eine Notiz ändern können, ohne dabei die
Fachangabe zu verlieren, die ich nie angefasst habe.

## Ist-Stand am Code

Stand `d36a11a`, alles am Code nachgesehen statt aus dem Review-Befund übernommen.

**Der Defekt, an zwei Stellen statt einer.** Beide Formulare bauen den Lösch-Schalter aus dem
**aktuellen Formularwert** statt aus einem Vergleich gegen den Ladezustand:

- `frontend/src/vater/ChildMaterialSection.tsx:165-175` (Lehrbuch am Kind) —
  `clearSubject: dto.subjectId == null`, dazu `clearGrade`, `clearSeries`, `clearUnit` nach demselben Muster.
- `frontend/src/vater/VaterFachlehrer.tsx:245-251` (Fachlehrer-Profil) — **dieselbe Zeile**, dazu
  `clearSeries`, `clearGradeMin`, `clearGradeMax`. Das beantwortet den offenen Punkt 1: `CreatorProfile`
  trägt es, und zwar identisch.

Beide Fach-`<select>` kennen nur Katalog-Fächer plus „– keine Angabe –"
(`ChildMaterialSection.tsx:196-199`) — sie können das **Freitext-Fach** nicht darstellen.

> **Begriff:** Ein *Freitext-Fach* ist der Zustand `subjectId == null && subjectName != null`. Das Wort
> und der Sentinel dafür stammen aus [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md)
> (`seriesPatch.ts:33`, `FREETEXT_SUBJECT`); diese Story übernimmt es unverändert für Lehrbuch und
> Fachlehrer-Profil, statt daneben von einem „verwaisten Fach" zu sprechen.

**Die Kette** (für beide gleich):

1. Ein Lehrbuch bzw. Profil, dessen Fach gelöscht wurde, hat `subjectId: null` und
   `subjectName: "Englisch"` — `SetNull` räumt nur die Id
   ([B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md) hat dieses Löschverhalten ausdrücklich als
   richtig bestätigt; der Name ist die gewollte Rückfallebene).
2. Das Formular startet auf `subjectId: ""`, weil es den Zustand nicht darstellen kann.
3. **Jedes** Speichern eines beliebigen anderen Feldes schickt `clearSubject: true`.
4. Der Name ist weg, und daneben steht „Gespeichert.".

**Der Server ist nicht die Ursache und braucht keine Änderung.** `TextbooksController.cs:110-117` wendet
erst den Wert, dann den Schalter an und leitet den Namen anschließend gegen den **Ergebnis**-Zustand ab
(B-142); `CreatorProfilesController.cs:148` ist gleich gebaut. `clearSubject` nimmt Id **und** Name
absichtlich zusammen (`ProfileDtos.cs:52`, `CreatorProfileDtos.cs:46`) — das ist die richtige Semantik
für „Fach entfernen". Falsch ist allein, dass der Client sie ungefragt schickt.

**Warum nur beim Fach — und die Fehlerbedingung ist schärfer als „Momentanwert statt Vergleich".**
`clear…`-Schalter baut das Frontend an **sieben** Stellen: den beiden oben, `seriesPatch.ts:89-107`
(als Einziges per Vergleich), dazu `ClozeTexts.tsx:41,176`, `PlanPositions.tsx:247`,
`VaterClassTests.tsx:81` und `VaterVocab.tsx:414`. Sechs leiten aus dem Momentanwert ab und sind trotzdem
heil, weil ihre Werte im Formular **vollständig darstellbar** sind — `dto.X == null` heißt dort wirklich
„der Nutzer hat geleert". Die Bedingung lautet also nicht „Momentanwert", sondern *Momentanwert über ein
Feld, dessen Ladezustand das Formular nicht darstellen kann*. Das ist heute genau `subjectId`.

`clearSeries` ist **nachgemessen und ungefährlich**: `form.seriesId` wird aus `book?.seriesId`
initialisiert, nicht gegen die Reihenliste gefiltert. Fehlt die Reihe im Pulldown (`api.textbookSeries()`
deckelt bei `take=200`), bleibt die Id im State, `dto.seriesId` ist eine Zahl, der Schalter bleibt
`false`. Er feuert nur, wenn der Nutzer die Auswahl anfasst.

## Die echte Lücke

Der Unterschied zu B-143 ist der entscheidende: Dort **schützte der Diff-Vergleich** — `form` blieb gleich
`loaded`, also ging nichts mit, und der Defekt war „man kommt nicht heran". Hier gibt es keinen Vergleich,
sondern eine Ableitung aus dem Momentanwert. Der Defekt ist damit nicht „man kommt nicht heran", sondern
**aktive Zerstörung bei einer unbeteiligten Handlung** — deshalb `P2` statt `P3`.

Und die Lücke ist eine Ebene tiefer, als der Befund sie beschrieb: Es fehlt nicht ein Sonderfall für das
Fach, sondern die **Regel**, dass ein `Clear…`-Schalter aus einem *Vergleich* entsteht. B-143 hat sie in
`seriesPatch.ts` für die Reihe schon gebaut und mit Tests belegt — sie steht dort als lokale Lösung einer
Datei, obwohl inzwischen drei Formulare dieselbe Semantik bedienen.

**Der Vergleich allein reicht aber nicht, und das kippt die Empfehlung des Ausformulierens.** Dort stand,
die zwei Hälften (Vergleich, Sentinel) seien trennbar und der Vergleich sei die dringende. Gegengerechnet:
Mit reinem Diff gilt für ein Buch mit Freitext-Fach `loaded.subjectId === ""` **und**
`form.subjectId === ""` — unverändert, kein Schalter, der Name bleibt. Wählt der Nutzer jetzt aber
ausdrücklich „– keine Angabe –", ändert sich nichts, es wird nichts gesendet, und der Freitext-Name ist
**nicht wegzubekommen**. Das ist wörtlich der Defekt von B-143 („man kommt nicht heran"). Der Diff tauscht
die Zerstörung gegen die Unerreichbarkeit; erst der Sentinel macht die beiden Zustände unterscheidbar.

## Warum das niemandem aufgefallen ist

Die Verfolgung ist **verwaist**: [B-137](B-137-freitext-fach-unerreichbar.md) hielt unter Punkt 3 fest,
dass `CreatorProfile` und `Textbook` dieselbe Frage stellen, und vermerkte „reist mit B-144". B-144 nennt
sie in der gebauten Fassung in **keinem** Akzeptanzkriterium — beim Grillen wurde die Frage auf das
Löschverhalten verengt, und der Rest fiel zwischen die Stories.

**Kein `entgangen_bei`:** Der Zustand ist älter als B-143/B-144 und wurde von keiner Abnahme
durchgelassen — er wurde in einer Notiz verfolgt und dort vergessen.

## Offene Punkte

1. ~~**Trägt `CreatorProfile` dasselbe?** B-137 nannte beide in einem Atemzug; gemessen ist bisher nur
   `Textbook`.~~ **Beantwortet am 2026-08-11:** ja, `VaterFachlehrer.tsx:245-251`, identische Zeile.
   Die Story deckt beide Formulare ab — sie einzeln zu fahren hieße, dieselbe Regel zweimal zu bauen.
2. ~~**Übernimmt diese Story den Sentinel aus B-143 oder reicht der Diff-Vergleich?** Empfehlung: erst den
   Vergleich, den Sentinel nur bei Bedarf — die zwei Hälften sind trennbar.~~ **Die Empfehlung war
   falsch** und ist beim Grillen gegen den Code gefallen: Der Diff allein tauscht die Zerstörung gegen die
   Unerreichbarkeit (siehe „Die echte Lücke"). → Entscheidung 1.
3. ~~**Wird die Regel geteilt oder je Formular wiederholt?**~~ → Entscheidung 2. Die Frage nach dem
   *allgemeinen* Helfer hat sich dabei erledigt: Die Fehlerbedingung ist fach-spezifisch, nicht
   schalter-allgemein.
4. ~~**Soll der Zustand am Kind sichtbar werden?**~~ → folgt aus Entscheidung 1 (der Sentinel *ist* die
   Anzeige) und wird von Entscheidung 4 beschriftet.

## Entscheidungen

1. **Das vollständige B-143-Muster, beide Formulare, in einem Zug** — Sentinel `FREETEXT_SUBJECT` **und**
   Diff gegen den Ladezustand, nicht nur der Diff. **Begründung:** Der Zwischenstand „nur Diff" ist kein
   neutraler Teilfortschritt, sondern ein Defekt mit eigener Id: Er macht das Freitext-Fach unzerstörbar
   *und* unentfernbar, also genau B-143. Ihn absichtlich einzubauen hieße, eine schon bezahlte Story an
   zwei neuen Stellen zu wiederholen, und verletzte Akzeptanzkriterium 3 von vornherein.
   **Kosten:** Die Story ist größer als der Befund klang. Beide Formulare bauen ihren PATCH-Rumpf heute
   inline im `submit`; das Muster verlangt je eine `…FormValues`-Fassung des Ladezustands und eine reine
   Vergleichsfunktion — dieselbe Struktur, die `seriesFormValues`/`seriesPatch` für die Reihe haben,
   zweimal. Dazu die `disabled`-Option je Feld samt der Zusicherung, dass der Sentinel nie in den Rumpf
   gerät (`Number("__freetext__")` wäre `NaN`).
2. **Geteilt wird genau das Fach, nicht mehr.** Ein Modul (etwa `src/vater/subjectField.ts`) trägt
   `FREETEXT_SUBJECT`, die Ableitung `{subjectId, subjectName} → Formularwert` und den Patch-Zweig
   („leer → `clearSubject`", „Id → `subjectId`", „Sentinel → gar nichts"). Jedes Formular behält daneben
   seine **eigene** Patch-Funktion mit eigener Testdatei nach dem Muster `seriesPatch.ts`.
   **Begründung:** Ein allgemeiner „Schalter-aus-Vergleich"-Helfer wäre die Antwort auf eine
   Fehlerbedingung, die wir gerade widerlegt haben — die anderen sechs Schalter sind nicht kaputt, und
   sie für die Symmetrie anzufassen hieße, funktionierenden Code zu bewegen. Umgekehrt ist „der Sentinel
   darf nie in den Rumpf" keine Regel, die man dreimal unabhängig richtig hält.
   **Kosten:** `FREETEXT_SUBJECT` zieht aus `seriesPatch.ts` um; damit fallen `seriesPatch.ts`,
   `seriesPatch.test.ts` und `VaterLehrwerke.tsx` in den Diff — Code, der seit dem 2026-08-10 abgenommen
   ist und grüne Tests hat. Reine Import-Umstellung, aber sie muss im Review als solche erkennbar sein.
   Und `seriesPatch.ts` gibt ein Stück seiner heutigen Selbsterklärung an das neue Modul ab.
3. **Anlegen und Bearbeiten bleiben ein Formular.** Der Ladezustand wird eingefroren
   (`useRef`/`useState`-Initializer) und ist beim Anlegen `null`; `submit` verzweigt wie heute nach
   `book ?` bzw. `profile ?`.
   **Begründung:** Die Trennung bei der Reihe (`VaterLehrwerke.tsx:218`, `NewSeries` neben `SeriesForm`)
   ist mit *Feldsichtbarkeit* begründet — beim Anlegen einer Reihe gibt es Felder, die es nicht geben
   soll. Beim Lehrbuch und beim Profil ist das Formular in beiden Modi dasselbe; die Trennung verdoppelte
   also nur das Rendern. Der Bezugspunkt ist beim Fachlehrer ohnehin schon da
   (`VaterFachlehrer.tsx:180`), er muss nur erweitert werden.
   **Kosten:** Beide Formulare behalten ihre `book ? … : …`-Verzweigungen und bekommen eine dazu — genau
   die Unübersichtlichkeit, gegen die B-143 sich bei der Reihe entschieden hat; wir entscheiden hier
   bewusst anders. Die Patch-Funktion läuft nur auf dem Bearbeiten-Pfad, der Anlege-Pfad braucht seine
   eigene Zusicherung, sonst verschiebt sich die Lücke nur. Und der Einfrier-Grund aus
   `VaterLehrwerke.tsx:234-243` muss an beiden Stellen mitgeschrieben werden — ein mitlaufendes `useState`
   wäre der stille Rückfall in den Ausgangsdefekt. Die zwei Schreibweisen des Einfrierens (`useRef` bei
   der Reihe, `useState`-Initializer beim Fachlehrer) werden dabei vereinheitlicht oder begründet.
4. **Die Sentinel-Option heißt wortgleich „`<Fachname>` (Freitext)", in allen drei Feldern.**
   **Begründung:** Die Formulierung steht bereits als Nutzertext in `fieldHelp.ts:155`, als Zusicherung in
   `SeriesForm.test.tsx:42` und als E2E-Erwartung in `e2e/lehrwerk-bearbeiten.spec.ts:143`. „(Fach
   gelöscht)" wäre außerdem nicht immer wahr: Der Server nimmt einen `subjectName` ohne `subjectId`
   weiterhin an (`TextbooksController.cs:111`, gleiche Zeile im Profil-Controller) — nur die Oberfläche
   schickt keinen mehr. Ein gelöschtes Fach ist der häufigste Weg in den Zustand, nicht der einzige.
   **Kosten:** Das Etikett sagt *was*, nicht *warum*. Die Erklärung trägt die Feldhilfe, und die ist heute
   nur fürs Reihen-Feld geschrieben — Lehrbuch und Fachlehrer brauchen je einen eigenen `HelpTopic` mit
   demselben Gedanken in ihren Worten (`frontend/CLAUDE.md`: der Text steht nie am Feld, und zwei
   Formulierungen desselben Begriffs werden zwei Bedeutungen). Also zwei Einträge in `fieldHelp.ts` plus
   die `FieldLabel`-Umstellung der beiden Fach-Felder — Arbeit, die im Befund nicht vorkam.
5. **Der Satz in `VaterFachlehrer.tsx:196` wird mitgezogen.** Er sagt heute, ein Freitext-`subjectName`
   lasse sich im Pulldown nicht abbilden; nach Entscheidung 1 stimmt das nicht mehr.
   **Begründung:** Er beschreibt zwar einen anderen Fall (das Fach der gewählten *Reihe*, nicht das des
   Profils) und die Ableitungsregel bleibt unverändert — aber ein Kommentar, der eine widerlegte
   Unmöglichkeit behauptet, ist genau die Sorte, die beim nächsten Umbau falsch gelesen wird.
   **Kosten:** Eine Zeile Kommentar und der zugehörige Testname in `VaterFachlehrer.test.tsx:44`.

## Akzeptanzkriterien

1. Ein Lehrbuch mit Freitext-Fach behält Id-losen Namen und Anzeige, wenn ein **anderes** Feld geändert
   und gespeichert wird.
2. Dasselbe für ein Fachlehrer-Profil mit Freitext-Fach.
3. Das Fach-Feld zeigt den Zustand als vorausgewählte, gesperrte Option „`<Fachname>` (Freitext)" — in
   beiden Formularen, wortgleich zur Reihe.
4. Wählt der Nutzer ausdrücklich „– keine Angabe –", wird das Fach weiterhin geleert (Id **und** Name).
   Die Behebung darf den Weg nicht zumauern.
5. Der Sentinel erreicht den PATCH-Rumpf nie — auch nicht, wenn die `disabled`-Option umgangen wird.
6. Ein unverändertes Feld wird gar nicht gesendet (die Zusicherung des Diffs, wie bei der Reihe).
7. Das Anlegen funktioniert unverändert und schickt keinen `clear…`-Schalter.
8. Je Formular ein Regressionsfall, der vor der Behebung rot ist.

## Schätzung

**Größe: M**, `wo: frontend`, `migration: nein`, `vertragsbruch: nein`.

**Warum M und nicht S** — der naheliegende Vergleich ist B-143 (`S`), und er trägt nicht: B-143 war klein,
**weil B-123 die Struktur schon gebaut hatte**. `seriesPatch.ts` und die Trennung `NewSeries`/`SeriesForm`
existierten; B-143 musste nur den Sentinel einhängen. Hier fehlt die Struktur an **beiden** Formularen
komplett — je eine `…FormValues`-Ableitung, je eine Patch-Funktion, je ein eingefrorener Bezugspunkt. Das
ist mehr als zweimal B-143 und liegt damit beim Anker M (vokabel-basierter Batch-Pfad im `MediaSelector`,
B-03): eine überschaubare Zahl von Dateien, aber neue Struktur statt eines eingehängten Sonderfalls.

**Die beiden Flags sind nachgesehen, nicht vermutet.** Kein Schema wird angefasst — die Behebung ist
vollständig clientseitig (Entscheidungen 1–4 lassen den Server unverändert, siehe „Ist-Stand"). Und
`Pugling.Contracts` bleibt unberührt: `UpdateTextbookDto` (`ProfileDtos.cs:48-52`) und
`UpdateCreatorProfileDto` (`CreatorProfileDtos.cs:42-46`) tragen `ClearSubject` bereits; wir schicken den
Schalter künftig nur seltener, nicht anders.

### Risiken

1. **Frisch abgenommener Code im Diff.** Der Umzug von `FREETEXT_SUBJECT` fasst `seriesPatch.ts`,
   `seriesPatch.test.ts` und `VaterLehrwerke.tsx` an — B-123/B-143, abgenommen am 2026-08-10. Gegenmittel:
   Schritt 1 des Angriffsplans ist eine **reine** Umstellung, und ihre Zusicherung ist, dass
   `seriesPatch.test.ts`, `SeriesForm.test.tsx` und `VaterLehrwerke.test.tsx` **unverändert** grün bleiben.
2. **Zwei Regeln an einem Bezugspunkt.** `VaterFachlehrer.tsx:180` hält `loaded` heute für die
   Ableitungsregel aus B-126 (`applySeriesChange(f, touched, previous, next, loaded)`). Wird dasselbe
   Objekt für den Patch erweitert, hängen zwei Regeln an einem Wert, und eine Änderung für die eine kann
   die andere verstellen. Sauberer wäre ein zweiter, eigener Schnappschuss — das kostet eine Zeile und ist
   beim Bauen zu entscheiden.
3. **Der eingefrorene Bezugspunkt muss nach dem Speichern nachziehen.** `SeriesForm` tut das aus der
   **Antwort** (`VaterLehrwerke.tsx:267`) und benutzt dafür `action.runFor`. Beide Formulare hier benutzen
   heute `action.run`, das nur `boolean` liefert. Wird das übersehen, rechnet der **zweite** Speichervorgang
   derselben Sitzung gegen einen veralteten Bezugspunkt — ein Fehler, der im ersten Durchgang unsichtbar
   ist und darum einen eigenen Testfall braucht.
4. **`ChildMaterialSection` hat heute keinen einzigen Komponententest.** Die Lehre aus B-143 ist, dass der
   Defekt *nicht* in der Regel saß, sondern im Formular (`SeriesForm.test.tsx:7-8`). Für das Lehrbuch gibt
   es diese Prüfebene noch gar nicht; sie entsteht mit dieser Story.

### Angriffsplan

„Backend zuerst" entfällt — die Story ist reines Frontend. Die Reihenfolge folgt stattdessen dem Risiko:
zuerst das, was bestehenden Code berührt, damit ein Rotwerden dort nicht in späteren Änderungen untergeht.

1. **`src/vater/subjectField.ts` anlegen**, `FREETEXT_SUBJECT` dorthin ziehen, `seriesPatch.ts` und
   `VaterLehrwerke.tsx` auf den neuen Import umstellen. Kein Verhalten ändert sich; die bestehende Suite
   ist der Beweis.
2. **Lehrbuch:** `textbookFormValues`/`textbookPatch` samt Testdatei schreiben (rot), dann
   `ChildMaterialSection.tsx` umstellen — Bezugspunkt einfrieren, `runFor` statt `run`, gesperrte Option
   im Fach-Feld.
3. **Fachlehrer:** dasselbe für `VaterFachlehrer.tsx`; dabei Risiko 2 entscheiden und den widerlegten
   Kommentar in Zeile 196 samt Testnamen mitziehen (Entscheidung 5).
4. **Feldhilfe:** zwei `HelpTopic`-Einträge neben `seriesSubject` (`fieldHelp.ts:153`), die beiden
   Fach-Felder auf `FieldLabel` umstellen.
5. **E2E** als Letztes, wenn die Oberfläche steht.

### Testweg

| Ebene | Datei | Was sie hält |
| --- | --- | --- |
| Regel (neu) | `src/vater/subjectField.test.ts` | Ableitung `{subjectId, subjectName} → Wert`, die drei Patch-Zweige, „Sentinel nie im Rumpf" und `Number(FREETEXT_SUBJECT)` ist `NaN` (zieht aus `seriesPatch.test.ts:45-47` um) |
| Regel (neu) | `src/vater/textbookPatch.test.ts`, `src/vater/profilePatch.test.ts` | Je „nur das Geänderte" und „leeren gegen unverändert", Vorbild `seriesPatch.test.ts` |
| Formular (neu) | `src/vater/ChildMaterialSection.test.tsx` | Die gesperrte Option und AK 1 — die Ebene, auf der B-143 seinen Defekt hatte |
| Formular (Bestand) | `src/vater/VaterFachlehrer.test.tsx` | Um AK 2/3 erweitert; der Fall bei Zeile 44 wird umbenannt |
| Regression (Bestand) | `seriesPatch.test.ts`, `SeriesForm.test.tsx`, `VaterLehrwerke.test.tsx` | Müssen **unverändert** grün bleiben (Zusicherung für Schritt 1) |
| Durchstich (neu) | `frontend/e2e/kind-lehrbuch-fach.spec.ts` | Fach löschen → Buch bearbeiten → anderes Feld speichern → Name steht noch; dann „– keine Angabe –" → Name weg. Vorbild `e2e/lehrwerk-bearbeiten.spec.ts:109` |

**Der E2E ist hier zugleich der Rollengang** — und das ist der Unterschied zu B-127/B-143/B-144, die ihn
alle drei nicht führen konnten: Deren Löschpfade hängen an `confirmAction`, und ein `window.confirm`
blockiert die Chrome-Extension. Das Bearbeiten eines Lehrbuchs hängt an keinem Dialog. Es gibt hier also
**keine Entschuldigung** für einen ausgefallenen Rollengang.

**Rote Probe:** AK 1 und 2 müssen vor der Behebung rot sein. AK 6 (unverändertes Feld wird nicht gesendet)
ist heute schon verletzt und wird es ebenfalls.

## Verlauf

- **2026-08-10** — angelegt aus dem Frontend-Review des Nachtlauf-Sprints 3. **Bewusst nicht im Sprint
  behoben:** der Fund liegt außerhalb seines Diffs, das Sprint-Ziel ist ohne ihn erreicht, und B-143 zu
  erweitern hieße, eine geschätzte Story während des Bauens wachsen zu lassen.
- **2026-08-11** — **ausformuliert.** Gegen den Code belegt statt aus dem Review-Befund abgeschrieben, und
  das hat den Zuschnitt geändert: Der Defekt sitzt an **zwei** Formularen, nicht an einem
  (`VaterFachlehrer.tsx:245-251` trägt dieselbe Zeile — offener Punkt 1 damit beantwortet). Nachgesehen und
  ausdrücklich **nicht** betroffen: der Server. `TextbooksController.cs:110-117` und
  `CreatorProfilesController.cs:148` wenden erst den Wert, dann den Schalter an und leiten den Namen gegen
  den Ergebniszustand ab (B-142) — die `clearSubject`-Semantik „Id und Name zusammen" ist richtig, falsch
  ist nur, dass der Client sie ungefragt schickt. Zwei neue offene Punkte kamen dazu (geteilter Helfer statt
  dritter Kopie; Sichtbarkeit des verwaisten Zustands), einer fiel weg. **Nicht** nachgemessen und als
  solches benannt: ob `clearSeries` denselben nicht darstellbaren Zustand kennt.
- **2026-08-11** — **gegrillt** (Dialog, fünf Entscheidungen). Zwei Ergebnisse, die die Story vorher nicht
  hatte: Erstens ist die **Empfehlung des Ausformulierens gefallen** — „erst der Diff, der Sentinel
  später" hätte an zwei neuen Stellen den Defekt von B-143 erzeugt; die beiden Hälften sind nicht
  trennbar. Zweitens ist die **Fehlerbedingung geschärft**: nicht „Momentanwert statt Vergleich" (das tun
  sechs von sieben `clear…`-Stellen, alle heil), sondern „Momentanwert über ein Feld, dessen Ladezustand
  das Formular nicht darstellen kann" — heute genau `subjectId`. Daraus folgte Entscheidung 2 gegen den
  allgemeinen Helfer, den das Ausformulieren noch empfohlen hatte.
  Begrifflich übernommen statt neu erfunden: **Freitext-Fach** aus B-143 (`FREETEXT_SUBJECT`), das Wort
  „verwaist" ist raus. Zwei Kostenposten kamen dazu, die im Befund nicht vorkamen: zwei neue
  `HelpTopic`-Einträge samt `FieldLabel`-Umstellung, und das Anfassen von `seriesPatch.ts` /
  `VaterLehrwerke.tsx` beim Umzug des Sentinels (Code, der seit dem 2026-08-10 abgenommen ist).
- **2026-08-11** — **geschätzt**: `M`, `frontend`, `migration: nein`, `vertragsbruch: nein` (beide Flags
  nachgesehen: kein Schema, und `ClearSubject` steht in beiden DTOs längst — wir schicken den Schalter
  künftig nur seltener). Der naheliegende Vergleich mit B-143 (`S`) trägt nicht: B-143 war klein, **weil
  B-123 die Struktur schon gebaut hatte**; hier fehlt sie an beiden Formularen komplett. Vier Risiken
  benannt, drei davon aus dem Bestand: frisch abgenommener Code im Diff, zwei Regeln an einem
  Bezugspunkt (`VaterFachlehrer.tsx:180` trägt `loaded` schon für B-126), und das Nachziehen des
  Bezugspunkts nach dem Speichern (`run` liefert nur `boolean`, gebraucht wird `runFor`) — ein Fehler,
  der erst beim **zweiten** Speichern derselben Sitzung sichtbar wird.
  **Der Rollengang ist hier nicht wegzudiskutieren:** Anders als bei B-127/B-143/B-144 hängt dieser Weg
  an keinem `confirmAction`, die Chrome-Extension blockiert also nicht. Der neue E2E fährt ihn.
- **2026-08-11** — **gebaut** (Stufe `in-arbeit`), alle fünf Schritte des Angriffsplans.
  **Rote Probe vorher**, und zwar in der treuen Fassung: den Sentinel aus `subjectFormValue` entfernt,
  also den Ausgangszustand beider Formulare hergestellt. **14 von 27** Fällen rot, darunter beide
  Kern-Fälle („lässt ein Freitext-Fach unangetastet, wenn ein ANDERES Feld geändert wird" in
  `textbookPatch` **und** `profilePatch`) und beide Formular-Fälle („zeigt es als vorausgewählte,
  gesperrte Option"). Eine erste, schwächere Probe — nur den Diff abschalten — ließ genau diese Fälle
  **grün**: der Sentinel allein trägt sie. Das ist die Probe wert gewesen, sie belegt die Arbeitsteilung
  der beiden Hälften aus Entscheidung 1.
  **Risiko 3 hat sich als gegenstandslos erwiesen, und der Beleg dafür änderte den Code:** Beide
  Bearbeiten-Formulare **schließen** sich beim Speichern (`onDone` → `setEditing(null)`), der
  Bezugspunkt kann also gar nicht veralten. Das zuerst gebaute Nachziehen aus der Antwort samt `runFor`
  ist darum wieder raus — es hätte ausgesehen, als liefe es. Stattdessen steht die **Bedingung** an
  beiden `useRef`: Wer das Formular je offen stehen lässt (wie `SeriesForm`, aus gutem Grund), muss die
  Antwort einfließen lassen.
  **Risiko 2 entschieden:** eigener Schnappschuss für den Patch (`geladen`), `loaded` bleibt bei B-126 —
  aber `loaded.subjectId` musste auf dieselbe Darstellung nachziehen (`subjectFormValue`), sonst verlöre
  Fall 3 von `applySeriesChange` für genau dieses Fach seine Wirkung.
  Belegt: Vitest **238/238** (vorher 204), Playwright **34/34** (vorher 33), `tsc -b` sauber. Die drei
  Bestands-Dateien der Reihe (`seriesPatch.test.ts`, `SeriesForm.test.tsx`, `VaterLehrwerke.test.tsx`)
  sind **unverändert** grün — die Zusicherung für Schritt 1.
  **Fund daneben, als eigene Story abgelegt:**
  [B-151](B-151-gespeichert-banner-verschwindet-mit-dem-formular.md) — „Gespeichert." ist in beiden
  Formularen nie zu sehen, weil `onDone` sie mitsamt ihrem `StatusBanner` aushängt. Nicht mitgenommen:
  das Ziel dieser Story ist ohne den Punkt erfüllt, und er trägt eine eigene Entscheidung.
  Offen bis zur Abnahme: `frontend-reviewer`.
