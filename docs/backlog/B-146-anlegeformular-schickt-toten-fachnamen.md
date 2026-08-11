---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [NewSeries schickt subjectName, toter Fachname beim Anlegen, Rest aus B-142]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-142 (beim Grillen von B-143 am 2026-08-10 gefunden)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: 2026-08-11
wartet_auf: ""
---

# B-146 · Das Anlege-Formular schickt einen Fachnamen, den der Server ignoriert

Ein Rest aus [B-142](B-142-fachname-driftet-gegen-fach-id.md). Dort wurde `seriesPatch.ts` bereinigt —
das **Anlege**-Formular daneben blieb stehen.

## User Story

Als **Entwickler** möchte ich, dass keine Stelle im Frontend eine Regel behauptet, die der Server nicht
mehr kennt — sonst lese ich beim nächsten Mal den Kommentar und nicht den Code.

## Ist-Stand am Code

`frontend/src/vater/VaterLehrwerke.tsx:590,597-598` in `NewSeries.submit`:

```tsx
const subject = subjects.find((s) => String(s.id) === form.subjectId);
…
// Den Fachnamen mitschicken: er trägt die Reihe auch dort, wo kein Katalog-Fach gewählt ist.
subjectName: subject?.name ?? null,
```

Beides ist seit B-142 gegenstandslos, und der Kommentar war es sogar schon vorher:

- **Ist ein Fach gewählt**, leitet der Server den Namen selbst ab und ignoriert den mitgeschickten. Totes
  Feld.
- **Ist keines gewählt**, liefert der Ausdruck `null`. Das Formular hat **kein Freitext-Feld fürs Fach** —
  der Kommentar verspricht also eine Fähigkeit, die es an dieser Stelle nie gab.

> **Genau lesen, das ist beim Review aufgefallen:** „Der Server ignoriert `subjectName`" wäre als
> *allgemeine* Regel falsch. `TextbookSeriesController.cs:173` schreibt
> `SubjectName = await SubjectNaming.ResolveNameAsync(db, dto.SubjectId, ct) ?? Trimmed(dto.SubjectName)`
> — das Feld ist beim Anlegen sehr wohl die **Rückfallebene**, wenn keine Id mitkommt. Tot ist allein die
> *Kombination mit einer Id*. Dass das Entfernen trotzdem verhaltensneutral ist, liegt an dieser Stelle
> hier und nicht am Server: Die alte Zeile zog den Namen aus der gewählten Id und schickte darum genau
> dann `null`, wenn keine Id mitging.

Es ist **kein Fehlverhalten**: Die angelegte Reihe ist in beiden Fällen korrekt. Nur die Zeile und ihr
Kommentar behaupten eine Bringschuld, die es nicht mehr gibt.

## Die echte Lücke

Dieselbe Klasse wie Fund 3 des `pugling-reviewer` zu B-142 (`seriesPatch.ts` begründete seine
Kompensation mit einer Server-Aussage, die falsch geworden war) — eine Datei weiter und im Anlege- statt
im Änderungspfad. Der Reviewer hat den einen Pfad geprüft, nicht den anderen; ich beim Beheben ebenso
wenig.

**Kein `entgangen_bei`:** Es ist kein durchgekommener Defekt, sondern toter Code mit falschem Kommentar —
`art: Aufräumen`. Der Vollständigkeit halber gehört aber notiert, dass B-142 seine eigene
Frontend-Bereinigung nicht zu Ende geführt hat.

## Akzeptanzkriterien

1. `NewSeries.submit` schickt kein `subjectName` mehr; der `subjects`-Nachschlag entfällt, sofern er
   nirgends sonst gebraucht wird.
2. Kein Kommentar im Frontend behauptet mehr, der Server leite den Fachnamen nicht ab.
3. Eine neu angelegte Reihe mit gewähltem Fach trägt danach denselben Fachnamen wie vorher —
   abgesichert durch den bestehenden E2E-Weg über `/vater/lehrwerke`.

## Schätzung

**Größe `XS`** — der Anker ist B-02 (zwei Sätze in `lib/fieldHelp.ts` plus der E2E, der sie prüft). Hier
sind es drei Zeilen weniger statt zwei Sätze mehr, und der E2E existiert bereits.

**`migration: nein`**, **`vertragsbruch: nein`** — beides nachgesehen, nicht vermutet:

- Am Schema ändert sich nichts; die Story fasst nur den Absender an.
- `CreateTextbookSeriesDto` behält sein `SubjectName` (`TextbookSeriesDtos.cs:23`). Das Feld zu
  **entfernen** wäre der Bruch: `UnmappedMemberHandling.Disallow` macht aus jedem Client, der es weiter
  schickt, ein `400 unknown_field`. Es stehenzulassen ist die additive, verträgliche Hälfte — und es
  trägt dort weiter eine Aufgabe, nämlich die Rückfallebene ohne Fach-Id (siehe den Kasten oben).

