---
tags: [typ/story, status/in-arbeit, bereich/katalog, bereich/training, rolle/creator, rolle/supervisor]
aliases: [Heftseite zur Übung, Scan zur Übung, Buchseite zu Stoffnotizen, Foto zu Stoffnotizen]
status: in-arbeit
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/baobab-vergleich-plan.md#wo-baobab-weiter-ist
grund: ""
ersetzt_durch: []
wartet_auf: "Fotos echter Buchseiten vom Nutzer (Rollengang am echten Lehrwerk)"
---

# B-375 · Fotografierte Buchseiten werden zu Stoffnotizen einer Unit, aus denen eigene Übungen entstehen

Ursprünglich: „Eine fotografierte Heftseite wird zum Übungsentwurf, den der Vater vor dem Zuweisen prüft“
(Baobab-Vergleich). Am 2026-11-10 im Dialog umgeschnitten: Das Foto wird **nicht** zur Übung, sondern zum
**Stoff** – damit das Kind genau das lernt, was gerade im Unterricht dran ist, ohne die Aufgaben des
Lehrwerks zu kopieren (Entscheidung 1).

## User Story

Als *Vater* (Creator-Konto, Besitzer einer Lehrwerk-Reihe) möchte ich die Seiten der aktuellen Unit
abfotografieren und daraus die Stoffnotizen der Unit (`Topics`, `Grammar`, `VocabularyNotes`) entwerfen
lassen, damit der KI-Creator anschließend **eigene** Übungen genau zu diesem Stoff erzeugt – ohne dass
Aufgaben, Texte oder Beispielsätze des Buches in den Katalog oder zum Kind gelangen.

## Ist-Stand am Code

- Der Weg „Quelle → Stoffnotizen“ existiert seit B-22 als Verb `fill`: `UnitNotesFiller.RunAsync`
  (`backend/Pugling.Agent.Creator/UnitNotesFiller.cs:85`) prüft Reihe und Unit, lässt das Modell die Quelle in
  drei Felder fassen und schreibt nur leere Felder per Unit-PATCH (`UnitNotesFiller.cs:121`). Die Quelle ist
  **nur Text**: `AgentCommands.ReadSourceAsync` (`AgentCommands.cs:83`) liest `--source-file` oder Stdin. Bilder
  nimmt kein Verb an.
- Die Übungserzeugung liest den Stoff ausschließlich über diese drei Felder:
  `ProfileFacts` (`Briefing/ProfileFacts.cs:62-65`) schreibt Themen, Grammatik und Wortschatz der Unit ins
  Briefing, und `create`/`exam`/`plan` bauen darauf. Eine Buchseite erreicht die Übungserzeugung also heute
  nur über die Stoffnotizen. Genau diese Engstelle macht den Copyright-Schnitt strukturell (Entscheidung 1).
- Ein Modell-Wechsel pro Aufruf geht ohne zweiten Client: Das Modell kommt aus `AgentOptions.Model`
  (`AgentOptions.cs:24`) als Vorgabe des `OllamaApiClient` (`Program.cs:126`). `ChatOptions.ModelId` überschreibt
  sie je Aufruf.
- Lokal installiert sind (2026-11-10, `ollama list`) `qwen2.5:14b-instruct` und `gemma4:e2b`, aber kein
  `llama3.2-vision`. Der Fall „Vision-Modell fehlt“ ist also der Normalfall beim ersten Aufruf.
- Tests des Verbs: `CreatorAgentTests.Fill_*` (`backend/Pugling.Api.Tests/CreatorAgentTests.cs:67-375`) gegen
  die echte In-Process-API mit `FakeChatClient`. Der Fake merkt sich nur die **letzten** Nachrichten
  (`FakeChatClient.cs:17`). Ob ein Bild im richtigen Aufruf steckt, kann damit heute niemand prüfen.

## Die echte Lücke

Es fehlt nur der Schritt **Foto → Text** vor `fill`, und eine Regel, dass dabei nur Stoff entsteht und keine
Abschrift. Übungserzeugung, Prüfung im Testmodus und Lehrplan stehen schon. Der ursprüngliche Zuschnitt
(Foto → Übungsentwurf) hätte dagegen die Aufgaben des Buches in den Katalog gebracht. Genau das soll nicht
passieren.

## Offene Punkte

