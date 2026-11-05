---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Fokus bei gesperrtem Knopf ohne Meldung, Mehr laden ohne Fehler]
status: abgenommen
prio: P3
art: Frage
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-228, Entscheidung 2 (Nachtlauf 2026-10-17, zweiter Lauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: "Tastatur-Check im Browser: Kaufhistorie, Mehr laden, Fokusring an der Liste"
---

# B-231 · Wohin gehört der Fokus, wenn ein Knopf ohne Fehlerkasten gesperrt wird?

Ein Knopf, der nach dem Klick `disabled` wird, verliert in Chromium den Fokus auf `<body>` (B-216, im E2E
gemessen). B-216 und B-228 legen ihn dort, wo eine Meldung steht, auf den Fehlerkasten. Für den Normalfall
gibt es keinen Kasten: „Mehr laden" ohne Fehler (`frontend/src/sohn/SohnShop.tsx`, gefüllte Verlaufskarte)
lässt den Fokus weiter fallen – und dasselbe gilt für jeden `disabled={busy}`-Knopf der Hausregel
(`frontend/CLAUDE.md`), sobald er per Tastatur gedrückt wird. Nach dem Laden steht der Fokus am
Dokumentanfang, und der Sohn verliert in einer langen Liste seinen Platz.

Kandidaten: der umgebende Container (`.list`, `tabIndex={-1}`, mit Fokusring), oder `aria-disabled` statt
`disabled` am Knopf (behält den Fokus, bricht aber die Hausregel und Playwrights Actionability-Wartepunkt).
Beides ist eine Gestaltungsentscheidung über alle Knöpfe der Arcade, keine Reparatur eines einzelnen – darum
eine `Frage`, nicht im Nachtlauf entschieden (Freigabe 1).

## User Story

Als **Sohn**, der in der Kaufhistorie per Tastatur „Mehr laden" drückt, möchte ich danach in der Liste weitermachen, statt
am Seitenanfang zu landen.

## Ist-Stand am Code (vor dem Bau, Stand `37e6f6e`)

- `frontend/src/sohn/SohnShop.tsx:392`: `onClick={() => { meldung.current?.focus(); onLoadMore(); }}` – ohne Meldung kein
  Ziel; der gesperrte Knopf gab den Fokus an `<body>` ab (B-216, in Chromium gemessen).

## Entscheidungen

1. **(Nutzer, 2026-10-23)** Der Fokus geht auf den umgebenden Container (`tabIndex={-1}`, sichtbarer Fokusring);
   `disabled={busy}` bleibt Hausregel.
2. **(Bau)** Vorrang: Meldung vor Liste (`(meldung.current ?? liste.current)?.focus()`), Ring über
   `.list[tabindex]:focus-visible` in `index.css`. Die Regel steht in `frontend/CLAUDE.md`; weitere Stellen mit
   gesperrtem Knopf ohne Fehlerkasten sind nicht bekannt (die Vater-Listen blättern über `Pager`, der nicht sperrt).

## Akzeptanzkriterien

1. Nach „Mehr laden" ohne Fehler liegt der Fokus auf der Liste, und die Liste ist per `tabindex="-1"` fokussierbar.
2. Mit Fehlerkasten bleibt der Fokus beim Kasten (B-228 unverändert).

## Schätzung

`XS`, `wo: frontend`. Testweg: Vitest an `HistoryTab`; den Chromium-Fokusverlust selbst deckt die Bauart (wie bei B-228).

## Verlauf

- **2026-10-17** — angelegt aus B-228, Entscheidung 2: der Fehlerfall ist dort behoben, der Fall ohne Meldung
  bewusst ausgespart.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): der Fokus geht auf den **umgebenden Container** (`tabIndex={-1}`, sichtbarer Fokusring); `disabled={busy}` bleibt Hausregel. **Freigabe des Nutzers (Autonomer Modus, Opt-in je Vorhaben, README):** ein Nachtlauf darf diese Story auf Grundlage der getroffenen Entscheidung selbst ausformulieren, schätzen und bauen (Paket NP-14); trifft er dabei auf eine Produktfrage, die die Entscheidung nicht hergibt, hält er an und notiert sie. Rollengang und Reviewer bleiben Pflicht.
- **2026-11-05** — ausformuliert, geschätzt und gebaut in Sprint 1 der Sitzung 2026-11-05 ([Protokoll](../pm-sitzung-2026-11-05.md)). AK 1 → `HistoryTab.test.tsx:127`, Mutanten „`?? liste.current` weg" und (Review-Fund 🟡 2) „`tabIndex={-1}` weg" → je rot (1 von 13). AK 2 → der B-228-Fall `:111`, Mutant „Vorrang vertauscht" → rot (1 von 13). 🟢 3: ein Rerender-Teil, der in happy-dom nicht fallen konnte, ist entfernt.
- **2026-11-05** — **abgenommen** mit einer benannten Lücke: den Fokusverlust des gesperrten Knopfs selbst hat nur Chromium, und eine E2E dafür bräuchte mehr Käufe als eine Seite fasst – der Weg ist nicht im Browser gefahren. Zu prüfen bleibt am Gerät: in der Kaufhistorie per Tastatur „Mehr laden" drücken, der Fokusring steht danach an der Liste. Vitest 648/648, E2E 110/110 (Sohn-Specs grün als Regressionszeugen). Commit: siehe nächster Eintrag.
