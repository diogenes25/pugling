---
tags: [typ/story, status/in-arbeit, bereich/qualitaet, bereich/frontend, bereich/tests]
aliases: [OpenAPI-Typen generieren, TS-Vertragstor]
status: in-arbeit
prio: P2
art: Aufräumen
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/testplan.md#nachmessung-2026-07-31-die-drei-unbeobachteten-flächen
---

# B-42 · TypeScript-Typen aus dem OpenAPI-Dokument erzeugen statt von Hand pflegen

> **Schritt 1 (E3) ist gebaut und belegt** – das vertragsreine Dokument liegt unter
> [`docs/openapi/v1.json`](../openapi/README.md), das CI-Tor steht. Siehe „Verifikation Schritt 1".
> Offen ist **Schritt 2 (E6)**: die Typen im Frontend erzeugen. Die Story bleibt deshalb `in-arbeit`.

`frontend/src/lib/types.ts` trägt **1950 handgeschriebene Zeilen** Vertrag – dieselben Felder, die
`Pugling.Contracts` besitzt und die der OpenAPI-Generator ohnehin ausgibt. Solange beide Seiten von Hand
gleichgehalten werden, findet `tsc` einen Feldumbau im Backend nur, wenn jemand die TS-Zeile mitzieht.

## User Story

Als **Entwickler**, der ein Feld im Vertrag ändert, möchte ich, dass die **Typprüfung des Frontends bricht**,
wenn ich die Oberfläche nicht nachziehe, damit ein Vertragsbruch beim Bauen auffällt und nicht als stiller
`400 unknown_field` in einer Maske landet, die „Gespeichert." meldet.

## Ist-Stand am Code

- **`types.ts`: 190 exportierte `interface`/`type`.** Gegen die **221** Records in `Pugling.Contracts`
  gezählt: **94 Namen sind identisch**, 96 TS-Typen haben keinen gleichnamigen Record (Mischung aus C#-*Enums*
  wie `Currency`/`ContentRating`/`DayOfWeek`, umbenannten Antworttypen und echt UI-eigenen wie
  `CreateExercisePayload`), und **127 Records haben keinen TS-Typ** – das Frontend deckt also rund 42 % der
  Vertragsfläche ab, bewusst.
- **Das OpenAPI-Dokument ist zur Laufzeit da und im Test schon in Gebrauch**: `ErrorCodeTests.cs:155` liest
  `/openapi/v1.json`. Erzeugt wird es vom eingebauten `AddOpenApi` (`Program.cs:276`) mit einem
  Operations-Transformer für die Beispiele und einem Schema-Transformer, der Enum-Werte in die Beschreibung
  schreibt.
- **Für „generiertes Artefakt + Diff als CI-Tor" gibt es das Muster schon.** `DocsCaptureTests` schreibt
  `docs/api-examples/` bei *jedem* Lauf (`DocsCaptureTests.cs:1096`), und `ci.yml` macht daraus Tor **D4**.
- **Nebenbefund aus dem Grillen:** `backend/Pugling.Api/OpenApi/openapi-examples.generated.json` wird vom
  selben Test geschrieben (`DocsCaptureTests.cs:1114`), ist **eingecheckt**, wird zur Laufzeit gelesen
  (`OpenApiExampleCatalog.cs:20`) und beim Publish mitkopiert (`Pugling.Api.csproj:44`) – aber das D4-Tor
  diffs nur `docs/api-examples`. **Diese Datei kann in CI still driften.**
- **Zwei generische Vertragstypen** sind der Sonderfall, den ein Generator anders abbildet als die Handarbeit:
  `ExercisePayload<TConfig>` und `ExerciseResponse<TConfig>`
  (`Contracts/Creator/ExerciseAuthoringDtos.cs:12`/`:24`).
