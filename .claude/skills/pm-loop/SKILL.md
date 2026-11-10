---
name: pm-loop
description: >-
  Run one Pugling product cycle as the PM coordinating Creator, Vater/Supervisor, Sohn/Student and
  Entwickler: exercise the REAL running app from each role's seat, turn the friction into a prioritized
  backlog, build the fixes API-First, and loop until all three customer roles sign off against the
  running product. Also holds the sprint mechanics: Sprint-Ziel, Rollengang before `abgenommen`,
  Retrospektive, and the authorized autonomous backlog run. Use for "PM-Sitzung", "pm-loop",
  "was bauen wir als Nächstes", "Feedback-Runde", "Sprint fahren", "arbeite das Backlog ab",
  "Retrospektive". NOT the file-based `lehrplan-autor`/`lehrplan-lerner` course format.
---

# PM-Loop

You are the PM. Three customers who want different things, plus the developer:

- **Creator** (`api/v1/creator/*`) authors catalog content, often a teacher. Wants authoring power
  without hand-editing JSON, and to try an exercise before assigning it.
- **Vater** (Supervisor, `api/v1/supervisor/*`) steers and enforces. Wants control, proof of effect,
  fraud-proof points. What he cannot do in the UI does not exist for him.
- **Sohn** (~11, Student, `api/v1/student/*`) has to be pulled in. Wants fun, fairness, a "TADAA"
  moment, and notices anything fake immediately.
- **Entwickler** builds API-First: `api/v1` is the product, the frontend hangs off it.

You make the tradeoff none of them can make alone.

## The constraint everything follows from

The loop ends only when all three customer roles sign off on **pre-stated acceptance conditions,
against the running product, with the change verified.**

- Role feedback comes from exercising the app (UI and/or API), never from imagination.
- A sign-off is earned by walking the role's acceptance list, not declared.
- "Done" = build clean, relevant tests green, flow driven end-to-end.

Stages, entry conditions, frontmatter, the autonomous-run limits and their reasoning live in
`docs/backlog/README.md`. This skill does not repeat them; read the named section when a step points there.

## Step 1 — Set up or resume

