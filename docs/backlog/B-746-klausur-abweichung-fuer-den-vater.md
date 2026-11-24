---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, bereich/frontend, rolle/supervisor]
aliases: [Abweichung Prognose Note, Eichung von Hand]
status: abgenommen
prio: P2
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-683
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-746 · Prognose und Note nebeneinander: die Abweichung für den Vater

Das Modell eicht sich im ersten Bau nicht von selbst ([B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md), Entscheidung 2). Statt
dessen sieht der Vater je Klassenarbeit die Prognose-Stufe neben der echten Note und stellt den Notenschlüssel
([B-740](B-740-notenschluessel-percenttograde.md)) bei Bedarf von Hand nach. Backend zuerst: ein additives Feld an der Klassenarbeit;
danach die Spalte in der Klassenarbeiten-Liste.

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-683.*

## User Story

Als *Vater* möchte ich nach einer Arbeit sehen, was die Prognose erwartet hat und was die Note war, damit ich der Prognose trauen
oder den Schlüssel nachstellen kann.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`.

- `KlassenarbeitResponse` trägt keine Prognose (`backend/Pugling.Contracts/Supervisor/ClassTestDtos.cs`); der Mapper steht im
  `KlassenarbeitenController` (`Map(...)`, Aufrufe `:117`, `:165`).
- Die Klassenarbeiten-Seite des Vaters: `frontend/src/vater/VaterClassTests.tsx` mit Tabelle (Notenzelle `GradeCell`, `:135`,
  `:159`) und der Liste „zu wiederholen" (`:268`); Test `VaterClassTests.test.tsx`.
- Der Schnappschuss `PredictedLevel` entsteht mit [B-744](B-744-klausur-prognose-schnappschuss.md); die Abweichung ist daraus und
  aus der Note berechenbar, eine eigene Speicherung ist nicht nötig.

## Entscheidungen

1. **Die Abweichung wird nicht gespeichert, sondern aus `PredictedLevel` und `Grade` abgeleitet; das Feld `PredictedLevel` steht
   additiv an der `KlassenarbeitResponse`** (B-683 Entscheidung 2). Kosten: Wird der Schlüssel später geändert, ändert sich die
   angezeigte Abweichung älterer Arbeiten nicht (die Stufe ist am Klausurtag festgeschrieben, die Note-zu-Stufe-Umrechnung aber
   nicht). Verworfen: automatisches Nachstellen des Schlüssels. Schließt aus: „wird nachgeschärft" im Sinn von „von selbst".
2. **Nur der Vater sieht die Abweichung** (Rahmen D, B-683 Entscheidung 6): Das Kind sieht seine Klausur nicht.

## Akzeptanzkriterien

1. Die Klassenarbeiten-Liste zeigt je Zeile mit Schnappschuss und Note die Prognose-Stufe neben der Note und ihre Differenz; ohne
   Schnappschuss zeigt sie „keine Prognose". *Rot, wenn* eine Zeile ohne Schnappschuss eine Stufe zeigt (der Ausgangszustand zeigt
   keine, der Test prüft die Zeile mit Schnappschuss und die ohne).
2. `KlassenarbeitResponse.PredictedLevel` ist leer, solange nichts gesetzt wurde, und trägt danach die Zahl. *Rot, wenn* das Feld im
   Vertrag fehlt oder bei fehlendem Schnappschuss eine Null liefert.
3. Ein Kind erreicht die Abweichung nicht. *Rot, wenn* ein Student-Endpunkt `Klassenarbeit` mit `PredictedLevel` liefert (Absichts-
   erklärung; es gibt heute keinen).

## Schätzung

**Größe: S**, wie „`childId` aus dem Test-Pfad ziehen" (B-01) plus eine Spalte in einer Tabelle: ein additives Feld, eine Zelle.

- **`wo: beides`**, Backend zuerst: ein Feld im Antwort-DTO und im Mapper, danach die Tabellenspalte.
- **`migration: nein`**: das Feld kommt aus der Spalte aus B-744.
- **`vertragsbruch: nein`**: neues optionales Feld an `KlassenarbeitResponse`. Ein positionaler Record bekommt ein neues Argument am
  Ende; Aufrufer des Konstruktors (Mapper, Tests) ziehen mit, nach außen additiv. `contract.ts` wird neu erzeugt.

### Risiken

- **Positionaler Record:** `KlassenarbeitResponse` ist ein Record; ein Argument am Ende verlangt, jeden Konstruktoraufruf in
  Controller und Tests anzufassen (Suche vor dem Bau). Kein Bruch auf dem Draht.
- **`unknown_field`:** Das Frontend schickt `KlassenarbeitResponse` nie zurück; kein Risiko dort. `UpdateClassTestDto` bekommt
  kein Feld.
- **Zeigen heißt nicht eichen:** Der Vater braucht einen Ort, an dem er den Schlüssel nachstellt; das ist eine Datei
  (`appsettings.json`, [B-740](B-740-notenschluessel-percenttograde.md)) und für ihn kaum nutzbar. Das ist die Folge der
  Entscheidung (Kosten), kein Defekt dieser Story.

### Angriffsplan

1. `PredictedLevel` an `KlassenarbeitResponse` (Contracts) und im Mapper des Controllers; Tests, die den Konstruktor aufrufen,
   nachziehen.
2. OpenAPI und `frontend/src/lib/contract.ts` neu erzeugen.
3. Spalte in `VaterClassTests.tsx`: „Prognose / Note / Differenz".
4. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`, Vitest, Playwright.