- **CI prüft das Frontend schon** (`ci.yml`, Job „Frontend – Typecheck, Build, Vitest": `npm ci
  --legacy-peer-deps` + `npm run build` = `tsc -b && vite build`) – es fehlt nur die *Quelle* der Typen.
- Der Übungstyp-Teil ist **kein** Fall für den Generator: Übungstypen kommen laut `frontend/CLAUDE.md` aus dem
  **Server-Manifest** zur Laufzeit, nicht aus statischen Typen.

## Die echte Lücke

Sie ist schmaler als „1950 Zeilen sind Dopplung". Ehrlich gerechnet ist die driftgefährdete Fläche die der
**94 namensgleichen** Typen plus der umbenannten – dort steht dieselbe Aussage zweimal, und nur eine Seite hat
ein Tor. Die 127 Records ohne TS-Gegenstück sind **kein** Mangel, sondern die Entscheidung, dass die
Oberfläche einen Ausschnitt bedient; ein Generator muss also mehr ausgeben dürfen, als benutzt wird
(ungenutzte TS-Typen kosten nichts).

Der eigentliche Hebel ist nicht die Zeilenzahl, sondern **wo der Fehler heute auffällt**: erst in Playwright
(PR + nachts, kein Freigabe-Tor) oder im Betrieb als `unknown_field`. Mit generierten Typen wandert er in
`tsc -b` – in denselben CI-Job, der schon läuft.

## Offene Punkte

Alle im Grillen vom 2026-07-31 entschieden bzw. ausdrücklich zurückgestellt.

1. ~~Ersetzen oder gegenprüfen?~~ → Entscheidung 1
2. ~~Welcher Generator?~~ → Entscheidung 2
3. ~~Woher kommt das Dokument im Build?~~ → Entscheidung 1 (eingecheckt) und
   [B-40](B-40-client-routen-waechter.md), Entscheidung 1 (der Wächter dort bleibt bewusst lebend)
4. ~~Was passiert mit den zwei generischen Typen?~~ → **zurückgestellt**: nachsehen, bevor gebaut wird,
   nicht raten (siehe Entscheidung 5)
5. ~~Ersetzt das B-24?~~ → Entscheidung 4

## Entscheidungen

1. **Zwei Schritte, beide in dieser Story.** Schritt 1: Ein Testlauf schreibt `/openapi/v1.json` als
   eingechecktes Artefakt, `ci.yml` macht aus einem Diff ein Rot (Muster D4). Schritt 2: Das Frontend erzeugt
   daraus die Vertragstypen, `types.ts` schrumpft auf die UI-eigenen. Begründung: Schritt 1 allein fängt zwar
   *jede* unbeabsichtigte Vertragsänderung, sagt aber nur „das Dokument hat sich geändert" – nicht „und das
   Frontend hängt daran". Erst Schritt 2 bricht `tsc`, und das ist der Zweck der Story.
   **Kosten:** Schritt 2 berührt 190 Typen und potenziell viele der 83 Quelldateien – ein großer Diff, der
   einmal am Stück durchgezogen werden muss.
2. **`openapi-typescript`, nur Typen – kein generierter Client.** Begründung: `lib/api.ts` (870 Zeilen) trägt
   projekteigene Konventionen, an denen andere Bausteine hängen: `httpPaged` mit `X-Total-Count`,
   `errorMessage` über den stabilen `code` (davon lebt `useAction`), der globale 401-Abfang, der die Sitzung
   beendet. Ein generierter Client brächte das durcheinander oder müsste umhüllt werden.
   **Kosten:** eine neue devDependency, die auf den ungelösten Peer-Konflikt aus
   [B-25](B-25-vite-pwa-peer-konflikt.md) trifft – vor dem Bauen mit `npm ci --legacy-peer-deps` gegenprüfen,
   sonst bricht der CI-Job „Frontend" für alle.
3. **Das Diff-Tor deckt auch `openapi-examples.generated.json` ab** (Nebenbefund oben). Begründung: die Datei
   ist eingecheckt, wird zur Laufzeit gelesen und beim Publish mitkopiert – ein stiller Drift dort verändert
   die ausgelieferte Dokumentation. **Kosten:** eine Zeile im D4-Schritt; und ein weiterer Pfad, der bei
   Nichtdeterminismus rot wird (der Beleg für Byte-Stabilität steht aus, siehe Akzeptanzkriterium 1).
4. **[B-24](B-24-frontend-unknown-field.md) wird verkleinert, nicht verworfen.** Neuer Zuschnitt: „die
   Stellen finden, an denen Nutzlasten **untypisiert** abgeschickt werden", eingereiht hinter B-42.
   Begründung: generierte Typen fangen ein falsches Feld nur dort, wo die Nutzlast typisiert übergeben wird;
   ein Objekt-Literal mit Zusatzfeld bleibt unsichtbar. **Kosten:** eine Story bleibt offen – aber kleiner und
   gezielter, statt still zu verschwinden.
5. **Die Benennung der generischen Typen wird vor dem Bauen nachgesehen, nicht entschieden.** Fällt sie
   unbrauchbar aus, bleiben genau diese zwei von Hand – mit einem Satz, **warum**. **Kosten:** eine
   Unbekannte, die die Schätzung erst nach dem Nachsehen belastbar macht.
6. **Reihenfolge:** B-42 wird als **dritte** der vier Test-Stories gebaut, nach
   [B-41](B-41-produktions-startup-smoke.md) und [B-40](B-40-client-routen-waechter.md).

## Akzeptanzkriterien

1. `/openapi/v1.json` liegt als eingecheckte, **byte-stabile** Datei im Repo, erzeugt vom Testlauf; zwei Läufe
   hintereinander erzeugen denselben Inhalt (belegt, nicht angenommen – dieselbe Probe wie bei D4).
2. Ein CI-Schritt macht aus einem Diff an diesem Dokument **und** an `openapi-examples.generated.json` ein
   Rot, mit einer Meldung, aus der die Handlung folgt („neu erzeugen und committen").
3. Die Vertragstypen des Frontends werden aus dem Dokument **erzeugt**; `npm run build` bricht, wenn ein Feld
   im Backend umbenannt oder entfernt wurde und die Oberfläche es noch benutzt.
4. **Gegenprobe gefahren:** ein Feld in einem `Pugling.Contracts`-Record umbenennen → Backend-Suite grün (der
   Vertrag darf sich in `v1` frei ändern), Frontend-Job **rot**, Meldung nennt Datei und Feld.
5. Von Hand gepflegt bleiben nur noch die UI-eigenen Typen, in einer eigenen Datei, je Ausnahme ein Satz
   Begründung (Generika, falls nötig).
6. `npm ci --legacy-peer-deps` bleibt fehlerfrei; der Übungstyp-Weg über das Server-Manifest bleibt unberührt.
7. B-24 ist auf den neuen Zuschnitt gekürzt und verweist auf diese Story.

## Schätzung

**M**, `wo: beides`, geteilt an der eigenen Naht: **Schritt 1 (E3) = S, backend** – erledigt;
**Schritt 2 (E6) = M, frontend** – offen und hinter E4/E5.

- `migration: nein`, `vertragsbruch: nein`. Schritt 1 ändert **keinen** Vertrag, er schreibt ihn nur auf.
- **Risiko Schritt 1 war die Byte-Stabilität**, nicht das Erzeugen. Zwei Quellen von Nichtdeterminismus
  waren vorher benannt (Beispielkatalog, Naht 2) bzw. sind beim Bauen aufgetaucht (Zeilenenden in den
  Werten, siehe Verifikation).
- **Risiko Schritt 2 bleibt `required`/`nullable`** – die unvermessene Größe. Vor dem Schätzen von E6
  einmal `openapi-typescript` über das jetzt vorhandene Dokument laufen lassen und `tsc -b` die Fehler
  zählen lassen. Das ist ab sofort billig: die Datei liegt da.

**Testweg:** Schritt 1 über `ContractDocumentTests` + den CI-Schritt; Schritt 2 über `npm run build`
(`tsc -b`) und die Gegenprobe aus Akzeptanzkriterium 4.

## Verifikation Schritt 1 (E3)

Gebaut am **2026-08-01**. Das Dokument liegt als [`docs/openapi/v1.json`](../openapi/README.md)
(197 Pfade, 246 Schemata, 888 KB, 32 874 Zeilen), geschrieben bei jedem Testlauf von
[`ContractDocumentTests`](../../backend/Pugling.Api.Tests/ContractDocumentTests.cs).

| Beleg | Ergebnis |
| --- | --- |
| `dotnet test Pugling.sln -c Release` | **623/623 grün** (vorher 622) |
| **AK 1 – Byte-Stabilität** | **zwei volle Läufe, identischer SHA-256** (`856c014e…`); zusätzlich prüft der Test selbst zwei getrennte Hosts gegeneinander |
| Endpunkt-Abdeckung | 263/263, 0 offen – unverändert |
| `dotnet format Pugling.sln --verify-no-changes` | sauber |
| markdownlint | 0 Treffer |

### Gegenproben

| # | Eingriff | Reaktion |
| --- | --- | --- |
| a | Feld `Description` → `Beschreibung` in `ShopArticleDto` | Dokument bewegt sich, Diff **exakt zwei Zeilen** (`"description"` → `"beschreibung"` in `required` und `properties`) – das CI-Tor wäre rot |
| b | Zeilenenden-Normalisierung ausgebaut | **185 Zeilen mit 401 escaped `\r\n`** im Dokument (siehe unten) |
| c | Beispiele im Dokument | `Assert.DoesNotContain("\"examples\"")` im Test selbst – bliebe der Schalter wirkungslos, wäre das Dokument sofort rot statt später flappend |

### Der Fund, der das Tor bei seinem ersten CI-Lauf zerlegt hätte

Die `summary`-Felder tragen die XML-Doc-Kommentare **wörtlich**, samt ihrer Umbrüche. Unter Windows sind
das `\r\n` und landen als escaptes `\r\n` *innerhalb* der JSON-Zeichenketten; auf dem Linux-Runner checkt
git dieselben Quellen mit `\n` aus. Das Dokument hätte sich an **185 Stellen** unterschieden – und der
erste rote Lauf hätte wie eine Vertragsänderung ausgesehen, nicht wie ein Plattformunterschied.

Bemerkenswert daran: die **In-Suite-Probe fängt das nicht**. Sie vergleicht zwei Hosts derselben Maschine,
und beide haben `\r\n`. Eine Byte-Stabilitätsprobe belegt nur Stabilität *innerhalb* einer Plattform –
den Rest muss man wissen. Dieselbe Fehlerklasse hatte D4 schon einmal (`Environment.NewLine` in
`Truncate()`), was die Regel bestätigt: **Zeilenenden explizit, nie über die Plattform.**

Derselbe Gedanke gilt für einen zweiten Vektor, den der Review benannt hat und den **nur** die zwei vollen
Läufe schließen: die zwei Hosts der In-Suite-Probe teilen den **Prozess** und damit den Seed des
randomisierten String-Hashings. Hinge irgendetwas im Dokument an einer Hash-Reihenfolge, wären zwei Hosts
einer Meinung und zwei Prozesse nicht. Von den drei Belegen ist der prozessübergreifende also der tragende –
die anderen zwei sind Bequemlichkeit.

### Was der Review geändert hat

Fünf Punkte übernommen, zwei begründet abgelehnt:

- **Der Test schrieb erst nach der Stabilitätszusicherung.** Fiel sie, stand der Entwickler ohne Artefakt da
  – und mit einem xUnit-String-Diff über 900 KB, der nichts zeigt. Jetzt wird zuerst geschrieben, und bei
  Abweichung landet das zweite Dokument als `v1.second-host.json` daneben, damit `git diff` die Differenz
  benennt.
- **`git diff --exit-code` sieht neu angelegte Dateien nicht** – beide Tore (das neue **und** D4, wo die
  Lücke seit immer bestand) prüfen jetzt `git status --porcelain`. Ein zusätzliches Artefakt hätte das Tor
  **still grün** gelassen.
- **`schema.Required` kam aus einem `HashSet`** – dessen Enumerationsreihenfolge ist ein
  Implementierungsdetail, keine Zusage. Vierzig Zeilen weiter unten begründet dieselbe Datei beim
  Tag-Transformer ausdrücklich das Gegenteil („SortedSet instead of HashSet: its enumeration order is
  contractually the comparer order"). Jetzt `SortedSet` mit `StringComparer.Ordinal`; die einmalige
  Neuerzeugung hat 482 `required`-Einträge umsortiert.
- **`Seed:Enabled=false`** für den Vertragshost – das Dokument hängt an keiner Datenzeile.
- **`linguist-generated=true`** für beide erzeugten Artefakte, damit die PR-Ansicht bei einer
  Vertragsänderung benutzbar bleibt. Lokal zeigt `git diff` weiter alles.

**Abgelehnt, mit Grund:**

- **Pfade im Test umsortieren** (als Versicherung gegen einen künftigen Generator-Patch, der die
  Entdeckungsreihenfolge ändert). Das Artefakt soll **spiegeln, was der Server ausliefert**; eine eigene
  Sortierung wäre eine stille Abweichung. Ändert der Generator die Reihenfolge, ist genau **ein** roter Lauf
  mit einer Neuerzeugung der richtige Preis – ein sichtbares Ereignis ist besser als eine verborgene
  Umschreibung. (Bei `required` war es umgekehrt: dort hatte die Datei die Regel schon selbst aufgestellt.)
- **Die Beispiele im Test aus dem JSON herausschneiden** statt eines Schalters im Produktivcode. Das wären
  vier Zeilen weniger Produktivfläche, macht aber die Selbstprüfung „enthält keine Beispiele"
  gegenstandslos – sie *verdeckte* die Beispiele, statt sie nicht zu erzeugen. Der Schalter ist zudem der
  einzige Weg: `OpenApiOptions` legt die Transformer-Liste nicht offen, ein Transformer lässt sich von
  außen nicht abmelden.

**Zwei vorbestehende Befunde** sind als eigene Ideen abgelegt statt hier mitgenommen:
[B-56](B-56-problemdetails-required-extensions.md) (`ProblemDetails` fordert `extensions`, ein Feld, das
nicht in `properties` steht – ein Nebengewinn des Tors: solche Merkwürdigkeiten sind jetzt sichtbar) und
[B-57](B-57-beispielkatalog-schreib-lese-rennen.md) (dieselbe Katalogdatei wird im selben Lauf gelesen und
geschrieben).

### Kosten, die bewusst in Kauf genommen sind

- **888 KB generiertes JSON im Repo**, das sich bei jeder Vertragsänderung bewegt. Der Diff ist dafür
  lesbar (eingerückt, eine Eigenschaft je Zeile) – eine Minified-Zeile über ein Megabyte hätte dem
  Reviewer nichts gesagt.
- **`servers` trägt `http://localhost/`**, die Adresse des Testhosts. Deterministisch, also unschädlich
  fürs Tor, aber das Dokument taugt nicht als Client-Konfiguration ohne eigenen Server-Eintrag.
- **Ein Schalter mehr im Produktivcode** (`OpenApi:ExamplesEnabled`, Vorgabe `true`). Er existiert nur für
  dieses Artefakt; im Betrieb ändert sich nichts.

## Verlauf

- **2026-07-31** — angelegt (Quelle: Nachmessung der Test-Abdeckung, [testplan.md](../testplan.md)).
- **2026-07-31** — ausformuliert: gezählt statt geschätzt (190 TS-Typen, 221 Records, **94 namensgleich**,
  127 Records ohne TS-Gegenstück).
- **2026-07-31** — gegrillt: sechs Entscheidungen, die Generika-Benennung zurückgestellt. Beim Nachsehen fiel
  auf, dass `openapi-examples.generated.json` zwar eingecheckt, aber **nicht vom D4-Tor gedeckt** ist – das
  geht als Entscheidung 3 mit.
- **2026-08-01** — ins [Testabdeckungs-Paket](../testabdeckung-plan.md) aufgenommen, **an der eigenen Naht
  geteilt**: Schritt 1 wird **E3** (Backend), Schritt 2 wird **E6** (Frontend, hinter E5). Drei Änderungen
  aus der Dev-Runde, alle in der Story wirksam:
  1. **Entscheidung 3 gekippt.** Das eingecheckte Dokument wird **vertragsrein** erzeugt (Beispielkatalog
     übersprungen), weil es sonst nicht byte-stabil ist: `Program.cs:279` lädt den Katalog beim Hoststart aus
     dem Quellbaum, `DocsCaptureTests.cs:1105` schreibt ihn im selben Lauf neu, und xUnit gibt keine
     Reihenfolge her – **Akzeptanzkriterium 1 wäre heute unerfüllbar.** `openapi-examples.generated.json`
     kommt stattdessen ins bestehende D4-Tor: zwei Tore, nicht drei.
  2. **Der große Diff ist keiner.** `types.ts` hat null Laufzeit-Exporte, alle 54 Importe sind `import type`
     – die Datei bleibt **Barrel**, die Deklarationen werden scheibenweise durch `S["…"]`-Aliase ersetzt, die
     Konsumenten bleiben unberührt. Akzeptanzkriterium 5 bleibt damit Pflicht, aber teilbar.
  3. **Drei Hand-Ausnahmen statt zwei**, und die teuerste war nicht bekannt: neben den Generika bricht
     `[Flags] SchoolTypes` (`Contracts/Common/LearnBaseTypes.cs:8-9`; das Frontend führt es bewusst als
     `string`, `ExerciseEditModal.tsx:102`/`:142`), und die **unvermessene** Größe ist `required`/`nullable`
     (`Program.cs:297-303`) – vor E6 einmal generieren und `tsc`-Fehler zählen. Entlastend: Enums überleben
     als String-Literal-Unions (`Program.cs:284-295`), und `openapi-typescript` hat mit dem Peer-Konflikt aus
     [B-25](B-25-vite-pwa-peer-konflikt.md) nichts zu tun (Peer ist nur `typescript ^5.x`).
- **2026-08-01** — geschätzt (M, geteilt: S backend / M frontend) und **Schritt 1 gebaut**. Das vertragsreine
  Dokument liegt unter [`docs/openapi/v1.json`](../openapi/README.md), zwei CI-Tore stehen. Der teuerste Fund
  war keiner aus der Story: die `summary`-Felder tragen die XML-Docs wörtlich, also unter Windows mit `\r\n`
  **innerhalb** der JSON-Zeichenketten – das Tor wäre bei seinem ersten Lauf auf dem Linux-Runner an 185
  Stellen rot gewesen und hätte wie eine Vertragsänderung ausgesehen. Der `pugling-reviewer` hat danach fünf
  Punkte beigetragen, darunter zwei echte Mängel (der Test schrieb erst nach der Zusicherung; `git diff` sieht
  neue Dateien nicht) und einen Selbstwiderspruch der Datei (`HashSet` für `required`, während der
  Tag-Transformer vierzig Zeilen weiter `SortedSet` begründet). Abgespalten: [B-56](B-56-problemdetails-required-extensions.md),
  [B-57](B-57-beispielkatalog-schreib-lese-rennen.md). **Schritt 2 (E6) bleibt offen**, die Story deshalb `in-arbeit`.
