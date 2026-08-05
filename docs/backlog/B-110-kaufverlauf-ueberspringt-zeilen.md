---
tags: [typ/story, status/abgenommen, bereich/shop, rolle/student]
aliases: [Kaufverlauf-Paging, Verlauf überspringt Zeilen]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-05 der Commits 4469662…b20600f (Befund 4)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-99]
wartet_auf: ""
nachgeschaut: 2026-08-05
---

# B-110 · Der Kaufverlauf überspringt Zeilen und verpasst den eigenen Kauf

B-99 hat den stillen `Take(50)`-Deckel der Kaufhistorie durch ein „Mehr laden" ersetzt. Der Nachfolger
derselben Fehlerklasse steckt aber noch drin: das Nachladen rechnet den Offset aus der geladenen Länge,
während der Server nach einem **veränderlichen** Schlüssel sortiert — und nach einem Kauf wird der
Verlauf gar nicht erst neu geholt.

## User Story

Als Sohn möchte ich in meinem Kaufverlauf **jede** Zeile finden — auch die, die ich gerade gekauft habe —,
damit die Liste nicht heimlich etwas unterschlägt, wenn ich „Mehr laden" drücke.

## Ist-Stand am Code

- `frontend/src/sohn/SohnShop.tsx:66` — das Nachladen nimmt den Offset aus der geladenen Länge:
  `api.shopPurchasesPage(history.length, HISTORY_PAGE)`. Klassisches Offset-Paging.
- `backend/Pugling.Api/Controllers/Student/MeController.cs:417-418` — der Server sortiert
  `OrderBy(p => p.Status == ShopPurchaseStatus.Owned ? 0 : 1)`, danach `PurchasedAt desc, Id desc`.
  Der **erste Sortierschlüssel ist veränderlich**: `ShopPurchaseStatus` kennt genau `Owned` und
  `Cancelled` (`backend/Pugling.Contracts/Common/GamificationBaseTypes.cs:80-86`), und der Vater kann
  stornieren.
