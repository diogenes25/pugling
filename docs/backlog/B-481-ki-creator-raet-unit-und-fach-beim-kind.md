---
tags: [typ/story, status/abgenommen, bereich/ki, rolle/creator]
aliases: [--child mit --series rät die Unit, zwei Fächer des Kindes ohne Rückfrage]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 33)
unverifiziert: false
entgangen_bei: [B-473]
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-481 · Der KI-Creator rät beim Kind weiter die Unit und das Fach

Nachschau von Sprint 33 (frischer `pugling-reviewer` an `e94aa1e`, gelesen, nicht nachgestellt). B-473 hat das
geratene Regal nur für den Fall „`--child` ganz ohne Ortsangabe" geschlossen; daneben liegen zwei Wege, auf denen der
Agent still falsch ablegt – derselbe Fehler, den B-473 abstellen sollte:

1. **`--child` mit `--series`, ohne `--series-unit`** (`AgentCommands.cs:430`): der Kind-Zweig läuft nur ohne beide
   Angaben, also landet `create --child 1 --series <Access>` in der *ersten* Unit der Reihe statt in der aktuellen
   des Kindes. Hilfetext (`:670`) und README lesen sich, als sei der Fall gelöst.
2. **Zwei Bücher in verschiedenen Fächern** (`AgentCommands.cs:489`): nur ein *exakter* Punktgleichstand führt zur
   Rückfrage. Hat das Englisch-Profil Klassengrenzen und das Latein-Profil keine, gewinnt Englisch mit einem Punkt
   Vorsprung – das Fach wird still gewählt. Theodor hat genau diese zwei Bücher.
3. **Test unscharf**: `Ohne_Profil_zum_Buch_des_Kindes_…` prüft nur `"--profile"` in der Meldung; die
   Gleichstand-Meldung enthält das auch. Unter der Mutante „jedes Profil mit Reihe zählt" bleibt er in der geteilten
   Test-DB grün, sobald genug gleich gute `FreshProfileAsync`-Profile herumliegen – die rote Messung in B-473 hing
   an Reihenfolge und DB-Bestand. Abhilfe: auf „kennt kein Creator-Profil" prüfen.

Naheliegende Abhilfe: (1) mit `childId` und ohne `--series-unit` das Buch des Kindes in der gewünschten Reihe
nehmen; (2) jede zweite *verschiedene* Reihe unter den `series_match`-Treffern ist eine Fehlbedienung mit Liste,
nicht nur ein Gleichstand an der Spitze.

## Akzeptanzkriterien

1. **Mit `--child` und ohne `--series-unit` liegt die Übung in der aktuellen Unit des Kindes** – gleich, ob die Reihe
   aus `--series`, aus `--profile` oder aus dem bestpassenden Profil kommt, solange das Kind ein Buch dieser Reihe mit
   aktueller Unit hat. Ohne ein solches Buch bleibt es bei der ersten Unit der genannten Reihe (der Mensch hat die
   Reihe ausdrücklich gewählt).
2. **Haben Profile auf mehr als einer Reihe der Kinder-Bücher Treffer, ist das eine Fehlbedienung mit Liste** – auch
   wenn ihre Punkte verschieden sind. Mehrere Profile auf *derselben* Reihe entscheiden weiter nach Punkten.
3. **Der Test „kein Profil zum Buch" erkennt seine eigene Meldung** („kennt kein Creator-Profil"), nicht nur
   `--profile`, das auch in der Liste der Mehrdeutigkeit steht.

## Entscheidungen (autonom, `art: Defekt`)

1. **Die Unit des Kindes gilt für jede Reihe, die nicht über `--series-unit` festgelegt ist** – eine Stelle nach dem
   Auflösen der Reihe, nicht drei Zweige. Vor ihr steht `--unit` (die Unit, aus der das Briefing den Stoff nimmt):
   die Übung entsteht, wo ihr Stoff herkommt; das war vorher schon auseinander (Stoff aus Unit 3, Ablage in Unit 1). Kosten: `--child --profile X` legt jetzt ebenfalls in der aktuellen Unit ab
   statt in der ersten; das ist, was der Hilfetext schon versprach.
