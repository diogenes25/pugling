---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/vater]
aliases: [role=radio ohne Pfeiltasten, Radiogruppe verspricht Tastaturbedienung]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf L, Teil 4 in Lauf M (docs/pm-sitzung-2026-11-02-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-348]
wartet_auf: ""
nachgeschaut: "2026-11-02"
---

# B-352 · Die Radiogruppen des Vater-Webs versprechen Pfeiltasten, die nicht kommen

Seit [B-348](B-348-assistent-pillen-ohne-aria-pressed.md) tragen `ChoicePill` und `OptionCard` im Assistenten
(`frontend/src/vater/VaterWizard.tsx:812`, `:821`) `role="radio"` in einer benannten `radiogroup`. Nach dem
APG-Radiogroup-Muster ist eine solche Gruppe **ein** Tab-Stopp, und die Pfeiltasten wechseln und wählen. Hier ist jede
Option ein eigener Tab-Stopp, und es gibt weder Pfeiltasten-Handler noch roving `tabIndex` (kein `ArrowRight`/`ArrowDown`
in `frontend/src/`). Ein Screenreader sagt „Optionsfeld, nicht aktiviert, 2 von 3"; der Nutzer drückt Pfeil-rechts, und
nichts passiert. Die erste Fassung von B-348 (`aria-pressed`) wäre ehrlich gewesen – ein Knopf verspricht keine Pfeiltasten.

Dasselbe Muster haben die Vorbilder, auf die sich B-348 berief: `VaterLogin.tsx:36/154`, `VaterMedia.tsx:198`,
`VaterLernstand.tsx:41`. Nur `PlanPositions.tsx:571` nutzt native `<input type="radio">` und bekommt die Pfeiltasten
umsonst. Vorschlag des Prüfers: ein kleiner Baustein (`RadioPills`) mit roving tabindex und Pfeiltasten – oder native
Radios in Pillen-Optik – und die fünf Gruppen darauf umziehen. Zu klären beim Ausformulieren: welcher der beiden Wege.

## User Story

Als **Vater**, der das Vater-Web mit der Tastatur oder einem Screenreader bedient, möchte ich in einer Auswahlgruppe mit
den Pfeiltasten wählen und sie mit einem Tab verlassen können, damit die Gruppe tut, was sie ansagt.

## Ist-Stand (Lauf N, am Code belegt, Stand `d0afbc0`)

- Sieben `radiogroup`s aus Knöpfen mit `role="radio"`: `VaterLogin.tsx:36` (Zugang) und `:154` (Art des Kontos),
  `VaterMedia.tsx:198` (Quelle), `VaterLernstand.tsx:41` (Sicht), `VaterWizard.tsx:416/486/496` (Kind, Ziel,
  Intensität; Bausteine `ChoicePill` `:812`, `OptionCard` `:821`). Jede Option ist ein eigener Tab-Stopp; kein
  `onKeyDown`, kein `tabIndex` (grep nach `Arrow` in `frontend/src/`: kein Treffer).
- Gegenfundstelle: `PlanPositions.tsx:571` – `radiogroup` aus nativen `<input type="radio">`, dort liefert der Browser
  Pfeiltasten und einen Tab-Stopp selbst.

## Die echte Lücke

