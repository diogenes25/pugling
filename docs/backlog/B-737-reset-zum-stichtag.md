---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/training, rolle/supervisor]
aliases: [Stichtag, Reset, Lernstand zurücksetzen, Planer scharf schalten, Etappe E3b]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
---

# B-737 · Stichtag: Reset von Lernstand und Plänen und Umschalten auf den scharfen Planer (B-644, Etappe E3b)

Achte Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Der Stichtag setzt in **einer
Transaktion** den Lernstand und die festen Pläne beider Kinder zurück und schaltet den Planer von Schatten auf scharf
([B-734](B-734-planer-kern-und-schattenmodus.md), `PlannerSettings`). Münzen, Gems, Skins, Shop-Einlösungen und das Kassenbuch
bleiben. Die Pflicht, die danach gilt, liest [B-736](B-736-pflicht-aus-der-tagesaufgabe.md).

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 geerbt. Entscheidungen 11, 12, 14 stammen vom Vater (2026-11-17).

## User Story

Als *Vater* möchte ich den Stichtag selbst setzen, wenn mir die Schattenpläne sieben Tage lang plausibel vorkamen, damit die Kinder ab
dann nach dem Planer lernen, ohne dass ihr Guthaben oder ihre gekauften Sachen verloren gehen. Als *Sohn* möchte ich meine Münzen,
Gems und Skins behalten.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/Pugling.Api/`.

- **Das Kassenbuch hat keinen Fremdschlüssel auf Plan oder Position:** `ChildPointsEntry` (`Models/AdminEntities.cs:211`-`:220`) trägt
  Kind, Betrag, Art, Begründung als Text, Zeit. Löscht man Pläne und Positionen, bleibt es lesbar (Befund aus B-644, hier gegen die
  Fremdschlüsselliste der Modelle gegengelesen: kein `PlanPositionId` in `AdminEntities.cs` oder den Shop-Modellen).
- **Löschweg heute:** Positionen und Pläne hängen über `PlanPositionId`/`StudyPlanId` an `PositionItemProgress`,
  `PositionGoalReward`, `PositionGoalPenalty`, `PracticeSession`, `TestAttempt` (`Models/PlanPositionEntities.cs:186`, `:206`, `:226`,
  `:247`; `Models/StudyPlanEntities.cs:110`, `:173`); `ExerciseFeedback` und `Remark` tragen eine **nullable** Referenz
  (`Models/FeedbackEntities.cs:24`, `Models/RemarkEntities.cs:106`, `:110`).
- **Lernstand je Wort:** `ItemProgress` und `ItemReviewEvent` (`Models/ItemProgressEntities.cs:18`, `:61`) hängen am Kind, nicht an der
  Position; sie müssen eigens gelöscht werden.
- **Kein Reset-Weg:** Eine Funktion, die Lernstand und Pläne eines Kindes zurücksetzt, gibt es nicht (keine Fundstelle).
- **Nicht in B-644 genannt und am Code zu entscheiden:** `DailyBoxClaim` (Serien-Stand beim Öffnen, `Models/GamificationEntities.cs:98`),
  `MissionAward`, `ExtraTrainingRun` (`:124`), `Objective`/`KeyResult`, `Klassenarbeit`, `TimetableEntry`.
- **Heimnetz:** Der Start sichert die echte `pugling.db` (`scripts/heimnetz.ps1`); ein Zurück gibt es nur über das Backup (B-644
  Entscheidung 11).

## Entscheidungen (geerbt)

1. **Umstellung zum Stichtag ohne Rücksicht auf den bisherigen Lernstand; beide Kinder können komplett zurückgesetzt werden**
   (B-644 Entscheidung 11, Vater). Ausnahme für diesen Umbau von der Regel „die Daten der Söhne überleben jede Schemaänderung"; die
   Migrationskette wird weiter nur verlängert.
2. **Der Reset umfasst Lernstand und Pläne; das Guthaben bleibt:** Leitner-Stände, Testergebnisse, Verlauf, feste Pläne und
   Positionen weg; Münzen, Gems, gekaufte Skins, eingelöste Shop-Artikel bleiben; die Serie beginnt neu (Entscheidung 12, Vater).
3. **Eine Woche Schattenmodus vor dem Stichtag; der Vater setzt den Stichtag** (Entscheidung 14, Vater).
4. **Der Planer startet ohne Leistungsprofil** und behandelt anfangs bekannte Wörter als neu (Entscheidung 12, Kosten).

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **Der Stichtag ist ein Endpunkt des Vater-Web, kein Skript:** `POST supervisor/planner/stichtag` mit einem Bestätigungstext,
   gated auf `Roles.Supervisor`, wirkt auf die Kinder des Aufrufers (`SupervisorLink`) und kippt `PlannerSettings` in derselben
   Transaktion. Begründung: Ein Reset und ein Modus, die auseinanderlaufen, wären der schlimmste Zustand; eine Konfigurationsdatei
   ließe einen Neustart dazwischen. Verworfen: ein Wartungsskript außerhalb der API (kein Gate, keine Prüfung des Eigentums).
2. **Der Modus ist global (`PlannerSettings`, eine Zeile),** entsprechend dem Wegfall des Parallelbetriebs je Kind (Entscheidung 11).
   Kosten: Ein zweiter Vater im selben System würde mitgeschaltet; heute gibt es nur eine Familie.
3. **Es bleiben:** `ChildPointsEntry`, `Child.Coins`/`Gems`/`OwnedSkins`, `ShopPurchase`, Inventar, `DailyBoxClaim` und `MissionAward` als
   Historie, `ExtraTrainingRun`, `Klassenarbeit`, `TimetableEntry`, `ExerciseFeedback` und `Remark` (deren Positionsverweis wird leer).
   **Es gehen:** Positionen, Pläne, `ItemProgress`, `ItemReviewEvent`, Sitzungen, Testversuche, alle Schattenpläne und `DailyEvaluation`
   (sie entstanden aus gelöschten Antworten). Offen ist, ob `Objective`/`KeyResult` Positionen oder Pläne referenzieren; beim Bau
   nachzusehen und bei einem Verweis entweder mitzunehmen oder zu verwerfen.
4. **Ein zweiter Aufruf nach dem Stichtag ist `409` mit `code: planner_already_live`** und ändert nichts.

## Akzeptanzkriterien

1. Nach dem Stichtag sind Lernstand (`ItemProgress`, `ItemReviewEvent`), Pläne, Positionen, Sitzungen und Testversuche beider Kinder leer;
   Münzen, Gems, Skins, Shop-Einlösungen und das Kassenbuch sind unverändert. *Rot, wenn* der Reset `ChildPointsEntry` oder das Inventar
   anfasst oder ein Lernstand übrig bleibt.
2. Reset und Modus gelten gemeinsam: Scheitert der Reset, bleibt der Modus bei „Schatten"; gelingt er, steht der Modus auf „scharf".
   *Rot, wenn* ein Abbruch in der Mitte zwei Zustände hinterlässt.
3. Die Serie beginnt neu (Serie 0 nach dem Stichtag); der Tagesbox-Verlauf bleibt als Historie lesbar. *Rot, wenn* die Serie weiterläuft.
4. Ein Verweis von `ExerciseFeedback` oder `Remark` auf eine gelöschte Position blockiert den Reset nicht und wird leer. *Rot, wenn* der
   Reset an einer Fremdschlüsselverletzung scheitert.
5. Der Endpunkt verlangt Supervisor-Claim, Eigentum an den Kindern und den Bestätigungstext: ein Konto nur mit Creator-Profil bekommt
   `403`, ohne Text `400` mit `code: validation_error`, ein zweiter Aufruf `409`. *Rot, wenn* ein Gate oder die Bestätigung fehlt.
6. `Klassenarbeit` und `TimetableEntry` bleiben. *Rot, wenn* der Reset sie löscht.
7. Nach dem Stichtag entsteht ein Tagesplan im scharfen Modus mit Pflicht aus Tagesaufgaben (Zusammenspiel mit
   [B-736](B-736-pflicht-aus-der-tagesaufgabe.md)); ohne Profil gilt der Boden. *Rot, wenn* der Planer ohne Profil keinen Plan erzeugt.
8. Das Vater-Web zeigt den Stichtag-Schritt mit Bestätigungstext und dem Hinweis auf das Backup und liest den Modus danach zurück.
   *Rot, wenn* die Maske den Schritt ohne Bestätigung zulässt oder den Modus nicht vom Server liest.
9. Vor dem Stichtag wird der Reset an einer **Kopie der echten DB** gemessen (kein CI-Test, ein Abnahme-Beleg im Verlauf). *Rot, wenn* der
   Verlauf bei der Abnahme diesen Beleg nicht nennt.

## Schätzung

**Größe: M** — Anker: wie der Batch-Pfad im `MediaSelector`, aber mit Löschreihenfolge über zwölf Tabellen und einer Transaktion über
zwei Zustände; kein Schema, ein Endpunkt, eine kleine Maske. Wirkt auf echte Daten und ist nicht rückholbar, darum die Messung an einer
Kopie.

- **`migration: nein`** — `PlannerSettings` und die übrigen Tabellen kommen aus [B-734](B-734-planer-kern-und-schattenmodus.md) und
  [B-736](B-736-pflicht-aus-der-tagesaufgabe.md); der Reset löscht nur Zeilen.
- **`vertragsbruch: nein`** — ein neuer Endpunkt mit neuem DTO.
- **`wo: beides`** — Backend zuerst, dann eine Maske im Vater-Web.

### Risiken

- **Nicht umkehrbar:** Ein Fehler trifft die Münzen beider Kinder; ein Zurück gibt es nur über das Backup.
- **Löschreihenfolge und Fremdschlüssel:** Die meisten Verweise sind Kaskaden, `ExerciseFeedback` und `Remark` setzen auf `SetNull`; die
  Reihenfolge ist mit dem Modell abzugleichen (`SchemaGuardTests`, `FachLoeschenSperreTests` für das Sperrverhalten ähnlicher Löschwege).
- **Datum:** Die Klausurpläne der Söhne laufen noch (`scripts/klausurplan-b-2026-11-24.ps1`, `scripts/friedrich-vokabeltest-2026-11-24.ps1`);
  ein Stichtag mitten in einem laufenden Klausurplan würde ihn löschen. Der Zeitpunkt ist eine Entscheidung des Vaters, siehe Protokoll.
- **Der Test-Host kennt keine echte Datenbank:** Der Beleg an einer Kopie ist ein Handgriff bei der Abnahme.
- **Der Modus ist global** (Annahme 2).

### Angriffsplan

1. Reset-Dienst (eine Transaktion, Löschreihenfolge, Zähler je Tabelle in der Antwort).
2. Endpunkt mit Bestätigung und Gates; Modus in derselben Transaktion.
3. Integrationstest an der geseedeten Familie (`SeedFamilyTests`-Muster) mit Vorher-/Nachher-Zählern.
4. Maske im Vater-Web.
5. Messung an einer Kopie der echten DB (außerhalb CI).

### Testweg

- Neue Klasse `StichtagTests` (Integration): Familie mit Plänen, Positionen, Lernstand, Käufen, Skins und Kassenbuch; Vorher-/Nachher-Zähler
  je Tabelle; Abbruch in der Mitte (Fehler eingespritzt, Modus bleibt); zweiter Aufruf `409`; Gates. Rote Probe je AK: `ChildPointsEntry`
  in die Löschliste aufnehmen lässt AK 1 fallen; die Transaktion in zwei Speichern aufteilen lässt AK 2 fallen.
- Regression: `SeedFamilyTests`, `OwnershipMatrixTests`, `ShopFlowTests`, `SkinPurchaseTests`, `PointsCurrencyTests`.
- Frontend: Vitest für die Maske; E2E neu `frontend/e2e/stichtag.spec.ts` (Vater bestätigt, Sohn sieht nach dem Stichtag den Plan und behält
  die Münzen).
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer`, Rollengang als Vater und Sohn, Beleg der Kopie-Messung im `## Verlauf`.

## Reihenfolge und Abhängigkeiten

Nach [B-736](B-736-pflicht-aus-der-tagesaufgabe.md) und [B-735](B-735-stoffrahmen-ohne-termin.md) (der Vater legt nach dem Reset
Stoffrahmen ohne erfundenes Datum an) und nach der Schattenwoche aus B-734. Vor [B-738](B-738-eingriffe-bonus-und-sohn-begruendung.md)
und [B-739](B-739-foerder-uebungen-und-zusammenfassung.md), die den scharfen Planer voraussetzen.

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E3b) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet):
  Eintrittsbedingung `gegrillt` aus B-644 geerbt. Nachgesehen: Das Kassenbuch ist frei von Positionsverweisen, `ExerciseFeedback` und `Remark`
  verweisen nullable. Ein Reset-Weg existiert nicht. M, beides, keine Migration, kein Vertragsbruch.
- **2026-11-27** — Nachtlauf P: [B-1039](B-1039-umschalten-scharf-umschalttag-und-wochenziel.md) (Umschalttag, angebrochene Woche) vor dem Bau beantworten; [B-1037](B-1037-malus-je-tagesaufgabe-und-rueckstand.md) muss vor dieser Story stehen, sonst gilt der scharfe Modus ohne Malus.
