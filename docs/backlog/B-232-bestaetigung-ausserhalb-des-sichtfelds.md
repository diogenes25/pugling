---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Gespeichert außerhalb des Sichtfelds, Meldungsplatz scrollt nur bei Fehlern]
status: idee
prio: P3
art: Frage
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 2, Fund D1)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-232 · Soll eine Bestätigung zum Meldungsplatz scrollen, wenn er weit vom Knopf entfernt liegt?

Die Lehrwerke-Seite hat seit B-212 **einen** Meldungsplatz – unter der Reihen-Tabelle, am Ende des ersten
Abschnitts (`frontend/src/vater/VaterLehrwerke.tsx`, `meldungsplatz`). Der Scroll-Effekt dorthin greift
**nur bei Fehlern** (`if (meldung && !meldung.ok) …scrollIntoView({ block: "nearest" })`), mit begründeter
Entscheidung im Kommentar: „Eine Bestätigung, die man nicht sieht, ist verschmerzbar; ein Fehler, den man
nicht sieht, ist der stumme Pfad aus B-200." Diese Abwägung kannte als Sender nur die Formulare direkt unter
der Tabelle. Seit B-224 meldet auch das **Verlags-Vokabular** dorthin – und das liegt unter dem gesamten
Anlege-Formular, zwei Abschnitte tiefer.

**Gemessen im Rollengang am 2026-10-18** (Desktop, 889 px Höhe, vier geseedete Reihen): nach dem Umbenennen
eines Verlags stand „Gespeichert." bei Viewport-y 302, der gedrückte OK-Knopf bei y 807 – beides sichtbar, 505 px
auseinander. Mit dreißig Reihen (die Tabelle hat kein Paging, je Zeile ~56 px) läge der Platz rund 1,7 Bildschirme
über dem Knopf: der Creator sähe nur, dass der OK-Knopf verschwindet (`dirty` kippt nach dem Reload), und läse
keine Rückmeldung. Weder E2E (`toHaveCount`/`toBeVisible` prüfen nicht den Viewport) noch der Rollengang vom
2026-10-17 (`.banner.ok` per Skript gelesen) messen den Sichtbereich.

Es ist eine **Frage**, kein Defekt: B-212 hat „Bestätigung darf unsichtbar sein" ausdrücklich entschieden;
B-224 hat den Sender hinzugefügt, ohne die Entscheidung neu zu wägen. Kandidaten: die Bedingung `!meldung.ok`
fallen lassen (`block: "nearest"` scrollt ohnehin nur, wenn nötig – eine sichtbare Bestätigung bleibt ein
No-op); oder den Meldungsplatz ans Seitenende/sticky setzen. Beides ändert, wie die Seite sich beim Speichern
bewegt – Produktgefühl, darum beim Nutzer.

## Verlauf

- **2026-10-18** — angelegt aus der Nachschau zu Sprint 2 des Nachtlaufs 2026-10-17 (Fund D1, `frontend-reviewer`),
  im Browser nachgemessen (Werte oben). Als `Frage` abgelegt, weil B-212 den Punkt entschieden hat.
