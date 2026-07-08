# Vokabel-Import (Top-100 Englisch & Französisch)

Kleines Hilfsprogramm, das den Pugling-**Vokabel-Store** über den Batch-Endpunkt
(`POST /api/v1/creator/vocabulary/batch`) mit fertigen Vokabel-Listen füllt.

## Inhalt

```text
tools/vokabel-import/
├── data/
│   ├── en-de-top100.json      # 100 häufigste englische Wörter → Deutsch
│   ├── fr-de-top100.json      # 100 häufigste französische Wörter → Deutsch
│   ├── en-de-101-1000.json    # weitere ~950 häufige englische Wörter → Deutsch (Rang ~101–1000)
│   └── fr-de-101-1000.json    # weitere ~970 häufige französische Wörter → Deutsch (Rang ~101–1000)
├── import-vokabeln.ps1        # Login + Batch-Upload
└── README.md
```

> **Herkunft der 101–1000er-Listen:** aus gängigen Häufigkeitslisten (COCA/Oxford bzw.
> französische Frequenzlisten) zusammengestellt und ins Deutsche übersetzt. Bei der Menge
> lohnt vor dem Einpflegen ein Stichproben-Check der Übersetzungen/Genera.

Die JSON-Dateien folgen exakt dem `CreateVocabularyDto`-Schema des Stores:

```json
{
  "key": "en_time_de_zeit",
  "sourceLanguage": "en",
  "targetLanguage": "de",
  "word": "time",
  "translation": "Zeit",
  "partOfSpeech": 0,
  "version": "1.0",
  "noun": { "article": "die", "genus": "Feminine", "plural": "Zeiten" },
  "verb": null,
  "baseFormKey": null,
  "pronunciationAudioUrl": null
}
```

- **`partOfSpeech`** ist der numerische Enum-Wert (`PartOfSpeech`):
  `0 Noun · 1 Verb · 2 Adjective · 3 Adverb · 4 Pronoun · 5 Preposition ·
  6 Conjunction · 7 Article · 8 Numeral · 9 Interjection · 10 Phrase · 11 Other`.
- **`noun`** (nur bei Substantiven) trägt `article`/`genus`/`plural`,
  **`verb`** (nur bei Verben) `isBaseForm`/`infinitive` – der Rest ist `null`.
- **`key`** ist explizit gesetzt (Muster `{src}_{wort}_{tgt}_{übersetzung}`), damit der
  Batch **idempotent** ist: Ein erneuter Lauf meldet vorhandene Keys als `existing`
  statt Duplikate anzulegen.
- Quelle→Ziel ist **en/fr → de** (deutsche Übersetzung), passend zum Key-Beispiel
  `en_run_verb_laufen` im Store.

## Voraussetzung

Das Backend muss laufen (`cd backend/Pugling.Api && dotnet run`, Standard `:5200`).
Der Import braucht einen **Vater-Login** (der Store ist `[Authorize(Roles = Vater)]`).

## Aufruf

```powershell
# Standard: beide Top-100-Listen, Vater 1 / PIN 0000, gegen localhost:5200
./tools/vokabel-import/import-vokabeln.ps1

# Die großen 101–1000er-Listen (mit Tags gruppiert)
./tools/vokabel-import/import-vokabeln.ps1 -Files ./tools/vokabel-import/data/en-de-101-1000.json -Tags "Englisch 101-1000"
./tools/vokabel-import/import-vokabeln.ps1 -Files ./tools/vokabel-import/data/fr-de-101-1000.json -Tags "Französisch 101-1000"

# Alles auf einmal
./tools/vokabel-import/import-vokabeln.ps1 -Files `
  ./tools/vokabel-import/data/en-de-top100.json, `
  ./tools/vokabel-import/data/fr-de-top100.json, `
  ./tools/vokabel-import/data/en-de-101-1000.json, `
  ./tools/vokabel-import/data/fr-de-101-1000.json

# Andere API/Zugangsdaten
./tools/vokabel-import/import-vokabeln.ps1 -BaseUrl http://localhost:5200 -FatherId 1 -Pin 0000
```

> **Reihenfolge-Hinweis:** Der Batch ist idempotent (gesetzte Keys → `existing`), die Dateien
> können in beliebiger Reihenfolge und mehrfach gefahren werden.

Parameter: `-BaseUrl` (Default `http://localhost:5200`), `-FatherId` (`1`),
`-Pin` (`0000`), `-Files` (Default: beide `data/*.json`), `-Tags` (optional).

> **PIN-Hinweis:** Vater 1 („Papa") hat im Seed die PIN **`0000`**. Die `1111` ist die
> **Sohn**-PIN – ein Vater-Login damit scheitert mit HTTP 401.

Das Skript gibt am Ende eine Zusammenfassung aus (`created` / `existing` / `error`).

## Prüfen

```powershell
# Nach Login (siehe Skript) – Anzahl im Header X-Total-Count
GET /api/v1/creator/vocabulary?sourceLanguage=en&take=100
GET /api/v1/creator/vocabulary?sourceLanguage=fr&take=100
```

Oder in der Vater-Web-UI unter `/vater` (Vokabel-Verwaltung).
