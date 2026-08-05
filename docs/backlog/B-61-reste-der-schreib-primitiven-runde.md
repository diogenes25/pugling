---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: [CatalogAdmin leert unbedingt, Kinderliste flackert]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/backlog/B-54-objectivecard-schreib-primitive.md
---

# B-61 · Zwei Reste aus der Schreib-Primitiven-Runde

Beim Bauen von [B-54](B-54-objectivecard-schreib-primitive.md) sind zwei Stellen aufgefallen, die zur
selben Familie gehören, aber auf anderen Bildschirmen liegen. Sie sind **nicht** in B-54 mitgemacht worden,
weil sie deren Zuschnitt gesprengt hätten – und nicht in einen „offen:"-Vermerk gewandert, weil dafür
dieser Bereich existiert.

## User Story

Als **Vater**, der im Katalog ein Fach/Kapitel/Art anlegt oder auf dem Dashboard ein Kind hinzufügt, möchte
ich dieselbe Rückmeldungs-Disziplin wie überall sonst im Vater-Web bekommen – ein abgelehnter Eintrag darf
mir den getippten Text nicht wegwerfen, und eine schon sichtbare Liste darf bei einem `reload()` nicht kurz
durch die Ladeanzeige ersetzt werden.

## Ist-Stand am Code

**1. `CatalogAdmin.tsx:180` leert sein Eingabefeld unbedingt.**

```tsx
onSubmit={(e) => { e.preventDefault(); if (name.trim()) { onCreate(name.trim()); setName(""); } }}
```

`NewName.onCreate` ist als `(name: string) => void` typisiert (`CatalogAdmin.tsx:175`) und wird **nicht**
awaited – das Feld leert sich, egal ob der Server ablehnt. Drei Aufrufstellen hängen daran:
`createSubject` (`:48-53`, eigene `async function`, liefert heute nichts zurück), sowie zwei Inline-Lambdas
für Kapitel (`:105-107`) und Art (`:126`), die beide über den gemeinsamen Helfer `act()` (`:40-45`) laufen –
auch der liefert heute implizit `void`. Der Knopf selbst trägt korrekt `disabled={busy}` (`:187`); es geht
**nur** um den verlorenen Text.

Der Vergleichsfall ist im selben Repo schon gelöst: `TagAdder.onAdd` (`VaterVocab.tsx:880`) ist
`(name: string) => Promise<boolean>`, und `TagAdder` leert sein Feld nur bei Erfolg
(`VaterVocab.tsx:899`: `if (await onAdd(name)) setValue("")`) – genau das Muster, das B-54 dort für denselben
Defekt eingeführt hat.

Und der Fehlschlag ist **real erreichbar, nicht nur denkbar**: `ChaptersController.Create`
(`backend/Pugling.Api/Controllers/Creator/ChaptersController.cs:61-62`) lehnt einen doppelten Kapitelnamen
im selben Fach mit `ApiErrors.DuplicateChapterName` ab, `ExerciseCategoriesController.Create`
(`backend/Pugling.Api/Controllers/Creator/ExerciseCategoriesController.cs:65-66`) ebenso mit
`ApiErrors.Conflict`. (Nur `Subject.Name` trägt keine Eindeutigkeit – `PuglingDbContext.cs:415` legt dafür
bewusst nur einen nicht-eindeutigen Index an, `SubjectsController.Create` prüft nichts. Der Subject-Pfad
kann also heute gar nicht ablehnen; der Defekt zeigt sich dort nie.)

**2. `VaterDashboard.tsx:48` und `:73` benutzen weiter `{x.loading ? "Lade…" : …}`.**

```tsx
{today.loading ? <div className="loading">Lade…</div> : today.error ? … : ( … )}      // :48
{children.loading ? <div className="loading">Lade…</div> : children.error ? … : ( … )} // :73
```

