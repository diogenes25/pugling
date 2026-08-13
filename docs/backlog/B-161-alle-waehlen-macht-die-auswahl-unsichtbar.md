---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/lehrplan, rolle/supervisor]
aliases: [Alle wählen wählt Unsichtbares, Auswahl überlebt den Filterwechsel]
status: abgenommen
nachgeschaut: 2026-08-13
prio: P1
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau im Nachtlauf 2026-08-12 (zu B-18)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-18]
---

# B-161 · „Alle wählen" wählt bis zu 400 Übungen, die der Vater nie sieht und nicht abwählen kann

[B-18](B-18-auto-lehrplan-generator.md) hat den Assistenten um drei Filter und einen „Alle wählen"-Knopf
erweitert, der bis zu **500** Treffer wählt. Gerendert wird aber nur die geladene erste Seite (≤100). Bis zu
400 gewählte Übungen sind damit unsichtbar — und weil die Auswahl **jeden** Filterwechsel überlebt, kann der
Plan Positionen enthalten, die der gezeigte Filter ausschließt.

## User Story

Als *Vater* möchte ich, dass die Zahl „gewählt" und die Liste vor mir dasselbe meinen, damit ich nicht einen
Lehrplan mit Übungen anlege, die ich nie gesehen und nicht gewollt habe.

## Ist-Stand am Code

Zwei zusammenhängende Hälften, beide in `frontend/src/vater/VaterWizard.tsx`:

**1. Gewählt, aber nicht erreichbar.**

- `selectAll` (`:203-213`) fragt bei mehr Treffern als geladen einmal mit `take: 500` nach und schreibt
  **alle** zurückgegebenen Ids in `selected` (`:213`).
- Gerendert werden ausschließlich `filteredExercises` = `exercises.data?.items` (`:154`, `:440`) — die
  geladene erste Seite.
- `toggle` (`:171-173`) existiert nur an einer gerenderten Zeile, und es gibt **keinen** „Auswahl
  leeren"-Knopf (nachgezählt: `setSelected` steht nur an `:172`, `:204`, `:213`, `:217`).
- **Vor diesem Diff war `selected` immer eine Teilmenge des Gerenderten** — jede gewählte Id war
  erreichbar. Genau diese Invariante bricht der Diff.

**2. Die Auswahl überlebt jeden Filterwechsel.**

- Kein `setSelected([])` bei einer Filteränderung — die vier Fundstellen oben sind alles.
- Folge, am Bildschirm gleichzeitig sichtbar: `:399` zeigt „({selected.length} gewählt)", `:424` zeigt
  „{filteredExercises.length} passende Übungen". Nach „500 wählen → Typ-Filter enger stellen" steht dort
  „5 passende Übungen" neben „(500 gewählt)".
- `canAdvance` (`:231`) prüft nur `selected.length === 0`, lässt also weiter; `finish` (`:267`) schickt
  `exerciseIds: selected`, und `wizardFinish.ts:101-113` legt **500 Positionen** an.
- `:457` sagt dazu „gilt für alle {selected.length} Positionen" — die Zahl ist korrekt, die Auswahl dahinter
  nicht das, was der Vater vor sich hatte.

## Die echte Lücke

`selected` trägt zwei Bedeutungen in einem Zustand: „aus der aktuellen Trefferliste gewählt" und „irgendwann
früher gewählt". Das ist wörtlich die Fehlerfamilie, die
[nachtlauf.md](../nachtlauf.md) für dieses Repo gemessen hat — *eine Bedingung, die zwei Situationen
zusammenzieht*, wie `Testable` als Typ- statt Tages-Aussage (B-114) oder „leer" für „nichts gekauft" *und*
„Laden gescheitert" (B-111).

**Alt oder neu?** Die Wurzel ist älter: `contentSearch` konnte die Auswahl schon überleben. **Neu und in
B-18s Diff** ist (a) dass `selected` Ids enthalten kann, die nie gerendert wurden — der Rücknahmeweg fehlt
also ganz —, und (b) die Anhebung von 100 auf 500 plus drei neue Filter, die zum Nachschärfen einladen.
Damit wurde aus einem Randfall der Regelfall. Die Abnahme war dem Fund sehr nah: der Reviewer bemerkte, dass
`selected` jetzt über die Seite hinausreicht, reparierte aber nur das *Metadaten*-Nachschlagen (`gesehen`)
und fragte nicht, ob der Vater die Auswahl noch sehen und zurücknehmen kann.

