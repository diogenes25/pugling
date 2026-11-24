---
tags: [typ/story, status/abgenommen, bereich/a11y, rolle/supervisor]
aliases: [Am-Rechner-Hinweis bedingte Live-Region]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Nachtlauf 2026-11-21, Vor-Prüfung der Dad-Arbeit: `src/liveRegions.test.ts` rot auf `main`"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-663]
nachgeschaut: "2026-11-21"
---

# B-774 · Der Hinweis „nur am Rechner“ entsteht zusammen mit seiner Live-Region

Seit B-663 zeigt das Vater-Web bei schmalem Fenster in Zuweisen/Erstellen den Hinweis „Zuweisen und Erstellen gibt es
nur am Rechner“. Die Region `role="status"` wird erst **mit** dem Text gerendert
(`frontend/src/vater/VaterApp.tsx:199-203`). Ein Screenreader sagt eine Region, die zusammen mit ihrem Inhalt
eingefügt wird, unzuverlässig an. Das Tor `src/liveRegions.test.ts` (B-134) meldet genau das, und seit `5f024e2b` ist
die Vitest-Suite auf `main` rot.

## User Story

Als *Vater mit Screenreader am Handy* möchte ich den Hinweis „nur am Rechner“ angesagt bekommen, sobald er erscheint,
damit ich weiß, warum die Seite leer bleibt.

## Ist-Stand am Code

Gemessen am 2026-11-21 auf `fd714804`.

- `VaterApp.tsx:199`: `{amRechnerHinweis && narrow && (<div className="banner info" role="status">…</div>)}`.
- `liveRegions.test.ts:141` erwartet `[]` und bekommt `["./vater/VaterApp.tsx:200 steht hinter einer Bedingung …"]`.
- Vorbild im Bestand: `ListControls.tsx:71` und `exerciseConfig.tsx:718` rendern die Region immer und hängen die sichtbare
  Klasse an die Bedingung; `.live-region:empty { position: absolute; }` (`index.css:540`) nimmt die leere Region aus dem
  Fluss.

## Entscheidungen

1. **Die Region steht immer da, nur Klasse und Text hängen an der Bedingung**, nach dem Muster aus `ListControls.tsx`.
   Begründung: Das Tor verlangt genau diese Form, und das Muster ist im Bestand zweimal erprobt. Kosten: ein leeres
   `div` im DOM jeder Vater-Seite. Verworfen: ein Eintrag in `AUSNAHMEN`. Der Hinweis erscheint als Folge einer
   Navigation, also nach dem ersten Rendern, und genau dann muss die Region schon stehen.
2. **Nachtrag aus dem Review: Die Sprechblase von `LearningDad` ist dekorativ und keine Live-Region.** Vor dem Commit der
   Dad-Arbeit hatte ich sie nach demselben Muster zur dauerhaften Region gemacht. Das ließ `Lektion.test.tsx` („Blatt ist
   selbst keine Live-Region“) rot werden, und angesagt hätte sie trotzdem nie etwas: Die Feier-Ebene und das
   Ergebnisblatt sind `aria-hidden`, im Test steht die Beschriftung schon beim Einhängen. Darum hat sie jetzt kein
   `role`, und der Untertitel hängt nicht mehr am Titel: Die kleine Feier gibt nur „+N 🪙“ mit, und der verschwand
   bisher. Kosten: Ein künftiger Aufrufer, dessen Beschriftung sich nach dem Einhängen ändert, muss die Ansage selbst
   tragen.

## Akzeptanzkriterien

1. `liveRegions.test.ts` meldet `VaterApp.tsx` nicht mehr. *Rot, wenn* die Region wieder hinter der Bedingung steht.
2. Bei schmalem Fenster auf `/vater/plaene` ist der Hinweis sichtbar und trägt die Banner-Optik; ohne Hinweis ist die
   Region leer und ohne Banner-Optik. *Rot, wenn* die Klasse `banner info` fehlt, der Text nicht erscheint oder die
   Klasse bedingungslos am Element hängt.
