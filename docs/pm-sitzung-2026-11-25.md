---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Platzhalter der Wortliste – B-626 zu Ende bauen

**Datum:** 2026-11-25  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Aus dem Rollengang an der echten DB das Stück finden, das die Söhne heute am meisten kostet, und es fertig bauen.

Ohne Zielvorgabe aufgerufen (`/pm-loop`). Kein neuer Befund unter `docs/anmerkungen/` (letzter Export 2026-08-14, beide
Einträge erledigt). Rollengang an einer Kopie der echten `pugling.db` (`kopie-server.sh`, :5201, Release, Seed aus);
:5200 (Heimnetz) unberührt.

## Runde 1 — Vorstellung & Feedback

### Feedback Vater (O-Ton)

- **Gefällt:** „Die Tagesübersicht zeigt mir alle drei Kinder auf einen Blick – Ziele, Münzen, geübt ja/nein.“
- **Stört/fehlt:**
  - „Friedrich hat heute **zehn** Ziele, acht davon täglich – Unité 5 Découvertes und Atelier A (165/166) mit 20 und 58
    fälligen Karten, dazu vier Grammatik-Positionen (214–217). Der Vokabeltest war gestern. Das sollte seit gestern Abend
    Kür sein.“ (Beobachtet an der Kopie: Paket B, `scripts/klausurplan-b-2026-11-24.ps1`, ist in der echten DB **nicht
    gelaufen** – Theodors Part C/Generalprobe 209 sind ebenso noch Pflicht.)
  - „Theodor hat mir seit dem 19.11. **sechs** Rückmeldungen geschickt, alle offen. Drei davon sind dieselbe Sache: er tippt
    ‚smt.‘/‚smb‘, wo das Buch ‚sth.‘/‚sb.‘ druckt.“
  - „Friedrichs Malus für Montag 23.11. kam erst heute Morgen (zusammen mit Dienstag) – er war gestern in der App, hat einen
    Skin gekauft. Abgerechnet wird nur bei Anmeldung und Shop-Kauf; ein Skin-Kauf zählt nicht.“ (Nachgelesen:
    `SettleClosedPeriodsAsync` läuft an Kind-Login und Shop-Kauf, so dokumentiert – kein Defekt, aber der Sohn sieht seine
    Strafe zwei Tage zu spät.)
- **Top-3:** (1) Paket B einspielen, (2) Theodors Rückmeldungen beantworten, (3) die „zu streng“-Fälle beheben.

### Feedback Sohn (O-Ton, aus Theodors echten Rückmeldungen)

- **Nervt:** „‚turn to smb‘ ist falsch? Ich weiß doch, was es heißt!“ (Rückmeldung 6, 24.11.) – „‚remote controll‘ – ein L
  zu viel, ganze Karte weg.“ (Rückmeldung 5, das ist [B-784](backlog/B-784-tippfehler-im-ueben-bei-theodor.md)) –
  „‚towards the station/Mr bell‘ ist falsch?“ (Rückmeldung 4).
- **Friedrich (aus seinem Tag):** zehn offene Ziele um sieben Uhr, 138 fällige Karten – nach dem gestrigen Test.
- **Top-3:** (1) weniger Pflicht nach dem Test, (2) Kürzel/Platzhalter nicht als Fehler, (3) Tippfehler-Toleranz.

### Feedback Creator (O-Ton)

- **Baut sich gut:** Katalog mit 147 Übungen, Units und Reihen sauber geordnet; keine Reibung im Lesen.
- **Fehlt:** „Der KI-Creator weiß nicht, dass ‚sth.‘ kein Teil der Lösung ist – ein Lückentext mit ‚[point sth. out]‘
  würde das Kind ‚sth.‘ tippen lassen.“ (B-626 Entscheidung 6, ungebaut.)
- **Top-3:** (1) Platzhalter-Regel im Agenten.

## PM-Synthese & Priorisierung (→ Entwickler)

Beobachtung: Der stärkste Befund kommt von einem echten Kind, nicht aus meiner Begehung – Theodors Rückmeldungen. Drei von
sechs betreffen Platzhalter der Wortliste, eine einen mehrwortigen Schrägstrich. Genau das ist
[B-626](backlog/B-626-antwort-verlangt-mehr-als-die-vokabel.md), mit dem Vater am 17.11. im Dialog gegrillt und seit 22.11.
**zur Hälfte gebaut** (`1171f1f4`: Vergleich und Kästchen-Maske) – ohne Pfad-Tests, Agentenregel, Mutanten, Review,
Rollengang und Datenkorrektur, die Story stand weiter auf `geschaetzt`. Halbfertiges Liegenlassen ist der teuerste Zustand:
der Code wirkt schon an der echten DB, aber nichts belegt ihn.

