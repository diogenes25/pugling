---
tags: [typ/protokoll, bereich/pm]
aliases: [PM-Sitzung 2026-11-05, Lauf T]
---

# PM-Sitzung: Backlog-Lauf nach der Delegation (Lauf T)

**Datum:** 2026-11-05  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** die 37 autonom bearbeitbaren Stories aus der [Backlog-Sichtung vom selben Tag](pm-sitzung-2026-11-05-delegation.md)
in Sprints abarbeiten, bis ein Limit greift.

**Auftrag:** im Dialog („Ja, loslegen"), nach der Delegation „übernimm meine Position und entscheide nach bestem Wissen und
Gewissen". Produktfragen, die ein Bau aufwirft, entscheidet Claude und protokolliert sie als „Entscheidung im Auftrag des
Nutzers". Sonst gelten die Freigaben 2–10 aus [nachtlauf.md](nachtlauf.md): Fünfer-Zähler je Sprint, Retro schlägt nur vor,
Commits selbst, Push beim Nutzer.

## Sprint 1

**Sprint-Ziel (Creator und Sohn):** „Wer auf einer Liste oder in einem Formular etwas auslöst, sieht danach, was passiert ist,
und verliert seinen Platz nicht – kein stummer Knopf, keine unsichtbare Bestätigung, kein ‚keine' während noch geladen wird."

**Umfang:** B-231, B-232, B-233, B-234 (Paket NP-14, alle am 2026-10-23 vom Nutzer entschieden und freigegeben). Verfeinert in
diesem Sprint von `idee` bis `geschaetzt`; jede XS bis S, reines Frontend, keine Migration, kein Vertragsbruch.

**Entwickler-Brief:** Quelle der Wahrheit ist jeweils die Oberfläche selbst (keine API-Änderung). B-234 bekommt einen
`pending`-Zustand an `CategorySection` (`data === null || stale`); B-232 lässt die Bedingung `!meldung.ok` am Scroll-Effekt
fallen; B-233 sperrt die Speichern-Knöpfe von `SeriesForm`/`UnitForm` bei leerem Pflichtfeld und streicht die lokale
Meldung; B-231 legt den Fokus beim gesperrten „Mehr laden" ohne Fehlerkasten auf die Liste. Testweg: Vitest je Baustein mit
Mutanten, eine neue E2E für den Sichtbereich (B-232), die volle E2E-Suite als Rollengang.

**Entscheidung im Auftrag des Nutzers (beim Bau aufgetreten):** B-233 zieht die Regel nur auf der Lehrwerke-Seite nach;
die übrigen fünfzehn Formulare mit demselben Muster gehen als [B-380](backlog/B-380-pflichtfeld-sperre-in-den-uebrigen-formularen.md)
in den Backlog. Begründung: das Sprint-Ziel ist ohne sie erfüllt, und das Nachziehen trifft zehn exakte Label-Abfragen in
fremden Tests.

### Iteration 1 — umgesetzt

- **B-234** (`CatalogAdmin.tsx`): `pending` an `CategorySection`, Verdrahtung in `artenPending`; drei sichtbare Zustände.
- **B-232** (`VaterLehrwerke.tsx:155`): Scroll zum Meldungsplatz auch bei Erfolg; neue Spec `bestaetigung-scrollt-zum-platz.spec.ts`.
- **B-233** (`VaterLehrwerke.tsx`): `SeriesForm`/`UnitForm` sperren bei leerem Pflichtfeld, „(Pflicht)" am Label, stiller Riegel vor
  dem Räumen; vier E2E auf das neue Label, `bestaetigung-ueberlebt.spec.ts` Schritt 2 umgestellt; Hausregel in `frontend/CLAUDE.md`
  (Datei nach dem Sprint zunächst 42 B größer, nicht kleiner – in Sprint 2 berichtigt, siehe B-382).
- **B-231** (`SohnShop.tsx`, `index.css`): Fokus auf die Liste beim gesperrten „Mehr laden" ohne Fehlerkasten.

**Verifikation:** Vitest 648/648, `tsc -b` sauber, Playwright 110 passed (volle Suite, 6,7 min; vor den zwei Review-Fixes, die nur
eine Funktion herauslösen und Tests ergänzen). Jede neue Zusicherung mit benanntem Mutanten rot geprüft (Zahlen in den Stories).
**Review** `frontend-reviewer`: 🟡 1 Verdrahtung von `pending` ungetestet, 🟡 2 `tabIndex={-1}` ungetestet (happy-dom fokussiert
alles) – beide behoben, **Zähler Sprint 1: 2 von 5**; 🟢 3 (Rerender ohne Fallmöglichkeit, entfernt), 🟢 4 (Regel-Satz vermischt,
getrennt) ohne Zählung.

### Runde 1 — Re-Review / Abnahme

- **Creator:** „Ich sehe jetzt nach jedem Speichern im Verlags-Vokabular die Bestätigung, ohne hochzuscrollen, und ein leerer
  Reihen-Name lässt den Knopf grau – ich weiß, warum." Belegt durch die neue Spec und die vier nachgezogenen Lehrwerke-Specs im
  echten Browser. Den Arten-Ladezustand sieht er nur kurz; belegt über Vitest, nicht im Browser (ein Ladefenster wäre nur mit
  künstlich langsamem Server herstellbar).
- **Sohn:** abgenommen mit **benanntem Gerätecheck** (B-231 `wartet_auf`): „Mehr laden" per Tastatur, Fokusring an der Liste. Die
  Sohn-Specs liefen grün als Regressionszeugen.
- **Vater:** Regressionszeuge – Suite grün, kein Weg von ihm vom Diff berührt.

## Retrospektive (Sprint 1)

**Nachschau:** Lauf S, Teil 3 (`46e3871`) – B-372 per frischem `pugling-reviewer` (Suite 1073/1073), B-373 selbst per `grep`;
`nachgeschaut: 2026-11-05` an beiden. **Entgangen:** B-372s Entscheidung 2 behauptet die Vorrangfolge „Name vor PIN", kein Test trägt sie
→ [B-379](backlog/B-379-name-vor-pin-vorrang-ungetestet.md) (`entgangen_bei: [B-372]`). Index danach: Nachgeschaut 309 von 313 (die
vier offenen sind die dieses Sprints).

**Was die eigenen Tore durchgelassen haben:** zweimal dieselbe Klasse – eine Zusicherung am **Baustein**, nicht an seiner
**Verdrahtung**. In B-234 testete jeder Fall `CategorySection` mit gesetztem `pending`, keiner, wie der Aufrufer es berechnet; in
B-231 prüfte der Fokus-Fall in happy-dom, das jedes Element fokussiert, das Attribut also nicht brauchte. Beide fand der Reviewer,
kein Tor. B-379 aus der Nachschau ist eine Verwandte: eine Entscheidung nennt zwei Zweige, der Test trägt einen.

**Vorschlag (nicht gelandet, Freigabe 3):** (BG) In Step 5 von `pm-loop` ein Satz: „Wird ein Prop am Aufrufer **berechnet**, braucht
die Berechnung einen eigenen Fall (reine Funktion), sonst testet der Baustein-Fall einen Wert, den niemand liefert." Ein Tor dafür
wäre eine Regel über Prop-Ausdrücke in JSX – zu unscharf, um es zu mechanisieren; darum Prosa.

## Sprint 2

**Sprint-Ziel (Vater, für die Familie):** „Weder ein fremder Creator noch ein Fremder am Login erfährt über eine Antwort
etwas über unsere Konten oder Kinder – nicht über den Statuscode, nicht über eine bekannte PIN, nicht über die Antwortzeit."

**Umfang:** B-310, B-379, B-282 (Punkte 2 und 4). Alle `Defekt`, autonom gegrillt (Freigabe 1); in B-282 zusätzlich drei
Entscheidungen im Auftrag des Nutzers (Seed-PIN an den Betreuer, Attrappen-Ableitung, gemischte Konten nicht bauen).
B-329 (Fehlercodes im Übungspfad) bleibt draußen: anderes Thema, eigener Vertragsbruch.

**Entwickler-Brief:** Quelle der Wahrheit ist der Server. B-310 – eine Prüfung über `SupervisorOwnsChildAsync`, 404 wie der
`ChildOwnershipFilter`; der Agent fängt nur noch 404. B-379 – ein Testfall. B-282 – `SeededChildPins` nach (Betreuer-E-Mail,
Name), `PinHasher.Verify(string?)` mit Attrappe, Login-Aktionen prüfen vor der Verzweigung. Keine Migration; Vertragsbruch nur
B-310 (403 fällt weg). Testweg: Integrationstests mit Mutanten, volle Suite.

### Iteration 2 — umgesetzt

- **B-310** `CreatorProfilesController.Match`, `AgentCommands.ShowProfilesAsync`, `Pugling.Api/CLAUDE.md`, `docs/REST/Creator.http`.
- **B-379** `AccountSelfServiceTests.LeererName_MitZuKurzerPin_MeldetWeiterDenNamen`.
- **B-282** `Seed.SeededChildPin`, `PinHasher.Verify`, `AuthController` (drei Login-Aktionen); Tests in `SeedContractTests` und
  `SecurityHardeningTests` (Zeitproben je Pfad und je Login-Route).
- Nebenbei (🟢 aus der Nachschau zu Lauf S): Using-Reihenfolge in `DeletedAccountTokenTests.cs` per `dotnet format`.

**Verifikation:** `dotnet build Pugling.sln` ohne Warnung, volle Suite 1076/1076, nach den Review-Fixes 1079/1079. Sechs
Mutanten, jeder fällt genau in seinem Fall (Zahlen in den Stories). **Review** `pugling-reviewer`: keine 🔴; vier 🟡 – zwei mit
Test-Änderung behoben (**Zähler Sprint 2: 2 von 5**), zwei nur Text ohne Zählung; zwei 🟢 hingenommen (eine SQLite-Abfrage
Zeitunterschied; Seed-Test fängt das Weglassen des `StudentId`-Filters nicht).

### Runde 2 — Re-Review / Abnahme

- **Vater:** „Ein fremder Lehrer bekommt für mein Kind dieselbe Antwort wie für ein Kind, das es nicht gibt, und mein Sohn
  behält seine 1111." Belegt durch `CreatorProfileTests`/`PuglingClientTests` gegen den echten In-Process-Server und den
  Seed-Test; der Login der Familie läuft durch jede Testklasse.
- **Creator:** der Agent-Weg `profiles --child` meldet ein fremdes Kind weiter als Fehlbedienung (`CreatorAgentTests`).
- **Sohn:** Regressionszeuge – sein Login (1111) ist der Login jeder Student-Testklasse, Suite grün.

## Retrospektive (Sprint 2)

**Nachschau:** Sprint 1 (`c6876b4`) per frischem `frontend-reviewer`, read-only; `nachgeschaut: 2026-11-05` an B-231…B-234.
**Entgangen:** zwei 🟡 im Increment **dieses Laufs** – B-232s Erfolgs-Scroll springt auch für Formulare in der Tabelle
→ [B-381](backlog/B-381-erfolgs-scroll-springt-aus-dem-formular.md), und B-233 AK 3 hielt nicht (`frontend/CLAUDE.md` +42 B,
dazu vier Verweise/Zählungen) → [B-382](backlog/B-382-groessenangabe-und-zaehlungen-aus-sprint-1.md), im selben Sprint
berichtigt (nur Text, jetzt 10325 B). Zwei 🟢 (`preventScroll` an B-231s Fokus, Zeilenverweise) in B-231/B-382.

**Was die eigenen Tore durchgelassen haben:**

1. **Eine Messung vor dem letzten Edit.** Die Größe von `frontend/CLAUDE.md` wurde vor dem 🟢-Fix des Reviews gemessen und danach
   als Beleg weitergetragen – dieselbe Fehlerklasse wie „Probe gegen den alten Stand" (Memory „Wiederherstellung braucht neue Zeit").
