---
tags: [typ/plan, bereich/api, bereich/frontend, bereich/doku]
aliases: [Etappe 2, Vertrag umbenennen, AdultResponse, auth/adult]
---

# Etappe 2: den Vertrag von `father` auf `adult` nachziehen

Status: **umgesetzt (2026-07-29).** Etappe 1 (Entität + Datenbank) steckt in `ba21dd8`, Etappe 2 – der
Vertrag – ist damit nachgezogen. Die Datei bleibt als Begründung stehen: das Inventar unten beschreibt den
Stand *vorher*, die Regel „was nicht mitwandert" gilt weiter.

Über das Inventar hinaus mitgezogen, weil es sonst ins Leere gezeigt hätte: der Swagger-Hinweis auf den
Login-Pfad, die REST-Client-Variablen in `.vscode/settings.json` (`adultId`/`adultPin`, passend zu
`docs/REST/*.http`), der Bruno-Generator (`tools/bruno/generate-bruno.mjs`), die Dev-Skripte
(`login_father` → `login_adult`) und `tools/vokabel-import` (`-FatherId` → `-AdultId`).

**Bewusst nicht angefasst** – rein interne Namen, die kein Konsument sieht: `EnsureForFatherAsync`,
`FatherOwnsChildAsync`, `ListingsForFatherAsync`, `IsOwnedBy(authorFatherId, fatherId)` und lokale
`fatherId`-Variablen in Controllern/Services. Sie bleiben Aufräumarbeit ohne Vertrags-Wirkung.

## Ausgangslage

Etappe 1 hat die **Entität** `Father` → `Adult` umbenannt, samt Datenbank-Migration
(`20260728230358_RenameFatherToAdult`). Der **Vertrag blieb bewusst unberührt**, damit der Umbau prüfbar
blieb: dass die 25 Playwright-E2E ohne eine einzige Frontend-Änderung durchliefen, war der Beleg dafür,
dass nur Internes bewegt wurde.

Damit steht heute eine **bewusste Inkonsistenz**: der Code sagt `Adult`, der Vertrag sagt `father`. Sie ist
in [CLAUDE.md](../CLAUDE.md) dokumentiert, damit sie nicht als Versehen gelesen wird. Etappe 2 löst sie auf.

## Die eine Regel, ohne die das schiefgeht

**„Vater" ist meistens richtig.** Zu eng war der *Typ*, nicht das Wort. Eine globale Ersetzung zerstört
Bedeutung. Nicht anfassen:

- `SupervisorRelation.Father` – die Verwandtschaftsangabe (neben `Mother`, `Grandparent`).
- `EnsureForFatherAsync` – legt genau **ein Vater-Konto** an (Creator + Supervisor), im Gegensatz zu
  `EnsureForTeacherAsync`.
- Der Token-Claim **`fid`** – er steckt in bereits ausgestellten Tokens. Umbenennen macht jede offene
  Sitzung ungültig, für einen Namen, den niemand sieht.
- Deutsche Prosa über den *Vater als Person* in `docs/`, `wiki/` und in der Oberfläche.
- Die Rolle heißt weiter `Supervisor`; „Vater" ist nur ihre häufigste Ausprägung.

## Inventar (vollständig, gemessen am Stand `ba21dd8`)

### Vertrag – `backend/Pugling.Contracts` (9 Symbole)

| heute | neu |
|---|---|
| `FatherResponse` | `AdultResponse` |
| `CreateFatherDto` | `CreateAdultDto` |
| `UpdateFatherDto` | `UpdateAdultDto` |
| `FatherLoginDto` | `AdultLoginDto` |
| `FatherLoginDto.FatherId` | `.AdultId` |
| `MeResponse.FatherId` | `.AdultId` |
| `ExerciseSummary.AuthorFatherId`, `ExerciseDetail.AuthorFatherId` | `AuthorAdultId` |
| `TextbookSeriesResponse.OwnerFatherId`, `CreatorProfileResponse.OwnerFatherId` | `OwnerAdultId` |
| `GrantResponse.GrantedByFatherId` | `GrantedByAdultId` |

Die Namen sind **global eindeutig** zu halten (der OpenAPI-Generator schlüsselt Schemas über den einfachen
Typnamen) – siehe [Pugling.Contracts/CLAUDE.md](../backend/Pugling.Contracts/CLAUDE.md).

### Routen (2)

