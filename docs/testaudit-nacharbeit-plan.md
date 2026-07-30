---
tags: [typ/plan, bereich/qualitaet, bereich/tests]
aliases: [Testaudit-Nacharbeit, Restliste Defektinjektion]
---

# Nacharbeit zur Defektinjektion

Status: **abgeschlossen** (2026-07-30, angelegt auf `feafd7d`, ausgeführt im dritten Commit).
Ergebnis-Stand: **597/597 grün, 268/268 Actions, Build warnungsfrei, `dotnet format` sauber.**

Der Befund selbst steht in [testplan.md](testplan.md) – dort sind auch alle Ergebnisse dieser Nacharbeit
eingetragen (umgestufte Injektion, neue Tests je Rang, die Wächter-Entscheidung mit Zahlen, der dritte
Commit). **Diese Seite ist ab jetzt das Protokoll, nicht die Aufgabenliste.** Es bleibt nichts zu tun.

## Was herauskam – die vier Etappen

| Etappe | Ergebnis |
|---|---|
| 1 · Produktivcode-Fund (Reward-Pfad) | **behoben.** `catch (DbUpdateException)` + `ChangeTracker.Clear()` in `EvaluateAndAwardAsync`, dazu ein **deterministischer** Nebenläufigkeits-Test. Die offene Frage („je real aufgetreten?") ist mit Nein beantwortet. |
| 2 · Restliste, sechs unbewachte Regeln | **fünf gepinnt, eine umgestuft.** D07, D15, B12, B08, B06 haben je einen Test mit Gegenprobe. B02 ist **keine Lücke** – siehe unten. |
| 3 · Der wanduhr-abhängige Test | **behoben** über eine `TimeProvider`-Naht. Die Untergrenze ist jetzt beidseitig gepinnt statt nur einseitig, und die `Task.Delay` sind weg. |
| 4 · Der vorgeschlagene reflexive Wächter | **nicht gebaut, begründet verworfen.** Vorher gemessen; die sieben Altlasten sind einzeln repariert. |

## Die drei Entscheidungen, die zählen

### B02 war falsch eingeordnet – der Guard ist nicht testbar

Rang 1 der Restliste („Anderes Bild verbrennt den einzigen Kandidaten") beschrieb einen Schaden, der **nicht
eintreten kann**. Fällt der Guard weg, wird die aktuelle Wahl auf `Rejected` gesetzt, die Neuwahl findet
nichts mehr, und `ReshuffleAsync` steigt bei `chosen is null` **vor** `SaveFreezeAsync` aus – die Ablehnung
wird nie geschrieben. Beide aufrufenden Endpunkte antworten danach `409` ohne eigenes `SaveChanges`.

Der vorgeschlagene Test existiert überdies längst
(`MediaSelectionTests.OhneAlternative_BleibtDasBildStehen_StattZuVerschwinden`) und deckt beide Teile ab,
auch den „eigentlichen". Er war unter der Injektion grün, weil es **nichts zu sehen gab** – nicht weil er
wegsah. Nachgemessen statt gefolgert: Injektion erneut gesetzt, alle 48 Medien-Tests grün.

Konsequenz: kein neuer Test (einer, der nicht fallen *kann*, behauptet eine Absicherung, die es nicht gibt),
Guard bleibt stehen, und der Grund steht jetzt als Kommentar an der Stelle – damit niemand denselben Weg noch
einmal geht. In [testplan.md](testplan.md) ist B02 von (c) nach **(b) Tiefenverteidigung** umgestuft.

### Der Nebenläufigkeits-Test stellt das Rennen, statt darauf zu hoffen

Die Vorlage wollte „zwei Submits nebenläufig abschicken". Das wäre ein Test, der fast nie prüft, was er
prüfen soll: das Fenster zwischen Existenzprüfung und `SaveChanges` ist Bruchteile einer Millisekunde breit.
Zwei parallele HTTP-Requests treffen es kaum, der Test bliebe grün **ohne den Pfad je zu betreten** –
dieselbe Sorte Scheinsicherheit wie die flachen Zusicherungen aus Etappe 1a.

Stattdessen wird der Zustand des Verlierers hergestellt: die Belohnung ist vom echten Gewinner (einem
bestandenen Test über HTTP) festgeschrieben, ein zweiter Kontext hält seine Buchung noch ungespeichert vor –
genau die Lage, in der seine Prüfung vor dem Commit des Gewinners lief. Der Konflikt tritt damit **immer**
ein. Gegenprobe gefahren: ohne den `catch` fällt der Test mit `UNIQUE constraint failed`.

### Kein Wächter – die Kennlinie trägt kein Tor

Gemessen (Skript auf dem Stand vor der Reparatur), nicht geschätzt:

| Verengung | Treffer | davon echt | echte verloren |
|---|---|---|---|
| Roh | 24 | 7 | – |
| + Folgestatus als Beleg ausgenommen | **8** | 6 | 1 |
| + Id-/`GetFromJsonAsync`-Vergleich ausgenommen | 5 | 3 | 4 |
| + Fehlercode-/Header-/Textprüfung ausgenommen | 3 | 3 | 4 |

Keine Stufe ist gleichzeitig genau und vollständig. Der härtere Grund kam erst nach der Reparatur: zwei der
reparierten Tests **bleiben** Treffer, weil ihr Nachlesen in einen gemeinsamen Helfer gewandert ist. Die
Heuristik sieht nur den Methodenrumpf – dieselbe Blindheit wie CA2016 bei tokenlosen Helfern. Ein Tor, das die
saubere Refaktorierung bestraft und die Lücke durchlässt, ist das falsche Werkzeug. Die sieben Altlasten sind
darum einzeln repariert; die Fehlerklasse bleibt Prüffrage im Review, nicht Tor.

## Was dabei sonst noch geprüft wurde

- **Die Logs zur offenen Frage aus Etappe 1.** Kein 500 auf einem Test-/Overview-Pfad in 14 Logtagen. Die 108
  gefundenen 500 liegen alle auf `supervisor/children/daily-overview` und sind `TaskCanceledException` durch
  Client-Abbruch – sämtlich **vor** `179cc06` („Client-Abbruch ist kein Serverfehler"), heute also 499 ohne
  Fehler-Log. Kein neuer Befund, sondern die Bestätigung, dass jener Commit gewirkt hat.
- **Das Azure-Deploy** blieb unangetastet, wie die Vorlage verlangt. Es liegt beim Eigentümer.

## Gegenproben (alle gefahren, keine angenommen)

| Injektion | erwartet rot | Ergebnis |
|---|---|---|
| Reward-`catch` entfernt | der neue Nebenläufigkeits-Test | rot (`UNIQUE constraint failed`) |
| Tiebreak → `Random.Shared` | beide neuen `MediaSelector`-Tests | **beide** rot |
| `WeightSeries` 8 → 4 | Rangfolge-Test | rot |
| Kauf-Titel prüft falsche Variable | Kauf-Beleg-Test | rot |
| Malus-Text Tag/Woche vertauscht | Buchungstext-Test | rot |
| `&& p.PointsGoalMet > 0` entfernt | Null-Punkte-Test | rot |
| `MinSpeedSeconds` 1,0 → 0,5 | 900-ms-Fall der neuen `Theory` | rot |
| Alternativen-Guard entfernt (B02) | *nichts* – Beleg für die Umstufung | grün, 48/48 |

## Verwandt

- [testplan.md](testplan.md) – der Befund und die eingetragenen Ergebnisse dieser Nacharbeit
- [codequalitaet-gates-plan.md](codequalitaet-gates-plan.md) – die Tore und die „erst messen"-Haltung, an der
  die Wächter-Entscheidung hier hängt
- [backlog-vokabellernen.md](backlog-vokabellernen.md) – eigene, weiter **offene** Spur; enthält den
  P1-Defekt „Test friert unsichtbare Bildwahlen ein", der dieselbe `MediaSelector`-Ecke berührt
