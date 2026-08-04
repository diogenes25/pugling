---
tags: [typ/story, status/geschaetzt, bereich/doku, bereich/auth]
aliases: [Admin-Rolle, Break-Glass, vierter Akteur]
status: geschaetzt
prio: P3
art: Aufräumen
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: Sitzung 2026-08-01 (Rollen-Durchgang)
---

# B-51 · Die Admin-Rolle kommt in keinem Rollen-Dokument vor

Neben Creator, Supervisor und Student gibt es einen **vierten Akteur**: `Roles.Admin`, der
Plattform-Superuser als Break-Glass. Er wird nicht über die API vergeben, sondern über das Flag
`Adult.IsAdmin` (DB/Seed) und beim Login als Rollen-Claim ausgestellt. Er umgeht die RWX-Prüfung an
Übungen — gedacht etwa, um verwaiste Übungen ohne Owner zu reparieren — und darf alle Anmerkungen lesen
und fremde Kommentare löschen.

**Die drei Dokumente, die Rollen erklären, kennen ihn nicht**: weder [grundprinzip.md](../grundprinzip.md)
noch [rollen-doku.md](../rollen-doku.md) noch [wiki/02 · Authentifizierung](../../wiki/02-authentifizierung.md)
erwähnen ihn. Substanziell beschrieben ist er heute nur als Nebenbemerkung in einem Feature-Plan
([anmerkungen-plan.md](../anmerkungen-plan.md)) — und zwar an der interessantesten Stelle: Dort wurde
`Roles.Admin` als Bedingung **ausdrücklich verworfen**, weil die Rolle „auch die RWX-Rechte umgeht", also
zu breit ist, um als Sichtbarkeitsschalter zu dienen. Dazu der Fallstrick, dass ein frisch gesetztes
`IsAdmin` **erst nach neuer Anmeldung** wirkt, weil Rollen im JWT stecken.

## User Story

Als Entwickler, der eine neue Rechte-Prüfung entwirft, möchte ich an einer zentralen, technischen Stelle
nachlesen können, dass es die Admin-Rolle gibt, was sie bewirkt und wo ihre Grenzen liegen, damit ich sie
beim nächsten Rechte-Entwurf weder übersehe noch — wie schon einmal versucht — als generischen
Sichtbarkeitsschalter missbrauche.

## Ist-Stand am Code

- **Definition:** `Roles.Admin = "Admin"` ([AuthAccess.cs:38](../../backend/Pugling.Api/Auth/AuthAccess.cs)),
  Flag `Adult.IsAdmin`
  ([AdminEntities.cs:50-55](../../backend/Pugling.Api/Models/AdminEntities.cs)) — laut eigenem XML-Doc
  „deliberately not settable through the API – only via DB/seed (no self rights escalation)".
- **Claim-Ausstellung:** `TokenService.IssueForAccount(..., bool isAdmin)` setzt den Rollen-Claim nur, wenn
  `isAdmin` gesetzt ist
  ([TokenService.cs:28,41](../../backend/Pugling.Api/Auth/TokenService.cs)). `AuthController` berechnet
  `isAdmin` beim Adult-Login aus `adult.IsAdmin` (`:59`) und beim konto-zentrischen `/auth/login` über alle
  Profile eines Kontos (`:102-103`,
  [AuthController.cs](../../backend/Pugling.Api/Controllers/AuthController.cs)).
- **Wirkung (Bypass):** `ExercisePermissionService.CanWrite`/`CanAdminister`/das `ExecutePublic`-Gate geben
  bei `user.IsAdmin()` sofort `true` zurück
  ([ExercisePermissionService.cs:24,34,46](../../backend/Pugling.Api/Auth/ExercisePermissionService.cs));
  dazu zwei statische Overloads für Stellen ohne `ClaimsPrincipal` (`:56-62`).
- **Zweite Verwendung:** `RemarksController` nutzt `User.IsAdmin()` für kontenübergreifendes Lesen
  (`:113`), das Löschen fremder Kommentare (`:435`) und den Scope-Zuschnitt (`:502`,
  [RemarksController.cs](../../backend/Pugling.Api/Controllers/RemarksController.cs)).
