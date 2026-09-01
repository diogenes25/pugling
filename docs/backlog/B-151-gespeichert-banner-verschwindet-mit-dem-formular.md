---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Gespeichert-Banner unsichtbar, onDone schließt das Formular, StatusBanner im geschlossenen Formular]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Fund beim Bauen von B-148 (2026-08-11)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: 2026-09-02
wartet_auf: ""
---

# B-151 · „Gespeichert." ist im Lehrbuch- und Fachlehrer-Formular nie zu sehen

Beide Bearbeiten-Formulare setzen ihre Erfolgsmeldung und schließen sich im selben Atemzug: `onDone`
ruft `setEditing(null)`, das Formular wird ausgehängt — und mit ihm der `StatusBanner`, der die Meldung
trägt. Der Nutzer sieht die Bestätigung nie, nur die geänderte Zeile.

## User Story

Als **Vater** möchte ich nach dem Speichern eine Bestätigung sehen, die lange genug steht, um sie zu lesen,
damit ich nicht raten muss, ob mein Klick angekommen ist.

## Ist-Stand am Code (am 2026-09-01 selbst nachgeprüft)

Die Kette ist an beiden Stellen dieselbe und lückenlos belegt:

| Schritt | Lehrbuch | Fachlehrer |
|---|---|---|
| Erfolgsmeldung gesetzt | `ChildMaterialSection.tsx:187` (`"Gespeichert."`) | `VaterFachlehrer.tsx:254` (`"Gespeichert."`) |
| direkt danach `onDone()` | `:188` | `:255` |
| was `onDone` tut | `:92` — `setEditing(null); books.reload()` | `:100` — `setEditing(null); list.reload()` |
| woran das Formular hängt | `:87` — `{editing === b.id && …}` | `:95` — `{editing === p.id && …}` |
| wo der Banner sitzt | `:302` — **im** Formular | `:426` — **im** Formular |

Gegenbeispiel in derselben Codebasis: `VaterLehrwerke.tsx` hält den Banner der Zeile **außerhalb** des
Formulars (`:269`) und lässt das Formular offen.

### Was dabei genauer ist als die Aufnahme

Drei Wege setzen eine Meldung, aber nur **einer** verliert sie — das ist beim Bauen wichtig, damit die
Reparatur nicht Wege anfasst, die heute richtig sind:

- **Speichern mit Änderung** → Meldung gesetzt, Formular schließt: **verloren**. Das ist der Defekt.
- **Speichern ohne Änderung** → `action.succeed("Nichts geändert.")` (`:185` / `:252`) und **kein**
  `onDone()`: Das Formular bleibt stehen, die Meldung ist sichtbar. Funktioniert heute.
- **Anlegen** → `onDone` ist dort `books.reload` bzw. `list.reload` **ohne** `setEditing` (`:107` / `:120`),
  das Anlege-Formular ist dauerhaft montiert: Meldung sichtbar. Funktioniert heute.

### Die Falle, die eine naheliegende Reparatur auslösen würde

„Formular offen lassen" ist der Reflex — und beide Dateien warnen im Kommentar ausdrücklich davor:
`ChildMaterialSection.tsx:148-151` und `VaterFachlehrer.tsx:202-205` sagen, dass der `useRef`-Bezugspunkt
des PATCH-Diffs **nur deshalb** nicht aus der Antwort nachgezogen wird, weil `onDone` das Formular schließt.
Wer es offen stehen lässt, ohne beide Schnappschüsse nachzuziehen, macht aus einem unsichtbaren Banner ein
**falsches zweites Speichern** derselben Sitzung. Diese Story darf den Reflex also nicht bedienen.

## Die echte Lücke

Die Bestätigung lebt in dem Element, dessen Verschwinden sie bestätigt. Das ist dieselbe Wurzel wie in
[B-173](B-173-speichern-vernichtet-seine-eigene-bestaetigung.md) — dort verschwindet nicht das Formular,
sondern die ganze Zeile aus einer gefilterten Liste.

## Entscheidungen

1. **Die Bestätigung wandert auf Seitenebene, das Formular schließt weiter.** Das Formular meldet den Text
   nach oben (`onDone(message)`), die Seite hält ihn und rendert einen `StatusBanner` an ihrem eigenen Rand.
   *Begründung:* Es ist die einzige Fassung, die den Kommentar-Vorbehalt oben respektiert — der
   `useRef`-Bezugspunkt bleibt gültig, weil das Formular weiterhin schließt. Und es ist derselbe Schnitt,
   den B-173 für die Zeile braucht: **eine** Bewegung für beide Stories statt zweier Muster.
   *Kosten:* Ein Zustand mehr je Seite, und die Meldung steht weiter vom Ort ihrer Ursache entfernt als
   heute (die aber unsichtbar ist — ein schlechterer Zustand).
2. **Keine automatische Ausblendung, kein Timer.** *Begründung:* `StatusBanner` kennt heute keinen und
   bekäme mit einem Timer einen Effekt, den 20 andere Aufrufstellen mittragen müssten. Die nächste Meldung
   ersetzt die vorige — das genügt. *Kosten:* Die Bestätigung steht, bis etwas anderes passiert.
3. **Nur die zwei benannten Formulare.** *Begründung:* Der Fund ist an zwei Stellen belegt; „gibt es
   weitere?" ist eine Messung und keine Vermutung, und der Bereich hat mit
   [B-174](B-174-arten-liste-verschweigt-ihren-fehler.md) bereits die Sammel-Story für die *Lese*-Hälfte
   derselben Familie. *Kosten:* Eine dritte Stelle könnte existieren und bliebe unentdeckt — ausgesprochen,
   nicht kaschiert.

