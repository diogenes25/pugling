---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Nachschau-Reste Sprint 60-62]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 60–62)
unverifiziert: false
entgangen_bei: [B-520, B-493, B-501, B-492]
grund: ""
ersetzt_durch: []
---

# B-521 · Nachschau-Reste Sprint 60–62: eine Zone, die nicht zurückkommt, und Lücken der Fallliste

Nachschau von `e37f65e` (frischer `pugling-reviewer` und `frontend-reviewer`, gelesen; Vitest 102/102). Kein 🔴.

1. 🟡 `datum.test.ts` `inZone`: unter Windows setzt `delete process.env.TZ` Node nicht zurück – der Rest der Datei rechnete in
   der zuletzt gesetzten Zone (gemessen, Node 22.17).
2. 🟢 Fallliste ohne ‘, ʼ, `` ` `` und ohne Groß/Klein außerhalb von ASCII.
3. 🟢 `AnswerVariants.IsOnePhrase` trennte das letzte Wort nur am Leerzeichen („Gebäude" + NBSP + „usw.").
4. 🟢 CI: ein roter Python-Schritt vor `dotnet test` verdeckte das Ergebnis der Suite.
5. 🟢 `answerNormalize.test.ts` liest die Liste relativ zu `process.cwd()` – in CI richtig (`working-directory: frontend`),
   aus dem Repo-Wurzelverzeichnis nicht; `import.meta.url` ist in der happy-dom-Umgebung kein `file:`-URL. Offen.

## Entscheidungen

1. `inZone` merkt die wirksame Zone und setzt sie vor dem `delete` zurück; ein Test prüft die Zone danach.
2. Vier Fälle in der Liste (21 statt 17); alle drei Leser grün.
3. `Regex.Split` an jedem Leerraum. 4. Python-Schritt hinter `dotnet test`, mit `if: !cancelled()`.

## Verlauf

- **2026-11-13** — angelegt und gebaut (Sprint 63, Tageslauf). S1 (alte Wiederherstellung) → rot; S2 (Leerzeichen-Split) →
  rot; Python 4/4 grün mit 21 Fällen. Abgenommen mit dem Commit des Sprints.