Die [in `frontend/CLAUDE.md` beschriebene `useAsync`-Falle](../../frontend/CLAUDE.md) beißt hier nicht
hart – keine der beiden Tabellen hat aufklappbare Zeilen, deren Zustand verloren gehen könnte. Sechs andere
Dateien tragen inzwischen den Wächter, den B-54 für die harten Fälle eingeführt hat:
`x.loading && x.data === null` (`VaterZiele.tsx:205`, `VaterVocab.tsx:304`, `VaterKatalog.tsx:32`,
`VaterExercises.tsx:152`, `VaterKind.tsx:89`, `VaterLehrwerke.tsx:148`/`ChildMaterialSection.tsx:46` je als
`x.data === null`-Variante).

**Präzisierung gegenüber der Idee:** `addChild` (`VaterDashboard.tsx:30-41`) ruft nach dem Anlegen nur
`children.reload()` (`:40`) auf – **nicht** `today.reload()`. Die Behauptung „beide Listen flackern nach
jedem Anlegen eines Kindes" stimmt am Code nur für die „Kinder"-Tabelle (`:73`); die „Heute"-Tabelle (`:48`)
wird im ganzen Component nirgends neu geladen und flackert nach einem `addChild` darum **nicht** – ihr
Wächter ist reine Konventionstreue, kein Regressionsschutz.

## Die echte Lücke

Zwei getrennte Familien, kleiner als die Idee-Notiz vermuten ließ:

- Ein fire-and-forget-`onCreate`, das den Text auch bei einer echten Server-Ablehnung wegwirft – belegt
  über den bereits existierenden Dedup-Check bei Kapitel und Art.
- Ein kosmetischer Lade-Flicker in genau **einer** Tabelle (`:73`), plus eine zweite Zeile (`:48`), die
  denselben Wächter aus Konsistenzgründen verdient, obwohl sie heute nichts sichtbar kaputt macht.

Eine dritte, optisch identische Stelle im selben File (`:124`, `plans.loading ? "Lade…" : …`) gehört **nicht**
dazu – siehe Entscheidung 5.

## Offene Punkte

1. ~~Ungeprüft: gibt es im Vater-Web weitere `{loading ? …}`-Stellen, an denen die Falle harmlos, aber
   sichtbar ist?~~ → siehe Entscheidung 1.
2. ~~Ungeprüft: gibt es weitere Formulare, die ihr Feld unbedingt statt nur bei Erfolg leeren?~~ → siehe
   Entscheidung 2.

## Entscheidungen

1. **Der Scope bleibt auf die zwei ursprünglich benannten Bildschirme beschränkt.** Nachgesehen: das Muster
   `{x.loading ? <div className="loading">…</div> : …}` ohne `data === null`-Wächter steht in über 25
   weiteren Dateien quer durchs Vater-Web (u. a. `VaterShop.tsx`, `VaterRewards.tsx`, `VaterClassTests.tsx`,
   `ClozeTexts.tsx`, `VaterFachlehrer.tsx`) – es ist der **Standardstil**, nicht die Ausnahme; nur sieben
   Dateien tragen bislang den Wächter. Diese Story zieht die übrigen **nicht** nach – das wäre ein eigener,
   viel größerer Aufräum-Auftrag ohne mechanisch entscheidbare Grenze (dieselbe Erkenntnis wie
   [B-54](B-54-objectivecard-schreib-primitive.md) Entscheidung 3: „reload-getrieben und harmlos" ist nicht
   mechanisch bewachbar). *Kosten:* der Flicker bleibt an den übrigen ~25 Stellen bestehen, potenziell ein
   eigener späterer Backlog-Eintrag.
2. **Kein zweiter Fund bei den „leert unbedingt"-Formularen.** Eine gezielte Suche über alle
   `onSubmit`-Handler im Frontend (Muster: ein nicht-awaiteter Aufruf, direkt gefolgt vom Leeren eines
   States in derselben Zeile) findet ausschließlich `CatalogAdmin.tsx:180`. *Kosten:* keine – der zweite
   Offene Punkt schließt sich, ohne weitere Arbeit auszulösen.
