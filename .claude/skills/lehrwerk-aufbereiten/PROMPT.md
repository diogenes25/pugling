# Startprompt für ein neues Lehrwerk

In eine **frische** Sitzung im Repo kopieren und die drei Platzhalter in spitzen Klammern ersetzen. Der Prompt
lädt den Skill `lehrwerk-aufbereiten`; dort stehen Regeln, Format und Prüfschritte.

```text
Bereite ein neues Lehrwerk aus Fotos auf und nutze dafür den Skill lehrwerk-aufbereiten.

- Fotos: <raw/france/Decouvertes/Serie bleue 2>  (schreibgeschützt, nie committen)
- Buch: <Découvertes Série bleue 2, Klett, Französisch, Klasse 7, Gymnasium>
- Kind: <1 – nach dem Laden auf dieses Buch umstellen | nicht umstellen>

Lies jede Seite selbst (Read-Tool), dichte Seiten in Kacheln mit kacheln.ps1. Dass die Fotos dafür an dich
gehen, ist in Ordnung. Schreib alles nach raw/<fach>/<werk-slug>-pugling/ im Format aus FORMAT.md:
Stoffnotizen je Unit in eigenen Worten, die Wortlisten jeder fotografierten Vokabelseite buchstabengetreu
und ohne Raten (unsicher markieren statt vermuten), dazu je Unit zwei bis vier eigene Start-Übungen zu den
Grammatik-Schwerpunkten. Keine Aufgaben, Texte, Dialoge oder Beispielsätze aus dem Buch.

Dann: laden.py --pruefen, alle Fehler beheben, gegen eine Wegwerf-DB auf Port 5280 laden (jede Übung 100 %,
zweiter Lauf ohne Änderung) und das Briefing des KI-Creators mit --profile/--series-unit/--unit prüfen.

Halte an, bevor du in meine echte Instanz lädst: Zeig mir die Units, die Zahl der Wörter je Unit, jede
Zeile, die unsicher war, und wie du sie geklärt hast, und die Titel der Start-Übungen. Frag mich dann nach
Adresse und PIN. Committe nichts aus raw/; Funde am Werkzeug gehören in docs/backlog/.
```

## Für einen Nachtrag (weitere Seiten desselben Buchs)

```text
Zum Lehrwerk in <raw/france/decouvertes-2-pugling> sind neue Fotos dazugekommen: <Ordner oder Dateien>.
Nutze den Skill lehrwerk-aufbereiten als Nachtrag: buch.json lesen und ergänzen, neue Wortlisten als
vokabeln-unit<N>.tsv, Korrekturen an bestehenden Listen mit --ersetzen laden. Gleicher Ablauf wie beim
ersten Mal, mit Halt vor der echten Instanz.
```
