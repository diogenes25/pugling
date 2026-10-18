---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [NewSeries stummer Pfad, Reihe anlegen ohne Namen]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 2026-09-05)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-212]
wartet_auf: ""
nachgeschaut: "2026-10-18: drei Sperren trimmen (VaterLehrwerke :966/:989, PublisherRow :139); E2E-Zeilen 24/28/31/34 gegen den Ausgangszustand geprueft; Entscheidung 2 falsch begruendet (Enter feuert bei gesperrtem Submit nicht) -> berichtigt, Kommentar :975 nachgezogen; zwei Sperrmuster auf einer Seite -> B-233 (Frage); Zeilenangabe der roten Probe veraltet (22 -> 24)"
---

# B-225 · „Reihe anlegen“ ist bei leerem Namen klickbar und schweigt

Der Kommentar, den Sprint 2026-09-05 unter das Anlege-Formular der Reihe schrieb, behauptete: „dieses
Formular hat keine Eingabeprüfung, es sperrt den Knopf stattdessen." Gesperrt ist aber nur der
Verlags-Knopf. Der stumme Pfad ist älter als der Sprint; der Kommentar war neu – und falsch.

## User Story

Als **Creator** möchte ich, dass „Reihe anlegen" bei leerem Namen entweder gesperrt ist oder mir sagt, was
fehlt – statt dass der Klick nichts tut.

## Ist-Stand am Code

Nachgesehen am 2026-10-17 (Nachschau, Reviewer-Agent; von mir gegengelesen).

- `frontend/src/vater/VaterLehrwerke.tsx:1023`: `<button type="submit" … disabled={action.busy}>` – nur die
  Sperre während der Aktion.
- `:951` (`NewSeries.submit`): bei leerem Namen `return` ohne `fail(...)` – kein Banner, und seit B-212 auch
  kein lokaler Meldungsplatz mehr, der etwas sagen könnte.
- `:1045`: der Verlags-Knopf trägt `disabled={… || !form.newPublisher.trim()}` – das Muster, das der
  Kommentar für beide Knöpfe behauptete.
- Der Kommentar (`:1031-1034`) ist in diesem Nachtlauf wahrheitsgemäß nachgezogen und verweist hierher.

## Die echte Lücke

Ein Klick ohne Wirkung und ohne Wort. Die Reparatur ist eine Bedingung am Knopf
(`disabled={action.busy || !form.name.trim()}`) **oder** ein `action.fail("Der Name der Reihe fehlt.")` wie
in `SeriesForm` `:490` – eines von beiden, konsistent mit dem Verlags-Knopf daneben.

## Offene Punkte

1. ~~Sperren (wie der Verlags-Knopf) oder melden (wie `SeriesForm`)?~~ → Entscheidung 1.

## Entscheidungen

Autonom gegrillt am 2026-10-17 unter Freigabe 1 (`art: Defekt`), Nachtlauf Sprint 2.

1. **Sperren, wie der Verlags-Knopf daneben:** `disabled={action.busy || !form.name.trim()}`. *Begründung:*
   derselbe Baustein, dasselbe Muster; eine Meldung „Der Name fehlt." wäre die zweite Form im selben
   Formular. *Kosten:* Der gesperrte Knopf muss seinen Grund nennen (B-160) – das Feld „Reihe" trägt darum
   „(Pflicht)" als Text, nicht als `title` am Knopf, der nie erscheint.
2. **Der `return` in `submit` bleibt** als zweite Verteidigung gegen programmatische Absendungen. *Kosten:*
   keine. *(Berichtigt in der Nachschau 2026-10-18: Die erste Fassung nannte „Enter im Feld bei leerem Namen" –
   die implizite Formularabsendung feuert aber nicht, wenn der Default-Submit-Knopf `disabled` ist; per
   Tastatur ist `submit` bei leerem Namen gar nicht erreichbar. Der Code ist unverändert richtig, die
   Begründung war es nicht.)*

## Akzeptanzkriterien

