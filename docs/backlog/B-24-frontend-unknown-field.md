---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [unknown_field im Frontend]
status: idee
prio: P2
art: Frage
quelle: memory/codequalitaet-gates.md
unverifiziert: true
---

# B-24 · Frontend gegen `unknown_field` durchspielen

Das Backend lehnt unbekannte Felder ab (`UnmappedMemberHandling.Disallow` → `400 unknown_field`). Ob das
Frontend irgendwo ein Feld schickt, das der Vertrag nicht kennt, ist **nie verifiziert** worden.

**Ungeprüft:** genau das ist die Aufgabe. Ein Durchgang durch alle schreibenden Masken; jeder Treffer ist
ein Formular, das heute still scheitert oder scheitern wird.

## Zuschnitt gekürzt durch B-42 (2026-07-31)

[B-42](B-42-openapi-typen-generieren.md) erzeugt die TypeScript-Vertragstypen aus dem OpenAPI-Dokument; danach
bricht `tsc` bei jedem Feld, das der Vertrag nicht kennt – **sofern die Nutzlast typisiert übergeben wird**.
Der Handdurchgang durch alle schreibenden Masken erübrigt sich damit. Was **bleibt**, ist der Rest, den ein
Generator nicht sehen kann: Stellen, an denen ein Objekt-Literal mit Zusatzfeld untypisiert abgeschickt wird
(kein Typ verlangt es, also meldet `tsc` nichts).

Neuer Zuschnitt dieser Story: **die untypisierten Absende-Stellen finden**, eingereiht **hinter** B-42.
Bewusst nicht verworfen – wer sie streicht, verliert genau diesen Rest aus dem Blick.

## Der Rest ist gezählt (2026-08-01, nach B-42 Schritt 2)

Die Lücke ist keine Vermutung mehr, und sie hat eine einzige Ursache: `http<T>(url, method, body?: unknown)`
in [api.ts](../../frontend/src/lib/api.ts) nimmt den Rumpf als **`unknown`**. Wer ein typisiertes DTO
durchreicht, ist geprüft; wer ein Objekt-Literal an dieser Stelle baut, ist es nicht – `tsc` hat nichts, wogegen
es prüfen könnte.

| Schreib-Aufrufe in `api.ts` | Zahl | Von `tsc` bewacht? |
| --- | --- | --- |
| Rumpf ist ein typisierter Bezeichner (`dto`, `p`, …) | 52 | **ja**, seit B-42 Schritt 2 |
| Rumpf ist ein Objekt-Literal (`{ name }`, `{ adultId, pin }`, …) | **34** | **nein** |

Damit ist auch die Reichweite von B-42 ehrlich benannt: **60 % der Schreibpfade**, alle Lesepfade. Die 34
sind fast alle klein (ein bis drei Felder) – der Zuschnitt ist also „je Aufruf den passenden Vertragstyp
annotieren", nicht „Masken durchklicken". Ob eine generische Signatur (`body?: TBody`) das billiger macht als
34 Annotationen, ist die erste Frage beim Grillen.

Die Zahl war zwischenzeitlich **40**: der `frontend-reviewer` fand sechs der 52 „typisierten" Aufrufe, die
nichts bewachten – vier inline getippte Rümpfe (`dto: { name?: string; … }`, also faktisch Literale) und zwei,
die gegen das **falsche** Schema prüften (`Partial<MissionDef>`/`Partial<AchievementDef>` typisierten den PATCH
gegen die *Antwort*; `id`/`metric`/`period` hätte `tsc` erlaubt und der Server mit `400 unknown_field`
abgewiesen). Alle sechs sind in E6 geschlossen (`UpdateMissionDto`, `UpdateAchievementDto`, `CreateTeacherDto`,
`CreateTimetableEntryDto`, `UpdateChapterDto`, `CreateTagDto` – alle lagen längst im Dokument und fehlten nur im
Barrel). Die 34 sind also nicht mehr geschätzt, sondern der geprüfte Rest.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-07-31** — Zuschnitt gekürzt: B-42 nimmt den Handdurchgang ab, hier bleiben die untypisierten
  Nutzlasten. Stufe unverändert `idee` (Entscheidung 4 im Grillen von B-42).
- **2026-08-01** — **entblockt und gezählt.** B-42 Schritt 2 ist abgenommen, die Vertragstypen sind generiert.
  Der verbleibende Rest ist gemessen: 34 von 86 Schreib-Rümpfen sind Objekt-Literale an einem
  `body?: unknown`. Damit ist die Story nicht mehr „ungeprüft" in ihrer Kernbehauptung – nur die Frage, welche
  der 34 tatsächlich ein falsches Feld schicken, ist noch offen. `unverifiziert` bleibt darum stehen.
