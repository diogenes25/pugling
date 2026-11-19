---
tags: [typ/protokoll, bereich/backlog, bereich/shop]
---

# PM-Sitzung: Shop je Kind (Sprint 96)

**Datum:** 2026-11-19  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Der Familien-Shop zählt je Kind – eigener Bestand, eigenes Taschengeld, monatliches Auffüllen –, und der
Vater kann ausnahmsweise selbst aus dem Inventar einlösen.

## Runde 1 — Ausgangslage

Aufruf `/pm-loop` ohne eigenes Ziel. Laut Roadmap des Vortags ([Runde 8](pm-sitzung-2026-11-18.md)) wären B-672 und
Sprint 90 dran. Der Vater hat aber am 2026-11-18 und heute im Chat zwei Wünsche aus dem **echten Betrieb**
nachgereicht; die sind beobachtet und gehen vor (Step 2.1). Kein neuer Befund unter `docs/anmerkungen/`.

- **Vater (O-Ton, Chat 2026-11-19):** „Shops müssen pro Kind sein: Theodor bekommt 10 €, Friedrich 20 €, je einmal im
  Monat; Daddelzeit 1 h täglich je Kind; keiner darf dem anderen etwas wegkaufen." Und (2026-11-18): „Das Kind will
  einlösen, hat aber das Handy nicht dabei – ich will dann selbst aus seinem Inventar einlösen können, als Ausnahme."
- **Sohn:** am Code belegt, nicht erfunden – kauft Friedrich das Monats-Taschengeld, ist es für Theodor weg
  (`ShopService.cs:147`, `:174`); ein wöchentliches Angebot fällt aus, wenn am Stichtag keiner den Shop öffnet.
- **Creator:** kein Pfad in diesem Sprint.

Entscheidungen des Vaters in dieser Runde: Sprint „Shop je Kind" statt Sprint 90; B-645 Punkt 12 → **Zielgruppe am
Angebot** (Entscheidung 12); B-665 → alle sechs Empfehlungen (gegrillt und geschätzt, M).

## PM-Synthese & Priorisierung (→ Entwickler)

| Prio | Story | Größe | Wo | Warum jetzt |
| --- | --- | --- | --- | --- |
| P2 | [B-645](backlog/B-645-taschengeld-und-auffuell-plan-fuer-alle-angebote.md) | L | beides | Wunsch aus dem Betrieb; enthält zwei Defekte (Wöchentlich holt nicht nach, Formular ohne Wochentag) |
| P2 | [B-665](backlog/B-665-vater-loest-aus-dem-inventar-des-kindes-ein.md) | M | beides | Wunsch aus dem Betrieb; derselbe Bereich, derselbe Rollengang |
| P2 | [B-672](backlog/B-672-startverlegung-zurueck-holt-unspielbare-tage-zurueck.md) | — | backend | nächste Sitzung (Umgehung: Planstart nicht zurückverlegen) |
| P1 | [B-580](backlog/B-580-plan-aus-unit.md) | L | beides | Sprint 90, danach |

**Roter Faden:** Der Shop gehört jedem Kind einzeln.

## Sprint 96 — Ziel & Umfang

**Sprint-Ziel (Sicht Vater):** „Ich lege für Theodor und Friedrich je ein eigenes monatliches Taschengeld an; jeder
kauft nur aus seinem eigenen Bestand, der sich am 1. von selbst auffüllt – auch wenn keiner reingeschaut hat –, und
wenn ein Kind ohne Handy vor mir steht, löse ich selbst aus seinem Inventar ein und es sieht das hinterher."

**Umfang:** B-645 (L), B-665 (M). Zwei Migrationen, nacheinander in **einer** Kette.

**Entwickler-Brief:**

- **Reihenfolge:** B-645 Backend (Stichtag-Funktion → Schema/Migration → `ShopService` → Vertrag/Controller →
  Client) → B-665 Backend → Frontend beider Stories → E2E.
- **Quelle der Wahrheit:** der Bestand je Kind in `ShopListingStock`; die **Senke** jedes Auffüllens ist eine
  Funktion (gebaut als `ShopService.MaterializeStocksAsync`), die alle vier Auffüll-Stellen (zwei Listen, Kauf, Einzelansicht) rufen, und die
  Startregel (Entscheidung 11) eine zweite neben ihr, die Migration und Laufzeit teilen.
- **Guards:** Wallet-Invariante beim Kauf (`child.ConcurrencyStamp`); Sperre am Kind-Bestand statt am Angebot;
  Zielgruppe filtert **jeden** Sohn-Pfad (Liste und Kauf); Ausstellerbindung beim Einlösen durch den Vater.
