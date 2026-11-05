---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Sprachwechsel behält Store-Bindung]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-x
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-05"
---

# B-396 · Wer im Übersetzungs-Editor nur das Sprachpaar ändert, behält die Bindung an die Vokabel des alten Sprachpaars

`frontend/src/vater/exerciseConfig.tsx:294` behält die `vocabularyId` eines Paars, solange Quelle und Ziel unverändert sind – auch wenn
`sourceLang`/`targetLang` der Übung geändert wurden. Das Paar zeigt dann auf einen Speicher-Eintrag des alten Sprachpaars. Zu klären:
ob ein Sprachwechsel alle Bindungen lösen soll (dann legt der Server seit B-392 neu an bzw. findet), und ob der Server das prüfen soll
(B-387 hat die Sprachpaar-Prüfung bewusst ausgelassen – dort für Birkenbihl).

## User Story

Als **Creator** möchte ich das Sprachpaar einer Übersetzungs-Übung ändern können, ohne dass ihre Paare weiter auf Vokabeln des alten
Sprachpaars zeigen.

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** Ein Wechsel von Ausgangs- **oder** Zielsprache löst die Bindung aller geladenen Paare, genau wie eine
   Textänderung; der Server findet bzw. legt im neuen Sprachpaar an (seit B-392). Jede geladene Zeile merkt sich das Sprachpaar, wie der
   Editor es zeigt – mit dem Rückfall `en`/`de` für eine Altübung ohne Sprachen, sonst löste schon Öffnen+Speichern die Bindung.
2. **Entscheidung im Auftrag des Nutzers, getroffen von Claude** (Auftrag im Dialog am 2026-11-05: „kläre Fragen selber"): **keine**
   Sprachpaar-Prüfung am Server. Begründung: dieselbe Abwägung wie bei B-387 (dort bewusst ausgelassen); ein Agent, der eine fremde
   Bindung schickt, schickt eine gültige Id – die Prüfung wäre eine neue Regel für alle Wege, nicht die Reparatur dieses Klickwegs.
   *Kosten:* ein API-Client kann weiter eine Vokabel eines anderen Sprachpaars binden.

## Akzeptanzkriterien (je Fall)

| Fall | Erwartet | Beleg |
|---|---|---|
| nur Ausgangssprache geändert | Bindung `null` | `exerciseConfig.test.ts` „Wechsel der Quellsprache" |
| nur Zielsprache geändert | Bindung `null` | „Wechsel der Zielsprache" |
| Altübung ohne Sprachen öffnen + speichern | Bindung bleibt | „Öffnen und Speichern einer Übung ohne Sprachen" |
| nichts geändert | Bindung bleibt | „Alternativen bleiben Listen und die Store-Bindung bleibt erhalten" (**Gegenprobe**) |
| Klickweg im Bearbeiten-Dialog: Ausgangssprache en→fr, „Inhalt speichern" | Satz steht danach im Store unter `fr→de` | `e2e/uebersetzung-sprachwechsel.spec.ts` |

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf W, Sprint 9 (Lauf X, Retro Sprint 10; frischer `pugling-reviewer`, 🟢). Älter als
  B-392, darum ohne `entgangen_bei`; am Code gelesen, nicht im Browser gemessen (`unverifiziert: true`).
- **2026-11-05** — gegrillt, geschätzt und gebaut in Lauf Y, Sprint 13. **Rote Probe** gegen den Ausgangszustand: die zwei Wechsel-Fälle –
  erwartet 2 rot, gemessen 2 rot („expected 7 to be null"). **Mutanten:** gemerktes Sprachpaar ohne Rückfall → rot nur der Altübungs-Fall;
  Zielsprachen-Vergleich entfernt → rot nur der Zielsprachen-Fall; Sprach-Vergleich ganz entfernt → E2E rot an der Schlusszusicherung
  (`fr→de` fehlt). Vitest 651/651, `tsc` sauber.
- **2026-11-05** — Review `frontend-reviewer`: kein 🔴/🟡. Geprüft: Anlegen und neue Zeile (ohne `orig*` → nie gebunden), Rückfall gleich
  der Anzeige, mehrfaches „Inhalt speichern" im Dialog ohne Neuladen der Zeilen (B-210) – die gemerkten Werte bleiben untereinander
  stimmig. 🟢 zwei Spec-Konventionen (`exact: true`, `toHaveCount(1)` statt `.first()`) – nachgezogen, ohne Zählung. 🟢 **bekannte
  Grenze:** eine Altübung ohne Sprachpaar bekommt beim Speichern en→de festgeschrieben und behält ihre Bindung, auch wenn der Eintrag
  in einem anderen Paar liegt – im Frontend nicht erkennbar, dieselbe Folge der Entscheidung 2 (keine Server-Prüfung).
- **2026-11-05** — **abgenommen.** Rollengang Creator als E2E: `e2e/uebersetzung-sprachwechsel.spec.ts` fährt den echten
  Bearbeiten-Dialog (Ausgangssprache en→fr, „Inhalt speichern") und misst am Store – 1 passed nach dem letzten Edit; volle Playwright-Suite
  116 passed (Vater und Sohn als Regressionszeugen, kein Pfad von ihnen berührt). Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `609b754`.
- **2026-11-05** — **Nachschau** (Lauf Z, Retro Sprint 14; frischer `frontend-reviewer`, `git show 609b754`): geprüft (1) rote Probe und die
  drei Mutanten am Code nachgerechnet – plausibel; (2) Kommentare gegen Code, Mehrfach-Speichern im Dialog; (3) andere Zeilen-Bauer
  (Anlegen nur über `emptyRow`, kein Kopieren/Import, Birkenbihl ohne Bindung); (4) Vokabel-Typ mit eigenen Sprachfeldern – kein
  Geschwister-Defekt (Einträge sind die Store-Vokabeln selbst). **Entgangen (🟡):** die Gegenprobe baut ihre Zeile von Hand, kein Fall lädt
  ein Paar ≠ en/de zurück – eine fest verdrahtete `origSourceLang: "en"` überlebt alle Tests, und eine `fr→de`-Übung verlöre beim bloßen
  Speichern ihre Bindung → [B-400](B-400-uebersetzung-sprachpaar-rueckweg-ungetestet.md). 🟢 eine ältere Verlaufszeile war beim Umstellen
  des Frontmatters mitersetzt worden – zurückgestellt.
