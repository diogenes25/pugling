# Tools

Hilfsprogramme rund um die Pugling-API. Alle werden **aus dem Repo-Root** aufgerufen und
sprechen die laufende API an (`cd backend/Pugling.Api && dotnet run`, Standard `:5200`).

| Tool | Zweck | Einstieg |
|------|-------|----------|
| [bruno/](bruno/README.md) | Generiert aus der OpenAPI eine vollständige [Bruno](https://www.usebruno.com/)-Collection (`.yml`) mit Auth, Variablen, Parameter-Tabellen und verifizierten Beispielen. | `npm run bruno:generate` |
| [vokabel-import/](vokabel-import/README.md) | Füllt den Vokabel-Store per Batch-Endpunkt mit fertigen Wortlisten (Top-100 & 101–1000, EN/FR → DE). | `./tools/vokabel-import/import-vokabeln.ps1` |

Details, Flags und Voraussetzungen stehen jeweils im README des Tools.

> **API-First:** Die REST-API ist die Quelle der Wahrheit; diese Tools sind Zulieferer/Konsumenten
> davon, kein Ersatz. Beide brauchen ein laufendes Backend (Vokabel-Import zusätzlich einen
> **Vater-Login**, PIN `0000`). Siehe auch [CLAUDE.md](../CLAUDE.md).
