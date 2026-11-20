---
tags: [typ/protokoll, bereich/backlog, bereich/shop, bereich/dashboard, rolle/supervisor]
---

# PM-Sitzung: Vater-Sicht und Anmeldung (Sprint 97)

**Datum:** 2026-11-20  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Der Nutzer gibt das Thema vor: „Vater-Sicht und Anmeldung“.

## Runde 1 — Ausgangslage

Das Thema kommt vom Nutzer. Die Stories dazu stammen aus dem echten Betrieb und liegen schon auf `geschaetzt`:

- **Vater (O-Ton, 2026-11-18, aus [B-663](backlog/B-663-profilwahl-vater-oder-creator.md)):** Er will beim Anmelden
  „Vater“ oder „Creator“ wählen. Der Vater-Teil (Fortschritt, Shop/Einlösen) muss am Handy gehen. Erstellen ist dort
  „nicht handhabbar“ und bleibt Web.
- **Vater (aus [B-662](backlog/B-662-vater-sicht-arbeit-tagesziel-kaeufe.md)):** Die Startseite soll zeigen, was ihn
  interessiert: Arbeit, Tagesziel, Käufe. Im Vorbereitungslauf wurde das geteilt in B-747 (Sammel-Endpunkt), B-770
  (Antwortzahl) und B-749 (Startseite).
- **Sohn:** Kein Pfad in diesem Sprint. Er ist Zeuge, dass sich Einlösen und Kaufen nicht ändern.
- **Creator:** Er ist Zeuge für die Werkstatt am Rechner. Am Handy entfällt sie bewusst.

Kein neuer Befund unter `docs/anmerkungen/`. Die parallel vom Nutzer angelegte
[B-748](backlog/B-748-vater-morgenuebersicht-tagesmission-lernstand-klausurchance.md) (`idee`, P1) berührt dasselbe
Thema. Sie ist nicht `geschaetzt` und damit nicht im Sprint. Einen Teil davon liefert dieser Sprint: „Tagesmission
geschafft?“ und „wie viel?“.

## Sprint 97 — Ziel & Umfang

**Sprint-Ziel:** Der Vater sieht nach dem Anmelden am Handy auf einen Blick, ob jedes Kind heute gelernt und sein
Tagesziel erreicht hat. Offene Einlöse-Wünsche gibt er frei, ohne ein Kind zu wählen. Am Rechner wählt er beim
Anmelden, ob er als Vater oder als Creator arbeitet.

**Umfang:** [B-747](backlog/B-747-einloesungen-aller-kinder-endpunkt.md) (S, backend) ·
[B-770](backlog/B-770-antwortzahl-je-kind-und-tag.md) (S, backend) ·
[B-663](backlog/B-663-profilwahl-vater-oder-creator.md) (M, frontend) ·
[B-749](backlog/B-749-vater-startseite-entruempeln-und-einloesen.md) (L, frontend).

**Entwickler-Brief:**

- **Reihenfolge:** API-First, also B-747 und B-770 zuerst, danach B-663 vor B-749 (B-663 Entscheidung 5).
- **Quelle der Wahrheit:**
  - `ActivationRequests`, gefiltert nach Aussteller und Kind-Bindung.
  - `ReviewEvent` und `TestItemResult` je Tag.
  - `auth/me.roles` für den Wahlschritt.
- **Guards:**
  - Eigentum ohne `ChildOwnershipFilter`. Die Bindung läuft über `SupervisorLinks`.
  - Das Token bleibt unverändert (B-663 Entscheidung 1).
  - Keine Migration.
- **Senke:** Freigeben und Ablehnen gehen durch eine Stelle, `src/vater/einloesungen.ts`. Shop-Seite, Startseite und der
  Zähler an der Navigation lesen alle von dort.
- **Testweg:** Integrationstests, Vitest, eine E2E-Probe bei 390 px und der volle Playwright-Lauf auf Ausweichports. Auf
  :5200 läuft die echte DB.

## Iteration 97 — umgesetzt

**Backend**

- `GET supervisor/shop/activations?status=&skip=&take=` (`ShopController.AllActivations`) mit `X-Total-Count`. Dazu
  kommt die Client-Methode `ListAllActivationsAsync`.
- `ChildDay.AnswersToday` (`ChildrenDashboardService.CountAnswersAsync`).
- **Abweichung von B-770, am Code begründet:** Die Story wollte `ItemReviewEvent` und `TestItemResult` zählen. Das
  zählt jede Vokabel-Testantwort doppelt und lässt jede Nicht-Vokabel-Übung fehlen. Gezählt wird deshalb so:
  - `ReviewEvent` für das Üben;
  - **beantwortete** `TestItemResult` (`GivenAnswer != null || WasCorrect`), ohne Versuche des Vaters;
  - der Start legt je Frage eine Zeile vorab an (Review-Fund).
- Abdeckungs-Wächter auf 296 Actions.

**Frontend**

