---
tags: [typ/story, status/verworfen, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Münz-Verhältnis, Münzen zu wenig]
status: verworfen
prio: P1
art: Frage
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Feedback der Söhne, vom Vater weitergegeben am 2026-11-21 (Sitzung)"
grund: "Beantwortet (Vater, 2026-11-26): die Menge bleibt – gemessen großzügig; der Hebel ist das Gefühl → B-1032. Theodors Truhe ist kein Defekt: an der Kopie zwei Truhen (21. und 22.11.), keine an Tagen mit offener Latein-Pflicht – die Regel alle Tagespflichten."
ersetzt_durch: [B-1032]
---

# B-780 · Die Söhne finden, es gibt zu wenig Münzen: Verhältnis Übung zu Münzen nachmessen

Rückmeldung der Söhne: was man an Münzen bekommt, ist zu wenig. Ob das stimmt, ist eine Rechnung, keine
Meinung: Münzen je Lernminute bzw. je Lerntag, gemessen an einer **Kopie** der echten `pugling.db`, gegen die
Preise im Familien-Shop. Verwandt: [B-535](B-535-belohnungen-erreichen-friedrich-nicht.md),
[B-538](B-538-bonus-muenzregeln-form-und-degression.md), Sparziel [B-593](B-593-sparziel-im-familien-shop.md),
Zufallsboxen [B-779](B-779-mehr-zufall-und-arcade-mechaniken.md), Lernzeit [B-781](B-781-lernzeit-anzeige-mit-aktivitaetsnachweis.md).

## User Story

Als Vater möchte ich wissen, wie viele Münzen eine Lernminute meinen Söhnen einbringt und was sie dafür im Shop
bekommen, damit ich das Verhältnis bewusst einstelle – und nicht auf ein Gefühl hin Sätze oder Preise drehe.

## Ist-Stand am Code

Woher Münzen kommen (alle Sätze je Position gleich, bei beiden Söhnen `NewContentPoints` 10, Combo 5/5):

- **Je richtige Antwort** `ScoringService.BasePoints` (`backend/Pugling.Api/Services/Shared/ScoringService.cs:131`):
  neues Wort = `NewContentPoints` (10), Wiederholung = `max(2, 8 − Box)`, beides × Tageszeit-Faktor
  (`appsettings.json:17`: vormittags 1,5, abends 0,8).
- **Selbsteinschätzung „gewusst"** zahlt pauschal **1 Münze** (`ScoringService.cs:93`, `ClaimCoins`, B-657).
- **Combo** ab 5 Treffern in Folge +5 (`ScoringService.cs:79`).
- **Pflicht erfüllt** `PointsGoalMet` je Position (20 bzw. 30), gebucht in `PositionProgressService.cs:544-560`.
- **Tagestruhe** 10–30 Münzen, 0–2 Gems, Serien-Faktor (`appsettings.json:24-26`) – nur wenn die Tagesziele
  **aller** aktiven Pläne erfüllt sind (`DailyBoxService.cs:38`, `RewardsUnlocked`, B-606/B-611).
- **Bonus-Training** 1/1/3 je Treffer, Tagesdeckel 90 (`appsettings.json:37`).
- **Missionen**, Auszeichnungen, Verschenken (`Manual`); dagegen der Malus `GoalPenalty`.

### Messung an der Kopie vom 2026-11-21

Kopie per SQLite-Backup (schreibgeschützt gelesen), Ledger `ChildPointsEntries`, Lernzeit aus
`PracticeSessions.ActiveSeconds`. Gezählt sind verdiente Münzen ohne Shop, Startguthaben und Malus.

| Kind | Zeitraum | Münzen verdient | aktive Minuten | Münzen/Minute | Münzen/Lerntag |
| --- | --- | --- | --- | --- | --- |
| Theodor (Child 1) | 17.–20.11., 4 Tage | 495 | 46 | **≈ 11** | ≈ 124 |
| Friedrich (Child 3) | 18.–20.11., 3 Tage | 1 945 | 79 | **≈ 25** | ≈ 650 |

