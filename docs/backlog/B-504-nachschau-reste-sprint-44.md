---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Lernstand Pager nach Kinderwechsel, Inventar-Satz ohne Bezug]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 44 in der Retro von Sprint 45)
unverifiziert: false
entgangen_bei: [B-499]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-12
---

# B-504 · Nachschau-Reste Sprint 44: Seitenzähler nach Kinderwechsel, stumme Zählabfrage, Inventar-Satz ohne Bezug

Nachschau von Sprint 44 (frischer `frontend-reviewer` an `a719ed7`, gelesen, nicht gefahren). Kein 🔴/🟡; drei 🟢:

1. `frontend/src/vater/VaterLernstand.tsx:58`: `skip` überlebt einen Kinderwechsel (kein `key={childId}`). Kind 1 auf
   Seite 2, Wechsel per URL zu Kind 2 mit 3 schwachen Wörtern → leere Seite, „es sitzt alles 👍". Praktisch nur per
   Hand-URL erreichbar. Fix: `skip` beim Wechsel auf 0 (Muster `prevOnlyWeak`) oder `key={childId}`.
2. `:92`: scheitert nur die Zählabfrage, steht dauerhaft der neutrale Satz ohne Fehler und ohne „Erneut laden"
   (`fehler={words.error ?? anyWord.error}`).
3. `insufficient_inventory` im Vater-Banner (`VaterShop.tsx` `ChildShopView`) steht über der Tabelle, „Davon …" hat
   kein Bezugswort, und dass die Anfrage offen bleibt, sagt niemand (Wortgleichheit: die Arcade-Fassung müsste mit).

## User Story

Als **Vater** möchte ich, dass der Lernstand und der Shop mir nur sagen, was für **dieses** Kind und **diese** Anfrage
stimmt, damit ich kein „es sitzt alles" für ein Kind mit schwachen Wörtern lese und nach einer gescheiterten Freigabe
weiß, was mit der Anfrage passiert ist.

## Ist-Stand am Code (2026-11-12)

- `frontend/src/vater/VaterLernstand.tsx:60` – `skip` ist State von `WeakWords`; zurückgesetzt wird er nur bei einem
  Filterwechsel (`:73-74`, `prevOnlyWeak`). `VaterLernstand` reicht `childId` ohne `key` weiter (`:51`), die Route
  `/vater/kind/:childId/lernstand` hängt dieselbe Instanz um.
- `:107-113` – der Leerfall hängt an `items.length === 0`, nicht an `total === 0`. Eine Seite jenseits des Bestands
  (Kind 2 hat 3 schwache Wörter, `skip` = 20) liefert `items: []`, `total: 3`; mit `wordsMeasured > 0` steht dann
  „Kein Wort unter 50 % – es sitzt alles. 👍" – falsch, und das ist der eigentliche Schaden.
- `:92` – `ListenAusfall` bekommt nur `words.error`. Scheitert `anyWord` (`:68`), ist `wordsMeasured` `undefined`
  (`:70`), der Leerfall sagt „Kein Wort unter 50 %." und nichts lädt die Zahl je nach (`onRetry` nur am Kasten der Liste,
  der ohne Fehler der Liste nicht erscheint).
- `frontend/src/vater/VaterShop.tsx:379-386` – `decide` reicht den Fehler von `approveActivation` an `useAction`
  (Formatierer `errorMessage`), also an `GERMAN_PROBLEM_TEXT.insufficient_inventory` (`lib/api.ts:188`): „Davon ist
  nicht mehr genug im Inventar." Der Banner steht über der Tabelle (`:397`), „Davon" zeigt auf nichts.
- Server: `ShopService.ApproveActivationAsync` (`ShopService.cs:342-347`) – bei zu wenig Inventar Fehler, die Anfrage
  bleibt `Pending` (Doku `:329`, Test `ShopFlowTests.cs:1088`). `CanReject` gilt für jede offene Anfrage
  (`ShopController.cs:47`) – „Ablehnen" ist für den Vater also gangbar.
- Das Kind erreicht denselben Code beim Einlösen (`MeController.cs:351`); dort entsteht keine Anfrage. Die Arcade liest
  `problem.insufficient_inventory` (`i18n/de.ts:335`, `en.ts:322`), wortgleich mit `lib/api.ts` (Tor in
  `arcadeSpielweg.test.tsx`).

