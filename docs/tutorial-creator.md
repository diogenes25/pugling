---
tags: [typ/tutorial, bereich/katalog, rolle/creator, lerntechnik/vokabeln]
---

# Tutorial · Creator — den Lernkatalog bauen

Dieses Tutorial führt Schritt für Schritt durch die **Creator-Rolle**: das Anlegen einer
**Lehrwerk-Reihe** mit ihren **Units** — der Ort, an dem seit [B-106](backlog/B-106-lehrwerkgetriebener-katalog.md)
jede Übung hängt —, den zentralen **Vokabelspeicher** und die Tags. Ein **Fach** (`Subject`) bleibt
daneben bestehen, ist aber nur noch eine **Metadaten-Verknüpfung der Reihe**, kein Elternobjekt der
Übung mehr.

> **Rollen-Brücke:** Technisch heißen die drei Ebenen Creator/Supervisor/Student. Die
> Produkt-/Familienmetapher **Vater/Sohn** bleibt daneben bestehen. Der **Vater hält
> technisch Creator+Supervisor**; der reine Creator-Archetyp ist der **Lehrer (Herr
> Schmidt)** — er baut Inhalte, ohne selbst ein Kind zu steuern. Details:
> [rollen-doku.md](rollen-doku.md).

Alle Beispiele gehen von `http://localhost:5200` aus. Geschützte Aufrufe benötigen einen JWT
im Header `Authorization: Bearer <token>`. Die Swagger-UI liegt unter `/swagger`. Vollständige,
verifizierte Antwort-Bodies stehen in [api-examples/catalog.md](api-examples/catalog.md) und
[api-examples/vocabulary.md](api-examples/vocabulary.md).

---

## Worum es geht

Der **Katalog** ist die globale Übungsbibliothek und die einzige Quelle der Wahrheit für
Lerninhalte. Er ist **kindneutral**: Ein Creator legt Inhalte einmal an, mehrere Supervisor
weisen sie später ihren Kindern zu. Die Trennung ist wichtig:

- **Creator** (`api/v1/creator/…`) — baut Inhalte: Lehrwerk-Reihen, Units, Übungen, Vokabelspeicher, Tags.
- **Supervisor** (`api/v1/supervisor/…`) — verweist über `PlanPosition` auf Katalog-Übungen und
  vergibt Ziel/Punkte/Leitner je Position. Das ist der **nächste Schritt**:
  [tutorial-supervisor.md](tutorial-supervisor.md).

Der Katalog selbst kennt **kein Kind**. Er trägt nur **Metadaten** (Klassenstufe, Schulart,
Quelle, Kategorie) für die spätere Suche und Vorfilterung.

**Seit B-106 (abgenommen 2026-08-05) hängt jede Übung an einer Lehrwerk-`SeriesUnit`, nicht mehr an
einem `Chapter`.** Die Entität `Chapter` existiert nicht mehr; ein `Subject` trägt keine Kapitel-Kinder
mehr, sondern wird nur noch optional mit einer Lehrwerk-Reihe (`TextbookSeries.SubjectId`) verknüpft.
Wer die Struktur einer bestehenden App noch mit `POST creator/subjects/{id}/chapters` kennt: diese
Route gibt es nicht mehr.

---

## 1. Anmelden als Creator (Herr Schmidt)

Der Lehrer meldet sich per PIN an. Seed-Konto in diesem Tutorial: `adultId=2`, PIN `9999`.

```http
POST /api/v1/auth/adult
{ "adultId": 2, "pin": "9999" }
→ { "token": "…", "role": "Creator", … }
```

Der zurückgegebene Token trägt die Rolle `["Creator"]` — Herr Schmidt ist ein reines
**Lehrer-Konto** ([docs/lehrer-konto-plan.md](lehrer-konto-plan.md)), keine Supervisor-Rolle. Wer
hinter dem Token steckt, zeigt der Endpunkt `auth/me`:

```http
GET /api/v1/auth/me
Authorization: Bearer <token>
→ {
  "accountId": 2,
  "role": "Creator",
  "roles": ["Creator"],
  "adultId": 2,
  "childId": null,
  "name": "Herr Schmidt (Englischlehrer)"
}
```

