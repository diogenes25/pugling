---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Nachtlauf 2026-11-17

## Auftrag und Ausgangswert

„Starte einen Nachtlauf" – unbeaufsichtigter Backlog-Lauf nach `pm-loop`, [docs/nachtlauf.md](nachtlauf.md)
(Freigaben 1–10) und [docs/backlog/README.md](backlog/README.md). Arbeit direkt auf `main` (Stand `4586e493`, 27
Commits ungepusht); der Heimnetz-Server läuft auf `:5200` aus seinem Publish-Ordner und wird nicht angefasst –
Messungen nur auf Ausweichports gegen Wegwerf-DBs. Keine weitere Sitzung schreibt im Checkout (Worktree
`backlog-lauf-1116` ist nach `main` gemergt und ruht).

**Ausgangswert:** 29 offen (Index vom 2026-11-16).

## Auswahl

Baubar nach Freigabe 8/9 bzw. autonom grillbar nach Freigabe 1:

| Id | Art | Stufe | Prio | Größe | Urteil |
| --- | --- | --- | --- | --- | --- |
| B-539 | Wunsch | `geschaetzt` | P1 | über L | teilen (Entscheidung 9), Teil B zuerst |
| B-580 | Wunsch | `geschaetzt` | P1 | L | braucht B-539 Teil A (Fach und Unit am Plan) |
| B-223, B-377 | Wunsch | `geschaetzt` | P3 | M | baubar, Thema fremd zu Sprint 83 |
| B-558, B-602, B-603, B-605 | Defekt | `idee` | P3 | — | autonom grillbar |
| B-248 | Wunsch | `idee` | P2 | — | Freigabe des Nutzers (NP-22), hängt an B-220/B-39 |
| übrige `Wunsch`/`Frage` auf `idee`/`ausformuliert` | | | | | gesperrt (Freigabe 1/8) |

Roter Faden der Nacht: **„ein aktiver Plan je Fach"** (B-539 → B-580), weil beide P1 sind und der Vater sie im
Dialog entschieden hat. Danach, wenn der Lauf noch steht, die Test-Flackerer (B-602, B-603, B-605).

## Refinement

- **B-539 geteilt** nach ihrer Entscheidung 9: Teil B „Kind-Tagessicht" ist **B-606** (`geschaetzt`, M, keine
  Migration, kein Vertragsbruch); B-539 behält AK 1–4 (L). Abweichend von README „Teilen" bleibt B-539 bestehen,
  weil Entscheidung 9 das so festlegt und B-580 auf die Id verweist.

## Sprint 83 — Ziel & Umfang

**Sprint-Ziel (Sicht Sohn):** „Auf der Startseite sehe ich alle Pflichten des Tages aus allen meinen Plänen nach
Fach, und die Tagesbox gibt es erst, wenn alle erledigt sind – mit einem Plan ändert sich für mich nichts."

**Umfang:** B-606.

**Entwickler-Brief:** Quelle der Wahrheit ist eine Kind-Tagesrechnung in `PositionProgressService` über alle
aktiven, laufenden Pläne; Box, Bonus-Sperre, Metrik-Serie und Overview-Serie lesen nur sie (Regel an der Senke).
Guards: Idempotenz-Vorprüfung der Box vor der Aggregation; Student-Endpunkt nur `[Authorize]` mit
`IsStudent`/`IsSupervisor` inline wie die Nachbarn; kein Lösungsfeld. Keine Migration. Testweg: neue
`KindTagessichtTests.cs`, Bestandstests als Gegenprobe, Vitest am Gruppier-Baustein, E2E-Rollengang.

## Iteration 83 — umgesetzt

- **Backend (API-First):** `PositionProgressService.ComputeChildDayAsync` (Kind-Tag über alle aktiven, laufenden Pläne),
  `IsCompleteDay`/`ChildStreakAsync`/`CompleteChildDaysAsync` (Serie nach Entscheidung 6); Truhe
  (`DailyBoxService.EvaluateAndAwardAsync(childId…)`), Bonus-Sperre, Missions-Metrik, `overview`- und
  `overview/progress`-Serie und Dashboard lesen nur dort. Neuer Endpunkt `GET student/children/{childId}/today`
  (Abweichung von `student/today`: Muster der Nachbarn, `ChildOwnershipFilter`). `PositionStatus` additiv um `PlanId`,
  `PlanTitle`, `SubjectName`. Keine Migration, kein Vertragsbruch.
- **Frontend:** Arcade-Startseite auf die Kind-Tagessicht, je Fach ein Abschnitt mit Überschrift, ein START über alle,
  „Los" merkt den Plan der Position; Plantitel-Zeile unter „Tagesmission" (jetzt `h1`).
