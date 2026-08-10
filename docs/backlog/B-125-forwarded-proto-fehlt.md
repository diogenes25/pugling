---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/api]
aliases: [XForwardedProto fehlt, Location-Header bleibt http, Scheme hinter TLS-Terminierung]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 3)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-119]
nachgeschaut: 2026-08-09
wartet_auf: ""
---

# B-125 · Die App weiß hinter dem Proxy, wer ruft — aber nicht, dass er über HTTPS ruft

[B-119](B-119-ratenbegrenzer-hinter-proxy.md) hat die App proxy-bewusst gemacht, aber nur zur Hälfte:
`UseForwardedHeaders` liest `X-Forwarded-For` und damit die echte Client-Adresse — `X-Forwarded-Proto`
bleibt unbeachtet. Hinter der TLS-Terminierung eines Azure App Service ist `Request.Scheme` deshalb
weiterhin `http`, und **jede absolute URL, die der Server ausgibt, ist falsch**.

## User Story

Als **Konsument der API** (Frontend, `Pugling.Client`, KI-Agent) möchte ich dem `Location`-Header einer
`201`-Antwort folgen können, ohne dass mich die App von HTTPS auf HTTP zurückschickt.

## Ist-Stand am Code

`Program.cs:514` registriert genau ein Flag:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor });
```

Der Kommentar darüber (`:507-513`) begründet ausschließlich die Ratenbegrenzer-Partition — das Schema
kommt in der ganzen Überlegung nicht vor. Betroffen ist alles, was aus `Request.Scheme` eine absolute
URL baut:

- **Jedes `CreatedAtAction`** — der Vertrag deklariert `201` an 30 Stellen; `POST creator/publishers`
  (`PublishersController.cs:80`) antwortete hinter Azure mit `Location: http://host/api/v1/creator/publishers/7`.
- **Das OpenAPI-Dokument zur Laufzeit** (Scalar-UI): die `servers`-URL entstünde ebenso mit `http://`.

Kein Test sieht das: `RateLimiterForwardedHeadersTests` (aus B-119) prüft ausschließlich die
Partitionierung über `X-Forwarded-For`.

**Heute kein Schaden**, aus demselben Grund wie bei B-119: das Azure-Deploy ist stillgelegt
(`.github/workflows/deploy-azure.yml`, dazu [B-07](B-07-db-umbau-restetappen.md)). Der Punkt gehört
**vor** die erste öffentliche Instanz — genau wie sein Vorgänger.

## Die echte Lücke

