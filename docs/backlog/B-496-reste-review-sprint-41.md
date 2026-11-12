---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [no_checkable_content Ausweg, nichts nachzuholen ohne Noten]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Review Sprint 41)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-496 · Zwei Nachbarn derselben Fehlerform außerhalb von Sprint 41

Gefunden im `frontend-reviewer` zu Sprint 41 (🟢, außerhalb des Sprint-Diffs, darum nicht gezählt und nicht gebaut).

1. **Ausweg in einer Sperre:** `frontend/src/vater/ExercisePreviewModal.tsx:46-47` sagt bei `no_checkable_content`
   „Du kannst sie zuweisen, aber nicht durchspielen." – Zuweisen hat eigene Sperren (`exercise_not_executable`,
   `exercise_empty`); die Meldungsregel vom 2026-11-07 verbietet den Ausweg über die eigene Sperre hinaus.
2. **Erfolg ohne Messung:** `frontend/src/vater/VaterClassTests.tsx:259` zeigt „Keine Arbeiten mit Note ≥ … – nichts
   nachzuholen. 👍" auch für ein Kind ganz ohne bewertete Klassenarbeit – dieselbe Form wie B-467 (vertretbar wahr, aber
   als Erfolg gelesen).

## Akzeptanzkriterien

1. Der Testmodus nennt bei `no_checkable_content` keinen Ausweg.
2. Der leere Wiederholen-Bereich der Klassenarbeiten meldet keinen Erfolg.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Sprint 45 auf Zuruf)

1. „Diese Übung hat keine einzeln prüfbaren Aufgaben – durchspielen lässt sie sich darum nicht."
2. „Keine Arbeit mit Note ≥ X – aus Klassenarbeiten ist nichts zu wiederholen." ohne 👍: die Antwort kennt die Zahl
   bewerteter Arbeiten nicht, also kein Erfolg; eine neue API-Größe dafür wäre ein Wunsch.

## Verlauf

- **2026-11-12** — angelegt (Review Sprint 41, Nachtlauf).
- **2026-11-12** — Sprint 45: autonom gegrillt, geschätzt XS, umgesetzt (`RepeatPanel` exportiert für den Test).
- **2026-11-12** — Rote Probe: 2/2 (Satz mit „zuweisen"; Leerfall mit „nichts nachzuholen. 👍"). Mutanten F2, F3 rot.
- **2026-11-12** — `frontend-reviewer`: zwei 🟡 ohne Verhaltensänderung – ein Kommentar begründete noch den entfernten
  Ausweg (`ExercisePreviewModal.tsx`), und `removeItemQuestion` stand zwischen der Komponenten-Doku und der Komponente
  (Doku hing am falschen Symbol); beide behoben, das Verschieben gezählt. Bestätigt gegen den Server: Entfernen nur ohne
  Plan (`exercise_items_locked`), Lernstand geht per Cascade, Historie unerreichbar, Vokabel bleibt (Restrict).
- **2026-11-12** — Mutanten nach der letzten Änderung: B1–B5 (Backend), F1–F4 (Frontend) – 9/9 rot, Grundlinien grün.
- **2026-11-12** — **abgenommen.** Backend 1410/1410 (Debug), Vitest 789/789, `tsc -b` sauber, Playwright 2/2
  (`full-flow`, `vater-von-null`, Ausweichports). **Review-Zähler Sprint 45: 2.** Rollengang als Maschinengang (Creator:
  Konsole und Pipeline mit `FakeChatClient`; Vater: Komponententests); nicht gefahren: Ollama, Browser.
