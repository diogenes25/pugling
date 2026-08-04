---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/api]
aliases: [Idempotency-Key, ETag, If-Match, optimistische Sperre in der API]
status: ausformuliert
prio: P3
art: Frage
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/api-design-bewertung.md (Vorschläge B1, B2) — Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04
grund: ""
ersetzt_durch: []
---

# B-103 · Prüfauftrag: Brauchen `Idempotency-Key` und ETag/`If-Match` in dieser App einen Platz?

Die API-Design-Bewertung nennt beide als **fehlende Bausteine** und gewichtet den Idempotenz-Schlüssel mit
der höchsten Stufe. Die Arbeitsrunde hat beide gemessen und kommt zu **nein** — belegt, nicht aus
Bequemlichkeit. Diese Story existiert, damit die Antwort auffindbar ist: sonst kommen zwei
Lehrbuch-Bausteine in einem halben Jahr als frische Idee zurück und kosten wieder eine Runde.

**Empfehlung: `verworfen` mit dem Grund unten.** Die Entscheidung gehört dem Menschen — und für eine
`Frage` ist `verworfen` das Erfolgsergebnis, kein Scheitern.

## User Story

Als **Entwickler** möchte ich eine belegte Antwort auf die Frage „fehlen dieser API Idempotenz-Schlüssel und
bedingte Schreibzugriffe?", damit die beiden Bausteine nicht bei jeder Bewertung neu als Lücke gemeldet
werden und niemand eine Migration für ein Problem baut, das diese App nicht hat.

## Ist-Stand am Code

**Zu Teil 1 (Idempotenz-Schlüssel):**

- Kein `Idempotency-Key` in der gesamten API (0 Treffer).
- Von den vier im Bericht genannten geldbewegenden POSTs sind **drei nicht betroffen**:
  `POST student/me/skins/{skinId}/purchase` weist einen wiederholten Kauf über
  `skin_already_unlocked` ab (`Controllers/Student/MeController.cs:181`) und ist damit **schon idempotent**;
  `shop/inventory/{articleId}/activate` bewegt kein Geld (der Bestand sinkt erst bei der Genehmigung,
  dokumentiert `:322`); `POST supervisor/children/{childId}/points`
  (`Controllers/Supervisor/ChildrenController.cs:228`) ist ein Vater-Geschenk mit sofort sichtbarer
  Ledger-Zeile (`:199`). Übrig bleibt **einer**: `POST student/me/shop/listings/{listingId}/purchase`
  (`MeController.cs:268`).
- Der Rückweg für einen Doppelkauf **existiert und ist Produktverhalten**: `POST children/{}/points` als
  Druckventil des Vaters.
