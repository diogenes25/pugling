---
tags: [typ/story, status/gegrillt, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Freitext-Fach nicht wegzubekommen, Schulart-Kombination geht verloren, Formular kennt Zustand nicht]
status: gegrillt
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-137
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-143 · Das Reihen-Formular kennt zwei Zustände nicht, die das Modell erlaubt

Abgespalten von [B-137](B-137-freitext-fach-unerreichbar.md) (dessen Punkte 1 und 5). Zweimal dieselbe
Fehlerklasse, ein Feld auseinander — und beide Male entscheidet **die Oberfläche**, nicht der Code, wie
die Lösung aussieht.

## User Story

Als **Creator** möchte ich, dass das Bearbeiten-Formular jeden Zustand ausdrücken kann, in dem eine Reihe
tatsächlich sein darf — sonst zerstöre ich beim Speichern etwas, das ich gar nicht angefasst habe.

## Ist-Stand am Code

**Zustand A — Freitext-Fach ohne Fach-Id.** Entsteht ohne Zutun (siehe
[B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md)): die Zeile zeigt „Englisch" (Rückfall auf
`subjectName`, `VaterLehrwerke.tsx:135-137`), das Formular zeigt „– keine Angabe –", weil es nur das
Katalog-`<select>` hat. Und der Freitext ist nicht wegzubekommen:
`loaded.subjectId === form.subjectId === ""` ⇒ kein Diff ⇒ weder `clearSubject` noch `subjectName` gehen
mit (`seriesPatch.ts`). Der Nutzer liest „Gespeichert." und „Englisch" steht weiter da.

