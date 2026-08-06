---
tags: [typ/story, status/idee, bereich/backend, bereich/qualitaet]
aliases: [DailyBox-Spanne ungeprüft, Min/Max ohne Test]
status: idee
prio: P3
art: Aufräumen
quelle: pugling-reviewer-Befund zur Abnahme von
  [B-107](B-107-dailybox-zufallswert-in-docs-capture.md) (2026-08-06) — nicht dort mitgenommen, weil
  B-107s Ziel (byte-stabile Capture-Dateien) ohne diesen Punkt erfüllt ist
unverifiziert: true
---

# B-118 · Keine Zusicherung sieht die Ziehungsspanne der Tagesbox mehr

Seit [B-107](B-107-dailybox-zufallswert-in-docs-capture.md) pinnt `PuglingWebAppFactory` die Münz- und
Gem-Ziehung der täglichen Box auf je einen festen Wert. Damit durchläuft **kein** Test mehr eine echte
Ziehung über `[Min,Max]`: die dokumentierte Inklusivität der Obergrenze (`opts.MaxCoins + 1` in
`DailyBoxService.cs:44`) und die Bindung an die Spanne aus `appsettings.json` sind unbelegt. Der Reviewer
hält ausdrücklich fest, dass B-107 dabei **nichts kaputt gemacht** hat — die vorherige
`Assert.InRange(10, 30)` hätte weder eine vertauschte noch eine um eins verfehlte Grenze gefangen; der Pin
macht eine schon vorhandene Lücke nur sichtbar. Vorschlag aus dem Review: eine eigene kleine Factory mit
einer engen, von der Produktionsspanne verschiedenen Spanne (etwa `Min 7 / Max 9`) und einer
`InRange(7, 9)`-Zusicherung — dasselbe Muster, mit dem `TimeSlotsOnFactory` die stillgelegten Zeitfenster
für den einen Test wieder anschaltet, der sie sehen muss.
