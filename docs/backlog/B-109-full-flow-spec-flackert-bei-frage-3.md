---
tags: [typ/story, status/idee, bereich/frontend, bereich/qualitaet]
aliases: [full-flow.spec.ts hängt, Gewusst-Knopf detached]
status: idee
prio: P3
art: Defekt
quelle: eigener Fund beim Bauen von B-62 (2026-08-05) — `git stash` gegen den unveränderten Stand
  bestätigte, dass derselbe Fehler schon vor B-62 bestand, also nicht durch B-62 verursacht
unverifiziert: true
---

# B-109 · `full-flow.spec.ts` hängt reproduzierbar bei „Frage 3/5" der Klausur

`frontend/e2e/full-flow.spec.ts` scheitert reproduzierbar (zweimal identisch beobachtet) im Klausur-Teil:
nach „Aufdecken 🔄" resolved der `Gewusst`-Locator zu einem `disabled`-Button, der dann aus dem DOM
entfernt wird, bevor er wieder anklickbar wird (`element was detached from the DOM, retrying`), bis der
Playwright-Timeout (60 s) greift. Beobachtet exakt bei „Frage 3 / 5" (die dritte `answerOne()`-Iteration
nach dem Wiedereintritt in die Klausur), beide Male mit identischem Fehlerbild.

**Nicht durch B-62 verursacht:** vor dem Bau von B-62 wurden alle betroffenen Dateien
(`SohnPractice.tsx`, `SohnTest.tsx`, `TestResult.tsx`, `full-flow.spec.ts` selbst) per `git stash`
auf den unveränderten Stand zurückgesetzt und der Test erneut gelaufen – derselbe Fehler an derselben
Stelle. Der Defekt ist vorbestehend.

Noch nicht untersucht: ob es ein reiner Timing-Flake ist (Busy-State/Re-Render-Fenster in
`SelfAssessAnswer`, `frontend/src/sohn/SohnTest.tsx:224-248`) oder ein echter Anwendungsfehler, der nur
unter der spezifischen Abfolge „Frage 1 beantworten → Klausur verlassen → wieder betreten → weiter
beantworten" auftritt (die einzige Stelle im Spec, die diese Abfolge testet). Ob der Fehler auch ohne
die Verlassen/Wiederbetreten-Sequenz reproduzierbar ist, ist offen.

## Der Schaden ist größer als die eine Spec (nachgetragen 2026-08-05)

Die Klausur-Sequenz liegt in **Zeile 80–122**, und alles, was danach kommt, läuft seither **nicht mehr**:

| Block | Zeile | Prüft |
| --- | --- | --- |
| Familien-Shop | 124–142 | Kauf und Kaufhistorie des Sohns (B-99) |
| Vater sieht Fortschritt | 143–152 | Tagesverlauf-Tabelle |
| Positions-Report | 153–167 | Vater-only-Sicht |
| Plan-übergreifender Lernstand | 168 ff. | Drilldown |

Damit ist der Ausfall nicht „ein Test rot", sondern **vier stillgelegte Prüfflächen**. Aufgefallen ist es
erst, als B-110 einen Testweg für den Kaufverlauf suchte und den vorhandenen Shop-Block nicht nutzen
konnte.

**Die Zustellung funktioniert dabei** — das ist ausdrücklich *nicht* die Lücke: `.github/workflows/e2e.yml`
fährt die Suite an jedem Pull Request und nachts um 03:00 UTC auf `main` und stellt ein Rot als Issue mit
Zustand zu (aus B-26). Nur `ci.yml` selbst fährt kein E2E, und das ist dort eine begründete Entscheidung.
Was die Meldung **nicht** sagt, ist der eigentliche Punkt: „E2E ist rot" nennt den gescheiterten Schritt,
nicht die vier Flächen, die dahinter aufgehört haben, geprüft zu werden. Der Umfang des Schadens ist aus
dem Signal nicht ablesbar.

Daraus folgt eine zweite, allgemeinere Frage für die Ausformulierung: **eine lange Spec ist ein einziger
Ausfallpunkt.** Ob der Vater→Sohn-Durchstich weiter als *eine* Datei laufen soll oder in Abschnitte
zerfällt, die einzeln scheitern können, ist zu entscheiden (Empfehlung: der Durchstich bleibt als *ein*
Weg erhalten — sein Wert ist ja die Kette —, aber neue Flächen bekommen eigene, kurze Specs, damit sie
nicht hinter fremden Schritten liegen; B-110 legt `e2e/shop-verlauf.spec.ts` schon so an).

## Verlauf

- **2026-08-05** — angelegt aus dem eigenen Fund beim Bauen von B-62; die Vorbestehen-Prüfung per
  `git stash` steht oben.
- **2026-08-05** — Schadensbild nachgetragen (bleibt `idee`, die Ursache ist weiter unbekannt): der
  Ausfall stellt vier weitere Prüfflächen derselben Spec still, darunter den Shop-Verlauf. Der Fund kam
  aus der Testweg-Suche für B-110, nicht aus einer neuen Messung an der Ursache selbst.
