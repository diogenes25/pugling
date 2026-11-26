---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/supervisor, rolle/student]
aliases: [Lob-Kanal, Lob schicken]
status: abgenommen
prio: P3
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: docs/baobab-vergleich-plan.md#wo-baobab-weiter-ist
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-377 · Ein Lob-Kanal: der Vater schickt Lob, und Erfolge des Kindes erreichen den Vater

Baobab verspricht Eltern Entlastung: ermutigen und gratulieren statt kontrollieren. Unser Vater-Web
zeigt vor allem Pflicht, Zielverfehlung und Malus; einen Weg, das Kind für etwas Gelungenes zu loben,
gibt es nicht. Die Idee: der Supervisor schickt dem Kind aus einer Erfolgszeile heraus ein kurzes Lob,
das die Sohn-Arcade beim nächsten Öffnen zeigt, und das Vater-Dashboard hebt Erfolge eigens hervor.
Ergänzt die Kernidee „Pflicht mit Konsequenz", ohne sie aufzugeben.

## User Story

Als **Vater** möchte ich meinem Sohn direkt aus der Zeile „✓ geschafft" ein kurzes Lob schicken und sehen, ob er
es gelesen hat, damit er neben Pflicht und Malus auch spürt, dass Gelungenes bemerkt wird.
Als **Sohn** möchte ich beim nächsten Öffnen der App sehen, wer mich wofür gelobt hat – einmal, ohne dass es mich
beim Lernen aufhält.

## Ist-Stand am Code

- **Kein Lob-Kanal.** Kein Modell, kein Endpunkt, keine Oberfläche für eine Nachricht Supervisor → Kind; die
  einzigen Treffer zu „Lob" sind das automatische Antwort-Lob der Arcade (`frontend/src/sohn/Lektion.tsx:193-207`,
  `LOB`-Schlüssel `lektion.praise1…4`). Geprüft 2026-11-14 und erneut 2026-11-15.
- **Der Erfolg des Tages ist schon berechnet und sichtbar.** `GET supervisor/children/daily-overview`
  (`Controllers/Supervisor/ChildrenDashboardController.cs:13`, Aggregation `Services/Supervisor/ChildrenDashboardService.cs:17-50`)
  liefert je Kind `dutyDone`; `frontend/src/vater/VaterDashboard.tsx:53-75` zeigt ihn als Pill „✓ geschafft"
  (`:64`). Das ist die Erfolgszeile, aus der heraus gelobt wird.