- **Bereits verworfen als generische Bedingung:** [anmerkungen-plan.md:386-391](../anmerkungen-plan.md)
  dokumentiert ausführlich, warum `Roles.Admin` **nicht** als Bedingung für das
  Anmerkungs-`GlobalRead` diente — sie hätte über den RWX-Bypass jedem debuggenden Vater zugleich erlaubt,
  fremde Übungen zu ändern/löschen. Gebaut wurde stattdessen der engere, zweckgebundene Schalter
  `RemarkOptions.GlobalRead`
  ([RemarkOptions.cs:12-16](../../backend/Pugling.Api/Services/Shared/RemarkOptions.cs)).
- **Kein Konto trägt es heute:** `Data/Seed.cs:941-969` (`SeedAdmin`) legt Vater und Kind an, setzt
  `IsAdmin` nirgends. Eine repo-weite Suche nach `IsAdmin =` findet es nur in zwei Testfixtures
  (`ExerciseGrantsTests.cs:50`, `RemarkTests.cs:665`), die es direkt auf der DB setzen — nie über Seed oder
  API. Die App läuft also heute ohne ein einziges Admin-Konto.
- **Die drei Rollen-Dokumente kennen ihn wirklich nicht:** eine gezielte Suche nach „Admin" über
  `docs/grundprinzip.md`, `docs/rollen-doku.md` und `wiki/02-authentifizierung.md` liefert null Treffer in
  allen drei Dateien.
- **Wo er heute erklärt ist:** dichte, aber verstreute XML-Doc-Kommentare
  (`AuthAccess.cs:35-38`, `AdminEntities.cs:50-55`, `TokenService.cs:27`,
  `ExercisePermissionService.cs:12-13`) plus die eine ausführliche Passage in `anmerkungen-plan.md` — einem
  Feature-Plan, keiner Rollen-Referenz.

## Die echte Lücke

Nicht „Admin ist undokumentiert" — im Code ist die Rolle ordentlich beschrieben, und als abgelehnter
Präzedenzfall ist sie in `anmerkungen-plan.md` sogar ungewöhnlich gut begründet. Die echte Lücke ist
schmaler: Wer über die drei Rollen-Dokumente einsteigt — der dokumentierte Startpunkt für „welche Rollen
gibt es" —, erfährt nichts von einem vierten, quer liegenden Flag, nichts über seinen Bypass, und nichts
davon, dass es als generischer Rechte-Schalter bereits einmal versucht und verworfen wurde. Genau das ist
das Risiko aus der Notiz: ein künftiger Rechte-Entwurf entsteht ohne diesen Präzedenzfall im Blick und
übersieht Admin entweder ganz oder reift denselben Fehler noch einmal nach.

## Offene Punkte

1. ~~Gehört Admin in die Rollen-Doku (sichtbar, mit Warnung vor ihrer Breite) oder ausdrücklich nicht?~~
   → siehe Entscheidung 1.
2. ~~Soll sie ohne API vergeben bleiben?~~ → siehe Entscheidung 2.
3. ~~Trägt ihre Breite noch, oder gehört sie in engere Rechte zerlegt?~~ → siehe Entscheidung 3.

## Entscheidungen

