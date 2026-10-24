---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-24, Fünfter Nachtlauf]
---

# PM-Sitzung: Nachtlauf 2026-10-24 — NP-5 (Vater-Web am Tablet), unterbrochen

**Datum:** 2026-10-24  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** die Pakete NP-5 und NP-6 aus der [Backlog-Sichtung](pm-sitzung-2026-10-23-backlog-sichtung.md) fahren,
in dieser Reihenfolge.

Auftrag: der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–9 plus *„Fahre die Pakete NP-5,
NP-6 aus docs/pm-sitzung-2026-10-23-backlog-sichtung.md, in dieser Reihenfolge."* Ausgangslage: HEAD `515022c`,
`git status` sauber. Ausdrücklich ausgenommen: B-15, B-17, B-21 (Sprint 7), NP-3 und die Nachschau-Defekte der
Vorläufe (B-257, B-260 u. a.). Sprints laufen weiter durchnummeriert (10, 11). Die Chrome-Extension wurde nicht
benutzt; der Rollengang von B-68 ist seine eigene E2E-Spec (Freigabe 6, Ersatz).

## Nachschau zu Sprint 9 (erste Pflichthandlung)

Frischer Prüfer, read-only über `90387d5`/`515022c`, je Story benannte Prüfpunkte (Auflage 4, Zeilen im `## Verlauf`
jeder Story). Die Testzahlen des Commits sind nachgerechnet (Backend 895 + 5 = 900, Vitest 406 + 3 = 409, Playwright
81 → 82 `test(`-Aufrufe), die Freigabe-Commits `3615d46`/`7b8ac96` existieren und tragen die genannten Entscheidungen.

- **B-242, B-239, B-214:** kein Befund, mit Prüfpunkten (Sweeps nachgerechnet, Verweise gesucht, Locator gegen
  `VaterVocab.tsx:474`).
- **B-201:** kein Verhaltensbefund (Text gegen `VaterWizard.tsx:235/366`, `PlanPositionsController.cs:184`,
  `PlanPositions.tsx:194/512/632`); Doku-Nit: der Kommentar `PlanPositions.tsx:505-510` zitiert die Feldhilfe noch im
  alten Wortlaut.
- **B-181: eine Entgleitung → [B-261](backlog/B-261-produktionspfad-des-beispielkatalogs-ungetestet.md)**
  (`entgangen_bei: B-181`): der Produktionspfad `OpenApiExampleCatalog.Load` und die Pause hängen an keinem Test;
  dazu vier verschobene Mutanten-Zeilen im Verlauf. Den einzigen Aufrufer (`Program.cs:290`) und die Zeilen selbst
  nachgelesen.
