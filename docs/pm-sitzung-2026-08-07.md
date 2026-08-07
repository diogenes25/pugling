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

## Sprint 2 — Ziel & Umfang

**Sprint-Ziel:** Zwei Lücken, die derselbe Reviewer beim Abnehmen zweier B-48/B-107-Nachbarstories am
2026-08-06 fand, sind jetzt Tore statt Notizen — eine schwächer werdende Zusicherung (DailyBox-Spanne) und
eine Regel, die an Disziplin statt an einem Test hing (Anonym-heißt-gedrosselt).
**Umfang:** B-118 + B-120 — beide `Aufräumen`, beide aus demselben `pugling-reviewer`-Ursprung
(B-48-/B-107-Abnahme vom 2026-08-06), beide reiner Test-Code ohne Produktivverhalten.
**Entwickler-Brief:** Ziel: zwei vom Reviewer vorgeschlagene Test-/Guard-Lücken schließen, nach den
jeweils schon im Review skizzierten Mustern (`TimeSlotsOnFactory` für B-118, der bestehende
Ownership-Guard für B-120). Quelle der Wahrheit: `DailyBoxService.cs`/`PositionProgressService.cs`
(B-118), `ConventionGuardTests.cs`/die fünf realen `[AllowAnonymous]`-Fundstellen (B-120). Guards: keine
neuen Produktiv-Guards für B-118 (reiner Test), B-120 IST der neue Guard. Migration: nein. Vertragsbruch:
nein. Testweg: beide Stories beweisen ihren Test durch gezielte Fehler-Injektion (rot→grün), nicht nur
durch grünen Erstlauf.

## Iteration 2 — umgesetzt

- **B-118**: `DailyBoxRangeTests.cs` + `DailyBoxRangeFactory` (Coins 7-9, Gems 2-4 — bewusst verschieden
  von Produktions-Default UND vom Test-Pin). Zieht 60× direkt über `DailyBoxService.EvaluateAndAwardAsync`
  (DI-Scope, kein HTTP-Umweg), je ein Wegwerf-Plan ohne Positionen pro Tag — hält den Streak-Multiplikator
  dadurch bei jedem Versuch auf 1.0. **Rote Probe** (Fehler injiziert: exklusive statt inklusive
  Obergrenze in `DailyBoxService.cs`): `Assert.Contains() Failure: ... Not found: 9`. Zurückgesetzt: grün.
- **B-120**: `ConventionGuardTests.Anonyme_Actions_Tragen_EnableRateLimiting`, Muster identisch zum
  bestehenden Ownership-Filter-Wächter (Selbstschutz `checkedActions >= 5`, leere, begründete
  Ausnahmeliste). **Rote Probe** (`[EnableRateLimiting("login")]` testweise von `AuthController.LoginAdult`
  entfernt): Test nennt exakt diese Action. Wiederhergestellt: grün.

Volle Suite: **760/760 grün** (758 vor diesem Sprint + 2 neue Tests).

## Runde — Abnahme Sprint 2 (Rollengang: Regression)

Beide Stories sind reiner Test-Code ohne Produktivverhalten — kein Rollengang-Kandidat. Ersatz: die volle
Suite, die beiden gezielten Fehler-Injektions-Belege (stärker als ein bloßer grüner Erstlauf: beide Tests
beweisen, dass sie die Regel wirklich prüfen, nicht nur zufällig grün sind) und `pugling-reviewer` (kein
Blocker; ein 🟢-Nice-to-have zu einer vorbestehenden Buchstaben-Unschärfe in `ConventionGuardTests.cs`,
nicht behoben — kosmetisch, nicht Teil dieser Stories).

**Ergebnis:** B-118 und B-120 sind `abgenommen`.

## Retrospektive — Sprint 2

**Nachschau:** Sprint 1 dieser Sitzung (B-119) ist unmittelbar vorheriger Sprint, frisch reviewt und per
rot→grün-Beleg belegt — kein zweiter Nachhol-Bedarf am selben Tag. Index-Stand nach diesem Sprint: **36
offen, 75 abgenommen, 11 verworfen.**

