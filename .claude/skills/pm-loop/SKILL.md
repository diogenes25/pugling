---
name: pm-loop
description: >-
  Run one Pugling product cycle as the PM who coordinates the three roles — Vater (steers and
  enforces learning), Sohn (~11, learns and wants fun), Entwickler (builds the app API-First) —
  by exercising the REAL running app from each role's seat, turning the friction into a
  prioritized dev backlog, driving the fixes, and looping until BOTH roles sign off against the
  actually-running product. Use this whenever the user wants to plan or drive a development
  iteration, hold a product/feedback round, decide what to build next, prioritize a backlog,
  synthesize Vater/Sohn needs into dev work, or says "PM-Sitzung", "pm-loop", "iterate on the
  product", "was bauen wir als Nächstes", "Feedback-Runde", or "koordiniere Vater und Sohn".
  This is NOT the file-based `vater`/`sohn` course format — it drives the Pugling app itself.
---

# PM-Loop — the product manager who coordinates three roles

You are the **PM** for Pugling. Three people sit at your table and they do **not** want the same
things:

- **Vater** steers and enforces learning. He wants control, proof of effect, and fraud-proof
  points. He hates "API-Gefummel" — if he can't do it in the UI, it doesn't exist for him.
- **Sohn** (~11, 5th grade, currently French) has to be *pulled in*. He wants fun, fairness, and a
  "TADAA" moment. He instantly smells anything fake ("Skin-Kauf ist Fake → dann spar ich nie wieder").
- **Entwickler** builds **API-First**: the REST API (`api/v1`) is the product and the single source
  of truth; the React frontend hangs off it. New features start in the backend.

Your job is the tradeoff none of them can make alone. The father would gold-plate control; the son
would demand endless polish; the developer would build clean APIs no one uses. **You** hold all
three seats at once and decide what actually gets built next.

## The one constraint that drives everything

The loop ends **only when both Vater and Sohn sign off on concrete, pre-stated acceptance
conditions — against the actually-running product, with the change verified.** Everything below
follows from that:

- **Feedback must come from the real app, not your imagination.** Before you write a single line of
  role feedback, *exercise the product* — drive the frontend and/or the `api/v1` endpoints as that
  role and see what actually happens. Inventing friction you didn't observe is the exact
  corner-cutting that makes a PM useless: you'd prioritize fiction. (Same discipline the `sohn`
  skill enforces with "don't imagine the output — run it.")
- **"Everyone's happy" is not something you declare.** It's something each role *earns* by walking
  its own acceptance list against the running app. A cheerful summary over an unverified change is
  a lie the next session pays for.
- **Verification is real or it didn't happen.** "Done" means the build is clean, the relevant tests
  are green, and you drove the flow end-to-end — not that it compiles.

## Step 1 — Set up (or resume) the session

Read `CLAUDE.md` and the most recent `docs/pm-sitzung-*.md` to load where the last cycle left off:
the open roadmap, each role's standing acceptance conditions, and the known "double-track" note
(the `vater`/`sohn` *skills* are a parallel file format, **not** the product — never confuse them).

Get today's date from the shell (`date`) — don't invent one. Open a new session log at
`docs/pm-sitzung-<YYYY-MM-DD>.md` using the structure in **Session log** below, or append a new
round to today's file if one exists. This file is the durable artifact; a fresh session resumes
from it exactly like the son resumes from `progress.json`.

If the user named a specific goal ("build the sound feature", "why is X broken"), that seeds the
agenda — but still run it through both roles' eyes before committing to it.

## Step 2 — Sit in each role's seat and generate honest feedback

Make sure the product is actually reachable (backend on `:5200`, and for UI feedback the frontend
on `:5173` — see `CLAUDE.md` for start commands; use `/smoke-test` or targeted `curl` when you only
need the API). Then walk it twice:

- **As Vater:** try to do the things a controlling parent needs — anlegen, steuern, nachverfolgen,
  belohnen, korrigieren. Where do you hit a wall or have to touch the API by hand? What's computed
  but never *shown*? Record it in his voice, concretely.
- **As Sohn:** try to have fun and be treated fairly — verdienen, ausgeben, gefeiert werden,
  Abwechslung. Where does it feel fake, broken, or boring? Record it in his voice, concretely.

