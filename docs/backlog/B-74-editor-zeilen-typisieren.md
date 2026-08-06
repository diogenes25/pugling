---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Row typisieren, Record<string any>]
status: abgenommen
prio: P3
art: Aufräumen
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-69 (Entscheidung 5)
---

# B-74 · Die Zeilen des Übungs-Editors sind `Record<string, any>`

## User Story

Als **Entwickler** möchte ich, dass ein vergessener Aufrufer beim Ändern einer Zeilenform (z. B. `string`
→ `string[]`, wie in B-69) ein **Compilerfehler** ist statt eines stillen Laufzeitfehlers, damit die
mirror-Funktionen `buildTypeConfig`/`configToEditorState`/`emptyRow`/`emptyExtra` nicht mehr allein auf
Testabdeckung angewiesen sind, um auseinanderzulaufen.

## Ist-Stand am Code

### Eine gemeinsame lose Zeile für elf Übungstypen

[exerciseConfig.tsx:22](../../frontend/src/vater/exerciseConfig.tsx) definiert
`export type Row = Record<string, any>` mit einem `eslint-disable` direkt darüber – der Typ ist bewusst
so lose, nicht aus Versehen. Vier Funktionen greifen mit String-Schlüsseln hinein, jede mit einem
`switch (type)` über alle elf `AUTHORABLE_TYPES` ([:25-28](../../frontend/src/vater/exerciseConfig.tsx)):

| Funktion | Zeile | Tut |
| --- | --- | --- |
| `emptyRow` | [:80-95](../../frontend/src/vater/exerciseConfig.tsx) | leere Anfangszeile je Typ |
| `emptyExtra` | [:98-111](../../frontend/src/vater/exerciseConfig.tsx) | leere Extra-Felder je Typ |
| `buildTypeConfig` | [:123-209](../../frontend/src/vater/exerciseConfig.tsx) | Zeilen → Server-Config beim Senden |
| `configToEditorState` | [:227-301](../../frontend/src/vater/exerciseConfig.tsx) | Server-Config → Zeilen beim Laden |

Keine der vier prüft, ob eine Zeile die Felder trägt, die ihr `case`-Zweig erwartet – `r.alternatives`,
`r.decoding`, `r.choices` sind alle `any`. Der `Row`-Typ ist zusätzlich der öffentliche Vertrag von
`ConfigEditor` ([:321-554](../../frontend/src/vater/exerciseConfig.tsx), rendert je Typ eigene Felder aus
`r.…`) und von `RowField`/`RowRepeatedField`.

### Zwei Aufrufer halten Typ, Zeilen und Extra als drei getrennte States

`VaterExerciseCreate.tsx` ([:68-94](../../frontend/src/vater/VaterExerciseCreate.tsx)) und
`ExerciseEditModal.tsx` ([:281-287](../../frontend/src/vater/ExerciseEditModal.tsx)) halten je
`useState<ExerciseTypeKey>` (der Typ), `useState<Row[]>` (die Zeilen) und `useState<Row>` (Extra) **getrennt**
voneinander. Das ist die eigentliche Hürde für eine echte Union: TypeScript kann `rows`/`extra` nur dann
anhand von `type` diskriminieren (narrowen), wenn beide **aus demselben Objekt** stammen. Solange die drei
getrennte Variablen sind – und `Row` weiterhin strukturell mit `any`-Feldern kompatibel zu jeder denkbaren
Zeilenform ist – bliebe ein reiner Union-Typ am Aufrufer wirkungslos: Ein `Record<string, any>[]` ist
strukturell zu jeder Union-Alternative kompatibel, weil `any` mit allem verträglich ist. Eine Union, die an
dieser Stelle wirklich etwas verhindert, verlangt also, `type`+`rows`+`extra` zu **einem** State-Objekt zu
bündeln – das änderte Zustandsform und alle Handler (`patchRow`, `addRow`, `removeRow`, Typwechsel) in
**beiden** Aufrufern.

### Kein Spread der rohen Zeile in die Nutzlast (bestätigt durch B-24)

