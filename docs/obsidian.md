---
tags: [typ/konzept, bereich/doku, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Obsidian, Wissensvernetzung, Wissenskarte]
---

# Obsidian: Wissensvernetzung für Doku & Planung

Wie wir Obsidian über die vorhandene Markdown-Doku legen, damit **Mensch und KI schnell Zusammenhänge
erkennen** (Backlinks, Graph, Tags) und wir – während neue Lerntechniken und Themen dazukommen –
**konsistent bleiben statt Insellösungen** anzuhäufen. Obsidian ist dabei nur ein **Lese-/Denk-Layer**
auf denselben `.md`-Dateien; es ersetzt nichts und legt keine Parallelstruktur an.

## Leitplanken (verbindlich)

1. **GitHub-first bei eingecheckter Doku.** `docs/` und `wiki/` nutzen **relative Markdown-Links**
   (`[Text](pfad.md)`) und Mermaid – sie müssen auf GitHub/im Repo rendern. **Nicht** auf
   `[[wikilinks]]` umstellen. Obsidian indiziert relative MD-Links **trotzdem** für Backlinks/Graph.
2. **Keine Tool-Config im Repo.** `.obsidian/` ist gitignored (lokale Plugins/Layout). Quelle der
   Wahrheit sind die Markdown-Dateien, nicht die Vault-Einstellungen.
3. **Kein zweiter Ablageort.** Obsidian betrachtet dieselben Dateien. Planung/Doku bleiben in
   `docs/`, `wiki/`, `.claude/plans/` und im Memory-Ordner – Obsidian erzeugt keine Kopien.
4. **Wikilinks nur dort, wo sie schon leben:** im **Memory-Ordner** (`MEMORY.md` + Notizen nutzen
   bereits `[[name]]`) und optional in einer separaten, nicht-ausgelieferten Wissensbasis. Nicht in
   den Produkt-Docs.

## Vault einrichten (lokal, einmalig)

1. Obsidian → **„Open folder as vault"** auf den Repo-Ordner `pugling/`.
2. **Settings → Files & Links:**
   - *Use [[Wikilinks]]* → **aus** (wir tippen GitHub-kompatible `[text](pfad.md)`-Links).
   - *Detect all file extensions* → **aus** (nur `.md`/`.canvas` etc.).
3. **Settings → Files & Links → Excluded files:** `node_modules`, `frontend/node_modules`, `bin`,
   `obj`, `TestResults`, `dist`, `.opencode` – sonst indiziert Obsidian tausende Fremd-`README.md`.
