---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/doku, rolle/creator]
aliases: [Vision-Modell zu schwach, Warnung fill --images]
status: abgenommen
prio: P3
art: Wunsch
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: B-375
grund: ""
ersetzt_durch: []
---

# B-459 · `fill --images` warnt vor dicht bedruckten Buchseiten und verweist auf `lehrwerk-aufbereiten`

Im Rollengang von B-375 (2026-11-10) mit 53 Fotos eines echten Lehrwerks hat `gemma4:e2b` die Zeile des
Inhaltsverzeichnisses zu Unit 2 gelesen. Die Themen stimmten, die Grammatik war falsch („Zustandssätze“
statt will-future und conditional 1/2), und Themen landeten ohne Übersetzung als Wörter. Eine selbst
gerenderte, groß gesetzte Testseite las dasselbe Modell richtig. Vermutlich überfordern dicht bedruckte,
hochauflösende Doppelseiten ein Modell dieser Größe (verkleinert gelesen). Für das echte Lehrwerk hat
deshalb Opus die Seiten ausgewertet, nicht der Agent.

## User Story

Als *Creator* (Vater mit Buchfotos) möchte ich vor `fill --images` erfahren, dass ein kleines lokales
Vision-Modell dichte Buchseiten falsch liest und welcher Weg für ein echtes Lehrwerk trägt, damit ich nicht
unbemerkt falsche Grammatik in die Stoffnotizen einer Unit schreibe.

## Ist-Stand am Code (2026-11-14)

- Der Weg „echtes Lehrwerk aus Fotos" ist anders gelöst: [B-470](B-470-foto-zu-wortliste-als-werkzeug.md)
  (abgenommen) entschied, dass Claude (Opus) die Seiten im Skill `lehrwerk-aufbereiten` liest, mit `kacheln.ps1`
  für dichte Seiten ([B-474](B-474-kacheln-ohne-drehung-und-uebersicht.md)); beides liegt unter
  `.claude/skills/lehrwerk-aufbereiten/` (`SKILL.md`, `kacheln.ps1`). Für diesen Weg ist die Story erledigt.
- Offen bleibt nur `fill --images` des Agenten: der `PageReader` schickt jede Seite ungekachelt an
  `Agent:VisionModel` (`backend/Pugling.Agent.Creator/PageReader.cs:137-162`), Vorgabe `llama3.2-vision`
  (`AgentOptions.cs:33`, `appsettings.json:9`).
- Die Doku warnt nicht: `backend/Pugling.Agent.Creator/README.md:262-287` (Abschnitt „Aus Fotos der Buchseiten“)
  beschreibt Ablauf, Copyright, Formate und Laufzeit, aber keine Lesegrenze des Modells; `.claude/skills/ki-creator/SKILL.md:51-56`
  ebenso. Keiner der beiden Texte nennt `lehrwerk-aufbereiten`.

## Die echte Lücke

Die Lesequalität ist bekannt schlecht für dichte Seiten, und der bessere Weg existiert – aber wer `fill --images`
aufruft, erfährt beides nicht. Die Lücke ist Doku, nicht Code.

## Offene Punkte

1. ~~Reicht ein größeres lokales Vision-Modell?~~ → zurückgestellt (Entscheidung 2).
2. ~~Soll der `PageReader` große Seiten in Kacheln zerlegen?~~ → zurückgestellt (Entscheidung 2).
3. ~~Welche Empfehlung nennen README und Skill?~~ → Entscheidung 1.

## Entscheidungen

1. **Die Story wird auf die Doku-Fassung geschnitten: Warnung in README und Skill `ki-creator`, Verweis auf
   `lehrwerk-aufbereiten`.** Stakeholder-Entscheidung im Auftrag des Nutzers. Inhalt der Warnung: kleine
   lokale Vision-Modelle lesen dicht bedruckte Buch-/Doppelseiten unzuverlässig (gemessen B-375: falsche
   Grammatik, Themen als Wörter); `fill --images` taugt für einzelne, groß fotografierte Seiten und immer nur mit
   `--dry-run` und Abgleich am Foto; für ein ganzes Lehrwerk ist der Skill `lehrwerk-aufbereiten` der Weg.
   Begründung: der Vater will, dass das Kind den *richtigen* Stoff lernt – falsche Grammatik in den Notizen
   wandert in jede erzeugte Übung. Kosten: zwei Absätze Doku; der Agent selbst wird nicht besser.
2. **Zurückgestellt: Modellwechsel und Kacheln im `PageReader`.** Stakeholder-Entscheidung im Auftrag des
   Nutzers. Begründung: der reale Bedarf (echtes Lehrwerk) ist über B-470/B-474 gedeckt; ein Modellvergleich
   braucht Messläufe an echter Hardware mit Ollama und ein Kachel-Umbau im `PageReader` eigene Tests – beides
   ohne aktuellen Nutzer. Kosten: `fill --images` bleibt für dichte Seiten schwach; wer es trotzdem nutzt, ist
   gewarnt. Wird später gebraucht, entsteht eine eigene Story (Kacheln nach dem Muster von `kacheln.ps1`).

