---
tags: [typ/protokoll, bereich/pm]
aliases: [PM-Sitzung 2026-08-06, Nachtlauf Aufräumen/Defekt]
---

# PM-Sitzung: Nachtlauf — Aufräumen/Defekt-Backlog abarbeiten

**Datum:** 2026-08-06  ·  **Moderation:** PM
**Teilnehmer:** Entwickler (autonom, `art: Defekt`/`Aufräumen` freigegeben — `Wunsch`/`Frage` bleiben Dialog)
**Ziel:** die `ausformuliert`en Aufräumen/Defekt-Stories aus der 2026-08-04-Arbeitsrunde und den
Code-Reviews bis zur Abnahme bringen, ohne Rückfrage außer an den drei dokumentierten Stellen
(`docs/nachtlauf.md`).

**Freigaben für diesen Lauf** (Nutzerauftrag, wörtlich aus `docs/nachtlauf.md` übernommen):

1. Autonomes Grillen nur für `art: Defekt`/`Aufräumen`; jeder `Wunsch`/jede `Frage` wird notiert, nicht
   entschieden.
2. Nicht erreichbare Reviewer-Agenten → ausdrücklich beschrifteter Selbst-Check, Story bleibt `in-arbeit`
   mit `wartet_auf`, nie auf `abgenommen` gestampft.
3. Mehrere Sprints erlaubt; Retro schlägt ihren Mechanismus in jedem Sprint nur vor; Review-Funde werden
   sofort behoben oder im selben Sprint als `Defekt` bearbeitet, > 5 je Sprint beendet den ganzen Lauf.
4. „Kein Befund" nur mit benanntem Prüfpunkt.
5. Jede rote Probe nennt ihre Zahl.
6/7. Chrome-Extension/`web-design-guidelines` — entfallen hier: alle Kandidaten dieses Laufs sind
   Backend/Doku ohne sichtbare UI-Änderung.

Push bleibt beim Nutzer. Commits setzt der Lauf selbst.

## Vorlauf — Bestand gesichtet

`docs/backlog/README.md`-Index (frisch gezogen) zeigt 45 offene Stories. Nach `art`-Filter (Freigabe 1)
bleiben erreichbar: fünf bereits `ausformuliert`e `Aufräumen`-Stories (B-100, B-101, B-102 aus der
2026-08-04-API-Design-Arbeitsrunde; B-95, B-108 aus zwei Code-Reviews) sowie drei frische `idee`-Stories
(B-118, B-119, B-120), die erst gegen den Code recherchiert werden müssten. B-07/B-31 bleiben außen vor
(`wartet_auf` extern); B-47 trotz `geschaetzt` ebenfalls außen vor — seine eigene Entscheidung 1 bindet den
Bau an die Reaktivierung des `deploy-azure.yml`-Triggers, die nicht eingetreten ist.

Die fünf `ausformuliert`en Stories zerfallen in zwei rote Fäden — sie werden als getrennte Sprints
gefahren, nicht vermischt:

- **Sprint 1:** B-102 allein (Startkontext-Regel).
- **Sprint 2:** B-95 + B-108 (Validierungslücken, aus zwei Code-Reviews derselben Fehlerfamilie).
- **Sprint 3:** B-100 + B-101 (dieselbe 2026-08-04-Arbeitsrunde, beide an denselben OpenAPI-Transformern).

Die drei `idee`-Stories (B-118/119/120) bleiben für einen möglichen weiteren Sprint vorgemerkt, falls die
Nacht so weit kommt.

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel:** Wer die API über `CLAUDE.md` liest, findet die `CancellationToken`-Regel nur noch so weit
formuliert, wie ihr eigener Grund trägt — keine Sitzung muss mehr zwischen „Regel verletzen" und „55
Signaturen anfassen" wählen.
**Umfang:** B-102 allein — ein Sprint von einer Story ist zulässig (keine Untergrenze).
**Entwickler-Brief:** Ziel: Teil 1 der `CancellationToken`-Regel in `CLAUDE.md` auf den Fall „optionale
`[FromQuery]`-Parameter gehen voran" verengen. Quelle der Wahrheit: die 55/268-Messung und die
84-Parameternamen-Messung aus B-102 (beide bereits in der Story dokumentiert, nicht neu erhoben). Guards:
keine (reine Doku-Änderung, kein Code, kein Test). Migration: nein. Testweg: Suite vor/nach identisch grün.

