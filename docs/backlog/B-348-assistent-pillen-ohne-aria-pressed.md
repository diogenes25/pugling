---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/a11y, rolle/supervisor]
aliases: [Assistent-Pillen ohne aria-pressed]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Bau von B-262 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-11-02"
---

# B-348 · Die Auswahl-Pillen im Assistenten sagen nicht, welche gewählt ist

`ChoicePill` (`frontend/src/vater/VaterWizard.tsx`, „Bestehendes Kind" / „Neues Kind anlegen" u. a.) zeigt die Wahl nur
über die Klasse `ghost` – also über Farbe. Ein Screenreader hört zwei gleich klingende Schaltflächen ohne Zustand. Die
Konvention des Vater-Webs ist `aria-pressed` (Accessibility-Pass, „aria-pressed statt role=\"tab\"").

Gemessen beim Bau von B-262: `toHaveAttribute("aria-pressed", "true")` an „Bestehendes Kind" fällt, das Attribut fehlt.
Vorschlag: `aria-pressed={active}` an `ChoicePill`, eine Zusicherung in `assistent-kinderliste-kommt-spaet.spec.ts`.

## User Story

Als **Vater** mit Screenreader möchte ich im Assistenten hören, welche Kind-Art, welches Ziel und welche Intensität gewählt
ist.

## Ist-Stand (Lauf L, am Code belegt)

`ChoicePill` und `OptionCard` (`VaterWizard.tsx`) zeigen die Wahl nur über Farbe bzw. Rahmen. Gemessen: die Zusicherung
`aria-pressed` fällt an beiden.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. ~~`aria-pressed` an beiden Bausteinen~~ – nach dem Review revidiert: **`role="radio"` + `aria-checked`**, die drei
   Gruppen (Kind-Art, Ziel, Intensität) als benannte `radiogroup`. Die Gruppen haben immer genau eine Wahl, ein zweiter
   Klick löst sie nicht – das ist Radio-, nicht Umschalter-Verhalten, und das Vater-Web zeichnet genau diese Form schon
   so aus (`VaterLogin`, `VaterMedia`, `VaterLernstand`). Die erste Begründung („Konvention ist `aria-pressed`") stimmte
   nur halb: das Repo kennt beides. *Kosten:* sieben Specs sprechen die Pillen jetzt als `radio` an.

## Akzeptanzkriterien

1. Nach der späten Kinderliste trägt „Bestehendes Kind" `aria-checked="true"`, „Neues Kind anlegen" `"false"`; nach Eingabe
   eines neuen Kindes umgekehrt (beide Seiten geprüft).
2. Die vorbelegte Ziel-Karte trägt `"true"`; nach einem Klick auf eine andere wechselt es.
3. Die drei Gruppen sind als `radiogroup` mit ihrer Frage bzw. „Welches Kind?" benannt.

## Schätzung

`XS`, Frontend. Testweg: `e2e/assistent-kinderliste-kommt-spaet.spec.ts`.

## Verlauf

- **2026-10-30** — angelegt beim Bau von B-262 (Lauf L, Teil 3); außerhalb des Sprint-Ziels, darum eigene Story.
- **2026-10-30** — Lauf L, Teil 4: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe: beide Fälle
  der Spec **2 failed** (erwartet 2, `:46` und `:73`, die Pillen). Nach dem Fix **2 passed**. Probe nur für `OptionCard`
  (Attribut dort entfernt) → **1 failed** an `:35`, der Ziel-Karten-Zusicherung.
- **2026-10-30** — Review `frontend-reviewer`: 🟡 Toggle statt Radio (Begründung nur halb wahr) → `role="radio"`/`aria-checked`,
  benannte `radiogroup` (zählt); 🟡 Ziel und Intensität ohne Gruppennamen (das `<label>` hing an keinem Feld) → `span` mit
  `id` + `aria-labelledby` (zählt); 🟢 AK 1 „umgekehrt" nur halb geprüft → Gegenseite ergänzt; 🟢 Stepper zeigt den
  aktuellen Schritt nur über Farbe → nicht im Diff, als Hinweis in [B-350](B-350-wartebild-der-klausur-ohne-ausweg.md)
  nicht passend, darum hier vermerkt und offen. Danach die sieben Assistenten-Specs **15 passed**; Probe „Ziel-Gruppe
  ohne `aria-labelledby`" **1 failed** an `:34`, Probe „`ChoicePill` ohne `role`" **2 failed**. Kein Delta-Review
  (Selbst-Check mit gemessenen Proben).
- **2026-10-30** — Abnahme Lauf L, Teil 4: `tsc -b` sauber, Vitest **587/587**, E2E-Gesamtlauf **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet). Rollengang als E2E: Vater über die sieben Assistenten-Specs (Pillen und Karten jetzt als `radio` angesprochen) und `TextbookForm`; Sohn und Creator Regressionszeugen (Gesamtlauf grün, kein Pfad im Diff). Gerätecheck offen: die Radiogruppen einmal mit NVDA/VoiceOver anhören. **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
- **2026-11-02** — Nachschau in Lauf M, Teil 1 (frischer `frontend-reviewer`, `git show d8cc6b9 -- frontend`, Vitest **34 passed** in `TextbookForm`/`SohnTest`): **entgangen** [B-352](B-352-radiogruppen-ohne-pfeiltasten.md) (🟡, `role="radio"` ohne Pfeiltasten/roving tabindex – das Muster der zitierten Vorbilder, aber hier neu eingeführt). Geprüft: AK 1 → `assistent-kinderliste-kommt-spaet.spec.ts:56-57/86-87`, Mutanten „`aria-checked` weg", „konstant true", „`role` weg", „Call-Site vertauscht" je rot (abgeleitet); AK 2 → `:38-41`; AK 3 → `:34/35/85`; kein übriggebliebener `getByRole("button", …)` auf Pillen/Karten. 🟢: Zustand der Intensitäts-Karten nirgends zugesichert (Mutante `active={false}` an `VaterWizard.tsx:498` bliebe grün) – in Lauf M, Teil 2 behoben; `aria-label` „Welches Kind?" statt `aria-labelledby` am sichtbaren „Für welches Kind?" (ohne Story).
