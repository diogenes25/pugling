---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [onBlur legt Thema an, halb getipptes Thema landet in der Unit]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 7)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-63]
wartet_auf: ""
nachgeschaut: 2026-08-10
---

# B-129 · Das Themenfeld legt an, was beim Wegklicken gerade dasteht

Im Unit-Formular werden Themen als Chips gesammelt: tippen, Enter, Chip. Das Feld committet aber
**zusätzlich bei jedem Fokusverlust** — wer zu tippen anfängt und es sich anders überlegt, hat das
Bruchstück als Thema in der Unit, sobald er irgendwo anders hinklickt. Es gibt keinen Weg, eine
begonnene Eingabe zu verwerfen, außer das Feld vorher von Hand zu leeren.

## User Story

Als **Creator** möchte ich eine angefangene Themen-Eingabe abbrechen können, indem ich woanders
hinklicke — so wie in jedem anderen Formular auch.

## Ist-Stand am Code

`frontend/src/vater/VaterLehrwerke.tsx:345-350`:

```tsx
onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); addTopic(); } }}
onBlur={addTopic}
placeholder="Thema eintippen, Enter fügt hinzu"
```

Der Platzhalter nennt **nur** Enter als Weg — `onBlur` ist ein zweiter, unangekündigter. Ausgelöst wird
er von allem, was den Fokus nimmt: das „×" eines bestehenden Chips (`:340`), das Buchtyp-Pulldown, der
„Abbrechen"-Knopf des Formulars, ein Klick ins Grammatik-Feld darunter.

Serverseitig hält nichts dagegen: `SeriesUnitsController.CleanTopics` (`:163-164`) trimmt nur und wirft
Leeres weg — ein Bruchstück wie „Fam" ist ein gültiges Thema.

## Die echte Lücke

Nicht „`onBlur` ist grundsätzlich falsch" — für ein Chip-Feld ist das Committen beim Verlassen ein
verbreitetes, oft hilfreiches Muster: es rettet die Eingabe dessen, der Enter vergisst. Die Lücke ist,
dass es **keine Gegenrichtung** gibt: kein Escape, kein „Abbrechen", keine Anzeige, dass beim Wegklicken
etwas passieren wird. Damit ist jede versehentliche Fokusbewegung eine Dateneingabe.

Der Schaden ist klein und lautlos: ein falsches Thema fällt erst auf, wenn es im KI-Briefing des
Fachlehrers auftaucht — die Unit-Themen sind genau dafür da.

## Offene Punkte

1. **`onBlur` behalten oder streichen?** Empfehlung: behalten, aber ergänzen — `Escape` leert das Feld
   (und verhindert damit das Commit), und der Platzhalter nennt beide Wege. Das rettet weiter die
   vergessene Enter-Eingabe, ohne die Abbruchmöglichkeit zu nehmen. Alternative (billiger, härter):
   `onBlur` streichen, dann ist Enter der einzige Weg — kostet die Rettung und überrascht andersherum.
2. **Gilt dasselbe für andere Chip-Felder?** Beim Ausformulieren nicht erhoben. Vor dem Bau prüfen, ob
   `RepeatedTextFields` (aus [B-69](B-69-wiederhol-felder-alternativen.md)) oder der Interessen-Editor
   dieselbe Bauart haben — falls ja, gehört die Regel einmal an eine Stelle statt dreimal.

## Entscheidungen

Autonom gegrillt im Nachtlauf am 2026-08-09 (Freigabe 1: `art: Defekt`), Protokoll
[pm-sitzung-2026-08-09.md](../pm-sitzung-2026-08-09.md).