**Zustand B — Schulart-Kombination.** `SchoolTypes` ist ein `[Flags]`-Enum, eine Kombination
(„Realschule, Gymnasium") reist als freier String (B-60). Das Formular hat dafür keine Option: der
`<select>` zeigt leer, der Nutzer greift zu „– für alle –", und `"None"` **löscht** die Kombination. Vor
B-123 gab es diesen Weg für Reihen nicht.

Beides ist dieselbe Aussage: *das Formular kennt einen Zustand nicht, den das Modell erlaubt* — und in
beiden Fällen ist die Folge nicht „man kann etwas nicht", sondern „man zerstört etwas, ohne es zu merken".

## Die echte Lücke

Nicht das fehlende Bedienelement, sondern die **stille Zerstörung**: In beiden Fällen meldet die App
Erfolg. Das ist die Fehlerklasse, gegen die die PATCH-Regel der Root-`CLAUDE.md` geschrieben ist („ohne
den Schalter meldet eine Oberfläche mit ‚– keine Angabe –' fröhlich ‚Gespeichert.'") — hier eine Ebene
höher, weil nicht der Schalter fehlt, sondern die Anzeige des Zustands, für den er gedacht ist.

## Am 2026-08-10 nachgemessen

Der Ist-Stand stammte vom `frontend-reviewer` und ist **älter als die Änderung an `seriesPatch.ts` aus
B-142**. Nachgeprüft, weil eine Story nicht auf einer überholten Messung gegrillt wird:

- **Zustand A trägt weiter.** Der `clearSubject`-Zweig ist unverändert (`seriesPatch.ts:79-82`), das
  Fach-`<select>` kennt nur Katalog-Optionen (`VaterLehrwerke.tsx:287`), und die Zeile fällt weiter auf
  `series.subjectName` zurück (`:137`). Der Freitext bleibt unerreichbar.
- **Zustand B trägt ebenfalls** — das Bearbeiten-`<select>` hat `"None"` plus sechs Einzelwerte
  (`:294-298`), während die Zeile die Kombination roh anzeigt (`:153`).
- **Aber sein Gewicht ist geringer als gedacht:** An einer *Reihe* kann eine Kombination heute **nur über
  die API** entstehen. Der Seed setzt Kombinationen an Übungen (`Seed.cs:637,663`), nicht an Reihen, und
  die einzige Mehrfachauswahl im Frontend steht im Übungs-Formular. Zustand B ist damit **latent**: real,
  aber vom Vater heute nicht auslösbar. Zustand A trifft ihn dagegen sofort, sobald er ein Fach löscht.
- **Ein Fund nebenan**, ausgelagert als [B-146](B-146-anlegeformular-schickt-toten-fachnamen.md): Das
  Anlege-Formular schickt weiterhin einen `subjectName`, den der Server seit B-142 ignoriert.

Die Messung hat außerdem die Diagnose geschärft: Es sind **nicht zwei Defekte, sondern einer an zwei
Stellen** — die Zeile zeigt zweimal einen Zustand, den das Formular nicht ausdrücken kann.

## Offene Punkte

1. ~~**Wie drückt das Formular den Freitext aus?**~~ → Entscheidungen 1 und 2.
2. ~~**Wie überlebt die Schulart-Kombination?**~~ → Entscheidung 3.

## Entscheidungen

1. ~~**Eine `.sub`-Zeile unter dem Feld plus ein „entfernen"-Knopf.**~~ **Revidiert noch in derselben
   Runde**, und der Weg dahin gehört ins Protokoll: Beim Durchdenken der Schulart-Seite zeigte sich, dass
   die `.sub`-Zeile dort **überflüssig und schlechter** wäre — die Kombination lässt sich direkt im
   `<select>` ausdrücken. Der ersten Fassung fehlte also genau die Probe am zweiten Fall.
2. **Beide Zustände leben in ihrem eigenen `<select>`, als vorausgewählte, deaktivierte Option.** Bei der
   Schulart: „Realschule, Gymnasium" statt eines leeren Feldes. Beim Fach: „Englisch (Freitext)".
   Begründung: Das Feld sagt damit die Wahrheit statt zu schweigen — und der Schutz vor versehentlichem
   Speichern fällt **ohne jede zusätzliche Mechanik** ab, weil `form` gleich `loaded` bleibt, solange
   niemand aktiv etwas anderes wählt. `seriesPatch` schickt dann nichts. **Kosten:** Das Formularmodell
   braucht einen Sonderwert für „Freitext-Fach liegt vor" (etwa `subjectId: "__freetext__"`), den
   `seriesPatch` kennen muss — Mechanik, die man erklären muss, aber an einer Stelle und mit einem Vitest.
3. **Der Freitext wird über die bestehende Option „– keine Angabe –" entfernbar, nicht über einen neuen
   Knopf.** Wechselt der Nutzer vom Sonderwert auf `""`, ist das ein Unterschied → `clearSubject` geht mit,
   und der Server räumt Id und Namen gemeinsam (den Schalter gibt es seit B-123). Begründung: Das Formular
   hat für „nichts" bereits eine Option; ein zweites Bedienelement daneben wäre eine zweite Art, dasselbe
   zu sagen. **Kosten:** Der Weg ist weniger auffällig als ein Knopf mit Beschriftung — wer den Freitext
   loswerden will, muss auf die Idee kommen, „– keine Angabe –" zu wählen. Ein Satz Hilfetext gleicht das
   aus.
4. **Die Schulart bekommt bewusst *keine* Entfern-Aktion.** Bei ihr ist „– für alle –" (`None`) ein
   *echter Wert*, nicht „nichts" — wer ihn wählt, meint ihn. Der Schutz besteht allein darin, dass die
   Kombination nicht mehr unbeabsichtigt verlorengeht. Begründung: Der Freitext ist ein **Unfall** (er
   entsteht, weil jemand ein Fach gelöscht hat) und soll wegräumbar sein; die Kombination ist eine
   **Absicht** und soll überleben. **Kosten:** Zwei Felder, die gleich aussehen und sich verschieden
   verhalten — das braucht je einen Satz an Ort und Stelle, sonst ist es eine Falle statt einer Hilfe.
5. **Zustand B bleibt in dieser Story, obwohl er latent ist.** Begründung: Nach Entscheidung 2 ist der
   Fix fast gratis — dieselbe deaktivierte Option, eine Stelle, kein eigenes Bedienelement. Und „kein UI
   erzeugt sie" ist eine Aussage über heute: Sobald Reihen-Metadaten in den KI-Creator oder die
   `.http`-Flows wandern, zerstört das Formular Daten, ohne dass jemand es mit dem Formular in Verbindung
   bringt. **Kosten:** Die Akzeptanzkriterien tragen einen Fall, den man ohne API-Aufruf nicht herstellen
   kann — der Test muss die Kombination über die API setzen, nicht über die Oberfläche.
6. **Eine echte Mehrfachauswahl bleibt draußen.** Sie wäre eine neue *Fähigkeit* (`Wunsch`), nicht die
   Behebung des Defekts; sie hier mitzunehmen ließe die stille Zerstörung warten, bis der Entwurf einer
   Mehrfachauswahl steht. **Kosten:** Eine Kombination bleibt an der Reihe weiterhin nur über die API
   setzbar — die Oberfläche schützt einen Wert, den sie selbst nicht erzeugen kann. Das ist ungewöhnlich
   genug, um es in den Kriterien zu benennen.

## Akzeptanzkriterien

1. Trägt eine Reihe einen Fachnamen ohne Fach-Id, zeigt das Fach-`<select>` diesen Namen als
   vorausgewählte, nicht wählbare Option — Zeile und Formular sagen dasselbe.
2. Der Freitext lässt sich entfernen, indem „– keine Angabe –" gewählt und gespeichert wird; danach sind
   Id **und** Name weg.
3. Ein Speichern, das die Schulart **nicht** anfasst, lässt eine Kombination unverändert — auch dann,
   wenn andere Felder geändert wurden.
4. Wählt der Nutzer bei der Schulart aktiv einen Wert (auch „– für alle –"), wird dieser gespeichert.
   Der Schutz gilt dem Versehen, nicht der Absicht.
5. Vitests über `seriesPatch` für die drei Fälle aus 2–4; der Kombinations-Fall stellt seinen
   Ausgangszustand **über die API** her, weil die Oberfläche ihn nicht erzeugen kann (Entscheidungen 5
   und 6).
6. Ein E2E-Fall, der Zustand A auf dem gewöhnlichen Weg herstellt (Fach löschen) und über die Oberfläche
   auflöst.

## Verlauf

- **2026-08-10** — abgespalten von [B-137](B-137-freitext-fach-unerreichbar.md) im Nachtlauf (Sprint 2),
  weil B-137 faktisch XL war. Hier liegen die Punkte, die **nur im Dialog** fallen können; der Lauf hält
  an ihnen an und entscheidet sie nicht.
- **2026-08-10** — **gegrillt** im Dialog mit dem Nutzer (sechs Entscheidungen, davon eine revidiert).
  Der Ist-Stand wurde vorher nachgemessen, weil er älter war als die B-142-Änderung an `seriesPatch.ts` —
  er trägt weiter, aber Zustand B ist **latent** (an einer Reihe nur über die API erzeugbar).
  Die Runde hat die Diagnose geschärft: ein Defekt an zwei Stellen, nicht zwei Defekte. Entscheidung 1
  wurde **noch in derselben Runde zurückgenommen**, weil die vorgeschlagene `.sub`-Zeile am zweiten Fall
  scheiterte — die gefundene Form (deaktivierte Option im `<select>`) trägt beide und braucht kein
  eigenes Bedienelement. Nebenbei ausgelagert:
  [B-146](B-146-anlegeformular-schickt-toten-fachnamen.md).
  Voraussichtlich `S`, `wo: frontend`, `migration: nein` — zu bestätigen beim Schätzen.
