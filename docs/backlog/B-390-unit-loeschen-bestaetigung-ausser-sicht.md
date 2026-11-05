---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Unit-Löschbestätigung außer Sicht]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
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

## User Story

Als **Creator** möchte ich nach dem Löschen oder Ändern einer Unit die Bestätigung sehen, auch wenn die Liste der Reihen lang
ist – sonst sieht ein geglücktes Löschen aus wie nichts.

## Entscheidungen

1. **(im Auftrag entschieden, siehe Verlauf)** Das Kriterium gilt je **Handlung**: Löschen und Ändern einer Unit holen den Platz
   (`meldeVonWeitWeg`), Anlegen nicht (B-381).
2. **(Bau)** Verdrahtet je **Formular-Instanz**: das Bearbeiten-Formular einer Unit bekommt den holenden Kanal, das
   Anlege-Formular den nahen. Löschen gibt es nur im Bearbeiten-Formular, also deckt eine Stelle beide Handlungen.

## Akzeptanzkriterien

1. Löschen einer Unit in einer langen Liste → „Unit „X" gelöscht." steht im Sichtbereich.
2. Speichern einer Unit-Änderung in einer langen Liste → „Gespeichert." steht im Sichtbereich.
3. Anlegen einer Unit lässt das offene Formular im Bild (Gegenprobe B-381, unverändert grün).

## Schätzung

`XS`, `wo: frontend`, eine Prop durchreichen, zwei Kommentare berichtigen, eine Spec mit zwei Fällen nach dem Muster von B-385.

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
- **2026-11-05** — ausformuliert, geschätzt und gebaut in Lauf W, Sprint 7. Handlungs-Tabelle (Vorschlag (BM), per `grep` der
  Aufrufe von `onMeldung`/`onMeldungZeileWeg`/`nachObenMelden`): Reihe löschen/speichern → holend (B-385); Unit ändern/löschen →
  **holend (neu)**; Unit anlegen, Reihe/Verlag anlegen → nah; Verlags-Vokabular → holend; Öffnen/Schließen → nur räumen.
  AK 1 → `frontend/e2e/unit-bestaetigung-im-bild.spec.ts:73`, AK 2 → `:86`, AK 3 → `unit-anlegen-bleibt-im-bild.spec.ts` (grün).
  Rote Probe: das Bearbeiten-Formular bekommt wieder `onMeldung` → beide Fälle rot an ihrer letzten Zeile (`:73`, `:86`), nach der
  Review-Korrektur wiederholt.
- **2026-11-05** — Review `frontend-reviewer`: kein 🔴; Verdrahtung je Handlung bestätigt. 🟡 Kommentar am Effekt sagte noch „aus
  einem Formular in der Tabelle scrollt sie nicht" (nur Text); 🟢 Prop-Doku an `SeriesRow` nannte die Units nicht (nur Text);
  🟢 die Vorbedingung des Löschfalls zog nur Unit- und Formularzeile ab, obwohl mit der einzigen Unit die ganze Tabelle verschwindet
  – jetzt wird die ganze Tabelle abgezogen (strenger), **Zähler Sprint 7: 1 von 5**; 🟢 `exact` am Knopf „Unit hinzufügen".
- **2026-11-05** — **abgenommen.** Rollengang Creator als E2E: volle Playwright-Suite 115 passed (die zwei neuen Fälle dabei, `unit-anlegen-bleibt-im-bild.spec.ts` als Gegenprobe grün), Vitest 648/648, `tsc -b` sauber. Vater und Sohn unberührt (nur `/vater/lehrwerke`, Creator-Perspektive), ihre Specs in derselben Suite grün. Commit: siehe nächster Eintrag.
