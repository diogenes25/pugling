---
tags: [typ/story, status/geschaetzt, bereich/doku, bereich/backend]
aliases: [Lösungsfeld-Regel, Tor folgt dem Geheimnis, Rollenreichweite eines Lese-DTOs]
status: geschaetzt
prio: P3
art: Aufräumen
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: B-82 (E3′, Bau-Sitzung 2026-08-03)
---

# B-83 · Die Lösungsfeld-Regel steht nur als Kommentar am Wächter

Seit [B-82](B-82-positions-report-gibt-loesungen-preis.md) gilt mechanisch: **gibt eine Action in ihrem
Nutzlast-Graphen ein Feld namens `Answer`/`Solution`/`CorrectAnswer` heraus, muss sie auf eine Rollenmenge
ohne `Student` gegated sein** — `Roles.Creator` genügt dafür ebenso wie `Roles.Supervisor`, denn ein Autor
muss die Lösung seiner eigenen Übung sehen. Der Wächter
(`ConventionGuardTests.Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated`) hält die Regel und begründet
sie ausführlich im Kommentar. **Nur steht sie nirgends, wo man sie vor dem Schreiben eines DTOs liest**: die
Root-`CLAUDE.md` zählt unter „Mechanische Tore statt Disziplin" die Wächter auf, ohne diesen; die
Konventions-Liste sagt nichts über die Rollenreichweite eines Lese-DTOs; und
[docs/codequalitaet-gates-plan.md](../codequalitaet-gates-plan.md) führt das Inventar der Tore, in dem er
fehlt.

## User Story

Als **Entwickler** (Mensch oder Modell), der ein neues Auswertungs-DTO schreibt, möchte ich die
Lösungsfeld-Regel **vor** dem Schreiben lesen können, damit ich sie kenne, bevor das Tor rot wird — ein rotes
Tor ohne vorher gelesene Regel liest sich wie eine Schikane, nicht wie eine Zusicherung.

## Ist-Stand am Code

- Der Wächter samt Begründung steht ausschließlich in
  [backend/Pugling.Api.Tests/ConventionGuardTests.cs:208-327](../../backend/Pugling.Api.Tests/ConventionGuardTests.cs):
  die Kammer-Metapher, die Herleitung „das Tor folgt dem Geheimnis, nicht dem Ordner" (Zeile 215), die
  Namensliste `SolutionPropertyNames = ["Answer", "Solution", "CorrectAnswer", "Translation"]` (Zeile 300)
  und die vier begründeten Ausnahmen (`SolutionFieldExceptions`, Zeile 318-327).
- Die Root-`CLAUDE.md` (Abschnitt „Mechanische Tore statt Disziplin" / „Konventionen") nennt `ConventionGuardTests`
  namentlich für PATCH-Semantik, Vertragstypen und Ownership-Filter — **kein Treffer** für Lösungsfeld,
  `Answer`, `Solution` oder `CorrectAnswer` (per Grep gegen die Datei geprüft, 0 Treffer).
- [backend/Pugling.Api/CLAUDE.md](../../backend/Pugling.Api/CLAUDE.md) beschreibt Lern-Katalog, Lehrplan,
  Services, Reward-Ökonomie und Medien-Anti-Cheat im Detail — **ebenfalls kein Treffer** für die Regel.
- [docs/codequalitaet-gates-plan.md](../codequalitaet-gates-plan.md) trägt das Inventar der **Schema**-Tore
  G1–G9 (Etappe E, Zeilen 714-746) und nennt aus Etappe B nur pauschal „Vier reflexive Konventions-Wächter
  (`ConventionGuardTests`)" (Zeile 369) — **ohne** die einzelnen Facts aufzuzählen. Seit Etappe B sind über
  B-80/81/82 mehrere weitere `[Fact]`s in derselben Klasse dazugekommen (u. a. dieser), ohne dass das Dokument
  je nachgezogen wurde: es ist ein **datiertes Umsetzungsprotokoll** der Etappen A–E, keine lebende Registry
  jedes einzelnen `ConventionGuardTests`-Facts.

## Die echte Lücke

Schmaler als die Notiz nahelegt: Das Tor selbst ist scharf, gemessen und mehrfach gegen sich selbst rot
gesehen (siehe B-82s Verlauf) — es fehlt **keine** mechanische Absicherung. Die Lücke ist rein
dokumentarisch, und sie betrifft **nur** die Root-`CLAUDE.md`: `docs/codequalitaet-gates-plan.md` ist kein
zweiter Ort, an dem diese Regel fehlt, sondern ein historisches Protokoll, das grundsätzlich keine
Einzel-Facts von `ConventionGuardTests` führt (siehe Ist-Stand) — dort etwas nachzutragen wäre die erste
Ausnahme von diesem eigenen Muster, nicht das Schließen einer Lücke.

## Offene Punkte

1. ~~**Wo wird die Regel resident: Root-`CLAUDE.md` oder `backend/Pugling.Api/CLAUDE.md`?**~~ → siehe
   Entscheidung 1.
2. ~~**Muss `docs/codequalitaet-gates-plan.md` nachgezogen werden (E4-Wegfall aus B-82/E3′)?**~~ → siehe
   Entscheidung 2.

