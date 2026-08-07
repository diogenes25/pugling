---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/training, rolle/supervisor]
aliases: [ItemCount-Hilfetext, Fund 2]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/backlog-vokabellernen.md#fund-2--das-formular-erklärt-itemcount-falsch-herum
nachgeschaut: "2026-08-07"
---

# B-02 · Der Hilfetext erklärt `ItemCount` falsch herum

Kein fehlender Hinweis, sondern ein **irreführender**: Der Vater trifft eine Einstellung, deren Wirkung
ihm das Formular falsch erklärt — und wird zu genau der Zahl ermutigt, die den Großteil seiner Vokabeln
dauerhaft stilllegt.

## User Story

Als Vater möchte ich am Feld „Inhalte" lesen, was die Zahl wirklich tut, damit ich nicht ungewollt 80 von
100 Vokabeln aus dem Lernbetrieb nehme.

## Ist-Stand am Code · Entscheidungen

→ Grill-Protokoll vom 2026-07-30, Abschnitt **„Fund 2"**:
[backlog-vokabellernen.md](../backlog-vokabellernen.md#fund-2--das-formular-erklärt-itemcount-falsch-herum).

Kern, belegt: `ItemCount` **schneidet ab** statt zu portionieren
(`PositionPlayService.DueItemIndicesAsync`, `Enumerable.Range(0, poolSize)`); der Hilfetext behauptet mit
„Inhalte je Durchgang" und „Leer = alle Inhalte der Übung" ein rotierendes Tageskontingent
(`lib/fieldHelp.ts:32-36`, gesetzt in `vater/PlanPositions.tsx:167`) und ermutigt im Schlusssatz aktiv zur
kleinen Zahl. `defaultItemCount` wiederholt den Fehler (`lib/fieldHelp.ts:101-105`).

## Akzeptanzkriterien

1. Beide Texte sagen, was der Server tut: die Übung wird **dauerhaft auf die ersten N Inhalte begrenzt**,
   die übrigen werden nie abgefragt.
2. Auch der **Titel** trägt die Korrektur — „je Durchgang" *ist* die irreführende Formulierung.
3. Der Schlusssatz ermutigt nicht mehr zur kleinen Zahl.
4. `frontend/e2e/feldhilfe.spec.ts` prüft die neuen Texte.

## Schätzung

**Größe: XS** — zwei Hilfetexte und der E2E, der sie prüft.

- **Nicht** nur eine Datei: `frontend/e2e/feldhilfe.spec.ts:40` prüft Titel **und** Text
  (`/Leer = alle Inhalte der Übung/`) und zieht mit.
- **Risiko:** keins am Verhalten — reine Textänderung; kein `.cs`, also greift das Test-Tor nicht.
- **Testweg:** `npm run build` (tsc) und `npx playwright test feldhilfe`.
- **Haltbarkeit:** Wird B-04 gebaut, *stimmt* der heutige Text plötzlich und muss nur um den Tagesdeckel
  ergänzt werden — dieser Fix ist dann teilweise obsolet. Er lohnt trotzdem, weil B-04 M ist und dies XS.

## Umgesetzt

Beide Hilfetexte sagen jetzt, was der Server tut, und der Titel trägt die Korrektur mit — er war der
irreführendste Teil:

| | vorher | nachher |
| --- | --- | --- |
| `itemCount` | „Inhalte **je Durchgang**" · „…was eine Sitzung höchstens vorlegt" · „Eine kleine Zahl macht die tägliche Pflicht kurz" | „Inhalte **dauerhaft begrenzen**" · „…dauerhaft auf ihre ersten N Inhalte – die übrigen werden **nie** abgefragt, auch nicht an einem anderen Tag" · „Leer = alle Inhalte, und das ist meist richtig" |
| `defaultItemCount` | „Standard-**Menge**" · „wie viele Inhalte eine **Sitzung zeigen** soll" | „Standard-**Begrenzung**" · „auf wie viele der ersten Inhalte die Übung **beschränkt** wird" |

Der neue Text nennt außerdem, wer die Tagesmenge *wirklich* bestimmt: der Leitner-Kasten über die
Fälligkeit. Der Satz, der zur kleinen Zahl ermutigte, ist weg.

Dateien: `frontend/src/lib/fieldHelp.ts` (`itemCount`, `defaultItemCount`),
`frontend/e2e/feldhilfe.spec.ts` (erwarteter Titel + Textausschnitt, mit Begründung im Kommentar).
Der Titel ist gleichzeitig der barrierefreie Knopfname (`InfoHint.tsx:40`,
`aria-label={"Erklärung zu „" + help.title + """}`) — deshalb *musste* der E2E mitziehen.

## Verifikation

- `npm run build` (tsc -b + vite build) **grün**.
- `npx playwright test feldhilfe` — **3/3 grün** (34,7 s): Feld→Text-Zuordnung, Schließverhalten,
  Gleichlaut zwischen Assistent und Plan-Seite.
- Kein `.cs` berührt, das Test-Tor greift nicht; `pugling-reviewer` entfällt (keine Backend-Änderung).

**Commits:** `6471e1d` trug die Textänderung (von einer parallelen Sitzung mitgenommen), `d3bf81f` den
nachgezogenen E2E.

**Benannte Ausnahme:** Der `frontend-reviewer` ist **nicht** gelaufen, obwohl die Eintrittsbedingung ihn für
`wo: frontend` verlangt — in dieser Sitzung laufen Agenten nur auf ausdrückliche Ansage. Die Verifikation
stützt sich stattdessen auf Build und E2E. Das ist eine *benannte* Ausnahme, keine stille: nachholbar mit
einem Wort.

## Verlauf

- **2026-07-30** — geerntet aus dem Grill-Protokoll vom selben Tag, Stufe `geschaetzt` übernommen.
- **2026-07-30** — umgesetzt und verifiziert (Build + 3/3 E2E).
- **2026-07-30** — `abgenommen`. Dabei aufgefallen: `6471e1d` hatte nur die Textänderung mitgenommen und den
  E2E zurückgelassen — der war damit auf HEAD rot, bis `d3bf81f` ihn nachzog. Ein Beleg dafür, dass „gebaut"
  und „verifiziert" zwei verschiedene Zustände sind.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `fieldHelp.ts` weiterhin „dauerhaft begrenzen" statt
  „je Durchgang" sagt — hält (`fieldHelp.ts:36-42,115-120`, Titel „Inhalte dauerhaft begrenzen"/„Standard-
  Begrenzung"). Kein Fund.
