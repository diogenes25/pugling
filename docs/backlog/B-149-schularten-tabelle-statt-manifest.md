---
tags: [typ/story, status/idee, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [SCHOOL_TYPES handgepflegt, Schularten ohne Manifest, Enum-Kopie im Frontend]
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer im Nachtlauf Sprint 3 (2026-08-10)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-149 · Die Schularten-Liste ist eine handgepflegte Kopie eines Server-Enums

## User Story

Als **Entwickler** möchte ich, dass eine neue Schulart im Server sie auch im Frontend erscheinen lässt —
ohne dass jemand daran denken muss.

## Ist-Stand am Code

`frontend/src/lib/labels.ts:31` hält `SCHOOL_TYPES` als Literal-Liste. Der Typ hilft dabei nicht: das
generierte Schema gibt `SchoolTypes` als `string` heraus (`src/lib/contract.ts`, weil es ein
`[Flags]`-Enum ist), `SCHOOL_TYPES.includes(...)` ist also eine reine **Laufzeit**-Prüfung ohne
Compiler-Netz.

**Seit [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md) ist die Liste erstmals
entscheidungstragend**, nicht nur beschriftend: Das Reihen-Formular fragt sie, um zu entscheiden, ob ein
Wert eine *Kombination* ist und darum als gesperrte Option erscheint. Ergänzt der Server eine Schulart,
zeigt das Formular diesen ganz gewöhnlichen **Einzelwert** als „Kombination" an — unwählbar, obwohl er
wählbar sein müsste.

## Die echte Lücke

Dieselbe Klasse, gegen die die Manifest-Regel für Übungstypen geschrieben ist
(`frontend/CLAUDE.md`: *„Übungstypen kommen aus dem Server-Manifest"*, weil drei Kopien zwangsläufig
auseinanderliefen). Hier ist es eine Kopie statt dreier — aber der Schaden ist neuerdings nicht mehr nur
eine fehlende Beschriftung, sondern ein gesperrtes Bedienelement.

**Kein `entgangen_bei`:** Die Liste ist alt; B-143 hat ihr nur eine Aufgabe gegeben, für die sie nicht
gedacht war.

## Offene Punkte

1. **Woher kommt die Liste künftig?** Drei Wege: ein eigener Endpunkt (wie das Übungstyp-Manifest); das
   bereits generierte OpenAPI-Dokument, falls das `[Flags]`-Enum seine Einzelwerte dort überhaupt
   ausweist (**zuerst nachsehen** — davon hängt ab, ob es geschenkt ist oder einen Endpunkt kostet); oder
   ein Guard-Test, der die Liste gegen das Server-Enum prüft, statt sie zu ersetzen.
   Empfehlung: erst messen, ob das Dokument die Werte trägt. Ist das so, ist es der billigste Weg.
2. **Reicht ein Tor statt einer Ablösung?** Ein Test „`SCHOOL_TYPES` deckt sich mit `SchoolTypes`" wäre
   deutlich kleiner als ein Manifest-Endpunkt und fängt genau den Fall, um den es geht. Die Frage ist,
   ob die Liste noch andere Aufgaben hat, die eine Server-Quelle nicht erfüllen kann (Reihenfolge,
   deutsche Beschriftung).

## Verlauf

- **2026-08-10** — angelegt aus dem Frontend-Review des Nachtlauf-Sprints 3. Nicht im Sprint behoben: der
  Fund betrifft eine Datei, die dieser Sprint gar nicht anfasst, und die Lösung ist eine eigene
  Entwurfsfrage.
