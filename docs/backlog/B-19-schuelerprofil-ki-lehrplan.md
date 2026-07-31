---
tags: [typ/story, status/idee, bereich/training, bereich/frontend, rolle/supervisor]
aliases: [Schülerprofil-Generator]
status: idee
prio: P2
art: Wunsch
quelle: memory/schueler-profil-ki-lehrplan.md
unverifiziert: true
---

# B-19 · Schülerprofil-getriebener KI-Lehrplan

Das Fundament steht (Gender/Interests/ProfileNotes am `Child`, `Textbook`-Entity). Offen sind **drei
Dinge**: der Generator selbst, das Frontend (VaterKind-Editor + Wizard) und ein optionaler read-only
„Generierungs-Brief"-Endpunkt, der Profil und Katalog-Kontext bündelt.

**Voraussichtlich XL — also ein Teilungsfall** (siehe README, „Teilen und Zusammenlegen"): drei Dinge in
einer Notiz. Beim Ausformulieren zuerst prüfen, ob die drei Teile unabhängig auslieferbar sind; wenn ja,
geht diese Id auf `verworfen: geteilt` mit `ersetzt_durch`.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft), als Teilungsfall markiert.
