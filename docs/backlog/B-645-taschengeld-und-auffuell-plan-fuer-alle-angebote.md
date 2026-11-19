---
tags: [typ/story, status/abgenommen, bereich/shop, bereich/gamification, rolle/supervisor, rolle/student]
aliases: [Taschengeld, Monatliches Auffüllen, Auffüll-Plan]
status: abgenommen
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: ja
quelle: "Vater im Chat, 2026-11-17"
grund: ""
ersetzt_durch: []
---

# B-645 · Taschengeld als Shop-Belohnung mit monatlichem Auffüllen – und ein Auffüll-Plan, der überall greift

Der Vater will das **Taschengeld** über Pugling laufen lassen: Die Söhne müssen es sich erarbeiten. Es ist
günstig, gibt es aber nur **einmal im Monat**, und der Bestand füllt sich **einmal im Monat** wieder auf.
Darüber soll auch nachvollziehbar sein, ob ein Sohn sein Taschengeld bekommen hat. Bei der Spielzeit gibt es
schon etwas Ähnliches (Auffüllen bis zu einer Menge), aber das „scheint nicht überall zu funktionieren“.
Der Wunsch: ein Auffüll-Plan („wie oft, bis zu welcher Menge“), der bei **allen** Angeboten gleich gut greift.

## User Story

Als **Vater (Supervisor)** möchte ich jedes Shop-Angebot mit einem Auffüll-Plan versehen – täglich,
wöchentlich, **monatlich** – und das Taschengeld als Angebot in Euro einstellen, das sich jeder Sohn **einmal im
Monat** für ein paar Münzen erarbeiten kann. **Damit** das Taschengeld an den Lernerfolg gebunden ist und ich in
der Kauf- und Einlösehistorie sehe, wer es sich in welchem Monat verdient hat und ob ich es ausgezahlt habe.

Als **Sohn (Student)** möchte ich sehen, wann ein leeres Angebot wieder aufgefüllt wird, **damit** ich weiß, ob
sich das Sparen jetzt lohnt.

## Ist-Stand am Code

**Das Fundament ist da: der Shop, nicht die Missionen.** „Belohnungen“ im Vater-Web
(`frontend/src/vater/VaterRewards.tsx:40-43`) sind Missionen und Auszeichnungen, die nur Gems zahlen. Echte
Belohnungen laufen über den Familien-Shop: Ein `ShopArticle` ist die Art („Spielzeit“, Einheit Minute), ein
`ShopListing` das Angebot mit Preis, Menge und Bestand (`backend/Pugling.Api/Models/GamificationEntities.cs:157-215`).
Wer kauft, bekommt die Menge ins `ChildInventory` gebucht. Einlösen läuft über eine `ActivationRequest`, die der
Vater bestätigt (`GamificationEntities.cs:282-310`). Taschengeld passt genau hinein: Angebot kaufen, dann
Auszahlung beantragen, dann bestätigt der Vater, sobald das Geld übergeben ist. Damit ist „hat er es bekommen?“
schon heute beantwortbar.

**Der Auffüll-Mechanismus hängt schon an jedem Angebot**, also nicht nur an der Spielzeit:
`CurrentStock`, `MaxStock`, `RefillKind`, `RefillAtUtc`, `RefillDayOfWeek`, `LastRefilledAtUtc`
(`GamificationEntities.cs:202-212`). Er wird **lazy** angewandt, nämlich beim Lesen der Angebotslisten (Vater:
`ShopService.cs:85`, Sohn: `ShopService.cs:122`) und beim Kauf (`ShopService.cs:145`). Dazu kommt eine vierte
Stelle: Die Einzelansicht eines Angebots für den Vater (`ShopController.cs:208`) rechnet das Auffüllen auf einer
nicht verfolgten Kopie nur für die Anzeige aus und speichert nichts. Es gibt keinen Hintergrund-Job. Ein fälliges Auffüllen setzt den Bestand auf `MaxStock` hoch; was übrig war, addiert es nicht
(`ShopService.cs:380-389`).

Arten heute: `None`, `Once`, `Daily`, `TwiceDaily`, `Weekly`
(`backend/Pugling.Contracts/Common/GamificationBaseTypes.cs:65-77`). Einheiten: `Stueck`, `Minute`, `Stunde`,
`Gramm`, `Mal` (`GamificationBaseTypes.cs:35-47`).

**Echte DB (gelesen auf einer Kopie, 2026-11-17).** Sechs Angebote, alle aus dem Seed, alle von Adult 1. „30 Minuten
Zocken“ (Listing 3) steht auf `Daily`, trägt aber noch `RefillDayOfWeek = Monday`. Im Seed war es `Weekly`
(`backend/Pugling.Api/Data/Seed.cs:1031-1032`), es wurde also umgestellt. „60 Minuten Zocken“ (Listing 4) steht auf
`Weekly/Monday`. An Adult 1 hängen drei Kinder (`SupervisorLinks`: Child 1, 3, 4). Gekauft hat bisher nur Child 4.

### Warum es „nicht überall funktioniert“: vier belegte Ursachen

1. **Wöchentlich verfällt, wenn am Stichtag niemand in den Shop schaut.** `Weekly` ist nur fällig, wenn
   *heute* der eingestellte Wochentag ist (`ShopService.cs:398-399`). Öffnet am Montag weder Sohn noch Vater den
   Shop, wird bis zum nächsten Montag nichts aufgefüllt. Die Woche fällt aus, weil es keinen Nachholmechanismus gibt.
   `Daily` hat das Problem nicht (`ShopService.cs:394`), denn dort reicht „letztes Auffüllen vor heute“.
