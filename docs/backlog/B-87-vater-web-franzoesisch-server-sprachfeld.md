---
tags: [typ/story, status/verworfen, bereich/frontend, bereich/backend]
aliases: [Vater-Web Übersetzung, Französisch, Server-Sprachfeld]
status: verworfen
prio: P4
art: Wunsch
quelle: B-38 (geteilt)
grund: "geteilt — selbst noch ein Programm (Entscheidung 1); siehe die recherchierte Grundlage oben und die
  drei Teilstorys"
ersetzt_durch: [B-91, B-92, B-90]
---

# B-87 · Rest des Mehrsprachigkeits-Programms: Vater-Web, Französisch, Server-Sprachfeld

Dritter, bewusst noch unausformulierter Teil aus dem geteilten [B-38](B-38-mehrsprachige-oberflaeche.md):
alles, was nach der ersten Teilstufe ([B-85](B-85-i18n-infrastruktur-sohn-arcade-englisch.md) — Infrastruktur
und Sohn-Arcade auf Englisch, `geschaetzt`, Größe L) und der zweiten Teilstufe
([B-86](B-86-uebungstyp-manifest-anzeigenamen-schluessel.md) — Übungstyp-Manifest-Schlüssel, `geschaetzt`,
Größe M) noch offen bleibt: das größere Vater-Web (40 `.tsx`-Dateien, inkl. der 305 Fließtextzeilen in
`fieldHelp.ts`), Französisch als zweite Zielsprache, und ein mögliches Server-Sprachfeld an `Adult`/`Child`.

## User Story

> Als **Vater** möchte ich die Verwaltung in meiner Muttersprache bedienen, damit ich beim Zuweisen von
> Pflichten und beim Deuten des Lernstands keine Fachbegriffe in einer Fremdsprache raten muss.
>
> Als **Kind, das Französisch lernt**, möchte ich meine Arcade **auf Französisch** stellen können, damit die
> Sprache im Alltag vorkommt und nicht nur in der Übung.

(Übernommen aus [B-38](B-38-mehrsprachige-oberflaeche.md), auf die für diese Story relevanten zwei Rollen
zugeschnitten — die Lehrer-Rolle aus B-38s dritter User-Story-Zeile betrifft dieselben Vater-Web-Dateien und
ist hier mitgemeint, nicht separat aufgeführt.)

## Ist-Stand am Code

Frisch gegen den heutigen Code geprüft (nicht aus B-38 übernommen — genau das war der Auftrag dieses
Durchgangs):

- **Der Vater-Web-Korpus ist unverändert bei 40 `.tsx`-Dateien** (`frontend/src/vater/*.tsx`, direkt
  gezählt: 39 Produktionsdateien + `ClozeTexts.test.tsx`). Das deckt sich exakt mit B-38s eigener Zählung
  vom 2026-07-31 — der Dateibestand ist seither **nicht gewachsen**.
