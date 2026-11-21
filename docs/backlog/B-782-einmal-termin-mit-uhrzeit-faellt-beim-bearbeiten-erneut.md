---
tags: [typ/story, status/abgenommen, bereich/shop, rolle/supervisor]
aliases: [Einmal-Termin mit Uhrzeit füllt erneut]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Sprint 100 (a0b73724) im Nachtlauf 2026-11-21, docs/pm-sitzung-2026-11-21.md"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-776]
---

# B-782 · Ein „einmalig“-Termin mit Uhrzeit füllt beim Bearbeiten im Vater-Web noch einmal auf

Seit B-776 leert ein **geänderter** Termin an einem „einmalig“-Angebot den Auffüll-Zeitpunkt aller Bestände
(`ShopController.UpdateListing`, `resetsOnceInstant`, Vergleich `nextRefillAt != listing.RefillAtUtc`). Das Vater-Web
schickt beim Speichern immer `<Datum>T00:00:00Z` (`frontend/src/vater/VaterShop.tsx:228`) und liest den Termin auf das
Datum zugeschnitten ein (`VaterShop.tsx:423`).

Legt ein API-Aufrufer (Agent, Skript) ein „einmalig“-Angebot mit Uhrzeit an, zum Beispiel `2026-11-12T10:00:00Z`, und
der Vater ändert später im Vater-Web nur den Preis, dann geht `2026-11-12T00:00:00Z` zurück. Das ist ein anderer Wert,
also leert der PATCH die Zeitpunkte, und jedes Kind wird noch einmal aufgefüllt, weil Mitternacht schon vorbei ist.
Derselbe Effekt entsteht bei einem Termin, der mit einem anderen Offset als `Z` geschickt wurde (Review-Hinweis 4 von
Sprint 100).

Am Code nachgelesen, nicht im Browser gesehen. Heute schreibt nur das Vater-Web `RefillAtUtc`: gegrept in `Seed.cs`,
`Pugling.Agent.*`, `scripts/` und `frontend/src`. Der Pfad braucht darum einen fremden Aufrufer.

Mögliche Reparatur (zu entscheiden): den Termin serverseitig auf UTC-Mitternacht normalisieren (er ist fachlich ein
Tag), oder im Vater-Web den geladenen Wert unverändert zurückschicken, solange das Datum nicht geändert wurde. Rote
Probe: POST „einmalig“ mit `T10:00:00Z`, Termin vorbei, ausverkauft, dann PATCH nur `coinPrice` mit
`refillAtUtc = <Datum>T00:00:00Z`. Erwartet ist Bestand 0, gemessen würde vermutlich `MaxStock`.

## User Story

Als Vater möchte ich bei einem „einmalig“-Angebot nur den Preis ändern können, ohne dass dabei jedes Kind noch einmal
aufgefüllt wird.

## Ist-Stand am Code

- Server: `ShopController.UpdateListing` (`backend/Pugling.Api/Controllers/Supervisor/ShopController.cs:279`) nimmt
  `dto.RefillAtUtc ?? listing.RefillAtUtc`; `resetsOnceInstant` (`:301-302`) vergleicht den **Zeitpunkt** mit dem
  gespeicherten. Ein anderer Zeitpunkt leert die Auffüll-Zeitpunkte aller Bestände (`:326`). Das ist für einen
  ausdrücklich neuen Termin richtig (B-776).
- Vater-Web: `ListingRow` lädt den Termin abgeschnitten (`refillDate: listing.refillAtUtc?.slice(0, 10)`,
  `frontend/src/vater/VaterShop.tsx:423`), `planDto` schickt `${refillDate}T00:00:00Z` (`:228`). Ein gespeicherter
  Termin mit Uhrzeit oder mit Nicht-UTC-Zeitpunkt kommt darum **verändert** zurück, auch wenn der Vater das Datum
  nicht angefasst hat.
- Schreiber von `RefillAtUtc` heute: nur das Vater-Web (gegrept in `Seed.cs`, `Pugling.Agent.*`, `scripts/`,
  `frontend/src`). Der Pfad braucht also einen API-Aufrufer mit Uhrzeit; die API lässt ihn zu.

## Offene Punkte

1. ~~Server normalisieren oder Client-Rundweg?~~ Entschieden: Entscheidung 1.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-22 (Defekt, autonom grillbar), unbestätigt.*

1. **Der Client schickt den geladenen Termin unverändert zurück, solange das Datum im Feld gleich geblieben ist.**
   Die Regel sitzt in `planDto` (der einen Stelle, an der das Vater-Web den Termin für den Server baut; Anlegen und
   Bearbeiten laufen beide hindurch). Begründung: Der Server vergleicht richtig – ein anderer Zeitpunkt *ist* ein
   anderer Termin; falsch ist, dass der Client einen Wert ändert, den der Vater nicht geändert hat. Verworfen:
   (a) serverseitig auf UTC-Mitternacht normalisieren – das verschöbe den Termin eines API-Aufrufers still um bis zu
   23 Stunden und ändert Vertragsverhalten für einen Fall, den nur ein Client erzeugt; (b) serverseitig nur das Datum
   vergleichen – dann wäre eine absichtliche Uhrzeit-Änderung am selben Tag keine Änderung. Kosten: ein anderer Client
   mit demselben Rundweg-Fehler hätte denselben Defekt; der Server schützt nicht davor.