**Risiko, eines:** `subjects` bleibt in `NewSeries` gebraucht — nicht für den Nachschlag, aber für die
`<option>`-Liste des Fach-`<select>` (`VaterLehrwerke.tsx:~640`). Wer „der Nachschlag entfällt" aus
Akzeptanzkriterium 1 zu weit liest und die Prop mitnimmt, bricht das Anlege-Formular. Gemessen: `subjects`
kommt in `NewSeries` an genau zwei Stellen vor, davon fällt eine weg.

**Angriffsplan** (kein Backend-Teil, deshalb ohne Reihenfolge-Frage):

1. `VaterLehrwerke.tsx:590` — die Zeile `const subject = subjects.find(…)` entfernen.
2. `:597-598` — Kommentar und `subjectName:` aus dem Payload entfernen. Kein Ersatzkommentar: die
   verbleibenden Felder erklären sich, und die Regel steht im Vertrag (`TextbookSeriesDtos.cs:20`).
3. Prüfen, dass `subjects` als Prop bleibt (Risiko oben).

**Testweg:** `frontend/e2e/lehrwerke.spec.ts` legt eine Reihe über `/vater/lehrwerke` an — das ist
Akzeptanzkriterium 3 ohne eine neue Zeile. Dazu `npm run test` (Vitest) für `seriesPatch.test.ts`, das von
dieser Änderung **nicht** berührt sein darf: fällt es, war der Griff zu breit. Kein neuer Test — die Story
entfernt totes Feld, sie ändert kein Verhalten, und ein Test über „das Feld wird nicht mehr geschickt"
würde den Absender gegen sich selbst prüfen.

## Verlauf

- **2026-08-10** — angelegt beim Messen für [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md).
  **Bewusst nicht in B-143 mitgenommen:** dessen Ziel — das Formular soll Zustände ausdrücken können, die
  das Modell erlaubt — ist ohne diesen Rest erfüllt, und hier geht es um das Gegenteil, nämlich ein Feld
  zu *entfernen*.
- **2026-08-10** — **geschätzt** (`XS`, `frontend`, `migration: nein`, `vertragsbruch: nein`). Beide Flags
  nachgesehen: das Vertrags-Feld `SubjectName` bleibt bewusst stehen, weil sein Entfernen wegen
  `UnmappedMemberHandling.Disallow` erst der Bruch wäre. Ein Risiko benannt, das beim Lesen der Kriterien
  entsteht — `subjects` trägt in `NewSeries` noch die `<option>`-Liste und darf nicht mitentfernt werden.
- **2026-08-10** — **gebaut** im Nachtlauf (Sprint 3). Keine rote Probe, und das ist die Aussage der
  Story: Es ändert sich kein Verhalten, nur ein totes Feld und ein falscher Kommentar fallen weg. Beleg
  ist deshalb, dass **nichts** rot wurde — Vitest **195/195**, E2E **33/33**, darunter der bestehende Weg
  über `/vater/lehrwerke`, der eine Reihe mit gewähltem Fach anlegt (Kriterium 3). Das benannte Risiko hat
  gehalten: `subjects` bleibt als Prop, nur der Nachschlag ist weg.
- **2026-08-10** — **nach dem Frontend-Review präzisiert**: Die Begründung „der Server ignoriert
  `subjectName`" war als allgemeine Regel falsch — beim Anlegen ist das Feld die Rückfallebene ohne Id
  (`TextbookSeriesController.cs:173`). Tot ist allein die Kombination mit einer Id. Am Code ändert das
  nichts (die entfernte Zeile schickte genau dann `null`, wenn keine Id mitging), an der Story schon.
- **2026-08-10** — **abgenommen** (Commit `637478f`, Rollengang-Nachtrag `d4d3595`).
  Belegt: Backend **813/813**, Vitest **204/204**, Playwright **33/33**, `pugling-reviewer`
  und `frontend-reviewer` gelaufen, ihre Funde behoben oder als eigene Story abgelegt.
  **Rollengang teils im echten Browser** (Anmeldung als Papa, Vater-Web, Katalogseite),
  teils per dokumentiertem Ersatz: Alle Löschpfade hängen an `confirmAction`, und ein
  `window.confirm` blockiert die Chrome-Extension — ein injizierter Ersatz greift nicht, weil
  er in einer isolierten Welt läuft. Dafür stehen die Playwright-Spec (echter Browser, echter
  Dialog) und eine Live-Probe gegen die laufende API. Protokoll:
  [pm-sitzung-2026-08-10.md](../pm-sitzung-2026-08-10.md) → Nachtlauf, Sprint 3.
- **2026-08-11** — **nachgeschaut** (`/code-review` über `1867cfd..HEAD`), **ohne Fund**. Belegt statt
  geglaubt: Das Weglassen von `subjectName` im Anlege-Rumpf ist gefahrlos, weil
  `TextbookSeriesController.cs:173` den Namen serverseitig auflöst. Eingetragen, obwohl nichts gefunden
  wurde — der Blick zählt nur, wenn er verzeichnet ist.
