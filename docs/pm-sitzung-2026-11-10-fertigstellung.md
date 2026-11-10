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

## Iteration 29 — umgesetzt

- **Backend (API-First):** B-451 – 45 Uhr-Stellen in 14 Dateien auf den injizierten `TimeProvider`, Stempel von
  Buchungen und Sitzungen ausdrücklich aus der Uhr, `TestClock.FreezeAt`. B-04 – `PlanPosition.NewItemsPerDay`
  (Migration neu gefaltet, Snapshot-Diff genau eine Spalte), Dosis in der Lernrunde, Tagesportion im Test samt
  Einführen des kalt Gefragten, Validierung (0–200, nur mit Leitner, nicht auf ungeordneten Listen),
  `PositionStatus.IntroducedCount`. B-440 – Apostroph-Varianten gefaltet.
- **Frontend:** B-452 „Liste einfügen" im Übungs-Formular; B-04 Feld „Neue Inhalte je Tag" (Positions-Formular,
  Assistent mit Vorgabe 10), „x von y schon kennengelernt" beim Kind; B-454 Üben-Knopf auch ohne Leitner-Kasten.
- **Verifikation:** Backend **1211/1211**, Vitest **741/741**, `tsc -b` sauber, E2E **134 passed** (darunter neu
  `ziel-vier-wochen`, `vokabelliste-einfuegen`, `ueben-ohne-leitner`), 14 Mutanten rot am benannten Test (Liste
  in B-04), `pugling-reviewer` und `frontend-reviewer` gelaufen. Commit `595b429`.

## Runde 2 — Re-Review / Abnahme (Rollengang nach Sprint 29)

Der Rollengang ist maschinell und wiederholbar: `VierWochenTests.VierWochen_TaeglichEinePortion_…` spielt das Kind
28 Tage über die Student-API auf vorgestellter Uhr; `e2e/ziel-vier-wochen.spec.ts` fährt Vater und Kind durch die
echte Oberfläche (Server von Playwright nach dem letzten Edit gestartet).

- **Sohn – abgenommen.** 120 Wörter, 8 neue je Tag: jeden Tag höchstens 70 abgefragte Karten, ein Test über 1–60
  Wörter, bestanden, Pflicht erfüllt; der eine ausgelassene Tag kostet genau einen Malus. In der Oberfläche: Tag 1
  zeigt vier von zwölf Wörtern mit Lösung, die Startseite sagt „4 von 12 schon kennengelernt". Der Gang hat einen
  Defekt gefunden, bevor abgenommen wurde: ohne Leitner-Kasten gab es nur „TEST" (B-454, behoben).
- **Vater – abgenommen.** Liste einfügen → Übung → Plan über 28 Tage → eine Position mit Tagesdosis, alles in der
  Oberfläche; der Report der Position nennt „4/12 eingeführt" (Sprint 30).
- **Creator – Regressionszeuge.** Eigene Specs grün (`creator-lehrwerk-weg`, `uebungstypen`, `lehrwerke`); sein
  Formular hat „Liste einfügen" dazubekommen, sonst kein Pfad von ihm im Diff.
- **Offen bei einem Menschen** (kein Befund des Gangs, aber nicht maschinell urteilbar): siehe „Liste für den
  Menschen" unten.

**Abstand zum Ziel nachher: 0 offene Glieder von 5**, gemessen an Simulation und Rollengang – mit den Vorbehalten
der Liste für den Menschen.

## Sprint 30 — Ziel & Umfang

**Sprint-Ziel:** *Der Vater sieht in der Oberfläche, wie weit sein Kind im Stoff ist, und die Startseite stimmt
direkt nach dem Anlegen des Kindes.* **Umfang:** B-453 (XS) und die Vater-Zeile im Ziel-Rollengang.
**Umgesetzt:** `today.reload()` nach dem Anlegen (rote Probe: ohne die Zeile fällt `ziel-vier-wochen.spec.ts:28`),
Report-Zusicherung „4/12 eingeführt". Commit `1079a96`. Rollengang: dieselbe Spec, grün.

## Retrospektive

**Nachschau:** Sprint 28 (B-426, B-428, B-431, B-450) nachgesehen von einem frischen `frontend-reviewer` an den
Commits `48173c0`/`7e98879`. B-428 und B-450 sauber; B-426 ein kleiner Defekt (Reihenfolge entscheidet über die
Ablehnung eines unbekannten Typs → [B-455](backlog/B-455-fachlehrer-typen-pruefung-nach-dem-entdoppeln.md),
`entgangen_bei: [B-426]`); B-431 ohne Defekt, Reste in [B-456](backlog/B-456-nachschau-reste-sprint-28.md). Beide
dienen dem Ziel nicht und bleiben nach der Vollmacht dieses Laufs als `idee` liegen. `nachgeschaut: 2026-11-10` an
allen vier.