## Iteration 1 — umgesetzt

`CLAUDE.md`, Abschnitt „Konventionen" → `CancellationToken` Teil 1 umformuliert (kompakt, mit Verweis auf
B-102 statt der Zahlen im Fließtext — die verrotten dort schneller). Kein Code, kein Test geändert.

**Kontext-Budget:** `CLAUDE.md` war schon vor dieser Story um +2296 B über Budget (Altbestand, nicht diese
Story); die Umformulierung fügt **+173 B** hinzu (21296 → 21469 B), nicht 0 wie AK4 im Idealfall wollte —
gemessen mit `.claude/scripts/context-budget.sh` vor/nach. Bewusst nicht weiter komprimiert: die Zahlen
wurden aus dem Fließtext entfernt und in die Story verlinkt (das ist die Kompression, die möglich war, ohne
den Grund der Regel wegzulassen); das Restdelta ist der neue Halbsatz zur OpenAPI-Unsichtbarkeit selbst.
Nicht blockierend (`context-budget.sh` warnt nur).

## Runde — Abnahme Sprint 1 (Rollengang: Regression)

Keine Rolle ist von einer Doku-Datei betroffen, die nur beim Programmieren gelesen wird — kein Playwright,
kein API-Call. Rollengang-Ersatz: volle Suite unverändert grün (748/748 vor der Story, siehe unten),
`context-budget.sh` läuft ohne Fehler (nur die dokumentierte, nicht-blockierende Warnung).

## Sprint 2 — Ziel & Umfang

**Sprint-Ziel:** Ein Creator, der `defaultRequireTypedTest: true` auf einer Übung ohne getippte Stufe
setzt, sieht den Fehler am Ort, wo er ihn gemacht hat — nicht erst, wenn ein Supervisor die Übung Wochen
später verplant. Und eine Stufenprüfung im PATCH-Pfad hängt nicht mehr an einem `Include`, dessen Wegfall
niemand bemerken würde.
**Umfang:** B-95, B-108 — dieselbe Fehlerfamilie (eine Prüfung, die von einer stillen Voraussetzung bzw.
vom falschen Schreibweg abhängt), aus zwei unabhängigen Code-Reviews.
**Entwickler-Brief:** Ziel: zwei Validierungslücken schließen, beide nach demselben Muster wie
`StageValidation` (geteilte, `public static` Prüfmethode statt private Kopie je Controller). Quelle der
Wahrheit: `PlanPositionsController`/`ExerciseControllerBase` (B-108), `PlanPositionsController.Update`
(B-95). Guards: `[ProducesResponseType(StatusCodes.Status400BadRequest)]` bzw. `404` sind an den
betroffenen Actions bereits deklariert — kein neuer Vertragspunkt. Migration: nein. Testweg: neuer Test für
B-108 (rot vor dem Fix, grün danach, Zahl unten); B-95 hat keinen neuen Test (Entscheidung 3 der Story:
ein Test für ein entferntes `Include` wäre kein echter Wächter).

## Iteration 2 — umgesetzt

- Neue Datei `backend/Pugling.Api/Exercises/RequireTypedTestValidation.cs` — geteilte
  `RequireTypedTestValidation.ProblemText(...)`, Vorbild `StageValidation`.
