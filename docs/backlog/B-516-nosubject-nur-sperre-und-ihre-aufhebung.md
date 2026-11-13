---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [NoSubject vereinfacht, Ausweg nur die Aufhebung]
status: abgenommen
prio: P2
art: Defekt
groesse: "S"
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 55 in der Retro von Sprint 56/57)
unverifiziert: false
entgangen_bei: [B-514]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-516 · Die Meldung „Reihe ohne Fach" nennt nur die Sperre und ihre Aufhebung

Nachschau von Sprint 55 (frischer `pugling-reviewer` an `637d868`, gelesen). Drei 🟡, ein 🟢 – zum fünften Mal dieselbe
Meldung (`ProfileFit.NoSubject`):

1. 🟡 Ein Profil, dessen Fach nicht zu seiner eigenen Reihe passt (der Server erlaubt das), läuft mit zwei der
   B-514-Ratschläge („lass --series weg", „ein Profil, das an eine Reihe mit Fach gebunden ist") in `ProfileFit.Problem`.
2. 🟡 Der Teil „setze ein Fach im Vater-Web / bitte den Eigentümer" hilft im Zweig „an eine andere Reihe gebunden" nie
   und in zwei Zweigen nur mit genau dem Fach des Profils.
3. 🟡 Zwei Zweige werden ausgeführt, drei nur gelesen.
4. 🟢 `--unit` bleibt nach „lass --series weg" stehen.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf, Nutzerrolle übertragen):

1. **Die Meldung nennt die Sperre und nur den Schritt, der sie aufhebt** (Fach setzen: eigene Reihe → Vater-Web;
   fremde → nur der Eigentümer; Grundbestand → niemand). Keine Ratschläge zu anderen Reihen oder Profilen mehr: jede
   Fassung seit B-510 fand eine Kombination aus Profil und Reihe, in der der Rat an der nächsten Sperre endete. Erreicht
   der Nutzer die Profil-Sperre des Briefings, nennt die ihren eigenen Ausweg. Kosten: die Meldung führt weniger weit –
   dafür führt sie nie in die Irre.
2. Die Regel landet in `pm-loop` Step 5 (die Aufhebung der Sperre ist immer erlaubt, jeder andere Ausweg braucht einen
   Test, der ihn ausführt).

## Akzeptanzkriterien

1. Die Meldung enthält kein `--series`/`--profile` mehr, in keinem Zweig.
2. Eigene Reihe: Fach (das des Profils) gesetzt → derselbe Aufruf schreibt.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 55, Retro Sprint 56/57), autonom gegrillt, geschätzt S; in Sprint 58 gebaut.
- **2026-11-13** — gebaut (Sprint 58). `NoSubject(series)` ohne Profil-Parameter; drei Bestandstests auf die neue Fassung,
  ein neuer Test führt den einen Ausweg aus (`Eigene_Reihe_ohne_Fach_Fach_setzen_genuegt`, nach Review mit Prüfung, dass
  genau diese Sperre stoppte). Review: kein 🟡 zu den Meldungen; Zahl „vier/fünf Sprints" angeglichen.
  - AK 1 → `Gebundenes_Profil_an_Reihe_ohne_Fach_nennt…`, `Fremd_gebundenes_Profil…`, `Fremde_Reihe_ohne_Fach…`; N1, N2
    (Rat wieder angehängt) → rot. AK 2 → `Eigene_Reihe_ohne_Fach_Fach_setzen_genuegt`.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1441/1441; Mutanten G1–G4, N1, N2 6/6 rot nach der letzten Änderung
    (Release, gefiltert).
  - Rollengang Creator: Konsolenaufrufe gegen den echten In-Process-Server (Modell gefälscht); Vater, Sohn
    Regressionszeugen. Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 59/60, frischer Reviewer, gelesen; Filter-Wege, Render-Phasen-Zustand bzw. NEU-Erkennung gegen die echten Unit-Notizen nachgerechnet). Befund: 🟡 Herkunft der Reihe fehlt in der Meldung; 🟢 AK 1 nur in einem Zweig rot → [B-519](B-519-nachschau-reste-sprint-58.md).
