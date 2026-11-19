---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student]
aliases: [Knoten startet direkt, Kenn-ich-Frage gestrichen, Lernkarte ohne Kannte-ich]
status: abgenommen
prio: P1
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Vater im Gespräch am 2026-11-19 (Anmerkungen zum Starten einer Übung)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-686 · Eine Übung startet mit dem Tipp auf den Knoten, und die Lernkarte fragt nicht mehr „Kanntest du das Wort schon?“

Zwei Klicks, die das Lernen bremsen, ohne etwas zu bringen: vor jeder Übung ein Blatt mit „▶ LOS“ und „🎯 Test“
(dazu „8 von 18 eingeprägt · 4 fällig“), und auf jeder Lernkarte die Frage, ob das Wort schon bekannt war. Die Frage
beantwortet der Sohn aus Reflex mit „ja“, weil er keinen Nutzen darin sieht – und gelesen wurde die Antwort von nichts.

## User Story

Als **Sohn** möchte ich, dass ein Tipp auf die Übung im Pfad sofort mit dem Üben beginnt und mich die Lernkarte nicht
vor jedem Wort nach meiner Einschätzung fragt, damit ich ohne Zwischenklicks lerne. Als **Vater** möchte ich, dass der
Test das Üben abschließt und am Start nicht als Abkürzung angeboten wird.

## Ist-Stand am Code (2026-11-19, vor dem Umbau)

- Der Knoten öffnete ein Blatt (`KnotenBlatt`, `frontend/src/sohn/SohnHome.tsx:387`) mit Hauptknopf „Los“ und dem
  zweiten Weg (`nebenweg`, `frontend/src/sohn/pfad.ts:120`) – bei fälligen Karten also „Test“ direkt am Start.
- Die Lernkarte (`frontend/src/sohn/Lernkarte.tsx`) hatte vier Schritte; Schritt 2 fragte „Kanntest du das Wort
  schon?“. Die Antwort (`WasKnown`) landete als ungewertete Historienzeile (`PositionPracticeController.cs:882`) –
  B-598, Entscheidung 3: „gespeichert, aber nicht gewertet … ein Wert, den heute nichts anzeigt“. Gedacht war sie für
  einen Einstufungstest ([B-587](B-587-einstufungstest-vor-der-vokabel-position.md)), der nur als Idee existiert.
- Der Server erkannte „aufgegeben“ nur daran, dass `WasKnown` mitkam: eine Antwort ohne jedes Ergebnis ist nach
  [B-433](B-433-vorschau-des-vaters-verbraucht-die-einfuehrung.md) kein Urteil (zweiter Tab, schon anderswo eingeführt).

## Entscheidungen

Mit dem Vater am 2026-11-19, je eine Frage.

1. **Der Buchbezug (B-588) wandert in den Kopf der Übungsrunde.** *Begründung:* Dort braucht das Kind die Seite, während
   es übt. *Kosten:* ein zusätzliches Lesen der Plan-Übersicht beim Start der Runde; scheitert es, fehlt nur die Pille.
2. **Die Test-Sperre bleibt Oberfläche.** Der Knoten führt ins Üben, direkt in den Test nur, wenn heute nichts zu üben
   ist; sonst bietet erst das Ende der Runde den Test an. *Begründung:* klein und sofort wirksam. *Kosten:* Wer die
   Test-Adresse kennt, kommt weiter direkt hinein – keine Anti-Cheat-Regel am Server.
3. **Die Kenn-ich-Frage wird ersatzlos gestrichen.** Aufdecken führt direkt zum Abschreiben. *Begründung:* Das Erinnern
   aus dem Kopf sagt mehr als eine Selbsteinschätzung, und die Antwort las niemand. *Kosten:* B-587 verliert eine
   Datenquelle; „Aufgeben“ braucht ein eigenes Signal (`ReviewDto.GaveUp`, additiv), sonst wäre es nach B-433 kein Urteil
   und das Wort käme nicht wieder.

Mitgenommen: „· N fällig“ stand nur auf dem Blatt und entfällt; „eingeprägt“ zeigen Ring und Vorlesename des Knotens.

## Akzeptanzkriterien

