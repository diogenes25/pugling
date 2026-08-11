---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Freitext-Fach nicht wegzubekommen, Schulart-Kombination geht verloren, Formular kennt Zustand nicht]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
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

## Schätzung

**Größe `S`** — der Anker ist B-01 (`childId` aus dem Test-Pfad ziehen). Die Rechtfertigung liegt darin,
dass Entscheidung 2 die Mechanik **fast wegdefiniert** hat: Der Schutz vor dem versehentlichen Speichern
fällt aus dem bestehenden Vergleich ab, weil `form` gleich `loaded` bleibt, solange niemand aktiv wählt.
`seriesPatch.ts:66` vergleicht die Schulart schon heute so (`form.schoolTypes !== loaded.schoolTypes`) —
für Zustand B ist **keine Zeile** in der Regel zu ändern, nur eine `<option>` im Formular.

**Was sie auf `M` heben würde:** wenn Akzeptanzkriterium 6 einen eigenen E2E-Aufbau braucht statt eines
bestehenden. Gemessen: `frontend/e2e/lehrwerk-bearbeiten.spec.ts` fährt den Bearbeiten-Weg bereits, und
`creator-lehrwerk-weg.spec.ts` legt Fach und Reihe an — der neue Fall hängt sich an, statt eine Kulisse
neu zu bauen.

**`migration: nein`**, **`vertragsbruch: nein`** — nachgesehen: Der Sentinel lebt ausschließlich im
Formularmodell (`SeriesFormValues.subjectId`, ein `string`). Er erreicht den Server nie; `UpdateTextbookSeriesDto`
bleibt unverändert, und damit auch Client und `unknown_field`-Guards.

**Risiken, zwei — beide an derselben Stelle:**

1. **`Number("__freetext__")` ist `NaN`.** Der bestehende `else`-Zweig (`seriesPatch.ts:87`) macht aus
   jedem Nicht-Leerstring eine Zahl. Er ist heute unerreichbar für den Sentinel (die Option ist
   `disabled`, der Nutzer kann ihn nicht *wählen*) — aber „unerreichbar, weil das Formular es verhindert"
   ist genau die Art Zusicherung, die beim nächsten Umbau kippt. Der Sentinel gehört **vor** den Zweig
   abgefangen, nicht daneben.
2. **Der Sentinel darf nicht mit einer echten Fach-Id kollidieren.** `"__freetext__"` ist keine Zahl,
   also sicher — aber das ist eine Eigenschaft der Wahl, nicht des Typs. Ein Vitest hält sie fest.

**Angriffsplan** (rein Frontend, deshalb ohne Backend-Vorlauf):

1. `seriesPatch.ts` — Sentinel als exportierte Konstante, `seriesFormValues` setzt ihn, wenn
   `subjectId == null && subjectName != null`. Der `subjectId`-Zweig fängt ihn zuerst ab (Risiko 1).
2. `seriesPatch.test.ts` — die drei Fälle aus den Kriterien 2–4, dazu Risiko 2.
3. `VaterLehrwerke.tsx` — Fach-`<select>`: die deaktivierte Option, wenn der Sentinel geladen ist.
   Schulart-`<select>`: dieselbe Form, wenn `loaded.schoolTypes` weder `"None"` noch in `SCHOOL_TYPES`
   ist (`lib/labels.ts:31`).
4. Ein Satz Hilfetext in `lib/fieldHelp.ts` zum Fach-Feld — Entscheidung 3 hat ihn ausdrücklich als
   Ausgleich dafür eingekauft, dass der Entfern-Weg unauffällig ist, und Entscheidung 4 verlangt einen
   Satz dazu, warum die Schulart sich anders verhält.
5. Der E2E aus Kriterium 6.

