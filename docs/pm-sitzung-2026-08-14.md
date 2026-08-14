---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-08-14, die Funde von gestern schliessen]
---

# PM-Sitzung 2026-08-14 — Nachtlauf: die Funde der Nachschau schließen

## Auftrag und Freigaben

Unbeaufsichtigter Backlog-Lauf nach `pm-loop` und [docs/backlog/README.md](backlog/README.md).
**Alle acht Freigaben** aus [docs/nachtlauf.md](nachtlauf.md) gelten, wie dort dokumentiert, vom Nutzer für
diesen Lauf erteilt (nicht aus einer früheren Sitzung fortgeschrieben).

**Zwei Entscheidungen des Nutzers zum Umfang**, vor dem Start eingeholt:

1. **Roter Faden: die Funde der Nachschau vom 2026-08-13 schließen.** Begründung des Nutzers folgend meiner
   Empfehlung: Die Ist-Stände dieser Stories sind gestern am Code geprüft worden und verrotten nicht über
   Nacht — bei den älteren `ausformuliert`-Stories wäre der erste Schritt eine Neuprüfung.
2. **B-170 bekommt nur das Ventil, kein Schema.** Die Migrationskette wird in diesem Lauf **nicht** gefaltet.
   Begründung: Ein Fehlgriff an der Kette ist unbeaufsichtigt der teuerste mögliche, `SchemaGuardTests`
   erzwingt Länge 1, und der Snapshot-Diff ist die eigentliche Abnahme — die kann nachts niemand beurteilen.
   Ein Ventil behebt den unreparierbaren Zustand bereits; die saubere Lösung mit `CreatedByAdultId` bleibt
   eine Entscheidung am Tag.

## Ausgangslage (gemessen, nicht geschätzt)

- `geschaetzt`: **28** Stories, fast alle `Wunsch` (seit Freigabe 8 baubar), überwiegend M/L.
  Zwei davon blockiert: B-7 (`wartet_auf` einen Handgriff an der Azure-Instanz), B-31 (`wartet_auf` ein
  echtes Handy).
- `ausformuliert`: **16** Stories, **ausnahmslos** `Defekt` oder `Aufräumen` — also nach Freigabe 1
  vollständig autonom grillbar. Darunter vier P1-Defekte.
- `in-arbeit`: **0** — kein Rest aus einem früheren Lauf.
- Nachschau-Stand beim Start: **103 von 103**, Arbeitsvorrat leer
  ([Protokoll von gestern](pm-sitzung-2026-08-13.md)).

## Sprint-Plan

Drei Sprints, jeder mit einem Ziel aus einem Rollen-Sitz — nicht mit einer Id-Liste, damit Step 6 es
widerlegen kann.

| Sprint | Ziel (aus dem Sitz) | Stories |
|---|---|---|
| 1 | **Vater:** „Ich bekomme im Assistenten keine Übung in meinen Plan, die der gezeigte Filter ausschließt — und wenn der Bildschirm noch lädt, kann ich nichts anklicken, was gleich verschwindet." | B-169, B-171 |
| 2 | **Creator:** „Ein Tippfehler in einer Art ist wieder wegräumbar, und dass ein Fach seinem Anleger gehört, hält ein Test, der beim Gegenteil rot wird." | B-168, B-170 |
| 3 | **Entwickler:** „Ein rotes Test-Tor sagt mir, *was* schiefging — ein gesperrtes Dateihandle und ein zerrissener Inhalt sind nicht dieselbe Meldung." | B-165 |

**Warum B-171 in Sprint 1 und nicht verteilt:** Sie trägt zwei Zusicherungen, eine davon in
`assistent.spec.ts` — derselben Datei und Fläche, die Sprint 1 ohnehin anfasst. Eine Story halb zu bauen
wäre schlimmer als die kleine Unschärfe, dass ihre zweite Hälfte im Backend liegt (zwei Zeilen).

**Warum B-174 nicht dabei ist:** `unverifiziert: true` — sie ist eine Behauptung, die erst reproduziert
werden muss. Das ist Verfeinerung, nicht Bauen, und gehört nicht in einen Sprint, dessen Ziel eine Fläche
verspricht.

## Fehlerzähler

Freigabe 3: je Sprint zählt jeder von einem Review (Agent, Selbst-Check, roter Test) gefundene Fehler, dessen
Behebung **Code oder Tests** ändert. Über 5 endet der **gesamte** Lauf.

| Sprint | Zähler | Stand |
|---|---|---|
| 1 | **2 / 5** | abgeschlossen |

## Sprint 1 — Ziel erreicht

**Ziel war:** „Ich bekomme im Assistenten keine Übung in meinen Plan, die der gezeigte Filter ausschließt —
und wenn der Bildschirm noch lädt, kann ich nichts anklicken, was gleich verschwindet."

**Erreicht, live belegt.** Am Assistenten mit verzögerter Suche gemessen: im Ladefenster stehen die sechs
alten Zeilen noch da, **alle sechs gesperrt**, und „Alle wählen" ebenfalls; nach der Antwort ist die neue
Zeile bedienbar. Der Zusatz „was gleich verschwindet" war die Hälfte, die Entscheidung 1 überhaupt nötig
machte — eine Ref hätte die frischen Zeilen gesperrt gelassen.

Beide Stories `abgenommen`: [B-169](backlog/B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md),
[B-171](backlog/B-171-zwei-zusicherungen-pruefen-den-ausgangszustand.md).

