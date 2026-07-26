# Pugling.Agent.Creator – der KI-Creator

Eine lokale Konsolen-App, die die Rolle des **Creators** übernimmt: sie erzeugt Übungen, die auf ein
bestimmtes Kind zugeschnitten sind, und legt sie über die Pugling-REST-API im Katalog an.
Alles läuft auf dem eigenen Rechner – die API auf `localhost:5200`, das Sprachmodell in **Ollama**.

## Die Kernidee

Der feste Lernstoff (Lehrbuch, Kapitel, Wortschatz) und die **Interessen des Kindes** haben getrennte
Aufgaben:

> Interessen ändern **nie**, *welche* Wörter oder Inhalte geübt werden – sie bestimmen nur die
> **Einkleidung**: Sätze, Situationen, Beispiele, Merkhinweise.

Diese Regel steht nicht nur im Prompt, sondern wird deterministisch geprüft: Ist ein Pflicht-Wortschatz
vorgegeben und taucht er im Entwurf nicht vollständig auf, wird der Entwurf abgelehnt.

## Wer steuert – und warum so

Nicht das Sprachmodell, sondern C#. Das Modell liefert ausschließlich **strukturierten Inhalt**
(JSON nach Schema), der Ablauf ist fest verdrahtet:

```
1  Briefing   Kind-Profil + Interessen + Lehrbuch (Supervisor-API), Fach/Kapitel + vorhandene
              Übungen (Creator-API), optional der Lernstand (Student-API)
2  Entwurf    IChatClient.GetResponseAsync<TDraft>()  →  typisierter Entwurf
3  Prüfung    deterministische Regeln; bei Verstößen eine Reparatur-Runde mit den konkreten Mängeln
4  Anlegen    Entwurf → ExercisePayload<TConfig> → POST über Pugling.Client
5  Selbsttest Vorschau + Check mit den eigenen Soll-Antworten – erwartet werden 100 %
```

Das ist auf kleinen lokalen Modellen deutlich verlässlicher als Tool-Calling und – wichtiger – jeder
Schritt ist einzeln prüfbar. Die Tests (`CreatorAgentTests`) fahren die komplette Pipeline gegen den
echten In-Process-Server mit einem `FakeChatClient` statt Ollama.

## Voraussetzungen

1. **Die API läuft**: `cd backend/Pugling.Api && dotnet run` (http://localhost:5200).
2. **Ollama läuft** mit einem Instruct-Modell, das verlässlich JSON nach Schema liefert:
   ```bash
   ollama pull qwen2.5:14b-instruct    # gutes Deutsch, stabiles JSON
   ollama pull llama3.1:8b             # Alternative bei knapper Hardware
   ```
   Roleplay-Finetunes taugen nicht. Kleine Modelle (2–4 B) liefern zwar meist gültiges JSON, aber
   inhaltlich schwache Aufgaben – der Validator fängt einiges davon ab.
3. **Ein Konto mit Creator *und* Supervisor-Rolle**, das das Kind betreut. Jeder Vater-Account hat
   beide Rollen; im Seed ist das Konto 1 („Papa", PIN 0000) für Kind 1. Ein reines Lehrer-Konto sieht
   keine Kinder und kann daher nicht zuschneiden.

## Konfiguration

`appsettings.json` trägt alles außer dem Geheimnis:

```jsonc
{
  "Pugling": { "BaseUrl": "http://localhost:5200", "AccountId": 1 },
  "Agent":   { "Endpoint": "http://localhost:11434", "Model": "qwen2.5:14b-instruct",
               "Temperature": 0.4, "TimeoutSeconds": 180, "RepairAttempts": 1 }
}
```

Die PIN gehört **nicht** dorthin:

```bash
cd backend/Pugling.Agent.Creator
dotnet user-secrets set "Pugling:Pin" "0000"
# oder als Umgebungsvariable: Pugling__Pin=0000
```

Jede Einstellung lässt sich per Umgebungsvariable überschreiben (`Agent__Model=llama3.1:8b`).

## Verwendung

```bash
# Welche Übungstypen kennt der Server – und welche kann der Agent erzeugen?
dotnet run --project backend/Pugling.Agent.Creator -- types

# Worauf würde der Agent zuschneiden? (ohne Sprachmodell, ohne Schreibzugriff)
dotnet run --project backend/Pugling.Agent.Creator -- briefing --child 1 --topic "Unit 3: Animals"

# Planen, aber nichts speichern
dotnet run --project backend/Pugling.Agent.Creator -- create --child 1 --type Vocabulary \
    --subject 1 --chapter 1 --topic "Tiere im Zoo" --count 10 --dry-run

# Wirklich anlegen – und bei misslungenem Selbsttest wieder zurücknehmen
dotnet run --project backend/Pugling.Agent.Creator -- create --child 1 --type Cloze \
    --subject 1 --chapter 1 --words "horse,sheep,stable" --count 8 --strict
```

### Optionen von `create`

| Option | Bedeutung | Standard |
|---|---|---|
| `--child <id>` | Kind, auf das zugeschnitten wird | Pflicht |
| `--type <Typ>` | `Vocabulary`, `Cloze`, `Translation`, `Grammar` | Pflicht |
| `--subject`/`--chapter` | Ort im Katalog | erstes Fach mit Kapitel |
| `--topic "…"` | Thema bzw. Lehrbuch-Unit | – |
| `--count <n>` | Anzahl Aufgaben (3–30) | 10 |
| `--words a,b,c` | Pflicht-Wortschatz (unveränderlich) | – |
| `--use-weak` | schwach beherrschte Wörter des Kindes als Wortschatz | aus |
| `--source-lang`/`--target-lang` | Lern-/Muttersprache | `en`/`de` |
| `--points <n>` | Punkte der Übung | 10 |
| `--dry-run` | nur planen und drucken | aus |
| `--strict` | Übung löschen, wenn der Selbsttest < 100 % | aus |

Exit-Codes: `0` fertig · `1` fachlich gescheitert · `2` falsch aufgerufen · `130` abgebrochen.

## Was der Agent bewusst *nicht* tut

- **Zuweisen.** Lehrplan, Ziele, Punkte und Malus sind Sache des Supervisors – der Agent füllt nur
  den Katalog. Die erzeugte Übungs-Id wandert von Hand (oder später vom Supervisor-Agenten) in eine
  Lehrplan-Position.
- **Schreiben, ohne zu prüfen.** Jede Übung wird nach dem Anlegen im nebenwirkungsfreien Testmodus
  gegen ihre eigenen Lösungen gespielt.
- **Den Lernstoff wählen, wenn er vorgegeben ist.** Siehe Kernidee.

## Erweitern

Ein neuer Übungstyp = eine Klasse, die von `ExerciseStrategy<TDraft, TConfig>` erbt und fünf Dinge
beisteuert: Typ-Schlüssel, Auftragsbeschreibung im Prompt, Regeln, Abbildung auf die Vertrags-Config
und die Soll-Antworten. Registrieren in `Program.BuildHost` (`AddSingleton<IExerciseStrategy, …>`).
Der Ablauf (Reparatur-Runde, Trockenlauf, Selbsttest, Rücknahme) kommt aus der Basisklasse.

Ein neuer API-Endpunkt gehört **zuerst** ins Backend, dann als einzeiliger Wrapper in
`Pugling.Client` – der Agent baut ausschließlich auf dieser Schicht auf, nie auf eigenem HTTP-Code.
