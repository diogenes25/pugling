# Frontend (Vite + React + TS + PWA)

Lädt nur bei Arbeit unter `frontend/`. Der Rahmen (API-First, Ebenen, Konventionen) steht in der
[CLAUDE.md im Repo-Root](../CLAUDE.md).

```bash
cd frontend            # alle Befehle von dort
npm install            # einmalig; erzeugt dabei auch src/lib/contract.ts
npm run dev            # http://localhost:5173, /api-Proxy → :5200 (Backend muss laufen)
npm run build          # tsc -b && vite build (Typecheck + Prod-Build)
npm test               # Vitest (src/**/*.test.ts(x), happy-dom) – Logik + Komponenten/Hooks
npm run test:e2e       # Playwright: startet Backend (Temp-DB) + Vite, fährt den Vater→Sohn-Loop
```

**Vertragstypen kommen aus dem Dokument, nicht aus der Hand.** `npm run gen:contract` erzeugt
`src/lib/contract.ts` aus `docs/openapi/v1.json` (bei `postinstall`/`predev`/`prebuild`, gitignored);
[src/lib/types.ts](src/lib/types.ts) ist nur noch ein Barrel aus `S["…"]`-Aliasen. Ein neues DTO
wird **nicht hier getippt**: Backend bauen, Testlauf schreibt das Dokument, generieren, eine Alias-Zeile.
Von Hand bleiben elf Typen in [src/lib/uiTypes.ts](src/lib/uiTypes.ts) – **je mit Grund**, sonst gehört es
in den Barrel. Grenze: `http(…, body?: unknown)` prüft **keinen** Objekt-Literal-Rumpf (34 von 86,
[B-24](../docs/backlog/B-24-frontend-unknown-field.md)).

**Anmerkungs-Widget** (`src/components/RemarkWidget.tsx`, Alt+A, nur `import.meta.env.DEV`): erfasst
Beobachtungen beim Testen und zeigt die Log-Id fürs Einlösen in Claude Code. In der Sohn-Arcade mit
`bottomOffset={96}`, sonst verdeckt es die `.sohn-nav`; für E2E per `localStorage["pugling.remarks.off"]="1"`
aus (`anmerkungen.spec.ts` hebt das für sich auf). Plan: [anmerkungen-plan.md](../docs/anmerkungen-plan.md).

**Feld-Erklärungen** (`src/components/InfoHint.tsx` + `src/lib/fieldHelp.ts`): Erklärungsbedürftige
Eingabefelder tragen statt `<label>` ein `<FieldLabel htmlFor=… topic="…">`, das ein „ⓘ" mit Popover anhängt
(Checkbox-Zeile: `<span className="label-row">` um `<label className="checkline">` + `<InfoHint>`).
**Der Text steht nie am Feld, sondern in `fieldHelp.ts`** – dieselbe Größe wird mehrfach eingestellt
(Assistent wie Plan-Seite), und zwei Formulierungen desselben Begriffs werden zwei Bedeutungen; `HelpTopic`
lässt einen Tippfehler beim Übersetzen auffallen. Der Hinweis-Knopf heißt
`Erklärung zu „<Feldname>"` – **`getByLabel` in Tests darum mit `{ exact: true }`**, sonst trifft der
Teilstring-Vergleich den Knopf statt das Eingabefeld. E2E:
[e2e/feldhilfe.spec.ts](e2e/feldhilfe.spec.ts) prüft Feld → *richtiger* Text, nicht „irgendein Popover".

**Komponenten/Hooks testet React Testing Library** (`render`, `renderHook`); der Test liegt beim Geprüften.
Tragend: `setupFiles: ["src/test-setup.ts"]` räumt das DOM zwischen den Fällen ab – ohne `globals: true` tut
RTL das **nicht** selbst (Begründung in [src/test-setup.ts](src/test-setup.ts), bewacht von
`src/test-setup.test.tsx`). **Grenze: kein nachgebauter Bildschirm mit gefälschtem `fetch`** – Bausteine und
Regeln hier, Wege durch die App bei Playwright. Klicks per `fireEvent`, nie `node.click()`.