[B-24](B-24-frontend-unknown-field.md) hat am 2026-08-03 exakt diese Datei geprüft: Kein `case` in
`buildTypeConfig` spreadet `{ ...r }` roh in die Config – jeder baut sein Ergebnis feldweise aus benannten
Literalen (Beispiel `:138`: `{ prompt: r.prompt, answer: Number(r.answer), tolerance: … }`). Das
Restrisiko eines unbekannten Felds am Server ist damit ausgeschlossen; das **hier** offene Problem ist ein
anderes: nicht „schickt die Zeile ein Feld, das der Server nicht kennt", sondern „merkt der Compiler, wenn
ein `case`-Zweig ein Feld **falsch** oder **veraltet** liest".

### Der Anlassfall: B-69/E5, bewusst zurückgestellt

B-69 (abgenommen) hat fünf Felder von `string` auf `string[]` umgestellt und sich dabei **nicht** auf den
Compiler verlassen, sondern auf neue Rundlauf-Vitests (`exerciseConfig.test.ts`) – mit der ausdrücklichen
Notiz, dass `Row` weiter untypisiert bleibt und „der Compiler hilft weiter nicht; die Absicherung hängt an
der Vollständigkeit der Fälle" (B-69, Entscheidung E5). Genau diese Lücke ist der Gegenstand von B-74.

## Die echte Lücke

Kleiner als die Idee unterstellte, an einer Stelle. Ein **vollständiger** diskriminierter Union-Typ, der
auch am Aufrufer greift, verlangt die State-Konsolidierung in zwei Dateien (siehe oben) – das ist
Architektur, keine reine Typisierung, und der Nutzen (verhindert eine falsche Zeile *im JSX-Rendern* von
`ConfigEditor`) ist der am wenigsten wertvolle Teil: Ein Feldname-Tippfehler im JSX fällt im Browser sofort
optisch auf (leeres Feld, falscher Wert), lange bevor ihn ein Test bräuchte.

Der **wertvollere** Teil ist innerhalb einer einzelnen Datei erreichbar, **ohne** die Aufrufer anzufassen:
`buildTypeConfig` und `configToEditorState` sind reine Funktionen, die die lose `Row`/`Row[]`-Signatur nach
außen behalten können, aber **innerhalb** jedes `case`-Zweigs die Zeile einmalig auf eine
typtragende Schnittstelle bringen (ein Cast pro Zweig, z. B. `const clozeRows = rows as ClozeRow[]`) – ab
diesem Punkt prüft `tsc` **jeden weiteren Feldzugriff** in diesem Zweig gegen die echte Form. `emptyRow`/
`emptyExtra` können ihre Objekt-Literale ganz ohne Cast direkt gegen die Schnittstelle prüfen (`satisfies
ClozeRow`). Genau das ist der Fehlerfall, den B-69/E5 meinte: ein Feld, das umbenannt oder in seiner Form
geändert wird, während ein `case`-Zweig noch den alten Namen/Typ liest.

## Offene Punkte

1. ~~Volle diskriminierte Union inklusive State-Konsolidierung in den zwei Aufrufern, oder ein
   eingeschränkter Zuschnitt nur innerhalb von `exerciseConfig.tsx`?~~ → **Entscheidung 1.**
2. ~~Bleibt `Row` als öffentlicher, loser Typ für `ConfigEditor` und die Aufrufer bestehen?~~ → **Entscheidung 2.**
3. ~~Eigene Schnittstellen für `Extra` (Vokabel/Liste/Übersetzung/Aufsatz/Hörverstehen/Rechen-Drill) oder
   genügt die grobe `Row`?~~ → **Entscheidung 3.**
4. ~~Prio: bleibt sie bei P3?~~ → **Entscheidung 4.**

## Entscheidungen

Weil der Nutzer diese Sitzung ausdrücklich autorisiert hat, `gegrillt`/`geschätzt` mit bester Empfehlung
selbst zu entscheiden (kein Dialog-Termin), stehen hier vier Entscheidungen mit Begründung und Kosten statt
einer Grill-Runde.

