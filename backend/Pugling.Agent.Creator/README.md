# Pugling.Agent.Creator – der KI-Creator

Eine lokale Konsolen-App, die die Rolle des **Creators** übernimmt: sie erzeugt Übungen – **allgemein**
für den geteilten Katalog oder **individuell** auf ein Kind zugeschnitten – und legt sie über die
Pugling-REST-API an. Alles läuft auf dem eigenen Rechner: die API auf `localhost:5200`, das Sprachmodell
in **Ollama**.

## Die Kernidee

Der feste Lernstoff (Lehrwerk, Unit, Wortschatz) und die **Interessen des Kindes** haben getrennte
Aufgaben:

> Interessen ändern **nie**, *welche* Wörter oder Inhalte geübt werden – sie bestimmen nur die
> **Einkleidung**: Sätze, Situationen, Beispiele, Merkhinweise.

Diese Regel steht nicht nur im Prompt, sondern wird deterministisch geprüft: Ist ein Pflicht-Wortschatz
vorgegeben und taucht er im Entwurf nicht vollständig auf, wird der Entwurf abgelehnt.

## Der Fachlehrer: das Creator-Profil

Ein **Creator-Profil** (`api/v1/creator/profiles`) ist ein Lehrer: ein Fach, ein Schulzweig, ein
Klassenstufen-Bereich, optional eine **Buchreihe** – dazu `persona` und `didactics`, die dem festen
Regelblock des System-Prompts *vorangestellt* werden (prägen die Rolle, weichen keine Regel auf).

Die Buchreihe (`api/v1/creator/textbook-series` + `…/units`) ist der Angelpunkt. Eine `SeriesUnit`
trägt Band, Bezeichnung und – entscheidend – **Themen, Grammatik und Wortschatz der Unit**. Nur weil das
Lehrbuch des Kindes (`supervisor/children/{id}/textbooks` mit `seriesId`/`currentUnitId`) und das Profil
denselben Datensatz nennen, kann der Agent den passenden Lehrer *finden* statt zu raten:

```text
Kind (Klasse 8, Gymnasium, Lehrbuch → Reihe „Access", Unit 3)
        │  GET creator/profiles/match?childId=…
        ▼
Profile nach Passung: Reihe (8) > Fach (4) > Klassenstufe (2) > Schulart (1)
```

Das Matching ist deterministisch (harte Ausschlüsse, dann Punkte, Gleichstand über die Id) – derselbe
Datenstand liefert denselben Lehrer, damit die Herkunft einer Übung nachvollziehbar bleibt.

Gepflegt wird beides im **Vater-Web**: Reihen und Units unter `/vater/lehrwerke`, Profile unter
`/vater/fachlehrer`, und die Reihe am Kind auf `/vater/kind/:id` („Unterrichtsmaterial", dort steht auch
die begründete Trefferliste). Wer lieber die API bedient: [docs/REST/Creator.http](../../docs/REST/Creator.http).

## Wer steuert – und warum so

Nicht das Sprachmodell, sondern C#. Das Modell liefert ausschließlich **strukturierten Inhalt**
(JSON nach Schema), der Ablauf ist fest verdrahtet:

```text
1  Briefing   Profil + Reihe/Unit (Creator-API), Fach + vorhandene Übungen der Unit (Creator-API),
              optional Kind-Profil + Interessen + Lehrbuch (Supervisor-API) und Lernstand (Student-API)
2  Entwurf    IChatClient.GetResponseAsync<TDraft>()  →  typisierter Entwurf
3  Prüfung    deterministische Regeln; bei Verstößen eine Reparatur-Runde mit den konkreten Mängeln
4  Anlegen    Entwurf → ExercisePayload<TConfig> → POST über Pugling.Client
5  Selbsttest Vorschau + Check mit den eigenen Soll-Antworten – erwartet werden 100 %
```

Das ist auf kleinen lokalen Modellen deutlich verlässlicher als Tool-Calling und – wichtiger – jeder
Schritt ist einzeln prüfbar. Die Tests (`CreatorAgentTests`) fahren die komplette Pipeline gegen den
echten In-Process-Server mit einem `FakeChatClient` statt Ollama.

## Voraussetzungen

