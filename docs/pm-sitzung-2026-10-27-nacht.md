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

## Lauf E, Teil 4 — NP-9 Grammatik als Thema (B-155), Zähler 0

**Sprint-Ziel (aus dem Paket):** *Der Creator ordnet Regelaufgaben einem Grammatik-Thema zu, und der Vater findet sie
darüber – quer über Lehrwerke und Klassenstufen.* Umfang: [B-155](backlog/B-155-grammatik-themen-als-tags.md)
(`geschaetzt`, Freigabe 8, L mit Migration, allein, nicht geteilt – Entscheidung 11). **Zähler: 0.**

**Entwickler-Brief:** Backend zuerst (Modell, Kette neu falten, Vertrag additiv, Controller nach dem Owner-Muster
von `Subject`, Fachgrenze an beiden Trägern, achte Facette, Seed), dann der Agent (eine Stelle), dann der Client,
dann das Frontend beidseitig (Anlegen, Bearbeiten, Unit, Facette). Die eine Schätzungs-Entscheidung, die offen
war, hat der Nutzer am 2026-08-13 getroffen (Cascade am Fach). Die Seed-Liste aus Entscheidung 6 wird aus den
Lehrwerken vervollständigt und im Verlauf genannt.

### Iteration

- **Backend:** drei Entitäten, sechs G2-Zeilen, Migration neu gefaltet (Snapshot-Diff nur die Neuheiten), Controller
  mit idempotentem POST und fail-closed Seed-Themen, Zuweisung mit Fachgrenze an Übung und Unit, Filter, Seed (24 + 22
  Themen). Agent und Client nachgezogen. `GrammatikThemenTests` + Agent-Fall; der PATCH-Semantik-Wächter verlangte den
  Rundlauf für `UpdateGrammarTopicDto`, der Abdeckungs-Wächter 275. Suite **960/960 `exit=0`**.
- **Frontend:** Picker mit Anlegen nebenbei, Vorbelegung als reine Funktion (fünf Fälle), Rundlauf im
  Bearbeiten-Dialog mit Test, Unit-Formular, Facette mit sichtbarer Beschriftung, Feldhilfe, Fach-Löschdialog.
- **Von der E2E gefunden, vor dem Review:** die Planbau-Seite kannte die achte Facette in ihrer Kriterienliste nicht
  und lief nicht neu – `grammatik-themen.spec.ts` fiel am Filter (erwartet 0 Treffer „Ohne Thema“, gemessen 1),
  nach dem Nachtrag **1 passed**. Vitest **460/460**, Build grün.

### Review (nacheinander je Bereich)

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | Unit-Zweig der 409-Sperre ohne Probe | Fall im Pflege-Test; Mutante „zweiter `\|\|`-Zweig weg“ → **1 failed** | ja (Test) |
| – | `pugling-reviewer` 🟡 | Summary von `SeedFrench` verrutscht | zurückgeschoben | nein (Kommentar) |
| – | `pugling-reviewer` 🟢 | Fehltext bei fachloser Reihe; schwache Id-Zusicherung; doppelte Fach-Abfrage | wahre Begründung an beiden Trägern; `detail` begrenzt gelesen; Abfrage bleibt | nein (Freigabe 10) |
| 2 | `frontend-reviewer` 🟡 | Chip hieß bis zum Nachladen „Thema #id“, auch im Knopfnamen | angelegte Themen lokal zusammengeführt | ja (Code) |
| 3 | `frontend-reviewer` 🟡 | Listen-Ausfall stumm, ohne Weg zurück | `ListenAusfall` mit Wiederholknopf | ja (Code) |
| 4 | `frontend-reviewer` 🟡 | Assistent ohne die Facette (AK 7 formal erfüllt, Story-Intention nicht) | Facette in `WizardFilter`/Assistent, Test „acht Kriterien“ | ja (Code+Test) |
| – | `frontend-reviewer` 🟢 | Unit-Rückweg ungetestet; `.first()` und ungenaue Labels in der Spec; Leisten-Label ohne `htmlFor` | Komponententest; Spec gehärtet; Leiste bleibt | nein (Freigabe 10) |

