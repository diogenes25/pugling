# Pugling 🐣 — Lern-App mit Punktesystem

> **Vater** steuert und erzwingt Lernerfolg, **Sohn** lernt mit Spaß.
> Drei Ebenen, Punkte, Zeitfenster, Leitner-Karteikasten, Klassenarbeiten — alles über eine REST-API.

Pugling ist **API-First**: Die REST-API (OpenAPI/Swagger) ist das Produkt und die einzige Quelle der
Wahrheit. Jedes Feature lebt im Backend; das Frontend hängt daran. Diese Doku ist ein **Wiki** — sie
erklärt jede Funktion so vollständig, dass ein Mensch **oder ein LLM** die App bedienen, einen
kompletten Lernplan bauen oder die App um neue Übungstypen erweitern kann.

---

## 🚀 Schnellstart

```bash
cd backend/Pugling.Api && dotnet run     # → http://localhost:5200 , Swagger unter /swagger
cd frontend && npm install && npm run dev # → http://localhost:5173 (Web-UI, /api-Proxy → :5200)
```

- **Base-URL:** `http://localhost:5200`
- **OpenAPI-JSON:** `http://localhost:5200/openapi/v1.json` · **Swagger-UI:** `/swagger`
- **Web-UI:** `http://localhost:5173` — Produktseite `/`, Vater-Web `/vater` (mit Lehrplan-Assistent
  `/vater/wizard`), Sohn-App `/sohn`. Setzt die laufende API voraus.
- **Alle Routen:** `api/v1/…` (Versionssegment zentral in `ApiRoutes.V1`)
- **Auth:** PIN-Login → JWT als `Authorization: Bearer <token>`
- **Seed-Konten:** Vater `id=1` PIN `0000` · Sohn (Kind) `id=1` PIN `1111`
- **DB:** SQLite (`pugling.db`), Migrationen laufen beim Start automatisch

