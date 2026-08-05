---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/gamification, rolle/student]
aliases: [Kaufhistorie bei 50 abgeschnitten, stille Take(50), Shop-Paging]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/api-design-bewertung.md (Vorschlag B3) — Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04
grund: ""
ersetzt_durch: []
---

# B-99 · Die Kaufhistorie des Kindes endet lautlos bei 50 Zeilen

Der Shop-Schirm des Kindes schneidet seine Kaufhistorie mit einem harten `.Take(50)` ab — ohne
`X-Total-Count`, ohne `skip`, ohne irgendeinen Weg an die älteren Zeilen. Das Kind kann nicht einmal
*merken*, dass etwas fehlt. Nach der Rechnung unten fällt die Grenze innerhalb des ersten Schuljahrs.

## User Story

Als **Kind** möchte ich meine gesamte Kaufhistorie erreichen können, damit ich sehe, wofür ich gespart und
was ich mir schon geleistet habe — das ist der Beleg dafür, dass Lernen sich gelohnt hat.

## Ist-Stand am Code

- `Controllers/Student/MeController.cs:414` schneidet die Käufe der Sammel-Antwort (`ShopViewResponse`) mit
  `.Take(50)` ab. Kein `skip`, kein `take` im Vertrag, kein `X-Total-Count` im Kopf.
- **Es gibt keinen Student-Endpunkt für Käufe.** In `MeController` existieren `shop`, `shop/inventory` und
  `shop/activations` — **kein** `shop/purchases`. Paginierte Käufe gibt es nur supervisor-seitig
  (`Controllers/Supervisor/ShopController.cs:333`), wo das Kind nicht hinkommt. Der Bericht schlug vor, die
  Käufe „auf den bereits paginierten Weg zu legen" — **diesen Weg gibt es nicht**; beide Rollen haben das in
  der Runde unabhängig festgestellt.
- **Kein Aufräumpfad:** kein `Remove`/`Delete` auf `ShopPurchases` im ganzen Backend; ein Storno setzt nur
  den `Status`. Die Zeilen wachsen also monoton.
- **Wann die Grenze fällt** (gerechnet in der Runde, aus dem Seed): billigstes Angebot 50 Münzen
  (`Data/Seed.cs:830`), realistischer Tagesertrag 40–80 (Missionen 15+10, `Seed.cs:684-685`, plus 10–20 je
  Position, `:622,647`) ⇒ ein Kauf alle 1–3 Tage ⇒ **50 Zeilen nach etwa 4–12 Monaten**.

## Die echte Lücke

