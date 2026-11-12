---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor, rolle/student]
aliases: [Leerfall an items.length, leere Seite jenseits des Bestands]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Review Sprint 46, B-504)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-505 · Paged-Listen sagen „noch keine …" für eine leere Seite, nicht für einen leeren Bestand

Gefunden im `frontend-reviewer` zu B-504 (🟢, außerhalb des Diffs, bewusst nicht in Sprint 46 – das Sprint-Ziel hielt
ohne). B-504 hat die Regel im Lernstand an die Senke gelegt (`total === 0`); acht weitere Listen hängen ihren Leerfall noch
an die Länge der Seite (`items.length === 0`):

- `frontend/src/vater/VaterClassTests.tsx:139` „Noch keine Klassenarbeiten."
- `frontend/src/vater/VaterKonto.tsx:79` „Noch keine Buchungen."
- `frontend/src/vater/VaterShop.tsx:476` „Noch keine Käufe."
- `frontend/src/vater/VaterAnmerkungen.tsx:155`
- `frontend/src/vater/ClozeTexts.tsx:111`
- `frontend/src/vater/VaterVocab.tsx:324` „Keine Vokabeln in dieser Sprach-Kombination."
- `frontend/src/vater/VaterZiele.tsx:229` „Noch keine großen Ziele."
- `frontend/src/sohn/SohnKonto.tsx:41` (`account.noEntries`)

Erreichbar, wenn die letzte Zeile der letzten Seite gelöscht wird oder ein Filter den Bestand unter `skip` drückt. Die
Sätze sagen „nichts da", keinen Erfolg – darum P3. Frage fürs Ausformulieren: eine gemeinsame Senke (Leerzeile im
`Pager`-Umfeld bzw. ein Helfer, der `total` liest) statt acht Einzelstellen; und ob die leere Seite auf die letzte
gültige zurückspringen soll.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf 2026-11-12, Nutzerrolle übertragen):

1. **Die Senke ist der `Pager`:** steht `skip` hinter dem Bestand (`total > 0 && skip >= total`) und ist nichts unterwegs,
   springt er auf die letzte gültige Seite (`onSkip`). Heute verschwindet er dort sogar (`total <= take` → `null`), und
   der Vater steht ohne Weg zurück auf einer leeren Seite. Eine Stelle für alle sechs geblätterten Listen.
2. **Der Leerfall liest den Bestand:** die sechs geblätterten Listen (`VaterClassTests`, `VaterKonto`, `VaterShop`,
   `VaterAnmerkungen`, `ClozeTexts`, `VaterVocab`) sagen „Noch keine …" nur bei `total === 0` (wie B-504 im Lernstand).
3. `VaterZiele` und `SohnKonto` blättern nicht (kein `skip`), dort fallen leere Seite und leerer Bestand zusammen –
   bewusst unverändert.

## Akzeptanzkriterien

1. `Pager` mit `skip=25`, `total=25`, nicht busy: ruft `onSkip(0)`; mit `total=60`, `skip=75`: `onSkip(50)`.
2. Busy oder `total=0`: kein Sprung.
3. Jede der sechs Listen: leere Seite mit `total > 0` zeigt nicht „Noch keine …"; `total = 0` zeigt es.

## Schätzung

**Größe: S** — eine Wirkung im `Pager` (`ListControls.tsx`) plus sechs gleichförmige Zeilen; Test am `Pager` und ein
Quelltext-Test über die sechs Dateien (kein nachgebauter Bildschirm).

## Verlauf

- **2026-11-12** — angelegt (Review Sprint 46 zu B-504).
- **2026-11-12** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 49).
- **2026-11-13** — gebaut (Sprint 49). `Pager` springt (Wirkung vor dem frühen `return null`); Leerfall an `total`. Der neue
  Wächter `src/leerfall.test.ts` fand zwei Stellen, die die Story nicht nannte: `VaterExercises.tsx:216` (Form
  `(…items.length ?? 0) === 0`) und – nach der Verbreiterung durch das Review – die ungeblätterte Suchliste
  `VaterClassTests.tsx:220` (auf `total` umgestellt, gleiches Verhalten, damit der Wächter je Datei scharf bleibt).
  - AK 1 → `springt bei einer Seite Bestand auf Seite 1`, `springt auf die letzte gültige Seite…` (F7 → rot),
    `zählt eine Seite, die genau am Bestand beginnt…` (F6 → rot).
  - AK 2 → `springt nicht, solange etwas lädt, und nicht bei leerem Bestand`; F5, F8 → rot.
  - AK 3 → `leerfall.test.ts` über alle acht Quellen mit `<Pager`; F9, F10, H4 → rot; Selbsttest über fünf Formen.
  - Verifikation: Vitest 810/810 (78 Dateien), `tsc -b` sauber; Mutanten-Probe nach der letzten Änderung 17/17 rot je am
    genannten Test (Tabelle im Protokoll `docs/pm-sitzung-2026-11-12-tageslauf.md`, Sprint 49). Playwright 140/140 auf
    Ausweichports (5201/5174) – vor den Review-Nachzügen H1–H4, die tragen Komponententests.
  - Rollengang Vater: E2E-Suite gegen echten Server und echtes UI; Creator, Sohn: Regressionszeugen (Suite grün, kein
    Pfad ihrer Ebenen außer `SohnKonto` unverändert). Abgenommen.