Nicht „B-119 war falsch" — die Partitionierung stimmt. Die Lücke ist, dass die Story ihr eigenes Ziel
(„die App verhält sich hinter einem Reverse Proxy richtig") an genau einem Symptom festgemacht hat, dem
Ratenbegrenzer, und die zweite Hälfte derselben Middleware-Entscheidung nicht mitgenommen hat. Wer
`UseForwardedHeaders` registriert, beantwortet die Frage „was hat der Proxy uns verschwiegen?" — und die
Antwort hat zwei Teile, nicht einen.

Bemerkenswert für die Messung: der Nachschau-Sweep vom **2026-08-07** hat B-119 ausdrücklich als
„hält, kein Fund" geprüft — er hat nachgerechnet, dass die Middleware noch an der richtigen Stelle
steht, nicht ob sie das Richtige tut. Siehe [pm-sitzung-2026-08-07.md](../pm-sitzung-2026-08-07.md).

## Entscheidungen

1. **`XForwardedProto` dazu, `XForwardedHost` nicht.** Begründung: das Schema ist die Angabe, die heute
   nachweislich falsche Ausgaben erzeugt; der weitergereichte **Host** dagegen ist ein bekannter
   Angriffsweg (Host-Header-Poisoning) und löst kein beobachtetes Problem — die App bindet keine Links
   an einen konfigurierten Hostnamen. **Kosten:** ein zusätzliches Flag in derselben Zeile; das
   Vertrauensmodell ändert sich nicht (weiterhin nur Loopback, siehe B-119, Entscheidung 1).
2. **Der Test prüft die Wirkung, nicht die Registrierung.** Ein Test, der `ForwardedHeaders`-Optionen
   aus dem DI-Container liest, wäre eine Tautologie. Geprüft wird stattdessen, dass eine Anfrage mit
   `X-Forwarded-Proto: https` einen `Location`-Header mit `https://` erzeugt. Begründung: dasselbe
   Prinzip wie B-119, Entscheidung 2 („prüft die reale Partitionierung, nicht nur, dass der Header
   gelesen wird"). **Kosten:** der Test braucht einen Endpunkt mit `CreatedAtAction`, also etwas
   Aufsetz-Arbeit statt eines Einzeilers.

## Akzeptanzkriterien

1. `Program.cs` registriert `ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto`.
2. Ein Integrationstest belegt, dass eine `201`-Antwort auf eine Anfrage mit `X-Forwarded-Proto: https`
   einen `Location`-Header mit `https://` trägt — und **nicht** nur, dass der Header gelesen wird.
3. Die Partitionierung aus B-119 bleibt unberührt (`RateLimiterForwardedHeadersTests` weiter grün).
4. Der Test war **vor** der Änderung rot (Abnahmeform `art: Defekt`).

## Schätzung

**Größe: XS** (Anker B-119 selbst: eine Middleware-Zeile plus ein Integrationstest). `wo: backend`,
`migration: nein`, `vertragsbruch: nein` — keine neue Route, kein neues Feld, kein bestehender Client
verzweigt auf das Schema eines `Location`-Headers.

**Risiko:** keins am Vertrauensmodell — `KnownNetworks`/`KnownProxies` bleiben auf dem Default (nur
Loopback), also gilt für `X-Forwarded-Proto` exakt derselbe Schutz, den B-119 für `X-Forwarded-For`
belegt hat: ein nicht-lokaler Aufrufer bekommt seinen Header schlicht ignoriert.

**Angriffsplan:** ein Flag in `Program.cs:514`, ein Testfall neben den bestehenden
`RateLimiterForwardedHeadersTests`. **Testweg:** `POST` auf einen `CreatedAtAction`-Endpunkt mit
gesetztem `X-Forwarded-Proto`, `Location`-Header prüfen; rot gegen den Vorzustand.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`, Fundstelle selbst am
  Code nachgeprüft (`Program.cs:514`, nur `XForwardedFor`). `entgangen_bei: [B-119]`: die Zeile ist in
  jener Story entstanden und war `abgenommen` — und der Nachschau-Sweep desselben Tages hat sie
  passieren lassen.
- **2026-08-07** — gegrillt, geschätzt und gebaut (autonom, `art: Defekt`). **Rote Probe zuerst**, neue
  `ForwardedProtoTests.cs` gegen den Vorzustand: **1 Failed / 1 Passed** —
  `Assert.StartsWith() Failure … String: "http://localhost/api/v1/creator/publishers/2", Expected start:
  "https://"`. Der zweite Test (ohne Header bleibt es `http://`) war schon grün und bleibt es: er hält
  fest, dass der Fix das Schema **weiterreicht** und nicht hart auf HTTPS stellt — sonst bräche die
  lokale Entwicklung über Klartext-HTTP.
  Umgesetzt: `ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto` in `Program.cs`, mit
  einem Kommentarabsatz, der auch das ausdrückliche **Nicht**-Ziel `XForwardedHost` benennt (Entscheidung
  1). Danach 2/2 grün, `RateLimiterForwardedHeadersTests` aus B-119 unverändert grün (AK3), volle Suite
  **767/767**.
- **2026-08-07** — `pugling-reviewer` gefahren: **kein Blocker**; Middleware-Platzierung, Testaussage
  („prüft die Wirkung, nicht die Registrierung") und das Spoofing-Modell einzeln bestätigt — es verzweigt
  nirgends etwas auf `Request.Scheme`/`IsHttps` (kein `UseHttpsRedirection`, kein `UseHsts`, keine
  Secure-Cookie-Entscheidung), ein gefälschter Header ändert also einzig das Schema erzeugter URLs.
  **Ein Fund über den Diff hinaus, und er ist wichtig (Befund 5):** der Fix kann in Azure **wirkungslos**
  sein. `KnownNetworks`/`KnownProxies` stehen auf dem Default, vertrauen also nur dem **Loopback**-Hop —
  das trifft Windows/In-Process, **nicht** Linux/Container-App-Service. Dort käme die Anfrage von einer
  anderen Adresse, und `X-Forwarded-Proto` **und** `X-Forwarded-For` würden still verworfen: beide Fixes
  (dieser und B-119) wären lautlos nichts. Der Risiko-Absatz oben beschrieb die *Sicherheit* richtig und
  übersah die *Wirksamkeit*.
  Kein Code geändert — die Zielplattform ist eine Eigenschaft der Instanz, nicht des Repos. Stattdessen
  als **Punkt 4 in die Checkliste** von [deployment-azure.md](../deployment-azure.md) („Wieder scharf
  stellen") aufgenommen, samt Gegenprobe: den `Location`-Header einer echten `201` ansehen. Damit steht
  die Bedingung dort, wo jemand nachliest, **bevor** er deployt — dieselbe Ablage, die B-119 für sich
  selbst gewählt hat.
- **2026-08-07** — **abgenommen.** Volle Suite **772/772 grün**, `RateLimiterForwardedHeadersTests`
  (B-119) unverändert grün. **Rollengang-Ersatz:** kein UI-Kandidat (reine Middleware-Registrierung,
  keine neue Route, kein neuer Vertragspunkt); Ersatz ist der rot→grün-Beleg über eine echte
  `201`-Antwort, die volle Suite und der Reviewer. Der Wirksamkeits-Vorbehalt aus Befund 5 ist **keine
  offene Arbeit an dieser Story**, sondern eine Betriebsbedingung — sie steht in der Deploy-Checkliste
  und wird dort geprüft, wenn die Instanz wieder scharf gestellt wird (B-07).
- **2026-08-09** — nachgeschaut (Nachtlauf, Retro zu Sprint 1), **kein Befund**. **Prüfpunkt:** der
  klassische Fallstrick bei `UseForwardedHeaders` — ob `KnownNetworks`/`KnownProxies` geleert werden
  müssen, damit die Header hinter Azure überhaupt gelesen werden. Nachgerechnet an
  `Program.cs:508-522`: der ANCM-Hop **ist** loopback, die Vorgabe ist damit richtig und die Begründung
  steht am Code. `XForwardedHost` ist ausdrücklich ausgelassen, mit Grund. Nichts zu ändern.
