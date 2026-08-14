---
tags: [typ/story, status/abgenommen, bereich/tests, bereich/frontend]
aliases: [drei Ursachen ein disabled, 20 ms Reserve, Wartepunkt auf die 500]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-08-14 zu B-169 (Nachtlauf, Retro Sprint 2)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-169]
wartet_auf: ""
nachgeschaut: ""
---

# B-180 · Eine Zusicherung hing an 20 ms Scheduling statt am Gate

## Ist-Stand am Code (gemessen, nicht geschlossen)

`frontend/e2e/assistent.spec.ts`, Fall „Scheitert die Suche, ist auch der Knopf »Alle wählen« gesperrt":

```ts
await page.getByLabel("Übung suchen", { exact: true }).fill("environment");
await expect(alleWaehlen).toBeDisabled();
```

Das `disabled` des Knopfes hat **drei** Ursachen (`VaterWizard.tsx`:
`selectAllBusy || exercises.loading || zeilenVeraltet`), und die Zusicherung trennt sie nicht. Ohne
Wartepunkt auf die gescheiterte Antwort ist sie schon **im Ladefenster** wahr — durch genau das
`exercises.loading`, von dem der Kommentar zwei Zeilen darüber sagt, dass es den Fehlerzweig *nicht* deckt.
Der Fall behauptete also, das Gate zu belegen, und belegte „irgendwann nach dem Tippen gesperrt".

**Die Reserve, über acht Läufe vermessen** (`--repeat-each 8 --trace on`, Traces gegen `0-trace.network`
gelegt): Puffer zwischen „Antwort fertig" und der ersten Auswertung der Knopf-Zusicherung
**+25,2 / +15,8 / +24,8 / +19,2 / +19,0 / +18,9 / +15,5 / +36,2 ms**. Die Kästchen-Zusicherung eine Zeile
darüber wertete in 7 von 8 Läufen **vor** dem Antwortende aus (−0,5 bis −5,6 ms) — die Kommandokette liegt
unmittelbar auf der Antwortgrenze, und nur ein zusätzliches IPC-Kommando schiebt die zweite dahinter.

Beim **Kästchen** ist das harmlos: sein `disabled` trägt allein `zeilenVeraltet`, es ist von sich aus
eindeutig. Beim **Knopf** ist es der Unterschied zwischen Beweis und Zufall.

## Die echte Lücke

Nicht „der Test ist rot geworden, obwohl er grün sein sollte" — er war grün, und die rote Probe der
Mutter-Story war rot. Die Lücke ist, dass **AK 3 von B-169** („ein Test wird rot, wenn das Gate entfernt
wird") damit an einer Scheduling-Reihenfolge hängt statt an der Struktur. `route.fulfill` läuft über
denselben Treiber wie die Zusicherungen; die Reihenfolge ist nirgends garantiert.

Und es ist dieselbe Fehlerfamilie wie der Defekt, den der Fall bewachen soll: **eine Bedingung, die drei
Situationen zusammenzieht** — hier im Messinstrument statt im Produkt. Das ist in diesem Lauf der zweite
Fall davon (der erste war B-165s Zähler, der „Sperre" und „zerrissener Inhalt" nicht unterscheidet).

## Fix

Ein Wartepunkt vor der Zusicherung, damit `exercises.loading` aus der Gleichung ist:

```ts
const gescheitert = page.waitForResponse((r) => r.url().includes("/creator/exercises") && r.status() === 500);
await page.getByLabel("Übung suchen", { exact: true }).fill("environment");
await gescheitert;
await expect(alleWaehlen).toBeDisabled();
```

Fall 2 derselben Datei macht es strukturell schon richtig vor (Frist 1200 ms **kürzer** als die 2-s-Verzögerung).
Fall 3 hatte dieselbe Sorgfalt nicht bekommen, obwohl sein Attribut mehrdeutig ist.

## Verlauf

- 2026-08-14 · Aufgenommen **und gebaut** aus der Nachschau zu B-169 (Retro von Sprint 2 desselben
  Nachtlaufs). Eigene Story statt einer Zeile im `## Verlauf` von B-169, weil ein Defekt in abgenommener
  Arbeit sonst aus der Messung fällt (`docs/backlog/README.md` → „Warum der Defekt eine eigene Story
  braucht") — auch wenn er in einer Zeile behoben ist.
  **Zählt nicht in den Fehlerzähler von Sprint 2:** Der Fund liegt außerhalb dessen Diffs (Sprint 1s Datei),
  und Freigabe 3 schneidet dort ausdrücklich. Er ist trotzdem sofort behoben statt nur gemeldet.
  **Verifikation:** `assistent.spec.ts` **3 passed**, der betroffene Fall zusaetzlich `--repeat-each 4`
  **4 passed** — der Wartepunkt haengt nicht mehr an der Reserve, sondern an der Antwort. Die Struktur ist
  damit der Beleg, nicht das Timing.
