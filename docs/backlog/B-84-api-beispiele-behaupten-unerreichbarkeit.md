---
tags: [typ/story, status/abgenommen, bereich/doku, bereich/backend]
aliases: [Fehlercode nicht erreichbar, api-examples Generator-Vorgabe,
  Verifiziert-Zähler lügt]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: doku
migration: nein
vertragsbruch: nein
quelle: B-81 (Abnahme, Reviewer-Befund außerhalb des Schnitts)
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-08-05
---

# B-84 · Die API-Beispiele behaupten Unerreichbarkeit, wo nur nichts mitgeschnitten wurde

`docs/api-examples/index.md` listet jeden Fehlercode aus `ApiErrors`, den der Doku-Lauf nicht mitgeschnitten
hat, mit dem Satz **„Über HTTP im In-Process-Test nicht erreichbar."** Das ist die Vorgabe des Generators
(`DocsCaptureTests.cs:1242`, gesetzt für jeden Code ohne Eintrag in seiner `reasons`-Tabelle) — und für
mindestens einen Code **nachweislich falsch**: `vocabulary_not_assigned` wird seit B-81 von einem
Integrationstest per HTTP ausgelöst und steht trotzdem in der Liste. Richtig wäre „nicht von
`DocsCaptureTests` erfasst"; das ist etwas anderes und viel harmloser.

## User Story

Als *Entwickler*, der `docs/api-examples/index.md` liest, um eine Testlücke zu finden, möchte ich, dass die
Datei nur behauptet, was der Generator wirklich weiß — nicht, dass ein Code über HTTP unerreichbar ist, wenn
er das nicht geprüft hat —, damit ich keine Test-Lücke übersehe, die längst geschlossen ist, und keine Zeit
in einen HTTP-Pfad stecke, der bereits woanders existiert.

## Ist-Stand am Code

- Die Vorgabe steht in `backend/Pugling.Api.Tests/DocsCaptureTests.cs:1226-1244` (`RenderIndex`): eine
  `reasons`-Tabelle mit **vier** handgeschriebenen, spezifischen Begründungen
  (`bad_request`, `concurrency_conflict`, `rate_limited`, `internal_error` — Zeilen 1228-1231) und ein
  Default-Satz für jeden übrigen fehlenden Code (Zeile 1242): „Über HTTP im In-Process-Test nicht
  erreichbar."
- `docs/api-examples/index.md:61-85` rendert daraus die Sektion „Nicht automatisch erfassbar": 4 Zeilen mit
  echter Begründung (63, 64, 71, 82) und **19 Zeilen mit dem Default-Satz** (65-70, 72-81, 83-85), macht
  23 von 57 Codes, `Verifiziert: 34 / 57` (Zeile 22) zählt den Rest — dieser Zähler selbst ist korrekt, er
  zählt genau das, was `DocsCaptureTests` nachweist, und ist nicht Teil des Defekts.
- Das Problem ist **dokumentiert und bereits einmal für harmlos erklärt worden**:
  `docs/db-struktur-umbau-plan.md:636-640`, Fallstrick 6: „`DocsCaptureTests` zählt nur, was es selbst
  aufruft. Ein neuer Fehlercode erscheint … als „Über HTTP im In-Process-Test nicht erreichbar", auch wenn
  ein anderer Test ihn sehr wohl über HTTP prüft. … das ist der Normalzustand, kein Befund." Das ist exakt
  die Verwechslung, die B-84 jetzt als Defekt einstuft — nicht am Verhalten (das *ist* der Normalzustand),
  sondern an der **Formulierung**, die das Gegenteil von dem behauptet, was tatsächlich der Fall ist.
