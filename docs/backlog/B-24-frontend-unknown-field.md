---
tags: [typ/story, status/verworfen, bereich/frontend]
aliases: [unknown_field im Frontend]
status: verworfen
prio: P2
art: Frage
quelle: memory/codequalitaet-gates.md
grund: "Alle 34 untypisierten Schreib-Rümpfe in api.ts sind Feld für Feld gegen die Contracts-DTOs
  geprüft (Datei:Zeile-Liste im Verlauf) – jeder trifft exakt die erwarteten Felder, keiner schickt ein
  zusätzliches. Der einzige generische Editor mit Record<string, any>-Zeilen (exerciseConfig.tsx,
  buildTypeConfig) spreadet die lose Zeile nie in die Nutzlast, sondern pickt je Typ benannte Felder
  einzeln heraus – das Restrisiko war theoretisch, nicht real."
---

# B-24 · Frontend gegen `unknown_field` durchspielen

Das Backend lehnt unbekannte Felder ab (`UnmappedMemberHandling.Disallow` → `400 unknown_field`). Ob das
Frontend irgendwo ein Feld schickt, das der Vertrag nicht kennt, ist **nie verifiziert** worden.

**Ungeprüft:** genau das ist die Aufgabe. Ein Durchgang durch alle schreibenden Masken; jeder Treffer ist
ein Formular, das heute still scheitert oder scheitern wird.

## Zuschnitt gekürzt durch B-42 (2026-07-31)

[B-42](B-42-openapi-typen-generieren.md) erzeugt die TypeScript-Vertragstypen aus dem OpenAPI-Dokument; danach
bricht `tsc` bei jedem Feld, das der Vertrag nicht kennt – **sofern die Nutzlast typisiert übergeben wird**.
Der Handdurchgang durch alle schreibenden Masken erübrigt sich damit. Was **bleibt**, ist der Rest, den ein
Generator nicht sehen kann: Stellen, an denen ein Objekt-Literal mit Zusatzfeld untypisiert abgeschickt wird
(kein Typ verlangt es, also meldet `tsc` nichts).

Neuer Zuschnitt dieser Story: **die untypisierten Absende-Stellen finden**, eingereiht **hinter** B-42.
Bewusst nicht verworfen – wer sie streicht, verliert genau diesen Rest aus dem Blick.

## Der Rest ist gezählt (2026-08-01, nach B-42 Schritt 2)

Die Lücke ist keine Vermutung mehr, und sie hat eine einzige Ursache: `http<T>(url, method, body?: unknown)`
in [api.ts](../../frontend/src/lib/api.ts) nimmt den Rumpf als **`unknown`**. Wer ein typisiertes DTO
durchreicht, ist geprüft; wer ein Objekt-Literal an dieser Stelle baut, ist es nicht – `tsc` hat nichts, wogegen
es prüfen könnte.

| Schreib-Aufrufe in `api.ts` | Zahl | Von `tsc` bewacht? |
| --- | --- | --- |
| Rumpf ist ein typisierter Bezeichner (`dto`, `p`, …) | 52 | **ja**, seit B-42 Schritt 2 |
| Rumpf ist ein Objekt-Literal (`{ name }`, `{ adultId, pin }`, …) | **34** | **nein** |

Damit ist auch die Reichweite von B-42 ehrlich benannt: **60 % der Schreibpfade**, alle Lesepfade. Die 34
sind fast alle klein (ein bis drei Felder) – der Zuschnitt ist also „je Aufruf den passenden Vertragstyp
annotieren", nicht „Masken durchklicken". Ob eine generische Signatur (`body?: TBody`) das billiger macht als
34 Annotationen, ist die erste Frage beim Grillen.

