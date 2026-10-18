---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Mehr laden Fokus, gefüllte Verlaufskarte ohne Fokusregel]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 1)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-216]
wartet_auf: ""
---

# B-228 · „Mehr laden“ in der gefüllten Verlaufskarte verliert den Fokus

B-216 hat an drei Stellen den Fokus beim Wiederholen auf den Fehlerkasten gelegt, weil ein `disabled`-Knopf
ihn in Chromium verliert. Die **gefüllte** Verlaufskarte des Sohn-Shops (`frontend/src/sohn/SohnShop.tsx`,
Banner über den Zeilen und Knopf „Mehr laden" darunter) bekam im selben Sprint zwar das Fehler-Gedächtnis
(Reviewer-Fund), aber weder `tabIndex` am Banner noch den Fokus-Umzug am Knopf – der gesperrte „Mehr laden"
lässt den Fokus auf `<body>` fallen, genau der Verlust, den Entscheidung 5 von B-216 an den anderen Stellen
behebt. Kein Kriterium verlangte es dort; für „Mehr laden" ohne Fehler ist der Verlust älter als der Sprint.

## User Story

Als **Sohn** möchte ich, dass nach einem Fehler beim Nachladen des Kaufverlaufs der Fokus beim Drücken von
„Mehr laden" bei der Meldung bleibt – nicht ins Leere fällt –, damit ich per Tastatur dort weitermache, wo
die Karte mir sagt, was gerade passiert.

## Ist-Stand am Code

Nachgesehen am 2026-10-17.

- `frontend/src/sohn/SohnShop.tsx:330-343` (leere Karte): `useGemerkterFehler`, Banner mit `tabIndex={-1}`
  und `ref={meldung}`, Knopf „Nochmal versuchen" mit `onClick={() => { meldung.current?.focus();
  onLoadMore(); }}` – die vollständige B-216-Regel.
- `SohnShop.tsx:352-367` (gefüllte Karte): Banner `{gescheitert != null && <div className="banner err"
  role="alert">{gescheitert}</div>}` **ohne** `tabIndex`/`ref`; Knopf „Mehr laden" mit
  `disabled={loading} onClick={onLoadMore}` – kein Fokus-Umzug. Ein gesperrter Knopf verliert in Chromium
  den Fokus (B-216, im E2E gemessen), hier also auf `<body>`.
- Die drei anderen Stellen (`ListenAusfall.tsx:33-36`, `VaterWizard.tsx:723-726`,
  `PlanPositions.tsx:544-547`) tragen die Regel vollständig; der Fokusring `.banner[tabindex]:focus-visible`
  (`index.css:339`) greift über die Klasse automatisch.
- `HistoryTab.test.tsx:93-101` prüft für die gefüllte Karte nur, dass die Meldung während des Nachladens
  steht – keinen Fokus. Keine Zusicherung an irgendeiner Stelle misst `document.activeElement`.

## Die echte Lücke

Eine Regel an fünf Stellen, an vier vollständig. Der fünfte Fall ist derselbe Knopf in demselben Zustand
(Meldung steht, Wiederholung läuft) – nur mit Zeilen darüber.

## Offene Punkte

1. ~~Nur der Fehlerfall, oder auch „Mehr laden" ohne Fehler?~~ → Entscheidung 2.

## Entscheidungen

Autonom gegrillt am 2026-10-17 unter Freigabe 1 (`art: Defekt`).

1. **Die gefüllte Karte bekommt dieselbe Regel wie die vier anderen Stellen:** Banner mit `tabIndex={-1}` und
   Ref, „Mehr laden" legt den Fokus beim Klick auf das Banner, wenn eins steht. *Begründung:* B-216,
   Entscheidung 5 – der Kasten ist das Element, das sagt, was gerade passiert, und von dort ist der Knopf per
   Tab erreichbar, wenn es wieder scheitert. *Kosten:* ein `useRef` ist schon da (`meldung`), zwei Zeilen;
   ein Vitest-Fall misst `document.activeElement` (der programmatische Fokus ist in happy-dom messbar, der
   Chromium-Verlust nicht – den deckt die Bauart, nicht der Test).
2. **Ohne Meldung bleibt der Verlust bestehen und wird eine eigene Frage** →
   [B-231](B-231-fokus-ohne-fehlerkasten.md). *Begründung:* Wohin der Fokus gehört, wenn **nichts** sagt, was
   passiert, hat B-216 bewusst offen gelassen („ein Zustand ohne Fehler, in dem es nichts mehr zu fokussieren
   gibt") – Kandidaten wären die Liste selbst oder ein Knopf mit `aria-disabled` statt `disabled`, und Letzteres
   bricht die Hausregel `disabled={busy}`. Das ist eine Gestaltungsfrage über alle `disabled={busy}`-Knöpfe der
   Arcade, keine Reparatur dieses Knopfs. *Kosten:* der Sohn verliert ohne Fehler beim Nachladen weiter den
   Fokus – wie vor dem Sprint.

## Akzeptanzkriterien

1. Steht in der gefüllten Karte eine Meldung, liegt nach dem Klick auf „Mehr laden" der Fokus auf der Meldung;
   der Knopf heißt „Lädt…" und ist gesperrt; die Meldung steht. *Beleg:* Vitest `HistoryTab.test.tsx`
   (`document.activeElement` ist das Banner – vor dem Fix `<body>`).
2. Die leere Karte verhält sich unverändert. *Beleg:* bestehende Vitest-Fälle, E2E
   `shop-verlauf-wiederholen.spec.ts`.
3. Rollengang: der Sohn sieht im Browser, dass der Fokusring nach dem Klick auf der Meldung liegt. Dafür braucht
   die Karte mehr als 20 Käufe (`HISTORY_PAGE`) und eine scheiternde zweite Seite – im Rollengang per
   Punkte-Gutschrift des Vaters, Kaufschleife über die echte API und Fetch-Wrapper in der Seite; fällt das aus,
   steht es als Zeile im Verlauf (Vitest + Code-Lesung als Ersatz).

## Schätzung

**Größe: XS** — zwei Zeilen Code, ein Vitest-Fall. Anker: B-02 (XS), ohne eigene E2E (der Zustand braucht
21 Käufe; die Regel ist dieselbe wie an den vier bewachten Stellen, der Vitest misst den Fokus).

**Risiken:** keine im Code; der Browser-Rollengang ist aufwendig (siehe AK 3).

**Angriffsplan:** (1) Vitest rot, (2) `SohnShop.tsx`, (3) Suite.

**Testweg:** `vitest run src/sohn/HistoryTab.test.tsx` (erwartet rot 1/1: activeElement ist `<body>`).

## Verlauf

- **2026-10-17** — angelegt aus der **Nachschau** zu Sprint 1 des Nachtlaufs (Commit `d834a8e`),
  `entgangen_bei: B-216`.
- **2026-10-17** — **idee → ausformuliert** im zweiten Lauf der Nacht: Ist-Stand an fünf Stellen belegt,
  `unverifiziert: false`.
- **2026-10-17** — **ausformuliert → gegrillt**, autonom unter Freigabe 1: Fehlerfall wie B-216 E5; der Fall
  ohne Meldung wird die Frage B-231 (gehört dem Nutzer).
- **2026-10-17** — **gegrillt → geschaetzt**: XS, frontend, Vitest misst den Fokus.
- **2026-10-18** — **geschaetzt → abgenommen** im Nachtlauf (zweiter Lauf der Nacht 2026-10-17, Sprint 3).
  **Rote Probe (erwartet/gemessen):** `HistoryTab.test.tsx:117` 1/1 (`activeElement` erwartet das Banner,
  gemessen `<body>`). Belege: `tsc -b` sauber · `vitest run` 389/389 · `frontend-reviewer` kein Fund an dieser
  Stelle · Skill `web-design-guidelines` (Freigabe 7): kein Fund – Fokusring über `.banner[tabindex]:focus-visible`
  vorhanden, Knopf nur während der Anfrage gesperrt, „Lädt…" endet auf „…". **Browser-Rollengang in Chrome
  (Freigabe 6) als Sohn, AK 3 erfüllt:** Vater schreibt 1 100 Münzen gut (`POST supervisor/children/1/points`),
  Artikel „Sticker" (1 Münze, Bestand 60) angelegt, 21 Käufe über die echte API (24 Käufe gesamt, Seite 20) →
  Verlauf zeigt „Mehr laden (20 von 24)"; Fetch-Wrapper lässt Seite 2 scheitern → Meldung „Failed to fetch"
  über den Zeilen; Wrapper auf 2,5 s Verzögerung, „Mehr laden" geklickt → **im Fenster gemessen:
  `document.activeElement` ist das Banner**, Knopf „Lädt…" gesperrt, Meldung steht; danach 24 Zeilen, keine
  Meldung, kein Knopf. Commit `5a91d27` (Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`, „Zweiter Lauf").
