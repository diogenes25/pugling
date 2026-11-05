---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Erfolgs-Scroll springt]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#retrospektive-sprint-2
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-232]
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
