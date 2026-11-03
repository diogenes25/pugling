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

## Lauf M, Teil 2 — Ein Text benennt genau ein Schlagwort, auch ohne Buchstaben (B-353, B-354)

**Sprint-Ziel:** *Der Vater, der dem Kind ein Interesse mit einem Emoji gibt, bekommt ein eigenes Schlagwort; der Creator
kann sich darauf verlassen, dass jede Hälfte der Synonym-Regel von einem Test gehalten wird.*

**Umfang:** [B-353](backlog/B-353-leerer-label-slug-trifft-emoji-synonym.md) und
[B-354](backlog/B-354-synonym-gegen-fremden-slug-ungetestet.md), beide aus der Nachschau von Teil 1, `Defekt`, XS, unter
Freigabe 1 gegrillt. Ein Thema: die Synonym-Regel aus B-351.

**Entwickler-Brief:** Ein Leer-Wächter am Label-Vergleich in `EnsureAsync`; zwei Tests für die bisher ungetragenen Teile
von B-351 (Slug in der Konfliktmenge, „bereits geführt" per Slug). Testweg: `InterestTagLabelTests`, Mutanten je Teil.

### Iteration

- **B-353:** rote Probe **1 failed** (erwartet 1); nach dem Fix grün; Mutante „ohne Leer-Wächter" **1 failed**.
- **B-354:** beide Tests vor jeder Mutante grün (Testlücke); Mutante „Slug fehlt in der Konfliktmenge" **1 failed**,
  „`carried` per Rohtext" **1 failed**.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | AK 2 von B-354 fängt nur die case-sensitive Rohtext-Mutante; `OrdinalIgnoreCase` bliebe grün | erneut geschickte Schreibweise weicht über Groß/Klein hinaus ab (Bindestrich); beide Mutanten **1 failed** | ja |
| 2 | `pugling-reviewer` 🟢 | B-353-Zusicherung nur negativ („nicht der Eigentümer") | positive Zusicherung auf den eigenen Slug ergänzt; Mutante weiter **1 failed** | nein (🟢) |

Geprüft und sauber laut Prüfer: kein Geschwisterpfad mit demselben Leer-Problem (`SynonymClashAsync`, `SynonymTakenAsync`,
Create/Update, `EnsureManyAsync`, `Pending`). **Zähler Teil 2: 1.** Volle Suite danach **1045/1045**. Ein Lauf des
Test-Tors fiel während einer Mutanten-Probe rot (die Mutante stand im Quelltext) – der rote Fall war genau die Probe; nach
der Wiederherstellung grün.

**Rollengang:** Vater maschinell – der B-353-Test setzt das Kind-Interesse über die echte Supervisor-Route; Creator über die
Schlagwort-Tests der Creator-Route. Sohn als Regressionszeuge. E2E-Gesamtlauf **102 passed** (`exit=0`, gestartet nach dem
letzten Code-Edit). **B-353, B-354 abgenommen.**

### Retrospektive

**Nachschau:** Teil 1 (`ffbb0b6`: B-346, B-347, B-349) per frischem `pugling-reviewer`: **sauber**, kein 🔴/🟡. Drei 🟢
(Sprachkürzel mit anderem Leerraum als Leerzeichen fallen aus den Kandidaten; zwei gleiche Paare in einem
`GetOrCreateManyAsync`-Aufruf ungetestet; `Slug` entfernt auch nichtlateinische Diakritika) →
[B-355](backlog/B-355-store-faltung-restkanten.md), P4. `nachgeschaut` an allen drei, mit Prüfpunkten. Index danach:
**Nachgeschaut 276 von 278** (offen: B-353, B-354 – dieser Teil).

**Was die eigenen Tore durchgelassen haben:** Die Mutante zu B-354 AK 2 war in ihrer schwächsten Form gemessen (Rohtext,
case-sensitiv); die natürlichere Form (`OrdinalIgnoreCase`, wie `Clean()` selbst dedupliziert) blieb grün, der Reviewer
fand es. Dazu derselbe Prozessfall wie in Teil 1 in anderer Gestalt: das Test-Tor (Stop-Hook) lief, während eine
Mutanten-Probe im Quelltext stand, und meldete deren rotes Ergebnis als Tor-Fehler.

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(AQ)** Prosa in pm-loop Step 5 neben (AJ): „Eine Vergleichs-Mutante wird in ihrer stärksten naheliegenden Form gemessen
  – bei Textvergleichen auch ohne Groß/Klein –, nicht nur in der schwächsten." Gemeinsam mit (AP) wäre der Prozessfall
  gedeckt: ein Mutanten-Werkzeug, das vor dem Start prüft, ob ein Server lauscht, und die Datei unter einer Sperre hält,
  die das Test-Tor respektiert.

## Ende Lauf M

Endgrund: **beide Sprints geschlossen**, keine Halt-Bedingung gegriffen (Zähler 0/1, keine Retro hat ein Tor gelandet, die
Nachschau über Teil 1 fand nichts im Increment dieses Laufs). **Fünf Stories abgenommen:** B-346, B-347, B-349 (Teil 1);
B-353, B-354 (Teil 2). Nachgeschaut: Lauf L Teil 4 + B-351 + B-344/B-345 und Teil 1 dieses Laufs; Teil 2 ist **unbeobachtet**
(Nachschau in Lauf N). Entgangen gefunden: B-352 (bei B-348), B-353/B-354 (bei B-351). Neu: B-355 (P4). Alles **ungepusht**.

**Offene Entscheidungen für den Nutzer:** Retro-Vorschläge (AO), (AP), (AQ); dazu unverändert (AC)–(AI) außer (AD)/(AH).

## Lauf N, Teil 1 — Jede Bedienung hält, was sie ansagt (B-350, B-352)

Lauf N beginnt nach Lauf M ohne Unterbrechung; Ausgangslage `110c0a2`. Kandidaten aus Ende Lauf M: die Entgleitung B-352
und B-350 (Nachschau Lauf L, Teil 3).

**Sprint-Ziel:** *Der Vater wählt in jeder Auswahlgruppe mit den Pfeiltasten und verlässt sie mit einem Tab; der Sohn kommt
aus dem Wartebild der Klausur zur Basis, ohne den Browser zu bemühen.*

**Umfang:** [B-350](backlog/B-350-wartebild-der-klausur-ohne-ausweg.md) (XS) und
[B-352](backlog/B-352-radiogruppen-ohne-pfeiltasten.md) (S), beide `Defekt`, unter Freigabe 1 ausformuliert, gegrillt,
geschätzt. Ein Thema: Bedienwege ohne Sackgasse und ohne leeres Versprechen.

**Entwickler-Brief:** Frontend allein. Ein Helfer `radioGroupKeys` (`lib/ui.ts`, framework-frei) am Container jeder
Radiogruppe aus Knöpfen, roving tabindex an jeder Option, ein Wächter über alle Quellen; ein Geister-Knopf im Wartebild.
Testweg: Vitest je Taste, Wächter mit Selbsttest, E2E im Assistenten (Pfeil und Tab im echten Browser), `SohnTest.test.tsx`.

### Iteration

- **B-350:** rote Probe **1 failed** (erwartet 1), danach grün.
- **B-352:** rote Probe **6 failed** (erwartet 6); sieben Gruppen in vier Dateien umgestellt; Mutanten „ohne Umlauf" 2,
  „jede Taste wandert" 1, „Sperre ignoriert" 1 failed; Wächter fällt bei zurückgesetztem `VaterMedia.tsx` (2 Funde); im
  Browser „ohne Helfer" und „ohne `tabIndex`" je **1 failed** an ihrer Zeile.
- Vitest **597/597**, `tsc -b` sauber, Assistenten-Spec **2 passed**.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | B-350: nach „Zur Basis" wirken hängende Aufrufe weiter – ein später 404 holt das Kind von einer anderen Seite zurück, ein später fertiger Versuch feiert dort | Ref `gone` (StrictMode-fest), geprüft vor Umleitung und Feier; rote Probe **2 failed**, drei Mutanten je **1 failed** | ja |
| 2 | `frontend-reviewer` 🟢 | Pfeile mit Alt/Strg/Cmd ändern die Wahl (Alt+Links ist „Zurück") | ausgenommen, Test; Mutante **1 failed** | nein |
| 3 | `frontend-reviewer` 🟢 | Wächter prüft nur, *dass* `tabIndex` steht, nicht dass er wandert | verlangt `… ? 0 : -1`, Selbsttest | nein |
| 4 | `frontend-reviewer` 🟢 | Wächter übersieht eine Gruppe, deren Optionen aus einem Baustein anderer Datei kommen | zählt jetzt jede Datei mit `radiogroup` ohne native Radios | nein |
| 5 | `frontend-reviewer` 🟢 | gesperrte gewählte Option → Gruppe ohne Tab-Stopp | als Voraussetzung im JSDoc (Entscheidung 2: kommt nicht vor) | nein |
| 6 | `frontend-reviewer` 🟢 | Fokus fällt beim Erscheinen der Frage auf `<body>` (wie im Fehlerbild) | nur vermerkt | nein |

**Zähler Teil 1: 1.** Vitest danach **601/601**, `tsc -b` sauber.

**Rollengang:** Vater im echten Browser über die Assistenten-Spec (Pfeiltaste wählt und fokussiert, Tab verlässt die
Gruppe); Sohn über die Komponententests des Wartebilds – ein hängender Start ist im E2E nur mit einem künstlich kaputten
Server zu haben (argumentierte Ausnahme, pm-loop Step 6), seine Specs im Gesamtlauf grün; Creator Regressionszeuge.
E2E-Gesamtlauf **102 passed** (`exit=0`, nach dem letzten Edit). **Gerätecheck offen:** die Radiogruppen einmal mit
Screenreader und Pfeiltasten bedienen. **B-350, B-352 abgenommen.**

### Retrospektive

**Nachschau:** Lauf M, Teil 2 (`51783ae`: B-353, B-354) per frischem `pugling-reviewer`: **sauber**, zwei 🟢 (Entscheidungstext
von B-354 nannte noch „Groß/Klein" – berichtigt; Emoji-Label per `PATCH` nicht setzbar – Produktfrage, ohne Story).
`nachgeschaut` an beiden, mit Prüfpunkten.

**Was die eigenen Tore durchgelassen haben:** B-350 fügte einen Ausweg in einen Bildschirm mit laufenden Aufrufen ein und
übersah, dass ein neuer Ausweg zugleich ein neuer Abbruchpfad für jeden ausstehenden `await` ist. Die Story dachte nur an
das Bild, nicht an die Aufrufe dahinter; das Repo kennt die Klasse (Gedächtnis „Effekt-Doppellauf", `alive`-Flags), aber
keine Regel verbindet sie mit „Navigation hinzufügen".

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(AR)** Prosa in `frontend/CLAUDE.md`: „Wer einem Bildschirm einen Weg hinaus gibt, prüft jede Fortsetzung nach einem
  `await` darin auf ‚noch da?' – `navigate` bleibt nach dem Aushängen wirksam (react-router 6)." Mechanisch nur grob
  (ein Wächter, der in Dateien mit `nav(` nach `await` ohne Ref-Prüfung sucht, wäre laut und unscharf).

## Lauf N, Teil 2 — Jede Spec kennt die Id ihres eigenen Kindes (B-255)

**Sprint-Ziel:** *Der Entwickler, der den Vater→Sohn-Durchstich per E2E fährt, bekommt für jedes angelegte Kind dessen Id –
unabhängig davon, welche Tabelle zuerst lädt und ob der Name schon existiert.*

**Umfang:** [B-255](backlog/B-255-kinderzeile-per-namen-zweideutig.md) (`Defekt`, S, unter Freigabe 1 gegrillt). Ein
Sprint aus einer Story; die Testlücken B-272/B-273/B-276 sind ein anderes Thema (Feldhilfe, Live-Regionen) und bleiben draußen.

**Entwickler-Brief:** Ein gemeinsamer Helfer `createChild(page, name, pin)` in `e2e/helpers.ts`, der die Id aus der
POST-Antwort liest; alle Stellen ziehen darauf um. Testweg: rote Probe mit dem alten Muster im Helfer, neue Spec mit zwei
gleichnamigen Kindern, Gesamtlauf.

### Iteration

- Rote Probe (Helfer mit altem Muster): `kind-anlegen-id.spec.ts` **1 failed** (Strict-Mode-Verstoß, erwartet 1); mit dem
  Fix **1 passed**. Zehn Stellen umgestellt; E2E-Gesamtlauf **103 passed** (`exit=0`).

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | vier weitere Stellen (`punkte-empfehlung` 3×, `vater-von-null`) nehmen die Id aus einem Namens-Link; AK 2 zu eng gemessen | drei auf den Helfer, eine liest die POST-Antwort; Prüf-grep erweitert | ja |
| 2 | `frontend-reviewer` 🟡 | gescheitertes Anlegen hängt bis zum Test-Timeout ohne Grund | Status/Text zugesichert, Warten auf 10 s begrenzt; Probe PIN „1" fällt nach 10 s an `helpers.ts:42` | ja |
| 3 | `frontend-reviewer` 🟢 | die neue Spec unterscheidet die Kinder nur über „zwei Ids + eindeutiger Name" | als Kommentar benannt | nein |

**Zähler Teil 2: 2.**

**Rollengang:** Die Specs sind hier der Weg selbst: Vater und Sohn fahren zwölf Specs über den neuen Helfer, alle grün;
Creator Regressionszeuge. E2E-Gesamtlauf **103 passed** (`exit=0`, nach dem letzten Edit). **B-255 abgenommen.**

### Retrospektive

**Nachschau:** Teil 1 (`b73bcde`: B-350, B-352) per frischem `frontend-reviewer`: **sauber**, fünf 🟢 (erneuter Klick auf die
gewählte Option, `preventDefault` ohne Zusicherung, Wächter-Ausnahme je Datei, Serie nach dem Verlassen nicht aufgefrischt,
negativer `celebrate`-Fall ohne Erreichbarkeits-Beleg) → [B-356](backlog/B-356-radiogruppe-und-wartebild-restkanten.md), P4.
`nachgeschaut` an beiden, mit Prüfpunkten.

**Was die eigenen Tore durchgelassen haben:** AK 2 („keine Spec liest die Id mehr aus einer über den Namen gefundenen
Zeile") wurde mit einem `grep` nach **einer** Form des Musters belegt; die zweite Form (Link + `href`) entging. Das ist
dieselbe Klasse wie (AN) – ein Beleg per Suche nennt seine Suchmuster und sucht nach Fund- **und** Gegenformen –, nur an
einem AK statt an einer Konvention.

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(AS)** Ergänzung zu (AN) in `docs/backlog/README.md` („Ausformulieren heißt gegen den Code belegen"): „Ein AK der Form
  ‚nirgends mehr X' wird nicht mit der Suche nach der Gestalt belegt, die man umgebaut hat, sondern nach der Handlung
  (hier: ‚wo entsteht eine Kind-Id?')." Mechanisch hier sogar greifbar: ein Wächter über `e2e/*.ts`, der `childId =` nur aus
  `createChild(` oder einer Antwort zulässt.

## Ende Lauf N

Endgrund: **beide Sprints geschlossen**, keine Halt-Bedingung gegriffen (Zähler 1/2, keine Retro hat ein Tor gelandet, die
Nachschau über Teil 1 fand nichts im Increment dieses Laufs). **Drei Stories abgenommen:** B-350, B-352 (Teil 1); B-255
(Teil 2). Nachgeschaut: Lauf M Teil 2 und Teil 1 dieses Laufs; Teil 2 ist **unbeobachtet** (Nachschau in Lauf O). Neu:
B-356 (P4). **Gerätecheck offen:** Radiogruppen mit Screenreader und Pfeiltasten. Alles **ungepusht**.

**Offene Entscheidungen für den Nutzer:** Retro-Vorschläge (AR), (AS); dazu (AO)–(AQ) aus Lauf M.

## Lauf O, Teil 1 — Die Proben des Beispielkatalogs messen, was sie behaupten (B-261, B-265, B-274)

Lauf O beginnt nach Lauf N ohne Unterbrechung (Kalendertag inzwischen 2026-11-03); Ausgangslage `cc19020`.

**Sprint-Ziel:** *Der Entwickler, dessen Suite an einer Probe des Beispielkatalogs rot wird, weiß, ob der Start-Pfad der App
kaputt ist oder die Maschine nur belastet war – und ein Rückbau des Start-Pfads fällt überhaupt auf.*

**Umfang:** [B-261](backlog/B-261-produktionspfad-des-beispielkatalogs-ungetestet.md) (S),
[B-265](backlog/B-265-sperr-zaehler-zusicherung-kann-nie-fallen.md) (XS),
[B-274](backlog/B-274-atomares-schreiben-flackert-wieder.md) (XS) – alle `Defekt`, unter Freigabe 1 gegrillt. Ein Thema:
`OpenApiExampleCatalog` und seine zwei Testklassen. Draußen bleiben die übrigen Testlücken (B-272/B-273/B-276, Frontend)
und die Doku-Generator-Defekte B-259/B-260 (eigenes Thema).

**Entwickler-Brief:** Eine Naht `Load(contentRoot, open, pause)` unter dem öffentlichen `Load(string)`; vier neue Fälle
(Inhaltspfad mit `ReadAttempts`, fehlender Katalog, Pause als Untergrenze, echte Sperre). In der Nebenläufigkeitsprobe die
leere Spur weg, „gehalten" über alle zehn Fenster, Verhungern eigens. Testweg: Mutanten je AK, volle Suite.

### Iteration

- **B-261:** neue Fälle vor jeder Mutante grün (Testlücke). Mutanten „`ReadAttempts` → 1" 2, „öffentlich direkt" 1, „ohne
  Pause" 2, „ohne `File.Exists`" 1 failed. Retry-Klasse **9/9**.
- **B-265/B-274:** Mutante „unsicherer Schreiber" **1 failed** an der Summen-Zusicherung: 925 ok gegen 13 639 gesperrt, alle
  zehn Fenster in der Meldung. Probe **2/2**. Für das Flackern selbst gibt es keine rote Probe (nicht reproduzierbar).

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | Test-Tor rot + `pugling-reviewer` 🟡 | der neue Fall „echte Sperre" ist zeitabhängig (Fenster real ~200 ms, Freigabe am Thread-Pool); unter Volllast **1 failed** | eine Methode mit optionalen Nähten statt zwei Überladungen, der Fall entfällt | ja |
| 2 | `pugling-reviewer` 🟡 | Untergrenze `>= 80 ms` unter Windows nicht garantiert | `>= 50 ms`, Grund im Kommentar | ja |
| 3 | `pugling-reviewer` 🟡 | Schleifen-Kommentar beschreibt die alte Regel | gekürzt, Begründung mit den Lauf-O-Zahlen an die Summe | nein (Kommentar) |
| 4 | `pugling-reviewer` 🟢 | fünfter Versuch liest die frische Datei von der Platte | aus dem Speicher | nein |
| 5 | `pugling-reviewer` 🟢 | leerer `catch` in `TempRoot.Dispose` | begründet | nein |
| 6 | `pugling-reviewer` 🟢 | zweite öffentliche Überladung | mit Fund 1 gegenstandslos | nein |

**Zähler Teil 1: 2.** Volle Suite danach **1048/1048** (ein Fall weniger: der zeitabhängige ist bewusst entfernt).

**Rollengang:** Der Entwickler ist hier die Rolle: die Proben sind sein Weg, jede mit benannter Mutante. Creator, Vater und
Sohn als Regressionszeugen – E2E-Gesamtlauf **103 passed** (`exit=0`, nach dem letzten Edit; der Dev-Server lädt den
Beispielkatalog über `Load`). **B-261, B-265, B-274 abgenommen.** Für B-274 bleibt ehrlich offen, ob das Flackern weg ist –
das zeigt nur die Zeit; tritt es wieder auf, nennt die Meldung jetzt jedes Fenster.

### Retrospektive

**Nachschau:** Lauf N, Teil 2 (`cc19020`: B-255) per frischem `frontend-reviewer`: **sauber**. AK 2 diesmal nach der Handlung
gesucht (jede Stelle, an der in `e2e/*.ts` eine Kind-Id entsteht) – keine über einen Namen. Drei 🟢 →
[B-357](backlog/B-357-e2e-helfer-restkanten.md), P4. `nachgeschaut` gesetzt. Index danach: **Nachgeschaut: 281 von 284**.

**Was die eigenen Tore durchgelassen haben:** Ein neuer zeitabhängiger Fall (echte Dateisperre mit Freigabe nach 120 ms)
wurde nur gefiltert auf einer ruhigen Maschine gemessen und erst vom Test-Tor unter Volllast rot – obwohl dieselbe
Testklasse im Kommentar von `CountFailedReadsDuring` festhält, dass der Thread-Pool unter der vollen Suite Arbeit weit
über 300 ms zurückhält. Das Wissen stand im Repo, nur nicht an der Stelle, an der der neue Fall entstand. Das Test-Tor hat
gehalten – genau dafür ist es da.

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(AT)** Prosa in `backend/Pugling.Api.Tests/CLAUDE.md`: „Ein neuer Fall mit Zeitschranke (`Delay`, `Sleep`,
  Stoppuhr) läuft vor dem Review einmal in der vollen Suite; die Schranke steht mit ihrem Abstand zum Mutanten im
  Kommentar, nicht mit dem Idealwert." Mechanisch denkbar als Wächter, der `Task.Delay`/`Stopwatch` in Testdateien ohne
  Kommentarzeile meldet – laut und unscharf, darum Prosa.

## Ende Lauf O

Endgrund: **Sprint geschlossen**, keine Halt-Bedingung gegriffen (Zähler 2, keine Retro hat ein Tor gelandet, die Nachschau
über Lauf N Teil 2 fand nichts). **Drei Stories abgenommen:** B-261, B-265, B-274. Teil 1 dieses Laufs ist **unbeobachtet**
(Nachschau im nächsten Lauf). Neu: B-357 (P4). Alles **ungepusht**.

## Ende der Nacht — drei Läufe

| Lauf | Teile | Abgenommen | Zähler | Commits |
| --- | --- | --- | --- | --- |
| M | 2 | B-346, B-347, B-349; B-353, B-354 | 0 / 1 | `ffbb0b6`, `51783ae`, `110c0a2` |
| N | 2 | B-350, B-352; B-255 | 1 / 2 | `b73bcde`, `cc19020` |
| O | 1 | B-261, B-265, B-274 | 2 | siehe `git log` |

**Elf Stories abgenommen**, drei Entgleitungen aus älteren Läufen gefunden und behoben (B-352 bei B-348, B-353/B-354 bei
B-351), vier P4-Restkanten-Stories neu (B-355, B-356, B-357; dazu B-352 als Entgleitung). Nachgeschaut bis auf Lauf O, Teil 1.

**Offene Entscheidungen für den Nutzer:**

1. **Retro-Vorschläge (AO)–(AT)** – keiner gelandet (Freigabe 3). Der Auftrag „Nachfragen selbst beantworten" wurde so
   gelesen, dass er Fragen an den Lauf deckt, nicht die Freigaben-Sperren; (AP) und (AS) wären Tore.
2. **Gerätecheck:** die Radiogruppen des Vater-Webs mit Screenreader und Pfeiltasten bedienen (B-352).
3. **Produktfrage aus der Nachschau (ohne Story):** ein Emoji-Label lässt sich anlegen, aber per `PATCH` nicht setzen.
4. **B-274:** ob das Flackern weg ist, zeigt nur die Zeit.