Der schnellste Weg, die ganze API in Aktion zu sehen: Skill `/smoke-test` oder Swagger-UI
(„Authorize"-Button mit dem Login-Token). Die komplette Rollen-Schleife im echten UI zeigt der
Playwright-E2E: `cd frontend && npm run test:e2e`.

---

## 🗺️ Die drei „Pläne" — bitte nicht verwechseln

Im Repo heißen **drei verschiedene Dinge** „Lehrplan/Plan". Das ist die häufigste Verwechslung:

| # | Name | Was es ist | Doku |
| --- | --- | --- | --- |
| 1 | **Study-Plan** (API) | Das **produktive** Trainingsobjekt: Container für ein Kind mit `PlanPosition`s auf Katalog-Übungen, Leitner, Stufen, Punkten, Combo und Missionen. | **Dieses Wiki** → [Lernplan bauen](wiki/04-lernplan-bauen.md) |
| 2 | **Katalog-Übung** | Globale **Übungs-Bibliothek** `Subject → Chapter → Exercise` (12 typisierte Übungsarten mit Metadaten). Fundament für den künftigen Auto-Generator. | [Übungstypen](wiki/03-uebungstypen.md) |
| 3 | **Markdown-Lehrplan** | Von den Skills `lehrplan-autor`/`lehrplan-lerner` erzeugter/abgearbeiteter Kurs **ohne** laufende App (reine Dateien). | [docs/lehrplan-erstellen.md](docs/lehrplan-erstellen.md) |

Dieses Wiki behandelt vor allem **#1 (Study-Plan)** und **#2 (Katalog)** — die API. #3 ist eine
eigenständige, dateibasierte Welt.

---

## 📚 Wiki-Inhalt

### Rollen-Einstiege

| Rolle | Einstieg | Fokus |
| --- | --- | --- |
| **Creator** | [Creator-Tutorial](docs/tutorial-creator.md) · [Übungstypen](wiki/03-uebungstypen.md) · [Doku nach Rollen](docs/rollen-doku.md#creator--inhalte-und-katalog) | Inhalte, Katalog, Übungstypen, Stores, Metadaten. |
| **Supervisor** | [Supervisor-Tutorial](docs/tutorial-supervisor.md) · [Doku nach Rollen](docs/rollen-doku.md#supervisor--steuerung-kontrolle-belohnung) | Study-Pläne, Ziele, Kontrolle, Punkte, Shop, Klassenarbeiten. |
| **Student** | [Student-Tutorial](docs/tutorial-student.md) · [Doku nach Rollen](docs/rollen-doku.md#student--lernen-fortschritt-einlösen) | Lernen, Tests, Fortschritt, Punkte-Sicht, Einlösen. |

### Thematische Seiten

| Seite | Inhalt |
| --- | --- |
| **[01 · Überblick & Architektur](wiki/01-ueberblick-architektur.md)** | Konzepte, Rollen, Datenmodell, wie alles zusammenhängt. **Hier anfangen.** |
| **[02 · Authentifizierung & Rollen](wiki/02-authentifizierung.md)** | PIN-Login, JWT, Rollen, Eigentum (Ownership), Anti-Schummel. |
| **[03 · Übungstypen (Katalog)](wiki/03-uebungstypen.md)** | Alle 12 Übungsarten mit Config-Schema, Beispiel-Requests, Auswertung. |
| **[04 · Einen Lernplan bauen (Supervisor)](wiki/04-lernplan-bauen.md)** | Wegweiser → die vollständige Fassung ist jetzt das [Supervisor-Tutorial](docs/tutorial-supervisor.md). |
| **[05 · Punkte & Bonus-System](wiki/05-punkte-und-bonus.md)** | Wie Punkte entstehen, alle Bonus-Quellen (Combo, Speed, Zeitfenster), Missionen & Auszeichnungen — mit Formeln. |
| **[06 · Anleitung für die Sohn-App](wiki/06-sohn-app.md)** | Wegweiser → die vollständige Fassung ist jetzt das [Student-Tutorial](docs/tutorial-student.md). |
| **[07 · API-Referenz](wiki/07-api-referenz.md)** | Kompakter Endpunkt-Index über die ganze API. |
| **[08 · Erweitern: neue Übung / neues Verfahren](wiki/08-erweitern.md)** | Für Entwickler & LLMs: neuen Übungstyp anlegen, neues Lernverfahren, Add-Ons. |
| **[09 · LLM-Kochbuch: Lernplan aus einem Prompt](wiki/09-llm-kochbuch.md)** | Rezept, damit eine AI wie ein Vater aus „Erstelle einen Lernplan für die 9. Klasse …" einen fertigen Plan über die API baut. |

### Weiterführende Bestandsdoku (`docs/`)

- [Doku nach Rollen: Creator, Supervisor, Student](docs/rollen-doku.md) — rollenbasierter Einstieg in Doku und Wiki
- [Architektur-Entscheidung (API-First)](docs/architektur-entscheidung.md) · [Architektur-Resümee](docs/architektur-resumee.md)
- [Endpunkt-Beziehungen](docs/endpunkt-beziehungen.md) — wie die Endpunkte inhaltlich zusammenhängen (Übung → Lehrplan → Kind → Auswertung), mit Datenfluss-Diagramm
- [Obsidian: Wissensvernetzung](docs/obsidian.md) — Doku/Planung als navigierbarer Graph (Tags/Backlinks), Konsistenz-Konventionen gegen Insellösungen
- [Tutorials nach Rolle](docs/tutorial.md) — Index → [Creator](docs/tutorial-creator.md) · [Supervisor](docs/tutorial-supervisor.md) · [Student](docs/tutorial-student.md) (verifiziert)
- [Vokabel-Funktionalitäten für Entwickler](docs/vokabel-funktionalitaeten-entwickler-tutorial.md) — Store, `ExerciseItem`s, Progress und API-Flows
- [Vokabeltraining-Prozess-Log](docs/vokabeltraining-prozess.md) — wie das Study-Plan-System in 8 Iterationen entstand
- [Klassenarbeiten & Tagging](docs/klassenarbeiten-tagging.md) · [Code-Review](docs/code-review.md)
- [Markdown-Lehrplan erstellen](docs/lehrplan-erstellen.md) — die dateibasierte `lehrplan-autor`/`lehrplan-lerner`-Welt

---

## 🧩 Auf einen Blick: Was kann die App?

- **Lern-Katalog** pflegen: Fächer, Kapitel und **12 Übungstypen** (Vokabeln, Grammatik, Lückentext,
  Leseverstehen, Hörverstehen, Aufsatz, Zuordnung, Übersetzung, Rechnen fest/zufällig, Listen,
  Birkenbihl) — mit Metadaten (Klassenstufe, Schulart, Quelle, Art) für die Vorfilterung.
- **Study-Pläne** je Kind: gemischte Positionen auf Katalog-Übungen mit
  - Leitner-Karteikasten (Boxen 1–5, Fälligkeit),
  - Stufen-Fahrplan (Schwierigkeit steigt über die Tage),
  - Positionszielen (frei, täglich oder wöchentlich),
  - Bestehensschwellen, Combo-/Speed-Boni und Zielpunkten je Position.
- **Server-autoritative Bewertung**: Der Server prüft jede Antwort und vergibt Punkte — nie das Frontend.
- **Motivation**: konfigurierbare Punkte, Combo-Bonus, Schnelle-Antwort-Bonus, Zeitfenster-Multiplikator,
  **Missionen** (Tages-/Wochenziele) und **Auszeichnungen** (Badges).
- **Kontrolle für den Vater**: Tag-für-Tag-Fortschritt, Mastery pro Inhalt, Testhistorie, Punkte-Ledger.
- **Klassenarbeiten & Tags**: Übungen taggen, Arbeiten planen/benoten, gezielt üben.

---

## 🛠️ Für Entwickler

- **Stack:** C# 14 / .NET 10, ASP.NET Core, EF Core 10 + SQLite, JWT-Auth, Asp.Versioning, OpenAPI.
- **Konventionen:** siehe [CLAUDE.md](CLAUDE.md) — dünne Controller, Logik in Services, `record`-DTOs,
  deutsche `/// <summary>`-Docs, Guard Clauses zuerst, `ProblemDetails` für Fehler, EF-Migrationen.
- **Neuen Übungstyp anlegen:** [wiki/08-erweitern.md](wiki/08-erweitern.md) oder Skill `/neuer-uebungstyp`.
- **Tests:** `dotnet test` (`backend/Pugling.Api.Tests`).
