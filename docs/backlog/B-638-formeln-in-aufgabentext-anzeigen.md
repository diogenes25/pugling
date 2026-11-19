---
tags: [typ/story, status/gegrillt]
aliases: []
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Mathe in die App; Material raw/Math/lambacher-schweizer-9-sh-pugling/md)"
grund: ""
ersetzt_durch: []
wartet_auf: Bestätigung der Sonnet-Entscheidungen
---

# B-638 · Formeln in Aufgabentext und Auswahl anzeigen

Das Mathe-Lehrwerk (Lambacher Schweizer 9, Kapitel IV) liegt durchgehend in LaTeX vor (`$x^2 - 6x + 8 = 0$`), das Frontend
kann Formeln aber nicht setzen – in `frontend/package.json` steht weder KaTeX noch MathJax. Ohne das ist kein
Mathe-Inhalt lesbar, auch nicht mit den vorhandenen Typen Zuordnen und Lückentext (Merkwissen: pq-Formel, Satz von
Vieta, Diskriminanten-Fälle; Karten-Aufgaben Gleichung ↔ p/q ↔ Term ↔ Lösung). Gedacht: `$…$` in Aufgabentext,
Auswahloptionen und Auflösung wird als Formel gesetzt, im Sohn- und im Vater-Web. Erste Stufe des Mathe-Vorhabens;
B-640 bis B-642 hängen daran.

## User Story

Als **Schüler** möchte ich eine Formel in Aufgabe, Auswahl und Auflösung lesbar gesetzt sehen (Hochzahl, Bruch,
Wurzel), damit ich eine Mathe-Aufgabe verstehe, statt `x^2 - 6x + 8 = 0` als Quelltext zu entziffern.

Als **Creator/Vater** möchte ich dieselbe Formel in der Vorschau und im Editor gesetzt sehen, damit ich beim Anlegen
sehe, was das Kind sieht.

## Ist-Stand am Code

Recherchiert am 2026-11-19 gegen `47791819`.

- **Keine Formel-Bibliothek.** `frontend/package.json:19`–`:25` führt `i18next`, `react`, `react-dom`, `react-i18next`,
  `react-router-dom`; KaTeX/MathJax stehen weder dort noch irgendwo unter `frontend/` (Suche ohne Treffer).
  `dangerouslySetInnerHTML` gibt es **am `HEAD`** genau einmal (`frontend/src/components/Mascot.tsx:62`); der
  ungecommittete Arbeitsbaum fasst `Mascot.tsx` an und enthält die Stelle nicht mehr (Suche unter `frontend/src` ohne
  Treffer). Eine gemeinsame Text-Anzeigefunktion für Aufgabentexte ist **nicht gefunden**.
- **Aufgabentext erscheint als reiner Text an vielen Stellen.**
  - Sohn-Karte: `ClozePrompt` als Textknoten (`frontend/src/components/ClozePrompt.tsx:52`, `:57`), eingebunden in
    `frontend/src/sohn/Lektion.tsx:117`; Text-Passage `Lektion.tsx:102` (`components/Passage.tsx:14`), Hinweis `:121`,
    Wortkasten `:123`, Auswahlknöpfe in `Antwortflaeche` (`Lektion.tsx:151`).
  - Auflösung/Ergebnis: `frontend/src/sohn/ErgebnisZeile.tsx:13` und `frontend/src/sohn/SohnTest.tsx:366`–`:391`
    (`expected` in `<b>`).
  - Vater-Vorschau: `frontend/src/vater/ExercisePreviewModal.tsx:153`, `:175`, `:192`, `:227`, `:240` (Optionen).
  - Vater-Editor: Eingabefelder je Zeile, z. B. `frontend/src/vater/exerciseConfig.tsx:673`, `:713`, `:728` – nur Rohtext,
    keine Vorschau der Zeile (gelesen: Platzhalter-Texte; Live-Vorschau **nicht gefunden**).
- **Screenreader und Vorlesen lesen den Quelltext.** Die Live-Region sagt den rohen Aufgabentext an
  (`ClozePrompt.tsx:95`–`:105`, `spoken`); `SpeakButton` liest `karte.prompt`, aber nur, wenn `karte.speechLang` gesetzt
  ist (`Lektion.tsx:110`); ob für Mathe je eine Sprache gesetzt wird, ist nicht nachgesehen (`speechLang` kommt vom
  Server; im Backend unter dem Namen `speechLang` nicht gefunden).
- **Das Material trägt LaTeX mit Eigenheiten.** `raw/Math/lambacher-schweizer-9-sh-pugling/md/s074-075.md:25` schreibt
  `$h(x) = 0{,}5x^2$` – das Dezimalkomma als `{,}` (hängt an [B-639](B-639-dezimalkomma-im-zahlenvergleich.md)).
