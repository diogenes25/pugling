---
tags: [typ/story, status/ausformuliert, bereich/tests, bereich/frontend, bereich/backend]
aliases: [0 gewaehlt trifft die Regex, AK-8 ohne Vorbedingung, Zusicherung kann nicht fallen]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-13 zu B-161 und B-157
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-161, B-157]
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
