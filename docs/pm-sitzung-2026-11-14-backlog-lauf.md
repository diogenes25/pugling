---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Backlog-Lauf als PO und Stakeholder

**Datum:** 2026-11-14  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (Friedrich/Theodor, Student) · Entwickler
**Ziel:** Offene Stories im Index um mindestens 30 % senken (alles im Index gezählt), mindestens 30 % von N in Sprints
eingearbeitet; Stakeholder-Rolle beim Grillen ausdrücklich übertragen, auch für `Wunsch` und `Frage`.

## Auftrag und Ausgangswert

- Branch `auto/backlog-lauf-2026-11-14` im Worktree `.claude/worktrees/backlog-lauf-1114`, abgezweigt von `main`
  (`40dd9bfa`). Der Haupt-Checkout steht auf `auto/fertigstellung` (`701aedba`), das ein Vorfahre von `main` ist –
  `main` trägt also schon alles; abzweigen vom älteren HEAD hätte 13 Commits verloren.
- `bash .claude/scripts/backlog-index.sh` auf `main`: **N = 35 offen** (473 abgenommen, 34 verworfen).
- Zielmarken: offen am Ende **≤ 24** (0,7 × 35 = 24,5); eingearbeitet **≥ 11** (30 % von 35 = 10,5).
- Entscheidungen, die ich als Stakeholder treffe, tragen den Vermerk „Stakeholder-Entscheidung im Auftrag des
  Nutzers". Frühere ausdrückliche Entscheidungen des Nutzers (z. B. B-535, gegrillt im Chat 2026-11-13) bleiben.

## Abgleich gegen den Code

Vier Agenten haben die 32 Stories außerhalb von B-534…B-536 parallel gegen den Code geprüft (B-534…B-536 selbst:
B-534 ist gebaut und reviewt und braucht nur die Abnahme; B-535 ist vom Nutzer gegrillt; B-536 ist groß und wird
gegrillt). Ergebnis: **keine Story erledigt, doppelt oder eine Sammel-Story** – es gab also nichts mit Beleg zu
verwerfen. Vierzehn trugen veraltete Zeilenangaben oder Zahlen und sind berichtigt:

| Urteil | Stories |
| --- | --- |
| gilt | B-39, B-218, B-220, B-248, B-249, B-410, B-545, B-09, B-20, B-219, B-221, B-250, B-251, B-252, B-376, B-377, B-527, B-546 |
| veraltet, Kern gilt (Ist-Stand berichtigt) | B-31, B-469, B-423, B-540, B-407, B-542, B-223, B-16, B-122 (sechs statt sieben Listen), B-140, B-369, B-459 (eingeengt auf `fill --images`), B-47, B-380 |

Folge für das Ziel: die −30 % kommen nicht aus dem Aufräumen, sondern aus gebauter Arbeit. Bei 35 offenen Stories
braucht es netto −11; mit einem Puffer von einer neuen Story je Sprint aus Reviews sind das vier Sprints à 4–6.

### Zurückgestellt (nicht in diesem Lauf)

- **B-31** – wartet auf ein echtes Handy (Klang/Haptik); kein Code.
- **B-47** – wartet auf den Neubau des Deploys (Azure stillgelegt).
- **B-09, B-39, B-221, B-248, B-249, B-251, B-376, B-377** – L/XL, neue Fähigkeiten mit Migration; zu groß für
  diesen Lauf, Teilung erst bei Bedarf.
- **B-218, B-220, B-250** – hängen aneinander bzw. an B-248 (Queue, Stundenplan, Klassenarbeit).
- **B-410** – ein neuer reflexiver Wächter: würde den Lauf nach Regel anhalten; bleibt dem Menschen.

## Refinement

Drei Agenten treiben je vier bis fünf Kandidaten bis `geschaetzt` (nur Doku, getrennte Dateien), mit der
übertragenen Stakeholder-Rolle: Vater-UI (B-540, B-16, B-140, B-380), Sohn/Belohnung (B-545, B-527, B-252, B-542),
Creator/Entwickler (B-122, B-459, B-20, B-469, B-423). B-535 war geschätzt; B-546 trat beim Bau von B-535 erneut auf
und wurde sofort ausformuliert, gegrillt (`Aufräumen`, autonom) und geschätzt (XS).

Ergebnis (alle bis `geschaetzt`, Entscheidungen als „Stakeholder-Entscheidung im Auftrag des Nutzers“ markiert):
B-540 (S), B-16 (M), B-140 (M), B-380 (M), B-545 (S), B-527 (M), B-252 (S), B-542 (XS), B-122 (S), B-459 (XS),
B-20 (M), B-469 (S), B-423 (M). Ich habe B-536 selbst gegrillt (fünf Empfehlungen übernommen, XL); es geht als Karte
in den Dialog. Zwei Befunde beim Belegen: B-252 braucht ein Vertragsfeld (die Karte kennt ihre Sprache nicht), B-527
findet die Kiste heute in keiner Oberfläche – das Kind sieht sie nur im Kontoauszug.

## Sprint 73 — Ziel & Umfang

**Sprint-Ziel (Sohn):** Friedrich bekommt für ehrliches Üben sichtbar Münzen und Kisten – ohne dass ihn offene
Wochenziele, ein vor seinem ersten Spiel angelegter Plan oder eine während der Runde geänderte Übung um die Belohnung
bringen.
**Umfang:** B-535, B-546, B-545, B-542.
**Entwickler-Brief:** Quelle der Wahrheit ist `PositionProgressService` (eine Senke `RewardObligations` für Kiste und
Bonus-Sperre; das Malus-Fenster in `SettleClosedPeriodsAsync`). Zwei Migrationen, nacheinander angehängt
(`NewBonusPaidAt`, `ExtraRunContentHash`), vorab gegen eine Kopie der echten DB geprobt. Die Arcade rechnet die Sperre
nicht mehr selbst nach (`rewardsUnlocked`). B-542 ändert kein Verhalten (Entscheidung „bleibt“, Regressionstest).

### Messung vor dem Bau (B-535)

An einer Kopie der echten `pugling.db` widerlegt: „Der Neu-Bonus hat nie ausgezahlt“. Theodor hat elf
`Base`-Buchungen zu je 10 Münzen (= `NewContentPoints`). Die freien Sprossen erhöhen `ReviewCount` nicht; der Bonus
ging verloren, wenn der *erste gewertete* Versuch falsch war. Darum der Backfill `ReviewCount > 0 ⇒ bezahlt` (B-535
Entscheidung 6), damit Theodor nichts doppelt bekommt. Entscheidung 4 (nachträglich angelegte Position) war durch
B-432 schon gebaut.

### Migrationsprobe

`dotnet-ef database update` gegen eine Kopie der echten DB: `CardShownAt` (aus dem Lauf 2026-11-13, auf der echten
DB noch nicht angewandt), `NewBonusPaidAt`, `ExtraRunContentHash` laufen durch; 70 Fortschrittszeilen als bezahlt
markiert, 35 offen; 11 Bestandsrunden ohne Fingerabdruck.
