---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Lernstand Leerfall zählt gewertete Antworten, Übung löschen kündigt Wirkung an]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 41 in der Retro von Sprint 42)
unverifiziert: false
entgangen_bei: [B-467, B-418]
nachgeschaut: 2026-11-12
grund: ""
ersetzt_durch: []
---

# B-499 · Nachschau Sprint 41: der Leerfall des Lernstands misst noch immer nicht, was er sagt – und „Übung löschen" kündigt an, was nie passiert

Nachschau von Sprint 41 (frischer `frontend-reviewer` an `1c7b567`; sechs Vitest-Dateien des Commits 55/55; gelesen und
gegen den Server nachgerechnet, nicht im Browser).

1. **🟡 „Das Kind hat noch keine Frage beantwortet" stimmt nicht** (`frontend/src/vater/VaterLernstand.tsx:106`,
   Kommentar `:63-66`). Die Liste liest `ItemProgress`, und eine Zeile entsteht nur bei `countsForMastery`
   (`ItemProgressService.cs:51`); gewertet wird nur `(typed || !RequireTypedTest) && due && !alreadyScoredToday &&
   !displayOnly && !selfClaimRung` (`PositionPracticeController.cs:402-403`). Ein Kind, das neue Wörter auf der
   Selbsteinschätzungs-Stufe (B-476) beantwortet, hat Antworten, aber keine Zeile. Gemessen ist „gewertete Antwort".
   Dazu: der neutrale Zweig (lädt/scheitert) hat keinen Test – die Mutante „immer Erfolg" überlebt, weil `findByText` auf
   den Endzustand wartet; „Nochmal" in `ListenAusfall` lädt `anyWord` nicht neu; `anyWord` liest `stale` nicht (kurz der
   Erfolg des vorigen Kindes bei Zurück/Vor).
2. **🟡 Dieselbe Form wie B-418:** `frontend/src/vater/VaterExercises.tsx:343` fragt „Zuordnungen in Lehrplänen können
   betroffen sein" – der Server lehnt jede benutzte Übung ab (`ExerciseControllerBase.cs:504-507`, `exercise_in_use`).
3. **🟢 Skin-Kauf mit zu wenig Gems** zeigt dem Kind die Rohzeile (`SohnSkins.tsx:76` sperrt nicht wie `SohnShop.tsx:227`;
   `400 insufficient_gems` steht in keiner Tabelle – das 409-Tor aus B-411 sieht 400er nicht). Nur per Rennen:
   `insufficient_coins`, `shop_listing_inactive`, `insufficient_inventory`.
4. **🟢** Arcade-Fehler aus `useAsync` gehen über `errorMessage` statt `useErrorMessage` (`SohnHome.tsx:30/64`,
   `SohnProgress.tsx:16/30`, `SohnKonto.tsx:30`) – älter, praktisch folgenlos.

## Akzeptanzkriterien

1. **Der Leerfall des Lernstands nennt, was der Server zählt** (gewertete Antworten), und meldet nie Erfolg aus einer
   Zahl, die nicht zu diesem Kind gehört oder nicht geladen ist; „Erneut laden" holt beide Abfragen.
2. **„Übung löschen" kündigt keine Wirkung an, die der Server ablehnt**, und nennt, was wirklich passiert.
3. **Vier Sperren aus Kauf und Einlösen** (`insufficient_gems`, `insufficient_coins`, `shop_listing_inactive`,
   `insufficient_inventory`) sprechen in der Arcade einen eigenen Satz, auf Deutsch und Englisch.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Sprint 44 auf Zuruf „Ja, B-499")

1. **„noch keine Antwort des Kindes wurde gewertet"** – das Wort des Servers (`scored`, `countsForMastery`), ohne die
   Stufen aufzuzählen; der Kommentar am Code nennt die Quelle.
2. **`stale` zählt als „unbekannt"** → neutraler Satz, nie der Erfolg des vorigen Kindes.
3. **Skins nicht vorab sperren:** anders als der Shop (`affordable` vom Server) gibt es für Skins keine Server-Aussage;
   ein Sperren nach dem HUD-Stand hinge an einer Zahl, die veraltet sein kann. Der Satz zur Sperre genügt.
4. **Teil 4 (Arcade-Fehler aus `useAsync` über `errorMessage`) bewusst nicht:** älter, und `plans`/`overview`/Verlauf
   liefern keinen der `problem.*`-Codes – der Umbau von `useAsync` auf einen Formatierer wäre größer als der Nutzen.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 41, Retro Sprint 42 im Nachtlauf).
