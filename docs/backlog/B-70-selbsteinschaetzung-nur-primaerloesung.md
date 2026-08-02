---
tags: [typ/story, status/idee, bereich/training, lerntechnik/vokabeln, rolle/student]
aliases: [Selbsteinschätzung Alternativen, Reveal zeigt nur eine Lösung]
status: idee
prio: P2
art: Defekt
quelle: docs/backlog/B-65-vokabel-mehrere-uebersetzungen.md (Review-Nebenbefund)
unverifiziert: true
---

# B-70 · Die Selbsteinschätzung zeigt nur die primäre Übersetzung

Seit [B-65](B-65-vokabel-mehrere-uebersetzungen.md) kann eine Vokabel mehrere gleichwertige Übersetzungen
tragen, und bei den getippten Stufen zählt jede. Die Stufe **Selbsteinschätzung** deckt aber nur die
primäre auf: `PositionPracticeController.BuildCard` und `PositionTestsController.ToItem` reichen
`f.Reveal` (= `item.Answer`) durch. Wer „sehr groß" gedacht hat und „riesig" aufgedeckt bekommt, wertet
sich selbst als falsch — derselbe Schaden wie im ursprünglichen Defekt, nur diesmal vom Kind selbst
verursacht. Genau die Ecke, aus der die Anmerkungen #11/#12 kamen.

Zu klären wäre, ob die Alternativen beim Aufdecken **immer** mitkommen oder nur als „auch richtig:"-Zeile,
und ob das die Bildregel des Anti-Cheats berührt (die Alternativen verlassen den Server heute nirgends —
das ist der Grund, warum es nicht auffällt).

## Verlauf

- **2026-08-02** — aufgenommen als Nebenbefund des `pugling-reviewer` beim Bau von B-65; am Code
  angesehen, aber nicht selbst reproduziert.
