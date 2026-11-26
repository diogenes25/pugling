---
tags: [typ/protokoll, bereich/pm, bereich/gamification, bereich/shop]
---

# PM-Sitzung: Familie & Belohnung – Sparziel und Lob-Kanal (Nachtlauf N)

**Datum:** 2026-11-25  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die Belohnungsseite sichtbar machen: das Kind sieht, wofür es spart, und hört, dass Gelungenes bemerkt wird.

Autonomer Nachtlauf, einer von drei parallelen (M, N, O), freigegeben vom Nutzer für das Paket N: [B-593](backlog/B-593-sparziel-im-familien-shop.md)
und [B-377](backlog/B-377-lob-kanal-des-vaters.md). Beide Stories stehen auf `geschaetzt`, ihre Entscheidungen sind gefallen
(B-593 im Vorbereitungslauf 2026-11-20, B-377 aus der Delegation vom 2026-11-05 mit Opt-in für den autonomen Modus). Dieser
Lauf grillt nichts neu, er baut. Arbeitsbaum `.claude/worktrees/nachtlauf-n`, Branch `nachtlauf/2026-11-25-n`; Ports API 5242,
Vite 5282; die echte `pugling.db` nur als Kopie. Kein neuer Befund unter `docs/anmerkungen/`.

## Runde 1 — Vorstellung & Feedback

Die Rollen sprechen hier aus den Stories und dem letzten Protokoll ([pm-sitzung-2026-11-25](pm-sitzung-2026-11-25.md)), nicht
aus einer eigenen Begehung vor dem Bau – das Paket ist vorgegeben, die Begehung ist der Rollengang am Ende (Runde 2).

### Feedback Sohn (O-Ton)

- **Nervt:** „Münzen hab ich, aber wofür? Der Shop ist eine Liste.“ ([B-535](backlog/B-535-belohnungen-erreichen-friedrich-nicht.md):
  die Münzen erreichten Friedrich nicht.)
- **Top-3:** (1) ein Ziel, das ich mir aussuche, (2) sehen, wie lange es noch dauert, (3) dass Papa merkt, wenn ich was schaffe.

### Feedback Vater (O-Ton)

- **Stört:** „Ich sehe Pflicht, Ziel verfehlt, Malus. Für ‚gut gemacht‘ habe ich keinen Knopf.“
- **Top-3:** (1) aus „Heute“ heraus loben, (2) sehen, ob er es gelesen hat, (3) sehen, worauf er spart.

### Feedback Creator (O-Ton)

- Kein Bezug zum Paket; Katalog unberührt. Regressionszeuge.

## PM-Synthese & Priorisierung (→ Entwickler)

| Prio | Punkt | Größe | Wo | Weg |
| --- | --- | --- | --- | --- |
| P2 | B-593 Sparziel mit Tempo-Prognose | M | beides | Sprint 181 |
| P3 | B-377 Lob-Kanal Vater → Kind | M | beides | Sprint 181 |

**Roter Faden:** Beides liest bzw. schreibt neben dem Wallet, nie hinein. Sparziel und Lob buchen nichts; die
Wallet-Invariante bleibt unberührt, weil keiner der neuen Pfade abbucht – und das Sparziel schreibt seine zwei Spalten per
`ExecuteUpdateAsync`, damit es nicht am `ConcurrencyStamp` des Kindes hängt.

## Sprint 181 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn sieht auf seiner Startseite, worauf er spart und wie lange es bei seinem Tempo noch dauert, und ein
Lob des Vaters erreicht ihn genau einmal – ohne dass eine Münze bewegt wird.

**Umfang:** B-593, B-377 (beide `geschaetzt`, M, beides, Migration ja).

**Entwickler-Brief:**

- **Quelle der Wahrheit:** Sparziel = ein Verweis am Kind (`Child.SavingsGoalListingId`, FK `SetNull`) plus Titel-Momentaufnahme;
  Preis, Bestand und Kontostand live. Tempo = verdiente Münzen (`EarnedCoinKinds`, dieselbe Definition wie Wochenvergleich und
  Tageszahl) der sieben vollen Lerntage vor heute / 7. Lob = neue Tabelle `Praises` mit `SeenAt`.
- **Senken:** `SavingsGoalService.ReadAsync` ist die eine Stelle, die Kind und Vater lesen; `PraiseService` die eine für Lob.
- **Guards:** Setzen nur über `ShopService.ListingsForStudentAsync` (aktiv, Zielgruppe); Vater liest nur (`ChildOwnershipFilter`);
  Lob nur vom Supervisor des Kindes, Stempel nur an eigenen ungelesenen.