- **Wahlschritt nach der PIN** (`VaterWahl`), nur für Konten mit beiden Rollen. Er ist mit der gemerkten Wahl
  vorbelegt und hat einen Ausgang „Abmelden“.
- **Breitenweiche:** `useNarrowViewport` abonniert `matchMedia` (unter 768 px). `perspectivesFor(role, narrow)` lässt
  dem Vater am Telefon nur das Betreuen. Wer direkt auf eine gesperrte Seite geht, landet auf der Startseite und sieht
  den Hinweis „nur am Rechner“.
- **Startseite** `/vater`: Karten je Kind mit „Geübt heute“, „Tagesziel n von m“ und „Punkte heute“. Darunter stehen
  die offenen Einlöse-Wünsche aller Kinder mit Freigeben und Ablehnen, darunter die Zeitachse.
- **Verwaltung** `/vater/kinder`: Tabelle, „Kind anlegen“ und die Pläne-Karte. Dazu kommt ein Zähler am „Shop“.
- **Eine Senke** für Freigeben und Ablehnen: `einloesungen.ts`.
- **E2E:** 15 Login-Stellen gehen jetzt über den Helfer `alsVaterStarten`. Neue Specs: `vater-schmal.spec.ts` und
  `vater-handy.spec.ts`.

**Verifikation**

- **Backend:** `dotnet test Pugling.sln -c Release` lief mit 1926/1926 grün und Exit-Code 0.
- **Vitest:** 717/717 in `src/vater` und `src/lib`. Ein Fokus-Fall flackerte einmal im vollen Lauf und war einzeln grün
  (B-603).
- **Mutanten:** 14/14 im Backend und 9/9 im Frontend werden rot, jeweils am benannten Test. Die Tabellen stehen in den
  Stories.
- **Playwright, Lauf 1:** 173 grün, 6 rot.
  - 3 davon (`full-flow`, `shop-verlauf`, `taschengeld-monatlich`) warten auf `.cel-title`. Diese Klasse hat die
    **fremde, ungecommittete LearningDad-Arbeit** in `Celebration.tsx` entfernt. Das ist nicht Teil dieses Sprints.
  - 2 lagen am Sprint (`ziel-vier-wochen`, `feldhilfe-lage` mit einer Verkleinerung auf 700 px). Beide sind repariert.
  - `uebersetzung-sprachwechsel` flackerte einmal.
  - Nachlauf der betroffenen Specs: 27/27 grün.
- **Playwright, Lauf 2** (nach den Review-Fixes, Rollengang): 176 grün, 3 rot wie oben (fremd).

## Review Sprint 97

**`pugling-reviewer`**

- **Blocker:** Der Abdeckungs-Wächter war auf 295 gepinnt. Behoben.
- **Echter Fehlzähler:** Unbeantwortete Testzeilen und Vater-Versuche zählten als Antworten. Behoben, mit Testfall und
  zwei Mutanten.
- **Bewusst offen und im `<summary>` benannt:**
  - eine Übungssitzung des Vaters (es gibt keine Akteur-Spalte);
  - eine selbst eingeschätzte „nicht gewusst“-Testantwort;
  - das planunabhängige Bonus-Training.
- Die Client-Methode ist nachgezogen.

**`frontend-reviewer`**

- **Blocker:** „Abbrechen“ bei der Rückfrage meldete „freigegeben“. Behoben: Die Rückfrage steht jetzt vor
  `action.run`. Dazu kommt ein Vitest-Fall und das neue Tor (siehe Retro).
- **Blocker:** `ziel-vier-wochen.spec.ts` hing noch an der alten Startseite. Behoben.
- **Veraltete Verweise behoben:**
  - `VaterPlaene`: „Lege zuerst eines unter Betreuen an“;
  - Rücklink „← Alle Kinder“;
  - vier Banner „unter Kinder & Heute“;
  - `docs/medien-bilder.md` und `docs/tutorial-supervisor.md`.
- **Liste über 100 offene Wünsche:** Sie nennt jetzt den Rest.
- **Aufgeräumt:**
  - Der Wahlschritt steht per Layout-Effekt, bevor die Startseite zeichnet. So feuert sie keine verworfenen Abrufe.
  - Der Hinweis „nur am Rechner“ gilt nur, solange das Fenster schmal ist.

## Runde 2 — Rollengang und Abnahme

Der Rollengang ist der volle Playwright-Lauf gegen einen frisch gestarteten Server (Temp-DB, Ports 5310/5183) nach
dem letzten Edit. `vater-handy.spec.ts` ist der Weg des Vaters: Das Kind kauft und will einlösen. Der Vater meldet
sich bei 390 px an und sieht den Wunsch mit dem Kindnamen auf der Startseite und den Zähler am Shop. Er gibt frei.
Danach sinkt der Zähler um eins, und die Seite scrollt nie seitlich. `vater-schmal.spec.ts`: kein Wahlschritt, kein
Umschalter, die Werkstatt leitet zur Startseite mit Hinweis. Nach dem Verbreitern ist alles zurück.

