---
tags: [typ/story, status/abgenommen, bereich/lernen, rolle/student]
aliases: [angehängte Lösung ungetrimmt, NBSP in Auswahl, Nachschau-Reste Sprint 40]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: "backend, frontend, .claude/skills/lehrwerk-aufbereiten"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 40 in der Retro von Sprint 41)
unverifiziert: false
entgangen_bei: [B-491, B-490]
nachgeschaut: 2026-11-12
grund: ""
ersetzt_durch: []
---

# B-497 · Nachschau-Reste Sprint 40: die angehängte Lösung ist ungetrimmt, und NBSP macht zwei gleiche Knöpfe

Nachschau von Sprint 40 (frischer `pugling-reviewer` an `b521cd9`, per `git archive` exportiert, 1396/1396). Kein 🔴/🟡;
vier 🟢, die ersten zwei mit Fall:

1. **Die ergänzte Lösung wird nicht getrimmt** (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:242` Wortbank,
   `:495-496` Verstehensfrage). `Tiles` trimmt nur die Einträge des Autors; die Ergänzung kommt roh aus
   `GradedAnswers()`, und der Server trimmt `Gap.Answer`/`Question.Answer` beim Speichern nicht. Lücke `" went"` + Bank
   `["go","gone"]` → Pool `[" went","go","gone"]`: das Leerzeichen sortiert die Lösung immer nach vorn und verrät sie –
   genau was die Sortierung aus B-487 verhindern soll. Bewertet wird richtig (`Normalize` trimmt).
2. **NBSP:** `StageMechanics.Normalize` teilt nur am Leerzeichen, der Editor (`exerciseConfig.tsx:136-139`) an jedem
   Leerraum. Antwort `"in York"` + Möglichkeit `"in York"` mit NBSP: der Editor warnt nicht, der Server hängt die Antwort
   an – zwei gleich aussehende Knöpfe, einer richtig. Ebenso legt `Tiles` NBSP-Dubletten nicht zusammen.
3. **Zweite richtige Kachel über eine Alternative:** `Tiles` legt nur nach `Normalize` zusammen; eine Kachel gleich
   einer `alternative` einer Lücke bleibt eine zweite richtige (B-487 hält das als gewollt fest). Der Lader nennt das auf
   dem `markup`-Weg einen Fehler, bei der handgeschriebenen `wordBank` prüft er nur die Form.
4. **Doku:** `.claude/skills/lehrwerk-aufbereiten/FORMAT.md:128-130` nennt die neue Formregel der `wordBank` nicht;
   `frontend/src/lib/fieldHelp.ts:249-251` (`questionChoices`) sagt „in dieser Reihenfolge" ohne Zusammenlegen und
   Anhängen. Für den Lader gibt es keinen bleibenden Test (B-492).

## Akzeptanzkriterien

1. **Was die Senke ergänzt, kommt getrimmt an** – Wortbank-Lösung wie Verstehensfrage-Antwort; die Sortierung verrät die
   ergänzte Lösung nicht.
2. **Ein geschütztes Leerzeichen ist für den Vergleich ein Leerzeichen:** Kacheln, die sich nur darin unterscheiden, sind
   eine; eine Möglichkeit mit NBSP beantwortet eine Frage mit normalem Leerzeichen (kein angehängter Doppelgänger); der
   Lader legt dieselben Kacheln zusammen.
3. **Der Lader** nennt eine handgeschriebene Wortbank-Kachel, die die Bewertung als *andere Schreibweise* einer Lösung
   annimmt (Alternative), einen Fehler – wie auf dem `markup`-Weg.
4. `FORMAT.md` nennt die Formregel der `wordBank`; `fieldHelp.ts` (`questionChoices`) sagt, was der Server mit der Liste tut.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-12)

1. **`Normalize` trennt an jedem Leerraum**, nicht nur am Leerzeichen – eine Stelle, die Bewertung, Zusammenlegen und
   Ergänzen gleichermaßen trägt (Senke). Die Editor-Warnung tut es schon (`/\s+/`), der Lader (`graded()`) zieht nach.
   **Kosten:** die Bewertung wird an genau einer Stelle nachsichtiger – ein Tab oder NBSP in einer getippten Antwort
   zählt als Leerzeichen; dass ein Kind darüber eine falsche Antwort richtig bekäme, ist ausgeschlossen (Leerraum trennt
   nur Wörter).
2. **Getrimmt wird beim Ergänzen**, nicht beim Speichern: Altbestand bleibt unverändert und heilt beim Lesen; kein
   Vertragsbruch.
3. **Alternative als zweite Kachel:** nur der Lader meldet es (wie bei `distractors`); die Senke lässt eine solche Kachel
   stehen, weil B-487 sie als gültige Schreibweise festhält – ein richtiger Knopf mehr verrät nichts, er ist nur keine
   Ablenkung.

- **2026-11-12** — angelegt (Nachschau Sprint 40, Retro Sprint 41 im Nachtlauf).
- **2026-11-12** — Nachtlauf, Sprint 43: autonom gegrillt (`art: Defekt`), geschätzt S, umgesetzt (`Normalize` an jedem
  Leerraum, Ergänzung getrimmt, Lader `graded()` und Alternativen-Prüfung der eigenen `wordBank`, `FORMAT.md`,
  `fieldHelp.ts`).
- **2026-11-12** — Rote Probe vor dem Fix (Debug): erwartet 4 rot, gemessen 4 rot (`" went"` an Index 0; `" York "`
  angehängt; NBSP-Kachel blieb neben `in York`; NBSP-Möglichkeit bekam einen Doppelgänger). Lader-Probe `probe497.py`
  (Scratchpad): vorher 4 von 6 falsch (N1, N2, A1, A2), danach 6/6; `probe491.py` weiter 7/7; beide echten Lehrwerke
  bestehen `--pruefen`.
- **2026-11-12** — Belege je AK: AK 1 → `Ergaenzte_Loesung_kommt_getrimmt_und_einsortiert` (S2),
  `Ergaenzte_Antwort_kommt_getrimmt` (S3); AK 2 → `Wortbank_legt_Eintraege_mit_geschuetztem_Leerzeichen_zusammen`,
  `Moeglichkeit_mit_geschuetztem_Leerzeichen_genuegt` (S1), Lader N1/N2; AK 3 → Lader A1–A4; AK 4 gelesen.
- **2026-11-12** — Mutanten (`mutant-probe.py`, Debug): S1 `Normalize` nur am Leerzeichen → rot; S2 Lösung roh ergänzt →
  rot; S3 Antwort roh ergänzt → rot. Grundlinie danach grün.
- **2026-11-12** — `pugling-reviewer`: kein 🔴/🟡 – **Review-Zähler Sprint 43: 0.** Geprüft: jeder Aufrufer von
  `Normalize` (Bewertung, `Tiles`, ungeordnete Liste, `DistractorPool`) – aus `Normalize` wird nichts gespeichert
  (Speicher-Schlüssel laufen über `VocabKey`/`InterestSlug`, Lernstand über `ItemId`), die echte DB verliert also keinen
  Schlüssel; Artikel-Nachsicht jetzt auch bei NBSP (gewollt); Editor-`normalizeAnswer` gleich bis auf exotische Zeichen.
  🟢 behoben ohne Zählung: Lader prüft `alternatives` als Liste, Doku-Umbruch, eigener Testabschnitt B-497, Leerzeilen. 🟢
  offen, bewusst: `DistractorPool` gibt ungetrimmte Paare heraus (rotiert, nicht sortiert – verrät nichts); der Lader hat
  keinen bleibenden Test (B-492).
- **2026-11-12** — Mutanten nach der letzten Änderung: S1–S3 rot, Grundlinie grün.
- **2026-11-12** — **abgenommen.** Volle Suite 1405/1405 (Debug), `tsc -b` sauber, Vitest 774/774, Lader-Proben 6/6 und
  7/7, beide echten Lehrwerke bestehen `--pruefen`. **Rollengang (Sohn) als Maschinengang:** die Tests spielen
  Wortbank- und Auswahlkarten über die Karten-Route des Kindes; kein Browser. Creator: Lader offline geprüft.
- **2026-11-12** — Nachschau (Retro Sprint 44): frischer `pugling-reviewer` an `0ffa5e0` (per `git archive`, 1405/1405).
  Geprüft: alle Aufrufer von `Normalize`, `WithoutLeadingArticle` (arbeitet auf normalisiertem Text), Frontend-
  `normalizeAnswer`, Spiel-Frontend (vergleicht nichts selbst), Birkenbihl-Tokenizer, `LetterBoxPattern`, die einzige
  Sortierung von Antworttexten, Python-Spiegel, Lader-Regel gegen den `markup`-Weg, die vier Tests vor dem Fix
  nachgerechnet. Kein 🔴/🟡; sieben 🟢 → [B-501](B-501-nachschau-reste-sprint-43.md).
