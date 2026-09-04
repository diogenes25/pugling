---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-09-04, PM-Sitzung 04.09.2026]
---

# Nachtlauf 2026-09-04 — „Die Oberfläche sagt die Wahrheit über ihren eigenen Zustand"

Beauftragt für **drei benannte Stories**: B-160, B-188, B-209. Alle drei `art: Defekt` auf
`ausformuliert`, damit unter Freigabe 1 autonom grillbar. Ein Sprint, danach **Halt an der
Fünf-Funde-Grenze** (Freigabe 3) — Einzelheiten unter „Warum der Lauf hier endet".

## Sprint-Ziel, aus einer Rollensicht

> Kein Bildschirm behauptet etwas, das nicht stimmt: Der **Creator** sieht, warum er nicht löschen darf,
> der **Vater** erfährt, wenn eine Liste fehlt statt leer zu sein, und der **Sohn** sieht eine Bestätigung
> nur dort, wo er etwas gespeichert hat.

Drei Rollen, ein Faden. Die drei Defekte sind dieselbe Familie, die dieses Repo schon mehrfach bezahlt hat:
**eine Bedingung zieht zwei Situationen zusammen** — „keine da" und „nicht geladen", „gesperrt" und
„unauffindbar", „keine Meldung dabei" und „nichts zu tun".

## Was gebaut wurde

| Story | Kern | Belegebene |
| --- | --- | --- |
| [B-209](backlog/B-209-savednotice-raeumt-nie.md) | Die Bestätigung gehört zu einem Bildschirm, nicht zur Sitzung | Baustein (3 Fälle, vorher **keiner**) + E2E |
| [B-160](backlog/B-160-gesperrter-knopf-nennt-den-grund-nie.md) | `aria-disabled` statt `disabled`, sichtbarer Grund statt `title` | Baustein (4 Fälle, **erste** Testebene) + E2E |
| [B-188](backlog/B-188-wizard-verschweigt-drei-weitere-ausfaelle.md) | `ListenAusfall`: benannter Ausfall mit Weg zurück, an drei Stellen | Baustein (4 Fälle) + E2E |

**Zwei Entscheidungen, die am Code fielen statt nach Geschmack:**