- **Migration:** Daten der Söhne – Probelauf auf einer Kopie der echten `pugling.db`, Migrationstest im Altzustand.
- **Testweg:** wie in den Stories; Rollengang Vater **und** Sohn als E2E `taschengeld-monatlich.spec.ts`.
- **Reviewer:** `pugling-reviewer` und `frontend-reviewer`.

## Iteration 96 — umgesetzt

**Backend (API-First).**

- **Stichtage als reine Funktion:** `ShopRefillSchedule` zählt Auffüll-Termine statt „ist heute fällig?“ – Wöchentlich holt
  einen verpassten Montag nach, Monatlich gilt am 1. (UTC). Dazu der nächste Termin, der Periodenbeginn und die Startregel
  (Entscheidung 11).
- **Bestand je Kind:** neue Tabelle `ShopListingStocks` (Angebot × Kind, eigener Concurrency-Stamp) und
  `ShopListingAudiences` (Zielgruppe, Entscheidung 12). `ShopService.MaterializeStocksAsync` ist die **eine Senke**: jede
  Zeile entsteht dort nach der Startregel, jedes Auffüllen läuft dort durch – beide Listen, Kauf, Einzelansicht (nur
  Anzeige) und „Bestand setzen“. Der Kauf sperrt die Zeile des Kindes und zählt weiter den Stamp des Kindes hoch
  (Wallet-Invariante).
- **Vertrag (Bruch, wie geschätzt):** `ShopListingDto` trägt `RefillQuantity`, `ChildIds` und `Stocks[]` statt
  `CurrentStock`/`LastRefilledAtUtc`. Neu sind `PUT …/listings/{id}/stock/{childId}`, `ShopListingResponse.NextRefillAtUtc`,
  `UnitType.Euro`, `ShopRefillKind.Monthly`, `ActivationRequester` und `POST children/{id}/shop/inventory/{articleId}/redeem`
  (B-665). `Pugling.Client` ist nachgezogen.
- **Migration `ShopStockPerChild` (von Hand):** Das Gerüst hatte `CurrentStock` in `RefillQuantity` umbenannt, was den
  Bestand weggeworfen hätte. Die Migration legt je Angebot und verknüpftem Kind einen Startbestand nach Entscheidung 11 an:
  `MaxStock` minus die eigenen, nicht stornierten Käufe seit dem letzten Auffüllen. Sie setzt `RefillQuantity = MaxStock`
  (verhält sich wie heute) und `RequestedBy = Child`. Den Fehler in der Reihenfolge (`MaxStock = 0` zuerst angehoben, danach
  griff „Auffüll-Art → None“ nicht mehr) fand der Migrationstest, bevor etwas eingespielt war.

**Frontend.**

- **Vater-Web:** Auffüllen mit Wochentag, Datum und Monatlich, „Obergrenze je Kind“ und „+ je Termin“ (Ansparen). Die
  Zielgruppe heißt „Für“ (leer = alle). Der Bestand steht je Kind in der Tabelle, mit „füllen“ je Kind. Im Inventar
  steht ein unauffälliges „Selbst einlösen…“ mit Bestätigung.
- **Sohn-App:** Ein leeres Angebot zeigt „wieder da: …“, eine Einlösung durch den Vater heißt „Papa hat für dich
  eingelöst“, und es gibt Euro als Einheit.

**Vorfall – echte DB migriert, bevor der Sprint fertig war.** Um 08:17:50 startete der Heimnetz-Server neu (Sicherung
`pugling.db.bak-auto-20261119-081750`). Er baute den ungecommitteten Arbeitsstand und spielte `ExerciseFeedback` **und**
`ShopStockPerChild` in die echte `pugling.db` ein. Ausgelöst hat das nicht diese Sitzung. Der eingespielte Stand ist der
nach der Korrektur der Reihenfolge. Gegen die Sicherung geprüft (an einer Kopie): Jedes Kind hat den Startbestand nach
Entscheidung 11. Beispiel „60 Minuten Zocken“: Friedrich hat seit Montag zweimal gekauft und steht auf 0, Theodor und das
Testkind stehen auf 1. Alle sechs Anträge stehen auf `Child`. Folge: **Die Migrationsdatei ist ab jetzt eingefroren**,
jede Korrektur geht als neue Migration hinten an die Kette.

## Review Sprint 96

`pugling-reviewer` und `frontend-reviewer`, eingearbeitet:

- 🟡 **Einmalig gab Geschwistern verschiedene Bestände.** Eine Zeile, die vor dem Termin entstand, füllte um die Menge auf
  (4), eine danach angelegte startete bei der Obergrenze (8). Jetzt füllt Einmalig immer bis zur Obergrenze, mit neuem Test
  `Einmalig_Gibt_Geschwistern_Denselben_Bestand_Egal_Wann_Gelesen`.
- 🟢 **PATCH der Obergrenze allein** machte aus einem Spar-Angebot still ein verfallendes (Menge auf die neue Obergrenze
  gekappt). Jetzt folgt nur „verfällt“, und eine Obergrenze unter der Spar-Menge antwortet 400.
- 🟢 **Negative Menge** wurde bei „Kein Auffüllen“ gespeichert, jetzt antwortet der Server 400.
- 🟢 **„Bestand setzen“** hatte eine Meldung für zwei Ursachen, jetzt sind es zwei (Spanne bzw. „nicht für dieses Kind“).
- 🟡 **„Für“-Legende:** Ein `<label for>` in der Legende hakte beim Klick das erste Kind an. Jetzt steht dort eine
  Überschrift (`group-label` + `InfoHint`).
- 🟡 **Obergrenze tippen:** Beim Tippen von „12“ über „1“ wurde aus Ansparen kurz Verfallen. Jetzt gibt es einen eigenen
  Merker `expires`.
- 🟡 **Prüfung beim Bearbeiten:** Sie lief über eine eigene Fehlerbox, jetzt über das Banner des Panels. „Einmalig“ ohne
  Datum sperrt Anlegen bzw. OK, statt eine Meldung zu zeigen (B-233).
- 🟡 **Einlöse-Knopf:** Er trägt jetzt den Artikel im Namen und setzt Autofokus aufs Mengenfeld; die Ids laufen über
  `useId`.
- 🟡 **E2E:** trägt jetzt AK 6 ganz – Einmalig mit Datum, Bearbeiten des Wochentags, „+4 bis 8“ in der Zeile, den Knopf
  „füllen“. Die Zusicherung, die nichts trug (`zeileA` enthält A), ist weg.

Bewusst offen (nicht blockierend):

- Der Fokus kehrt nach „Abbrechen“ beim Einlösen nicht zum Knopf zurück.
- Die Sohn-App zeigt bei täglich/2× täglich nur das Datum, keine Uhrzeit.
- „Papa hat für dich eingelöst“ steht auch, wenn Mama oder Oma einlösen; das folgt dem bestehenden Wortlaut „Anfrage an
  Papa“.
- Ein Kommentar in der eingefrorenen Migration ist ungenau („never fired“).
- Produktfrage an den Vater: Ein neues Spar-Angebot (+4 bis 8) ist am ersten Tag achtmal kaufbar, weil die Startregel die
  Obergrenze vorgibt (Entscheidung 11 wörtlich).

## Runde 2 — Rollengang und Abnahme

Der Rollengang lief nach der letzten Bearbeitung gegen einen frischen Server auf einer Wegwerf-DB (Ausweichports
5211/5181, weil der Heimnetz-Server auf :5200 läuft). Die Spec `taschengeld-monatlich.spec.ts` ist der Gang von Vater und
Sohn. Zusammen mit den Regressionszeugen `shop-verlauf`, `shop-verlauf-wiederholen`, `full-flow`, `rueckmeldung` und
`feldhilfe`: **8 passed**.

- **Vater — abgenommen, bis auf eine Bestätigung.** Was der Gang zeigt:
  - „Taschengeld A“ legt der Vater monatlich mit +4 bis 8 nur für A an; die Zeile zeigt „Monatlich (am 1.), +4 bis 8“
    und den Bestand je Kind.
  - Ein Wochen-Angebot mit Wochentag lässt sich anlegen und auf Donnerstag umstellen; ein Einmal-Angebot mit Datum
    ebenfalls.
  - „füllen“ füllt genau A's Bestand.
  - Aus A's Inventar löst der Vater selbst ein, und der Antrag steht als „selbst eingelöst“ da.
  - **Offen:** die Bestätigung von Entscheidung 11 (B-645 bleibt bis dahin `in-arbeit`). Dazu muss der Heimnetz-Server
    neu starten, denn er läuft noch mit dem Stand von 08:17, also vor den Review-Korrekturen.
- **Sohn — abgenommen.** Was der Gang zeigt:
  - A kauft das gemeinsame Angebot und sein Taschengeld leer und sieht „ausverkauft · wieder da: …“.
  - B sieht A's Taschengeld gar nicht; das gemeinsame Angebot bleibt für B kaufbar.
  - A sieht „Papa hat für dich eingelöst“.
  - Gerätecheck offen: ob „wieder da“ auf der Karte am Handy Platz hat.
