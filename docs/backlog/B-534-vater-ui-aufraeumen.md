---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Vater-UI aufräumen, Reiter, AddPanel, ein Speichern]
status: abgenommen
prio: P1
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Nutzer im Chat 2026-11-13: Vater-Seite total überladen, mehrere Speichern-Knöpfe je Seite für unterschiedliche Aspekte"
grund: ""
ersetzt_durch: []
wartet_auf: ""
nachgeschaut: 2026-11-14
---

# B-534 · Das Vater-Web zeigt je Fläche höchstens ein Speichern

Der Vater verlor auf seinen Seiten den Überblick: Der Kind-Hub stellte neun Abschnitte untereinander, mit drei
verschiedenen „Speichern" (Stammdaten, Bonus-Tagesdeckel, Interessen) und vier weiteren Absende-Knöpfen
(Buch hinterlegen, Betreuer hinzufügen, Eintragen, Kind löschen direkt neben Speichern). Der Übungsdialog hatte
zwei Speichern-Knöpfe für dieselbe Übung („Inhalt speichern", „Beschreibung speichern"), deren Wechselspiel
schon zwei Defekte getragen hat (B-206, B-210). Auf der Plan-Seite stand das große Anlege-Formular dauerhaft
vor der Positions-Tabelle.

## User Story

Als **Vater** möchte ich auf jeder Seite sofort sehen, was ein Knopf speichert, damit ich keine Eingabe verliere
und nicht rätseln muss, welcher von drei Knöpfen meine Änderung sichert.

## Entscheidungen

1. **Kind-Hub in Reiter** (`?bereich=profil|schule|interessen|einstellungen|betreuer`, `SectionTabs`). Links statt
   Knöpfe: der Reiter steht in der URL, Lesezeichen und „Interessen pflegen →" öffnen ihn direkt. Je Reiter höchstens
   ein Speichern. Die Sprunglinks zu Lernstand/Zielen/… sind leise Textlinks statt einer zweiten Knopfreihe.
2. **Hinzufügen-Formulare zugeklappt** (`AddPanel`): Buch, Betreuer, Stundenplan, Plan-Position. Im Leerfall offen
   – dort ist das Formular der einzige nächste Schritt.
3. **Übungsdialog: ein Knopf „Übung speichern"** für Inhalt und Metadaten in einem PUT. Damit entfällt der
   gemeinsame Riegel zweier `useAction`-Instanzen (B-210) – die Fehlerklasse „ein Knopf dreht den anderen zurück"
   gibt es nicht mehr. Das Nachziehen des geladenen Stands bleibt (der Server normalisiert die Config).
4. **„Kind löschen" in eine Gefahrenzone** unter dem Formular, nicht mehr neben „Speichern".
5. **Bewusst nicht angefasst:** `/vater/lehrwerke` (Formulare liegen schon hinter „Units"/„Bearbeiten"; das
   Anlege-Formular steht am Seitenende) und die Plan-Kopfzeile (Bearbeiten ist schon ein Umschalter).

## Akzeptanzkriterien

1. Auf `/vater/kind/:id` ist je Reiter höchstens ein „Speichern" sichtbar; `?bereich=` öffnet den Reiter direkt.
2. Hinzufügen-Formulare neben einem Bestand sind zu, bis man sie öffnet; ohne Bestand offen (`AddPanel.test.tsx`).
3. Der Übungsdialog hat genau einen Speichern-Knopf, und eine Speicherung trägt Inhalt **und** Titel
   (`e2e/uebung-teilspeichern.spec.ts`).

## Verlauf

- **2026-11-13** — angelegt und umgesetzt (Quelle: Nutzer im Chat).
- **2026-11-14** — Im Code-Review des Backlog-Laufs 2026-11-13 (Branch `auto/backlog-lauf-2026-11-13`, nach dem Merge von `auto/fertigstellung`) drei Funde an dieser Arbeit, dort behoben: (1) `ExerciseEditModal` – Enter speicherte nicht (kein Submit-Knopf mehr im Formular) und `save()` prüfte `veraltet` nicht (Vollersatz aus altem Stand möglich, B-210) → Knopf per `form=` gebunden, `veraltet`-Prüfung; (2) `VaterKind` – ein Reiterwechsel verwarf ungesicherte Eingaben → Reiter bleiben eingehängt (`SectionPanel`, `hidden`), die Fachlehrer-Suche lädt nach dem Speichern von Klasse/Schulart neu; (3) `PlanPositions` – die Meldung zum Hinzufügen stand weit über dem `AddPanel` → eigenes Banner am Panel, Ladefehler als `ListenAusfall`. Dazu `AddPanel` `aria-controls` nur offen, `SectionTabs`-Kommentar zu `replace` berichtigt.
- **2026-11-14** — **abgenommen** (Sprint 74; gebaut und im Gesamt-Review des Laufs 2026-11-13 nachgebessert,
  `a3e5c1ba` + `40dd9bfa`). Belege nach dem letzten Edit: volle Suite 1621/1621 (Release), Vitest 1023/1023 (drei volle Läufe in Folge), `tsc -b` grün; Rollengang Vater als Playwright gegen frisch gestartete Server nach dem letzten Edit: 19 passed (`auswahl-nicht-zuweisbar` 3 Fälle, `lernkontrolle-sichtbar`, `planbau-bewertung`, `vater-von-null`, `assistent`, `uebung-teilspeichern`, `interessen`, `bonus-deckel`, `kind-lehrbuch-fach`, `responsive-vater`, `full-flow`); Reviews `pugling-reviewer` + `frontend-reviewer`, Funde im Sprint behoben; Commit `defa3ea0`. AK 1 (Reiter, `?bereich=`) tragen `interessen`,
  `bonus-deckel`, `kind-lehrbuch-fach`; AK 2 `AddPanel.test.tsx`; AK 3 `uebung-teilspeichern`. Keine eigene
  Mutantenprobe in diesem Lauf (die Arbeit stammt aus dem Vorlauf, dort reviewt).
- **2026-11-14** — Nachschau Backlog-Lauf: ohne Fund. Gegen `HEAD` gelesen: `VaterKind.tsx` zeigt je Reiter höchstens ein Speichern (`:288`, `:388`, Löschen abgesetzt `:292`), `ExerciseEditModal.tsx` und `PlanPositions.tsx` tragen keinen zweiten Speichern-Knopf; der Rest der Arbeit war im Vorlauf zweimal reviewt.
