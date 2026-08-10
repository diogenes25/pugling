---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/katalog, rolle/creator]
aliases: [SubjectName driftet, Fachname gegen Fach-Id, Bringschuld des Aufrufers]
status: abgenommen
prio: P3
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-137
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-142 · Ein Fachwechsel hinterlässt den alten Fachnamen

Abgespalten von [B-137](B-137-freitext-fach-unerreichbar.md) (dessen Punkt 4). Das ist der Teil, dessen
Lösung **der Code bestimmt** und nicht der Geschmack: eine Zeile behauptet per Id das eine Fach und per
Name ein anderes.

## User Story

Als **Creator** möchte ich, dass ein Lehrwerk nach einem Fachwechsel nicht weiter den alten Fachnamen
behauptet — sonst zeigen Liste und Formular verschiedene Fächer für dieselbe Reihe.

## Ist-Stand am Code

Drei Ressourcen tragen das Paar `SubjectId` + `SubjectName`, und **alle drei** setzen die beiden Felder
unabhängig voneinander (nachgezählt am 2026-08-10):

| Controller | `Create` | `Update` |
| --- | --- | --- |
| `Controllers/Creator/TextbookSeriesController.cs` | `:171-172` | `:228-229` |
| `Controllers/Creator/CreatorProfilesController.cs` | `:99-100` | `:142,145` |
| `Controllers/Supervisor/TextbooksController.cs` | `:69-70` | `:109-110` |

Das Muster ist überall dasselbe:

```csharp
if (dto.SubjectName is not null) series.SubjectName = Trimmed(dto.SubjectName);
if (dto.SubjectId.HasValue)      series.SubjectId  = dto.SubjectId;
```

Ein `PATCH {"subjectId": 2}` setzt also die Id auf Französisch und lässt `SubjectName = "Englisch"`
stehen. Nur `ClearSubject` behandelt das Paar als Paar (`:234`, `:146`, `:111`) — und ist damit die
einzige Stelle, die die Invariante kennt.

**Kompensiert wird das heute allein im Frontend:** `frontend/src/lib/seriesPatch.ts` schickt Id und Namen
als Paar. `Pugling.Client.UpdateSeriesAsync`, der KI-Creator-Agent und die `.http`-Flows unter `docs/REST/`
haben kein Gegenstück. Aus B-123 steht seither ein Satz im Vertrag, der die **Bringschuld des Aufrufers**
benennt — also eine Regel, die der Server nicht durchsetzt.

`ValidateReferencesAsync` (`TextbookSeriesController.cs:113-120`) fragt `db.Subjects` ohnehin ab, um die
Existenz zu prüfen. Der Name ist dort einen `Select(s => s.Name)` statt eines `AnyAsync` entfernt.

## Die echte Lücke

Nicht „ein vergessenes Feld", sondern eine **Invariante ohne Wächter**: `SubjectName` ist die
Rückfallebene für *unkatalogisierte* Werke. Sobald eine `SubjectId` steht, ist der Katalog die Wahrheit
und der Freitext eine zweite, konkurrierende Aussage über dieselbe Sache. Der Vertrag löst das, indem er
die Pflicht an den Aufrufer weiterreicht — und genau das ist die Lücke: eine Zusicherung, die von der
Disziplin jedes einzelnen Clients abhängt, ist in diesem Repo sonst ein Fall für ein Tor.

## Offene Punkte

1. ~~**Ableiten immer, oder nur wenn kein Name mitkommt?**~~ → Entscheidung 1.
2. ~~**Gilt es für alle drei Ressourcen gleich?**~~ → Entscheidung 2.
3. ~~**Wächter?**~~ → Entscheidung 3 (nein, und mit Grund).

## Entscheidungen

1. **Abgeleitet wird immer, sobald eine `SubjectId` gesetzt ist** — nicht nur, wenn kein Name mitkommt.
   Begründung: nur so ist der Widerspruch **unmöglich** statt bloß unwahrscheinlich. „Nur wenn der Name
   fehlt" ließe `{subjectId: 2, subjectName: "Englisch"}` weiter durch, und ein Aufrufer, der beide Felder
   falsch befüllt, ist der wahrscheinlichere Fall als einer, der eines weglässt — die drei Nicht-Frontend-
   Konsumenten schicken den Namen heute überhaupt nicht mit. Kosten: ein ausdrücklich mitgeschickter,
   abweichender Anzeigename geht verloren. Nach dem Modell ist das kein Verlust (`SubjectName` hat bei
   gesetzter Id keine eigene Bedeutung), aber es ist eine **Verhaltensänderung** und steht darum im Vertrag.
