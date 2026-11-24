---
tags: [typ/story, status/abgenommen, bereich/gamification, bereich/lernstand, rolle/student, rolle/supervisor]
aliases: [Notenstufe lesen, Stufe Endpunkte]
status: abgenommen
prio: P2
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-682
grund: ""
ersetzt_durch: []
---

# B-742 · Die Notenstufe lesen: ein Endpunkt fürs Kind, einer für den Vater

Zwei lesende Endpunkte auf der Tabelle aus [B-741](B-741-notenstufe-berechnen-und-speichern.md): das Kind liest seine Stufen je Fach,
der Vater die des Kindes. Beide stoßen die träge tägliche Neubewertung an und liefern neben Stufe und Bestwert die Zahl der Wörter,
die bis zur nächsten Stufe fehlen. Abgespalten aus [B-682](B-682-notenstufe-je-fach-statt-liga.md).

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-682.*

## User Story

Als *Schüler* möchte ich je Fach meine Stufe und das nächste Ziel abrufen können, und als *Vater* die Stufen meines Kindes, damit
beide dieselbe Zahl sehen und nie eine Stufe im Vergleich zu einem anderen Kind.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`; Pfade relativ zu `backend/Pugling.Api/`.

- Student-Lesepfade für den Lernstand laufen unter `student/me/…` (Beispiel `MeController`, Aktivierungen `student/me/shop/activations`,
  `frontend/src/lib/api.ts:855`); Vater-Lesepfade je Kind unter `supervisor/children/{childId}/…` mit `ChildOwnershipFilter`
  (`Controllers/Supervisor/ShopController.cs:457-459`).
- Ein Endpunkt, der eine Stufe oder einen Anteil je Fach für das Kind liefert, ist nicht gefunden; die Lernstand-Drilldowns
  (`ChildLearnProgressService`) sind vater-seitig, ein Student-Gegenstück ist nicht gefunden.
- `EndpointCoverageGuard` verlangt für jeden Endpunkt einen Eintrag in der Abdeckung (`Pugling.Api.Tests/EndpointCoverage.cs`).
- Lösungsfeld-Regel: keine betroffene Antwort (keine Felder `Answer`, `Solution`, `CorrectAnswer`, `Translation`).

## Entscheidungen

1. **Das Kind sieht die Stufe je Fach, der Vater im Kind-Hub, nie im Vergleich mit anderen Kindern** (B-682 Entscheidung 6, G-6).
   Kosten: kein Eintrag im Wochenvergleich der Familie (B-253). Schließt aus: einen Endpunkt, der Stufen mehrerer Kinder in einer
   Antwort liefert.
2. **Die Antwort trägt Stufe, Bestwert und `WordsToNextLevel`; Wortlaut und Etikettierung liegen im Frontend** (B-682 Entscheidung 3:
   die Textregel ist Oberfläche). Kosten: der Server liefert die Stufenzahl auch für 4 bis 6; die Zurückhaltung gegenüber dem
   Kind ist Frontend-Pflicht (siehe [B-743](B-743-notenstufe-anzeige-kind-und-vater.md)). Verworfen: Stufen 4 bis 6 serverseitig
   verbergen – dann sähe auch der Vater sie nicht ohne zweiten Endpunkt.
3. **Der Lesezugriff stößt die träge Neubewertung des Tages an** (B-682 Entscheidung 4, B-741). Kosten: ein Lesepfad schreibt
   höchstens einmal je Tag und Fach.

## Akzeptanzkriterien

1. Ein Kind liest seine eigenen Stufen; ein Fach ohne Maß fehlt in der Liste. *Rot, wenn* Mathe ohne Wortschatz mit Stufe erscheint.
2. Ein Kind kann die Stufe eines anderen Kindes nicht lesen; ein fremder Vater die des Kindes nicht. *Rot, wenn* der Besitz-Filter
   fehlt (der Vater eines anderen Kindes bekäme 200 statt 404).
3. `WordsToNextLevel` ist 0 oder fehlt auf der besten Stufe und sinkt, wenn ein weiteres Wort beherrscht wird. *Rot, wenn* der
   Wert aus der Antwortzahl statt aus der Stufengrenze entsteht.
4. Die Antwort enthält keine Stufe eines anderen Kindes. *Rot, wenn* ein Vater mit zwei Kindern aus dem Kind-Endpunkt beide sieht.
5. Ein Lesepfad ändert die Stufe nicht außerhalb der Frist-Regeln. *Rot, wenn* zwei Lesezugriffe am selben Tag zwei Senkungen
   auslösen.
6. Der Vertrag trägt ein neues Antwort-Objekt und keine Änderung an bestehenden. *Rot, wenn* ein bestehendes Feld in
   `Pugling.Contracts` seine Form ändert (Absichtserklärung, `ContractDocumentTests` läuft mit).

## Schätzung

**Größe: S**, wie „`childId` aus dem Test-Pfad ziehen" (B-01) mit einem Endpunktpaar: zwei Lese-Actions über einen vorhandenen
Dienst, ein DTO.

- **`wo: backend`**: die Oberfläche folgt in [B-743](B-743-notenstufe-anzeige-kind-und-vater.md).
- **`migration: nein`**: die Tabelle kommt aus B-741.
- **`vertragsbruch: nein`**: neues Antwort-Objekt, additiv. Der Client (`Pugling.Client`) bekommt keine zwingende Änderung;
  `frontend/src/lib/contract.ts` wird neu erzeugt.

### Risiken

- **`WordsToNextLevel` braucht die Umkehr der Notentabelle** ([B-740](B-740-notenschluessel-percenttograde.md)): „wie viel Anteil
  fehlt bis zur nächsten Grenze, in Wörtern gerundet auf ganze". Rundungsfehler an den Grenzen sind die Falle; die Umkehr gehört zur
  Funktion aus B-740, nicht in eine zweite Tabelle.
- **Lesen schreibt.** Ein `GET`, der eine Zeile anlegt, widerspricht dem Gefühl von „sicher". Alternative (nicht gewählt):
  Berechnung nur bei der Anmeldung. Dann zeigt ein Kind, das tagelang angemeldet bleibt, eine Stufe von gestern.
- **Doppelter Aufruf:** Der eindeutige Index aus B-741 trägt auch hier die Verliererbehandlung.

### Angriffsplan

1. DTO `SubjectLevelResponse` (Fach, Stufe, Bestwert, `WordsToNextLevel`) in `Pugling.Contracts/Student`.
2. Student-Action unter `student/me/subject-levels`, nur `[Authorize]`, Rollenprüfung inline (`IsStudent`, wie die übrigen
   Student-Lesepfade).
3. Vater-Action unter `supervisor/children/{childId}/subject-levels` mit `ChildOwnershipFilter`, `Roles.Supervisor`.
4. Abdeckungseintrag, OpenAPI neu, `frontend/src/lib/contract.ts` neu erzeugen (nur Typen; Verbraucher folgen in B-743).
5. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Testklasse `SubjectLevelEndpointTests` (Integrationstest, `PuglingWebAppFactory`, Rollen über `TestApi`; Fremdzugriff nach
  dem Muster von `MultiSupervisorTests`) für AK 1 bis 5.
- `EndpointCoverageGuard`, `ContractDocumentTests`, `ConventionGuardTests` laufen mit (AK 6, Lösungsfeld-Regel).
- Vor der Abnahme: Agent `pugling-reviewer`; `/smoke-test` gegen die Wegwerf-DB für beide Endpunkte (Backend ohne Oberfläche, der
  Rollengang folgt mit B-743).

## Verlauf

- **2026-11-20** — angelegt aus [B-682](B-682-notenstufe-je-fach-statt-liga.md) (geteilt); `geschaetzt`: S, backend, keine
  Migration, kein Vertragsbruch (Vorbereitungslauf, Sonnet).
- **2026-11-24** — `in-arbeit` und gebaut (Nachtlauf G, Sprint 127): DTO `SubjectLevelResponse`
  (`Pugling.Contracts/Student/SubjectLevelDtos.cs`, mit `SubjectName`, weil die Tagessicht ihre Fachkarten nach Namen
  gliedert), `MySubjectLevelsController` (`student/me/subject-levels`, `Roles.Student`, Kind aus dem Token),
  `ChildSubjectLevelsController` (`supervisor/children/{childId}/subject-levels`, `Roles.Supervisor` +
  `ChildOwnershipFilter`), `SubjectLevelService.ReadAsync` (bewertet den Tag, listet nur Fächer mit Maß, sortiert nach
  Name; eine Zeile ohne Maß bleibt gespeichert, erscheint aber nicht). `WordsToNextLevel`: live gezählt gegen die Grenze
  der nächstbesseren Note über `GradeKey.MinPartForGrade` (die Umkehr aus B-740, keine zweite Tabelle); `0` = erreicht,
  die Stufe folgt mit der Bewertung des nächsten Tages – eine Ausführungsfrage innerhalb Entscheidung 2, Wortlaut im
  Frontend. Client: `StudentApi.ListMySubjectLevelsAsync`, `SupervisorApi.ListSubjectLevelsAsync`. Tor
  `Unpaginierte_Array_GETs_Sind_Gepinnt` 31 → 33 mit Begründung (roter Wächter, Freigabe 11, gezählt, Sprint-Zähler **2**).
- **2026-11-24** — AK → Test (`backend/Pugling.Api.Tests/SubjectLevelEndpointTests.cs`): AK 1 →
  `Kind_liest_eigene_Stufen_Fach_ohne_Mass_fehlt` (N3: Lesen ohne Bewertung → rot) · AK 2/4 →
  `Fremde_lesen_nicht_mit_und_Geschwister_bleiben_getrennt` (N4 Besitz-Filter weg → rot, N5 fremdes Kind aus `/me` → rot;
  dazu `OwnershipMatrixTests` über alle `{childId}`-Actions) · AK 3 → `Woerter_bis_zur_naechsten_Stufe_sinken_mit_jedem_Wort`
  (N1 falsche Grenze → rot, N2 kein `null` auf Stufe 1 → rot) · AK 5 → `Zwei_Lesezugriffe_am_selben_Tag_senken_nur_einmal`:
  **doppelt gehalten** (Tagesprüfung und Sieben-Tage-Frist), keine Einzelmutante macht ihn rot; die Tagesprüfung trägt
  `ChildSubjectLevelTests` (M8) · AK 6 → `ContractDocumentTests` grün, `docs/openapi/v1.json` nur ergänzt (+167 Zeilen).
  Tabelle: B-741.
- **2026-11-24** — Review `pugling-reviewer` (Agent, nur gelesen, Sprint 127, B-740 bis B-742 gemeinsam): keine 🔴.
  Bestätigt: ein Schreiber der Stufe (`SubjectLevelService`), eine Umrechnung (`GradeKey`), das Maß aus
  `ChildLearnProgressService` ohne Kopie, Tag nur aus `LearningDay`, Verlierer-Pfad am eindeutigen Index. 🟡 „Doku
  widerspricht dem Verhalten“: der Satz „ein Kind, das tagelang fehlt, wird nicht neu bewertet“ stimmte nicht, weil auch
  der Vater-Lesepfad bewertet (B-742 Entscheidung 3) – Satz berichtigt, ohne Zählung (nur Doku); die Spannung zur Risiko-
  Notiz „keine Senkung ohne Lernen“ ist als offene Frage im Protokoll. 🟢 ohne Zählung, behoben: (1) das Maß wurde je
  Lesen zweimal gerechnet – `EvaluateAndMeasureAsync` gibt es jetzt zurück; (2) eine kaputte Tabelle hätte jede
  Kind-Anmeldung geworfen – Prüfung beim Start (`AddOptions…Validate(GradeKey.IsValid).ValidateOnStart()`); (3) Kommentar
  am Verlierer-Pfad präzisiert („nahezu dieselben Daten“). 🟢 nicht behoben: Rennen ungetestet (nicht deterministisch
  herstellbar), Student-Endpunkt `[Authorize(Roles = Student)]` statt `[Authorize]` inline (strenger, `/me` braucht keinen
  Supervisor), AK-5-Endpunkttest schwach (Tagesprüfung trägt `ChildSubjectLevelTests`).
- **2026-11-24** — `/smoke-test` ersetzt durch die E2E `frontend/e2e/notenstufe.spec.ts` gegen eine Wegwerf-DB auf :5211
  (beide Endpunkte über die echte Oberfläche). `abgenommen`; Commit `4aae8d36`.
