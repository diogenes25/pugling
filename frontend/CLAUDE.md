# Frontend (Vite + React + TS + PWA)

Diese Datei lädt nur, wenn unter `frontend/` gearbeitet wird – die Landkarte der Oberfläche gehört
nicht in jede Backend-Sitzung. Der Rahmen (API-First, Ebenen, Konventionen) steht in der
[CLAUDE.md im Repo-Root](../CLAUDE.md).

```bash
cd frontend && npm install        # einmalig
cd frontend && npm run dev         # http://localhost:5173, /api-Proxy → :5200 (Backend muss laufen)
cd frontend && npm run build       # tsc -b && vite build (Typecheck + Prod-Build)
cd frontend && npm run test:e2e    # Playwright: startet Backend (Temp-DB) + Vite, fährt den Vater→Sohn-Loop
```

Rollen im SPA: `/` Produktseite, `/vater` Web-Admin (inkl. `/vater/wizard` Lehrplan-Assistent,
`/vater/lehrwerke` Buchreihen + Units, `/vater/fachlehrer` Creator-Profile), `/sohn` Arcade-PWA.
API-Client + Types zentral unter [src/lib/](src/lib/).
Ein Vater entsteht **im UI**: `/vater` hat neben „Anmelden" den Modus „Neu registrieren" (gegen das anonyme
`POST supervisor/fathers`, meldet direkt an und nennt die neue Vater-Id — sie ist der Login-Name); das eigene
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
Übungen sind über `/vater/exercises` **bearbeitbar**
(Metadaten per PUT — den geladenen `config`/`suggestedBonus`/`executePublic` mitschicken, sonst löscht der
Vollersatz sie; Vokabelpaare einzeln über `…/vocabulary/{id}/items`, damit die Item-Ids und der Lernstand
des Kindes erhalten bleiben).
