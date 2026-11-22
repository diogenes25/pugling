---
tags: [typ/story, status/idee, bereich/vater-web, rolle/supervisor]
aliases: [Geleerte Felder gehen als null hinaus (Plan, Unit, Vokabel)]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Review von B-789 im Nachtlauf A 2026-11-22 (docs/pm-sitzung-2026-11-22.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
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

## Verlauf

- **2026-11-22** — angelegt aus dem Review von B-789 (`idee`); bewusst nicht in B-789 (anderer Bereich, eine Stelle mit
  Vertragsfrage).