Der Bericht hat daraus ein Programm gemacht („35 Array-GETs ohne Paging"). Das ist als Zählung richtig, als
Handlungsbedarf überzeichnet: von den 35 wachsen in dieser App real **sieben** (Runde 2, gemessen) —
`creator/tags`, `creator/vocabulary/tags`, die beiden `usage`-Listen über den gesamten Katalog,
`study-plans/{planId}/positions`, `creator/subjects/{subjectId}/chapters` und `supervisor/adults`; der Rest ist
durch eine Familie, eine Woche, ein Lehrwerk oder ein Manifest begrenzt. **Eine** Stelle verliert heute Daten
sichtbar, und das ist diese.

## Ergebnis der Arbeitsrunde vom 2026-08-04 (gegrillt)

1. **Die kleinste Variante bauen, nicht das Programm:** `?purchaseTake=`/`?purchaseSkip=` deklarieren und
   `X-Total-Count` setzen — **statt** einen neuen Student-Endpunkt anzulegen. Vorschätzung **S**,
   `wo: beides` (der Sohn-Shop braucht den Weg zu den älteren Zeilen), keine Migration,
   **kein** Vertragsbruch (additive Query-Parameter).
   *Alternative, falls das Kind eine eigene Historien-Seite bekommen soll:* ein neuer
   `GET student/me/shop/purchases`. Kostet zusätzlich eine Client-Methode, eine Zeile im
   Endpunkt-Abdeckungs-Wächter (263 → 264) und einen Test — das ist die teurere Hälfte und gehört nur
   gebaut, wenn die Oberfläche es hergibt.
2. **Die anderen sechs wachsenden Listen sind kein Teil dieser Story.** Sie sind hier notiert, damit sie
   nicht verloren gehen: bei `supervisor/adults` hängt das Wachstum an
   `Controllers/Supervisor/TeacherAccountsController.cs:41` (`[AllowAnonymous]`) und damit an der offenen
   Frage [B-48](B-48-anonyme-registrierung-produktion.md).
3. **Das vorgeschlagene Paging-Tor ist offen** — hier lagen die Rollen auseinander, und beide haben nachgegeben:
   Der Entwickler nimmt „sofort scharf stellen" zurück (seine zehn Zeilen zeigten, dass die Regel
   *entscheidbar* ist, nicht dass sie *trennt*). Der API-Designer gibt zu, dass sein strukturelles Merkmal
   („keine `Where`-Bedingung auf einen Eigentümer-Schlüssel") im LINQ-Rumpf steht und damit weder reflexiv
   noch aus dem Dokument entscheidbar ist. Zwei Formen sind übrig; die Entscheidung liegt in
   [B-101](B-101-fehlercodes-und-drei-waechter.md), damit diese Story ein reiner Defekt-Fix bleibt.

## Akzeptanzkriterien

1. Ein Kind mit mehr als 50 Käufen erreicht über `GET student/me/shop` (oder den neuen Endpunkt, je nach
   Entscheidung 1) **alle** seine Käufe.
2. Die Antwort nennt die Gesamtzahl (`X-Total-Count`), sodass die Oberfläche „51 von 137" anzeigen kann
   statt stillschweigend 50 Zeilen.
3. Der Sohn-Shop hat einen Weg zu den älteren Zeilen (Pager oder „mehr laden"), keine abgeschnittene Liste
   ohne Hinweis.
4. Ein Integrationstest legt >50 Käufe an und war vor der Änderung rot.

## Schätzung

`groesse: S`, `wo: beides` (Backend: additive Query-Parameter am Bundle-Endpunkt; Frontend: der
Sohn-Shop hatte bislang **gar keine** Verlaufs-Ansicht — `view.purchases` wurde geladen, aber nirgends
gezeigt), `migration: nein`, `vertragsbruch: nein`. Angriffsplan: `purchaseSkip`/`purchaseTake` an
`MeController.Shop`/`ShopViewAsync`, `.Take(50)` durch den geteilten `ToPagedListAsync`-Helfer ersetzt
→ Client-Methode `shopPurchasesPage` (eigener Zuschnitt, weil `httpPaged` ein Objekt mit
verschachteltem Array nicht parst) → neuer „Verlauf"-Tab im Sohn-Shop mit „Mehr laden" (Anhängen statt
Pager, passend zur Arcade). Testweg: ein Integrationstest mit 60 direkt geseedeten `ShopPurchase`-Zeilen
(rot gegen den Vorzustand, `git stash` von `MeController.cs`), eine reine `HistoryTab`-Komponente per
Vitest (Muster `SelfAssessAnswer.test.tsx`), eine kurze Ergänzung in `full-flow.spec.ts`.

## Verlauf

- **2026-08-04** — angelegt aus `docs/api-design-bewertung.md` (B3) und der Arbeitsrunde. Der Bericht nannte
  einen Endpunkt, den es nicht gibt (`student/me/shop/purchases`); die Rechnung „50 Zeilen in 4–12 Monaten"
  und die Korrektur „sieben statt zwei wachsende Listen" stammen aus Runde 2.
- **2026-08-05** — im Autonomen Modus gegrillt (Arbeitsrunden-Ergebnis übernommen), geschätzt und gebaut.
  Rote Probe zuerst: der neue Backend-Test scheiterte gegen den Vorzustand (kein `X-Total-Count`-Header).
  `dotnet test Pugling.sln -c Release` → **716/716 grün**. `pugling-reviewer` fand keinen Blocker (nur
  einen Doku-Hinweis am Contract-Record, direkt ergänzt). `frontend-reviewer` fand zwei Punkte: die
  Nachlade-Sperre stand auf `useState` statt `useRef` (derselbe Fallstrick, den `useAction`/
  `SohnPractice.tsx` im Projekt schon einmal gelöst haben — zwei überlappende Aufrufe hätten Zeilen
  doppelt angehängt) und keine E2E-Abdeckung für den neuen Tab. Beides behoben: `useRef`-Sperre nach
  demselben Muster, eine kurze Ergänzung in `full-flow.spec.ts` (Verlauf-Tab öffnen, den eben getätigten
  Kauf sehen). Den E2E-Lauf selbst konnte ich nicht gegenprüfen — Port 5200 war von einem vorbestehenden,
  nicht von mir gestarteten Dev-Server-Prozess belegt; die neuen Zeilen folgen aber exakt dem
  Locator-Muster der übrigen Datei. `npm run build` (Typecheck) und `npm test` → **131/131** (127 + 4 neu,
  `HistoryTab.test.tsx`) grün. Commit: siehe Repo-Verlauf (B-99-Commit). Status → `abgenommen`.
- **2026-08-05** — Nachtrag zur neuen Eintrittsbedingung (README → „Der Rollengang fällt am leichtesten
  weg"): **kein Rollengang geführt.** Belegt waren die Suite und der Reviewer, nicht aber ein Gang als
  Sohn an der laufenden App. Kein Schaden bekannt — die Lücke steht hier, statt still zu bleiben.
