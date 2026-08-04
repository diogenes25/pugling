---
tags: [typ/story, status/verworfen, bereich/frontend, bereich/backend, rolle/student, rolle/supervisor]
aliases: [Mehrsprachigkeit, i18n, Oberflächensprache, Deutsch Englisch Französisch]
status: verworfen
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nutzer, Sitzung 2026-07-31
grund: "geteilt — ein Programm, keine Story (Entscheidung 8); siehe die recherchierte Grundlage unten und
  die drei Teilstorys"
ersetzt_durch: [B-85, B-86, B-87]
---

# B-38 · Mehrsprachige Oberfläche (Deutsch, Englisch, Französisch)

Die App spricht heute ausschließlich Deutsch: `frontend/index.html:2` steht hart auf `lang="de"`, es gibt
**keine** i18n-Bibliothek, und jeder Oberflächentext liegt als deutsches Literal im TSX bzw. in
`src/lib/fieldHelp.ts`. Die Idee: die Oberfläche in **Deutsch, Englisch und Französisch** anbieten, mit
diesen drei als erster Ausbaustufe. Die Recherche zu dieser Stufe zeigt: der volle Umfang ist ein
**Programm**, keine Story — siehe Entscheidung 8.

## User Story

> Als **Nutzer** (Vater, Lehrer oder Kind) möchte ich die App **in meiner Sprache** bedienen — Deutsch,
> Englisch oder Französisch —, damit ich sie überhaupt verstehe und nicht an der Sprache der Oberfläche
> scheitere, statt am Lernstoff.

Feiner geschnitten, weil die drei Rollen unterschiedliche Gründe haben:

> Als **Vater** möchte ich die Verwaltung in meiner Muttersprache bedienen, damit ich beim Zuweisen von
> Pflichten und beim Deuten des Lernstands keine Fachbegriffe in einer Fremdsprache raten muss.
>
> Als **Lehrer** möchte ich Material in der Sprache meines Kollegiums pflegen, damit ein geteilter Katalog
> über Sprachgrenzen hinweg brauchbar ist.
>
> Als **Kind, das Französisch lernt**, möchte ich meine Arcade **auf Französisch** stellen können, damit die
> Sprache im Alltag vorkommt und nicht nur in der Übung — Immersion als Lerneffekt, nicht als Einstellung.

## Ist-Stand am Code

- **Keine i18n-Bibliothek eingebunden.** `frontend/package.json` (dependencies + devDependencies, geprüft
  per grep) enthält weder `react-i18next`/`i18next` noch `react-intl`/`formatjs` noch `lingui`. Nichts
  davon steckt auch nur transitiv im Lockfile.
- **`<html lang="de">` ist hart codiert** (`frontend/index.html:2`) — es gibt keinen Mechanismus, der das
  Attribut mit einer Sprachwahl mitführt.
- **Kein Sprachfeld am Konto.** `backend/Pugling.Api/Models/AdminEntities.cs` (Klassen `Adult`, `Child`,
  Zeilen 43ff./63ff.) trägt kein `Locale`/`Language`-Feld; grep auf `Locale|Language|CultureInfo` über die
  Datei ergibt null Treffer.
- **Der Textkorpus ist groß.** Grobe, aber belastbare Zählung über `frontend/src/**/*.tsx` (79 Dateien):
  ~1052 JSX-Textknoten mit alphabetischem Inhalt ≥3 Zeichen, ~659 Stringliterale mit deutschen Umlauten in
  `.ts`/`.tsx`-Dateien insgesamt. Beide Zahlen sind Ober- bzw. Unterschätzungen derselben Grundmenge (der
  Regex trifft nicht jeden Fall exakt einmal), zusammen aber eindeutig eine Größenordnung von **mehreren
  hundert bis über tausend Einzeltexten** — nicht „ein paar Dutzend Labels".
- **Verteilt über zwei getrennte Produkt-Oberflächen** mit unterschiedlichem Ton: `frontend/src/vater/`
  40 `.tsx`-Dateien (sachliche Verwaltung), `frontend/src/sohn/` 12 `.tsx`-Dateien (Arcade, kindgerecht),
  `frontend/src/components/` 20 `.tsx`-Dateien (von beiden geteilt), `frontend/src/lib/` 3 weitere.
