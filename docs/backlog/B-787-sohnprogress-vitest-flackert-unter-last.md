---
tags: [typ/story, status/abgenommen, bereich/test, rolle/entwickler]
aliases: [SohnProgress-Vitest flackert]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
nachgeschaut: "2026-11-22"
quelle: "Nachtlauf B 2026-11-22, voller Vitest-Lauf bei B-603 (docs/pm-sitzung-2026-11-22-b.md)"
grund: ""
ersetzt_durch: []
---

# B-787 · `SohnProgress.test.tsx` flackert im vollen Vitest-Lauf

Die Story B-603 hat das Flackern in `SohnProgress.test.tsx` bewusst zurückgestellt (Entscheidung 4). Begründung: Es gab
keinen Fallnamen. Jetzt gibt es einen: Im vollen Lauf am 2026-11-22 (Worktree von Lauf B, B-603 schon umgestellt) fiel

`SohnProgress mit einer gemerkten Plan-Id, die nicht mehr gilt > nimmt den spielbaren Plan und merkt ihn sich`
(`frontend/src/sohn/SohnProgress.test.tsx:66`)

Der Lauf endete 1 failed / 1303 passed. Allein lief die Datei dreimal 9/9 grün, zwei weitere volle Läufe 1304/1304.

**Fehlermeldung nicht mitgeschnitten:** Der Lauf ging durch `grep`. Wer das nächste Rot sieht, schreibt den Lauf in eine
Datei und trägt die Zeile hier ein.

Vermutung, nur gelesen: Der Fall prüft direkt nach `findByText(/Plan 5/)`, ohne `waitFor`. Gemeint sind
`overview`-Aufrufe und `setPlanId` (`:72-73`). `setPlanId` kommt aus einem `useEffect` in `useChosenPlan`
(`if (chosen && chosen.id !== planId) setPlanId(chosen.id)`). Das ist dasselbe Muster wie bei B-603: ein Zeitpunkt wird
geprüft statt eines Zustands. Ob es wirklich die Ursache ist, entscheidet erst die Fehlermeldung.

## Ist-Stand am Code (reproduziert 2026-11-22, Lauf B)

Drei volle Vitest-Läufe in Dateien (`vitest run > log`) blieben 1327/1327 grün. Das Rot ließ sich aber deterministisch
herstellen: eine Pause von 50 ms (`await act(() => new Promise(r => setTimeout(r, 50)))`) direkt nach `renderProgress()`
in der Fassung von `ec0c705a`. Ergebnis 2 failed / 7 passed:

```text
× nimmt den spielbaren Plan und merkt ihn sich 4111ms
× bleibt beim gemerkten Plan, auch wenn ein anderer spielbar ist 4082ms
TestingLibraryElementError: Found multiple elements with the text: /Plan 5/
TestingLibraryElementError: Found multiple elements with the text: /Plan 99/
```

Ursache: Mit zwei Plänen zeigt `SohnProgress` die Planwahl (`role="group"`, Knöpfe „Plan 4“/„Plan 5“). Die Zusammenfassung
von `ProgressForPlan` („Plan 5 · 0/7 Tage geschafft …“, `progress.summary`) trägt denselben Titel. `findByText(/Plan 5/)`
ist nur grün, wenn die Suche zwischen dem Knopf und der Zusammenfassung zum ersten Mal fragt. Kommt sie später (Last),
findet sie zwei Treffer, wiederholt bis zum Zeitlimit und fällt. Die Vermutung aus der Idee (`setPlanId` ohne `waitFor`)
ist damit widerlegt; die Zeitspanne von gut 4 s im Bericht passt zur Wiederholung bis zum Limit. Der dritte Fall der
Gruppe (`Plan 99`) hatte dieselbe Lücke, nur trat sie im Lauf vom 2026-11-22 nicht auf.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf B 2026-11-22 (Defekt, autonom grillbar), unbestätigt.*

