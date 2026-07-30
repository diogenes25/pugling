---
tags: [typ/plan, bereich/qualitaet, bereich/tests]
aliases: [Testaudit-Nacharbeit, Restliste Defektinjektion]
---

# Nacharbeit zur Defektinjektion

Status: **offen** (angelegt 2026-07-30, Basis `feafd7d`).

## Einstieg für eine frische Sitzung

Diese Seite ist die **vollständige Übergabe** der Reste aus der Messung in
[testplan.md](testplan.md) – dort steht der Befund mit allen 30 Injektionen, den Quoten und den
Fundstellen. **Nicht neu messen.** Hier steht nur, was bewusst offen blieb, warum, und wie man es angeht.

Was dort schon **erledigt** ist (nicht erneut anfassen): die vier Lücken mit Geldwirkung sind geschlossen und
per Gegenprobe belegt (`Test_ErgebnisGenauAufDerSchwelle_IstBestanden`, der Tag-Beleg in
`Vater_DarfFremdenTagNachtragen`, die Status-Prüfung in `PositionGoalOverviewTests`, die Untergrenze in
`ErrorCodeTests`). Der Referenzstand ist **588/588 grün, 268/268 Actions, CI grün**.

Kernaussage der Messung, die die Priorisierung hier trägt: die dominierende Fehlerklasse ist **nicht**
„Regel ungetestet", sondern **„Regel getestet, Grenzfall offen"**. Wer hier arbeitet, sucht also nicht neue
Testflächen, sondern **Ränder** an bereits getesteten Regeln.

## Wichtig: die Reihenfolge ist begründet, nicht Geschmack

Etappe 1 ist ein **Produktivcode-Fund** – der einzige in dieser Liste. Alles andere sind Tests. Wer sie
umdreht, schreibt Tests gegen ein Verhalten, das sich in Etappe 1 noch ändert.

---

## Etappe 1 · Der Produktivcode-Fund: der Reward-Pfad verschluckt seinen Nebenläufigkeits-Konflikt nicht

**Das ist der einzige Punkt hier, der echte Nutzer trifft.** Gefunden als Nebenwirkung von Injektion D13,
gemeldet statt nebenbei gefixt (dieselbe Trennung wie beim `CancellationToken`-Umbau).

Die beiden Idempotenz-Pfade in [PositionProgressService.cs](../backend/Pugling.Api/Services/Shared/PositionProgressService.cs)
sind **unterschiedlich robust**, obwohl beide dieselbe Garantie tragen sollen:

| Pfad | Existenz-Check | Unique-Index | `catch (DbUpdateException)` |
|---|---|---|---|
| Malus – `SettleClosedPeriodsAsync` | Zeile 213 | ja | **ja**, Zeile 242 ff. |
| Reward – `EvaluateAndAwardAsync` | Zeile 133 | ja | **nein**, Zeile 145 |

Folge: zwei *gleichzeitige* Zielabschlüsse derselben Periode (Doppeltipp, React-StrictMode-Doppelaufruf, zwei
Tabs) laufen beim Reward in den Unique-Index `(PlanPositionId, PeriodKey)` und der Verlierer bekommt einen
**500**, obwohl der fachliche Zustand vollkommen in Ordnung ist – die Belohnung liegt ja. Beim Malus ist genau
dieser Fall als „gutartig, schon erledigt" ausformuliert und wird geschluckt.

**Zu tun:** `EvaluateAndAwardAsync` dieselbe Behandlung geben wie `SettleClosedPeriodsAsync` – Konflikt
fangen, `ChangeTracker.Clear()`, den aktuellen Tages-Status zurückgeben. Der Kommentar dort muss sagen
*warum* das kein Fehler ist, nicht *dass* gefangen wird.

**Vorher entscheiden (offene Frage):** Ist der 500 je real aufgetreten? Wenn ja, wäre die Reihenfolge
umgekehrt (erst fixen, dann alles andere). Ein Blick in die Logs/Anmerkungen (`api/v1/remarks`) vor dem Umbau
kostet fünf Minuten und beantwortet es.

