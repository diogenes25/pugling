---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/tests, rolle/creator]
aliases: [Bewertungsliste Reihenfolge ungetestet]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Review Sprint 12 zu B-12 (siebter Nachtlauf, docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-26"
---

# B-269 · Bewertungen: Reihenfolge der Liste und `UpdatedAt` beim Überschreiben sind ungetestet

Der `pugling-reviewer` hat zu [B-12](B-12-uebungen-kuratieren.md) zwei Testlücken benannt, die grün bleibende
Mutanten zulassen (`backend/Pugling.Api.Tests/ExerciseRatingsTests.cs`):

1. `GET …/ratings` sortiert „jüngste Änderung zuerst“ (`ExerciseRatingsController.List`, `OrderByDescending(r =>
   r.UpdatedAt)`). Jede Zusicherung sieht höchstens eine Zeile, also bleibt das Entfernen der Sortierung grün.
2. Ein erneutes `PUT` setzt `UpdatedAt` neu. Der Test vergleicht nur `CreatedAt`, also bleibt das Weglassen von
   `rating.UpdatedAt = now` grün.

Vorschlag: zwei Bewerter, der erste ändert danach, die Liste muss ihn zuerst nennen; dazu
`second.updatedAt > first.updatedAt`. Beides als rote Probe gegen die genannte Mutante.

**Nicht in diesem Lauf behoben:** Der Reviewer stufte beide als Hinweis (🟢) ein. Jede Behebung ändert Tests und
zählt damit als Fund (Freigabe 3); der Zähler von Sprint 12 stand bei 4, zwei weitere hätten den Lauf beendet.
Ausdrücklich zurückgestellt, nicht übersehen.

## Verlauf

- **2026-10-26** — angelegt aus dem `pugling-reviewer` zu B-12 im siebten Nachtlauf (Hinweise 6 und 7).
- **2026-10-26** — **Entscheidung des Nutzers** (Morgen-Entscheidung 2026-10-26, Dialog, [Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)): Das Zurückstellen unter dem alten Zähler war richtig. Die Story wird jetzt in der **Nacharbeit von Sprint 12** (zu B-12) mitgenommen. **Freigabe des Nutzers (Autonomer Modus, Opt-in):** ein Nachtlauf darf sie selbst ausformulieren, schätzen und bauen.
- **2026-10-26** — ausformuliert, geschätzt (XS, nur Tests, kein Verhalten) und gebaut in Lauf B unter Freigabe 9 (Freigabe-Zeile des Nutzers vom 2026-10-26) ([Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf B). Neuer Test `Liste_Nennt_Die_Juengste_Aenderung_Zuerst` (drei Bewerter, r1 ändert nur den Kommentar → erwartet `[r1, r3, r2]`; Anlage-, Id- und Sterne-Reihenfolge ergeben je etwas anderes) und `updatedAt`-Zusicherung im Überschreib-Test. Rote Proben: Liste nach `Id` / `CreatedAt` / Sternen → je **1 failed**; `UpdatedAt` nicht gesetzt → **2 failed**; `UpdatedAt` nur bei neuen Sternen → **1 failed**. Review: `pugling-reviewer` + Delta-Review (Restlücke benannt: „Zeilen mit Kommentar zuerst“ bliebe grün, konstruiert, nicht behoben). **Abgenommen**; Commit: `ee71a74`.
- **2026-10-26** — **Nachschau** (frischer Prüfer, read-only über `ee71a74`, [Protokoll](../pm-sitzung-2026-10-26-tag.md)): kein Entgleiten. Geprüft: die drei Reihenfolgen `[9,11,10]` / `[11,10,9]` / `[9,10,11]` nachgerechnet; die Mutanten „nach Sternen“ und „`UpdatedAt` nur bei neuen Sternen“; „`UpdatedAt` nicht gesetzt → 2 failed“ (Id-Ordnung plus strenges `>`); die Uhr-Anmerkung (zwischen den beiden verglichenen PUTs liegt ein dritter).