- **Markup-Kollisionen, nur gelesen:**
  1. Der Platzhalter `{{n}}` wird im Frontend mit `/\{\{(\d+)\}\}/` zerlegt (`frontend/src/lib/cloze.ts:11`, `:29`) –
     bevor irgendein Formel-Baustein den Text sieht. Ein Text wie `$2^{{10}}$` enthielte damit eine „Lücke 10“.
  2. Das Lücken-Markup mit eckigen Klammern (`ClozeMarkup.cs`, `Bracket()`; `laden.py:83` `parse_cloze`) kennt `[ ]`
     als Lücke; LaTeX benutzt sie für `\sqrt[3]{x}` und Intervalle. Beides sind Pfade des Agenten und des Laders, nicht
     des Frontends.
- Die PWA lädt automatisch neu (`frontend/vite.config.ts:9`, `registerType: "autoUpdate"`); die Datei setzt keine
  eigenen `workbox`-Muster (Suche in `vite.config.ts`: kein `globPatterns`), es gilt der Plugin-Standard. Ob dieser
  Schriftdateien (`woff2`) in den Precache nimmt, ist nicht nachgesehen und beim Bau zu prüfen.

## Die echte Lücke

Es fehlt **eine Schicht**: ein Baustein, der aus einem Text mit `$…$` Textteile und Formelteile macht, und sein Einsatz an den
gut zehn Stellen oben. Der Server muss nichts ändern (Formel bleibt Text in `Prompt`/`Answer`). Nicht trivial sind drei
Ränder: **Vorlesen/Ansagen** (Quelltext statt Sprache), **Lückentext-Markup** (`[ ]`/`{{ }}` gegen LaTeX) und **das
Vater-Web** (Editor ohne Vorschau).

## Offene Punkte

1. ~~**KaTeX oder MathJax?**~~ → Entscheidung 1.
2. ~~**Welche Trennzeichen?**~~ → Entscheidung 2.
3. ~~**Wo wird gesetzt?**~~ → Entscheidung 3.
4. ~~**Sicherheit.**~~ → Entscheidung 4.
5. ~~**Screenreader/Vorlesen.**~~ → Entscheidung 5.
6. ~~**Lückentext mit Formel.**~~ → Entscheidung 6.
7. ~~**Offline.**~~ → Entscheidung 7.
8. **Lücke innerhalb einer Formel** (Lückentext der pq-Formel mit Lücke mitten im Bruch) – zurückgestellt, siehe
   Entscheidung 6: Grund ist, dass die Zerlegung an `{{n}}` im Frontend und der Parser des Agenten (`[ ]`) beide vor
   der Formel greifen; eine Lösung berührt Frontend, Lader und Agent und wäre eine eigene Story. Frage an den Nutzer:
   Braucht das Mathe-Vorhaben Lücken *in* Formeln, oder genügen Zuordnen und Auswahl für das Merkwissen? Empfehlung:
   erst beantworten, wenn der Rollengang an B-640 zeigt, dass Merkwissen als Zuordnung nicht reicht.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **KaTeX, dynamisch nachgeladen; bis es da ist, steht der Quelltext.** Begründung: synchron zu rendern und ohne
   Skript-Nachlauf, kleiner als MathJax; der dynamische Import hält die Arcade ohne Mathe unverändert. **Verworfen:**
   MathJax (asynchroner Nachlauf, schwerer) und das Mitliefern im Hauptbündel. **Ausgeschlossen:** LaTeX-Makros, die
   KaTeX nicht kennt – sie fallen auf den Quelltext zurück (Entscheidung 4). **Kosten:** ein kurzes Aufblitzen des
   Quelltexts, bis die Bibliothek geladen ist; die Größe der Bibliothek ist nicht gemessen und wird vor dem Bau
   gemessen (`vite build`, Chunk-Größe).
2. **Nur `$…$` (inline), mit der Pandoc-Regel; `$$…$$` gibt es nicht.** Ein öffnendes `$` darf nicht von einem
   Leerzeichen gefolgt sein, ein schließendes nicht von einem Leerzeichen vorangehen und nicht von einer Ziffer
   folgen; ein unpaariges `$` bleibt Rohtext; ein wörtliches Dollarzeichen geht als `\$`. Begründung: Die Regel macht
   englischen Text wie „it costs $5 and $6“ nicht zur Formel (das schließende `$` hinge hinter einem Leerzeichen).
   Im Seed steht kein Dollarpreis (Suche `\$[0-9]` in `backend/Pugling.Api/Data/Seed*.cs`: kein Treffer) – das ist
   **keine** Gegenprobe für Katalog und echte DB, darum die vorsichtigere Regel. **Verworfen:** jedes `$`-Paar als
   Formel (Preise im Englisch-Stoff würden verschluckt) und `$$…$$`. **Ausgeschlossen:** große, abgesetzte Formeln –
   ein Bruch erscheint inline kleiner; wer ihn groß will, schreibt `\displaystyle` in die Formel. **Kosten:** eine
   Regel mehr, die ein Creator kennen muss (steht in der Editor-Vorschau, Entscheidung 3).
