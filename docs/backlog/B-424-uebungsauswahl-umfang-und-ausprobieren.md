---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/katalog, rolle/supervisor]
aliases: [Übungsauswahl mit Umfang, Ausprobieren im Assistenten]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-vater
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-424 · Die Übungsauswahl im Assistenten und im Planbau zeigt den Umfang nicht und bietet kein „Ausprobieren"

Im Rollengang vom 2026-11-08 wählte der Vater im Assistenten zwei Übungen – als Zeile mit Titel, Typ und
Unit. Wie viele Wörter das sind, wie lange eine Runde dauert, wie sich die Übung für das Kind anfühlt: nichts
davon ist sichtbar, und ein „Ausprobieren" gibt es nur in der Creator-Werkstatt. Der Vater weist blind zu und
setzt darauf eine Pflicht mit Malus.

## User Story

Als *Vater* möchte ich in der Übungsauswahl je Zeile **den Umfang** („12 Karten") sehen und die Übung **vor dem
Zuweisen ausprobieren**, damit ich weiß, worauf ich mein Kind verpflichte.

## Ist-Stand am Code

- **Zeile im Assistenten:** `VaterWizard.tsx:766` – Checkbox, Titel, Typ, Unit; keine Menge, kein Knopf.
  Dieselbe Zeile im Planbau (`PlanPositions.tsx`, Trefferliste „Übung aus dem Katalog") zeigt zusätzlich Klasse,
  Art und Bewertung (seit B-270); [B-408](B-408-assistent-zeigt-keine-bewertung.md) will die Bewertung auch im
  Assistenten.
- **Vertrag:** `ExerciseSummary` (`ExerciseCatalogDtos.cs:27-31`) trägt `DefaultItemCount` (die Begrenzung,
  die der Creator als Standard-Menge setzt), aber **nicht** die tatsächliche Zahl der Inhalts-Atome.
- **Vorschau existiert:** `GET creator/exercises/{id}/preview` + `POST …/preview/check`
  (`ExercisePreviewModal.tsx`), eingebaut in `VaterExerciseCreate.tsx` und `VaterExercises.tsx` – nicht im
  Assistenten, nicht im Planbau. Ein Vater-Konto trägt immer die Creator-Rolle (`docs/grundprinzip.md`), der
  Aufruf ist für ihn erlaubt.
- **Inhalts-Atome** liefert `ExerciseContentProvider.ItemsOf` je Typ (Vokabel: `ExerciseItem`-Zeilen; inline
  Typen: Listen in der Config).

## Die echte Lücke

Zwei Informationen, die für die Entscheidung „zuweisen oder nicht" tragen, fehlen genau an der Stelle, an der
sie fällt. Beide Quellen existieren (Inhalts-Atome, Vorschau-Dialog); es fehlt ein additives Feld und ein
Knopf an zwei Zeilen.

## Offene Punkte

1. ~~Umfang als Zahl oder als Dauer?~~ → Entscheidung 1.
2. ~~Vorschau im Assistenten – Dialog oder Link in die Werkstatt?~~ → Entscheidung 2.
3. ~~Zusammen mit B-408?~~ → Entscheidung 3.

## Entscheidungen

Getroffen vom PM in der Fable-Sitzung vom 2026-11-08 unter dem Auftrag des Nutzers, das Backlog
nachtlauffähig zu machen ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Vetorecht beim Nutzer.

1. **Umfang als Zahl mit typgerechter Einheit, keine Dauer.** `ExerciseSummary` bekommt additiv `ItemCount`
   (Zahl der Inhalts-Atome über `ExerciseContentProvider`); die Zeile zeigt „12 Karten" / „4 Lücken" /
   „16 Einträge" – das Wort kommt aus dem Typ-Manifest (neues Feld `ItemNoun` je Typ, additiv), nicht aus einer
   Frontend-Tabelle (Hausregel). Eine Dauer wäre geraten. Kosten: Config-Parsen je Zeile im Katalog-Endpunkt
   (Vokabel über DB-Count, inline Typen über die geladene Config); bei 20 Zeilen je Seite tragbar, mit Messung
   im Test.
2. **„Ausprobieren" öffnet den vorhandenen Dialog**, in beiden Zeilen (Assistent, Planbau), neben der Checkbox.
   Kein Link in die Werkstatt – der Vater soll die Perspektive nicht wechseln müssen. Kosten: `ExercisePreviewModal`
   bekommt zwei weitere Einbaustellen; sein Zustand (eine offene Übung) wird hochgezogen.
3. **Im selben Sprint wie B-408**, denn beide ändern dieselben zwei Zeilen; B-408 zuerst (Bewertung im
   Assistenten), dann diese Story. Kosten: keine über die Reihenfolge hinaus.

## Akzeptanzkriterien

1. `GET creator/exercises` liefert je Zeile `itemCount` (Vokabel: Zahl der Items; Lückentext: Zahl der Lücken;
   Liste: Zahl der Einträge; Rechen-Drill: Zahl der Aufgaben oder `null`, wenn generiert).
2. `GET creator/exercise-types` liefert je Typ `itemNoun` (de, z. B. „Karten", „Lücken").
3. Die Zeile im Assistenten und im Planbau zeigt „<n> <itemNoun>" und einen Knopf „🧪 Ausprobieren", der den
   Vorschau-Dialog für genau diese Übung öffnet; Schließen lässt die Auswahl unverändert.
4. Eine Übung ohne Atome zeigt „leer" – und ist damit als nicht zuweisbar erkennbar.
5. Vitest: Zeile rendert Umfang und Knopf; Integrationstest: `itemCount` für Vokabel und einen inline Typ.

## Schätzung

**Größe: M** — Backend: zwei additive Felder (Summary, Manifest) mit Zählung je Typ; Frontend: zwei Zeilen,
Dialog-Einbau, Vitest; OpenAPI/Client nachziehen. Kein Schema, kein Vertragsbruch (additiv).

**Risiken:** (1) Der Katalog-Endpunkt projiziert heute ohne Config – die Zählung darf kein N+1 werden: Vokabel
als Sub-Count in der Projektion, inline Typen aus der ohnehin geladenen `ConfigJson` (messen, Zahl ins
Verlauf). (2) `DocsCaptureTests` ändern sich (neue Felder) – Capture neu erzeugen. (3) Der Vorschau-Dialog
nutzt Creator-Routen; für ein reines Supervisor-Konto (Zweitbetreuer ohne Creator-Rolle) den Knopf
ausblenden, wenn `session.role` keine Creator-Rolle trägt – prüfen, wie `auth.tsx` die Rollen hält.

**Angriffsplan (Backend zuerst):** (1) `ItemNoun` am `IExerciseType` + Manifest; (2) `ItemCount` in
`ExerciseCatalogController`-Projektion; (3) Tests + Capture; (4) `gen:contract`, Client; (5) Zeile im
Assistenten (nach B-408) und im Planbau; (6) `VaterWizard.test.tsx`, `PlanPositions.test.ts`; E2E-Fall in
`assistent.spec.ts` („Ausprobieren" öffnet Dialog).

**Testweg:** `ExerciseCatalogTests` bzw. die bestehende Katalog-Testklasse (Integration), `VaterWizard.test.tsx`,
`assistent.spec.ts` (Rollengang Vater).

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Vater ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt, Entscheidungen vom PM unter dem Auftrag „nachtlauffähig machen" (Vetorecht beim Nutzer),
  geschätzt. Läuft im selben Sprint wie B-408.
- **2026-11-08** — **Freigabe des Nutzers (Autonomer Modus, Opt-in):** die sieben vorgelegten Entscheidungen der Fable-Sitzung hat der Nutzer im Dialog bestätigt („Ja, so fahren“, Antwort auf die Frage nach den Entscheidungen für die erste Nacht, [Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Ein Nachtlauf darf diese Story auf Grundlage der `## Entscheidungen` bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
