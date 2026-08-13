---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [categories.error wird nie gezeigt, Fach hat keine Arten oder Laden gescheitert]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-13 zu B-154
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-154]
---

# B-174 · Die Arten-Liste verschweigt, dass ihr Laden gescheitert ist

Dieselbe Klasse wie [B-111](B-111-verlauf-luegt-im-fehlerfall.md) und
[B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md), eine Fläche weiter: „dieses Fach hat keine
Arten" und „das Laden ist gescheitert" sehen identisch aus.

## Behauptung (aus der Nachschau, von mir nicht nachgeprüft)

`frontend/src/vater/CatalogAdmin.tsx:36-37`, `:143`, `:177` — **`categories.error` wird nirgends gerendert.**
Das Gegenbeispiel steht in derselben Story: `VaterKatalog.tsx:29` zeigt `subjects.error` an.

Scheitert der **erste** Abruf, ist `data === null`, `CategoryRows` liefert `null` (`:177`), die Überschrift
heißt „Arten" ohne Zahl — und kein Fehler steht auf dem Bildschirm. Unter einem fremden Fach fällt zusätzlich
der erklärende Satz weg, die Fläche wird also vollständig stumm.

**Attribution, ehrlich:** Nicht von B-154 eingeführt — der Vorgängerstand hatte `{categories.data?.map(...)}`
und ebenfalls keine Fehleranzeige. Es ist aber die im Nachschau-Auftrag benannte Familie auf genau der
Fläche, die B-154 und B-157 zweimal umgebaut haben, und **beide** Reviews haben es nicht gemeldet. B-157 hat
es leicht verschärft, weil im Nicht-Eigentümer-Zweig nun auch der Erklärsatz an `categories` hängt.

## Abgrenzung — was hier *nicht* hingehört

Der zweite Teil desselben Fundes (beim Fachwechsel stehen die Arten des **vorigen** Fachs mit den Rechten des
neuen) ist **bereits** [B-164](B-164-useasync-paart-frischen-zustand-mit-alten-daten.md), dort mit allen drei
Wegen und einer gemessenen Breite (110 `useAsync`-Aufrufstellen, 67 mit Abhängigkeiten). Diese Story ist
ausdrücklich **nur** die verschwiegene Fehlermeldung — sonst hätte der Bereich zwei Fassungen derselben
Sache, und die veraltete gewinnt.

## Offene Punkte

1. Reproduzieren: den einen Abruf scheitern lassen und beide Fälle ansehen (erster Abruf vs. Wechsel).
2. Nur hier oder als Muster? Empfehlung: **hier zuerst**, dann greppen — `useAsync`-Aufrufe, deren `error`
   nirgends gerendert wird, sind auszählbar, und die Zahl entscheidet, ob daraus eine eigene Sammel-Story
   wird. Nicht vermuten.
3. Zusammen mit B-164 bauen? Empfehlung: **ja, wenn beide dran sind** — dieselbe Datei, dieselben zwanzig
   Zeilen. Aber nicht zusammenlegen: eine verschwiegene Fehlermeldung und ein Alt-Daten-Fenster sind zwei
   Aussagen, und B-164 trägt die gemessene Breite.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-154. Bleibt `unverifiziert`. Die Alt-Daten-Hälfte des
  Fundes ist bewusst **nicht** hier, sondern liegt schon als B-164 vor — vor dem Anlegen geprüft, um kein
  Duplikat zu erzeugen.
