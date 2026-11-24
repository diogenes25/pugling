# PM-Sitzung: Nachtlauf J – Hausaufgaben

**Datum:** 2026-11-24  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Das Paket Hausaufgaben (B-760, B-761, B-762, B-763) so weit bauen, wie seine Entscheidungen tragen – Backend vor
Frontend. Einer von drei parallelen Nachtläufen (Worktree `nachtlauf-j`, Branch `nachtlauf/2026-11-24-j`, Basis `5993c86b`).

## Runde 1 — Vorstellung & Feedback

Kein Rollengang zu Beginn: Das Paket ist vorgegeben, die Stimmen stammen aus der Quelle
[B-687](backlog/B-687-hausaufgabe-mit-foto-nachweis.md) (Chat mit dem Vater am 2026-11-19, Friedrichs Mathe-Hausaufgabe per
Foto nachgeprüft). Unter `docs/anmerkungen/` liegt kein neuer Befund.

### Feedback Vater (aus B-687)

- Stört: Die Schul-Hausaufgabe steht außerhalb der App; ob sie gemacht ist, sieht er nur am Heft.
- Top-3: Hausaufgabe anlegen, Abgabe sehen, ohne Abgabe zur Frist ein Malus wie bei jeder Pflicht.

### Feedback Sohn / Creator

- Sohn: keine eigene Stimme in der Quelle. Der Hebel sind Münzen (Gedächtnis „Friedrichs Motivation“).
- Creator: nicht betroffen.

## PM-Synthese & Priorisierung (→ Entwickler)

| Story | Art / Stufe | baubar? | Grund |
| --- | --- | --- | --- |
| B-760 Kern (Backend) | Wunsch / `geschaetzt` | ja (Freigabe 8) | Entscheidungen 1–5 tragen den Bau vollständig |
| B-761 Foto-Ablage (Backend) | Wunsch / `geschaetzt`, `wartet_auf` | **nein** | Ablageweg mit B-684 (`idee`) nicht entschieden |
| B-762 Vater-Web | Wunsch / `geschaetzt` | **nein** (siehe Halt) | AK 2 hängt an B-761; allein gebaut erzeugt sie Hausaufgaben, die das Kind nicht abgeben kann |
| B-763 Sohn-App | Wunsch / `geschaetzt` | **nein** (siehe Halt) | Abgabe = Foto-Upload (B-761); eine Abgabe ohne Foto ist nicht entschieden |

**Roter Faden:** Erst der Kern, der Geld bewegt (B-760), weil jede Oberfläche an ihm hängt.

**Anmerkung zur Herkunft der Entscheidungen:** B-760 bis B-763 tragen „stellvertretend entschieden im Vorbereitungslauf
2026-11-20 (Sonnet) auf Anweisung des Nutzers, unbestätigt“. Ein `wartet_auf: Bestätigung …` steht an keiner der vier, und
der Auftrag dieses Laufs nennt das Paket ausdrücklich; gebaut wird darum nach Freigabe 8. Wo eine Entscheidung fehlte, steht sie
unten unter „Offene Entscheidungen“ statt im Code.

## Sprint 141 — Ziel & Umfang

**Sprint-Ziel:** Der Vater legt über die API eine Hausaufgabe mit Frist an; gibt das Kind bis zum Ende des Fälligkeitstags
nichts ab, kostet sie beim nächsten Login oder Kauf genau einmal den Malus des Kindes, und nimmt der Vater eine Abgabe an,
bekommt das Kind genau einmal seine Münzen.
**Umfang:** B-760.
**Entwickler-Brief:** Senke für den Malus ist ein neuer, kleiner Abrechnungspfad `HomeworkService.SettleAsync` neben
`SettleClosedPeriodsAsync`, aufgerufen an denselben zwei Nähten (Kind-Login, Kauf vor der Kontoprüfung). Idempotenz über den
eindeutigen Schlüssel `HomeworkPenalty.HomeworkId`, Wallet-Bump an jedem buchenden Weg (Malus und Annahme). Rückblick über die
gemeinsame Konstante `PositionProgressService.MaxSettleLookbackDays` (nicht kopiert). „Rechtzeitig angelegt“ und „rechtzeitig
abgegeben“ zählen im Lerntag (`LearningDay.DayOf`). Neue Münzart `PointKind.HomeworkCoins` (zählt als verdient, ohne
Sonderliste). Zwei nullbare Beträge am Kind, Vorgabe aus `Gamification:Homework`. Migration `Homework` an die Kette.
Testweg: `HausaufgabenTests`, `HausaufgabenMigrationTests`, Schema-/Patch-/Ownership-Tore.

