---
tags: [typ/story, status/abgenommen, bereich/medien, bereich/frontend, bereich/auth, rolle/student, rolle/supervisor]
aliases: [Interessen vom Kind, Selbstbeschreibung, Interessen-Onboarding]
status: abgenommen
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
nachgeschaut: 2026-09-03
quelle: Sitzung 2026-08-01 (Rollen-Durchgang, Nutzer-Entscheidung)
wartet_auf: ""
---

# B-50 · Das Kind beschreibt sich selbst: Interessen in einem geführten Prozess

Heute pflegt der **Supervisor** die Interessen des Kindes; das Kind hat keinen Schreibweg. Es soll sie
selbst angeben können — in einem geführten Prozess mit **Auswahllisten** statt Freitext, damit es für ein
Kind so leicht wie möglich ist. Der Anti-Cheat-Einwand greift hier nicht: Nach der Hausregel „Interessen
kleiden den Stoff ein, sie ersetzen ihn nie" ändern sie weder *welche* Inhalte geübt werden noch wie oft
oder mit welcher Schwelle. Es ist damit die einzige Stelle, an der sich das Kind ohne Risiko selbst
einbringen kann.

## User Story

Als Kind möchte ich in ein paar geführten Schritten aus Bildern und Kacheln auswählen, was ich mag und was
gar nicht, damit die Übungen und Bilder zu mir passen — ohne tippen zu müssen und ohne meine Eltern zu
fragen.

## Ist-Stand am Code

Das Fundament ist stärker als erwartet — es fehlt fast nur die Kindseite. Alle Belege sind gegen den
heutigen Code nachgeprüft (2026-08-04); an den Zeilen hat sich seit dem Ausformulieren nichts verschoben.

