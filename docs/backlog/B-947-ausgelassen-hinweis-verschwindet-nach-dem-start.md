---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, rolle/supervisor]
aliases: []
status: abgenommen
prio: P2
art: Defekt
groesse: M
wo: "backend"
migration: "ja"
vertragsbruch: "nein"
quelle: "Nachschau Sprint 135 in der Retro von Sprint 137 (docs/pm-sitzung-2026-11-24.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-941]
wartet_auf: ""
---

# B-947 · Nach dem Start verschwindet der Hinweis „N Übungen ohne Ausführ-Recht ausgelassen“, ohne dass die Übung je zur Pflicht wird

Am Code bestätigt (2026-11-24, `cfebfe60`): `ClassTestPrepService.NotExecutableCountAsync`
(`backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:142-153`) zählt gegen den **Live-Stand** der Rechte. Der Start
läuft aber genau einmal (`ReconcileAsync`, `:196-198`, Anspruch über `PrepStartedOn`) und friert das Bündel ein.

So passiert es: Ein Bündel hat eine öffentliche Übung A und eine zurückgezogene Übung B. Der erste Login im Fenster startet die
Pflicht nur mit A. Der Vater sieht „1 Übung ohne Ausführ-Recht ausgelassen“ und holt sich beim Lehrer eine Freigabe für B. Danach
fällt der Zähler auf 0, und der Hinweis verschwindet. B wird aber nie zur Position, und nichts sagt das dem Vater. Der Hinweis rät
ihm also einen Ausweg, der nach dem Start nichts bewirkt. Das ist dieselbe Fehlerklasse wie die Ratschläge in B-508…B-516.

## User Story

Als Vater möchte ich, dass eine Freigabe, die ich nach dem Hinweis „N Übungen ohne Ausführ-Recht ausgelassen“ hole, die Übung auch
nach dem Start noch zur Vorbereitungs-Pflicht macht, damit der Hinweis mir keinen Ausweg rät, der nichts bewirkt.

## Ist-Stand am Code (2026-11-24, `f4324888`)

- `ReconcileAsync` startet eine Arbeit genau einmal (`ClassTestPrepService.cs:196-215`); danach legt niemand mehr Positionen an.
- `StartAsync` (`:223-256`) legt Positionen nur für `NewCandidatesAsync` an, also nur für ausführbare Übungen (B-941).
- `NotExecutableCountAsync` (`:142-153`) zählt gegen den Live-Stand der Rechte, auch nach dem Start.
- Welche Übungen beim Start ausgelassen wurden, steht nirgends: `KlassenarbeitTags` hat kein `CreatedAt`, das Bündel zum Startzeitpunkt
  ist also nicht rekonstruierbar.

## Die echte Lücke

Der Hinweis nennt einen Ausweg, und der Start nimmt ihm nachträglich die Wirkung.

## Entscheidungen

1. **Nutzer, 2026-11-24: Nachziehen.** Eine nach dem Start ausführbar gewordene, beim Start ausgelassene Übung wird beim nächsten
   Abgleich zur Position. Verworfen: „ehrlich zählen“ (der Ausweg bliebe wirkungslos).
2. **Momentaufnahme statt lebendem Bündel** (stellvertretend): Der Start schreibt die ausgelassenen Übungen in eine neue JSON-Spalte
   `Klassenarbeit.PrepLeftOutExerciseIds`. Nur sie dürfen nachgezogen werden, und nur solange sie noch im Bündel stehen. Begründung:
   B-752 Entscheidung 3 (eingefrorenes Bündel) gilt weiter für alles, was nach dem Start ins Bündel kommt. Kosten: Migration (Spalte
   nullable, Bestand `NULL` = nichts nachzuziehen) und ein `ValueComparer` (Tor G7).
3. Die nachgezogene Position gleicht einer beim Start gemachten: tägliche Pflicht ab heute (kein rückwirkender Malus), Ende am Tag vor
   der Arbeit, Dosis über die verbleibenden Tage, keine Ziel-Belohnung. **Malus** (stellvertretend): `PenaltyCoinsPerDay / Anzahl der
   Vorbereitungspositionen der Arbeit nach dem Nachziehen`, mindestens 1; die laufenden Positionen behalten ihren Malus, damit sich
   kein noch nicht abgerechneter Tag rückwirkend ändert. Kosten: Der Tagesmalus der Arbeit kann leicht über 5 steigen.
4. Nach dem Start zählt der Hinweis nur noch die ausgelassenen Übungen der Momentaufnahme, die weiter nicht ausführbar sind und noch im
   Bündel stehen – genau die, die eine Freigabe nachzieht. Vor dem Start bleibt die Zählung, wie sie ist.

## Akzeptanzkriterien

1. Gemischtes Bündel (A öffentlich, B zurückgezogen): Der Start legt nur A an und merkt sich B.
2. Danach Freigabe für B, dann Kind-Login: B ist Position der Arbeit, `DutySince` = heute, `DutyUntil` = Vortag der Arbeit. (Die Probe
   nimmt den Ausweg **nach** dem Start und prüft die Wirkung, pm-loop Step 5.)
