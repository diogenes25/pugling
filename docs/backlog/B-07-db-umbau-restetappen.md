---
tags: [typ/story, status/geschaetzt, bereich/doku, rolle/creator, rolle/supervisor, rolle/student]
aliases: [DB-Umbau Betriebsschritt, Azure-DB-Umstellung]
status: geschaetzt
prio: P1
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/db-struktur-umbau-plan.md
---

# B-07 · DB-Struktur-Umbau: der offene Betriebsschritt

**Sammel-Story, keine Kopie.** Quelle der Wahrheit für Entscheidungen, Etappen-Zuschnitt und Fallstricke
bleibt [db-struktur-umbau-plan.md](../db-struktur-umbau-plan.md). Diese Story trägt nur Sichtbarkeit und
Zustand — sie führt **keine** Etappen-Zustände und wird erst `abgenommen`, wenn dort keine offene Etappe
mehr steht.

## User Story

Als Betreiber möchte ich die Azure-Instanz auf die neu gefaltete Migrationskette umstellen, damit das
nächste Deployment überhaupt startet.

## Ist-Stand (am 2026-07-30 gegen Plandokument und Commits geprüft)

**Der Umbau ist durch: E0–E14** (`74637b0` schließt ab, `6471e1d` ist E13). Kein Modell-Drift mehr —
`LearnGoal` ist aus Snapshot *und* `PuglingDbContext` verschwunden. **Offen ist nur noch der eine
Betriebsschritt außerhalb des Repos**, und den kann keine Code-Sitzung ausführen.

**Der Stand wanderte während der Ernte fünfmal** — das Lehrstück, warum eine Sammel-Story auf das
Plandokument zeigt statt Etappen zu kopieren: `MEMORY.md` behauptete „E7 und E9–E14 offen"; beim Ernten waren
E7/E9 schon committet, eine Stunde später E10–E12, kurz darauf E13 und E14. Jede kopierte Etappenliste wäre
binnen Stunden falsch gewesen.

## Der offene Betriebsschritt — **vor dem nächsten Deploy**

Die Azure-DB stammt aus der alten Migrationskette und wird vom Historien-Guard abgewiesen; ein Deploy würde
beim Start scheitern. Nötig in den **Azure App Settings**:

1. `ConnectionStrings__Default` → `Data Source=/home/data/pugling-v2.db`
2. `Seed__Enabled=true` — sonst bleibt die neue Datei leer (E9 hat ein Umgebungs-Gate eingeführt).

Die alte Datei **nicht löschen**: sie ist die Rückfallebene, und das Zurückflippen der Einstellung ist der
komplette Rollback. Aufräumen erst, wenn v2 mehrere Tage steht. Details im Plandokument, Abschnitt
„Betriebsschritt".

Zur Einordnung: Der Azure-Deploy ist ohnehin stillgelegt (Trigger entfernt, `4eadba8`), und das
Publish-Profile-Secret fehlt bewusst (B-33). Diese Story ist damit **nicht dringend, aber blockierend** —
sie ist die erste Handlung, sobald wieder deployt werden soll.

## Was das für andere Stories bedeutet

- **`TimeSlotRule` ist Konfiguration** (E12): B-10 ist darauf nachgezogen und `geschaetzt`.
- **`LearnGoal` ist gelöscht** (E13), seine Rolle hat das `KeyResult`: B-04 Kriterium 5 nachgezogen; **B-14
  ist damit erfüllt**, denn `ObjectiveRewardService` belohnt schon idempotent per Lazy Settlement.

## Entscheidungen

→ [db-struktur-umbau-plan.md](../db-struktur-umbau-plan.md), Abschnitt **„Getroffene Entscheidungen (nicht
neu verhandeln)"** und **„Arbeitsregeln"**. Sie werden hier ausdrücklich **nicht** wiederholt: ein
Plandokument, das eine Sitzung Etappe für Etappe fortschreibt, ist der einzige Ort, an dem sie stimmen.

## Akzeptanzkriterien

1. ~~E11–E14 sind je grün abgeschlossen~~ — erledigt am 2026-07-30 (`74637b0`, 610 Tests grün).
2. ~~Die Legacy-Ausnahme `TimeSlotRule` ist aus `CLAUDE.md` verschwunden~~ — erledigt mit E12.
3. Beide App Settings sind gesetzt, die App startet gegen `pugling-v2.db`, der Seed hat gegriffen.
4. Die alte Datei ist nach der Karenzzeit gelöscht — erst dann geht diese Story auf `abgenommen`.

## Schätzung

**Größe: XS** — zwei Einstellungen im Azure-Portal. Der Code-Anteil ist **null**.

- **`migration: nein` · `vertragsbruch: nein`** — es wird nichts gebaut. Der Umbau, der beides mitbrachte,
  ist durch.
- **Das kann keine Code-Sitzung erledigen** (dieselbe Klasse wie B-31, „am echten Handy gegenhören"): Es
  braucht einen Menschen im Azure-Portal. Die Story wartet also nicht auf Entwicklung, sondern auf eine
  Handlung.
- **Risiko, das die Reihenfolge bestimmt:** Wird `Seed__Enabled=true` vergessen, startet die App gegen eine
  **leere** Datenbank — und das sieht wie Datenverlust aus, obwohl die alte Datei unberührt daneben liegt.
  Beide Einstellungen gehören in denselben Vorgang.
- **Verifikation:** App startet ohne Historien-Guard-Fehler, ein Login gegen die geseedeten Konten geht durch.

## Verlauf

- **2026-07-30** — als Sammel-Story geerntet; Stand gegen das Plandokument geprüft und gegenüber den
  Notizen korrigiert (E11–E14 statt E7/E9–E14).
- **2026-07-30** — der Umbau wurde in derselben Sitzung fertig (E13 `6471e1d`, E14 `74637b0`, 610 grün).
  Die Story schrumpft damit von „vier Etappen, L" auf **den einen Betriebsschritt, XS** — und wechselt vom
  Entwicklungs- in den Betriebs-Charakter.