3. **Eine Komponente `MathText` an allen Stellen des Ist-Stands, im Sohn- und im Vater-Web – und eine Vorschauzeile im
   Editor.** Gesetzt wird an den Stellen unter „Ist-Stand“ (Karte, Auswahl, Wortkasten, Hinweis, Auflösung, Vorschau).
   Die Vorschauzeile unter einem Editor-Feld erscheint nur, wenn der Text ein gepaartes `$…$` enthält. Begründung: Die
   zweite User Story verlangt die Formel „in der Vorschau **und im Editor**“ – nur die Vorschau (`ExercisePreviewModal`)
   zu setzen, ließe den Creator beim Anlegen mit Quelltext allein. **Verworfen:** den Editor in eine eigene Story
   auslagern (der ursprüngliche Vorschlag) – gegen die User Story gerechnet bliebe deren zweiter Satz offen.
   **Ausgeschlossen:** ein WYSIWYG-Formeleditor; der Creator tippt LaTeX. **Kosten:** der Editor wird angefasst; an
   wie vielen Feldtypen, ist nicht gezählt und gehört zur Schätzung (mindestens die Aufgaben- und Optionsfelder).
4. **KaTeX mit `trust: false` und `throwOnError: false`, gerendert über `katex.render(…, Element)` in einer
   Ref-Wirkung – nicht über `dangerouslySetInnerHTML`.** Ein LaTeX-Fehler zeigt den Quelltext (KaTeX setzt ihn dann
   selbst, rot gefärbt), nie eine leere Karte. Begründung: Nutzertext landet so nie als HTML-String im DOM; am `HEAD`
   steht die Stelle `Mascot.tsx:62`, die zeigt, dass das Repo den Weg sonst nur mit einem eigenen SVG gegangen ist,
   und der Arbeitsbaum entfernt ihn gerade. **Verworfen:** `renderToString` + `dangerouslySetInnerHTML` (eine zweite
   Stelle dieser Art ohne Not). **Ausgeschlossen:** `\href`/`\url` und Ähnliches (`trust: false`). **Kosten:** das
   Rendern hängt an einer Wirkung, im Test (jsdom) muss die Bibliothek erreichbar sein.
5. **Ansage und Vorlesen:** Die Live-Region (`TaskAnnouncer`) sagt eine Formel als das Wort „Formel“ an, nie LaTeX; der
   `SpeakButton` erscheint bei einem Text mit Formel nicht. Hilfstechnik liest die Formel aus dem MathML, das KaTeX
   (`output: "htmlAndMathml"`, Vorgabe) neben dem sichtbaren HTML im DOM hält. Begründung: Der Quelltext
   („x hoch 2 minus 6 x …“ aus `\frac`, `^`) ist keine Sprache, und das MathML trägt die Formel für Screenreader besser
   als jede eigene Ansage. **Verworfen:** eine eigene Sprechtext-Erzeugung aus LaTeX (zweiter Parser).
   **Ausgeschlossen:** Vorlesen von Mathe-Aufgaben für Kinder mit Leseschwäche – gegen die User Story gerechnet gibt es
   dafür keinen Satz, es ist aber ein Verlust gegenüber Sprachkarten. **Kosten:** wer die Ansage hört, erfährt den
   Formelinhalt nur über die Karte selbst.
6. **Lückentext mit Formel: Lücken stehen außerhalb von `$…$`; der Fall wird im Test festgeschrieben, kein Parser wird
   umgebaut.** `ClozePrompt` zerlegt zuerst an `{{n}}` (`cloze.ts`), dann setzt `MathText` jedes Textstück einzeln. Eine
   Formel, die eine Lücke enthält, wird dadurch in zwei Stücke mit je einem unpaarigen `$` geschnitten und bleibt
   Rohtext – lesbar, aber ungesetzt. Auch `{{10}}` aus `$2^{{10}}$` wäre eine Lücke. Begründung: Das hält den Parser
   des Agenten und des Laders (`[ ]`) unberührt. **Verworfen:** die Formel vor der Zerlegung herauslösen und Lücken darin
   als `\boxed`/`\ldots` zu setzen (berührt Frontend, Lader und Agent). **Ausgeschlossen – gegen die User Story
   gerechnet:** ein Lückentext der pq-Formel mit einer Lücke im Bruch; Zuordnen und Auswahl tragen das Merkwissen,
   der Lückentext nur mit der Formel *neben* der Lücke („Die Lösung von $x^2 = 9$ ist {{1}}“). Siehe offener Punkt 8.
   **Kosten:** eine Einschränkung, die ein Creator wissen muss und die der Editor nicht meldet.
