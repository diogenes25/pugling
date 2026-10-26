---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [ⓘ-Popover am rechten Rand, Feldhilfe scrollt die Seite seitlich]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Messung beim Bau von B-68 (fünfter Nachtlauf, docs/pm-sitzung-2026-10-24-nacht.md, Sprint 10)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-263 · Ein aufgeklapptes ⓘ am rechten Rand lässt die Seite seitlich scrollen

## User Story

Als Vater am Tablet möchte ich die Erklärung zu einem Feld aufklappen können, ohne dass die Seite seitlich
verrutscht oder die Erklärung abgeschnitten ist.

## Ist-Stand am Code

`.info-hint-pop` ist am Knopf verankert (`position: absolute; left: -6px`, `frontend/src/index.css`) und bis zu
`min(320px, 78vw)` breit; `InfoHint` (`frontend/src/components/InfoHint.tsx`) setzt keine Lage. Steht das ⓘ nahe
am rechten Rand, ragt das Popover über. Gemessen beim Bau von B-68 bei 768 px (Wegwerf-Spec, Seed-Daten): an
sieben ⓘ auf `/vater/exercises/neu` (2), `/vater/kind/1`, `/vater/shop`, `/vater/rewards` (2) und `/vater/vocab`
steht das Popover **91 bis 213 px** über den Rand von `.vater-main` – ohne B-68 scrollt dann die ganze Seite
seitlich, mit B-68s `overflow-x: auto` an `.vater-main` würde es dort abgeschnitten.

## Die echte Lücke

Das Popover kennt den Platz nicht, in dem es aufgeht.

## Offene Punkte

1. ~~Verschieben, umklappen (rechtsbündig) oder per Portal aus dem Rahmen heben?~~ → Entscheidung 1.

## Entscheidungen

1. **Beim Öffnen messen und nach links verschieben, so weit es übersteht – nie über den linken Rand hinaus.**
   Begründung: hält die gewohnte Lage (links am Knopf), wo Platz ist; eine Portal-Lösung bräuchte eigene
   Lageberechnung beim Scrollen. Grenze ist der sichtbare Streifen: das Fenster, verengt um jeden Vorfahren
   mit beschnittenem Überlauf (Tabellen-Rahmen, `.vater-main`). Kosten: ein `useLayoutEffect` je Öffnen; ein
   Fenster-Resize bei offenem Popover wird nicht nachgeführt (es schließt beim nächsten Klick ohnehin).
   Autonom gegrillt (`Defekt`, Freigabe 1).
2. **`position: fixed` statt `absolute`** (Entscheidung des Nutzers, 2026-10-26): Das Popover steht fest zum
   Fenster und kommt damit aus jedem Rahmen mit beschnittenem Überlauf heraus, auch aus dem `overflowX`-Rahmen
   der Lückentext-Tabelle. Grenze für Verschieben und Klappen ist darum das Fenster, nicht mehr der Rahmen
   (ersetzt in Entscheidung 1 den „sichtbaren Streifen“). Ein solches Popover liefe beim Scrollen vom Knopf weg,
   also **schließt es beim Scrollen und beim Resize**. Geschlossen wird nur, wenn der Knopf sich wirklich bewegt
   hat: `scroll` kommt erst im nächsten Frame an, und ein Scrollen von *vor* dem Öffnen schlösse es sonst sofort
   wieder (gemessen, siehe Verlauf).

## Akzeptanzkriterien

1. Bei 768 px wird kein aufgeklapptes ⓘ des Vater-Webs abgeschnitten, weder vom Fensterrand noch von einem
   Rahmen mit beschnittenem Überlauf (auch nicht in der Lückentext-Tabelle mit einer Lücke). Am unteren
   Fensterrand klappt es über den Knopf, wenn oben Platz ist. Das Dokument wird dabei nicht breiter als das Fenster.
   *(Geändert 2026-10-26 mit Entscheidung 2, vorher: „über den sichtbaren Rand von `.vater-main`“.)*