**Entscheidung 1 · Eingeschränkter Zuschnitt: nur die vier Funktionen in `exerciseConfig.tsx`, keine
State-Konsolidierung in den Aufrufern.** *Begründung:* Eine vollständige Union, die auch am Aufrufer
greift, würde `type`/`rows`/`extra` in `VaterExerciseCreate.tsx` und `ExerciseEditModal.tsx` zu einem
einzigen State-Objekt bündeln müssen (siehe „Ist-Stand"); ohne das bleibt jede Union am Aufrufer wirkungslos,
weil `Record<string, any>` strukturell zu jeder Alternative passt. Dieser Umbau wäre eine
Architekturänderung an zwei Formularen (Anlegen **und** Bearbeiten, für alle elf Typen), mit realem
Regressions-Risiko an den 25 E2E aus `uebungstypen.spec.ts` – für einen Nutzen (Tippfehler im JSX), der im
Browser ohnehin sofort auffällt. Der eingeschränkte Zuschnitt trifft stattdessen genau den Fall, den B-69/E5
als Anlass nannte: eine stille Formänderung zwischen den beiden **mirror**-Funktionen. *Kosten:* `Row`
bleibt der öffentliche Typ von `ConfigEditor`/den Aufrufern – ein Feldname-Tippfehler *im Rendern* bleibt
ungeprüft, wie heute. Das ist eine bewusste Lücke, keine übersehene.

**Entscheidung 2 · `Row`/`Row[]` bleiben die öffentlichen Signaturen; typtragende Schnittstellen leben als
private Zwischenform in den `case`-Zweigen.** *Begründung:* `emptyRow`/`emptyExtra`/`buildTypeConfig`/
`configToEditorState` behalten ihre heutige Signatur (`(type, rows: Row[], extra: Row) => …`), damit die
Aufrufer **unverändert** bleiben. Je `case`-Zweig wird die Zeile einmal auf die passende Schnittstelle
gebracht (`emptyRow`/`emptyExtra`: `satisfies ClozeRow` an den Objekt-Literalen, kein Cast nötig;
`buildTypeConfig`/`configToEditorState`: ein Cast am Zweig-Anfang, z. B. `const r0 = rows as ClozeRow[]`) –
ab dort prüft `tsc` jeden Feldzugriff im Zweig. *Kosten:* Der eine Cast pro Zweig in `buildTypeConfig`/
`configToEditorState` (rund 20 Stellen über beide Funktionen) ist ein bewusster, einmaliger
„Vertrauensbeweis" an der Grenze – er kann selbst falsch sein, wenn die Zeile tatsächlich die falsche Form
hat. Das ist dieselbe Grenze, die jede Typisierung an einer `unknown`/API-Antwort hat, hier aber
bewusst benannt statt versteckt.

**Entscheidung 3 · Eigene Schnittstellen für jede Zeilen- und Extra-Form, nicht nur für die fünf von B-69
betroffenen.** *Begründung:* Halbe Typisierung (nur die B-69-Felder) ließe die übrigen sechs Typen weiter
ungeschützt und wäre nach kurzer Zeit nicht mehr nachvollziehbar, warum genau diese fünf eine Schnittstelle
haben und Vokabel/Rechnen/Aufsatz nicht. Vollständig sind das: `VocabularyRow`+`VocabularyExtra`,
`ArithmeticRow`, `ArithmeticDrillExtra` (rowless), `ClozeRow`+`ClozeExtra`, `MatchingRow`+`MatchingExtra`,
`ListRow`+`ListExtra`, `ReadingRow`+`ReadingExtra`/`ListeningRow`+`ListeningExtra` (teilen sich `toQuestion`/
`fromQuestion`), `EssayRow`+`EssayExtra`, `GrammarRow`+`GrammarExtra`, `TranslationRow`+`TranslationExtra`,
`BirkenbihlRow`+`BirkenbihlExtra` – rund 20 kleine Schnittstellen. *Kosten:* mehr Code in einer ohnehin
langen Datei (heute 634 Zeilen); keine neue Datei nötig, die Schnittstellen stehen neben `Row` am Kopf der
Datei.

**Entscheidung 4 · Prio bleibt P3.** *Begründung:* Reines Aufräumen ohne Verhaltensänderung, ohne
laufenden Schaden – der Fehlerfall, den es verhindert (stille Formänderung zwischen den mirror-Funktionen),
ist seit B-69 durch Rundlauf-Vitests bereits abgesichert; B-74 macht die Absicherung **zusätzlich**
mechanisch, ersetzt sie aber nicht. Kein Kind ist heute betroffen. *Kosten:* keine – Bestätigung des
Status quo.

## Akzeptanzkriterien

1. Für jeden der elf Übungstypen existiert eine eigene, benannte Zeilen-Schnittstelle (und, wo Extra-Felder
   echte Werte tragen, eine eigene Extra-Schnittstelle) in `exerciseConfig.tsx`.
2. `emptyRow`/`emptyExtra` prüfen ihre Objekt-Literale je `case` gegen die passende Schnittstelle
   (`satisfies`) – ein fehlendes oder falsch benanntes Feld ist ein Compilerfehler.
3. `buildTypeConfig`/`configToEditorState` casten die Zeile(n) je `case`-Zweig einmalig auf die passende
   Schnittstelle; jeder weitere Feldzugriff in diesem Zweig ist gegen die Schnittstelle geprüft.
4. Die öffentlichen Signaturen von `emptyRow`, `emptyExtra`, `buildTypeConfig`, `configToEditorState`,
   `ConfigEditor`, `RowField`, `RowRepeatedField` sowie deren Aufrufer in `VaterExerciseCreate.tsx` und
   `ExerciseEditModal.tsx` bleiben **unverändert** – kein Zeilendiff außerhalb von `exerciseConfig.tsx`.
5. Kein Verhalten ändert sich: `npm run build` (`tsc -b` + `vite build`) ist grün, die bestehenden Vitest-
   und E2E-Läufe (`exerciseConfig.test.ts`, `uebungstypen.spec.ts`) bleiben unverändert grün.
6. Eine mit Absicht eingebaute Typ-Abweichung (z. B. ein `case`-Zweig liest testweise ein nicht
   existierendes Feld) lässt `tsc --noEmit` **rot** werden – als Beleg, dass die Typisierung tatsächlich
   greift, nicht nur dekorativ dasteht.

## Schätzung

**Größe: M** — `wo: frontend`, `migration: nein`, `vertragsbruch: nein`.

Beide Flags nachgesehen: Der Angriffsplan fasst keine `.cs`-Datei, keine Route und kein DTO an – die
Server-Config-Form ändert sich durch diese Story nicht, nur die interne Typisierung des Editors. Kein
Schema, keine Migration, kein `Pugling.Client`.

**M** gegen die Anker: größer als `childId` aus dem Test-Pfad ziehen (B-01, S) – rund 20 neue
Schnittstellen plus ~20 Cast-Stellen über zwei Funktionen, verteilt auf elf Übungstypen in **einer** Datei.
Kleiner als eine DB-Umbau-Etappe (L) und kleiner als der volle State-Umbau, den Entscheidung 1 bewusst
ausschließt – ohne den Umbau an den zwei Aufrufer-Dateien bleibt der Schnitt auf eine Datei begrenzt, mit
starkem bestehendem Test-Netz (aus B-69: 73 Vitest, 25 E2E), das jede Verhaltensabweichung auffinge. Am
ehesten vergleichbar mit B-69 selbst (M), nur ohne neue Komponente und ohne UI-Änderung – dafür über alle
elf statt fünf Typen.

### Angriffsplan

„Backend zuerst" entfällt – es gibt keine Backend-Arbeit. Reihenfolge, damit ein Fehlgriff früh auffällt:

1. **Schnittstellen anlegen** (Kopf von `exerciseConfig.tsx`, neben `Row`): je Übungstyp eine
   Zeilen-Schnittstelle, wo nötig eine Extra-Schnittstelle (siehe Entscheidung 3) – rund 20 kleine
   `interface`-Blöcke, jeder ein Spiegel des heutigen `emptyRow`/`emptyExtra`-Literals für diesen Typ.
2. **`emptyRow`/`emptyExtra` mit `satisfies` absichern** ([:80-95](../../frontend/src/vater/exerciseConfig.tsx),
   [:98-111](../../frontend/src/vater/exerciseConfig.tsx)): jedes Objekt-Literal bekommt sein
   `satisfies <Typ>Row`/`<Typ>Extra`.
3. **`buildTypeConfig`** ([:123-209](../../frontend/src/vater/exerciseConfig.tsx)): je `case`-Zweig einen
   Cast auf die Zeilen-Schnittstelle, danach Feldzugriffe unverändert lassen – ein Tippfehler oder eine
   falsche Form wird jetzt rot.
4. **`configToEditorState`** ([:227-301](../../frontend/src/vater/exerciseConfig.tsx)): dieselbe Behandlung
   für die Rückrichtung; `toQuestion`/`fromQuestion` ([:212-218](../../frontend/src/vater/exerciseConfig.tsx))
   bekommen die `ReadingRow`/`ListeningRow`-Form.
5. **Gegenprobe, dass die Typisierung greift**: testweise ein falsches Feld in einen `case`-Zweig
   einbauen, `tsc --noEmit` läuft rot, dann zurücknehmen (Beleg für Akzeptanzkriterium 6, nicht Teil des
   Commits).
6. `npm run build`, bestehendes `npm test` (insbesondere `exerciseConfig.test.ts`), `npm run test:e2e`
   (`uebungstypen.spec.ts`), Agent `frontend-reviewer`.

### Risiken

- **Ein Cast kann selbst falsch sein.** Der einmalige Cast je `case`-Zweig in `buildTypeConfig`/
  `configToEditorState` ist eine Behauptung an der Grenze zwischen „lose Zeile" und „typisierte Form" –
  er verhindert *keinen* Fehler, der schon beim Cast selbst passiert (z. B. ein Feld, das in Wahrheit nie
  gesetzt wurde). Das ist dieselbe Grenze wie bei jeder Typisierung gegen `unknown`; hier bewusst benannt.
- **`ConfigEditor` und die Aufrufer bleiben ungeschützt** (Entscheidung 1/2) – ein Tippfehler im JSX-Rendern
  fällt weiterhin nur im Browser auf, nicht im Compiler. Kein neues Risiko, aber eine bewusst offene Lücke,
  die eine künftige Story (State-Konsolidierung in `VaterExerciseCreate.tsx`/`ExerciseEditModal.tsx`)
  schließen müsste, sollte sie sich lohnen.
- **Verhaltensgleichheit ist die ganze Abnahme** (`art: Aufräumen`) – jede der elf `case`-Zweige wird
  angefasst; das bestehende Testnetz aus B-69 (73 Vitest, 25 E2E) ist der eigentliche Schutz, nicht die
  neue Typisierung selbst.
- **Was nicht droht:** kein Schema, keine Route, kein DTO – `git diff --name-only -- '*.cs'` bleibt leer.

### Testweg

| Was | Wo |
| --- | --- |
| Typsicherheit (der eigentliche Zweck dieser Story) | `tsc --noEmit` (bzw. `npm run build`, das `tsc -b` einschließt) |
| Verhaltensgleichheit, Rundlauf je Typ | bestehend: `frontend/src/vater/exerciseConfig.test.ts` |
| Verhaltensgleichheit, Editor-Komponenten | bestehend: `RepeatedTextFields.test.tsx` u. a. |
| Durchstich Anlegen/Bearbeiten für alle elf Typen | bestehend: `frontend/e2e/uebungstypen.spec.ts` |
| Abnahme | `npm run build`, `npm test`, `npm run test:e2e`, Agent `frontend-reviewer` |

**Kein `/smoke-test` und kein Backend-Test**: keine Route, kein DTO und kein Serververhalten ändert sich –
ein HTTP-Durchgang bewiese hier nichts, was `tsc` und die bestehenden Frontend-Tests nicht schon zeigen.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-69, Entscheidung 5.
- **2026-08-03** — ausformuliert am Code (autonom getroffen, Nutzerauftrag 2026-08-04). Kernbefund: Eine
  vollständige diskriminierte Union, die auch an den Aufrufern greift, verlangt eine State-Konsolidierung
  in `VaterExerciseCreate.tsx` und `ExerciseEditModal.tsx` (heute drei getrennte States für Typ/Zeilen/
  Extra) – ohne die bleibt jede Union wirkungslos, weil `Record<string, any>` strukturell zu jeder
  Alternative passt. B-24 (2026-08-03) hat bestätigt, dass kein `case`-Zweig die rohe Zeile in die
  Nutzlast spreadet; offen bleibt nicht das, sondern ob ein `case`-Zweig ein Feld falsch oder veraltet
  liest.
- **2026-08-03** — gegrillt (autonom getroffen, Nutzerauftrag 2026-08-04). Vier Entscheidungen, keine
  zurückgestellt: eingeschränkter Zuschnitt auf die vier reinen Funktionen in `exerciseConfig.tsx`, keine
  State-Konsolidierung in den Aufrufern (Entscheidung 1); `Row`/`Row[]` bleiben die öffentlichen Signaturen,
  ein Cast bzw. `satisfies` je `case`-Zweig trägt die eigentliche Prüfung (Entscheidung 2); alle elf Typen
  bekommen eigene Schnittstellen, nicht nur die fünf aus B-69 (Entscheidung 3); Prio bleibt P3
  (Entscheidung 4).
- **2026-08-03** — geschätzt (autonom getroffen, Nutzerauftrag 2026-08-04): **M**, `wo: frontend`,
  **migration: nein**, **vertragsbruch: nein** — beides nachgesehen, der Angriffsplan fasst keine
  `.cs`-Datei an und ändert an keiner Server-Config-Form etwas. Sechs Schritte, Testweg ist in erster Linie
  `tsc --noEmit`/`npm run build` (der eigentliche Zweck der Story) plus das bestehende Vitest-/E2E-Netz aus
  B-69 als Beleg für Verhaltensgleichheit; `/smoke-test` entfällt begründet.
- **2026-08-05** — aus der Nachschau zu B-72 nachgetragen: die Birkenbihl-Dekodierung ist inzwischen ein
  **zweites** Feld, dessen Sicherheit nicht am Compiler hängt, sondern an einer Laufzeitprüfung
  (`RowRepeatedPairField` nimmt `pairs: unknown` und verengt mit `Array.isArray(pairs) ? pairs : []`). Das
  ist heute korrekt und stürzt auch bei Altdaten nicht ab — aber es ist ein weiterer Beleg dafür, was
  `Row = Record<string, any>` kostet: jede neue Feldform muss ihre Typprüfung selbst mitbringen.
- **2026-08-06** — gebaut (Nachtlauf 2, Sprint 4): ~20 Zeilen-/Extra-Schnittstellen ergänzt (eine je
  Übungstyp, `VocabularyRow`/`Extra` … `BirkenbihlRow`/`Extra`, plus die geteilte `QuestionRow` für
  Reading/Listening). `emptyRow`/`emptyExtra` prüfen ihre Literale per `satisfies` (bei genuin partiellen
  Extra-Werten wie „List" gegen `Partial<…Extra>`, mit begründetem Kommentar); `buildTypeConfig` castet
  `rows`/`extra` je `case`-Zweig einmal auf die passende Schnittstelle; `configToEditorState`s `.map()`-
  Aufrufer tragen Rückgabetyp-Annotationen bzw. `satisfies`. `toQuestion`/`fromQuestion` nehmen/liefern
  jetzt `QuestionRow` statt `Row`. Die öffentlichen Signaturen (`Row`/`Row[]`, `ConfigEditor`, `RowField`,
  `RowRepeatedField`, beide Aufrufer-Komponenten) bleiben unverändert (Entscheidung 1/2). **Rote Probe:**
  ein Feldname in `Cloze`s `buildTypeConfig`-Zweig testweise auf `vocabKeyTypo` verfälscht →
  `tsc -b` sofort rot (`TS2551: Property 'vocabKeyTypo' does not exist on type 'ClozeRow'. Did you mean
  'vocabKey'?`), zurückgenommen (`git diff` danach ohne Rest). `npm run build` clean, `npm test -- --run`
  → **153/153 grün** (unverändert). `frontend-reviewer` lief gegen den Diff.
