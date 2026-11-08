---
tags: [typ/protokoll, bereich/pm, rolle/student]
aliases: [Lauf AE, Nachtlauf NP-24 NP-25]
---

# PM-Sitzung: Lauf AE – erst lesen, dann prüfen; der Grader ist fair

**Datum:** 2026-11-08  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Unbeaufsichtigter Lauf über die Pakete NP-24 und NP-25 aus dem
[Rollengang vom 2026-11-08](pm-sitzung-2026-11-08-rollengang.md), nach `pm-loop` und
[docs/backlog/README.md](backlog/README.md), mit den Freigaben aus [nachtlauf.md](nachtlauf.md).

**Auftrag des Nutzers (wörtlich):** „Nachtlauf: NP-24 + NP-25 (Erst lesen, dann prüfen). Fahre einen unbeaufsichtigten
Backlog-Lauf nach pm-loop und docs/backlog/README.md. Entscheide selber nach besten Wissen und gewissen."

**Eingangsprüfung:** B-419, B-420, B-421, B-430 stehen auf `geschaetzt`; die `Wunsch`-Stories B-419, B-421, B-430 tragen je
eine Freigabe-Zeile des Nutzers aus Commit `05700b6` (Dialog-Sitzung, nicht aus diesem Lauf) – Freigabe 9 deckt sie. B-420
ist `Defekt` (Freigabe 1). Keine Nachprüfungs-Zeilen vorhanden.

## Sprint 22 — Ziel & Umfang (NP-24)

**Sprint-Ziel:** Der Sohn sieht jedes neue Wort einmal mit Lösung, bevor er es tippen muss; ein Fehler hält die Karte an, bis
er die Lösung gelesen hat; und nach einem verpatzten Test kann er die Wörter sofort üben.
**Umfang:** B-419, B-420, B-430.
**Entwickler-Brief:** Quelle der Wahrheit für „neu" ist `PositionItemProgress.IntroducedAt` (schon vorhanden, kein Schema). Der
Server entscheidet je Karte, ob sie eine Einführung ist, und spielt sie dann auf der Anzeigestufe des Typs aus
(`PositionPlayService.IsIntroduction`/`IntroductionStage`) – an allen vier Stellen, die eine Karte bauen oder bewerten
(`Cards`, `Next`, `Review` samt Folgekarte, `ReshuffleImage`). Guards: nur Lern-Modus, nur Typen mit Anzeigestufe (heute
Vokabeln), nie auf einer Anzeigestufe des Tages; die Bewertung richtet sich nach der Stufe der **Karte**, damit eine
übersprungene Einführung keine Punkte bringt. Danach Frontend: Rundenende (B-419), Haltezustand bei Fehler (B-420), Knopf im
Testergebnis (B-430). Testweg: `ErstbegegnungTests`, Vitest, neue Spec `erstbegegnung.spec.ts` als Rollengang Sohn mit
**frischem** Plan (Retro-Vorschlag CB).

## Iteration 22 — umgesetzt

**Backend (API-First):** additives Kartenfeld `introduction`; die vier Kartenwege lesen die eingeführten Atome einmal je Anfrage
(`IntroducedIndicesAsync`) und entscheiden je Karte. Keine Migration, kein Vertragsbruch.

**Abweichung vom Angriffsplan (B-419, Entscheidung 4, Mechanik – nicht Produkt):** Der Server zählt die eingeführten Karten
**nicht** im End-Response, sondern markiert jede Karte; das Frontend zählt die markierten Karten der Runde. Ein Zähler am
`/end` bräuchte ein gespeichertes Merkmal je Review (eine Migration, die Entscheidung 5 ausschließt) oder eine Heuristik
über Zeitfenster bzw. `ReviewCount`, die bei Positionen ohne Leitner die zweite Runde noch einmal mitzählen würde. Die
Produktentscheidung (der Bildschirm nennt die Zahl und bietet die Abfrage an) bleibt unverändert; die Wahrheit „ist diese
Karte eine Einführung" bleibt beim Server.

