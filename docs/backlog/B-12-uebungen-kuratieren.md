---
tags: [typ/story, status/in-arbeit, bereich/katalog, rolle/creator]
aliases: [Rating, Kuratierung]
status: in-arbeit
prio: P3
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: memory/geteilte-uebungs-bibliothek.md
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-12 · Geteilte Übungen bewerten und kuratieren

In einer geteilten Bibliothek braucht es einen Weg, Gutes nach oben zu bringen — Bewertung, Kuratierung
oder redaktionelle Auswahl. Existiert nicht: Der Katalog kennt heute keine Bewertung, kein Melde- und
kein Kuratierungssignal für Übungen.

## User Story

Als Creator möchte ich eine fremde, geteilte Übung mit einer kurzen Bewertung versehen und den Schnitt
anderer Creator sehen, damit ich beim Durchsuchen des Katalogs erkenne, welches Material sich bewährt hat,
bevor ich es in einen Lehrplan übernehme.

## Ist-Stand am Code

- **Der Katalog ist global lesbar, RWX ist granular geregelt.** `Exercise.AuthorAdultId`/`Grants` und das
  Kommentar-Paar dazu erklären das Prinzip ausdrücklich: „every adult may find and use every exercise, but
  only the author may change or delete it"
  (`backend/Pugling.Api/Models/LearnEntities.cs:98-107`). Rechte laufen über `ExerciseGrant`
  (`Id, ExerciseId, CreatorId, Permission, GrantedByAdultId, CreatedAt`,
  `backend/Pugling.Api/Models/LearnEntities.cs:129-141`) mit `GrantPermission` `Owner ⊃ Write ⊃ Execute`
  (`backend/Pugling.Contracts/Common/LearnBaseTypes.cs:47`) — **Read ist bewusst kein Recht**
  (`backend/Pugling.Api/Controllers/Creator/ExerciseGrantsController.cs:14`).
- **`Exercise.ExecutePublic`** (Default `true`, `backend/Pugling.Api/Models/LearnEntities.cs:110-115`) ist
  der bestehende Sichtbarkeits-Schalter: „anyone may assign it" bzw. nur Owner/Grant-Inhaber, wenn auf
  `false` gesetzt. Umgeschaltet wird er über `PATCH api/v1/creator/exercises/{id}/sharing`
  (`ExerciseCatalogController.SetSharing`, `backend/Pugling.Api/Controllers/Creator/ExerciseCatalogController.cs:170-183`,
  nur Owner/`CanAdministerAsync`, DTO `SetExerciseSharingDto`/`ExerciseSharingResponse` in
  `backend/Pugling.Contracts/Creator/ExerciseGrantDtos.cs:24` und `:56`). Dieser Schalter existiert schon
  vor B-11 und wird von diesem seit `9f9c185` unverändert genutzt (`git log` auf die Datei) — B-11s
  „`published`-Flag" ist also faktisch schon da, nur nicht so benannt.
- **Discovery**: `GET api/v1/creator/exercises` (`ExerciseCatalogController.Search`,
  `backend/Pugling.Api/Controllers/Creator/ExerciseCatalogController.cs:43-101`) durchsucht **alle**
  Übungen aller Creator (Filter `subjectId/chapterId/grade/schoolType/categoryId/type/search`), mit
  `mineOnly` (:36, :56-58) als reiner Verwaltungs- statt Entdeckungsfilter. Sortierbar ist nach
  `title/type/grade/source/created` (`ApplySort`, :108-124) — **keine** Qualitäts- oder Beliebtheits-Spalte.
  Die Antwort (`ExerciseSummary`, `backend/Pugling.Contracts/Creator/ExerciseCatalogDtos.cs:13-16`) trägt
  `AuthorAdultId`/`AuthorName` zur Anzeige, aber kein Qualitätssignal.