2. **Das Vater-Web kann „Wöchentlich“ und „Einmalig“ nicht anlegen.** Das Formular bietet alle fünf Arten an
   (`frontend/src/vater/VaterShop.tsx:18`, `:248-249`, `:311-312`), hat aber weder ein Wochentag- noch ein
   Datumsfeld. `refillDayOfWeek`/`refillAtUtc` kommen in `VaterShop.tsx` nicht vor. Der Server verlangt sie und
   antwortet mit `400` (`backend/Pugling.Api/Controllers/Supervisor/ShopController.cs:454-457`). Umstellen klappt nur,
   wenn die Zeile schon einen Wochentag trägt (PATCH übernimmt den alten, `ShopController.cs:267-273`), wie beim
   Seed-Montag. Das ist aus dem Code abgeleitet, nicht im Browser nachgeklickt.
3. **Der Bestand gilt für die ganze Familie, nicht je Kind.** `ShopListing` hat kein Kind-Feld. Der Kauf
   prüft und senkt *einen* Zähler am Angebot (`ShopService.cs:147`, `:174`). Kauft Friedrich das eine
   Monats-Taschengeld, ist es für Theodor weg. Bei drei verknüpften Kindern ist das für Taschengeld unbrauchbar
   und für „3× Zocken am Tag“ schon heute falsch.
4. **`TwiceDaily` schaltet um 12 Uhr UTC um** (`ShopService.cs:395-397`), lokal also um 13 bzw. 14 Uhr. Das ist
   konsistent mit der Projektregel „Tageslogik in UTC“, aber vermutlich nicht, was der Vater erwartet.

### Was fürs Taschengeld fehlt

- **Kein `Monthly`** in `ShopRefillKind`.
- **Keine Geld-Einheit** in `UnitType`. „20 Stück“ wäre der Notbehelf.
- **Der Sohn sieht nicht, wann aufgefüllt wird.** Das DTO trägt `RefillKind` und `LastRefilledAtUtc`
  (`ShopController.cs:31-35`), aber keinen berechneten nächsten Zeitpunkt.

## Die echte Lücke

Es braucht **keinen neuen Belohnungstyp**. Ein Taschengeld-Angebot im Familien-Shop mit Kauf und
Auszahlungsbestätigung deckt den Ablauf ab. Die Lücke ist schmaler und liegt tiefer:

- Der Auffüll-Plan braucht eine **einheitliche Fälligkeitsregel mit Nachholen**: „seit dem letzten Auffüllen ist
  mindestens ein Stichtag vergangen“. Heute gilt „heute ist der Stichtag“. Dazu kommt die Art **`Monthly`** mit
  einem Tag im Monat.
- Der **Bestand muss je Kind zählen**. Das ist der Kern von „gibt es nur einmal im Monat“ bei zwei Söhnen und
  braucht eine Migration (neue Tabelle oder Spalte, die die heutigen Bestände mitnimmt).
- Das **Vater-Web muss jede Art, die es anbietet, auch anlegen können**: Wochentag, Monatstag, Datum.
- Eine **Geld-Einheit** und ein **„füllt sich wieder auf am …“** für den Sohn.

## Offene Punkte

1. ~~**Träger: Shop-Angebot oder eigener Belohnungstyp?**~~ → Entscheidung 2.
2. ~~**Ein Kauf = das ganze Taschengeld, oder Teilbeträge?**~~ → Entscheidung 3.
3. ~~**Bestand je Kind?**~~ → Entscheidung 1.
4. ~~**Verfallen oder ansparen?**~~ → Entscheidung 4.
5. ~~**Stichtag beim Monat.**~~ → Entscheidung 5.
6. ~~**Einheit Geld.**~~ → Entscheidung 6.
7. ~~**Was heißt „gekriegt“?**~~ → Entscheidung 7.
8. ~~**Defekte abspalten?**~~ → Entscheidung 8.
9. ~~**`TwiceDaily` um 12 Uhr UTC.**~~ → **zurückgestellt** (Entscheidung 9).
10. ~~**Anzeige für den Sohn.**~~ → Entscheidung 10.
12. ~~**Unterschiedlich hohes Taschengeld je Kind.**~~ → Entscheidung 12.

## Entscheidungen

1. **Jeder Sohn hat seinen eigenen Bestand** (Vater, 2026-11-17), und zwar bei allen Angeboten. Der Auffüll-Plan
   gilt je Kind. Begründung: Ein Monats-Taschengeld, das sich zwei Söhne teilen, ist sinnlos, und für die
   Tages-Spielzeit gilt dasselbe. Kosten: eine Migration, die jedem verknüpften Kind einen eigenen Startbestand gibt
   (Regel in Entscheidung 11, nicht einfach der familienweite Bestand), ein Kaufpfad, der den Zähler des Kindes sperrt (Concurrency am Kind-Bestand
   statt am Angebot), und eine Vater-Ansicht, die den Bestand je Kind zeigt statt einer einzigen Zahl.
2. **Das Taschengeld ist ein normales Shop-Angebot.** Ablauf: kaufen, Auszahlung beantragen, der Vater bestätigt
   nach der Übergabe. Begründung: Kauf, Inventar und Einlösebestätigung gibt es schon. Einen zweiten Belohnungspfad
   neben dem Shop hat das Projekt bewusst abgeschafft (`GamificationEntities.cs:153-155`). Kosten: Das Taschengeld
   ist nicht automatisch an eine erfüllte Monatspflicht gebunden, sondern nur über die Münzen. Das ist gewollt,
   denn die Münzen sind die Pflicht-Währung.
