---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/lehrplan, rolle/supervisor]
aliases: [Alle wählen wählt Unsichtbares, Auswahl überlebt den Filterwechsel]
status: ausformuliert
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau im Nachtlauf 2026-08-12 (zu B-18)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-18]
---

# B-161 · „Alle wählen" wählt bis zu 400 Übungen, die der Vater nie sieht und nicht abwählen kann

[B-18](B-18-auto-lehrplan-generator.md) hat den Assistenten um drei Filter und einen „Alle wählen"-Knopf
erweitert, der bis zu **500** Treffer wählt. Gerendert wird aber nur die geladene erste Seite (≤100). Bis zu
400 gewählte Übungen sind damit unsichtbar — und weil die Auswahl keinen Filterwechsel überlebt-ihn-nicht
kennt, kann der Plan Positionen enthalten, die der gezeigte Filter ausschließt.

## User Story

Als *Vater* möchte ich, dass die Zahl „gewählt" und die Liste vor mir dasselbe meinen, damit ich nicht einen
Lehrplan mit Übungen anlege, die ich nie gesehen und nicht gewollt habe.

## Ist-Stand am Code

Zwei zusammenhängende Hälften, beide in `frontend/src/vater/VaterWizard.tsx`:

**1. Gewählt, aber nicht erreichbar.**

- `selectAll` (`:203-213`) fragt bei mehr Treffern als geladen einmal mit `take: 500` nach und schreibt
  **alle** zurückgegebenen Ids in `selected` (`:213`).
- Gerendert werden ausschließlich `filteredExercises` = `exercises.data?.items` (`:154`, `:440`) — die
  geladene erste Seite.
- `toggle` (`:171-173`) existiert nur an einer gerenderten Zeile, und es gibt **keinen** „Auswahl
  leeren"-Knopf (nachgezählt: `setSelected` steht nur an `:172`, `:204`, `:213`, `:217`).
- **Vor diesem Diff war `selected` immer eine Teilmenge des Gerenderten** — jede gewählte Id war
  erreichbar. Genau diese Invariante bricht der Diff.

**2. Die Auswahl überlebt jeden Filterwechsel.**

- Kein `setSelected([])` bei einer Filteränderung — die vier Fundstellen oben sind alles.
- Folge, am Bildschirm gleichzeitig sichtbar: `:399` zeigt „({selected.length} gewählt)", `:424` zeigt
  „{filteredExercises.length} passende Übungen". Nach „500 wählen → Typ-Filter enger stellen" steht dort
  „5 passende Übungen" neben „(500 gewählt)".
- `canAdvance` (`:231`) prüft nur `selected.length === 0`, lässt also weiter; `finish` (`:267`) schickt
  `exerciseIds: selected`, und `wizardFinish.ts:101-113` legt **500 Positionen** an.
- `:457` sagt dazu „gilt für alle {selected.length} Positionen" — die Zahl ist korrekt, die Auswahl dahinter
  nicht das, was der Vater vor sich hatte.

## Die echte Lücke

`selected` trägt zwei Bedeutungen in einem Zustand: „aus der aktuellen Trefferliste gewählt" und „irgendwann
früher gewählt". Das ist wörtlich die Fehlerfamilie, die
[nachtlauf.md](../nachtlauf.md) für dieses Repo gemessen hat — *eine Bedingung, die zwei Situationen
zusammenzieht*, wie `Testable` als Typ- statt Tages-Aussage (B-114) oder „leer" für „nichts gekauft" *und*
„Laden gescheitert" (B-111).

**Alt oder neu?** Die Wurzel ist älter: `contentSearch` konnte die Auswahl schon überleben. **Neu und in
B-18s Diff** ist (a) dass `selected` Ids enthalten kann, die nie gerendert wurden — der Rücknahmeweg fehlt
also ganz —, und (b) die Anhebung von 100 auf 500 plus drei neue Filter, die zum Nachschärfen einladen.
Damit wurde aus einem Randfall der Regelfall. Die Abnahme war dem Fund sehr nah: der Reviewer bemerkte, dass
`selected` jetzt über die Seite hinausreicht, reparierte aber nur das *Metadaten*-Nachschlagen (`gesehen`)
und fragte nicht, ob der Vater die Auswahl noch sehen und zurücknehmen kann.

## Offene Punkte

1. **Auswahl bei Filterwechsel leeren oder behalten?** Empfehlung: **leeren**, mit sichtbarem Hinweis. Sie
   zu behalten wäre nur richtig, wenn man sie auch sehen könnte — und das ist Punkt 2. Kosten: wer den
   Filter nur verfeinern wollte, verliert seine Auswahl; das ist der billigere Fehler gegenüber einem Plan
   mit ungesehenen Positionen.
2. **Wie wird eine Auswahl jenseits der Seite überhaupt bedienbar?** Empfehlungen, in dieser Reihenfolge:
   ein „Auswahl leeren"-Knopf (billig, löst den Sackgassen-Fall sofort); und die gewählten Ids, die nicht in
   der Liste stehen, entweder als eigene Zeilen nachladen oder — deutlich einfacher — `selectAll` auf die
   **geladene** Seite begrenzen und den Rest ehrlich als Kappung anzeigen (`TruncationHint` existiert
   schon).
3. **Ist P1 richtig?** Empfehlung: ja. Es entstehen ungewollte Pflichten am Kind, und eine Position mit
   Pflichtziel kostet bei Nichterfüllung **Münzen** (`PenaltyCoins`) — der Fehler wirkt also nicht nur auf
   den Vater, sondern aufs Kind. Das ist derselbe Grund, aus dem B-114 P1 war.
4. **Deckt ein Komponententest das, oder braucht es den E2E?** Zu klären: `wizardSearch`/`wizardFinish` sind
   schon als reine Logik getestet; der Zustandsübergang „wählen → Filter ändern" liegt aber im Bildschirm.

## Akzeptanzkriterien (Entwurf)

1. Nach einer Filteränderung enthält `selected` keine Übung, die der neue Filter ausschließt.
2. Jede gewählte Übung ist entweder sichtbar oder es gibt einen Weg, die Auswahl zurückzunehmen.
3. Die Zahl „N gewählt" und die Liste widersprechen sich nie ohne einen Satz, der den Unterschied erklärt.
4. Der Plan enthält am Ende genau die Positionen, die der Vater in der Liste gewählt hat.
5. Ein Test fährt „alle wählen → Filter verengen → abschließen" und belegt, dass keine ungesehene Position
   entsteht; die rote Probe zeigt, dass er den heutigen Stand fängt.

## Verlauf

- **2026-08-12** — angelegt aus der **Nachschau** des Nachtlaufs (Retrospektive Sprint A), zur am
  2026-08-11 abgenommenen [B-18](B-18-auto-lehrplan-generator.md). `entgangen_bei: [B-18]` — die Wurzel ist
  älter, aber erreichbar und zum Regelfall gemacht hat sie **dieser** Diff (100 → 500, drei neue Filter,
  Auswahl jenseits des Gerenderten). Selbst am Code nachgeprüft: alle vier `setSelected`-Fundstellen
  einzeln, `filteredExercises` als einzige Renderquelle, und die beiden Zahlen an `:399`/`:424`.