**Nachweis:** ein Test, der zwei Submits desselben Versuchs *nebenläufig* abschickt und beide auf
`< 400` prüft, plus weiterhin genau **eine** `PositionGoalReward`-Zeile. Ohne den nebenläufigen Fall wäre der
Test wertlos – der sequenzielle Weg ist längst grün (das ist die in Commit 2 ergänzte Zeile).

---

## Etappe 2 · Die benannte Restliste: sechs unbewachte Regeln ohne Geldwirkung

Alle sechs sind aus [testplan.md](testplan.md) übernommen, nach **Schadenshöhe** sortiert. Jede Zeile ist
eine Injektion, die grün blieb – die Fundstelle ist also belegt, nicht vermutet. Zeilennummern gegen
`feafd7d` geprüft.

| Rang | Stelle | Was unbemerkt durchgeht | Testansatz |
|---|---|---|---|
| 1 | **B02** [MediaSelector.cs:120](../backend/Pugling.Api/Services/Shared/MediaSelector.cs) | „Anderes Bild" darf den **einzigen** Kandidaten verbrennen → die Karte ist für dieses Kind **dauerhaft** bildlos, ohne Weg zurück über die API. Genau der Schaden, den der Kommentar dort abwenden will. | Träger mit **genau einem** zulässigen Bild, Reshuffle anfordern → `409 media_no_alternative`, **und** danach zeigt die Karte weiter dasselbe Bild. Der zweite Teil ist der eigentliche Test. |
| 2 | **D07** [MediaSelector.cs:275](../backend/Pugling.Api/Services/Shared/MediaSelector.cs) | Der `StableTiebreak` darf durch `Random` ersetzt werden. Kein Test erzeugt einen **Punktgleichstand**, also ist die dokumentierte Determinismus-Zusage unbewacht – und Bildkonstanz *ist* laut `CLAUDE.md` der Merkeffekt. | Zwei Bilder mit **identischem** Score und identischem `Weight` an einem Träger, Wahl über mehrere Abrufe **und** über einen Neustart der Factory hinweg gleich. Der Neustart ist der Punkt (`string.GetHashCode` ist pro Prozess randomisiert). |
| 3 | **D15** [CreatorProfileService.cs:18](../backend/Pugling.Api/Services/Creator/CreatorProfileService.cs) | `WeightSeries` darf von 8 auf 4 fallen, also auf `WeightSubject`. Die dokumentierte Rangfolge **Reihe 8 > Fach 4 > Stufe 2 > Schulart 1** ist nirgends festgenagelt; geprüft ist nur, *dass* ein Profil gewinnt. | Zwei Profile so bauen, dass eines **nur** über die Reihe punktet und das andere **nur** über das Fach → das Reihen-Profil muss gewinnen. Analog ein Paar für Stufe gegen Schulart. |
| 4 | **B12** [ShopService.cs:177](../backend/Pugling.Api/Services/Shared/ShopService.cs) | `ShopPurchase.Title` kann leer werden, wenn das Angebot keinen eigenen Titel trägt – der Vater sieht in der Kaufhistorie eine namenlose Zeile. | Angebot **ohne** eigenen Titel kaufen → der Kauf trägt den Artikel-Titel. Ein zweiter Fall mit eigenem Angebots-Titel dazu, sonst ist die Fallunterscheidung nicht gepinnt. |
| 5 | **B08** [PositionProgressService.cs:228](../backend/Pugling.Api/Services/Shared/PositionProgressService.cs) | Der Malus-Buchungstext darf „Tagesziel" und „Wochenziel" vertauschen. Sichtbar im Punkte-Verlauf des Kindes, keine Buchung falsch. | `PflichtMalusTests` um eine **Wochen**-Position erweitern und beide `Reason`-Texte prüfen. Heute deckt die Klasse nur den Tages-Rhythmus ab. |
| 6 | **B06** [PositionProgressService.cs:129](../backend/Pugling.Api/Services/Shared/PositionProgressService.cs) | Positionen mit `PointsGoalMet == 0` dürfen eine Reward-Zeile und eine Ledger-Buchung über **0** Münzen bekommen. Saldo unverändert, aber Rauschen in Verlauf und Auswertung. | Position mit `pointsGoalMet: 0` und erreichtem Ziel → **keine** `PositionGoalReward`-Zeile, **kein** `ChildPointsEntry`. |

