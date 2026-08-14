---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-08-14, die Funde von gestern schliessen]
---

# PM-Sitzung 2026-08-14 — Nachtlauf: die Funde der Nachschau schließen

## Auftrag und Freigaben

Unbeaufsichtigter Backlog-Lauf nach `pm-loop` und [docs/backlog/README.md](backlog/README.md).
**Alle acht Freigaben** aus [docs/nachtlauf.md](nachtlauf.md) gelten, wie dort dokumentiert, vom Nutzer für
diesen Lauf erteilt (nicht aus einer früheren Sitzung fortgeschrieben).

**Zwei Entscheidungen des Nutzers zum Umfang**, vor dem Start eingeholt:

1. **Roter Faden: die Funde der Nachschau vom 2026-08-13 schließen.** Begründung des Nutzers folgend meiner
   Empfehlung: Die Ist-Stände dieser Stories sind gestern am Code geprüft worden und verrotten nicht über
   Nacht — bei den älteren `ausformuliert`-Stories wäre der erste Schritt eine Neuprüfung.
2. **B-170 bekommt nur das Ventil, kein Schema.** Die Migrationskette wird in diesem Lauf **nicht** gefaltet.
   Begründung: Ein Fehlgriff an der Kette ist unbeaufsichtigt der teuerste mögliche, `SchemaGuardTests`
   erzwingt Länge 1, und der Snapshot-Diff ist die eigentliche Abnahme — die kann nachts niemand beurteilen.
   Ein Ventil behebt den unreparierbaren Zustand bereits; die saubere Lösung mit `CreatedByAdultId` bleibt
   eine Entscheidung am Tag.

## Ausgangslage (gemessen, nicht geschätzt)

- `geschaetzt`: **28** Stories, fast alle `Wunsch` (seit Freigabe 8 baubar), überwiegend M/L.
  Zwei davon blockiert: B-7 (`wartet_auf` einen Handgriff an der Azure-Instanz), B-31 (`wartet_auf` ein
  echtes Handy).
- `ausformuliert`: **16** Stories, **ausnahmslos** `Defekt` oder `Aufräumen` — also nach Freigabe 1
  vollständig autonom grillbar. Darunter vier P1-Defekte.
- `in-arbeit`: **0** — kein Rest aus einem früheren Lauf.
- Nachschau-Stand beim Start: **103 von 103**, Arbeitsvorrat leer
  ([Protokoll von gestern](pm-sitzung-2026-08-13.md)).

## Sprint-Plan

Drei Sprints, jeder mit einem Ziel aus einem Rollen-Sitz — nicht mit einer Id-Liste, damit Step 6 es
widerlegen kann.

| Sprint | Ziel (aus dem Sitz) | Stories |
|---|---|---|
| 1 | **Vater:** „Ich bekomme im Assistenten keine Übung in meinen Plan, die der gezeigte Filter ausschließt — und wenn der Bildschirm noch lädt, kann ich nichts anklicken, was gleich verschwindet." | B-169, B-171 |
| 2 | **Creator:** „Ein Tippfehler in einer Art ist wieder wegräumbar, und dass ein Fach seinem Anleger gehört, hält ein Test, der beim Gegenteil rot wird." | B-168, B-170 |
| 3 | **Entwickler:** „Ein rotes Test-Tor sagt mir, *was* schiefging — ein gesperrtes Dateihandle und ein zerrissener Inhalt sind nicht dieselbe Meldung." | B-165 |

**Warum B-171 in Sprint 1 und nicht verteilt:** Sie trägt zwei Zusicherungen, eine davon in
`assistent.spec.ts` — derselben Datei und Fläche, die Sprint 1 ohnehin anfasst. Eine Story halb zu bauen
wäre schlimmer als die kleine Unschärfe, dass ihre zweite Hälfte im Backend liegt (zwei Zeilen).

**Warum B-174 nicht dabei ist:** `unverifiziert: true` — sie ist eine Behauptung, die erst reproduziert
werden muss. Das ist Verfeinerung, nicht Bauen, und gehört nicht in einen Sprint, dessen Ziel eine Fläche
verspricht.

