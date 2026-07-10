---
tags: [typ/tutorial, bereich/katalog, rolle/creator, lerntechnik/vokabeln]
---

# Tutorial · Creator — den Lernkatalog bauen

Dieses Tutorial führt Schritt für Schritt durch die **Creator-Rolle**: das Anlegen des
gemeinsamen, **kindneutralen** Lernkatalogs — Fächer → Kapitel → typisierte Übungen —
sowie den zentralen **Vokabelspeicher** und die Tags.

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

- **Creator** (`api/v1/creator/…`) — baut Inhalte: Fächer, Kapitel, Übungen, Vokabelspeicher, Tags.
- **Supervisor** (`api/v1/supervisor/…`) — verweist über `PlanPosition` auf Katalog-Übungen und
  vergibt Ziel/Punkte/Leitner je Position. Das ist der **nächste Schritt**:
  [tutorial-supervisor.md](tutorial-supervisor.md).

Der Katalog selbst kennt **kein Kind**. Er trägt nur **Metadaten** (Klassenstufe, Schulart,
Quelle, Kategorie) für die spätere Suche und Vorfilterung.

---

## 1. Anmelden als Creator (Herr Schmidt)

Der Lehrer meldet sich per PIN an. Seed-Konto in diesem Tutorial: `fatherId=2`, PIN `9999`.

```http
POST /api/v1/auth/father
{ "fatherId": 2, "pin": "9999" }
→ { "token": "…", "role": "Supervisor", … }
```

Der zurückgegebene Token trägt die Rollen `["Creator","Supervisor"]`. Wer hinter dem Token
steckt, zeigt der Endpunkt `auth/me`:

```http
GET /api/v1/auth/me
Authorization: Bearer <token>
→ {
  "accountId": 2,
  "role": "Supervisor",
  "roles": ["Creator", "Supervisor"],
  "fatherId": 2,
  "childId": null,
  "name": "Herr Schmidt (Englischlehrer)"
}
```

`childId: null` ist hier bewusst — Herr Schmidt ist reiner Inhaltebauer und steuert kein
eigenes Kind. Trotzdem enthält sein Token die `Creator`-Rolle, mit der die gesamten
`api/v1/creator/…`-Routen offenstehen. Ab hier setzen alle Beispiele diesen Bearer-Token voraus.

---

## 2. Fach anlegen

Jede Übung lebt in einem Kapitel eines Fachs. Also zuerst das Fach:

```http
POST /api/v1/creator/subjects
{ "name": "Biologie" }
→ {
  "id": 5,
  "name": "Biologie",
  "createdAt": "…",
  "chaptersCount": 0
}
```

Die neue `id: 5` merken — sie steckt in allen folgenden Routen.

---

## 3. Kapitel anlegen

```http
POST /api/v1/creator/subjects/5/chapters
{ "name": "Zelle", "orderIndex": 1 }
→ {
  "id": 7,
  "subjectId": 5,
  "name": "Zelle",
  "orderIndex": 1,
  "exercisesCount": 0
}
```

`orderIndex` bestimmt die Reihenfolge der Kapitel im Fach. Ergebnis: Kapitel `id: 7` im
Fach `id: 5`.

> **Optional:** Pro Fach lassen sich **fachabhängige Arten** als kontrolliertes Vokabular für
> die Vorfilterung anlegen (`POST /api/v1/creator/subjects/5/categories { "name": "Vokabeln" }`).
> Die zurückgegebene `categoryId` kann man dann beim Anlegen einer Übung mitgeben.

---

