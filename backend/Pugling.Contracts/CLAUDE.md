# Pugling.Contracts – das Vertrags-Projekt

*Alle* Request-/Response-`record`s und die geteilten Basistypen (Enums wie `PointKind`/`GoalCadence`,
`StageStep`, `NounInfo`, die Übungs-Configs) liegen hier – **nicht** als verschachtelte Typen im Controller.

**Aufteilung**: `Common/` + `Exercise/` = Wurzel-Namespace `Pugling.Contracts` (ebenen-neutral), dazu je
Ebene ein Ordner **und** Namespace `Pugling.Contracts.{Auth,Creator,Supervisor,Student,Shared}`; alle sechs
sind per csproj-`<Using>` projektweit sichtbar.

Das Projekt ist ein **Blatt**: keine Referenz auf `Pugling.Api`, kein EF, keine Entities – damit ein Client
es pur verwenden kann. Folgerichtig bleibt Entity-Wissen in der API (Mapping-Klasse statt Factory am Record,
siehe `ExerciseBriefMapping`), und Service-Ergebnisse, die `ApiError` oder Entities tragen, bleiben ebenfalls
dort.

Einzige reine Funktion neben den Typen (und dem `Exercise/VocabRefJsonConverter.cs`): `Common/VocabKey.cs`
(B-336). Der Vokabel-Schlüssel ist der Server-Begriff „dasselbe Wort", und der KI-Creator muss vor dem Anlegen
genauso falten – eine zweite Kopie würde driften.

Beschreibt ein `/// <summary>` ein **Verhalten** (wann ein Treffer, wann ein Fehler), ist der Satz eine Behauptung wie ein
Ist-Stand: er nennt die Grenze, an der der Code aufhört, und wird gegen die Service-Methode **und** ihre Aufrufer gelesen,
die geteilten Zustand mitbringen (Batch, Schleife, `Local`) – nicht gegen die Story. Zweimal in Lauf L gemessen (B-340).

Jedes DTO trägt ein `/// <summary>` – **auf Englisch** (siehe [docs/translate.md](../../docs/translate.md)).