- **Recherche gegen die echte Suite** (alle 19 betroffenen Codes gegen `backend/Pugling.Api.Tests`
  gegrept, jeweils geprüft, ob die Zusicherung an einem echten `HttpClient`-Aufruf hängt, nicht an einem
  Unit-/Service-Test):
  - **14 von 19 werden bereits per HTTP anderswo in der Suite ausgelöst** — die Behauptung
    „nicht erreichbar" ist für diese 14 schlicht falsch:
    `duplicate_chapter_name` (`CatalogManagementTests.cs:44`), `duplicate_email`
    (`AdultLifecycleTests.cs:160,173,244`), `duplicate_vocabulary_in_exercise`
    (`ExerciseItemsAndProgressTests.cs:54`), `exercise_not_assigned`
    (`TagsRatingsTimetableTests.cs:98,119,177`), `item_not_found` (`MediaLinkTests.cs:156`),
    `media_already_linked` (`MediaLinkTests.cs:73`), `media_link_not_found` (`MediaLinkTests.cs:211`),
    `media_no_alternative` (`MediaSelectionTests.cs:135`), `media_not_an_image`
    (`MediaUploadTests.cs:91`), `media_not_on_card` (`MediaSelectionTests.cs:296`), `media_variant_exists`
    (`MediaStoreTests.cs:96`), `media_variant_not_found` (`MediaStoreTests.cs:151`,
    `MediaLinkTeardownTests.cs:96`), `vocabulary_not_assigned` (`TagsRatingsTimetableTests.cs:207,228,275`
    — der B-81-Beleg aus der Idee).
  - **5 haben tatsächlich keinen HTTP-Test** in der Suite, tragen aber einen über einen normalen Request
    erreichbaren Guard im Controller/Service (kein Wall, nur keine geschriebene Zusicherung):
    `duplicate_profile_name` (`CreatorProfilesController.cs:93,139`), `media_upload_too_large`
    (`MediaAssetsController.cs:222`), `purchase_not_open` (`ShopController.cs:367`, `ShopService.cs:61`),
    `remark_comment_not_found` (`RemarksController.cs:436`), `unknown_exercise_type`
    (`PositionPracticeController.cs`/`PositionTestsController.cs`, mehrere Stellen — Guard gegen einen
    korrupten Übungstyp, der über die reguläre API praktisch nicht entsteht).
  - **`http_error` ist ein Sonderfall**, kein Gegenbeispiel: `ApiErrors.ForStatus(415)` ist der
    Framework-Fallback für Antworten, die den Controller nie erreichen (falscher `Content-Type`); geprüft
    wird er nur als Unit-Test (`ErrorCodeTests.cs:57-58`), nicht über HTTP — für diesen einen Code trifft
    die heutige Formulierung die Lage tatsächlich ziemlich genau.

## Die echte Lücke

Der Satz vermischt zwei verschiedene Aussagen: „von `DocsCaptureTests` nicht mitgeschnitten" (wahr, für
alle 23) und „über HTTP nicht erreichbar" (falsch für mindestens 14 von 19, unbekannt für 5, richtig nur
für den Sonderfall `http_error`). Die Recherche zeigt: das ist **kein** Randfall wie bei
`vocabulary_not_assigned` allein — es ist der **Normalfall** für diese Zeilenklasse. Wer die Liste liest,
bekommt bei drei von vier Zeilen eine falsche Testlage vorgespiegelt. Die Lücke ist damit rein in der
**Formulierung** des Generators, nicht im Testverhalten selbst (das dokumentierte Verhalten aus
`db-struktur-umbau-plan.md` bleibt korrekt: der Generator zählt nur, was er selbst aufruft — das ist
legitim und bleibt so). Eine echte Drei-Wege-Unterscheidung („von `DocsCaptureTests` erfasst" /
„anderswo in der Suite per HTTP ausgelöst" / „im laufenden System nicht erreichbar") wäre die
vollständigste Reparatur, kostet aber eine dauerhafte Kreuz-Test-Erhebung (siehe Entscheidung 1) — für
diesen Defekt reicht, die falsche Behauptung durch eine wahre zu ersetzen.

## Offene Punkte

1. ~~Soll der Generator die Aussage nur umformulieren oder den Unterschied (erfasst / anderswo getestet /
   wirklich unerreichbar) echt abbilden?~~ → siehe Entscheidung 1.
