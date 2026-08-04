# T-05 · Wie verschmelzen `/vater/katalog` und `/vater/lehrwerke` im Frontend?

Status: entschieden     <!-- offen | beansprucht | entschieden -->
Typ: grilling           <!-- research | prototype | grilling | task -->
Blockiert durch: T-01

## Frage

Beide Bereiche sind heute getrennte, unverlinkte Routen. Nach der Verschmelzung ist der Katalog-Baum
strukturell `TextbookSeries → SeriesUnit → Exercise` statt `Subject → Chapter → Exercise` —
verschmelzen die beiden Frontend-Bereiche zu einer Route, bleibt `/vater/katalog` bestehen und zeigt
jetzt Lehrwerk-Struktur, oder bleiben es zwei verlinkte, aber getrennte Ansichten (Lehrwerk-Verwaltung
vs. Übungs-Anlage innerhalb einer gewählten Unit)?

## Antwort

**Zwei verlinkte, getrennte Ansichten — keine Routen-Verschmelzung.** Der Notfall-Fix aus dem
Bau-Sprint (siehe `docs/pm-sitzung-2026-08-04.md`, Runde „Re-Review") hat diese Option bereits
umgesetzt, nicht nur vorläufig: `/vater/katalog` (`CatalogAdmin.tsx`) verwaltet Fach+Art und verweist
auf `/vater/lehrwerke` für Reihe/Unit; die Übungs-Anlage (`VaterExerciseCreate.tsx`) bekam einen
eigenen Fach→Reihe→Unit-Kaskaden-Picker, ohne die Seite zu wechseln. Das erfüllt beide Teile der
Nutzer-Absicht (Reihe/Unit-Verwaltung an einem Ort, Übung anlegen ohne Seitenwechsel) und deckt sich
mit B-106s eigener Abgrenzung: „Frontend-Detailarbeit über das Zusammenführen von Katalog/
Lehrwerke-Bereichen **hinaus**" steht dort ausdrücklich außerhalb des Ziels — die Grund-Konsolidierung
(dieser Fix) war also immer Teil der Karte, eine engere Routen-Verschmelzung nie.

Formal offen bleibt nur eine kleinere UX-Idee (Units direkt aus der Übungs-Anlage heraus anlegen, ohne
zu `/vater/lehrwerke` zu wechseln) — kein Live-Befund einer der drei Rollen verlangt sie, darum keine
eigene Story, sondern höchstens ein künftiger Anmerkungs-Punkt, falls ein Creator sie tatsächlich vermisst.
