---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Testmodus Lückentext, Ausprobieren Cloze mobil]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: Nutzer beim Ausprobieren eines Lückentexts (Unit 1 · Past Progressive in der Stadt), Screenshot im Chat 2026-11-12
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-486 · Testmodus: Lückentext eine Lücke nach der anderen

Im Testmodus („🧪 Ausprobieren“) stand der Trägertext oben mit lauter gleichen „…“, darunter **je Lücke eine Karte
mit der ganzen Wortbank** – bei acht Lücken acht Mal fünfzehn Knöpfe. Am Handy hieß das: zwischen Text und Auswahl
hoch- und runterscrollen und abzählen, welche „…“ die „Lücke 7“ ist. Die Vorschau versprach „genau wie beim Kind“;
die Sohn-App zeigt aber je Karte **eine** Lücke, mit „?“ markiert (B-76).

## User Story

Als Vater möchte ich einen Lückentext am Handy ausprobieren können, ohne Lücken abzuzählen, damit ich ihn so prüfe,
wie mein Kind ihn erlebt.

## Ist-Stand am Code (vorher)

- `frontend/src/vater/ExercisePreviewModal.tsx` – `ClozePrompt` ohne `gapIndex` (alle Lücken neutral), danach
  `data.items.map(…)` mit je einer Wortbank.
- `frontend/src/components/ClozePrompt.tsx` – kennt nur „gefragt“ (`?`) und „neutral“ (`…`).

## Akzeptanzkriterien

1. Beim Lückentext zeigt der Testmodus **eine** Aufgabe; die gefragte Lücke trägt im Text das „?“ wie beim Kind.
2. Eine gewählte Antwort steht im Text in ihrer Lücke; ein Tipp auf eine Lücke springt zu ihr.
3. Nach einer Wahl in der Wortbank geht es zur nächsten Lücke; „← Zurück“ / „Weiter →“ / „Lücke n von N“.
4. Der Text ist höhenbegrenzt (40vh) und scrollt die gefragte Lücke ins Bild – Wortbank und Text passen am Handy
   zusammen auf den Schirm.
5. Andere Übungstypen und die Sohn-App unverändert (`ClozePrompt` ohne `onGap` rendert weiter Bilder, keine Knöpfe).

## Verlauf

- 2026-11-12 – gebaut: `ClozePrompt` um `filled`/`onGap` erweitert, Testmodus auf Schrittfolge umgestellt.
  Vitest 752/752 grün, drei neue Fälle in `ClozePrompt.test.tsx`; rote Probe (Antwort nicht eingetragen, Klick ohne
  Wirkung) lässt genau die beiden tragenden Fälle fallen, der dritte ist Absichtserklärung (rot erst, wenn die
  Kind-Ansicht Knöpfe bekäme). `tsc -b` sauber.
- 2026-11-12 – Rollengang im Browser bei 375×812 gegen eine Wegwerf-DB (eigenes Backend :5201, der Heimnetz-Server
  auf :5200 blieb unberührt): Wortbank-Stufe, „?“ an Lücke 1, „played“ gewählt → steht in Lücke 1, weiter zu
  Lücke 2; Tipp auf „Lücke 1: played“ springt zurück (`Lücke 1 von 8`). Konsole ohne Fehler.
  **Nicht** geprüft: die Sohn-Übung mit Wortbank am Handy (unverändert, nur der gemeinsame Baustein).
  Kein E2E ergänzt: Playwright braucht Port 5200, den der Heimnetz-Server hält.