- **Die im Auftrag genannte Prämisse „seit B-38 sind mindestens zwei neue Vater-Web-Seiten dazugekommen"
  ist widerlegt.** `git log --follow --diff-filter=A` zeigt für sowohl `VaterFachlehrer.tsx` als auch
  `VaterLehrwerke.tsx` denselben Commit `a3a83c6` vom **2026-07-26** („Vater-Web vervollständigt:
  Registrierung, Kind-Stammdaten, Übungs-Bearbeitung, Lernstand, Ziele") — fünf Tage **vor** B-38s
  Recherche (2026-07-31). Beide Dateien waren also bereits in B-38s Zählung von 40 Dateien enthalten, nicht
  neu seither. Die Stories [B-63](B-63-lehrwerk-hierarchie.md), [B-64](B-64-textbook-vs-series.md) und
  [B-67](B-67-fachlehrer-aus-lehrwerk.md), die den Inhalt dieser beiden Seiten vertiefen sollen, stehen
  laut Backlog-Index weiterhin auf `ausformuliert` — **nicht gebaut**, sie haben den Korpus (noch) nicht
  vergrößert. Das ändert am Kernbefund nichts (siehe nächster Punkt), korrigiert aber die Argumentationslinie:
  Größe kommt nicht aus Wachstum seit B-38, sondern war schon in B-38s eigener Zahl enthalten.
- **Der Vater-Web-Textkorpus ist ~9,3× so groß wie der bereits als `L` geschätzte Sohn-Arcade-Korpus.**
  Gemessen mit derselben Heuristik wie B-38 (Fundstellen mit deutschen Umlauten als Textmengen-Proxy):
  `frontend/src/vater/` → **1415 Treffer über 46 Dateien** (39 Produktions-`.tsx` + Tests + zwei
  `.ts`-Hilfsdateien mit Text, `navigation.ts`/`wizardFinish.ts`); `frontend/src/sohn/` → **152 Treffer über
  12 Dateien** (der komplette Scope von B-85, dort mit Größe `L` geschätzt). Das ist keine graduelle
  Verschärfung, sondern eine andere Größenordnung.
- **`frontend/src/lib/fieldHelp.ts` ist unverändert 305 Zeilen lang**, 107 Umlaut-Treffer darin — laut
  B-85s eigener Einordnung „überwiegend Vater-Web-Text", mit echtem Übersetzungsqualitätsanspruch
  (Fließtext, keine Labels) statt mechanischer Ersetzung.
- **Kein Sprachfeld an `Adult`/`Child`.** `grep` auf `Locale|Language|CultureInfo` gegen
  `backend/Pugling.Api/Models/AdminEntities.cs` ergibt weiterhin **null Treffer** — unverändert seit B-38.
- **`react-i18next`/`i18next`/`react-intl`/`formatjs`/`lingui` sind weiterhin nicht in
  `frontend/package.json`.** B-85 (die die Bibliothek einbinden soll) ist `geschaetzt`, aber noch nicht
  `in-arbeit` — es existiert also noch **keine** Infrastruktur, auf der B-87 aufbauen könnte. Jede
  Umsetzung von B-87 setzt voraus, dass B-85 zuerst gebaut ist.
- **Französisch hat eigene Pluralregeln**, die von B-38 nur benannt, nicht ausgearbeitet wurden: Anders als
  Deutsch/Englisch (Singular nur bei genau 1) zählt Französisch **0 und 1 als „one"**, erst ab 2 als
  „other" (CLDR-Pluralkategorien). B-38s Entscheidung 6 (react-i18next mit ICU-Plural-Support) deckt das
  technisch ab — die inhaltliche Übersetzungsarbeit für eine zweite Sprache verdoppelt sich dadurch aber,
  sie lässt sich nicht als Kopie der englischen Strings erledigen.

## Die echte Lücke

Nicht eine Lücke, sondern drei voneinander unabhängige, von B-38 absichtlich zurückgestellte Baustellen,
die sich in der Recherche als **einzeln bereits zu groß** herausstellen:

1. **Vater-Web-Extraktion**: derselbe Vorgang wie B-85 (Textkorpus auf Übersetzungsschlüssel umstellen,
   Sprachumschalter, `<html lang>`), aber über einen ~9,3-mal größeren Korpus, inklusive des
   qualitätsintensiven `fieldHelp.ts`-Fließtexts.
2. **Französisch als zweite Zielsprache**: ein vollständiger zweiter Übersetzungsdurchgang (nicht nur ein
   Bibliotheks-Flag) für Sohn-Arcade **und** Vater-Web, mit eigenen Pluralregeln.
3. **Server-Sprachfeld**: eine Migration und ein Vertragsbruch (`Adult`/`Child` um `Locale` erweitern), die
   B-38 selbst als „erst nötig, falls der Bedarf real ansteht" eingestuft hat — ein Bedarf, der bis heute
   nicht belegt ist (kein serverseitig ausgelieferter Text hängt heute an einer Sprachwahl, außer dem in
   B-86 separat behandelten Übungstyp-Anzeigenamen).

## Offene Punkte

- ~~Ist der Vater-Web-Korpus seit B-38 gewachsen (neue Seiten aus B-63/B-64/B-67)?~~ → widerlegt, siehe
  Ist-Stand: unverändert bei 40 Dateien, die zwei genannten Seiten existierten schon vor B-38s Zählung.
- ~~Wie groß ist der Vater-Web-Korpus im Vergleich zum bereits geschätzten Sohn-Arcade-Teil?~~ → siehe
  Ist-Stand: ~9,3× (1415 zu 152 Umlaut-Fundstellen).
- ~~Gibt es inzwischen ein Server-Sprachfeld?~~ → nein, weiterhin unverändert seit B-38.
- ~~Ist B-87 in dieser Form schätzbar oder muss sie geteilt werden?~~ → siehe Entscheidung 1: **muss
  geteilt werden.**
- ~~Wie sieht ein sinnvoller Split aus?~~ → siehe Entscheidung 2.
- ~~Ist selbst der schmalste Teilschnitt (nur Vater-Web, nur Englisch) schon schätzbar?~~ → siehe
  Entscheidung 3: vermutlich **nein**, voraussichtlich selbst noch zu groß für eine Sitzung.

## Entscheidungen

1. **B-87 ist — wie zuvor B-38 selbst — ein Programm, kein schätzbarer Story-Zuschnitt. Muss geteilt
   werden. Kein `geschaetzt` in diesem Durchgang.** Begründung: Der Vater-Web-Korpus allein ist mit ~9,3×
   der Textmenge des bereits an der `L`-Obergrenze geschätzten Sohn-Arcade-Teils (B-85) belegt größer als
   ein einzelner Anker dieses Bereichs verträgt; addiert man Französisch als zweite Zielsprache mit eigenen
   Pluralregeln und ein unabhängiges Server-Sprachfeld mit Migration und Vertragsbruch, sprengt B-87 den
   `L`-Anker in mindestens zwei unabhängigen Dimensionen gleichzeitig (Korpusgröße **und** Sprachenzahl).
   Kosten: **kein** `groesse`/`wo`/`migration`/`vertragsbruch` in diesem Durchgang; stattdessen unten ein
   konkreter Split-Vorschlag als Grundlage für den nächsten `/backlog`-Durchgang je Teilstory. Das
   eigentliche Teilen (`B-87` → `verworfen`, `grund: geteilt`, neue `B-nn`-Ids) ist mechanische
   Backlog-Pflege und bewusst **nicht** Teil dieses Durchgangs (derselbe Schnitt wie bei B-38: Auftrag war
   „nur diese eine Datei anfassen", keine neuen Ids vergeben).

2. **Konkreter Split-Vorschlag: drei Teilstorys entlang der drei unabhängigen Baustellen aus „Die echte
   Lücke".**
   - **B-91 — Vater-Web-Extraktion auf Englisch.** Derselbe Vorgang wie B-85 (Textkorpus auf
     Übersetzungsschlüssel umstellen), aber für `frontend/src/vater/` statt `frontend/src/sohn/`, inklusive
     `fieldHelp.ts`. Baut zwingend auf der von B-85 gelieferten Infrastruktur auf (Bibliothek,
     Sprachumschalter-Mechanik) — kann erst geschätzt/gebaut werden, wenn B-85 mindestens `in-arbeit` ist.
     Begründung: gleiche Sprache (Englisch) wie B-85, damit sich der Wortschatz aus fachlichen Begriffen
     (Übung, Plan, Position, Ziel …) nicht in zwei separaten Durchgängen auseinanderentwickelt. Kosten:
     nichts Neues gegenüber B-85 außer der Korpusgröße — siehe Entscheidung 3 zur Sorge, dass das selbst
     noch zu groß ist.
   - **B-92 — Französisch als zweite Zielsprache (Sohn-Arcade + Vater-Web).** Ein vollständiger zweiter
     Übersetzungsdurchgang über den gesamten dann bereits auf Schlüssel umgestellten Korpus aus B-85 **und**
     B-91, plus die französischen CLDR-Pluralregeln (0 und 1 als „one", ab 2 als „other" — anders als
     Deutsch/Englisch) in den ICU-Plural-Strings nachziehen. Begründung, warum eigenständig statt Teil von
     B-91: Übersetzungsarbeit in eine zweite Sprache ist inhaltlich (Fremdsprachenqualität) und nicht
     technisch limitiert — sie skaliert nicht mit, nur weil die Schlüssel-Infrastruktur schon steht. Kosten:
     hängt von B-85 **und** B-91 ab (die Schlüssel müssen stehen, bevor eine zweite Sprache sie befüllen
     kann) — das ist die späteste der drei Teilstorys in der Reihenfolge.
   - **B-90 — Server-Sprachfeld an `Adult`/`Child`.** Migration + additives Vertragsfeld `Locale`, damit der
     Server pro Konto weiß, in welcher Sprache er antworten soll (relevant erst, sobald tatsächlich
     serverseitig lokalisierter Text ausgeliefert wird — heute liefert außer dem in B-86 separat behandelten
     Übungstyp-Anzeigenamen kein Endpunkt sprachabhängigen Text). Begründung, warum eigenständig: technisch
     unabhängig von B-91/B-92 (reine Backend-Änderung, keine Abhängigkeit auf die Frontend-Extraktion),
     bedarfsgetrieben statt vorgezogen (B-38 Entscheidung 3 gilt unverändert: „eine spätere Teilstory kann
     das Feld nachziehen, sobald der Bedarf real ansteht"). Kosten: bleibt am sinnvollsten auf `idee`
     stehen, bis ein konkreter Bedarf (z. B. lokalisierte Ledger-Texte, B-30) sie zur Ausformulierung
     drängt — ein vorgezogenes Ausformulieren ohne echten Konsumenten würde eine Migration rechtfertigen, die
     noch niemand braucht.

3. **Selbst der schmalste Teilschnitt (B-91, nur Vater-Web, nur Englisch, ohne Französisch, ohne
   Server-Feld) ist bei ~9,3-facher Textmenge des bereits `L`-groß geschätzten Sohn-Arcade-Teils
   vermutlich SELBST noch zu groß für eine Sitzung.** Begründung: `L` ist der höchste Anker dieses
   Bereichs, den es außer `XL` („gibt es nicht — dann wird geteilt") gibt; B-85 hat diesen Anker bereits für
   152 Umlaut-Fundstellen über 12 Dateien ausgeschöpft. 1415 Fundstellen über 39 Produktionsdateien plus
   305 qualitätsintensive `fieldHelp.ts`-Zeilen sind keine lineare Fortsetzung, sondern eine andere
   Größenordnung. Kosten: eine ehrliche Warnung, keine Vorwegnahme — der nächste `/backlog`-Durchgang auf
   B-91 sollte beim Ausformulieren **explizit prüfen**, ob auch B-91 noch einmal geteilt werden muss (z. B.
   entlang der Funktionsbereiche: Katalog/Übungen, Pläne/Positionen, Kind-Verwaltung/Konto,
   Shop/Rewards/Ziele, und `fieldHelp.ts` separat wegen seines eigenen Übersetzungsqualitätsanspruchs),
   statt das als gegeben `L` zu übernehmen, nur weil B-85 denselben Namen trug.

## Akzeptanzkriterien

Noch nicht final — abhängig vom Ausgang des Splits (Entscheidung 2) und insbesondere davon, ob B-91 beim
Ausformulieren noch einmal geteilt werden muss (Entscheidung 3). Als Entwurf für die grobe Richtung, gültig
für das gesamte Restprogramm:

1. Der komplette Vater-Web-Textkorpus (`frontend/src/vater/`, inkl. `fieldHelp.ts`) liegt auf Deutsch
   **und** Englisch vor, mit demselben Sprachumschalter-Mechanismus wie die Sohn-Arcade aus B-85.
2. Sohn-Arcade **und** Vater-Web liegen zusätzlich auf Französisch vor, inklusive korrekter
   Pluralformen (CLDR-Kategorien „one"/„other" für Französisch, nicht die deutsch/englische Zählweise).
3. Nutzerdaten (Missions-/Auszeichnungstitel, Shop-Artikel, Plan-/Übungstitel) bleiben unübersetzt
   (unverändert aus B-38 Entscheidung 2 / B-85 Entscheidung 3).
4. Ein Server-Sprachfeld an `Adult`/`Child` existiert **nur**, falls B-90 vorher gegen einen echten Bedarf
   ausformuliert wurde — kein Vorgriff ohne Konsument.
5. Alle heute an deutschem Wortlaut hängenden Playwright-/Vitest-Assertions über Vater-Web-Seiten sind auf
   die neue Struktur umgestellt und bleiben grün.

## Schätzung

**Kein `groesse` in diesem Durchgang** — siehe Entscheidung 1. `wo`, `migration`, `vertragsbruch` bleiben
aus demselben Grund offen; sie werden erst gesetzt, wenn B-91/B-92/B-90 real angelegt und einzeln geschätzt
sind.

**Risiken** (gelten für das ganze Restprogramm):

- Der Übersetzungsqualitätsanspruch von `fieldHelp.ts` (305 Zeilen Fließtext) ist pro Zeile der teuerste
  Teil des Korpus, nicht der größte in der Zeilenzahl — das gilt für Englisch **und** noch einmal separat
  für Französisch.
- Ohne B-85/B-86 in `in-arbeit`/`abgenommen` hat B-91 keine Infrastruktur, auf der es aufbauen kann — eine
  harte Reihenfolge-Abhängigkeit, kein bloßer Vorschlag.
- Französische Pluralregeln unterscheiden sich strukturell von Deutsch/Englisch (0 **und** 1 als „one");
  eine Übersetzung, die das übersieht, erzeugt grammatisch falsche UI-Texte, keinen offensichtlichen Fehler.
- Ein Server-Sprachfeld (B-90), das vorgezogen wird, obwohl kein Endpunkt sprachabhängigen Text ausliefert,
  wäre eine Migration ohne Konsumenten — genau die Art Vorgriff, die B-38 Entscheidung 3 schon einmal
  ausdrücklich vermieden hat.

**Angriffsplan** (Reihenfolge über das Restprogramm):

1. B-85 fertigstellen (Infrastruktur + Sohn-Arcade Englisch) — Voraussetzung für alles Weitere hier.
2. B-91 ausformulieren; dabei prüfen, ob eine weitere Teilung nötig ist (Entscheidung 3), dann bauen.
3. B-92 (Französisch, Sohn-Arcade + Vater-Web) erst danach — sie braucht die Schlüssel aus B-85 **und**
   B-91 als Grundlage.
4. B-90 (Server-Sprachfeld) unabhängig und bedarfsgetrieben, sobald ein realer serverseitiger
   Lokalisierungsbedarf ansteht — keine feste Position in dieser Reihenfolge.

**Testweg**: noch nicht final, hängt an der Testselektor-Strategie, die B-85 für die Sohn-Arcade etabliert
(gepinnte deutsche Locale als E2E-Standard, siehe B-85 Entscheidung 6) — B-91 würde dasselbe Muster auf die
Vater-Web-Playwright-Specs übertragen, B-92 auf beide Oberflächen mit einer dritten gepinnten Locale
erweitern.

## Verlauf

- **2026-08-03** — angelegt beim Teilen von [B-38](B-38-mehrsprachige-oberflaeche.md) (Entscheidung 8
  dort), bewusst auf `idee` belassen: der volle Umfang (Vater-Web-Korpus, Pluralregeln des Französischen,
  Bedarfsfrage für ein Server-Sprachfeld) ist in B-38 nur benannt, nicht recherchiert — eine ehrliche
  Ausformulierung bräuchte einen eigenen Durchgang gegen den dann aktuellen Code (insbesondere nachdem
  B-85/B-86 gebaut sind und die Infrastruktur real steht).
- **2026-08-04** — ausformuliert: Ist-Stand gegen den echten Code neu belegt. Kernbefund: Vater-Web-Korpus
  unverändert bei 40 Dateien (die im Auftrag vermutete Größenzunahme durch B-63/B-64/B-67 ließ sich nicht
  bestätigen — `VaterFachlehrer.tsx`/`VaterLehrwerke.tsx` bestehen bereits seit 2026-07-26, vor B-38s
  eigener Zählung), aber ~9,3× so textreich wie der bereits `L`-groß geschätzte Sohn-Arcade-Teil (B-85):
  1415 zu 152 Umlaut-Fundstellen. `fieldHelp.ts` unverändert 305 Zeilen. Kein Sprachfeld an Adult/Child,
  keine i18n-Bibliothek eingebunden (B-85 selbst noch nicht gebaut). Autonom getroffen, Nutzerauftrag
  2026-08-04.
- **2026-08-04** — gegrillt: Entscheidung 1 — **B-87 ist selbst noch ein Programm und muss geteilt
  werden**, kein `geschaetzt` in diesem Durchgang. Konkreter Split-Vorschlag (Entscheidung 2): B-91
  (Vater-Web-Extraktion Englisch, baut auf B-85 auf), B-92 (Französisch für Sohn-Arcade + Vater-Web, baut
  auf B-85 **und** B-91 auf), B-90 (Server-Sprachfeld, unabhängig, bedarfsgetrieben). Zusätzliche Warnung
  (Entscheidung 3): selbst B-91 allein ist bei 9,3-facher Korpusgröße gegenüber dem bereits `L`-groß
  geschätzten B-85 vermutlich noch zu groß für eine Sitzung und sollte beim nächsten Ausformulieren erneut
  auf Teilungsbedarf geprüft werden. Das eigentliche Teilen (neue `B-nn`-Ids) ist bewusst nicht Teil dieses
  Durchgangs. Autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-04** — geteilt: `status: verworfen`, `grund: geteilt`. Drei Teilstorys angelegt, alle mit
  `quelle: B-87`: [B-91](B-91-vater-web-extraktion-englisch.md) (Vater-Web-Extraktion Englisch),
  [B-92](B-92-franzoesisch-zweite-zielsprache.md) (Französisch für Sohn-Arcade + Vater-Web),
  [B-90](B-90-server-sprachfeld.md) (Server-Sprachfeld, bedarfsgetrieben). Alle drei bewusst auf `idee`
  belassen statt vorschnell geschätzt — B-87s eigene Entscheidung 3 warnt, dass selbst B-91 allein
  wahrscheinlich noch zu groß ist und eine eigene Recherche zum Teilungsbedarf braucht, bevor eine ehrliche
  Schätzung möglich ist. Autonom getroffen, Nutzerauftrag 2026-08-04.
