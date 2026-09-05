---
tags: [typ/protokoll, bereich/pm]
aliases: [Sprint 2026-09-05, Ein Meldungsplatz]
---

# Sprint 2026-09-05 — „Eine Rückmeldung gehört an einen Platz, der sie überlebt"

Fortsetzung des Laufs vom 2026-09-04 auf ausdrückliche Anweisung, nachdem dieser an der
Fünf-Funde-Grenze geendet hatte. Ein Sprint über drei Defekte, alle `art: Defekt` auf `ausformuliert`,
autonom gegrillt unter Freigabe 1.

## Sprint-Ziel, aus einer Rollensicht

> Der **Creator** liest nach jeder Handlung genau **eine** Rückmeldung: Sie steht lange genug, sie gehört
> zu dem, was er gerade getan hat, und beim Löschen gibt es sie überhaupt.

Die drei Stories sind dieselbe Wurzel aus drei Richtungen — und derselbe Faden wie B-209 am Vortag:

| Story | Richtung |
| --- | --- |
| [B-187](backlog/B-187-unitform-verliert-seine-bestaetigung.md) | Die Meldung lebt in dem Element, dessen Verschwinden sie bestätigt |
| [B-200](backlog/B-200-drei-stumme-erfolgspfade.md) | Es gibt gar keine Meldung |
| [B-212](backlog/B-212-fuenfter-meldungsplatz-in-lehrwerke.md) | Es gibt zwei Plätze, und keiner kann den anderen räumen |

## Der Kern: ein Schnitt an etwas Ablesbarem

Bisher galt in `VaterLehrwerke.tsx`: **„nur der Erfolg reist"**. Jetzt gilt: **alles, was vom Server
kommt, reist** — am Formular bleibt allein, was der Bildschirm **selbst** festgestellt hat.

Der Unterschied ist keine Geschmacksfrage, sondern eine Eigenschaft der beiden Sorten: Bei einer
Eingabeprüfung *ist nichts passiert*, sie kann ihr Formular gar nicht überdauern. Jede Server-Antwort
gehört dagegen zu einer Handlung, die die Zeile aus der Filterung werfen oder das Formular schließen kann.
Der geteilte Weg heißt `nachObenMelden` und ersetzt vier handgeschriebene Kaskaden.