3. **Teilbeträge statt Alles-oder-nichts**, zum Beispiel ein Angebot „5 €“ mit 4 Stück im Monat statt „20 €“
   mit 1 Stück. Begründung: Ein Sohn, der schwächelt, bekommt einen Teil, und das Erarbeiten wird stufenweise
   sichtbar. Kosten: keine eigene Mechanik, denn es ist Konfiguration. Das Beispiel im Seed bzw. in der Doku zeigt
   diese Form.
4. **Ansparen bis zu einer Obergrenze, für alle Angebote mit einem Modell.** Jedes Angebot bekommt eine
   **Auffüllmenge je Stichtag** (neues Feld, z. B. +4). Der Bestand steigt um diese Menge, höchstens bis
   `MaxStock` (z. B. 8 = höchstens zwei Monate angespart). Verfallen ist der Sonderfall Auffüllmenge = `MaxStock`.
   Die Migration setzt alle bestehenden Angebote auf diesen Sonderfall, sodass TV und Zocken sich verhalten wie
   heute. Wurden mehrere Stichtage verpasst, zählen alle: Nachholen addiert je verpasstem Stichtag eine
   Auffüllmenge, gedeckelt bei `MaxStock`. Begründung: Ein Modell statt einer Taschengeld-Sonderregel, und
   „wie oft, bis zu welcher Menge“ ist genau das, was der Vater beschrieben hat. Kosten: eine neue Spalte, die
   Fälligkeitsregel muss die verpassten Stichtage zählen statt nur „fällig ja/nein“ zu prüfen, und das Formular
   bekommt ein Feld mehr.
5. **Monatlich heißt immer am 1.** (Kalendertag in UTC wie die übrige Tageslogik.) Begründung: kein Feld, kein
   Februar-Sonderfall, einfachster Bau. Kosten: Ein anderer Taschengeld-Tag ist nicht einstellbar. Er ließe sich
   später nachrüsten, ohne dass das Modell bricht.
6. **Neue Einheit `UnitType.Euro`, ganze Beträge.** Begründung: Mengen bleiben Ganzzahlen, Teilbeträge zu 5 €
   reichen. Kosten: Vertragsänderung, das Enum im Frontend (`frontend/src/lib/shop.ts:8`, `UNIT_OPTIONS`) zieht nach.
   Krumme Beträge wie 2,50 € gehen nicht.
7. **„Gekriegt“ liest man aus der vorhandenen Historie.** Der Kauf heißt verdient, die bestätigte Einlösung heißt
   ausgezahlt. Keine Monatsübersicht in dieser Story. Begründung: kein Mehrbau, solange unklar ist, ob die Historie
   reicht. Kosten: Wer wissen will, wie viel ein Sohn im November verdient hat, muss die Historie selbst
   durchsehen. Fehlt das in der Praxis, wird es eine eigene Story.
8. **Die beiden Defekte werden hier mitbehoben**: Wöchentlich holt nicht nach, und das Formular hat kein
   Wochentag- bzw. Datumsfeld. Begründung: Die neue Fälligkeitsregel (Entscheidung 4) und das neue Formular
   ersetzen genau diese Zeilen. Kosten: Die Spielzeit wird erst mit dieser Story wieder verlässlich wöchentlich
   aufgefüllt.
9. **`TwiceDaily` bleibt bei 12 Uhr UTC: zurückgestellt.** Begründung: Projektweit gilt UTC. Eine lokale Grenze
   braucht eine Familien-Zeitzone, und das ist eine eigene Story. Kosten: Der zweite Schub des Tages kommt lokal
   erst um 13 bzw. 14 Uhr.
10. **Der Sohn sieht, wann ein Angebot wieder aufgefüllt wird.** Der Server liefert je Angebot und Kind
    `nextRefillAtUtc`, berechnet und ohne Spalte. Die Sohn-App zeigt an einem leeren Angebot „wieder da am …“.
    Begründung: Es hilft beim Sparen und kostet wenig. Kosten: ein DTO-Feld (Vertragsänderung) und eine Zeile UI.
11. **Eine Regel für den Startbestand eines Kindes** (aus dem Code-Review abgeleitet, 2026-11-17; setzt
    Entscheidung 1 um, beim Rollengang vom Vater bestätigen lassen). Sie gilt überall, wo ein Kind noch keine
    Bestandszeile hat: in der Migration, bei einem neu verknüpften Kind und bei einem neu angelegten Angebot.
    - Das Kind startet mit **`MaxStock` minus seinen eigenen, nicht stornierten Käufen dieses Angebots seit dem
      letzten Auffüllen**, nie unter 0. „Seit dem letzten Auffüllen“ heißt: seit `LastRefilledAtUtc` des
      Angebots, bei `None` seit Anlage des Angebots. `LastRefilledAtUtc` der neuen Zeile übernimmt den Wert des
      Angebots. Die Käufe kommen aus `ShopPurchases` (`ChildId`, `ShopListingId`, `PurchasedAt`, `Status`).
    - `Once` vor `RefillAtUtc` startet mit 0 und `LastRefilledAtUtc = null`. Das Angebot gibt es erst ab dem Datum.
    - Begründung: Den familienweiten Bestand einfach zu kopieren, würde den Kauf eines Kindes allen Geschwistern
      anrechnen. Bei `Once` (schon ausgelöst) und `None` füllt danach nichts mehr auf, die Geschwister wären
      dauerhaft ausgesperrt. Eine einzige Regel verhindert außerdem, dass ein Kind, das einen Tag nach der
      Migration verknüpft wird, anders startet als seine Geschwister.
    - Kosten: Ein vom Vater von Hand gesenkter oder erhöhter Bestand wird nicht übernommen. Bei auffüllbaren
      Angeboten gleicht das der nächste Stichtag ohnehin aus. Bei `None` startet jedes Kind neu mit `MaxStock`.
      Das ist die bewusste Folge von „Bestand je Sohn“.
    - Sonderfall `MaxStock = 0`: Das ist heute erlaubt und bedeutet einen Bestand ohne Obergrenze, der nie
      aufgefüllt wird (`ShopService.cs:382`, `ShopController.cs:452`). Die Migration setzt hier vorher
      `MaxStock = CurrentStock + Käufe aller Kinder`, also ungefähr den Anfangsbestand. Bleibt das 0, ist das Angebot
      für jedes Kind leer wie heute. Laut Seed (Werte 1–4) gibt es diesen Fall in der echten DB vermutlich nicht.
      Der Probelauf auf der Kopie prüft das.