### Testweg

- Backend: `KlassenarbeitenTests` (vorhanden) um einen Fall ergänzen, dazu `KlassenarbeitPrognoseTests` (neu, aus
  [B-744](B-744-klausur-prognose-schnappschuss.md)) für das gesetzte Feld (AK 2); `ContractDocumentTests` läuft mit.
- Frontend: `frontend/src/vater/VaterClassTests.test.tsx` (vorhanden) für AK 1, ein Fall mit, einer ohne Schnappschuss.
- Rollengang: E2E-Spec `frontend/e2e/klassenarbeit-abweichung.spec.ts` (neu), Vater trägt eine Note ein und sieht die Zeile; das
  ist zugleich der Rollengang für B-744 und B-745.
- Vor der Abnahme: beide Reviewer (`pugling-reviewer`, `frontend-reviewer`).

## Verlauf

- **2026-11-20** — angelegt aus [B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md) (geteilt); `geschaetzt`: S, beides (Backend
  zuerst), keine Migration, kein Vertragsbruch (Vorbereitungslauf, Sonnet).
- **2026-11-24** — Nachprüfung vor dem Bau (Nachtlauf L, Sprint 161, [Protokoll](../pm-sitzung-2026-11-24-l.md)): Ist-Stand
  bestätigt; `KlassenarbeitResponse` wird nur im Mapper `KlassenarbeitenController.MapAsync` gebaut (kein Konstruktor in
  Tests), die Liste ist der einzige Ort im Frontend, der sie zeigt. Die Differenz braucht die offene Umrechnung
  Kommanote → Stufe aus B-745 nicht: sie ist Note minus Stufe. `in-arbeit`.
- **2026-11-24** — gebaut: `KlassenarbeitResponse.PredictedLevel` (`int?`, additiv am Ende), Mapper reicht die Spalte durch,
  `docs/openapi/v1.json` und `contract.ts` neu erzeugt. Frontend: Spalte „Prognose“ in `VaterClassTests.tsx` mit
  `PrognoseZelle` („Stufe N“, mit Note „· 0,5 besser“ / „· 2 schlechter“ / „· wie erwartet“, ohne Schnappschuss „keine
  Prognose“), reine Funktionen `prognoseAbstand` (Note minus Stufe, roh) und `abstandText` (Zehntel, symmetrisch über
  `toLocaleString`, auf 0 gerundet = „wie erwartet“). Kein Student-Endpunkt trägt das Feld.