**Was dieser Sprint gelernt hat:** Für reinen Test-Code (kein Produktivverhalten geändert) ist "der Test
läuft grün" ein schwacher Beleg — er beweist nur, dass nichts kaputt ist, nicht dass der Test die Regel
tatsächlich prüft. Beide Stories dieses Sprints haben das über eine gezielte Fehler-Injektion (den
Defekt, den der Test fangen soll, kurz einbauen, rot sehen, zurücksetzen) stärker belegt als die übliche
`git stash`-Probe an Produktivcode — dieselbe Disziplin wie bei B-121s Rot-Listen-Probe
(2026-08-06), hier zum ersten Mal für zwei reine Test-Additions in einem Sprint angewendet.

**Kein neuer Mechanismus** — die Lehre ist prozedural (wie diese beiden Test-Stories verifiziert wurden),
kein Gate im Produkt könnte das automatisch erzwingen. Als konkrete Konsequenz: beide `## Verlauf`-Einträge
benennen die injizierten Fehler wörtlich, nicht nur "getestet".

## Sprint 3 — Ziel & Umfang

**Sprint-Ziel:** Wer die API nur über ihr Dokument liest (Mensch im Scalar-UI, KI-Creator), sieht 401/403,
den `X-Total-Count`-Kopf und einen typ-spezifischen Namen je Übungs-Endpunkt — nicht mehr nur bei 5 von
323 Operationen, 0 Antwort-Köpfen und 24 generischen Zeilen.
**Umfang:** B-100 allein (Aufräumen, `ausformuliert` → in diesem Sprint gegrillt, geschätzt und gebaut) —
die Bündel-Auflage mit B-56/B-60 aus der 2026-08-04-Arbeitsrunde ist gegenstandslos, beide sind seit
2026-08-06 `abgenommen`.
**Entwickler-Brief:** Ziel: vier Lücken im generierten OpenAPI-Dokument schließen (401/403, `X-Total-
Count`, 24 Summaries, `Cache-Control: no-store`), über dieselbe Transformer-Infrastruktur wie der
bestehende Fehlercode-Schema-Transformer. Quelle der Wahrheit: `Program.cs` (Transformer-Kette),
`ExerciseTypeManifest`/`ExerciseTypeRegistry` (Anzeigenamen), `AuthController.cs` (Login/`me`). Guards:
drei neue Assertions in `ContractDocumentTests`, ein neuer Integrationstest für den Laufzeit-Header.
Migration: nein. Vertragsbruch: nein (additiv — kein bestehender Client verzweigt auf die neuen Felder).
Testweg: gezielte Fehler-Injektion je AC (rot→grün), volle Suite, Frontend-Build gegen das regenerierte
Dokument.

## Iteration 3 — umgesetzt

Drei neue `AddOperationTransformer`-Registrierungen in `Program.cs`: (1) 401 auf jeder nicht-anonymen
Operation, zusätzlich 403 bei `[Authorize(Roles=…)]`; (2) `X-Total-Count` auf jeder 2xx-Antwort einer
`skip`/`take`-paginierten Operation; (3) typ-spezifische Summaries für die 24 Übungs-Create/Update-
Operationen, gelesen aus `ExerciseTypeManifest.Label` (derselben Quelle, die das Frontend für
Anzeigenamen nutzt) — Entscheidung 2 der Story („Notausgang" statt Ursachenforschung, warum
`[EndpointSummary]` bei geerbten generischen Methoden nicht auflöst). Dazu `[ResponseCache(NoStore = true,
…)]` an den drei Login-Actions und `GET auth/me` (AC4). Drei neue Assertions (7–9) in
`ContractDocumentTests` plus ein neuer Integrationstest für den Laufzeit-Header.

**Vier gezielte rot→grün-Belege** (Fehler injiziert, Test schlägt exakt benannt fehl, zurückgesetzt):
Ausnahme aus `Unauthorized401Exceptions` entfernt → `1 operations without 401: POST
/api/v1/creator/teacher-accounts`; `[ResponseCache]` von `GetMe` entfernt → `GET auth/me must send
Cache-Control: no-store.`. **Gemessen statt aus dem Bericht übernommen** (dieselbe Lehre wie B-121 am
2026-08-06): 321/323 Operationen mit 401, 278/323 mit 403, 0/323 ohne Summary, 44 paginierte Operationen
(nicht die im Ist-Stand genannten 31) alle mit Header. `docs/openapi/v1.json` in einem Hunk gewachsen
(5825 Einfügungen/149 Löschungen).

