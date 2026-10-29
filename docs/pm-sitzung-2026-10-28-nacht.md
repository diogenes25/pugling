---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-28, Lauf H]
---

# PM-Sitzung: Nachtlauf 2026-10-28 – Lauf H

**Datum:** 2026-10-28  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die baubaren `Defekt`-Stories nach Themen in Sprints fahren, beginnend mit dem Kandidaten aus Lauf G (B-311).

Auftrag: „Starte einen Nachtlauf" – der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10.
Ausgangslage: HEAD `6b6eb34`, Baum sauber, 90 Commits ungepusht. Keine Paket-Vorgabe; Auswahl nach Thema, erster
Kandidat laut [Ende Lauf G](pm-sitzung-2026-10-27-nacht.md#ende-lauf-g) B-311.

**Ungeprüfte abgenommene Stories zu Beginn** (Index „Nachgeschaut 221 von 226"): B-305, B-315, B-316 (Lauf G, Teil 2)
und B-306, B-307 (Lauf F, Teil 4) – die Nachschau des ersten Sprints nimmt alle fünf.

## Lauf H, Teil 1 — Der Agent findet, was es gibt, und sagt, was er tut (B-311, B-312, B-279, B-264), Zähler 1

**Sprint-Ziel:** *Der Creator, der den KI-Creator mit einer gültigen Id aufruft, wird nie als Tippfehler abgewiesen,
mit einer falschen Id nie belogen – und eine allgemeine Übung ist für niemanden zugeschnitten und doch in die Interessen
eingekleidet, die er vorgibt.* Umfang: [B-311](backlog/B-311-agent-reihensuche-liest-nur-50-reihen.md) (P2),
[B-312](backlog/B-312-agent-profiles-unbekanntes-fach-meldet-kein-profil.md),
[B-279](backlog/B-279-general-use-weak-zieht-woerter-trotzdem.md),
[B-264](backlog/B-264-interessen-ohne-kind-kleiden-nicht-ein.md) – alle `Defekt` auf `idee`, unter Freigabe 1 selbst
ausformuliert, gegrillt und geschätzt (Refinement: Ist-Stand je Story am Code belegt, siehe die Abschnitte in den
Story-Dateien). Draußen: [B-313](backlog/B-313-agent-unbekannter-typ-je-teil-statt-fehlbedienung.md) (`Frage`, gesperrt)
und B-310 (Vertragsfrage, beim Nutzer).

**Entwickler-Brief:** Alles im Agenten, kein Server-Code. Quelle der Wahrheit bleibt die API: eine genannte Id wird beim
Server erfragt (`GET series/{id}`, `GET subjects/{id}`), nicht in einer geblätterten Liste gesucht; 404 → Fehlbedienung
(Exit 2) mit der Id, die Linie von B-301/B-307/B-308. Die Briefing-Regeln (B-279, B-264) sitzen im `BriefingBuilder`,
damit sie für jedes Verb gelten. Testweg: Integrationstests in `CreatorAgentTests` mit `FakeChatClient`, rote Probe vor
jedem Fix.

### Iteration

- Sechs neue Fälle gegen den Ausgangsstand: **6 failed** an der erwarteten Stelle (Reihen-Fall an der ersten Zeile,
  `AgentCommands.cs:473`). Danach `CreatorAgentTests` **75/75**.
- Vor dem Review selbst gefunden: mit Seitengröße 200 bliebe die Mutante „nur eine Seite“ grün (die Test-DB hat weniger
  als 200 Reihen) → Seitengröße 50; Mutante danach **1 failed**. Weitere Mutanten (B-264: „neutral immer“, „neu
  immer“, „Beschreibung alt“) je **1 failed**, Kontrolle **6 passed**.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | `--child X --general --interests` ungetestet; Mutante `request.ChildId is null` blieb grün | Prompt-Zusicherung im bestehenden Fall; Mutante **1 failed** | ja |
| – | `pugling-reviewer` 🟢 | Reihen-Fall prüft nicht, *welche* Unit gewählt wurde | zurückgestellt: `Console.Out` abzugreifen ist prozessweit und verträgt sich nicht mit parallelen Testklassen | nein |
| – | `pugling-reviewer` 🟢 | Wortlaut „Kein Profil für Fach …“ ungetestet | als Konsolenausgabe benannt | nein |
| – | `pugling-reviewer` 🟢 | `\n` in `AppendLine`, veralteter Kommentar, `create`-Ausgabe ohne Einkleidung, Formatierungsrauschen | alle vier behoben | nein |

**Zähler: 1.** Suite **1000/1000** (`exit=0`), `dotnet format` an den geänderten Dateien sauber. Rollengang: der Agent
hat keine Oberfläche, die Integrationstests fahren seine Verben gegen die echte API (in-process) – das ist der Weg;
Vater und Sohn Regressionszeugen (Suite grün, kein Server- oder Frontend-Pfad im Diff). **B-311, B-312, B-279, B-264
`abgenommen`**, dazu **B-318** aus der Nachschau (siehe unten).

### Retrospektive

**Nachschau:** fünf bis dahin unbeobachtete Stories, je von einem frischen Reviewer ohne Testlauf. Lauf F, Teil 4
(`d8e4a0a`, B-306/B-307, `pugling-reviewer`): jede Strategie auf `null`-Dereferenzierung nach bestandener Prüfung, jedes
Verb mit unbekannter Reihe, Mutante je Fall hergeleitet. **Entgangen:** [B-318](backlog/B-318-null-element-ohne-fall-in-drei-strategien.md)
(kein Fall für `null` in Vokabel/Übersetzung/Grammatik; im Sprint behoben, drei Mutanten je **1 failed**) und der
`--series`-ohne-Unit-Pfad, identisch mit B-311 (dort `entgangen_bei` ergänzt). `Assert.Equal(0, chat.Calls)` in drei
Fällen als Absichtserklärung beschriftet. Lauf G, Teil 2 (`43b5eef`, B-305/B-315/B-316, `frontend-reviewer`): Spielweg
auf deutsche Literale gelesen, Vater-Vorschau auf Scope, Plural-Zeilen nachgerechnet. **Entgangen:**
[B-319](backlog/B-319-zwei-zusicherungen-aus-b-305-tragen-nicht.md) (zwei Zusicherungen aus B-305 können nicht fallen) und
[B-320](backlog/B-320-klausur-fehler-erreicht-den-bildschirm-nicht.md) (P2, älter: ein Fehler beim Beantworten in der
Klausur erreicht nie den Bildschirm). Beide Increments stammen aus **früheren** Läufen – Limit 2 greift nicht; beide
Stories gehen in Teil 2. `nachgeschaut` an B-305, B-306, B-307, B-315, B-316. Index danach: **Nachgeschaut 226 von 231**
(die fünf offenen sind die dieses Sprints).

**Was die eigenen Tore durchgelassen haben:** dieselbe Familie wie in Lauf F und G – eine Regel an einer Stelle belegt,
ihr Geschwister nicht: B-306 testete den Lückentext, nicht die drei anderen Strategien; B-264 hier zuerst den kindlosen
Pfad, nicht `--general` mit Kind. Neu: ein **`.or(…)` in einer E2E-Zusicherung** (B-319) ist derselbe Zweig wie das
`if (await … .isVisible())` aus Vorschlag (T) – er lief nie, und (T) in der vorgeschlagenen Fassung hätte ihn nicht
gefangen.

**Vorschlag, nicht gelandet (Freigabe 3):** **(U)** Vorschlag (T) auf `.or(` innerhalb eines `expect` in
`frontend/e2e/*.ts` ausdehnen. Bestand gemessen: drei Stellen; zwei sind ehrliche Wartepunkte auf „leer *oder* erste
Zeile“ (`shop-verlauf.spec.ts:54`, `shop-verlauf-wiederholen.spec.ts:56`) und bräuchten eine begründete Ausnahme, die
dritte ist B-319.

Commit Teil 1: `c195ef2`.

## Lauf H, Teil 2 — Meldungen erreichen Kind und Vater verständlich (B-320, B-319, B-254, B-317, B-205, B-281), Zähler 5

**Sprint-Ziel:** *Wo das Kind oder der Vater auf eine Sperre oder einen Fehler trifft, liest er einen Satz in seiner
Sprache, der zu dem passt, was er getan hat – nie eine englische Serverzeile, einen Enum-Wert oder nichts.* Umfang
(sechs `Defekt`, unter Freigabe 1 ausformuliert und geschätzt; B-205 mit der Entscheidung des Nutzers vom 2026-10-23):
[B-320](backlog/B-320-klausur-fehler-erreicht-den-bildschirm-nicht.md) (P2, Klausur zeigt den Fehler nicht),
[B-254](backlog/B-254-weg-zeigt-englischen-planfehler.md) (P2, Weg zeigt „Study plan not found.“),
[B-319](backlog/B-319-zwei-zusicherungen-aus-b-305-tragen-nicht.md), [B-317](backlog/B-317-ziel-etappe-ohne-titel-zeigt-enum.md),
[B-205](backlog/B-205-exercise-in-use-ohne-deutsche-fassung.md), [B-281](backlog/B-281-pin-felder-pruefen-mindestlaenge-im-ui.md).
Draußen, weil sie dem Ziel nicht dienen: B-280, B-284 (Fokus/Tastatur), B-257 (Feier-Zeitpunkt).

**Entwickler-Brief:** Frontend zuerst prüfen, ob der Server die richtige Antwort schon gibt – nur B-205 braucht ihn
(eigener Code für die Wörter-Sperre, kleiner Vertragsbruch, Vertrag und `contract.ts` neu erzeugen). Die Arcade nimmt
ihre Sätze über `useErrorMessage`/`t`, das Vater-Web über `GERMAN_PROBLEM_TEXT` bzw. eine Prüfung vor dem Absenden
(B-281, Weg 1 – Weg 2 wäre gegen B-28 Entscheidung 2). Testweg: Komponententests je Bildschirm, rote Probe gegen den
Ausgangsstand, volle E2E-Suite als Rollengang.

### Iteration

- Rote Proben gegen den Ausgangsstand: B-320 **1 failed**, B-254 **2 failed**, B-317 **3 failed** (Datum deutsch blieb
  grün: die lokale Test-Locale ist Deutsch – gemessen; je Fall darum die Mutante „fest in der anderen Sprache“), B-205
  **1 failed** (Backend) und **2 failed** (Texte). B-319 und B-281 ohne Ausgangsfall (Testlücke bzw. neues Modul):
  Mutanten je **1–2 failed**.
- Selbst gestrichen vor dem Review: ein zweiter B-320-Fall ohne eigene Mutante (fiel aus demselben Grund wie der erste).

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | dritter Sperrpfad (Einfügen mit `orderIndex`) ungetestet | dritte Anfrage im Fall; Mutante **1 failed** | ja |
| 2 | `frontend-reviewer` 🟡 | Weg blitzte nach dem Laden der Planliste einen Render lang den Fehlerkasten | `ProgressForPlan` erst mit der Wahl gemountet | ja |
| 3 | `frontend-reviewer` 🟡 | Testantwort passte nicht zum Bildschirm, Erfolgsweg ohne Zusicherung | vollständige Antwort, positive Zusicherung | ja |
| 4 | `frontend-reviewer` 🟡 | B-320-Fall ohne Code sah den Formatierer nicht | Fall in der englischen Arcade; Mutante **1 failed** | ja |
| 5 | `frontend-reviewer` 🟡 | `pin.test.ts` behauptete, ein sechstes Formular zu fangen | Behauptung gestrichen, Grenze benannt | ja |
| – | beide 🟢 | `409` an den Item-Routen, Wiki, zweiter Etappen-Bereich, Doku-Zeile im Shop | genommen | nein |
| – | `frontend-reviewer` 🟢 | Datum in `RequestsTab` ohne eigenen Fall; Text „Inhalte daraus“ bei einer einzelnen Übung | zurückgestellt bzw. als Kosten in B-205 Entscheidung 2 | nein |

**Zähler: 5** – an der Grenze, nicht darüber (Freigabe 3). Fund 5 ändert nur einen Testkommentar, zählt aber, weil
eine Testdatei geändert wurde. Backend **1000/1000**, Vitest **555/555**, `tsc -b` sauber, volle E2E **99 passed**
(alle `exit=0`, die E2E nach der letzten Änderung gestartet). Rollengang: Sohn über `sprache.spec.ts` (englische
Übungsrunde, jetzt fest „Card 2“) und den Spielweg der Suite, Vater über `creator-lehrwerk-weg.spec.ts` (Löschsperre
jetzt deutsch) und die übrigen Vater-Specs, Creator Regressionszeuge (Vertrag um einen Code erweitert, keine seiner
Specs rot). Nicht im Browser belegt: der Klausur-Fehlerweg und der verwaiste Plan (beide brauchen einen scheiternden
bzw. veränderten Server) – dafür die Komponententests. **B-320, B-319, B-254, B-317, B-205, B-281 `abgenommen`.**

Commit Teil 2: `f96d358`.

### Retrospektive

**Nachschau:** Teil 1 (`c195ef2`, B-311/B-312/B-279/B-264/B-318) von einem frischen `pugling-reviewer`, Dateien im Stand
des Commits, Mutante je Fall hergeleitet, alle Listenaufrufe und Id-Parameter des Agenten durchgesehen. **Zwei
Entgleiten im Increment dieses Laufs**, je als `Defekt` angelegt und im Sprint behoben:
[B-321](backlog/B-321-null-element-fall-ohne-pflichtwortschatz.md) (die `null`-Fälle lesen ohne Pflicht-Wortschatz
`CoversRequiredWords` nie; vier Mutanten danach je **1 failed**, `entgangen_bei: B-318, B-306`) und
[B-322](backlog/B-322-briefing-verb-nennt-interessen-nicht.md) (das Verb `briefing` verwies auf Interessen, ohne sie zu
nennen; **1 failed** vor dem Fix, `entgangen_bei: B-264`). Beide außerhalb des Diffs von Teil 2, darum ohne Zählung.
`nachgeschaut` an B-311, B-312, B-279, B-264, B-318. Suite danach **1001/1001** (`exit=0`). Index: **Nachgeschaut 231
von 239** (offen: die sechs Stories von Teil 2 und B-321/B-322).

**Was die eigenen Tore durchgelassen haben:** zum dritten Mal in Folge dieselbe Familie – die Regel an einer Stelle
belegt, das Geschwister nicht. Neu ist die Form: B-321 ist eine Lesestelle, die **nur unter einer Bedingung läuft**
(Pflicht-Wortschatz gesetzt), und der Fall setzte sie nicht; die Mutante war dort strukturell grün. Zwei weitere
Lehren aus Teil 2 selbst, beide im Lauf gefangen: (1) die lokale Vitest-Umgebung formatiert Daten deutsch, die
Linux-CI vermutlich englisch – eine Datums-Zusicherung „ohne Sprache ist falsch“ fällt darum je Plattform nur in einem
der beiden Fälle (gemessen: gegen den Ausgangsstand blieb der deutsche Fall grün); (2) eine Mutante an einer **neuen,
ungetrackten** Datei lässt sich nicht per `git checkout` zurücknehmen – der Befehl tat nichts, die Mutante stand noch
in `pin.ts`, erst der Nachlauf von `grep` zeigte es.

**Warum der Lauf hier endet (Limit 2):** die Nachschau fand zwei Defekte im Increment von Teil 1 – das ist das
Increment dieses Laufs. Dass beide XS waren und behoben sind, ändert die Regel nicht (wie in Lauf E, F, G). Der Zähler
von Teil 2 stand zudem bei 5.

**Vorschläge, nicht gelandet (Freigabe 3):**

- **(V)** Prosa in `pm-loop` Step 5 (Mutanten): eine Mutante wird aus einer Sicherungskopie zurückgespielt, nie per
  `git checkout` – bei einer neuen Datei ist das ein stilles No-op. Eine Zeile; ergänzt die Erinnerung „Wiederherstellung
  braucht neue Zeit“.
- **(W)** Tor: die Vitest-Läufe auf eine Locale festlegen (`LANG`/`LC_ALL` bzw. `Intl`-Vorgabe in `src/test-setup.ts`
  oder im Testskript), wie CI es mit `TZ: UTC` für die Zeitzone tut. Bestand gemessen: `vitest.config.ts` und
  `test-setup.ts` setzen keine Locale; CI setzt nur `TZ`. Dann misst eine Datums-Zusicherung lokal und in CI dasselbe.
- **(X)** Prosa für die Reviewer-Briefings: „Läuft die Lesestelle nur unter einer Bedingung (Pflicht-Wortschatz,
  Sprache, Plan vorhanden), muss der Fall die Bedingung setzen“ – die dritte Spielart der Geschwister-Familie, nach
  (T)/(U) für Zweige in Specs.

## Ende Lauf H

Endgrund: **Limit 2** – die Nachschau von Teil 2 fand im Increment von Teil 1 zwei Entgleiten (B-321, B-322, behoben).
Zwei Teile, **dreizehn Stories abgenommen**: B-311, B-312, B-279, B-264, B-318 (Teil 1), B-320, B-319, B-254, B-317,
B-205, B-281 (Teil 2), B-321, B-322 (aus der Nachschau); dazu fünf ältere nachgeschaut (B-305, B-306, B-307, B-315,
B-316). Höchster Zähler 5 (Teil 2). Commits `c195ef2`, `f96d358` und der Abschluss-Commit. Alles **ungepusht**.

**Offene Entscheidungen für den Nutzer:**

1. **Retro-Vorschläge (U)–(X)**, keiner gelandet; dazu unverändert (S), (T) aus Lauf G, (O)–(R) aus Lauf F, (H)–(N)
   aus Lauf E.
2. **B-205 Entscheidung 2 gegenlesen:** der feste deutsche Satz für `exercise_in_use` ersetzt beim Löschen einer Übung
   die Zählung des Servers (eigene gegen unsichtbare Verwendungen) durch „vielleicht auch bei einem Kind, das du nicht
   betreust“. Reicht das, oder soll der Code für den Fall „nur unsichtbare Verwendungen“ geteilt werden?
3. **Neue Texte gegenlesen:** `objectives.untitled.*` (de/en), `exercise_in_use`, `exercise_items_locked`, die
   PIN-Meldung.
4. Unverändert offen: B-313 (`Frage`), B-310 (Vertragsfrage 404/403), B-291 (`Frage`), B-267, B-268; die englischen
   Texte aus Lauf G (`en.ts`).
5. Nächste baubare `Defekt`-Kandidaten: B-280 und B-284 (Fokus/Tastatur, P2/P3), B-257, B-258, B-277.

## Lauf I — Start

Auftrag: „Starte Nachtlauf“ – der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10, keine
Paket-Vorgabe. Ausgangslage: HEAD `f6a2347`, Baum sauber, 93 Commits ungepusht. Index „Nachgeschaut 231 von 239“ –
unbeobachtet sind die sechs Stories aus Lauf H, Teil 2 und B-321/B-322; die Nachschau des ersten Sprints nimmt alle
acht. Auswahl nach Thema aus den Kandidaten am [Ende Lauf H](#ende-lauf-h).

## Lauf I, Teil 1 — Das Kind wird nicht übergangen und nicht falsch gefeiert (B-284, B-257)

**Sprint-Ziel:** *Das Kind sieht beim Üben jede Karte, auch wenn es zweimal Enter drückt, und wird nur für einen Erfolg
gefeiert, den es gerade geschafft hat.* Umfang: [B-284](backlog/B-284-doppeltes-enter-ueberspringt-karte.md),
[B-257](backlog/B-257-zielfeier-verspaetet-jenseits-der-ersten-fuenf.md) – beide `Defekt` auf `idee`, unter Freigabe 1
ausformuliert, gegrillt und geschätzt (XS; Ist-Stand je Story gemessen). Draußen, weil sie dem Ziel nicht dienen:
B-280 (Fokus im Vater-Formular), B-258/B-277 (Creator/Vertrag) – Kandidaten für Teil 2.

**Entwickler-Brief:** Reines Frontend, kein Server-Code. Die Sperre in `useAction` bleibt die eine Quelle dafür, wer
weiterschaltet (B-284: `if (ran) next()`). Für B-257 bleibt die Server-Sortierung unangetastet; nur der erste Besuch
lädt bis `MaxTake` und seedet die Gesehen-Mengen daraus. Testweg: Komponententests mit roter Probe gegen den
Ausgangsstand, Mutante je Fall; Rollengang über die E2E-Suite.

### Iteration

- Rote Proben gegen den Ausgangsstand: B-284 **1 failed** (gemessen „Karte 3 / 3“), B-257 **1 failed** (2 Feiern statt
  keiner). Mutanten der neuen Fälle je **1 failed** (Liste ungekürzt, immer 500, Plural fest, Währung fest).
- Vitest **561/561** (`exit=0`), `tsc -b` sauber.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | B-284: Mutante `if (!ran) next()` grün, AK 2 („rückt auch nach Fehlschlag weiter“) nur als Text | Zwischen-Zusicherung nach leerem `act`, Fall „Fehlschlag“; Mutante **2 failed** | ja |
| 2 | `frontend-reviewer` 🟡 | B-257: Geräte mit altem Stand (nur fünf Ziele geseedet) bleiben falsch | Schlüssel `v2`, Fall „altes Gerät“; Mutante **2 failed** | ja |
| – | `frontend-reviewer` 🟢 | Plural-Fall trennt „neu erreicht“ nicht von „erreicht“ | Setup mit vorher erreichter Etappe; Mutante **2 failed** | nein |
| – | `frontend-reviewer` 🟢 | Enter während „anderes Bild“ tut jetzt nichts | angenommen (vorher: Karte ohne Bewertung weiter) | nein |

Selbst gefangen beim Beheben von Fund 1: die erste Fassung der Zwischen-Zusicherung prüfte, bevor der verworfene Aufruf
zu Ende lief – die Mutante blieb **gemessen** grün, erst das leere `act` davor machte sie rot.

**Zähler: 2.** Vitest **563/563**, `tsc -b` sauber, volle E2E **99 passed** (alle `exit=0`, E2E nach der letzten
Änderung gestartet). Rollengang: Sohn über den Spielweg der Suite (Üben, Klausur, Startseite mit Zielen) – die beiden
Fehlerbilder selbst (Doppel-Enter im selben Tick, ein Gerät mit Vorgeschichte) tragen die Komponententests; Vater und
Creator Regressionszeugen (Suite grün, kein Pfad von ihnen im Diff). Browser-Extension nicht genutzt.
**B-284, B-257 `abgenommen`.**

### Retrospektive

**Nachschau:** die acht bis dahin unbeobachteten Stories aus Lauf H, je von einem frischen Reviewer im Stand des Commits.
Teil 2 (`f96d358`): `frontend-reviewer` über B-320/B-319/B-254/B-317/B-281 und den Text von B-205, `pugling-reviewer`
über den Server-Teil von B-205; Abschluss (`f6a2347`): `pugling-reviewer` über B-321/B-322 (Filterlauf **6/6 passed**).
**Entgangen:** [B-324](backlog/B-324-gemerkter-plan-zweig-ungetestet.md) (🟡, Zweig „gemerkter Plan“ aus B-254 ohne Fall
– fällt er weg, ist der Plan-Wähler tot) und [B-325](backlog/B-325-klausur-doppelklick-sperre-ungetestet.md) (🟢, AK 3
von B-320 ohne Fall). Außerhalb der AKs bzw. älter: [B-326](backlog/B-326-ueben-und-klausur-nehmen-ungepruefte-plan-id.md)
(Reload nach Plan-Löschen) und [B-323](backlog/B-323-ganzer-uebungs-put-umgeht-woerter-sperre.md) (P2: ganzer PUT und
`refs-from-tags` umgehen die Wörter-Sperre, Lernstand kann ans falsche Wort rutschen). Alle aus dem Increment eines
**früheren** Laufs – Limit 2 greift nicht. `nachgeschaut` an B-320, B-319, B-254, B-317, B-205, B-281, B-321, B-322,
je mit dem geprüften Punkt.

**Was die eigenen Tore durchgelassen haben:** zwei Spielarten, beide vom Reviewer gefunden, keine vom Tor. (1) Eine
Zusicherung über einen **Zwischenstand** direkt nach `fireEvent` prüft vor dem `await` des Handlers – sie sieht wie ein
Beleg aus und kann nicht fallen (beim Beheben gemessen). (2) Ein Fix an **abgeleitetem, gespeichertem Zustand**
(`localStorage`) heilt nur neue Geräte; der Bestand braucht eine eigene Entscheidung, und die Story hatte keine.

**Vorschläge, nicht gelandet (Freigabe 3):**

- **(Y)** Prosa in `frontend/CLAUDE.md` (Tests): „Ein Zwischenstand nach `fireEvent` auf einen `async`-Handler wird erst
  nach `await act(async () => {})` zugesichert“ – ein Satz, gemessen an B-284.
- **(Z)** Prosa in `docs/backlog/README.md` (Ausformulieren): „Ändert ein Fix, wie gespeicherter Client-Zustand
  entsteht, sagt die Story, was mit dem schon gespeicherten geschieht“ – eine Entscheidung, kein Tor.

Commit Teil 1: siehe unten.

Commit Teil 1: `c1a4e3f`.

## Lauf I, Teil 2 — Das Kind spielt den Plan, den es gewählt hat (B-324, B-325, B-326)

**Sprint-Ziel:** *Das Kind bleibt bei dem Plan, den es auf der Basis wählt; ein Doppelklick in der Klausur zählt
einmal; und wer nach dem Löschen eines Plans neu lädt, landet auf der Basis statt vor einer englischen Serverzeile.*
Umfang: die drei Stories aus der Nachschau von Teil 1 – [B-324](backlog/B-324-gemerkter-plan-zweig-ungetestet.md) (P2),
[B-325](backlog/B-325-klausur-doppelklick-sperre-ungetestet.md), [B-326](backlog/B-326-ueben-und-klausur-nehmen-ungepruefte-plan-id.md),
alle `Defekt`, unter Freigabe 1 ausformuliert und geschätzt (XS). Draußen: [B-323](backlog/B-323-ganzer-uebungs-put-umgeht-woerter-sperre.md)
(Creator/Backend, eigenes Thema), B-258 (Vertragsfrage, siehe Ende).

**Entwickler-Brief:** Reines Frontend. B-324/B-325 sind Testlücken an richtigem Code – Beleg ist die Mutante. B-326 nutzt
den Status 404, nicht den generischen Code `not_found`, und nur am Start; die Basis (`useChosenPlan`) bleibt die eine
Stelle, die eine Wahl korrigiert. Testweg: Komponententests, rote Probe bzw. Mutante je Fall; Rollengang über die E2E-Suite.

### Iteration

- B-324: Mutante „Zweig gemerkt gestrichen“ **1 failed**. B-325: Mutante „Sperre weg“ **1 failed**. B-326: rote Probe
  **2 failed**, Gegen-Mutante „jeder Fehler zur Basis“ je **1 failed**.
- Aus der Nachschau von Teil 1 in den Sprint genommen: [B-327](backlog/B-327-ziel-spaeter-jenseits-platz-fuenf-erreicht.md)
  (rote Probe **1 failed**, Mutanten **3 failed** / **2 failed**), siehe Retrospektive.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | B-325-Fall erreichte die Ref-Sperre nie (`disabled` fing den zweiten Klick nach einem Render ab) | beide Klicks in einem `act`; Mutante „Ref-Prüfung weg“ **1 failed** | ja |
| – | `frontend-reviewer` 🟢 | `.error-box`-Zusicherung fällt nur als Nebenwirkung | beschriftet | nein |
| – | `frontend-reviewer` 🟢 | `replace` ohne Fall; 403 statt 404 für eine fremde Plan-Id | zurückgestellt, in B-326 benannt | nein |
| – | Delta-Review 🟢 | Entscheidung 2 von B-327 ohne Zusicherung; Titel/Kommentar | genommen; Mutante **1 failed** | nein |

B-327 selbst stammt aus der Nachschau und liegt im Increment von Teil 1, nicht im Diff von Teil 2 – ohne Zählung (wie
B-321/B-322 in Lauf H). **Zähler: 1.** Vitest **569/569**, `tsc -b` sauber, volle E2E **99 passed** (alle `exit=0`,
E2E nach der letzten Änderung gestartet). Rollengang: Sohn über den Spielweg der Suite (Basis, Weg, Üben, Klausur); die
drei Fehlerbilder (bewusste Planwahl, Doppelklick im selben Tick, Reload nach Plan-Löschen) tragen die Komponententests;
Vater und Creator Regressionszeugen. Browser-Extension nicht genutzt. **B-324, B-325, B-326, B-327 `abgenommen`.**

### Retrospektive

**Nachschau:** Teil 1 (`c1a4e3f`, B-284/B-257) von einem frischen `frontend-reviewer` im Stand des Commits. B-284 ohne
Befund (einziger Aufrufer von `next()`, Scan aller `await *.run(…)`). **Entgangen bei B-257:**
[B-327](backlog/B-327-ziel-spaeter-jenseits-platz-fuenf-erreicht.md) (🟡) – spätere Besuche luden wieder nur fünf Ziele;
Entscheidung 1 von B-257 nahm an, der Fall zähle nur beim ersten Besuch. Im Sprint behoben. `nachgeschaut` an B-284,
B-257.

**Was die eigenen Tore durchgelassen haben:** dieselbe Familie wie in Teil 1, nun zweimal im selben Lauf: (1) eine
**Annahme in einer Entscheidung** („zählt nur beim ersten Besuch“) wurde nicht gegen die User Story gerechnet – der
Review von Teil 1 prüfte die AKs, und AK 1 nannte wörtlich nur den ersten Besuch; (2) ein Doppelklick-Test, dessen
zwei `fireEvent` dazwischen rendern, prüft `disabled`, nicht die Sperre – das Spiegelbild von (Y): dort prüfte die
Zusicherung zu früh, hier der zweite Klick zu spät.

**Warum der Lauf hier endet (Limit 2):** die Nachschau fand im Increment von Teil 1 – dem Increment dieses Laufs – einen
Defekt (B-327). Dass er XS war und behoben ist, ändert die Regel nicht.

**Vorschläge, nicht gelandet (Freigabe 3):**

- **(AA)** Prosa in `docs/backlog/README.md` (Ausformulieren): „Eine Entscheidung, die einen Fall ausschließt (‚zählt
  nur …‘), wird gegen die User Story gerechnet, nicht nur gegen die AKs.“
- **(AB)** Ergänzung zu (Y) für `frontend/CLAUDE.md`: „Ein Doppelklick, der die Ref-Sperre prüfen soll, feuert beide
  Klicks in einem `act` – sonst prüft er `disabled`.“

## Ende Lauf I

Endgrund: **Limit 2** – die Nachschau von Teil 2 fand im Increment von Teil 1 einen Defekt (B-327, behoben). Zwei Teile,
**sechs Stories abgenommen**: B-284, B-257 (Teil 1), B-324, B-325, B-326, B-327 (Teil 2); dazu acht ältere nachgeschaut
(Lauf H). Höchster Zähler 2 (Teil 1). Commits `c1a4e3f` und der Abschluss-Commit. Alles **ungepusht**.

**Offene Entscheidungen für den Nutzer:**

1. **Retro-Vorschläge (Y), (Z), (AA), (AB)**, keiner gelandet; dazu unverändert (U)–(X) aus Lauf H und ältere.
2. **B-258 (Vertragsfrage):** dieselbe unbekannte Referenz meldet `invalid_reference` oder `validation_error` – nicht nur
   an den zwei Stellen der Story, auch `GrammarTopicsController.cs:85`, `SeriesUnitsController.cs:191` und
   `TextbooksController.cs:146,151`. Vereinheitlichen auf `invalid_reference` (kleiner Vertragsbruch) – ja/nein?
3. **B-323 (P2, neu):** ganzer Übungs-PUT und `refs-from-tags` umgehen die Wörter-Sperre einer gespielten Übung;
   Lernstand kann ans falsche Wort rutschen. `Defekt`, autonom baubar – Kandidat für den nächsten Lauf; zu klären beim
   Ausformulieren, ob Sperre erweitern oder Doku verengen.
4. **B-257/B-327 Entscheidung (`v2`-Schlüssel):** jedes Gerät seedet einmal still neu – eine seit dem letzten Besuch
   neu erreichte Etappe bleibt dabei einmal ungefeiert. Gegenlesen.
5. Nächste baubare `Defekt`-Kandidaten: B-323, B-280, B-262/B-266 (Assistent), B-275, B-277.

**Nachtrag, nach Lauf I im Dialog entschieden** („Nimm die von dir vorgeschlagenen Entscheidungen“): (Y) und (AB) als
ein Satz in `frontend/CLAUDE.md` (Testabsatz; bezahlt durch Kürzen von Widget-, E2E- und Reihen-Absatz, netto +16 Bytes –
die Datei bleibt mit 10 365 von 9 000 Bytes über Budget, die Altlast ist unverändert offen); (Z) und (AA) in
`docs/backlog/README.md` → „Ausformulieren heißt gegen den Code belegen“; B-258: einheitlich `invalid_reference`
(Freigabe im Verlauf der Story); `v2`-Schlüssel bestätigt. Ältere Vorschläge (U)–(X) und davor bleiben offen.

## Lauf J — Auftrag und Ausgangslage

Auftrag: „Starte Nachtlauf" – der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10. Ausgangslage:
HEAD `ecc899d`, Baum sauber, 96 Commits ungepusht. Keine Paket-Vorgabe; erste Kandidaten laut
[Ende Lauf I](#ende-lauf-i): B-323 (P2) und B-258 (freigegeben). Index zu Beginn: „Nachgeschaut 241 von 245" – offen
sind B-324..B-327 (Lauf I, Teil 2); die Nachschau des ersten Sprints nimmt sie.

## Lauf J, Teil 1 — Der Creator pflegt über die API, ohne dem Kind etwas zu verschieben (B-323, B-258)

**Sprint-Ziel:** *Der Creator, der seinen Katalog über die API pflegt (auch als KI-Creator), kann einer Vokabelübung,
die ein Kind schon spielt, auf keinem Weg den Lernstand an ein anderes Wort schieben – und eine Id, die auf nichts
zeigt, meldet ihm überall denselben Code.* Umfang: [B-323](backlog/B-323-ganzer-uebungs-put-umgeht-woerter-sperre.md)
(P2, `Defekt`, unter Freigabe 1 ausformuliert, gegrillt, geschätzt: S) und
[B-258](backlog/B-258-unbekannte-fach-id-zwei-fehlercodes.md) (P3, Freigabe 9: XS). Draußen, neu abgelegt:
[B-328](backlog/B-328-inline-typen-put-verschiebt-lernstand.md) (`Frage`: Inline-Typen und Wort-Tausch – Korrektur
oder Ersatz?) und [B-329](backlog/B-329-uebung-unbekannte-referenz-validation-error.md) (Übungs-Validierung, außerhalb
der freigegebenen Fundstellen).

**Entwickler-Brief:** Reines Backend. Quelle der Wahrheit für die Sperre ist die Item-Liste, nicht die Route: die Regel
„bisherige Items bleiben der Anfang, in derselben Reihenfolge" sitzt jetzt in `ExerciseItemService.WouldShiftAsync`
und wird an allen drei schreibenden Wegen gefragt; am `PUT` **vor** dem ersten Schreiben (neuer Haken
`UpdateConflictAsync`), damit ein abgewiesener `PUT` auch den Kopf nicht ändert. B-258 tauscht nur den Code an sechs
Stellen. Testweg: Integrationstests, rote Probe und Mutante je Fall, volle Suite, Live-Probe als Creator.

### Iteration

- B-323: rote Probe **2 failed** (erwartet 2, beide an `Conflict` erwartet / `OK` gemessen). Mutanten „Obermenge statt
  Präfix" **1 failed**, „Prüfung nach dem Speichern" **1 failed** (Titel halb geschrieben).
- B-258: rote Probe **1 failed** (alle sechs Stellen `validation_error`); Mutante „Unit-Stelle zurück" **1 failed**, benannt.
- Erwartete Anpassung, kein Fund: `SharedLibraryScenarioTests` Schritt 7 ersetzte die Wörter einer Übung, die gerade
  Toms Plan spielt – genau das, was B-323 sperrt. Schritt 7 prüft jetzt die Änderung ohne Wörter (Absicht des Schritts:
  die Autorin darf schreiben). Das Frontend schickt beim Bearbeiten nie Items (`exerciseConfig.tsx`, `items: []`).
- Volle Suite **1004/1004**, `tsc -b` sauber nach Neuerzeugen des Vertrags, `dotnet format` ohne Fund in den
  geänderten Dateien.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | Refs-Zweig der neuen Prüfung ohne Fall (Vorrang Refs vor Items umkehrbar, ohne dass ein Test fällt) | zwei Fälle; Mutanten **1 failed** (Z. 332), **1 failed** (Z. 332), **1 failed** (Z. 354) | ja |
| – | `pugling-reviewer` 🟡 | doppeltes Wort im Payload → `500` nach dem Speichern des Kopfes | älter als der Diff → [B-331](backlog/B-331-doppeltes-wort-im-uebungs-payload-500.md); Wiki-Satz auf `409` verengt | nein |
| – | `pugling-reviewer` 🟢 | Schlüssel-Refs in `PeekDesiredIdsAsync`; Szenario-Schritt 7 | Kommentar; `409` zugesichert | nein |
| – | `pugling-reviewer` 🟢 | redundante Klausel; Prüfen und Schreiben nicht atomar | belassen (dasselbe Muster wie die Item-Routen) | nein |

**Zähler: 1.** Volle Suite **1004/1004**, E2E **99 passed** (`exit=0`, nach der letzten Änderung gestartet), `tsc -b`
sauber. Rollengang **Creator**: Live-Probe gegen einen nach der letzten Änderung gestarteten Server (Wegwerf-DB, Port
5280): Übung anlegen, per Supervisor-API in einen Plan nehmen, `PUT` ohne erstes Wort → `409 exercise_items_locked`,
Titel unverändert; Anhängen → `200`; Lehrbuch und Grammatikthema mit unbekanntem Fach → `400 invalid_reference`.
**Vater** und **Sohn** Regressionszeugen: E2E grün, das Frontend schickt beim Bearbeiten keine Items und liest keinen der
beiden Codes. Browser-Extension nicht genutzt. **B-323, B-258 `abgenommen`** (`1a3a73f`).

### Retrospektive

**Nachschau:** Lauf I, Teil 2 (`b64006c`, B-324..B-327) von einem frischen `frontend-reviewer`. B-324, B-325, B-327 ohne
Befund (je mit Mutante nachgerechnet, Prüfpunkte in den Stories). **Entgangen bei B-326:**
[B-330](backlog/B-330-plan-inactive-ohne-deutsche-fassung.md) (🟡) – die Story nennt „Löschen oder Deaktivieren“, der Fix
behandelt nur `404`; ein deaktivierter Plan antwortet `403 plan_inactive`, und der Code hat im Frontend keine Fassung.
Ist-Stand von B-326 korrigiert (fremder Plan `403`). `nachgeschaut` an B-324..B-327. Liegt im Increment eines
**früheren** Laufs – Limit 2 greift nicht.

**Was die eigenen Tore durchgelassen haben:** (1) Eine Prüfung, die eine Config mit **zwei Formen** liest (`refs` oder
`items`, mit Vorrang), war nur mit einer Form getestet – der Reviewer fand es, kein Tor. Familie „Regel getestet,
Grenzfall offen“. (2) In der Nachschau wieder die Familie von (AA): ein Fall aus der Problembeschreibung („oder
Deaktivieren“) fehlte in den AKs – B-326 ist älter als die gelandete Regel (AA), also kein neuer Befund über sie.

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(AC)** Prosa in `backend/CLAUDE.md` (Tests): „Liest eine Prüfung eine Eingabe mit mehreren Formen (`refs`/`items`,
  Inline/Id), bekommt jede Form einen Fall – und der Vorrang einen eigenen, in dem die Formen sich widersprechen.“
  Kein Tor: welche Formen eine Eingabe hat, kann kein Wächter aus dem Code lesen.

## Lauf J, Teil 2 — Das Kind versteht, warum sein Plan gerade nicht geht (B-330)

**Sprint-Ziel:** *Das Kind, dessen Plan pausiert, abgelaufen oder nicht der aktive ist, liest in seiner Sprache, was los
ist und wen es fragen kann – auch nach einem Reload auf Üben oder Klausur, nie eine englische Serverzeile mit Ref.*
Umfang: [B-330](backlog/B-330-plan-inactive-ohne-deutsche-fassung.md) (P2, `Defekt`, aus der Nachschau von Teil 1; unter
Freigabe 1 ausformuliert, gegrillt, geschätzt: XS). Ein Sprint aus einer Story ist zulässig; andere Sohn-Defekte dienen
diesem Ziel nicht. Draußen: [B-331](backlog/B-331-doppeltes-wort-im-uebungs-payload-500.md) (Creator/Backend).

**Entwickler-Brief:** Reines Frontend, kein Vertrag. Der Code ist die Quelle; der Satz gehört der Oberfläche
(`GERMAN_PROBLEM_TEXT` fürs Deutsche, `PROBLEM_KEYS` + `sohn.problem.*` für die Arcade, Deutsch wortgleich). Keine
Umleitung (Entscheidung 1). Testweg: Komponententest mit roter Probe und Mutante, Vitest, E2E-Regression.

### Iteration

- Rote Probe **1 failed** (deutsche Zeile, gemessen die englische Serverzeile mit Ref). Mutante „Arcade-Schlüssel
  gestrichen" **1 failed** (englische Zeile). Vitest **570/570**, `tsc -b` sauber.
- Mitgenommene 🟢 aus der Nachschau: zwei Absichtserklärungen beschriftet (`SohnPractice.test.tsx`, `SohnTest.test.tsx`).
- Aus der Nachschau von Teil 1 in den Sprint genommen: [B-332](backlog/B-332-put-nur-mit-refs-ungetestet.md) (Refs-only-PUT
  ohne Fall; Fall ergänzt, Mutante **1 failed**), siehe Retrospektive.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | `SohnPractice.judge` verschluckte `plan_inactive` mitten in der Runde – Entscheidung 1 behauptete das Gegenteil | Runde endet mit dem Satz; rote Probe **1 failed**, Mutante **1 failed** | ja |
| – | `frontend-reviewer` 🟢 | „Weiter versuchen“ nach `plan_inactive` holt dieselbe 403 | ausgeblendet; rote Probe **1 failed** | nein |
| – | `frontend-reviewer` 🟢 | englischer Satz steif, Singular/Plural; Testkommentar ungenau | angepasst | nein |
| 2 | Delta-Review 🟡 | Neustart aus dem Ergebnis trug den alten Versuch → „Weiter versuchen“ wieder da | `start()` setzt Versuch zurück; rote Probe **1 failed** (Z. 85) | ja |
| – | Delta-Review 🟢 | `resume()`-Zweig ohne Fall | Fall; Mutante **1 failed** (Z. 101) | nein |
| – | Delta-Review 🟢 | `reshuffleImage` beendet die Runde nicht; doppelte Prüfung | zurückgestellt (nächste Bewertung beendet sie) | nein |

B-332 stammt aus der Nachschau und liegt im Increment von Teil 1, nicht im Diff von Teil 2 – ohne Zählung (wie B-327 in
Lauf I). **Zähler: 2.** Vitest **574/574**, `tsc -b` sauber, Backend **1004/1004**, E2E **99 passed** (`exit=0`, nach der
letzten Frontend-Änderung gestartet; B-332 ändert nur einen Backend-Test). Rollengang **Sohn**: Live-Probe gegen einen frisch
gestarteten Server – Kind startet Üben (`201`), Vater deaktiviert, Kind startet erneut → `403 plan_inactive`; die Anzeige
tragen die Komponententests. **Vater**/**Creator** Regressionszeugen. Browser-Extension nicht genutzt. **B-330
`abgenommen`** (`02ce632`). B-332: Delta-Review (`pugling-reviewer`) ohne Blocker, 🟢 Code-Zusicherung übernommen,
Suite **1004/1004** → **`abgenommen`** (`599e217` + Abschluss-Commit).

### Retrospektive

**Nachschau:** Teil 1 (`1a3a73f`, B-323/B-258) von einem frischen `pugling-reviewer`. **Entgangen bei B-323:**
[B-332](backlog/B-332-put-nur-mit-refs-ungetestet.md) (🟡) – ein `PUT` nur mit `refs` war von keinem Fall gehalten; die
Nachbesserung aus dem Review von Teil 1 hatte den Refs-Vorrang getestet, aber jeweils mit `items` daneben. Im Sprint
behoben. 🟢 Doku nannte den `PATCH`-Tausch und die Schlüssel-Ordnung von `refs-from-tags` nicht (nachgetragen); B-258s
Verlauf-Behauptung „keine weitere Stelle“ war falsch (`BaseFormKey` → B-329); `vocabularyId: 0` → B-331. `nachgeschaut` an
B-323, B-258.

**Was die eigenen Tore durchgelassen haben:** zweimal dieselbe Familie, beide Male vom Reviewer gefunden: (1) Eine
**Entscheidung behauptet einen Fall**, den der Code nicht trägt („der Code kommt auch mitten im Spiel“ – `judge`
verschluckte ihn). Das ist (AA) in umgekehrter Richtung: dort schloss eine Entscheidung einen Fall aus, hier nahm sie ihn
in Anspruch, ohne ihn zu prüfen. (2) Bei B-323 deckten die Fälle jede **Form einzeln mit einer anderen zusammen** ab, nie
die nackte Form – genau der Vorschlag (AC) aus Teil 1, bevor er gelandet ist.

**Warum der Lauf hier endet (Limit 2):** die Nachschau von Teil 2 fand im Increment von Teil 1 – dem Increment dieses
Laufs – eine Testlücke (B-332). Dass sie XS war und behoben ist, ändert die Regel nicht.

**Vorschläge, nicht gelandet (Freigabe 3):**

- **(AD)** Prosa in `docs/backlog/README.md` (Ausformulieren, neben (AA)): „Beruft sich eine Entscheidung auf einen Fall
  (‚kommt auch mitten im Spiel‘), wird dieser Fall am Code nachgewiesen – mit `Datei:Zeile` des Pfads, der ihn trägt.“
- **(AC′)** Schärfung von (AC): „… jede Form **allein** einen Fall – ein Fall, der zwei Formen mischt, belegt keine von
  beiden.“

## Ende Lauf J

Endgrund: **Limit 2** – die Nachschau von Teil 2 fand im Increment von Teil 1 eine Testlücke (B-332, behoben). Zwei Teile,
**vier Stories abgenommen**: B-323, B-258 (Teil 1), B-330, B-332 (Teil 2); dazu vier ältere nachgeschaut (B-324..B-327).
Höchster Zähler 2 (Teil 2). Commits `1a3a73f`, `76a5cfa`, `02ce632`, `599e217` und der Abschluss-Commit. Alles **ungepusht**.

**Offene Entscheidungen für den Nutzer:**

1. **Retro-Vorschläge (AC), (AC′), (AD)**, keiner gelandet; dazu unverändert (U)–(X) aus Lauf H und ältere.
2. **B-328 (`Frage`):** Wort-Tausch per `PATCH` und Textänderung an Inline-Typen einer gespielten Übung – Korrektur
   (erlaubt) oder Ersatz (gesperrt)?
3. **B-329:** dieselbe Vereinheitlichung auf `invalid_reference` für den Übungspfad (Interessen-/Grammatik-/Vokabel-Ids)
   und `BaseFormKey` – weiterer kleiner Vertragsbruch; braucht einen Rückgabetyp mit Code. Freigabe ja/nein?
4. **B-330, Entscheidung 1 + 5:** eine Übungsrunde bricht bei `plan_inactive` ab (ehrlich, aber hart) – gegenlesen; der
   Satz „Frag deine Eltern, welcher Plan jetzt gilt“ ist am echten Kind ungesehen.
5. Nächste baubare `Defekt`-Kandidaten: B-331 (P3, `500` bei doppeltem Wort), B-280, B-262/B-266, B-275, B-277.