1. ~~Abgrenzung zu B-251~~ → Entscheidung 2.
2. ~~Wo prüft der Vater?~~ → Entscheidung 4.
3. ~~Welche Typen?~~ → gegenstandslos durch Entscheidung 1: Die Typen kommen unverändert aus `create`.
4. ~~Ein Bild je Aufruf oder alle Seiten in einem?~~ → Entscheidung 3.
5. ~~Was geschieht, wenn das Vision-Modell fehlt?~~ → Entscheidung 5.
6. ~~Wird das Foto gespeichert?~~ → Entscheidung 6.

## Entscheidungen

1. **Das Foto wird Stoff, nicht Übung** (Nutzer, Dialog 2026-11-10: „Das Copyright soll nicht verletzt
   werden … ohne direkt auf die Übungen des Originals zu kommen“; den Vorschlag „nur Stoff, keine Übungen aus
   dem Foto“ bestätigt). Der Foto-Weg endet in den drei Stoffnotizen der Unit, und Übungen entstehen danach
   wie bisher über `create`/`plan`/`exam` aus diesen Notizen. Übernommen werden Themen, Grammatikregeln **in
   eigenen Worten** und Wortpaare. Nicht übernommen werden Aufgabenstellungen, Lese- und Hörtexte, Dialoge,
   Beispielsätze, Aufgabennummern und Bilder. *Begründung:* Fakten, Regeln und einzelne Vokabeln sind in der
   Regel nicht geschützt, die Gestaltung von Aufgaben und Texten schon. Die Engstelle der drei Felder hält
   den Schnitt im Code, nicht nur im Prompt: Die Übungserzeugung sieht das Foto nie. *Kosten:* Die Regel im
   Vision-Prompt bleibt eine Bitte an das Modell. Eine wörtlich abgeschriebene Regel-Erklärung kann trotzdem
   in `grammar` landen. Dagegen hilft nur das Lesen im `--dry-run` (Entscheidung 4). Ein mechanischer
   Abschrift-Detektor bräuchte den Originaltext, also genau die Abschrift, die wir nicht erzeugen wollen.
   Dazu kommt Leerlauf: Wer wirklich Übungen aus einem Arbeitsblatt will, bekommt sie nicht.
2. **B-375 baut den Foto-Weg selbst; B-251 bleibt eigenständig und nutzt ihn später.** Gelesen wird wie in
   B-251 (G-8) mit einem lokalen Vision-Modell in Ollama, ohne OCR-Vorstufe. Diese Story liefert den Lese-
   Baustein (`PageReader`) und den Eingang `--images` an `fill`. B-251 (Wortpaare in den Vokabelspeicher)
   nimmt denselben Baustein später für den Speicher statt für die Notizen. *Begründung:* Für „das Kind lernt den
   aktuellen Stoff“ genügt der Wortschatz in `VocabularyNotes`, denn `ProfileFacts.cs:64` reicht ihn an jede
   Übung weiter. Der Speicher-Import mit Dubletten-Auflösung ist eigene Arbeit. *Kosten:* Bis B-251 steht, sind
   die Wörter einer Unit nur Notiz-Text und keine Store-Einträge mit Lernstand je Vokabel.
3. **Eine Seite je Modellaufruf, danach ein Strukturierungsaufruf.** Jede Seite geht einzeln an das
   Vision-Modell und kommt als Klartext-Auszug zurück („Stoffauszug“, kein JSON). Die Auszüge aller Seiten
   werden zusammen mit einem optionalen `--source-file` zur Quelle des bestehenden `fill`-Aufrufs auf dem
   Textmodell. *Begründung:* `llama3.2-vision` nimmt in Ollama nur ein Bild je Anfrage, und kleine
   Vision-Modelle liefern unzuverlässiges JSON. Das Textmodell und seine Regeln aus B-22/B-292 bleiben
   unverändert. *Kosten:* n+1 Modellaufrufe statt einem. Auf CPU braucht eine Seite eher Minuten, deshalb
   nennt das README `Agent:TimeoutSeconds`.
4. **Prüfung ohne neue Oberfläche.** `--dry-run` druckt je Seite den erkannten Stoffauszug und danach den
   Entwurf der Notizen. Der Vater legt das neben das Foto und liest. Die daraus erzeugten Übungen prüft er
   wie jede KI-Übung im Testmodus, und `plan` legt den Lehrplan weiterhin **inaktiv** an. Bestätigt die
   Entscheidung vom 2026-11-05, Punkt 2. *Kosten:* Ob der Scan alles erfasst hat, sieht nur, wer liest.
