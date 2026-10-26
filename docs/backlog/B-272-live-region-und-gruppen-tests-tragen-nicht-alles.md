---
tags: [typ/story, status/idee, bereich/frontend, bereich/a11y, bereich/test]
aliases: [Wächter-Selbsttests B-134, Gruppen-Spec zu weit]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu B-134/B-236 in Lauf A (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
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

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-134/B-236 in Lauf A
  ([Protokoll](../pm-sitzung-2026-10-26-tag.md)), Hinweise 3–6.
