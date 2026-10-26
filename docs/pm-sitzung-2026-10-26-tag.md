---
tags: [typ/protokoll, bereich/pm]
aliases: [Läufe nach der Morgen-Entscheidung 2026-10-26]
---

# PM-Sitzung: Läufe A–E nach der Morgen-Entscheidung 2026-10-26

**Datum:** 2026-10-26  ·  **Moderation:** PM (unbeaufsichtigt nach dem Dialog)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** die liegengebliebenen Sprints 10, 12 und 7 schließen, danach NP-3, NP-7 und NP-9 fahren, nacheinander.

Auftrag: der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–9. Grundlage ist der Abschnitt
[Morgen-Entscheidung 2026-10-26](pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26) (Commit `732e4ce`):
Jede Nacharbeit läuft mit **neuem Zähler (0)**. Die Retro-Regel zu Abgrenzungs-Testdaten ist seit `732e4ce` in
pm-loop Step 5 gelandet und gilt hier erstmals als Pflichtschritt. Ausgangslage: HEAD `732e4ce`, Baum sauber.

## Lauf A — Fortsetzung Sprint 10 (NP-5)

**Sprint-Ziel (unverändert aus dem fünften Lauf):** *Der Vater kann das Vater-Web am Tablet bedienen, ohne dass
eine Seite seitlich scrollt oder eine Feld-Erklärung abgeschnitten ist, und ein Screenreader kündigt Meldungen und
Kästchen-Gruppen an.* Umfang: B-68, B-263, B-134, B-236.

**Zähler (Freigabe 3): 0** zu Beginn. Fund 1 und 2 aus dem Delta-Review vom 2026-10-25 sind die Nacharbeit selbst und
werden nicht neu gezählt.

### Iteration — B-263 nach Entscheidung 2 (`position: fixed`)

`InfoHint.tsx` rechnet `left`/`top` aus dem Knopf, begrenzt durch das Fenster. Das Popover klappt nach oben, wenn
unten im Fenster kein Platz ist, und schließt bei `resize` sowie bei `scroll`, wenn sich der Knopf bewegt hat.
`.info-hint-pop` ist `position: fixed`. Neue Spec `e2e/feldhilfe-lage.spec.ts`. `responsive-vater.spec.ts` misst gegen
das Fenster plus Treffertest an den Ecken.

Zwei Messungen auf dem Weg, je mit Konsequenz (Einzelheiten im Verlauf von B-263):

1. Ein Schließen bei *jedem* `scroll` schloss das Popover gleich nach dem Öffnen (3 von 4 Tests rot), denn das
   Scrollen, das den Knopf ins Bild holt, kommt erst im nächsten Frame an. Seither wird mit der Lage des Knopfs
   verglichen.
2. Der Treffertest mit dem obersten Element 3 px vom Eck meldete Verdecktes, wo nichts abgeschnitten war:
   Scrollbalken-Trefferzone von `.vater-main` und `border-radius`. Seither `elementsFromPoint` mit 16 px Abstand.

| Probe | erwartet | gemessen |
| --- | --- | --- |
| HEAD-Stand gegen `feldhilfe-lage` | 3 rot | **3 failed** (`:69` Treffertest, `:93` Klappen, `:109` Schließen) |
| `up = false` | Klappen rot | **2 failed** (Klappen + Tabelle, die bei 1024 px ebenfalls klappt) |
| ohne `scroll`- / `resize`-Listener | je rot | **`:109`** / **`:114`** |
| Schließen bei jedem `scroll` | rot | `feldhilfe-lage` grün, **`responsive-vater` 1 failed** (`:70`) |
| ohne Klemmen nach links | rot | **22** `overRight`-Meldungen |
| beide Specs / Vitest / `tsc -b` | grün | **4 passed** / **428/428** / sauber |

**Abgrenzungen im Testaufbau (Pflichtschritt, pm-loop Step 5):** *oben statt unten*: Die Vorbedingung `untenZuEng`
(`feldhilfe-lage.spec.ts:88`) trennt beide Fälle, bei „beide gleich“ fällt `:93`. *Ragt über den Rahmen hinaus*:
Die Vorbedingung `ragtHeraus` (`:68`) stellt sicher, dass der Treffertest nicht trivial grün ist. *Ruhelage statt
verschoben*: Der Schritt „Ruhelage bleibt“ (`dx: -6`) steht gegen die 22 verschobenen Popover. *Schließen nur bei
Bewegung*: Die Mutante „immer schließen“ trägt nur `responsive-vater:70`. Das ist benannt, nicht verdoppelt.

### Review (nacheinander, je Bereich)

