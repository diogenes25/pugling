---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: [vite-plugin-pwa Peer-Konflikt]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: memory/codequalitaet-gates.md
---

# B-25 · Peer-Konflikt `vite-plugin-pwa` ↔ `vite@8` lösen

Umgangen, nicht gelöst: die Installation läuft nur mit `--legacy-peer-deps`. Das ist eine tickende
Abhängigkeit — beim nächsten frischen Clone oder CI-Runner ohne den Schalter bricht es.

## User Story

Als Entwickler möchte ich `npm install`/`npm ci` im Frontend **ohne** `--legacy-peer-deps` laufen lassen
können, damit eine frische Maschine oder ein neuer CI-Runner nicht an einem vergessenen Flag scheitert.

## Ist-Stand am Code

- `frontend/package.json:33-34` — `vite: "^8.1.3"`, `vite-plugin-pwa: "^0.21.1"`. Der Lockfile-Eintrag
  löst tatsächlich `vite-plugin-pwa@0.21.2` auf (`frontend/package-lock.json:6758-6776`).
- `vite-plugin-pwa@0.21.2` deklariert `peerDependencies.vite: "^3.1.0 || ^4.0.0 || ^5.0.0 || ^6.0.0"`
  (geprüft per `npm view vite-plugin-pwa@0.21.2 peerDependencies`) — `vite@8` ist darin **nicht** enthalten,
  der Konflikt ist real, kein Phantom.
- Der Workaround ist an **vier** Stellen verankert, nicht nur einer:
  `.github/workflows/ci.yml:187` (`npm ci --legacy-peer-deps`), `.github/workflows/deploy-azure.yml:71`,
  `.github/workflows/e2e.yml:78` und die Doku-Zeile `frontend/CLAUDE.md:51-56`, die neue Abhängigkeiten
  ausdrücklich mit dem Flag installieren lässt.
- `frontend/vitest.config.ts:10` hält zusätzlich als Kommentar fest, warum die Vitest-Konfiguration von
  `vite.config.ts` getrennt ist: „`vite-plugin-pwa` verträgt sich ohnehin nicht mit vite 8".
- `frontend/vite.config.ts:8-22` nutzt `VitePWA(…)` nur minimal: `registerType: "autoUpdate"` plus ein
  reines `manifest`-Objekt (Name, Icons, Farben) — keine Workbox-Sonderkonfiguration, kein
  `@vite-pwa/assets-generator`-Aufruf, der von einer neuen Major-Version betroffen sein könnte.

## Die echte Lücke

Ungeprüft war, ob inzwischen eine `vite-plugin-pwa`-Version existiert, die `vite@8` als Peer akzeptiert —
**ja, seit `1.3.0`**: `npm view vite-plugin-pwa@1.3.0 peerDependencies` liefert
`vite: "^3.1.0 || ^4.0.0 || ^5.0.0 || ^6.0.0 || ^7.0.0 || ^8.0.0"`; das GitHub-Release von `1.3.0` nennt
explizit „Add vite 8 peer dependency support" (Commit `276af62`).

Der Sprung von `0.21.2` (installiert) auf `1.3.0` (aktuell) überspringt einen Major (`1.0.0`). Das
GitHub-Release von `1.0.0` listet als **einzigen** Breaking Change das Anheben des *optionalen* Peers
`@vite-pwa/assets-generator` auf `1.0.0` — dieser Peer steht in `frontend/package.json` gar nicht als
Abhängigkeit, das Projekt nutzt den Assets-Generator nicht. `1.0.1`/`1.0.3` fügten nur Vite-7-Support hinzu,
`1.1.0`/`1.2.0` reine Bugfixes. Damit ist der Versionssprung für dieses minimale `VitePWA(…)`-Setup
folgenlos.

**Verifiziert per Testlauf** (Kopie von `frontend/package.json` in einem Scratch-Verzeichnis,
`vite-plugin-pwa` auf `^1.3.0` angehoben, `npm install --package-lock-only` ausgeführt): Auflösung läuft
**ohne** `ERESOLVE`-Fehler und ohne das Flag durch.