1. **Admin bekommt einen eigenen Absatz in [wiki/02-authentifizierung.md](../../wiki/02-authentifizierung.md)**
   (die technische Rollen-/Claims-Referenz), **nicht** in `docs/grundprinzip.md`. Begründung:
   `grundprinzip.md` beschreibt das Drei-Ebenen-**Geschäftsmodell** (Creator/Vater/Kind) — Admin ist dort
   kein vierter Player, sondern ein quer liegendes Break-Glass-Flag ohne Produktrolle (kein Konto lebt heute
   mit `IsAdmin = true`). Ihn dort als vierte Zeile zu führen würde das Modell falsch darstellen, das
   `CLAUDE.md` ausdrücklich „Drei Ebenen" nennt. `wiki/02-authentifizierung.md` ist laut
   `docs/rollen-doku.md:77` bereits die Landkarte für „Accounts, Rollenclaims, Ownership und
   Anti-Schummel-Regeln" — der fachlich richtige Ort. Zusätzlich bekommt `docs/rollen-doku.md` in seiner
   Tabelle „Übergreifende Orientierung" **eine neue Zeile**, die dorthin verweist — dieser Abschnitt listet
   bereits rollenübergreifende Themen (Authentifizierung, API-Referenz …) und ist der richtige
   Einstiegspunkt für jemanden, der von den drei Rollen-Tabellen kommt, ohne diese selbst um einen vierten
   Eintrag zu erweitern.
   **Kosten:** zwei Dateien statt einer angefasst; wer nur `grundprinzip.md` liest, sieht Admin weiterhin
   nicht — akzeptiert, weil das genau die Trennung ist, die diese Entscheidung will (Geschäftsmodell vs.
   technische Rechte-Referenz).