**Was die eigenen Tore durchgelassen haben:** nichts nach der Abnahme – aber zwei Dinge erst spät. (1) Der
Anti-Cheat-Fehler der Tagesportion (wer nie übt, wird ewig auf dieselben Wörter geprüft) fiel keinem meiner Tests
auf, weil die Simulation ein kooperatives Kind spielt; gefunden hat ihn der `pugling-reviewer`. (2) B-454 fand der
Rollengang, nicht die Suite. Beides sind die Tore, die es dafür gibt, und sie haben gehalten.

**Mechanismus:** kein neuer. Die Simulation `VierWochenTests` ist ein Test, kein Verfahren; der Fall „Kind, das nicht
übt" steht jetzt als eigener Test neben dem kooperativen. Ein Tor „jede Mehrtages-Regel braucht den unwilligen
Fall" wäre der naheliegende neue Mechanismus – die Vollmacht erlaubt einen je drei Sprints gegen Streichung eines
bestehenden; ich lande ihn nicht, weil ein Reviewer-Auftrag („spiele das Kind, das die Regel umgehen will") dasselbe
leistet und schon im Brief dieses Sprints stand.

**Bewusst ungedeckt:** der Zeitfenster-Multiplikator hängt in der Simulation an der Zeitzone des Hosts; Punktzahlen
sichert sie darum nicht zu. Die CI läuft auf einem Branch-Push nicht von selbst (`ci.yml` hört auf `main`, PRs und
`workflow_dispatch`) – gestartet per `gh workflow run ci.yml --ref auto/fertigstellung`.

## Produktentscheidungen dieses Laufs (zum Umstoßen)

1. **Tagesdosis als eigenes Feld, Vorgabe 10 im Assistenten** (B-04, 1 und 6).
2. **Mit Dosis prüft der Test die Tagesportion, nicht den Bestand** (B-04, 2) – die Gesamtprüfung fehlt damit bis
   zu einer Probeklausur (B-249).
3. **Der Test führt ein, was er kalt abfragt; wer nicht übt, bekommt täglich einen längeren Test** (B-04, 8).
4. **Dosis nur mit Leitner-Kasten, nicht auf ungeordneten Listen** (B-04, 9).
5. **„Im Test gekonnt → nie wieder" ist verworfen, das `KeyResult` „Pool durch bis Datum" zurückgestellt** (B-04, 3–4).
6. **Die Vokabelliste gehört ins Übungs-Formular; Komma ist kein Trenner** (B-452, 1–2).
7. **Üben steht dem Kind immer offen, sobald die Position Inhalte hat** (B-454, 1).
8. **Apostroph-Varianten sind für den Grader ein Zeichen; Akzente bleiben streng** (B-440) – „ecole" für „école"
   ist weiter falsch, weil der Akzent zum Französischen gehört.
9. **Die Uhr der API ist der `TimeProvider`, die Token-Laufzeit bleibt an der Wanduhr** (B-451, 1–2).

## Liste für den Menschen

- **Deine `pugling.db` startet nicht mehr** (neu gefaltete Migration, kein Upgrade-Pfad – Regel des Repos). Ich
  habe sie nicht angefasst; neu anlegen oder löschen ist deine Entscheidung.
- **Fühlt sich die Portion richtig an?** 8–10 neue Wörter plus Wiederholung sind in der Simulation höchstens 70
  Karten und ein Test bis 60 Wörter an Spitzentagen. Ob das für einen Neuntklässler 15 oder 40 Minuten sind, misst
  nur ein Kind.
- **Tippen von é, è, ç, œ auf dem echten Gerät** (Tablet- oder Handy-Tastatur) – der Grader verlangt sie.
- **Ton der neuen Texte:** „4 von 12 schon kennengelernt", die Feldhilfe „Neue Inhalte je Tag", die Meldungen der
  Listen-Eingabe.
- **Wochenenden und Ferien:** „täglich" heißt sieben Tage; ein freier Sonntag kostet heute den Malus.
- **Was nach vier Wochen passiert:** der Plan endet, das Kind hat keine Pflicht mehr – einen Folgeplan legt der
  Vater an.
- **Klang, Haptik, Animation** der Kennenlernrunde (B-31 unverändert offen).
- **Die zwei Validierungsmeldungen zur Dosis sind englisch** (Server-Detail); die Oberfläche verhindert beide Fälle,
  per API sieht man sie.

## Offene Roadmap

[docs/backlog/](backlog/README.md). Aus diesem Lauf als `idee` liegen geblieben, weil sie dem Ziel nicht dienen:
B-455, B-456. Zurückgestellt aus B-04: `KeyResult` „Pool durch bis Datum", `IntroducedCount` für vergangene Tage,
Scope „nur alte" mit Dosis. Nicht nach `main` gepusht; Branch `auto/fertigstellung`.
