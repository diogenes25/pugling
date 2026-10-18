---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [PublisherAdmin StatusBanner, zweiter Meldungsplatz Lehrwerke-Seite]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 2026-09-05)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-212]
wartet_auf: ""
nachgeschaut: "2026-10-18: acht Server-Schreibpfade der Seite auf nachObenMelden gezaehlt (VaterLehrwerke :275/:449/:737/:754/:882/:902, PublisherAdmin :44/:58), verbleibende lokale StatusBanner nur Eingabepruefung (:540/:849); AK 3 falsche Praemisse (409 beim Umbenennen ausloesbar) -> E2E nachgereicht; Sichtbarkeit der Bestaetigung im Browser gemessen -> B-232 (Frage); Zeilenangabe der roten Probe veraltet (44 -> 46)"
---

# B-224 · Der Verlags-Baustein trägt auf der Lehrwerke-Seite einen zweiten Meldungsplatz

B-212 versprach in seiner User Story, dass auf der **Lehrwerke-Seite** nie gleichzeitig eine Fehler- und
eine Erfolgsmeldung stehen. Gebaut – und so im Ist-Stand und in `VaterLehrwerke.tsx:401` auch beschrieben –
wurde das für die **Datei**. Dieselbe Seite rendert aber `PublisherAdmin` mit eigenem Platz.

## User Story

Als **Creator** möchte ich auf der Lehrwerke-Seite auch nach einer Verlags-Aktion genau **eine**
Rückmeldung lesen, die zu dem gehört, was ich zuletzt getan habe – statt einer roten Meldung am Verlag
unter einer grünen Meldung an der Reihe.

## Ist-Stand am Code

Nachgesehen am 2026-10-17 (Nachschau, Reviewer-Agent; Zeilen von mir gegengelesen).

- `frontend/src/vater/PublisherAdmin.tsx:80`: `<StatusBanner message={action.message} />` – eigener Platz
  für Server-Erfolg („Gespeichert." `:56`, „Verlag gelöscht." `:67`) **und** Server-Fehler (409 bei fremd
  genutztem Verlag).
- `frontend/src/vater/VaterLehrwerke.tsx:230`: der Baustein wird mit `onChanged` eingehängt; `onChanged`
  räumt den Seitenplatz nicht, und kein Aufrufer kann den Platz in `PublisherAdmin` räumen – `nachObenMelden`
  ist dateiprivat in `VaterLehrwerke.tsx`.
- Weg zum Widerspruch: Verlags-Vokabular öffnen → Verlag löschen scheitert (rot dort) → Reihe löschen
  gelingt (grün im Seitenplatz). Oder zwei Grüns: „Reihe X gelöscht." oben, „Gespeichert." unten.
- Mildernd: `PublisherAdmin` ist standardmäßig zugeklappt (`:22`). Von keiner Spec gefahren; nicht im
  Ist-Stand von B-212, darum dort nicht gezählt (B-212 zählt „drei"/„vier" `action.message`-Banner – die
  Zahl ist in der Story selbst uneinheitlich).
