---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/qualitaet]
aliases: [ObjectiveCard ohne useAction, Etappe ohne Erfolgsmeldung, Dashboard-Doppelklick]
status: ausformuliert
prio: P2
art: Defekt
quelle: docs/backlog/B-26-e2e-in-ci.md
---

# B-54 · Fünf Knöpfe im Vater-Web gehen an den Schreib-Primitiven vorbei

Die Ziel-Karte im Vater-Web schreibt an `useAction`/`StatusBanner` vorbei: sie baut ihr eigenes
`try/catch` mit lokalem `err`-State. Folge – **eine geglückte Mutation meldet gar nichts.** Anlegen,
Ändern und Löschen einer Etappe (Key Result) laufen wortlos durch; nur der Fehler wird sichtbar.
Aufgefallen beim Umschreiben des E2E-Abschnitts in [B-26](B-26-e2e-in-ci.md): der alte, tote Abschnitt
konnte noch auf „Lernziel angelegt." prüfen, der neue hat keine Meldung mehr, auf die er prüfen könnte.

## User Story

Als **Vater**, der eine Etappe an einem großen Ziel nachträgt, möchte ich dieselbe Rückmeldung bekommen
wie überall sonst im Vater-Web – damit ich nicht aus dem Erscheinen einer Tabellenzeile schließen muss,
ob mein Klick angekommen ist, und damit ein zweiter Klick nicht zwei Etappen anlegt.

## Ist-Stand am Code

- `VaterZiele.tsx:209-212` – eigenes `act()` mit `setErr(null)` / `try … catch (e) { setErr(errorMessage(e)) }`
  statt `action.run(fn, okText)`. Ausgabe nur über `{err && <div className="banner err">}` (`:249`).
- Betroffen sind darüber drei Schreibpfade: `createKeyResult` (`:266`), `updateKeyResult` (`:237`) und
  `deleteKeyResult` (`:240`).
- Die Ebene darüber macht es richtig: `Objectives` (`:164`) nimmt `useAction` und rendert einen
  `StatusBanner` (`:180`) – Stilllegen und Löschen eines Ziels melden also, das Bearbeiten seiner Etappen
  nicht. Dieselbe Seite, zwei Verhalten.
- Regel dazu: [frontend/CLAUDE.md](../../frontend/CLAUDE.md) bzw.
  [memory/frontend-schreib-primitive](../obsidian.md) – „`useAction` + `StatusBanner` für **jede** Mutation".

### Die vollständige Liste (nachgezählt 2026-08-01, nach E5)

Nach dem E5-Durchgang tragen **fünf** mutierende Knöpfe des Vater-Webs kein `disabled={busy}`, alle aus
demselben Grund: ihr Schreibpfad geht am Primitiv vorbei und hat darum kein `busy`.

| Datei:Zeile | Knopf | Schreibpfad |
|---|---|---|
| `VaterZiele.tsx:306` | „OK" (Zielwert einer Etappe) | `ObjectiveCard.act` (`:213`) |
| `VaterZiele.tsx:308` | „Entfernen" (Etappe) | `ObjectiveCard.act` |
| `VaterZiele.tsx:349` | „Etappe übernehmen" | `ObjectiveCard.act` – **trägt** ein `disabled`, aber an einer Eingabeprüfung (`scope.subjectId === ""`), nicht an `busy` |
| `VaterVocab.tsx:694` | `TagChip` „×" | `removeGlobal` (`:622`) / `removeChild` (`:642`), eigenes `try/catch` mit `setErr` |
| `VaterDashboard.tsx:96` | „Kind anlegen" | `addChild` (`:22`), eigenes `try/catch` mit `msg` |

Zwei Dinge, die dabei über die ursprüngliche Story hinausgehen:

- **`VaterDashboard.addChild` ist derselbe Defekt wie [B-53](B-53-wizard-doppelklick.md)**, auf dem
  Bildschirm, auf dem ein neuer Vater **zuerst** landet: kein `busy`, kein Ref-Gate, zwei Klicks im selben
  Tick legen **zwei Kinder** an. B-53 hielt den Assistenten für „den teuersten Fall der Klasse" – der
  Assistent legt mehr an, aber dieser Weg wird häufiger gegangen.
- **Der Fehler erscheint dort grün.** `:103` rendert die Meldung fest als `<div className="banner ok">`,
  und `:33` schreibt in dieselbe Variable den `errorMessage(err)`. „Kind angelegt." und „Name schon
  vergeben" sehen identisch aus. Genau das verhindert `StatusBanner` + `ActionMessage.ok`.