## Iteration 141 — umgesetzt

- **Backend (B-760):** Entitäten `Homework`, `HomeworkSubmission`, `HomeworkPenalty` (eindeutig je Hausaufgabe), zwei
  nullbare Beträge am Kind (`HomeworkCoins`, `HomeworkPenaltyCoins`, PATCH mit `Clear…`-Schaltern), `PointKind.HomeworkCoins`.
  `HomeworkService`: anlegen, abgeben, annehmen (nur aus `Submitted`, bucht einmal, Wallet-Bump), Nacharbeit mit Notiz,
  löschen, abrechnen. Routen `supervisor/children/{childId}/homework` (Liste mit `take`, anlegen, `…/accept`, `…/rework`,
  löschen) und `student/homework` (Liste mit `take`, `…/{id}/submissions`). Abrechnung im Kind-Login und vor der
  Kontoprüfung des Kaufs. Neue 409-Codes `homework_accepted`, `homework_not_submitted` (deutsche Texte im Vater-Web).
- **Migration:** `20261124201850_Homework` (angehängt an `20261124131013_ClassTestPrepLeftOut`; beim Merge ggf. neu zu
  erzeugen, wenn ein paralleler Lauf ebenfalls anhängt).
- **Frontend (nur Vertrag):** `labels.ts` und i18n de/en kennen `HomeworkCoins`; keine Oberfläche (B-762/B-763 angehalten).
- **Client:** je Route eine Methode in `SupervisorApi`/`StudentApi`.
- **Verifikation:** .NET 2357/2357 (Release, ganze Lösung, nach dem letzten Code-Edit außer dem Titel-Text unten), Vitest
  1441/1441, `tsc -b` sauber nach Neuerzeugen des Vertrags, `dotnet format Pugling.sln` ohne Änderung. Mutanten-Probe nach dem
  letzten Code-Edit: 24 Zeilen, 21 rot am benannten Test, 3 kompilierten nicht und wurden als M18b/M19b/M22b neu gemessen (rot);
  Tabelle in B-760. E2E: keine (keine Oberfläche in diesem Sprint).
- **Review:** `pugling-reviewer` – ein 🟡 (Annahme aus `Rework` konnte mit einer gleichzeitigen Abgabe zur zweiten Buchung
  führen; behoben: Annahme nur aus `Submitted`, Test und Mutant M13b), 🟢 behoben ohne Zählung: fehlendes `dueOn` → 400
  (M22b), `FirstAsync` → `FirstOrDefaultAsync` bei gelöschtem Kind. 🟢 zurückgestellt ohne Zählung: Betrag 0 schiebt den Malus
  auf statt ihn zu erlassen (wie bei der Pflicht, siehe Offene Entscheidungen); `auth/login` rechnet nicht ab (alt, betrifft auch
  die Pflicht; die Sohn-App meldet sich über `auth/child` an, `frontend/src/lib/api.ts:370` – darum keine Story); keine
  Obergrenze für Abgaben (nach B-761 verschoben); AK-6-Test vergleicht rohe Antworten (das Feld `day` kippt nur um Mitternacht);
  Migrations-Reihenfolge beim Merge.

**Fehlerzähler Sprint 141: 4** (Grenze 5) – (1) Tor „unpaginierte Array-GETs“ rot → `take` an beiden Listen; (2) eigener
Test erwartete 409 statt `400 insufficient_coins` beim Kauf; (3) Review 🟡 Annahme aus `Rework`; (4) Abdeckungs-Pin
`FullRunTouchedActions` 308 → 315 (das Tor meldet sich nur im Bericht `TestResults/endpoint-coverage.txt`, nicht als roter
Test – siehe Retro). Ohne Zählung: drei 🟢 (oben), Titeltext von `homework_not_submitted` an die neue Bedeutung angepasst.