| Prio | Punkt | Größe | Wo | Weg |
| --- | --- | --- | --- | --- |
| P0 | Paket B einspielen (Friedrich 8 → 4 Tagespflichten, Theodor Part C → Kür) | — | Daten | **Vater** startet das Skript (tippt die PIN) – keine Code-Arbeit |
| P1 | B-626 fertig bauen (AK 5–7, Review, Rollengang) | M | backend | dieser Sprint |
| P1 | Theodors Kürzel „smb/smt“ | ? | backend | neu [B-990](backlog/B-990-eigene-platzhalter-kuerzel-des-kindes.md), `idee`, Dialog |
| P2 | Tippfehler-Toleranz Theodor | ? | backend | [B-784](backlog/B-784-tippfehler-im-ueben-bei-theodor.md), `idee` |
| P3 | Malus erst bei Login/Shop sichtbar | — | — | dokumentiertes Verhalten, keine Story |

**Roter Faden:** Ein Fehler zählt nur, wenn das Kind das Wort nicht weiß.

## Sprint 162 — Ziel & Umfang

**Sprint-Ziel:** Theodor bekommt zu jeder Vokabel mit Platzhalter („(to) turn to sb.“) den Kern („turn to“) als richtig
gewertet – im Üben, im Abschlusstest, im Bonus-Training und in der Vorschau des Vaters gleich –, die Kästchen lassen ihn nur
den Kern tippen, und der KI-Creator erzeugt keine Lösung mit Platzhalter.
**Umfang:** B-626 (M, backend).
**Entwickler-Brief:** Quelle der Wahrheit ist die Platzhalter-Liste – bisher privat in `AnswerVariants`, jetzt
`Pugling.Contracts/Common/WordListPlaceholders` (Präzedenz `VocabKey`: Server und Agent brauchen dieselbe Liste, eine
Kopie würde driften). Senke ist der `AnswerGrader` über die Markierung `MeaningsSeparable` des Resolvers – alle vier Pfade
laufen dort hindurch, darum je Pfad ein Integrationstest statt je Pfad eine eigene Regel. Agent: Regel 9 im Systemprompt
(Anfrage) plus `DraftRules.AnswerWithoutPlaceholder` an Lücken (Antwort **und** Alternativen) und Grammatik-Lösungen
(Garantie); `CoversRequiredWords` lässt den Kern eines Pflichtworts gelten, sonst widersprächen sich beide Regeln in jeder
Reparatur-Runde. Keine Migration, kein Vertragsbruch. Datenkorrektur AK 7 an der echten DB nur mit dem Vater.

## Iteration 162 — umgesetzt

**Backend (API-First):** Platzhalter-Liste als `Pugling.Contracts/Common/WordListPlaceholders` (`IsPlaceholder`,
`ContainsPlaceholder`, `Core`); `AnswerVariants` nutzt sie für Vergleich und Kästchen-Maske (der Bau von `1171f1f4` bleibt
sonst unverändert). Neue Pfad-Tests `PlatzhalterPfadTests` (Üben, Abschlusstest, Vorschau, Bonus-Training, Kästchen-Karte).

**KI-Creator:** Regel 9 im Systemprompt; `DraftRules.AnswerWithoutPlaceholder` an Lücken (Antwort und Alternativen),
Grammatik-Lösungen und Übersetzungen (Ziel und Alternativen). `CoversRequiredWords` bekommt einen Pflicht-Schalter
`slotFree`: nur wo Antworten platzhalterfrei sein müssen (Lückentext, Grammatik), deckt auch der Kern eines Pflichtworts –
Klammern und Platzhalter weg, „(to) point sth. out (to sb.)“ → „point out“. Vokabel, Übersetzungs-Ausgangssatz und
Schultest-Hinweis bleiben streng.

**Frontend:** keine Änderung (die Sohn-App zeigt feste Maskenzeichen schon, `LetterBoxes.tsx` `lbox-fixed`).