1. Bei leerem oder nur aus Leerzeichen bestehendem Namen ist „Reihe anlegen" gesperrt; mit Namen bedienbar.
   *Beleg:* E2E `reihe-anlegen-braucht-namen.spec.ts` (rot vor dem Fix an der ersten Sperr-Zusicherung).
2. Am Feld steht der Grund „(Pflicht)". *Beleg:* dieselbe E2E.
3. Der Kommentar unter dem Formular sagt, was der Code tut. *Beleg:* Lesebeleg.

## Schätzung

**Größe: XS** – eine Bedingung, ein Label-Zusatz, ein Kommentar, eine kurze E2E. Anker: B-02.
`wo: frontend`, `migration: nein`, `vertragsbruch: nein`.

**Testweg:** `playwright test e2e/reihe-anlegen-braucht-namen.spec.ts`; `bestaetigung-ueberlebt.spec.ts` und
`ein-meldungsplatz.spec.ts` füllen den Namen vor dem Klick und bleiben grün.

## Verlauf

- **2026-10-17** — angelegt aus der **Nachschau** zu Sprint 2026-09-05 (Nachtlauf, Protokoll
  `docs/pm-sitzung-2026-10-17-nacht.md`), `entgangen_bei: B-212` (der Kommentar ging durch die Abnahme; der
  Pfad selbst ist älter). Direkt `ausformuliert`, Ist-Stand mit `Datei:Zeile` belegt.
- **2026-10-17** — **ausformuliert → gegrillt → geschaetzt** im Nachtlauf, Sprint 2, autonom unter Freigabe 1 (`art: Defekt`); Entscheidungen, Kriterien und Schätzung oben.
- **2026-10-17** — **geschaetzt → abgenommen** im Nachtlauf, Sprint 2. Rollengang: „Reihe anlegen" gesperrt bei leerem und bei nur-Leerzeichen-Namen, „(Pflicht)" am Feld, bedienbar mit Namen, Anlegen gelingt. Rote Probe: E2E `reihe-anlegen-braucht-namen.spec.ts` 1/1 an Zeile 22 (`toBeDisabled` → `enabled`). Belege: `tsc -b` sauber · `vitest run` 381/381 · die neun Specs der Lehrwerke-Seite samt der zwei neuen 14/14 · volle Playwright-Suite **72/72** · `frontend-reviewer` gelaufen, **kein Blocker, kein Fund mit Code-/Testwirkung**, drei Kommentar-Hinweise eingearbeitet (Testkommentar zur tragenden Zeile, Vorbedingung beschriftet, zwei überzogene Formulierungen) · **Browser-Rollengang in Chrome (Freigabe 6)** als Creator auf `/vater/lehrwerke`, siehe Protokoll `docs/pm-sitzung-2026-10-17-nacht.md` Runde 2. Commit `dc2ce84`.
- **2026-10-18** — Nachschau (zweiter Lauf der Nacht, Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`,
  „Retrospektive Sprint 3"): drei Sperren trimmen; die E2E-Zeilen tragen (24 vor dem Fix `enabled`, 28 gegen
  einen Fix ohne `trim()`, 34 Text existierte nicht; 23 richtig als Vorbedingung beschriftet). **Berichtigt:**
  Entscheidung 2 begründete den `return` mit „Enter im Feld" – bei gesperrtem Default-Submit-Knopf feuert die
  implizite Absendung nicht, der `return` schützt nur programmatische Absendungen (Kommentar in
  `VaterLehrwerke.tsx` nachgezogen); Zeilenangabe der roten Probe 22 → 24. **Offengelegt:** Die Seite trägt
  zwei Sperrmuster (`disabled` bei leerem Namen hier, `aria-disabled` für fachliche Sperren in `PublisherAdmin`)
  und zwei Leer-Eingabe-Muster (`SeriesForm`/`UnitForm` melden lokal statt zu sperren) – die Sprint-Behauptung
  „genau EINE Rückmeldung an EINEM Platz" galt darum nicht für jede Handlung →
  [B-233](B-233-zwei-sperrmuster-auf-der-lehrwerke-seite.md) (`Frage`).
