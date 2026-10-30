---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Klausur „wird vorbereitet" ohne Ausweg]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf L, Teil 3 (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-350 · Im Wartebild der Klausur hat das Kind keinen Ausweg

Das Bild „wird vorbereitet" (`frontend/src/sohn/SohnTest.tsx`, Zweig `!item`) zeigt nur einen Kasten, keinen Knopf
„Zur Startseite" oder „Später weiter". Hängt `startTest`/`nextTest`, steht das Kind dort, bis es über den Browser
zurückgeht. Seit [B-339](B-339-fortsetzen-sagt-alte-frage-an.md) führt auch „Weiter versuchen" in dieses Bild; vorher
blieb die alte Karte mit ihrem Ausweg stehen. Dasselbe Bild erreicht eine Antwort `done: false` mit `item: null` – der
Vertrag lässt sie zu, der Server sendet sie heute nicht; dann bliebe es für immer.

Vorschlag des Prüfers: ein Geister-Knopf „Zur Startseite" (`t("progress.toHome")`) im Wartebild. Daneben (aus dem Review
von B-348, eigenes Thema): der Stepper des Assistenten zeigt den aktuellen Schritt nur über Farbe (`aria-current="step"`).

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf L, Teil 3 (`frontend-reviewer` über `fce0076`, 🟢). Aus Code
  gelesen. Älter als B-339 (der `start()`-Pfad führte schon dorthin).
