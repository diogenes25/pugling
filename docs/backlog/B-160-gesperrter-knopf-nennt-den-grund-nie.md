---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [title am disabled-Knopf erscheint nie, Verlagssperre ohne Grund]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau im Nachtlauf 2026-08-12 (zu B-150)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-150]
---

# B-160 · Der gesperrte Löschen-Knopf nennt seinen Grund nie — der `title` erscheint nicht

[B-150](B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md) hat den Löschen-Knopf am Verlag
gesperrt, damit der Creator nicht in einen `409` läuft, und den Grund in ein `title`-Attribut geschrieben.
Das Attribut sitzt **auf dem `disabled`-Knopf** — dort zeigt Chromium und WebKit keinen Tooltip.

## User Story

Als *Creator* möchte ich an einem gesperrten Löschen-Knopf lesen können, **warum** er gesperrt ist, damit
ich nicht rate, statt einer Fehlermeldung nachzugehen, die ich absichtlich nicht mehr bekomme.

## Ist-Stand am Code

- `frontend/src/vater/PublisherAdmin.tsx:116-121`: derselbe Knopf trägt `disabled={busy || locked}` **und**
  `title={locked ? "Gesperrt: An diesem Verlag hängen Reihen, die dir nicht gehören …" : undefined}`.
- Ein `disabled` Form-Control empfängt in Chromium und WebKit **keine** Pointer-Events; der native Tooltip
  wird darum nie ausgelöst. Nur Firefox zeigt ihn.
- Der Kommentar darüber (`:113-115`) behauptet ausdrücklich das Gegenteil: „der `title` sagt, warum, statt
  es dem Fehlversuch zu überlassen." Er begründet also eine Wirkung, die nicht eintritt.
- **Sichtbar bleibt nur die Tatsache, nicht die Folge:** `:106` zeigt „davon N fremd". Dass daraus „nicht
  löschbar" folgt, sagt niemand.
- **Es ist die einzige Stelle im ganzen Frontend, die `disabled` mit einem erklärenden `title` kombiniert**
  (nachgezählt über `frontend/src/`), und `aria-disabled` kommt **nirgends** vor — es gibt also kein
  Vorbild, an dem das Muster schon aufgefallen wäre.
- **Null automatisierte Deckung** für die betroffenen Akzeptanzkriterien von B-150: es gibt keinen
  `PublisherAdmin.test.tsx`, und keine E2E-Spec fährt den Verlags-Löschpfad.

## Die echte Lücke

B-150s Akzeptanzkriterium 2 hat zwei Hälften — „der Knopf ist gesperrt" **und** „nennt den Grund". Die
erste ist gebaut, die zweite nur scheinbar. Die Story hat ihren eigenen Ausfall dabei vorhergesagt: ihr
Verlauf notiert „Was er *nicht* ersetzt: dass jemand den gesperrten Knopf im Browser gesehen hat" — genau
der Rollengang, der es gezeigt hätte, fand nicht statt, ein Reviewer lief nicht, und eine Testebene gibt es
nicht.

## Offene Punkte

1. **Welche Bauform trägt den Grund?** Empfehlung: den Knopf **nicht** `disabled` machen, sondern
   `aria-disabled="true"` plus einen sichtbaren Satz neben der Zahl „davon N fremd" — dann ist der Grund
   ohne Hover lesbar (auch auf dem Handy, wo es kein Hover gibt) und der Knopf bleibt fokussierbar, was
   Screenreader-Nutzern die Existenz der Sperre überhaupt erst verrät. Kosten: `onClick` muss den Klick
   dann selbst verwerfen, sonst läuft er in den `409`, den B-150 abschaffen wollte.
2. **Oder einfacher: den Grund immer zeigen?** Die Zeile „davon N fremd" steht ohnehin da; sie könnte
   „davon N fremd – darum nicht löschbar" heißen. Empfehlung: das ist die billigste Hälfte und sollte in
   jedem Fall passieren, unabhängig von Punkt 1.
3. **Gilt das Muster noch woanders?** Nachgezählt: nein, diese Stelle ist die einzige. Ein Wächter wäre
   damit ein Tor für einen Einzelfall — erst messen, ob `disabled` + `title` je wieder entsteht.
4. **Testebene:** Empfehlung: ein `PublisherAdmin.test.tsx` mit den zwei Zuständen (gesperrt/nicht), Vorbild
   `CatalogAdmin.test.tsx` aus B-154. Ein E2E wäre teurer und deckte den A11y-Teil nicht besser ab.

## Akzeptanzkriterien (Entwurf)

1. Bei `foreignSeriesCount > 0` ist der Grund **ohne Hover** lesbar.
2. Der Knopf löst in diesem Zustand keine Anfrage aus (kein `409`).
3. Ein Screenreader erfährt, dass der Knopf gesperrt ist, und warum.
4. Bei `foreignSeriesCount === 0` ist alles unverändert.
5. Ein Komponententest deckt beide Zustände; die rote Probe belegt, dass er den heutigen Stand fängt.

## Verlauf

- **2026-08-12** — angelegt aus der **Nachschau** des Nachtlaufs (Retrospektive Sprint A), zur am
  2026-08-11 abgenommenen [B-150](B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md).
  `entgangen_bei: [B-150]` — der Defekt liegt **innerhalb** ihres Diffs. Selbst am Code nachgeprüft
  (`PublisherAdmin.tsx:116-121`, plus die Gegenzählung, dass `aria-disabled` im Frontend nirgends vorkommt).