**Zähler Lauf A (Freigabe 3): 2** nach Runde 1.

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` B-134/B-236 | `TruncationHint` hinter `&&` in `PlanPositions:621` (Filterwechsel stumm) und `VaterClassTests:231` | Region immer gerendert, Bedingung in `total` | ja |
| 2 | `frontend-reviewer` B-134/B-236 | Wächter sieht keine bedingt eingehängten Region-Bausteine; eine eingerückte innere Funktion beendet die Suche nach `return null` | `REGION_BAUSTEINE`, Kopf in Spalte 0, Klammertiefe gegen `return null` in inneren Funktionen; `RemarkWidget` als begründete Ausnahme | ja |

Proben: erweiterter Wächter gegen die alten Aufrufstellen → **2** Funde (erwartet 2). Der Spalte-0-Anker allein meldete
zusätzlich `VaterWizard:526/644` falsch, das hat die Klammertiefe ausgelöst. Mutanten „jedes `return null`“ → 3 failed,
„Kopf eingerückt“ → 2 failed. Vitest **435/435**.

Nicht gezählt, ausdrücklich zurückgestellt: die Hinweise 3–6 (🟢: Selbsttests des Wächters, `TruncationHint`-Vitest,
Name „Freigabe (Startzustand)“, Gruppen-Spec zu weit) → [B-272](backlog/B-272-live-region-und-gruppen-tests-tragen-nicht-alles.md).
Jede Behebung hätte gezählt, und das Review zu B-263 stand noch aus. Das ist dieselbe Abwägung, die der Nutzer bei
B-268/B-269 als richtig bestätigt hat.

**Runde 2 — `frontend-reviewer` zu B-263.** Zähler danach: **4**.

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 3 | `frontend-reviewer` B-263 | Weder unten noch oben Platz (Querformat): Das Popover lief aus dem Fenster und ließ sich fest stehend nicht herscrollen | `top` ins Fenster geklemmt, `max-height` + `overflow-y: auto` | ja |
| 4 | `frontend-reviewer` B-263 | Abgrenzungen ungetragen: senkrechte Schnitte < 16 px, „bleibt offen“ nur zufällig getragen, Schwelle 40 px | `imFenster` mit genauen Koordinaten, Fall Querformat, Fall „Scrollen von vor dem Öffnen“, Probe 3 px | ja |

Proben: ohne Klemmen und Klemmen bis an den Rand → je rot an `:132`, „jedes `scroll`“ → rot im neuen Fall, Schwelle
39 px → rot an `:162`. Specs **6 passed**. Zurückgestellt → [B-273](backlog/B-273-feste-feldhilfe-resize-und-layoutverschiebung.md):
`resize` ohne Prüfung (ungemessen; Chrome Android ab 108 und iOS lösen es nicht aus), Layout-Verschiebung ohne Scrollen,
die nicht prüfbare Linksrand-Regel. Doku-Funde ohne Zählung nachgezogen.

**Beobachtung:** Fund 4 gehört zur Klasse, die der seit heute geltende Pflichtschritt fangen soll. Ich hatte die
Abgrenzungen benannt, aber nur die gegen *meine* Mutanten. Eine Abgrenzung über eine Toleranz (16-px-Saum,
40-px-Scroll) hat der Schritt nicht erfasst.

**Runde 3 — Delta-Review über die Behebungen.** Zähler danach: **5** (Grenze erreicht, nicht überschritten).

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 5 | Delta-Review | Der Querformat-Fall erreichte `max-height` nie (gemessen 101–271 px gegen 344 px) | eigener Fall 768×200, Vorbedingung „höher als das Fenster“, `scrolltInSich`; ohne `max-height` rot an `:148` | ja |

Ohne Zählung: zwei Kommentare nachgezogen (`VaterClassTests`: Region erst nach dem Laden der Arbeit; Wächter-Kopf:
Lücke „`if` mit Lambda in der Bedingung“). Der Hinweis „`REGION_BAUSTEINE` ohne Selbsttest je Name“ ist zurückgestellt
→ B-272 Punkt 5, weil er als Fund 6 den gesamten Lauf beendet hätte.

### Runde — Rollengang / Abnahme

Die Chrome-Extension war nicht verbunden, darum nach Freigabe 6 als E2E. **Vater:** `responsive-vater.spec.ts` (22
Routen bei 768 px, jedes ⓘ aufgeklappt, nichts scrollt seitlich), `feldhilfe-lage.spec.ts` (Tabellen-Rahmen, Klappen,
Querformat, niedriges Fenster, Schließen), `gruppenkopf-benennt-die-gruppe.spec.ts`. Das Sprint-Ziel hält für alles,
was eine Maschine sehen kann. **Sohn, Creator:** Regressionszeugen. Volle Suite grün, die Sohn-Stellen von B-134
(`SohnPractice`, `SohnShop`) fährt `full-flow.spec.ts` mit. **Offen für einen Menschen:** die echte Screenreader-Ansage
der Live-Regionen und Gruppen-Namen, und das Popover am echten Tablet.

Verifikation: `tsc -b` sauber, Vitest **435/435**, Playwright voll nach dem letzten Code-Edit **93 passed** (5,9 min),
danach `feldhilfe-lage` mit dem nur testseitigen Zusatz **6 passed**. **B-68, B-263, B-134, B-236 abgenommen.**

### Retrospektive

**Nachschau:** Die zu Sprint 10 gehörige Nachschau (Sprint 9) ist seit dem fünften Lauf erledigt
([Protokoll 2026-10-24](pm-sitzung-2026-10-24-nacht.md)). Index: **Nachgeschaut 182 von 184**. Die zwei
unbeobachteten sind B-64 und B-71 (Sprint 11), ihre Nachschau ist die erste Pflichthandlung von Lauf B. Sprint 10
selbst ist erst jetzt geschlossen, seine Nachschau fällt darum ebenfalls in die nächste Retro.

**Was die eigenen Tore durchgelassen haben:** Der seit heute geltende Pflichtschritt (Abgrenzungen im Testaufbau) lief
und hat drei Abgrenzungen sauber belegt. Durchgelassen hat er zwei Formen, beide gemessen:

1. **Abgrenzung über eine Toleranz** (Fund 4): ein Treffertest mit 16 px Saum, eine Scroll-Probe von 40 px. Die
   Mutante „beide gleich“ ist dort keine Gleichheit, sondern eine verschobene Grenze unterhalb der Toleranz.
2. **Eine Regel, die die Testdaten nie erreichen** (Fund 5): `max-height` greift bei keinem Popover des Seeds. Die
   Vorbedingung „Grenze wird erreicht“ fehlte.

Wohlgemerkt: Das ist eine Messung an *einem* Lauf. Belegt ist, dass der Schritt diese zwei Formen nicht fängt. Wie
oft sie vorkommen, ist nicht belegt.

**Vorschlag (nur vorgeschlagen, Freigabe 3):** den Pflichtschritt in pm-loop Step 5 um einen Satz erweitern: *„Hat der
Fall eine Toleranz oder Grenze (Saum, Schwelle, Höchstwert), gehört die Mutante ‚Grenze knapp verschoben‘ dazu, und die
Vorbedingung, dass die Testdaten die Grenze überhaupt erreichen.“* Prosa, kein Tor, aus demselben Grund wie der
Schritt selbst.

**Beobachtung aus dem fünften Lauf, jetzt eingelöst:** Die erkannte Testlücke zu AK 1 von B-263 (Klappen nach oben)
ist geschlossen (`feldhilfe-lage.spec.ts:112` im Commit `0ecb31b`).

**Commit:** `0ecb31b` (nicht gepusht).

## Lauf B — Fortsetzung Sprint 12 (NP-8)

**Sprint-Ziel (unverändert):** *Der Vater sieht beim Stöbern in der geteilten Bibliothek, wie andere Creator eine Übung
bewertet haben, sortiert danach und bewertet fremde Übungen selbst; der Autor liest die Urteile, bewertet aber nicht
sein eigenes Material.* Umfang: [B-12](backlog/B-12-uebungen-kuratieren.md) (AK 6 nach der Morgen-Entscheidung
geändert), dazu [B-269](backlog/B-269-bewertungen-reihenfolge-und-updatedat-ungetestet.md) (Freigabe-Zeile vom
2026-10-26, autonom ausformuliert: zwei Testlücken, kein Verhalten). **Zähler: 0.** Fund 6 und 7 aus dem siebten Lauf
sind die Nacharbeit selbst.

### Iteration

- **AK 6 neu:** In `ApplySort` (`sort=-rating`) bleibt der Leitschlüssel „bewertet zuerst“ aufsteigend. Unbewertete
  Übungen stehen in beiden Richtungen hinten. XML-Doku, Frontend-Kommentar und `docs/openapi/v1.json` sind nachgezogen.
- **B-269:** neuer Test `Liste_Nennt_Die_Juengste_Aenderung_Zuerst` (drei Bewerter, einer ändert) und eine
  `updatedAt`-Zusicherung im Überschreib-Test.
- **Fund 6:** Helfer `canRate` (`!isOwner`), Vitest mit dem Grantee-Fall `{isOwner: false, isOwn: true}`.
- **Fund 7:** `role="img"` am Sternen-`span`, dazu eine E2E-Zusicherung `getByRole("img", { name: "5 von 5 Sternen" })`.
- **E2E-Spec auf drei Übungen erweitert**, damit „absteigend“ etwas trägt: Mit nur bewertet/unbewertet wären auf- und
  absteigend nach der neuen Regel gleich.

| Probe | erwartet | gemessen |
| --- | --- | --- |
| Spiegel absteigend (alter Stand) | Sortiertest rot | **1 failed** (`[18,21,20,19]` statt `[20,19,18,21]`); E2E rot (gemessen an `:89` eines Zwischenstands; im Commit fällt der Spiegel an `:93`) |
| Liste nach `Id` / nach `CreatedAt` | Listentest rot | je **1 failed** (`[9,10,11]` bzw. `[11,10,9]` statt `[9,11,10]`) |
| `UpdatedAt` beim Überschreiben nicht gesetzt | rot | **2 failed** (Liste + `Assert.True` im Überschreib-Test) |
| `canRate` liest `isOwn` | Grantee-Fall rot | **1 failed** (genau der Grantee-Fall) |
| Sterne ohne `role="img"` | E2E rot | **1 failed** (`toBeVisible` der neuen Zusicherung) |
| Build / Backend / Vitest / Spec | grün | 0 Warnungen / **918/918** / **438/438** / **1 passed** |

**Abgrenzungen im Testaufbau (Pflichtschritt):** *Unbewertet hinten statt gespiegelt*: Im Sortiertest liegt der Spiegel
`[a,d,c,b]` neben der Erwartung `[c,b,a,d]`. In der E2E-Spec sind Titel- und Anlage-Reihenfolge `[bewährt, neu, schwach]` (nach Fund 3; vorher `[schwach, neu, bewährt]`),
damit trifft keine der vier Titel-/Anlage-Reihenfolgen eine Bewertungs-Reihenfolge. *Jüngste Änderung statt Anlage
oder Id*: drei Bewerter, einer ändert, das ergibt drei verschiedene Antworten. *`isOwner` statt `isOwn`*: Der
Grantee-Fall belegt beide verschieden. *Grenze/Toleranz*: Der Vergleich `updatedAt >` ist strikt, Toleranz 0. Er
hängt an `DateTime.UtcNow` zwischen zwei Anfragen, dazwischen liegt eine dritte. Das ist benannt, weil es die
einzige zeitabhängige Zusicherung ist.

### Review (nacheinander, je Bereich)

**Zähler Lauf B: 4** nach zwei Runden.

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` (+ Test-Tor) | Der Abdeckungs-Wächter erwartete 267 Actions, B-12 brachte 270. Jeder volle Lauf seit `2f7ca24` scheiterte im Assembly-Cleanup, die Konsole zeigte trotzdem „Passed!“ | `FullRunTouchedActions = 270`; Probe mit 267 → **`exit=1`** bei „Passed! 918“ | ja |
| 2 | `pugling-reviewer` | Der Listentest ließ „nach Sternen“ und „`UpdatedAt` nur bei neuen Sternen“ grün (r1 war zugleich jüngste Änderung und bester Wert) | r1 ändert nur den Kommentar; beide Mutanten → **1 failed** | ja |
| 3 | `frontend-reviewer` | Die E2E-Spec bewies nicht, dass der Durchschnitt sortiert („bewertet zuerst, dann Id“ blieb grün) | Titel/Anlage innerhalb der bewerteten Zeilen gegenläufig; Mutante → rot an der aufsteigenden Zusicherung (im Commit `:91`) | ja |
| 4 | `frontend-reviewer` | Bild-Name der Sterne nur für 5 geprüft (ein fester Wert blieb grün) | Zusicherung „2 von 5 Sternen“; Mutante → rot an der neuen Zusicherung (im Commit `:110`) | ja |