- **B-209:** Die naheliegende Reparatur („kein `state.saved` ⇒ räumen") ist nachweislich falsch — der
  Baustein leert seinen Verlaufseintrag selbst und liefe sofort ein zweites Mal ohne `saved`. Die Meldung
  wäre im selben Atemzug gelöscht, in dem sie entstand. Fall 1 des Bausteintests bewacht genau das.
- **B-188:** Der offene Punkt „braucht es einen Wiederholweg?" war eine **Frage an den Code**, keine
  Entscheidung. `onGo` ruft nur `setStep`, `children` hängt an `[session!.id]` — der Stepper-Rückweg löst
  keine neue Abfrage aus. Der Kommentar zu B-162 sagt das in derselben Datei bereits.

**Beim Bauen gemessen und in die Aufteilung eingeflossen:** Playwrights Actionability wertet
`aria-disabled="true"` selbst als „nicht bedienbar". Die Aussage „ein Klick löst nichts aus" ist im E2E
darum gar nicht prüfbar — sie kommt nie so weit. Diese Hälfte trägt der Bausteintest; der E2E sagt
stattdessen, dass jedes Werkzeug mit Actionability abprallt. Zwei Ebenen, zwei verschiedene Aussagen.

## Rollengang (Step 6)

Kein Browser-Handgang möglich — keine Chrome-Anbindung in dieser Sitzung. Es greift der dokumentierte
Ersatz, und zwar in seiner **stärksten** Form: drei neue Playwright-Specs fahren die echte Oberfläche im
echten Browser gegen einen echten Server, je aus der Rolle, der die Story gehört.

| Rolle | Spec | Was sie fährt |
| --- | --- | --- |
| Sohn | `e2e/bestaetigung-reist-nicht-mit.spec.ts` | Interessen speichern → Shop → Weg → Basis, unter „Bewegung reduzieren" |
| Creator | `e2e/gesperrter-knopf-nennt-den-grund.spec.ts` | Verlags-Vokabular, gesperrter Knopf: Grund lesbar, fokussierbar, unbedienbar |
| Supervisor | `e2e/assistent-listen-fallen-aus.spec.ts` | Kinderliste abgeschossen → Meldung → „Erneut laden" → Liste wieder da |

Bei B-160 ist das kein Formalismus: Die **Ursache** war eine Browser-Eigenschaft (`disabled` empfängt keine
Pointer-Events und ist nicht fokussierbar), die happy-dom nur nachbildet.

## Verifikation

`tsc -b` sauber · `vitest run` **368/368** · `playwright test` **59/59** · `markdownlint-cli2` 0 Befunde.

Rote Proben, je erwartet/gemessen — vollständig in den drei Stories. Alle acht Messungen trafen die
Erwartung.

## Was der Reviewer fand

`frontend-reviewer`, **kein Blocker**, sechs Funde mit Code- oder Testwirkung (alle behoben und
nachgemessen) plus vier, die nur Doku bewegten.

Die zwei, die wirklich etwas geändert haben:

1. **Der Wiederholknopf hing sich unter dem Finger aus.** `useAsync.reload()` setzt `error` auf `null` und
   `loading` auf `true` im selben Schritt — ein Banner, das an `error` allein hängt, verschwindet genau im
   Moment des Klicks. Der Fokus fällt auf `<body>`, während der Wiederholung steht gar nichts da, und
   `disabled={laedt}` war toter Code. `ListenAusfall` hält den letzten Fehler jetzt fest; **erst dadurch**
   ist der gesperrte Zustand überhaupt erreichbar, und der Bausteintest, der ihn behauptete, sagt jetzt die
   Wahrheit. Zwei ältere Stellen mit derselben Bauform laufen als
   [B-216](backlog/B-216-wiederholknopf-haengt-sich-aus.md) weiter.
2. **Der gesperrte Knopf sank beim Klick weiterhin ein.** Ein echtes `disabled` matcht `:active` nie, ein
   `aria-disabled` schon — die neue Optik-Regel fing `transform`, nicht die Press-Schatten der vier
   Varianten. Er sah genau in dem Moment bedienbar aus, in dem er ablehnt.

Dazu: das Arten-Banner saß in Schritt 3 über der falschen Bedienung, ein Bausteinfall belegte nicht, was
sein Kommentar behauptete (Link statt echtem Zurück), und zwei E2E-Bezüge waren weiter als nötig.

**Nicht mitgebaut, als eigene Story:** [B-215](backlog/B-215-typ-manifest-ohne-fehlerkanal.md) — die
**vierte** Liste des Assistenten (`useExerciseTypes`) hat gar keinen Fehlerkanal; der Hook ist geteilt, ein
`error` dort träfe jeden Konsumenten und wäre ein Umbau außerhalb des Sprint-Diffs.

## Retrospektive

**Nachschau** (Pflichthandlung, erste Zeile): Nenner waren die beiden heute abgenommenen Stories B-210 und
B-153 — beide bis dahin unbeobachtet.

- **B-210:** Prüfpunkt war die eine Annahme, die beim Bauen nur *hergeleitet* war — dass der
  `ItemEditor`-Pfad kein Nachladen braucht. Gegen den **erfassten Vertrag** geprüft statt gegen den
  Controller: `docs/api-examples/catalog.md:172-178` zeigt `"refs": null, "items": []` in der Antwort einer
  Vokabelübung. Die Annahme trägt. **Kein Defekt.**
- **B-153:** Prüfpunkt war die Gegenzählung, wo `AssetThumb` sonst noch neben `MediaSearch` steht.
  Ergebnis: **zwei** Aufrufer, und der zweite (`ExerciseSharingPanels`, Titelbild) trägt dieselbe
  Zweideutigkeit. Heute greift dort kein Test per Alt-Text — es flackert also nichts, aber die Falle ist
  scharf für den nächsten, der einen schreibt. Statt einer Story steht die Warnung jetzt an der Quelle
  (`MediaPickers.tsx`, Doku an `AssetThumb`). **Kein Defekt in abgenommener Arbeit.**

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

> **Wer einen Zustand aus einem Hook liest, um daran eine Fläche zu zeigen, prüft zuerst, welche Zustände
> der Hook überhaupt *gleichzeitig* erzeugen kann.** `useAsync` setzt beim `reload` `error → null` und
> `loading → true` im selben Schritt; „Fehler steht und Abfrage läuft" gibt es darum nicht, solange die
> Fläche an `error` allein hängt. Drei Flächen im Repo haben daraus ein Element gebaut, das im Moment
> seines Klicks verschwindet — und eine Sperre, die nie greift.

Ziel wäre `frontend/CLAUDE.md`, neben der bestehenden `useAsync`-Falle. **Landen oder verwerfen ist deine
Entscheidung.** Zusammen mit den vier noch offenen Vorschlägen aus früheren Nächten sind es jetzt fünf.

## Warum der Lauf hier endet

Freigabe 3 zieht die Grenze bei **fünf** Review-Funden je Sprint, deren Behebung Code oder Tests ändert.
Dieser Sprint hatte **sechs**. Alle sind behoben und nachgemessen, das Increment ist vollständig — aber die
Grenze misst nicht die Qualität des Ergebnisses, sondern die **Dichte der Nacharbeit**, und die ist
ablesbar überschritten. Der Lauf endet damit nach Sprint 1; ein Sprint 2 beginnt nicht.

Das ist die zweite Nacht in Folge, die so endet (03.09.: neun Funde). Ob die Grenze für Sprints dieser
Bauart — drei kleine Frontend-Defekte, viel neue A11y-Fläche, drei neue Testdateien — richtig gesetzt ist,
ist eine Frage an dich, nicht an den Lauf.

## Für den Morgen

1. **Retro-Vorschlag** oben: landen oder verwerfen (dazu vier ältere).
2. **Neue Stories:** [B-215](backlog/B-215-typ-manifest-ohne-fehlerkanal.md),
   [B-216](backlog/B-216-wiederholknopf-haengt-sich-aus.md) — beide P3, beide belegt.
3. **Push:** nichts ist gepusht, das bleibt bei dir.
