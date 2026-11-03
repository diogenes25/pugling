---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: [kein Retry am Fehlerbanner, gescheitertes Lesen nur per F5, useAsync.reload ungenutzt]
status: abgenommen
prio: P3
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-172, Entscheidung 5
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-183 · Ein gescheitertes Lesen ist ohne F5 nicht zu wiederholen

Scheitert ein Abruf, zeigt die Oberfläche einen Satz — und lässt den Nutzer damit stehen. `useAsync` hält
mit `reload` längst den Weg zurück bereit (`frontend/src/lib/useAsync.ts:23`), aber **von 68 `banner err`
im Frontend bietet keines einen erneuten Versuch** (gezählt am 2026-09-01); die 75 `reload`-Aufrufe hängen
ausnahmslos an einer geglückten Mutation, nie an einem Fehler. Der Nutzer hilft sich mit F5 und verliert
dabei jede Eingabe, die im Formular daneben schon stand.

Aufgekommen in der Grill-Runde zu [B-172](B-172-fachauswahl-loescht-wenn-ihre-optionen-fehlen.md): Dort
wird ein Auswahlfeld während eines Ausfalls unbrauchbar, weil seine Optionen fehlen — und genau dann wäre
ein zweiter Versuch die einzige Handlung, die weiterhilft. B-172 baut ihn bewusst **nicht** ein: fünf
Banner mit Knopf und 68 ohne wären eine Inkonsistenz nach dem Muster von B-178 und B-126, und das Ziel von
B-172 (das Feld lügt nicht mehr) ist ohne ihn erfüllt.

Zu klären beim Ausformulieren ist vor allem, wie viele der 68 Stellen überhaupt ein eindeutiges `reload`
haben: mehrere Seiten führen drei und mehr `useAsync`-Instanzen, und welche zu welchem Banner gehört, ist
nicht überall aus dem Code abzulesen.

## User Story

Als **Vater** möchte ich bei jeder Liste, die nicht geladen werden konnte, einen Knopf „Erneut laden" haben, damit ich nicht
die ganze Seite neu laden und dabei meine Eingaben verlieren muss.

## Ist-Stand am Code (Lauf R, Stand `66ff94d`)

- **29 Stellen** im Vater-Web zeigen einen Lesefehler als `{x.error && <div className="banner err">…{x.error}</div>}`,
  alle einzeilig, alle an einem `useAsync`-Ergebnis (gezählt per Regex über `frontend/src`, keine mehrzeilige Form):
  `ChildCarePanels` 2, `ChildMaterialSection` 4, `ClozeTexts` 1, `ExerciseEditModal` 2, `ExerciseRatingPanel` 1,
  `ExerciseSharingPanels` 2, `InterestTagAdmin` 1, `PublisherAdmin` 1, `VaterFachlehrer` 3, `VaterKatalog` 1, `VaterKind` 2,
  `VaterLehrwerke` 4, `VaterLernstand` 2, `VaterMedia` 1, `VaterPlanDetail` 1, `VocabMediaPanel` 1.
- Die Zahl der Idee (68 `banner err`) zählt auch Mutationsfehler (`useAction`) und Formular-Hinweise. **Berichtigung nach dem
  Review:** die 29 sind nur die `&&`-Form; dazu kommen 21 Lesefehler in Ternär-Form hinter dem „Lade…"-Platzhalter, drei unter
  nacktem Namen und zwei in der Sohn-Arcade – zusammen in [B-369](B-369-lesefehler-in-ternaer-form.md) abgeteilt.
- Der Baustein `ListenAusfall` (`components/ListenAusfall.tsx`, B-188) steht an sieben Dateien; er merkt sich den Fehler über
  den Start der Wiederholung (`useGemerkterFehler`), kennt aber keinen `schluessel` – an einer Abfrage mit wechselnden
  Abhängigkeiten stünde nach einem Wechsel der alte Fehler da (B-227).
- „Welches `reload` gehört zu welchem Banner": an allen 29 Stellen eindeutig – jedes Banner liest genau ein `x.error`.

## Die echte Lücke

29 Banner ohne Weg zurück, und kein Wächter.

## Entscheidungen

