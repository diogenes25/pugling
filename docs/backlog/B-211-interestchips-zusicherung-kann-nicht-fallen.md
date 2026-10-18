---
tags: [typ/story, status/abgenommen, bereich/frontend, typ/test]
aliases: [leere Zusicherung Einkleidung hinzufuegen]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-03 zu B-206 (Nachtlauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-206]
wartet_auf: ""
---

# B-211 · Eine Zusicherung prüft den Ausgangszustand und kann darum nie fallen

Der **fünfte** gemessene Fall dieser Klasse in diesem Repo (nach B-154, B-161, B-157 zweimal) — und der
erste, den die Nachschau statt eines Reviewers gefunden hat.

## User Story

Als **Entwickler** möchte ich, dass ein Testfall, der die `busy`-Verdrahtung eines Knopfes behauptet, auch
fällt, wenn diese Verdrahtung verschwindet.

## Ist-Stand am Code

`frontend/src/vater/InterestChips.test.tsx:95` im Fall „sperrt … solange gespeichert wird":

```ts
expect(screen.getByRole("button", { name: "Einkleidung hinzufügen" }).hasAttribute("disabled")).toBe(true)
```

Der Knopf trägt `disabled={disabled || pick === ""}`. Der Test **wählt nichts aus**, `pick === ""` ist also
wahr — der Knopf ist im Ausgangszustand ohnehin gesperrt. **Rote Probe (gemessen):** `disabled ||` aus der
Bedingung streichen, und Zeile 95 bleibt grün.

Zeile 94 (der Entfernen-Knopf) trägt dagegen echt — der Fall ist also nicht wertlos, nur zur Hälfte.

## Offene Punkte

1. Eine Auswahl treffen und dann prüfen, oder den Knopf mit gesetztem `pick` rendern? *Empfehlung:
   Auswahl treffen* — das ist der Weg, den ein Nutzer nimmt, und er deckt zugleich, dass `pick` überhaupt
   gesetzt werden kann.

## Entscheidungen

Autonom gegrillt am 2026-10-18 unter Freigabe 1 (`art: Defekt`).

1. **Erst wählen, dann sperren.** Der Fall rendert mit `disabled={false}`, wählt im Pulldown „Comic" (Wert 7),
   prüft, dass der Knopf *jetzt* bedienbar ist, rendert dann mit `disabled={true}` neu und prüft die Sperre.
   *Begründung:* Das ist der Weg eines Nutzers (das Pulldown ist während des Speicherns selbst gesperrt, eine
   Auswahl während des Speicherns gibt es nicht), und die Zwischenzusicherung „bedienbar mit Auswahl" ist die
   Hälfte, ohne die die Sperr-Zusicherung wieder am Ausgangszustand hinge. *Kosten:* ein `rerender`, zwei
   Zeilen; der Fall bleibt derselbe (kein neuer Testname), damit die Historie seines Zwecks erhalten bleibt.

## Akzeptanzkriterien

1. Die Zusicherung an „Einkleidung hinzufügen" fällt, wenn `disabled ||` aus der Knopf-Bedingung gestrichen wird.
   *Beleg:* rote Probe gegen das mutierte Bauteil (erwartet 1 von 8 rot, an der Sperr-Zusicherung – heute
   Zeile 105, der Entfernen-Knopf Zeile 103), danach wiederhergestellt 8/8 grün.
2. Der Entfernen-Knopf (Zeile davor) wird weiter geprüft.

## Schätzung

**Größe: XS** – ein Testfall, drei Zeilen. Anker: B-02. **Testweg:** `vitest run src/vater/InterestChips.test.tsx`
mit vorübergehend mutiertem `ExerciseEditModal.tsx` (rot), `git checkout` (grün).

## Verlauf

- **2026-09-03** — aus der **Nachschau** zu B-206 (Nachtlauf), im Abnahme-Diff `5836585` entstanden. Rote
  Probe gemessen: Zusicherung bleibt grün, wenn `disabled` aus der Bedingung fällt.
- **2026-10-18** — **ausformuliert → gegrillt → geschaetzt** im Nachtlauf (zweiter Lauf der Nacht 2026-10-17,
  Sprint 4), autonom unter Freigabe 1: erst wählen, dann sperren; XS, frontend.
- **2026-10-18** — **geschaetzt → abgenommen** (Sprint 4). **Rote Probe (erwartet/gemessen):** `ExerciseEditModal.tsx:764`
  vorübergehend ohne `disabled ||` → `InterestChips.test.tsx:105` **1 von 8 rot** (`true` erwartet, `false`
  gemessen); nach `git checkout` 8/8. Belege: `vitest run` 389/389 · `frontend-reviewer`: fällt aus dem richtigen
  Grund (Pulldown eindeutig, Zwischenzusicherung hält auch für die Mutante, `pick` überlebt das `rerender`), ein
  Nit zur Zählweise in AK 1 (übernommen) · **Rollengang:** Test-Änderung ohne sichtbares Verhalten – die drei
  Rollen sind Regressionszeugen; `einkleidung.spec.ts` 1/1 fährt die Fläche im echten Browser. Commit `49b1fbc`
  (Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`, „Sprint 4").