**Review (`pugling-reviewer`):** kein 🔴. Drei 🟡, alle im Agenten und alle behoben mit Test: (1) Pflichtwort mit Klammern
und Platzhalter blieb eine unlösbare Reparatur-Schleife – der Kern ließ die Klammern stehen; (2) die Kern-Deckung lockerte
auch die Vokabel-Strategie, deren Front an den Store-Eintrag des schwachen Worts gebunden ist; (3) Übersetzungen prüften
ihre Lösungen nicht. Von den 🟢 behoben: Platzhalter an einer Klammer („(to sb.)“) zählt für die Agentenprüfung. Bewusst
nicht: Salat (B-992), Lücke mit Store-Bezug ohne `Form` wertet das rohe Store-Wort (Story-Verlauf), Wrapper in
`AnswerVariants` (kosmetisch). Senke bestätigt: alle getippten Vokabelpfade (auch Lernkarte und Mischrunde) laufen über
`AnswerGrader` → `GradedAnswers`. **Fehlerzähler Sprint 162: 3** (drei 🟡 im Increment).

## Runde 162 — Re-Review / Abnahme

Kopie-Server nach der letzten Backend-Änderung neu gestartet (`kopie-server.sh`, :5201), danach beendet.

- **Sohn – geliefert, wartet auf einen Blick am Gerät:** An Theodors echter Übung 15 (Part C) wertet die Vorschau (sie wertet
  wie sein Test, `PlatzhalterPfadTests` belegt das je Pfad) „turn to“ für „(to) turn to sb.“ und „turn up/down“ für
  „(to) turn sth. up/down“ als richtig; die Kästchen zeigen „(to) ____ __ sb.“. „turn to smb“ bleibt falsch – das ist sein
  eigenes Kürzel, [B-990](backlog/B-990-eigene-platzhalter-kuerzel-des-kindes.md). Offen für einen Menschen: ob „(to)“ und
  „sb.“ auf dem Handy deutlich als fester Text erkennbar sind (Entscheidung 2, Risiko 4: Bildschirmleser liest „s“, „b“).
- **Creator – abgenommen (maschinell):** „Ein Lückentext mit ‚[point sth. out]‘ geht als Verstoß zurück ans Modell, eine
  Übersetzung mit ‚jdm.‘ ebenso; und ein schwaches Wort ‚(to) point sth. out (to sb.)‘ ist mit der Lücke ‚point out‘
  erledigt, statt die Reparatur-Runde endlos laufen zu lassen.“ Gegangen über `CreatorAgentTests` mit `FakeChatClient`;
  ein Lauf gegen Ollama fand nicht statt.
- **Vater – Regressionszeuge:** kein Vater-Pfad im Diff; Suite grün. Seine Bedingung aus Runde 1 ist kein Code: Paket B
  einspielen und die Vokabeln 122/953 berichtigen (AK 7) – beides an der echten DB, beides bei ihm.

**Stand:** B-626 `abgenommen` am selben Tag, nachdem AK 7 an der echten DB lief (`143ff649`) und der Vater die Kästchen als Testjunge am Gerät abgenommen hat („passt so“). Verifikation siehe
Abschluss: .NET 2398/2398 grün (Release, Ausgangsstand 2394), `dotnet format --verify-no-changes` sauber, Mutanten 17/17 rot am benannten Test (Tabelle in B-626), Frontend unverändert (kein Vitest/E2E nötig).

## Retrospektive

**Nachschau:** Sprint 161 (Lauf L) – B-744: die Schreiber der Eingabefelder des Schnappschusses gegrept
(`KlassenarbeitenController.cs:186-189`): Fachwechsel verwirft, Terminwechsel lässt bewusst stehen (AK 5); kein Befund.
B-746: `prognoseAbstand`/`abstandText` gelesen, Vorzeichen und Rundung wie dokumentiert; kein Befund. `nachgeschaut:
2026-11-25` an beiden.

**Was die eigenen Tore durchgelassen haben:** alle drei 🟡 kamen vom Review, keiner von Test oder Mutant – meine Tests
prüften genau die Fälle, die ich gebaut hatte. Zwei davon sind dieselbe Fehlerklasse wie in Step 4 schon benannt: ein
geteilter Helfer (`CoversRequiredWords`) bekam eine Lockerung, und ich habe nur für die Aufrufer entschieden, an die ich
dachte – die Vokabel-Strategie bekam sie ungefragt mit, die Übersetzung blieb ohne die neue Regel. Was es geschlossen hat,
war mechanisch: `slotFree` als Pflichtparameter ohne Vorgabe, der Compiler hielt an allen fünf Aufrufern an. Von außen kam
nichts; der stärkste Befund der Sitzung (Theodors Rückmeldungen) stand seit dem 19.11. unbeantwortet in der DB.

