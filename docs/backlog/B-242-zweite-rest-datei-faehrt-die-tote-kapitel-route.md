---
tags: [typ/story, status/ausformuliert, bereich/doku, rolle/creator]
aliases: [VocabularyExecises.http, eingebranntes Token in docs/REST/Creator/]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: doku
migration: nein
vertragsbruch: nein
quelle: Routen-Sweep beim Bauen von B-238 (zweiter Nachtlauf 2026-10-23, Sprint 4)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-242 · Eine zweite `.http`-Datei unter `docs/REST/Creator/` fährt die tote Kapitel-Route – mit eingebranntem Token

B-238 hat `docs/REST/Creator.http` auf die Unit-Route umgebaut und dabei den Routen-Sweep aus B-166 AK 1
über `docs/REST/` gefahren. Er fand eine Datei, die weder das REST-README noch B-238 kannte.

## User Story

Als **Creator mit dem REST-Client** möchte ich, dass jede `.http`-Datei im Repo entweder läuft oder nicht da
ist, damit ich nicht an einer Nebendatei scheitere, die aussieht wie das Tutorial.

## Ist-Stand am Code

`docs/REST/Creator/VocabularyExecises.http` (Tippfehler im Namen, 3.390 Byte, zuletzt berührt in `42cd318`,
2026-07-29 – also **vor** B-106):

| Zeile | Befund |
| --- | --- |
| `:1-3` | `@adultId = 6`, `@subjectId = 1`, `@chapterId = 1` – feste Ids einer damaligen Dev-DB |
| `:4` | `@authToken = eyJ…` – ein **eingebranntes JWT** (abgelaufen, `exp` 2026-07-24), das die Login-Kette (`:17`, auskommentiert) ersetzt |
| `:29, :34, :39, :46, :53, :74, :81, :88, :98` | **neun Requests auf `…/subjects/{id}/chapters/…`** – die Route gibt es seit B-106 nicht (gemessen an `Creator.http` heute: `405`/`404`) |
| `:88-93` | `POST …/items { "vocabularyId": 825 }` – Id einer fremden DB |

`docs/REST/README.md` listet die Datei nicht; `.vscode/settings.json` kennt sie nicht. Sie ist der Rest einer
Arbeitssitzung, kein Tutorial – aber sie liegt im Tutorial-Ordner und öffnet sich im REST-Client wie eines.

## Die echte Lücke

Dieselbe wie bei B-238 (Doku, die beim Lesen nicht und beim Ausführen sofort auffällt), plus ein Token in
einer getrackten Datei. Dass es abgelaufen ist, macht den Befund kleiner, nicht gegenstandslos: ein Dev-JWT
mit Rollen-Claims gehört nicht ins Repo, auch nicht als Beispiel.

## Akzeptanzkriterien

1. `docs/REST/` enthält keine Route mit `chapters` mehr (Sweep aus B-166 AK 1 über `docs/REST/` → 0).
2. Kein `@authToken = eyJ…` in einer getrackten Datei (`git grep -n "eyJ" -- 'docs/**/*.http'` → 0).
3. Entweder ist die Datei weg, oder sie steht im REST-README mit ihrem Zweck und läuft von oben bis unten
   (Datum wie in `Creator.http`).

## Offene Punkte

1. **Löschen oder nachziehen?** Alles, was die Datei zeigt (Übung anlegen, Items hinzufügen, lesen), zeigt
   `Creator.http` nach B-238 bereits auf der lebenden Route. *Empfehlung: löschen* – eine zweite
   Creator-Datei ist ein zweiter Ablageort. Das ist eine Streich-Entscheidung, darum hier offen und nicht im
   Nachtlauf gefällt.
2. Steht das Token noch in der Git-Historie? Ja (`42cd318` und früher) – ein abgelaufenes Dev-Token aus der
   Historie zu tilgen lohnt den Rewrite nicht; die Entscheidung gehört trotzdem benannt.

## Verlauf

- **2026-10-23** — angelegt im zweiten Nachtlauf (Sprint 4) beim Routen-Sweep zu B-238, direkt
  `ausformuliert`: Fundstellen mit Zeile, Token-Ablauf aus dem Payload gelesen. **Bewusst nicht in B-238
  mitgenommen** (README: ein Fund beim Bauen wird eine eigene Story): B-238s AK 1 ist auf `Creator.http`
  geschnitten, und Punkt 1 ist eine Streich-Entscheidung.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (9× `chapters`, `eyJ` in `:4`); die Datei wird nirgends referenziert. Die Streich-Entscheidung (offener Punkt 1) steht auf der Grill-Tagesordnung als Ja/Nein-Frage; nach einem Ja ist der Rest XS.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-0 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): **löschen** (offener Punkt 1). Die Git-Historie wird **nicht** getilgt – das Token ist abgelaufen, ein History-Rewrite samt Force-Push lohnt nicht (offener Punkt 2). Damit ist die Story autonom schätz- und baubar (XS, Paket NP-12).
