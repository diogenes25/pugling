---
tags: [typ/story, status/in-arbeit, bereich/backend, bereich/frontend, rolle/creator]
aliases: [unloeschbare Art, Grammtik bleibt fuer immer, B-157 nahm eine Faehigkeit weg]
status: in-arbeit
prio: P1
art: Defekt
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: Nachschau 2026-08-13 zu B-157 und B-154
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-157]
wartet_auf: pugling-reviewer + frontend-reviewer
---

# B-170 · Eine selbst angelegte Art im Grundbestands-Fach ist unlöschbar — und der Seitentext verspricht das Gegenteil

B-157 hat das Eigentum am Fach auf seine Arten ausgedehnt und das **Anlegen** bewusst frei gelassen. Beides
ist begründet. Ihre Schnittmenge hat niemand gewogen: Wer eine Art in ein Fach ohne Eigentümer legt — und
das sind für einen neuen Nutzer **alle vier** Fächer —, kann sie danach **nie wieder** entfernen. Niemand
kann es.

**Vor `3b65475` war genau dieses `DELETE` ein `204`.** Die Story hat also im Namen des Schutzes eine
Fähigkeit entfernt, die ein normaler Nutzer für seine einzigen Fächer braucht.

## User Story

Als *Creator* (in der Praxis der Vater, der seinen eigenen Katalog ordnet) möchte ich eine Übungsart, die
**ich** angelegt habe, wieder umbenennen und entfernen können — auch in einem Fach aus dem Grundbestand,
denn das sind für einen neuen Nutzer die einzigen Fächer, die er hat —, damit ein Tippfehler nicht dauerhaft
in der Vorfilterung des Planbaus stehen bleibt.

## Ist-Stand am Code (selbst nachgeprüft)

| Schritt | Ergebnis | Fundort |
|---|---|---|
| Art in ein Fach ohne Eigentümer anlegen | **201**, ohne Prüfung (Absicht, Entscheidung 2) | `ExerciseCategoriesController.cs:73-78` |
| Sie umbenennen | **403 `not_owner`** | `ExerciseCategoriesController.cs:111` |
| Sie löschen | **403 `not_owner`** | `ExerciseCategoriesController.cs:143` |
| Das Fach löschen (Fluchtweg) | **403** | `SubjectsController.cs:120-123` |
| Als Admin eingreifen | **kein Ventil** — `grep IsAdmin` in beiden Controllern: nichts | — |

Ein zweiter Schreibpfad auf `ExerciseCategories` existiert nicht. Beide Hälften sind je getestet
(`FachEigentumTests.cs:194` pinnt das freie Anlegen, `:215` das Gesperrtsein für jeden) — nur die
Schnittmenge nicht, darum blieb die Suite grün.

**Der Kontrast ist im Repo belegt:** `ExerciseControllerBase.cs:117,126` reicht `User.IsAdmin()` bewusst
durch, ausdrücklich „to edit orphaned (ownerless) exercises in an emergency". Die Übung hat das Ventil, die
Art nicht.

## Fehlerszenario

Auf `/vater/katalog` das Fach „Englisch" wählen und bei „Neue Art" **„Grammtik"** tippen (Tippfehler).
Ein Klick, `201`, Banner „Art angelegt." — und „Grammtik" steht ab jetzt **dauerhaft** in der
Vorfilter-Liste, an der der Planbau hängt (`VaterWizard.tsx:133`, `ExerciseFilterBar.tsx:44`).
Reparierbar nur durch Wegwerfen der Datenbank.

## Die zweite Hälfte: der Bildschirm widerspricht sich zwei Absätze weit

`frontend/src/vater/VaterKatalog.tsx:26-27` sagt:

> Fächer und Arten sind der **gemeinsame** Rahmen aller Übungen – auch der von anderen Vätern. Hier legst du
> neue an; umbenennen und löschen kannst du, was du selbst angelegt hast.

`frontend/src/vater/CatalogAdmin.tsx:183-184` sagt zwei Absätze darunter:

> Diese Arten gehören zum Fach „Englisch" aus dem Grundbestand – ergänzen darfst du sie, umbenennen und
> löschen kann sie **niemand**.

Die Karte hat recht, der Vorspann nicht. **Attribution, ehrlich:** Der Satz stammt aus B-154 und war dort
nur *zu streng* (Arten waren damals bedingungslos änderbar) — also harmlos. Falsch **in der gefährlichen
Richtung** (Versprechen > Server) wurde er mit B-157, und B-157 hat `VaterKatalog.tsx` nicht angefasst
(`git show 3b65475 --stat`).

