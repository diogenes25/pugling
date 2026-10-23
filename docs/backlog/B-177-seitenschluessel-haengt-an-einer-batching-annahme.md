---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Schluessel der Antwort, Batching-Annahme im Assistenten, spaete Kinderliste schaltet auf bestehendes Kind]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer zu B-169 (Nachtlauf 2026-08-14, Fund ausserhalb des Schadens)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-177 · Der Seitenschlüssel hängt an einer Batching-Annahme, die kein Tor hält

B-169 sperrt die Zeilen, solange sie zum vorigen Filter gehören. Der Vergleich stützt sich auf eine
Annahme, die **im Code nur als Kommentar** steht: dass eine Antwort und ein Kriterienwechsel nie im
*selben* React-Batch committen.

## User Story

Als **Vater im Lehrplan-Assistenten** möchte ich, dass die Übungen, die ich anhaken kann, immer zu den Kriterien
gehören, die gerade gelten – und dass der Assistent mein eingetipptes neues Kind nicht gegen ein anderes tauscht.

## Ist-Stand am Code

- `frontend/src/vater/VaterWizard.tsx:259-269`: der Effekt auf `[exercises.data]` setzt `seitenSchluessel` auf den
  **aktuellen** `filterKey` – er unterstellt, dass der Schlüssel noch der ist, mit dem die Antwort abgeschickt wurde.
- `filterKey` (`:175`) hängt an `effectiveGrade`/`effectiveSchoolType` (`:113-116`), die im Modus „existing" aus
  `children.data` über `selectedChild` kommen. Im Modus „neu" kommen sie aus eigenem State.
- `:150-155`: ein Effekt schaltet bei ankommender Kinderliste auf „existing" und das erste Kind, solange
  `touchedMode` falsch ist. **`touchedMode` setzten nur die zwei Pillen** (`:411-419`), nicht die Felder des
  Neues-Kind-Formulars.

## Die echte Lücke

Gemessen, nicht vermutet (Mess-Spec, siehe Verlauf): Die Behauptung der Story in ihrer ursprünglichen Form trägt nicht
– `children.data` allein verschiebt den Schlüssel nie, und im gemessenen Ablauf schloss das Gate eine Frame nach dem
Wechsel. **Der Wechsel selbst ist aber der Defekt:** kam die Kinderliste erst an, nachdem der Vater ein neues Kind
eingetippt hatte (ohne die Pille zu klicken), schaltete der Effekt still auf das erste bestehende Kind – auch noch in
Schritt 2 oder 3, wo der Vater es nicht sieht –, und `finish` hätte den Plan an dieses Kind geschickt
(`existingChildId`, `:344-351`). Derselbe Wechsel ist der einzige Weg, auf dem sich `filterKey` mitten in einer
Übungsabfrage durch die Kinderliste verschieben kann; laut `frontend-reviewer` bleibt dort unter React 18 ein
Macrotask-Fenster, in dem Antwort und Wechsel doch in einem Render landen könnten.

## Offene Punkte

1. ~~Ist der Fall konstruierbar?~~ → gemessen (Verlauf); die Gleich-Batch-Variante nicht beobachtet, der Moduswechsel schon.
2. ~~Wie härten?~~ → Entscheidung 1 (die Ursache schließen statt den Schlüssel in eine Ref zu legen).
3. ~~Gilt dasselbe für `geltenderFilterKey`?~~ → nein: er wird im Effekt auf `[filterKey]` aus derselben Quelle gesetzt (Reviewer bestätigt).

## Entscheidungen

Autonom gegrillt im vierten Nachtlauf (`art: Defekt`, Freigabe 1).

