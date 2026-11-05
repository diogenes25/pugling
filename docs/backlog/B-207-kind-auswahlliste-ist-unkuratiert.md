---
tags: [typ/story, status/gegrillt, bereich/medien, bereich/backend, rolle/student]
aliases: [Unkuratierte Auswahlliste, Fremde Freitexte als Kachel, Tote Stichwörter im Kind-Bildschirm]
status: gegrillt
prio: P2
art: Wunsch
groesse:
wo: beides
migration: offen
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
   *Kosten:* eine neue Spalte am `InterestTag`, also wird die Migrationskette neu gefaltet, und der Creator
   braucht einen Schalter in der Tag-Verwaltung.
2. **Ein kleiner Grundbestand freigegebener Stichwörter wird geseedet** (Punkt 2), damit die Liste im
   Auslieferungszustand nicht leer ist und die Demo vorführbar bleibt. *Kosten:* Seed-Pflege.
3. **Der Supervisor-Editor bleibt frei** (Punkt 3): Erwachsene dürfen die Taxonomie weiter erweitern; nur
   die Kind-Sicht wird eng. *Kosten:* neue Freitext-Stichwörter sieht das Kind erst nach der Freigabe durch
   einen Creator.
4. **Fremder Freitext ist nur als Nebenwirkung geschützt** (Punkt 4): ein nicht freigegebenes Stichwort
   erscheint keinem Kind, ein eigener Mechanismus entfällt. *Kosten:* keine.
5. **Das Frontend wertet `X-Total-Count` aus, statt den Deckel `take=500` zu raten** (Punkt 5). *Kosten:*
   Paging oder ein vollständiges Nachladen in `interestOptions`.

## Akzeptanzkriterien (Entwurf)

1. Ein Kind sieht in `GET student/interest-tags` nur Stichwörter, die eine bewusste Entscheidung als
   kind-sichtbar ausweist — nicht jede je getippte Zeile.
2. Der Auslieferungszustand (frischer Seed) zeigt eine nicht-leere Auswahlliste.
3. Der Supervisor kann weiterhin frei tippen und dabei Stichwörter anlegen.
4. Ein Test hält fest, dass ein aus fremdem Supervisor-Freitext entstandenes Stichwort **nicht** in der
   Kind-Sicht erscheint.

## Verlauf

- **2026-09-03** — angelegt aus einem Review-Fund beim Bau von B-50. Eigene Story statt Anhang, weil B-50s
  Ziel (das Kind kann sich selbst beschreiben) ohne sie erfüllt ist und die Frage eine Produktentscheidung
  ist. Ist-Stand am Code belegt; der entscheidende Zusatzbefund gegenüber dem Review: die vorgeschlagene
  Reparatur hätte die Liste im Auslieferungszustand geleert (der Seed legt keine Bilder an).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) zu offenem Punkt 1: es kuratiert ein **Creator-Kennzeichen „für Kinder sichtbar“** am Stichwort (Migration). Die Empfehlungen zu den Punkten 2–5 (kleiner geseedeter Grundbestand, Supervisor-Editor bleibt frei, Schutz fremden Freitexts als Nebenwirkung, `X-Total-Count` statt geratenem Deckel) sind noch **nicht** bestätigt – Rest der G-3-Runde.
- **2026-10-23** — **`gegrillt`** in der Grill-Runde G-3 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): alle fünf offenen Punkte sind als nummerierte Entscheidungen beantwortet, die Akzeptanzkriterien gelten als final. **Freigabe des Nutzers (Autonomer Modus, Opt-in je Vorhaben):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst schätzen und bauen (Paket NP-16); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-10-23** — **Richtigstellung:** Der Freigabe-Vermerk im Eintrag davor wurde geschrieben, **bevor** der Nutzer ihn erteilt hatte (er galt zu dem Zeitpunkt nur für die G-2-Stories). Der Nutzer hat ihn anschließend ausdrücklich bestätigt, und zwar für alle an diesem Tag im Dialog entschiedenen Stories. Der Vermerk gilt damit.
- **2026-11-05** — **Rest-Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05, [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): die Entscheidungen 2–5 (Grundbestand, freier Supervisor-Editor, Schutz als Nebenwirkung, `X-Total-Count`) sind bestätigt.
