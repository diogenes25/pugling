---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/tests, bereich/qualitaet]
aliases: [Assistent ohne Durchstich, Wizard-E2E]
status: ausformuliert
prio: P2
art: Aufräumen
quelle: docs/testabdeckung-plan.md#e5-sperre-und-primitive-tests
---

# B-58 · Der Lehrplan-Assistent hat keinen Durchstich

Beim Bauen von [B-53](B-53-wizard-doppelklick.md) aufgefallen: `/vater/wizard` legt Kind, Plan und alle
Positionen in einem Zug an – **kein Playwright-Test fährt ihn zu Ende.** Direkt `ausformuliert`, nicht
`idee`: der Ist-Stand unten ist an den vier E2E-Dateien nachgesehen, nicht vermutet.

## User Story

Als **Entwickler**, der am Assistenten etwas ändert, möchte ich einen Test, der ihn **zu Ende** fährt und
die angelegte Position nachliest – damit ein vertauschtes Feld im Auftrag auffällt und nicht erst bei dem
Vater, der die App zum ersten Mal öffnet.

## Ist-Stand am Code

- `e2e/feldhilfe.spec.ts:96` öffnet `/vater/wizard`, klickt aber nur einen Feldhinweis auf; es wird nichts
  abgeschickt.
- `e2e/vater-von-null.spec.ts` richtet ein Szenario „von Null" ein – über den **manuellen** Weg
  (Dashboard → Neuer Plan → Positionen), nicht über den Assistenten.
- `e2e/lehrer-konto.spec.ts:36` prüft nur, dass „Assistent" in der Navigation **fehlt**.
- Damit hat der Abschluss (`VaterWizard.finish()` → `runWizardFinish`) genau eine Absicherung: die sieben
  Unit-Fälle in `src/vater/wizardFinish.test.ts` plus `tsc`. Die **Verdrahtung** des Bildschirms mit dem
  echten `api`-Objekt und dem Router prüft nichts.

## Die echte Lücke

Nicht „ein Test fehlt", sondern: der Weg **eines neuen Vaters** ist unbeobachtet. B-53 nennt ihn selbst „der
Einstiegsweg"; er schreibt in einem Klick mehr als jeder andere Bildschirm (Kind + Plan + n Positionen), und
sein Fehlschlag trifft jemanden, der die App zum ersten Mal benutzt und keinen Vergleich hat. Dass der
Doppelklick-Defekt dort bis 2026-08-01 unbemerkt lag, ist die Folge – nicht der Anlass.

Die Auslagerung nach `wizardFinish.ts` hat den Ablauf prüfbar gemacht, aber genau *seine* Naht nicht:
Was der Bildschirm aus fünf Schritten in den Auftrag schreibt, ist nur so richtig wie `tsc` das sehen kann –
und `tsc` sieht keine verwechselten Zahlenfelder (`pointsGoalMet` gegen `penaltyCoins`).

## Akzeptanzkriterien (Entwurf)

1. Ein Test geht `/vater/wizard` von „neues Kind" bis „Fertig" durch und landet auf der Plan-Seite.
2. Er liest an der angelegten Position **mindestens zwei** Feinschliff-Werte nach (Bestehensgrenze und
   Münz-Malus – die beiden, die sich beim Zusammenbauen des Auftrags am ähnlichsten sehen).
3. Er weist nach, dass **ein** Kind entstanden ist, nicht zwei (die Sperre aus
   [B-53](B-53-wizard-doppelklick.md) bekommt damit auch einen Beleg am echten Knopf).
4. Er läuft im vorhandenen Playwright-Job mit, ohne eigene Vorbereitung außer der Anmeldung.

## Offene Punkte

1. Ein neuer Spec oder ein Abschnitt in `vater-von-null.spec.ts`? Der zweite Weg wäre schneller (die
   Anmeldung steht schon), macht den langen Durchstich aber noch länger. **Empfehlung:** eigener Spec, damit
   ein Rot benennt, *welcher* Weg brach.
2. Reicht „ein Kind, ein Fach, eine Übung, Fertig → Plan-Seite erscheint"? Oder muss der Test die
   Feinschliff-Werte an der angelegten Position **nachlesen** (genau die Naht aus „Die echte Lücke")?
   **Empfehlung:** nachlesen – sonst prüft er die Verdrahtung nicht, die er prüfen soll.
3. Braucht er einen Fall für die Wiederaufnahme nach einem Fehler? Die deckt der Unit-Test ab; ein E2E dafür
   müsste den Server zum Scheitern bringen. **Empfehlung:** nein.

## Verlauf

- **2026-08-01** — angelegt beim Bauen von E5'/[B-53](B-53-wizard-doppelklick.md); Ist-Stand direkt an den
  vier E2E-Dateien belegt, die den Assistenten erwähnen.
