---
tags: [typ/protokoll, bereich/pm]
aliases: [Vorbereitungslauf 2026-11-19]
---

# Vorbereitungslauf 2026-11-19 (Sonnet 5.5)

Unbeaufsichtigter Lauf nach [vorbereitungslauf.md](vorbereitungslauf.md). Gebaut wurde nichts: Es änderten sich nur
Dateien unter `docs/backlog/` und dieses Protokoll. Commits: `76cdf8d2` (Runde 1), `58474adc` (Runde 2),
`d6a0b3d0` (Runde 3), dazu der Protokoll-Commit. Nichts gepusht.

**Verfahren:** Drei Runden, eine Stufe je Story und Runde. Runde 1 `idee` → `ausformuliert`, Runde 2 → `gegrillt`,
Runde 3 → `geschaetzt`. Die Recherche und das Schreiben liefen über Sonnet-Teilagenten mit festem Dateiumfang; Index,
Lint und Commits habe ich selbst gemacht. Der Markdown-Lint (`npx markdownlint-cli2`) ist am Ende grün (0 Befunde).

**Nicht angefasst:** die P3-Wünsche (B-221, B-369, B-587, B-590, B-594, B-600, B-601, B-617, B-641, B-642, B-684),
B-09, B-223, B-377, B-649, B-31, B-618, B-624 bis B-626, B-631, B-647, B-654, B-580, B-621, B-622, B-630, B-639 (alle
schon `geschaetzt` oder P3) – keine Zeit mehr bzw. nicht dran. B-644 blieb wie vorgegeben `ausformuliert`.

## Tabelle

Unsicherheit: niedrig, mittel oder hoch.

