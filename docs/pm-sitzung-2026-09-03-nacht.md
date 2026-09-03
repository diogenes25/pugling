---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-09-03, Auswahl-Drift]
---

# PM-Sitzung: Was nach einem Wechsel auf dem Schirm steht (Nachtlauf)

**Datum:** 2026-09-03 (UTC; lokal bereits der 04.)  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Unbeaufsichtigt Stories aus `docs/backlog/` abarbeiten, solange keine Halt-Bedingung greift.

## Auftrag & Freigaben

Auftrag des Nutzers: „starte einen nachtlauf" → das dokumentierte Verfahren aus
[docs/nachtlauf.md](nachtlauf.md), Freigaben 1–8. Kurzfassung dessen, was diese Nacht ändert:

1. Autonomes Grillen **nur** für `art: Defekt` und `art: Aufräumen`.
2. Reviewer nicht erreichbar → beschrifteter Selbst-Check, Story bleibt `in-arbeit` mit `wartet_auf`.
3. Mehrere Sprints erlaubt; Retro **schlägt nur vor**, landet nichts; Review-Funde werden sofort behoben,
   ab **mehr als fünf mit Code-/Testwirkung je Sprint** endet der ganze Lauf.
4. „Kein Befund" nur mit benanntem Prüfpunkt.
5. Jede rote Probe nennt erwartet/gemessen.
6. Browser-Rollengang zählt, wenn erreichbar; sonst benannter Ersatz.
7. `web-design-guidelines` zusätzlich erlaubt bei sichtbarem UI.
8. `geschaetzt` + `Wunsch`/`Frage` ist **baubar**; gesperrt bleibt nur das Grillen unterhalb `geschaetzt`.

## Ausgangslage (Step 1)

| | Anzahl |
| --- | --- |
| Baubar (`geschaetzt`, nicht extern blockiert) | 22 |
| Verfeinerbar (`Defekt`/`Aufräumen` unterhalb `geschaetzt`, autonom grillbar) | 29 |
| Extern blockiert (`wartet_auf`) | 2 (B-07 Azure, B-31 Gerät) |

Vorlauf derselben Sitzung (vor dem Nachtlauf-Auftrag, im Dialog): **B-19 abgenommen** — der KI-Creator
legt ganze Lehrpläne an (Commits `adfa05e`, `852f62e`). Diese Story ist damit **unbeobachtet** und gehört
in die Nachschau der ersten Retrospektive.

## Ein Befund schon beim Aufbau (wie in der Nacht davor)

Der erste Startversuch der API endete wieder mit der `InvalidOperationException` „`pugling.db` stammt aus
der alten Migrationskette". **Kein Defekt, sondern die Regel bei der Arbeit** (kein Upgrade-Pfad, siehe
[CLAUDE.md](../CLAUDE.md) → „EF-Migrationen"). Die vorhandene `pugling.db` wurde **nicht angetastet**; der
Lauf fuhr gegen `nachtlauf-2026-09-03.db`.

**Kein Browser-Rollengang möglich:** In dieser Sitzung ist keine Chrome-Anbindung erreichbar (geprüft über
die Werkzeugliste). Freigabe 6 greift also nicht, und es gilt der dokumentierte Ersatz — hier die stärkste
seiner Formen: eine eigene Playwright-Spec, die den Weg im echten Browser gegen einen echten Server fährt.

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel (aus dem Sitz des Vaters, in Step 6 widerlegbar):** *Wechsle ich Fach, Reihe oder Filter,
gehört alles, was ich anklicken oder ablesen kann, zum neuen Zustand — nichts stammt mehr aus der vorigen
Abfrage.*

**Umfang:** [B-167](backlog/B-167-katalogliste-zeigt-beim-filterwechsel-alte-treffer.md),
[B-195](backlog/B-195-unit-liste-bleibt-waehrend-des-reihenwechsels-waehlbar.md),
[B-164](backlog/B-164-useasync-paart-frischen-zustand-mit-alten-daten.md) (angehalten, siehe unten).

