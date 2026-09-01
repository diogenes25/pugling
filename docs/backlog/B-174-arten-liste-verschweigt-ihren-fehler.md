---
tags: [typ/story, status/in-arbeit, bereich/frontend, rolle/creator]
aliases: [categories.error wird nie gezeigt, Fach hat keine Arten oder Laden gescheitert]
status: in-arbeit
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-08-13 zu B-154
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-154]
wartet_auf: ""
---

# B-174 · Die Arten-Liste verschweigt, dass ihr Laden gescheitert ist

Dieselbe Klasse wie [B-111](B-111-verlauf-luegt-im-fehlerfall.md) und
[B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md), eine Fläche weiter: „dieses Fach hat keine
Arten" und „das Laden ist gescheitert" sehen identisch aus.

## User Story

Als **Creator** möchte ich sehen, dass das Laden der Arten gescheitert ist, damit ich nicht eine Art ein
zweites Mal anlege, die längst existiert — und damit ich weiß, dass der Filter beim Planbau gerade nicht
das zeigt, was im Katalog steht.

## Ist-Stand am Code (am 2026-09-01 selbst nachgeprüft)

Die Behauptung der Aufnahme stimmt, und der Weg dorthin ist kürzer als dort beschrieben:

| Schritt | Ergebnis | Fundort |
|---|---|---|
| `categories` wird geladen | `useAsync`, Abhängigkeit `[subjectId]` | `CatalogAdmin.tsx:45-46` |
| `categories.error` irgendwo gerendert? | **nein** — kein einziges Vorkommen in der Datei | `CatalogAdmin.tsx` (gegrept) |
| Was der `StatusBanner` trägt | nur `action.message`, also **Mutationen** — nie einen Ladefehler | `CatalogAdmin.tsx:116` |
| Was `CategoryRows` bei `null` tut | `if (!categories?.length) return null` — leerer Bildschirm | `CatalogAdmin.tsx:177` |
| Was die Überschrift dann sagt | `Arten {categories ? "(n)" : ""}` — „Arten" ohne Zahl | `CatalogAdmin.tsx:143` |

**Der scheiternde erste Abruf ist damit von einem leeren Fach nicht zu unterscheiden**, und zwar auf allen
drei Fach-Zuständen. Unter einem fremden Fach ist es am schlimmsten: dort hängt seit B-157 auch der
**erklärende Satz** an `categories` (`:180-193`), die Fläche wird also vollständig stumm — keine Zeile,
keine Zahl, kein Grund.

**Der Schaden ist nicht bloß kosmetisch.** Der Erklärtext der Seite verspricht „sie filtern die Übungssuche
beim Planbau" (`:145-147`). Wer auf einer stumm gebliebenen Liste eine Art neu anlegt, erzeugt eine Dublette
im **geteilten** Katalog — und die ist nach B-157/B-170 in einem Grundbestands-Fach von niemandem mehr
löschbar.

**Attribution, ehrlich:** Nicht von B-154 eingeführt — der Vorgängerstand hatte `{categories.data?.map(...)}`
und ebenfalls keine Fehleranzeige. Es ist aber die im Nachschau-Auftrag benannte Familie auf genau der
Fläche, die B-154 und B-157 zweimal umgebaut haben, und **beide** Reviews haben es nicht gemeldet.

## Die Breite, gemessen statt vermutet

Offener Punkt 2 der Aufnahme verlangte eine Zahl. Am 2026-09-01 über das ganze Frontend erhoben (je
`useAsync`-Aufrufstelle geprüft, ob der zugehörige `.error` in derselben Datei gerendert wird):

**64 von 105 Aufrufstellen rendern ihren Fehler, 41 nicht.**

Das ist zu breit für diese Story und wird **nicht** hier miterledigt — siehe Entscheidung 2.

## Die echte Lücke

`categories.data === null` trägt zwei Bedeutungen: „das Fach hat keine Arten" und „die Abfrage ist
gescheitert". Der Zustand, der beide trennt, existiert bereits (`categories.error`) — er wird nur nicht
gelesen.

## Entscheidungen

1. **Der Fehler wird als Prop durchgereicht, nicht in `CatalogAdmin` gerendert.** `CategorySection` bekommt
   ein `error: string | null`, `CategoryRows` ebenso. *Begründung:* Die Testkonvention dieser Datei prüft
   **Bausteine mit Props** und mockt `../lib/api` ausdrücklich nicht (`CatalogAdmin.test.tsx:6-9`); ein
   Fehler, der nur im netzhängenden `CatalogAdmin` sichtbar wird, wäre auf dieser Ebene gar nicht prüfbar.
   *Kosten:* Zwei Bausteine bekommen je einen Parameter mehr.
2. **Die übrigen 40 Stellen werden hier nicht angefasst**, sondern als eigene Story mit der gemessenen Zahl
   abgelegt. *Begründung:* 41 Stellen sind faktisch XL, und Freigabe 3 des Nachtlaufs verlangt dafür Teilen
   statt Bauen. Vor allem aber sind sie **nicht eine Klasse**: Ein Options-Lieferant für ein Auswahlfeld
   (`subjects`, `units`, `series` in den Filterleisten) verliert bei einem Fehler eine Auswahlmöglichkeit;
   hier verliert die Fläche ihren **Hauptinhalt**. Ein Sammel-Fix über beide wäre eine Regel ohne Gefälle.
   *Kosten:* Der Bereich trägt eine weitere offene Story, und die 40 Stellen bleiben bis dahin stumm.
