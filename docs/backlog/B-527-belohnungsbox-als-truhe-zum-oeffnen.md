---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student]
aliases: [Truhe auf dem Pfad, Belohnungsbox öffnen]
status: abgenommen
prio: P3
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: ja
quelle: "B-525, Entscheidung 8 (Grill-Runde 2026-11-13)"
unverifiziert: false
grund: ""
nachgeschaut: 2026-11-15
ersetzt_durch: []
wartet_auf: ""
---

# B-527 · Belohnungsbox als Truhe auf dem Pfad, die das Kind selbst öffnet

Die tägliche Belohnungsbox wird heute vom Server von selbst vergeben, wenn eine Runde oder ein Test endet.
Auf dem Lernpfad ([B-525](B-525-sohn-startseite-als-schlangenpfad.md)) soll sie als **Truhe** stehen, die
das Kind antippt und öffnet, wie es das aus Duolingo kennt. Das ändert die Vergaberegel (Anspruch entsteht
beim Abschluss, Gutschrift beim Öffnen) und ist darum mehr als Darstellung. Beim Grillen von B-525 bewusst
abgespalten: eine Truhe, die nur anzeigt, wäre die schwächste Fassung gewesen.

## User Story

Als *Sohn* möchte ich am Ende meines Pfads eine Truhe sehen, die aufleuchtet, sobald ich meine Tagesziele geschafft
habe, und die ich selbst öffne, damit ich die Zufalls-Belohnung erlebe statt sie nur im Kontoauszug zu finden. Als
*Vater* möchte ich, dass gerade dieser Antrieb – Friedrichs stärkster ([B-535](B-535-belohnungen-erreichen-friedrich-nicht.md))
– sichtbar an der Pflicht hängt.

## Ist-Stand am Code

Stand mit B-535 im Worktree (Vergabe an `RewardsUnlocked`).

1. **Vergabe und Gutschrift sind ein Schritt.** `DailyBoxService.EvaluateAndAwardAsync`
   (`backend/Pugling.Api/Services/Shared/DailyBoxService.cs:33-87`) zieht Münzen/Gems zufällig, legt den
   `DailyBoxClaim` an und bucht im selben `SaveChanges` die Ledger-Zeilen `DailyBoxCoins`/`DailyBoxGems` (`:58-77`).
   Gerufen nur an den Schreibnähten Übungsende (`PositionPracticeController.cs:612`) und Testabgabe
   (`PositionTestsController.cs:446`).
2. **Das Kind sieht die Box nirgends.** Die Übersicht liefert `DailyBoxStatus` mit (`PlanOverviewController.cs:39-41`,
   `Pugling.Contracts/Shared/ProgressDtos.cs:52`, `Student/ProgressDtos.cs:10`), aber kein Frontend-Code liest
   `dailyBox` – einzig die E2E `frontend/e2e/belohnung-trotz-wochenziel.spec.ts:65` und die Beschriftung der
   Ledger-Art „Tägliche Belohnungsbox“ (`frontend/src/i18n/de.ts:148-149`). Die Zufalls-Kiste ist für das Kind heute
   eine Zeile im Kontoauszug.
3. **Der Pfad hat keinen Endknoten.** `Pfad` in `frontend/src/sohn/SohnHome.tsx:121`, `:134-170` zeichnet nur
   Positionen; danach folgt der Bonus-Einstieg (`:125`).
4. **Der Saldo ist die Summe des Ledgers** (`WalletService.cs:27`, Währung je Art in `PointKindCurrency.cs:22-26`) –
   was nicht gebucht ist, ist nicht im Saldo.
5. **Leitplanke aus B-105, Entscheidung 7:** keine Ablaufzeit, kein Drängeln (`DailyBoxService.cs:14-17`). Je Kind
   und Tag höchstens ein Claim (eindeutiger Index `PuglingDbContext.cs:852`).

## Die echte Lücke