- **Die Taxonomie trennt Thema und Stil bereits.** `InterestTag`
  ([InterestEntities.cs:16](../../backend/Pugling.Api/Models/InterestEntities.cs#L16)) trägt eine
  `Facet`; die Werte
  ([MediaBaseTypes.cs:72](../../backend/Pugling.Contracts/Common/MediaBaseTypes.cs#L72)) sind acht
  thematische (Franchise, Sport, Animal, Vehicle, Music, Hobby, Nature, Other) **plus `Style`**
  („comic, foto, pixel art – orthogonal zum Thema",
  [:91](../../backend/Pugling.Contracts/Common/MediaBaseTypes.cs#L91)). Genau die zwei Achsen, die eine
  Auswahlliste braucht.
- **Abneigungen sind schon modellierbar.** `ChildInterest.Weight` läuft von
  `MinWeight = -3` bis `MaxWeight = 3`
  ([InterestEntities.cs:67](../../backend/Pugling.Api/Models/InterestEntities.cs#L67) ff.), Skala
  „bewusst grob, weil ein Mensch sie pflegt".
- **Ein Trichter für Text → Tag existiert**, samt Synonymen und Duplikat-Schutz:
  [InterestTagService.cs:25](../../backend/Pugling.Api/Services/Shared/InterestTagService.cs#L25)
  (`EnsureAsync` / `EnsureManyAsync`). Drei Pfade laufen dort schon zusammen (Creator taggt ein Bild,
  Supervisor tippt ein Interesse, Backfill übernimmt Freitext) — ein vierter (Kind wählt aus) wäre kein
  neuer Mechanismus, sondern **darf ihn gerade nicht benutzen** (siehe „Die echte Lücke").
- **Schreiben darf heute nur der Supervisor**:
  [ChildInterestsController.cs:26](../../backend/Pugling.Api/Controllers/Supervisor/ChildInterestsController.cs#L26)
  (`[Authorize(Roles = Roles.Supervisor)]`, Route `supervisor/children/{childId}/interests`). Unter
  `Controllers/Student/` kommt `ChildInterest` **nicht vor** (Grep bestätigt) — es gibt keinen
  Kind-Schreibweg, auch keinen lesenden. Der Supervisor-Endpunkt löst unbekannte Eingaben über
  `InterestTagService.EnsureAsync` auf und **legt dabei neue Tags an** (`ResolveAsync`,
  [ChildInterestsController.cs:126-133](../../backend/Pugling.Api/Controllers/Supervisor/ChildInterestsController.cs#L126)) —
  genau das darf der neue Kind-Weg nicht tun.
- **Die vorhandene Vater-Web-Oberfläche nutzt diesen Endpunkt bereits**: `InterestEditor` in
  [VaterKind.tsx:310](../../frontend/src/vater/VaterKind.tsx#L310) zeigt/ändert die Interessen per
  PUT-Vollersatz. Akzeptanzkriterium 5 (Supervisor sieht die Interessen weiterhin) ist damit **heute schon
  erfüllt** und braucht keine neue Arbeit — nur der Kind-Schreibweg fehlt.
- **Die Taxonomie gehört dem Creator**:
  [InterestTagsController.cs:23](../../backend/Pugling.Api/Controllers/Creator/InterestTagsController.cs#L23)
  (`[Authorize(Roles = Roles.Creator)]`) — Tags anlegen ist heute keine Kind-Handlung und soll es nicht
  werden (siehe „Die echte Lücke"). Ein Kind kann die Taxonomie also auch **lesend** nicht abfragen; das
  braucht einen neuen, schlanken Student-Endpunkt.
- **Am `Child` liegen daneben zwei Freitextfelder**: `Interests` (JSON-Liste,
  [AdminEntities.cs:88](../../backend/Pugling.Api/Models/AdminEntities.cs#L88)) und `ProfileNotes`
  ([:92](../../backend/Pugling.Api/Models/AdminEntities.cs#L92)) — laut Kommentar ergänzt die Taxonomie
  den Freitext ausdrücklich, statt ihn zu ersetzen. Verbraucher des Freitexts ist allein der KI-Creator.
- **Die Bildwahl ist eingefroren**: `ChildMediaPick`
  ([MediaEntities.cs:157](../../backend/Pugling.Api/Models/MediaEntities.cs#L157)) — „Bildkonstanz *ist*
  der Merkeffekt" ([medien-bilder.md](../medien-bilder.md)). Der einzige vorhandene Rückkanal vom Kind ist
  heute „anderes Bild"
  ([ChildMediaPicksController.cs:25-52](../../backend/Pugling.Api/Controllers/Student/ChildMediaPicksController.cs#L25)),
  und dieses Signal wird nirgends als Interessenhinweis ausgewertet. Wichtig für die Wirkungsregel: der
  Selektor liest die Interessen nur **beim Ziehen einer neuen Wahl**, eine bestehende `ChildMediaPick`-Zeile
  wird nie neu bewertet — eine Profiländerung kann eine eingefrorene Wahl allein architektonisch **nicht**
  rückwirkend anfassen.
- **Die Produktbeschreibung der Sohn-App sagt heute das Gegenteil**: „Er legt keine Inhalte an und
  **steuert nichts**" ([sohn-app-funktionsbeschreibung.md:22](../sohn-app-funktionsbeschreibung.md)). Der
  Satz braucht mit dieser Story eine Ausnahme — Interessen sind keine Steuerung.
- **Die Geschlechterfrage ist zwischenzeitlich anderswo entschieden**: [B-46](B-46-interessenbasierte-uebungen.md)
  (Entscheidung 2, dort bereits `geschaetzt`) legt fest, dass `Gender` reine Einkleidungs-Information bleibt
  und **kein** Filter-/Zielgruppenmerkmal wird. B-50 braucht dieselbe Antwort nicht mehr selbst zu finden.
- **Muster für den Self-Service-Schreibweg existiert bereits**: `student/me/…`-Routen mit `[Authorize(Roles
  = Roles.Student)]` und `User.ChildId()` statt `{childId}`-Pfad, z. B.
  [MeController.cs:16-32](../../backend/Pugling.Api/Controllers/Student/MeController.cs#L16) und
  [MyObjectivesController.cs:17](../../backend/Pugling.Api/Controllers/Student/MyObjectivesController.cs#L17).
  Anders als `ChildMediaPicksController` (das auch der Supervisor per `{childId}` bedient) ist das hier der
  passende Zuschnitt, weil ausschließlich das Kind selbst schreibt (siehe Entscheidung 2).

## Die echte Lücke

Nicht das Datenmodell, sondern drei Dinge daneben:

1. **Ein kindsicherer Schreibweg** (`student/me/interests`) — auswählen aus der Taxonomie, gewichten,
   löschen. **Ohne** das Recht, neue `InterestTag`-Zeilen anzulegen: die Taxonomie ist die geteilte
   Matching-Achse zu den Bildern; ein langer Schwanz aus Einzel-Tags träfe *nie* ein Bild und machte die
   Auswahl schlechter, nicht besser. Ein Wunsch außerhalb der Liste geht als Freitext („Sonstiges: …"),
   den später ein Erwachsener über den vorhandenen Backfill zum Tag macht.
2. **Der geführte Prozess** in der Sohn-App — Kacheln statt Formular, ein eigener kurzer Durchgang „was
   geht gar nicht?" für die negativen Gewichte. Abbrechbar und fortsetzbar.
3. **Die Wirkungsregel gegenüber der eingefrorenen Bildwahl**: Eine Profiländerung wirkt auf **künftige**
   Wahlen; bestehende bleiben stehen, bis das Kind bewusst „anderes Bild" drückt. Ohne diese Regel lernt
   ein Kind, das gern an seinem Profil schraubt, jede Woche neue Bilder zum selben Wort — und der
   Merkeffekt, für den das Einfrieren gebaut wurde, ist weg. (Die Regel ist bereits architektonisch erfüllt,
   siehe Ist-Stand — hier bleibt nur, sie per Test **festzuhalten**, nicht sie zu bauen.)

Klar abzugrenzen: **Keine** Interessen sind Klassenstufe, Schulart und Lehrwerk/Unit — das sind
Lehrplan-Fakten und bleiben beim Supervisor.

## Offene Punkte

1. ~~Gehört das Geschlecht zur Zielgruppe oder nur zur Einkleidung?~~ → Entscheidung 1 (per Referenz auf
   B-46 erledigt).
2. ~~Schreibt das Kind direkt, oder muss der Supervisor freigeben?~~ → Entscheidung 2.
3. ~~Deckel gegen häufiges Ändern?~~ → Entscheidung 3.
4. ~~Bleiben `Child.Interests` (Freitext) und `ProfileNotes` beide?~~ → Entscheidung 4.
5. ~~Wird das Ablehnen eines Bildes als Signal ausgewertet?~~ → Entscheidung 5.
6. ~~Darf das Kind seinen eigenen Freitext schreiben?~~ → Entscheidung 6.
7. ~~Wann wird der Prozess angestoßen?~~ → Entscheidung 7.

## Entscheidungen

1. **Geschlecht bleibt reine Einkleidungs-Information, kein Filter-/Zielgruppenmerkmal.** Begründung:
   dieselbe Frage ist in [B-46](B-46-interessenbasierte-uebungen.md) Entscheidung 2 bereits so entschieden;
   beide Stories müssen dieselbe Antwort tragen, sonst driftet der Begriff „Zielgruppe" auseinander.
   `Gender` bleibt ein vom Supervisor gepflegtes Profilfeld (`UpdateChildDto`); der geführte Prozess dieser
   Story fasst es nicht an. Kosten: keine — reine Bestätigung, kein Code nötig.
2. **Das Kind schreibt direkt, ohne Freigabeschritt.** Begründung: eine Genehmigungsschlange widerspricht
   „so leicht wie möglich"; der Supervisor kann über den **bereits vorhandenen** Endpunkt
   (`supervisor/children/{id}/interests`, im Vater-Web als `InterestEditor` sichtbar) jederzeit
   nachsehen und korrigieren. Folge fürs Routendesign: Der neue Schreibweg braucht **kein** `{childId}` im
   Pfad — er ist immer „ich selbst", passend zum bestehenden `student/me/…`-Muster
   (`MeController`/`MyObjectivesController`) statt zum `{childId}`-Muster von
   `ChildMediaPicksController` (das auch der Supervisor mitbedient). Kosten: keine zusätzliche
   Moderationsschicht zu bauen — spart eher Aufwand.
3. **Kein serverseitiger Änderungsdeckel.** Begründung: Die eingefrorene Bildwahl (`ChildMediaPick`)
   entkoppelt Profiländerungen bereits strukturell von der Bebilderung (siehe Ist-Stand) — eine neue
   Gewichtung wirkt frühestens beim nächsten expliziten „anderes Bild", nie rückwirkend. Ein zusätzlicher
   Zeit-Deckel löste also ein Problem, das durch das Einfrieren schon gelöst ist, und Interessen fließen
   laut Story-Kopf nirgends in Punkte oder Pflicht ein — es gibt nichts zu gamen. Kosten: keine (kein neues
   Feld, keine Migration). Risiko: ein Kind kann beliebig oft ändern; hinnehmbar, da folgenlos.
4. **`Child.Interests` und `ProfileNotes` werden in dieser Story NICHT zusammengeführt.** Begründung: Das
   wäre eine Contracts-Änderung, die den KI-Creator als Verbraucher anfasst, unabhängig vom Kind-Schreibweg
   lösbar ist und die Größe Richtung XL triebe. Stattdessen bekommt das Kind über Entscheidung 6 einen
   schmalen neuen Schreibzugang zum **bestehenden** `Child.Interests`-Feld — es wird weiterverwendet, nicht
   ersetzt. Kosten: zwei Freitextfelder bleiben vorerst nebeneinander bestehen; eine eigene, spätere Story
   kann die Zusammenführung angehen.
5. **Bild-Ablehnung wird nicht als Interessensignal ausgewertet — zurückgestellt.** Begründung: vermischt
   „mag ich nicht" mit „kenne ich schon" und bräuchte eine eigene, unabhängige Auswertung. Kosten: keine
   (nichts gebaut); der vorhandene Rückkanal (`ChildMediaPicksController.Reshuffle`) bleibt unverändert.
6. **Das Kind darf einen kurzen, optionalen Freitext-Wunsch schreiben.** Begründung: kein geheimer Kanal —
   er landet für den Supervisor sichtbar in `Child.Interests` (bestehendes Feld, Entscheidung 4) und kann
   später über den vorhandenen Backfill-Trichter (`InterestTagService`) von einem Erwachsenen zum Tag
   gemacht werden. Kosten: neuer, schmaler Schreibpfad auf ein bislang nur dem Supervisor gehörendes Feld —
   serverseitig **muss** Länge (z. B. 60 Zeichen) und Listengröße gedeckelt sein, sonst wächst
   `Child.Interests` unbegrenzt (siehe Risiken).
7. **Anstoß beim ersten Login, danach freiwillig — ohne neues Feld/Migration.** Begründung: „beim ersten
   Login" lässt sich **heuristisch** aus einem leeren Interessen-Bestand ableiten (weder `ChildInterest`
   noch `Child.Interests` vorhanden) statt über eine neue Zeitstempel-Spalte am `Child` — das hält
   `migration: nein`. Ein dauerhafter Menüpunkt in der Sohn-App macht den Prozess zusätzlich jederzeit
   freiwillig erreichbar, statt ihn nach dem ersten Mal zu verstecken. Ein automatischer
   Halbjahres-/Geburtstags-Reminder (wie ursprünglich vorgeschlagen) wird zurückgestellt: eigener Zustand,
   kein belegter Bedarf, ließe sich als kleine Folge-Story ergänzen. Kosten: kein erzwungener
   Wiedervorlage-Reminder — ein Kind, das den Menüpunkt nie anklickt, aktualisiert sein Profil nie von
   selbst; hinnehmbar, weil unverändert lassen keinen Schaden anrichtet.
8. **UI-Mechanik vereinfacht: Kachel-Mehrfachauswahl statt Paarvergleichs-Algorithmus.** Begründung: Die
   ursprünglich skizzierten „Paarvergleiche"/„Top-3-Wahl" bräuchten eine eigene Ranking-Engine (mehr
   Zustand, mehr Tests) für denselben fachlichen Nutzen — gewichtete Tags. Eine Kachel-Mehrfachauswahl mit
   wenigen Gewichtsstufen (antippen = mag, zweite Stufe = Favorit; eigener Reiter „mag nicht") erreicht
   dieselben Akzeptanzkriterien mit einem Bruchteil des Aufwands und hält die Story bei `groesse: L` statt
   XL. Kosten: weniger „spielerisch" als ein Paarvergleich — ein akzeptierter Abstrich, korrigierbar in
   einer späteren Iteration, falls das Feedback (z. B. über `pm-loop`) das nahelegt.
9. **Der Kind-Schreibweg löst Eingaben ausschließlich über bestehende `TagId`s auf, ruft `EnsureAsync`
   nie auf.** Begründung: setzt „Die echte Lücke" Punkt 1 direkt um — ein unbekannter/erfundener Tag darf
   nicht still entstehen. Ein `TagId` ohne Treffer liefert denselben `invalid_reference`-Fehlercode, den
   der Supervisor-Endpunkt schon für fehlende Referenzen nutzt; ein neuer `ApiErrors`-Code ist nicht nötig.
   Kosten: eine eigene, schlankere Resolver-Logik statt Wiederverwendung von
   `ChildInterestsController.ResolveAsync`.

## Akzeptanzkriterien

1. Ein eingeloggtes Kind kann seine Themen- und Stil-Interessen **selbst** setzen, gewichten und entfernen
   (`PUT student/me/interests`); ohne gültigen Kind-Token liefert der Endpunkt `401`/`403`.
2. Das Kind kann dabei **keine** neue `InterestTag`-Zeile erzeugen — ein `TagId` ohne Treffer in der
   bestehenden Taxonomie liefert `invalid_reference`, nicht ein stilles Anlegen (Entscheidung 9).
3. Der geführte Prozess (Kachel-Mehrfachauswahl, Entscheidung 8) ist ohne Tippen bedienbar: ein erneuter
   Besuch zeigt den zuletzt gespeicherten Stand (fortsetzbar), ein Verlassen ohne zu speichern ändert
   nichts (abbrechbar). Am Ende stehen gewichtete Einträge; mindestens eine Abneigung (negatives Gewicht)
   ist wählbar, aber nicht erzwungen.
4. Eine Profiländerung ändert **keine bereits eingefrorene** Bildwahl — ein Test hält fest, dass eine
   bestehende `ChildMediaPick`-Zeile nach einer Interessenänderung unverändert bleibt.
5. Der Supervisor sieht die vom Kind gesetzten Interessen weiterhin an der gewohnten Stelle
   (`supervisor/children/{id}/interests`, `VaterKind.tsx`) und kann sie ändern — dieser Pfad besteht schon
   und bleibt unangetastet.
6. Ein optionaler, kurzer Freitext-Wunsch („Sonstiges: …") landet über einen eigenen Endpunkt lesbar in
   `Child.Interests`; Länge und Listengröße sind serverseitig gedeckelt (Entscheidung 6).
7. Der geführte Prozess wird beim ersten Login ohne vorhandene Interessen automatisch angeboten und ist
   danach dauerhaft über einen Menüpunkt in der Sohn-App erreichbar (Entscheidung 7).
8. Die Produktbeschreibung der Sohn-App ist um die Ausnahme ergänzt („steuert nichts" gilt für Inhalte und
   Pflicht, nicht für die eigene Interessen-Beschreibung).

## Schätzung

**Größe: L** — kein Schema-Umbau (die Taxonomie und `ChildInterest` existieren bereits), aber eine
zusammenhängende Änderung über drei Backend-Projekte (`Pugling.Contracts`, `Pugling.Api`,
`Pugling.Client`) **plus** ein neuer, mehrteiliger geführter Screen in der Sohn-App inklusive
Erstlogin-Erkennung, Navigation und Doku-Satz — vergleichbar mit einer DB-Umbau-Etappe wie E6 an
Aufwand, nur ohne deren Migrations-Sonderfälle. Kein Split nötig: es ist **ein** zusammenhängender
vertikaler Schnitt (ein neuer Endpunkt-Satz, ein neuer Screen), keine Bündel-Story.

**`wo`: beides** (Backend zuerst, dann Frontend) · **`migration`: nein** (keine neue Entität/Spalte —
Entscheidung 7 vermeidet bewusst ein neues Zeitstempel-Feld) · **`vertragsbruch`: nein** (nur additive
neue DTOs/Endpunkte; bestehende Verträge ändern sich nicht).

**Risiken:**

- Der Freitext-Wunsch mutiert `Child.Interests`, bisher nur über `UpdateChildDto`/den Supervisor
  beschreibbar — der neue Schreibpfad braucht eine eigene Länge-/Mengenbegrenzung (Entscheidung 6), sonst
  wächst die Liste unbegrenzt.
- `ChildInterestResponse`/`ChildInterestInput`/`SetChildInterestsDto` liegen im Namespace
  `Pugling.Contracts.Supervisor`, werden aber jetzt konzeptionell auch vom Student-Weg gebraucht —
  technisch sichtbar per projektweitem `<Using>`, semantisch aber unsauber. Der Entwickler entscheidet beim
  Bauen, ob eigene, schmalere Student-DTOs (empfohlen, siehe Angriffsplan) oder eine Verschiebung nach
  `Common` sauberer ist; kein Blocker.
- Das Erstlogin-Kriterium „keine Interessen vorhanden" (Entscheidung 7) triggert erneut, wenn ein Kind
  bewusst „nichts davon" wählt und daher leer bleibt. Ein lokal (`localStorage`) gemerktes
  „schon gesehen"-Flag verhindert das, ohne ein neues Server-Feld zu brauchen.
- Der **Endpunkt-Abdeckungs-Wächter** verlangt für jede neue Controller-Action einen aufrufenden Test mit
  Status < 400 — bei zwei neuen Controllern (Kind-Interessen, Taxonomie-Lesezugriff) nicht vergessen.
- Kachel-Vereinfachung (Entscheidung 8) ist ein bewusster Komfort-Abstrich gegenüber der ursprünglichen
  Notiz — falls Kinder das UI nicht gut annehmen, ist Nacharbeit eine eigene, kleine Folge-Story, kein
  Rückbau der Datenschicht.

**Angriffsplan** (Backend zuerst):

1. `Pugling.Contracts`: schmale neue DTOs für den Kind-Schreibweg (`Student/InterestDtos.cs`) —
   `MyInterestInput(int TagId, int Weight)`, `SetMyInterestsDto(List<MyInterestInput> Interests)`,
   `AddInterestWishDto(string Text)`, dazu eine schlanke `InterestTagOptionResponse(int Id, string Slug,
   string Label, InterestFacet Facet)` für die Taxonomie ohne Nutzungszähler (die verrieten sonst
   Populationsgröße anderer Kinder).
2. `Pugling.Api`: `Controllers/Student/MyInterestsController.cs`
   (`student/me/interests`, `[Authorize(Roles = Roles.Student)]`, `User.ChildId()`) mit `GET` (eigene
   gewichtete Interessen), `PUT` (Vollersatz, Auflösung **nur** über existierende `TagId`, siehe
   Entscheidung 9) und `POST .../wish` (Länge-/Mengendeckel auf `Child.Interests`); dazu
   `GET student/interest-tags` (schlanke, kind-lesbare Taxonomie-Liste, global, kein Ownership-Filter
   nötig).
3. `Pugling.Client`: je eine Zeile für die drei neuen Aufrufe (kein neues HTTP-Plumbing).
4. Backend-Tests: neue Klasse nach Vorbild `InterestTaxonomyTests.cs` — Ownership (403 ohne Kind-Token),
   Tag-Erzeugung blockiert (`invalid_reference` statt stillem Anlegen), Gewichtsgrenzen, PUT-Rundlauf,
   Freitext-Deckel, sowie ein Test, der eine eingefrorene `ChildMediaPick` nach einer Interessenänderung
   unverändert vorfindet (AC4).
5. Frontend: neue Route/Komponente `src/sohn/SohnInterests.tsx` — Kachel-Mehrfachauswahl
   (`aria-pressed` statt `role="tab"`, Konvention aus `frontend-a11y`), `useAction` + `StatusBanner` fürs
   Speichern, `disabled={busy}` am Speichern-Knopf, Freitextfeld mit `<label htmlFor>`, Erstlogin-Erkennung
   (leerer Bestand → Hinweis) plus dauerhafter Menüpunkt in `SohnKonto.tsx`/`SohnApp.tsx`-Navigation.
6. Doku: `sohn-app-funktionsbeschreibung.md` Abschnitt 1 um die Ausnahme ergänzen.

**Testweg:** neue Backend-Testklasse (Schritt 4 oben, Vorbild `InterestTaxonomyTests.cs`); ein
Komponententest für `SohnInterests.tsx` (React Testing Library: Kachel togglet `aria-pressed`, Speichern
ruft die PUT-Route); ein neuer, schlanker `frontend/e2e/interessen.spec.ts` deckt den Durchstich „Kind setzt
Interesse → Vater sieht es in `VaterKind.tsx`" ab (Vorbild `full-flow.spec.ts`). `/smoke-test` als manueller
Gegencheck vor dem Commit.

## Bau (2026-09-03)

**Backend** (API-First, zuerst):

- `Pugling.Contracts/Student/InterestDtos.cs` — `InterestTagOptionResponse`, `MyInterestInput`,
  `SetMyInterestsDto`, `AddInterestWishDto`, `MyInterestResponse`, `MyInterestProfileResponse`.
  Eigene, schmale Student-DTOs statt einer Verschiebung nach `Common` (offen gelassenes Risiko der
  Schätzung): `MyInterestInput` trägt **nur** `TagId` — der Kind-Vertrag kennt gar keinen Textweg,
  über den ein Tag entstehen könnte.
- `Controllers/Student/MyInterestsController.cs` (`student/me/interests`, `Roles.Student`,
  `User.ChildId()`): `GET` (Profil), `PUT` (Vollersatz, Auflösung nur über bestehende `TagId`),
  `POST wishes` (Freitext, gedeckelt auf 60 Zeichen / 20 Einträge).
- `Controllers/Student/InterestOptionsController.cs` (`student/interest-tags`): die kind-lesbare
  Auswahlliste, gepaginiert, mit `facet`-Filter, **ohne** Nutzungszähler.
- `ApiErrors.InterestWishLimitReached` (`interest_wish_limit_reached`, 409) — neu, additiv.
- `Pugling.Client/StudentApi.cs` — drei Zeilen für die drei Aufrufe.
- `EndpointCoverageGuard.FullRunTouchedActions` 263 → **267** (die vier neuen Actions sind abgedeckt).

**Frontend**:

- `src/sohn/SohnInterests.tsx` — der geführte Prozess in vier Schritten (Kachel-Mehrfachauswahl,
  `aria-pressed`, `useAction` + `StatusBanner`, `disabled={busy}`), dazu `InterestIntroCard` als
  Erstbesuch-Angebot auf der Basis.
- Route `/sohn/ich` (`SohnApp.tsx`), dauerhafter Menüpunkt in `SohnKonto.tsx`, Angebot in `SohnHome.tsx`.
- `src/lib/api.ts` + `types.ts` — drei Aufrufe, vier Alias-Zeilen aus dem neu erzeugten Vertrag.

**Doku**: `docs/sohn-app-funktionsbeschreibung.md` — die Ausnahme zu „steuert nichts" (Abschnitt 1),
neuer Abschnitt 4.10, Navigationsmodell und Checkliste (AK 8).

### Zwei Abweichungen vom Angriffsplan, beide bewusst

1. **Der Freitext-Wunsch liegt unter `POST me/interests/wishes`**, nicht `…/wish`. Plural, weil die
   Ressource eine Liste ist.
2. **Die Erfolgsmeldung ist die Feier der Shell, nicht der `StatusBanner` des Bildschirms.** Der erste
   E2E-Lauf fiel genau daran: der Bildschirm springt nach dem Speichern zurück zur Basis und nimmt seinen
   Banner mit — die Fehlerklasse aus [B-151](B-151-gespeichert-banner-verschwindet-mit-dem-formular.md).
   Die Bestätigung wandert damit auf eine Ebene, die den Sprung überlebt; der `StatusBanner` bleibt für
   den Fehlerfall, in dem der Bildschirm ja stehen bleibt.

### Rote Proben (wo sie fielen, nicht ob)

| Probe | Erwartete Fundstelle | Gemessen |
|---|---|---|
| Referenzprüfung aus `MyInterestsController.Replace` entfernen | `invalid_reference`-Zusicherung | `KindInteressenTests.cs:105` ✓ |
| „Die eingefrorene Wahl gewinnt" aus `MediaSelector` entfernen | AK-4-Zusicherung auf das unveränderte Bild | `KindInteressenTests.cs:279` ✓ |

### Review-Runde (2026-09-03)

Beide Reviewer gelaufen, **elf Funde angenommen, einer abgelehnt, einer abgetrennt.** Die zwei wichtigsten
Änderungen daran:

**Die Bestätigung war zweimal am falschen Ort.** Der `frontend-reviewer` hat gezeigt, dass die Feier der
Shell den `StatusBanner` nicht ersetzen kann: `CelebrationLayer` rendert `aria-hidden="true"`,
[index.css](../../frontend/src/index.css) blendet `.cel-layer` bei „Bewegung reduzieren" **ganz** aus, und
sie räumt sich nach 1,5 s selbst ab. Für ein Kind mit reduzierter Bewegung gab es nach „Fertig" also gar
keine Rückmeldung — [B-151](B-151-gespeichert-banner-verschwindet-mit-dem-formular.md) im neuen Mantel
statt seiner Vermeidung. Neu: eine **Meldungsstelle in der Shell**
([SavedNotice.tsx](../../frontend/src/components/SavedNotice.tsx)), die die Meldung über
`navigate(…, { state: { saved } })` entgegennimmt, sie in eine bereits vorhandene Live-Region wachsen lässt
und den Verlaufseintrag danach räumt. Die Feier bleibt — als Schmuck, nicht als Beleg. Der E2E fährt
seither unter `reducedMotion: "reduce"` und sichert zu, dass die Bestätigung **nicht** unter
`aria-hidden` liegt.

**Ein Fund war echt, seine Reparatur nicht.** Der `pugling-reviewer` hat zu Recht gemeldet, dass die
Kind-Auswahlliste jedes je getippte Stichwort zeigt — auch Freitext fremder Familien und tote Stichwörter,
die nichts einkleiden können. Sein Vorschlag (`Where(MediaLinks.Count > 0 || ExerciseLinks.Count > 0)`)
ist hier aber **nicht anwendbar**: `Data/Seed.cs` legt kein einziges `MediaAsset` an, die Liste wäre im
Auslieferungszustand leer und das Feature aus. Der Fund läuft darum als eigene Story
[B-207](B-207-kind-auswahlliste-ist-unkuratiert.md) weiter — B-50s Ziel ist ohne sie erfüllt, und *welche*
Achse kuratiert, ist eine Produktentscheidung.

Angenommen und behoben, mit je einem Test, der die Änderung rot macht:

| Fund | Behoben durch |
|---|---|
| Auswahlliste sortiert nach `Label` — SQLite vergleicht byteweise, „Zebra" vor „apfel" | `OrderBy(Slug).ThenBy(Id)` wie beim Creator; Test mit zwei Stichwörtern |
| `AddWish` bumpt `Child.ConcurrencyStamp` nicht → verlorenes Update als 200, Kollision als 500 | Stamp-Bump + `TrySaveAsync` → `concurrency_conflict`; Test auf den Stempel |
| `GroupBy` gegen den Doppel-Tag war ungetestet | Test mit zweimal demselben `tagId` |
| Gewichts-Grenzen nur oben geprüft | beide Enden und beide akzeptierten Ränder |
| `Assert.False(after.Rejected)` prüft den Ausgangszustand, kann nie fallen | gestrichen |
| `Replace` lädt das Kind nie → FK-Verletzung als 500 statt 403 | Existenzprüfung wie in `AddWish` |
| 409-Text verlangt vom Kind eine Handlung, die sein Vertrag nicht anbietet | „ask a grown-up to remove one" |
| Drei deutsche Inseln in der Code-Doku der Testklasse | übersetzt |
| Kachel widerspricht sich bei den Zwischengewichten `±1`, die der Vater setzen kann | Text aus dem **Vorzeichen** statt aus der Gleichheit — bewusst **ohne** Normalisierung, die schriebe die feinere Angabe des Vaters still um; zwei Tests |
| AK 7 (Erstbesuch-Angebot) hatte null Abdeckung | E2E fährt jetzt ein **eigenes** Kind: Angebot → Prozess → eingelöst |
| Schrittwechsel wurde nicht angesagt; `markIntroSeen` vor dem Laden; fehlende `textDecoration`; ungenutzter `facet`-Parameter und `MyInterest`-Alias; redundanter Cast | je behoben |
| Wunsch-Fehler nach erfolgreichem PUT meldete nur den Wunsch | Teilerfolg wird benannt („Deine Auswahl ist gespeichert – nur … ") |

### Was offen bleibt

- **Nichts für diese Story.** Beide Reviewer sind gelaufen, ihre Funde sind behoben oder als
  [B-207](B-207-kind-auswahlliste-ist-unkuratiert.md) abgetrennt.
- `/smoke-test` als manueller Gegencheck ist **nicht** gelaufen; der E2E fährt denselben Weg gegen einen
  echten Server und deckt ihn ab — das ist eine Ersetzung, keine Auslassung, aber sie sei benannt.

## Verlauf

- **2026-08-01** — angelegt und direkt ausformuliert (Quelle: Rollen-Durchgang; Kernentscheidung „das Kind
  gibt seine Interessen selbst an" vom Nutzer). Ist-Stand am Code belegt; die drei Fragen zu Geschlecht,
  Freigabe und Änderungs-Deckel bleiben für die Grill-Runde.
- **2026-08-04** — gegrillt: alle sieben offenen Punkte in nummerierte Entscheidungen überführt, dazu zwei
  zusätzliche Entscheidungen aus der Recherche (Routendesign `student/me/…` statt `{childId}`-Pfad,
  vereinfachte Kachel-Mechanik statt Paarvergleichs-Algorithmus, um die Story unter `L` zu halten); Ist-Stand
  gegen den Code vom 2026-08-04 erneut geprüft (alle Belege stimmen weiter, keine Korrektur nötig, zwei
  Belege ergänzt: `ResolveAsync`-Zeilen und das `student/me/…`-Routenmuster); Akzeptanzkriterien final —
  autonom getroffen, Nutzerauftrag.
- **2026-08-04** — geschätzt: `groesse: L`, `wo: beides`, `migration: nein`, `vertragsbruch: nein`,
  Risiken, Angriffsplan (Backend zuerst) und Testweg festgelegt; kein XL-Split nötig — autonom getroffen,
  Nutzerauftrag.
- **2026-09-03** — gebaut, alle acht Akzeptanzkriterien erfüllt; Stand siehe „Bau" unten. Belegt: Backend
  **863/863 grün** (`dotnet test Pugling.sln -c Release`), davon 11 neue in `KindInteressenTests`;
  Frontend **339/339** Vitest (11 neue in `SohnInterests.test.tsx`); neuer E2E `interessen.spec.ts` grün
  („1 passed"), der zugleich den **Rollengang** fährt (Kind → Vater am laufenden Server). Zwei rote Proben
  gemessen, je an der richtigen Zusicherung. **Bleibt auf `in-arbeit`**: die Eintrittsbedingung von
  `abgenommen` verlangt `pugling-reviewer` und `frontend-reviewer`, und diese Sitzung trägt die Regel,
  keine Agenten unaufgefordert zu starten — Freigabe steht beim Nutzer aus (`wartet_auf`).
  Commit `0f5acb3` (nicht gepusht).
- **2026-09-03** — `pugling-reviewer` und `frontend-reviewer` gelaufen (Nutzerfreigabe). Elf Funde
  angenommen und behoben, einer als [B-207](B-207-kind-auswahlliste-ist-unkuratiert.md) abgetrennt (die
  vorgeschlagene Reparatur hätte die Auswahlliste im Auslieferungszustand geleert), keiner offen. Der
  schwerste: die Erfolgsmeldung lag in der Feier der Shell, die `aria-hidden` ist und bei „Bewegung
  reduzieren" ganz verschwindet — für dieses Kind gab es nach „Fertig" **keine** Rückmeldung. Neu belegt:
  Backend **865/865**, Vitest **341/341**, E2E **47/47** (der eigene Lauf jetzt unter
  `reducedMotion: "reduce"` und mit eigenem Kind, womit auch AK 7 abgedeckt ist), `dotnet format` sauber.
  Drei weitere rote Proben gemessen, je an der eigenen Zusicherung (Sortierung, Doppel-Tag, Stempel).
  Commit `827b220`.
- **2026-09-03** — **abgenommen.** Die Eintrittsbedingung ist vollständig belegt: echte Testzahlen
  (865 Backend / 341 Vitest / 47 E2E), beide Reviewer gelaufen und ihre Funde behoben bzw. als
  [B-207](B-207-kind-auswahlliste-ist-unkuratiert.md) abgetrennt, Commits `0f5acb3`, `87ef55e`, `827b220`
  (nicht gepusht). **Rollengang:** `frontend/e2e/interessen.spec.ts` fährt den Weg Kind → Vater in einem
  echten Browser gegen einen echten Server — vom Erstbesuch-Angebot über den geführten Prozess bis zur
  Anzeige beim Vater; nach der README-Regel zählt das als Rollengang und ist ihm hier überlegen, weil es
  sich wiederholt. Nicht abgedeckt und ausdrücklich benannt: das sinnliche Urteil („fühlt sich die
  Kachel-Mechanik für ein Kind richtig an") — dafür ist `pm-loop` der Ort, nicht ein Test.
