---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [Pager ohne Ladezustand, Blättern ohne Rückmeldung]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-05 auf die sechs ungeprüften Abnahmen der autonomen Runde
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-89]
---

# B-116 · Beim Blättern gibt es keine Rückmeldung mehr — und der Pager meldet eine Seite, die noch nicht da ist

B-89 hat richtig behoben, dass ein `reload()` die ganze Liste aushängt und aufgeklappte Bereiche
wegnimmt. Dabei ist eine zweite Situation mitgefangen worden, die sich nur *technisch* gleich anfühlt:
das **Blättern**. Dort war der Ladehinweis kein Flackern, sondern die einzige Rückmeldung — und er ist
jetzt weg.

## User Story

Als Vater möchte ich beim Klick auf „Weiter ›" sehen, dass etwas passiert, damit ich nicht auf eine
Tabelle starre, die noch die alte Seite zeigt, während die Seitenanzeige schon die neue nennt.

## Ist-Stand am Code

- `frontend/src/components/ListControls.tsx:14-35` — `Pager` kennt **keinen** Ladezustand: kein `busy`/
  `loading`-Prop, beide Knöpfe bleiben während eines laufenden Abrufs bedienbar, und die Bereichsangabe
  („26–50 von 312") wird aus dem **neuen** `skip` berechnet und trägt `aria-live="polite"`.
- `b9b8279` (B-89) hat 29 Stellen von `loading ? Spinner : Zeilen` auf
  `loading && data === null ? Spinner : Zeilen` umgestellt. `useAsync` setzt bei jedem Dep-Wechsel
  `loading = true`, lässt `data` aber stehen (`frontend/src/lib/useAsync.ts:24-37`) — bei einem
  Seitenwechsel ist `data` also die **alte Seite**, und die Bedingung ist falsch.
- Acht Bildschirme rendern einen `Pager`, und alle acht tragen die neue Bedingung: `ClozeTexts`,
  `VaterAnmerkungen`, `VaterClassTests`, `VaterExercises`, `VaterKonto`, `VaterLernstand`, `VaterVocab`
  (plus `ListControls.test.tsx`). Bei zwei davon steht der Offset nachweislich in den `useAsync`-Deps:
  `VaterKonto.tsx:45` (`[childId, acctSkip]`) und `VaterClassTests.tsx:50` (`[childId, skip]`).
- **Warum die übrigen Fälle des Rundumschlags in Ordnung sind** (mitgeprüft, damit die Story nicht zu weit
  greift): bei einem Wechsel der *Auswahl* montiert das Repo die Unterkomponente per `key=` neu — die
  Hooks starten dann mit `data === null` und der Spinner erscheint korrekt (`VaterKonto.tsx:35`,
  `VaterShop.tsx:364`, `VaterClassTests.tsx:41`, `VaterRewards.tsx:58-59`; in `VaterClassTests` steht die
  Begründung sogar als Kommentar daneben). Blättern ist der eine Fall **ohne** Remount.

## Die echte Lücke

Der Zustand `loading && data !== null` trägt zwei Bedeutungen, und B-89 hat beide gleich behandelt:

1. **Dieselbe Abfrage wird wiederholt** (`reload()` nach einer Mutation) — hier ist Stehenlassen richtig,
   und genau das war B-89s Auftrag.
2. **Eine andere Abfrage läuft** (nächste Seite) — hier zeigen die stehengebliebenen Zeilen etwas, das
   nicht mehr zur Anzeige darüber gehört, und es gibt **kein einziges** Signal, dass etwas unterwegs ist:
   kein Spinner, kein gesperrter Knopf, keine Andeutung.

Zwei konkrete Folgen: die `aria-live`-Zeile **sagt eine Unwahrheit** („26–50 von 312", während 1–25 im
Bild stehen) — und weil „Weiter ›" bedienbar bleibt, springt ein zweiter Klick zwei Seiten weiter, ohne
dass der Nutzer je eine Rückmeldung auf den ersten bekam. Auf einer langsamen Verbindung ist das kein
Millisekunden-Effekt.

Dieselbe Fehlerklasse wie [B-114](B-114-showboth-position-unspielbar.md) (eine Bedingung, die zwei
Situationen zusammenzieht) und [B-111](B-111-verlauf-luegt-im-fehlerfall.md) (eine Anzeige, die etwas
behauptet, das sie nicht belegen kann).

## Offene Punkte

1. **Wo sitzt das Signal — im `Pager` oder in der Liste?** Empfehlung: **im `Pager`**, als `busy`-Prop, das
   beide Knöpfe sperrt und die Bereichsangabe zurückhält, bis die Seite da ist. Begründung: der Pager ist
   der einzige Ort, der beides kennt (die geklickte Seite und dass sie noch nicht angekommen ist), und alle
   acht Bildschirme bekommen die Behebung damit auf einmal. Die Alternative — je Liste die Zeilen ausgrauen —
   müsste achtmal gebaut und achtmal richtig gemacht werden.
2. **Muss die Bereichsangabe zurückgehalten werden, oder genügt das Sperren?** Empfehlung: **zurückhalten**
   (die alte Angabe stehen lassen, bis die neuen Zeilen da sind), denn die falsche `aria-live`-Meldung ist
   der eigentliche Schaden — ein gesperrter Knopf verhindert nur den Doppelklick.
3. **Gilt derselbe Fall für die Sortierung** (`SortableTh` ändert `sort`/`dir` und damit die Deps)?
   Empfehlung: **erst messen**, dann in dieselbe Behebung aufnehmen — vermutlich ja, mit demselben Muster.

## Akzeptanzkriterien

1. Ein Klick auf „Weiter ›"/„‹ Zurück" erzeugt eine sichtbare Rückmeldung, solange die Seite lädt.
2. Beide Knöpfe sind während des Ladens gesperrt; ein zweiter Klick kann keine Seite überspringen.
3. Die Bereichsangabe nennt erst dann die neue Seite, wenn ihre Zeilen im Bild sind — die
   `aria-live`-Meldung ist zu keinem Zeitpunkt falsch.
4. Ein `reload()` derselben Abfrage lässt die Zeilen weiter stehen (B-89 bleibt behoben, keine Rückkehr
   des Flackerns).

## Verlauf

- **2026-08-05** — gefunden in der **Nachschau** auf die sechs Abnahmen der autonomen Runde, die der
  Code-Review vom Folgetag nicht abgedeckt hatte (Protokoll `docs/pm-sitzung-2026-08-05.md`). Direkt
  ausformuliert: Ist-Stand mit `Datei:Zeile` belegt, und ausdrücklich mitgeprüft, **warum die übrigen 27
  Stellen des Rundumschlags in Ordnung sind** — sonst hätte die Story die halbe Oberfläche verdächtigt.
  `entgangen_bei: [B-89]`. Nicht geschätzt: offener Punkt 3 (gilt es auch fürs Sortieren?) ist eine
  Messung und verändert den Umfang.