- **`frontend/src/lib/fieldHelp.ts` ist 305 Zeilen lang** — laut `frontend/CLAUDE.md` bewusst „lange
  Fließtexte, nicht Labels"; das ist ein anderer Übersetzungs-Qualitätsanspruch als ein Button-Text.
- **Ein Teil der Oberflächensprache kommt schon heute vom Server**, und zwar hart deutsch:
  `backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs` liefert für alle 12 eingebauten Übungstypen einen
  deutschen `DisplayName` im Code — Zeilen 22 (`"Leseverständnis"`), 39 (`"Hörverständnis"`), 65
  (`"Aufsatz"`), 78 (`"Grammatik"`), 97 (`"Übersetzung"`), 114 (`"Birkenbihl"`), 132 (`"Lückentext"`), 188
  (`"Zuordnung"`), 225 (`"Rechenaufgaben"`), 265 (`"Rechen-Drill"`), 304 (`"Liste"`). Ausgeliefert über
  `GET creator/exercise-types`; das Frontend liest den Namen laut `frontend/CLAUDE.md` roh durch
  (`src/lib/exerciseTypes.ts`), „**nicht** aus einer Tabelle im Frontend". Das ist die reale
  Architekturfrage dieser Story, keine Nebensache.
- **236 Test-Assertions hängen am deutschen Wortlaut.** 10 Playwright-Specs unter `frontend/e2e/` plus
  weitere Component-/Hook-Tests unter `frontend/src/` (18 Dateien insgesamt) enthalten zusammen rund 236
  Treffer für `getByText(...)`/`getByRole(..., { name: "…" })` gegen deutschen UI-Text.
- **`ApiErrors.detail` ist bereits Englisch** (CLAUDE.md-Konvention, memory
  `api-fehlermeldungen-englisch.md`) — das ist Diagnosetext für Entwickler/i18n-Vorbereitung, **keine**
  UI-Chrome und nicht Teil dieser Lücke.
- **Ledger-/Content-Platzhaltertexte** (`ScoringService`-Buchungstexte, `(Vokabel '…' fehlt)` in
  `ExerciseContentResolver`) bleiben bewusst deutsch/Produktinhalt und sind eigenständig in
  [B-30](B-30-i18n-rest.md) verortet — nicht Teil dieser Story.

## Die echte Lücke

Nicht „Texte austauschen", sondern fünf voneinander unabhängige Baustellen gleichzeitig:

1. Eine **fehlende Laufzeit-Infrastruktur** — es existiert schlicht keine i18n-Bibliothek, kein
   Übersetzungsschlüssel-Format, kein Lade-/Fallback-Mechanismus.
2. Ein **Textkorpus in der Größenordnung des bereits gelaufenen Backend-Doku-Übersetzungsprogramms**
   ([docs/translate.md](../translate.md): ~2650 Kommentarzeilen über 239 Dateien, neun Etappen, ein
   eigener Branch) — aber diesmal mit **Laufzeit-Rendering statt reinen Kommentaren**, über **zwei**
   separate Produkt-Oberflächen mit unterschiedlichem Ton, und mit einem Fließtext-Anteil
   (`fieldHelp.ts`), der Übersetzungsqualität statt mechanischer Wort-für-Wort-Ersetzung verlangt.
