---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [unknown_field im Frontend]
status: idee
prio: P2
art: Frage
quelle: memory/codequalitaet-gates.md
unverifiziert: true
---

# B-24 · Frontend gegen `unknown_field` durchspielen

Das Backend lehnt unbekannte Felder ab (`UnmappedMemberHandling.Disallow` → `400 unknown_field`). Ob das
Frontend irgendwo ein Feld schickt, das der Vertrag nicht kennt, ist **nie verifiziert** worden.

**Ungeprüft:** genau das ist die Aufgabe. Ein Durchgang durch alle schreibenden Masken; jeder Treffer ist
ein Formular, das heute still scheitert oder scheitern wird.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
