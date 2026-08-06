---
tags: [typ/story, status/idee, bereich/backend, bereich/auth]
aliases: [RemoteIpAddress hinter Proxy, ForwardedHeaders fehlt]
status: idee
prio: P2
art: Defekt
quelle: pugling-reviewer-Befund zur Abnahme von
  [B-48](B-48-anonyme-registrierung-produktion.md) (2026-08-06) — dort nicht mitgenommen, weil B-48s Ziel
  (die fehlende Bremse an der Registrierung) ohne diesen Punkt erfüllt ist und der Befund den Login
  genauso trifft, also älter ist als B-48
unverifiziert: true
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
