---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Platzhalter „Cornelsen“ im leeren Verlagsfeld]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer zu B-267 in Lauf B (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-11-02"
---

# B-278 · Im leeren Verlagsfeld sieht der Platzhalter aus wie ein gespeicherter Wert

Seit [B-267](B-267-entkoppeltes-lehrbuch-zeigt-katalogwert-im-freitextfeld.md) zeigt das Freitextfeld nach dem Lösen
der Reihe, was gespeichert ist. Bei einem Buch, das nur über den Katalog verknüpft war, ist das *nichts*. Das leere
Feld trägt dann aber den grauen Platzhalter „Cornelsen“ (`frontend/src/vater/ChildMaterialSection.tsx`, Verlagsfeld).
Das liest sich wie „der Verlag ist noch da“, ausgerechnet wenn vorher Cornelsen aus dem Katalog dastand. Das
Klassenfeld (Platzhalter „8“) hat dasselbe in milderer Form.

Vorschlag des Reviewers: ein neutraler Platzhalter („z. B. Verlag“) oder keiner.

**In Lauf B bewusst nicht behoben:** 🟢-Hinweis, und der Zähler des Mini-Sprints stand bei 4 vor dem Delta-Review.

## User Story

Als **Vater**, der ein Lehrbuch von der Katalog-Reihe löst, möchte ich ein leeres Verlagsfeld als leer erkennen.

## Ist-Stand (Lauf L, am Code belegt)

Siehe oben; die Platzhalter sind „Cornelsen" (Verlag) und „8" (Klasse) in `ChildMaterialSection.tsx`.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **„z. B." vor beide Beispiele** („z. B. Cornelsen", „z. B. 8") statt sie zu streichen – das Beispiel hilft beim
   Ausfüllen, das Präfix macht es unverwechselbar zum Hinweis; dasselbe Muster wie „z. B. Fußball, Soccer" in der
   Schlagwort-Verwaltung. *Kosten:* zwei längere Platzhalter; die übrigen („Access", „Access 8", „Unit 4 – …") bleiben,
   sie stehen nie neben einem eben gelösten Katalogwert.

## Akzeptanzkriterien

1. Nach dem Entkoppeln eines nur über den Katalog verknüpften Buchs sind Verlag und Klasse leer, und beide Platzhalter
   beginnen mit „z. B.".

## Schätzung

`XS`, Frontend. Testweg: `TextbookForm.test.tsx`.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-267 in Lauf B ([Protokoll](../pm-sitzung-2026-10-26-tag.md)), Hinweis 4.
- **2026-10-30** — Lauf L, Teil 4: gebaut. Rote Probe **1 failed** (erwartet 1; „expected 'Cornelsen' to match /^z\. B\. /"
  – erst nachdem der Aufbau `publisherFreeText`/`gradeFreeText` ausdrücklich leer setzte; die erste Fassung fiel zu früh am
  Wert, siehe „Rote Probe am richtigen Ort"). Danach **20/20**.
- **2026-10-30** — Review `frontend-reviewer`: sauber (keine zweite Stelle mit nacktem Beispiel neben einem gelösten
  Katalogwert; `VaterFachlehrer` leitet keine Klasse ab). 🟢 die Klassen-Zusicherung (`:314`) ist von der Verlags-Probe
  verdeckt worden → einzeln gemessen, siehe nächste Zeile.
- **2026-10-30** — Klassen-Probe einzeln (nur `placeholder="8"` zurück): **1 failed** (erwartet 1, „expected '8' to match
  /^z\. B\. /"). Diese Zusicherung hängt nicht am Entkoppeln (ohne Unit-Liste steht das Klassenfeld schon vorher da).
- **2026-10-30** — Abnahme Lauf L, Teil 4: `tsc -b` sauber, Vitest **587/587**, E2E-Gesamtlauf **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet). Rollengang als E2E: Vater über die sieben Assistenten-Specs (Pillen und Karten jetzt als `radio` angesprochen) und `TextbookForm`; Sohn und Creator Regressionszeugen (Gesamtlauf grün, kein Pfad im Diff). Gerätecheck offen: die Radiogruppen einmal mit NVDA/VoiceOver anhören. **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
- **2026-11-02** — Nachschau in Lauf M, Teil 1 (frischer `frontend-reviewer`, `git show d8cc6b9 -- frontend`): **sauber** (nur 🟢). Geprüft: AK 1 Platzhalter → neuer Fall in `TextbookForm.test.tsx` (~:304), Mutanten `placeholder="Cornelsen"`/`"8"` je rot (gemessen); „Verlag leer" → `verlag.value === ""`, Mutante „Katalog-Verlag in den Freitext" rot; keine weitere Stelle mit nacktem Beispiel neben einem Katalogwert. 🟢: „Klasse leer" fehlt im neuen Fall (Mutante `grade` aus `book.grade` bliebe dort grün; der B-267-Fall daneben trägt sie) – in Lauf M, Teil 2 ergänzt.
