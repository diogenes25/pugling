---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/tests, rolle/creator]
aliases: [Eigentuemer ist Adult 1, Anlegen macht den Aufrufer zum Eigentuemer prueft eine Zahl]
status: abgenommen
prio: P1
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-08-13 zu B-13
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-13]
---

# B-168 · „Anlegen macht den Aufrufer zum Eigentümer" prüft eine Konstante, keine Identität

Die Zusicherung, die B-13s wichtigstes Akzeptanzkriterium tragen soll, vergleicht gegen die Zahl `1`.
Im Seed **ist** das die Id des Vaters — die Zusicherung hält also „Eigentümer ist Adult 1" und nicht
„Eigentümer ist der Aufrufer". Solange kein anderer Creator dasselbe tut, ist der Unterschied unsichtbar,
und niemand tut es.

## Ist-Stand am Code (belegt und selbst nachgeprüft)

```csharp
// backend/Pugling.Api.Tests/FachEigentumTests.cs:52 (im Test Anlegen_MachtDenAufrufer_ZumEigentuemer)
Assert.Equal(1, created.GetProperty("ownerAdultId").GetInt32());
```

`TestApi.AdultAsync(factory)` steht per Vorgabe auf dem geseedeten Vater, `id = 1`
(`backend/Pugling.Api.Tests/TestApi.cs:32`). Dieselbe Verwechslung ein zweites Mal in `:102`.

**Der Grund, warum das trägt, ist die Lücke daneben.** Alle vier `PATCH`-Aufrufe auf ein Fach in der
gesamten Suite (`FachEigentumTests.cs:62,78,300,320`) sind entweder Adult 1 auf eigenem Fach, ein Fremder
auf **fremdem** Fach (403 erwartet) oder ein Seed-Fach (403 erwartet). **Kein Test lässt einen anderen
Creator als den Seed-Vater sein eigenes Fach erfolgreich umbenennen oder löschen.**

Fremde Creator legen durchaus Fächer an (`TeacherAccountTests.cs:85`, `SharedLibraryScenarioTests.cs:29`,
`ExerciseUsageScopeTests.cs:114`), aber keiner liest danach `ownerAdultId` oder schreibt auf das Fach.
Auch `TeacherAccountTests.Lehrer_DarfInhalteAnlegenUndBesitzenWieBisher` — der Fall mit „…UndBesitzen" im
Namen — prüft `isOwner` an der **Übung** (`:126`), nie am Fach.

## Fehlerszenario

`SubjectsController.cs:60` von `OwnerAdultId = fid` auf `OwnerAdultId = 1` ändern (oder auf
`User.AccountId()`, was für Adult 1 dieselbe `1` liefert):

1. Zweit-Creator (Adult 2) → `POST creator/subjects {name:"Latein"}` → `201`, `ownerAdultId: 1`.
2. Derselbe Creator → `PATCH creator/subjects/{id}` → **`403 not_owner` auf sein eigenes, gerade
   angelegtes Fach.** Umbenennen und Löschen sind für jeden Creator außer dem Seed-Vater dauerhaft zu —
   und der Seed-Vater darf dafür jedes fremde Fach umbenennen.
3. Suite: **828/828 grün.** Alle sechs B-13-Fälle bleiben grün: Fall 1 sieht die erwartete `1`, Fall 2
   arbeitet als Adult 1, die Fälle 3–6 erwarten ohnehin `403`.

Das ist genau der Schaden, gegen den B-13 gebaut wurde, nur in der anderen Richtung. **Der
Produktionscode ist heute richtig** — an allen vier Stellen. Was fehlt, ist das Netz: B-13s AK 1 ist zur
Hälfte belegt, AK 3 nur für den einen Eigentümer, dessen Id mit der Konstante zusammenfällt.

## Fehlerfamilie

