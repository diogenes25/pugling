---
tags: [typ/plan, bereich/gamification, bereich/training, bereich/auswertung, rolle/supervisor, rolle/student]
aliases: [Khan-Academy-Vergleich, Khan Academy, Mastery Challenge, Learning Queue]
---

# Khan Academy als Vergleichsmaßstab: was wir übernehmen, was nicht

Status: **Plan** (2026-10-17). Die Übernahme läuft als Stories im [Backlog](backlog/README.md); die
Sammel-Story ist [B-217](backlog/B-217-khan-academy-vergleich-sammel-story.md). Dieses Dokument hält
den Vergleich und die Reihenfolge, **nicht** den Zustand der Etappen.

← [Zurück zur Wissenskarte](obsidian.md) · Fachmodell: [grundprinzip.md](grundprinzip.md) ·
Punkte: [wiki/05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md)

## Warum dieser Vergleich

Khan Academy (auch [de.khanacademy.org](https://de.khanacademy.org/), eine Übersetzung der US-Plattform)
ist die größte frei zugängliche Lernplattform mit Mastery-Modell, Eltern-Dashboard und Gamification.
Sie spiegelt den Creator- und den Student-Teil unseres Drei-Ebenen-Modells fast deckungsgleich wider und
ist uns an einigen Stellen der Lernmechanik voraus. Was ihr strukturell fehlt, ist unsere zweite Ebene:
ein Supervisor, der Pflicht mit Konsequenz verknüpft (`PenaltyCoins`) und reale Belohnungen über den
Familien-Shop abrechnet. Der Vergleich dient also zweierlei: Ideen übernehmen, wo Khan weiter ist, und
den eigenen Kern schärfen, wo Khan ihn nicht hat.

Quellenstand: Khan-Support-Artikel und Blog, abgerufen am 2026-10-17 (Links am Ende).

## Gemeinsamkeiten

| Khan Academy | Pugling | Bewertung |
| --- | --- | --- |
| Kurs → Unit → Skill | `TextbookSeries → SeriesUnit → Exercise` | Deckungsgleich; Khan am US-Curriculum, wir am deutschen Lehrwerk. |
| Mastery-Stufen je Skill (Attempted / Familiar / Proficient / Mastered), Abstieg bei Fehlern | Leitner-Box 1–5 je Item plus `MasteryPercent`, Abstieg auf Box 1 | Gleiche Idee, bei uns feiner (Vokabel-Item statt Skill). |
| Gems für Proficient/Mastered, Badges, Avatar-Kosmetik | Gems aus Combo/Speed/Missionen → Skins | Fast identisch; beide ohne realen Gegenwert. |
| Wochen-Streak (mindestens ein Skill pro Woche auf Proficient) | Tages-Streak über erfüllte Pflicht (`CurrentStreak`, B-105) | Vorhanden, andere Taktung. |
| Lehrer-/Eltern-Zuweisung mit Fälligkeitsdatum | `PlanPosition` mit `Daily/Weekly` und `GoalThreshold` | Beide binden Katalog an Pflicht; unsere ist wiederkehrend, Khans einmalig. |
| Unit-Test, Course Challenge | Positions-Abschlusstest, Klassenarbeiten | Vergleichbar. |
| Eltern-Reports (Aktivität, Einzelbericht, Scores, Mastery je Skill) | Positions-Report, Vokabel-Lernstand flach und hierarchisch | Inhaltlich da, bei uns über Endpunkte verteilt; Dashboard ist [B-39](backlog/B-39-supervisor-dashboard.md). |

## Wo Khan weiter ist

1. **Mastery Challenge.** Alle 12 Stunden 6 Fragen aus 3 Skills, gewählt nach Zeit seit der letzten
   Wiederholung und aktueller Stufe; zwei Fehler je Skill senken die Stufe, einer wird verziehen.
   Unser Leitner läuft **pro Position**; eine kind-weite Wiederholungsrunde fehlt.
2. **Gestufte Bewertung.** Stufe aus dem Prozentwert einer Runde (70–99 % Familiar, 100 % Proficient).
   Bei uns setzt ein Fehler das Item auf Box 1 zurück.
3. **Learning Queue.** Alles nach Fälligkeit in Tages-, Wochen- und Unit-Missionen; Überfälliges rutscht
   automatisch in die Tagesliste. Unser `overview` liefert die Tagesmission ohne gestaffelte Warteschlange.
4. **Levels.** Ein Kurs in Levels zerlegt, Badge-Leiter bei 5, 10, 15, 25, 50, 75, 100. Unsere
   Auszeichnungen sind frei konfigurierbar, aber ohne Leiter.
5. **Hilfe vor der Antwort.** Khanmigo gibt Strategiehinweise ohne Lösung. Unser Item kennt einen
   statischen `Hint`, aber keinen gestuften, bepreisten Hinweisweg.
6. **Content-Mix.** Video, Artikel, Übung, Quiz in einer Unit. Wir sind übungszentriert.

## Wo Pugling stärker ist

- **Der Stick.** Khan hat keine Konsequenz bei verpasster Pflicht. `PenaltyCoins` mit Schulden und
  Lazy Settlement ist unser Alleinstellungsmerkmal.
- **Zwei Währungen mit realem Ende.** Münzen landen im Familien-Shop mit Aktivierungsanfrage und Bestand.
- **Pflicht pro Position konfigurierbar.** Rhythmus, Schwelle, Zeitfenster, Bonus, Malus je Position und Kind.
- **Multi-Supervisor mit gemeinsamem Wallet.** Khan braucht dafür den Umweg über ein Lehrer-Dashboard.
- **Eigene Inhalte zum Lehrwerk.** Khan-Eltern erstellen nichts.
- **Anti-Cheat.** Server-autoritative Stufen, Lösungsfeld-Regel, Medien-Schranken.

## Bewusst nicht übernommen

- **Zuweisung als einmalige Frist** statt wiederkehrender Pflicht. Unser Modell ist die Pflicht mit
  Rhythmus; eine Frist ist dort ein Sonderfall (siehe [B-09](backlog/B-09-lehrer-hausaufgaben.md)).
- **Gems als einzige Belohnung.** Die Trennung Münzen/Gems ist der Kern der Reward-Ökonomie.
- **Content-Mix mit Video/Artikel.** Kein eigener Übungstyp „Video"; erklärender Einstieg ist Sache des
  Lehrwerks, nicht der App.
- **Mastery-Prozent als Kursfortschritt.** Khan selbst ersetzt ihn in „Reimagined" durch Unit-Missionen.

## Übernahmeplan

Die Reihenfolge folgt der Abhängigkeit, nicht der Attraktivität. Backend zuerst (API-First), jede Story
läuft die normale Kette im Backlog (`idee → ausformuliert → gegrillt → geschaetzt → in-arbeit → abgenommen`).

| Etappe | Story | Was | Hängt ab von |
| --- | --- | --- | --- |
| 1 | [B-218](backlog/B-218-mischwiederholung-ueber-alle-positionen.md) | Kind-weite Mischwiederholung aus fälligen Items aller spielbaren Positionen, zeitlich gedeckelt | Abgrenzung zu [B-04](backlog/B-04-adaptiver-vokabel-pool.md) (positionsgebundener Pool) |
| 2 | [B-219](backlog/B-219-verzeihende-abstufung-je-position.md) | Verzeihende Abstufung als Positions-Option: ein Fehler senkt eine Box, zwei setzen zurück | keine; wirkt auch in Etappe 1 |
| 3 | [B-220](backlog/B-220-learning-queue-im-sohn-dashboard.md) | Learning Queue in der Sohn-Arcade: heute / diese Woche / überfällig aus `overview` und Missionen | Etappe 1 liefert die Zeile „Wiederholung heute" |
| 4 | [B-221](backlog/B-221-level-leiter-fuer-auszeichnungen.md) | Geseedete Badge-Leiter mit festen Schwellen im `GamificationService` | keine |
| 5 | [B-222](backlog/B-222-wochen-streak-als-default.md) | Prüfauftrag: ist der Tages-Streak aus B-105 der richtige Default, oder Khans Wochen-Streak? | B-105 ist abgenommen; Antwort darf `verworfen` sein |
| 6 | [B-223](backlog/B-223-gestufte-hinweise-vor-der-antwort.md) | Gestufter Hinweis am Item, der Punkte kostet; kompatibel mit der Lösungsfeld-Regel | keine |
| 7 | [B-39](backlog/B-39-supervisor-dashboard.md) | Supervisor-Dashboard; Khans drei Eltern-Reports stützen die Priorität | bereits `geschaetzt`, kein neuer Eintrag |

Etappen 1 bis 3 bilden den Kern (Lernmechanik und Sichtbarkeit beim Kind), 4 bis 6 sind unabhängig
voneinander und können einzeln fallen oder verworfen werden. Jede Story muss beim Ausformulieren gegen
den Code belegt werden; die Beschreibung hier ist der Anlass, nicht der Ist-Stand.

## Quellen

- [Mastery levels](https://support.khanacademy.org/hc/tr/articles/5548760867853) ·
  [Mastery points and Course Challenge](https://support.khanacademy.org/hc/en-us/articles/115002552631) ·
  [Mastery Challenges](https://support.khanacademy.org/hc/en-us/articles/360037494231)
- [Streaks and Levels (Blog)](https://blog.khanacademy.org/?p=17224) ·
  [Badges for streaks and levels](https://support.khanacademy.org/hc/en-us/articles/31179888119181-Update-New-badges-for-streaks-and-levels-including-a-new-Black-Hole-badge) ·
  [Daily streaks retired 2021](https://support.khanacademy.org/hc/en-us/community/posts/360075847492-Update-Streaks-are-going-away-on-January-4-2021)
- [Parent Dashboard](https://support.khanacademy.org/hc/en-us/articles/360039664491-What-can-I-do-from-the-Khan-Academy-Parent-Dashboard) ·
  [Parent reports](https://support.khanacademy.org/hc/en-us/articles/36120531499789-What-reports-can-I-use-as-a-parent-to-monitor-my-child-s-activity-on-Khan-Academy) ·
  [Parents can make assignments](https://support.khanacademy.org/hc/en-us/community/posts/36122612106765-Update-Parents-can-now-make-assignments-for-their-children-on-Khan-Academy)
- [Student dashboard (Reimagined)](https://support.khanacademy.org/hc/en-us/articles/46935588089997-How-do-I-use-the-student-dashboard-in-Khan-Academy) ·
  [Assignments in Reimagined](https://support.khanacademy.org/hc/en-us/articles/46938861004685-Where-can-I-find-my-assignments-in-Khan-Academy-Reimagined) ·
  [Khanmigo for parents](https://support.khanacademy.org/hc/en-us/articles/21823169137037-What-Khanmigo-tools-are-available-to-parents)
- [Das deutschsprachige Team](https://support.khanacademy.org/hc/tr/articles/16103450282765-Das-deutschsprachige-Team-The-German-Team)

---

**Verwandt:** [grundprinzip.md](grundprinzip.md) · [wiki/05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md) ·
[docs/backlog/](backlog/README.md) · [B-105 Tagesbox](backlog/B-105-taegliche-belohnungsbox.md)
