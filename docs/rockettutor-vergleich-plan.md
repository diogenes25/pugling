---
tags: [typ/plan, bereich/klassenarbeit, bereich/training, bereich/auswertung, rolle/supervisor, rolle/student, rolle/creator]
aliases: [RocketTutor-Vergleich, RocketTutor, Probearbeit auf Papier, Erklärung nach dem Fehler]
---

# RocketTutor als Vergleichsmaßstab: was wir übernehmen, was nicht

Status: **Plan** (2026-11-15). Die Übernahme läuft als Stories im [Backlog](backlog/README.md); die
Sammel-Story ist [B-541](backlog/B-541-rockettutor-vergleich-sammel-story.md). Dieses Dokument hält den
Vergleich und die Reihenfolge, **nicht** den Zustand der Etappen.

← [Zurück zur Wissenskarte](obsidian.md) · Vorgänger-Vergleiche: [Khan Academy](khan-academy-vergleich-plan.md),
[Knowunity](knowunity-vergleich-plan.md), [Baobab](baobab-vergleich-plan.md),
[Duolingo/Snipwise](duolingo-snipwise-vergleich-plan.md) · Fachmodell: [grundprinzip.md](grundprinzip.md)

## Warum dieser Vergleich

[RocketTutor](https://www.rockettutor.de/) ist eine Web-Lernplattform der Rocket Tutor GmbH für Klasse 5
bis Abitur. Der Schwerpunkt ist Mathe, dazu kommen Englisch, Physik, Chemie und Biologie. Hervorgegangen
ist die Plattform aus einem Forschungsprojekt der TU München, deren Lehrstuhl für Mathematikdidaktik sie
weiter begleitet. Eigenangaben: über 100.000 Nutzer, ein Auftritt in „Die Höhle der Löwen", Kofinanzierung
durch Bund und EU. Käufer sind Eltern (Premium-Abo) und Schulen (Schullizenz ab 11 € pro Jahr).

RocketTutor ist das Gegenstück zu Baobab: keine Spielelemente, keine Belohnung, kein Eltern-Cockpit. Der
Antrieb ist allein **die Note in der nächsten Arbeit**. Damit liegt RocketTutor genau dort, wo Pugling am
dünnsten ist (Termin, Probearbeit, Note), und nicht dort, wo Pugling stark ist (Pflicht, Münzen, Shop).

Quellenstand: Startseite, Eltern-, Lehrkräfte- und Creator-Seite sowie die TUM-Projektseite, abgerufen am
2026-11-15 (Links am Ende). Die App selbst wurde nicht mit Konto durchgespielt. Aussagen über RocketTutor
sind Anbieterangaben, unabhängige Erfahrungsberichte waren nicht zu finden.

## Was RocketTutor anbietet

- **Lernplan nach Bundesland, Klasse und Schulbuch.** Erklärvideos (11.000+), interaktive Aufgaben
  (14.000+) und ein Abschlusstest nach jeder Lektion.
- **KI-Tutor „Sophia".** Erklärt im Dialog Schritt für Schritt und ist rund um die Uhr erreichbar.
- **Korrektur des Lösungswegs Zeile für Zeile**, nicht nur des Endergebnisses. Erkannte Lücken passen den
  Lernpfad an.
- **Gratis-Probearbeit.** Der Schüler wählt Themen, bekommt eine PDF mit Lösungen, löst auf Papier,
  fotografiert das Ergebnis und bekommt eine Korrektur mit Punkten **und Note**.
- **Klausur- und Wochenplaner.** Verteilt Zeit und Aufgaben bis zum Prüfungstermin (Premium).
- **Prüfungs-Simulator.** Ein Wissenstest nach jedem Unterkapitel, Abitur-Simulation mit Originalaufgaben
  je Bundesland (Premium).
- **Lehrkräfte.** Klassen per QR-Code, Link oder E-Mail. Hausaufgaben per Schlagwortsuche, mit sichtbarem
  Vorwissen sowie Start- und Abgabedatum. Eine Übersicht zeigt die größten Wissenslücken je Klasse und je
  Schüler.
- **Eltern.** Nur eine Marketingseite und „Fortschritt jederzeit sichtbar". Ein eigenes Eltern-Werkzeug
  wird nicht beschrieben.

## Gemeinsamkeiten

| RocketTutor | Pugling | Bewertung |
| --- | --- | --- |
| Lernplan nach Schulbuch | `TextbookSeries → SeriesUnit`, Kind zeigt auf sein Buch, Lehrwerk aus Fotos (Skill `lehrwerk-aufbereiten`) | Gleichwertig; RocketTutor bestätigt die Richtung. |
| Abschlusstest je Lektion | Abschlusstest je Position, Klausur-Modus one-at-a-time | Gleichwertig. |
| Lücken erkennen, gezielt üben | Leitner-Stufe je Item, `onlyWeak` im Lernstand (`ChildVocabularyProgressController.cs:47`), `PracticeOrder.WeakestFirst` als Default (`PlanPositionsController.cs:193`) | Wir erkennen die Lücke. Der Weg von der Lücke zur Pflicht fehlt (→ B-544). |
| Lehrer-Hausaufgabe mit Frist | Lehrer-Konto ohne Betreuungsauftrag, Hausaufgaben in [B-09](backlog/B-09-lehrer-hausaufgaben.md) ausformuliert | RocketTutor ist weiter; der QR-Beitritt ist ein günstiger Weg in B-09. |

## Wo RocketTutor weiter ist

1. **Termin treibt den Plan.** Ein Planer verteilt die Arbeit rückwärts vom Prüfungstermin. Das ist der
   dritte Beleg von außen für [B-248](backlog/B-248-klassenarbeit-countdown-treibt-den-plan.md), nach
   Knowunity und Baobab.
2. **Papier als Prüfungsform.** Die Probearbeit wird geschrieben wie in der Schule und erst danach
   fotografiert. Friedrichs Vokabeltest-Format ([B-524](backlog/B-524-vokabeltest-wie-in-der-schule.md))
   zeigt, dass genau diese Form zählt.
3. **Ergebnis als Note.** Punkte und Note statt nur Punkte. [B-249](backlog/B-249-probeklausur-mit-notenprognose.md)
   hat das entschieden, aber noch nicht gebaut.
4. **Erklärung beim Fehler.** Der Tutor erklärt, *warum* eine Antwort falsch ist. Pugling zeigt nach der
   Antwort nur `Expected`. Am Item gibt es einen `Hint` (`ExerciseItemEntities.cs:34`), aber keine
   Erklärung.
5. **Lücken-Übersicht als Arbeitsliste.** Die Lehrer-Sicht „größte Lücken" ist der Startpunkt einer
   Zuweisung, nicht nur eine Anzeige.

## Wo Pugling stärker ist

- **Pflicht mit Konsequenz.** Pflichtziele, `PenaltyCoins`, Klassenarbeiten. RocketTutor setzt allein auf
  den Notendruck von außen.
- **Belohnung.** Münzen, Familien-Shop, Kisten. Laut [B-535](backlog/B-535-belohnungen-erreichen-friedrich-nicht.md)
  ist das Friedrichs stärkster Hebel; RocketTutor hat nichts davon.
- **Mehrere Betreuer, gemeinsames Wallet**, Eltern als steuernde Rolle statt als Zuschauer.
- **Anti-Cheat.** Server-autoritativ, Lösungsfelder vor dem Studenten gegated. Ein Dialog-Tutor rund um
  die Uhr ist zugleich ein Hausaufgaben-Löser.
- **Kein Abo.**

## Bewusst nicht übernommen

- **Dialog-Tutor beim Kind.** Bricht „kein LLM im Backend" und die Lösungsfeld-Regel. Übernommen wird nur
  die Offline-Form: Erklärungen entstehen beim Creator und sind vor dem Zuweisen prüfbar (B-543).
- **KI korrigiert beim Kind.** Dieselbe Entscheidung wie im Baobab-Vergleich. Die Probearbeit auf Papier
  korrigiert der Vater bzw. ein Creator-Werkzeug, das Ergebnis trägt der Vater ein (B-542).
- **Erklärvideos.** Content-Aufwand ohne Bezug zum Vokabel-Schwerpunkt.
- **Mathe-Lösungsweg Zeile für Zeile.** Wäre ein eigener Übungstyp mit eigener Bewertung; erst
  anzugehen, wenn Mathe für Friedrich ein eigenes Vorhaben wird.
- **Abo- und Creator-Influencer-Modell.**

## Übernahmeplan

Backend zuerst (API-First); jede Story läuft die normale Kette im Backlog. Die Beschreibung hier ist der
Anlass, nicht der Ist-Stand. Beim Ausformulieren wird gegen den Code belegt.

| Etappe | Story | Was | Hängt ab von |
| --- | --- | --- | --- |
| 1 | [B-248](backlog/B-248-klassenarbeit-countdown-treibt-den-plan.md) (bestehend) | Klassenarbeit-Termin verteilt die Tagesdosis rückwärts | [B-423](backlog/B-423-assistent-klassenarbeit-termin-und-stufenleiter.md) |
| 2 | [B-249](backlog/B-249-probeklausur-mit-notenprognose.md) (bestehend) | Probeklausur mit Note und Vergleich Prognose ↔ echte Note | B-248 |
| 3 | [B-542](backlog/B-542-probearbeit-auf-papier-mit-foto-korrektur.md) | Probearbeit als PDF, Lösung auf Papier, Foto → Korrektur durch Vater/Creator-Werkzeug → Note | B-249; Vision-Qualität aus [B-459](backlog/B-459-vision-modell-fuer-buchseiten.md) |
| 4 | [B-543](backlog/B-543-erklaerung-nach-falscher-antwort.md) | Erklärung je Übung/Item, offline vom KI-Creator erzeugt, nach der Antwort gezeigt | keine |
| 5 | [B-544](backlog/B-544-schwache-woerter-werden-wiederholungs-position.md) | Schwache Wörter mit einem Griff zur Wiederholungs-Position mit Pflichtziel | berührt [B-39](backlog/B-39-supervisor-dashboard.md) |

Dazu bekommt [B-09](backlog/B-09-lehrer-hausaufgaben.md) den QR-Code/Link als Beitrittsweg als
Anregung für das Grillen, ohne neue Story.

Etappen 1 und 2 sind der Kern, und sie sind schon im Backlog: RocketTutor ändert nicht *was*, sondern
stützt *dass* Termin und Note zuerst kommen. Etappe 3 hängt die Schulform daran (Papier statt Bildschirm).
Etappe 4 ist die einzige Übernahme aus dem KI-Tutor, die zu Pugling passt.

## Quellen

- [rockettutor.de](https://www.rockettutor.de/) · [Eltern](https://www.rockettutor.de/parents) ·
  [Lehrkräfte](https://www.rockettutor.de/teacher) · [Creator-Programm](https://www.rockettutor.de/creators-program)
- [TUM EdTech: RocketTutor](https://www.edtech.tum.de/rockettutor/) (Herkunft, didaktische Begleitung)
- [Trustpilot rockettutor.de](https://ca.trustpilot.com/review/rockettutor.de) (nur Selbstdarstellung, keine Kundenbewertungen)

---

**Verwandt:** [baobab-vergleich-plan.md](baobab-vergleich-plan.md) ·
[knowunity-vergleich-plan.md](knowunity-vergleich-plan.md) ·
[khan-academy-vergleich-plan.md](khan-academy-vergleich-plan.md) · [docs/backlog/](backlog/README.md)
