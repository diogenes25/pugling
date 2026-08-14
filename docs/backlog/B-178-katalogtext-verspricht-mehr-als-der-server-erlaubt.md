---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/backend, rolle/creator]
aliases: [Vorspann verspricht zu viel, Admin-Ventil fuer Arten, was du selbst angelegt hast]
status: abgenommen
prio: P1
art: Defekt
groesse: XS
wo: beides
migration: nein
vertragsbruch: nein
quelle: Teilung von B-170 (Nachtlauf 2026-08-14)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-157]
wartet_auf: ""
nachgeschaut: ""
---

# B-178 · Der Katalogtext verspricht mehr als der Server erlaubt

Abgeteilt von [B-170](B-170-selbst-angelegte-art-im-grundbestand-ist-unloeschbar.md): Deren Kern — darf der
Anleger seine Art in einem fremden Fach entfernen? — braucht eine Schemaentscheidung und bleibt liegen. Was
**ohne** sie richtig wird, steht hier.

## Zwei Sätze auf einem Bildschirm, und einer ist falsch

`frontend/src/vater/VaterKatalog.tsx:26-27`:

> Fächer und **Arten** sind der gemeinsame Rahmen aller Übungen – auch der von anderen Vätern. Hier legst du
> neue an; umbenennen und löschen kannst du, **was du selbst angelegt hast**.

`frontend/src/vater/CatalogAdmin.tsx:183-184`, zwei Absätze darunter:

> Diese Arten gehören zum Fach „Englisch" aus dem Grundbestand – ergänzen darfst du sie, umbenennen und
> löschen kann sie **niemand**.

Die Karte hat recht. Der Vorspann verspricht für **Arten** etwas, das der Server seit B-157 für jeden mit
`403 not_owner` abweist — auch für den, der die Art selbst angelegt hat, sobald sie unter einem Fach ohne
Eigentümer liegt. Und das sind für einen neuen Nutzer **alle vier** Fächer.

**Im Rollengang am 2026-08-14 leibhaftig gesehen**, nicht aus dem Code geschlossen: `/vater/katalog` zeigte
den Satz Wort für Wort.

Attribution: Der Satz stammt aus B-154 und war dort nur *zu streng* (Arten waren bedingungslos änderbar),
also harmlos. Falsch in der **gefährlichen** Richtung (Versprechen > Server) wurde er mit B-157, und B-157
hat `VaterKatalog.tsx` nicht angefasst. Der Kommentar direkt über dem Satz (`:22-24`) begründet ihn damit,
dass im Rollengang zu B-154 „eine Zeile über der Karte weiter etwas versprach, was die Karte darunter
verweigert" — er wurde also **gegen genau diese Fehlerklasse** geschrieben und ist ihr wieder verfallen.

## Die zweite Hälfte: der Zustand ist heute für niemanden reparierbar

