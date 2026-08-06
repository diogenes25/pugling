---
tags: [typ/story, status/idee, bereich/backend, bereich/api]
aliases: [Paging-Form a, Top-Level-Sammlungen paginieren]
status: idee
prio: P3
art: Wunsch
quelle: B-121 (Entscheidung 4) — abgespalten, weil die im Bericht empfohlene Paging-Form (a) tatsächliches
  Verhalten für heutige Aufrufer ändert, keine reine Aufräumarbeit ist
unverifiziert: true
---

# B-122 · Sieben Top-Level-Sammlungen bekommen `skip`/`take`

Aus [B-101](B-101-fehlercodes-und-drei-waechter.md)/[B-121](B-121-platzhalter-und-paging-tore.md): ein
Array-`GET` **ohne Routenparameter** (Top-Level-Sammlung) soll `take` unterstützen. Trifft nach dem alten
Bericht 10 Endpunkte, davon 3 begründete Ausnahmen (`exercise-types` = Manifest, `profiles/match` = ein
Treffer, `profiles` = wenige) ⇒ **7 zu ergänzen**. Diese Zahl ist gegen den aktuellen Code **nicht** erneut
nachgezählt (das ist die erste Aufgabe von `ausformuliert`) — der alte Bericht ist zwei Tage alt, B-121s
eigene Nachzählung der Paging-Gesamtzahl hat bereits eine Abweichung gefunden (35 → 34).

**Warum das ein `Wunsch` ist, kein `Aufräumen`:** Sobald ein Endpunkt einen Default-`take` bekommt (z. B.
`PagingExtensions.DefaultTake = 100`), ändert sich sein Antwortverhalten für jeden heutigen Aufrufer, der
sich auf eine vollständige, unbegrenzte Liste verlässt — er bekäme ab einer bestimmten Größe stillschweigend
nur einen Ausschnitt. Das ist eine Kompatibilitäts-/Produktentscheidung (welcher Default? Frontend-seitig
schon auf Paging vorbereitet? Gibt es einen Aufrufer, der heute wirklich mehr als `DefaultTake` Zeilen
erwartet?), keine Arbeit, bei der „alles bleibt so grün wie vorher" gilt.

## Offene Punkte

1. Welche der (laut Bericht) 7 Endpunkte tatsächlich `> DefaultTake` Zeilen liefern können — und ob das für
   die betroffenen Datenmengen dieser App (Familien-Maßstab, keine Publikums-App) überhaupt ein reales
   Risiko ist. Nicht verifiziert.
2. Ob das Frontend an einer der 7 Stellen schon mit einer unbegrenzten Antwort rechnet (z. B. eine Auswahl-
   Liste, die vollständig sein muss, damit eine Suche/Filterung im Client funktioniert) — das wäre der Fall,
   in dem `take` ohne Frontend-Anpassung einen echten Regressionsdefekt einführt.
3. Empfehlung, falls gebaut wird: `take` **optional**, ohne Parameter unverändertes (unbegrenztes) Verhalten
   — additiv statt mit einem greifenden Default. Das widerspricht der Empfehlung aus B-101s Arbeitsrunde
   („Enge Regel: hat `take`"), ist aber die risikoärmere Form. Zur Entscheidung, nicht vorentschieden.

## Verlauf

- **2026-08-06** — angelegt beim Bauen von B-121 (Entscheidung 4 dort): Form (a) der Paging-Empfehlung
  bewusst nicht autonom umgesetzt, weil sie Bestandsverhalten ändert.
