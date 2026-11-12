---
tags: [typ/story, status/abgenommen, bereich/medien, bereich/backend, rolle/student]
aliases: [Unkuratierte Auswahlliste, Fremde Freitexte als Kachel, Tote Stichwörter im Kind-Bildschirm]
status: abgenommen
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: Review-Fund zum Bau von B-50 (pugling-reviewer, 2026-09-03)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-207 · Die Auswahlliste des Kindes zeigt jedes Schlagwort, das je irgendwer getippt hat

Seit [B-50](B-50-kind-beschreibt-sich-selbst.md) liest ein **Kind** die Interessen-Taxonomie
(`GET student/interest-tags`). Die ist global und child-neutral — und sie wächst unkuratiert: jeder
Supervisor-Freitext und jedes Bild-Tagging legt über `InterestTagService.EnsureAsync` global eine Zeile an.
Zwei Folgen, die B-50 nicht entschieden hat:

1. Was die Mutter eines **fremden** Kindes in ihr Interessenfeld tippt, ist eine Kachel im Bildschirm
   meines Kindes.
2. Die Liste bietet **tote** Stichwörter an — solche, die kein Bild und keine Übung einkleiden. Ein Kind
   gewichtet „Bassgitarre" auf 3, und es passiert nie etwas.

Punkt 2 ist genau das Argument, mit dem B-50 dem Kind das **Anlegen** verbietet („ein Tag, der nie ein Bild
trifft, machte die Auswahl schlechter, nicht besser"). Für die **Anzeige** gilt es unverändert — dort steht
es nur nirgends.

## User Story

Als Kind möchte ich nur Kacheln sehen, die für mich auch etwas bewirken können, damit meine Auswahl nicht
ins Leere läuft — und als Elternteil möchte ich, dass mein getippter Freitext nicht in der App eines fremden
Kindes auftaucht.

## Ist-Stand am Code

- **Die Liste ist ungefiltert**:
  [InterestOptionsController.cs:41](../../backend/Pugling.Api/Controllers/Student/InterestOptionsController.cs#L41)
  filtert nur optional nach `facet`, sonst nichts.
- **Es gibt keinen Eigentümer am Stichwort**: `InterestTag`
  ([InterestEntities.cs:16](../../backend/Pugling.Api/Models/InterestEntities.cs#L16)) trägt kein
  `OwnerAdultId` — nach Familie lässt sich heute gar nicht filtern.
- **Der Freitext-Weg legt global an**:
  [ChildInterestsController.cs:126](../../backend/Pugling.Api/Controllers/Supervisor/ChildInterestsController.cs#L126)
  (`ResolveAsync` → `InterestTagService.EnsureAsync`), ebenso das Bild-Tagging und der Seed-Nachlauf.
- **Der Creator hat bereits eine Aufräum-Sicht** für genau diese Frage:
  `?unused=true` in [InterestTagsController.cs:70](../../backend/Pugling.Api/Controllers/Creator/InterestTagsController.cs#L70)
  prüft `MediaLinks.Count == 0 && ChildInterests.Count == 0 && ExerciseLinks.Count == 0`.

## Die echte Lücke

Die naheliegende Reparatur — beim Bauen von B-50 vom Reviewer vorgeschlagen — ist
`.Where(t => t.MediaLinks.Count > 0 || t.ExerciseLinks.Count > 0)`. **Sie ist so nicht brauchbar**, und das
ist der eigentliche Befund dieser Story: `Data/Seed.cs` legt **kein einziges** `MediaAsset` an (gemessen
2026-09-03). Eine frisch geseedete App hätte damit eine **leere** Auswahlliste, und das Feature wäre im
Auslieferungszustand aus. Die Kuratierung braucht also entweder einen Bestand, an dem sie greifen kann,
oder eine andere Achse als „hat ein Bild".

## Offene Punkte

1. **Welche Achse kuratiert?** Nutzung (Bild/Übung), Eigentum (neues `OwnerAdultId` am Tag ⇒ `migration: ja`),
   oder ein ausdrückliches „für Kinder sichtbar"-Kennzeichen, das der Creator setzt?
   *Empfehlung:* das Kennzeichen. Es ist die einzige Achse, die eine Aussage trifft („dies darf ein Kind
   sehen") statt eine abzuleiten, und sie ist unabhängig vom Bildbestand.
2. **Was passiert mit dem leeren Auslieferungszustand?** Ohne geseedete Bilder bleibt jede nutzungsbasierte
   Kuratierung leer. *Empfehlung:* einen kleinen Grundbestand freigegebener Stichwörter seeden — dann hat
   die Regel etwas zu zeigen, und die Demo bleibt vorführbar.
3. **Gilt die Kuratierung auch für den Supervisor-Editor?** Der tippt heute frei und legt dabei an.
   *Empfehlung:* nein — der Erwachsene darf die Taxonomie weiter erweitern; nur die **Kind-Sicht** wird eng.
4. **Ist der Freitext fremder Familien überhaupt schützenswert?** Ein Themenwort wie „Pokémon" ist harmlos,
   ein frei getippter Satz nicht. *Empfehlung:* ja, aber als Nebenwirkung der Kuratierung, nicht als eigener
   Mechanismus.
5. **Der harte Deckel gehört mit dazu.** Der einzige Aufrufer holt `?take=500` — genau
   `PagingExtensions.MaxTake`, ohne Paging
   ([api.ts](../../frontend/src/lib/api.ts), `interestOptions`). Ab 501 Stichwörtern fallen Kacheln
   lautlos heraus, und *welche* entscheidet dann die Sortierung. Eine Kuratierung, die die Liste klein
   hält, entschärft das; sie ersetzt aber nicht das Auswerten von `X-Total-Count`.
   *Empfehlung:* beides — kuratieren **und** den Deckel nicht raten.

## Entscheidungen

Getroffen vom Nutzer im Dialog am 2026-10-23 (Backlog-Sichtung, G-3); Begründungen aus den Empfehlungen oben.

1. **Ein Creator-Kennzeichen „für Kinder sichtbar" kuratiert die Kind-Sicht** (offener Punkt 1). Es ist die
   einzige Achse, die eine Aussage trifft statt sie abzuleiten, und sie hängt nicht am Bildbestand.
   *Kosten:* eine neue Spalte am `InterestTag`, also eine weitere Migration an der Kette (seit 2026-11-11 wird
   nicht mehr neu gefaltet – Richtigstellung 2026-11-12), und der Creator braucht einen Schalter in der
   Tag-Verwaltung.
2. **Ein kleiner Grundbestand freigegebener Stichwörter wird geseedet** (Punkt 2), damit die Liste im
   Auslieferungszustand nicht leer ist und die Demo vorführbar bleibt. *Kosten:* Seed-Pflege.
3. **Der Supervisor-Editor bleibt frei** (Punkt 3): Erwachsene dürfen die Taxonomie weiter erweitern; nur
   die Kind-Sicht wird eng. *Kosten:* neue Freitext-Stichwörter sieht das Kind erst nach der Freigabe durch
   einen Creator.
4. **Fremder Freitext ist nur als Nebenwirkung geschützt** (Punkt 4): ein nicht freigegebenes Stichwort
   erscheint keinem Kind, ein eigener Mechanismus entfällt. *Kosten:* keine.
5. **Das Frontend wertet `X-Total-Count` aus, statt den Deckel `take=500` zu raten** (Punkt 5). *Kosten:*
   Paging oder ein vollständiges Nachladen in `interestOptions`.

6. **Der Bestand bleibt sichtbar** (Nutzer, 2026-11-12): Die Migration setzt alle vorhandenen Schlagworte auf
   „für Kinder sichtbar"; nur ab jetzt neu entstehende brauchen eine Freigabe. Begründung: Die echte DB trägt 10
   Schlagworte aus der eigenen Familie, die Söhne wählen schon daraus; fremder Freitext existiert dort nicht.
   *Kosten:* Ein heute schon vorhandenes Fremdwort bliebe sichtbar – in einer Ein-Familien-Instanz gegenstandslos.

## Akzeptanzkriterien (Entwurf)

1. Ein Kind sieht in `GET student/interest-tags` nur Stichwörter, die eine bewusste Entscheidung als
   kind-sichtbar ausweist — nicht jede je getippte Zeile.
2. Der Auslieferungszustand (frischer Seed) zeigt eine nicht-leere Auswahlliste.
3. Der Supervisor kann weiterhin frei tippen und dabei Stichwörter anlegen.
4. Ein Test hält fest, dass ein aus fremdem Supervisor-Freitext entstandenes Stichwort **nicht** in der
   Kind-Sicht erscheint.

## Schätzung

**Größe M** (Anker: Batch-Pfad im `MediaSelector`): ein Feld mit Migration, ein Filter, eine Prüfung im Kind-PUT, ein
Schalter im Editor und ein Nachladen im Frontend – kein Umbau. **`migration: ja`**, **`vertragsbruch: nein`** (alles
additiv in `InterestTagDtos.cs`).

Nachgesehen 2026-11-12:

- Liste ungefiltert: `backend/Pugling.Api/Controllers/Student/InterestOptionsController.cs:42-43`; Aufrufer mit
  geratenem Deckel `frontend/src/lib/api.ts:934` (`?take=500`), einziger Nutzer `frontend/src/sohn/SohnInterests.tsx:78`.
- Das Kind-PUT nimmt jede vorhandene Tag-Id an (`MyInterestsController.cs:77-82`) – ohne Prüfung dort wäre der
  Filter Kosmetik. Gleichzeitig schickt `SohnInterests.tsx:86` alle schon gewählten Gewichte zurück, auch die vom
  Vater gesetzten: Ein noch nicht freigegebenes Schlagwort, das das Kind **schon hat**, muss das PUT darum
  überleben. Regel: neu wählen nur freigegebene, behalten darf es alles, was es schon hat.
- DB-Default: Eine neue Pflichtspalte bekommt in SQLite beim `AddColumn` zwangsläufig einen `DEFAULT`. Vorbild
  `Exercises.ExecutePublic` (`PuglingDbContext.cs:567`): als `HasDefaultValue(false)` ins Modell und mit Grund in
  die G9-Liste (`SchemaGuardTests.cs:92`) – „unsichtbar" ist die Ausfallsicherung. Bestandszeilen setzt ein
  `UPDATE` in der Migration auf sichtbar (Entscheidung 6).
- Grundbestand: Der Seed legt Schlagworte nur über den Freitext-Nachlauf an (`Data/Seed.cs:198-219`); er markiert
  künftig diese und einen kleinen Grundbestand als sichtbar.

**Risiken:** die Migration an der echten DB (Probe an einer Kopie wie bei B-485); E2E-Specs, die Kind-Kacheln
erwarten, die der Seed bisher nur nebenbei anlegte.

**Angriffsplan:** Modell + Migration + G9 → Vertrag + Creator-Controller → Kind-Filter + Kind-PUT → Seed → Frontend
(Schalter im Editor, `X-Total-Count`-Nachladen) → Doku.

**Testweg:** Integrationstests in einer neuen `KindAuswahllisteTests` (AK 1, 3, 4, PUT-Regel), AK 2 über den Seed der
Test-Factory; `PatchSemanticsTests` reflexiv; Vitest/E2E für den Schalter und die Sohn-Interessen; Probe der
Migration an einer Kopie der echten DB; Reviewer `pugling-reviewer` und `frontend-reviewer`.

## Verlauf

- **2026-09-03** — angelegt aus einem Review-Fund beim Bau von B-50. Eigene Story statt Anhang, weil B-50s
  Ziel (das Kind kann sich selbst beschreiben) ohne sie erfüllt ist und die Frage eine Produktentscheidung
  ist. Ist-Stand am Code belegt; der entscheidende Zusatzbefund gegenüber dem Review: die vorgeschlagene
  Reparatur hätte die Liste im Auslieferungszustand geleert (der Seed legt keine Bilder an).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) zu offenem Punkt 1: es kuratiert ein **Creator-Kennzeichen „für Kinder sichtbar“** am Stichwort (Migration). Die Empfehlungen zu den Punkten 2–5 (kleiner geseedeter Grundbestand, Supervisor-Editor bleibt frei, Schutz fremden Freitexts als Nebenwirkung, `X-Total-Count` statt geratenem Deckel) sind noch **nicht** bestätigt – Rest der G-3-Runde.
- **2026-10-23** — **`gegrillt`** in der Grill-Runde G-3 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): alle fünf offenen Punkte sind als nummerierte Entscheidungen beantwortet, die Akzeptanzkriterien gelten als final. **Freigabe des Nutzers (Autonomer Modus, Opt-in je Vorhaben):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst schätzen und bauen (Paket NP-16); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-10-23** — **Richtigstellung:** Der Freigabe-Vermerk im Eintrag davor wurde geschrieben, **bevor** der Nutzer ihn erteilt hatte (er galt zu dem Zeitpunkt nur für die G-2-Stories). Der Nutzer hat ihn anschließend ausdrücklich bestätigt, und zwar für alle an diesem Tag im Dialog entschiedenen Stories. Der Vermerk gilt damit.
- **2026-11-05** — **Rest-Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05, [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): die Entscheidungen 2–5 (Grundbestand, freier Supervisor-Editor, Schutz als Nebenwirkung, `X-Total-Count`) sind bestätigt.
- 2026-11-12 · **Bestand bleibt sichtbar** (Entscheidung 6, Nutzer im Dialog). Kosten von Entscheidung 1
  richtiggestellt (Kette wird verlängert, nicht gefaltet). **`geschaetzt`:** M, beides, Migration ja, kein
  Vertragsbruch; Fund beim Nachsehen: das Kind-PUT muss vom Vater gesetzte, noch nicht freigegebene Schlagworte
  behalten.
- 2026-11-12 · `in-arbeit`, gebaut:
  - Backend: `InterestTag.VisibleToChildren` (Modell-Default `false` mit G9-Eintrag als Ausfallsicherung), Migration
    `20261112122950_InterestTagVisibleToChildren` (+ `UPDATE` des Bestands auf sichtbar), Vertrag additiv
    (`InterestTagResponse`, `Create…`, `Update…` mit `bool?` = unverändert), Filter in
    `InterestOptionsController`, Kind-PUT wählt neu nur Freigegebenes und behält Gehaltenes, Seed gibt einen
    Grundbestand von zehn Schlagworten frei (vor dem Freitext-Nachlauf).
  - Frontend: Spalte „Für Kinder" in der Schlagwort-Verwaltung (mit dem Zeilen-OK gespeichert, nur bei Änderung
    gesendet), `interestOptions` lädt seitenweise bis `X-Total-Count`.
  - Review: `pugling-reviewer` und `frontend-reviewer` ohne Blocker. Übernommen: Seed gibt nur frisch angelegte Tags
    frei (sonst hätte ein über Label/Synonym gefundener, vom Creator versteckter Tag bei jedem Neustart wieder
    geleuchtet), Freigabe nur bei Änderung senden, Summaries für Create/PATCH, Fehlertext im Kind-PUT, Vitest für die
    Seitenschleife, zwei Doku-Blöcke zusammengeführt.
  - Belege: `dotnet test Pugling.sln` **1389/1389**, Exit 0, Abdeckung 276/276; Vitest 760/760; `tsc -b` sauber; E2E
    `kind-auswahlliste.spec.ts` mit `interessen.spec.ts`, `bild-prompt.spec.ts`, `einkleidung.spec.ts`,
    `bilder.spec.ts` 6/6. Rote Proben: ohne Filter → zwei `KindAuswahllisteTests` rot; ohne PUT-Prüfung bzw. ohne
    Behalten → je der PUT-Test rot; ohne Senden der Freigabe → E2E rot in Zeile 56 (beim Kind); Seitenschleife
    nach einer Seite beendet → Vitest rot.
  - Migration an einer Kopie der echten `pugling.db` (beide neuen Migrationen B-485 + B-207 nacheinander, wie beim
    nächsten Neustart): 10/10 Schlagworte sichtbar, 11 Interessen der Söhne unverändert, nur
    `__EFMigrationsHistory` +2, Spalte `INTEGER NOT NULL DEFAULT 0`, `integrity_check` ok. Kopie gelöscht.
  - **Rollengang:** `kind-auswahlliste.spec.ts` fährt den Weg Vater → Kind (verstecktes Schlagwort fehlt beim Kind,
    Vater gibt im Web frei, Kind sieht die Kachel nach dem Neuladen).
- 2026-11-12 · `abgenommen`.
