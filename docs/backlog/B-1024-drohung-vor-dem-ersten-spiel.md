---
tags: [typ/story, status/idee, bereich/gamification, rolle/supervisor]
aliases: []
status: idee
prio: P2
art: Frage
unverifiziert: false
quelle: docs/pm-sitzung-2026-11-26.md#sprint-192--ziel--umfang
grund: ""
ersetzt_durch: []
---

# B-1024 · Soll ein noch nie geöffneter Plan dem Kind Frist und Malus androhen?

Fund des `pugling-reviewer` im Sprint 192 über B-1022: `PositionStatus.owesDuty` folgt der Abrechnung in allen Toren bis
auf eines – die Abrechnung beginnt erst mit dem ersten Spiel des Kindes im Plan (B-535, `SettleClosedPeriodsAsync` über
`FirstPlayDaysAsync`), die Tagessicht droht aber schon vorher „noch N Std. · −X Münzen“ an. Öffnet das Kind den Plan nie,
wird nie gebucht.

Gemessen: mit dem Erst-Spiel-Tor in `OwesDutyOn` (Sprint 192, gebaut und zurückgenommen) zeigte `frontend/e2e/pfad.spec.ts`
auf einem frisch angelegten Plan keine Frist mehr – die Arcade schweigt dann bei jedem neuen Plan, bis das Kind ihn einmal
gespielt hat. Darum ist das keine Reparatur, sondern eine Entscheidung des Vaters.

Kosten beider Antworten:

- **ja, androhen (Ist-Stand):** die Drohung ist der Grund, den Plan zu öffnen; sie ist aber für ein Kind, das den Plan
  meidet, nie wahr – und genau das ist der Ausweg, den B-535 offen gelassen hat.
- **nein, erst nach dem ersten Spiel:** die Anzeige ist ehrlich; ein neuer Plan erscheint ohne Frist, bis das Kind ihn
  einmal anfängt. Der Bau ist klein (Tor `played <= to` in `OwesDutyOn`, erste Einführung aus der Abfrage der Tagessicht,
  Tests nur als Rückfall – sonst wird `SchreibwegMessungTests` rot), `pfad.spec.ts` spielt vorher eine Karte.
- **dritter Weg:** B-535 selbst ändern – Malus ab dem ersten Pflichttag statt ab dem ersten Spiel; dann ist die Drohung
  wahr. Berührt die Begründung von B-535 („kein Stapel Abzüge zur Begrüßung“).

## Verlauf

- **2026-11-26** — angelegt im Sprint 192 (PM-Sitzung 2026-11-26); `OwesDutyOn` trägt die Ausnahme im Kommentar, die
  Vertragsdoku (`ProgressDtos.cs`) nennt sie.