**Zähler: 4.** Danach Backend **960/960 `exit=0`**, Vitest **462/462**, Build grün, die vier berührten Specs **8 passed**;
Gesamtsuite nach den Behebungen erneut **97 passed**.

### Rollengang / Abnahme

Kein Browser; `grammatik-themen.spec.ts` ist der Rollengang aus Creator- und Vater-Sitz (Thema anlegen, zweimal
zuweisen, zurücklesen, über die Facette finden – Reihe A Klasse 5, Reihe B Klasse 7, dritte Übung bleibt draußen,
„(2 Treffer)“ vom Server). *Creator:* „Ich tippe das Thema einmal ein, danach steht es in der Liste – und der
Dialog zeigt es mir mit Namen wieder.“ *Vater:* „Ich wähle ‚Present perfect‘ und sehe beide Übungen, egal aus
welchem Buch.“ *Sohn:* unberührt (Entscheidung 9), Zeuge über die Suiten. **B-155 `abgenommen`.**

### Retrospektive

**Nachschau:** Teil 3 (`12eab76`: B-287, B-288, B-289) von einem frischen Prüfer an einem `git archive` geprüft,
alle Mutanten im Export nachgefahren (2 failed / 1 failed / 1 failed an den richtigen Zeilen), Begründungen gegen
die Controller nachgerechnet. **Kein Befund.** `nachgeschaut` an allen drei gesetzt.

**Was die eigenen Tore durchgelassen haben:** (1) Die Planbau-Seite kannte die achte Facette nicht – ihre
Kriterienliste ist handgepflegt, und ihr eigener Kommentar sagt voraus, dass genau das achte Feld dort vergessen
wird. Gefunden hat es die E2E-Spec, kein Tor: die Liste und `ExerciseFilter` sind zwei Quellen für dieselben
Felder. (2) Der Assistent führt eine dritte Filterquelle (`WizardFilter`); AK 7 nannte nur die Leiste, und die
Schätzung hat den Zwilling nicht gesehen. Beides ist dieselbe Klasse: **eine Facette, drei Orte**.

**Vorschlag, nicht gelandet (Freigabe 3):** **(I)** ein Vitest-Tor, das `ExerciseFilter` (Leiste), die
Kriterienliste in `PlanPositions` und `WizardFilter` auf dieselbe Schlüsselmenge prüft – oder, besser, die
Kriterienliste aus `ExerciseFilter` ableiten, damit es nur noch zwei Quellen gibt. Kein Tor liest heute, ob eine
Suchfacette alle ihre Verbraucher erreicht.

**Commit:** `1c1fe62` (nicht gepusht).

## Lauf E, Teil 5 — NP-10 Der Agent füllt die Unit-Notizen (B-22), Zähler 0

**Sprint-Ziel (aus dem Paket):** *Der Creator lässt die Stoffnotizen einer Unit vom Agenten vorschlagen, statt sie
abzutippen.* Umfang: [B-22](backlog/B-22-unit-stoffnotizen-llm.md) (`geschaetzt`, Freigabe 8, S–M, Migration nur noch
für `Grammar`; nicht im selben Sprint wie NP-9 – darum nach dessen Commit). **Zähler: 0.**

**Entwickler-Brief:** Schema zuerst (`Grammar` als Freitext, Kette falten), dann ein eigener Baustein im Agenten
neben den Strategien (kein Selbsttest, drei Textfelder), Verb `fill`, Tests mit dem Fake-Modell, Doku. Die
Entscheidungen 1–6 tragen; die Nachprüfung vom 2026-10-23 streicht `Topics` aus der Migration.

### Iteration

- Schema: Suffix „Grammar“ → 2000; Snapshot-Diff **eine Zeile**. Agent: `UnitNotesFiller` + Verb `fill`; Tests mit
  drei Mutanten je **1 failed** (Behalten-Zweig, DryRun-Abzweig, Suffix → 2000 vs. 200). Filterlauf **17/17**.

