---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf C – Nacharbeit aus der Nachschau von B-730 und B-660

**Datum:** 2026-11-23  ·  **Moderation:** PM (Nachtlauf, unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die sieben offenen Defekte aus den Nachschauen der letzten Nacht schließen, Sprint für Sprint.

Auftrag: „starte einen nachtlauf“ – gelesen als der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10.
Ausgangsstand `main` `f41d035d`, sauber; keine parallele Pugling-Sitzung aktiv (geprüft per Sitzungsliste). Story-Ids
dieses Laufs: ab B-860.

## Runde 1 — Auswahl

Kein Befund unter `docs/anmerkungen/` neu. Der Backlog trägt 101 offene Stories; autonom grillbar sind nur `Defekt`/
`Aufräumen`, und offen auf `idee` liegen genau sieben Defekte, alle aus den Nachschauen vom 2026-11-22: B-842/843/844
(B-730, Lerntag), B-845/846/847 (B-660, „Noch einmal“) und B-820 (B-659, Kür). Die geschätzten `Wunsch`-Stories der
B-644-Kette (B-731 ff.) bleiben liegen: B-731 baut auf fünf unbestätigten Entscheidungen von B-730 (Gedächtnis-Notiz
Lauf A), B-701/B-703 tragen offene Fragen aus Lauf C vom 2026-11-22.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 117 | B-842, B-843, B-844 | Lerntag: was B-730 versprach, auch an den Rändern |
| 118 | B-845, B-846, B-847 | „Noch einmal“: Text und Belege von B-660 |
| 119 | B-820 | Laufende Nacharbeit auf Kür hält die Serie |

## Sprint 117 — Ziel & Umfang

**Sprint-Ziel:** Jede Zeitangabe, die Sohn oder Vater rund um Mitternacht und in einer Umstellungswoche sehen, folgt dem
deutschen Lerntag – und das Tor fängt jede Form, die es verspricht.
**Umfang:** B-842, B-843, B-844 (alle XS–S, backend, keine Migration).
**Entwickler-Brief:** Quelle der Wahrheit bleibt `LearningDay` (neu: `SameTimeDaysEarlier`). Tests zuerst in
`TagesbegriffTests` (Berlin-Fabrik), Tor in `ConventionGuardTests` auf das ganze API-Projekt mit Selbsttest. Reihenfolge:
Tor gegen den Ausgangsstand (muss B-844 melden) → B-844 → B-842.

## Iteration 117 — umgesetzt

- **Backend:** `LearningDay.SameTimeDaysEarlier` (Wanduhr minus Kalendertage, übersprungene Frühjahrs-Uhrzeit +1 h);
  `WalletService.WeekCoinsAsync` schneidet die Vorwoche damit; der Titel der Schwachwort-Wiederholung trägt
  `learningDay.DayOf(now)`.
- **Tor:** `Tage_Entstehen_Nur_Im_Lerntag` über das ganze API-Projekt (Filter auf dem relativen Pfad), neue Formen
  (`.Date`, `new DateOnly(Y,M,D)`, `FromDayNumber`, `ToDateTime(default)`, Datumsformat auf Zeitpunkten, `DayOfWeek` auf
  UTC-Uhren), Ausnahmen als `(Datei, Fragment)`; Selbsttest `Lerntag_Tor_Faengt_Jede_Form` (18 Fälle).
- **Verifikation:** rote Proben 3/3 (Tagesbegriff) und 1/1 (Tor gegen den Stand vor B-844); Mutanten M1–M13 alle rot;
  Backend 2126/2126 (Release, nach der letzten Änderung). Commit `e3f51ee7`.
- **Review** (`pugling-reviewer`): kein 🔴, ein 🟡 (Ausnahme am zweiten Doppelpunkt abgeschnitten → Tupel; **Zähler
  1/5**), sieben 🟢 – sechs behoben, einer (eine Umstellung um eine Stunde angenommen) im Kommentar benannt; alle ohne Zählung.

## Runde 117 — Abnahme

- **Sohn — abgenommen:** „In der Woche mit Zeitumstellung vergleiche ich mich wirklich mit Sonntagmittag, nicht mit 11 Uhr.“
  Gegangen maschinell (Integrationstests über die HTTP-Pipeline mit eingefrorener Uhr in der Umstellungswoche – ein
  laufender Server hat diese Uhr nicht).
- **Vater — abgenommen:** „Die Wiederholung von 0:30 Uhr heißt nach dem neuen Tag.“ Ebenso maschinell
  (`Schwachwort_Wiederholung_Traegt_Den_Lerntag_Im_Titel`).
- **Creator — Regressionszeuge:** volle Suite grün; keine Creator-Fläche im Diff.

## Retrospektive Sprint 117

**Nachschau:** B-700 und B-840 (Lauf C vom 2026-11-22), `pugling-reviewer`, nur gelesen. B-700: Prüfung vor jedem
Schreibweg, kein Umweg, Migration nur nullable Spalten – zwei 🟢 → [B-860](backlog/B-860-aufbau-weg-wettlauf-baut-kette.md)
(Wettlauf baut Kette/Kreis, `entgangen_bei: B-700`) und [B-861](backlog/B-861-deutscher-kommentar-im-uebungs-loeschen.md)
(deutscher `/* */`-Kommentar). B-840: kein Befund, Prüfpunkte im Verlauf. `nachgeschaut` an beiden gesetzt.

**Was die eigenen Tore durchgelassen haben:** Der Selbsttest des Tors prüft das Muster, nicht die Ausnahmeliste – ein
Eintrag mit Doppelpunkt wurde still verkürzt, und kein Test sah es. Gefunden hat es nur der Reviewer.

**Vorschlag (nicht gelandet, Freigabe 3):** (CY) Die Ausnahmelisten aller reflexiven Tore als `(Datei, Fragment)`-Tupel
plus ein Test, dass jede Ausnahme **genau eine** heutige Zeile trifft – fängt Verkürzung und verwaiste Ausnahmen. Der
zweite Fall mit `Split(':')` steht noch in `ConventionGuardTests.cs:654` (heute ohne Doppelpunkt im Fragment).

**Bewusst ungedeckt:** `.DayOfWeek` auf einem Zeitpunkt in einer Variablen; Standard-Datumsformate (`{x:d}`).
