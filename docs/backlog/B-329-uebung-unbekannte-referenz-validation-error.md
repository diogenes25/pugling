---
tags: [typ/story, status/idee, bereich/backend, bereich/vertrag, rolle/creator]
aliases: [Übungs-Validierung meldet unbekannte Ids als validation_error]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: B-258 (Lauf J, docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
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

Diese Prüfungen liefern einen **Text** (`string?`), den der Aufrufer pauschal als `validation_error` meldet – die
Umstellung braucht also einen Rückgabetyp, der den Code trägt, nicht nur ein Austauschen der Konstante.

Bewusst nicht in B-258 genommen: dessen Freigabe nennt die sechs Fundstellen ausdrücklich; der Übungspfad ist ein
anderer Mechanismus und ein weiterer Vertragsbruch, den der Nutzer nicht gesehen hat.

## Verlauf

- **2026-10-28** — angelegt beim Bau von B-258 in Lauf J ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); Fundstellen
  per `grep` gelesen, nicht gemessen.
