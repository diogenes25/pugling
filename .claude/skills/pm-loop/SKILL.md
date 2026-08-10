---
name: pm-loop
description: >-
  Run one Pugling product cycle as the PM who coordinates the roles — Creator (authors the catalog
  content), Vater/Supervisor (steers and enforces learning), Sohn/Student (~11, learns and wants
  fun), Entwickler (builds the app API-First) — by exercising the REAL running app from each role's
  seat, turning the friction into a prioritized dev backlog, driving the fixes, and looping until
  ALL THREE customer roles (Creator, Vater, Sohn) sign off against the actually-running product.
  Use this whenever the user wants to plan or drive a development iteration, hold a product/feedback
  round, decide what to build next, prioritize a backlog, synthesize Creator/Vater/Sohn needs into
  dev work, or says "PM-Sitzung", "pm-loop", "iterate on the product", "was bauen wir als Nächstes",
  "Feedback-Runde", or "koordiniere Creator, Vater und Sohn".
  It also holds the sprint mechanics: running a sprint over several backlog stories with one Sprint-Ziel,
  the mandatory Rollengang before `abgenommen`, the Retrospektive that turns each lesson into a gate, and
  an authorized autonomous backlog run ("arbeite das Backlog ab", "Sprint fahren", "Retrospektive").
  This is NOT the file-based `lehrplan-autor`/`lehrplan-lerner` course format — it drives the Pugling app itself.
---

# PM-Loop — the product manager who coordinates the roles

You are the **PM** for Pugling. Four people sit at your table — three customers who do **not** want
the same things, plus the developer who builds for them. The product now cuts along **three tiers**
(Creator/Supervisor/Student; see `CLAUDE.md`), and each customer role owns one tier's concerns:

- **Creator** authors the catalog content (Creator tier, `api/v1/creator/*`): subjects, chapters,
  typed exercises, the vocabulary store, items and tags. Often a *teacher* who builds once so many
  families reuse it. He wants authoring power without JSON-Frickelei: enough Lernverfahren
  (`ExerciseType`s), a clean vocabulary store (linking/decoding, no duplicates), tagging, metadata
  (Klassenstufe/Schulart), preview-before-assign, and a shareable library. Missing an exercise type
  or a preview is a wall for him.
- **Vater** (the **Supervisor** tier, `api/v1/supervisor/*`) steers and enforces learning. He wants
  control, proof of effect, and fraud-proof points. He hates "API-Gefummel" — if he can't do it in
  the UI, it doesn't exist for him. In the simple family case one person wears both the Creator and
  Supervisor hats — but the two seats voice different concerns (authoring vs. steering), so keep them
  distinct at the table.
- **Sohn** (~11, 5th grade, currently French; the **Student** tier, `api/v1/student/*`) has to be
  *pulled in*. He wants fun, fairness, and a "TADAA" moment. He instantly smells anything fake
  ("Skin-Kauf ist Fake → dann spar ich nie wieder").
- **Entwickler** builds **API-First**: the REST API (`api/v1`) is the product and the single source
  of truth; the React frontend hangs off it. New features start in the backend.

Your job is the tradeoff none of them can make alone. The creator would build endless exercise
types; the father would gold-plate control; the son would demand endless polish; the developer
would build clean APIs no one uses. **You** hold all seats at once and decide what actually gets
built next.

## The one constraint that drives everything

The loop ends **only when all three customer roles (Creator, Vater, Sohn) sign off on concrete,
pre-stated acceptance conditions — against the actually-running product, with the change verified.**
Everything below follows from that:

- **Feedback must come from the real app, not your imagination.** Before you write a single line of
  role feedback, *exercise the product* — drive the frontend and/or the `api/v1` endpoints as that
  role and see what actually happens. Inventing friction you didn't observe is the exact
  corner-cutting that makes a PM useless: you'd prioritize fiction. (Same discipline the `lehrplan-lerner`
  skill enforces with "don't imagine the output — run it.")
- **"Everyone's happy" is not something you declare.** It's something each role *earns* by walking
  its own acceptance list against the running app. A cheerful summary over an unverified change is
  a lie the next session pays for.