- Verwandt, außerhalb des B-212-Diffs: `VaterLehrwerke.tsx:692` – „Bearbeiten" am `UnitPanel` räumt den
  Seitenplatz nicht, anders als `bearbeitenUmschalten` `:323` und `onToggle` `:209` (Regel aus B-197,
  „Formular öffnen räumt"). Kein Widerspruch, nur eine veraltete Bestätigung bleibt stehen.

## Die echte Lücke

Der Schnitt aus B-212 („alles vom Server reist nach oben") endet an der Dateigrenze, die Seite nicht.

## Offene Punkte

1. ~~`PublisherAdmin` ein `onMeldung` geben und `nachObenMelden` nach `src/lib/` ziehen – oder B-212
   ausdrücklich auf die Datei einschränken und diesen Baustein so lassen?~~ → Entscheidung 1.
2. ~~Den `UnitPanel`-„Bearbeiten" (`:692`) mitnehmen?~~ → Entscheidung 2.

## Entscheidungen

Autonom gegrillt am 2026-10-17 unter Freigabe 1 (`art: Defekt`), Nachtlauf Sprint 2.

1. **`PublisherAdmin` meldet nach oben.** Er bekommt `onMeldung` als Pflicht-Prop (dasselbe Argument wie
   `TextbookForm`, B-197: ein `?`-Prop lässt sich weglassen, und genau so entsteht die nächste Stelle ohne
   Räumen), seine beiden Server-Pfade (Umbenennen, Löschen) laufen über `nachObenMelden`, der eigene
   `StatusBanner` entfällt. `nachObenMelden` zieht dafür nach `src/lib/nachObenMelden.ts` – ein Import aus
   `VaterLehrwerke.tsx` in einen Baustein wäre die falsche Abhängigkeitsrichtung. *Begründung:* Die User
   Story von B-212 spricht von der Seite, und der Weg zum Widerspruch ist real. *Kosten:* Umzug einer
   Funktion samt Docblock, ein Prop mehr, der Lesefehler `publishers.error` bleibt lokal (wie bei jedem
   Lesefehler der Seite, B-172 Entscheidung 5).
2. **Öffnen räumt – auch hier.** Das Aufklappen des Vokabulars und der „Bearbeiten"-Knopf am `UnitPanel`
   (`:692`) rufen `onMeldung(null)`, wie `onToggle` (`:209`) und `bearbeitenUmschalten` (`:323`).
   *Begründung:* die B-197-Regel „Formular öffnen räumt" kennt sonst zwei Ausnahmen auf derselben Seite.
   *Kosten:* zwei Zeilen; für den `UnitPanel`-Weg kein eigener E2E-Fall – er ist eine Zeile Parität, und
   der Beleg ist die Codezeile selbst (ausdrücklich als Lesebeleg beschriftet, nicht als Test).

## Akzeptanzkriterien

1. Nach dem Umbenennen eines Verlags im Vokabular steht „Gespeichert." genau einmal auf der Seite – und
   nicht innerhalb des Vokabular-Abschnitts. *Beleg:* E2E `verlag-meldet-am-platz-der-seite.spec.ts`
   (rot vor dem Fix: die Meldung stand im Abschnitt).
2. Die nächste Handlung auf der Seite (Reihe anlegen) räumt diese Meldung. *Beleg:* dieselbe E2E (rot vor
   dem Fix: „Gespeichert." blieb stehen).
3. Ein Fehler einer Verlags-Aktion steht ebenfalls am Platz der Seite. *Beleg:* E2E
   `verlag-fehler-am-platz-der-seite.spec.ts` (Umbenennen eines eigenen Verlags auf „Klett" → 409
   `DuplicatePublisher`, Meldung am Seitenplatz, keine im Vokabular-Abschnitt). *(Berichtigt in der Nachschau
   2026-10-18: Die erste Fassung behauptete, der 409-Fall sei im Seed „gar nicht auslösbar" – das galt nur fürs
   **Löschen** (Sperre am Knopf). Beim Umbenennen sperrt der OK-Knopf nur bei leerem Namen, und
   `PublishersController.cs:154-163` liefert 409 bei vergebenem Namen. Der Lesebeleg ruhte auf einer falschen
   Prämisse; der rote Pfad des `onMeldung`-Vertrags war bis dahin ungetestet.)*
4. Aufklappen des Vokabulars und `UnitPanel`-„Bearbeiten" räumen den Platz. *Beleg:* Lesebeleg
   `onMeldung(null)` an beiden Stellen; kein Test (Parität zu `:209`/`:323`).
5. Die bestehenden Specs der Seite (`gesperrter-knopf-nennt-den-grund`, `bestaetigung-ueberlebt`,
   `ein-meldungsplatz`, `loeschen-meldet-sich`, `lehrwerke`) bleiben grün.

## Schätzung

**Größe: S** – Umzug einer Funktion, ein Prop, zwei Pfade umgestellt, zwei Räum-Zeilen, eine E2E. Anker: B-01.
`wo: frontend`, `migration: nein`, `vertragsbruch: nein`.

**Risiken:** `nachObenMelden` nutzt `errorMessage` aus `../lib/api` und die Typen aus `useAction` – beide
liegen schon in `src/lib/`, der Umzug zieht keine Abhängigkeit in die falsche Richtung.

**Angriffsplan:** (1) E2E rot messen, (2) `src/lib/nachObenMelden.ts`, Import in `VaterLehrwerke.tsx`,
(3) `PublisherAdmin` mit `onMeldung`, (4) Räum-Zeilen, (5) E2E grün, Suite.

**Testweg:** `playwright test e2e/verlag-meldet-am-platz-der-seite.spec.ts`, dann die Specs der Seite.

## Verlauf

- **2026-10-17** — angelegt aus der **Nachschau** zu Sprint 2026-09-05 (Nachtlauf, Protokoll
  `docs/pm-sitzung-2026-10-17-nacht.md`), `entgangen_bei: B-212`. Direkt `ausformuliert`: Der Ist-Stand ist
  aus der Nachschau mit `Datei:Zeile` belegt.
- **2026-10-17** — **ausformuliert → gegrillt → geschaetzt** im Nachtlauf, Sprint 2, autonom unter Freigabe 1 (`art: Defekt`); Entscheidungen, Kriterien und Schätzung oben.
- **2026-10-17** — **geschaetzt → abgenommen** im Nachtlauf, Sprint 2. Rollengang: Verlag angelegt (Bestätigung am Platz der Seite), Vokabular aufgeklappt (Platz geräumt), Verlag umbenannt → „Gespeichert." genau einmal, am Platz der Seite, keiner im Vokabular-Abschnitt; Reihe angelegt → „Gespeichert." weg, Katalog-Satz da. Rote Probe: E2E `verlag-meldet-am-platz-der-seite.spec.ts` 1/1 an Zeile 44 (im Vokabular erwartet 0, gemessen 1). Belege: `tsc -b` sauber · `vitest run` 381/381 · die neun Specs der Lehrwerke-Seite samt der zwei neuen 14/14 · volle Playwright-Suite **72/72** · `frontend-reviewer` gelaufen, **kein Blocker, kein Fund mit Code-/Testwirkung**, drei Kommentar-Hinweise eingearbeitet (Testkommentar zur tragenden Zeile, Vorbedingung beschriftet, zwei überzogene Formulierungen) · **Browser-Rollengang in Chrome (Freigabe 6)** als Creator auf `/vater/lehrwerke`, siehe Protokoll `docs/pm-sitzung-2026-10-17-nacht.md` Runde 2. Commit `dc2ce84`.
- **2026-10-18** — Nachschau (zweiter Lauf der Nacht, Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`,
  „Retrospektive Sprint 3"): acht Server-Schreibpfade der Seite melden nach oben, keine lokale Server-Meldung
  mehr. **Drei Berichtigungen:** AK 3 ruhte auf einer falschen Prämisse (409 beim Umbenennen auf „Klett"
  auslösbar) – der rote Pfad des `onMeldung`-Vertrags war ungetestet, E2E `verlag-fehler-am-platz-der-seite.spec.ts`
  nachgereicht; die Bestätigung einer Verlags-Aktion liegt bei langer Reihen-Tabelle außerhalb des Sichtfelds
  (im Browser gemessen: bei vier Reihen sichtbar, 505 px über dem Knopf) →
  [B-232](B-232-bestaetigung-ausserhalb-des-sichtfelds.md) als `Frage`, weil B-212 „Bestätigung darf unsichtbar
  sein" entschieden hat; Zeilenangabe der roten Probe 44 → 46 (zwei Reviewer-Kommentare kamen nach der
  Messung). Nuancen ohne Nutzerwirkung: „Schließen" des Vokabulars räumt nicht (B-197 verlangt nur Öffnen);
  `nachObenMelden` ohne eigenen Vitest.
