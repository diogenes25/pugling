---
tags: [typ/story, status/abgenommen, bereich/frontend, a11y]
aliases: [Gruppenkopf ohne fieldset/legend, Freigabe-Gruppe ohne Gruppennamen]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Skill `web-design-guidelines` zu B-176 (Nachtlauf 2026-10-23, Sprint 1)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-236 · Eine Gruppen-Überschrift über Kontrollkästchen ist sichtbar, aber für den Screenreader an nichts gebunden

B-176 hat die **Optik** der Gruppen-Überschriften repariert (`group-label`) und die Markup-Entscheidung aus
B-11 bewusst stehen lassen: „Freigabe (Startzustand)" ist ein `<span>`, weil es kein einzelnes Eingabefeld
beschriftet. Der zweite Blick (`web-design-guidelines`: „Use semantic HTML before ARIA", „Form controls need
label") zeigt die andere Hälfte: Die Überschrift ist an die Gruppe darunter **nicht gebunden**. Ein
Screenreader liest „Für andere Betreuer zuweisbar, Kontrollkästchen" – ohne zu sagen, dass es um die Freigabe
geht. Dasselbe gilt für „Schularten" (`VaterExerciseCreate.tsx:247`, ein `FieldLabel` ohne `htmlFor`, über
fünf Kästchen) und „Lern-Standards" (`:255`, ein `<label>` ohne Ziel).

Die semantische Form dafür ist `<fieldset><legend>` – dann trägt die Gruppe den Namen selbst, und die
Typografie-Regel aus B-176 müsste `legend` mit abdecken (oder `legend.group-label`).

## Offene Punkte

1. ~~Welche Gruppenköpfe sind tatsächlich Köpfe einer **Gruppe von Controls** (fieldset/legend) und welche nur
   Überschrift über einem Wiederhol-Feld (`RepeatedTextFields` – dort trägt jedes Feld schon ein `aria-label`
   mit Scope)? Einzeln ansehen, nicht pauschal umstellen – dieselbe Mahnung wie in B-176.~~ → Entscheidung 1.
2. ~~`fieldset` bringt Browser-Standardoptik (Rahmen, Padding) mit – die muss im `index.css` zurückgesetzt
   werden, sonst verschiebt sich die Karte.~~ → Entscheidung 2.

## Entscheidungen

Autonom gegrillt am 2026-10-25 (`Defekt`, Freigabe 1), gegen den Stand nach `3270a2f` gemessen.

