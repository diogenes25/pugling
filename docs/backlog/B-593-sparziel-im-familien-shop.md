---
tags: [typ/story, status/ausformuliert, bereich/gamification, bereich/shop, rolle/student]
aliases: [Sparziel, Wunsch-Artikel anheften]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Marktvergleich Belohnungs-Apps (Sitzung 2026-11-16, Quick-Win 2)
grund: ""
ersetzt_durch: []
---

# B-593 · Sparziel im Familien-Shop mit Fortschritt in Pflicht-Tagen

Das Kind heftet einen Artikel aus dem Familien-Shop als Sparziel an und sieht in der Arcade dauerhaft, wie weit es noch
ist – in Münzen und übersetzt in Pflicht-Tage („noch 140 Münzen ≈ 4 Tage Pflicht"). Das ist der stärkste Mechanismus der
Taschengeld-Apps (Greenlight, BusyKid) und setzt dort an, wo die Münzen Friedrich nicht erreicht haben
([B-535](B-535-belohnungen-erreichen-friedrich-nicht.md)): der Lohn wird zu einem sichtbaren, selbst gewählten Ziel statt
zu einem abstrakten Kontostand.

**Diese Story ändert keine Münz-Regel.** Sie zeigt nur an, was das Kind schon verdient und was ein Artikel kostet. Wo die
Umrechnung in „Pflicht-Tage" eine Annahme über die Münz-Ökonomie braucht, steht das unten als offener Punkt.

## User Story

Als **Schüler** möchte ich einen Artikel aus dem Shop als mein Ziel anheften und immer sehen, wie viele Münzen und wie
viele Lerntage mir noch fehlen, damit ich weiß, wofür ich lerne.

## Ist-Stand am Code

**Der Shop und der Kontostand:**

- Shop-Ansicht des Kindes: `GET me/shop` (`backend/Pugling.Api/Controllers/Student/MeController.cs:265`, Aufbau
  `ShopViewAsync` `:401`). Sie liefert die Angebote **aller** Betreuer des Kindes (`:406-407`) mit `Affordable`,
  das nur `Münzen ≥ Preis`, `Gems ≥ Preis` **und eigenen Bestand > 0** prüft (`:419`).
- Vertrag `ShopListingResponse(…, CoinPrice, GemPrice, …, CurrentStock, Affordable, NextRefillAtUtc)`
  (`backend/Pugling.Contracts/Student/MeDtos.cs:28-32`). Ein Angebot kann in Münzen **und/oder** Gems bepreist sein
  (`Models/GamificationEntities.cs:195-198`).
- Der Kontostand kommt aus `WalletService.BalancesAsync` (`Services/Shared/WalletService.cs:22`), geteilt über alle
  Betreuer; Schulden sind erlaubt, der Stand kann negativ sein (`PositionProgressService.cs:746-754`).
- Der Kauf ist ausstellergebunden und über `child.ConcurrencyStamp` serialisiert (`Services/Shared/ShopService.cs:182`,
  `Models/AdminEntities.cs:131`).
- Oberfläche: Karten mit Preis-Pille „gold/rot" je nach `affordable` (`frontend/src/sohn/SohnShop.tsx:236-249`);
  Kontostand-Chip oben (`SohnShop.tsx:173`).

**Was einem Sparziel nahe kommt, aber keines ist:**

- Der Bestand eines Angebots kann leer sein und füllt sich später auf; die Karte nennt dann das Datum („Sparen lohnt sich",
  `SohnShop.tsx:244-249`, B-645).
- `GET me/points/week` liefert die **verdienten** Münzen der Woche und der Vorwoche (`MeController.cs:45`,
  `Services/Shared/WalletService.cs:56-72`, `WeekCoinsResponse` `MeDtos.cs:19`); ohne Käufe, Strafen und Gems, **keine
  Vergleiche mit Geschwistern** (Nutzerentscheidung G-6, `MeController.cs:38-40`). Das ist die einzige vorhandene
  Verdienst-Kennzahl.

**Nicht gefunden (belegt durch Suche in `Models/`, `Contracts/`, `frontend/src`):**

- Kein Sparziel, keine Wunschliste, kein „angeheftet" am Kind oder Angebot (Treffer zu „Wunsch/Favorit" gehören zu den
  Interessen-Tags, `Models/InterestEntities.cs`, nicht zum Shop).
