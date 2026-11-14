---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Pflichtfeld-Sweep]
status: geschaetzt
prio: P3
art: Aufräumen
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#sprint-1
grund: ""
ersetzt_durch: []
---

# B-380 · Die Hausregel „Pflichtfeld leer → Speichern gesperrt" gilt erst auf der Lehrwerke-Seite

B-233 hat die Regel in `frontend/CLAUDE.md` gesetzt und für `SeriesForm`/`UnitForm` gebaut. Dasselbe Muster – Knopf
klickbar, lokale Meldung „… fehlt." – steht noch in fünfzehn Formularen (gezählt per `grep "action.fail("` am
2026-11-05, nur die Pflichtfeld-Fälle): `ChildMaterialSection.tsx:254` (neue Reihe) und `:292` (Titel),
`ClozeTexts.tsx:165/166`, `VaterFachlehrer.tsx:259`, `VaterKind.tsx:176`, `VaterDashboard.tsx:33`,
`VaterClassTests.tsx:69/70`, `VaterAnmerkungen.tsx:271`, `ChildCarePanels.tsx:36/121`, `ExerciseSharingPanels.tsx:36`,
`RemarkWidget.tsx:107`, `VaterProfil.tsx:111`, `VaterZiele.tsx:433`, `PlanPositions.tsx:490` (die letzten drei aus der Nachschau vom 2026-11-05 nachgetragen). Nicht mitgezählt sind Prüfungen, die keinen leeren Pflichtwert meinen (PIN-Länge, Klassenstufen-
Reihenfolge, Sterne).

Bewusst nicht in B-233 genommen: das Sprint-Ziel (eine Seite, ein Sperrmuster) war ohne diese Stellen erfüllt, und
mehrere davon werden per exaktem Label in E2E und Vitest angesprochen (`"Name der neuen Reihe"` allein zehnmal) – das
Nachziehen ist eigenes Handwerk.

## User Story

Als *Vater* möchte ich an einem leeren Pflichtfeld schon am gesperrten Speichern-Knopf und am „(Pflicht)" im Label sehen,
was fehlt – überall gleich wie auf der Lehrwerke-Seite –, statt erst nach dem Klick eine lokale Meldung „… fehlt." zu lesen.

## Ist-Stand am Code (nachgezählt 2026-11-13, Zeilen nachgemessen 2026-11-14)

- „(Pflicht)" im Label trägt heute nur `VaterLehrwerke.tsx` (`grep -l "(Pflicht)"`), als
  `<span className="muted">(Pflicht)</span>` im Label (`VaterLehrwerke.tsx:532`, `:841`, `:1005`) – der Zusatz ist damit
  Teil des zugänglichen Namens, und sieben E2E-Specs sprechen ihn so an (`getByLabel("Name der Reihe (Pflicht)", { exact: true })`,
  u. a. `e2e/bestaetigung-ueberlebt.spec.ts:130`, `e2e/lehrwerk-bearbeiten.spec.ts:53`). Die Sperre steht inline:
  `disabled={action.busy || !form.name.trim()}` (`:599`, `:924`, `:1049`).
- Die Form `fail("… fehlt")` steht noch an fünf Stellen: `ChildMaterialSection.tsx:264`, `:302`, `ClozeTexts.tsx:165`,
  `:166`, `VaterFachlehrer.tsx:267` (vorher `:259` – die Zeile ist gewandert).
- Die übrigen zehn Dateien der Liste oben rufen weiter `.fail(` auf (VaterKind 4, VaterProfil 3, PlanPositions 3,
  VaterDashboard 2, VaterClassTests 2, ChildCarePanels 2, je 1 in VaterAnmerkungen, ExerciseSharingPanels, RemarkWidget,
  VaterZiele) – mit anderem Wortlaut. **Welche davon Pflichtfeld-Fälle sind, ist je Zeile zu lesen**, nicht zu zählen.
- **Je Zeile gelesen am 2026-11-14** (Ergebnis in Entscheidung 1). Dabei drei Dateien außerhalb der Liste gefunden:
  `VaterRewards.tsx:79` und `:163` („Titel nötig.“), `VaterExerciseCreate.tsx:143-145` (drei `setError("Bitte …")` statt
  `fail`), `ExerciseEditModal.tsx:274` („Bitte einen Titel angeben.“, von `e2e/uebung-teilspeichern.spec.ts:104` geprüft).
- `PlanPositions.tsx:541` ist schon halb konform: der Knopf ist bei leerer Auswahl gesperrt (`:703`), die `fail`-Zeile ist
  nur noch toter Schutz; es fehlt „(Pflicht)“ am Label „Übung aus dem Katalog“.