12. **Unterschiedliche Höhe über ein Angebot je Kind: Zielgruppe am Angebot** (Vater, 2026-11-19). Anlass: Ein
    Kind bekommt 10 € Taschengeld, das andere 20 €, beide einmal im Monat. Mit Entscheidung 1 hat zwar jedes Kind
    seinen eigenen Bestand, aber `MaxStock` und `RefillQuantity` liegen am Angebot und gelten für alle Kinder
    gleich. Darum bekommt jedes Angebot eine **Zielgruppe**: eine Liste von Kindern, leer heißt „alle verknüpften
    Kinder“ (Verhalten wie heute). Der Vater legt „Taschengeld Theodor“ (z. B. 5 €, Menge 2) und „Taschengeld
    Friedrich“ (5 €, Menge 4) getrennt an. Ein Kind außerhalb der Zielgruppe sieht das Angebot nicht, kann es nicht
    kaufen (`404` wie ein fremdes Angebot) und bekommt keine Bestandszeile.
    - Begründung: Das Modell bleibt einfach (keine Werte, die je Kind überschrieben werden), und Angebote, die nur
      ein Kind sehen soll, sind auch über das Taschengeld hinaus nützlich. Die Daddelzeit (1 h täglich für jedes
      Kind) bleibt **ein** Angebot mit leerer Zielgruppe, denn das Wegkaufen verhindert schon der Bestand je Kind.
    - Kosten: eine Join-Tabelle `ShopListingAudience` (Angebot × Kind, Cascade auf beiden Seiten), die die Migration
      leer anlegt. Bestehende Angebote gelten also weiter für alle Kinder. Ein Feld `childIds` an Anlage, Änderung
      und Antwort des Vater-DTOs (PATCH: `null` = unverändert, `[]` = alle Kinder). Ein Kind, das nicht über
      `SupervisorLinks` mit dem Aussteller verknüpft ist, wird mit `validation_error` abgewiesen. Im Vater-Web eine
      Mehrfachauswahl „für“ und eine Spalte mit der Zielgruppe. Wird die Zielgruppe verkleinert, bleiben die
      Bestandszeilen und die Käufe des herausgenommenen Kindes erhalten, das Kind sieht das Angebot nur nicht mehr.
      Die Startregel (Entscheidung 11) gilt für ein Kind, das neu in die Zielgruppe kommt.

## Akzeptanzkriterien

1. **Je Kind:** Ein Angebot mit `RefillKind = Monthly`, Auffüllmenge 1 und `MaxStock = 1` ist für **jedes**
   verknüpfte Kind genau einmal je Monat kaufbar. Kauft Friedrich, bleibt es für Theodor kaufbar.
2. **Nachholen:** Öffnet niemand den Shop am Stichtag, ist das Angebot beim nächsten Aufruf **trotzdem**
   aufgefüllt. Das gilt für `Weekly` und `Monthly`. Testweg: Integrationstest mit `TimeProvider`, der ohne
   Zwischenaufruf über den Stichtag springt.
3. **Ansparen:** Auffüllmenge 4, `MaxStock` 8. Jeder Fall nennt seinen Startbestand, und jeder ist so gewählt,
   dass ihn die heutige Regel (`Math.Max(Bestand, MaxStock)`, `ShopService.cs:385`) **rot** macht:
   - Startbestand 2 am 31. Oktober, kein Kauf → am 1. November **6** (heute: 8);
   - Startbestand 6 → am 1. November **8**, gedeckelt (heute: 8 – dieser Fall prüft nur den Deckel und wird
     durch eine Regel ohne `Math.Min` rot, nicht durch die heutige);
   - Startbestand 0 am 31. Oktober, nächster Aufruf erst am 2. Dezember (zwei Stichtage verpasst) → **8**;
     dieselbe Probe mit nur einem verpassten Stichtag (Aufruf am 2. November) → **4** (heute: 8).
4. **Verfallen wie bisher:** Bei Auffüllmenge = `MaxStock` verhält sich ein Angebot wie heute. Nach der Migration
   gilt das für alle bestehenden Angebote.
5. **Monatlich am 1.:** Ein `Monthly`-Angebot füllt sich am 1. des Monats (UTC) auf, an keinem anderen Tag.
6. **Vater-Web:** `Weekly` (mit Wochentag), `Monthly` und `Once` (mit Datum) lassen sich samt Auffüllmenge
   **anlegen** und ändern, ohne dass der Server `400` antwortet (E2E). Der Bestand wird je Kind angezeigt.
7. **Migration:** Eine bestehende `pugling.db` migriert, ohne dass Käufe verloren gehen. Jedes verknüpfte Kind
   hat danach den Startbestand nach Entscheidung 11. Ein Kind, das selbst nichts gekauft hat, ist also **nicht**
   ausgesperrt, weil ein Geschwisterkind ein `Once`- oder `None`-Angebot leergekauft hat. Kein Angebot verletzt
   danach die neue Validierung: Ein PATCH, der nur den Preis ändert, antwortet nicht mit `400`.