Die Rolle verspricht das Tastaturmodell einer Radiogruppe; geliefert ist das von Knöpfen.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Das Verhalten nachrüsten, nicht die Auszeichnung zurückdrehen.** Ein Helfer `radioGroupKeys` in `lib/ui.ts` am
   Gruppen-Container: Pfeil rechts/unten → nächste nicht gesperrte Option, links/oben → vorige (umlaufend), `Home`/`End` →
   erste/letzte; der Fokus wandert und die Option wird gewählt (APG: „selection follows focus"). Dazu roving tabindex:
   die gewählte Option `tabIndex=0`, die übrigen `-1`. *Kosten:* jede der sieben Stellen bekommt zwei Attribute; native
   Radios hätten das umsonst, verlangten aber einen Umbau der Pillen-Optik an sieben Stellen – größer und mit
   Layout-Risiko. Zurück zu `aria-pressed` hieße, B-348 zurückzunehmen.
2. **Eine gesperrte gewählte Option** kommt nicht vor (alle Gruppen haben eine wählbare Vorgabe); der Helfer überspringt
   gesperrte Optionen beim Wandern.

## Akzeptanzkriterien

1. In einer Gruppe wählt Pfeil rechts die nächste Option und fokussiert sie; Pfeil links am Anfang springt ans Ende.
2. Eine Gruppe ist ein Tab-Stopp: nur die gewählte Option ist per Tab erreichbar.
3. Gesperrte Optionen werden übersprungen.
4. Alle sieben Gruppen nutzen den Helfer.

## Schätzung

`S`, Frontend, keine Migration, kein Vertragsbruch. Angriffsplan: Helfer + Vitest (Wandern, Umlauf, Sperre, Home/End) →
sieben Stellen → E2E im Assistenten (Tab-Stopp und Pfeiltaste am echten Browser). Testweg: `lib/ui.test.ts`,
`assistent-kinderliste-kommt-spaet.spec.ts`; ein Wächter, dass jede `radiogroup` aus Knöpfen den Helfer trägt.

## Verlauf

- **2026-11-02** — angelegt aus der Nachschau zu Lauf L, Teil 4 in Lauf M (`frontend-reviewer` über `d8cc6b9`, 🟡). Aus
  Code gelesen (grep nach Pfeiltasten und `tabIndex`), nicht im Browser gemessen. Entgangen bei B-348: dessen
  Entscheidung führte `role="radio"` im Assistenten ein, ohne das dazugehörige Tastaturverhalten.
- **2026-11-02** — Lauf N: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt.
- **2026-11-02** — Lauf N, Teil 1: gebaut. Rote Probe `lib/ui.test.ts` **6 failed** (erwartet 6, Helfer fehlt). AK 1 → „Pfeil rechts"/„Pfeil links vom Anfang"/„Pfeil unten vom Ende"/„Home/End"; Mutanten „ohne Umlauf" **2 failed**, „jede Taste wandert" **1 failed** („lässt andere Tasten in Ruhe"). AK 3 → „überspringt gesperrte", Mutante „Sperre ignoriert" **1 failed**. AK 4 → Wächter `radioGroups.test.ts` (Probe: `VaterMedia.tsx` zurückgesetzt → 2 Funde; Selbsttest je Lücke). Im echten Browser (`assistent-kinderliste-kommt-spaet.spec.ts`): AK 1 → `:52-59`, Mutante „ohne Helfer an der Intensität" **1 failed** an `:55`; AK 2 → `:60-62`, Mutante „ohne `tabIndex` an `OptionCard`" **1 failed** an `:62`. Spec **2 passed**, Vitest **597/597**, `tsc -b` sauber.
- **2026-11-02** — Review `frontend-reviewer`: kein 🔴/🟡 zu B-352. 🟢 behoben ohne Zählung: Pfeile mit Alt/Strg/Cmd gehören dem Browser (Test, Mutante **1 failed**); der Wächter verlangt jetzt einen *wandernden* `tabIndex` (`… ? 0 : -1`) und zählt auch Dateien mit `radiogroup` ohne eigenes `role="radio"` (Bausteine aus anderer Datei), Selbsttest je Lücke; JSDoc nennt die Voraussetzung „gewählte Option nicht gesperrt" (Entscheidung 2). Vitest **601/601**.
- **2026-11-02** — Abnahme Lauf N, Teil 1: Vitest **601/601**, `tsc -b` sauber, E2E **102 passed** (`exit=0`, nach dem letzten Edit gestartet). Rollengang: Vater im echten Browser über `assistent-kinderliste-kommt-spaet.spec.ts` (Pfeiltaste und Tab-Stopp in der Intensitäts- bzw. Ziel-Gruppe); Sohn über die Komponententests des Wartebilds (ein hängender Start lässt sich im E2E nicht billig erzeugen – argumentierte Ausnahme nach pm-loop Step 6), seine Specs im E2E grün; Creator Regressionszeuge. **Gerätecheck offen:** die Gruppen einmal mit Screenreader und Pfeiltasten bedienen. **Abgenommen**, Commit siehe Protokoll ([Lauf N](../pm-sitzung-2026-11-02-nacht.md)).
- **2026-11-02** — Nachschau in Lauf N, Teil 2 (frischer `frontend-reviewer`): **sauber** (🟢 → [B-356](B-356-radiogruppe-und-wartebild-restkanten.md)). Geprüft: AK 1 gegen Mutanten Clamp, ohne `focus()`, Home/End vertauscht, ohne Modifier-Ausnahme, `preventDefault` zu früh – je rot; AK 2 Tab von der mittleren Ziel-Karte; AK 3 Sperr-Filter; AK 4 sieben Gruppen per grep, Wächter mit Mindestzahl und Selbsttest; Fokus vor Klick, Gruppen in Formularen (`type="button"`), Vorgabe nie gesperrt.