Nicht gezählt: Der Test-Tor-Flake `AtomaresSchreiben_KeineLeseFehler` (außerhalb des Diffs) →
[B-274](backlog/B-274-atomares-schreiben-flackert-wieder.md). Zurückgestellt (🟢): Die Aufrufstelle von `canRate`
fährt keinen Grantee → [B-275](backlog/B-275-bewertungsformular-fuer-grantee-ungetestet.md). Info ohne Fix: Der
Leitschlüssel im absteigenden Zweig ist auf SQLite eine äquivalente Mutante, weil NULL bei `DESC` ohnehin hinten steht.

**Fund 1 ist ein Entgleiten aus dem siebten Lauf:** Das Protokoll von Sprint 12 nennt „917/917“, abgelesen an der
Zeile „Passed!“. Der Exit-Code war 1.

**Runde 3 — Delta-Review.** Kein Fund mit Code- oder Teständerung. Ohne Zählung: Der Spec-Kopf behauptete, „bewertet
zuerst, dann Id/Titel“ falle in beiden Richtungen. Belegt ist das nur für die aufsteigende Richtung, die absteigende
trägt der Backend-Sortiertest. Der Kommentar ist berichtigt. Benannte Restlücke, nicht behoben, weil konstruiert:
„Zeilen mit Kommentar zuerst“ bliebe im Listentest grün. **Zähler Lauf B am Ende: 4.**

