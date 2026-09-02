---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [gruenes Gespeichert ueber rotem Fehler, vierter setMeldung(null) fehlt]
status: abgenommen
prio: P2
art: Defekt
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-02 zu Sprint 1 des Nachtlaufs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-173, B-151]
wartet_auf: ""
---

# B-197 · Der Meldungsplatz wird beim Absenden nicht geräumt — und zeigt dann zwei Wahrheiten

Die Reparatur von Sprint 1 hat an dieser Stelle **eine neue Instanz genau des Defekts erzeugt, gegen den
sie gebaut wurde.**

## User Story

Als **Creator** möchte ich nicht gleichzeitig „Gespeichert." und eine Fehlermeldung sehen, damit ich weiß,
ob mein letzter Klick geglückt ist.

## Ist-Stand am Code

Geräumt wird an genau **drei** Stellen, und alle drei hängen am **Öffnen** eines Formulars:
`ChildMaterialSection.tsx:93`, `VaterFachlehrer.tsx:93`, `VaterLehrwerke.tsx:196` (`onOeffnen`).

Es fehlt das Räumen beim **Absenden** — und das trifft ausgerechnet die eine Fläche, auf der das Formular
offen bleibt:

- `VaterLehrwerke.tsx:415` setzt bei Erfolg den Seitenplatz, `SeriesForm` bleibt danach offen. Ein
  **zweites** Absenden derselben Sitzung, das scheitert (`action.fail("Der Name der Reihe fehlt.")` auf
  `:390`, oder ein Server-Fehler), zeigt den roten Banner am Formular — während das grüne „Gespeichert."
  am Seitenplatz (`:210`) stehen bleibt. **Zwei widersprechende Meldungen in einem Blickfeld.**
- Kleinere Ausprägungen derselben Lücke: `ChildMaterialSection.tsx:117` und `VaterFachlehrer.tsx:124`
  (Anlegen räumt nicht), `VaterLehrwerke.tsx:195` (die Unit-Ziehharmonika räumt nicht).

## Die echte Lücke

„Der Erfolg reist nach oben, Fehler bleiben lokal" ist **zustandsfrei** formuliert, der Seitenplatz ist
aber **zustandsbehaftet**. Solange nichts ihn räumt, überlebt eine Meldung die Handlung, die sie meinte.

## Entscheidungen

1. **Der Platz wird bei jedem Beginn einer Handlung geräumt, nicht nach einer Frist.** *Begründung:*
   B-151s Entscheidung 2 hat einen Timer schon einmal verworfen — `StatusBanner` kennt keinen, und einen
   einzuführen zwänge zwanzig andere Aufrufstellen, ihn mitzutragen. „Beginn einer Handlung" ist zudem
   der Zeitpunkt, an dem die alte Meldung *nachweislich* nicht mehr gilt, während eine Frist es rät.
   *Kosten:* Wer zwei Sekunden nach dem Speichern etwas anderes anfasst, verliert seine Bestätigung.
2. **Drei Anlässe, ein Prop.** `onOeffnen` und `onMessage` werden zu **einem** `onMeldung(m:
   ActionMessage | null)`; `null` heißt „ab jetzt gilt hier nichts mehr". Gerufen wird es beim Öffnen
   eines Formulars, **beim Beginn** jeder Handlung und beim Erfolg. *Begründung:* Zwei Props, die
   denselben Platz beschreiben, laufen auseinander — genau so ist die vierte Räum-Stelle entstanden
   (drei existierten, alle am Öffnen). Ein Prop mit `null` als Wert kann man nicht halb verdrahten.
   *Kosten:* `ActionMessage | null` an einer Prop-Grenze ist unschärfer als zwei benannte Ereignisse;
   dafür gibt es nur noch eine Stelle, die man vergessen kann.
3. **Auch die Formulare, die sich schließen, räumen beim Beginn.** `TextbookForm` und `ProfileForm`
   geben ihre Bestätigung heute über `onDone(message)` weiter; sie bekommen zusätzlich `onMeldung`.
   *Begründung:* Ihr Anlege-Zwilling schließt sich **nicht** und kann darum sehr wohl eine alte Meldung
   überleben lassen — der Fund nennt beide Stellen. *Kosten:* Diese zwei Bausteine tragen danach zwei
   Rückkanäle (`onDone` fürs Schließen, `onMeldung` für den Platz). Das ist ausgesprochen, nicht schön.

## Akzeptanzkriterien

1. Speichern (Erfolg) → „Gespeichert." steht am Seitenplatz.
2. Danach ein **zweites** Absenden, das scheitert → der Fehler steht am Formular, und der Seitenplatz ist
   **leer**. Kein grünes „Gespeichert." neben einer roten Meldung.
3. Ein Formular öffnen räumt den Platz weiterhin (heutiges Verhalten, unverändert).
4. Anlegen und „Nichts geändert." verhalten sich unverändert.

## Belege je Kriterium