**Bewusst nicht in dieser Liste:** die fünf grünen Injektionen, bei denen eine **zweite Schranke** hält
(D03/D13 über den Unique-Index, B07 über `PlanDueForPeriod`, B01 über `SaveFreezeAsync`). Sie sind
Tiefenverteidigung, keine Lücke – siehe die Begründung in [testplan.md](testplan.md), Abschnitt „(b)".
Wer sie trotzdem „schließt", schreibt Tests gegen redundante Vorab-Optimierungen.

**Rangfolge ist Empfehlung, nicht Zwang.** 1–3 lohnen sich; 4–6 sind Kosmetik am Rand und dürfen auch
liegen bleiben, wenn Wichtigeres ansteht. Was fallen gelassen wird, gehört hier als erledigt-oder-verworfen
markiert, nicht stillschweigend gestrichen.

---

## Etappe 3 · Der wanduhr-abhängige Test

[SpeedBonusTests.cs:59](../backend/Pugling.Api.Tests/SpeedBonusTests.cs)
`ZuSchnelleAntwort_UnterAntiCheatUntergrenze_BringtKeinenBonus` setzt voraus, dass zwei aufeinanderfolgende
Antworten **unter** 1 s auseinanderliegen (die Anti-Farming-Untergrenze `MinSpeedSeconds`). Unter Last reißt
das: der Test fiel bei zwei Injektionen mit, die `ShopService` betrafen und mit Bonuspunkten nichts zu tun
haben – beobachtet bei drei parallel laufenden Suiten auf 20 Kernen.

Kein Produktfehler, sondern eine Fragilität des Tests. Auf einem langsamen oder ausgelasteten CI-Runner wird
sie zum Flake, und ein Flake an dieser Stelle ist besonders teuer: er sieht aus wie ein Punkte-Regress.

**Zu tun:** die gemessene Zeit **einspeisen** statt sie zu erwarten. Der Server misst sie serverseitig
(Abstand zur letzten Antwort), also braucht es eine Naht – eine `TimeProvider`-Injektion im Testhost oder ein
Weg, den Zeitstempel der Vorgänger-Antwort zu setzen. **Offene Entwurfsfrage**, und die einzige hier: welche
Naht ist die kleinste, ohne dem Produktivcode eine Testschnittstelle aufzudrängen? Der zweite Test derselben
Klasse (`SchnelleAntwort_ImZeitfenster…`) nutzt `await Task.Delay(1200)` und hat das Problem in umgekehrter
Richtung – er wird nur langsamer, nicht falsch. Beide gehören auf dieselbe Naht.

---

## Etappe 4 · Der vorgeschlagene reflexive Wächter (Entscheidung nötig, nicht nur Arbeit)