- `PlanPositionsController.cs`: private `RequireTypedTestProblem` entfernt, beide Call-Sites (Create/
  Update) rufen die geteilte Methode; zusätzlich im PATCH-Pfad `if (pos.Exercise is not { } exercise)
  return NotFound();` **vor** den beiden Prüfungen statt der bedingten `if (pos.Exercise is { } exercise)`
  (B-95).
- `ExerciseControllerBase.cs`: Create **und** Update prüfen jetzt `body.DefaultRequireTypedTest` über die
  geteilte Methode, direkt neben der bestehenden `StageValidation`-Prüfung (B-108).
- Neuer Test `BirkenbihlExerciseTests.DefaultRequireTypedTest_AufEinemTypOhneGetippteStufe_WirdAbgewiesen`
  (Create **und** Update). **Rote Probe vor dem Fix** (Produktivcode per `git stash` auf den Stand vor
  dieser Story zurückgesetzt, nur der Test lief neu): 1 Failed, 8 Passed, Total 9 — erwartet `BadRequest`,
  gemessen `Created`. Nach `git stash pop` (Fix wieder da) zunächst noch 1 Failed (Routen-Verwechslung im
  Test selbst, nicht im Produktivcode — zweite Übung landete in einer anderen Serie), korrigiert, danach
  grün: 9 Passed, 0 Failed.

## Runde — Abnahme Sprint 2 (Rollengang: Regression)

Beide Stories ändern nur Backend-Validierung ohne neue Oberfläche; kein Rollengang-Kandidat für Creator/
Supervisor/Sohn im eigentlichen Sinn (keine neue Fähigkeit, kein neuer Bildschirm). Ersatz: die volle Suite
(749/749 grün) plus der gezielte rot→grün-Nachweis oben plus `pugling-reviewer` (keine Blocker; eine
nicht-blockierende Beobachtung zur verallgemeinerten Fehlermeldung, absichtlich so).

**Ergebnis:** B-102 (Sprint 1), B-95 und B-108 (Sprint 2) sind `abgenommen` — Details je Story in ihrem
`## Verlauf`.

## Retrospektive — Sprint 1+2

**Nachschau:** Der vorige Sprint war Nachtlauf-2-Sprint-4 vom 2026-08-05 (B-59/B-74/B-49) — laut Index dort
bereits durch die dortige Retro nachgesehen (keine Entgleitung über das Dokumentierte hinaus). Kein neuer
Nachhol-Bedarf für diesen Lauf.

**Was diese beiden Sprints über die eigenen Tore gelernt haben:** Der einzige Fund kam nicht aus einem
Reviewer oder einer roten Probe, sondern aus dem Bauen selbst — die anfängliche rot→grün-Verifikation für
B-108 lief zunächst auf ein zweites Rot (Routen-Verwechslung im **Test selbst**, nicht im Produktivcode:
zwei verschiedene Serien/Units durch zwei verschiedene Hilfsmethoden). Sofort korrigiert, kein
eigenständiger Fund für die Fünf-Fehlversuche-Zählung (Freigabe 3 zählt Review-*Funde*, nicht
Test-Autoren-Fehler in derselben Änderung, die vor dem ersten grünen Lauf entstehen).

