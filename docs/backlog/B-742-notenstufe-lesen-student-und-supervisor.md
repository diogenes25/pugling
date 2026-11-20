---
tags: [typ/story, status/geschaetzt, bereich/gamification, bereich/lernstand, rolle/student, rolle/supervisor]
aliases: [Notenstufe lesen, Stufe Endpunkte]
status: geschaetzt
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