**Testweg:** `frontend/src/vater/seriesPatch.test.ts` (Vitest) für die Regel; ein neuer Fall in
`frontend/e2e/lehrwerk-bearbeiten.spec.ts` für Zustand A auf dem gewöhnlichen Weg. Zustand B bekommt
**keinen** E2E — die Oberfläche kann seinen Ausgangszustand nicht herstellen (Entscheidungen 5 und 6),
er bleibt Vitest-Sache mit einem von Hand gesetzten Ladezustand.

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
- **2026-08-10** — **geschätzt** (`S`, `frontend`, `migration: nein`, `vertragsbruch: nein`). Die Größe
  hängt daran, dass Entscheidung 2 die Mechanik fast wegdefiniert hat: der bestehende Vergleich in
  `seriesPatch.ts:66` trägt Zustand B ohne eine geänderte Zeile. Zwei Risiken benannt, beide am
  `subjectId`-Zweig — `Number("__freetext__")` ist `NaN`, und die Kollisionsfreiheit des Sentinels ist
  eine Eigenschaft der Wahl, nicht des Typs; darum je ein Vitest.
- **2026-08-10** — **gebaut** im Nachtlauf (Sprint 3). Rote Probe vorher: **1 von 12** Vitests fiel —
  und zwar ein *bestehender*, der den Freitext-Zustand schon beschrieb (`subjectId: null` bei gesetztem
  `subjectName`) und dazu das alte Verhalten behauptete (`subjectId: ""`). Er ist nicht angepasst, sondern
  **aufgeteilt**: sein ursprünglicher Zweck („`null` wird zum leeren String") hat jetzt eine Vorlage ohne
  Fachnamen, der Freitext-Fall steht als eigener Fall daneben. Danach **18/18** in dieser Datei,
  **195/195** insgesamt, E2E **33/33** inklusive des neuen Falls, der Zustand A auf dem gewöhnlichen Weg
  herstellt (Fach anlegen → Reihe daran → Fach löschen → über die Oberfläche auflösen). Beide benannten
  Risiken sind abgedeckt: der Sentinel erreicht den `Number()`-Zweig nicht mehr, und seine
  Kollisionsfreiheit hat einen eigenen Fall.
- **2026-08-10** — **nach dem Frontend-Review nachgebessert**, drei Punkte. (1) Bedingung *und*
  Beschriftung beider Optionen kommen jetzt aus `series` statt aus `form` — der Angriffsplan hatte
  `loaded` gesagt, gebaut war `form`. Verhaltensgleich, aber mit `form` verschwindet der Ursprungswert aus
  dem Pulldown, sobald der Nutzer etwas anderes probiert; ein Zurück kostete dann das Zuklappen des
  Formulars mitsamt allen anderen Eingaben. (2) Der Hilfetext zur Schulart widersprach sich selbst
  („kannst du sie hier nicht ändern" … „gilt deine Wahl") — gemeint war „nicht zusammenstellen".
  (3) **Die Schulart-Option hatte keinen Test**: `seriesPatch` war dort nie kaputt, kaputt war die
  fehlende `<option>` — und die erreicht kein Vitest zur Regel und kein E2E (der Zustand ist über die
  Oberfläche nicht herstellbar). Dafür ist `SeriesForm` jetzt exportiert und hat fünf RTL-Fälle;
  rote Probe durch Neutralisieren beider Optionen: **2 von 5 rot**, die drei Negativfälle bleiben
  naturgemäß grün.
- **2026-08-10** — **abgenommen** (Commit `637478f`, Rollengang-Nachtrag `d4d3595`).
  Belegt: Backend **813/813**, Vitest **204/204**, Playwright **33/33**, `pugling-reviewer`
  und `frontend-reviewer` gelaufen, ihre Funde behoben oder als eigene Story abgelegt.
  **Rollengang teils im echten Browser** (Anmeldung als Papa, Vater-Web, Katalogseite),
  teils per dokumentiertem Ersatz: Alle Löschpfade hängen an `confirmAction`, und ein
  `window.confirm` blockiert die Chrome-Extension — ein injizierter Ersatz greift nicht, weil
  er in einer isolierten Welt läuft. Dafür stehen die Playwright-Spec (echter Browser, echter
  Dialog) und eine Live-Probe gegen die laufende API. Protokoll:
  [pm-sitzung-2026-08-10.md](../pm-sitzung-2026-08-10.md) → Nachtlauf, Sprint 3.
