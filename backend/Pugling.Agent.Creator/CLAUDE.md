# Pugling.Agent.Creator – KI-Creator

Konsolen-App, die die Creator-Rolle übernimmt – lokal gegen Ollama
(`Microsoft.Extensions.AI`/`IChatClient` + OllamaSharp). Aufrufe und Betriebsarten stehen im Skill
`ki-creator`, die Bedienung im [README](README.md).

**Deterministische Pipeline**: C# besitzt den Ablauf (Briefing → Entwurf → Regelprüfung mit
Reparatur-Runde → Anlegen → nebenwirkungsfreier Selbsttest über `preview`/`preview/check`), das Modell
liefert nur strukturierten Inhalt – kein Tool-Calling. Fachliche Kernregel: **Interessen kleiden den Stoff
ein, sie ersetzen ihn nie** (bei vorgegebenem Wortschatz deterministisch erzwungen).

Das `CreatorBriefing` hat **zwei Quellen**: `ProfileFacts` (der Fachlehrer; Werk und Unit-Stoff stehen als
`MaterialFacts` immer daneben, B-484) und **optional** `ChildFacts` – daran hängt die Betriebsart: mit Kind entsteht eine *individuelle* Übung, ohne
eine *allgemeine* für den geteilten Katalog (dann kommen Klassenstufen-Bereich/Schulart/Quelle aus dem
Profil, und der Agent braucht kein Betreuungsrecht). Ohne `--profile` nimmt der Agent mit Kind das
Profil auf der Reihe seines Buchs (`profiles/match`, `series_match`); mehrere solche Bücher sind eine Fehlbedienung.
**Ort und Stoff entscheidet eine Stelle** (`BuildRequestAsync`, B-484): Reihe, Unit und Fach (das der Reihe, B-488)
stehen danach fest, das Briefing liest den Stoff aus genau dieser Unit und wählt ein ungenanntes Profil nur unter
denen, die passen – das der Reihe, sonst eines ohne Reihe.
Ein werkunabhängiges Profil nimmt die Reihe aus dem Buch des Kindes in seinem Fach – nie die erste des Katalogs.
**Ob ein Profil zum Ort passt** (Reihe und Fach), entscheidet nur `ProfileFit` im Briefing – der Senke, die `create`,
`plan` und `exam` vor dem ersten Schreiben durchlaufen (B-488); die Konsole wählt Buch und Profil mit derselben Regel
(`FitsSubjectOf`), nie mit einer eigenen – und beim Schreiben mit der Sperre der Senke (Reihe ohne Fach, B-510).

Der `ExamPlanner` baut **Übungsklausuren**: ein Pipeline-Lauf je Typ (jeder mit eigenem Selbsttest),
danach – nur mit Kind – ein kind-skopierter Tag und eine `Klassenarbeit` (Status *geplant*) mit genau
diesen Übungen; ein gescheiterter Teil bricht die Klausur nicht ab, macht sie aber sichtbar unvollständig
(Exit 1). Ein Typ in `--types`, den der Agent nicht erzeugt, ist dagegen kein gescheiterter Teil, sondern eine
Fehlbedienung des ganzen Aufrufs – vor dem ersten Modellaufruf, Exit 2 (B-313; gilt ebenso für `plan`).

Der `PlanPlanner` ist sein Zwilling für **Lehrpläne**: gleicher Ablauf, anderer Behälter – ein
`StudyPlan` mit je einer `PlanPosition` pro erzeugter Übung, Ziel/Punkte/Malus aus `--intensity`
(dieselben drei Zeilen wie `VaterWizard.INTENSITY`). Hat die Unit eine Buch-Wortliste, plant er für
`Vocabulary` deren Übungen ganz ein statt Scheiben zu erzeugen, mit Karteikasten, Tagesdosis und getipptem
Test (B-463). Zwei Regeln tragen ihn: `--child` ist Pflicht und
wird **vor** dem ersten Modellaufruf geprüft, und der Plan entsteht **inaktiv** – ungeprüfter KI-Inhalt
darf nicht über Nacht zur Pflicht werden, und ein aktiv angelegter Plan würde den laufenden Plan des
Kindes abschalten. Dafür trägt `CreatePlanDto` seit B-19 ein `Active`-Feld (Vorgabe `true`).

`fill --images` (B-375) liest Fotos von Buchseiten mit dem `PageReader`: eine Seite je Aufruf auf
`Agent:VisionModel` (per `ChatOptions.ModelId`), Ergebnis ist ein Stoffauszug als Klartext. Daraus werden nur die
Stoffnotizen. **Das Foto wird Stoff, nicht Übung**: Kein Schritt nach dem Vision-Aufruf sieht das Bild, und es geht
nie an die API (Copyright: Aufgaben und Texte des Buches gelangen nicht in den Katalog).

`images` (B-485) zeichnet über einen **OpenAI-kompatiblen Bild-Dienst**, eingesetzt lokal (LocalAI, siehe README):
`ImageRun` liest `creator/media/demand`, der `MotifPlanner` fragt das lokale Modell „zeichenbar? welche Szene?" und je
Interesse nach der mit dessen `ImagePrompt` verwobenen Szene (nie Label oder Kinddaten; keine Szene → kein
Interessen-Bild). `ImagePromptBuilder` baut Prompt, Schlüssel, Spur und Alt-Text deterministisch, gezeichnet wird über
`IImageGenerator` (`Lazy` – ohne Schlüssel laufen Trockenlauf und alle anderen Befehle). `IImageGenerator` ist
experimentell (`MEAI001`, nur hier und in den Tests unterdrückt). Getestet mit `FakeImageGenerator` (`ImageAgentTests`).

**Was nur eine richtige Antwort hat, entscheidet C#, nicht das Modell** (B-460). Gemeint ist Nummerierung,
Auswahl, Übersetzung aus der Wortliste und Kürzen auf die Anzahl. Der Entwurf ist so geschnitten, dass das
Modell nur Sprache liefert:

- Lückentexte schreibt es als `[Lösung]` im Text, Ablenker getrennt. Daraus baut `ClozeMarkup` Platzhalter,
  Lücken und Wortbank.
- Vokabeln kommen aus der Wortliste der Unit (`UnitVocabulary`, gelesen aus ihren Vokabelübungen). Das Modell
  schreibt nur Titel und Hinweise.
- `Normalize` räumt vor den Regeln auf, was eindeutig ist. Eine Reparatur-Runde kostet einen ganzen Modellaufruf.

Neuer Typ = eine Klasse auf `ExerciseStrategy<TDraft,TConfig>`. Getestet ohne Ollama via `FakeChatClient`
(`CreatorAgentTests`).