3. Eine nach dem Start neu ins Bündel gekommene, ausführbare Übung wird **nicht** nachgezogen (Einfrieren bleibt).
4. Eine ausgelassene Übung, die vor der Freigabe aus dem Bündel genommen wurde, wird nicht nachgezogen.
5. Nach dem Start zählt der Hinweis B, solange B nicht ausführbar ist, danach 0; eine nach dem Start neu gekommene, nicht
   ausführbare Übung zählt nicht mit.
6. Zweimaliges Abgleichen nach der Freigabe legt B nur einmal an.
7. Migration: Bestand bleibt unverändert, neue Spalte `NULL` (Probe an einer Kopie der echten DB).

## Schätzung

- **Größe:** M, backend, Migration (eine nullable Spalte), kein Vertragsbruch.
- **Risiko:** Positionsanlage aus `StartAsync` herauslösen, ohne B-752-Fälle zu verschieben (Mutanten-Probe danach).
- **Testweg:** Integrationstests in `VorbereitungsPflichtTests` (AK 1–6), Migrationstest nach dem Muster
  `VorbereitungsPflichtMigrationTests` (AK 7), Mutante je AK.

## Angriffsplan

Spalte und Comparer, Migration. `StartAsync` schreibt die Momentaufnahme; die Positionsanlage wird eine private Methode. In
`ReconcileAsync` für gestartete Arbeiten im Fenster die nachziehbaren Übungen anlegen (gleiche Transaktion wie der Start nicht nötig:
die Momentaufnahme wird im selben `SaveChanges` gekürzt). `NotExecutableCountAsync` nach dem Start auf die Momentaufnahme umstellen.

## Verlauf

- **2026-11-24** — angelegt aus der Nachschau von Sprint 135 (Reviewer-Fund, von mir am Code nachgelesen). Bewusst nicht in Sprint
  137 genommen: Dessen Ziel hängt nicht daran, und der Ausweg ist eine Produktentscheidung.
- **2026-11-24** — Runde 138: Entscheidung 1 vom Nutzer, Entscheidungen 2–4 stellvertretend; `geschaetzt` für Sprint 138.
- AK 1, Start merkt sich B → `Freigabe_Nach_Dem_Start_Zieht_Die_Ausgelassene_Uebung_Nach`, B947-1 (leere Momentaufnahme) → rot.
- AK 2, Freigabe nach dem Start wirkt → derselbe Test, B947-2 (nie nachziehen), B947-3 (`DutySince` einen Tag früher), B947-3b
  (voller Malus) → rot. Die erste Form von B947-3 kompilierte nicht und ist umformuliert neu gemessen.
- AK 3, Einfrieren bleibt → `Nach_Dem_Start_Ins_Buendel_Gekommenes_Wird_Nicht_Nachgezogen`, B947-4 (Momentaufnahme ignoriert). **Erster
  Lauf grün:** Der Test ließ beim Start nichts aus, das Nachziehen lief also nie. Umgebaut (beim Start wird eine Übung ausgelassen),
  danach rot.
- AK 4, aus dem Bündel genommen → `Nachziehen_Nur_Was_Noch_Im_Buendel_Steht_Und_Nur_Einmal`, B947-5 → rot.
- AK 5, Hinweis nach dem Start → B947-7 (Live-Zählung) und B947-7b (immer 0) → rot.
- AK 6, nur einmal → zwei unabhängige Wächter (Dubletten-Filter, gekürzte Momentaufnahme). Einzeln grün (B947-6); von Hand beide
  zugleich entfernt → rot, nach dem Zurückspielen grün.
- AK 7, Migration → `PrepLeftOutMigrationTests`, B947-8 (`defaultValue: "[]"`) → rot. Die Migration ist ein reines nullable
  `AddColumn`; gegen die Kopie der echten DB lief sie beim Start ohne Fehler.
- `pugling-reviewer`: kein Blocker. Bewusst offen: Zwischen Freigabe und nächster Kind-Anmeldung zeigt der Hinweis schon 0, die Position
  entsteht aber erst bei der Anmeldung (eine Freigabe gleicht nicht ab). Vor dem Start laufende Arbeiten haben keine Momentaufnahme
  (`NULL`) – ihr Hinweis zählt nach dem Start 0, nachgezogen wird dort nichts.
- **2026-11-24** — abgenommen in Sprint 138, Commit `08933431`. Volle Suite 2323/2323 grün (Release), Vitest 1439/1439, `npm run build` grün. Mutanten-Probe nach dem letzten Edit (`mutant-probe.py`, Tabelle im Protokoll Runde 138). Rollengang: Playwright auf Ausweichports 5201/5174, Server frisch gestartet: `klassenarbeit-countdown`, `klassenarbeit-vorbereitung`, `klassenarbeit-stoff-dicht`, `rueckmeldung`, `pfad` – 5 passed.