**Wie die Zahl zweimal falsch war** (erst „fünf fehlen", dann „genau zwei bleiben"): beide Male hat eine
Messung nur geprüft, ob am Knopf das Wort `disabled` **steht** – nicht, woran es gebunden ist. `:349` oben
ist der Beleg. Wer nachzählt, muss die Bindung lesen.

## Die echte Lücke

Nicht „ein Banner fehlt", sondern: **kein Rückkanal für Erfolg** an der einzigen Stelle, an der ein Ziel
überhaupt erreichbar gemacht wird. Dazu kommt die Wiedereintritts-Sperre aus
[B-43](B-43-frontend-komponententests.md)/E5, die genau an `useAction` hängt – dieser Pfad bekäme sie
nicht mit. Ein Doppelklick auf „Etappe übernehmen" legt heute zwei Etappen an.

## Offene Punkte

1. **Zusammen mit E5 bauen oder danach?** Die Sperre aus B-43 wirkt je `useAction`-Instanz; solange dieser
   Pfad daran vorbeigeht, ist er von ihr nicht gedeckt. **Empfehlung:** danach, aber unmittelbar – E5
   ändert das Primitiv, diese Story ändert nur den Aufrufer.
2. Gehört das mit [B-49](B-49-sohn-app-schreib-primitive.md) („Die Sohn-App benutzt die geteilten
   Schreib-Primitive nicht") in **eine** Story? **Empfehlung:** nein – andere Fläche, andere Rolle, und
   B-49 ist ungeprüft.

## Akzeptanzkriterien

1. Die drei Schreibpfade der Karte (`createKeyResult`, `updateKeyResult`, `deleteKeyResult`) laufen über
   `useAction`; das eigene `try/catch` mit `err`-State ist weg.
2. Jeder der drei meldet **Erfolg** über einen `StatusBanner` – nicht nur den Fehler.
3. `vater-von-null.spec.ts` prüft nach „Etappe übernehmen" die Erfolgsmeldung, nicht nur das Erscheinen der
   Zeile. (Heute äußert sich ein fehlgeschlagener Aufruf als Timeout auf die Zeile statt als lesbare
   Meldung — genau die Diagnose, die der Abschnitt vor dem Umbau hatte.)
4. Ein Doppelklick auf „Etappe übernehmen" legt **eine** Etappe an — greift, sobald die Sperre aus
   [B-43](B-43-frontend-komponententests.md) im Primitiv sitzt (dort seit 2026-08-01).
5. Dasselbe für die zwei nach E5 nachgetragenen Stellen: `VaterVocab` `TagChip` und
   `VaterDashboard.addChild`. Beim Dashboard gehört dazu, dass ein **Fehler nicht mehr grün** erscheint –
   heute schreiben Erfolg und Fehler in dieselbe Variable, die fest als `banner ok` gerendert wird.
6. Danach trägt **jeder** mutierende Knopf des Vater-Webs `disabled={busy}`. Gegenprobe: nachzählen mit
   Blick auf die **Bindung** des `disabled`, nicht auf seine Anwesenheit (`VaterZiele.tsx:349` ist der Fall,
   an dem genau das zweimal schiefging).

## Verlauf

- **2026-08-01** — angelegt aus dem `frontend-reviewer`-Befund zu B-26/E0 (Befund 6), am Code belegt.
- **2026-08-01** — beim Bauen von E5 ([B-43](B-43-frontend-komponententests.md)) auf **fünf Stellen**
  erweitert (Liste oben). `VaterVocab` `TagChip` fiel beim Durchgang auf, `VaterZiele:349` und
  `VaterDashboard:96` erst im `frontend-reviewer`-Lauf danach – beide, weil die erste Zählung nur die
  *Anwesenheit* eines `disabled` prüfte. Offener Punkt 1 ist damit entschieden: die Sperre sitzt seit E5 im
  Primitiv, diese Story ist ihr Nachlauf – und der Vollständigkeits-Beweis dazu, dass danach **jeder**
  mutierende Knopf im Vater-Web `disabled={busy}` trägt. Die Priorität ist dadurch gestiegen: mit
  `VaterDashboard.addChild` steckt jetzt ein Doppelklick-Defekt auf der Startseite darin, nicht nur eine
  fehlende Erfolgsmeldung.
