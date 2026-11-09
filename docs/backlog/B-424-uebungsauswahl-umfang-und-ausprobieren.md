---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/katalog, rolle/supervisor]
aliases: [Übungsauswahl mit Umfang, Ausprobieren im Assistenten]
status: abgenommen
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
nachgeschaut: "2026-11-09"
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
- **2026-11-09** — gebaut (Lauf AF, Sprint 26). Vor dem Bau die jüngste Verlauf-Zeile gelesen (Freigabe aus `05700b6`, Dialog).
  **Ohne B-408:** die Nachbarin ist `idee` ohne eigene Freigabe; Entscheidung 3 war Reihenfolge, keine Abhängigkeit.
  **Rote Probe mit Zahl:** die vier neuen Integrationstests gegen den Ausgangsstand → 4 von 4 rot an `KeyNotFoundException`
  (`itemCount`/`itemNoun` fehlen), nicht am Aufbau.
  **Backend:** `ExerciseTypeManifest.ItemNoun`/`ItemNounOne` (additiv, je Typ gesetzt; Aufsatz und Rechen-Drill bewusst ohne),
  `IExerciseType.ItemCount` (Basis: Atome aus `ItemsOf`, Vokabel: Item-Zeilen), `ExerciseSummary.ItemCount` aus einer
  korrelierten Unterabfrage in derselben Projektion. **Auslegung (Mechanik, nicht Produkt):** ein Typ ohne zählbare Atome liefert
  `null`, nicht `0` – sonst läse sich ein zuweisbarer Aufsatz als „leer" (AK 4). Einzahl-Wort `ItemNounOne` dazu, damit „1 Karte"
  statt „1 Karten" steht (Entscheidung 1 nennt nur `ItemNoun`; additiv, keine Produktfrage).
  **Frontend:** `umfangText`/`types.umfang` (`lib/exerciseTypes.ts`), Zeile im Assistenten (`WizardTrefferbereich`) und im Planbau
  (`AddPosition`) als `div.row` mit Label **und daneben** „🧪 Ausprobieren" → `ExercisePreviewModal` (im Planbau außerhalb des
  `<form>`). Risiko 3 entfällt: jedes Erwachsenen-Konto trägt die Creator-Rolle (`AccountService.cs:49`), ein reiner Supervisor
  existiert nicht. Risiko 2 entfällt: die Katalogsuche wird von `DocsCaptureTests` nicht mitgeschnitten (Capture unverändert).
  Risiko 1 (Kosten): **nicht gemessen** – eine SQL-Abfrage mit korrelierter Unterabfrage (Reviewer am Code bestätigt), die Config
  wird je Zeile der Seite einmal geparst; bei 20 Zeilen je Seite als tragbar hingenommen, bei `take=500` ungeprüft.
  Doku: `wiki/08-erweitern.md` und `/neuer-uebungstyp` nennen das neue Manifest-Feld samt Testzeile.
  **Belege je Fall** – `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Edit an `3515390 + Arbeitsbaum`, 2026-11-09 15:01:

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | AK 1 Vokabel | `ExerciseItemCountTests.Vokabel_ZaehltIhreItems` | MB1 Vokabel zählt 0 (`VocabularyExerciseType.cs:41`) | red, am Fall |
  | AK 1 Vokabel, nur eigene Zeilen | dieselbe | MB2 Unterabfrage ohne Übungsfilter (`ExerciseCatalogController.cs:137`) | red, am Fall |
  | AK 1 Aufsatz → `null` | `…Aufsatz_HatKeinenUmfang` | MB3 Noun-Gate aus (`ExerciseTypeBase.cs:34`) | red, am Fall |
  | AK 1 inline (Liste, Rechnen) | `…InlineTypen_ZaehlenIhreAtomeAusDerConfig` | MB4 inline → `null` (`:37`) | red, am Fall |
  | Review: kaputte Config | `…KaputteConfig_KostetNurDenUmfangDieserZeile` | MB6 `catch` nur `JsonException` (`:41`) | red, am Fall (`[null]`-Element) |
  | AK 2 | `…Manifest_NenntDieEinheitJeTyp` | MB5 Lückentext ohne Noun (`BuiltInExerciseTypes.cs:190`) | red, am Fall |
  | AK 3 Einzahl | `exerciseTypes.test.ts` „nimmt für genau eins die Einzahl" | MF1 (`exerciseTypes.ts:98`) | red, am Fall |
  | AK 4 „leer" | „sagt bei null Atomen „leer"" | MF2 (`:97`) | red, am Fall |
  | AK 3 Umfang Assistent | `VaterWizard.test.tsx` „zeigt den Umfang der Übung in der Zeile" | MF3 (`VaterWizard.tsx:785`) | red, am Fall |
  | AK 3 genau diese Übung | „öffnet den Testmodus für genau diese Übung …" | MF4 öffnet die erste Zeile (`:789`) | red, am Fall |
  | AK 3 Auswahl unverändert | dieselbe + E2E | MF5/ME2 Knopf schaltet die Auswahl mit um (`:789`) | red, an beiden |
  | Review: veraltete Zeile | „sperrt Ausprobieren auf veralteten Zeilen" | MF6 ohne `disabled` (`:788`) | red, am Fall |
  | AK 3 Umfang Planbau | `e2e/uebungsauswahl-umfang.spec.ts` | ME1 Umfang aus der Planbau-Zeile (`PlanPositions.tsx:614`) | red, am Fall |

  Grundlinie nach dem Zurückspielen grün. Ungemessen: „Knopf im Label" als Verschiebung – gehalten von der E2E-Zusicherung, dass
  der Name des Kästchens/Radios „ausprobieren" nicht enthält (Review-Fund), aber ohne eigene Mutante.
- **2026-11-09** — Review: `frontend-reviewer` keine 🔴; 🟡 Planbau-Hälfte der E2E prüfte nur den Ausgangszustand (Radio nie
  gewählt) → wählt jetzt vorher, prüft danach `toBeChecked`, dazu Namensprüfung gegen „Knopf im Label" (**Zähler 1**); 🟢 Ausprobieren
  auf veralteten Zeilen → gesperrt (ohne Zählung); 🟢 Knopf in `role="radiogroup"` (Pfeiltasten unberührt, mehr Tab-Stopps) und
  Zeilen-Umbruch bei 768 px im Assistenten (ungemessen) → hingenommen; 🟢 `exercise_empty`-Text nennt einen Ausweg, Fehler im
  Dialog ohne `role="alert"` → [B-444](B-444-testmodus-fehler-ohne-alert-und-alter-ausweg.md). `pugling-reviewer` keine 🔴; 🟡 `catch`
  nur `JsonException`, eine parsbare Config mit `null`-Element hätte die ganze Suche auf 500 gesetzt (**Zähler 2**) und 🟡 dieser
  Fehlerpfad ungetestet (**Zähler 3**) → `catch (Exception) when (not OperationCanceled)` + Theory mit direkt geschriebener kaputter
  Config; 🟢 Manifest-Test zählt jetzt auch die Endpunkt-Einträge, 🟢 Summary trennt `ItemCount` (Größe) von `DefaultItemCount`
  (Rundenlimit) – beide ohne Zählung; 🟢 Umbenennen in `ContentCount` → nicht, der Doc-Kommentar nennt die Namensnähe.
  **Zähler Sprint 26: 3 von 5.**
- **2026-11-09** — abgenommen. Backend 1178/1178, Vitest 696/696, `tsc -b` sauber, Playwright **122 passed** (ganze Suite, Server
  vom Lauf nach dem letzten Edit gestartet). **Rollengang** Vater = `e2e/uebungsauswahl-umfang.spec.ts` (eigene Übung mit drei
  Vokabeln: Assistent und Planbau zeigen „3 Karten", „Ausprobieren" öffnet den Testmodus genau dieser Übung, Schließen lässt die
  Wahl stehen). Creator und Sohn als Regressionszeugen: Suite grün; der Creator-Vertrag ist additiv (`exercise-types`,
  `exercises`), kein Spielweg berührt. Kein Browser-Rollengang von Hand. Commit: siehe `git log -- docs/backlog/B-424-uebungsauswahl-umfang-und-ausprobieren.md` (Lauf AF, Sprint 26).
- **2026-11-09** — Nachschau (Retro Sprint 27, Lauf AG, frischer `pugling-reviewer`, `git show 453a740`): kein 🔴, drei 🟡 →
  [B-448](B-448-umfang-nachschau-reste.md) (`entgangen_bei: [B-424]`). Prüfpunkte: alle Datei:Zeile-Angaben der Tabelle gegen
  `453a740`, MB1/MB2/MB3/MB6 am Code nachgerechnet (rot am Fall); „Risiko 2 entfällt" (`DocsCaptureTests.cs:570` ohne `Capture`),
  „Creator-Rolle für jedes Erwachsenen-Konto" (`AccountService.cs:49`, einzige `Profiles.Add`-Stellen 49/50/64) und „eine Abfrage je
  Seite" (plus das `CountAsync` des Pagings) bestätigt; Zählung je Typ gegen `ExerciseContentResolver.ItemsOfAsync`.
