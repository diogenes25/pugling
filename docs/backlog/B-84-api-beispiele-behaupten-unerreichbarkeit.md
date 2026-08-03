---
tags: [typ/story, status/idee, bereich/doku, bereich/backend]
aliases: [Fehlercode nicht erreichbar, api-examples Generator-Vorgabe,
  Verifiziert-Zähler lügt]
status: idee
prio: P3
art: Defekt
quelle: B-81 (Abnahme, Reviewer-Befund außerhalb des Schnitts)
unverifiziert: true
---

# B-84 · Die API-Beispiele behaupten Unerreichbarkeit, wo nur nichts mitgeschnitten wurde

`docs/api-examples/index.md` listet jeden Fehlercode aus `ApiErrors`, den der Doku-Lauf nicht mitgeschnitten
hat, mit dem Satz **„Über HTTP im In-Process-Test nicht erreichbar."** Das ist die Vorgabe des Generators
(`DocsCaptureTests.cs:1242`, gesetzt für jeden Code ohne Eintrag in seiner `reasons`-Tabelle) — und für
mindestens einen Code **nachweislich falsch**: `vocabulary_not_assigned` wird seit B-81 von einem
Integrationstest per HTTP ausgelöst und steht trotzdem in der Liste. Richtig wäre „nicht von
`DocsCaptureTests` erfasst"; das ist etwas anderes und viel harmloser. Gemessen am 2026-08-03: **19 Zeilen**
tragen die Vorgabe, vier weitere haben eine echte Begründung aus `reasons`, und darüber steht der Zähler
`Verifiziert: 34 / 57`.

Der Schaden ist nicht die Formulierung, sondern was sie über die **Testlage** behauptet. Wer die Liste liest,
schließt „für diesen Code gibt es keinen HTTP-Pfad, also ist er nicht testbar" — und schreibt den Test nicht,
den er hätte schreiben sollen. Das trifft genau die Stelle, an der dieses Projekt schon einmal gemessen hat,
dass ein Tor über ein Artefakt nur beweist, dass sich das Artefakt nicht ändert, nicht dass es stimmt
(`docs/testplan.md`).

Zu klären beim Ausformulieren: ob der Generator die Aussage **nur umformulieren** soll (eine Zeile, sofort
richtig, aber der Zähler bleibt eine Aussage über den Doku-Lauf und liest sich weiter wie eine über die
Suite) — oder ob er den Unterschied **abbilden** soll, also „von `DocsCaptureTests` mitgeschnitten" gegen
„irgendwo in der Suite per HTTP ausgelöst" gegen „im laufenden System nicht erreichbar" trennen. Die zweite
Fassung wäre die wahre, kostet aber eine Erhebung über die ganze Suite. Dazu die Frage, ob die vier
handgeschriebenen `reasons` dann noch nötig sind.

## Verlauf

- **2026-08-03** — angelegt aus der Abnahme von [B-81](B-81-vokabel-tags-geben-uebersetzungen-preis.md): der
  `pugling-reviewer` hat den Satz als 🟢 gemeldet, weil der dortige Commit ihn für den neuen Code
  `vocabulary_not_assigned` in ein eingechecktes Artefakt getragen hat. Bewusst **nicht** als Nebenbei-Fix
  dort mitgenommen — es ist eine Änderung am Generator, die 19 Zeilen umschreibt, und die interessante Frage
  (umformulieren oder den Unterschied wirklich abbilden) ist keine, die man in einer Abnahme entscheidet.
  Belegt sind die Fundstelle (`index.md:85`), die Herkunft (`DocsCaptureTests.cs:1242`), die Zahl der
  betroffenen Zeilen (19) und der Gegenbeweis (der B-81-Test löst den Code per HTTP aus); **nicht** belegt
  ist, wie viele der übrigen 18 Codes ebenfalls falsch beschrieben sind — genau das ist die Arbeit des
  Ausformulierens, darum `unverifiziert: true`. `prio: P3` in Analogie zu
  [B-83](B-83-loesungsfeld-regel-residenter-kontext.md) vorgeschlagen (Doku, die von einer im Code schon
  greifenden Lage falsch erzählt) — nicht vom Nutzer bestätigt. Ein Argument für `P2` gibt es: die Aussage
  kann jemanden davon abhalten, einen Test zu schreiben.
