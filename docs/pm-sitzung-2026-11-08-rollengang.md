---
tags: [typ/protokoll, bereich/pm, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Rollengang 2026-11-08, Konzept-Durchspiel, Nachtlauf-Pakete NP-24 bis NP-27]
---

# PM-Sitzung: das ganze Konzept durchgespielt – Creator, Vater, Sohn an der laufenden App

**Datum:** 2026-11-08  ·  **Moderation:** PM (Fable-Sitzung)
**Teilnehmer:** Creator (Lehrer-Konto „Herr Schmidt") · Vater (Supervisor „Papa") · Sohn (~11, Student „Sohn") · Entwickler
**Ziel:** Das Pugling-Konzept aus allen drei Rollen in der **laufenden App** durchspielen, die Handhabung
bewerten, Lücken gegen das Backlog abgleichen und die Arbeit so in Stories und Nachtlauf-Pakete schneiden,
dass ein günstigeres Modell sie unbeaufsichtigt bauen kann.

**Auftrag des Nutzers (wörtlich):** „Spiele das gesamte Pugling-Konzept in der App durch. Arbeite als Vater,
Sohn und Lehrer … Sind die Verbesserungen bereits im Backlog enthalten? … Erstelle entsprechende Tasks …
Überarbeite das Backlog soweit, dass die Tasks in Nachtläufen durchgeführt werden können."

**Aufbau:** Backend aus `main` (HEAD `22a4619`) gegen eine **Wegwerf-DB** (`pugling_walk.db`, Seed frisch,
Launch-Konfiguration `backend-walk` in `.claude/launch.json`), Frontend `npm run dev`; Rollengang im
eingebauten Browser, jede Beobachtung danach am Code mit `Datei:Zeile` nachgelesen. Die echte `pugling.db`
blieb unberührt.

## Runde 1 — Rollengang und Feedback

### Feedback Creator (O-Ton, Lehrer-Konto #2)

**Baut sich gut:** Die Werkstatt erklärt den Weg („erst die Unit"), das Anlege-Formular startet aus der Unit
heraus im richtigen Stoff (Fach, Reihe, Unit vorbelegt – per Skript geprüft), alle zwölf Typen stehen zur Wahl,
der Vokabelspeicher ist durchsuchbar, neue Wörter lege ich inline an, und **„Ausprobieren"** direkt nach dem
Anlegen zeigt mir die Übung so, wie das Kind sie bekommt – mit wählbarer Abfrageform. Das ist genau die
Standing-Bedingung des Creators („ohne JSON-Handarbeit anlegen und vorher testen") – **erfüllt.**

**Fehlt / nervt:**

- Im Fachlehrer-Profil stehen „VOCABULARY · CLOZE · TRANSLATION · GRAMMAR" – vier rohe Schlüssel, nicht die
  deutschen Namen, nicht alle Typen (`VaterFachlehrer.tsx:24`). → [B-426](backlog/B-426-fachlehrer-uebungstypen-aus-frontend-liste.md)
- In der Unit-Liste sehe ich nicht, welche Unit schon Übungen hat. → [B-428](backlog/B-428-unit-zeigt-uebungszahl-nicht.md)
- „Übungen verwalten" zeigt mir zuerst nichts („Wähle ein Fach"), obwohl die Werkstatt „3 eigene Übungen"
  zählt. → [B-431](backlog/B-431-uebungen-verwalten-startet-leer.md)
- Als Lehrer habe ich kein Profil, obwohl der Seed mich „Englischlehrer" nennt. → Teil von [B-429](backlog/B-429-seed-familie-widerspricht-dem-katalog.md)

**Top-3:** B-426 (Schlüssel statt Namen – sieht nach Baustelle aus), B-428, B-431.

### Feedback Vater (O-Ton, Konto #1)

**Gefällt:** Drei Perspektiven, klare Startseite („ohne Plan hat dein Kind keine Pflicht"), der Assistent in
fünf Schritten mit Intensitäts-Vorgaben (Locker/Normal/Intensiv samt Malus), die Plan-Seite mit
Tagesverlauf, Report je Position und der Warnung zum Anti-Cheat („nur der aktive Plan ist spielbar"). Shop mit
Artikeln, Angeboten, Anfragen; Belohnungen mit Missionen und Auszeichnungen; Kind-Hub mit Stammdaten,
Unterrichtsmaterial, Betreuern, Stundenplan, Interessen. **Ich kann alles in der UI – kein API-Gefummel.**

**Stört / fehlt:**

- Ich wähle „Klassenarbeit vorbereiten" – und niemand fragt mich **wann** die Arbeit ist. 14 Tage pauschal,
  Tipp-Stufe ab Tag 1 (`VaterWizard.tsx:46`), und unter „Klassenarbeiten" taucht nichts auf. Zwei Seiten, die
  dasselbe meinen, kennen sich nicht. → [B-423](backlog/B-423-assistent-klassenarbeit-termin-und-stufenleiter.md)
- In der Übungsauswahl sehe ich weder, wie viele Wörter eine Übung hat, noch kann ich sie ausprobieren – ich
  verpflichte mein Kind blind. → [B-424](backlog/B-424-uebungsauswahl-umfang-und-ausprobieren.md) (Bewertung fehlt
  dort auch: schon [B-408](backlog/B-408-assistent-zeigt-keine-bewertung.md))
- Im Kontostand steht „Willkommens-Gems +300" in der Spalte **Münzen**. → [B-425](backlog/B-425-kontoverlauf-gems-in-muenzspalte.md)
- Drei Datumsschreibweisen: „11/8/2026" im Hub und Konto, „2026-11-08" auf der Plan-Seite. → [B-427](backlog/B-427-datumsformat-uneinheitlich.md)
- Der Hub meines Kindes sagt „Green Line 1 – nicht katalogisiert" und „kein Profil passt", obwohl Reihe und
  Lehrer im Katalog stehen – der Seed selbst ist inkonsistent. → [B-429](backlog/B-429-seed-familie-widerspricht-dem-katalog.md)
- Die Stufenleiter (`StageSchedule`) kann der Server, aber ich sehe sie nirgends – weder im Assistenten noch im
  Positions-Formular (dort bewusst zurückgestellt, [B-16](backlog/B-16-positions-formular-umfang.md)). B-423 setzt
  sie aus dem Assistenten.

**Top-3:** B-423 (der Termin ist die Kernidee), B-424, B-425.

### Feedback Sohn (O-Ton, Kind #1, PIN-Login mit Ziffernblock)

**Mega:** Der Login fühlt sich nach Spiel an (Held wählen, Ziffernblock), die Basis zeigt Münzen, Gems,
Streak, Tagesmission mit zwei Knöpfen je Übung, Missionen mit Fortschritt; Shop mit echten Sachen von Papa
(10 Minuten TV für 50), Skins für Gems (den Fuchs könnte ich mir sofort leisten), Trophäenweg mit
Auszeichnungen, „Das bin ich" mit Interessen in vier Schritten. Der Test-Ergebnisbildschirm zeigt jedes Wort mit
der richtigen Lösung.

**Nervt (und zwar richtig):**

- **Ich soll Wörter tippen, die ich noch nie gesehen habe.** Erste Karte „la ville", leeres Feld. Fünf Karten,
  fünf Fehler, **+0 Münzen**, dann Test: 20 %. Und um Mitternacht kostet das Tagesziel 5 Münzen. → [B-419](backlog/B-419-erstbegegnung-neue-karte-erst-zeigen.md) (P1), Malus-Frage [B-432](backlog/B-432-erster-tag-einer-position-ohne-malus.md)
- Wenn ich falsch liege, springt sofort die nächste Karte, und die Lösung steht 1,6 Sekunden **darunter** –
  unter der falschen Frage. → [B-420](backlog/B-420-loesung-blitzt-als-toast.md)
- „Stadt" war falsch, weil „die Stadt" erwartet war. Das ist unfair. → [B-421](backlog/B-421-antwort-toleranz-artikel.md), Tippfehler-Frage [B-422](backlog/B-422-beinahe-treffer-tippfehler.md)
- Nach dem verpatzten Test kann ich nur „Nochmal versuchen" – nicht die falschen Wörter üben. → [B-430](backlog/B-430-nach-test-diese-woerter-ueben.md)
- Der Tipp zu „la ville" war „la". Danke für nichts. → Seed, [B-429](backlog/B-429-seed-familie-widerspricht-dem-katalog.md)

**Top-3:** B-419, B-420, B-421 – zusammen sind sie **ein** Erlebnis: „die App prüft, bevor sie lehrt, und
sagt mir dann zu kurz und zu streng, was richtig gewesen wäre".

### Was gut funktioniert (für die Akte, damit es nicht als Lücke wiederkommt)

Login aller drei Rollen; Plan anlegen per Assistent (Plan 2 mit zwei Tageszielen); der Durchstich Vater →
Sohn (Plan sofort in der Tagesmission); Üben (Sitzung, Heartbeat, Karten, Tipp), Test one-at-a-time mit
Ergebnisliste; Shop-Angebote beim Kind sichtbar; Übung anlegen aus der Unit heraus inkl. Vorschau; Daily
Overview beim Vater („OFFEN 0/2" nach dem Anlegen). Die E2E-Suite deckt diese Wege (`full-flow.spec.ts`,
`assistent.spec.ts`, `creator-lehrwerk-weg.spec.ts`).

## Die Fragen, die diese App beantworten muss (PM-Sicht)

Der Auftrag verlangte, selbst nach den Fragen zu suchen. Das sind die, an denen sich das Konzept entscheidet,
mit dem heutigen Stand:

| Frage | Stand nach dem Rollengang |
| --- | --- |
| **Lehrt die App, bevor sie prüft?** | **Nein.** Neue Wörter werden kalt getippt abgefragt (B-419). Das ist die wichtigste Lücke des Produkts: der „Stick" (Malus) trifft Nichtwissen, das die App selbst erzeugt hat. |
| Ist der Stick fair? | Teilweise. Pflicht, Bestehensgrenze und Malus sind klar beschriftet; aber exakte Wertung ohne Artikel-Toleranz (B-421) und der kalte Einstieg (B-419) machen ihn am ersten Tag willkürlich (B-432). |
| Erzwingt der Vater **Klausurvorbereitung**, die Kernidee? | Halb. Klassenarbeiten existieren mit Termin und Note, aber der Assistent kennt sie nicht (B-423) und das Kind sieht sie nicht (B-248, vom Nutzer entschieden, Paket NP-22). |
| Bauen die drei Ebenen aufeinander auf? | Ja, im Datenmodell und in der UI-Navigation (drei Perspektiven, Unit-zuerst). Bruchstellen sind Querverbindungen: Assistent↔Klassenarbeit, Auswahl↔Vorschau (B-424), Hub↔Katalog (B-429). |
| Kann der Lehrer ohne Hilfe Material bauen? | Ja. Rest: Sichtbarkeit (B-428, B-431) und ein Konventionsbruch (B-426). |
| Stimmen Zahlen, die der Vater sieht? | Nicht überall: Gems in der Münzspalte (B-425), drei Datumsformate (B-427). |
| Wird der Sohn **gefeiert**? | Der Rahmen ist da (Combo, Toast, Trophäenweg); den Erfolgs-Moment nach dem ersten Lernen gibt es erst, wenn er Punkte verdienen kann (B-419 → NewContentPoints in Runde 2). |
| Skaliert der Katalog? | Offen, nicht gemessen: die Vokabelauswahl im Editor listet den ganzen Store (12 Wörter heute). Mit Suche vorhanden; bei 2.000 Wörtern braucht sie Paging – keine Story heute, weil kein Beleg. |

## PM-Synthese & Priorisierung

**Beobachtung:** Das Produkt ist in der Fläche vollständig – jede Rolle kann alles in der UI. Die Lücken sind
nicht mehr „fehlt eine Seite", sondern **Lernmechanik auf Kartenebene** (Sohn) und **Querverbindungen**
zwischen Seiten, die dasselbe meinen (Vater). Der Sohn-Befund wiegt am schwersten, weil er die Kernidee
umkehrt: eine App, die Pflicht mit Malus erzwingt, muss vorher gelehrt haben.

| Prio | Story | Rolle | Wo | Größe | Stufe | Wer entscheidet noch |
| --- | --- | --- | --- | --- | --- | --- |
| P1 | [B-419](backlog/B-419-erstbegegnung-neue-karte-erst-zeigen.md) Erstbegegnung je Karte | Sohn | beides | M | geschaetzt | PM-Entscheidungen, Veto beim Nutzer |
| P2 | [B-420](backlog/B-420-loesung-blitzt-als-toast.md) Lösung hält an statt Toast | Sohn | frontend | S | geschaetzt | Defekt, autonom |
| P2 | [B-421](backlog/B-421-antwort-toleranz-artikel.md) Artikel-Toleranz | Sohn | backend | S | geschaetzt | PM-Entscheidungen, Veto beim Nutzer |
| P2 | [B-423](backlog/B-423-assistent-klassenarbeit-termin-und-stufenleiter.md) Assistent: Termin, Klassenarbeit, Leiter | Vater | frontend | M | geschaetzt | PM-Entscheidungen, Veto beim Nutzer |
| P2 | [B-424](backlog/B-424-uebungsauswahl-umfang-und-ausprobieren.md) Auswahl: Umfang + Ausprobieren | Vater | beides | M | geschaetzt | PM-Entscheidungen, Veto beim Nutzer |
| P3 | [B-425](backlog/B-425-kontoverlauf-gems-in-muenzspalte.md) Gems in Münzspalte | Vater/Sohn | beides | — | ausformuliert | Defekt, autonom |
| P3 | [B-426](backlog/B-426-fachlehrer-uebungstypen-aus-frontend-liste.md) Fachlehrer-Typen aus Manifest | Creator | frontend | — | ausformuliert | Defekt, autonom |
| P3 | [B-427](backlog/B-427-datumsformat-uneinheitlich.md) Ein Datumsformat | Vater | frontend | — | ausformuliert | Aufräumen, autonom |
| P3 | [B-428](backlog/B-428-unit-zeigt-uebungszahl-nicht.md) Übungszahl je Unit | Creator | beides | S | geschaetzt | PM-Entscheidungen, Veto beim Nutzer |
| P3 | [B-429](backlog/B-429-seed-familie-widerspricht-dem-katalog.md) Seed konsistent | alle | backend | — | ausformuliert | Aufräumen, autonom |
| P3 | [B-430](backlog/B-430-nach-test-diese-woerter-ueben.md) „Diese Wörter üben" | Sohn | frontend | XS | geschaetzt | PM-Entscheidungen, Veto beim Nutzer |
| P3 | [B-431](backlog/B-431-uebungen-verwalten-startet-leer.md) „Meine Übungen" zuerst | Creator | frontend | S | geschaetzt | PM-Entscheidungen, Veto beim Nutzer |
| P3 | [B-422](backlog/B-422-beinahe-treffer-tippfehler.md) Tippfehler-Toleranz | Sohn | backend | — | idee (Frage) | **nur im Dialog** |
| P3 | [B-432](backlog/B-432-erster-tag-einer-position-ohne-malus.md) Erster Tag ohne Malus? | Sohn/Vater | backend | — | idee (Frage) | **nur im Dialog** |

**Roter Faden:** *„Die App lehrt, bevor sie prüft – und der Vater weiß beim Zuweisen, worauf er verpflichtet."*

### Schon im Backlog (kein Doppel angelegt)

Geprüft mit `grep` über `docs/backlog/` und gegen die Backlog-Sichtung vom 2026-10-23:

- Bewertung im Assistenten → [B-408](backlog/B-408-assistent-zeigt-keine-bewertung.md) (läuft mit B-424 in einem Sprint).
- Klassenarbeit beim Kind, Countdown, Zusatzpflicht → [B-248](backlog/B-248-klassenarbeit-countdown-treibt-den-plan.md)/[B-249](backlog/B-249-probeklausur-mit-notenprognose.md) (Paket NP-22, Freigabe des Nutzers liegt vor). B-423 liefert ihnen die Entität.
- Stufenleiter im Positions-Formular → [B-16](backlog/B-16-positions-formular-umfang.md), dort bewusst zurückgestellt; B-423 setzt sie nur aus dem Assistenten.
- Zurückgezogene Übungen in der Auswahl → [B-140](backlog/B-140-freigabe-kennzeichen-in-der-uebungsauswahl.md) (NP-17).
- Learning Queue / „was kostet Nichtstun" → [B-220](backlog/B-220-learning-queue-im-sohn-dashboard.md) (NP-20).
- Mischwiederholung, verzeihende Abstufung, gestufte Hinweise → [B-218](backlog/B-218-mischwiederholung-ueber-alle-positionen.md)/[B-219](backlog/B-219-verzeihende-abstufung-je-position.md)/[B-223](backlog/B-223-gestufte-hinweise-vor-der-antwort.md) (NP-18) – **nicht** dasselbe wie B-419: dort geht es um Wiederholung und Hinweise, hier um den allerersten Kontakt.
- Arcade-Sperrcodes ohne Text → [B-415](backlog/B-415-arcade-sperrcodes-ohne-text.md); Position-entfernen-Dialog → [B-418](backlog/B-418-position-entfernen-verspricht-verlust.md).

**Nicht gefunden, darum neu:** alle 14 Stories oben. Keine davon war als Anmerkung oder Story vorhanden.

### Was der Rollengang bewusst nicht geprüft hat

Klang/Haptik (B-31, Gerät), Tablet-Breite (NP-5, abgenommen), der KI-Creator-Agent (eigene Spur,
`ki-creator`), Mehrsprachigkeit (B-85-Reihe, abgenommen), Zweitbetreuer-Konto (nur gelesen, nicht gespielt),
Medien/Bilder (kein Bild im Seed des Plans).

## Nachtlauf-Pakete aus dieser Sitzung

Dieselben Regeln wie in der [Backlog-Sichtung vom 2026-10-23](pm-sitzung-2026-10-23-backlog-sichtung.md): ein
Paket = ein Sprint nach `pm-loop`, höchstens sechs Stories, ein Sprint-Ziel aus Rollensicht, je Sprint höchstens
eine Migration (hier: keine). Alle Stories sind entweder `geschaetzt` (baubar nach Freigabe 8) oder
`Defekt`/`Aufräumen` (autonom grillbar nach Freigabe 1). **Die Entscheidungen in den geschätzten
`Wunsch`-Stories hat der PM dieser Sitzung getroffen, nicht der Nutzer** – sie stehen je Story unter
`## Entscheidungen` mit Begründung und Kosten und sind im Abschnitt „Vorgelegte Entscheidungen" unten gesammelt.
Ein Veto des Nutzers vor der Nacht setzt die Story auf `gegrillt` zurück und das Paket verliert sie.

| Paket | Sprint-Ziel (aus Rollensicht) | Stories | Größe | Stolperstein |
| --- | --- | --- | --- | --- |
| **NP-24** Erst lesen, dann prüfen | *Der Sohn sieht jedes neue Wort einmal mit Lösung, bevor er es tippen muss; ein Fehler hält die Karte an, bis er die Lösung gelesen hat; und nach einem verpatzten Test kann er die Wörter sofort üben.* | B-419, B-420, B-430 | M + S + XS, beides | B-419 zuerst (Backend). B-420 ändert, wann die nächste Karte erscheint – `full-flow.spec.ts` vorher lesen, `old-phrases.py` nach dem Umbau. Rollengang als neue Spec `erstbegegnung.spec.ts`, die alle drei Wege fährt. Der Reviewer prüft gezielt `alreadyScoredToday` (Risiko 3 in B-419). |
| **NP-25** Der Grader ist fair | *Der Sohn bekommt „Stadt" für „die Stadt" als richtig gewertet – im Üben und im Test – es sei denn, die Übung trainiert den Artikel.* | B-421 | S, backend | Allein, weil der Grader an vier Stellen aufgerufen wird; Gaps (Lückentext) ausdrücklich **nicht** tolerant. Unit-Tests zuerst (AK 1–5), dann Integration. Danach B-422 nur notieren, nicht entscheiden. |
| **NP-26** Zuweisen mit offenen Augen | *Der Vater gibt beim Ziel „Klassenarbeit" den Termin ein und bekommt Plan, Klassenarbeit und Stufenleiter aus einer Hand; in der Übungsauswahl sieht er Umfang, Bewertung und kann jede Übung ausprobieren.* | B-408, B-424, B-423 | S + M + M, beides | Reihenfolge: B-408 → B-424 (dieselben zwei Zeilen) → B-423. B-424 beginnt im Backend (`ItemCount`, `ItemNoun`), `DocsCaptureTests` neu erzeugen. B-423 nur Frontend; Termin als `DateOnly`-String rechnen, nie über `new Date()` mit Uhrzeit. Rollengang Vater als `assistent-klassenarbeit.spec.ts`. |
| **NP-27** Zahlen und Namen stimmen | *Der Vater liest im Konto je Buchung die Währung und überall dasselbe Datumsformat; der Creator sieht im Fachlehrer-Profil die deutschen Typnamen, je Unit die Übungszahl und beim Verwalten zuerst seine Übungen; die Demo-Familie passt zum Katalog.* | B-425, B-426, B-427, B-428, B-431, B-429 | XS–S je Story, beides | Sechs Stories = Obergrenze. B-429 zuletzt, weil der Seed die Capture ändert und B-425 die Kategorie des Startguthabens mit anfasst (dort nur erwähnt – in B-429 bauen). B-427 greppt am Ende als Tor. Drei Vertrags-Erweiterungen (B-425 `currency`, B-428 `exerciseCount`), beide additiv. |

**Nachtfolge-Vorschlag:** Nacht 1 = NP-24 + NP-25 (der Sohn-Befund, zusammen der rote Faden); Nacht 2 = NP-26;
Nacht 3 = NP-27. Danach weiter mit der offenen Folge der Sichtung vom 2026-10-23 (als Nächstes NP-7/NP-9ff.,
Stand dort). B-422 und B-432 gehören auf die nächste Grill-Tagesordnung des Nutzers, nicht in eine Nacht.

**Auftragssatz für die Nacht** (anzuhängen an den Auftragstext aus [nachtlauf.md](nachtlauf.md)): *„Fahre die
Pakete NP-24, NP-25 aus docs/pm-sitzung-2026-11-08-rollengang.md, in dieser Reihenfolge."*

### Vorgelegte Entscheidungen (zum Veto vor der ersten Nacht)

Je Story die Kernentscheidung in einem Satz; Begründung und Kosten stehen in der Story.

1. **B-419:** Erstbegegnung ist eine Eigenschaft der *Karte* (kein Fortschritt / kein `IntroducedAt` → Anzeigekarte in
   der Übungsrunde), zwei Runden am ersten Tag statt einer, Test unberührt, kein neues Positionsfeld.
2. **B-421:** Ein führender Artikel der Zielsprache (de/en/fr) ist beim Tippen optional, Opt-out je Übung
   (`articleRequired`), gleich im Üben und Test; Tippfehler bleiben außen vor (B-422).
3. **B-423:** Beim Ziel „Klassenarbeit" ist der Termin Pflicht; Plan endet am Vortag; Leiter = erstes Drittel
   Selbsteinschätzung, dann Tipp-Stufe; die Klassenarbeit wird mit den gewählten Übungen **angelegt**.
4. **B-424:** Umfang als Zahl mit Typ-Einheit aus dem Manifest (`itemCount`, `itemNoun`), „Ausprobieren" öffnet den
   vorhandenen Dialog in Assistent und Planbau; gemeinsam mit B-408.
5. **B-428:** Additives `exerciseCount` je Unit, Spalte mit Link, Zählung ohne Rollenfilter.
6. **B-430:** Ein Knopf „Diese Wörter üben" zur normalen Übungsrunde (Server ordnet „Schwächste zuerst").
7. **B-431:** Ohne Fach zeigt „Übungen verwalten" die eigenen Übungen aller Fächer.

## Retrospektive

Diese Sitzung hat nichts gebaut, darum keine Nachschau und kein Increment – die Retro-Pflicht liegt bei den
Sprints, die die Pakete fahren. Zwei Beobachtungen über das Verfahren selbst, als Vorschläge, nicht gelandet:

- **(CB)** Der schwerste Befund des Tages (B-419) wäre von keinem Tor und keinem Review gefunden worden, weil
  jeder Test „Karte → Antwort → Wertung" prüft und keiner „Hat das Kind das Wort je gesehen?". Das ist
  derselbe Befund wie beim Rollengang-Argument im README: nur das Hinsetzen als Sohn mit einem **frischen** Plan
  findet ihn. Vorschlag: der Rollengang des Sohnes in jedem Sprint beginnt mit einem **neu angelegten** Plan,
  nicht mit dem Demo-Plan, dessen Items längst eingeführt sind.
- **(CC)** `read_page` des eingebauten Browsers meldete Checkboxen als „on" (ohne Namen); die Prüfung per
  `input.labels` zeigte, dass alle beschriftet sind. Ein Werkzeugbefund ist kein Produktbefund – darum steht
  hier keine A11y-Story. Als Regel für den Rollengang: Barrierefreiheits-Befunde am DOM nachmessen, bevor sie
  eine Story werden.

## Offene Roadmap

Die Liste ist [docs/backlog/](backlog/README.md); diese Sitzung hat 14 Stories ergänzt (B-419 … B-432) und vier
Pakete geschnitten (NP-24 … NP-27). Reihenfolge-Begründung: Sohn-Lernmechanik vor Vater-Querverbindungen vor
Hygiene – weil der Malus heute Nichtwissen bestraft, das die App selbst erzeugt. Nichts gepusht; die Wegwerf-DB
`backend/Pugling.Api/pugling_walk.db` ist gitignored und kann gelöscht werden.
