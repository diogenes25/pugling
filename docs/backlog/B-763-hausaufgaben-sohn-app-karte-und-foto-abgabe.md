---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/belohnung, rolle/student, rolle/supervisor, bereich/frontend]
aliases: [Hausaufgabe Sohn-App]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "B-687"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-763 · Hausaufgaben in der Sohn-App: Karte mit Frist, Foto aufnehmen und abgeben, Nacharbeit lesen

Die Kind-Seite von [B-687](B-687-hausaufgabe-mit-foto-nachweis.md): Hausaufgaben sehen, mit einem Handyfoto abgeben, die Nacharbeits-Notiz lesen.
**Baut nach B-760 und B-761.**

## User Story

Als **Student** möchte ich meine Hausaufgaben in der App sehen und mit einem Handyfoto abhaken, damit ich dafür Münzen bekomme und keinen Malus kassiere.

## Ist-Stand am Code

- In der Sohn-Arcade gibt es kein Datei-Eingabefeld und keine Kamera-Auswahl (`capture="environment"`: **nicht gefunden**); die Startseite ist `frontend/src/sohn/SohnHome.tsx`.
- `capture` ist nur ein Hinweis an den Browser; manche Geräte öffnen die Galerie (B-687 Entscheidung 4).
- **Fremde, ungecommittete Arbeit am 2026-11-20:** `frontend/src/sohn/SohnApp.tsx` und mehrere Dateien unter `frontend/src/sohn/` (LearningDad) – die Route und die Karte
  hängen an `SohnApp.tsx`; erst bauen, wenn diese Arbeit committet ist.

## Die echte Lücke

Die gesamte Kind-Oberfläche; das Backend liefert die Wege.

## Entscheidungen

*Entscheidungen stammen aus [B-687](B-687-hausaufgabe-mit-foto-nachweis.md): stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt. Die Nummern der Entscheidungen verweisen dorthin.*

1. **Die Kamera wird angeboten, nicht erzwungen** (B-687 Entscheidung 4): `<input type="file" accept="image/*" capture="environment">`; kein EXIF.
2. **Die Hausaufgabe ist keine Pflicht des Tages:** sie erscheint als eigene Karte, nicht im Pfad, und ändert Serie und Kiste nicht (B-687 Entscheidung 1).
3. **Nacharbeit zeigt die Notiz an der Karte**, die Frist verschiebt sich nicht (B-687 Entscheidung 6).

## Akzeptanzkriterien

1. Die Startseite zeigt offene Hausaufgaben mit Fach, Text und Frist; ohne offene Hausaufgabe keine Karte. *Rot, wenn die Karte ohne Hausaufgabe erscheint (Vitest an `SohnHome`).*
2. „Abgeben“ lädt ein oder mehrere Fotos hoch und ist beim Senden gesperrt (zweimal tippen sendet einmal). *Rot, wenn `disabled={busy}` fehlt.*
3. Ein Upload, den der Server mit `400` ablehnt, zeigt die Meldung und lässt die Karte offen. *Rot, wenn der Fehler verschluckt wird.*
4. Eine Nacharbeits-Notiz steht an der Karte. *Rot, wenn die Notiz fehlt.*

## Schätzung

**Größe: M** – eine Karte, ein Upload-Feld, drei Zustände.

- **`wo: frontend`, `migration: nein`, `vertragsbruch: nein`.**
- **Risiken:** (1) Handy-Browser öffnen bei `capture` teils die Galerie, teils die Kamera – am echten Gerät prüfen (der Fall von B-31, nicht in der E2E); (2) große Fotos (bis 10 MB) auf dem
  Heimnetz; (3) die Karte teilt `SohnHome.tsx` mit anderer Arbeit.

### Angriffsplan

1. Client-Methoden, Typen, i18n Deutsch und Englisch.
2. Karte und Abgabe-Dialog in der Arcade-Gestaltung (`frontend/CLAUDE.md`), Datei-Feld, Fortschritt, Fehler am Platz.
3. Vitest, E2E, `frontend-reviewer`.

### Testweg

- `SohnHome.test.tsx`: AK 1; neuer Test `SohnHausaufgabe.test.tsx` für AK 2 bis 4.
- E2E `frontend/e2e/hausaufgabe.spec.ts` (gemeinsam mit B-762): als Kind ein Foto hochladen (Playwright `setInputFiles`), als Vater annehmen; das ist der Rollengang als Sohn.
- Das sinnliche Urteil „öffnet die Kamera“ bleibt beim Menschen (Handy), ausdrücklich benannt statt weggelassen.
- Vor der Abnahme: `frontend-reviewer`.

## Verlauf

- 2026-11-20 · aus B-687 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): M, frontend, keine Migration, kein Vertragsbruch. Baut nach B-760 und B-761; fremde Arbeit an `SohnApp.tsx` abwarten.
- **2026-11-24** — geprüft im Nachtlauf J, **angehalten**: Die Abgabe ist laut Entscheidung 1 ein Foto-Upload (B-761,
  `wartet_auf`). Eine Zwischenlösung „Abgeben ohne Foto“ (die Route aus B-760, `POST student/homework/{id}/submissions`) wäre
  eine Produktentscheidung, die die `## Entscheidungen` nicht hergeben: Sie ließe das Kind den Malus mit einem Tipp abwenden,
  B-761 AK 5 sieht genau das erst mit Foto vor. Die fremde Arbeit an `SohnApp.tsx` ist inzwischen committet (Arbeitsbaum an
  `5993c86b` sauber). Offene Entscheidung im Protokoll `pm-sitzung-2026-11-24-j.md`.
- **2026-11-26** — Hinweis aus B-1023: eine Hausaufgabe mit Fälligkeit in einer Pause kostet nichts, `HomeworkResponse.PenaltyCoins`
  trägt aber weiter den Betrag des Kindes (`penaltyCharged: false`). Die Karte darf für sie keine Malus-Drohung zeigen.