Die Zahl war zwischenzeitlich **40**: der `frontend-reviewer` fand sechs der 52 „typisierten" Aufrufe, die
nichts bewachten – vier inline getippte Rümpfe (`dto: { name?: string; … }`, also faktisch Literale) und zwei,
die gegen das **falsche** Schema prüften (`Partial<MissionDef>`/`Partial<AchievementDef>` typisierten den PATCH
gegen die *Antwort*; `id`/`metric`/`period` hätte `tsc` erlaubt und der Server mit `400 unknown_field`
abgewiesen). Alle sechs sind in E6 geschlossen (`UpdateMissionDto`, `UpdateAchievementDto`, `CreateTeacherDto`,
`CreateTimetableEntryDto`, `UpdateChapterDto`, `CreateTagDto` – alle lagen längst im Dokument und fehlten nur im
Barrel). Die 34 sind also nicht mehr geschätzt, sondern der geprüfte Rest.

## Die 34 sind geprüft, keiner schickt ein falsches Feld (2026-08-03)

Jeder der 34 Objekt-Literal-Rümpfe aus [api.ts](../../frontend/src/lib/api.ts) wurde gegen sein
Ziel-DTO in `backend/Pugling.Contracts` abgeglichen (Feldname für Feldname, inklusive optionaler
Felder, die weggelassen werden dürfen):

| `api.ts`-Aufruf (Zeile) | Rumpf | Ziel-DTO |
| --- | --- | --- |
| `loginAdult:225` | `{ adultId, pin }` | `AdultLoginDto(int AdultId, string Pin)` |
| `loginChild:227` | `{ childId, pin }` | `ChildLoginDto(int ChildId, string Pin)` |
| `addChildSupervisor:266` | `{ supervisorId, relation }` | `AddSupervisorDto(int SupervisorId, SupervisorRelation Relation)` |
| `createSubject:297` | `{ name }` | `CreateSubjectDto(string Name)` |
| `updateSubject:299` | `{ name }` | `UpdateSubjectDto(string? Name)` |
| `createCategory:309` | `{ name }` | `CreateCategoryDto(string Name)` |
| `updateCategory:311` | `{ name }` | `UpdateCategoryDto(string? Name)` |
| `createChapter:317` | `{ name, orderIndex }` | `CreateChapterDto(string Name, int OrderIndex)` |
| `setExerciseSharing:338` | `{ executePublic }` | `SetExerciseSharingDto(bool ExecutePublic)` |
| `checkPreviewExercise:343` | `{ answers, stage }` | `PreviewCheckDto(List<PreviewAnswer> Answers, int? Stage)` |
| `addExerciseGrant:355` | `{ creatorId, permission }` | `AddGrantDto(int CreatorId, GrantPermission Permission)` |
| `linkExerciseMedia:364` / `linkExerciseItemMedia:370` / `linkVocabularyMedia:830` | `{ mediaAssetId, weight }` | `AddMediaLinkDto(int? MediaAssetId, string? Key, int Weight)` |
| `attachVocabTags:431` | `{ tags }` | `TagVocabDto(List<string> Tags)` |
| `tagVocabulary:442` | `{ vocabularyIds }` | `TagVocabularyDto(List<int> VocabularyIds)` |
| `startSession:546` | `{}` | `StartPracticeDto(DateOnly? Day, PlayMode Mode = Lern)` – alle Felder optional |
| `heartbeat:549` | `{ seconds, active }` | `HeartbeatDto(int Seconds, bool Active)` |
| `endSession:559` | `{}` | kein `[FromBody]`-Parameter am Controller – Rumpf wird gar nicht deserialisiert |
| `startTest:565` | `{}` | `StartTestDto(int? Stage, DateOnly? Day)` – alle Felder optional |
| `submitTest:571` | `{ answers }` | `SubmitDto(List<AnswerDto>? Answers)` |
| `purchaseSkin:598` / `equipSkin:599` / `purchaseListing:665` / `cancelPurchase:706` / `approveActivation:714` / `rejectActivation:716` / `reshuffleCardImage:845` | `{}` | kein `[FromBody]`-Parameter (nur Routen-Ids) |
| `grantPoints:634` | `{ amount, reason, currency }` | `PointsEntryDto(int Amount, string Reason, Currency Currency)` |
| `assignClassTestExercises:650` | `{ exerciseIds }` | `AssignExercisesDto(List<int> ExerciseIds)` |
| `activateInventory:668` | `{ quantity }` | `ActivateDto(int Quantity)` |
| `setChildInterests:795` | `{ interests }` | `SetChildInterestsDto(List<ChildInterestInput> Interests)` |
| `tagMedia:823` | `{ tags }` | `TagMediaDto(List<string> Tags)` |
| `reshuffleMedia:836` | `{ vocabularyId }` | `ReshuffleMediaDto(int? VocabularyId = null, int? ExerciseItemId = null)` |