**Draußen geblieben, mit Namen:** B-194 (ungerenderter Fehlerzustand — andere Klasse, `ReferenceSelect`),
B-185 (irreführender Hinweistext), und der Rückmeldungs-Faden B-134/B-176/B-187/B-200/B-201. Sie dienen
dem Ziel nicht.

**Verfeinerung, getrennt gezählt:** B-167 kam als `idee` **mit `unverifiziert: true`** herein und musste
erst reproduziert werden; B-195 und B-164 standen auf `ausformuliert`. Drei Stories von der Idee bis zur
Schätzung zu bringen war der größere Teil dieses Sprints, das Bauen der kleinere.

## Iteration 1 — umgesetzt

Kein Backend-Anteil (reines Frontend, keine Server-Antwort ändert sich).

- **B-167** — `PlanPositions.tsx`, Baustein `AddPosition`: `filterKey` über die normalisierten
  Suchkriterien, `seitenSchluessel`/`zeilenVeraltet`, daran Trefferzahl, `TruncationHint`, Radios und
  Absende-Knopf; Auswahl-Rücknahme über `auswahlNachFilterwechsel`; Fehlerkasten mit „Erneut suchen".
- **B-195** — `unitOptions` bekommt `seriesId` als **Pflicht**-Parameter und filtert; Aufrufer in
  `ChildMaterialSection` nachgezogen, dazu Ladesignal und der schweigende Leer-Satz.
- **Neu:** `e2e/planbau-ladefenster.spec.ts` (4 Fälle), `src/lib/referenceOptions.test.ts` (4 Fälle).

**Verifikation:** `tsc -b` sauber · `vitest run` **357/357** · `playwright test` **52/52**.

## Der teuerste Umweg der Nacht: eine autonome Entscheidung auf falscher Prämisse

Er gehört ins Protokoll, weil er die Nacht rund eine Stunde gekostet hat und weil das Verfahren ihn
gefangen hat, nicht ich.

B-164 wurde autonom gegrillt (Freigabe 1, `art: Defekt`). Entscheidung 1 wählte „die Heilung liegt in
`useAsync`, als Feld `stale`". Es wurde **gebaut**, mit drei Tests und zwei gemessenen roten Proben
(`stale = false` → 2 Fälle fallen; `stale = loading` → **2**, erwartet war 1 — auch der Fehlerfall trennt,
weil `loading` nach dem Fehlschlag zurückfällt).

Beim Suchen des ersten Abnehmers zeigte sich: `VaterWizard.tsx:183-201` trägt für **genau diese Frage**
seit B-169 ein entschiedenes, getestetes und begründetes Muster — und B-195 hatte in seinem offenen Punkt
darauf hingewiesen. Der Umbau wurde vollständig zurückgenommen; es steht kein Code aus B-164 im Baum.

**Was daraus folgt und was nicht:** Die Regel „`Defekt` darf autonom gegrillt werden" hat hier **nicht**
versagt — sie sagt, dass „richtig herstellen" keine Produktentscheidung ist, und das stimmt. Versagt hat
die *Recherche* vor der Entscheidung: Der Ist-Stand nannte drei Aufrufstellen und `useAsync`, aber nicht
die bereits gebaute Antwort im Nachbarbildschirm. Das ist dieselbe Klasse wie die Retro-Regel
„Verifiziere die Prämisse, bevor du etwas darauf landest" — nur eine Stufe früher, beim Grillen statt bei
der Retro.

## Runde 1 — Rollengang / Abnahme (Step 6)

