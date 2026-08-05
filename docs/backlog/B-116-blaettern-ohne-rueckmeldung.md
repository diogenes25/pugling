---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Pager ohne Ladezustand, Blättern ohne Rückmeldung]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-08-05 auf die sechs ungeprüften Abnahmen der autonomen Runde
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-89]
wartet_auf: ""
nachgeschaut: ""
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

## Entscheidungen

1. **Das Signal sitzt im `Pager`, als `busy`-Prop, das beide Knöpfe sperrt und die Bereichsangabe
   zurückhält.** Begründung: der Pager ist der einzige Ort, der beides kennt (die geklickte Seite und
   dass sie noch nicht angekommen ist), und alle acht Bildschirme bekommen die Behebung damit auf einmal.
   Die Alternative — je Liste die Zeilen ausgrauen — müsste achtmal gebaut und achtmal richtig gemacht
   werden. **Kosten:** keine neue Komponente, aber jeder der acht Aufrufer muss `busy={loading}`
   ergänzen — ein Aufrufer, der es vergisst, bleibt beim alten (falschen) Verhalten, ohne dass ein Typfehler
   das anzeigt (die Prop ist optional).
2. **Die Bereichsangabe wird zurückgehalten, nicht nur der Klick gesperrt.** Begründung: die falsche
   `aria-live`-Meldung ist der eigentliche Schaden (Akzeptanzkriterium 3); ein gesperrter Knopf verhindert
   nur den Doppelklick (Kriterium 2), behebt aber nicht die Falschaussage währenddessen. **Kosten:** keine.
3. **Die Sortierung (`SortableTh`) bleibt außerhalb dieser Story.** Begründung: ob ein Sortierwechsel
   dieselbe Lücke hat, ist noch nicht gemessen (offener Punkt 3 des Ausformulierens), und diese Story fixt
   das *Blättern* — ein ungemessener Verdacht würde den Umfang verschieben, ohne dass die Größe dafür
   passt. **Kosten:** falls die Messung zutrifft, bleibt derselbe Fehler beim Sortieren vorerst bestehen;
   als eigene Idee vorgemerkt, sobald das Muster wirklich beobachtet wird.

## Akzeptanzkriterien

1. Ein Klick auf „Weiter ›"/„‹ Zurück" erzeugt eine sichtbare Rückmeldung, solange die Seite lädt.
2. Beide Knöpfe sind während des Ladens gesperrt; ein zweiter Klick kann keine Seite überspringen.
3. Die Bereichsangabe nennt erst dann die neue Seite, wenn ihre Zeilen im Bild sind — die
   `aria-live`-Meldung ist zu keinem Zeitpunkt falsch.
4. Ein `reload()` derselben Abfrage lässt die Zeilen weiter stehen (B-89 bleibt behoben, keine Rückkehr
   des Flackerns).

## Schätzung

**Größe: S** — eine Komponente (`Pager`) bekommt eine Prop und zwei Verzweigungen, acht Aufrufer bekommen
je eine Zeile (`busy={loading}`); kein Server-Anteil, kein Vertragsbruch, keine Migration.

**Risiken:** ein Aufrufer, der `busy` vergisst, bleibt beim alten Verhalten, ohne dass irgendetwas das
meldet (die Prop ist optional, damit bestehende Aufrufer nicht brechen) — das Nachsehen aller acht Stellen
ist darum Teil des Angriffsplans, nicht optional.

**Angriffsplan** (frontend-only):

1. `ListControls.tsx`, `Pager` — `busy?: boolean`-Prop: beide Knöpfe `disabled={busy || …bestehende
   Bedingung}`, die Bereichsangabe zeigt bei `busy` weiter die zuletzt bekannte Spanne statt der aus dem
   neuen `skip` berechneten.
2. Alle sieben Bildschirme, die `<Pager>` rendern (`ClozeTexts`, `VaterAnmerkungen`, `VaterClassTests`,
   `VaterExercises`, `VaterKonto`, `VaterLernstand`, `VaterVocab` — nachgezählt per Grep, die achte
   Fundstelle aus dem Ist-Stand ist `ListControls.test.tsx` selbst, kein Bildschirm) ergänzen
   `busy={<ihr loading-Flag>}` an ihrem `Pager`.