**Der eine Mechanismus:** ein Halbsatz in pm-loop Step 4 an der bestehenden Aufrufer-Regel: eine Lockerung für einige
Aufrufer kommt als Parameter ohne Vorgabe. Kein neues Tor – darum kein Halt.

**Bewusst ungedeckt:** (1) Der Teil-Commit `1171f1f4` vom 22.11. ließ B-626 auf `geschaetzt` ohne Verlaufszeile – kein Tor
prüft, ob ein Commit, der eine Story-Id nennt, in deren Verlauf auftaucht; erst messen, wie oft das vorkommt.
(2) Die Agentenregel sitzt je Strategie, nicht an einer Senke: `ExpectedAnswers` wäre eine, nimmt aber die Vokabel mit,
deren Front ein Platzhalter tragen darf. (3) Offene Kind-Rückmeldungen altern lautlos (6 seit 19.11.) – das ist eine
Produktfrage (B-592-Nähe), keine Prozesslücke.

## Offene Roadmap (priorisiert)

Die Liste, die gilt, ist `docs/backlog/`. Reihenfolge-Begründung für die nächsten Schritte:

1. **Beim Vater, heute:** Paket B einspielen (`scripts/klausurplan-b-2026-11-24.ps1`), danach Theodors offene Rückmeldungen
   beantworten; B-626 AK 7 (Vokabeln 122 „towards the station/Mr Bell“, 953 „(to) pose a risk/a problem“) berichtigen.
2. [B-990](backlog/B-990-eigene-platzhalter-kuerzel-des-kindes.md) im Dialog klären – es ist der Rest von Theodors „zu streng“.
3. [B-784](backlog/B-784-tippfehler-im-ueben-bei-theodor.md) (Tippfehler) und [B-624](backlog/B-624-akzentfehler-nicht-ganzer-fehler.md)
   gehören zum selben roten Faden „ein Fehler zählt nur, wenn das Kind das Wort nicht weiß“.
4. Neu und klein: [B-991](backlog/B-991-besitz-platzhalter-sb-s-jds.md), [B-992](backlog/B-992-buchstabensalat-legt-platzhalter.md).

## Nachtrag — Paket B

Der Befund „Paket B ist in der echten DB nicht gelaufen" (Runde 1) galt für die Kopie von 07:01 Uhr. Das Skript lief am
selben Morgen um 08:29 Uhr (Log 07:28:59–07:29:05 UTC, alle Aufrufe 200); ein zweiter Lauf um 10 Uhr brach an der eigenen
Vorprüfung ab, ohne etwas zu ändern. Lesend in `pugling.db` geprüft: Theodor täglich 99/100/193/195 + Nacharbeit 101,
Friedrich täglich 202/133/134/211 + Generalproben 167/168/218/219. Für Friedrichs Vormittag zählt das nicht als Malus: die
abgeschalteten Positionen trugen keinen (165/166/214–217: 0 Münzen), und eine freigegebene Pflicht verzichtet auf ihre
offenen Perioden (B-659).

## Runde 2 — Vorstellung & Feedback

Zweiter Aufruf von `/pm-loop` am selben Tag, ohne Zielvorgabe. Rollengang an einer frischen Kopie der echten DB
(`kopie-server.sh`, :5201, 09:54); :5200 unberührt. Kein neuer Export unter `docs/anmerkungen/`.

### Feedback Vater (beobachtet, Rückmeldung 8 des Testjungen, 08:25)

- **Stört:** „‚turn sth. up / down‘ hätte auch funktionieren sollen.“ An der Kopie nachgestellt (Vorschau, Übung 150):
  „turn sth. up/down“ richtig, „turn sth. up / down“, „turn sth. up/ down“, „to turn sth. up / down“ falsch, je mit
  „falsch geschrieben“. Umgekehrt trifft es seit B-626 AK 7 Vokabel 122 („towards the station / towards Mr Bell“): der ganze
  Eintrag ohne Leerzeichen getippt ist falsch. 15 Store-Wörter tragen einen Schrägstrich ohne, 88 mit Leerzeichen.
