---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Row typisieren, Record<string any>]
status: idee
prio: P3
art: Aufräumen
quelle: B-69 (Entscheidung 5)
unverifiziert: true
---

# B-74 · Die Zeilen des Übungs-Editors sind `Record<string, any>`

Elf Übungstypen teilen sich im Editor eine Zeile ohne Typ:
`export type Row = Record<string, any>` ([exerciseConfig.tsx:19](../../frontend/src/vater/exerciseConfig.tsx)).
Jedes Formularfeld, jeder Aufbau in `buildTypeConfig` (`:121`) und jeder Rückweg in
`configToEditorState` (`:225`) greift mit einem String-Schlüssel hinein — der Compiler prüft weder den
Namen noch den Typ des Werts.

Sichtbar wurde das beim Grillen von [B-69](B-69-wiederhol-felder-alternativen.md): Dort wechseln fünf
Felder von `string` auf `string[]`, und dieser Wechsel geht **stumm** durch. Ein vergessener Aufrufer
fällt erst zur Laufzeit auf. B-69 sichert sich deshalb mit Rundlauf-Tests ab statt mit Typen — die
Typisierung wäre größer gewesen als der Umbau selbst und hätte den Rückweg aus `unknown` trotzdem
ungetypt gelassen.

**Zu prüfen beim Ausformulieren:** ob eine Union je Übungstyp trägt oder ob die Zeilen je Typ eigene
Interfaces brauchen; was `emptyRow`/`emptyExtra`/`patchRow`/`RowField` daran kostet; und ob der Rückweg
aus der `unknown`-Config sinnvoll typisierbar ist oder eine Prüfung an der Grenze braucht.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-69, Entscheidung 5.
