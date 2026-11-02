---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Klausur „wird vorbereitet" ohne Ausweg]
status: abgenommen
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
nachgeschaut: "2026-11-02"
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
- **2026-11-02** — Lauf N, Teil 1: gebaut. Rote Probe **1 failed** (erwartet 1): `SohnTest.test.tsx` „bietet den Weg zur Basis an, solange die Frage noch nicht da ist" (kein Knopf „Zur Basis"). AK 1 → dieser Fall (Klick führt zur Basis-Route); Mutante „Knopf weg" ist der Ausgangszustand. Vitest **597/597**.
- **2026-11-02** — Review `frontend-reviewer`: 🟡 der neue Ausweg ließ hängende Aufrufe weiterwirken – ein später 404 holte das Kind von einer anderen Seite zur Basis zurück, ein später fertiger Versuch feierte dort (react-router setzt `navigate` beim Aushängen nicht still). Behoben: Ref `gone` (beim Einhängen zurückgesetzt, StrictMode), geprüft vor der Umleitung und vor Ergebnis/Feier; das Guthaben wird trotzdem aufgefrischt. Rote Probe **2 failed** (erwartet 2: SHOP fehlt bzw. `celebrate` gerufen); Mutanten „ohne `gone` im 404", „ohne `gone` in `finish`", „ohne Zurücksetzen" (StrictMode-Fall) je **1 failed**. Zählt (Zähler Teil 1: 1). 🟢 ohne Zählung, nur vermerkt: der Fokus fällt beim Erscheinen der Frage auf `<body>` (wie im Fehlerbild).
- **2026-11-02** — Abnahme Lauf N, Teil 1: Vitest **601/601**, `tsc -b` sauber, E2E **102 passed** (`exit=0`, nach dem letzten Edit gestartet). Rollengang: Vater im echten Browser über `assistent-kinderliste-kommt-spaet.spec.ts` (Pfeiltaste und Tab-Stopp in der Intensitäts- bzw. Ziel-Gruppe); Sohn über die Komponententests des Wartebilds (ein hängender Start lässt sich im E2E nicht billig erzeugen – argumentierte Ausnahme nach pm-loop Step 6), seine Specs im E2E grün; Creator Regressionszeuge. **Gerätecheck offen:** die Gruppen einmal mit Screenreader und Pfeiltasten bedienen. **Abgenommen**, Commit siehe Protokoll ([Lauf N](../pm-sitzung-2026-11-02-nacht.md)).
- **2026-11-02** — Nachschau in Lauf N, Teil 2 (frischer `frontend-reviewer`, `git show b73bcde -- frontend`, Vitest 28/28): **sauber** (🟢 → [B-356](B-356-radiogruppe-und-wartebild-restkanten.md)). Geprüft: AK 1 → Knopf und Ziel-Route; 404- und Fertig-Fall ohne `gone` je rot (Microtasks nachgezählt); StrictMode: `gone` und Start-Effekt laufen synchron im Commit, `startedFor` verhindert den zweiten Start; jede Fortsetzung nach `await` (`start`, `resume`, `answerAndAdvance`) geht über die geschützten Stellen.
