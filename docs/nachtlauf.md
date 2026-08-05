---
tags: [typ/referenz, bereich/pm]
aliases: [Nachtlauf, Unbeaufsichtigter Backlog-Lauf]
---

# Nachtlauf: einen unbeaufsichtigten Backlog-Lauf beauftragen

Die **Regeln** des Verfahrens stehen woanders — die Stufenkette und ihre Eintrittsbedingungen in
[docs/backlog/README.md](backlog/README.md), der Zyklus in `.claude/skills/pm-loop/SKILL.md`. Diese Seite
ist nur die **Betriebsanleitung**: wie ein Lauf gestartet wird, der ohne Rückfragen arbeitet, was er dabei
darf, und was am Morgen davon übrig ist.

Sie existiert, weil das Verfahren an drei Stellen ausdrücklich einen Menschen verlangt. Ohne Vorab-Freigabe
bleibt ein unbeaufsichtigter Lauf an jeder dieser Stellen stehen — richtig, aber nutzlos.

## Der Auftragstext

Wörtlich einsetzbar. Die drei Freigaben sind der eigentliche Inhalt; ohne sie ist es kein Nachtlauf.

```text
Arbeite das Backlog ab, unbeaufsichtigt, nach pm-loop und docs/backlog/README.md.

Freigaben für diesen Lauf:
1. Autonomes Grillen ist freigegeben – aber nur für `art: Defekt` und `art: Aufräumen`.
   Bei jedem `Wunsch` und jeder `Frage` hältst du an und schreibst die Entscheidung, die
   du gebraucht hättest, als offenen Punkt ins Protokoll. Du entscheidest sie nicht.
2. Sind die Reviewer-Agenten nicht erreichbar, führe einen ausdrücklich als solchen
   beschrifteten Selbst-Check und lass die Story auf `in-arbeit` mit `wartet_auf`.
   Stampfe sie nicht auf `abgenommen`.
3. Mehrere Sprints sind erlaubt (nicht mehr nur einer). Ein Sprint bleibt, was pm-loop
   ohnehin definiert – ein roter Faden über bis zu sechs `geschaetzt`e Stories **desselben
   Themengebiets**, kein Zeitfenster: ob alle Stories „in einen Sprintzeitraum passen"
   ist keine Bedingung. Nach JEDEM Sprint läuft der Rollengang (Step 6) und die
   Retrospektive (Step 8) einzeln; die Retro darf ihren Mechanismus in jedem einzelnen
   Sprint der Nacht nur VORSCHLAGEN, nie landen – das gilt durchgehend, nicht nur für den
   letzten Sprint. Findet ein Review einen Defekt im eigenen Increment eines Sprints,
   endet der GESAMTE Lauf dort, nicht nur dieser eine Sprint. Ausgesprochen große Stories
   (faktisch XL) werden im Nachtlauf nicht gebaut – sie werden nach dem bestehenden
   Teilen-Mechanismus (`docs/backlog/README.md` → „Teilen und Zusammenlegen") in kleinere
   Stories zerlegt, und dieses Teilen selbst ist eine zulässige Tätigkeit dieses Laufs.

Zwei Auflagen, weil ich das am Morgen nachprüfen will und nicht glauben soll:
4. „Kein Befund" gilt nur mit benanntem Prüfpunkt. Jede `nachgeschaut`-Zeile sagt, WAS
   geprüft wurde – nicht „sauber", sondern „Behauptung X nachgerechnet, Zeile A gegen B".
5. Jede rote Probe nennt ihre Zahl (erwartet/gemessen) im `## Verlauf`. „War rot" genügt
   nicht: ein Test, der vorher schon grün war, sieht sonst wie ein behobener Fehler aus.

Neu seit der Erprobung vom 2026-08-05:
6. Ist die Chrome-Extension in dieser Sitzung erreichbar (das gilt für einen abends
   angestoßenen Lauf, nicht für einen zeitgesteuerten), zählt ein echter Rollengang im
   Browser als Rollengang – nicht nur ein Ersatzbeleg. Bricht die Verbindung ab oder
   läuft dieser Lauf zeitgesteuert, gilt wieder der dokumentierte Ersatz
   (Integrationstest, eigene E2E-Spec, Live-Probe gegen die laufende API), benannt im
   `## Verlauf`.
7. Für Frontend-/UI-Stories, die das sichtbare UI verändern, darf zusätzlich zum
   `frontend-reviewer` der Skill `web-design-guidelines` laufen.