## Entscheidungen

### 1 · Die Regel wird resident in der Root-`CLAUDE.md`, nicht in `backend/Pugling.Api/CLAUDE.md`

Neue Zeile im Abschnitt „Konventionen", direkt neben den strukturell gleichartigen Regeln
(PATCH-Semantik, Eigentum, Fehler-Format) — nicht als eigener Abschnitt.

*Begründung.* Root-`CLAUDE.md`s eigenes Kriterium: „Resident wird nur, was bei einer **beliebigen**
Änderung eine Entscheidung ändert." Ein neues Lese-DTO mit potenziellem Lösungsfeld entsteht in
`Pugling.Contracts` **und** wird von einem Controller in `Pugling.Api` zurückgegeben — zwei Projekte, zwei
verschachtelte `CLAUDE.md`s (`backend/Pugling.Contracts/CLAUDE.md`, `backend/Pugling.Api/CLAUDE.md`), von
denen keine automatisch lädt, wenn nur im jeweils anderen Projekt gearbeitet wird. Die Root-`CLAUDE.md` ist
die einzige Datei, die **jede** Sitzung lädt, unabhängig davon, welches der beiden Projekte gerade offen ist
— genau die Eigenschaft, die die Nachbarregeln (PATCH-Semantik, Eigentum, `CancellationToken`) dort schon
nutzen, obwohl auch sie API-spezifisch sind.