## Akzeptanzkriterien

1. Nach erfolgreichem Speichern im Lehrbuch-Formular ist „Gespeichert." sichtbar, nachdem das Formular
   geschlossen hat.
2. Dasselbe im Fachlehrer-Formular.
3. Die beiden heute funktionierenden Wege („Nichts geändert.", Anlegen) verhalten sich unverändert.
4. Der `useRef`-Bezugspunkt beider Formulare bleibt unangetastet — kein Formular bleibt nach dem Speichern
   offen.
5. Ein E2E deckt 1 und 2; die rote Probe nennt erwartete und gemessene Zahl.

## Schätzung

- **Größe: S.** Anker: zwei Seiten je ein `useState` und ein `StatusBanner`, zwei Formulare je eine
  geänderte `onDone`-Signatur, ein E2E-Spec mit zwei Fällen.
- **Migration: nein**, **Vertragsbruch: nein** — reine Oberfläche.
- **Testweg:** `frontend/e2e/` — neue Spec (zusammen mit B-173, siehe dort) oder Erweiterung von
  `kind-lehrbuch-fach.spec.ts`. **Ein E2E ist hier die richtige Ebene, nicht ein Komponententest:** Der
  Fall verlangt einen *erfolgreichen Server-Schreibvorgang*, und kein Test dieses Frontends mockt
  `../lib/api` (`CatalogAdmin.test.tsx:6-9`). Der E2E fährt echten Browser gegen echten Server und ist
  damit zugleich der **Rollengang** (`pm-loop` Step 6).

## Verlauf

- **2026-08-11** — angelegt aus dem Bau von B-148. Belegstellen: `ChildMaterialSection.tsx` (`onDone`
  schließt beim Speichern), `VaterFachlehrer.tsx` (dieselbe Zeile), Gegenbeispiel
  `VaterLehrwerke.tsx:263-266`.
- 2026-09-01 · **`idee` → `ausformuliert`** (Nachtlauf). Beide Ketten Zeile für Zeile nachgeprüft.
  **Zwei Präzisierungen gegenüber der Aufnahme:** (a) Von den drei Wegen, die eine Meldung setzen,
  verliert nur *einer* sie — „Nichts geändert." und das Anlegen funktionieren heute; (b) die naheliegende
  Reparatur „Formular offen lassen" ist durch zwei Code-Kommentare ausdrücklich **verboten**, weil der
  `useRef`-Bezugspunkt des PATCH-Diffs daran hängt. `unverifiziert` entfernt.
- 2026-09-01 · **`ausformuliert` → `gegrillt`** (Nachtlauf, Freigabe 1 — `art: Defekt`). Drei
  Entscheidungen mit Begründung und Kosten. Entscheidung 1 ist bewusst dieselbe Bewegung wie in B-173 —
  die beiden Stories teilen ihre Wurzel und sollen nicht zwei Muster hinterlassen.
- 2026-09-01 · **`gegrillt` → `geschaetzt`** (Nachtlauf). S, Testweg E2E mit ausdrücklicher Begründung,
  warum ein Komponententest hier nicht trägt.
- 2026-09-01 · **`geschaetzt` → `in-arbeit`** (Nachtlauf, Sprint 1, Commit `e479d83`). Beide Formulare
  reichen die Bestätigung über `onDone(message)` an einen Meldungsplatz der Seite; das Formular schließt
  weiter, der `useRef`-Bezugspunkt bleibt damit gültig (Akzeptanzkriterium 4). Der Platz wird beim Öffnen
  eines Formulars geleert, damit keine alte Bestätigung über einer neuen Bearbeitung steht.
  **Rote Probe: erwartet rot, gemessen rot — aber erst nach einer Korrektur an der Zusicherung selbst.**
  Die erste Fassung prüfte „Gespeichert." *vor* dem Verschwinden des Formulars; `toBeVisible()` pollt und
  traf das Banner im Frame, in dem es noch stand — sie hätte **vor wie nach** der Reparatur gehalten.
  Erst mit umgedrehter Reihenfolge (erst auf `toHaveCount(0)` warten, dann die Meldung fordern) fiel sie.
- 2026-09-01 · **`in-arbeit` → `abgenommen`** (Nachtlauf, Sprint 1). Belege:
  - **Reviewer:** `frontend-reviewer` in **zwei** Runden. Runde 1 setzte einen berechtigten Blocker
    (Meldungsführung), Runde 2 meldete **keinen Blocker mehr**; ihre drei kleineren Funde sind in
    `6688ff5` eingearbeitet.
  - **Rollengang:** maschinell geführt — voller Playwright-Lauf **42/42 grün**, echter Chromium gegen
    echten Server mit Wegwerf-DB. Freigabe 6 greift nicht (keine Browser-Verbindung in dieser Sitzung);
    der E2E-Weg ist der dokumentierte **stärkere** Beleg, kein Ersatz.
  - **Zahlen:** `npx tsc -b` sauber · `npm run test` **326/326** in 38 Dateien.
  - **Commits:** `e479d83` (Bau) und `6688ff5` (Nachprüfung des Reviewers).
  - **Was ein Mensch noch ansehen muss:** nur bei B-151/B-173 — siehe dort.
  - **Offen für ein menschliches Auge** (dritter Ausgang, `pm-loop` Step 6): Ob der Meldungsplatz der
    Seite dort liegt, wo der Nutzer hinsieht, kann kein Test beurteilen. Benannter Check:
    `/vater/kind/:id` mit mehreren Büchern öffnen, ein mittleres bearbeiten und speichern.