- **Nutzungszahlen existieren, aber nur als Lösch-Sperre, nicht als Kuratierungssignal.**
  `GET .../{id}/usage` (`ExerciseCatalogController.Usage`, :196-225) liefert `Plans`, `ClassTests` (nur der
  eigenen Kinder) und `OtherLearnersCount` (Kinder anderer Supervisor, ohne Namen) — Zweck ist die
  Lösch-Blockade (`UsageResponse`, `backend/Pugling.Contracts/Creator/ExerciseCatalogDtos.cs:57-58`,
  Kommentar zu „409 exercise_in_use"), nicht ein Popularitäts- oder Qualitätswert im Suchergebnis.
- **Keine Bewertungs-, Melde- oder Kuratierungsfunktion.** Grep über `Rating|Review|Flag|Report|Curat|Feedback|Quality`
  in `backend/Pugling.Api` (Models, Controllers, Services) liefert ausschließlich Fehltreffer:
  `ContentRating` ist die Medien-Altersfreigabe (`backend/Pugling.Api/Models/MediaEntities.cs:34`,
  `AdminEntities.cs:105`), "Review" bezeichnet die Leitner-Wiederholung beim Üben
  (`PositionPracticeController.Review`, s. `backend/Pugling.Api/CLAUDE.md`), und
  `TagsRatingsTimetableTests.cs` ist eine Namenskollision (Zeitfenster-Tests, kein Rating-Feature). Es gibt
  kein `ExerciseRating`, kein `Flag`/`Report` je im Sinne „Übung melden" und keine redaktionelle Auswahl
  (kein „featured"/„curated"-Feld am `Exercise`-Modell, `backend/Pugling.Api/Models/LearnEntities.cs:51-121`).
- **`Anmerkungen`** (`api/v1/remarks`) sind kein Ersatz: Dev-Werkzeug zum Testen der App selbst
  (`backend/Pugling.Api/CLAUDE.md`, Abschnitt „Anmerkungen"), nicht ein Produktfeature zur
  Katalog-Kuratierung durch Creator.

## Die echte Lücke

Nicht das Fehlen einer Veröffentlichungs-Schranke (die gibt es längst über `ExecutePublic`/`sharing`,
s. o.) — sondern das Fehlen jedes **Qualitätssignals**, sobald eine Übung sichtbar/nutzbar ist. Ein Creator,
der im globalen Katalog sucht, sieht Titel, Typ, Klassenstufe, Autor — aber nichts, was zwischen einer
sorgfältig gebauten und einer hingeworfenen Übung unterscheidet. „Gutes nach oben bringen" heißt hier
konkret: ein Bewertungswert je Übung, sichtbar in Suche und Detail, gesetzt von den Creators, die die Übung
tatsächlich nutzen — nicht vom Autor selbst.

## Offene Punkte

~~Ob das überhaupt gebraucht wird, solange die Bibliothek klein ist?~~ → siehe Entscheidung 1.
~~Ob eine Bewertung kindneutral bleibt oder am Erfolg der Kinder hängt?~~ → siehe Entscheidung 3.
~~Wer darf bewerten (nur Nutzer der Übung, oder jeder Creator)?~~ → siehe Entscheidung 2.
~~Braucht es zusätzlich einen Melde-/Flag-Weg für fehlerhafte Übungen?~~ → siehe Entscheidung 5.
~~Verhältnis zu B-11 (`published`-Konzept)?~~ → siehe Entscheidung 7.

## Entscheidungen

1. **Bewertungsform: Sterne 1–5 plus optionaler kurzer Kommentar, kein Report-/Flag-Workflow.**
   Ein Skalarwert ist sofort vergleich- und sortierbar (`sort=rating`), ein optionales Freitextfeld kostet
   fast nichts zusätzlich (ein nullable `string`, wie `Exercise.Description`). Begründung für „ja, jetzt
   bauen" trotz kleiner Bibliothek: Der Aufwand ist klein (eine Tabelle, ein Controller, additive DTO-Felder,
   s. Schätzung) und das Signal wird *mit* der Bibliothek wertvoller, nicht erst danach — jede spätere
   Übung profitiert von am Tag 1 vorhandenen Bewertungen der ersten. Kosten: bei sehr wenigen Bewertern ist
   der Schnitt statistisch instabil (siehe Risiken).
2. **Bewerten darf jeder Creator außer den Owner-/Administer-Berechtigten der Übung selbst** (Check über
   `ExercisePermissionService.CanAdminister`, dieselbe Prüfung wie bei `SetSharing`). Eine zusätzliche
   Hürde „nur wer die Übung tatsächlich in einem eigenen Plan zugewiesen hat" wurde geprüft und verworfen:
   sie bräuchte eine Nutzungsabfrage über `PlanPosition`/`KlassenarbeitExercise` bei jedem Bewertungsversuch
   und würde die Story von M Richtung L schieben. Kosten: ein Creator kann theoretisch bewerten, ohne die
   Übung je gespielt zu haben — akzeptiertes Risiko in einem kleinen, vertrauten Kreis (Familie/Lehrer),
   siehe Risiken.
3. **Die Bewertung ist rein subjektiv vom bewertenden Creator, nicht aus Kinddaten (Erfolgsquote,
   Abschlussrate) abgeleitet.** Lernerfolg hängt von zu vielen Störfaktoren ab (Motivation, Vorwissen,
   Tagesform des Kindes), um ein Qualitätssignal für das *Material* zu sein — eine gute Übung an einem
   unmotivierten Tag sähe schlecht aus. Kosten: kein automatisches, „ehrliches" Signal ohne menschliches
   Zutun; das Bewerten bleibt eine bewusste Handlung des Creators.
4. **Ein Rating pro (Übung, Creator), überschreibbar (PUT-Semantik), kein Verlauf mehrerer Bewertungen
   derselben Person.** Eindeutiger unique Index `(ExerciseId, CreatorId)`, analog zum bestehenden
   `(ExerciseId, CreatorId, Permission)`-Index auf `ExerciseGrant`
   (`backend/Pugling.Api/Data/PuglingDbContext.cs:478-487`). `UpdatedAt` trägt die Änderungshistorie als
   Zeitstempel, nicht als Zeilen-Historie. Kosten: ein Meinungswechsel überschreibt die alte Bewertung
   spurlos — für ein Qualitätssignal (kein Audit-Trail) akzeptabel.
5. **Kein Melde-/Flag-Mechanismus für fehlerhafte Übungen in dieser Story.** Das ist eine andere
   Fähigkeit („etwas ist kaputt" statt „das ist gut/schlecht") mit eigenem Eskalationsweg zum Owner: sie in
   dieselbe Story zu packen, würde sie XL machen und zwei unabhängige Entscheidungsketten vermischen. Wird
   bei Bedarf eine eigene Idee — nicht Teil dieser Schätzung.
6. **Durchschnitt (`AverageRating`) und Anzahl (`RatingCount`) additiv in `ExerciseSummary`/`ExerciseDetail`,
   für jeden Creator sichtbar — nicht Owner-only.** Konsistent mit „Read ist kein Recht"
   (`ExerciseGrantsController.cs:14`): wer die Übung sehen und nutzen darf, darf auch ihre Bewertung sehen.
   Einzelbewertungen (Sterne, Kommentar, Name des Bewerters, Zeitstempel) liegen hinter einem eigenen
   `GET .../ratings`, ebenfalls ohne Owner-Beschränkung — Creator-Namen sind im Katalog ohnehin sichtbar
   (`AuthorName` in `ExerciseSummary`). Kosten: ein kritischer Kommentar ist für den Owner namentlich
   nachvollziehbar; das ist im Rahmen von Kuratierung gewollt, nicht versehentlich.
7. **Verhältnis zu B-11**: `docs/backlog/B-11-uebungen-veroeffentlichen.md` ist inzwischen (von anderer
   Hand, parallel zu dieser Recherche) selbst bis `geschaetzt` gelaufen — mit demselben Befund wie hier:
   „kein zweites Rechte-Konzept" (B-11, Entscheidung 5), `ExecutePublic`/`PATCH …/sharing` bleibt die
   *einzige* Sichtbarkeits-Schicht, B-11 ergänzt nur eine fehlende UI-Zeile beim Anlegen, kein neues Modell.
   B-12 referenziert also ein **festgelegtes**, nicht mehr offenes Konzept und baut direkt auf
   `Exercise.ExecutePublic` auf. Das in der ursprünglichen Fassung dieser Entscheidung vermerkte Risiko
   (ein künftiges, von `ExecutePublic` getrenntes Draft/Published/Archived-Modell) entfällt damit — B-11
   hat diese Erweiterung ausdrücklich verworfen.
8. **Suche sortierbar nach Bewertung** (`sort=rating`, neuer Fall in `ApplySort`,
   `ExerciseCatalogController.cs:108-124`). Ohne diese Erweiterung wäre der Durchschnittswert nur Zierde in
   der Detailansicht, nicht nutzbar beim Stöbern — genau das Szenario der User Story („beim Durchsuchen des
   Katalogs erkennen"). Kosten: ein weiterer Sortier-Fall, den der Endpunkt-Abdeckungs-Wächter mit abdeckt.

## Nach B-106

Keine Auswirkung auf diese Story. Das RWX-/`ExecutePublic`-Modell, auf dem B-12 aufbaut, hängt an der
Übung selbst und ist von B-106 unverändert
([LearnEntities.cs:68,98-141](../../backend/Pugling.Api/Models/LearnEntities.cs), nachgeprüft
2026-08-06) — B-106 hat nur den Pflicht-Anker der Übung von `Chapter` auf `SeriesUnit` verschoben, nicht
ihr Rechte-/Sichtbarkeitsmodell. Der geplante `ExerciseRatingsController` (Muster
`ExerciseGrantsController`) und die Sortier-Erweiterung `sort=rating` in `ApplySort`
([ExerciseCatalogController.cs:108-124](../../backend/Pugling.Api/Controllers/Creator/ExerciseCatalogController.cs))
bleiben unabhängig von der Chapter→SeriesUnit-Verschiebung.

**Empfehlung: bleibt gültig, unverändert.**

## Akzeptanzkriterien

1. Ein Creator kann eine fremde Übung mit 1–5 Sternen und optionalem Kommentar bewerten
   (`PUT api/v1/creator/exercises/{id}/rating`).
2. Ein Owner/Administer-Berechtigter der Übung erhält beim Versuch, sie zu bewerten, `403` mit einem
   eigenen, additiven `ApiErrors`-Code (z. B. `cannot_rate_own_exercise`).
3. `ExerciseSummary` und `ExerciseDetail` tragen zusätzlich `AverageRating` (`double?`, `null` ohne
   Bewertung) und `RatingCount` (`int`, `0` ohne Bewertung) — kein Fehler bei fehlenden Bewertungen.
4. `GET api/v1/creator/exercises/{id}/ratings` listet alle Einzelbewertungen (Sterne, Kommentar,
   Bewerter-Name, `CreatedAt`/`UpdatedAt`), sichtbar für jeden Creator (nicht nur den Owner).
5. Ein erneutes `PUT` derselben Person überschreibt die eigene Bewertung; es entsteht nie eine zweite Zeile
   je `(ExerciseId, CreatorId)`. `DELETE api/v1/creator/exercises/{id}/rating` entfernt die eigene Bewertung
   wieder.
6. `GET api/v1/creator/exercises?sort=rating` (und `-rating`) sortiert nach dem Durchschnitt, unbewertete
   Übungen in **beiden** Richtungen am Ende (geändert 2026-10-26, Morgen-Entscheidung) — Tie-Breaker bleibt `Id` wie bei den anderen
   Sortierfällen.
7. Im Vater-Web zeigt die Übungsliste (`VaterExercises.tsx`) Durchschnitt + Anzahl an; ein
   Bewertungs-Widget (Schreib-Primitiv `useAction`, analog `ExerciseSharingPanels.tsx`) erlaubt das Setzen/
   Ändern/Löschen der eigenen Bewertung für fremde Übungen und blendet sich für eigene Übungen aus
   (konsistent mit Akzeptanzkriterium 2).

## Schätzung

**Größe: M** — neue Entity + Migration, ein neuer Controller (Analog `ExerciseGrantsController`, drei
Endpunkte), additive Erweiterung zweier bestehender DTOs, eine neue Sortier-Option, plus ein schlankes
Frontend-Widget. Vergleichbar mit B-65 (Vokabel mit zwei Übersetzungen: Migration + Contract-Erweiterung +
Frontend) — nicht XS/S (mehr als ein Feld/eine Zeile), nicht L (kein Umbau bestehender Abläufe, reine
Ergänzung).

- **wo**: `beides` — Backend zuerst (API-First), danach das Frontend-Widget; ein Backend-Endpunkt ohne UI
  widerspricht der Frontend-Norm „jeder Backend-Endpunkt braucht ein UI".
- **migration**: `ja` — neue Tabelle `ExerciseRating` (`Id, ExerciseId, CreatorId, Stars, Comment?,
  CreatedAt, UpdatedAt`), unique Index `(ExerciseId, CreatorId)`; die Migrationskette wird wie gewohnt neu
  gefaltet (`rm -rf backend/Pugling.Api/Data/Migrations` + `migrations add InitialCreate`).
- **vertragsbruch**: `nein` — ausschließlich additive Felder (`AverageRating`, `RatingCount`) und neue
  Records (`RateExerciseDto`, `ExerciseRatingResponse`); keine bestehende Signatur ändert sich brechend.

**Risiken:**

- Instabiler Schnitt bei wenigen Bewertern (eine einzelne Fünf- oder Ein-Stern-Bewertung dominiert den
  Durchschnitt) — akzeptiert für den Start, keine Mindestanzahl vor Anzeige vorgesehen (Entscheidung 1).
- Kein Nutzungsnachweis vor dem Bewerten (Entscheidung 2) — theoretisch bewertbar ohne je gespielt zu
  haben; im vertrauten Familien-/Lehrer-Kreis als tragbar eingeschätzt.
- Namentlich sichtbare Kritik am Material kann zwischen Creators unangenehm werden — kein
  Melde-/Eskalationsweg vorgesehen (Entscheidung 5), bewusst aus der Story herausgehalten.
- Keine Abhängigkeit zu B-11 mehr: das dort befürchtete zweite Rechte-/Sichtbarkeits-Konzept wurde
  dort ausdrücklich verworfen (Entscheidung 7) — `ExecutePublic` bleibt die einzige Schicht, auf der
  diese Story aufbaut.

**Angriffsplan** (Backend zuerst):

1. `ExerciseRating`-Entity + `DbSet` + Fluent-Config (unique Index, String-Länge `Comment`) in
   `backend/Pugling.Api/Models/LearnEntities.cs` / `Data/PuglingDbContext.cs`; Migrationskette neu falten.
2. `ApiErrors` um `CannotRateOwnExercise` (403) ergänzen (`backend/Pugling.Api/Errors/ApiErrors.cs`).
3. Contracts: `RateExerciseDto`, `ExerciseRatingResponse` (neue Datei oder `ExerciseCatalogDtos.cs`),
   `AverageRating`/`RatingCount` additiv in `ExerciseSummary`/`ExerciseDetail`
   (`backend/Pugling.Contracts/Creator/ExerciseCatalogDtos.cs`).
4. Neuer `ExerciseRatingsController` (Muster `ExerciseGrantsController.cs`): `GET/PUT/DELETE
   .../exercises/{id}/rating(s)`, Owner-Ausschluss über `ExercisePermissionService.CanAdminister`.
5. `ApplySort` um `rating` erweitern (`ExerciseCatalogController.cs:108-124`), `Search`/`Get` um die
   Aggregation ergänzen (Subquery/Projektion, kein N+1 über `Include`).
6. `Pugling.Client`: eine einzeilige Methode je neuem Endpunkt ergänzen.
7. `SchemaGuardTests` (G1–G9) um die neue Beziehung/den neuen Unique-Index ergänzen.
8. Frontend: `ExerciseRatingPanel` (oder Erweiterung von `ExerciseSharingPanels.tsx`) mit `useAction`,
   Einbindung in `VaterExercises.tsx` (Durchschnitt in der Liste, Bewertungs-Widget im Detail).

**Testweg**: neue Integrationstestklasse `ExerciseRatingsTests` in `backend/Pugling.Api.Tests` (Muster
`ExerciseGrantsTests.cs`) — Bewerten/Ändern/Löschen, Owner-Verbot (403), Aggregation bei 0/1/mehreren
Bewertungen, Sortierung `sort=rating`. Der Endpunkt-Abdeckungs-Wächter deckt die drei neuen Actions
automatisch ab; `SchemaGuardTests` prüft die neue Tabelle/den Index; `/smoke-test` für den
End-to-End-Rundlauf nach dem Bauen. Kein Playwright-E2E zwingend (kein Vater→Sohn-Durchstich betroffen).

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Ist-Stand gegen den Code belegt (`ExerciseGrant`, `ExecutePublic`/
  `SetSharing`, `ExerciseCatalogController.Search`/`Usage`); bestätigt, dass keine Bewertungs-, Melde- oder
  Kuratierungsfunktion existiert (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — gegrillt: acht Entscheidungen selbst getroffen (Sterne+Kommentar, Owner-Ausschluss,
  kindneutrale Bewertung, ein Rating je Person, kein Melde-Weg, additive Sichtbarkeit, Verhältnis zu B-11,
  Sortierung) (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschätzt: Größe M, `wo: beides`, `migration: ja`, `vertragsbruch: nein`, Angriffsplan
  und Testweg festgelegt (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-06** — Nachtlauf, Prämissen-Nachprüfung nach B-106s Abnahme: keine Auswirkung, das RWX-/
  `ExecutePublic`-Modell ist von der Chapter→SeriesUnit-Verschiebung unberührt. `status` unverändert.
- **2026-08-07** — Autonomer Modus (Opt-in je Vorhaben, README → „Autonomer Modus") vom Nutzer im Dialog
  ausdrücklich freigegeben: ein Nachtlauf darf diese Story trotz `art: Wunsch` ohne weitere Rückfrage bauen
  (Rollengang/Reviewer bleiben Pflicht wie bei jeder Abnahme).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): gilt, kein Teil geliefert (kein `ExerciseRating`/`AverageRating` im Code). Zeilen verschoben (`ApplySort` → `ExerciseCatalogController.cs:133-150`, `Usage` → `:225`, `Exercise` → `LearnEntities.cs:77`, `ExerciseGrant` → `:189`); der Filter `chapterId` heißt heute `seriesUnitId` (`:49`). Nachtlauffähig (`migration: ja` – nicht im selben Sprint wie eine andere Migration).
- **2026-10-26** — Nachtlauf (7. Lauf), Sprint 12 / NP-8: gebaut. Prämisse vorher am laufenden Vertrag geprüft
  (`docs/openapi/v1.json`: kein Bewertungs-Endpunkt, `rating` nur als Medien-Altersfreigabe). Backend: Entity
  `ExerciseRating` (unique `(ExerciseId, CreatorId)`, beide FKs Cascade), Kette neu gefaltet (eine Migration),
  `ExerciseRatingsController` (`GET …/ratings`, `PUT`/`DELETE …/rating`), `cannot_rate_own_exercise` (403),
  `AverageRating`/`RatingCount` an `ExerciseSummary`/`ExerciseDetail`, `sort=rating` (unbewertete aufsteigend am Ende,
  absteigend am Anfang – AK 6 wörtlich), Client-Methoden. Frontend: `ExerciseRatingPanel` (Liste + eigenes Formular,
  für Owner ausgeblendet), Zeilentext „★ 3,5 (2 Bewertungen)“, Sortieroption „Bewertung“, Zeile als benannte Gruppe.
  Tests: `ExerciseRatingsTests` (8), Vitest-Helfer (5), E2E `e2e/uebungen-bewerten.spec.ts`.
- **2026-10-26** — Rote Proben (Auflage 5): 13 Backend-Mutanten gegen `ExerciseRatingsTests`, erwartet je ≥ 1 rot am
  benannten Fall, gemessen **13 von 13 rot** (Owner-Ausschluss weg / über Schreibrecht, PUT legt neue Zeile an,
  Max statt Schnitt, Sortierung ohne Leitschlüssel, nach Anzahl, Liste nur eigene, Delete entfernt alle, keine
  Sterneprüfung, Detail-Anzahl 0, Kommentar ungetrimmt, absteigend nicht gespiegelt, alter Kommentar bleibt).
  Nach dem Review: IsMine in der Liste immer `false` → **1 rot** (vorher grün laut Reviewer), Sternegrenze `> 4` →
  **2 rot**. Fünf UI-Mutanten gegen die Spec, je **1 failed** an der erwarteten Zeile (Formular für Owner, Zeile zieht
  nicht nach, Anzahl statt Schnitt, Formular liest nicht zurück, Sortieroption fehlt); nach Fund 5 zwei weitere
  (Bewertung schickt `created` bzw. `title`) → je **1 failed** an `:80`. Ein Nebenläufigkeitstest für Fund 2
  (8 parallele erste PUTs) blieb gegen den Stand ohne Abfangen **3 von 3 Läufen grün** – er trug nichts und wurde
  wieder entfernt; das Rennen ist in-process nicht deterministisch auslösbar (wie `concurrency_conflict`).
- **2026-10-26** — Review (Freigabe 3, Zähler Sprint 12): Fund 1 roter Wächter `Unpaginierte_Array_GETs_Sind_Gepinnt`
  (Pin 34 → 35, begründet); `pugling-reviewer`: Fund 2 Rennen beim ersten PUT → 500 (behoben, Muster
  `DailyBoxService`), Fund 3 `isMine` der Liste nie `true` geprüft (behoben), Fund 4 Setup-PUTs/Grants ungeprüft
  (behoben); Hinweise 6/7 → [B-269](B-269-bewertungen-reihenfolge-und-updatedat-ungetestet.md). `frontend-reviewer`:
  Fund 5 Sortier-Zusicherungen bestanden auch mit Titel-/Anlage-Reihenfolge (behoben, Titel und Anlage laufen jetzt
  entgegen). **Fund 6** (`isOwner`/`isOwn`-Abgrenzung ungetestet: der Autor hält Owner *und* Write, die Mutante
  `exercise.isOwn ? Hinweis : Formular` bliebe grün) überschreitet die Grenze von fünf – **der Lauf endet hier**.
  Nicht mehr behoben: Fund 6 und **Fund 7** (`aria-label` auf blankem `<span>` mit den Sternen,
  `ExerciseRatingPanel.tsx`, wird nicht verlässlich angesagt → `role="img"`). 🟢-Hinweise nicht behoben: Ladefehler
  ohne `ListenAusfall`, Zeile kann nach dem Speichern unter `sort=rating` die Seite wechseln, PUT-Rumpf ohne
  `satisfies RateExerciseDto`, Sternanzeige je Zeile und Formular-Leeren nach dem Löschen ungeprüft.
- **2026-10-26** — Rollengang (Freigabe 6, Browser-Extension nicht verbunden → E2E): `e2e/uebungen-bewerten.spec.ts`
  fährt Vater (sieht Schnitt/Anzahl, sortiert, bewertet, ändert, liest nach Neuladen zurück, löscht) und Autorin
  (sieht die Bewertungen, kein Formular). **Keine Abnahme:** der Lauf endete an Freigabe 3 vor Rollengang-Runde und
  Retro; Story bleibt `in-arbeit`, Wiederaufnahme im Protokoll. Commit `2f7ca24` (nicht gepusht).
- **2026-10-26** — **Entscheidung des Nutzers** (Morgen-Entscheidung 2026-10-26, Dialog, [Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)): (1) Unbewertete Übungen stehen in **beiden** Sortierrichtungen **am Ende** (`sort=rating` und `-rating`); AK 6 ist damit geändert, rote Probe gegen die Spiegel-Sortierung. (2) Gebaut ist die Bewertung in der Verwaltungsliste, so wie die Story es sagt, und damit wird B-12 abgenommen. Anzeige und Sortierung im Positions-Formular kommen als eigene Story [B-270](B-270-bewertung-im-positions-formular.md). (3) Die Nacharbeit (Fund 6, Fund 7, AK 6 neu, dazu [B-269](B-269-bewertungen-reihenfolge-und-updatedat-ungetestet.md)) läuft mit **neuem Zähler (0)**. `wartet_auf` geleert. **Freigabe des Nutzers (Autonomer Modus, Opt-in):** der Nachtlauf darf die geänderte AK 6 ohne weiteres Grillen bauen.