**Lauf 2:** 176 grün und 3 rot, nach 24,6 Minuten. Die 3 roten sind wieder `full-flow`, `shop-verlauf` und
`taschengeld-monatlich`. Alle drei warten auf `.cel-title` (`COMBO ×5`, `GEKAUFT!`), und diese Klasse fehlt durch die
fremde Celebration-Änderung. Kein Pfad des Sprints ist darunter.

- **Vater: abgenommen.** Er meldet sich am Handy an, sieht je Kind „Geübt heute“, das Tagesziel und die Punkte und gibt
  frei, ohne ein Kind zu wählen. Am Rechner wählt er Vater oder Creator, und Enter nimmt die gemerkte Wahl.
  Offen bleibt das Gefühl am echten Handy: Ob die Karten lesbar sind, prüft der Vater selbst mit Friedrichs Daten, nach
  einem Heimnetz-Neustart auf dem neuen Stand.
- **Creator: Zeuge, ohne Befund.** `perspektiven`, `lehrer-konto`, `uebungstypen`, `lehrwerke` und
  `creator-lehrwerk-weg` sind grün. Die Werkstatt ist am Rechner unverändert erreichbar; unter 768 px fehlt sie bewusst.
- **Sohn: Zeuge.** Seine Specs sind grün, bis auf die drei `.cel-title`-Fälle aus fremder Arbeit. Kein Sohn-Pfad steht
  im Diff.

## Retrospektive

**Nachschau:** Sprint 96 ([B-645](backlog/B-645-taschengeld-und-auffuell-plan-fuer-alle-angebote.md),
[B-665](backlog/B-665-vater-loest-aus-dem-inventar-des-kindes-ein.md), `08fd05a6`) hat einen Durchrutscher:
[B-771](backlog/B-771-auffuellung-ohne-zeitpunkt-fuellt-sofort-auf.md) (P2, `entgangen_bei: [B-645]`). Ein
Kind-Bestand ohne Auffüll-Zeitpunkt füllt bei wiederkehrender Auffüllung sofort auf. Das passiert zum Beispiel, wenn
der Vater ein Angebot von „keine“ auf „monatlich“ umstellt. Dann ist Taschengeld zweimal im Monat möglich. Dazu kommt
der kosmetische [B-772](backlog/B-772-bestand-setzen-antwortet-mit-altem-geschwisterbestand.md). Beide Stories tragen
`nachgeschaut: 2026-11-20`. Der Hauptfund ist am Code nachgelesen (`ShopRefillSchedule.cs:24`, `:96`), nicht im Browser
gesehen.

- **Was die eigenen Tore durchließen:**
  - Eine Rückfrage *innerhalb* von `action.run` meldete nach „Abbrechen“ Erfolg. Kein Test sah es, weil jeder
    Vitest-Fall `confirm` auf `true` stellt und jede E2E-Spec den Dialog annimmt. Gefunden hat es der
    `frontend-reviewer`.
  - Die Story B-770 war auf eine Datenquelle geschätzt, die doppelt zählt. Das fiel nur beim Lesen des Codes vor dem
    Bau auf, nicht in der Schätzung.
- **Von außen kam:** nichts. Der Nutzer hat in diesem Sprint nichts beobachtet; alles kam aus Review und Nachschau.
- **Der eine Mechanismus (neues Tor):** `frontend/src/rueckfrage.test.ts` liest das Argument jedes
  `.run(`/`.runFor(` und meldet eine Rückfrage darin (`confirmAction`, `confirm`, `bestaetige…`). Gemessen wurde:
  - 0 Funde im Bestand;
  - rot mit der Fassung vor dem Review-Fix, und zwar an der Stelle `VaterDashboard.tsx:43`;
  - der Selbsttest deckt fünf Formen ab.
  - Die Regel steht als ein Satz in `frontend/CLAUDE.md`.
- **Bewusst ungedeckt:** Ein Helfer, der selbst fragt, aber nicht `bestaetige…` heißt, wird nicht erkannt.
- **Der Lauf hält hier**, weil die Retro ein neues Tor gebracht hat. Der Nutzer sieht es zuerst.

## Offene Roadmap (priorisiert)

Der Backlog liegt unter [docs/backlog/](backlog/INDEX.md). Diese Reihenfolge folgt aus dem Sprint:

1. **B-771 zuerst.** Das ist ein Geld-Defekt im echten Shop der Söhne (P2).
2. **B-748.** Die Morgenübersicht des Vaters baut auf diesem Sprint auf; Lernstand und Klausurchance fehlen noch.
3. **Prüfung durch den Nutzer:** die eigene Celebration-Arbeit gegen `full-flow`, `shop-verlauf` und
   `taschengeld-monatlich` laufen lassen (`.cel-title` fehlt dort).
