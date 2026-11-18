---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/student]
aliases: [Alles gibt Münzen, Zusatzübungen mit Münzen]
status: abgenommen
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Vater am 2026-11-18 beim Ansehen des Pfads (Testjunge, Child 4)"
grund: ""
ersetzt_durch: []
---

# B-657 · Jede Übung gibt Münzen – auch die Zusatzübungen ohne Pflicht

Der Vater am 2026-11-18: „Alles muss Münzen geben.“ Die Startseite zeigt die Fächer seit dem Umbau als
aufklappbare Reiter mit „🪙 noch N“ für die offenen Ziele und die freien Positionen getrennt als
„Zusatzübungen“ – der Gedanke dahinter: wer mehr Münzen will, geht in die Zusatzübungen. Das zahlt heute
nichts: seit [B-538](B-538-bonus-muenzregeln-form-und-degression.md) bucht eine Position **ohne Pflicht**
keine Münzen (`PositionPracticeController`, `paysCoins = PositionProgressService.HasDutyOn(…)`), damit neben dem
gedeckelten Bonus-Training kein zweiter, ungedeckelter Münzweg vor der Pflicht offensteht. Am Knoten steht
darum „frei · ohne Münzen“.

Gemessen am Ledger von Child 4 (2026-11-16…18): Ziel-Münzen (`Goal`, 15–20 je Ziel) kommen an; Antwort-Münzen
(`Base`/`Combo`) gab es zuletzt am 2026-11-16 – seither keine, auch nicht auf Pflicht-Positionen. Das ist der
zweite Teil des Eindrucks „Tagesmissionen geben keine Münzen“ und vor dem Ausformulieren am Code zu klären
(`scored` hat viele Ausschlüsse: Lernkarten-Stufen, Selbsteinschätzung, `eased`, schon heute gewertet).

Zu entscheiden: Zahlen Kür-Positionen Münzen (gedeckelt? erst nach der Pflicht? degressiv wie das
Bonus-Training?), und soll das Kind die Antwort-Münzen beim Üben sehen? Berührt den Anti-Cheat-Grund von B-538.

## Ursache: warum „nichts Münzen gab“ (gemessen an einer Kopie der echten DB, 2026-11-18)

Nicht nur die Zusatzübungen: **alle drei Kinder** bekamen seit dem 14.–16.11. keine einzige Antwort-Münze (`Base`),
nur Ziel-Münzen. Die erste Woche eines Worts besteht fast nur aus Stufen, die laut Regel nichts zahlten –
Lernkarte (B-598/B-643), Selbsteinschätzung auf der Leiter (B-476), Kennenlernen (B-96). Testjunge am 18.11. auf
Pflicht-Positionen richtig: 9× Selbsteinschätzung, 6× Lernkarte aus dem Gedächtnis, 1× Buchstabenkästchen.

## Entscheidungen (Vater, 2026-11-18)

1. **Alles zahlt.** Positionen ohne Pflicht zahlen wie Pflicht-Positionen (nimmt B-538 Entscheidung 4 zurück).
2. **„Gewusst“ in der Selbsteinschätzung bringt 1 Münze** – geprüft wird dort nichts; eine geprüfte Antwort bringt
   mehr. Gilt auf der Leiter und im Bonus-Training (`CoinsPerSelfAssessHit`).
3. **Zusatzübungen zahlen immer**, nicht erst nach den Tageszielen. „Pflicht zuerst“ hält weiter über Truhe,
   Bonus-Training-Sperre und Malus.

Beim Bau abgeleitet (nicht eigens gefragt): das **erste freie Erinnern auf der Lernkarte** zahlt die Neu-Punkte der
Position (Standard 10), einmal je Wort – dort wird das Wort gelernt; der spätere erste Leiter-Treffer zahlt dann nur
Wiederholung. Positionen **ohne Karteikasten** zahlen ebenfalls, einmal je Wort und Tag (`LastReviewedAt` als
Tagesstempel). **Weiter ohne Münzen:** Kennenlernen (reine Anzeige), mit Hilfe erleichterte Antworten, die
Lernkarte im Vorschau-/Nachtrags-Spiel des Vaters.

## Bau (`8165c1dd`, 2026-11-18)

- `PositionPracticeController.Review`: die Pflicht-Sperre entfällt; ohne Kasten zahlt ein Treffer und stempelt den Tag;
  der Claim-Zweig bucht `ScoringService.ClaimCoins` (1); `LearnCardReviewAsync` bucht `ScoreNewWordRecall`.
- `ExtraTrainingOptions.CoinsPerSelfAssessHit = 1` (+ `appsettings.json`), unter Deckel und Halbierung.
- Frontend: Knoten heißt „Zusatzübung“ statt „frei · ohne Münzen“; Bonus-Karteikarte „1 🪙 je gewusstem Wort“;
  Vater-Positionsformular zeigt bei einer freien Position die Übungs-Punkte (nur Ziel-Punkte und Malus nicht).
- Nebenfund: `ErstbegegnungTests.PointsOf` verglich mit einem Präfix ohne `[` und lieferte immer 0 – jede
  „0 Punkte“-Zusicherung dort war leer.

## Nachweis

Acht Tests hielten die alten Null-Regeln fest und wurden auf die neue Regel umgeschrieben, dazu zwei neue
(`KuerMitMuenzenTests.Position_Ohne_Karteikasten_Zahlt_Einmal_Am_Tag`, Vater-Lernkarte zahlt nichts). Rote Proben je
Regel (Claim zahlt nicht, Lernkarte zahlt nicht, Neu-Bonus doppelt, Pflicht-Sperre zurück, ohne Kasten zahlt nichts,
ohne Tagesstempel, Vater-Lernkarte zahlt, Bonus-Karteikarte 0) fallen je am zugehörigen Test. 1846 Backend- und
1216 Vitest-Fälle grün; E2E `bonus`, `full-flow`, `pfad`, `zwei-faecher`, `lernkarte-leichter`, `ziel-vier-wochen`
grün. Review `pugling-reviewer`: nichts blockierend; eingearbeitet Lernkarte/Kasten-Halbzustand, veraltete Doku,
Vater-Test, Nullbuchung.

**Bewusst offen:** zwei gleichzeitige Anfragen (Doppeltipp, zweiter Tab) können beide zahlen – die Lücke bestand
im geprüften Pfad schon immer (keine Concurrency-Marke an `PositionItemProgress`), jetzt mit mehr Einsatz. Eine
ganze Tagesstufe „Selbsteinschätzung“ ohne `RequireTypedTest` zahlt weiter den vollen Satz je „gewusst“ – Altbestand,
jetzt auch auf Zusatzübungen.
