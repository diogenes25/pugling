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

## Lauf S, Teil 2 — Jede Zeile, die richtig ist, trägt eine Zusicherung (B-355, B-358, B-371)

**Sprint-Ziel:** *Der Entwickler, der eine richtige, aber ungetestete Zeile im Vokabelspeicher oder im PIN-Pfad zurückbaut, sieht
einen Test fallen; der Vater, der einen Login per leerer PIN abschaltet, kann sich darauf verlassen.*
**Umfang:** B-355 und B-358 (`Defekt`, `idee`), dazu [B-371](backlog/B-371-leere-pin-per-patch-nur-als-angenommen-geprueft.md),
abgeteilt aus B-282 Punkt 5 (Teilen, Freigabe 3). Die übrigen Punkte von B-282 bleiben dort: Abbruch-Log und Guard-Reihenfolge sind
Verhalten im Auth-Pfad, Seed-PIN und Zeitseitenkanal brauchen eine Abwägung – kein Thema dieses Sprints.

**Entwickler-Brief:** Alle drei Stories betreffen Code, der richtig ist und nur keine Zusicherung hat. Je Fall ein Test, der im
Ausgangszustand grün ist, und eine Mutante, die ihn rot macht – gesetzt per Skript mit Byte-Vergleich beim Zurückspielen (Lehre aus
Lauf R). Keine Verhaltensänderung, keine Migration, kein Vertragsbruch. B-358 Punkt 1 ist Robustheit im Aufräumen (keine rote Probe
möglich), Punkt 2 eine Einengung der AK von B-265.

### Umgesetzt

- **B-355:** zwei Service-/HTTP-Fälle in `VocabularyStoreTests` – zwei gleiche Paare in einem `GetOrCreateManyAsync`-Aufruf ergeben
  einen Eintrag; ein mit `" en "` gespeicherter Eintrag ist für `en` ein Treffer. Tab im Sprachkürzel bleibt benannt ungedeckt.
- **B-358:** `TempRoot.Dispose` schluckt auch `UnauthorizedAccessException`; B-265 AK 2 auf die Summen-Zusicherung eingeengt (Satz
  am Code, AK-Zeile); lange Kommentarzeile umbrochen.
- **B-371** (aus B-282 Punkt 5): `PinPolicyTests.Leere_Pin_Per_Patch_Sperrt_Den_Alten_Login`, je Änderungsweg ein eigenes Subjekt mit
  Gegenprobe vor dem PATCH.

**Rote Proben** (alle Tests im Ausgangszustand grün – der Code war richtig, es fehlte die Zusicherung): „neuer Eintrag nicht gemerkt"
**1 failed**, „gespeicherte Sprache ohne `Trim()`" **1 failed** (nach dem Entfernen des überflüssigen PATCH erneut **1 failed**),
„Pfad überspringt die leere PIN" für Kind, `supervisor/adults`, `auth/me` je **1 failed**. Gesetzt per Skript, byte-gleich zurück.

**Zähler Teil 2: 0.** Review `pugling-reviewer`: kein 🔴/🟡; vier 🟢 ohne Zählung behoben (überflüssiger PATCH im Test, Zeilennummer
`:49`, „keine rote Probe möglich" → „nicht lohnend", AK-Zeile von B-265 selbst eingeengt).

**Verifikation nach dem letzten Code-Edit:** volle Suite **1071/1071**, Build ohne Warnung, Vitest **638/638**, E2E-Gesamtlauf
**109 passed** (`exit=0`).

### Rollengang

**Vater** maschinell: der neue Fall schaltet im echten HTTP-Pfad den Login des Kindes und den eigenen (über beide Wege) per leerer
PIN ab und sieht die alte PIN danach abgewiesen. **Creator** maschinell über die beiden Store-Fälle (POST mit `reuseExisting` gegen den
echten Controller). **Entwickler** über die Mutanten. **Sohn** Regressionszeuge (E2E grün, Arcade unberührt).
**B-355, B-358, B-371 abgenommen.**

### Retrospektive

**Nachschau:** Lauf S, Teil 1 (`cc789ed`: B-361, B-365, B-370) per frischem `pugling-reviewer`, read-only. **Keine Entgleitung** (kein
🔴/🟡). Drei 🟢 ohne Zählung behoben: der Wächter-Kommentar sagte „22 Stellen" ohne „im Vater-Web" (über ganz `src` 26); in B-369
`MediaPickers` aus „frühe Rückgabe" gelöst und zwei Start-Fehler der Arcade als offene Zuordnung notiert; im Verlauf von B-365 die
übrig bleibenden Testdaten vollständig benannt. `nachgeschaut` an B-361, B-365, B-370, je mit Prüfpunkten (21/21 Meldungen, 4/4 rote
Probe, 22/22 bzw. 25/25 Stellen).

**Was die eigenen Tore durchgelassen haben:** nichts Neues an Code. Zwei Beobachtungen:

1. Der Reviewer fand einen **überflüssigen Schritt im Test** (ein PATCH, der nichts trug) – harmlos, aber er hätte einen Leser glauben
   lassen, der Fall hänge am veralteten Schlüssel. Die Mutante war rot und blieb es ohne den Schritt; eine Mutante prüft, *ob* der Fall
   fällt, nicht, *welche Zeile* ihn trägt. Mechanisierbar wäre das nur mit einer zweiten Mutante je Arrange-Schritt – teurer, als der
   Fund wiegt. **Bewusst ungedeckt.**
2. Die Nachschau fand am Kommentar aus Teil 1 dieselbe Familie wie (BE): eine Zahl ohne ihren Geltungsbereich. Kein neuer Vorschlag –
   (BE) deckt es, jetzt mit fünf Messungen.

**Vorschlag:** keiner neu; (BE) aus Teil 1 bleibt der Vorschlag.
