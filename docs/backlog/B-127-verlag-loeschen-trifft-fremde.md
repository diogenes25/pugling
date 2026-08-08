---
tags: [typ/story, status/ausformuliert, bereich/katalog, bereich/backend, rolle/creator]
aliases: [Verlag löschen ohne Eigentum, SetNull auf fremde Reihen, geteilte Zeile ohne Schutz]
status: ausformuliert
prio: P3
art: Frage
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 2)
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-127 · Jeder Creator darf einen Verlag löschen, den alle benutzen

`DELETE creator/publishers/{id}` steht jedem Konto mit der Creator-Rolle offen — auf einer Zeile, die
**global geteilt** ist. Der Fremdschlüssel räumt danach still auf: jede Reihe, die auf den Verlag zeigte,
verliert ihre Zuordnung (`SetNull`), auch die Reihen aller anderen Creator. Ein Lehrer-Konto kann damit
in einem Aufruf die Verlagszuordnung des gesamten Katalogs entfernen.

Das ist **keine Nachlässigkeit, sondern eine ausdrückliche Entscheidung** aus
[B-63](B-63-lehrwerk-hierarchie.md) — die Frage ist, ob sie mit dem heutigen Löschradius noch trägt.

## User Story

Als **Creator**, dessen Reihen auf geteilte Verlage zeigen, möchte ich wissen, ob ein fremdes Konto meine
Verlagszuordnung entfernen kann — damit ich mich auf den Katalog verlassen kann oder zumindest weiß,
dass ich es nicht kann.

## Ist-Stand am Code

- `Controllers/Creator/PublishersController.cs:21` gated die **ganze** Klasse nur mit
  `[Authorize(Roles = Roles.Creator)]`; `Delete` (`:111-119`) prüft kein Eigentum, weil es keines gibt.
- Der Klassenkommentar (`:11-15`) sagt das ausdrücklich zu: *„Global and child-neutral like the
  vocabulary store: naming a publisher is not authorship, so – unlike the series itself – there is no
  owner and no write restriction."*
- Der Methodenkommentar (`:106-109`) beziffert die Kosten: *„a publisher carries no content, its loss
  only costs a filter/display value."* Das stimmt **pro Reihe**, verschweigt aber, dass der Verlust alle
  Reihen aller Creator gleichzeitig trifft.
- Zum Vergleich: `TextbookSeriesController.Delete` (`:196-200`) ist eigentümergebunden **und** trägt eine
  Nutzungssperre (`409`, solange eine Übung daran in einem Plan hängt).
- Die Oberfläche formuliert es als Randnotiz: `frontend/src/vater/PublisherAdmin.tsx:153` fragt
  „… N Reihe(n) verlieren nur die Zuordnung" — ohne zu sagen, dass es fremde Reihen sein können.

## Die echte Lücke

Nicht „das Löschen ist ungeschützt" — das ist es absichtlich, und das Argument dafür ist gut: ein
Verlagsname ist keine Autorschaft, und eine Eigentümerbindung an „Klett" wäre absurd.

Die Lücke liegt zwischen der **Begründung** und der **Reichweite**: „kostet nur einen Anzeigewert" ist
für den Löschenden wahr und für alle anderen unvollständig. Es gibt keinen Weg zurück (die Zuordnung ist
danach weg, nicht wiederherstellbar) und keine Warnung, dass fremde Daten betroffen sind. Dieselbe
Freiheit hat der Vokabelspeicher — aber dort **löscht** niemand fremde Verknüpfungen mit einem Aufruf.

## Offene Punkte

Diese Story ist bewusst eine **`Frage`**: sie kann in `verworfen` enden, und das wäre ein Erfolg. Ein
Agent darf sie darum nicht selbst entscheiden (README → „Was der Agent selbst grillen darf").

1. **Gilt die B-63-Entscheidung weiter?** Empfehlung: ja für `POST`/`PATCH`, nein für `DELETE`. Anlegen
   und Umbenennen sind additiv bzw. reparierbar; Löschen ist der einzige Aufruf, der fremde Daten
   irreversibel verändert.
2. **Falls ja — welche Bremse?** Drei Formen, aufsteigend teuer: (a) nur die Bestätigungsfrage im
   Frontend ehrlich machen („davon N Reihen anderer Konten"), (b) eine Nutzungssperre wie bei der Reihe
   (`409`, solange **fremde** Reihen daran hängen), (c) Löschen nur für die Admin-Rolle. Empfehlung: (b)
   — es folgt dem Muster, das der Nachbar-Controller schon hat, und lässt das Aufräumen ungenutzter
   Verlage weiterhin zu.
3. **Ist das eine Sicherheits- oder eine Bedienfrage?** Empfehlung: Bedienfrage. Die Creator-Rolle
   bekommt nur, wer ein Konto anlegt; das ist kein anonymer Angriffsweg. Bei anderer Einschätzung
   verschiebt sich die `prio` deutlich nach oben.

## Akzeptanzkriterien

> Entwurf — hängen an Offenem Punkt 2 und werden erst beim Grillen final.

1. Das Löschen eines Verlags, an dem Reihen **fremder** Konten hängen, ist nicht mehr folgenlos möglich
   bzw. wird dem Löschenden vorher wahrheitsgemäß beziffert.
2. Das Löschen eines Verlags ohne fremde Reihen bleibt möglich.
3. Der Klassen- bzw. Methodenkommentar sagt die tatsächliche Reichweite, nicht nur die Kosten pro Reihe.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`, am Code nachgeprüft
  (`PublishersController.cs:21,106-119`). Als **`Frage`** eingestuft und nicht autonom entschieden: der
  Zustand ist eine dokumentierte B-63-Entscheidung, und sie zu revidieren ist eine Wertentscheidung.
  `entgangen_bei` bleibt **leer** — es ist kein durchgekommener Defekt, sondern eine bewusste
  Entscheidung, deren Begründung sich als zu eng erwiesen hat.
