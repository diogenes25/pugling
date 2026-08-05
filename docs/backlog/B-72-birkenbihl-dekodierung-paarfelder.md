---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Dekodierung Paar-Felder, Wort:wörtlich]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-69 (Entscheidung 2)
---

# B-72 · Die Birkenbihl-Dekodierung trägt zwei Trennzeichen in einem Feld

## User Story

Als **Creator** möchte ich die Wort-für-Wort-Dekodierung eines Birkenbihl-Satzes **paarweise** eintragen
(ein Feld je Wort, ein Feld je wörtlicher Glosse), damit weder ein Komma noch ein Doppelpunkt in einer
Glosse verschluckt wird und die eingetragenen Paare beim Zurücklesen unverändert wiederkommen.

## Ist-Stand am Code

### Der Schreibweg: ein String, zweifach zerlegt

[exerciseConfig.tsx:88](../../frontend/src/vater/exerciseConfig.tsx) hält die Zeile für eine
Birkenbihl-Zeile als `{ text: "", decoding: "", naturalTranslation: "" }` — `decoding` ein **einzelner
String**. Eingegeben wird er über ein einzelnes `RowField` mit Platzhalter „What:Was, is:ist"
([:541](../../frontend/src/vater/exerciseConfig.tsx)). Beim Senden zerlegt `buildTypeConfig`
([:199-207](../../frontend/src/vater/exerciseConfig.tsx)) ihn **zweifach**: erst `split(",")` in Paare,
dann jedes Paar `split(":")` in Wort und Glosse:

```tsx
decoding: (r.decoding ?? "").split(",").map((p: string) => p.split(":"))
  .filter((kv: string[]) => kv[0]?.trim())
  .map((kv: string[]) => ({ learningWord: kv[0].trim(), gloss: (kv[1] ?? "").trim() || null })) })) };
```