Read the most recent `docs/pm-sitzung-*.md` (open roadmap, each role's standing conditions). Take the
date from the shell. Open `docs/pm-sitzung-<YYYY-MM-DD>.md` (skeleton below) or append a round to
today's file. A goal named by the user seeds the agenda but still goes through all three roles.

## Step 2 — Sit in each seat

1. If a Befund exists under `docs/anmerkungen/`, read it first. It was observed by a real person and
   outranks anything you produce yourself; fold it into the matching role and mark it as observed.
2. Make the product reachable (backend `:5200`, frontend `:5173`; `/smoke-test` or `curl` for API only).
   The role skills `creator`, `supervisor`, `student` seat you quickly.
3. Walk the product as each role and record concrete friction in that role's voice
   ("Münzen gehen beim Kauf nicht runter"), each role naming its **Top-3**.

## Step 3 — Synthesize and prioritize

Classify each item by where the work lives: new backend capability (typical for Creator; these enable
content, so weigh them as enablers), UI + client over an existing API (typical for Vater; cheap), or
frontend polish (typical for Sohn). Correctness bugs in a core loop outrank polish regardless of who
raised them.

Write a ranked table (P0…P3, size, backend/frontend/both) and one **red thread** into the log. The
table is a dated snapshot; the durable list is `docs/backlog/`. Everything not built now becomes a
story via `/backlog` (`idee`, `quelle:` this log); existing stories are cited by id, not restated.

- A story that does not fit one sitting runs as a **Karte** (README → "Karten"). Its tickets are
  decided in dialogue with the user unless the user authorized an autonomous run for that initiative
  (README → "Autonomer Modus"). Never infer that authorization from a past session.
- An open-ended "work the backlog" grant has its own limits (README → "Der Backlog-Lauf"): alone you
  may grill only `art: Defekt`/`Aufräumen`; `Wunsch`/`Frage` stay in dialogue; the run stops after a
  sprint whose retro produced a new gate or whose increment carried a defect. Read that section first.

## The Sprint

- One red thread over **at most six** stories, all at `geschaetzt`; no lower bound. The cap keeps
  Step 6 affordable.
- One **Sprint Goal** in a sentence from a role's seat, falsifiable in Step 6 ("the son can play every
  assigned position without a dead end"), not a list of ids. Scope may change when the goal demands it.
- Defined by scope and evidence, not duration. No story points, no separate sprint-backlog artifact.
- **Refinement** (getting candidates from `idee` to `geschaetzt`) is its own phase and often the larger
  one. Log it separately; do not count it into the sprint's size.

## Step 4 — Brief and build, API-First

Write **one Entwickler-Brief per sprint** (goal, server-side source of truth, guards, order between
the stories); each story already has its `Angriffsplan`. Build backend first, then the frontend through
the central client/types, following the `CLAUDE.md` of each area.

Sequential is the default. Fan out to subagents only when stories touch disjoint files, and then at
every fan-in: merge, run the full suite plus guard tests, confirm the sprint goal still holds. Two
traps: hooks measure `$CLAUDE_PROJECT_DIR`, so a worktree's build/test hits the main worktree; and
`SchemaGuardTests` keeps the migration chain at length 1, so two branches that each fold it cannot be
merged, only rebuilt.

## Step 5 — Verify, then review

- Run build and `dotnet test`; for runtime changes `/smoke-test`; for UI flows `npm run test:e2e`.
  Report real counts. Add an integration test for nontrivial backend changes.
- Before the review, one `## Verlauf` line per AK *case* and per part of a decision:
  *AK n, case → test (file:line), mutant → red*. Cases are path × value; a "like X" yardstick joins
  with the same values; a computed prop is a case; a changed shared condition gets one row per
  *action*, found by `grep`. A distinction needs the two values to differ in the setup. `verworfen`
  holds only for the forms measured — name them in `grund`.
- Measure with `.claude/scripts/mutant-probe.py` **after the last edit** and paste its table: per case
  and per condition of a new query, one mutant that turns only it red. `ungemessen`, `abweichend` and a
  line that is green without the fix are no evidence. Size or contract claims: measured at the file,
  command in the line.
- Error messages name the lock and no way out beyond it; a surface they point at must exist and show
  the lock. After rewording, `.claude/scripts/old-phrases.py` finds tests expecting the old text.
- Run `pugling-reviewer` and/or `frontend-reviewer` (by the story's `wo`). Fold in correctness
  findings; note cleanup-only findings as non-blocking.
- If you may not start agents, stop at `in-arbeit`, write one `## Verlauf` line naming the unmet
  condition, and ask the user. Your own reading of the diff is not "the review".
- A finding that surfaces during verification becomes **its own story** if the sprint goal is met
  without it; say there that it was left out deliberately.

## Step 6 — Rollengang: re-review and gate

This is the entry condition for `abgenommen`; autonomous mode does not relax it. Once per sprint.

- **Start the server the walk uses after the last edit**, ideally let the walk start it
  (`/smoke-test`, `playwright.config.ts`). A walk against a stale server is false evidence.
- The role whose tier changed walks its acceptance list. The other two are **regression witnesses**:
  record what they actually did (suite green, own specs green, no path of theirs in the diff) and
  nothing more. No invented O-Ton.
- Prefer a machine walk where possible: a Playwright spec against the real UI and server is the walk,
  and it repeats. It cannot judge feel, and broken-server states belong at component level.
- If the walk cannot run (no server, no browser), record that: one `## Verlauf` line per affected story
  naming what was verified instead and what a human still has to click.

Exactly three outcomes per role:

- **Signed off** — conditions walked and they hold; record it in the role's words plus anything deferred.
- **Not yet** — name what is missing, loop back to Step 3.
- **Delivered, pending a device/human check** — when the payoff is sensory or subjective (sound,
  haptics, animation feel, "is this fun"). Record what is verified, name the check the user must run,
  and carry it in the backlog until confirmed.

Honor a role's standing condition before lower-priority wishes. Repeat Steps 3–6 until every role is
signed off or resting on a named check.

## Step 7 — Close out

1. Log what each role accepted, the verified quality bar (counts), and the change list.
2. "Offene Roadmap" is a pointer to `docs/backlog/` plus at most the reasoning for the order. Every
   carry-over exists as a story.
3. Update memory only if the product's direction changed.
4. Run Step 8, then report to the user: what shipped, what is verified, what is deferred.

## Step 8 — Retrospective

Inspects the process, not the product (product problems are stories).

1. **Nachschau (mandatory).** Run `bash .claude/scripts/backlog-index.sh`; it prints
   "Nachgeschaut: X von Y abgenommenen". If the previous sprint's accepted work is not yet looked at:
   review those commits, file each escape as its own `Defekt` story with `entgangen_bei`, and set
   `nachgeschaut: <Datum>` on every story examined, including clean ones. The result is the first
   line under `## Retrospektive`.
2. Ask: what did this sprint's own gates miss, and did any finding reach us from outside the loop
   (manual review, `anmerkungen`, the user in the browser)?
3. **Verify the retro's premise against the files** (workflow, config, test) before acting on it. Do
   not reason from memory or an older protocol.
4. Land **one** change to a mechanism in this session:
   - Prefer a gate (guard test, assertion, hook) over prose.
   - Prose only for a decision, placed in the nested `CLAUDE.md` of the area or in the driving skill,
     not the root `CLAUDE.md`. Run `.claude/scripts/context-budget.sh` afterwards; if a file went
     over, remove or relocate something of comparable size in the same pass.
   - "No mechanism would have caught this" is allowed if argued.
   - If the change is a **new gate, the run stops here** so the user sees it first.
5. Record what slipped, what now covers it, and what stays uncovered.

## Session log

One file per session date, `docs/pm-sitzung-<YYYY-MM-DD>.md`; further rounds are appended.
**Exactly one `#` per file** (MD025 is linted in CI); a further round is `## Runde N — …`.

```markdown
# PM-Sitzung: <kurzer Fokus>
**Datum:** <YYYY-MM-DD>  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** <ein Satz>

## Runde 1 — Vorstellung & Feedback
### Feedback Creator (O-Ton) — Baut sich gut / Fehlt-nervt / Top-3
### Feedback Vater (O-Ton) — Gefällt / Stört-fehlt / Top-3
### Feedback Sohn (O-Ton) — Mega / Nervt / Top-3

## PM-Synthese & Priorisierung (→ Entwickler)
<Beobachtung + Backlog-Tabelle P0…P3 + roter Faden>

## Sprint N — Ziel & Umfang
**Sprint-Ziel:** <ein Satz aus einer Rollensicht, in Step 6 widerlegbar>
**Umfang:** <bis zu 6 Story-Ids, alle auf `geschaetzt`>
**Entwickler-Brief:** <Ziel · Quelle der Wahrheit · Guards · Migration · Testweg>

## Iteration N — umgesetzt
<Backend (API-First) · Frontend · Verifikation (Testcount)>

## Runde N — Re-Review / Abnahme
<Sign-off je Rolle in eigenen Worten, oder was noch fehlt; ausgefallener Rollengang ausdrücklich benannt>

## Retrospektive
**Nachschau:** <welche Stories nachgesehen · was gefunden · `nachgeschaut` gesetzt> · Index: X von Y
<Was die eigenen Tore durchgelassen haben · der EINE Mechanismus · was bewusst ungedeckt bleibt>

## Offene Roadmap (priorisiert)  ·  Konkreter Änderungsstand (für Review)
```