2. **Zwei Bücher mit Profil heißen „wähle", nicht „nimm das mit mehr Punkten".** Die Punkte messen Klassenstufe und
   Schulart, nicht das Fach, um das es geht; ein Punkt Vorsprung ist keine Antwort auf „Englisch oder Latein".
   Kosten: Theodor braucht für den KI-Creator künftig immer `--profile` oder `--series`, sobald beide Bücher ein
   Profil haben.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 33 im Backlog-Lauf).
- **2026-11-12** — gegen den Code belegt (`AgentCommands.cs:430` Kind-Zweig nur ohne beide Angaben, `:489` Gleichstand nur an der Spitze), autonom gegrillt und auf S geschätzt (Backlog-Lauf, Sprint 35).
- **2026-11-12** — umgesetzt (Sprint 35). Fälle und Belege (`CreatorAgentTests`):
  - AK 1, Reihe aus `--series` / aus `--profile` → `Mit_genannter_Reihe_landet_die_Uebung_bei_der_Unit_des_Kindes`
    (Theory); ein Buch in einer anderen Reihe liegt davor, damit „Buch dieser Reihe" etwas trägt.
  - AK 1, ohne Ortsangabe → weiter `Ohne_Ortsangabe_landet_…` (B-473).
  - AK 2, verschiedene Punkte → `Zwei_Buecher_mit_verschiedenen_Punkten_verlangen_trotzdem_die_Wahl`; die Zusicherung
    im Aufbau belegt, dass die Punkte wirklich verschieden sind. Gleichstand → weiter `Zwei_gleich_gute_Buecher_…`.
  - AK 3 → `Ohne_Profil_zum_Buch_…` prüft jetzt „kennt kein Creator-Profil".
  - Review-Fälle → `Genannte_Unit_geht_vor_der_Unit_des_Kindes`, `Unsichtbares_Kind_mit_Reihe_meldet_eine_Fehlbedienung`.
  - Ungetestet (🟢, gelesen): ohne Buch in der genannten Reihe bleibt es bei der ersten Unit; mehrere Profile auf
    derselben Reihe entscheiden nach Punkten (Sortierung des Servers, `CreatorProfileService.cs:97`).
- **2026-11-12** — `pugling-reviewer`: kein 🔴, zwei 🟡, beide behoben – **Funde 1–2 des Sprints 35 (zählen)**:
  (1) die neue Buch-Abfrage ließ bei einem fremden Kind mit `--series`/`--profile` einen rohen 403/404 durch statt der
  Fehlbedienung des Briefings; (2) `--unit` wurde von der Unit des Kindes überstimmt – Stoff aus der genannten Unit,
  Ablage in der aktuellen. 🟢 Zusammenfassung, Umbruch im Hilfetext und `CLAUDE.md` des Agenten nachgezogen.
- **2026-11-12** — Mutanten nach der letzten Änderung (`c53291c + Arbeitsbaum`, 07:2x–07:3x):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | Unit des Kindes nur ohne Reihe | AgentCommands.cs:441 | red | red |
  | irgendein Buch des Kindes | AgentCommands.cs:477 | red | red |
  | Gleichstand nur an der Spitze | AgentCommands.cs:512 | red | red |
  | jedes Profil mit Reihe zählt | AgentCommands.cs:504 | red | red |
  | `--unit` überstimmt | AgentCommands.cs:439 | red | red |
  | 404 entweicht | AgentCommands.cs:480 | red | red |

  Grundlinie nach dem Zurückspielen grün (`--unit`-Mutante in einem zweiten Lauf, die erste Form `if (false)`
  kompilierte nicht).
- **2026-11-12** — **abgenommen.** Volle Suite nach der letzten Änderung: Backend 1343/1343, Build ohne Warnung.
  Review-Zähler Sprint 35: 2 von 5. **Rollengang (Creator) ohne Live-Lauf**, wie in Sprint 33: `CreatorAgentTests`
  fährt die Konsole mit `FakeChatClient` gegen die In-Process-API; gegen Ollama und die echte DB nicht.
