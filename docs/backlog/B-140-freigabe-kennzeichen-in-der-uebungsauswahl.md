---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/supervisor]
aliases: [zurückgezogen in der Übungsauswahl, exercise_not_executable erst beim Speichern, canAssign]
status: abgenommen
prio: P3
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-11
---

# B-140 · Die Übungsauswahl verschweigt, dass eine Übung zurückgezogen ist

Wer eine Position in einen Lehrplan aufnimmt, wählt aus der geteilten Bibliothek — Lesen ist bewusst
global, jeder Creator findet jede Übung. Zurückgezogene Übungen tragen in der **Verwaltung** dafür ein
sichtbares Kennzeichen (`pill mag` „zurückgezogen", `VaterExercises.tsx:321-324`); in der **Auswahl** beim
Zuweisen fehlt es. Der Betreuer wählt also eine Übung, die er nicht zuweisen darf, und erfährt es erst
beim Speichern als `exercise_not_executable`.

Die Daten liegen bereit: `ExerciseSummary.executePublic` steht im Vertrag
(`backend/Pugling.Contracts/Creator/ExerciseCatalogDtos.cs:15`) und wird in derselben Liste schon geladen.
Zu ergänzen wäre dasselbe Kennzeichen an zwei Stellen — `PlanPositions.tsx:411ff` und der Lehrplan-Assistent
(`VaterWizard.tsx:108`). (Die Zeilenangaben dieses Absatzes sind die ursprünglichen Reviewer-Angaben; belegt ist der
Abschnitt „Ist-Stand am Code“.)

**Warum nicht in B-11 mitgenommen:** B-11s Ziel ist ohne diese Änderung erfüllt — der Creator kann beim
Anlegen entscheiden, und das Kennzeichen erscheint dort, wo er sein eigenes Material verwaltet. Hier geht es
um eine **andere Rolle** (der zuweisende Supervisor) auf einer **anderen Fläche** (die Auswahl im Plan-Bau).

**Was B-11 daran ändert, und deshalb steht die Story überhaupt hier:** „zurückgezogen" war bisher ein
seltener Nachträglich-Zustand — jemand nahm etwas aus dem Verkehr. Seit B-11 ist es ein regulärer
**Anfangszustand**, den ein Creator mit einem Haken beim Anlegen wählt. Damit wird aus einem
Randfall ein Alltagsfall, und eine Lücke, die vorher niemanden traf, trifft jetzt jeden, der Material aus
der geteilten Bibliothek zusammenstellt.

## User Story

Als *Vater* möchte ich schon in der Übungsauswahl (Planbau und Assistent) sehen, welche Übungen ich zuweisen darf, und
nicht zuweisbare gar nicht erst wählen können, damit ich keinen Plan zusammenstelle, der erst beim Speichern mit
`exercise_not_executable` scheitert.

## Ist-Stand am Code (2026-11-14)

- Kennzeichen „zurückgezogen“ nur in der Verwaltung: `frontend/src/vater/VaterExercises.tsx:402-404`.
- `ExecutePublic` im Vertrag: `backend/Pugling.Contracts/Creator/ExerciseCatalogDtos.cs:35` (und `:58`); ein
  `CanAssign` o. ä. gibt es nicht.
- Auswahl beim Zuweisen: `frontend/src/vater/PlanPositions.tsx:481` und Assistent `frontend/src/vater/VaterWizard.tsx:143` –
  beide lesen `executePublic` nicht.
- Fehler erst beim Speichern: `exercise_not_executable` (`backend/Pugling.Api/Errors/ApiErrors.cs:181`), geworfen in
  `Controllers/Supervisor/PlanPositionsController.cs:156` und `KlassenarbeitenController.cs:342`.
- Die Regel „darf zuweisen“: `ExercisePermissionService.CanExecuteAsync` (`backend/Pugling.Api/Auth/ExercisePermissionService.cs:44-50`)
  = `ExecutePublic` **oder** Admin **oder** irgendein Grant des anfragenden Erwachsenen (Owner/Write/Execute). Darum ist
  `executePublic = false` allein kein „nicht zuweisbar“ (Nachprüfung 2026-10-23).
- Die Suche projiziert `IsOwn`/`IsOwner` schon in SQL über `e.Grants` (`Controllers/Creator/ExerciseCatalogController.cs:116-122`) –
  dieselbe Stelle kann `CanAssign` mitliefern, ohne Zusatzabfrage.
- Planbau: die Radio-Zeile `PlanPositions.tsx:620-660`, gesperrt heute nur bei `zeilenVeraltet` (`:632`); Absende-Knopf `:702-703`.
- Assistent: Checkbox je Zeile `VaterWizard.tsx:801`; „Alle wählen“ übernimmt bis zu 500 Ids vom Server
  (`selectAll`, `:294-317`) – ohne Blick auf die Zuweisbarkeit.
- Dritte Auswahl mit derselben Suche: Klassenarbeit (`VaterClassTests.tsx:181`, Server-Prüfung `KlassenarbeitenController.cs:341`).

## Die echte Lücke

Der Server kennt die Antwort („darf dieser Erwachsene diese Übung zuweisen?“), liefert sie aber nicht mit der Trefferzeile.
Das Frontend kann sie nicht selbst rechnen, ohne die Grant-Regel zu kopieren. Folge: eine Auswahl, die erst beim
Speichern Nein sagt – im Assistenten nach „Alle wählen“ für Hunderte Übungen auf einmal.

## Offene Punkte

1. ~~Kennzeichen „zurückgezogen“ oder serverseitiges „für dich zuweisbar“?~~ → Entscheidung 1 (Nutzer, G-4).
2. ~~Wie bleibt die Regel eine Regel (Projektion vs. `CanExecuteAsync`)?~~ → Entscheidung 2.
3. ~~Welcher Grund steht an der gesperrten Zeile, und woher kommt er?~~ → Entscheidung 3.
4. ~~Ausblenden oder sperren?~~ → Entscheidung 4.
5. ~~Was tut „Alle wählen“ im Assistenten?~~ → Entscheidung 5.
6. ~~Gehört die Klassenarbeit-Auswahl dazu?~~ → Entscheidung 6 (zurückgestellt).

## Entscheidungen

1. **Serverseitiges „für dich zuweisbar“ als additives Feld `CanAssign` an `ExerciseSummary`.** Entscheidung des
   Nutzers in G-4 (2026-10-23): Rolle, Eigentum und Grants zählen, nicht zuweisbare Übungen sind gesperrt und nennen den
   Grund, das Frontend rechnet nichts selbst. Kosten: ein Feld mehr je Trefferzeile, `contract.ts` neu erzeugen.
2. **Eine Regel, gehalten von einem Gleichheitstest.** `CanAssign` wird in der Such-Projektion in SQL gerechnet wie
   `IsOwn` (`e.ExecutePublic || isAdmin || e.Grants.Any(g => g.CreatorId == fid)`); ein Integrationstest prüft für jede
   Konstellation, dass `canAssign` dasselbe sagt wie ein echtes `POST …/positions`. *Stakeholder-Entscheidung im Auftrag
   des Nutzers.* Begründung: eine geteilte C#-Methode ließe sich in der SQL-Projektion nicht ohne Nachladen der Grants je
   Zeile verwenden; der Test macht die zwei Schreibweisen zu einer überprüften Regel. Kosten: die Regel steht an zwei
   Stellen; wer `CanExecuteAsync` ändert, bekommt den Test rot, nicht stillen Drift.
3. **Der Grund ist ein fester Satz im Frontend:** „Nicht zuweisbar – der Ersteller hat die Übung zurückgezogen; zuweisen
   darf nur, wem er sie freigegeben hat.“ *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: `canAssign =
   false` hat heute genau **einen** Grund (nicht öffentlich und kein Grant); ein Grund-Code im Vertrag wäre Vorrat ohne
   Verwendung. Kosten: kommt ein zweiter Grund, braucht der Vertrag ein Feld `assignBlockedReason` – dann bricht nichts,
   es wird additiv ergänzt.
4. **Gesperrt, nicht ausgeblendet.** Die Zeile steht in der Liste, ihr Radio/ihre Checkbox ist `disabled`, der Grund steht
   als Text daneben (nicht als `title`, B-160) und ist per `aria-describedby` verbunden; Trefferzahl und Sortierung zählen
   sie weiter mit. „Ausprobieren“ (Testmodus, B-424) bleibt möglich – Lesen ist global. *Stakeholder-Entscheidung im Auftrag
   des Nutzers.* Begründung: der Vater soll sehen, dass es die Übung gibt (und beim Ersteller um Freigabe bitten können);
   eine verschwundene Übung wirkte wie ein Suchfehler. Kosten: gesperrte Zeilen nehmen Platz in der ersten Seite.
5. **„Alle wählen“ übernimmt nur zuweisbare Übungen** und meldet die übersprungenen („… gewählt, N nicht zuweisbare
   übersprungen“). *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: sonst scheitert der Assistent am Ende an
   genau dem Fehler, den diese Story verhindern soll – mit Hunderten Positionen. Kosten: eine Zahl mehr im Hinweis.
6. **Die Klassenarbeit-Auswahl (`VaterClassTests.tsx:181`) ist zurückgestellt.** *Stakeholder-Entscheidung im Auftrag des
   Nutzers.* Begründung: G-4 nannte die Auswahl im Planbau; die Klassenarbeit liest dasselbe Feld später ohne
   Backend-Arbeit nach, und `VaterClassTests.tsx` wird von B-380 umgebaut (Konflikt). Kosten: dort bleibt bis dahin der
   Fehler beim Speichern. Zurückgestellt, keine eigene Story-Datei.

## Akzeptanzkriterien

1. Integrationstest (Such-Endpunkt `GET api/v1/creator/exercises`): `canAssign` ist `true` für eine öffentliche Übung,
   für eine zurückgezogene mit Owner-, Write- **und** Execute-Grant des Anfragenden und für einen Admin; `false` für eine
   zurückgezogene ohne Grant. **Rot**, wenn die Projektion etwa nur Owner/Write zählt (Execute-Grant fehlt) oder
   `ExecutePublic` ignoriert.
2. Derselbe Test vergleicht je Fall `canAssign` mit dem Ergebnis von `POST …/study-plans/{id}/positions` (201 bzw. 400
   `exercise_not_executable`). **Rot**, wenn `CanExecuteAsync` und die Projektion auseinanderlaufen.
3. Planbau: eine Zeile mit `canAssign: false` hat ein gesperrtes Radio und den Grund-Text, per `aria-describedby`
   verbunden; eine zuweisbare Zeile nicht (Vitest in `PlanPositionsAdd.test.tsx`). **Rot**, wenn `disabled` nur an
   `zeilenVeraltet` hängt.
4. Assistent: die Checkbox einer nicht zuweisbaren Zeile ist gesperrt; „Alle wählen“ übernimmt nur Ids mit
   `canAssign: true` und nennt die Zahl der übersprungenen (Vitest in `VaterWizard.test.tsx`). **Rot**, wenn `selectAll`
   alle Ids übernimmt.
5. E2E: ein Creator zieht eine Übung zurück (bestehender Weg aus B-11), ein zweiter Erwachsener ohne Grant sieht sie im
   Planbau gesperrt mit Grund und kann sie nicht wählen. **Rot** wie AK 3, an der laufenden App.

## Schätzung

**Größe: M** (unterer Rand) – Backend ein Projektionsfeld und ein Tabellentest; Frontend zwei Flächen (Radio sperren,
Checkbox sperren, `selectAll` filtern) mit je einem Komponententest und eine E2E. Vergleich: kleiner als B-535, größer
als B-540.

- **`migration: nein`** – keine Spalte, `ExecutePublic` und `ExerciseGrants` gibt es.
- **`vertragsbruch: nein`** – `CanAssign` ist ein additives Antwortfeld. (Positionsparameter am Ende des Records; Tests,
  die `ExerciseSummary` von Hand bauen, ziehen nach.)

**Risiken:**

- Die Suche ist auf `Roles.Creator` gegated (`ExerciseCatalogController.cs:22`), das Zuweisen auf `Roles.Supervisor`; ein
  Vater ist beides. Ein reines Lehrer-Konto (nur Creator) bekommt `canAssign` auch – harmlos, es weist nicht zu.
- Der zweite Erwachsene für AK 5 braucht ein Konto ohne Grant; die E2E legt ihn an (Muster der B-11-Specs) statt den Seed
  zu verändern.
- **Konflikt mit B-16 und B-540** in `PlanPositions.tsx` (Radio-Zeile `:620-660`); dazu `VaterWizard.tsx`. Nacheinander bauen.

**Angriffsplan (Backend zuerst):**

1. `ExerciseSummary.CanAssign` (Vertrag) + Projektion in `ExerciseCatalogController.cs:116ff`; Integrationstest AK 1+2.
2. `contract.ts` neu erzeugen; Doku-Capture (`docs/api-examples`) prüfen.
3. Planbau: Radio `disabled={zeilenVeraltet || !ex.canAssign}`, Grund-Text mit Id (AK 3).
4. Assistent: Checkbox sperren, `selectAll` filtern + Hinweis (AK 4).
5. E2E (AK 5) als eigene Spec-Datei (eine Fläche je Spec).

**Testweg:**

- Integrationstest in `Pugling.Api.Tests` (AK 1, 2), Tabellenform.
- Vitest: `PlanPositionsAdd.test.tsx` (AK 3), `VaterWizard.test.tsx` (AK 4).
- Playwright: neue Spec `frontend/e2e/auswahl-nicht-zuweisbar.spec.ts` (AK 5); zählt als Rollengang.

## Verlauf

- **2026-08-10** — angelegt beim Bauen von [B-11](B-11-uebungen-veroeffentlichen.md): Fund des
  `frontend-reviewer` neben dem Diff. Ungeprüft aufgenommen (`unverifiziert: true`) — die Belege sind
  Reviewer-Angaben, nicht eigene Recherche.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (Kennzeichen nur in `VaterExercises.tsx:325-327`; `executePublic` kommt in `PlanPositions.tsx`/`VaterWizard.tsx` nicht vor; DTO jetzt `ExerciseCatalogDtos.cs:24`). **Haken:** `executePublic=false` heißt nicht „nicht zuweisbar“ – Eigentümer und Grant-Inhaber dürfen weiter zuweisen (`VaterExercises.tsx:258`). Ein reines „zurückgezogen“-Kennzeichen führte sie in die Irre; zu entscheiden ist, ob die Liste ein serverseitiges „für dich zuweisbar“ braucht.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-4 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): die Auswahl zeigt ein **serverseitiges „für dich zuweisbar“**, ein additives Feld je Übung (z. B. `canAssign`), das Rolle, Eigentum und Grants berücksichtigt. Nicht zuweisbare Übungen sind gesperrt und nennen den Grund. Das Frontend rechnet nichts selbst. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidung selbst ausformulieren, schätzen und bauen (Paket NP-17); trifft er auf eine Produktfrage, die die Entscheidung nicht hergibt, hält er an.
