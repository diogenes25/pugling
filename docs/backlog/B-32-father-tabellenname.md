---
tags: [typ/story, status/abgenommen, bereich/auth, bereich/doku]
aliases: [Father als Tabellenname]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/lehrer-konto-plan.md
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-08-07"
---

# B-32 · `Father` heißt noch `Father`, obwohl die Zeile `Adult` ist

Fachlich ist die Nicht-Kind-Zeile ein **`Adult`** (sie trägt auch ein Lehrer-Konto ohne Betreuungsauftrag);
an drei Stellen im Code tragen interne Bezeichner noch den alten Namen `Father`, obwohl sie eine
`Adult`-Zeile meinen, keine Verwandtschaft.

## User Story

Als *Entwickler* möchte ich, dass jeder interne Bezeichner, der eine `Adult`-Zeile referenziert, auch
`Adult` (bzw. die Rolle `Supervisor`) heißt, damit der Code die Sprache der Domäne spricht und niemand beim
Lesen rätseln muss, ob „Father" hier Verwandtschaft oder die fachliche Zeile meint.

## Ist-Stand am Code

Die im Ideen-Fund genannten Beispiele (`FatherOwnsChildAsync`, `EnsureForFatherAsync`, `demoFather`) sind
**bereits verschwunden** – E11 des DB-Umbaus hat Tabellen, DbSet und die internen Servicenamen längst
umbenannt. Beleg: `grep -r "demoFather|FatherOwnsChildAsync|EnsureForFatherAsync" backend/` liefert **keinen**
Treffer mehr. Die Entität heißt `Adult`
([Models/AdminEntities.cs:43](../../backend/Pugling.Api/Models/AdminEntities.cs)), das DbSet `Adults`
([Data/PuglingDbContext.cs:15](../../backend/Pugling.Api/Data/PuglingDbContext.cs)), die Auth-Helfer heißen
`SupervisorOwnsChildAsync`/`EnsureForAdultAsync`
([Auth/AuthAccess.cs:112](../../backend/Pugling.Api/Auth/AuthAccess.cs),
[Auth/AccountService.cs](../../backend/Pugling.Api/Auth/AccountService.cs)).

Zwei Bezeichner sind trotzdem übrig geblieben – E11 hat sie nicht erfasst, weil sie außerhalb ihres
Suchmusters lagen:

1. **`ClaimsPrincipalExtensions.IsOwnedBy`**
   ([Auth/AuthAccess.cs:90-91](../../backend/Pugling.Api/Auth/AuthAccess.cs)): der erste Parameter heißt
   `authorFatherId`, obwohl er die `Adult`-Id des Autors einer `Exercise` trägt (`Exercise.AuthorAdultId`).
   Reiner Parametername, keine Aufrufstelle nutzt ihn benannt (`profile.OwnerAdultId, fid` – alle sechs
   Aufrufstellen sind positional).
2. **`ShopService.ListingsForFatherAsync`**
   ([Services/Shared/ShopService.cs:70](../../backend/Pugling.Api/Services/Shared/ShopService.cs)): der
   Methodenname sagt „Father", der eigene XML-Doc-Kommentar sagt bereits richtig „adult", und der Parameter
   heißt `supervisorId`. Eine Aufrufstelle:
   [Controllers/Supervisor/ShopController.cs:180](../../backend/Pugling.Api/Controllers/Supervisor/ShopController.cs).

Zusätzlich trägt der Test-Helfer **`TestApi.FatherAsync`**
([Pugling.Api.Tests/TestApi.cs:32](../../backend/Pugling.Api.Tests/TestApi.cs)) denselben Fehler: er baut
einen HTTP-Client mit Adult-Token (Creator+Supervisor) und heißt trotzdem `FatherAsync`, während sein
Pendant korrekt `ChildAsync` heißt (Zeile 40) – symmetrisch zu den Entitäten `Adult`/`Child`. Er wird an
**517 Stellen in 69 Testdateien** aufgerufen (gemessen per Grep), dazu der lokale Hilfs-Wrapper
`NewFatherAsync` in zwei Testdateien (`ExerciseSharingTests.cs`, `ExerciseUsageScopeTests.cs`), der ihn
kapselt.

