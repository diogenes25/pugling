---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [Loeschen meldet nichts, stumme Erfolgspfade]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Reviewer-Fund zu Sprint 3 des Nachtlaufs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-17: alle drei Fundorte (ChildMaterialSection:258-261, VaterFachlehrer:310-318, VaterLehrwerke:814-815) gegen die Kriterien gelesen, Reihenfolge Dialog→onMeldung(null) geprüft; Spec-Zeile korrigiert (77 statt 75)"
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

1. ~~Alle drei, oder nur die, deren Zeile verschwindet?~~ → Entscheidung 1.
2. ~~Gilt dasselbe für weitere `action.run` ohne `okText`, die nicht löschen?~~ → Entscheidung 2.

## Entscheidungen

1. **Alle drei, und jede Meldung nennt den Gegenstand.** Nach B-196s Entscheidung 1 muss eine
   Löschmeldung den Namen tragen — die Zeile, auf die sie sich bezieht, ist ja gerade verschwunden. Ohne
   ihn wäre „Entfernt." eine Meldung ohne Gegenstand. **Kosten:** drei Literale, je einmal vergeben und an
   zwei Stellen benutzt („ein Text, zwei Anzeigeorte", das Muster der Nachbarn).
2. **Nicht ausgeweitet.** `frontend/CLAUDE.md` erlaubt stummen Erfolg ausdrücklich, wo die Änderung selbst
   die Rückmeldung ist (Tag-Editor: ein Banner je Chip wäre Lärm). Beim Löschen ist sie das gerade nicht —
   das Verschwinden einer Zeile kann auch heißen, dass sie aus der Filterung gefallen ist. Der Schnitt
   läuft also am **Löschen**, nicht an „hat kein `okText`". **Kosten:** die übrigen stummen Pfade bleiben
   stumm; das ist Absicht und keine Restschuld.

## Rote Proben (erwartet / gemessen)

| Probe | erwartet | gemessen |
| --- | --- | --- |
| `UnitForm.remove` ohne Namen im Text | 1 | **1**, an Zeile 77 (Nachschau 2026-10-17: vorher „75" notiert, das ist der Kommentar davor) |
| Buch- und Fachlehrer-Meldung geleert | 2 | **2** |

**Die zweite Zeile gäbe es ohne die Review-Runde nicht.** Sie hat aufgedeckt, dass „rote Probe erwartet 1 /
gemessen 1" nur den **Unit**-Pfad betraf — die anderen beiden waren unverifiziert, und dass sie
funktionieren, wäre eine Behauptung geblieben. Dafür ist `e2e/loeschen-meldet-sich.spec.ts` entstanden.

## Verlauf

- 2026-09-02 · Aufgenommen aus dem `frontend-reviewer`-Review zu Sprint 3. Er hat die Zählung in B-196s
  Entscheidung 3 widerlegt: vier statt zwei. Die Fundorte sind vom Reviewer benannt und von mir
  gegengelesen.
- **2026-09-05** — **ausformuliert → abgenommen** im Sprint „Eine Rückmeldung gehört an einen Platz, der sie überlebt — und es darf nur einer
  sein" (B-187/B-200/B-212 gemeinsam). Autonom gegrillt unter Freigabe 1 (`art: Defekt`).
  **Rollengang:** `e2e/ein-meldungsplatz.spec.ts` (Unit) und `e2e/loeschen-meldet-sich.spec.ts` (Buch am
  Kind, Fachlehrer) fahren alle drei Pfade im echten Browser gegen einen echten Server. Kein
  Browser-Handgang möglich (keine Chrome-Anbindung), damit greift der dokumentierte Ersatz.
  Belege: `tsc -b` sauber, `vitest run` **368/368** grün, `playwright test` **63/63** grün.
  `frontend-reviewer` gelaufen, **kein Blocker**; seine acht Funde mit Code- oder Testwirkung sind
  eingearbeitet (Protokoll `docs/pm-sitzung-2026-09-05.md`). Commit `3d763fe`.
- **2026-10-17** — **Nachschau** (Nachtlauf, Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`): alle drei Fundorte gebaut, jede Meldung nennt den Gegenstand, Reihenfolge Dialog → `onMeldung(null)` überall eingehalten. Zeilenangabe der roten Probe korrigiert (77 statt 75). Kein Defekt.
