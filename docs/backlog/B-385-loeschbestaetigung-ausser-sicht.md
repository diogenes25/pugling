---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Löschbestätigung außer Sicht]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-u
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-381]
---

# B-385 · Seit B-381 landet „Reihe … gelöscht." in einer langen Liste wieder außerhalb des Sichtbereichs

B-381 lässt eine Bestätigung nur noch scrollen, wenn ihr Absender `meldeVonWeitWeg` benutzt – heute nur `PublisherAdmin`.
`SeriesRow` bekommt `melde` (`VaterLehrwerke.tsx:220`). Damit scrollt auch die Bestätigung **nicht** mehr, deren Zeile beim
Speichern verschwindet: Löschen einer Reihe (`:284-285`), Umbenennen mit Umsortierung, Speichern, das die Zeile aus dem Filter
fallen lässt (`:474-475`). Bei dreißig Reihen steht „Reihe „X" gelöscht." unter der Tabelle außer Sicht, und ein geglücktes
Löschen sieht wieder aus wie nichts – genau der Fall, für den B-173 (AK 2) den Platz gebaut hat. B-381s Entscheidung wog nur
das Anlegen von Units; keine Spec prüft hier den Sichtbereich (die B-173-Specs nutzen `toBeVisible`).

Gefunden in der Nachschau zu Lauf U, Sprint 3 (frischer `frontend-reviewer`, `git show 2a9add3`, 🟡).

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau (Lauf U, Sprint 4). Entgleitung im Increment **dieses** Laufs: **Limit 2**, Lauf U
  endet nach Sprint 4.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es
  etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen"; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)):
  die Regel aus B-381 wird verfeinert – **eine Bestätigung holt den Platz, wenn ihre Zeile dabei verschwinden kann** (Löschen der
  Reihe, Speichern der Reihe in `SeriesForm`), dazu weiter das Verlags-Vokabular. Units anlegen, ändern und löschen bleiben
  ohne Scroll, weil ihr Formular stehen bleibt. Testweg: eine Spec „Reihe in langer Liste löschen" mit `toBeInViewport`, die
  B-381-Spec als Gegenprobe. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben
  Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidung selbst ausformulieren, schätzen und bauen; trifft er
  auf eine Produktfrage, die sie nicht hergibt, hält er an.
