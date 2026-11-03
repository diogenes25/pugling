---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-11-03, Lauf P, Lauf Q, Lauf R]
---

# PM-Sitzung: Nachtläufe 2026-11-03 – Lauf P, Q, R

**Datum:** 2026-11-03  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Drei Nachtläufe nacheinander abschließen, je mit den baubaren Stories nach Themen.

Auftrag: „Schließe 3 Nachtläufe ab. Kommt es zu Nachfragen, beantworte diese selber nach bestem Wissen und Gewissen." Es
gelten die Freigaben 1–10 aus [nachtlauf.md](nachtlauf.md); wie in der Nacht zuvor deckt die Erlaubnis, Nachfragen selbst zu
beantworten, Fragen **an** den Lauf (welches Thema, welcher Weg), nicht die Sperren der Freigaben – Retro-Vorschläge werden
nicht gelandet. Ausgangslage: HEAD `299d216`, Baum sauber, alles seit Lauf J ungepusht. Index zu Beginn: „Nachgeschaut 281
von 284" – offen sind B-261, B-265, B-274 (Lauf O); die Nachschau des ersten Sprints nimmt sie.

**Auswahl:** Ohne Paket-Vorgabe wie in den Läufen zuvor die baubaren `Defekt`-Stories nach Themen: Lauf P die
Frontend-Testlücken und Restkanten (B-356, B-357, B-272; danach B-276, B-273), Lauf Q die Doku-Generator- und
Backend-Restkanten (B-259, B-260, B-355, B-282), Lauf R das Paket NP-15 (B-164 `Defekt`, B-183 mit Freigabe des Nutzers,
Freigabe 9).

**Nicht angefasst, weil gesperrt:** B-329 (Vertragsbruch ohne Freigabe), B-310 (`vertragsbruch` ungeklärt), B-328 und die
übrigen `Frage`-/`Wunsch`-Stories ohne Freigabe-Zeile.

## Lauf P, Teil 1 — Jede Zusicherung fängt ihre Mutante, jede Bedienung hält still, wo nichts zu ändern ist (B-356, B-357, B-272)

**Sprint-Ziel:** *Der Entwickler, der eine Radiogruppe, eine Live-Region oder eine benannte Kästchen-Gruppe zurückbaut, sieht
einen Test fallen; der Vater wählt mit Home/End nichts neu, und der Sohn sieht nach einer verlassenen Klausur seine Serie.*

**Umfang:** [B-356](backlog/B-356-radiogruppe-und-wartebild-restkanten.md) (S),
[B-357](backlog/B-357-e2e-helfer-restkanten.md) (XS),
[B-272](backlog/B-272-live-region-und-gruppen-tests-tragen-nicht-alles.md) (S) – alle `Defekt`, unter Freigabe 1 ausformuliert,
gegrillt, geschätzt. Ein Thema: Zusicherungen im Frontend, die ihre Regel nicht tragen. Draußen bleiben B-276 und B-273
(Layout des ⓘ-Popovers, eigener Teil).

**Entwickler-Brief:** Zwei Verhaltensfixes (`radioGroupKeys` bei `to === at`, Serie nach dem Verlassen), sonst nur
Zusicherungen. Die Wächter urteilen je Gruppe bzw. gegen die Quellen statt gegen sich selbst. Testweg: rote Probe je
Verhaltens-AK, Mutante je übrigem AK und Entscheidungsteil, Vitest voll, E2E-Gesamtlauf.

### Iteration

- **B-356:** rote Proben AK 1 **1 failed**, AK 3 **2 failed**, AK 4 **1 failed** (je erwartet); AK 2/5 Testlücke, Mutanten
  „ohne `preventDefault`" **4 failed**, „`await` vor der Abgabe" **1 failed**.
- **B-357:** keine rote Probe erzeugbar (Zweige mit dem Seed unerreichbar), `tsc --strict` sauber.
- **B-272:** Testlücke; sieben Mutanten je **1 failed** am erwarteten Fall (eine davon E2E: Gruppe an die Karte gerückt).
  Vorschlag 5 der Idee verschärft: `it.each(REGION_BAUSTEINE)` hätte eine gestrichene Zeile nicht bemerkt, die Liste wird
  jetzt gegen die Quellen geprüft.