1. **Umfang: sechs Gruppen, nicht elf Kandidaten.** Echte Gruppen von Kontrollkästchen unter einer Überschrift
   sind „Schularten", „Lern-Standards" und „Freigabe (Startzustand)" in `VaterExerciseCreate.tsx`, dieselben zwei
   ersten in `ExerciseEditModal.tsx` und „Rechenarten" im geteilten `ConfigEditor` (`exerciseConfig.tsx`). Die
   Überschriften über `RepeatedTextFields` (`exerciseConfig.tsx` „Lückenwörter"/„Auswahl", `ClozeTexts.tsx`,
   `VaterVocab.tsx` „Auch richtig") bleiben: dort trägt jedes Feld seinen Namen samt `scope` selbst, eine Gruppe
   sagte dasselbe zweimal. Kosten: keine – die Ansage dort ist heute schon vollständig.
2. **`role="group"` + `aria-labelledby` am vorhandenen `.field`-Container, nicht `<fieldset><legend>`.**
   Begründung: (a) AK 2 dieser Story-Familie (B-176: „die Karte verschiebt sich nicht") – `fieldset` bringt
   Rahmen, Innenabstand und `min-inline-size: min-content` mit; das Letzte kann eine Zeile mit `flexWrap` am
   Tablet breiter als die Karte machen, genau die Messung, die B-68 im selben Sprint eingeführt hat. (b) `legend`
   ist in einem Flex-Container kein Flex-Kind und nimmt den `gap` der Spalte nicht mit – die Überschrift säße
   anders als ihre Nachbarn. (c) Für Hilfsmittel ist eine benannte `role="group"` dieselbe Gruppe wie ein
   `fieldset` mit `legend` (beide `group` mit Namen im Barrierefreiheitsbaum), und das Repo nutzt die Form schon
   (`BirkenbihlDecoding.tsx`, die Vorschau-Gruppen in `uebungstypen.spec.ts`). Der Name zeigt nur auf das
   Stichwort („Lern-Standards"), nicht auf den Klammerzusatz – das wird vor jedem Kästchen angesagt; dafür
   sitzt die `id` an einem inneren `span` bzw. am `label` von `FieldLabel` (neue Eigenschaft `id`), nicht an
   der Zeile mit dem ⓘ-Knopf. Kosten: die Regel „semantisches HTML vor ARIA" wird hier bewusst nicht befolgt;
   das `fieldset`-Vorbild in `VaterFachlehrer.tsx:427` bleibt daneben stehen, das Repo kennt also beide Formen.

## Akzeptanzkriterien

1. Jede der sechs Gruppen aus Entscheidung 1 erscheint im Barrierefreiheitsbaum als `group` mit dem Namen ihrer
   Überschrift, und ihre Kontrollkästchen stehen in ihr.
2. Die sichtbare Darstellung ändert sich nicht (keine neue Optik, kein `fieldset`-Rahmen).

## Schätzung

**S**, `wo: frontend`, keine Migration, kein Vertragsbruch. Risiko: ein Name, der den Klammerzusatz oder den
ⓘ-Knopf mitliest (darum die `id` am Stichwort). **Angriffsplan:** (1) Spec schreiben, (2) rote Probe gegen
den Ausgangsstand, (3) `FieldLabel` um `id` erweitern, sechs Container umstellen, (4) grün, Vitest/`tsc -b`,
(5) `frontend-reviewer`. **Testweg:** E2E `frontend/e2e/gruppenkopf-benennt-die-gruppe.spec.ts` über
`getByRole("group", { name, exact: true })` im echten Chromium (Anlegen-Seite und Bearbeiten-Dialog).

## Verlauf

- **2026-10-23** — angelegt aus dem `web-design-guidelines`-Lauf zu B-176 (Freigabe 7). **Bewusst nicht in
  B-176 mitgenommen:** dessen Ziel (die Überschrift sieht aus wie eine) ist ohne diese Story erfüllt, und
  die Bindung ist eine Markup-Entscheidung, die B-11 ausdrücklich anders getroffen hat. `unverifiziert`:
  wie Screenreader die drei Gruppen heute ansagen, ist nicht gemessen.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (`VaterExerciseCreate.tsx:247,255,290`). Vorbild im Repo: `VaterFachlehrer.tsx:427-428` (`<fieldset>` mit zurückgesetzter Optik + `<legend>`). Weitere Kandidaten einzeln prüfen: `exerciseConfig.tsx:438,694,719`, `ClozeTexts.tsx:272`, `VaterVocab.tsx:224,600`. S, autonom.
- **2026-10-24** — fünfter Nachtlauf, Sprint 10 (NP-5, [Protokoll](../pm-sitzung-2026-10-24-nacht.md)): Kandidaten einzeln angesehen (offener Punkt 1). **Echte Gruppen ohne Gruppennamen:** `VaterExerciseCreate.tsx:247` (Schularten), `:255` (Lern-Standards), `:290` (Freigabe), dieselben zwei in `ExerciseEditModal.tsx:361,378`, dazu `exerciseConfig.tsx:547` (Rechenarten, `<label>` ohne Ziel). **Keine Gruppe im Sinn der Story:** `exerciseConfig.tsx:694,719`, `ClozeTexts.tsx:272`, `VaterVocab.tsx:224,600` – Überschrift über `RepeatedTextFields`, dort trägt jedes Feld seinen Namen samt `scope` selbst. Vorschlag für das Grillen (offener Punkt 2): `role="group"` + `aria-labelledby` am vorhandenen `.field`-Container statt `fieldset` – null Optik-Änderung, prüfbar per `getByRole("group", { name })`; das `fieldset`-Vorbild (`VaterFachlehrer.tsx:427`) bliebe daneben bestehen. **Unterbrochen (Rechner aus)**, nicht gebaut; Stufe unverändert.
- **2026-10-25** — fortgesetzt am 2026-10-25 (Wiederaufnahme des fünften Nachtlaufs, Sprint 10 / NP-5, Zähler weiter bei 5; [Protokoll](../pm-sitzung-2026-10-25-nacht.md)).
- **2026-10-25** — autonom gegrillt (Entscheidungen 1–2), geschätzt (S) und gebaut: `FieldLabel` mit `id`, sechs `.field`-Container mit `role="group"` + `aria-labelledby` (`VaterExerciseCreate.tsx`, `ExerciseEditModal.tsx`, `exerciseConfig.tsx`), neue Spec `frontend/e2e/gruppenkopf-benennt-die-gruppe.spec.ts` (zwei Tests, acht weiche Zusicherungen). Rote Probe (Auflage 5) – die vier Quelldateien aus `HEAD`, Spec unverändert: erwartet 8 rote Zusicherungen → gemessen **8** (`:22`, `:26`, `:27`, `:30`, `:35`, `:36` im Anlegen-Test, `:77`, `:79` im Dialog-Test, je „element(s) not found" bzw. `count` 0), **2 failed**; mit dem Fix **2 passed**. `tsc -b` sauber, Vitest **418/418**. `unverifiziert` auf `false`: der fehlende Gruppenname ist jetzt im Barrierefreiheitsbaum von Chromium gemessen; wie ein echter Screenreader ansagt, bleibt ein Blick des Menschen.
- **2026-10-25** — Lauf an Freigabe 3 beendet (Fund 6 im Delta-Review von B-263, nicht in dieser Story), bevor der `frontend-reviewer` für B-134/B-236 lief. Stand gebaut, grün, committet; Stufe bleibt `in-arbeit`.
- **2026-10-26** — **Entscheidung des Nutzers** (Morgen-Entscheidung 2026-10-26, Dialog, [Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)): Sprint 10 wird fortgesetzt, die Nacharbeit (inkl. des nicht gelaufenen `frontend-reviewer`) läuft mit **neuem Zähler (0)**. `wartet_auf` geleert.
- **2026-10-26** — `frontend-reviewer` (Lauf A, nachgeholte Runde 2, ([Protokoll](../pm-sitzung-2026-10-26-tag.md))): Die sechs Gruppen sind wie in Entscheidung 1 benannt, alle `aria-labelledby` lösen auf. Zwei Hinweise (🟢): Der Name „Freigabe (Startzustand)“ schließt die Klammer ein (gegen Entscheidung 2), und die Spec bliebe grün, wenn `role="group"` an die umgebende `<section>` rückte. Beide sind ausdrücklich zurückgestellt → [B-272](B-272-live-region-und-gruppen-tests-tragen-nicht-alles.md), weil jede Behebung als Fund zählt.
- **2026-10-26** — Rollengang (Lauf A, [Protokoll](../pm-sitzung-2026-10-26-tag.md)): Die Chrome-Extension war nicht verbunden, darum nach Freigabe 6 als E2E: `responsive-vater.spec.ts` (Vater am Tablet, 22 Routen, jedes ⓘ), `feldhilfe-lage.spec.ts` (6 Fälle), `gruppenkopf-benennt-die-gruppe.spec.ts`, Wächter `liveRegions.test.ts`. Sohn und Creator als Regressionszeugen: volle Suite grün, keine ihrer Specs rot. Verifikation: `tsc -b` sauber, Vitest **435/435**, Playwright voll nach dem letzten Code-Edit **93 passed**, danach `feldhilfe-lage` mit dem nur testseitigen Zusatz **6 passed**. Offen für einen Menschen: die echte Screenreader-Ansage (B-134/B-236) und das Popover am echten Tablet. **Abgenommen**; Commit: `0ecb31b`.