### Runde — Rollengang / Abnahme

Die Extension war nicht verbunden, darum E2E. **Vater:** `uebungen-bewerten.spec.ts` (Schnitt und Anzahl sehen,
auf- und absteigend sortieren über drei Übungen, bewerten, ändern, nach dem Neuladen zurücklesen, löschen; die
Sterne als benanntes Bild). **Creator (Autorin):** in derselben Spec. Sie liest die Urteile, bekommt kein Formular.
**Sohn:** Regressionszeuge, volle Suite grün. Das Sprint-Ziel hält.

Verifikation: `dotnet build Pugling.sln` 0 Warnungen; `dotnet test Pugling.sln -c Release` **918/918, `exit=0`**
(abgelesen am Exit-Code, siehe Retro); Vitest **438/438**; Playwright voll nach dem letzten Code-Edit **94 passed**
(5,1 min). **B-12 und B-269 abgenommen.**

### Retrospektive

**Nachschau** (erste Pflichthandlung; frischer Prüfer, read-only über `git show`): **Sprint 11** (B-64, B-71,
`3094e34`) und **Sprint 10** (B-68, B-263, B-134, B-236; `9c7578c`, `8bc78d8`, `0ecb31b`). Je Story sind die
Prüfpunkte im Verlauf benannt, `nachgeschaut: 2026-10-26` ist an allen sechs gesetzt. **Zwei Entgleitungen:**
[B-276](backlog/B-276-vater-main-ueberlauf-regel-ungetragen.md) (`entgangen_bei: B-68`: Die Überlauf-Regel von
`.vater-main` trägt seit dem festen Popover kein Test mehr) und
[B-277](backlog/B-277-schnellanlage-legt-doppelten-store-eintrag-an.md) (`entgangen_bei: B-71`: Der Schnellanleger
legt für ein bekanntes Wort einen `_2`-Eintrag an, AK 5 gilt nur für neue Wörter). Dazu drei Doku-Befunde ohne
Story: Die M4-Zahl in B-64 rechnet sich nicht nach (2 statt 1), dazu verrutschte Zeilenangaben in B-263 und im
Protokoll von Lauf A (berichtigt) sowie ein veralteter `.vater-main`-Kommentar (in B-276). Index nach der Abnahme:
**Nachgeschaut 188 von 190**. Die zwei offenen sind B-12 und B-269, die eben abgenommen wurden.

**Was die eigenen Tore durchgelassen haben:**