## Offene Punkte

1. ~~Auswahl bei Filterwechsel leeren oder behalten?~~ → Entscheidung 1.
2. ~~Wie wird eine Auswahl jenseits der Seite bedienbar?~~ → Entscheidung 2 und 3.
3. ~~Ist P1 richtig?~~ → Entscheidung 5.
4. ~~Komponententest oder E2E?~~ → Entscheidung 4.

## Entscheidungen

1. **Die Auswahl wird geleert, sobald sich ein Suchkriterium ändert** — und der Vater erfährt es, wenn
   dabei wirklich etwas verloren ging. Begründung: `exercises` hängt schon an genau diesen sieben Eingaben
   (`VaterWizard.tsx:127`); `selected` an keiner. Eine Auswahl über eine geänderte Trefferliste hinweg wäre
   nur zu verantworten, wenn man sie **sehen** könnte, und das ist der Fall, den es nicht gibt. Stumm zu
   leeren wäre die B-116-Falle (eine Änderung ohne Rückmeldung liest sich als Fehler), darum ein kurzer
   Hinweis — aber **nur**, wenn vorher etwas gewählt war. **Kosten:** Wer den Filter nur verfeinern wollte,
   verliert seine Auswahl und muss neu wählen. Das ist der billigere Fehler gegenüber einem Plan mit
   ungesehenen Pflichten.
2. **Ein „Auswahl leeren"-Knopf neben der Zahl.** Begründung: Er löst den Sackgassen-Fall vollständig und
   in drei Zeilen — nach „Alle wählen" ist er der **einzige** Weg zurück, weil `toggle` nur an gerenderten
   Zeilen existiert. **Kosten:** ein Bedienelement mehr in einer schon dichten Kopfzeile.