3. Eine kleine Feier zeigt ihren Untertitel ohne Titel (Entscheidung 2). *Rot, wenn* die Blase nur an `caption` hängt.

## Schätzung

**Größe: XS**: eine JSX-Stelle und das bestehende Tor. Testweg: `liveRegions.test.ts` (AK 1); AK 2 über die bestehende
Playwright-Probe bei 390 px aus B-663 (`e2e/vater-schmal.spec.ts`).

## Verlauf

- **2026-11-21** — angelegt im Nachtlauf (Fund eines roten Tors, Freigabe 3) und in einem Zug `ausformuliert` →
  `gegrillt` (Defekt, autonom, Freigabe 1) → `geschaetzt` → `in-arbeit`. Rote Probe: `liveRegions.test.ts` erwartet
  0 Funde, gemessen 2 (davon einer in der Dad-Arbeit, `LearningDad.tsx:250`, vor deren Commit `8a9812a4` behoben;
  danach erwartet 0, gemessen 1).
- **2026-11-21** — gebaut: `VaterApp.tsx` (Region immer da, Klasse und Text bedingt), `vater-schmal.spec.ts` (Banner-Klasse
  mit Hinweis, leere Region ohne Banner davor), `LearningDad.tsx` (Entscheidung 2) und
  `LearningDad.test.tsx` „zeigt den Münz-Untertitel einer kleinen Feier auch ohne Titel“. AK 1 → `liveRegions.test.ts`
  (vor dem Fix 1 Fund, danach 0). AK 2 → `vater-schmal.spec.ts:30-40`. AK 3 → `LearningDad.test.tsx`. Rote Probe AK 3 und
  Lektion gegen `LearningDad.tsx` aus `8a9812a4`: erwartet 2 failed, gemessen 2 failed (der neue Fall und `Lektion.test.tsx`
  „Blatt ist selbst keine Live-Region“).
  Review `frontend-reviewer`: 🟡 Untertitel der kleinen Feier verschluckt → Entscheidung 2, AK 3 (gezählt 1);
  🟡 die Klassen-Zusicherung fing ein bedingungsloses Banner nicht → Gegenfall ergänzt (gezählt 2); 🟢 Kommentar
  versprach eine Ansage, die kein Aufrufer bekommt → mit Entscheidung 2 erledigt (ohne Zählung). Der volle Vitest-Lauf fand
  zusätzlich das rote `Lektion.test.tsx` (gezählt 3). Mutanten (E2E, von Hand):

| Mutante | Stelle | erwartet | gemessen |
| --- | --- | --- | --- |
| Region ohne `banner info` | `VaterApp.tsx` | `vater-schmal` rot | rot (`toHaveClass(/banner/)`) |
| `banner info` bedingungslos | `VaterApp.tsx` | `vater-schmal` rot | rot (`not.toHaveClass`, 1 failed) |

- **2026-11-21** — `abgenommen` mit `9ab66ecc`. Rollengang als Maschinenlauf gegen den nach dem letzten Edit gestarteten
  Server (Ausweichports 5201/5174): volle E2E-Suite **179/179**, Vitest **1297/1297**. Vater: `vater-schmal` (390 px) zeigt
  den Hinweis mit Banner-Optik und davor eine leere Region ohne Banner. Sohn: `full-flow` und `shop-verlauf` zeigen die
  Feier. Creator: Regressionszeuge, kein Pfad im Diff, eigene Specs grün. Ob ein Screenreader den Hinweis am echten Handy
  ansagt, hat niemand gehört.
- **2026-11-21** — Nachschau (Retro Sprint 99): Diff `9ab66ecc` gegen AK 1–3 gelesen. Die leere Region steht in `.vater-main` (Flex mit `gap`) außerhalb des Flusses (`.live-region:empty`, `index.css:540`), also ohne Zusatzabstand. Gezählt: 27 `role="status"` im Frontend; keine Spec findet mit einem ungefilterten `getByRole("status")` jetzt eine zusätzliche leere Region (E2E 179/179 im Lauf von Sprint 99). Kein Befund.