- **2026-11-14** — Abgleich Backlog-Lauf: veraltet – Zeilenangaben berichtigt (Abschnitt „Ist-Stand am Code“); `canAssign` gibt es weder im Vertrag noch im Frontend, die Lücke besteht.
- **2026-11-14** — Refinement Backlog-Lauf: ausformuliert (User Story, Ist-Stand um Regel `CanExecuteAsync`, Projektion und `selectAll` ergänzt, `unverifiziert` entfernt), gegrillt (Entscheidung 1 vom Nutzer aus G-4, 2–6 als Stakeholder-Entscheidungen im Auftrag des Nutzers: SQL-Projektion mit Gleichheitstest, fester Grund-Satz, sperren statt ausblenden, „Alle wählen“ filtert, Klassenarbeit-Auswahl zurückgestellt), geschätzt: M, `wo: beides`, keine Migration, kein Vertragsbruch.
- **2026-11-14** — Sprint 74, Frontend gebaut: fester Grund-Satz `NICHT_ZUWEISBAR_GRUND` und reine Regel `nurZuweisbare` (`frontend/src/vater/wizardSearch.ts`); Planbau-Radio `disabled={zeilenVeraltet || !ex.canAssign}` mit Grund-Text per `aria-describedby`, „Ausprobieren“ bleibt bedienbar (`PlanPositions.tsx`); Assistent: Kästchen gesperrt + Grund, „Alle wählen“ übernimmt über `nurZuweisbare` nur Zuweisbare und meldet die übersprungenen im Auswahl-Hinweis (`VaterWizard.tsx`). Tests: `PlanPositionsAdd.test.tsx` „sperrt das Radio, nennt den Grund und lässt die zuweisbare Zeile frei“ (AK 3); `VaterWizard.test.tsx` „sperrt das Kästchen und nennt den Grund“, „„Alle wählen“ übernimmt nur zuweisbare und nennt die übersprungenen“, „schweigt, wenn alles zuweisbar ist“ (AK 4); neue E2E `e2e/auswahl-nicht-zuweisbar.spec.ts` (AK 5, prüft dazu das 403 `exercise_not_executable` des Servers). Grenze: AK 4 hält die Regel, nicht ihre Aufrufstelle in `selectAll` (ein Rückbau dort auf `items.map(e => e.id)` fängt kein Vitest).
- **2026-11-14** — Sprint 74, Lücke an der Aufrufstelle geschlossen: `e2e/auswahl-nicht-zuweisbar.spec.ts` gemeinsamer Aufbau `aufbau()` und zweiter Fall „„Alle wählen“ im Assistenten überspringt die zurückgezogene Übung und sagt es“ (Kästchen gesperrt mit Beschreibung, Hinweis „1 Übung gewählt, 1 nicht zuweisbare übersprungen.“, zurückgezogene nicht gewählt). Rote Probe: `VaterWizard.tsx` `selectAll` auf `setSelected(filteredExercises.map(…))` zurückgebaut → Fall rot (Hinweis fehlt). Weiter ungedeckt: der 500er-Nachschlag-Zweig `uebernehmen(alle.items)` (braucht mehr Treffer als eine Seite).
- **2026-11-14** — **abgenommen** (Sprint 74). volle Suite 1621/1621 (Release), Vitest 1023/1023 (drei volle Läufe in Folge), `tsc -b` grün; Rollengang Vater als Playwright gegen frisch gestartete Server nach dem letzten Edit: 19 passed (`auswahl-nicht-zuweisbar` 3 Fälle, `lernkontrolle-sichtbar`, `planbau-bewertung`, `vater-von-null`, `assistent`, `uebung-teilspeichern`, `interessen`, `bonus-deckel`, `kind-lehrbuch-fach`, `responsive-vater`, `full-flow`); Reviews `pugling-reviewer` + `frontend-reviewer`, Funde im Sprint behoben; Commit `defa3ea0`. Mutanten rot: N5 Execute-Grant fehlt, N6 `ExecutePublic`
  ignoriert, N7 Admin fehlt (AK 1/2, sechs Fälle gegen echtes Zuweisen – der Server antwortet 403, nicht 400), N12
  Planbau-Radio (AK 3), N13 Assistent-Kästchen und N14 „Alle wählen“-Filter (AK 4; die Aufrufstelle hält die E2E,
  Rot-Probe vom Bau-Agenten gemessen), N16 Klassenarbeit (Review-Fund: Entscheidung 6 hatte sie zurückgestellt, die
  Grenze war offen – nachgezogen, E2E-Fall). Grund-Satz nach Review neutral: „hat sie nicht für alle freigegeben“
  (gilt auch für nie freigegebene Übungen). Bewusst offen: der Grund wird doppelt vorgelesen (Span im Label und per
  `aria-describedby`), Kosmetik. Ungedeckt: `VaterWizard.tsx` `uebernehmen(alle.items)` (greift erst bei mehr Treffern
  als einer Seite).
