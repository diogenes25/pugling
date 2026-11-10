# Pugling.Agent.Creator – KI-Creator

Konsolen-App, die die Creator-Rolle übernimmt – lokal gegen Ollama
(`Microsoft.Extensions.AI`/`IChatClient` + OllamaSharp). Aufrufe und Betriebsarten stehen im Skill
`ki-creator`, die Bedienung im [README](README.md).

**Deterministische Pipeline**: C# besitzt den Ablauf (Briefing → Entwurf → Regelprüfung mit
Reparatur-Runde → Anlegen → nebenwirkungsfreier Selbsttest über `preview`/`preview/check`), das Modell
liefert nur strukturierten Inhalt – kein Tool-Calling. Fachliche Kernregel: **Interessen kleiden den Stoff
ein, sie ersetzen ihn nie** (bei vorgegebenem Wortschatz deterministisch erzwungen).

Das `CreatorBriefing` hat **zwei Quellen**: `ProfileFacts` (der Fachlehrer samt Reihe/Unit-Stoff) und
**optional** `ChildFacts` – daran hängt die Betriebsart: mit Kind entsteht eine *individuelle* Übung, ohne
eine *allgemeine* für den geteilten Katalog (dann kommen Klassenstufen-Bereich/Schulart/Quelle aus dem
Profil, und der Agent braucht kein Betreuungsrecht). Ohne `--profile` sucht der Agent selbst das
bestpassende (`profiles/match`).

Der `ExamPlanner` baut **Übungsklausuren**: ein Pipeline-Lauf je Typ (jeder mit eigenem Selbsttest),
danach – nur mit Kind – ein kind-skopierter Tag und eine `Klassenarbeit` (Status *geplant*) mit genau
diesen Übungen; ein gescheiterter Teil bricht die Klausur nicht ab, macht sie aber sichtbar unvollständig
(Exit 1). Ein Typ in `--types`, den der Agent nicht erzeugt, ist dagegen kein gescheiterter Teil, sondern eine
Fehlbedienung des ganzen Aufrufs – vor dem ersten Modellaufruf, Exit 2 (B-313; gilt ebenso für `plan`).

Der `PlanPlanner` ist sein Zwilling für **Lehrpläne**: gleicher Ablauf, anderer Behälter – ein
`StudyPlan` mit je einer `PlanPosition` pro erzeugter Übung, Ziel/Punkte/Malus aus `--intensity`
(dieselben drei Zeilen wie `VaterWizard.INTENSITY`). Zwei Regeln tragen ihn: `--child` ist Pflicht und
wird **vor** dem ersten Modellaufruf geprüft, und der Plan entsteht **inaktiv** – ungeprüfter KI-Inhalt
darf nicht über Nacht zur Pflicht werden, und ein aktiv angelegter Plan würde den laufenden Plan des
Kindes abschalten. Dafür trägt `CreatePlanDto` seit B-19 ein `Active`-Feld (Vorgabe `true`).

`fill --images` (B-375) liest Fotos von Buchseiten mit dem `PageReader`: eine Seite je Aufruf auf
`Agent:VisionModel` (per `ChatOptions.ModelId`), Ergebnis ist ein Stoffauszug als Klartext. Daraus werden nur die
Stoffnotizen. **Das Foto wird Stoff, nicht Übung**: Kein Schritt nach dem Vision-Aufruf sieht das Bild, und es geht
nie an die API (Copyright: Aufgaben und Texte des Buches gelangen nicht in den Katalog).

Neuer Typ = eine Klasse auf `ExerciseStrategy<TDraft,TConfig>`. Getestet ohne Ollama via `FakeChatClient`
(`CreatorAgentTests`).