8. **Euro:** Ein Angebot mit Einheit `Euro` lässt sich anlegen. Ein Kauf erscheint als „5 €“ im Inventar. Die
   bestätigte Einlösung ist in der Vater-Historie als ausgezahlt erkennbar.
9. **Anzeige für den Sohn:** Die Sohn-App zeigt an einem leeren, auffüllbaren Angebot, wann es wieder da ist. Bei
   `RefillKind = None` zeigt sie keinen Termin. „Kaufbar“ (`Affordable`) richtet sich nach dem **eigenen**
   Bestand des Kindes: Ist er 0, ist das Angebot nicht kaufbar, auch wenn ein Geschwisterkind noch Bestand hat.
10. **Startbestand eines neuen Kindes und eines neuen Angebots:** Ein Kind, das nach der Migration verknüpft wird,
    und jedes Kind bei einem neu angelegten Angebot startet nach derselben Regel (Entscheidung 11) wie die
    Geschwister in der Migration. Ein `Once`-Angebot ist vor seinem Datum für niemanden kaufbar.
11. **Zielgruppe:** Ein Angebot mit Zielgruppe [Friedrich] erscheint in Friedrichs Shop, aber nicht in Theodors.
    Theodors Kaufversuch antwortet `404`. Ein Angebot mit leerer Zielgruppe sehen beide. `childIds` mit einem nicht
    verknüpften Kind antwortet `400 validation_error`. Nach der Migration hat jedes bestehende Angebot eine leere
    Zielgruppe. Rot machen diese Fälle ein Sohn-Listenpfad oder ein Kaufpfad, der die Zielgruppe nicht filtert.

## Schätzung

**Größe: L.** Das ist eine Etappe vom Format E6: neues Schema mit Datenübernahme aus der echten DB, ein umgebauter
Kaufpfad mit eigener Concurrency und dazu die UI auf beiden Seiten. Es bleibt unter XL, weil alle Teile an einem
Aggregat hängen (Angebot plus Bestand je Kind) und kein zweiter Bereich berührt wird.

- **`wo: beides`**: Backend zuerst (API-First), dann Vater-Web und Sohn-App.
- **`migration: ja`**: Eine neue Tabelle `ShopListingStock` (Angebot × Kind: `CurrentStock`, `LastRefilledAtUtc`,
  `ConcurrencyStamp`) und am Angebot die neue Spalte `RefillQuantity` mit dem Enum-Wert `Monthly`. Die Migration
  legt für jedes Angebot und jedes über `SupervisorLinks` verknüpfte Kind eine Zeile mit dem Startbestand nach
  Entscheidung 11 und dem heutigen `LastRefilledAtUtc` an. Sie setzt `RefillQuantity = MaxStock` (Entscheidung 4:
  Verhalten wie heute). Ein Angebot mit Auffüll-Art, aber `MaxStock = 0` wird heute nie aufgefüllt
  (`ShopService.cs:382`). Die Migration stellt es auf `RefillKind = None`. Das Verhalten bleibt gleich, und die
  neue Validierung (siehe Angriffsplan, Schritt 4) gilt danach für jede Zeile. Danach entfallen `CurrentStock` und `LastRefilledAtUtc` am Angebot. `ConcurrencyStamp` am Angebot bleibt für
  Stammdaten-Änderungen.
- **`vertragsbruch: ja`**: Das Feld `ShopListingDto.CurrentStock` hat nur noch je Kind eine Bedeutung. Es wird
  durch eine Liste je Kind ersetzt (`stocks: [{childId, currentStock, nextRefillAtUtc}]`). „Bestand füllen“ des
  Vaters (`UpdateShopListingDto.CurrentStock`) braucht ein Kind, also einen eigenen Endpunkt
  `PUT …/listings/{id}/stock/{childId}` statt des PATCH-Felds. Auch `CreateShopListingDto.CurrentStock`
  (`ShopDtos.cs:47`, verwendet in `ShopController.cs:225`, `:236`) **entfällt**. Ein neues Angebot hat keinen
  familienweiten Startbestand mehr, jedes Kind startet nach Entscheidung 11. Additiv kommen dazu: `UnitType.Euro`,
  `ShopRefillKind.Monthly`, `RefillQuantity`, `ShopListingResponse.NextRefillAtUtc`. `ShopListingResponse.CurrentStock`
  bleibt im Namen gleich, bedeutet aber den **eigenen** Bestand des Kindes. Die Projektion in
  `backend/Pugling.Api/Controllers/Student/MeController.cs:416-417` liest ihn samt `Affordable` aus der
  Kind-Zeile. Betroffen sind
  `backend/Pugling.Contracts/Supervisor/ShopDtos.cs:12-54`, `backend/Pugling.Contracts/Student/MeDtos.cs:25-27`,
  `backend/Pugling.Api/Controllers/Student/MeController.cs:416-417`,
  `backend/Pugling.Client/SupervisorApi.cs`, `frontend/src/lib/contract.ts` (neu erzeugt) sowie
  `frontend/src/vater/VaterShop.tsx` und `frontend/src/sohn/SohnShop.tsx`.

### Risiken

1. **Die Migration läuft gegen die echte `pugling.db`.** Die Söhne haben dort echte Käufe und Bestände. Die
   Datenübernahme braucht einen eigenen Test auf einer DB im Altzustand, nicht nur einen auf dem neuen Schema. Vor
   dem Einspielen sichert das Startskript (`scripts/heimnetz.ps1`). Der Probelauf erfolgt auf einer Kopie.