## Runde 141 — Re-Review / Abnahme

Server: nach dem letzten Code-Edit an einer frischen Kopie der echten DB gestartet (`kopie-server.sh … 5212`), die Migration
`Homework` lief dabei ein (Migrationsprobe an der Kopie), danach beendet. Kein Browser-Rollengang: es gibt keine Oberfläche.

- **Vater – abgenommen an der API:** „Ich lege für den Testjungen ‚Mathe S. 42 Nr. 1-3‘ für heute an, gebe die Abgabe mit
  ‚Nr. 3 fehlt‘ zurück, Annehmen ohne neue Abgabe sagt mir, worauf es wartet; nach der zweiten Abgabe nehme ich an und er hat
  20 Münzen mehr (2640 → 2660), ein zweites Annehmen geht nicht.“ Nach der Migration standen alle Konten wie vorher:
  Theodor 275, Friedrich 2138, Testjunge 2640 (vorher aus der echten DB nur lesend summiert), beide Beträge am Kind leer.
- **Sohn – abgenommen an der API:** sieht beide Hausaufgaben mit „bringt 20, sonst −2“, gibt ab, liest die Notiz, die Frist
  bleibt; „diese Woche“ und die Buchung „[Hausaufgabe] Mathe S. 42 Nr. 1-3 angenommen“ zeigen die 20. Theodor kann die
  Hausaufgabe des Testjungen nicht abgeben (404). Ein Nachtrag mit Frist gestern kostete beim Login nichts.
- **Nicht an der Kopie gelaufen:** der Malus selbst – er braucht einen abgelaufenen Fälligkeitstag; belegt durch
  `HausaufgabenTests` (sechs Fälle, Mutanten M3–M9, M18b, M19b).
- **Creator – Regressionszeuge:** kein Creator-Pfad im Diff; Suite grün.

**Stand:** B-760 `abgenommen`. B-761 wartet weiter (`wartet_auf`), B-762 und B-763 angehalten (Entscheidung fehlt).

## Halt nach Sprint 141

Das Paket ist ohne neue Produktentscheidung nicht weiter baubar:

- **B-761** wartet auf den Ablageweg der Fotos (mit B-684, `idee`).
- **B-762** allein gebaut ließe den Vater Hausaufgaben anlegen, die das Kind nicht abgeben kann – jede kostete den Malus.
- **B-763** braucht für die Abgabe das Foto (B-761) oder die Entscheidung, vorerst ohne Foto abzugeben.

Andere Themen nimmt dieser Lauf laut Auftrag nicht.

## Offene Entscheidungen (für den Nutzer)

1. **Ablageweg der Hausaufgaben-Fotos** (B-761 mit B-684): Ort der Dateien (Sicherung im Heimnetz-Skript), Auslieferung hinter
   der Anmeldung (Blob-Abruf mit Token oder kurzlebiger Link), Mechanik und Länge der Löschfrist.
2. **Zwischenstufe ohne Foto?** Dürfen B-762/B-763 vor B-761 kommen, mit „Abgeben“ als einfachem Knopf? Folge: das Kind wendet
   den Malus mit einem Tipp ab, die Prüfung liegt dann ganz beim Vater (Annahme/Nacharbeit). Ohne diese Entscheidung bleibt das
   Paket bis B-761 stehen.
3. **Startbeträge** `Gamification:Homework` = 20 Münzen / 2 Malus (abgeleitet aus einer Vokabel-Tagespflicht, B-760 Risiko 6) –
   bestätigen oder ändern; je Kind über die zwei neuen Felder übersteuerbar (noch ohne Oberfläche).
4. **Betrag 0 = erlassen oder aufgeschoben?** Heute (wie bei der Pflicht): Malus 0 bucht nichts und merkt nichts; hebt der Vater
   ihn später an, werden verpasste Hausaufgaben der letzten 14 Tage nachträglich belastet.
