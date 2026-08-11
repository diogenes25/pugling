---
tags: [typ/story, status/idee, bereich/frontend, bereich/backend, bereich/katalog]
aliases: [Ein Enum zwei Rollen, SchoolTypes Filter oder Attribut, unbekannte Schulart]
status: idee
prio: P3
art: Frage
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-149, Entscheidung 4 (Grill-Runde 2026-08-11)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-152 · `SchoolTypes` ist Filter und Attribut zugleich — vier Stellen leiten daraus drei Antworten ab

Prüfauftrag, abgespalten von [B-149](B-149-schularten-tabelle-statt-manifest.md) (dort Entscheidung 4).
B-149 sorgt dafür, dass eine **neue** Schulart am Build auffällt; sie fasst aber keine der Stellen an, die
mit einem Wert umgehen müssen, den die Liste nicht kennt. Und die gehen drei verschiedene Wege.

Die tiefere Ursache ist keine Schlamperei, sondern ein Typ mit zwei Aufgaben:

- **Filter** (Übung, Reihe, Fachlehrer-Profil): Eine Kombination ist sinnvoll, `None` heißt *„für alle"*
  (`Pugling.Contracts/Common/LearnBaseTypes.cs`: „no filter exclusion").
- **Attribut** (Kind, `AdminEntities.cs:71`): Ein Kind besucht **eine** Schulart, eine Kombination ist ein
  Modellierungsunfall, und `None` heißt *„nicht angegeben"* (`VaterKind.tsx:228` beschriftet es so).

## Die drei Antworten (Stand `d8abdc9`, in B-149 gemessen)

| Stelle | Verhalten bei einem Wert, den `SCHOOL_TYPES` nicht kennt |
| --- | --- |
| `VaterLehrwerke.tsx:319`, `VaterFachlehrer.tsx:327` | gesperrte Option — sichtbar, unwählbar, überlebt |
| `ExerciseEditModal.tsx:142-143` | **still verworfen** beim Laden ins Formular |
| `VaterKind.tsx:150,186` | überlebt, aber `None` wird dadurch **unerreichbar** |

Die dritte Zeile ist dieselbe Klasse, die am 2026-08-11 in B-148 als Regression behoben wurde — dort im
Fachlehrer-Formular, hier seit Längerem am Kind. Und sie greift **heute schon**, weil `Child.SchoolType`
dasselbe `[Flags]`-Enum ist und eine Kombination tragen kann.

## Warum das ein Prüfauftrag ist und kein Aufräumen

Ein denkbares Ergebnis ist „bleibt". Zwei Rollen an einem Enum können billiger sein als zwei Typen, und
dann ist das richtige Ergebnis eine Begründung im Vertragsprojekt und ein `verworfen` hier — nach den
Regeln dieses Bereichs ein Erfolg. Als `Aufräumen` etikettiert („kein Verhalten ändert sich") wäre dieser
Ausgang gar nicht vorgesehen.

Zu klären wäre mindestens: Soll ein Kind überhaupt eine Kombination tragen dürfen? Wenn nein — ist das eine
Sache des Servers (eigener Typ, Validierung) oder des Frontends? Und was *soll* eine Oberfläche mit einem
Wert tun, den sie nicht kennt: zeigen und sperren, verwerfen, oder durchreichen?

**Kein `entgangen_bei`:** Der Zustand ist älter als B-148/B-149; die beiden haben ihn nur sichtbar gemacht.

## Verlauf

- **2026-08-11** — angelegt aus der Grill-Runde zu B-149 (Entscheidung 4). Bewusst nicht dort
  mitgenommen: B-149s Ziel ist ohne diese Frage erfüllt, und die Antwort ist eine Produktentscheidung,
  keine Aufräumarbeit.