34 von 34 – kein einziger Rumpf trägt ein Feld, das sein DTO nicht kennt; wo Felder fehlen, sind sie am
DTO optional (Server-Default greift), nie umgekehrt zusätzlich.

Zusätzlich geprüft: der einzige Ort, an dem eine Nutzlast aus **unbenannten** Feldern (`Row =
Record<string, any>`, [exerciseConfig.tsx:22](../../frontend/src/vater/exerciseConfig.tsx)) entsteht –
`buildTypeConfig` ([exerciseConfig.tsx:123](../../frontend/src/vater/exerciseConfig.tsx)). Trotz der losen
Zeilen-Typisierung (das eigentliche, weiter offene Problem von
[B-74](B-74-editor-zeilen-typisieren.md)) baut jeder `case` sein Ergebnis **feldweise** aus benannten
Literalen (z. B. Zeile 138: `{ prompt: r.prompt, answer: Number(r.answer), tolerance: … }`) – nirgends
wird die rohe Zeile `{ ...r }` in die Config gespreadet. Der Weg zum Server läuft über `payloadFrom`
([ExerciseEditModal.tsx:93](../../frontend/src/vater/ExerciseEditModal.tsx)) bzw. den Aufbau in
[VaterExerciseCreate.tsx:125](../../frontend/src/vater/VaterExerciseCreate.tsx), beide mit explizitem
Rückgabetyp `CreateExercisePayload` – ein Objekt-Literal mit annotiertem Zieltyp prüft `tsc` **auch** auf
Überschuss (`excess property check`), das greift hier also doch. Ein zweiter Fund
(`VaterRewards.tsx:81/166`, `{ ...form, title }`) ist unschädlich, weil `form` selbst als `CreateMissionDto`
bzw. Basis von `CreateAchievementDto` typisiert ist – der Spread trägt keine Fremdfelder.

Die Ausgangsfrage der Story („schickt das Frontend irgendwo ein Feld, das der Vertrag nicht kennt?") ist
damit **abschließend mit Nein beantwortet** – nicht durch Vermutung, sondern durch einen vollständigen
Abgleich aller 34 Stellen plus der einzigen strukturell riskanten Editor-Komponente.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-07-31** — Zuschnitt gekürzt: B-42 nimmt den Handdurchgang ab, hier bleiben die untypisierten
  Nutzlasten. Stufe unverändert `idee` (Entscheidung 4 im Grillen von B-42).
- **2026-08-01** — **entblockt und gezählt.** B-42 Schritt 2 ist abgenommen, die Vertragstypen sind generiert.
  Der verbleibende Rest ist gemessen: 34 von 86 Schreib-Rümpfen sind Objekt-Literale an einem
  `body?: unknown`. Damit ist die Story nicht mehr „ungeprüft" in ihrer Kernbehauptung – nur die Frage, welche
  der 34 tatsächlich ein falsches Feld schicken, ist noch offen. `unverifiziert` bleibt darum stehen.
- **2026-08-03** — geprüft und verworfen: alle 34 untypisierten Schreib-Rümpfe treffen ihr Ziel-DTO
  feldgenau, und der einzige generische Zeilen-Editor spreadet nie roh in die Nutzlast (autonom geprüft,
  Nutzerauftrag 2026-08-04).