- **Andere Erfolge liegen verstreut.** Missionen/Auszeichnungen je Kind unter
  `supervisor/children/{childId}/missions|achievements` (`Controllers/Supervisor/MissionsController.cs:18`, `:96`),
  Zeitstempel `AwardedAt`/`EarnedAt` (`Models/GamificationEntities.cs:53`, `:85`); einen gemeinsamen Erfolgs-Feed
  gibt es nicht (vgl. [B-39](B-39-supervisor-dashboard.md) Entscheidung 2, „Tätigkeits-Zeitleiste" bewusst draußen).
- **Muster für die Bausteine:** kindbezogene Supervisor-Ressourcen unter `supervisor/children/{childId}/…` mit
  `ChildOwnershipFilter` (Konvention `backend/CLAUDE.md` → „Eigentum"); Sohn-eigene Ressourcen unter
  `student/me/…` mit `[Authorize(Roles = Roles.Student)]` (`Controllers/Student/MeController.cs:18`, `:21`).
  Mehrere Supervisor je Kind über `SupervisorLink` (`Models/AdminEntities.cs`), jeder mit eigenem Namen am `Adult`
  (`AdminEntities.cs:43`).
- **Schema-Tore:** eine neue Tabelle braucht je FK eine abgezeichnete Löschregel (G2,
  `Pugling.Api.Tests/SchemaGuardTests.cs:173`) und eine String-Länge (G3, `:363`); die Migration wird an die Kette
  gehängt (letzte: `Data/Migrations/20261114114551_DailyBoxOpenedAt.cs`).

## Die echte Lücke

Eine kleine Nachrichten-Tabelle mit „gelesen"-Stempel, ein Schreib- und ein Lese-Endpunkt je Seite, ein Knopf in
der Erfolgszeile und ein einmaliges Overlay in der Arcade. Für „Erfolge erreichen den Vater" fehlt kein Datum –
nur die Hervorhebung der Erfolgszeile und die Rückmeldung „gelesen".

## Offene Punkte

~~Lob nur Text oder auch Münzen?~~ → Entscheidung 1 (Nutzer-Delegation 2026-11-05).
~~Mehrere Supervisor getrennt sichtbar?~~ → Entscheidungen 2 und 7.
~~Freitext oder vorgefertigte Sätze?~~ → Entscheidung 3 (Nutzer-Delegation 2026-11-05) und 6.
~~Wie erreichen Erfolge den Vater – kleinster ehrlicher Schnitt?~~ → Entscheidungen 4 und 8.
~~Wann gilt ein Lob als gesehen, was bei mehreren ungelesenen?~~ → Entscheidung 5.

## Entscheidungen

1. **Lob ist Text ohne Münzen.** *Entscheidung im Auftrag des Nutzers vom 2026-11-05* (siehe Verlauf),
   unverändert übernommen. Begründung: kein neuer Münzweg, die Wallet-Concurrency-Regel bleibt unberührt; Münzen
   schenkt der Vater weiter über `POST children/{}/points`. Kosten: keine.
2. **Jedes Lob trägt den Namen des Supervisors; die Arcade zeigt es einmal beim nächsten Öffnen.** *Entscheidung
   im Auftrag des Nutzers vom 2026-11-05*, unverändert übernommen.
3. **Kurztext mit Obergrenze plus vorgefertigte Sätze.** *Entscheidung im Auftrag des Nutzers vom 2026-11-05*,
   unverändert übernommen; die Obergrenze setzt Entscheidung 6.
4. **Das Vater-Dashboard hebt Erfolge hervor.** *Entscheidung im Auftrag des Nutzers vom 2026-11-05*; den Umfang
   schneidet Entscheidung 8.
5. **Gesehen heißt: der Sohn hat das Overlay mit „Danke!" geschlossen.** Das Overlay zeigt alle ungelesenen Lobe auf
   einmal (neueste zuerst, höchstens 5; weitere bleiben ungelesen und folgen beim nächsten Öffnen), ein Klick stempelt sie per
   `POST student/me/praise/seen` mit den gezeigten Ids. Scheitert der Stempel, erscheint das Overlay beim nächsten
   Öffnen wieder. Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-15). Begründung:
   „einmal" (Entscheidung 2) braucht einen Server-Stempel, sonst zeigt jedes Gerät es neu; ein Klick statt je Lob
   einer, damit es den Sohn nicht aufhält. Kosten: ein sechstes Lob wartet bis zum nächsten Öffnen –
   akzeptiert, fünf Lobe zwischen zwei Logins sind schon viel.
6. **Obergrenze 140 Zeichen, nur Klartext, vier vorgefertigte Sätze im Frontend.** Der Server prüft getrimmt
   1–140 (`400 validation_error`); die Arcade rendert den Text als reinen Text (kein Markdown/HTML), die
   vorgefertigten Sätze sind deutsche Produkt-Strings im Vater-Web und landen als gewöhnlicher Text. Stakeholder-Entscheidung im Auftrag des Nutzers.
   Begründung: Kindersicherheit hängt hier nicht am Inhalt (schreiben darf nur
   ein Supervisor des Kindes, also ein Elternteil), sondern daran, dass nichts als Link/Markup wirkt; 140 passt
   in die Konventions-Länge 200 (G3) ohne Ausnahme. Kosten: keine Emojis-Auswahl, keine Formatierung.
7. **Mehrere Supervisor: der Sohn sieht jedes Lob mit Absendername; jeder Supervisor sieht in seiner Liste nur die
   eigenen gesendeten Lobe samt „gelesen am".** Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: die
   Wallet ist geteilt, der Shop ausstellergebunden – das Lob folgt dem Shop (persönlich je Absender); eine
   gemeinsame Liste aller Eltern wäre eine zweite Ansicht ohne gezeigten Bedarf. Kosten: Mutter sieht nicht, dass
   der Vater heute schon gelobt hat.
8. **Kleinster ehrlicher Schnitt für „Erfolge erreichen den Vater": die vorhandene Erfolgszeile.** In der
   Tabelle „Heute" bekommt eine Zeile mit `dutyDone` eine Hervorhebung (Pill bleibt, Zeile hebt sich ab) und den
   Knopf „👏 Loben" als Hauptknopf; in anderen Zeilen steht er zurückhaltend (loben darf man auch fürs Dranbleiben).
   Die Rückmeldung „gelesen" in der eigenen Lob-Liste schließt den Kreis. **Zurückgestellt:** ein Erfolgs-Feed
   aus Missionen, Auszeichnungen und Klausuren – das ist die Tätigkeits-Zeitleiste, die B-39 (Entscheidung 2)
   bewusst ausgeklammert hat, und bräuchte einen Aggregat-Endpunkt über drei Tabellen. Stakeholder-Entscheidung
   im Auftrag des Nutzers. Begründung: liefert die Rolle des Vaters (sehen + reagieren) in einem Sprint, ohne
   neues Aggregat. Kosten: eine bestandene Klausur löst keine eigene Hervorhebung aus.
9. **Kein Push, keine Benachrichtigung außerhalb der App.** Das Lob wartet auf das nächste Öffnen (Entscheidung 2).
   Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: PWA-Push wäre ein eigenes Vorhaben. Kosten: keine.

## Akzeptanzkriterien

1. `POST api/v1/supervisor/children/{childId}/praise` mit `{ text }` legt ein Lob an (`SupervisorId` = Aufrufer,
   `CreatedAt` jetzt, `SeenAt` leer) und liefert `201`. Ein nicht betreutes Kind → `404` (Ownership-Filter), ein
   Kind-Login → `403`. *Rot, wenn* `ChildOwnershipFilter` oder das Rollen-Gate fehlt (Test mit zweitem Vater).
2. Text leer/nur Leerzeichen oder über 140 Zeichen nach dem Trimmen → `400 validation_error`; genau 140 → `201`.
   *Rot, wenn* die Grenze fehlt oder vor dem Trimmen zählt (Test: 140 Zeichen plus Randleerzeichen ⇒ `201`).
3. `GET api/v1/supervisor/children/{childId}/praise` liefert nur die **eigenen** Lobe an dieses Kind, neueste zuerst,
   mit `seenAt`, paged mit `X-Total-Count`. *Rot, wenn* das Lob eines zweiten Supervisors desselben Kindes erscheint.
4. `GET api/v1/student/me/praise` liefert die ungelesenen Lobe des eingeloggten Sohns (neueste zuerst, höchstens 5)
   mit `supervisorName`; ein Supervisor-Login → `403`. *Rot, wenn* gelesene Lobe oder Lobe eines anderen Kindes
   erscheinen.
5. `POST api/v1/student/me/praise/seen` mit `{ ids }` stempelt `SeenAt` nur an eigenen, noch ungelesenen Loben
   (fremde Ids werden ignoriert, ein zweiter Aufruf ändert den Stempel nicht) und liefert `204`. *Rot, wenn* ein
   fremdes Lob gestempelt wird oder der zweite Aufruf `SeenAt` überschreibt.
6. Keine Münz- oder Gem-Buchung entsteht durch Lob (Saldo vorher = nachher). *Rot, wenn* jemand an den Lob-Pfad
   eine `ChildPointsEntry` hängt.
7. Vater-Web: in „Heute" trägt jede Zeile „👏 Loben"; eine Zeile mit `dutyDone` ist hervorgehoben und der Knopf dort
   Hauptknopf; das Panel bietet vier Sätze und ein Feld mit `maxLength` 140, Senden ist bei leerem Feld gesperrt.
   *Rot (Vitest `VaterDashboard.test.tsx`), wenn* die Hervorhebung an `practiced` statt `dutyDone` hängt oder
   Senden leer freigegeben ist.
8. Arcade: `SohnHome` zeigt bei ungelesenen Loben einmal ein Overlay mit Absendername und Text (Text als
   Platzhalter, nicht durch `t()` übersetzt); „Danke!" stempelt und schließt. *Rot (Vitest `SohnHome.test.tsx`),
   wenn* das Overlay bei leerer Liste erscheint oder ohne Stempel-Aufruf schließt.
9. Die Migration hängt an die Kette, legt nur die neue Tabelle an und lässt die echte `pugling.db` starten (Probe
   gegen eine Kopie); `SchemaGuardTests` grün mit je einer abgezeichneten Zeile für die zwei FKs. *Rot, wenn* die
   Löschregel nicht abgezeichnet ist (G2) oder das Modell driftet (G1).

## Schätzung

**Größe: M** – eine neue Tabelle mit Migration, vier kleine Actions, ein Panel im Vater-Web, ein Overlay in der
Arcade. Vergleich: in der Breite wie B-538 (M), aber mit Migration; kleiner als eine DB-Umbau-Etappe (L), weil die
Migration nur anlegt und keine Bestandsdaten umformt.

- **`migration: ja`** – neue Tabelle `Praises` (`Id`, `ChildId` → `Child` Cascade, `SupervisorId` → `Adult` Cascade,
  `Text` ≤ 200 per Konvention, `CreatedAt`, `SeenAt?`), Index auf `(ChildId, SeenAt)`. Keine Bestandszeilen, also
  kein Daten-SQL; trotzdem Probe gegen eine Kopie der echten DB.
- **`vertragsbruch: nein`** – nur neue DTOs (`CreatePraiseDto`, `PraiseResponse`, `MyPraiseResponse`,
  `MarkPraiseSeenDto`) und neue Routen.

### Risiken

- **Zwei Cascade-Pfade auf eine Tabelle** (Kind und Adult). SQLite verträgt das; G2 verlangt je FK eine bewusste
  Zeile in der Liste – vor dem Build eintragen, sonst ist das Tor rot.
- **`EndpointCoverageGuard`** (`Pugling.Api.Tests/EndpointCoverageGuard.cs:30`, 282) zählt vier neue Actions.
- **Doku-Capture:** `CreatedAt`/`SeenAt` in `DocsCaptureTests`-Beispielen maskieren, sonst ändert jeder Lauf die
  Datei (Memory „Doku-Capture byte-stabil").
- **Arcade-Startweg:** das Overlay darf `SohnHome` nicht blockieren, wenn `GET me/praise` scheitert – Fehler still
  schlucken (Lob ist kein Pflichtweg), aber keine Endlosschleife beim Stempeln.
- **i18n:** Overlay-Rahmen („Lob von {name}", „Danke!") in `src/i18n/de.ts` und `en.ts` (Parität per `i18n.test.ts`);
  der Lobtext selbst reist als Platzhalter.

#### Angriffsplan (Backend zuerst, API-First)

1. Modell `Praise` (`Models/GamificationEntities.cs` oder eigene Datei), `DbSet`, G2/G3-Zeilen, Migration
   `AddPraise` anhängen, Snapshot-Diff lesen.
2. Contracts-DTOs; `PraiseController` unter `supervisor/children/{childId}/praise` (`Roles.Supervisor`,
   `ChildOwnershipFilter`), zwei Actions in `MeController` (`me/praise`, `me/praise/seen`).
3. Integrationstests (AK 1–6), Client-Zeilen in `Pugling.Client`, OpenAPI, `contract.ts`.
4. Frontend Vater: Panel in der „Heute"-Zeile (`useAction`, `disabled={busy || !text.trim()}`), Hervorhebung bei
   `dutyDone`, eigene Lob-Liste mit „gelesen am" im Kind-Hub-Bereich des Dashboards.
5. Frontend Sohn: Overlay in `SohnHome.tsx`, Stempel beim Schließen.
6. Probe der Migration gegen eine Kopie der echten DB (`scripts/heimnetz.ps1 -Database <kopie> -Port <anderer>`).

##### Testweg

- Integration: neue Klasse `backend/Pugling.Api.Tests/PraiseTests.cs` (AK 1–6, je Fall die rote Probe oben);
  `SchemaGuardTests` (AK 9).
- Vitest: `frontend/src/vater/VaterDashboard.test.tsx` (AK 7), `frontend/src/sohn/SohnHome.test.tsx` (AK 8).
- E2E: neue Spec `frontend/e2e/lob.spec.ts` (eine Fläche): Vater lobt aus „Heute" → Sohn loggt ein, sieht das Lob
  mit Vaternamen, „Danke!" → erneutes Öffnen ohne Overlay → Vater sieht „gelesen". Zählt als Rollengang.
- Reviewer: `pugling-reviewer` und `frontend-reviewer` (`wo: beides`).

## Verlauf

- **2026-11-05** — angelegt (Quelle: Baobab-Vergleich, Etappe 2 des Übernahmeplans).
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): (1) Lob ist Text ohne Münzen; (2) jedes Lob trägt den Namen des Supervisors, die Arcade zeigt es einmal beim nächsten Öffnen; (3) Kurztext mit Obergrenze plus vorgefertigte Sätze; (4) das Vater-Dashboard hebt Erfolge hervor. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt – kein Lob-Kanal Supervisor→Kind im Code; die einzigen Treffer zu „Lob“ sind das automatische Antwort-Lob der Arcade (`frontend/src/sohn/Lektion.tsx:194`, `i18n/de.ts:316`), kein Supervisor-Endpunkt.
- **2026-11-15** — Backlog-Lauf 2026-11-15: `ausformuliert` – Ist-Stand erneut belegt (kein Lob-Modell; Erfolg des
  Tages schon als `dutyDone` in „Heute"; Missionen/Auszeichnungen ohne gemeinsamen Feed), User Story, Lücke.
- **2026-11-15** — Backlog-Lauf 2026-11-15: `gegrillt` – Entscheidungen 1–4 aus der Delegation vom 2026-11-05
  unverändert übernommen; 5–9 als Stakeholder-Entscheidung im Auftrag des Nutzers (gesehen = „Danke!" mit
  Server-Stempel, höchstens 5; 140 Zeichen Klartext; jeder Supervisor sieht nur eigene Lobe, Sohn alle mit Namen;
  Erfolg = vorhandene `dutyDone`-Zeile, Erfolgs-Feed zurückgestellt; kein Push).
- **2026-11-15** — Backlog-Lauf 2026-11-15: `geschaetzt` – M, beides, Migration ja (neue Tabelle, nur anlegen),
  kein Vertragsbruch.
- **2026-11-25** — `geschaetzt` → `in-arbeit` (Nachtlauf N, Sprint 181, [Protokoll](../pm-sitzung-2026-11-25-n.md)). Gebaut: Tabelle
  `Praises` + Migration `20261125214724_Praise`, `PraiseService`, `PraiseController`, zwei Actions im `MeController`, „👏 Loben“ mit
  `LobPanel` in „Heute“, `LobOverlay` auf `SohnHome`. Je Fall der Test und die Mutante (gemessen mit `mutant-probe.py`):
  - AK 1, anlegen/Ownership/Rolle → `PraiseTests.Lob_Wird_Angelegt_Und_Bucht_Nichts`; P1 (Ownership-Filter weg), P2 (Rollen-Gate gelockert) rot.
  - AK 2, Trimmen und Grenze 140 → `Text_Wird_Getrimmt_Und_Auf_140_Zeichen_Begrenzt`; P3 (zählt vor dem Trimmen), P4 (keine Obergrenze) rot.
  - AK 3, nur eigene, neueste zuerst, `X-Total-Count` → `Jeder_Sieht_Nur_Seine_Lobe`; P5 (fremder Betreuer sichtbar) rot.
  - AK 4, ungelesen, höchstens 5, Reihenfolge, kein Geschwister → `Jeder_Sieht…` / `Gesehen_Stempelt…`; P6 (gelesene erscheinen), P7 (kein Deckel), P8 (Reihenfolge) rot.
  - AK 5, nur eigene, Stempel einmal → `Gesehen_Stempelt_Nur_Eigene_Und_Nur_Einmal`; P9 (fremdes Lob gestempelt), P10 (Stempel überschrieben) rot.
  - AK 6, keine Buchung → `Lob_Wird_Angelegt…` (Anzahl, Summe, Stamp); P11 (Ledger-Zeile am Lob-Pfad) rot.
  - AK 7 → `VaterDashboard.test.tsx` „Loben aus Heute“ (drei Fälle); F1 (`practiced` statt `dutyDone`), F2 (Senden bei Leerzeichen), F3 (kein `maxLength`) rot.
  - AK 8 → `sparzielLob.test.tsx` (fünf Fälle); F4 (Overlay bei leerer Liste), F5 (schließt ohne Stempel), F6 (bleibt nach gescheitertem Stempel offen), F7 (Text durch `t()`), F8 (Escape stempelt) rot.
  - AK 9 → `SchemaGuardTests` grün mit den zwei abgezeichneten FK-Zeilen; Migrationsprobe an der Kopie der echten DB (s. u.).
- **2026-11-26** — Verifikation wie bei [B-593](B-593-sparziel-im-familien-shop.md) (gemeinsamer Sprint). Rollengang an der Kopie der echten DB:
  Papa lobt Friedrich (Text mit Randleerzeichen → getrimmt), Friedrich liest es mit „Papa“, „Danke!“ → Liste leer, Papa liest
  `seenAt`; Kontostand vorher = nachher (2070/186). Maschinell: `frontend/e2e/lob.spec.ts` (Vater → Sohn → Vater, „gelesen am“).
  Review-Funde eingearbeitet: Fokus-Falle des Dialogs (`useDialogFocus`), `useAction` im Overlay, `<Pflicht />` am Feld.
- **2026-11-26** — `in-arbeit` → `abgenommen`: Commit `6a0257dd`; Rollengang an der Kopie der echten DB (Sohn Friedrich, Vater) und maschinell per Playwright, Creator Regressionszeuge; beide Reviews ohne Blocker. Offen nur ein Blick am Gerät (Overlay bzw. Streifen am Handy), kein Abnahmehindernis.