- **Wartet bei ihm:** [B-743](backlog/B-743-notenstufe-anzeige-kind-und-vater.md) (Entscheidung AK 2),
  [B-752](backlog/B-752-vorbereitungs-pflicht-im-fenster.md) (Gerät: wirkt die Last fair?).
- **Top-3:** (1) Leerzeichen am Schrägstrich, (2) B-743 AK 2 entscheiden, (3) Akzent-Milde für Friedrich (B-624).

### Feedback Sohn (aus den offenen Rückmeldungen 2–7, unverändert seit Runde 1)

- Theodors „zu streng“ ist nach B-626 noch zweimal offen: sein Kürzel „smb/smt“
  ([B-990](backlog/B-990-eigene-platzhalter-kuerzel-des-kindes.md)) und „remote controll“
  ([B-784](backlog/B-784-tippfehler-im-ueben-bei-theodor.md)). Friedrich tippt Französisch mit Akzenten – jeder Akzent ein
  ganzer Fehler ([B-624](backlog/B-624-akzentfehler-nicht-ganzer-fehler.md), gegrillt und geschätzt, ungebaut).

### Feedback Creator

- Keine neue Reibung seit Runde 1; Regressionszeuge.

## PM-Synthese & Priorisierung (Runde 2)

Wieder kam der stärkste Befund von außen – vom Vater am Gerät, eine Stunde nach der Abnahme von B-626. Er ist ein Defekt im
Kern des Übens und liegt auf dem roten Faden der Runde 1 („ein Fehler zählt nur, wenn das Kind das Wort nicht weiß“).

| Prio | Punkt | Größe | Wo | Weg |
| --- | --- | --- | --- | --- |
| P1 | Leerzeichen am Schrägstrich | S | beides | neu [B-993](backlog/B-993-leerzeichen-um-den-schraegstrich.md), Defekt, dieser Sprint |
| P2 | Akzentfehler zählt halb | L | beides | [B-624](backlog/B-624-akzentfehler-nicht-ganzer-fehler.md), geschätzt – nächster Sprint |
| P2 | Kürzel des Kindes | ? | backend | [B-990](backlog/B-990-eigene-platzhalter-kuerzel-des-kindes.md), Dialog |
| — | B-743 AK 2, B-752 Gerät | — | — | beim Vater |

**Roter Faden:** Ein Fehler zählt nur, wenn das Kind das Wort nicht weiß.

## Sprint 163 — Ziel & Umfang

**Sprint-Ziel:** Theodor verliert keine Karte mehr am Leerzeichen um einen Schrägstrich – im Üben, im Abschlusstest und in der
Vorschau des Vaters gleich, in beide Richtungen („up / down“ für „up/down“ und umgekehrt).
**Umfang:** B-993 (S, beides).
**Entwickler-Brief:** Senke ist `StageMechanics.Normalize` – jeder Textvergleich läuft dort durch; die Fallliste
`docs/fallliste-antwortvergleich.json` hält ihre zwei Spiegel (Vater-Web `normalizeAnswer`, Lehrwerk-Lader `graded()`) gleich,
darum zuerst die Fälle (rot an allen drei Lesern), dann die drei Stellen. `AnswerVariants` teilt den **rohen** Eintrag am
gespaceten `/` vor `Normalize` und bleibt unberührt. Keine Migration, kein Vertragsbruch.

## Iteration 163 — umgesetzt

**Backend (API-First):** `StageMechanics.Normalize` lässt Leerraum am `/` fallen (nach dem Zusammenziehen, also je Seite ein
Leerzeichen). Die Bedeutungsteilung in `AnswerVariants` läuft vorher auf dem rohen Eintrag und bleibt, wie sie ist. Neue
Pfad-Tests `SchraegstrichPfadTests` (Üben, Abschlusstest, Vorschau).
**Spiegel:** `frontend/src/lib/answerNormalize.ts` und `graded()` im Lehrwerk-Lader, gehalten von fünf neuen Fällen der
Fallliste (vor dem Bau rot an allen drei Lesern).
**Verifikation:** .NET 2412/2412 (Release, Ausgangsstand 2398), Vitest 502/502, `test_laden.py` OK; Mutanten 7/7 rot am
benannten Fall (Tabelle in B-993). **Review (`pugling-reviewer`):** kein 🔴/🟡; zwei 🟢 übernommen (Absichtserklärungen
beschriftet, „turn up / down“ ergänzt). Den einzeiligen Frontend-Spiegel hat derselbe Review gelesen; `frontend-reviewer`
lief nicht, weil keine Oberfläche berührt ist. **Fehlerzähler Sprint 163: 0.**

