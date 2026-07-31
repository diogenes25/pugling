---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Motivations-Animationen, Serien-Animation, Streak-Feier]
status: idee
prio: P3
art: Wunsch
quelle: Nutzer, Sitzung 2026-07-31
unverifiziert: true
---

# B-36 · Motivations-Animationen bei erreichten Teilzielen

Erreicht der Sohn **während** einer Übung ein Teilziel — etwa **5 richtige Antworten hintereinander** —,
soll die Oberfläche das mit einer Animation feiern, statt erst am Ende eine Zahl zu zeigen. Die Feier hängt
also an einem Zwischenstand innerhalb der laufenden Runde, nicht an Münzen, Missionen oder dem
Abschlussergebnis. Verwandt mit [B-35](B-35-karten-umdrehen-animation.md) (Karten-Flip): dieselbe
Arcade-Politur, aber ein anderer Auslöser.

**Ungeprüft, beim Ausformulieren zu belegen:**

- **Welche Teilziele es geben soll** — die Serie („5 richtig hintereinander") ist das Beispiel des Nutzers,
  nicht die Liste. Kandidaten sind außerdem Halbzeit einer Runde, erste richtige Antwort nach einem Fehler,
  fehlerfreie Runde. Diese Liste ist eine Entscheidung für die Grill-Runde.
- **Wer die Serie zählt** — kennt der Server einen Serienstand (es gibt ein Bonus-/Missionssystem mit
  `PointKind` und `ScoringService`), oder muss der Client mitzählen? Ein
  server-getriebener Auslöser wäre vertragsrelevant und damit Backend-zuerst; ein reines Client-Zählen wäre
  rein `frontend`. Davon hängen `wo`, `vertragsbruch` und die Größe ab.
- **Wo die Animation läuft** — die Ausspielung ist server-getrieben mit `/next`+`/answer` und drei Modi
  (Info/Lern/Klausur). Ob in der **Klausur** überhaupt gefeiert werden darf, ist eine fachliche Frage: der
  Modus ist strikt one-at-a-time, und ein Serien-Feedback verrät dort Richtigkeit, die der Modus
  vielleicht nicht verraten will.
- **`prefersReducedMotion`** — das Projekt respektiert den Schalter bereits (`lib/ui.ts`); eine Animation
  ohne Rückfallweg wäre ein A11y-Rückschritt.
- **Verhältnis zu den bestehenden Belohnungen** — es gibt schon Münzen, Gems, Missionen und
  Auszeichnungen. Wenn die Animation nur *dieselbe* Information doppelt zeigt, ist sie Rauschen; sie muss
  einen Zwischenstand feiern, den bisher niemand sieht.

## Verlauf

- **2026-07-31** — vom Nutzer direkt aufgenommen (ungeprüft, keine Recherche — das ist der nächste Schritt).
