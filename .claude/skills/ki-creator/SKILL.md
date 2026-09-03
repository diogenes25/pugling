---
name: ki-creator
description: Den KI-Creator-Konsolenagenten (backend/Pugling.Agent.Creator) gegen die laufende Pugling-API fahren – Fachlehrer-Treffer prüfen (profiles), Briefing ohne LLM ansehen, Übungen erzeugen (create, auch --dry-run), Übungsklausuren planen (exam) und ganze Lehrpläne erzeugen (plan). Nutze dies, wenn Übungen, eine Klausur oder ein kompletter Lehrplan per LLM erzeugt werden sollen, wenn der Agent selbst eingerichtet oder debuggt wird (Ollama-Modell, Pugling:Pin), oder bei Fragen zu individuell (--child) vs. allgemein (--profile).
---

# KI-Creator fahren

Braucht die **laufende API** (`dotnet run` aus `backend/Pugling.Api`, http://localhost:5200).

## Einmalige Einrichtung

```bash
ollama pull qwen2.5:14b-instruct                      # Modell mit verlässlichem JSON
cd backend/Pugling.Agent.Creator && dotnet user-secrets set "Pugling:Pin" "0000"
```

## Aufrufe

```bash
dotnet run --project backend/Pugling.Agent.Creator -- profiles --child 1           # welcher Lehrer passt?
dotnet run --project backend/Pugling.Agent.Creator -- briefing --child 1           # ohne LLM
dotnet run --project backend/Pugling.Agent.Creator -- create --child 1 --type Cloze --count 8 --dry-run
dotnet run --project backend/Pugling.Agent.Creator -- create --profile 3 --type Cloze --unit 12   # allgemein
dotnet run --project backend/Pugling.Agent.Creator -- exam --child 1 --types Vocabulary,Cloze --date 2026-09-15
dotnet run --project backend/Pugling.Agent.Creator -- plan --child 1 --types Vocabulary,Cloze --per-type 2 --intensity Normal
```

Bei `plan` zählt `--per-type` **Übungen** je Typ (je eine Position), nicht Aufgaben – die kommen wie bei
`create` aus `--count`. `--child` ist dort Pflicht, und der erzeugte Lehrplan ist **inaktiv**: der Vater
sichtet ihn unter `/vater/plaene` und schaltet ihn selbst scharf, damit ungeprüfter KI-Inhalt nicht über
Nacht zur Pflicht wird. Ziel/Punkte/Malus je Position kommen aus `--intensity`
(`Locker` 70 %/10/0 · `Normal` 80 %/20/−5 · `Intensiv` 90 %/30/−10) – dieselben Zeilen wie im
Lehrplan-Assistenten des Vater-Webs.

## Betriebsarten

**Individuell** (`--child`) verlangt ein Konto mit Creator **und** Supervisor-Rolle, das das Kind
betreut (Seed: Konto 1) – daraus entsteht eine auf das Kind zugeschnittene Übung.
**Allgemein** (`--profile`, ohne Kind) genügt die Creator-Rolle und erzeugt Katalogware; Klassenstufe,
Schulart und Quelle kommen dann aus dem Profil. Ohne `--profile` sucht der Agent das bestpassende
selbst (`profiles/match`).

Fachliche Kernregel: **Interessen kleiden den Stoff ein, sie ersetzen ihn nie.**

Details (Pipeline, Strategien, Tests ohne Ollama):
[backend/Pugling.Agent.Creator/README.md](../../../backend/Pugling.Agent.Creator/README.md).