Ein Muster trat mehrfach auf und lässt sich mechanisch fassen statt mit Einzeltests – die im Projekt
etablierte Antwort („mechanische Tore statt Disziplin"): **ein Test, der auf einem Schreibpfad einen
Erfolgsstatus zusichert und den Effekt nirgends nachliest.** Das ist die Fehlerklasse hinter D11, also
belegt und nicht theoretisch.

**Stand:** 7 Altlasten (D11s Vertreter ist in Commit 2 behoben):

- `AntiCheatTests.Vater_DarfInaktivenPlanTrotzdemDurchspielen`
- `EmptyExerciseGuardTests.GefuellteVokabeluebung_LaesstSichWeiterZuweisen`
- `EmptyExerciseGuardTests.ErstAnlegenDannFuellen_BleibtMoeglich`
- `EmptyExerciseGuardTests.Aufsatz_OhneItems_BleibtZuweisbar`
- `ExerciseGrantsTests.OeffentlicheUebung_BleibtFuerFremdeZuweisbar`
- `ExerciseGrantsTests.Admin_DarfFremdeUebungAendernUndLoeschen`
- `ExerciseGrantsTests.Admin_KannVerwaisteUebungBearbeiten_AutorNichtMehr`

**Warum er in der Messung bewusst nicht scharf gestellt wurde:** der Plan sagt „kein Umbau der Suite", und
die Heuristik ist noch zu grob – sie fand 81 Rohtreffer, davon 25 nach Filterung, davon 8 echte. Ein Tor mit
dieser Trefferquote produziert Ausnahmen statt Qualität.

**Die eigentliche Entscheidung lautet darum nicht „bauen oder nicht", sondern:** lassen sich die 25 auf die 8
verengen, ohne eine Ausnahmeliste zu pflegen? Wenn nein, ist der Wächter die falsche Antwort und die sieben
Altlasten werden schlicht einzeln repariert (eine Zeile je Test) – das ist billiger als ein Tor, das man
ständig beschwichtigt. Erst messen, dann entscheiden; genau die Haltung aus
[codequalitaet-gates-plan.md](codequalitaet-gates-plan.md).

Die Heuristik, mit der die Zahlen entstanden sind (zum Weiterschärfen, nicht zum Übernehmen):

- Methode trägt `[Fact]`/`[Theory]` **und** enthält `PostAsJsonAsync|PatchAsJsonAsync|PutAsJsonAsync|DeleteAsync|PostAsync(`
- erwartet mindestens einen **Erfolgs**status (`HttpStatusCode.OK|Created|NoContent|Accepted`)
- enthält **kein** `GetProperty|ReadFromJsonAsync<|EnumerateArray|GetArrayLength|JsonAssert.|scope.ServiceProvider`
- **Falsch-Positive, die verengt werden müssen:** Effekt über Folgestatus belegt (`204` → `404`, `201` → `409`)
  und Effekt über einen Id-Vergleich belegt (`Assert.Equal(await TestApi.IdAsync(a), await TestApi.IdAsync(b))`
  – daran scheiterte die erste Fassung).

Wenn er gebaut wird: als **Zuwachs-Sperre mit Baseline** wie der `CancellationToken`-Wächter es war, nicht
sofort hart – sonst blockt er die sieben Altlasten am ersten Tag.

---

## Was ausdrücklich **nicht** hierher gehört

- **Das Azure-Deploy.** `deploy-azure.yml:66` erwartet `secrets.AZURE_WEBAPP_PUBLISH_PROFILE`, im Repo ist
  kein Secret gesetzt (`No credentials found`). Aufgedeckt, weil CI erstmals grün war und das Deploy zum
  ersten Mal wirklich anlief statt `skipped` zu bleiben. **Der Eigentümer hat es übernommen** – nicht
  anfassen, auch nicht „hilfsweise" den Workflow umbauen.
- **Neue Messungen.** Der Befund steht mit Datei:Zeile in [testplan.md](testplan.md). Eine zweite Runde
  Injektionen bringt nichts Neues, solange diese Liste nicht abgearbeitet ist.
- **Die Abdeckungsquote.** 69 % Zweigabdeckung ist keine Zielgröße; die Messung hat gerade gezeigt, dass
  98 % Zeilenabdeckung und 268/268 erreichte Actions nichts über Empfindlichkeit sagen.

## Verwandt

- [testplan.md](testplan.md) – der Befund, aus dem diese Liste stammt (Quoten, alle 30 Injektionen)
- [codequalitaet-gates-plan.md](codequalitaet-gates-plan.md) – die Tore und die „erst messen"-Haltung
- [backlog-vokabellernen.md](backlog-vokabellernen.md) – eigene Spur; enthält den P1-Defekt
  „Test friert unsichtbare Bildwahlen ein", der dieselbe `MediaSelector`-Ecke berührt wie Rang 1 und 2 hier
