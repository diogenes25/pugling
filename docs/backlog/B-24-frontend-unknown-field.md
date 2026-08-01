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

## Zuschnitt gekürzt durch B-42 (2026-07-31)

[B-42](B-42-openapi-typen-generieren.md) erzeugt die TypeScript-Vertragstypen aus dem OpenAPI-Dokument; danach
bricht `tsc` bei jedem Feld, das der Vertrag nicht kennt – **sofern die Nutzlast typisiert übergeben wird**.
Der Handdurchgang durch alle schreibenden Masken erübrigt sich damit. Was **bleibt**, ist der Rest, den ein
Generator nicht sehen kann: Stellen, an denen ein Objekt-Literal mit Zusatzfeld untypisiert abgeschickt wird
(kein Typ verlangt es, also meldet `tsc` nichts).

Neuer Zuschnitt dieser Story: **die untypisierten Absende-Stellen finden**, eingereiht **hinter** B-42.
Bewusst nicht verworfen – wer sie streicht, verliert genau diesen Rest aus dem Blick.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-07-31** — Zuschnitt gekürzt: B-42 nimmt den Handdurchgang ab, hier bleiben die untypisierten
  Nutzlasten. Stufe unverändert `idee` (Entscheidung 4 im Grillen von B-42).
