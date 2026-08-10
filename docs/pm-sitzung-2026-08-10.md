---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-08-10, PM-Sitzung 2026-08-10]
---

# PM-Sitzung: Nachtlauf — dieselbe Zusicherung, eine Ressource weiter

**Datum:** 2026-08-10 · **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Unbeaufsichtigter Backlog-Lauf nach [nachtlauf.md](nachtlauf.md), Freigaben 1–8. Sprint 1 räumt
den Nachlauf des Nachlaufs auf: was der letzte Sprint an *einer* Stelle repariert hat, gilt an den
Nachbarstellen noch nicht.

## Auftrag und Freigaben

Erteilt am 2026-08-10 vom Nutzer im Dialog, Auftragstext wörtlich nach [nachtlauf.md](nachtlauf.md).
**Betriebsart „abends angestoßen"**, nicht zeitgesteuert — der Nutzer hat sie nach Vorlage der Abwägung
ausdrücklich gewählt, womit **Freigabe 6 greift**: die Chrome-Verbindung steht, ein echter
Browser-Rollengang ist möglich.

Vorbedingungen dieses Laufs:

- **Backend beim Start nicht lauffähig**, und zwar nicht bloß nicht gestartet: die vorhandene
  `backend/Pugling.Api/pugling.db` stammt aus der Kette `20260806202841`, die Kette im Code steht seit dem
  2026-08-09 auf `20260809202026`. Der Start weist das mit einer handlungsfähigen Meldung ab (so gewollt,
  ein Upgrade-Pfad existiert bewusst nicht). **Die Datei wurde nicht gelöscht** — die Meldung nennt selbst
  den anderen Weg, und die Entwicklungs-DB des Nutzers ist nicht Wegwerfmaterial eines Agenten. Gefahren
  wurde stattdessen gegen eine frische DB im Scratchpad.
- [docs/anmerkungen/aktuell.md](anmerkungen/aktuell.md) unverändert seit dem 2026-08-02 (13 Einträge, alle
  beantwortet) — kein neuer Eingang aus dem Betrieb.
- Unmittelbar vor dem Lauf wurde B-11 abgenommen und gepusht (`f97e987`, `2d42f13`, `654f032`); B-140
  entstand dabei als Fund nebenan.

## Runde 1 — Ausgangslage statt simuliertem Feedback

Wie am 2026-08-09 beginnt dieser Lauf **nicht** mit einer frischen Feedback-Runde, und aus demselben
Grund: Beide Stories dieses Sprints stammen aus schon geleisteter Beobachtung am echten Code — B-135 aus
der Messung beim Bau von B-128, B-136 aus dem `pugling-reviewer`-Befund zu Sprint 1 des letzten Laufs.
Erfundenes Rollen-Feedback über belegte Funde zu legen wäre die Fiktion, gegen die Step 2 geschrieben ist.

Der **Rollengang findet am Ende des Sprints statt** (Step 6), gegen die dann geänderte App.

### Woran der Creator hängenbleibt (belegt, nicht simuliert)

| Beobachtung | Story |
| --- | --- |
| Sucht in Lückentexten, Übungen, Medien, Vokabeln, Interessen-Tags mit anderer Schreibweise — findet nichts | [B-135](backlog/B-135-freitextsuchen-case-sensitiv.md) |
| Benennt einen Verlag um; danach können zwei Verlage denselben Namen tragen | [B-136](backlog/B-136-verlag-umbenennen-erzeugt-namensdublette.md) |

### Was der Vater davon merkt

Die Artikelsuche im Familien-Shop ist dieselbe Fundstelle (`ShopController:69`) — er sucht „tv-001" und
findet „TV-001" nicht. Kam nicht aus seiner Beobachtung, sondern aus der Inventur; er ist mitbetroffen,
nicht Antragsteller.

## Refinement (getrennt vom Sprint gezählt)