1. **Jede Eingabe im Neues-Kind-Formular gilt als Entscheidung** wie der Pillen-Klick: `onChange` am Container
   (`div.form-grid`) setzt `touchedMode`. Damit kann der Vorwahl-Effekt nach Schritt 0 nicht mehr umschalten –
   Schritt 0 verlässt man im Modus „neu" nur mit Namen (`canAdvance`) –, und `filterKey` kann sich während einer
   Übungsabfrage nicht mehr durch die Kinderliste verschieben. Die Ref-Härtung aus Punkt 2 wird damit unnötig.
   *Kosten:* ein Browser-Autofill im Formular zählt ebenfalls als Entscheidung (plausibel); wer den Assistenten öffnet
   und nichts tippt, bekommt weiter „bestehend" vorgewählt.
2. **Keine Ref für den Schlüssel der Antwort.** Sie hätte das Symptom an einer Stelle gedeckt, die Ursache (der stille
   Moduswechsel) wäre geblieben. *Kosten:* sollte künftig ein anderer Weg `filterKey` mitten in einer Abfrage
   verschieben, hält wieder nur der Kommentar am Effekt.

## Akzeptanzkriterien

1. Eine spät ankommende Kinderliste schaltet den Assistenten nicht mehr vom eingetippten neuen Kind weg.
2. Ohne Eingabe bleibt die Vorwahl „bestehendes Kind" erhalten.
3. Eine E2E-Spec hält 1 mit angehaltener Kinderliste fest.

## Schätzung

- **groesse:** S · **wo:** frontend · **migration:** nein · **vertragsbruch:** nein.
- **Testweg:** `e2e/assistent-kinderliste-kommt-spaet.spec.ts`, `assistent.spec.ts` unverändert grün, Rollengang Assistent.

## Verlauf

- 2026-08-14 · Aufgenommen aus dem Review von B-169 im Nachtlauf. Der Reviewer hat ihn selbst als praktisch
  unerreichbar eingeordnet; er steht hier, weil die Annahme an keinem Tor hängt und beim nächsten Umbau des
  Assistenten still brechen kann. Bleibt `unverifiziert`, bis jemand versucht hat, den Fall herzustellen.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): der beschriebene Zustand besteht (`VaterWizard.tsx:259-269` hängt an `[exercises.data]`, liest `filterKey` `:175`), Herstellbarkeit weiter unversucht. Mess-Story: ein Nachtlauf versucht AK 1; gelingt es nicht, `verworfen` mit Messung.
- **2026-10-23** — vierter Nachtlauf, Sprint 9 (NP-12, [Protokoll](../pm-sitzung-2026-10-23-nacht.md)): **Messung** mit einer Wegwerf-Spec (nicht eingecheckt): ein Kind mit Klasse 5 sortiert zuerst; `GET /supervisor/children` angehalten, der Vater tippt ein neues Kind (ohne Pille) und kommt bis Schritt 3; dann Kinderliste frei, jede weitere Katalogantwort 2,5 s verzögert, per `requestAnimationFrame` mitgeschnitten: **521 ms** „env:enabled · 6 passende Übungen", **547 ms** „env:disabled · Suche läuft…", **3096 ms** „3 passende Übungen". Kein Frame mit freigegebenen veralteten Zeilen – aber die Trefferzahl 6 → 3 zeigt, dass der Assistent still auf das bestehende Kind (Klasse 5) umgeschaltet hatte. `frontend-reviewer` bestätigt den Moduswechsel als Defekt und nennt das verbleibende Macrotask-Fenster der Gleich-Batch-Variante. Gebaut nach Entscheidung 1. Rote Probe (Auflage 5): neue Spec ohne den Fix – erwartet rot am Namensfeld, gemessen **rot an `:44`** (`Expected: "Spät-Neu"`); mit Fix **1 passed**. Delta-Review (`frontend-reviewer`): die erste Fassung der Spec hätte ohne Fix auch zufällig grün werden können (`waitForResponse` wartet nicht auf React) → Wartezeile auf die freigeschaltete Pille ergänzt (gezählt), Probe danach erneut rot an `:44`. Playwright voll **82 passed**. Rollengang im Browser: ohne Eingabe wählt der Assistent weiter „Bestehendes Kind / Sohn" vor; Durchlauf bis „Feinschliff". Abgenommen; Commit: `90387d5`.
