---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [onBlur legt Thema an, halb getipptes Thema landet in der Unit]
status: ausformuliert
prio: P3
art: Defekt
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 7)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-63]
wartet_auf: ""
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

## Akzeptanzkriterien

> Entwurf, hängt an Offenem Punkt 1.

1. Eine angefangene, nicht bestätigte Themen-Eingabe lässt sich verwerfen, ohne dass sie als Thema
   angelegt wird.
2. Der Platzhalter bzw. die Feld-Erklärung nennt alle Wege, auf denen ein Thema entsteht.
3. Eine per Enter bestätigte Eingabe verhält sich unverändert.
4. Ein Vitest-Fall über das Feld deckt Punkt 1 ab und war vorher rot.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`, am Code nachgeprüft
  (`VaterLehrwerke.tsx:348`). `entgangen_bei: [B-63]`: das Formular ist in jener Story entstanden und
  war `abgenommen`.
