---
tags: [typ/story, status/abgenommen, bereich/vater-web, rolle/supervisor]
aliases: [Geleerte Felder gehen als null hinaus (Plan, Unit, Vokabel)]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Review von B-789 im Nachtlauf A 2026-11-22 (docs/pm-sitzung-2026-11-22.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: "2026-11-22"
---

# B-791 · Drei Vater-Formulare leeren ein Feld nicht, sondern lassen es beim Server stehen

Dieselbe Falle wie [B-789](B-789-angebotstitel-leeren-bleibt-stehen.md) außerhalb des Shops: das Formular schickt ein
geleertes Textfeld als `null`, und der PATCH liest `null` als „unverändert“. Die Oberfläche meldet Erfolg, der alte Wert
bleibt stehen. Gefunden im Review von B-789 (🟢, außerhalb des Diffs), am Code gelesen, nicht im Browser gesehen:

1. **Plan-Beschreibung** – `frontend/src/vater/VaterPlanDetail.tsx:158` schickt `form.description.trim() || null`;
   `StudyPlansController.cs:232` übernimmt nur, was nicht `null` ist. Der Kommentar an `:157` („auch Leeren erlaubt → null“)
   stimmt nicht.
2. **Unit-Stoffnotizen** – `frontend/src/vater/VaterLehrwerke.tsx:803-804` (`grammar`, `vocabularyNotes`) beim Bearbeiten;
   `SeriesUnitsController.cs:136-137` überspringt `null`.
3. **Vokabel-Store** – `frontend/src/vater/VaterVocab.tsx:419-420` (`baseFormRelation`, `pronunciationAudioUrl`);
   `VocabularyStoreController.cs:345`/`:364` überspringen `null`. Achtung: `:345` übernimmt `""` ungetrimmt als URL – hier
   braucht es eine eigene Entscheidung (Schalter oder `""` → `null`).

Mögliche Reparatur (zu entscheiden je Stelle): `""` schicken, wo der Server `""` schon als Leeren liest (1, 2); bei der
Audio-URL einen `Clear…`-Schalter nach `backend/CLAUDE.md` („PATCH-Semantik“).

## User Story

Als Vater (und Creator) möchte ich, dass ein Feld, das ich in einem Bearbeiten-Formular leere und speichere, danach
wirklich leer ist, damit „gespeichert" stimmt und ich keine alte Beschreibung, Stoffnotiz oder Audio-Adresse behalte,
die ich loswerden wollte.

## Ist-Stand am Code (2026-11-22, Lauf 2026-11-23-a)

- **Plan:** `PlanEditForm` (`frontend/src/vater/VaterPlanDetail.tsx:158`) schickt `form.description.trim() || null`, nur wenn
  geändert. `StudyPlansController.cs:232`: `if (dto.Description is not null) plan.Description = … Trim() is { Length: > 0 } d ? d : null`
  – `""` leert also schon heute.
- **Unit:** `UnitForm` (`VaterLehrwerke.tsx:803-804`) baut **ein** DTO für Anlegen und Bearbeiten (`:816-818`) mit
  `grammar`/`vocabularyNotes: … || null`. `SeriesUnitsController.cs:136-137` überspringt `null`; `Trimmed` (`:186`) macht
  aus `""` ein `null` – `""` leert also schon heute, beim Anlegen (`:94-95`) ist `""` gleichwertig zu `null`.
- **Vokabel:** `VocabRow.save` (`VaterVocab.tsx:419-420`) schickt `baseFormRelation`/`pronunciationAudioUrl` als `|| null`.
  `VocabularyStoreController.cs:366-367` leert die Relation mit `""` schon heute; die Audio-URL dagegen übernimmt jeden
  Nicht-`null`-Wert **ungetrimmt** (`:345`), ebenso das Anlegen (`:293`). Ein `""` stünde dann als leerer String in der DB.
  Leser der URL: `ExerciseContentResolver.cs:102` reicht sie als `AudioUrl` an die Karte, `:108` prüft `IsNullOrWhiteSpace`
  (Sprachausgabe als Ersatz), das Sohn-Web `Lektion.tsx:108` prüft auf Wahrheit. Batch-Update (`:564`) läuft durch
  denselben `UpdateCoreAsync`.
- **Breiter gesucht** (`grep "trim() || null"` in `frontend/src/vater`, 30 Treffer): alle übrigen sind Anlege-Formulare
  (`ChildMaterialSection:330`, `VaterFachlehrer:301-304` im Anlege-Zweig, `VaterLehrwerke:993`, `VaterZiele`,
  `VaterClassTests:76`, `VaterPlanCreate`, `VaterExerciseCreate`, `VaterWizard`, `VaterRewards`, `VaterLogin`), tragen
  schon einen `clear…`-Schalter (`ClozeTexts:176-178`) oder gehen in einen Vollersatz-PUT (`ExerciseEditModal.payloadFrom`,
  wo `null` leert). Die Substantiv-/Verb-Felder der Vokabel (`VaterVocab.tsx:424-428`) ersetzen das ganze Objekt
  (`VocabularyStoreController.cs:343-344`) – dort leert `null`. Kein weiterer Fall.

## Die echte Lücke

Drei Client-Stellen, die `""` zu `null` machen, wo der Server `""` als Leeren liest – und eine Server-Stelle (Audio-URL),
die `""` nicht normalisiert, sodass die Reparatur im Client allein einen zweiten Zustand für „keine Adresse" erzeugte.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-a (Defekt, autonom grillbar, Freigabe 1), unbestätigt.*

1. ~~Clear-Schalter oder `""`?~~ **Plan und Unit: das Formular schickt das getrimmte Feld, auch leer (`""`).** Der Server
   leert damit schon (siehe Ist-Stand); Vorbild B-789 E1. Verworfen: `ClearDescription`/`ClearGrammar`/… – vier neue
   Vertragsschalter für etwas, das `""` heute leistet. Kosten: die Regel „leer heißt `""`" steht nur am Code; AK 5 hält sie.
2. ~~Audio-URL: Schalter oder `""` → `null`?~~ **Der Server normalisiert eine leere oder nur aus Leerzeichen bestehende
   Audio-URL zu `null` – beim Anlegen und beim Ändern**, wie er es im selben Controller für die Grundform-Relation schon tut
   (`:292`, `:366-367`); das Formular schickt `""`. Begründung: kein Vertragsbruch, Batch-Update und Agent bekommen es
   mit, und die DB kennt nur einen Zustand für „keine Adresse". Verworfen: `ClearPronunciationAudioUrl` (Vertrag,
   Client-Typen, Batch-DTO wachsen) und „`""` speichern" (zwei Formen von leer; jeder Leser müsste beide kennen).
   Eine gesetzte URL wird dabei getrimmt (wie die Relation). Kosten: eine URL aus Leerzeichen, die ein Creator absichtlich setzt, gibt es nicht mehr – sie war nie abspielbar.
   Bereits gespeicherte Werte: der Client schickte bisher nie `""`, eine Altlast ist darum nicht zu erwarten; keine Migration.
