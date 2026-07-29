# Frontend (Vite + React + TS + PWA)

Diese Datei lädt nur, wenn unter `frontend/` gearbeitet wird – die Landkarte der Oberfläche gehört
nicht in jede Backend-Sitzung. Der Rahmen (API-First, Ebenen, Konventionen) steht in der
[CLAUDE.md im Repo-Root](../CLAUDE.md).

```bash
cd frontend && npm install        # einmalig
cd frontend && npm run dev         # http://localhost:5173, /api-Proxy → :5200 (Backend muss laufen)
cd frontend && npm run build       # tsc -b && vite build (Typecheck + Prod-Build)
cd frontend && npm test            # Vitest (src/**/*.test.ts, happy-dom) – Logik unter src/lib/
cd frontend && npm run test:e2e    # Playwright: startet Backend (Temp-DB) + Vite, fährt den Vater→Sohn-Loop
```

**Anmerkungs-Widget** (`src/components/RemarkWidget.tsx`, Alt+A, unten rechts im Vater-Web **und** in der
Sohn-Arcade — dort mit `bottomOffset={96}`, sonst überdeckt es die klebende `.sohn-nav`): erfasst
Beobachtungen beim Testen samt Kontext-Schnappschuss und Fehler-Ringpuffer, zeigt die Log-Id fürs
Einlösen in Claude Code. Nur `import.meta.env.DEV` – im Prod-Bundle ist es wegoptimiert. Für E2E über
`localStorage["pugling.remarks.off"]="1"` abgeschaltet (gesetzt in `playwright.config.ts` via
`use.storageState`); `e2e/anmerkungen.spec.ts` hebt das für sich auf. Plan: [docs/anmerkungen-plan.md](../docs/anmerkungen-plan.md).

**Feld-Erklärungen** (`src/components/InfoHint.tsx` + `src/lib/fieldHelp.ts`): Erklärungsbedürftige
Eingabefelder tragen statt `<label>` ein `<FieldLabel htmlFor=… topic="…">`, das ein „ⓘ" mit Popover
anhängt (Checkbox-Zeilen: `<span className="label-row">` um `<label className="checkline">` + `<InfoHint>`).
**Der Text steht nie am Feld, sondern in `fieldHelp.ts`** – dieselbe Größe wird an mehreren Stellen
eingestellt (der Assistent stellt dieselbe Position ein wie die Plan-Seite), und zwei Formulierungen
desselben Begriffs werden zwei Bedeutungen; `HelpTopic` lässt einen Tippfehler beim Übersetzen auffallen.
Der Hinweis-Knopf heißt `Erklärung zu „<Feldname>"` – **`getByLabel` in Tests darum mit `{ exact: true }`**,
sonst trifft der Teilstring-Vergleich den Knopf statt das Eingabefeld.
E2E: [e2e/feldhilfe.spec.ts](e2e/feldhilfe.spec.ts) prüft Feld → *richtiger* Text, nicht „irgendein Popover".

**Neue Abhängigkeiten bitte mit `--legacy-peer-deps` installieren:** `vite-plugin-pwa@0.21` deklariert
Peer `vite@^3…^6`, installiert ist `vite@8` – jede Neuauflösung bricht sonst mit `ERESOLVE` ab
(vorbestehend, der Build läuft trotzdem). **Das gilt auch für `npm ci`** und damit für jede frische
Maschine: CI (`ci.yml`, Job `frontend`) und Deploy (`deploy-azure.yml`) installieren deshalb mit dem Flag.
Ohne es scheiterte das Deploy von 2026-07-05 bis 2026-07-29 unbemerkt am Install – Hintergrund in
[docs/codequalitaet-gates-plan.md](../docs/codequalitaet-gates-plan.md) (Etappe D1).

Rollen im SPA: `/` Produktseite, `/vater` Web-Admin (inkl. `/vater/wizard` Lehrplan-Assistent,
`/vater/lehrwerke` Buchreihen + Units, `/vater/fachlehrer` Creator-Profile), `/sohn` Arcade-PWA.

**Zwei Konto-Arten** ([docs/lehrer-konto-plan.md](../docs/lehrer-konto-plan.md)): ein **Vater**-Konto trägt
die Rollen Creator + Supervisor, ein **Lehrer**-Konto nur Creator. `session.role` (`Supervisor` | `Creator` |
`Student`) entscheidet, was die Oberfläche zeigt: ein Lehrer sieht nur die Erstellen-Perspektive, keinen
Umschalter, keinen Profil-Link (der zeigt auf einen Supervisor-Endpunkt). Wer eine neue Seite ergänzt, muss
sie einer Perspektive zuordnen oder in `NEUTRAL_PREFIXES` eintragen – sonst leitet die Schranke ein
Lehrer-Konto von ihr weg. Die Rechteprüfung bleibt beim Server; das Frontend zeigt nur keine Türen, die
verschlossen sind.

