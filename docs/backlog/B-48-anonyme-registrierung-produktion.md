---
tags: [typ/story, status/geschaetzt, bereich/auth, bereich/qualitaet]
aliases: [Offene Registrierung]
status: geschaetzt
prio: P2
art: Frage
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-41
---

# B-48 · Anonyme Registrierung ist auch in Produktion offen

Beim Nachsehen für [B-41](B-41-produktions-startup-smoke.md) aufgefallen: `POST api/v1/supervisor/adults`
trägt `[AllowAnonymous]` (`AdultsController.cs:49-51`, „Creates a new father (registration, reachable without
login)"), ebenso das Lehrer-Konto in `TeacherAccountsController`. Die Recherche zeigt: das ist **kein**
pauschaler Fehlbefund, aber auch keine reine Nicht-Aufgabe — der anonyme Zugang selbst ist gewollt, ihm
fehlt aber ausgerechnet der mechanische Schutz, den der Login schon hat.

## User Story

Als Betreiber einer öffentlich erreichbaren Pugling-Instanz möchte ich, dass die anonymen
Registrierungs-Endpunkte densel­ben Bot-Schutz tragen wie der Login, damit ein Skript nicht unbegrenzt
Konten anlegen oder E-Mail-Adressen für echte Nutzer blockieren kann.

## Ist-Stand am Code

- **Zwei anonym erreichbare Schreib-Endpunkte:** `POST api/v1/supervisor/adults`
  (`AdultsController.cs:49-54`, „registration, reachable without login") und
  `POST api/v1/creator/teacher-accounts` (`TeacherAccountsController.cs:40-44`, „like the father
  registration"). Beide legen einen `Adult` an, hashen die PIN und spiegeln sie sofort auf ein Login-Konto
  (`accounts.EnsureForAdultAsync`/`EnsureForTeacherAsync`).
- **Kein Umgebungsunterschied:** Es gibt genau eine `appsettings.json`, kein
  `appsettings.Production.json` (`Glob backend/Pugling.Api/appsettings*.json`). `builder.Environment.
  IsDevelopment()` steuert an drei Stellen etwas (`Program.cs:200` Remarks-Sichtbarkeit, `:262` Jwt-Key-
  Pflicht, `:495` Seed), aber nirgends die beiden Registrierungs-Endpunkte. Der Befund aus der Notiz „das
  ist nicht umgebungsabhängig" ist damit bestätigt.
- **Login hat einen Ratenbegrenzer, Registrierung nicht:** `Program.cs:246-257` registriert die Policy
  `"login"` (Fixed-Window, 10 Anfragen/Minute je IP, abschaltbar über `RateLimiting:LoginEnabled`). Alle
  drei Login-Endpunkte tragen `[EnableRateLimiting("login")]` (`AuthController.cs:48-50, 65-66, 91-92`).
  Die beiden Registrierungs-Endpunkte tragen dieses Attribut **nicht** — das Ausgangs-„Ungeprüft" („erfasst
  der Login-Rate-Limiter die Registrierung überhaupt?") ist damit klar beantwortet: **nein, gar keine
  Begrenzung.**
- **Kein CAPTCHA irgendwo im Code** (`grep -i captcha` über `backend/` — kein Treffer).
- **Keine E-Mail-Verifizierung, keine Mail-Infrastruktur:** kein Treffer für `SmtpClient`/`EmailService`/
  `MailKit` o. Ä. im Backend. `Email` ist optional (`CreateAdultDto.Email` nullable) und dient nur der
  Eindeutigkeit (`EmailTakenAsync`, `AdultsController.cs:104-108` → `ApiErrors.DuplicateEmail`), nicht
  einem Passwort-Reset-Fluss — es gibt keinen.
- **Der anonyme Weg ist architektonisch gewollt, nicht nur ein Bootstrap-Notnagel:** `docs/grundprinzip.md:
  116` (`SupervisorLink`, mehrere Supervisor je Kind) und die geteilte Übungs-Bibliothek/das
  Lehrer-Konto-Szenario („Herr Schmidt", `backend/Pugling.Api/CLAUDE.md` → Creator-Profile) legen eine
  Instanz an, auf der mehrere Familien und Lehrer unabhängig registrieren — nicht ausschließlich eine
  Ein-Familien-Installation. Dazu passt der E2E `frontend/e2e/vater-von-null.spec.ts`, der genau diesen
  anonymen Weg fährt und ohne ihn nicht funktionieren würde.

## Die echte Lücke

Nicht „anonyme Registrierung ist grundsätzlich falsch" — der Zugang selbst ist für Bootstrap, E2E und das
Mehr-Familien-/Lehrer-Design bewusst richtig, und CAPTCHA oder E-Mail-Verifizierung wären für eine
Familien-Lern-App ohne Massenandrang unverhältnismäßiger Aufwand (keine Infrastruktur vorhanden, müsste
komplett neu gebaut werden). Die tatsächliche Lücke ist enger: **die Registrierung hat, anders als der
Login, überhaupt keine mechanische Bremse.** Ein Skript kann unbegrenzt Konten anlegen (Ressourcenverbrauch)
oder gezielt E-Mail-Adressen „besetzen" (der `DuplicateEmail`-Check blockiert dann die echte Person) — ohne
dass irgendetwas greift, obwohl das günstige, bereits vorhandene Rate-Limit-Muster direkt nebenan liegt.

## Entscheidungen

1. **`[AllowAnonymous]` bleibt unverändert an beiden Endpunkten.** Begründung: Bootstrap einer frischen
   Instanz, der E2E `vater-von-null.spec.ts` und das Mehr-Familien-/Lehrer-Design (`SupervisorLink`,
   geteilte Bibliothek) brauchen den anonymen Zugang; ein Login-Zwang davor wäre ein Henne-Ei-Problem.
   Kosten: keine — es ändert sich nichts an diesem Verhalten.
2. **Beide Registrierungs-Endpunkte bekommen dieselbe Ratenbegrenzung wie der Login:**
   `[EnableRateLimiting("login")]` zusätzlich zu `[AllowAnonymous]` auf `AdultsController.Create`
   (`AdultsController.cs:50`) und `TeacherAccountsController.Create` (`TeacherAccountsController.cs:41`).
   Begründung: schließt die einzige tatsächlich fehlende Absicherung, mit dem bereits etablierten,
   getesteten Muster (`Program.cs:246-257`) — keine neue Policy, keine neue Konfiguration. Kosten: gering
   (zwei Attribute); eine IP, die in derselben Minute mehr als 10 echte Konten anlegt (z. B. eine Schule
   hinter einem NAT-Gateway), träfe dieselbe Grenze wie beim Login — bewusst in Kauf genommen, weil sie
   dort schon gilt und in der Praxis nicht die 1-2 Registrierungen einer einzelnen Familie/eines einzelnen
   Lehrers betrifft.
3. **Kein CAPTCHA.** Begründung: keine Infrastruktur im Code vorhanden (kein externer Dienst integriert),
   und der Nutzungsrahmen (Familien-App, keine Massenplattform) rechtfertigt den Aufwand einer
   Neuintegration nicht. Kosten: ein entschlossener Bot, der die 10/Minute-Grenze respektiert (langsam
   genug bleibt), wird weiterhin nicht gestoppt — als Restrisiko akzeptiert, weil dafür eigens ein externer
   Dienst angebunden werden müsste.
4. **Keine E-Mail-Verifizierung.** Begründung: Es existiert keine Mail-Versand-Infrastruktur (kein SMTP/
   `EmailService`), `Email` ist rein ein optionales Eindeutigkeits-Merkmal ohne Passwort-Reset-Fluss dahinter
   — eine Verifizierung einzuführen wäre ein eigenständiges, deutlich größeres Vorhaben (Mailversand, Token,
   UI) und gehört, falls je gebraucht, in eine eigene Story. Kosten: eine unverifizierte, ggf. falsche
   E-Mail-Adresse bleibt möglich — heute schon der Fall, keine Verschlechterung.
5. **Kein Einladungscode / kein „erste Registrierung offen, danach zu".** Begründung: Beide Varianten
   widersprechen dem architektonisch gewollten Mehr-Familien-/Lehrer-Betrieb auf einer Instanz (Entscheidung
   1) und würden den Bootstrap-Fluss verkomplizieren, ohne mehr zu leisten als die Ratenbegrenzung aus
   Entscheidung 2. Kosten: keine — es wird nichts davon gebaut.
6. **Kein neuer Umgebungsschalter (Feature-Flag je Environment).** Begründung: Die Policy `"login"` ist
   bereits global über `RateLimiting:LoginEnabled` schaltbar (u. a. für die Test-Factory); ein zweiter,
   registrierungsspezifischer Schalter wäre eine zweite Stelle zum Vergessen für dieselbe Entscheidung.
   Kosten: keine zusätzliche Konfiguration.

## Akzeptanzkriterien

1. `POST api/v1/supervisor/adults` trägt zusätzlich zu `[AllowAnonymous]` das Attribut
   `[EnableRateLimiting("login")]`.
2. `POST api/v1/creator/teacher-accounts` trägt dasselbe Attribut.
3. Ein neuer Test in `SecurityHardeningTests.cs`, analog zu `Login_UeberschreitetRateLimit_Liefert429`,
   belegt: mit `RateLimiting:LoginEnabled=true` liefert der 11. anonyme Registrierungsversuch derselben IP
   innerhalb einer Minute `429`.
4. `frontend/e2e/vater-von-null.spec.ts` bleibt unverändert grün (ein einzelner Registrierungsaufruf bleibt
   deutlich unter der 10/Minute-Grenze).

## Schätzung

**Größe: S** — zwei vorhandene Attribute ergänzen, ein neuer Test nach bestehendem Muster
(`SecurityHardeningTests.cs:41-57`), keine neue Infrastruktur, keine neue Konfiguration.

- **`wo: backend`**, **`migration: nein`** (kein Schema betroffen), **`vertragsbruch: nein`** (kein
  Contracts-Feld ändert sich, nur ein Verhaltens-Attribut).
- **Risiken:** Eine IP mit mehr als 10 echten Registrierungen pro Minute (Schule/Firma hinter NAT) bekäme
  vorübergehend `429` — dieselbe Grenze, die für Logins schon gilt, und in der Praxis unwahrscheinlich für
  einzelne Familien-/Lehrer-Registrierungen.
- **Angriffsplan:** rein Backend — zwei Attribute ergänzen (`AdultsController.cs`,
  `TeacherAccountsController.cs`), danach der Test.
- **Testweg:** neuer Fall in `SecurityHardeningTests.cs` (Integrationstest gegen die echte Rate-Limiter-
  Pipeline, Muster wie `Login_UeberschreitetRateLimit_Liefert429`); bestehender E2E
  `vater-von-null.spec.ts` als Regressionscheck, dass ein einzelner Registrierungsaufruf weiterhin
  durchgeht.

## Verlauf

- **2026-07-31** — geerntet beim Grillen der vier Test-Stories (Nebenbefund aus der B-41-Recherche).
- **2026-08-03** — ausformuliert: Recherche belegt, der anonyme Zugang ist architektonisch gewollt
  (Bootstrap, E2E, Mehr-Familien-/Lehrer-Design), trägt aber — anders als der Login — keinerlei
  Ratenbegrenzung; kein CAPTCHA, keine E-Mail-Verifizierung, keine Mail-Infrastruktur im Code, kein
  Produktions-/Entwicklungs-Unterschied (belegt gegen `AdultsController.cs`, `TeacherAccountsController.cs`,
  `AuthController.cs`, `Program.cs`, `docs/grundprinzip.md`) (autonom geprüft, Nutzerauftrag 2026-08-04).
- **2026-08-03** — gegrillt: sechs Entscheidungen getroffen (anonymer Zugang bleibt, Registrierung bekommt
  dieselbe `"login"`-Ratenbegrenzung wie der Login, kein CAPTCHA, keine E-Mail-Verifizierung, kein
  Einladungscode, kein zusätzlicher Umgebungsschalter) (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschätzt: Größe S, `wo: backend`, keine Migration, kein Vertragsbruch — zwei
  Attribute plus ein Test nach bestehendem Muster (autonom getroffen, Nutzerauftrag 2026-08-04).