**Folge für 35 Bestandstests:** Sie spielten eine nie gesehene Karte sofort getippt – genau das Verhalten, das B-419 abschafft.
`TestApi.SeedLeitnerPosition` markiert die Atome darum standardmäßig als eingeführt (`introduced: true`, abschaltbar),
API-angelegte Positionen bekommen `TestApi.MarkIntroduced`; drei Tests, deren Gegenstand der Erstkontakt ist (Info-Modus,
Mission „neue Wörter", Bild-Wechsel auf einer Kennenlernkarte), laufen ausdrücklich mit `introduced: false`.

**Frontend:** `SohnPractice` – Rundenende mit „N neue Wörter kennengelernt" und „▶ Jetzt abfragen" (neue Sitzung über einen
Rundenzähler im Startschlüssel und im Heartbeat-Effekt), Haltezustand `wrong` für falsch **getippte** Antworten (die
Selbsteinschätzung hält bewusst nicht an: dort stand die Lösung schon auf der Rückseite); `TestResult` – „▶ Diese Wörter üben".
Drei E2E-Specs (`full-flow`, `buchstaben-ordnen`, `karten-umdrehen`) spielten neue Wörter sofort ab und gehen jetzt erst
durch die Kennenlern-Runde (Helfer `introduceAndQuiz`).

**Verifikation (nach dem letzten Edit):** Backend 1135/1135, Vitest 683/683, `tsc -b` sauber, Playwright **120 passed**
(volle Suite). Mutanten mit `.claude/scripts/mutant-probe.py`, alle rot am benannten Test (Tabellen in B-419/B-420). Ein
erster Volllauf der E2E lief versehentlich parallel zu einer Mutantenprobe und zeigte zwei Rote: `sprache.spec.ts` (echt:
spielte eine frische Position, jetzt über die Kennenlernkarte) und `responsive-vater.spec.ts` (Last, im sequenziellen
Lauf grün) – die Hausregel „Mutanten nie parallel zu E2E" gilt, hier verletzt und an genau diesem Rauschen gemessen.

## Runde 2 — Review (Sprint 22)

`pugling-reviewer`: keine 🔴. 🟡 Combo-Bruch in gemischten Runden → Einführungen vorn (**Zähler 1**); 🟡 Abweichung von
Entscheidung 4 nicht notiert → in B-419 notiert (Doku, ohne Zählung); 🟢 Folgekarte außer der Reihe, Lesbarkeit →
behoben (ohne Zählung); 🟢 Vater-Runde verbraucht die Einführung → [B-433](backlog/B-433-vorschau-des-vaters-verbraucht-die-einfuehrung.md);
🟢 Item-Historie bucht Kennenlernen als „nicht gewusst", API-Beispiel zeigt `awarded: 0` → bewusst hingenommen.

`frontend-reviewer`: keine 🔴. 🟡 Rest-Sekunden der alten Sitzung gingen verloren (**2**), 🟡 Fokus fiel nach „Weiter" auf
`body` (**3**), 🟡 eine gemischte Runde verschwieg ihre Münzen (**4**), 🟡 Rundenneustart ohne Test (**5**) – alle behoben,
je mit Mutante. 🟡 Schnellbonus misst die Lesezeit mit → Server, [B-434](backlog/B-434-schnellbonus-misst-die-lesezeit-der-loesung-mit.md).
🟡 **„Diese Wörter üben" kann ins Leere führen** – die Prämisse von B-430 war falsch (der Test setzt die
Positions-Fälligkeit nicht). Das ist eine Produktentscheidung, die die Story nicht hergibt → nach Freigabe 8 angehalten,
Code zurückgenommen, B-430 auf `ausformuliert` mit Offenem Punkt 3 und `wartet_auf`. Nicht gezählt: keine Reparatur,
sondern ein Halt. **Zähler Sprint 22: 5 von 5** – die Grenze ist erreicht, nicht überschritten.

## Runde 3 — Abnahme (Sprint 22)

