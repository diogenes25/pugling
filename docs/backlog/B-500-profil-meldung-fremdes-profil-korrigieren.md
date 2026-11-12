---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/creator]
aliases: [ProfileFit rät zum Korrigieren eines fremden Profils]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
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

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 42, Retro Sprint 43 im Nachtlauf).
