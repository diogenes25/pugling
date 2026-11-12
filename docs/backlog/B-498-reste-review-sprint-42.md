---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [Freitext-Fach in der Reihenwahl der Konsole]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Review Sprint 42)
unverifiziert: false
entgangen_bei: []
nachgeschaut: 2026-11-12
grund: ""
ersetzt_durch: []
---

# B-498 · Die Konsole entscheidet „Profil passt zum Fach" für Freitext-Profile noch anders als das Briefing

Gefunden im `pugling-reviewer` zu Sprint 42 (🟢, ohne Zählung zurückgestellt). Seit B-495 lehnt `ProfileFit` ein
Freitext-Profil (`SubjectId == null`, `SubjectName` gesetzt) eines anderen Fachs ab. Zwei Stellen davor entscheiden
dieselbe Frage noch nur über die Fach-Id:

1. `SeriesOfChildsBookAsync` (`backend/Pugling.Agent.Creator/AgentCommands.cs:592-606`) zählt für ein Freitext-Profil
   „Latein" alle Bücher des Kindes: bei einem Englisch-Buch wird dessen Reihe gewählt und das Briefing lehnt ab (kein
   Kreis, aber die passende Meldung „kein Lehrbuch in Latein" bei `:602` wird nie erreicht); bei mehreren Büchern bietet
   „wähle mit --series" auch Reihen an, die das Briefing ablehnt. Vorschlag: den Namensvergleich aus
   `ProfileFit.TeachesAnother` als einen Helfer teilen.
2. `profiles --child … --subject X` (`AgentCommands.cs:242`) listet Freitext-Profile eines anderen Fachs als Treffer
   (Server-Filter `CreatorProfileService.cs:57`).

Dazu zwei Reste aus B-495: `ProfileFit` gibt den CLI-Tipp „lass --series/--series-unit weg" auch direkten
Pipeline-Aufrufern; die Summary des B-308-Tests nennt nicht, dass die Plan-Hälfte allein nichts beweist.

## Akzeptanzkriterien

1. Ein Freitext-Profil ohne Reihe wählt nur Bücher seines Fachs; ohne eines meldet es genau das.
2. Die Notiz am B-308-Test nennt, welche Hälfte die Mutante trägt.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Sprint 45 auf Zuruf)

1. **Dieselbe Funktion wie das Briefing** (`ProfileFit.Teaches`) entscheidet in `SeriesOfChildsBookAsync`.
2. **Bewusst nicht:** `profiles --child --subject X` listet weiter Freitext-Profile anderer Fächer (reine Anzeige, der
   Server-Filter ist eine eigene Größe); der CLI-Tipp „lass --series weg" bleibt auch für direkte Pipeline-Aufrufer
   (kosmetisch, die Konsole ist der einzige Aufrufer mit Optionen).

## Verlauf

- **2026-11-12** — angelegt (Review Sprint 42, Nachtlauf).
- **2026-11-12** — Sprint 45: autonom gegrillt, geschätzt S, umgesetzt (Teil 1 und 4; Teil 2 und 3 bewusst nicht).
- **2026-11-12** — Rote Probe: `Freitext_Profil_ohne_Reihe_nimmt_kein_Buch_eines_anderen_Fachs` erwartet rot, gemessen
  rot (Meldung „unterrichtet …" aus dem Briefing statt „kein Lehrbuch"). Mutante B4 (Buchwahl nur nach Fach-Id) rot.
- **2026-11-12** — `pugling-reviewer` (Backend): **ein 🟡** – bei einer Reihe ohne Fach hielt die neue Bevorzugung
  jeden Fachlehrer für unpassend und nahm das fachlose Profil, mit dem der Lauf an „hat kein Fach" scheiterte (das Briefing
  nimmt dort das Fach des Profils). Behoben: bevorzugt wird nur bei einer Reihe mit Fach; Test
  `Nur_mit_Kind_in_einer_Reihe_ohne_Fach_spricht_der_Fachlehrer` (rot vor dem Fix gemessen, Mutante B5). 🟢 ohne Zählung:
  `GetSeriesOrUsageErrorAsync`, „gehört nicht diesem Konto" (auch für besitzerlose Zeilen wahr), der Test der Konsolen-Wahl
  prüft jetzt die Persona im Prompt statt nur den Exit-Code, die Leerzeichen-Annahme im Freitext-Test korrigiert (der
  Server trimmt), README nennt B-498/B-500. Bestätigt: `IsOwn` ist dieselbe Regel wie die PATCH-Sperre.
- **2026-11-12** — Mutanten nach der letzten Änderung: B1–B5 (Backend), F1–F4 (Frontend) – 9/9 rot, Grundlinien grün.
- **2026-11-12** — **abgenommen.** Backend 1410/1410 (Debug), Vitest 789/789, `tsc -b` sauber, Playwright 2/2
  (`full-flow`, `vater-von-null`, Ausweichports). **Review-Zähler Sprint 45: 2.** Rollengang als Maschinengang (Creator:
  Konsole und Pipeline mit `FakeChatClient`; Vater: Komponententests); nicht gefahren: Ollama, Browser.