2. **Doppelkauf unter Parallelität.** Die Bestandssperre wandert vom Angebot an die Kind-Zeile. Der Kauf bucht
   zugleich die Geldbörse ab und muss weiter `child.ConcurrencyStamp` hochzählen (Wallet-Invariante). Die beiden
   Sperren dürfen sich nicht widersprechen. Der bestehende Parallelkauf-Test in `ShopFlowTests` wird auf den
   Bestand je Kind umgestellt, nicht gelöscht.
3. **Lazy-Auffüllen schreibt bei einem Lesezugriff.** Das passiert schon heute beim Laden der Liste
   (`ShopService.cs:85`, `:122`). Je Kind entstehen mehr Zeilen, die gleichzeitig geschrieben werden, wenn Vater
   und Sohn den Shop zugleich öffnen. Der vorhandene Ausweg bei einem Konflikt (`TrySaveAsync` +
   `ChangeTracker.Clear`) taugt nur auf dem Vater-Pfad (`ShopService.cs:88-98`), denn der liest nach dem `Clear`
   neu. Der Sohn-Pfad (`ShopService.cs:123-124`) gibt nach dem `Clear` die **ungespeicherten** Objekte zurück. Mit
   der additiven Regel zeigt er dann einen Bestand, den es nicht gibt: angezeigt 4 + 4 = 8, in der DB 4.
   Beide Pfade lesen darum nach einem verlorenen Konflikt neu. Ein Test erzwingt den Konflikt und prüft, dass
   die Antwort dem gespeicherten Bestand entspricht.
4. **Die Nachhol-Rechnung zählt Stichtage**, sie fragt nicht mehr nur „fällig ja/nein“. Fehlerträchtig sind die
   Grenzen: Wochenwechsel in UTC, Monatswechsel, ein `LastRefilledAtUtc` von `null` bei einem neuen Kind und ein
   Kind, das erst nach dem Stichtag verknüpft wurde. Das wird als reine Funktion mit Unit-Tests über die Grenzen
   abgesichert, bevor der Pfad sie benutzt.
5. **Ein neu verknüpftes Kind** (z. B. Oma als zweite Supervisorin, neues Kind) hat noch keine Bestandszeile. Die
   Zeile entsteht beim ersten Lesen nach **derselben** Startregel wie in der Migration (Entscheidung 11), nicht
   pauschal mit `MaxStock`. Sonst startet ein Kind, das einen Tag nach der Migration verknüpft wird, anders als
   seine Geschwister, und ein `Once`-Angebot wäre vor seinem Datum kaufbar. Die Regel gehört darum als eine
   Funktion neben die Stichtag-Rechnung. Migration und Laufzeit benutzen sie gemeinsam, die Migration als
   gleichwertiges SQL, das der Migrationstest gegen die Funktion prüft.
6. **Doku-Capture und OpenAPI-Beispiele** ändern sich mit dem Vertrag (`docs/api-examples`, `openapi-examples`).
   Sie werden neu erzeugt und auf byte-stabile Werte (Zeiten) geprüft.

### Angriffsplan

1. **Fälligkeitsrechnung als reine Funktion:** „wie viele Stichtage seit `last` bis `now`“ für `Daily`,
   `TwiceDaily`, `Weekly` und `Monthly` (am 1.), dazu „nächster Stichtag“. Unit-Tests über die Grenzen, mit roter
   Probe: Der Wochen-Test muss an der heutigen Regel `ShopService.cs:398` fallen.
2. **Schema und Migration:** `ShopListingStock` und `RefillQuantity` anlegen, die Datenübernahme schreiben, die
   alten Spalten entfernen. Migrationstest auf einer Altzustand-DB. `SchemaGuardTests` muss grün bleiben.
3. **`ShopService` umbauen:** Auffüllen je Kind mit Nachholen und Deckel bei `MaxStock`, Kauf gegen die
   Kind-Zeile. Umgebaut werden **alle vier** Auffüll-Stellen: beide Listenpfade (mit Neu-Lesen nach einem Konflikt,
   Risiko 3), der Kauf und die Einzelansicht `ShopController.cs:208`. Die Einzelansicht rechnet weiter ohne zu
   schreiben, jetzt aber je Kind.
4. **Vertrag und Controller:** DTOs, der neue Bestand-Endpunkt, `MeController`-Projektion, Validierung:
   Monatlich ohne Pflichtfeld. Für jede Auffüll-Art außer `None` gilt `MaxStock ≥ 1` und
   `1 ≤ RefillQuantity ≤ MaxStock`. Bei `None` wird `RefillQuantity` nicht geprüft. Dazu `UnitType.Euro` und
   `NextRefillAtUtc`. Danach `Pugling.Client` nachziehen,
   `dotnet build Pugling.sln`.
5. **Seed:** ein Beispiel-Angebot „Taschengeld 5 €“ (`Monthly`, Menge 4, Obergrenze 8). Es kommt nur in den Seed
   für neue DBs. In die echte DB stellt es der Vater selbst ein.
6. **Vater-Web:** Formular mit Wochentag (bei `Weekly`), Datum (bei `Once`) und Auffüllmenge, „Monatlich (am
   1.)“ in der Auswahl, Bestand als Spalte je Kind mit „Bestand füllen“ je Kind, Einheit Euro.
7. **Sohn-App:** „wieder da am …“ an einem leeren Angebot, Euro-Anzeige im Inventar.
8. Nach Schritt 4 der **`pugling-reviewer`**, nach Schritt 7 der **`frontend-reviewer`**.

### Testweg