3. `ListControls.test.tsx` — neuer Fall: `busy` gesetzt → beide Knöpfe `disabled`, alte Bereichsangabe
   bleibt sichtbar.

**Testweg:** `frontend/src/components/ListControls.test.tsx` (Komponententest, reine Props — kein
gefälschtes `fetch` nötig, `frontend/CLAUDE.md`). Keine E2E: der Ladezustand selbst ist zeitkritisch und
auf einem lokalen Server kaum reproduzierbar zu erzwingen; die sieben Verdrahtungsstellen sind mechanisch
gleich und tragen kein eigenes Risiko, das ein Komponententest nicht schon deckt.

## Verlauf

- **2026-08-05** — gefunden in der **Nachschau** auf die sechs Abnahmen der autonomen Runde, die der
  Code-Review vom Folgetag nicht abgedeckt hatte (Protokoll `docs/pm-sitzung-2026-08-05.md`). Direkt
  ausformuliert: Ist-Stand mit `Datei:Zeile` belegt, und ausdrücklich mitgeprüft, **warum die übrigen 27
  Stellen des Rundumschlags in Ordnung sind** — sonst hätte die Story die halbe Oberfläche verdächtigt.
  `entgangen_bei: [B-89]`. Nicht geschätzt: offener Punkt 3 (gilt es auch fürs Sortieren?) ist eine
  Messung und verändert den Umfang.
- **2026-08-05 (Nachtlauf)** — **autonom gegrillt und geschätzt** (`art: Defekt`). Die Fundstelle
  `busy`-Prop im `Pager` bündelt alle sieben Aufrufer in einer Änderung; die Sortier-Frage (offener Punkt
  3) bleibt bewusst außerhalb, weil sie ungemessen ist. `groesse: S`, `wo: frontend`, keine Migration,
  kein Vertragsbruch. Beim Nachzählen der Fundstellen per Grep: es sind **sieben** echte Bildschirme, nicht
  acht — die achte Fundstelle aus dem Ist-Stand ist die Testdatei selbst.
- **2026-08-05 (Nachtlauf, Sprint 2)** — **gebaut wie geplant.** `Pager` bekommt `busy?: boolean`: ein
  `useRef` friert `{skip, take, total}` ein, solange `busy` gilt, und gibt erst nach `busy=false` wieder
  die aktuellen Props aus – sonst behauptete die `aria-live`-Zeile die neue Seite, bevor sie im Bild ist
  (Kriterium 3). Neuer Testfall in `ListControls.test.tsx`: `busy=true` hält beide Knöpfe `disabled` und
  die alte Spanne „1–25 von 60" fest, erst nach `busy=false` springt sie auf „26–50". Alle sieben
  Aufrufer (`ClozeTexts`, `VaterAnmerkungen`, `VaterClassTests`, `VaterExercises`, `VaterKonto`,
  `VaterLernstand`, `VaterVocab`) ergänzt um `busy={<ihr loading>}`. **Verifikation:** `npm run build`
  sauber, `npm test` → **153/153 grün** (152 + 1). `frontend-reviewer` lief erfolgreich, kein Blocker;
  bestätigte insbesondere, dass der `useRef` synchron beim Rendern mutiert (kein Seiteneffekt-Leck über
  Renderzyklen) und dass `useAsync`s `setData`/`setLoading(false)` gebündelt landen, also beim ersten
  Mount kein falsches Einfrieren entsteht. **Kein Browser-Rollengang möglich** (Chrome-Extension in
  dieser unbeaufsichtigten Sitzung nicht verbunden) und **kein HTTP-Äquivalent**: der Defekt ist reine
  Render-Zeitlichkeit ohne Server-Anteil. Ein Mensch sollte einmal im Vater-Web auf einer mehrseitigen
  Liste zügig „Weiter ›" klicken und prüfen, dass die Zahl erst mit den neuen Zeilen wechselt.
  **Eintrittsbedingung erfüllt, Stufe auf `abgenommen`.** Commit: `07eddc6`.
