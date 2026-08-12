---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-08-12, PM-Sitzung 2026-08-12]
---

# PM-Sitzung: Nachtlauf — der Katalog sagt auch im Vater-Web die Wahrheit

**Datum:** 2026-08-12 · **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Unbeaufsichtigter Backlog-Lauf nach [nachtlauf.md](nachtlauf.md), Freigaben 1–8. Sprint A schließt
die Lücke zwischen dem, was der Server seit B-13 verweigert, und dem, was die Oberfläche weiter anbietet.

## Auftrag und Freigaben

Erteilt im Dialog, abends angestoßen (Sitzung bleibt offen): **alle acht Freigaben wie dokumentiert.**
Damit gilt insbesondere:

- **Freigabe 1** — autonom gegrillt wird nur `art: Defekt` und `Aufräumen`.
- **Freigabe 3** — mehrere Sprints erlaubt; Review-Funde werden sofort behoben statt gemeldet; die
  Retrospektive **schlägt** ihren Mechanismus nur **vor**, sie landet ihn nicht; ein Fehlerzähler je Sprint
  beendet bei >5 den ganzen Lauf.
- **Freigabe 6** — die Chrome-Verbindung stand, also zählt ein **echter Browser-Rollengang**.
- **Freigabe 8** — `geschaetzt` + `Wunsch`/`Frage` ist baubar; gesperrt bleibt nur, was noch nicht
  geschätzt ist.

### Ein Befund vor dem ersten Sprint: der `/doctor`-Lauf hatte das Verfahren beschädigt

Unmittelbar vor diesem Lauf hat ein `/doctor`-Durchgang in derselben Sitzung vier Skills als „unbenutzt"
abgeschaltet. Vier davon verlangt das Verfahren **namentlich**, und der Nachtlauf hätte sie diese Nacht
gebraucht:

| Skill | Wer ihn verlangt |
| --- | --- |
| `web-design-guidelines` | Freigabe 7 dieses Auftrags ([nachtlauf.md](nachtlauf.md):68) und die `wo`-Tabelle des `backlog`-Skills |
| `accessibility` | Entscheidung 3 in B-132 („Empfehlung: der `accessibility`-Skill, nicht nach Gefühl") |
| `prototype` | Ticket-Typ `prototype` der Karten (`docs/backlog/karten/B-106/T-*.md`) |
| `lehrplan-autor` / `lehrplan-lerner` | je 11 Dateien, u. a. [architektur-entscheidung.md](architektur-entscheidung.md) |

Zurückgedreht, bevor der Lauf begann. **Die Lehre gehört in die Retrospektive**, nicht hierher: ein Zähler
für Aufrufe ist blind für „vom geschriebenen Verfahren verlangt", und bei `accessibility` stand die
Begründung sogar als nummerierte Entscheidung in einer Story.

## Runde 1 — Ausgangslage statt simuliertem Feedback

Kein neuer beobachteter Input: der jüngste Anmerkungs-Befund
([befund-2026-08-02.md](anmerkungen/befund-2026-08-02.md)) ist geerntet, seine Ideen liegen als B-63…B-68.
Die Ausgangslage ist damit der Backlog selbst plus der **Rollengang-Fund vom 2026-08-11**, mit dem B-13
geendet hatte.

### Woran der Creator hängenbleibt (belegt, nicht simuliert)

Am 2026-08-11 hat B-13 das Fach geschlossen: umbenennen und löschen darf nur der Eigentümer, ein Seed-Fach
niemand. Der Creator sieht davon nichts. Er klickt „Löschen", liest einen Dialog, der ihm fünf Folgen
aufzählt — und bekommt `403 not_owner`. Bei **vier von sechs** Fächern der laufenden Instanz (allen
Seed-Fächern) ist das der Normalfall, nicht der Rand.

## Refinement (getrennt vom Sprint gezählt)

Die größere Hälfte, wie üblich. Drei Stories bewegt, davon zwei ganz neu recherchiert:

| Story | von → nach | Was die Recherche geändert hat |
| --- | --- | --- |
| B-154 | `idee` → `geschaetzt` | Zwei Funde, die die Idee nicht hatte: die **„Art" ist nicht betroffen** (`ExerciseCategory` trägt gar kein Eigentümer-Feld) — der Schnitt ist kleiner als „die Katalogseite"; und im selben Bauteil standen **zwei seit B-13 falsche Behauptungen**, eine als sichtbarer UI-Text |
| B-13 | `in-arbeit` → `abgenommen` | Die Notiz behauptete „es fehlt nur der Reviewer und der Commit" — **der Commit fehlte nicht** (`bd2dcdb`). Am Code nachgesehen statt geglaubt |
| B-155 | (nicht angefasst) | `gegrillt`, aber nicht `geschaetzt`. Freigabe 8 nennt nur `geschaetzt`→baubar und `ausformuliert`→gesperrt; `gegrillt` ist ungeregelt — **im Protokoll notiert, nicht selbst entschieden** |

## Sprint A — Ziel & Umfang

**Sprint-Ziel:** *Der Creator sieht an jedem Fach, ob er es ändern darf — kein Knopf verspricht mehr etwas,
das der Server verweigert.*

**Umfang:** B-13 (Abnahme) und B-154 (Bau). **Draußen geblieben, mit Namen:** B-145 (denormalisierte
`SubjectName`-Kopien) und B-141 (Interessen-Tag-Dubletten) waren als Sprint-Kandidaten geplant und dienen
dem Ziel **nicht** — B-145 betrifft die Namensgleichheit über Tabellen, B-141 eine andere Ressource. Sie
bleiben `idee` und sind das Material des nächsten Sprints.

**Entwickler-Brief:** Kein Backend-Anteil. Quelle der Wahrheit ist `SubjectResponse.isMine`/`ownerAdultId`,
die seit B-13 in jeder Antwort stehen und im Frontend von keiner Produktionszeile gelesen wurden. Guards:
keine neuen — die Story *liest* nur, was der Server schon entschieden hat. Migration: nein. Vertrag:
unangetastet. Testweg: Komponententest (es gibt keinen `CatalogAdmin.test.tsx`), Rollengang im Browser.

## Iteration A — umgesetzt

- **`CatalogAdmin.tsx`**: die Fach-Zeile liegt jetzt in einem **exportierten** Baustein `SubjectRow`. Der
  Export ist keine Bequemlichkeit, sondern der Preis für einen Test, der die **Bindung** prüft:
  `CatalogAdmin` lädt beim Fachwechsel die Arten nach und hängt damit am Netz, und **kein** Test dieses
  Frontends mockt `../lib/api` (nachgezählt: null `vi.mock` in `frontend/src/`). Vorbild ist
  `VaterLehrwerke.test.tsx`, das das exportierte `UnitForm` direkt rendert.
- **Zwei Fälle statt einem** bei „darf nicht ändern": „hat jemand anderes angelegt" wäre bei einem Seed-Fach
  nicht unschön, sondern falsch — es gehört niemandem. `== null` deckt dabei `null` **und** ein fehlendes
  Feld, weil der Vertrag `ownerAdultId` optional herausgibt.
- **Zwei veraltete Texte** korrigiert (Entscheidung 4): der sichtbare Absatz in `CatalogAdmin` und der
  Datei-Kommentar, der die Löschwarnung mit der Globalität des Katalogs begründete.
- **Verifikation:** `npx vitest run` **256/256 grün** (35 Dateien), `npm run build` (tsc -b + vite) grün,
  Backend unberührt und weiter **821/821**, `dotnet format --verify-no-changes` sauber,
  `markdownlint-cli2` **0 Issues** über 253 Dateien.
- **Rote Probe (Auflage 5):** mit entfernter Bedingung (`if (true || subject.isMine)`) fallen **3 von 5**
  Fällen — genau die drei Nicht-Eigentümer-Fälle; die beiden anderen bleiben grün, wie sie sollen.
- **Rote Probe B-13 (Auflage 5):** mit getauschten Blöcken im `Delete` **erwartet `Forbidden`, gemessen
  `Conflict`**, 1 von 6 rot.

## Angehalten (Freigabe 1) — was ein Mensch entscheiden muss

1. **B-155 ist `gegrillt`, aber nicht `geschaetzt`.** Freigabe 8 regelt `geschaetzt` (baubar) und
   `ausformuliert` (gesperrt) — nicht `gegrillt`. Meine Lesart wäre: die Sperre schützt vor
   Produktentscheidungen des Agenten, und die sind bei B-155 im Dialog gefallen; Schätzen ist ein
   Entwicklerschritt, die Story also baubar. **Das ist Auslegung, keine Regel** — entschieden wurde sie
   nicht.
2. **B-157, offener Punkt 2** (aus dem Reviewer-Fund entstanden): Soll auch `POST` einer neuen „Art" in
   einem fremden Fach gesperrt werden? Das weicht **bewusst** von B-13s Entscheidung 2 ab („Anlegen bleibt
   ungegatet"), und die Begründung dafür ist neu: eine neue Art landet *in* einem fremden Baum, ein neues
   Fach gehört niemandem. Eine Abweichung von einer bestehenden Entscheidung entscheidet der Lauf nicht
   selbst.

## Runde 2 — Re-Review / Abnahme (Sprint A)

### `pugling-reviewer` zu B-13: kein Blocker, ein Fund sofort behoben

Alle fünf gestellten Prüffragen einzeln belegt — `IsOwnedBy` fail-closed an allen vier Stellen (auch die
Inline-Projektion: das `fid != null` verhindert genau den EF-Null-Rewrite, der `isMine` für Seed-Fächer
`true` machen würde), Projektion EF-übersetzbar, AK-Deckung getroffen statt daneben, `ct`/`ProblemWithCode`
sauber, Migrationskette bei 1.

**Behoben statt gemeldet** (Freigabe 3): die bewusste 403-vor-409-Reihenfolge hing an einem Kommentar und
an keinem Test. Neuer Fall `FachEigentumTests.FremderCreator_BekommtNotOwner_AuchWennDasFachBenutztIst`.

### `frontend-reviewer` zu B-154: kein Blocker, zwei Funde mitgenommen

Er hat zwei Dinge **bestätigt**, die sonst nur Behauptung geblieben wären: `== null` ist richtig *und*
besser als das `=== null` meines eigenen Angriffsplans; und die **fehlende** Live-Region am erklärenden Satz
ist richtig — sie entstünde mit ihrem Text in einem per `key` neu montierten Teilbaum und wäre nach der
Messung aus B-132 garantiert stumm.

Zwei Funde mitgenommen:

1. `expect(okKnopf()).toBeNull()` konnte **nie** fehlschlagen — „OK" erscheint nur bei `dirty`, beim Mount
   also niemals. Jetzt tragend: tippen → OK erscheint → klicken → `onSave` bekommt den neuen Namen. Damit
   pinnt der Test genau den Weg, den der Browser mir verweigert hat (siehe unten).
2. Die Begründung in meinem **neuen** `key`-Kommentar hielt nicht (`SubjectRow` hält selbst keinen State).
   Korrigiert — ein Kommentar mit nicht tragender Begründung ist genau die Klasse, die diese Story oben
   repariert.

### Rollengang (Step 6, Freigabe 6) — im echten Browser

Server **nach** der letzten Änderung gestartet (die Regel, die der 2026-08-10er Lauf hinterlassen hat),
Wegwerf-DB im Scratchpad, Frontend auf `:5173`. Die drei Zustände über die **echte API** hergestellt, nicht
per SQL (die Lehre vom 2026-08-05):

| Zustand | Gesehen |
| --- | --- |
| Seed-Fach „Englisch" (`owner=null`) | kein Feld, keine Knöpfe, dafür „gehört zum Grundbestand – du kannst es verwenden, aber niemand kann es umbenennen oder löschen" |
| Fremdes Fach (`owner=4`, zweiter registrierter Creator) | kein Feld, keine Knöpfe, dafür „hat jemand anderes angelegt" |
| Eigenes Fach (`owner=1`) | „FACH UMBENENNEN" samt „Löschen", unverändert |
| Art-Zeilen, alle drei Fälle | behalten ihr „Löschen" (AK 4) |

Server-Gegenprobe im selben Lauf: `PATCH` fremdes Fach `403 not_owner`, Seed-Fach `403 not_owner`, eigenes
Fach `200`.

**Ehrliche Grenze des Gangs:** Der OK-Knopf ließ sich **nicht** klicken — die CDP-Tastenanschläge erreichten
das Eingabefeld nach drei Versuchen nicht (Werkzeug-Artefakt, kein Produktbefund: das Feld blieb unverändert,
der Fokusring stand auf dem Auswahlfeld). Nicht weiterverfolgt. Der Schreibweg ist stattdessen serverseitig
belegt **und** seit dem Reviewer-Fund im Komponententest — was der Browser nicht konnte, hält jetzt ein Test.

### Fund im eigenen Rollengang

Eine Zeile **über** der Karte versprach weiter „Hier legst du sie an, benennst sie um und löschst sie"
(`VaterKatalog.tsx:22-25`). Der Bildschirm widersprach sich zwei Absätze weit. Sofort behoben (Freigabe 3),
im selben Schnitt statt ausgelagert: mit dem Satz ist das Sprint-Ziel nicht erfüllt. Der
`frontend-reviewer` hat denselben Fund unabhängig gemeldet, mit fast demselben Wortlaut — zwei Prüfschritte,
die sich gegenseitig bestätigen, ist das beste Ergebnis dieses Sprints.

### Fehlerzähler (Freigabe 3)

**3 von 5.** Gezählt wird ein Fund, wenn seine Behebung Code oder Tests ändert:

| # | Fund | Was der Fix anfasste |
| --- | --- | --- |
| 1 | 403-vor-409 von keinem Test gehalten (`pugling-reviewer`) | Test |
| 2 | Veralteter Seiten-Vorspann in `VaterKatalog` (eigener Rollengang) | Code |
| 3 | Leere Zusicherung auf den OK-Knopf (`frontend-reviewer`) | Test |

**Nicht gezählt:** die korrigierte `key`-Begründung — sie fasst nur einen Kommentar an.

### Sign-off je Rolle

- **Creator (die geänderte Ebene):** zufrieden. Er sieht an jedem Fach, was er darf; wo er nichts darf,
  steht der Grund statt eines Knopfes, der ins `403` läuft. Ausdrücklich **zurückgestellt**: dass die Arten
  unter einem fremden Fach weiter voll bedienbar sind — das entspricht dem heutigen Server, und dass er
  sich ändern sollte, trägt B-157.
- **Vater (Supervisor):** Regressionszeuge, kein zweites Urteil. Kein Pfad seiner Ebene liegt im Diff
  (`frontend/src/vater/CatalogAdmin*`, `VaterKatalog`); Suite und Typecheck grün, die fünf E2E-Specs, die
  `/vater/katalog` anfassen, legen ihr Fach selbst an und löschen es als Eigentümer — die Verengung bricht
  keine davon (vom `frontend-reviewer` einzeln nachgesehen).
- **Sohn (Student):** Regressionszeuge. Kein Backend, keine Sohn-Fläche berührt; 821/821 unverändert grün.

## Retrospektive Sprint A

**Nachschau (Pflichthandlung, zuerst).** Geprüft wurde die Arbeit des vorigen Sprints, also die vier am
2026-08-11 abgenommenen und **nie nachgeschauten** Stories: **B-148, B-149, B-150, B-18**. Alle vier tragen
jetzt `nachgeschaut: 2026-08-12` — **auch die beiden ohne Fund**, sonst zählt der Blick nicht. Index vor der
Prüfung: *Nachgeschaut 91 von 100*.

**Ergebnis: zwei durchgekommene Defekte.** Beide selbst am Code nachgeprüft, nicht vom Prüfbericht
übernommen:

| Fund | Story | Wirkung |
| --- | --- | --- |
| [B-160](backlog/B-160-gesperrter-knopf-nennt-den-grund-nie.md) · P2 | `entgangen_bei: [B-150]` | Der `title` sitzt auf einem `disabled`-Knopf — Chromium und WebKit zeigen dort **nie** einen Tooltip. B-150s Akzeptanzkriterium 2 hat zwei Hälften („gesperrt" **und** „nennt den Grund"); die zweite ist nur scheinbar gebaut. Der Kommentar darüber behauptet ausdrücklich das Gegenteil |
| [B-161](backlog/B-161-alle-waehlen-macht-die-auswahl-unsichtbar.md) · **P1** | `entgangen_bei: [B-18]` | „Alle wählen" schreibt bis zu **500** Ids in `selected`, gerendert wird die erste Seite (≤100). Bis zu 400 Übungen sind unsichtbar **und** nicht abwählbar; und weil kein `setSelected` bei einem Filterwechsel läuft, kann der Plan Positionen tragen, die der gezeigte Filter ausschließt — mit Pflichtziel und damit `PenaltyCoins` fürs Kind |

Dazu ein **Nebenfund außerhalb aller vier Diffs**, also keiner Abnahme entgangen:
[B-162](backlog/B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) — eine gescheiterte Übungssuche im
Assistenten zeigt „Keine passenden Übungen im Katalog. Lege welche unter ‚Übungen' an", weil `exercises.error`
nirgends gerendert wird. Die B-111-Familie, diesmal mit falscher Handlungsanweisung. Bewusst **nicht** mit
B-161 zusammengelegt, obwohl beide in `VaterWizard.tsx` liegen: B-161 trägt ein `entgangen_bei`, dieses hier
nicht, und ein gemeinsamer Eintrag würde die Wirkungszahl verfälschen.

**Was die zwei Funde über die Tore sagen — und das ist die eigentliche Lehre:** B-161 ist **dieselbe
Fehlerfamilie**, die [nachtlauf.md](nachtlauf.md) für dieses Repo schon gemessen hat — *eine Bedingung, die
zwei Situationen zusammenzieht*. `selected` bedeutet „aus der aktuellen Trefferliste gewählt" **und**
„irgendwann früher gewählt", genau wie `Testable` (B-114) und „leer" (B-111). Die Familie ist damit nicht
abgeklungen; sie ist die stabilste Eigenschaft dieser Arbeit.

Und B-160 ist der Beleg dafür, dass das Rollengang-Tor **wirkt, wo es geführt wird**: B-150 hat ihren eigenen
Ausfall vorhergesagt („Was er *nicht* ersetzt: dass jemand den gesperrten Knopf im Browser gesehen hat"),
und genau dort saß der Defekt. Kein Reviewer, kein Rollengang, keine Testebene — drei ausgefallene Tore, ein
durchgekommener Fehler.

**Die beiden Stories ohne Fund sind kein „sauber", sondern benannte Prüfpunkte** (Auflage 4): bei B-148 die
drei Stellen, die *dieselbe* Bedingung benutzen und darum nicht auseinanderlaufen können, plus die gegen
`TextbooksController.cs:104-106` verifizierte Server-Behauptung; bei B-149 das Tor am echten Artefakt
(`contract.ts:25000` trägt den 7-gliedrigen Union) statt am Kommentar, plus der ausgeschlossene Grenzfall
zusammengesetzter Enum-Mitglieder. Je eine Reichweiten-Notiz steht in beiden Stories.

### Was die eigenen Tore durchgelassen haben

Nicht der Produktcode ist die Lehre dieses Sprints, sondern **ein Werkzeug, das das Verfahren beschädigt
hat, ohne es zu merken**: Der `/doctor`-Lauf derselben Sitzung hat vier Skills abgeschaltet, weil sie im
Zählfenster null Aufrufe hatten — darunter `web-design-guidelines`, das **Freigabe 7 dieses Auftrags
namentlich nennt**, und `accessibility`, dessen Nutzung als nummerierte Entscheidung in B-132 steht.

Die Fehlerklasse ist die des Repos: **eine Bedingung, die zwei Situationen zusammenzieht.** „Null Aufrufe im
Fenster" bedeutet entweder „wird nicht gebraucht" **oder** „der Anlass war noch nicht da" — und für ein
Werkzeug, das ein *Verfahren* an einer benannten Stelle vorschreibt, ist der zweite Fall der Normalfall.
Gefunden hat es kein Tor, sondern der Umstand, dass der nächste Auftrag zufällig genau diese Skills
brauchte.

### Vorgeschlagener Mechanismus (nach Freigabe 3 nicht gelandet)

**Ein Wächter, der jeden Skill-Namen aus `skillOverrides: off` gegen die Verfahrenstexte hält** — ein
`grep` über `docs/**.md` und `.claude/skills/**/SKILL.md` nach dem Namen; ein Treffer heißt: dieser Skill
ist Teil eines geschriebenen Verfahrens und darf nicht als „unbenutzt" abgeschaltet werden, ohne dass die
Stelle mitgeändert wird.

**Kosten:** Der Wächter kann nur lokale Einstellungen prüfen, und `~/.claude/settings.json` liegt außerhalb
des Repos — er müsste also als Skript laufen, das man aufruft, nicht als Testfall. Damit ist er schwächer
als ein Tor. **Warum trotzdem kein Tor:** Was hier zu prüfen wäre, ist eine Aussage über die
Maschine des Nutzers, nicht über das Repo — ein Test, der auf `~/.claude` zugreift, wäre in CI sinnlos und
lokal umgebungsabhängig.

**Die ehrlichere Alternative, die zur Entscheidung mitgehört:** die Regel gehört vielleicht gar nicht ins
Repo, sondern in den `doctor`-Ablauf selbst („bevor du einen Skill abschaltest, grep seinen Namen im
Repo"). Dann ist es kein Mechanismus dieses Projekts, sondern eine Korrektur an einem fremden Werkzeug —
und das ist eine Entscheidung, die der Nutzer treffen muss, nicht dieser Lauf.

## Offene Roadmap

Die dauerhafte Liste ist [docs/backlog/](backlog/README.md); hier nur die Begründung der aktuellen
Reihenfolge. **Fünf neue Stories** sind in diesem Sprint entstanden, alle als eigene Datei statt als Anhang
an eine laufende:

| Story | Warum sie nicht mitgeschluckt wurde |
| --- | --- |
| [B-156](backlog/B-156-ismine-heisst-anderswo-isown.md) | `isMine` gegen siebenmal `isOwn`. Anderes `art`, anderes `wo`, Vertragsfrage — und B-154s Ziel ist ohne sie erfüllt |
| [B-157](backlog/B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md) | **P2.** Die Arten unter einem fremden Fach sind ungeschützt. Eigene Fehlerklasse, außerhalb beider Diffs |
| [B-158](backlog/B-158-subjectscontroller-drei-kleine-reste.md) | Drei kleine Reste im `SubjectsController`, zwei davon älter als B-13s Commit |
| [B-159](backlog/B-159-reihe-ohne-owner-behauptet-fremden-ersteller.md) | Dieselbe Fall-Unterscheidung fehlt bei der Lehrwerk-Reihe. Andere Datei, andere Katalogebene |

Dazu **drei aus der Nachschau**, die nicht aus diesem Sprint stammen, sondern aus der Arbeit des vorigen:
[B-160](backlog/B-160-gesperrter-knopf-nennt-den-grund-nie.md) (P2, `entgangen_bei: B-150`),
[B-161](backlog/B-161-alle-waehlen-macht-die-auswahl-unsichtbar.md) (**P1**, `entgangen_bei: B-18`) und
[B-162](backlog/B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) (P3, keiner Abnahme entgangen).

**Nächster Sprint, empfohlen: B-161 zuerst** — es ist das einzige **P1** unter den offenen Stories, und
zwar aus einem Grund, der nicht am Vater hängt: ein ungewollt angelegter Lehrplan trägt Pflichtziele, und
eine gerissene Pflicht kostet das Kind Münzen (`PenaltyCoins`). Derselbe Grund, aus dem B-114 P1 war.
Danach B-157 (P2, Server-Hälfte dessen, was Sprint A im UI sichtbar gemacht hat) — aber erst nach der
Entscheidung zu seinem offenen Punkt 2, siehe „Angehalten" —, dann B-160. B-145 und B-141 als der
Namensgleichheits-Faden rücken damit nach hinten: sie sind der ältere Plan, nicht der dringendere.

## Konkreter Änderungsstand (für Review)

| Commit | Inhalt |
| --- | --- |
| `003332d` | B-13 abgenommen: neuer Testfall für die 403-vor-409-Reihenfolge, Kommentar nennt den Test; drei Stories neu (B-156/157/158) |
| `22ac8f4` | B-154 abgenommen: `SubjectRow`, `CatalogAdmin.test.tsx` (5 Fälle), zwei Textkorrekturen, `VaterKatalog`-Vorspann; B-159 neu |
| (folgend) | Nachschau: `nachgeschaut: 2026-08-12` auf B-148/149/150/18, drei neue Stories B-160/161/162, dieses Protokoll, Index |

**Kein Produktcode in der Nachschau angefasst.** Die zwei gefundenen Defekte sind als Stories abgelegt, nicht
behoben: Freigabe 3 verlangt das sofortige Beheben für Funde **im Increment dieses Sprints**, und diese
liegen ausdrücklich in benachbarter, älterer Fläche. Aus demselben Grund zählen sie **nicht** in den
Fehlerzähler (der bleibt bei 3 von 5).

**Nicht committet und bewusst offen:** `CLAUDE.md` und `backend/CLAUDE.md` im Arbeitsbaum — das sind die
Änderungen des `/doctor`-Laufs (Verlagerung des C#-Handwerks in eine neue `backend/CLAUDE.md`), die der
Nutzer prüfen wollte. Sie wurden per pfad-genauem `git add` aus beiden Commits herausgehalten.

**Nichts gepusht.**