1. **Ein roter Lauf, als grün protokolliert** (Fund 1). Der siebte Lauf las „917/917“ an der Zeile „Passed!“. Der
   Abdeckungs-Wächter urteilt im Assembly-Cleanup, und der Exit-Code war 1. **Prämisse geprüft:** Das Test-Tor
   (`.claude/hooks/test-gate.sh`) wertet den Exit-Code aus und meldet einen Cleanup-Abbruch. Es zeigt dann aber nur
   `TestResults/endpoint-coverage.txt` („270/270, 0 offen“), weil der Wächter nur die *fehlenden* Actions dorthin
   schreibt, nicht seine zweite Prüfung („mehr abgedeckt als die Konstante“). In diesem Lauf habe ich das rote Tor
   darum zuerst allein dem gleichzeitigen Flake zugeschrieben (B-274). Den wahren Grund fand erst der Reviewer.
2. **Abgrenzungen, die der Pflichtschritt nicht sah** (Funde 2–4): ein Testfall, in dem die jüngste Änderung
   zugleich der beste Wert war; eine E2E-Reihenfolge, in der Titel und Durchschnitt gleich liefen; ein Label, das
   nur an einem Wert geprüft war. Alle drei sind „zwei Eigenschaften fallen in den Testdaten zusammen“. Das ist die
   Klasse aus Lauf A, hier ohne Toleranz.

**Vorschläge (nur vorgeschlagen, Freigabe 3):**

- **(Tor)** `EndpointCoverageGuard` schreibt jedes eigene Urteil in `endpoint-coverage.txt`, auch „mehr abgedeckt
  als `FullRunTouchedActions`“. Dann nennt das Test-Tor den Grund statt „0 offen“. Rot machen ließe sich das mit der
  Probe 267 von heute.
- **(Prosa, ergänzt den Vorschlag aus Lauf A)** Im Pflichtschritt von pm-loop Step 5: *„Je Testfall prüfen, ob im
  Aufbau eine zweite Eigenschaft mit der behaupteten zusammenfällt (jüngste Änderung = bester Wert, Titelordnung =
  Wertordnung, nur ein geprüfter Wert); wenn ja, die Daten so wählen, dass sie auseinanderlaufen.“*
- **(Prosa)** Eine Backend-Suite gilt nur mit `exit=0` als grün. Die Zeile „Passed!“ ist kein Beleg. Das ist das
  Spiegelbild zu „E2E nie am Exit-Code messen“.

**Commit:** `ee71a74` (nicht gepusht).

## Lauf B, zweiter Sprint — *Lehrbuch-Formular* (B-267, B-268)

**Sprint-Ziel:** *Der Vater sieht nach dem Lösen des Lehrbuchs vom Katalog, was wirklich gespeichert ist, und eine
Tastaturnutzerin verliert beim Anlegen einer Reihe nicht ihren Platz.* Umfang:
[B-267](backlog/B-267-entkoppeltes-lehrbuch-zeigt-katalogwert-im-freitextfeld.md) (Weg a) und
[B-268](backlog/B-268-fokus-nach-inline-reihe.md). Beide Stories trugen die Freigabe-Zeile des Nutzers vom 2026-10-26
und wurden autonom ausformuliert und geschätzt (S, XS). **Zähler: 0.**

### Iteration

- **B-267:** `TextbookResponse` trägt zusätzlich `PublisherFreeText`/`GradeFreeText`, die Projektion liest die
  gespeicherten Spalten, und `textbookFormValues` liest den Freitext. Proben: Projektion mit dem abgeleiteten Wert →
  2 failed; Formular mit dem Katalogwert → 3 failed.
- **B-268:** Das Fokusziel nach Öffnen, Anlegen und Abbrechen läuft über einen Effekt. Proben: jeder der drei Sprünge
  einzeln weg → rot an `:333`/`:336`/`:344` (im Commit liegt der Reihen-Sprung an `:352`, Nachschau).