Shop (`ShopListings`): 10 Min TV 50 · 30 Min TV 130 · 30 Min Zocken 200 · 60 Min Zocken 350 · „Spielzeit"
100 · Taschengeld 1 000 + 10 Gems (monatlich). Gekauft seit 18.11.: Theodor 2 × 30 Min Zocken, Friedrich
7 Angebote für 1 400 Münzen. Saldo heute: Theodor 394, Friedrich 797.

Umgerechnet auf „30 Min Zocken" (200): **Theodor ≈ 19 Lernminuten oder 1,6 Lerntage, Friedrich ≈ 8
Lernminuten**. Nach dieser Rechnung bekommen beide **mehr** Spielzeit, als sie lernen.

Was die Zahl relativiert:

- **Die Minuten sind Verweildauer, nicht aktive Zeit.** Das Frontend meldet pauschal `active: true`
  (`frontend/src/sohn/SohnPractice.tsx:155`, Befund aus B-781). Die echten Münzen je aktiver Minute liegen
  also eher **höher**.
- **Die häufigste Buchung ist „+1".** Bei Theodor sind seit dem 17.11. 23 von 49 Grund-Buchungen die
  Selbsteinschätzung zu 1 Münze, bei Friedrich 110 von 260. Was das Kind am häufigsten aufleuchten sieht,
  ist also eine einzelne Münze.
- **Theodor hat noch nie eine Tagestruhe bekommen** (`DailyBoxClaims` leer für Child 1); Friedrich hat vier
  geöffnet. Theodor hat zwei aktive Pläne mit Tagespflicht (Englisch, Latein). Die naheliegende Ursache ist,
  dass er nie an einem Tag die Pflicht **beider** Pläne erfüllt hat; das ist **nicht geprüft**.
- **Kleine Stichprobe**: drei bis vier Lerntage je Kind, mitten in der Klausurphase (Pläne seit 17.11.).

## Die echte Lücke

Das Verhältnis Lernzeit zu Shop-Wert ist nicht knapp; gemessen ist es großzügig. Wenn sich die Münzen
trotzdem nach „zu wenig" anfühlen, liegt das nach dieser Messung an der **Wahrnehmung** (die sichtbare
Einheit ist „+1", die Summe des Tages sieht das Kind nicht) und bei Theodor an der **Truhe, die nie kommt**,
nicht an den Sätzen. Es fehlt außerdem eine Stelle, an der der Vater das Verhältnis sieht: die Rechnung oben
gibt es nur als Handmessung.

## Akzeptanzkriterien (Entwurf)

1. Die Messung ist wiederholbar (Skript oder Vater-Sicht), nicht nur diese einmalige Handrechnung.
   *Rot, wenn* eine zweite Messung in einer Woche wieder von Hand gebaut werden muss.
2. Die Ursache, warum Theodor keine Truhe bekommt, ist am Code belegt; ist sie ein Defekt, liegt sie als
   eigene Story `art: Defekt` vor.
3. Je nach Entscheidung in der Grill-Runde: die gewählte Maßnahme (Sichtbarkeit, Sätze oder Preise) ist
   gebaut, und eine zweite Messung nach einer Woche zeigt, ob die Söhne sie bemerken.

## Offene Punkte

1. **Was ist der Maßstab?** Empfehlung: „Münzen je aktiver Lernminute" gegen „Münzen je Spielminute im
   Shop". Der Vater legt einen Zielwert fest (z. B. eine Lernminute ergibt eine Spielminute), gegen den
   künftig gemessen wird.
