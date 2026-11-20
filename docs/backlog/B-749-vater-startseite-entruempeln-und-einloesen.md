---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/supervisor]
aliases: [Vater-Sicht, Vater-Startseite entrümpeln, Einlösen am Handy]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-662
grund: ""
ersetzt_durch: []
---

# B-749 · Die Vater-Startseite zeigt Arbeit, Tagesziel, Einlösen; die Verwaltung zieht um

Der Oberflächenteil von [B-662](B-662-vater-sicht-arbeit-tagesziel-kaeufe.md): `/vater` wird zur Vater-Sicht (geübt heute mit
Antwortzahl, Tagesziel, Punkte, offene Einlösungen aller Kinder mit Freigeben), Kind anlegen, Kinder-Tabelle und Pläne-Karte ziehen auf
eine Verwaltungsseite, an der Navigation steht ein Zähler offener Einlösungen, und eine Probe bei 390 px sichert das Handy. Baut auf
[B-663](B-663-profilwahl-vater-oder-creator.md) (zuerst), [B-747](B-747-einloesungen-aller-kinder-endpunkt.md) und
[B-770](B-770-antwortzahl-je-kind-und-tag.md) auf.

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-662.*

## User Story

Als *Vater* möchte ich auf meiner Startseite genau sehen, ob und wie meine Kinder heute gelernt haben, ob sie ihr Tagesziel erreicht
haben und was einzulösen ist, damit ich das am Handy in einem Blick erledige.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`; Pfade relativ zu `frontend/`.

- Die Startseite `/vater` ist `src/vater/VaterDashboard.tsx`: Karte „Heute" (`:57-81`, Überschrift `:58`), Zeitachse
  (`ChildrenTimeline`, `:82`), Karte „Kinder" mit Tabelle, Anlegeformular und Lehrpläne-Karte (`:84-150`, Überschrift `:85`).
- Die Navigation „Betreuen" führt „🏠 Kinder & Heute" auf `/vater` (`src/vater/navigation.ts:62`).
- Offene Einlösungen: nur im Shop-Bereich je Kind (`src/vater/VaterShop.tsx:500-552`, `api.childActivations`
  `src/lib/api.ts:901`); eine Liste über alle Kinder und ein Zähler: nicht gefunden.
- **15 E2E-Specs warten nach der Anmeldung auf die Überschrift „Kinder"** (`getByRole("heading", { name: "Kinder" })`, Beispiel
  `e2e/perspektiven.spec.ts:24`, Zählung per Grep über `e2e/*.ts`), und Specs wie `kind-anlegen-id.spec.ts` und
  `kind-auswahlliste.spec.ts` gehen über die Kinder-Tabelle. Jeder Umbau der Startseite berührt sie.
- Probe bei Telefonbreite: nur 768 px (`e2e/responsive-vater.spec.ts:19`); 390 px: nicht gefunden. `src/index.css` kennt keinen
  `max-width`-Block für `.vater-*`.
- Die Perspektiven stehen seit 2026-07-28 (`navigation.ts:54-97`); ihre Handy-Weiche kommt mit B-663.
- Daten: `ChildDay` trägt heute kein Antwortzahlfeld ([B-770](B-770-antwortzahl-je-kind-und-tag.md)); der Sammelabruf der Einlösungen
  kommt mit B-747.

## Entscheidungen

1. **Die Betreuen-Startseite `/vater` wird zur Vater-Sicht; Kind anlegen, Kinder-Tabelle und Pläne-Karte ziehen auf eine eigene
   Verwaltungsseite unter `/vater`** (B-662 Entscheidung 1). Kosten: ein Klick mehr zum Anlegen eines Kindes; die Verwaltungsseite
   braucht einen Navigationspunkt. Schließt aus: Verwaltung vor dem Blick auf das Lernen.
2. **Die Zahl offener Einlösungen steht am Navigationspunkt „Shop"; die Liste aller Kinder ohne Kindwahl steht auf der Startseite**
   (B-662 Entscheidung 2). Kosten: ein Zählabruf je Seitenaufruf der Betreuen-Sicht.
3. **Die Anzeige „Tagesziel erreicht" liest `GoalsMet` und `GoalsTotal`; nach dem Planer (B-644 E3) bleiben die Felder** (B-662
   Entscheidung 8). Kosten: Die Bedeutung ändert sich später still von Position zu Pflichtaufgabe.
4. **Telefonbreite ist Ziel nur für die Betreuen-Sicht; Zuweisen und Erstellen bleiben Web** (B-662 Entscheidung 4). Kosten: eine
   E2E-Probe bei 390 px zusätzlich zu der bei 768 px.
5. **Ein Lehrer-Konto sieht die Seite nicht** (B-662 Entscheidung 7, Rahmen C).
6. **Der Vater setzt Leitplanken und hat eine Notbremse, er legt keine Tagesmengen fest** (B-662 Entscheidung 6, Rahmen B): gilt für
   den Inhalt dieser Sicht, keine Plan-Maske.

## Akzeptanzkriterien

1. Die Betreuen-Startseite zeigt je Kind: geübt heute (Antwortzahl), Tagesziel („n von m") und Punkte heute, und keine Kinder-Tabelle,
   kein Anlegeformular, keine Pläne-Karte. *Rot, wenn* eines dieser Elemente weiter auf `/vater` steht (`VaterDashboard.tsx`).
2. Offene Einlöse-Anfragen aller Kinder stehen auf einer Seite, ohne ein Kind zu wählen, mit dem Kindnamen je Zeile. *Rot, wenn* die
   Liste weiter nur nach Kindwahl erscheint.
3. Die Zahl offener Anfragen steht an der Navigation und sinkt nach dem Freigeben. *Rot, wenn* der Zähler fehlt oder nach `approve`
   stehen bleibt.
4. Die Verwaltungsseite trägt Kind anlegen, Kinder-Tabelle und Pläne-Karte und ist aus der Navigation erreichbar. *Rot, wenn* eines
   davon beim Umzug verloren geht.
5. Bei 390 px Breite scrollt die Betreuen-Startseite nicht seitlich, und Freigeben ist erreichbar. *Rot, wenn* die neue
   Playwright-Probe bei 390 px einen Überlauf misst.
6. *Absichtserklärung.* Ein Lehrer-Konto (nur Creator) sieht die Seite nicht. *Rot, wenn* `perspectivesFor` sie ihm zeigt.
7. Die 15 Specs, die auf die Überschrift „Kinder" warten, laufen grün; sie warten auf die neue Startseite oder gehen über die
   Verwaltungsseite. *Rot, wenn* eine davon wegen der verschobenen Überschrift fällt.

## Schätzung

**Größe: L**, wie eine DB-Umbau-Etappe (E6) im Aufwand, nicht im Risiko: eine Seite wird zerlegt, drei Verbraucher kommen dazu (Zähler,
Sammelliste, Kindname), und fünfzehn Specs hängen an der Startseite. Kein XL, weil die Teile alle in einer Oberfläche liegen und die
Daten schon in B-747 und B-770 stehen.

- **`wo: frontend`**: Backend kam mit B-747 und B-770, hier fällt kein C# an.
- **`migration: nein`**.
- **`vertragsbruch: nein`**: nur neue Typen aus der erzeugten `contract.ts`.

### Risiken

- **15 Specs (AK 7):** Die Überschrift „Kinder" gehört heute zur Startseite. Der bequeme Weg, die Überschrift beizubehalten, würde die
  Entrümpelung unterlaufen; der ehrliche Weg ändert die Wartebedingung in den Specs, am besten über einen gemeinsamen Helfer in
  `e2e/helpers.ts` statt 15 Einzeländerungen. Das ist die versteckte Hälfte des Aufwands.
- **Reihenfolge:** B-663 muss zuerst fertig sein (Perspektiv-Weiche und Telefonbreite), sonst baut diese Story eine Startseite ohne
  bekannte Perspektive (B-662 Entscheidung 5).
- **Fremde Arbeit:** `frontend/package.json` und `sohn/*` tragen ungecommittete LearningDad-Arbeit; `vater/*` ist nicht betroffen,
  `index.css` aber schon (`M frontend/src/index.css` im Arbeitsbaum): Stilblock für 390 px erst nach Abstimmung.
- **Zähler und Aktualität:** Der Zähler an der Navigation muss nach `approve` aus der Shop-Seite nachziehen (gemeinsame Quelle, kein
  zweiter Abruf, der veraltet).
- **Kindname:** `ActivationRequestDto` trägt nur `ChildId` (B-747); die Kinderliste muss geladen sein, sonst erscheint eine Id.
- **Zuweisen und Erstellen bleiben erreichbar** über die Perspektive; die Sperre am Handy kommt aus B-663, nicht aus dieser Story.

### Angriffsplan

1. Verwaltungsseite aus den Teilen von `VaterDashboard.tsx` (`:84-150`) auslagern, Route und Navigationspunkt ergänzen
   (`navigation.ts`, `navigation.test.ts` anpassen).
2. Startseite: Karte „Heute" um die Antwortzahl aus B-770 erweitern, Sammelliste der Einlösungen aus B-747 mit Freigeben/Ablehnen
   (Schreib-Primitive `useAction`, `StatusBanner`), Zähler am Navigationspunkt „Shop".
3. Specs umstellen (gemeinsamer Helfer), danach neue Probe bei 390 px.
4. Vitest, Playwright, `npm run build`, Lint.

### Testweg

- Vitest `src/vater/VaterDashboard.test.tsx` (vorhanden): AK 1; `src/vater/navigation.test.ts` (vorhanden): Verwaltungspunkt, Zähler.
- Neu: Vitest für die Sammelliste (Kindname, Freigeben), AK 2 und 3.
- E2E `e2e/perspektiven.spec.ts` und die 14 weiteren Specs (AK 7), neue Spec `e2e/vater-handy.spec.ts` bei 390 px (AK 5), angelehnt an
  `e2e/responsive-vater.spec.ts`. Je Spec die Änderung benennen, die sie rot macht (nie eine Zusicherung auf einen Zustand, den der
  Ausgangsbestand ohnehin hat).
- Rollengang: die neue 390-px-Spec fährt Vater, Einlösen freigeben (Kind kauft und löst ein, Vater gibt frei), das ist der Rollengang.
- Vor der Abnahme: Agent `frontend-reviewer`.

## Verlauf

- **2026-11-20** — angelegt aus [B-662](B-662-vater-sicht-arbeit-tagesziel-kaeufe.md) (geteilt); `geschaetzt`: L, frontend, keine
  Migration, kein Vertragsbruch (Vorbereitungslauf, Sonnet).