**Review-Fund, sofort behoben (Zähler: 1):** `pugling-reviewer` fand ein 🟡 — das Summary-Matching prüfte
nur „letztes Routensegment == `AuthoringRoute`", nicht ob die Action wirklich von `ExerciseControllerBase<>`
stammt; ein künftiger fremder Endpunkt mit gleichnamigem Segment hätte eine falsche, plausible Beschriftung
bekommen. Gehärtet: zusätzliche Prüfung auf `MethodInfo.DeclaringType` gegen die offene generische Basis.
Erneut verifiziert (weiterhin 0 fehlende Summaries).

Volle Suite: **761/761 grün** (758 vor diesem Sprint + 3 neue Tests). Frontend-Build gegen das
regenerierte Dokument: grün.

## Runde — Abnahme Sprint 3 (Rollengang: Regression)

Reine Dokument-/Header-Vervollständigung, additiv, kein Vertragsbruch — kein UI-Kandidat. Ersatz: die vier
rot→grün-Belege, die volle Suite, `pugling-reviewer` (ein 🟡 gefunden und sofort behoben) und
`npm run build` im Frontend gegen das neu generierte Dokument (der einzige echte Konsument außerhalb der
Suite).

**Ergebnis:** B-100 ist `abgenommen`.

## Retrospektive — Sprint 3

**Nachschau:** Sprint 2 dieser Sitzung (B-118+B-120) ist unmittelbar vorheriger Sprint, frisch reviewt und
per rot→grün-Beleg belegt — kein zweiter Nachhol-Bedarf am selben Tag. Index-Stand nach diesem Sprint:
**35 offen, 76 abgenommen, 11 verworfen.**

**Was dieser Sprint gelernt hat:** Ein zwei Tage alter Ist-Stand-Bericht ist eine Momentaufnahme, kein
aktueller Zustand — zum wiederholten Mal in dieser Woche (B-121 am 2026-08-06 mit den Platzhalter-/Paging-
Zahlen, jetzt B-100 mit 401-Abdeckung und paginierten Operationen). Alle vier Zahlen im Ist-Stand von
2026-08-04 (5/323, 0 Köpfe, 24 ohne Summary, 31 paginiert) waren als *Startzustand* richtig, aber die
Zielgrößen nach dem Fix (321/323, 278/323 mit 403, 44 paginiert) mussten frisch gemessen werden, nicht aus
der Story übernommen.

**Kein neuer Mechanismus für diese Lehre** — sie ist bereits als Praxis etabliert (`docs/backlog/README.md`
„Ausformulieren heißt gegen den Code belegen") und wurde in diesem Sprint einfach wieder angewendet, nicht
neu erfunden. Stattdessen als konkrete Handlung: die drei neuen `ContractDocumentTests`-Assertionen (Punkte
7–9) machen genau diese Zahlen jetzt PERMANENT nachprüfbar, statt sie in einer Story einzufrieren, die beim
nächsten neuen Endpunkt sofort wieder veraltet.

## Stand nach dem Nachtlauf (Sprint 1–3)

Drei Sprints, vier Stories abgenommen (B-119, B-118, B-120, B-100). Kein Abbruchgrund eingetreten
(Review-Fund-Zähler blieb je Sprint bei höchstens 1, weit unter der Fünf-Fehlversuche-Schwelle). Kein
`Wunsch`/`Frage` wurde autonom entschieden — keiner der vier bearbeiteten Kandidaten war einer. Drei
Commits gesetzt, nichts gepusht — das bleibt beim Nutzer. Index-Endstand: **35 offen, 76 abgenommen, 11
verworfen** (Start: 39/72/11).

**Offen für den Nutzer:**

- **B-07/B-47** bleiben außen vor (warten auf die Azure-Reaktivierung, unverändert seit 2026-08-06).
- Keine neuen `Wunsch`/`Frage`-Punkte sind in diesem Lauf entstanden — die vier bearbeiteten Kandidaten
  waren ausnahmslos `Defekt`/`Aufräumen` bereits vor Sitzungsbeginn.
- Kein baubares `Defekt`/`Aufräumen`-Thema mehr auf `geschaetzt`/`ausformuliert` übrig, das nicht extern
  wartet — der Lauf endet hier regulär (kein weiterer Sprint möglich), nicht wegen einer Abbruchbedingung.

