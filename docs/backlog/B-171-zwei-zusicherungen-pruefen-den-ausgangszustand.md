---
tags: [typ/story, status/abgenommen, bereich/tests, bereich/frontend, bereich/backend]
aliases: [0 gewaehlt trifft die Regex, AK-8 ohne Vorbedingung, Zusicherung kann nicht fallen]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: beides
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-08-13 zu B-161 und B-157
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-161, B-157]
nachgeschaut: 2026-08-14
---

# B-171 · Zwei Zusicherungen prüfen den Ausgangszustand mit — und können darum nicht fallen

Die Regel steht seit dem 2026-08-12 in der Root-`CLAUDE.md`, mit vier gemessenen Fällen. Die Nachschau vom
2026-08-13 hat zwei weitere gefunden, beide in Arbeit derselben Woche. Sie stehen zusammen in einer Story,
weil sie **eine** Handlung sind: je eine Zeile, die die Vorbedingung belegt.

## Fall 1 — die E2E-Zusicherung im Assistenten trifft die Null

`frontend/e2e/assistent.spec.ts:61`

```ts
await expect(page.getByRole("heading", { name: /\(\d+ gewählt/ })).toBeVisible();
```

Die Überschrift heißt ab dem **ersten** Render von Schritt 3 „Übungen wählen (0 gewählt)" — die Zahl steht
unbedingt da (`VaterWizard.tsx:464-466`). `\d+` trifft die `0`. Die Zusicherung ist im Ausgangszustand schon
wahr, kehrt sofort zurück und **wartet auf nichts**.

Ihr eigener Kommentar drei Zeilen darüber nennt genau das Warten als ihren Zweck: *„Auf die Zahl in der
Überschrift warten, statt sofort weiterzuklicken: Sonst träfe ‚Weiter' ein noch leeres `selected`."*

**Der Fall, für den sie geschrieben wurde:** Sobald „Alle wählen" den asynchronen `take:500`-Weg nimmt
(`total > geladene Seite`), kehrt der Klick zurück, bevor `setSelected` lief. „Weiter" ist während
`selectAllBusy` nicht gesperrt (`:620`), `canAdvance` sieht `length === 0` und setzt „Bitte mindestens eine
Übung wählen." — die Spec fällt eine Zeile später an `/Feinschliff/`, also **ohne Hinweis auf die Ursache**.
Heute grün nur deshalb, weil der Seed-Filter (`environment`) in den synchronen Zweig fällt.

Besonders belegkräftig: **diese Zeile wurde in B-161s Diff bewusst umgeschrieben** und mit einem Kommentar
versehen, der ihren Versagensfall analysiert (die schließende Klammer wurde entfernt). Der
Ausgangszustands-Treffer blieb dabei unbemerkt.

*Belegt auch außerhalb des Codes:* Der Rollengang zu B-163 am 2026-08-13 hat die Seite ausgelesen, und dort
stand wörtlich `Übungen wählen (0 gewählt)`.

**Fix:** `/\([1-9]\d* gewählt/` — dann wartet sie auf eine nicht-leere Auswahl, wie ihr Kommentar behauptet.

## Fall 2 — der AK-8-Test von B-157 belegt die Zuordnung nicht, die er verlieren sieht

`backend/Pugling.Api.Tests/FachEigentumTests.cs:261-288`
(`Eine_Benutzte_Art_Zu_Loeschen_Nimmt_Der_Uebung_Nur_Die_Zuordnung`) legt die Übung **mit** `categoryId` an,
löscht die Art und behauptet danach `categoryId == null` (`:287`) — **ohne vorher zu prüfen, dass die
Zuordnung überhaupt bestand.** Die Zusicherung ist damit zur Hälfte eine Aussage über den Ausgangszustand.

