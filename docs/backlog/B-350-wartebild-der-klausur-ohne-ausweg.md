---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/student]
aliases: [Klausur „wird vorbereitet" ohne Ausweg]
status: geschaetzt
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf L, Teil 3 (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
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

## User Story

Als **Sohn**, dessen Klausur beim Vorbereiten hängt, möchte ich zurück zur Basis kommen, ohne den Browser zu bemühen.

## Ist-Stand (Lauf N, am Code belegt, Stand `d0afbc0`)

- `SohnTest.tsx:170`: `if (!item) return …<div className="loading">{t("test.preparing")}</div>…` – kein Knopf. Die Zweige
  davor (Fehler `:162-164`, Ergebnis) haben je einen Weg zur Basis (`progress.toHome`).

## Die echte Lücke

Das einzige Bild der Klausur ohne Ausweg ist das, in dem man hängen bleiben kann.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Ein Geister-Knopf „Zur Basis" im Wartebild**, derselbe wie im Fehlerbild. *Kosten:* das Bild zeigt beim normalen
   Start für einen Moment einen Knopf; das ist im Fehlerbild genauso.
2. **Der Stepper des Assistenten** (aus dem Review von B-348 hier vermerkt) ist ein eigenes Thema und bleibt außen vor.

## Akzeptanzkriterien

1. Im Wartebild steht „Zur Basis" und führt dorthin.

## Schätzung

`XS`, Frontend. Testweg: `SohnTest.test.tsx`.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf L, Teil 3 (`frontend-reviewer` über `fce0076`, 🟢). Aus Code
  gelesen. Älter als B-339 (der `start()`-Pfad führte schon dorthin).
- **2026-11-02** — Lauf N: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt.