2. **Menge oder Gefühl?** Empfehlung: zuerst das Gefühl. Eine Tagessumme sichtbar machen („heute +178 🪙"),
   die Gutschrift animieren, kleine Beträge bündeln. Die Sätze bleiben, denn gemessen ist das Verhältnis
   schon großzügig. Vorher die Söhne fragen, *wann* es sich wenig anfühlte: beim Kaufen, beim Üben oder im
   Vergleich miteinander.
3. **Theodors Truhe:** gewollte Härte (Regel „alle Pläne", B-611) oder Defekt? Empfehlung: erst an der Kopie
   klären; bei zwei Fächern mit Tagespflicht ist die Regel für ihn deutlich härter als für einen Sohn mit
   einem einzigen Plan.
4. **Ungleichheit zwischen den Söhnen** (≈ 11 gegen ≈ 25 Münzen je Minute): gewollt? Empfehlung: nicht
   angleichen. Sie kommt aus dem Tempo (Friedrich spielt mehr Karten je Minute) und aus den neuen Wörtern.
   Erst vergleichen, wenn die Minuten aktiv gemessen werden (B-781).
5. **Welcher Hebel, falls doch nachgeregelt wird?** Empfehlung: nicht die Shop-Preise senken, sie sind der
   reale Gegenwert des Vaters. Eher die +1 der Selbsteinschätzung anheben oder durch Zufallsbeträge ersetzen
   (Brücke zu B-779).
6. **Wo lebt die Messung?** Empfehlung: zuerst ein Skript unter `scripts/` gegen eine DB-Kopie. Eine
   Vater-Sicht erst, wenn die Kennzahl sich bewährt hat; [B-662](B-662-vater-sicht-arbeit-tagesziel-kaeufe.md)
   ist der Ort dafür.

## Entscheidungen

Vom Vater am 2026-11-26:

1. **Gefühl, nicht Menge** (Offener Punkt 2): Sätze und Shop-Preise bleiben. Eine sichtbare Tagessumme und gebündelte kleine
   Beträge kommen als eigene Story [B-1032](B-1032-muenzen-tagessumme-in-der-arcade.md); vorher werden die Söhne gefragt, wann es
   sich nach wenig anfühlte.
2. **Theodors Truhe erst prüfen** (Offener Punkt 3) – geprüft an einer Kopie der echten DB (Stand 2026-11-26): Theodor hat zwei
   Truhen bekommen (21.11., 22.11.). Am 20. und 24.11. war Englisch voll erfüllt, Latein nicht – keine Truhe, wie die Regel „alle
   Tagespflichten“ (B-606/B-611) es will. Latein war an 1 von 8 Pflichttagen erfüllt (Auswertung B-733). Kein Defekt; der Hebel ist
   Latein, nicht die Truhe.
3. Maßstab, Ungleichheit, Ort der Messung (Punkte 1, 4, 6): nicht entschieden, weil nichts nachgeregelt wird; die Messung liest
   der Vater künftig in der Auswertung (B-733).

1. **Vorerst wird nur festgehalten, dass die Söhne mit der Münzausbeute unzufrieden sind; es gibt keinen
   Zielwert.** (Vater, 2026-11-21, auf Punkt 1.) Begründung: Das Gefühl der Kinder ist der Befund, eine
   Kennzahl soll noch nicht festgelegt werden. Kosten: Ohne Maßstab lässt sich „zu wenig“ nicht prüfen. Eine
   Maßnahme wäre heute Bauchgefühl, darum bleiben die Punkte 2 bis 6 ungegrillt und die Story auf
   `ausformuliert`.

## Verlauf

- **2026-11-21** — angelegt (Quelle: Kinder-Feedback, vom Vater in der Sitzung weitergegeben).
- **2026-11-21** — `idee` → `ausformuliert`: Münzquellen am Code belegt, Ledger und Lernzeit an einer Kopie der
  echten DB gemessen (Theodor ≈ 11, Friedrich ≈ 25 Münzen je Minute; 30 Min Zocken = 200). Befund: das
  Verhältnis ist großzügig, die Lücke liegt in Wahrnehmung („+1") und Theodors fehlender Truhe.
- **2026-11-21** — Grill-Runde nach Punkt 1 vom Vater angehalten: vorerst nur die Unzufriedenheit festhalten
  (Entscheidung 1). Bleibt `ausformuliert`.
- **2026-11-25** — AK 2 an einer Kopie geklärt (PM-Sitzung 2026-11-25, Runde 4): Theodor hat am 21. und 22.11. je eine Truhe
  bekommen, an beiden Tagen waren die Tagesziele aller Pläne erfüllt (4 von 6, offen nur Wochenziele). Kein Defekt, die Regel
  aus B-611 greift. Bleibt `ausformuliert`, die Punkte 2–6 warten auf den Vater.
- **2026-11-26** — beantwortet und `verworfen` (Erfolg einer Frage): Entscheidungen oben, Folgestory B-1032.
