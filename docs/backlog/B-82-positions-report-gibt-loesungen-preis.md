---
tags: [typ/story, status/in-arbeit, bereich/backend, rolle/student]
aliases: [Positions-Report gibt die Lösungen preis, ItemReport trägt Answer,
  Kind liest die Lösung jeder Karte im Report, Tür C]
status: in-arbeit
prio: P1
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: ja
quelle: B-80 (pugling-reviewer, Befund außerhalb des Diffs)
---

# B-82 · Über den Positions-Report kann ein Kind die Lösung jeder Karte lesen

## User Story

Als **Vater** möchte ich, dass die Lösung einer Karte für mein Kind erst dann lesbar ist, wenn die Stufe sie
zeigen darf, damit der Lernbericht meine Auswertung bleibt und nicht sein Spickzettel wird.

## Ist-Stand am Code

Die **dritte Tür** in dieselbe Kammer wie [B-80](B-80-tags-geben-fremde-konfiguration-preis.md) — und von
dessen Reparatur **nicht** gedeckt, weil sie kein `ExerciseBrief` benutzt.

### 1 · Das DTO trägt die Lösung als eigenes Feld

`ItemReport` führt sie namentlich
([LearnProgressDtos.cs:30](../../backend/Pugling.Contracts/Student/LearnProgressDtos.cs)):

```csharp
public record ItemReport(int ItemIndex, string Prompt, string Answer, bool Introduced, …);
```

Gefüllt wird das Feld aus `ContentItem.Answer`
([PositionReportService.cs:53](../../backend/Pugling.Api/Services/Student/PositionReportService.cs)), und das
ist bei **jedem** Übungstyp die Lösung (Lückentext-Gap, Hörverstehen-Frage, Grammatik, Übersetzung — die
Projektionen in `BuiltInExerciseTypes.cs`).

### 2 · Der Endpunkt ist für das Kind offen — ohne Trick

Route `api/v1/student/study-plans/{planId}/positions/{positionId}/report`
([PositionReportController.cs:14](../../backend/Pugling.Api/Controllers/Student/PositionReportController.cs)),
klassenweit nur `[Authorize]` plus `[ServiceFilter(typeof(PlanOwnershipFilter))]` — und der Filter lässt einen
Student für seinen **eigenen** Plan durch. Das braucht so wenig einen Trick wie Tür B in B-80.

### 3 · Am laufenden System nachgespielt

2026-08-03, Wegwerf-DB auf `:5280` (die echte `pugling.db` unangetastet). Vater legt einen Lückentext an und
eine Position darauf; das Kind hat nie eine Karte gesehen:

```text
GET student/study-plans/{plan}/positions/{pos}/report   (Kind-Token)   → 200
  item 0: introduced=False  prompt='I ___ to school.'  answer='walked'
```

`introduced: false` ist der Punkt: die Lösung kommt **auch** für Karten, die dem Kind noch nie gezeigt
wurden. Das ist genau die Zusicherung, die `CardFacets` auf getippten Stufen hält (kein `reveal`) und an der
[B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) und [B-77](B-77-liste-menge-als-folge.md) gefeilt haben.

### 4 · Der Code weiß, dass er den Vater meint

Der Service-Kommentar lautet „Answers the **supervisor's** question", der Controller „shows the **father**
for each content item" — und trotzdem liegt die Route unter `student/…` und ist für das Kind offen. Die
Absicht steht also im Code; nur die Wand fehlt.

### 5 · Die Testlage

`OwnershipTests.cs:95` prüft für diesen Endpunkt `403` bei einem **fremden** Kind — also genau an der Lücke
vorbei, dieselbe Klasse „Regel getestet, Grenzfall offen" wie bei B-77 und B-80
([docs/testplan.md](../testplan.md)).

### 6 · Die Verbraucher, nachgezählt (Grill-Runde 2026-08-03)

Wie B-80s Ist-Stand 5, weil davon der Schnitt abhängt: **genau ein Verbraucher von `answer`, und der ist der
Vater.**