Die Belohnung, die Friedrich am meisten antreibt, hat kein Erlebnis: kein Gegenstand, kein Moment, kein „was ist
drin?“. Dafür muss der Server zwischen *verdient* und *geöffnet* unterscheiden, und die Arcade braucht einen Ort und
einen Knopf.

## Offene Punkte

1. ~~Wann wird gezogen – beim Verdienen oder beim Öffnen?~~ → Entscheidung 1.
2. ~~Verfällt eine ungeöffnete Truhe?~~ → Entscheidung 2.
3. ~~Was zeigt die Übersicht vor dem Öffnen?~~ → Entscheidung 3.
4. ~~Wer darf öffnen?~~ → Entscheidung 4.
5. ~~Bestandsdaten?~~ → Entscheidung 5.
6. ~~Wo steht die Truhe, wie viel Animation?~~ → Entscheidung 6.
7. ~~Was gehört nicht in den Kern?~~ → Entscheidung 7.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-14), auf B-535 Entscheidung 5 aufbauend
(B-535 zuerst, B-527 baut das Öffnen darauf).

1. **Gezogen wird beim Verdienen, gutgeschrieben beim Öffnen.** `EvaluateAndAwardAsync` legt den Claim samt Zufallszug
   und Streak wie heute an, bucht aber **keine** Ledger-Zeile; `OpenedAt` bleibt leer. Erst das Öffnen bucht
   `DailyBoxCoins`/`DailyBoxGems` und setzt `OpenedAt`. Begründung: Der Streak-Multiplikator gehört zum Tag des
   Verdienens, nicht zum Tag des Öffnens (sonst lohnte es, Truhen bis zu einer langen Serie aufzusparen); die
   Vergabestellen und ihr Idempotenz-Index bleiben unverändert. Kosten: Der Inhalt steht in der DB, bevor das Kind
   öffnet – er darf darum nirgends vorher ausgeliefert werden (Entscheidung 3).
2. **Eine ungeöffnete Truhe verfällt nie** (B-105, Entscheidung 7, nicht umgestoßen). Wartet mehr als eine, öffnet der
   Knopf die **älteste**; die Übersicht nennt die Zahl wartender Truhen. Kosten: Ein Kind kann Truhen sammeln und
   später öffnen – es gewinnt dadurch nichts (Entscheidung 1).
3. **Vor dem Öffnen ist der Inhalt geheim.** `DailyBoxStatus` wird zu `(bool ClaimedToday, bool OpenedToday, int
   Waiting, int? CoinsAwarded, int? GemsAwarded, int StreakAtClaim)`; `CoinsAwarded`/`GemsAwarded` nur, wenn die
   heutige Truhe geöffnet ist. Begründung: Sonst ist die Überraschung im Netzwerk-Tab und im Vater-Blick schon weg.
   Kosten: Vertragsbruch (Feldsatz ändert sich).
4. **Nur das Kind öffnet.** `POST api/v1/student/children/{childId}/daily-box/open`, `[Authorize]` mit Eigentum wie
   `ExtraTrainingController` (`ExtraTrainingController.cs:16`, `:43` `if (!User.IsStudent()) return Forbid();`).
   Antwort `DailyBoxOpened(int Coins, int Gems, int StreakAtClaim, DateOnly Day, int WaitingAfter)`; ohne wartende
   Truhe `409 daily_box_none`. Begründung: Das Öffnen ist das Erlebnis des Kindes; der Vater sieht das Ergebnis im
   Ledger. Kosten: Der Vater kann eine vergessene Truhe nicht für das Kind öffnen – er kann Münzen von Hand buchen.
   Gutschrift serialisiert über `child.ConcurrencyStamp` (Wallet-Invariante) und eine bedingte Aktualisierung
   `OpenedAt == null`, damit ein Doppeltipp nicht doppelt bucht.
