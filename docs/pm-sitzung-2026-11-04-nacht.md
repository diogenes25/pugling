---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-11-04, Lauf S]
---

# PM-Sitzung: Nachtlauf 2026-11-04 – Lauf S

**Datum:** 2026-11-04  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Einen Nachtlauf über die baubaren Stories, nach Themen in Sprints geschnitten.

Auftrag: „Start nachtlauf." Es gelten die Freigaben 1–10 aus [nachtlauf.md](nachtlauf.md) unverändert; Retro-Vorschläge werden
nicht gelandet. Ausgangslage: HEAD `b79b4a4`, Baum sauber, alles seit Lauf J ungepusht. Lauf R, Teil 2 (`3ea3b1d`: B-183, B-368)
ist unbeobachtet – die Nachschau des ersten Sprints nimmt ihn.

**Auswahl:** Ohne Paket-Vorgabe die baubaren `Defekt`-/`Aufräumen`-Stories nach Themen. Teil 1: das Vertragsdokument und seine
Wächter (B-361, B-365). Danach die Backend-Restkanten (B-282, B-355, B-358), soweit die Halt-Bedingungen es zulassen.

**Nicht angefasst, weil gesperrt:** B-329 und B-310 (Vertragsbruch ohne Freigabe, wie in der Nacht zuvor), B-369 (Gestaltungsfrage
ohne Bau-Freigabe), B-328 und die übrigen `Frage`-/`Wunsch`-Stories ohne Freigabe-Zeile; B-04/B-39 (`Wunsch`, `geschaetzt`) sind
nach Freigabe 8 baubar, aber kein Teil dieses Themas und für eine Nacht nach zwei Sprints zu groß (M/L).

## Lauf S, Teil 1 — Was der Wächter sagt und was das Dokument zeigt (B-361, B-365)

**Sprint-Ziel:** *Der Entwickler, den ein Vertrags- oder Schema-Tor rot meldet, liest die Meldung auf Englisch, und der Creator
sieht im Vertragsdokument jeden Code- und Fettsatz, den der Quelltext setzt – auch in einem Listenpunkt.*
**Umfang:** B-361 (`Defekt`), B-365 (`Aufräumen`), beide auf `idee` – im Lauf ausformuliert, gegrillt (Freigabe 1) und geschätzt.
Dazu aus der Nachschau [B-370](backlog/B-370-zwei-behauptungen-aus-lauf-r-teil-2.md) (`Defekt`, im selben Sprint, Freigabe 3).

**Entwickler-Brief:** Das Vertragsdokument ist das Produkt; was der Generator verliert, muss im Quelltext verhindert werden, denn der
Normalisierer sieht das Markup nie (wie B-259). Quelle der Wahrheit: `docs/openapi/v1.json`, geschrieben von `ContractDocumentTests`.
Tor über dieselben Dateien wie das Klebestellen-Tor (Contracts + Controller). Die Tor-Meldungen werden nach Eigenschaft gezählt
(deutsche Meldungstexte aller reflexiven Wächter), nicht nach den zwei, die die Story nannte. Keine Migration, kein Vertragsbruch.

### Umgesetzt

- **B-361:** Tor `Listenpunkte_Tragen_Keine_Inline_Elemente` mit Selbsttest (7 Fälle + Zeilen-Fall); die zwei Gründe in
  `UsageResponse.otherLearnersCount` und die Controller-Klassen-Summary als Prosa. Rote Probe: erwartet 4 Fundstellen, gemessen 4.
  Im Dokument jetzt `` `409 exercise_in_use` `` und `**creator without own children**`.
- **B-365:** 21 deutsche Meldungen in vier Wächter-Dateien übersetzt (gezählt beim Ausformulieren 19, zwei weitere im Review).
- **B-370:** Kommentar am Wizard-Knopf nennt `exercises.stale` statt des entfernten Effekts; Inventar in B-369 und im Wächter-Kommentar
  auf 22 Ternär-Stellen, frühe Rückgaben und acht Arcade-Stellen ergänzt.

**Zähler Teil 1: 2** – (1) der eigene Fall „Zustand leckt" konnte nicht fallen (beim Mutanten-Messen selbst gefunden, Test geändert);
(2) 🟡 zwei übersehene deutsche Meldungen (Code geändert). Ohne Zählung (🟢, Freigabe 10): Kommentar an einem Selbsttest-Fall, `break`
überspringt ein `</item>` derselben Zeile (Merker + neuer Fall).

**Verifikation nach dem letzten Edit:** volle Suite **1068/1068**, Vitest **638/638**, `tsc -b` sauber, E2E-Gesamtlauf **109 passed**
(`exit=0`).

### Rollengang

Der **Entwickler** ist die Rolle der Tor-Meldungen und des neuen Tors: rote Probe mit Zahl, vier plus eine Mutante am Scan je rot.
**Creator** maschinell: `ContractDocumentTests` schreibt das Dokument, der Diff ist genau eine Zeile, Code- und Fettsatz stehen da.
**Vater** und **Sohn** Regressionszeugen: E2E-Gesamtlauf grün, im Frontend nur Kommentare geändert. **B-361, B-365, B-370 abgenommen.**

### Retrospektive

**Nachschau:** Lauf R, Teil 2 (`3ea3b1d`: B-183, B-368; `b79b4a4`) per frischem `frontend-reviewer`, read-only. **Entgangen:**
[B-370](backlog/B-370-zwei-behauptungen-aus-lauf-r-teil-2.md) bei B-368 (Kommentar am Zwilling nannte den entfernten Effekt) und
bei B-183 (Inventar der Ternär-Form zeilenweise gezählt: 21 statt 22, frühe Rückgaben und sechs Arcade-Stellen fehlten) – kein
Increment dieses Laufs, also kein Limit 2; in diesem Teil behoben. `nachgeschaut` an B-183 und B-368, je mit nachgerechneten
Prüfpunkten (29/29 Banner, 29/29 `ListenAusfall`, 113/113 `useAsync`).

**Was die eigenen Tore durchgelassen haben:**

1. *Eine AK, die nur sucht, was sie schon kennt.* B-365 AK 1 war ein `grep` über die bekannten Sätze – zwei übersehene Meldungen
   hätte sie nie gemeldet; erst der Reviewer fand sie. Das ist dieselbe Familie wie (AS) („ein AK ‚nirgends mehr X' wird nach der
   Handlung gesucht") und die Nachschau-Entgleitung von B-183 (zeilenweise Regex über eine mehrzeilige Form): **dritte und vierte
   Messung**.
2. *Ein Selbsttest-Fall, der seine Mutante nicht fing.* Gefunden, weil vor dem Review jede Mutante wirklich gelaufen ist – die Regel
   aus Step 5 hat gehalten; kein neuer Mechanismus nötig.

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(BE)** Prosa in `docs/backlog/README.md` (Stufe `ausformuliert`, Ist-Stand): „Eine Zählung im Ist-Stand, die eine spätere AK
  ‚keine mehr' trägt, nennt ihre Suchform **und** eine zweite, unabhängige Form (Eigenschaft statt Satz, mehrzeilig statt zeilenweise);
  weichen die Zahlen ab, ist die Zählung nicht fertig." Das ist (AS), geschärft um den Gegencheck – mit jetzt vier Messungen.
