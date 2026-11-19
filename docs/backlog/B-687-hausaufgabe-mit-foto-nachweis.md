---
tags: [typ/story, status/idee, bereich/training, bereich/medien, bereich/belohnung, rolle/student, rolle/supervisor]
aliases: [Hausaufgaben-Foto, Hausaufgabe machen, Foto-Nachweis]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-19 (Friedrichs Mathe-Hausaufgabe S. 80/82 per Foto nachgeprüft)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-687 · Hausaufgabe als Pflicht mit Foto-Nachweis: der Sohn fotografiert sein Heft, der Vater nimmt ab

**Anlass (2026-11-19):** Der Vater sah in WebUntis, dass Friedrich die Mathe-Hausaufgabe zum 18.11. (S. 80 Nr. 1+2,
S. 82 Nr. 12) „nicht erledigt“ hatte. Am nächsten Tag zeigte Friedrich ihm das Heft. Claude prüfte das Foto gegen die
aufbereitete Buchseite (`raw/Math/lambacher-schweizer-9-sh-pugling/md/s080-081.md`, `s082-083.md`): alles richtig,
12d fehlte, das Lösungswort auch. Zwei Erkenntnisse daraus:

1. **Das WebUntis-Signal taugt nicht.** Das Feld `completed` stand bei allen 16 Hausaufgaben seit dem 1.11. auf
   `false`. Es ist ein Häkchen des Schülers, kein Abgabe-Nachweis der Schule.
2. **Das Foto taugt.** Es zeigt, *dass* gearbeitet wurde, und ist gegen die Buchseite nachprüfbar, *ob* richtig.

Die Idee: Eine Schul-Hausaufgabe wird in Pugling zur **Aufgabe „Hausaufgabe machen“ mit Frist**. Das Kind erledigt sie,
indem es **ein oder mehrere Fotos** seines Hefts hochlädt. Der Vater sieht die Fotos und nimmt ab: angenommen →
Münzen, Nacharbeit nötig → zurück ans Kind mit einer Zeile Begründung. Ist bis zur Frist nichts hochgeladen, greift der
**Malus** wie bei jeder verpassten Pflicht. Das ist der „Stick“ aus der CLAUDE.md, diesmal für die Schule statt für die
App-Übungen.

## User Story (Entwurf)

Als **Supervisor** möchte ich, dass mein Kind jede Schul-Hausaufgabe mit einem Foto des Hefts als erledigt meldet, und
dieses Foto abnehmen oder zurückgeben. Dann weiß ich, ob die Hausaufgabe wirklich gemacht ist, und belohne bzw. bestrafe
sie wie die übrigen Pflichten.

Als **Student** möchte ich meine Hausaufgaben in der App sehen und mit einem Handyfoto abhaken, damit ich dafür Münzen
bekomme und keinen Malus kassiere.

## Bausteine (Skizze, ungeprüft)

- **Woher kommt die Hausaufgabe?** Bevorzugt aus WebUntis ([B-654](B-654-webuntis-connector-backend.md), Text +
  Fach + Fälligkeit), sonst vom Vater von Hand angelegt. Ohne B-654 funktioniert die Story auch, nur mit Abtippen.
- **Erledigen (Sohn-Arcade):** Karte „Hausaufgabe: Mathe – S. 80 Nr. 1+2, S. 82 Nr. 12 · fällig morgen“ → Kamera
  öffnen (`<input type="file" accept="image/*" capture="environment">`) → 1–n Fotos → „Abgeben“.
- **Abnehmen (Vater-Web):** Liste offener Abgaben mit Fotos in groß; Knöpfe *Annehmen* / *Nacharbeit* (mit Notiz).
  Die Notiz sieht das Kind an der Karte („12d fehlt noch“).
- **Belohnung:** Annahme schreibt Münzen gut wie eine erfüllte Pflicht; Frist verpasst ohne Abgabe → Malus wie eine
  verpasste Pflicht. Nacharbeit verschiebt die Frist nicht von selbst.