5. **Vision-Modell konfigurierbar (`Agent:VisionModel`, Vorgabe `llama3.2-vision`); fehlt es lokal, Exit 2
   mit `ollama pull`-Hinweis, vor dem ersten Modellaufruf** (Rest-Entscheidung zu B-251 vom 2026-11-05, hier
   übernommen). *Begründung:* Ohne das Modell hätte der Lauf minutenlang Reihe und Unit geprüft und wäre dann
   mit einer rohen Ollama-404 gescheitert. *Kosten:* Eine kleine Abfrage der lokalen Modellliste
   (`IModelInventory`) vor dem Lesen. In den Tests ersetzt sie ein Fake.
6. **Das Foto verlässt den Rechner nicht und wird nirgends gespeichert.** Die Bilder gehen nur an das lokale
   Ollama, nie an die Pugling-API. Gespeichert wird nur, was der PATCH der drei Felder trägt. *Begründung:*
   Eine abgelegte Buchseite wäre genau die Kopie, die Entscheidung 1 vermeidet, und kein LLM läuft im Backend
   (G-8). *Kosten:* Später lässt sich nicht mehr nachsehen, aus welchem Foto eine Notiz stammt. Den Abgleich
   gibt es nur im Moment des `--dry-run`.

## Akzeptanzkriterien

1. `fill --series <id> --unit <id> --images <Datei|Ordner>[,…]` liest jede Seite einzeln mit dem
   Vision-Modell und befüllt aus den Auszügen die leeren Stoffnotizen der Unit. Ein Ordner heißt: alle
   `.jpg/.jpeg/.png/.webp` darin, nach Namen sortiert. `--source-file` bleibt erlaubt und wird angehängt.
2. Der Vision-Aufruf trägt das Bild und läuft auf `Agent:VisionModel`. Der Strukturierungsaufruf trägt
   **kein** Bild. Die Pugling-API empfängt außer dem Unit-PATCH nichts (Entscheidungen 1 und 6).
3. Der Vision-Prompt verbietet ausdrücklich, Aufgaben, Texte, Dialoge und Beispielsätze zu übernehmen, und
   verlangt Regeln in eigenen Worten. Der Systemprompt des Strukturierungsaufrufs sagt dasselbe.
4. `--dry-run` druckt je Seite den Stoffauszug (mit Dateinamen) und den Entwurf und schreibt nichts.
5. Fehlt das Vision-Modell lokal: Fehlbedienung (Exit 2), die Meldung nennt `ollama pull <Modell>`, und kein
   Modell wurde gefragt.
6. Ohne `--images` und ohne `--source-file`: Fehlbedienung wie bisher. Eine fehlende Bilddatei, ein Ordner ohne
   Bilder oder ein nicht unterstütztes Format (z. B. `.heic`): Fehlbedienung, die die Datei nennt.
7. Bestehende `Fill_*`-Tests bleiben grün. Die Regeln aus B-22/B-292 gelten unverändert für den Entwurf aus
   Fotos.

## Schätzung

- **Größe M**: ein neuer Baustein (`PageReader` + `IModelInventory`), ein erweitertes Verb und eine
  Fake-Erweiterung. Ungefähr vergleichbar mit dem vokabel-basierten Batch-Pfad des Ankers B-03.
- **`wo: backend`**: nur `Pugling.Agent.Creator` und `Pugling.Api.Tests`. Kein Endpunkt, kein Frontend.
- **`migration: nein`** (kein Schema). **`vertragsbruch: nein`**: `Pugling.Contracts` bleibt unberührt, der
  Agent nutzt den bestehenden Unit-PATCH.
- **Risiken:** (a) Das Mapping von `DataContent` auf Ollama-Bilder in OllamaSharp 5.4.30 ist nur am echten
  Modell zu sehen und mit dem Fake nicht prüfbar, darum braucht es einen Live-Lauf mit einem installierten
  Vision-Modell. (b) Die Lesequalität kleiner Modelle bei schräg fotografierten Seiten. Das fängt der
  `--dry-run`, nicht der Code.
