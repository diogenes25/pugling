---
tags: [typ/protokoll, bereich/pm]
aliases: [PM-Sitzung 2026-08-07, Nachtlauf Aufräumen/Defekt-Backlog]
---

# PM-Sitzung: Nachtlauf — Aufräumen/Defekt-Backlog abarbeiten

**Datum:** 2026-08-07  ·  **Moderation:** PM
**Teilnehmer:** Entwickler (autonom, `art: Defekt`/`Aufräumen` freigegeben — `Wunsch`/`Frage` bleiben Dialog)
**Ziel:** die erreichbaren `Defekt`/`Aufräumen`-Stories (B-100, B-118, B-119, B-120 — alle aus Reviews/
Arbeitsrunden vom 2026-08-04/-06 geerbt) bis zur Abnahme bringen, ohne Rückfrage außer an den drei
dokumentierten Stellen (`docs/nachtlauf.md`).

**Freigaben für diesen Lauf** (Nutzerauftrag, wörtlich aus `docs/nachtlauf.md` übernommen):

1. Autonomes Grillen nur für `art: Defekt`/`Aufräumen`; jeder `Wunsch`/jede `Frage` wird notiert, nicht
   entschieden.
2. Nicht erreichbare Reviewer-Agenten → ausdrücklich beschrifteter Selbst-Check, Story bleibt `in-arbeit`
   mit `wartet_auf`, nie auf `abgenommen` gestampft.
3. Mehrere Sprints erlaubt; Retro schlägt ihren Mechanismus in jedem Sprint nur vor; Review-Funde werden
   sofort behoben oder im selben Sprint als `Defekt` bearbeitet, > 5 je Sprint beendet den ganzen Lauf.
   XL-Stories werden geteilt, nicht gebaut.
4. „Kein Befund" nur mit benanntem Prüfpunkt.
5. Jede rote Probe nennt ihre Zahl.
6/7. Chrome-Extension/`web-design-guidelines` — voraussichtlich entfallen: alle vier Kandidaten dieses
   Laufs sind Backend/Doku/Test ohne sichtbare UI-Änderung (wird je Sprint neu geprüft).

Push bleibt beim Nutzer. Commits setzt der Lauf selbst.

## Vorlauf — Bestand gesichtet

`docs/backlog/README.md`-Index frisch gezogen (`.claude/scripts/backlog-index.sh`): **39 offen, 72
abgenommen, 11 verworfen.** Vor dem ersten Schritt trug der Arbeitsbaum ein fertiges, aber uncommittetes
Paket aus der vorherigen Sitzung (B-63-Nachtrag: Verlags-Admin-UI, geteilter Slug-Helfer) — verifiziert
(Backend 757/757, Frontend-Build grün) und committet (`a43ad3f`), damit der Abendstand eindeutig ist.

Nach `art`-Filter (Freigabe 1) bleiben erreichbar:

- **B-07** (Aufräumen, `geschaetzt`) und **B-47** (Aufräumen, `geschaetzt`) — beide außen vor, warten auf
  die Azure-Reaktivierung (eigene Entscheidung/Deploy-Trigger, siehe 2026-08-06-Protokoll).
- **B-100** (Aufräumen, `ausformuliert`) — Vertragsdokument unterdeklariert (401/`X-Total-Count`/24
  Summaries). War an B-56/B-60 gebündelt gedacht — **beide sind seit 2026-08-06 `abgenommen`**, die
  Bündelauflage entfällt damit, B-100 kann solo laufen.
- **B-118** (Aufräumen, `idee`) — DailyBox-Ziehungsspanne ohne Zusicherung.
- **B-119** (Defekt, `idee`) — Ratenbegrenzer hinter Reverse Proxy.
- **B-120** (Aufräumen, `idee`) — Wächter „Anonym heißt gedrosselt" fehlt.

Drei rote Fäden, drei Sprints:

- **Sprint 1:** B-119 allein (Defekt, echte Verhaltensänderung — `ForwardedHeaders`-Middleware).
- **Sprint 2:** B-118 + B-120 (beide Aufräumen, beide aus demselben `pugling-reviewer`-Ursprung — ein
  Test-/Guard-Rigor-Nachschlag zu zwei am 2026-08-06 abgenommenen Stories).
