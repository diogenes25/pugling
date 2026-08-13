---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachschau-Runde 2026-08-13, Arbeitsvorrat geleert]
---

# PM-Sitzung 2026-08-13 — die Nachschau-Runde

**Was das hier ist und was nicht.** Kein vollständiger `pm-loop`-Durchgang: kein Rollengang aus drei
Rollen-Sitzen, keine neue Prio-Tabelle. Der Tag bestand aus **Verfeinerung und drei Bauten** (B-155 auf
`geschaetzt`, B-157 und B-163 auf `abgenommen`) und danach aus **der Nachschau über den gesamten
Arbeitsvorrat**. Das Protokoll trägt darum nur, was wirklich stattgefunden hat — eine erfundene
Sitzungsstruktur wäre die Sorte Formtreue, die dieser Bereich ausdrücklich nicht will.

## Retrospektive

**Nachgeschaut: 103 von 103 abgenommenen** — der Arbeitsvorrat der Nachschau ist **leer**. Vorher: 95 von
103, acht nie angesehen (B-11, B-13, B-123, B-139, B-154, B-157, B-161, B-163). Alle acht sind an diesem Tag
angesehen worden, jede mit `nachgeschaut: 2026-08-13` und einer Verlaufszeile — **auch die ohne Fund**, denn
sonst zählt der Blick nicht.

**Die Entgleitungs-Zahl steigt von 13 auf 20.** Von den acht angesehenen Stories trug **eine keinen** Fund
(B-163) und **sieben** mindestens einen; bei einer davon (B-11) ist der Fund rein kosmetisch. Neun neue
Stories tragen `entgangen_bei`.

### Was die Zahl sagt — und was sie nicht sagt

