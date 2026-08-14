---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/tests]
aliases: [Suite flackert, AtomaresSchreiben_KeineLeseFehler, Dateirennen unter Volllast]
status: abgenommen
prio: P1
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: beim Bauen von B-163 beobachtet (docs/backlog/B-163-art-und-typ-tragen-dieselben-woerter.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-165 · `AtomaresSchreiben_KeineLeseFehler` fällt unter Volllast — und färbt das Test-Tor rot

## Beobachtung

Beim Verifizieren von B-163 lief `dotnet test Pugling.sln -c Release` dreimal hintereinander auf
**demselben** Arbeitsstand:

| Lauf | Ergebnis |
|---|---|
| 1 | `Failed: 1, Passed: 827, Total: 828` |
| 2 | `Failed: 0, Passed: 828` |
| 3 | `Failed: 0, Passed: 828` |

Der Name fehlte zunächst: der erste Aufruf schnitt die Ausgabe auf die letzten drei Zeilen ab, und darin
stand nur ein Stapel-Fragment (`at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs`).

## Der Name — gefunden, nicht geraten

Die **Nachschau zu B-157** am selben Tag hat ihn unabhängig eingefangen, mit Zahl:

> `dotnet test Pugling.sln -c Release` → 827 grün, **1 rot:
> `OpenApiExampleCatalogConcurrencyTests.AtomaresSchreiben_KeineLeseFehler`**
> (`backend/Pugling.Api.Tests/OpenApiExampleCatalogConcurrencyTests.cs:231`, erwartet 0 Lesefehler,
> gemessen 1). Einzeln nachgefahren (`--filter ~OpenApiExampleCatalogConcurrencyTests`) → **grün.**

Das passt an einer Stelle zur ersten Beobachtung, die keine Vermutung ist: der Test ist `public void` —
**synchron** —, und `MethodBaseInvoker.InvokeWithNoArgs` ist der Aufrufweg einer parameterlosen synchronen
Methode. Die meisten Fälle dieser Suite sind `async Task`. Zwei unabhängige Beobachtungen, dasselbe Bild;
`unverifiziert` ist damit weg.

## Warum das mehr wiegt als ein flackernder E2E

Das **Test-Tor** (`.claude/hooks/test-gate.sh`, Stop-Hook) und die CI hängen an dieser Suite. Ein Test,
der einen von drei Läufen ohne Zutun fällt, blockt also gelegentlich eine **korrekte** Änderung — und
weil ein rotes Tor per Konvention „die verletzte Regel und den Fundort selbst benennt", kostet jeder
solche Fehlalarm eine Fehlersuche an einer Stelle, an der nichts kaputt ist. Das ist die teuerste Sorte
Rauschen: sie untergräbt das Vertrauen in das Tor, das die Disziplin ersetzen sollte.

`B-153` (flackernde `bilder.spec.ts`) ist **nicht** derselbe Befund: das ist ein Playwright-E2E, dieser
hier ein Integrationstest.

## Was der Test tut, und warum die Volllast ihn kippt

Er belegt das Rennen, das B-57 behoben hat: Ein Leser-Thread pollt eine Wegwerf-Datei, während ein Schreiber
sie mitten im Schreiben pausiert; `AtomaresSchreiben_KeineLeseFehler` fordert **0** Lesefehler, und zwar
**zehn Mal hintereinander** („Several independent repetitions, not one lucky pass").

Die Klasse ist an dieser Empfindlichkeit ausdrücklich gebaut und **kennt die Umgebung schon**: ihr
Klassen-Kommentar beschreibt einen „real-time-antivirus-driven exclusive-access blip on **any** fresh write
or rename" auf genau dieser Windows-Maschine und begründet damit, warum das Schreiben von Hand pausiert
statt gegen echte Plattengeschwindigkeit gefahren wird. `CountFailedReadsDuring` nimmt zusätzlich einen
**eigenen** `Thread` statt `Task.Run`, mit dieser Begründung im Code:

> when the full suite runs hundreds of test classes in parallel (xunit.v3 parallelizes collections by
> default, and this project has no `[Collection]` grouping), the ThreadPool queue can be under enough real
> pressure that a queued work item sits waiting well past a 300ms pause

Die Vorkehrungen greifen also gegen ThreadPool-Aushungern — der beobachtete Fehlschlag ist ein **Lesefehler**,
nicht ein ausgehungerter Leser. Was die Volllast zusätzlich verbreitert, ist damit noch nicht belegt und
gehört gemessen, nicht behauptet.

## Offene Punkte

Alle in der Grill-Runde vom 2026-08-14 geschlossen (autonom, `art: Defekt`, Freigabe 1). Der Ist-Stand ist
dabei am Code bestätigt worden: `TryRead` fängt `catch (Exception ex) when (ex is IOException or
JsonException) { return false; }` — **eine** Zahl für zwei Befunde.

1. ~~Was genau schlägt fehl?~~ → Entscheidungen 1 und 2.
2. ~~Sind zehn Wiederholungen das wert?~~ → Entscheidung 3. Die Antwort hat sich **umgedreht**.
3. ~~Ein `[Collection]` für die Klasse?~~ → Entscheidung 4 (zurückgestellt).
4. ~~Wiederholungslauf im Test-Tor?~~ → Entscheidung 5 (nein, unverändert).

## Entscheidungen

1. **Die zwei Befunde werden getrennt gezählt: gesperrte Datei und zerrissener Inhalt.** Begründung: Sie
   bedeuten Verschiedenes. Ein `JsonException` ist **der Fehler, den B-57 behoben hat** — ein Leser sieht
   halb geschriebenen Inhalt. Ein `IOException` ist ein **Umgebungsartefakt**, das der Klassen-Kommentar
   selbst beschreibt (Windows-Echtzeit-Virenscanner hält kurz eine Sperre auf jede frisch geschriebene oder
   umbenannte Datei). *Kosten:* Zwei Zähler statt einem, und die Meldung wird länger — dafür sagt sie beim
   nächsten Rot, welcher der beiden Fälle eintrat.
2. **Nur der zerrissene Inhalt lässt den Fall fallen; die Sperre wird gemeldet, nicht bestraft.**
   Begründung: Die Eigenschaft unter Prüfung ist „ein Leser sieht **nie** einen halb geschriebenen Stand" —
   die hält exakt, `0` ist die richtige Grenze. Die Sperre hängt an der Maschine und ist genau der Grund,
   warum dieser Fall das Test-Tor gelegentlich rot färbte.
   *Kosten, und sie sind der wunde Punkt dieser Story:* Ein Regress, der **ausschließlich** Sperren erzeugt
   (etwa ein Schreiber, der die Zieldatei exklusiv öffnet, statt atomar umzubenennen), käme damit durch. Das
   ist ein benannter blinder Fleck, kein vergessener — und er wiegt weniger als ein Tor, dem man nicht mehr
   glaubt. `OpenApiExampleCatalog.Load` hat ohnehin keinen eigenen Wiederholungsversuch, ein solcher Regress
   würde also in `OpenApiExampleTests` ungefiltert auffallen, nicht hier.
3. **Die zehn Wiederholungen bleiben — und werden dadurch erst sinnvoll.** *Die Empfehlung dieser Story hat
   sich umgedreht:* Sie vermutete, die Wiederholungszahl sei die **Ursache** des Flackerns (zehn Versuche
   multiplizieren die Wahrscheinlichkeit eines Blips mit zehn). Das stimmte, **solange** die Sperre den Fall
   fallen ließ. Nach Entscheidung 2 multipliziert sie nur noch die Aussagekraft über den *zerrissenen Inhalt*.
   *Kosten:* Der Fall bleibt der langsamste der Klasse (zehn Durchgänge à 30 ms Pause).
4. **Kein `[Collection]` für die Klasse — zurückgestellt.** Begründung: Es würde die Suite verlangsamen und
   die Ursache verdecken; nach Entscheidung 2 ist die Parallelität gar nicht mehr der Auslöser. *Kosten:*
   Sollte sich zeigen, dass Sperren auch ohne Volllast auftreten, ist die Frage neu zu stellen.
5. **Kein automatischer Wiederholungslauf im Test-Tor.** Begründung unverändert: Das macht aus einem
   sichtbaren Flackern ein unsichtbares. *Kosten:* keine — nach dieser Story flackert der Fall nicht mehr.

## Akzeptanzkriterien

1. `TryRead` unterscheidet „gesperrt" von „zerrissen"; kein Aufrufer sieht mehr eine Sammel-Zahl.
2. `AtomaresSchreiben_KeineLeseFehler` fällt bei **einem einzigen** zerrissenen Lesevorgang und **nicht**
   bei einer Sperre. Seine Meldung nennt beide Zahlen.
3. Der Nachbar-Fall `UnsicheresSchreiben_ErzeugtLeseFehler` fordert weiterhin einen Fehler — aber
   ausdrücklich einen **zerrissenen**, nicht „irgendeinen". Das ist die schärfere Aussage und heute nicht
   gestellt.
4. Der blinde Fleck aus Entscheidung 2 steht als Kommentar am Fall, nicht nur in dieser Story.
5. Rote Probe **mit Zahl** für beide Fälle.

## Schätzung

**Größe: XS** — ein Rückgabetyp statt `bool`, zwei Zähler, zwei Meldungen, zwei Kommentare. Kein
Produktivcode.

- **`wo: backend`** — ausschließlich `Pugling.Api.Tests`.
- **`migration: nein`**, **`vertragsbruch: nein`** — es wandert nichts außerhalb einer Testdatei.

**Risiken:**

1. **Die rote Probe für „zerrissen" ist die leichte, die für „gesperrt" die schwere.** Zerrissenen Inhalt
   erzeugt der Nachbar-Schreiber (`WriteUnsafePaused`) frei Haus. Eine Sperre absichtlich herzustellen heißt,
   die Datei parallel exklusiv zu öffnen — machbar, aber es ist ein eigenes Stück Testmechanik. Wenn das zu
   teuer wird, ist die ehrliche Antwort, AK 2 zur Hälfte per Konstruktion zu belegen (der `IOException`-Pfad
   ist im Code sichtbar getrennt) und das zu benennen.
2. **`OpenApiExampleEntry` ist ein interner Typ** — der neue Rückgabetyp muss in derselben Datei liegen,
   sonst wächst die Änderung über die Testklasse hinaus.

**Angriffsplan:**

1. `TryRead` gibt statt `bool` ein Ergebnis mit zwei unterscheidbaren Fehlerarten zurück.
2. `CountFailedReadsDuring` zählt beide getrennt.
3. Beide Fälle auf die neue Form ziehen, Meldungen mit beiden Zahlen, Kommentar zum blinden Fleck.
4. Rote Proben: für „zerrissen" den atomaren Schreiber testweise auf den unsicheren umstellen; für
   „gesperrt" eine exklusive Öffnung einschieben — je mit Zahl, je zurücknehmen.

## Testweg

Kein neuer Test, sondern **schärfere Meldungen und Zusicherungen** in den zwei bestehenden Fällen. Die Probe
läuft über das Vertauschen der Schreibarten: der atomare Fall muss fallen, wenn er unsicher schreibt, der
unsichere, wenn er atomar schreibt.

## Verlauf

- 2026-08-13 · Aufgenommen. Beim Verifizieren von B-163 beobachtet: derselbe Stand, drei Läufe, ein Rot.
  Name nicht mitgeschnitten, weil der Aufruf die Ausgabe abschnitt — das Einfangen per `.trx` war darum
  als erster Schritt notiert, nicht die Ursachenanalyse.
- 2026-08-13 · `idee → ausformuliert`, ohne dass der `.trx`-Schritt nötig war: Die **Nachschau zu B-157**
  hat denselben Fehlschlag am selben Tag unabhängig erwischt und den Namen mitgeliefert
  (`AtomaresSchreiben_KeineLeseFehler`, im Einzellauf grün). Die synchrone Signatur des Tests passt zum
  Stapel-Fragment der ersten Beobachtung. Dabei verschoben: Der interessante Fund ist **nicht** die
  Parallelität, sondern dass das Messinstrument selbst „Sperre" und „zerrissener Inhalt" in einem Zähler
  zusammenzieht — und damit nicht sagen kann, ob hier ein Umgebungsartefakt flackert oder der von B-57
  behobene Fehler zurück ist.
- 2026-08-14 · `ausformuliert` auf `gegrillt` auf `geschaetzt`, autonom (`art: Defekt`, Freigabe 1). Fünf
  Entscheidungen. Der Ist-Stand wurde dabei am Code bestätigt: `TryRead` fing `IOException or JsonException`
  in **einem** `catch` und gab `false` zurück.
- 2026-08-14 · Gebaut und `abgenommen`. **Der Bau hat die Prämisse der Klasse korrigiert und meine eigene
  Entscheidung 2 zweimal widerlegt** — beides durch Messung, nicht durch Nachdenken.

  **(1) Der Nachbar-Fall hat nie zerrissenen Inhalt gesehen.** Nach dem Trennen der Zähler meldete
  `UnsicheresSchreiben_ErzeugtLeseFehler`: **`Torn reads: 0, locked reads: 1867`**. Seine gesamte Beweiskraft
  kam also aus **Sperren**. Ursache, mit einer eigenständigen Probe nachgemessen (2351 Ausnahmen, alle
  identisch): `File.OpenRead` fordert `FileShare.Read` — „andere dürfen lesen, nicht schreiben" —, und ein
  bereits offenes **Schreib**-Handle widerspricht dem. Windows verweigert das Öffnen, der Leser kommt gar
  nicht bis zu den Bytes. Zerrissenen Inhalt könnte er nur sehen, wenn der Schreiber Schreibzugriff teilte,
  und das tut weder die Vor-B-57-Form (`File.WriteAllText`) noch der Fix.
  **Folge:** Die Aussage des Falls heißt jetzt „während eines unsicheren Schreibens ist die Zieldatei
  **unlesbar**" — das ist, was er messen kann, und es ist genau das, was B-57 behoben hat.

  **(2) Entscheidung 2 machte den atomaren Fall zahnlos, und die Probe hat es gezeigt.** Mit nur
  `zerrissen == 0` blieb er **grün**, obwohl er unsicher schrieb — denn unsicheres Schreiben erzeugt Sperren,
  keinen zerrissenen Inhalt. Vor dieser Story fing er den Tausch (er zählte beides zusammen); danach nicht
  mehr. Das war eine **Verschlechterung**, eingeführt von mir, gefunden von meiner eigenen Probe.
  Erster Reparaturversuch `ok > 0` fiel ebenfalls durch: `OpenForWriteWithRetry` kämpft erst um sein Handle,
  die Lesevorgänge **davor** gelingen also immer.
  **Was trägt, ist das Verhältnis:** `ok > gesperrt`. Unsicher gemessen **2954 abgewiesen zu 450 erfolgreich**,
  atomar ~2000 erfolgreich zu 0–1 abgewiesen. Der beobachtete Virenscanner-Blip war genau **1** — zwei
  Größenordnungen Abstand.

  **Rote Proben, beide Richtungen, je mit Zahl:**

  | Probe | Ergebnis |
  |---|---|
  | atomarer Fall schreibt unsicher | rot: „2954 reads were denied and only 450 succeeded" |
  | unsicherer Fall schreibt atomar | rot: „Locked reads: 0, torn reads: 0" |

  **Verifikation:** **831/831** Backend, `dotnet format` sauber.
  **Kein Rollengang nötig und keiner ausgefallen:** `wo: backend`, ausschließlich Testcode, keine Fläche am
  Produkt. Der Beleg sind die zwei Proben.
  **Nicht mitgenommen und darum abgelegt:**
  [B-181](B-181-praemisse-der-rennen-klasse-stimmt-nicht.md) — die Klasse behauptet in ihrem Kommentar,
  der Fehler sei „torn/incomplete JSON content", und das trifft auf keinen Leser dieses Repos zu.
