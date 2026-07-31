---
tags: [typ/plan, bereich/doku, status/offen]
aliases: [Übersetzung XML-Docs, Doku auf Englisch, Glossar Übersetzung]
---

# XML-Doc-Kommentare im Backend auf Englisch übersetzen

> **Lebendes Nachschlagewerk, kein Wegwerf-Plan.** Diese Seite trägt Glossar und Fortschritt der gesamten
> Übersetzungsarbeit. Jeder Übersetzungs-Agent bekommt den Abschnitt „Glossar" im Prompt mit; gepflegt wird
> er **nur hier** und **nur von der steuernden Sitzung** (siehe Entscheidung 4).

## Warum

Sämtliche Code-Dokumentation im Backend ist auf Deutsch (Konvention in `CLAUDE.md`: „Doku auf Deutsch.").
Stattdessen soll internationales Englisch stehen – u. a. weil einige Fachbegriffe in der Doku
uneinheitlich/seltsam übersetzt wurden (z. B. deutsche Prosa, die einen Typ beschreibt, der im Code längst
einen anderen, englischen Namen trägt – der `Adult`/`Vater`-Fall ist in `CLAUDE.md` selbst dokumentiert).

**Scope dieser Etappe: nur `///`-XML-Doc-Kommentare** (`<summary>`, `<param>`, `<returns>`, `<remarks>`) in
den fünf Backend-Projekten. Inline-`//`-Warum-Kommentare (~1900 Zeilen) und Markdown-Docs (`docs/`, `wiki/`,
restliche `CLAUDE.md`-Prosa, ~16k Zeilen) sind bewusst **nicht** Teil dieser Etappe – das wäre ein eigener
Plan.

## Ausgangsbefund (Momentaufnahme, read-only erhoben)

~2303 `<summary>` + ~441 `<param>` + 5 `<returns>` + 2 `<remarks>`:

| Projekt | `<summary>` |
|---|---|
| `Pugling.Api` | 1260 |
| `Pugling.Contracts` | 492 |
| `Pugling.Api.Tests` | 243 |
| `Pugling.Client` | 181 |
| `Pugling.Agent.Creator` | 127 |

**Keine Tooling-Kopplung an den Wortlaut** – Übersetzen ist build-/testgefahrlos:

- `CS1591` prüft nur *Vorhandensein*, nicht Sprache.
- `DocsCaptureTests` generiert `docs/api-examples/*.md` aus eigenen deutschen Titel-Strings **im Testcode**,
  nicht aus `/// <summary>`-Text.
- Keine `IncludeXmlComments`-Anbindung an Swagger gefunden.

## Glossar

Bedeutung am Code ausrichten, nicht Wort für Wort übersetzen.

| Deutsch (in der Doku) | Englisch | Warum |
|---|---|---|
| „Stick" / Malus | **penalty** | Deckt sich mit `PenaltyCoins`, `GoalPenalty` – die Bild-Metapher nicht wörtlich übersetzen. |
| „Sohn" (Rolle/Person) | **child** | Entität heißt `Child`. |
| „Sohn" (Lern-Ebene gemeint) | **student** | Nur wo die Tier gemeint ist (`api/v1/student/…`), analog zur Adult/Father-Unterscheidung. |
| „Vater" (Nicht-Kind-Zeile) | **adult** | `CLAUDE.md`: im Vertrag heißt es durchgehend `Adult`. |
| „Vater" (wirklich der Vater) | **father** | Bleibt richtig, wo ein Vater gemeint ist (`SupervisorRelation.Father`). |
| „Fahrplan" / „Lehrplan" | **study plan** | Beide Wörter stehen im Fließtext für `StudyPlan` – einheitlich auflösen. |
| „Klassenarbeit" | **class test** | Deckt sich mit `api/v1/supervisor/class-tests`, `ClassTestGrade`. |

Weitere Begriffe entstehen im Verlauf und werden hier nachgetragen (Entscheidung 4).

## Entscheidungen (geklärt)

1. **Mechanischer mehrsprachiger Regex-Vorlauf, zuerst, über alle fünf Projekte in einem Lauf.**
   Ursprünglich verworfen, aber auf expliziten Wunsch nach maximaler Token-Sparsamkeit wieder aufgenommen:
   ein einmaliges Such-/Ersetzen-Skript (PowerShell) ersetzt exakte, wiederkehrende Textbausteine
   („Erstellt eine(n) neue(n) X" → „Creates a new X", „Liefert Y zurück" → „Returns Y",
   „Eindeutige(r) ID/Bezeichner" → „Unique ID/identifier" …) **ohne jeden Agenten-Aufruf** – kostet null
   LLM-Tokens und schrumpft den Rest-Korpus, den die Agenten noch anfassen müssen, spürbar (Nebeneffekt:
   diese Fälle sind danach auch garantiert konsistent, kein Agent muss sie individuell entscheiden).
   Muster vorher an 15–20 Beispielen aus der Recherche verifizieren, dann breit anwenden, danach `grep`
   auf verbliebene deutsche Signalwörter, um zu sehen, was übrig bleibt.
2. **Straffungs-Lizenz eng begrenzt.** Beim Übersetzen dürfen Agenten **ausschließlich** klar erkennbare
   Änderungshistorie/Datums-Prosa entfernen (z. B. „Hieß bis 2026-07-29 `Adult`…", „seit dem Umbau…").
   Keine sonstige eigenständige Kürzung/Umformulierung – sonst driftet der Ton zwischen parallelen
   Agenten auseinander. Ansonsten treue vollständige Übersetzung.
3. **Glossar-Festlegungen** für mehrdeutige Fachbegriffe: siehe Abschnitt „Glossar".
4. **Glossar-Pflege zentral, nicht durch die Übersetzungs-Agenten.** Jeder Modul-Agent übersetzt nur seine
   zugewiesenen `.cs`-Dateien und meldet neue/uneindeutige Begriffe **in seiner Textantwort** – die
   steuernde Sitzung pflegt `docs/translate.md` danach selbst nach (eine Schreibstelle, keine
   Merge-Konflikte bei parallelen Agenten).
5. **Nur vorhandenen Text übersetzen, keine Docs ergänzen.** Fehlt z. B. nach dem jüngsten
   CancellationToken-Rollout ein `<param name="ct">`-Eintrag, wird er **nicht** neu angelegt – das ist eine
   Übersetzungs-, keine Vervollständigungs-Etappe.
6. **`Pugling.Api.Tests` bleibt reguläre fünfte Etappe** (trotz fehlender CS1591-Pflicht dort) – sonst
   bleibt dauerhaft ein deutscher Rest übrig.
7. **Commit pro Etappe, nur nach expliziter Bestätigung.** Nach jeder erfolgreich reviewten Etappe wird
   nachgefragt, ob committet werden soll – kein automatisches Committen (CLAUDE.md-Konvention).

## Strategie: wenig Tokens pro übersetztem Kommentar

1. **Mechanischer Vorlauf zuerst** (Entscheidung 1) – über alle fünf Projekte in einem Skript-Lauf,
   kostet keine Agenten-Tokens, schrumpft den Rest-Korpus vor dem ersten Agenten-Aufruf.
2. **Glossar zweitens, einmalig** (Abschnitt „Glossar") – die Festlegungen aus Entscheidung 3, plus die
   Adult/Father-Regel aus `CLAUDE.md` übernommen. Jeder Übersetzungs-Agent bekommt dieses kompakte Glossar
   im Prompt mitgegeben statt es jedes Mal neu herzuleiten.
3. **Grobe statt feine Agenten-Granularität** – pro Projekt-Etappe möglichst **ein** `Agent`-Aufruf statt
   vieler kleiner pro Unterordner, um den pro Call wiederholten Overhead (System-Prompt, Tool-Schemas,
   Glossar-Präambel) so oft wie möglich auf einmal abzuschreiben. Nur wenn ein Projekt für einen einzelnen
   Agenten zu groß würde (voraussichtlich nur `Pugling.Api`, 1260 summaries), in 2–3 große Blöcke statt
   8 kleine teilen (z. B. Controllers+Auth / Models+Data / Services+Errors+Exercises+OpenApi). Jeder Agent
   liest seine zugewiesenen Dateien **einmal**, übersetzt darin **nur** die `///`-Kommentare (Code/Signaturen
   unverändert) und wendet seine Edits **im selben Turn** an (kein Round-Trip pro Datei), statt bei jedem
   einzelnen Kommentar zurückzufragen. `opts.effort: "low"` für diese Aufrufe – Übersetzen mit festem
   Glossar ist mechanisches Handwerk, kein tiefes Reasoning, das spart Reasoning-Tokens. Rückmeldung knapp:
   Anzahl übersetzter Blöcke + neue Begriffsvorschläge (Entscheidung 4), keine Erzähl-Prosa.
   Unabhängige Blöcke parallel (mehrere `Agent`-Aufrufe in einer Nachricht), abhängige (Api nach Contracts)
   sequenziell. Kein `Workflow`-Einsatz – einzelne `Agent`-Aufrufe reichen, keine Multi-Agent-Orchestrierung
   angefragt.
4. **Nach jeder Etappe**: `dotnet build` fürs jeweils besitzende Projekt (Hook übernimmt das nach Edits
   ohnehin), zusätzlich `dotnet build Pugling.sln` nach der Contracts-Etappe (Api/Client/Agent.Creator
   hängen daran – das deckt der Datei-Hook nicht ab), stichprobenartig `grep` nach verbliebenen deutschen
   Signalwörtern (z. B. `Gibt|Liefert|Erstellt|Eindeutige`) im übersetzten Bereich.
   Danach kurze Rückfrage, ob committet werden soll (Entscheidung 7).

## Fortschritt

Jede Etappe = eigener Commit-Kandidat, nach Review stoppbar. Reihenfolge ist bewusst so gewählt:
`Contracts` prägt das Glossar, `Api` hängt daran.

| # | Etappe | Umfang | Stand | Belege |
|---|---|---|---|---|
| 0 | Mechanischer Regex-Vorlauf (alle 5 Projekte) | – | offen | |
| 1 | `Pugling.Contracts` – prägt das Glossar | 492 summaries + 181 params | offen | |
| 2 | `Pugling.Api` – in 2–3 große Blöcke | 1260 summaries | offen | |
| 3 | `Pugling.Client` | 181 summaries | offen | |
| 4 | `Pugling.Agent.Creator` | 127 summaries | offen | |
| 5 | `Pugling.Api.Tests` | 243 summaries | offen | |
| 6 | Konventionszeile in den `CLAUDE.md` umstellen | 4 Dateien | offen | |

## CLAUDE.md-Konvention umstellen

In `CLAUDE.md` unter „Konventionen" die Zeile

> **Doku auf Deutsch.** Öffentliche Typen/Members tragen `/// <summary>` (fließt in Swagger).

auf Englisch umstellen (nur diese eine Konventions-Zeile – der Rest von `CLAUDE.md` bleibt auf Deutsch,
das ist eine separate spätere Etappe). Gleiche Prüfung für die kurzen Konventionshinweise in
`backend/Pugling.Contracts/CLAUDE.md`, `backend/Pugling.Client/CLAUDE.md`,
`backend/Pugling.Agent.Creator/CLAUDE.md` (je 14–24 Zeilen).

## Kritische Dateien

- `docs/translate.md` – **diese Seite**: Glossar + Fortschrittstabelle je Etappe.
- `CLAUDE.md` (+ die drei Unterprojekt-`CLAUDE.md`) – Konventionszeile.
- `backend/Pugling.Contracts/**/*.cs`, `backend/Pugling.Api/**/*.cs`,
  `backend/Pugling.Client/**/*.cs`, `backend/Pugling.Agent.Creator/**/*.cs`,
  `backend/Pugling.Api.Tests/**/*.cs` – je Etappe die `///`-Blöcke, Musterbeispiele aus der Recherche:
  `Pugling.Contracts/Common/AdminBaseTypes.cs:3-24`, `Pugling.Api/Models/AdminEntities.cs:12-143`,
  `Pugling.Api/Controllers/Supervisor/AdultsController.cs:49`, `Pugling.Client/CreatorApi.cs:13`.

## Verifikation

- Nach jeder Projekt-Etappe: `dotnet build` (bzw. `dotnet build Pugling.sln` nach Contracts) – muss grün
  bleiben (CS1591 prüft nur Präsenz, nicht Sprache).
- `dotnet format` läuft automatisch über den Hook nach `.cs`-Edits.
- Stichproben-Grep auf deutsche Signalwörter im gerade übersetzten Bereich als Restefund.
- Test-Gate (`dotnet test Pugling.sln -c Release`) greift automatisch am Ende der Antwort/vor Push –
  sollte unverändert grün bleiben, da kein Test auf `///`-Wortlaut prüft.
- Swagger einmal am Ende der letzten Etappe stichprobenartig ansehen (`/swagger`), um zu bestätigen,
  dass keine gemischt-sprachige Ausgabe übrig bleibt, falls doch irgendwo XML-Docs einfließen.