4. Der **Memory-Ordner** (`~/.claude/projects/.../memory/`) liegt außerhalb des Repos – bei Bedarf als
   **eigene Vault** öffnen (er nutzt schon `[[ ]]`, der Graph „leuchtet" dort sofort).

## Was das der KI bringt (Tokens sparen, Zusammenhänge sehen)

Claude Code liest den Obsidian-Graph nicht direkt – der Nutzen entsteht durch die **Disziplin**, die
Obsidian erzwingt und sichtbar macht:

- **Gezielt statt alles lesen:** Eine gepflegte **Wissenskarte** (unten) + „Verwandt"-Links am Fuß
  jeder Seite lassen die KI vom Einstieg (`CLAUDE.md`, `MEMORY.md`, diese Karte) in **1–2 Sprüngen**
  zur einzig relevanten Datei springen – statt breit zu grep­pen und viele Dateien zu lesen.
- **Backlinks/Graph decken Lücken auf:** verwaiste Seiten, fehlende Querverweise, doppelte Themen
  (= entstehende Insellösungen) werden sofort sichtbar.
- **Tags bündeln Querschnitt:** „alles zu `#bereich/auswertung`" oder „alle `#lerntechnik/*`" ist ein
  Klick – die KI bekommt denselben Kontext über die Frontmatter-Tags, ohne Volltext-Scan.
- **Optional programmatisch:** der Skill `obsidian-cli` erlaubt es der KI, die Vault gezielt
  abzufragen/zu erweitern (Notiz öffnen/suchen/anlegen) statt Dateien manuell zu durchsuchen.

## Konsistenz-Konventionen (gegen Insellösungen)

Damit spätere Lerntechniken/Themen **schmerzfrei** andocken, gelten dieselben Muster wie heute beim
Vokabeltraining:

### a) Frontmatter-Tags (Bereich + Rolle + Doku-Typ)

Jede neue Doku-Seite bekommt oben eine kleine YAML-Frontmatter (rendert auf GitHub als Tabelle,
in Obsidian als Tags):

```yaml
---
tags: [typ/konzept, bereich/auswertung, rolle/supervisor, lerntechnik/vokabeln]
aliases: [Lernstand-Hierarchie]
---
```

- **`bereich/…`** – Architektur-Bereich: `katalog`, `training`, `auswertung`, `punkte`,
  `gamification`, `auth`, `shop`, `frontend`, `doku`.
- **`rolle/…`** – fachlicher Blick: `creator` (Inhalte/Katalog), `supervisor` (Steuerung/Kontrolle),
  `student` (Lernen/Einlösen). Rollenübergreifende Seiten tragen mehrere Rollen-Tags.
- **`lerntechnik/…`** – fachliche Technik: `vokabeln` (heute), später z. B. `karteikarten`,
  `lueckentext`, `rechnen` … – **eine** neue Tag pro Technik, nie ein paralleler Doku-Baum.
- **`typ/…`** – Doku-Art: `konzept`, `referenz`, `tutorial`, `plan`, `adr`.

Die **Evergreen-Doku** (`wiki/01–09` + die konzeptionellen/Referenz-Seiten in `docs/`) ist bereits getaggt.
**Ausgenommen:** generierte Dateien (`docs/api-examples/*`, von `DocsCaptureTests` geschrieben) und
Protokolle/Pläne (`pm-sitzung-*`, `e2e-protokoll`, `code-review`, `vokabeltraining-prozess`, `*-plan`) –
Letztere bei Bedarf mit `typ/log` bzw. `typ/plan` nachziehbar.

### b) Neue Lerntechnik = gleiches Muster, kein neuer Stack

Eine neue Technik ist ein **`ExerciseType`** im bestehenden Katalog, kein Parallel-System:

- Übung erben aus `ExerciseControllerBase<TConfig>`, Route `learn/subjects/{}/chapters/{}/<typ>`.
- Ins Training über **`PlanPosition`** (Ziel/Punkte/Leitner), nicht über eine Sonderentität.
- Punkte **nur** über `ScoringService`, Fortschritt über die **bestehenden** Spuren
  (`PositionItemProgress` + `ItemProgress`) – nicht neu erfinden.
- Doku nach demselben Schnitt (`bereich/…` + `lerntechnik/…` taggen, in die Wissenskarte eintragen,
  „Verwandt"-Links setzen).

Anleitung/Checkliste: [wiki/08 · Erweitern](../wiki/08-erweitern.md) und der Skill `/neuer-uebungstyp`.

### c) „Verwandt"-Fußzeile

Jede Konzept-Seite endet mit einer kurzen **Verwandt**-Liste (relative Links) auf ihre Nachbarn –
das ist der Backlink-Klebstoff des Graphen.

## Wissenskarte (Map of Content)

Der Graph-Hub: Bereich → maßgebliche Seite(n). Von hier aus ist jede Domäne in einem Sprung erreichbar.

| Bereich | Einstieg |
| --- | --- |
| Gesamtbild / Konzepte | [wiki/01 · Überblick & Architektur](../wiki/01-ueberblick-architektur.md) · [CLAUDE.md](../CLAUDE.md) |
| **Rollen-Einstieg** (Creator/Supervisor/Student) | [docs/rollen-doku.md](rollen-doku.md) |
| **Grundprinzip** (Creator→Vater→Kind) | [docs/grundprinzip.md](grundprinzip.md) |
| **Endpunkt-Beziehungen** (Übung→Lehrplan→Kind→Auswertung) | [docs/endpunkt-beziehungen.md](endpunkt-beziehungen.md) |
| Endpunkt-Index | [wiki/07 · API-Referenz](../wiki/07-api-referenz.md) |
| Katalog & Übungstypen | [wiki/03 · Übungstypen](../wiki/03-uebungstypen.md) |
| Training / Lehrplan bauen | [wiki/04 · Lernplan bauen](../wiki/04-lernplan-bauen.md) |
| Auswertung / Lernstand | [docs/endpunkt-beziehungen.md § 3](endpunkt-beziehungen.md#3-übung--auswertung-des-kindes) |
| Punkte & Gamification | [wiki/05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md) |
| Auth & Rollen | [wiki/02 · Authentifizierung](../wiki/02-authentifizierung.md) |
| Erweitern (neue Technik) | [wiki/08 · Erweitern](../wiki/08-erweitern.md) |
| Vokabel-Details (Entwickler) | [docs/vokabel-funktionalitaeten-entwickler-tutorial.md](vokabel-funktionalitaeten-entwickler-tutorial.md) |
| KI-Gedächtnis | `MEMORY.md` + Notizen im Memory-Ordner (eigene Vault) |

## Skills installieren

Die Obsidian-Skills (steuern Claudes Verhalten, liegen user-global unter `~/.claude/skills`):

```bash
npx skills add kepano/obsidian-skills@obsidian-markdown -g -y   # Obsidian-Markdown-Konventionen
npx skills add mattpocock/skills@obsidian-vault -g -y           # Arbeiten in der Vault
npx skills add kepano/obsidian-skills@obsidian-cli -g -y        # (optional) Vault programmatisch abfragen
```

Quelle/Details: <https://skills.sh/kepano/obsidian-skills> · <https://skills.sh/mattpocock/skills>.

---

**Verwandt:** [endpunkt-beziehungen.md](endpunkt-beziehungen.md) ·
[wiki/01 · Überblick](../wiki/01-ueberblick-architektur.md) ·
[wiki/08 · Erweitern](../wiki/08-erweitern.md) · [CLAUDE.md](../CLAUDE.md)
