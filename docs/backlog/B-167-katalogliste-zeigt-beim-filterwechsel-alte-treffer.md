---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [alte Treffer nach Filterwechsel, Trefferzahl passt nicht zum Filter]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer beim Review von B-163 (docs/backlog/B-163-art-und-typ-tragen-dieselben-woerter.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-167 · Katalogliste im Planbau zeigt beim Filterwechsel die alten Treffer weiter

**Die Behauptung war ungeprüft; jetzt ist sie reproduziert — und sie war zu klein.** Es sind vier
Symptome, nicht zwei, und eines davon ist **schreibend**.

## User Story

Als **Vater** möchte ich nach einem Filterwechsel nur Übungen angeboten bekommen, die zu meinem
eingestellten Filter gehören, damit ich keine Position aus einer Übung baue, die ich gar nicht mehr sehe.

## Ist-Stand am Code

Alles in `frontend/src/vater/PlanPositions.tsx`, Baustein `AddPosition`. Die Trefferliste hängt an
`useAsync` mit **sieben** Filter-Abhängigkeiten (`:411-412`); `useAsync` behält seine `data` über einen
Dep-Wechsel (`useAsync.ts:44-58` — kein `setData(null)`, das ist begründet). Daraus folgt für das
Ladefenster nach jedem Filterwechsel:

1. **Die Trefferzahl ist falsch.** `:456` schreibt `({results.length} Treffer)` unbedingt — die Zahl gehört
   zum vorigen Filter.
2. **Kein Ladesignal.** `:457` prüft `exercises.loading && exercises.data === null`; nach der ersten
   Ladung ist `data` nie wieder `null`, der Platzhalter erscheint also nie mehr.
3. **Die alten Zeilen sind auswählbar.** `:460-484` rendert die Radios ungebremst; ein Klick übernimmt
   über `onChange` die Lern-Standards dieser Übung ins Formular (`:472-481`).
4. **Und das ist der schreibende Teil:** `exerciseId` überlebt den Filterwechsel. Ist die gewählte Übung
   in den neuen Treffern nicht mehr enthalten, verschwindet nur ihre *Markierung* — der Zustand bleibt,
   und `:511` hält den Knopf mit `disabled={action.busy || exerciseId === ""}` **aktiv**. `add()`
   (`:428-436`) legt dann eine Position auf eine Übung an, die der Vater auf dem Schirm nicht mehr hat.

**Kein Duplikat von [B-116](B-116-blaettern-ohne-rueckmeldung.md)** (offener Punkt 2, beantwortet): Dort
ging es um den **Pager** einer geblätterten Liste. Diese Stelle hat keinen Pager, sondern eine
Filterleiste und einen `TruncationHint` (`:502`) — dieselbe Familie („ein Zustand, der *aktuell* und *von
vorhin* nicht trennt"), andere Datei, andere Bedienelemente.

**Warum die Lösung des Katalogs hier nicht trägt:** `CategorySection` filtert die Zeilen über
`c.subjectId === subjectId` (`CatalogAdmin.tsx:164`) — dort trägt *die Zeile* ihren Schlüssel selbst. Hier
sind die Treffer die **Antwort auf eine beliebige Filterkombination**; eine `ExerciseSummary` trägt nicht,
ob sie zu „Klasse 7 + Freitext *avoir*" gehört. Der Vergleich muss also an der **Abfrage** hängen, nicht
an der Zeile — und genau das ist der `seitenSchluessel` aus dem nächsten Abschnitt.

## Die echte Lücke — und die Antwort steht schon im Haus

Nicht die Liste, sondern das fehlende Signal: `loading` beantwortet „läuft eine Abfrage", nicht „läuft
eine **andere**".

**Diese Frage ist im Repo bereits entschieden, getestet und begründet** —
`VaterWizard.tsx:183-201` und `:673-689` ([B-169](B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md)):
ein `seitenSchluessel` über die Suchkriterien, daraus ein `zeilenVeraltet`, und daran hängen Trefferzahl,
Kästchen und Knopf. Der Kommentar dort nennt sogar die Begründung, die auch hier gilt: verglichen werden
**Schlüssel**, nicht Ladezustände, weil `loading` „Kriterien geändert" und „dieselben Kriterien neu
geladen" zusammenzöge — und weil der Ladezustand den **Fehlerzweig** nicht deckt (scheitert die neue
Abfrage, fällt `loading` zurück, während die alten Zeilen stehen bleiben).

`AddPosition` und der Assistenten-Schritt „Übungen" sind Zwillinge: dieselbe Filterleiste, dieselben
`ExerciseSummary`-Zeilen, dieselbe geblätterte Suche. Der eine trägt die Regel seit B-169, der andere
nicht. **Diese Story ist deshalb keine Erfindung, sondern eine Übertragung.**

## Offene Punkte

1. ~~Tritt es überhaupt auf?~~ → **ja**, am Code reproduziert (vier Symptome oben, mit Zeilen).
2. ~~Ist es dieselbe Stelle wie B-116?~~ → **nein**, Begründung oben. Kein Duplikat, kein `verworfen`.
3. ~~Ladesignal oder Zeilen leeren?~~ → Entscheidung 2 und 3.

## Entscheidungen

1. **Diese Story überträgt das Muster aus B-169 (`seitenSchluessel`/`zeilenVeraltet`), sie erfindet
   nichts.**
   Begründung: Die Regel ist für die Zwillingsfläche bereits entschieden, gebaut und getestet, samt
   der Begründung für den Schlüsselvergleich und für den Fehlerzweig. Eine zweite Fassung derselben Frage
   wäre die Doppelung, die dieses Repo teuer bezahlt hat — und die veraltete gewinnt.
   **Kosten:** Die Regel steht danach an **zwei** Aufrufstellen ausformuliert statt an einer zentralen.
   Das ist der bewusst in Kauf genommene Preis dafür, `useAsync` nicht um eine querschnittliche Aussage zu
   erweitern, die 111 Aufrufstellen berührt; die Frage, ob es die trotzdem geben soll, bleibt bei
   [B-164](B-164-useasync-paart-frischen-zustand-mit-alten-daten.md) und beim Menschen.
   **Verworfene Alternative:** ein Feld `stale` in `useAsync` (Dep-Signatur-Vergleich). Es wurde in diesem
   Lauf gebaut und samt Tests **wieder zurückgenommen**, als das bestehende Muster gefunden war — die
   Begründung steht in B-164.
2. **Die Zahlen setzen aus, die Zeilen bleiben.** Trefferzahl (`:456`) und `TruncationHint` (`:502`)
   verschwinden, solange die Daten veraltet sind; die Zeilen bleiben stehen.
   Begründung: Die Zahl ist die **falsche Aussage** — sie behauptet etwas über den eingestellten Filter.
   Die Zeilen sind bloß *alt*; sie zu leeren wäre das Flackern, das die heutige Bauform bewusst vermeidet.
   **Kosten:** Ein kurzer Moment ohne Trefferzahl; der Vater sieht eine Liste, deren Umfang gerade nicht
   beziffert wird. Das ist ehrlicher als eine Zahl, die nicht stimmt, aber es ist ein Zustand mehr.
3. **Die Zeilen bleiben sichtbar, sind aber nicht wählbar**, und daneben steht, warum.
   Begründung: B-164s Entscheidung 4 („sperren statt ausblenden"), hier zusätzlich zwingend, weil ein
   Klick nicht nur markiert, sondern die Lern-Standards der falschen Übung ins Formular schreibt.
   **Kosten:** Ein `disabled` an einem Radio bleibt im Fokusbaum, ohne seinen Grund zu nennen — deshalb
   muss der Grund als Text daneben stehen, sonst wiederholt sich [B-160](B-160-gesperrter-knopf-nennt-den-grund-nie.md).
   Das ist eine zusätzliche Zeile Oberfläche, die nur in einem kurzen Fenster erscheint.
4. **Die überlebende Auswahl fällt beim Kriterien-Wechsel — über `auswahlNachFilterwechsel`, die Funktion,
   die der Assistent dafür schon hat (B-161).**
   Begründung: Symptom 4 ist das einzige mit Schreibwirkung, und es gehört in diese Story, nicht in eine
   eigene: dieselbe Bedienhandlung, dieselbe Fläche, dieselbe Ursache. Die Funktion ist auf eine *Liste*
   von Ids geschnitten; eine Einzelauswahl ist eine Liste mit höchstens einem Element, also passt sie ohne
   Umbau. Ihr Hinweistext („Auswahl zurückgesetzt (1 Übung), weil sich die Suche geändert hat.") löst
   zugleich [B-116](B-116-blaettern-ohne-rueckmeldung.md)s Regel ein: stumm zu leeren liest sich als Fehler.
   **Erwogen und verworfen:** erst löschen, wenn die *frischen* Treffer die Auswahl nicht mehr enthalten.
   Das wäre schonender, aber es ist eine **zweite, feinere Regel** für dieselbe Frage — und der Assistent
   beantwortet sie seit B-161 anders. Zwei Bildschirme, die eine Auswahl unterschiedlich lange halten,
   sind schlechter als einer, der sie einen Tick zu früh verwirft.
   **Kosten:** Der Vater verliert seine Auswahl auch dann, wenn die gewählte Übung im neuen Filter noch
   enthalten wäre — etwa beim Tippen im Freitextfeld. Gedeckelt ist das dadurch, dass der Schlüssel über
   die **normalisierten** Kriterien gebildet wird: ein Leerschlag am Ende verwirft nichts.

## Akzeptanzkriterien

1. Während der Abfrage zu einem geänderten Filter zeigt die Fläche **keine** Trefferzahl und **keinen**
   `TruncationHint`.
2. Die Zeilen des vorigen Filters bleiben in diesem Fenster sichtbar, sind aber **nicht wählbar**, und ein
   Text nennt den Grund.
3. Ein `reload` derselben Abfrage sperrt **nichts** — die Liste bleibt bedienbar (sonst wäre `stale` nur
   ein zweiter Name für `loading`).
4. Nach einer Änderung der Suchkriterien ist nichts ausgewählt, der Knopf „+ Position hinzufügen" ist
   gesperrt, und ein Hinweis nennt den Grund.
5. Ein Leerschlag am Ende der Freitextsuche ändert die Kriterien **nicht** und verwirft die Auswahl darum
   auch nicht.
6. Ein Test deckt jeden dieser Fälle ab; je Fall ist benannt, welche Änderung ihn rot macht.

## Schätzung

**Größe: S** — eine Datei, ein Baustein, vier kleine Stellen; das Signal selbst kommt aus B-164.

- **`migration: nein`** · **`vertragsbruch: nein`** — reines Frontend.
- **`wo: frontend`** — `frontend/CLAUDE.md`, vor der Abnahme `frontend-reviewer`.
- **Risiken:**
  - Der Schlüssel muss über die **normalisierten** Kriterien laufen, sonst verwirft jeder Tastendruck die
    Auswahl. Dagegen steht Akzeptanzkriterium 5 mit einem eigenen Fall.
  - `AddPosition` ist heute nicht exportiert und hängt am Netz; der Test braucht entweder einen Export
    oder greift auf der Ebene der reinen Funktion an (siehe Testweg).
- **Angriffsplan:** (1) `filterKey` über die normalisierten Kriterien plus `seitenSchluessel`/
  `zeilenVeraltet` nach dem Muster aus `VaterWizard`; (2) Trefferzahl und `TruncationHint` daran hängen;
  (3) Radios sperren plus Begründungstext; (4) Auswahl über `auswahlNachFilterwechsel` verwerfen;
  (5) Tests.
- **Testweg:** `frontend/src/vater/PlanPositions.test.ts` existiert und prüft heute reine Funktionen —
  die Auswahl-Regel (Entscheidung 4) wird als solche herausgezogen und dort geprüft. Für die
  Oberflächen-Fälle (AK 1–3) ein Baustein-Test nach dem Muster von `CatalogAdmin.test.tsx`, wofür
  `AddPosition` exportiert wird. Kein Backend-Test, kein `/smoke-test`.

## Verlauf

- 2026-08-13 · Aufgenommen. Vom `frontend-reviewer` beim Review von B-163 als Umfeld-Beobachtung
  gemeldet, von ihm selbst als nicht vermessen gekennzeichnet. Bleibt `unverifiziert`, bis es reproduziert
  ist.
- **2026-09-03** — beim Bauen **korrigiert**: Die erste Fassung der Entscheidungen (eine Stunde zuvor in
  derselben Sitzung getroffen) baute auf einem neuen `stale`-Feld in `useAsync` auf. Das war eine falsche
  Prämisse — `VaterWizard` trägt seit B-169 ein entschiedenes, getestetes Muster für exakt diese Frage,
  und B-195 hatte darauf sogar hingewiesen. Der bereits gebaute Hook-Umbau samt drei Tests wurde
  zurückgenommen, die Entscheidungen 1 und 4 stehen jetzt auf dem bestehenden Muster.
- **2026-09-03** — Nachtlauf: **reproduziert am Code** (`unverifiziert` entfernt), ausformuliert, autonom
  gegrillt (Freigabe 1, `art: Defekt`) und geschätzt. Die Behauptung der Ernte war **zu klein**: Zur
  falschen Trefferzahl und dem fehlenden Ladesignal kommen die auswählbaren alten Zeilen und — als
  einziges schreibendes Symptom — eine Auswahl, die den Filterwechsel überlebt und den Absende-Knopf
  aktiv hält. Offener Punkt 2 (Duplikat zu B-116?) mit Begründung verneint.

## Was der Review noch gefunden hat — und was daraus wurde

Der `frontend-reviewer` fand **sieben** Punkte mit Code- oder Testwirkung an diesem Diff, alle
eingearbeitet. Die drei, die wirklich weh taten:

1. **Der Fehlerzweig war übertragen, seine Anzeige nicht.** Scheitert die Suche, bleibt `zeilenVeraltet`
   *dauerhaft* wahr (der Effekt hängt an `exercises.data`, und das ändert sich nicht mehr) — dort stand
   für immer „(Suche läuft…)" neben einer Liste, die nie wieder wählbar wird, ohne Grund und ohne Ausweg.
   Der eigene E2E-Fall 2 fuhr genau in diesen Zustand und prüfte nur die Sperre, nicht was dort steht.
   Jetzt: Fehlerkasten mit `role="alert"` und „Erneut suchen" (`reload()` setzt `data` neu und löst die
   Sperre) — derselbe Aufbau wie im Zwilling.
2. **Das einzige Element mit Schreibwirkung trug die Sperre nicht.** Der Absende-Knopf hing allein an
   `exerciseId === ""` — also daran, dass der Effekt schon gelaufen ist, und das ist einen Tick später als
   der Render. Genau die Klasse Fehler, um die es in dieser Story geht.
3. **Zwei handgepflegte Kriterienlisten**, die übereinstimmen mussten (Abhängigkeiten der Abfrage und der
   Schlüssel). Ein achtes Filterfeld wäre an einer von beiden vergessen worden, und die Sperre ginge dann
   nicht mehr auf oder gar nicht mehr an. Jetzt **eine** Liste, zwei Verwendungen — mit dem Nebeneffekt,
   dass ein Leerschlag am Ende der Suche gar keine zweite Abfrage mehr auslöst.

Dazu: „Keine Treffer" erschien im Ladefenster neben „(Suche läuft…)", `folge.selected` wurde verworfen
statt ausgewertet, die Radiogruppe bekam `aria-describedby` auf den Grund, und E2E-Fall 4 maß den
Ausgangszustand statt der Wirkung (er zählt jetzt Anfragen und trägt eine Gegenprobe).

## Rote Proben (erwartet / gemessen)

| Probe | erwartet | gemessen |
| --- | --- | --- |
| Radio-Sperre entfernt | 2 | **2** |
| Trefferzahl wieder unbedingt | 1 | **1** |
| Auswahl-Rücknahme entfernt | 1 | **1** |
| Normalisierung (`trim`) entfernt | 1 | **1** |

- **2026-09-03** — **abgenommen** (Nachtlauf, Sprint 1). Belege: `tsc -b` sauber, `vitest run`
  **357/357** grün, `playwright test` **52/52** grün. `frontend-reviewer` gelaufen, **kein Blocker**;
  seine Funde sind eingearbeitet (siehe unten). **Rollengang:** die vier Fälle in
  `e2e/planbau-ladefenster.spec.ts` fahren die echte Oberfläche im echten Browser gegen einen echten
  Server — das ist der Rollengang, und er wiederholt sich, statt einmal stattzufinden. Kein
  Browser-Handgang möglich (keine Chrome-Anbindung in dieser Sitzung), damit greift der dokumentierte
  Ersatz. Commit `a7e3da6`; Protokoll `docs/pm-sitzung-2026-09-03-nacht.md`.
