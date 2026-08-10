---
tags: [typ/story, status/idee, bereich/katalog, rolle/supervisor]
aliases: [zurückgezogen in der Übungsauswahl, exercise_not_executable erst beim Speichern]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-11
unverifiziert: true
---

# B-140 · Die Übungsauswahl verschweigt, dass eine Übung zurückgezogen ist

Wer eine Position in einen Lehrplan aufnimmt, wählt aus der geteilten Bibliothek — Lesen ist bewusst
global, jeder Creator findet jede Übung. Zurückgezogene Übungen tragen in der **Verwaltung** dafür ein
sichtbares Kennzeichen (`pill mag` „zurückgezogen", `VaterExercises.tsx:321-324`); in der **Auswahl** beim
Zuweisen fehlt es. Der Betreuer wählt also eine Übung, die er nicht zuweisen darf, und erfährt es erst
beim Speichern als `exercise_not_executable`.

Die Daten liegen bereit: `ExerciseSummary.executePublic` steht im Vertrag
(`backend/Pugling.Contracts/Creator/ExerciseCatalogDtos.cs:15`) und wird in derselben Liste schon geladen.
Zu ergänzen wäre dasselbe Kennzeichen an zwei Stellen — `PlanPositions.tsx:411ff` und der Lehrplan-Assistent
(`VaterWizard.tsx:108`). **Alles unverifiziert**: die Zeilenangaben stammen aus dem `frontend-reviewer`-Lauf
zu [B-11](B-11-uebungen-veroeffentlichen.md) und sind vor dem Ausformulieren am Code nachzuschlagen.

**Warum nicht in B-11 mitgenommen:** B-11s Ziel ist ohne diese Änderung erfüllt — der Creator kann beim
Anlegen entscheiden, und das Kennzeichen erscheint dort, wo er sein eigenes Material verwaltet. Hier geht es
um eine **andere Rolle** (der zuweisende Supervisor) auf einer **anderen Fläche** (die Auswahl im Plan-Bau).

**Was B-11 daran ändert, und deshalb steht die Story überhaupt hier:** „zurückgezogen" war bisher ein
seltener Nachträglich-Zustand — jemand nahm etwas aus dem Verkehr. Seit B-11 ist es ein regulärer
**Anfangszustand**, den ein Creator mit einem Haken beim Anlegen wählt. Damit wird aus einem
Randfall ein Alltagsfall, und eine Lücke, die vorher niemanden traf, trifft jetzt jeden, der Material aus
der geteilten Bibliothek zusammenstellt.

## Verlauf

- **2026-08-10** — angelegt beim Bauen von [B-11](B-11-uebungen-veroeffentlichen.md): Fund des
  `frontend-reviewer` neben dem Diff. Ungeprüft aufgenommen (`unverifiziert: true`) — die Belege sind
  Reviewer-Angaben, nicht eigene Recherche.