Ein Tippfehler („Grammtik") in einem Grundbestands-Fach steht **dauerhaft** in der Planbau-Vorfilterung.
Nachgeprüft: `Create` ist ungegated (`ExerciseCategoriesController.cs:73-78`), `Update` und `Delete` liefern
`403` (`:111`, `:143`), das Fach zu löschen ebenfalls (`SubjectsController.cs:120-123`), und ein `IsAdmin`
gibt es in **keinem** der beiden Controller.

Der Kontrast steht im Repo: `ExerciseControllerBase.cs:117,126` reicht `User.IsAdmin()` bewusst durch,
ausdrücklich „to edit orphaned (ownerless) exercises in an emergency". Die Übung hat das Ventil, die Art
nicht.

## Entscheidungen

1. **Das Admin-Ventil kommt (`IsOwnedBy(…) || User.IsAdmin()`), obwohl es dem Vater nicht hilft.** Vom
   Nutzer für diesen Lauf freigegeben. Begründung: Es macht den Zustand überhaupt **reparierbar** und stellt
   die Symmetrie zur Übung her, die schon Präzedenz ist. *Kosten, offen ausgesprochen:* Ein Haushalt hat
   keinen Admin — für den Vater bleibt der Tippfehler stehen, bis B-170 entschieden ist. Diese Story behebt
   **nicht** seinen Fall, und sie behauptet das auch nicht.
2. **Der Vorspann sagt die heutige Wahrheit, nicht die künftige.** Er trennt Anlegen von Ändern und benennt,
   dass das Recht am **Fach** hängt. Begründung: Ein Satz, der auf B-170 vorgreift, wäre wieder falsch —
   diesmal in der anderen Richtung. *Kosten:* Nach B-170 muss er ein zweites Mal angefasst werden; das ist
   billiger als ein Versprechen, das der Server heute bricht.
3. **Das Ventil reicht weiter als sein Anlass, und das bleibt so.** *Beim Bauen aufgefallen:* Der Grund ist
   das **ownerlose** Fach, die Wirkung ist breiter — ein Admin passiert die Prüfung für **jedes** Fach, auch
   ein von einem anderen Creator besessenes. Begründung fürs Belassen: Genau so nimmt die Übung ihren
   `IsAdmin` (`ExercisePermissionService.CanWrite`), und ein Break-glass, der nur ownerlose Zeilen öffnet,
   wäre eine zweite, anders geformte Regel für dieselbe Idee. *Kosten:* Der Kommentar muss die **Reichweite**
   nennen, nicht nur den Anlass — sonst ist er die dritte Kommentar-Behauptung dieses Laufs, die mehr sagt als
   der Code hält. Er tut es jetzt.
4. **Der Kartensatz in `CatalogAdmin.tsx` bleibt unverändert.** Er ist korrekt und war es immer. *Kosten:*
   keine — die Entscheidung steht hier, damit niemand beide Stellen „vereinheitlicht" und dabei die richtige
   der falschen anpasst.

## Akzeptanzkriterien

1. Der Vorspann auf `/vater/katalog` verspricht für Arten nichts, was der Server abweist — und sagt, dass
   das Änderungsrecht am **Fach** hängt.
2. Ein Admin darf eine Art unter einem Fach ohne Eigentümer umbenennen und löschen; ein Nicht-Admin
   weiterhin nicht (`403 not_owner`).
3. Ein Test hält beides und wird rot, wenn das Ventil entfernt wird — mit Zahl.
4. Die Karte in `CatalogAdmin.tsx` ist unverändert.

## Schätzung

**Größe: XS** — zwei Bedingungen um `|| User.IsAdmin()` erweitert, ein Satz umgeschrieben, ein Testfall.

- **`wo: beides`** — Backend (Ventil) zuerst, dann der Satz im Frontend.
- **`migration: nein`** — kein Schema. Das ist der ganze Grund, warum diese Hälfte heute Nacht baubar ist.
- **`vertragsbruch: nein`** — kein DTO, kein Feld, kein Statuswort ändert sich für bestehende Aufrufer; das
  Ventil **erweitert** nur, wer `200`/`204` bekommt.

**Risiken:**

1. **Gibt es überhaupt einen Admin-Anmeldeweg in den Tests?** Wenn nicht, ist AK 2 nur zur Hälfte prüfbar.
   Das ist **vor** dem Bauen nachzusehen — `Roles.Admin` existiert, ein Testhelfer dafür vielleicht nicht.
2. **Die Rollenmenge des Controllers.** `[Authorize(Roles = Roles.Creator)]` steht auf der Klasse. Ein Admin,
   der nicht auch Creator ist, kommt gar nicht bis zur Eigentumsprüfung — dann wäre das Ventil wirkungslos
   und der Fix eine Attrappe. Ebenfalls vorher nachsehen, nicht annehmen.

**Angriffsplan:**

1. Nachsehen, wie ein Admin in den Tests entsteht und ob er die Klassen-Autorisierung passiert (Risiken 1
   und 2). Fällt das negativ aus, ist AK 2 nicht erreichbar und die Story wird **darauf** reduziert, was
   erreichbar ist — mit einer Zeile, die es benennt.
2. Ventil in `Update` und `Delete`, mit Kommentar auf die Präzedenz.
3. Testfall in `FachEigentumTests`, rote Probe mit Zahl.
4. Dann der Satz in `VaterKatalog.tsx`.

**Testweg**: `backend/Pugling.Api.Tests/FachEigentumTests.cs` (die Klasse hält schon die Eigentumsregeln der
Arten). Für den Satz kein Test — Vorspann-Prosa ist nicht sinnvoll zusicherbar, und das ist genau der Grund,
warum sie zwei Stories lang falsch stehen konnte; der Beleg ist der Rollengang.

## Verlauf

- 2026-08-14 · Angelegt als Teilung von [B-170](B-170-selbst-angelegte-art-im-grundbestand-ist-unloeschbar.md)
  im Nachtlauf, direkt auf `geschaetzt`: Ist-Stand, Entscheidungen und Testweg sind aus der Mutter-Story
  belegt übernommen, die Schemafrage ist dort geblieben. Prio P1 wie die Mutter — der falsche Satz trifft
  jeden neuen Nutzer beim ersten Blick in den Katalog.
- 2026-08-14 · Gebaut und `abgenommen`. **Rote Probe:** ohne das Ventil erwartet `OK`, gemessen `Forbidden`
  (vom Reviewer unabhaengig nachgestellt, gleiche Zahl). Danach 831/831.
  **Rollengang an der laufenden App**, Server nach der letzten Aenderung mit frischer DB gestartet: Auf
  `/vater/katalog` stimmen jetzt **alle vier** Texte ueberein - Vorspann („umbenennen und loeschen nur, wer
  das Fach angelegt hat - bei den mitgelieferten also niemand"), Faecher-Karte, Englisch-Karte und
  Arten-Karte. Vorher widersprach der Vorspann der Arten-Karte zwei Absaetze weit. AK 1 damit **live** belegt,
  und das war noetig: ein Test haelt Vorspann-Prosa nicht.
  **Zwei Funde beim Bauen, einer von mir, einer vom Reviewer:** Das Ventil **reicht weiter als sein Anlass** -
  ein Admin passiert fuer jedes Fach, nicht nur fuer ownerlose. Das bleibt so (dieselbe Blankovollmacht hat er
  an der Uebung und am Verlag), aber es ist jetzt **festgenagelt** statt behauptet:
  `Admin_DarfAuchDieArten_EinesFremden_Fachs_Aendern`. Und die **XML-Doku sagte weiter das Gegenteil**
  („Only the owner of the subject may do so") - sie fliesst in den Vertrag, also haben sich zwei Zeilen in
  `docs/openapi/v1.json` mitbewegt.
  **Regressionszeugen:** Der Assistent liest dieselben Arten unveraendert (`Grammatik · Lesetexte · Vokabeln`);
  die Sohn-Arcade liegt nicht im Diff und ist im vollen E2E-Lauf gruen.
  **Was diese Story ausdruecklich NICHT behebt:** den Fall des Vaters. Kein geseedeter Erwachsener traegt das
  Admin-Flag (nachgesehen), sein Tippfehler bleibt stehen, bis
  [B-170](B-170-selbst-angelegte-art-im-grundbestand-ist-unloeschbar.md) entschieden ist.