- **Sprint 3:** B-100 allein (Aufräumen, Vertragsdokument — eigener großer Diff durch die 900-KB-
  Regenerierung, anderes Risiko als die beiden Test-Sprints).

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel:** Skaliert die App eines Tages hinter Azure App Service, drosselt die Login-Bremse jeden
Angreifer wirklich einzeln — nicht die gesamte Nutzerschaft gemeinsam, weil alle hinter derselben
Loopback-Adresse verschwinden.
**Umfang:** B-119 allein (Defekt, `idee` → in diesem Sprint ausformuliert, gegrillt, geschätzt und
gebaut — die Recherche ließ keine offene Frage für einen Zwischenschritt).
**Entwickler-Brief:** Ziel: `ForwardedHeadersMiddleware` so registrieren, dass die „login"-Policy im
Azure-App-Service-Fall (Out-of-Process-Hosting, Kestrel sieht nur den Loopback-Hop von IIS/ANCM) nach der
echten Client-Adresse partitioniert. Quelle der Wahrheit: `Program.cs:258` (Partition-Key
`Connection.RemoteIpAddress`) und das dokumentierte Hosting-Modell. Guards: keine neuen (reine
Middleware-Registrierung). Migration: nein. Vertragsbruch: nein. Testweg: neuer Integrationstest, rot vor
dem Fix (`git stash` auf `Program.cs`), grün danach, Zahl im `## Verlauf` von B-119.

## Iteration 1 — umgesetzt

`Program.cs` registriert `app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders =
ForwardedHeaders.XForwardedFor })` als erste Pipeline-Middleware (vor `UseExceptionHandler`, weit vor
`UseRateLimiter`). Default-`KnownNetworks`/`KnownProxies` genügen: sie vertrauen bereits Loopback, und das
ist exakt die Adresse, die Kestrel im Azure-Out-of-Process-Modell von IIS/ANCM sieht — keine zusätzliche
Konfiguration nötig, kein Spoofing-Weg für einen Aufrufer, dessen eigene Adresse nicht Loopback ist.

Neuer Test `RateLimiterForwardedHeadersTests` + `RateLimitedFactory` (einzige Factory der Suite mit
eingeschalteter Login-Bremse). **Rote Probe vor dem Fix** (`git stash` nur auf `Program.cs`):
`Assert.Equal() Failure: Expected Unauthorized, Actual TooManyRequests` — der zweite, per
`X-Forwarded-For` unterschiedene Client wurde von der Partition des ersten mitgesperrt. Nach
`git stash pop`: grün. Volle Suite: **758/758 grün** (757 vor dieser Story + 1 neuer Test).

## Runde — Abnahme Sprint 1 (Rollengang: Regression)

Kein UI-Kandidat — reine Middleware-Registrierung, keine neue Route, kein neuer Vertragspunkt. Ersatz nach
`docs/nachtlauf.md`: der gezielte rot→grün-Beleg oben, die volle Suite als Regressionsnetz, und
`pugling-reviewer` (kein Blocker — Platzhierung, Vertrauensmodell, Testaussage und Factory-Konventionen
einzeln bestätigt, Details im `## Verlauf` von B-119).

**Ergebnis:** B-119 ist `abgenommen`.

## Retrospektive — Sprint 1

**Nachschau:** der vorige Sprint war Sprint 6 der 2026-08-06-Sitzung (B-63) — dort bereits durch
Reviewer + E2E-Ersatz belegt, kein zweiter Nachhol-Bedarf. Index-Stand nach diesem Sprint: **38 offen, 73
abgenommen, 11 verworfen.**

**Was dieser Sprint gelernt hat:** Ein Defekt, dessen Story selbst schon "vielleicht `wartet_auf`" als
offene Frage nennt, muss das nicht bleiben — die Recherche (Azure App Service ist im Out-of-Process-Modell
selbst der Loopback-Nachbar von Kestrel) hat die vermutete Verifikationslücke aufgelöst, bevor sie zu einer
wurde. Der `TestServer` verhält sich zufällig identisch zum Produktionsfall (auch er meldet sich als
Loopback), was den Integrationstest ohne jede Sonderkonstruktion ermöglicht hat.

**Kein neuer Mechanismus** — die Lehre ist einmalig für diese Story (eine Recherchefrage, die sich beim
genauen Hinsehen auflöste), kein Gate im Produkt könnte das fangen.

## Vorlauf zu Sprint 2 — Ausformulieren/Schätzen

