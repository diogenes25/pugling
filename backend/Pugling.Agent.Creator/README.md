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

### Was das Programm entscheidet und was das Modell

Faustregel: **Was nur eine richtige Antwort hat, entscheidet C#.** Das Modell schreibt nur, was Sprache
und Einfall braucht. Gemessen am ersten Lehrplan-Lauf gegen das Access-Lehrwerk (2026-11-10, `qwen2.5:14b`):
4 Teile in 14,5 Minuten. Zwei davon scheiterten an Regeln, zwei kamen durch und hatten trotzdem Mängel.
Nach dem Umbau lief derselbe Befehl in 12,1 Minuten mit 4 von 4 Teilen, beide Lückentexte beim ersten Versuch.
Beide verbliebenen Reparatur-Runden gingen auf einen vergebenen Titel zurück, den C# seitdem selbst nummeriert.

| Lauf | Teile ok | Dauer | Reparatur-Runden | Mängel trotz „ok“ |
|---|---|---|---|---|
| vorher (Lehrplan 2) | 2 von 4 | 14,5 min | 3 | Ablenker als Lösungen; Titel nach Interessen |
| Zwischenstand (Lehrplan 3) | 3 von 4 | 10,2 min | 2 | Auswahllisten hinter Lücken |
| nachher (Lehrplan 4) | 4 von 4 | 12,1 min | 2 (beide: Titel vergeben) | Zeitform-Feinheiten, die nur der Testmodus des Vaters findet |