- **Migrationen:** `SavingsGoal` (zwei Spalten, FK) und `Praise` (neue Tabelle), angehängt an `AccentLeniency`.
- **Reihenfolge:** Backend B-593 → Backend B-377 → Frontend beider.

## Iteration 181 — umgesetzt

**Backend (API-First).**

- B-593: `Child.SavingsGoalListingId` (FK `SetNull` auf `ShopListing`) + `SavingsGoalTitle`, Migration
  `20261125213333_SavingsGoal`. `SavingsGoalService` (Lesen, Setzen, Entfernen; Tempo über die sieben vollen Lerntage vor
  heute mit `WalletService.EarnedCoinsByDayAsync`). Routen `GET|PUT|DELETE student/me/savings-goal` (204 ohne Ziel) und
  `GET supervisor/children/{childId}/savings-goal` (nur lesen, `ChildOwnershipFilter`). Setzen und Entfernen per
  `ExecuteUpdateAsync`: kein Ledger, kein Stamp.
- B-377: Tabelle `Praises` (Kind und Absender `Cascade`, Index `(ChildId, SeenAt)`), Migration `20261125214724_Praise`.
  `PraiseService`; `POST|GET supervisor/children/{childId}/praise` (1–140 Zeichen nach dem Trimmen, nur eigene Lobe, paged),
  `GET student/me/praise` (ungelesen, höchstens 5, mit Absendername), `POST student/me/praise/seen` (nur eigene, Stempel
  einmal).
- Client-Zeilen in `Pugling.Client` (dazu `GetOptionalAsync` für 204), Tore nachgezogen: G2 (drei FK-Zeilen),
  Endpunkt-Abdeckung 315 → 323, ungepagte Array-GETs 34 → 35 (`me/praise`, höchstens fünf by design).
- Review-Fund eingearbeitet: das Lesen des Ziels schrieb über `ListingsForStudentAsync` Bestandszeilen (ein GET mit
  Schreib-Nebenwirkung, auch für den Vater). `ListingsForStudentAsync` hat jetzt `persist` **ohne Vorgabewert** (Step 4:
  eine Lockerung für einige Aufrufer ist ein Parameter ohne Default); der Shop schreibt wie bisher, das Ziel nie.

**Frontend.** Sohn: `SparzielStreifen` auf der Startseite (beide Zweige, auch ohne Mission), gepinnte Karte und „☆ Als Ziel“
(`aria-pressed`) im Shop, `LobOverlay` als Dialog mit Fokus-Falle (`useDialogFocus`) und `useAction`. Vater: „👏 Loben“ in
jeder Zeile von „Heute“, Hervorhebung bei `dutyDone`, `LobPanel` (vier Sätze, 140 Zeichen, `<Pflicht />`, eigene Lobe mit
„gelesen am“), im Kind-Hub die Sparziel-Zeile (nur lesend).

**Verifikation.** Backend-Suite vor den Review-Fixes 2480/2481 (rot nur das gepinnte Zählertor der ungepagten GETs, 34 → 35
bewusst nachgezogen). Endstand nach den Review-Fixes: 2480/2481 – rot nur `OpenApiExampleTests` nach 4 min 16 s im Gesamtlauf (26 min
unter der Last dreier paralleler Läufe), einzeln danach 2/2 grün; kein Bezug zum Diff außer den neuen Routen, die er mit
prüft und einzeln besteht. Vitest 1482/1482, `tsc -b` sauber, `dotnet format Pugling.sln
--verify-no-changes` sauber. Playwright (Ports 5242/5282, Release): `sparziel.spec.ts`, `lob.spec.ts`, `shop-verlauf.spec.ts`,
`full-flow.spec.ts` – 4 passed. Mutanten nach der letzten Änderung: Backend 29 Zeilen, davon 27 rot; S16 und S16b (die zwei
Schichten von „der Vater setzt nicht“) je grün – doppelt gesichert, je Schicht allein ununterscheidbar, steht so in B-593.
Frontend 12/12 rot. Die Tabellen je Fall stehen im `## Verlauf` der beiden Stories.

