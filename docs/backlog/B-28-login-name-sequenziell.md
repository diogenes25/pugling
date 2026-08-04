---
tags: [typ/story, status/geschaetzt, bereich/auth]
aliases: [Sequenzielle Login-IDs]
status: geschaetzt
prio: P3
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: memory/offene-maengel-backlog.md
grund: ""
ersetzt_durch: []
---

# B-28 · Sequenzielle IDs als Login-Name

Der Login-Name ist eine fortlaufende Zahl. Wer sich anmelden will, muss nur hochzählen — die PIN ist der
einzige Schutz (gehasht und ratenbegrenzt, aber der Name ist geraten).

## User Story

Als **Vater** möchte ich, dass das Erraten der Kind-Nummer einem Angreifer möglichst wenig nützt, damit die
PIN tatsächlich die einzige und eine ausreichende Hürde bleibt — ohne dass mein Kind sich einen komplizierten
Namen merken muss.

## Ist-Stand am Code

- **Kein separates `LoginName`-Feld.** Der Login-Bezeichner ist die EF-Auto-Increment-`Id` der Entität
  selbst — `Child.Id`/`Adult.Id`/`Account.Id` (`backend/Pugling.Api/Models/AdminEntities.cs:49,107`,
  `backend/Pugling.Api/Models/IdentityEntities.cs:17`). Kein GUID, kein Zufallscode, kein Name+Suffix.
- Login läuft über `AdultLoginDto(int AdultId, string Pin)`, `ChildLoginDto(int ChildId, string Pin)`,
  `AccountLoginDto(int AccountId, string Pin)` (`backend/Pugling.Contracts/Auth/AuthDtos.cs:49,52,55`) —
  ein exakter Ganzzahlvergleich in `AuthController.cs:55-56` (Adult), `:71-72` (Child), `:97-98` (Account).
- **Das Frontend spiegelt das 1:1**: Die Sohn-Login-Maske fragt wörtlich nach der „Helden-Nummer" —
  `frontend/src/sohn/SohnLogin.tsx:66-71` — ein reines Zahlenfeld, `localStorage` merkt sich die zuletzt
  verwendete Id (`LAST_ID`, Zeile 7/11/28). `frontend/CLAUDE.md` bestätigt fürs Vater-Konto dasselbe Muster:
  „die neue Id ist der Login-Name".
- **Unbekannte Id und falsche PIN sind ununterscheidbar**: alle drei Login-Actions liefern für beide Fälle
  denselben `401 invalid_credentials` (`ApiErrors.cs:30`) — es gibt **keinen** 404-Pfad, der eine gültige Id
  verriete. Das ist der einzige (implizite) Enumerationsschutz.
- **Rate-Limiting ist IP-, nicht identitätsbasiert**: `Program.cs:249-258`, Policy `"login"`,
  `FixedWindowRateLimiter` mit `PermitLimit = 10`, `Window = 1 min`, partitioniert nach
  `http.Connection.RemoteIpAddress`. Ein verteilter Angreifer (viele IPs) unterliegt diesem Limit gar nicht.
  Ein Account-Lockout nach X Fehlversuchen existiert **nicht** (Suche nach `Lockout`/`FailedAttempts`: keine
  Treffer).
- **Kein Mindestmaß für die PIN.** `CreateChildDto`/`UpdateChildDto`/`CreateAdultDto`/`UpdateAdultDto`/
  `CreateTeacherDto`/`UpdateMyAccountDto` (`Pugling.Contracts`) tragen `Pin` als ungeprüften `string?`; jeder
  Controller hasht nur, prüft aber keine Länge (`ChildrenController.cs:78,117`, `AdultsController.cs:59,82`,
  `TeacherAccountsController.cs:54`, `AuthController.cs:179`). Eine PIN mit einer einzigen Ziffer wird
  klaglos angenommen und gehasht.