3. **Grundform-Relation im selben Zug** (gleiche Zeile, gleicher Fehler, Server leert mit `""` schon).
4. **Umfang bleibt bei diesen drei Formularen** – die breitere Suche (Ist-Stand) fand keinen weiteren Fall.

## Akzeptanzkriterien

1. Plan bearbeiten, Beschreibung leeren, Speichern → das DTO trägt `description: ""`. Rot: `|| null`.
2. Unit bearbeiten, Grammatik und Wortschatz leeren, Speichern → der PATCH trägt `grammar: ""` und `vocabularyNotes: ""`.
   Rot: `|| null` je Feld.
3. Vokabel bearbeiten, Relation und Audio-URL leeren, OK → der PATCH trägt `baseFormRelation: ""` und
   `pronunciationAudioUrl: ""`. Rot: `|| null` je Feld.
4. Server: PATCH `pronunciationAudioUrl: "  "` leert die URL (Antwort `null`), `null` lässt sie stehen; Anlegen mit `""`
   speichert `null`. Rot: Update ohne Normalisierung; Anlegen ohne Normalisierung.
5. Festschreibung am Server: PATCH `description: ""` leert die Plan-Beschreibung, PATCH `grammar: ""`/`vocabularyNotes: ""`
   die Stoffnotizen der Unit; `null` lässt jeweils stehen. Rot: der Controller überspringt `IsNullOrWhiteSpace`.

## Angriffsplan

1. Server: Audio-URL beim Anlegen und Ändern normalisieren (ein Helfer im Controller) – Fakten für AK 4 und 5 in
   `PatchClearFieldTests`.
2. Client: drei Stellen auf das getrimmte Feld umstellen; `VocabRow` für den Test exportieren.
3. Vitest je Formular (AK 1–3); E2E `vater-felder-leeren` als Rollengang (Plan-Beschreibung, Unit-Grammatik,
   Vokabel-Audio, je nach dem Neuladen leer).

