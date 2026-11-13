---
tags: [typ/story, status/verworfen, bereich/frontend, rolle/creator]
aliases: [Reihe anlegen auf dem Handy]
status: verworfen
prio: P3
art: Frage
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-u
grund: "gemessen 2026-11-13: bei 393x830 liegt die Unterkante des Meldungsplatzes nach 'Reihe anlegen' bei 507,75 px, also im Bild - die Vorbedingung der Entscheidung trifft nicht zu, kein Code noetig"
ersetzt_durch: []
---

# B-384 · Prüfauftrag: sieht man „steht im Katalog" auf dem Handy, wenn man unten auf „Reihe anlegen" drückt?

B-381 hat entschieden: eine Bestätigung aus einem Formular „in oder direkt unter der Tabelle" scrollt nicht zum Meldungsplatz.
„Lehrwerk hinzufügen" (`NewSeries`) zählt dazu, weil der Platz direkt darüber steht. Auf einem schmalen Bildschirm ist das
Formular aber hoch (sieben Felder untereinander): wer unten auf „Reihe anlegen" drückt, sieht womöglich nur das Namensfeld
leer werden und nicht die grüne Meldung – dasselbe Muster wie B-232. Zu messen bei 393 × 830 (Playwright-Vorgabe); trägt
die Messung den Fall, scrollt auch `NewSeries` über `meldeVonWeitWeg`. Eine belegte Antwort „sichtbar, nichts zu tun" ist
zulässig.

## User Story

Als **Creator am Handy** möchte ich nach „Reihe anlegen" die Bestätigung sehen, ohne selbst nach oben zu scrollen, damit
ich weiß, dass die Reihe im Katalog steht, und sie nicht ein zweites Mal anlege.

## Ist-Stand am Code

- Der eine Meldungsplatz der Seite steht **über** dem Anlege-Formular, am Ende des Abschnitts „Lehrwerke"
  (`frontend/src/vater/VaterLehrwerke.tsx:266`); `NewSeries` folgt darunter (`:269-272`) und bekommt `onMeldung={melde}`
  (`:270`), also „nah" – eine Bestätigung scrollt nicht.
- Die Regel: Fehler holen den Platz immer, Bestätigungen nur über `meldeVonWeitWeg`; gescrollt wird mit
  `block: "nearest"`, also **nur, wenn der Platz außer Sicht ist** (`:177-195`). Die Nachbarn `PublisherAdmin` (`:275`)
  und Reihen-Zeilen (`:253`) melden schon „weit weg".