3. Eine echte **Architekturentscheidung**: Das Übungstyp-Manifest liefert Anzeigenamen als *Daten vom
   Server*, nicht als UI-Text im Frontend. Ohne eine Umstellung auf Schlüssel bleibt dieser Pfad ein
   blinder Fleck, der jede sonst vollständige Frontend-Übersetzung sichtbar unterläuft (das Kind sieht
   „Leseverständnis" auf einer sonst französischen Arcade).
4. **236 Testassertions**, die am deutschen Wortlaut hängen und mit jeder Extraktion in eine i18n-Schicht
   zur beweglichen Fläche werden.
5. Eine fehlende Grundlage: **keine** Entscheidung und **kein** Feld dafür, ob die Sprachwahl client- oder
   kontogebunden lebt — und der im Produkt übliche Fall „Vater Deutsch, Kind Französisch, gleicher
   Haushalt" spricht gegen eine globale Einstellung.

## Offene Punkte

- ~~Wie groß ist der Textkorpus?~~ → siehe Ist-Stand am Code; Empfehlung: das ist die zentrale Zahl hinter
  Entscheidung 8 (Split).
- ~~Welche Texte sind Nutzerdaten und dürfen NICHT übersetzt werden?~~ → siehe Entscheidung 2.
- ~~Wo lebt die Sprachwahl?~~ → siehe Entscheidung 3.
- ~~Was ist mit den serverseitig deutschen Texten?~~ → siehe Entscheidung 4 (bleibt B-30, unabhängig).
- ~~Das Server-Manifest liefert deutsche Anzeigenamen — wie damit umgehen?~~ → siehe Entscheidung 5.
- ~~Plural, Zahlen, Datum?~~ → siehe Entscheidung 6.
- ~~Barrierefreiheit (`<html lang>`)?~~ → siehe Entscheidung 7.
- ~~Ist das eine Story oder ein Programm?~~ → siehe Entscheidung 8: **ein Programm. Muss geteilt werden.**

## Entscheidungen

1. **Der Umfang ist belegt groß, nicht nur vermutet groß.** Grundlage: ~1052 JSX-Textknoten / ~659
   umlauthaltige Stringliterale über 79 `.tsx`-Dateien, 305 Zeilen Fließtext in `fieldHelp.ts`, zwei
   getrennte Produkt-Oberflächen (Vater-Web 40 Dateien, Sohn-Arcade 12 Dateien, geteilte Komponenten 20).
   Kosten: keine — das ist die Recherche, die diese Stufe verlangt, kein zusätzlicher Aufwand.

2. **Nur UI-Chrome wird übersetzt, Nutzerdaten nicht.** Missions-/Auszeichnungstitel, Shop-Artikel, Plan-
   und Übungstitel gibt der Vater selbst ein und bleiben unverändert. Begründung: eine i18n-Schicht über
   selbst eingegebenem Text wäre falsch (der Vater erwartet sein eigenes Wort wieder, nicht eine
   Übersetzung davon). Kosten: **keine automatische Erkennung** — jede Komponente, die Text zeigt, muss
   im Einzelfall zwischen „Chrome" und „Nutzerdaten" unterschieden werden; das ist manuelle
   Sorgfaltsarbeit über den ganzen Korpus, kein mechanischer Fund-Ersetze-Lauf.

3. **Sprachwahl lebt zuerst client-seitig (kein Server-Feld, keine Migration).** Begründung: ein Feld an
   `Adult`/`Child` wäre eine Migration **und** eine Vertragsänderung, bevor auch nur eine Zeile Text
   übersetzt ist — das ist der falsche erste Schnitt. Kosten: Die Wahl geht bei Geräte-Wechsel oder
   mehreren Geräten verloren; „Vater Deutsch, Kind Französisch am selben Gerät" wird über getrennte
   Browser-Profile/Logins gelöst, nicht über ein Server-Feld. Eine spätere Teilstory kann das Feld
   nachziehen, sobald der Bedarf (z. B. Server-seitig lokalisierte Ledger-Texte) real ansteht.

4. **B-30 bleibt eigenständig.** Diese Story fasst das Backend nur dort an, wo die UI-Sprache direkt
   gekoppelt ist (Punkt 5, Übungstyp-Manifest) — nicht die Ledger-/Content-Platzhaltertexte. Begründung:
   andere Zielgruppe (das Kind sieht Ledger-Text als Produktinhalt, nicht als Chrome) und andere Frage
   (Lokalisierung statt reiner Übersetzung, wie B-30 selbst schon vermerkt). Kosten: keine zusätzlichen —
   B-30 bleibt offen und unabhängig schätzbar.

5. **Das Übungstyp-Manifest muss auf Schlüssel statt Anzeigename umgestellt werden.** Begründung: Solange
   `BuiltInExerciseTypes.cs` einen fest-deutschen `DisplayName` ausliefert, bleibt jede sonst vollständige
   Frontend-Übersetzung an dieser Stelle löchrig — das Kind sähe „Leseverständnis" mitten in einer
   französischen Arcade. Kosten: **Vertragsbruch** — `ExerciseTypeResponse` (oder wie das DTO im Vertrag
   heißt) ändert sich brechend, `Pugling.Client`, das Frontend (`src/lib/exerciseTypes.ts`) und ggf. der
   KI-Creator-Agent (liest Anzeigenamen für Prompts) müssen nachgezogen werden. Das ist selbst schon eine
   eigene Teilstory, keine Nebenarbeit einer Frontend-Extraktion.

6. **Pluralisierung/Zahlen/Datum über eine etablierte Bibliothek (react-i18next mit ICU-Plural-Support),
   kein Eigenbau.** Begründung: Französisch hat andere Pluralregeln als Deutsch/Englisch, eine
   selbstgebaute Ersetzung würde das falsch machen. Kosten: eine neue Laufzeit-Abhängigkeit — die
   Peer-Konflikt-Historie mit `vite-plugin-pwa`/`vite@8` (vgl. [B-25](B-25-vite-pwa-peer-konflikt.md))
   muss vor der Wahl der konkreten Bibliothek geprüft werden.

7. **`<html lang>` wandert mit der Sprachwahl.** Begründung: Barrierefreiheit — ein Screenreader liest
   sonst französischen Text mit deutscher Aussprache vor. Kosten: gehört zwingend in die
   Infrastruktur-Etappe, ist aber selbst klein (ein `useEffect`, der das Attribut setzt).

8. **B-38 ist ein Programm, kein Story-Zuschnitt — muss geteilt werden. Kein `geschaetzt` in diesem
   Durchgang.** Begründung: Der Vergleichsfall im eigenen Repo ist das Backend-Doku-Übersetzungsprogramm
   ([docs/translate.md](../translate.md)) — das brauchte für **nur** ~2650 reine Kommentarzeilen, **eine**
   Zielsprache (Englisch) und **keine** Laufzeit-Bibliothek, **keine** Vertragsänderung und **keine**
   Testkopplung neun Etappen und einen eigenen Branch. B-38 verlangt gleichzeitig: (a) eine neue
   Laufzeit-Abhängigkeit einbinden und gegen die bestehende Peer-Konflikt-Historie prüfen, (b) einen
   Textkorpus in ähnlicher Größenordnung, aber als **Laufzeit-Rendering** statt Kommentaren, über **zwei**
   separate Produkt-Oberflächen mit unterschiedlichem Ton extrahieren, (c) 305 Zeilen
   Fließtext-Übersetzung mit echtem Qualitätsanspruch statt mechanischer Muster-Ersetzung, (d) einen
   Vertragsbruch im Übungstyp-Manifest (Entscheidung 5) mit Folgeänderungen in drei weiteren Projekten,
   (e) mindestens zwei Zielsprachen mit unterschiedlichen Pluralregeln, (f) 236 Testassertions umstellen.
   Das sprengt den `L`-Anker dieses Bereichs (**eine einzelne** DB-Umbau-Etappe wie E6) um ein Vielfaches
   — `L` künstlich anzusetzen würde eine Vollständigkeit versprechen, die eine Sitzung nicht liefert.
   Kosten dieser Entscheidung: **kein** `groesse`/`wo`/`migration`/`vertragsbruch` und **keine**
   Akzeptanzkriterien für den vollen Scope in diesem Durchgang; stattdessen unten (Abschnitt „Empfohlener
   erster Schnitt") ein konkreter, realistisch schätzbarer Zuschnitt für die **erste** Teilstory. Das
   eigentliche Teilen (`B-38` → `verworfen`, `grund: geteilt`, neue `B-nn`-Ids mit `quelle: B-38`) ist
   mechanische Backlog-Pflege und bewusst **nicht** Teil dieses Durchgangs (Auftrag: „nur diese eine Datei
   anfassen").

## Empfohlener erster Schnitt (Grundlage für die erste Teilstory)

Realistischer, session-großer Zuschnitt statt „alle drei Sprachen vollständig": **i18n-Infrastruktur +
Sohn-Arcade + eine Zusatzsprache (Englisch) als Machbarkeitsnachweis.** Begründung für genau diesen
Zuschnitt: die Sohn-Arcade ist mit 12 `.tsx`-Dateien der kleinere der beiden Frontend-Bereiche, trägt die
stärkste inhaltliche Begründung der User Story (Immersion als Lerneffekt) und berührt `fieldHelp.ts` kaum
(die langen Fließtexte hängen überwiegend am Vater-Web). Explizit **außerhalb** dieses ersten Schnitts:
Vater-Web (40 Dateien), Französisch, die Manifest-Schlüssel-Umstellung (Entscheidung 5, eigener
Vertragsbruch), ein Server-Sprachfeld.

## Akzeptanzkriterien

Für die empfohlene erste Teilstory, nicht für den vollen B-38-Scope (siehe Entscheidung 8):

1. Eine i18n-Bibliothek mit Plural-Unterstützung (react-i18next o. ä., gegen die Peer-Konflikt-Historie
   geprüft) ist eingebunden; ein rein client-seitiger Sprachumschalter (kein Server-Feld) ist in der
   Sohn-Arcade sichtbar.
2. Jeder UI-Chrome-Text unter `frontend/src/sohn/` sowie der von der Arcade genutzten geteilten
   Komponenten ist extrahiert und liegt vollständig auf Deutsch **und** Englisch vor.
3. `<html lang>` wechselt mit der gewählten Sprache mit (Entscheidung 7).
4. Nutzerdaten (Missions-/Auszeichnungstitel, Shop-Artikel, Plan-/Übungstitel) bleiben unübersetzt
   (Entscheidung 2).
5. Die betroffenen Playwright-/Vitest-Assertions, die heute an deutschem Wortlaut hängen, sind auf die
   neue Struktur umgestellt (z. B. stabile `data-testid`/Rollen statt Text-Matching, oder die Locale ist
   in den Tests explizit auf Deutsch gepinnt) und bleiben grün.
6. Die Übungstyp-Anzeigenamen aus dem Server-Manifest bleiben in dieser Teilstory ausdrücklich **noch
   Deutsch** (Entscheidung 5 ist eine eigene Folge-Teilstory) — das ist eine benannte, keine übersehene
   Lücke.

## Schätzung

**Kein `groesse` in diesem Durchgang** — siehe Entscheidung 8. `wo`, `migration`, `vertragsbruch` bleiben
aus demselben Grund offen; sie werden erst gesetzt, wenn die Teilstorys aus dem Split real angelegt und
einzeln geschätzt sind.

Zur Einordnung dennoch eine grobe Schätzung der empfohlenen **ersten Teilstory** (Abschnitt oben): das läge
in der Nähe von `L` (Bibliotheks-Integration + Extraktion über ~32 Dateien + Testumstellung), aber erst
nach dem eigentlichen Zuschnitt verbindlich zu setzen.

**Risiken** (gelten für das ganze Programm):

- Die Peer-Konflikt-Historie (`vite-plugin-pwa` ↔ `vite@8`, [B-25](B-25-vite-pwa-peer-konflikt.md)) kann
  die Wahl der i18n-Bibliothek einschränken.
- 236 Testassertions gegen deutschen Wortlaut sind eine stille Fläche — ein Umbau, der sie nicht
  systematisch mitzieht, hinterlässt rote Tests weit nach der eigentlichen Übersetzungsarbeit.
- `fieldHelp.ts` verlangt echte Übersetzungsqualität (Fließtext), keine mechanische Ersetzung — das ist
  der teuerste Teil pro Zeile, nicht der größte in der Zeilenzahl.
- Die Manifest-Umstellung (Entscheidung 5) ist ein Vertragsbruch mit Streuwirkung in drei weiteren
  Projekten (`Contracts`, `Client`, `Agent.Creator`) — wird sie vergessen, bleibt eine sichtbare deutsche
  Lücke in jeder sonst übersetzten Oberfläche.

**Angriffsplan** (Reihenfolge über das ganze Programm, Backend zuerst wo es eine Rolle spielt):

1. Split ausführen (`/backlog`-Pflege: `B-38` → `verworfen, grund: geteilt`, neue Ids anlegen).
2. Erste Teilstory zuerst die Manifest-Frage (Entscheidung 5) **oder** bewusst zurückstellen — das ist
   selbst eine zu treffende Entscheidung beim Zuschnitt der Teilstorys, nicht hier vorwegzunehmen.
3. Danach Infrastruktur + Sohn-Arcade + Englisch (empfohlener erster Schnitt oben).
4. Danach Vater-Web, danach Französisch, danach ein Server-Sprachfeld (falls der Bedarf real ansteht).

**Testweg**: noch nicht final — hängt am Ausgang von Akzeptanzkriterium 5 (welche Test-Selektor-Strategie
gewählt wird). Für die erste Teilstory mindestens: die 10 bestehenden Playwright-Specs unter
`frontend/e2e/` bleiben grün (Deutsch als Standardsprache gepinnt), plus ein neuer E2E-Fall, der die
Sohn-Arcade auf Englisch umschaltet und mindestens einen übersetzten Text prüft.

## Verlauf

- **2026-07-31** — vom Nutzer direkt aufgenommen (ungeprüft). Die User Story ist auf Wunsch schon
  formuliert; das macht die Story **nicht** `ausformuliert` — dafür fehlen der belegte Ist-Stand am Code
  (Textkorpus, Manifest-Kopplung) und die Akzeptanzkriterien.
- **2026-08-03** — ausformuliert: Ist-Stand gegen den echten Code belegt (keine i18n-Lib, `lang="de"`
  hart, ~1052/~659 deutsche Textfundstellen über 79 `.tsx`-Dateien, 305 Zeilen `fieldHelp.ts`, 12 fest
  deutsche Anzeigenamen im Übungstyp-Manifest, kein Sprachfeld an Adult/Child, 236 testgebundene
  Text-Assertions). Autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-03** — gegrillt: alle offenen Punkte in nummerierte Entscheidungen überführt (1–8); Ergebnis
  von Entscheidung 8: **B-38 ist ein Programm und muss geteilt werden** — der Vergleich mit dem
  neun-Etappen-Aufwand von `docs/translate.md` (dort nur Kommentare, eine Sprache, keine
  Laufzeit-Bibliothek, kein Vertragsbruch) trägt diesen Schluss. Story bleibt bewusst auf `gegrillt`
  stehen, **kein** `geschaetzt` in diesem Durchgang — ein empfohlener, realistisch schätzbarer erster
  Schnitt (Infrastruktur + Sohn-Arcade + Englisch) ist als Grundlage für die künftige erste Teilstory
  dokumentiert. Autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-03** — geteilt: `status: verworfen`, `grund: geteilt`. Drei Teilstorys angelegt, alle mit
  `quelle: B-38`: [B-85](B-85-i18n-infrastruktur-sohn-arcade-englisch.md) (Infrastruktur + Sohn-Arcade +
  Englisch, direkt `geschaetzt` übernommen — der empfohlene erste Schnitt oben war bereits vollständig
  recherchiert), [B-86](B-86-uebungstyp-manifest-anzeigenamen-schluessel.md) (Übungstyp-Manifest auf
  Schlüssel umstellen, Entscheidung 5, ebenfalls direkt `geschaetzt`), [B-87](B-87-vater-web-franzoesisch-server-sprachfeld.md)
  (Vater-Web, Französisch, Server-Sprachfeld — bewusst auf `idee` belassen, da B-38 diesen Rest nur
  benannt, nicht recherchiert hat). Autonom getroffen, Nutzerauftrag 2026-08-04.
