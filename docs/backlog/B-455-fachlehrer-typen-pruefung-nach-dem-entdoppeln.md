---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/creator]
aliases: [DefaultTypes Reihenfolge]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-10-fertigstellung.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-426]
---

# B-455 · Ob ein unbekannter Übungstyp im Fachlehrer-Profil abgelehnt wird, hängt an der Reihenfolge

`UnknownTypesProblem` prüft erst nach `Clean()`, und `Clean()` entdoppelt ohne Rücksicht auf Groß/Klein, die Registry
vergleicht exakt (`backend/Pugling.Api/Controllers/Creator/CreatorProfilesController.cs:229-238`):
`defaultTypes: ["Cloze","cloze"]` wird angenommen und das unbekannte `cloze` still verworfen, `["cloze","Cloze"]` ist
ein 400. Gespeichert wird nie ein falscher Schlüssel. Vorschlag des Reviewers: die getrimmten Rohwerte prüfen, dann
entdoppeln. Für das Ziel des Fertigstellungs-Laufs nicht tragend, darum `idee`.

## Akzeptanzkriterien

1. **Ein unbekannter Typ-Schlüssel wird abgelehnt, gleich an welcher Stelle der Liste** – auch hinter einer bekannten
   Schreibweise desselben Worts.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-12)

1. **Erst die getrimmten Rohwerte prüfen, dann entdoppeln** (Vorschlag des Reviewers aus Sprint 28); gespeichert wird
   weiter die entdoppelte Liste.

## Verlauf

- **2026-11-10** — angelegt aus der Nachschau zu Sprint 28 (Retro Sprint 29).
- **2026-11-12** — Nachtlauf, Sprint 42: autonom gegrillt, geschätzt XS, umgesetzt (`UnknownTypesProblem`).
- **2026-11-12** — Rote Probe (`CreatorProfileTypeKeyTests`, Theorie mit beiden Reihenfolgen): erwartet 1 rot
  („Cloze","cloze" → 201), gemessen 1 rot; die andere Reihenfolge war vorher schon 400.
- **2026-11-12** — Beleg: AK 1 → `Anlegen_MitUnbekannterSchreibweise_Liefert400_GleichAnWelcherStelle` (P4).
- **2026-11-12** — `pugling-reviewer`: kein 🔴/🟡 – **Review-Zähler Sprint 42: 0.** Bestätigt: alle Wege zum Profil
  (benannt, per Kind im Briefing, `BestProfileForChildAsync`, Klausur- und Planplaner) enden in `ProfileFit`; die Auswege
  beider Meldungen bestehen beide Prüfungen; die Fläche „Vater-Web, Fachlehrer" ändert Fach und Reihe
  (`profilePatch.ts`). 🟢 behoben ohne Zählung: die gebundene Meldung rät zuerst zum Fach („…oder seine Reihe, wenn es zu
  einem anderen Werk gehört"), eine Vorbedingung im Test vor den Aufruf gezogen, der zweite B-455-Fall als
  Absichtserklärung beschriftet. 🟢 zurückgestellt → [B-498](B-498-reste-review-sprint-42.md). Die vier roten Tests im
  Lauf des Reviewers waren die roten Proben von Sprint 43 vor ihrem Fix, nicht dieser Diff.
- **2026-11-12** — Kopie der echten DB: zwei Profile, beide mit Fach-Id passend zu ihrer Reihe (1→5, 4→6), kein
  Freitext-Profil – für die Söhne ändert sich nichts.
- **2026-11-12** — Mutanten nach der letzten Änderung (`mutant-probe.py`, Debug):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | P1 keine Gebunden-Meldung | ProfileFit.cs:24 | red | red |
  | P2 Freitext-Fach passt immer | ProfileFit.cs:41 | red | red |
  | P3 nur gegen Fach der Reihe | ProfileFit.cs:21 | red | red |
  | P4 Typprüfung nach Entdoppeln | CreatorProfilesController.cs:233 | red | red |

- **2026-11-12** — **abgenommen.** Volle Suite 1405/1405 (Debug, mit Sprint 43 im Arbeitsbaum), `CreatorAgentTests` +
  `CreatorProfile*` 148/148 nach der letzten Änderung. **Rollengang (Creator) als Maschinengang:** die Tests fahren
  Konsole und Pipeline gegen die In-Process-API mit `FakeChatClient`; Ollama nicht gefahren.