### Review und Rollengang

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | Rollengang (live) | Erster Lauf gegen die geseedete Reihe: eine Minute Modellzeit, dann 403 `not_owner` | Eigentum vor dem Modellaufruf prüfen; Mutante → **1 failed**; live 4 s, Exit 2 | ja (Code+Test) |
| 2 | `pugling-reviewer` 🟡 | Fehlende Quelldatei stürzt ab statt Exit 2 | `IOException` → Fehlbedienung; Mutante → **1 failed** | ja (Code+Test) |
| 3 | `pugling-reviewer` 🟡 | „Entwurf für kein Feld“ ungetestet | Fall mit leerem Entwurf; Mutante → **1 failed** | ja (Test) |
| – | `pugling-reviewer` 🟢 | Kommentar-Grenzen, Suffix-Doku, Wächter-Hinweis, AK 6 | berichtigt | nein |

**Zähler: 3.** Suite **968/968 `exit=0`**. **Rollengang live** (Creator-Sitz mit Ollama `qwen2.5:14b-instruct` gegen eine Wegwerf-API):
Trockenlauf aus der Quelle in 70 s, Schreiben auf eigener Reihe mit handgeschriebener Grammatik (behalten, Rest
geschrieben, per `GET` bestätigt), geseedete Reihe und fehlende Quelle je Exit 2. *Creator:* „Ich gebe das
Inhaltsverzeichnis, er füllt, was leer ist – und lässt meine Notiz stehen.“ Vater/Sohn Zeugen über die Suite.
**B-22 `abgenommen`.**

### Retrospektive

**Nachschau:** Teil 4 (`1c1fe62`, B-155) von einem frischen Prüfer an einem `git archive` geprüft. **Ein
Code-Entgleiten (🔴):** der Idempotenz-Schlüssel der Themen hängt am Fachnamen, Fachnamen sind nicht eindeutig →
[B-290](backlog/B-290-themen-schluessel-haengt-am-fachnamen.md) (`entgangen_bei: B-155`), als nächster Teil
behoben. Eine Produktfrage → [B-291](backlog/B-291-fachwechsel-der-reihe-laesst-themen-stehen.md) (`Frage`, nicht
entschieden). `nachgeschaut` an B-155 gesetzt.

**Was die eigenen Tore durchgelassen haben:** (1) Der Rollengang fand, was kein Test fand: die Reihenfolge
„Modell vor Eigentum“ kostet eine Minute und endet in der 403 – der Test-Harness hat den Vater immer als Besitzer.
(2) B-290: die Entscheidung 4 der Story sagt „fachqualifiziert“, die Umsetzung nahm den Fachnamen, und alle Tests
bauen Fächer mit `UniqueName` – eine Eindeutigkeit, die das Modell ausdrücklich **nicht** garantiert (der
Kommentar am `Subject`-Index sagt es), kam in keinem Testaufbau vor. Beides dieselbe Klasse: der Testaufbau ist
bequemer als die Welt (Besitzer immer der Vater, Fachnamen immer eindeutig).

**Vorschlag, nicht gelandet (Freigabe 3):** **(J)** ein `TestApi`-Helfer, der je Testklasse **zwei** gleichnamige
Fächer verschiedener Creator anlegt, und die Regel im Pflichtschritt von Step 5: *„jede Eindeutigkeit, die ein
Test voraussetzt, gegen die Index-Kommentare des DbContext prüfen“*. Vorschlag (H)/(I) bleiben stehen.

**Commit:** `f033f70` (nicht gepusht).

## Lauf E, Teil 6 — B-290 aus der Nachschau (Themen-Schlüssel je Fach-Id), Zähler 0

**Sprint-Ziel:** *Ein zweiter Creator mit einem gleichnamigen Fach kann sein eigenes „Present perfect“ anlegen, und
ein umbenanntes Fach behält seine Themen.* Umfang: [B-290](backlog/B-290-themen-schluessel-haengt-am-fachnamen.md)
(`Defekt`, Freigabe 1, S, Migration – darum ein eigener Teil nach NP-10). **Zähler: 0.**

**Entwickler-Brief:** Schlüssel `(SubjectId, Slug)` statt Namenspräfix: zusammengesetzter Unique-Index, Suche beim
Anlegen über Fach-Id und Slug, Seed mit reinem Schlüssel, Kette falten; Test mit zwei gleichnamigen Fächern zweier
Creator und mit Umbenennen.