5. **Bestandsclaims gelten als geöffnet.** Die Migration hängt `DailyBoxClaim.OpenedAt` (`DateTime?`) an und setzt
   für vorhandene Zeilen `OpenedAt = AwardedAt` – sie sind bereits gebucht. Begründung: sonst stünden alte Truhen als
   „wartend“ da und würden ein zweites Mal gutgeschrieben. Kosten: ein `UPDATE` in der Migration (nimmt die Zeilen mit,
   schreibt keine Werte um, die jemand liest).
6. **Die Truhe steht als letzter Knoten am Ende des Pfads**, vor dem Bonus-Einstieg, mit drei Zuständen: *zu* (Tagesziele
   offen, `rewardsUnlocked` falsch – Hinweis „Schaffe deine Tagesziele“), *bereit* (wartende Truhe – leuchtet, Knopf
   „Truhe öffnen“, bei mehreren eine Zahl), *offen* (heute geöffnet – zeigt „+N Münzen, +M Gems“). Öffnen: eine
   einfache CSS-Animation (Wackeln, Deckel auf, höchstens ~600 ms), bei `prefers-reduced-motion` ohne Bewegung
   (`lib/ui.ts` `prefersReducedMotion`); danach Saldo neu laden. Kosten: kein Klang, kein Konfetti.
7. **Zurückgestellt** (keine eigene Story; beim nächsten Anlass wieder aufnehmen):
   - aufwendige Animation, Klang (Klang-Vorbehalt B-31), Konfetti;
   - Truhen-Stufen sichtbar nach Serie (Bronze/Silber/Gold) – heute steckt die Serie nur im Multiplikator;
   - Hinweis „Truhe wartet“ am Ende einer Übungsrunde bzw. eines Tests;
   - Vater-Sicht auf ungeöffnete Truhen;
   - eine Wochentruhe für Wochenziele.

## Akzeptanzkriterien

1. Tagesziele erfüllt, Runde beendet ⇒ ein `DailyBoxClaim` mit `OpenedAt = null`, **keine** `DailyBoxCoins`/`Gems`-Zeile,
   Saldo unverändert; Übersicht: `claimedToday: true`, `openedToday: false`, `waiting: 1`, `coinsAwarded: null`. Rot,
   wenn `EvaluateAndAwardAsync` weiter bucht oder der Status den Inhalt vorab nennt.
2. `POST …/daily-box/open` als Kind ⇒ `200` mit den gezogenen Werten, Ledger-Zeilen in genau dieser Höhe, `OpenedAt`
   gesetzt; Übersicht danach `openedToday: true` mit Inhalt. Rot, wenn das Öffnen nicht bucht.
3. Zweites Öffnen (auch parallel, zwei Anfragen gleichzeitig) ⇒ genau eine Buchung; die zweite Anfrage `409
   daily_box_none` bzw. `409 concurrency_conflict`. Rot ohne bedingte Aktualisierung/Stamp.
4. Zwei wartende Truhen (gestern und heute) ⇒ das erste Öffnen öffnet die von gestern, `waitingAfter: 1`. Rot, wenn
   nach `Day` absteigend gewählt wird.
5. Supervisor ruft `open` ⇒ `403`. Rot ohne Rollenprüfung.
6. Migration: ein Bestandsclaim (vor der Migration angelegt) gilt als geöffnet; `open` bucht ihn nicht erneut
   (`SchemaGuardTests` plus ein Test, der die Kette auf einer Datenbank mit Bestandszeile anwendet, sofern das Muster
   im Repo existiert – sonst Zusicherung über das `UPDATE` im Migrationscode im Review).
7. Streak-Multiplikator: eine am Tag mit Serie 7 verdiente und am Tag darauf (Serie 0) geöffnete Truhe zahlt den
   Wert vom Verdienen (`DailyBoxRangeTests`-Muster). Rot, wenn beim Öffnen neu gezogen wird.