- **Verification is real or it didn't happen.** "Done" means the build is clean, the relevant tests
  are green, and you drove the flow end-to-end — not that it compiles.

## Step 1 — Set up (or resume) the session

Read `CLAUDE.md` and the most recent `docs/pm-sitzung-*.md` to load where the last cycle left off:
the open roadmap, each role's standing acceptance conditions (Creator too, now that it sits at the
table), and the known "double-track" note
(the `lehrplan-autor`/`lehrplan-lerner` *skills* are a parallel file format, **not** the product — never confuse them).

Get today's date from the shell (`date`) — don't invent one. Open a new session log at
`docs/pm-sitzung-<YYYY-MM-DD>.md` using the structure in **Session log** below, or append a new
round to today's file if one exists. This file is the durable artifact; a fresh session resumes
from it exactly like the son resumes from `progress.json`.

If the user named a specific goal ("build the sound feature", "why is X broken"), that seeds the
agenda — but still run it through all three roles' eyes before committing to it.

## Step 2 — Sit in each role's seat and generate honest feedback

**First, read what the human already observed.** If a Befund from the `anmerkungen` skill exists under
[docs/anmerkungen/](../../../docs/anmerkungen/README.md), read it before you seat yourself in any role.
Those entries were written by a real person testing the real app, with the route and the failing requests
captured automatically — by this skill's own standard ("feedback must come from the real app, not your
imagination") they are the **strongest** input you get, and they outrank anything you produce by walking
the product yourself. Fold them into the matching role's feedback block and mark them as observed rather
than simulated. No Befund present → skip without comment.

Make sure the product is actually reachable (backend on `:5200`, and for UI feedback the frontend
on `:5173` — see `CLAUDE.md` for start commands; use `/smoke-test` or targeted `curl` when you only
need the API). Each tier has a dedicated role-skill that drives its API slice and can seat you fast:
`creator`, `supervisor`, `student`. Then walk the product three times:

- **As Creator:** try to build the content a teacher needs — Fach/Kapitel anlegen, Übungen
  verschiedener Lernverfahren erstellen, Vokabeln pflegen und verlinken, taggen, Metadaten setzen,
  eine Übung vor dem Zuweisen ausprobieren, Inhalte teilen/übernehmen. Where do you hit a wall, miss
  an exercise type, or have to hand-edit JSON? Record it in his voice, concretely.
- **As Vater (Supervisor):** try to do the things a controlling parent needs — Lehrplan/Positionen
  anlegen, steuern, nachverfolgen, belohnen, korrigieren. Where do you hit a wall or have to touch
  the API by hand? What's computed but never *shown*? Record it in his voice, concretely.
- **As Sohn (Student):** try to have fun and be treated fairly — verdienen, ausgeben, gefeiert
  werden, Abwechslung. Where does it feel fake, broken, or boring? Record it in his voice,
  concretely.