- **Unit:** neue Testklasse `ShopRefillScheduleTests` für die Stichtag-Rechnung (AK 2, 3, 5 an der Grenze).
- **Integration:** `ShopFlowTests` (heute 39 Fälle) erweitern:
  - zwei Kinder kaufen dasselbe Monatsangebot (AK 1);
  - `TimeProvider` springt ohne Zwischenaufruf über einen Wochen- bzw. Monatsstichtag (AK 2);
  - Ansparen mit den Startbeständen aus AK 3 (2 → 6, 6 → 8, 0 → 4 bzw. 8);
  - Sohn-Liste nach einem erzwungenen Konflikt zeigt den gespeicherten Bestand (Risiko 3);
  - Einzelansicht eines Angebots liefert den Bestand je Kind, ohne zu schreiben;
  - `Affordable` ist `false` bei eigenem Bestand 0, auch wenn ein Geschwisterkind noch Bestand hat (AK 9);
  - neu verknüpftes Kind und neues Angebot starten nach Entscheidung 11, `Once` vor dem Datum mit 0 (AK 10);
  - Altverhalten bei `RefillQuantity = MaxStock` (AK 4);
  - Euro-Kauf und Einlösung (AK 8);
  - `NextRefillAtUtc` gesetzt bzw. `null` bei `None` (AK 9).
- **Migration:** ein Test auf einer DB im Schema vor der Migration mit zwei Kindern (AK 7). Die Fälle:
  - Ein `Daily`-Angebot mit `MaxStock` 3, und Kind A hat seit dem letzten Auffüllen einmal gekauft
    (Familienbestand 2). Erwartet: Kind A hat 2, Kind B hat 3. Eine Migration, die den Familienbestand kopiert,
    macht Kind B rot.
  - Ein `Once`-Angebot, schon ausgelöst und von Kind A leergekauft. Erwartet: Kind B hat `MaxStock`.
  - Ein `Weekly`-Angebot mit `MaxStock = 0`. Erwartet: `RefillKind = None`, und ein PATCH, der nur den Preis
    ändert, antwortet `200`.
  Dazu ein Probelauf auf einer Kopie der echten `pugling.db`. Er zählt nach, ob es dort `MaxStock = 0` gibt.
- **E2E:** eine neue Spec `frontend/e2e/taschengeld-monatlich.spec.ts` im Muster von `shop-verlauf.spec.ts`. Ablauf:
  Vater legt „Taschengeld 5 €, monatlich, Menge 4, Obergrenze 8“ und ein wöchentliches Angebot mit Wochentag an
  (AK 6, kein `400`). Sohn A kauft, Sohn B sieht weiterhin vollen Bestand (AK 1). Sohn A kauft leer und sieht
  „wieder da am 1. …“ (AK 9). Diese Spec gilt zugleich als Rollengang.
- **`/smoke-test`** gegen eine Wegwerf-DB nach Schritt 4.

## Verlauf

- **2026-11-17** — angelegt und ausformuliert (Quelle: Vater im Chat). Belegt: Der Auffüll-Plan sitzt schon an
  jedem Angebot. Er hakt an vier Stellen: Wöchentlich holt nicht nach, das Formular hat kein Wochentag-Feld, der
  Bestand gilt familienweit, `TwiceDaily` schaltet in UTC um. `Monthly` und eine Geld-Einheit fehlen. Echte DB
  nur auf einer Kopie gelesen.
- **2026-11-17** — Punkt 3 vom Vater entschieden: Bestand je Sohn (Entscheidung 1). Die übrigen Punkte sind
  offen, Stufe bleibt `ausformuliert`.
- **2026-11-17** — gegrillt mit dem Vater: alle zehn Punkte entschieden (Punkt 9 zurückgestellt). Kern: Shop-Angebot,
  Teilbeträge in Euro, Bestand je Sohn, ein Auffüll-Modell „Menge je Stichtag bis Obergrenze“ mit Nachholen,
  monatlich immer am 1.
- **2026-11-17** — geschätzt: L, beides, Migration ja (Bestand je Kind als eigene Tabelle, Datenübernahme aus der
  echten DB), Vertragsbruch ja (Bestand je Kind im Vater-DTO, eigener Bestand-Endpunkt). Testweg:
  `ShopRefillScheduleTests`, `ShopFlowTests`, Migrationstest, E2E `taschengeld-monatlich.spec.ts`.
- **2026-11-17** — Code-Review der Schätzung, acht Funde eingearbeitet, Stufe bleibt `geschaetzt`. Neu ist
  Entscheidung 11: Jedes Kind startet mit `MaxStock` minus seinen eigenen Käufen seit dem letzten Auffüllen.
  Das gilt in der Migration, für ein neu verknüpftes Kind und für ein neues Angebot. Zuvor hätte die Migration
  einen leergekauften `Once`- oder `None`-Bestand an alle Geschwister vererbt. Weiter eingearbeitet:
  - `MaxStock = 0` mit Auffüll-Art wird zu `None`, damit die neue Validierung keinen Altbestand bricht;
  - `CreateShopListingDto.CurrentStock` entfällt;
  - die vierte Auffüll-Stelle `ShopController.cs:208` und `MeController.cs:416-417` sind ergänzt;
  - der Sohn-Pfad liest nach einem Konflikt neu;
  - AK 3 nennt jetzt Startbestände, die die heutige Regel rot machen;
  - AK 10 ist neu: der Startbestand eines neuen Kindes und eines neuen Angebots.
  Entscheidung 11 ist aus dem Review abgeleitet und beim Rollengang vom Vater zu bestätigen (bestätigt 2026-11-19).