## Fehlerzähler

Freigabe 3: je Sprint zählt jeder von einem Review (Agent, Selbst-Check, roter Test) gefundene Fehler, dessen
Behebung **Code oder Tests** ändert. Über 5 endet der **gesamte** Lauf.

| Sprint | Zähler | Stand |
|---|---|---|
| 1 | **2 / 5** | abgeschlossen |
| 2 | **2 / 5** | abgeschlossen |
| 3 | **2 / 5** | abgeschlossen |

## Sprint 1 — Ziel erreicht

**Ziel war:** „Ich bekomme im Assistenten keine Übung in meinen Plan, die der gezeigte Filter ausschließt —
und wenn der Bildschirm noch lädt, kann ich nichts anklicken, was gleich verschwindet."

**Erreicht, live belegt.** Am Assistenten mit verzögerter Suche gemessen: im Ladefenster stehen die sechs
alten Zeilen noch da, **alle sechs gesperrt**, und „Alle wählen" ebenfalls; nach der Antwort ist die neue
Zeile bedienbar. Der Zusatz „was gleich verschwindet" war die Hälfte, die Entscheidung 1 überhaupt nötig
machte — eine Ref hätte die frischen Zeilen gesperrt gelassen.

Beide Stories `abgenommen`: [B-169](backlog/B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md),
[B-171](backlog/B-171-zwei-zusicherungen-pruefen-den-ausgangszustand.md).

### Was die Reviews gefunden haben (der Fehlerzähler im Einzelnen)

| # | Fund | ändert | zählt? |
|---|---|---|---|
| 1 | `frontend-reviewer`: „Alle wählen" ist im **Fehlerzweig** ungesichert — derselbe P1-Schaden über die Nachbartür | Code | **ja** |
| 2 | `frontend-reviewer`: die neue Spec **erbt** das Kind statt es zu wählen, und Klasse/Schulart filtern serverseitig | Test | **ja** |
| 3 | `pugling-reviewer`: der Kommentar behauptet mehr als er hält (`GetInt32()` auf `null` ist auch rot) | Kommentar | nein |
| 4 | `pugling-reviewer`: Kommentar zu lang, Umbau-Erzählung | Kommentar | nein |
| 5 | `pugling-reviewer`: B-171s Frontmatter stand auf `ausformuliert`, obwohl der Rumpf die Schätzung trug | Story | nein |
| 6 | `frontend-reviewer`: `getByLabel` ohne `{ exact: true }` | Test (Konvention, kein Defekt) | nein |
| — | voller E2E-Lauf: `bilder.spec.ts` rot | außerhalb des Diffs | nein |

Der Schnitt an „was der Fix anfasst" hat sich hier **bewährt**: Von sieben Beobachtungen sind zwei echte
Defekte, und der Zähler nennt genau die zwei. Hätte er an einer Einschätzung gehangen, wie schwer ein Fund
wiegt, wäre er bei fünf oder sechs gestanden und der Lauf beinahe beendet.

### Retrospektive Sprint 1

**Nachschau (Pflichthandlung, zuerst):** Index beim Start des Sprints **103 von 103** — die Arbeit des
vorigen Durchgangs (B-163, abgenommen am 2026-08-13) war unter den X, geprüft am 2026-08-13 mit benanntem
Prüfpunkt („der neue Wächter deckt die geseedeten Arten, nicht selbst angelegte; `POST …/categories` ist
ungegated, die Kollision ist von der anderen Seite zurückholbar — aufgefangen durch das Art-Etikett").
**Die Nachschau-Pflicht ist damit erfüllt, ohne dass ein neuer Blick nötig war.** Nach der Abnahme von
Sprint 1 steht der Index bei **103 von 105**: B-169 und B-171 sind die Arbeit, die **Sprint 2s** Retro
ansehen muss. Das ist die Kette, nicht eine Lücke.

**Was gut lief.** Die Grill-Runde hat die eigene Empfehlung der Story widerlegt, *bevor* gebaut wurde
(Ref → State). Das ist der Wert des Grillens an einer Story, die man selbst geschrieben hat: der Vorschlag
im Angriffsplan war plausibel und falsch.

**Was schiefging, und es ist derselbe Fehler zweimal.** Ich habe in *beiden* Stories eine Behauptung in
einen Kommentar geschrieben, die ich nicht geprüft hatte: „`exercises.loading` deckt dasselbe Fenster" (falsch
im Fehlerzweig) und „`GetInt32()` würde werfen statt zu vergleichen" (richtig, aber der Fall wäre auch so rot).
Beide Male hat ein Reviewer es gefunden, kein Tor. **Ein Kommentar, der eine Begründung behauptet, ist eine
Zusicherung ohne Test** — und genau die Klasse, für die diese Story-Familie überhaupt existiert.