2. **Admin bleibt ohne API vergebbar** — Status quo, keine Code-Änderung. Begründung: Das ist bereits die
   Absicht (`AdminEntities.cs:53`: „Deliberately not settable through the API – only via DB/seed (no self
   rights escalation)") und wird heute durchgehend eingehalten — kein Controller setzt `IsAdmin`, die
   einzigen Stellen sind zwei Testfixture-Zeilen. Ein API-Vergabepfad für ein Break-Glass-Recht widerspräche
   seinem Zweck (Selbst-Eskalation wäre dann ein Feature, kein Versehen). Der neue Absatz aus Entscheidung 1
   macht diese Absicht nur **sichtbar**, statt sie zu ändern.
   **Kosten:** keine — reine Dokuklarstellung.
3. **Die Breite bleibt, wird aber als Warnung mit Präzedenzfall festgeschrieben**, statt die Rolle in
   engere Rechte zu zerlegen. Begründung: Der Anmerkungen-Präzedenzfall zeigt, dass Admin als generische
   Sichtbarkeits-Bedingung zu breit war — die Antwort darauf ist dort aber bereits gefunden
   (`RemarkOptions.GlobalRead`, ein eigener, engerer Schalter), nicht eine Zerlegung von `Roles.Admin`
   selbst. Eine echte Rechte-Zerlegung (z. B. getrennte Flags für „RWX umgehen" vs. „alle Anmerkungen
   lesen") wäre ein Wunsch mit Code-Aufwand und hat heute keinen zweiten Bedarfsfall außer dem bereits
   gelösten. Der neue Absatz trägt darum die **Handlungsanweisung** „für ein neues Privileg einen eigenen,
   engen Schalter bauen (Muster `RemarkOptions.GlobalRead`), nicht `Roles.Admin` erweitern oder
   wiederverwenden" statt eines Umbaus.
   **Kosten:** die Breite bleibt technisch unverändert; ein zweiter Bedarfsfall, der denselben Fehler
   wiederholen will, wird nur durch gelesene Doku abgefangen, nicht durch ein Code-Gate. Für eine
   Aufräumen-Story bewusst akzeptiert — ein mechanisches Tor dafür wäre eine eigene, größere Story.

## Akzeptanzkriterien

1. `wiki/02-authentifizierung.md` enthält einen eigenständigen Abschnitt zu `Roles.Admin`, der mindestens
   benennt: Zweck (Break-Glass-Superuser), Vergabe (nur `Adult.IsAdmin` über DB/Seed, ausdrücklich keine
   API), Wirkung (Bypass der RWX-Prüfung an Übungen, `ExercisePermissionService.cs:24/34/46`), den
   JWT-Fallstrick (ein frisch gesetztes `IsAdmin` wirkt erst nach neuer Anmeldung) und die
   Handlungsanweisung aus Entscheidung 3 (neues Privileg → eigener enger Schalter statt Admin), belegt mit
   einem Verweis auf den Präzedenzfall in `docs/anmerkungen-plan.md`.
2. `docs/rollen-doku.md` verlinkt in der Tabelle „Übergreifende Orientierung" auf diesen neuen Abschnitt —
   die drei Rollen-Tabellen (Creator/Supervisor/Student) bleiben unverändert bei drei Einträgen.
3. `docs/grundprinzip.md` bleibt unverändert: die Drei-Ebenen-Aussage bleibt so, wie sie ist — kein vierter
   Eintrag.
4. `markdownlint-cli2` bleibt für beide geänderten Dateien grün (CI-Job „Markdown-Lint").

## Schätzung

**Größe: XS** — reines Dokuwerk ohne Codepfad: ein neuer Absatz plus eine Tabellenzeile, vergleichbar mit
dem XS-Anker „zwei Sätze in `lib/fieldHelp.ts`" (B-02). `wo: doku`, `migration: nein` (kein Schema
berührt), `vertragsbruch: nein` (kein `Pugling.Contracts`-Typ berührt).

**Risiken:** gering. Das einzige Risiko ist eine Formulierung, die Admin fälschlich als vierte
Produktebene lesen lässt — dagegen steht die explizite Abgrenzung aus Entscheidung 1, die im neuen Absatz
selbst benannt wird.

**Angriffsplan** (reine Doku, kein Backend/Frontend-Unterschied):

1. Absatz in `wiki/02-authentifizierung.md` schreiben, mit den in AK 1 genannten Punkten und den oben
   gesammelten Code-Belegen zitiert.
2. Zeile in `docs/rollen-doku.md` → „Übergreifende Orientierung" ergänzen.
3. `markdownlint-cli2` lokal für beide Dateien laufen lassen.

**Testweg:** kein Integrationstest/E2E nötig (reine Markdown-Doku ohne Code-Änderung) — Prüfweg ist
`markdownlint-cli2` (CI-Job „Markdown-Lint") plus eine manuelle Gegenprüfung der zitierten Zeilen gegen den
aktuellen Code (`AuthAccess.cs:35-38`, `AdminEntities.cs:50-55`, `TokenService.cs:27-41`,
`ExercisePermissionService.cs:24/34/46`, `AuthController.cs:59/102-103`, `docs/anmerkungen-plan.md:386-391`).

## Verlauf

- **2026-08-01** — angelegt (Quelle: Rollen-Durchgang; die Doku-Lücke ist geprüft, die Frage nach dem
  Zuschnitt der Rolle nicht).
- **2026-08-03** — ausformuliert: gegen den Code geprüft (`AuthAccess.cs`, `AdminEntities.cs`,
  `TokenService.cs`, `ExercisePermissionService.cs`, `AuthController.cs`, `RemarksController.cs`,
  `Data/Seed.cs`). Bestätigt: die drei Rollen-Dokumente kennen `Roles.Admin` tatsächlich nicht; die Rolle
  selbst ist im Code über XML-Docs bereits dicht dokumentiert, und `docs/anmerkungen-plan.md` trägt einen
  belegten Präzedenzfall (Admin als Bedingung verworfen). Kein Seed-Konto trägt heute `IsAdmin = true` —
  nur zwei Testfixtures setzen es direkt auf der DB.
- **2026-08-03** — gegrillt (autonom getroffen, Nutzerauftrag 2026-08-04): drei Entscheidungen —
  Platzierung in `wiki/02-authentifizierung.md` plus Verweiszeile in `rollen-doku.md`, ohne
  `grundprinzip.md` anzufassen; API-Vergabe bleibt aus; die Breite bleibt, wird aber als Warnung samt
  Präzedenzfall festgeschrieben statt zerlegt.
- **2026-08-03** — geschätzt (autonom getroffen, Nutzerauftrag 2026-08-04): Größe XS, `wo: doku`,
  `migration: nein`, `vertragsbruch: nein`, Testweg `markdownlint-cli2` plus manuelle Gegenprüfung der
  Code-Belege.