1. **(Nutzer, 2026-10-23) `ListenAusfall` ist Hausregel** für jeden Lesefehler, wo das `reload` eindeutig ist.
2. **(Lauf R, Freigabe 9) Alle 29 Stellen in einem Zug**, je mit eigenem Satzanfang („Die Fächer", „Die Units", …); vorhandene
   Präfixe („Reihen:", „Fächer:") werden zum Satzanfang. *Kosten:* die Meldungen werden länger und alle `role="alert"`.
3. **(Lauf R) `ListenAusfall` bekommt einen optionalen `schluessel`**, durchgereicht an `useGemerkterFehler`; jede Stelle mit
   Abhängigkeiten übergibt sie (bei mehreren als `JSON.stringify([...])`). *Kosten:* der Aufrufer muss die Deps zweimal
   nennen; vergisst er den Schlüssel, steht nach einem Wechsel der alte Fehler (das prüft kein Wächter).
4. **(Lauf R) Ein Wächter `src/lesefehler.test.ts`** meldet jedes `{x.error && <… banner err>}`, mit Selbsttest je Form.
6. **(Lauf R, nach dem Review) Geteilt:** diese Story liefert die `&&`-Form; die Ternär-Form, die nackten Namen und die
   Arcade trägt [B-369](B-369-lesefehler-in-ternaer-form.md). Grund: dort steht der Platzhalter vor dem Ausfall, und „Erneut
   laden" tauschte den Kasten gegen „Lade…" (B-216) – eine Gestaltungsfrage, kein Ersetzen. *Kosten:* bis B-369 gilt die
   Hausregel nur zur Hälfte; Wächter, `CLAUDE.md` und AK 1 sagen das ausdrücklich.
5. **(Lauf R) Die Hausregel steht als zwei Zeilen in `frontend/CLAUDE.md`** – im selben Durchgang bezahlt (die Datei ist danach
   kleiner als vorher).

## Akzeptanzkriterien

1. Kein Lesefehler steht in der `&&`-Form als nacktes Banner da; ein neues macht den Wächter rot (die übrigen Formen: B-369).
2. Im echten Browser bringt „Erneut laden" eine ausgefallene Liste zurück, ohne die Seite neu zu laden.
3. Nach einem Wechsel der Abhängigkeiten zeigt `ListenAusfall` den alten Fehler nicht weiter.

## Schätzung

`M`, frontend (29 Stellen, ein Baustein-Feld, ein Wächter, eine Spec). Risiken: ein falscher Satzanfang; ein vergessener
Schlüssel. Testweg: `lesefehler.test.ts`, `ListenAusfall.test.tsx`, neue Spec `e2e/lesefehler-erneut-laden.spec.ts`,
E2E-Gesamtlauf.

## Verlauf

- 2026-09-01 · Aufgenommen aus der Grill-Runde zu B-172 (Entscheidung 5). Die beiden Zahlen (68 Banner
  ohne Retry, 75 `reload`-Aufrufe) sind an diesem Tag gezählt; welche Stelle *welches* `reload` bräuchte,
  ist nicht geprüft — darum `unverifiziert`.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht, **der Ist-Text ist überholt** – es gibt inzwischen Retry-Knöpfe: `ListenAusfall` (`components/ListenAusfall.tsx:36`, 5 Einsätze), `ExerciseEditModal.tsx:170`, `PlanPositions.tsx:549`, `VaterWizard.tsx:735`. Die in B-172 befürchtete Inkonsistenz (wenige Banner mit Knopf, die Mehrheit ohne) ist damit real. Neu gezählt: 81× `banner err`, 55× `.reload()`. Frage: wird `ListenAusfall` Hausregel für jeden Lesefehler?
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-2 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): **Ja – `ListenAusfall` wird Hausregel** für jeden Lesefehler: jeder Banner, der ein gescheitertes Lesen meldet, bietet einen erneuten Versuch, wo das zugehörige `reload` eindeutig ist. Beim Ausformulieren zu klären: die Stellen mit mehreren `useAsync`-Instanzen (welches `reload` gehört zu welchem Banner) und ob ein Wächter die Regel hält.
- **2026-10-23** — **Freigabe des Nutzers (Autonomer Modus, Opt-in je Vorhaben, README):** ein Nachtlauf darf diese Story auf Grundlage der getroffenen Entscheidung selbst ausformulieren, schätzen und bauen (Paket NP-15); trifft er dabei auf eine Produktfrage, die die Entscheidung nicht hergibt, hält er an und notiert sie. Rollengang und Reviewer bleiben Pflicht. Erteilt in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)).
- **2026-11-03** — Lauf R, Teil 2: auf der Freigabe des Nutzers (Freigabe 9) ausformuliert, geschätzt, gebaut. Rote Proben: der
  Wächter meldete **29** Stellen (erwartet 29), Selbsttests 7/7; `ListenAusfall` mit Schlüssel **1 failed** (erwartet 1; der
  Gegenfall war schon grün). Nach dem Umbau Wächter 0 Funde, Vitest **638/638**, `tsc -b` sauber (zwei Importe landeten
  zuerst in einer mehrzeiligen Import-Klammer, korrigiert). Neue Spec gegen die Mutanten: „alte Form (nacktes Banner)" **1
  failed** (`:21`), „Knopf ruft nicht `reload`" **1 failed** (`:28`).