- **Angriffsplan:** `AgentOptions.VisionModel` → `IModelInventory`/`OllamaModelInventory` → `PageReader`
  (Laden, Lesen) → `UnitNotesFiller` nimmt Seiten an → `AgentCommands --images` und Hilfetext → Doku
  (README, Skill `ki-creator`, Agent-`CLAUDE.md`).
- **Testweg:** neue Fälle in `CreatorAgentTests` (`Fill_Aus_Fotos_*`) gegen die In-Process-API mit einem
  erweiterten `FakeChatClient`, der jeden Aufruf samt `ModelId` mitschreibt. Dazu je Fall eine rote Probe am
  benannten Ort und ein Live-Lauf gegen Ollama mit einem lokal vorhandenen Vision-Modell (`--dry-run`). Einen
  Rollengang an der App gibt es hier nicht: Die Oberfläche ist die Konsole, der Lauf ist der Gang des
  Creators.

## Verlauf

- **2026-11-05** — angelegt (Quelle: Baobab-Vergleich, Etappe 1 des Übernahmeplans).
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): (1) Nachfolger von B-251 auf demselben Vision-Weg; (2) Prüfung ohne neue Oberfläche: `--dry-run` zeigt erkannten Text und Entwurf, angelegt wird ohne Zuweisung, der Vater probiert im Testmodus; (3) nur Typen, die der Agent heute kann. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-10** — idee → ausformuliert → gegrillt → geschaetzt → in-arbeit in einer Sitzung, auf ausdrücklichen Auftrag des Nutzers im Dialog („Arbeite dieses Issue komplett aus und implementiere es anschließend“). Umgeschnitten auf „Foto → Stoffnotizen“ (Entscheidung 1, vom Nutzer bestätigt). Punkt (1) vom 2026-11-05 („Nachfolger von B-251“) ist damit revidiert: B-375 baut den Lese-Baustein zuerst (Entscheidung 2). Die Entscheidungen 3–6 hat Claude unter derselben Freigabe getroffen; der Nutzer kann jede umstoßen.
- **2026-11-10** — gebaut: `PageReader` (Laden, Seitenreihenfolge mit Zahlen als Zahlen, Lesen je Seite auf `Agent:VisionModel`), `IModelInventory` gegen `ollama list`, `fill --images`, `CommandLine.Has`. Belege: `CreatorAgentTests` 94/94, Suite **1220/1220** (`dotnet test Pugling.sln -c Release`), `dotnet format` sauber, Markdown-Lint 0. Rote Probe je neuem Fall am benannten Ort (11 Mutanten, jede rot an genau ihrem Test). Gemessen: Die Ordner-Sortierung allein bleibt auf NTFS grün, weil das Dateisystem schon nach Namen liefert. Darum gibt es den eigenen Fall `Fotos_Reihenfolge_*` mit umgedrehter Eingabe. `pugling-reviewer`: keine Blocker, fünf kleine Funde, vier davon behoben (fehlender Ordner bekommt eine eigene Meldung, ein blankes `--source-file` neben `--images` wird gemeldet, doppelte Seiten werden entfernt, Zahlen auf 20 Stellen aufgefüllt). Den fünften (Wortlaut bei `--source-file=""`) habe ich gelassen, es bleibt Exit 2. **Live-Lauf** gegen eine isolierte API (Port 5280, Wegwerf-DB) mit einer selbst gerenderten Testseite (Grammatik, „Exercise 4“ mit Lückensätzen, Wortliste) und `gemma4:e2b` als Vision-Modell (`llama3.2-vision` ist lokal nicht installiert, dafür Exit 2 mit `ollama pull`-Hinweis, ebenfalls live gesehen). Ergebnis: Themen, Regel in eigenen Worten und alle fünf Wortpaare erkannt, **keiner der drei Übungssätze übernommen**. Etwa 70 s je Seite. Der schreibende Lauf hat die drei Felder per PATCH gesetzt und sie wurden zurückgelesen. Zwei Live-Funde sofort behoben, je mit Test und roter Probe: Umlaute im gedruckten Entwurf erschienen als `ü` (jetzt lesbares Encoding), und die Übersetzungen fielen beim Strukturieren weg (Prompt-Regel „Übersetzung bleibt dabei“). **Rollengang offen:** Mit echten Buchseiten des Nutzers lief noch nichts. Er bringt die Fotos in diese Sitzung. Erst danach steht `abgenommen` an (`wartet_auf`).
