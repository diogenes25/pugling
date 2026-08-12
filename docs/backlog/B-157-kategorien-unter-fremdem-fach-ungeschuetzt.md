---
tags: [typ/story, status/ausformuliert, bereich/katalog, bereich/auth, rolle/creator]
aliases: [Arten im fremden Fach umbenennbar, ExerciseCategory ohne Eigentum]
status: ausformuliert
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

1. **Gilt das Eigentum des Fachs für seine Arten, oder sind Arten bewusst gemeinsam?** Empfehlung: es gilt.
   Eine Art ist ohne ihr Fach bedeutungslos (`SubjectId` ist Pflicht, Cascade), und `CatalogAdmin`
   beschreibt sie als „der einzige Ordnungsbegriff, den der Vater selbst erfinden darf" — an *seinem* Fach.
2. **Auch `POST`, oder nur `PATCH`/`DELETE`?** Empfehlung: alle drei. Beim Fach durfte `Create` frei
   bleiben, weil ein neues Fach niemandem gehört; eine neue Art landet dagegen **in** einem fremden Baum.
   Das ist der Unterschied, der die Abweichung von B-13s Entscheidung 2 rechtfertigt — er gehört benannt,
   nicht stillschweigend übernommen.
3. **Bekommt `CategoryResponse` ein `isOwn`?** Empfehlung: ja, additiv — sonst kann die Oberfläche die
   Sperre wieder nur erraten, und wir hätten B-154 eine Ebene tiefer erneut. Abhängigkeit:
   [B-156](B-156-ismine-heisst-anderswo-isown.md) entscheidet den Feldnamen; diese Story sollte danach
   kommen oder den dort beschlossenen Namen übernehmen.
4. **Zieht das Frontend im selben Schnitt mit?** Empfehlung: ja — anders als bei B-13 ist der UI-Nachtrag
   hier nicht hypothetisch, sondern durch B-154 schon sichtbar halb fertig.
5. **Trägt der Sonderfall „ownerloses Seed-Fach" seine Arten mit?** Zu prüfen: Die Seed-Fächer haben keinen
   Owner, ihre Arten wären damit fail-closed für **jeden** unveränderbar. Das ist konsequent, aber es
   trifft mehr Zeilen als bei B-13 (dort vier Fächer, hier deren gesamte Art-Listen).

## Akzeptanzkriterien (Entwurf)

1. `POST`/`PATCH`/`DELETE` auf `…/subjects/{subjectId}/categories…` liefert `403 not_owner`, wenn der
   aufrufende Creator nicht Eigentümer des **Fachs** ist — inklusive `OwnerAdultId == null`.
2. Der Eigentümer des Fachs kann seine Arten unverändert anlegen, umbenennen und löschen.
3. `GET`/`List` der Arten bleibt für jeden Creator offen (Lesen ist global, wie beim Fach).
4. Ein Integrationstest belegt den Fremd-Zugriff auf allen drei schreibenden Wegen; ein Fall deckt das
   ownerlose Seed-Fach.
5. Falls offener Punkt 3 mit „ja" entschieden wird: `CategoryResponse` trägt das Flag, und
   `CatalogAdmin.tsx` liest es (die `NameRow` der Art bekommt dieselbe Behandlung wie die des Fachs).

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
