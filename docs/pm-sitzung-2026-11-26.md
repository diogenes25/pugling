# PM-Sitzung: Abnahme der Lerntage (B-731/B-732 und ihre Review-Defekte)

**Datum:** 2026-11-26  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die im Nachtlauf O gebauten und im Merge-Review nachgebesserten Lerntage (B-731), die Fehlerart (B-732) und
ihre drei Review-Defekte (B-1020…B-1022) gegen die laufende App an einer Kopie der echten DB abnehmen – oder benennen,
was fehlt.

## Runde 1 — Vorstellung & Feedback

Ausgangslage aus [pm-sitzung-2026-11-25-o](pm-sitzung-2026-11-25-o.md): fünf Stories `in-arbeit`, gebaut und committet
(`f4df2984`, `48550d7c`), offen jeweils nur Review über den Fix und Rollengang. Keine neue Anmerkung unter
`docs/anmerkungen/` (Export-Stand 2026-08-14, beide erledigt). Rollengang an der Kopie auf `:5201`.

Der Rollengang dieser Runde lief an der Kopie (Stand `144639e0`, Migrationen beim Start eingespielt) über die API; die
Oberfläche deckt die E2E `lerntage-und-pause` ab (Wegwerf-DB, echte UI). Konten: Vater = Konto 1, Sohn = Friedrich
(Kind 3), Lehrer = Konto 2, fremder Vater = Konto 3.

### Feedback Creator (O-Ton) — Regressionszeuge

Keine Creator-Fläche im Diff. „Für mich hat sich nichts bewegt – meine Übungen spielen wie vorher.“ Beleg: kein
`creator/*`-Pfad in `f4df2984`/`48550d7c`, Suite grün.

### Feedback Vater (O-Ton) — Gefällt / Stört-fehlt / Top-3

- Gefällt: „Ich trage ‚krank‘ für heute und morgen ein, und sofort droht Friedrichs Tag nichts mehr an – alle 46 Positionen
  `owesDuty: false`, auch die Wochenpflichten 167/168, die vorher `true` standen.“ Der Lehrer bekommt 403, der Demo-Vater
  404, „bis vor von“ ein klares 400.
- Stört: nichts Neues am eigenen Weg. Die Fragen aus Lauf O (B-1023 Hausaufgabe in der Pause) liegen weiter bei ihm.
- Top-3: (1) Lerntage abnehmen, (2) B-1023 entscheiden, (3) — keine weitere aus diesem Gang.

### Feedback Sohn (O-Ton) — Mega / Nervt / Top-3

- Mega: „Wenn Papa mich krankmeldet, steht da kein ‚−20 Münzen‘ mehr.“
- Beobachtet an der Kopie: zwei falsch getippte Antworten („eine Jahreszeitt“ statt „une saison“) stehen als `WrongWord`
  in `ItemReviewEvents` – die Fehlerart entsteht beim Antworten (B-732).
- Nervt (Fund des Reviews, nachgestellt): ein Plan, den er noch nie geöffnet hat, droht mit Malus, den der Server nie
  bucht (B-535: abgerechnet wird erst ab dem ersten Spiel im Plan). Offen als Frage B-1024.

## PM-Synthese & Priorisierung (→ Entwickler)

Zwei Reviewer (`pugling-reviewer` über `f4df2984`, `frontend-reviewer` über beide Commits) und der Gang ergeben:

| Prio | Fund | Größe | Wo | Entscheidung |
|---|---|---|---|---|
| P1 | `owesDuty` ohne das Erst-Spiel-Tor (B-535): nie gespielter Plan droht Malus an | XS | backend | gebaut, zurückgenommen → Frage B-1024 |
| P1 | Entscheidung 2 von B-1021 (Zählfenster ab dem ersten Pflichttag) ohne Test | XS | backend | Test ergänzt |
| P2 | `PlanDueForPeriod` in `owesDuty` ohne Test | XS | backend | Test ergänzt |
| P2 | Gruppe „Leitplanken“ heißt für Screenreader auch „Erklärung zu …“ (B-236) | XS | frontend | in B-1020 behoben |
| P2 | E2E-Banner-Probe hängt am Timing | XS | frontend | Wartepunkt „Leitplanken entfernen“ |
| — | toter Zweig `DutyUntil` in `OwesDutyOn` | — | backend | kommentiert (spiegelt die Abrechnung), nicht entfernt |
| — | Aufräum-Vorschläge (eslint-disable, feste id, doppelter Titel, E2E für `owesDuty:false`) | — | frontend | nicht blockierend, nicht gebaut |

