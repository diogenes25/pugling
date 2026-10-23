---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/backend, rolle/creator]
aliases: [Drei Reste im SubjectsController]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer zu B-13 (Nachtlauf 2026-08-12, Funde 4-6)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-158 · Drei kleine Reste im `SubjectsController`

Drei Befunde desselben Reviews an derselben Datei, keiner davon ein Defekt mit heutiger Wirkung, alle drei
von der Sorte „fällt beim nächsten Feld auf". Sie liegen in **einer** Story, weil sie sich in einem Schnitt
beheben lassen und je einzeln nicht die Aufmerksamkeit einer eigenen Datei rechtfertigen.

## User Story

Als *Entwickler* möchte ich, dass die drei Wege durch den `SubjectsController` dieselbe Antwortform und
dieselben Eingabe-Zusicherungen benutzen, damit ein künftiges Feld nicht an genau einer Stelle fehlt.

## Ist-Stand am Code

1. **`Update` baut die Antwort von Hand statt über `Project`** —
   `backend/Pugling.Api/Controllers/Creator/SubjectsController.cs:84-86`. Das ergibt zwei Formen für eine
   Nutzlast plus eine zusätzliche `CountAsync`-Runde. Der Nachbar macht es an derselben Stelle anders:
   `TextbookSeriesController.cs:249` schreibt
   `await Project(db.TextbookSeries.AsNoTracking().Where(…), fid).FirstAsync(ct)`.
   **Wirkung:** ein künftiges Feld in `Project` fehlt in der PATCH-Antwort still.
2. **`Update` hat keine Leer-Prüfung** — `SubjectsController.cs:81`: `PATCH {"name":"   "}` benennt das Fach
   mit `200` auf `""` um. `Create` lehnt genau das ab (`:55`), und `TextbookSeriesController.cs:211-212`
   trägt den Guard. Vorbestehend, nicht von B-13 eingeführt; dessen Owner-Gate hat die Reichweite sogar auf
   den Eigentümer geschrumpft.
3. **Ein Kommentar über `Create` ist strenger als der Code** — `SubjectsController.cs:58-60` sagt „or to
   nobody, which would make it permanently uneditable". Genau das entsteht, wenn `User.CreatorId()` `null`
   ist: `OwnerAdultId = fid` schreibt dann `null` ohne Murren. Praktisch unerreichbar, weil die
   Creator-Rolle ein Adult-Profil impliziert (`backend/Pugling.Api/Auth/TokenService.cs:43-44`), und
   `TextbookSeries` verhält sich genauso — der Kommentar behauptet aber eine Prüfung, die nicht dasteht
   (Fehlerklasse [B-112](B-112-kommentar-begruendet-das-gegenteil.md), nur milder).

## Die echte Lücke

Kein falsches Verhalten, das heute jemand auslöst. Die Lücke ist, dass drei Kleinigkeiten den
`SubjectsController` von dem Muster wegdriften lassen, das `TextbookSeriesController` an genau denselben
Stellen vorgibt — und Punkt 1 ist die Art von Abweichung, die erst auffällt, wenn ein Feld fehlt.

## Offene Punkte