- [x] Ein Tipp auf den Knoten startet die Übung ohne Zwischenblatt; mit fälligen Karten ins Üben, ohne in den Test.
- [x] Am Start gibt es keinen Test-Knopf; der Test kommt am Ende der Übungsrunde.
- [x] Der Buchbezug steht im Kopf der Übungsrunde und des Tests, ohne Seite steht keine Pille.
- [x] Die Lernkarte geht vom Aufdecken direkt zum Abschreiben; ihre Antwort trägt kein `wasKnown`.
- [x] „Später nochmal“ schickt `gaveUp: true`; das Wort kommt am Rundenende wieder, die Historie hält nur das Erinnern.
- [x] Eine Antwort ganz ohne Ergebnis bleibt kein Urteil (B-433 unverändert grün).

## Verlauf

- 2026-11-19: gebaut. Backend: `ReviewDto.GaveUp` (additiv), `LearnCardReviewAsync` liest es als Urteil und schreibt
  die Selbsteinschätzungs-Zeile nur noch, wenn `WasKnown` kommt. Neuer Integrationstest
  `LernkarteTests.Lernkarte_Ohne_Kannte_Ich_Frage_Wiederholt_Und_Speichert_Das_Erinnern`, rote Probe: ohne
  `dto.GaveUp ||` fällt er an `repeatsAtEnd`. Erster Wurf (jede Lernkarten-Antwort ist ein Urteil) brach B-433 –
  `ErstbegegnungTests.AntwortloseBewertung…` fing es, darum das explizite Feld.
- Frontend: Knoten ist ein Link (`hauptweg`), `KnotenBlatt`/`nebenweg` samt CSS und i18n-Schlüsseln entfernt; Buchbezug
  in `SohnPractice`; Lernkarte in drei Schritten. Vitest 115 Dateien / 1256 Fälle grün, `tsc -b` sauber.
- E2E (Ausweichports 5201/5174) mit den Pfad-, Lernkarten- und Test-Specs: alles grün bis auf
  `erstbegegnung.spec.ts:92` – fällt identisch auf dem unveränderten `main` (Tippfehler-Blatt ohne `<s>`), also nicht
  von dieser Story.
- Review: `pugling-reviewer` ohne Befund (zwei Kosmetik-Hinweise übernommen: Doku umbrochen, überflüssiges
  `verdict &&` entfernt). `frontend-reviewer` fand einen echten Fehler im E2E-Helfer `pfadStart`: der synthetische
  Strg-Klick öffnete in Chromium einen zweiten Tab, der eine eigene Übungsrunde spielte – ersetzt durch einen in der
  Capture-Phase abgebrochenen Klick. Außerdem übernommen: `pfadStart` springt in den verlangten Weg statt immer in den
  Test; Buchbezug mit eigenen Unit-Tests in `SohnPractice.test.tsx` (rote Probe: ohne `bookPages`-Bedingung fällt
  „zeigt ohne Seite keine Pille“), die E2E-Abwesenheitsprüfung wartet auf die Übersicht; zwei Zusicherungen, die nie
  fallen konnten (Blatt/Test-Knopf abwesend), gestrichen; Payload-Typ aus dem Vertrag (`Omit<ReviewInput, "itemIndex">`).
- ~~Offen, an den PO: führt der Knoten direkt in den Test (heute nichts zu üben), sieht das Kind den Buchbezug nirgends –
  der Testbildschirm hat keine Pille.~~ Vater am 2026-11-19: „ja, auch im Test“ (siehe unten).
- Schlussstand: `dotnet test Pugling.sln -c Release` 1921/1921, Vitest 115 Dateien / 1258 Fälle, E2E der berührten
  Specs grün (Pfad, Lernkarte, Test-Einstiege, Rollengänge 81/82/83, full-flow).
- 2026-11-19: abgenommen, Commit `bc4be340`. Rollengang = die E2E-Specs oben (Pfad-Start, Lernkarte, Buchbezug im Kopf); der Blick des Sohns am echten Gerät steht nach dem nächsten Heimnetz-Neustart aus.
- 2026-11-19, Nachtrag: der Buchbezug steht auch im Kopf des Tests. Das Lesen liegt jetzt einmal in `useBuchbezug`
  (`frontend/src/sohn/buchbezug.ts`), Üben und Test nutzen es. Unit-Test `SohnTest.test.tsx` „Buchbezug im Kopf“ (rote
  Probe: ohne `unterKopf` fällt er), E2E `sprint-81-rollengang.spec.ts` prüft die Pille jetzt in Üben **und** Test.
  Vitest 115 Dateien / 1259 Fälle, `tsc -b` sauber.