| Id | Titel | Stufe | Kern der Entscheidungen | Unsicherheit |
| --- | --- | --- | --- | --- |
| B-672 | Zurückverlegter Planstart holt unspielbare Tage zurück | ausformuliert → geschaetzt (S) | `ActivatedAt = max(ActivatedAt, min(alter Start, heute))` nur bei aktivem Plan; gebuchter Malus bleibt | niedrig, Formel am Code durchgerechnet |
| B-685 | „Später“-Test sperrt Rückmeldung im Üben | idee → geschaetzt (S) | Server-Sperre `feedback_during_test` entfällt samt Fehlercode (Weg d) | mittel, nimmt B-623 AK 4 und B-628 AK 3 zurück |
| B-653 | „Nur neue Inhalte“ verliert offene Lernkarte | idee → geschaetzt (S) | `New` lässt offene Lernkarten durch, Prädikat von `OpenLearnCards` | mittel, AK 4 ungemessen |
| B-614 | Startseite gruppiert nach Übungsfach statt Planfach | idee → geschaetzt (M) | `SubjectName` meint künftig das Planfach, Rückfall auf Reihe | niedrig |
| B-607 | Hinweis auf Schreibstufen | idee → geschaetzt (S) | nur Erklärtext (ⓘ) im Editor, kein Schreib-Hinweisfeld | niedrig |
| B-608 | Bonus-Kopf nennt zu hohen Satz | idee → geschaetzt (M) | Satz je Karte vom Server (`CoinsPerHit`), Übersicht „bis zu N“ | mittel, berührt Abrechnungspfad |
| B-671 | Sonderzeichen im falschen Kästchen (Bonus) | idee → geschaetzt (S) | Fix nur in `SohnBonusRun` (Fokusregel wie Üben/Test) | mittel, Handy-Tastatur unprüfbar |
| B-673 | Umgehängter Plan erbt Vergangenheit | idee → geschaetzt (M) | gespielter Plan: `409 plan_has_data`; ungespielt aktiv: `ActivatedAt = heute` | mittel, verbietet einen heute möglichen Weg |
| B-609 | Truhe rechnet den Tag doppelt | idee → geschaetzt (M) | zuerst messen, tote Schlussrechnung weg, Verlaufsschleifen nicht mitnehmen | mittel, Schwelle ungemessen |
| B-602 | `feldhilfe.spec.ts` flackert | idee → geschaetzt (S) | nur Spec reparieren, auf Radiogruppe warten, Dauerfall mit verzögerter Antwort | niedrig bis mittel |
| B-603 | Fokus-Vitests flackern | idee → geschaetzt (M) | 24 positive Fokus-Zusicherungen auf `waitFor`, 3 verneinende bleiben | mittel, fremde Änderungen an `sohn/*` |
| B-605 | Playwright-Worker hängt | idee → geschaetzt (S) | `PWTEST_CHILD_PROCESS_TIMEOUT` 45 s; Hänger mitten im Lauf bleibt offen | mittel |
| B-670 | Sonderzeichen-Leiste folgt der Reihe | idee → ausformuliert | beim Grillen als am Code nicht vorkommend erkannt, Stufe bleibt | mittel, nicht an der laufenden App geprüft |
| B-659 | Pflicht wird Kür bricht die Serie | idee → ausformuliert | nicht gegrillt: berührt Malus-Regeln; Frage mit Empfehlung notiert | mittel |
| B-604 | Sprachübungen fragen nur Sprache | idee → geschaetzt (M) | Regel als Text in Skill, Prompt, Editor-Hilfe, kein Server-Check | mittel |
| B-638 | Formeln in Aufgabentext anzeigen | idee → geschaetzt (L) | KaTeX, nur `$…$`, eine Komponente `MathText` | mittel bis hoch, Bundle ungemessen |
| B-640 | Gleichung lösen | idee → verworfen (geteilt) | in B-710, B-711, B-712 zerlegt | hoch, siehe unten |
| B-710 | Gleichung lösen: Typ, Löser, Editor | neu → geschaetzt (L) | Mengen-Wertung, Eingabe `2;4` | mittel |
| B-711 | Lösungsmenge auswählen | neu → geschaetzt (M) | Auswahlstufe mit berechneten Fehlermodellen | niedrig bis mittel |
| B-712 | Sohn-Eingabe der Lösungsmenge | neu → geschaetzt (M) | getrennte Felder plus Knopf, neues Kartenfeld `setAnswer` | hoch bis mittel |
| B-596 | Vokabelübung 5 bis 10 Vokabeln | idee → geschaetzt (L) | „darf nicht wachsen“ statt „muss ≤ 10 sein“; Untergrenze nur beim Zuweisen | mittel |
| B-650 | Vokabel im Satz (Birkenbihl-Karte) | idee → geschaetzt (L) | Cloze mit `Gap.Decoding`, Lernwort maskieren | hoch, siehe unten |
| B-586 | Kind fragt den Betreuer | idee → geschaetzt (M) | `ExerciseFeedback` um Antwort erweitern, Migration; Z1 bis Z3 zurückgestellt | mittel |
| B-220 | Learning Queue | idee → geschaetzt (L) | eigene Route `queue`, `DueBy` als Datum, ergänzt die Tagessicht | mittel |
| B-648 | Birkenbihl: jedes Wort eine Vokabel | gegrillt → geschaetzt (L) | Grundform-Liste, Bestätigungsfeld, Entscheidungen 10 bis 12 nachgetragen | mittel |
| B-656 | KI-Creator: Übung aus Hausaufgabe | gegrillt → geschaetzt (M) | Entscheidungen 10 bis 13 nachgetragen (verpasst = offen, 14/30 Tage) | mittel |
| B-619 | Fächer-Verteilung: Regler und Sohn-Ansicht | gegrillt → geschaetzt (M) | AKs umgeschrieben; hängt an B-618 und B-644 | mittel bis hoch |
| B-655 | Vater-Web: WebUntis-Zugang | gegrillt → geschaetzt (M) | AK in fünf zerlegt; Abruf auf Knopfdruck | niedrig bis mittel |
| B-536 | Aufbau-Übungen | gegrillt → verworfen (geteilt) | in B-700 bis B-704 zerlegt | siehe unten |
| B-700 | Aufbau-Stufe am Katalog | neu → geschaetzt (M) | `BuildLevel`, `BuildTargetExerciseId`, Migration | mittel |
| B-701 | Position spielt die Aufbau-Stufe | neu → geschaetzt (L) | Aufstieg nach bestandenem Test, Migration | mittel |
| B-702 | Abstieg und Pflichtziel der Stufe | neu → gegrillt | Malus- und Münzfragen nicht entschieden, `wartet_auf` Nutzer | – |
| B-703 | KI-Creator erzeugt Vorstufen | neu → gegrillt | Rollenschnitt nicht entschieden, `wartet_auf` Nutzer | – |
| B-704 | Aufbau-Weg in der Oberfläche | neu → geschaetzt (M) | Vorstufen im Editor, Stufenband beim Kind | mittel |
| B-680 | Täglicher Lehrplan je Fach | idee → ausformuliert | nicht gegrillt (Leitbild Privatlehrer) | – |
| B-681 | Rollenschnitt Vater/Lehrer | idee → ausformuliert | nicht gegrillt | – |
| B-682 | Notenstufe je Fach | idee → ausformuliert | nicht gegrillt | – |
| B-683 | Klausur eicht die Stufe | idee → ausformuliert | nicht gegrillt | – |
| B-664 | Diktat und Einfügen erkennen | idee → ausformuliert | großenteils schon gebaut (`kopierSperre.ts`) | – |
| B-663 | Profilwahl Vater oder Creator | idee → ausformuliert | Rollen berührt, nicht gegrillt | – |
| B-662 | Vater-Sicht: Arbeit, Tagesziel, Käufe | idee → ausformuliert | Rollenschnitt, nicht gegrillt | – |
| B-660 | Tagesaufgaben beliebig oft, Münzen sinken | idee → ausformuliert | Münz-Ökonomie, nicht gegrillt | – |
| B-592 | Erinnerung vor Pflicht-Frist | idee → ausformuliert | nicht gegrillt | – |
| B-593 | Sparziel im Familien-Shop | idee → ausformuliert | Münz-Ökonomie, nicht gegrillt | – |
| B-627 | Berechtigte Meldung bucht nach | idee → ausformuliert | Münz- und Malus-Regeln, nicht gegrillt | – |
| B-687 | Hausaufgabe mit Foto-Nachweis | idee → ausformuliert | Malus-Höhe offen, nicht gegrillt | – |
| B-218 | Mischwiederholung über alle Positionen | idee → ausformuliert | Münz-Ökonomie, nicht gegrillt | – |
| B-248 | Klassenarbeit-Countdown | idee → ausformuliert | Malus-Regeln, nicht gegrillt | – |
| B-249 | Probeklausur mit Notenprognose | idee → ausformuliert | nicht gegrillt | – |