- **Sohn:** Rollengang als E2E mit **frischem** Plan (`erstbegegnung.spec.ts`): die erste Runde zeigt „la ville → die
  Stadt" ohne Antwortfeld, das Ende sagt „3 neue Wörter kennengelernt", „Jetzt abfragen" fragt mit Antwortfeld; „die
  Strasse" hält die Karte mit „Lösung: die Straße" auch nach 2 s an, Enter trägt weiter, der Fokus steht wieder im Feld.
  Dazu `full-flow` (Kennenlernen, Abfrage, Combo ×5, Test). Sprint-Ziel in zwei von drei Teilen gehalten; der dritte
  („nach einem verpatzten Test sofort üben") ist **nicht geliefert** – B-430 wartet auf die Entscheidung.
- **Vater, Creator:** Regressionszeugen – volle Suite grün, keine ihrer Flächen im Diff. Kein O-Ton erfunden.
- **Ausgefallen:** ein Browser-Rollengang von Hand (Chrome nicht geprüft); die E2E fährt denselben Weg.

B-419, B-420 → `abgenommen` (`652e5d3`); B-430 → `ausformuliert`, `wartet_auf`.

## Retrospektive (Sprint 22)

**Nachschau:** Sprint 21 (`4c25504`: B-411, B-413, B-414), frischer `pugling-reviewer`. Ein 🟡 entgangen bei B-413 – der
Reihen-Pfad der Unit-Etappen-Sperre ist ungetestet, obwohl Meldung, Summary und Tutorial ihn behaupten →
[B-436](backlog/B-436-reihe-loeschen-mit-unit-etappe-ungetestet.md) (`entgangen_bei: [B-413]`). 🟢: eine der zwei
Bedingungen im Ausnahme-Selbsttest von B-411 ist ohne Mutante, deren Definition nicht abgelegt; ein Summary-Kommentar mit
Ausweg-Ton; eine ungenaue Begründung in B-414. Alle drei tragen `nachgeschaut: 2026-11-08` mit ihrem Prüfpunkt. Increment eines früheren Laufs (AD) – kein Limit 2 aus der Nachschau (Präzedenz Lauf AD); kein Tor gelandet, Zähler nicht überschritten → der Lauf geht mit NP-25 weiter.

**Was die eigenen Tore durchgelassen haben:** (1) Die Ist-Stand-Behauptung von B-430 („der Test schreibt den Lernstand,
also kennt die Übungsrunde die Fehler") verband zwei verschiedene Tabellen (`ItemProgress` vs. `PositionItemProgress`) –
weder die Schätzung noch mein Bau noch meine eigene E2E sahen es; die E2E war grün, weil dasselbe Wort schon in der Übung
falsch war, und ihr Kommentar behauptete die falsche Ursache. Gefunden hat es der `frontend-reviewer`. (2) Zweimal war
meine erste Mutante die falsche Frage: „Reparatur entfernen" (R3) blieb grün, weil der Mount-Wert die Lücke zudeckte; die
Regression des Funds war „Uhr beim Klick zurücksetzen". Das Skript hat das sichtbar gemacht (`abweichend`), nicht verdeckt.

**Vorschläge (nicht gelandet, Freigabe 3):**

- **(CD)** Eine Ist-Stand-Aussage der Form „A schreibt X, also liest B es" gilt in `ausformuliert` nur mit **beiden**
  Enden belegt: der Schreibstelle und der Lesestelle **derselben** Spalte (`Datei:Zeile` je Ende). Ort: Eintrittsbedingung
  `ausformuliert` im Backlog-README. Prosa, weil es eine Prüfung beim Lesen ist; ein Tor fände keine Kausalbehauptungen.
- **(CE)** Eine E2E, die „wird durch Y fällig/sichtbar" zusichert, baut den Fall, in dem **nur** Y es verursacht (hier:
  alle Übungsantworten richtig, dann den Test verpatzen). Ort: `frontend/CLAUDE.md` neben „Eine Fläche je E2E-Spec".
- **(CF)** Eine Mutante zu einem Review-Fund formuliert **die Regression, die der Fund beschreibt**, nicht das Entfernen
  der Reparatur. Ort: der Docstring von `mutant-probe.py`.

**Bewusst ungedeckt:** dass ein Kind die Kennenlernkarte wirklich *liest* (subjektiv; Gerätecheck nicht nötig, aber kein
Tor kann es sagen).

## Sprint 23 — Ziel & Umfang (NP-25)

**Sprint-Ziel:** Der Sohn bekommt „Stadt" für „die Stadt" als richtig gewertet – im Üben und im Test –, es sei denn, die
Übung trainiert den Artikel.
**Umfang:** B-421. B-422 (Tippfehler) nur notiert, nicht entschieden.
**Entwickler-Brief:** Quelle der Wahrheit bleibt der **eine** `AnswerGrader`; die Toleranz reist als Eigenschaft des
Inhalts-Atoms (`ContentItem.ArticleOptionalIn` = Sprache der erwarteten Antwort oder `null`), damit alle fünf
Bewertungsstellen (Üben, Test einzeln, Test gesammelt, Vorschau, Mengen-Abgleich) über **eine** Methode laufen und nicht
auseinanderdriften können. Gesetzt nur für Vokabel-Atome im Resolver; Lückentexte, Birkenbihl und Übersetzung bleiben
exakt. Opt-out `VocabularyConfig.ArticleRequired` (additiv, kein Schema). Testweg: `AnswerGraderTests` (Unit, zuerst rot),
`ArtikelToleranzTests` (Integration), Vitest für den Editor-Haken, Rollengang Sohn in `erstbegegnung.spec.ts`.

## Iteration 23 — umgesetzt

**Rote Probe:** Grader-Signatur mit Sprache zuerst ohne Toleranz → `AnswerGraderTests` **11 rot erwartet, 11 gemessen**
(der Fall „Die Stadt" war schon vorher richtig und bleibt Kontrollfall).

**Backend:** Artikel-Tabelle je Sprache in `StageMechanics.WithoutLeadingArticle` (de/en/fr, französische Elision als
Präfix, Regionssuffix `de-DE` abgeschnitten); `AnswerGrader.Matches(given, item)` als einziger Atom-Einstieg; die Sprache
der Antwort folgt der Abfragerichtung (`ExerciseContentProvider.Swaps`) – bei „hinten → vorne" ist die Antwort
französisch, und dann darf „la" fehlen. **Auslegung, keine neue Entscheidung:** „Zielsprache" in Entscheidung 1 lese ich
als „Sprache der erwarteten Antwort" – die Story begründet die Toleranz damit, dass diese Sprache bekannt ist.

**Frontend:** Haken „Artikel muss mitgetippt werden" unter der Abfragerichtung, Hin- und Rückweg in `exerciseConfig.tsx`.
**Doku:** `wiki/05-punkte-und-bonus.md` sagt, was „richtig" heißt (inkl. Kennenlernkarte aus B-419).

**Verifikation (nach dem letzten Edit):** Backend **1169/1169** (ein erster Volllauf fiel an einem Zeitmess-Test außerhalb
des Diffs, der einzeln zweimal grün war → [B-438](backlog/B-438-pinhasher-zeittest-flackert-unter-last.md); der zweite
Volllauf war grün), Vitest 686/686, `tsc -b` sauber. Mutanten: G1–G8 (Grader, Tabelle, Resolver) und H1–H9 (Haken an
beiden Orten, Erklärung, Elision, Auswahl, Richtung „beide"), alle rot am benannten Fall (Tabellen in B-421).

## Runde 2 — Review (Sprint 23)

`frontend-reviewer`: **🔴 der Haken stand im Typ-Editor (`ConfigEditor`), den die App für Vokabeln nie zeigt** – Anlegen
läuft über den Wortwähler, Bearbeiten über den Beschreibungs-Block des Dialogs (**Zähler 1**); **🔴 die Tests bewachten
genau diesen toten Zweig** (**2**); 🟡 die Erklärung gehört nach `fieldHelp.ts` (**3**); 🟡 Vertragsdokument noch nicht
erzeugt (keine Code-Änderung, ohne Zählung). Behoben: ein Baustein `ArticleRequiredCheck` mit ⓘ an beiden echten Orten,
der tote Zweig wieder entfernt, Belege über die reinen Funktionen (`initialForm`, `vocabSettingsConfig`,
`buildTypeConfig`) und eine E2E `artikel-pflicht.spec.ts`, die anhaken → anlegen → im Dialog lesen → abhaken → speichern
→ neu laden fährt.

`pugling-reviewer`: keine 🔴. Auslegung „Sprache der erwarteten Antwort" bestätigt (die andere Lesart täte nichts).
🟡 Vertragsdateien mitcommitten (ohne Zählung). 🟡 „a few" nimmt „few" an – vom Wortlaut der Entscheidung gedeckt, eine
Produktfrage → [B-437](backlog/B-437-artikel-toleranz-nur-bei-nomen.md), nicht entschieden. 🟢 Elision mit Leerzeichen,
🟢 ein artikelloses Nachbarwort als zweite richtige Auswahl, 🟢 Richtung „beide" ungetestet → alle behoben (ohne Zählung,
je mit Mutante). **Zähler Sprint 23: 3 von 5.**

## Runde 3 — Abnahme (Sprint 23)

- **Sohn:** E2E `erstbegegnung.spec.ts` – in der Abfrage-Runde tippt er „Stadt" für „die Stadt", die Karte rückt weiter
  (sie hielte sonst nach B-420 an). Sprint-Ziel gehalten: im Üben (Integration + E2E) und im Test (beide Antwortwege,
  Integration) gleich gewertet.
- **Creator:** E2E `artikel-pflicht.spec.ts` – der Haken steht beim Anlegen und im Dialog, mit ⓘ, und hält beim Speichern.
  Die Standing-Bedingung („ohne JSON-Handarbeit") hält: kein Config-Feld von Hand.
- **Vater:** Regressionszeuge – volle Suite grün, keine Vater-Fläche im Diff.

B-421 → `abgenommen` (`481c973`).

## Retrospektive (Sprint 23)

**Nachschau:** Sprint 22 (`652e5d3`: B-419, B-420), frischer `frontend-reviewer`, nur gelesen. **Ein 🟡 entgangen bei B-420:**
die sichtbare Lösung in der angehaltenen Karte prüft kein Test – Vitest und E2E treffen auch die `sr-only`-Region →
[B-439](backlog/B-439-loesung-in-angehaltener-karte-ungetestet.md) (`entgangen_bei: [B-420]`, mit drei 🟢: `bilder.spec.ts`
läuft nur zufällig, ein Testname verspricht die Combo, Fokus nach der letzten Karte). Eine zweite Folge der Einführung
(Server entscheidet sie beim Review neu) ergänzt [B-433](backlog/B-433-vorschau-des-vaters-verbraucht-die-einfuehrung.md).
B-419 und B-420 tragen `nachgeschaut: 2026-11-08` mit ihren Prüfpunkten. **Das ist ein Defekt im Increment dieses Laufs →
Limit 2: der Lauf endet hier** (er war mit NP-25 ohnehin am Ende der Pakete). Index: **Nachgeschaut 349 von 350** (offen nur B-421 dieses Sprints).

**Was die eigenen Tore durchgelassen haben:** (1) Ich habe den Haken in den Typ-Editor gesetzt, weil die Vokabel-Zeile dort
schon „Abfragerichtung" trug – ohne zu prüfen, ob die App diesen Zweig für Vokabeln überhaupt rendert. Die Tests waren
grün, die Mutanten rot – an einem Ort, den niemand sieht. Erst der `frontend-reviewer` las, **wer** die Komponente mountet.
(2) Dieselbe Klasse in der Nachschau: eine Zusicherung `getAllByText(…).length > 0` bzw. `.first()` trifft auch ein
unsichtbares Duplikat (die `sr-only`-Region). Beide Male bewiesen Tests einen Zweig bzw. ein Element, das es gibt – aber
nicht das, was der Nutzer sieht.

**Vorschläge (nicht gelandet, Freigabe 3):**

- **(CG)** Ein Tor, das einen Zweig `type === "Vocabulary"` in `ConfigEditor` meldet, solange `VaterExerciseCreate` und
  `ExerciseEditModal` den `ConfigEditor` für Vokabeln nie rendern – allgemeiner: Code, den kein Mount-Pfad erreicht, ist
  im Frontend nicht „ungenutzt", sondern ein Ort für Fehlbauten. Mechanisch schwer (statisch), darum zuerst als Regel in
  `frontend/CLAUDE.md`: **„Neues Feld an einer Komponente? Erst greppen, wer sie für diesen Typ mountet."**
- **(CH)** Zusicherungen auf sichtbaren Text, der zusätzlich in einer Live-Region steht, werden auf den Container begrenzt
  (`within(...)`, `locator(".card")`), nie `getAllByText(...).length` oder `.first()`. Ort: `frontend/CLAUDE.md` neben der
  RTL-Regel.
- Die Vorschläge (CD)–(CF) aus Sprint 22 gelten weiter.

**Bewusst ungedeckt:** ob „Stadt" für „die Stadt" für Lehrer und Kind *fair* wirkt – das ist der Kern der Entscheidung,
kein Messwert; B-437 trägt die offene Grenze.

## Offene Roadmap

Die Liste ist [docs/backlog/](backlog/README.md). Dieser Lauf: B-419, B-420, B-421 `abgenommen`; B-430 zurück auf
`ausformuliert` (`wartet_auf`, Offener Punkt 3). Neu: B-433, B-434, B-435, B-437 (Fragen für den Dialog), B-436, B-438,
B-439 (Defekte, autonom grillbar). **Für den Dialog am Morgen:** B-430 Punkt 3 (wohin führt „Diese Wörter üben"), B-437
(Toleranz nur bei Nomen?), B-433/B-434/B-435, dazu die Retro-Vorschläge (CD)–(CH). **Nächster Nachtlauf:** zuerst B-439
(Defekt aus der Nachschau) und B-436, dann NP-26 nach Plan. Nichts gepusht.