**Review.** `pugling-reviewer`: kein Blocker; eingearbeitet: (1) das Lesen des Ziels schrieb Bestandszeilen – ein GET mit
Schreib-Nebenwirkung, auch für den Vater; `ListingsForStudentAsync` nimmt jetzt `persist` ohne Vorgabewert, Test sichert „keine
Bestandszeile nach dem Lesen“ (S17 rot); (2) Rennen „Angebot zwischen Prüfung und Schreiben gelöscht“ → 404 statt 500; (3) eine
Uhr je Lesung. Offen gelassen (🟢): Titel-Momentaufnahme veraltet bei Umbenennen-dann-Löschen (spezifiziert so); `Praise.Text`
trägt die Freitext-Länge 2000 statt 200 (harmlos). `frontend-reviewer`: kein Blocker; eingearbeitet: Fokus-Falle des Overlays
(`useDialogFocus`, Escape schließt ohne Stempel), `useAction` im Overlay, `<Pflicht />`, die zwei E2E-Specs, Gem-only-Text,
Plural und `stale` im Kind-Hub, Label-in-Name am Ziel-Knopf, `.heute-erfolg` als Klasse.

## Runde 2 — Re-Review / Abnahme

Rollengang an einer **Kopie der echten DB** (`kopie-server.sh`, :5242, gestartet nach der letzten Änderung; zugleich die
Migrationsprobe – beide Migrationen laufen durch, EF meldet den FK-Neubau von `Children` als nicht-transaktional, alle vier Kinder
und ihr Ledger unverändert). Dazu maschinell die zwei Playwright-Specs gegen echte Oberfläche und Server (Wegwerf-DB).

- **Sohn – abgenommen.** „Ich hab mir ‚60 Minuten Zocken‘ angeheftet. Als Papa den Preis auf 4000 gesetzt hat, stand da ‚noch
  1930 Münzen, bei deinem Tempo etwa 5 Tage‘ – das stimmt, ich hab in der Woche 3368 verdient. Und Papas Lob kam einmal, mit
  seinem Namen, und nach ‚Danke!‘ war es weg.“ (Friedrich an der Kopie per API; Streifen, Shop-Knopf, Overlay per Playwright.)
  **Offen für den Menschen:** ob das Overlay sich am Handy gut anfühlt (Größe, Lesbarkeit des Lobtexts) – ein Blick am Gerät.
- **Vater – abgenommen.** „Ich lobe aus ‚Heute‘ heraus, sehe danach ‚gelesen am‘, und im Kind-Hub steht, worauf er spart. Gebucht
  wird dabei nichts – Kontostand 2070/186 vorher wie nachher.“
- **Creator – Regressionszeuge.** Kein Pfad im Diff; `creator/subjects` und `creator/exercise-types` an der Kopie 200, Suite grün.

## Retrospektive

**Nachschau:** Sprint 165 – B-990 (`fc3c37f4`): `AnswerGrader.Comparable` und `AnswerVariants.CanonicalSlots` gelesen; alle
Vergleiche des Graders gehen durch dieselbe Form, die Art-Trennung hält, Wortgrenzen beidseitig – kein Befund.
`nachgeschaut: 2026-11-25` an B-990. (Index nicht neu erzeugt – Kollisionsschutz der parallelen Läufe; das macht der Merge.)

**Was die eigenen Tore durchgelassen haben:** Beide 🟡 kamen vom Review, keiner von Test oder Mutant: ein GET, der schreibt
(`ListingsForStudentAsync` materialisiert Bestand), und ein `aria-modal` ohne Fokus-Falle. Beides sind Eigenschaften eines
**wiederverwendeten** Bausteins, nicht des neuen Codes – die Step-4-Regel „ein geteilter Helfer wird je Aufrufer entschieden“ hätte
den ersten gefangen, hätte ich sie beim *Aufruf* (nicht nur beim Ändern) eines Helfers angewandt.

**Von außerhalb der Schleife:** Ein Werkzeug-Fund. Die drei parallelen Läufe teilen sich **ein** Scratchpad; meine Datei
`mutanten-fe.jsonl` wurde von einem anderen Lauf überschrieben, die Probe brach an einem fremden Dateipfad ab. Glück: ein fremder,
im eigenen Baum *existierender* Pfad hätte still die Mutanten des anderen Laufs gemessen.

**Der eine Mechanismus:** Prosa im treibenden Skill (`pm-loop` Step 5): Mutantendatei in einen Ordner je Lauf. Kein Tor – die
Probe selbst kann nicht wissen, welcher Lauf sie aufruft. `context-budget.sh` danach gelaufen.

**Bewusst ungedeckt:** „Ein GET schreibt nicht“ hat kein Tor; es hängt an Kommentaren in `ShopService`. Ein reflexiver Wächter
bräuchte eine Liste schreibender Service-Methoden – erst messen, nicht hier.

## Offene Roadmap (priorisiert)

Durable Liste: [docs/backlog/](backlog/). Aus diesem Sprint nichts übrig außer dem Blick am Gerät (oben). Keine neue Story; die
Ids B-1010…B-1019 bleiben unbenutzt.