| Fund im Lauf | Jetzt |
|---|---|
| Das Modell erfand die Übersetzungen selbst. „test = Test“ und „pub = Pub“ scheiterten an der Regel „Aufgabe gleich Lösung“, obwohl die Unit 124 Wortpaare aus dem Buch hatte | Hat die Unit Vokabelübungen, die nicht der Agent erzeugt hat (also aus der Buchliste), ist ihre **Wortliste** der Stoff. Ohne `--words` und ohne `--use-weak` legt C# Wörter und Übersetzungen fest (schwache Wörter zuerst, dann je Teil die nächste Scheibe, ab drei Wörtern). Das Modell schreibt nur Titel und Merkhinweise. Ein Hinweis, der die Übersetzung nennt (auch gebeugt: „Kathedralen“), fliegt raus. Dann gilt der Hinweis aus dem Buch |
| Ein Lückentext scheiterte an der Kopplung von Platzhaltern und Lücken. Ein anderer kam durch, aber mit den **Ablenkern als akzeptierten Lösungen**: „came“ zählte für „went“ als richtig | Das Modell schreibt die Lösung in eckige Klammern: `We [went] (go) to the Tower.` Nummern, Lücken und Wortbank (sortiert) baut C#. Andere richtige Schreibweisen gibt es nur in der Klammer (`[didn't\|did not]`), Ablenker nur in `distractors` |
| „verraten“ wurde als Teilwort geprüft („is“ in „This“) | Geprüft wird das ganze Wort außerhalb der Klammern. Die Grundform-Hilfe direkt hinter der Lücke zählt nicht |
| Grammatik-Aufgaben durften die Lösung im Satz nennen (B-458) | Neue Regel: Steht die Lösung schon in der Aufgabe (ohne runde Klammern), wird der Entwurf abgelehnt |
| Das Modell kopierte die Lösungen in `distractors` (8 von 10 im zweiten Lauf) | Solche Doppelungen streicht C# ohne Rückfrage, denn die Wortbank enthält die Lösungen ohnehin. Gezählt werden nur echte Ablenker, mindestens zwei |
| Hinter Lücken standen Auswahllisten: „(Pokémon, friend, match)“ | In der runden Klammer hinter einer Lücke steht genau eine Grundform. Eine Liste ist eine Regelverletzung |
| Zu viele Aufgaben kosteten eine Reparatur-Runde | Überzählige Einträge streicht C#, überzählige Klammern werden samt Grundform-Hilfe zu normalem Text |
| Im dritten Lauf gingen beide Reparatur-Runden auf einen schon vergebenen Titel zurück | Ein vergebener Titel bekommt eine Nummer: „Unit 1 · Wörter (2)“ |
| Der Lehrplan unterschied seine Teile nur durch „(Lehrplan, Cloze 1/2)“. Jeder Lückentext mischte alles aus der Unit | Die Cloze- und Grammar-Teile bekommen reihum einen **Grammatik-Schwerpunkt** der Unit, etwa „question tags“ oder „past progressive“ |
| Der Titel lautete „Wortpaare von Brawl Stars, Pokémon und Fußball“ | Regel 8 im System-Prompt: Der Titel nennt den Stoff, nie die Interessen |
| Ollama lief mit 32 768 Tokens Kontext, das Modell zu 73 % auf der CPU | `Agent:ContextLength` (Vorgabe 8192) wird bei jedem Aufruf als `num_ctx` mitgeschickt |

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
               "Temperature": 0.4, "ContextLength": 8192, "TimeoutSeconds": 180, "RepairAttempts": 1 }
}
```

`ContextLength` ist das Kontextfenster je Aufruf (Ollama `num_ctx`). Ohne die Angabe nimmt der Server seine
eigene Vorgabe. Bei 32 768 Tokens passte `qwen2.5:14b` nicht mehr in den Grafikspeicher und rechnete zu drei
Vierteln auf der CPU. Mit `0` entscheidet der Server.

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
| `--profile <id>` | Creator-Profil („Fachlehrer") | das Profil auf dem Buch des Kindes; hat es mehrere Bücher mit Profil, ist die Angabe Pflicht (B-481). Mit `--series` nur ein Profil, das zu Reihe und Fach passt – das der Reihe, sonst eines ohne Reihe im Fach, sonst keines (B-484, B-488) |
| `--general` | mit `--child`: Stoff des Kindes nutzen, aber **nicht** individualisieren | aus |
| `--unit <id>` | Unit der Buchreihe, aus der der Stoff kommt – und damit auch die, in der die Übung entsteht; neben einem anderen `--series-unit` eine Fehlbedienung | aktuelle Unit des Kindes |

Eines von `--child`/`--profile` ist Pflicht – sonst fehlt sowohl die Zielgruppe als auch das Fachwissen.

### Weitere Optionen

| Option | Bedeutung | Standard |
|---|---|---|
| `--type <Typ>` | `Vocabulary`, `Cloze`, `Translation`, `Grammar` | Pflicht bei `create` |
| `--series`/`--series-unit` | Reihe und Unit, in der die Übung entsteht (`--series-unit` allein bestimmt die Reihe; ein mit `--profile` genanntes Profil muss zu ihr passen: dieselbe Reihe oder keine, dasselbe Fach (auch als freier Fachname, ohne Groß/Klein) oder keines – geprüft gegen das Fach der Übung, auch in einer Reihe ohne Fach (B-495); das Fach der Übung ist das der Reihe, das des Profils nur, wo die Reihe keines hat – B-488) | Reihe des mit `--profile` genannten Profils – hat es keine, mit `--child` die Reihe seines Buchs im Fach des Profils, sonst eine Fehlbedienung (B-484); nur mit `--child` die Reihe des Profils auf seinem Buch (B-473) – unter mehreren Profilen dieses Buchs zuerst eines, das das Fach der Reihe unterrichtet (B-500) –, ohne ein solches Buch oder bei mehreren eine Fehlbedienung; ein Profil mit freiem Fachnamen nimmt nur Bücher dieses Fachs (B-498); Unit: die mit `--unit` genannte, sonst mit `--child` die aktuelle des Kindes in der Reihe, woher die Reihe auch kommt (B-481), sonst die erste der Reihe |
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
| `--use-weak` | schwach beherrschte Wörter des Kindes als Wortschatz, nur des Sprachpaars der Übung; braucht `--child`, verträgt kein `--general` | aus |
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
Ein Typ in `--types`, den der Agent nicht erzeugt, ist dagegen kein gescheiterter Teil: er bricht den Aufruf vor dem
ersten Modellaufruf als Fehlbedienung ab (Exit-Code 2, B-313; ebenso bei `plan`).
Ohne `--child` gibt es keinen Tag (Tags sind kind-skopiert) – dann hält allein die Quellenangabe das
Bündel zusammen.

### Der Lehrplan (`plan`)

Das Gegenstück zur Klausur mit demselben Ablauf, aber einem anderen Behälter: statt einer
Klassenarbeit entsteht ein **Lehrplan mit je einer Position** pro erzeugter Übung. `--per-type` zählt
hier **Übungen** (nicht Aufgaben – die kommen aus `--count`), und `--child` ist **Pflicht**: ein
Lehrplan gehört immer einem Kind. Die Ablehnung kommt, bevor das Modell läuft, damit ein Vertipper
nicht erst Minuten Erzeugung kostet.

**Hat die Unit eine Wortliste aus dem Buch, wird für `Vocabulary` nichts erzeugt** (B-463). Der Plan nimmt
dann die Vokabelübungen der Unit selbst auf – jede, die der Liste ein eigenes Wort beiträgt, und keine, die der
Agent erzeugt hat. So übt er die ganze Liste statt `--per-type` × `--count` Wörtern. Diese Positionen bekommen
Karteikasten, eine Tagesdosis von ⌈Karten / `--days`⌉ (die Liste ist am letzten Tag durch; fragt sie „beide“
Richtungen ab, sind es zwei Karten je Wort, B-472), getippten Test auf
der Stufe „frei tippen“. Die Richtung ist die der Buch-Übung. Wer früher fertig sein will, setzt `--days` auf den
Testtermin.

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

#### Aus Fotos der Buchseiten (`fill --images`, B-375)

```bash
ollama pull llama3.2-vision                                             # einmalig; anderes Modell: Agent:VisionModel
pugling-creator fill --series 3 --unit 12 --images fotos/unit-3 --dry-run    # Ordner: alle Bilder nach Namen
pugling-creator fill --series 3 --unit 12 --images s42.jpg,s43.jpg --source-file inhaltsverzeichnis.txt
```

Jede Seite geht **einzeln** an das lokale Vision-Modell (`Agent:VisionModel`, Vorgabe `llama3.2-vision`) und
kommt als **Stoffauszug** zurück: Themen, Grammatik in eigenen Worten, Wortpaare. Die Auszüge aller Seiten (dazu ein
optionales `--source-file`) sind dann die Quelle des gewohnten `fill`-Laufs auf dem Textmodell. Alle Regeln von oben
gelten unverändert. `--dry-run` druckt je Seite den erkannten Stoff, damit du ihn neben das Foto legen kannst.

**Copyright: Das Foto wird Stoff, nicht Übung.** Beide Prompts verbieten, Aufgabenstellungen, Lese- und Hörtexte,
Dialoge und Beispielsätze des Buches zu übernehmen. Gehalten wird der Schnitt aber im Code: Das Foto erreicht nur den
Vision-Aufruf, und `create`/`plan`/`exam` sehen allein die drei Stoffnotizen der Unit. Die Übungen entstehen also
neu aus dem Stoff und nicht aus den Aufgaben des Buches. Die Bilder gehen nur an das lokale Ollama, nie an die
Pugling-API, und werden nirgends gespeichert. Ein Prompt ist trotzdem nur eine Bitte. Lies den Auszug im Trockenlauf,
bevor du schreibst.

Formate: `.jpg/.jpeg/.png/.webp`. HEIC vom iPhone vorher als JPEG exportieren. Fehlt das Vision-Modell lokal, bricht
der Befehl vor dem ersten Modellaufruf ab (Exit 2) und nennt `ollama pull`. Auf CPU braucht eine Seite eher Minuten
als Sekunden. Läuft die Zeit ab, setze `Agent:TimeoutSeconds` hoch (gilt je Aufruf).

Danach entstehen die Übungen wie gewohnt aus der Unit, zum Beispiel
`pugling-creator plan --child 1 --types Vocabulary,Cloze` (Unit des Kindes) oder `create --profile 3 --unit 12 …`.

### Vokabelbilder nach Interessen (`images`, B-485)

```bash
pugling-creator images --child 1 --dry-run          # Prompts und Kostenschätzung, kein Schlüssel nötig
pugling-creator images --child 1 --max 20           # zeichnet höchstens 20 Wörter
pugling-creator images --unit 12 --tag fussball     # eine ganze Unit für ein Interesse vorproduzieren
```

Bilder entstehen je **Vokabel × Interesse**, nie je Kind: ein zweites Kind mit denselben Interessen bekommt dieselben
Bilder, und der Bild-Dienst sieht keine Daten eines Kindes. Je Wort zeichnet der Lauf ein neutrales Bild (für jedes
Kind ohne passenderes) und je eins für die zwei stärksten Themen-Interessen des Kindes. Welches Bild ein Kind dann
sieht, entscheidet wie immer der `MediaSelector`; die Interessen-Bilder hängen mit Rang −1 an der Vokabel, jedes
Bild ohne Schlagwort mit 0. So bekommt ein Kind ohne passendes Interesse das neutrale (auch ein älteres, von Hand
zugeordnetes) und nicht per Zufall das Fußball-Bild eines anderen Kindes.

Das lokale Textmodell beantwortet „zeichenbar?" und liefert die englische Szene des Worts. Für jedes Interesse
verbindet es diese Szene danach mit dem markenneutralen `ImagePrompt` zu **einer** Szene („a soccer player kicking a
ball down a city street"). Ein angehängtes „…, on a soccer pitch" übergehen Diffusionsmodelle (gemessen, B-485
Entscheidung 10). Label und Kinddaten sieht das Modell nie. Fällt eine Szene durch die Prüfung, entsteht kein
Interessen-Bild, und der Bericht nennt das Paar. Den Rest baut C#: Szene, fester Stil, Schriftverbot. Funktionswörter
und Abstrakta bleiben ohne Bild. Ein Interesse ohne `ImagePrompt` wird übersprungen und genannt, damit keine Marke
(„Minecraft") über ihren Namen beim Bild-Dienst landet.

Konfiguration im Abschnitt `Images`: `Model`, `Quality`, `Size`, `Style`, `CostPerImage`, optional `Endpoint` für
einen OpenAI-kompatiblen Dienst, `ResponseFormat` und `NegativeSeparator`. Für OpenAI nur den Schlüssel als Secret:

```bash
cd backend/Pugling.Agent.Creator
dotnet user-secrets set "Images:ApiKey" "sk-…"
```

**Lokal mit LocalAI** (der eingesetzte Weg, B-485 Entscheidung 9): LocalAI `v4.12.1` mit dem Modell `dreamshaper`
(Stable Diffusion 1.5, passt in 6 GB) läuft im Docker auf Port 8080:

```bash
docker run -d --name localai --gpus all -p 8080:8080 -v localai-models:/models localai/localai:v4.12.1-gpu-nvidia-cuda-12
curl http://localhost:8080/models/apply -H "Content-Type: application/json" -d "{\"id\": \"localai@dreamshaper\"}"
```

Dazu diese Werte, zum Beispiel als Secrets. Der Schlüssel ist bei LocalAI ein beliebiger Platzhalter:

```bash
dotnet user-secrets set "Images:Endpoint" "http://localhost:8080/v1"
dotnet user-secrets set "Images:Model" "dreamshaper"
dotnet user-secrets set "Images:Size" "512"
dotnet user-secrets set "Images:ResponseFormat" "Data"
dotnet user-secrets set "Images:NegativeSeparator" "|"
dotnet user-secrets set "Images:CostPerImage" "0"
dotnet user-secrets set "Images:ApiKey" "local"
```

`ResponseFormat=Data`: Ohne den Wert liefert LocalAI eine URL statt der Bilddaten. Bei OpenAI bleibt er leer, weil die
GPT-Bildmodelle den Parameter ablehnen. `NegativeSeparator=|`: Das Schriftverbot geht in den Negativ-Prompt, denn
„no text" im Prompt zeichnet ein Diffusionsmodell eher hinein. Ollama und LocalAI teilen sich die GPU. Lässt man
Ollama mit `OLLAMA_KEEP_ALIVE=0` laufen, gibt es den Speicher nach jeder Antwort frei. Ein Bild in 512 × 512 dauert
auf der RTX 4050 etwa 20–45 s.

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