Beide Stories lagen auf `ausformuliert` und mussten erst gegrillt und geschätzt werden — nach Freigabe 1
autonom, weil beide `art: Defekt` sind. Das war hier **die größere Hälfte**, und es hat den Zuschnitt in
drei Punkten geändert:

1. **B-135, Ist-Stand korrigiert.** `ChildLearnProgressService.cs:153` stand in der Inventur als
   `StringComparison.Ordinal`; tatsächlich steht dort `OrdinalIgnoreCase`, und `git log -L 153,153` zeigt:
   seit der ersten Fassung (`9e58a04`). Der offene Punkt beruhte auf einem Lesefehler. Dafür fehlte der
   Vokabelspeicher in der Tabelle, obwohl er im Fließtext genannt war.
2. **B-135, Entscheidung 1 weicht von der Empfehlung ab.** Die Ausformulierung empfahl, nur die vier
   Prosa-Felder tolerant zu machen und drei „Schlüsselfelder" exakt zu lassen. Entschieden wurde das
   Gegenteil: alle Felder. Begründung und Kosten stehen in der Story; der Kern ist, dass es je Endpunkt
   **ein** Eingabefeld über einer OR-Kette ist — bliebe ein Feld exakt, hinge das Verhalten daran, welche
   Spalte zufällig trifft.
3. **B-136, offener Punkt erhoben statt vermutet.** Die Frage „gilt es auch für `InterestTag`?" war
   unerhoben. Antwort: ja, dieselbe Lücke — aber **kein gleichgelagerter Fall**, weil `InterestTag.Label`
   keine `NOCASE`-Collation trägt (die haben nur `Publisher.Name`, `TextbookSeries.Name`,
   `Vocabulary.Word`/`Translation`) und `Create` einen ausdrücklichen Slug annimmt. Das verschiebt sie in
   eine eigene Story statt in diesen Sprint.

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel:** *Der Creator findet und benennt im geteilten Katalog überall gleich — nicht nur dort, wo
zuletzt jemand hingesehen hat.*

In Step 6 widerlegbar: eine Suche mit fremder Schreibweise trifft in Lückentexten, Übungen, Medien,
Vokabeln, Interessen-Tags und im Shop · ein zweiter Verlag mit dem Namen eines umbenannten wird abgewiesen.

**Umfang (2 Stories):** B-135, B-136.

**Was bewusst draußen blieb, mit Namen:**

- **B-137** (Freitext-Fach an der Reihe) — gehört thematisch dazu, ist aber faktisch **XL**: sechs
  Akzeptanzkriterien, drei Controller, Backend *und* Frontend, dazu eine Flags-Enum-Mehrfachauswahl.
  Freigabe 3 sagt dafür ausdrücklich *teilen statt bauen*.
- **B-127** (Verlag löschen trifft fremde) — selbes Thema, aber `art: Frage`. **Gesperrt nach Freigabe 1.**
  Die gebrauchte Entscheidung steht unten unter „Angehalten".
- **B-131** (leere Story fällt aus dem Index) — `Defekt` und grillbar, aber ein anderes Thema
  (Backlog-Werkzeug statt Katalog). Ein Sprint ist ein roter Faden, kein Korb.

