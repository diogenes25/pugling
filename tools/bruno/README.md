# Bruno-Collection-Generator

Erzeugt aus der OpenAPI-Spezifikation der Pugling-API eine vollständige
[Bruno](https://www.usebruno.com/)-Collection im **OpenCollection-`.yml`-Format** –
inklusive Auth, Variablen, Parameter-Tabellen und verifizierten Beispielen.

```text
tools/bruno/
├── generate-bruno.mjs     # der Generator (Node, ohne externe Dependencies)
├── Pugling.Api/           # generierte Collection (in Bruno öffnen)
│   ├── opencollection.yml # Collection-Root (Bearer-Auth {{token}})
│   ├── environments/local.yml
│   ├── <tag>/folder.yml   # ein Ordner je OpenAPI-Tag
│   └── <tag>/<slug>.yml   # ein Request je Endpunkt
└── README.md
```

## Aufruf

```bash
npm run bruno:generate                 # gegen laufendes Backend (http://localhost:5200)
```

Alternativ direkt, z. B. gegen eine gespeicherte OpenAPI-Datei oder ein anderes Ziel:

```bash
node tools/bruno/generate-bruno.mjs --input ./openapi.json --output tools/bruno/Pugling.Api
```

| Flag | Kurz | Default | Bedeutung |
|------|------|---------|-----------|
| `--input`  | `-i` | `http://localhost:5200/openapi/v1.json` | OpenAPI-Quelle (URL **oder** Dateipfad); auch als erstes Positionsargument |
| `--output` | `-o` | `tools/bruno/Pugling.Api` | Zielverzeichnis der Collection |
| `--force`  |      | aus | Zielordner auch ohne Marker als „von uns verwaltet" behandeln (siehe *Sync*) |

> **Voraussetzung** für den Standardaufruf: Das Backend läuft
> (`cd backend/Pugling.Api && dotnet run`, Standard `:5200`).

## In Bruno öffnen

Bruno → *Open Collection* → Ordner `tools/bruno/Pugling.Api` wählen. Environment `local`
aktivieren. Zum Testen zuerst einen **Login** ausführen (Ordner `auth`) – dessen Script legt
`token` ins Environment; alle anderen Requests erben die Bearer-Auth und sind sofort nutzbar.

Mit der [Bruno-CLI](https://docs.usebruno.com/bru-cli/overview) lässt sich ein Ordner auch
headless fahren:

```bash
bru run auth --env local        # aus tools/bruno/Pugling.Api ausführen
```

## Warum nicht einfach Brunos eigener OpenAPI-Import?

Der Generator liefert dasselbe native `.yml`-Format wie Brunos Import, aber gezielt aufgewertet:

| Feature | Umsetzung |
|---------|-----------|
| **Collection-Auth** | `opencollection.yml` trägt Bearer `{{token}}`; Ordner und Requests erben per `auth: inherit`. |
| **Vorbelegte Variablen** | Path-Parameter tragen `{{childId}}` usw. **mit Werten** aus `environments/local.yml` (Brunos Import lässt sie leer). |
| **Sinnvolle Query-Defaults** | Optionale Filter ohne Default sind **abgehakt** (`disabled: true`), damit nicht bei jedem Request `?a=&b=` leer mitgeht; nur `required`/Default-Parameter (z. B. `skip`/`take`) sind aktiv. |
| **Native Beispiele** | Die von `DocsCaptureTests` verifizierten Antworten landen als `examples` – pro Szenario **gepaart** aus Request-Eingabe und Response-Body. |
| **Auto-Token & Chaining** | Login setzt `{{token}}`; andere Requests fangen per `after-response`-Script IDs aus der Antwort ins Environment. |
| **Doku** | Jeder Request bekommt einen `docs`-Block (Titel, Route, Beschreibung). |
| **Saubere Namen** | Dateien = `methode-pfad`-Slug, Ordner = ASCII-Slug des Tags; der volle Titel bleibt in `info.name`. |

## Neu generieren (Sync-Verhalten)

Der Generator schreibt **in-place** und löscht **nicht** den ganzen Zielordner:

- Alle Dateien werden überschrieben/ergänzt; anschließend werden nur *verwaiste* Altdateien
  (z. B. umbenannte Slugs) entfernt.
- Ob überhaupt geprunt wird, entscheidet der Marker `.pugling-generated` (bei jedem Lauf
  geschrieben) bzw. `--force` – so bleibt ein fremdes Verzeichnis unangetastet.
- Schreib-/Löschzugriffe haben kurze Retries gegen transiente Windows-Sperren (Virenscanner,
  Indexer, IDE-Watcher). Eine dauerhaft gesperrte Datei (im Editor geöffnet) bricht den Lauf
  **nicht** ab, sondern wird am Ende als Warnung gelistet.

> **Achtung:** Die Dateien unter `Pugling.Api/` sind generiert. Manuelle Änderungen gehen beim
> nächsten Lauf verloren. Details zur erzeugten Collection: [Pugling.Api/README.md](Pugling.Api/README.md).
