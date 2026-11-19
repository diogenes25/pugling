---
tags: [typ/story, status/abgenommen, bereich/shop, rolle/supervisor]
aliases: [Einlösen ohne Handy, Vater löst ein]
status: abgenommen
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: "Vater am 2026-11-18, Erfahrung aus dem echten Betrieb (Kind will einlösen, hat das Handy nicht dabei)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-665 · Der Vater löst direkt aus dem Inventar des Kindes ein

Einlösen ist heute zweistufig: Das Kind kauft im Familien-Shop, stellt aus seinem Inventar einen
Einlöse-Antrag, und der Vater gibt ihn frei. Mehrfach wollte das Kind sofort einlösen, hatte aber das
Handy nicht dabei. Der Vater soll dann selbst aus dem Inventar des Kindes einlösen können – „alles klar,
ich nehme mir aus deinem Inventar jetzt 30 Minuten Spielzeit" –, ohne den Umweg über den Antrag des
Kindes: sofort genehmigt, das Inventar sinkt, und das Kind sieht hinterher, dass eingelöst wurde.
Verwandt: [B-662](B-662-vater-sicht-arbeit-tagesziel-kaeufe.md) (Vater-Sicht mit „was ist einzulösen").

**Vorgabe des Vaters (2026-11-18, nachgereicht):** Das Einlösen durch den Vater soll die **Ausnahme** bleiben, aber
möglich sein. Der normale Weg bleibt der Antrag des Kindes. Für die Grill-Runde heißt das etwa: ein unauffälliger
Zweit-Knopf mit Bestätigung, offene Anträge des Kindes stehen weiter oben und vorn.

## User Story

Als **Supervisor (Vater)** möchte ich eine Menge aus dem Inventar meines Kindes **selbst einlösen**
(z. B. 30 Minuten Spielzeit), **damit** das Kind seine Belohnung bekommt, auch wenn es sein Handy gerade
nicht dabei hat – und das Kind hinterher in seiner Anfrage-Liste sieht, dass und was eingelöst wurde.

## Ist-Stand am Code

**Backend – der Weg ist heute nur vom Kind aus zu öffnen:**

- Den Antrag stellt ausschließlich das Kind: `POST student/me/shop/inventory/{articleId}/activate`
  ([MeController.cs:347](../../backend/Pugling.Api/Controllers/Student/MeController.cs)), mit
  `User.ChildId()`-Pflicht (`:357`) – ein Supervisor-Token bekommt hier `403`.
- `ShopService.RequestActivationAsync`
  ([ShopService.cs:290](../../backend/Pugling.Api/Services/Shared/ShopService.cs)) prüft Menge > 0, dass
  der Artikel einem betreuenden Supervisor gehört (`:299–302`) und dass das Inventar die Menge deckt
  (`:304–307`); er legt eine `ActivationRequest` mit Status `Pending` an und stempelt den Aussteller als
  `SupervisorId` (`:313`). **Das Inventar sinkt dabei noch nicht.**
- Erst `ApproveActivationAsync` (`:335`) zieht ab: erneute Deckungsprüfung (`:345–347`), `inv.Quantity -=`
  und Bump des `ConcurrencyStamp` am Inventar (`:349–350`), Status `Approved`, `ClosedAt` (`:353–354`).
  Die Freigabe ist **ausstellergebunden** (`LoadPendingActivationAsync`, `:409–411`).
- Der Supervisor hat für ein Kind: Inventar lesen
  ([ShopController.cs:316](../../backend/Pugling.Api/Controllers/Supervisor/ShopController.cs), nur eigene
  Artikel über den Snapshot `i.SupervisorId == fid`, `:329`), Anfragen lesen (`:385`), freigeben (`:407`),
  ablehnen (`:421`). **Einen Endpunkt, der aus dem Inventar heraus einlöst, gibt es nicht.**
- `ActivationRequest` ([GamificationEntities.cs:294](../../backend/Pugling.Api/Models/GamificationEntities.cs))
  trägt **kein Feld, wer den Antrag gestellt hat** – heute ist es implizit immer das Kind.
- Inventarzeilen zu gelöschten Artikeln haben `ShopArticleId = null`
  ([ShopController.cs:325–327](../../backend/Pugling.Api/Controllers/Supervisor/ShopController.cs)); das
  Kind kann sie nicht einlösen, weil der Antrag über die Artikel-Id läuft (Frontend fängt das ab,
  [SohnShop.tsx:152–157](../../frontend/src/sohn/SohnShop.tsx)).

**Frontend:**

- Vater-Web: `ChildShopView`
  ([VaterShop.tsx:371](../../frontend/src/vater/VaterShop.tsx)) zeigt „Offene Aktivierungsanfragen" mit
  Freigeben/Ablehnen (`:411–435`), darunter das **Inventar als reine Lese-Tabelle** Artikel/Menge
  (`:437–455`) – ohne Aktion je Zeile.
- Sohn-App: Einlösen im Tab „Sachen" über `requestActivation`
  ([SohnShop.tsx:151](../../frontend/src/sohn/SohnShop.tsx)); die Anfragen-Liste (`RequestsTab`, `:314`)
  zeigt Status Pending/Approved/Rejected (`:22–23`).

## Die echte Lücke

Nicht die Freigabe fehlt, sondern der **Anstoß von Vater-Seite**: Der Antrag kann nur mit dem Token des
Kindes entstehen. Gebraucht wird ein Supervisor-Endpunkt (und ein Knopf je Inventarzeile im Vater-Web),
der in einem Schritt Antrag + Freigabe erledigt – mit denselben Prüfungen (Ausstellerbindung,
Inventardeckung, Concurrency-Stamp), ohne die Logik ein zweites Mal zu schreiben.

## Akzeptanzkriterien

1. Ein Supervisor kann für ein betreutes Kind eine Menge eines **eigenen** Artikels aus dessen Inventar
   einlösen; das Ergebnis ist eine `ActivationRequest` mit Status `Approved`, und das Inventar sinkt sofort
   um die Menge.
2. Deckt das Inventar die Menge nicht → `insufficient_inventory`, nichts wird angelegt, Inventar unverändert.
3. Artikel eines **anderen** Supervisors (Mutter/Oma) → `404`, wie bei der Freigabe heute.
4. Fremdes Kind → `404` über den `ChildOwnershipFilter`; Student-Token → `403`.
5. Menge ≤ 0 → `400` (`validation_error`).
6. Das Kind sieht den Vorgang in seiner Anfrage-Liste (`student/me/shop/activations`) als eingelöst,
   unterscheidbar davon, dass es selbst angefragt hat (siehe offener Punkt 2).
7. Vater-Web: je Inventarzeile ein Knopf „Einlösen" mit Mengeneingabe und Bestätigung; nach Erfolg laden
   Inventar und Anfrage-Liste neu (`useAction` + `StatusBanner`).
8. Integrationstest in `ShopFlowTests` je Kriterium 1–5, jeder Fall mit benannter Änderung, die ihn rot macht.

## Offene Punkte

Alle sechs am 2026-11-19 vom Vater entschieden (Empfehlungen übernommen) → Entscheidungen 1–6.

1. **Form des Endpunkts.** Vorschlag:
   `POST supervisor/children/{childId}/shop/inventory/{articleId}/redeem` mit `{ quantity }`, Antwort
   `ActivationRequestDto`. Alternative: Supervisor ruft den Student-Endpunkt mit – widerspricht der
   Ebenen-Taxonomie. **Empfehlung:** eigener Supervisor-Endpunkt; im `ShopService` eine Methode, die
   `RequestActivationAsync`-Prüfungen und die Abbuchung aus `ApproveActivationAsync` teilt (ein
   `SaveChanges`, kein Pending-Zwischenstand).
2. **Wer hat eingelöst – festhalten?** Ohne neues Feld sieht das Kind einen `Approved`-Eintrag, den es nie
   angefragt hat. Optionen: (a) neues Feld `RequestedBy` (Kind/Supervisor) an `ActivationRequest` ⇒
   Migration mit Default „Kind" für Bestandszeilen, Vertragsfeld in beiden DTOs; (b) nichts speichern.
   **Empfehlung:** (a) – genau dieses Misstrauen („das habe ich nie eingelöst!") will die Kind-Ansicht
   vermeiden, und die Kosten sind eine kleine, anhängende Migration.
3. **Soll das Kind zustimmen müssen?** Der Vater handelt ja *auf Zuruf* des Kindes. **Empfehlung:** nein –
   der Vater ist hier zugleich der Freigebende; eine Zustimmung bräuchte wieder das Handy.
4. **Inventar zu gelöschten Artikeln** (`ShopArticleId = null`) – soll der Vater die einlösen können (das
   Kind kann es heute nicht)? **Empfehlung:** zurückstellen; der Endpunkt läuft wie beim Kind über die
   Artikel-Id. Falls gewünscht, eigener Folgepunkt.
5. **Offene Kind-Anträge auf denselben Artikel.** Löst der Vater ein, kann ein schon offener Antrag des
   Kindes danach nicht mehr gedeckt sein. Heute fängt die Freigabe das ab (`insufficient_inventory`, Antrag
   bleibt offen). **Empfehlung:** so lassen – kein automatisches Verrechnen; der Vater lehnt den alten Antrag
   ggf. ab.
6. **Menge im UI.** Einheiten je Artikel (z. B. Minuten). **Empfehlung:** Eingabe mit Vorgabe = eine
   Kauf-Einheit bzw. der gesamte Bestand, Obergrenze = Bestand.

## Entscheidungen

1. **Eigener Supervisor-Endpunkt** `POST supervisor/children/{childId}/shop/inventory/{articleId}/redeem` mit
   `{ quantity }`, Antwort `ActivationRequestDto`. Im `ShopService` **eine** Methode, die die Prüfungen von
   `RequestActivationAsync` (Ausstellerbindung, Deckung) und die Abbuchung von `ApproveActivationAsync`
   (Inventar sinkt, Stamp am Inventar) in einem `SaveChanges` vereint – kein `Pending`-Zwischenstand.
2. **Wer eingelöst hat, wird gespeichert:** neues Feld `RequestedBy` (`Child`/`Supervisor`) an
   `ActivationRequest`, als String wie jedes Enum; die Migration setzt Bestandszeilen per `UPDATE` auf `Child`
   (kein DB-Default, Tor G9). Additives Vertragsfeld in `ActivationRequestDto` und `MyActivationResponse`.
3. **Das Kind muss nicht zustimmen** – der Vater handelt auf Zuruf und ist zugleich der Freigebende.
4. **Inventar zu gelöschten Artikeln:** zurückgestellt; der Endpunkt läuft wie beim Kind über die Artikel-Id.
5. **Offene Kind-Anträge bleiben stehen**, kein Verrechnen; die Freigabe fängt eine fehlende Deckung schon heute
   mit `insufficient_inventory` ab.
6. **Menge im UI:** Vorgabe der ganze Bestand, Obergrenze der Bestand; als **Zweit-Knopf mit Bestätigung** unter
   den offenen Anträgen (Vorgabe des Vaters: die Ausnahme, nicht der Normalweg).

## Schätzung

**Größe: M.** Eine Service-Methode aus vorhandenen Bausteinen, ein Endpunkt, eine anhängende Migration mit einem
`UPDATE`, ein additives Vertragsfeld, ein Knopf im Vater-Web und eine Kennzeichnung in der Sohn-App.

- **`wo: beides`**, Backend zuerst.
- **`migration: ja`**: Spalte `ActivationRequests.RequestedBy` (String), Bestand → `Child`.
- **`vertragsbruch: nein`**: nur additiv (`RequestedBy` in zwei Antwort-DTOs, ein neues Request-DTO
  `RedeemInventoryRequest`).

### Risiken

1. **Wallet-Invariante gilt nicht** – das Einlösen bucht kein Wallet ab, nur das Inventar; der Stamp am Inventar
   reicht wie bei der Freigabe. Ein paralleler Kind-Antrag wird durch die Deckungsprüfung der Freigabe gehalten.
2. **Ausstellerbindung doppelt:** der Artikel muss dem Aufrufer gehören (`AdultId == supervisorId`), nicht nur
   irgendeinem Betreuer des Kindes – sonst löste der Vater Omas Artikel ein (AK 3).

### Angriffsplan

1. `ShopService.RedeemForChildAsync(supervisorId, childId, articleId, quantity, now, ct)`; Prüfungen in dieser
   Reihenfolge: Menge > 0, Artikel des Aufrufers, Inventar deckt; dann Inventar senken und Antrag als `Approved`
   mit `RequestedBy = Supervisor`, `RequestedAt = ClosedAt = now` anlegen.
2. Migration `ActivationRequestedBy`; Mapping in beiden Controllern.
3. Endpunkt mit `ChildOwnershipFilter`, `Pugling.Client`-Methode.
4. Vater-Web `ChildShopView`: je Inventarzeile „Selbst einlösen…" mit Menge und `confirmAction`; Sohn-App
   `RequestsTab`: „vom Vater eingelöst".

### Testweg

- `ShopFlowTests`: AK 1 (Approved, Inventar sinkt), AK 2 (`insufficient_inventory`, nichts angelegt), AK 3
  (Omas Artikel → 404), AK 4 (fremdes Kind 404, Student-Token 403), AK 5 (Menge 0 → 400), AK 6 (Kind-Liste trägt
  `RequestedBy = Supervisor`, ein Kind-Antrag `Child`).
- Migrationstest: Bestandszeile ohne Spalte → `Child`.
- E2E im Shop-Spec des Sprints: Vater löst ein, Sohn sieht „vom Vater eingelöst".

## Verlauf

- 2026-11-18 · idee · aufgenommen aus dem Wunsch des Vaters
- 2026-11-18 · ausformuliert · Ist-Stand am Code belegt (Antrag nur mit Kind-Token, kein Feld für den Antragsteller), sechs offene Punkte mit Empfehlung
- 2026-11-18 · ausformuliert · Vorgabe des Vaters nachgetragen: Einlösen durch den Vater bleibt die Ausnahme, der Antrag des Kindes der Normalfall
- 2026-11-19 · gegrillt · Vater übernimmt alle sechs Empfehlungen (Entscheidungen 1–6)
- 2026-11-19 · geschaetzt · M, beides, Migration `RequestedBy` (Bestand → `Child`), kein Vertragsbruch; Sprint 96 mit B-645
- 2026-11-19 · in-arbeit · gebaut mit B-645 in Sprint 96 (Migration gemeinsam: `ShopStockPerChild` trägt `RequestedBy`). Belege je AK-Fall:
  - AK 1 Freigabe sofort, Inventar sinkt → `ShopStockPerChildTests.cs:501`, R2 (Inventar bleibt), R4 (Status Pending) → rot.
  - AK 2 + AK 5 Deckung, Menge 0 → `ShopStockPerChildTests.cs:526`, R5, R6 → rot.
  - AK 3 + AK 4 Omas Artikel 404, fremder Erwachsener 404, Sohn-Token 403 → `ShopStockPerChildTests.cs:547`, R1 (Ausstellerbindung weg) → rot.
  - AK 6 Kind sieht „von Papa eingelöst“ → `ShopStockPerChildTests.cs:501` (R3 → rot) und E2E (F2 → rot); Migration: Bestand `Child` → G3 → rot.
  - AK 7 Vater-Web, Knopf je Inventarzeile mit Bestätigung → E2E `taschengeld-monatlich.spec.ts`.
  - AK 8 Integrationstests je AK 1–5 → oben.
- 2026-11-19 · abgenommen · `08fd05a6`; Rollengang Vater und Sohn per `taschengeld-monatlich.spec.ts` gegen frischen Server nach der letzten Bearbeitung (8 passed mit den Regressionszeugen). Bewusst offen, nicht blockierend: Der Fokus kehrt nach „Abbrechen“ nicht zum Knopf zurück; „Papa hat für dich eingelöst“ steht auch bei Mama oder Oma.
