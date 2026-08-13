---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [Ladefenster im Assistenten, Kästchen ohne disabled, Auswahl überlebt den Filterwechsel]
status: ausformuliert
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-13 zu B-161
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-161]
---

# B-169 · Im Ladefenster sind die alten Zeilen anklickbar — und die Auswahl überlebt

B-161 hat die unsichtbare Auswahl im Lehrplan-Assistenten geschlossen. Ihr Schaden ist über eine zweite Tür
weiter erreichbar: **während** eine neue Suche lädt, stehen die Zeilen der *vorigen* noch da und sind
bedienbar. Ein Haken darin gehört zu einem Filter, der nicht mehr gilt — und nichts leert ihn je wieder.

## Ist-Stand am Code (selbst nachgeprüft)

Drei Bausteine, jeder einzeln richtig, zusammen ein Loch:

1. **Der Effekt hängt am Kriterien-Schlüssel** (`frontend/src/vater/VaterWizard.tsx:184-194`): Bei einer
   Abweichung leert er die Auswahl und setzt `geltenderFilterKey.current = filterKey` — **sofort**, lange
   vor der Antwort.
2. **Gerendert wird `exercises.data`, nicht der Schlüssel.** Der Platzhalter greift bewusst nur bei
   `exercises.loading && exercises.data === null` (`:532`) — nach dem ersten Laden bleiben die alten Zeilen
   also stehen (das ist die B-116-Regel und richtig, sonst hängen aufgeklappte Bereiche aus).
3. **Das Kästchen trägt kein `disabled`** (`:542`). „Alle wählen" daneben trägt
   `disabled={selectAllBusy || exercises.loading}` (`:525`) — **der Wächter steht ein Element neben dem
   Loch.**

## Fehlerszenario

1. Schritt 3, Fach Englisch. Die Liste zeigt „Unit 2 – Wörter" (#42).
2. In „Übung suchen…" `envi` tippen. Kein Debounce (`:475`) — jeder Tastendruck feuert eine Abfrage. Der
   Effekt hat schon geleert und den Schlüssel weitergesetzt; die alten Zeilen stehen noch.
3. **Während die Abfrage läuft** das Kästchen von #42 anklicken → `selected = [42]`, und `toggle` löscht
   dabei zusätzlich den Hinweis (`:220-224`).
4. Die Antwort kommt, die Liste zeigt zwei andere Übungen. `filterKey` hat sich **nicht erneut** geändert →
   der Effekt läuft nicht → `selected` bleibt `[42]`.
5. „Weiter" prüft nur `length === 0` (`:294`) → Feinschliff → `finish` schickt `exerciseIds: [42]`.

**Ergebnis:** eine Tagesziel-Position mit `penaltyCoins` für eine Übung, die der gezeigte Filter
ausschließt. Wörtlich der P1-Schaden, gegen den B-161 gebaut wurde.

Der einzige Resthinweis ist „davon 1 unten nicht sichtbar" (`:465`) — und der ist hier **irreführend**:
`unsichtbar > 0` zieht die zwei Situationen zusammen, die B-161 getrennt haben wollte. Die genehmigte
(„gewählt jenseits der geladenen Seite", Entscheidung 3 — dort ist „unten" richtig) und die verbotene
(„gar nicht in dieser Trefferliste"). Ein Titel steht nirgends; der Vater kann #42 nicht identifizieren.

## Abgrenzung

**Nicht** von [B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) gedeckt: dessen AK 3
repariert die **Trefferzahl** während des Ladens. Ein Zahlen-Fix lässt die alten Zeilen anklickbar. B-162
nennt dieses Fenster ausdrücklich als „von B-161s Diff nicht verschlechtert" — das ist richtig und trifft
diesen Fund nicht, denn er war schon vor B-161 da und ist von ihr nur *nicht mitgenommen* worden.

## Angriffsplan (Vorschlag)

Denselben Gedanken wie das Generationen-Gate in `selectAll`, aber für den **synchronen** Weg: den Schlüssel
der **geladenen Seite** mitführen, nicht nur den der Kriterien. Im `merken`-Effekt (`:241`)
`seitenSchluessel.current = filterKey` setzen und das Kästchen sperren, solange
`seitenSchluessel.current !== filterKey`.

Dazu die irreführende Hälfte der Unsichtbaren-Zahl auflösen — „jenseits der Seite" und „nicht in dieser
Liste" sind zwei Aussagen und brauchen zwei Sätze.

**Testweg**: `wizardSearch.test.ts` kann das **nicht** allein tragen (es kennt die Ladezustände nicht); der
tragende Fall gehört auf Komponentenebene oder in `assistent.spec.ts` mit einer verzögerten Antwort.
Rote Probe **mit Zahl**: das `disabled` entfernen und belegen, dass genau der neue Fall fällt.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-161. Der Ist-Stand ist von mir gegengeprüft
  (`:184-194`, `:525`, `:532`, `:542`): das Kästchen trägt tatsächlich kein `disabled`, während der Knopf
  daneben eines hat.
