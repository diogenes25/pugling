---
tags: [typ/story, status/geschaetzt, bereich/shop, rolle/supervisor]
aliases: [Offene Einlösungen aller Kinder, Einlöse-Endpunkt]
status: geschaetzt
prio: P2
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-662
grund: ""
ersetzt_durch: []
---

# B-747 · Offene Einlöse-Anfragen aller Kinder in einem Aufruf

Ein Endpunkt `GET supervisor/shop/activations?status=Pending` liefert die Anfragen aller Kinder des Vaters, ausstellergebunden. Die
Zahl der offenen Anfragen für den Navigationspunkt „Shop" kommt aus demselben Aufruf (`X-Total-Count`). Abgespalten aus
[B-662](B-662-vater-sicht-arbeit-tagesziel-kaeufe.md) (Entscheidung 2); die Oberfläche folgt in
[B-749](B-749-vater-startseite-entruempeln-und-einloesen.md).

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-662.*

## User Story

Als *Vater* möchte ich alle offenen Einlöse-Wünsche meiner Kinder ohne Kindwahl sehen, damit ich sie am Handy in einem Blick
freigebe.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`; Pfade relativ zu `backend/Pugling.Api/`.

- Es gibt nur den Abruf je Kind: `GET supervisor/children/{childId}/shop/activations` (`Controllers/Supervisor/ShopController.cs:457-474`)
  mit `ChildOwnershipFilter`, Filter `r.ChildId == childId && r.SupervisorId == fid`, Sortierung neueste zuerst mit Id als
  Tiebreaker und Seitenkopf `X-Total-Count`.
- Freigeben und Ablehnen laufen weiter je Kind (`:479`, `:493`) und bleiben unverändert.
- Der Controller liegt unter `supervisor/shop` (`:17`); ein Pfad `activations` ohne `articles/` kollidiert mit keiner vorhandenen Route
  (Routenliste `:62-339`).
- `ActivationRequestDto` trägt `ChildId`, aber keinen Kindnamen (`Pugling.Contracts/Supervisor/ShopDtos.cs:41`).
- Wallet: der Endpunkt liest nur; die Wallet-Invariante (jeder abbuchende Pfad bumpt `ConcurrencyStamp`) berührt ihn nicht.

## Entscheidungen

1. **Ein Endpunkt über alle Kinder, ausstellergebunden (`SupervisorId`) und auf eigene Kinder begrenzt; das Antwortfeld ist additiv**
   (B-662 Entscheidung 2). Kosten: ein Endpunkt mehr mit eigener Eigentumsprüfung und Test; der Abruf je Kind bleibt. Verworfen: N
   Einzelabrufe im Frontend. Schließt aus: ein Einlösen, das erst nach der Wahl eines Kindes sichtbar wird.
2. **Kein neues Feld im Vertrag; der Kindname kommt im Frontend aus der Kinderliste.** Kosten: Das Frontend verbindet zwei Listen.
   Verworfen: `ChildName` im DTO (hätte den Vertrag geändert).

## Akzeptanzkriterien

1. Der Endpunkt liefert nur Anfragen des eigenen Supervisors (`SupervisorId`) und nur eigener Kinder. *Rot, wenn* der Filter fehlt
   (ein fremder Vater sähe Anfragen).
2. Mit `status=Pending` fehlen genehmigte und abgelehnte Anfragen; ohne Filter erscheinen alle. *Rot, wenn* der Statusfilter
   ignoriert wird.
3. Der Seitenkopf trägt die Gesamtzahl der gefilterten Anfragen. *Rot, wenn* `X-Total-Count` fehlt oder nur die Seitengröße meldet.
4. Nach `approve` sinkt die Zahl der offenen Anfragen um eins. *Rot, wenn* ein freigegebener Eintrag weiter als `Pending` zählt.
5. Die Reihenfolge ist stabil unter `approve` und `reject` (neueste zuerst, Id als Tiebreaker). *Rot, wenn* die Sortierung sich
   unter der Aktion verschiebt (B-113-Regel).
6. Zwei Väter mit je einem Kind sehen nie die Anfragen des anderen (Multi-Supervisor-Fall: Anfrage ausgestellt von Vater A, Kind
   auch bei Vater B). *Rot, wenn* B die Anfrage von A sieht.

## Schätzung

**Größe: S**, wie „`childId` aus dem Test-Pfad ziehen" (B-01): eine Lese-Action nach dem Muster der vorhandenen, ohne Schreibpfad.

- **`wo: backend`**: die Oberfläche folgt in B-749.
- **`migration: nein`**: gelesen wird `ActivationRequests` mit vorhandenen Spalten und Indizes.
- **`vertragsbruch: nein`**: neue Action, bestehende DTOs unverändert.

### Risiken

- **Eigentumsprüfung ohne `ChildOwnershipFilter`:** Der Filter hängt an einer `childId` im Pfad; hier gibt es keine. Die Begrenzung
  läuft über `SupervisorId` und die Kinder des Supervisors (`SupervisorLinks`); beides muss im Test gegen einen Fremdzugriff stehen.
  Der Fall „mehrere Supervisor je Kind" (`MultiSupervisorTests`) ist die Lücke, die man übersieht.
- **Index:** Eine Abfrage nach `SupervisorId` und Status ohne `ChildId`: ob ein Index dafür vorhanden ist, steht nicht fest
  (Messung vor dem Bau; bei wenigen hundert Zeilen unkritisch, eine Migration wäre ein Fund und würde `migration` ändern).
- **Seitengröße der Zählung:** Das Frontend ruft für den Zähler `take=1` ab; der Wert kommt aus `X-Total-Count`.

### Angriffsplan

1. Action `ActivationsAll` in `ShopController` (`HttpGet("activations")`, `Roles.Supervisor`), Filter nach `SupervisorId`, optional
   `status`, Paging und Sortierung wie die Kind-Variante.
2. Abdeckungseintrag; OpenAPI und `frontend/src/lib/contract.ts` neu erzeugen.
3. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Testklasse `ShopActivationsOverviewTests` (Integrationstest, `PuglingWebAppFactory`, Aufbau der Anfragen wie `ShopFlowTests`
  `:1394-1404`) für AK 1 bis 5; AK 6 mit dem Aufbau aus `MultiSupervisorTests`.
- `EndpointCoverageGuard` und `ContractDocumentTests` laufen mit.
- Vor der Abnahme: Agent `pugling-reviewer`; `/smoke-test` mit zwei Kindern und einer offenen Anfrage.

## Verlauf

- **2026-11-20** — angelegt aus [B-662](B-662-vater-sicht-arbeit-tagesziel-kaeufe.md) (geteilt); `geschaetzt`: S, backend, keine
  Migration, kein Vertragsbruch (Vorbereitungslauf, Sonnet).