**Eine Fläche je E2E-Spec** – Neues als eigene Datei, nicht als Block am Ende einer bestehenden: eine Spec
bricht beim ersten Rot ab und nimmt alles Nachfolgende mit ([B-109](../docs/backlog/B-109-full-flow-spec-flackert-bei-frage-3.md)
legte so vier Flächen still). Der Durchstich bleibt **ein** Weg.

**Jeder Knopf, der eine Mutation auslöst, trägt `disabled={busy}`** – auch wenn `useAction` seinen
Wiedereintritt selbst sperrt (`useRef`, synchron): es ist der Wartepunkt der Playwright-Actionability und der
sichtbare Grund, warum eine verworfene zweite Aktion nicht wie „nichts passiert" aussieht. Die Sperre gilt je
**Hook-Instanz**, in Listen mit *geteilter* `ActionState` also listenweit. Als **Regel**, nicht als Zustand:
die Sohn-Arcade (B-49) folgt ihr noch nicht. **Erfolg darf stumm bleiben** (`run` ohne `okText`), wo die
Änderung selbst die Rückmeldung ist – so der Tag-Editor im Vokabel-Store; ein Banner je Chip wäre Lärm.

**Normales `npm install`/`npm ci` genügt** (seit [B-25](../docs/backlog/B-25-vite-pwa-peer-konflikt.md),
`vite-plugin-pwa@^1.3.0`) – der frühere Peer-Konflikt mit `vite@8` ist gelöst, `--legacy-peer-deps` ist
nirgends mehr nötig, auch nicht in CI/Deploy. Der historische Schaden (24 Tage unbemerkt scheiternder
Deploy) steht in [docs/codequalitaet-gates-plan.md](../docs/codequalitaet-gates-plan.md) (D1).

Rollen im SPA: `/` Produktseite, `/vater` Web-Admin, `/sohn` Arcade-PWA.
API-Client unter [src/lib/](src/lib/), kein HTTP daneben.

**Zwei Konto-Arten** ([docs/lehrer-konto-plan.md](../docs/lehrer-konto-plan.md)): ein **Vater**-Konto trägt
die Rollen Creator + Supervisor, ein **Lehrer**-Konto nur Creator. `session.role` (`Supervisor` | `Creator` |
`Student`) entscheidet, was die Oberfläche zeigt: ein Lehrer sieht nur die Erstellen-Perspektive, keinen
Umschalter und keinen Profil-Link. Wer eine neue Seite ergänzt, muss sie einer Perspektive zuordnen oder in
`NEUTRAL_PREFIXES` eintragen – sonst leitet die Schranke ein Lehrer-Konto von ihr weg. Die Rechteprüfung
bleibt beim Server; das Frontend zeigt nur keine Türen, die verschlossen sind.

**Informationsarchitektur des Vater-Webs** ([vater-perspektiven-plan.md](../docs/vater-perspektiven-plan.md)):
**Drei Perspektiven** – 👀 Betreuen (`/vater`), 🎯 Zuweisen (`/vater/plaene`), ✏️ Erstellen
(`/vater/inhalte`). Sie folgen den Ebenen des Produkts und sind **keine Rechte**, sondern eine Antwort auf
„woran arbeite ich gerade".

**Die Architektur liegt als Daten in [src/vater/navigation.ts](src/vater/navigation.ts)** – ein neuer
Bereich wird *dort* eingetragen, nicht in die Kopfzeile geschrieben. Die aktive Perspektive kommt aus dem
**Pfad** (`perspectiveOfPath`), nicht aus einem State: sonst öffnete ein Lesezeichen die richtige Seite in
der falschen Perspektive. Eine neue Unterseite, die nicht selbst Nav-Eintrag ist, braucht darum einen
Eintrag in `EXTRA_ROUTES` – sonst springt die Navigation auf „Betreuen".

