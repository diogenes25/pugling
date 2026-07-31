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

Jedes DTO trägt ein `/// <summary>` – **auf Englisch** (siehe [docs/translate.md](../../docs/translate.md)).
