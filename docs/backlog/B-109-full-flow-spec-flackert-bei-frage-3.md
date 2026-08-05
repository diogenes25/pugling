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
