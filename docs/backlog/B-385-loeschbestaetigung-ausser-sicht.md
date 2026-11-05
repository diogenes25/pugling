---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Löschbestätigung außer Sicht]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-u
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-381]
nachgeschaut: "2026-11-05"
---

# B-385 · Seit B-381 landet „Reihe … gelöscht." in einer langen Liste wieder außerhalb des Sichtbereichs

B-381 lässt eine Bestätigung nur noch scrollen, wenn ihr Absender `meldeVonWeitWeg` benutzt – heute nur `PublisherAdmin`.
`SeriesRow` bekommt `melde` (`VaterLehrwerke.tsx:220`). Damit scrollt auch die Bestätigung **nicht** mehr, deren Zeile beim
Speichern verschwindet: Löschen einer Reihe (`:284-285`), Umbenennen mit Umsortierung, Speichern, das die Zeile aus dem Filter
fallen lässt (`:474-475`). Bei dreißig Reihen steht „Reihe „X" gelöscht." unter der Tabelle außer Sicht, und ein geglücktes
Löschen sieht wieder aus wie nichts – genau der Fall, für den B-173 (AK 2) den Platz gebaut hat. B-381s Entscheidung wog nur
das Anlegen von Units; keine Spec prüft hier den Sichtbereich (die B-173-Specs nutzen `toBeVisible`).

Gefunden in der Nachschau zu Lauf U, Sprint 3 (frischer `frontend-reviewer`, `git show 2a9add3`, 🟡).

## User Story

Als **Creator** mit einer langen Reihen-Liste möchte ich nach dem Löschen oder Speichern einer Reihe die Bestätigung sehen,
auch wenn die Zeile dabei verschwindet oder wegsortiert wird.

## Ist-Stand am Code (vor dem Bau, Stand `6c00986`)

- `frontend/src/vater/VaterLehrwerke.tsx:220`: `SeriesRow` bekam nur `melde`, und reichte es an `remove` (`:284`) und an
  `SeriesForm` (`:361`) weiter. Beide Bestätigungen scrollten darum nicht, der Platz liegt unter der Tabelle.

## Entscheidungen

1. **(Im Auftrag des Nutzers, siehe Verlauf)** Eine Bestätigung holt den Platz, wenn ihre Zeile dabei verschwinden kann:
   Löschen der Reihe und Speichern in `SeriesForm`; dazu weiter das Verlags-Vokabular. Units bleiben ohne Scroll.
2. **(Bau)** `SeriesRow` bekommt einen zweiten Prop `onMeldungZeileWeg` (`:269`), verdrahtet mit `meldeVonWeitWeg` (`:224`);
   `remove` (`:287`, `:290`) und `SeriesForm` (`:367`) nutzen ihn, der Umschalter und `UnitPanel` weiter `onMeldung`. Kein
   dritter Weg in den Effekt – „weit weg" deckt beide Gründe ab, der Kommentar am Effekt (`:155-158`) nennt sie.

**Absender der Seite** (freiwillig nach Vorschlag (BK), nicht als Tor):

| Absender | Zeile kann verschwinden? | Callback | Test |
|---|---|---|---|
| `SeriesRow.remove` | ja | `meldeVonWeitWeg` | `reihe-bestaetigung-im-bild.spec.ts:51` |
| `SeriesForm` (Speichern) | ja (Umsortieren, Filter) | `meldeVonWeitWeg` | `reihe-bestaetigung-im-bild.spec.ts:64` |
| `PublisherAdmin` | – (weit weg) | `meldeVonWeitWeg` | `bestaetigung-scrollt-zum-platz.spec.ts` |
| `UnitPanel`/`UnitForm` | nein | `melde` | `unit-anlegen-bleibt-im-bild.spec.ts:22` |
| `NewSeries` | nein | `melde` | Handy-Frage offen in B-384 |
| Umschalter (`onToggle`, Bearbeiten) | – (räumt nur) | `melde` | – |

## Akzeptanzkriterien

1. Nach dem Löschen einer oberen Reihe in einer langen Liste steht „Reihe „X" gelöscht." im Sichtbereich.
2. Nach dem Speichern einer oberen Reihe steht „Gespeichert." im Sichtbereich.
3. „Unit hinzufügen" lässt das offene Formular weiter im Bild (B-381, Gegenprobe).

