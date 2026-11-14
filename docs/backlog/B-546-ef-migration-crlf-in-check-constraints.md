---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/qualitaet]
aliases: [EF-Migration schreibt CRLF in Check-Constraints]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Bau von B-530 im Backlog-Lauf 2026-11-13 (docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-546 · `migrations add` unter Windows schreibt `\r\n` in Check-Constraint-Strings

Beim Anlegen der Migration `20261113224441_CardShownAt` (B-530) hat EF unter Windows in drei
Check-Constraint-Strings des Snapshots `\r\n` statt des `\n` der bestehenden Kette geschrieben; von Hand
zurückgesetzt. Jede künftige `migrations add` auf Windows braucht dieselbe Korrektur, und kein Tor fängt sie: der
Unterschied ist für `HasPendingModelChanges` unsichtbar, erzeugt aber einen lauten Snapshot-Diff und – je nach
Plattform des nächsten Laufs – eine scheinbare Modelländerung (vgl. Memory „Lokal Windows, CI Linux“).

Zu klären: ein Nachlauf-Skript zu `migrations add` (normalisiert die Strings), oder ein Wächter in `SchemaGuardTests`,
der `\r` in Check-Constraint-SQL des Snapshots ablehnt.

## Ist-Stand am Code (2026-11-14)

- Die Ursache liegt vermutlich nicht in EF, sondern in der Quelle: die drei Constraints stehen als Raw-String-Literale
  (`"""`) in `backend/Pugling.Api/Data/PuglingDbContext.cs:133/449/479`; die Datei ist im Index LF, im Windows-
  Arbeitsbaum CRLF (`git ls-files --eol`: `i/lf w/crlf`, `.gitattributes` setzt für `*.cs` nur `text`). Der
  Compiler übernimmt das Zeilenende des Quelltexts in den String, also schreibt `migrations add` dort `\r\n`.
- Der Snapshot ist heute sauber (`\n` in `PuglingDbContextModelSnapshot.cs:93/424/1405`, kein `\r` im Ordner
  `Data/Migrations`); `SchemaGuardTests.cs:554` liest die Constraints nur nach Namen, nicht nach Inhalt.
- Damit kommt eine dritte Lösung dazu: `.ReplaceLineEndings("\n")` an den drei Literalen oder `eol=lf` für die Datei.

## Die echte Lücke

Das Modell selbst hängt vom Zeilenende des Checkouts ab. Die Korrektur gehört an die Quelle (das Modell), nicht an
den Snapshot – ein Nachlauf-Skript würde jede Migration von Hand reparieren, ein Wächter nur auf dem Snapshot die
Ursache stehen lassen.

## Entscheidungen

1. **Die Quelle normalisiert:** alle Check-Constraint-SQL laufen durch einen Helfer `CheckSql` in
   `PuglingDbContext` (`ReplaceLineEndings("
")`). Begründung: eine Stelle, plattformunabhängig, kein Nachlauf.
   Kosten: ein vierter Constraint muss den Helfer nutzen – das hält der Regressionstest unten. (`Aufräumen`, autonom
   gegrillt.)
2. **Kein `eol=lf` für die Datei:** das änderte die Arbeitskopie aller Windows-Nutzer und hilft keinem künftigen
   Raw-String in einer anderen Datei.

## Akzeptanzkriterien

1. Das Design-Time-Modell trägt in keinem Check-Constraint ein `` (Windows-Arbeitskopie).
2. `migrations add` auf Windows ändert die drei Constraint-Zeilen im Snapshot nicht.

## Schätzung

**Größe XS**, `wo: backend`, `migration: nein` (das Modell-SQL war in der Kette schon `
`), `vertragsbruch: nein`.
Risiko: Modell-Drift, falls `ReplaceLineEndings` ein anderes SQL ergäbe als der Snapshot – `SchemaGuardTests`
(`HasPendingModelChanges`) fängt das. Testweg: `SchemaGuardTests.CheckConstraintSql_Traegt_Nur_LF` (rot auf
Windows ohne den Helfer; auf Linux strukturell blind – dort hat die Quelle schon LF).

## Verlauf

- **2026-11-13** — angelegt aus dem Bau von B-530.
- **2026-11-14** — Abgleich Backlog-Lauf: gilt, Ursache präzisiert (Raw-String-Literale in einer CRLF-Arbeitskopie, `PuglingDbContext.cs:133/449/479`); Snapshot derzeit sauber, kein Tor prüft den Inhalt.
- **2026-11-14** — Backlog-Lauf, Sprint 73: beim Anlegen der B-535-Migration wieder aufgetreten (drei `
` im
  Snapshot), damit gemessen. Ausformuliert, gegrillt (autonom, `Aufräumen`), geschätzt und gebaut: `CheckSql` an den
  drei Literalen, Regressionstest in `SchemaGuardTests`.
- **2026-11-14** — **abgenommen** (Sprint 73). volle Suite 1612/1612 (Release), Vitest 1000/1000, `tsc -b` grün, Rollengang Sohn als Playwright gegen frisch gestartete Server nach dem letzten Edit: 9 passed (`belohnung-trotz-wochenziel`, `bonus`, `bonus-deckel*`, `pfad`, `lektion`, `erstbegegnung`, `full-flow`); Reviews `pugling-reviewer` + `frontend-reviewer` (Funde im Sprint behoben); Commit `fa335075`. Mutante M13 (`CheckSql` am ersten Constraint weg) → rot an
  `CheckConstraintSql_Traegt_Nur_LF` (Windows-Arbeitskopie; auf Linux strukturell blind, im Test so beschriftet).
  Beleg im Gebrauch: die zweite Migration des Sprints (`ExtraRunContentHash`) brauchte keine Handkorrektur mehr.
  Kein Rollengang (kein Laufzeitverhalten).
