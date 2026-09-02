---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [Loeschen meldet nichts, stumme Erfolgspfade]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Reviewer-Fund zu Sprint 3 des Nachtlaufs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-200 · Drei Löschwege melden gar nichts

[B-196](B-196-loeschen-meldet-nichts-und-raeumt-nichts.md) hat den einen Löschpfad behoben, der von einem
Akzeptanzkriterium beansprucht war. Beim Review kam heraus, dass es **vier** waren, nicht zwei — die drei
übrigen stehen hier.

## User Story

Als **Vater** möchte ich nach jedem Löschen eine Bestätigung sehen, damit ich nicht am Verschwinden einer
Zeile ablesen muss, ob der Server zugestimmt hat.

## Ist-Stand am Code

Alle drei nach demselben Muster: `action.run(…)` **ohne** `okText`, danach `onDone()` ohne Meldung.

| Fundort | Was stumm bleibt |
|---|---|
| `ChildMaterialSection.tsx` (`remove`) | ein Buch am Kind entfernen |
| `VaterFachlehrer.tsx` (`remove`) | ein Fachlehrer-Profil löschen |
| `VaterLehrwerke.tsx` (`UnitForm.remove`) | eine Unit löschen |

**Der Unterschied zu B-196, und er ist der Grund für die niedrigere Priorität:** Dort blieb zusätzlich
eine **alte** Meldung stehen und las sich als Löschbestätigung. Hier räumt der „Bearbeiten"-Knopf bzw.
seit Sprint 3 auch das Absenden den Platz — es ist reines Schweigen, kein Widerspruch.

**`entgangen_bei` bleibt leer:** Diese drei Pfade waren vor wie nach Sprint 1 stumm und von keiner Story
beansprucht.

## Offene Punkte

1. Alle drei, oder nur die, deren Zeile verschwindet? Empfehlung: **alle drei** — nach B-196s
   Entscheidung 1 muss eine Löschmeldung ohnehin den Namen nennen, und das ist überall dieselbe Zeile.
2. Gilt dasselbe für weitere `action.run` ohne `okText`, die **nicht** löschen? Empfehlung: nachsehen,
   nicht vermuten — `frontend/CLAUDE.md` erlaubt stummen Erfolg ausdrücklich, wo die Änderung selbst die
   Rückmeldung ist (Tag-Editor). Beim Löschen ist sie das gerade **nicht**.

## Verlauf

- 2026-09-02 · Aufgenommen aus dem `frontend-reviewer`-Review zu Sprint 3. Er hat die Zählung in B-196s
  Entscheidung 3 widerlegt: vier statt zwei. Die Fundorte sind vom Reviewer benannt und von mir
  gegengelesen.