5. **Bestätigung der stellvertretenden Entscheidungen** von B-687 (Vorbereitungslauf 2026-11-20, „unbestätigt“), auf denen
   B-760 jetzt gebaut ist.

## Retrospektive

**Nachschau:** Sprint 140 (B-954, `ddf6d1d6`) – Behauptung „dieselbe Definition wie ‚diese Woche‘“ nachgelesen:
`WeekCoinsAsync` und `EarnedCoinsByDayAsync` gehen beide über `EarnedCoins` (`WalletService.cs:107-109`), der Tag über
`learningDay.DayOf` (`:101`), `ChildrenDashboardService.cs:32` und `:98` lesen nur diese Abfrage. Kein Befund;
`nachgeschaut: 2026-11-24` an B-954 gesetzt. Folge für B-760: `HomeworkCoins` erscheint am Annahmetag auch in „Münzen heute“.

**Was die eigenen Tore durchgelassen haben:** Die Race „Annahme aus Nacharbeit“ fand nur das Review; kein Test und kein Tor
sahen sie, weil die Tests sequenziell laufen. Selbst gefunden: Der Abdeckungs-Wächter (`EndpointCoverageGuard`) blieb bei 315
statt gepinnter 308 still – seine Prüfung läuft im `DisposeAsync` einer Assembly-Fixture, und deren Ausnahme erscheint im
`dotnet test`-Ergebnis nicht als roter Test (die Lösung meldete „Passed! 2357“). Sichtbar war es nur im Bericht
`TestResults/endpoint-coverage.txt`. Zwei volle Läufe dauerten unter der Last dreier paralleler Nachtläufe 20 bzw. 8 Minuten
statt ~1; ein Test lief dabei einmal in eine Zeitüberschreitung (`OpenApi_Schema…`, 3 min 47 s) und war allein grün.

**Vorschlag (nicht gelandet, Freigabe 3):** *J1* – den Abdeckungs-Wächter sichtbar machen: entweder das Ergebnis der
Assembly-Fixture prüfen (ein Test, der den Bericht liest und gegen die Inventur vergleicht) oder im Test-Tor-Hook den Bericht
auf „N offen“ und auf Abweichung vom Pin auswerten. Erst messen, ob xUnit v3 den Fehler wirklich verschluckt (eine
absichtlich offene Action, ein Lauf) – die Beobachtung hier ist ein einziger Lauf.

**Bewusst ungedeckt:** Nebenläufigkeit von Annahme und Abgabe ist nur durch Lesen gesichert (Zustandsprüfung + Stamp), nicht
durch einen Test; die Lerntag-Grenze „Abgabe 23:30 vs. 00:30“ ist nur in UTC gemessen (das Tor `Tage_Entstehen_Nur_Im_Lerntag`
hält den Weg über `LearningDay`).

## Offene Roadmap (priorisiert)

Die Liste, die gilt, ist `docs/backlog/`. Für das Paket Hausaufgaben: erst die Offenen Entscheidungen 1 oder 2 (oben), dann
B-761 oder direkt B-762/B-763. B-760 ist auf dem Branch abgenommen und wird mit dem Merge wirksam (Migration `Homework` spielt
der nächste Heimnetz-Start ein).

## Konkreter Änderungsstand (für Review)

Branch `nachtlauf/2026-11-24-j` (Basis `5993c86b`, ungemergt, ungepusht):

- `788921d6` – Code-Commit B-760: Backend, Vertrag, Client, Migration `20261124201850_Homework`, Tests, generierte Doku,
  Frontend-Beschriftungen.
- danach ein Doku-Commit: B-760 (abgenommen), B-761–B-763 (geprüft, angehalten), B-954 (Nachschau), dieses Protokoll, Index.

Beim Merge: Migration ggf. neu erzeugen (Snapshot), `FullRunTouchedActions` (hier 315) mit den anderen
Läufen zusammenrechnen, `INDEX.md` neu erzeugen. Neue Story-Ids: keine.
