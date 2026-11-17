---
tags: [typ/story, status/idee]
aliases: []
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Mathe in die App; Material raw/Math/lambacher-schweizer-9-sh-pugling/md)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-640 · Übungstyp „Gleichung lösen“ mit Lösungsmenge

Der Typ `Arithmetic` kennt je Aufgabe genau eine Dezimalzahl mit Toleranz. Eine quadratische Gleichung hat zwei, eine
oder keine Lösung – das Kernstück von Kapitel IV (Abschnitte IV.2–IV.4) ist damit nicht abfragbar. Gedacht: ein neuer
Typ mit festen Aufgaben, der die **Koeffizienten** speichert statt eines Lösungstexts; der Server rechnet die
Lösungsmenge aus (der Lösungsanhang des Buchs ist nicht fotografiert, und nach B-460 entscheidet C#, was eine Antwort
ist). Antwort als Menge: Reihenfolge egal, Brüche und gerundete Werte zugelassen, eigener Knopf „keine Lösung“.
Stufenleiter: Anzahl der Lösungen bestimmen (Diskriminante) → Verfahren wählen (Ausklammern, Wurzelziehen, Vieta,
pq-Formel; Buch S. 121 Aufgabe 9) → Lösungsmenge auswählen → Lösungsmenge tippen. Der Test von S. 133 dient als
Vorlage für die Klausur. Braucht B-638 (Formeln) und B-639 (Komma). Bewusst nicht enthalten: freie Term-Eingabe mit
Äquivalenzprüfung, Graphen, Begründungsaufgaben.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Konzept „Mathe in die App" aus dem Chat, Lambacher Schweizer 9, Kapitel IV).
