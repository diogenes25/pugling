---
tags: [typ/story, status/verworfen, bereich/doku]
aliases: [i18n-Rest, Ledger-Texte]
status: verworfen
prio: P3
art: Aufräumen
quelle: memory/api-fehlermeldungen-englisch.md
grund: "Der vermutete Rest existiert nicht: kein einziger `///`- oder `//`-Doku-Kommentar im Backend ist
  noch deutsch (Vollscan über alle fünf Projekte, Belege im Ist-Stand). Die drei in der Idee genannten
  Korpora — ScoringService-Buchungstexte, ExerciseContentResolver-Platzhalter, interne Exceptions — sind
  keine i18n-Reste, sondern exakt der von CLAUDE.md und docs/translate.md ausdrücklich benannte
  bewusst-deutsche Anteil (Produktinhalt bzw. Laufzeit-Diagnose). Die einzige ungeprüfte Frage der Idee
  (ob Buchungstexte fürs Kind auf Englisch stehen sollten) ist keine offene Frage, sondern von CLAUDE.md
  längst beantwortet — siehe Entscheidung 1."
---

# B-30 · i18n-Rest: Ledger-Texte, Platzhalter, interne Exceptions

Die Fehlermeldungen sind englisch. Bewusst deutsch geblieben sind die `ScoringService`-Buchungstexte
(`Reason`), Content-Platzhalter wie `(Vokabel '…' fehlt)` in `ExerciseContentResolver` und interne
Exceptions.

## User Story

Als Entwickler möchte ich wissen, ob nach der XML-Docs-/Kommentar-Übersetzung (B-08, `docs/translate.md`)
noch ein echter unübersetzter Rest an Code-Dokumentation im Backend steht, damit „i18n" nicht als
Dauerauftrag offenbleibt, obwohl er längst erledigt ist.

## Ist-Stand am Code

**Die Code-Dokumentation ist vollständig englisch — kein Rest gefunden.**

