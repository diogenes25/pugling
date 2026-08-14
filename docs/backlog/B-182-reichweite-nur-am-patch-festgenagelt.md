---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/tests]
aliases: [Ventil halb belegt, Delete ohne Reichweiten-Fall, siehe oben stimmt nicht]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-08-14 zu B-178 (Nachtlauf, Retro Sprint 3)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-178]
wartet_auf: ""
nachgeschaut: ""
---

# B-182 · Die Reichweite des Ventils war nur am PATCH festgenagelt

## Ist-Stand am Code

B-178s Entscheidung 3 ist die **Reichweite** des Admin-Break-glass: ein Admin passiert für *jedes* Fach, nicht
nur für ownerlose. Der Reviewer hat sie mit `Admin_DarfAuchDieArten_EinesFremden_Fachs_Aendern` festgenagelt —
der Fall machte aber **nur ein PATCH**.

`ExerciseCategoriesController.cs` trägt am Löschpfad nur:

> *Same break-glass as in Update (B-178) - see the reasoning there.*

Und der Vertrag verspricht dort „*or a platform admin*". Beides ungeprüft.

**Die Auszählung, die den Fund belegt** (alle `categories/`-Aufrufe der Suite): Der **einzige** Admin-`DELETE`
auf eine Art steht in `Admin_DarfDieArten_EinesFachsOhneEigentuemer_Aufraeumen` — und der zielt auf das
**ownerlose** Seed-Fach.

## Fehlerszenario

Ein späterer Leser verengt das Break-glass „auf seinen Anlass", aber nur an einer Stelle:

```csharp
// nur in Delete
&& !(User.IsAdmin() && category.Subject?.OwnerAdultId is null)
```

- `Admin_DarfDieArten_EinesFachsOhneEigentuemer_Aufraeumen` → Fach ownerlos → `204` → grün
- `Admin_DarfAuchDieArten_EinesFremden_Fachs_Aendern` → nur PATCH → grün
- die Nicht-Admin-Fälle → `403` in beiden Fassungen → grün

**Ergebnis: 831/831 grün**, und PATCH und DELETE öffnen unterschiedlich weit — während der Kommentar
behauptet, sie seien dasselbe.

## Fehlerfamilie

Dieselbe wie in B-168, eine Ebene höher: **die Zusicherung deckt die Behauptung nur halb, und der Kommentar
liest sich, als läge sie ganz.** Der Doku-Absatz des Falls benennt sogar wörtlich die Mutation, gegen die er
gebaut ist („the next reader could narrow the condition to `OwnerAdultId is null` believing they fix a bug")
— und ließ sie an der Nachbar-Action überleben.

## Fix

Zwei Zeilen im bestehenden Fall: nach dem PATCH auch löschen und `204` fordern. Damit hält er beide Hälften,
so wie der Anlass-Fall es für sein ownerloses Fach schon tut.

## Verlauf

- 2026-08-14 · Aufgenommen **und gebaut** aus der Nachschau zu B-178 (Retro von Sprint 3 desselben
  Nachtlaufs). Eigene Story, weil ein Defekt in abgenommener Arbeit sonst aus der Messung fällt.
  **Zählt nicht in den Fehlerzähler von Sprint 3:** außerhalb dessen Diffs (Sprint 2s Arbeit), Freigabe 3
  schneidet dort. Trotzdem sofort behoben statt nur gemeldet.
  **Verifikation:** `FachEigentumTests` **15/15**, volle Suite **831/831**.
  **Rote Probe, gefahren und mit Zahl** (der Reviewer hatte sie nicht gefahren, sein Beleg war die
  lückenlose Auszählung): Nur `Delete` auf `OwnerAdultId is null` verengt →
  **14 grün, 1 rot**, und der Rote ist genau `Admin_DarfAuchDieArten_EinesFremden_Fachs_Aendern`
  (*erwartet `NoContent`, gemessen `Forbidden`*). Vor dem Fix wären es 15 grün gewesen. Mutation
  zurückgenommen.
