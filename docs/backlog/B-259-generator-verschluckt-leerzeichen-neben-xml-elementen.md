---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/doku]
aliases: [Leerzeichen neben c und b verschluckt, list-Ende klebt am Folgetext]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer zu Sprint 8 (vierter Nachtlauf 2026-10-23, Nit 5)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-03"
entgangen_bei: []
---

# B-259 · Der XML-Doc-Generator verschluckt Leerzeichen zwischen zwei Elementen

Gefunden beim Review von Sprint 8 (B-147/B-138), aber älter als dessen Diff: zwischen zwei aufeinanderfolgenden
Elementen geht das Leerzeichen verloren – `<b>PATCH semantics:</b> <c>null</c>` wird zu
``**PATCH semantics:**`null` `` (`UpdateMyAccountDto`), und ein `</list>` klebt am Folgetext
(`used?**Children** are…`, Quelle `ExerciseCatalogDtos.cs:68-70`). Der `OpenApiProseNormalizer` kann das nicht
reparieren: das Leerzeichen ist schon weg, wenn er läuft. Zu klären beim Ausformulieren: wie viele Stellen es
gibt (ein Prüfskript über `docs/openapi/v1.json`), ob ein Umformulieren der Quelle genügt (Text statt zweier
Elemente in Folge) oder ob ein Tor die Form in der Quelle verbieten soll. **Bewusst nicht in Sprint 8
mitgenommen:** außerhalb des Diffs, und das Sprint-Ziel (keine Einrückung, keine rohen Tags) ist ohne es erfüllt.

## User Story

Als **Entwickler** und als **Creator**, der die API über Swagger/Scalar liest, möchte ich, dass zwei hervorgehobene oder als
Code gesetzte Wörter im Vertragsdokument durch ein Leerzeichen getrennt bleiben, damit Sätze wie „**PATCH semantics:** a
`null` means …" lesbar sind.

## Ist-Stand am Code (Lauf Q, Stand `a25a1a6`)

- **Gemessen an den Rohstrings des Generators** (Normalisierer einmal abgeschaltet, `ContractDocumentTests` schrieb das rohe
  Dokument): ein Textknoten, der nur aus Leerraum besteht, fällt weg – `…being used?<b>Children</b>` (das `</list>` davor
  und der Zeilenumbruch sind weg). `OpenApiProseNormalizer` (`backend/Pugling.Api/OpenApi/OpenApiProseNormalizer.cs`)
  sieht das Leerzeichen nie.
- **Fünf Stellen im Dokument**: `UpdateMyAccountDto` und `UpdatePositionDto` („**PATCH semantics:**`null`"),
  `TeacherAccountResponse.roles` („**only**`Creator`"), `GrammarTopicResponse` („OwnerAdultId`null`"),
  `UsageResponse.otherLearnersCount` („used?**Children**").
- In den Quellen, deren Doku ins Dokument fließt (`Pugling.Contracts`, `Pugling.Api/Controllers`), **acht** solche Stellen;
  drei davon in Controllern (zwei davon heute nicht im Dokument; `RemarksController` GetOne steht als `summary` darin, die Hervorhebung fiel dort schon weg, das Leerzeichen blieb).

## Die echte Lücke

Kein Weg im Normalisierer – die Information ist vor ihm verloren. Nur die Quelle kann es verhindern, und nichts hält sie.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Die acht Stellen umformulieren** (ein Wort dazwischen, ein Tag weniger, oder Reihenfolge getauscht). *Kosten:* eine
   Hervorhebung fällt zweimal weg („only", „without").
2. **Ein Tor in `ConventionGuardTests`** (`Doku_Stellt_Zwei_Elemente_Nie_Nur_Durch_Leerraum_Getrennt`) verweigert die Form in
   genau diesen Quellen, mit Selbsttest je erkannter Form. *Kosten:* ein Zeilen-Scan, kein XML-Parser; rechts zählen nur
   Inline-Tags, Blockelemente (`</para>`, `</item>`) davor sind erlaubt.

## Akzeptanzkriterien

1. Im Vertragsdokument klebt keine der fünf Stellen mehr.
2. Eine neue Stelle in Contracts oder Controllern macht das Tor rot, mit Datei und Zeile.

## Schätzung

`S`, backend (Doku + Tor). Testweg: `ConventionGuardTests`, `ContractDocumentTests` (Diff von `docs/openapi/v1.json`).

## Verlauf

- **2026-10-23** — angelegt aus dem Review von Sprint 8 im vierten Nachtlauf; ungeprüft.
- **2026-11-03** — Lauf Q, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe: das neue Tor
  meldet **8 Stellen** (erwartet: die fünf im Dokument nachgewiesenen plus Controller – gemessen 8), Selbsttests **5/5**. Nach
  dem Umformulieren grün; im Dokument alle fünf getrennt (nachgelesen: „**PATCH semantics:** a `null`", „only `Creator`",
  „OwnerAdultId is `null`", „used?\nIt counts **children** (nach dem Review so formuliert)"). Mutanten am Scan: „schließende Tags nicht gesehen"
  **2 failed**, „selbstschließende nicht gesehen" **1 failed**, „nur Leerzeichen, kein Umbruch" **1 failed**. Nebenfund beim
  Lesen der Rohstrings: in Listenpunkten fallen `<c>`/`<b>` ganz weg → [B-361](B-361-listenpunkte-verlieren-inline-markup.md).
- **2026-11-03** — Review `pugling-reviewer`: kein 🔴/🟡 (Prüfpunkte im Protokoll); 🟢 ohne Zählung behoben bzw. benannt. Abnahme Lauf Q, Teil 1: volle Suite **1056/1056**, Solution-Build ohne Warnung, E2E-Gesamtlauf **107 passed** (`exit=0`, nach dem letzten Edit). Rollengang: Creator – sein Weg ist das Vertragsdokument, jede geänderte Zeile von `docs/openapi/v1.json` nachgelesen; Vater und Sohn Regressionszeugen (nur Doku-Text und ein Normalisierer, kein Pfad von ihnen; ihre Specs grün). **Abgenommen**, Commit siehe Protokoll ([Lauf Q](../pm-sitzung-2026-11-03-nacht.md)).
- **2026-11-03** — Nachschau in Lauf Q, Teil 2 (frischer `pugling-reviewer`, `git show 1067b55`, read-only): **Entgleitung** → [B-366](B-366-klebestellen-tor-nennt-ungepruefte-zeile.md) (🟡: die Zeile der Meldung trug keine Zusicherung). Sonst geprüft: Diff von `v1.json` genau 14 Zeilen (10 × B-260, 4 × B-259), keine weitere Klebestelle im Dokument; „8 Stellen", „fünf Klebestellen" stimmen; Tags mit Attributen links über Fall 2 gedeckt. Zwei Sätze berichtigt (🟢).