## Schätzung

`XS`, `wo: frontend`, keine Migration, kein Vertragsbruch. Testweg: neue Spec `e2e/reihe-bestaetigung-im-bild.spec.ts`
(zwei Fälle, Vorbedingung „Platz unter dem Sichtbereich" nachgerechnet), `unit-anlegen-bleibt-im-bild.spec.ts` als Gegenprobe.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau (Lauf U, Sprint 4). Entgleitung im Increment **dieses** Laufs: **Limit 2**, Lauf U
  endet nach Sprint 4.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es
  etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen"; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)):
  die Regel aus B-381 wird verfeinert – **eine Bestätigung holt den Platz, wenn ihre Zeile dabei verschwinden kann** (Löschen der
  Reihe, Speichern der Reihe in `SeriesForm`), dazu weiter das Verlags-Vokabular. Units anlegen, ändern und löschen bleiben
  ohne Scroll, weil ihr Formular stehen bleibt. Testweg: eine Spec „Reihe in langer Liste löschen" mit `toBeInViewport`, die
  B-381-Spec als Gegenprobe. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben
  Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidung selbst ausformulieren, schätzen und bauen; trifft er
  auf eine Produktfrage, die sie nicht hergibt, hält er an.
- **2026-11-05** — ausformuliert und geschätzt in Lauf V, Sprint 5 ([Protokoll](../pm-sitzung-2026-11-05.md)), auf Grundlage
  der Entscheidung oben; keine neue Produktfrage.
- **2026-11-05** — gebaut. AK 1 → `e2e/reihe-bestaetigung-im-bild.spec.ts:46` (heute `:51`; Vorbedingung `:42`; im ersten Lauf griff sie
  zu Recht – die Reihen sortierten nach unten, darum Präfix „0"). Rote Probe 1: `remove` meldet über `onMeldung` → 1 failed in
  `:46` (`toBeInViewport`), `:59` und die Units-Spec grün. AK 2 → `:59`; Rote Probe 2: `SeriesForm` bekommt `onMeldung` → 1 failed
  in `:59`, `:46` und die Units-Spec grün. AK 3 → `unit-anlegen-bleibt-im-bild.spec.ts:22`, in beiden Proben grün. Vitest
  648/648, `tsc -b` sauber.
- **2026-11-05** — Review `frontend-reviewer`: Verdrahtung je Absender nachgeprüft, kein 🔴. **🟡** Die Vorbedingung des
  Lösch-Falls rechnete nicht ein, dass die gelöschte Zeile den Platz ohne Scroll um ihre Höhe nachrücken lässt – bei knappem
  Abstand wäre `toBeInViewport` grün geworden, obwohl nichts gescrollt hat. Behoben: `platzLiegtUnterDemBild(page, nachruecken)`
  verlangt den Abstand plus die Zeilenhöhe (`:38-49`); Probe 1 wiederholt → 1 failed in `:51`, `:64` grün. **Zähler Sprint 5:
  1 von 5.** 🟢 Prop-Doku von `onMeldung` sprach noch von „ein Prop", ergänzt (nur Text).
- **2026-11-05** — **abgenommen.** Playwright 113 passed (volle Suite = Rollengang aus der Creator-Rolle), Vitest 648/648, `tsc -b`
  sauber. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `b27dc51`.
- **2026-11-05** — **Nachschau** (Lauf V, Retro Sprint 6; frischer `frontend-reviewer`, `git show b27dc51`; 7 passed in den
  vier Lehrwerke-Specs). Verdrahtung je Tabellenzeile und beide Vorbedingungen bestätigt. **Entgangen (🟡):** die Tabellenzeile
  „`UnitPanel`/`UnitForm` – nein" gilt nur fürs Anlegen; Löschen und Ändern einer Unit lassen Zeile bzw. Formular verschwinden →
  [B-390](B-390-unit-loeschen-bestaetigung-ausser-sicht.md). 🟢 hingenommen: das Speichern einer Reihe scrollt jetzt auch, wenn die
  Zeile bleibt (Notizen, Sprache) – so entschieden; eine Verfeinerung („nur wenn die Zeile das Ergebnis verlässt") wäre möglich, ist
  aber ohne Fund.