Die Zeile B-644 fehlt absichtlich: unverändert `ausformuliert`.

## Zum Bestätigen

Sortiert nach Unsicherheit, hoch zuerst. Bei allen Wünschen (B-604, B-638, B-710 bis B-712, B-596, B-650, B-586, B-220,
B-648, B-656, B-619, B-655, B-700, B-701, B-704) steht `wartet_auf: Bestätigung der Sonnet-Entscheidungen`. Bei
Defekten und Aufräumen gibt es das Feld nicht.

1. **B-650 (hoch).** Entscheidung 2: Der Kopftext der Story („alle Wörter außer der Zielvokabel übersetzt“) widerspricht
   dem Beispiel („Tom rennt heim“). Der Agent hat dem Beispiel gefolgt, also das Lernwort maskiert und die Glosse
   behalten. Entscheidung 8: Die Satzkarte füttert den wortgebundenen Lernstand nicht, die „Schwach“-Liste hebt sich
   durch sie also nicht. Schätzung: `Gap.Decoding` hängt je Lücke, der Satz gilt aber für alle; vielleicht gehört das
   Feld an `ClozeConfig`.
2. **B-640 / B-710 bis B-712 (hoch).** Die Stufenleiter wurde von vier auf zwei Stufen verkleinert, weil der
   Typ-Vertrag kein anderes Soll je Stufe trägt. „Anzahl bestimmen“ und „Verfahren wählen“ sind zurückgestellt. Brüche
   sind nicht zugelassen, obwohl die Story-Idee sie nannte. B-712 braucht ein neues additives Kartenfeld `setAnswer`;
   gibt es zwei Karten-Projektionen (Üben, Test), wird es größer.
3. **B-619 (mittel bis hoch).** Drei Annahmen stellvertretend: anteilige Regler-Umrechnung, Vater-Web-Ort Reiter
   „Schule“, Sohn-Block im Lernstand. Hängt an B-618 (ungebaut, Feldnamen unbekannt) und kann von B-644 überholt
   werden.
4. **B-685 (mittel).** Nimmt eine abgenommene Server-Regel zurück (B-623 AK 4, B-628 AK 3). Rückfall: Weg (b), wenn
   der Vater eine Server-Sperre will.
