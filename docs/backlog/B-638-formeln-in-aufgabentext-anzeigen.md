---
tags: [typ/story, status/ausformuliert]
aliases: []
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Mathe in die App; Material raw/Math/lambacher-schweizer-9-sh-pugling/md)"
grund: ""
ersetzt_durch: []
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
  `dangerouslySetInnerHTML` gibt es genau einmal (`frontend/src/components/Mascot.tsx:62`), also **keine** gemeinsame
  Rich-Text-Stelle. Eine gemeinsame Text-Anzeigefunktion für Aufgabentexte ist **nicht gefunden**.
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
  (`ClozePrompt.tsx:95`–`:105`, `spoken`); `SpeakButton` liest `karte.prompt` (`Lektion.tsx:110`), ob für Mathe je eine
  Sprache gesetzt wird, ist nicht nachgesehen (`speechLang` kommt vom Server).
- **Das Material trägt LaTeX mit Eigenheiten.** `raw/Math/lambacher-schweizer-9-sh-pugling/md/s074-075.md:25` schreibt
  `$h(x) = 0{,}5x^2$` – das Dezimalkomma als `{,}` (hängt an [B-639](B-639-dezimalkomma-im-zahlenvergleich.md)).
- **Zwei Markup-Kollisionen im Backend, nur gelesen:** der Lückentext kennt `[Lösung]` in eckigen Klammern im Text
  (`backend/Pugling.Agent.Creator/Drafting/ClozeStrategy.cs:53`) und Platzhalter `{{n}}`
  (`.claude/skills/lehrwerk-aufbereiten/laden.py:94`–`:95`); LaTeX benutzt `[ ]` (`\sqrt[3]{x}`, Intervalle) und
  geschweifte Klammern. Ob der Parser (`ClozeMarkup`) an einer Formel scheitert, ist **nicht geprüft**.
- Die PWA lädt automatisch neu (`frontend/vite.config.ts:9`, `registerType: "autoUpdate"`); ob Schriften und CSS einer
  Formel-Bibliothek im Offline-Cache landen, ist nicht nachgesehen.

## Die echte Lücke

Es fehlt **eine Schicht**: ein Baustein, der aus einem Text mit `$…$` Textteile und Formelteile macht, und sein Einsatz an den
gut zehn Stellen oben. Der Server muss nichts ändern (Formel bleibt Text in `Prompt`/`Answer`). Nicht trivial sind drei
Ränder: **Vorlesen/Ansagen** (Quelltext statt Sprache), **Lückentext-Markup** (`[ ]`/`{{ }}` gegen LaTeX) und **das
Vater-Web** (Editor ohne Vorschau).

## Offene Punkte

Alle ohne Entscheidung; Empfehlung dabei.

1. **KaTeX oder MathJax?** Empfehlung: **KaTeX** (synchron, klein, ohne Skript-Nachlauf; React braucht nur
   `renderToString` in einem eigenen Baustein), dynamisch nachgeladen, damit die Arcade ohne Mathe nicht wächst.
2. **Welche Trennzeichen?** Empfehlung: nur `$…$` (inline), `$$…$$` zunächst nicht; ein **unpaariges** `$` bleibt
   Rohtext, ein wörtliches Dollarzeichen geht als `\$`.
3. **Wo wird gesetzt?** Empfehlung: **eine** Komponente (`MathText`) an allen Stellen des Ist-Stands, Sohn **und** Vater-Vorschau;
   der Editor bekommt eine Vorschau je Zeile erst, wenn der Rollengang sie vermisst (eigene Story).
4. **Sicherheit.** Empfehlung: KaTeX mit `trust: false`, `throwOnError: false` – ein Tippfehler im LaTeX zeigt den
   Quelltext, nie eine leere Karte; die Ausgabe wird nicht per `dangerouslySetInnerHTML` aus Nutzertext gebaut, sondern aus der
   Bibliotheksausgabe (Vorbild-Ausnahme: `Mascot.tsx:62`).
5. **Screenreader/Vorlesen.** Empfehlung: KaTeX-MathML-Ausgabe bleibt für Hilfstechnik im DOM; `TaskAnnouncer` sagt
   weiter den lesbaren Text, `SpeakButton` wird bei Formeln nicht gesetzt (Mathe hat keine `speechLang`) – nachzusehen.
6. **Lückentext mit Formel.** Empfehlung: Fall im Test festschreiben (`[ ]`/`{{ }}` innerhalb von `$…$` werden vom
   Lücken-Parser nicht angefasst), ohne den Parser jetzt umzubauen; bricht er, ist das eine eigene Defekt-Story.
7. **Offline.** Empfehlung: Schriften und CSS der Formel-Bibliothek in den Precache der PWA, sonst steht daheim im
   Heimnetz ohne Internet die Formel als Quelltext.

## Akzeptanzkriterien (Entwurf)

1. Ein Aufgabentext `Löse $x^2 - 6x + 8 = 0$` zeigt auf der Sohn-Karte die gesetzte Formel (DOM trägt das Formel-Element),
   den Text daneben unverändert. *Rot, wenn:* `ClozePrompt` weiter den Rohtext rendert.
2. Dasselbe in Auswahloption, Wortkasten, Hinweis und Auflösung (`ErgebnisZeile`). *Rot, wenn:* eine der Stellen
   (`Lektion.tsx:123`, `Antwortflaeche`, `ErgebnisZeile.tsx:13`) nicht auf die neue Komponente umgestellt ist – ein
   Test je Stelle.
3. Dieselbe Formel erscheint in der Vater-Vorschau (`ExercisePreviewModal`). *Rot, wenn:* die Vorschau Rohtext zeigt.
4. Ein Text ohne `$` bleibt unverändert, ein unpaariges `$` bleibt Rohtext, `\$` zeigt ein Dollarzeichen.
   *Rot, wenn:* der Baustein bei ungerader `$`-Zahl wirft oder Text verschluckt.
5. Ein LaTeX-Fehler (`$\frac{1$`) zeigt den Quelltext, keine leere Karte und keinen Fehlerbildschirm.
   *Rot, wenn:* `throwOnError` an ist.
6. `TaskAnnouncer` sagt für eine Aufgabe mit Formel keinen LaTeX-Quelltext an. *Rot, wenn:* die Ansage `\frac`/`^` enthält.
7. Eine Vokabelkarte mit Text ohne `$` bleibt für alle bestehenden Frontend-Tests unverändert grün – **Absichtserklärung**
   (der Ausgangszustand erfüllt sie), rot nur bei einem unbeabsichtigten Eingriff in den Rendering-Pfad.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Konzept „Mathe in die App" aus dem Chat, Lambacher Schweizer 9, Kapitel IV).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): keine Formel-Bibliothek, kein gemeinsamer Anzeigebaustein, gut
  zehn Textstellen im Sohn- und Vater-Web; Server unberührt; Ränder: Ansage/Vorlesen, Lückentext-Markup, Offline-Cache.