Sonst gilt das Verfahren unverändert: Sprint-Ziel aus Rollensicht, rote Probe vor dem Fix,
Rollengang als E2E, Nachschau als erste Pflichthandlung der Retro, Commits selbst setzen,
Push bleibt bei mir.
```

## Warum genau diese drei Freigaben

Jede setzt eine Schutzregel aus, und jede hat ihren Preis:

1. **`art`-Schnitt** — er bleibt bestehen, wird hier nur ausdrücklich bestätigt. `Wunsch` und `Frage`
   sind Produktrichtung; die entscheidet der Agent auch nachts nicht ([README →
   „Der Backlog-Lauf"](backlog/README.md#der-backlog-lauf-dieselbe-freigabe-aber-offen-statt-je-vorhaben)).
   **Preis:** der Lauf liefert weniger, als das Backlog hergibt.
2. **Selbst-Check als Ersatz** — sonst hält der Lauf bei der ersten Abnahme an, sobald ein Reviewer nicht
   antwortet (am 2026-08-05 sechs Versuche, sechs serverseitige `529`). **Preis:** der schwächere Beleg,
   und die Story bleibt `in-arbeit` — fertig gebaut, aber nicht abgenommen. Das ist gewollt: die Abnahme
   ist deine Handlung.
3. **Retro schlägt nur vor, in jedem Sprint** — das ist die wichtigste der drei, und sie ist der Grund,
   warum mehrere Sprints pro Nacht überhaupt vertretbar sind. Step 8 verlangt je Sprint einen gelandeten
   Mechanismus; würde die Retro ihn landen, schriebe ein unbeaufsichtigter Lauf über mehrere Sprints hinweg
   mehrere neue Dauerregeln, die niemand gesehen hat. Die Freigabe setzt genau das außer Kraft, und zwar
   für **jeden** Sprint der Nacht, nicht nur den letzten — so bleibt „mehr als ein Sprint" trotzdem sicher.
   **Preis:** ein Vorschlags-Stapel statt einer gelandeten Regel; was davon gilt, entscheidest du am
   Morgen. Ein Defekt im eigenen Increment eines Sprints bleibt der einzige Grund, der den **gesamten**
   Lauf beendet statt nur den einen Sprint — dort ist die Qualitätsschwelle gerutscht, und das betrifft
   die Nacht als Ganzes, nicht nur ein Thema.

## Wenn der Lauf mit Sonnet fährt

Das ist der Normalfall, und er ist **gemessen**, nicht geschätzt: die vierzehn Abnahmen der autonomen
Runde vom 2026-08-05 stammen **ausnahmslos von Sonnet 5** (Commit-Trailer `Co-Authored-By`, siehe unten).
Die Nachschau über alle vierzehn hat **vier Stories mit einer Entgleitung** gefunden, fünf insgesamt
([Protokoll](pm-sitzung-2026-08-05.md), Index-Abschnitt „Nach der Abnahme entgangen").

**Die Zahl gehört dem Modell nicht allein.** In derselben Runde fehlte auch der Rollengang — zwei
Variablen haben sich gleichzeitig geändert. Wer die 29 % dem Modell zuschreibt, rechnet unsauber.

Aussagekräftiger als die Quote ist das **Fehlerprofil**: alle fünf Entgleitungen sind eine Familie —
*eine Bedingung, die zwei Situationen zusammenzieht.* `Testable` als Typ- statt Tages-Aussage (B-114),
„leer" für „nichts gekauft" *und* „Laden gescheitert" (B-111), `loading && data` für „Neuladen" *und*
„andere Abfrage" (B-116), ein veränderlicher Sortierschlüssel unter Offset-Paging (B-110). Das ist kein
schlampiges Ausführen, sondern ein **enger Betrachtungsraum**: jeweils korrekt für den bedachten Fall.

Die Gegenprobe stützt das, und sie gehört dazu: B-72 ist defensiv gebaut (`Array.isArray`-Verengung,
symmetrischer Rundlauf), B-84 zieht eine Assertion ein, die die falsche Behauptung nicht zurückkehren
lässt, B-57 behebt ein Test-Rennen mit begründeter eigener Factory, und B-62s Kommentar-Behauptung war
nachrechenbar wahr. Die Arbeit war sorgfältig — nur schmal.

**Warum die Nacht diesem Profil entgegenkommt:** die erreichbaren Stories sind ausnahmslos `Aufräumen`
(kein einziger `Defekt` steht auf `geschaetzt`). Das ist die Kategorie mit dem billigsten
Korrektheitsmaßstab — *kein Verhalten ändert sich, alles bleibt so grün wie vorher* — und dort trägt die
Maschine die Prüfung: `TreatWarningsAsErrors`, die Guard-Tests, das Test-Tor, Markdown-Lint.

**Warum die Auflagen 4 und 5 im Auftrag stehen:** sie sichern die zwei Schritte, an denen ein schmaler
Betrachtungsraum am teuersten ist. Eine flüchtige Nachschau ist *schlimmer* als keine — sie vergiftet den
Nenner der Wirkungs-Zahl, und ein falsches „geprüft, sauber" ist nicht mehr von „nie angesehen" zu
unterscheiden. Und eine rote Probe ohne Zahl belegt nichts: am 2026-08-05 war einer von drei neuen Tests
schon **vor** dem Fix grün. Beide Auflagen sind übrigens für **jedes** Modell richtig — der Fehler mit dem
vorher-grünen Test und eine unbelegte Behauptung in einer Retro sind an diesem Tag *Opus* passiert, nicht
Sonnet. Sie stehen hier, weil sie beim schmaleren Betrachtungsraum wahrscheinlicher greifen.

## Was realistisch passiert

Stand 2026-08-05, 63 offene Stories:

| | Anzahl | Folge für den Lauf |
| --- | --- | --- |
| `Wunsch` + `Frage` | **33** | **gesperrt** — der Lauf hält an und notiert die Frage |
| `Aufräumen`, `geschaetzt` | 15 | baubar (B-07 fällt aus: wartet auf Azure) |
| `Defekt`, `in-arbeit` | 4 | fertig, warten auf Reviewer |
| `Defekt`/`Aufräumen`, noch nicht geschätzt | 9 | erst grillen/schätzen, dann baubar |

Erwartung also: **etwa vierzehn Stories** sind überhaupt erreichbar, und die Obergrenze von sechs Stories
je Sprint begrenzt jeden einzelnen Sprint zusätzlich. Seit mehrere Sprints pro Nacht erlaubt sind (siehe
Freigabe 3), arbeitet ein Nachtlauf mehrere thematisch verwandte Sprints nacheinander durch — aber immer
noch **nicht** das ganze Backlog: `Wunsch`/`Frage` bleiben gesperrt, jeder Sprint braucht seinen eigenen
Rollengang, und der Lauf legt sich hin, sobald kein baubares Thema mehr übrig ist oder eine der
Halt-Bedingungen greift (siehe unten).

## Wo er anhält — und warum das Erfolg ist

- Wenn kein weiteres Thema mehr baubar ist — alles Übrige ist `Wunsch`/`Frage` (gesperrt, Freigabe 1),
  wartet extern (`wartet_auf`) oder ist zu groß und noch nicht geteilt (Freigabe 3).
- An jedem `Wunsch`/`Frage` (Freigabe 1) — notiert, nicht entschieden; der Lauf macht mit dem nächsten
  baubaren Thema weiter, statt dort ganz zu enden.
- Wenn ein Review einen Defekt **im eigenen Increment** eines Sprints findet (Freigabe 3): dann ist die
  Qualitätsschwelle gerutscht, und Weiterlaufen wäre die falsche Reaktion — das beendet die **ganze**
  Nacht, nicht nur den einen Sprint.
- An einer Story, die auf etwas außerhalb wartet (`wartet_auf`) — Gerät, Betreiber-Handgriff, echtes Ohr.

## Wenn kein Browser da ist

Das galt uneingeschränkt für die Erprobung vom 2026-08-05, gilt aber **nicht mehr pauschal**: die
Chrome-Extension bindet sich an eine laufende Sitzung, nicht an eine zusehende Person. Ein **abends
angestoßener** Lauf (du tippst den Auftrag und gehst) läuft in derselben Sitzung weiter — bleibt Chrome
verbunden, ist ein echter Browser-Rollengang möglich, obwohl niemand zusieht (Freigabe 6). Ein
**zeitgesteuerter** Lauf startet dagegen in einer frischen Sitzung ohne bestehende Browser-Verbindung —
dort gilt weiterhin, was folgt.

Ist kein Browser erreichbar (zeitgesteuert, oder die Verbindung bricht ab), hat der Lauf **strukturell
keinen Browser-Rollengang**. Gemessen am Nachtlauf vom 2026-08-05 (Sprint 2, B-113/B-114/B-115/B-116,
[Protokoll](pm-sitzung-2026-08-05.md)): jede der vier Stories musste den Rollengang mit etwas anderem
ersetzen — Integrationstests, ein Komponententest, oder ein Live-Aufruf gegen die laufende API — und jede
Story trägt die Ersatz-Zeile ausdrücklich im `## Verlauf`, statt den fehlenden Rollengang zu verschweigen
(pm-loop Step 6 verlangt genau das).