Drei Regeln beim Ergänzen: **eine Aktion bekommt keinen Nav-Eintrag** (deshalb stehen „+ Neue Übung" und
„+ Neuer Plan" am Bestand, den sie erweitern); **ein Bereich, der mehrere Übungen trägt, ist ein eigener Ort**
(darum liegen Katalog und Lückentexte neben dem Anlegen); und **eine Auswahl reist als Query mit**
(`?childId=`, `?subjectId=&chapterId=`) – sonst steht im Zielformular wieder das erste Kind bzw. Fach.
Anlegen und Verwalten sind getrennt: `/vater/exercises` verwaltet, `/vater/exercises/neu` legt an.
**Der Einstieg zum Anlegen ist „Unit zuerst"**: `/vater/lehrwerke` trägt je Unit ein „+ Übung"
(`createExerciseHref` reicht `subjectId`/`seriesId`/`seriesUnitId` als Query durch), die Bestandsliste ihr
„+ Neue Übung" mit dem gesetzten Filter. Die Werkstatt hat **bewusst keinen kontextfreien Knopf** mehr –
seit B-106 ist die Unit Pflichtfeld, ein Einstieg mit leerem Kaskadenpicker verlegte die Auswahl nur ins
Formular. Der Knopf hängt **nicht** an `series.isOwn` (fremde Reihen dürfen Übungen tragen), sondern am
Fach: eine Reihe ohne `subjectId` kann keine tragen (`series_without_subject`).
Ein Vater entsteht **im UI**: `/vater` hat neben „Anmelden" den Modus „Neu registrieren" (anonymes
`POST supervisor/adults`; die neue Id ist der Login-Name); das eigene Konto liegt unter
`/vater/profil`. `/vater/kind/:id` ist der **Kind-Hub** (Stammdaten, PIN, Bild, Interessen) und verlinkt alles Kindbezogene per `?childId=`, darunter `…/lernstand` (schwache Wörter +
Katalog-Drilldown) und `…/ziele` (Objectives/OKR mit ihren Etappen).
**Alle Übungstypen des Servers sind im UI anlegbar** — Anzeigename und Routen-Segment kommen aus dem
Typ-Manifest (`GET creator/exercise-types`, gelesen über [src/lib/exerciseTypes.ts](src/lib/exerciseTypes.ts)),
**nicht** aus einer Tabelle im Frontend: der Schlüssel weicht von der Route ab (Aufsatz → `essays`), und drei
Kopien liefen zwangsläufig auseinander. Die Formulare je Typ stehen in
[src/vater/exerciseConfig.tsx](src/vater/exerciseConfig.tsx) (Hin- **und** Rückweg, siehe
[wiki/08-erweitern.md](../wiki/08-erweitern.md)); `e2e/uebungstypen.spec.ts` schlägt fehl, sobald ein
Server-Typ kein UI hat.
**Material zurückziehen** (`PATCH creator/exercises/{id}/sharing`, nur Owner): der einzige Weg, eine Übung aus
dem Verkehr zu nehmen – Löschen verweigert eine benutzte Übung zu Recht (laufende Pflichten dürfen nicht unter
dem Kind wegbrechen). Es stoppt nur **neue** Zuweisungen; der Schalter sitzt neben der Verwendungs-Anzeige.

**Wiederkehrende Falle bei Listen mit aufklappbaren Zeilen:** `useAsync` behält `data` über ein `reload`,
setzt aber `loading` erneut. Wer `{loading ? "Lade…" : rows}` schreibt, hängt bei **jeder** Änderung alle
Zeilen aus – aufgeklappte Bereiche sind weg. Der Platzhalter greift nur ohne Daten
(`loading && data === null`).

Übungen sind über `/vater/exercises` **bearbeitbar**: Metadaten per PUT — den geladenen
`config`/`suggestedBonus`/`executePublic` **mitschicken**, sonst löscht der Vollersatz sie; Vokabelpaare
einzeln über `…/vocabulary/{id}/items`, sonst brechen Item-Ids und Lernstand des Kindes weg. Wer einen neuen
Typ ergänzt, braucht beide Wege: das Formular auf `/vater/exercises/neu`, den Rückweg im Dialog.