### Review

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` | Kein verknüpftes Buch ohne Freitext getestet, ein Rückfall auf den Katalog bliebe grün | Fall `bare`; Mutante → 1 failed | ja |
| 2 | `frontend-reviewer` | dasselbe im Frontend | Nullfall in `textbookPatch.test.ts`; Mutante → 1 failed | ja |
| 3 | `frontend-reviewer` | Zurücksetzen des Fokusziels ungetestet (kein echter Fehler, wie zuerst angenommen) | Ref statt Zustand, Schritt „öffnen – zu – öffnen“ | ja |
| 4 | `frontend-reviewer` | Fehlerweg des Anlegens ungetestet | Fall „Anlegen scheitert“ | ja |
| 5 | Delta-Review | Im Browser gibt der gesperrte Knopf den Fokus an `body` ab, jsdom nicht. Der Fall aus Fund 4 belegte nur jsdom | Fokus im Fehlerzweig ins Namensfeld; Spec `reihe-anlegen-scheitert-fokus.spec.ts`, **vorher `body`, nachher Namensfeld** | ja |

**Zähler am Ende: 5**, die Grenze ist erreicht, aber nicht überschritten. Darum habe ich bewusst kein weiteres
Review gestartet: Jeder weitere Fund hätte den gesamten Lauf beendet. Die letzte Behebung ist klein und auf beiden
Testebenen mit einer roten Probe belegt. Zurückgestellt: [B-278](backlog/B-278-platzhalter-sieht-aus-wie-gespeicherter-wert.md)
(Platzhalter „Cornelsen“ im leeren Feld).

### Rollengang / Abnahme

E2E, weil die Extension nicht verbunden war. **Vater:** `lehrbuch-katalog-zuerst.spec.ts` und die neue
Fehlerweg-Spec. **Sohn, Creator:** Regressionszeugen. Verifikation: Solution-Build 0 Warnungen, Backend **918/918
`exit=0`**, Vitest **443/443**, Playwright voll **95 passed**. **B-267 und B-268 abgenommen.**

### Retrospektive

**Nachschau** über den vorigen Sprint 12 (B-12, B-269; `ee71a74`), frischer Prüfer, read-only: **kein
Code-Entgleiten.** Die Prüfpunkte stehen in beiden Stories. Drei Doku-Befunde sind berichtigt: Die Titelreihenfolge
im Protokoll war veraltet, E2E-Zeilenangaben waren um zwei verrutscht (gemessen an einem Zwischenstand), und der
Kommentar im Backend-Sortiertest behauptete zu viel („nur der Durchschnitt stellt B vor C“, die Id-Ordnung tut es
aufsteigend auch, das fängt die E2E-Spec). `nachgeschaut` ist gesetzt. Index: **Nachgeschaut 190 von 192**, offen
sind B-267 und B-268 aus diesem Sprint.

**Was die eigenen Tore durchgelassen haben:** zwei Formen, beide neu in diesem Lauf.

1. **Der Nullfall.** Zweimal (Backend und Frontend) prüfte kein Fall „nichts gespeichert“, nur „etwas anderes
   gespeichert“. Ein `?? Rückfall` bliebe grün. Das ist dieselbe Familie wie „zwei Eigenschaften fallen zusammen“:
   Die Testdaten belegen das Feld immer.
2. **Eine Umgebung, die eine Browser-Regel nicht kennt** (Fund 5): Der Komponententest bestätigte einen Fokus, den es
   im Browser nicht gibt. Prämisse geprüft: im Browser gemessen, vorher `body`. `frontend/CLAUDE.md` legt Wege durch
   die App bereits zu Playwright. Fokus nach einem gesperrten Knopf ist aber ein Weg, der wie ein Baustein aussieht.

**Vorschläge (nur vorgeschlagen, Freigabe 3):**

- **(Prosa, `frontend/CLAUDE.md`)** *„Fokus-Zusicherungen, an denen ein `disabled` beteiligt ist, gehören nach
  Playwright: jsdom gibt den Fokus eines gesperrten Knopfs nicht ab, der Browser schon.“*
- **(Prosa, Pflichtschritt pm-loop Step 5)** Zur Liste der Abgrenzungen gehört der **Nullfall**: Behauptet ein Test
  „X statt Y“, braucht es auch den Fall „X leer“.

**Commit:** `4a25d47` (nicht gepusht).

## Lauf C, Teil 1 — Nacharbeit Sprint 7 (NP-2: B-15, B-17, B-21)

**Sprint-Ziel (unverändert aus dem dritten Lauf):** *Ich kann jede Übung vor dem Zuweisen sinnvoll ausprobieren –
auch einen Aufsatz –, eine Birkenbihl-Übung bekommt nur Sprachcodes, die der Vokabelspeicher kennt, und der Agent
zieht im Fördermodus nur Wörter der Sprache meiner Übung heran.* Sprint 7 bleibt nach der Morgen-Entscheidung stehen
(`93f227a`). Die sechs offenen Funde sind die Nacharbeit selbst. **Zähler: 0.** Die Nachschau zu Sprint 6 hatte ein
späterer Lauf schon erledigt (Index).

### Iteration — die sechs Funde

| Fund (2026-10-23) | Behebung | Probe |
| --- | --- | --- |
| B-17 (a) `contentProblem` prüft die Sprachen nicht | Birkenbihl: beide Codes müssen in `LANGUAGES` stehen | Prüfung weg → 1 failed |
| B-17 (b) Fall 2 prüft nur „Muttersprache“ | beide Auswahlen einzeln | Lernsprache schreibt `nativeLang` → 1 failed |
| B-15 (a)/(b) Texte nennen den Aufsatz | typneutral („Diese Übung …“) | UI-Text, kein Test (benannt) |
| B-15 (c) Test-Kommentare ohne „was macht ihn rot“ | ergänzt; `Preview_OhneAtome_…` als Wächter beschriftet | – |
| B-21 (a) Profil-Zweig ungetestet | neuer Fall (la-Profil, Anfrage ohne Sprachen) | Profil ignoriert → 1 failed |
| B-21 (b) `targetLang`-Filter ungetragen | drittes Wort en→fr im Rollup-Test | Filter weg → 1 failed (nach den Review-Behebungen: 2, Nachschau) |
| B-21 (c)/(d) Nits | `IsNullOrWhiteSpace`; Hilfe und README nennen „braucht `--child`“; `exam` als `InlineData` | Wächter weg → 3 failed |

Neu angelegt: [B-279](backlog/B-279-general-use-weak-zieht-woerter-trotzdem.md) (`--general --use-weak`, vorbestehend,
im dritten Lauf als „eigene Story wert“ benannt).

### Review

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` | Rollup-Test ohne Positivkontrolle für das en→fr-Wort | `Assert.Contains(englishToFrench, all)` | ja |
| 2 | `pugling-reviewer` | Kaskade „Anfrage > Profil“ und „Ziel aus dem Profil“ ungetragen (alle Profile en/de) | Profil la→fr, zweite Anfrage mit en/de; beide Mutanten → 1 failed | ja |
| 3 | `frontend-reviewer` | Reihenfolge „Sprache vor Satz“ behauptet, nicht getestet | vierter Fall; Reihenfolge getauscht → 1 failed | ja |
| 4 | Delta-Review | Ohne `sourceLang`-Filter blieb der Kaskaden-Test grün (die Geschwister-Tests fingen es) | eine Zusicherung; Probe siehe unten | ja |

