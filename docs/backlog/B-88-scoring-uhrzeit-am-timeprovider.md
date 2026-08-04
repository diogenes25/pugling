---
tags: [typ/story, status/idee, bereich/punkte, bereich/tests, rolle/student]
aliases: [Scoring-Uhrzeit, DateTime.Now im Punkte-Pfad]
status: idee
prio: P3
art: Aufräumen
quelle: docs/backlog/B-10-zeitfenster-pro-kind.md
unverifiziert: true
---

# B-88 · Die Punkte-Uhrzeit kommt von der Wanduhr, nicht vom `TimeProvider`

`PositionPracticeController` übergibt dem `ScoringService` ein `DateTime.Now` — zwei Zeilen über der
Stelle, an der dieselbe Methode den injizierten `TimeProvider` für Zeitstempel, Combo und
Schnell-Antwort-Bonus benutzt. Damit hängt die **Zeitfenster-Entscheidung** an der echten Uhr des Servers
und lässt sich mit dem `TestClock` nicht einfrieren.

Sichtbar geworden ist das beim Bauen von [B-10](B-10-zeitfenster-pro-kind.md): weil die Uhrzeit nicht
steuerbar ist, braucht der End-to-End-Test einen **eigenen Host**, ein Ganztags-Fenster und zehn
neutralisierte globale Fenster, nur um vom Zeitpunkt des Testlaufs unabhängig zu werden. Mit
`time.GetLocalNow().DateTime` ließe sich stattdessen „13:00 → ×2,0 / 21:00 → ×0,8" direkt festnageln, und
der ganze Neutralisierungs-Apparat entfiele.

Befund des `pugling-reviewer` beim Review zu B-10; die Zeile ist älter als B-10.

## Verlauf

- **2026-08-04** — aus dem B-10-Review aufgenommen (ungeprüft: der genaue Umfang der Testvereinfachung
  ist geschätzt, nicht gemessen).
