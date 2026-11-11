---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Access-Daten aufräumen, laden.py Kind]
status: abgenommen
prio: P2
art: Aufräumen
groesse: XS
wo: "doku"
migration: nein
vertragsbruch: nein
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-464 · Der Access-Datenstand trägt Trümmer des ersten Laufs, und das Kind zeigt auf das falsche Buch

## User Story

Als Vater möchte ich, dass im Access-Datenstand nur geprüfte Übungen stehen und das Kind auf das Buch zeigt,
mit dem es lernt, damit der KI-Creator den richtigen Stoff liest und ich keine kaputten Übungen aus Versehen
zuweise.

## Ist-Stand am Code

Funde in `pugling.db` (nur gelesen, 2026-11-10):

- **Erfundene Wortpaare im Vokabelspeicher** aus dem Lauf vor B-460 (Ids 158–166), teils falsch:
  `post → Brief` (im Buch: Blog-Eintrag), `figure → Person`, `cathedral → Dom` als Dublette zu
  `cathedral → Kathedrale, Dom`. Sie hängen an Übung 24 („Wortpaare von Brawl Stars, Pokémon und Fußball“).
- **Übung 25** („London Reisebericht“) wertet ihre Ablenker als Lösung
  ([B-461](B-461-lueckentext-ablenker-zaehlen-als-loesung.md)) und steht weiter im Katalog und in Plan 2.
- **Zwei inaktive Lehrpläne gleichen Titels** (2 und 3).
- **Das Kind „Sohn“** hat weder Klasse noch Schulart, und sein Unterrichtsmaterial ist „Green Line 1, Unit 1 –
  Greetings“ aus dem Seed. Der Fachlehrer-Treffer für das Access-Profil zählt deshalb nur „gleiches Fach“
  statt „gleiche Buchreihe“. Ein Agentenlauf ohne `--series/--unit` liest den Stoff des falschen Buchs, denn
  `ChildFacts.PrimaryTextbook` nimmt das erste Buch des Fachs. Verwandt: [B-445](B-445-seed-kind-ohne-klasse-und-fachlehrer.md).
- **Die Klassenarbeit** „Vokabeltest Unit 1“ am 2026-11-20 ist die des Seeds (Thema „Begrüßungen“) und hängt
  an keiner Access-Übung.
- Der Vokabel-Hinweis `Druck verblasst, aber lesbar` u. ä. wird vom Ladeskript gefiltert; `(infml)` kommt als
  einziger Hinweis beim Kind an. Das ist beabsichtigt (`hint()` in `laden.py`), kein Fund.

## Entscheidungen

1. **Aufgeräumt über die API, nicht in der Datenbank.** Gelöscht wird mit denselben Endpunkten, die das
   Vater-Web nutzt, damit Eigentum und Abhängigkeiten geprüft werden.
2. **Das vorhandene Englischbuch des Kindes wird zum Access-Buch umgebogen**, statt ein zweites anzulegen. Ein
   zweites Buch neben Green Line bliebe für den Agenten unsichtbar, weil er das erste Buch des Fachs nimmt.
3. **Plan 3 bleibt stehen.** Er trägt Übung 28, den Beleg für [B-465](B-465-ki-lueckentext-mehrdeutig-und-verraten.md).
   Damit gibt es weiter zwei inaktive Pläne gleichen Titels (3 und 4); sie unterscheiden sich in der Beschreibung.
4. **Kind und Klassenarbeit setzt das Ladeskript**, idempotent. Ein frisch geladener Datenstand ist damit
   von Anfang an richtig, nicht nur dieser.

## Akzeptanzkriterien

1. Übungen 24 und 25, Plan 2 und die Speicher-Einträge 158–166 sind entfernt.
2. Das Ladeskript setzt am Kind Klasse 7, Gymnasium und das Access-Buch mit Unit 1.
3. Die Klassenarbeit zeigt auf die Wortlisten der Unit.
4. Ein zweiter Lauf des Ladeskripts ändert nichts mehr.

## Schätzung

XS: Daten und `raw/english/access-pugling/laden.py` (gitignoriert, Copyright), kein Produktcode.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-11** — umgesetzt auf Nutzerauftrag („Ja, aufräumen“). Belege, gelesen über die laufende API:
  - `DELETE` auf Plan 2, Übung 24 (`…/units/7/vocabulary/24`), Übung 25 (`…/units/7/cloze/25`) und die
    Speicher-Einträge 158–166: je `204`, danach `404`. Vorher geprüft: 158–166 hingen nur an Übung 24.
  - `laden.py` neu mit `--child` (Vorgabe 1): Kind 1 hat Klasse 7 und Gymnasium. Buch 1 zeigt auf
    „English G Access G9 – Band 3“, Unit „Unit 1 This is London“. Klassenarbeit 1 hat das Thema
    „Access Unit 1 – This is London: Wörter Part A–C und Text“ und 4 direkt zugeordnete Übungen (13–16).
  - Zweiter Lauf: keine neue Übung, die Zuordnung zur Klassenarbeit bleibt bei 4.
  - `profiles/match?childId=1&subjectId=1`: Profil 1 mit 15 Punkten, Gründe `series_match`, `subject_match`,
    `grade_in_range`, `school_type_match`.
- **2026-11-11** — abgenommen. Ein Code-Commit fehlt: Es änderten sich nur Daten und das gitignorierte
  Ladeskript. Committet ist nur diese Datei. Einen Rollengang im Vater-Web gab es **nicht**. Geprüft wurde nur
  über die API.
- **2026-11-11** — Korrektur: Ein Agentenlauf mit `--child 1`, aber ohne `--profile`/`--series-unit`, liest auch
  mit dem umgestellten Buch nicht Access; die Ablage nimmt dann die erste Reihe überhaupt
  ([B-473](B-473-ki-creator-ablage-ignoriert-buch-des-kindes.md)). Mit `--profile 1` stimmt der Stoff. Das
  Ladeskript ist inzwischen buchunabhängig im Skill `lehrwerk-aufbereiten`
  ([B-470](B-470-foto-zu-wortliste-als-werkzeug.md)); Kind und Klassenarbeit setzt dort `--child`.