2. **Eine Entscheidung über einen Absender, gebaut für alle.** B-232 entschied für das Verlags-Vokabular, der Bau änderte die
   Bedingung für jeden Absender der Seite. Die neue Spec fuhr genau den entfernten Fall und keinen nahen.

**Vorschläge (nicht gelandet, Freigabe 3):**

- (BH) Die Belegzeile eines AK, das eine **Größe** behauptet (Bytes, Anzahl), wird erst **nach dem letzten Edit des Sprints** und
  direkt vor dem Commit geschrieben – als letzter Schritt vor `git commit`, mit dem Befehl in der Zeile. Prosa im `pm-loop`
  Step 5; ein Tor wäre ein Skript, das `wc -c`-Behauptungen in Story-Zeilen gegen die Datei rechnet – möglich, aber für zwei Fälle
  im ganzen Backlog zu teuer.
- (BI) Ändert ein Bau eine **gemeinsame** Bedingung (ein Effekt, der für alle Absender einer Seite gilt), nennt die Story je
  Absender, ob die Entscheidung ihn meint. Prosa in `frontend/CLAUDE.md` wäre die Stelle; sie ist über Budget, darum nur Vorschlag.

## Ende des Laufs — Limit 2

Die Nachschau hat Entgleitungen im Increment **dieses** Laufs gefunden (B-381, B-382). Nach `docs/backlog/README.md` („Der
Backlog-Lauf", Limit 2) endet der Lauf mit Sprint 2, damit der Nutzer die Funde sieht, bevor weitergebaut wird.

**Stand:** 7 Stories abgenommen (B-231, B-232, B-233, B-234, B-310, B-379, B-282), dazu B-382 (nur Text) und die Nachschau
über Lauf S. Neu im Backlog: B-379 (abgenommen), B-380, B-381 (mit Entscheidung und Freigabe), B-382 (abgenommen).
Offene Gerätechecks: B-231 (Tastatur „Mehr laden", Fokusring, `preventScroll`). Retro-Vorschläge (BG), (BH), (BI) unentschieden.
Nichts gepusht.

**Nächster Start:** B-381 zuerst (P2, Defekt aus diesem Lauf), dann B-329, dann die Wünsche nach der Reihenfolge im
[Delegations-Protokoll](pm-sitzung-2026-11-05-delegation.md).

## Lauf U

Fortsetzung nach „Ja, weiter" (Dialog, 2026-11-05) unter demselben Auftrag; neuer Fünfer-Zähler je Sprint.

### Sprint 3

**Sprint-Ziel (Creator):** „Wer Units hintereinander anlegt, behält sein offenes Formular im Bild – und sieht die Bestätigung
aus dem Verlags-Vokabular trotzdem."

**Umfang:** B-381 (Defekt aus der Nachschau von Lauf T, mit Entscheidung und Freigabe). **Entwickler-Brief:** reines Frontend;
der Absender wählt über `melde`/`meldeVonWeitWeg`, ob eine Bestätigung den Platz holt; Fehler immer. Testweg: neue Spec mit
nachgerechneter Vorbedingung, B-232-Spec als Gegenprobe, volle Suite als Rollengang.

**Verifikation:** Vitest 648/648, `tsc -b` sauber, Playwright 111 passed; zwei rote Proben je genau in ihrer Spec (Zahlen in B-381).
**Review** `frontend-reviewer`: keine 🔴/🟡, drei 🟢 ohne Zählung (eins behoben, eins → [B-384](backlog/B-384-reihe-anlegen-bestaetigung-auf-dem-handy.md),
eins hingenommen). **Zähler Sprint 3: 0 von 5.**

**Abnahme:** Creator – „Ich lege vier Units am Stück an und muss nicht mehr zurückscrollen; das Umbenennen eines Verlags
bestätigt weiter sichtbar." (beide Specs im echten Browser). Vater und Sohn: Regressionszeugen, Suite grün.

### Retrospektive (Sprint 3)

**Nachschau:** Lauf T, Sprint 2 (`b32460e`) per frischem `pugling-reviewer`; `nachgeschaut: 2026-11-05` an B-310, B-282, B-379, B-382.
**Entgangen:** zwei 🟡, beide Text – die Client-Doku versprach weiter 403, und „403 fällt aus dem Dokument" stimmte nicht (das
403 bleibt für eine fehlende Creator-Rolle) → [B-383](backlog/B-383-match-403-behauptungen-nach-b-310.md), im selben Sprint berichtigt.
Die Entgleitung liegt im Increment von Lauf T, darum **kein Limit 2** für Lauf U.

**Was durchkam:** „403 fällt weg" war aus einem Diff gelesen, in dem die 403-Zeilen mit `-` standen – sie standen weiter unten
wieder mit `+`. Eine Behauptung über ein generiertes Dokument wurde am Diff statt am Ergebnis geprüft.

**Vorschlag (nicht gelandet, Freigabe 3):** (BJ) Eine Behauptung über den Vertrag („X fällt weg", „Y kommt dazu") wird an der
**Datei** belegt (`jq`/`grep` auf `docs/openapi/v1.json`), nicht am Diff. Prosa im `pm-loop` Step 5; ein Tor wäre der vorhandene
OpenAPI-Abgleich, er prüft aber nur Gleichheit mit dem Server, nicht Behauptungen in Stories.

### Sprint 4

**Sprint-Ziel (Creator und KI-Agent):** „Wer beim Anlegen einer Übung eine Id oder einen Schlüssel nennt, den es nicht gibt,
bekommt überall denselben Code – `invalid_reference` – und kann darauf verzweigen."

**Umfang:** B-329 (Defekt, autonom gegrillt). **Entwickler-Brief:** `RequestProblem` trägt den Code; nur der Code wechselt, Status
und Text bleiben (wie B-258); Regelbrüche bleiben `validation_error`; Stapelpfad unverändert. Testweg: `UnknownReferenceCodeTests`,
ein Mutant je Fundstelle, volle Suite.

**Verifikation:** volle Suite 1085/1085, `dotnet format` ohne Befund, neun Mutanten je genau in ihrem Fall rot (Zahlen in B-329).
**Review** `pugling-reviewer`: zwei 🟡 (Item-Routen – Code und Test, **Zähler Sprint 4: 1 von 5**; Vertragskommentar – nur Text),
ein 🟢 → [B-386](backlog/B-386-uebersetzung-speichert-unbekannte-vokabel-id.md).

**Abnahme:** Creator/Agent – die sechs Integrationsfälle fahren die Anlege-Wege gegen den In-Process-Server; das Frontend
verzweigt nicht auf den Code. Vater und Sohn: Regressionszeugen, Suite grün.

### Retrospektive (Sprint 4)

**Nachschau:** Lauf U, Sprint 3 (`2a9add3`) per frischem `frontend-reviewer`; `nachgeschaut: 2026-11-05` an B-381 und B-383.
**Entgangen (🟡):** B-381 nahm auch den Bestätigungen das Scrollen, deren Zeile dabei verschwindet (Reihe löschen, umbenennen mit
Umsortierung) – genau der Fall, für den B-173 den Platz gebaut hat → [B-385](backlog/B-385-loeschbestaetigung-ausser-sicht.md), mit
Entscheidung und Freigabe, **nicht** gebaut. 🟢: der Seed-Kommentar war noch zu stark, berichtigt.

**Was durchkam:** dieselbe Klasse wie B-232 → B-381, nur in die andere Richtung. Die Entscheidung über eine gemeinsame Bedingung
wurde an einem Absender gemessen (Units anlegen) und für alle gebaut. Vorschlag (BI) aus Sprint 2 hätte genau das verlangt:
„je Absender nennen, ob die Entscheidung ihn meint". Er ist unentschieden geblieben – die Retro darf ihn nicht landen.

**Vorschlag (nicht gelandet, Freigabe 3):** (BK) (BI) als **Tabelle in der Story**, nicht als Satz: jede Story, die eine gemeinsame
Bedingung einer Seite ändert, trägt eine Zeile je Absender mit „gemeint ja/nein" und dem Testfall dazu. Der Index könnte das Fehlen
prüfen, wenn die Story ein Kennzeichen `gemeinsame_bedingung: ja` trägt – ein Tor, darum nur Vorschlag.

## Ende von Lauf U — Limit 2

Die Nachschau hat eine Entgleitung im Increment **dieses** Laufs gefunden (B-385). Lauf U endet mit Sprint 4.

**Stand:** 2 Stories abgenommen (B-381, B-329), dazu B-383 (nur Text). Neu: B-384 (Frage), B-385 (Defekt, entschieden und freigegeben),
B-386 (Defekt). Retro-Vorschläge (BG)–(BK) unentschieden. Gerätecheck B-231 offen. Nichts gepusht.

**Nächster Start:** B-385, dann B-386, dann die Wünsche nach dem [Delegations-Protokoll](pm-sitzung-2026-11-05-delegation.md).

## Lauf V

Gestartet im Dialog („Ja, weiter") auf Grundlage derselben Delegation; Reihenfolge aus dem Ende von Lauf U.

### Sprint 5

**Sprint-Ziel (Creator):** Wer in einer langen Reihen-Liste eine Reihe löscht oder speichert, sieht die Bestätigung – auch wenn
die Zeile dabei verschwindet –, und wer Units anlegt, behält sein Formular weiter im Bild.
**Umfang:** [B-385](backlog/B-385-loeschbestaetigung-ausser-sicht.md) (XS, auf Grundlage der Entscheidung im Auftrag ausformuliert
und geschätzt).
**Entwickler-Brief:** `SeriesRow` bekommt einen zweiten Prop für die Handlungen, nach denen die Zeile fehlen kann, verdrahtet mit
`meldeVonWeitWeg`; Units, Umschalter und `NewSeries` bleiben bei `melde`. Testweg: eigene Spec mit nachgerechneter Vorbedingung,
die B-381-Spec als Gegenprobe. Die Absender stehen als Tabelle in der Story (freiwillig nach (BK)).

**Umgesetzt:** `frontend/src/vater/VaterLehrwerke.tsx` (Prop `onMeldungZeileWeg`, Kommentar am Effekt), neue Spec
`frontend/e2e/reihe-bestaetigung-im-bild.spec.ts` (zwei Fälle). Die Vorbedingung schlug im ersten Lauf zu Recht an (die Reihen
sortierten nach unten) – Präfix „0".
**Verifikation:** zwei rote Proben je genau im eigenen Fall, Units-Spec in beiden grün; Vitest 648/648, `tsc -b` sauber,
Playwright **113 passed** (volle Suite).
**Review** `frontend-reviewer`: Verdrahtung je Absender bestätigt; ein 🟡 – die Lösch-Vorbedingung rechnete das Nachrücken des
Platzes um die gelöschte Zeile nicht ein (Test geändert, Probe wiederholt, **Zähler Sprint 5: 1 von 5**); ein 🟢 Prop-Doku (nur Text).

**Abnahme:** Creator – die volle Suite fährt beide Wege und die Gegenprobe im echten Browser (Rollengang als E2E). Vater und Sohn:
Regressionszeugen, ihre Specs grün, kein Pfad von ihnen berührt.

### Retrospektive (Sprint 5)

**Nachschau:** Lauf U, Sprint 4 (`f4cf656`, B-329) per frischem `pugling-reviewer`, der den Commit selbst per `git archive` gebaut
und getestet hat (1085/1085); `nachgeschaut: 2026-11-05` an B-329. **Entgangen (🟢):** zwei der vier Stellen, an denen AK 1 „Id 0
bleibt `validation_error`" behauptet, haben keinen Test → [B-388](backlog/B-388-id-null-faelle-ohne-test.md). Außerhalb des Diffs:
Birkenbihl nimmt eine unbekannte Id still an → [B-387](backlog/B-387-birkenbihl-speichert-unbekannte-vokabel-id.md); Leerzeichen-
Grundform → [B-389](backlog/B-389-leerzeichen-grundform-meldet-invalid-reference.md). Index: Nachgeschaut 320 von 321.
**Limit 2 greift nicht:** die Entgleitung liegt im Increment von Lauf U, nicht von Lauf V.

**Was durchkam:** dieselbe Form wie B-381/B-385, diesmal im Test statt im Bau – eine Regel für mehrere Stellen, belegt an einem Teil
davon. (BK) hätte es gefangen, wenn die Tabelle je *Stelle* statt je *Absender* verlangt würde; das ist eine Schärfung desselben
Vorschlags, kein neuer.

**Eigener Fund in diesem Sprint, ohne Folgen:** zwei Mutanten zu B-386 kompilierten nicht (`CS0162`, Warnungen sind Fehler), und
`dotnet test --no-build` lief gegen die Assembly des vorigen Mutanten – die Ausgabe sah aus wie zwei gültige rote Proben. Bemerkt an
der identischen Fehlerzeile, beide wiederholt. Verwandt mit [[wiederherstellung-braucht-neue-zeit]].

**Vorschlag (nicht gelandet, Freigabe 3):** (BL) ein kleines Proben-Skript unter `.claude/scripts/`, das Mutant anwenden → bauen →
**bei Build-Fehler abbrechen** → gefilterten Test laufen lassen → zurückspielen und neu bauen in einem Schritt erledigt. Ein Tor im
weiteren Sinn, darum nur Vorschlag.

### Sprint 6

**Sprint-Ziel (Creator):** Keine Übersetzungsübung speichert mehr einen Verweis in den Vokabelspeicher, den es nicht gibt – und keine
Vokabel verschwindet unter einer Übersetzung weg.
**Umfang:** [B-386](backlog/B-386-uebersetzung-speichert-unbekannte-vokabel-id.md) (S, `Defekt`, im Lauf gegrillt). Birkenbihl
([B-387](backlog/B-387-birkenbihl-speichert-unbekannte-vokabel-id.md)) bleibt draußen: in der Retro von Sprint 5 gefunden, eigene
Story, damit der Sprint nicht wächst.
**Entwickler-Brief:** Übersetzung prüft `vocabularyId` wie die Vokabelübung; die Löschsperre und `usage` kennen Übersetzungen (sonst
macht die Prüfung aus einem erlaubten Löschen eine nicht mehr speicherbare Übung). Testweg: Integrationsfälle, Gegenprobe über eine
fremde Id im Satztext.

**Umgesetzt:** `TranslationController.ValidateConfigAsync`, `VocabularyStoreController.ReferencingExercisesAsync` + Doku,
`VocabUsage`-Vertragskommentar, `docs/uebergabe-vokabeln.md`; zwei Integrationsfälle.
**Verifikation:** vier rote Proben je in eigener Zeile (zwei davon wiederholt, weil der erste Mutant nicht kompilierte – siehe
Retro Sprint 5); volle Suite 1087/1087; `dotnet format` ohne Befund an berührten Dateien (ein alter WHITESPACE-Fund in
`ExerciseCatalogController.cs`, nicht berührt).
**Review** `pugling-reviewer`: kein 🔴, ein 🟡 nur Text (Vertragskommentar), drei 🟢 – **Zähler Sprint 6: 0 von 5**.

**Abnahme:** Creator – sechs E2E-Specs über Übersetzung und Vokabelspeicher, 8 passed (Rollengang als E2E; eine Übersetzung
*angelegt* in `uebungstypen.spec.ts`, das erneute Speichern mit geladener Id nur über den Integrationsfall belegt). Vater und Sohn:
Regressionszeugen, Suite grün, der Spielweg liest die Id nicht.

### Retrospektive (Sprint 6)

**Nachschau:** Lauf V, Sprint 5 (`b27dc51`, B-385) per frischem `frontend-reviewer`; `nachgeschaut: 2026-11-05` an B-385.
**Entgangen (🟡):** Löschen und Ändern einer Unit lassen Zeile bzw. Formular verschwinden, ihre Bestätigung bleibt aber ohne Scroll →
[B-390](backlog/B-390-unit-loeschen-bestaetigung-ausser-sicht.md), mit Entscheidung und Freigabe, **nicht** gebaut. 🟢 hingenommen:
das Speichern einer Reihe scrollt auch, wenn die Zeile bleibt.

**Was durchkam:** zum dritten Mal dieselbe Klasse (B-232 → B-381 → B-385 → B-390). Diesmal **trotz** der Absender-Tabelle nach (BK):
sie fragte je *Absender*, und für `UnitForm` habe ich die Frage für eine seiner drei Handlungen beantwortet. Die Tabelle hätte je
**Handlung** fragen müssen (anlegen / ändern / löschen), denn das Kriterium hängt an der Handlung, nicht am Formular. Dasselbe Muster
wie die Nachschau zu Sprint 4 fand (B-388): eine Regel für mehrere Stellen, belegt an einem Teil davon.

**Vorschlag (nicht gelandet, Freigabe 3):** (BM) Schärfung von (BK): die Tabelle hat **eine Zeile je Handlung**, die eine Meldung
auslöst, ermittelt per `grep` nach den Aufrufen von `nachObenMelden`/`onMeldung(` in der Datei – nicht aus dem Gedächtnis; der
Index könnte die Zeilenzahl gegen die Trefferzahl prüfen. Ein Tor, darum nur Vorschlag.

## Ende von Lauf V — Limit 2

Die Nachschau hat eine Entgleitung im Increment **dieses** Laufs gefunden (B-390). Lauf V endet mit Sprint 6.

**Stand:** 2 Stories abgenommen (B-385 `b27dc51`, B-386 `28cb072`). Neu: B-387 und B-388 (geschätzt, bereit für den nächsten Sprint),
B-389 (idee), B-390 (Defekt, entschieden und freigegeben). Retro-Vorschläge (BG)–(BM) unentschieden. Gerätecheck B-231 offen.
Nichts gepusht.

**Nächster Start:** B-390, dann B-388 + B-387 (ein Sprint: „jede unbekannte Vokabel-Referenz abgewiesen"), dann die Wünsche nach dem
[Delegations-Protokoll](pm-sitzung-2026-11-05-delegation.md).

## Lauf W

Gestartet im Dialog („Ja, weiter"), unter der Delegation vom selben Tag.

### Sprint 7 — B-390

**Sprint-Ziel (Creator):** Wer in einer langen Reihen-Liste eine Unit löscht oder ändert, sieht die Bestätigung.

**Gebaut:** Das Bearbeiten-Formular einer Unit meldet über den holenden Kanal (`meldeVonWeitWeg`), das Anlege-Formular bleibt nah.
Zuerst die Handlungs-Tabelle nach (BM), per `grep` statt aus dem Gedächtnis – sie steht in B-390 und fand keine weitere Handlung.
Zwei Kommentare berichtigt, die „aus einem Formular in der Tabelle scrollt nichts" bzw. „Units bleiben nah" behaupteten.

**Belege:** `e2e/unit-bestaetigung-im-bild.spec.ts` (Löschen, Ändern), rote Probe → beide rot an der letzten Zeile; Gegenprobe
`unit-anlegen-bleibt-im-bild.spec.ts` grün. Volle Playwright-Suite 115 passed, Vitest 648/648.

**Review** (`frontend-reviewer`): kein 🔴. Ein Testfund (Vorbedingung des Löschfalls rechnete das Nachrücken zu knapp, weil mit der
einzigen Unit die ganze Tabelle verschwindet) → **Zähler 1 von 5**; zwei Kommentar-Funde nur Text.

**Retrospektive Sprint 7:** Die Tabelle je Handlung hat ihren Zweck erfüllt – sie stellte die Frage für Ändern und Löschen getrennt,
an der B-385 vorbeiging. Kein neuer Mechanismus; (BM) bleibt Vorschlag, jetzt mit einer Messung dafür.

### Sprint 8 — B-387 + B-388

**Sprint-Ziel (Creator/KI-Agent):** Jede Vokabel-Referenz, die eine Übung beim Anlegen oder Ersetzen nennt, ist entweder gültig oder
wird mit dem richtigen Code abgewiesen – und eine verlinkte Vokabel verschwindet nicht still aus dem Speicher.

**Gebaut:** Die Existenzprüfung ist **ein** Helfer (`ExerciseControllerBase.VocabularyIdsProblemAsync`) für Vokabelübung, Übersetzung
und Birkenbihl; Birkenbihl prüft damit `decoding[].vocabularyId` (B-387), die Verwendung im Speicher kennt Birkenbihl-Wörter. B-388
ergänzt die zwei fehlenden Zusicherungen „Id 0 bleibt Formfehler".

**Belege:** sechs rote Proben, je in ihrem eigenen Fall an der eigenen Zeile, alle mit Build-Prüfung im Proben-Skript (Vorschlag
(BL) angewandt – ein Mutant, der nicht kompiliert, hätte den Lauf abgebrochen statt gegen die alte Assembly zu testen). Volle Suite
1089/1089, Playwright 115 passed gegen ein frisch gestartetes Backend.

**Review** (`pugling-reviewer`): kein 🔴/🟡, die Extraktion verhaltensgleich. 🟢 „Wort-`PATCH`" ist ein `PUT` (nur Text);
`"decoding": null` endet schon vorher in einer 500 → [B-391](backlog/B-391-birkenbihl-decoding-null-ist-500.md). **Zähler 0 von 5.**

**Retrospektive Sprint 8 — Nachschau:** Die Retro zu Sprint 7 hatte die Nachschau zu Sprint 6 (B-386) ausgelassen; beide jetzt nachgeholt.
- **Sprint 7 (`e0fdd3b`, B-390)**, frischer `frontend-reviewer`: kein 🔴/🟡 – **nichts entgangen**, kein Limit 2.
- **Sprint 6 (`28cb072`, B-386, Lauf V)**, frischer `pugling-reviewer`: **entgangen (🟢)** – AK 1 „Ändern mit `0`" ohne Test →
  [B-393](backlog/B-393-uebersetzung-put-id-null-ohne-test.md). Daneben **älter** (🟡): der `PUT` legt neue Übersetzungspaare nie im
  Speicher an, und die Oberfläche löst das bei jeder Textänderung eines Paars aus →
  [B-392](backlog/B-392-uebersetzung-put-legt-paare-nicht-an.md). Increment von Lauf V, darum kein Limit 2 für Lauf W.

**Was durchkam:** zum dritten Mal in diesem Tag „ein AK nennt zwei Wege, der Test trägt einen" (B-388 an B-329, B-393 an B-386). Die
Verlauf-Zeile „AK n → Test" listete beim PUT nur die unbekannte Id und schrieb trotzdem „AK 1" daran.

**Vorschlag (nicht gelandet, Freigabe 3):** (BN) Die Belegzeile je AK zerlegt ein AK in seine **Fälle** (Weg × Wert): „Anlegen/Ändern ×
unbekannt/0" sind vier Zeilen, nicht eine. Ein AK mit „und"/„oder" bekommt so je Kombination eine Datei:Zeile oder ein ausdrückliches
„nicht getestet, weil …". Kein Tor, darum nur Vorschlag.

### Sprint 9 — B-392 + B-393

**Sprint-Ziel (Creator):** Wer den Text eines Übersetzungspaars ändert, findet das Paar danach verlinkt im Vokabelspeicher – wie
nach dem Anlegen.

**Gebaut:** `Update` ruft `NormalizeConfigAsync` wie `Create` – nach dem Konfliktcheck, damit ein abgewiesener `PUT` nichts halb schreibt.
Nur die Übersetzung überschreibt den Schritt; kein anderer Typ ändert sein Verhalten. B-393 ergänzt `PUT` mit `0`.

**Belege:** zwei rote Proben (Aufruf entfällt → rot `CatalogReadDeleteTests.cs:197`; nur der `PUT` meldet `0` als unbekannt → rot
`UnknownReferenceCodeTests.cs:137`), beide kompiliert. Volle Suite 1090/1090, Playwright 115 passed. Keine Spec klickt „Paar ändern“.

**Review** (`pugling-reviewer`): kein 🔴. 🟡 Die B-393-Zusicherung fängt allein nichts (ein Validator für beide Wege) → als
Absichtserklärung beschriftet, nur Text. 🟢 Parameter `subjectId` → `seriesId` (Code) → **Zähler 1 von 5**.

**Retrospektive Sprint 9 — Nachschau zu Sprint 8** (`cae15a1`, B-387 + B-388, frischer `pugling-reviewer`): kein 🔴/🟡, die
Extraktion verhaltensgleich. **Entgangen (🟢):** B-387 versprach „ein Feld, eine Antwort", glich aber nur die unbekannte Id an – der
Wort-`PUT` meldet `0` weiter als `invalid_reference`, und meine Summary behauptete „wie der Wort-Endpunkt" →
[B-394](backlog/B-394-birkenbihl-wort-put-id-null-ist-unbekannte-referenz.md). Zwei Summaries berichtigt (nur Text), `"sentences": null`
zu B-391 ergänzt. B-388: nichts entgangen.

**Was durchkam:** dieselbe Klasse wie B-388 und B-393, diesmal über zwei Endpunkte statt zwei Fälle eines Endpunkts: die Story
nannte den Vergleichsweg („wie der Wort-`PUT`") in ihrem Motiv, die AKs prüften ihn aber nicht mit. Vorschlag (BN) aus Sprint 8 hätte
es gefangen, wenn der **Vergleichsweg** als eigene Spalte der Fälle zählt.

**Vorschlag (nicht gelandet, Freigabe 3):** (BO) Schärfung von (BN): nennt eine Story einen zweiten Weg als Maßstab („wie X"), wird X
mit denselben Werten in die Fall-Tabelle aufgenommen – ein Abgleich, kein Neubau.

## Ende von Lauf W — Limit 2

Die Nachschau zu Sprint 8 hat eine Entgleitung im Increment **dieses** Laufs gefunden (B-394). Lauf W endet mit Sprint 9.

**Stand:** 5 Stories abgenommen in drei Sprints: B-390 (`e0fdd3b`), B-387 + B-388 (`cae15a1`), B-392 + B-393 (`c429792`). Nachschau zu
Sprint 6, 7 und 8 erledigt. Neu: B-391 (idee), B-392/B-393 (aus der Nachschau, gleich gebaut), B-394 (idee, Defekt). Retro-Vorschläge
(BG)–(BO) unentschieden. Gerätecheck B-231 offen. Nichts gepusht.

**Nächster Start:** B-394 (klein, Defekt), dann B-389 und B-391 (beide `idee`, Defekt), dann die Wünsche nach dem
[Delegations-Protokoll](pm-sitzung-2026-11-05-delegation.md).

## Lauf X

Gestartet im Dialog („Ja, weiter“), unter der Delegation vom selben Tag.

### Sprint 10 — B-394, B-389, B-391

**Sprint-Ziel (Creator/KI-Agent):** Ein Formfehler im Creator-Payload heißt `validation_error` – nicht `invalid_reference`, nicht 500.

**Vorgehen:** Tests zuerst gegen den Ausgangszustand; die Fälle je Weg × Wert als Tabelle, mit dem Vergleichsweg (Vorschläge (BN)/(BO)
angewandt). Die roten Läufe vor dem Fix sind die Belege für B-394 (`:200`) und B-389 (`:273`).

**B-391 widerlegt:** die angenommene 500 bei `null` in einer Config gibt es nicht – die Modellvalidierung (implizites `[Required]`)
antwortet schon `400 validation_error` mit dem Pfad in `errors`. Ein zuerst gebauter Reflexions-Wächter war doppelt und ist wieder
entfernt; geblieben sind sechs Regressionsfälle (rote Probe: implizites `[Required]` aus → genau diese sechs rot). `verworfen` im
Auftrag. Die falsche 500-Aussage in B-387 ist berichtigt.

**Review** (`pugling-reviewer`): kein 🔴/🟡. Ein Testfund (B-389: die Entscheidung entfernt auch die Bezeichnung, der Test prüfte sie
nicht) → **Zähler 1 von 5**. Volle Suite 1098/1098, Playwright 115 passed.

**Was durchkam, bevor es durchkam:** Die 500 aus B-391 stammte aus einer Nachschau, die den Validator las, ohne zu messen – und ich
hatte sie in B-387 übernommen und darauf einen Wächter gebaut. Der Test gegen den Ausgangszustand zuerst hat das in Minuten gezeigt.

**Retrospektive Sprint 10 — Nachschau zu Sprint 9** (`c429792`, B-392 + B-393, frischer `pugling-reviewer`): kein 🔴. **Entgangen (🟡):**
der echte Klickweg (Mischliste, „findet es") hat keinen Test, und mein Beleg „AK 2 → `:203`" kann nicht fallen →
[B-395](backlog/B-395-uebersetzung-put-mischliste-ungetestet.md). Increment von Lauf W, darum **kein Limit 2** für Lauf X. Daneben
[B-396](backlog/B-396-uebersetzung-sprachwechsel-behaelt-bindung.md) (älter, `idee`).

**Was durchkam:** wieder „ein AK, ein Fall getestet" – diesmal nicht Weg × Wert, sondern **Listenform**: ich testete die kleinste Liste,
der Klickweg schickt eine gemischte. Und eine Gegenprobe stand als Beleg im Verlauf. (BN) bekommt damit eine dritte Spalte: die Form
der Eingabe, wie die Oberfläche sie schickt.

**Vorschlag (nicht gelandet, Freigabe 3):** (BP) Jede Belegzeile „AK n → Test" nennt die Mutante, die **nur** diese Zeile rot macht;
eine Zeile, die auch ohne den Fix grün ist, heißt „Gegenprobe" und zählt nicht als Beleg.

**Lauf X pausiert** nach Sprint 10 auf Wunsch im Dialog (App im eingebauten Browser geöffnet). Kein Limit erreicht. **Nächster Start:**
B-395 (XS, Tests), dann B-396 (`idee`, Frage nach dem Sprachwechsel) und die Wünsche nach dem
[Delegations-Protokoll](pm-sitzung-2026-11-05-delegation.md).

### Sprint 11 — B-395

**Sprint-Ziel (Creator):** Der echte Klickweg „ein Paar einer Übersetzung ändern“ ist belegt – mit der Liste, wie der Editor sie schickt.

**Gebaut:** ein Test (Mischliste: ein Paar behält seine Id, das bearbeitete findet den vorhandenen Eintrag; die alte Vokabel ist vorher
gesperrt und danach frei). Je Fall die Mutante, die nur ihn rot macht (Vorschlag (BP) angewandt), die Gegenprobe als solche beschriftet.
Volle Suite 1099/1099. Kein Laufzeit-Code, darum kein Gang durch die App.

**Review:** 🟡 die Gegenprobe zeigte nicht, dass die Vokabel vorher gesperrt war – ergänzt; 🟢 Anzahl der Paare. **Zähler 2 von 5.**

**Retrospektive Sprint 11 — Nachschau zu Sprint 10** (`2312aaf`, frischer `pugling-reviewer`): **entgangen (🟡):** B-391 war nur zur
Hälfte widerlegt – `null` als Wert eines Members ist `400`, `null` als **Element** einer Liste ist **500**, jetzt **gemessen** →
[B-397](backlog/B-397-null-element-in-config-liste-ist-500.md). Daneben [B-398](backlog/B-398-pflichtfelder-die-optional-gemeint-sind.md)
(Frage: zwei Pflichtfelder, die optional gemeint sind) und veraltete Zeilenangaben in B-389 (berichtigt). B-394: nichts entgangen.

**Was durchkam:** derselbe Fehler zweimal in einer Richtung. Erst übernahm ich eine 500 aus dem Lesen von Code (B-391), dann widerlegte
ich sie mit Messungen – aber nur an der Form, die mir einfiel (`null` als Wert), nicht an der zweiten Form (`null` in der Liste). Die
Messung schützte vor der falschen Behauptung, nicht vor der zu weiten Verallgemeinerung.

**Vorschlag (nicht gelandet, Freigabe 3):** (BQ) Wer eine Behauptung *verwirft*, schreibt in den `grund` die gemessenen Formen und lässt
alles andere ausdrücklich offen („gemessen: X, Y; nicht gemessen: Z“). Ein `verworfen` gilt nur für das Gemessene.

## Ende von Lauf X — Limit 2

Die Nachschau hat eine Entgleitung im Increment **dieses** Laufs gefunden (B-397 an B-391). Lauf X endet mit Sprint 11.

**Stand Lauf X:** 3 Stories abgenommen (B-394, B-389 `2312aaf`; B-395 siehe Commit), B-391 `verworfen` und teilweise wieder geöffnet als
B-397. Neu: B-396 (`idee`), B-397 (Defekt, gemessen), B-398 (Frage). Vorschläge (BG)–(BQ) unentschieden. Nichts gepusht.

**Nächster Start:** B-397 (Wächter für `null`-Elemente), dann B-396 und B-398 (beide brauchen eine Antwort im Dialog).

## Lauf Y

Gestartet im Dialog am 2026-11-05: „Starte 4 Nachtläufe hintereinander. Kommt es zu Fragen, kläre diese selber nach bestem Wissen und
Gewissen." Entscheidungen zu `Frage`-Stories trifft Claude darum im Auftrag; jede steht als solche beschriftet in der Story.

### Sprint 12 — B-397 + B-398

**Sprint-Ziel (Creator/KI-Agent):** Ein Payload mit `null` an einer Stelle, die der Vertrag „nie null" nennt, ist ein Formfehler mit
Pfad – und ein Feld, das der Code optional meint, ist es auch im Vertrag.

**Gebaut:** `NullElementGuardFilter` – ein globaler Aktionsfilter vor dem `ModelStateInvalidFilter`, der `null`-Elemente nicht-nullbarer
Listen in jedem Body meldet (B-397; gemessen vorher: dreimal `500`, `wordBank` mit `null` sogar still `201`). B-398 im Auftrag
entschieden: `NaturalTranslation` optional in beiden Eingaben, Sprachen der Vorschau bleiben Pflicht, `Gap.Answer` bleibt Pflicht und
ist so dokumentiert.

**Review** (`pugling-reviewer`): 🟡 ein Body, der selbst eine Liste ist (Vokabel-Batch, vom Agenten benutzt), lief vorbei → behoben,
rote Probe 3/3. **Zähler 1 von 5.** Drei 🟢 (zwei berichtigt, einer ohne Zählung zurückgestellt). Delta-Review ohne 🔴/🟡. Volle Suite
1108/1108, Playwright 115 passed nach dem letzten Code-Edit.

**Rollengang:** Creator als Integrationsfälle (die Oberfläche schickt keine `null`-Elemente); Vater und Sohn Regressionszeugen über die
volle E2E-Suite.

**Retrospektive Sprint 12 — Nachschau zu Sprint 11** (`e5ff96e`, B-395, frischer `pugling-reviewer`): geprüft je Belegzeile, ob ihre
Mutante genau dort fällt, und der Payload gegen `exerciseConfig.tsx:286-296`. **Gefunden (🟡, nur Text):** die AK-Zeile nannte für
„jedes Paar neu auflösen" `:249`, die Mutante fällt schon bei `:242` – Tabelle und Test-Summary berichtigt, kein Test fehlt. Increment
von Lauf X: **kein Limit 2.** Index: Nachgeschaut 330 von 332.

**Was durchkam:** mein eigener Filter versprach „eine Regel für jeden Body" und deckte nur Bodies, die Objekte sind – dieselbe
Familie wie (BN)/(BO): eine Behauptung über *alle*, gebaut und getestet an der Form, die mir einfiel. Der Reviewer fand die Listenform.

**Vorschlag (nicht gelandet, Freigabe 3):** (BR) Ein reflexiver Wächter über alle `[FromBody]`-Parameter: für jede Liste im Typgraphen
(auch den Body selbst) wird ein `null`-Element erzeugt und geprüft, dass der Filter es meldet. Dann hängt „jede Liste" an einer
Aufzählung, nicht an meiner Vorstellung.

### Sprint 13 — B-396

**Sprint-Ziel (Creator):** Wer im Übersetzungs-Editor nur das Sprachpaar ändert, findet seine Paare danach im neuen Sprachpaar im Store.

**Gebaut:** jede geladene Zeile merkt sich ihr Sprachpaar (mit dem Rückfall, den der Editor zeigt); die Bindung reist nur, solange Text
**und** Sprachpaar stehen. Im Auftrag entschieden: keine Server-Prüfung des Sprachpaars (wie B-387). Rote Probe 2/2, drei Mutanten je
einzeln rot, eine neue E2E-Spec als Klickweg (Mutante → rot an der Schlusszusicherung). Vitest 651/651, Playwright 116 passed.

**Review** (`frontend-reviewer`): kein 🔴/🟡; zwei 🟢 an der Spec nachgezogen, eine bekannte Grenze in der Story. **Zähler 0 von 5.**

**Retrospektive Sprint 13 — Nachschau zu Sprint 12** (`49e773d`, frischer `pugling-reviewer`): geprüft Belegzeilen, „eine Regel für
jeden Body" gegen alle `[FromBody]`-Parameter, `NaturalTranslation`-`null` auf allen Wegen, `Gap.Answer`-Summary. **Entgangen (🟡):**
eine Mutanten-Zeile in B-398 behauptet „rot `:193`" für eine Mutante, die nicht kompiliert – ich hatte sie nicht gemessen, aber wie
gemessen notiert; dazu eine verrutschte Zeilenangabe in B-397 → [B-399](backlog/B-399-b398-mutantenzeile-ungemessen-und-falsch.md).
Increment **dieses** Laufs: **Limit 2.**

**Was durchkam:** eine Beleg-Zeile, die nach Messung aussah. Der Reviewer hatte die Mutanten aufgezählt, ich habe sie in den Verlauf
übernommen und nur die erste gemessen – der Satz „Gemessen ist nur die erste" stand dahinter, die Zeile selbst trug keine Kennzeichnung.

**Vorschlag (nicht gelandet, Freigabe 3):** (BS) Eine Mutanten-Zeile trägt `gemessen: erwartet/gemessen` oder `ungemessen`, je Mutante;
eine ungemessene zählt nicht als Beleg. Verwandt mit (BP); prüfbar per Grep über `## Verlauf`.

## Ende von Lauf Y — Limit 2

Die Nachschau zu Sprint 12 fand eine Entgleitung im Increment **dieses** Laufs (B-399 an B-398/B-397). Lauf Y endet mit Sprint 13.

**Stand Lauf Y:** 3 Stories abgenommen (B-397, B-398 `49e773d`; B-396 siehe Commit). Neu: B-399 (Defekt, Doku). Vorschläge (BG)–(BS)
unentschieden. Nichts gepusht. **Nächster Start:** B-399 (messen und berichtigen), dann die Wünsche nach dem
[Delegations-Protokoll](pm-sitzung-2026-11-05-delegation.md).
