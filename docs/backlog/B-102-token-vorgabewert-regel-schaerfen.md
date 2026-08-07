---
tags: [typ/story, status/abgenommen, bereich/doku, bereich/qualitaet]
aliases: [CancellationToken ohne default, 55 Signaturen, Regel statt Churn]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: docs/api-design-bewertung.md (Geschmacksfrage C4) — Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-08-07"
wartet_auf: ""
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

## Entscheidungen

1. **Die Regel schärfen, keine Signatur anfassen.** Teil 1 der `CancellationToken`-Regel in `CLAUDE.md`
   verlangt den Vorgabewert künftig nur noch dort, wo optionale `[FromQuery]`-Parameter vorangehen — und
   nennt CS1737 als den Compiler-Mechanismus, der ihn genau dort erzwingt. Begründung: 55 von 268 Actions
   nehmen den Token heute ohne `= default` und kompilieren trotzdem, weil keine optionalen Parameter
   vorangehen; der Vorgabewert ist unter den 84 distinkten Parameternamen des OpenAPI-Dokuments kein
   einziges Mal sichtbar (`ApiExplorer` unterdrückt `CancellationToken` vollständig) — die einzige
   Begründung, die die Regel selbst nennt, trägt dort nicht. Kosten: keine — die 55 Signaturen bleiben wie
   sie sind, das ist der ganze Punkt der Story.
2. **Kein neuer Wächter.** `ConventionGuardTests.Async_Actions_Nehmen_Einen_CancellationToken` prüft weiter
   nur die Anwesenheit des Parameters. Begründung: eine Zusatzregel für den Vorgabewert bräuchte einen Test
   für etwas, das der Compiler an der einzig begründeten Stelle schon garantiert (CS1737) — ein Wächter ohne
   eigenen Fund ist Dekoration. Kosten: keine.
3. **55 Signaturen bewusst nicht angeglichen.** Wer sie vereinheitlichen will, tut es als eigene
   Geschmacksentscheidung, vollständig in einem Zug — nicht als Nebeneffekt dieser Story. Kosten: die
   Uneinheitlichkeit (mal mit, mal ohne `= default`) bleibt bestehen, ist aber ab jetzt keine
   Regelverletzung mehr.

## Akzeptanzkriterien

1. Der Abschnitt `CancellationToken` in `CLAUDE.md` verlangt den Vorgabewert nur dort, wo optionale Parameter
   vorangehen, und nennt CS1737 als den Mechanismus, der ihn dort erzwingt.
2. Die Aussage, dass der Vorgabewert im OpenAPI-Dokument unsichtbar ist, steht als Begründung dabei — sonst
   wird die Regel beim nächsten Lesen wieder ausgeweitet.
3. Kein Test und keine Signatur ändern sich; die Suite bleibt so grün wie vorher.
4. Das Kontext-Budget (`.claude/scripts/context-budget.sh`) reißt nicht — die Änderung ist eine
   Umformulierung, keine Ergänzung.

## Schätzung

**Größe XS** (Anker: zwei Sätze in `lib/fieldHelp.ts`, B-02) — eine Umformulierung von zwei, drei Sätzen in
einer bereits residenten Datei, kein Code, kein Test. `wo: doku`. `migration: nein`/`vertragsbruch: nein` —
kein Entity, kein Contract-Typ ändert sich.

### Testweg

Kein neuer Test — die Suite bleibt unverändert grün (Nachweis: `dotnet test` vor/nach identisch), zusätzlich
`.claude/scripts/context-budget.sh` gegen `backend/Pugling.Api/CLAUDE.md` bzw. das Root-`CLAUDE.md` (je
danach, wo der Abschnitt tatsächlich steht — siehe Ist-Stand: Root-`CLAUDE.md`).

## Verlauf

- **2026-08-04** — angelegt aus `docs/api-design-bewertung.md` (C4) und der Arbeitsrunde. Der Bericht hatte
  eine „mechanische Sitzung plus eine Zeile im Wächter" vorgeschlagen; der Entwickler widersprach, der
  API-Designer hat es nachgemessen (84 Parameternamen, kein Token-Treffer) und zugestimmt.
- **2026-08-06** — gegrillt und geschätzt: autonom, Nachtlauf-Freigabe 1 (`art: Aufräumen`). Die drei
  Punkte der Arbeitsrunde in nummerierte Entscheidungen überführt, Größe XS/`doku` gesetzt.
- **2026-08-06** — abgenommen: `CLAUDE.md` umformuliert (Sprint 1 von `docs/pm-sitzung-2026-08-06.md`).
  Suite unverändert grün (748/748 vor der Story), `context-budget.sh` läuft ohne Fehler (Warnung
  nicht-blockierend, Delta dokumentiert). Kein Code betroffen, daher kein `pugling-reviewer`-Lauf nötig
  (reine Prosa-Änderung an einer bereits vom Reviewer nicht abgedeckten Doku-Datei) — Rollengang ausdrücklich
  ausgefallen, Ersatz: keine Rolle liest `CLAUDE.md` zur Laufzeit, die Suite ist der einzig mögliche
  Regressionsbeleg.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob der Wortlaut in `CLAUDE.md` weiterhin exakt der
  geschärften Regel entspricht — hält (Abschnitt „Konventionen" → `CancellationToken`, Punkt 1, Wortlaut
  deckungsgleich mit der Story). Kein Fund.