### Iteration

- Modell/Index/Controller/Seed umgestellt, Snapshot-Diff zwei Zeilen (Index `Slug` → `(SubjectId, Slug)`,
  Einzelindex weg). Neuer Fall; Mutante „nur Slug“ → **4 failed** (der neue Fall und drei bestehende, die nun fremde
  Fächer träfen), danach **17/17**. Ohne Zählung: `key={subjectId}` am Picker, `toBeUndefined()` im Assistenten-Test.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| – | `pugling-reviewer` 🟡 | `Create`-Summary beschrieb noch den Namenspräfix; `v1.json` trägt den neuen Vertragstext | Summary + Client-Kommentar nachgezogen, Dokument neu erzeugt | nein (Kommentar/Doku) |

**Zähler: 0.** Suite **969/969 `exit=0`**, Specs `grammatik-themen` + `assistent` **4 passed**. Rollengang als
Integrationstest (zwei Creator mit gleichnamigen Fächern, Umbenennen): *Creator B:* „Mein ‚Present perfect‘ landet in
meinem Englisch, nicht im fremden.“ **B-290 `abgenommen`.**

### Retrospektive

**Nachschau:** Teil 5 (`f033f70`, B-22) von einem frischen Prüfer geprüft (je Behauptung eine Zeile im Verlauf).
**Zwei Entgleiten:** der Agent kürzt zu lange Themen still auf 200 Zeichen, während die API das seit B-130 bewusst
ablehnt → [B-292](backlog/B-292-agent-kuerzt-themen-still.md); `--source-file -` blockiert am Terminal stumm,
Ctrl+C wird geschluckt, die Doku schweigt → [B-293](backlog/B-293-fill-von-stdin-blockiert-stumm.md) (je
`entgangen_bei: B-22`). `nachgeschaut` an B-22 gesetzt.

**Was die eigenen Tore durchgelassen haben:** B-292 ist ein Widerspruch zwischen zwei Stellen, die dieselbe Regel
tragen müssten – der Controller lehnt ab, der Agent kürzt – und die Regel steht als Kommentar am Controller, nicht
als Vertrag. Weder Reviewer noch Tests haben den Agenten gegen den Kommentar des Controllers gelesen.

**Vorschlag, nicht gelandet (Freigabe 3):** **(K)** der Agent prüft die Themen-Länge nicht selbst, sondern lässt
die API ablehnen und zeigt die Ablehnung – eine Regel, ein Ort. Allgemeiner: *ein Client darf die Validierung des
Servers nicht durch eine mildere ersetzen* als Satz in `backend/Pugling.Client/CLAUDE.md` (Prosa, kein Tor).

**Commit:** `6d20ddd` (nicht gepusht).

## Lauf E, Teil 7 — B-292, B-293 aus der Nachschau (Agent hält die Grenzen der API), Zähler 0

**Sprint-Ziel:** *Der Creator sieht, was die Grenzen aus dem Entwurf genommen haben, bevor etwas gespeichert ist –
und ein Thema wird nie still halbiert.* Umfang: [B-292](backlog/B-292-agent-kuerzt-themen-still.md),
[B-293](backlog/B-293-fill-von-stdin-blockiert-stumm.md) (`Defekt`, Freigabe 1, je XS, nur Agent). **Zähler: 0.**

**Entwickler-Brief:** Keine mildere Validierung als der Server: zu lange Themen weglassen und nennen, Freitext am
Wortende kürzen und nennen, beides vor der Schreib-Meldung drucken. Stdin-Verhalten benennen statt schweigen.

### Iteration

- `Clamp` → `(Draft, Dropped, Shortened)`, `FillAsync` druckt beides; Unit per `GetUnitAsync`; Hinweis bei `-` ohne
  Umleitung; Usage/README/Skill. Mutanten „kein Weglassen“ und „Schnitt im Wort“ → je **1 failed**, `Fill_*` **10/10**.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | `Take(50)` ließ das 51. Thema ungenannt verschwinden – dieselbe Klasse eine Stufe weiter | Überzahl weglassen **und nennen**; Test auf 52 Themen; Mutante → **1 failed** | ja (Code+Test) |
