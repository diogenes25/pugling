---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, bereich/frontend, rolle/supervisor]
aliases: []
status: abgenommen
prio: P1
art: Wunsch
groesse: S
wo: "frontend"
migration: "nein"
vertragsbruch: "nein"
quelle: "Rollengang Runde 138 an einer Kopie der echten DB (docs/pm-sitzung-2026-11-24.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-949 · Der Vater sieht in der Klassenarbeiten-Liste „x von y Wörtern sicher“

Teil von B-748 (Klausurchance), der ohne Prognose auskommt. Das Kind sieht auf seiner Karte, wie viele Wörter der Arbeit es sicher
kann (B-751, B-945). Der Vater sieht das nirgends. Beobachtet an einer Kopie der echten DB: Friedrichs Englisch-Arbeit in zehn Tagen
hat 21 von 209 Wörtern sicher.

## User Story

Als Vater möchte ich in der Klassenarbeiten-Liste je geplanter Arbeit sehen, wie viele Wörter das Kind schon sicher kann, damit ich
früh eingreife statt nach der Note.

## Ist-Stand am Code (2026-11-24, `f4324888`)

- `GET student/children/{childId}/class-tests/{id}/readiness` (`backend/Pugling.Api/Controllers/Student/ChildClassTestsController.cs`)
  trägt `ChildOwnershipFilter`: Ein betreuender Supervisor darf lesen.
- Die Kind-Karte liest sie (`frontend/src/sohn/KlassenarbeitKarte.tsx`); in `frontend/src/vater/` liest niemand den Endpunkt.

## Die echte Lücke

Der Stand liegt im Vertrag und ist dem Vater erlaubt, aber keine Vater-Oberfläche zeigt ihn.

## Entscheidungen

1. **Nutzer, 2026-11-24:** in den Sprint, in der Liste unter „Klassenarbeiten“.
2. Dieselbe Quelle wie beim Kind (Endpunkt `readiness`), kein zweites Rechenmodell. Kosten: ein Aufruf je geplanter Arbeit.
3. Nur für **geplante** Arbeiten mit Vokabeln (`totalItems` nicht leer); sonst steht nichts. Formulierung wie beim Kind: „x von y
   Wörtern sicher“.

## Akzeptanzkriterien

1. Eine geplante Arbeit mit Vokabeln zeigt „x von y Wörtern sicher“ mit den Zahlen des Servers.
2. Eine Arbeit ohne Vokabeln (`totalItems` leer) zeigt keine Zeile.
3. Eine geschriebene oder abgesagte Arbeit fragt keine Bereitschaft ab und zeigt keine.
4. Ein Fehler des Aufrufs lässt die Zeile weg und lässt die Liste stehen.

## Schätzung

- **Größe:** S, frontend, keine Migration, kein Vertragsbruch.
- **Testweg:** Vitest an der Zelle (AK 1–4, je eine Mutante), E2E `klassenarbeit-countdown` um eine Vater-Zusicherung erweitert.

## Angriffsplan

In `VaterClassTests.tsx` eine Zelle mit eigenem `useAsync`, nur für `status === "Planned"`, über den Client-Aufruf, den die Kind-Karte
nutzt.

## Verlauf

- **2026-11-24** — angelegt im Rollengang Runde 138, vom Nutzer in den Sprint genommen, `geschaetzt` für Sprint 138.
- AK 1 → `VaterClassTests.test.tsx` „zeigt die Zahlen des Servers…“, U949-1 (Kind/Arbeit vertauscht) → rot.
- AK 2 → „zeigt nichts ohne Vokabeln“, U949-2 → rot. AK 3 → „fragt bei geschriebenen…“, U949-3 → rot.
- AK 4 → „lässt die Zeile nach einem Fehler weg“ (rot, wenn ein Fehler Text in die Zeile bringt).
- `frontend-reviewer`-Fund eingearbeitet: Die Zeile lud nach dem Zuweisen von Übungen nicht neu. Das Bündel steht jetzt in den
  Abhängigkeiten, Test „lädt neu, wenn sich das Bündel ändert“, U949-5 → rot.
- Rollengang: `klassenarbeit-countdown` prüft dieselbe Zahl beim Kind und in der Vater-Liste („0 von 2 Wörtern sicher“).
- **2026-11-24** — abgenommen in Sprint 138, Commit `08933431`. Volle Suite 2323/2323 grün (Release), Vitest 1439/1439, `npm run build` grün. Mutanten-Probe nach dem letzten Edit (`mutant-probe.py`, Tabelle im Protokoll Runde 138). Rollengang: Playwright auf Ausweichports 5201/5174, Server frisch gestartet: `klassenarbeit-countdown`, `klassenarbeit-vorbereitung`, `klassenarbeit-stoff-dicht`, `rueckmeldung`, `pfad` – 5 passed.
