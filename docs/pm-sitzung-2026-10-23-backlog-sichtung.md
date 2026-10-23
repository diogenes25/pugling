---
tags: [typ/protokoll, bereich/pm]
aliases: [Backlog-Sichtung 2026-10-23, Nachtlauf-Arbeitspakete]
---

# PM-Sitzung: Backlog-Sichtung 2026-10-23 — bereinigen und in Nachtlauf-Pakete schneiden

**Datum:** 2026-10-23  ·  **Moderation:** PM (Auftrag des Nutzers: „Analysiere das Backlog, bereinige die
Task ggf. falls sie sich ersetzen, widersprechen oder nicht mehr zum Produkt passen. Erstelle
Arbeitspakete der Task, die in Nachtläufen umgesetzt werden können.")
**Ausgangslage:** HEAD `b716271`, Index **70 offen**.

Dieses Protokoll ist eine **datierte Momentaufnahme mit Story-Ids**, wie `pm-loop` sie vorsieht – kein
zweites Sprint-Backlog. Der Zustand jeder Story steht in ihrer Datei; hier steht nur, **welche Stories
zusammen einen Nachtlauf-Sprint ergeben** und **welche Entscheidung vorher fällt**. Veraltet die Liste,
gewinnt die Story-Datei.

## Befund

**1. Die Nachtläufe arbeiten überwiegend ihre eigene Nachschau ab.** Seit dem 2026-08-15 nennen 72 Commits
40 Story-Ids. Darunter sind nur vier ältere Produkt-Stories (B-19, B-45, B-46, B-50). Der Rest sind
Defekte, die ein Review oder die Nachschau des Vorsprints gefunden hat. Das ist nicht falsch, aber es hat
einen Preis: die **zwanzig geschätzten `Wunsch`-Stories**, die seit Freigabe 8 nachts baubar wären, blieben
liegen. Die Pakete unten setzen sie darum bewusst nach vorn.

**2. Die geschätzten Stories waren 2,5 Monate nicht gegen den Code geprüft.** Fast alle stammen vom
2026-08-04 bis 2026-08-13, also aus der Zeit vor oder während B-106 und vor rund zwanzig Nachtlauf-Sprints.
Ein Nachtlauf baut eine geschätzte Story ohne Rückfrage. Passen ihre Entscheidungen nicht mehr zum Code,
baut er das Falsche. Darum ist jede offene Story gegen HEAD geprüft worden, in sechs parallelen
Lese-Durchgängen mit `Datei:Zeile`-Belegen. Das Ergebnis steht als Verlaufszeile „Nachprüfung in der
Backlog-Sichtung 2026-10-23" in der jeweiligen Story, mit den korrigierten Fundstellen.

**3. Ergebnis der Prüfung:**

| Verdikt | Stories |
| --- | --- |
| gilt, nachtlauffähig | B-12, B-15, B-17, B-21, B-28, B-36, B-71, B-155 |
| gilt, Ist-Stand korrigiert, danach nachtlauffähig | B-22, B-35, B-64, B-68, B-05 (ohne neue Bibliothek) |
| gilt, aber zu groß: Teilen ist Schritt 0 | B-85, B-39 (B-39 braucht zusätzlich eine Entscheidung) |
| **Entscheidungen tragen nicht mehr → zurück auf `ausformuliert`** | B-09, B-16, B-20, B-23 |
| **verworfen** (gegenstandslos) | B-86, B-226 |
| wartet auf Zutun von außen | B-07, B-47 (Deploy stillgelegt), B-31 (Handy), B-23 (zweiter Plugin-Autor), B-164 (Entscheidung) |
| Defekt/Aufräumen, besteht, autonom grillbar | B-134, B-138, B-145, B-147, B-156, B-177, B-181 (Doku-Teil), B-214, B-236, B-239, B-245 |
| besteht, braucht vorher eine menschliche Entscheidung | alle übrigen `idee`/`ausformuliert` – siehe Grill-Tagesordnung |

## Bereinigung (in dieser Sitzung durchgeführt)

### Verworfen

- **[B-86](backlog/B-86-uebungstyp-manifest-anzeigenamen-schluessel.md)** – die Prämisse „das Manifest liefert
  nur Daten, keinen Schlüssel" stimmt nicht. `ExerciseTypeManifest(string Type, string Label, …)` trägt mit
  `type` bereits den stabilen Schlüssel (`ExerciseTypeManifest.cs:33,43`), und das Frontend sucht schon
  darüber nach (`exerciseTypes.ts:76`). Der Rest, eine Übersetzungstabelle im Frontend, ist in
  [B-85](backlog/B-85-i18n-infrastruktur-sohn-arcade-englisch.md) vermerkt.
- **[B-226](backlog/B-226-zwei-muenzstaende-im-shop-bildschirm.md)** – am Code widerlegt. Beide Münz-Chips
  rendern dieselbe Context-Variable `coins` aus einer Quelle (`SohnApp.tsx:64,96`, `SohnShop.tsx:29,152`).

### Zurück auf `ausformuliert` – Widerspruch zum Code oder zu einer späteren Entscheidung

- **[B-09](backlog/B-09-lehrer-hausaufgaben.md)** behauptet, es gebe keinen Guard „ein aktiver Plan je Kind".
  `StudyPlansController.cs:102-116` setzt genau diese Anti-Cheat-Invariante seit dem 2026-07-05 durch. Der
  Angriffsplan (ein eigener Hausaufgaben-Plan je Kind) würde den Plan des Vaters abschalten. Dazu kommen
  „Kapitel", das es seit B-106 nicht mehr gibt, und B-46/B-18 als angeblich ungebaut. Faktisch XL; der
  Teilungsvorschlag steht in der Story.
- **[B-20](backlog/B-20-ki-supervisor-agent.md)** verlangt ein zweites Konsolenprojekt, das
  [B-19](backlog/B-19-schuelerprofil-ki-lehrplan.md) in seiner Entscheidung 2 ausdrücklich verworfen hat. Das
  Verb `plan` existiert inzwischen im Creator-Agenten.
- **[B-16](backlog/B-16-positions-formular-umfang.md)** – Entscheidung 4 (Stufe als freies Zahlenfeld) ist
  durch B-79 überholt; der Server validiert `Stage` jetzt.
- **[B-23](backlog/B-23-uebungstyp-plugins-dll.md)** – Entscheidung 2 (SDK ohne `Pugling.Api`) und
  Entscheidung 3 (das Plugin bringt einen `ExerciseControllerBase`-Ableger mit) schließen sich aus. Die
  Story trägt jetzt `wartet_auf` mit dem Gate aus Entscheidung 7.

### Korrigiert, ohne die Stufe zu ändern

- In 52 offenen Stories steht eine Zeile „Nachprüfung" mit den heutigen Fundstellen und den überholten
  Behauptungen. Besonders wichtig vor dem Bau: **B-35** (ShowBoth hat seit B-96 keine Rückseite mehr),
  **B-68** (es gibt neun `overflowX`-Wrapper, die die damalige Suche übersehen hat), **B-22** (`Topics` ist
  seit B-63/B-130 eine Liste mit Server-Grenze), **B-64** (der Blocker B-63 ist erledigt), **B-04** (der
  Nebeneffekt auf B-02 ist umgekehrt), **B-220** („überfällig" gibt es für Pflichtziele nicht) und
  **B-252** (Vokabeln haben schon Aussprache-Audio).
- **B-138 ↔ B-147** widersprachen sich in der Frage, ob eine leere `///`-Zeile einen Absatz ergibt.
  Antwort: in Schema-Beschreibungen ja, in Operation-Summaries nein. Beide Stories sagen das jetzt und
  gehören in einen Sprint.
- **B-147** stand auf `idee`, obwohl die Datei alle Abschnitte von `ausformuliert` trug. Die Stufe ist
  nachgezogen. **B-181, B-201, B-208** haben die fehlenden Abschnitte bekommen, die der Index als
  „Stufe behauptet, Datei belegt nicht" gemeldet hatte.
- **B-47** nannte als Gate das verworfene B-33 und trägt jetzt `wartet_auf` (Neubau des Deploys).
- **Nebenbei korrigiert:** `docs/knowunity-vergleich-plan.md:40` führte B-11 als offen, B-11 ist aber
  abgenommen. Die zwei Kommentare in `Models/AdminEntities.cs` zeigten auf die doppelt vergebene Id B-216
  statt auf B-230.

### Bewusst nicht zusammengelegt

Die beiden Vergleichs-Sammelstories (B-217 Khan, B-247 Knowunity) überschneiden sich mit älteren Stories:
B-218 mit B-04, B-220 mit B-250 und B-248, B-221 mit B-36, B-222 mit B-253. Keine davon ist eine Dublette.
Jede trägt eine eigene Entscheidung, und ein Zusammenlegen wäre selbst eine Produktentscheidung. Die
Überschneidungen sind darum als **gemeinsame Grill-Sitzung** geschnitten (unten), nicht als Merge.

## Arbeitspakete für Nachtläufe

Jedes Paket ist **ein Sprint** im Sinn von `pm-loop` („The Sprint"): ein roter Faden, höchstens sechs
Stories, ein Sprint-Ziel aus der Sicht einer Rolle. Alle Stories darin sind entweder `geschaetzt` (baubar
nach Freigabe 8) oder `Defekt`/`Aufräumen` (autonom grillbar nach Freigabe 1). Ein Paket nennt nur, was die
Story-Dateien nicht ohnehin sagen: den Zusammenhang, die Reihenfolge und den Stolperstein.

**Regeln für alle Pakete:**

- Je Sprint höchstens **eine** Migration, denn die Kette wird neu gefaltet. Pakete mit `migration: ja`
  laufen darum allein (NP-8, NP-9, NP-10).
- Ein Paket, dessen Story an einer Entscheidung hängt, die ihre `## Entscheidungen` nicht hergeben, hält
  dort an (Freigabe 8) und schreibt die Frage ins Protokoll. Die bekannten Fälle sind unten benannt.
- Die „Nachprüfung"-Zeile jeder Story ist Pflichtlektüre vor dem Bau. Sie ersetzt veraltete Zeilenangaben
  und streicht überholte Annahmen.

| Paket | Sprint-Ziel (aus Rollensicht) | Stories | Größe | Stolperstein |
| --- | --- | --- | --- | --- |
| **NP-1** Erfolge spürbar machen | *Der Sohn sieht und hört, wenn er ein Teilziel erreicht, und eine Karte dreht sich beim Aufdecken um.* | B-36, B-35 | 2 × S, frontend | B-35 Entscheidung 3 gilt nur noch für SelfAssess; die Klickkette aus B-109 im E2E festhalten. Wie sich die Animation anfühlt, sieht am Ende ein Mensch an (`wartet_auf`). |
| **NP-2** Creator prüft, was er baut | *Der Creator kann jede Übung vor dem Zuweisen sinnvoll ausprobieren, und der Agent erzeugt Förderübungen nur in der richtigen Sprache.* | B-15, B-17, B-21 | S + XS + S, beides | B-21: Testfall auch für das Verb `plan`. B-17: die Entscheidungen hat ein Agent getroffen; bei Zweifel anhalten. |
| **NP-3** Login und Konto härten | *Der Vater kann kein Konto mit leerer oder zu kurzer PIN anlegen, und ein gelöschtes Konto bekommt 401 statt 500.* | B-28, B-245 | S + S, backend | B-245 zuerst **messen** (Zu klären 1). Den Leer-Guard aus B-28 zentral in `PinHasher.Verify` setzen, damit er B-230 übersteht. |
| **NP-4** Der Katalog-Vertrag liest sich sauber | *Wer die API liest, findet jede Beschreibung als Fließtext, dasselbe Eigentums-Flag überall unter einem Namen und nach dem Umbenennen eines Fachs überall den neuen Namen.* | B-147, B-138, B-156, B-145 | S + M + S + S, backend | B-147 vor B-138 (der Transformer entscheidet, was B-138 noch von Hand tun muss). B-156 ist ein Vertragsbruch: Client, Frontend (`CatalogAdmin.tsx:244`) und vier Fixtures ziehen mit. |
| **NP-5** Vater-Web am Tablet und mit Screenreader | *Der Vater bedient jede Seite bei 768 px ohne seitliches Scrollen, und ein Screenreader sagt jede Rückmeldung und jede Kästchen-Gruppe an.* | B-68, B-236, B-134 | M + S + M, frontend | B-68: die neun vorhandenen `overflowX`-Wrapper einbeziehen; der Spec setzt die Breite selbst. B-134 bringt einen Wächter mit Ausnahmeliste. |
| **NP-6** Lehrwerk und Vokabeln aus einer Hand | *Der Vater wählt das Lehrbuch seines Kindes aus dem Katalog statt es frei zu tippen, und der Creator legt gleichwertige Übersetzungen direkt im Übungs-Editor an.* | B-64, B-71 | M + M, beides | B-64: die Wortfrage „Lehrwerk/Reihe/Buchreihe" aus B-123 **auslassen**, sie ist offen. B-71 AK 6 (liest der Test `TranslationAlternatives`?) vorher prüfen. |
| **NP-7** Neue Übungsstufe: Buchstaben tauschen | *Der Sohn kann ein Wort aus vertauschten Buchstaben zusammensetzen, per Klick oder Tastatur.* | B-05 | M, beides | Keine neue Drag-&-Drop-Bibliothek, denn die Story entscheidet keine. Den Misch-Seed aus der Sitzungs-Id ableiten, dann bleibt es ohne Migration. |
| **NP-8** Gute Übungen finden | *Der Vater sieht beim Zusammenstellen, welche geteilten Übungen andere gut bewertet haben.* | B-12 | M, beides, **Migration** | allein im Sprint |
| **NP-9** Grammatik als Thema | *Der Creator ordnet Regelaufgaben einem Grammatik-Thema zu, und der Vater findet sie darüber.* | B-155 | L, beides, **Migration** | allein; der Nutzer hat Teilen ausdrücklich abgelehnt (Entscheidung 11). Die Seed-Liste in Entscheidung 6 endet mit „…" – vervollständigen und im Verlauf nennen. |
| **NP-10** Der Agent füllt die Unit-Notizen | *Der Creator lässt die Stoffnotizen einer Unit vom Agenten vorschlagen, statt sie abzutippen.* | B-22 | S–M, backend, **Migration** | `Topics` ist eine Liste mit 50 × 200 Zeichen; die Migration betrifft nur noch `Grammar`. Nicht im selben Sprint wie NP-9. |
| **NP-11** Mehrsprachigkeit, Schritt 1 | *Der Sohn kann die Arcade auf Englisch umschalten, mindestens ein Bildschirm ist vollständig übersetzt, und alle Specs laufen weiter auf Deutsch.* | B-85 (→ teilen) | L → 2 × M | Schritt 0 ist das Teilen nach `docs/backlog/README.md`, gebaut wird nur Teil a. Die englischen Texte liest am Ende ein Mensch gegen. |
| **NP-12** Werkzeug und Doku sagen die Wahrheit | *Wer eine Anleitung, eine Spec, einen Test-Kommentar oder eine Feldhilfe liest, wird nicht in die Irre geführt.* | B-239, B-214, B-177, B-181 (Doku-Teil), B-242, B-201 | XS–S je Story | B-239 betrifft auch aktive Anweisungen (`.claude/commands/neuer-uebungstyp.md`, `pugling-reviewer.md`). B-177 erst herstellen; gelingt das nicht, `verworfen` mit Messung. Bei B-181 nur der Kommentar, **kein** Wiederholungsversuch in `Load`. |
| **NP-13** Der Vater sieht alle Kinder auf einen Blick | *Der Vater sieht für jedes seiner Kinder in einer Zeile, ob die Pflicht dieser Woche bzw. dieses Monats erfüllt ist, und blättert über die Kinder.* | B-39 (→ teilen) | L → 2 × M, beides | Schritt 0 ist das Teilen (a: Endpunkt mit Integrationstests, b: zwei Einbaustellen im Frontend). Geblättert wird über Kinder, der Zeitraum ist ein Filter (Entscheidung vom 2026-10-23). `ComputeDayAsync` je Kind × Plan × Tag vorher messen. |

### Vorschlag für die Nachtfolge

Die bisherigen Läufe schafften drei bis fünf Sprints je Nacht. Daraus ergibt sich diese Folge. Die
Reihenfolge stellt **Produktwert vor Hygiene**, und jede Nacht mischt ein größeres Paket mit kleinen:

| Nacht | Pakete | Warum so |
| --- | --- | --- |
| 1 | NP-1, NP-2, NP-3 | drei kleine, risikoarme Pakete in drei Rollen; der Sohn bekommt zum ersten Mal seit Wochen etwas Sichtbares |
| 2 | NP-4, NP-12 | Vertrag und Doku, dieselbe Prüfmaschinerie (OpenAPI-Diff, Guard-Tests, Markdown-Lint) |
| 3 | NP-5, NP-6 | zwei Frontend-lastige Pakete, Browser-Rollengang lohnt (Freigabe 6) |
| 4 | NP-8, NP-7 | eine Migration (B-12) plus ein migrationsfreies Paket |
| 5 | NP-9 | L mit Migration, allein |
| 6 | NP-10, NP-11 | NP-11 beginnt mit dem Teilen |
| 7 | NP-13 | nach der Entscheidung zu B-39 baubar; beginnt mit dem Teilen |

Der **Auftragstext** bleibt der aus [nachtlauf.md](nachtlauf.md). Damit ein Lauf ein Paket statt einer
selbst gewählten Auswahl fährt, genügt ein Satz am Ende, zum Beispiel: *„Fahre die Pakete NP-1, NP-2, NP-3
aus docs/pm-sitzung-2026-10-23-backlog-sichtung.md, in dieser Reihenfolge."*

## Grill-Tagesordnung (beim Menschen, nicht im Nachtlauf)

Diese Stories brauchen eine Entscheidung, bevor ein Lauf sie anfassen darf. Sie sind so geschnitten, dass
**eine Sitzung einen Cluster schließt**. Je Cluster steht die Leitfrage da, die die übrigen Fragen
meistens mitentscheidet. Nach der Sitzung kann ein Nachtlauf schätzen und bauen.

| Cluster | Stories | Leitfrage | Gewinn danach |
| --- | --- | --- | --- |
| **G-0 Schnellentscheidungen** (Ja/Nein, je eine Minute) | B-242, B-07, B-17, B-252 | B-242: Nebendatei löschen? · B-07: Gibt es die alte Azure-Instanz noch? (Nein heißt `verworfen`) · B-17: Die Agent-Entscheidungen vom 2026-08-03 bestätigen? · B-252: TTS nur als Rückfall für Vokabeln ohne Aufnahme und nie in der Hörstufe? | B-242 kommt in NP-12, B-17 bleibt in NP-2 |
| **G-1 Punkte-Vorbelegung** | B-186 → B-201 | Gewinnt im Assistenten die gewählte Intensität oder die Empfehlung des Creators? | B-201 wird ein XS-Defekt (NP-12 oder NP-2) |
| **G-2 Rückmeldung, Sperre, Fokus: eine Hausregel** | B-231, B-232, B-233, B-234, B-183, B-164 | Welches Muster gilt seitenweit für Pflichtfeld-Sperren (`disabled` oder `aria-disabled`), für den Meldungsplatz, für den Retry nach einem Lesefehler (`ListenAusfall` als Hausregel?) und für den Leerzustand – und gehört „veraltet" in `useAsync` oder an den Aufrufer? | ein Frontend-Paket mit sechs Defekten; `frontend/CLAUDE.md` bekommt die Regel |
| **G-3 Interessen-Taxonomie und Listengrößen** | B-207, B-208, B-141, B-122 | Wer entscheidet, was ein Kind sehen darf: ein Creator-Kennzeichen „für Kinder sichtbar" oder Eigentum (Migration)? Dazu: bleibt PUT ein PUT, ist ein eindeutiges Label gewollt, bekommt `take` einen Default? | ein Backend-Paket mit einer Migration |
| **G-4 Katalog und Zuweisen** | B-140, B-152, B-205, B-16 | Was sieht der Vater in der Übungsauswahl: „zurückgezogen" oder ein serverseitiges „für dich zuweisbar"? Dazu: darf ein Kind eine Schulart-Kombination tragen, wird `exercise_in_use` aufgespalten, woher kommen die Stufen-Optionen im Positions-Formular? | ein Vater-Paket „Plan bauen ohne Überraschung" |
| **G-5 Leitner beim Kind** | B-04, B-218, B-219, B-223, B-220 | Wird der Übungspool je Position, je Plan oder kind-weit gesteuert? Davon hängen Tagesdeckel (B-04), Mischwiederholung (B-218) und Warteschlange (B-220) ab. Dazu: verzeiht ein Fehler eine Box (B-219), kosten Hinweise Punkte über das vorhandene `HintsUsed` (B-223)? | der Kern des Khan-Plans, sauber geschnitten statt in drei konkurrierenden Sortierlogiken |
| **G-6 Druck und Motivation** | B-222, B-253, B-221 | Wie viel Vergleich und Serie verträgt ein Kind um 11 in einer App, deren Peitsche `PenaltyCoins` ist? „Tages-Streak bleibt" und „kein Geschwister-Vergleich" sind beide zulässige Ausgänge. | wahrscheinlich zwei `verworfen` und eine baubare Badge-Leiter |
| **G-7 Klassenarbeit und Schulwoche** | B-248, B-249, B-250, B-39, B-09 | Ist die Vorbereitung auf eine Klassenarbeit **Pflicht mit Malus** oder **Angebot mit Bonus**, und wer setzt das Fenster? Das ist die Kernidee in Reinform. Dazu: B-39 – blättert das Dashboard über Kinder oder über Perioden? B-09 – wie verträgt sich eine Hausaufgabe mit „ein aktiver Plan je Kind"? | das wertvollste Produkt-Paket des Backlogs; B-39 wird danach sofort baubar |
| **G-8 Agenten und Plattform** | B-20, B-23, B-251, B-230, B-181 (Teil b) | Lohnt B-20 nach B-19 noch, als `assign`-Verb im bestehenden Agenten? Wird B-23 ohne zweiten Plugin-Autor `verworfen`? Dazu: Vision-Modell oder OCR (B-251), welche PIN-Quelle entfällt (B-230), Wiederholungsversuch beim Lesen des Beispielkatalogs (B-181)? | eher Aufräumen des Backlogs als Bau |
| **G-9 Mehrsprachigkeit** | B-90, B-91, B-92 | Ist Mehrsprachigkeit **über die Arcade (B-85) hinaus** ein Ziel einer deutschen Familien-App? Ohne Ja: alle drei `verworfen`. | drei Stories weniger oder ein klarer Fahrplan |

**Empfehlung zum Streichen**, als Vorschlag und nicht entschieden: B-23 (nach eigener Entscheidung 7 erst
mit einem zweiten Plugin-Autor sinnvoll, den eine Familien-App nicht hat), B-20 (zum größten Teil durch
B-19 erledigt), B-90/B-92 (ohne Konsumenten). Das sind Wertentscheidungen. Darum stehen sie hier und sind
nicht ausgeführt.

## Was außerhalb des Repos wartet

- **B-31** – Klang und Haptik an einem echten Android- und iOS-Gerät. Sinnvoll erst **nach NP-1**, weil
  es dann mehr Feiern gibt, die man gegenhören kann.
- **B-07, B-47** – hängen am stillgelegten Azure-Deploy. G-0 klärt, ob B-07 überhaupt noch existiert.

## Nicht angefasst

- Die 17 **abgenommenen** Stories, die der Index unter „Stufe behauptet, Datei belegt nicht" führt
  (fehlende Abschnitte, vor allem „Schätzung"). Sie sind Historie. Abschnitte nachträglich zu erfinden
  wäre schlechter als die Lücke, und keine davon ändert, was als Nächstes gebaut wird.
- Die **unentschiedenen Retro-Vorschläge** der letzten Nachtläufe
  ([2026-10-17](pm-sitzung-2026-10-17-nacht.md), [2026-10-23](pm-sitzung-2026-10-23-nacht.md)). Sie sind
  Prozessentscheidungen und kein Backlog-Inhalt. Sie gehören in eine eigene Runde mit dem Nutzer und sind
  hier weder gezählt noch bewertet.

## Grill-Runden mit dem Nutzer

### G-0 Schnellentscheidungen – entschieden

| Story | Entscheidung | Folge |
| --- | --- | --- |
| B-242 | Nebendatei **löschen**, Git-Historie nicht tilgen | in NP-12 aufgenommen |
| B-07 | Das Deployment wird **neu gebaut**, es gibt keine Alt-DB | `verworfen` (gegenstandslos); B-47 wartet allein auf den Neubau |
| B-17 | Die Agent-Entscheidungen vom 2026-08-03 gelten als bestätigt | bleibt in NP-2, ohne Vorbehalt |
| B-252 | TTS **nur als Rückfall** für Vokabeln ohne Aufnahme, **nie** in der Hörstufe | Rahmen fürs Ausformulieren; bleibt `idee` |

### G-1, G-9 und zwei Streichkandidaten – entschieden

| Story | Entscheidung | Folge |
| --- | --- | --- |
| B-186 | Im Assistenten **gewinnt die Intensität** | `verworfen` („bleibt wie es ist“); B-201 ist entsperrt und kommt in NP-12 |
| B-90, B-91, B-92 | Mehrsprachigkeit nur für die Arcade (B-85) | alle drei `verworfen`; B-85 bleibt NP-11 |
| B-23 | Keine externen Plugin-Autoren | `verworfen` |
| B-20 | Als **`assign`-Verb** im bestehenden Agenten neu schneiden, kein zweites Projekt | bleibt `ausformuliert`; drei Detailfragen stehen in der Story (doppelte Position, `--tag`, Positions-Vorgaben), danach schätzbar |

Offen auf der Tagesordnung bleiben damit G-2 bis G-8 (ohne die erledigten Teile von G-8: B-20, B-23).

### G-7 Klassenarbeit und Schulwoche – teilweise entschieden

| Story | Entscheidung | Folge |
| --- | --- | --- |
| B-248 | Vorbereitung ist **Pflicht mit Malus**; das Fenster hat einen **festen Default, der Vater kann es je Arbeit ändern** | bleibt `idee`; als Nächstes ausformulieren (wie entsteht die Zusatzpflicht, Höhe des Malus, Bündel-Auflösung) |
| B-39 | Geblättert wird **über Kinder**, der Zeitraum ist ein Filter | nachtlauffähig → neues Paket **NP-13** |
| B-09 | Hausaufgabe landet **als Positionen mit Frist im aktiven Plan** | bleibt `ausformuliert`; offen: Auth-Wand für den Lehrer, „erledigt", Kind ohne aktiven Plan, Interessen, Teilung |

Noch offen in G-7: B-249 (Notenschlüssel der Probeklausur) und B-250 (Stundenplan – nur Reihenfolge oder auch Pflicht?).