Die Regel dieses Bereichs lautet: *steigt sie, ist die Abnahme zu weich* — und die richtige Reaktion sei,
das Rollengang-Tor zu **verwerfen oder umzubauen**, nicht es zu verschärfen. Dieser Sprung trägt diese
Aussage **nicht**, und zwar aus demselben Grund, den der Bereich schon einmal notiert hat („die Zahl gehört
dem Modell nicht allein … zwei Variablen haben sich gleichzeitig geändert"):

**Die Messmethode hat sich geändert.** Frühere Blicke waren Durchgänge über viele Stories in einem Kopf.
Dieser Durchgang hat je Story **einen eigenen, frischen Leser** angesetzt, der den Bau nie gesehen hat, mit
einem ausdrücklichen Auftrag auf die zwei **gemessenen Fehlerfamilien** dieses Repos und der Anweisung, ein
„nichts gefunden" als vollwertiges Ergebnis zu liefern. Wer schärfer hinsieht, findet mehr — das sagt
zunächst etwas über das Hinsehen.

**Was die Zahl damit trotzdem belegt:** Die vorherige Quote von 13 in 95 war **zu optimistisch**. Nicht weil
jemand geschludert hätte, sondern weil ein flüchtigerer Blick als „geprüft, sauber" gezählt wurde. Genau
davor warnt `nachtlauf.md` („eine flüchtige Nachschau ist *schlimmer* als keine — sie vergiftet den Nenner").
Die ehrliche Lesart ist deshalb nicht „die Abnahme ist schlechter geworden", sondern **„wir wissen jetzt
mehr, und der neue Nenner ist der belastbarere"**. Der Vergleichsmaßstab für künftige Sprints ist ab heute
dieser Durchgang, nicht der alte Stand.

### Das Fehlerprofil bleibt dasselbe — mit einem Zusatz

Von den neun Funden fallen sieben in die seit dem 2026-08-05 bekannte Familie *eine Bedingung, die zwei
Situationen zusammenzieht*:

| Fund | Die zwei zusammengezogenen Situationen |
|---|---|
| B-170 | „gehört zum Grundbestand" / „hat gerade jemand selbst dort angelegt" |
| B-172 | „nicht geladen" / „das ist die vollständige Liste" |
| B-169 | „diese Zeilen gelten" / „diese Zeilen sind die des vorigen Filters" |
| B-173 | „es gibt keine Reihe" / „keine passt zum Filter" |
| B-174 | „hat keine Arten" / „das Laden ist gescheitert" |
| B-168 | „Eigentümer ist der Aufrufer" / „Eigentümer ist Adult 1" |
| B-165 | „Datei war gesperrt" / „Inhalt war zerrissen" |

Der **Zusatz** ist die zweite Familie, die diese Woche neu in die `CLAUDE.md` kam: *eine Zusicherung, die
den Ausgangszustand prüft*. Sie hat zwei weitere Fälle geliefert (B-171) — und beide in ihrer tückischsten
Form. Nicht leere Zusicherungen, sondern solche, die **fast** das Richtige prüfen: eine Regex, die eine
Ziffer zu viel zulässt (`\d+` trifft die `0`), und eine Prüfung des Ketten-Endes ohne ihren Anfang. Beide
sind beim Lesen unauffällig, weil Name und Kommentar das Richtige behaupten. Das ist die Beobachtung, die
nach dem Bauen von B-171 in die Begründung der Regel gehört — **nicht** als neue Regel daneben.

### Drei Beobachtungen, die mehr wiegen als die Zahl

1. **Zwei Funde sind erst *nach* der Abnahme falsch geworden.** Der Vorspann-Satz auf `/vater/katalog` war
   zum Zeitpunkt von B-154 nur zu *streng* und damit harmlos; mit B-157 wurde er falsch in der gefährlichen
   Richtung (Versprechen > Server). Und B-157 hat eine Fähigkeit **entfernt** (das `DELETE` einer selbst
   angelegten Art war vorher ein `204`), ohne dass eine der beiden getesteten Hälften das zeigte — nur ihre
   Schnittmenge. **Eine Story kann eine andere kaputtmachen, ohne deren Dateien anzufassen.** Dagegen hilft
   kein Reviewer, der einen Diff liest.
2. **Die Nachschau schloss eine offene Frage einer anderen Story.** Der Blick auf B-157 hat den Namen des
   flackernden Tests aus B-165 eingefangen (`AtomaresSchreiben_KeineLeseFehler`) — ohne den dort geplanten
   `.trx`-Schritt. Und der interessante Teil ist nicht die Parallelität, sondern dass das **Messinstrument
   selbst** die Fehlerfamilie trägt: sein Zähler kann „Sperre" nicht von „zerrissenem Inhalt" unterscheiden,
   und damit nicht sagen, ob ein Umgebungsartefakt flackert oder der von B-57 behobene Fehler zurück ist.
3. **Ein Duplikat wurde vermieden, weil vorher nachgesehen wurde.** Die Alt-Daten-Hälfte des B-154-Fundes
   liegt schon als B-164 vor, mit gemessener Breite (110 `useAsync`-Stellen, 67 mit Abhängigkeiten). Sie
   wurde **nicht** neu angelegt. Der Bereich hätte sonst zwei Fassungen derselben Sache, und die veraltete
   gewinnt.

### Was daraus folgt (Vorschläge, keine Beschlüsse)

- **Kein neues Tor.** Sieben von neun Funden sind dieselbe Familie, und für sie gibt es kein mechanisches
  Tor — „prüft diese Bedingung zwei Dinge?" ist keine Frage, die ein Test stellt. Was hilft, ist die
  Frage im Kopf des Lesers, und die steht schon in den Reviewer-Aufträgen.
- **Aber ein Prüfschritt bei *fremden* Versprechen.** Beobachtung 1 ist die einzige, für die es heute
  nichts gibt: eine Story, die eine Regel verschärft, sollte die Sätze suchen, die die alte Regel
  versprechen. Das ist greppbar (Prosa mit „darfst du", „kannst du") und kostet eine Minute. Ob das eine
  Zeile in einem Reviewer-Auftrag wert ist, entscheidet der Nutzer — **erst messen**, wie oft es greift.
- **Die Nachschau in dieser Tiefe ist teuer** (sechs Leser über acht Stories). Sie hat sich hier gelohnt,
  weil acht Stories aufgestaut waren. Als Dauerbetrieb je Sprint wäre sie unverhältnismäßig; der
  Normalfall bleibt ein Blick je Sprint auf die Arbeit des vorigen.

## Die neun Stories aus dieser Runde

| Id | Titel | Prio | entgangen bei | Stufe |
|---|---|---|---|---|
| [B-168](backlog/B-168-eigentuemer-zusicherung-prueft-eine-konstante.md) | „Anlegen macht den Aufrufer zum Eigentümer" prüft eine Konstante | P1 | B-13 | `ausformuliert` |
| [B-169](backlog/B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md) | Im Ladefenster sind die alten Zeilen anklickbar | P1 | B-161 | `ausformuliert` |
| [B-170](backlog/B-170-selbst-angelegte-art-im-grundbestand-ist-unloeschbar.md) | Selbst angelegte Art im Grundbestands-Fach ist unlöschbar | P1 | B-157 | `ausformuliert` |
| [B-171](backlog/B-171-zwei-zusicherungen-pruefen-den-ausgangszustand.md) | Zwei Zusicherungen prüfen den Ausgangszustand mit | P2 | B-161, B-157 | `ausformuliert` |
| [B-172](backlog/B-172-fachauswahl-loescht-wenn-ihre-optionen-fehlen.md) | Die Fachauswahl löscht, wenn ihre Optionen fehlen | P1 | B-123 | `idee` |
| [B-173](backlog/B-173-speichern-vernichtet-seine-eigene-bestaetigung.md) | Speichern vernichtet seine eigene Bestätigung | P2 | B-123 | `idee` |
| [B-174](backlog/B-174-arten-liste-verschweigt-ihren-fehler.md) | Die Arten-Liste verschweigt ihren Fehler | P2 | B-154 | `idee` |
| [B-175](backlog/B-175-zwei-gerottete-zahlen-in-kommentaren.md) | Zwei gerottete Zahlen in Kommentaren | P3 | B-139 | `ausformuliert` |
| [B-176](backlog/B-176-gruppen-ueberschrift-verliert-die-feld-optik.md) | Gruppen-Überschrift verliert die Feld-Optik | P3 | B-11 | `ausformuliert` |

**Die Stufen sind nicht einheitlich, und das ist Absicht:** `ausformuliert` steht dort, wo ich den Ist-Stand
**selbst am Code gegengeprüft** habe; `idee` mit `unverifiziert: true` dort, wo die Fundstellen benannt sind,
ich die Klickfolge aber nicht gefahren habe. Eine Kette mit vier Gliedern schreibt man nicht ab.

## Nicht aus der Nachschau, aber am selben Tag entstanden

| Id | Titel | Prio | Anlass |
|---|---|---|---|
| [B-165](backlog/B-165-backend-suite-flackert.md) | `AtomaresSchreiben_KeineLeseFehler` fällt unter Volllast | P1 | beim Verifizieren von B-163 |
| [B-166](backlog/B-166-wiki-nennt-die-entfernte-kapitel-route.md) | Wiki nennt die entfernte Kapitel-Route | P2 | beim Nachziehen der Typ-Label-Prosa |
| [B-167](backlog/B-167-katalogliste-zeigt-beim-filterwechsel-alte-treffer.md) | Katalogliste zeigt beim Filterwechsel alte Treffer | P3 | Umfeld-Fund eines Reviewers |
