---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/a11y, bereich/test]
aliases: [Wächter-Selbsttests B-134, Gruppen-Spec zu weit]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer zu B-134/B-236 in Lauf A (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-03"
entgangen_bei: []
wartet_auf: ""
---

# B-272 · Tests zu B-134 und B-236 lassen Mutanten grün

Der `frontend-reviewer` hat in Lauf A (Fortsetzung Sprint 10) zu Commit `8bc78d8` vier Hinweise (🟢) gegeben, das Delta-Review einen fünften. Jeder
benennt eine Mutante, die grün bliebe:

1. **Selbsttests des Wächters** (`frontend/src/liveRegions.test.ts`): Kein Fall hat die Region im Else-Zweig
   (`{x ? null : <p role="status">}`). Damit bleibt das Streichen von `:` aus der Bedingungs-Regex grün. Ebenso fehlt
   ein JSX-Kommentar zwischen `&&` und Tag, und es gibt keine Pfeil-Komponente `const C = () =>`. Der
   Ausnahmen-Test prüft nur, dass das Tag noch existiert, nicht, dass es noch einen Fund erzeugt. Eine überholte
   Ausnahme bliebe so still stehen.
2. **`TruncationHint`-Vitest** (`frontend/src/components/ListControls.test.tsx:56-59`): `textContent === ""` gilt
   mit und ohne Region. Die Mutante `if (total <= shown) return null` fängt nur der Wächter. Gebraucht werden
   `getByRole("status")` bei voller Liste und ein `rerender` 12/12 → 12/40 auf **denselben** Knoten.
3. **B-236, Name „Freigabe (Startzustand)“** (`frontend/src/vater/VaterExerciseCreate.tsx:292`): Die `id` sitzt am
   ganzen Span. Entscheidung 2 von B-236 will nur das Stichwort. Entweder ein eigener `<span id>` um „Freigabe“
   samt angepasster Spec, oder in der Story festhalten, dass „Startzustand“ hier zur Bedeutung gehört.
4. **B-236-Spec zu weit** (`frontend/e2e/gruppenkopf-benennt-die-gruppe.spec.ts:49-58`): Rückt `role="group"` an die
   umgebende `<section>`, bleibt alles grün. Je Gruppe gehört dazu, dass nichts Fremdes darin liegt (z. B.
   `toHaveCount(0)` für ein Kästchen einer anderen Gruppe).
5. **`REGION_BAUSTEINE` ohne Selbsttest je Name** (`frontend/src/liveRegions.test.ts`, aus dem Delta-Review): Streicht man
   `TruncationHint` oder `SavedNotice` aus der Liste, bleibt alles grün. Vorschlag: `it.each(REGION_BAUSTEINE)` mit
   `{x && <Name a={1} />}` → 1. Dazu fehlen `DerivedHint` (`VaterFachlehrer.tsx`) und `DuplicateHint` (`VaterVocab.tsx`),
   die ebenfalls eine Region rendern; heute hängt keiner von beiden bedingt.

**Nicht in Lauf A behoben, sondern ausdrücklich zurückgestellt:** Jede Behebung ändert Tests oder Code und zählt
damit als Fund (Freigabe 3). Die beiden 🟡-Funde desselben Reviews wurden behoben (Zähler 2), und das Review zu
B-263 stand noch aus. Das ist dieselbe Abwägung wie bei B-268/B-269, die der Nutzer am 2026-10-26 als richtig
bestätigt hat.

## User Story

Als **Entwickler** möchte ich, dass jeder Teil des Live-Region-Wächters, der `TruncationHint`-Test und die Gruppen-Spec
eine benannte Mutante rot machen, damit ein Rückbau von B-134 oder B-236 nicht grün durchläuft.

## Ist-Stand am Code (Lauf P, Stand `299d216`)

1. `frontend/src/liveRegions.test.ts`: kein Fall mit der Region im Else-Zweig, keiner mit JSX-Kommentar vor dem Tag, keiner
   mit Pfeil-Komponente; der Ausnahmen-Test prüft nur `includes(tag)`.
2. `components/ListControls.test.tsx:56-59` prüft `textContent === ""` – das gilt auch ohne Region.
3. `vater/VaterExerciseCreate.tsx:310ff.`: Gruppenname „Freigabe (Startzustand)"; der Kommentar `:299-307` begründet,
   warum die Beschriftung vom **Startzustand** spricht.
4. `e2e/gruppenkopf-benennt-die-gruppe.spec.ts` prüft je Gruppe nur, was darin sein soll.
5. `REGION_BAUSTEINE` ohne `DerivedHint` (`VaterFachlehrer.tsx:153`) und `DuplicateHint` (`VaterVocab.tsx:344`).

## Die echte Lücke

Wie beschrieben, mit einer Verschärfung zu 5: der vorgeschlagene `it.each(REGION_BAUSTEINE)` leitet seine Fälle aus der
Liste selbst ab – streicht man einen Namen, verschwindet sein Fall mit, und alles bleibt grün.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Drei Selbsttest-Fälle** (Else-Zweig, JSX-Kommentar, `return null` in der Funktion vor einer Pfeil-Komponente) und ein
   Ausnahmen-Test, der je Eintrag einen **Fund** verlangt. (Ein vierter Fall wurde im Review gestrichen, siehe Verlauf.)
2. **`REGION_BAUSTEINE` wird gegen die Quellen geprüft:** jede Komponente, deren eigenes `return` als erstes Element eine
   Region oder einen Baustein liefert, muss in der Liste stehen, und umgekehrt. *Kosten:* ein halber Parser mehr
   (Funktionskopf in Spalte 0, `return` mit zwei Leerzeichen); eine Pfeil-Komponente, `export default function` und ein
   Fragment `<>` an der Wurzel sieht er nicht (heute keiner davon mit Region).
3. **„Freigabe (Startzustand)" bleibt der Gruppenname.** Der Zusatz gehört hier zur Bedeutung: die Gruppe stellt den
   Startzustand ein, nachträglich wird anderswo umentschieden (Kommentar `VaterExerciseCreate.tsx:299-307`). Abweichung von
   B-236 Entscheidung 2, ohne Code. *Kosten:* der Name ist länger als bei den anderen Gruppen.
4. **Je Gruppe eine `toHaveCount(0)`-Zusicherung** auf ein Kästchen einer anderen Gruppe.

## Akzeptanzkriterien

1. Jede der Mutanten „ohne `:`", „ohne Kommentar-Entfernung", „ohne Pfeil-Kopf", „Ausnahme überholt" macht einen Fall rot.
2. `TruncationHint` mit `return null` bei voller Liste macht einen Fall rot.
3. Rückt `role="group"` an die umgebende Karte, fällt die Gruppen-Spec.
4. Fällt ein Baustein aus `REGION_BAUSTEINE`, fällt ein Fall.

## Schätzung

`S`, frontend (nur Tests). Testweg: `liveRegions.test.ts`, `ListControls.test.tsx`, Gruppen-Spec mit Mutante.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-134/B-236 in Lauf A
  ([Protokoll](../pm-sitzung-2026-10-26-tag.md)), Hinweise 3–6.
- **2026-11-03** — Lauf P, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Alle neuen Fälle vor jeder
  Mutante grün (Testlücke, das Verhalten stimmte). AK 1 → „ohne `:`" **1 failed** („Ternär, Region im Else-Zweig"), „ohne
  Kommentar-Entfernung" **1 failed** („&& mit JSX-Kommentar davor"), „ohne Pfeil-Kopf" **1 failed** („return null in der
  Funktion vor einer Pfeil-Komponente"), „Ausnahme überholt" (`return null` im `Pager` gestrichen) **1 failed**; AK 2 →
  „`return null` bei voller Liste" **1 failed**; AK 3 → Gruppe an die Karte gerückt: Spec **1 failed** an der neuen
  `toHaveCount(0)` (`:24`); AK 4 → „`SavedNotice` gestrichen" **1 failed**. Beim Bau gefunden: die mehrzeilige Signatur von
  `DuplicateHint` beendete den Rumpf zu früh – Rumpfende ist jetzt eine Zeile aus `}` allein. Vitest **619/619**.