## 4. Der Vokabelspeicher — die einzige Quelle der Wahrheit

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
füllt sich beim Inline-Anlegen von Items automatisch (siehe [Schritt 7](#7-items-pflegen--der-wichtige-stolperstein)).

---

## 5. Eine Vokabelübung anlegen

Jetzt die erste typisierte Übung im Kapitel — eine **Vocabulary**-Übung, die zwei
Store-Vokabeln referenziert:

```http
POST /api/v1/creator/subjects/5/chapters/7/vocabulary
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
  "chapterId": 7,
  "type": "Vocabulary",
  "title": "Zell-Vokabeln",
  "authorFatherId": 2,
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
- `authorFatherId: 2` + `isOwn: true` — die Übung gehört Herrn Schmidt. Andere Creator sehen
  sie in der Suche mit `isOwn: false` und dürfen sie **nicht** ändern (nur der Autor).

---

## 6. Items lesen (die materialisierten Vokabelpaare)

```http
GET /api/v1/creator/subjects/5/chapters/7/vocabulary/13/items
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
`back` sind aus dem Store aufgelöst; `vocabulary` verlinkt auf den Store-Eintrag. Der
Engine-Index beim Spielen ist die Listenposition (`orderIndex`) — er bleibt zum alten
`ItemIndex` kompatibel.

Item-CRUD läuft immer über diese Subressource:

```http
POST   …/vocabulary/13/items          # Item anhängen
PATCH  …/vocabulary/13/items/{itemId} # Item ändern (z. B. lokalen Hinweis)
DELETE …/vocabulary/13/items/{itemId} # Item entfernen
```

> **Achtung, sobald die Übung in einem Study-Plan genutzt wird:** Items dürfen dann nicht mehr
> gelöscht oder umsortiert werden (sonst kippt bestehender Lernfortschritt auf andere Wörter).
> **An das Ende anhängen bleibt erlaubt.**

---

## 7. Items pflegen — der wichtige Stolperstein

Ein Item lässt sich auf **zwei** Wegen anlegen: per bestehender `vocabularyId` oder inline per
`front`/`back`. Der inline-Weg hat eine Bedingung, die man kennen muss.

### Der Fehler

Ein inline-Item **ohne** dass die Übung Sprachen kennt, schlägt fehl:

```http
POST /api/v1/creator/subjects/5/chapters/7/vocabulary/13/items
{ "front": "cell", "back": "Zelle" }
→ 400  validation_error
"Provide an existing vocabularyId, or front and back (plus the exercise's
 sourceLang/targetLang) to create one."
```

Grund: Um aus `front`/`back` **automatisch** einen neuen Store-Eintrag zu erzeugen, braucht der
Server die Ausgangs- und Zielsprache. Die Übung aus Schritt 5 hat keine `sourceLang`/
`targetLang` in der Config — also fehlt die Information.

### Weg (a): bestehende Store-Vokabel referenzieren

Immer robust — es wird nichts Neues erzeugt, nur verlinkt:

```http
POST /api/v1/creator/subjects/5/chapters/7/vocabulary/13/items
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
POST /api/v1/creator/subjects/5/chapters/7/vocabulary
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

POST …/vocabulary/{neueId}/items
{ "front": "membrane", "back": "Membran" }
→ {
  "id": 19,
  "vocabularyId": 26,   // neuer Store-Eintrag automatisch angelegt
  "front": "membrane",
  "back": "Membran",
  …
}
```

**Faustregel:** Vokabelübungen, in die du inline neue Wörter tippen willst, gleich mit
`sourceLang`/`targetLang` anlegen. Willst du nur bestehende Store-Wörter verknüpfen, reicht
`vocabularyId`.

---

## 8. Den Katalog durchsuchen (kindneutral, per Metadaten)

Alle Übungen aller Kapitel lassen sich fachübergreifend über ihre Metadaten finden — die
Grundlage, damit Supervisor passende Inhalte für ihr Kind auswählen:

```http
GET /api/v1/creator/exercises?type=Vocabulary&take=3
→ [ ExerciseSummary, … ]
```

Jede Zeile trägt `gradeMin`/`gradeMax` (Klassenstufe), `schoolTypes`, `source`, `categoryName`
sowie `authorFatherId`/`authorName` und `isOwn`. Alle Filter sind optional und **UND-verknüpft**:

```http
GET /api/v1/creator/exercises?subjectId=5&grade=9&schoolType=Gymnasium&type=Vocabulary&search=Zell
```

Übungen mit `authorFatherId: 2` sind die von Herrn Schmidt (für ihn `isOwn: true`). Nur der
Autor darf eine Übung ändern; alle anderen dürfen sie lesen und in ihre Pläne übernehmen
(geteilte Bibliothek).

Damit Übungen gut gefunden werden, beim Anlegen die Metadaten mitgeben (alle optional):
`gradeMin`/`gradeMax`, `schoolTypes` (`[Flags]`, kommasepariert wie `"Realschule, Gymnasium"`,
`"None"` = für alle), `source` (Schulbuch o. Ä.) und `categoryId`. Enums werden als **String**
übertragen.

---

## 9. Testmodus — die Übung nebenwirkungsfrei durchspielen

Bevor eine Übung zugewiesen wird, kann der Creator sie im **Preview** ansehen. Das erzeugt
keinerlei Fortschritt oder Punkte:

```http
GET /api/v1/creator/exercises/13/preview
→ {
  "type": "Vocabulary",
  "stage": 4,
  "typed": true,
  "stages": [
    { "value": 2, "label": "Selbsteinschätzung" },
    { "value": 6, "label": "Multiple-Choice" },
    { "value": 3, "label": "Buchstabenkästchen" },
    { "value": 4, "label": "Freitext (tippen)" },
    { "value": 5, "label": "Hören → tippen" }
  ],
  "items": [ { "itemIndex": 0, "prompt": "go", … } ]
}
```

`stages` zeigt, in welchen Ausspiel-Stufen die Übung spielbar ist (von reiner
Selbsteinschätzung bis „Hören → tippen"). `typed: true` heißt: Es gibt eine echte
Tipp-Prüfung. So sieht der Creator, was das Kind später sehen wird — ohne Seiteneffekt.

---

## 10. Die 12 Übungstypen im Überblick

Jeder Typ erbt dasselbe CRUD aus `ExerciseControllerBase<TConfig>`; nur die typ-spezifische
`config` unterscheidet sich. Routenmuster:
`api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/<typ-pfad>`.

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

Die **vollständige Typ-Referenz** mit vollständigen Config-Schemata und Beispiel-Requests für
jeden Typ steht in [wiki/03 · Übungstypen](../wiki/03-uebungstypen.md). Willst du einen
**neuen** Übungstyp bauen, folge dem etablierten Muster (ein Controller je Typ, kein
Parallel-Stack): [wiki/08 · Erweitern](../wiki/08-erweitern.md).

---

## 11. Tags (kind-skopiert)

Neben den kindneutralen Metadaten können Creator und Supervisor Übungen **taggen** — etwa für
gezieltes Wiederholen oder Klassenarbeiten:

```http
POST /api/v1/creator/tags
{ "name": "Klausur Zelle", "childId": 1 }
```

Tags sind (anders als der übrige Katalog) kind-skopiert, wenn eine `childId` mitgegeben wird —
so lässt sich pro Kind ein eigener Wiederholungsfokus setzen, ohne den geteilten Katalog zu
verändern.

---

## Nächster Schritt

Der Katalog steht — Fächer, Kapitel, Übungen, Vokabeln und Tags. Jetzt übernimmt der
**Supervisor**: Er baut aus diesen Inhalten einen trainierbaren Study-Plan, indem er Übungen
als Positionen mit Ziel, Punkten und Leitner-Stufe zuweist. Weiter in
[tutorial-supervisor.md](tutorial-supervisor.md).

---

**Verwandt:** [tutorial-supervisor.md](tutorial-supervisor.md) ·
[tutorial.md](tutorial.md) ·
[wiki/03 · Übungstypen](../wiki/03-uebungstypen.md) ·
[wiki/08 · Erweitern](../wiki/08-erweitern.md) ·
[rollen-doku.md](rollen-doku.md) ·
[api-examples/catalog.md](api-examples/catalog.md) ·
[api-examples/vocabulary.md](api-examples/vocabulary.md)