## Die echte Lücke

Zwei Sätze behaupten mehr, als gemessen ist (Erfolg aus einer leeren *Seite*; neutraler Satz ohne Hinweis, dass die
Zahl fehlt), und ein Satz nennt weder Gegenstand noch Folge.

## Akzeptanzkriterien

1. Nach einem Kinderwechsel beginnt die Wortliste auf Seite 1; der Filter „nur schwache" bleibt.
2. „es sitzt alles" (und jeder Leerfall) steht nur, wenn der Bestand des Kindes leer ist – nie für eine leere Seite
   jenseits des Bestands.
3. Scheitert nur die Zählabfrage, steht ein Fehler mit „Erneut laden", der sie neu holt; scheitert die Liste selbst,
   nicht zwei Kästen.
4. Scheitert die Freigabe an zu wenig Inventar, nennt der Vater-Banner Artikel und Menge, sagt, dass die Anfrage offen
   bleibt, und nennt „Ablehnen" als Ausweg; Inventar und Anfragen darunter werden neu geladen, damit sie den wahren Stand
   zeigen.
5. Der geteilte Satz (Vater-Tabelle und Arcade de/en) hat ein Bezugswort und spricht niemanden mit „du" an.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Sprint 46 auf Zuruf)

1. **`skip` per Muster `prevChildId` zurücksetzen, nicht `key={childId}`** – wie B-502: der Filter überlebt den
   Wechsel. Kosten: eine zweite Zurücksetz-Zeile neben `prevOnlyWeak`.
2. **Die Regel an die Senke:** der Leerfall hängt an `total === 0`, nicht an der Länge der Seite. Damit ist AK 2 auch
   dann wahr, wenn ein anderer Weg `skip` jenseits des Bestands bringt (Reload nach einer Abnahme des Bestands). Kosten:
   eine leere Seite jenseits des Bestands zeigt keinen Satz, nur den Pager – ehrlich, nicht schön.
3. **Eigener Kasten für die Zählabfrage** („Die Angaben zum Lernstand konnten nicht geladen werden"), nur ohne Fehler
   der Liste – statt `words.error ?? anyWord.error`, das „Die Vokabeln" für eine Liste behauptete, die geladen ist.
4. **Der Vater-Satz entsteht an der Stelle der Freigabe** (`decide`), nicht in der geteilten Tabelle: nur dort sind
   Artikel und Menge bekannt, und nur dort bleibt eine Anfrage offen (beim Kind entsteht keine). Kosten: ein Code mit
   zwei Sätzen im Vater-Web – der geteilte bleibt für jeden anderen Weg.
5. **Geteilter Satz: „Von diesem Artikel ist nicht mehr genug im Inventar."** (en: „There isn't enough of this item
   left in the inventory.") – in beiden Tabellen gleich.

## Risiken

- Ein Test, der den alten Satz erwartet (`old-phrases.py` nach der Umformulierung).
- `useGemerkterFehler` hält den Fehler über einen Schlüsselwechsel nicht – der neue Kasten bekommt `schluessel={childId}`.

## Angriffsplan

Rein Frontend. 1) `VaterLernstand.tsx`: `prevChildId`, Leerfall an `total`, zweiter `ListenAusfall`. 2) `VaterShop.tsx`:
`ChildShopView` exportieren, Fehler bei der Freigabe übersetzen, Inventar bei Misserfolg neu laden. 3) Geteilter Satz
in `lib/api.ts`, `i18n/de.ts`, `i18n/en.ts`.

## Testweg

Vitest: `VaterLernstand.test.tsx` (AK 1–3), neues `VaterShop.test.tsx` (AK 4), `arcadeSpielweg.test.tsx` (AK 5, Tor
der Wortgleichheit läuft mit). Mutanten je Fall mit `mutant-probe.py`. Playwright: die Specs, die Shop und Lernstand
fahren, auf Ausweichports.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 44, Retro Sprint 45).
- **2026-11-12** — Sprint 46: ausformuliert gegen den Code, autonom gegrillt (`art: Defekt`), geschätzt S.
- **2026-11-12** — umgesetzt: `WeakWords` (`prevChildId`, Leerfall an `total`, Kasten der Zählung), `ChildShopView`
  (`decide` übersetzt `insufficient_inventory` beim Freigeben; bei Misserfolg Inventar und Anfragen neu), geteilter Satz
  „Von diesem Artikel …" in `lib/api.ts`, `i18n/de.ts`, `i18n/en.ts`. `old-phrases.py`: 0 Fundstellen.