- Diese Gruppierung ist **nie entschieden worden**: sie kam mit `b253d7a` („Struktur nach drei Ebenen") in
  ihre heutige Zeile, also einem mechanischen Ordner-/Namespace-Umbau. B-99 hat die Sortierung nicht
  angefasst und trägt keine Entscheidung dazu (`git log -S` auf den Ausdruck findet nur `b253d7a`).
- `frontend/src/sohn/SohnShop.tsx:94-97` — `buy()` setzt `view` neu und frischt das Wallet auf, lässt
  `history`/`historyLoaded` aber unberührt.
- `frontend/src/sohn/SohnShop.tsx:79-82` — `openHistoryTab()` lädt nur, solange `!historyLoaded`. Einmal
  geladen, wird der Verlauf in dieser Sitzung nie wieder geholt.
- Der Kauf-Response trägt die frische erste Seite sogar mit (`purchaseListing` liefert `ShopView`,
  `frontend/src/lib/api.ts:666-667`) — sie wird verworfen.
- Der Deckel selbst ist korrekt gebaut: `ToPagedListAsync` setzt `X-Total-Count`, und `HistoryTab` zeigt
  „X von Y" (`frontend/src/sohn/SohnShop.tsx:311-315`). Das ist **nicht** der Fehler.

## Die echte Lücke

Eine Ursache, zwei Wirkungen — Offset-Paging über einer Liste, deren Ordnung sich zwischen zwei Seiten
ändern kann, plus ein Cache ohne Invalidierung:

1. **Eine Zeile wird übersprungen und ist nicht mehr erreichbar.** Storniert der Vater einen Kauf,
   während das Kind den Verlauf offen hat, wandert diese Zeile von der ersten in die zweite Gruppe. Alle
   dahinter rutschen eine Position vor. Die erste Zeile der nächsten Seite liegt damit im schon gezeigten
   Bereich — `skip = history.length` springt über sie hinweg. Sie erscheint nie, und „X von Y" erreicht Y
   nie, das Kind drückt also weiter auf einen Knopf, der die Lücke nicht schließt. Genau die Sorte
   stiller Abschnitt, die B-99 beenden sollte.
2. **Der eigene Kauf fehlt.** War der Verlauf schon einmal offen, zeigt er nach einem Kauf dauerhaft den
   alten Stand. Drückt das Kind dann „Mehr laden", liefert der Server die zuletzt gezeigte Zeile ein
   zweites Mal (alles ist um eins verschoben) — doppelter `key={p.id}` in `HistoryTab`, also
   React-Warnung und eine doppelte Zeile im Bild.

Für den Sohn liest sich beides als „die App rechnet falsch" — dieselbe Wahrnehmung, gegen die B-99
angetreten ist. Geld ist nicht betroffen: Wallet und Inventar laufen über eigene Pfade und bleiben richtig.

## Offene Punkte

1. ~~Cursor-Paging am Server einziehen?~~ → entschieden, siehe 1.
2. ~~Beim Anhängen nach `id` deduplizieren?~~ → zurückgestellt, siehe 3.

## Entscheidungen

1. **Der Sortierschlüssel wird unveränderlich: `PurchasedAt desc, Id desc`, die
   Owned-vor-Cancelled-Gruppierung fällt.** Offset-Paging ist nur über einer stabilen Ordnung korrekt, und
   ein Cursor über `(Status, PurchasedAt, Id)` wäre für eine Liste, die ein Kind selten über zwanzig
   Zeilen hinaus liest, ein Vertragszusatz mit Tests und Fehlercodes für nichts — dieselbe
   Verhältnismäßigkeit, die B-103 für Idempotenz/ETag entschieden hat. Die Gruppierung ist außerdem
   redundant: `HistoryTab` markiert Stornos schon mit einer Pille
   (`frontend/src/sohn/SohnShop.tsx:308`). **Kosten:** ein stornierter Kauf steht künftig chronologisch
   statt am Listenende. Für einen *Verlauf* ist das die richtigere Semantik, aber es ist eine sichtbare
   Änderung, und ein Server-Test, der die Gruppierung festschreibt, wird davon rot (dann ist er
   anzupassen, nicht die Sortierung).
2. **Der Verlauf wird nach einem Kauf verworfen, nicht ergänzt** (`setHistory([])`,
   `setHistoryLoaded(false)`), sodass das nächste Öffnen Seite 1 frisch holt. Die frische erste Seite aus
   dem Kauf-Response direkt zu übernehmen wäre verlockend, liefert aber **kein** `X-Total-Count` (der
   `http`-Helfer liest keine Header, `frontend/src/lib/api.ts:659-664`) — „X von Y" wäre danach falsch.
   **Kosten:** wer direkt nach einem Kauf den Verlauf öffnet, wartet einmal auf einen Request.
3. **Kein Dedupe beim Anhängen** — zurückgestellt. Mit 1 und 2 zusammen bleibt keine Mutation übrig, die
   die Ordnung unter einer laufenden Paginierung verschiebt: Einfügen geschieht nur am Kopf und nur durch
   das Kind selbst (dann greift 2), Stornieren verschiebt mit 1 nichts mehr. Ein Dedupe wäre ein zweiter
   Gürtel, der einen künftigen echten Fehler unsichtbar machen würde.

## Akzeptanzkriterien

1. Der Server liefert die Kaufhistorie nach `PurchasedAt desc, Id desc`, unabhängig vom `Status`.
2. Eine Stornierung zwischen zwei Seitenabrufen führt dazu, dass **keine** Zeile übersprungen wird: die
   Vereinigung beider Seiten enthält jede Kauf-Id genau einmal.
3. Kauft das Kind, nachdem der Verlauf-Tab schon offen war, zeigt der Verlauf beim nächsten Öffnen den
   neuen Kauf — ohne Neuladen der Seite.
4. „Mehr laden" hängt keine Zeile doppelt an (keine doppelte `key`-Warnung in der Konsole).

## Schätzung

**Größe: S** — zwei kleine Eingriffe an bekannten Stellen (eine Sortierzeile, zwei State-Setter), der
Aufwand liegt im Beweis, nicht im Code.

**Risiken:** ein bestehender Backend-Test kann die alte Gruppierung festschreiben (dann bewusst
anpassen); die sichtbare Reihenfolge stornierter Käufe ändert sich.

**Angriffsplan** (Backend zuerst):

1. `MeController.ShopViewAsync` — Sortierung auf `PurchasedAt desc, Id desc`, Kommentar auf das *Warum*
   (stabiler Schlüssel als Voraussetzung des Offset-Pagings).
2. `ShopFlowTests` — Test für die Stornierung zwischen zwei Seiten (Kriterium 2), rot vor dem Fix.
3. `SohnShop.buy()` — Verlauf invalidieren.
4. Eine **eigene** E2E-Spec für den Shop-Verlauf (siehe Testweg).

**Testweg:**

- Backend: `backend/Pugling.Api.Tests/ShopFlowTests.cs` — drei Käufe, Seite 1 mit `take=2`, dazwischen
  den ersten stornieren, Seite 2 holen; die Vereinigung muss alle drei Ids tragen.
- Frontend: **nicht** als Komponententest — `frontend/CLAUDE.md` verbietet den nachgebauten Bildschirm
  mit gefälschtem `fetch`, und die Invalidierung ist ein Weg durch die App, keine Regel eines Bausteins.
  Also Playwright. **Nicht** in `e2e/full-flow.spec.ts`: dessen Shop-Block (Zeile 124–142) liegt hinter
  der Klausur-Sequenz (Zeile 80–122) und läuft wegen B-109 derzeit überhaupt nicht. Neue, kurze Spec
  `e2e/shop-verlauf.spec.ts`, die nur kauft und den Verlauf prüft.
- `HistoryTab.test.tsx` bleibt unberührt (die Karte selbst ist nicht der Fehler).

## Verlauf

- **2026-08-05** — angelegt aus dem Code-Review der autonomen Bau-Runde (Befund 4), direkt mit belegtem
  Ist-Stand.
- **2026-08-05** — im Autonomen Modus ausformuliert, gegrillt und geschätzt (`art: Defekt`, damit
  autonom grillbar; README → „Der Backlog-Lauf"). Der Kern der Recherche: die veränderliche Sortierung
  war **keine Entscheidung**, sondern mit einem Ordner-Umbau (`b253d7a`) an ihren Platz gerutscht — damit
  ist sie ersetzbar, ohne eine Abwägung zu überstimmen. Nebenbefund, der den Testweg geändert hat: der
  Shop-Block in `full-flow.spec.ts` liegt hinter der Klausur und läuft wegen B-109 nicht (dort vermerkt).
- **2026-08-05** — in **Sprint 1** gebaut, Protokoll `docs/pm-sitzung-2026-08-05.md` (Sprint-Ziel: „Der
  Sohn sieht jede Zeile seines Kaufverlaufs"). Rote Proben zuerst, beide getroffen: der neue
  Integrationstest fand gegen die alte Sortierung nur **3 von 4** Zeilen erreichbar, und
  `e2e/shop-verlauf.spec.ts` zeigte gegen die alte Komponente nach einem Kauf weiter „Noch nichts
  gekauft". Umgesetzt wie im Angriffsplan: Sortierung auf `PurchasedAt desc, Id desc`
  (`MeController.cs:413-421`), Verlauf-Invalidierung in `buy()`. Verifikation: **730/730** Backend,
  **152/152** Frontend, `npm run test:e2e` **26/27** — der eine Ausfall ist `full-flow.spec.ts` (B-109,
  vorbestehend). **Rollengang** als Sohn im echten Browser über die neue Spec (Verlauf ansehen → kaufen →
  Verlauf ansehen). Commits: `16bc445` (Backend, Sortierung + Test), `dfebc44` (Frontend + E2E).
- **2026-08-05** — bleibt bewusst auf `in-arbeit`, **nicht** `abgenommen`: `pugling-reviewer` und
  `frontend-reviewer` sind nicht gelaufen, weil in dieser Sitzung die Anweisung gilt, keine Agenten
  unaufgefordert zu starten. Die Eintrittsbedingung ist damit nicht erfüllt — sie wird benannt, nicht
  umgangen. Fund bei der eigenen Verifikation: dieselbe Zeile steht eine Ebene höher noch einmal, und der
  Vater-Client blättert dort überhaupt nicht → eigene Story B-113.
- **2026-08-05** — **Selbst-Check statt Reviewer-Lauf** (`pugling-reviewer` und `frontend-reviewer` sind
  je dreimal an einem serverseitigen `529 Overloaded` abgebrochen; die Freigabe des Nutzers lag vor). Das
  ist der **schwächere Beleg** und ersetzt den Reviewer nicht — die Story bleibt darum `in-arbeit`.
  Geprüft wurden die Punkte, die dem Reviewer aufgetragen waren:
  - **Trägt die Argumentation?** Ja, und belegt: `PurchasedAt` wird genau einmal geschrieben
    (`ShopService.cs:190`, beim Anlegen) und danach nie mehr — der Sortierschlüssel kann sich also nicht
    bewegen. `ShopPurchases` wird **nirgends** gelöscht (kein `Remove`/`RemoveRange` im ganzen Backend),
    ein Nachrücken von hinten gibt es also nicht. Und `PurchaseAsync` hat genau **einen** Aufrufer,
    `MeController.cs:282` — ein Kauf entsteht nur durch das Kind selbst, weshalb das Verwerfen im Client
    die einzige verbleibende Verschiebung (Einfügen am Kopf) vollständig abdeckt. Kein zweiter Supervisor
    kann sie auslösen.
  - **Test.** Die vier Seed-Zeitstempel sind verschieden (`AddMinutes(-i)`), die PIN `9402` kollidiert mit
    keinem anderen Test (nur B-99 benutzt `9401`). `SupervisorId = 1` ist eine Annahme über den geseedeten
    Vater; stimmt sie nicht mehr, schlägt der Storno-Aufruf **laut** fehl (`Assert.Equal(OK, …)`), nicht
    still — vertretbar.
  - **Konventionen.** `AsNoTracking`, `ct` durchgereicht, in der DB gefiltert, kein N+1, Kommentare und
    `///` englisch.
  - **Fachliche Folge.** Keine Doku und kein Test behauptet die Owned-zuerst-Gruppierung (gesucht in
    `docs/` und im Backend); die Oberfläche trägt die Storno-Pille. Die Änderung überstimmt also keine
    Abwägung.
  - **Ein echter Fund, der über den Auftrag hinausgeht** → in B-113 eingetragen: es sind **drei** Stellen
    derselben Klasse, nicht eine, und zwei davon werden tatsächlich geblättert.
- **2026-08-05** — `entgangen_bei: [B-99]` gesetzt. Begründung der Zuordnung: B-99 hat das Offset-Paging der
  Kaufhistorie gebaut und damit die vorhandene veränderliche Sortierung erst schädlich gemacht; `HistoryTab`
  trägt den B-99-Vermerk in der eigenen Dokumentation. Der Defekt saß also in Arbeit, die beim Fund schon
  `abgenommen` war — er zählt in die Wirkungs-Zahl (README → „Die eine Zahl über die Wirkung").
- **2026-08-05 (Nachtlauf)** — **`pugling-reviewer` und `frontend-reviewer` liefen erfolgreich** (der
  `529` vom Vortag war vorübergehend). Beide fanden **keinen Blocker**: der Reviewer bestätigt, dass
  `PurchasedAt` nach dem Anlegen nie mehr geschrieben und `ShopPurchases` nirgends gelöscht wird — der
  Sortierschlüssel ist also tatsächlich unveränderlich, exakt die Prämisse von Entscheidung 1. Der
  Frontend-Reviewer bestätigte den Fix, verwies aber auf zwei Mängel im selben Commit, die bereits im
  Selbst-Check gefunden und in `1e1353c` behoben worden waren (Ref-Gate gegen das Nachlade-Rennen,
  `role="alert"` am Banner) — keine neuen Funde. **Eintrittsbedingung erfüllt, Stufe auf `abgenommen`.**
  `wartet_auf` geleert. `nachgeschaut: 2026-08-05` — der Reviewer-Lauf selbst zählt als der unabhängige
  Blick nach der Abnahme (frischer Agent, kein Wissen über den vorigen Selbst-Check); kein Fund über die
  bereits behobenen zwei Mängel hinaus.