3. **„Alle wählen" behält seine 500** — die Obergrenze wird *nicht* auf die geladene Seite zurückgedreht.
   Begründung: Genau das war der Wert von [B-18](B-18-auto-lehrplan-generator.md) („statt 100 nun bis 500");
   sie zurückzunehmen würde einen abgenommenen Nutzen wieder abbauen, statt einen Defekt zu beheben — und
   das ist eine Produktentscheidung, die diese Story nicht trifft. Stattdessen sagt die Zahl künftig die
   Wahrheit: „N gewählt, davon M unten nicht sichtbar". **Kosten:** Die unsichtbaren M bleiben einzeln
   unerreichbar; abwählen geht nur ganz (Entscheidung 2). Ein echtes Paging über die Auswahl wäre eine
   eigene Story und steht hier ausdrücklich nicht drin.
4. **Komponententest an einem exportierten Baustein, keine E2E-Spec.** Begründung: Der Defekt ist ein
   **Zustandsübergang** („wählen → Filter ändern"), nicht ein Weg durch die App; `frontend/CLAUDE.md` weist
   genau das dem Komponententest zu. `VaterWizard` selbst hängt an vier Ladevorgängen und ist als Ganzes
   nicht ohne Netz zu rendern — die Ableitung „welche Auswahl gilt noch, und wie viele davon sind sichtbar"
   wandert darum als **reine Funktion** in `wizardSearch.ts` (dort liegen schon `wizardSearchParams` und die
   Nachbarn `wizardFinish`/`seriesDerivation` als reine Logik). **Kosten:** Der Test prüft dann die
   Ableitung, nicht die Bindung im Bildschirm — das ist der schwächere Beleg, und er wird durch den
   Rollengang ergänzt, nicht ersetzt. Ausdrücklich benannt statt verschwiegen.
5. **P1 bleibt.** Begründung: Es entstehen Positionen mit Pflichtziel, die niemand gewählt hat, und eine
   gerissene Pflicht zieht dem Kind **Münzen** ab (`PenaltyCoins`) — derselbe Grund, aus dem
   [B-114](B-114-showboth-position-unspielbar.md) P1 war: der Fehler wirkt nicht auf den Bedienenden,
   sondern auf das Kind. **Kosten:** Die Story drängt sich vor die drei P2 des Katalog-Fadens.

## Akzeptanzkriterien

1. Ändert sich ein Suchkriterium (Fach, Klassenstufe, Schulart, Inhaltssuche, Art, Typ, Quelle), ist
   `selected` danach leer — es kann also keine Übung überleben, die der neue Filter ausschließt.
2. War vor dem Leeren etwas gewählt, sagt ein Hinweis, dass die Auswahl zurückgesetzt wurde. War nichts
   gewählt, erscheint kein Hinweis.
3. Ein „Auswahl leeren"-Knopf steht neben der Zahl und setzt `selected` zurück. Er bleibt **montiert** und
   wird bei leerer Auswahl nur gesperrt — nachgezogen aus dem Reviewer-Fund: hängt man ihn aus, verschwindet
   er nach dem Klick unter dem Finger und der Fokus fällt auf `<body>`.
4. Steht mehr in `selected` als die Liste zeigt, nennt die Kopfzeile beides: die Gesamtzahl **und** wie
   viele davon unten nicht sichtbar sind.
5. Der Plan enthält am Ende genau die Ids aus `selected` — unverändert; die Story ändert **nicht**, was
   „Alle wählen" wählen darf (Entscheidung 3).
6. Eine reine Funktion trägt die Ableitung aus AK 1 und 4 und ist getestet; die rote Probe belegt, dass der
   Test den heutigen Stand fängt.
7. `npm test` und `npm run build` bleiben grün; keine Server-Datei wird angefasst.

## Schätzung

**Größe: S** — ein Bildschirm, ein Effekt, ein Knopf, eine Zahl und eine reine Funktion mit Test. Näher an
[B-116](B-116-blaettern-ohne-rueckmeldung.md) (dieselbe Klasse: ein Zustand, der zwei Dinge bedeutete, `S`)
als an B-113; kein neues Feld, kein neuer Aufruf, kein Server-Anteil.

- **`migration: nein`** — keine Schemaänderung, die Story fasst nur `frontend/src/vater/` an.
- **`vertragsbruch: nein`** — kein DTO, kein Endpunkt, keine Client-Methode berührt.

**Risiken:**

- **Der Effekt darf nicht beim ersten Rendern feuern und keinen Hinweis erzeugen, wenn nichts gewählt war.**
  Sonst begrüßt der Assistent den Vater mit „Auswahl zurückgesetzt", bevor er etwas getan hat. Die Bedingung
  ist darum „vorher nicht leer", nicht „Kriterien haben sich geändert".
- **Der Vergleich der Kriterien braucht einen stabilen Schlüssel.** `categoryId` ist `number | ""`,
  `typeKey`/`sourceSearch` sind Strings — ein Objektvergleich per Referenz würde bei jedem Rendern feuern.
  Ein serialisierter Schlüssel (dieselben sieben Werte, die schon in der `useAsync`-Abhängigkeitsliste
  stehen) ist der billigste stabile Vergleich.
- **`merken`/`gesehen` nicht mit leeren.** Der Metadaten-Zwischenspeicher aus B-18 (`:186-190`) darf beim
  Leeren der Auswahl **nicht** mitgelöscht werden: er trägt Titel und Typ auch für Ids, die gerade nicht
  gerendert sind, und `wizardFinish` braucht ihn. Beim Bauen gegenprüfen.

**Angriffsplan** (kein Backend-Anteil):

1. `wizardSearch.ts`: eine reine Funktion `auswahlNachFilterwechsel(vorher, kriterienSchlüsselAlt,
   kriterienSchlüsselNeu)` bzw. — einfacher und besser prüfbar — `kriterienSchluessel({...})`, die die sieben
   Werte zu einem Vergleichsstring macht, plus `unsichtbareAuswahl(selected, geladeneIds)` für AK 4.
2. `VaterWizard.tsx`: ein `useEffect` auf den Kriterien-Schlüssel, der `selected` leert und **nur bei
   vorher nicht-leerer Auswahl** eine Meldung setzt.
3. Kopfzeile (`:399`): Zahl um „davon M unten nicht sichtbar" ergänzen (nur wenn M > 0) und den
   „Auswahl leeren"-Knopf daneben (nur wenn Auswahl nicht leer), mit `aria-label`.
4. `wizardSearch.test.ts` (existiert) um die Fälle für beide Funktionen erweitern; rote Probe.
5. `npm test`, `npm run build`, `frontend-reviewer` über den Diff.

**Testweg**: `frontend/src/vater/wizardSearch.test.ts` (bestehende Datei) für die reinen Funktionen aus
AK 1/4/6; `npm run build` als Typecheck für AK 7. **Rollengang**: live im Browser auf `/vater/plaene/neu` —
alle wählen, Filter verengen, Zahl und Hinweis ansehen, „Auswahl leeren" drücken. Keine E2E-Spec
(Entscheidung 4, mit Kosten benannt).

## Verlauf

- **2026-08-12** — angelegt aus der **Nachschau** des Nachtlaufs (Retrospektive Sprint A), zur am
  2026-08-11 abgenommenen [B-18](B-18-auto-lehrplan-generator.md). `entgangen_bei: [B-18]` — die Wurzel ist
  älter, aber erreichbar und zum Regelfall gemacht hat sie **dieser** Diff (100 → 500, drei neue Filter,
  Auswahl jenseits des Gerenderten). Selbst am Code nachgeprüft: alle vier `setSelected`-Fundstellen
  einzeln, `filteredExercises` als einzige Renderquelle, und die beiden Zahlen an `:399`/`:424`.
- **2026-08-12** — gegrillt (autonom, `art: Defekt`, Freigabe 1 des Nachtlaufs, Sprint B): fünf
  Entscheidungen mit Begründung und Kosten. Die wichtigste ist eine **Nicht**-Änderung: „Alle wählen" behält
  seine 500 (Entscheidung 3). Die Grenze auf die geladene Seite zurückzudrehen wäre die einfachere Reparatur
  gewesen, hätte aber den abgenommenen Nutzen von B-18 abgebaut — und das ist eine Produktentscheidung, die
  ein autonomer Lauf nicht trifft. Stattdessen sagt die Zahl die Wahrheit.
- **2026-08-12** — geschätzt (Nachtlauf, Sprint B): `S`, `wo: frontend`, `migration: nein`,
  `vertragsbruch: nein`. Drei Risiken benannt, darunter eines, das beim Bauen leicht kaputtgeht: der
  Metadaten-Zwischenspeicher `gesehen` aus B-18 darf beim Leeren der Auswahl **nicht** mitgelöscht werden.
- **2026-08-12** — gebaut (Nachtlauf, Sprint B). Drei reine Funktionen in `wizardSearch.ts`
  (`wizardFilterKey`, `unsichtbareAuswahl`, `auswahlNachFilterwechsel`), ein Effekt auf den
  Kriterien-Schlüssel, ein „Auswahl leeren"-Knopf, und die Zahl nennt die Unsichtbaren mit.
  **Rote Probe:** mit `auswahlNachFilterwechsel` auf „immer `null`" — dem Altverhalten — fallen **3 von 18**
  Fällen, genau die drei, die die Regel beschreiben.
- **2026-08-12** — **Rollengang im echten Browser** (Freigabe 6): Server nach der letzten Änderung
  gestartet, Wegwerf-DB, Assistent bis Schritt 3 gefahren. Sechs Übungen gewählt → „(6 gewählt)" plus
  erschienener „Auswahl leeren"-Knopf; Typ-Filter auf „Lückentext" → **„(0 gewählt)"** und
  **„Auswahl zurückgesetzt (6 Übungen), weil sich die Suche geändert hat."** über zwei ungehakten Treffern.
  Vor dem Fix hätte dort „(6 gewählt)" neben „2 passende Übungen" gestanden. „Auswahl leeren" geprüft: setzt
  zurück, Knopf wird danach gesperrt.
  **Ehrliche Grenze:** AK 4 (die „davon M nicht sichtbar"-Zahl) ist im Browser **nicht** gesehen — der Seed
  hat sechs Übungen im Fach, nicht die >100, die den Fall auslösen. Die Arithmetik deckt der Unit-Test
  (500 gewählt / 100 geladen → 400), das Rendern ist eine einzeilige Bedingung. Benannt statt verschwiegen.
- **2026-08-12** — `geschaetzt → abgenommen`. **`frontend-reviewer`: ein abnahmerelevanter Fund, sofort
  behoben** (Freigabe 3), und er ist der wertvollste Beitrag dieses Sprints: **`selectAll` hatte kein
  Generationen-Gate.** Der `take:500`-Nachschlag ist der einzige Ladeweg neben `useAsync` (das sein eigenes
  `cancelled`-Flag hat), und die Filterfelder sind währenddessen nicht gesperrt. Wer während des Ladens den
  Filter verengt, bekam die Ids der **verworfenen** Suche zurückgeschrieben — der Effekt hatte korrekt
  geleert, danach stand die Auswahl wieder da, und nichts leerte sie je wieder. AK 1 galt damit nur für den
  synchronen Pfad; derselbe Weg machte „Auswahl leeren" während des Ladens stumm rückgängig. Behoben mit dem
  Schlüssel, der ohnehin dasteht (`geltenderFilterKey`, umbenannt, weil die Ref nach dem Effekt immer den
  *aktuellen* Schlüssel trägt).
  **Vier weitere Funde mitgenommen, alle nicht blockierend:** die Hinweis-Region war für Screenreader stumm
  und optisch fast unsichtbar (`.banner` allein trägt keinen Hintergrund) — jetzt dauerhaft montierte
  `role="status"`-Region nach dem Vorbild von `StatusBanner`/`DerivedHint`, plus eine neue
  `.banner.info`-Variante, weil hier nichts gelang und nichts fehlschlug; „Auswahl leeren" verschwand nach
  dem Klick unter dem Finger und nahm den Fokus mit — bleibt jetzt montiert und wird nur gesperrt; die
  E2E-Zusicherung `/\(\d+ gewählt\)/` wäre **genau in dem Fall gefallen, für den ihr Kommentar geschrieben
  wurde** (sobald der Katalog über eine Seite wächst, heißt die Überschrift „…, davon N unten nicht
  sichtbar") — Klammer entfernt; und „1 Übungen" war ungebeugt, während das Haus an vergleichbarer Stelle
  beugt. Dazu ein Testfall, der **nie** rot werden konnte, ehrlich als Absichtserklärung beschriftet statt
  als Beleg gezählt — dieselbe Klasse wie der B-154-Fund.
  **Belege:** `npx vitest run` **269/269 grün** (35 Dateien), `npm run build` grün,
  `npx playwright test assistent.spec.ts` **1 passed** (27,3 s, frische Temp-DB) — sie fährt den
  `selectAll`-Pfad und damit den Ort des Korrektheitsfunds. Backend unberührt.
  **Ein Fund außerhalb des Diffs** wanderte nach [B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md):
  während eine neue Suche lädt, nennt die Trefferzahl weiter die alte — dieselbe Wurzel, aber vorbestehend.
- 2026-08-13 · **Nachschau: zwei Funde.** Der schwere: im **Ladefenster** nach einem Filterwechsel stehen die alten Zeilen noch und sind anklickbar — das Kästchen trägt kein `disabled`, während „Alle wählen" daneben eines hat —, und die so entstandene Auswahl wird nie wieder geleert; damit ist der P1-Schaden dieser Story über eine zweite Tür erreichbar → [B-169](B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md). Der zweite: die in diesem Diff **umgeschriebene** E2E-Zusicherung trifft „(0 gewählt)" und wartet damit auf nichts → [B-171](B-171-zwei-zusicherungen-pruefen-den-ausgangszustand.md). Sauber befunden: der Filter-Schlüssel deckt die Abfrage vollständig ab (sieben Werte, deckungsgleich mit der Abhängigkeitsliste), zwei überlappende `selectAll`-Läufe sind unmöglich, und das Generationen-Gate deckt den Wechsel mitten im Nachladen in beiden Zweigen.