1. **Die Suche zielt auf die Zusammenfassung, nicht auf den Titel allein:** `/Plan 5 ·/` bzw. `` `Plan ${STALE} ·` ``.
   *Begründung:* Der Fall meint „der Weg des gewählten Plans steht da“, und das zeigt nur die Zusammenfassung; der Knopf
   steht schon, bevor `overview` antwortet. *Kosten:* Hängt die Zeile am Format von `progress.summary` (Trenner „·“); ändert
   sich das Format, fällt der Fall laut statt still. *Verworfen:* `findAllByText` (prüfte weiter einen Zeitpunkt) und eine
   Suche über `role="group"` ausschließen (umständlicher, gleiches Ergebnis).
2. **Kein Produktcode.** Die Komponente tut das Richtige; nur die Abfrage war mehrdeutig.

## Akzeptanzkriterien

1. Mit 50 ms Pause nach dem Rendern in allen acht Fällen, die direkt nach `renderProgress()` eine Leerzeile haben, bleibt
   die Datei 9/9 grün. *Rot auf `ec0c705a` (2 failed, Meldung oben).*
2. Die Fälle tragen weiter ihr Rot: ohne den Zweig „spielbar“ in `useChosenPlan` fällt „nimmt den spielbaren Plan“, ohne den
   Zweig „gemerkt“ fällt „bleibt beim gemerkten Plan“.

## Schätzung

**XS** – zwei Zeilen Testcode, eine Datei. `wo: frontend`, `migration: nein`, `vertragsbruch: nein`.

## Verlauf

- **2026-11-22** — angelegt (Nachtlauf B, Fallname ohne Meldung).
- **2026-11-22** — ausformuliert, gegrillt, geschaetzt (Nachtlauf B 2026-11-23, Sprint B1; Defekt, autonom): Rot mit
  eingeschleuster Pause reproduziert; der Treffer der Pause ist durch die Meldung „Found multiple elements“ zugesichert,
  nicht nur durch das Rot. Drei volle Läufe ohne Pause: 1327/1327, 1327/1327, 1327/1327.
- **2026-11-22** — in-arbeit: AK 1, Pause in 8 Fällen → `SohnProgress.test.tsx` 9/9 grün (erwartet grün, gemessen grün);
  auf `ec0c705a` mit derselben Pause 2 failed / 7 passed (erwartet rot, gemessen rot, Fälle `:66` und `:80`).
  AK 2, Mutanten-Probe (`.claude/scripts/mutant-probe.py`, nach der letzten Änderung):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | M1-spielbar (`?? plans.data.find((p) => p.isPlayable)` gestrichen) | frontend/src/sohn/useChosenPlan.ts:18 | red | red |
  | M2-gemerkt (`find((p) => p.id === planId) ??` gestrichen) | frontend/src/sohn/useChosenPlan.ts:18 | red | red |

- **2026-11-22** — Review `frontend-reviewer`: keine Funde. Nebenbefund: die alte Suche löste schon der Knopf allein auf; erst
  die neue prüft wirklich, dass der Titel aus der Antwort für Plan 5 kommt. Suche nach derselben Fehlerklasse (Titel neben
  gleichnamigem Knopf) in allen `*.test.tsx`: keine weitere Stelle.
- **2026-11-22** — abgenommen (7b501ed4). Rollengang Sprint B1 als Maschinengang: E2E `full-flow`, `test-durchgefallen-ueben`, `erfolge-woche-frist`, `karten-umdrehen` auf :5207/:5179 gegen eine Wegwerf-DB, 4 passed; Vitest voll 1327/1327. Sohn: Klausur und Erfolge laufen wie zuvor (kein sichtbarer Unterschied – reine Test-/Abhängigkeitsänderung); Vater und Creator: Regressionszeugen, kein Pfad im Diff.

- **2026-11-22** — Nachschau (Retro Sprint B2): `git show 7b501ed4`: nur `SohnProgress.test.tsx` und die Story, keine eingeschleuste Pause im Commit (0 Zeilen `setTimeout`); Vitest voll nach Sprint B2 1327/1327; kein Befund.
