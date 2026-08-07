---
tags: [typ/story, status/geschaetzt, bereich/katalog, rolle/creator]
aliases: [Vorschau für nicht-prüfbare Typen]
status: geschaetzt
prio: P3
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: memory/uebungs-testmodus.md
---

# B-15 · Vorschau für die nicht-prüfbaren Übungstypen

## User Story

Als Vater (Creator) möchte ich einen Aufsatz vor dem Zuweisen genauso ansehen können wie jede andere
Übung im Testmodus, damit ich weiß, was mein Kind zu lesen und zu schreiben bekommt, bevor ich die Übung
in einen Plan aufnehme.

## Ist-Stand am Code

- `EssayExerciseType.ItemsOf` liefert für jeden Aufsatz unbedingt eine leere Liste
  ([BuiltInExerciseTypes.cs:68](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) — ein Aufsatz
  hat keine Frage-Antwort-Paare, das ist die Natur des Typs, keine unfertige Dateneingabe.
- `ExercisePreviewService.BuildAsync` bricht bei `items.Count == 0` sofort mit `null` ab
  ([ExercisePreviewService.cs:30](../../backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs)),
  `CheckAsync` ebenso ([:49](../../backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs)) — ein
  Aufsatz kommt in beiden Methoden nie über die erste Zeile hinaus.
- Der Controller übersetzt das `null` in ein `400` mit `code: no_checkable_content`
  ([ExercisePreviewController.cs:35-42](../../backend/Pugling.Api/Controllers/Creator/ExercisePreviewController.cs),
  Fehlercode [ApiErrors.cs:139](../../backend/Pugling.Api/Errors/ApiErrors.cs)) — bewusst **nicht** derselbe
  Code wie „Übung noch leer" (`exercise_empty`, [ApiErrors.cs:146](../../backend/Pugling.Api/Errors/ApiErrors.cs)),
  weil hier die *Art* des Typs gemeint ist, keine unfertige Übung.
- Das Frontend fängt genau diesen Code ab und zeigt eine freundliche, korrekte Erklärung statt der
  technischen Meldung: „Diese Übung hat keine einzeln prüfbaren Aufgaben … Du kannst sie zuweisen, aber
  nicht durchspielen." ([ExercisePreviewModal.tsx:33-43](../../frontend/src/vater/ExercisePreviewModal.tsx)).
  Der Vater sieht also nie den Schreibauftrag, die Wortzahl-Grenzen oder die Bewertungskriterien, die er
  selbst beim Anlegen eingegeben hat (`EssayConfig.Prompt`/`MinWords`/`MaxWords`/`Rubric`,
  [ExerciseConfigs.cs:83-93](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) — nur diese
  Erklärung, warum er nichts sieht.
- Ein E2E-Test **pinnt dieses Verhalten aktuell als richtig**: er öffnet den Testmodus für einen Aufsatz und
  erwartet exakt den Text „keine einzeln prüfbaren Aufgaben"
  ([uebungstypen.spec.ts:192-197](../../frontend/e2e/uebungstypen.spec.ts)) — jede Behebung muss diese
  Zeile mit ändern.
- `IExerciseType` kennt heute keinen Haken für „zeig etwas an, ohne es zu bewerten"
  ([IExerciseType.cs:30-100](../../backend/Pugling.Api/Exercises/IExerciseType.cs)); `ExerciseTypeBase`
  liefert für jede Facette einen neutralen Standard
  ([ExerciseTypeBase.cs:12-70](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)), aber keinen für
  eine reine Anzeige.
- Nachgesehen, aber **nicht Teil dieser Lücke**: Auch der Sohn bekommt beim Üben keine Karte für den
  Aufsatz-Prompt, weil dieselbe leere `ItemsOf`-Liste den Karteikarten-Cursor auf 0 setzt
  (`DueItemIndicesAsync` → `PoolSize` → 0,
  [PositionPlayService.cs:80-81](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs));
  `PositionProgressService` behandelt das bewusst als „inhaltslose Runde" und zählt das Pflichtziel über
  eine Mindest-Verweildauer statt über Karten
  ([PositionProgressService.cs:106-114](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs)).
  Ob der Sohn den Schreibauftrag dabei je zu sehen bekommt, ist ungeklärt — das ist die Sohn-Ausspielung,
  nicht der Vater-Testmodus dieser Story (siehe Entscheidung 4).

## Die echte Lücke

Nach der Korrektur vom 2026-08-02 bleibt genau **ein** Typ ohne jede Vorschau: **Essay**. Reading und
Listening haben Inhalts-Atome und werden im Testmodus sogar bewertet; ArithmeticDrill geht bewusst seinen
eigenen `generate`/`check`-Weg. Nur der Aufsatz hat *grundsätzlich* keine Atome (freier Text, kein
Frage-Antwort-Paar) — und darum bleibt sein Testmodus heute bei „geht nicht" stehen, obwohl der Vater
etwas zu sehen hätte: den Schreibauftrag, die Wortzahl-Grenzen, die Bewertungskriterien. Die Lücke ist also
nicht „Essay lässt sich nicht bewerten" (das ist richtig und bleibt so), sondern „Essay lässt sich nicht
mal **ansehen**".

## Offene Punkte

1. Was genau soll der Vater beim Aufsatz sehen? *Empfehlung:* Schreibauftrag (`Prompt`) plus
   Wortzahl-Grenzen (`MinWords`/`MaxWords`) plus die Bewertungskriterien (`Rubric`), alles rein lesend —
   dieselben Felder, die er beim Anlegen selbst eingegeben hat. → siehe Entscheidung 1.
2. Passt „nur anzeigen, nicht bewerten" in das bestehende `PreviewData`-Modell, oder braucht es eine eigene
   Antwortform? *Empfehlung:* ein additives `Gradable`-Flag am bestehenden `PreviewData` statt eines
   Parallel-Vertrags — ein Aufsatz ist sonst noch eine Vorschau, nur ohne Bewertungsknopf. → siehe
   Entscheidung 2.
3. Wo lebt der neue Anzeige-Weg — ein generischer Haken an `IExerciseType`, oder eine Sonderbehandlung des
   Schlüssels `"Essay"` im Service/Controller? *Empfehlung:* ein virtueller Haken (Standard `null`) nach dem
   Muster „ein Typ = eine Klasse", wie `ListeningExerciseType.StageFacets` es für sein eigenes Bedürfnis
   schon vormacht. → siehe Entscheidung 3.
4. Der Sohn-seitige Fund (siehe Ist-Stand, letzter Punkt): bekommt das Kind den Aufsatz-Prompt beim Üben
   überhaupt zu sehen? *Empfehlung:* **nicht** in dieser Story klären oder beheben — das ist die
   Sohn-Ausspielung, ein anderer Code-Pfad (`PositionPracticeController`/Sohn-Arcade) und, sollte sich ein
   Defekt bestätigen, eine eigene, wahrscheinlich höher priorisierte Story (ein Kind, das seinen Auftrag
   nicht sieht, wiegt schwerer als ein Vater ohne Vorschau). → siehe Entscheidung 4.

## Entscheidungen

1. **Der Anzeige-Umfang ist Prompt + Wortzahl-Grenzen + Rubrik, rein lesend.** Begründung: Das sind exakt
   die Felder, die `EssayConfig` trägt und die der Vater selbst befüllt hat — nichts wird erfunden, nichts
   verschwiegen. Kosten: die drei Felder müssen zu **einem** Anzeige-Text zusammengefasst werden (kein
   eigenes Rubrik-Widget in dieser Story) — akzeptiert für die Größe S, siehe Risiko in der Schätzung.
2. **`PreviewData` bekommt ein additives `Gradable`-Flag (Default `true`)**, kein eigener DTO-Zweig.
   Begründung: Ein Aufsatz *ist* eine Vorschau — Typ, Stufe, ein Item — nur ohne Bewertungsknopf; ein
   Parallel-Vertrag würde denselben Zustand zweimal modellieren. Kosten: Frontend muss `!data.gradable`
   vor dem Rendern der Eingabe-/Bewertungs-Elemente prüfen; das Feld ist additiv, darum `vertragsbruch: nein`.
3. **Ein neuer virtueller Haken `StaticView(configJson)` an `IExerciseType`/`ExerciseTypeBase`** (Standard
   `null`), nur von `EssayExerciseType` überschrieben. Begründung: folgt der bestehenden
   Übungstyp-Plugin-Regel „ein Typ = eine Klasse" statt eines `if (exercise.Type == "Essay")` im Service —
   ein künftiger weiterer inhaltsloser Typ bekäme die Vorschau geschenkt, statt eine zweite Sonderbehandlung
   zu brauchen. Kosten: ein zusätzliches Interface-Mitglied, das aktuell zehn von elf eingebauten Typen nie
   überschreiben (Leerlauf-Standard, aber genau das Muster, das `ExerciseTypeBase` schon für jede andere
   Facette trägt).
4. **Der Sohn-seitige Fund wird nicht in dieser Story behoben.** Begründung: andere Fläche
   (Übungs-Ausspielung statt Vater-Testmodus), anderer Code-Pfad, potenziell andere Priorität. Kosten:
   keine — die Story bleibt bei ihrem Titel. Empfehlung an den Nutzer: den Fund separat per `/backlog neu`
   als eigene Idee aufnehmen (nicht Teil dieses Durchgangs, da der Auftrag hier ausdrücklich nur B-15
   anfasst).

## Akzeptanzkriterien

1. `GET creator/exercises/{id}/preview` liefert für einen Aufsatz **kein** `400` mehr, sondern ein
   `PreviewData` mit `gradable: false`, einem Item, dessen `prompt` der Schreibauftrag ist und dessen
   `hint` Wortzahl-Grenzen und Bewertungskriterien lesbar zusammenfasst.
2. `POST …/preview/check` bleibt für einen Aufsatz unverändert bei `400 no_checkable_content` — es gibt
   nichts zu bewerten, und das war nie die Lücke.
3. Für jeden anderen eingebauten Typ (insbesondere `ArithmeticDrill`) ändert sich am Vorschau-Verhalten
   **nichts** — der neue Haken bleibt für sie `null`.
4. Der Testmodus-Dialog zeigt für einen Aufsatz Schreibauftrag, Wortzahl-Grenzen und Rubrik an, **ohne**
   Eingabefeld, Multiple-Choice, Selbsteinschätzung oder „Auswerten"-Knopf — nur eine schließende Aktion.
5. `frontend/e2e/uebungstypen.spec.ts:192-197` ist auf das neue Verhalten umgeschrieben (Prompt/Wortzahl/
   Rubrik sichtbar, kein „no_checkable_content"-Text mehr) statt gelöscht.

## Schätzung

**Größe: S** — ein neuer, generisch geschnittener aber schmal genutzter Haken (nur ein Typ überschreibt
ihn), ein additives Vertragsfeld, eine Frontend-Verzweigung, eine anzupassende E2E-Zeile. Kein DB-Zugriff,
keine neue Tabelle, keine neue Spalte.

- **`wo`: beides** — Backend trägt den Haken/die Projektion, Frontend die Anzeige-Verzweigung im
  `ExercisePreviewModal`.
- **`migration`: nein** — `EssayConfig` trägt `Prompt`/`MinWords`/`MaxWords`/`Rubric` bereits
  ([ExerciseConfigs.cs:83-93](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)); keine neue
  Spalte, keine neue Tabelle.
- **`vertragsbruch`: nein** — `PreviewData.Gradable` ist ein additives Feld mit Default `true`; bestehende
  Aufrufer (`ExercisePreviewService.BuildAsync`, ein einziger Konstruktionsaufruf) bleiben unverändert
  kompilierbar.

**Angriffsplan** (Backend zuerst):

1. `IExerciseType`/`ExerciseTypeBase`: neuer virtueller Haken
   `ContentItem? StaticView(string configJson) => null;`.
2. `EssayExerciseType.StaticView`: ein `ContentItem` aus `EssayConfig` bauen (`Prompt` = Schreibauftrag,
   `Hint` = Wortzahl-Grenzen + Rubrik als lesbarer Text).
3. `ExercisePreviewService.BuildAsync`: `type` **vor** der Leer-Prüfung auflösen (Reihenfolge dreht sich
   um); bei `items.Count == 0` erst `type.StaticView(...)` versuchen, nur bei `null` weiterhin `null`
   zurückgeben. `CheckAsync` bleibt unverändert (bewusst, siehe Akzeptanzkriterium 2).
4. `Pugling.Contracts/Creator/ExercisePreviewDtos.cs`: `PreviewData` um `bool Gradable = true` ergänzen.
5. Backend-Tests in `ExercisePreviewTests.cs`: Essay liefert jetzt `PreviewData` mit `Gradable: false` und
   sichtbarem Prompt/Hint; `CheckAsync` bleibt `null`; ein zweiter eingebauter Typ (z. B. `ArithmeticDrill`)
   bleibt unverändert `null` in `BuildAsync`.
6. Frontend: `npm run gen:contract` zieht `gradable` automatisch in `src/lib/contract.ts`;
   `ExercisePreviewModal.tsx` verzweigt auf `!data.gradable` (Prompt/Hint anzeigen, Eingabe-/
   Bewertungs-UI und „Auswerten" auslassen, nur eine schließende Aktion).
7. `frontend/e2e/uebungstypen.spec.ts:192-197` auf das neue Verhalten umschreiben.

**Risiken:**

- Wortzahl-Grenzen und Rubrik werden zu **einem** `Hint`-Text zusammengefasst statt strukturiert übertragen
  — für S ausreichend, aber wenn die Rubrik später als Liste mit eigenem Layout erscheinen soll, braucht es
  eigene Felder statt eines flachen Strings (dann eine Folge-Story).
- Der neue Interface-Haken ist echt generisch, wird aber aktuell von **genau einem** Typ genutzt — bewusst
  in Kauf genommen (Entscheidung 3), nicht versehentlich Über-Engineering.

**Testweg:** `backend/Pugling.Api.Tests/ExercisePreviewTests.cs` (neue Fälle für Essay `BuildAsync`/
`CheckAsync` sowie einen unveränderten Kontroll-Typ), `frontend/e2e/uebungstypen.spec.ts` (angepasste
Zeile 192-197, Lauf über `npm run test:e2e`), danach ein manueller `/smoke-test`-Aufruf des
Preview-Endpunkts gegen einen echten Aufsatz.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-02** — Ist-Stand richtiggestellt: von fünf genannten Typen bleibt einer. Der Rest der Story
  ist unberührt, die Stufe bleibt `idee` — ausformuliert ist damit nichts, nur eine falsche Behauptung
  weniger im Bestand.
- **2026-08-03** — ausformuliert: Ist-Stand vollständig gegen den Code belegt (Datei:Zeile für
  `ItemsOf`/`BuildAsync`/`CheckAsync`/Controller/Fehlercode/Frontend/E2E-Test), die veraltete Zeilenangabe
  `BuiltInExerciseTypes.cs:48` auf `:68` korrigiert, die echte Lücke geschärft („nicht bewertbar" bleibt
  richtig, „nicht ansehbar" ist die Lücke), vier offene Punkte je mit Empfehlung formuliert.
- **2026-08-03** — gegrillt: alle vier offenen Punkte in nummerierte Entscheidungen überführt (autonom
  getroffen, Nutzerauftrag 2026-08-04) — Anzeige-Umfang, additives `Gradable`-Flag, generischer
  `StaticView`-Haken statt Typ-Sonderbehandlung im Service, Sohn-seitiger Fund bewusst ausgeklammert.
- **2026-08-03** — geschätzt: Größe S, `wo: beides`, `migration: nein`, `vertragsbruch: nein`,
  Angriffsplan (Backend zuerst) und Testweg festgelegt (autonom getroffen, Nutzerauftrag 2026-08-04). Kein
  XL-Split nötig.
- **2026-08-07** — Autonomer Modus (Opt-in je Vorhaben, README → „Autonomer Modus") vom Nutzer im Dialog
  ausdrücklich freigegeben: ein Nachtlauf darf diese Story trotz `art: Wunsch` ohne weitere Rückfrage bauen
  (Rollengang/Reviewer bleiben Pflicht wie bei jeder Abnahme).