**Die einzige Beleg-Art, die diese Lücke wirklich schließt, ist eine E2E-Spec.** Sie fährt den echten
Browser gegen den echten Server — das *ist* der Rollengang, nur wiederholbar statt einmalig (dieselbe
Begründung wie bei B-110s `e2e/shop-verlauf.spec.ts`). Alles andere (Integrationstest, Live-`curl`,
Komponententest) ist ein schwächerer, aber ehrlicher Ersatz für exakt das, was ohne Browser nicht geht:
das *Aussehen* zu beurteilen. Ein Nachtlauf, der eine sinnlich-visuelle Frage klärt (B-115: „verschiebt
sich ein Zeichen im Kästchen sichtbar?"), kann das nachts grundsätzlich nicht — das ist kein Fehler des
Laufs, sondern der Grund, warum Step 6 den dritten Ausgang „delivered, pending device/human check" kennt.

**Falls du selbst live gegen die API prüfst** (Demo-Kind/Demo-Vater eignen sich, `docs/backlog/README.md`
kennt die PINs nicht extra, sie stehen im Seed): Testzeilen immer über den **echten Endpunkt** anlegen
(Kauf, Aktivierungsanfrage, …), nie per rohem SQL-`INSERT` in `pugling.db`. Grund, gemessen am 2026-08-05:
eine roh eingefügte `ShopPurchase`-Zeile ließ sich nicht stornieren (`409 concurrency_conflict`) — kein
Produktdefekt, sondern ein `ConcurrencyStamp`, den nur der EF-Pfad korrekt setzt. Der Umweg über die
echte API kostet ein paar Zeilen mehr, spart aber die Verwechslung zwischen einem echten Befund und einem
Artefakt des eigenen Kurzschritts.

## Am Morgen: fünf Zeilen prüfen

1. `bash .claude/scripts/backlog-index.sh` läuft ~4–5 min — danach im Index: **Offen**, **Nachgeschaut
   X von Y**, **Nach der Abnahme entgangen**, **Wartet auf Zutun von außen**. Steigt die Entgleitungs-Zahl,
   ist die Abnahme zu weich geworden.
2. `## Retrospektive` im Protokoll `docs/pm-sitzung-<Datum>.md`: **erste Zeile ist die Nachschau.** Fehlt
   sie, ist der Sprint nicht geschlossen.
3. Der vorgeschlagene Mechanismus aus der Retro — landen oder verwerfen. Das ist deine Entscheidung.
4. `git log --oneline` gegen den Abendstand; **nichts ist gepusht**, das bleibt bei dir. Welches Modell
   gearbeitet hat, steht dabei von selbst im Commit (`Co-Authored-By: Claude …`) — filterbar mit
   `git log --format='%h %b' | grep -o "Claude [A-Za-z]* [0-9.]*"`, ohne dass jemand es mitschreiben muss.
5. Die notierten `Wunsch`/`Frage`-Punkte: das ist die Tagesordnung fürs Grillen.

## Zeitgesteuert oder abends angestoßen?

Beides geht; die Wahl ist eine Abwägung, keine technische Frage.

- **Abends angestoßen** (du tippst den Auftragstext und gehst): der Lauf beginnt mit deinem Kontext, du
  siehst die ersten Schritte noch. Braucht dich für zwei Minuten.
- **Zeitgesteuert** (Claude Code kann Aufträge planen): braucht dich gar nicht, startet aber in einer
  frischen Sitzung ohne jeden Gesprächskontext und **ohne** Browser-Verbindung (siehe oben) — der
  Auftragstext muss dann *allein* tragen. Genau dafür ist er oben wörtlich formuliert.

**Getestet ist bisher nur „abends angestoßen"**: der Lauf vom 2026-08-05 (Sprint 1+2, mehrere
Nachträge, [Protokoll](pm-sitzung-2026-08-05.md)) war abends angestoßen, mit genau einem Sprint und ohne
Browser. „Zeitgesteuert" und „mehrere Sprints pro Nacht" sind mit dieser Fassung beide neu und noch
ungetestet — der erste Lauf mit ihnen gehört entsprechend beobachtet, nicht verschlafen.

## Design-Skill im Rollengang

Neu seit der Erprobung: der Skill `web-design-guidelines` (Review von UI-Code gegen die Web Interface
Guidelines) steht zur Verfügung. Er ersetzt **nicht** den `frontend-reviewer` (der prüft Korrektheit und
Konventionstreue) und **nicht** den Rollengang (der prüft, ob eine Rolle die Änderung tatsächlich nutzen
kann) — er ist ein dritter, zusätzlicher Blick auf sichtbare UI-Änderungen, den Freigabe 7 für
Frontend-/UI-Stories freigibt. Läuft er, gehört das Ergebnis wie jeder andere Prüfschritt ins
`## Verlauf` der Story.
