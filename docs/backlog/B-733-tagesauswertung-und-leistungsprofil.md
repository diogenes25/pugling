---
tags: [typ/story, status/in-arbeit, bereich/auswertung, bereich/training, rolle/supervisor]
aliases: [Tagesauswertung, Leistungsprofil, Problemwörter, Verlauf, Etappe E1b]
status: in-arbeit
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
---

# B-733 · Tagesauswertung je Kind, Tag und Fach, Leistungsprofil und die Vater-Ansicht (B-644, Etappe E1b)

Vierte Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Pugling schreibt je Kind, Tag und
Fach mit, was gelernt wurde und wo es hakte, und zeigt dem Vater den Verlauf und die Problemwörter. Aus den
gespeicherten Tagen entsteht das Leistungsprofil, das der Planer ([B-734](B-734-planer-kern-und-schattenmodus.md)) als
Eingang braucht. Diese Etappe wirkt auf die Kinder **nicht**, sie liest nur.

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 geerbt. Entscheidung 13 stammt vom Vater, 21 ist stellvertretend und
unbestätigt.

## User Story

Als *Vater* möchte ich je Fach und Tag nachlesen, was mein Kind gelernt hat und wo es Probleme gab, mit dem Warum
(Fehlerart), damit ich nicht nur eine Wortliste sehe. Als *Planer* brauche ich das Profil (Beherrschung, tragfähiges
Tempo, typische Fehlerarten, Belastbarkeit) als gespeicherte, nachrechenbare Größe.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/Pugling.Api/`.

- **Rohdaten sind vollständig:** `ItemReviewEvent` mit Antwort, richtig/falsch, Zeit (`Models/ItemProgressEntities.cs:61`),
  `TestItemResult` und `TestAttempt` (`Models/StudyPlanEntities.cs:202`, `:163`), `PracticeSession.ActiveSeconds`
  (`:112`), `ItemProgress` mit Beherrschung in Prozent und Einführungstag (`ItemProgressEntities.cs:18`, `:41`, `:49`).
- **Fehlerart und Fach vor der Antwort fehlen heute**; sie kommen mit [B-732](B-732-fehlerart-wird-beim-antworten-gespeichert.md).
- **Eine gespeicherte Tagesauswertung und ein Profil gibt es nicht** (keine Entität, kein Dienst).
- **Lesepfade für die Vater-Seite:** `ChildrenDashboardService` (`Services/Supervisor/ChildrenDashboardService.cs:68`) rechnet
  Tageswerte pro Aufruf; die schwachen Wörter liefert `ChildVocabularyProgressController` (`WeakBelowPercent = 50`).
  Die Vater-Zeitachse liegt in `frontend/src/vater/ChildrenTimeline.tsx`.
- **SQLite-Falle:** Die Hierarchie-Sicht (`ChildLearnProgressTests`) musste Aggregationen auf der Client-Seite rechnen; das
  wiederholt sich hier (Summen und Gruppen über `DateOnly`).
- Navigation und Perspektiven: `frontend/src/vater/navigation.ts:57`, `:72`.

## Entscheidungen (geerbt)

1. **Die Auswertung speichert Mengen und Fehlerarten je Kind, Tag und Fach** (B-644 Entscheidung 13, Vater). Vater-Web:
   Verlauf über die Wochen (gekonnte Wörter, neue Wörter je Tag, erfüllte und verpasste Pflicht), Problemwörter mit
   Fehlerart, Tagesplan mit Begründung. Die Sohn-App sieht nur die eigene Kurve (in [B-738](B-738-eingriffe-bonus-und-sohn-begruendung.md)).
2. **Das Profil ist je Kind und Fach gleitend über die letzten 7 und 28 Tage** (B-644 Entwurf, Baustein 2).
3. **Der Planer ist regelbasiert, ein Sprachmodell nimmt nicht an der Auswertung teil** (Entscheidungen 10 und 21).

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **Das Profil wird berechnet, nicht gespeichert:** ein Dienst rechnet es bei Bedarf aus den gespeicherten Tagen der
   letzten 28 Tage. Begründung: Die gespeicherte Auswertung ist die stabile Basis (Entscheidung 13); eine zweite Tabelle
   müsste bei jeder Regeländerung neu befüllt werden. Kosten: Rechenzeit je Planerlauf (28 Zeilen je Fach, gering).
   Verworfen: Tabelle `PerformanceProfile`.
2. **Nur abgeschlossene Tage werden gespeichert;** der laufende Tag wird beim Lesen live gerechnet und nicht abgelegt.
   Ein Tag gilt als abgeschlossen, sobald der Ortstag ([B-730](B-730-tagesbegriff-in-deutscher-zeit.md)) vorbei ist; die
   Berechnung läuft träge beim ersten Zugriff und in der Planer-Zeitschaltung ([B-734](B-734-planer-kern-und-schattenmodus.md))
   idempotent über alle noch fehlenden Tage der letzten 28.
3. **Zählregeln je Tag und Fach:** *neu* = Einführungstag des `ItemProgress` ist der Tag; *richtig*/*falsch* =
   gezählte Antworten; *gefestigt* = richtig und `BoxBefore + 1` erreicht das oberste Fach (`ItemProgress.MaxBox`);
   *zurückgefallen* = falsch bei `BoxBefore` über 1; *Fehlerarten* = Zähler je Art (JSON-Spalte); geübte Zeit = Summe
   `ActiveSeconds`; Pflicht erfüllt/verpasst = Ergebnis der Tageszustände des Dienstes `PositionProgressService`. Kosten:
   eine Näherung, bis der Planer eigene Tagesaufgaben kennt ([B-736](B-736-pflicht-aus-der-tagesaufgabe.md)).
4. **Pause- und Sonntagstage tragen keine Zeile** (Lerntag aus [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md));
   die Kurve zeigt eine Lücke, keinen Nullwert. Wird an einem freien Tag trotzdem geübt, entsteht eine Zeile.
5. **Der Reset ([B-737](B-737-reset-zum-stichtag.md)) löscht die Auswertungen mit,** weil sie aus gelöschten Antworten
   entstanden sind und das Profil ohne Vorgeschichte starten soll (Entscheidung 12).

## Akzeptanzkriterien

1. Für jeden abgeschlossenen Lerntag, an dem das Kind geübt hat, entsteht je Fach genau eine Zeile (eindeutiger Index
   auf Kind, Tag, Fach); zweimaliges Berechnen legt keine zweite an und ändert die erste nicht. *Rot, wenn* der Index
   fehlt oder das Berechnen nicht idempotent ist.
2. Die Zeile trägt neue, gefestigte und zurückgefallene Wörter, richtige und falsche Antworten, geübte Sekunden, die Zähler je
   Fehlerart und erfüllte und verpasste Pflicht. *Rot, wenn* eine Spalte aus dem Verlauf eines Tages anders hervorgeht als
   die Zählregel (Test mit bekanntem Antwortverlauf).
3. Die gespeicherten Tage bleiben stabil, wenn sich die Antworten später ändern oder die Regeln wechseln. *Rot, wenn* ein
   Lesepfad einen abgeschlossenen Tag neu aus den Ereignissen rechnet.
4. Das Profil je Fach liefert Beherrschung, tragfähiges Tempo (im Mittel gefestigte neue Wörter je Lerntag), typische
   Fehlerarten und Belastbarkeit (Anteil erfüllter Pflichttage) über 7 und 28 Tage, gerechnet aus den gespeicherten Tagen.
   *Rot, wenn* es aus den Rohereignissen statt aus den Tageszeilen gerechnet wird oder ein Fenster fehlt.
5. `GET supervisor/children/{childId}/evaluations` (mit Zeitraum, seitenweise) und `…/profile` liefern Verlauf und Profil;
   ein Konto nur mit Creator-Profil bekommt `403`, ein fremder Vater `404`. *Rot, wenn* ein Gate fehlt.
6. Kein Student-Endpunkt gibt einen Auswertungs- oder Profil-Typ zurück; ein Student-Token bekommt auf den Pfaden `403`.
   *Rot, wenn* ein Student-Endpunkt Fehlerarten oder Zahlen des Bruders liefert (Reflexionsprüfung nach dem Muster
   `Actions_Geben_Nur_Vertragstypen_Zurueck`).
7. Das Vater-Web zeigt Verlauf (neue und gefestigte Wörter, erfüllte und verpasste Pflicht je Tag und Fach) und die
   Problemwörter mit Fehlerart; ein Tag ohne Zeile ist eine Lücke. *Rot, wenn* ein Pausentag als Null gezeichnet wird oder
   ein Problemwort ohne seine Art erscheint, obwohl sie gespeichert ist.
8. Die Migration legt die Tabelle leer an; bestehende Daten bleiben. *Rot, wenn* die Migration fehlt oder die
   JSON-Spalte keinen `ValueComparer` trägt (Tor G7 in `SchemaGuardTests`).

## Schätzung

**Größe: L** — Anker: eine DB-Umbau-Etappe wie E6: neue Tabelle mit Migration, ein Rechendienst über vier Quellen,
vier Lese-Endpunkte, eine neue Vater-Ansicht. Am oberen Rand; sprengt sie beim Bau, ist das Frontend als eigene Story
(neue Id, `quelle: B-733`) abzuspalten.

- **`migration: ja`** — Tabelle `DailyEvaluation` (Kind, Tag, Fach, Zähler, Fehlerarten als JSON, Anlagezeit), eindeutiger Index
  (Kind, Tag, Fach). An die Kette hängen.
- **`vertragsbruch: nein`** — nur neue DTOs und Endpunkte; kein bestehender Vertragstyp ändert sich.
- **`wo: beides`** — Backend zuerst, dann Vater-Web (`ChildrenTimeline.tsx`-Nachbarschaft, neue Maske, Eintrag in
  `navigation.ts`).

### Risiken

- **„Gefestigt" und „zurückgefallen" hängen an `BoxBefore`** aus B-732; ohne die Spalte ist die Zählung nicht zu bauen.
- **Fehlende Tage nach Serverausfall:** Die träge Nachberechnung muss die letzten 28 Tage idempotent füllen können;
  ein Ausfall von mehr als 28 Tagen bleibt eine Lücke.
- **SQLite-Aggregation:** Gruppieren über `DateOnly` im Speicher rechnen, nicht in der Abfrage (Erfahrung aus der
  Lernstand-Hierarchie).
- **Endpunkt-Abdeckung und Paging:** `EndpointCoverageGuard` und `Unpaginierte_Array_GETs_Sind_Gepinnt` verlangen Aufruf und
  Seitenbildung je neuem Endpunkt.
- **Fremde Sohn-Dateien:** Der Arbeitsbaum trägt ungecommittete Frontend-Arbeit (LearningDad, `frontend/src/sohn/*`);
  diese Story fasst `sohn/` nicht an.

### Angriffsplan

1. Migration und Modell `DailyEvaluation`, Migrationstest.
2. Rechendienst (eine Methode für einen Tag und ein Kind, idempotent), Zählregeln als eigene, getestete Funktionen.
3. Profil-Dienst über die gespeicherten Tage.
4. Endpunkte mit Gates, Paging und Reflexionsprüfung für Student.
5. Vater-Web: Verlauf und Problemwörter, Lesen vom Server.

### Testweg

- Neue Klasse `TagesauswertungTests` (Integration mit `TestClock`, Antwortverlauf über die Student-API, danach Direktlesen
  der Zeile): Zählregeln, Idempotenz, Stabilität nach Änderung der Antworten. Rote Probe je AK: Index entfernen lässt AK 1
  fallen; den Lesepfad auf Rohereignisse umstellen lässt AK 3 fallen.
- `ProfilTests` (Fenster 7 und 28, Pausen- und Sonntagslücken).
- `ConventionGuardTests`: Student-Aktionen geben keinen Auswertungs-Typ zurück (Rot-Probe: einen solchen Rückgabetyp an einen
  Student-Endpunkt hängen). `OwnershipMatrixTests` deckt die neuen Pfade über den Ownership-Filter.
- Migration: `TagesauswertungMigrationTests` nach `ActivatedAtMigrationTests`.
- Frontend: Vitest für die Verlaufsmaske; E2E neu `frontend/e2e/auswertung-verlauf.spec.ts` (Kind übt, nächster Tag, Vater
  sieht Verlauf und Problemwort mit Art), Regression `frontend/e2e/vater-zeitachse.spec.ts`.
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer`, Rollengang als Vater.

## Reihenfolge und Abhängigkeiten

Nach [B-730](B-730-tagesbegriff-in-deutscher-zeit.md), [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md) und
[B-732](B-732-fehlerart-wird-beim-antworten-gespeichert.md). Vor [B-734](B-734-planer-kern-und-schattenmodus.md).

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E1b) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet):
  Eintrittsbedingung `gegrillt` aus B-644 geerbt. Nachgesehen: Rohdaten vollständig, aber das Fach vor der Antwort fehlt (nach
  B-732 verlegt). Profil berechnet statt gespeichert (Schätz-Annahme). L, beides, Migration, kein Vertragsbruch.
- **2026-11-26** — `in-arbeit` im Sprint 193 (PM-Sitzung 2026-11-26, Runde 3: der Vater sieht „une côte“ mit 0 % und kein Warum).
  Gebaut: Tabelle `DailyEvaluations` plus Wasserstand `EvaluatedDays` (Migration `Tagesauswertung`, rein additiv), Dienst
  `DailyEvaluationService`, drei Endpunkte `supervisor/children/{id}/evaluations`, `…/profile`, `…/problem-words`, Client,
  Vater-Seite `/vater/kind/:id/auswertung` (Link „🔎 Auswertung“ im Kind-Hub). Abweichungen von den Schätz-Annahmen, bewusst:
  (a) eine Zeile entsteht auch an einem Pflichttag **ohne** Übung (sonst wäre der ganz verpasste Tag unsichtbar – AK 1 sagt
  „an dem das Kind geübt hat“, Entscheidung 1 verlangt aber die verpasste Pflicht im Verlauf); (b) gezählt wird nur die
  **Tagespflicht** – eine Wochenpflicht ist an einem Wochentag noch nicht entschieden; (c) Fach einer Antwort = Fach der Reihe
  ihrer Übung, sonst des Plans; (d) Problemwörter werden live aus den Antworten gelesen (die Tageszeile trägt Zahlen, keine
  Wörter); (e) ein abgeschlossener Tag wird über einen eigenen Wasserstand je Kind und Tag genau einmal gerechnet, auch wenn er
  keine Zeile ergibt.
- **2026-11-26** — Review und Nachbau. `pugling-reviewer`: 🔴 angezeigte Kennenlern-Karten, Lernkarten und erleichterte Abrufe
  zählten als falsch und als Problemwort → eine Regel `Counts` an beiden Lesern (Urteil = `BoxBefore`; Zeilen von vor B-732: getippt,
  keine Lernkarte), Test über die echte Sohn-API; 🟡 „neu“ jetzt aus `PositionItemProgress.IntroducedAt`; 🟡 abgeschaltete Pläne
  beim ersten Nachrechnen → als Grenze dokumentiert; 🟡 Antworten des Vaters → B-1028 (Schreibseite, älter). `frontend-reviewer`:
  🔴 Fenster in UTC statt Lerntag → `verlaufFenster` mit `heuteImLerntag()`, Fenster an den Server, keine Zeile geht verloren.
  Beim Bau der E2E gefunden: auf dem echten Testweg speichert der Server keine Fehlerart → B-1030 (`entgangen_bei: B-732`).
  - AK 1, eine Zeile je Tag und Fach → `TagesauswertungTests.Eindeutiger_Index_je_Kind_Tag_und_Fach_auch_ohne_Fach`; zweites
    Rechnen → `Abgeschlossene_Tage_werden_einmal_gespeichert_und_nie_neu_gerechnet`. Index-Mutante nicht gemessen (Migration);
    C19 (Wasserstand übersprungen) bleibt grün, er spart nur Arbeit – der Index trägt die Einmaligkeit.
  - AK 2, Zählregeln → `Tageszeilen_folgen_den_Zaehlregeln_und_freie_Tage_ohne_Uebung_sind_Luecken`: gefestigt C1, zurückgefallen C2,
    Fehlerart C3, neu C4, Sekunden C5, nur Tagespflicht C6, nur geschuldete Pflicht C7 (`Nicht_geschuldete_Tagespflicht_…`), heute
    nicht gespeichert C10, Zählregel R1/R3/R4/R5 → je rot.
  - AK 3, Lesepfad rechnet nie neu → C11 rot.
  - AK 4, Profil aus den Tageszeilen, Fenster 7/28 → `Profil_rechnet_aus_den_gespeicherten_Tagen_ueber_7_und_28_Tage`, C12 rot.
  - AK 5, Gates und Zeitraum → `Pfade_pruefen_Rolle_Eigentum_und_Zeitraum`, C16/C17/C18 rot.
  - AK 6, kein Student-Pfad → Kind-Token 403 im selben Test; `ConventionGuardTests.Auswertungstypen_Erreichen_Keinen_Studenten`,
    C17 rot.
  - AK 7, Lücke statt Null, Problemwort mit Art → Vitest `verlaufTage`, `verlaufFenster`, `fehlerartText`; Problemwörter
    `Problemwoerter_tragen_Fehlerart_letzte_Antwort_und_Fach` (C14/C15/R2 rot), `Kennenlernkarte_ueber_die_Sohn_API_…` (R1/R2 rot);
    Fach-Regel `Antwort_zaehlt_fuers_Fach_der_Reihe_Pflicht_fuers_Fach_des_Plans` (C8/C9 rot). E2E `auswertung-verlauf` (1 passed):
    heute live mit offener Pflicht, gestern Lücke, Problemwort mit getippter Antwort – die Art kann dieser Weg wegen B-1030 nicht zeigen.
  - AK 8, Migration leer und additiv → `SchemaGuardTests` (Pins, kein Drift, `ValueComparer`).

  Mutanten am Stand vor der letzten Änderung (danach nur: toter Filter `d.Day < today` gestrichen, Fall C7 ergänzt; C7 und C10 am
  Endstand nachgemessen):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | C1 gefestigt | `DailyEvaluationService.cs:216` | red | red |
  | C2 zurückgefallen | `:222` | red | red |
  | C3 Fehlerart | `:223` | red | red |
  | C4 neu aus der Position | `:192` | red | red |
  | C5 Sekunden | `:229` | red | red |
  | C6 nur Tagespflicht | `:251` | red | red |
  | C7 nur geschuldete Pflicht (Endstand) | `:252` | red | red |
  | C8 Fach der Reihe vor Plan | `:287` | red | red |
  | C9 Pflicht fürs Fach des Plans | `:253` | red | red |
  | C10 heute nicht gespeichert (Endstand, `Range`) | `:56` | red | red |
  | C11 Lesepfad rechnet neu | `:87` | red | red |
  | C12 Fenster 7 | `:116` | red | red |
  | C14 Problemwort-Reihenfolge | `:170` | red | red |
  | C15 letzte getippte Antwort | `:167` | red | red |
  | C16 Zeitraum | `ChildEvaluationsController.cs:71` | red | red |
  | C17 Supervisor-Gate | `ChildEvaluationsController.cs:18` | red | red |
  | C18 Eigentums-Filter | `ChildEvaluationsController.cs:19` | red | red |
  | R1 Zählregel (Tag) weg | `:187` | red | red |
  | R2 Zählregel (Problemwörter) weg | `:139` | red | red |
  | R3 Altzeilen-Zweig weg | `:267` | red | red |
  | R4 Altzeilen auch nach B-732 | `:267` | red | red |
  | R5 Lernkarte zählt | `:267` | red | red |
  | C19 Wasserstand (nur Arbeit) | `:55` | green | green |
  | C20 Gate freier Tag (nur Arbeit) | `:244` | green | green |