Die zweite der beiden gemessenen: *eine Zusicherung, die nicht fehlschlagen kann.* Hier in ihrer
tückischsten Form — sie prüft nicht den Ausgangszustand, sondern eine Zahl, die im Ausgangszustand
**zufällig** stimmt. Ein Testname („…MachtDenAufrufer…") behauptet dabei mehr als der Rumpf hält, was die
Verwechslung beim Lesen zudeckt.

## Offene Punkte

Beim Aufnehmen sah der Fix wie fünf determinierte Schritte aus. Beim Grillen kamen **zwei** echte Fragen
heraus.

1. ~~Bleibt der Seed-Fall bestehen, wenn es einen Fremd-Fall gibt?~~ → Entscheidung 1.
2. ~~Tupel oder `record` für die Id des Fremden?~~ → Entscheidung 2.

## Entscheidungen

1. **Der Seed-Fall bleibt, aber er kann die Identität nicht tragen — und sagt das künftig selbst.**
   *Beim Bauen korrigiert:* Der erste Entwurf dieser Entscheidung wollte ihn „zusätzlich identitätsbasiert"
   machen. Das ist **nicht möglich**: Die Id seines Aufrufers *ist* konstruktionsbedingt die `1`
   (`TestApi.AdultAsync` hat `id = 1` als Vorgabe), also stimmt jeder Vergleich — auch ein „identitätsbasiert"
   geschriebener — bei einem hartkodierten `OwnerAdultId = 1` weiterhin. Ein benannte Konstante hätte die
   Absicht dokumentiert und **null** Erkennungskraft hinzugefügt: Kosmetik.
   Er bleibt trotzdem, denn er hält etwas anderes, das gebraucht wird: dass Anlegen **überhaupt** einen
   Eigentümer setzt und `isMine` wahr wird — auf dem Pfad, den die ganze übrige Suite benutzt. Was fehlt,
   liefert allein der Fremd-Fall. *Kosten:* Zwei Fälle mit verschiedenen Aufgaben, und ein Kommentar, der
   sagt welcher was hält — sonst liest der nächste den Seed-Fall wieder als Identitätsbeleg.
2. **Ein Tupel, kein `record`.** Begründung: Die Id wird an genau zwei Stellen gebraucht und verlässt die
   Testklasse nie; ein benannter Typ für `(HttpClient, int)` wäre Bauwerk ohne Leser. *Kosten:* Kommt eine
   dritte Angabe hinzu, wird das Tupel unleserlich und muss dann doch ein `record` werden — dann aber mit
   einem Grund.

## Akzeptanzkriterien

1. Ein Fall vergleicht gegen die Id eines Aufrufers, die **nicht** `1` ist — das ist der einzige, der ein
   hartkodiertes `OwnerAdultId = 1` fangen kann. Der Seed-Fall trägt einen Kommentar, der benennt, was er
   hält (Eigentümer wird gesetzt, `isMine` stimmt) und was nicht (die Identität).
2. Ein fremder Creator darf sein **eigenes** Fach umbenennen (`200`) — der heute vollständig fehlende Fall.
3. Der geseedete Vater bekommt auf dem Fach des Fremden `403 not_owner`.
4. Rote Probe **mit Zahl**: `OwnerAdultId = 1` hartkodiert, und es fallen genau die neuen Fälle.

## Angriffsplan

Das Material liegt schon da: `ZweiterCreatorAsync` (`FachEigentumTests.cs:16-24`) liest die Adult-Id des
Fremden bereits und **wirft sie weg**.

1. `ZweiterCreatorAsync` gibt die Id mit zurück (Tupel oder ein kleines `record`).
2. Beide Konstanten-Vergleiche (`:52`, `:102`) gegen die Id des tatsächlichen Aufrufers stellen.
3. **Den fehlenden Fall ergänzen** — er ist der tragende: Der Fremde legt sein **eigenes** Fach an →
   `ownerAdultId == seineId`, `isMine == true`; sein `PATCH` darauf → `200`.
4. Gegenrichtung, heute ebenfalls ungeprüft: Adult 1 auf das Fach des Fremden → `403 not_owner`.
5. Rote Probe **mit Zahl**: `OwnerAdultId = 1` hartkodieren und belegen, dass jetzt genau die neuen Fälle
   fallen — nicht „irgendetwas wird rot".

**Testweg**: `FachEigentumTests` erweitern, keine neue Datei. Kein Frontend-Anteil, kein Schema, kein
Vertrag.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-13, dort belegt und von mir gegengeprüft
  (`TestApi.cs:32`, die vier `PATCH`-Fundstellen, `ZweiterCreatorAsync`). `ausformuliert` direkt, weil der
  Ist-Stand aus dem Code belegt ist und der offene Punkt keiner ist: der fehlende Fall steht fest.
- 2026-08-14 · `ausformuliert` auf `gegrillt`, autonom (`art: Defekt`, Freigabe 1). Zwei Entscheidungen. Die
  tragende: Der Seed-Fall wird **nicht** durch den Fremd-Fall ersetzt, sondern zusätzlich
  identitätsbasiert — der Seed-Pfad ist der, den die ganze übrige Suite benutzt, und ein Regress, der nur
  fuer Adult 1 bricht, fiele sonst nirgends auf.
- 2026-08-14 · `gegrillt` auf `geschaetzt`. **XS** / `backend` / `migration: nein` / `vertragsbruch: nein` —
  es aendert sich ausschliesslich Testcode.
- 2026-08-14 · Gebaut und `abgenommen`. **Rote Probe mit Zahl, und sie ist der Kern dieser Story:**
  `OwnerAdultId = 1` in `SubjectsController.cs:60` hartkodiert -> **829 gruen, 1 rot**, und der Rote ist genau
  `FremderCreator_BesitztSeinEigenesFach_UndDarfEsAendern`. Alle alten B-13-Faelle blieben gruen. Der
  `pugling-reviewer` hat die Probe in einer eigenen Kopie nachgestellt und dieselben Zahlen gemessen.
  Zuruecknahme, danach 831/831.
  **Eine eigene Entscheidung wurde beim Bauen widerlegt:** Entscheidung 1 wollte den Seed-Fall „zusaetzlich
  identitaetsbasiert" machen. Das ist unmoeglich - die Id seines Aufrufers *ist* konstruktionsbedingt die `1`,
  jeder Vergleich stimmt also auch bei hartkodierter Konstante. Das waere Kosmetik gewesen. Er bleibt mit
  einem Kommentar, der sagt was er haelt (ein Eigentuemer wird gesetzt, `isMine` stimmt) und was nicht.
  **Zwei Luecken aus dem Review nachgezogen:** der Loeschpfad war noch **nie** von einem Eigentuemer != 1
  gelaufen, und die PATCH-Antwort wurde nur auf den Status geprueft, obwohl die Update-Projektion `isMine`
  neu baut. Beides jetzt im Fall.
  **Verifikation:** 831/831 Backend, 280/280 Komponententests, 36/36 E2E, `dotnet format` sauber,
  `pugling-reviewer` ohne Korrektheitsfund am Fall selbst. Rollengang: `wo: backend`, reine Testarbeit - der
  Beleg ist die Probe, und der Sprint-Rollengang lief an B-178s Flaeche.
