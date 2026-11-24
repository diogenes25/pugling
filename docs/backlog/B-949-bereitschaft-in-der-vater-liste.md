---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, bereich/frontend, rolle/supervisor]
aliases: []
status: geschaetzt
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
