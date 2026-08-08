---
tags: [typ/referenz, bereich/qualitaet, bereich/frontend]
aliases: [Deployment, Azure-Deploy, Single-Host]
---

# Deployment nach Azure – Zielbild und die zwei Fallstricke

Status: **stillgelegt seit 2026-07-30.** Azure ist nicht konfiguriert (es ist **überhaupt kein**
Repo-Secret gesetzt), das Deployment wird später neu gebaut.
[deploy-azure.yml](../.github/workflows/deploy-azure.yml) existiert noch, hat aber keinen automatischen
Trigger mehr – nur `workflow_dispatch`.

Diese Seite ist der Grund, warum die Workflow-Datei nicht gelöscht wurde. Sie hält **das Zielbild** und
**zwei Fallstricke** fest, die zusammen 24 Tage unbemerkten Deploy-Ausfall gekostet haben. Wer das
Deployment neu baut – gleich mit welcher Technik –, fängt hier an und nicht bei null. Der zweite
Fallstrick ist der wichtigere: sein Fehlen bricht nichts, es deployt nur still das Falsche.

## Zielbild: ein Host für SPA und API

Die React-PWA und die .NET-API liegen in **einem** App Service, nicht in zwei. Damit sind `/api/v1/…`
und die Oberfläche **same-origin** – kein CORS, kein zweites Zertifikat, keine geteilte Session-Frage.
Der Preis ist ein Bau-Schritt, der die beiden zusammenlegt:

```text
frontend: npm ci → npm run build   →   frontend/dist/
                                            │  kopieren
                                            ▼
backend:  Pugling.Api/wwwroot/  →  dotnet publish -c Release  →  publish/  →  App Service
```

Die API liefert die SPA also aus ihrem eigenen `wwwroot` aus. Die Laufzeit-Seite davon steht in
[Program.cs](../backend/Pugling.Api/Program.cs): `UseDefaultFiles()`/`UseStaticFiles()` **vor** der
Authentifizierung (statische Assets sind öffentlich) und `MapFallbackToFile("index.html")` am Ende, damit
Direktaufrufe von `/sohn`, `/vater` … im React-Router landen statt im 404. Lokal ist `wwwroot` leer – das
Frontend läuft über Vite auf `:5173` mit `/api`-Proxy, beide Aufrufe passieren also einfach nichts.

**Wer auf zwei Hosts aufteilt**, muss CORS wieder aufmachen: `Cors:Origins` ist konfigurierbar und fällt
auf `http://localhost:5173` zurück – und braucht `WithExposedHeaders("X-Total-Count")`, sonst darf die
Browser-App den Paging-Header nicht lesen.

> **Beim Neubau nicht verlieren:** hochgeladene Bilder liegen bewusst **nicht** in `wwwroot`, sondern in
> einem eigenen Ordner mit eigener `UseStaticFiles`-Middleware (`Media:RootPath`). Grund: dorthin kopiert
> der Deploy das gebaute Frontend – lägen die Bilder dort, löschte **jeder Redeploy die Bilder der
> Familie mit**. Ein Deployment, das diesen Ordner nicht persistent hält (App-Service-Storage, Volume
> oder Blob-Ablage über `IMediaStorage`), verliert sie genauso, nur langsamer.

## Fallstrick 1 (behoben seit B-25) · `npm ci` brauchte `--legacy-peer-deps`

`vite-plugin-pwa@0.21` deklarierte als Peer `vite@^3…^6`, installiert war aber `vite@8`. Jede
**frische** Auflösung brach darum mit `ERESOLVE` ab – und `npm ci` ist immer eine frische Auflösung.
Lokal fiel das nie auf, weil dort ein `node_modules` liegt. **Seit
[B-25](backlog/B-25-vite-pwa-peer-konflikt.md) (`vite-plugin-pwa@^1.3.0`) ist der Konflikt gelöst** –
`npm ci` läuft ohne das Flag, hier wie in `ci.yml`/`e2e.yml`.

**Was es gekostet hat:** der Deploy scheiterte vom **2026-07-05** (Vite-8-Sprung, `2c4eb69`) bis zum
**2026-07-29** an genau dieser Zeile – 24 Tage, unbemerkt, weil niemand das Ergebnis des Workflows las.
Gefunden hat es erst das Frontend-Tor in `ci.yml` (Etappe D1), das absichtlich **denselben Befehl in
derselben Umgebung** ausführt.

**Konsequenz für jede Neufassung:**

- **Dieselbe Node-Version wie `ci.yml`** (`NODE_VERSION: "20"`). Ein Tor, das eine andere Umgebung prüft
  als die, in der gebaut und deployt wird, bewacht nichts. `setup-node` zieht bei `"20"` das neueste
  20.x und erfüllt damit die `engines` von `vite@8` (`^20.19.0 || >=22.12.0`) – wer hier auf ein
  älteres 20.x pinnt, bricht den Build.
- Der Peer-Konflikt selbst ist **vorbestehend und offen**, nicht gelöst (siehe
  [frontend/CLAUDE.md](../frontend/CLAUDE.md) und [codequalitaet-gates-plan.md](codequalitaet-gates-plan.md),
  D1). Fällt er weg, kann das Flag weg – vorher nicht.

## Fallstrick 2 · `workflow_run` checkt den falschen Commit aus

**Das ist der stille.** Ein Deployment, das per `workflow_run` am Test-Workflow hängt, bekommt von
GitHub Actions per Default den **HEAD des Default-Branch** in den Checkout – *nicht* den Commit, dessen
CI-Lauf gerade grün geworden ist. Wer in der Zwischenzeit pusht, deployt damit einen **ungeprüften**
Folge-Commit, und zwar mit grünem Häkchen daneben.

