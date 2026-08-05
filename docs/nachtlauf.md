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
3. Genau EIN Sprint. Die Retrospektive darf ihren Mechanismus nur VORSCHLAGEN, nicht
   landen – ich sehe ihn mir am Morgen an.

Zwei Auflagen, weil ich das am Morgen nachprüfen will und nicht glauben soll:
4. „Kein Befund" gilt nur mit benanntem Prüfpunkt. Jede `nachgeschaut`-Zeile sagt, WAS
   geprüft wurde – nicht „sauber", sondern „Behauptung X nachgerechnet, Zeile A gegen B".
5. Jede rote Probe nennt ihre Zahl (erwartet/gemessen) im `## Verlauf`. „War rot" genügt
   nicht: ein Test, der vorher schon grün war, sieht sonst wie ein behobener Fehler aus.

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
3. **Ein Sprint, Retro schlägt nur vor** — das ist die wichtigste der drei. Step 8 verlangt je Sprint
   einen gelandeten Mechanismus; über mehrere Sprints hinweg schriebe ein unbeaufsichtigter Lauf also
   mehrere neue Dauerregeln, die niemand gesehen hat. Der Budget-Warner meldet das Wachstum zwar, aber erst
   am Morgen. **Preis:** die Nacht endet früher — und die Regel-Einbahnstraße bleibt unter deiner Kontrolle.

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
je Sprint plus „genau ein Sprint" begrenzt die Nacht zusätzlich. Ein Nachtlauf arbeitet das Backlog
**nicht** ab; er arbeitet einen Sprint sauber durch und legt sich dann hin.

## Wo er anhält — und warum das Erfolg ist

- Am Ende des einen Sprints (Freigabe 3).
- An jedem `Wunsch`/`Frage` (Freigabe 1).
- Wenn ein Review einen Defekt **im eigenen Increment** dieses Sprints findet: dann ist die
  Qualitätsschwelle gerutscht, und Weiterlaufen wäre die falsche Reaktion.
- An einer Story, die auf etwas außerhalb wartet (`wartet_auf`) — Gerät, Betreiber-Handgriff, echtes Ohr.

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
  frischen Sitzung ohne jeden Gesprächskontext — der Auftragstext muss dann *allein* tragen. Genau dafür
  ist er oben wörtlich formuliert.

Ungetestet ist bisher **beides**: bis zum 2026-08-05 hat kein Lauf dieser Art stattgefunden. Der erste
gehört entsprechend beobachtet, nicht verschlafen.