- **Optional, Creator-seitig: Korrekturvorschlag.** Claude liest das Foto gegen die aufbereitete Buchseite und schlägt
  eine Bewertung vor, genau wie am 2026-11-19 im Chat. Das läuft **nicht im Backend** (kein LLM dort; das lokale
  Vision-Modell liest laut [B-459](B-459-vision-modell-fuer-buchseiten.md) nicht einmal Buchseiten sicher), sondern als
  Skill/Agent, der über die API liest und eine Bemerkung zurückschreibt. Der Vater entscheidet.

## Abgrenzung zu Nachbar-Stories

- [B-654](B-654-webuntis-connector-backend.md)/[B-655](B-655-webuntis-hausaufgaben-vater-web.md)/[B-656](B-656-ki-creator-uebung-aus-hausaufgabe.md):
  Hausaufgabe → *Übung im Katalog* (Stoff üben). Diese Story: Hausaufgabe → *Nachweis, dass die Schulaufgabe gemacht ist*.
  B-651 nannte „Hausaufgaben als Pflichtziel mit Münz-Malus“ ausdrücklich als späteren Schritt – das ist er.
- [B-684](B-684-korrigierte-klausur-einscannen.md): ebenfalls Foto von Papier, aber die *korrigierte Klausur*, und der
  Vater scannt. Hier fotografiert das **Kind**, regelmäßig. Speicherort und Datenschutz der Fotos sollten beide
  Stories gleich lösen.
- [B-582](B-582-probearbeit-auf-papier-mit-foto-korrektur.md) (verworfen): Foto-Korrektur einer Probearbeit; der dort
  formulierte Grundsatz „Korrektur beim Vater, KI nur als Vorschlag“ gilt hier genauso.
- [B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md): passt zum Rollenschnitt – der Vater belohnt und
  bestraft die Schulpflicht, unterrichtet wird woanders.

## Offene Fragen (je mit Empfehlung)

1. **Ist „Hausaufgabe machen“ ein neuer `ExerciseType` in einer `PlanPosition` oder eine eigene Sache am Kind?**
   Empfehlung: eigene Ressource am Kind (`supervisor/children/{id}/homework`, `student/homework`). Eine Katalog-Übung
   ist wiederverwendbar und hat eine prüfbare Antwort; eine Hausaufgabe gilt einmal, für ein Kind, mit einer Frist und
   ohne Antwortschlüssel. Die Pflicht-/Malus-Mechanik soll aber **wiederverwendet**, nicht nachgebaut werden – beim
   Ausformulieren nachsehen, wie weit das ohne Position geht.
2. **Was zählt als „erledigt“ für die Frist: das Hochladen oder die Annahme?** Empfehlung: das Hochladen. Sonst hinge
   der Malus des Kindes an der Reaktionszeit des Vaters.
3. **Wie viel ist eine Hausaufgabe wert?** Empfehlung: fester Betrag je Hausaufgabe (Familien-Einstellung), nicht je
   Aufgabe frei – der Vater soll beim Abnehmen nicht rechnen müssen.
4. **Anti-Cheat: altes oder fremdes Foto?** Empfehlung: nur die Kamera-Aufnahme anbieten, Aufnahmezeit mitspeichern,
   den Rest leistet die Abnahme durch den Vater. Keine EXIF-Forensik.
5. **Wo liegen die Fotos, wie lange?** Empfehlung: im Medien-Speicher der App (nicht im Repo), mit Löschung nach einer
   Frist (z. B. Ende des Schulhalbjahres). Beim Ausformulieren prüfen, was der bestehende Medien-Pfad
   (Medien-Anti-Cheat, `backend/Pugling.Api/CLAUDE.md`) für Uploads des Kindes schon hergibt.
6. **Teilweise erledigt (wie 12d)?** Empfehlung: kein Teilpunkte-System; der Vater gibt mit „Nacharbeit: 12d fehlt“
   zurück, das Kind lädt ein weiteres Foto nach.
7. **Gilt das für beide Söhne?** Theodors Schule (AVS) gibt dem Elternkonto Hausaufgaben frei, Prüfungen nicht – für
   die Hausaufgaben selbst reicht das. Beim Ausformulieren bestätigen.

## Verlauf

- **2026-11-19** — angelegt aus dem Chat mit dem Vater (Friedrichs Mathe-Hausaufgabe per Foto nachgeprüft); Prio P2
  als Vorschlag, User Story und offene Fragen als Material fürs Ausformulieren.
