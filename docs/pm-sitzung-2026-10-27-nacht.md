---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-27, Lauf E]
---

# PM-Sitzung: Nachtlauf 2026-10-27 – Nacharbeit B-05, dann NP-9 und Folgepakete

**Datum:** 2026-10-27  ·  **Moderation:** PM (unbeaufsichtigt nach der Morgen-Entscheidung)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Lauf D (B-05) abschließen, danach die Pakete NP-9, NP-10 und NP-11 nacheinander fahren.

Auftrag: der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10. Ausgangslage: HEAD `a4b712c`,
Baum sauber, nichts gepusht. Lauf-Reihenfolge nach der [Morgen-Entscheidung vom
2026-10-26](pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26): A–D sind gelaufen
([Protokoll](pm-sitzung-2026-10-26-tag.md)), Lauf D endete an Freigabe 3 mit Zähler 6.

## Morgen-Entscheidung 2026-10-27

Der Nutzer hat die beiden offenen Punkte aus [Lauf D](pm-sitzung-2026-10-26-tag.md#wiederaufnahme--offene-entscheidungen-für-den-nutzer)
im Dialog entschieden:

1. **Lauf D wird als Nacharbeit mit neuem Zähler (0) fortgesetzt**: Fund 6 (Tastaturfokus fällt nach jeder Karte auf
   `body`) und die übrigen Punkte (a)–(e) aus dem Verlauf von B-05, dazu die Testlücken aus B-283. Danach Abnahme von
   B-05, dann NP-9, NP-10, NP-11.
2. **🟢-Hinweise zählen nicht mehr auf den Fünfer-Zähler.** Gezählt wird ein Fund nur noch, wenn er 🟡 oder 🔴 ist
   *und* seine Behebung Code oder Tests ändert. 🟢-Hinweise werden behoben, wenn das billig ist, sonst als Story
   zurückgestellt – beides ohne Zählung, im Protokoll weiter als „ohne Zählung“ ausgewiesen. Festgehalten als
   Freigabe 10 im Auftragstext ([nachtlauf.md](nachtlauf.md)).

Die sieben Retro-Vorschläge des 2026-10-26 (A, A2, B–F) bleiben unentschieden; dieser Lauf schlägt weiter nur vor.

## Lauf E, Teil 1 — Nacharbeit B-05 (Buchstaben ordnen), Zähler 0

**Sprint-Ziel (unverändert aus NP-7):** *Der Sohn kann ein Wort aus vertauschten Buchstaben zusammensetzen, per Klick
oder Tastatur* – und jetzt auch **mehrere Karten hintereinander nur mit der Tastatur**, ohne dass der Fokus verloren
geht. Umfang: [B-05](backlog/B-05-buchstaben-tausch.md) (offene Funde a–e) und [B-283](backlog/B-283-buchstaben-ordnen-testluecken.md)
(`Defekt`, Testlücken, Freigabe 1). **Zähler: 0.**

**Entwickler-Brief:** Der Fokus gehört in die Komponente, nicht in die Aufrufer: `LetterTiles` setzt ihren Zustand
selbst zurück, wenn sie neue Kacheln bekommt (bisher per `key` von außen erzwungen), und holt den Fokus auf die erste
Kachel, wenn er vorher im Block stand oder verloren ging (`body`). Stand er woanders (etwa auf „Prüfen“ nach einem
Mausklick), bleibt er dort. Strg/Alt/Cmd-Kombinationen gehören dem Browser. Die Feldhilfe und der Plan-Assistent
nennen die Stufe. Backend: nur Tests (B-283), kein Verhalten ändert sich, keine Migration.

### Iteration

- **(a) Fokus** – `LetterTiles` ohne `key`: Zustand im Render zurückgesetzt, wenn `tiles` wechselt; ein Effekt auf `tiles`
  holt den Fokus auf die erste Kachel, wenn er im Block stand oder auf `body` lag (Kästchen-Karte davor), sonst bleibt er.
  Vitest: vier neue Fälle, Mutanten „nie“/„immer“/„kein Reset“/„kein Modifier-Wächter“ → je **1 failed**, danach **10/10**.
  E2E mit vier Karten, zweite Tastatur-Karte ohne Fokussieren: gegen die alte Komponente **1 failed**
  (`toBeFocused`: expected focused, received inactive), danach **1 passed**.
- **(c)** Strg/Alt/Cmd ignoriert. **(d)** Feldhilfe nennt „gemischte Buchstaben ordnen“. **(e)** Assistent bietet
  „6 · Auswahl“/„7 · Ordnen“ (Kurznamen-Familie wie `STAGE_LABEL`; Langnamen in `VOCAB_FORMS`).
- **B-283** – Prüfungstest mit zweitem Versuch auf zweiter Position (Mutante Seed `attemptId` → 1: **1 failed**),
  Vorschau-Test `stage=7` (Mutante `LetterTiles` → null: **1 failed**); die Spec wertet jede Antwort über `wasCorrect`.
  Filterlauf **13/13 `exit=0`**, Vitest gesamt **454/454**.

### Review (nacheinander, je Bereich)

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| – | `pugling-reviewer` | Testkommentar begründet die zweite Position falsch; Summary als Umbau-Erzählung | zwei Kommentare | nein (nur Kommentar) |
| 1 | `frontend-reviewer` 🟡 | Der kommentierte Maus-Weg „Fokus bleibt auf ‚Prüfen‘“ entsteht nicht: der gesperrte Knopf gibt den Fokus ab | Spec auf `Serial` umgestellt, `toBeFocused` nach jedem der drei Übergänge; gemessen **1 passed** (Fokus auf der Kachel); Kommentare berichtigt | ja (Test) |
| – | `frontend-reviewer` 🟢 | Reset-Vertrag nur implizit; dritte Handkopie der Stufenliste; Fokus-Sprung im Vorschau-Dialog; Aufgabe wird nach Kartenwechsel nicht angesagt | JSDoc; Assistent liest `VOCAB_FORMS`; notiert; [B-286](backlog/B-286-kartenwechsel-sagt-die-aufgabe-nicht-an.md) | nein (Freigabe 10) |

**Zähler: 1.** Nach den Behebungen: Build grün, Vitest **454/454**, `assistent.spec` + `feldhilfe.spec` **6 passed**,
Backend **949/949 `exit=0`**.

### Rollengang / Abnahme

Kein Browser verbunden (Extension nicht erreichbar), darum E2E als Rollengang (Freigabe 6):

- **Sohn:** `buchstaben-ordnen.spec.ts` fährt vier Karten in serieller Reihenfolge – Kästchen per Enter, Kacheln per
  Tastatur ohne erneutes Fokussieren, Kacheln per Klick und „Prüfen“, Kacheln per Tastatur – und prüft je Antwort die
  Server-Wertung. *„Ich tippe einfach weiter, ohne erst irgendwo hinzuklicken.“* **1 passed.**
- **Vater:** Assistent bietet alle sieben Stufen aus derselben Liste wie das Anlege-Formular; Feldhilfe nennt die
  Form. Regressionszeugen `assistent.spec`, `feldhilfe.spec` **6 passed**; Playwright voll **96 passed** vor der
  Spec-Umstellung.
- **Creator:** nur Tests und Vorschau-Test berührt, kein Weg geändert; Suite **949/949 `exit=0`**.

B-05 und B-283 `abgenommen`.

### Retrospektive

**Nachschau:** NP-3 (`b644081`: B-245, B-28, B-230) von einem frischen Prüfer gegen den Commit geprüft, je Behauptung
eine Prüfzeile in den Stories. **Ein Code-Entgleiten** (B-245): löscht ein Vater sich selbst, bleiben die Konten seiner
verwaisenden Kinder als leere Hülle, der Kind-Token bleibt gültig → [B-285](backlog/B-285-kind-konto-bleibt-nach-vater-loeschung.md)
(`entgangen_bei: B-245`). Ein Doku-Entgleiten (`MeResponse.AccountId` „null for a legacy token“) sofort berichtigt.
B-28 und B-230 ohne Befund, `nachgeschaut` an allen drei gesetzt. Index vor der Abnahme: **Nachgeschaut: 198 von 198**, danach 198 von 200 (B-05, B-283 stehen für die nächste Nachschau an).

**Was die eigenen Tore durchgelassen haben:** (1) Eine Behauptung über Browser-Fokus stand als JSDoc und als
Vitest-Fall („Maus auf ‚Prüfen‘ behält den Fokus“), und beide waren über einen Zustand formuliert, den die App nicht
erzeugt – der Knopf ist in dem Moment gesperrt. jsdom/happy-dom kennen die Fokus-Abgabe eines gesperrten Knopfs
nicht; nur die E2E hat es gezeigt. Das ist Vorschlag **(D)** vom 2026-10-26 ein zweites Mal gemessen: *Fokus-
Zusicherungen, die `disabled` oder `body` berühren, gehören in eine E2E-Zeile, nicht in Vitest.* (2) Die B-245-Lücke
ist eine **Asymmetrie zweier Löschpfade** (Adult vs. Child), die der Reviewer des Sprints nicht gesehen hat, weil er
den Diff las und der zweite Pfad außerhalb lag.

**Vorschlag, nicht gelandet (Freigabe 3):** **(G)** eine suite-weite Invariante in `PuglingWebAppFactory` –
*nach dem Lauf existiert kein `Account` ohne `AccountProfile`* – als Tor; sie hätte B-285 an jedem Test gezeigt, der
einen Vater mit Kind löscht, ohne dass jemand den zweiten Pfad kennen muss. Bewusst ungedeckt bleibt die Frage,
welche der Review-Kommentare wahr sind: ein Kommentar ist nicht ausführbar.

**Commit:** `4e50370` (nicht gepusht).

## Lauf E, Teil 2 — B-285 aus der Nachschau (Kind-Konto nach Vater-Löschung), Zähler 0

**Sprint-Ziel:** *Der Sohn eines Vaters, der sein Konto gelöscht hat, kommt mit seinem alten Token nirgends mehr
hinein – genauso wie nach dem Löschen seines eigenen Profils.* Umfang: [B-285](backlog/B-285-kind-konto-bleibt-nach-vater-loeschung.md)
(`Defekt`, Freigabe 1, XS, keine Migration). Vor NP-9 gezogen, weil ein gültiger Token auf ein gelöschtes Kind
dieselbe 500-Klasse wieder öffnet, die NP-3 gerade geschlossen hatte. **Zähler: 0.**

**Entwickler-Brief:** Dieselbe Regel wie in `ChildrenController.Delete` an den Vater-Pfad: nach dem Sammeln der
verwaisenden Kinder die Konten entfernen, deren Profile alle auf diese Kinder zeigen. Kein Vertrag, kein Schema.

### Iteration

- Test zuerst: `Token_Eines_Mit_Dem_Vater_Geloeschten_Kindes_Bekommt_401` – der frische Vater ist alleiniger
  Betreuer, das Kind loggt sich ein, der Vater löscht sich. **Rote Probe:** erwartet 401, gemessen **200** (1 failed,
  2 passed). Fix in `AdultsController.Delete`, danach Token-, Lebenszyklus- und PIN-Tests **23/23 `exit=0`**.

### Review

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | Das Überleben des Kontos eines ko-betreuten Kindes war nicht zugesichert | Zeile in `Loeschen_Laesst_Das_Ko_Betreute_Kind_Stehen`; Mutante „alle betreuten Kinder“ → **3 failed** | ja (Test) |
| 2 | `pugling-reviewer` 🟡 | Gemischtes Konto (Erwachsenen- + Student-Profil) fällt durch beide getrennten Abfragen | **ein** Prädikat; neuer Fall sät das gemischte Konto über den Kontext; Probe gegen die getrennten Abfragen **1 failed**, danach 14/14 | ja (Code+Test) |
| – | `pugling-reviewer` 🟢 | Summary nennt die Kinder-Konten nicht; Leer-Abfrage ohne Schutz | Summary berichtigt; Abfrage bleibt symmetrisch | nein |

**Zähler: 2.** Ein erster „roter“ Lauf des gemischten Falls fiel an einer falschen Aufbau-Zusicherung (3 statt 2
Profile – der Vater trägt Creator *und* Supervisor) und zählt nicht als Probe; die Probe wurde danach gegen die
tatsächlich getrennten Abfragen gefahren. Außerhalb des Diffs: `ChildrenController.Delete` hat dieselbe latente
Lücke → Restpunkt in B-282. Suite **951/951 `exit=0`**.

### Rollengang / Abnahme

Kein Browser; der Weg ist ein reiner API-Pfad ohne Oberfläche (ein Vater löscht sich im Profil, das Kind ist danach
draußen). Die drei Fälle in `DeletedAccountTokenTests` fahren ihn: Vater löscht sich → Kind-Token 401; Ko-Betreuung
bleibt samt Kind-Login; gemischtes Konto verschwindet. Vater und Sohn als Zeugen über die Suite, Creator unberührt.
B-285 `abgenommen`.

### Retrospektive

**Nachschau:** Teil 1 (`4e50370`: B-05, B-283) von einem frischen Prüfer geprüft, je Behauptung eine Zeile in den
Stories. **Zwei Test-Entgleiten:** eine Zusicherung im Fokus-Fall, die den Ausgangszustand prüft →
[B-287](backlog/B-287-fokus-test-prueft-den-ausgangszustand.md) (`entgangen_bei: B-05`); der Prüfungstest misst
„neu je Position“ statt „neu je Versuch“ → [B-288](backlog/B-288-pruefungstest-misst-neu-je-position.md)
(`entgangen_bei: B-283`). Ein Doku-Entgleiten (Satz (e) in B-05) berichtigt. `nachgeschaut` an B-05 und B-283 gesetzt.
Index: **Nachgeschaut: 200 von 201** (B-285 steht an).

**Was die eigenen Tore durchgelassen haben:** B-287 ist die **fünfte** Messung der Klasse „Zusicherung prüft den
Ausgangszustand“ (CLAUDE.md zählt vier), und B-288 die Klasse „Name verspricht eine Unterscheidung, der Aufbau trägt
sie nicht“ – beide trotz des Pflichtschritts aus pm-loop Step 5, der vor dem Review je Fall die Mutante verlangt.
Ich habe die Mutanten gemessen, aber nur die, die ich selbst erwartet habe; die zweite Zusicherung im selben Fall
und die Positions-Mutante habe ich nicht gefragt. Der Pflichtschritt ist eine Selbstauskunft, und sie hat genau an
den Zeilen versagt, die neben einer scharfen Zusicherung stehen.

**Vorschlag, nicht gelandet (Freigabe 3):** **(H)** der Pflichtschritt wandert als Frage in die beiden
Review-Agenten (`pugling-reviewer`, `frontend-reviewer`): *„Nenne je neuer `Assert`/`expect`-Zeile die Mutante, die sie
rot macht; gibt es keine, ist es ein Fund.“* Das ist derselbe Blick, der heute zweimal erst in der Nachschau kam, nur
eine Runde früher und von jemandem, der den Code nicht geschrieben hat. Kein Tor – ein Tor müsste Testdaten lesen.
Vorschlag **(G)** aus Teil 1 (Suite-Invariante „kein Konto ohne Profil“) hätte B-285 *und* das gemischte Konto
gezeigt; er bleibt stehen.

**Commit:** `01631ad` (nicht gepusht).

## Lauf E, Teil 3 — Nachschau-Defekte B-287, B-288 (Tests), Zähler 0

**Sprint-Ziel:** *Die beiden Tests zu „Buchstaben ordnen“ beweisen, was ihr Name verspricht: der Fokus-Fall fällt ohne
Reset, der Prüfungsfall fällt mit einem positionsgebundenen Seed.* Umfang: [B-287](backlog/B-287-fokus-test-prueft-den-ausgangszustand.md),
[B-288](backlog/B-288-pruefungstest-misst-neu-je-position.md) (`Defekt`, Freigabe 1, je XS, nur Tests). **Zähler: 0.**

**Entwickler-Brief:** Kein Verhalten ändert sich. B-287: vor dem Kartenwechsel eine Kachel legen und den Wert halten,
damit allein der Kartenwechsel zurücksetzt. B-288: den ersten Versuch abschließen und den zweiten auf **derselben**
Position starten.

### Iteration

- **B-287:** Harness `MitWert` mit referenzstabilen Karten; Mutante „kein Render-Reset“ → **2 failed** (vorher fiel nur
  der Reset-Fall), danach **10/10**.
- **B-288:** `/answer` + `/submit`, zweiter Start auf derselben Position mit zugesicherter neuer Id; Mutante Seed aus
  `positionId` → **1 failed** (vorher grün), danach **12/12**.

### Review (nacheinander je Bereich)

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| – | `pugling-reviewer` (B-288) 🟡 | Kommentar als Umbau-Erzählung | umformuliert | nein (Kommentar) |
| – | `pugling-reviewer` (B-288) 🟢 | Der ältere Übungs-Test hat denselben blinden Fleck, seine Begründung trug nicht | nachgezogen: zweite Sitzung auf derselben Position vor dem Review; Mutante Seed aus `exercise.Id` → **1 failed**, danach 12/12 | nein (Freigabe 10) |
| – | `frontend-reviewer` (B-287) 🟢 | Kommentar begründete die Konstanten falsch | umformuliert | nein |
| – | `pugling-reviewer` (B-289) 🟡 | Summary in `TokenSubjectCheck.cs` ohne seine Prämisse | Prämisse wiederhergestellt, Token-Vektor an B-282 | nein (Kommentar) |
| – | `pugling-reviewer` (B-289) 🟢 | Sä-Block doppelt | Helfer mit Form-Zusicherung | nein (Freigabe 10) |

**Zähler: 0.** Dazu aus der Nachschau zu Teil 2 (unten) der dritte Test-Defekt **B-289** in diesen Sprint gezogen:
Ausschluss-Fall für die `All`-Klausel des Konten-Sweeps, Mutante „`All` entfernt“ → **1 failed**. Stand: Backend
**952/952 `exit=0`**, Vitest **454/454**.

### Rollengang / Abnahme

Drei Test-Defekte, kein Verhalten ändert sich: der Rollengang ist der jeweilige Test selbst (Komponententest,
zwei Integrationstests), die Rollen sind Zeugen über die Suiten. B-287, B-288, B-289 `abgenommen`.

### Retrospektive

**Nachschau:** Teil 2 (`01631ad`, B-285) von einem frischen Prüfer mit einem Wegwerf-Programm gegen In-Memory-SQLite
geprüft (SQL-Übersetzung belegt, Lehrer/Admin/Doppelrolle nachgerechnet, kein Wiederbelebungs-Pfad). **Ein
Test-Entgleiten:** die `All`-Hälfte des Sweeps ohne rote Probe → [B-289](backlog/B-289-all-klausel-des-konten-sweeps-ohne-probe.md)
(`entgangen_bei: B-285`), in diesem Teil behoben. Drei veraltete Kommentare berichtigt, zwei hypothetische
Restlücken an B-282. `nachgeschaut` an B-285 gesetzt. Index nach der Abnahme: siehe README (B-287, B-288, B-289
stehen für die nächste Nachschau an).

**Was die eigenen Tore durchgelassen haben:** zum dritten Mal in diesem Lauf eine Zusicherung, die auf *einer* Seite
einer Unterscheidung steht (Einschluss ohne Ausschluss, „neu je Versuch“ auf zwei Positionen, Fokus ohne gelegte
Kachel). Der Pflichtschritt aus pm-loop Step 5 fragt „welche Mutante macht den Fall rot“ – er fragt nicht „welche
zweite Mutante bleibt grün“. Alle drei Funde kamen von Prüfern, die den Code nicht geschrieben hatten.

**Vorschlag, nicht gelandet (Freigabe 3):** **(H)** aus Teil 2 bleibt und wird schärfer: die Review-Agenten fragen
je neuer Zusicherung nach der Mutante, die sie rot macht, **und** nach der naheliegenden Mutante, die grün bliebe
(die andere Seite derselben Unterscheidung). Kein Tor; kein Mechanismus liest Testdaten.

**Commit:** `12eab76` (nicht gepusht).
