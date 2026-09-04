---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Bestätigung reist durch die Arcade, SavedNotice ohne Räumen]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-03 zu B-50 (Nachtlauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-50]
wartet_auf: ""
---

# B-209 · „Gespeichert" bleibt über jedem folgenden Bildschirm der Arcade stehen

## User Story

Als **Sohn** möchte ich, dass eine Bestätigung zu dem Bildschirm gehört, auf dem ich etwas gespeichert
habe — und nicht über Shop, Weg und Konto weiter mitreist, als hätte ich dort gerade etwas getan.

## Ist-Stand am Code

`frontend/src/components/SavedNotice.tsx:23-33`: Der Effekt setzt `message`, wenn `location.state.saved`
ein String ist, und **kehrt sonst früh zurück, ohne zu räumen**. Der Baustein sitzt in der Shell **über**
den `<Routes>` (`SohnApp.tsx:114`), bleibt also die ganze Sitzung montiert.

Der Weg, auf dem es auftritt: Kind speichert seine Interessen → Sprung nach `/sohn` (Banner richtig) →
tippt auf „Shop", „Weg" oder „Konto". Die Meldung „Gespeichert – deine Übungen passen sich an." steht
danach über **jedem** Bildschirm bis zum Abmelden.

**Dieselbe Fehlerfamilie, die dieses Repo schon dreimal bezahlt hat:** eine Bedingung zieht zwei
Situationen zusammen — „diese Navigation bringt keine Meldung mit" und „es ist nichts zu tun".

**Warum es keiner Prüfung auffiel:** `frontend/src/components/` enthält **keine** `SavedNotice.test.tsx`,
und `e2e/interessen.spec.ts` prüft die Meldung nur auf `/sohn` selbst — also genau dort, wo sie richtig
ist.

## Offene Punkte

1. ~~Räumen beim Navigieren ohne `saved`, oder die Meldung nach einer Frist verfallen lassen?~~ →
   Entscheidung 1.
2. ~~Gehört der Fall an den Baustein oder in einen E2E?~~ → Entscheidung 2.

## Entscheidungen

1. **Geräumt wird beim Wechsel des Pfades, nicht beim Fehlen von `saved`.** Die naheliegende Fassung
   („kein `saved` ⇒ räumen") ist **falsch**, und zwar nicht knapp: Der Effekt leert den Verlaufseintrag
   gleich nach dem Setzen selbst (`navigate(pathname, { replace: true, state: null })`) und läuft dadurch
   sofort ein zweites Mal ohne `saved` — die Meldung wäre im selben Atemzug gelöscht, in dem sie entstand.
   Ein `gezeigtAuf`-Ref hält darum den Pfad, zu dem die stehende Meldung gehört. **Kosten:** ein Ref mehr,
   und eine Annahme, die benannt sein muss — der Vergleich sieht `search`/`hash` nicht. Heute folgenlos
   (kein Arcade-Bildschirm trägt Query-Parameter), als Kommentar am Code festgehalten.
   Eine Frist wurde verworfen: das wäre eine zweite Zeitachse neben der Navigation und im Test nur mit
   Warten zu fassen.
2. **Beides.** Der Baustein trägt die Regel (er hatte bis hierher **keinen** Test), der E2E den Weg, auf
   dem sie auffiel. **Kosten:** zwei Ebenen statt einer — gerechtfertigt, weil die Regel zwei einander
   widersprechende Hälften hat (räumen, aber nicht beim eigenen `state: null`) und der Weg durch die
   Arcade-Shell nur im Browser existiert.

## Rote Proben (erwartet / gemessen)

| Probe | erwartet | gemessen |
| --- | --- | --- |
| Ausgangsstand, `SavedNotice.test.tsx` | 2 | **2** (Fall 1 grün — er bewacht die naive Reparatur) |
| Ausgangsstand, `e2e/bestaetigung-reist-nicht-mit.spec.ts` | 1 | **1** |
| Abräumen wieder entfernt (Baustein / E2E) | 2 / 1 | **2 / 1** |

## Verlauf

- **2026-09-03** — aus der **Nachschau** zu B-50 (Nachtlauf). Im Abnahme-Diff `827b220` entstanden, darum
  `entgangen_bei: [B-50]`. Der Ist-Stand ist am Code nachgesehen, nicht vermutet.
- **2026-09-04** — **ausformuliert → abgenommen** im Nachtlauf, Sprint 1 („Die Oberfläche sagt die Wahrheit
  über ihren eigenen Zustand"). Autonom gegrillt unter Freigabe 1 (`art: Defekt`); beide offenen Punkte
  trugen eine Empfehlung, und die erste ließ sich am Code **entscheiden** statt abstimmen — die naive
  Fassung ist nachweislich falsch (siehe Entscheidung 1). **Rollengang:**
  `e2e/bestaetigung-reist-nicht-mit.spec.ts` fährt die Sohn-Rolle im echten Browser gegen einen echten
  Server, unter „Bewegung reduzieren" (damit die Feier die Aussage nicht verfälscht). Kein
  Browser-Handgang möglich (keine Chrome-Anbindung), damit greift der dokumentierte Ersatz.
  Belege: `tsc -b` sauber, `vitest run` **368/368** grün, `playwright test` **59/59** grün.
  `frontend-reviewer` gelaufen, **kein Blocker**; seine sechs Funde mit Code- oder Testwirkung sind
  eingearbeitet (Protokoll `docs/pm-sitzung-2026-09-04-nacht.md`). Commit `6f7fc60`.