- `UnitsPerPurchase`/`CurrentStock` machen den **wiederholten Kauf desselben Angebots zu einem legitimen
  Akt** (zweimal „30 Minuten Zocken" hintereinander) — entscheidend für die Ersatzidee unten.

**Zu Teil 2 (ETag/`If-Match`):**

- 0 Treffer für ETag/`If-Match`/`Cache-Control`; der registrierte Code `concurrency_conflict` ist für einen
  Client damit **unerreichbar**.
- `ConcurrencyStamp` liegt an **vier** Entities: `Child` (`Models/AdminEntities.cs:129`) und drei Shop-Zeilen
  (`Models/GamificationEntities.cs:149,190,220`). **`StudyPlan` und `PlanPosition` tragen keinen** — kein
  Feld im Modell, kein `IsConcurrencyToken` in `Data/PuglingDbContext.cs`.
- `Child.ConcurrencyStamp` ist der **Wallet-Serialisierungspunkt**: jeder abbuchende Pfad bumpt ihn
  (residente Invariante, Kommentar an `Services/…/ShopService.cs:220`).

## Die echte Lücke

Beide Bausteine fehlen tatsächlich — die Frage ist nicht, *ob*, sondern *wofür*. Nach der Messung bleibt für
Teil 1 ein einziger Endpunkt mit einem existierenden manuellen Rückweg, und für Teil 2 ein Vorschlag, der
gegen den Code nicht ausführbar ist, ohne zwei Spalten und eine Neufaltung der Migrationskette zu bezahlen.
Die Lücke ist damit keine Aufgabe, sondern eine **zu dokumentierende Entscheidung**.

## Die Antwort der Arbeitsrunde vom 2026-08-04

1. **Teil 1: nein.** Eine Tabelle `(AccountId, Key)` bedeutet Migrationskette **neu falten** plus bewusste
   Zeilen in den Schema-Toren G1–G9 — für einen Endpunkt, dessen Fehlerfall der Vater in zehn Sekunden
   korrigiert. Das ist Zahlungsdienst-Hygiene in einem Haushalt mit drei Konten.
2. **Die tabellenfreie Ersatzidee trägt nicht.** Der API-Designer schlug ein Fenster-Dedup in
   `ShopService.PurchaseAsync` vor (Platz sauber bestimmt: nach `ApplyDueRefill`, vor den Ledger-Zeilen; die
   Wallet-Invariante bliebe unberührt, weil ein Pfad ohne Abbuchung den Stamp nicht bumpen muss). Der
   Entwickler hat es widerlegt: ein Fenster unterscheidet **„Retry" nicht von „noch eins"** — die Information
   dazu liegt ausschließlich im Client, und genau dafür gäbe es den Header. Ein Fenster frisst also entweder
   echte Käufe (zu groß) oder fängt nichts (zu klein). Es wäre kein Idempotenz-Mechanismus, sondern ein
   Ratenlimit im Kaufpfad, das korrektes Verhalten des Kindes stumm unmöglich macht.
3. **Teil 2: nein, und der Vorschlag war gegen den Code unausführbar.** „Dort, wo schon ein
   `ConcurrencyStamp` liegt" trifft genau die zwei Ressourcen **nicht**, für die der Bericht argumentiert
   (Plan und Position) — das wären zwei neue Spalten und eine Neufaltung, im Aufwand „mittel" stand davon
   nichts. Und der Stamp am Kind **kann nicht beides sein**: als ETag ausgegeben ändert er sich, sobald das
   Kind eine Karte richtig beantwortet — der Vater bekäme beim Patchen des Kindnamens ein `412`, weil das Kind
   zwischenzeitlich *gelernt* hat. Saldo-Serialisierung und Ressourcen-Version sind zwei Bedeutungen für einen
   Wert.
4. **Halb ist hier schlimmer als nichts** — das Argument des Berichts über sich selbst: ein Client, der ETags
   mal bekommt und mal nicht, baut die `If-Match`-Prüfung nicht ein; dann trägt der Server die Kosten für
   einen Mechanismus, den niemand benutzt. Darum gibt es keine sinnvolle Minimalvariante und darum hat der
   API-Designer vollständig zurückgezogen statt verkleinert.
5. **Eine falsche Verbindung im Bericht ist aufgelöst:** C3 (`POST creator/vocabulary/lookup` als
   Lesevorgang) hängt **nicht** an Teil 2 („nur relevant, wenn B2 kommt"). C3 wäre ein Thema für
   `If-None-Match`/Antwort-Caching, ETag/`If-Match` sind bedingte **Schreib**zugriffe.
6. **Was die Antwort kippen würde** (damit die Frage nicht dogmatisch geschlossen wird): ein *gemessener*
   Wiederholungsfall — etwa ein Netz-Timeout auf dem Handy des Kindes, der einen doppelten Kauf erzeugt. Bis
   dahin ist der Baustein vorsorglich; der Bericht nennt keine gemessene Wiederholung.

## Akzeptanzkriterien

Diese Story ist erledigt, wenn eine **belegte Antwort** steht — nicht, wenn etwas gebaut ist:

1. Der Mensch hat entschieden: `verworfen` (Empfehlung) oder eine der beiden Teilfragen bleibt offen, mit
   Begründung.
2. Bei `verworfen` steht das Feld `grund` gesetzt. Vorschlag: „gemessen in der Arbeitsrunde 2026-08-04 — ein
   betroffener Endpunkt statt vier, Rückweg existiert; der ETag-Vorschlag ist gegen den Code unausführbar und
   mit der Wallet-Invariante unverträglich."
3. **Ein Satz steht in `backend/Pugling.Api/CLAUDE.md`**, warum `concurrency_conflict` bewusst für Clients
   unerreichbar bleibt — ohne ihn ist der Code in einem Jahr wieder ein „toter Code"-Befund und die Frage neu
   offen.
4. Es wurde **nichts** gebaut: keine Tabelle, keine Spalte, kein Header, keine Migration.

## Verlauf

- **2026-08-04** — angelegt aus `docs/api-design-bewertung.md` (B1, B2) und der Arbeitsrunde
  PM/API-Designer/Entwickler. Beide Bausteine wurden von **beiden** Rollen unabhängig verworfen; der
  API-Designer hat Teil 1 von vier auf einen betroffenen Endpunkt korrigiert und Teil 2 vollständig
  zurückgezogen, der Entwickler hat die tabellenfreie Ersatzidee widerlegt. Als `art: Frage` angelegt statt
  direkt als `verworfen`, weil das Verwerfen laut [README](README.md) dem Menschen gehört.