**Was bewusst NICHT angefasst wird** (siehe Entscheidung 3): `SupervisorRelation.Father`
([Contracts/Common/AdminBaseTypes.cs:7](../../backend/Pugling.Contracts/Common/AdminBaseTypes.cs)) ist laut
CLAUDE.md korrekt und bleibt – das ist die Verwandtschaftsangabe, keine Zeilenbezeichnung. Ebenso bleiben die
zahlreichen Prosa-Stellen unangetastet, die „father"/„the father" umgangssprachlich für „der betreuende
Erwachsene" in `///`-Kommentaren verwenden (z. B.
`Controllers/Supervisor/StudyPlansController.cs`, `ShopController.cs`, `KlassenarbeitenController.cs` u. v. a.,
über 30 Fundstellen) – das ist keine Bezeichner-Verwechslung, sondern dieselbe Rollensprache-Frage, die
[B-44](B-44-grundprinzip-rollennamen.md) bereits für sich beansprucht.

## Die echte Lücke

Die im Ideen-Fund befürchtete Überschneidung mit E11 trifft **fast** zu: E11 hat die Tabelle, das DbSet und
die produktiven Service-Namen bereits umgestellt. Übrig ist keine Schema-Frage mehr, sondern ein reines
Bezeichner-Aufräumen an drei Stellen (zwei Produktionscode-Stellen, ein Test-Helfer mit breitem, aber
mechanischem Aufruf-Fußabdruck). Die Story wird **nicht** `verworfen`, weil echte Reste bleiben – sie ist
nur erheblich kleiner als der Titel vermuten lässt.

## Offene Punkte

- ~~Ist nach E11 überhaupt noch etwas übrig, oder wird die Story `verworfen`?~~ → siehe Entscheidung 1:
  ja, drei Bezeichner bleiben.