Und der bittere Teil: der Kommentar direkt über diesem Satz (`VaterKatalog.tsx:22-24`) begründet ihn damit,
dass im Rollengang zu B-154 „eine Zeile über der Karte weiter etwas versprach, was die Karte darunter
verweigert" — der Satz wurde also *gegen genau diese Fehlerklasse* geschrieben und ist ihr wieder verfallen.
Kein Test deckt Vorspann-Prosa, und ein Rollengang liest sie als „stimmt ja".

## Offene Punkte

1. ~~Wie darf man aufräumen?~~ → Entscheidungen 4 und 5 (Grill-Runde 2026-09-02). Entscheidung 1 hatte den Punkt
   ausdrücklich offen gelassen, bis die Schemafrage am Tag beantwortet ist — sie ist es jetzt.
2. ~~Oder erst nur ein Ventil?~~ → Entscheidung 2 (in eine eigene Story ausgelagert).
3. ~~Was gilt für eine benutzte Art?~~ → Entscheidung 3.
4. ~~Wird der Vorspann-Satz mitrepariert?~~ → Entscheidung 2. Die Empfehlung „hier mit" ist **entfallen**;
   ihre Begründung hing an Punkt 1. **Und kommt mit Entscheidung 7 zurück:** Punkt 1 ist jetzt da, also
   ändert sich wieder, was wahr ist — B-178 hat den Satz auf die *heutige* Wahrheit gesetzt, diese Story
   setzt ihn auf die von morgen. Zweimal geschrieben, wie in Entscheidung 2 vorhergesagt, und das war der
   Preis der Teilung.

## Entscheidungen

1. **Der eigentliche Fix bleibt `ExerciseCategory.CreatedByAdultId` — und wird am Tag entschieden, nicht
   nachts.** Vom Nutzer so entschieden (Nachtlauf 2026-08-14): Die Migrationskette wird bei jeder
   Schemaänderung **neu gefaltet**, `SchemaGuardTests` erzwingt Länge 1, und der Snapshot-Diff *ist* die
   Abnahme — die kann unbeaufsichtigt niemand beurteilen. *Kosten:* Der Vater kann seinen Tippfehler bis
   dahin nicht wegräumen. Das ist der Preis, und er ist ausgesprochen statt weggelassen.
2. **Diese Story wird geteilt.** Was heute Nacht baubar ist, liegt in
   [B-178](B-178-katalogtext-verspricht-mehr-als-der-server-erlaubt.md): das Admin-Ventil (vom Nutzer
   freigegeben) **und** der Vorspann-Satz, der die *heutige* Wahrheit sagen muss.
   **Begründung, und sie widerspricht der ursprünglichen Empfehlung dieser Story:** Punkt 4 empfahl, den Satz
   hier mitzunehmen, „denn nach Punkt 1 ändert sich, was wahr ist". Punkt 1 kommt aber nicht — also ändert
   sich für den Vater **nichts**, und der Satz muss etwas anderes sagen als nach dem Schema-Fix. Ihn hier zu
   lassen hieße, ihn zweimal zu schreiben. *Kosten:* Zwei Stories über einen Zustand; der Zusammenhang hängt
   an den Querverweisen. Dafür schreibt keine der beiden eine Wahrheit hin, die gerade nicht gilt.
3. **Eine benutzte Art bleibt unverändert.** Löschen nimmt der Übung nur die Zuordnung
   (`ExerciseCategoriesController.cs:128-131`), das ist entschieden und seit B-171 auch tragend getestet.
   *Kosten:* keine — die Regel wird von diesem Vorhaben nicht berührt.

