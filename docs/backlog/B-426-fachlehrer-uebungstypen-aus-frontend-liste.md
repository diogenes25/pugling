---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Fachlehrer-Typen roh, Bevorzugte Übungstypen ohne Manifest]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-creator
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-426 · Fachlehrer-Profil: „Bevorzugte Übungstypen" zeigt vier rohe Schlüssel aus einer Frontend-Liste

Im Rollengang vom 2026-11-08 sah der Lehrer unter `/vater/fachlehrer` die Knöpfe **VOCABULARY · CLOZE ·
TRANSLATION · GRAMMAR** – englische Schlüssel statt „Vokabelkarten", und nur vier von zwölf Typen. Die
Hausregel des Frontends sagt: Übungstypen kommen aus dem Server-Manifest, nie aus einer Tabelle im Frontend
(`frontend/CLAUDE.md`).

## User Story

Als *Creator* möchte ich im Fachlehrer-Profil **alle** Übungstypen mit ihrem deutschen Namen anklicken können,
damit der KI-Creator meine Vorlieben vollständig kennt und ich nicht raten muss, was „CLOZE" ist.

## Ist-Stand am Code

- `VaterFachlehrer.tsx:24` – `const TYPES = ["Vocabulary", "Cloze", "Translation", "Grammar"] as const;`
  `:431-436` rendert `{t}` roh als `toggle-pill`.
- Das Manifest liegt bereit: `GET creator/exercise-types` liefert `type` + `label` für zwölf Typen (Rollengang:
  Vokabelkarten … Birkenbihl); `exerciseTypes.ts:52` `useExerciseTypes()` ist der vorgesehene Zugang und wird
  im Anlege-Formular schon benutzt.
- Das Profil speichert `DefaultTypes` als JSON-Liste von Schlüsseln (`backend/Pugling.Api/CLAUDE.md`,
  „Creator-Profile": „JSON-Liste → neu zuweisen"); ob der Server unbekannte Schlüssel abweist, ist nicht
  belegt – im Nachtlauf prüfen (`CreatorProfilesController`), ggf. als Teil der Reparatur ergänzen.

## Die echte Lücke

Eine dritte handgepflegte Typliste, obwohl das Manifest und der Hook existieren – genau der Drift, den die
Hausregel verhindern soll.

## Offene Punkte

Keine Produktfrage; Defekt, autonom grillbar. Empfehlung: `useExerciseTypes()`; Knopf zeigt `label`, speichert
`type`; Typen, die der Agent nicht erzeugen kann, nicht ausfiltern (das weiß der Agent selbst). Server:
unbekannter Schlüssel → `400 validation_error`, falls heute nicht so.

## Akzeptanzkriterien (Entwurf)

1. Die Knöpfe zeigen die zwölf Manifest-Namen (deutsch) und speichern die Manifest-Schlüssel.
2. Ein gespeichertes Profil mit `["Cloze"]` zeigt „Lückentext" gedrückt.
3. `PATCH creator/profiles/{id}` mit einem unbekannten Typ-Schlüssel → `400 validation_error` (prüfen, ggf.
   herstellen, Regressionstest).
4. Vitest `VaterFachlehrer.test.tsx`: Fälle 1–2; Mutante „Label durch Schlüssel ersetzen" → rot.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Creator ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt (`ausformuliert`); Grillen und Schätzen im Nachtlauf (Defekt, Freigabe 1).
