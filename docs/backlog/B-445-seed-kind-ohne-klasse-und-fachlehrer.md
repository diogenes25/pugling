---
tags: [typ/story, status/abgenommen, bereich/seed, rolle/supervisor]
aliases: [Seed-Kind ohne Klasse und Fachlehrer]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-09-nacht.md
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: 2026-11-14
---

# B-445 · Seed: „Sohn" hat weder Klasse noch Schulart, und Herr Schmidt trägt kein Fachlehrer-Profil

Teil 2 von B-429. Im Kind-Hub steht für das Seed-Kind „Kein Profil passt zu Sohn – prüfe Klasse und Schulart"
(`Seed.cs` `SeedAdmin`: `Child` ohne `Grade`/`SchoolType`), und der Lehrer-Seed legt kein `CreatorProfile` an.

**Warum nicht im selben Sprint wie das Buch:** Klasse und Schulart des Kindes filtern den Katalog serverseitig. Die E2E
`frontend/e2e/assistent.spec.ts` (Kommentar über `waehleSohnUndEnglisch`) braucht genau deshalb ein Kind **ohne** Klasse:
„Begrüßungen" ist Klasse 5–6, „Vocabulary: The environment" 8–10 – mit gesetzter Klasse verschwände eine der beiden Zeilen.
Dazu laufen viele Integrationstests auf Kind 1. Das ist kein Seed-Aufräumen mehr, sondern ein Umbau der Testannahmen,
der eigene Entscheidungen braucht (Klasse 5 am Seed-Kind und die Specs auf ein eigenes Kind umziehen? Oder ein zweites
Seed-Kind mit Klasse für den Fachlehrer-Treffer?).

## Befund am Code (2026-11-13): was die echte DB davon träfe

`Seed.RunAsync` läuft bei **jedem** Start, auch gegen die echte `pugling.db` der Söhne (`Seed.cs:28-31`).
`SeedAdmin` kehrt bei vorhandenen Erwachsenen sofort zurück (`Seed.cs:1088`) – Klasse/Schulart am Seed-Kind erreichten die
echte DB also **nie**, nur frische DBs (Tests, E2E). `SeedTeacherLibrary` (`Seed.cs:516`) hat dagegen keine solche Sperre
am Anfang, sondern prüft je Schritt – ein dort ergänztes `CreatorProfile` für Herrn Schmidt landete beim nächsten Start
**auch in der echten DB**. Für die Entscheidung heißt das: Teil „Seed-Kind" ist ein reiner Test-/Demo-Umbau, Teil
„Fachlehrer-Profil" ändert die Daten der Familie und braucht dieselbe Idempotenz-Prüfung wie die übrigen Schritte dort.

## User Story

Als **Vater auf einer frischen Demo-Instanz** möchte ich im Kind-Hub des Seed-Kinds einen passenden Fachlehrer sehen statt
„Kein Profil passt", damit der KI-Creator-Weg (Profil → Übungen) ohne Handarbeit vorführbar ist – ohne dass die echte
`pugling.db` der Söhne dadurch verändert wird.

## Ist-Stand am Code

- `SeedAdmin` (`backend/Pugling.Api/Data/Seed.cs:1086`) kehrt bei `db.Adults.Any()` zurück (`:1088`); das Kind „Sohn"
  (`:1093`) hat weder `Grade` noch `SchoolType`.
- `SeedTeacherLibrary` (`Seed.cs:516`) legt „Herr Schmidt (Englischlehrer)" an und **kehrt zurück, sobald die Demo-Unit
  existiert** (`Seed.cs:530`). Korrektur zum Befund oben: es gibt also doch eine Sperre am Anfang des Inhaltsteils; ein
  Profil, das **nach** `:530` angelegt wird, erreicht nur DBs, in denen die Demo-Unit noch fehlt – also frische.
- Kein `CreatorProfile` wird irgendwo geseedet (nur `CreatorProfilesController`/`CreatorProfileService` legen an).
- Der Abgleich (`backend/Pugling.Api/Services/Creator/CreatorProfileService.cs:104-110`, `Fits`) schließt nur bei
  **gesetzter** Klasse/Schulart aus: ein Kind ohne Klasse passt zu jedem aktiven Profil. Die Meldung „Kein Profil
  passt" (`frontend/src/vater/ChildMaterialSection.tsx:567`) verschwindet also schon mit einem Profil – die Klasse am
  Seed-Kind ist dafür **nicht** nötig.

## Die echte Lücke

