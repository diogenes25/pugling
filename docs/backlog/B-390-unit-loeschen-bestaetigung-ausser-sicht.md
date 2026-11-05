---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Unit-Löschbestätigung außer Sicht]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-v
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-385]
---

# B-390 · „Unit … gelöscht." und „Gespeichert." einer Unit landen in einer langen Liste außer Sicht

B-385 führte das Kriterium ein „eine Bestätigung holt den Platz, wenn ihre Zeile dabei verschwinden kann" – und wandte es nur auf die
Reihen-Zeile an. `UnitPanel` bekommt weiter `melde` (`frontend/src/vater/VaterLehrwerke.tsx:379`). Für **Anlegen** stimmt das (das
Formular bleibt stehen, B-381), für die anderen beiden Unit-Handlungen nicht:

- **Löschen** (`UnitForm.remove`, `:786-797`) entfernt die Unit-Zeile.
- **Speichern einer Änderung** (`:778-783`) schließt das Formular über `onDone` → `setEditing(null)` (`:678`).

Der Platz steht nicht „direkt darunter", wie der Kommentar an `UnitPanel` (`:591-594`) und `:158` behaupten, sondern unter der
**ganzen** Reihen-Tabelle (`:237`). Bei 30 Reihen und aufgeklappter Reihe 2 steht „Unit „X" gelöscht." unter dem Sichtbereich, und
ein geglücktes Löschen sieht wieder aus wie nichts.

Gefunden in der Nachschau zu Lauf V, Sprint 5 (frischer `frontend-reviewer`, `git show b27dc51`, 🟡) und am Code bestätigt.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau (Lauf V, Retro Sprint 6). Entgleitung im Increment **dieses** Laufs: **Limit 2**, Lauf V
  endet nach Sprint 6. Die Absender-Tabelle in B-385 hatte die Zeile „`UnitPanel`/`UnitForm` – Zeile kann verschwinden? nein" –
  beantwortet für das Anlegen, nicht für Ändern und Löschen.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es
  etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen"; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)):
  das Kriterium aus B-385 gilt **je Handlung, nicht je Formular** – Löschen einer Unit und Speichern einer Unit-Änderung holen den
  Platz ins Bild (`meldeVonWeitWeg`), **Anlegen** einer Unit bleibt ohne Scroll (B-381). Die Kommentare an `UnitPanel` und `:158`
  werden berichtigt. Testweg: zwei Fälle nach dem Muster von `reihe-bestaetigung-im-bild.spec.ts` (Vorbedingung inkl. Höhe der
  verschwindenden Zeile), `unit-anlegen-bleibt-im-bild.spec.ts` als Gegenprobe. **Freigabe des Nutzers (Autonomer Modus, Opt-in,
  erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidung selbst
  ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergibt, hält er an.
