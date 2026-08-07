---
tags: [typ/story, status/geschaetzt, bereich/katalog, rolle/creator]
aliases: [published-Flag, Sichtbarkeit, Übung veröffentlichen bei Anlage]
status: geschaetzt
prio: P2
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: memory/geteilte-uebungs-bibliothek.md
---

# B-11 · Übungen ausdrücklich veröffentlichen

> **Der Kern der Idee war beim Ausformulieren bereits gebaut** (siehe Ist-Stand): Ein Creator kann eine
> Übung heute schon ausdrücklich aus dem Verkehr nehmen und wieder freigeben. Die reale Lücke ist enger als
> die Notiz vermuten ließ — sie betrifft nur den Anlage-Zeitpunkt, nicht das Rechte-Modell.

## User Story

Als Creator möchte ich beim **Anlegen** einer Übung ausdrücklich entscheiden, ob sie sofort für andere
Betreuer zuweisbar ist, damit ich Material, das ich privat halten will (z. B. eine unfertige Klausur),
nicht erst anlegen und dann in einem zweiten Schritt zurückziehen muss.

## Ist-Stand am Code

Die Notiz (`memory/geteilte-uebungs-bibliothek.md`, Stand 2026-07-30) behauptet „ein `published`-Flag gibt
es nicht". Das stimmt nicht mehr — und traf schon beim Schreiben der Notiz nicht mehr zu:

