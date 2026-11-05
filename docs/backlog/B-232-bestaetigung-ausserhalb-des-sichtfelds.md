---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Gespeichert außerhalb des Sichtfelds, Meldungsplatz scrollt nur bei Fehlern]
status: abgenommen
prio: P3
art: Frage
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 2, Fund D1)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-11-05"
---

# B-232 · Soll eine Bestätigung zum Meldungsplatz scrollen, wenn er weit vom Knopf entfernt liegt?

Die Lehrwerke-Seite hat seit B-212 **einen** Meldungsplatz – unter der Reihen-Tabelle, am Ende des ersten
Abschnitts (`frontend/src/vater/VaterLehrwerke.tsx`, `meldungsplatz`). Der Scroll-Effekt dorthin greift
**nur bei Fehlern** (`if (meldung && !meldung.ok) …scrollIntoView({ block: "nearest" })`), mit begründeter
Entscheidung im Kommentar: „Eine Bestätigung, die man nicht sieht, ist verschmerzbar; ein Fehler, den man
nicht sieht, ist der stumme Pfad aus B-200." Diese Abwägung kannte als Sender nur die Formulare direkt unter
der Tabelle. Seit B-224 meldet auch das **Verlags-Vokabular** dorthin – und das liegt unter dem gesamten
Anlege-Formular, zwei Abschnitte tiefer.

**Gemessen im Rollengang am 2026-10-18** (Desktop, 889 px Höhe, vier geseedete Reihen): nach dem Umbenennen
eines Verlags stand „Gespeichert." bei Viewport-y 302, der gedrückte OK-Knopf bei y 807 – beides sichtbar, 505 px
auseinander. Mit dreißig Reihen (die Tabelle hat kein Paging, je Zeile ~56 px) läge der Platz rund 1,7 Bildschirme
über dem Knopf: der Creator sähe nur, dass der OK-Knopf verschwindet (`dirty` kippt nach dem Reload), und läse
keine Rückmeldung. Weder E2E (`toHaveCount`/`toBeVisible` prüfen nicht den Viewport) noch der Rollengang vom
2026-10-17 (`.banner.ok` per Skript gelesen) messen den Sichtbereich.

Es ist eine **Frage**, kein Defekt: B-212 hat „Bestätigung darf unsichtbar sein" ausdrücklich entschieden;
B-224 hat den Sender hinzugefügt, ohne die Entscheidung neu zu wägen. Kandidaten: die Bedingung `!meldung.ok`
fallen lassen (`block: "nearest"` scrollt ohnehin nur, wenn nötig – eine sichtbare Bestätigung bleibt ein
No-op); oder den Meldungsplatz ans Seitenende/sticky setzen. Beides ändert, wie die Seite sich beim Speichern
bewegt – Produktgefühl, darum beim Nutzer.

## User Story

Als **Creator**, der im Verlags-Vokabular einen Verlag umbenennt, möchte ich die Bestätigung sehen, auch wenn der
Meldungsplatz der Seite weit über dem Knopf liegt.

## Ist-Stand am Code (vor dem Bau, Stand `37e6f6e`)

- `frontend/src/vater/VaterLehrwerke.tsx:154`: `if (meldung && !meldung.ok) meldungsplatz.current?.scrollIntoView({ block: "nearest" })`
  – nur ein Fehler holte den Platz in den Sichtbereich.
- Sonst nutzt keine Seite `scrollIntoView` (`grep -rn scrollIntoView frontend/src`, ohne Tests: ein Treffer) – der Sweep „übrige Seiten mit
  Meldungsplatz" hat nichts weiter zu ändern.

## Entscheidungen

1. **(Nutzer, 2026-10-23)** Auch eine Bestätigung scrollt, aber nur wenn nötig: `!meldung.ok` fällt, `block: "nearest"` bleibt.

## Akzeptanzkriterien

1. Nach einem geglückten Umbenennen im Verlags-Vokabular steht „Gespeichert." im Sichtbereich, wenn der Platz beim Klick
   außerhalb lag.

## Schätzung

`XS`, `wo: frontend`. Testweg: eigene E2E-Spec mit niedriger Fensterhöhe und `toBeInViewport` (die bestehenden prüfen nur
`toBeVisible`).

## Verlauf

- **2026-10-18** — angelegt aus der Nachschau zu Sprint 2 des Nachtlaufs 2026-10-17 (Fund D1, `frontend-reviewer`),
  im Browser nachgemessen (Werte oben). Als `Frage` abgelegt, weil B-212 den Punkt entschieden hat.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-2 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): auch eine **Bestätigung** scrollt zum Meldungsplatz, aber **nur wenn nötig** – die Bedingung `!meldung.ok` fällt, `scrollIntoView({ block: "nearest" })` bleibt (eine sichtbare Meldung bewegt nichts). Damit ist B-212s Abwägung („eine unsichtbare Bestätigung ist verschmerzbar“) zurückgenommen. Rest ist Handwerk: dieselbe Regel für die übrigen Seiten mit einem Meldungsplatz prüfen.
- **2026-10-23** — **Freigabe des Nutzers (Autonomer Modus, Opt-in je Vorhaben, README):** ein Nachtlauf darf diese Story auf Grundlage der getroffenen Entscheidung selbst ausformulieren, schätzen und bauen (Paket NP-14); trifft er dabei auf eine Produktfrage, die die Entscheidung nicht hergibt, hält er an und notiert sie. Rollengang und Reviewer bleiben Pflicht. Erteilt in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)).
- **2026-11-05** — ausformuliert, geschätzt und gebaut in Sprint 1 der Sitzung 2026-11-05 ([Protokoll](../pm-sitzung-2026-11-05.md)). AK 1 → neue Spec `e2e/bestaetigung-scrollt-zum-platz.spec.ts` (Vorbedingung: Platz beim Klick oberhalb, `top < 0`). Rote Probe: `!meldung.ok` zurück → die Spec fällt in Zeile 46 (`toBeInViewport`), erwartet 1 failed, gemessen 1 failed; mit Fix 1 passed.
- **2026-11-05** — **abgenommen.** Die Spec ist zugleich der Rollengang aus der Creator-Rolle (echter Browser, echter Server); volle E2E-Suite 110/110, Vitest 648/648. Review `frontend-reviewer`: zur Spec kein Fund. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `c6876b4`.
- **2026-11-05** — **Nachschau** (frischer `frontend-reviewer`, `git show c6876b4 -- frontend/`, read-only): kein Scroll beim Laden oder `reload` (`meldung` startet `null`, nur `setMeldung` setzt es). **Entgangen (🟡):** die Entscheidung wog nur den entfernten Absender – Bestätigungen aus Formularen in der Tabelle scrollen jetzt auch und schieben das offene Formular aus dem Bild → [B-381](B-381-erfolgs-scroll-springt-aus-dem-formular.md).
