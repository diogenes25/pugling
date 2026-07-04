# 05 · Punkte & Bonus-System

← [Zurück zum Wiki-Index](../README.md)

Punkte sind die Währung der Motivation. Diese Seite erklärt **exakt**, wie sie entstehen, welche
Bonus-Quellen es gibt (mit Formeln) und wie der Vater sie je Kind hochdreht.

---

## 1. Das Ledger & die Punkt-Kategorien

Jede Buchung ist eine **`ChildPointsEntry`** (positiv = gutgeschrieben, negativ = eingelöst) mit einer
Kategorie **`PointKind`** — dadurch ist auswertbar, *woher* Punkte kamen.

| `PointKind` | Quelle |
| --- | --- |
| `Base` | Basispunkte einer richtigen Leitner-Wiederholung (inkl. Zeitfenster-Faktor) |
| `Combo` | Combo-Bonus (Treffer in Folge) |
| `Speed` | Bonus für schnelle Antwort |
| `Duration` | Bonus für durchgehende Lernzeit *(reserviert, aktuell über Missionen abgebildet)* |
| `Minutes` | Tagesziel Übungszeit erreicht |
| `Test` | Abschlusstest bestanden |
| `DayComplete` | Tag vollständig (Zeit **und** Test) |
| `Mission` | erfüllte Mission |
| `Achievement` | erreichte Auszeichnung |
| `Manual` | manuelle Vater-Buchung |

Lesen: `GET /api/v1/children/{childId}/points` (Vater) bzw. `GET /api/v1/me/points` (Sohn).

---

## 2. Zwei Punkte-„Bahnen"

Punkte entstehen an genau zwei Stellen:

### Bahn A — Tages-Belohnungen (`StudyProgressService`)

Nach jeder Aktivität (Heartbeat, Test-Submit, Session-Ende) wird der Tag ausgewertet und fällige
Punkte werden **idempotent** gebucht (Unique-Index `StudyDayReward` je Plan/Tag/Art → nie doppelt):

| Ereignis | `PointKind` | Punkte |
| --- | --- | --- |
| Tages-Übungszeit erreicht (`dailyMinutesRequired`) | `Minutes` | `pointsMinutesMet` (Default 10) |
| Abschlusstest bestanden (`≥ dailyTestPassPercent`) | `Test` | `pointsTestPassed` (Default 20) |
| Tag vollständig (Zeit **und** Test) | `DayComplete` | `pointsDayCompleteBonus` (Default 10) |

Diese Bahn gilt für **jeden** Plan (auch ohne Leitner). Die beiden Tagespflichten (Zeit, Test) zählen
**unabhängig** — ein bestandener Test ohne genug Minuten gibt Testpunkte, aber keinen Tages-Bonus.

### Bahn B — Review-Punkte (`ScoringService`, nur Leitner-Pläne)

Beim server-autoritativen `POST …/practice-sessions/{sid}/review` einer **richtigen** Antwort auf einem
`useLeitner`-Plan bucht der `ScoringService` mehrere Beiträge auf einmal:

```text
Review-Punkte = Base (Pflicht)  + Combo (falls Meilenstein)  + Speed (falls schnell)
```

Falsche Antwort → **0 Punkte** (und Karte fällt in Box 1).

---

## 3. Die Basispunkte-Formel

```text
basis = (reviewCount == 0)  ?  newContentPoints            // erstmals geübt → „neuer Stoff zählt am meisten"
                            :  max(2, 8 − box)             // Wiederholung: je höher die Box, desto weniger

Base  = round(basis × Zeitfenster-Multiplikator)
```

- `reviewCount`/`box` sind der Zustand **vor** dem Aufstieg (neuer Inhalt zählt voll).
- Wiederholungs-Staffel `max(2, 8−box)`: Box 1→7, Box 2→6, Box 3→5, Box 4→4, Box 5→3, ab Box 6→2.

### Zeitfenster-Multiplikator (`TimeSlotRule`)

Der Multiplikator hängt an der **Server-Uhrzeit** der Antwort. Geseedete Fenster (vom Vater änderbar):

| Fenster | Zeit | Multiplikator |
| --- | --- | --- |
| Vormittag | 08:00–12:00 | ×1.5 |
| Nachmittag | 12:00–18:00 | ×1.0 |
| Abend | 18:00–21:00 | ×0.8 |

