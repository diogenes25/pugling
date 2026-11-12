---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [ProfileFit rät zum Korrigieren eines fremden Profils]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 42 in der Retro von Sprint 43)
unverifiziert: false
entgangen_bei: [B-495]
grund: ""
ersetzt_durch: []
---

# B-500 · Die Profil-Meldung rät, ein fremdes Profil zu korrigieren – das darf nur sein Eigentümer

Nachschau von Sprint 42 (frischer `pugling-reviewer` an `b401e92`, per `git archive` exportiert, 1401/1401).

1. **🟡** `backend/Pugling.Agent.Creator/Briefing/ProfileFit.cs:26-28` rät bei einem an die Reihe gebundenen Profil mit
   fremdem Fach „…oder korrigiere das Fach dieses Profils (Vater-Web, Fachlehrer)". Ändern darf nur der Eigentümer
   (`CreatorProfilesController.cs:134-135` → `not_owner`; besitzerlos ist für niemanden änderbar), das Vater-Web zeigt
   bei fremden Profilen nur „fremd" (`VaterFachlehrer.tsx:93-99`). Der zweite Ausweg endet an einer weiteren Sperre.
   Vorschlag: nach `profile.IsOwn` verzweigen (die Antwort trägt es schon); Test mit einem Profil eines zweiten Kontos.
2. **🟢** Entscheidung 4 aus B-495 (Freitext-Fach getrimmt, ohne Groß/Klein) hat keinen Test auf der Gut-Seite – die
   Mutanten „Ordinal" und „Freitext passt nie" überleben vermutlich (gesucht, nicht gemessen).
3. **🟢** Mit nur `--child` wählt `BestProfileForChildAsync` (`AgentCommands.cs:494-497`, `:641`) ein Profil ohne
   `ProfileFit`; passt es nicht, wirft die Meldung dem Nutzer ein Profil vor, das er nicht genannt hat. Kein Kreis
   (`--profile` hilft); passende Profile zuerst wählen.
4. **🟢** `backend/Pugling.Agent.Creator/README.md:178` beschreibt die Fachregel vor B-495 („dasselbe Fach oder keines").

## Akzeptanzkriterien

1. Ein **fremdes** an die Reihe gebundenes Profil mit fremdem Fach rät nicht zum Korrigieren, sondern zu einem anderen
   Profil; ein eigenes weiter zu beidem.
2. Ein Freitext-Profil, dessen Fachname das Fach der Übung ist (getrimmt, ohne Groß/Klein), passt.
3. Mit nur `--child` spricht auf dem Buch des Kindes ein passendes Profil vor einem höher bewerteten unpassenden.
4. Die README beschreibt die Fachregel seit B-495.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Sprint 45 auf Zuruf)

1. **Nach `IsOwn` verzweigen** – die Antwort trägt es aus Sicht des Agent-Kontos; ein besitzerloses Profil ist für
   niemanden eigen und bekommt die fremde Meldung.
2. **Ein geteilter Vergleich `ProfileFit.Teaches`** für Briefing und Konsole (Regel an der Senke, die Konsole leiht sie
   sich nur, um nicht zu raten).
3. **Fällt kein Profil des Buchs**, nimmt die Konsole weiter das beste – das Briefing sagt dann warum.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 42, Retro Sprint 43 im Nachtlauf).
- **2026-11-12** — Sprint 45: autonom gegrillt, geschätzt S, umgesetzt.
- **2026-11-12** — Rote Probe (Debug): erwartet 2 rot + 1 Gegenfall grün, gemessen so – `Fremdes_gebundenes_Profil_…`
  (Meldung ohne „anderen Creator"), `Nur_mit_Kind_spricht_das_passende_Profil_des_Buchs` (Briefing lehnte das Profil ab,
  das die Konsole gewählt hatte); `Profil_mit_freiem_Fachnamen_desselben_Fachs_passt` vorher grün, trägt B2. Der dritte
  Test brauchte ein fachloses passendes Profil: ein passendes mit Fach-Id schlägt ein fachfremdes immer (Fach 4 > Stufe 2)
  – der Fall existiert nur so; die Testvorbereitung prüft die Punkte.
- **2026-11-12** — Mutanten: B1 fremdes wie eigenes, B2 Freitext ordinal, B3 Konsole nimmt besten Treffer – rot.
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