8. Arcade (Vitest `SohnHome.test.tsx`): drei Zustände der Truhe aus der Übersicht; Antippen im Zustand *bereit* ruft
   `openDailyBox` genau einmal (Sperre über `useAction`, `disabled={busy}`) und zeigt danach den Inhalt. Rot, wenn die
   Truhe im Zustand *zu* einen Knopf hat.
9. E2E `frontend/e2e/truhe.spec.ts` (Rollengang): Kind erfüllt die Tagesziele, sieht auf dem Pfad die leuchtende
   Truhe, öffnet sie, sieht „+N Münzen“, der Saldo im HUD steigt um N; nach Neuladen steht die Truhe offen. Aufbau wie
   `belohnung-trotz-wochenziel.spec.ts`.

## Schätzung

**Größe: M** – eine Spalte mit Bestands-Update, eine Umstellung der Buchung, ein Endpunkt, ein geänderter Status, ein
Pfad-Knoten mit drei Zuständen und einer E2E. Vergleich: etwa B-535 (M) – weniger Regeln, aber dafür Frontend und
E2E; deutlich kleiner als B-525 (L).

- **`migration: ja`** – `DailyBoxClaim.OpenedAt` (`DateTime?`) angehängt, Bestandszeilen per `UPDATE … SET OpenedAt =
  AwardedAt` (Entscheidung 5); neue Migration an die Kette.
- **`vertragsbruch: ja`** – `DailyBoxStatus` ändert Feldsatz und Bedeutung (Inhalt erst nach dem Öffnen), neuer
  Endpunkt und DTO `DailyBoxOpened`, neuer Fehlercode `daily_box_none`; `contract.ts` neu erzeugen. Die einzige
  Leserin des Status ist die E2E aus B-535 (`belohnung-trotz-wochenziel.spec.ts:65`) – nachziehen.

**Risiken:**

- **Abhängigkeit:** setzt B-535 (`RewardsUnlocked`, im Bau) voraus; erst nach dessen Commit beginnen, sonst
  Konflikte in `DailyBoxService.cs`, `DailyBoxTests.cs` und `DailyBoxRangeTests.cs`.
- Bestehende Tests erwarten die Buchung beim Abschluss (`DailyBoxTests.cs:16`, `:75`, `DailyBoxRangeTests.cs`) – sie
  müssen auf „Abschluss + Öffnen“ umgestellt werden, ohne ihre Aussage (eine Box je Tag, Streak skaliert) zu verlieren.
- Doppelbuchung beim Öffnen: Wallet-Invariante (jeder Pfad, der den Saldo ändert, bumpt `child.ConcurrencyStamp`).
- Gamification, die auf Ledger-Zeilen der Box reagierte, gibt es nicht (nur `PointKindCurrency.cs:22-26` ordnet die
  Währung zu) – beim Bau noch einmal greppen.

**Angriffsplan (Backend zuerst):**

1. Spalte + Migration mit Bestands-`UPDATE`; `SchemaGuardTests`.
2. `DailyBoxService`: Buchung aus `EvaluateAndAwardAsync` in neue `OpenAsync(childId)` verlegen (älteste wartende,
   bedingt, Stamp); `StatusAsync` auf den neuen Feldsatz.
3. Endpunkt `daily-box/open` (eigener kleiner Controller unter `Controllers/Student`, Route `children/{childId}/daily-box`),
   `ApiErrors.DailyBoxNone`, DTOs in `Pugling.Contracts/Shared/ProgressDtos.cs`; Client (`Pugling.Client`) nachziehen.
4. Integrationstests AK 1–7; bestehende Box-Tests umstellen.
5. `contract.ts`, `api.ts` (`openDailyBox`), Truhe als Endknoten in `Pfad` (`SohnHome.tsx`), Texte `de.ts`/`en.ts`,
   Problem-Key `daily_box_none`.