### Was die Reviews gefunden haben (der Fehlerzähler im Einzelnen)

| # | Fund | ändert | zählt? |
|---|---|---|---|
| 1 | `frontend-reviewer`: „Alle wählen" ist im **Fehlerzweig** ungesichert — derselbe P1-Schaden über die Nachbartür | Code | **ja** |
| 2 | `frontend-reviewer`: die neue Spec **erbt** das Kind statt es zu wählen, und Klasse/Schulart filtern serverseitig | Test | **ja** |
| 3 | `pugling-reviewer`: der Kommentar behauptet mehr als er hält (`GetInt32()` auf `null` ist auch rot) | Kommentar | nein |
| 4 | `pugling-reviewer`: Kommentar zu lang, Umbau-Erzählung | Kommentar | nein |
| 5 | `pugling-reviewer`: B-171s Frontmatter stand auf `ausformuliert`, obwohl der Rumpf die Schätzung trug | Story | nein |
| 6 | `frontend-reviewer`: `getByLabel` ohne `{ exact: true }` | Test (Konvention, kein Defekt) | nein |
| — | voller E2E-Lauf: `bilder.spec.ts` rot | außerhalb des Diffs | nein |

Der Schnitt an „was der Fix anfasst" hat sich hier **bewährt**: Von sieben Beobachtungen sind zwei echte
Defekte, und der Zähler nennt genau die zwei. Hätte er an einer Einschätzung gehangen, wie schwer ein Fund
wiegt, wäre er bei fünf oder sechs gestanden und der Lauf beinahe beendet.

### Retrospektive Sprint 1

**Nachschau (Pflichthandlung, zuerst):** Index beim Start des Sprints **103 von 103** — die Arbeit des
vorigen Durchgangs (B-163, abgenommen am 2026-08-13) war unter den X, geprüft am 2026-08-13 mit benanntem
Prüfpunkt („der neue Wächter deckt die geseedeten Arten, nicht selbst angelegte; `POST …/categories` ist
ungegated, die Kollision ist von der anderen Seite zurückholbar — aufgefangen durch das Art-Etikett").
**Die Nachschau-Pflicht ist damit erfüllt, ohne dass ein neuer Blick nötig war.** Nach der Abnahme von
Sprint 1 steht der Index bei **103 von 105**: B-169 und B-171 sind die Arbeit, die **Sprint 2s** Retro
ansehen muss. Das ist die Kette, nicht eine Lücke.

**Was gut lief.** Die Grill-Runde hat die eigene Empfehlung der Story widerlegt, *bevor* gebaut wurde
(Ref → State). Das ist der Wert des Grillens an einer Story, die man selbst geschrieben hat: der Vorschlag
im Angriffsplan war plausibel und falsch.

**Was schiefging, und es ist derselbe Fehler zweimal.** Ich habe in *beiden* Stories eine Behauptung in
einen Kommentar geschrieben, die ich nicht geprüft hatte: „`exercises.loading` deckt dasselbe Fenster" (falsch
im Fehlerzweig) und „`GetInt32()` würde werfen statt zu vergleichen" (richtig, aber der Fall wäre auch so rot).
Beide Male hat ein Reviewer es gefunden, kein Tor. **Ein Kommentar, der eine Begründung behauptet, ist eine
Zusicherung ohne Test** — und genau die Klasse, für die diese Story-Familie überhaupt existiert.

**Vorschlag für einen Mechanismus** — nach Freigabe 3 nur vorgeschlagen, **nicht gelandet**:
Wo ein Kommentar begründet, *warum* ein Gate nur an einer Stelle sitzt, gehört die andere Stelle in dieselbe
rote Probe. Konkret: „das Nachbar-Steuerelement ist schon abgedeckt" ist eine prüfbare Aussage, und sie war
hier in einem Satz widerlegt. Ob das eine Zeile im Reviewer-Auftrag wert ist oder in die
`frontend/CLAUDE.md`, entscheidet der Nutzer — **erst messen**, wie oft ein solcher Satz vorkommt.

**Eine Beobachtung zum Verfahren selbst.** `bilder.spec.ts` fiel im vollen Lauf, und die *erhaltene* Meldung
hat B-153 von „flackert" auf eine benannte Ursache gebracht (`strict mode violation`, der Bildbezug trifft
zwei Elemente). Der Grund, dass es diesmal gelang: Ich habe den Einzellauf **nach** dem Sichern der Meldung
gefahren, nicht davor — genau der Fehler, den B-153 sich selbst am 2026-08-11 vorwarf. Der Lauf hat also eine
fremde Story mit einem Beleg bezahlt, ohne sie zu bauen.

## Verlauf des Laufs

- **2026-08-14** — Lauf gestartet. Freigaben erteilt, Umfang entschieden, Sprint-Plan steht.
- **2026-08-14** — **Sprint 1 durch.** B-169 und B-171 von `ausformuliert` bis `abgenommen`, Fehlerzähler
  **2 von 5**. Zwei Reviewer-Funde waren echte Defekte und wurden behoben, nicht nur gemeldet. Vier
  Nebenfunde als eigene Stories bzw. Korrekturen abgelegt:
  [B-177](backlog/B-177-seitenschluessel-haengt-an-einer-batching-annahme.md) neu,
  [B-162](backlog/B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) mit korrigiertem Ist-Stand und
  P3 → P2, [B-153](backlog/B-153-bilder-spec-flackert-im-vollen-lauf.md) mit belegter Ursache und
  `idee → ausformuliert`, P3 → P2.