- **Verifikation:** `dotnet test` 1776/1776, Vitest 1150/1150, `tsc -b` und `dotnet format` sauber; Mutanten M1–M12
  nach der letzten Änderung 12/12 rot am benannten Test (Tabelle in B-606). Reviews: 5 gezählte Funde (Grenze: mehr als
  5), alle behoben; Einzelheiten und die nicht gezählten im `## Verlauf` von B-606.
- **Commits:** `0901b4c8` (Backlog), `14fd6ae6` (B-606).

## Runde 83 — Abnahme

- **Sohn — abgenommen.** „Auf der Startseite stehen meine Sachen jetzt unter dem Fach, ‚Los' bringt mich in die richtige
  Übung, die Truhe kommt wie vorher erst nach der Pflicht." Belegt durch `sprint-83-rollengang.spec.ts`, `pfad.spec.ts`,
  `truhe.spec.ts` (3 passed, frisch gestarteter Server auf 5201/5174). Zwei Fächer nebeneinander sieht er erst mit
  B-539 Teil A; bis dahin tragen `KindTagessichtTests` und `tagessichtWeg.test.tsx` diesen Fall.
- **Vater — Regressionszeuge:** volle Playwright-Suite 166 passed (die eine rote war die neue Sohn-Spec, siehe B-606);
  Dashboard rechnet über die Kind-Sicht mit unveränderten Bestandstests; die Tagessicht seines Kindes liest er über
  denselben Endpunkt (in der Rollengang-Spec geprüft).
- **Creator — Regressionszeuge:** kein Pfad des Creators im Diff; Suite grün.

Sprint-Ziel gilt: alle Pflichten aus allen Plänen nach Fach, Truhe erst nach allen; mit einem Plan unverändert bis auf die
Fach-Überschrift und die über einen Kapitelwechsel weiterlaufende Serie (gewollt, Entscheidung 6, AK 3 nachgetragen).

## Retrospektive 83

**Nachschau:** Sprint 82 (B-597, B-599, B-598) gelesen vom `pugling-reviewer` (nur lesend). B-597: Drehung an jeder
Karten- und Wertungsstelle nachgerechnet, Obergrenze auf jedem Stufen-Weg, Rot-Probe gegen Mutante gerechnet – Funde
**B-607** (🟡, Hinweis verschwindet still auf Schreibstufen bei „Fremdwort → Deutsch") und **B-608** (🟢, Bonus-Kopf
nennt zu hohen Satz), beide `entgangen_bei: [B-597]`. B-599: Kästchen-Rückfall nur in `CardFacets`, Ablenker-Index gegen
beide Quellen, Bonus-Sätze gegen Entscheidung 7 – kein Fund. B-598: Lernkarte bucht nichts, Wiederholung höchstens
einmal, kein Weg in Test/Info/Bonus – kein Fund. `nachgeschaut` an allen dreien gesetzt. Index: **Nachgeschaut 488 von
510**.

**Was die eigenen Tore durchgelassen haben:** (1) Der Bau-Auftrag schloss E2E aus („das mache ich"), und die Startseite
änderte einen Text, auf den `pfad.spec.ts:61` wartet – gefunden hat es der `frontend-reviewer` durch Lesen, nicht ein
Lauf. (2) Meine eigene Rollengang-Spec sicherte einen Text zu, den die Zielseite nie zeigt – gefunden erst vom vollen
E2E-Lauf. Beide sind dieselbe Klasse: eine Zusicherung über sichtbaren Text, die gegen den Code nicht abgeglichen war.

**Vorschlag (CY), nicht gelandet (Freigabe 3):** ein kleines Skript `e2e-text-touch.py`, das zu einem Frontend-Diff die
aus `.tsx`-Dateien **entfernten** sichtbaren Strings bzw. `t()`-Schlüssel sammelt und die E2E-Specs nennt, die sie per
`getByText`/`name:` erwarten – im Bau-Brief als Pflichtschritt vor der Übergabe. Es hätte Fund (1) vor dem Review
gezeigt; Fund (2) fängt es nicht (neue Spec, nichts entfernt) – dafür bleibt der volle E2E-Lauf vor der Abnahme.

**Bewusst ungedeckt:** Zwei aktive Pläne im echten Browser (erst mit B-539 Teil A herstellbar).

**Halt-Prüfung:** kein neues Tor gelandet; 5 gezählte Funde (Endlosschleife erst bei mehr als 5) → der Lauf geht weiter.