6. Vitest AK 8, E2E AK 9; `docs/tutorial-student.md` und `docs/api-examples` (Doku-Capture) nachziehen.

**Testweg:**

- Integrationstests in `DailyBoxTests.cs` (AK 1–6) und `DailyBoxRangeTests.cs` (AK 7).
- Vitest `SohnHome.test.tsx` (AK 8).
- Playwright `frontend/e2e/truhe.spec.ts` (AK 9) – zugleich der Rollengang. Das sinnliche Urteil über die Animation
  bleibt beim Menschen (argumentierte Ausnahme).

## Verlauf

- 2026-11-13 · `idee` – abgespalten aus B-525; Prio P3 ist ein Vorschlag.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt – die Box wird beim Abschluss automatisch gutgeschrieben (`DailyBoxService.cs:33-36`, gerufen aus `PositionPracticeController.cs:612` und `PositionTestsController.cs:446`); einen Öffnen-Endpunkt oder eine Truhe in der Arcade gibt es nicht. Seit B-535 hängt die Vergabe an `RewardsUnlocked` statt `DutyDone`.
- **2026-11-14** — Refinement Backlog-Lauf: ausformuliert (beim Belegen gefunden: kein Frontend-Code liest `dailyBox` – das Kind sieht die Box heute nur im Kontoauszug), gegrillt (sieben Stakeholder-Entscheidungen im Auftrag des Nutzers: gezogen beim Verdienen, gebucht beim Öffnen; verfällt nie; Inhalt bis zum Öffnen geheim; nur das Kind öffnet; Bestandsclaims gelten als geöffnet; Truhe als Endknoten mit einfacher Animation; Klang, Stufen, Wochentruhe und Vater-Sicht zurückgestellt) und geschätzt: M, Migration ja, Vertrag ja; baut auf B-535 auf.
- **2026-11-14** — Bau Backlog-Lauf (nicht committet): Migration `20261114114551_DailyBoxOpenedAt` (Spalte + `UPDATE … SET OpenedAt = AwardedAt`), `DailyBoxService.EvaluateAndAwardAsync` bucht nicht mehr, neu `OpenAsync` (älteste zuerst, bedingtes `ExecuteUpdate` + Stamp in einer Transaktion), `DailyBoxStatus` neuer Feldsatz, `POST student/children/{id}/daily-box/open` (`DailyBoxController`, `daily_box_none`), Client `OpenDailyBoxAsync`; Arcade-Truhe als letzter Pfadknoten (`Truhe` in `SohnHome.tsx`). Tests: `DailyBoxTests` (AK 1–5), `DailyBoxOpenedAtMigrationTests` (AK 6, Kette down/up auf Wegwerf-DB), `DailyBoxRangeTests.Truhe_Zahlt_Den_Wert_Vom_Verdienen_Nicht_Vom_Oeffnen` (AK 7, Service-Ebene statt Uhr vorstellen), `SohnHome.test.tsx` (AK 8), `e2e/truhe.spec.ts` (AK 9). Abweichung: Zustand *zu* hängt an „nichts wartet, heute nichts geöffnet“ statt direkt an `rewardsUnlocked` (gleichwertig, weil die Truhe beim Freischalten verdient wird); keine Übersicht-Neuladung nach dem Öffnen (die Startseite hinge sonst kurz aus). Suite 1626 grün, Vitest 1027, E2E 4 passed.
- **2026-11-14** — Review-Funde behoben (nicht committet): Öffnen nur durch das Kind selbst (`User.ChildId() == childId`, Test mit Zwei-Profil-Token) und Fremd-Kind-Test (404); Doku `PointKind.DailyBox*` und `StreakAtClaim`; Arcade: `daily_box_none` in `PROBLEM_KEYS`, nach dem 409 nicht weiter leuchten + Übersicht neu laden (Startseite hängt am `showPlaceholder` statt `loading`), Fokus nach dem Öffnen auf der Truhe, „Heute geöffnet“ nur für die Truhe von heute, Hinweis bei Plänen ohne Tagesziele, kein „+0 Gems“. Suite 1628, Vitest 1033, E2E 4 passed.
- **2026-11-14** — **abgenommen** (Sprint 75). volle Suite 1628/1628 (Release, nach den Review-Fixes), Vitest 1033/1033 (zwei volle Läufe), `tsc -b` grün; Rollengang Sohn als Playwright gegen frisch gestartete Server nach dem letzten Edit: 8 passed (`truhe`, `belohnung-trotz-wochenziel`, `bonus`, `bonus-deckel-voll`, `pfad`, `lektion`, `full-flow`, `interessen`); Reviews `pugling-reviewer` + `frontend-reviewer`, zehn Funde im Sprint behoben; Commit `f96ec5c6`. Mutanten rot (mutant-probe): P1 neueste statt älteste Truhe (AK 4),
  P2b Öffnen bucht nicht (AK 2; P2 `if (false)` kompilierte nicht, zählt nicht), P3 Inhalt vorab im Status (AK 1),
  P4 Konto mit zwei Profilen öffnet fremde Truhe (Review-Fund), P5 Supervisor (AK 5), P6 Bestands-`UPDATE` fehlt
  (AK 6), P7 ältere Truhe als „heute geöffnet“, P8 Truhe leuchtet nach `daily_box_none` weiter, P9 Fokus fällt auf
  `body`, P10 Knopf im Zustand *zu* (AK 8), P11 `daily_box_none` ohne Arcade-Satz. AK 3 (paralleles Öffnen) ist ein
  Rennen und wird nur rot, wenn beide Sperren fehlen – vom Bau-Agenten so gemessen, nicht deterministisch. AK 7 am
  Service mit festem Claim. Migration gegen eine Kopie der echten DB geprobt: 2 Bestandstruhen, beide als geöffnet
  markiert. **Gerät:** das Urteil über die Animation (Wackeln/Deckel) bleibt beim Menschen.