- **2026-11-03** — Review `frontend-reviewer`: 🔴 AK 1 und die Hausregel waren zu weit formuliert – die Zählung sah nur die
  `&&`-Form; 21 Lesefehler in Ternär-Form, drei unter nacktem Namen und zwei in der Arcade blieben nackt. Selbst nachgezählt
  (Regex über `frontend/src`). Behoben durch **Teilen** (Entscheidung 6, [B-369](B-369-lesefehler-in-ternaer-form.md)) statt
  mechanischem Umbau, weil dort „Erneut laden" den Kasten gegen „Lade…" tauschte; AK 1, Wächter-Kommentar und `CLAUDE.md`
  eingeengt, Selbsttest-Fall als Absichtserklärung beschriftet (zählt). 🟡 `ExerciseEditModal` verlor `marginBottom: 6` →
  `ListenAusfall` nimmt `style` (zählt). 🟡 `VaterPlanDetail.tsx` nannte den Kasten noch „ohne Wiederhol-Knopf" → berichtigt
  (Kommentar). 🟢 Spec-Filter auf `/^Englisch$/`, Gegenfall in `ListenAusfall.test.tsx` mit „Rot, wenn", die gekürzte Zeile in
  `frontend/CLAUDE.md` trägt wieder „anonymes `POST supervisor/adults`, die Id ist der Login-Name" (ohne Zählung); die Datei ist
  danach 10 347 B gegen 10 365 B vorher. **Zähler Teil 2: 2.**
- **2026-11-03** — E2E-Gesamtlauf nach dem Review **rot**: 3 failed, 106 passed – drei Specs suchten den alten Präfix („Fächer:", „Kinder:") des Banners (`fachlehrer-faecher-fallen-aus`, `optionsliste-faellt-aus`, `plan-kinderliste-faellt-aus`). Auf den neuen Satz umgestellt (dieselbe Aussage: der Ausfall ist sichtbar). Dabei selbst gefunden: `lehrbuch-units-laden-noch.spec.ts:60` sicherte zu, dass **kein** „Units: …" erscheint – mit dem neuen Text immer wahr, also leer; auf den Satz des Bausteins umgestellt. Die vier Specs danach **4 passed** (zählt: Tests geändert). **Zähler Teil 2: 3.**
- **2026-11-03** — Abnahme Lauf R, Teil 2: Vitest **638/638**, `tsc -b` sauber, E2E-Gesamtlauf **109 passed** (`exit=0`, nach dem letzten Edit). Rollengang: Vater maschinell – `lesefehler-erneut-laden.spec.ts` schießt die Fächer im Katalog ab und holt sie mit „Erneut laden" zurück, ohne Neuladen der Seite; die drei Ausfall-Specs (Fachlehrer, Optionsliste, Plan) sehen den neuen Satz im echten Browser. Creator (Katalog, Lehrwerke) über dieselben Seiten, Sohn Regressionszeuge (Arcade unberührt, B-369). Umfang ausdrücklich: die `&&`-Form; der Rest in B-369. **Abgenommen**, Commit siehe Protokoll ([Lauf R](../pm-sitzung-2026-11-03-nacht.md)).
