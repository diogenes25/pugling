---
tags: [typ/story, status/idee, bereich/punkte, rolle/supervisor, rolle/student]
aliases: [Münzen heute im Kind-Hub, Zielbonus in der Sohn-App]
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-24.md#runde-140--re-review--abnahme
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-955 · Dieselben Worte für verschiedene Zahlen: „Münzen heute“ im Kind-Hub, 🏆 in der Sohn-App

Aus dem Review zu B-954, dort bewusst nicht gebaut (das Sprint-Ziel war ohne sie erreicht).

- Die Vater-Startseite sagt seit B-954 „Münzen heute“ und meint alle verdienten Münzen des Tages. Der Kind-Hub sagt
  „Münzen heute: N von Deckel“ (`frontend/src/vater/VaterKind.tsx`, um Zeile 442) und meint nur die Münzen aus
  Extra-Übungen gegen den Deckel. Für dasselbe Kind stehen dieselben zwei Worte auf zwei Seiten für zwei Zahlen.
- Die Sohn-App zeigt in „Fortschritt“ `totalPoints` als 🏆 ohne Wort (`frontend/src/sohn/SohnProgress.tsx:82`). Die Zahl ist
  die Summe der Zielboni des Plans, nicht die verdienten Münzen.
- Ein nachgetragener Test bucht seinen Zielbonus am Tag des Nachtrags (B-954, Entscheidung 6): Die Zeitleiste zeigt für den
  nachgetragenen Tag „1 von 1 Zielen, 0 Münzen“.

## User Story

Als Vater und als Sohn möchte ich, dass eine Zahl überall dasselbe heißt, damit ich zwei Seiten nicht gegeneinander lese.

## Verlauf

- **2026-11-24** — angelegt (Quelle: `frontend-reviewer` und `pugling-reviewer` zu B-954; Fundorte ungeprüft übernommen).