`childId: null` ist hier bewusst — Herr Schmidt ist reiner Inhaltebauer und steuert kein
eigenes Kind. Ein **Vater**-Konto hingegen trägt beide Rollen (`["Creator","Supervisor"]`) und
verhält sich hier identisch. Ab hier setzen alle Beispiele diesen Bearer-Token voraus.

---

## 2. Fach anlegen (optionale Metadaten-Verknüpfung)

Ein Fach ist heute nur noch ein Klassifikations-Objekt für die Reihe — es trägt selbst keine
Übungen mehr:

```http
POST /api/v1/creator/subjects
{ "name": "Biologie" }
→ {
  "id": 5,
  "name": "Biologie",
  "createdAt": "…",
  "categoriesCount": 0
}
```

Die neue `id: 5` merken. `categoriesCount` zählt die fachabhängigen **Arten** (kontrolliertes
Vokabular fürs Vorfiltern, `POST /api/v1/creator/subjects/5/categories { "name": "Vokabeln" }`), nicht
mehr Kapitel — die gibt es nicht mehr.

---

## 3. Verlag anlegen (geteiltes Vokabular)

Seit [B-63](backlog/B-63-lehrwerk-hierarchie.md) ist der Verlag eine **eigene, geteilte** Katalog-Größe
— wie eine Reihe slug-idempotent, aber ohne Owner (einen Verlagsnamen zu nennen ist keine Autorschaft):

```http
POST /api/v1/creator/publishers
{ "name": "Cornelsen" }
→ {
  "id": 2,
  "name": "Cornelsen",
  "slug": "cornelsen",
  "seriesCount": 0,
  "createdAt": "…"
}
```

`seriesCount` zeigt, ob der Verlag noch in Gebrauch ist. Ein zweites `POST` mit demselben Namen liefert
denselben Eintrag zurück (`200` statt `201`) — derselbe Slug-Mechanismus wie bei der Reihe.

---

## 4. Lehrwerk-Reihe anlegen

Die Reihe (`TextbookSeries`) ist der **geteilte, wiederverwendbare** Katalog-Baustein — sie trägt
optional das Fach aus Schritt 2 und den Verlag aus Schritt 3 (`publisherId`, nicht mehr Freitext):

```http
POST /api/v1/creator/textbook-series
{ "name": "Bio compact", "subjectId": 5, "publisherId": 2 }
→ {
  "id": 5,
  "name": "Bio compact",
  "slug": "bio-compact",
  "publisherId": 2,
  "publisherName": "Cornelsen",
  "subjectName": null,
  "subjectId": 5,
  "schoolTypes": "None",
  "sourceLanguage": null,
  "targetLanguage": null,
  "ownerAdultId": 2,
  "isOwn": true,
  "unitCount": 0,
  "gradeMin": null,
  "gradeMax": null,
  …
}
```

`slug` macht die Reihe **idempotent** (derselbe Name führt zu derselbe Zeile). `publisherName` ist
serverseitig aus dem Verlag aufgelöst (nicht mehr gespeicherter Freitext); `gradeMin`/`gradeMax`
aggregieren die Bände der vorhandenen Units (siehe Schritt 5) und sind hier noch `null`. Die neue
`id: 5` merken — sie steckt in allen folgenden Routen.

---

## 5. Unit anlegen — der eigentliche Träger des Stoffs

```http
POST /api/v1/creator/textbook-series/5/units
{
  "label": "Kapitel 1 – Die Zelle",
  "orderIndex": 1,
  "topics": ["Zellorganellen", "Mitose"],
  "vocabularyNotes": "cell, membrane, nucleus"
}
→ {
  "id": 7,
  "seriesId": 5,
  "grade": null,
  "orderIndex": 1,
  "label": "Kapitel 1 – Die Zelle",
  "bookType": "Textbook",
  "topics": ["Zellorganellen", "Mitose"],
  "grammar": null,
  "vocabularyNotes": "cell, membrane, nucleus",
  …
}
```