3. **`CatalogAdmin`s `NewName.onCreate` folgt exakt dem `TagAdder.onAdd`-Muster:** Signatur
   `(name: string) => Promise<boolean>`, Feld nur bei `true` geleert. *Begründung:* B-54 hat genau dieses
   Problem beim `TagAdder` schon gelöst; eine zweite Lösung wäre eine zweite Konvention für denselben Fall.
   *Kosten:* `act()` und `createSubject()` müssen `boolean` statt implizit `void` liefern – beide sind
   reine Wrapper um `action.run`/`action.runFor`, die das Ergebnis schon kennen, die Änderung ist rein
   mechanisch.
4. **`VaterDashboard`s zwei Stellen bekommen den in sechs anderen Dateien schon etablierten
   `x.loading && x.data === null`-Wächter**, kein neues Muster. *Kosten:* keine über die Umstellung hinaus.
5. **Die dritte, optisch gleiche Stelle im selben File (`:124`, `plans.loading ? "Lade…" : …`) bleibt
   draußen.** Anders als `:48`/`:73` wird `plans.reload()` im ganzen Repo nirgends aufgerufen – es gibt
   keinen Aufrufer, der dort heute einen Flicker auslöst. *Kosten:* eine dritte, ungleich behandelte Zeile
   bleibt stehen; vertretbar, weil sie nichts Beobachtbares repariert und ein Fix dort unbelegte Arbeit
   wäre.