Die echte Lücke ist also schmaler als vermutet: kein Ersatz-Paket, kein API-Umbau am `VitePWA`-Aufruf nötig
— nur ein Versions-Bump plus das Entfernen des Workarounds an seinen vier Fundstellen.

## Offene Punkte

1. ~~Existiert eine `vite-plugin-pwa`-Version, die `vite@8` als Peer akzeptiert?~~ → siehe Entscheidung 1.
2. ~~Reicht ein reiner Versions-Bump, oder zieht der Major-Sprung 0.21→1.x einen Config-Umbau nach sich?~~
   → siehe Entscheidung 1.
3. ~~Auf welche Version genau pinnen?~~ → siehe Entscheidung 2.
4. ~~Gehört das Entfernen des Workarounds in den drei Workflow-Dateien und in `frontend/CLAUDE.md` zum
   Umfang dieser Story, oder bleibt das eine Folge-Story?~~ → siehe Entscheidung 3.

## Entscheidungen

1. **`vite-plugin-pwa` auf `^1.3.0` anheben**, kein API-Umbau am `VitePWA(…)`-Aufruf. Begründung: `1.3.0`
   ist die erste (und bislang einzige) Version mit `vite@8` im Peer-Range; der einzige Breaking Change auf
   dem Weg dorthin (`1.0.0`, Anheben des optionalen `@vite-pwa/assets-generator`-Peers) betrifft eine nicht
   genutzte Abhängigkeit. Kosten: keine — der Testlauf gegen die echte Registry-Version bestätigt eine
   konfliktfreie Auflösung.
2. **`^1.3.0` als Caret-Range, nicht exakt gepinnt.** Begründung: konsistent mit jeder anderen
   `devDependency` in `frontend/package.json` (alle tragen `^`); ein Exakt-Pin wäre eine Sonderregel ohne
   erkennbaren Grund. Kosten: künftige Patch-/Minor-Releases von `vite-plugin-pwa` fließen ohne erneuten
   Review ein — wie bei jeder anderen Abhängigkeit auch.
3. **Der Workaround wird an allen vier Fundstellen entfernt**, nicht nur im `package.json`: die drei
   `npm ci --legacy-peer-deps`-Zeilen in `ci.yml`/`deploy-azure.yml`/`e2e.yml` werden zu `npm ci`, die
   Warnung in `frontend/CLAUDE.md:51-56` und der Kommentar in `vitest.config.ts:10` entfallen bzw. werden
   auf „kein Konflikt mehr" korrigiert. Begründung: Ein gelöster Konflikt, dessen Workaround-Dokumentation
   stehen bleibt, ist eine Lüge mit Haltbarkeitsdatum — die nächste Person liest „installiere mit
   `--legacy-peer-deps`" und tut es unnötig. Kosten: vier zusätzliche Datei-Änderungen neben dem
   eigentlichen Bump, aber jede ist eine Zeile.

## Akzeptanzkriterien

1. `frontend/package.json` trägt `vite-plugin-pwa: "^1.3.0"` (oder neuer), `frontend/package-lock.json` ist
   neu erzeugt.
2. `npm install` im `frontend`-Verzeichnis läuft **ohne** `--legacy-peer-deps` und **ohne**
   `ERESOLVE`-Warnung durch.
3. `npm ci --legacy-peer-deps` ist in `.github/workflows/ci.yml`, `.github/workflows/deploy-azure.yml` und
   `.github/workflows/e2e.yml` durch `npm ci` ersetzt.
4. `frontend/CLAUDE.md` und `frontend/vitest.config.ts` nennen den Peer-Konflikt nicht mehr als aktuellen
   Zustand.
5. `npm run build` (Typecheck + Vite-Build) läuft unverändert grün; `dist/` enthält weiterhin
   `manifest.webmanifest` und den generierten Service Worker — das PWA-Verhalten selbst ändert sich nicht
   (`art: Aufräumen`).
6. CI (`ci.yml`) und der E2E-Nachtlauf (`e2e.yml`) bleiben grün mit dem entfernten Flag.