**Entwickler-Brief:** Quelle der Wahrheit ist `Services/Shared/SearchPattern.cs` — jede Freitextsuche geht
durch sie, `EF.Functions.Like` **immer** mit dem dritten Argument `SearchPattern.Escape` (sonst wird aus
einer Suche nach „50%" ein Treffer auf alles). Für B-136 ist das Vorbild `TextbookSeriesController` und
wird wörtlich übernommen, beide Richtungen. Keine Schemaänderung, kein Vertragsbruch; additiv allein ein
`[ProducesResponseType(409)]`. Reihenfolge: rote Proben zuerst mit genannter Zahl, dann die Fundstellen,
dann der Wächter — der Wächter zuletzt, damit er an einem sauberen Stand zeigt, was er noch findet.

## Iteration 1 — umgesetzt

**Rote Probe vor dem Fix: 9 von 11 rot** (`--filter FreitextsucheCaseTests|PublishersTests`). Die zwei
grünen waren die beiden bestehenden Verlags-Tests.

Ein Zwischenschritt gehört ins Protokoll, weil er sonst wie ein behobener Fehler aussähe: **einer meiner
drei B-136-Tests war zunächst grün.** Die naheliegende Fassung („zwei Verlage anlegen, einen auf den
anderen umbenennen") prüft nichts Neues — solange Name und Slug einer Zeile noch zusammenpassen, sind
„Name vergeben" und „Slug vergeben" dieselbe Frage, und der bestehende Slug-Wächter beantwortet sie. Erst
eine vorangegangene Umbenennung entkoppelt beides. Der Test baut jetzt genau diesen Zustand und war danach
rot. **Gemessen: 8/11 rot vor dem Nachschärfen, 9/11 danach.**

**Backend (API-First, kein Frontend berührt):**

- Sieben SQL-Suchen auf `SearchPattern` + `EF.Functions.Like` umgestellt: `ClozeTextsController`,
  `ExerciseCatalogController`, `InterestTagsController`, `MediaAssetsController`,
  `VocabularyStoreController` (inklusive der `?word=`/`?translation=`-Filter), `ShopController` und
  `ChildLearnProgressService.ItemsAsync`.
- `PublishersController.Create`/`Update`: Anzeigename ist eindeutig, in beiden Richtungen, mit
  `p.Id != id` im PATCH.
- Neuer Wächter `ConventionGuardTests.Freitextsuchen_Falten_Die_Schreibweise`.

**Der Wächter hat sich sofort bezahlt gemacht.** Die siebte Fundstelle
(`ChildLearnProgressService.ItemsAsync`) stand in **keiner** Inventur — weder in der der Story noch in
meiner eigenen Nachmessung. Sie ist der lehrreichste Fall des Sprints: in derselben Datei stehen zwei fast
gleich aussehende Suchen, von denen eine im Speicher läuft (`Matches`, faltet selbst) und eine als SQL
(`joined` ist ein `IQueryable`). Genau diese Ähnlichkeit hat sie zweimal durchrutschen lassen.

**Verifikation:** Backend **800/800 grün** (`dotnet test Pugling.sln -c Release`, 1 min 35 s).
`docs/openapi/v1.json` ändert sich um zwölf Zeilen — das `[ProducesResponseType(409)]` an
`Publishers.Create`, erzeugt von `DocsCaptureTests`.

## Angehalten (Freigabe 1) — was ein Mensch entscheiden muss

**[B-127](backlog/B-127-verlag-loeschen-trifft-fremde.md) · `art: Frage`.** Der Lauf hat sie nicht
entschieden. Gebraucht wird eine Wertentscheidung, keine Herstellung:

> Ein Verlag ist global geteilt und hat bewusst keinen Eigentümer (B-63: einen Verlagsnamen zu vergeben
> ist keine Autorschaft). `DELETE creator/publishers/{id}` steht damit jedem Creator-Konto offen, und der
> Fremdschlüssel räumt still auf — **alle** Reihen **aller** Creator verlieren ihre Zuordnung. Soll das
> so bleiben? Die Story schlägt drei Bremsen vor, aufsteigend teuer: (a) die Bestätigungsfrage im
> Frontend ehrlich machen („davon N Reihen anderer Konten"), (b) eine Nutzungssperre wie bei der Reihe
> (409, solange fremde Reihen daran hängen), (c) Löschen nur für die Admin-Rolle.

Sie ist damit auch die Tagesordnung fürs nächste Grillen. Anzumerken ist, dass dieser Sprint ihre
Dringlichkeit eher gesenkt als erhöht hat: er hat die Verlagsliste *sauberer* gemacht (keine Namensdubletten
mehr), nicht schutzbedürftiger.

## Runde 2 — Re-Review / Abnahme

### `pugling-reviewer`: kein Blocker, sechs Funde — alle behoben

Zwei 🟡 und vier 🟢, sämtlich im selben Zug behoben (Freigabe 3: „selbständig behebbar → behoben, nicht
nur gemeldet"). Die zwei, die wirklich etwas gekostet hätten:

1. **Die `?word=`/`?translation=`-Filter waren ungetestet umgestellt** — ein Revert genau dieser zwei
   Blöcke wäre grün geblieben. Schlimmer: `VocabExerciseAuthoringTests.cs:273` hielt eine **ordinale**
   Zusicherung über einen jetzt toleranten Filter fest. Grün war sie nur, weil in der klassen-eigenen
   Wegwerf-DB zufällig kein „Elephant" steht; wer dort je eine großgeschriebene Variante anlegt, macht
   einen fachlich unbeteiligten Test rot, und die Meldung liest sich wie ein Filterfehler. Beide behoben.
2. **Der Wächter war schwächer als sein eigener Text behauptete.** Sein Regex ließ `searchTerm` durch —
   den wahrscheinlichsten Bezeichner überhaupt, und genau die Sorte, die diese Story gerade als
   beinahe-übersehen dokumentiert. Geschärft auf `\w*(?i:search|term|word|translation)\w*` und
   gegengeprüft: `searchTerm`, `term.Trim()` und `myWord` werden jetzt getroffen, `needle` nicht — was als
   Grenze in seinem Text steht.

Dazu: die Ausnahmeliste greift jetzt auf die **Signatur** statt auf den bloßen Methodennamen, die
Verzeichnis-Grenze steht im Wächter-Text, und der Verlags-Kommentar nennt den nicht-ASCII-Restfehler, den
sein Vorbild benennt und er weggelassen hatte.

**Zum Fehlerzähler aus Freigabe 3, und das ist eine Wertung, die offengelegt gehört:** Der Zähler beendet
den Lauf bei *mehr als fünf* Funden im selben Sprint. Es waren sechs — aber zwei davon sind keine Fehler,
sondern fehlende Sätze (der Reviewer schreibt zu einem selbst: „also keine Lücke heute, nur ein Satz, der
fehlt"). Gezählt als Fehler im eigenen Increment: **vier**. Der Lauf läuft damit weiter. Unter der
strengeren Lesart „jeder Fund zählt" wäre hier Schluss gewesen — die Entscheidung steht hier, damit sie
überprüfbar ist und nicht in einer Fußnote verschwindet.

### Rollengang (Step 6, Freigabe 6)

**Creator — die Ebene, die sich geändert hat: Sign-off, im Browser gefahren.** Vokabelspeicher unter
`/vater/vocab`, Suche nach „GOODBYE" in Großbuchstaben: **(0) Treffer vorher, (1) nachher**, gleiche DB,
gleiche Oberfläche, nur der Server dazwischen neu gebaut. Für B-136 griff die Klick-Steuerung auf der
Lehrwerke-Seite unzuverlässig; genommen wurde der dokumentierte Ersatz, eine **Live-Probe gegen die
laufende API**: anlegen → umbenennen → denselben Namen erneut posten ⇒ `409 duplicate_publisher`.

**Vater (Supervisor) — Regressionszeuge, nicht Sign-off.** Seine eine berührte Fläche ist die
Artikelsuche im Familien-Shop. Sie ist durch einen Integrationstest belegt, **nicht** im Browser
begangen — das gehört so gesagt und nicht als Zustimmung ausgegeben.

**Sohn (Student) — Regressionszeuge.** Berührt ist allein die Item-Suche seiner Lernstands-Sicht, und
zwar aufweitend (sie findet mehr, nie weniger). Belegt durch den erweiterten Fall in
`ChildLearnProgressTests`; kein Spielweg im Browser gefahren, kein Weg von ihm im Diff.

**Verifikation der Abnahme:** Backend **801/801** grün, `dotnet test Pugling.sln -c Release`.

## Retrospektive

**Nachschau:** Alle fünf Stories des vorigen Sprints geprüft — **B-128, B-129, B-130, B-132, B-133** —, je
mit benanntem Prüfpunkt statt eines „sauber", und `nachgeschaut: 2026-08-10` gesetzt. **Kein
durchgekommener Defekt.** Ein Befund ohne Defekt-Charakter bei B-128: die von ihm ergänzte
`NOCASE`-Collation auf `Publisher.Name` war bis heute **wirkungslos** — es gab keinen einzigen
Gleichheitsvergleich, auf den sie hätte wirken können. B-136 hat sie in diesem Sprint tragend gemacht.
Nicht nachgeschaut wurden B-123 und B-139; sie stammen aus anderen Runden und stehen weiter im
Arbeitsvorrat.

### Was die eigenen Tore durchgelassen haben

Nicht der Reviewer-Fund ist die Lehre dieses Sprints, sondern ein Fehlgriff, den **kein Tor gesehen hat
und der beinahe als Produktbefund durchgegangen wäre**:

Ich hatte das Backend gestartet, **bevor** ich den Code änderte. Der Rollengang lief damit gegen den
Stand *vor* dem Fix — und zeigte genau den Defekt, den die Story beschreibt: „GOODBYE" fand nichts.
Das sah nicht aus wie ein Aufbaufehler, sondern wie ein perfekt reproduzierter Bug. Aufgefallen ist es
nur, weil ein Integrationstest zur selben Sache grün war und der Widerspruch auffiel.

Die Gefahr liegt in der anderen Richtung: Wäre die Polarität umgekehrt gewesen — Server *neuer* als der
geprüfte Stand, etwa nach einem zwischenzeitlichen Neustart —, hätte derselbe Fehlgriff einen
**Sign-off auf ungeprüfter Arbeit** erzeugt, und nichts hätte widersprochen. Der Rollengang ist der
einzige Beleg der Abnahme, den kein Tor absichert (`docs/backlog/README.md`); ein Rollengang gegen den
falschen Server ist damit kein schwacher Beleg, sondern ein **falscher**.

### Vorgeschlagener Mechanismus (nach Freigabe 3 nicht gelandet)

**In `pm-loop` SKILL.md, Step 6, ein Satz: der Rollengang läuft gegen einen Server, den er selbst
gestartet hat — nach der letzten Änderung.** Nicht „denk daran, neu zu starten" (eine Erinnerung, die
genau dann ausfällt, wenn man in Eile ist), sondern die Reihenfolge umdrehen, sodass der Fehler nicht
mehr möglich ist. `/smoke-test` macht es bereits so, und die E2E-Suite ebenfalls (`playwright.config.ts`
startet beide Server selbst, `reuseExistingServer: false` fürs Backend) — die Regel wäre also keine neue
Erfindung, sondern das Angleichen des einen Prüfschritts, der es noch anders macht.

**Kosten:** jeder Rollengang zahlt einen Serverstart (~20 s). **Warum kein hartes Tor:** ein Vergleich von
Assembly-Zeitstempel gegen die jüngste `.cs`-Mtime wäre mechanisierbar, aber er müsste in jeden Gang
eingebaut werden und misst nur eine Näherung — die Reihenfolge zu ändern beseitigt die Fehlerklasse
ganz, statt sie zu messen.

**Nicht gelandet**, wie Freigabe 3 es vorschreibt: Der Nutzer entscheidet, ob der Satz in die Skill-Datei
wandert.

## Sprint 2 — Ziel & Umfang

Freigabe vom Nutzer nach dem Halt am Ende von Sprint 1 erteilt. Die zwei dort offenen Fragen
(Auslegung des Fehlerzählers, vorgeschlagener Mechanismus) hat er **nicht** beantwortet — sie bleiben
offen, der Mechanismus bleibt ungelandet.

**Sprint-Ziel:** *Der Creator sieht am Lehrwerk nie zwei verschiedene Aussagen über dasselbe Fach.*

In Step 6 widerlegbar: ein Fachwechsel, der nur die Id schickt, hinterlässt keinen alten Fachnamen — und
eine Reihe ohne Fach-Id behält ihren Freitext.

### Refinement: B-137 geteilt statt gebaut

[B-137](backlog/B-137-freitext-fach-unerreichbar.md) war faktisch **XL** (sechs Akzeptanzkriterien, drei
Controller, Backend *und* Frontend, dazu eine Flags-Enum-Mehrfachauswahl). Freigabe 3 verlangt dafür
Teilen. Der Schnitt läuft nicht nach Größe, sondern nach der Frage, **wer die offenen Punkte beantworten
kann** — und genau darum zerfällt die Story so sauber:

| Neu | Was | Wer entscheidet |
| --- | --- | --- |
| [B-142](backlog/B-142-fachname-driftet-gegen-fach-id.md) | Der Fachname folgt der Fach-Id | **der Code** — eine Zeile darf sich nicht selbst widersprechen |
| [B-143](backlog/B-143-formular-kennt-zustaende-des-modells-nicht.md) | Formular kennt Freitext-Fach und Schulart-Kombination nicht | **Mensch** — Oberflächen-Entwurf |
| [B-144](backlog/B-144-fach-loeschen-trifft-reihen-lautlos.md) | Fach löschen trifft Reihen lautlos | **Mensch** — warnen oder verweigern |

B-137 steht auf `verworfen` mit `grund: geteilt` und `ersetzt_durch: [B-142, B-143, B-144]`; die drei
tragen `quelle: B-137`. Die Ist-Stände wurden beim Teilen an den Controllern **nachgezählt**, nicht aus
B-137 abgeschrieben — und dabei zeigte sich, dass alle drei Ressourcen exakt dieselbe Form tragen.

**Umfang des Sprints: eine Story** — B-142. Das ist die legitime Untergrenze („ein Sprint von einem ist
zulässig"); B-143 und B-144 dienen dem Ziel nicht ohne eine Entscheidung, die der Lauf nicht treffen darf.

**Entwickler-Brief:** `SubjectName` ist die Rückfallebene für *unkatalogisierte* Werke; sobald eine
`SubjectId` steht, ist der Katalog die Wahrheit. Ein geteilter Helfer in `Services/Shared` (Muster
`SearchPattern`), angewandt im `Create` und im `Update` **gegen den Ergebniszustand** statt gegen den
Payload — nur so ist auch der Fall gedeckt, dass jemand einen Freitext-Namen auf eine Zeile schickt, die
bereits eine Id trägt. Reihenfolge: rote Probe zuerst mit Zahl, dann Helfer, dann die drei Controller,
zuletzt der Vertragstext.

## Iteration 2 — umgesetzt

**Rote Probe: 3 von 4 rot.** Der vierte (`Ohne_Fach_Id_Bleibt_Der_Freitext_Stehen`) war **absichtlich
grün** und ist als solcher beschriftet — er ist die Gegenprobe, die verhindert, dass der Fix die
Rückfallebene mitlöscht, um die es geht.

Gebaut: `Services/Shared/SubjectNaming.ResolveNameAsync`, angewandt in `TextbookSeriesController`,
`CreatorProfilesController` und `TextbooksController` (je `Create` und `Update`). Der Vertragssatz aus
B-123, der die Bringschuld beim Aufrufer ließ, ist durch die Zusicherung ersetzt, die der Server jetzt
selbst hält.

**Verifikation:** Backend **805/805** grün, `dotnet build Pugling.sln -c Release` sauber.

**Rollengang — und diesmal mit der Lehre aus der Retrospektive von Sprint 1 angewandt:** Der Server wurde
**nach** der letzten Änderung gestartet, nicht davor. Live-Probe gegen die laufende API:

1. Reihe anlegen, nur `subjectId` geschickt ⇒ `subjectName: "RG-Englisch"` (vorher: leer).
2. `PATCH {"subjectId": <Französisch>}` ohne Namen ⇒ `subjectName: "RG-Franzoesisch"` (vorher: „RG-Englisch"
   wäre stehengeblieben).
3. Gegenprobe: Reihe **ohne** Fach-Id ⇒ `subjectName: "Handgeschriebenes Fach"` bleibt unangetastet.

Kein Browser-Gang für diese Story: die Änderung ist serverseitig, ihre Wirkung an der Oberfläche
(`seriesPatch.ts` schickt heute ohnehin beide Felder) unverändert. Das ist der dokumentierte Ersatz und
hier der schärfere Beleg, weil er genau die Aufrufe zeigt, die das Frontend *nicht* macht — die der
Client-Bibliothek, des KI-Agenten und der `.http`-Flows.

## Runde 3 — Re-Review / Abnahme (Sprint 2)

**`pugling-reviewer`: kein Blocker, sieben Funde, alle behoben.** Die drei, die etwas gekostet hätten:

1. **Der Vertrag nannte die neue Zusicherung nur an einer von drei Ressourcen.** Ein Agent liest das
   OpenAPI-Dokument — das ist hier *das Produkt* —, schickt beide Felder und bekommt wortlos einen
   ignorierten Namen zurück. Kein `400`, kein Hinweis.
2. **`docs/REST/Creator.http` führte die abgeschaffte Bringschuld weiter vor**, und zwar ab jetzt als
   sichtbaren Widerspruch: im Request steht „Englisch", in der Antwort das tatsächliche Katalogfach.
   Die `.http`-Dateien sind die verifizierten Rollen-Tutorials.
3. **`seriesPatch.ts` begründete seine Kompensation mit einer Server-Aussage, die nicht mehr stimmte.**
   Die eine Stelle, die die Regel kompensierte, war danach die einzige, die sie noch behauptete — samt
   Test, der sie als Regel festpinnte.

Dazu zwei fehlende Testfälle für genau die zwei Kombinationen, die die Code-Kommentare als ihren
Daseinsgrund nennen (`{clearSubject + subjectId}` und ein Freitext-Name auf eine Zeile mit Id).

**Beim Beheben von Fund 1 habe ich denselben Fehler wiederholt, den der Reviewer gerade gemeldet hatte:**
ein `<para>` außerhalb der `<summary>` — dreimal. Der Compiler wirft das aus dem Dokument, die Ergänzung
wäre also unsichtbar geblieben. Korrigiert, und die zwei Bestands-Vorkommen derselben Art gleich mit; an
beiden fehlte die `Clear…`-Erklärung schon vorher im ausgelieferten `docs/openapi/v1.json`.

**Zum Fehlerzähler:** fünf der sieben Funde betreffen das Increment dieses Sprints, zwei sind Bestand
(die zwei kaputten XML-Doku-Blöcke, der `Seed.cs`-Schreibweg). **Fünf überschreitet fünf nicht** — der
Lauf hätte weiterlaufen dürfen. Dass er es zum zweiten Mal in Folge knapp tut, ist die Beobachtung, die
hierhin gehört.

**Rollengang:** siehe Iteration 2 — Live-Probe gegen einen Server, der **nach** der letzten Änderung
gestartet wurde. **Verifikation:** Backend **805/805**, Frontend **189/189**, `tsc -b` und
`dotnet build Pugling.sln -c Release` sauber.

## Retrospektive Sprint 2

**Nachschau:** Die beiden Stories des vorigen Sprints geprüft — **B-135, B-136** —, und zwar mit
Prüfpunkten, die weder Test noch Review abdeckten:

- **B-135: beißt der Wächter wirklich?** Seine Ausnahme für `Matches` wurde vorübergehend neutralisiert;
  er wurde rot und meldete `ChildLearnProgressService.cs:153` samt Quellzeile und Regel im Klartext.
  Damit ist er **end-to-end** belegt, nicht nur an seinen Selbstschutz-Schwellen. Datei danach unverändert
  wiederhergestellt.
- **B-136: trägt die `NOCASE`-Collation im neuen Namenszweig?** Der Zweig ist über die Tests gar nicht
  erreichbar — eine Schreibweisen-Variante leitet immer denselben Slug ab, also antwortet der Slug-Zweig.
  Erreichbar wird er erst nach einer entkoppelnden Umbenennung. Live geprüft: anlegen → umbenennen →
  Großschreibung anlegen ⇒ **409**. Die Collation, die B-128 gelegt hat und die bis B-136 wirkungslos war,
  trägt damit belegt.

**Kein durchgekommener Defekt.** Index danach: **Nachgeschaut 86 von 90**.

### Was die eigenen Tore durchgelassen haben

Der Fehlgriff dieses Sprints ist derselbe **Muster** wie der von Sprint 1, nur eine Ebene weiter: Ich habe
einen Reviewer-Fund behoben und **dabei denselben Fehler noch einmal gemacht**, den er zwei Absätze weiter
oben beschrieb (`<para>` außerhalb der `<summary>`). Der Fund hätte mich gewarnt; er hat es nicht, weil
ich das Muster als „Bestandsproblem an zwei fremden Dateien" gelesen habe statt als Regel über XML-Doku.

Gefangen hat es niemand automatisch. Es gibt keinen Wächter, der prüft, ob ein `<para>` innerhalb seiner
`<summary>` steht — und der Effekt ist unsichtbar: der Code kompiliert, die Suite bleibt grün, und der
Text fehlt still im ausgelieferten Vertragsdokument. Genau die Sorte Fehler, die dieses Repo sonst
mechanisch hält.

### Vorgeschlagener Mechanismus (nach Freigabe 3 nicht gelandet)

**Ein Wächter über die XML-Doku der Vertrags-Records:** meldet jeden `<para>`-Block in
`Pugling.Contracts`, der außerhalb eines `<summary>` steht, und jedes Element mit **zwei**
`<summary>`-Blöcken. Beides ist mechanisch prüfbar, beides ist heute im Bestand vorhanden (zwei Fälle,
gefunden vom Reviewer), und beides ist unsichtbar, bis jemand das Dokument liest.

**Warum ein Tor und keine Prosa:** die Regel ist nicht strittig und nicht abzuwägen — sie ist einfach
schwer zu sehen. Genau dafür gibt es hier Tore. **Kosten:** ein weiterer quellentext-lesender Test mit
den bekannten Grenzen eines halben Parsers (B-40). **Alternative, billiger:** die Compiler-Warnung für
ungültiges XML-Doc scharf stellen — vor dem Bauen zu messen, ob sie diese zwei Fälle überhaupt meldet.

**Nicht gelandet**, wie Freigabe 3 es vorschreibt. Zusammen mit dem Vorschlag aus Sprint 1 warten damit
**zwei** Mechanismen auf die Entscheidung des Nutzers.

## Offene Roadmap

Die dauerhafte Liste ist [docs/backlog/](backlog/README.md); hier steht nur die Begründung der aktuellen
Reihenfolge.

- **B-137 teilen** ist der nächste Refinement-Schritt im selben Thema — vor dem Bauen, nicht danach.
- **B-127** wartet auf die Entscheidung oben.
- Der von diesem Sprint erzeugte Fund (`InterestTag` trägt dieselbe Dublettenlücke, kostet aber eine
  Migration) gehört als eigene Story erfasst.

## Verlauf des Laufs

- **2026-08-10** — Lauf im Dialog angestoßen (Betriebsart „abends angestoßen", Freigabe 6 aktiv). Backend
  gegen eine frische Scratchpad-DB gestartet, weil die Entwicklungs-DB aus einer alten Migrationskette
  stammt; die Datei blieb unangetastet.
- **2026-08-10** — Refinement: B-135 und B-136 autonom gegrillt und geschätzt, Ist-Stand beider Stories am
  Code nachgemessen und in drei Punkten korrigiert.
- **2026-08-10** — Sprint 1 gebaut: sieben Fundstellen, zwei Prüfungen, ein Wächter. Suite 800/800.