- **2026-11-12** — Sprint 44 (auf Zuruf „Ja, B-499"): autonom gegrillt (`art: Defekt`), geschätzt S, umgesetzt.
- **2026-11-12** — Rote Probe vor dem Fix: erwartet 8 rot, gemessen 8 rot – `VaterLernstand.test.tsx` 3 (Satz ohne
  „gewertet"; „Erneut laden" holt die zweite Abfrage nicht; nach Kinderwechsel „sitzt alles" statt des neutralen Satzes),
  `VaterExercises.test.ts` 1 (nach Herausziehen der alten Frage als Konstante), `arcadeSpielweg.test.tsx` 4 (Server-Zeile
  mit Ref). Ein erster Lauf fiel am falschen Ort: ein ASCII-`"` im Testnamen beendete den String (Parse-Fehler, „no
  tests"), korrigiert.
- **2026-11-12** — Mutanten (`mutant-probe.py`, Vitest): L1 Satz „beantwortet", L2 „Erneut laden" nur Liste, L3 `stale`
  ignoriert, L4 neutraler Zweig fehlt (die Mutante, die die Nachschau überleben sah), E1 alte Lösch-Frage, P1
  Arcade-Schlüssel fehlen, P2 deutscher Satz fehlt – 7/7 rot, Grundlinie grün.
- **2026-11-12** — Belege: Vitest 782/782, `tsc -b` sauber, `ProblemTextGuard`/`ErrorCodeTests` 13/13, Playwright auf
  Ausweichports 4/4 (`full-flow`, `vater-von-null`, `loeschen-meldet-sich`) und 4/4 (`bestaetigung-ueberlebt`,
  `creator-lehrwerk-weg` – beide nehmen einen Lösch-Dialog an).
- **2026-11-12** — `frontend-reviewer`: **ein 🔴, zwei 🟡 – Review-Zähler Sprint 44: 3.** 🔴 `insufficient_inventory`
  erreicht auch den Vater (zwei Aktivierungen genehmigen, `ShopController.cs:441`), und „Davon hast du gerade nichts
  mehr" sprach das Kind an und sagte „nichts" statt „nicht genug" → „Davon ist nicht mehr genug im Inventar.", Test
  über den Vater-Weg (`errorMessage`). 🟡 „Übung löschen" verschwieg den Lernstand der Kinder (Cascade `ExerciseItem` →
  `ItemProgress`) → die Frage nennt ihn. 🟡 dieselbe Form bei Reihe und Unit (`VaterLehrwerke.tsx`, „verlieren nur die
  Angabe") → `deleteSeriesQuestion`/`deleteUnitQuestion` nennen Übungen und Lernstand. Rote Proben dazu: 3/3 (eine zuerst
  mit einem Backspace-Zeichen statt `\b` in der Regex – fiel an der zweiten Zusicherung, korrigiert). Bestätigt:
  „gewertet" stimmt für Üben und Test; die drei Lernstand-Tests tragen je einen Fall. Reste → [B-502](B-502-reste-review-sprint-44.md).
- **2026-11-12** — Mutanten nach der letzten Änderung (Vitest): L1–L4, P1, P2, E1 (Lösch-Frage ohne Lernstand), E2 (Unit
  verschweigt Übungen), I1 (Inventar spricht das Kind an) – 9/9 rot, Grundlinie grün.
- **2026-11-12** — **abgenommen.** Vitest 785/785, `tsc -b` sauber, Backend-Tore 13/13, Playwright auf Ausweichports 8/8
  nach der letzten Änderung (`full-flow`, `vater-von-null`, `loeschen-meldet-sich`, `bestaetigung-ueberlebt`,
  `creator-lehrwerk-weg`; die Lösch-Specs nehmen den Dialog nur an, den Wortlaut prüft keine).
  **Rollengang (Vater) als Maschinengang**; nicht im Browser angesehen: die drei Lösch-Fragen und der Inventar-Satz.
- **2026-11-12** — Nachschau (Retro Sprint 45): frischer `frontend-reviewer` an `a719ed7` (gelesen per `git show`).
  Geprüft: wer die vier Codes bekommt (`MeController.cs:186/291/292/351`, `ShopController.cs:441`; „deine Gems/Münzen"
  erreicht nur das Kind), i18n-Gleichstand (Deutsch typgeprüft über `i18next.d.ts`), die drei Lösch-Fragen gegen
  `ExerciseControllerBase.cs:505-507`, `SeriesUnitsController.cs:164-169`, `TextbookSeriesController.cs:296-301` und die
  Cascades, `stale`/`reload` der Zählabfrage, je Test die Änderung, die ihn rot macht. Kein 🔴/🟡; drei 🟢 →
  [B-504](B-504-nachschau-reste-sprint-44.md).
