---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Feste Feldhilfe schließt bei Tastatur-Resize, Popover zeigt nach Layout-Verschiebung aufs falsche Feld]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu B-263 in Lauf A (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-273 · Das feste ⓘ-Popover: Resize ohne Prüfung, Layout-Verschiebung ohne Schließen

Seit [B-263](B-263-info-popover-steht-am-rechten-rand-ueber.md) steht das ⓘ-Popover fest zum Fenster
(`position: fixed`). Der `frontend-reviewer` hat in Lauf A drei Restpunkte genannt, die dort nicht behoben wurden:

1. **`resize` schließt ohne Prüfung** (`frontend/src/components/InfoHint.tsx`, anders als `scroll`). Browser, die
   für die Bildschirmtastatur noch das Layout-Viewport verkleinern (Firefox Android, ältere Chromium-Versionen),
   lösen beim Wegklappen der Tastatur `resize` aus. Ein Tipp aufs ⓘ neben einem gerade bearbeiteten Feld könnte das
   Popover darum sofort wieder schließen. Chrome Android ab 108 und iOS Safari tun das nicht. **Unverifiziert.**
   Vorschlag: wie bei `scroll` nur schließen, wenn sich der Knopf bewegt oder `clientWidth` geändert hat.
2. **Layout-Verschiebung ohne Scrollen**: Verschiebt sich der Inhalt über dem Knopf (eine Liste lädt nach, ein Banner
   erscheint), bleibt das Popover stehen und zeigt auf das falsche Feld. Selten, weil Öffnen ein bewusster Klick ist.
   Vorschlag: `ResizeObserver` oder eine rAF-Prüfung des Ankers, solange es offen ist.
3. **Testlücken, benannt statt geschlossen**: Die Regel „nie über den linken Rand hinaus“ (`Math.max(EDGE, …)`)
   greift bei keiner realen Breite und ist darum nicht prüfbar. Ein Scrollen *nur waagrecht* (Vergleich nur über
   `top`) bleibt ungeprüft. Der Wächter „oben nur, wenn oben Platz ist“ wirkt seit dem senkrechten Klemmen nur noch
   auf die Klasse `.up`.

**In Lauf A bewusst nicht behoben:** Punkt 1 ist ungemessen, und die Zielbrowser lösen den Fall nicht aus. Jede
Behebung hätte als Fund gezählt (Freigabe 3), der Zähler stand bei 4.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-263 in Lauf A
  ([Protokoll](../pm-sitzung-2026-10-26-tag.md)), Funde 2–4 und die Restzeilen der Mutanten-Tabelle.