- `NewSeries` selbst: `VaterLehrwerke.tsx:938`. Die Playwright-Vorgabe ist 393 × 830 (`frontend/playwright.config.ts:42`).
- Gegenproben zu B-381/B-385 liegen bereits als E2E vor: `frontend/e2e/reihe-bestaetigung-im-bild.spec.ts` (Muster mit
  Vorbedingung „Platz liegt außer Sicht") und `frontend/e2e/unit-anlegen-bleibt-im-bild.spec.ts`.

## Die echte Lücke

Ob der Platz bei 393 × 830 nach dem Klick auf „Reihe anlegen" außer Sicht liegt, ist **ungemessen**. Liegt er außer
Sicht, sieht der Creator nur das Namensfeld leer werden.

## Offene Punkte

1. ~~Erst messen und dann entscheiden, oder die Messung zur Vorbedingung des Tests machen?~~ → Entscheidung 1.
2. ~~Verletzt `meldeVonWeitWeg` für `NewSeries` die Entscheidung aus B-381 (offenes Formular bleibt im Bild)?~~ → Entscheidung 2.

## Entscheidungen

1. **Die Messung ist die Vorbedingung der E2E.** Die Spec fährt bei 393 × 830 bis „Reihe anlegen", prüft vor dem Klick,
   dass der Meldungsplatz oberhalb des Sichtbereichs liegt, und danach, dass die Bestätigung im Bild ist. Fällt schon die
   Vorbedingung (Platz sichtbar), ist die belegte Antwort „sichtbar, nichts zu tun": Story `verworfen` mit dem Messwert,
   kein Code. Begründung: eine einmalige Handmessung verrottet, eine Vorbedingung im Test bleibt. Kosten: eine E2E mehr
   (~10 s).
2. **`NewSeries` meldet über `meldeVonWeitWeg`.** Begründung: `block: "nearest"` scrollt nur, wenn der Platz außer Sicht ist
   – am Desktop ändert sich nichts, und B-381 bleibt dort gewahrt; am Handy rückt der Platz an den oberen Rand, direkt
   darunter steht das geleerte Namensfeld, das Formular bleibt also im Bild. Kosten: am Handy springt die Seite nach
   jedem Anlegen ein Stück nach oben; wer mehrere Reihen hintereinander anlegt, scrollt zum Knopf zurück.

Beide: Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
[Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

## Akzeptanzkriterien

1. Bei 393 × 830, Knopf „Reihe anlegen" ins Bild gescrollt und Meldungsplatz nachweislich darüber außer Sicht: nach dem
   Klick ist „… steht im Katalog" (bzw. der Bestätigungstext von `NewSeries`) `toBeInViewport`. *Rot macht ihn:*
   `onMeldung={melde}` an `VaterLehrwerke.tsx:270` (heutiger Stand).
2. Im selben Lauf ist danach das Namensfeld `#ns-name` im Bild. *Rot macht ihn:* ein Scroll mit `block: "start"` oder auf
   ein Ziel, das das Formular aus dem Bild schiebt.
3. Desktop (1024 × 768, kurze Liste): nach „Reihe anlegen" ändert sich `window.scrollY` nicht. *Rot macht ihn:* ein Scroll,
   der nicht `nearest` ist. (Gegenprobe zu B-381.)

## Schätzung

**Größe: XS** — eine Prop an `VaterLehrwerke.tsx:270` plus eine E2E nach dem Muster von `reihe-bestaetigung-im-bild.spec.ts`.

- **Risiken:** Die Vorbedingung kann bei leerer Test-DB anders ausfallen als bei langer Liste – die Spec stellt ihren
  Zustand selbst her (Viewport setzen, Knopf ins Bild scrollen), statt auf die Listenlänge zu vertrauen. Sitzt der Platz
  schon im Bild, ist das Ergebnis die Verwerfung, nicht ein grüner Test ohne Aussage.
- **Angriffsplan:** kein Backend. (1) E2E schreiben, gegen den heutigen Stand laufen lassen: Vorbedingung grün und AK 1
  rot = Fall getragen; Vorbedingung rot = verwerfen. (2) Prop umstellen, Kommentar `:177-187` um den Handy-Fall ergänzen.
  (3) `unit-anlegen-bleibt-im-bild.spec.ts` und `reihe-bestaetigung-im-bild.spec.ts` mitlaufen lassen.
- **Testweg:** neue Spec `frontend/e2e/reihe-anlegen-handy-im-bild.spec.ts`, Lauf über `./node_modules/.bin/playwright test
  e2e/reihe-anlegen-handy-im-bild.spec.ts e2e/reihe-bestaetigung-im-bild.spec.ts e2e/unit-anlegen-bleibt-im-bild.spec.ts`;
  gezählt wird „N passed", nicht der Exit-Code.

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-381 (Lauf U, Sprint 3, `frontend-reviewer`, 🟢).
- **2026-11-13** — ausformuliert (Backlog-Lauf): Platz über dem Formular (`VaterLehrwerke.tsx:266-270`), Scroll nur bei
  `nearest`; die Messung fehlt.
- **2026-11-13** — gegrillt: Messung als Vorbedingung der E2E, `NewSeries` meldet „weit weg". Stakeholder-Entscheidung im
  Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — geschaetzt: XS, frontend, keine Migration, kein Vertragsbruch.
- **2026-11-13** — `in-arbeit` (Sprint 71, Frontend), Angriffsplan-Schritt 1: Spec
  `frontend/e2e/reihe-anlegen-handy-im-bild.spec.ts` geschrieben (Handy-Fall mit der Messung als Vorbedingung
  „Platz-Unterkante ≤ 0", Wert als Anhang `Messung`; AK 1 Meldung im Bild, AK 2 `#ns-name` geleert und im Bild;
  Desktop-Gegenprobe 1024 × 768 mit Vorbedingung „Platz im Bild" und unverändertem `scrollY`). Code unverändert
  (`VaterLehrwerke.tsx:270` trägt weiter `onMeldung={melde}`): die Messung gegen den heutigen Stand steht aus, weil
  Playwright im Sprint wegen paralleler Debug-Builds des Backends zurückgestellt ist. Weiter: Spec auf Ausweichports
  (5212/5185) gegen den heutigen Stand – Vorbedingung rot → `verworfen` mit Messwert; Vorbedingung grün und AK 1 rot →
  Prop auf `meldeVonWeitWeg`, Kommentar `:177-187` ergänzen, drei Specs laufen lassen.
- **2026-11-13** — **verworfen** nach Messung (Entscheidung der Story: erst messen, sonst verwerfen). Playwright gegen frisch gestartete Server auf Ausweichports (API 5212, Web 5185), Viewport 393 × 830: Unterkante des Meldungsplatzes **507,75 px** (`getBoundingClientRect().bottom`), die Vorbedingung „≤ 0 px“ fiel; die Desktop-Gegenprobe lief grün. Die Mess-Spec `reihe-anlegen-handy-im-bild.spec.ts` ist wieder entfernt, weil sie eine verworfene Annahme dauerhaft rot hielte. Nichts gebaut.
