---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Schalter geteilte Übungen springt zurück]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-450 · „Geteilte Übungen anderer Väter anzeigen" springt nach dem Klick zurück

Fund 6 in Sprint 28 (Lauf AG), gefunden vom vollen E2E-Lauf: `e2e/typ-manifest-faellt-aus.spec.ts:40` – „Clicking the checkbox did not
change its state". Ursache, am Code analysiert: der Review-Fix zu B-428 hat den Schalter in die Adresse gelegt (`?shared=1`,
`frontend/src/vater/VaterExercises.tsx`, `setFilter`). Fach-Wechsel und Schalter-Klick gehen beide über `setSearchParams`; der zweite
Aufruf baut die Adresse aus den Werten des **alten** Renders (veraltete Closure – das gerade gewählte Fach fehlt), und React Router
rendert die Adress-Änderung verzögert, darum springt der kontrollierte Haken zurück.

~~Reparaturweg: `setParams((prev) => …)` in der funktionalen Form~~ (überholt, siehe Verlauf 2026-11-10), die die aktuelle Adresse ändert statt sie aus Render-Werten neu zu bauen;
danach `typ-manifest-faellt-aus.spec.ts` und `creator-katalog-ueberblick.spec.ts` grün, eine Mutante „Render-Werte statt `prev`".
Blockiert die Abnahme von B-426, B-428, B-431 (`wartet_auf`).

## Verlauf

- **2026-11-09** — angelegt beim Ende von Lauf AG: Fund 6 überschritt den Zähler von Sprint 28 (Freigabe 3), der Lauf endete sofort,
  ohne Reparatur.
- **2026-11-10** — gebaut im Dialog („Ja, B-450 reparieren"); Nacharbeit mit neuem Zähler (Regel vom 2026-10-26). **Die notierte
  Reparatur war falsch:** react-router-dom 6.30 gibt `prev` aus dem Render (`node_modules/react-router-dom/dist/index.js:1031`), die
  funktionale Form ändert nichts. Und `main.tsx` setzt `v7_startTransition` – die Adress-Änderung rendert in einer Transition, bis dahin
  setzt React den kontrollierten Haken zurück. **Reparatur:** eigener Zustand `showShared` für den Haken, im Render mit der Adresse
  abgeglichen (wie `prevFilterKey`); `setFilter` nimmt einen Patch und baut die Adresse aus `window.location.search` (Vorsorge, s. u.).
  **Rote Probe mit Zahl:** der Stand vor der Reparatur fällt isoliert in **2 von 3** Läufen (`typ-manifest-faellt-aus.spec.ts:40`) – ein
  Wettlauf, darum alle Proben mit `--repeat-each=6`. Neuer E2E-Fall in `creator-katalog-ueberblick.spec.ts` (Haken folgt dem Klick und der
  Adresse).
  **Belege** – `.claude/scripts/mutant-probe.py`, je 6 Wiederholungen beider Fälle, nach dem letzten Edit an `48173c0 + Arbeitsbaum`, 2026-11-10 09:55:

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | Haken folgt dem Klick | `typ-manifest-faellt-aus.spec.ts` + Fall B-450 (`check()`) | MB Haken an der Adresse (`VaterExercises.tsx:172`) | red |
  | Haken folgt dem Klick | dieselben | MD Klick setzt den eigenen Zustand nicht (`:173`) | red |
  | Haken folgt der Adresse | Fall B-450, Navigation auf dieselbe Route | MC Abgleich im Render weg (`:55`) | red |

  Grundlinie (Reparatur) unter Wiederholung grün. **Ungemessen / Vorsorge:** die Adresse aus `window.location.search` statt aus
  Render-Werten – die Mutante „Adresse aus `params`" blieb in 6 Wiederholungen grün; die zugehörige URL-Zusicherung im Test ist als
  Absichtserklärung beschriftet, der Code-Kommentar sagt dasselbe.
- **2026-11-10** — Review `frontend-reviewer`: keine 🔴; 🟡 die Zusicherungen „Fach bleibt stehen" konnten nicht fallen und waren nicht
  beschriftet → als Absichtserklärung beschriftet, redundante `toHaveValue`-Zeile gestrichen (**Zähler 1**); 🟢 Abgleich per Effekt →
  Render-Muster der Datei; 🟢 Pulldowns flackern in derselben Transition → als bewusst hingenommen im Kommentar (ohne Zählung).
- **2026-11-10** — abgenommen. Backend 1187/1187, Vitest 723/723, `tsc -b` sauber, Playwright **131 passed** (ganze Suite, Server vom Lauf nach dem letzten Edit gestartet). **Rollengang** Creator = `e2e/creator-katalog-ueberblick.spec.ts` (Fall B-450) und
  `typ-manifest-faellt-aus.spec.ts`; Vater/Sohn als Regressionszeugen (Suite grün). Commit: siehe `git log -- docs/backlog/B-450-schalter-geteilte-uebungen-springt-zurueck.md`.