**Vorschlag für einen Mechanismus** — nach Freigabe 3 nur vorgeschlagen, **nicht gelandet**:
Wo ein Kommentar begründet, *warum* ein Gate nur an einer Stelle sitzt, gehört die andere Stelle in dieselbe
rote Probe. Konkret: „das Nachbar-Steuerelement ist schon abgedeckt" ist eine prüfbare Aussage, und sie war
hier in einem Satz widerlegt. Ob das eine Zeile im Reviewer-Auftrag wert ist oder in die
`frontend/CLAUDE.md`, entscheidet der Nutzer — **erst messen**, wie oft ein solcher Satz vorkommt.

**Eine Beobachtung zum Verfahren selbst.** `bilder.spec.ts` fiel im vollen Lauf, und die *erhaltene* Meldung
hat B-153 von „flackert" auf eine benannte Ursache gebracht (`strict mode violation`, der Bildbezug trifft
zwei Elemente). Der Grund, dass es diesmal gelang: Ich habe den Einzellauf **nach** dem Sichern der Meldung
gefahren, nicht davor — genau der Fehler, den B-153 sich selbst am 2026-08-11 vorwarf. Der Lauf hat also eine
fremde Story mit einem Beleg bezahlt, ohne sie zu bauen.

## Sprint 2 — Ziel erreicht, aber eine Story musste geteilt werden

**Ziel war:** „Ein Tippfehler in einer Art ist wieder wegräumbar, und dass ein Fach seinem Anleger gehört,
hält ein Test, der beim Gegenteil rot wird."

**Die zweite Hälfte ist erreicht und ist die stärkste Messung des Laufs.** `OwnerAdultId = 1` hartkodiert →
**829 grün, 1 rot**, und der Rote ist genau der neue Fall. Jeder Creator außer Adult 1 wäre dauerhaft aus
seinen eigenen Fächern gesperrt worden, ohne dass ein Test es merkt.

**Die erste Hälfte ist nur zur Hälfte erreicht, und das steht so in der Story.** Der Nutzer hatte die
Schemaänderung für diesen Lauf ausgeschlossen; das freigegebene Admin-Ventil macht den Zustand *reparierbar*,
löst aber den Fall des Vaters **nicht** — kein geseedeter Erwachsener trägt das Flag. Statt das zu übergehen,
wurde [B-170](backlog/B-170-selbst-angelegte-art-im-grundbestand-ist-unloeschbar.md) **geteilt**:
[B-178](backlog/B-178-katalogtext-verspricht-mehr-als-der-server-erlaubt.md) trägt, was ohne Schema richtig
wird (Ventil + der Katalogtext), B-170 behält die Schemafrage mit `wartet_auf`.

**Der Grund für die Teilung war eine widerlegte eigene Empfehlung:** B-170s offener Punkt 4 wollte den
Vorspann-Satz „hier mitnehmen, denn nach Punkt 1 ändert sich, was wahr ist". Punkt 1 kommt nicht — also
ändert sich für den Vater nichts, und der Satz muss eine andere Wahrheit sagen als nach dem Schema-Fix. Ihn
in B-170 zu lassen hätte bedeutet, ihn zweimal zu schreiben.

### Retrospektive Sprint 2