| AK | Beleg |
|---|---|
| 1 | `e2e/bestaetigung-ueberlebt.spec.ts`, Fall B-173 (bestehend) |
| 2 | neuer E2E-Fall in derselben Datei — **die rote Probe dieser Story** |
| 3 | derselbe neue Fall (er öffnet das Formular als ersten Schritt) |
| 4 | `e2e/bestaetigung-ueberlebt.spec.ts`, Fall B-151 (bestehend, prüft „Buch hinterlegt.") |

## Schätzung

- **Größe: M.** Anker: drei Seiten, ein zusammengelegtes Prop, fünf Aufrufstellen, ein E2E-Fall.
- **Migration/Vertragsbruch: nein** — reine Oberfläche.
- **Testweg:** `frontend/e2e/bestaetigung-ueberlebt.spec.ts`. **E2E und kein Komponententest:** Der Fall
  verlangt ein *erfolgreiches* Speichern gefolgt von einem *scheiternden* — beides gegen den echten
  Server, und kein Test dieses Frontends mockt `../lib/api`.

## Verlauf

- 2026-09-02 · Aufgenommen aus der **Nachschau** zu Sprint 1 (zweiter Nachtlauf). `entgangen_bei` nennt
  beide Stories, weil der Platz aus B-173 stammt und B-151 ihn auf zwei weitere Flächen gebracht hat.
- 2026-09-02 · **`ausformuliert` → `gegrillt` → `geschaetzt`** (dritter Sprint, Freigabe 1 — `art:
  Defekt`). Beide offenen Punkte entschieden; Punkt 2 ist von „billigster Schnitt" zu einer Entscheidung
  **mit Kosten** geworden. Belege je Akzeptanzkriterium benannt — das ist der Retro-Vorschlag aus Nacht 2,
  hier in der Praxis befolgt, ohne ihn als Regel zu landen.
- 2026-09-02 · **`geschaetzt` → `in-arbeit` → `abgenommen`** (Sprint 3). Belege:
  - **Reviewer:** `frontend-reviewer`, zwei Runden. Runde 1 setzte **einen Blocker** (siehe unten),
    Runde 2 steht aus bzw. ist in den Verlauf eingearbeitet.
  - **Rollengang:** maschinell — voller Playwright-Lauf, echter Chromium gegen echten Server.
  - **Zahlen:** `npx tsc -b` sauber · `npm run test` **327/327**.
  - **Commits:** `6843d42` (Bau) und der Abnahme-Commit dieses Sprints.
  - **Der Blocker war die schmerzhafteste mögliche Sorte:** Der Ist-Stand dieser Story nennt **drei**
    kleinere Ausprägungen der Lücke, darunter die Unit-Ziehharmonika. Gebaut wurden zwei; die dritte fiel
    **kommentarlos** weg — genau die stille Auslassung, gegen die dieser Sprint antritt. B-196 macht es
    im selben Sprint richtig vor: Dort steht der Ausschluss als Entscheidung 3 mit Begründung und Kosten.
    Nachgebaut: `onMeldung` reicht jetzt durch `SeriesRow → UnitPanel → UnitForm`, und `UnitForm` räumt
    zu Beginn von `submit` und `remove`.
  - **Entscheidung 2 war zu stark formuliert, und der Reviewer hat es belegt:** „Ein Prop mit `null` als
    Wert kann man nicht halb verdrahten" — ein `?`-Prop kann man aber ganz **weglassen**, und genau so
    wäre die nächste Stelle entstanden. `onMeldung` ist darum jetzt **Pflicht**, wo nichts auf seine
    Abwesenheit verzweigt (`TextbookForm`, `ProfileForm`, `UnitForm`); optional bleibt es nur bei
    `SeriesForm`, wo die Anwesenheit entscheidet, ob die Bestätigung nach oben geht oder lokal bleibt.
    *Preis, gezahlt statt aufgeschrieben:* **18** Aufrufstellen in den Komponententests tragen jetzt
    `onMeldung={() => {}}`.
  - **Ein unbeanspruchter A11y-Gewinn, den die Story nicht kannte** (Fund des Reviewers, SC 4.1.3): Vor
    der Änderung setzte ein zweites erfolgreiches Speichern denselben String „Gespeichert." — keine
    DOM-Änderung in der `aria-live`-Region, also **keine Ansage**. Durch das Räumen beim Absenden leert
    sich die Region zwischendurch, und die Wiederholung wird angesagt. Das ist ein zweiter Grund für
    Entscheidung 1.
  - **Bewusst NICHT geräumt** (damit die nächste Nachschau es nicht als Lücke liest): ein Filterwechsel
    tauscht die Tabelle unter einer stehenden Meldung aus — hier spricht B-173 sogar *für* das
    Stehenbleiben, denn die Meldung gehört zu einer Zeile, die gerade wegen der Filterung verschwindet.
    `NewSeries` und `PublisherAdmin` tragen eigene Banner und rühren den Platz nicht an; nach „Reihe
    löschen → Reihe anlegen" stehen beide Meldungen nebeneinander. Das ist ein sichtbarer, aber nicht
    widersprüchlicher Zustand und bleibt bis zu einer Entscheidung darüber, wie viele Meldungsorte eine
    Seite haben darf.