| Wo | Befund |
|---|---|
| Frontend Vater | `frontend/src/vater/PlanPositions.tsx:406` (`<td>{it.answer}</td>`, Spalte „Lösung") — der einzige Aufruf von `api.positionReport` (`lib/api.ts:467`) |
| Frontend Sohn | **kein** Aufruf; `SohnProgress.tsx:12-13` liest `overview`/`overviewProgress`, das Kind hat seinen eigenen Lernstand-Pfad |
| `Pugling.Client` | **keine** Methode für den Report (einziger `report`-Treffer ist ein Kommentar in `CreatorApi.cs:201`) |
| Tests | `PositionReportTests.cs:24-70` fährt den Endpunkt durchweg mit dem **Vater**-Client; `OwnershipTests.cs:95` mit einem fremden Kind |

Damit ist auch der offene Punkt 5 beantwortet: `ChildLearnProgressController.Items` (`:81-92`) gibt
`ItemProgressResponse` heraus ([ProgressDtos.cs:17](../../backend/Pugling.Contracts/Student/ProgressDtos.cs),
mit `Front`/`Back`), gespeist aus `db.ItemProgress`
([ChildLearnProgressService.cs:284](../../backend/Pugling.Api/Services/Student/ChildLearnProgressService.cs))
— dort gibt es nur Zeilen für Items, die das Kind **schon beantwortet** hat. Also **nicht** dieselbe Bauart,
sondern die Familie von `ChildVocabularyProgress` und damit nahe
[B-81](B-81-vokabel-tags-geben-uebersetzungen-preis.md).

## Die echte Lücke

Derselbe Defekttyp wie B-80s Tür B, aber **nicht** dieselbe Stelle: die Ownership-Prüfung ist richtig
(es ist der Plan des Kindes), die **Rollenreichweite eines Lese-DTOs** ist falsch. B-80/E1 hat die
Zusicherung für `ExerciseBrief` zu einer Eigenschaft des *Typs* gemacht — dieser Typ war davon nicht
betroffen, weil er die Lösung nicht als rohe Config trägt, sondern als benanntes Feld. Solange ein DTO unter
`student/…` ein `Answer` führt, ist jede künftige Auswertungssicht ein neuer Kandidat.

## Offene Punkte

Alle fünf sind in der Runde vom 2026-08-03 erledigt — durchgestrichen statt gelöscht, damit die Frage
nachlesbar bleibt. Zwei Empfehlungen sind dabei **korrigiert** worden (Punkt 3), zwei Punkte **aufgelöst**
statt beantwortet (4 und 5).

1. ~~**Welcher Schnitt: Feld leeren oder Route gaten?**~~ → **E1**, Gaten.
2. ~~**Hat `answer` überhaupt einen Verbraucher?**~~ → nachgezählt, **genau einer, und der ist der Vater**
   (Ist-Stand 6). Damit war Punkt 1 entscheidbar.
3. ~~**Zieht das Ebenen-Präfix mit?**~~ → **E2**, und die eigene Empfehlung („nicht anfassen, wie B-80/E5")
   ist **korrigiert**: nach E1 darf kein Student die Route aufrufen, das Präfix wäre dann nachweislich falsch
   statt nur schief.
4. ~~**Sagen weitere Felder zu viel?**~~ → **aufgelöst durch E1**: es wird kein Feld geschnitten, der ganze
   Endpunkt gehört dem Vater. `prompt`, `box`, `dueOn`, `testsCorrect` bleiben unangetastet.
5. ~~**Hat der Nachbar `ChildLearnProgressController` dieselbe Bauart?**~~ → **nein**, am Code beantwortet
   (Ist-Stand 6): andere Familie, nahe [B-81](B-81-vokabel-tags-geben-uebersetzungen-preis.md). Keine eigene
   Entscheidung nötig.

## Entscheidungen

Aus der Grill-Runde vom 2026-08-03. Vier Entscheidungen; **E2 korrigiert die eigene Empfehlung** der
Ausformulierung, **E3** geht über den ursprünglichen Schnitt hinaus.

### E1 · Die Route wird auf `Roles.Supervisor` gehoben

`[Authorize(Roles = Roles.Supervisor)]` an den `PositionReportController`. `ItemReport.Answer` bleibt
unverändert im Vertrag — nicht nullable, nicht rollenabhängig geleert.

*Begründung.* Der Report **ist** die Auswertung des Vaters; das sagen beide Kommentare bereits („Answers the
supervisor's question", „shows the father"), nur die Wand fehlte. Und es nimmt nachgezählt nichts weg
(Ist-Stand 6): kein Sohn-UI ruft ihn auf, die Client-Bibliothek kennt ihn nicht, kein Test fährt ihn mit
einem Kind-Token. Ein rollenabhängig geleertes Feld hätte dagegen genau das getan, was B-80/E1 abgeschafft
hat — die Zusicherung in den Endpunkt zurückverlagern und durch Lesen des Vertrags unprüfbar machen
(„`string Answer`, Inhalt abhängig vom Token"). Die Variante „`answer` nur für beantwortete Items" (die
`ChildVocabularyProgress`-Familie) ist verworfen: eine Regel mehr in einem Endpunkt, den kein Kind aufruft.

*Kosten.* Der Vater behält seine Spalte unverändert, aber **drei kind-adressierte Doku-Stellen werden falsch**
und ziehen mit: `docs/tutorial-student.md:308`, `.claude/skills/student/SKILL.md:65`,
`docs/REST/Student.http:128`. Die Zusicherung hängt danach am **Endpunkt**, nicht am Typ — genau darum kommt
E3 dazu.

### E2 · Route und DTOs wandern in die Supervisor-Ebene

Neue Route `api/v1/supervisor/study-plans/{planId}/positions/{positionId}/report`; `Report` und `ItemReport`
verlassen `Contracts/Student/LearnProgressDtos.cs` und ziehen in eine eigene `Contracts/Supervisor`-Datei.
Der Controller wandert nach `Controllers/Supervisor/`.

*Begründung.* Das **korrigiert die Empfehlung der Ausformulierung** („nicht anfassen, wie B-80/E5"). B-80/E5
ließ `supervisor/class-tests` beim Kind, weil die Klausur eine **geteilte** Ressource ist — der Vater plant
sie, das Kind übt darauf. Hier ist es das Gegenteil: nach E1 darf **kein** Student die Route mehr aufrufen,
also widerspricht das `student/`-Präfix dem Leser nicht mehr weich, sondern hart — und das Präfix ist laut
Root-`CLAUDE.md` die Taxonomie, mit der sich die Auth-Wand deckt, wo gegated wird. Der Zielort existiert
bereits (`supervisor/study-plans/{planId}/…`, dort liegt der `PlanOwnershipFilter` ohnehin). Dass die beiden
Records heute neben den echt kind-lesbaren `SubjectProgressResponse` & Co. stehen, ist derselbe Fehler eine
Ebene tiefer.

*Kosten.* Vertragsbruch an der Route, ausgezählt statt geschätzt: 1 Routen-Zeile, 1 Frontend-Zeile
(`frontend/src/lib/api.ts:468`), 3 Test-Zeilen (`PositionReportTests.cs:24,70`, `OwnershipTests.cs:95`), 8
Doku-Stellen (`wiki/07-api-referenz.md:89`, `docs/endpunkt-beziehungen.md:59,348,393,685`,
`docs/vokabel-funktionalitaeten-entwickler-tutorial.md:149`, `docs/tutorial-supervisor.md:455,561`,
`.claude/skills/supervisor/SKILL.md:62`) plus Artefakt-Neubau (`docs/openapi/v1.json`, `docs/api-examples/`,
`frontend/src/lib/contract.ts`). Die Namespace-Verschiebung selbst ist frei — alle sechs
`Pugling.Contracts.*` sind per csproj-`<Using>` projektweit sichtbar. Die drei Doku-Stellen aus E1 fallen
ohnehin an, ein Teil der Kosten ist also schon bezahlt.

### E3 · Ein reflexives Tor erzwingt die Rollenreichweite eines Lese-DTOs

> **Beim Bauen am 2026-08-03 auf einer widerlegten Messung ertappt und daraufhin neu geschnitten** — die
> Fassung unten ist der ursprüngliche Beschluss, **E3′** direkt danach ist die gebaute. Der Beschluss bleibt
> stehen, weil seine Begründung („die echte Lücke ist die wachsende Kandidatenmenge") trägt; nur sein
> *Geltungsbereich* war auf einer Zahl gebaut, die nicht stimmte.

Neuer Wächter in `ConventionGuardTests`: **jede Action, die ein DTO aus `Pugling.Contracts.Supervisor`
zurückgibt, trägt `[Authorize(Roles = Roles.Supervisor)]`** — an der Klasse oder an der Action. Mit einer
namentlichen Ausnahmeliste, je mit englischer Begründung.

*Begründung.* E1 und E2 reparieren **diesen** Endpunkt; die „echte Lücke" dieser Story ist aber, dass jede
künftige Auswertungssicht ein neuer Kandidat ist. Ohne Tor hängt die Zusicherung wieder an Disziplin — und
B-80 und B-82 sind zusammen der Beweis, dass genau diese Disziplin in diesem Repo **zweimal** gerissen ist,
ohne dass jemand einen Fehler gemacht hat. Das Repo hat für diesen Fall eine gelebte Antwort („mechanische
Tore statt Disziplin"), und der Wächter passt ins bestehende Muster (`Actions_Geben_Nur_Vertragstypen_Zurueck`,
`Actions_Unter_ChildId_Oder_PlanId_Tragen_Den_Ownership_Filter`).

*Kosten.* **Gemessen, bevor entschieden wurde: das Tor ist heute an vier Stellen rot** —
`KlassenarbeitenController.List/Get/Practice/Repeat` (`:59,76,260,276`, klassenweit nur `[Authorize]`), und
alle vier sind durch **B-80/E5 ausdrücklich gewollt**. Es braucht also eine Ausnahmeliste mit vier
begründeten Einträgen, und eine Liste verrottet: wird einer dieser Endpunkte umbenannt, meldet das Tor eine
Ausnahme, die es nicht mehr gibt. Dazu treibt E3 die Größe voraussichtlich von S nach M.

### E3′ · Das Tor folgt dem Geheimnis, nicht dem Ordner (gebaut)

`ConventionGuardTests.Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated`: **gibt eine Action in ihrem
Nutzlast-Graphen ein Feld namens `Answer`/`Solution`/`CorrectAnswer` heraus, muss sie auf eine Rollenmenge
**ohne** `Student` gegated sein** — `Roles.Creator` genügt also ebenso wie `Roles.Supervisor`. Der Graph wird
über Properties, Sammlungen und Verschachtelung abgelaufen (tiefenbegrenzt, zyklensicher), damit ein
Lösungsfeld auch aus einem Unter-DTO gefunden wird — `ItemReport` hängt genau so unter `Report`.

*Begründung.* Am 2026-08-03 gemessen, und die Messung hat E3 umgeworfen: namespace-basiert ist das Tor an
**zehn** Stellen rot, nicht an vier. Sechs davon zählen den **Normalfall** auf — `PlanResponse` und
`ObjectiveResponse` sind *als Typen* dual gelesen (ein Kind muss seinen eigenen Plan und seine eigenen Ziele
sehen), und `StudentPlansController.List` trägt sogar `[Authorize(Roles = Roles.Student)]`, ist also bewusst
kind-only und gibt trotzdem ein `Contracts.Supervisor`-DTO heraus. Damit ist die Prämisse von E3 („ein Record
unter `Contracts.Supervisor` ist für die Auswertung des Erwachsenen geschrieben") für zwei der acht
DTO-Dateien falsch — und **E4s eigenes Argument greift dann gegen E3**: eine Ausnahmeliste, die den Normalfall
aufzählt, beweist nichts mehr. Der Ordner ist ein Näherungswert; das Lösungsfeld *ist* die Sache.
Die Rollenbedingung heißt „ohne Student" statt „mit Supervisor", weil das die wahre Zusicherung ist: ein Autor
**muss** die Lösung der Übung sehen, die er schreibt (`ClozeTextsController` gibt `Gap.Answer` heraus und ist
`Roles.Creator`-gegated — das Tor lässt es durch, statt eine Ausnahme zu brauchen).

*Kosten.* **Vier Ausnahmen, eine Gruppe, ein Grund** (gemessen: 10 Actions im Geltungsbereich, 6 davon
korrekt gegated): `RemarksController.Create/GetOne/List/Update`, wo `RemarkDto.Answer` die *Antwort auf eine
Entwickler-Notiz* ist und nicht die Lösung einer Karte. Das ist die ehrliche Grenze einer namensbasierten
Regel — sie kann zwei Bedeutungen eines Wortes nicht unterscheiden, also stehen sie namentlich da.
`Expected` ist **bewusst nicht** in der Namensliste: das ist der Reveal *nachdem* das Kind geantwortet hat
(`ItemOutcome`, `ReviewOutcome`, `ItemCheck`) und damit der Zweck der Rückmeldung, kein Leck — mit `Expected`
in der Liste wären es 16 Ausnahmen statt 4. **E4 wird dadurch gegenstandslos**: der Geltungsbereich ist
ordner-unabhängig, ein künftiges *Creator*-DTO mit Lösungsfeld fängt das Tor jetzt mit — die bewusste Lücke,
die E4 in Kauf nahm, ist zu.

### E4 · Das Tor greift nur `Contracts.Supervisor`, nicht `Contracts.Creator`

> **Durch E3′ gegenstandslos** (2026-08-03): das gebaute Tor unterscheidet keine Ordner mehr, sondern fragt
> nach dem Lösungsfeld. Die unten in Kauf genommene Lücke („ein künftiges Creator-DTO mit einem Geheimnis
> fängt das Tor nicht") ist damit geschlossen, ohne die zwölf Ausnahmen zu kosten, die sie rechtfertigte.

*Begründung.* Der Creator-Bereich ist genau die Fläche, die **bewusst** kind-lesbar ist: `TagsController`
trägt klassenweit nur `[Authorize]` und gibt an acht Stellen Creator-DTOs an ein Kind heraus (`TagResponse`
sechsmal, `ExerciseBrief` `:206`, `TaggedVocabularyDto` `:284`) — durch B-80/E3 entschieden, weil das Kind
seine Übungen selbst markieren darf. Zwölf Ausnahmen statt vier machen die Ausnahmeliste zur Regel, und eine
Liste, die den Normalfall aufzählt, beweist nichts mehr.

*Kosten.* Ein künftiges **Creator**-DTO mit einem Geheimnis fängt das Tor nicht. Dafür bleibt B-80/E1s Weg
zuständig: ein Geheimnis gehört nicht in ein Listen-DTO. Das ist eine bewusste Lücke im Netz, keine
vergessene.

## Akzeptanzkriterien

- Ein Kind-Token bekommt über den Positions-Report **keine** Lösung — insbesondere nicht für Karten mit
  `introduced: false`. Nach E1 antwortet der Endpunkt ihm mit `403`.
- Der Vater sieht den Report unverändert vollständig; sein UI verliert **keine** Spalte (die Spalte „Lösung"
  in `PlanPositions.tsx:406` bleibt gefüllt).
- Die Route liegt unter `api/v1/supervisor/…`; die alte `student/…`-Route existiert **nicht** mehr (kein
  Weiterleiten, kein Parallelbetrieb — vor der Publikation wird gebrochen, nicht überbrückt).
- Kein Verbraucher bleibt zurück: `frontend/src/lib/api.ts` zeigt auf die neue Route, und die kind-adressierte
  Doku nennt den Report nicht mehr als Student-Aufruf.
- Das Tor aus E3′ ist scharf und **grün**, mit genau vier begründeten Ausnahmen; eine neue Action, die ein
  Lösungsfeld an ein Student-Token herausgibt, macht es rot.
- **Regressionstest, vorher rot**: Kind-Token liest den Report seiner **eigenen** Position → `403`; heute
  liefert derselbe Aufruf `200` mit `answer`.

## Schätzung

**M · beides · keine Migration · Vertragsbruch: ja.**

**Größe M**, an den Ankern gemessen. **Ohne E3 wäre es S** — der Umfang von E1+E2 ist „`childId` aus dem
Test-Pfad ziehen" (B-01): ein Attribut, eine Routen-Konstante, zwei Records in eine neue Datei, drei
Test-Zeilen, eine Frontend-Zeile. **E3 hebt es auf M**, weil ein *neuer Mechanismus* dazukommt, nicht nur
eine geänderte Zeile: ein reflexiver Wächter samt gemessener Ausnahmeliste und Selbstschutz — derselbe
Charakter wie der neue Batch-Pfad im `MediaSelector` (B-03, der M-Anker). Kein L: es wird keine Etappe eines
Umbaus, kein Löschverhalten und kein bezahltes Inventar berührt.

Was E3 **billig** macht und darum gemessen ist: die Maschinerie existiert vollständig.
`ApiSurface.Controllers()/Actions()/RouteOf()` und die Helfer `PayloadType`/`LeafTypes`
([ConventionGuardTests.cs:213,224](../../backend/Pugling.Api.Tests/ConventionGuardTests.cs)) entpacken
`Task<ActionResult<T>>` und Sammlungen schon; das Muster „Ausnahmeliste mit Begründung" steht als
`OwnershipExceptions` (`:202`, heute leer) daneben, samt der Warnung „if this list grows, the reason belongs
with it". Der Wächter ist damit ein Fact von ~40 Zeilen, nicht ein Werkzeug.

**Keine Migration**, nachgesehen: **kein Entity wird angefasst.** E1 setzt ein Attribut, E2 verschiebt
Dateien und Namespaces, E3 fügt einen Test hinzu. `SchemaGuardTests` hat nichts zu melden, die Kette bleibt
bei 1.

**Vertragsbruch: ja**, und zwar an der **Route**, nicht am Schema: der Pfad
`api/v1/student/study-plans/{planId}/positions/{positionId}/report` verschwindet und entsteht unter
`supervisor/…` neu. Die **Schemanamen bleiben** `Report`/`ItemReport` — der OpenAPI-Generator schlüsselt über
den *einfachen* Typnamen (`Vertragstypen_Sind_Global_Namens_Eindeutig`, `ConventionGuardTests.cs:71`), die
Namespace-Verschiebung ist im Dokument also unsichtbar. Folge: `frontend/src/lib/types.ts:428-431`
(`S["ItemReport"]`, `S["Report"]`) bleibt gültig, und `MasteryPill.tsx` ebenfalls. Kein Parallelbetrieb, kein
Weiterleiten — bis zur Publikation wird gebrochen (`CLAUDE.md`, API-Versionierung).

**`wo: beides`**, und das ist die Prüfung, nicht die Vermutung: genau **eine** Frontend-Quelle ändert sich,
`frontend/src/lib/api.ts:468` (die Routen-Zeichenkette). Das ist kein UI-Durchgang — aber `wo: backend` wäre
falsch, und B-80 durfte es nur behaupten, weil dort *keine* Frontend-Quelle anzufassen war. Reviewer:
`pugling-reviewer` vollständig, `frontend-reviewer` als Stichprobe über die eine Zeile.

### Risiken

**R1 · `ApiRoutes.cs` behauptet das Gegenteil von E2 — und nennt den Report als Beispiel.** Der Kommentar
([ApiRoutes.cs:13-16](../../backend/Pugling.Api/Controllers/ApiRoutes.cs)) lautet: „The prefix is resource
taxonomy, not the auth wall … Individual routes (e.g. reports) are dual on purpose - a supervisor then reads
a student-tagged route and vice versa." E2 bleibt tragfähig — der Satz rechtfertigt duale Routen für **dual
gelesene** Ressourcen, und genau das hört der Report mit E1 auf zu sein —, aber der **Kommentar wird durch
E2 falsch** und muss mit: er ist die einzige Stelle im Code, die die Dualität als Absicht festhält, und wer
ihn stehen lässt, hinterlässt zwei widersprechende Quellen der Wahrheit. Beim Bauen also umschreiben, nicht
löschen: die Regel bleibt („Präfix ist Taxonomie"), das Beispiel wird ein anderes. Aufgefallen erst beim
Schätzen, nicht beim Grillen.

**R2 · Die Ownership-Zusicherung wird zu einem stillen Grün.** `OwnershipTests.cs:95` prüft heute, dass ein
**fremdes** Kind `403` bekommt. Nach E1 bekommt **jedes** Kind `403`, weil die Rollenschranke zuerst
antwortet — die Zeile bleibt grün und beweist die Kreuz-Kind-IDOR-Regel nicht mehr. Genau die Fehlerklasse,
die diese Story-Reihe verfolgt („Regel getestet, Grenzfall offen", [docs/testplan.md](../testplan.md)). Der
Fall muss auf einen **fremden Erwachsenen** umgestellt werden (Supervisor ohne Betreuungsauftrag für diesen
Plan → `403` über den `PlanOwnershipFilter`), sonst tauscht die Reparatur eine echte Zusicherung gegen eine
scheinbare.

**R3 · Die Untergrenze des Selbstschutzes darf nicht geraten werden.** Jeder Wächter dieser Klasse trägt ein
`Assert.True(checkedActions >= N)` gegen ein leeres Grün. Wie viele Actions ein `Contracts.Supervisor`-DTO
zurückgeben, ist **nicht** gezählt (acht DTO-Dateien unter `Contracts/Supervisor/`, die Zahl der Actions
darüber ist nicht erhoben) — die Grenze wird beim Bauen **gemessen** und knapp unter den Istwert gesetzt.
Eine zu hoch geratene Grenze ist ein rotes Tor ohne Fehler, eine zu tiefe ein Tor, das nicht beißt; feste
Untergrenzen verrotten ohnehin (Erfahrung des Client-Routen-Wächters, B-40).

**R4 · Die Ausnahmeliste ist eine Liste.** Die vier Klausur-Einträge (`KlassenarbeitenController.List/Get/
Practice/Repeat`) sind über `Controller.Action`-Schlüssel notiert. Wird eine dieser Actions umbenannt, zeigt
das Tor auf eine Ausnahme, die es nicht mehr gibt — und dann ist der Endpunkt ungegated *und* unbemerkt. Der
Wächter braucht darum eine zweite Zusicherung: **jeder Eintrag der Ausnahmeliste muss auf eine existierende
Action zeigen** (dasselbe Muster wie `PatchSemanticsTests`, das jeden Schalter gegen seine Tabelle hält).

**R5 · Das Vertragsdokument ist eingecheckt.** `ContractDocumentTests` schreibt `docs/openapi/v1.json` bei
**jedem** Lauf; der Pfadschlüssel wandert von `student` nach `supervisor`, dazu der Swagger-`Tags`-Name. Der
Diff ist erwartet, muss aber **mitcommittet** werden, sonst ist CI rot. Der Selbstschutz des Tests hängt an
`"/api/v1/supervisor/children"` (`:52`) und ist nicht betroffen.

**R6 · Was *nicht* betroffen ist**, damit niemand danach sucht: `DocsCaptureTests` schneidet diesen Endpunkt
**nicht** mit (kein Treffer in der Datei) — `docs/api-examples/` und `openapi-examples.generated.json` bleiben
also unverändert, anders als bei B-80. `Pugling.Client` kennt den Report nicht (keine Methode), der
`ClientRouteGuard` hat nichts nachzuziehen. **Kein E2E** fährt die Route (`frontend/e2e/` ohne Treffer). Und
der Endpunkt-Abdeckungs-Wächter sieht keinen neuen Endpunkt: `ApiSurface.Key` ist
`PositionReportController.Get`, der Name bleibt.

### Angriffsplan

Backend zuerst; das Frontend hängt an der API und wird eine Zeile.

1. **Vertrag**: `Report` und `ItemReport` aus
   [Contracts/Student/LearnProgressDtos.cs:30-37](../../backend/Pugling.Contracts/Student/LearnProgressDtos.cs)
   in eine neue `Contracts/Supervisor/PositionReportDtos.cs` (Namespace `Pugling.Contracts.Supervisor`). Keine
   `using`-Änderung nötig — alle sechs Vertrags-Namespaces sind per csproj-`<Using>` projektweit sichtbar.
2. **Controller** nach `Controllers/Supervisor/PositionReportController.cs`: Namespace, `ApiRoutes.Supervisor`
   statt `.Student`, `[Authorize(Roles = Roles.Supervisor)]` (E1, `Roles` liegt in
   [AuthAccess.cs:18](../../backend/Pugling.Api/Auth/AuthAccess.cs)), `[Tags("Supervisor – Position Report")]`.
   Der `[ServiceFilter(typeof(PlanOwnershipFilter))]` **bleibt** — er ist für `{planId}`-Routen Pflicht
   (Wächter (b)) und trägt jetzt die Erwachsenen-Prüfung allein.
3. **Service** nach `Services/Supervisor/PositionReportService.cs` (Namespace-Wechsel; die
   DI-Registrierung in [Program.cs:170](../../backend/Pugling.Api/Program.cs) bleibt unverändert). Den
   Kommentar „Answers the supervisor's question" behalten — er stimmt jetzt auch für die Route.
4. **`ApiRoutes.cs`-Kommentar** umschreiben (R1): die Regel bleibt, das Beispiel „reports" fällt weg.
5. **Tor** (E3/E4) in `ConventionGuardTests`: neuer Fact über `Controllers()`/`Actions()` +
   `PayloadType`/`LeafTypes`, Bedingung „Leaf-Namespace `Pugling.Contracts.Supervisor` ⇒ `AuthorizeAttribute`
   mit `Roles` enthält `Supervisor`, an Action **oder** Klasse". Dazu die vier begründeten Ausnahmen, der
   gemessene Selbstschutz (R3) und die Prüfung, dass jede Ausnahme existiert (R4).
6. **Tests** (siehe Testweg), inklusive der Umstellung aus R2.
7. **Doku**, 11 Stellen: `wiki/07-api-referenz.md:89`, `docs/endpunkt-beziehungen.md:59,348,393,685` (`:393`
   verlinkt den Service zusätzlich noch unter `Services/PositionReportService.cs` statt `Services/Student/` —
   beim Verschieben gleich richtig setzen), `docs/vokabel-funktionalitaeten-entwickler-tutorial.md:149`,
   `docs/tutorial-supervisor.md:455,561`, `.claude/skills/supervisor/SKILL.md:62`. Und die drei
   kind-adressierten, die den Report **nicht mehr nennen** dürfen: `docs/tutorial-student.md:308`,
   `.claude/skills/student/SKILL.md:65`, `docs/REST/Student.http:128` — der Aufruf wandert aus
   `Student.http` nach `docs/REST/Supervisor.http` (existiert).
8. **Frontend**: `frontend/src/lib/api.ts:468` auf `supervisor/…` umstellen, `npm run gen:contract`, dann
   `npm run build` als Gegenprobe (es darf sich **keine** weitere Quelle ändern müssen).
9. **Artefakte**: `docs/openapi/v1.json` schreibt der Testlauf, mitcommitten (R5).

### Testweg

- **Regressionstest, vorher rot** — in `AntiCheatTests` (dort liegen die serverseitigen Zusicherungen, wie
  bei B-80): Kind-Client (`TestApi.ChildAsync(factory)`) liest den Report **seiner eigenen** Position →
  `403`. Heute liefert derselbe Aufruf `200` mit `answer` für eine Karte mit `introduced: false` — der Fall
  aus Ist-Stand 3.
- **`PositionReportTests.cs:24,70`**: Routen-Zeichenketten nachziehen; die vorhandenen Zusicherungen des
  Vaters (`totalItems`, `introducedItems`, Box-Fortschritt, `404` bei fremder Position) müssen **unverändert**
  grün bleiben — das ist der Nachweis „der Vater verliert nichts".
- **`OwnershipTests.cs:95` umstellen** (R2): statt des fremden Kindes ein **fremder Erwachsener** → `403`.
  Ohne diese Umstellung bleibt die Zeile grün, ohne noch etwas zu beweisen.
- **Das neue Tor gegen sich selbst prüfen**: `[Authorize(Roles = …)]` am Report-Controller probehalber
  entfernen — der Wächter muss rot werden. Und ein erfundener fünfter Ausnahme-Eintrag muss die Prüfung aus
  R4 rot machen. Ein Tor, das nie rot gesehen wurde, ist unbelegt.
- **`ContractDocumentTests`** läuft mit und schreibt das Dokument; der erwartete Diff ist der Pfadschlüssel
  plus `Tags`-Name (R5).
- **`/smoke-test`** plus der Live-Durchgang aus Ist-Stand 3 gegen eine Wegwerf-DB: Kind-Token auf den Report
  → jetzt `403`, Vater-Token auf die **neue** Route → `200` mit gefüllter Lösung.
- **E2E**: nicht nötig, es ändert sich keine Oberfläche (`frontend/e2e/` fährt die Route nicht);
  `full-flow.spec.ts` muss grün bleiben.

## Verlauf

- **2026-08-03** — angelegt aus dem `pugling-reviewer`-Befund zur Abnahme von B-80, und gleich
  **ausformuliert**: der Ist-Stand ist am Code belegt *und* am laufenden System nachgespielt (ein Kind-Token
  liest `answer` für eine Karte mit `introduced: false`), also wäre `idee` mit seinem `unverifiziert: true`
  die falsche Stufe gewesen — der Index-Wächter hat das prompt gemeldet. Nicht dem Reviewer geglaubt, sondern
  selbst nachgespielt; er hatte den Befund nur aus dem Code gelesen.
  `prio: P1` in Analogie zu B-80 vorgeschlagen (dieselbe Anti-Cheat-Zusicherung, ohne Zutun des Vaters
  ausnutzbar) — nicht vom Nutzer bestätigt.
  Bewusst **nicht** in B-80 eingefaltet: dessen Akzeptanzkriterien sind auf die *Konfiguration* geschnitten
  und wörtlich erfüllt; hier trägt ein **anderes** DTO die Lösung als eigenes Feld, an einem anderen
  Endpunkt, mit einer eigenen Entscheidung (Feld leeren vs. Route gaten). Handhabung wie B-76 → B-79 und
  B-80 → B-81.
- **2026-08-03** — gegrillt, vier Entscheidungen. Tragend ist **E1**: die Route wird auf `Roles.Supervisor`
  gehoben, statt `answer` rollenabhängig zu leeren — der Report **ist** die Auswertung des Vaters, und das
  Leeren hätte die Zusicherung genau dorthin zurückverlagert, wo B-80/E1 sie weggeholt hat (im Vertrag nicht
  mehr lesbar). Entscheidbar wurde das erst durch das Nachzählen (Ist-Stand 6): **genau ein Verbraucher, und
  der ist der Vater** — kein Sohn-UI, keine Client-Methode, kein Kind-Test.
  Die Runde hat eine eigene Empfehlung **korrigiert**. **E2**: Route *und* die beiden DTOs wandern in die
  Supervisor-Ebene, entgegen dem „nicht anfassen" der Ausformulierung. B-80/E5 ließ die Klausur beim Kind,
  weil sie eine *geteilte* Ressource ist; hier darf nach E1 **kein** Student die Route mehr aufrufen, also
  widerspricht das `student/`-Präfix dem Leser hart statt weich. Die Kosten sind ausgezählt statt geschätzt
  (15 Stellen plus Artefakte), und drei davon — die kind-adressierte Doku — fallen durch E1 ohnehin an.
  **E3 geht über den ursprünglichen Schnitt hinaus**: ein reflexives Tor in `ConventionGuardTests` („Action mit
  `Contracts.Supervisor`-DTO ⇒ `Roles.Supervisor`"), weil E1/E2 allein nur *diesen* Endpunkt reparieren,
  während die „echte Lücke" die wachsende Kandidatenmenge ist. Vor der Entscheidung gemessen: das Tor ist
  heute an **vier** Stellen rot (`KlassenarbeitenController.List/Get/Practice/Repeat`), und alle vier sind
  durch B-80/E5 gewollt — es braucht also eine Ausnahmeliste mit vier Einträgen und treibt die Größe
  voraussichtlich von S nach M. **E4** hält das Tor bei `Contracts.Supervisor`: auf `Contracts.Creator`
  ausgedehnt wären es mindestens zwölf Ausnahmen (acht davon Tag-Actions, durch B-80/E3 gewollt), und eine
  Ausnahmeliste, die den Normalfall aufzählt, beweist nichts mehr.
  Zwei offene Punkte wurden **aufgelöst statt beantwortet**: Punkt 4 („sagen weitere Felder zu viel?") hat
  nach E1 keinen Gegenstand, weil kein Feld geschnitten wird; Punkt 5 ist am Code beantwortet —
  `ChildLearnProgress` liest aus `db.ItemProgress`, also nur **beantwortete** Items, und ist damit die Familie
  von B-81, nicht diese. Abgeleitet und unwidersprochen geblieben: die drei kind-adressierten Doku-Stellen
  ziehen mit, und der Titel bleibt (er benennt den Produktfehler aus Rollensicht, nicht das DTO).
- **2026-08-03** — geschätzt: **M · beides · keine Migration · Vertragsbruch ja**. **M statt S allein wegen
  E3**: E1+E2 sind mechanisch (ein Attribut, eine Routen-Konstante, zwei verschobene Records, drei Test-Zeilen,
  eine Frontend-Zeile), aber das Tor bringt einen *neuen Mechanismus* mit Ausnahmeliste und Selbstschutz.
  Billig ist es, weil die Maschinerie nachgesehen ist statt vermutet: `ApiSurface` plus die Helfer
  `PayloadType`/`LeafTypes` entpacken `Task<ActionResult<T>>` schon, und `OwnershipExceptions` ist das fertige
  Muster für eine begründete Ausnahmeliste. **Keine Migration**, weil kein Entity angefasst wird. Der
  **Vertragsbruch sitzt an der Route, nicht am Schema**: `Report`/`ItemReport` behalten ihre Namen, weil der
  Generator über den *einfachen* Typnamen schlüsselt — `types.ts:428-431` und `MasteryPill.tsx` bleiben also
  gültig. `wo: beides` ist geprüft: genau **eine** Frontend-Quelle ändert sich (`api.ts:468`), womit das
  `backend` aus B-80 hier nicht wiederholbar ist.
  Die Schätzung hat drei Dinge freigelegt, die im Grillen nicht sichtbar waren. **R1**: `ApiRoutes.cs:13-16`
  hält die Dualität als **Absicht** fest und nennt „reports" als Beispiel — E2 bleibt tragfähig (der Satz gilt
  für *dual gelesene* Ressourcen, und das hört der Report mit E1 auf zu sein), aber der Kommentar wird falsch
  und muss umgeschrieben werden, sonst stehen zwei Quellen der Wahrheit gegeneinander. **R2**: `OwnershipTests.cs:95`
  wird durch E1 zu einem **stillen Grün** — nach dem Gaten bekommt *jedes* Kind `403`, die Kreuz-Kind-IDOR-Regel
  ist damit nicht mehr bewiesen; der Fall muss auf einen fremden **Erwachsenen** umgestellt werden. Genau die
  Fehlerklasse, die diese Story-Reihe verfolgt. **R4**: die Ausnahmeliste des Tors zeigt über
  `Controller.Action`-Schlüssel auf vier Actions — wird eine umbenannt, ist der Endpunkt ungegated *und*
  unbemerkt; der Wächter braucht deshalb eine zweite Zusicherung, dass jede Ausnahme existiert (Muster
  `PatchSemanticsTests`).
  Gegenprobe zum Umfang, damit niemand danach sucht: `DocsCaptureTests` schneidet den Endpunkt **nicht** mit
  (also keine Änderung unter `docs/api-examples/`, anders als bei B-80), `Pugling.Client` kennt ihn nicht, kein
  E2E fährt ihn, und der Abdeckungs-Wächter sieht keinen neuen Endpunkt (`PositionReportController.Get` behält
  seinen Namen).
- **2026-08-03** — in Arbeit: **E1 und E2 gebaut wie beschlossen, E3 neu geschnitten (E3′), E4 dadurch
  gegenstandslos.** 668 Tests grün, `markdownlint` sauber, Frontend baut.
  Der Angriffsplan lief wie geschrieben durch, ohne Überraschung in den ersten vier Schritten: `Report`/
  `ItemReport` liegen in `Contracts/Supervisor/PositionReportDtos.cs`, Controller und Service in den
  `Supervisor`-Ordnern, `[Authorize(Roles = Roles.Supervisor)]` sitzt, der `PlanOwnershipFilter` bleibt und
  trägt die Erwachsenen-Prüfung jetzt allein. Der `ApiRoutes.cs`-Kommentar aus **R1** ist umgeschrieben: die
  Regel bleibt, das Beispiel ist jetzt die Klassenarbeit (eine *wirklich* dual gelesene Ressource), und ein
  Satz sagt ausdrücklich, dass Dualität keine Entschuldigung für ein rollen-gegatetes Fremdpräfix ist.
  **R2 ist umgesetzt** und war die richtige Sorge: die Report-Zusicherung ist aus
  `Sohn_KannPlanEinesAnderenKindes_NichtBenutzen_403` heraus- und in `FremderVater_KannPlanNichtSehen_403`
  hineingewandert (fremder Supervisor ohne Betreuungsauftrag → `403` über den Filter). Am alten Ort steht
  jetzt ein Kommentar, warum die Zeile dort nichts mehr bewiesen hätte.
  **Drei Tore gegen sich selbst geprüft, jedes einzeln rot gesehen** — ohne das wäre keins belegt: `Roles`
  am Controller entfernt → Regressionstest **und** neues Tor rot; ein erfundener fünfter Ausnahme-Eintrag
  (`ErfundenerController.Gibtsnicht`) → die R4-Prüfung rot. **R3 ist gemessen statt geraten**: 64 Actions
  geben ein `Contracts.Supervisor`-DTO zurück, die Untergrenze steht bei 55.
  Am laufenden System nachgespielt (Wegwerf-DB auf `:5280`, echte `pugling.db` unangetastet), also derselbe
  Weg wie im Ist-Stand 3: Kind-Token auf `supervisor/…/report` → **403** (`code: forbidden`, kein `answer` im
  Rumpf); Vater-Token auf dieselbe URL → **200** mit `answer='gehen'`/`'geht'` bei `introduced=false`; die
  alte `student/…`-Route → **404** für beide Rollen. Dazu `/smoke-test` vollständig grün (13 Checks).
  Der **Vertragsbruch traf genau wie geschätzt**: `docs/openapi/v1.json` zeigt Pfadschlüssel plus
  `Tags`-Namen, das Schema heißt weiter `Report`/`ItemReport` — darum blieben `contract.ts` **und**
  `types.ts` nach `npm run gen:contract` byte-gleich, und im Frontend änderte sich die eine vorhergesagte
  Zeile (`api.ts`). **R6 hält**: `docs/api-examples/` ist inhaltlich unverändert (die zwei Dateien zeigten
  nur CRLF-Rauschen und sind zurückgesetzt).
  Doku: 11 Stellen nachgezogen, die drei kind-adressierten **inhaltlich** ersetzt statt nur umgebogen — in
  `tutorial-student.md` ist aus Abschnitt 5 „Positionsreport" ein „Eigener Lernstand" geworden, der auf die
  `vocabulary-progress`-Sichten zeigt und den `403` benennt; derselbe Schnitt im Skill `student` und in
  `docs/REST/Student.http`, wo der Aufruf nach `Supervisor.http` umgezogen ist.
  **E3 ist beim Bauen umgeworfen worden, und zwar von seiner eigenen Kostenmessung.** Namespace-basiert
  aufgesetzt war das Tor nicht an vier, sondern an **zehn** Stellen rot; die sechs ungemessenen
  (`StudyPlansController.Get`, `ObjectivesController.List/Get`, `MyObjectivesController.List/Get`,
  `StudentPlansController.List`) zählen den **Normalfall** auf, weil `PlanResponse`/`ObjectiveResponse` *als
  Typen* dual gelesen sind — `StudentPlansController.List` ist sogar `[Authorize(Roles = Roles.Student)]`,
  also bewusst kind-only, und gibt trotzdem ein `Contracts.Supervisor`-DTO heraus. Damit greift **E4s eigenes
  Argument gegen E3**. Nach Vorlage beim Nutzer neu geschnitten als **E3′**: das Tor folgt dem Geheimnis statt
  dem Ordner — Lösungsfeld (`Answer`/`Solution`/`CorrectAnswer`) im Nutzlast-Graphen ⇒ Rollenmenge **ohne**
  `Student`. Ergebnis **4 Ausnahmen in einer Gruppe** (`RemarksController.*`, wo `Answer` die Antwort auf eine
  Notiz ist), und der Geltungsbereich ist ordner-unabhängig, womit **E4 gegenstandslos** wird: ein künftiges
  *Creator*-DTO mit Lösungsfeld fängt das Tor jetzt mit.
  Zwei Zwischenmessungen, die den Schnitt getragen haben und darum notiert sind: mit `Expected` in der
  Namensliste wären es **16** Ausnahmen statt 4 — `Expected` ist der Reveal *nach* der Antwort
  (`ItemOutcome`/`ReviewOutcome`/`ItemCheck`) und gehört nicht dazu. Und „ohne Student" statt „mit Supervisor"
  spart die Creator-Ausnahmen: `ClozeTextsController` gibt `Gap.Answer` heraus, ist `Roles.Creator`-gegated und
  läuft glatt durch — ein Autor **muss** die Lösung seiner Übung sehen. Beim Prüfen der Kandidaten fiel auch
  `ArithmeticDrillController.Generate → GeneratedProblem.Answer` auf; **keine vierte Tür**, die Basisklasse
  `ExerciseControllerBase` trägt `[Authorize(Roles = Roles.Creator)]` (nachgesehen, nicht vermutet).
  Das neue Tor ist ebenfalls **beide Zusicherungen einzeln rot gesehen** (Offender und Stale-Prüfung getrennt,
  weil die eine sonst die andere verdeckt), Untergrenze gemessen: 10 im Geltungsbereich, Grenze bei 8.
