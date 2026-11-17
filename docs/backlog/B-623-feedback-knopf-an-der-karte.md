---
tags: [typ/story, status/idee, bereich/training, bereich/katalog, rolle/student, rolle/creator, rolle/supervisor]
aliases: [Feedback-Knopf, Rückmeldung zur Übung]
status: idee
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Erfahrung aus der Testphase mit Friedrich und Theodor)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-623 · Der Lernende gibt zur Karte eine Rückmeldung, und neue Übungen berücksichtigen sie

In der Testphase zeigt sich, dass manche Übungen zu schwer oder zu leicht sind, Vokabeln abfragen, die
im Unterricht noch nicht dran waren, falsch übersetzen, zu viele Wörter auf einmal verlangen oder zu
streng prüfen. Friedrich und Theodor merken das an der Karte, haben dort aber keinen Weg, es zu sagen.
Das Anmerkungs-Widget ist ein Dev-Werkzeug und in der Sohn-App nur im Entwicklungsmodus eingebunden
(Stand laut [B-586](B-586-kind-fragt-den-betreuer-zur-aufgabe.md), hier nicht nachgeprüft).

Die Idee: An jeder Karte bzw. Übung ein Feedback-Knopf. Er öffnet eine **Vorauswahl**, damit das Kind
kaum schreiben muss, und ein **optionales Freitextfeld** für „was war gut/schlecht". Mögliche Gründe
aus dem Gespräch:

- zu schwer / zu leicht
- Vokabel ist weiter als unser Unterrichtsstoff
- falsch übersetzt
- zu viele Wörter verlangt (die Antwort ist umfangreicher als die eigentliche Vokabel)
- zu streng geprüft (Akzent/Strich/ein Buchstabe falsch ⇒ ganzer Fehler)
- das Wort hat mehrere Übersetzungen, die Eingabe passt nicht dazu
- (positiv) gut so / hat Spaß gemacht

Der Server hängt den Kontext an (Kind, Position, Übung, Item, letzte Antwort), so wie das Widget ihn heute
mitschneidet. Die Rückmeldungen sollen **wirken**: Der Vater sieht sie gesammelt, und beim Erstellen
neuer Übungen – von Hand und im [KI-Creator](../../backend/Pugling.Agent.Creator/CLAUDE.md) – fließen sie
ein (etwa: Wörter jenseits des Unterrichtsstands meiden, Antworten kürzer halten).

Zu klären beim Ausformulieren: Abgrenzung zu [B-586](B-586-kind-fragt-den-betreuer-zur-aufgabe.md) (Frage
mit Antwort vs. Bewertung ohne Antwort – ein Knopf oder zwei?), ob die Remarks-API (`remarks/…`) der
Träger sein kann, ob eine Rückmeldung den Lernstand berührt (Anti-Cheat: „zu schwer" darf keine Karte
überspringen), wie sie im Vater-Web erscheint und in welcher Form der KI-Creator sie liest. Verwandt:
[B-590](B-590-lehrwerk-bezug-im-vater-plan.md) („schon durchgenommen"),
[B-536](B-536-aufbau-uebungen-statt-versagen.md) (Stoff zu schwer).

Die Einzelbefunde aus demselben Gespräch stehen als eigene Stories: [B-624](B-624-akzentfehler-nicht-ganzer-fehler.md),
[B-625](B-625-ein-eingabefeld-je-bedeutung.md), [B-626](B-626-antwort-verlangt-mehr-als-die-vokabel.md).

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