**Informationsarchitektur des Vater-Webs** ([docs/vater-perspektiven-plan.md](../docs/vater-perspektiven-plan.md),
Vorgänger: [docs/vater-informationsarchitektur-plan.md](../docs/vater-informationsarchitektur-plan.md)):
Das Vater-Web hat **drei Perspektiven** – 👀 Betreuen (`/vater`), 🎯 Zuweisen (`/vater/plaene`),
✏️ Erstellen (`/vater/inhalte`). Sie folgen den Ebenen des Produkts (Supervisor / Brücke / Creator) und
sind **keine Rechte**, sondern eine Antwort auf „woran arbeite ich gerade".

**Die Architektur liegt als Daten in [src/vater/navigation.ts](src/vater/navigation.ts)** – ein neuer
Bereich wird *dort* eingetragen, nicht in die Kopfzeile geschrieben. Die aktive Perspektive kommt aus dem
**Pfad** (`perspectiveOfPath`), nicht aus einem State: sonst öffnete ein Lesezeichen die richtige Seite in
der falschen Perspektive. Eine neue Unterseite, die nicht selbst Nav-Eintrag ist, braucht darum einen
Eintrag in `EXTRA_ROUTES` – sonst springt die Navigation auf „Betreuen".

Drei Regeln, die man beim Ergänzen kennen muss:
**Eine Aktion bekommt keinen Nav-Eintrag** (deshalb stehen „+ Neue Übung" und „+ Neuer Plan" am Bestand,
den sie erweitern); **ein Bereich, der mehrere Übungen trägt, ist ein eigener Ort** (darum liegen Katalog
und Lückentexte neben dem Anlegen statt eingeklappt darin); und **eine Auswahl reist als Query mit**
(`?childId=`, `?subjectId=&chapterId=`) – sonst steht im Zielformular wieder das erste Kind bzw. Fach.
Anlegen und Verwalten sind getrennt: `/vater/exercises` verwaltet, `/vater/exercises/neu` legt an.
API-Client + Types zentral unter [src/lib/](src/lib/).
Ein Vater entsteht **im UI**: `/vater` hat neben „Anmelden" den Modus „Neu registrieren" (gegen das anonyme
`POST supervisor/adults`, meldet direkt an und nennt die neue Id — sie ist der Login-Name); das eigene
Konto liegt unter `/vater/profil`. `/vater/kind/:id` ist der **Kind-Hub** (Stammdaten inkl. PIN, Bild-Freigabe,
gewichtete Interessen) und verlinkt alles Kindbezogene per `?childId=`; darunter
`/vater/kind/:id/lernstand` (plan-übergreifender Lernstand: schwache Wörter + Katalog-Drilldown) und
`/vater/kind/:id/ziele` (Lernziele + Objectives/OKR).
**Alle Übungstypen des Servers sind im UI anlegbar** — Anzeigename und Routen-Segment kommen aus dem
Typ-Manifest (`GET creator/exercise-types`, gelesen über [src/lib/exerciseTypes.ts](src/lib/exerciseTypes.ts)),
**nicht** aus einer Tabelle im Frontend: der Schlüssel weicht von der Route ab (Aufsatz → `essays`), und drei
Kopien liefen zwangsläufig auseinander. Die Formulare je Typ stehen in
[src/vater/exerciseConfig.tsx](src/vater/exerciseConfig.tsx) (Hin- **und** Rückweg, siehe
[wiki/08-erweitern.md](../wiki/08-erweitern.md)); `e2e/uebungstypen.spec.ts` vergleicht das
Typ-Pulldown gegen das Manifest und schlägt fehl, sobald ein Server-Typ kein UI hat.
**Material zurückziehen** (`PATCH creator/exercises/{id}/sharing`, nur Owner): der einzige Weg, eine Übung
aus dem Verkehr zu nehmen – Löschen verweigert eine benutzte Übung, und das zu Recht (laufende Pflichten
dürfen nicht unter dem Kind wegbrechen). Zurückziehen stoppt nur **neue** Zuweisungen. Der Schalter sitzt in
der Verwendungs-Anzeige, nicht in der Zeile: eine seltene Verwaltungs-Entscheidung neben der Auskunft, die
sie begründet. Der Zustand steht als Pille „zurückgezogen" in der Zeile, für jeden sichtbar.

**Wiederkehrende Falle bei Listen mit aufklappbaren Zeilen:** `useAsync` behält `data` über ein `reload`,
setzt aber `loading` erneut. Wer `{loading ? "Lade…" : rows}` schreibt, hängt bei **jeder** Änderung alle
Zeilen aus – aufgeklappte Bereiche und ihr Zustand sind weg. Der Platzhalter darf nur greifen, solange es
noch keine Daten gibt (`loading && data === null`); getroffen hat es schon `VaterKatalog` und `VaterExercises`.

Übungen sind über `/vater/exercises` **bearbeitbar**
(Metadaten per PUT — den geladenen `config`/`suggestedBonus`/`executePublic` mitschicken, sonst löscht der
Vollersatz sie; Vokabelpaare einzeln über `…/vocabulary/{id}/items`, damit die Item-Ids und der Lernstand
des Kindes erhalten bleiben). Das **Anlege**-Formular liegt daneben auf `/vater/exercises/neu`
([src/vater/VaterExerciseCreate.tsx](src/vater/VaterExerciseCreate.tsx)) — wer einen neuen Typ ergänzt,
braucht beide Wege: dort das Formular, im Dialog den Rückweg.
