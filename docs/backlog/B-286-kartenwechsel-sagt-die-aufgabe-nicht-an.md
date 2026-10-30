---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/a11y, rolle/student]
aliases: [Kartenwechsel sagt die neue Aufgabe nicht an]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer zu B-05 in Lauf E (docs/pm-sitzung-2026-10-27-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: "Gerätecheck: Screenreader-Ansage (NVDA/VoiceOver) in Übung und fortgesetzter Klausur anhören"
nachgeschaut: "2026-10-30"
---

# B-286 · Nach dem Kartenwechsel hört ein Screenreader die Eingabe, aber nicht die neue Aufgabe

Beim Üben (`SohnPractice.tsx`) und in der Klausur (`SohnTest.tsx`) wechselt die Karte, ohne dass die neue Aufgabe
(`prompt`) angesagt wird: Bei Buchstabenkästchen und dem Freitextfeld behält das `<input>` den Fokus, bei
„Buchstaben ordnen“ steht er seit B-05 auf der ersten Kachel („Buchstabe d, Schaltfläche“). Ein sehender Nutzer liest
die Aufgabe über dem Feld; ein Screenreader-Nutzer muss zurücknavigieren, um zu erfahren, was gefragt ist.

Altlast, nicht durch B-05 verursacht (vorher fiel der Fokus auf `body`, das war schlechter). Der Review nennt zwei
Wege: die Aufgabe beim Kartenwechsel in eine Live-Region spiegeln (Muster B-134, `role="status"` steht dauerhaft im
DOM) oder den Fokus auf die Karte selbst setzen und die Eingabe darunter lassen. Beides ist zu entscheiden, nicht nur
zu bauen.

## User Story

Als Kind, das mit einem Screenreader übt, möchte ich nach jeder Karte hören, was jetzt gefragt ist, damit ich nicht erst
zurücknavigieren muss.

## Ist-Stand (Lauf K, am Code belegt)

- `SohnPractice.tsx`: die einzige Live-Region ist der Toast (`:381`); die Aufgabe steht in `ClozePrompt` ohne Region.
- `SohnTest.tsx`: gar keine Region für die Aufgabe; die Karte bleibt gemountet (kein `key`), nur `item` wechselt.
- `LetterTiles` hat eine eigene Region („Gelegt: …"), die die gelegten Buchstaben ansagt, nicht die Aufgabe.

## Die echte Lücke

Der Fokus bleibt nach dem Wechsel auf der Eingabe (gewollt, B-05); was sich ändert, ist nur Text an anderer Stelle.

## Entscheidungen (autonom, Freigabe 1 – `Aufräumen`)

1. **Live-Region statt Fokuswechsel.** Den Fokus auf die Karte zu setzen bräche die Regel aus B-05 (Fokus auf der ersten
   Kachel, von einer E2E-Spec gehalten) und kostete einen Tab-Schritt je Karte. *Kosten:* „polite" – die Ansage wartet,
   bis der Screenreader fertig ist. ~~Die erste Karte bleibt stumm.~~ **Überholt nach dem Review:** die Region steht über
   allen Ansichten und ist derselbe Knoten ab dem Laden – auch die erste und die fortgesetzte Aufgabe werden angesagt.
2. **Ein Bauteil `TaskAnnouncer`** neben `ClozePrompt`, immer im DOM, Text „Neue Aufgabe: …"; Lücken gesprochen, wie
   `ClozePrompt` sie benennt, mit derselben Rückfall-Regel. In die Bausteinliste des Live-Region-Wächters
   (`liveRegions.test.ts`), damit niemand es bedingt einhängt. *Kosten:* ein i18n-Schlüssel (`play.newTask`, beide Sprachen).
3. **Hörstufe ohne Text sagt nichts** – der Ton spielt selbst an; zwei Karten mit gleichem Text werden nur einmal
   angesagt. *Kosten:* benannt, nicht gelöst.

## Akzeptanzkriterien

1. Nach dem Weiterrücken in Übung und Klausur steht die neue Aufgabe in einer `role="status"`-Region.
2. Lücken werden als „gesuchte Lücke"/„Lücke n" gesprochen, nie als `{{n}}`; fehlt die gefragte Lücke, der rohe Text.
3. Die Region steht ohne Aufgabe leer im DOM; bedingtes Einhängen fängt der Wächter.

## Schätzung

`S`, Frontend; Komponententests (`ClozePrompt.test.tsx`, `SohnPractice.test.tsx`, `SohnTest.test.tsx`), Wächter.

## Verlauf

- **2026-10-27** — angelegt aus dem `frontend-reviewer` zu B-05 in Lauf E ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Hinweis 5 (🟢, außerhalb des Diffs).
- **2026-10-29** — Lauf K, Teil 2: ausformuliert, autonom gegrillt (Freigabe 1, `Aufräumen`), geschätzt, gebaut. Rote Probe
  **5 failed / 25 passed** (erwartet 5: drei am fehlenden Bauteil, je einer an der fehlenden Ansage in Übung und Klausur).
  Der Rückfall-Fall prüfte zuerst einen Text ohne Lücken – beide Zweige gleich; umgestellt auf „gefragte Lücke fehlt".
  Mutanten je **1 failed / 160 passed**: ohne Rückfall (Rückfall-Fall); rohe Platzhalter (Lücken-Fall); bedingt eingehängt
  (Wächter `liveRegions.test.ts`); Übung folgt dem Wechsel nicht (Übungs-Fall); Klausur ohne Ansage (Klausur-Fall).
- **2026-10-29** — `frontend-reviewer`: 🟡 nach „Weiter versuchen" hängt das Fehlerbild die Region aus und mit fertigem Text wieder ein – die fortgesetzte Frage blieb stumm, und der Kommentar zur ersten Karte stimmte nicht (**zählt**); 🟡 kein Fall hielt, dass die Region DERSELBE Knoten bleibt – `key=` blieb grün (**zählt**). Behebung: die Region steht in Übung und Klausur über allen Ansichten (frühe Returns in `view()`), damit wachsen auch die erste und die fortgesetzte Aufgabe in einen vorhandenen Knoten; Entscheidung 1 („die erste Karte bleibt stumm") ist damit überholt. Fälle mit Knoten-Identität; Mutanten „`key` je Karte" **1 failed**, „neuer Knoten nach Fehler" **1 failed**, „Fehlerbild behält alte Frage" **1 failed**. 🟢 Rückfall-Regel doppelt → Helfer `showsGaps` in `lib/cloze.ts` (Mutante **2 failed**: Anzeige und Ansage); 🟢 Testkommentar zum Fokus berichtigt; 🟢 kein englischer Fall → Fall unter `ArcadeScope` (alle ohne Zählung).
- **2026-10-29** — Abnahme Lauf K, Teil 2: volle Suite **1017/1017**, Vitest **584/584**, `tsc -b` sauber, E2E **100 passed** (`exit=0`, nach der letzten Code-Änderung gestartet). Rollengang als E2E-Gesamtlauf (Browser-Extension nicht genutzt): Sohn über seine Arcade-Specs (Übung, Klausur, Buchstaben ordnen) plus die Komponentenfälle mit Knoten-Identität; Creator über `schnellanlage-ohne-dublette`; Vater Regressionszeuge. Was ein echter Screenreader ansagt, prüft keine Maschine – **Gerätecheck offen**: einmal mit NVDA/VoiceOver eine Übungsrunde und eine fortgesetzte Klausurfrage anhören. Reviews: `frontend-reviewer`, Delta-Review (ohne 🔴/🟡). **Abgenommen**, Commit siehe Protokoll ([Lauf K](../pm-sitzung-2026-10-29-nacht.md)).
- **2026-10-30** — Nachschau in Lauf L, Teil 1 (frischer `frontend-reviewer`, `git show 448785c -- frontend/`): **entgangen** → [B-339](B-339-fortsetzen-sagt-alte-frage-an.md) (🟡: `resume()` lässt `item` stehen, die Region sagt die schon beantwortete Frage als „Neue Aufgabe" an; der Test prüft nur den Endzustand). Geprüft: `TaskAnnouncer` immer im DOM, `spoken` mit derselben `showsGaps`-Rückfallregel wie die Anzeige; in beiden Seiten im Fragment über `view()` ohne `key`; alle Übergänge von Übung (Laden → Vorderseite → Rückseite → nächste Karte, `plan_inactive`, fertig/leer) und Klausur (Start, Weiter, Fehler, Ergebnis, Fortsetzen); `play.newTask` in `de.ts`/`en.ts`; je neuem Test die tötende Mutante. 🟢 ohne Story: der Wächter `liveRegions.test.ts` fängt eine Region hinter einem frühen `return <div …>` nicht (getragen von den Knoten-Identitäts-Tests).
