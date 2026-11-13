---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Datum UTC-CI, Folgetag UTC+12]
status: abgenommen
prio: P2
art: Defekt
groesse: "S"
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 54 in der Retro von Sprint 56/57)
unverifiziert: false
entgangen_bei: [B-449, B-447]
grund: ""
ersetzt_durch: []
---

# B-517 · Datums-Tests greifen in der UTC-CI nicht, und östlich von UTC+11 steht der Folgetag

Nachschau von Sprint 54 (frischer `frontend-reviewer` an `edaceab`, gelesen, `datum.test.ts` 20/20 in Berlin und UTC).

1. 🔴 `datum.test.ts:28-32`: unter `TZ=UTC` ist ein ohne `Z` als Ortszeit gelesener Zeitstempel derselbe Augenblick –
   die Mutanten D2 („kein `Z`") und D3 („Bruchteil nicht erkannt") bleiben in der CI grün. Gemessen nur in Berlin.
2. 🟡 `formatDateIn` liest einen `DateOnly` als Mittag UTC und formatiert in der Gerätezone – östlich von UTC+11
   (Auckland im Sommer, Kiritimati) steht der Folgetag. Vorher (Textzerlegung) war es in jeder Zone richtig.
3. 🟡 Die drei ursprünglichen ISO-Stellen der Arcade (`MyObjectives`, `SohnPractice`, `SohnProgress`) hält kein Test;
   der Wächter findet nur selbstgebaute Formatierung, nicht rohes ISO.
4. 🟢 `arcadeDatum("2026-11-08", "de", "America/New_York")` kann nicht aus dem genannten Grund rot werden.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. Die reine Text-Regel wird eine eigene Funktion (`alsUtcText`) und als **String** geprüft – das ist zonenunabhängig.
2. Ein `DateOnly` wird überall in UTC formatiert (Vater-Web wie Arcade); der Test mit `Pacific/Kiritimati` hält es.
3. Rohes ISO an den drei Arcade-Stellen fängt ein Wächter über den Quelltext (`ROH` in `datum.test.ts`, mit Selbsttest);
   `MyObjectives` trägt zusätzlich einen Komponententest mit echtem Datum. `SohnPractice`/`SohnProgress` bewusst nur über
   den Wächter (Review: Kurzschreibweisen wie `{ date }` sieht er nicht – benannte Lücke). Die NY-Zeile entfällt.
4. (Review) Wo ein Fehler nur außerhalb von UTC sichtbar ist, setzt der Test die Zone zur Laufzeit (`process.env.TZ`) –
   so misst die UTC-CI dasselbe wie der Berliner Rechner.

## Akzeptanzkriterien

1. „kein `Z`" und „Bruchteil nicht erkannt" machen einen Test auf jeder Maschine rot.
2. `formatDateIn("2026-11-08", "Pacific/Kiritimati") === "08.11.2026"`.
3. Rohes ISO an einer der drei Arcade-Stellen macht einen Test rot.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 54, Retro Sprint 56/57), autonom gegrillt, geschätzt S; in Sprint 59 gebaut.
- **2026-11-13** — gebaut (Sprint 59). `alsUtcText` (Text-Regel als Text geprüft), DateOnly überall in UTC formatiert,
  Wächter `ROH` gegen rohes ISO in der Arcade, Komponententest `MyObjectives` mit echtem Datum. Beim Bau gefunden: der
  committete Arcade-Wächter `SELBST` aus Sprint 54 trug zwei Backspace-Zeichen (U+0008) statt `` – zwei seiner vier
  Formen griffen nie; behoben, mit Selbsttest. Review: vier 🟡 (Zähler 1, eine Fix-Runde): `augenblick`-Test läuft nun
  in New York (Zone zur Laufzeit gesetzt), Arcade-DateOnly auf einem Gerät in Kiritimati geprüft, Kommentar berichtigt,
  Entscheidung 3 an den Bau angeglichen; 🟢 Steuerzeichen-Tor auf `e2e/` und Konfiguration erweitert.
  - AK 1 → „hängt einem Zeitstempel ohne Offset ein Z an" (U1, U2 → rot) und „liest … als UTC" in New York (U9 → rot).
  - AK 2 → Kiritimati in `formatDateIn` (U3 → rot) und auf dem Gerät in der Arcade (U8 → rot).
  - AK 3 → `ROH`-Wächter (U5, U6 → rot) und `MyObjectives` (U4 → rot).
  - Verifikation: Vitest 832/832 (auch `TZ=UTC` für `datum.test.ts`), Playwright 140/140, Mutanten nach der letzten Änderung
    rot. Rollengang Sohn/Vater: E2E gegen echten Server und UI. Abgenommen.