**Risiken:** gering – kein Vertrag, keine Migration. Das Vokabel-Formular schickt die Felder bei **jedem** Speichern
mit; `""` statt `null` heißt dort „leer setzen", was der angezeigte (leere) Stand ohnehin ist.

**Testweg:** Vitest (`src/vater`), `dotnet test` (Release), E2E auf Ausweichports 5211/5184.

## Verlauf

- **2026-11-22** — angelegt aus dem Review von B-789 (`idee`); bewusst nicht in B-789 (anderer Bereich, eine Stelle mit
  Vertragsfrage).
- **2026-11-22** — ausformuliert (breiter gesucht: kein vierter Fall), gegrillt (stellvertretend, Defekt, Entscheidung 2 zur Audio-URL) und geschätzt (S, beides, kein Vertrag) im Nachtlauf A; in-arbeit in Sprint 115.
  - Rote Probe vor dem Fix: `PatchClearFieldTests.B791_*` erwartet 1 rot (AK 4) und 1 grün (AK 5 ist Festschreibung),
    gemessen 1 rot / 1 grün.
  - AK 1 → `planSubject.test.tsx` „schickt eine geleerte Beschreibung als leeren Text", T1 (`|| null`) → rot.
  - AK 2 → `VaterFelderLeeren.test.tsx` „eine Unit schickt geleerte Grammatik …", T2 (Grammatik) → rot, T3 (Wortschatz) → rot.
  - AK 3 → `VaterFelderLeeren.test.tsx` „eine Vokabel schickt geleerte Relation …", T4 (Relation) → rot, T5 (Audio) → rot;
    E2E `vater-felder-leeren`, E1 (= T5) → rot.
  - AK 4 → `PatchClearFieldTests.B791_Leere_Audio_Url_Wird_Null_Beim_Anlegen_Und_Aendern`, M1 (PATCH ohne Normalisierung)
    → rot, M2 (Anlegen ohne Normalisierung) → rot, M7 (gesetzte URL ungetrimmt, Review-🟢) → rot.
  - AK 5 → `PatchClearFieldTests.B791_Leerer_Text_Leert_Planbeschreibung_…`, M3 (Plan überspringt Leeres) → rot, M4
    (Grammatik) → rot, M5 (Wortschatz) → rot.
  - Messung mit `.claude/scripts/mutant-probe.py` nach der letzten Änderung, Grundlinien grün; 15 von 15 rot (M1–M7,
    T1–T6, E1–E2 zusammen mit B-800).
- **2026-11-22** — Review: `pugling-reviewer` kein 🟡/🔴; 🟢 Trimmen ungetestet → Zusicherung und M7 ergänzt, 🟢 Trimmen nicht
  in Entscheidung 2 → ergänzt (beide ohne Zählung). `frontend-reviewer`: 🟡 geleerter Band einer Unit (Zahlenfeld, außerhalb
  des Diffs, ohne Zählung) → [B-800](B-800-band-einer-unit-leeren-bleibt-stehen.md), im selben Sprint gebaut; der Satz „Kein
  weiterer Fall" galt nur für Textfelder. 🟢 E2E-Labels ohne `exact` → nachgezogen.
- **2026-11-22** — abgenommen (`d9af0c17`, Sprint 115). Verifikation: Backend 2028/2028 (Release, nach der letzten Änderung),
  Vitest `src/vater` 549/549, `dotnet format Pugling.sln --verify-no-changes` sauber. Rollengang Vater per E2E
  `vater-felder-leeren` gegen frisch gestarteten Server (5211/5184, Wegwerf-DB): Plan-Beschreibung, Unit-Grammatik, Band und
  Audio-URL nach dem Neuladen leer. Sohn Regressionszeuge (`full-flow` grün; die Audio-URL erreicht die Karte nur gesetzt
  oder `null`). Creator Regressionszeuge (Vokabel-Store und Units sind Creator-Fläche; dieselbe E2E fährt sie).
- **2026-11-22** — nachgeschaut (Retro Sprint 116). Prüfpunkte: `d9af0c17` sendet `clearGrade` nur im Bearbeiten-Zweig
  (`VaterLehrwerke.tsx:819`), das Anlegen bekommt das DTO ohne den Schalter (sonst `unknown_field`); der Agent
  (`UnitNotesFiller.cs:142`) schickt `null` und lässt damit alles stehen, was er nicht setzt; `AudioUrlOrNull` ist der
  einzige Schreiber der Audio-URL (Review-Befund). Kein Befund.