- **2026-11-19** — Vater im Chat: Shops müssen pro Kind sein (Taschengeld 10 € vs. 20 €, Daddelzeit 1 h täglich je
  Kind, kein Wegkaufen unter Geschwistern). Am Code erneut bestätigt: der Bestand ist weiter familienweit
  (`ShopService.cs:147`, `:174`). Das Wegkaufen deckt Entscheidung 1 ab, die unterschiedliche Höhe nicht → offener
  Punkt 12. Vom Vater entschieden: Angebot je Kind über eine Zielgruppe am Angebot (Entscheidung 12, AK 11). Die
  Größe bleibt L: eine Join-Tabelle, ein DTO-Feld und ein Filter an den Pfaden, die B-645 ohnehin umbaut.
  Stufe bleibt `geschaetzt`.
- **2026-11-19** · in-arbeit · Sprint 96 gebaut (Backend, Vater-Web, Sohn-App), Entscheidung 12 eingearbeitet. Belege je AK-Fall
  (Mutanten aus `.claude/scripts/mutant-probe.py` nach der letzten Bearbeitung, alle am genannten Test rot):
  - AK 1, Geschwister kaufen je aus eigenem Bestand → `ShopStockPerChildTests.cs:74`, A1 (Kauf senkt nicht) → rot.
  - AK 2 wöchentlich, Montag verpasst → `ShopStockPerChildTests.cs:129` + `ShopRefillScheduleTests.cs:19`, A3 (alte Regel „heute ist der Tag“) → rot.
  - AK 2 + AK 5 monatlich, nur am 1., nachgeholt → `ShopStockPerChildTests.cs:109` + `ShopRefillScheduleTests.cs:34`, A4 (Tage statt Monate) → rot.
  - AK 3 Ansparen 2→6 / 6→8 / 0→8 / 0→4 → `ShopStockPerChildTests.cs:155` + `ShopRefillScheduleTests.cs:72`, A5 (kein Deckel), A6 (alte Regel „auf Max“), A7 (Nachholen als ja/nein) → je rot.
  - AK 4 Verfallen wie bisher → Migration setzt `RefillQuantity = MaxStock`, `ShopStockPerChildMigrationTests.cs:27`, G4 → rot.
  - AK 6 Vater-Web legt Wöchentlich mit Wochentag, Monatlich (+4 bis 8), Einmalig mit Datum an und ändert den Wochentag ohne `400` → `taschengeld-monatlich.spec.ts:33`, F3 (Menge fällt weg), F4 (Wochentag fällt weg) → rot.
  - AK 7 Migration, Startbestand je Kind, Käufe bleiben, `MaxStock = 0` → None → `ShopStockPerChildMigrationTests.cs:27`, G1 (fremde Käufe zählen), G2 (Käufe vor dem Auffüllen zählen) → rot; Probelauf: real eingespielt (siehe Vorfall im Sitzungslog), gegen die Sicherung geprüft.
  - AK 8 Euro im Inventar, Auszahlung erkennbar → `ShopStockPerChildTests.cs:385`.
  - AK 9 Affordable nach eigenem Bestand, „wieder da“ → `ShopStockPerChildTests.cs:74` (A2 → rot) und E2E (F1 → rot).
  - AK 10 neues Kind und Einmalig vor dem Termin → `ShopStockPerChildTests.cs:192` (A8 → rot); Einmalig gleich für früh und spät gelesene Geschwister → `ShopStockPerChildTests.cs:225` (A9 → rot).
  - AK 11 Zielgruppe in Liste und Kauf, fremdes Kind 400 → `ShopStockPerChildTests.cs:250`, A10 (Filter weg), A11 (fremdes Kind angenommen) → rot.
  - Bestand setzen: Spanne, Zielgruppe → `ShopStockPerChildTests.cs:317`, A12, A13 → rot. Validierung/PATCH-Regel → `ShopStockPerChildTests.cs:351`, A15, A16, A17 → rot. Risiko 3 (Konflikt, Sohnliste liest neu) → `ShopStockPerChildTests.cs:415`, A14 → rot.
  - A18 (eigene Käufe in der Startregel zur Laufzeit) bleibt **grün**: zur Laufzeit entsteht eine Zeile nie nach eigenen Käufen (Käufe brauchen die Zeile; Zeilen bleiben beim Herausnehmen aus der Zielgruppe). Getragen wird die Regel dort, wo sie wirkt – in der Migration (G1, G2).
- **2026-11-19** · in-arbeit · Review (`pugling-reviewer`, `frontend-reviewer`) eingearbeitet: Einmalig füllt bis zur Obergrenze, PATCH der Obergrenze kappt keine Spar-Menge mehr, negative Menge 400, zwei Meldungen beim Bestand-Setzen, „Für“-Legende ohne `<label for>`, Merker `expires` beim Tippen, Prüfung übers Panel-Banner. Verifikation nach der letzten Bearbeitung: `dotnet test Pugling.sln -c Release` 1920/1920, Vitest 1258/1258, `tsc -b` sauber, Mutanten 32/32 wie erwartet, E2E 8 passed.
- **2026-11-19** · in-arbeit · Gebaut, verifiziert und gecommittet (`08fd05a6`), Rollengang Vater und Sohn grün. **Offen für `abgenommen`:** die Bestätigung von Entscheidung 11 durch den Vater (so in der Story verlangt). Konkret: Ein neues Spar-Angebot (+4 bis 8) ist am ersten Tag achtmal kaufbar, weil jedes Kind mit der Obergrenze startet.
- **2026-11-19** · abgenommen · `08fd05a6`. Der Vater bestätigt Entscheidung 11 im Chat („Ja, voll starten“): Ein neues Angebot startet für jedes Kind voll, auch ein Spar-Angebot (+4 bis 8 startet mit 8). Rollengang Vater und Sohn grün (siehe Sitzungslog, Runde 2). Gerätecheck offen: ob „wieder da: …“ auf der Karte am Handy Platz hat.