7. **Schrift und CSS der Formel-Bibliothek gehören in den Precache der PWA.** Der Plugin-Standard nimmt, soweit bekannt,
   `js`, `css` und `html`; `woff2` muss ergänzt werden (`workbox.globPatterns` in `vite.config.ts`). Begründung: Daheim im
   Heimnetz ohne Internet stünde die Formel sonst als Quelltext oder mit Ersatzschrift da. **Verworfen:** Schriften von
   einem CDN laden (kein Netz, kein Precache, Datenschutz). **Kosten:** Der Precache wächst um die Schriftdateien.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. Ein Aufgabentext `Löse $x^2 - 6x + 8 = 0$` zeigt auf der Sohn-Karte die gesetzte Formel (DOM trägt das KaTeX-Element),
   den Text daneben unverändert. *Rot, wenn:* `ClozePrompt` weiter den Rohtext rendert.
2. Dasselbe in Auswahloption, Wortkasten, Hinweis und Auflösung (`ErgebnisZeile`). *Rot, wenn:* eine der Stellen
   (`Lektion.tsx:123`, `Antwortflaeche`, `ErgebnisZeile.tsx:13`, `SohnTest.tsx:366`) nicht auf `MathText` umgestellt ist –
   ein Test je Stelle.
3. Dieselbe Formel erscheint in der Vater-Vorschau (`ExercisePreviewModal`). *Rot, wenn:* die Vorschau Rohtext zeigt.
4. Ein Editor-Feld mit gepaartem `$…$` zeigt eine gesetzte Vorschauzeile, eines ohne `$` keine. *Rot, wenn:* die
   Vorschauzeile fehlt oder bei Text ohne `$` erscheint.
5. Ein Text ohne `$` bleibt unverändert, ein unpaariges `$` bleibt Rohtext, `\$` zeigt ein Dollarzeichen,
   „it costs $5 and $6“ bleibt Rohtext. *Rot, wenn:* der Baustein bei ungerader `$`-Zahl wirft, Text verschluckt oder die
   Preise zur Formel macht.
6. Ein LaTeX-Fehler (`$\frac{1$`) zeigt den Quelltext, keine leere Karte und keinen Fehlerbildschirm. *Rot, wenn:*
   `throwOnError` an ist.
7. `TaskAnnouncer` sagt für eine Aufgabe mit Formel das Wort „Formel“ statt des Quelltexts an, und der `SpeakButton`
   fehlt dort. *Rot, wenn:* die Ansage `\frac` oder `^` enthält oder der Knopf bei einer Formel erscheint.
8. Ein Lückentext mit einer Lücke **innerhalb** von `$…$` zeigt Rohtext und wirft nicht; eine Lücke neben der Formel
   zeigt Formel und Lücke. *Rot, wenn:* der Zerlege-Pfad eine halbe Formel setzt oder eine Karte leer bleibt.
9. `vite.config.ts` nimmt `woff2` in den Precache. *Rot, wenn:* `workbox.globPatterns` kein `woff2` enthält – ein
   Vitest liest die Konfiguration.
10. Eine Vokabelkarte mit Text ohne `$` bleibt für alle bestehenden Frontend-Tests unverändert grün – **Absichtserklärung**
    (der Ausgangszustand erfüllt sie), rot nur bei einem unbeabsichtigten Eingriff in den Rendering-Pfad.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Konzept „Mathe in die App" aus dem Chat, Lambacher Schweizer 9, Kapitel IV).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): keine Formel-Bibliothek, kein gemeinsamer Anzeigebaustein, gut
  zehn Textstellen im Sohn- und Vater-Web; Server unberührt; Ränder: Ansage/Vorlesen, Lückentext-Markup, Offline-Cache.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): KaTeX dynamisch, `$…$` mit
  Pandoc-Regel, Vorschauzeile auch im Editor (gegen die zweite User Story), Rendern ohne `dangerouslySetInnerHTML`,
  Lücken nur außerhalb von Formeln (offener Punkt 8 zurückgestellt). Abgestimmt mit [B-639](B-639-dezimalkomma-im-zahlenvergleich.md)
  und [B-640](B-640-uebungstyp-gleichung-loesen.md).