Nur das fehlende Fachlehrer-Profil. Die Klasse am Seed-Kind war als Ursache vermutet, ist es aber nicht (`Fits`), und
sie zu setzen bräche die Annahmen von `assistent.spec.ts` und der Tests auf Kind 1.

## Offene Punkte

1. ~~Klasse 5 am Seed-Kind und Specs umziehen, oder zweites Seed-Kind?~~ → Entscheidung 1.
2. ~~Wo und wie idempotent wird das Profil angelegt, und erreicht es die echte DB?~~ → Entscheidung 2.
3. ~~Mit welchen Werten?~~ → Entscheidung 3.

## Entscheidungen

1. **Das Seed-Kind bleibt ohne Klasse und Schulart; kein zweites Seed-Kind.** Begründung: `Fits` lässt ein Kind ohne
   Klasse zu jedem Profil passen, die sichtbare Meldung verschwindet also ohne Umbau der Testannahmen. Kosten: der
   Treffer zeigt beim Seed-Kind keinen Grund `grade_in_range`/`school_type_match` – die Demo führt nur Fach (und ggf.
   Reihe) vor.
2. **Das Profil entsteht nur auf einer frischen DB**: im Inhaltsteil von `SeedTeacherLibrary`, also **nach** der Sperre
   `Seed.cs:530`, zusammen mit der Demo-Unit. Begründung: der Seed darf eine bestehende DB nicht anfassen; die echte
   `pugling.db` hat die Demo-Unit (oder bekommt das Profil nie). Kosten: eine bestehende Demo-DB bekommt das Profil
   nicht nachgereicht – wer es dort will, legt es im Vater-Web an.
3. **Werte:** Name „Herr Schmidt – Englisch", Besitzer Herr Schmidt, Fach Englisch, Schulart Gymnasium, Klasse 5–10,
   ohne Reihe, kurze deutsche `Persona`, `Active = true`. Begründung: deckt die Demo-Inhalte (Klasse 5–6 und 9) ab und
   matcht das klassenlose Seed-Kind. Kosten: ein zweites aktives Englisch-Profil in Test-DBs kann einen Test, der
   „kein Treffer" erwartet, umwerfen – Teil des Testwegs.