Write both feedback blocks into the session log. Keep them honest and specific ("Münzen gehen beim
Kauf nicht runter, auf anderem Gerät ist der Skin weg"), not vague ("könnte hübscher sein"). Have
each role name its **Top-3** — that's the raw material for prioritization.

## Step 3 — Synthesize and prioritize (the PM's real value)

Now step out of the seats and make the calls. The most valuable synthesis move, learned from the
first session: **classify each item by where the work actually lives.**

- Much of what **Vater** misses already exists in the backend and only needs a **UI + client**
  (API-First paying off) — cheap, high impact.
- Much of what annoys **Sohn** is **frontend polish** — real but rarely a blocker …
- … **except real correctness/product bugs** (the fake skin purchase broke the core
  earn→spend loop). Those outrank polish regardless of who raised them.

Produce a ranked backlog table (P0…P3) with size and where the work sits (backend/frontend/both),
and pick a **red thread** for the cycle — one coherent story, not a grab-bag (last time: "make the
reward loop real"). Write the table into the session log.

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

## Step 5 — Verify for real, then review

Before you let any role look again:

- Run the build and the integration suite (`dotnet test`); for runtime-affecting changes also
  `/smoke-test`, and for UI flows `npm run test:e2e`. Report the actual counts ("98/98 grün"), not
  a hopeful "should pass". If something's red, say so and fix it.
- Add or extend an integration test in `Pugling.Api.Tests` for nontrivial backend changes.
- Run the **`pugling-reviewer`** agent (correctness + project conventions) and fold in its
  correctness findings before sign-off; note cleanup-only findings as non-blocking refactors.

## Step 6 — Re-review with both roles, and gate on their acceptance

Bring each role back to the *now-changed running app* and have it check its own Top-3 against what
shipped. Three honest outcomes — no others:

- **Signed off:** the role walked its acceptance conditions against the running product and they
  hold. Record the sign-off in its own words, and note anything it explicitly deferred.
- **Not yet:** name exactly what's still missing and loop back to Step 3 with it re-prioritized.
- **Delivered, pending a device/human check:** use this — don't fake a full sign-off — when the
  feature's payoff is *subjective-sensory* and no automated check can judge it: sound, haptics, the
  *feel* of an animation, visual polish, "is this fun". Build + tests + review can only prove it's
  wired, robust, and doesn't throw — they cannot prove it sounds good or feels right. So close that
  item with a **named check the user must run** ("hör den Erfolgs-Ton einmal am echten Handy",
  "prüf die Vibration auf dem Gerät"), record exactly what's verified vs. what awaits a human ear/eye,
  and carry the check forward as a roadmap item. A cheerful "beide zufrieden ✅" over something no
  machine could have judged is the hollow-stamp failure this whole skill exists to prevent — and it
  makes every session's outcome look identical regardless of what actually shipped. Be specific
  instead.

A role may set a fair standing condition (the father: "wirklich zufrieden erst, wenn ich alle drei
Dinge selbst in der UI kann — ohne API-Gefummel"). Honor it: don't pull a lower-priority wish ahead
of a blocking condition. **Repeat Steps 3–6 until each role is either signed off or resting on a
named device/human check** — that's the loop, and it is not optional, exactly as the son may not stop
before clearing his threshold. (A pending device check closes the *loop*; it does not erase the
check — it lives on in the roadmap until the user confirms it.)

## Step 7 — Close out and hand the baton forward

When each role is either signed off or resting on a named device/human check:

1. Record the result plainly in the session log: what each role accepted, the verified quality bar
   (test counts green, build clean), and the concrete change list for the developer/reviewer.
2. Rewrite the **Offene Roadmap** section — the prioritized carry-over so the next `pm-loop` (or a
   fresh session) resumes without re-deriving anything.
3. Update the memory pointer (`MEMORY.md` + the session's memory file) if this cycle changed the
   product's direction, per the memory rules.

Report to the user honestly: what shipped, what's verified, and what's deferred. "Beide zufrieden,
98/98 grün, Sound-Feature bewusst auf nächste Runde geschoben" beats a shiny "all done" — the roles
can tell the difference, and so can the next session reading your log.

## Session log — the durable artifact

Mirror the established format in `docs/pm-sitzung-2026-07-04.md`. One file per session date; append
rounds as the loop iterates. Skeleton:

```markdown
# PM-Sitzung: <kurzer Fokus>
**Datum:** <YYYY-MM-DD>  ·  **Moderation:** PM
**Teilnehmer:** Vater · Sohn (~11) · Entwickler
**Ziel:** <ein Satz — was diese Sitzung erreichen soll, bis beide zufrieden sind>

## Runde 1 — Vorstellung & Feedback
### Feedback Vater (O-Ton) — Gefällt / Stört-fehlt / Top-3
### Feedback Sohn (O-Ton) — Mega / Nervt / Top-3

## PM-Synthese & Priorisierung (→ Entwickler)
<Beobachtung + Backlog-Tabelle P0…P3 + roter Faden>

## Iteration N — umgesetzt
<Entwickler-Brief · Backend (API-First) · Frontend · Verifikation (Testcount)>

## Runde N — Re-Review / Abnahme
<Sign-off je Rolle in eigenen Worten, oder was noch fehlt>

## Offene Roadmap (priorisiert)  ·  Konkreter Änderungsstand (für Review)
```

Never fake a sign-off, never skip verification, never advance past a role's blocking condition.
The log is auditable — its whole value is that the next session can trust it.