**Roter Faden:** Die Arcade droht nur, was der Server bucht – jede Schranke der Abrechnung steht auch in `owesDuty`.

## Sprint 192 — Ziel & Umfang

**Sprint-Ziel (Sohnsicht):** An einem Tag, an dem mir nichts abgezogen werden kann – Sonntag, Pause, eine Woche, in der Papa
den Plan erst mittendrin eingeschaltet hat –, zeigt mir die App weder Frist noch Malus. Widerlegt, wenn
`GET student/children/{id}/today` in einem dieser Fälle `owesDuty: true` meldet. (Der nie geöffnete Plan stand zuerst im Ziel;
er ist eine Produktfrage, B-1024 – siehe Iteration.)

**Umfang:** B-1020, B-1021, B-1022 (Nacharbeit aus dem Review) und die Abnahme von B-731, B-732.

**Entwickler-Brief:** *Quelle der Wahrheit* `SettleClosedPeriodsAsync`; `OwesDutyOn` übernimmt ihre Schranken. *Guards*
unverändert. *Migration* keine. *Testweg* `LerntageTests` (+2), `LerntagePanels.test.tsx` (+1), E2E `lerntage-und-pause`, `pfad`.

## Iteration 192 — umgesetzt

- **Backend:** Test für Entscheidung 2 von B-1021 und für `PlanDueForPeriod` in `owesDuty`; `OwesDutyOn` benennt die eine
  bewusste Abweichung von der Abrechnung (Erst-Spiel-Tor), die Vertragsdoku ebenso. Das Tor selbst war gebaut (erst mit
  `FirstPlayDaysAsync` je Tag – rot in `SchreibwegMessungTests`, eine zweite Tagessicht-Abfrage –, dann aus der vorhandenen
  Abfrage) und wurde zurückgenommen, als `e2e/pfad.spec.ts` zeigte, dass jeder frische Plan dann ohne Frist erscheint: das
  entscheidet der Vater (B-1024), nicht der Lauf.
- **Frontend:** Gruppenname „Leitplanken“ ohne den ⓘ-Namen (B-236); E2E wartet auf „Leitplanken entfernen“ statt nur auf die
  `GET`-Antwort.
- **Mutanten** (Endstand, `.claude/scripts/mutant-probe.py`):

| Mutante | Stelle | erwartet | gemessen |
|---|---|---|---|
| M2 `PlanDueForPeriod` aus `OwesDutyOn` | `PositionProgressService.cs:455` | red | red |
| M3 Zählfenster nur am Einführungstag | `PositionProgressService.cs:883` | red | red |
| M4 Gruppen-`id` an der Zeile (von Hand, altes Markup) | `ChildCarePanels.tsx:357` | red | red |

  Die einzeilige Fassung von M4 (doppelte `id`) blieb grün – happy-dom löst die zweite auf; kein Beleg, darum von Hand mit
  dem vollständigen alten Markup gemessen.

**Verifikation am Endstand `4e70c583`:** Backend-Suite 2609/2609 (Release; ein Zwischenlauf unter Last hatte drei rote, die
einzeln grün liefen – `SchreibwegMessungTests.Messung_Verlauf`, `OpenApi_Schema…`, `Vertragsdokument…`), `vitest` 1514/1514,
`tsc -b` grün, E2E `lerntage-und-pause` + `pfad` 2 passed (Ports 5244/5284). `pugling-reviewer` über den Endstand: kein Fund.

## Runde 2 — Re-Review / Abnahme

Server der Kopie nach der letzten Änderung neu gestartet (`kopie-server.sh`, frische Kopie, `:5201`).

