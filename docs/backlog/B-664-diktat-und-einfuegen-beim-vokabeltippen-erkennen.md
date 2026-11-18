---
tags: [typ/story, status/idee, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Diktierfunktion erkennen, Buchstabe für Buchstabe, Anti-Cheat Eingabe]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Vater am 2026-11-18, Feedback nach der ersten echten Runde mit Friedrich"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-664 · Diktieren, Einfügen und Autovervollständigung beim Vokabeltippen erkennen

Beim Vokabellernen müssen die Kinder die Wörter schreiben – richtig so, denn im Vokabeltest wird auch
geschrieben. Sie nutzen dafür aber die **Diktierfunktion** des Handys. Der Vater will das nicht: jeder
Buchstabe soll bewusst eingegeben werden. Ebenso unerwünscht sind **Copy-Paste** und die
**Autovervollständigung** der Tastatur.

Idee des Vaters: das Frontend erkennt, ob ein Wort Buchstabe für Buchstabe getippt wurde. Kommen mehrere
Zeichen auf einmal ins Feld (oder in unrealistisch kurzer Zeit), war es wahrscheinlich kein Tippen.

Beim Ausformulieren zu klären: Welche Signale der Browser liefert (`beforeinput`/`inputType` wie
`insertFromPaste`, `insertReplacementText`, `insertFromDictation`; Zeichen je Ereignis; Zeitabstände) und
wie zuverlässig sie auf Android/iOS sind; ob man das Feld dafür konfiguriert (`autocomplete="off"`,
`autocorrect="off"`, `spellcheck="false"`, `autocapitalize="off"`); was bei einem Treffer passiert
(Eingabe verwerfen und neu tippen lassen, als falsch werten, nur dem Vater melden). Der Server kann das
nicht prüfen – ein clientseitiges Signal ist umgehbar; reicht das als Hürde für ein Kind? Abgrenzung zu
[B-661](B-661-fehlende-sonderzeichen-auf-der-tastatur-blockieren-die-mission.md): eine Sonderzeichen-Leiste
fügt ebenfalls ganze Zeichen ein und darf nicht als Schummeln zählen.

## Verlauf

- 2026-11-18 · idee · aufgenommen aus dem Feedback des Vaters