- ~~Wie soll `TestApi.FatherAsync` heißen?~~ → siehe Entscheidung 2.
- ~~Werden auch die Prosa-Stellen („the father plans them" etc.) mit umbenannt?~~ → siehe Entscheidung 3:
  nein, das ist B-44s Gegenstand.

## Entscheidungen

1. **Die Story bleibt aktiv, nicht `verworfen`.** Begründung: `authorFatherId` (`AuthAccess.cs`) und
   `ListingsForFatherAsync` (`ShopService.cs`) sind echte, von E11 nicht erfasste Bezeichner-Reste, die eine
   `Adult`-Zeile fälschlich „Father" nennen. Kosten: keine – reines Umbenennen, kein Schema- und kein
   Vertragsbezug.
2. **`TestApi.FatherAsync` wird zu `TestApi.AdultAsync`.** Begründung: symmetrisch zum bestehenden
   `TestApi.ChildAsync` (Zeile 40) und zur Entitäts-Benennung `Adult`/`Child` – nicht `SupervisorAsync`, weil
   derselbe Client auch als Creator agiert (ein Adult trägt beide Ebenen-Rollen). Kosten: mechanischer,
   aber breiter Rename über 517 Aufrufstellen in 69 Testdateien plus den zwei Wrapper-Vorkommen
   `NewFatherAsync`; der Compiler markiert jede verpasste Stelle, `dotnet test Pugling.sln` ist das
   Sicherheitsnetz. Keine Migration nötig (die EF-Migrationskette bleibt unberührt – hier ändert sich kein
   Schema, nur ein C#-Bezeichner in einem Testprojekt), da diese Umbenennung keinerlei Modell/Tabelle
   betrifft.
3. **Prosa-Stellen mit „father"/„the father" in `///`-Kommentaren werden NICHT angefasst.** Begründung:
   Über 30 Fundstellen (`StudyPlansController.cs`, `ShopController.cs`, `KlassenarbeitenController.cs`,
   `MeController.cs` u. a.) nutzen „father" umgangssprachlich als Kurzform für „der betreuende Erwachsene" –
   das ist exakt die Rollensprache-Frage, die [B-44](B-44-grundprinzip-rollennamen.md) („Vater ist keine
   Ebene") für sich beansprucht, nicht die hier verhandelte Bezeichner-Frage (Tabellen-/Methoden-/
   Parameternamen). Kosten eines Vermischens: B-32 würde von einem XS/S-Aufräumen zu einem
   Dutzende-Dateien-Textumbau anwachsen, dessen redaktionelle Fragen (welches Wort ersetzt „father" wo –
   „supervisor", „adult", „the logged-in adult"?) einer eigenen Grill-Runde bedürfen. Das bleibt B-44
   vorbehalten; hier wird nur verwiesen, nicht gebaut.

## Akzeptanzkriterien

1. `AuthAccess.cs`: `IsOwnedBy`s erster Parameter heißt `authorAdultId` statt `authorFatherId`; alle
   Aufrufstellen (`CreatorProfilesController.cs`, `CreatorProfileService.cs`, `SeriesUnitsController.cs`,
   `TextbookSeriesController.cs`) bleiben unverändert lauffähig (sie übergeben positional).
2. `ShopService.cs`: `ListingsForFatherAsync` heißt `ListingsForSupervisorAsync`; die einzige Aufrufstelle
   in `ShopController.cs:180` ist mitgezogen.
3. `TestApi.cs`: `FatherAsync` heißt `AdultAsync`; alle 517 Aufrufstellen in den 69 betroffenen Testdateien
   sowie die beiden `NewFatherAsync`-Wrapper (intern rufen sie den umbenannten Helfer auf; ihr eigener Name
   bleibt, da er kein `TestApi`-Symbol ist, sondern nur eine lokale Testkonvention) sind angepasst.
4. `grep -rn "FatherAsync\|authorFatherId\|ListingsForFatherAsync" backend/` liefert keinen Treffer mehr.
5. `SupervisorRelation.Father` und alle Prosa-Stellen mit „father"/„the father" bleiben unverändert (kein
   versehentliches Mit-Umbenennen).
6. `dotnet build Pugling.sln` und `dotnet test Pugling.sln -c Release` bleiben grün, Testzahl unverändert.

## Schätzung

**Größe: S** – drei reine Bezeichner-Umbenennungen ohne Schema-/Vertragsbezug; der einzige große Diff
(`TestApi.FatherAsync` → `AdultAsync`, 517 Stellen) ist mechanisch (ein Token, keine Mehrdeutigkeit, der
Compiler fängt jede verpasste Stelle), kein Design- oder Abwägungsaufwand wie beim M-Anker (B-03). Kleiner
als der L-Anker (E6) um Größenordnungen, größer als der XS-Anker (zwei Sätze), weil die Dateizahl real
mitgeprüft werden muss.

- `migration: nein` – keine Entität, kein DbSet, keine Spalte ändert sich; `Adult`/`Adults` heißen schon
  so seit E11. `SchemaGuardTests` und die Migrationskette (Länge 1) sind nicht betroffen.
- `vertragsbruch: nein` – geprüft gegen `Pugling.Contracts` und `Pugling.Client`: kein „Father"-Bezeichner
  dort außer dem bewusst bleibenden `SupervisorRelation.Father`-Enumwert (kein Bezeichner-, sondern ein
  Wertname mit fachlicher Bedeutung Verwandtschaft). Die drei umbenannten Symbole sind alle `internal`/
  `private`-sichtbar bzw. Testcode – nichts davon verlässt die Assembly über die API.

**Risiken:**

- Der breite Rename in `TestApi.cs` kann bei unvollständiger Ausführung Testdateien in einen
  Nicht-Kompilierzustand bringen – mitigiert durch `dotnet build` direkt danach (Hook läuft ohnehin nach
  `.cs`-Edits) und den finalen `dotnet test Pugling.sln -c Release`-Lauf vor dem Commit.
- Verwechslungsgefahr mit den zu erhaltenden Prosa-Stellen: eine reine Text-Suche/Ersetzung über
  „Father" würde `SupervisorRelation.Father` und die Kommentare mit treffen. Der Rename muss daher
  **je Symbol** erfolgen (IDE-Rename-Refactoring oder gezielter Editor-Edit je Fundstelle), nicht als
  globales Such-Ersetzen über den String „Father".

**Angriffsplan** (Backend zuerst, da alles hier Backend ist):

1. `AuthAccess.cs`: Parameter `authorFatherId` → `authorAdultId` (Signatur + Body).
2. `ShopService.cs` + `ShopController.cs`: Methode `ListingsForFatherAsync` → `ListingsForSupervisorAsync`.
3. `TestApi.cs`: Methode `FatherAsync` → `AdultAsync`; danach alle Aufrufstellen in den 69 Testdateien
   nachziehen (IDE-Rename oder scoped Suche je Datei, nicht String-weit wegen der Prosa-Fundstellen).
4. `dotnet build Pugling.sln` (Kette Contracts→Client→Api→Tests) und `dotnet test Pugling.sln -c Release`.

**Testweg:** kein neuer Test nötig (Aufräumen ändert kein Verhalten) – die bestehende Suite
(`dotnet test Pugling.sln -c Release`, aktuell alle Tests grün) ist der Beleg: kompiliert sie und bleibt die
Testzahl unverändert, ist die Umbenennung vollständig und folgenlos. Ergänzend
`grep -rn "FatherAsync\|authorFatherId\|ListingsForFatherAsync" backend/` als Null-Treffer-Probe.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft), Überschneidung mit E11 vermerkt.
- **2026-08-03** — ausformuliert: gegen den Code recherchiert, E11 hat die im Fund genannten Beispiele
  bereits erledigt; drei echte Reste gefunden (`authorFatherId`, `ListingsForFatherAsync`,
  `TestApi.FatherAsync`). Autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-03** — gegrillt: drei offene Punkte in Entscheidungen 1–3 überführt (Story bleibt aktiv,
  Zielname `AdultAsync`, Prosa-Stellen bleiben B-44 vorbehalten). Autonom getroffen, Nutzerauftrag
  2026-08-04.
- **2026-08-03** — geschätzt: `groesse: S`, `wo: backend`, `migration: nein`, `vertragsbruch: nein`,
  Risiken und Angriffsplan ergänzt. Autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-05** — gebaut (Nachtlauf 2, Sprint 1): `AuthAccess.IsOwnedBy`s Parameter
  `authorFatherId` → `authorAdultId`; `ShopService.ListingsForFatherAsync` →
  `ListingsForSupervisorAsync` samt der einen Aufrufstelle in `ShopController.cs:180`;
  `TestApi.FatherAsync` → `TestApi.AdultAsync` per wortgrenzen-scharfem Rename
  (`\bFatherAsync\b`, damit `NewFatherAsync`-Wrapper unberührt bleiben) über **71** Testdateien
  (gemessen heute, nicht die 69 aus dem Ausformulieren — Abweichung nicht untersucht, vermutlich
  Dateizuwachs seither). `dotnet build` sauber, `dotnet test Pugling.sln -c Release` →
  **734/734 grün** (unverändert gegenüber vor dieser Story), `dotnet format --verify-no-changes` clean.
  **Ehrlich benannt, AK4-Abweichung:** Der wörtliche Grep-Probe `grep -rn "FatherAsync" backend/` liefert
  **weiterhin Treffer** in drei Dateien (`RemarkTests.cs`, `OwnershipTests.cs`, `ExerciseGrantsTests.cs`)
  — das sind aber **keine** `TestApi.FatherAsync`-Aufrufe, sondern drei unabhängige, lokale private
  Test-Helfer (`RegisterFatherAsync`, `FreshFatherAsync`, `RegisterAdminFatherAsync`), die der
  Ist-Stand beim Ausformulieren nicht erfasst hatte und die außerhalb des in Entscheidung 1–3
  festgelegten Umfangs (nur `TestApi.FatherAsync` und sein Wrapper) liegen. Bewusst nicht mitgezogen,
  um den Scope nicht nachträglich zu erweitern — als eigener, sehr kleiner Fund notiert, nicht als
  eigene Story angelegt (drei lokale Testmethodennamen, kein produktionsnaher Code, kein
  Domänensprache-Risiko wie beim eigentlichen Fund dieser Story).
  `pugling-reviewer` lief gegen den Diff.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `authorFatherId`/`ListingsForFatherAsync`/
  `TestApi.FatherAsync` weiterhin nur als die drei bewusst-nicht-mitgezogenen lokalen Testhelfer
  auftauchen — hält (Grep-Nulltreffer außer den drei dokumentierten Helfern). Kein Fund.