- Keine Kennzahl „Münzen je Pflicht-Tag". Der Tagessatz ist nicht als Feld vorhanden; ableitbar nur aus den Zielpunkten der
  Pflichtpositionen (`PlanPosition.PointsGoalMet`, `Models/PlanPositionEntities.cs:84`, im Vertrag
  `PositionStatus.PointsGoalMet`, `Contracts/Shared/ProgressDtos.cs:47`) plus Kiste (`DailyBoxService`) oder aus
  `WeekCoinsResponse`.
- Keine Anzeige des Sparziels in `SohnHome.tsx`.

## Die echte Lücke

Es fehlt nicht der Shop, sondern ein **gespeicherter Zielzeiger** (welches Angebot will dieses Kind) und eine
**ehrliche Umrechnung** Münzen → Tage. Der Zeiger ist klein (ein Verweis am Kind); die Umrechnung ist das Heikle: Der
Tagessatz hängt von Plänen, Kiste, Bonus und Streak ab und schwankt, jede „≈ 4 Tage" ist eine Prognose. Dazu kommt, dass
das Ziel an ein **Angebot** eines bestimmten Betreuers hängt, das der Vater ändern, deaktivieren oder löschen kann.

## Offene Punkte

1. **Was ist ein „Pflicht-Tag" in Münzen?** Empfehlung: **die tatsächlich verdienten Münzen der letzten 7 Tage geteilt durch
   7** (`WeekCoinsResponse`-Quelle, keine Annahme über Pläne), als Näherung gekennzeichnet („etwa"). Eine Berechnung aus
   den Plan-Zielpunkten wäre genauer für heute, aber falsch, sobald der Vater die Pläne umbaut. Welche Münzarten
   zählen (`EarnedCoinKinds`, `WalletService.cs:47`), ist damit bereits entschieden.
2. **Anfangsphase ohne Verdienst:** Bei weniger als sieben Tagen Verlauf oder null verdienten Münzen: keine Tage anzeigen,
   nur Münzen. Empfehlung: ja, statt „∞ Tage" oder einer geschönten Zahl.
3. **Ein Ziel oder mehrere?** Empfehlung: **genau eines** – ein Ziel ist ein Fokus, eine Liste ist ein Wunschzettel.
4. **Wo steht das Ziel?** Empfehlung: ein Streifen auf der Startseite und die gepinnte Karte im Shop, nicht im Lernfluss
   (kein Ablenken beim Üben; die UI-Konventionen in `frontend/CLAUDE.md` vor dem Bau gegenlesen, hier nicht geprüft).
5. **Speicherort:** am Kind (`Child.SavingsGoalListingId`, nullable) oder eigene Tabelle? Empfehlung: nullable Spalte am
   `Child` mit `OnDelete SetNull`; Migration additiv an die Kette (echte DB der Söhne). Der Schlüssel zeigt auf die
   **Listing-Id**, nicht auf eine Kopie des Preises: ein Preis, den der Vater ändert, muss durchschlagen.
6. **Wenn das Angebot verschwindet oder deaktiviert wird:** Ziel still entfernen oder sichtbar „nicht mehr
   verfügbar"? Empfehlung: sichtbar mit Hinweis, damit das Kind nicht ohne Erklärung ein Ziel verliert; der Vater, der
   den Preis anhebt, hebt damit auch die Latte (Motivationsrisiko, aber seine Entscheidung).
7. **Gems und Münzen:** bei Gem-Preis oder Mischpreis: nur den Münzanteil in Tage umrechnen, den Gem-Anteil als Zahl
   zeigen. Empfehlung: ja; Gems hängen an Boni, nicht an Pflicht-Tagen.
8. **Bestand 0 / eigener Bestand leer:** das Ziel bleibt, die Anzeige nennt das Auffülldatum (`NextRefillAtUtc`).
9. **Schuldenstand:** bei negativem Kontostand „noch X Münzen" ab dem Minus oder ab null? Empfehlung: ab dem echten
   Stand (inklusive Schulden), weil ein Ziel, das die Strafe verschweigt, unehrlich wäre.
10. **Berührt die Münz-Ökonomie (Entscheidung des Nutzers, nicht hier):** Solange das Ziel nur liest, ändert sich nichts. Zwei
    Erweiterungen wären Ökonomie und gehören **nicht** in diese Story: Sparbonus/Zinsen auf gespartes Ziel,
    Vorab-Reservierung („Münzen sperren"), und ein Eltern-Zuschuss zum Ziel. Empfehlung: ausdrücklich ausgeschlossen
    lassen. Zu klären ist nur, ob eine Reservierung je gewollt ist: sie würde das Wallet-Prinzip „jeder
    abbuchende Pfad bumpt den ConcurrencyStamp" betreffen.
11. **Zusammenhang mit Leitbild Privatlehrer** ([Leitbild](../leitbild-privatlehrer.md), B-681: „der Vater belohnt"): Das
    Sparziel unterstützt die dort gestärkte Belohnungsseite. Empfehlung: unabhängig davon bauen.

## Entscheidungen

Noch keine – Stufe `ausformuliert`.

## Akzeptanzkriterien (Entwurf)

Je Kriterium: welche Änderung macht es rot.

1. Das Kind setzt über einen Aufruf ein Angebot aus **seiner** Shop-Ansicht als Ziel; ein Angebot eines Betreuers, der nicht
   sein Betreuer ist, liefert 404. Rot, wenn die Prüfung nur auf Existenz statt auf `ListingsForStudentAsync` geht.
2. Die Antwort nennt `fehlendeMuenzen = max(0, Preis − Kontostand)` (bei Schulden vom echten Stand). Rot, wenn mit 0 als
   Boden gerechnet wird oder die Schuld fehlt.
3. `Tage` ist `null` bei null verdienten Münzen der letzten sieben Tage und sonst `aufrunden(fehlend / Schnitt)`.
   Rot, wenn Division durch null auftreten kann oder der Schnitt einen anderen Zeitraum nimmt.
4. Setzt der Vater den Preis des Angebots herauf, zeigt die nächste Abfrage den neuen Fehlbetrag. Rot, wenn der Preis am Kind
   kopiert wurde.
5. Wird das Angebot deaktiviert oder gelöscht, bleibt das Ziel mit `verfuegbar: false`, sonst ist es still weg. Rot, wenn
   `SetNull` das Ziel ohne Kennzeichen löscht.
6. Ein zweites Setzen ersetzt das erste (genau ein Ziel). Rot, wenn zwei Ziele möglich sind.
7. Das Setzen ändert weder Kontostand noch Ledger noch `ConcurrencyStamp`. Rot, sobald ein `ChildPointsEntry`
   entsteht (Regressionstest: Kontostand vor/nach).
8. Ein Supervisor liest das Ziel seines Kindes, setzt es aber nicht an dessen Stelle (wie bei der Rückmeldung,
   `ExerciseFeedbackController.cs:39-46`). Rot, wenn `User.IsStudent()` nicht geprüft wird.
9. Die Sohn-App zeigt Ziel, fehlende Münzen und Tage auf der Startseite; ein Kind ohne Ziel sieht den Streifen nicht.
   Rot, wenn der Streifen ohne Ziel erscheint.

## Verlauf

- **2026-11-16** — angelegt aus dem Marktvergleich Belohnungs-Apps (Quick-Win 2).
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Ist-Stand gegen den Code; Shop und Verdienst-Kennzahl bestehen
  (`GET me/shop`, `GET me/points/week`), ein Zielzeiger und die Umrechnung in Tage fehlen; elf offene Punkte, darunter
  die Ökonomie-Grenze, mit Empfehlung. Keine Entscheidung gefällt.
