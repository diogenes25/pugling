---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, bereich/frontend, rolle/supervisor]
aliases: [Fenster und Vorschau im Klassenarbeiten-Dialog]
status: geschaetzt
prio: P2
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-248
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-757 · Der Vater stellt das Vorbereitungs-Fenster ein und sieht „etwa N Karten je Tag“

Teil (c), Oberfläche, von [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md), geteilt beim Schätzen am 2026-11-20. Jeder Backend-Endpunkt
braucht ein UI: die Felder aus [B-752](B-752-vorbereitungs-pflicht-im-fenster.md) bekommen ihren Platz im Klassenarbeiten-Dialog des Vaters.

## User Story

Als *Vater* möchte ich im Dialog der Klassenarbeit das Vorbereitungs-Fenster einstellen und sehen, wie viel Last es dem Kind macht, und erfahren, wenn
keine Pflicht entsteht, damit ich die Vorbereitung bewusst steuere.

## Ist-Stand am Code

- Der Dialog ist `frontend/src/vater/VaterClassTests.tsx` (`ClassTestManager`, Test `VaterClassTests.test.tsx`); er kennt Titel, Termin, Fach, Übungen,
  Tags, Note und Status, aber kein Fenster. Die Typen stehen in `frontend/src/lib/types.ts` (`CreateKlassenarbeitDto`, `KlassenarbeitResponse`).
- Der Assistent `VaterWizard.tsx` legt Termin und Klassenarbeit an, setzt aber kein Fenster (abgenommen in B-423; der Standard gilt).

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt; übernommen aus B-248.*

1. **Die Vorschau ist eine reine Zahl „etwa N Karten je Tag“, vom Server geliefert, nicht im Client gerechnet** (B-248 Entscheidung 8, Vorschau). Begründung: die
   Regel (Karten / Fenstertage, Deckel) steht an **einer** Stelle, der Deckel ist Konfiguration. Kosten: ein Feld mehr in der Antwort. Verworfen: Rechnung im
   Client. Schließt aus: zwei Fassungen der Dosisregel.
2. **Der Grund für „keine Vorbereitungs-Pflicht“ steht in der Klassenarbeiten-Liste** (B-248 Entscheidung 5): kein aktiver Plan im Fach oder Fach leer.
   Kosten: ein Text mehr. Schließt aus: dass die Kernidee still ausfällt.
3. **Das Fenster steht im Dialog als Zahl mit Standard; 0 schaltet ab** (B-248 Entscheidung 10). Leer lässt den Standard gelten. Kosten: ein Eingabefeld mehr im
   Formular.

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht (aus Entscheidungen 1 bis 3; B-248 hatte dafür kein eigenes Kriterium).

1. Das Formular schickt `prepWindowDays` beim Anlegen und Ändern und zeigt den gespeicherten Wert; leer lässt den Standard gelten, 0 schaltet ab. *Rot, wenn* das Feld
   im Formular oder in `lib/types.ts` fehlt (der Server lehnt es als `unknown_field` ab oder speichert nichts).
2. Die Liste zeigt für eine geplante Arbeit mit Pflicht „etwa N Karten je Tag“ mit der Zahl des Servers; bei Fenster 0 und bei abgesagten oder geschriebenen Arbeiten keine
   Zahl. *Rot, wenn* der Client selbst rechnet oder eine Zahl bei Fenster 0 steht.
3. Entsteht keine Pflicht, nennt die Liste den Grund (kein Plan im Fach, kein Fach). *Rot, wenn* die Anzeige fehlt oder bei vorhandener Pflicht einen Grund zeigt.
4. Das Speichern ohne Änderung des Fensters lässt den Wert unverändert, und „zurück auf Standard“ ist möglich. *Rot, wenn* ein leeres Feld beim Speichern einen gesetzten Wert
   überschreibt oder es keinen Weg zurück auf den Standard gibt (PATCH-Semantik des `UpdateClassTestDto`: ein fehlendes Feld ändert nichts, Zurücksetzen braucht einen
   Schalter wie `ClearGrade`).

## Schätzung

**Größe: S** – wie `childId` aus dem Test-Pfad ziehen (B-01): ein Formularfeld, zwei Anzeigen in einer bestehenden Liste, Typen und ein Test. Kein Schema.

- **`migration: nein`**.
- **`vertragsbruch: nein`** – nachgesehen: die Felder liefert B-752 additiv.
- **`wo: frontend`** – Backend steht in B-752.

### Risiken

- **Hängt an B-752** (Felder und Server-Zahl); vorher nichts zu zeigen.
- **Zurück auf Standard braucht einen Schalter:** `UpdateClassTestDto` ist PATCH (ein fehlendes Feld ändert nichts); ohne ein `ClearPrepWindow` nach dem Muster
  `ClearGrade` (`Contracts/Supervisor/ClassTestDtos.cs`) lässt sich ein einmal gesetztes Fenster nicht auf „leer“ zurückstellen. B-752 legt das Feld additiv an.
- **Tagesplaner-Risiko wie B-752:** übernimmt B-644 die Terminpflicht, entfällt die Story.
- **Der Vater-Dialog hat deutsche Literale** (`VaterClassTests.tsx`), die Sohn-App übersetzt; hier gelten die Vater-Konventionen aus `frontend/CLAUDE.md`.

### Angriffsplan

1. `lib/types.ts` und `lib/api.ts` um die Felder ergänzen.
2. Formularfeld und Anzeigen in `ClassTestManager`; `useAction`/`StatusBanner` wie die übrigen Schreibwege.
3. Test, E2E; Typprüfung und `vitest` über `./node_modules/.bin/`.

### Testweg

- `frontend/src/vater/VaterClassTests.test.tsx`: AK 1 (Feld wird gesendet), AK 2 und 3 (Anzeigen), AK 4.
- Neue E2E `frontend/e2e/klassenarbeit-vorbereitung.spec.ts` (Muster `assistent-klassenarbeit.spec.ts`): Vater legt eine Arbeit mit Fenster an, sieht die
  Zahl; ein Kind ohne Plan im Fach zeigt den Grund. Das ist der Rollengang.
- Vor der Abnahme: Agent `frontend-reviewer`.

## Verlauf

- **2026-11-20** — angelegt beim Teilen von [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md) und geschätzt (Vorbereitungslauf, Sonnet): S, frontend,
  keine Migration, kein Vertragsbruch. Hängt an B-752.