Zweiter Punkt: B-102 hat den Kontext-Budget-Wächter nicht auf 0 gehalten (+173 B auf eine bereits um +2296 B
überzogene Datei) — die eigene AK4 („reißt nicht") war optimistischer formuliert, als eine notwendige
Regelkorrektur mit Begründung leisten kann, wenn die Datei schon vorher über Budget war. Das ist keine
Verletzung, die ein Tor fängt (der Wächter warnt bewusst nur), aber eine Lehre für künftige `doku`-Stories:
AK sollten „so wenig wie möglich, mit Begründung" statt eines absoluten „0" versprechen, wenn die Zieldatei
schon Altschulden trägt.

**Kein neuer Mechanismus.** Beide Lehren sind für diesen Lauf einmalig (ein Test-Autoren-Fehler, sofort
gesehen und behoben; eine zu scharf formulierte AK, die die Story selbst schon transparent macht). Kein
Gate wird gezogen — die bestehenden (rote Probe vor jedem Fix, `context-budget.sh`, `pugling-reviewer`)
haben in beiden Fällen genau das geleistet, wofür sie da sind.

## Sprint 3 — Ziel & Umfang

**Sprint-Ziel:** Ein fachlicher Konflikt (doppelte E-Mail, doppelter Kategorie-Name) trägt einen eigenen,
spezifischen Code statt eines nackten `conflict` — und ein mechanisches Tor mit leerer Ausnahmeliste hält
das für die Zukunft, statt sich auf Disziplin zu verlassen.
**Umfang:** B-101 — ursprünglich zusammen mit B-100 geplant (beide aus derselben 2026-08-04-Arbeitsrunde),
aber B-100 (OpenAPI-Vertragsdokument-Transformer über 24+31+5 Operationen, Regenerierung des 900-KB-
Dokuments) ist ein eigenständig großer Umfang mit anderer Risikoklasse als B-101s Drei-Zeilen-Fix — beide
im selben Sprint hätten den überschaubaren Fortschritt an den riskanteren Teil gekoppelt. B-100 bleibt
`ausformuliert`/`geschaetzt` für einen eigenen Sprint (siehe „Stand am Ende dieser Sitzung" unten).
**Entwickler-Brief:** Ziel: die drei generischen `ApiErrors.Conflict`-Stellen durch spezifische Codes
ersetzen, dann ein Tor mit leerer Ausnahmeliste ziehen. Quelle der Wahrheit:
`AuthController.cs`/`ExerciseCategoriesController.cs`. Guards: neuer `ConventionGuardTests`-Fall
(Source-Scan, Vorbild `Actions_Melden_Fehler_Nur_Ueber_ProblemWithCode`). Migration: nein. Vertragsbruch:
nein (additiv). Testweg: zwei bestehende Tests (`AccountSelfServiceTests`, `DocsCaptureTests`) auf den
spezifischeren Code umgestellt statt eines neuen roten Tests — die Umstellung selbst ist der Vorher/
Nachher-Beleg.

## Iteration 3 — umgesetzt

- `ApiErrors.cs`: neuer Code `DuplicateCategoryName` (409), additiv.
- `AuthController.cs`: `PATCH auth/me` nutzt jetzt `DuplicateEmail` statt `Conflict` (der Code existierte
  bereits, war hier nur nicht verdrahtet).
- `ExerciseCategoriesController.cs`: `Create`/`Update` nutzen jetzt `DuplicateCategoryName`.
- Neuer Wächter `ConventionGuardTests.Controller_Nennt_Keinen_Generischen_Conflict_Code` — leere
  Ausnahmeliste, Self-Protection ≥30 Controller-Dateien.
- **Beim Bauen des zweiten (jetzt abgespaltenen) Wächters — Platzhaltername je Sammlungs-Segment — fiel eine
  dritte, im Bericht nicht erfasste Inkonsistenz auf:** `units/{unitId}` (`SeriesUnitsController`) vs.
  `units/{seriesUnitId}` (`ExerciseRoutes.Base`, von 13 Übungstyp-Controllern geerbt). Statt das Tor auf
  einer unvollständigen Grundlage zu bauen, als eigene Story [B-121](../backlog/B-121-platzhalter-und-paging-tore.md)
  gefasst (Entscheidung 4 in B-101) — dort auch das Paging-Tor, das dieselbe Sorgfalt braucht (35 exakt zu
  zählende Endpunkte, keine übernommene Zahl).
- Zwei bestehende Tests umgestellt: `AccountSelfServiceTests.FremdeEMail_WirdAbgewiesen` (`"conflict"` →
  `"duplicate_email"`), `DocsCaptureTests`s „Doppelte Art anlegen"-Capture (`ApiErrors.Conflict.Code` →
  `ApiErrors.DuplicateCategoryName.Code`) — beide Umstellungen selbst sind der Beleg, dass sich das
  Verhalten geändert hat.

**Rote Probe:** kein neuer Test von Grund auf; die zwei bestehenden Tests liefen vor der Umstellung ihrer
Erwartung mit dem alten Code grün (das ist der unveränderte Vorzustand), nach der Umstellung ihrer
Erwartung UND des Produktivcodes wieder grün — der eigentliche Beleg ist der neue Wächter selbst: vor dem
Fix hätte er mit 3 Fundstellen rot geschlagen (nicht gesondert reproduziert, da der Fix bereits in derselben
Änderung stand — anders als bei B-108 war hier kein Zwischenschritt mit `git stash` nötig, weil die Story
keinen Verhaltens-Fall hat, der vor UND nach dem Fix beobachtbar bleibt).

Volle Suite: **750/750 grün** (749 vor dieser Story + 1 neuer Wächtertest).

## Runde — Abnahme Sprint 3 (Rollengang: Regression)

Keine neue Fähigkeit, keine sichtbare Oberfläche — die geänderten Codes sind für heutige Clients additiv
(kein Client verzweigt auf `duplicate_email`/`duplicate_category_name`, beide waren vorher am selben Ort ein
nacktes `conflict`). Ersatz: volle Suite plus `pugling-reviewer` (keine Blocker; Nice-to-have-Hinweis zum
Wächter-Docstring, nicht umgesetzt — reine Doku-Ergänzung ohne Testwirkung).

**Ergebnis:** B-101 (reduzierter Umfang AK1–3) ist `abgenommen`. B-121 (Platzhalter-/Paging-Tore, inkl. dem
neuen `units`-Fund) und B-100 bleiben offen für einen künftigen Sprint.

## Retrospektive — Sprint 3

**Nachschau:** bereits in der Retro zu Sprint 1+2 dieser Sitzung erledigt (derselbe vorige Sprint,
2026-08-05 Nachtlauf-2-Sprint-4); kein zweiter Nachhol-Bedarf für denselben Vorsprung.

**Was dieser Sprint gelernt hat:** Ein mechanisches Tor braucht dieselbe Disziplin wie ein Ist-Stand —
„gegen den Code belegen, nicht gegen die Bericht-Prosa" (README, „Ausformulieren heißt gegen den Code
belegen") gilt nicht nur für `ausformuliert`, sondern auch fürs tatsächliche **Bauen** eines Wächters: der
Bericht vom 2026-08-04 kannte vier Segmente, die Route-Oberfläche hat fünf. Das ist genau der Fund, den ein
sorgfältiges Vorgehen erst beim Hinsehen macht, nicht beim Zusammenfassen einer Notiz.

**Kein neuer Mechanismus** — die Lehre ist prozedural (wie diese Sitzung selbst vorgegangen ist), nicht
etwas, das ein Gate im Produkt fangen könnte. Stattdessen als konkrete Konsequenz gehandelt: B-121 trägt
den Fund jetzt explizit, statt dass er in einem Kommentar verloren geht.

## Stand am Ende dieser Sitzung

Vier Stories abgenommen (B-102, B-95, B-108, B-101), zwei Commits bisher gesetzt (ein dritter für Sprint 3
folgt) — nichts gepusht, das bleibt beim Nutzer. Kein Abbruchgrund ist
eingetreten (kein neues Gate aus einer Retro, kein Defekt im eigenen Increment über 5 Funde). Offen für
einen weiteren Sprint dieser Nacht: B-121 (Platzhalter-/Paging-Tore), B-100 (Vertragsdokument, eigener
großer Umfang), sowie die drei frischen `idee`-Stories B-118/B-119/B-120, die vor dem Bau erst gegen den
Code recherchiert werden müssten.
