---
tags: [typ/story, status/verworfen, bereich/frontend]
aliases: [Vater-Web i18n, Vater-Web Englisch]
status: verworfen
prio: P4
art: Wunsch
quelle: B-87 (geteilt)
unverifiziert: true
ersetzt_durch: []
grund: "Entscheidung des Nutzers am 2026-10-23 (G-9): Mehrsprachigkeit ist über die Sohn-Arcade auf Englisch (B-85) hinaus kein Ziel; das Vater-Web bleibt deutsch"
---

# B-91 · Vater-Web-Textkorpus auf Übersetzungsschlüssel umstellen (Englisch)

Erster Teil aus dem geteilten [B-87](B-87-vater-web-franzoesisch-server-sprachfeld.md): derselbe Vorgang wie
[B-85](B-85-i18n-infrastruktur-sohn-arcade-englisch.md) (Textkorpus auf Übersetzungsschlüssel umstellen,
mit derselben i18n-Infrastruktur, gleiche Zielsprache Englisch), aber für `frontend/src/vater/` statt
`frontend/src/sohn/` — inklusive `fieldHelp.ts` (305 Zeilen Fließtext). Setzt voraus, dass B-85 mindestens
`in-arbeit` ist. **B-87s eigene Recherche (Entscheidung 3) warnt ausdrücklich, dass auch dieser Teil allein
noch zu groß sein dürfte** (~9,3× der Textmenge des bereits `L`-groß geschätzten B-85) und beim
Ausformulieren geprüft werden sollte, ob eine weitere Teilung nötig ist — z. B. entlang Funktionsbereichen
(Katalog/Übungen, Pläne/Positionen, Kind-Verwaltung/Konto, Shop/Rewards/Ziele) und `fieldHelp.ts` separat
wegen seines eigenen Übersetzungsqualitätsanspruchs. Diese Story ist deshalb bewusst nicht weiter
ausformuliert, sondern nur als Platzhalter mit Kontext angelegt.

## Verlauf

- **2026-08-04** — angelegt beim Teilen von [B-87](B-87-vater-web-franzoesisch-server-sprachfeld.md)
  (Entscheidung 2 dort), bewusst auf `idee` belassen: B-87s eigene Recherche warnt, dass dieser Teil selbst
  wahrscheinlich noch zu groß ist — eine ehrliche Ausformulierung braucht einen eigenen Durchgang, der
  diese Frage zuerst klärt, nicht eine vorgezogene Schätzung auf Verdacht. B-91 ist der stabile Bezeichner
  dieser Story ab jetzt (ursprünglich als B-88 angelegt, aber wegen einer ID-Kollision mit einer parallel
  angelegten, inhaltlich unabhängigen Story umbenannt — reine Backlog-Pflege, keine inhaltliche Änderung).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): Zahlen veraltet (`fieldHelp.ts` 382 Zeilen, `vater/` 49 `.tsx`). Steht auf der Grill-Tagesordnung „Ist Mehrsprachigkeit über B-85 hinaus ein Ziel?“.
- **2026-10-23** — **verworfen** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (G-9).