Die in B-212 erwogene Ausnahme („ein Formular hat, anders als eine Zeile, einen eigenen Fokusbereich")
trägt nicht: Der Fehler war von **keiner** anderen Handlung mehr erreichbar — und genau das *war* der
Defekt.

**B-187 wurde dabei gegen seine eigene Empfehlung entschieden.** Die Story schlug einen Meldungsplatz an
`UnitPanel` vor; das wäre der vierte gewesen und hätte den Widerspruch erzeugt, den B-212 im selben Sprint
auflöst. Der berechtigte Einwand („am Seitenrand weit weg") ist stattdessen dort beantwortet, wo er
hingehört — siehe unten.

## Verifikation

`tsc -b` sauber · `vitest run` **368/368** · `playwright test` **63/63** · `markdownlint-cli2` 0 Befunde.

Fünf rote Proben, alle erwartet/gemessen deckungsgleich — und jede fällt **an der Zeile, die ihren Fall
trägt**, nicht irgendwo davor. Tabellen in den drei Stories.

## Was der Reviewer fand

`frontend-reviewer`, **kein Blocker**, **acht** Funde mit Code- oder Testwirkung, alle behoben und
nachgemessen. Die drei, die etwas Substanzielles geändert haben:

1. **Ich hatte eine Verschlechterung eingebaut.** Der eine Meldungsplatz steht *unter* der Tabelle, und die
   Liste hat kein Paging: Bei dreißig Reihen liegt ein Fehler aus Zeile 3 außerhalb des Sichtfelds — der
   Knopf entgraut, sonst passiert sichtbar nichts. Das ist der stumme Pfad aus B-200, neu erzeugt durch die
   Reparatur von B-212. Behoben mit `scrollIntoView({ block: "nearest" })`, **nur im Fehlerfall**: Eine
   Bestätigung, die man nicht sieht, ist verschmerzbar; ein Fehler nicht. Bemerkenswert: **Die E2E kann das
   nicht prüfen** — Playwrights `toBeVisible()` verlangt eine Bounding-Box, nicht den Viewport.
2. **Der Schnitt galt nicht „für die ganze Datei".** `NewSeries` trug weiter zwei eigene Plätze — mein
   korrigierter Kommentar behauptete also erneut zu viel, exakt derselbe Fehler wie bei B-204, nur eine
   Runde später. Jetzt melden auch diese beiden nach oben.
3. **Zwei von drei B-200-Pfaden waren unverifiziert.** „Rote Probe erwartet 1 / gemessen 1" betraf nur den
   Unit-Pfad; dass die anderen beiden funktionieren, wäre eine Behauptung geblieben. Dafür ist
   `e2e/loeschen-meldet-sich.spec.ts` entstanden (rote Probe: erwartet 2, gemessen 2).

Dazu: der tote `!onMeldung`-Zweig (jetzt Pflicht-Prop), ein synchroner Wurf, der vollständiges Schweigen
erzeugt hätte, zwei Anführungszeichen-Glyphen für denselben Namen vier Zeilen auseinander, ein fehlendes
`onMeldung(null)` in zwei `remove()`, eine Spec-Zusicherung, die beim zweiten Aufruf den Ausgangszustand
prüfte, und ein Spec-Kommentar mit der richtigen Reihenfolge aus dem falschen Grund.

## Rollengang (Step 6)

Kein Browser-Handgang möglich (keine Chrome-Anbindung); es greift der dokumentierte Ersatz in seiner
stärksten Form — zwei neue Playwright-Specs fahren die echte Oberfläche gegen einen echten Server:
`ein-meldungsplatz.spec.ts` (Creator: Unit speichern, Unit löschen, Fehler an Reihe A gegen Erfolg an
Reihe B) und `loeschen-meldet-sich.spec.ts` (Buch am Kind, Fachlehrer).

## Retrospektive

**Nachschau** (Pflichthandlung, erste Zeile): Nenner war der Sprint vom 2026-09-04 (B-160, B-188, B-209).
Drei benannte Prüfpunkte statt „sieht sauber aus":

- **B-209:** Die dokumentierte Annahme („der Pfadvergleich sieht `search` nicht — heute folgenlos")
  gegengezählt: **kein** Arcade-Bildschirm benutzt `useSearchParams` oder `location.search`. Trägt.
- **B-160:** `aria-disabled` kommt weiterhin nur an dieser einen Stelle vor (keine CSS-Kollision), und
  **alle vier** `.btn…:active`-Regeln tragen die Ausnahme.
- **B-188:** Jedes der drei `ListenAusfall` bezieht `fehler`/`laedt`/`onRetry` aus **derselben**
  `useAsync`-Instanz — genau die Deckungslücke, die der Reviewer benannt hatte („ein vertauschtes Paar fiele
  niemandem auf"), von Hand nachgesehen. Kein vertauschtes Paar.

**Kein Defekt gefunden.**

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

> **Wer einen zweiten Meldungsplatz einführt, muss sagen, wer ihn räumt.** Ein Platz, den nur die
> Komponente räumen kann, in der er steckt, wird von jeder Handlung daneben überlebt — und dann stehen zwei
> Meldungen zu zwei Handlungen in einem Blickfeld. Zulässig ist ein lokaler Platz nur für das, was der
> Bildschirm **selbst** festgestellt hat: Dabei ist nichts passiert, was ihn überdauern könnte.

Ziel wäre `frontend/CLAUDE.md`. Vier Stories in Folge (B-151, B-173, B-197/B-204, B-212) haben dieselbe
Frage einzeln beantwortet; das ist der Punkt, an dem eine Regel billiger ist als die fünfte Herleitung.
**Landen oder verwerfen ist deine Entscheidung** — zusammen mit den fünf offenen Vorschlägen sind es sechs.

## Warum der Lauf hier endet

Freigabe 3 zieht die Grenze bei fünf Review-Funden je Sprint mit Code- oder Testwirkung. Dieser Sprint
hatte **acht** — nach neun (03.09.) und sechs (04.09.) der dritte in Folge über der Linie. Alle sind
behoben und nachgemessen; das Increment ist vollständig.

**Das Muster ist inzwischen aussagekräftiger als die einzelne Zahl.** Auffällig ist nicht die Menge,
sondern ihre Art: Ein Fund war eine Verschlechterung, die ich selbst eingebaut hatte, einer eine
Behauptung, die zu weit ging, einer eine Messung, die weniger belegte als sie sagte. Das sind keine
Flüchtigkeitsfehler, sondern der Preis dafür, dass diese Sprints jeweils eine **Regel** ändern statt eine
Stelle. Ob fünf dafür die richtige Grenze ist, ist eine Frage an dich.

## Für den Morgen

1. **Retro-Vorschlag** oben: landen oder verwerfen (dazu fünf ältere).
2. **Push:** nichts ist gepusht, das bleibt bei dir.
