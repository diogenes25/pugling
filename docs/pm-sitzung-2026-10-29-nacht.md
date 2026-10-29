---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-29, Lauf K]
---

# PM-Sitzung: Nachtlauf 2026-10-29 – Lauf K

**Datum:** 2026-10-29  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die baubaren `Defekt`-Stories nach Themen in Sprints fahren, beginnend mit den Kandidaten aus
[Ende Lauf J](pm-sitzung-2026-10-28-nacht.md#ende-lauf-j).

Auftrag: „Starte Nachtlauf" – der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10. Ausgangslage:
HEAD `2a470be`, Baum sauber, 101 Commits ungepusht. Keine Paket-Vorgabe. Index zu Beginn: „Nachgeschaut 247 von 249" –
offen sind B-330 und B-332 (Lauf J, Teil 2); die Nachschau des ersten Sprints nimmt sie.

**Nicht angefasst, weil gesperrt:** B-329 (Vereinheitlichung auf `invalid_reference` im Übungspfad – Vertragsbruch ohne
Freigabe), B-328 (`Frage`). Die offenen Entscheidungen aus Lauf J bleiben beim Nutzer.

## Lauf K, Teil 1 — Der Creator legt nichts doppelt an (B-331, B-141, B-277)

**Sprint-Ziel:** *Der Creator, der Wörter und Schlagwörter anlegt – über die API, als KI-Creator oder im Assistenten des
Vater-Webs –, bekommt für etwas, das es schon gibt, weder einen zweiten Eintrag noch eine halb geschriebene Übung mit
`500`, sondern den vorhandenen Eintrag oder eine klare Meldung.*

**Umfang:** [B-331](backlog/B-331-doppeltes-wort-im-uebungs-payload-500.md) (P3, doppeltes Wort im Übungs-Payload →
`500`), [B-141](backlog/B-141-interest-tag-label-dublette.md) (P3, zwei Schlagwörter mit demselben Label; fachliche
Frage am 2026-10-23 vom Nutzer entschieden), [B-277](backlog/B-277-schnellanlage-legt-doppelten-store-eintrag-an.md) (P3,
der Schnellanleger im Assistenten legt `_2` an). Alle drei `Defekt`, unter Freigabe 1 ausformuliert, gegrillt und
geschätzt (je S). Draußen: B-329 (siehe oben) – B-331 meldet deshalb bewusst `validation_error` (Entscheidung 1 dort).

**Entwickler-Brief:** Ein Faden, drei Geburtswege einer Dublette. **B-331** (Backend): was der Unique-Index
`(ExerciseId, VocabularyId)` verbietet, prüft jetzt die Validierung, vor jedem Schreiben, mit derselben Auflösung wie der
Reconcile (Refs vor Items, Inline-Wort per Store-Id oder Schlüssel). **B-141** (Backend + ein Text): das Label ist an
allen drei Stellen eindeutig, an denen eines entsteht – Anlegen und Umbenennen (`409`), `EnsureAsync` (Treffer statt
neuer Zeile); exakter Vergleich, keine Migration. **B-277** (Frontend): der Schnellanleger sieht vor dem Anlegen im Store
nach und ergänzt Varianten am vorhandenen Eintrag; der Store-`POST` bleibt unverändert. Testweg: rote Probe und Mutante je
Fall, Integrationstests bzw. eine E2E-Spec, volle Suite, E2E-Gesamtlauf als Rollengang.

### Iteration

- **B-331:** rote Probe **2 failed** (erwartet 2; `PUT` „refs twice: 500“, `POST` `InternalServerError`). Fünf Mutanten,
  je eine Form, jede fällt an ihrer eigenen Form (Zahlen in der Story). Eine erste Fassung zweier Mutanten war falsch
  gebaut (`if (false)` bricht den Build an CS0162; eine fiel in den Schlüssel-Zweig durch) und wurde neu gemessen.
- **B-141:** rote Probe **3 failed / 1 passed** (erwartet 3/1). Mutante „sich selbst nicht ausnehmen“ blieb grün → Fall
  „eigenes Label unverändert mitschicken“ ergänzt, dann **1 failed**. Dritter Geburtsweg `EnsureAsync` beim Ausformulieren
  gefunden und in den Umfang genommen (Entscheidung 3 der Story).
- **B-277:** rote Probe der neuen E2E-Spec **1 failed** (gemessen zweimal `riesig`); zwei Mutanten je **1 failed**.
- Volle Suite **1011/1011**; der OpenAPI-Schnappschuss trägt den neuen `409` am Schlagwort-`POST`.
- Nachbesserung nach den Reviews (Tabelle unten): B-277 läuft jetzt über den Server (`reuseExisting` am Store-`POST`,
  additiv), B-334 und B-333 im Sprint gebaut. Danach volle Suite **1015/1015**, Vitest **576/576**, `tsc -b` sauber,
  `dotnet format` ohne Fund in den geänderten Dateien.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | Client-Vergleich (Groß/Klein) schwächer als der Server-Schlüssel; NOCASE faltet nur ASCII → „École"/„école" weiter `_2` | Server-Feld `reuseExisting`, Client ruft nur noch das | ja |
| 2 | `frontend-reviewer` 🟡 | neue Mutation an `useAction` vorbei, Fehler-`div` ohne Rolle | `useAction().runFor` + `StatusBanner` | ja |
| 3 | Delta-Review 🟡 | Reuse-Zweig übersprang `BaseFormKey`-Prüfung, verwarf Tags still | Prüfung vor den Zweig, Tags additiv | ja |
| 4 | Delta-Review 🟡 | `key` + `reuseExisting` stumm ignoriert | `400 validation_error` | ja |
| 5 | Delta-Review 🟡 | Batch `Reused → Existing` ohne Fall, Batch-Doku ohne das Feld | Fall + Satz | ja |
| – | `pugling-reviewer` 🟡 | `PatchItem` auf ein belegtes Wort → `500` (älter, außerhalb des Diffs) | [B-334](backlog/B-334-patch-item-auf-belegtes-wort-500.md), im Sprint gebaut | nein |
| – | `pugling-reviewer` 🟢 | „inline word twice" nahm ein Wort, das ein anderer Test anlegt | frisches Wort | nein |
| – | `pugling-reviewer` 🟢 | KI-Creator-Dublettenregel schwächer als der Schlüssel | [B-336](backlog/B-336-ki-creator-dublettenregel-schwaecher-als-store-schluessel.md) | nein |
| – | `pugling-reviewer` 🟢 | Label-Eindeutigkeit ohne Index (Rennen) | in B-141 benannt, wie entschieden | nein |
| – | `frontend-reviewer` 🟢 ×3 | Groß/Klein ungetestet; Token an erster Store-Seite; verlorenes Update | „Riesig" in der Spec; Suchfilter; entfällt mit Server-Vereinigung | nein |
| – | Delta-Review 2 🟢 | Testkommentar behauptete `200` statt `409` für den Schlüssel-Fall | berichtigt | nein |

**Zähler: 5** – an der Grenze, nicht darüber. Das zweite Delta-Review fand kein 🔴/🟡 mehr. Volle Suite **1015/1015**,
E2E **100 passed** (`exit=0`, nach der letzten Code-Änderung gestartet; danach nur der Testkommentar). Rollengang als
E2E-Gesamtlauf: **Creator/Vater** über `schnellanlage-ohne-dublette.spec.ts` (Assistent → Store, vorhandener Eintrag
samt ergänzter Variante, Homonym eigener Eintrag) und `auch-richtig-im-uebungseditor.spec.ts`; die Server-Regeln (Label,
doppeltes Wort, `PatchItem`) über Integrationstests. **Sohn** Regressionszeuge: seine Specs grün, kein Pfad der Arcade im
Diff außer dem neuen Testfall B-333. Browser-Extension nicht genutzt. **B-331, B-141, B-277, B-333, B-334 `abgenommen`.**

### Retrospektive

**Nachschau:** Lauf J, Teil 2 (`02ce632` B-330, `599e217` B-332) von einem frischen `pugling-reviewer`. **Entgangen bei
B-330:** [B-333](backlog/B-333-uebungsrunde-andere-api-fehler-ungetestet.md) (🟡, im Sprint behoben) und
[B-335](backlog/B-335-klausurstart-plan-inactive-weiter-versuchen.md) (🟢, zurückgestellt). B-332: sauber, nur zu enge
Formulierung („gespieltes Wort" statt „in einem Plan"). `nachgeschaut` an B-330 und B-332, mit Prüfpunkten im Verlauf.

**Was die eigenen Tore durchgelassen haben:** (1) Meine Entscheidung 1/2 zu B-277 behauptete „Groß/Klein egal, **wie beim
Store-Schlüssel**" – ungeprüft; `VocabKey.Slug` faltet mehr, NOCASE weniger. Dieselbe Familie wie (AD) aus Lauf J: eine
Entscheidung beruft sich auf ein Verhalten, ohne es am Code nachzuweisen. (2) Die Behebung von Fund 1 legte einen neuen
Erfolgsweg an (Reuse), und drei der fünf gezählten Funde saßen dort: der Abkürzungsweg lief nicht durch die Prüfungen des
vollen Wegs. (3) Nachschau B-333: ein Fall „anderer Fehler" mit einem `Error` statt einem `ApiError` – der Test trennte
nicht, was die Entscheidung trennt (Code vs. Fehlerart).

**Vorschläge, nicht gelandet (Freigabe 3):**

- **(AE)** Prosa in `backend/CLAUDE.md`: „Ein Abkürzungsweg (Idempotenz, Reuse, ‚vorhandenes zurückgeben') steht **hinter**
  allen Prüfungen des vollen Wegs, nicht davor – und je Prüfung ein Fall, der über den Abkürzungsweg kommt."
- **(AF)** Prosa in `frontend/CLAUDE.md`: „Entscheidet der Server per Schlüssel, ob es etwas schon gibt, vergleicht der Client
  nicht selbst – er fragt den Server. Eine zweite Kopie der Schlüsselregel ist immer die schwächere."
- (AD) aus Lauf J ist durch Fund 1 ein zweites Mal belegt.

**Kein neues Tor gelandet; der Lauf hält deshalb nicht an Limit 2.** Der Nachschau-Fund liegt im Increment des Vorlaufs,
nicht dieses Laufs.

## Lauf K, Teil 2 — Das Kind in Übung und Klausur (B-335, B-286)

Fortsetzung nach Rückfrage („weitermachen").

**Sprint-Ziel:** *Das Kind, das übt oder eine Klausur schreibt, wird nicht an eine Wand geschickt, wenn sein Plan
pausiert – und hört mit einem Screenreader nach jeder Karte, was jetzt gefragt ist.*

**Umfang:** [B-335](backlog/B-335-klausurstart-plan-inactive-weiter-versuchen.md) (`Defekt`, aus der Nachschau von
Teil 1) und [B-286](backlog/B-286-kartenwechsel-sagt-die-aufgabe-nicht-an.md) (`Aufräumen`, A11y). Beide autonom
ausformuliert, gegrillt, geschätzt (XS, S). Draußen: B-280, B-275 (Vater-Seite, anderes Themengebiet), B-336 (Agent).

**Entwickler-Brief:** Reines Frontend, nur die zwei Arcade-Bildschirme. B-335: derselbe Satz wie in `resume()`, im Fang
von `start()`. B-286: Live-Region statt Fokuswechsel (B-05 hält den Fokus auf der Eingabe); ein Bauteil `TaskAnnouncer`
neben `ClozePrompt`, immer im DOM, in die Bausteinliste des Live-Region-Wächters. Testweg: Komponententests mit roter
Probe und Mutante je Fall; Rollengang als E2E-Gesamtlauf.

### Iteration

- **B-335:** rote Probe **1 failed / 10 passed** (erwartet 1); Mutante „jeder Fehler stoppt" **1 failed**.
- **B-286:** rote Probe **5 failed / 25 passed** (erwartet 5); fünf Mutanten je **1 failed**, eine davon fängt der Wächter.
  Ein eigener Fall prüfte zuerst nichts (Rückfall ohne Lücken) – vor dem Review umgestellt.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | nach „Weiter versuchen" entsteht die Region neu mit fertigem Text – fortgesetzte Frage stumm; Kommentar zur ersten Karte falsch | Region über allen Ansichten (frühe Returns in `view()`), in Übung und Klausur | ja |
| 2 | `frontend-reviewer` 🟡 | kein Fall hält „derselbe Knoten" – `key=` blieb grün | Fälle mit Knoten-Identität ab dem Laden und nach dem Fehler | ja |
| – | `frontend-reviewer` 🟢 ×3 | Rückfall-Regel doppelt; Testkommentar zum Fokus; kein englischer Fall | Helfer `showsGaps`; Kommentar; Fall unter `ArcadeScope` | nein |
| – | Nachschau Teil 1 🟡 | `reuseExisting` folgt dem gespeicherten Schlüssel, den ein `PATCH` nicht nachzieht | [B-337](backlog/B-337-reuse-folgt-veraltetem-schluessel.md), im Sprint gebaut | nein (anderer Diff) |
| – | Nachschau Teil 1 🟡 + 🟢 | B-331-Schlüssel-Zweig nur mit zeichengleichen Strings; `EnsureAsync` exakt ohne Fall | [B-338](backlog/B-338-doppeltes-wort-schreibweise-ungetestet.md), Fälle | nein (anderer Diff) |
| – | Nachschau Teil 1 🟢 ×2 | Wiki-Label-Satz zu stark; Wiki ohne die neuen `400`/`409` | umformuliert, nachgetragen | nein |
| – | Delta-Review 🟢 ×3 | B-286-Entscheidungstexte veraltet; englischer Fall trennt weniger als sein Kommentar sagt; Schlüssel-Kandidat von B-337 | berichtigt; eingeengt; nachgemessen – getragen (Z. 85) | nein |

**Zähler: 2.** Das Delta-Review fand kein 🔴/🟡.

Volle Suite **1017/1017**, Vitest **584/584**, `tsc -b` sauber, E2E **100 passed** (`exit=0`, nach der letzten
Code-Änderung gestartet). Rollengang als E2E-Gesamtlauf: **Sohn** über seine Arcade-Specs (Übung, Klausur, Buchstaben
ordnen – der Umbau der Bildschirme läuft dort durch) und die Komponentenfälle mit Knoten-Identität; **Creator** über
`schnellanlage-ohne-dublette` (B-337 berührt den Reuse-Weg); **Vater** Regressionszeuge. Browser-Extension nicht genutzt.
**Gerätecheck offen (B-286):** was ein echter Screenreader ansagt, beurteilt keine Maschine – einmal mit NVDA oder
VoiceOver eine Übungsrunde und eine nach „Weiter versuchen" fortgesetzte Klausurfrage anhören.
**B-335, B-286, B-337, B-338 `abgenommen`.**

### Retrospektive

**Nachschau:** Teil 1 (`0fcff19`, B-331/B-141/B-277/B-333/B-334) von einem frischen `pugling-reviewer`. **Entgangen:**
[B-337](backlog/B-337-reuse-folgt-veraltetem-schluessel.md) bei B-277 (🟡) und
[B-338](backlog/B-338-doppeltes-wort-schreibweise-ungetestet.md) bei B-331/B-141 (🟡 + 🟢); beide im Sprint behoben.
B-333, B-334 sauber. `nachgeschaut` an allen fünf, mit Prüfpunkten im Verlauf.

**Was die eigenen Tore durchgelassen haben:** (1) B-277 Entscheidung 2 setzte „derselbe Schlüssel" gleich „dasselbe Wort" –
ein „also", das der `PATCH` widerlegt. Das ist (AD) zum **dritten** Mal: eine Entscheidung beruft sich auf eine
Gleichsetzung, ohne sie am Code nachzuweisen (hier: wer schreibt `Key`, und hält er ihn nach?). (2) B-338: wieder ein Fall,
dessen zwei Eingaben sich nicht in der Eigenschaft unterscheiden, die die Prüfung behauptet (zeichengleich, wo es um
Faltung ging) – die Regel aus pm-loop Step 5 („die zwei Werte müssen sich im Setup unterscheiden") hat sie nicht
erzwungen, weil der Fall „doppelt" hieß und doppelt war. (3) B-286: eine Live-Region in einem Zweig, den frühe Returns
ersetzen, entsteht neu – der statische Wächter sieht nur `&&`/`?`, nicht Returns und `key`.

**Vorschläge, nicht gelandet (Freigabe 3):**

- **(AG)** Tor-Erweiterung `liveRegions.test.ts`: ein Region-Baustein (`TaskAnnouncer`, `StatusBanner` …) muss im
  **äußersten** Return seiner Komponente stehen oder in einer Komponente ohne frühen Return; ein `key=` an einem
  Region-Baustein ist rot. (Würde B-286 Fund 1 und 2 mechanisch fangen.)
- **(AH)** Prosa in `docs/backlog/README.md` → Ausformulieren, Präzisierung von (AD): „Setzt eine Entscheidung zwei Größen
  gleich (‚Schlüssel = Inhalt', ‚Code = Fehlerart'), nennt sie den Schreiber jeder Größe mit `Datei:Zeile` – wer die eine
  ändert, ohne die andere nachzuziehen, widerlegt das ‚also'."
- **(AI)** Prosa in pm-loop Step 5: „Behauptet ein Fall eine Faltung (Groß/Klein, Akzente, Schlüssel), unterscheiden sich
  seine zwei Eingaben genau in dem, was gefaltet wird."

**Warum der Lauf hier endet (Limit 2):** die Nachschau von Teil 2 fand im Increment von Teil 1 – dem Increment dieses
Laufs – zwei 🟡 (B-337, B-338). Dass sie behoben sind, ändert die Regel nicht.

## Ende Lauf K

Endgrund: **Limit 2** – die Nachschau von Teil 2 fand im Increment von Teil 1 zwei Defekte (B-337, B-338, behoben). Zwei
Teile, **neun Stories abgenommen**: B-331, B-141, B-277, B-333, B-334 (Teil 1, Zähler 5), B-335, B-286, B-337, B-338 (Teil 2,
Zähler 2); dazu sieben nachgeschaut (B-330, B-332 aus Lauf J; die fünf aus Teil 1). Commits `0fcff19` und der
Abschluss-Commit. Alles **ungepusht**.

**Offene Entscheidungen für den Nutzer:**

1. **Retro-Vorschläge (AE), (AF)** aus Teil 1 und **(AG)** (Tor), **(AH)**, **(AI)** aus Teil 2; dazu unverändert (AC), (AC′),
   (AD) aus Lauf J – (AD) ist jetzt dreimal belegt – und ältere.
2. **Gerätecheck B-286:** Screenreader-Ansage einmal am echten Gerät anhören.
3. **Vertrag:** `POST creator/vocabulary` hat das additive Feld `reuseExisting` (B-277) – gegenlesen, ob der Name passt.
4. Unverändert aus Lauf J: B-328 (`Frage`), B-329 (Freigabe für `invalid_reference` im Übungspfad).
5. Nächste baubare `Defekt`-Kandidaten: B-336 (KI-Creator-Dublettenregel), B-280, B-275, B-262/B-266; zurückgestellt aus
   B-337: `GetOrCreateAsync` folgt ebenfalls dem gespeicherten Schlüssel.