`grade` ist der **Band** der Reihe (z. B. „Access 8"), hier `null` (Reihe ohne Bände). `Topics`/
`Grammar`/`VocabularyNotes` sind der Stoff, den ein KI-Creator kennen muss, statt ihn zu erraten.
`Topics` ist eine **Liste** (nicht mehr ein Komma-String); `bookType` unterscheidet weitere Bände
derselben Reihe (Lehrbuch/Arbeitsheft/Lehrerhandreichung, Vorgabe `Textbook`). Ergebnis: Unit `id: 7`
in Reihe `id: 5`.

---

## 6. Der Vokabelspeicher — die einzige Quelle der Wahrheit

Vokabeln leben **nicht** in den Übungen, sondern zentral im **Store**. Eine Vokabelübung
enthält nur **Referenzen** auf Store-Einträge. Front/Back/Audio kommen live aus dem Store —
korrigiert man dort ein Wort, ändert es sich überall.

```http
GET /api/v1/creator/vocabulary?take=2
→ [
  {
    "id": 2,
    "key": "en_go_de_gehen",
    "word": "go",
    "translation": "gehen",
    "partOfSpeech": "Verb",
    …
  },
  {
    "id": 3,
    "key": "en_goes_de_geht",
    "word": "goes",
    "translation": "geht",
    "baseFormId": 2,
    …
  }
]
```

Wichtige Punkte:

- **Referenzen sind ID-basiert** (`vocabularyId`), **nicht** key-basiert. Der `key`
  (`en_go_de_gehen`) ist nur ein menschenlesbarer, stabiler Bezeichner; verlinkt wird über die
  numerische `id`.
- `baseFormId` verknüpft Flexionsformen mit ihrer Grundform (hier: `goes` → `go`).
- Suchen im Store: `GET /api/v1/creator/vocabulary?word=go` bzw. `?translation=gehen`.

Wer noch keine passende Vokabel findet, muss sie nicht zwingend vorab von Hand anlegen — der Store
füllt sich beim Inline-Anlegen von Items automatisch (siehe [Schritt 9](#9-items-pflegen--der-wichtige-stolperstein)).

---

## 7. Eine Vokabelübung IN der Unit anlegen

Jetzt die erste typisierte Übung — hängt an der **Unit**, nicht mehr an einem Kapitel:

```http
POST /api/v1/creator/textbook-series/5/units/7/vocabulary
{
  "title": "Zell-Vokabeln",
  "orderIndex": 1,
  "rewardPoints": 10,
  "config": {
    "direction": "front-to-back",
    "refs": [ { "vocabularyId": 2 }, { "vocabularyId": 3 } ]
  }
}
→ {
  "id": 13,
  "seriesUnitId": 7,
  "type": "Vocabulary",
  "title": "Zell-Vokabeln",
  "authorAdultId": 2,
  "isOwn": true,
  …
}
```

Beachtenswert:

- `direction` steuert die Abfragerichtung (`front-to-back` | `back-to-front` | `both`).
- `refs` verweist ID-basiert auf den Store.
- **Materialisierung:** In der POST-Response ist `config.refs` anschließend `null` — die
  Referenzen wurden beim Speichern in **stabile Item-Zeilen** überführt. Die Vokabelpaare
  leben jetzt als eigene Ebene unter `…/items`, nicht mehr in der Config. Die Config trägt danach
  nur noch Einstellungen (Richtung, Sprachen).
- `authorAdultId: 2` + `isOwn: true` — Herr Schmidt ist Autor **und** (Auto-)Owner, darf also ändern.
  `isOwn` = Schreibrecht (Owner **oder** Write-Grant), `isOwner` = Verwaltungsrecht (Owner: löschen, Rechte
  vergeben, Sichtbarkeit umschalten). Andere Creator sehen die Übung mit `isOwn: false` und dürfen sie **nicht**
  ändern, solange ihnen kein Recht erteilt wurde (siehe Abschnitt „Rechte teilen (RWX)").

---

## 8. Items lesen (die materialisierten Vokabelpaare)

```http
GET /api/v1/creator/textbook-series/5/units/7/vocabulary/13/items
→ [
  {
    "id": 15,
    "orderIndex": 0,
    "vocabularyId": 2,
    "front": "go",
    "back": "gehen",
    "_self": "…/items/15",
    "vocabulary": "/api/v1/creator/vocabulary/2"
  },
  {
    "id": 16,
    "orderIndex": 1,
    "vocabularyId": 3,
    "front": "goes",
    "back": "geht",
    …
  }
]
```

Jedes Item ist eine **positionierte Referenz** (`orderIndex`) auf eine Store-Vokabel. `front`/
`back` sind aus dem Store aufgelöst; `vocabulary` verlinkt auf den Store-Eintrag.

Item-CRUD läuft immer über diese Subressource (Route folgt der Übung: Reihe → Unit → Übung → Items):

```http
POST   …/units/7/vocabulary/13/items          # Item anhängen
PATCH  …/units/7/vocabulary/13/items/{itemId} # Item ändern (z. B. lokalen Hinweis)
DELETE …/units/7/vocabulary/13/items/{itemId} # Item entfernen
```

> **Achtung, sobald die Übung in einem Study-Plan genutzt wird:** Items dürfen dann nicht mehr
> gelöscht oder umsortiert werden (sonst kippt bestehender Lernfortschritt auf andere Wörter).
> **An das Ende anhängen bleibt erlaubt.**

---

## 9. Items pflegen — der wichtige Stolperstein

Ein Item lässt sich auf **zwei** Wegen anlegen: per bestehender `vocabularyId` oder inline per
`front`/`back`. Der inline-Weg hat eine Bedingung, die man kennen muss.

### Der Fehler

Ein inline-Item **ohne** dass die Übung Sprachen kennt, schlägt fehl:

```http
POST /api/v1/creator/textbook-series/5/units/7/vocabulary/13/items
{ "front": "cell", "back": "Zelle" }
→ 400  validation_error
"Provide an existing vocabularyId, or front and back (plus the exercise's
 sourceLang/targetLang) to create one."
```

Grund: Um aus `front`/`back` **automatisch** einen neuen Store-Eintrag zu erzeugen, braucht der
Server die Ausgangs- und Zielsprache. Die Übung aus Schritt 7 hat keine `sourceLang`/
`targetLang` in der Config — also fehlt die Information.

### Weg (a): bestehende Store-Vokabel referenzieren

Immer robust — es wird nichts Neues erzeugt, nur verlinkt:

```http
POST /api/v1/creator/textbook-series/5/units/7/vocabulary/13/items
{ "vocabularyId": 1 }
→ {
  "id": 17,
  "vocabularyId": 1,
  "front": "house",
  "back": "Haus",
  …
}
```

### Weg (b): Übung mit Sprachen anlegen, dann inline

Trägt die Übungs-Config `sourceLang`+`targetLang`, funktioniert der inline-Weg — und legt bei
Bedarf **automatisch** einen Store-Eintrag an:

```http
POST /api/v1/creator/textbook-series/5/units/7/vocabulary
{
  "title": "Zell-Vokabeln (mit Sprachen)",
  "orderIndex": 2,
  "rewardPoints": 10,
  "config": {
    "direction": "front-to-back",
    "sourceLang": "en",
    "targetLang": "de",
    "refs": [ { "vocabularyId": 2 } ]
  }
}
→ { "id": 14, "seriesUnitId": 7, … }

POST …/vocabulary/14/items
{ "front": "membrane", "back": "Membran" }
→ {
  "id": 19,
  "vocabularyId": 25,   // neuer Store-Eintrag automatisch angelegt
  "front": "membrane",
  "back": "Membran",
  …
}
```

**Faustregel:** Vokabelübungen, in die du inline neue Wörter tippen willst, gleich mit
`sourceLang`/`targetLang` anlegen. Willst du nur bestehende Store-Wörter verknüpfen, reicht
`vocabularyId`.

---

## 10. Den Katalog durchsuchen (kindneutral, per Metadaten)

Alle Übungen aller Units lassen sich fachübergreifend über ihre Metadaten finden — die
Grundlage, damit Supervisor passende Inhalte für ihr Kind auswählen:

```http
GET /api/v1/creator/exercises?type=Vocabulary&take=3
→ [ ExerciseSummary, … ]
```

Jede Zeile trägt `seriesId`/`seriesUnitId`/`subjectId` (der Weg zur Übung), `gradeMin`/`gradeMax`
(Klassenstufe), `schoolTypes`, `source`, `categoryName` sowie `authorAdultId`/`authorName` und
`isOwn`. Alle Filter sind optional und **UND-verknüpft**:

```http
GET /api/v1/creator/exercises?subjectId=5&type=Vocabulary&search=Zell
→ [
  { "id": 13, "seriesId": 5, "seriesUnitId": 7, "subjectId": 5, "type": "Vocabulary",
    "title": "Zell-Vokabeln", "authorAdultId": 2, "isOwn": true, … },
  { "id": 14, "seriesId": 5, "seriesUnitId": 7, "subjectId": 5, "type": "Vocabulary",
    "title": "Zell-Vokabeln (mit Sprachen)", … }
]
```

`subjectId` filtert **transitiv** über `SeriesUnit.Series.SubjectId` — das Fach hängt an der
Reihe, nicht mehr an der Übung selbst. Übungen mit `authorAdultId: 2` sind die von Herrn Schmidt
(für ihn `isOwn: true`). Standardmäßig darf nur der Owner ändern; alle anderen dürfen lesen und in
ihre Pläne übernehmen (geteilte Bibliothek). Über **RWX-Grants** kann der Owner das gezielt
aufweichen (nächster Abschnitt).

Die Detail-Ansicht einer einzelnen Übung trägt zusätzlich den lesbaren Weg dorthin:

```http
GET /api/v1/creator/exercises/13
→ {
  "id": 13, "seriesId": 5, "seriesUnitId": 7, "seriesUnitLabel": "Kapitel 1 – Die Zelle",
  "subjectId": 5, "subjectName": "Biologie", "type": "Vocabulary", "title": "Zell-Vokabeln",
  "grantCount": 1, "isOwn": true, "isOwner": true, …
}
```

## Rechte teilen (RWX)

Der Katalog bleibt für alle **lesbar** – Read ist bewusst kein Recht. Owner können aber **Write** (ändern)
und **Execute** (in Lehrplan/Klassenarbeit aufnehmen) an einzelne Creator vergeben, und die Ausführbarkeit
für alle abschalten:

- Beim Anlegen/Ändern einer Übung `executePublic: false` setzen → nur Owner und Creator mit Execute-/Write-/
  Owner-Grant dürfen sie noch **zuweisen** (bereits laufende Pläne bleiben unberührt). Default `true`.
- Rechte verwalten (nur Owner) unter `api/v1/creator/exercises/{exerciseId}/grants`:

```http
GET    /api/v1/creator/exercises/13/grants
→ [ { "creatorId": 2, "creatorName": "Herr Schmidt (Englischlehrer)", "permission": "Owner",
      "grantedByAdultId": 2, … } ]
POST   /api/v1/creator/exercises/13/grants     { "creatorId": 3, "permission": "Write" }
DELETE /api/v1/creator/exercises/13/grants/3/Write
```

`permission` ist `Owner` | `Write` | `Execute` (Owner ⊃ Write ⊃ Execute). Der Anleger wird automatisch erster
Owner; der **letzte Owner** kann nicht entfernt werden. Fehlt das Recht: `403 not_author` (ändern),
`403 not_owner` (verwalten/löschen), `403 exercise_not_executable` (zuweisen).

Die Detail-Response (`GET /creator/exercises/{id}`) trägt zusätzlich `grantCount` (Anzahl vergebener Rechte,
inkl. Owner) – so ist erkennbar, dass eine Übung geteilt ist, ohne die volle Liste zu ziehen. Die
**vollständige** Rechteliste gibt es nur owner-only über den `/grants`-Endpunkt.

**Break-Glass-Admin:** Ein als `Adult.IsAdmin` markierter Vater erhält beim Login den `Admin`-Claim und
umgeht **alle** RWX-Prüfungen – gedacht, um im Notfall **verwaiste** (ownerlose) Übungen zu reparieren
(z. B. nachdem der einzige Owner-Vater gelöscht wurde). Das Flag ist bewusst **nicht** per API setzbar,
sondern nur über die DB/Seed (kein Selbst-Rechteausbau).

Damit Übungen gut gefunden werden, beim Anlegen die Metadaten mitgeben (alle optional):
`gradeMin`/`gradeMax`, `schoolTypes` (`[Flags]`, kommasepariert wie `"Realschule, Gymnasium"`,
`"None"` = für alle), `source` (Schulbuch o. Ä.) und `categoryId`. Enums werden als **String**
übertragen.

---

## 11. Testmodus — die Übung nebenwirkungsfrei durchspielen

Bevor eine Übung zugewiesen wird, kann der Creator sie im **Preview** ansehen. Das erzeugt
keinerlei Fortschritt oder Punkte:

```http
GET /api/v1/creator/exercises/13/preview
→ {
  "type": "Vocabulary",
  "stage": 4,
  "typed": true,
  "stages": [
    { "value": 1, "label": "Beide zeigen (Kennenlernen)" },
    { "value": 2, "label": "Selbsteinschätzung" },
    { "value": 6, "label": "Multiple-Choice" },
    { "value": 3, "label": "Buchstabenkästchen" },
    { "value": 4, "label": "Freitext (tippen)" },
    { "value": 5, "label": "Hören → tippen" }
  ],
  "items": [ { "itemIndex": 0, "prompt": "go", … } ]
}
```

`stages` zeigt, in welchen Ausspiel-Stufen die Übung spielbar ist (von der reinen
Kennenlern-Anzeige bis „Hören → tippen"). `typed: true` heißt: Es gibt eine echte
Tipp-Prüfung. So sieht der Creator, was das Kind später sehen wird — ohne Seiteneffekt.

Die Vorschau gibt **keine Lösungen** heraus: Antworten erscheinen weder als Feld noch im
`reveal` (das ist stufenabhängig und bleibt in der Vorschau leer). Nachprüfbar mit einer
Cloze-/Matching-Übung — die Musterlösungen tauchen im Response-Body nicht auf.

> **Ausnahme Essay:** Der Typ hat keine prüfbaren Einzelaufgaben (nur einen Schreibauftrag),
> deshalb antwortet die Vorschau mit `400 no_checkable_content`. Der Auftrag selbst lässt sich
> über das normale `GET …/essays/{id}` lesen.

---

## 12. Die 12 Übungstypen im Überblick

Jeder Typ erbt dasselbe CRUD aus `ExerciseControllerBase<TConfig>`; nur die typ-spezifische
`config` unterscheidet sich. Routenmuster:
`api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/<typ-pfad>`.

Das vollständige Typ-Manifest liefert:

```http
GET /api/v1/creator/exercise-types
```

Es listet alle 12 Typen (auch für den Studenten lesbar, damit das Frontend den passenden Renderer
wählt):

| Typ | Pfad | Zweck (Kurz) |
| --- | --- | --- |
| **Vocabulary** | `/vocabulary` | Vokabelpaare (Store-Refs, Item-CRUD, Leitner) |
| **Reading** | `/reading` | Leseverständnis: Text + Fragen (MC oder Freitext) |
| **Cloze** | `/cloze` | Lückentext mit `{{n}}`-Lücken, optionaler Wortbank |
| **Essay** | `/essays` | Aufsatz mit Wortgrenzen + Bewertungsrubrik |
| **Listening** | `/listening` | Hörverständnis: Audio + Fragen |
| **Grammar** | `/grammar` | Grammatikaufgaben (z. B. Zeitform einsetzen) |
| **Matching** | `/matching` | Paare zuordnen · hat `/check` |
| **Translation** | `/translation` | Sätze übersetzen (mit Alternativen) |
| **Arithmetic** | `/arithmetic` | Feste Rechenaufgaben · hat `/check` |
| **ArithmeticDrill** | `/arithmetic-drill` | Zufalls-Rechnen aus Regeln + Seed · `/generate` + `/check` |
| **List** | `/list` | Auswendig-Liste (geordnet/ungeordnet) · hat `/check` |
| **Birkenbihl** | `/birkenbihl` | Wort-für-Wort-Dekodierung (reine Inhaltsübung, kein Abfragen) |

Bis auf **Birkenbihl** legt man jeden Typ in **einem** POST an: `title`, `orderIndex`,
`rewardPoints` und die typ-spezifische `config`. Der Essay-Typ trägt die Rubrik als
`{ "criterion": "…", "maxScore": … }`-Zeilen (nicht `points` — ein falscher Feldname liefert
`400 unknown_field`, siehe „Unbekannte Felder werden abgelehnt" in der Root-`CLAUDE.md`).

### Sonderfall Birkenbihl: erst anlegen, dann dekodieren

Die Sätze einer Birkenbihl-Übung lassen sich **nicht** vollständig inline mitgeben — jeder Satz
braucht seine Wort-für-Wort-`decoding`, und die erzeugt der Server. Ein POST mit Sätzen ohne
`decoding` scheitert. Der vorgesehene Weg ist zweistufig — Übung **leer** anlegen, dann Sätze
einzeln über die Auto-Dekodierung anhängen:

```http
POST …/units/7/birkenbihl
{ "title": "Zell-Sätze", "orderIndex": 3, "rewardPoints": 15,
  "config": { "learningLang": "en", "nativeLang": "de", "sentences": [] } }
→ 201  { "id": 15, "config": { "nextSentenceId": 1, "nextWordId": 1, "sentences": [] } }

POST …/birkenbihl/15/sentences
{ "learningSentence": "The cell has a membrane.",
  "naturalTranslation": "Die Zelle hat eine Membran." }
→ 201 {
  "sentenceId": 1,
  "result": [
    { "wordId": 1, "learningWord": "The", "gloss": null,      "vocabularyId": null },
    { "wordId": 2, "learningWord": "cell", "gloss": null,      "vocabularyId": null },
    { "wordId": 5, "learningWord": "membrane", "gloss": "Membran", "vocabularyId": 25,
      "_self": "/api/v1/creator/vocabulary/25" }
  ]
}
```

Der Server tokenisiert den Satz und schlägt jedes Wort im Vokabelspeicher nach: Treffer bekommen
Glosse + `vocabularyId`, **unbekannte Wörter kommen mit leerer Glosse** zurück (kein Fehler —
sie lassen sich später per `PUT …/birkenbihl/{id}/words/{wordId}` nachziehen). Bei mehrdeutigen
Wörtern liefert die Antwort zusätzlich `candidates` zur Auswahl. Die `wordId` ist **übungsweit**
eindeutig, damit der Austausch-Endpunkt ein Wort ohne Satz-Segment eindeutig trifft.

Die **vollständige** Typ-Referenz mit vollständigen Config-Schemata und Beispiel-Requests für
jeden Typ steht in [wiki/03 · Übungstypen](../wiki/03-uebungstypen.md). Willst du einen
**neuen** Übungstyp bauen, folge dem etablierten Muster (ein Controller je Typ, kein
Parallel-Stack): [wiki/08 · Erweitern](../wiki/08-erweitern.md).

---

## 13. Tags (kind-skopiert)

Neben den kindneutralen Metadaten können Creator und Supervisor Übungen **taggen** — etwa für
gezieltes Wiederholen oder Klassenarbeiten:

```http
POST /api/v1/creator/tags
{ "name": "Klausur Zelle", "childId": 1 }
```

Tags sind (anders als der übrige Katalog) **kind-skopiert** — eine bewusste Abweichung, damit
sich pro Kind ein eigener Wiederholungsfokus setzen lässt, ohne den geteilten Katalog zu
verändern.

> **Achtung, gilt auch für den reinen Creator:** `childId` ist **Pflicht**, und der Aufrufer muss
> Zugriff auf dieses Kind haben. Herr Schmidt als reiner Lehrer betreut kein Kind — für ihn
> antwortet `POST /creator/tags` (ohne bzw. mit fremder `childId`) mit `403 forbidden`
> (nachgeprüft 2026-08-06). Taggen ist damit faktisch dem **Supervisor** vorbehalten, obwohl der
> Endpunkt unter der Creator-Taxonomie liegt.

---

## Nächster Schritt

Der Katalog steht — Lehrwerk-Reihe, Unit, Übungen, Vokabeln und Tags. Jetzt übernimmt der
**Supervisor**: Er baut aus diesen Inhalten einen trainierbaren Study-Plan, indem er Übungen
als Positionen mit Ziel, Punkten und Leitner-Stufe zuweist. Weiter in
[tutorial-supervisor.md](tutorial-supervisor.md).

---

**Verwandt:** [tutorial-supervisor.md](tutorial-supervisor.md) ·
[tutorial.md](tutorial.md) ·
[wiki/03 · Übungstypen](../wiki/03-uebungstypen.md) ·
[wiki/08 · Erweitern](../wiki/08-erweitern.md) ·
[rollen-doku.md](rollen-doku.md) ·
[backlog/B-106](backlog/B-106-lehrwerkgetriebener-katalog.md) ·
[api-examples/catalog.md](api-examples/catalog.md) ·
[api-examples/vocabulary.md](api-examples/vocabulary.md)
