---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-09-02, Interessen am Katalog]
---

# PM-Sitzung: Interessen werden ein Merkmal des Katalogs (Nachtlauf)

**Datum:** 2026-09-02  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Der Creator kann festhalten, für welche Interessen eine Übung eingekleidet ist — damit ein
zweites Kind mit demselben Profil sie wiederverwendet, statt dass sie beim ersten Kind verborgen bleibt.

## Auftrag & Freigaben

Unbeaufsichtigter Lauf nach [docs/nachtlauf.md](nachtlauf.md), Freigaben 1–8, gefahren nach `pm-loop` und
[docs/backlog/README.md](backlog/README.md). Abends angestoßen, **kein Browser gebunden** — es gibt in
dieser Sitzung keine Chrome-Verbindung, also gilt Freigabe 6 in ihrer Ersatz-Fassung: der Rollengang wird
als E2E-Spec oder als benannte Live-Probe gegen die laufende API erbracht und im `## Verlauf` der Story
ausdrücklich als solcher beschriftet.

## Anmerkungen des Menschen (Step 2, vor jedem eigenen Urteil)

[docs/anmerkungen/aktuell.md](anmerkungen/aktuell.md) trägt zwei Einträge, **beide auf `erledigt`**
(#1 Fach-Filter der Lehrwerk-Suche, #2 Band/Buchtyp gehört ans Lehrwerk). Kein offener Befund aus dem
echten Betrieb — es gibt also nichts, was mein eigenes Urteil überstimmen müsste.

## Runde 1 — Rollengang am laufenden Produkt (Step 2)

Gefahren gegen eine frisch geseedete Instanz auf `localhost:5200` (zum Start siehe den Befund unten).
Alles hier Notierte ist **beobachtet**, nicht simuliert; die Antworten stehen im Klartext daneben.

### Feedback Creator (O-Ton)

> „Die Interessen-Taxonomie ist da und sie ist gut. `creator/interest-tags` liefert mir `brawl-stars`,
> `pokemon`, `fussball`, `basketball` — mit Slug, Label, Facette, Synonymen. Ich sehe sogar, wie viele
> Kinder und wie viele Bilder an einem Stichwort hängen (`childCount`, `mediaCount`).
>
> Und dann lese ich eine Übung, und da ist **nichts**. Sie trägt `gradeMin: 5`, `gradeMax: 6`,
> `schoolTypes: "Realschule, Gymnasium"`, `source: "Green Line 1, Unit 1"`, `categoryId` — jede
> Vorfilter-Achse, die ich mir denken kann, außer der einen, um die es geht. Ich habe für den springenden
> Roboter eine eigene Übung geschrieben; im Katalog ist sie von der Einhorn-Fassung **nicht
> unterscheidbar**. Der Nächste, der sie sieht, weiß nicht, für wen sie gemacht ist, und schreibt sie neu.
>
> Beim Bild ist genau das schon gelöst — `MediaTagLink` hängt am selben `InterestTag`. Bei der Übung
> fehlt es. Das ist keine neue Idee, das ist eine fehlende Zeile in einer Symmetrie, die es schon gibt."

**Belegt:** `creator/interest-tags` → 5 Tags mit Slugs; `creator/exercises` → Übung 1 („Begrüßungen")
trägt 19 Felder, kein einziges über Interessen. `grep -c InterestTag LearnEntities.cs` → **0**.
`grep -c -i interest ExerciseAuthoringDtos.cs` → **0**.

### Feedback Vater (O-Ton, Supervisor)

> „Ich pflege die Interessen meines Sohnes selbst, und das funktioniert: `supervisor/children/1/interests`
> gibt mir `brawl-stars +2`, `fussball +2`, `pokemon +2`. Gewichtet, mit Vorzeichen — Abneigungen kann ich
> also auch hinterlegen.
>
> Was ich davon **habe**, sehe ich nicht. Die Gewichte steuern die Bildwahl, gut. Aber wenn ich eine Übung
> zuweise, kann ich nicht erkennen, ob sie zu ihm passt — es steht ja nicht dran. Solange das nicht am
> Katalog hängt, ist mein Pflegeaufwand eine Eingabe ohne Ausgabe."

**Belegt:** `supervisor/children/1/interests` → drei Tags, `weight: 2`. Die Anzeige der Passung ist
ausdrücklich **nicht** Teil dieser Story (B-46, Entscheidung 3 → gehört zu
[B-18](backlog/B-18-auto-lehrplan-generator.md)); der Satz bleibt hier als das stehen, was er ist: eine
richtige Erwartung an eine spätere Story.

### Feedback Sohn (O-Ton, Student)

> „Ich mag Pokémon. Woher weiß das Programm das? Ich hab's nie gesagt — mein Papa hat's eingetippt.
> Ich kann's auch nirgends nachlesen und nichts dazu tun. Wenn ich morgen Basketball besser finde, muss
> ich fragen."

**Belegt:** `Controllers/Student/` enthält **keinen** Treffer für `interests`. Der ganze
Interessen-Bereich liegt unter `supervisor/children/{id}/interests` mit
`[Authorize(Roles = Roles.Supervisor)]` (`ChildInterestsController.cs:23,26`) — das Kind hat nicht einmal
einen **Lese**weg. Das ist der Kern von [B-50](backlog/B-50-kind-beschreibt-sich-selbst.md) und damit
Kandidat für Sprint 2, nicht für diesen.

## Ein Befund schon beim Aufbau: die Entwickler-DB wird abgewiesen

Der erste Startversuch der API endete mit einer `InvalidOperationException`:

```text
Die Datenbank 'pugling.db' stammt aus der alten Migrationskette (1 angewandte, davon keine bekannt –
z. B. '20260811194329_InitialCreate'). Die Kette wurde zu einer InitialCreate zusammengefaltet.
```

Das ist **kein Defekt, sondern die Regel bei der Arbeit**: B-170 hat heute die Kette neu gefaltet, und ein
Upgrade-Pfad existiert bewusst nicht ([CLAUDE.md](../CLAUDE.md) → „EF-Migrationen"). Der Lauf hat die
vorhandene `pugling.db` deshalb **nicht angetastet**, sondern gegen eine frische Datei gefahren
(`ConnectionStrings__Default=Data Source=nachtlauf-2026-09-02.db`) — das ist auch der Weg, den ein Mensch
nach dem Pull nehmen muss. Notiert, weil es die erste sichtbare Folge der heutigen Schemaänderung ist und
sonst morgen als Fehler gelesen wird.

## PM-Synthese & Priorisierung (Step 3)

Der Backlog trägt 64 offene Stories, davon **27 auf `geschaetzt`** und damit nach Freigabe 8 baubar
(zwei davon extern blockiert: [B-07](backlog/B-07-db-umbau-restetappen.md) wartet auf einen Azure-Handgriff,
[B-31](backlog/B-31-geraete-vorbehalt-klang.md) auf ein echtes Handy).

Die höchste erreichbare Priorität ist P2, und dort liegt genau **ein** roter Faden statt einer Sammlung:

| Story | Prio | Art | Größe | Wo | Rolle im Faden |
|---|---|---|---|---|---|
| [B-46](backlog/B-46-interessenbasierte-uebungen.md) | P2 | Wunsch | L | backend | **Fundament** — der Katalog kann Interessen überhaupt tragen |
| [B-50](backlog/B-50-kind-beschreibt-sich-selbst.md) | P2 | Wunsch | L | beides | Das Kind füllt sie selbst |
| [B-19](backlog/B-19-schuelerprofil-ki-lehrplan.md) | P2 | Wunsch | M | backend | eigener Faden (Lehrplan aus Profil) — **nicht** in diesem Sprint |
| [B-39](backlog/B-39-supervisor-dashboard.md) | P2 | Wunsch | L | beides | eigener Faden (Dashboard) — **nicht** in diesem Sprint |

**Roter Faden: „Interessen".** B-46 und B-50 sind dieselbe Sache von zwei Seiten; B-19 und B-39 sind
gleich wichtig, aber andere Fäden — sie bleiben ausdrücklich draußen, statt den Sprint zu einer Sammlung
zu machen.

**Und der Faden wird auf zwei Sprints geschnitten, nicht in einen gepresst.** Beide Stories sind `L`,
und Step 6 läuft je Sprint einmal: B-50 bringt eine eigene Oberfläche für das Kind mit und braucht daher
einen eigenen Rollengang. Zwei L-Stories mit zwei Oberflächen in einem Sprint wären genau der Zustand,
den die Obergrenze von sechs Stories verhindern soll — „Step 6 muss bezahlbar bleiben".

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel (aus dem Sitz des Creators, in Step 6 widerlegbar):** Ich kann eine Übung anlegen, die
festhält, für welche Interessen sie eingekleidet ist, und sie im Katalog von einer anders eingekleideten
unterscheiden — auch dann, wenn ich sie für **kein** bestimmtes Kind gebaut habe.

**Umfang:** [B-46](backlog/B-46-interessenbasierte-uebungen.md), allein. Ein Sprint von eins ist
zulässig (`pm-loop`: „Take as few as the thread demands — there is no lower bound"), und hier ist er
richtig: das Fundament trägt zuerst, B-50 setzt darauf.

**Draußen, mit Namen:** B-50 (eigener Rollengang, Sprint 2), B-19 und B-39 (andere Fäden), B-18 (die
Anzeige der Passung — von B-46s Entscheidung 3 ausdrücklich abgeschnitten).

**Autonomie:** B-46 ist ein `Wunsch`, aber `geschaetzt` — nach Freigabe 8 baubar. Zusätzlich trägt ihr
`## Verlauf` eine ausdrückliche Autonomie-Freigabe des Nutzers vom **2026-08-07** für genau diese Story.
Es ist also nichts mehr zu entscheiden, nur auszuführen.

## Entwickler-Brief (Sprint 1)

**Ziel:** Der Katalog trägt, für welche Interessen eine Übung eingekleidet ist — als Referenz auf die
**bestehende** Taxonomie, nicht als neues Konzept.

**Quelle der Wahrheit:** `InterestTag`. Die Übung bekommt eine Verknüpfungstabelle nach dem Muster von
`MediaTagLink` (n:m, **ohne** Gewicht: Dressing ist Ja/Nein, keine Rangfolge). Damit referenzieren Bild
**und** Übung dieselbe Achse, an der schon die Kind-Interessen hängen.

**Guards:** Existenz (unbekannte Tag-Id → `validation_error`, keine stille Übernahme) · Rolle/Eigentum
unverändert (die Links laufen durch `Create`/`Update` der bestehenden Basis, also durch `EnsureCanWrite`) ·
Vollständigkeit (PUT ersetzt die Menge, wie jedes andere Feld dort).

**Migration:** ja — Kette wird **neu gefaltet**, nicht verlängert. Zwei neue Fremdschlüssel gehören ins
abgenommene Löschverhalten-Inventar der `SchemaGuardTests`.

**Die eine Stelle je Schicht** (das ist, was diesen `L`-Umfang trägt):

| Schicht | Die eine Stelle |
|---|---|
| Vertrag | `ExercisePayload<TConfig>` / `ExerciseResponse<TConfig>` — deckt **alle** Übungstypen |
| API | `ExerciseControllerBase.Create`/`Update`/`Map` — eine Basis für jeden Typ-Controller |
| Client | `CreateExerciseAsync`/`UpdateExerciseAsync` nehmen das Payload-`record` — **keine** Änderung nötig |
| Agent | `ExerciseStrategy.Payload(…)` — der eine Helfer, den alle vier Strategien benutzen |

**Testweg:** `ExerciseMetadataTests` (Anlegen/Ändern/unbekannte Id), `CreatorAgentTests` (beide
Agenten-Pfade, `FakeChatClient` — kein Ollama), `SchemaGuardTests` (die neue Beziehung).
Kein Frontend betroffen (B-46 Entscheidung 3), daher **kein** E2E-Zusatz — der Rollengang wird als
Live-Probe gegen die laufende API erbracht und als solche benannt.

## Iteration 1 — umgesetzt

**Backend (API-First, zuerst):** neue Verknüpfung `ExerciseInterestLink` (n:m **ohne** Gewicht, Muster
`MediaTagLink`), Kette neu gefaltet — Snapshot-Diff **45 Zeilen, rein additiv**. `InterestTagIds` am
generischen Vertrag `ExercisePayload`/`ExerciseResponse` (deckt alle Übungstypen) und an `ExerciseDetail`
(dessen Doku „all metadata" verspricht); `ExerciseSummary` bleibt bewusst schmal. Zentral in
`ExerciseControllerBase`: `InterestTagsProblemAsync` (unbekannte Id → `validation_error`) und
`SetInterestLinks` (Vollersatz, nicht Merge).

**Agent:** `--interests a,b` für den **kindlosen** Pfad; mit `--child` kommen die Tags aus den
**positiv** gewichteten Interessen des Kindes — Abneigungen können strukturell nicht mitkommen, weil
`ChildFacts.WeightedInterests` schon auf `Weight > 0` gefiltert ist.

**Client:** **keine** Änderung. Die Methoden reichen das Payload-`record` durch; der Angriffsplan hatte
hier „eine Zeile je Methode" erwartet.

**Frontend (nicht geplant, siehe unten):** `payloadFrom` reicht die Einkleidung durch, weil der Editor per
Vollersatz-PUT speichert — sonst hätte „Speichern" gelöscht, was der Agent eingetragen hat.

## Review (Step 5) — beide Reviewer, vier Funde mit Code-Wirkung

Der Zähler der Freigabe 3 steht damit auf **4 von 5** (gezählt wird ein Fund nur, wenn seine Behebung Code
oder Tests ändert): der `unused`-Filter ohne den dritten Referenzierer, die unbeobachtete Tag-Validierung im
Änderungspfad, die auf 100 Einträge abgeschnittene Slug-Auflösung, und mein falscher Kommentar über den
`as`-Cast in der Frontend-Attrappe. Details und je eine rote Probe stehen im `## Verlauf` von
[B-46](backlog/B-46-interessenbasierte-uebungen.md).

**Nicht gezählt, weil als Story gebucht statt behoben:** die Anzeige der Einkleidung im Vater-Web samt der
Sackgasse „gelöschtes Stichwort ⇒ unbehebbarer 400" →
[B-206](backlog/B-206-einkleidung-ist-unsichtbar-und-nicht-korrigierbar.md). Der Riegel am Löschen ist eine
**Produktentscheidung**; Freigabe 1 verbietet sie in diesem Lauf ausdrücklich, und sie steht unten bei den
offenen Punkten.

## Der teuerste Fehler dieser Nacht war ein Mess-Fehler, kein Code-Fehler

Er gehört hierher, weil er zwei Stunden gekostet hat und keine einzige Zeile Produktcode betraf.

Meine Proben-Schleife spielte die Datei nach jeder Probe per `mv` aus einer Sicherung zurück. `mv` erhält
die **alte** Änderungszeit — MSBuild hielt den Build daraufhin für aktuell und benutzte weiter die Assembly
**mit** der eingebauten Probe. Folgen:

1. Vier Probenergebnisse waren wertlos (zwei zeigten den falschen Test).
2. Danach war die „Grundlinie" rot, und zwar an einem Fall, der in Ordnung war.
3. Ich habe diesen Phantom-Defekt bis zur minimalen Reproduktion verfolgt — inklusive eines instrumentierten
   Wächters —, bis der entscheidende Fall nach einer `cp`-Wiederherstellung plötzlich **korrekt 400**
   lieferte.

Was daran zählt: **eine falsche Messung ist teurer als ein Fehler**, weil sie Vertrauen in die Zahl
zerstört, an der alles andere hängt. Der Vorschlag der Retrospektive unten greift genau das auf.

## Runde 1 — Rollengang / Abnahme (Step 6)

**Kein Browser gebunden** (Freigabe 6, Ersatz-Fassung). Der Rollengang ist darum zweiteilig, und beide
Teile sind benannt statt behauptet.

### Creator — signiert

Live gegen die laufende Instanz auf `:5200` gefahren, **nach** der letzten Änderung gestartet (frische
Wegwerf-DB, danach entfernt). Skript-Ausgabe im Klartext:

| Schritt | Ergebnis |
|---|---|
| Zwei Stichworte anlegen, Übung damit einkleiden | `id 13`, Tags `[6, 7]` — aufsteigend |
| Einzelabruf / Liste / Katalog-Detail | `[6, 7]` · `[6, 7]` · `[6, 7]` — alle drei Sichten einig |
| PUT nur mit Einhorn | Einkleidung jetzt `[6]` — der Vollersatz **nimmt weg** |
| PUT mit unbekannter Id `999999` | **HTTP 400** |
| … und danach der Stand | Titel `Robo-Rechnen` (**nicht** „Kaputt"), Einkleidung `[6]` — der abgelehnte PUT hat nichts geschrieben |
| Aufräum-Sicht für das benutzte Stichwort | `exerciseCount = 1`, **nicht** unter `?unused=true` |
| Aufräum-Sicht für das freigewordene Stichwort | erscheint dort — aufräumbar, wie gewollt |

> **Creator:** „Jetzt steht dran, für wen die Übung gemacht ist. Ich sehe es über die API in allen drei
> Sichten, und ich kann eine falsche Zuordnung wieder abnehmen. Was ich noch **nicht** habe, ist die
> Anzeige im Vater-Web — das ist als [B-206](backlog/B-206-einkleidung-ist-unsichtbar-und-nicht-korrigierbar.md)
> notiert, und die Sackgasse darin nehme ich ernst."

### Vater und Sohn — Regressions-Zeugen, nicht zweite Meinung

Beide Ebenen sind von diesem Sprint **nicht** berührt (Entscheidung 3 schneidet die Passung ab, es gibt
keine neue Vater- oder Sohn-Fläche). Was sie tatsächlich getan haben, und nichts darüber hinaus:
**46/46 Playwright** grün über alle Flächen beider Rollen, **852/852** Backend, **328/328** vitest. Kein
O-Ton, weil keine der beiden Rollen mit der Änderung in Berührung kam — eine erfundene Zustimmung wäre
genau der hohle Stempel, den dieser Schritt verhindern soll.

**Ein Wort zu einem E2E-Rot, das keins war:** Der erste vollständige Lauf dieser Nacht meldete
`vater-von-null.spec.ts` als gefallen — während gleichzeitig das Test-Tor `dotnet test` fuhr. Allein
gefahren ist die Spec grün (46/46, 3,9 min). Dieselbe Ursache ließ im Tor-Lauf einen Vertragstest fallen,
der isoliert **12 s statt 2 m 6 s** braucht. Beides ist Last, kein Defekt — und beides ist unten
Retro-Material.

## Retrospektive (Sprint 1)

**Nachschau:** B-196, B-197, B-198 (die Abnahmen des vorigen Sprints) am Code nachgerechnet, **kein
Befund**, `nachgeschaut: 2026-09-02` auf allen drei gesetzt. Je Story steht im `## Verlauf`, *was* geprüft
wurde — nicht „sauber". Index vor diesem Sprint: **Nachgeschaut 117 von 128**, davon 29 mit einem Defekt,
der bei der Abnahme durchkam.

Ein Beinahe-Fehlalarm gehört dazu: Bei B-197 fand mein `grep` nach `onMeldung(null)` den Absende-Weg der
Reihen-Zeile nicht — er heißt `onMeldung?.(null)`. Zwei Minuten lang sah das nach einer **fünften**
Instanz einer Fehlerklasse aus, die schon vier hatte. Das Suchmuster war das fehlerhafte Instrument, nicht
der Code.

### Was die eigenen Tore durchgelassen haben

Drei Dinge, und **keins** davon war ein Produktdefekt:

1. **Eine falsche Messung.** Meine Proben-Schleife spielte Dateien per `mv` zurück; die alte
   Änderungszeit ließ den nächsten Build ein No-op sein, und die Assembly trug weiter die Probe. Vier
   Probenergebnisse waren wertlos, danach war die „Grundlinie" rot, und ich habe einen Phantom-Defekt bis
   zur minimalen Reproduktion verfolgt. **Gemessen**, nicht vermutet: derselbe Aufruf lieferte nach einer
   `cp`-Wiederherstellung korrekt `400`.
2. **Ein Test, der tragfähig aussah und nichts trug.** „Slug wird exakt getroffen" blieb unter seiner
   eigenen Probe **47/47 grün**, weil der Lockvogel hinter dem Ziel sortierte. Die Regel dazu steht schon
   im Projekt; sie hat hier ihren teuersten Fall bekommen, weil der Fall *plausibel* war.
3. **Zwei Läufe, die sich gegenseitig langsam machen.** Das Stop-Hook-Test-Tor feuerte, während der
   E2E-Lauf Server und Browser fuhr: ein roter Test, der isoliert in 12 s grün ist, und eine gefallene
   Spec, die allein durchläuft. Ein Rot, das kein Defekt ist, kostet dieselbe Aufmerksamkeit wie eines,
   das einer ist.

### Der vorgeschlagene Mechanismus — nur vorgeschlagen (Freigabe 3)

**Vorschlag:** Eine Regel in [backend/Pugling.Api.Tests/CLAUDE.md](../backend/Pugling.Api.Tests/CLAUDE.md),
dort wo rote Proben geschrieben werden:

> **Wer für eine rote Probe eine Datei zurückspielt, erneuert ihre Änderungszeit.** Eine Wiederherstellung,
> die den alten Zeitstempel behält (`mv` einer Sicherung, ein Archiv-Auspacken), lässt den nächsten Build
> ein No-op sein — die Assembly trägt weiter die Probe, und die *nächste* Messung ist eine Lüge. `cp` oder
> ein `touch` danach genügt. Erkennungszeichen: die Grundlinie ist rot, obwohl die Datei nachweislich in
> Ordnung ist.

Warum Prosa und kein Tor: Es geht um eine **Vorgehensweise** beim Messen, nicht um eine Eigenschaft des
Codes — ein Tor hätte nichts, woran es prüfen könnte. Und die Datei ist der richtige Ort, weil sie genau
dann lädt, wenn jemand einen Testfall schreibt.

**Zwei Kandidaten, die ich nicht vorschlage, aber benenne** — beide hätten Tor-Form und wären damit nach
der Hausregel eigentlich stärker; sie kosten aber je eine eigene Sitzung:

- **Das Test-Tor pausieren, solange ein E2E-Lauf läuft** (Hook-Änderung: `:5200` belegt bzw. ein
  `dotnet run` lebt → nicht messen, sondern melden). Deckt Punkt 3 mechanisch.
- **E2E nie am Exit-Code messen.** `npx playwright test | grep …` gibt den Code des *letzten* Pipe-Glieds
  zurück, und ein Lauf, der **null Tests** ausführt (Webserver nicht bereit), endet ebenfalls mit `0`.
  Beides ist mir diese Nacht passiert. Ein Wrapper, der die Zeile „N passed" verlangt und bei „0 passed"
  oder fehlender Zeile rot wird, deckt es.

**Bewusst ungedeckt bleibt:** dass eine rote Probe am *richtigen Ort* fällt. Das kann kein Tor prüfen —
es ist eine inhaltliche Frage, und die Antwort heißt „messen, welcher Fall fällt", nicht „prüfen, ob
einer fällt". Genau das ist diese Nacht einmal schiefgegangen und einmal gutgegangen.

## Der Lauf endet hier — und nicht an einer Halt-Bedingung

Keine der Grenzen aus [docs/nachtlauf.md](nachtlauf.md) hat gegriffen: Der Fund-Zähler stand bei **4 von
5**, baubare Themen sind reichlich übrig (26 weitere Stories auf `geschaetzt`), und die Retro hat kein Tor
gelandet, sondern nur vorgeschlagen — Limit 2 greift also nicht.

Er endet, weil zwei Dinge auf einen Menschen warten und beide den nächsten Schritt betreffen:

1. Die **Produktentscheidung** aus B-206 (darf ein benutztes Interessen-Stichwort gelöscht werden?).
2. Der **Retro-Vorschlag** — Freigabe 3 macht ihn absichtlich zu einem Vorschlag, damit er gesehen wird,
   bevor er weitere Arbeit formt.

Der nächste Sprint desselben roten Fadens liegt bereit: [B-50](backlog/B-50-kind-beschreibt-sich-selbst.md)
(„Das Kind beschreibt sich selbst"), `geschaetzt`, `L`, `wo: beides`. Er braucht eine eigene
Sohn-Oberfläche und damit einen eigenen Rollengang — und für dessen sinnlich-visuelle Hälfte einen
Browser oder ein Auge.

## Offene Punkte für den Morgen — die Entscheidungen, die ich gebraucht hätte

1. **Darf ein Interessen-Stichwort gelöscht werden, das Übungen einkleidet?**
   ([B-206](backlog/B-206-einkleidung-ist-unsichtbar-und-nicht-korrigierbar.md), offener Punkt 1). Drei
   Möglichkeiten mit Kosten stehen dort, Empfehlung: **Riegel wie bei der Übung** (`409`), weil die
   Einkleidung Autoren-Information ist und nicht rekonstruierbar. Die Begründung am `DELETE` im Code sagt
   inzwischen ausdrücklich, dass ihr alter Satz seit B-46 nicht mehr stimmt und die Frage offen ist.
2. **Reicht eine nur lesbare Anzeige der Einkleidung, oder braucht es das Entfernen?** (B-206, Punkt 2).
   Empfehlung: beides — eine Anzeige ohne Korrekturweg lädt denselben Vorwurf ein.
3. **Der Retro-Vorschlag** (Änderungszeit bei der Wiederherstellung einer Probe): landen oder verwerfen.
   Dazu die zwei tor-förmigen Kandidaten, die je eine eigene Sitzung kosten.

## Offene Roadmap

Die dauerhafte Liste ist [docs/backlog/](backlog/README.md) — hier nur die Begründung der aktuellen
Reihenfolge: Der rote Faden „Interessen" ist zur Hälfte gebaut. **B-50** ist die andere Hälfte und die
einzige P2-Story, die direkt auf diesem Sprint aufsetzt; **B-206** ist ihr kleiner, defekt-getriebener
Nachbar und sollte davor oder gleichzeitig laufen, weil beide dieselbe Fläche anfassen. **B-19** und
**B-39** sind gleich wichtig, aber eigene Fäden — sie gewinnen nichts dadurch, dass sie jetzt kommen.

## Konkreter Änderungsstand (für Review)

| Commit | Inhalt |
|---|---|
| `0af87e9` | B-170 gebaut — die Übungsart bekommt einen Eigentümer |
| `65591f1` | B-170: elf Reviewer-Funde eingearbeitet |
| `c0b1ad5` | B-170 abgenommen |
| `7d91ac8` | `frontend/CLAUDE.md`: die zweite Hälfte der `useAsync`-Falle |
| `644cff7` | B-46 gebaut — Einkleidung am Katalog, `--interests` am Agenten, Reviewer-Funde |
| `aba1147` | B-46 abgenommen, Index neu |

**Nichts ist gepusht** — das bleibt beim Nutzer. Die Wegwerf-Datenbanken dieses Laufs sind entfernt; die
echte `pugling.db` wurde **nicht** angetastet und muss nach dem Pull einmal gelöscht werden, weil die
Migrationskette zweimal neu gefaltet wurde (B-170 und B-46).