4. **Die Spalte heißt `ExerciseCategory.OwnerAdultId` — nicht `CreatedByAdultId`.** Die Regel ist Wort für
   Wort die von `Subject` und `TextbookSeries` („jeder darf lesen und benutzen, nur einer darf umbenennen
   und löschen"), und für die gibt es in diesem Projekt schon einen Namen. Neben `OwnerAdultId`,
   `Exercise.AuthorAdultId` (Autorschaft) und `ExerciseGrant.CreatorId` (Rechtezeile) wäre
   `CreatedByAdultId` das **vierte** Wort für dieselbe Sache; dieses Repo hat für Begriffsdrift schon
   bezahlt (`Father`→`Adult`, Lernziel→`KeyResult`). *Kosten:* Eine zweite Eigentumszeile im Katalog —
   genau das, wogegen `LearnEntities.cs:22-24` **vorab argumentiert** („a second, laxer reading of
   *ownerless* would be a third ownership semantics inside one catalog"). Dieser Kommentar ist damit
   widerlegt und muss beim Bauen umgeschrieben werden, nicht stehen bleiben: Die Lesart wird nicht laxer,
   das Recht bekommt einen **zweiten Träger**. Dazu Schemaänderung, also Kette neu falten. Ein Nachtrag für
   bestehende DBs ist nicht nötig und auch nicht möglich — `backend/Pugling.Api/CLAUDE.md:22-23`: eine DB
   aus einer alten Kette wird beim Start abgewiesen, „ein Upgrade-Pfad existiert bewusst nicht". Der
   Tippfehler aus dem Fehlerszenario geht mit der alten DB.
5. **Reichweite: jedes Fach, ohne `null`-Sonderfall.** Wer eine Art angelegt hat, darf sie überall wieder
   entfernen — auch aus einem Fach, das ein anderer Creator besitzt; und der Fach-Eigentümer darf
   weiterhin jede Art in seinem Fach entfernen, auch eine fremde. Begründung: Anlegen ist überall frei
   (B-157 Entscheidung 2), also ist „entferne, was du hinzugefügt hast" die symmetrische Regel — eine
   Bedingung statt eines `OwnerAdultId is null`-Zweigs, der erklären müsste, warum die laxe Lesart bei der
   Art richtig und beim Fach falsch ist. *Kosten:* Ein fremder Creator kann eine Art aus meinem Fach
   entfernen. Begrenzt durch Entscheidung 3 (Löschen nimmt der Übung nur die Zuordnung) und dadurch, dass
   es nur eine Art sein kann, die er selbst dort hineingelegt hat.
6. **Das Vertragsfeld heißt `CategoryResponse.IsOwn`, nicht `isMine`.** [B-156](B-156-ismine-heisst-anderswo-isown.md)
   hat 7:1 auf `IsOwn` vermessen, und dort trägt `IsOwn` schon die Bedeutung „darf schreiben (Owner oder
   Grant)" — genau die, die eine Art hier braucht, weil das Recht zwei Träger hat. `isMine` würde ein
   neuntes DTO zum Ausreißer machen und B-156 Arbeit hinzufügen. *Kosten:* ~~Änderung an
   `Pugling.Contracts`, also `vertragsbruch`~~ — **beim Schätzen nachgesehen und berichtigt:** ein
   **additives** Feld ist laut Größen-Anker im README kein Bruch, und das ist hier auch praktisch so.
   `CategoryResponse` wird an genau **drei** Stellen gebaut, alle in `ExerciseCategoriesController.cs`
   (`:47`, `:59`, `:94`); `Pugling.Client` liest es nur (`CreatorApi.cs:51-56`), das Frontend über den
   erzeugten Typ (`types.ts:375`), und `unknown_field` gilt für **Anfragen**, nicht für Antworten. Es
   bleibt also eine Pflichtaufgabe (positional record ⇒ alle drei Bauplätze ziehen nach) und ein Nachzug
   in `contract.ts`, aber kein Vertragsbruch. Nicht gefragt, sondern aus B-156 abgeleitet — widerspricht
   das, ist B-156 die Story, die es entscheidet, nicht diese.
7. **Drei Texte ziehen nach, und B-154s Satz wird wieder wahr.** Der Vorspann sagt wieder, dass umbenennen
   und löschen darf, wer angelegt hat — nur nennt er jetzt **beide** Träger (Fach *oder* Art). Der
   Kartensatz „löschen kann sie **niemand**" fällt weg, denn er wird falsch. Pro Zeile: Knöpfe da oder
   nicht, wie heute — kein deaktivierter Knopf mit `title`, der erscheint nicht
   ([B-160](B-160-gesperrter-knopf-nennt-den-grund-nie.md)). Begründung: Die Löschbarkeit variiert ab jetzt
   **pro Zeile**; ein Satz über die ganze Liste kann das nicht mehr sagen, und „gar kein Satz" hieße, den
   Nutzer die Regel erraten zu lassen — die Klasse, gegen die `VaterKatalog.tsx:20-24` geschrieben wurde.
   *Kosten:* Der Vorspann trägt damit seine vierte Fassung, und die begründenden Kommentare in beiden
   Dateien müssen mit, sonst erzählen sie eine Geschichte, die zwei Stories zurückliegt.
8. **Umbenennen folgt löschen** (nicht gefragt, Präzedenz): Beide Wege liefern heute `403`
   (`ExerciseCategoriesController.cs:124`, `:157`); eine Story, die nur einen befreit, hinterlässt eine
   Art, die man wegwerfen aber nicht korrigieren kann — bei einem *Tippfehler* die falsche Hälfte.
   *Kosten:* keine, es ist dieselbe Bedingung an zwei Stellen.
9. **Stirbt der Eigentümer-`Adult`, wird die Spalte `null`** (FK → `SetNull`, nicht gefragt, Präzedenz):
   genau wie bei `Subject` und `TextbookSeries` (`LearnEntities.cs:25-27`). Folge: Die Art fällt in den
   Zustand „nur Fach-Eigentümer oder Admin" zurück, statt mit dem Konto zu verschwinden — die Fächer und
   Pläne aller anderen behalten ihre Achse. *Kosten:* Eine verwaiste Art im Grundbestand ist wieder nur
   über das Admin-Ventil wegzuräumen. Das ist die vorhandene Semantik, keine neue.

## Was diese Story nach der Teilung noch ist

Genau eine Frage: **Darf eine selbst angelegte Art in einem Fach ohne Eigentümer von ihrem Anleger entfernt
werden, und trägt die DB dafür eine Spalte?** Alles andere ist in B-178 abgeflossen. Der Ist-Stand oben
bleibt gültig und ist der Beleg dafür, dass die Frage nicht akademisch ist: ein Tippfehler steht heute
dauerhaft in der Planbau-Vorfilterung.

**Beantwortet am 2026-09-02** (Entscheidungen 4–9): ja, sie darf entfernt werden, und ja, die DB trägt
dafür eine Spalte — `ExerciseCategory.OwnerAdultId`. Der Begriff „Anleger" aus der Fragestellung ist dabei
gefallen: das Projekt nennt diese Rolle **Eigentümer** (`OwnerAdultId`), und die Art hat ab jetzt einen —
neben dem des Fachs.

## Akzeptanzkriterien

1. **`ExerciseCategory` trägt `OwnerAdultId` (`int?`, FK → `SetNull`).** `POST` setzt sie auf
   `User.CreatorId()`; die geseedeten Arten bleiben `null`. Der Snapshot-Diff am
   `PuglingDbContextModelSnapshot.cs` zeigt **genau** diese Spalte und ihren Index, sonst nichts.
2. **`DELETE` einer selbst angelegten Art in einem Fach ohne Eigentümer liefert `204`** — der Fall, der vor
   `3b65475` ein `204` war und seither `403` ist. Rote Probe: die Zusicherung fällt, wenn man
   `IsOwnedBy(category.OwnerAdultId, …)` aus der Bedingung nimmt.
3. **`PATCH` derselben Art liefert `200` und den neuen Namen.** Ohne dieses Kriterium bliebe eine Art
   wegwerfbar, aber nicht korrigierbar — bei einem Tippfehler die falsche Hälfte.
4. **`DELETE`/`PATCH` einer *geseedeten* Art liefert weiter `403 not_owner`** für jeden Nicht-Admin, auch
   für den, der gerade eine eigene Art im selben Fach entfernen durfte. Das ist die Zusicherung, die den
   Grundbestand vom Selbstangelegten trennt.
5. **`DELETE` einer *fremden* Art in einem Fach, das mir gehört, liefert weiter `204`** — heutiges
   Verhalten, das die neue Bedingung nicht verlieren darf (Entscheidung 5, erster Halbsatz).
6. **`DELETE` meiner Art in einem Fach, das ein anderer Creator besitzt, liefert `204`** — die Reichweite
   aus Entscheidung 5. Ohne diese Zusicherung wäre nicht unterscheidbar, ob die Regel wie gewollt ohne
   `null`-Sonderfall gebaut wurde.
7. **`CategoryResponse` trägt `IsOwn`** und es ist `true` genau dann, wenn der Aufrufer umbenennen und
   löschen darf (Eigentümer der Art **oder** des Fachs **oder** Admin) — dieselbe Bedeutung wie in
   `ExerciseCatalogDtos`, nicht „ist Eigentümer der Art".
8. **Die Katalogseite zeigt „umbenennen"/„löschen" pro Zeile** genau dann, wenn `isOwn` gilt, und sonst
   **keinen** Knopf — kein deaktivierter mit `title` (der erscheint nicht,
   [B-160](B-160-gesperrter-knopf-nennt-den-grund-nie.md)). In einem Grundbestands-Fach mit einer eigenen
   Art stehen also beide Sorten Zeilen nebeneinander.
9. **Der Vorspann in `VaterKatalog.tsx` nennt beide Träger des Rechts** („wer das Fach **oder** die Art
   angelegt hat") und der Kartensatz „löschen kann sie **niemand**" ist weg. Kein Satz auf der Seite
   verspricht mehr oder weniger als der Server liefert — das ist die Fehlerklasse, an der diese Story
   zweimal hängt.
10. **Das Admin-Ventil aus B-178 bleibt** und sein Kommentar verliert den Halbsatz „so the father still
    cannot clean up his own typo until B-170 decides the schema" — er wäre dann falsch.

## Schätzung

**Größe: M** — verankert am Anker „vokabel-basierter Batch-Pfad im `MediaSelector`" (B-03). Jedes Einzelteil
ist klein und hat sein Vorbild im Repo (`Subject` und `TextbookSeries` führen dieselbe Regel), aber es sind
**fünf Schichten**: Schema, Controller, Vertrag, Backend-Tests, Frontend samt Komponententests.

*Gegen die Nachbarn abgewogen, damit die Größe nicht bloß ein Buchstabe ist:* **nicht S**, weil ein
`describe`-Block im Frontend und eine Test-Vorbedingung im Backend nicht angepasst, sondern **umgebaut**
werden (Risiken 1 und 4). **Nicht L**, weil der L-Anker eine DB-Umbau-Etappe wie E6 ist — dort wurde
bezahltes Inventar *gerettet*; hier gibt es keine Datenwanderung, weil die alte DB beim Falten ohnehin
abgewiesen wird (Entscheidung 4). Zu L kippte es, wenn die geseedeten Arten einen Eigentümer bekommen
müssten oder das Frontend-Tor mehr als diesen einen Verbraucher hätte.

- **`wo: beides`** — Backend zuerst (API-First; das Frontend hängt an `isOwn`). Beide Brillen.
- **`migration: ja`** — `ExerciseCategory` bekommt eine Spalte. Die Kette wird **neu gefaltet**, nicht
  verlängert; sie ist heute genau eine (`20260811194329_InitialCreate`), und `SchemaGuardTests` hält
  Länge 1 plus kein Modell-Drift. Nachgesehen, nicht vermutet.
- **`vertragsbruch: nein`** — additives Antwortfeld; Begründung und die drei Bauplätze stehen in
  Entscheidung 6, wo die frühere Behauptung berichtigt ist.

**Risiken:**

1. **B-178s Admin-Test bricht — an seiner Vorbedingung, die den Defekt festnagelt.**
   `FachEigentumTests.cs:344-348` legt die Art als `adult` an und behauptet dann `403 not_owner` für
   *ebendiesen* `adult`. Unter Entscheidung 5 wird daraus `200`. Der Test darf nicht gelöscht werden — die
   Vorbedingung existiert ausdrücklich, „otherwise the case below could pass for the wrong reason". Sie
   muss auf einen **fremden** Creator umgestellt werden (der besitzt weder Art noch Fach), damit sie weiter
   belegt, dass das Ventil überhaupt gebraucht wird.
2. **Der Snapshot-Diff ist die Abnahme.** Er muss **genau** die Spalte und ihren FK-Index zeigen. Es darf
   kein zweiter DB-Default nachwachsen — die Schema-Konvention lässt genau einen zu
   (`Exercises.ExecutePublic`).
3. **Drei Projektionen brauchen den Aufrufer.** `ProjectOne` (`:44`), `List` (`:58`) und `Create` (`:94`)
   bauen `CategoryResponse` heute **ohne** `fid` und `isAdmin`; `IsOwn` braucht beides. Vorbild ist
   `SubjectsController.cs:32`. Wer nur eine der drei nachzieht, liefert an einer Stelle still `false`.
4. **Im Frontend hängt ein ganzer `describe`-Block an der alten Regel**, nicht nur ein Zweig:
   `CatalogAdmin.test.tsx:110` heißt „CategoryRows – **das Fach** entscheidet, wer die Arten ändern darf"
   und prüft sechs Fälle über `subject.isMine` und `ownerAdultId`. Der Titel wird durch diese Story falsch.
   Der Block ist umzuschreiben, nicht zu ergänzen — und `CategoryRows` ist ausdrücklich exportiert, damit
   der Test die **Bindung** prüfen kann (`CatalogAdmin.tsx:225`); beim Umbau zählt darum die Bindung, nicht
   die Fallzahl.
5. **Der frühe Ausstieg `if (!subject.isMine)` (`CatalogAdmin.tsx:190`) fällt weg.** Er liefert heute die
   ganze Liste nur-lesend; pro Zeile zu entscheiden heißt, diesen Zweig aufzulösen. Dabei darf **kein**
   deaktivierter Knopf mit `title` entstehen — das wäre
   [B-160](B-160-gesperrter-knopf-nennt-den-grund-nie.md) nachgebaut (Entscheidung 7).
6. **`DocsCaptureTests` schreibt `docs/api-examples` bei jedem Lauf neu** und fängt die Art-Endpunkte
   (`:326`, `:328`). Das zusätzliche Feld ändert die mitgeschnittene JSON — erwartet, aber der Diff gehört
   angesehen, nicht überflogen.

**Angriffsplan** (Backend zuerst, in dieser Reihenfolge):

1. `ExerciseCategory.OwnerAdultId` samt Navigation, FK → `SetNull`; Kette neu falten, Snapshot-Diff prüfen.
2. `Create` setzt den Eigentümer; `Update` und `Delete` bekommen die eine erweiterte Bedingung ohne
   `null`-Sonderfall (Entscheidung 5).
3. `CategoryResponse.IsOwn` plus die drei Bauplätze mit `fid` und `isAdmin`.
4. Backend-Tests: Risiko 1 umbauen, die vier neuen Fälle (AK 2, 3, 5, 6) ergänzen, AK 4 als **unverändert
   grün** nachweisen. Den Kommentar von B-178s Ventil um seinen dann falschen Halbsatz kürzen (AK 10).
5. Vertragsdokument und `contract.ts` neu erzeugen, den `DocsCaptureTests`-Diff ansehen.
6. Frontend: `CategoryRows` pro Zeile, der `describe`-Block neu, die drei Texte (AK 8, 9).
7. Rollengang auf `/vater/katalog`: eine eigene Art in „Englisch" anlegen, umbenennen, löschen — und
   danach prüfen, dass die geseedeten Arten weiter keine Knöpfe tragen.

**Testweg** (benannt, nicht „wird getestet"):

- `backend/Pugling.Api.Tests/FachEigentumTests.cs` — trägt schon alle Nachbarfälle; hier kommen AK 2, 3, 5
  und 6 hinzu, Risiko 1 wird umgebaut, AK 4 bleibt unverändert.
- `backend/Pugling.Api.Tests/SchemaGuardTests.cs` — hält Kettenlänge 1 und kein Modell-Drift; der
  Snapshot-Diff ist der Beleg für AK 1.
- `frontend/src/vater/CatalogAdmin.test.tsx` — der `describe`-Block aus Risiko 4, neu gefasst auf `isOwn`.
- **Rollengang** an der laufenden App (Schritt 7). Eine E2E wäre die bessere Wahl, ist hier aber offen: es
  gibt heute keine Spec, die die Katalog-Arten bedient. Ob eine entsteht, entscheidet sich beim Bauen — als
  Vorschlag, nicht als stille Auslassung.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-157 (Server-Hälfte) und B-154 (Text-Hälfte); beide
  Hälften haben eine Wurzel und darum eine Story. Von mir gegengeprüft: `Create` ist ungegated, `Update` und
  `Delete` liefern `403`, es gibt kein `IsAdmin` in beiden Controllern, und die zwei Sätze widersprechen
  sich wörtlich.
- 2026-08-14 · `ausformuliert` bleibt, **Story geteilt** (Nachtlauf, Freigabe 3 erlaubt das Teilen
  ausdrücklich). Der Nutzer hat die Schemaänderung für diesen Lauf **ausgeschlossen**; damit fällt der Kern
  dieser Story auf den Tag, und `wartet_auf` benennt das. Was heute Nacht baubar ist, steht in
  [B-178](B-178-katalogtext-verspricht-mehr-als-der-server-erlaubt.md).
  **Die eigene Empfehlung dieser Story wurde dabei widerlegt:** Punkt 4 wollte den Vorspann-Satz hier
  mitnehmen, weil „sich nach Punkt 1 ändert, was wahr ist". Ohne Punkt 1 ändert sich für den Vater nichts —
  der Satz muss also die heutige Wahrheit sagen, und die ist eine andere. Ihn hier zu lassen hätte bedeutet,
  ihn zweimal zu schreiben.
- 2026-09-02 · **`ausformuliert` → `gegrillt`** (Grill-Runde am Tag, drei Fragen, `wartet_auf` geleert).
  Der belegte Kern: die Schemafrage ist entschieden, aber **anders benannt** als die Story vorschlug —
  `OwnerAdultId` statt `CreatedByAdultId`, weil `Subject` und `TextbookSeries` dieselbe Regel schon unter
  diesem Namen führen und `CreatedByAdultId` das vierte Wort für eine Sache gewesen wäre.
  **Zwei angenommene offene Punkte waren beim Nachsehen gegenstandslos:** ein Nachtrag für bestehende
  Arten ist unmöglich *und* unnötig (`backend/Pugling.Api/CLAUDE.md:22-23` — eine DB aus alter Kette wird
  abgewiesen, ein Upgrade-Pfad existiert bewusst nicht), und der Vertragsfeldname ist von
  [B-156](B-156-ismine-heisst-anderswo-isown.md) 7:1 auf `IsOwn` vorentschieden.
  Beim Nachsehen ebenfalls bestätigt: das Admin-Ventil aus B-178 **steht** (`:124`, `:157`), und sein
  Kommentar sagt selbst, dass es den Haushalt nicht befreit — der Defekt für einen normalen Vater war
  unverändert.
- 2026-09-02 · `gegrillt` → **`geschaetzt`**. **M** / `beides` / `migration: ja` / `vertragsbruch: nein`.
  Alles vier am Code nachgesehen statt abgeleitet — und dabei **zwei Behauptungen dieser Story berichtigt**:
  1. **`vertragsbruch` ist `nein`, nicht `ja`.** Entscheidung 6 hatte es behauptet. Der Größen-Anker im
     README sagt „ein *additives* Feld ist kein Bruch", und praktisch stimmt das: drei Bauplätze, alle im
     selben Controller; `Pugling.Client` und das Frontend **lesen** nur; `unknown_field` gilt für Anfragen.
     Die Kostenzeile der Entscheidung ist durchgestrichen und ersetzt.
  2. **Der Ist-Stand nennt `FachEigentumTests.cs:194` und `:215` als die zwei Pins.** Beim Nachsehen ist
     die Rollenverteilung eine andere: `Arten_EinesSeedFachs_SindFuerJedenGesperrt` greift die **erste
     bestehende** Art des Seed-Fachs, also eine *geseedete* — die bleibt owner-los und gesperrt, der Test
     ist damit **AK 4** und überlebt unverändert. Der Test, der wirklich **bricht**, ist B-178s
     `Admin_DarfDieArten_EinesFachsOhneEigentuemer_Aufraeumen`: seine Vorbedingung (`:344-348`) behauptet
     `403` für genau den Erwachsenen, der die Art angelegt hat. Das ist der Defekt, als Zusicherung
     festgenagelt — und er wäre beim Bauen als „Test kaputt" durchgerutscht, statt als der Beleg gelesen
     zu werden, der er ist.
  Der größte einzelne Posten liegt nicht im Backend, sondern im Frontend: ein `describe`-Block mit sechs
  Fällen, dessen **Titel** („das Fach entscheidet, wer die Arten ändern darf") durch diese Story falsch
  wird — umschreiben, nicht ergänzen.
- 2026-09-02 · **Gebaut** (`geschaetzt` → `in-arbeit`), Angriffsplan in der geplanten Reihenfolge.
  **Alle zehn Akzeptanzkriterien erfüllt**, jedes tragende mit roter Probe an *seiner* Zusicherung:

  | Probe | erwartet / gemessen |
  |---|---|
  | Art-Zweig aus `MayWrite` entfernt | rot: **AK 2, 3, 6** — AK 5 blieb grün |
  | Fach-Zweig aus `MayWrite` entfernt | rot: **AK 5** allein |
  | Art-Zweig aus der Lese-Projektion entfernt | rot: **AK 7** allein |
  | `isOwn` pro Zeile ignoriert (Frontend) | rot: 4 Fälle, u. a. der gemischte |
  | Server auf die Regel vor B-170 zurückgesetzt | rot: die E2E an Zeile 54 (eigene Zeile ohne Knöpfe) |

  **Verifikation:** **844/844** Backend (+5 Fälle), **325/325** vitest, **46/46** E2E (+1 neue Spec),
  `tsc` sauber, `dotnet format` und `markdownlint` sauber. Snapshot-Diff **genau 12 Zeilen**: Spalte,
  FK-Index, `SetNull`, Navigation — kein DB-Default nachgewachsen (Risiko 2 erledigt).

  **Die Schätzung lag an drei Stellen daneben, und zwar nach oben wie nach unten:**
  1. **Ein Tor mehr als geplant.** `SchemaGuardTests.Jeder_Fremdschluessel_Hat_Ein_Abgenommenes_Loeschverhalten`
     verlangt jeden neuen Fremdschlüssel im abgenommenen Inventar. Der Angriffsplan hatte es nicht genannt —
     das Tor hat genau seine Arbeit getan.
  2. **Ein Testfall, den die Schätzung als „überlebt unverändert" führte, fiel doch** — aber aus einem
     anderen Grund als dem vermuteten: `Arten_EinesSeedFachs_SindFuerJedenGesperrt` greift `.First()` der
     nach Namen sortierten Liste, und eine von meinen Fällen angelegte `Art-…` sortiert **vor**
     `Grammatik`. Das war vorbestehende Fragilität, aufgedeckt durch die neuen Fälle. Behoben durch einen
     Helfer `SeedArtIdAsync`, der die geseedete Art **beim Namen** nimmt, statt „die erste" zu raten.
  3. **Das Frontend war größer als „ein `describe`-Block".** Weil `CategoryRows` das Fach nicht mehr
     braucht, braucht `CategorySection` es auch nicht — und damit verloren **vier** Zusicherungen ihren
     Sinn: drei parametrisierte Fälle „Anlegen bleibt frei je Fach-Zustand" wären ohne das Prop drei
     identische Renderings, und von B-174s Paar lebte einer davon, dass die Fläche unter einem fremden
     Fach stumm war. Sie sind **zusammengelegt, nicht gestrichen**: die Zusage „Anlegen bleibt frei" hängt
     jetzt an der **Signatur** (der Abschnitt bekommt das Fach nicht, *kann* also nicht danach gaten), und
     das ist stärker als drei Fälle. Darum sinkt die vitest-Zahl 329 → 325; weniger Fälle, mehr
     Tragfähigkeit. Der Grund steht an beiden Stellen im Code, damit niemand sie als Lücke liest.

  **Die offene Entscheidung der Schätzung ist getroffen: es gibt jetzt eine E2E.**
  `frontend/e2e/katalog-arten.spec.ts` fährt Vorspann → Seed-Art ohne Knöpfe → Tippfehler anlegen →
  umbenennen → löschen. Sie ist damit der Rollengang (Bereichs-README), deckt AK 8 und 9, die sonst nichts
  abdeckt, und ist wiederholbar. Ihr erster Lauf fiel an `getByLabel("Fach")` — Teilstring-Treffer auf drei
  Elemente, genau die Falle aus `frontend/CLAUDE.md`; `{ exact: true }` steht jetzt mit Begründung dort.

  **Zwei Kommentare habe ich beim Bauen als falsch entlarvt und umgeschrieben, nicht stehen gelassen:**
  `LearnEntities.cs` argumentierte vorab gegen diesen Fix („a second, laxer reading of ownerless") — das war
  Entscheidung 4 und ist jetzt dort widerlegt statt nur in der Story. Und mein eigener erster
  `MayWrite`-Kommentar behauptete, die Lesewege benutzten ihn; sie können es nicht (eine Projektion muss
  nach SQL übersetzbar sein). Aufgefallen ist es daran, dass Probe 1 die Lese-Zusicherung **nicht** fallen
  ließ. Die Lesewege haben jetzt **eine** gemeinsame `Projection`, und der Kommentar nennt beide Fassungen
  samt der Probe, die je an ihr hängt.

  Ein Nebenbefund, der **nicht** eingearbeitet wurde: Meine `<param>`-Beschreibung sollte kein rohes
  Markup ins Vertragsdokument tragen (B-138). Beim Prüfen lieferte ein `grep` erst ein falsch-negatives
  „null Treffer"; ein JSON-Durchlauf zeigt **79 `<b>`-Paare / 180 Vorkommen** — B-138s Prämisse hält, und
  mein Feld trägt keins. Fast hätte ich „Prämisse veraltet" in eine Story geschrieben.
- 2026-09-02 · Bleibt auf `in-arbeit`: **beide Reviewer sind nicht gelaufen** (`wo: beides`, diese Sitzung
  startet keine Agenten unaufgefordert). Alles andere ist belegt, `wartet_auf` benennt es. Der Rollengang
  ist erbracht — als E2E, die sich wiederholt.