- **2026-11-14** — Sprint 76, Review-Funde (Frontend): Truhe liest „erfüllt“ am Server (`freigeschaltet` = `today.rewardsUnlocked`) und sagt bei erfüllten Zielen ohne verdiente Truhe „…die Truhe kommt, sobald du eine Runde oder einen Test abschließt“ (`truhe.comesNext`, de+en; verdient wird sie nur beim Abschluss, `DailyBoxService.EvaluateAndAwardAsync`); Ziel-Auswahl für Wortlaut und Balken aus `belohnungsZiele` (dieselbe Regel wie `RewardObligations`), Balken der Ziele-Karte nur über diese Ziele; gescheitertes Neuladen nach `daily_box_none` sichtbar (`NeuLadenFehler`, `home.reloadFailed`/`home.retry`). Vitest: 4 neue Fälle in `SohnHome.test.tsx`.
- **2026-11-14** — Sprint 76, Gesamt-Review: die `daily_box_none`-Meldung des verlorenen Doppeltipps sagte „already opened“, obwohl noch eine Truhe warten kann – jetzt nennt sie nur die Sperre („Another request opened this reward chest at the same moment.“); `docs/tutorial-student.md` ergänzt, dass immer die älteste wartende Truhe geöffnet wird. Kein eigener Test: der Zweig hängt an einem echten Wettlauf, `DailyBoxTests` prüft dort nur den Code.
- **2026-11-15** — Nachschau im Gesamt-Review des Laufs (Backend + Frontend über `40dd9bfa..HEAD`): Funde aus dem Zusammenspiel im Lauf behoben (Lernkontrolle vom Server `learningCheck`, Truhe/Bonus-Texte aus `rewardsUnlocked`, Agent prüft `canAssign` vor dem Schreiben, `--cadence None` abgelehnt, Truhen-Meldung und Tutorial-Halbsatz); siehe Protokoll „Abschluss“.
