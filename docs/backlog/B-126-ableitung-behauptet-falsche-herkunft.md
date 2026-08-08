---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [aus dem Lehrwerk übernommen stimmt nicht, isDerived ohne Herkunftsprüfung,
  Ableitung räumt nicht ab]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Funde 5 und 6)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-67]
nachgeschaut: ""
wartet_auf: ""
---

# B-126 · „aus dem Lehrwerk übernommen" steht auch unter Werten, die nicht von dort kommen

[B-67](B-67-fachlehrer-aus-lehrwerk.md) füllt Fach und Sprachen aus der gewählten Lehrwerk-Reihe vor und
schreibt „aus dem Lehrwerk übernommen" darunter. Der Hinweis prüft aber nicht, **ob der angezeigte Wert
tatsächlich von dort stammt** — er prüft nur, ob der Nutzer das Feld nicht angefasst hat und die Reihe
*irgendeinen* Wert hätte. Beim Bearbeiten eines gespeicherten Profils behauptet er damit regelmäßig eine
Herkunft, die nicht stimmt. Und beim Wechsel auf eine Reihe ohne Sprachen bleibt der alte Wert stehen,
während der Hinweis verschwindet.

## User Story

Als **Creator** möchte ich, dass „aus dem Lehrwerk übernommen" nur dort steht, wo der Wert wirklich aus
dem Lehrwerk kommt — damit ich einem gespeicherten Profil ansehe, was ich selbst gesetzt habe und was
das Werk beisteuert.

## Ist-Stand am Code

Alle drei Punkte in `frontend/src/vater/VaterFachlehrer.tsx`:

1. **Der Hinweis prüft die Herkunft nicht** (`:174`):
   `const isDerived = (field) => !touched.has(field) && Boolean(derivableValues[field]);`
   `touched` startet leer (`:152`), auch beim **Bearbeiten** eines bestehenden Profils. Ein gespeichertes
   Profil mit `sourceLang: "fr"`, das auf eine englische Reihe zeigt, zeigt „fr" im Feld — und darunter
   „aus dem Lehrwerk übernommen" (`:308`), obwohl die Reihe „en" trüge.
2. **Die Ableitung räumt nie ab** (`:186`):
   `fields.filter(([field, value]) => value && !touched.has(field))` — ein leerer Wert der neuen Reihe
   fällt aus dem Filter. Wer „Green Line 1" wählt (`sourceLang` füllt sich mit „en") und dann auf eine
   Mathe-Reihe ohne Sprachen wechselt, behält „en" im Feld, während der Hinweis verschwindet: das Profil
   wird mit einer Sprache gespeichert, die aus einer nicht mehr referenzierten Reihe stammt.
3. **`touched` überlebt das Anlegen** (`:236-239`): nach einem erfolgreichen `create` werden `form` und
   `types` zurückgesetzt, `touched` nicht. Das zweite Profil im selben Formular leitet die Felder nicht
   mehr ab, die beim ersten von Hand geändert wurden.

