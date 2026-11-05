---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Erfolgs-Scroll springt]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#retrospektive-sprint-2
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-232]
nachgeschaut: "2026-11-05"
---

# B-381 · Seit B-232 scrollt jede Bestätigung zum Meldungsplatz – auch die aus einem Formular mitten in der Tabelle

B-232 hat die Bedingung `!meldung.ok` am Scroll-Effekt der Lehrwerke-Seite fallen lassen (`VaterLehrwerke.tsx:155`). Die
Entscheidung wog nur den **entfernten** Absender (Verlags-Vokabular, zwei Abschnitte tiefer). Für die Absender **in** der
Tabelle bricht sie das Sprint-Ziel („verliert seinen Platz nicht"):

- **Units hintereinander anlegen:** Reihe 2 von dreißig ist aufgeklappt, jede „Unit hinzugefügt." scrollt zum Platz unter
  der Tabelle, das offene `UnitForm` verlässt den Bildschirm, und vor der nächsten Unit muss der Creator zurückscrollen.
- **Verlag nebenbei anlegen** im halb ausgefüllten „Reihe anlegen": die Bestätigung scrollt nach oben, das Formular, in das
  er tippt, kann den Bildschirm verlassen.

Gefunden in der Nachschau zu Sprint 1 der Sitzung 2026-11-05 (frischer `frontend-reviewer`, `git show c6876b4`, 🟡).

## User Story

Als **Creator**, der Units hintereinander anlegt, möchte ich nach jedem Speichern mein offenes Formular im Bild behalten –
und eine Bestätigung aus dem weit entfernten Verlags-Vokabular trotzdem sehen.

## Ist-Stand am Code (vor dem Bau, Stand `9fd94d8`)

- `frontend/src/vater/VaterLehrwerke.tsx:155`: `if (meldung) meldungsplatz.current?.scrollIntoView({ block: "nearest" })` – jede
  Meldung jedes Absenders holte den Platz unter der Tabelle.

## Entscheidungen

1. **(Im Auftrag des Nutzers, siehe Verlauf)** Fehler scrollen immer; eine Bestätigung nur, wenn ihr Absender es verlangt –
   heute nur `PublisherAdmin`. Kein klebender Platz.
2. **(Bau)** Der Absender sagt es über die Funktion, die er bekommt: `melde` oder `meldeVonWeitWeg` (`:157-158`); beide setzen eine
   Ref vor `setMeldung`, der Effekt liest sie (`:160`). `NewSeries` gilt als nah (Platz direkt darüber); die Frage, ob das auf dem
   Handy hält, ist [B-384](B-384-reihe-anlegen-bestaetigung-auf-dem-handy.md).

## Akzeptanzkriterien

1. Nach „Unit hinzufügen" in einer oberen Zeile bleibt der Knopf im Sichtbereich, obwohl der Platz weit darunter liegt.
2. Eine Bestätigung aus dem Verlags-Vokabular scrollt weiter in den Sichtbereich (B-232).

## Schätzung

`S`, `wo: frontend`, keine Migration, kein Vertragsbruch. Testweg: neue Spec `unit-anlegen-bleibt-im-bild.spec.ts` mit
nachgerechneter Vorbedingung, B-232-Spec als Gegenprobe.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau (Lauf T, Sprint 2). Eine Entgleitung im Increment **dieses** Laufs: **Limit 2**,
  der Lauf endet nach Sprint 2.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es
  etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen"; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)):
  eine **Bestätigung scrollt nur, wenn ihr Absender es verlangt** – das Verlags-Vokabular (`PublisherAdmin`) meldet mit einem
  Kennzeichen „weit weg", die Formulare in und direkt unter der Tabelle nicht; ein **Fehler** scrollt weiter immer. Ein
  klebender Meldungsplatz wurde verworfen: er verdeckte auf dem Handy dauerhaft Inhalt. Die Spec
  `bestaetigung-scrollt-zum-platz.spec.ts` bleibt gültig; dazu kommt ein Fall, dass „Unit hinzugefügt." das offene Formular
  nicht aus dem Sichtbereich schiebt. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit
  derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidung selbst ausformulieren, schätzen und bauen;
  trifft er auf eine Produktfrage, die sie nicht hergibt, hält er an.
- **2026-11-05** — gebaut in Lauf U, Sprint 3 ([Protokoll](../pm-sitzung-2026-11-05.md)). AK 1 → `e2e/unit-anlegen-bleibt-im-bild.spec.ts` (Vorbedingung `platzOben − Fensterhöhe > knopfUnten` nachgerechnet). Rote Probe 1: jede Bestätigung scrollt (`if (meldung)`) → erwartet 1 failed, gemessen 1 failed in Zeile 52 (`toBeInViewport`), B-232-Spec grün. AK 2 → `bestaetigung-scrollt-zum-platz.spec.ts`; Rote Probe 2: `PublisherAdmin` mit `melde` → 1 failed in deren Zeile 46, die neue Spec grün.
- **2026-11-05** — **abgenommen.** Vitest 648/648, `tsc -b` sauber, Playwright 111 passed (volle Suite = Rollengang aus der Creator-Rolle, die neue Spec fährt den Weg). Review `frontend-reviewer`: keine 🔴/🟡; drei 🟢 ohne Zählung – `onToggle` über `melde(null)` statt `setMeldung(null)` behoben (danach die vier Lehrwerke-Specs 7 passed), Handy-Frage als B-384 abgelegt, acht Reihen je Lauf in der Wegwerf-DB hingenommen. Zähler Sprint 3: 0 von 5. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `2a9add3`.
- **2026-11-05** — **Nachschau** (frischer `frontend-reviewer`, `git show 2a9add3`, read-only): Zeilen `:155/157-158/160/242`, Spec-Zeile 52 und „111 passed" (111 Tests per `grep`) nachgerechnet; Vorbedingung der Spec bestätigt, keine Verschmutzung anderer Specs. **Entgangen (🟡):** die Entscheidung nahm auch Bestätigungen das Scrollen, deren Zeile dabei verschwindet (Reihe löschen, umbenennen mit Umsortierung) → [B-385](B-385-loeschbestaetigung-ausser-sicht.md).