- **Randbefund, der die Lücke verschärft:** Eine leere PIN ist als „Konto absichtlich deaktivieren" gedacht
  (dokumentiert an `UpdateMyAccountDto.Pin`, `AuthDtos.cs:44-45`, und `CreateTeacherDto.Pin`,
  `ExerciseGrantDtos.cs:31`: „Empty = the account cannot (yet) log in"). Tatsächlich verifiziert
  `PinHasher.Verify` (`Auth/PinHasher.cs:30-34`) eine leere gespeicherte PIN über den Legacy-Klartext-Zweig
  (`stored.Split('.').Length != 4 → return stored == pin`): Bei `stored == ""` liefert `"" == pin` **`true`**,
  sobald `pin` ebenfalls `""` ist. Ein Kind ohne gesetzte PIN — laut Dokumentation nicht einloggbar — lässt
  sich also über einen rohen API-Call mit `pin: ""` anmelden. Die App-Oberflächen verhindern das nur, weil
  sie eine leere Eingabe clientseitig ablehnen (`SohnLogin.tsx:24`), nicht der Server.

## Die echte Lücke

Nicht die vermutete („der Name selbst verrät etwas") — die identischen 401-Antworten verhindern bereits,
dass ein Angreifer eine gültige Id von einer ungültigen unterscheiden kann, ohne gleichzeitig die richtige
PIN zu treffen. Die **echte** Lücke liegt tiefer: weil der Id-Raum klein, dicht und ab 1 durchnummeriert ist,
trägt die Id **keine eigene Entropie** — die gesamte Sicherheit hängt an der PIN allein. Und genau dort
fehlt jede Untergrenze: eine einstellige PIN wird angenommen, und eine (laut Doku) deaktivierte,
leere PIN lässt sich über die API sogar mit einer leeren Eingabe einloggen. Die Id durch einen
unerratbaren Code zu ersetzen würde die Helden-Nummer-Eingabe verkomplizieren, ohne das eigentliche Loch zu
stopfen — die PIN bliebe weiterhin beliebig schwach.

## Offene Punkte

- ~~Ist ein sprechender Login-Name (oder ein Zufallscode) den Kindern zumutbar — die Einfachheit war der
  Grund für die Zahl?~~ → siehe Entscheidung 1.
- ~~Reicht die bestehende Absicherung (identische 401, IP-Rate-Limit) angesichts der kleinen Id-Range, oder
  muss zusätzlich etwas an der PIN selbst gehärtet werden?~~ → siehe Entscheidung 2.
- ~~Der Randbefund zur leer-verifizierbaren PIN — im Rahmen dieser Story mitfixen oder als eigener Defekt
  auslagern?~~ → siehe Entscheidung 3.

## Entscheidungen

1. **Der Login-Bezeichner bleibt die schlichte Id — kein Zufallscode, keine Umbenennung.** Begründung: Die
   identischen 401-Antworten (Ist-Stand) machen die Id allein schon wertlos für eine Enumeration ohne
   Treffer; ein unerratbarer Code würde nur die kindgerechte „Helden-Nummer" (`SohnLogin.tsx`) ersetzen,
   ohne die eigentliche Schwachstelle (PIN-Stärke) zu berühren. Kosten: keine — es ändert sich nichts am
   bestehenden Verhalten, das Risiko wird stattdessen an der PIN geschlossen (Entscheidung 2).
2. **Statt die Id zu härten, bekommt die PIN eine Mindestlänge von 4 Zeichen** (passend zur geseedeten
   Familie, `"0000"`, und dem Sohn-PIN-Pad, das ohnehin 4 Punkte anzeigt) — geprüft an allen fünf
   Schreibpfaden: `ChildrenController.Create/Update`, `AdultsController.Create/Update`,
   `TeacherAccountsController.Create`, `AuthController.UpdateMe`. Eine explizit leere PIN (`""`) bleibt
   erlaubt und bedeutet weiterhin „Login deaktiviert" (dokumentiertes Verhalten, PATCH-Konvention: der Wert
   zuerst, dann kein zusätzlicher Schalter nötig, da `""` selbst schon eindeutig ist). Verstoß meldet
   `ApiErrors.ValidationError` (400) — kein neuer Fehlercode nötig, dasselbe Muster wie das bestehende
   „Name is required." Kosten: fünf Codestellen plus Tests; keine Migration (keine Spaltenänderung), kein
   Vertragsbruch (DTO-Form bleibt, nur eine zusätzliche 400-Antwort auf bisher unvalidierten Input).
3. **Der Randbefund (leere gespeicherte PIN verifiziert gegen leere Eingabe) wird in derselben Änderung
   mitgeschlossen**, nicht ausgelagert: Er hängt an derselben Codestelle (`PinHasher.Verify`) und derselben
   Ursache (kein Mindestmaß), und ihn offen zu lassen würde Entscheidung 2 aushebeln — eine PIN, die durch
   die neue Untergrenze nie kürzer als 4 Zeichen sein darf, aber über den Login-Endpunkt trotzdem mit `""`
   umgangen werden könnte, wäre nur auf dem Papier gehärtet. Fix: die drei Login-Actions (oder zentral
   `PinHasher.Verify`) lehnen eine leere gespeicherte PIN **unabhängig** von der eingegebenen PIN ab —
   „deaktiviert" heißt „kein Login", nicht „Login mit leerer Eingabe". Kosten: ein Guard-Clause je
   Login-Action (oder eine Zeile in `Verify`), plus ein Regressionstest in `SecurityHardeningTests.cs`.

## Akzeptanzkriterien

1. `POST supervisor/children`, `PATCH supervisor/children/{id}`, `POST supervisor/adults`,
   `PATCH supervisor/adults/{id}`, `POST creator/teachers` und `PATCH auth/me` lehnen eine **nicht-leere**
   PIN mit weniger als 4 Zeichen mit `400 validation_error` ab.
2. Dieselben Endpunkte akzeptieren weiterhin eine explizit leere PIN (`""`) als „Login deaktivieren" —
   unverändertes Verhalten.
3. `POST auth/child`, `POST auth/adult`, `POST auth/login` liefern `401 invalid_credentials`, wenn die
   gespeicherte PIN leer ist — unabhängig davon, ob die eingegebene PIN ebenfalls leer ist oder nicht.
4. Die geseedete Familie (PIN `"0000"`) und alle bestehenden Login-Testfälle bleiben unverändert grün.
5. Die IP-basierte Rate-Limit-Policy `"login"` und die identische 401-Antwort für unbekannte Id/falsche PIN
   bleiben unangetastet (bewusst nicht Teil dieser Story, siehe Entscheidung 1).

## Schätzung

**Größe: S** — Anker B-01 („`childId` aus dem Test-Pfad ziehen"): eine kleine, gut lokalisierte
Validierungsregel über fünf bekannte Schreibpfade plus ein Guard in den drei Login-Actions, keine neue
Fachlichkeit, kein neues Modell.

- **`wo`: backend** — reine API-Validierung, kein UI-Vertrag ändert sich (die Sohn-„Helden-Nummer"-Maske
  bleibt unverändert, Entscheidung 1).
- **`migration`: nein** — `Pin`/`PinHash` bleiben unveränderte Spalten, es wird nur strenger validiert.
- **`vertragsbruch`: nein** — kein DTO-Feld wird umbenannt/entfernt; ein bisher unvalidierter Input löst neu
  einen 400 statt eines stillen Erfolgs aus. Bestehende Fixtures (PIN `"0000"`) sind bereits konform.
- **Risiken**: eine Test-Fixture oder ein Seed-Pfad, der irgendwo eine kürzere Test-PIN nutzt, könnte rot
  werden — mechanisch aufzufinden über den ersten roten Testlauf, kein verdecktes Risiko. Der
  Enumerationsschutz durch identische 401-Antworten bleibt unverändert; diese Story schließt nur die
  PIN-seitige Schwäche, nicht die grundsätzliche Erratbarkeit der Id (bewusst, Entscheidung 1) — ein
  entschlossener, verteilter Angreifer mit vielen IPs kann die (jetzt 4-stellige) PIN gegen eine bekannte Id
  weiterhin offline-artig durchprobieren, da kein Account-Lockout existiert; das bleibt ein **bewusst
  zurückgestelltes** Restrisiko dieser Story und keine eigene Empfehlung hier (kein neuer Backlog-Eintrag,
  wie vom Auftrag vorgegeben).
- **Angriffsplan** (Backend zuerst, es gibt kein Frontend-Werk): (1) eine kleine Validierungshilfe (z. B.
  `PinHasher`- oder Controller-nahe Konstante `MinPinLength = 4`) einführen; (2) die fünf Schreibpfade um die
  Prüfung ergänzen (Guard-Clause-Stil, `this.ProblemWithCode(ApiErrors.ValidationError, …)`); (3) die drei
  Login-Actions bzw. `PinHasher.Verify` um den Leer-PIN-Guard ergänzen; (4) Tests.
- **Testweg**: `SecurityHardeningTests.cs` um Fälle für „leere gespeicherte PIN loggt nicht mit leerer
  Eingabe ein" erweitern; `ChildrenDashboardTests.cs`/`AdultLifecycleTests.cs` um je einen Fall „PIN mit 1-3
  Zeichen → 400" ergänzen. Kein E2E/`/smoke-test` nötig, da rein serverseitige Validierung ohne
  UI-Sichtbarkeit.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Ist-Stand gegen den Code belegt (`AdminEntities.cs`, `AuthController.cs`,
  `Program.cs`, `PinHasher.cs`, `SohnLogin.tsx`) — die vermutete Lücke („Name verrät etwas") entkräftet
  durch die identischen 401-Antworten, dafür ein schärferer Randbefund gefunden (leere PIN verifiziert
  gegen leere Eingabe).
- **2026-08-03** — gegrillt: drei Entscheidungen autonom getroffen, Nutzerauftrag 2026-08-04 (Id bleibt
  unverändert; PIN-Mindestlänge 4 statt Id-Umbau; Randbefund im selben Zug geschlossen).
- **2026-08-03** — geschätzt: Größe S, `wo: backend`, keine Migration, kein Vertragsbruch, Testweg über
  `SecurityHardeningTests.cs`/`ChildrenDashboardTests.cs`/`AdultLifecycleTests.cs` — autonom getroffen,
  Nutzerauftrag 2026-08-04.