## Runde 163 — Re-Review / Abnahme

Kopie-Server nach der letzten Code-Änderung neu gestartet (frische Kopie, :5201), danach beendet.

- **Sohn – abgenommen (maschinell an der Kopie):** Als Testjunge auf Position 220 die Karte „etwas lauter/leiser stellen“
  mit „turn sth. up / down“ beantwortet → richtig, kein „falsch geschrieben“. Theodors eigene Rückmeldungen 3 und 7
  („turn smt. up/down“) bleiben falsch – das ist sein Kürzel, [B-990](backlog/B-990-eigene-platzhalter-kuerzel-des-kindes.md).
- **Vater – abgenommen:** „Die Vorschau an Theodors Übung 15 sagt jetzt dasselbe wie mein Testjunge: ‚turn sth. up / down‘
  und ‚turn up / down‘ zählen, ‚turn sth. up down‘ nicht.“ Seine Rückmeldung 8 kann er damit als erledigt beantworten.
- **Creator – Regressionszeuge:** kein Creator-Pfad im Diff außer dem Lehrwerk-Lader, dessen `graded()` jetzt gleich
  vergleicht (`test_laden.py` OK); Suite grün.

**Stand:** B-993 `abgenommen`.

## Retrospektive (Runde 2)

**Nachschau:** Sprint 162 – B-626: ein Befund, B-993 (AK 7, Skript `0d88e81d`, schrieb Vokabel 122 auf „ / “ um; danach war
der ganze Eintrag ohne Leerzeichen falsch). Gelesen: das Skript und die Bedeutungsteilung in `AnswerVariants`, sonst kein
Fund. `nachgeschaut: 2026-11-25` an B-626.

**Was durchkam und woher es kam:** Der Fund kam von außen – vom Vater am Gerät, eine Stunde nach der Abnahme von B-626, über
den Rückmeldeknopf der Karte. Die Datenkorrektur AK 7 wurde an einer Kopie geprobt, aber nur auf „die Hälften zählen“, nicht
auf „die alte Schreibweise zählt weiter“. Bei der eigenen Mutanten-Probe fiel M4 zuerst `abweichend` aus: mein Kommentar
behauptete, die Pfad-Kontrolle fange „jedes Leerzeichen weg“; gemessen fing das nur die Fallliste. Die Probe am richtigen
Ort hat die falsche Behauptung gefunden, bevor sie ins Commit ging.

**Der eine Mechanismus:** keiner, begründet. Die Fallliste ist das Tor für diese Fehlerklasse und hat sofort gegriffen, als der
Fall darin stand; gefehlt hat der Fall, nicht das Tor. Ein Tor „jede Store-Schreibweise wird getippt akzeptiert“ bräuchte die
echte DB und liefe nicht in CI. Kein neues Tor – darum kein Halt.

**Bewusst ungedeckt:** Leerraum an Komma und Bindestrich (nicht beobachtet, B-993 Entscheidung 2); der KI-Creator findet
„up/down“ nicht in einem Lückentext mit „up / down“ (`ClozeMarkup.cs:91`, Erstellen statt Werten).

## Offene Roadmap (Stand Runde 2)

Die Liste, die gilt, ist `docs/backlog/`. Reihenfolge:

1. **Beim Vater:** Entscheidung zu [B-743](backlog/B-743-notenstufe-anzeige-kind-und-vater.md) AK 2; der Blick am Gerät zu
   [B-752](backlog/B-752-vorbereitungs-pflicht-im-fenster.md); Rückmeldung 8 und Theodors offene Rückmeldungen beantworten.
2. [B-624](backlog/B-624-akzentfehler-nicht-ganzer-fehler.md) (L, gegrillt und geschätzt) als nächster Sprint – derselbe rote
   Faden, und Friedrich tippt täglich Französisch. Vor dem Bau die Schätzung vom 17.11. gegen den Code prüfen
   (Migrationskette, Pflicht-Zählung, Risiko 2).
3. [B-990](backlog/B-990-eigene-platzhalter-kuerzel-des-kindes.md) und [B-784](backlog/B-784-tippfehler-im-ueben-bei-theodor.md) im Dialog.