- **Vater — abgenommen.** „Pause für heute eingetragen: alle acht Pflichten meines Sohns drohen nichts mehr; Pause gelöscht:
  alles wieder scharf. Der Lehrer kommt nicht ran, der Demo-Vater sieht das Kind nicht.“ Die Maske (Pause + fünf Leitplanken)
  fährt die E2E `lerntage-und-pause` mit build + read-back. Offen bei ihm: B-1023 und neu B-1024 (beides Fragen).
- **Sohn — abgenommen.** „Krank heißt: keine Drohung.“ Seine getippten Fehler stehen mit Art in der Datenbank:
  `la rentree` → *Akzent* (27×), `savoi`/`quix` → *Tippfehler*, `eine Jahreszeitt` → *falsches Wort*; keine falsche getippte
  Antwort ohne Art. Gefühl und Klang nicht berührt.
- **Creator — Regressionszeuge.** Kein `creator/*`-Pfad im Diff, Suite grün; mehr hat er nicht getan.

Abgenommen: B-731, B-732, B-1020, B-1021, B-1022.

## Retrospektive

**Nachschau:** B-377, B-593, B-710, B-711, B-712 (Läufe M/N) – nachgesehen hat sie das Merge-Review vom 2026-11-26 und die
Funde direkt behoben (`61a11f55`, `5446640c`), ohne Defekt-Story. Nachgeholt: **B-1025** (`entgangen_bei: B-712`, „keine Lösung“
nur in einer Schreibweise) und **B-1026** (`entgangen_bei: B-593`, zu weiter Fang → 404, Vorlesename ohne sichtbaren Text);
`nachgeschaut: 2026-11-26` an allen fünf. B-624 war schon am 2026-11-25 nachgesehen.

**Was die eigenen Tore durchließen bzw. fingen:**

- Gefangen hat alles ein Tor, nichts kam von außen: das Erst-Spiel-Tor (Review-Vorschlag) machte zuerst
  `SchreibwegMessungTests` rot (zweite Tagessicht-Abfrage), in der zweiten Fassung `e2e/pfad.spec.ts` (jeder frische Plan ohne
  Frist). Die Schwäche lag vor den Toren: ein Review-Fund der Form „Verhalten ändern *oder* Ausnahme dokumentieren“ wurde als
  Reparatur gelesen, obwohl er sichtbares Verhalten des Sohns entschied.
- Werkzeug: eine einzeilige Mutante mit doppelter `id` blieb grün (happy-dom löst die zweite auf) – eine „grüne Mutante“ ist
  keine Entwarnung, das alte Markup musste vollständig zurück.

**Der eine Mechanismus:** Regel in `pm-loop` Step 5 (Prosa im treibenden Skill, keine resident geladene Datei): ein Fund
„ändern oder dokumentieren“, der rollen-sichtbares Verhalten betrifft, wird dokumentiert und als `Frage` abgelegt. Kein Tor –
das Urteil „sichtbar für eine Rolle“ ist nicht mechanisch zu fassen; die E2E bleibt das Netz dahinter. Kein neues Tor, also
kein Halt aus diesem Grund.

**Bewusst ungedeckt:** Ob eine nachgeholte Nachschau (Merge-Review statt Retro) ihre Funde als Story ablegt, prüft weiterhin
niemand; der Index zählt nur `nachgeschaut`.

## Offene Roadmap (priorisiert)  ·  Konkreter Änderungsstand (für Review)

Siehe `docs/backlog/`. Für den Vater liegen zwei Fragen: **B-1023** (Hausaufgabe in der Pause) und **B-1024** (Drohung vor dem
ersten Spiel). Danach ist B-644 die nächste Etappe (B-733 Tagesauswertung baut auf der jetzt gespeicherten Fehlerart auf).

Änderungsstand: `4e70c583` (Code), danach ein Doku-Commit (Backlog, Protokoll, `pm-loop`-Regel). Ungepusht.

**Nachtrag (2026-11-26):** Der Vater hat B-1024 entschieden – ein noch nie geöffneter Plan droht weiter Frist und Malus
an. B-1024 `verworfen` mit Grund; offen bleibt B-1023.
