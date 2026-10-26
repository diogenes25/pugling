---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: [Buch hinterlegt trifft zweimal, getByText ist Teilstring]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: eigener voller E2E-Lauf im Nachtlauf 2026-09-01
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-148]
nachgeschaut: 2026-10-26
wartet_auf: ""
---

# B-189 · Eine Teilstring-Zusicherung bricht, sobald der Leer-Text daneben stehen bleibt

## User Story

Als **Entwickler** möchte ich, dass eine E2E-Zusicherung genau das Element trifft, das sie meint, damit ein
roter Lauf eine Aussage über das Produkt ist und nicht über das Zeitverhalten des Servers.

## Ist-Stand am Code

`kind-lehrbuch-fach.spec.ts:51` sicherte `page.getByText("Buch hinterlegt.")` zu. `getByText` vergleicht
als **Teilstring**, und der Leer-Text der Bücherliste lautet „Noch kein **Buch hinterlegt.**"
(`ChildMaterialSection.tsx`). Nach dem Anlegen läuft `books.reload()`; solange es läuft, hält `useAsync`
die alte — leere — Seite, der Leer-Text steht also noch da. Trifft Playwrights Polling dieses Fenster,
sind es **zwei** Treffer und der Fall bricht mit `strict mode violation` ab.

**Gemessen am 2026-09-01, und die Messung ist der Kern dieser Story:** Der Fall ist allein grün (10,3 s)
und in einer Gruppe von drei Specs grün — rot wurde er nur im **vollen Lauf über 42 Fälle**, mit und ohne
die damals uncommitteten Änderungen des Sprints. Der Auslöser ist also die Last, nicht der Diff; die
Zweideutigkeit selbst liegt seit B-148 in der Datei.

## Die echte Lücke

Ein Teilstring-Vergleich, dessen Treffermenge vom **Zeitpunkt** abhängt. Er sagt nichts über das Produkt
aus und wird trotzdem als roter Lauf gelesen — dieselbe Familie wie die
[flackernde `bilder.spec.ts`](B-153-bilder-spec-flackert-im-vollen-lauf.md).

## Entscheidungen

1. **`{ exact: true }` statt einer umformulierten Meldung.** *Begründung:* Der Produkttext ist richtig;
   falsch war die Zusicherung. Einen Text zu ändern, damit ein Test ihn unterscheiden kann, hieße das
   Produkt nach dem Test zu formen. *Kosten:* keine; die Regel steht als Kommentar an der Zeile.
2. **Kein Wächter über alle `getByText`-Aufrufe.** *Begründung:* Teilstring ist meistens gewollt und
   trennscharf nicht von diesem Fall zu unterscheiden. *Kosten:* Eine zweite solche Stelle bliebe
   unentdeckt, bis sie im vollen Lauf bricht — ausgesprochen, nicht kaschiert.

## Akzeptanzkriterien

1. `kind-lehrbuch-fach.spec.ts` ist im vollen Suite-Lauf grün.
2. Die Zusicherung trifft nur noch den Banner, nicht den Leer-Text.

## Schätzung

- **Größe: XS** — eine Zeile plus Kommentar. **Migration/Vertragsbruch: nein.**
- **Testweg:** der volle Playwright-Lauf selbst.

## Verlauf

- 2026-09-01 · Gefunden im **eigenen vollen E2E-Lauf** des Nachtlaufs (Sprint 1), nicht von einem Agenten.
  Vor dem Zuschreiben gemessen statt vermutet: allein und in Dreiergruppe grün, mit **und ohne** die
  Sprint-Änderungen — der Diff dieses Sprints ist also nicht die Ursache. `entgangen_bei: [B-148]`, weil
  die Zweideutigkeit mit jener abgenommenen Story in die Datei kam.
- 2026-09-01 · Behoben (`{ exact: true }`) und **abgenommen**: voller Playwright-Lauf **42/42 grün**,
  `npm run test` 326/326, `tsc -b` sauber. Rollengang: dieser Fall IST ein Rollengang — er fährt echten
  Browser gegen echten Server. Reviewer: der Fund stammt aus dem Lauf selbst, der `frontend-reviewer` hat
  den Sprint gegengelesen, in dem er behoben wurde.
- **2026-10-26** — Nachschau (sechster Nachtlauf, Sprint 11, [Protokoll](../pm-sitzung-2026-10-26-nacht.md)): AK 2 nachgerechnet, `kind-lehrbuch-fach.spec.ts:55` (`exact: true`) gegen die zwei Texte `ChildMaterialSection.tsx:65` („Noch kein Buch hinterlegt.") und `:242` („Buch hinterlegt."). `StatusBanner.tsx:18-19` rendert `message.text` ohne Präfix in ein eigenes `div`, der exakte Vergleich trifft also genau den Banner und nie den Leer-Text; keine dritte Stelle im Frontend trägt den Satz. Kein Befund. Die rote Probe bleibt wie in Entscheidung 2 benannt lastabhängig (ohne `exact` rot nur, wenn das Polling das Reload-Fenster trifft).