- Vitest **619/619**, `tsc -b` sauber.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | B-356 Entscheidung 1, Scroll-Hälfte ohne Test: „Wächter vor `preventDefault`" blieb grün | AK-1-Fall prüft `defaultPrevented` an Home/erster und am Pfeil bei einer Option; Mutante **1 failed** | ja |
| 2 | `frontend-reviewer` 🟢 | B-272: Fall „return null in einer Pfeil-Komponente" ohne eigene Mutante | gestrichen, der Gegenfall trägt die Regel | nein |
| 3 | `frontend-reviewer` 🟢 | B-272: Kommentar nennt eine ungemessene Hälfte (neuer Baustein außerhalb der Liste) | gemessen mit `ProbeHinweis`: **1 failed** | nein |
| 4 | `frontend-reviewer` 🟢 | B-356: Wächter prüft nicht, dass der Helfer am Tag der Gruppe sitzt | Selbsttest „Helfer an einem Kind", Mutante „ohne Anker" **1 failed** | nein (🟢) |

Dazu ohne Fund: englische `//`-Kommentare im deutsch kommentierten Frontend-Umfeld auf Deutsch umgestellt (nur Kommentar).
**Zähler Teil 1: 1.** Vitest danach **619/619**, `tsc -b` sauber.

**Rollengang:** Vater maschinell – der Radiogruppen-Wächter läuft über alle Quellen, die Gruppen-Spec fragt den
Barrierefreiheitsbaum des echten Browsers, die Assistenten-Specs fahren die Wahl. Sohn über `SohnTest.test.tsx` (Verlassen mit
fertigem Versuch → Serie in der Kopfzeile) und die Klausur-Specs. Entwickler über die Proben selbst. Creator Regressionszeuge.
E2E-Gesamtlauf **103 passed** (`exit=0`, gestartet nach dem letzten Code-Edit). **B-356, B-357, B-272 abgenommen.**
Gerätecheck aus B-352 (Radiogruppen mit Screenreader) bleibt offen.

### Retrospektive

**Nachschau:** Lauf O (`299d216`: B-261, B-265, B-274) per frischem `pugling-reviewer`, read-only: **sauber**, kein 🔴/🟡.
Prüfpunkte je AK in den drei Stories (Mutanten „`open` ignoriert", „`ReadAttempts` → 4", „ohne Sleep", „ohne `File.Exists`",
unsicherer Schreiber je mit Zeile; `Program.cs:295` ruft die getestete Methode). Drei 🟢 →
[B-358](backlog/B-358-beispielkatalog-proben-restkanten.md), P4. `nachgeschaut` an allen drei gesetzt.

**Was die eigenen Tore durchgelassen haben:** Eine Entscheidung mit zwei Hälften („die Taste gehört der Gruppe" *und* „nichts
neu wählen") war je Hälfte getestet – aber jede in einem Aufbau, in dem die andere nicht greift. Die Mutante, die die
Reihenfolge der beiden Wächter vertauscht, war darum grün; der Reviewer fand sie. Das ist die (AJ)-Zeile je
Entscheidungsteil, angewandt und trotzdem zu schmal: sie fragt „trägt jeder Teil eine Zusicherung", nicht „trägt der Fall, in
dem beide zugleich greifen, beide". In die andere Richtung hat der Lauf selbst gefangen, was die Idee vorschlug:
`it.each(REGION_BAUSTEINE)` hätte sich aus der Liste gespeist, die er prüfen soll. Von außen: nichts.

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(AU)** Prosa in pm-loop Step 5 neben (AJ): „Hat eine Entscheidung zwei Wächter, deren Reihenfolge zählt, braucht es einen
  Fall, in dem beide zugleich greifen, und die Mutante ‚Reihenfolge vertauscht' wird gemessen."