## Akzeptanzkriterien

1. `backend/Pugling.Agent.Creator/README.md`, Abschnitt „Aus Fotos der Buchseiten“, trägt einen Absatz, der
   (a) die Lesegrenze kleiner Vision-Modelle bei dicht bedruckten Seiten nennt, (b) `--dry-run` mit Abgleich am
   Foto verlangt und (c) für ein ganzes Lehrwerk auf den Skill `lehrwerk-aufbereiten` verweist. *Prüfbar*:
   `grep -n "lehrwerk-aufbereiten" backend/Pugling.Agent.Creator/README.md` trifft im Abschnitt.
2. `.claude/skills/ki-creator/SKILL.md` trägt im Absatz „Aus Fotos der Buchseiten“ denselben Hinweis in einem
   Satz mit Verweis auf `lehrwerk-aufbereiten`. *Prüfbar* wie AK 1.
3. Kein Code, keine Konfiguration ändert sich (`git diff --stat` zeigt nur die zwei Markdown-Dateien).

## Schätzung

**Größe: XS** – zwei Absätze Doku. Vergleich: kleiner als zwei Sätze in `lib/fieldHelp.ts` samt E2E (B-02, XS),
weil kein Test nötig ist.

- **`migration: nein`**, **`vertragsbruch: nein`** – keine Code-Änderung.

**Risiken**

- Der Kontext-Wächter (`.claude/scripts/context-budget.sh`) misst die dauerhaft geladenen Anweisungsdateien;
  `SKILL.md` lädt nur beim Aufruf – trotzdem kurz halten (ein Satz).
- `old-phrases.py` (falls es die Skill-Texte prüft) nach dem Edit laufen lassen.

**Angriffsplan**

1. README-Absatz unter „Aus Fotos der Buchseiten“, direkt nach dem Ablauf-Absatz.
2. Ein Satz in `ki-creator/SKILL.md`.
3. Querlesen gegen `.claude/skills/lehrwerk-aufbereiten/SKILL.md` (Name und Zweck richtig zitiert).

**Testweg**

- Kein automatischer Test (reine Doku, `art: Wunsch` mit AK als Prüfauftrag): `grep` je AK und Sichtprüfung des
  Abschnitts; Markdown-Lint des Repos, falls im Stop-Hook aktiv.

## Verlauf

- **2026-11-10** — angelegt (Rollengang von B-375).
- **2026-11-14** — Abgleich Backlog-Lauf: veraltet, eingeengt. Der Lehrwerk-Weg liest per Opus (B-470, abgenommen; Kacheln B-474); offen bleibt nur, was `fill --images` empfiehlt bzw. ob der `PageReader` kachelt (Abschnitt „Ist-Stand am Code").
- **2026-11-14** — Refinement Backlog-Lauf: ausformuliert, gegrillt, geschätzt – auf die Doku-Fassung geschnitten (Warnung in Agent-README und Skill `ki-creator`, Verweis auf `lehrwerk-aufbereiten`); Modellwechsel und Kacheln im `PageReader` zurückgestellt (Stakeholder im Auftrag des Nutzers); XS, doku.
- **2026-11-14** — Sprint 76, gebaut (nur Doku): Absatz „Grenze des Modells" im README-Abschnitt „Aus Fotos der Buchseiten" (Lesegrenze mit B-375-Befund, nur einzelne groß fotografierte Seiten, `--dry-run` mit Abgleich am Foto, ganzes Lehrwerk über `lehrwerk-aufbereiten` samt `kacheln.ps1`) und ein Satz in `ki-creator/SKILL.md`; `grep -n lehrwerk-aufbereiten` trifft README:310 und SKILL.md:59; `old-phrases.py` ohne Fund. AK 3 nur eingeschränkt prüfbar: in beiden Dateien liegen parallel die unverbundenen `assign`-Absätze aus B-20.
- **2026-11-14** — **abgenommen** (Sprint 76). volle Suite 1650/1650 (Release, nach den Review-Fixes), Vitest 1079/1079, `tsc -b` grün; Rollengang als Playwright gegen frisch gestartete Server nach dem letzten Edit: 38 passed (Assistent inkl. `assistent-klassenarbeit`, Lehrwerk-/Reihen-/Fachlehrer-Formulare, `vater-von-null`, `kind-anlegen-id`, `uebung-teilspeichern`, `erstbegegnung`); Reviews `pugling-reviewer` + `frontend-reviewer`, Funde im Sprint behoben; Commit `8e970493`. Reine Doku (Agent-README, Skill `ki-creator`); kein Test, kein Rollengang. Kacheln und Modellwechsel bleiben zurückgestellt.