- **2026-11-12** — `frontend-reviewer`: kein 🔴; drei 🟡, alle Tests: (1) AK 1 prüfte den Filter nur im Ausgangszustand
  (konnte nie fallen) → Filter vorher abgewählt; (2) Fixture `unitType: "Minutes"` (gültig ist `Minute`), der Satz las
  „30 undefined" → korrigiert, Menge zugesichert; (3) AK 5 („Bezugswort") nur als Kommentar → `toMatch(/Artikel/)` in
  `arcadeSpielweg.test.tsx`. Übernommen ohne Zählung: „weniger als N übrig" (genau die Server-Bedingung statt „keine N
  mehr"), Anfragen bei Misserfolg neu laden, `waitFor`. Geprüft: kein anderer Vater-Weg zu `insufficient_inventory`,
  „Ablehnen" ist für jede offene Anfrage da (`CanReject`), gelöschter Artikel liefert den Code nie. Rest → [B-505](B-505-leerfall-an-der-seite-statt-am-bestand.md)
  (acht Listen mit Leerfall an der Seite), bewusst nicht in diesem Sprint.
- **2026-11-12** — Rote Probe nach dem Review: S3 (Prüfung des Codes) war zuerst **grün** – die Gegenprobe warf einen
  einfachen `Error`, `instanceof ApiError` trug den Fall allein; jetzt `ApiError` mit `activation_not_pending`.
- **2026-11-12** — Mutanten nach der letzten Änderung (`mutant-probe.py`, an `4134209 + Arbeitsbaum`):

| Mutante | Fall | Stelle | gemessen |
|---|---|---|---|
| L1 | AK 1, `skip` beim Kinderwechsel | `VaterLernstand.tsx:77` | rot |
| L5 | AK 1/Entscheidung 1, Filter bleibt | `VaterLernstand.tsx:77` | rot |
| L2 | AK 2, Leerfall an `total` | `VaterLernstand.tsx:117` | rot |
| L3 | AK 3, Kasten der Zählung fehlt | `VaterLernstand.tsx:98` | rot |
| L4 | AK 3, zwei Kästen | `VaterLernstand.tsx:98` | rot |
| S1 | AK 4, Übersetzung fehlt | `VaterShop.tsx:387` | rot |
| S2 | AK 4, auch beim Ablehnen | `VaterShop.tsx:387` | rot |
| S3 | AK 4, ohne Prüfung des Codes | `VaterShop.tsx:387` | rot |
| S4 | AK 4, Inventar nicht neu | `VaterShop.tsx:396` | rot |
| S5 | AK 4, Anfragen nicht neu | `VaterShop.tsx:396` | rot |
| S6 | AK 4, Menge falsch formuliert | `VaterShop.tsx:389` | rot |
| A1 | AK 5, „Davon" zurück | `lib/api.ts:189` | rot |

Grundlinie nach dem Zurückspielen grün.

- **2026-11-12** — **abgenommen.** Vitest **796/796**, `tsc -b` sauber, Playwright **6/6** (`full-flow`, `freigabe`,
  `shop-verlauf`, `shop-verlauf-wiederholen`, `vater-von-null`; Ausweichports 5201/5174, Server vom Lauf gestartet).
  **Review-Zähler Sprint 46: 3.** Rollengang als Maschinengang: Vater über Komponententests (keine Spec fährt eine
  Freigabe, die am Inventar scheitert, oder den Kinderwechsel im Lernstand); **nicht im Browser angesehen.** Commit `3618e63`.
- **2026-11-12** — Nachschau (Retro Sprint 47): ein 🟡, drei 🟢 → [B-509](B-509-lernstand-kopfzahl-und-zeilen-des-vorigen-kindes.md).