1. **Die API läuft**: `cd backend/Pugling.Api && dotnet run` (<http://localhost:5200>).
2. **Ollama läuft** mit einem Instruct-Modell, das verlässlich JSON nach Schema liefert:

   ```bash
   ollama pull qwen2.5:14b-instruct    # gutes Deutsch, stabiles JSON
   ollama pull llama3.1:8b             # Alternative bei knapper Hardware
   ```

   Roleplay-Finetunes taugen nicht. Kleine Modelle (2–4 B) liefern zwar meist gültiges JSON, aber
   inhaltlich schwache Aufgaben – der Validator fängt einiges davon ab.
3. **Ein Konto mit Creator-Rolle.** Für **allgemeine** Übungen (`--profile`, ohne `--child`) genügt das.
   Wer **individuell** zuschneiden will (`--child`), braucht ein Konto, das zusätzlich die
   Supervisor-Rolle hat und dieses Kind betreut – im Seed das Konto 1 („Papa", PIN 0000) für Kind 1.
   Ein reines Lehrer-Konto sieht keine Kinder und kann daher nur allgemein arbeiten.

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

# Welches Profil passt zu diesem Kind? (mit Begründung, ohne Sprachmodell)
dotnet run --project backend/Pugling.Agent.Creator -- profiles --child 1

# Worauf würde der Agent zuschneiden? (ohne Sprachmodell, ohne Schreibzugriff)
dotnet run --project backend/Pugling.Agent.Creator -- briefing --child 1

# Individuell: Profil automatisch gewählt, Unit aus dem Lehrbuch des Kindes
dotnet run --project backend/Pugling.Agent.Creator -- create --child 1 --type Vocabulary \
    --count 10 --dry-run

# Allgemein: nur das Profil, kein Kind – die Übung geht in den geteilten Katalog
dotnet run --project backend/Pugling.Agent.Creator -- create --profile 3 --type Cloze \
    --unit 12 --count 8 --strict

# Übungsklausur: mehrere Typen zum selben Stoff + geplante Klassenarbeit
dotnet run --project backend/Pugling.Agent.Creator -- exam --child 1 \
    --types Vocabulary,Cloze,Grammar --per-type 6 --date 2026-09-15

# Lehrplan: je Typ mehrere Übungen, jede als eigene Position – der Plan entsteht INAKTIV
dotnet run --project backend/Pugling.Agent.Creator -- plan --child 1 \
    --types Vocabulary,Cloze --per-type 2 --count 10 --intensity Normal --days 21
```

### Wer und in wessen Namen

| Option | Bedeutung | Standard |
|---|---|---|
| `--child <id>` | Kind, auf das zugeschnitten wird (individuelle Übung) | – |
| `--profile <id>` | Creator-Profil („Fachlehrer") | bestpassendes zum Kind |
| `--general` | mit `--child`: Stoff des Kindes nutzen, aber **nicht** individualisieren | aus |
| `--unit <id>` | Unit der Buchreihe | aktuelle Unit des Kindes |

Eines von `--child`/`--profile` ist Pflicht – sonst fehlt sowohl die Zielgruppe als auch das Fachwissen.

### Weitere Optionen

| Option | Bedeutung | Standard |
|---|---|---|
| `--type <Typ>` | `Vocabulary`, `Cloze`, `Translation`, `Grammar` | Pflicht bei `create` |
| `--series`/`--series-unit` | Reihe und Unit, in der die Übung entsteht (`--series-unit` allein bestimmt die Reihe; mit `--profile` muss sie in dessen Reihe liegen; das Fach kommt aus dem mit `--profile` genannten Profil, sonst aus der Reihe) | Reihe des mit `--profile` genannten Profils (das zum Kind gewählte verschiebt sie nicht), sonst erste Reihe mit Unit; erste Unit der Reihe |
| `--subject` | nur `profiles`: Fach, auf das die Treffer gefiltert werden | – |
| `--topic "…"` | Thema bzw. Lehrbuch-Unit | – |
| `--count <n>` | Anzahl Aufgaben (3–30) | 10 |
| `--types a,b,c` | Übungstypen des Bündels | `exam`: `Vocabulary,Cloze,Grammar` · `plan`: `Vocabulary,Cloze` |
| `--per-type <n>` | `exam`: **Aufgaben** je Typ · `plan`: **Übungen** je Typ (je eine Position) | 6 bzw. 2 |
| `--date JJJJ-MM-TT` | Termin der Klassenarbeit | in 7 Tagen |
| `--title "…"` | Titel der Klausur bzw. des Lehrplans | aus Unit bzw. Thema |
| `--intensity <Stufe>` | `Locker`/`Normal`/`Intensiv` – Bestehensgrenze, Punkte und Malus je Position | `Normal` |
| `--start JJJJ-MM-TT` | erster Tag des Lehrplan-Zeitraums | heute |
| `--days <n>` | Dauer des Lehrplan-Zeitraums in Tagen | 14 |
| `--words a,b,c` | Pflicht-Wortschatz (unveränderlich) | – |
| `--interests a,b` | Interessen-Slugs, in die die Übung eingekleidet wird — **nur ohne Kind** | – |
| `--use-weak` | schwach beherrschte Wörter des Kindes als Wortschatz, nur des Sprachpaars der Übung; braucht `--child` | aus |
| `--source-lang`/`--target-lang` | Lern-/Muttersprache | Profil, sonst `en`/`de` |
| `--points <n>` | empfohlene Zielpunkte, die eine neue Position erbt (max. 200) | 20 |
| `--dry-run` | nur planen und drucken | aus |
| `--strict` | Übung löschen, wenn der Selbsttest < 100 % | aus |

Exit-Codes: `0` fertig · `1` fachlich gescheitert · `2` falsch aufgerufen · `130` abgebrochen.

### Die Übungsklausur (`exam`)

Der `ExamPlanner` schickt jeden gewünschten Typ durch dieselbe Pipeline – jeder Teil mit **eigenem
Selbsttest**. Erst danach entstehen (nur mit `--child`) ein kind-skopierter Tag und eine
**Klassenarbeit** im Status *geplant*, der genau diese Übungen zugewiesen sind. Ein gescheiterter Teil
bricht den Lauf nicht ab, wird aber gemeldet und setzt Exit-Code 1: eine halb gelungene Klausur soll
sichtbar halb gelungen sein, statt als vollständige Arbeit im Kalender des Kindes zu landen.
Ohne `--child` gibt es keinen Tag (Tags sind kind-skopiert) – dann hält allein die Quellenangabe das
Bündel zusammen.

### Der Lehrplan (`plan`)

Das Gegenstück zur Klausur mit demselben Ablauf, aber einem anderen Behälter: statt einer
Klassenarbeit entsteht ein **Lehrplan mit je einer Position** pro erzeugter Übung. `--per-type` zählt
hier **Übungen** (nicht Aufgaben – die kommen aus `--count`), und `--child` ist **Pflicht**: ein
Lehrplan gehört immer einem Kind. Die Ablehnung kommt, bevor das Modell läuft, damit ein Vertipper
nicht erst Minuten Erzeugung kostet.

Ziel, Punkte und Münz-Malus je Position kommen aus `--intensity` – **denselben drei Zeilen, die auch
der Lehrplan-Assistent im Vater-Web anbietet** (`frontend/src/vater/VaterWizard.tsx`, `INTENSITY`):

| Stufe | Bestehen ab | Punkte je Ziel | Malus bei Versäumnis |
|---|---|---|---|
| `Locker` | 70 % | 10 | – (reine Belohnung) |
| `Normal` | 80 % | 20 | −5 🪙 |
| `Intensiv` | 90 % | 30 | −10 🪙 |

Die Test-Stufe schickt der Agent bewusst **nicht** mit: die Zahl ist die Vokabel-Skala, und jeder andere
Typ liest sie anders – weggelassen entscheidet der Server anhand der Übung selbst.

**Der Plan entsteht `active: false`.** Das ist die eine Sicherung, die diese Betriebsart trägt: ein
ungeprüfter KI-Plan darf nicht über Nacht zur Pflicht werden und Malus-Münzen kosten – und ein aktiv
angelegter Plan würde zusätzlich den Plan abschalten, den das Kind heute spielt (die Regel „genau ein
aktiver Plan je Kind"). Der Vater sichtet ihn unter `/vater/plaene` und schaltet ihn dort scharf.

### Die Stoffnotizen einer Unit (`fill`)

```bash
pugling-creator fill --series 3 --unit 12 --source-file inhaltsverzeichnis.txt --dry-run
pugling-creator fill --series 3 --unit 12 --source-file - < kapitel.txt      # Quelle von Stdin (Umleitung/Pipe)
```

Entwirft `Topics`/`Grammar`/`VocabularyNotes` einer Unit aus einer Quelle, die **du** lieferst – ein abgetipptes
Inhaltsverzeichnis, eine Verlags-Kapitelübersicht (B-22). Die Quelle ist Pflicht und die einzige Grundlage: das
Modell strukturiert sie, es erfindet nichts dazu (Systemprompt: „nutze ausschließlich die Quelle, füge nichts
hinzu“). Ohne Quelle bricht der Befehl als Fehlbedienung ab (Exit 2). Befüllt werden **nur leere Felder**; was schon
von Hand dort steht, bleibt – `--force` überschreibt. `--dry-run` zeigt den Entwurf, ohne zu schreiben. Einen
Selbsttest gibt es hier nicht: drei Textfelder sind nicht spielbar, die Prüfung ist das Lesen. Die Grenzen der API
gelten unverändert: ein Thema über 200 Zeichen wird **weggelassen und genannt**, nie gekürzt (die API lehnt gekürzte
Themen bewusst ab); Freitext über 2000 Zeichen wird am Wortende gekürzt und das Feld genannt. `-` für Stdin ist nur
mit Umleitung oder Pipe sinnvoll – am nackten Terminal wartet der Befehl auf das Eingabeende (Ctrl+Z+Enter bzw.
Ctrl+D) und sagt das.

## Was der Agent bewusst *nicht* tut

- **Einen Lehrplan aktiv schalten.** Er legt ihn an (`plan`) – scharf stellt ihn ein Mensch. Ziele,
  Punkte und Malus bleiben Sache des Supervisors; der Agent setzt sie nur als Vorbelegung aus
  `--intensity`, die im Vater-Web an jeder Position überschreibbar bleibt.
- **Die Unit-Inhalte erfinden.** `topics`/`grammar`/`vocabularyNotes` einer `SeriesUnit` pflegt ein
  Mensch (oder ein Import) – der Agent liest sie und richtet den Stoff daran aus.
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