- **B-177: eine schwache Entgleitung → [B-262](backlog/B-262-vorwahl-bestehendes-kind-ohne-eigene-spec.md)**
  (`entgangen_bei: B-177`): AK 2 („ohne Eingabe bleibt die Vorwahl") hält keine Spec gezielt.

`nachgeschaut: 2026-10-23` (UTC-Datum zur Zeit des Eintrags) auf allen sechs gesetzt. Beide Funde liegen im
Increment des **vierten** Laufs; nach der Auslegung von Limit 2 aus dem dritten Lauf beenden sie diesen Lauf nicht,
und der Auftrag nimmt Nachschau-Defekte früherer Läufe vom Bau aus – darum nur angelegt, nicht behoben.

## Sprint 10 — Ziel & Umfang (NP-5)

**Sprint-Ziel (aus dem Sitz des Vaters):** *Ich bediene jede Seite des Vater-Webs am Tablet (768 px) ohne seitliches
Scrollen – auch wenn ich eine Feldhilfe aufklappe –, und ein Screenreader sagt jede Rückmeldung und jede
Kästchen-Gruppe an.*

**Umfang:** B-68 (`Wunsch`, `geschaetzt` → baubar nach Freigabe 8), B-236 (`Defekt`, `idee`), B-134 (`Defekt`,
`ausformuliert`) – beide autonom grillbar (Freigabe 1). Nachprüfungs-Zeilen gelesen und am Code bestätigt. Dazu
B-263 (`Defekt`), beim Bau von B-68 gefunden (Freigabe 3, Zähler 1).

**Entwickler-Brief:** reines Frontend, kein Vertrag, keine Migration. **Quelle der Wahrheit:** die gemessene
Breite im echten Browser (`document.documentElement.scrollWidth`), nicht die Zahl der `.table`-Fundstellen. **Guards:**
das Netz am `html` schneidet ab, die Spec misst trotzdem; `InfoHint` hält sich im sichtbaren Streifen; Live-Regionen
stehen vor ihrem Text im DOM (Wächter mit Ausnahmeliste). **Testweg:** `responsive-vater.spec.ts` über 22 Routen mit
jedem sichtbaren ⓘ, Vitest-Wächter für Live-Regionen, rote Probe je Fix.

## Iteration 10 — Stand bei der Unterbrechung

- **B-68** (gebaut, `in-arbeit`): `html { overflow-x: hidden }` als Netz (nur `html`, Begründung in der Story),
  `.vater-main { min-width: 0; overflow-x: auto }`, neue Spec `frontend/e2e/responsive-vater.spec.ts`. **Die Prämisse
  war veraltet:** mit Seed-Daten läuft bei 768 px keine der 22 Routen über; über stand nur das ⓘ-Popover.
- **B-263** (neu, `Defekt`, gebaut, `in-arbeit`): `InfoHint` misst beim Öffnen, verschiebt nach links und klappt nach
  oben, wenn `.vater-main` es abschnitte.
- **B-134** (autonom gegrillt und geschätzt, nicht gebaut): Wächter `frontend/src/liveRegions.test.ts` geschrieben
  und rot gemessen (13 Funde, nach der einen begründeten Ausnahme 12) – liegt im Stash, weil er bis zum Umbau der
  zwölf Stellen rot ist.
- **B-236** (Kandidaten geprüft, Vorschlag für das Grillen notiert, nicht gebaut).

**Verifikation:**

| Probe | erwartet | gemessen |
| --- | --- | --- |
| Spec gegen HEAD (`InfoHint` + CSS alt) | Dokument-Überlauf am ⓘ | **11** an `:64` (Popover-Rechtskante 778–981 px) + **11** an `:66`; Routen ohne ⓘ **0** |
| neues CSS, `InfoHint` alt | ⓘ abgeschnitten | **11** an `:66`, **0** an `:64` |
| Fix (erste Fassung) | grün | **1 passed** |
| B-134-Wächter am Ausgangsstand | 12 offene Stellen | **13**, mit Ausnahme (Pager) **12**; sieben Erkennungsfälle grün |
| `tsc -b` / Vitest (ohne Wächter) | grün | sauber / **409/409** |
| Playwright voll (nach dem letzten Code-Edit; danach nur noch ein Kommentar in der Spec) | grün | **83 passed** (82 + die neue Spec) |

### Review-Funde (Zähler: 5 von 5 – an der Grenze, nicht darüber)

Gezählt (die Behebung ändert Code oder Tests): (1) B-263 selbst, eigene Messung beim Bau; aus dem `frontend-reviewer`
(B-68/B-263, kein Blocker): (2) `.vater-main` scrollt auch senkrecht – ein ⓘ unten würde abgeschnitten, die Spec sah
das nicht → Spec misst `scrollHeight`, `InfoHint` klappt nach oben; (3) B-263 AK 2 („Ruhelage bleibt") ohne Test →
eigener Spec-Schritt; (4) `POP_LEFT` doppelte die CSS → `marginLeft`; (5) der Effekt war nicht idempotent → angewandte
Lage wird herausgerechnet. Ohne Zähler: Story-Text nachgezogen, ein schiefer CSS-Kommentar, die Probe „Netz allein".
**Bewusst nicht behoben (Nits, jeder hätte die Grenze überschritten):** Laufzeit der Spec (22 Routen in einem Test,
60-s-Limit – `test.slow()` erwägen), `res.ok` beim Plan-Anlegen in der Spec, `visibleBounds` verengt auch in einem
`Modal` (erst ab ~1044 px spürbar). Ein Delta-Review der Behebung steht aus.

**Proben nach den Behebungen** (Skript, je ein Playwright-Lauf; Mutante im Speicher, danach zurückgelegt):

| Probe | erwartet | gemessen |
| --- | --- | --- |
| Fix (Endfassung) | grün | **1 passed** |
| `InfoHint` verschiebt jedes Popover (`shift = room`) | Ruhelage-Schritt rot | **1 Fehler an `:137`** (`toEqual({ dx: -6, unten: true })`) |
| Netz allein: `.vater-main`-Regel entfernt, `InfoHint` alt | Dokument-Zusicherung trotz `html { overflow-x: hidden }` rot | **11** an `:67` + **11** an `:69` – `scrollWidth` meldet den Überlauf auch unter dem Netz |
| Klappen nach oben abgeschaltet | senkrechte Zusicherung rot? | **1 passed** – an keinem ⓘ der 22 Routen schneidet `.vater-main` unten ab; der Klapp-Zweig ist begründet, aber von keinem Fall ausgelöst, die Zusicherung ist ein Wächter |

## Rollengang und Retrospektive

**Nicht geführt.** Der Rollengang des Vaters am Tablet ist für B-68/B-263 die eigene Spec (Freigabe 6, Ersatz: echter
Browser, echter Server, 768 px); Sohn und Creator sind Regressionszeugen über die volle Playwright-Suite (83 passed) –
ihre Pfade berührt der Diff nur über das `html`-Netz (vom Reviewer für Arcade und Landing ohne Wirkung gelesen). Der
Sprint ist nicht geschlossen, darum gibt es keine Retrospektive und **keinen Retro-Vorschlag**; die Nachschau, ihre
erste Zeile, ist oben schon erledigt. Eine Beobachtung für die spätere Retro: in diesem Lauf war die
Prämisse einer geschätzten Story am Code falsch, obwohl die Sichtung vom 2026-10-23 sie nachgeprüft hatte
(B-68 AK 5: „Seiten laufen über") – die Nachprüfung las Code, nicht das Verhalten im Browser.

## Unterbrechung / Wiederaufnahme

Der Koordinator hat den Lauf unterbrochen: der Rechner wird abgeschaltet. NP-5 ließ sich in der Zeit nicht
schließen (B-134 und B-236 ungebaut, kein Rollengang, keine Retro); **NP-6 (B-64, B-71) ist nicht begonnen**.

**Fertig:**

- Nachschau zu Sprint 9 (siehe oben): sechs Stories mit Prüfpunkten, zwei Entgleitungen → B-261, B-262.
- B-68 und B-263 gebaut und gemessen (siehe Iteration 10), committet als **`in-arbeit`** mit `wartet_auf`.
- B-134 gegrillt und geschätzt (`geschaetzt`), B-236 mit Kandidaten-Prüfung im Verlauf.

**Offen, in dieser Reihenfolge:**

1. `git stash list` → `nachtlauf-2026-10-24 NP-5 wip: B-134 Waechter …` mit `git stash pop` zurückholen
   (enthält nur `frontend/src/liveRegions.test.ts`).
2. B-134 bauen nach seinen Entscheidungen 1–4: Regel `.live-region:empty { position: absolute; }` in `index.css`,
   die zwölf Stellen (Liste im Wächter-Ausgang) auf „Region immer da, Klasse nur mit Meldung" umbauen; Wächter
   grün, Vitest voll.
3. B-236 grillen (Vorschlag `role="group"` + `aria-labelledby` im Verlauf) und bauen; die Stellen stehen im Verlauf.
4. Delta-Review für B-68/B-263 (die Behebungen nach dem `frontend-reviewer`), dann `frontend-reviewer` für B-134/B-236
   – **nacheinander**. Achtung: der Sprint-Zähler steht bei **5**; jeder weitere gezählte Fund in Sprint 10 überschreitet
   die Grenze und beendet den Lauf (Freigabe 3). Ein Folgelauf kann das als neuen Sprint werten, wenn der Nutzer das
   so entscheidet – das ist eine offene Entscheidung, keine Auslegung, die der Lauf selbst treffen darf.
5. Playwright voll (nach dem letzten Edit), Rollengang (Vater am Tablet: die Spec; Screenreader-Teil per Wächter und
   Komponententest, ein Mensch prüft die Ansage am echten Screenreader → `wartet_auf`), Retrospektive mit der Nachschau
   zu **Sprint 9** als erster Zeile (schon erledigt, hier oben), dann Abnahme und NP-6.
