---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/auth]
aliases: [RemoteIpAddress hinter Proxy, ForwardedHeaders fehlt]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer-Befund zur Abnahme von
  [B-48](B-48-anonyme-registrierung-produktion.md) (2026-08-06) — dort nicht mitgenommen, weil B-48s Ziel
  (die fehlende Bremse an der Registrierung) ohne diesen Punkt erfüllt ist und der Befund den Login
  genauso trifft, also älter ist als B-48
unverifiziert: false
---

# B-119 · Hinter einem Reverse Proxy partitioniert der Ratenbegrenzer alle Nutzer in einen Topf

Die Policy `"login"` partitioniert über `http.Connection.RemoteIpAddress` (`Program.cs:258`). Im Repo gibt
es nirgends `UseForwardedHeaders`/`KnownProxies` — hinter dem Front-End eines Azure App Service ist diese
Adresse die des **Proxys**, nicht die des Clients. Dann teilen sich *alle* Nutzer **eine** Partition:
statt 10 Anfragen pro Minute und Person gäbe es 10 pro Minute für die gesamte Instanz, für Login **und**
(seit B-48) Registrierung gemeinsam. Aus einer Bremse gegen Skripte würde eine Bremse gegen die Nutzer.

**Heute kein Schaden:** Das Azure-Deploy ist stillgelegt und Azure ist nicht konfiguriert
(`.github/workflows/deploy-azure.yml`, dazu [B-07](B-07-db-umbau-restetappen.md)). Der Punkt ist damit
nicht dringend, aber er gehört **vor** die erste öffentliche Instanz — und dorthin, wo jemand nachliest,
bevor er deployt (`docs/deployment-azure.md`, Abschnitt „Wieder scharf stellen").

Zu klären beim Ausformulieren: ob `UseForwardedHeaders` mit gesetzten `KnownProxies`/`KnownNetworks` die
richtige Antwort ist (ohne diese Einschränkung wird der Header selbst zum Umgehungsweg — wer ihn fälscht,
sucht sich seine Partition aus), und ob es ohne laufende Azure-Instanz überhaupt verifizierbar ist oder
`wartet_auf` gesetzt werden muss.

## User Story

Als **Betreiber**, der die App hinter einem Azure App Service (Reverse Proxy) veröffentlicht, möchte ich,
dass der Ratenbegrenzer jeden Client an seiner eigenen Adresse erkennt — damit aus einer Bremse gegen
Skripte keine Bremse gegen alle Nutzer der Instanz wird.

## Ist-Stand am Code

Die Policy `"login"` partitioniert über `http.Connection.RemoteIpAddress` (`Program.cs:258`). Im Repo
gibt es nirgends `UseForwardedHeaders`/`KnownProxies` — hinter dem Front-End eines Azure App Service ist
diese Adresse die des **Proxys**, nicht die des Clients. Dann teilen sich *alle* Nutzer **eine**
Partition: statt 10 Anfragen pro Minute und Person gäbe es 10 pro Minute für die gesamte Instanz, für
Login **und** (seit B-48) Registrierung gemeinsam. Heute kein Schaden: das Azure-Deploy ist stillgelegt
und Azure ist nicht konfiguriert (`.github/workflows/deploy-azure.yml`, dazu
[B-07](B-07-db-umbau-restetappen.md)).

## Entscheidungen

1. **Azure App Service ist selbst der Proxy, und er sitzt auf demselben Host wie Kestrel.** Im
   Out-of-Process-Hosting-Modell (der hier verwendete) reicht IIS/ANCM die Anfrage über die
   Loopback-Adresse an Kestrel weiter — `Connection.RemoteIpAddress` ist dort **immer** `127.0.0.1`,
   unabhängig vom echten Client. Genau das deckt sich mit den **Default**-`ForwardedHeadersOptions`:
   `KnownNetworks`/`KnownProxies` vertrauen ohne jede Konfiguration bereits dem Loopback-Host — kein
   `KnownProxies`-Eintrag nötig, und kein Umgehungsweg für einen externen Client, dessen eigene
   `RemoteIpAddress` eben nicht Loopback ist (der Header wird dann schlicht ignoriert).
2. **Verifizierbar ohne laufende Azure-Instanz.** `WebApplicationFactory`s In-Process-`TestServer` meldet
   sich bei jeder Anfrage ebenfalls als Loopback — exakt derselbe Vertrauens-Fall wie App Service. Ein
   Integrationstest mit zwei verschiedenen `X-Forwarded-For`-Werten prüft die reale Partitionierung, ohne
   einen echten Proxy zu brauchen. Kein `wartet_auf`.
3. **Reihenfolge ist die einzige Falle.** `UseForwardedHeaders` muss vor jeder Middleware laufen, die
   `Connection.RemoteIpAddress` liest — hier vor `UseRateLimiter` (und vor allem sonst, das die Adresse
   je lesen könnte, z. B. künftiges Logging). Es steht darum ganz am Anfang der Pipeline, noch vor
   `UseExceptionHandler`.

## Schätzung

`groesse: XS`, `wo: backend`, `migration: nein`, `vertragsbruch: nein` (reine Middleware-Registrierung,
keine neue Route, kein neuer Vertragspunkt). Angriffsplan: `app.UseForwardedHeaders(...)` als erste
Pipeline-Middleware in `Program.cs`, Default-`ForwardedHeadersOptions` (kein `KnownProxies`-Eintrag
nötig, siehe Entscheidung 1). Testweg: ein Integrationstest mit zwei verschiedenen
`X-Forwarded-For`-Werten gegen eine eigene `RateLimitedFactory` (Login-Bremse eingeschaltet), rot gegen
den Vorzustand per `git stash` verifiziert — siehe „Verlauf" für die tatsächliche Umsetzung und die
Messzahlen.

## Akzeptanzkriterien

1. `Program.cs` registriert `app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders =
   ForwardedHeaders.XForwardedFor })` als erste Pipeline-Middleware, vor `UseRateLimiter`.
2. Ein Integrationstest belegt, dass zwei Clients mit unterschiedlichem `X-Forwarded-For` getrennte
   Rate-Limiter-Partitionen bekommen — nicht nur, dass der Header gelesen wird.
3. Keine Konfigurationsänderung nötig (`KnownProxies`/`KnownNetworks` bleiben Default) — die Loopback-
   Vertrauensstellung ist bereits exakt der Azure-App-Service-Fall.

## Verlauf

- **2026-08-07** — ausformuliert, gegrillt und geschätzt (**XS**, `wo: backend`, keine Migration, kein
  Vertragsbruch) in einem Zug: die Recherche (App-Service-Hosting-Modell, Default-Vertrauen) ließ keine
  offene Frage für einen zweiten Schritt übrig.
- **2026-08-07** — umgesetzt: `Program.cs` registriert `UseForwardedHeaders` als erste Middleware (Zeile
  vor `UseExceptionHandler`). Neuer Test `RateLimiterForwardedHeadersTests` +
  `RateLimitedFactory` (die einzige Factory der Suite, die die Login-Bremse eingeschaltet lässt).
  **Rote Probe vor dem Fix** (`git stash` nur auf `Program.cs`, Test unverändert):
  `Assert.Equal() Failure: Expected Unauthorized, Actual TooManyRequests` — der zweite, per Header
  unterschiedene Client wurde von der Partition des ersten mitgesperrt. Nach `git stash pop`: grün.
  Volle Suite: **758/758 grün** (757 vor dieser Story + 1 neuer Test).
- **2026-08-07** — `pugling-reviewer` gefahren: **kein Blocker.** Middleware-Platzierung, Default-
  Vertrauensmodell (nur Loopback, kein Spoofing-Weg für einen nicht-lokalen Aufrufer), Testaussage
  (echte Partitionierung, nicht nur „Header wird gelesen") und `RateLimitedFactory`-Konventionen einzeln
  bestätigt; Coverage-Guard unverändert korrekt (der Test trifft nur 401/429, nie <400).
- **2026-08-07** — Rollengang-Ersatz: kein UI-Kandidat (reine Middleware-Registrierung, keine neue Route,
  kein neuer Vertragspunkt). Ersatz nach `docs/nachtlauf.md`: der gezielte rot→grün-Beleg oben plus die
  volle Suite als Regressionsnetz plus der Reviewer.
- **2026-08-07** — `abgenommen`.