1. **`onBlur` bleibt, Escape kommt dazu, der Platzhalter nennt beide Wege.** *Begründung*: Die
   Alternative („`onBlur` streichen") ist billiger, tauscht aber einen stillen Datenverlust gegen einen
   anderen — wer Enter vergisst, verliert seine Eingabe wortlos. Das Problem war nie das Committen,
   sondern die **fehlende Gegenrichtung**; genau die wird ergänzt. *Kosten*: das Feld hat jetzt drei
   Verhaltensweisen statt einer, und sie stehen nur im Platzhalter — wer den überliest, findet Escape
   nicht. Ein `InfoHint` wäre die gründlichere Antwort, kostet aber einen `HelpTopic` und einen
   E2E-Fall in `feldhilfe.spec.ts` für einen Hinweis, der in den Platzhalter passt.
2. **Es ist ein Einzelfall, keine Regel — gemessen, nicht angenommen.** `grep` über alle `.tsx` unter
   `frontend/src` findet **drei** `onBlur`-Handler, und nur einer schreibt Daten: `ClozeTexts.tsx:225`
   (`syncGaps`) leitet abhängigen Zustand aus dem Text ab, `VaterVocab.tsx:211` (`checkDuplicates`)
   liest nur nach. `RepeatedTextFields` aus [B-69](B-69-wiederhol-felder-alternativen.md) hat gar kein
   `onBlur`. *Begründung*: Damit entfällt der Anlass, die Regel an eine gemeinsame Stelle zu ziehen —
   eine geteilte Komponente für einen einzigen Aufrufer wäre Vorrat, kein Muster. *Kosten*: kommt ein
   zweites Chip-Feld dazu, wird die Regel dort neu erfunden; es gibt keinen Wächter dagegen.

## Akzeptanzkriterien

1. Eine angefangene, nicht bestätigte Themen-Eingabe lässt sich verwerfen, ohne dass sie als Thema
   angelegt wird.
2. Der Platzhalter bzw. die Feld-Erklärung nennt alle Wege, auf denen ein Thema entsteht.
3. Eine per Enter bestätigte Eingabe verhält sich unverändert.
4. Ein Vitest-Fall über das Feld deckt Punkt 1 ab und war vorher rot.

## Schätzung

**XS** (`wo: frontend`, `migration: nein`, `vertragsbruch: nein`) — ein `onKeyDown`-Zweig, ein
Platzhaltertext, eine neue Testdatei. Auf dem Niveau des XS-Ankers (B-02).

**Testweg:** neue Datei `frontend/src/vater/VaterLehrwerke.test.tsx` — dafür wird `UnitForm`
exportiert, wie `ProfileForm` es für seinen Test schon ist. Kein E2E: das Feld schickt nichts ab, der
Fehler liegt im Zustand einer Komponente.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`, am Code nachgeprüft
  (`VaterLehrwerke.tsx:348`). `entgangen_bei: [B-63]`: das Formular ist in jener Story entstanden und
  war `abgenommen`.
- **2026-08-09** — Nachtlauf, Sprint 1: autonom gegrillt (zwei Entscheidungen), geschätzt (**XS**,
  `frontend`) und gebaut. **Rote Probe mit Zahl:** von drei neuen Fällen war **1 rot, 2 grün** — der
  Escape-Fall erwartete keinen Chip und maß einen (`button[aria-label="Thema Halbfertig entfernen"]`);
  die beiden anderen (Rettung ohne Enter, Anlegen mit Enter) waren schon vorher grün und belegen, dass
  der Fix das bestehende Verhalten nicht nimmt. Nach dem Fix **3/3 grün**. Offener Punkt 2 durch Messung
  geschlossen statt durch Annahme: drei `onBlur` im ganzen Frontend, nur dieser schreibt.
- **2026-08-09** — `frontend-reviewer` (Sprint 1, Step 5): **AK 2 war nicht erfüllt**. Der Platzhalter
  nannte Enter und Esc, aber nicht den `onBlur`-Weg — also ausgerechnet den Auslöser des Defekts. Wer
  tippt und wegklickt, hatte weiterhin keine Vorwarnung, nur eine Rettungsleine, von der er wissen
  musste. Korrigiert auf „Thema eintippen · **Enter oder Verlassen** fügt hinzu · Esc verwirft".
  **Nicht umgesetzt** und damit offen: der Reviewer hält den Platzhalter für den falschen Träger (er
  verschwindet beim Tippen, WCAG 2.2 SC 3.3.2) und schlägt eine dauerhafte `.sub`-Zeile vor; dazu
  `stopPropagation()` beim Escape, damit das Feld in einem künftigen Dialog-Kontext nicht zusätzlich den
  Dialog schließt. Beides ist echte Arbeit, und der Lauf ist an dieser Stelle beendet worden.
- **2026-08-10** — `frontend-reviewer`, Re-Review: **kein Korrektheitsfund**, aber eine Messung, die AK 2
  endgültig klärt. Der verlängerte Platzhalter war 415 px breit und schnitt im Bearbeiten-Formular auf
  Telefonbreite (399 px verfügbar) ausgerechnet den Esc-Hinweis ab — ohne Ellipse. Wichtiger noch der
  Einwand darunter: ein Platzhalter ist **nur im leeren Feld** sichtbar, und beide angekündigten
  Verhaltensweisen wirken nur im **nicht** leeren. Die Wege waren also genau dann lesbar, wenn sie
  wirkungslos sind. Darum umgestellt: Platzhalter zurück auf „Thema eintippen", die Wege stehen als
  dauerhafte `.sub`-Zeile unter dem Feld — dasselbe Muster, das zwei Felder weiter unten schon steht.
  Damit ist AK 2 nicht nur buchstäblich, sondern praktisch erfüllt, und der offene Punkt „der Platzhalter
  ist der falsche Träger" ist geschlossen. `stopPropagation()` beim Escape bleibt offen und ist belegt
  harmlos: `UnitForm` rendert in einer Tabellenzeile, nicht in `Modal`, dessen Escape-Hörer auf
  `document` sitzt. Sobald es in einen Dialog wandert, wird der Punkt scharf.
- **2026-08-10** — **abgenommen.** Commit `6a545fe`. Verifikation: **3/3** eigene Fälle, Frontend-Suite
  **177/177**, `tsc -b` sauber, E2E **29/29** als Rollengang (`lehrwerke.spec.ts` und
  `creator-lehrwerk-weg.spec.ts` fahren den Creator-Weg durch dieses Formular), `frontend-reviewer`
  zweimal gelaufen.
- **2026-08-10** — nachgeschaut (Nachtlauf, Retro des Folge-Sprints). Geprüft wurde die Gegenrichtung, nicht
  die Existenz des Fixes: `onKeyDown` leert bei Escape das Feld, **bevor** `onBlur={addTopic}` greifen kann
  (`VaterLehrwerke.tsx:504-509`) — der Abbruch wirkt also wirklich und wird nicht vom Blur überholt. Die
  beiden Wege stehen als dauerhafte `.sub`-Zeile statt im Platzhalter, der im nicht-leeren Feld unsichtbar
  wäre. Kein durchgekommener Defekt.
