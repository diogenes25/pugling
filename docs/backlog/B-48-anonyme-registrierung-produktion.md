---
tags: [typ/story, status/idee, bereich/auth, bereich/qualitaet]
aliases: [Offene Registrierung]
status: idee
prio: P2
art: Frage
quelle: B-41
unverifiziert: true
---

# B-48 · Anonyme Registrierung ist auch in Produktion offen

Beim Nachsehen für [B-41](B-41-produktions-startup-smoke.md) aufgefallen: `POST api/v1/supervisor/adults`
trägt `[AllowAnonymous]` (`AdultsController.cs:49-51`, „Creates a new father (registration, reachable without
login)"), ebenso das Lehrer-Konto in `TeacherAccountsController`. Das ist **nicht** umgebungsabhängig – auf
einer öffentlich erreichbaren Instanz kann sich also jeder ein Erwachsenen- oder Lehrer-Konto anlegen. Für
die Entwicklung ist das genau richtig (der E2E `vater-von-null.spec.ts` fährt diesen Weg, und eine frische
Produktions-Instanz wäre ohne ihn gar nicht in Betrieb zu nehmen). Ob es in Produktion so bleiben soll, hat
nie jemand entschieden.

**Ungeprüft:** ob es überhaupt ein Problem ist (die App ist eine Familien-Lern-App, keine
Massenplattform – vielleicht ist offene Registrierung genau gewollt); welche Mittel in Frage kämen, falls
nicht (Einladungscode, erste Registrierung offen und danach zu, Rate-Limit wie beim Login, Abschalt-Einstellung
je Umgebung); und ob der Login-Rate-Limiter die Registrierung heute überhaupt erfasst. **Als `Frage`
aufgenommen, nicht als Defekt** – der Prüfauftrag kann mit „so gewollt" enden, und dann ist `verworfen` das
richtige Ergebnis.

## Verlauf

- **2026-07-31** — geerntet beim Grillen der vier Test-Stories (Nebenbefund aus der B-41-Recherche).
