---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/training, rolle/creator, rolle/supervisor]
aliases: [KI-Aufgaben optimieren, Programm statt Modell, Lückentext-Klammern, Wortliste der Unit]
status: abgenommen
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nutzerauftrag 2026-11-10 („Analysiere die Erstellung der Aufgaben und optimiere diese“)
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-460 · Was nur eine richtige Antwort hat, entscheidet beim KI-Creator das Programm, nicht das Modell

Der erste Lehrplan-Lauf gegen das Access-Lehrwerk (2026-11-10, `plan --child 1 --profile 1 --series 5 --unit 7`,
`qwen2.5:14b-instruct`) brauchte 14,5 Minuten für vier Teile. Zwei scheiterten an den Regeln, und die beiden
durchgekommenen hatten Mängel, die kein Tor sah.

## User Story

Als Vater möchte ich, dass der KI-Creator aus den aufbereiteten Buchdaten Übungen erzeugt, die beim ersten Versuch
durchgehen und fachlich stimmen, damit ich einen Lehrplan prüfe statt ihn zu reparieren.

## Ist-Stand am Code

| Fund im Lauf | Ursache |
|---|---|
| „test = Test“, „pub = Pub“, „Museum = Museum“: zwei Reparatur-Runden, ein Teil verloren | Das Modell erfand Wortpaare samt Übersetzung, obwohl die Unit 124 Paare aus dem Buch im Vokabelspeicher hatte. Die Stoffnotiz trug nur die englischen Wörter |
| Lückentext: „Platzhalter {{1}} … 2 Lücke(n)“, „{{3}} fehlt die Lücke“, ein Teil verloren | Das Modell musste Platzhalter im Text und Lücken in einer zweiten Liste konsistent nummerieren |
| Übung 25: Ablenker als akzeptierte Lösungen | eigener Defekt [B-461](B-461-lueckentext-ablenker-zaehlen-als-loesung.md) |
| „die Lösung 'visited' steht im Text“ | Teilwortprüfung (`Contains`), dazu wiederholte Lösungen im Text |
| Titel „Wortpaare von Brawl Stars, Pokémon und Fußball“ | Keine Regel zum Titel; die Interessen wurden zum Etikett |
| Alle Lückentexte mischen jede Grammatik der Unit | Der Plan unterschied seine Teile nur durch „(Lehrplan, Cloze 1/2)“ |
| Grammatik-Aufgabe nennt die Lösung im Satz | [B-458](B-458-grammatik-entwurf-verraet-die-loesung.md) |
| `ollama ps`: 32768 Tokens Kontext, 73 % CPU | Kein `num_ctx` gesetzt; Ollama nahm seine Vorgabe |

## Entscheidungen

1. **Faustregel: Was nur eine richtige Antwort hat, entscheidet C#.** Das Modell schreibt, was Sprache und
   Einfall braucht: Sätze, Situationen, Merkhinweise, Titel.
2. **Die Wortliste der Unit ist der Vokabelstoff.** Sie wird aus den Vokabelübungen der Unit gelesen
   (`UnitVocabulary`), aber nur aus denen, die nicht der Agent erzeugt hat, und nur ab drei Wörtern. Ohne `--words`/`--use-weak` legt die Vokabelübung Wörter **und** Übersetzungen fest:
   schwache Wörter zuerst, dann je Teil die nächste Scheibe. Das Modell liefert Titel und Hinweise. Ein Hinweis,
   der die Übersetzung nennt, wird gestrichen; fehlt einer, gilt der Hinweis aus dem Buch.
3. **Lückentext in Klammer-Schreibweise.** `We [went] (go) to the Tower.` Die Nummern, Lücken und die
   alphabetisch sortierte Wortbank baut `ClozeMarkup`. Die Grundform-Hilfe direkt hinter einer Lücke zählt
   nicht als verraten. Sie muss aber genau ein Wort sein und keine Auswahlliste. Ablenker, die zugleich Lösungen
   sind, werden gestrichen (B-461).
4. **Deterministisches Aufräumen vor den Regeln** (`Normalize`): Überzählige Aufgaben werden gestrichen,
   überzählige Klammern zu Text. Ein schon vergebener Titel bekommt eine Nummer. Das kostet keine
   Reparatur-Runde.
5. **Schwerpunkt je Lehrplan-Teil.** Cloze- und Grammar-Teile bekommen reihum ein Grammatik-Thema der Unit.
   Vokabel- und Übersetzungsteile bekommen ihre eigene Wortscheibe. Die Wortscheibe steht nur bei diesen beiden
   Typen im Prompt, dort statt der Stoffnotiz zum Wortschatz.
6. **`Agent:ContextLength`** (Vorgabe 8192) geht als `num_ctx` mit jedem Aufruf.
7. **Rohdaten:** Die Stoffnotiz zum Wortschatz trägt die Wortfelder und Stolpersteine der Liste (Betonung,
   unregelmäßige Formen, kein Plural) statt einer übersetzungslosen Kopie der Wörter.

## Akzeptanzkriterien

