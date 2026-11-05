---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/vertrag, rolle/creator]
aliases: [Übungs-Validierung meldet unbekannte Ids als validation_error]
status: abgenommen
prio: P3
art: Defekt
groesse: M
wo: backend
migration: nein
vertragsbruch: ja
quelle: B-258 (Lauf J, docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-11-05"
---

# B-329 · Die Übungs-Validierung meldet eine unbekannte Id als `validation_error`, die Kategorie daneben als `invalid_reference`

[B-258](B-258-unbekannte-fach-id-zwei-fehlercodes.md) hat die unbekannte Referenz an den sechs freigegebenen
Fundstellen (Lehrbuch, Grammatikthema, Unit) auf `invalid_reference` gezogen. Der Übungs-`POST`/`PUT` bleibt gespalten,
und zwar **innerhalb derselben Anfrage**: eine unbekannte `categoryId` meldet `invalid_reference`
(`ExerciseControllerBase.Update`, `CategoryValid`), eine unbekannte Interessen-, Grammatik- oder Vokabel-Id
`validation_error`:

- `ExerciseControllerBase.cs:133` (Interessen-Tags), `:216` (Grammatikthemen – Zwilling der Unit-Prüfung, die B-258
  umgestellt hat);
- `ExerciseControllers.cs:47,64` (unbekannte `vocabularyId` in `refs`/`items`); vermutlich weitere
  `ValidateConfigAsync`-Zweige (Lückentext mit unbekanntem `vocabKey`).

Außerhalb des Übungspfads, gefunden in der Nachschau von Lauf J: `VocabularyStoreController.cs:250` (POST) und `:344`
(PATCH) melden einen unbekannten `BaseFormKey` als `validation_error` – eine Referenz per Schlüssel statt Id, aber derselbe
Sinn („Client verzweigt nach Code“).

Diese Prüfungen liefern einen **Text** (`string?`), den der Aufrufer pauschal als `validation_error` meldet – die
Umstellung braucht also einen Rückgabetyp, der den Code trägt, nicht nur ein Austauschen der Konstante.

Bewusst nicht in B-258 genommen: dessen Freigabe nennt die sechs Fundstellen ausdrücklich; der Übungspfad ist ein
anderer Mechanismus und ein weiterer Vertragsbruch, den der Nutzer nicht gesehen hat.

## User Story

Als **Client** (Frontend, KI-Agent), der nach dem Fehlercode verzweigt, möchte ich eine unbekannte Id oder einen unbekannten
Schlüssel im Übungspfad und im Vokabelspeicher am selben Code erkennen wie an den übrigen Stellen: `invalid_reference`.

## Ist-Stand am Code (vor dem Bau, Stand `687abb0`)

- `ExerciseControllerBase.cs:133` (Interessen-Tags) und `:216` (Grammatikthemen), `ExerciseControllers.cs:47/66` (refs/items),
  `:361` (Lückentext-`vocabKey`), `VocabularyStoreController.cs:254/357` (`BaseFormKey`): Text-Rückgaben, die der Aufrufer pauschal
  als `validation_error` meldete. Daneben `CategoryValid` mit `invalid_reference` – zwei Codes für denselben Sinn in einer Anfrage.
- Item-Routen `…/vocabulary/{id}/items` (POST/PATCH, `:212-214`/`:260-262`): dieselbe Lücke, vom Review gefunden.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Wie B-258:** nur der Code wechselt auf `invalid_reference`; Status 400 und Meldungstext bleiben.
2. **Ein Rückgabetyp, der den Code trägt** (`Errors/RequestProblem.cs`, `Invalid`/`UnknownReference`) statt eines Texts – sonst
   kann der Aufrufer die zwei Fälle nicht unterscheiden. `ValidateConfigAsync` und die beiden Prüfer geben ihn zurück.
3. **Regelbrüche bleiben `validation_error`:** Id ≤ 0, fehlende Sprachen, Thema eines anderen Fachs, Reihe ohne Fach, Liste ohne
   Anleitung, doppeltes Wort.
4. **Stapelpfad bleibt:** `BatchItemStatus` trägt keine Codes; ein unbekannter `BaseFormKey` bleibt dort `Error`.
5. **Vertragsbruch angenommen** (Code-Wechsel an fünf Stellen); der Frontend-Client verzweigt nicht darauf.

## Akzeptanzkriterien

1. Je Fundstelle liefert eine unbekannte Referenz `400 invalid_reference`; eine Id 0 bleibt `validation_error`.
2. Status und `detail` sind unverändert.
3. Der Vertragstext von `ExercisePayload` nennt den neuen Code.

## Schätzung

`M`, `wo: backend`, keine Migration, `vertragsbruch: ja`. Testweg: `UnknownReferenceCodeTests` (neu), zwei Fälle in
`ExerciseMetadataTests` nachgezogen, je Fundstelle ein Mutant.

## Verlauf

- **2026-10-28** — angelegt beim Bau von B-258 in Lauf J ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); Fundstellen
  per `grep` gelesen, nicht gemessen.
- **2026-11-05** — gebaut in Lauf U, Sprint 4 ([Protokoll](../pm-sitzung-2026-11-05.md)). AK 1 → `UnknownReferenceCodeTests.cs:41/54/74/95/110/121`, Interessen über `ExerciseMetadataTests` (zwei Fälle von `validation_error` auf `invalid_reference` nachgezogen). Rote Proben, je Fundstelle zurück auf `validation_error`: refs, inline items, Lücke, Grammatikthema, Store-POST, Store-PATCH → je genau der eigene Fall rot (die Gesamtzahl der gefilterten Läufe wurde nicht mitgeschrieben); Interessen → genau die zwei `ExerciseMetadataTests`; Item-POST und Item-PATCH → je der Item-Routen-Fall (1 von 6). AK 2: die Meldungstexte sind wörtlich gleich (Diff). AK 3: `ExerciseAuthoringDtos.cs:17-24`, `docs/openapi/v1.json` vom Testlauf neu geschrieben.
- **2026-11-05** — **abgenommen.** Volle Suite 1085/1085, `dotnet format` ohne Befund. Review `pugling-reviewer`: zwei 🟡 – Item-Routen (Code + Test, **Zähler Sprint 4: 1 von 5**) und der Vertragskommentar (nur Text, ohne Zählung); 🟢: Übersetzung prüft `vocabularyId` gar nicht → [B-386](B-386-uebersetzung-speichert-unbekannte-vokabel-id.md). Rollengang: der Creator-Weg „Übung mit Store-Bezug anlegen" läuft durch die neuen Integrationsfälle gegen den In-Process-Server; das Frontend verzweigt nicht auf den Code (grep `invalid_reference` in `frontend/src` ohne Treffer außer dem generierten Vertrag), Vater und Sohn unberührt. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `f4cf656`.
- **2026-11-05** — **Nachschau** (Lauf V, Retro Sprint 5; frischer `pugling-reviewer`, `git archive f4cf656` gebaut und getestet: 1085/1085). Alle Fundstellen der Story bestätigt, keine weitere `validation_error`-Stelle für eine fehlende Zeile. **Entgangen (🟢):** zwei Id-0-Fälle ohne Test → [B-388](B-388-id-null-faelle-ohne-test.md). Außerhalb des Diffs: Birkenbihl prüft die Id gar nicht → [B-387](B-387-birkenbihl-speichert-unbekannte-vokabel-id.md); Leerzeichen-Grundform → [B-389](B-389-leerzeichen-grundform-meldet-invalid-reference.md).