Ein Komma **oder** ein Doppelpunkt in Wort oder Glosse zerreißt den Eintrag lautlos in zusätzliche,
falsche Paare — bei einer wörtlichen Übersetzung (Umschreibungen wie „ist im Begriff zu, …") ein
realistischer Fall, kein Rand.

Der Vertrag führt die Dekodierung längst als Liste getippter Paare:
`BirkenbihlSentence.Decoding` = `List<WordPair>`
([ExerciseConfigs.cs:249](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)), `WordPair` =
`(WordId, LearningWord, Gloss, VocabularyId, Self)`
([ExerciseConfigs.cs:257](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)). Der generische
Create-Endpunkt nimmt die volle Config entgegen und vergibt fehlende `SentenceId`/`WordId` serverseitig
(`BirkenbihlController.NormalizeConfig`,
[ExerciseControllers.cs:443-455](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)) —
am Vertrag und am Wire-Format ändert sich durch diese Story nichts, nur an der **Quelle** der Liste im
Editor.

### Der Leseweg existiert, wird aber von keiner Oberfläche gerendert

`configToEditorState` hat einen Birkenbihl-Zweig, der die Paare zu genau demselben String zurückfaltet
(`.join(", ")`, [exerciseConfig.tsx:292-299](../../frontend/src/vater/exerciseConfig.tsx)). Er läuft aber
**ins Leere**: Birkenbihl steht bewusst nicht in `CONTENT_EDITABLE`
([:43-46](../../frontend/src/vater/exerciseConfig.tsx)), mit Begründung im Kommentar
([:38-41](../../frontend/src/vater/exerciseConfig.tsx)) — Satz-/Wort-Ids hängen am
Wort-Austausch-Endpunkt und an der Store-Bindung, ein Vollersatz würde sie neu vergeben.
`ExerciseEditModal.tsx:67-75` zeigt für diesen Typ nur einen Hinweis-Banner statt des `ConfigEditor`; der
Birkenbihl-Zweig von `configToEditorState` wird also **nie gerendert** — nur `buildTypeConfig` läuft, und
zwar ausschließlich beim **Anlegen** (`VaterExerciseCreate.tsx:125`). Damit ist das gesuchte Feld real,
aber sein Rundlauf schon heute halb totes Gleis.

### Die automatische Dekodierung gegen den Vokabelspeicher ersetzt das Feld nicht

`BirkenbihlController` bietet bereits vokabelspeicher-gestützte Endpunkte: `POST …/birkenbihl/{id}/sentences`
(zerlegt automatisch und speichert), `PUT …/words/{wordId}` (Wort-Austausch bei Homonymen) und das
zustandslose `POST creator/birkenbihl/decode`
([ExerciseControllers.cs:480-623](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)).
Sie stehen im OpenAPI-Dokument und in `Pugling.Client` (für KI-Agenten), aber eine Volltextsuche über
`frontend/src` nach `birkenbihl` findet **keinen** Aufruf dieser Routen — nur `contract.ts` (generierte
Typen), `exerciseConfig.tsx` (das manuelle Feld), einen Kommentar in `RepeatedTextFields.tsx`, `uiTypes.ts`
und eine unabhängige String-Erwähnung in `SohnHome.tsx`. Das Vater-Web hat also **keinen** Weg zur
automatischen Dekodierung; das getippte „Wort:wörtlich"-Feld ist der einzige Weg, überhaupt eine
Dekodierung anzulegen. Die in der Idee offene Frage („könnte die Story erledigen, bevor sie gebaut wird")
ist damit beantwortet: **nein**, der Defekt ist aktuell und trifft den einzigen vorhandenen Weg.

### Keine Pflichtprüfung, kein Test

`contentProblem` verlangt für Birkenbihl nur Satz **und** natürliche Übersetzung
([exerciseConfig.tsx:631](../../frontend/src/vater/exerciseConfig.tsx)) — eine leere Dekodierung lässt sich
anlegen. Kein Unit-Test (`exerciseConfig.test.ts` hat keinen Birkenbihl-Fall) und kein E2E
(`uebungstypen.spec.ts` hat keine Erwähnung von „Birkenbihl"/„Dekodierung") berühren dieses Feld.

## Die echte Lücke

Derselbe Fehler wie in [B-69](B-69-wiederhol-felder-alternativen.md), nur mit **zwei** verschachtelten
Trennzeichen statt einem: ein Vertrag, der eine strukturierte Liste führt (`List<WordPair>`), trifft auf
ein Editor-Feld, das genau **einen** String hält. B-69 konnte das mit `RepeatedTextFields` lösen (ein
Trennzeichen, ein Wert je Feld); hier braucht jede Zeile ein **Paar** (Wort + Glosse), keinen Einzelwert —
`RepeatedTextFields` passt darum nicht direkt, eine Paar-Form muss dazukommen.

Zwei Dinge verengen den Zuschnitt gegenüber der ursprünglichen Vermutung:

1. Der Fehler trifft **nur den Schreibweg**. Der spiegelbildliche Leseweg (`configToEditorState`) existiert
   im Code, wird aber von keiner Oberfläche aufgerufen — Birkenbihl ist von `CONTENT_EDITABLE`
   ausgeschlossen. Diese Story kann also nicht an einem laufenden Bearbeiten-Formular geprüft werden,
   sondern nur über den Anlege-Weg und über Unit-Tests, die beide Richtungen direkt aufrufen.
2. Die automatische, vokabelspeicher-gestützte Dekodierung ist ein **eigener**, größerer, unbenutzter Weg
   (Wunsch, nicht Defekt) und macht diese Story nicht überflüssig — sie ändert nichts am getippten Feld.

## Offene Punkte

1. ~~Neue Komponente oder Erweiterung von `RepeatedTextFields`?~~ → **E1.**
2. ~~Wird `configToEditorState`s toter Birkenbihl-Zweig mitgezogen, obwohl ihn keine Oberfläche rendert?~~ → **E2.**
3. ~~Erzwingt `contentProblem` künftig mindestens ein nicht-leeres Wortpaar?~~ → **E3.**
4. ~~Bleibt die Prio bei P3?~~ → **E4.**

## Entscheidungen

Weil der Nutzer diese Sitzung ausdrücklich autorisiert hat, `gegrillt`/`geschätzt` mit bester Empfehlung
selbst zu entscheiden (kein Dialog-Termin), stehen hier vier Entscheidungen mit Begründung und Kosten statt
einer Grill-Runde.

**E1 · Eigene Komponente `RepeatedPairFields`, keine Erweiterung von `RepeatedTextFields`.**
*Begründung:* `RepeatedTextFields` führt `values: string[]` — ein Wert je Zeile. Ein Wortpaar ist **zwei**
Werte je Zeile (Wort + Glosse); das Prop auf ein Tupel-Array umzustellen bräche das bestehende Interface für
alle fünf heutigen Aufrufer (die drei Alternativen-Felder, Auswahl, Wortpool) ohne Nutzen für sie. Eine
eigene, kleine Komponente nach demselben Muster (ein Zeilen-Paar, „+ Wortpaar", Entfernen, Fokus aufs neue
Feld) hält beide Formen einfach. *Kosten:* eine neue Datei/Exportfunktion plus ein eigener Komponententest,
statt eine bestehende zu erweitern.

**E2 · `configToEditorState`s Birkenbihl-Zweig wird auf die neue Form gebracht, obwohl er unerreichbar
bleibt.** *Begründung:* Repo-Konvention ist der Rundlauf — was `buildTypeConfig` schreibt, muss
`configToEditorState` zurücklesen, sonst verliert ein künftiges „Birkenbihl editierbar machen" (Terrain von
[B-78](B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md)) sofort wieder Inhalte, weil die Gegenrichtung
seit dieser Story nicht mitgezogen wurde. *Kosten:* ein paar Zeilen Code, die im UI heute niemand sieht —
geprüft wird der Zweig ausschließlich über `exerciseConfig.test.ts`, nicht über einen Bildschirm.

**E3 · Keine neue Pflichtprüfung für ein nicht-leeres Wortpaar — zurückgestellt.** *Begründung:* Das ist
eine Inhalts-Vollständigkeitsfrage („bekommt die Übung überhaupt eine Dekodierung"), nicht der
Trennzeichen-Defekt dieser Story — genau das Terrain von B-78 („Die Birkenbihl-Dekodierung erreicht das
Kind nicht"). Beide in einem Fix zu vermischen macht den Regressionstest dieser Story unscharf: er soll
zeigen, dass ein Komma/Doppelpunkt nicht mehr zerreißt, nicht, dass eine leere Dekodierung neu abgelehnt
wird. *Kosten:* Übungen mit leerer Dekodierung bleiben anlegbar, bis B-78 das klärt.

**E4 · Prio bleibt P3.** *Begründung:* Eine Position, ein betroffenes Feld, nachweislich noch nie über eine
Oberfläche gespielt oder getestet (kein E2E, kein Unit-Test, kein Rundlauf über ein Bearbeiten-Formular) —
ein schmalerer Fußabdruck als B-69, das ein im Normalfall unbenutzbares Feld betraf. *Kosten:* keine —
reine Bestätigung des Status quo.

## Akzeptanzkriterien

1. Ein Wortpaar, dessen Wort **oder** Glosse selbst ein Komma oder einen Doppelpunkt enthält, lässt sich
   eintragen, anlegen (`POST`) und kommt beim Server als **ein** `WordPair` an — nicht zerrissen in
   mehrere.
2. Die Dekodierung wird über eine neue Komponente `RepeatedPairFields` eingegeben: zwei Felder je Zeile
   (Wort der Lernsprache, wörtliche Glosse), „+ Wortpaar", Entfernen je Zeile — kein einzelnes Textfeld mit
   Trennzeichen mehr.
3. `configToEditorState`s Birkenbihl-Zweig liest dieselbe Paarliste unverändert zurück (geprüft per
   Rundlauf-Test, auch ohne renderndes UI).
4. `emptyRow("Birkenbihl").decoding` ist eine leere Liste von Paaren, kein leerer String.
5. Ein Vitest deckt den Rundlauf `buildTypeConfig` → `configToEditorState` mit einem Komma- **und** einem
   Doppelpunkt-haltigen Wert je Feld (Wort und Glosse) ab.
6. Ein Komponententest für `RepeatedPairFields` (Zeile hinzufügen, tippen, entfernen), analog
   `RepeatedTextFields.test.tsx`.
7. `npm run build`, Vitest grün, Agent `frontend-reviewer` durch.

## Schätzung

**S · `wo: frontend` · `migration: nein` · `vertragsbruch: nein`**

Beide Flags nachgesehen: Der Angriffsplan fasst keine `.cs`-Datei an — der Vertrag führt `List<WordPair>`
bereits, der generische Create-Endpunkt normalisiert Ids serverseitig, und am Wire-Format des `POST` ändert
sich nichts; nur die **Quelle** der gesendeten Liste im Editor wechselt von String-Split auf ein direkt
gepflegtes Array. Also kein Schema, keine Migration, kein `Pugling.Client`.

**S** gegen den Anker (`childId` aus dem Test-Pfad ziehen, B-01): eine neue, kleine Komponente nach
bestehendem Muster, **ein** Feld in **einer** Datei (kein zweiter Editor wie bei B-69s `ClozeTexts.tsx`,
weil Birkenbihl nicht editierbar ist), keine Beschriftungs-Vereinheitlichung über mehrere Orte und kein
bestehender E2E zum Nachziehen. Kein M: B-69 traf fünf Felder in zwei Dateien mit Beschriftungs- und
E2E-Arbeit; hier ist es eine Feld-Ersetzung plus eine neue Komponente.

### Angriffsplan

„Backend zuerst" entfällt — es gibt keine Backend-Arbeit. Die Reihenfolge folgt dem Defekt: erst der Test,
der rot ist, dann der Umbau, der ihn grün macht.

1. **Regressionstest, rot** (`exerciseConfig.test.ts`, neu): `buildTypeConfig("Birkenbihl", …)` mit einer
   heutigen Zeile aufrufen, deren `decoding`-String ein eingebettetes Komma in der Glosse trägt (z. B.
   `"is:isst, gerade"`) — erwartet **ein** Paar, der heutige `split(",")` liefert zwei. Scheitert am
   aktuellen Code.
2. **`RepeatedPairFields`** (neue Komponente, neben `RepeatedTextFields.tsx` oder in derselben Datei): zwei
   Felder je Zeile (Wort, Glosse), „+ Wortpaar", Entfernen, Fokus aufs neu angelegte Feld — Muster wie
   `RepeatedTextFields.tsx:19-95`.
3. **`Row.decoding` auf `{ word: string; gloss: string }[]` umstellen**: `emptyRow("Birkenbihl")`
   ([:88](../../frontend/src/vater/exerciseConfig.tsx)) liefert `[]` statt `""`; `buildTypeConfig`
   ([:199-207](../../frontend/src/vater/exerciseConfig.tsx)) baut die `WordPair`-Liste direkt aus dem Array
   (kein `split` mehr); `configToEditorState`
   ([:292-299](../../frontend/src/vater/exerciseConfig.tsx)) liest die Paare direkt zurück (kein `.join`
   mehr).
4. **`ConfigEditor`** ([:539-543](../../frontend/src/vater/exerciseConfig.tsx)): das einzelne `RowField`
   „Dekodierung (Wort:wörtlich, …)" durch `RepeatedPairFields` ersetzen, `scope` = `Satz ${i + 1}`.
5. **Rundlauf-Vitest** in `exerciseConfig.test.ts`: `buildTypeConfig` → `configToEditorState` für
   Birkenbihl, mit einem Komma- und einem Doppelpunkt-haltigen Wert.
6. **Komponententest** `RepeatedPairFields.test.tsx`, analog `RepeatedTextFields.test.tsx` (Zeile
   hinzufügen/tippen/entfernen, Fokus aufs neue Feld).
7. `npm run build`, Vitest, Agent `frontend-reviewer`.

### Risiken

- **Der Leseweg bleibt ungeprüft durch eine echte Oberfläche.** Solange Birkenbihl nicht in
  `CONTENT_EDITABLE` steht, sichert einzig der Unit-Test in Schritt 5 den Rundlauf ab — eine Regression
  bliebe im Betrieb unsichtbar, bis B-78 Birkenbihl editierbar macht. Kein Zusatzrisiko, das *diese* Story
  einführt, aber ein bestehendes, das sie nicht schließt.
- **Kein bestehender E2E zum Nachziehen** (anders als bei B-69) — dafür auch keiner, der brechen kann.
- **Die Falle mit `getByLabel`.** Zwei neue, wortpaar-eigene Accessible Names je Zeile (`Wort N (Satz M)`,
  „wörtlich N (Satz M)") — Tests brauchen ggf. `{ exact: true }`, wie in `frontend/CLAUDE.md` beschrieben.
- **Was nicht droht:** `contentProblem` ändert sich nicht (E3), also kein neues Ablehnungsverhalten, das
  eine bestehende Übung nachträglich ungültig machen könnte.

### Testweg

| Was | Wo |
| --- | --- |
| Regression (Komma/Doppelpunkt in Wort/Glosse) | `frontend/src/vater/exerciseConfig.test.ts` — **neu**, vorher rot |
| Komponente selbst | `frontend/src/components/RepeatedPairFields.test.tsx` — **neu** |
| Abnahme | `npm run build`, `npm test`, Agent `frontend-reviewer` |

**Kein `/smoke-test` und kein Backend-Test**: keine Routen-, DTO- oder Serververhaltensänderung — das
Wire-Format des `POST` bleibt, wie es ist.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-69, Entscheidung 2.
- **2026-08-03** — ausformuliert am Code (autonom getroffen, Nutzerauftrag 2026-08-04). Die offene Frage der
  Idee ist beantwortet: Die vokabelspeicher-gestützte automatische Dekodierung
  (`BirkenbihlController.AddSentence`/`SetWord`/`birkenbihl/decode`) hat **keinen** Aufrufer im Frontend —
  das getippte „Wort:wörtlich"-Feld ist der einzige Weg im Vater-Web, eine Dekodierung anzulegen, der Defekt
  ist also aktuell. Neuer Fund: Birkenbihl ist von `CONTENT_EDITABLE` ausgeschlossen
  (Satz-/Wort-Ids gehören dem Wort-Austausch-Endpunkt), darum läuft der spiegelbildliche Leseweg in
  `configToEditorState` heute ins Leere — kein Bearbeiten-Formular rendert ihn. Der Zuschnitt bleibt eine
  Story, verengt sich aber auf den Schreibweg plus (aus Rundlauf-Konvention) den mitgezogenen, aber
  unerreichbaren Leseweg.
- **2026-08-03** — gegrillt (autonom getroffen, Nutzerauftrag 2026-08-04). Vier Entscheidungen, keine
  zurückgestellt: eigene Komponente `RepeatedPairFields` statt Erweiterung von `RepeatedTextFields` (E1),
  der tote Leseweg wird trotzdem mitgezogen (E2), eine Pflichtprüfung auf ein nicht-leeres Wortpaar bleibt
  außen vor und wandert gedanklich zu B-78 (E3), Prio bleibt P3 (E4).
- **2026-08-03** — geschätzt (autonom getroffen, Nutzerauftrag 2026-08-04): **S**, `wo: frontend`,
  **migration: nein**, **vertragsbruch: nein** — beides nachgesehen, der Angriffsplan fasst keine
  `.cs`-Datei an. Sieben Schritte, beginnend mit dem roten Regressionstest gegen den heutigen `split(",")`.
  Größter Unterschied zu B-69: kein zweiter Editor und kein bestehender E2E, dafür bleibt der Leseweg ohne
  Oberfläche ungeprüft — Testweg ist darum zwei neue Vitest-Dateien, `/smoke-test` entfällt begründet.
- **2026-08-05** — im Autonomen Modus gebaut, ohne Rückfrage je Ticket, exakt nach Angriffsplan: neue
  Komponente `RepeatedPairFields` (zwei Felder je Zeile, „+ Wort", Entfernen, Fokus aufs neue Wort-Feld),
  `Row.decoding` von `string` auf `{word, gloss}[]` umgestellt (`emptyRow`, `buildTypeConfig` via neuem
  `nonEmptyPairs`, `configToEditorState`), `ConfigEditor` nutzt einen neuen `RowRepeatedPairField`-Wrapper.
  Rote Probe zuerst: der neue Rundlauf-Testfall in `exerciseConfig.test.ts` (Wort **und** Glosse mit Komma
  *und* Doppelpunkt) warf gegen den Vorzustand `TypeError: split is not a function`, weil die neue
  Array-Form auf den alten String-Code trifft – danach grün. `npm run build` sauber, `npm test`
  **148/148 grün** (141 + 7 neue). `frontend-reviewer` fand einen echten 🟡-Befund: anders als der
  (unerreichbare) Leseweg ist der Birkenbihl-**Schreibweg** live (`VaterExerciseCreate.tsx` rendert
  `ConfigEditor` für jeden Typ in einem echten `<form>`) – `RepeatedPairFields` fehlte darum der
  Enter-Schutz, den `RepeatedTextFields` für genau dieses Formular schon trägt; ein Enter im Wortpaar-Feld
  hätte die Übung vorzeitig abgeschickt. Behoben (gleicher `onKeyDown`-Schutz wie im Vorbild, neuer Testfall),
  dazu ein 🟢-Fund (`disabled` fehlte an den Inputs, nur an den Buttons) mitbehoben. Commit `<hash>`.
  Status → `abgenommen`.