Außerhalb aller Fenster: ×1.0. **Hinweis:** Zeitfenster sind **global**, nicht pro Kind (bewusster
offener Punkt — ein „13–15-Uhr-Faktor pro Kind" wäre eine Schema-Änderung).

---

## 4. Ereignis-Boni

### Combo (Treffer in Folge)

Serverseitig gezählte richtige Antworten in Folge derselben Sitzung. Alle `comboThreshold` Treffer
gibt es einen **eskalierenden** Bonus:

```text
comboBonus = (combo > 0 && combo % comboThreshold == 0)
             ?  comboBonusPoints × (combo / comboThreshold)    // 1., 2., 3. Meilenstein → ×1, ×2, ×3
             :  0
```

Beispiel bei `comboThreshold=5, comboBonusPoints=5`: beim 5. Treffer +5, beim 10. +10, beim 15. +15 …
`comboThreshold=0` oder `comboBonusPoints=0` → Combo aus.

### Schnelle Antwort (Speed)

```text
speedBonus = (speedThresholdSeconds > 0 && speedBonusPoints > 0
              && 1.0s ≤ gemessene Zeit ≤ speedThresholdSeconds)
             ?  speedBonusPoints : 0
```

Die Zeit misst der Server als Abstand zur letzten Antwort derselben Sitzung. Die **Untergrenze von 1 s**
verhindert Punkte-Farming durch Doppel-Klicks. Bei der ersten Karte einer Sitzung gibt es keinen Speed-Bonus.

### Rechenbeispiel

Plan: `newContentPoints=12, comboThreshold=5, comboBonusPoints=5, speedThresholdSeconds=8, speedBonusPoints=3`.
Kind beantwortet **die 5. Karte in Folge richtig**, es ist ein **neuer Inhalt**, um **10:00 Uhr**
(Vormittag ×1.5), **4 s** nach der letzten Antwort:

```text
Base  = round(12 × 1.5) = 18   (PointKind.Base)
Combo = 5 × (5/5) = 5          (PointKind.Combo, 5. Meilenstein)
Speed = 3                      (PointKind.Speed, 4s liegt in [1,8])
──────────────────────────────
Summe = 26 Punkte in 3 Buchungen
```

Die `ReviewOutcome`-Antwort meldet `{ wasCorrect, expected, awarded (=Base), box, dueOn, combo, comboBonus, speedBonus }`.

---

## 5. Missionen & Auszeichnungen

Eine Motivations-Ebene **über** den Einzel-Boni. Beide sind **pro Kind vom Vater konfigurierbar**,
messen dieselben serverseitigen Metriken und belohnen **idempotent** (kein Client-Vertrauen).

### Fortschritts-Metriken (`ProgressMetric`)

| Metrik | Zählt |
| --- | --- |
| `NewWords` | neu eingeführte Inhalte |
| `CorrectReviews` | richtige Leitner-Wiederholungen |
| `TestsPassed` | bestandene Abschlusstests |
| `MinutesPracticed` | geübte Minuten |
| `DaysComplete` | vollständig geschaffte Tage |
| `StreakDays` | aktuelle Serie vollständiger Tage in Folge |

### Missionen (zeitgebundene, wiederkehrende Ziele)

`period` ∈ `Daily | Weekly | OneOff`. Erreicht das Kind im Zeitraum die `target`-Marke der `metric`,
gibt es **einmal je Zeitraum** `rewardPoints`.

```http
POST   /api/v1/children/{childId}/missions
{ "title": "Wochenziel: 3 Tests bestehen", "metric": "TestsPassed", "target": 3, "period": "Weekly", "rewardPoints": 30 }

GET    /api/v1/children/{childId}/missions        // Definitionen (Vater)
PATCH  /api/v1/children/{childId}/missions/{id}    { "target": 4, "rewardPoints": 40, "active": true }
DELETE /api/v1/children/{childId}/missions/{id}
```

### Auszeichnungen (permanente Badges)

Ab `threshold` der `metric` (lebenslang bzw. aktuelle Serie) einmalig verliehen — mit Emoji-Icon und
optionaler Punkte-Belohnung.

```http
POST /api/v1/children/{childId}/achievements
{ "title": "Wortschatz-Sammler", "icon": "📚", "metric": "NewWords", "threshold": 100, "rewardPoints": 50 }
```

### Wann wird ausgewertet?

`GamificationService.EvaluateAndAwardAsync` läuft **nach jeder gewerteten Wiederholung** und beim
Abruf der Sohn-Sicht (`GET /api/v1/me/missions|achievements`) — Belohnungen fließen beim Spielen, nicht
erst beim Ansehen. Sinnvolle Vorlagen werden pro Kind geseedet (frei editier-/löschbar).

**Sohn-Sicht** (mit Fortschritt): `GET /api/v1/me/missions` → `{ title, metric, target, current,
completed, rewardPoints }`; `GET /api/v1/me/achievements` → `{ title, icon, threshold, current,
earned, earnedAt, rewardPoints }`.

---

## 6. Bonus je Kind/Übung steuern

Zwei Wege, die Motivation gezielt hochzudrehen (z. B. Grammatik-Bonus für ein lustloses Kind):

1. **Pro Plan** — die Bonus-Felder (`comboThreshold`, `comboBonusPoints`, `speedThresholdSeconds`,
   `speedBonusPoints`, `newContentPoints`) jederzeit per `PATCH /api/v1/study-plans/{id}` anpassen.
   Das Bonus-System ist damit **kind-individuell**.
2. **Bonus-Vorschlag an einer Katalog-Übung** (`SuggestedBonus`) — dient nur als Vorlage: beim
   `to-study-plan` werden die Werte **einmal** in den neuen Plan kopiert. Spätere Änderungen an der
   Übung wirken **nicht** rückwirkend auf bestehende Kind-Pläne.

```jsonc
// SuggestedBonus (an der Exercise, siehe 03 · Übungstypen)
"suggestedBonus": { "comboThreshold": 3, "comboBonusPoints": 5,
                    "speedThresholdSeconds": 8, "speedBonusPoints": 3, "newContentPoints": 12 }
```

---

## 7. Bewusst offen

- **Zeitfenster pro Kind** (der „Hausaufgaben-Faktor 13–15 Uhr") — aktuell global.
- **Dauer-Bonus** (`PointKind.Duration`) als eskalierender Sitzungs-Bonus — reserviert, heute über
  `MinutesPracticed`-Missionen abgebildet.

Codeanker: [`ScoringService`](../backend/Pugling.Api/Services/ScoringService.cs),
[`StudyProgressService`](../backend/Pugling.Api/Services/StudyProgressService.cs),
[`GamificationService`](../backend/Pugling.Api/Services/GamificationService.cs).