**Nachschau (Pflichthandlung, zuerst):** Index vor der Abnahme **103 von 105** — die Arbeit von **Sprint 1**
(B-169, B-171) war *nicht* unter den geprüften, also wurde sie jetzt angesehen. Ergebnis: **kein
Produktdefekt.** Benannte Prüfpunkte, damit „sauber" nachvollziehbar ist: `geltenderFilterKey` läuft bei
leerer Auswahl nicht aus dem Takt; das Gate lässt sich ohne Batching nicht zu weit öffnen (`filterKey` kann
sich nicht ohne die `useAsync`-Deps ändern, und die Gegenrichtung ist harmlos, weil die Server-Sortierung
total ist); kein Fokusverlust durch das neue `disabled`; der `selectAll`-Fehlerzweig bleibt am
Generationen-Gate. Damit hält auch B-169s Entscheidung 4.

**Die Nachschau hat trotzdem zwei Dinge gefunden, und beide gehören mir:**

1. **Meine Zusicherung hing an 20 ms Scheduling statt am Gate** →
   [B-180](backlog/B-180-zusicherung-haengt-an-scheduling-statt-am-gate.md), sofort behoben. Das `disabled`
   des Knopfes hat drei Ursachen, und mein Fall trennte sie nicht — er wäre schon im Ladefenster wahr
   gewesen, durch genau das `exercises.loading`, von dem mein eigener Kommentar zwei Zeilen darüber sagt, dass
   es den Fehlerzweig nicht deckt. Der Reviewer hat den Puffer über acht Läufe vermessen: 15–36 ms.
   **Dieselbe Fehlerfamilie wie der Defekt, den der Fall bewachen soll** — nur im Messinstrument.
2. **Meine B-162-Korrektur war zu stark.** Ich schrieb „Dauerzustand ohne Ausweg"; es gibt drei. Der Befund
   wird dadurch nicht kleiner, sondern **schärfer**: Ausgerechnet die Bewegung, die ein Nutzer versucht
   (über den Stepper zurück und wieder vor), ist die wirkungslose — die `useAsync`-Deps ändern sich nicht,
   es fliegt keine neue Abfrage. Damit steht fest, was B-162 braucht: nicht nur ein Banner, sondern ein
   „Erneut suchen".

**Ein Beleg für eine Entscheidung, den erst die Nachschau geliefert hat.** B-169s Entscheidung 2 verwarf die
billigere Bedingung `loading && data !== null`, obwohl sie *heute* gleichwertig war — mit der Begründung, sie
ziehe zwei Situationen zusammen. Genau daran hängt jetzt B-162s Lösbarkeit: mit der verworfenen Variante
hätte ein `reload()` das Gate fälschlich geschlossen, der „Erneut suchen"-Knopf wäre also nicht baubar. Eine
Entscheidung, die zum Zeitpunkt des Treffens nur *prinzipiell* richtig war, hat sich zwei Sprints später
praktisch bezahlt.

**Vorschlag für einen Mechanismus** — nach Freigabe 3 nur vorgeschlagen, **nicht gelandet**:
Eine Zusicherung auf ein Attribut mit **mehreren** Ursachen (`disabled`, `aria-*`, `hidden`) braucht einen
Wartepunkt, der die anderen Ursachen ausschließt — sonst prüft sie nicht, was ihr Name sagt. Das ist die
Test-Variante der schon residenten Regel und in einem Halbsatz prüfbar („welche der Ursachen kann diese
Zusicherung erfüllen?"). Ob es in `frontend/CLAUDE.md` gehört, entscheidet der Nutzer.

**Eine Beobachtung an der Grenze des Fehlerzählers, ehrlich benannt.** Der Fund „die XML-Doku sagt das
Gegenteil des Verhaltens" ist ein **Vertrags**defekt mit konkretem Szenario (ein Client-Autor hält `200` für
einen Nicht-Eigentümer für unmöglich), aber sein Fix berührt nur Kommentare — nach dem Wortlaut von
Freigabe 3 zählt er also nicht, obwohl er zwei Zeilen in `docs/openapi/v1.json` bewegt hat. Ich habe mich an
den Wortlaut gehalten, statt die Regel im Lauf zu biegen. **Zur Entscheidung des Nutzers:** Soll „ändert ein
eingecheckt generiertes Artefakt" mitzählen? Der Schnitt bliebe damit ablesbar, würde aber Vertragstexte
erfassen.