2. **Dieselbe Regel für alle drei Ressourcen, an derselben Stelle formuliert.** Begründung: die Form ist
   in allen dreien identisch; drei verschiedene Regeln für dasselbe Paar wären schlimmer als die eine
   heute fehlende. Umgesetzt als `Services/Shared/SubjectNaming` (Muster `SearchPattern`), nicht als
   dreimal kopierter Ausdruck. Kosten: ein zusätzlicher indizierter Lookup je schreibendem Aufruf, der
   ein Fach setzt — bewusst **nicht** in die drei bestehenden Referenzprüfungen gefaltet, weil die drei
   verschieden geschnitten sind und ein durchgereichter Rückgabewert die Klarheit kostete, für die der
   Helfer da ist.
3. **Angewandt wird gegen den Ergebniszustand, nicht gegen den Payload** — und damit **kein** Wächter
   nötig. Begründung: `if (entity.SubjectId is not null)` nach allen Zuweisungen deckt auch den Fall ab,
   den eine Payload-Prüfung übersähe (ein `PATCH`, der nur einen Freitext-Namen auf eine Zeile schickt,
   die bereits eine Id trägt). Ein reflexives Tor über „Entity mit `…Id` + `…Name`" träfe heute genau
   diese drei Stellen und wäre damit ein Tor über eine Regel, die an einer Stelle steht — Kosten ohne
   Gegenwert. Kosten dieser Entscheidung: eine vierte Ressource mit demselben Paar käme ungewarnt hinzu;
   der Verweis in den drei Kommentaren („drei Stellen, eine Bedeutung") ist der bewusst schwächere Ersatz.
   **Und die Invariante ist damit nicht ‚unmöglich zu verletzen', sondern nur über die API gehalten:**
   `Data/Seed.cs` schreibt das Paar von Hand (vom Reviewer nachgezählt, heute konsistent) — der Weg
   daran vorbei existiert also. Erfasst als [B-145](B-145-fach-umbenennen-laesst-namen-stehen.md).

## Akzeptanzkriterien

1. Ein `PATCH`, der nur `subjectId` ändert, hinterlässt keinen widersprüchlichen Fachnamen — an allen
   drei Ressourcen.
2. Dasselbe beim `POST`.
3. `ClearSubject` räumt weiterhin beide Felder gemeinsam.
4. Eine Zeile **ohne** `SubjectId` behält ihren Freitext-Namen unverändert — die Rückfallebene bleibt.
5. Je Ressource ein Integrationstest, vorher rot (mit genannter Zahl).
6. Der Vertragssatz aus B-123 („der Aufrufer schickt beide Felder") wird ersetzt durch die Zusicherung,
   die der Server jetzt selbst hält.

## Schätzung

**Größe: M** — sechs Stellen (drei Ressourcen × `Create`/`Update`), ein geteilter Helfer, vier Tests und
ein Vertragstext. Über B-136 (`S`), unter einer DB-Umbau-Etappe.

- **`wo: beides`** — beim Schätzen stand hier `backend`, mit der Begründung, das Frontend schicke ohnehin
  beide Felder und brauche keine Änderung. Das war zu kurz gedacht: `seriesPatch.ts` kompensierte eine
  Regel, die es danach nicht mehr gibt, und begründete sich mit einer Server-Aussage, die falsch wurde.
  Eine tote Kompensation stehenzulassen heißt, eine Regel zu behaupten, die es nicht mehr gibt — der
  Reviewer hat es gefunden, korrigiert ist es hier.
- **`migration: nein`** — kein Schema ändert sich, nur wer welchen Wert schreibt.
- **`vertragsbruch: nein`** — kein DTO-Feld ändert sich. Der Vertrags*text* ändert sich allerdings in
  seiner Aussage (aus einer Bringschuld des Aufrufers wird eine Zusicherung des Servers), und ein
  mitgeschickter abweichender Name wird jetzt ignoriert statt gespeichert. Additiv im Sinne des Schemas,
  aber sichtbar im Verhalten — darum im Vertrag benannt.
- **Risiken:** 1. **Die Reihenfolge im `Update`.** Die Ableitung muss *nach* `ClearSubject` laufen, sonst
  füllt sie einen gerade geleerten Namen wieder. 2. **`ResolveNameAsync` liefert `null` auch für eine
  nicht existierende Id** — im `Create` fällt der Ausdruck dann auf den Payload-Namen zurück. Tragfähig
  nur, solange vorher eine Existenzprüfung läuft; die ist je Controller einzeln nachzusehen.
  3. Die Gegenprobe (Zeile ohne Id behält ihren Freitext) muss mitgetestet werden, sonst löscht der Fix
  die Rückfallebene, um die es geht.
- **Angriffsplan:** 1. `Services/Shared/SubjectNaming`. 2. Je Controller `Create` und `Update`.
  3. Vier Integrationstests (drei Ressourcen + Gegenprobe), rot vor dem Fix mit genannter Zahl.
  4. Vertragstext in `TextbookSeriesDtos` ersetzen.
- **Testweg:** `backend/Pugling.Api.Tests/FachnameFolgtDerFachIdTests.cs`, eine Klasse für alle drei
  Ressourcen — die Regel ist eine, und drei Klassen ließen sie wie drei Regeln aussehen.

## Verlauf

- **2026-08-10** — abgespalten von [B-137](B-137-freitext-fach-unerreichbar.md) im Nachtlauf (Sprint 2).
  B-137 war faktisch **XL** (sechs Akzeptanzkriterien, drei Controller, Backend *und* Frontend, dazu eine
  Flags-Enum-Mehrfachauswahl); Freigabe 3 verlangt dafür Teilen statt Bauen. Dieser Teil ist
  herausgeschnitten, weil er als **einziger** ohne Produktentscheidung auskommt: „die Zeile darf sich
  nicht selbst widersprechen" ist keine Geschmacksfrage. Ist-Stand beim Teilen an allen drei Controllern
  nachgezählt, nicht aus B-137 abgeschrieben.
- **2026-08-10** — gebaut und **abgenommen** (Nachtlauf, Sprint 2). **Rote Probe: 3 von 4 rot**; der vierte
  (`Ohne_Fach_Id_Bleibt_Der_Freitext_Stehen`) war **absichtlich grün** und ist als Gegenprobe beschriftet —
  ohne ihn könnte der Fix die Rückfallebene mitlöschen, um die es geht.
  `pugling-reviewer`: **kein Blocker**, sieben Funde, alle behoben. Die drei, die etwas gekostet hätten:
  (a) der Vertrag nannte die neue Zusicherung nur an **einer** von drei Ressourcen — ein Agent hätte am
  OpenAPI-Dokument gelesen, beide Felder geschickt und wortlos einen ignorierten Namen zurückbekommen;
  (b) `docs/REST/Creator.http` führte die abgeschaffte Bringschuld weiter vor und zeigte ab jetzt einen
  **Widerspruch** zwischen Request und Antwort; (c) `seriesPatch.ts` begründete seine Kompensation mit
  einer Server-Aussage, die nicht mehr stimmte — die einzige Stelle, die die Regel kompensierte, war
  danach die einzige, die sie noch behauptete. Dazu zwei fehlende Testfälle für genau die zwei
  Kombinationen, die die Code-Kommentare als ihren Daseinsgrund nennen.
  **Beim Beheben von (a) habe ich denselben Fehler wiederholt, den der Reviewer gerade gemeldet hatte:**
  ein `<para>` außerhalb der `<summary>`, dreimal — der Compiler wirft das aus dem Dokument. Korrigiert,
  und die zwei Bestands-Vorkommen derselben Art (`ProfileDtos.cs` doppelte `<summary>`,
  `CreatorProfileDtos.cs` `<para>` hinter `</summary>`) gleich mit; an beiden fehlte die
  `Clear…`-Erklärung im ausgelieferten `docs/openapi/v1.json`.
  `wo` von `backend` auf **`beides`** korrigiert: der Fix machte eine Frontend-Kompensation überflüssig,
  und die stehenzulassen hieße, eine Regel zu behaupten, die es nicht mehr gibt.
  **Rollengang** — mit der Lehre aus der Retro von Sprint 1: der Server wurde **nach** der letzten Änderung
  gestartet. Live gegen die laufende API: anlegen mit nur `subjectId` ⇒ Name abgeleitet; `PATCH` mit nur
  der neuen Id ⇒ Name folgt; Gegenprobe ohne Id ⇒ Freitext unangetastet.
  Suite **805/805** Backend, **189/189** Frontend, `tsc -b` und `dotnet build Pugling.sln -c Release` sauber.
  **Fund nebenan → eigene Story:** [B-145](B-145-fach-umbenennen-laesst-namen-stehen.md) (das Umbenennen
  eines Fachs zieht die drei Kopien nicht nach — durch diese Story allerdings **selbstheilend** beim
  nächsten Schreibzugriff, also entschärft statt verschärft).
