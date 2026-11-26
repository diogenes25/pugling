---
tags: [typ/story, status/abgenommen, bereich/gamification, bereich/shop, rolle/student]
aliases: [Sparziel, Wunsch-Artikel anheften]
status: abgenommen
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: Marktvergleich Belohnungs-Apps (Sitzung 2026-11-16, Quick-Win 2)
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-26"
---

# B-593 · Sparziel im Familien-Shop mit Fortschritt in Pflicht-Tagen

Das Kind heftet einen Artikel aus dem Familien-Shop als Sparziel an und sieht in der Arcade dauerhaft, wie weit es noch
ist – in Münzen und übersetzt in Pflicht-Tage („noch 140 Münzen ≈ 4 Tage Pflicht"). Das ist der stärkste Mechanismus der
Taschengeld-Apps (Greenlight, BusyKid) und setzt dort an, wo die Münzen Friedrich nicht erreicht haben
([B-535](B-535-belohnungen-erreichen-friedrich-nicht.md)): der Lohn wird zu einem sichtbaren, selbst gewählten Ziel statt
zu einem abstrakten Kontostand.

**Diese Story ändert keine Münz-Regel.** Sie zeigt nur an, was das Kind schon verdient und was ein Artikel kostet. Wo die
Umrechnung in „Pflicht-Tage" eine Annahme über die Münz-Ökonomie braucht, steht das unten als Entscheidung 1.

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

1. ~~**Was ist ein „Pflicht-Tag" in Münzen?** Empfehlung: **die tatsächlich verdienten Münzen der letzten 7 Tage geteilt durch
   7** (`WeekCoinsResponse`-Quelle, keine Annahme über Pläne), als Näherung gekennzeichnet („etwa"). Eine Berechnung aus
   den Plan-Zielpunkten wäre genauer für heute, aber falsch, sobald der Vater die Pläne umbaut. Welche Münzarten
   zählen (`EarnedCoinKinds`, `WalletService.cs:47`), ist damit bereits entschieden.~~ → Entscheidung 1.
2. ~~**Anfangsphase ohne Verdienst:** Bei weniger als sieben Tagen Verlauf oder null verdienten Münzen: keine Tage anzeigen,
   nur Münzen. Empfehlung: ja, statt „∞ Tage" oder einer geschönten Zahl.~~ → Entscheidung 2.
3. ~~**Ein Ziel oder mehrere?** Empfehlung: **genau eines** – ein Ziel ist ein Fokus, eine Liste ist ein Wunschzettel.~~ → Entscheidung 3.
4. ~~**Wo steht das Ziel?** Empfehlung: ein Streifen auf der Startseite und die gepinnte Karte im Shop, nicht im Lernfluss
   (kein Ablenken beim Üben; die UI-Konventionen in `frontend/CLAUDE.md` vor dem Bau gegenlesen, hier nicht geprüft).~~ → Entscheidung 4.
5. ~~**Speicherort:** am Kind (`Child.SavingsGoalListingId`, nullable) oder eigene Tabelle? Empfehlung: nullable Spalte am
   `Child` mit `OnDelete SetNull`; Migration additiv an die Kette (echte DB der Söhne). Der Schlüssel zeigt auf die
   **Listing-Id**, nicht auf eine Kopie des Preises: ein Preis, den der Vater ändert, muss durchschlagen.~~ → Entscheidung 5.
6. ~~**Wenn das Angebot verschwindet oder deaktiviert wird:** Ziel still entfernen oder sichtbar „nicht mehr
   verfügbar"? Empfehlung: sichtbar mit Hinweis, damit das Kind nicht ohne Erklärung ein Ziel verliert; der Vater, der
   den Preis anhebt, hebt damit auch die Latte (Motivationsrisiko, aber seine Entscheidung).~~ → Entscheidung 6.
7. ~~**Gems und Münzen:** bei Gem-Preis oder Mischpreis: nur den Münzanteil in Tage umrechnen, den Gem-Anteil als Zahl
   zeigen. Empfehlung: ja; Gems hängen an Boni, nicht an Pflicht-Tagen.~~ → Entscheidung 7.
8. ~~**Bestand 0 / eigener Bestand leer:** das Ziel bleibt, die Anzeige nennt das Auffülldatum (`NextRefillAtUtc`).~~ → Entscheidung 8.
9. ~~**Schuldenstand:** bei negativem Kontostand „noch X Münzen" ab dem Minus oder ab null? Empfehlung: ab dem echten
   Stand (inklusive Schulden), weil ein Ziel, das die Strafe verschweigt, unehrlich wäre.~~ → Entscheidung 9.
10. ~~**Berührt die Münz-Ökonomie (Entscheidung des Nutzers, nicht hier):** Solange das Ziel nur liest, ändert sich nichts. Zwei
    Erweiterungen wären Ökonomie und gehören **nicht** in diese Story: Sparbonus/Zinsen auf gespartes Ziel,
    Vorab-Reservierung („Münzen sperren"), und ein Eltern-Zuschuss zum Ziel. Empfehlung: ausdrücklich ausgeschlossen
    lassen. Zu klären ist nur, ob eine Reservierung je gewollt ist: sie würde das Wallet-Prinzip „jeder
    abbuchende Pfad bumpt den ConcurrencyStamp" betreffen.~~ → Entscheidung 10.
11. ~~**Zusammenhang mit Leitbild Privatlehrer** ([Leitbild](../leitbild-privatlehrer.md), B-681: „der Vater belohnt"): Das
    Sparziel unterstützt die dort gestärkte Belohnungsseite. Empfehlung: unabhängig davon bauen.~~ → Entscheidung 11.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

Die Story **liest** nur. Sie ändert keine Münz-Regel und bucht nichts. **Querfrage mit [B-627](B-627-berechtigte-meldung-bucht-nach.md)
und [B-687](B-687-hausaufgabe-mit-foto-nachweis.md):** Gegenbuchungen und `Manual`-Gutschriften zählen nicht als „verdient“
(`EarnedCoinKinds`, `WalletService.cs:47-48`), die Rücknahme eines Malus verkürzt also die Tagesrechnung nicht. Münzen aus
dem Bonus-Training, der Kiste und neuer Hausaufgaben-Gutschriften zählen dagegen mit, denn die Art ist nur durch Ausschluss
definiert. **Überschneidung mit abgenommenen Entscheidungen:** keine; G-6 (kein Vergleich mit Geschwistern,
`MeController.cs:38-40`) wird eingehalten.

1. **Ein „Tag“ ist ein Kalendertag beim Tempo der letzten sieben Tage; „Pflicht-Tag“ ist keine Einheit.** `Tage` =
   aufrunden(fehlende Münzen / durchschnittliche verdiente Münzen je Kalendertag der letzten sieben Tage), gerechnet über
   dieselben Münzarten wie `WeekCoinsAsync` (`WalletService.cs:47-48`, Summe der positiven Einträge). Die Anzeige sagt „bei
   deinem Tempo etwa N Tage“ und nennt keine Pflicht-Tage. *Begründung (zwei Größen nicht gleichgesetzt):* Ein Pflicht-Tag in
   Münzen gibt es nicht als Feld (Ist-Stand, „nicht gefunden“); der Tagessatz schwankt mit Plänen, Kiste, Bonus und Serie, und
   eine Rechnung aus `PointsGoalMet` wäre falsch, sobald der Vater die Pläne umbaut. `WeekCoinsAsync` selbst taugt nicht
   (Kalenderwochen: „diese Woche bis jetzt“ und „Vorwoche“, `:56-72`); gebraucht wird eine **gleitende** Summe der sieben
   vollen Tage vor heute, eine neue Abfrage im `WalletService` mit derselben Art-Liste. *Kosten:* eine zusätzliche Abfrage,
   ein Titel „Pflicht-Tagen“ in der Überschrift der Story passt nicht mehr (Text zieht beim Bauen nach). *Verworfen:* Teilen
   durch die Lerntage (Tage mit Verdienst): ein einziger guter Tag ergäbe eine geschönte Zahl. *Schließt aus:* ein Versprechen
   „so viele Pflicht-Tage“; die Zahl ist eine Prognose und heißt so.
2. **Zu wenig Verlauf heißt keine Tage.** `Tage` ist `null`, wenn die verdiente Summe der sieben Tage 0 ist **oder** der erste
   Eintrag im Kontoverlauf des Kindes jünger als sieben Tage ist; dann zeigt die Karte nur die fehlenden Münzen. *Begründung:*
   Mit einem Tag Verlauf und Teilung durch sieben läge die Zahl zu hoch, mit Teilung durch den einen Tag viel zu niedrig; statt
   „∞ Tage“ oder einer geschönten Zahl lieber keine. *Kosten:* eine Abfrage nach dem frühesten Eintrag; ein neues Kind sieht
   die ersten Tage keine Prognose. *Schließt aus:* eine Prognose in der ersten Woche.
3. **Genau ein Ziel.** Ein zweites Setzen ersetzt das erste. *Begründung:* ein Ziel ist ein Fokus, eine Liste ein
   Wunschzettel. *Kosten:* wer zwei Dinge will, wählt. *Verworfen:* mehrere Ziele.
4. **Der Streifen steht auf der Startseite, die gepinnte Karte im Shop, nicht im Lernfluss.** Kein Ablenken beim Üben;
   `frontend/CLAUDE.md` wird vor dem Bau gegengelesen. *Kosten:* ein Lernender, der sich beim Üben motivieren wollte, sieht
   das Ziel dort nicht.
5. **Gespeichert wird am Kind: ein Verweis auf die Listing-Id (FK, `SetNull`) und der Titel als Momentaufnahme nur für den
   Verlustfall.** `Child.SavingsGoalListingId` (nullbar) und `Child.SavingsGoalTitle` (nullbar). Der **Preis** kommt immer
   live vom Angebot, nie vom Kind: ein Preis, den der Vater ändert, muss durchschlagen. *Begründung (gefunden):* Angebote
   werden **hart gelöscht** (`ShopController.cs:339-350`, ein Artikel nimmt seine Angebote per `Cascade` mit,
   `PuglingDbContext.cs:913`); mit `SetNull` allein wäre „Ziel weg“ von „nie eines gesetzt“ nicht zu unterscheiden, und die
   Entscheidung 6 („sichtbar nicht mehr verfügbar“) ginge nicht. Der Titel macht den Unterschied: `Id` leer und `Title`
   gefüllt = verschwunden. *Kosten:* zwei nullbare Spalten, **eine additive Migration** an der Kette (die Probe auf einer Kopie
   der echten DB), ob die Schema-Tore einen FK ohne Titel verlangen, beim Schätzen lesen. *Verworfen:* eigene Tabelle (ein
   Ziel je Kind braucht keine) und ein Verweis ohne FK (bricht die Schema-Linie).
6. **Verschwindet oder deaktiviert der Vater das Angebot, bleibt das Ziel sichtbar mit „nicht mehr verfügbar“, und das Kind
   kann es entfernen.** Deaktiviert (`Active = false`): Id bleibt, Antwort mit `verfuegbar: false`. Gelöscht: Id leer, Titel
   bleibt (Entscheidung 5). *Kosten:* ein Aufruf zum Entfernen (leert beide Spalten). *Schließt aus:* ein Ziel, das ohne
   Erklärung verschwindet; der Vater, der den Preis anhebt, hebt damit die Latte, das bleibt seine Entscheidung.
7. **Gems:** Bei Gem- oder Mischpreis wird nur der Münzanteil in Tage umgerechnet, der Gem-Anteil als Zahl gezeigt; bei einem
   reinen Gem-Preis gibt es keine Tage. *Begründung:* Gems hängen an Boni, nicht an Tempo (`PointKindCurrency`). *Kosten:*
   Gem-Ziele haben keine Prognose.
8. **Bestand 0 oder eigener Bestand leer:** Das Ziel bleibt, die Anzeige nennt das Auffülldatum (`NextRefillAtUtc`,
   `ShopListingResponse`). *Kosten:* keine.
9. **Schuldenstand:** „Noch X Münzen“ wird vom echten Stand gerechnet, auch wenn er negativ ist
   (`fehlend = max(0, Preis − Kontostand)`, Schulden sind erlaubt, `PositionProgressService.cs:746-754`). *Begründung:* ein
   Ziel, das die Strafe verschweigt, wäre unehrlich. *Kosten:* ein Kind im Minus sieht eine Zahl über dem Preis.
10. **Münz-Ökonomie bleibt unberührt, und die Grenze steht ausdrücklich in der Story.** Ausgeschlossen: Sparbonus und Zinsen
    auf das Ziel, eine Reservierung („Münzen sperren“), ein Eltern-Zuschuss. *Begründung:* Das Ziel liest; eine Reservierung
    wäre ein Abbuchungspfad im Sinne der Wallet-Invariante und eine eigene Story. Setzen, Ersetzen und Entfernen ändern weder
    Kontostand noch Ledger noch `ConcurrencyStamp`. *Kosten:* wer eine Reservierung will, legt eine neue Story an. *Verworfen:*
    „später dazu“ im selben Zuschnitt.
11. **Unabhängig von B-681 (Leitbild Privatlehrer) bauen.** Das Sparziel stützt die dort gestärkte Belohnungsseite, hängt aber
    an keiner Rollenaufteilung. *Kosten:* Ändert B-681 die Belohnung, bleibt der Shop, den das Ziel liest. *Hinweis:* B-681 ist
    nicht entschieden; die Entscheidung berührt B-593 nicht.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. Das Kind setzt über einen Aufruf ein Angebot aus **seiner** Shop-Ansicht als Ziel; ein Angebot eines Betreuers, der nicht
   sein Betreuer ist, liefert 404. *Rot, wenn die Prüfung nur auf Existenz statt auf `ListingsForStudentAsync` geht.*
2. Die Antwort nennt `fehlendeMuenzen = max(0, Preis − Kontostand)`, bei Schulden vom echten Stand. *Rot, wenn mit 0 als
   Boden gerechnet wird oder die Schuld fehlt.*
3. `Tage` ist `null` bei null verdienten Münzen der letzten sieben Tage, **bei einem Kontoverlauf unter sieben Tagen**, bei
   einem reinen Gem-Preis und sonst `aufrunden(fehlend / Schnitt)` mit dem Schnitt je Kalendertag über die sieben vollen Tage
   vor heute. *Rot, wenn eine Division durch null auftreten kann, der Schnitt die Kalenderwoche statt der gleitenden Spanne
   nimmt (Test: Verdienst nur in der Vorwoche) oder bei kurzem Verlauf eine Zahl erscheint.*
4. Eine `Manual`-Gutschrift und eine Malus-Gegenbuchung ändern den Schnitt nicht. *Rot, wenn die Summe eine andere Art-Liste
   als `EarnedCoinKinds` nimmt.*
5. Setzt der Vater den Preis des Angebots herauf, zeigt die nächste Abfrage den neuen Fehlbetrag. *Rot, wenn der Preis am
   Kind kopiert wurde.*
6. Wird das Angebot **deaktiviert**, bleibt das Ziel mit `verfuegbar: false`; wird es **gelöscht**, bleibt der Titel der
   Momentaufnahme und `verfuegbar: false`, das Ziel ist nicht still weg. *Rot, wenn `SetNull` die Id leert und kein Titel
   bleibt (Test löscht Angebot und Artikel und liest die Antwort).*
7. Ein zweites Setzen ersetzt das erste; Entfernen leert Id und Titel. *Rot, wenn zwei Ziele möglich sind oder der Titel
   stehen bleibt.*
8. Setzen, Ersetzen und Entfernen ändern weder Kontostand noch Ledger noch `ConcurrencyStamp`. *Rot, sobald ein
   `ChildPointsEntry` entsteht (Regressionstest: Kontostand vor und nach).*
9. Ein Supervisor liest das Ziel seines Kindes, setzt es aber nicht an dessen Stelle (wie bei der Rückmeldung,
   `ExerciseFeedbackController.cs:39-46`). *Rot, wenn `User.IsStudent()` nicht geprüft wird.*
10. Die Sohn-App zeigt Ziel, fehlende Münzen und Tage auf der Startseite; ein Kind ohne Ziel sieht den Streifen nicht; die
    Tage sind als „bei deinem Tempo etwa“ gekennzeichnet. *Rot, wenn der Streifen ohne Ziel erscheint oder „Pflicht-Tage“ als
    Zusage dasteht. Vitest an `SohnHome`.*
11. Bestand: nach der Migration ist das Ziel bei jedem Kind leer, und der Kontostand ist unverändert. *Rot, wenn die Migration
    etwas vorbelegt (Vorbild `ActivatedAtMigrationTests`).*

## Schätzung

**Größe: M** – wie der Batch-Pfad im `MediaSelector` (B-03), in der oberen Hälfte: ein kleiner Backend-Zuschnitt (zwei Spalten,
drei Routen, eine neue Summenabfrage) und zwei Oberflächenstellen. Nicht L: die Migration ist additiv und nimmt keine
Bestandsdaten um, es gibt keine Abrechnung.

Nachgesehen am 2026-11-20:

- **`migration: ja`.** `Child.SavingsGoalListingId` (nullbar, FK auf `ShopListing`, `SetNull`) und `Child.SavingsGoalTitle` sind
  neue Spalten an `Child` (`Models/AdminEntities.cs`); die Kette endet heute bei `20261119065732_ShopStockPerChild`. Zwei
  Schema-Tore zeigen auf die neuen Spalten: `Jeder_Fremdschluessel_Hat_Ein_Abgenommenes_Loeschverhalten`
  (`SchemaGuardTests.cs:194`, die Liste `abgenommen` bekommt eine Zeile) und `Jede_String_Spalte_Hat_Eine_Laenge`
  (`:388`, der Titel braucht eine Höchstlänge). Beides ist die Antwort auf die in Entscheidung 5 offen gelassene Frage, ob die
  Tore einen FK ohne Titel verlangen: sie verlangen die Zeile und die Länge, nicht den Titel.
- **`vertragsbruch: nein`.** Neu sind eine Route und ihre Antwort in `Pugling.Contracts/Student/MeDtos.cs`; `ShopListingResponse`
  und `WeekCoinsResponse` bleiben. Die gepinnte Karte erkennt das Frontend an der Listing-Id der Zielantwort, ein Flag am
  Angebot braucht es nicht.
- **`wo: beides`, Backend zuerst.** Route, Summenabfrage und Löschverhalten, dann `SohnHome` und `SohnShop`.
- **Wallet-Invariante:** Setzen, Ersetzen und Entfernen buchen nichts und bumpen den `ConcurrencyStamp` nicht (AK 8). Wer
  `Child` dafür als getrackte Entität lädt und speichert, hängt am Token: ein paralleler Kauf liefert dann `409` für das
  bloße Setzen. Darum `ExecuteUpdateAsync` auf die zwei Spalten (prüft das Token nicht und bumpt es nicht).

### Risiken

1. **Löschen nimmt das Ziel mit (Entscheidung 5).** `ShopController.DeleteListing` löscht hart
   (`Controllers/Supervisor/ShopController.cs:339-352`); der Artikel nimmt seine Angebote per `Cascade` mit
   (`PuglingDbContext.cs:913`). Mit `SetNull` auf der Id und dem Titel als Rest ist „verschwunden“ erkennbar. Die Probe
   (AK 6) muss **beide** Wege löschen: Angebot und Artikel.
2. **Zeitfenster.** Eine gleitende Spanne („die sieben vollen Tage vor heute“) ist neu; `WeekCoinsAsync` rechnet Kalenderwochen
   (`WalletService.cs:56-72`) und taugt nicht. Grenzfall: Verdienst nur in der Vorwoche, Verdienst nur heute (zählt nicht).
3. **Frühester Eintrag.** „Kontoverlauf jünger als sieben Tage“ braucht `MIN(CreatedAt)` je Kind. Ein Kind ohne Einträge ist
   „unter sieben Tage“, nicht ein Fehler.
4. **Angebot außerhalb der Zielgruppe.** Das Ziel darf nur auf ein Angebot zeigen, das `ShopService.ListingsForStudentAsync`
   dem Kind liefert; ein später entfernter Zielgruppen-Eintrag macht es „nicht verfügbar“, nicht ungültig.
5. **Fremde, ungecommittete Arbeit:** `SohnHome.tsx` ist am 2026-11-20 nicht geändert, `SohnApp.tsx` schon (LearningDad); `SohnHome`
   und `SohnShop` bauen erst, wenn diese Arbeit committet ist, falls sie sie berührt.
6. **Text:** Die Überschrift der Story spricht von „Pflicht-Tagen“, die Anzeige darf das nicht (Entscheidung 1).

### Angriffsplan

1. Modell und Migration: zwei Spalten, FK mit `SetNull`, Länge des Titels, Eintrag in `abgenommen` des FK-Tors; Migration anhängen,
   Probe an einer Kopie der echten DB (Vorbild `ActivatedAtMigrationTests`, jüngstes Beispiel `ShopStockPerChildMigrationTests`).
2. `WalletService`: Summe der sieben vollen Tage vor heute über `EarnedCoinKinds`, dazu der früheste Eintrag.
3. `MeController`: `GET me/savings-goal`, `PUT me/savings-goal` (Listing-Id), `DELETE me/savings-goal`; Rollenprüfung
   `User.IsStudent()`, Zugriff über `ListingsForStudentAsync`; Antwort mit `fehlendeMuenzen`, `Tage`, `verfuegbar`, Titel, Gem-Anteil,
   `NextRefillAtUtc`.
4. Contract-Typen und Client-Methoden; die `unknown_field`-Guards des Frontends nachziehen (nur Felder, die der Vertrag kennt).
5. `SohnHome`: Streifen; `SohnShop`: gepinnte Karte und der Knopf „Als Ziel“; Texte in Deutsch und Englisch.
6. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`, Vitest, E2E, beide Reviewer.

### Testweg

- Neue Klasse `SparzielTests` (Vorbild `WochenvergleichTests` für die Uhr, `ShopFlowTests` für Angebote und Kauf): AK 1 bis 9. Rote Proben je
  Kriterium stehen oben in den Akzeptanzkriterien; besonders AK 3 (Verdienst nur in der Vorwoche, kurzer Verlauf, reiner Gem-Preis,
  Division durch null) und AK 6 (Angebot **und** Artikel löschen).
- `SparzielMigrationTests` (neu, nach `ActivatedAtMigrationTests`): AK 11, jedes Kind danach ohne Ziel, Kontostand gleich.
- `SchemaGuardTests` laufen ohnehin mit und halten FK-Verhalten und Spaltenlänge.
- `SohnHome.test.tsx`: AK 10 (kein Ziel, kein Streifen; „bei deinem Tempo etwa“, nie „Pflicht-Tage“).
- E2E: neue Spec `frontend/e2e/sparziel.spec.ts` (Vorbild `shop-verlauf.spec.ts`): Ziel anheften, Streifen auf der Startseite, Vater löscht
  das Angebot, Streifen sagt „nicht mehr verfügbar“. Das ist der Rollengang als Sohn.
- Vor der Abnahme: `pugling-reviewer` und `frontend-reviewer`.

## Verlauf

- **2026-11-16** — angelegt aus dem Marktvergleich Belohnungs-Apps (Quick-Win 2).
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Ist-Stand gegen den Code; Shop und Verdienst-Kennzahl bestehen
  (`GET me/shop`, `GET me/points/week`), ein Zielzeiger und die Umrechnung in Tage fehlen; elf offene Punkte, darunter
  die Ökonomie-Grenze, mit Empfehlung. Keine Entscheidung gefällt.
- **2026-11-20** — gegrillt (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers „Entscheide die offenen Fragen nach deiner Empfehlung“; unbestätigt): Tempo = Münzen je Kalendertag der letzten sieben Tage (keine Pflicht-Tage); Ziel am Kind als FK mit Titel-Momentaufnahme, weil Angebote hart gelöscht werden; Ökonomie unberührt (keine Reservierung, Zinsen, Zuschuss). Akzeptanzkriterien final.
- 2026-11-20 · geschaetzt (Vorbereitungslauf, Sonnet): M, beides (Backend zuerst), Migration ja (zwei Spalten, FK mit `SetNull`, Einträge in den zwei Schema-Toren), kein Vertragsbruch. Wallet-Invariante: das Ziel bucht nichts; Setzen über `ExecuteUpdateAsync`, damit weder Token noch Stamp berührt werden. Unabhängig von B-660 und B-218.
- **2026-11-25** — `geschaetzt` → `in-arbeit` (Nachtlauf N, Sprint 181, [Protokoll](../pm-sitzung-2026-11-25-n.md)). Gebaut:
  Spalten am Kind + Migration `20261125213333_SavingsGoal`, `SavingsGoalService`, `GET|PUT|DELETE student/me/savings-goal`,
  `GET supervisor/children/{childId}/savings-goal` (nur lesen), Streifen auf `SohnHome`, gepinnte Karte und „☆ Als Ziel“ in
  `SohnShop`, Zeile im Kind-Hub des Vaters. Der Titel der Story bleibt; die Anzeige sagt „bei deinem Tempo etwa“ (Entscheidung 1).
  Je Fall (Pfad × Wert) der Test und die Mutante, die ihn rot macht (gemessen mit `mutant-probe.py` nach der letzten Änderung):
  - AK 1, Angebot nur für das Geschwister bzw. inaktiv → 404 → `SparzielTests.Nur_Ein_Angebot_Aus_Dem_Eigenen_Shop_Wird_Ziel`; S1 (`activeOnly: false`) rot, S2 (Existenz statt Shop-Ansicht) rot.
  - AK 2, Schulden → `Fehlbetrag_Zaehlt_Ab_Dem_Echten_Stand_Auch_Bei_Schulden`; S3 (Stand mit 0 als Boden) rot.
  - AK 3, gleitende Spanne/heute/Tag davor/Kalenderwoche → `Tage_Beim_Tempo_Der_Sieben_Vollen_Tage_Vor_Heute`; S6 (heute zählt), S7 (Tag davor zählt), S8 (Kalenderwoche) rot.
  - AK 3, kurzer Verlauf, Grenze genau sieben Tage, nichts verdient, reiner Gem-Preis → `Keine_Tage_Bei_Kurzem_Verlauf_Ohne_Verdienst_Und_Bei_Gem_Preis`; S4 (`>=`), S5 (Verlaufsprüfung weg), S11 (Gem-Preis umgerechnet) rot.
  - AK 4, Geschenk und Malus-Korrektur → `Tage_Beim_Tempo…`; S9 (`Manual` zählt), S10 (positive `GoalPenalty` zählt) rot.
  - AK 5, Preis heraufgesetzt → `Preis_Schlaegt_Durch_Und_Ein_Verschwundenes_Angebot_Bleibt_Sichtbar`. Keine Ein-Zeilen-Mutante „Preis am Kind kopiert“ möglich (es gibt keine Preisspalte am Kind); die Zusicherung 140 nach dem PATCH trägt den Fall.
  - AK 6, deaktiviert / Angebot gelöscht / Artikel gelöscht → `Preis_Schlaegt…`; S12 (ohne Id gilt als „kein Ziel“) rot.
  - AK 7 und 8, ersetzen, entfernen, Stamp/Ledger → `Ersetzen_Und_Entfernen_Buchen_Nichts`; S13 (Titel bleibt), S14 (Stamp gebumpt) rot.
  - AK 9, Vater liest, kein Fremdzugriff, setzt nicht → `Betreuer_Liest_Das_Ziel_Setzt_Es_Aber_Nicht`; S15 (Ownership-Filter weg) rot. Das „setzt nicht“ ist **doppelt** gesichert (Rollen-Attribut am `MeController` und `User.ChildId()`-Prüfung): S16 (Inline-Prüfung weg) und S16b (Attribut gelockert) je **grün** gemessen – jede Schicht allein reicht, der Test unterscheidet sie nicht.
  - Review-Fund (GET schreibt Bestandszeilen) → `Betreuer_Liest…` (keine `ShopListingStock`-Zeile nach Lesen); S17 (`persist: true`) rot.
  - AK 10 → `frontend/src/sohn/sparzielLob.test.tsx` (vier Fälle + Gem-Preis); F9 („Pflicht-Tage“), F10 (Tage ohne Zahl), F11 (Streifen ohne Ziel), F12 (Gem-Preis mit „und“) rot.
  - AK 11 → `SparzielMigrationTests` (Kind, Skins, Stamp, Ledger überleben den Tabellen-Neubau, Ziel leer).
- **2026-11-26** — Verifikation: Backend 2481 Tests (2480 grün, 1 rot: das gepinnte Zählertor für ungepagte GETs, 34 → 35 nachgezogen; Nachlauf nach Review-Fixes siehe Protokoll),
  Vitest 1482/1482, `tsc -b` sauber, Playwright `sparziel.spec.ts` + `lob.spec.ts` + `shop-verlauf` + `full-flow` 4 passed (Ports 5242/5282).
  Reviews: `pugling-reviewer` ohne Blocker (eingearbeitet: GET ohne Schreib-Nebenwirkung über `persist`, Rennen beim Setzen → 404,
  eine Uhr je Lesung); `frontend-reviewer` ohne Blocker (eingearbeitet: Gem-only-Text, Plural/`stale` im Kind-Hub, Label-in-Name, E2E-Spec).
  Migrationsprobe an der Kopie der echten DB (`kopie-server.sh`, :5242): Kette läuft durch (Hinweis von EF: der FK-Neubau der Tabelle
  `Children` läuft außerhalb einer Transaktion), alle vier Kinder da, Ledger gleich; Rollengang dort als Sohn (Friedrich: Ziel setzen,
  ersetzen, Preis vom Vater angehoben → „noch 1930, etwa 5 Tage“, nachgerechnet 3368 Münzen in sieben Tagen; entfernen → 204) und als Vater (lesen).
- **2026-11-26** — `in-arbeit` → `abgenommen`: Commit `6a0257dd`; Rollengang an der Kopie der echten DB (Sohn Friedrich, Vater) und maschinell per Playwright, Creator Regressionszeuge; beide Reviews ohne Blocker. Offen nur ein Blick am Gerät (Overlay bzw. Streifen am Handy), kein Abnahmehindernis.
- **2026-11-26** — nachgeschaut: Merge-Review der Läufe M/N/O (Commits `61a11f55`, `5446640c`), festgehalten in der Retrospektive von Sprint 192. Fund: B-1026 (behoben in `5446640c`).
