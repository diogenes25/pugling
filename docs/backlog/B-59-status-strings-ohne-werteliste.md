---
tags: [typ/story, status/idee, bereich/backend, bereich/qualitaet]
aliases: [Status als String, GoalStatus ohne Werteliste]
status: idee
prio: P3
art: Aufräumen
quelle: docs/testabdeckung-plan.md
unverifiziert: true
---

# B-59 · Zwei Antwortfelder tragen einen Status als nackten `string`

Gefunden beim Bauen von [B-42](B-42-openapi-typen-generieren.md) Schritt 2 (E6): als die Vertragstypen des
Frontends aus dem Dokument kamen, blieben genau zwei Felder ohne Werteliste übrig.

## Ist-Stand am Code

- `KeyResultResponse.Status` und `ObjectiveResponse.Status`
  ([GoalDtos.cs](../../backend/Pugling.Contracts/Supervisor/GoalDtos.cs)) sind `string`. Es gibt nur drei
  Werte – `open`/`achieved`/`overdue`, gerechnet in `ObjectiveEvaluationService` –, aber kein Enum.
- `KeyResultResponse.Scope` ist ebenso `string` mit drei Werten
  (`"exercise"`/`"chapter"`/`"subject"`, `ObjectiveService.KrScope`).
- Das Frontend hat sich in E6 damit abgefunden: `GoalStatus` bleibt als Hand-Typ in
  [uiTypes.ts](../../frontend/src/lib/uiTypes.ts) und gilt jetzt ausdrücklich nur als Whitelist des
  **Filters** (`api.objectives({ status })`), nicht als Zusage über die Antwort. `StatusPill` nimmt darum
  `string` und fällt bei Unbekanntem auf „offen" zurück.
- **Das Gegenstück ist im selben Durchgang schon repariert**: `Metric` und `Kind` waren aus demselben Grund
  `string` (`.ToString()` in der Projektion) und tragen jetzt ihre Enums – wire-identisch, aber mit
  Werteliste im Dokument.

## Die echte Lücke

Klein und ehrlich benannt: eine Oberfläche kann über diese beiden Felder nicht vollständig
fallunterscheiden, und ein generierter Client bekommt `string`. Ein Tippfehler im Vergleich
(`"acheived"`) fällt niemandem auf.

## Warum es nicht in E6 mitgemacht wurde

Weil es **anders als bei `Metric`/`Kind` ein Vertragsbruch wäre.** Die Werte sind kleingeschrieben; ein
C#-Enum `GoalStatus { Open, Achieved, Overdue }` liefert über den `JsonStringEnumConverter`
`"Open"`/`"Achieved"`/`"Overdue"`. Das ändert die Leitung, bricht das Frontend an jedem Vergleich und die
E2E dazu. Die Reparatur braucht also eine Entscheidung (Enum + `JsonStringEnumConverter`-Namenspolitik, oder
`[JsonStringEnumMemberName]` je Wert), keine Zeile.

## Offene Punkte

1. Kleinschreibung beibehalten (`[JsonStringEnumMemberName("open")]`) oder auf die Hausform
   `Open`/`Achieved`/`Overdue` gehen und Frontend + E2E nachziehen? Ersteres ist unsichtbar, letzteres
   konsistent mit allen anderen Enums des Vertrags.
2. Gilt dasselbe für `Scope`? Dort ist die Kleinschreibung noch weniger begründet, weil der Wert nie
   angezeigt wird – er steuert nur die Anzeige-Logik.
3. Gibt es weitere `string`-Felder mit fester Werteliste? Die Suche ist billig, seit alle Antworten im
   Dokument stehen (E6 hat 167 fehlende Erfolgsantworten nachgetragen).

## Verlauf

- **2026-08-01** — angelegt beim Bauen von E6. Der Befund ist am Code belegt; unverifiziert ist nur, wie
  viele weitere Felder dieselbe Form haben (offener Punkt 3).