- **2026-11-24** — AK → Test: AK 1 mit Schnappschuss und Note → `VaterClassTests.test.tsx` „zeigt Stufe und Abstand in
  Worten“ (F1 Richtung gekippt, F6 Worte vertauscht) · AK 1 ohne Schnappschuss → „zeigt ohne Schnappschuss ‚keine
  Prognose‘“ (F4) · Schnappschuss ohne Note → „zeigt vor der Note nur die Stufe“ (F3) · Gleichstand → „nennt den
  Gleichstand“ (F5) · Rundung → „rechnet Note minus Stufe und rundet erst in der Anzeige“ (F2) · AK 1 in der Zeile der
  echten Liste → E2E `klassenarbeit-abweichung.spec.ts` (Stufe 6 · 2 besser, „keine Prognose“ ohne Fach) · AK 2 →
  `KlassenarbeitPrognoseTests.Vater_liest_die_Prognose_an_der_Arbeit` (M11 Mapper `null` → rot, M12 `?? 0` → rot) ·
  AK 3 → `KlassenarbeitCountdownTests.Antwort_Traegt_Weder_Note_Noch_Notenkommentar` um `predictedLevel` erweitert (M13:
  das Feld am Schülervertrag → rot).
- **2026-11-24** — Review `frontend-reviewer` (Agent, Vitest/`tsc` selbst gelaufen): kein 🔴. 🟡 **Das Vorzeichen trug die
  Bedeutung nur im `title`** – ein Vater liest „−“ leicht als „schlechter“, und `title` erreicht Handy und Tastatur nicht.
  Behoben (gezählt, Sprint 161: 2): Richtung in Worten („besser“/„schlechter“), kein `title` mehr. 🟡 **Doku „ungerundet“,
  Code rundete auf Zehntel mit `Math.round` (asymmetrisch: 2,25 → −0,7, 3,75 → +0,8; 3,04 → „wie erwartet“)**. Behoben
  (gezählt, Sprint 161: 3): `prognoseAbstand` liefert roh, gerundet wird nur in `abstandText` über `toLocaleString`
  (symmetrisch, 2,25 und 3,75 beide 0,8). 🟢 zurückgestellt ohne Zählung: eine noch geplante Arbeit zeigt „keine Prognose“,
  was nach „etwas fehlt“ klingen kann – AK 1 verlangt genau diesen Text; ein „kommt am Klausurtag“ wäre eine
  Wortlautentscheidung (B-980).
- **2026-11-24** — Mutanten-Probe Frontend nach der letzten Änderung (Vitest `VaterClassTests.test.tsx`, 2026-11-24 22:14, an
  `5993c86b + Arbeitsbaum geändert`, Grundlinie danach grün); die Backend-Mutanten M11–M13 stehen in B-744:

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | F1 Richtung gekippt | frontend/src/vater/VaterClassTests.tsx:174 | red | red |  |
  | F2 ganze Noten | frontend/src/vater/VaterClassTests.tsx:182 | red | red |  |
  | F3 fehlende Note als 0 | frontend/src/vater/VaterClassTests.tsx:173 | red | red |  |
  | F4 ohne Schnappschuss eine Zahl | frontend/src/vater/VaterClassTests.tsx:193 | red | red |  |
  | F5 Gleichstand als Zahl | frontend/src/vater/VaterClassTests.tsx:183 | red | red |  |
  | F6 Worte vertauscht | frontend/src/vater/VaterClassTests.tsx:184 | red | red |  |
- **2026-11-24** — Verifikation nach der letzten Änderung: .NET 2353/2353 (Release, Ausgangsstand 2343), Vitest 130 Dateien /
  1445 Tests, `tsc -b` sauber, `dotnet format --verify-no-changes` sauber. Rollengang: E2E `klassenarbeit-abweichung.spec.ts` (Vater liest „Stufe 6 · 2 besser“ und „keine Prognose“) mit den Zeugen `klassenarbeit-countdown`, `klassenarbeit-vorbereitung`, `notenstufe` – 4 passed; dieselbe Spalte an der Kopie der echten DB über die API gelesen (Friedrich: `predictedLevel` 6). `abgenommen`, Code in Commit
  `2886acf6`. Die `## Entscheidungen` sind weiter „stellvertretend, unbestätigt“ (Vorbereitungslauf 2026-11-20); die
  Bestätigung durch den Nutzer steht aus.