- **2026-11-03** — Review `frontend-reviewer`: 🟢 der Fall „return null in einer Pfeil-Komponente" hatte keine eigene Mutante (unter „ohne Pfeil-Kopf" findet die Suche dasselbe `return null`) → gestrichen, der Gegenfall davor trägt die Regel (ohne Zählung). 🟢 der Kommentar am `REGION_BAUSTEINE`-Test nannte eine ungemessene Hälfte → gemessen: eine neue Komponente `ProbeHinweis` mit `role="status"` an der Wurzel, nicht in der Liste, **1 failed** (ohne Zählung). Vitest **619/619**.
- **2026-11-03** — Abnahme Lauf P, Teil 1: Vitest **619/619**, `tsc -b` sauber, E2E-Gesamtlauf **103 passed** (`exit=0`, nach dem letzten Code-Edit gestartet). Rollengang: Der Entwickler ist die Rolle – jede Probe mit benannter Mutante; Vater über die Gruppen-Spec im echten Browser (Barrierefreiheitsbaum); Creator und Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf P](../pm-sitzung-2026-11-03-nacht.md)).
- **2026-11-03** — Nachschau in Lauf P, Teil 2 (frischer `frontend-reviewer`, `git show bffb583 -- frontend`, read-only): **Entgleitung** → [B-359](B-359-live-region-waechter-uebersieht-echte-kommentare.md) (🟡: die Kommentar-Mutante war an ungültigem TSX gemessen; die zwei gültigen Formen blieben unerkannt; im selben Sprint behoben).** Geprüft: `REGION_BAUSTEINE` gegen die Quellen – „`StatusBanner` gestrichen" fällt über `SavedNotice`, die Gleichheit fällt in beide Richtungen; `TruncationHint` – `return null` und Neu-Einhängen je rot; Gruppen-Spec – vor jedem `toHaveCount(0)` eine positive Sichtbarkeit. Entscheidungstexte (Fallzahl, Kosten von `wurzelRegionen`) nachgezogen (🟢).