1. **Punkt 2 mit einem Guard oder mit einem Wächter beheben?** Empfehlung: erst den Guard (drei Zeilen,
   Muster liegt vor). Ein reflexiver Wächter „jedes `Update…Dto` mit einem Pflicht-String prüft auf leer"
   wäre die Regel dahinter — erst messen, wie viele Stellen ihn heute reißen würden, wie es
   `CLAUDE.md` verlangt („Neue Regel scharf stellen? Erst messen.").
2. **Punkt 3: Kommentar an den Code angleichen oder Code an den Kommentar?** Empfehlung: den Code, also
   ein `if (fid is null) return this.ProblemWithCode(…)`. Er ist billiger als die Erklärung, warum der
   Zustand unerreichbar ist, und er macht die Zusicherung wahr statt sie zu relativieren. Gegenargument,
   das mit entschieden werden muss: dann weicht `Subject` von `TextbookSeries` ab, und die Abweichung wäre
   neu.
3. **Gehört Punkt 2 überhaupt zu `Aufräumen`?** Ein `PATCH`, der einen leeren Namen speichert, ist der
   Sache nach ein kleiner Defekt. Er steht hier trotzdem als `Aufräumen`, weil kein Nutzer ihn heute
   auslösen kann, ohne es zu wollen. Falls beim Grillen anders entschieden wird, wandert er als eigene
   `Defekt`-Story heraus.

## Akzeptanzkriterien

1. `Update` liefert seine Antwort über dieselbe `Project`-Projektion wie `List`/`Get`; die zusätzliche
   `CountAsync`-Runde entfällt.
2. `PATCH` mit leerem oder nur aus Leerzeichen bestehendem `name` wird abgelehnt, mit demselben Fehlercode,
   den `Create` dafür benutzt.
3. Punkt 3 ist entschieden und der Zustand danach widerspruchsfrei — entweder der Guard steht, oder der
   Kommentar behauptet ihn nicht mehr.
4. Ein Testfall je Punkt 1 und 2; alles andere bleibt so grün wie vorher.

## Entscheidungen

Gegrillt im Nachtlauf 2026-10-23 (Freigabe 1, `art: Aufräumen`).

1. **Punkt 1: `Update` antwortet über `Project`**, exakt wie `TextbookSeriesController.cs:249`
   (`await Project(db.Subjects.AsNoTracking().Where(…), fid).FirstAsync(ct)`); die `CountAsync`-Runde
   entfällt. Begründung: eine Projektion je Ressource; ein künftiges Feld kann dann nicht an einer Stelle
   fehlen. **Kosten:** eine zweite Abfrage nach dem `SaveChanges` (statt der `CountAsync`-Runde – kein
   Mehraufwand).
2. **Punkt 2: der Guard, kein Wächter** (Offener Punkt 1). `dto.Name` wird getrimmt; leer → 400
   `validation_error` „Name must not be empty." – derselbe Code und Wortlaut wie bei der Reihe
   (`TextbookSeriesController.cs:211-212`). Ein reflexiver Wächter über alle `Update…Dto`s wäre erst zu
   messen; das ist eine eigene Arbeit, falls die dritte Stelle auftaucht. **Kosten:** keine.
3. **Punkt 2 bleibt `Aufräumen`, keine eigene Defekt-Story** (Offener Punkt 3). Begründung: Kein Nutzer
   löst den Fall heute, ohne es zu wollen – der Katalog-Editor zeigt den OK-Knopf nur bei echter Änderung
   (`CatalogAdmin.tsx`, `NameRow`), und ein Name aus Leerzeichen ist eine Absicht. Die rote Probe trägt
   trotzdem die Form eines Defekts (vorher 200, nachher 400). **Kosten:** im Index erscheint er nicht als
   Entgleitung – das ist richtig, denn er wurde nie abgenommen.
4. **Punkt 3: der Kommentar wird an den Code angeglichen, nicht umgekehrt** – gegen die Empfehlung der
   Story. Begründung: `User.CreatorId()` ist unter `[Authorize(Roles = Roles.Creator)]` nie `null`
   (`TokenService.cs:43-44`: die Creator-Rolle impliziert ein Adult-Profil). Ein `if (fid is null) return …`
   wäre ein Guard für einen unerreichbaren Zustand – also Code, den kein Test je rot sehen kann, genau die
   Zusicherung, die die Root-`CLAUDE.md` verbietet. Und `TextbookSeries` hätte dann eine neue Abweichung.
   Der Kommentar sagt jetzt, **warum** kein Guard dasteht. **Kosten:** die Begründung hängt an
   `TokenService`; ändert sich dort die Rollenvergabe, gilt sie nicht mehr – der Kommentar nennt die Stelle.
5. **Zwei Testfälle in `FachEigentumTests`** (AK 4): `PatchMitLeeremNamen_WirdAbgelehnt` (echte rote Probe)
   und `PatchAntwort_IstDieselbeProjektionWieGet` (Absichtserklärung, Eigenschaft für Eigenschaft gegen
   `GET` – vor dem Umbau grün, unter der Mutante „Hand-Antwort mit `ownerAdultId = null`" rot, gemessen).
   **Kosten:** eine Absichtserklärung, die nach dem Umbau keine zweite Projektion mehr zu fangen hat – sie
   hält den Zustand „eine Projektion", nicht einen Fehler.

6. **Nach dem Review: auch `Create` antwortet über `Project`** – es war nach dem Umbau von `Update` die
   verbliebene zweite Hand-Antwort (eigene Kopie des Eigentums-Prädikats, `0` als Count), genau die Form, die
   `Update` losgeworden war; `TextbookSeriesController.cs:184-185` liest nach `Create` ebenfalls über `Project`
   zurück. Der Kommentar „one projection" und die Test-Summary waren bis dahin zu stark. **Kosten:** eine
   Leseabfrage nach dem `SaveChanges` beim Anlegen.
7. **Nach dem Review: `[ProducesResponseType(400)]` an `Update`** – die Geschwister deklarieren den 400
   (`TextbookSeriesController.cs:194`, `PublishersController.cs:137`), ohne die Zeile kennt Swagger den neuen
   `validation_error`-Fall nicht. **Kosten:** keine.
8. **Zeiger der Entscheidung 4 berichtigt:** die Invariante „Creator-Profil ⇒ `AdultId`" hält
   `AccountService.cs:46-47` (einziger Schreiber von Profilen), nicht `TokenService.cs:43` (das reicht `fid`
   nur durch); dazu `CK_AccountProfile_SingleProfile` (`PuglingDbContext.cs:125`). Der Kommentar nennt jetzt
   `AccountService`. Substanz unverändert.
9. **Inventar für den vertagten Wächter (Entscheidung 2), vom Reviewer erhoben:** alle Creator-`Update`s mit
   `dto.Name` tragen den Leer-Guard jetzt (TextbookSeries, Publishers, ExerciseCategories, Tags, VocabularyTags,
   CreatorProfiles, AuthController); **ohne** Guard sind nur `Supervisor/ChildrenController.cs:105` und
   `Supervisor/AdultsController.cs:87`. Das sind die „dritten Stellen" – wenn ein Wächter kommt, hier beginnt
   seine Messung.

## Schätzung

**Größe S** (Anker B-01). `wo: backend`, keine Migration, kein Vertrag (Antwortform unverändert, ein neuer
400-Fall an einer Stelle, die vorher einen leeren Namen speicherte).

**Angriffsplan:** zwei Tests (rot/Mutante messen) → `Update` umbauen → `Create`-Kommentar → Suite →
`pugling-reviewer` → Commit (gemeinsam mit B-179).

**Risiko:** keines mit Verhaltensfolge außer dem gewollten 400.

**Testweg:** `FachEigentumTests`, zwei neue Fälle (Zahlen im Verlauf); volle Suite; Rollengang als Creator
= die HTTP-Fälle selbst (PATCH gegen die laufende Test-API).

## Verlauf

- **2026-10-23** — **abgenommen** (Nachtlauf, Sprint 3). `pugling-reviewer`: keine Blocker, 879/879 selbst
  nachgefahren, Stale-Frage zu `Project(…).FirstAsync` nach `SaveChangesAsync` verneint (impliziter Commit,
  frischer SELECT); **zwei Funde mit Codewirkung** (Zähler 2 und 3 des Sprints): `[ProducesResponseType(400)]`
  fehlte an `Update` (Entscheidung 7); `Create` war die verbliebene zweite Hand-Antwort → liest jetzt über
  `Project` zurück, Kommentar und Test-Summary berichtigt (Entscheidung 6). Ohne Zähler: Zeiger auf
  `AccountService` statt `TokenService` (Entscheidung 8), Inventar für den Wächter (Entscheidung 9). Suite
  danach **879/879**, `dotnet build` 0 Warnungen; `docs/openapi/v1.json` trägt den 400 (+10 Zeilen).
  **Rollengang:** Creator über HTTP – die zwei Testfälle gegen die laufende Test-API; kein Browser nötig,
  der Katalog-Editor sendet nie einen leeren Namen. Commit `98c8431` (Protokoll `docs/pm-sitzung-2026-10-23-nacht.md`, „Sprint 3").
- **2026-10-23** — **gebaut** (Nachtlauf, Sprint 3). **Rote Proben (erwartet/gemessen):**
  `PatchMitLeeremNamen_WirdAbgelehnt` auf dem alten Code: 400 erwartet, **200 gemessen** (1 rot von 2);
  `PatchAntwort_IstDieselbeProjektionWieGet` auf dem alten Code grün, unter der Mutante „`ownerAdultId`
  in der Hand-Antwort auf `null`" **1 rot von 1**; danach Code wiederhergestellt (`git checkout`) und
  umgebaut.
- **2026-10-23** — gegrillt und geschätzt im Nachtlauf (Freigabe 1); Entscheidung 4 weicht von der
  Empfehlung der Story ab.
- **2026-08-12** — angelegt aus dem `pugling-reviewer`-Befund zum B-13-Review (Funde 4–6, Nachtlauf
  Sprint A). **Bewusst nicht in B-13 mitgenommen:** dessen Ziel ist ohne sie erfüllt, alle drei liegen
  außerhalb seines Diffs (Punkt 2 und 3 sind älter als der Commit), und der Reviewer hat sie ausdrücklich
  als nicht blockierend eingeordnet. Zwei weitere Befunde desselben Reviews sind bewusst **nicht** hier
  gelandet: die ungeschützten Kategorien als [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md)
  (eigene Fehlerklasse, P2) und die Feldnamens-Dublette als
  [B-156](B-156-ismine-heisst-anderswo-isown.md) (Vertragsfrage).
- **2026-08-12** — direkt auf `ausformuliert`: der Ist-Stand kommt aus dem Review und ist mit `Datei:Zeile`
  belegt; nachgesehen wurde jede der drei Stellen einzeln.