2. ~~Bleiben die vier handgeschriebenen `reasons` nötig, wenn der Default-Satz sich ändert?~~ → siehe
   Entscheidung 2.
3. Die 5 Codes ohne jeden HTTP-Test (`duplicate_profile_name`, `media_upload_too_large`, `purchase_not_open`,
   `remark_comment_not_found`, `unknown_exercise_type`) sind eine echte, wenn auch kleine, Testlücke — aber
   eine andere als die, die B-84 behebt: hier fehlt ein Test, dort eine falsche Zeile in generierter Doku.
   **Empfehlung**: nicht in diese Story ziehen (der Schnitt bliebe sonst unklar, ob B-84 Doku oder
   Testabdeckung behebt), stattdessen bei Bedarf als eigene, kleine Story ernten. Nicht selbst angelegt —
   das würde diesen Auftrag über die eine Datei hinaus erweitern.

## Entscheidungen

1. **Nur umformulieren — keine Drei-Wege-Erhebung über die ganze Suite.** Der Default-Satz wechselt von
   der falschen Behauptung „Über HTTP im In-Process-Test nicht erreichbar." zu einer wahren: „Von
   `DocsCaptureTests` nicht mitgeschnitten — ob ein anderer Test den Code über HTTP auslöst, ist hier nicht
   geprüft." Begründung: Die Recherche oben zeigt, dass 14 von 19 Zeilen bereits anderswo per HTTP getestet
   sind — eine echte Drei-Wege-Kennzeichnung wäre für diese 14 nur eine Bestätigung dessen, was diese
   Story ohnehin schon als Text belegt hat, keine neue laufende Information. Für die restlichen 5 bräuchte
   sie entweder eine Kreuz-Test-Registry (jede der ~15 betroffenen Testdateien müsste ihren HTTP-Treffer an
   den Generator melden) oder eine fragile Heuristik, die Testquelltext nach `Assert.Equal("<code>"` neben
   HTTP-Aufrufen durchsucht. Beides ist ein zweiter, teurer Wahrheits-Mechanismus, der nach dem Prinzip aus
   `docs/testplan.md` („ein Tor über ein Artefakt beweist nur, dass es sich nicht ändert, nicht dass es
   stimmt") ohnehin nur eine neue Momentaufnahme wäre, die beim nächsten Test in einer der 15 Dateien schon
   wieder veraltet. Kosten: eine geänderte Zeichenkette in `RenderIndex` (`DocsCaptureTests.cs:1242`); die
   19 betroffenen Zeilen in `docs/api-examples/index.md` schreibt der nächste Testlauf automatisch neu.
   Nutzen: die Falschbehauptung ist weg, ohne eine zweite Artefakt-Wahrheit zu bauen, die genauso schnell
   verrottet wie die jetzige.
2. **Die vier handgeschriebenen `reasons` bleiben unverändert.** `bad_request`, `concurrency_conflict`,
   `rate_limited`, `internal_error` tragen bereits eine spezifische, wahre Begründung, keinen Pauschalsatz —
   sie sind nicht Träger des Defekts. Kosten: keine.

## Akzeptanzkriterien

1. `RenderIndex` in `DocsCaptureTests.cs` verwendet für jeden nicht selbst mitgeschnittenen Code eine
   Formulierung, die nur behauptet, dass `DocsCaptureTests` ihn nicht erfasst hat — nicht, dass er über
   HTTP unerreichbar ist.
2. Nach einem `DocsCaptureTests`-Lauf zeigt `docs/api-examples/index.md` die neue Formulierung für alle
   (aktuell 19) betroffenen Codes; die vier handgeschriebenen `reasons`-Sätze bleiben unverändert.
3. Das Zitat des alten Satzes in `docs/db-struktur-umbau-plan.md:638` ist nachgezogen (sonst widerspricht
   sich die Doku selbst).
4. Ein Regressionstest belegt die neue Formulierung (liest die generierte `index.md` zurück und prüft,
   dass der alte Satz nicht mehr vorkommt und der neue vorkommt) und war vor der Änderung rot.
5. `markdownlint-cli2` bleibt für die neu erzeugte `index.md` grün.
6. Der `Verifiziert: 34 / 57`-Zähler und seine Bedeutung ändern sich nicht — er war nie die falsche Aussage.

## Schätzung

**Größe: S** — eine Zeichenkette in einer Methode (`DocsCaptureTests.cs:1242`), ein nachgezogenes Zitat in
einem Plandokument, ein Regressionstest, der die zurückgeschriebene Datei liest. Vergleichbar mit dem
Anker-Beispiel „`childId` aus dem Test-Pfad ziehen" (B-01): eine lokalisierte, unstrittige Änderung ohne
Verzweigung. `wo: doku` — der einzige Laufzeit-Effekt ist generierte Markdown-Dokumentation, kein
API-Vertrag und kein Frontend-Pfad ändert sich; die Abnahme prüft `markdownlint-cli2`, keinen
Backend-/Frontend-Reviewer. `migration: nein` (kein Schema berührt), `vertragsbruch: nein` (kein
`Pugling.Contracts`-Typ berührt).

**Risiken**: (a) `docs/db-struktur-umbau-plan.md:638` zitiert den alten Satz wörtlich — wird das vergessen,
widerspricht sich die Doku selbst über den eigenen Fund hinweg. (b) Byte-Stabilität: `DocsCaptureTests`
überschreibt `docs/api-examples/index.md` bei jedem Lauf (siehe Memory „Doku-Capture byte-stabil") — die
neue Zeile darf keinen zeitabhängigen Wert enthalten, sonst wird der Diff bei jedem Lauf neu geschrieben.
(c) Markdownlint-Zeilenregeln (Leerzeilen um Listen) dürfen durch den längeren Satz nicht brechen.

**Angriffsplan** (Backend zuerst, hier die einzige Fläche):

1. `RenderIndex` (`DocsCaptureTests.cs:1242`) auf die neue Formulierung ändern.
2. `dotnet test` gegen `DocsCaptureTests.CaptureAll` laufen lassen — schreibt `docs/api-examples/index.md`
   neu; die 19 betroffenen Zeilen und den `index.md`-Diff gegenlesen.
3. Zitat in `docs/db-struktur-umbau-plan.md:638` nachziehen.
4. Regressionstest ergänzen: im `finally`-Block von `CaptureAll`, direkt nach `WriteMarkdown()` (Zeile
   219), die frisch geschriebene `index.md` zurücklesen und `Assert.DoesNotContain` auf den alten Satz,
   `Assert.Contains` auf den neuen — an dieser Stelle ist Schreiben-vor-Lesen ohne Testreihenfolge-Trick
   garantiert.
5. `dotnet format` / `markdownlint-cli2` grün bestätigen.

**Testweg**: `DocsCaptureTests` selbst, weil der Generator dort geändert wird (siehe Angriffsplan Schritt
4) — kein zusätzlicher Integrationstest, kein E2E, kein `/smoke-test` nötig, da reine
Dokumentationserzeugung ohne Laufzeit-Effekt am Produkt.

## Verlauf

- **2026-08-03** — angelegt aus der Abnahme von [B-81](B-81-vokabel-tags-geben-uebersetzungen-preis.md): der
  `pugling-reviewer` hat den Satz als 🟢 gemeldet, weil der dortige Commit ihn für den neuen Code
  `vocabulary_not_assigned` in ein eingechecktes Artefakt getragen hat. Bewusst **nicht** als Nebenbei-Fix
  dort mitgenommen — es ist eine Änderung am Generator, die 19 Zeilen umschreibt, und die interessante Frage
  (umformulieren oder den Unterschied wirklich abbilden) ist keine, die man in einer Abnahme entscheidet.
  Belegt sind die Fundstelle (`index.md:85`), die Herkunft (`DocsCaptureTests.cs:1242`), die Zahl der
  betroffenen Zeilen (19) und der Gegenbeweis (der B-81-Test löst den Code per HTTP aus); **nicht** belegt
  ist, wie viele der übrigen 18 Codes ebenfalls falsch beschrieben sind — genau das ist die Arbeit des
  Ausformulierens, darum `unverifiziert: true`. `prio: P3` in Analogie zu
  [B-83](B-83-loesungsfeld-regel-residenter-kontext.md) vorgeschlagen (Doku, die von einer im Code schon
  greifenden Lage falsch erzählt) — nicht vom Nutzer bestätigt. Ein Argument für `P2` gibt es: die Aussage
  kann jemanden davon abhalten, einen Test zu schreiben.
- **2026-08-03** — ausformuliert: Ist-Stand gegen den Code geprüft (`docs/api-examples/index.md:61-85`,
  `DocsCaptureTests.cs:1226-1244`) und alle 19 betroffenen Codes gegen die Suite gegrept — 14 werden bereits
  anderswo per HTTP getestet (Belege in „Ist-Stand am Code"), 5 haben keinen HTTP-Test bei erreichbarem
  Guard, `http_error` bleibt ein legitimer Sonderfall (Framework-Fallback, nur unit-getestet). Der bereits
  einmal als „Normalzustand, kein Befund" abgetane Fund (`db-struktur-umbau-plan.md:636-640`) belegt, dass
  die Formulierung genau diese Verwechslung einlädt. `unverifiziert` entfernt.
- **2026-08-03** — gegrillt: beide offenen Fragen aus dem `idee`-Text als Entscheidungen 1 und 2
  beantwortet (Umformulieren statt teurer Drei-Wege-Erhebung; die vier handgeschriebenen `reasons` bleiben
  unverändert) — autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-03** — geschätzt: Größe S, `wo: doku`, `migration: nein`, `vertragsbruch: nein`, Testweg über
  einen Regressionstest in `DocsCaptureTests.CaptureAll` selbst — autonom getroffen, Nutzerauftrag
  2026-08-04.
- **2026-08-05** — im Autonomen Modus gebaut, exakt nach Angriffsplan: `RenderIndex`s Default-Satz
  umformuliert („Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP
  auslöst, ist hier nicht geprüft."), die vier handgeschriebenen `reasons` unverändert. Rote Probe zuerst:
  die neue Zusicherung am Ende von `CaptureAll` (liest `docs/api-examples/index.md` zurück) scheiterte
  gegen den alten Satz, danach grün. Das Zitat in `docs/db-struktur-umbau-plan.md` (Fallstrick 6)
  nachgezogen, dabei die inzwischen veraltete Zahl „21 der 54" entfernt statt aktualisiert (Codes wachsen
  seither weiter, eine feste Zahl im Fließtext veraltet zuverlässig wieder). `dotnet test Pugling.sln
  -c Release` → **728/728 grün**, `markdownlint-cli2` für beide geänderten Dateien grün (die sieben
  gemeldeten Befunde liegen alle in unberührten Fremddateien). Kein Reviewer-Agent nötig (`wo: doku`, per
  Schätzung). Commit `ff0b25b`, dazu dieser. Status → `abgenommen`.
- **2026-08-05** — Nachtrag zur neuen Eintrittsbedingung (README → „Der Rollengang fällt am leichtesten
  weg"): **kein Rollengang geführt, und keiner möglich** — die Änderung wirkt nicht zur Laufzeit für
  Creator, Vater oder Sohn (reine Doku-Erzeugung). Belegt bleiben Suite und Reviewer; das ist hier die
  vollständige Verifikation, keine Lücke.
- **2026-08-05** — **Nachschau** (Selbst-Check): **kein Befund.** Die Behebung ersetzt nicht nur die
  falsche Behauptung, sie **sichert sie ab**: `DocsCaptureTests` prüft, dass der alte Wortlaut nicht mehr
  im erzeugten Dokument steht und der neue drin ist — der Satz kann also nicht zurückkehren, ohne einen
  Test rot zu machen. Die Lese-nach-Schreib-Stelle ist im Kommentar als rennfrei begründet (synchron,
  derselbe Thread), und das deckt sich mit dem, was B-57 im selben Lauf am Katalog-Rennen geändert hat.
