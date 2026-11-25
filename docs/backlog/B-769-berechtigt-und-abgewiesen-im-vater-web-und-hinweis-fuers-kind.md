---
tags: [typ/story, status/abgenommen, bereich/training, bereich/gamification, rolle/supervisor, rolle/student, bereich/frontend]
aliases: [Meldung berechtigt Oberfläche]
status: abgenommen
prio: P2
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "B-627"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-769 · Berechtigt und abgewiesen im Vater-Web, Hinweis für das Kind, Beschriftung der Gegenbuchung

Die Oberfläche zu [B-627](B-627-berechtigte-meldung-bucht-nach.md): zwei Knöpfe an der Meldung des Vater-Webs, ein Hinweis an das Kind, der sagt, was nicht neu bewertet wird, und die richtige Beschriftung einer positiven Gegenbuchung.
**Baut nach B-767 und B-768.**

## User Story

Als **Supervisor** möchte ich an der Meldung „berechtigt“ oder „abgewiesen“ wählen; als **Student** möchte ich wissen, was die Bestätigung geändert hat und was nicht.

## Ist-Stand am Code

- Vater-Seite der Meldungen: `frontend/src/vater/VaterRueckmeldungen.tsx`, Panel am Ersteller: `ExerciseFeedbackPanel.tsx`; E2E `frontend/e2e/rueckmeldung.spec.ts`.
- Das Kind sieht den Hinweis nach „erledigt“ genau einmal (`frontend/src/sohn/KartenMeldung.tsx`, Test `ExerciseFeedbackTests.Kind_sieht_den_Hinweis_nach_erledigt_genau_einmal`).
- Beschriftung der Art `GoalPenalty`: `frontend/src/lib/labels.ts:19`, `i18n/de.ts:173`, `i18n/en.ts:161` („Pflicht gerissen (Malus)“) – unabhängig vom Vorzeichen.
- **Fremde, ungecommittete Arbeit am 2026-11-20:** `frontend/src/sohn/KartenMeldung.test.tsx` und weitere Dateien unter `frontend/src/sohn/` (LearningDad); den Kind-Hinweis erst nach dem Committen dieser Arbeit anfassen.

## Die echte Lücke

Knöpfe, Hinweis und Vorzeichen-Beschriftung; das Backend liefert die Wege.

## Entscheidungen

*Aus B-627 abgeleitet, stellvertretend, unbestätigt (Vorbereitungslauf 2026-11-20, Sonnet).*

1. **„Berechtigt“ nur bei einer Meldung mit Bezug** (B-627 Entscheidung 1): ohne Bezug bleibt nur „Erledigt“ (B-627 Entscheidung 2: Üben ohne gespeicherte Antwort).
2. **Der Hinweis an das Kind nennt, was nicht nachgebucht wird** (B-627 Entscheidung 7): Kasten, Missionen und eine geöffnete Kiste. *Kosten:* ein weiterer Satz.
3. **Ein positiver `GoalPenalty`-Eintrag heißt anders als der Malus** („Malus zurückgenommen“), nach Vorzeichen (B-627 Entscheidung 3).

## Akzeptanzkriterien

Neu aus den Entscheidungen; B-627 hatte keine Oberflächen-Kriterien.

1. Eine Meldung mit Bezug zeigt „Berechtigt“ und „Abgewiesen“, eine ohne Bezug nur „Erledigt“. *Rot, wenn der Knopf auch ohne Bezug erscheint (Vitest an `VaterRueckmeldungen`).*
2. Die Knöpfe sind beim Senden gesperrt (zweimal klicken sendet einmal). *Rot, wenn `disabled={busy}` fehlt.*
3. Der Hinweis an das Kind nach „berechtigt“ nennt, dass Kasten und Missionen nicht neu gebucht werden. *Rot, wenn der Satz fehlt.*
4. Ein positiver Eintrag der Art `GoalPenalty` erscheint als „Malus zurückgenommen“, ein negativer weiter als „Pflicht gerissen (Malus)“. *Rot, wenn beide gleich beschriftet sind.*

## Schätzung

**Größe: S** – wie `childId` aus dem Test-Pfad ziehen (B-01): zwei Knöpfe, ein Satz, eine Beschriftung nach Vorzeichen.

- **`wo: frontend`, `migration: nein`, `vertragsbruch: nein`** (alle Wege kommen aus B-767 und B-768).
- **Risiken:** (1) die Beschriftung hängt heute nur an der Art (`labels.ts`), nicht am Vorzeichen: die Kontoansicht muss den Betrag mitgeben; (2) fremde Arbeit an `frontend/src/sohn/`.

### Angriffsplan

1. Typen aus dem erzeugten Vertrag; i18n Deutsch und Englisch.
2. Knöpfe in `VaterRueckmeldungen` (`useAction`, `StatusBanner`), Hinweistext in `KartenMeldung`, Beschriftung nach Vorzeichen in der Kontoansicht.
3. Vitest, E2E, `frontend-reviewer`.

### Testweg

