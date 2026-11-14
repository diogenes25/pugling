---
tags: [typ/story, status/geschaetzt, bereich/backend]
aliases: [Tor null-Element je Body-Liste]
status: geschaetzt
prio: P2
art: Aufräumen
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-y
grund: ""
ersetzt_durch: []
---

# B-410 · Reflexiver Wächter: jede Liste in jedem `[FromBody]`-Graphen meldet ein `null`-Element als 400

Retro-Vorschlag (BR), entschieden am 2026-11-07 als Tor-Story. `NullElementGuardFilter`
(`backend/Pugling.Api/Errors/NullElementGuardFilter.cs`) soll seit B-397 jedes `null`-Element einer nicht-nullbaren Liste
mit `400 validation_error` beantworten. Die Regressionsfälle in `UnknownReferenceCodeTests.cs` decken aber nur die Formen ab,
die beim Bauen eingefallen sind. Die Listen im Body selbst kamen erst durch das Review dazu (Lauf Y, Sprint 12). „Jede Liste"
hängt also an einer Vorstellung, nicht an einer Aufzählung.

Ein reflexiver Test soll alle Actions mit `[FromBody]` aufzählen, im Typgraphen jede Liste finden (auch den Body selbst),
dort ein `null`-Element einsetzen und `400` mit dem Pfad in `errors` erwarten. Der Filter dokumentiert eine Ausnahme
(`List<List<T>>` wird nicht durchsucht, „no contract has one"). Der Wächter soll genau das ebenfalls festhalten.

**Erst messen** (`CLAUDE.md` → „Neue Regel scharf stellen? Erst messen"): Beim Ausformulieren zuerst zählen, wie viele
Listen der Wächter heute findet und ob eine davon rot wäre. Erst danach entscheiden, ob er scharf gestellt wird oder eine
Ausnahmeliste mit Grund bekommt. Ein neues Tor beendet den Lauf (`docs/backlog/README.md`, Limit 2).

## User Story

Als **Entwickler** möchte ich, dass ein Wächter jede Liste in jedem Request-Body aufzählt und ein `null`-Element dort als `400`
erwartet, damit „jede Liste" eine Aufzählung ist und eine neue Liste in einem neuen DTO nicht wieder als `500` beim Kind oder Vater
landet.

## Ist-Stand am Code (nachgeprüft 2026-11-13)

- Der Filter ist global registriert ([Program.cs:37](../../backend/Pugling.Api/Program.cs#L37)) und läuft vor
  `ModelStateInvalidFilter` (`Order -2001`). Er arbeitet **wertgetrieben**: er läuft die tatsächlich gebundenen Body-Werte ab
  ([NullElementGuardFilter.cs:35-46](../../backend/Pugling.Api/Errors/NullElementGuardFilter.cs#L35)), nur über Typen aus dem
  Vertragsprojekt (`ContractsAssembly`, `:27`), bis Tiefe 16 (`:25`).
- [NullElementGuardFilter.cs:19](../../backend/Pugling.Api/Errors/NullElementGuardFilter.cs#L19) dokumentiert die eine Ausnahme:
  `List<List<T>>` wird nicht durchsucht („no contract has one").
- `UnknownReferenceCodeTests.cs` trägt 12 Fälle (`[Fact]`/`[Theory]`) – die Formen, die beim Bauen und im Review auffielen.
- Nur **5** Actions tragen ein ausdrückliches `[FromBody]` (`grep` über `Controllers/`). Das ist **nicht** die Zahl der
  Body-Actions: `[ApiController]` leitet komplexe Parameter ohne Attribut als Body ab. 116 `[HttpPost|Put|Patch]` stehen im Quelltext,
  und die generische `ExerciseControllerBase<TConfig>` vervielfacht ihre `POST`/`PUT` je Übungstyp. Der Wächter muss darum über den
  `ApiExplorer` (`BindingSource.Body`) aufzählen, nicht über das Attribut.
- Grob ~61 Listen-Eigenschaften in `*Dtos.cs`/`*Configs.cs` des Vertrags (Regex über Typnamen, zählt auch Response-DTOs mit).
- Vorbild für einen reflexiven Wächter: `ConventionGuardTests.cs` (Typ-Aufzählung über die Assemblies, `:217`, `:255`).

## Die echte Lücke

Die Regel des Filters ist allgemein, ihr Nachweis ist es nicht: 12 handverlesene Fälle gegen eine Fläche von über hundert
Body-Actions. Eine Liste, die der Filter nicht erreicht (z. B. hinter einem Typ außerhalb des Vertragsprojekts oder jenseits der
Tiefe), fällt heute niemandem auf.

## Offene Punkte

1. ~~Messen: wie viele Listen sind heute rot?~~ → Entscheidung 1 (die Messung ist Schritt 1 des Baus)
2. ~~Scharf stellen oder Ausnahmeliste?~~ → Entscheidung 2
3. ~~HTTP-Durchlauf oder Filter direkt aufrufen?~~ → Entscheidung 3
4. ~~Wann im Lauf?~~ → Entscheidung 4

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
Die Tor-Story selbst entschied der Nutzer am 2026-11-07 („Ja, wie empfohlen").

1. **Gemessen wird mit dem Wächter selbst, als erster Bauschritt.** Er läuft zuerst im Melde-Modus (sammelt alle Fundstellen, schlägt
   noch nicht fehl) und schreibt Zahl der Body-Typen, Zahl der Listen und die roten Pfade in die Story. Begründung: eine Messung ohne
   den Test wäre ein zweiter Wegwerf-Test mit derselben Logik. *Kosten:* das Grillen entscheidet über eine Regel, deren Trefferzahl
   noch nicht vorliegt – darum Entscheidung 2 als Regel, nicht als Ergebnis.
2. **Regel nach der Messung:** Ein roter Pfad, den der Filter wegen einer allgemeinen Lücke verfehlt (Typ außerhalb von
   `ContractsAssembly`, Tiefe), wird **im Filter** behoben, nicht ausgenommen. Eine bewusste Ausnahme (`List<List<T>>`, `:19`) kommt
   in eine Ausnahmeliste mit Grund im Wächter, und der Wächter wird rot, sobald ein Vertrag doch eine solche Liste trägt. Sind es mehr
   als **drei** verschiedene Ursachen, hält die Story an: jede Ursache wird eine eigene `Defekt`-Story, der Wächter landet mit
   Ausnahmeliste. *Kosten:* im schlechtesten Fall Teilen statt Liefern.
3. **Der Wächter ruft den Filter direkt auf, nicht über HTTP.** Er zählt die Body-Parameter über `IApiDescriptionGroupCollectionProvider`
   der laufenden Test-App auf, baut je Typ per Reflexion eine Instanz, setzt in **jede** nicht-nullbare Liste (auch den Body selbst)
   ein `null`-Element und erwartet einen ModelState-Fehler mit genau diesem Pfad. Begründung: ein HTTP-Durchlauf bräuchte je Action
   gültige Fachdaten (Reihe, Übung, Kind …) und prüfte dann die Fachlogik mit. Der Filter ist wertgetrieben und läuft vor jeder Action,
   die Aussage „400 mit Pfad" folgt aus dem ModelState. Die bestehenden 12 HTTP-Fälle bleiben als Durchstich. *Kosten:* der Wächter sieht
   nicht, ob `System.Text.Json` ein `null`-Element überhaupt bis zum Filter durchlässt – das halten die HTTP-Fälle.
4. **Der Wächter ist ein neues Tor und kommt als letzte Story eines Sprints** (README, Limit 2: ein neues Tor beendet einen autonomen
   Lauf). *Kosten:* keine Story dieses Sprints nach ihm.

## Akzeptanzkriterien

1. Der Wächter zählt die Body-Parameter über den `ApiExplorer` auf und meldet ihre Zahl; sie ist größer als die 5 `[FromBody]`-Treffer
   (Zusicherung gegen das Zählen über das Attribut).
2. Für jede nicht-nullbare Liste im Graphen jedes Body-Typs (Tiefe wie im Filter) liefert der Filter einen ModelState-Fehler mit dem
   Pfad dieser Liste; jeder Verstoß steht mit Typ und Pfad in der englischen Fehlermeldung des Tors.
3. Eine nullbare Elementliste (`List<T?>`) erzeugt **keinen** Fehler.
4. Die Ausnahme `List<List<T>>` steht mit Grund in einer Ausnahmeliste; ein Vertrag, der eine solche Liste trägt, macht das Tor rot.
5. Messergebnis (Body-Typen, Listen, rote Pfade vor dem Fix) steht im `## Verlauf`.

## Schätzung

**Größe: M** – reflexives Instanziieren von Records mit verschachtelten Graphen (Positionsparameter, generische `ExercisePayload<TConfig>`,
Polymorphie in Configs) ist der teure Teil; dazu der Melde-Modus und die Ausnahmeliste. `migration: nein`, `vertragsbruch: nein`
(kein Vertrag ändert sich; ein Filter-Fix ändert nur, dass ein `500` zu einem `400` wird).

**Risiken**

- **Instanz-Bau:** Records ohne parameterlosen Konstruktor brauchen `RuntimeHelpers.GetUninitializedObject` plus Setzen der
  `init`-Properties; abstrakte/polymorphe Typen brauchen eine konkrete Wahl. Eine nicht baubare Form muss rot melden, nicht still
  übersprungen werden – sonst ist der Wächter an genau der Stelle blind, die er bewachen soll.
- **Leere Zusicherung:** Findet der Wächter null Listen (Aufzählung kaputt), ist er grün und wertlos. AK 1 und eine Mindestzahl an
  Listen sichern das ab.
- **Messung kann teilen** (Entscheidung 2): mehr als drei Ursachen → Story hält an.
- Der Wächter läuft in der vollen Suite; die Kosten müssen im Rahmen der bestehenden Guards bleiben (Reflexion einmal je Typ).

**Angriffsplan (Backend zuerst)**

1. Wächter im Melde-Modus in `ConventionGuardTests.cs` (oder einer eigenen Guard-Datei daneben), Messung in den Verlauf.
2. Rote Pfade nach Entscheidung 2 behandeln (Filter-Fix oder Ausnahme mit Grund).
3. Scharf stellen; je Zusicherung die rote Probe (siehe Testweg).
4. Eintrag ins Tor-Inventar `docs/codequalitaet-gates-plan.md`.

**Testweg**

- `backend/Pugling.Api.Tests/ConventionGuardTests.cs`, neuer Fall `Jede_Body_Liste_Meldet_Ein_Null_Element` (AK 1–4). Rote Proben:
  Aufzählung über `[FromBody]` statt `ApiExplorer` → AK 1 rot; im Filter die Body-Liste selbst überspringen (`elementNotNull` auf `false`,
  `NullElementGuardFilter.cs:42-43`) → AK 2 rot an den Batch-Bodies; `ElementNotNull` ignoriert die Nullability → AK 3 rot; ein
  Test-Vertrag mit `List<List<string>>` bzw. das Entfernen der Ausnahme → AK 4 rot.
- Bestehend: `UnknownReferenceCodeTests.cs` (12 Fälle) bleibt als HTTP-Durchstich.

## Verlauf

- **2026-11-07** — angelegt aus Retro-Vorschlag (BR); Entscheidung des Nutzers: „Ja, wie empfohlen" (Tore als Stories, nicht direkt landen).
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): Ist-Stand grob belegt; Falle „`[FromBody]` zählt nur 5" festgehalten; Messung als erster Schritt des Grillens.
- **2026-11-13** — Ist-Stand nachgeprüft (Backlog-Lauf): Filter wertgetrieben, nur `ContractsAssembly`, Tiefe 16; User Story, Lücke und
  AKs ergänzt, `unverifiziert` entfernt.
- **2026-11-13** — **gegrillt**: Entscheidungen 1–4; die Messung wandert in den ersten Bauschritt, die Regel für ihr Ergebnis steht fest.
  Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — **geschaetzt**: M (über dem Ziel XS/S des Sprints), backend, keine Migration, kein Vertragsbruch; als neues Tor letzte
  Story eines Sprints.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt – Filter global (`Program.cs:37`), Ausnahme `List<List<T>>` (`NullElementGuardFilter.cs:19`), Tiefe 16 (`:25`), Body-Schleife `:37-47`; weiter 12 Fälle in `UnknownReferenceCodeTests.cs`, 5 ausdrückliche `[FromBody]`; ein Wächter über alle Body-Listen fehlt in `ConventionGuardTests.cs`.
