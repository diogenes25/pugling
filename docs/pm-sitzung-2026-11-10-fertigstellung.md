---
tags: [typ/protokoll, bereich/pm, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Fertigstellungs-Lauf, Rollengang 2026-11-10, Abstand zum Ziel]
---

# PM-Sitzung: Fertigstellung – vier Wochen Französisch-Vokabeln ohne API von Hand

**Datum:** 2026-11-10  ·  **Moderation:** PM (Fable-Sitzung, autonom)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (9. Klasse, Student) · Entwickler
**Ziel (Auftrag des Nutzers):** *Pugling ist fertig, wenn ein Kind der 9. Klasse vier Wochen lang täglich
Französisch-Vokabeln üben kann, der Vater Pflichtziele setzt und den Fortschritt sieht, und kein Schritt davon
die API von Hand braucht.* Alles, was diesem Ziel nicht dient, ist nicht Teil des Auftrags.

**Vollmacht dieses Laufs** (ersetzt für diesen Auftrag die Grenzen aus `docs/backlog/README.md` „Der
Backlog-Lauf" und `pm-loop` Step 8): der PM entscheidet auch `Wunsch`/`Frage`, sofern sie dem Ziel dienen
(Begründung und Kosten in der Story); kein Halt bei neuem Tor oder Nachschau-Defekt; höchstens ein neuer
Mechanismus je drei Sprints, je neuem wird ein bestehender gestrichen; Push je Sprint auf
`auto/fertigstellung`, nie auf `main`. Abbruch: Rollengang ohne Befund, oder zwei Sprints ohne kleineren Abstand.

**Aufbau:** Branch `auto/fertigstellung` ab `main` (`295c686`). Backend gegen eine frische Wegwerf-DB
(`pugling_ziel.db`, Launch-Konfiguration `backend-ziel`), Frontend `npm run dev`, Rollengang im eingebauten
Browser mit einem **neu registrierten** Vater und einem neu angelegten Kind (Klasse 9) – nicht mit der
Demo-Familie. Beobachtungen danach am Code nachgelesen.

## Runde 1 — Rollengang 0: Abstand zum Ziel

Der Maßstab ist die Zielkette, nicht die Fläche der App: **(a)** Stoff für vier Wochen kommt hinein,
**(b)** der Vater macht daraus eine tägliche Pflicht, **(c)** das Kind kann sie 28 Tage lang jeden Tag
erfüllen, **(d)** der Vater sieht den Fortschritt, **(e)** nichts davon braucht die API.

### Feedback Vater (beobachtet, Konto frisch registriert)

**Geht:** Registrieren, Kind mit Klasse 9 und PIN anlegen, Werkstatt/Katalog/Lehrwerke/Vokabeln erreichen,
Sprachpaar Französisch→Deutsch wählbar, Assistent mit frei wählbarer Dauer (`VaterWizard.tsx:601`), jede
gewählte Übung wird ein Tagesziel. Den Weg „von Null bis zum spielenden Kind" fährt `vater-von-null.spec.ts`.

**Stört / fehlt:**

- **Z1 – Es gibt keine Tagesdosis.** Ich will „150 Vokabeln der Lektionen 1–3, jeden Tag ein Stück". Eine
  Position spielt aber am ersten Tag **jedes** nie gesehene Wort aus (`PositionPlayService.cs:116-118`, „never
  seen counts as due"); `ItemCount` schneidet nur ab. Mein Ausweg wäre: jede Woche von Hand eine neue kleine
  Übung zuweisen und die alten umstellen. Das ist die Pflicht des Vaters, nicht die des Kindes. → [B-04](backlog/B-04-adaptiver-vokabel-pool.md)
- **Z2 – Vokabeln tippe ich Zeile für Zeile.** „+ Zeile", Wort, Übersetzung, 150-mal (`VaterVocab.tsx`). Die
  Liste aus dem Buch oder vom Lehrer kann ich nicht einfügen. → [B-452](backlog/B-452-vokabelliste-einfuegen.md)
- **Z4 – Fortschritt über vier Wochen:** Tagesverlauf und Report je Position gibt es; „wie viele der 150 Wörter
  sind schon dran gewesen" steht nirgends. → Teil von B-04 (AK 4).
- **Z6 –** Nach „Kind anlegen" sagt die Karte „Heute" weiter „Noch keine Kinder", bis ich neu lade. → [B-453](backlog/B-453-heute-karte-nach-kind-anlegen.md)

### Feedback Sohn (aus dem Code gelesen, Gang folgt nach Sprint 1)

- **Z1 aus Kindersicht:** Tag 1 wären 150 Karten Erstbegegnung und danach 150 Karten Abfrage; der Test fragt
  alle eingeführten Wörter (`PositionTestsController.cs:176-179`) – nach vier Wochen jeden Tag alle.
- **Z7 – Französisch auf dem Tablet:** „l’école" mit typografischem Apostroph ist falsch, „école" richtig
  (`StageMechanics.cs:31-32`). → [B-440](backlog/B-440-typografischer-apostroph-wird-nicht-gefaltet.md), für dieses Ziel kein P3 mehr.

### Feedback Creator

Kein eigener Befund über die vom Rollengang 2026-11-08 hinaus; die Reihe „Découvertes 1" ist Klasse 8, eine
Reihe/Unit für Klasse 9 legt der Vater in der Oberfläche selbst an. Z2 trifft ihn wie den Vater.

### Entwickler

- **Z3 – „Vier Wochen" ist nicht messbar.** Die Uhr ist an rund 50 Stellen in Controllern und Services
  `DateTime.UtcNow`; nur ein Controller nutzt den injizierten `TimeProvider`. Kein Test kann 28 Tage spielen,
  also kann kein Sprint belegen, dass (c) hält. → [B-451](backlog/B-451-uhr-und-vier-wochen-simulation.md)

## PM-Synthese & Priorisierung

**Abstand zum Ziel vorher: 4 offene Glieder von 5.** (a) offen – Z2; (b) halb – Pflicht ja, Dosis nein;
(c) offen und unmessbar – Z1, Z3, Z7; (d) halb – Z4; (e) erfüllt, solange man Z2 „tippen" gelten lässt.

| Prio | Story | Glied | Wo | Größe |
| --- | --- | --- | --- | --- |
| P0 | [B-451](backlog/B-451-uhr-und-vier-wochen-simulation.md) Uhr über `TimeProvider` + 28-Tage-Simulation | c (Messung) | backend | M |
| P0 | [B-04](backlog/B-04-adaptiver-vokabel-pool.md) Neue Wörter je Tag | b, c, d | beides | M |
| P1 | [B-452](backlog/B-452-vokabelliste-einfuegen.md) Vokabelliste einfügen | a | frontend | S |
| P1 | [B-440](backlog/B-440-typografischer-apostroph-wird-nicht-gefaltet.md) Apostroph falten | c | backend | XS |
| P3 | [B-453](backlog/B-453-heute-karte-nach-kind-anlegen.md) „Heute" nach Kind anlegen | – | frontend | XS |

**Roter Faden:** *„Eine Pflicht, vier Wochen, jeden Tag eine machbare Portion – und ein Test, der das belegt."*

## Sprint 29 — Ziel & Umfang

**Sprint-Ziel:** *Der Vater gibt eine Übung mit 150 Französisch-Vokabeln als **eine** tägliche Pflicht auf, und
das Kind bekommt 28 Tage lang jeden Tag eine Portion, die es schaffen kann – ohne dass der Vater nachsteuert.*
Widerlegbar an der 28-Tage-Simulation (Karten je Tag, Test je Tag, Malus) und am Gang des Vaters durch das
Positions-Formular.

**Umfang:** B-451, B-04.

**Entwickler-Brief:** Zuerst die Uhr (B-451, reines Aufräumen, Suite bleibt grün), dann die Simulation als
roter Test gegen den heutigen Stand, dann B-04 im Backend (ein neues Feld `NewItemsPerDay` an der Position,
Migration neu gefaltet, Auswahl in `PositionPlayService`, Tagesportion im Test), dann Formular und Assistent.
Quelle der Wahrheit ist der Server: die Dosis rechnet er, der Client zeigt nur. Guards: Kind kann die Dosis
nicht umgehen (Info-Modus zählt nicht als Einführung, bleibt wie er ist).