Ohne Zählung: Kommentare berichtigt (Hinweis des Frontend-Reviewers: Birkenbihl hat im Bearbeiten-Dialog gar keinen
Inhalts-Editor, die Sprachprüfung ist heute ein Wächter, kein Schutz eines erreichbaren Wegs). Der Text in der
Guard-Zusammenfassung nennt `exam`. Benannt, nicht behoben: Die B-15-Texte trägt kein Test, und die Meldung zu
`no_checkable_content` wiederholt den zentralen Text.

**Beobachtung:** Fund B-17 (a) aus dem dritten Lauf beschrieb einen Weg, den es nicht gibt („eine bearbeitete
Alt-Übung …“). Gebaut ist er trotzdem, als Wächter. Das war richtig, aber die Begründung stand falsch im Code, bis der
Reviewer sie las.

### Rollengang / Abnahme

Die Extension war nicht verbunden. **Creator:** `uebungstypen.spec.ts` (jede Übungsart anlegen und im Testmodus
ausprobieren, der Aufsatz als Ansicht ohne Bewertung) und die Agent-Tests `CreatorAgentTests.Foerdermodus_*`
(Fördermodus nur mit dem Sprachpaar der Übung). **Vater, Sohn:** Regressionszeugen, volle Suiten grün. Verifikation:
Solution-Build 0 Warnungen, Backend **920/920 `exit=0`**, Vitest **444/444**, Playwright voll **95 passed**.
**Zähler am Ende: 4. B-15, B-17, B-21 abgenommen.**

### Retrospektive

**Nachschau** über den vorigen Sprint (*Lehrbuch-Formular*, `4a25d47`; frischer Prüfer, Mutanten an einem
`git archive` selbst gefahren): **ein Entgleiten** → [B-280](backlog/B-280-fokusziel-nicht-geleert-ungetestet.md)
(`entgangen_bei: B-268`). Die Mutante „Fokusziel nicht geleert“ ist grün, obwohl ich „rot an `:366`“ protokolliert
hatte. B-267 ist sauber. Ein Doku-Befund (`:344` statt `:352`) ist berichtigt. `nachgeschaut` ist an beiden
gesetzt. Index: **Nachgeschaut: 192 von 195**.

**Was die eigenen Tore durchgelassen haben:** Eine **rote Probe, die eine spätere Behebung entwertet hat.** Gemessen
wurde sie richtig, gegen den Fehlerweg-Fall, der damals `anlegen` erwartete. Fund 5 hat genau diesen Fall
umgestellt (jetzt Namensfeld, und dorthin setzt der Fehlerzweig ausdrücklich). Damit trug der Fall die Mutante nicht
mehr, und niemand hat die Probe nach der Umstellung wiederholt. Zum dritten Mal heute hängt ein Befund daran, dass
eine Messung an einem Zwischenstand gilt: dazu die verrutschten Zeilenangaben in Lauf A und B. Hier wurde aus der
verrutschten Adresse aber eine falsche Aussage über die Testabdeckung.

**Vorschlag (nur vorgeschlagen, Freigabe 3):** *„Ändert eine Behebung einen Test, gegen den schon eine rote Probe
protokolliert ist, wird diese Probe nach der Behebung wiederholt. Die Zeile im Verlauf nennt den Stand, an dem sie
gemessen ist.“* Prosa in pm-loop Step 5, neben dem Pflichtschritt. Ein Tor müsste Proben maschinenlesbar führen,
das tun sie heute nicht.

**Commit:** `14fa1f7` (nicht gepusht).

## Lauf C, Teil 2 — NP-3 Login und Konto härten (B-245, B-28, B-230)

**Sprint-Ziel (aus dem Paket):** *Der Vater kann kein Konto mit leerer oder zu kurzer PIN anlegen, und ein gelöschtes
Konto bekommt 401 statt 500.* Umfang: [B-245](backlog/B-245-token-mit-fid-auf-geloeschten-adult.md) (`Defekt`, erst
gemessen, dann autonom gegrillt), [B-28](backlog/B-28-login-name-sequenziell.md) (`geschaetzt`, Freigabe 8),
[B-230](backlog/B-230-pin-hash-doppelt-gehalten.md) (Freigabe-Zeile vom 2026-10-23, Migration). Reihenfolge wie im
Paket: messen → B-28 → B-230. **Zähler: 0.**

### Iteration

- **B-245:** gemessen **500** (Token eines gelöschten Erwachsenen, `POST creator/subjects`). Behoben zentral in
  `OnTokenValidated` (`Auth/TokenSubjectCheck`): Gibt es das Konto hinter `aid` nicht mehr, folgt 401. Eine eigene
  Prüfung von `fid`/`cid` habe ich wieder herausgenommen: Beide Löschpfade entfernen das Konto, die Prüfung wäre also
  unerreichbar und nicht testbar.
