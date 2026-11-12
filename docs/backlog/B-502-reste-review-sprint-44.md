---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Lernstand nach Kinderwechsel, Vokabel aus Übung entfernen kostet Lernstand]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Review Sprint 44)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-502 · Reste aus dem Review von Sprint 44: die Wortliste nach einem Kinderwechsel und das Entfernen einer Vokabel

Gefunden im `frontend-reviewer` zu B-499 (🟢, außerhalb des Diffs bzw. älter, ohne Zählung zurückgestellt).

1. `frontend/src/vater/VaterLernstand.tsx:104-113` liest `words.stale` nicht: nach einem Kinderwechsel steht die (leere)
   Liste des vorigen Kindes, bis die neue kommt – mit „nur schwache" aus harmlos, ohne kurz „Noch kein Lernstand" für das
   neue Kind aus dem Stand des alten. Fix: Leerzeile nur bei `!words.stale`, oder `WeakWords` mit `key={childId}`.
2. `frontend/src/vater/ExerciseEditModal.tsx:570`: eine Vokabel aus einer *unbenutzten* Übung zu entfernen nimmt den
   Lernstand der Kinder daran mit (`ExerciseItem` → `ItemProgress` Cascade); die Frage sagt nur „Vokabel bleibt im Store".
   Dazu die Produktfrage, ob `ItemProgress.ItemId` wie die Historie `SetNull` werden soll – eine Entscheidung, kein Fix.

## Akzeptanzkriterien

1. Nach einem Kinderwechsel steht kein Leerfall aus der Liste des vorigen Kindes.
2. „Vokabel aus der Übung entfernen" nennt den Lernstand, der mitgeht.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Sprint 45 auf Zuruf)

1. **`!words.stale`** an der Leerzeile, nicht `key={childId}` – die Seite behält Filter und Seite beim Wechsel.
2. Der Satz nennt die Folge; die Frage, ob der Lernstand das Löschen überleben soll, ist eine Produktfrage →
   [B-503](B-503-lernstand-ueberlebt-loeschen.md) (`art: Frage`, nicht entschieden).

## Verlauf

- **2026-11-12** — angelegt (Review Sprint 44).
- **2026-11-12** — Sprint 45: autonom gegrillt, geschätzt XS, umgesetzt (`removeItemQuestion`).
- **2026-11-12** — Rote Probe: 2/2 (Leerfall des vorigen Kindes sichtbar; Frage ohne „Lernstand"). Mutanten F1, F4 rot.
- **2026-11-12** — `frontend-reviewer`: zwei 🟡 ohne Verhaltensänderung – ein Kommentar begründete noch den entfernten
  Ausweg (`ExercisePreviewModal.tsx`), und `removeItemQuestion` stand zwischen der Komponenten-Doku und der Komponente
  (Doku hing am falschen Symbol); beide behoben, das Verschieben gezählt. Bestätigt gegen den Server: Entfernen nur ohne
  Plan (`exercise_items_locked`), Lernstand geht per Cascade, Historie unerreichbar, Vokabel bleibt (Restrict).
- **2026-11-12** — Mutanten nach der letzten Änderung: B1–B5 (Backend), F1–F4 (Frontend) – 9/9 rot, Grundlinien grün.
- **2026-11-12** — **abgenommen.** Backend 1410/1410 (Debug), Vitest 789/789, `tsc -b` sauber, Playwright 2/2
  (`full-flow`, `vater-von-null`, Ausweichports). **Review-Zähler Sprint 45: 2.** Rollengang als Maschinengang (Creator:
  Konsole und Pipeline mit `FakeChatClient`; Vater: Komponententests); nicht gefahren: Ollama, Browser.
