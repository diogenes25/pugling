# PM-Sitzung: Nachtlauf 2026-11-23-a (Lauf A, Worktree)

**Datum:** 2026-11-22 (Shell-Datum; der Dateiname folgt dem Auftrag „Nacht auf den 23.")  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Backlog-Lauf nach `pm-loop` im Worktree `claude/nachtlauf-a-be895d`, parallel zu Lauf B: zuerst B-791, dann
B-670, dann backend-/fachlastige Stories nach Priorität.

**Rahmen.** Autorisierter Backlog-Lauf (Freigaben 1–10 aus [nachtlauf.md](nachtlauf.md)), dazu die übertragene
Nutzer-/PO-Rolle: stellvertretend entscheiden statt stoppen, jede solche Entscheidung ist unten markiert und umstoßbar.
Ausgenommen (Lauf B): B-787, B-788, B-659 und alles zu flackernden Frontend-/E2E-Tests. Neue Nummern nur B-800…B-819.
Kein Push, kein Merge, kein Server auf :5200; E2E auf 5211/5184 mit Wegwerf-DB.

**Ausgangsstand.** Branch war auf `560bd2f0` (älter als `main`); per `git merge --ff-only main` auf `ec0c705a` vorgespult
(ein `reset --hard` wurde von der Sitzungs-Sperre abgelehnt; der Vorlauf war verlustfrei, weil `560bd2f0` Vorfahr von
`main` ist). `frontend/node_modules` als Junction auf den Haupt-Checkout; `contract.ts` im Worktree neu erzeugt.

## Runde 1 — Feedback

Kein eigener Rollengang vorab: der Auftrag nennt die Stories, und beide Startstories stammen aus Reviews (B-791) bzw. einer
Nachschau (B-670). Die Rollen sprechen in den Rollengängen der Sprints.

## Sprint 115 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Vaters ist ein Feld, das er in einem Bearbeiten-Formular außerhalb des Shops leert und
speichert, danach wirklich leer. Widerlegbar durch eine Plan-Beschreibung, Unit-Grammatik oder Audio-URL, die nach dem
Neuladen wieder dasteht.

**Umfang:** [B-791](backlog/B-791-geleerte-felder-ausserhalb-des-shops.md) (im Lauf ausformuliert, gegrillt und geschätzt;
Defekt, autonom).

**Entwickler-Brief:** Plan und Unit leeren am Server schon mit `""` – der Fehler sitzt im Client (`|| null`). Die Audio-URL
übernahm `""` dagegen wörtlich: dort normalisiert jetzt der Server (Senke für Anlegen, PATCH und Batch-PATCH), damit die DB
nur einen Zustand für „keine Adresse" kennt. Kein Vertrag, keine Migration.

**Nebenbei entschieden:** [B-670](backlog/B-670-sonderzeichen-leiste-folgt-der-reihe-nicht-der-richtung.md) **verworfen**
(stellvertretend): der beschriebene Fall ist seit B-597 nicht herstellbar; die Belege des Grillversuchs vom 2026-11-19 sind
nachgeprüft (Prüfpunkte in der Story). Kein Code.

## Iteration 115 — umgesetzt

- **Backend:** `VocabularyStoreController.AudioUrlOrNull` – leere Audio-URL wird beim Anlegen und Ändern `null`, gesetzte
  getrimmt (Senke für PATCH, Batch-PATCH, Anlegen und Import). `UpdateSeriesUnitDto.ClearGrade` (additiv, B-800).
- **Frontend:** `PlanEditForm`, `UnitForm`, `VocabRow` schicken geleerte Textfelder als `""`; `UnitForm` schickt beim
  geleerten Band `clearGrade`.
- **Tests:** `PatchClearFieldTests.B791_*` (2), `B800_*`, `PatchSemanticsTests`-Fall `UpdateSeriesUnitDto/clearGrade`,
  Vitest `VaterFelderLeeren.test.tsx` (3) und ein Fall in `planSubject.test.tsx`, E2E `vater-felder-leeren` (neu).
- **Mutanten:** 15 von 15 rot (M1–M7, T1–T6, E1–E2), Grundlinien danach grün.
- **Reviews:** `pugling-reviewer` (B-791) kein 🟡/🔴, zwei 🟢 behoben; `frontend-reviewer` 🟡 Band der Unit (außerhalb des
  Diffs) → [B-800](backlog/B-800-band-einer-unit-leeren-bleibt-stehen.md), im Sprint gebaut (Freigabe 3), 🟢 `exact` behoben;
  `pugling-reviewer` (B-800) kein 🟡/🔴, ein 🟢 (`unit.grade !== null` statt `!= null`, kosmetisch – der Typ ist
  `number | null`) nicht geändert. **Zähler 0** (beide 🟡/🟢-Funde lagen außerhalb des Diffs bzw. waren 🟢).
- **Volle Suite:** Backend 2028/2028 (Release), Vitest `src/vater` 549/549, `dotnet format Pugling.sln` sauber.
- Commit `d9af0c17`.

## Runde 2 — Rollengang Sprint 115

- **Vater — abgenommen** (E2E `vater-felder-leeren`, Server von Playwright frisch auf 5211/5184 gestartet, nach der letzten
  Code-Änderung): Plan-Beschreibung geleert → nach dem Neuladen weg; Unit-Grammatik und Band geleert → weg; Audio-URL
  geleert → im Formular und am Server leer. „Was ich lösche, ist jetzt überall weg, nicht nur im Shop."
- **Creator — Regressionszeuge:** Vokabel-Store und Units sind seine Fläche; dieselbe E2E fährt sie, `PatchSemanticsTests`
  grün.
- **Sohn — Regressionszeuge:** `full-flow` grün; die Audio-URL erreicht seine Karte nur gesetzt oder `null`.

## Retrospektive (Sprint 115)

**Nachschau:** Sprint 114 nachgesehen (B-789, `nachgeschaut: 2026-11-22`, Prüfpunkte in der Story) – kein Befund. Lauf Bs
Stories (B-602, B-603, B-605, B-790) bleiben Lauf B. Index: Nachgeschaut 533 von 568.

**Was die eigenen Tore durchgelassen haben:** meine eigene Ausformulierung von B-791. Ich habe die Klasse „geleertes Feld
geht als `null`" mit `grep "trim() || null"` abgegrenzt und „kein weiterer Fall" geschrieben; das Zahlenfeld-Muster
`=== "" ? null` lag außerhalb dieser Suche. Der `frontend-reviewer` hat es gefunden (B-800). Kein Fund kam von außen.

**Vorschlag (nur Vorschlag, nicht gelandet – Freigabe 3):** Das Tor aus der Retro von Sprint 114 („`.trim() || null` in
einem Update-Aufruf") greift zu kurz. Besser am Vertrag als an der Schreibweise: ein Vitest, der für jeden `api.update*`-
Aufruf in `src/vater` prüft, dass ein Feld, das `null` sein kann, entweder nie geleert wird oder einen `clear…`-Schalter
bzw. `""` mitbringt – gemessen über die Formular-Komponenten wäre das teuer; billiger ist ein Quelltext-Scan auf beide
Muster (`|| null`, `=== "" ? null`) in den Bearbeiten-Zweigen mit Ausnahmeliste. Das wäre ein neues Tor, darum hier nur
vorgeschlagen.

Kein Halt-Grund: kein Tor gelandet, kein Fund im Increment (Zähler 0).

## Sprint 116 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Sohns endet „bis heute Abend" um Mitternacht deutscher Zeit – eine Antwort um 23:30 zählt für
den alten Tag, eine um 00:30 für den neuen, und jede Tageszahl (Serie, Pflicht, Truhe, Woche, Shop, Zeitachse) sagt dasselbe.
Widerlegbar durch eine Stelle, die um 00:30 noch den Vortag nennt.

**Umfang:** [B-730](backlog/B-730-tagesbegriff-in-deutscher-zeit.md) (P1, `Wunsch`, `geschaetzt` → baubar nach Freigabe 8;
erste Etappe der Planer-Kette aus B-644).

**Entwickler-Brief:** Ein Dienst `LearningDay` (Today, DayOf, StartUtc, LocalNow) als einzige Senke; alle 36 direkten
UTC-Tagesbildungen und die Zeitstempel-Umrechnungen darauf; ein Quelltext-Tor dahinter. Keine Migration, kein Vertragsbruch.
Testweg: eigene Berlin-Fabrik mit eingefrorener Uhr; die übrige Suite bleibt auf UTC.

## Iteration 116 — umgesetzt

- **Backend:** `LearningDay` + `LearningDayOptions` (Startprüfung der Zone), 15 Controller/Dienste umgestellt, `DutyBegins`,
  `DailyTestPortion`, Wochenvergleich der Münzen, Schwachwort-Pflicht, Multiplikator-Uhr; `ShopRefillSchedule` rechnet auf der
  Wanduhr der Zone. Swagger-Texte nennen den Lerntag statt UTC.
- **Frontend:** `lib/datum.ts` – `LERNTAG_ZONE`, `heuteImLerntag`, `beginnDesLerntags`; Arcade-Fristen nach Mitternacht
  deutscher Zeit; Vater-Web belegt „heute" im Lerntag vor; Shop schickt/liest ein einmaliges Auffüllen als Mitternacht
  deutscher Zeit.
- **Tests:** `TagesbegriffTests` (6), `LerntagUmstellungTests` (4), Tor `Tage_Entstehen_Nur_Im_Lerntag`; Vitest-Fälle in
  `datum.test.ts` (5), `planSubject.test.tsx`, `VaterShopEinmalTermin.test.tsx`; umgeschrieben mit Grund: `pfad.test.ts`,
  `SohnHome.test.tsx`.
- **Mutanten:** 16 von 16 rot (D1–D9, F1–F4, S1–S2), Grundlinien grün.
- **Reviews:** beide ohne 🔴; je ein 🟡 (Swagger-Text „UTC" – Doku; Einmal-Auffüllen um 00:00 UTC – außerhalb des Diffs),
  beide behoben, **Zähler 0**. 🟢 überwiegend behoben; offen bewusst: Aufrufer-Tests für Wizard und Zeitachse.
- **Volle Suite:** Backend 2039/2039 (Release), Vitest 1338/1338, `dotnet format Pugling.sln` sauber.
- Commit `3c0122fa`. Root-`CLAUDE.md` „Zeit/UTC" auf die neue Regel korrigiert (eine Zeile ersetzt, keine dazu).

## Runde 3 — Rollengang Sprint 116

- **Sohn — abgenommen** (API an der eingestellten Uhr gegen einen Server in Europe/Berlin, `TagesbegriffTests`; E2E gegen
  frisch gestarteten Server in Europe/Berlin: `pfad`, `truhe`, `erfolge-woche-frist`, `full-flow`): eine Runde um 23:30 zählt
  heute, um 00:30 morgen; ein Wort, nach Mitternacht zweimal richtig, zahlt einmal; die Münze von 00:10 am Montag steht in der
  neuen Woche; der Pfad zeigt die Frist bis Mitternacht deutscher Zeit. „Wenn da ‚noch 2 Std.' steht, stimmt das jetzt auch."
- **Vater — abgenommen** (E2E `vater-zeitachse`, `shop-einmal-termin-bearbeiten`, `vater-felder-leeren`; Shop-Bestand füllt sich
  um Mitternacht Ortszeit, `Taeglicher_Shop_Bestand_…`): Zeitachse und Tagesstatus nennen denselben Tag wie das Kind.
- **Creator — Regressionszeuge:** keine Creator-Fläche im Diff; Suite grün.
- **Nicht maschinell:** ein Gang um echte Mitternacht (die E2E-Uhr ist die Wanduhr) – die Grenze belegen die API-Fälle mit
  eingestellter Uhr.

## Retrospektive (Sprint 116)

**Nachschau:** Sprint 115 nachgesehen (B-791, B-800, `nachgeschaut: 2026-11-22`, Prüfpunkte in den Stories) – kein Befund.

**Was die eigenen Tore durchgelassen haben:** meine Senkensuche im Frontend. Ich habe nach „heute aus der Uhr"
(`toISOString().slice`, `getUTC*`) gesucht, nicht nach der Gegenrichtung „Tag → Zeitpunkt" (`${tag}T00:00:00Z`). Genau dort
saß das einmalige Auffüllen im Shop; gefunden hat es der `frontend-reviewer`. Das Backend-Tor hatte dieselbe Lücke in kleinerer
Form (Muster `TimeOnly.FromDateTime`, `GetUtcNow().Date`) – vom `pugling-reviewer` benannt, im Sprint geschlossen.

**Vorschlag (nur Vorschlag, nicht gelandet – Freigabe 3):** ein Vitest-Tor im Stil von `datum.test.ts`, das in `frontend/src`
außerhalb von `lib/datum.ts` jedes `toISOString().slice(0, 10)` und jedes Literal `T00:00:00Z` in Quelltext meldet – das
Frontend-Gegenstück zu `Tage_Entstehen_Nur_Im_Lerntag`. Ein neues Tor, darum hier nur vorgeschlagen.

## Halt nach Sprint 116 (begründet)

Kein formaler Halt-Grund der Freigaben (kein Retro-Tor gelandet, Zähler 0). Ich halte trotzdem an, aus dem Grund, den die
Regel „neues Tor → Halt" schützt: B-730 hat per Akzeptanzkriterium ein neues Quelltext-Tor gesetzt, die Root-`CLAUDE.md`
geändert und den Tagesbegriff der echten `pugling.db` verschoben – und fünf seiner Entscheidungen (4–8) sind stellvertretend
und unbestätigt. Die nächsten Kandidaten nach Priorität (B-731, dann B-733 ff.) bauen alle auf B-730 auf; sie jetzt zu bauen,
hieße unbestätigte Entscheidungen zu stapeln. Ein Morgen mit dem Nutzer ist billiger als ein Rückbau.

## Stand am Ende von Lauf 2026-11-23-a

- **Branch:** `claude/nachtlauf-a-be895d` (Worktree), Basis `main` `ec0c705a`. Nichts gepusht, nichts gemergt.
- **Abgenommen:** B-791 + B-800 (`d9af0c17`), B-730 (`3c0122fa`). **Verworfen:** B-670 (stellvertretend). Backlog-Commits
  `f8c3f055`, `f05fa2d0` und dieser Abschluss.
- **Neu vergeben:** nur B-800 (Bereich B-800…B-819).
- **Entscheidungen zum Umstoßen** (alle stellvertretend): B-670 verworfen; B-791 E1/E2 (`""` statt Schalter; Audio-URL
  normalisiert der Server); B-800 E1 (`ClearGrade` additiv); B-730 E4–E8 (Suite auf UTC, Shop-Kalender und Arcade-Fristen in
  deutscher Zeit, Zone im Frontend gespiegelt, Multiplikator-Uhr, Seed außen vor).
- **Beim Merge beachten:** `PuglingWebAppFactory`, `ConventionGuardTests`, `lib/datum.ts` und `planSubject.test.tsx` sind
  geändert – kollidiert Lauf B dort, `Tage_Entstehen_Nur_Im_Lerntag` nach dem Merge laufen lassen (ein neuer Aufruf von
  `DateOnly.FromDateTime(…UtcNow…)` aus Lauf B fiele dort auf). Keine Migration.
- **Heimnetz:** beim nächsten Start rechnet „heute" in deutscher Zeit; keine Datenänderung.
- **Offen beim Nutzer:** die Retro-Vorschläge der Sprints 115 (Tor gegen `|| null`/`=== "" ? null` in Bearbeiten-Zweigen)
  und 116 (Frontend-Tor gegen `toISOString().slice(0, 10)`/`T00:00:00Z`); die Aufrufer-Tests für Wizard und Zeitachse.
- **Nächste Kandidaten:** B-731 (nach Bestätigung von B-730), B-732, B-700.