Würde der Anlegepfad `categoryId` künftig annehmen und still verwerfen, bliebe der Test grün: Das Feld
existiert im DTO (`Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:15`), `unknown_field` schlägt also
nicht zu, und gesetzt wird es an genau einer Stelle (`ExerciseControllerBase.cs:278`). AK 8 („exercises only
lose their assignment") wäre dann wieder nur ein Versprechen im Vertragsdokument — genau der Grund, aus dem
der Reviewer den Fall verlangt hat.

**Fix:** eine Zeile vor dem `DELETE`:

```csharp
var vorher = await adult.GetFromJsonAsync<JsonElement>($"{basePath}/{exerciseId}");
Assert.Equal(categoryId, vorher.GetProperty("categoryId").GetInt32());
```

**Rote Probe dafür:** `CategoryId = body.CategoryId` in `ExerciseControllerBase.cs:278` entfernen. Heute
bleibt der Fall grün; mit der Zeile fällt er.

## Offene Punkte

Beim Aufnehmen sahen beide Fälle wie zwei determinierte Zeilen aus. Beim Grillen kam **eine** echte Frage
heraus — und sie ist die interessante an dieser Story.

1. ~~Reicht die engere Regex, oder braucht der Fall, für den die Zusicherung geschrieben wurde, eigene
   Deckung?~~ → Entscheidungen 1 und 2.
2. ~~Wie belegt Fall 1 seine rote Probe, wenn der asynchrone Pfad im Seed nicht auftritt?~~ →
   Entscheidung 3.

## Entscheidungen

1. **Die Regex wird eng gezogen (`[1-9]\d*`), und das ist der ganze Fix für Fall 1.** Begründung: Die
   Zusicherung hat einen klar benannten Zweck — *warten*, bis `selected` nicht mehr leer ist —, und den
   erfüllt sie mit der engeren Regex. *Kosten:* Sie prüft weiterhin **nicht**, dass der asynchrone
   `take:500`-Pfad korrekt zurückschreibt; sie wartet nur richtig. Das ist genau so viel, wie ihr Kommentar
   behauptet, und keine Zeile mehr.
2. **Der asynchrone Pfad bekommt hier *keine* eigene Deckung — zurückgestellt.** Begründung: Ihn zu erzwingen
   heißt, den Seed-Katalog über eine Seite (100 Einträge) hinaus zu treiben oder die Antwort zu fälschen. Das
   erste macht jeden anderen Test langsamer, das zweite ist konventionswidrig. Und der Pfad selbst ist von
   B-161 über `wizardSearch.test.ts` an seiner *Logik* abgedeckt — was fehlt, ist nur die Naht im Browser.
   *Kosten:* Bleibt eine echte Lücke, und sie gehört benannt statt zugedeckt: ein Rückschreibfehler im
   `take:500`-Zweig fällt heute in keinem Test auf. Wenn das je zählt, ist es eine eigene Story mit einem
   Katalog-Fixture, nicht ein Nebensatz hier.
3. **Fall 1 belegt seine rote Probe durch Weglassen der Auswahl, nicht durch Erzwingen des Nachladens.**
   Begründung: Die Behauptung lautet „die Zusicherung hängt gar nicht an der Auswahl". Der direkte Beleg ist
   also, den `Alle wählen`-Klick zu entfernen: die **alte** Regex bleibt grün (das ist der Defekt), die
   **neue** fällt. Das ist eine schärfere Probe als der schwer herstellbare Echtfall — sie trifft die
   Eigenschaft, um die es geht. *Kosten:* Die Probe belegt die Zusicherung, nicht das Produkt; das ist bei
   einer Story über Zusicherungen aber genau richtig.

## Akzeptanzkriterien

1. `assistent.spec.ts` wartet auf eine **nicht-leere** Auswahl; ohne den `Alle wählen`-Klick fällt der Fall.
2. Der AK-8-Test von B-157 belegt die Zuordnung **vor** dem Löschen und fällt, wenn der Anlegepfad
   `categoryId` verwirft.
3. Beide roten Proben stehen mit **Zahl** (erwartet/gemessen) im `## Verlauf`.
4. Die zurückgestellte Lücke aus Entscheidung 2 steht als Satz in der Story, nicht als stille Auslassung.

## Schätzung

**Größe: XS** — zwei Zeilen Produktivcode-frei: eine Regex, eine Zusicherung. Der Aufwand liegt vollständig in
den zwei roten Proben, und für die zweite muss der Produktionscode kurzzeitig gebrochen werden.

- **`wo: beides`** — eine E2E im Frontend, ein Integrationstest im Backend. Kein Produktivcode auf beiden
  Seiten.
- **`migration: nein`**, **`vertragsbruch: nein`** — es wird nichts als Test geändert.

**Risiken:**

1. Die zweite rote Probe **bricht kurzzeitig den Anlegepfad** (`CategoryId = body.CategoryId` entfernen).
   Wird sie nicht zurückgenommen, ist eine echte Zuweisung kaputt. Darum: Probe, Zahl notieren, sofort
   zurücknehmen, voller Lauf **danach**.
2. Der AK-8-Test liest `categoryId` als `GetInt32()`. Ist das Feld im Ausgangszustand `null`, wirft der
   Zugriff. **Präzisiert nach dem Review:** Der Fall wird dadurch trotzdem rot — die `ValueKind`-Prüfung
   kauft also nicht das Fallen, sondern die *Meldung* (`Expected: Number / Actual: Null` statt einer
   Ausnahme mitten im Testkörper). Sie bleibt richtig, nur aus dem kleineren Grund.

**Angriffsplan:**

1. Backend zuerst (API-First gilt auch, wenn nur Tests wandern): die Vorbedingung in `FachEigentumTests`,
   rote Probe mit Zahl, Probe zurücknehmen.
2. Dann die Regex in `assistent.spec.ts`, rote Probe durch Weglassen des Klicks, mit Zahl.
3. Voller Lauf beider Suiten.

**Testweg**: `backend/Pugling.Api.Tests/FachEigentumTests.cs`
(`Eine_Benutzte_Art_Zu_Loeschen_Nimmt_Der_Uebung_Nur_Die_Zuordnung`) und `frontend/e2e/assistent.spec.ts`.
Keine neuen Dateien — beide Fälle existieren und werden tragend gemacht.

## Was das über die Regel sagt

Die Regel in `CLAUDE.md` sagt: „je Fall benennen, *welche* Änderung ihn rot macht". Beide Fälle hier zeigen
die **tückischste** Form — nicht eine leere Zusicherung, sondern eine, die *fast* das Richtige prüft:
Fall 1 wartet auf ein Muster, das eine Ziffer zu viel zulässt; Fall 2 prüft das Ende einer Kette, ohne ihren
Anfang. Beide sind beim Lesen unauffällig, weil der Name und der Kommentar das Richtige behaupten. Das ist
die Beobachtung, die diese Story dem Erfahrungsschatz hinzufügt — sie gehört nach dem Bauen in die
Begründung der Regel, nicht als neue Regel daneben.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-161 und B-157. Beide Fälle von mir gegengeprüft: die
  Überschrift trägt die Null unbedingt (zusätzlich belegt durch den ausgelesenen Bildschirm im
  B-163-Rollengang), und der AK-8-Test hat vor dem `DELETE` keine Zusicherung.
- 2026-08-14 · `ausformuliert → gegrillt`, autonom (`art: Defekt`, Freigabe 1 des Nachtlaufs). Drei
  Entscheidungen. Beim Grillen kam **eine echte Frage** heraus, die beim Aufnehmen nicht sichtbar war: Die
  engere Regex behebt das *Warten*, deckt aber den asynchronen `take:500`-Pfad weiterhin nicht ab. Das ist
  ausdrücklich zurückgestellt (Entscheidung 2) statt zugedeckt — ihn zu erzwingen heißt, den Seed-Katalog
  über 100 Einträge zu treiben oder die Antwort zu fälschen, und beides kostet mehr als es hier bringt.
- 2026-08-14 · `gegrillt → geschaetzt`. **XS** / `beides` / `migration: nein` / `vertragsbruch: nein`.
  Kein Produktivcode auf beiden Seiten — der Aufwand liegt vollständig in den zwei roten Proben.
- 2026-08-14 · Gebaut. **Beide roten Proben je mit Probe UND Gegenprobe**, denn eine Zusicherung, die fällt,
  belegt noch nicht, dass die alte nicht gefallen wäre:

  | Probe | erwartet / gemessen |
  |---|---|
  | Anlegepfad verwirft `categoryId`, **mit** der neuen Prüfung | rot — erwartet `Number`, gemessen `Null` |
  | derselbe Bruch, **ohne** die neue Prüfung | **grün, 1 passed** — das ist der Beleg des Defekts |
  | „Alle wählen"-Klick weggelassen, alte Regex `\d+` | **grün**; der Fall starb erst 10 s später an `/Feinschliff/` |
  | derselbe Klick weg, neue Regex `[1-9]\d*` | rot an der richtigen Stelle, mit Ursache in der Meldung |

  Die dritte Zeile ist mehr als „kann nicht fehlschlagen": die alte Fassung ließ den Fall **weiterlaufen** und
  an einer Stelle sterben, die die Ursache nicht nennt. Beide Eingriffe zurückgenommen, danach **828/828**
  Backend, **280/280** Komponententests, **35/35** E2E, `dotnet format` sauber.
- 2026-08-14 · `geschaetzt` auf `abgenommen`. Beide Zusicherungen sind tragend, beide Proben stehen mit Zahl
  oben. **Rollengang an der laufenden App** (Freigabe 6): Die Ueberschrift auf Schritt 3 liest live
  "Uebungen waehlen (0 gewaehlt)" — die Praemisse von Fall 1 also nicht nur aus dem Code, sondern am
  Bildschirm. Weg dorthin und Regressionszeugen wie bei
  [B-169](B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md) beschrieben.
  **Verifikation:** 828/828 Backend, 280/280 Komponententests, 35 von 36 E2E (der eine Rote ist
  `bilder.spec.ts`, allein gruen, ausserhalb dieses Diffs), `dotnet format` und `markdownlint` sauber, beide
  Reviewer gelaufen. Der `pugling-reviewer` hat den Backend-Anteil ohne Korrektheitsfund passiert und die
  Zahlen unabhaengig nachgefahren.
- 2026-08-14 · **Nachschau: kein Fund.** Benannter Pruefpunkt: Die Vorpruefung im AK-8-Test traegt wirklich —
  `Assert.Equal(JsonValueKind.Number, …)` faellt, wenn der Anlegepfad `categoryId` verliert, und macht die
  Schluss-Zusicherung auf `Null` damit erst zu einer Aussage ueber den *Verlust* statt ueber den
  Ausgangszustand. Die Regex `[1-9]\d*` kann „(0 gewaehlt)" nicht mehr treffen und trifft weiter die
  B-161-Form „(500 gewaehlt, davon 400 …)"; der `heading`-Rollenfilter haelt den Knopf „Auswahl leeren
  (N gewaehlt)" mit demselben Wortmaterial draussen.