- Meldungstexte, an denen Tests hängen: `e2e/anmerkungen.spec.ts:104` („Bitte etwas eintragen.“, RemarkWidget),
  `e2e/uebung-teilspeichern.spec.ts:104` (ExerciseEditModal). Vitest-Dateien gibt es für `ClozeTexts`, `VaterClassTests`,
  `VaterFachlehrer`, `VaterZiele`; keine für `ChildMaterialSection`, `VaterKind`, `VaterProfil`, `VaterDashboard`.

## Offene Punkte

1. ~~Je Zeile entscheiden: Pflichtfeld (→ Sperre + „(Pflicht)") oder andere Prüfung (bleibt).~~ → Entscheidung 1.
2. ~~Schnitt: ein Sprint je Seite oder ein gemeinsamer Baustein?~~ → Entscheidung 2 (Empfehlung PM übernommen) und
   Entscheidung 3 (Schnitt).
3. ~~Was wird aus der Prüfzeile im Submit-Handler?~~ → Entscheidung 4.
4. ~~Gehört das Anmerkungs-Widget dazu?~~ → Entscheidung 5.

## Entscheidungen

Stakeholder-Entscheidungen im Auftrag des Nutzers (Backlog-Lauf 2026-11-14).

1. **Klassifikation je Zeile.** *Stakeholder-Entscheidung im Auftrag des Nutzers.* Pflichtfeld heißt: die Prüfung fragt
   nur „leer?“ eines einzelnen Feldes. Alles, was einen Wert gegen eine Regel prüft, bleibt eine Meldung nach dem Klick.

   | Zeile | Prüfung | Urteil |
   | --- | --- | --- |
   | `ChildMaterialSection.tsx:264` | Name der neuen Reihe leer | **Pflicht** |
   | `ChildMaterialSection.tsx:302` | Titel des Buchs leer | **Pflicht** |
   | `ClozeTexts.tsx:165` | Titel leer | **Pflicht** |
   | `ClozeTexts.tsx:166` | Key leer, nur beim Anlegen | **Pflicht nur beim Anlegen** (Label-Zusatz nur dort) |
   | `ClozeTexts.tsx:167` | Lückentext-Problem | bleibt |
   | `VaterFachlehrer.tsx:267` | Name leer | **Pflicht** |
   | `VaterFachlehrer.tsx:271` | Klassenstufen-Reihenfolge | bleibt |
   | `VaterKind.tsx:205` | Name leer | **Pflicht** |
   | `VaterKind.tsx:231`, `:438`, `:442` | PIN-Regel, Schlagwort unbrauchbar/doppelt | bleibt |
   | `VaterProfil.tsx:112` | Name leer | **Pflicht** |
   | `VaterProfil.tsx:113`, `:116` | PINs ungleich, PIN-Regel | bleibt |
   | `VaterDashboard.tsx:33` | Vorname leer | **Pflicht** |
   | `VaterDashboard.tsx:35` | PIN-Regel | bleibt |
   | `VaterClassTests.tsx:70`, `:71` | Titel leer, Termin leer | **Pflicht** (beide) |
   | `VaterZiele.tsx:438` | Titel leer | **Pflicht** |
   | `VaterAnmerkungen.tsx:272` | Kommentar leer | Pflicht – zurückgestellt (Entscheidung 3) |
   | `ChildCarePanels.tsx:38`, `:125` | Betreuer-Id leer, Fach nicht gewählt | Pflicht – zurückgestellt |
   | `ExerciseSharingPanels.tsx:36` | Creator-Id leer | Pflicht – zurückgestellt |
   | `PlanPositions.tsx:541` | keine Übung gewählt | Pflicht – zurückgestellt (Sperre steht schon) |
   | `PlanPositions.tsx:543`, `:736` | Zeitfenster-Problem | bleibt |
   | `VaterRewards.tsx:79`, `:163` | Titel leer (neu gefunden) | Pflicht – zurückgestellt |
   | `VaterRewards.tsx:80` | Ziel nicht positiv | bleibt |
   | `VaterExerciseCreate.tsx:143-145` | Reihe/Unit/Titel leer (neu gefunden, `setError`) | Pflicht – zurückgestellt |
   | `ExerciseEditModal.tsx:274` | Titel leer (neu gefunden) | Pflicht – zurückgestellt |
   | `RemarkWidget.tsx:108` | Text leer | bleibt (Entscheidung 5) |

   Kosten: die Grenze „einzelnes Feld leer“ lässt eine leere *Auswahl* (Fach, Übung) als Pflicht gelten – richtig, aber
   bei Selects ohne Leer-Option entfällt der Fall.
2. **Ein kleiner Baustein für die Marke, die Sperre bleibt inline.** `frontend/src/components/Pflicht.tsx` rendert
   `<span className="muted">(Pflicht)</span>`; `VaterLehrwerke.tsx` wird als Erstes darauf umgestellt (zugänglicher Name
   unverändert, die sieben E2E-Specs bleiben grün). Die Sperre bleibt `disabled={action.busy || !wert.trim()}` am Knopf –
   ein Hook, der Sperre und Marke aus einer Quelle setzt, verdeckte die Bedingung, die der Leser am Knopf sehen muss.
   *Stakeholder-Entscheidung im Auftrag des Nutzers*, die Empfehlung des PM („Baustein zuerst“) übernommen und auf die
   Marke zugeschnitten. Begründung: eine Fassung des Wortlauts statt fünfzehn; der zugängliche Name trägt „(Pflicht)“ wie
   auf der Lehrwerke-Seite, damit Screenreader dasselbe hören, was das Auge sieht. Kosten: jede Spec und jeder Vitest, der
   ein umgestelltes Label exakt anspricht, zieht „ (Pflicht)“ nach – allein „Name der neuen Reihe“ elfmal.
3. **Schnitt: Kern sind acht Dateien, der Rest ist zurückgestellt.** Kern: Baustein + Lehrwerke, dann
   `ChildMaterialSection`, `ClozeTexts`, `VaterFachlehrer`, `VaterKind`, `VaterProfil`, `VaterDashboard`, `VaterClassTests`,
   `VaterZiele` (elf Zeilen). Zurückgestellt (keine neue Story-Datei, ein zweiter Durchgang derselben Story nach
   Abnahme des Kerns): `VaterAnmerkungen`, `ChildCarePanels`, `ExerciseSharingPanels`, `VaterRewards`,
   `VaterExerciseCreate`, `ExerciseEditModal` (E2E `uebung-teilspeichern` prüft dort die Meldung) und `PlanPositions`
   (dort bauen B-16, B-140, B-540 – Konflikt). *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: der Kern sind
   die Formulare der ursprünglichen Liste mit reinem Textfeld und ohne fremde Baustelle; er bleibt in einer Sitzung baubar
   (M). Kosten: bis zum zweiten Durchgang gilt die Hausregel in sieben Dateien weiter nicht.
4. **Die Prüfzeile im Submit-Handler wird stumm, nicht gelöscht:** `if (!wert.trim()) return;` ohne Meldung.
   *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: ein `onSubmit` erreicht der Vater auch per Enter im
   Feld; die Sperre des Knopfs hält die implizite Übermittlung zwar an, der Schutz kostet aber nichts und bewahrt den
   Server vor einem leeren 400. Kosten: keine.
5. **Das Anmerkungs-Widget (`RemarkWidget.tsx:108`) bleibt bei seiner Meldung.** *Stakeholder-Entscheidung im Auftrag des
   Nutzers.* Begründung: Dev-Werkzeug ohne Speichern-Formular, Hauptweg ist die Enter-Taste; ein gesperrter Knopf gäbe dort
   keine Rückmeldung – Enter auf leerem Feld bliebe stumm. Die E2E `anmerkungen.spec.ts:104` pinnt das. Kosten: eine
   bewusste Ausnahme von der Hausregel; sie wird in `frontend/CLAUDE.md` neben der Regel genannt.

## Akzeptanzkriterien

1. Für jede der elf Kern-Zeilen (Entscheidung 1, Urteil **Pflicht**): bei leerem Feld ist der Speichern-Knopf `disabled`,
   das Label trägt „(Pflicht)“, und nach einem Absendeversuch (Enter) erscheint **keine** Meldung „… fehlt“/„… nötig“/
   „… darf nicht leer sein“; mit Wert ist der Knopf bedienbar. Je Formular ein Vitest-Fall. **Rot**, wenn an dem Knopf das
   `|| !wert.trim()` fehlt (Knopf bleibt bedienbar) bzw. die `fail`-Zeile zurückkommt.
2. `ClozeTexts`: beim Bearbeiten eines bestehenden Textes trägt „Key“ kein „(Pflicht)“ und sperrt nicht. **Rot**, wenn die
   Bedingung `!existing` an Marke oder Sperre fehlt.
3. Die „bleibt“-Zeilen melden weiter nach dem Klick (Regression; je Datei mit Vitest ein Fall, etwa
   `VaterFachlehrer` Klassenstufen-Reihenfolge).
4. `VaterLehrwerke.tsx` benutzt `Pflicht` statt des eigenen `span`; die E2E-Specs mit `"Name der Reihe (Pflicht)"` bleiben
   grün.
5. `frontend/CLAUDE.md`: der Satz „(B-233; Rest: B-380)“ nennt den Baustein und die Ausnahme Anmerkungs-Widget; die
   zurückgestellten Dateien stehen in dieser Story, nicht als „offen:“-Vermerk dort.

## Schätzung

**Größe: M** – mechanisch, aber breit: neun Dateien, elf Zeilen, ein Baustein, vier neue Vitest-Dateien
(`ChildMaterialSection`, `VaterKind`, `VaterProfil`, `VaterDashboard`) und Label-Nachzug in E2E/Vitest. Vergleich:
mehr Fläche als B-233 (eine Seite), keine Logik.

- **`migration: nein`**, **`vertragsbruch: nein`** – reines Frontend.

**Risiken**

- Label-Nachzug: ein vergessener exakter Selektor bricht erst in der E2E. Gegenmittel: vor dem Umbau je Label die Treffer
  in `frontend/e2e` und `src/**/*.test.*` zählen und nach dem Umbau dieselbe Zahl mit „(Pflicht)“ finden.
- Neue Vitest-Dateien für vier Formulare ohne Test brauchen API-Mocks (Muster `PlanPositionsAdd.test.tsx`); falls eins
  davon zu viel Gerüst verlangt, deckt eine gezielte E2E-Zeile in einer bestehenden Spec es ab.
- **Konflikt**: `VaterClassTests.tsx` ist auch die zurückgestellte Klassenarbeit-Auswahl von B-140 – darum dort jetzt nur
  die Sperre. Keine Überschneidung mit B-16/B-140/B-540 in `PlanPositions.tsx` (zurückgestellt).

**Angriffsplan (kein Backend-Schritt)**

1. `components/Pflicht.tsx`, `VaterLehrwerke.tsx` darauf umstellen (AK 4).
2. Je Kern-Datei: Marke ins Label, Sperre an den Knopf, `fail`-Zeile stumm (Entscheidung 4), Vitest (AK 1–3).
3. Selektoren in E2E/Vitest nachziehen; `frontend/CLAUDE.md` (AK 5).

**Testweg**

- Vitest: vorhandene `ClozeTexts.test.tsx`, `VaterClassTests.test.tsx`, `VaterFachlehrer.test.tsx`, `VaterZiele.test.ts`
  erweitern; neu `ChildMaterialSection.test.tsx`, `VaterKind.test.tsx`, `VaterProfil.test.tsx`, `VaterDashboard.test.tsx`.
  Je Fall die rote Probe am richtigen Ort (Bedingung am Knopf entfernen).
- Playwright: alle Specs, die umgestellte Labels ansprechen, müssen grün laufen („N passed“ zählen); Rollengang: Kind
  anlegen und Buch mit neuer Reihe anlegen an der laufenden App.

## Verlauf

- **2026-11-05** — angelegt beim Bau von B-233 (Sprint 1 der Sitzung 2026-11-05).
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): Bestand nachgezählt (5 Stellen „fehlt", zehn Dateien mit anderem Wortlaut je Zeile zu prüfen), User Story, zwei offene Punkte.
- **2026-11-14** — Abgleich Backlog-Lauf: veraltet, Kern gilt. „(Pflicht)“ trägt weiter nur `VaterLehrwerke.tsx`; „fehlt“-Stellen nachgemessen: `ChildMaterialSection.tsx:264/302` (vorher 254/292), `ClozeTexts.tsx:165/166`, `VaterFachlehrer.tsx:267`; die `.fail(`-Zählung der zehn übrigen Dateien ist unverändert.
- **2026-11-14** — Refinement Backlog-Lauf: jede `.fail(`-Zeile gelesen und klassifiziert (Tabelle in Entscheidung 1), drei weitere Dateien gefunden (`VaterRewards`, `VaterExerciseCreate`, `ExerciseEditModal`); gegrillt mit fünf Stakeholder-Entscheidungen im Auftrag des Nutzers (Klassifikation, Baustein `Pflicht` mit Sperre inline, Kern acht Dateien + Lehrwerke und Rest zurückgestellt, stumme Prüfzeile, Anmerkungs-Widget bleibt); geschätzt: M, nur Frontend, `unverifiziert` entfernt.