*Kosten.* Eine zusätzliche Zeile im dauerhaft geladenen Startkontext — dem Budget, das
`.claude/scripts/context-budget.sh` warnend beobachtet (siehe Root-`CLAUDE.md`, „Arbeitsweise"). Eine Zeile
ist knapp gegenüber dem bestehenden Umfang des Abschnitts „Konventionen" und reißt das Budget nicht; bei
weiterem Wachstum dieser Regel (neue Ausnahmeklasse, neue Namen) gehört die Vertiefung nach
`backend/Pugling.Api/CLAUDE.md`, nicht in eine wachsende Root-Zeile.

### 2 · `docs/codequalitaet-gates-plan.md` bleibt unverändert, E4-Wegfall wird nicht nachgetragen

*Begründung.* Das Dokument ist ein **datiertes Umsetzungsprotokoll** der Etappen A–E (Schema-Tore G1–G9 plus
die Vorgeschichte L1–L9), keine lebende Registry, die jeden `ConventionGuardTests`-Fact einzeln führt — das
zeigt sich schon daran, dass Etappe B pauschal „vier reflexive Konventions-Wächter" zählt (Zeile 369), ohne
sie namentlich aufzuzählen, und seither mehrere weitere Facts (u. a. der hier behandelte, aus B-80/81/82)
dazukamen, ohne dass das Dokument je nachgezogen wurde. `E3`/`E3′`/`E4` sind zudem **interne
Entscheidungsnummern der Story B-82** (ihr eigener Verlauf, nicht das Nummernschema dieses Plandokuments,
das mit `G1`–`G9`/`L1`–`L9` arbeitet) — ein Eintrag „E4 entfallen" wäre dort ohne Kontext unlesbar. Die
residente Regel aus Entscheidung 1 ist der richtige Ort für den *heutigen* Stand der Zusicherung; das
Plandokument bleibt Protokoll der *damaligen* Messung.

*Kosten.* Keine — es ist die Feststellung, dass keine Änderung fällig ist. Wer künftig die Geschichte des
Tors nachvollziehen will, findet sie vollständig in B-82s `## Verlauf`, nicht im Gates-Plan.

## Akzeptanzkriterien

1. Die Root-`CLAUDE.md` nennt im Abschnitt „Konventionen" die Lösungsfeld-Regel: die Namensliste
   (`Answer`/`Solution`/`CorrectAnswer`/`Translation`), die Rollenbedingung („ohne `Student`", nicht „mit
   `Supervisor`") und den Wächtertest `ConventionGuardTests.Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated`
   beim Namen.
2. Die neue Zeile grenzt `Expected` ausdrücklich ab (Reveal nach der Antwort, kein Lösungsfeld) — sonst
   suggeriert die Kurzfassung eine schärfere Regel, als der Wächter tatsächlich hält.
3. `docs/codequalitaet-gates-plan.md` bleibt byte-identisch (Entscheidung 2).
4. `markdownlint-cli2` bleibt grün (CI-Job „Markdown-Lint").
5. Kein `.cs`-Code ändert sich, kein Test ändert Verhalten — die bestehende Testzahl bleibt unverändert grün.

## Schätzung

**Größe: XS** — analog [B-51](B-51-admin-rolle-dokumentieren.md) (Doku-Aufräumen an einer Regel, die im Code
schon mechanisch greift): zwei bis drei Sätze in einer bestehenden Markdown-Datei, kein Code, kein Test, kein
Split nötig.

**`wo: doku`**, **`migration: nein`** (keine Entity berührt), **`vertragsbruch: nein`** (kein DTO, keine
Route ändert sich).

**Risiken.** Die Namensliste in der Kurzfassung muss **wortgleich** mit
`ConventionGuardTests.SolutionPropertyNames` bleiben, sonst driftet die Doku genau auf die Art, die diese
Story beheben soll — künftige Änderungen an der Namensliste (z. B. eine fünfte Ausnahme wie bei B-81) müssen
die Root-`CLAUDE.md`-Zeile mitziehen. Kein technisches Risiko, da reine Textänderung.

**Angriffsplan.**

1. Zeile in `CLAUDE.md` (Repo-Root), Abschnitt „Konventionen", einfügen — direkt bei den strukturell
   verwandten Bullets (PATCH-Semantik, Eigentum, Fehler), nicht als neuer Abschnitt.
2. Gegenlesen: Namensliste und Rollenbedingung wortgleich mit
   `ConventionGuardTests.cs:300,318-327` (Ist-Stand oben).
3. `markdownlint-cli2` lokal laufen lassen (CI-Job „Markdown-Lint").

Kein `pugling-reviewer`/`frontend-reviewer` nötig (Tabelle „Fachbrille aus `wo`", `wo: doku`).

**Testweg.** Kein Integrationstest, kein E2E, kein `/smoke-test` — reine Markdown-Änderung ohne
Laufzeit-Effekt. Die einzige mechanische Prüfung ist `markdownlint-cli2` (CI-Job „Markdown-Lint"); die
inhaltliche Abnahme ist das Gegenlesen aus Schritt 2 des Angriffsplans.

## Verlauf

- **2026-08-03** — angelegt aus dem Bau von B-82: dort wurde E3 beim Scharfstellen von seiner eigenen
  Kostenmessung umgeworfen und als E3′ neu geschnitten (Tor folgt dem Geheimnis statt dem Ordner, vier
  Ausnahmen statt zehn, E4 dadurch gegenstandslos). Die Regel ist damit mechanisch gesichert, aber nur am
  Wächter dokumentiert — ausdrücklich als eigene Story abgelegt statt stillschweigend in die `CLAUDE.md`
  geschrieben, weil residenter Kontext eine Entscheidung des Nutzers ist. `prio: P3` in Analogie zu
  [B-51](B-51-admin-rolle-dokumentieren.md) vorgeschlagen (Doku-Aufräumen an einer Regel, die im Code schon
  greift) — nicht vom Nutzer bestätigt.
- **2026-08-03** — ausformuliert: gegen den Code recherchiert
  (`ConventionGuardTests.cs:208-327`, Namensliste `SolutionPropertyNames` Zeile 300, Ausnahmen Zeile
  318-327). Bestätigt: weder die Root-`CLAUDE.md` noch `backend/Pugling.Api/CLAUDE.md` noch
  `docs/codequalitaet-gates-plan.md` führen die Regel (Grep, je 0 Treffer). Dabei die echte Lücke geschärft:
  `docs/codequalitaet-gates-plan.md` ist ein datiertes Umsetzungsprotokoll der Etappen A–E, keine lebende
  Registry einzelner `ConventionGuardTests`-Facts — es zählt schon Etappe B nur pauschal („vier reflexive
  Wächter"), ohne sie aufzuzählen, und mehrere seither dazugekommene Facts (u. a. dieser) blieben dort
  ohnehin unerwähnt. Zwei offene Punkte formuliert: Wo wird die Regel resident (Root vs. verschachtelt), und
  ob der E4-Wegfall aus B-82/E3′ im Gates-Plan nachzuziehen ist.
- **2026-08-03** — gegrillt (autonom getroffen, Nutzerauftrag 2026-08-04): **Entscheidung 1**, Root-`CLAUDE.md`,
  weil ein neues Lösungsfeld-DTO zwei Projekte mit je eigener verschachtelter `CLAUDE.md` durchläuft
  (`Pugling.Contracts`, `Pugling.Api`) und nur die Root-Datei in beiden Sitzungen sicher lädt — dieselbe
  Begründung, die schon PATCH-Semantik und Eigentum dort trägt. **Entscheidung 2**, `codequalitaet-gates-plan.md`
  unverändert: das Dokument führt grundsätzlich keine Einzel-Facts von `ConventionGuardTests`, und `E3`/`E3′`/`E4`
  sind interne Entscheidungsnummern von B-82s eigenem Verlauf, nicht das `G`/`L`-Nummernschema des Plandokuments
  — ein Eintrag dort wäre ohne Kontext unlesbar und bräche das bestehende Muster, statt eine Lücke zu schließen.
- **2026-08-03** — geschätzt (autonom getroffen, Nutzerauftrag 2026-08-04): **Größe XS**, analog B-51 — zwei bis
  drei Sätze in der Root-`CLAUDE.md`, kein Code, kein Test, kein Split nötig. `wo: doku`, `migration: nein`,
  `vertragsbruch: nein` (keine Entity, kein DTO, keine Route berührt). Testweg ist `markdownlint-cli2` plus
  Gegenlesen der Namensliste gegen `ConventionGuardTests.cs` — kein Integrationstest, kein E2E, kein
  `/smoke-test`, da reine Doku-Änderung ohne Laufzeit-Effekt.
