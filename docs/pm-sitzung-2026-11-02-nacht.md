---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-11-02, Lauf M, Lauf N, Lauf O]
---

# PM-Sitzung: Nachtläufe 2026-11-02 – Lauf M, N, O

**Datum:** 2026-11-02  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Drei Nachtläufe nacheinander abschließen, je mit den baubaren `Defekt`-Stories nach Themen, beginnend mit den
Kandidaten aus [Ende Lauf L](pm-sitzung-2026-10-30-nacht.md#ende-lauf-l).

Auftrag: „Schließe 3 Nachtläufe ab. Kommt es zu Nachfragen, beantworte diese selber nach bestem Wissen und Gewissen." Es
gelten die Freigaben 1–10 aus [nachtlauf.md](nachtlauf.md); die Erlaubnis, Nachfragen selbst zu beantworten, betrifft
Fragen **an** den Lauf (welches Thema, welcher Weg), nicht die Sperren der Freigaben – `Wunsch`/`Frage` ohne Freigabe-Zeile
bleiben ungegrillt, Retro-Vorschläge werden nicht gelandet. Ausgangslage: HEAD `d0afbc0`, Baum sauber, alles seit Lauf J
ungepusht. Index zu Beginn: „Nachgeschaut 268 von 273" – offen sind B-278, B-348 (Lauf L, Teil 4), B-351 (Nachtrag) und
B-344/B-345 (vom Nutzer in `3190f9e` behoben, nachgetragen); die Nachschau des ersten Sprints nimmt sie.

**Nicht angefasst, weil gesperrt:** B-329 (Vertragsbruch ohne Freigabe), B-310 (`vertragsbruch` ungeklärt), B-328 und die
übrigen `Frage`-/`Wunsch`-Stories auf `idee`/`ausformuliert` ohne Freigabe-Zeile.

## Lauf M, Teil 1 — Der Store erkennt ein Wort an dem, was es sagt (B-346, B-347, B-349)

**Sprint-Ziel:** *Der Creator bekommt beim Anlegen und in der Übung für dasselbe Wort denselben Store-Eintrag und für zwei
verschiedene Wörter zwei – egal, wie der Schlüssel zusammengesetzt wurde, was ein `PATCH` geändert hat oder was im Batch
davor stand.*

**Umfang:** [B-346](backlog/B-346-vocabkey-trenner-mehrdeutig.md), [B-347](backlog/B-347-store-speichert-wort-ungetrimmt.md),
[B-349](backlog/B-349-batch-treffer-haengt-an-der-reihenfolge.md) – alle `Defekt`, unter Freigabe 1 ausformuliert, gegrillt,
geschätzt (S, S, XS). Ein Thema: „dasselbe Wort" im Store.

**Entwickler-Brief:** Der Verbundschlüssel ist Bezeichner, nicht Vergleich. `VocabKey.Identity` (vier einzeln gefaltete
Teile) wird der eine Vergleichsbegriff für Store-Treffer, Übungs-Dublettenprüfung und Peek; `FindManyAsync` nimmt jeden
gespeicherten Eintrag des Sprachpaars als Kandidaten (schmale Projektion, Faltung im Speicher) plus die ungespeicherten.
Keine Migration, gespeicherte Schlüssel unverändert. Testweg: rote Proben je Story, Mutanten je AK und Entscheidungsteil,
volle Suite.

### Iteration

- Rote Proben: **6 failed** (erwartet 6) – B-346 2, B-347 3, B-349 1; jede am erwarteten Ort (Statuscode bzw. `400` im
  Anlegen, bei B-349 die zweite Zusicherung).
- Nach dem Fix volle Suite **1039/1039**, Build der Solution ohne Warnung.
- Mutanten: „Identität = Verbundschlüssel" **2 failed**; „Kandidaten über die NOCASE-Wortspalte" **5 failed**; „Sprachen
  exakt" **1 failed**. „Neuester gewinnt" und „ohne ungespeicherte" blieben zunächst **grün** – zwei Teilentscheidungen
  ohne Zusicherung (pm-loop Step 5); je ein Test ergänzt, danach je **1 failed**.
- Nach dem Review: volle Suite **1045/1045** (darin schon die drei Tests aus Teil 2, siehe dort), Build ohne Warnung.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | Klassen-Doku von `VocabKey` nennt den Schlüssel noch „den Begriff von dasselbe Wort" | berichtigt: Schlüssel ist Bezeichner, Vergleich ist `Identity`, Client faltet mit `Part` | nein (Doku) |
| 2 | `pugling-reviewer` 🟡 | B-349-Test behauptet „rot, wenn der ganze Tracker zählt" – im neuen Aufbau nicht erzeugbar | Beschriftung auf den echten roten Fall, Service-Satz gekürzt | nein (Kommentar) |
| 3 | `pugling-reviewer` 🟢 | veraltete Testkommentare („key", „NOCASE-Kandidat") | in drei Testdateien nachgezogen | nein |
| 4 | `pugling-reviewer` 🟢 | SQL-`trim()` faltet nur Leerzeichen, der Kommentar behauptete mehr | Kommentar präzisiert | nein |
| 5 | `pugling-reviewer` 🟢 | `_` und Leerraum-Folgen auf dem Server-Pfad ungetestet, im Vertrag ungenannt | Theorie-Zeile `#_lake`/`#  lake`, Vertragstext ergänzt | nein (🟢) |
| 6 | `pugling-reviewer` 🟢 | ein Scan des Sprachpaars je Batch-Zeile, „Hunderte je Paar" ungemessen | bewusst stehen gelassen (Entscheidung 1 von B-347 nennt die Kosten) | nein |

**Zähler Teil 1: 0.**

**Rollengang:** Creator maschinell – die neuen Integrationstests sind sein Weg gegen die In-Process-API (Anlegen mit
`reuseExisting`, Batch, Übung mit zwei Paaren gleicher Schlüssel-Naht); kein Browser-Gang im Vokabel-Store. Vater und Sohn
als Regressionszeugen: E2E-Gesamtlauf **102 passed** (`exit=0`), gestartet nach dem letzten Produktcode-Edit (ein erster
Start lief parallel zu Mutanten-Builds und wurde abgebrochen, siehe Retrospektive). **B-346, B-347, B-349 abgenommen.**

### Retrospektive

**Nachschau:** Lauf L, Teil 4 (`d8cc6b9`: B-348, B-278) per frischem `frontend-reviewer`; B-351 (`d0afbc0`) und die vom
Nutzer behobenen B-344/B-345 (`3190f9e`) per frischem `pugling-reviewer`. **Entgangen:**
[B-352](backlog/B-352-radiogruppen-ohne-pfeiltasten.md) bei B-348 (🟡, `role="radio"` ohne Pfeiltasten),
[B-353](backlog/B-353-leerer-label-slug-trifft-emoji-synonym.md) und [B-354](backlog/B-354-synonym-gegen-fremden-slug-ungetestet.md)
bei B-351 (🟡 Regression bei leerem Label-Slug, 🟡 Slug-Hälfte ungetestet). B-278, B-344, B-345 sauber (B-344s Restlücke war
schon B-347). Zwei 🟢 zu B-348/B-278 (Intensitäts-Zustand, „Klasse leer") hier behoben, ohne Zählung. `nachgeschaut` an allen
fünf, mit Prüfpunkten. Index danach: siehe Ende Lauf M.

**Was die eigenen Tore durchgelassen haben:** (1) Die (AJ)-Zeile je AK hat gewirkt – zwei Teilentscheidungen ohne
Zusicherung fielen **vor** dem Review auf. Aber ein Testkommentar nannte eine Mutante, die nie gemessen war (B-349: „ganzer
Tracker"), und die gemessene stand nur im Verlauf; der Reviewer fand den Unterschied. (2) Ein Prozessfehler ohne Folge:
Mutanten-Builds liefen parallel zum Start des E2E-Servers, der aus denselben Quellen baut – er hätte eine Mutante
ausliefern können. Bemerkt, Lauf abgebrochen, Server beendet, neu gestartet. (3) Von außen: nichts.

**Vorschläge, nicht gelandet (Freigabe 3):**

- **(AO)** Prosa in pm-loop Step 5: „Die ‚Rot, wenn'-Zeile eines Tests nennt die Mutante, die im `## Verlauf` gemessen ist –
  wortgleich; eine gedachte Mutante wird gemessen oder gestrichen."
- **(AP)** Tor im Mutanten-Werkzeug: ein Mutanten-Lauf verweigert den Start, solange auf `:5200`/`:5173` ein Server lauscht
  (der E2E-Server baut aus denselben Quellen). Mechanisch billig; heute gibt es kein gemeinsames Mutanten-Skript im Repo,
  also wäre es zugleich dessen Einführung.
