---
tags: [typ/story, status/idee, bereich/backend, bereich/qualitaet]
aliases: [SchoolTypes im Dokument, Flags-Enum als Werteliste]
status: idee
prio: P2
art: Defekt
quelle: docs/testabdeckung-plan.md
unverifiziert: false
---

# B-60 · Das Vertragsdokument verbietet einen `SchoolTypes`-Wert, den Server und Frontend täglich austauschen

Gefunden vom `pugling-reviewer` beim Review von [B-42](B-42-openapi-typen-generieren.md) Schritt 2 (E6). Kein
Mangel dieser Etappe, aber E6 macht ihn **scharf**: seit die Frontend-Typen aus dem Dokument kommen, steht die
falsche Aussage als TypeScript-Union im Code.

## Ist-Stand am Code, belegt

- `SchoolTypes` ist ein `[Flags]`-Enum (`Contracts/Common/LearnBaseTypes.cs`). Der Schema-Transformer
  (`Program.cs`, „Allowed values") behandelt es wie jedes andere Enum: `type: string` plus eine `enum`-Liste
  der **sieben Einzelnamen**.
- Über die Leitung geht aber eine **Kombination**: der Seed legt Übungen mit `Realschule | Gymnasium` an
  (`Data/Seed.cs`), `GET /api/v1/creator/exercises` liefert also `"schoolTypes": "Realschule, Gymnasium"` –
  einen Wert, den das Schema ausschließt.
- Umgekehrt **sendet** das Frontend genau diesen String (`ExerciseEditModal.tsx:102`), und der Server nimmt
  ihn an.
- Folge in E6: `contract.ts` führt `schoolTypes` als Union der sieben Einzelnamen, was zur Laufzeit falsch ist.
  Das Frontend weicht per Hand aus (`uiTypes.ts` deklariert `schoolTypes?: string`) – die **Hand-Ausnahme** ist
  in B-42 vermerkt, der **Dokument-Mangel** bisher nirgends.

## Die echte Lücke

Das Dokument ist der Vertrag (E3). Hier behauptet es etwas, was die API widerlegt – und zwar in der
schlimmeren Richtung: ein Generator erzeugt einen Typ, der gültige Antworten **zurückweist**. Ein Client, der
streng gegen das Schema validiert, verwirft heute korrekte Daten.

Das ist dieselbe Familie wie die vier Mängel, die E6 behoben hat (Dokument beschreibt die Leitung nicht
wahrheitsgemäß), aber die einzige, bei der die Aussage **falsch** statt nur **fehlend** ist. Darum `Defekt`,
nicht `Aufräumen`, und P2 statt P3.

## Skizze

Für `[Flags]`-Typen im Schema-Transformer die `enum`-Liste **weglassen** und `type: string` mit einer
Beschreibung versehen („comma-separated combination of: …"). Dann ist der generierte Typ `string` – exakt das,
was das Frontend heute von Hand deklariert, und die Hand-Ausnahme in `uiTypes.ts` kann verschwinden.

## Offene Punkte

1. Gibt es weitere `[Flags]`-Enums im Vertrag, oder ist `SchoolTypes` das einzige? (Die Schema-Konvention im
   Backend nennt `[Flags]` als Ausnahme von „Enum als String in der DB" – die Liste ist also kurz, aber sie ist
   nicht gezählt.)
2. Soll die Ausnahme im Transformer stehen oder soll `SchoolTypes` einen eigenen Schema-Eintrag mit `pattern`
   bekommen? Ein `pattern` wäre präziser, aber `openapi-typescript` macht daraus trotzdem nur `string`.
3. Nach der Reparatur: fällt die Hand-Ausnahme `schoolTypes?: string` in `uiTypes.ts` weg? Wenn ja, sinkt die
   Zahl der Hand-Typen von elf auf zehn – und `CreateExercisePayload` bleibt der einzige Grund, warum sie
   überhaupt noch eine eigene Datei brauchen.

## Verlauf

- **2026-08-01** — angelegt aus dem Review zu E6. Der Befund ist **verifiziert**: Seed-Daten, Antwortwert,
  Sende-Stelle im Frontend und die generierte Union sind je am Code belegt.
