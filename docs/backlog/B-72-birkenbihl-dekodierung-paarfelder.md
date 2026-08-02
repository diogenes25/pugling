---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Dekodierung Paar-Felder, Wort:wörtlich]
status: idee
prio: P3
art: Defekt
quelle: B-69 (Entscheidung 2)
unverifiziert: true
---

# B-72 · Die Birkenbihl-Dekodierung trägt zwei Trennzeichen in einem Feld

Der Editor nimmt die Wort-für-Wort-Dekodierung als **einen** String im Format „Wort:wörtlich, …"
entgegen ([exerciseConfig.tsx:525](../../frontend/src/vater/exerciseConfig.tsx)) und zerlegt ihn beim
Senden doppelt — erst am Komma, dann am Doppelpunkt (`:203`); zurückgelesen wird über
`.join(", ")` (`:295`). Im Vertrag ist es längst eine Liste von Paaren
(`BirkenbihlSentence.Decoding` als `List<WordPair>`,
[ExerciseConfigs.cs:249](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)).

Damit hat das Feld denselben Fehler wie die fünf aus [B-69](B-69-wiederhol-felder-alternativen.md),
nur zweimal: Weder ein Wort noch seine wörtliche Glosse darf ein Komma **oder** einen Doppelpunkt
enthalten. Bei einer wörtlichen Übersetzung ist das keine ferne Möglichkeit — genau dort stehen
Umschreibungen.

B-69 stellt die fünf Listenfelder auf `RepeatedTextFields` um und lässt dieses eine ausdrücklich stehen:
Die Komponente kann nur Einzelwerte, ein Paar braucht **zwei** Felder je Zeile. Diese Story zieht es nach.

**Zu prüfen beim Ausformulieren:** ob eine Paar-Variante der Komponente entsteht oder eine eigene; ob die
Dekodierung überhaupt von Hand getippt wird, seit es die automatische Dekodierung gegen den
Vokabelspeicher gibt (`decode`-Vorschau) — das könnte die Story erledigen, bevor sie gebaut wird.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-69, Entscheidung 2.
