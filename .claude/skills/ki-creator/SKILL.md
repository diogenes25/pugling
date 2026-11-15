---
name: ki-creator
description: Den KI-Creator-Konsolenagenten (backend/Pugling.Agent.Creator) gegen die laufende Pugling-API fahren – Fachlehrer-Treffer prüfen (profiles), Briefing ohne LLM ansehen, Übungen erzeugen (create, auch --dry-run), Übungsklausuren planen (exam) und ganze Lehrpläne erzeugen (plan), vorhandene Übungen in den laufenden Plan eines Kindes übernehmen (assign, ohne LLM), Stoffnotizen einer Unit aus Text oder aus Fotos von Buchseiten befüllen (fill, fill --images), Bilder zu Vokabeln zeichnen lassen (images) und das Typ-Manifest des Servers zeigen (types). Nutze dies, wenn Übungen, eine Klausur oder ein kompletter Lehrplan per LLM erzeugt werden sollen, wenn der Agent selbst eingerichtet oder debuggt wird (Ollama-Modell, Pugling:Pin), oder bei Fragen zu individuell (--child) vs. allgemein (--profile).
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
dotnet run --project backend/Pugling.Agent.Creator -- create --child 3 --type Cloze --count 12 --schultest   # Schul-Vokabeltest (B-524)
dotnet run --project backend/Pugling.Agent.Creator -- plan --child 1 --types Vocabulary,Cloze --per-type 2 --intensity Normal
dotnet run --project backend/Pugling.Agent.Creator -- assign --child 1 --exercise-id 12 --exercise-id 15 --dry-run  # in den laufenden Plan
dotnet run --project backend/Pugling.Agent.Creator -- fill --series 3 --unit 12 --source-file inhalt.txt --dry-run  # Stoffnotizen
dotnet run --project backend/Pugling.Agent.Creator -- images --child 1 --dry-run   # Vokabelbilder: Prompts + Kosten, ohne Schlüssel
dotnet run --project backend/Pugling.Agent.Creator -- images --unit 12 --tag fussball --max 50
```

`images` (B-485) zeichnet Vokabelbilder nach den Interessen eines Kindes: je Wort ein **neutrales** Bild und je eins
für die **zwei stärksten Themen-Interessen** (Gewicht ≥ 2). Die Arbeitsliste kommt aus `GET creator/media/demand`;
das lokale Textmodell entscheidet, ob ein Wort zeichenbar ist und welche englische Szene es zeigt, und verwebt
diese Szene je Interesse mit dessen `ImagePrompt` (B-485 Entscheidung 10; keine Szene, kein Interessen-Bild). Den Rest
baut C# (fester Stil, Schriftverbot, als Negativ-Prompt bei gesetztem `Images:NegativeSeparator`). Gezeichnet wird
über einen OpenAI-kompatiblen Dienst. Eingesetzt ist **LocalAI mit `dreamshaper`** auf Port 8080 (Entscheidung 9).
Einrichtung und die sieben `Images:`-Secrets stehen im README des Agenten unter „Lokal mit LocalAI". Ohne
`Endpoint` geht es an OpenAI (`gpt-image-1-mini`, Schlüssel als `Images:ApiKey`). Vor einem Lauf prüfen, ob
LocalAI antwortet (`curl http://localhost:8080/v1/models`) und Ollama läuft. Bilder gehen **ohne
Freigabe** direkt an die Vokabel (das Kind verwirft mit „anderes Bild", der Vater filtert in der Bild-Bibliothek
auf „nur KI-erzeugte"). Ein Interesse ohne `ImagePrompt` bekommt keine Bilder und wird im Bericht genannt – den
Prompt pflegt man markenneutral in der Schlagwort-Verwaltung. Zweiter Lauf zeichnet nichts doppelt.

`fill` entwirft die Stoffnotizen einer Unit (`Topics`/`Grammar`/`VocabularyNotes`) aus einer Quelle, die du
lieferst (`--source-file <Datei>` oder `-` für Stdin, nur mit Umleitung/Pipe) – ohne Quelle Fehlbedienung, das
Modell erfindet keinen Stoff. Nur leere Felder werden befüllt, `--force` überschreibt, `--dry-run` zeigt nur den
Entwurf; zu lange Themen werden weggelassen und genannt, nie gekürzt. Die Reihe muss dir gehören (geseedete
Reihen gehören niemandem).

**Aus Fotos der Buchseiten** (B-375): `fill --series 3 --unit 12 --images <Datei|Ordner>[,…] --dry-run`. Jede Seite
liest einzeln das lokale Vision-Modell (`Agent:VisionModel`, Vorgabe `llama3.2-vision`, fehlt es: Exit 2 mit
`ollama pull`). Es liefert nur **Stoff** (Themen, Regeln in eigenen Worten, Wortpaare) und keine Aufgaben oder
Texte des Buches. Die Übungen entstehen danach mit `create`/`plan` aus den Notizen. Das Foto verlässt den Rechner
nicht. Die Regel stammt vom Nutzer: Copyright wahren und den aktuellen Unterrichtsstoff lernen, ohne die Übungen des
Originals. **Vorsicht:** Ein kleines lokales Vision-Modell liest dicht bedruckte Buch- und Doppelseiten unzuverlässig
(B-375: falsche Grammatik, Themen als Wörter) – nur einzelne, groß fotografierte Seiten, immer mit `--dry-run` und
Abgleich am Foto; ein ganzes Lehrwerk bereitet der Skill `lehrwerk-aufbereiten` auf (B-459).

`assign` (B-20) übernimmt vorhandene Übungen (`--exercise-id`, wiederholbar, oder `--tag <name>` des Kindes) als
Positionen in den **spielbaren** Plan des Kindes (`--plan-id` für einen anderen eigenen Plan) – ohne Sprachmodell,
also auch ohne Ollama. Schon vorhandene Positionen werden übersprungen (nichts neu ⇒ Exit 3, nicht 2 – der Aufruf war ja richtig), Ziel und Punkte sind
Server-Vorgaben, übersteuerbar mit `--points`, `--goal-threshold`, `--cadence Daily|Weekly`. Typischer Weg: `exam`
oder `create` erzeugt, `assign --tag`/`--exercise-id` hängt es in den laufenden Plan.

Bei `plan` zählt `--per-type` **Übungen** je Typ (je eine Position), nicht Aufgaben – die kommen wie bei
`create` aus `--count`. `--child` ist dort Pflicht, und der erzeugte Lehrplan ist **inaktiv**: der Vater
sichtet ihn unter `/vater/plaene` und schaltet ihn selbst scharf, damit ungeprüfter KI-Inhalt nicht über
Nacht zur Pflicht wird. Ziel/Punkte/Malus je Position kommen aus `--intensity`
(`Locker` 70 %/10/0 · `Normal` 80 %/20/−5 · `Intensiv` 90 %/30/−10) – dieselben Zeilen wie im
Lehrplan-Assistenten des Vater-Webs. Hat die Unit eine Wortliste aus dem Buch, erzeugt `plan` für
`Vocabulary` nichts, sondern nimmt deren Vokabelübungen ganz auf – mit Karteikasten, Tagesdosis bis
Planende (⌈Einträge / `--days`⌉) und getipptem Test (B-463).

## Betriebsarten

**Individuell** (`--child`) verlangt ein Konto mit Creator **und** Supervisor-Rolle, das das Kind
betreut (Seed: Konto 1) – daraus entsteht eine auf das Kind zugeschnittene Übung.
**Allgemein** (`--profile`, ohne Kind) genügt die Creator-Rolle und erzeugt Katalogware; Klassenstufe,
Schulart und Quelle kommen dann aus dem Profil. Ohne `--profile` nimmt der Agent das Profil auf der Reihe des
Kinder-Buchs (`profiles/match`); hat das Kind mehrere Bücher mit Profil, ist `--profile` Pflicht. Ist ein Ort genannt
(`--series`), wählt das Briefing nur unter Profilen, die passen (`ProfileFit`: Reihe und Fach) – bevorzugt das der
Reihe, sonst ein werkunabhängiges des Fachs der Reihe (B-488). Ein werkunabhängiges `--profile` legt mit Kind im
Buch des Kindes in seinem Fach ab, ohne Kind braucht es `--series`. Den Stoff liest der Agent immer aus der Unit, in
der die Übung entsteht (B-484).

Fachliche Kernregel: **Interessen kleiden den Stoff ein, sie ersetzen ihn nie.**

**Gute Übungen brauchen eine Wortliste in der Unit** (B-460). Hat die Unit Vokabelübungen, nimmt eine erzeugte
Vokabelübung Wörter und Übersetzungen von dort, und das Modell schreibt nur Titel und Merkhinweise. Ohne
Wortliste erfindet das Modell die Übersetzungen selbst. Eine Unit ohne Wortliste also zuerst mit
Vokabelübungen aus der Buchliste füllen (beim Lehrwerk aus Fotos: Skill `lehrwerk-aufbereiten`). Ein Lehrplan gibt jedem
Cloze-/Grammar-Teil reihum ein Grammatik-Thema der Unit. Das setzt voraus, dass die Unit Grammatik-Themen trägt.

Details (Pipeline, Strategien, Tests ohne Ollama):
[backend/Pugling.Agent.Creator/README.md](../../../backend/Pugling.Agent.Creator/README.md).