- **`Exercise.ExecutePublic`** ([Models/LearnEntities.cs:110-115](../../backend/Pugling.Api/Models/LearnEntities.cs#L110-L115))
  ist genau dieses Flag: „executable **for every** creator … If an owner sets it to `false`, only owners
  and creators holding an execute/write grant may assign the exercise."
- **`PATCH creator/exercises/{id}/sharing`** ([Controllers/Creator/ExerciseCatalogController.cs:170-183](../../backend/Pugling.Api/Controllers/Creator/ExerciseCatalogController.cs#L170-L183)),
  Vertrag `SetExerciseSharingDto`/`ExerciseSharingResponse`
  ([Contracts/Creator/ExerciseGrantDtos.cs:12-56](../../backend/Pugling.Contracts/Creator/ExerciseGrantDtos.cs#L12-L56)):
  ein eigener, owner-only Endpunkt, der **ausdrücklich** „Publishes an exercise or withdraws it" heißt.
  Gebaut in Commit `88ed858` („Material zurückziehen – sichtbar und bedienbar", **2026-07-28** — zwei Tage
  **vor** dem Datum der Notiz).
- **Frontend voll verdrahtet**: `VaterExercises.tsx` (`UsagePanel`, Zeilen 191-246) zeigt Zustand und
  Knopf „⬇️ Zurückziehen" / „⬆️ Wieder freigeben" in der Verwendungs-Anzeige jeder eigenen Übung; dokumentiert
  in [frontend/CLAUDE.md](../../frontend/CLAUDE.md) unter „Material zurückziehen". Getestet in
  [frontend/e2e/freigabe.spec.ts](../../frontend/e2e/freigabe.spec.ts) und
  [backend/Pugling.Api.Tests/ExerciseSharingTests.cs](../../backend/Pugling.Api.Tests/ExerciseSharingTests.cs).
- **Durchsetzung**: `ExercisePermissionService.CanExecuteAsync`
  ([Auth/ExercisePermissionService.cs:40-50](../../backend/Pugling.Api/Auth/ExercisePermissionService.cs#L40-L50))
  — `ExecutePublic || Grant vorhanden` entscheidet bei der Zuweisung (`POST .../study-plans/{planId}/positions`
  u. Klassenarbeiten), nicht beim Lesen.
- **Read bleibt bewusst global**: `ExercisePermissionService`-Kommentar Zeile 16 und
  `GrantPermission`-Doku ([Contracts/Common/LearnBaseTypes.cs:44-45](../../backend/Pugling.Contracts/Common/LearnBaseTypes.cs#L44-L45)):
  „Read is deliberately not part of the model – the catalog remains readable for everyone." Die Katalog-Suche
  (`ExerciseCatalogController.Search`, Zeilen 43-100) filtert nicht nach `ExecutePublic` — jede Übung bleibt
  für jeden Creator auffindbar, nur die **Zuweisung** wird gesperrt.
- **Kein zweites Rechte-Konzept**: `ExerciseGrant`/`GrantPermission` (Owner/Write/Execute,
  [Models/LearnEntities.cs:129-141](../../backend/Pugling.Api/Models/LearnEntities.cs#L129-L141)) ist die
  einzige Rechte-Schicht; `ExecutePublic` ist ihr Fail-Safe-Default, kein Parallelsystem.
- **Der Anlage-Zeitpunkt fehlt der UI**: `ExercisePayload<TConfig>` trägt `bool ExecutePublic = true`
  ([Contracts/Creator/ExerciseAuthoringDtos.cs:17](../../backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs#L17))
  und der Server übernimmt sie unverändert beim Anlegen
  ([Controllers/Creator/ExerciseControllerBase.cs:258](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs#L258)) —
  **das Feld ist also schon Backend-seitig entgegennehmbar**. Aber das Anlage-Formular
  ([frontend/src/vater/VaterExerciseCreate.tsx:119-134](../../frontend/src/vater/VaterExerciseCreate.tsx#L119-L134))
  baut die Payload **ohne** `executePublic` — jede neu angelegte Übung wird also stillschweigend öffentlich,
  ohne dass der Creator eine Entscheidung sieht oder trifft. `e2e/freigabe.spec.ts:38` hält das sogar als
  erwartetes Verhalten fest: „Eine eigene Übung anlegen – sie ist standardmäßig für alle zuweisbar."
- **Der DB-Default ist bewusst, nicht vergessen**: `PuglingDbContext.cs:469`
  (`modelBuilder.Entity<Exercise>().Property(e => e.ExecutePublic).HasDefaultValue(true)`) mit Kommentar:
  „neue Übungen sind öffentlich zuweisbar, solange niemand widerspricht" — der **einzige** DB-Default im
  Schema (siehe [backend/Pugling.Api/CLAUDE.md](../../backend/Pugling.Api/CLAUDE.md) → „Schema &
  Migrationen", Fail-Safe-Regel), von `SchemaGuardTests` gehalten.

## Die echte Lücke

Nicht „es gibt keinen Publikations-Zustand" (den gibt es, vollständig gebaut, getestet, dokumentiert).
Die Lücke ist **eine einzige fehlende UI-Zeile**: das Anlage-Formular fragt nicht, obwohl der Server das
Feld längst annimmt. Ein Creator, der eine Übung von Anfang an privat halten will, muss heute **anlegen
(→ kurz öffentlich) → in die Verwaltung wechseln → „Verwendung" aufklappen → zurückziehen** — ein Umweg über
vier Schritte für eine Entscheidung, die beim Anlegen genauso gut fällt.

## Offene Punkte

1. ~~Gibt es bereits einen Weg, mit dem ein Creator Sichtbarkeit/Zuweisbarkeit ausdrücklich entscheidet?~~
   → siehe Entscheidung 1 (ja, vollständig; die Notiz war beim Ernten schon veraltet).
2. ~~Soll der Default bei Neuanlage von „öffentlich" auf „privat" gedreht werden, damit „veröffentlichen"
   ein bewusster Schritt bleibt statt eines impliziten?~~ → siehe Entscheidung 2 (nein).
3. ~~Wo bekommt die ausdrückliche Entscheidung ihren Platz, wenn nicht über den Default?~~ → siehe
   Entscheidung 3.
4. ~~Braucht es einen zweiten Umschalter (Anlage-Formular UND Bearbeiten-Dialog), oder bleibt die
   nachträgliche Änderung an ihrem bestehenden Ort?~~ → siehe Entscheidung 4.
5. ~~Braucht es ein zweites Rechte-Konzept neben Grant/`ExecutePublic`?~~ → siehe Entscheidung 5 (nein).

## Entscheidungen

1. **Die Story wird nicht `verworfen`, sondern auf die reale Restlücke verengt.** Begründung: Das in der
   Idee beschriebene Fehlen eines Publikations-Umschalters existiert nicht (mehr) — Commit `88ed858` hat ihn
   zwei Tage vor der Notiz gebaut. Eine leere Story wäre aber selbst Verlust an Information: die
   E2E-dokumentierte Lücke am Anlage-Zeitpunkt ist real, klein und im Titel der Idee („**ausdrücklich**
   veröffentlichen") bereits angelegt. Kosten: der Titel bleibt, der Anspruch schrumpft — im Verlauf
   dokumentiert, nicht verschwiegen.
2. **Der Fail-Safe-Default (`ExecutePublic = true`) bleibt unverändert.** Begründung: Er ist eine bewusste,
   kommentierte Architekturentscheidung (`PuglingDbContext.cs:469`, „solange niemand widerspricht"), der
   *einzige* geduldete DB-Default im Schema, und von einem bestehenden E2E-Test als erwartetes Verhalten
   festgehalten (`freigabe.spec.ts:38`). Ein Wechsel zu „privat by default" bräche dieses Verhalten für
   *jede* neu angelegte Übung lautlos, sobald ein Creator das neue Feld übersieht — genau das Gegenteil von
   „ausdrücklich". Kosten: keine (Status quo bleibt bestehen); **verworfen**: „privat by default" (siehe
   Begründung), ebenso ein zweiter DB-Default (verletzt „genau ein DB-Default").
3. **Die Entscheidung bekommt ein Feld im Anlage-Formular, vorbelegt mit `true`.** `VaterExerciseCreate.tsx`
   erhält eine Checkbox „Für andere Betreuer zuweisbar" (Arbeitstitel), die den bereits vom Server
   akzeptierten `executePublic`-Wert in die Payload schreibt (aktuell fehlt die Zeile komplett, Zeilen
   119-134). Begründung: macht den bestehenden Default **sichtbar** statt ihn implizit zu lassen — das ist
   der Unterschied zwischen „passiert automatisch" und „wurde ausdrücklich entschieden", ohne den Default
   selbst anzufassen. Kosten: ein Formularfeld, ein `fieldHelp.ts`-Eintrag, keine Migration, kein
   Vertragsbruch (Feld existiert im DTO bereits additiv).
4. **Kein zweiter Umschalter im Bearbeiten-Dialog.** Die nachträgliche Änderung bleibt exklusiv am
   bestehenden Ort (`PATCH …/sharing`, Verwendungs-Anzeige in `VaterExercises.tsx`); `MetaEditor` /
   `ExerciseEditModal.tsx:112` reicht `executePublic` weiterhin unverändert durch, wie heute. Begründung:
   zwei Bedienelemente für dieselbe Einstellung laufen erfahrungsgemäß in Text/Verhalten auseinander (genau
   die Art Drift, die dieses Projekt bei Vater/Adult teuer bezahlt hat); die Owner-Prüfung
   (`ExerciseControllerBase.cs:305`) müsste sonst doppelt bedacht werden. Kosten: die Checkbox im
   Anlage-Formular ist eine **einmalige** Initial-Entscheidung, keine dauerhafte Einstellung im
   Bearbeiten-Formular — wer später umentscheiden will, nutzt weiter „Zurückziehen"/„Freigeben".
5. **Kein zweites Rechte-Konzept.** `ExerciseGrant`/`GrantPermission` bleibt die einzige Rechte-Schicht;
   `ExecutePublic` bleibt ihr Fail-Safe, keine neue Sichtbarkeits-Stufe (z. B. „unsichtbar für Read") wird
   eingeführt. Begründung: das ist eine bewusste, kommentierte Architekturentscheidung
   (`ExercisePermissionService.cs:16`, `LearnBaseTypes.cs:44-45`) mit eigener Begründung (geteilte
   Bibliothek bleibt durchsuchbar); sie zu ändern wäre ein eigenes, größeres Vorhaben ohne Anlass in dieser
   Idee. Kosten: „privat" heißt weiterhin „nicht neu zuweisbar", nicht „unsichtbar" — das muss der Hilfetext
   klarstellen (siehe Risiken).

## Nach B-106

Keine Auswirkung auf diese Story. `Exercise.ExecutePublic` und `PATCH …/sharing` hängen an der Übung
selbst, nicht an ihrem Kapitel/ihrer Unit — B-106 hat `Exercise.ChapterId` durch `Exercise.SeriesUnitId`
ersetzt, aber `ExecutePublic` unverändert gelassen
([LearnEntities.cs:37,68](../../backend/Pugling.Api/Models/LearnEntities.cs), nachgeprüft 2026-08-06).
Die einzige Textstelle, die sich geändert hat, ist kosmetisch: die Katalogsuche liest das Fach jetzt
transitiv über `SeriesUnit.Series.SubjectId` statt über `Chapter.SubjectId`
([ExerciseCatalogController.cs:60-61](../../backend/Pugling.Api/Controllers/Creator/ExerciseCatalogController.cs)),
ändert aber nichts an dieser Story — sie berührt weder Suche noch Sortierung, nur das Anlage-Formular.

**Empfehlung: bleibt gültig, unverändert.**

## Akzeptanzkriterien

1. Das Anlage-Formular (`/vater/exercises/neu`) zeigt ein Feld, mit dem der Creator vor dem Absenden
   entscheidet, ob die Übung sofort für andere Betreuer zuweisbar ist; vorbelegt mit „ja" (unveränderter
   Server-Default).
2. Wird das Feld auf „nein" gestellt, entsteht die Übung mit `executePublic: false` — ohne Umweg über
   Anlegen → Verwalten → Zurückziehen.
3. Ein `fieldHelp.ts`-Eintrag erklärt den Unterschied zu „Zurückziehen" (dasselbe Flag, nur der Zeitpunkt
   der Entscheidung ändert sich) und stellt klar: „privat" bedeutet **nicht zuweisbar**, nicht unsichtbar —
   der Katalog bleibt für jeden Creator durchsuchbar.
4. Kein zweiter Umschalter entsteht: `ExerciseEditModal`/`MetaEditor` ändert sein Verhalten nicht (reicht
   `executePublic` weiter unverändert durch); die einzige nachträgliche Änderung bleibt
   `PATCH …/exercises/{id}/sharing`.
5. Backend unverändert: keine neue Route, kein neues DTO, kein neuer `ApiErrors`-Code — `ExercisePayload`
   akzeptiert das Feld bereits.
6. `frontend/e2e/freigabe.spec.ts` bleibt grün (der Default-Fall „öffentlich anlegen" darf sich nicht
   ändern); ein neuer Test deckt den Fall „privat anlegen" ab.

## Schätzung

**Größe: S** — vergleichbar mit B-01 (`childId` aus dem Testpfad ziehen): ein Formularfeld, eine
Payload-Zeile, ein Hilfetext, ein Test. Kein Backend-Feature fehlt, keine Migration, kein Vertragsbruch.

- **`wo: frontend`** — das Backend nimmt `executePublic` beim Anlegen bereits entgegen
  (`ExerciseControllerBase.cs:258`); es fehlt ausschließlich die Bedienoberfläche.
- **`migration: nein`** — `Exercise.ExecutePublic` existiert als Spalte samt DB-Default seit dem
  RWX-Umbau (Commit `d1bcd0f`); nichts am Schema ändert sich.
- **`vertragsbruch: nein`** — `ExercisePayload.ExecutePublic` ist bereits additiv seit demselben Umbau;
  das Frontend befüllt lediglich ein längst bestehendes optionales Feld.
- **Risiken:**
  1. Verwechslungsgefahr „privat" = „unsichtbar" (ist es nicht — Read bleibt global). Der Hilfetext muss
     das explizit widerlegen, sonst erwartet ein Creator versehentlich Geheimhaltung seiner Inhalte.
  2. Ein neues Formularelement auf `/vater/exercises/neu` kann bestehende Playwright-Locators (Strict-Mode)
     stören, falls das Label mit einem vorhandenen Text kollidiert — vor dem Schreiben des neuen Tests
     `freigabe.spec.ts` unverändert laufen lassen.
  3. Das Feld darf **nicht** wie ein zweiter Dauer-Schalter wirken (siehe Entscheidung 4) — Formulierung
     als „Startzustand", nicht als „Einstellung", hält das auseinander.
- **Angriffsplan** (hier ausnahmsweise ohne Backend-Schritt, da nichts zu bauen ist):
  1. `VaterExerciseCreate.tsx`: `useState` für die Checkbox (default `true`), ins `payload` (Zeile 119-134)
     als `executePublic` aufnehmen.
  2. `fieldHelp.ts`: neuen `HelpTopic`-Eintrag samt `FieldLabel` am neuen Feld (Muster: bestehende
     Checkbox-Zeilen im selben Formular, `<span className="label-row">`).
  3. Neuer Playwright-Test (Muster `freigabe.spec.ts`): Checkbox abwählen → anlegen → in der Verwaltung
     erscheint „zurückgezogen", ohne den Umweg über den „Zurückziehen"-Knopf.
  4. `frontend-reviewer` vor Commit (Konventionen: Schreib-Primitive, `fieldHelp`-Pattern, A11y-Label).
- **Testweg:** neuer Playwright-Test im Muster von `frontend/e2e/freigabe.spec.ts`, ergänzt um den Fall
  „privat anlegen"; `frontend/e2e/freigabe.spec.ts` selbst dient als Regressionsschutz für den
  unveränderten Default-Fall. Kein neuer Backend-Test nötig — `ExerciseSharingTests.cs` und
  `ExerciseGrantsTests.cs` decken das Flag bereits vollständig ab.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft, Quelle `memory/geteilte-uebungs-bibliothek.md`).
- **2026-08-03** — ausformuliert gegen den Code: Die Notiz war bereits beim Ernten veraltet — `ExecutePublic`
  + `PATCH …/sharing` existieren vollständig seit Commit `88ed858` (2026-07-28, zwei Tage vor der Notiz),
  mit Frontend, Doku und E2E-Test. Die reale Lücke ist auf den Anlage-Zeitpunkt verengt.
- **2026-08-03** — gegrillt (autonom getroffen, Nutzerauftrag 2026-08-04): fünf offene Punkte aufgelöst;
  Kernentscheidung, den Fail-Safe-Default nicht anzutasten und die Lücke auf ein einzelnes Formularfeld zu
  verengen, statt die Story mangels Restumfang zu verwerfen.
- **2026-08-03** — geschätzt (autonom getroffen, Nutzerauftrag 2026-08-04): **S**, `wo: frontend`,
  `migration: nein`, `vertragsbruch: nein`. Kein XL-Split nötig — im Gegenteil, der Umfang schrumpfte beim
  Recherchieren auf eine einzelne UI-Zeile. Nicht umgesetzt.
- **2026-08-06** — Nachtlauf, Prämissen-Nachprüfung nach B-106s Abnahme: keine Auswirkung, `ExecutePublic`
  hängt an der Übung selbst, nicht an ihrem Kapitel/ihrer Unit. `status` unverändert.
- **2026-08-07** — Autonomer Modus (Opt-in je Vorhaben, README → „Autonomer Modus") vom Nutzer im Dialog
  ausdrücklich freigegeben: ein Nachtlauf darf diese Story trotz `art: Wunsch` ohne weitere Rückfrage bauen
  (Rollengang/Reviewer bleiben Pflicht wie bei jeder Abnahme).