| Rolle | Ergebnis |
| --- | --- |
| **Vater (Supervisor)** | **Abgenommen.** Sein Weg ist der geänderte, und er wird von vier Playwright-Fällen im echten Browser gefahren: Filter umstellen → keine Zeile des vorigen Filters wählbar, keine Trefferzahl von vorhin; Suche scheitert → alte Zeilen bleiben gesperrt und der Grund steht da; Filterwechsel → Auswahl verworfen, mit Hinweis; Leerschlag → dieselbe Suche, Auswahl bleibt. |
| **Creator** | **Regressionszeuge, kein zweites Urteil.** Sein Weg ist vom Diff nicht berührt (`AddPosition` ist Supervisor-Fläche, `unitOptions` hat genau einen Aufrufer am Kind-Lehrbuch). Gemessen: seine Specs (`creator-lehrwerk-weg`, `uebungstypen`, `lehrwerke`, `einkleidung`) sind Teil der grünen 52. |
| **Sohn (Student)** | **Regressionszeuge.** Kein Pfad der Arcade liegt im Diff; `full-flow`, `uebung-abbruch`, `shop-verlauf` grün. |

**Kein sinnliches Urteil offen** — dieser Sprint ändert kein Gefühl, keinen Klang, keine Animation.

## Retrospektive (Sprint 1)

**Nachschau:** vier abgenommene Stories des **vorigen** Sprints geprüft (B-50, B-206, B-203, B-204), je mit
benanntem Prüfpunkt; `nachgeschaut: 2026-09-03` auf allen vieren gesetzt — auch dort, wo nichts gefunden
wurde. **Vier Befunde**, als eigene Stories angelegt:

| Neu | Befund | `entgangen_bei` |
| --- | --- | --- |
| [B-209](backlog/B-209-savednotice-raeumt-nie.md) | `SavedNotice` räumt nie — „Gespeichert" reist durch die ganze Arcade | B-50 |
| [B-210](backlog/B-210-dialog-haelt-zwei-wahrheiten-fuer-die-config.md) | **P1:** „Beschreibung speichern" dreht den gerade gespeicherten Inhalt lautlos zurück | — (älter) |
| [B-211](backlog/B-211-interestchips-zusicherung-kann-nicht-fallen.md) | Zusicherung prüft den Ausgangszustand, rote Probe gemessen | B-206 |
| [B-212](backlog/B-212-fuenfter-meldungsplatz-in-lehrwerke.md) | Rot und grün gleichzeitig — der fünfte Meldungsplatz | — (älter) |

Zwei davon sind **älter als der Diff, der sie für erledigt erklärt hat** — das ist der eigentliche Befund
der Nachschau: nicht dass etwas kaputtging, sondern dass ein Abnahme-Kommentar mehr behauptete, als die
Datei hielt.

### Was die eigenen Tore durchgelassen haben

Der Reviewer fand **neun** Punkte mit Code-/Testwirkung an einem Diff von rund 130 Zeilen. Zwei davon
hätte kein Tor je gefunden, und beide gehören zusammen:

- **Der Fehlerzweig war übertragen, seine Anzeige nicht** — mein eigener E2E-Fall fuhr in genau diesen
  Zustand hinein und prüfte nur die Sperre, nicht was dort steht.
- **Das einzige Bedienelement mit Schreibwirkung trug die Sperre nicht.**

Beide sind dieselbe Klasse: *Ich habe ein Muster übertragen und dabei geprüft, was ich übertragen habe —
nicht, was das Vorbild sonst noch trägt.* Der Zwilling hat drei Steuerelemente und eine Fehlerfläche; ich
hatte zwei Steuerelemente und keine Fehlerfläche, und alle meine Proben waren grün.

### Der vorgeschlagene Mechanismus (Freigabe 3: **nur Vorschlag**, nicht gelandet)

> **Wer ein bestehendes Muster auf eine zweite Fläche überträgt, listet zuerst *alle* Stellen auf, an
> denen das Vorbild es trägt — und begründet je Stelle, ob sie mitkommt oder warum nicht.**

Ort, falls angenommen: `frontend/CLAUDE.md`, direkt bei der schon dort stehenden Regel „Die andere Hälfte
derselben Falle". **Kosten:** eine resident geladene Zeile mehr, die nach der Budget-Regel im selben
Durchgang bezahlt werden müsste. **Warum kein Tor:** „ist ein Muster vollständig übertragen" ist über den
Syntaxbaum nicht entscheidbar — hier ist Prosa die ehrliche Antwort, kein Ausweichen.

