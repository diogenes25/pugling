---
tags: [typ/story, status/ausformuliert, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Art gegen Typ, Vokabeln heißt zweimal etwas anderes]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-157 (Grill-Runde 2026-08-13, Entscheidung 5)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-163 · „Art" und „Typ" tragen dieselben Wörter — in einer Zeile sogar zweimal hintereinander

Die Übungssuche hat zwei unabhängige Achsen. Von den sieben geseedeten Arten kollidieren **fünf** mit einem
Übungstyp-Namen, und in der Auswahlliste des Planbaus stehen beide Werte punktgetrennt nebeneinander — beim
Seed-Bestand heißt das Zeilen wie „Begrüßungen · **Vokabeln** · **Vokabeln**".

## User Story

Als *Supervisor* möchte ich in der Übungsliste erkennen, welches Wort das Lernverfahren benennt und welches
den Ordnungsbegriff, damit ich beim Planbau nicht rate, was ich gerade auswähle.

## Ist-Stand am Code

**Die zwei Achsen und ihre Herkunft — und die ist entscheidend für die Reparatur.**

- Der **Typ** ist das Lernverfahren; sein Anzeigename ist eine **Konstante im Code** und kommt über
  `GET creator/exercise-types` aus dem Manifest (`frontend/CLAUDE.md`: „Anzeigename und Routen-Segment kommen
  aus dem Typ-Manifest"). Zwölf Typen, je eine Zeile in
  `backend/Pugling.Api/Exercises/` (z. B. `VocabularyExerciseType.cs:18` → „Vokabeln",
  `BuiltInExerciseTypes.cs:22` → „Leseverständnis").
- Die **Art** (`ExerciseCategory`) ist ein freier Ordnungsbegriff je Fach; ihr Name ist eine
  **Datenbankspalte** (`Models/LearnEntities.cs:40-46`), im Seed gesetzt und seit
  [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md) an den geseedeten Fächern für **niemanden**
  mehr änderbar.

**Die Kollisionen, ausgezählt** (das war der offene Punkt der Idee):

| Wort | als Typ | als Art | Fälle im Seed |
| --- | --- | --- | --- |
| **Vokabeln** | `Vocabulary` (`VocabularyExerciseType.cs:18`) | `Seed.cs:605`, `:997` | 2 (Französisch, Englisch) |
| **Grammatik** | `Grammar` (`BuiltInExerciseTypes.cs`) | `Seed.cs:606`, `:998` | 2 (Französisch, Englisch) |
| **Leseverstehen** / **Leseverständnis** | `Reading` → „Leseverständnis" | `Seed.cs:999` → „Leseverstehen" | 1 (Englisch) |

Von **sieben** geseedeten Arten kollidieren damit **fünf**; unbetroffen sind nur „Grundrechenarten" und
„Algebra" (`Seed.cs:1002-1003`). Die Idee sprach von zwei Fundstellen — „Grammatik" war übersehen, und das
ist die folgenreichste, weil sie auf beiden Achsen der *natürliche* Name ist.

**Wo es sichtbar wird, in aufsteigender Schwere:**

1. **`ExerciseFilterBar.tsx:96-108`** — zwei Auswahlfelder mit den sichtbaren Beschriftungen „Typ" und
   „Art" untereinander. Hier ist die Achse benannt; verwirrend sind nur die Werte.
2. **`VaterWizard.tsx:492-499`** — dieselben zwei Felder, aber **ohne** sichtbare Beschriftung: nur die
   Platzhalter „– alle Arten –" und „– alle Typen –" (`aria-label` trägt „Art" bzw. „Übungstyp"). Wer den
   Platzhalter durch eine Auswahl ersetzt hat, sieht die Achse nicht mehr.
3. **`PlanPositions.tsx:431-434`** — der schlimmste Fall und **heute reproduzierbar**: Titel, Typ-Label,
   Klassenstufe, Art und Quelle stehen punktgetrennt in *einer* Zeile, ohne ein Wort, das sagt welches
   welches ist:

   ```text
   {ex.title} · {typeLabel(ex.type)} · Kl. 5–7 · {ex.categoryName} · {ex.source}
   ```

   Mit dem Seed-Bestand ergibt das unter anderem:

   - „**Begrüßungen** · Vokabeln · Vokabeln" (`Seed.cs:1055`, Typ `Vocabulary`, Art „Vokabeln")
   - „**Vokabeln: En ville** · Vokabeln · Vokabeln" (`Seed.cs:639`) — dasselbe Wort **dreimal** in einer
     Zeile, einmal als Titel, einmal als Verfahren, einmal als Ordnungsbegriff

   Und das ist genau die Zeile, auf der der Supervisor beim **Planbau** seine Übungen anhakt.

**Der Vertrag hält die zwei Achsen sauber getrennt** — das Problem ist rein die Anzeige:
`ExerciseSummary`/`ExerciseResponse` tragen `Type` (der **Schlüssel**, nicht das Label) neben
`CategoryId`/`CategoryName` (`Contracts/Creator/ExerciseCatalogDtos.cs:14,26`). Ein Client kann sie also
unterscheiden; er tut es beim Rendern nur nicht.

**Eine dritte Achse ist unterwegs, mit demselben Wort.** [B-155](B-155-grammatik-themen-als-tags.md)
(`geschaetzt`) führt `GrammarTopic` als UI-„Grammatik-Thema" ein und hat in seiner Entscheidung 1 schon
notiert, dass dann „zwei Dinge ‚Thema' heißen". Kommt sie vor dieser Story, tragen **drei** Achsen der
Übungssuche das Wort „Grammatik": der Typ, die Art und das Thema.

## Die echte Lücke

Nicht die Doppelbelegung an sich — zwei Achsen dürfen sich Vokabular teilen, solange die Anzeige sagt, welche
gemeint ist. Die Lücke ist, dass die **eine Stelle, an der es zählt** (die Auswahlliste des Planbaus), beide
Werte als namenlose, punktgetrennte Fragmente zeigt. Und dass die Reparatur eine Asymmetrie hat, die die Idee
falsch vermutet hatte: **das Typ-Label ist billig zu ändern, der Art-Name teuer.** Das Label ist eine
Code-Konstante und wirkt beim nächsten Request in *jeder* bestehenden Datenbank; der Art-Name liegt in der DB
und ist seit B-157 an geseedeten Fächern gar nicht mehr änderbar — eine Seed-Änderung erreicht nur **frische**
Datenbanken.

## Offene Punkte

1. **Welche Achse weicht aus?** Empfehlung: **das Typ-Label**, nicht der Art-Name — das kehrt die Vermutung
   der Idee um, und zwar aus drei Gründen. (a) Es ist eine Konstante je Typ, also eine Zeile, und sie wirkt
   sofort in jeder bestehenden DB, während eine Seed-Änderung nur frische erreicht. (b) Der Typ benennt ein
   *Verfahren* und kann darum präziser heißen („Vokabelkarten", „Grammatik-Aufgaben"), während die Art der
   freie Ordnungsbegriff des Nutzers ist — dem sollte man sein natürliches Wort nicht wegnehmen. (c) Die
   Typen sind schon heute uneinheitlich benannt: „Rechen-Drill" und „Birkenbihl" nennen das Verfahren,
   „Vokabeln" und „Grammatik" nennen den Stoff.
2. **Reicht es, die Werte zu entzerren?** Empfehlung: **nein.** Selbst mit eindeutigen Werten bliebe
   `PlanPositions.tsx:431-434` eine Kette namenloser Fragmente. Die Art gehört dort gekennzeichnet (etwa
   „Art: Grammatik") oder optisch abgesetzt. Das ist die Hälfte, die den gemeldeten Schaden wirklich behebt.
3. **Zieht `VaterWizard` mit?** Empfehlung: ja, sichtbare Beschriftungen wie in `ExerciseFilterBar` — zwei
   nebeneinanderliegende Pulldowns, deren Achse nur im `aria-label` steht, sind für Sehende schlechter
   beschriftet als für Screenreader-Nutzer.
4. **Ist „Art" überhaupt der richtige Begriff?** Der Code heißt `ExerciseCategory`, das UI „Art", die
   Entity-Doku nennt es „controlled vocabulary per subject as the basis for pre-filtering". „Art" ist das
   allgemeinste Wort, das die Sprache hergibt, und kollidiert damit fast zwangsläufig. Empfehlung: in der
   Grill-Runde mitentscheiden, aber **nicht** zur Bedingung dieser Story machen — eine Umbenennung der
   Achse ist teurer als die Entzerrung der Werte.
5. **Vor oder nach B-155?** Empfehlung: **vorher entscheiden**, spätestens gemeinsam. Wenn B-155 sein
   „Grammatik-Thema" baut, während ein Typ und eine Art „Grammatik" heißen, entsteht die dritte Kollision
   sehenden Auges — und B-155 ist `L` und wird das nicht nebenbei mitziehen.

## Akzeptanzkriterien (Entwurf)

1. Kein Übungstyp-Anzeigename ist identisch mit einem geseedeten Art-Namen; „Leseverstehen" und
   „Leseverständnis" sind nicht mehr verwechselbar.
2. In der Auswahlliste des Planbaus ist erkennbar, welches Fragment die Art benennt.
3. Im Assistenten tragen der Art- und der Typ-Filter eine sichtbare Beschriftung.
4. Der Typ-Anzeigename kommt weiter **ausschließlich** aus dem Manifest — keine Tabelle im Frontend
   (`frontend/CLAUDE.md`), und `e2e/uebungstypen.spec.ts` bleibt grün.
5. Ein Test hält die Nicht-Kollision, statt sie einmalig herzustellen — sonst heißt der nächste neue Typ
   wieder wie eine Art.

## Verlauf

- **2026-08-13** — angelegt beim Grillen von
  [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md) (Entscheidung 5). **Bewusst nicht dort
  mitgenommen:** B-157 ist eine Eigentums-Story, ihr Ziel ist ohne die Entzerrung erfüllt, und eine
  Umbenennung von Produktinhalt daran zu hängen hätte ihre Akzeptanzkriterien unscharf gemacht.
- **2026-08-13** — Prio **P3 → P2** und damit vorgezogen, auf Entscheid des Nutzers. Der Grund ist nicht
  gestiegene Wichtigkeit, sondern ein **geschlossenes Fenster**: Mit der Abnahme von B-157 am selben Tag
  sind die sieben Seed-Arten fail-closed — `PATCH` liefert für **jeden** `403 not_owner`.
- **2026-08-13** — `idee → ausformuliert`. Die Recherche hat drei Dinge verschoben: **(1)** Es sind nicht
  zwei Kollisionen, sondern **fünf von sieben** geseedeten Arten — „Grammatik" war übersehen und ist die
  folgenreichste. **(2)** Der Schaden sitzt nicht im Filter, sondern in `PlanPositions.tsx:431-434`, wo Typ
  und Art punktgetrennt in *einer* Zeile stehen; der Seed erzeugt damit heute „Begrüßungen · Vokabeln ·
  Vokabeln" und sogar „Vokabeln: En ville · Vokabeln · Vokabeln". **(3)** Die Empfehlung dieser Story wird
  damit **umgekehrt**: nicht der Art-Name weicht aus, sondern das **Typ-Label** — es ist eine Code-Konstante
  und wirkt sofort in jeder bestehenden DB, während der Art-Name in der Datenbank liegt und seit B-157 an
  geseedeten Fächern unveränderbar ist. Dazu gefunden: eine **dritte** Achse mit demselben Wort ist mit
  B-155 unterwegs. `unverifiziert` entfernt.