- `api/v1/supervisor/fathers` → `api/v1/supervisor/adults` (`FathersController` → `AdultsController`).
- `api/v1/auth/father` → `api/v1/auth/adult`. **Das ist die wichtigste:** ein Lehrer-Konto meldet sich
  darüber an, und der Name behauptet etwas Falsches. `LoginFather` → `LoginAdult`.

Dass wir das dürfen, steht in CLAUDE.md: bis zur Publikation bleiben wir bei `1.0` und ändern frei.

### Client – `backend/Pugling.Client` (1 Stelle)

`SupervisorApi`/`AuthApi`: die Pfade und der DTO-Name. Danach `dotnet build Pugling.sln` – der Client ist
der Nachbar, den ein Contracts-Umbau bricht (der Edit-Hook baut nur das besitzende Projekt).

### Frontend – 45 Vorkommen, 8 Bezeichner

`fatherId` (18), `FatherResponse` (7), `loginFather` (5), `registerFather` (4), `CreateFatherDto` (3),
`UpdateFatherDto` (3), `authorFatherId` (2), `updateFather` (1), `lastFatherId` (1).

Zwei Entscheidungen, die dort zu treffen sind:

1. **Beschriftung „Vater-Id".** Sie steht im Login (`#fid`-Feld) und ist für ein Lehrer-Konto schon heute
   falsch – dasselbe Formular bedient beide. Vorschlag: **„Deine Id"**, plus im Fehlertext „Id oder PIN
   falsch." Die E2E greifen auf `#fid` (Feld-Id, bleibt) und auf den Knopf „Anmelden" – von der Beschriftung
   hängt nur `e2e/lehrer-konto.spec.ts` ab, wenn dort Text geprüft wird.
2. **`localStorage`-Schlüssel `pugling.lastFatherId`.** Das ist **Nutzerdaten**. Wer ihn hart umbenennt,
   nimmt jedem die vorbelegte Id. Also: neuer Schlüssel `pugling.lastLoginId`, und beim Lesen **einmal**
   auf den alten zurückfallen (`?? localStorage.getItem("pugling.lastFatherId")`). Der Rückfall darf
   drinbleiben; er kostet eine Zeile.

### Doku – 23 Dateien

Darunter `docs/REST/*.http` (ausführbare Tutorials, Routen ändern sich), `docs/tutorial-*.md`,
`wiki/*.md`. **Nicht von Hand** anfassen: `docs/api-examples/*` und
`backend/Pugling.Api/OpenApi/openapi-examples.generated.json` – die schreibt `DocsCaptureTests` bei jedem
Testlauf neu.

## Reihenfolge

1. **Contracts** umbenennen → `dotnet build Pugling.sln` und die Fehlerliste als Arbeitsvorrat nehmen
   (so lief Etappe 1: den Compiler die Folgestellen zeigen lassen).
2. **API**: Controller-Klassen, Routen, Mapping-Stellen. In Etappe 1 wurden dort die Entity-Namen schon auf
   `Adult` gezogen – die Mappings sehen heute aus wie `AuthorFatherId: e.AuthorAdultId` und werden zu
   `AuthorAdultId: e.AuthorAdultId`, also einfacher.
3. **Client** (eine Datei), dann `dotnet test Pugling.sln`.
4. **Frontend**: `types.ts`, `api.ts`, `VaterLogin`, `VaterProfil`, `ExerciseAttribution` (nutzt
   `authorFatherId`), E2E. Dann `npm run build`, `npm test`, `npx playwright test`.
5. **Doku + `.http`**, zuletzt. Die generierten Beispiele kommen aus dem Testlauf.

## Verifikation

```bash
dotnet build Pugling.sln          # muss 0 Warnungen bleiben
dotnet test Pugling.sln           # 491 Tests
cd frontend && npm run build && npm test && npx playwright test   # 21 + 25
dotnet format Pugling.sln
```

Playwright braucht Port 5200 **frei** (eigene Wegwerf-DB, `reuseExistingServer: false`) – die laufende
Instanz vorher stoppen. Der Edit-Hook kann `Pugling.Api` nicht bauen, solange die Instanz läuft (Dateisperre
auf `Pugling.Api.exe`); für Backend-Arbeit also stoppen und am Ende wieder starten.

Keine Migration nötig: Etappe 2 berührt das Schema nicht.

## Startsatz für eine frische Sitzung

> Setze `docs/father-zu-adult-etappe2-plan.md` um.

Mehr braucht es nicht: `CLAUDE.md` lädt automatisch, `frontend/CLAUDE.md` sobald unter `frontend/`
gearbeitet wird, und diese Datei trägt das Inventar samt der Regel, was **nicht** mitwandert.