- **Creator — Regressionszeuge.** Kein Creator-Pfad im Diff. `rueckmeldung.spec.ts` (Creator-Zählung) ist grün,
  ebenso die volle Suite.

**Verifizierte Qualität nach der letzten Bearbeitung:**

- `dotnet test Pugling.sln -c Release`: 1920/1920
- Vitest: 1258/1258
- `tsc -b`: sauber
- `dotnet format`: ohne Fund an den geänderten Dateien
- Mutanten: 32/32 wie erwartet; die Tabelle steht im Verlauf von B-645 und B-665
- E2E: 8 passed

## Retrospektive

**Nachschau:** Sprint 93 (B-666, B-667, B-668, B-669 über `f5b04d26`) ist ohne Befund. Sprint 88 (B-628, B-629 über
`796314db`/`8f3d12db`) hat einen Durchrutscher:
[B-685](backlog/B-685-abgebrochener-test-sperrt-rueckmeldung-im-ueben.md) (`entgangen_bei: [B-628]`). Ein mit „Später“
verlassener Test sperrt die Rückmeldung im Üben für den ganzen Tag. `nachgeschaut: 2026-11-19` steht an allen sechs
Stories. Index: **505 von 534**.

- **Was die eigenen Tore durchließen:** Die echte DB wurde mitten im Sprint migriert, weil der Heimnetz-Start den
  Arbeitsstand baut, wie er ist. Kein Tor sah das; aufgefallen ist es nur, weil der Probelauf auf einer Kopie die neuen
  Spalten schon vorfand. Ausgegangen ist es gut, weil der Migrationstest den Fehler in der Reihenfolge *vor* dem
  Heimnetz-Start gefunden hatte. Zwanzig Minuten früher hätte eine fehlerhafte Migration die Bestände der Söhne
  umgeschrieben, und danach hätte nur noch eine Korrektur-Migration geholfen.
- **Von außen kam:** nichts. Die Review-Funde (Einmalig bei Geschwistern, „Für“-Legende) fanden die Reviewer, der
  Durchrutscher von Sprint 88 kam aus der Nachschau.
- **Der eine Mechanismus (neues Tor):** `scripts/heimnetz.ps1` startet gegen die echte `pugling.db` nicht mehr, solange
  unter `backend/Pugling.Api/Data/Migrations` etwas ungecommittet ist. Die Meldung nennt die Dateien und den Ausweg
  „committen“; bewusst trotzdem geht es mit `-AllowUncommittedMigrations`. Gemessen:
  - Parser ohne Fehler;
  - der Prüfausdruck ist mit einer unversionierten Probedatei im Ordner rot und ohne sie grün.
  - Nicht gemessen ist der ganze Skriptlauf. Ein Start hätte einen zweiten Server auf die echte DB gesetzt.
  - Dokumentiert in `docs/heimnetz.md`.
- **Bewusst ungedeckt:** Ein Start mit einer *committeten*, aber noch nicht abgenommenen Migration (Sprint auf `main`,
  Abnahme offen) bleibt möglich. Dagegen hilft nur die Reihenfolge „erst abnehmen, dann committen“, die die Projektregel
  „Commits selbst setzen“ ohnehin vorsieht.
- **Der Lauf hält hier**, weil die Retro ein neues Tor gebracht hat (README → „Der Backlog-Lauf“). Der Nutzer sieht es
  zuerst.

## Offene Roadmap (priorisiert)

Die Liste lebt in [docs/backlog/](backlog/INDEX.md). Reihenfolge:

1. B-645 abnehmen, sobald der Vater Entscheidung 11 bestätigt hat (vorher den Heimnetz-Server neu starten).
2. [B-685](backlog/B-685-abgebrochener-test-sperrt-rueckmeldung-im-ueben.md) (P2, Defekt, braucht eine Entscheidung).
3. [B-672](backlog/B-672-startverlegung-zurueck-holt-unspielbare-tage-zurueck.md) (P2, Defekt).
4. Sprint 90 ([B-580](backlog/B-580-plan-aus-unit.md)). Die Leitbild-Stories B-680 bis B-684 der parallelen Sitzung sind
   noch nicht eingeordnet.

## Konkreter Änderungsstand (für Review)

- `08fd05a6`: Sprint 96 (Backend, Migration `ShopStockPerChild`, Vater-Web, Sohn-App, Tests, E2E).
- Abschluss-Commit: Abnahme B-665, Nachschau, B-685, Tor in `heimnetz.ps1`.
- Nichts ist gepusht.