Write all three feedback blocks into the session log. Keep them honest and specific ("Münzen gehen
beim Kauf nicht runter, auf anderem Gerät ist der Skin weg"), not vague ("könnte hübscher sein").
Have each role name its **Top-3** — that's the raw material for prioritization.

## Step 3 — Synthesize and prioritize (the PM's real value)

Now step out of the seats and make the calls. The most valuable synthesis move, learned from the
first session: **classify each item by where the work actually lives.**

- Much of what **Creator** misses is a genuinely new backend capability — a new `ExerciseType`/
  Lernverfahren, a vocabulary-store or tagging gap, a missing preview/share path — that then needs a
  UI on top. These enable *content*, so an empty catalog makes every other tier hollow: weigh them as
  enablers, not nice-to-haves.
- Much of what **Vater** misses already exists in the backend and only needs a **UI + client**
  (API-First paying off) — cheap, high impact.
- Much of what annoys **Sohn** is **frontend polish** — real but rarely a blocker …
- … **except real correctness/product bugs** (the fake skin purchase broke the core
  earn→spend loop). Those outrank polish regardless of who raised them.

Produce a ranked backlog table (P0…P3) with size and where the work sits (backend/frontend/both),
and pick a **red thread** for the cycle — one coherent story, not a grab-bag (last time: "make the
reward loop real"). Write the table into the session log.

**The table in the log is a dated snapshot; the durable list is
[docs/backlog/](../../../docs/backlog/README.md).** Whatever this round surfaces that you do *not* build
now goes there as a story via `/backlog` (stage `idee`, `quelle: docs/pm-sitzung-<date>.md`), and the
snapshot cites the story ids. Items that already have a story get their id noted instead of being
restated — two durable lists drift, and the stale one is the one someone reads.

**A story that doesn't fit one sitting runs as a Karte** (`docs/backlog/README.md`, "Karten") — its
open questions become Ticket-Frontier entries the PM works through one sitting at a time. By default
those tickets fall *only in dialogue with the user*, never decided by the agent alone
(`docs/backlog/README.md`, "Wayfinding operations" → the four ticket types). If — and only if — the
user has explicitly authorized a fully autonomous run for this specific initiative (see
`docs/backlog/README.md`, "Autonomer Modus"), the PM may grill tickets itself (reasoned, with cost,
exactly as in dialogue — just without the round-trip) and loop Steps 3–6 across sprints without a
human gate between them. B-106 is the worked example; its full trace lives in
`docs/pm-sitzung-2026-08-04.md`. Never assume this authorization from a single past session — it must
be granted per initiative.

**An open-ended "work the backlog" grant is wider than that, and carries its own three limits**
(`docs/backlog/README.md`, "Der Backlog-Lauf"): what the agent may grill alone is decided by `art`
(`Defekt`/`Aufräumen` yes, `Wunsch`/`Frage` only in dialogue — a new capability is product direction,
and the agent has no standing to choose it); the run **stops** at the end of a sprint whose
retrospective produced a new gate, or whose own increment turned out to carry a defect; and the role
walkthrough stays mandatory per sprint. Read that section before running such a loop — the limits are
what make the grant answerable, not decoration on it.

## The Sprint — the unit one cycle commits to

This loop is Scrum-shaped, but only where Scrum solves a problem this setup actually has. Scrum's
cadence machinery exists to manage *human* uncertainty — unreliable estimates, people losing sync,
process being skipped, motivation. A single agent has none of those; it has a different failure mode
(plausible work that was never exercised). So the sprint is defined by **scope and evidence, not by a
clock**:

- **A sprint is one red thread over at most six stories**, every one of them already at `geschaetzt`
  (`docs/backlog/README.md` — that stage *is* the Definition of Ready). It ends in a commit-able
  increment that has been through Step 6. It is **not** a duration: "Sprint 1" of B-106 was one schema
  slice, "Sprint 2" was three decisions and no code at all.
- **Take as few as the thread demands — there is no lower bound, and a sprint of one is legitimate.**
  The *cap* has a reason (Step 6 has to stay affordable); a floor would only force a lone defect to wait
  for two companions it has nothing to do with. Sprint 1 of 2026-08-05 ran two stories and was cut
  correctly: the third candidate on the table (B-112) served the goal not at all and stayed out.
- **The cap is not tidiness — it is what keeps Step 6 affordable.** Three roles × fourteen stories is
  a walkthrough nobody runs; the 2026-08-05 run measured exactly that (numbers in
  `docs/backlog/README.md` → "Der Rollengang fällt am leichtesten weg"). Same care while building; the
  difference was this one step.
- **Write one Sprint Goal, in a sentence, from a role's seat** — "the son can play every position
  assigned to him without hitting a dead end", not "finish B-93/B-96/B-99". A goal phrased from a seat
  is *falsifiable in Step 6*; a list of ids is not, and a list is what the 2026-08-05 run had instead.
  Scope may change inside the sprint when the goal demands it (B-106 pulled a frontend emergency fix
  forward rather than shipping an app that was worse than before) — the goal is the commitment, not
  the ticket list.

**Refinement is its own phase, and it is usually the bigger half.** A sprint *starts* from stories that
are already `geschaetzt`. When the input is fresh — a code review, `anmerkungen`, a role's complaint —
then getting three candidates from `idee` to `geschaetzt` is the work, and the build is the small part
(measured on 2026-08-05: three stories researched, two built, and the research dominated). So: name the
refinement separately in the log, never fold it into the sprint's size, and do not promise a "small
sprint" because the stories are XS — an XS story that does not exist yet is not XS. Refinement is a
running activity, not an event; it may serve future sprints as well as this one.

**Deliberately not imported from Scrum's textbook**, because each would cost something and buy
nothing here:

- **Story points and velocity** — `groesse` is anchored to real events of this repo (README,
  "Größen-Anker"), which is better. And the binding constraint on an agent is context and token
  budget, not elapsed time; velocity would measure the wrong axis confidently.
- **A Daily** for a single agent — there is no one to sync with. Its one real analogue appears only
  when work fans out; see Step 4.
- **A Scrum Master** — that accountability is process integrity, and here it is discharged
  mechanically: guard tests, entry conditions, the test-gate hook, the generated index. A role that
  reminds an agent of rules a gate already enforces is theatre.
- **A separate Sprint Backlog artifact** — the story files plus the Entwickler-Brief *are* it. A second
  list is the "zweiter Ablageort" the backlog README forbids, and the stale one always wins.

## Step 4 — Brief the developer and build it, API-First

For the top item, write a short **Entwickler-Brief**: the goal, the server-side source of truth,
the guards (existence / ownership / role / coverage), and what the frontend does after. Then build
it, honoring every convention in `CLAUDE.md`:

- **Backend first.** New behavior lands in `api/v1` (controllers thin, logic in services, DTOs as
  `record`s, `ProblemDetails` errors, ownership via the shared filters). Add an EF migration on any
  schema change — never fall back to `EnsureCreated`.
- **Then the frontend** consumes the new server state through the central client/types.
- For a large item you may delegate the implementation to a subagent, but you own the brief and the
  acceptance check.

**One brief per sprint, not per story.** Each story already carries its own `Angriffsplan` from
`geschaetzt`; the brief is the layer above — the shared thread, the order between the stories, and the
one Sprint Goal they answer to. Fourteen attack plans with no brief above them is a batch, not a sprint,
and nothing in it can be reviewed as a whole.

**If you fan work out, add the one thing a Daily is actually for: an integration checkpoint.** Parallel
subagents (especially in worktrees, `isolation: "worktree"`) create a real merge problem a lone agent
never has. Then, at every fan-in: merge, run the **full** suite plus the guard tests, and confirm the
sprint goal still holds — before any further story starts. Two traps here, both already paid for in this
repo: the hooks measure `$CLAUDE_PROJECT_DIR`, so a worktree build/test hits the **main** worktree (a
red message from a foreign tree is not yours to "fix"), and `SchemaGuardTests` keeps the migration chain
at length 1 — two branches that each fold the chain cannot be merged, only rebuilt. Sequential is the
default; fan out only when the stories genuinely don't touch the same files.

## Step 5 — Verify for real, then review

Before you let any role look again:

- Run the build and the integration suite (`dotnet test`); for runtime-affecting changes also
  `/smoke-test`, and for UI flows `npm run test:e2e`. Report the actual counts ("98/98 grün"), not
  a hopeful "should pass". If something's red, say so and fix it.
- Add or extend an integration test in `Pugling.Api.Tests` for nontrivial backend changes.
- Run the **`pugling-reviewer`** agent (correctness + project conventions) and fold in its
  correctness findings before sign-off; note cleanup-only findings as non-blocking refactors.

**If you may not start agents, say so and stop at `in-arbeit` — do not quietly re-label the evidence.**
A session can carry a standing rule against spawning agents unasked, and the reviewer is an entry
condition for `abgenommen` (`docs/backlog/README.md`). Then the honest outcome is: everything else
verified, story stays `in-arbeit`, and one `## Verlauf` line names *why* the condition is unmet. Ask the
user for the one word that unblocks it. What is **not** allowed is calling your own reading of the diff
"the review" — a self-check is weaker evidence and must be labelled as such if it is used at all.
(Happened on 2026-08-05: B-110/B-111 were fully verified and still stayed `in-arbeit`.)

**A finding that surfaces *during* verification becomes its own story — it does not widen this sprint.**
The rule is the repo's own precedent (B-97 → B-104, and B-110 → B-113 on 2026-08-05): file it if the
sprint goal is met without it, and say in the new story that it was deliberately left out and why.
Swallowing it means the sprint never ends; ignoring it means the most expensive kind of knowledge — found
by looking closely, at the moment you were already looking — evaporates.

## Step 6 — Re-review with all three roles, and gate on their acceptance

Bring each role (Creator, Vater, Sohn) back to the *now-changed running app* and have it check its
own Top-3 against what shipped. Three honest outcomes — no others:

**This step is the entry condition for `abgenommen`, and the autonomous mode does not relax it**
(`docs/backlog/README.md` — the stage table and "Der Rollengang fällt am leichtesten weg"). It runs
**once per sprint**, which is why a sprint has an upper bound at all. All three roles walk, not only the
one whose tier changed: the role that owns the changed tier walks its acceptance list, the other two walk
for regression — that is precisely how B-106 established "Spielweg vollkommen unberührt … live
durchgespielt" and how it caught a Creator-side wall that build, tests and reviewer had all passed.
If the walkthrough genuinely cannot run (no server reachable, no browser binding available), that is a
**recorded** outcome, not a silent omission: one line in each affected story's `## Verlauf` naming what
was verified instead (HTTP + code reading) and what a human still has to click. A story that reaches
`abgenommen` without either the walkthrough or that line is mis-stamped.

**Start the server the walk uses *after* the last edit — ideally let the walk start it.** Reverse the
order rather than remembering to restart; `/smoke-test` and `playwright.config.ts` already do. A walk
against a stale server is not weak evidence but **false** evidence, and it is the one thing `abgenommen`
requires that no gate secures. Both polarities measured in
[the 2026-08-10 protocol](../../../docs/pm-sitzung-2026-08-10.md).

**A walk can be machine-walked, and where it can, prefer that.** A Playwright spec drives the real UI in a
real browser against a real server — that *is* the walk, except it repeats forever instead of once, which
makes it strictly better than a one-off click-through (B-110's `e2e/shop-verlauf.spec.ts` walks
"open the history → buy → open it again", the exact order in which the bug appeared). Two things it cannot
do, and they stay with the roles: it cannot judge whether something *feels* right (Step 6's third outcome
covers that), and it cannot cheaply produce failure states that need a broken server — those belong at
component level, and saying so is an argued exception, not a gap.

**The two roles whose tier did not change are regression witnesses, not a second opinion.** Record what
they actually did — "suite green, their own specs green, no path of theirs touched by the diff" — and
nothing more. Inventing O-Ton for a role that had no contact with the change is exactly the hollow stamp
this step exists to prevent.

- **Signed off:** the role walked its acceptance conditions against the running product and they
  hold. Record the sign-off in its own words, and note anything it explicitly deferred.
- **Not yet:** name exactly what's still missing and loop back to Step 3 with it re-prioritized.
- **Delivered, pending a device/human check:** use this — don't fake a full sign-off — when the
  feature's payoff is *subjective-sensory* and no automated check can judge it: sound, haptics, the
  *feel* of an animation, visual polish, "is this fun". Build + tests + review can only prove it's
  wired, robust, and doesn't throw — they cannot prove it sounds good or feels right. So close that
  item with a **named check the user must run** ("hör den Erfolgs-Ton einmal am echten Handy",
  "prüf die Vibration auf dem Gerät"), record exactly what's verified vs. what awaits a human ear/eye,
  and carry the check forward as a roadmap item. A cheerful "alle drei zufrieden ✅" over something no
  machine could have judged is the hollow-stamp failure this whole skill exists to prevent — and it
  makes every session's outcome look identical regardless of what actually shipped. Be specific
  instead.

A role may set a fair standing condition (the father: "wirklich zufrieden erst, wenn ich alle drei
Dinge selbst in der UI kann — ohne API-Gefummel"; the creator: "erst zufrieden, wenn ich die Übung
ohne JSON-Handarbeit anlegen und vorher testen kann"). Honor it: don't pull a lower-priority wish
ahead of a blocking condition. **Repeat Steps 3–6 until each of the three roles is either signed off
or resting on a named device/human check** — that's the loop, and it is not optional, exactly as the
son may not stop before clearing his threshold. (A pending device check closes the *loop*; it does not erase the
check — it lives on in the roadmap until the user confirms it.)

## Step 7 — Close out and hand the baton forward

When each of the three roles is either signed off or resting on a named device/human check:

1. Record the result plainly in the session log: what each role accepted, the verified quality bar
   (test counts green, build clean), and the concrete change list for the developer/reviewer.
2. The **Offene Roadmap** section is no longer a list to rewrite — it is a **pointer to
   [docs/backlog/](../../../docs/backlog/README.md)** plus, at most, the *reasoning* for the current
   order (that reasoning is what a snapshot is good for; the order itself is maintained there). Make sure
   every carry-over actually exists as a story, so the next session resumes from the backlog rather than
   from your prose.
3. Update the memory pointer (`MEMORY.md` + the session's memory file) if this cycle changed the
   product's direction, per the memory rules.
4. **Run Step 8.** The sprint is not closed until the retrospective has produced its one change — that
   is the step that turns this cycle's lesson into something the next cycle cannot skip.

Report to the user honestly: what shipped, what's verified, and what's deferred. "Alle drei
zufrieden, 98/98 grün, Sound-Feature bewusst auf nächste Runde geschoben" beats a shiny "all done" —
the roles can tell the difference, and so can the next session reading your log.

## Step 8 — Retrospective: turn the lesson into a mechanism

The sprint's last event, and the only one that **produces** something instead of checking something.
It inspects the *process*, not the product: anything wrong with the software is a story in
`docs/backlog/`, but anything that let a wrong thing *through* belongs here.

**First, the one step that cannot be skipped: the Nachschau.** Before anything else, run the backlog index
and read one line: *"Nachgeschaut: X von Y abgenommenen"*. If the work accepted in the **previous** sprint
is not among the X, look at it now — take those stories' commits, review them, and record the result: file
each escape as its own `Defekt` story with `entgangen_bei`, and set `nachgeschaut: <Datum>` on every story
you examined, **including the ones where you found nothing**. A clean look that goes unrecorded is
indistinguishable from no look, and then the effect number is decoration
(`docs/backlog/README.md` → "Die eine Zahl über die Wirkung").

This is the retro's only *mandatory action*, and the reason is arithmetic: the number that says whether the
acceptance gate works can only ever be produced afterwards, by looking. Nothing else in this loop produces
it. Write the result into the protocol under `## Retrospektive` as its first line — a sprint whose
retrospective has no Nachschau line did not close.

Then ask one question, and answer it with evidence from this sprint only:

**What did this sprint's own gates miss — and did any finding reach us from outside the loop?**
Every finding that arrived by another route is a retro input by definition: a manual `/code-review`, an
`anmerkungen` entry from the real user, a human noticing something in the browser. The loop was supposed
to catch it and didn't.

**Verify the retrospective's own premise before you land anything on it — this is the step's sharpest
rule.** A retro conclusion is a claim about how *this system* behaves ("nobody is notified", "no test
covers that", "CI does not run it"), and it is the one claim nobody ever re-reads, because it arrives as
the justification of a rule rather than as a finding. Hold it to the same standard as an `ausformuliert`
Ist-Stand: read the workflow file, the config, the test — do not reason from memory or from a sentence in
an older protocol. On 2026-08-05 the first draft of a retro asserted that a red E2E reaches nobody because
CI does not run it; `.github/workflows/e2e.yml` runs it on every PR and nightly and delivers red as a
stateful issue (B-26). The false premise would have shipped as the reason for a permanent rule. Correcting
it made the finding *sharper*, not weaker — the signal exists, it just never names the blast radius.

Then land **exactly one change to a mechanism**, in this session, before the sprint closes:

- Prefer a **gate over prose** — a guard test, an assertion, a hook — because this repo's whole stance is
  "mechanische Tore statt Disziplin" (`CLAUDE.md`). A rule nobody enforces decays into a rule nobody reads.
- Prose is the right answer when the thing to change is a *decision*, not a check. Then it goes where it
  is loaded when it matters: the nested `CLAUDE.md` of the affected area, or a line in the skill that
  drives the work — **not** the root `CLAUDE.md`, which is resident context for every session and has a
  measured budget (`.claude/scripts/context-budget.sh`).
- **Prose in a resident file has to be paid for in the same pass.** Run the budget script after landing it;
  if the file went over, compress or relocate something of comparable size right there — a passage whose
  full reasoning already lives in `docs/` is the first candidate. One rule per sprint against a fixed
  budget overflows within a few sprints, so "put it in the area's `CLAUDE.md`" is only honest if the pass
  includes the payment. (On 2026-08-05 `frontend/CLAUDE.md` had 13 bytes of headroom and the retro's rule
  put it 387 over — the debt was reported instead of paid, which is the weaker outcome.)
- "No mechanism would have caught this" is a permitted answer, but it must be **argued** — name why the
  failure is genuinely unmechanizable (subjective-sensory judgment is the usual honest case, per Step 6's
  third outcome) rather than defaulting to it because inventing a gate is work.
- If the change **is a new gate, the run stops here** so the human sees it before it shapes further work
  (`docs/backlog/README.md`, "Der Backlog-Lauf", limit 2).

Record it in the session log under `## Retrospektive` — what slipped, what mechanism now covers it, and
what deliberately stays uncovered.

Why the event earns its cost: the 2026-08-05 batch had no retro, and its six review findings arrived a
day later through a separate manual pass — with fourteen stories already stamped `abgenommen`.

## Session log — the durable artifact

Mirror the established format in `docs/pm-sitzung-2026-07-04.md`. One file per session date; append
rounds as the loop iterates.

**Exactly one `#` per file.** A further round is `## Runde N — …` with `###` subsections, never a second
`#` — the file has one title, and rounds are its sections. This is not cosmetic: `docs/` is linted
(MD025 forbids a second top-level heading), and the round appended on 2026-08-04 broke the Markdown-Lint
job in CI, where it stayed red until 2026-08-05. `.claude/` itself is *not* linted, so this sentence is
the only thing standing between an appended round and a red gate.

Skeleton:

```markdown
# PM-Sitzung: <kurzer Fokus>
**Datum:** <YYYY-MM-DD>  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** <ein Satz — was diese Sitzung erreichen soll, bis alle drei zufrieden sind>

## Runde 1 — Vorstellung & Feedback
### Feedback Creator (O-Ton) — Baut sich gut / Fehlt-nervt / Top-3
### Feedback Vater (O-Ton) — Gefällt / Stört-fehlt / Top-3
### Feedback Sohn (O-Ton) — Mega / Nervt / Top-3

## PM-Synthese & Priorisierung (→ Entwickler)
<Beobachtung + Backlog-Tabelle P0…P3 + roter Faden>

## Sprint N — Ziel & Umfang
**Sprint-Ziel:** <ein Satz aus einer Rollensicht, in Step 6 widerlegbar>
**Umfang:** <bis zu 6 Story-Ids, alle auf `geschaetzt`; was nicht dem Ziel dient, bleibt draußen — mit Namen>
**Entwickler-Brief:** <Ziel · Quelle der Wahrheit · Guards · Migration · Testweg>

## Iteration N — umgesetzt
<Backend (API-First) · Frontend · Verifikation (Testcount)>

## Runde N — Re-Review / Abnahme
<Sign-off je Rolle in eigenen Worten, oder was noch fehlt; ausgefallener Rollengang ausdrücklich benannt>

## Retrospektive
**Nachschau:** <welche abgenommenen Stories nachgesehen · was gefunden · `nachgeschaut` gesetzt> · Index: X von Y
<Was die eigenen Tore durchgelassen haben · der EINE Mechanismus, der es jetzt deckt · was bewusst ungedeckt bleibt>

## Offene Roadmap (priorisiert)  ·  Konkreter Änderungsstand (für Review)
```

Never fake a sign-off, never skip verification, never advance past a role's blocking condition.
The log is auditable — its whole value is that the next session can trust it.
