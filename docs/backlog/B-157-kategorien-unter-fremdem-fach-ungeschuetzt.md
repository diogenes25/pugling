---
tags: [typ/story, status/gegrillt, bereich/katalog, bereich/auth, rolle/creator]
aliases: [Arten im fremden Fach umbenennbar, ExerciseCategory ohne Eigentum]
status: gegrillt
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zu B-13 (Nachtlauf 2026-08-12, Fund 2)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-157 · Das Fach ist geschützt, seine „Arten" sind es nicht

[B-13](B-13-fach-kapitel-eigentum.md) hat das `Subject` geschlossen: umbenennen und löschen darf nur der
Eigentümer. Eine Route tiefer gilt das nicht. Ein fremder Creator kann die Arten **in meinem Fach**
umbenennen und löschen — also genau die Vorfilter-Liste, an der die Lehrplan-Erstellung hängt.

## User Story

Als *Creator* möchte ich, dass die Ordnungsbegriffe unter meinem Fach demselben Eigentum folgen wie das Fach
selbst, damit ein fremder Creator meinen Katalog-Baum nicht eine Ebene tiefer umbauen kann, nachdem ihm die
obere Ebene verwehrt wurde.

## Ist-Stand am Code

- `ExerciseCategoriesController` liegt unter `api/v1/creator/subjects/{subjectId}/categories` und trägt
  außer `[Authorize(Roles = Roles.Creator)]` **keine** Prüfung
  (`backend/Pugling.Api/Controllers/Creator/ExerciseCategoriesController.cs:17-21`). `POST` (`:54`),
  `PATCH` (`:82`) und `DELETE` (`:104`) treffen damit jedes fremde Fach.
- `ExerciseCategory` trägt selbst **kein** Eigentümer-Feld — nur `Id`, `SubjectId`, `Subject`, `Name`,
  `CreatedAt` (`backend/Pugling.Api/Models/LearnEntities.cs:40-46`). Der Eigentümer, an dem sich eine
  Prüfung ausrichten könnte, hängt also am Fach darüber, nicht an der Zeile selbst.
- `CategoryResponse` hat folgerichtig kein `isMine`
  (`backend/Pugling.Contracts/Creator/CatalogDtos.cs:28`) — ein Client kann die Berechtigung heute nicht
  einmal anzeigen, wenn er wollte.
- **Die Klassen-Doku des Nachbarn verspricht mehr, als eine Route tiefer gilt:** `SubjectsController.cs:12-14`
  sagt „only the owner may rename or delete it", und die Entity-Doku nennt den Katalog „the tree a whole
  household plans against" (`LearnEntities.cs:20-21`).
- **Der Widerspruch ist heute sichtbar, nicht nur theoretisch:** Seit
  [B-154](B-154-katalogseite-bietet-fremde-faecher-zum-umbenennen.md) blendet `/vater/katalog` die
  Fach-Knöpfe an einem fremden Fach aus — die Art-Zeilen darunter bleiben bedienbar und funktionieren
  auch. Eine Oberfläche, die oben sperrt und unten nicht, liest sich als Versehen; sie spiegelt aber
  genau den Server.

## Die echte Lücke