3. **Der Leer-Zweig bleibt, wird aber nachgeschärft.** „Keine Arten" darf nur greifen, wenn wirklich
   geladen wurde. *Begründung:* Ohne das bliebe die Zweideutigkeit bestehen, nur mit einem Banner darüber.
   *Kosten:* Ein dritter sichtbarer Zustand auf der Fläche (Fehler / leer / Zeilen) statt zwei.
4. **Kein „Erneut versuchen"-Knopf in dieser Story.** *Begründung:* Der Fachwechsel im Auswahlfeld löst
   ohnehin eine neue Abfrage aus — der Ausweg existiert und ist bedienbar, anders als bei B-162, wo er
   nachweislich fehlt. Ein allgemeiner Wiederholmechanismus ist [B-183](B-183-gescheitertes-lesen-ohne-erneuten-versuch.md)
   und dort als `Wunsch` **gesperrt**. *Kosten:* Der Creator muss das Fach ab- und wieder anwählen.

## Akzeptanzkriterien

1. Scheitert das Laden der Arten, steht der Fehlertext auf dem Bildschirm — in allen drei Fach-Zuständen
   (eigenes Fach, fremdes Fach, Grundbestand).
2. Im Fehlerfall erscheint **nicht** der Zustand, der „dieses Fach hat keine Arten" bedeutet.
3. Lädt es erfolgreich und ist die Liste leer, verhält sich die Fläche wie heute.
4. Ein Komponententest deckt 1–3; die rote Probe nennt erwartete und gemessene Zahl.

## Schätzung

- **Größe: S.** Anker: eine Datei, zwei Bausteine je ein Prop, ein Renderzweig, dazu drei Testfälle.
  Vergleichbar mit B-002 (Hilfetext) plus einem Testblock.
- **Migration: nein**, **Vertragsbruch: nein** — `categories.error` existiert bereits in `useAsync`.
- **Testweg:** `frontend/src/vater/CatalogAdmin.test.tsx` (neuer `describe`-Block zu `CategorySection`),
  gefahren mit `npm run test` unter `frontend/`. **Kein** E2E: der Fall verlangt einen scheiternden Abruf,
  und den bricht man im Browser nur mit einem abgeschalteten Server — genau die Sorte Fehlzustand, die
  laut `pm-loop` Step 6 auf Bausteinebene gehört.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-154. Bleibt `unverifiziert`. Die Alt-Daten-Hälfte des
  Fundes ist bewusst **nicht** hier, sondern liegt schon als B-164 vor — vor dem Anlegen geprüft, um kein
  Duplikat zu erzeugen.
- 2026-09-01 · **`idee` → `ausformuliert`** (Nachtlauf). Ist-Stand selbst am Code nachgeprüft, alle fünf
  Fundorte mit Zeile belegt; die Behauptung der Aufnahme hält. `unverifiziert` entfernt. Offener Punkt 2
  ist **mit einer Zahl** beantwortet statt mit einer Vermutung: 41 von 105 `useAsync`-Aufrufstellen rendern
  ihren Fehler nicht.
- 2026-09-01 · **`ausformuliert` → `gegrillt`** (Nachtlauf, Freigabe 1 — `art: Defekt` ist autonom
  grillbar). Vier Entscheidungen mit Begründung und Kosten. Die eigene Empfehlung zu Punkt 3 der Aufnahme
  („zusammen mit B-164 bauen") ist **nicht** übernommen: B-164 ist eine eigene Aussage mit eigener
  gemessener Breite, und dieser Lauf baut sie nicht — ein Zusammenlegen hätte diese Story an eine fremde
  Fälligkeit gebunden.
- 2026-09-01 · **`gegrillt` → `geschaetzt`** (Nachtlauf). S, Testweg benannt (Komponententest, kein E2E,
  mit Begründung).
- 2026-09-01 · **`geschaetzt` → `in-arbeit`** (Nachtlauf, Sprint 1, Commit `e479d83`). `CategorySection`
  trägt `error` und rendert es mit `role="alert"`. **Rote Probe: erwartet 2 der 3 neuen Fälle rot,
  gemessen `2 failed | 17 passed`** von 19 (der dritte prüft Abwesenheit und ist heute korrekt grün).
  **Abweichung von Entscheidung 1, ausgesprochen statt verschwiegen:** Nur `CategorySection` bekommt das
  Prop, nicht zusätzlich `CategoryRows` — der Abschnitt rendert in allen drei Fach-Zuständen, das genügt.
  **Und der Testweg dieser Story war zu eng geschätzt:** „kein E2E, das bräuchte einen abgeschalteten
  Server" ist falsch — `page.route(…).abort()` kostet eine Zeile (`optionsliste-faellt-aus.spec.ts` sagt
  das im eigenen Kopfkommentar). Die Story hat darum zusätzlich `e2e/ausfall-wird-genannt.spec.ts`.