```yaml
- uses: actions/checkout@v4
  with:
    # Ohne dieses `ref` deployt der Default-Branch-HEAD, nicht der geprüfte Commit.
    ref: ${{ github.event.workflow_run.head_sha || github.ref }}
```

Der zugehörige Teil des Tors, ebenfalls nötig und ebenfalls leicht zu vergessen: der Job darf bei einem
**roten oder abgebrochenen** CI-Lauf gar nicht anlaufen. `needs:` geht dafür nicht, weil das Tor in
einem eigenen Workflow steckt – also über die Job-Bedingung:

```yaml
if: ${{ github.event_name == 'workflow_dispatch' || github.event.workflow_run.conclusion == 'success' }}
```

Hintergrund und Entscheidung: [codequalitaet-gates-plan.md](codequalitaet-gates-plan.md), Etappe A2.
Deshalb ist auf `main` auch `cancel-in-progress: false` gesetzt – ein abgebrochener CI-Lauf sähe sonst
wie „nie geprüft" aus.

## Warum es stillgelegt wurde (und nicht gelöscht)

Nachdem CI erstmals dauerhaft grün lief, lief das Deploy zum ersten Mal wirklich an – und scheiterte ab
da nach **jedem** grünen Lauf an `No credentials found`. Belegt an zwei aufeinanderfolgenden Läufen:
CI grün (`30537960757`) → Deploy rot (`30538226570`).

Ein Tor, das *immer* rot ist, erzieht zum Wegsehen und entwertet das echte Rot daneben. Genau das
widerspricht dem Zweck der Tore ([codequalitaet-gates-plan.md](codequalitaet-gates-plan.md),
„mechanische Tore statt Disziplin"). Der Trigger ist darum raus – die Datei bleibt, wegen der zwei
Punkte oben.

## Wieder scharf stellen

**Vor allem anderen: die Datenbank umstellen.** Die Azure-DB stammt aus der alten Migrationskette und wird
vom Historien-Guard abgewiesen – ohne diesen Schritt startet die App nach dem Deploy gar nicht, und die
vier Punkte unten sind dann umsonst. Er steht als eigene Story in
[B-07](backlog/B-07-db-umbau-restetappen.md); die Einzelheiten (zwei App Settings, warum beide in denselben
Vorgang gehören, wie der Rollback aussieht) im Plandokument
[db-struktur-umbau-plan.md](db-struktur-umbau-plan.md), Abschnitt „Betriebsschritt".

1. **Secret setzen:** `AZURE_WEBAPP_PUBLISH_PROFILE` (Portal → App Service → „Bereitstellungscenter"
   bzw. „Veröffentlichungsprofil abrufen"). Prüfen mit `gh secret list` – die Liste war zuletzt **leer**,
   es fehlt also nicht nur dieses Secret.
2. **`AZURE_WEBAPP_NAME`** in [deploy-azure.yml](../.github/workflows/deploy-azure.yml) gegen den echten
   App-Service-Namen prüfen (steht dort als `pugling` mit Anpassungs-Hinweis).
3. **Den `workflow_run`-Block einkommentieren.** Die `if:`-Bedingung am Job ist absichtlich stehen
   geblieben und trägt beide Fälle schon – am Job selbst ist nichts zu ändern.
4. **Betriebssystem des App Service prüfen — sonst sind die Proxy-Kopfzeilen wirkungslos.**
   `Program.cs` liest `X-Forwarded-For` (Ratenbegrenzer-Partition, [B-119](backlog/B-119-ratenbegrenzer-hinter-proxy.md))
   und `X-Forwarded-Proto` (Schema in jeder erzeugten URL, [B-125](backlog/B-125-forwarded-proto-fehlt.md)),
   vertraut den Kopfzeilen aber nur vom **Loopback**-Hop (Default von `ForwardedHeadersOptions`). Das trifft
   **Windows/In-Process** zu, wo IIS/ANCM über Loopback weiterreicht. Auf **Linux/Container** kommt die
   Anfrage mit einer anderen Adresse an, und beide Kopfzeilen werden **still verworfen**: alle Nutzer
   teilen wieder eine Ratenbegrenzer-Partition, und jeder `Location`-Header trägt `http://`. Kein Fehler,
   keine Meldung — nur wirkungslos. `deploy-azure.yml` legt die Ziel-Plattform nicht fest, also ist es
   eine Frage an die Instanz. Bei Linux: `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` setzen **oder**
   `KnownProxies`/`KnownNetworks` in `Program.cs` um das Netz des Front-Ends erweitern.
5. **Gegenprobe, nicht Annahme:** einmal `workflow_dispatch` fahren und das Ergebnis *lesen*. Der
   24-Tage-Ausfall bestand nur, weil niemand das tat. Für Punkt 4 gehört dazu, **eine echte Antwort
   anzusehen**: trägt der `Location`-Header einer `201` ein `https://`, greifen die Kopfzeilen.

Wird das Deployment stattdessen mit anderer Technik neu gebaut, gelten Zielbild und beide Fallstricke
unverändert – nur ihre Schreibweise ändert sich.

## Verwandt

- [codequalitaet-gates-plan.md](codequalitaet-gates-plan.md) – die Tore, Etappe A2 (Deploy-Kopplung) und
  D1 (Frontend-Tor, das Fallstrick 1 gefunden hat)
- [frontend/CLAUDE.md](../frontend/CLAUDE.md) – der Peer-Konflikt hinter Fallstrick 1
- [medien-bilder.md](medien-bilder.md) – der Medien-Ordner, der den Redeploy überleben muss
- [testplan.md](testplan.md) – der E2E-Abschnitt erklärt, warum rote E2E kein Freigabe-Tor sind