## Schätzung

**Größe: XS** — ein Versions-Bump in einer Zeile plus das Streichen eines acht Zeichen langen Flags an vier
belegten Fundstellen; kein API-Umbau, keine Schemaänderung, kein Vertragsbruch.

- **`migration: nein`** — reine Frontend-Abhängigkeit, keine EF-Schemaänderung betroffen.
- **`vertragsbruch: nein`** — `Pugling.Contracts` ist nicht berührt, `VitePWA(…)`-Aufruf bleibt
  API-kompatibel zwischen `0.21` und `1.3.0` für die hier genutzten Optionen.
- **Risiko:** gering. Die einzige denkbare Überraschung wäre ein Verhaltensunterschied im generierten
  Service Worker durch die `workbox-build`/`workbox-window`-Anhebung (`^7.3.0` → `^7.4.1`, Minor-Bump,
  keine Breaking Changes laut deren Changelogs) — abgefangen durch Akzeptanzkriterium 5 (Build-Artefakt
  prüfen, nicht nur Build-Exitcode).
- **Angriffsplan:** `vite-plugin-pwa` in `frontend/package.json` anheben → `npm install` (erzeugt neuen
  Lockfile) → `npm run build` lokal prüfen (Artefakt-Check) → die drei Workflow-Dateien und
  `frontend/CLAUDE.md`/`vitest.config.ts` nachziehen → Push, CI und E2E-Nachtlauf beobachten.
- **Testweg:** kein dedizierter Integrationstest nötig (reine Tooling-Frage) — Testweg ist der **clean
  `npm install`-Lauf** (Exitcode 0, kein `--legacy-peer-deps`, kein `ERESOLVE`), anschließend
  `npm run build` und die bestehende CI-Pipeline (`ci.yml`, Job „frontend") sowie der E2E-Nachtlauf
  (`e2e.yml`) als Regressionsnetz — beide laufen ohnehin bei jedem Push.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: gegen den echten Code geprüft (`frontend/package.json`,
  `package-lock.json`, `npm view`-Abfragen gegen die Registry, vier Workaround-Fundstellen belegt) und per
  Testlauf (Scratch-Kopie, Bump auf `vite-plugin-pwa@^1.3.0`, `npm install --package-lock-only`) bestätigt:
  der Konflikt ist seit `1.3.0` gelöst, kein API-Umbau nötig.
- **2026-08-03** — gegrillt: alle vier offenen Punkte in Entscheidungen überführt (autonom getroffen,
  Nutzerauftrag 2026-08-04) — Bump auf `^1.3.0`, Caret-Range, und Entfernen des Workarounds an allen vier
  Fundstellen inklusive Doku.
- **2026-08-03** — geschätzt: Größe XS, `wo: frontend`, `migration: nein`, `vertragsbruch: nein`, Risiken,
  Angriffsplan und Testweg festgelegt (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-06** — gebaut (Nachtlauf 2, Sprint 3 „CI/Deploy-Tooling"): `vite-plugin-pwa` auf `^1.3.0`
  angehoben, `npm install` **ohne** `--legacy-peer-deps` lief clean (kein `ERESOLVE`), Lockfile neu
  erzeugt. `npm run build` → `PWA v1.3.0`, die vorher bei **jedem** Build dieser Nacht aufgetauchte
  Warnung „vite-plugin-pwa:build … assigns to bundle variable" ist verschwunden (Nebeneffekt des Bumps,
  nicht Teil der AK). Das Flag aus `ci.yml`, `deploy-azure.yml`, `e2e.yml` entfernt samt der
  Begründungs-Kommentare; `frontend/CLAUDE.md`, `frontend/vitest.config.ts` und zusätzlich
  `docs/deployment-azure.md` (direkt von einem entfernten Workflow-Kommentar verwiesen, daher mitgezogen)
  nennen den Konflikt nicht mehr als aktuellen Zustand. `npm test -- --run` → **153/153 grün**.
  `frontend-reviewer` lief gegen den Diff.
