---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: []
status: abgenommen
prio: P1
art: Defekt
groesse: S
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-25-o.md#sprint-191--ziel--umfang
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-1020 · Die Leitplanken-Maske verliert nach dem Speichern ihre Bestätigung, und kein Test prüft das Zurücklesen

Fund des `frontend-reviewer` im Sprint 191 (B-731, Nachtlauf O), nicht mehr behoben, weil der Fehlerzähler den Lauf beendete.
`LeitplankenSection` hängt per `key` an den fünf Serverwerten (`frontend/src/vater/VaterKind.tsx`); nach dem Speichern lädt
die Seite das Kind neu, der Schlüssel ändert sich, die Sektion wird neu eingehängt – und die Meldung „Leitplanken
gespeichert.“ bzw. „Leitplanken entfernt.“ verschwindet nach wenigen Millisekunden (die Datei warnt selbst davor, Z. 57–61).
Die E2E `frontend/e2e/lerntage-und-pause.spec.ts` prüft genau diese Meldung und ist damit wackelig. Zweitens: kein Test wird
rot, wenn die Maske nach dem Speichern den getippten statt den gelesenen Wert zeigt (der Mock antwortet mit den getippten
Werten; „entfernen“ ist ungetestet). Drittens: der Knopf heißt wie zwei andere „Speichern“ im selben Reiter, die E2E wählt ihn
per `.last()`. Reparaturweg laut Review: Muster `CoreDataSection` (`runFor`, Felder aus der Antwort setzen, `key` nur am Kind),
Mock mit abweichendem Serverwert, Knopf „Leitplanken speichern“.

## Entscheidungen

1. `key` nur am Kind (wie `BonusSection`); die Felder ziehen den Serverstand per Effekt nach, nur wenn sich die fünf
   Serverwerte ändern – ein Neuladen aus anderem Grund überschreibt keine Eingabe.
2. Pausen und Leitplanken stehen im eigenen Reiter „📅 Lerntage“ (`?bereich=lerntage`); der Knopf bleibt „Speichern“, weil er
   dort der einzige ist (statt der Umbenennung aus dem Review).
3. Eine Gruppenüberschrift mit **einem** ⓘ über den fünf Feldern; „Leitplanken entfernen“ fragt vorher nach.

## Akzeptanzkriterien

1. Nach dem Speichern steht „Leitplanken gespeichert.“ auch nach dem Neuladen des Kindes, und die Felder zeigen den Serverstand.
2. Je Reiter höchstens ein „Speichern“ für diese Maske; die E2E wählt den Knopf über den Namen.

## Verlauf

- **2026-11-25** — angelegt im Nachtlauf O (Sprint 191) aus dem Review von B-731; der Lauf endete am Fehlerzähler, bevor der
  Fund behoben werden durfte.
- **2026-11-26** — `idee` → `in-arbeit` im Merge-Review von M/N/O (Branch `merge-1125`) auf Anordnung des Nutzers. Belege:
  - AK 1 → `frontend/src/vater/LerntagePanels.test.tsx` „zieht nach dem Speichern den Serverstand nach …“ (Server meldet
    einen anderen Deckel als getippt); rote Probe Effekt entfernt → rot. Kind-Hub-Schlüssel →
    `frontend/e2e/lerntage-und-pause.spec.ts` wartet auf das `GET` des Kindes; rote Probe alter Werte-`key` → rot an
    `Leitplanken gespeichert.`.
  - Rückfrage → „fragt vor dem Entfernen nach …“, ein ⓘ → „trägt genau einen Hinweis-Knopf …“ (je rote Probe gemessen).
  - Restbefund, nicht Teil dieser Story: unter „Einstellungen“ stehen weiter zwei „Speichern“ (Bonus-Deckel und Turm, B-851).
  - Commit: `48550d7c`. Offen für `abgenommen`: `frontend-reviewer` über den Fix.
  - Verifikation nach der letzten Code-Änderung: Backend-Suite 2607/2607 (Release), `vitest` 1512/1512, `tsc -b` grün, E2E
    `lerntage-und-pause`, `sparziel`, `lob`, `gleichung-loesen`, `pfad`, `bonus-deckel`(+`-voll`), `uebungstypen` 8 passed
    (Ports 5244/5284, Wegwerf-DB).
- **2026-11-26** — Sprint 192 (`docs/pm-sitzung-2026-11-26.md`): `frontend-reviewer` über `48550d7c` fand zwei Punkte, beide
  behoben. (1) Die Gruppe hieß für Screenreader „Leitplanken Erklärung zu …“, weil die `id` an der ganzen Zeile samt ⓘ saß
  (B-236) – jetzt am Wort; Test `LerntagePanels.test.tsx` „nennt die Gruppe nur ‚Leitplanken‘“, rote Probe von Hand mit dem
  alten Markup → rot (die einzeilige Mutante mit doppelter `id` blieb grün, happy-dom nimmt die zweite – kein Beleg).
  (2) Die E2E wartete auf die `GET`-Antwort, nicht auf den Render; jetzt zuerst auf „Leitplanken entfernen“, das erst mit dem
  neu geladenen Kind erscheint – im selben Render, in dem ein Werte-Schlüssel den Banner mitnähme. Aufräum-Vorschläge
  (eslint-disable, feste `id`, doppelter Titel) nicht gebaut.
- **2026-11-26** — `in-arbeit` → `abgenommen` (Sprint 192, `docs/pm-sitzung-2026-11-26.md`). Verifikation am Endstand `4e70c583`: Backend-Suite 2609/2609 (Release), `vitest` 1514/1514, `tsc -b` grün, E2E
  `lerntage-und-pause` + `pfad` 2 passed (Ports 5244/5284). Reviews: `pugling-reviewer` (zweimal, zuletzt ohne Fund),
  `frontend-reviewer` (zwei Funde, behoben). Rollengang an einer Kopie der echten DB (`kopie-server.sh`, `:5201`, Server nach
  der letzten Änderung gestartet): Kind-Hub-Reiter „Lerntage“ per E2E `lerntage-und-pause`: Banner bleibt stehen, Felder zeigen den Serverstand, Knopf per Name. Commits `48550d7c`, `4e70c583`.