Alle drei: Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
[Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

## Akzeptanzkriterien

1. Auf einer frischen DB liefert `GET api/v1/creator/profiles/match?childId=1` mindestens das Profil von Herrn Schmidt.
   *Rot macht ihn:* das Profil wird nicht angelegt.
2. Ein zweiter Seed-Lauf auf derselben DB legt kein zweites Profil an. *Rot macht ihn:* das Profil vor der Sperre
   `:530` oder ohne eigene Existenzprüfung.
3. Eine DB, in der die Demo-Unit schon existiert und kein Profil, bleibt nach dem Seed ohne Profil (die echte DB wird
   nicht angefasst). *Rot macht ihn:* das Profil vor der Sperre `:530`.
4. Das Seed-Kind „Sohn" trägt weiterhin weder Klasse noch Schulart. *Rot macht ihn:* Klasse am Seed-Kind gesetzt
   (`assistent.spec.ts` verlöre eine Zeile).

## Schätzung

**Größe: XS** — ein `db.CreatorProfiles.Add(…)` im bestehenden Inhaltszweig plus Tests.

- **Risiken:** Tests in `CreatorProfileTests.cs`/`ProfileFitTests.cs`, die auf einer Factory-DB „keine Profile" oder eine
  feste Trefferzahl erwarten, werden rot – vor dem Bau greppen und gezielt nachziehen, nicht die Zusicherung lockern.
- **Angriffsplan (Backend):** (1) AK 1–4 als Tests; (2) Profil in `SeedTeacherLibrary` nach `:530` anlegen; (3) volle
  Suite (Stop-Hook) und betroffene Profil-Tests anpassen.
- **Testweg:** `backend/Pugling.Api.Tests/SeedContractTests.cs` (AK 1, 4; Herr Schmidt ist dort schon gepinnt) und
  `SeedFamilyTests.cs` (AK 2, 3: Seed zweimal bzw. auf einer DB mit vorhandener Demo-Unit laufen lassen). Danach
  `/smoke-test`; die echte DB bleibt unberührt.

## Verlauf

- **2026-11-09** — abgeteilt aus [B-429](B-429-seed-familie-widerspricht-dem-katalog.md) beim Grillen in Lauf AG (Sprint 27). Abgeteilt, weil die Klasse am Seed-Kind den Katalog in mehreren Specs filtert (Begründung oben).
- **2026-11-13** — Befund (Tageslauf, Restzeit): welcher der zwei Teile die echte DB erreicht – nur das Fachlehrer-Profil.
- **2026-11-13** — ausformuliert (Backlog-Lauf): Befund korrigiert – `SeedTeacherLibrary` sperrt ab `Seed.cs:530`; die
  Meldung „Kein Profil passt" braucht keine Klasse am Kind (`CreatorProfileService.cs:104-110`).
- **2026-11-13** — gegrillt: Kind bleibt klassenlos, Profil nur auf frischer DB, Werte festgelegt. Stakeholder-Entscheidung
  im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — geschaetzt: XS, backend, keine Migration, kein Vertragsbruch.
- **2026-11-13** — `in-arbeit`: `SeedTeacherLibrary` legt das Profil „Herr Schmidt – Englisch“ (Herr Schmidt, Englisch, Gymnasium, Klasse 5–10, ohne Reihe, deutsche Persona, aktiv) im Inhaltszweig **nach** der Demo-Unit-Sperre an; die echte DB bekommt es nie. Tests `SeedContractTests.SeedKind_Findet_Den_Fachlehrer_Und_Bleibt_Ohne_Klasse` (AK 1 und AK 4; die AK-4-Hälfte ist Absichtserklärung, im Ausgangszustand grün) und `SeedFamilyTests.FachlehrerProfil_Entsteht_Nur_Auf_Frischer_Datenbank` (AK 2, 3: zweiter Seed-Lauf, dann Profil gelöscht und erneut geseedet). Keine bestehende Profil-Zusicherung wurde rot (CreatorProfile-, Fit-, Fachname-Tests grün). `/smoke-test` nicht gelaufen. Rote Probe 3/3 rot.
  Rote Probe (Gemessen an `afadf18d + Arbeitsbaum geändert`, 2026-11-13 22:15, `mutant-probe.py`, Grundlinie nach dem Zurückspielen grün):

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | B445-AK1 | backend/Pugling.Api/Data/Seed.cs:642 | red | red |  |
  | B445-AK2-3 | backend/Pugling.Api/Data/Seed.cs:530 | red | red |  |
  | B445-AK4 | backend/Pugling.Api/Data/Seed.cs:1111 | red | red |  |
- **2026-11-13** — Review-Fund (`pugling-reviewer`) behoben: der Testkommentar „der Zustand der echten pugling.db“ war irreführend – die echte DB schützt nicht die Unit-Sperre, sondern dass sie gar nicht geseedet wird (Production ohne Seed, `Program.cs:665`; `scripts/heimnetz.ps1:162` setzt `Seed__Enabled=false`). Kommentare in `SeedFamilyTests.cs` und `Seed.cs` berichtigt, Leerzeile vor `SeedAsync` ergänzt; die Verlauf-Zeile davor („die echte DB bekommt es nie“) gilt aus diesem Grund, nicht wegen der Sperre.
- **2026-11-13** — **abgenommen** mit Commit `f88464e8` (Sprint 70, Backlog-Lauf 2026-11-13). Verifikation: Backend 1569/1569 grün (dotnet test Pugling.sln -c Release), dotnet format für die geänderten Dateien sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: `pugling-reviewer` â€“ Fund ðŸŸ¢ (der Testkommentar schrieb den Schutz der echten DB der Unit-Sperre zu; tatsÃ¤chlich seedet Production nie und `heimnetz.ps1` setzt `Seed__Enabled=false`) im selben Sprint berichtigt. Rollengang: Playwright gegen frisch gestartete Server auf Ausweichports (API 5213, Web 5186) nach dem letzten Edit – creator-lehrwerk-weg, einkleidung, fachlehrer-faecher-fallen-aus, kind-anlegen-id, kind-auswahlliste, kind-lehrbuch-fach, lehrwerk-bearbeiten, lehrwerke, reihe-fachwechsel-gesperrt, uebung-teilspeichern u. a.: **23 von 24 grün**. `lehrwerke` war zuerst rot, weil sie den Zustand vor B-445 zusicherte („Kein Profil passt“) – angepasst und einzeln grün nachgelaufen. `vater-von-null` bleibt rot: zwei „Speichern“-Knöpfe auf der Kind-Seite, eingeführt von `f9df2087` (B-533-Nachtrag, **vor** diesem Lauf), unabhängig von dieser Story; die parallele B-534-Arbeit baut genau diese Fläche um. Sohn ist Regressionszeuge (Suite grün).