| – | `pugling-reviewer` 🟢 | Wortgrenze nur am Leerzeichen; Kommentar versprach Surrogat-Schutz; `global::`; „auf 2000“ | Zeilenumbruch/Tab, Grenze auf Leerraum; Kommentar; `using`; „höchstens“ | nein (Freigabe 10) |

**Zähler: 1.** Suite **970/970 `exit=0`**. Rollengang: der Agent hat keine Oberfläche, die Tests sind der Weg
(Dropped vs. Shortened, 52 Themen, Stdin-Hinweis nur dokumentiert – `Console.IsInputRedirected` ist im Test immer
wahr). **B-292, B-293 `abgenommen`.**

### Retrospektive

**Nachschau:** Teil 6 (`6d20ddd`, B-290) von einem frischen Prüfer an einem `git archive` geprüft. **Kein
Code-Entgleiten**; drei Doku-Entgleiten berichtigt (die residente `backend/Pugling.Api/CLAUDE.md` nannte noch den
Namenspräfix, Klassen-Summary gegen Property-Doku, ein Testname). `nachgeschaut` an B-290 gesetzt.

**Was die eigenen Tore durchgelassen haben:** B-292 hat sich im Review **wiederholt**: nach dem Weglassen der
überlangen Themen stand mit `Take(50)` dieselbe stille Kürzung eine Zeile tiefer – die Regel „jeder Verlust wird
genannt“ war an einer Stelle gelandet, nicht als Regel. Und die residente Anweisungsdatei lief dem Code eine Runde
hinterher, obwohl der Commit Entity, Vertrag, Client und OpenAPI nachgezogen hatte.

**Vorschlag, nicht gelandet (Freigabe 3):** **(L)** ein Wächter im Kopf des Reviews: *„Nennt der Diff eine
Grenze (Zahl, Länge, Anzahl)? Dann jede weitere Grenze derselben Daten im selben Pfad prüfen.“* Und für die
residente Datei: der Nachschau-Prüfer bekommt `backend/Pugling.Api/CLAUDE.md` ausdrücklich in den Scope, wenn der
Commit das Fachmodell ändert (Prosa in der pm-loop-Nachschau, kein Tor).

**Commit:** `4a3fa61` (nicht gepusht).

## Lauf E, Teil 8 — NP-11 Mehrsprachigkeit, Schritt 0 und Teil a (B-85 → B-294, B-295), Zähler 0

**Sprint-Ziel (aus dem Paket):** *Der Sohn kann die Arcade auf Englisch umschalten, mindestens ein Bildschirm ist
vollständig übersetzt, und alle Specs laufen weiter auf Deutsch.* Schritt 0: [B-85](backlog/B-85-i18n-infrastruktur-sohn-arcade-englisch.md)
(L) geteilt in [B-294](backlog/B-294-i18n-infrastruktur-und-startbildschirm.md) (Teil a, M, gebaut) und
[B-295](backlog/B-295-arcade-korpus-auf-englisch.md) (Teil b, M, `geschaetzt`). Umfang dieses Sprints: B-294
(`Wunsch` auf `geschaetzt` aus B-85, Freigabe 8). **Zähler: 0.**

**Entwickler-Brief:** `react-i18next` + `i18next` (Peer-Konflikt seit B-25 erledigt, `npm install` ohne
`--legacy-peer-deps`); Init in `src/lib/i18n.ts` mit Wahl im `localStorage`; Umschalter im HUD; `<html lang>` in
der Arcade-Wurzel; der Startbildschirm vollständig (SohnHome, HUD, Navigation, Missions-Panel, Interessen-Karte);
Deutsch in `playwright.config.ts` gepinnt; eine neue Spec. Nutzerdaten als Platzhalter.

### Iteration