2. **Ein geändertes Datum geht weiter als `<Datum>T00:00:00Z`.** Das bleibt die Bedeutung des Datumsfelds.
3. **Die Anzeige des Datums bleibt `slice(0, 10)`** (UTC-Datum). Ein Termin `23:00Z` erscheint also mit dem
   UTC-Datum. Das ist ein Anzeige-Thema, kein Teil dieses Defekts.

## Akzeptanzkriterien

1. Ein Angebot „einmalig“ mit `refillAtUtc = 2026-11-12T10:00:00Z`: im Vater-Web bearbeiten, nur den Preis ändern,
   OK → der PATCH trägt den Termin, wie die Liste ihn geliefert hat (`"2026-11-12T10:00:00"`, aus SQLite ohne Offset). Rot macht es: `planDto` baut weiter
   `${refillDate}T00:00:00Z` (Ausgangszustand: `2026-11-12T00:00:00Z`).
2. Dasselbe Angebot, Datum auf `2026-11-20` geändert → `refillAtUtc = "2026-11-20T00:00:00Z"`. Rot macht es: der
   geladene Wert wird immer zurückgeschickt.
3. Ein Angebot „wöchentlich“ ohne Termin, auf „einmalig“ mit Datum umgestellt → `<Datum>T00:00:00Z` (kein geladener
   Wert, der gewinnen könnte). Rot macht es: der Vergleich nimmt einen leeren geladenen Wert als „gleich“.
4. Server-Rundweg (Integrationstest): „einmalig“ mit `T10:00:00Z`, Termin vorbei, ausverkauft; PATCH nur `coinPrice`
   mit dem **unverändert zurückgeschickten** Termin → Bestand bleibt 0. Festschreibung, im Ausgangszustand grün; sie
   hält fest, dass der Rundweg des Clients am Server tatsächlich nichts auslöst. Rot macht es: `resetsOnceInstant`
   vergleicht etwas anderes als den Zeitpunkt (etwa `DateTimeKind`).

## Schätzung

**Größe: S** – eine Bedingung in `planDto` und ihr Aufrufer, ein Vitest an der Zeile, ein Integrationstest.

- **`wo: frontend`** – Produktcode nur in `VaterShop.tsx`; der Server bekommt nur die Festschreibung (AK 4).
- **`migration: nein`**, **`vertragsbruch: nein`**.

### Angriffsplan

1. AK 4 als Integrationstest neben den B-776-Fällen (grün im Ausgangszustand).
2. `planDto(plan, loadedRefillAtUtc)`: bei „Once“ und `refillDate === loaded?.slice(0, 10)` den geladenen Wert.
3. `ListingRow` reicht `listing.refillAtUtc` durch; das Anlegen-Formular nichts.
4. `ListingRow` für den Test exportieren; Vitest AK 1–3 (rot vor Schritt 2).

### Testweg

- AK 1–3: `frontend/src/vater/VaterShopEinmalTermin.test.tsx`, `ListingRow` in einer Tabelle rendern, „Bearbeiten“,
  Preis ändern, OK, `onSave`-Aufruf prüfen.
- AK 4: `backend/Pugling.Api.Tests` beim B-776-Test.

## Verlauf

- **2026-11-21** — angelegt aus der Nachschau von Sprint 100 (`idee`, `entgangen_bei: [B-776]`).
- **2026-11-22** — ausformuliert, gegrillt (stellvertretend, Defekt, autonom) und geschätzt im Nachtlauf A: S, frontend; Rundweg im Client statt Normalisierung am Server.
- **2026-11-22** — in-arbeit (Nachtlauf A, Sprint 111). `planDto(plan, loadedRefillAtUtc)` in `VaterShop.tsx`, `ListingRow` reicht `listing.refillAtUtc` durch und ist für den Test exportiert.
  - AK 1 → `VaterShopEinmalTermin.test.tsx` „schickt den geladenen Termin unverändert“, M1 (nie geladen) → rot, M4 (Aufrufer ohne Termin) → rot; E2E `shop-einmal-termin-bearbeiten.spec.ts`, E2 (= M4) → rot.
  - AK 2 → „schickt ein geändertes Datum als Mitternacht UTC“, M2 (immer geladen) → rot.
  - AK 3 → „Umstellung von wöchentlich auf einmalig“, M3 (leer gilt als gleich) → rot.
  - AK 4 → `ShopStockPerChildTests.B782_Termin_Mit_Uhrzeit_Unveraendert_Zurueck_Fuellt_Nicht_Erneut` (true → Bestand 0, false → 1), M5 (Server vergleicht nur das Datum) → rot. Rote Probe der Ursache: Mitternacht zurück → Bestand 1 (erwartet 0 nach dem Fix), gemessen im `false`-Fall.
  - Messung 00:19 nach der letzten Produktänderung: M1–M5 rot, Grundlinie grün. AK-1-Wortlaut an das Format der Liste angepasst.
- **2026-11-22** — Review `frontend-reviewer`: keine 🟡/🔴, Zähler 0. 🟢 1 und 2 (alter Termin beim Wechsel auf „einmalig“; „Abbrechen“ setzt das Formular nicht zurück) sind älter als B-782 → [B-786](B-786-shop-bearbeiten-uebernimmt-alten-termin.md); 🟢 3 (AK-Wortlaut) übernommen.
- **2026-11-22** — abgenommen (`fbf1bd94`, Sprint 111): Rollengang Vater per E2E `shop-einmal-termin-bearbeiten` gegen frisch gestarteten Server (5201/5174); Sohn Regressionszeuge (`shop-verlauf`, `full-flow`), Creator ohne Fläche im Diff. Backend 2021/2021 (Release), Vitest 1313/1313.