- `VaterRueckmeldungen.test.tsx` (neu, falls nicht vorhanden): AK 1, 2; `KartenMeldung.test.tsx`: AK 3; Test an der Kontoansicht: AK 4.
- E2E `frontend/e2e/rueckmeldung.spec.ts` um den Weg „Test falsch gewertet, Meldung, berechtigt, Kontostand“ erweitern; das ist der Rollengang als Vater und als Sohn.
- Vor der Abnahme: `frontend-reviewer`.

## Verlauf

- 2026-11-20 · aus B-627 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): S, frontend, keine Migration, kein Vertragsbruch. Baut nach B-767 und B-768; fremde Arbeit an `frontend/src/sohn/` abwarten.
- **2026-11-24** — in Arbeit (Nachtlauf K, Sprint 151). Die fremde Arbeit an `frontend/src/sohn/` vom 2026-11-20 ist
  längst committet (Arbeitsbaum sauber bei 5993c86b). Gebaut: `VaterRueckmeldungen` – an einer Meldung mit Test-Antwort
  „Berechtigt“ (mit Rückfrage, endgültig) und „Abgewiesen“, sonst „Erledigt“; „im Test als falsch gewertet“ an der Antwort;
  an einer erledigten Zeile das Urteil, „Wieder öffnen“ nicht bei „berechtigt“. `MeldungsHinweis` nennt nach „berechtigt“
  Kasten, Missionen und eine geöffnete Kiste. `ledgerLabelKey`/`pointKindLabel(kind, amount)`: ein positiver
  `GoalPenalty` heißt „Malus zurückgenommen“ (Vater-Konto und Sohn-Konto, i18n de/en). Deutsche Texte für die zwei neuen
  409-Codes in `GERMAN_PROBLEM_TEXT`.
- AK 1 → `VaterRueckmeldungen.test.tsx` „zeigt Berechtigt und Abgewiesen nur an einer Meldung mit Test-Antwort“.
- AK 2 → „sperrt die Knöpfe, solange das Urteil gesendet wird“ (`disabled` im Ladefenster, ein Aufruf mit `Justified`).
- AK 3 → `KartenMeldung.test.tsx` „nennt nach berechtigt, was nicht nachgebucht wird“ (und nicht nach „abgewiesen“).
- AK 4 → `labels.test.ts` „Malus nach Vorzeichen“ (beide Helfer).
- Endgültig → „bietet an einer berechtigten Meldung kein Wieder öffnen an“.
- **Review-Funde (`frontend-reviewer`):** 🔴 die zwei neuen 409-Codes hatten keinen deutschen Text (Tor
  `ProblemTextGuardTests`) → ergänzt; 🟡 ein „Abbrechen“ der Rückfrage war ungeprüft → Fall „sendet nach Abbrechen der
  Rückfrage nichts“; 🟡 der AK-2-Fall verließ sich auf das `confirm` der Testumgebung → `vi.stubGlobal`. Alle behoben,
  gezählt. 🟢 „Berechtigt“ auch an einer richtig gewerteten Antwort → so belassen (Vertrag, B-767), der Text sagt dann
  „nachgerechnet“, obwohl nichts zu rechnen war.
- Mutanten-Probe (Vitest, `VaterRueckmeldungen`/`KartenMeldung`/`labels`), gemessen an `5993c86b + Arbeitsbaum` nach dem
  letzten Edit, 2026-11-24 23:15; Grundlinie danach grün.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | F1 Urteil ohne Bezug | VaterRueckmeldungen.tsx:122 | red | red |  |
  | F2 Berechtigt nicht gesperrt | VaterRueckmeldungen.tsx:125 | red | red |  |
  | F3 Rueckfrage ignoriert | VaterRueckmeldungen.tsx:54 | red | red |  |
  | F4 berechtigt wieder oeffnbar | VaterRueckmeldungen.tsx:134 | red | red |  |
  | F5 Hinweissatz immer | KartenMeldung.tsx:160 | red | red |  |
  | F6 Vorzeichen ignoriert | labels.ts:34 | red | red |  |

- **2026-11-24** — Rollengang Sprint 151: `e2e/meldung-berechtigt.spec.ts` (Ausweichports 5221/5191, Server von Playwright
  nach dem letzten Edit gestartet) – Sohn verliert den Test an „eastward“, meldet „zu streng“ vom Ergebnisblatt; Vater sieht
  „im Test als falsch gewertet“, nur „Berechtigt“/„Abgewiesen“, bestätigt die Rückfrage; vorher keine Ziel-Belohnung,
  danach genau eine (15); die erledigte Zeile trägt „berechtigt“ ohne „Wieder öffnen“; der Sohn liest den Satz über Kasten
  und Missionen. Dazu `e2e/rueckmeldung.spec.ts` grün (2 passed). Migrationsprobe an einer Kopie der echten DB
  (`kopie-server.sh`, :5222): beide Migrationen eingespielt, Zeilenzahlen und Salden gleich, keine Spalte gefüllt.
  Verifikation: .NET 2356/2356 (Release), Vitest 1448/1448, `tsc` sauber, `dotnet format` gelaufen. Reviewer:
  `pugling-reviewer` und `frontend-reviewer`. Commit `fcb04f56`. **Abgenommen.** Die Entscheidungen bleiben „stellvertretend,
  unbestätigt“ (Vorbereitungslauf 2026-11-20) – zum Umstoßen im Protokoll.