5. **B-673 (mittel).** `409 plan_has_data` verbietet einen heute möglichen Vater-Weg. Ich habe das nicht als neue
   Malus-Regel gewertet; es verhindert nur, dass Malus mitwandert.
6. **B-596 (mittel).** Weak-Word-Review bleibt bei 25 als Ausnahme; `AllowShortList` ist ein neues Vertragsfeld;
   `laden.py` teilt automatisch. Der Bau ist größer als gedacht: `CreateVocabExerciseAsync` legt standardmäßig zwei
   Wörter an, 52 Testdateien laufen gegen die neue Untergrenze 5.
7. **B-656 (mittel).** Am ehesten anders: Entscheidung 10 (verpasste Aufgaben zählen als offen) und 11 (Zeitraum
   14 Tage zurück, 30 voraus).
8. **B-648 (mittel).** Am ehesten anders: Entscheidung 10, der Umfang der Grundform-Erkennung (Liste unregelmäßiger
   Formen plus wenige Endungsregeln, kein LLM).
9. **B-586 (mittel).** Entscheidung 3 (PATCH-Erweiterung, erste Antwort gilt für alle Betreuer, sonst `409`) und 4
   (Text erst nach dem Test, also erst beim nächsten Öffnen der Startseite). Z1 bis Z3 stehen zurückgestellt.
10. **B-220 (mittel).** „Nur Anzeige“ habe ich so gelesen, dass ich trotz Malus-Bezug entscheiden darf, solange die
    Queue nur liest. Der Nutzer legte am 2026-10-23 „erst nach B-218“ fest; Z1 fragt, ob die erste Fassung davor
    gebaut werden darf (Empfehlung: ja). Entscheidung 2: UTC-Tag statt lokaler Uhrzeit.
11. **B-638 (mittel bis hoch).** Vorschauzeile im Editor ist aufgenommen (macht die Story größer), Lücke in einer
    Formel entfällt. Die Testumgebung ist `happy-dom`, nicht jsdom; ob `katex.render` dort läuft, ist nicht erprobt.
    `frontend/package.json` trägt fremde ungecommittete Änderungen.
12. **B-604 (mittel).** `Translation` behält Sätze als Antwort; der Bestand wird nur gelistet, nicht umgeschrieben.
13. **B-608 (mittel).** Entscheidung 3: Übersicht sagt „bis zu 3“ bei gemischten Sätzen. Das Frontmatter sagt
    `vertragsbruch: nein`, weil die Felder additiv sind; die Entscheidung 1 meint den Nachzieh-Aufwand.
14. **B-671 (mittel).** Entscheidung 1 weicht von der Empfehlung der Ausformulierung ab: Fokus in `SohnBonusRun`
    statt Reset in `LetterBoxes`.
15. **B-609, B-603, B-605, B-653, B-614 (mittel), B-607, B-602, B-672 (niedrig).** Entscheidungen klein und am Code
    belegt; die Zahlen (20-%-Schwelle, 45 s, „fünf volle Läufe“) sind Setzungen, keine Messungen.

## Nicht entschieden

Fragen aus Freigabe 2 und weitere, je mit Empfehlung des Agenten:

1. **Leitbild Privatlehrer (B-680, B-681, B-682, B-683, B-644).** B-680 überschneidet sich fast ganz mit B-644.
   Empfehlung: B-680 wird die Produktseite von B-644 (bleibt Karte), nicht eine zweite Planer-Story. Regeln in C# statt
   LLM; Vater behält Notbremse (Pause, Streichen), aber keine Mengen mehr; Boden und Deckel werden Voreinstellungen;
   Münzen bleiben an Tagespflicht und Klausur, die Notenstufe zunächst ohne Münzwert. B-680 Punkt 3 und B-681 Punkt 4
   sind dieselbe Frage und gehören einmal entschieden, ebenso wer die Note einträgt (B-681, B-683, B-684).
2. **B-702 (Malus und Pflichtziel der Aufbau-Stufe).** Empfehlung: Abstieg bucht nichts, ein verpasster Zeitraum
   kostet weiter `PenaltyCoins`. Offen ist, ob ein Test der Vorstufe weniger Zielpunkte zahlt, sonst umgeht ein Kind den
   Stick mit der leichtesten Stufe.
