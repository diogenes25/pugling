---
tags: [typ/story, status/ausformuliert, bereich/doku, bereich/qualitaet]
aliases: [CancellationToken ohne default, 55 Signaturen, Regel statt Churn]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/api-design-bewertung.md (Geschmacksfrage C4) — Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04
grund: ""
ersetzt_durch: []
---

# B-102 · Die Token-Regel im Startkontext ist zu weit formuliert — 55 Signaturen „verstoßen" gegen eine Compilerregel

`CLAUDE.md` verlangt, dass jede async Action `CancellationToken ct = default` als letzten Parameter nimmt.
Gemessen tun das **55 von 268** Actions nicht — sie nehmen den Token ohne Vorgabewert. Die Arbeitsrunde hat
nachgemessen, dass die Regel dort, wo ihre eigene Begründung greift, schon **der Compiler** erzwingt, und dass
der Vorgabewert im Vertrag unsichtbar ist. Die billigere und richtigere Reparatur ist darum die
**Regelformulierung**, nicht 55 Signaturen.

## User Story

Als **Entwickler** möchte ich, dass eine residente Regel nur das verlangt, was sie begründen kann — damit ein
Wächter oder eine Sitzung Churn nicht eine Regel wörtlich nimmt, die weiter formuliert ist als ihr Grund.

## Ist-Stand am Code

- Die Regel steht im Startkontext (`CLAUDE.md`, Abschnitt „Konventionen", `CancellationToken`, Teil 1) mit
  dieser Begründung: *„der Vorgabewert ist nötig, weil C# keinen erforderlichen Parameter nach den optionalen
  `[FromQuery]`-Werten erlaubt"*.
- **Genau dort erzwingt ihn der Compiler** (CS1737: optionale Parameter müssen nach allen erforderlichen
  stehen). Wo keine optionalen Parameter vorangehen, greift die Begründung nicht — und die 55 Signaturen
  kompilieren.
- **Im Vertrag ist der Vorgabewert unsichtbar:** unter den 84 distinkten Parameternamen des ganzen
  OpenAPI-Dokuments kommt kein einziger vor, der den Token nennt — der ApiExplorer unterdrückt
  `CancellationToken` vollständig. Kein Effekt auf `docs/openapi/v1.json`, keiner auf das generierte
  `frontend/src/lib/contract.ts`, keiner auf `Pugling.Client` (handgeschrieben).
- Der Wächter prüft heute nur die **Anwesenheit** des Parameters (`ConventionGuardTests.cs:123`, Prüfung
  an `:148`) — nicht den Vorgabewert. Es gibt also kein rotes Tor, das die 55 anmahnt.
- Drei Actions haben gar keinen Token (`AuthController.Me`, die beiden `ExerciseTypesController`-GETs) — alle
  drei synchron, also korrekt.
- Teil 2 und 3 der Regel (Helfer **ohne** `= default`; kompensierende Schritte mit `CancellationToken.None`)
  sind unstrittig und bleiben unberührt.

## Die echte Lücke

Nicht 55 fehlende Vorgabewerte, sondern **eine Regel, die mehr verlangt als ihr Grund trägt**. Solange sie so
steht, hat jede Sitzung die Wahl zwischen „Regel verletzen" und „55 Signaturen anfassen" — und beides ist
falsch. Der einzige Rest-Wert des Vorgabewerts, den die Runde gefunden hat, ist Test-Ergonomie (eine Action
direkt aufrufen, ohne einen Token zu übergeben); das ist kein Vertragsargument und trägt keine
Sitzung Churn.

## Ergebnis der Arbeitsrunde vom 2026-08-04

1. **Die Regel schärfen, die Signaturen lassen.** Neue Formulierung für Teil 1: der Vorgabewert **nur** dort,
   wo optionale Parameter vorangehen — und dort erzwingt ihn der Compiler; sonst ist er frei. Vorschätzung
   **XS**, `wo: doku`, keine Migration, kein Vertragsbruch. Der API-Designer hat der Einordnung des
   Entwicklers zugestimmt, nachdem er den Vertrags-Effekt gemessen hatte (Ergebnis: null).
2. **Kein zusätzlicher Wächter.** `ConventionGuardTests` prüft weiter die Anwesenheit des Parameters; die
   geschärfte Zusatzregel bräuchte einen Test für etwas, das der Build schon garantiert.
3. **Nicht mit einer Aufräum-Sitzung verwechseln:** wer die 55 Signaturen trotzdem angleichen will, tut es
   als Geschmacksentscheidung — dann aber vollständig und in einem Zug, sonst sieht die verbliebene
   Uneinheitlichkeit absichtlich aus. Die Runde empfiehlt es nicht.

## Akzeptanzkriterien

1. Der Abschnitt `CancellationToken` in `CLAUDE.md` verlangt den Vorgabewert nur dort, wo optionale Parameter
   vorangehen, und nennt CS1737 als den Mechanismus, der ihn dort erzwingt.
2. Die Aussage, dass der Vorgabewert im OpenAPI-Dokument unsichtbar ist, steht als Begründung dabei — sonst
   wird die Regel beim nächsten Lesen wieder ausgeweitet.
3. Kein Test und keine Signatur ändern sich; die Suite bleibt so grün wie vorher.
4. Das Kontext-Budget (`.claude/scripts/context-budget.sh`) reißt nicht — die Änderung ist eine
   Umformulierung, keine Ergänzung.

## Verlauf

- **2026-08-04** — angelegt aus `docs/api-design-bewertung.md` (C4) und der Arbeitsrunde. Der Bericht hatte
  eine „mechanische Sitzung plus eine Zeile im Wächter" vorgeschlagen; der Entwickler widersprach, der
  API-Designer hat es nachgemessen (84 Parameternamen, kein Token-Treffer) und zugestimmt.