- `docs/translate.md:231-235` verbucht den Gesamtstand: alle 2303 `<summary>` *und* jeder `//`-Kommentar in
  `Pugling.Api`, `Pugling.Contracts`, `Pugling.Client`, `Pugling.Agent.Creator`, `Pugling.Api.Tests`
  sind übersetzt (Etappen 0–9, alle „durch"), verifiziert mit `dotnet build Pugling.sln -c Release` grün
  und `dotnet test Pugling.sln -c Release` 615/615 grün.
- Eigener Vollscan gegen den aktuellen Stand (`grep -rnE "^\s*(///|//)[^\"]*\b(und|für|wird|muss|kann|
  sollte|damit|dass|wenn|beim|einer|einem|eines)\b" backend --include="*.cs"`, `Migrations/` ausgenommen
  als Generat) liefert **keinen Treffer** — kein deutsches Wort in einer Doku-Kommentarzeile.
- Ein zweiter Scan auf deutsche Buchstaben (`äöüß`) in Kommentarzeilen trifft ausschließlich englische
  Prosa, die deutsche **Beispielwörter in Anführungszeichen** zitiert (`"Pokémon"`, `"sehr groß"`,
  `"Begrüßungen"`, `InterestSlug.cs:7`, `ScoringOptions.cs:21`, `TimetableController.cs:11`) — genau die
  in `CLAUDE.md` als „deutsche Beispielwörter und Testdaten" freigestellte Kategorie, keine unübersetzte
  Doku.
- Die drei in der Idee genannten Korpora sind am Code bestätigt, aber sie sind **Produktinhalt bzw.
  Laufzeit-Diagnose**, keine Doku:
  - `ScoringService.cs:68,73,77` — Ledger-Texte („Leitner-Wiederholung richtig → Box …", „Combo ×N –
    Bonus!", „Schnelle Antwort …"), dazu weitere Fundstellen in `GamificationService.cs:43,63`,
    `ShopService.cs:161,170,248,257`, `MeController.cs:193`, `PositionProgressService.cs:210,319`,
    `Data/Seed.cs:259-260,960-961` — durchgängig deutsch, weil sie im `Reason`-Feld an das **Kind**
    ausgespielt werden.
  - `ExerciseContentResolver.cs:95,127` — Content-Platzhalter `(Vokabel #… fehlt)` / `(Vokabel '…' fehlt)`,
    ebenfalls Produktinhalt, das im Spiel sichtbar wird.
  - `Program.cs:263` ("Konfiguration 'Jwt:Key' muss in Nicht-Dev-Umgebungen gesetzt sein."),
    `ArithmeticProblemGenerator.cs:20,22`, `PointKindCurrency.cs:26` — interne Exceptions, die an den
    Betreiber gehen, nicht an den Code-Leser.
  - `docs/translate.md:242-255` benennt exakt diese drei Kategorien (Produktinhalt, Laufzeit-Diagnose,
    Testdaten) bereits als **bewusst deutsch bleibend** — keine Lücke, sondern dokumentierte Grenze der
    B-08-Etappe.
- `B-08` selbst (`docs/backlog/B-08-xml-docs-englisch.md:39-40`) trennt den Korpus explizit: „Der deutsche
  Rest an *Laufzeit*-Texten (Ledger-Buchungen, Content-Platzhalter) hängt an B-30 und ist ein anderer
  Korpus." B-30 war also von Anfang an die Stelle, an der diese Trennung **bestätigt**, nicht aufgelöst
  werden sollte.

## Die echte Lücke

Es gibt keine. Der vermutete Rest an unübersetzter Code-Dokumentation existiert nicht (Vollscan negativ);
die drei benannten Textkorpora sind kein Versäumnis, sondern die in `CLAUDE.md`/`docs/translate.md`
dokumentierte, bewusste Grenze der Übersetzungs-Etappe.

## Offene Punkte

1. ~~Buchungstexte sieht das Kind in der App. Englisch wäre dort womöglich falsch — dann ist das keine
   i18n-Aufgabe, sondern eine Lokalisierungs-Entscheidung.~~ → siehe Entscheidung 1.
2. ~~Ist der Rest schon erledigt (verworfen-Kandidat) oder gibt es echte Reste?~~ → siehe Entscheidung 2.

## Entscheidungen

1. **Ledger-Texte, Content-Platzhalter und interne Exceptions bleiben deutsch — keine
   Lokalisierungs-Entscheidung fällig.** `CLAUDE.md` legt das bereits fest („Strings mit Produktinhalt
   … und die Laufzeit-Diagnose … bleiben deutsch"), `docs/translate.md` führt es mit Dateibelegen aus.
   Die Frage der Idee unterstellt eine offene Entscheidung, die es nicht gibt: Der Text, den das Kind
   sieht, ist Produktinhalt (wie Seed-Vokabeln oder Enum-Werte wie `Gymnasium`) und folgt derselben Regel
   wie jeder andere deutsche Produkttext in der App — die App ist heute durchgängig deutschsprachig.
   Begründung: eine zweite, abweichende Sprachregel nur für `Reason`-Strings wäre Inkonsistenz ohne
   Nutzen, solange die Oberfläche selbst nicht mehrsprachig ist. Kosten: keine — es ändert sich nichts am
   Code. Folgeeffekt: Sollte die App irgendwann mehrsprachig werden, ist das ein eigenes, sehr viel
   größeres Vorhaben (Übersetzungs-Infrastruktur für Produkttexte zur Laufzeit, nicht Code-Doku) —
   das liegt bereits als [B-38](B-38-mehrsprachige-oberflaeche.md) im Backlog (`idee`) und ist der
   richtige Ort für diese Frage, falls sie erneut aufkommt.
2. **Story wird `verworfen`, nicht `geschaetzt`.** Der Vollscan (Ist-Stand) findet keinen einzigen
   verbliebenen deutschen `///`- oder `//`-Kommentar im Backend. Begründung: `art: Aufräumen` verlangt ein
   Ergebnis, das „so grün wie vorher" aussieht — hier ist nichts zu bauen, weil nichts fehlt. Kosten:
   keine; Nutzen: die Idee taucht nicht in jeder Sichtung erneut als vermeintlich offene Aufgabe auf.

## Akzeptanzkriterien

Entfällt — die Story wird `verworfen`, es wird nichts gebaut. Der Beleg der Erledigung steht im
Ist-Stand (Vollscan-Ergebnis, `docs/translate.md`-Gesamtstand, Datei:Zeile-Liste der bewusst deutschen
Strings).

## Schätzung

Entfällt (`verworfen`) — kein Angriffsplan, keine Größe. Sollte die Grenzziehung aus Entscheidung 1
irgendwann revidiert werden (echte Mehrsprachigkeit), gehört die Schätzung zu B-38, nicht hierher.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert, gegrillt und geschätzt in einem Durchgang: Vollscan über alle fünf
  Backend-Projekte findet keinen verbliebenen deutschen Doku-Kommentar; die drei genannten Korpora sind
  als bewusst-deutscher Produktinhalt bzw. Laufzeit-Diagnose gegen `CLAUDE.md`/`docs/translate.md`
  bestätigt. Beide offenen Punkte in nummerierte Entscheidungen überführt, Ergebnis `verworfen`
  (autonom getroffen/geprüft, Nutzerauftrag 2026-08-04).
