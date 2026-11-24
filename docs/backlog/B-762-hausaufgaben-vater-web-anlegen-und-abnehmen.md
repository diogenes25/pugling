---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/belohnung, rolle/student, rolle/supervisor, bereich/frontend]
aliases: [Hausaufgabe Vater-Web]
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

# B-762 · Hausaufgaben im Vater-Web: anlegen, Abgaben mit Foto ansehen, annehmen oder zur Nacharbeit zurückgeben

Die Vater-Seite von [B-687](B-687-hausaufgabe-mit-foto-nachweis.md): Hausaufgabe von Hand anlegen, offene Abgaben mit den Fotos in groß sehen, annehmen
oder mit einer Zeile Begründung zurückgeben. **Baut nach B-760 und B-761.**

## User Story

Als **Supervisor** möchte ich Hausaufgaben meines Kindes anlegen und die abgegebenen Fotos prüfen, damit ich weiß, ob die Hausaufgabe wirklich gemacht ist.

## Ist-Stand am Code

- Ein Datei-Eingabefeld gibt es im Vater-Web (`frontend/src/vater/VaterMedia.tsx:245`, `VocabMediaPanel.tsx:164`); eine Hausaufgaben-Seite, ein Anlegeformular und eine
  Abnahme-Liste: **nicht gefunden**.
- Vergleichbare Vater-Seite mit Liste, Status setzen und Hinweis an das Kind: `frontend/src/vater/VaterRueckmeldungen.tsx` samt E2E `frontend/e2e/rueckmeldung.spec.ts`.
- Jede Mutation geht über `useAction` und `StatusBanner` (`frontend/CLAUDE.md`).

## Die echte Lücke

Die gesamte Oberfläche; das Backend (B-760, B-761) liefert die Wege.

## Entscheidungen

*Entscheidungen stammen aus [B-687](B-687-hausaufgabe-mit-foto-nachweis.md): stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt. Die Nummern der Entscheidungen verweisen dorthin.*

1. **Anlegen von Hand** (B-687 Entscheidung 7); WebUntis ist eine spätere Quelle (B-654, B-655).
2. **Abnahme:** *Annehmen* und *Nacharbeit* mit Notiz; keine Teilpunkte (B-687 Entscheidung 6).
3. **Anti-Cheat ist der Blick des Vaters:** die Seite zeigt die Upload-Zeit neben dem Foto (B-687 Entscheidung 4).

## Akzeptanzkriterien

1. Der Vater legt eine Hausaufgabe mit Fach, Text und Frist an, sie erscheint in der Liste. *Rot ohne Seite, Route oder Navigationseintrag.*
2. Die Liste zeigt je Abgabe die Fotos und die Upload-Zeit; ein Foto lädt nur nach Anmeldung. *Rot, wenn das Bild über eine öffentliche URL geladen wird.*
3. *Annehmen* bucht einmal Münzen (zweimal geklickt: einmal); *Nacharbeit* verlangt eine Notiz. *Rot, wenn `disabled={busy}` fehlt oder die Notiz leer durchgeht.*
4. Eine Seite, die eine Hausaufgabe eines anderen Kindes zeigt, gibt es nicht (Kinderwahl wie bei den anderen Vater-Listen). *Rot, wenn die Liste ohne Kindfilter lädt.*

## Schätzung

**Größe: M** – wie die Vater-Seite „Rückmeldungen“ (B-628/B-629): eine Listenseite, ein Formular, zwei Knöpfe, ein Bildbetrachter.

- **`wo: frontend`, `migration: nein`, `vertragsbruch: nein`** (alle Wege kommen aus B-760 und B-761).
- **Risiken:** die Foto-Auslieferung hinter der Anmeldung (siehe B-761, Risiko 2) bestimmt, ob die Seite ein Bild direkt laden kann; fremde uncommittete Arbeit in
  `frontend/src/` (LearningDad) berührt `SohnApp.tsx`, nicht die Vater-Seiten.

### Angriffsplan

1. Client-Methoden und Typen aus dem erzeugten Vertrag; i18n Deutsch und Englisch.
2. Seite `VaterHausaufgaben.tsx`, Route und Navigation; Anlegeformular, Liste, Bildansicht, zwei Knöpfe (`useAction`, `StatusBanner`).
3. Vitest, E2E, `frontend-reviewer`.

### Testweg

- `VaterHausaufgaben.test.tsx` (neu, Vorbild der Tests unter `frontend/src/vater/`): AK 1, 3, 4.
- E2E `frontend/e2e/hausaufgabe.spec.ts` (neu, Vorbild `rueckmeldung.spec.ts`): anlegen, als Kind abgeben (über die API), annehmen; das ist der Rollengang als Vater.
- Vor der Abnahme: `frontend-reviewer`.

## Verlauf

- 2026-11-20 · aus B-687 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): M, frontend, keine Migration, kein Vertragsbruch. Baut nach B-760 und B-761.
- **2026-11-24** — geprüft im Nachtlauf J, **angehalten**: AK 2 (Fotos ansehen) hängt an B-761 (`wartet_auf`). Allein gebaut
  wäre die Seite schädlich: der Vater könnte Hausaufgaben anlegen, die das Kind in der App nicht abgeben kann (B-763 fehlt) –
  jede würde zur Frist den Malus kosten. Die Backend-Wege aus B-760 stehen bereit (`supervisor/children/{childId}/homework`,
  `…/accept`, `…/rework`, `DELETE`, `take`); die Beträge je Kind gehören als zwei Felder auf die Kind-Einstellungen
  (Muster `towerGameMinutesPerDay` in `VaterKind.tsx`). Offene Entscheidung im Protokoll `pm-sitzung-2026-11-24-j.md`.