- **B-28:** Mindestlänge 4 an sechs Schreibpfaden (Regel einmal in `PinHasher`), leer = deaktiviert, und `Verify`
  lehnt eine leere gespeicherte PIN ab. Rote Proben: jede Guard-Clause einzeln → je 1 failed; Leer-Guard → 2 failed;
  Grenze 5 → 2 failed. Folgestory [B-281](backlog/B-281-pin-felder-pruefen-mindestlaenge-im-ui.md): Die Oberfläche
  zeigt die englische Meldung, weil `validation_error` keine deutsche Fassung hat und B-28 bewusst keinen eigenen
  Code wollte.
- **B-230:** `Adult.Pin`/`Child.Pin` gestrichen, die Kette neu gefaltet (der Snapshot-Diff zeigt genau die zwei Spalten),
  alle Logins prüfen `Account.PinHash`. **Auslegung, zur Kenntnis:** `MirrorAsync` spiegelt weiter Name und E-Mail,
  nur der PIN-Teil ist weg (Entscheidung 3 in der Story, der Reviewer hält sie für tragfähig). Der Fallstrick in der
  Root-`CLAUDE.md` lautet jetzt „PINs liegen gehasht und nur am Konto“.
- **Budget bezahlt:** `pm-loop/SKILL.md` lag durch den Pflichtschritt von heute Morgen 736 B über Budget. Der Absatz ist
  verdichtet, die Datei liegt wieder im Budget.

### Review

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` | AK 3 von B-230 trug nur 2 von 5 Seed-PINs | Theorien über alle fünf; Probe → 1 failed | ja |
| 2 | `pugling-reviewer` | „Ein PATCH ohne PIN behält sie“ ungetestet, ein überschreibendes `Ensure` bliebe grün | eigener Fall; Probe je Ensure-Pfad → 1 failed | ja |
| 3 | `pugling-reviewer` | Kind-Löschpfad von B-245 ungetestet | eigener Fall; Probe → 1 failed | ja |
| 4 | `pugling-reviewer` | leere PIN am Erwachsenen-PATCH ungeprüft | ergänzt; Probe → 1 failed | ja |
| 5 | Delta-Review | Ein `Ensure`, das eine *übergebene* PIN auf ein bestehendes Konto schreibt (jeder Neustart setzte die Seed-PINs zurück), bliebe grün | eigener Fall mit frischen Personen; Probe je Pfad → 1 failed | ja |

**Zähler am Ende: 5**, die Grenze ist erreicht. Danach habe ich kein weiteres Review gestartet. Zurückgestellt →
[B-282](backlog/B-282-login-haertung-restpunkte.md): Abbruch in `OnTokenValidated` loggt auf Error, Seed-PIN eines
Kindes nach Namen, Guard nach der Änderung, Zeitseitenkanal, „leer per PATCH“ nur als angenommen geprüft. Ohne
Zählung: zwei veraltete Kommentare. Ein Testlauf des Reviewers fiel am bekannten Flake
`AtomaresSchreiben_KeineLeseFehler` (B-274), einzeln wiederholt grün.

### Rollengang / Abnahme

Die Extension war nicht verbunden. **Vater und Sohn** melden sich in jeder E2E-Spec an, Playwright voll **95 passed**
(abgelesen an der Zusammenfassung; ein erster Lauf ohne diese Zeile zählte nicht und wurde wiederholt). **Creator**
(Lehrer-Konto) über `lehrer-konto.spec.ts`. Backend: Solution-Build 0 Warnungen, **936/936 `exit=0`**. Die lokale
`pugling.db` aus der alten Kette weist der Start jetzt ab, wie vorgesehen; ein Upgrade-Pfad existiert bewusst nicht.
**B-245, B-28, B-230 abgenommen.**

### Retrospektive

**Nachschau** über den vorigen Teil (Sprint-7-Nacharbeit, `14fa1f7`; frischer Prüfer an einem `git archive`,
Summen 920/444 nachgerechnet): **kein Code-Entgleiten.** Ein Doku-Befund: Die Probenzahlen zu den Sprachfiltern in
B-21 stammen von vor zwei Review-Behebungen und wurden nicht neu gemessen (3 statt „je 1“ bzw. 2 statt 1). Sie sind
im Verlauf berichtigt. `nachgeschaut` ist an B-15, B-17 und B-21 gesetzt. Index: **Nachgeschaut: 195 von 198**.

**Was die eigenen Tore durchgelassen haben:** Zum zweiten Mal heute (nach B-280) eine **Probenzahl, die eine spätere
Behebung überholt hat**. Hier war es harmlos, die Mutanten blieben rot, nur die Zahl stimmte nicht mehr. Dieselbe
Ursache wie in Teil 1, darum kein neuer Vorschlag: Der Vorschlag aus Teil 1 („nach einer Behebung, die einen Test
ändert, die protokollierten Proben wiederholen“) deckt beide Fälle. Seine Wirkung ist jetzt zweimal gemessen.

Zweiter Befund aus diesem Teil, **gemessen**: Ein Review-Durchgang über drei Stories brachte wieder genug für die
Grenze. Ich habe nach Gewicht ausgewählt und den Rest benannt zurückgestellt. Das ist dieselbe Abwägung, die der
Nutzer heute Morgen bei B-268/B-269 bestätigt hat. Sie ist jetzt die Regel des Tages, nicht die Ausnahme: Die
Zurückstell-Stories B-272, B-273, B-275, B-278 und B-282 kommen alle aus Läufen, die an oder kurz vor 5 standen.
**Beobachtung für den Nutzer, kein Vorschlag:** Ob Hinweise (🟢) überhaupt zählen sollen, entscheidet über die Hälfte
der heutigen Zurückstellungen.

**Commit:** folgt.