6. **`:48` (die „Heute"-Tabelle) wird trotzdem mitgezogen, obwohl sie heute nicht flackert.** Die
   Ist-Stand-Präzisierung oben zeigt: `today.reload()` wird nirgends aufgerufen, die Idee-Behauptung „beide
   Listen flackern" trifft nur auf `:73` zu. Der Fix bei `:48` ist **reine Konventionstreue** (ein Wort),
   nicht Regressionsschutz – er bekommt darum kein Akzeptanzkriterium mit Vorher-rot-Test. *Kosten:* ein
   AK, das nicht mechanisch geprüft werden kann; vertretbar bei einem Einzeiler, der die Datei mit den sechs
   anderen konsistent hält.
7. **Der Flicker-Nachweis für `:73` läuft strukturell, nicht zeitkritisch.** Ein Check „`.loading` erschien
   nie" direkt nach dem Klick ist rennabhängig und könnte in CI zufällig grün sein, ohne etwas zu beweisen.
   Der Test verzögert darum die GET-Antwort auf die Kinderliste künstlich (`page.route` mit einer kurzen
   Verzögerung), bevor er prüft, dass `.loading` in der „Kinder"-Sektion nicht erscheint, während die alte
   Tabelle noch sichtbar ist. *Kosten:* ein weiterer `page.route`-Abschnitt im schon langen
   `vater-von-null.spec.ts`.

## Akzeptanzkriterien

1. `NewName.onCreate` (`CatalogAdmin.tsx`) hat die Signatur `(name: string) => Promise<boolean>`; das
   Eingabefeld wird nur bei `true` geleert – wie `TagAdder.onAdd`.
2. Ein abgelehntes Anlegen (doppelter Kapitel- oder Art-Name im selben Fach – der Server lehnt das schon
   heute mit `DuplicateChapterName` bzw. `Conflict` ab) lässt den getippten Text im Feld stehen.
3. `VaterDashboard.tsx` zeigt „Heute" und „Kinder" nur beim allerersten Laden die Ladeanzeige
   (`x.loading && x.data === null`); ein `reload()` mit schon vorhandenen Daten ersetzt die Tabelle nicht
   mehr kurz durch „Lade…".
4. Ein neu angelegtes Kind lässt die „Kinder"-Tabelle beim anschließenden `reload()` sichtbar, statt sie
   kurz durch die Ladeanzeige zu ersetzen (belegt über eine verzögerte Server-Antwort, Entscheidung 7).
5. `vater-von-null.spec.ts` bekommt im bestehenden Katalog-Abschnitt eine Zusicherung, dass ein
   abgelehntes Kapitel (Namensduplikat) den Feldinhalt behält – vorher rot, weil `setName("")` unbedingt
   lief.
6. Unverändert grün: die bestehende Vitest- und Playwright-Suite. Kein Backend berührt, kein Vertrag
   geändert.

## Schätzung

**Größe: S** – zwei kleine, unabhängige Fixes in zwei Dateien (`CatalogAdmin.tsx`, `VaterDashboard.tsx`),
aber mit einer Typänderung an drei Aufrufstellen (`createSubject`, zwei `act()`-Lambdas) plus einer neuen
Zusicherung in einem bestehenden, schon langen E2E-Spec. Vergleichbar mit dem S-Anker `childId` aus dem
Test-Pfad ziehen (B-01), etwas mehr Ripple als ein reiner XS-Einzeiler.

`wo: frontend` – beide Fixes sind rein clientseitig; die serverseitigen Dedup-Prüfungen (Kapitel, Art)
existieren schon und werden nur erstmals vom Frontend korrekt konsumiert. `migration: nein`,
`vertragsbruch: nein` – kein `Pugling.Contracts`-DTO ändert sich, keine neue Route, kein neues Feld.

**Risiken.**

- Der reale Duplikat-Test hängt an der Chapter/Category-Dedup-Logik im Backend, nicht an einer gemockten
  Route – ändert sich diese Logik künftig, muss der Test mitgezogen werden. Vorteil: kein Interceptor, der
  von der echten Serverantwort abweichen könnte.
- Die künstlich verzögerte Antwort (Entscheidung 7) darf nicht in den Rest der langen `vater-von-null.spec.ts`
  lecken (`page.unroute` danach, analog zum bestehenden Muster bei der 409-Simulation für `addChild` in
  derselben Datei).
- `:48` bekommt ein AK ohne Vorher-rot-Test (Entscheidung 6) – das ist bewusst in Kauf genommen, nicht
  übersehen.

**Angriffsplan** (jede Stufe für sich lauffähig):

1. `CatalogAdmin.tsx`: `act()` und `createSubject()` liefern `Promise<boolean>`; `NewName.onCreate` bekommt
   die neue Signatur; die drei Aufrufstellen (Fach/Kapitel/Art) reichen das Ergebnis durch; `setName("")`
   nur bei `true`.
2. `VaterDashboard.tsx`: `:48`/`:73` auf `x.loading && x.data === null` umstellen.
3. `vater-von-null.spec.ts`: im Katalog-Abschnitt einen zweiten Kapitel-Anlage-Versuch mit demselben Namen
   ergänzen, Feldinhalt nach der Ablehnung prüfen; im Kind-Anlage-Abschnitt die verzögerte
   Kinderlisten-Antwort ergänzen und `.loading` als nicht sichtbar prüfen.

**Testweg.** Playwright, `vater-von-null.spec.ts` (erweitert, kein neues Spec-File) – die Datei deckt
bereits beide betroffenen Bildschirme (Kind anlegen, Katalog) im selben Durchstich ab. Kein Vitest-
Komponententest: `frontend/CLAUDE.md` zieht die Grenze „kein nachgebauter Bildschirm mit gefälschtem
`fetch`", und beide Fixes sitzen in ganzen Screens, nicht in isolierten Komponenten. Kein `/smoke-test`
nötig – rein clientseitig, keine Backend-Änderung, keine neue Route.

## Verlauf

- **2026-08-01** — angelegt aus zwei Funden beim Bauen von B-54 (einer davon aus dem
  `frontend-reviewer`-Lauf), je mit `Datei:Zeile` belegt. Bewusst **nicht** in B-54 mitgemacht: andere
  Bildschirme, und der Vollständigkeits-Beweis dieser Story galt den mutierenden Knöpfen, nicht dem
  Aufräumen jedes Formulars.
- **2026-08-03** — **ausformuliert.** Beide Stellen am Code nachgesehen und mit einer Zeilen-Präzisierung
  versehen: die „Heute"-Tabelle (`:48`) flackert entgegen der Idee-Behauptung nicht, weil `today.reload()`
  nirgends aufgerufen wird – nur die „Kinder"-Tabelle (`:73`) tut es. Zusätzlich belegt: der abgelehnte
  Katalog-Eintrag ist kein Gedankenexperiment, `ChaptersController`/`ExerciseCategoriesController` lehnen
  Namensduplikate schon heute serverseitig ab; nur `Subject.Name` trägt keine Eindeutigkeit. Beide offenen
  Punkte der Idee (weitere Flacker-Stellen, weitere unbedingt leerende Formulare) recherchiert.
- **2026-08-03** — **gegrillt** (autonom getroffen, Nutzerauftrag 2026-08-04). Sieben Entscheidungen: der
  Scope bleibt auf die zwei benannten Bildschirme beschränkt (>25 weitere Stellen mit demselben
  Flicker-Muster bestehen bewusst fort, kein mechanisches Tor dafür), kein zweiter Fund bei den
  unbedingt-leerenden Formularen, `CatalogAdmin` übernimmt das `TagAdder`-Muster 1:1, `VaterDashboard`
  übernimmt den in sechs Dateien etablierten `data === null`-Wächter, eine dritte optisch gleiche Zeile
  (`:124`) bleibt draußen (kein Aufrufer lädt dort neu), `:48` wird trotzdem mitgezogen (reine
  Konventionstreue, kein Vorher-rot-Test möglich), und der Flicker-Nachweis läuft über eine künstlich
  verzögerte Serverantwort statt über einen rennabhängigen Sofort-Check.
- **2026-08-03** — **geschätzt** (autonom getroffen, Nutzerauftrag 2026-08-04): **S**, `wo: frontend`,
  keine Migration, kein Vertragsbruch. Angriffsplan, Risiken und Testweg (Erweiterung von
  `vater-von-null.spec.ts`, kein neues Spec, kein `/smoke-test`) stehen oben.
- **2026-08-05** — im Autonomen Modus gebaut, ohne Rückfrage je Ticket: `CatalogAdmin`s `act()`/`createSubject()`
  liefern jetzt `Promise<boolean>`, `NewName.onCreate` übernimmt exakt das `TagAdder.onAdd`-Muster (Feld nur
  bei `true` geleert); `VaterDashboard`s „Heute"- und „Kinder"-Tabelle nutzen jetzt
  `x.loading && x.data === null` wie die sechs anderen Dateien, die dritte optisch gleiche Stelle (`plans`,
  Entscheidung 5) blieb unberührt. **Abweichung von AK 2/5:** die Story sprach von einem Kapitel-Duplikat,
  aber Kapitel sind seit [B-106](B-106-lehrwerkgetriebener-katalog.md) aus `CatalogAdmin` verschwunden
  (jetzt unter `/vater/lehrwerke`) – der Nachweis lief stattdessen über „Art" (Kategorie), denselben
  `NewName`/`act()`-Codepfad mit derselben `ApiErrors.Conflict`-Ablehnung. Rote Probe vorab bestätigt: mit
  gestashten Implementierungsdateien (nur `CatalogAdmin.tsx`/`VaterDashboard.tsx`, Testdatei blieb) schlug
  `vater-von-null.spec.ts` exakt an der neuen „Kinder"-Flicker-Zusicherung fehl (1 statt 0 `.loading`-Elemente);
  nach dem Zurückholen grün. `tsc -b` sauber, `npm test` **136/136 grün**, `vater-von-null.spec.ts` zweimal
  grün. `frontend-reviewer` fand keine Blocker (Vertrag zu `TagAdder`/den sechs `data === null`-Dateien
  deckungsgleich, keine Selektor-Fragilität, Kapitel→Art-Ersatz als sachgerecht bestätigt). Commit `9e16921`,
  dazu dieser. Status → `abgenommen`.
- **2026-08-05** — Nachtrag zur neuen Eintrittsbedingung (README → „Der Rollengang fällt am leichtesten
  weg"): **kein Rollengang geführt.** Belegt waren die Suite und der Reviewer, nicht aber ein Gang als
  Vater an der laufenden App. Kein Schaden bekannt — die Lücke steht hier, statt still zu bleiben.
