---
tags: [typ/story, status/gegrillt, bereich/qualitaet, bereich/frontend, bereich/tests]
aliases: [OpenAPI-Typen generieren, TS-Vertragstor]
status: gegrillt
prio: P2
art: Aufräumen
quelle: docs/testplan.md#nachmessung-2026-07-31-die-drei-unbeobachteten-flächen
---

# B-42 · TypeScript-Typen aus dem OpenAPI-Dokument erzeugen statt von Hand pflegen

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

## Verlauf

- **2026-07-31** — angelegt (Quelle: Nachmessung der Test-Abdeckung, [testplan.md](../testplan.md)).
- **2026-07-31** — ausformuliert: gezählt statt geschätzt (190 TS-Typen, 221 Records, **94 namensgleich**,
  127 Records ohne TS-Gegenstück).
- **2026-07-31** — gegrillt: sechs Entscheidungen, die Generika-Benennung zurückgestellt. Beim Nachsehen fiel
  auf, dass `openapi-examples.generated.json` zwar eingecheckt, aber **nicht vom D4-Tor gedeckt** ist – das
  geht als Entscheidung 3 mit.