Der Kommentar bei `:171-173` begründet die berechnete Variante ausdrücklich („a second Set kept in sync
by hand could desync") — die Entscheidung gegen ein zweites State-Feld war richtig, nur ist die
berechnete Bedingung unvollständig.

## Die echte Lücke

Nicht „die Ableitung funktioniert nicht" — beim Neuanlegen, dem Weg aus B-67s Akzeptanzkriterien, tut sie
genau das Richtige. Die Lücke liegt in den beiden Zuständen, die B-67 nicht durchgespielt hat: **ein
bestehendes Profil öffnen** und **die Reihe wechseln**. In beiden ist `touched` leer bzw. veraltet, und
der Hinweis wird zu einer Aussage über den Formularzustand statt über die Herkunft des Wertes.

Ein falsch beschrifteter Wert ist dabei schlimmer als gar kein Hinweis: Er sagt dem Creator, er müsse
sich nicht kümmern, das Werk regle das — und speichert dann seinen eigenen alten Wert.

## Entscheidungen

1. **„Abgeleitet" heißt: unberührt UND der angezeigte Wert ist der Wert der Reihe.** Die Bedingung
   bekommt den Vergleich, der ihr fehlt (`form[field] === derivableValues[field]`). Begründung: das
   bleibt die berechnete Variante — der Grund gegen ein zweites Set (`:171-173`) gilt unverändert —,
   sie beantwortet jetzt nur die richtige Frage. **Kosten:** keine; ein Term mehr in derselben Zeile.
2. **Ein abgeleitetes Feld folgt der Reihe, ein berührtes nie.** Beim Reihenwechsel wird je Feld
   verglichen, ob der aktuelle Wert der der **vorherigen** Reihe war; wenn ja, wird er durch den Wert
   der neuen Reihe ersetzt. Begründung: das ist die einzige Lesart, in der der Hinweis dauerhaft wahr
   bleibt, ohne je eine Eingabe des Nutzers zu verwerfen. **Kosten:** `deriveFromSeries` braucht die
   Werte der alten Reihe, muss sie also aus demselben Formularstand lesen wie die Werte selbst.
2a. **„Eingabe des Nutzers" schließt frühere Sitzungen ein** — nachgeschärft nach dem
   `frontend-reviewer` (Befund 2). `touched` kennt nur die laufende Sitzung; ein gespeicherter Wert
   („fr" an einer englischen Reihe, genau der Fall aus dem Ist-Stand) wäre beim Wechsel auf eine
   spanische Reihe überschrieben worden. Die Regel bekommt darum einen vierten Eingang: die **beim
   Öffnen geladenen** Profilwerte, die wie `touched` unantastbar sind. Bei einem *neuen* Profil bleibt
   er bewusst leer — dort soll die Vorgabe `en`/`de` ja gerade weichen (sonst fiele B-67 um).
   **Kosten:** ein zusätzlicher Parameter und ein `useState`-Schnappschuss im Formular.
2b. **Geleert wird auf die Vorgabe, nicht auf den leeren String** — ebenfalls aus dem Review (Blocker 1),
   und es ist kein Geschmack, sondern eine Korrektur: der Server kennt für die Sprachen **keinen** leeren
   Zustand (`CreatorProfile.SourceLang` ist non-null mit Vorgabe `en`/`de`), das Update-DTO hat kein
   `clearSourceLang`, und ein `null` gilt im PATCH als „nicht angegeben". Ein geleertes Sprachfeld wäre
   nie in der Datenbank angekommen — die Oberfläche hätte „Gespeichert." gemeldet und beim nächsten
   Öffnen wieder den alten Wert gezeigt. Genau der stille Fehler, gegen den die PATCH-Regel im
   Startkontext steht. Beim **Fach** bleibt `""` richtig: dort trägt `clearSubject` den Zustand.
   **Kosten:** eine Konstante `FIELD_FALLBACKS`, die zugleich die Vorgaben des Formulars speist — statt
   `en`/`de` an zwei Stellen zu wiederholen.
3. **Die Regel wird als reine Funktion ausgelagert und mit Vitest geprüft**, nicht im Bildschirm
   gelassen. Begründung: das etablierte Muster des Repos für genau solche Fälle (`reviewFeedback` aus
   [B-96](B-96-showboth-stufe-ohne-mechanik.md), `runWizardFinish` aus
   [B-53](B-53-wizard-doppelklick.md)); die Bedingung hat inzwischen drei Zustände und ist im Formular
   nicht prüfbar, ohne einen Bildschirm nachzubauen — was `frontend/CLAUDE.md` ausschließt.
   **Kosten:** eine neue Datei plus ihr Test.
4. **`touched` wird beim Zurücksetzen des Formulars mit zurückgesetzt.** Begründung: es gehört zum
   Formularzustand, den `create` verwirft — dass es überlebt hat, ist schlicht ein vergessenes Feld.
   **Kosten:** eine Zeile.

## Akzeptanzkriterien

1. Ein gespeichertes Profil mit einem Wert, der **nicht** dem der referenzierten Reihe entspricht, zeigt
   für dieses Feld **keinen** „aus dem Lehrwerk übernommen"-Hinweis.
2. Ein gespeichertes Profil, dessen Wert dem der Reihe entspricht und das der Nutzer nicht angefasst hat,
   zeigt den Hinweis weiterhin (B-67s Verhalten bleibt).
3. Wechsel auf eine Reihe ohne Wert für ein abgeleitetes Feld lässt den alten Wert **nicht** stehen: das
   Fach wird geleert, die Sprachen fallen auf ihre Vorgabe zurück (Entscheidung 2b — ein geleertes
   Sprachfeld wäre nicht speicherbar).
4. Ein vom Nutzer gesetzter Wert wird von keinem Reihenwechsel überschrieben — **weder** ein in dieser
   Sitzung geänderter **noch** ein beim Öffnen geladener (Entscheidung 2a).
5. Nach dem Anlegen eines Profils leitet das Formular für den nächsten Eintrag wieder alle Felder ab.
6. Die Regel liegt als reine Funktion vor und ist mit Vitest abgedeckt. **Rot vor der Änderung waren die
   Fälle 1 und 3** — sie beschreiben das defekte Verhalten. Die Fälle 2, 4 und 5 waren vorher schon grün
   bzw. gar nicht erreichbar; sie sind **Regressionswächter**, kein roter Nachweis. Das steht hier so
   ausdrücklich, weil die erste Fassung dieses Kriteriums „die Fälle 1–4 waren vor der Änderung rot"
   behauptete — eine Abdeckung, die es nie gab (Befund 4 des `frontend-reviewer`).

## Schätzung

**Größe: S** (Anker B-01 — eine Bedingung, eine Funktion und ihr Test; keine neue Fläche).
`wo: frontend`, `migration: nein`, `vertragsbruch: nein` — kein DTO ändert sich, der Server sieht davon
nichts.

**Risiko:** Der Vergleich in Entscheidung 1 läuft über `string`-Gleichheit, und `subjectId` reist im
Formular als `string`, in der Reihe als `number` — die bestehende Umrechnung (`:167`, `String(...)`)
muss auf **beiden** Seiten gleich bleiben, sonst meldet das Fach nie „abgeleitet". Zweitens darf
Entscheidung 2 nicht auf `chosenSeries` zugreifen, nachdem `up("seriesId", …)` gelaufen ist — React
hat den State dann noch nicht neu gerendert, aber der Code läse bereits die neue Absicht.

**Angriffsplan:** reine Funktion (`derivedFields`/`applySeries`) in einer eigenen Datei anlegen und mit
Vitest abdecken → `VaterFachlehrer.tsx` auf sie umstellen → `touched`-Reset. **Testweg:** Vitest über
die reine Funktion (Muster `SelfAssessAnswer.test.tsx`); kein Playwright — der Bildschirm ist sonst
fetch-getrieben, und `e2e/lehrwerke.spec.ts` deckt den Formularweg als solchen schon ab.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main` (zwei Funde, hier als
  eine Story: es ist dieselbe Ableitungsregel, einmal beim Anzeigen und einmal beim Wechseln).
  Alle drei Punkte selbst am Code nachgeprüft (`VaterFachlehrer.tsx:174`, `:186`, `:236-239`).
  `entgangen_bei: [B-67]`: die Ableitung ist in jener Story entstanden und war `abgenommen` — und der
  Nachschau-Sweep desselben Tages hat sie als „hält" passieren lassen, weil er geprüft hat, *dass* über
  `touched` abgeleitet wird, nicht *ob* die Bedingung stimmt.
- **2026-08-07** — gegrillt, geschätzt und gebaut (autonom, `art: Defekt`). Die Regel liegt jetzt in
  `frontend/src/vater/seriesDerivation.ts` (`derivableValues`/`isDerived`/`applySeriesChange`), der
  Bildschirm ruft sie nur noch auf.
  **Rote Probe durch gezielte Fehler-Injektion** statt durch einen ersten grünen Lauf — ein neuer Test
  über neuen Code ist sonst kein Beleg (Lehre aus Sprint 2 des 2026-08-06-Protokolls): beide alten
  Bedingungen wieder eingesetzt (`isDerived` ohne den Wertvergleich, `applySeriesChange` nur mit
  `after !== ""`), Lauf **2 failed | 9 passed** — gefangen wurden genau die zwei Defekte
  (`expected 'en' to be ''` beim Reihenwechsel und der falsch gemeldete Herkunftshinweis). Zurückgesetzt:
  grün.
  **Beim Bauen selbst hereingelaufen, und der Bestandstest hat es gefangen:** `applySeriesChange`
  deklarierte `DerivableValues` als Rückgabe, baute sie aber als `{ ...form }` — also inklusive aller
  übrigen Formularfelder. Der Aufrufer spreizt das Ergebnis, wodurch die **alte `seriesId` die frisch
  gewählte überschrieb**; der B-67-Regressionstest (`VaterFachlehrer.test.tsx`) wurde prompt rot
  (`Unable to find an element with the text: aus dem Lehrwerk übernommen`, während die Feldwerte stimmten).
  Die Funktion baut ihr Ergebnis jetzt aus genau den drei Feldern, und ein eigener Testfall hält die
  Klasse fest. Das ist der Beleg dafür, dass der Test aus B-67 seine Arbeit tut.
  `npm run build` (Typecheck) grün, `npm test -- --run` → **168/168 grün** (156 vor dieser Story + 12
  neue in `seriesDerivation.test.ts`). Backend unberührt (`git diff --name-only -- '*.cs'` leer für
  diese Story).
- **2026-08-07** — `frontend-reviewer` gefahren: **ein Blocker, drei „Sollte", drei „Nice-to-have"** —
  der ertragreichste Review dieser Runde, und der Blocker saß auf meinem eigenen Akzeptanzkriterium.
  - **Blocker (AK3 endete im Formular, nicht in der Datenbank):** das neu eingeführte Leeren der
    Sprachfelder ist gar nicht speicherbar — kein `clearSourceLang` im Vertrag, der Server überliest
    `null`, die Entität ist non-null. Ich hätte „Gespeichert." gemeldet und den alten Wert behalten.
    Behoben über `FIELD_FALLBACKS` (Entscheidung 2b); AK3 ist neu formuliert, weil es in seiner ersten
    Fassung etwas verlangte, das der Vertrag nicht hergibt.
  - **Sollte 2 (Fall 3 verwarf doch eine Eingabe — die aus einer früheren Sitzung):** behoben über den
    vierten Eingang `loaded` (Entscheidung 2a). Meine Entscheidung 2 hatte „ohne **je** eine Eingabe zu
    verwerfen" versprochen und dabei nur an die laufende Sitzung gedacht.
  - **Sollte 3 (Testlücke genau auf dem benannten Hauptrisiko):** `isDerived("subjectId", …)` hatte
    keinen Fall — die `number`↔`string`-Umrechnung, die die Schätzung als Risiko 1 nennt, war ungepinnt.
    Ergänzt, dazu die Fälle „werkunabhängig" und „geladener Fremdwert".
  - **Sollte 4 (AK6 behauptete zu viel):** richtiggestellt, siehe AK6.
  - **Nice-to-have 5/6** übernommen (`seriesId` hinter den Spread, `previous` aus demselben
    Formularstand lesen); **7** (die Live-Region wird mitsamt Inhalt ein-/ausgehängt, WCAG 2.2 SC 4.1.3)
    **nicht** hier behoben, sondern als [B-132](B-132-hinweis-live-region-haengt-aus.md) abgelegt: der
    Befund ist aus B-67 geerbt, das Ziel dieser Story ist ohne ihn erfüllt, und A11y-Arbeit gehört mit
    dem `accessibility`-Skill gemacht statt nebenbei.
  Nach den Änderungen: `npm run build` grün, `npm test -- --run` → **173/173 grün** (156 vor dieser
  Story + 17 neue). Ein alter Testfall („leert ein abgeleitetes Feld") ist dabei **ersetzt** worden statt
  angepasst — er kodierte genau das Verhalten, das der Blocker als nicht speicherbar entlarvt hat.
- **2026-08-07** — **abgenommen.** `npm run build` grün, `npm test -- --run` → **173/173 grün**; Backend
  unberührt, dessen Suite bleibt bei 772/772. **Rollengang-Ersatz:** die Chrome-Extension ist in dieser
  unbeaufsichtigten Sitzung nicht verbunden. Ersatz nach `docs/nachtlauf.md`: der Bestands-Komponententest
  aus B-67 fährt den sichtbaren Weg (Reihe wählen → drei Felder gefüllt → drei Hinweise) und ist grün, die
  Regel selbst ist über 17 Fälle gepinnt, und der `frontend-reviewer` hat alle sechs Akzeptanzkriterien
  einzeln gegengelesen. **Was ein Mensch trotzdem einmal tun sollte** (und was kein Test leisten kann):
  ein gespeichertes Profil öffnen, dessen Sprache von der Reihe abweicht, und prüfen, dass der Hinweis
  wirklich fehlt — die Zusicherung ist eine Aussage über die Oberfläche, nicht über eine Funktion.
