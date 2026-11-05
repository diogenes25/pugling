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