3. **B-703 (Rollenschnitt).** Empfehlung: Das Verb legt Entwürfe ohne Zuweisung an; ein Freigabeschritt hängt vom
   Rollenschnitt ab. Zusätzlich ein Modus ohne LLM, der vorhandene Übungen verknüpft.
4. **B-659 (Pflicht wird Kür).** Soll die frühere Pflicht für die Abrechnung offener Perioden zählen? Empfehlung:
   nein, Malus unverändert. Historie als zwei nullable Spalten plus Löschsperre für Pläne mit Spieldaten.
5. **B-660, B-593, B-627, B-687, B-218, B-248.** Münz- und Malus-Fragen stehen je als offene Punkte mit Empfehlung in
   den Stories (z. B. B-660: kein zweiter Münzweg, Stufung beibehalten; B-593: Anzeige ohne Sparbonus; B-627: nur die
   gemeldete Antwort, Gegenbuchung statt Löschen; B-687: Malus-Höhe als Familien-Einstellung).
6. **B-662 und B-663.** Brauchen eine gemeinsame Grill-Runde; B-663 legt die aktive Rolle fest, B-662 füllt die
   Betreuen-Startseite, die Telefonbreite ist in beiden dieselbe Frage.
7. **B-684 und B-687** teilen das ungelöste Problem eines authentifizierten Ablagewegs für Kinderfotos. Der Medienpfad
   ist Creator-only und wird öffentlich ohne Login ausgeliefert (`Program.cs:598-607`), taugt also nicht.

## Vorschläge

Verworfen oder erledigt sind sie nicht; das entscheidet der Nutzer.

1. **B-670** als `verworfen` (Grund B-597): Der Fall tritt am Code nicht auf, die Leiste steht nur unter Freitext und
   Kästchen und spielt die gelernte Sprache. Einziger echter Rest ist AK 5 (`fr-FR` am Bindestrich abschneiden), eine
   mögliche XS-Frontend-Story.
2. **B-664** weitgehend erledigt (`frontend/src/sohn/kopierSperre.ts:24-51`, Commit `0f8f0328`). Erst auf Friedrichs
   Gerät messen, ob Diktat noch durchkommt; dann `abgenommen` mit Restlücken-Story oder `verworfen`.
3. **B-605:** Hänger nicht reproduzierbar (ein Einzelfall vom 16.11.). Verwerfen erst, wenn fünf volle Läufe ohne
   Wiederkehr vorliegen; die Frist-Maßnahme ist davon unabhängig.
4. **B-640** hat noch `wartet_auf` im Frontmatter, obwohl `verworfen`; leeren, wenn der Index es stört.
5. **Ids:** B-536 ist nach B-700 bis B-704, B-640 nach B-710 bis B-712 geteilt. B-688 bis B-699 und B-705 bis B-709
   sind bewusst frei geblieben (Id-Bereiche für parallele Agenten). Ids werden nie neu vergeben.
6. **Widersprüche zwischen Stories:** keiner. B-586 zitiert `TestRunningAsync` und `feedback_during_test`, die B-685
   entfernt; B-586 ist vor dem Bau neu gegen den Code zu lesen. B-648 und B-650 sowie B-650 und B-596 berühren
   dieselben Dateien und sind nicht parallel zu bauen.
7. **Begriffskollision:** `Stage` heißt im Code schon die Spielform in einer Übung; die Aufbau-Stufe heißt darum
   `BuildLevel`. `docs/translate.md` müsste das aufnehmen (nicht getan).
8. **Nebenfunde ohne Story:** ein Vater, der früh aufs ⓘ im Planbau tippt, sieht das Popover schließen, weil die
   Trefferliste nachlädt (B-602). `MetricsService` scannt für `DaysComplete`/`StreakDays` die ganze Plandauer pro
   Abgabe, wahrscheinlich teurer als das Doppelrechnen in B-609. `hintPaidReduced` nennt bei Teilwert den vollen Satz.
   „Plan für ein zweites Kind kopieren“ wäre eine Wunsch-Idee zu B-673.
9. **Prozess:** Mehrere Agenten schrieben Frontmatter mit Unterstrich-Hervorhebung (MD049) und einmal mit einem
   Seitenvorschub statt `\f` (B-638, per Python repariert). Beides fing der Lint.
