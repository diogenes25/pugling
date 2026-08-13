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

## Sprint B — Ziel & Umfang

**Sprint-Ziel:** *Der Vater legt keinen Lehrplan mehr an, der Übungen enthält, die er nie gesehen hat.*

**Umfang:** B-161 allein. Ein Sprint von einer Story ist zulässig (`pm-loop`, „The Sprint": die Obergrenze
hat einen Grund, eine Untergrenze hätte keinen) — und hier ist er richtig: es ist das einzige **P1** des
Bereichs, und kein anderer offener Punkt dient diesem Ziel.

**Refinement:** B-161 von `ausformuliert` auf `geschaetzt` (autonom gegrillt, `art: Defekt`). Fünf
Entscheidungen; die tragende ist eine **Nicht**-Änderung — siehe unten.

**Entwickler-Brief:** Kein Backend-Anteil. Die Regel („was gilt noch, wenn sich die Suche ändert") wandert
als **reine Funktion** nach `wizardSearch.ts`, weil `VaterWizard` an vier Ladevorgängen hängt und als Ganzes
nicht ohne Netz zu rendern ist — nur als Funktion ist die Regel rot zu bekommen. Guards: keine neuen.
Testweg: `wizardSearch.test.ts` plus Browser-Rollengang.

## Iteration B — umgesetzt

Drei reine Funktionen (`wizardFilterKey`, `unsichtbareAuswahl`, `auswahlNachFilterwechsel`), ein Effekt auf
den Kriterien-Schlüssel, ein „Auswahl leeren"-Knopf, und die Zahl nennt die Unsichtbaren mit.

**Die wichtigste Entscheidung war, etwas *nicht* zu tun.** „Alle wählen" behält seine 500. Die Grenze auf die
geladene Seite zurückzudrehen wäre die einfachere Reparatur gewesen — und hätte den abgenommenen Nutzen von
B-18 abgebaut. Das ist eine Produktentscheidung, die ein autonomer Lauf nicht trifft (Freigabe 1).
Stattdessen sagt die Zahl die Wahrheit.

**Verifikation:** `npx vitest run` **269/269 grün**, `npm run build` grün,
`npx playwright test assistent.spec.ts` **1 passed** (27,3 s) — sie fährt den `selectAll`-Pfad und damit den
Ort des Korrektheitsfunds. Backend unberührt.

**Rote Probe (Auflage 5):** `auswahlNachFilterwechsel` auf „immer `null`" — dem Altverhalten — lässt **3 von
18** Fällen fallen.

## Runde 3 — Re-Review / Abnahme (Sprint B)

### Rollengang (Freigabe 6) — im echten Browser

Server nach der letzten Änderung gestartet, Wegwerf-DB, Assistent bis Schritt 3. Sechs Übungen gewählt →
„(6 gewählt)" plus erschienener Leeren-Knopf; Typ-Filter auf „Lückentext" → **„(0 gewählt)"** und
**„Auswahl zurückgesetzt (6 Übungen), weil sich die Suche geändert hat."** über zwei ungehakten Treffern.
Vor dem Fix hätte dort „(6 gewählt)" neben „2 passende Übungen" gestanden.

**Ehrliche Grenze:** AK 4 (die „davon M nicht sichtbar"-Zahl) ist im Browser **nicht** gesehen — der Seed hat
sechs Übungen im Fach, nicht die >100, die den Fall auslösen. Die Arithmetik deckt der Unit-Test
(500/100 → 400), das Rendern ist eine einzeilige Bedingung.

### `frontend-reviewer`: ein abnahmerelevanter Fund, und er war der Ertrag des Sprints

**`selectAll` hatte kein Generationen-Gate.** Der `take:500`-Nachschlag ist der einzige Ladeweg neben
`useAsync` (das sein eigenes `cancelled`-Flag hat), und die Filterfelder sind währenddessen nicht gesperrt.
Wer während des Ladens den Filter verengte, bekam die Ids der **verworfenen** Suche zurückgeschrieben — der
Effekt hatte korrekt geleert, danach stand die Auswahl wieder da, und nichts leerte sie je wieder. **Meine
eigene Invariante hielt nur für den synchronen Pfad**, und derselbe Weg machte „Auswahl leeren" während des
Ladens stumm rückgängig. Sofort behoben (Freigabe 3) mit dem Schlüssel, der ohnehin dasteht.

Vier weitere Funde mitgenommen, alle nicht blockierend: die Hinweis-Region war für Screenreader stumm **und**
optisch fast unsichtbar (`.banner` allein trägt keinen Hintergrund) — jetzt dauerhaft montierte
`role="status"`-Region nach dem Vorbild von `StatusBanner`, plus eine `.banner.info`-Variante, weil hier
nichts gelang und nichts fehlschlug; „Auswahl leeren" verschwand nach dem Klick unter dem Finger und nahm den
Fokus mit; die E2E-Zusicherung auf die Überschrift wäre **genau in dem Fall gefallen, für den ihr Kommentar
geschrieben wurde**; und „1 Übungen" war ungebeugt. Dazu ein Testfall von mir, der **nie** rot werden konnte —
jetzt ehrlich als Absichtserklärung beschriftet statt als Beleg gezählt, dieselbe Klasse wie der B-154-Fund.

### Sign-off je Rolle

- **Vater (Supervisor, die geänderte Ebene):** zufrieden. Ausdrücklich zurückgestellt: einzelne Übungen
  jenseits der geladenen Seite bleiben unerreichbar; abwählen geht nur ganz. Ein echtes Paging über die
  Auswahl ist kein Teil dieser Story (Entscheidung 3).
- **Creator / Sohn:** Regressionszeugen. Kein Pfad ihrer Ebenen im Diff; Suite, Typecheck und die
  Assistenten-E2E grün, Backend unberührt (821/821 unverändert).

### Fehlerzähler (Freigabe 3): 5 von 5 — die Obergrenze ist erreicht

| # | Fund | Was der Fix anfasste |
| --- | --- | --- |
| 1 | `selectAll` ohne Generationen-Gate (Korrektheit) | Code |
| 2 | Hinweis-Region stumm und optisch unsichtbar | Code + CSS |
| 3 | Leeren-Knopf nimmt beim Verschwinden den Fokus mit | Code |
| 4 | E2E-Zusicherung bricht, sobald die Überschrift die Unsichtbaren nennt | Test |
| 5 | „1 Übungen" ungebeugt + ein unfalsifizierbarer Testfall | Code + Test |

**Nicht gezählt:** die korrigierte Begründung am Hinweis-Guard und die Umbenennung
`vorigerFilterKey → geltenderFilterKey` (Kommentar und Name), sowie ein Fund **außerhalb** des Diffs, der
nach [B-162](backlog/B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) wanderte.

**Damit endet der Lauf hier — und zwar an einem Ergebnis, nicht an einer leeren Liste.** Fünf ist die
Obergrenze, nicht ihre Überschreitung; ein weiterer Fund in diesem Sprint hätte die Nacht sofort beendet. Der
Zähler so knapp am Anschlag ist genau das Signal, für das er gebaut ist: fünf Funde an einer `S`-Story sind
kein normales Finden-und-Beheben mehr. **Ein Sprint C wird nicht begonnen.**

## Retrospektive Sprint B

**Nachschau:** Der vorige Sprint ist Sprint A **dieses** Protokolls. Seine beiden Abnahmen (B-13, B-154)
tragen `nachgeschaut: ""` — sie sind **nicht** nachgeschaut, und das ist die ehrliche Angabe: eine Nachschau
auf Arbeit von vor zwei Stunden, im selben Kontext, von derselben Instanz, wäre der flüchtige Blick, den
[nachtlauf.md](nachtlauf.md) ausdrücklich für schlimmer als keinen hält („sie vergiftet den Nenner"). Sie
gehört in den nächsten Lauf; der Index führt sie im Arbeitsvorrat.

### Was die eigenen Tore durchgelassen haben

**Zweimal in einer Nacht habe ich eine Zusicherung geschrieben, die nicht fehlschlagen kann** — in B-154
(eine Prüfung auf das Fehlen eines Knopfes, der beim Mount ohnehin nie existiert) und in B-161 (eine Prüfung,
dass ein Feld nicht im Schlüssel steht, das `JSON.stringify` ohnehin wegwirft). Beide fand der Reviewer,
keines der Tore. Das ist keine Unachtsamkeit an zwei Stellen, sondern ein Muster: eine Zusicherung, die den
**Ausgangszustand** prüft statt den Übergang, ist grün, bevor irgendetwas gebaut ist.

Und die rote Probe fängt das **nicht** zuverlässig — sie prüft, ob *irgendein* Fall fällt, nicht ob *jeder*
Fall etwas trägt. In B-154 fielen 3 von 5, in B-161 3 von 18; die leeren Zusicherungen lagen jeweils unter
den grün gebliebenen und waren damit unsichtbar.

### Vorgeschlagener Mechanismus (nach Freigabe 3 nicht gelandet)

**Die rote Probe je Fall statt je Datei:** Wer einen Regressionstest schreibt, geht dessen Fälle einzeln durch
und benennt für jeden, *welche* Änderung ihn rot machen würde — und wenn die Antwort „keine" ist, wird der
Fall umgeschrieben oder als Absichtserklärung beschriftet (so steht er jetzt in `wizardSearch.test.ts`).

**Kosten:** Das ist Prosa, kein Tor, und genau die Sorte Regel, die dieses Repo misstrauisch betrachtet
(„mechanische Tore statt Disziplin"). **Warum trotzdem kein Tor:** Mutationstesting wäre der mechanische Weg
— das ist ein Werkzeug, keine Zeile, und es einzuführen ist eine eigene Story, keine Retro-Handlung. **Wo die
Prosa hingehört, falls du sie landen lässt:** in `frontend/CLAUDE.md` neben die bestehende Testebenen-Regel —
und dann muss dieselbe Runde das Budget bezahlen, das die Datei schon reißt.

**Die zweite Lehre gehört ebenfalls dir, nicht mir:** Der Reviewer hat in beiden Sprints Funde geliefert, die
kein Test und kein Rollengang gefunden hätte — der Generationen-Renner in `selectAll` ist der klarste Fall.
Zwei Sprints, zwei Reviewer-Läufe, zwei Korrektheitsfunde: das Tor **wirkt**, und der Zähler von Freigabe 3
bestraft es dafür. Ob fünf die richtige Grenze ist, wenn ein *funktionierender* Prüfschritt sie füllt, ist die
Frage, die dieser Lauf aufwirft.

## Nachtrag 2026-08-13 — was der Nutzer aus den Vorschlägen gemacht hat

Freigabe 3 hat die Retrospektiven auf **Vorschlagen** beschränkt; hier der Ausgang, damit die offene Schleife
im Protokoll geschlossen ist und nicht im Nichts endet.

| Vorschlag | Ausgang |
| --- | --- |
| **Rote Probe je Fall statt je Datei** (Retro Sprint B) | **Gelandet** — als Konventions-Zeile im Startkontext ([CLAUDE.md](../CLAUDE.md)), nicht in `frontend/CLAUDE.md` wie vorgeschlagen. Die Verlagerung ist eine Korrektur am Vorschlag selbst: von den vier gemessenen Fällen sind **zwei Backend** (B-157s Akzeptanzkriterien 4 und 8), die Regel gilt also, wo Tests geschrieben werden — und das sind beide Seiten. Bezahlt ist sie aus dem Platz, den die `backend/CLAUDE.md`-Verlagerung geschaffen hat: Root steht bei 15.359 von 19.000 B |
| **Skill-Wächter** (Retro Sprint A) | **Nicht ins Repo**, wie empfohlen. Ein Test, der `~/.claude` liest, wäre in CI sinnlos und lokal umgebungsabhängig; die Regel liegt stattdessen als Gedächtnis-Notiz beim `doctor`-Ablauf, wo der Fehler entstand |
| **Fehlerzähler: ist 5 die richtige Grenze?** | **Offen** — bewusst. Zwei Sprints, zwei Reviewer-Läufe, zwei echte Korrektheitsfunde, und der Zähler stand am Anschlag: er bestraft gerade den Prüfschritt, der wirkt. Die Frage braucht mehr als zwei Datenpunkte |

**Der vierte Fall kam am Tag danach dazu** und stützt die gelandete Regel: [B-157](backlog/B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md)
hatte zwei Akzeptanzkriterien, die nur als Kommentar existierten — AK 4 (nie las ein *fremder* Creator die
Arten) und AK 7 (kein Test sah je das Anlege-Formular, weil `CategoryRows` per Konstruktion keines enthält).
Beide fand ein Reviewer, keines ein Tor.

## Konkreter Änderungsstand (für Review)

| Commit | Inhalt |
| --- | --- |
| `003332d` | B-13 abgenommen: neuer Testfall für die 403-vor-409-Reihenfolge, Kommentar nennt den Test; drei Stories neu (B-156/157/158) |
| `22ac8f4` | B-154 abgenommen: `SubjectRow`, `CatalogAdmin.test.tsx` (5 Fälle), zwei Textkorrekturen, `VaterKatalog`-Vorspann; B-159 neu |
| `0674dfb` | Nachschau: `nachgeschaut: 2026-08-12` auf B-148/149/150/18, drei neue Stories B-160/161/162 |
| `e997393` | B-161 abgenommen (Sprint B): drei reine Funktionen, Generationen-Gate in `selectAll`, `.banner.info`, E2E-Zusicherung entschärft |
| (folgend) | dieses Protokoll und der neu erzeugte Index |

**Kein Produktcode in der Nachschau angefasst.** Die zwei gefundenen Defekte sind als Stories abgelegt, nicht
behoben: Freigabe 3 verlangt das sofortige Beheben für Funde **im Increment dieses Sprints**, und diese
liegen ausdrücklich in benachbarter, älterer Fläche. Aus demselben Grund zählen sie **nicht** in den
Fehlerzähler (der bleibt bei 3 von 5).

**Nicht committet und bewusst offen:** `CLAUDE.md` und `backend/CLAUDE.md` im Arbeitsbaum — das sind die
Änderungen des `/doctor`-Laufs (Verlagerung des C#-Handwerks in eine neue `backend/CLAUDE.md`), die der
Nutzer prüfen wollte. Sie wurden per pfad-genauem `git add` aus beiden Commits herausgehalten.

**Nichts gepusht.**