## Sprint 3 — Ziel erreicht, und die Prämisse der Klasse fiel dabei

**Ziel war:** „Ein rotes Test-Tor sagt mir, *was* schiefging — ein gesperrtes Dateihandle und ein zerrissener
Inhalt sind nicht dieselbe Meldung."

**Erreicht.** [B-165](backlog/B-165-backend-suite-flackert.md) `abgenommen`. Der Zähler ist getrennt, die
Meldungen nennen alle drei Zahlen, und das Flackern ist weg — der atomare Fall fällt nicht mehr an einer
Sperre, die der Schreiber nachweislich nicht verursacht haben kann.

**Der Bau hat mehr aufgedeckt als das Ziel verlangte, und zwar gegen mich.**

**(1) Der Nachbar-Fall hat nie zerrissenen Inhalt gesehen.** Nach dem Trennen der Zähler:
**`Torn reads: 0, locked reads: 1867`**. Seine gesamte Beweiskraft kam aus Sperren. Eine eigenständige Probe
erklärte es (2351 identische Ausnahmen): `File.OpenRead` fordert `FileShare.Read`, ein offenes
Schreib-Handle widerspricht dem, Windows verweigert das Öffnen — der Leser kommt nie bis zu den Bytes. Damit
ist die **Prämisse des Klassen-Kommentars falsch** („the bug is torn/incomplete JSON content"), und weil dort
eine Verhaltensfrage dranhängt (braucht `OpenApiExampleCatalog.Load` einen Wiederholungsversuch, wenn der
reale Fehlermodus eine Sperre ist?), wurde sie **nicht** umgedeutet, sondern als
[B-181](backlog/B-181-praemisse-der-rennen-klasse-stimmt-nicht.md) abgelegt.

**(2) Meine Entscheidung 2 hat den atomaren Fall zahnlos gemacht — zweimal widerlegt, beide Male von einer
Probe.** Mit nur `zerrissen == 0` blieb er **grün, obwohl er unsicher schrieb**. Vor dieser Story fing er den
Tausch; danach nicht mehr — eine **Verschlechterung**, eingeführt von mir. Der erste Reparaturversuch
(`ok > 0`) fiel ebenfalls durch, weil `OpenForWriteWithRetry` erst um sein Handle kämpft und die Lesevorgänge
davor immer gelingen. Was trägt, ist das **Verhältnis** `ok > gesperrt`.

| Rote Probe | Ergebnis |
|---|---|
| atomarer Fall schreibt unsicher | rot: „2954 reads were denied and only 450 succeeded" |
| unsicherer Fall schreibt atomar | rot: „Locked reads: 0, torn reads: 0" |

Der beobachtete Virenscanner-Blip war genau **1** — zwei Größenordnungen Abstand zu 2954. Der Schwellwert ist
damit gemessen begründet, nicht geraten.

### Retrospektive Sprint 3

**Nachschau (Pflichthandlung, zuerst):** Index vor der Abnahme **105 von 107** — die Arbeit von **Sprint 2**
(B-168, B-178) war nicht unter den geprüften, also wurde sie jetzt angesehen. Ergebnis: **ein Defekt und ein
stiller Rest.**

- **B-178:** Die Reichweite des Ventils war nur am **PATCH** festgenagelt; eine Verengung von `Delete` allein
  wäre bei 831/831 grün geblieben → [B-182](backlog/B-182-reichweite-nur-am-patch-festgenagelt.md), behoben
  und mit **gefahrener** roter Probe belegt (14 grün, 1 rot). Der Reviewer hatte sie nicht gefahren; ich habe
  sie nachgeholt, weil eine Story, die behauptet ein Test fange etwas, das zeigen muss.
- **B-168:** Der Angriffsplan nannte *zwei* Konstanten-Vergleiche, umgesetzt wurde einer. Der zweite bleibt
  zu Recht — dort meint die `1` den *anderen* Erwachsenen, nicht den Aufrufer — aber die Story hatte sich
  geirrt, und das steht jetzt am Code, statt als erledigt auszusehen. **Kein Defekt, ein Rest, der wie
  Erfüllung aussah.**

**Das Muster dieser Nacht, in einem Satz.** Alle fünf Selbstkorrekturen sind derselbe Fehler: **ich habe eine
Begründung aufgeschrieben, ohne sie zu prüfen.** „`exercises.loading` deckt dasselbe Fenster" (falsch),
„`GetInt32()` würde werfen statt zu vergleichen" (irrelevant), „der Seed-Fall wird identitätsbasiert"
(unmöglich), „das Ventil betrifft ownerlose Fächer" (zu eng), „nur zerrissener Inhalt muss zählen"
(macht den Fall zahnlos). Vier davon fand ein Reviewer oder eine Nachschau, eine fand meine eigene Probe.
**Was in jedem einzelnen Fall gereicht hätte, war die rote Probe *vor* dem Aufschreiben der Begründung** —
nicht mehr Sorgfalt, sondern dieselbe Sorgfalt in anderer Reihenfolge.

**Vorschlag für einen Mechanismus** — nach Freigabe 3 nur vorgeschlagen, **nicht gelandet**:
Ein Kommentar, der eine Bedingung *begründet* („weil X schon abgedeckt ist", „weil Y werfen würde", „weil das
nur Z betrifft"), ist eine prüfbare Aussage und gehört in dieselbe rote Probe wie die Bedingung. Das ist die
gemeinsame Wurzel aller fünf Fälle und wäre eine Zeile in der Root-`CLAUDE.md` — **erst messen**, wie oft ein
solcher Satz vorkommt, dann entscheiden.

## Verlauf des Laufs

- **2026-08-14** — Lauf gestartet. Freigaben erteilt, Umfang entschieden, Sprint-Plan steht.
- **2026-08-14** — **Sprint 1 durch.** B-169 und B-171 von `ausformuliert` bis `abgenommen`, Fehlerzähler
  **2 von 5**. Zwei Reviewer-Funde waren echte Defekte und wurden behoben, nicht nur gemeldet. Vier
  Nebenfunde als eigene Stories bzw. Korrekturen abgelegt:
  [B-177](backlog/B-177-seitenschluessel-haengt-an-einer-batching-annahme.md) neu,
  [B-162](backlog/B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) mit korrigiertem Ist-Stand und
  P3 → P2, [B-153](backlog/B-153-bilder-spec-flackert-im-vollen-lauf.md) mit belegter Ursache und
  `idee → ausformuliert`, P3 → P2.
- **2026-08-14** — **Sprint 2 durch.** B-168 und B-178 `abgenommen`, Fehlerzähler **2 von 5**. B-170 geteilt
  und mit `wartet_auf` auf die Schemaentscheidung am Tag gesetzt. Aus Reviews und Nachschau abgelegt:
  [B-179](backlog/B-179-drei-fassungen-des-admin-testhelfers.md) (drei Fassungen des Admin-Testhelfers),
  [B-180](backlog/B-180-zusicherung-haengt-an-scheduling-statt-am-gate.md) (aufgenommen **und** gebaut),
  dazu [B-162](backlog/B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) präzisiert.
  Index: **105 von 108** nachgeschaut.
- **2026-08-14** — **Sprint 3 durch, Lauf beendet.** B-165 `abgenommen`, Fehlerzähler **2 von 5**. Abgelegt:
  [B-181](backlog/B-181-praemisse-der-rennen-klasse-stimmt-nicht.md) (die Prämisse der Rennen-Klasse stimmt
  nicht — mit Verhaltensfrage im Produktionspfad, darum nicht nachts entschieden),
  [B-182](backlog/B-182-reichweite-nur-am-patch-festgenagelt.md) (aufgenommen **und** gebaut).
  Index: **107 von 110** nachgeschaut. Der Lauf endet nicht am Fehlerzähler, sondern weil der Sprint-Plan
  durch ist.