- Bibliothek eingebunden, Init + Ressourcen (Namespace `sohn`, 38 Schlüssel), Umschalter-Chip, `<html lang>`-Effekt
  mit Rückfall, Startbildschirm und seine zwei geteilten Karten über `t()`, Deutsch gepinnt. Vitest `i18n.test.ts`
  (Parität, kein leerer Text, Persistenz, Platzhalter) → **467/467**; `sprache.spec.ts` (die Spec fand zuerst, dass
  der Seed-Sohn heute keine Mission hat – Überschrift jetzt zustandsunabhängig geprüft) + `karten-umdrehen` +
  `interessen` **3 passed**; Build grün.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | Missions-Panel nicht vollständig übersetzt: `PERIOD_LABEL` („Heute/Diese Woche/Ziel“) lag als deutsche Literale neben den Ressourcen (AK 3) | drei Schlüssel `missions.period*`, `t()` im Panel, Spec schließt „Diese Woche“ auf Englisch aus | ja |
| 2 | `frontend-reviewer` 🟡 | Sprach-Chip: sichtbarer Text „EN“ nicht im Vorlesenamen (WCAG 2.5.3) | Name beginnt mit dem sichtbaren Ziel, Specs nachgezogen | ja |
| 3 | `frontend-reviewer` 🟡 | Schlüssel untypisiert – ein Tippfehler in `t("…")` fiele erst im Bild auf | `src/i18n/i18next.d.ts` (`CustomTypeOptions` an `typeof de`), der Build ist das Tor | ja |
| – | `frontend-reviewer` 🟢 | Locator `.first()` hing an der Intro-Karte; Sprache blieb nach rotem Fall auf Englisch; `t` fehlte in Effekt-Deps | `hasText`-Locator, `afterEach`, Deps | nein (Freigabe 10) |

**Zähler: 3.** Build grün, Vitest **467/467**, volle E2E **98 passed** (5,8 min). Rollengang: Sohn maschinell per
`sprache.spec.ts`, Vater/Creator als Regression über die volle Suite (das Vater-Web trägt kein `t()`). Kein
Browser verbunden – ob der Chip sich *richtig anfühlt*, hat niemand gesehen; die englischen Texte liest ein
Mensch gegen (AK 7 → `wartet_auf`). **B-294 `abgenommen`, B-85 `verworfen` (geteilt), B-295 `geschaetzt`.**

### Retrospektive

**Nachschau:** Teil 7 (`4a3fa61`, B-292/B-293) von einem frischen Prüfer am `git archive` geprüft. Code und
Reihenfolge der Grenzen stimmen; **zwei Test-Entgleiten**, je als `Defekt` mit `entgangen_bei: B-292` angelegt:
die Freitext-Zusicherung hat keine untere Grenze (ein Schnitt auf 100 Zeichen bliebe grün) →
[B-296](backlog/B-296-freitext-kuerzung-ohne-untere-grenze-im-test.md); der 404-Pfad der Unit-Suche im `fill`-Verb
ist ungetestet → [B-297](backlog/B-297-404-pfad-des-fill-verbs-ungetestet.md). `nachgeschaut` an B-292 und B-293
gesetzt; vier 🟢 dort notiert.

**Was die eigenen Tore durchgelassen haben:** Der Paritäts-Test vergleicht Ressource mit Ressource – eine Tabelle
deutscher Literale *neben* den Ressourcen (`PERIOD_LABEL`) ist für ihn unsichtbar, und genau die blieb im
„vollständig übersetzten“ Bildschirm stehen. Das ist dieselbe Klasse wie (L) aus Teil 7: die Regel war an den
Stellen gelandet, die ich angefasst hatte, nicht als Regel über die Datei. Fund 3 (typisierte Schlüssel) ist als
Review-Behebung gelandet, kein Retro-Tor – er schließt die Lücke *Verwendung → Ressource*, nicht die Lücke
*Literal neben der Ressource*.

**Vorschlag, nicht gelandet (Freigabe 3):** **(M)** ein Literal-Wächter für B-295: ein Vitest über
`src/sohn/**/*.tsx` (ohne `i18n/`), der JSX-Text und `aria-label`/`title`-Literale mit `[äöüßÄÖÜ]` oder aus einer
kleinen deutschen Stoppwortliste als rot meldet – mit Ausnahmeliste wie bei den `ConventionGuardTests`. Erst
messen (wie viele Treffer heute, B-295 ist der Lauf dafür), dann scharf stellen.

**Commit:** `1ee9bf0` (nicht gepusht).