1. Vokabelübung mit Wortliste in der Unit: Wörter, Übersetzungen und Store-Ids kommen aus der Unit, auch wenn
   das Modell sie ändert. Ein verratender Hinweis wird gestrichen.
2. Zwei Vokabelübungen eines Plans üben verschiedene Wörter.
3. Lückentext: Klammer-Schreibweise wird korrekt in Platzhalter, Lücken und eine sortierte Wortbank übersetzt.
   Unlesbare Klammern, verratene Lösungen (als ganzes Wort) und Ablenker, die zugleich Lösungen sind, sind
   Regelverletzungen.
4. Grammatik: Eine Lösung im Aufgabentext ist eine Regelverletzung (B-458). Die Grundform in Klammern zählt
   nicht.
5. Je Akzeptanzkriterium ein Test mit benannter roter Probe, und jede Probe gemessen.
6. Ein Live-Lauf mit demselben Befehl wie im Ausgangsbefund, Zahlen im Verlauf.

## Schätzung

M: ein Projekt (Agent), ein neuer Entwurfstyp für Cloze, keine Migration, kein Vertragsbruch an der API.

## Offene Punkte

- Ein einzelnes `create` ohne Plan nimmt immer die erste Wortscheibe (`Part` = 0). Zweimal hintereinander
  aufgerufen, üben beide Übungen dieselben Wörter. Abhilfe wäre ein Versatz nach der Zahl der schon erzeugten
  Übungen. Das ist absichtlich nicht gebaut, weil `--words` den Fall heute abdeckt.
- Im Vokabel-Entwurf füllt das Modell weiterhin das ganze Schema aus (`front`/`back`/`hint`), obwohl C# Wort
  und Übersetzung ersetzt. Ein schmales Schema nur für Titel und Hinweise würde den Aufruf kürzen. Gemessen:
  etwa 2,5 Minuten je Vokabel-Aufruf gegen 1,3 Minuten je Lückentext.
- Fachliche Feinheiten wie „was seeing“ (Zustandsverb im progressive) oder mehrdeutige Zeitformen fängt kein
  Tor. Dafür bleibt der Testmodus des Vaters vor dem Scharfschalten.
- [B-465](B-465-ki-lueckentext-mehrdeutig-und-verraten.md) beschreibt Übung 28 aus dem Zwischenstand (Lehrplan 3).
  Punkt 2 (Auswahlliste hinter der Lücke) ist seit Lehrplan 4 eine Regel. Offen sind Punkt 1 (Titel gegen
  Schwerpunkt), Punkt 3 (Auswahlliste im Fließtext) und Punkt 4 (mehrdeutige Zeitformen).
- [B-463](B-463-ki-lehrplan-deckt-ein-sechstel-der-wortliste.md) stellt die größere Frage: Soll ein Lehrplan
  bei vorhandener Wortliste die Buchübungen der Unit einplanen (Karteikasten, Tagesdosis, beide Richtungen),
  statt neue Zehner-Scheiben zu erzeugen? Das entscheidet dieser Umbau nicht.

## Verlauf

- **2026-11-10** — angelegt und in Arbeit genommen (Nutzerauftrag). Ausgangsbefund: Lehrplan 2 mit 4 Teilen in
  14,5 Minuten, 2 gescheitert, 2 mit Mängeln.
- **2026-11-10** — gebaut. Belege:
  - **Tests:** 103/103 `CreatorAgentTests`, die ganze Suite 1227/1227 (Release).
  - **Rote Proben:** 28 Mutanten, jede rot an ihrem eigenen Test.
  - **Review:** `pugling-reviewer` fand keinen Blocker. Seine vier Funde sind behoben: eigene KI-Übungen in der
    Wortliste, zu kurze Liste, wiederholter Schwerpunkt, zwei zu weite Testbehauptungen.
  - **Live-Läufe** mit demselben Befehl (`qwen2.5:14b-instruct`, Kontext 8192 statt 32768, Modell 10 statt 15 GB):
    - Lehrplan 3: 3 von 4 Teilen in 10,2 Minuten.
    - Lehrplan 4: 4 von 4 Teilen in 12,1 Minuten. Beide Lückentexte gingen beim ersten Versuch durch, beide
      Reparatur-Runden lagen an vergebenen Titeln, die seitdem nummeriert werden. Im Trockenlauf danach kam
      „Unit 1 · Wörter (2)“ ohne Reparatur-Runde.
  - **Rohdaten:** In `laden.py` wurde die Stoffnotiz zum Wortschatz von einer Wortliste (1538 Zeichen) auf
    Wortfelder und Stolpersteine (872 Zeichen) umgestellt.
- **2026-11-10** — abgenommen in `50e9b07`. Rollengang: drei Lehrplan-Läufe aus dem Creator-Sitz gegen die laufende App (Lehrpläne 2–4) und ein
  Vokabel-Trockenlauf. Im Vater-Web hat diese Sitzung Lehrplan 4 **nicht** angesehen. Das Nachschauen dort
  bleibt offen, ebenso der Prüfgang einer anderen Sitzung (B-462 bis B-470), der den Zwischenstand gesehen hat.