**Der Lauf landet ihn nicht**, weil Freigabe 3 das für jeden Sprint der Nacht ausschließt. Er liegt zur
Entscheidung vor.

## Der Lauf endet hier — an einer Halt-Bedingung, nicht an einer leeren Liste

**Zähler dieses Sprints: 9 Review-Funde mit Code- oder Testwirkung. Die Grenze der Freigabe 3 ist 5.**

Gezählt (jeder ändert Code oder Tests, nicht nur einen Kommentar): fehlende Fehlerfläche · Absende-Knopf
ohne Sperre · roher Leer-Satz plus fehlendes Ladesignal in `ChildMaterialSection` · zwei Kriterienlisten ·
E2E-Fall 4 maß den Ausgangszustand · „Keine Treffer" im Ladefenster · `aria-describedby`/Ansage-Häufigkeit ·
`folge.selected` verworfen · `as`-Cast in der Test-Fabrik. **Nicht** gezählt: zwei Doku-Nits und eine
notierte Kopplung ohne Handlungsbedarf.

Alle neun sind **behoben und verifiziert**, nicht bloß gemeldet — das verlangt dieselbe Freigabe. Danach
endet der Lauf: Neun Funde in 130 Zeilen sind keine normale Korrekturschleife mehr, sondern das Zeichen,
auf das die Grenze zielt. Ein zweiter Sprint hätte dieselbe Sorgfaltslücke mitgenommen.

**Was das nicht heißt:** Der Sprint ist vollständig, grün und abgenommen. Die Grenze misst die *Dichte der
Nacharbeit*, nicht die Qualität des Ergebnisses.

## Offene Punkte für den Morgen — die Entscheidungen, die ich gebraucht hätte

1. **B-164: Hook oder Aufrufer?** Soll `useAsync` eine querschnittliche „veraltet"-Aussage tragen (73
   Aufrufstellen könnten sie lesen), oder bleibt es beim Muster am Aufrufer (B-169), das an jeder Stelle
   einzeln steht? Die Gegenüberstellung mit Kosten steht in der Story unter „Angehalten". Der Lauf hat das
   **nicht** entschieden: es gibt zwei gebaute, begründete Wege — das ist Architektur, nicht „richtig
   herstellen".
2. **Der Retro-Vorschlag oben** — landen oder verwerfen.
3. **[B-210](backlog/B-210-dialog-haelt-zwei-wahrheiten-fuer-die-config.md) ist P1** und vernichtet
   Creator-Arbeit ohne jede Rückmeldung. Wenn morgen etwas gebaut wird, dann das.

## Offene Roadmap

Die dauerhafte Liste ist [docs/backlog/](backlog/README.md) — **65 offen, 134 abgenommen**. Zur Ordnung
für die nächste Runde: der Rückmeldungs-Faden (B-134/B-176/B-187/B-200/B-201, alle `Defekt`, alle
`ausformuliert`) ist ein fertig geschnittener nächster Sprint; die vier neuen Nachschau-Stories sind es
ebenfalls, mit B-210 an der Spitze.

## Konkreter Änderungsstand (für Review)

| Datei | Was |
| --- | --- |
| `frontend/src/vater/PlanPositions.tsx` | B-167: Schlüsselvergleich, Sperren, Fehlerfläche, Auswahl-Rücknahme |
| `frontend/src/vater/ChildMaterialSection.tsx` | B-195: Elternfilter, Ladesignal, schweigender Leer-Satz |
| `frontend/src/lib/referenceOptions.ts` | B-195: `seriesId` als Pflicht-Parameter |
| `frontend/src/vater/wizardSearch.ts` | Doku: die Regel dient jetzt zwei Bildschirmen |
| `frontend/e2e/planbau-ladefenster.spec.ts` | **neu**, 4 Fälle (zugleich der Rollengang) |
| `frontend/src/lib/referenceOptions.test.ts` | **neu**, 4 Fälle |
| `docs/backlog/B-209…B-212` | **neu**, aus der Nachschau |