2. Wo Platz ist, bleibt das Popover in seiner bisherigen Lage.
3. Scrollen oder eine Größenänderung bei offenem ⓘ schließen es.

## Schätzung

XS, `wo: frontend`, keine Migration, kein Vertragsbruch. Testweg: `responsive-vater.spec.ts` (B-68) klappt jedes
sichtbare ⓘ der 22 Routen auf und misst beides; `feldhilfe-lage.spec.ts` (seit 2026-10-26) prüft Tabellen-Rahmen,
Klappen und Schließen.

## Verlauf

- **2026-10-24** — angelegt beim Bau von B-68 (Sprint 10, NP-5); im selben Sprint behoben nach Freigabe 3 (gezählt:
  Fund 1). Autonom gegrillt und geschätzt.
- **2026-10-24** — gebaut: `InfoHint.tsx` misst beim Öffnen (`useLayoutEffect`), verschiebt per `marginLeft` und klappt nach oben (`.info-hint-pop.up`), wenn `.vater-main` es unten abschnitte; die angewandte Lage wird beim Messen herausgerechnet (Review-Fund). Rote Proben siehe B-68. `frontend-reviewer` siehe B-68. **Unterbrochen (Rechner aus)**, Stufe `in-arbeit`.
- **2026-10-25** — fortgesetzt am 2026-10-25 (Wiederaufnahme des fünften Nachtlaufs, Sprint 10 / NP-5, Zähler weiter bei 5; [Protokoll](../pm-sitzung-2026-10-25-nacht.md)).
- **2026-10-25** — Delta-Review (`frontend-reviewer`) der Behebungen: korrekt, aber **zwei offene Funde** (keiner behoben, der Lauf endete an Freigabe 3 – Fund 6 von Sprint 10):
  1. *Testlücke zu AK 1* („dort klappt es über den Knopf"): kein Fall der Spec löst `up` aus (schon im Vorlauf gemessen: „Klappen nach oben aus" → 1 passed). Vorschlag des Reviewers: einen Schritt, der die Lage erzwingt (ⓘ ans Ende von `.vater-main` bzw. in einen niedrigen Scroll-Rahmen), dann `.up`, `p.bottom <= Knopf.top`, `overBottom <= 0`; rote Probe: `.info-hint-pop.up` entfernen.
  2. *ⓘ in einem `overflowX`-Rahmen bleibt unten abgeschnitten* (`ClozeTexts.tsx:239-245`, „Auch richtig" im `<th>`): `overflowX: auto` macht den Rahmen auch senkrecht zum Scroll-Container; bei ein, zwei Lücken ist die Tabelle niedriger als das Popover, nach oben ist kein Platz. Die Spec misst nur gegen `.vater-main` und öffnet dieses ⓘ nie (es erscheint erst mit Lücken). Vorschlag: `position: fixed` mit gemessenen Koordinaten, oder B-263 ausdrücklich auf `.vater-main` beschränken; im Test gegen den nächsten beschneidenden Vorfahren messen.
  3. *Nit:* Drehen/Resize bei offenem Popover wird nicht nachgeführt (ein `resize`-Listener, der schließt, genügte).
- **2026-10-26** — **Entscheidung des Nutzers** (Morgen-Entscheidung 2026-10-26, Dialog, [Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)): Fund 2 des Delta-Reviews wird über **`position: fixed`** gelöst: das Popover misst seine Koordinaten wie bisher, steht aber fest zum Fenster und kommt so aus jedem beschneidenden Rahmen heraus; es **schließt beim Scrollen und beim Resize** (erledigt damit auch Nit 3). Die Spec misst gegen den nächsten beschneidenden Vorfahren und öffnet das ⓘ in `ClozeTexts` mit ein, zwei Lücken. Fund 1 (Klappen nach oben ungetestet) wird mitgenommen. Nacharbeit mit **neuem Zähler (0)**. `wartet_auf` geleert.
- **2026-10-26** — Nacharbeit gebaut (Lauf A, Fortsetzung Sprint 10, Zähler 0; [Protokoll](../pm-sitzung-2026-10-26-tag.md)). Fund 1 und Fund 2 aus dem Delta-Review sind die Nacharbeit selbst und werden nicht neu gezählt. `InfoHint.tsx`: Die Lage (`left`/`top`) kommt aus dem Knopf, begrenzt durch das Fenster, `.info-hint-pop` ist `position: fixed`. Das Popover schließt bei `scroll` (capture, nur wenn der Knopf sich bewegt hat) und bei `resize`. Neue Spec `e2e/feldhilfe-lage.spec.ts` (Lückentext-Tabelle mit einer Lücke, Klappen am unteren Fensterrand, Schließen). `responsive-vater.spec.ts` misst seither gegen das Fenster plus Treffertest an den Ecken (`elementsFromPoint`, 16 px nach innen).
- **2026-10-26** — Messungen auf dem Weg (Auflage 5, je erwartet/gemessen): (1) Ein Schließen bei jedem `scroll` schloss das Popover sofort nach dem Öffnen. Erwartet war offen, gemessen: 3 von 4 Tests rot, `toBeVisible` → *element(s) not found*. Daraus kommt der Abgleich mit der Knopf-Lage. (2) Ein Treffertest mit `elementFromPoint` 3 px vom Eck meldete Ecken als verdeckt, obwohl nichts abgeschnitten war. Rechts liegt dort die Trefferzone des überlagernden Scrollbalkens von `.vater-main` (757 px → `main`, 745 px → Popover), unten links die Rundung (`border-radius: 12px`; der Punkt traf den Knopf darunter). Darum gilt jetzt `elementsFromPoint` mit 16 px Abstand.
- **2026-10-26** — rote Proben (je erwartet → gemessen): **HEAD-Stand** (`absolute`) gegen `feldhilfe-lage` → erwartet 3 rot, gemessen **3 failed**: Tabelle an `:69` (Treffertest; die Vorbedingung `:68` hielt), Klappen an `:93`, Schließen an `:109`. **`up = false`** → erwartet Klappen rot, gemessen **2 failed** (Klappen und die Tabelle, deren ⓘ bei 1024 px ebenfalls am unteren Rand klappt). **Ohne `scroll`-Listener** → rot an `:109`, **ohne `resize`-Listener** → rot an `:114` (je 1 failed). **Schließen bei jedem `scroll`** → `feldhilfe-lage` 3 passed (trägt es nicht), `responsive-vater` **1 failed** an `:70` (`toBeVisible`). Dieser Fall wird also von der Routen-Spec getragen. **Ohne Klemmen** (`left = btn.left - 6`) → **22** `overRight`-Meldungen. Grün: beide Specs **4 passed**, Vitest **428/428**, `tsc -b` sauber.
- **2026-10-26** — Abgrenzungen im Testaufbau (Pflichtschritt pm-loop Step 5): *oben statt unten*: Die Vorbedingung `untenZuEng` (`:88`) belegt, dass unten kein Platz wäre. Bei der Mutante „beide gleich“ (`up` immer `false`) fällt `:93`. *Ragt über den Rahmen hinaus*: Die Vorbedingung `ragtHeraus` trennt „im Rahmen“ von „über den Rahmen“, ohne sie wäre der Treffertest trivial grün. *Ruhelage statt verschoben*: Der Schritt „Ruhelage bleibt“ (`dx: -6`) steht gegen die 22 verschobenen Popover der Routen.
- **2026-10-26** — `frontend-reviewer` (Lauf A, Runde 1 zu B-263, [Protokoll](../pm-sitzung-2026-10-26-tag.md)): kein Blocker; kein Vorfahr mit `transform`/`filter`/`contain` an irgendeinem ⓘ (17 Dateien, `Modal` geprüft), keine erste Zeichnung vor dem Messen. **Fund 3 von Lauf A (🟡, gezählt):** Passt das Popover weder unten noch oben (Querformat am Handy), lief es unten aus dem Fenster und ließ sich, fest stehend, nicht herscrollen – eine Regression des Umbaus. Behoben: `top` ins Fenster geklemmt, `max-height: calc(100dvh - 16px); overflow-y: auto`. **Fund 4 von Lauf A (gezählt):** Die Tests trugen mehrere behauptete Abgrenzungen nicht – senkrechte Schnitte unter 16 px (Treffertest-Saum), „bleibt offen nach einem Scrollen von vor dem Öffnen“ nur zufällig über `responsive-vater:70`, Schließ-Schwelle (Test scrollte 40 px). Behoben: `imFenster` misst die Koordinaten genau (Tabelle, Klappen, neuer Fall Querformat 768×360 mit Vorbedingung „weder unten noch oben Platz“), eigener Fall „Scrollen von vor dem Öffnen schließt nicht“ (Knopf unter dem Falz als Vorbedingung, zwei Frames), Scroll-Probe auf 3 px. Rote Proben (je erwartet rot → gemessen): ohne senkrechtes Klemmen → **1 failed** an `:132` (`imFenster`); Klemmen bis an den Fensterrand statt 8 px → **1 failed** an `:132`; jedes `scroll` schließt → **1 failed** im neuen Fall (schon beim Öffnen, `:57` – das nachgereichte `scroll` schließt es dort); Schwelle 39 px → **1 failed** an `:162`. Grün: beide Specs **6 passed**. Nicht behoben, zurückgestellt → [B-273](B-273-feste-feldhilfe-resize-und-layoutverschiebung.md): `resize` ohne Prüfung (ungemessen), Layout-Verschiebung ohne Scrollen, nicht prüfbare Linksrand-Regel. Doku-Funde (Kopfkommentare, Meldungstext `overBottom`) ohne Zählung nachgezogen.
- **2026-10-26** — Delta-Review `frontend-reviewer` (Lauf A, Runde 3, ([Protokoll](../pm-sitzung-2026-10-26-tag.md))): Die Behebungen sind korrekt. **Fund 5 von Lauf A (🟡, gezählt):** Der Querformat-Fall trug `max-height` nicht, weil bei 768×360 kein Popover höher als 344 px ist (gemessen 101–271 px). Behoben mit einem eigenen Fall bei 768×200 mit der Vorbedingung „Text höher als das Fenster“ und `scrolltInSich`. Rote Probe ohne `max-height`/`overflow-y` → erwartet rot, gemessen **1 failed** an `:148` (`scrolltInSich: false`, die Vorbedingung hielt). Grün: `feldhilfe-lage` **6 passed**.
- **2026-10-26** — Rollengang (Lauf A, [Protokoll](../pm-sitzung-2026-10-26-tag.md)): Die Chrome-Extension war nicht verbunden, darum nach Freigabe 6 als E2E: `responsive-vater.spec.ts` (Vater am Tablet, 22 Routen, jedes ⓘ), `feldhilfe-lage.spec.ts` (6 Fälle), `gruppenkopf-benennt-die-gruppe.spec.ts`, Wächter `liveRegions.test.ts`. Sohn und Creator als Regressionszeugen: volle Suite grün, keine ihrer Specs rot. Verifikation: `tsc -b` sauber, Vitest **435/435**, Playwright voll nach dem letzten Code-Edit **93 passed**, danach `feldhilfe-lage` mit dem nur testseitigen Zusatz **6 passed**. Offen für einen Menschen: die echte Screenreader-Ansage (B-134/B-236) und das Popover am echten Tablet. **Abgenommen**; Commit: folgt im Nachtrag.