Nicht ein fehlendes Feld an der Kategorie: die Berechtigung ist über `Subject.OwnerAdultId` bereits
vollständig bestimmt, sie wird nur nicht gelesen. Die Lücke ist, dass B-13 die Ebene geschlossen hat, auf
der die Idee formuliert war, und die darunterliegende offen ließ — dieselbe Fehlerklasse wie
[B-108](B-108-requiretypedtest-default-am-uebungstyp.md) und
[B-104](B-104-keyresult-dublette-zahlt-doppelt.md) („dieselbe Regel eine Ebene höher/tiefer").

## Offene Punkte

Alle in der Grill-Runde vom 2026-08-13 geschlossen (Nummern zeigen auf die Entscheidungen).

1. ~~Gilt das Eigentum des Fachs für seine Arten, oder sind Arten bewusst gemeinsam?~~ → Entscheidung 1.
2. ~~Auch `POST`, oder nur `PATCH`/`DELETE`?~~ → Entscheidung 2. Die Empfehlung der Ausformulierung („alle
   drei") wurde dabei **revidiert**: `POST` bleibt frei.
3. ~~Bekommt `CategoryResponse` ein `isOwn`?~~ → Entscheidung 3. Ebenfalls **revidiert**: kein neues Feld,
   und damit auch keine Abhängigkeit von [B-156](B-156-ismine-heisst-anderswo-isown.md).
4. ~~Zieht das Frontend im selben Schnitt mit?~~ → Entscheidung 4.
5. ~~Trägt der Sonderfall „ownerloses Seed-Fach" seine Arten mit?~~ → Entscheidung 1 (ja, fail-closed); die
   Reichweite ist am Seed nachgezählt.

In der Runde **neu aufgetaucht** und ausgelagert: die Begriffskollision „Art" gegen „Typ" (Entscheidung 5).

## Entscheidungen

1. **Das Eigentum des Fachs gilt für seine Arten, fail-closed.** Eine Art ist ohne ihr Fach bedeutungslos
   (`SubjectId` ist Pflicht), und `CatalogAdmin` beschreibt sie als „der einzige Ordnungsbegriff, den der
   Vater selbst erfinden darf" — an *seinem* Fach. Ein ownerloses Fach macht seine Arten damit für
   **niemanden** änderbar, dieselbe Semantik wie B-13s Entscheidung 3. *Kosten:* Am Seed nachgezählt sind
   das **sieben** Arten an vier ownerlosen Fächern (`Seed.cs:605-606, 997-999, 1002-1003`: Vokabeln,
   Grammatik ×2, Leseverstehen, Grundrechenarten, Algebra) — und das sind genau die Vorfilter-Listen, an
   denen der Planbau hängt. Sie sind ab dieser Story für alle eingefroren. Die Reichweite ist deutlich
   größer als B-13s vier Fächer; für dieses Projekt tragbar, weil Datenbanken weggeworfen werden, für eine
   Produktionsinstanz nicht.
2. **Anlegen bleibt frei — keine Abweichung von B-13s Entscheidung 2.** Die Ausformulierung hatte „alle
   drei Wege sperren" empfohlen; das ist **am Code widerlegt**: `IsOwnedBy(null, …)` ist `false`
   (`AuthAccess.cs:90`), ein gegatetes `POST` hätte mit Entscheidung 1 dazu geführt, dass **niemand** mehr
   eine Art unter „Englisch", „Mathe", „Erdkunde" oder „Französisch" anlegen kann — also unter den einzigen
   Fächern, die ein normaler Nutzer hat. Die Art-Achse des Seed-Katalogs wäre eingefroren, nicht geschützt.
   *Kosten:* Ein fremder Creator darf weiter Einträge in die Vorfilter-Liste meines eigenen Fachs legen; er
   kann sie nur nicht mehr umbenennen oder löschen. Das ist die bewusst in Kauf genommene Hälfte.
3. **Kein Eigentums-Flag an `CategoryResponse`.** Das Eigentum der Art *ist* das des Fachs (Entscheidung 1)
   — ein eigenes Feld wäre eine zweite Kopie derselben Wahrheit, und zwei Kopien laufen in diesem Repo
   auseinander. Nachgezählt: **alle vier** Leser holen die Arten über die Fach-Id
   (`CatalogAdmin.tsx:37`, `ExerciseFilterBar.tsx:44`, `VaterWizard.tsx:133`, `CreatorApi.cs:52`), und der
   **einzige**, der etwas ändert, ist `CatalogAdmin` — der hält `subject.isMine` seit B-154 schon.
   *Kosten:* Ein künftiger Client, der Arten **ohne** ihr Fach listet, müsste das Flag nachtragen; additiv,
   also kein Bruch. Folge dieser Entscheidung: `vertragsbruch: nein` und keine Abhängigkeit von B-156.
4. **Das Frontend zieht im selben Schnitt mit** (`wo: beides`). B-13 hat genau das aufgeschoben
   (`wo: backend`, Entscheidung 5) — und das Ergebnis war [B-154](B-154-katalogseite-bietet-fremde-faecher-zum-umbenennen.md):
   Knöpfe, die ein `403` versprechen. Ohne den UI-Teil erzeugt diese Story denselben Defekt eine Ebene
   tiefer, wissentlich, und B-154s Akzeptanzkriterium 4 („die Art-Zeilen bleiben bedingungslos") würde von
   einer richtigen Aussage zu einer falschen. *Kosten:* beide Reviewer und ein größerer Diff — der
   Frontend-Anteil ist allerdings **eine** Datei und dasselbe Muster wie `SubjectRow`, eine Ebene tiefer.
5. **Die Begriffskollision „Art" gegen „Typ" wird eine eigene Story.** Beim Nachsehen gefunden: beide Achsen
   der Übungssuche tragen **dieselben Wörter** — `VocabularyExerciseType.cs:18` heißt „Vokabeln" und
   `Seed.cs:997` heißt „Vokabeln"; `BuiltInExerciseTypes.cs:22` heißt „Leseverständnis" und `Seed.cs:999`
   heißt „Leseverstehen" — und im UI stehen sie als „– alle Arten –" und „– alle Typen –" nebeneinander in
   derselben Filterleiste. Das Ziel dieser Story ist ohne die Entzerrung erfüllt, also greift die Regel des
   Bereichs (eigene Story). *Kosten:* Nach Entscheidung 1 sind die Seed-Arten anschließend für niemanden
   mehr umbenennbar — eine Entzerrung der Namen muss also **im Seed** passieren, und je später sie kommt,
   desto mehr bestehende Datenbanken tragen die kollidierenden Namen. → **[B-163](B-163-art-und-typ-tragen-dieselben-woerter.md)**

## Akzeptanzkriterien

1. `PATCH`/`DELETE` einer Art liefert `403 not_owner`, wenn der aufrufende Creator nicht Eigentümer des
   **Fachs** ist — inklusive `OwnerAdultId == null` (Seed-Fach: für niemanden, auch nicht für den
   Seed-Vater).
2. `POST` einer neuen Art bleibt für **jeden** Creator frei — auch unter einem fremden und unter einem
   Seed-Fach (Entscheidung 2).
3. Der Eigentümer des Fachs kann seine Arten unverändert anlegen, umbenennen und löschen.
4. `GET`/`List` der Arten bleibt für jeden Creator offen — kein Verhalten ändert sich für Lesezugriffe.
5. **Kein neues Vertragsfeld.** `CatalogAdmin` gatet die Art-Zeilen über `subject.isMine` und nennt bei
   fremdem bzw. ownerlosem Fach den Grund, statt stumm zu bleiben (Muster `SubjectRow` aus B-154).
6. Ein Integrationstest belegt: fremder Creator → `403` auf **beiden** schreibenden Wegen; Seed-Fach →
   `403` auch für den Seed-Vater; **und in beiden Fällen gelingt das Anlegen weiterhin**.
7. Ein Komponententest belegt: Art-Zeilen ohne Bedien-Knöpfe bei fremdem und ownerlosem Fach, mit Knöpfen
   beim eigenen — und das „Neue Art"-Formular bleibt in **allen** Fällen bedienbar.
8. Das Löschverhalten ändert sich **nicht**: eine gelöschte Art nimmt ihren Übungen nur die Zuordnung
   (`Exercise.CategoryId` ist optional) — kein `409`, wie bisher.

Kriterium 6 und 7 tragen je die zweite Hälfte von Entscheidung 2 („Anlegen gelingt weiterhin"). Das ist
Absicht: genau diese Zusicherung würde ein späterer Umbau zu „symmetrisch sperren" brechen, und ohne sie
wäre die Entscheidung nur ein Kommentar.

## Verlauf

- **2026-08-12** — angelegt aus dem `pugling-reviewer`-Befund zum B-13-Review (Fund 2, Nachtlauf Sprint A).
  **Bewusst nicht in B-13 mitgenommen:** dessen Akzeptanzkriterien nennen Kategorien nicht, sein Ziel
  (`Subject` ist nicht mehr global schreibbar) ist ohne diese Story erfüllt, und der Fund liegt außerhalb
  seines Diffs. **Bewusst auch nicht in B-154 mitgenommen:** das ist die Server-Hälfte, B-154 war
  ausdrücklich frontend-only und hat die Art-Zeilen aus genau diesem Grund unangetastet gelassen (dessen
  Akzeptanzkriterium 4).
- **2026-08-12** — direkt auf `ausformuliert`: der Ist-Stand ist am Code belegt (Controller, Entity und
  Vertrag einzeln nachgesehen), darum wäre `unverifiziert: true` eine Untertreibung. Offen ist die
  **fachliche** Frage aus Punkt 1/2, und die entscheidet der Nachtlauf nicht selbst — `art: Defekt` erlaubt
  autonomes Grillen, aber Punkt 2 weicht bewusst von einer bestehenden Entscheidung ab (B-13, Entscheidung 2)
  und gehört darum vorgelegt.
- **2026-08-13** — `ausformuliert → gegrillt`. Fünf Entscheidungen im Dialog. **Zwei Empfehlungen der
  Ausformulierung wurden dabei widerlegt**, beide am Code: „alle drei Schreibwege sperren" hätte die
  Art-Achse aller vier Seed-Fächer eingefroren statt geschützt (`IsOwnedBy(null, …)` ist `false`), und ein
  Eigentums-Flag an `CategoryResponse` ist unnötig, weil alle vier Leser die Arten über die Fach-Id holen und
  der einzige Schreiber `subject.isMine` seit B-154 schon hält. Damit fällt die Abhängigkeit von B-156 weg
  und `vertragsbruch` ist `nein`. Die Reichweite von Entscheidung 1 ist am Seed nachgezählt: sieben Arten an
  vier ownerlosen Fächern. Neu aufgetaucht und ausgelagert: die Begriffskollision „Art" gegen „Typ"
  ([B-163](B-163-art-und-typ-tragen-dieselben-woerter.md)). Prio bleibt P2.
