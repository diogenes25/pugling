---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/vertrag, rolle/creator]
aliases: [isMine vs isOwn, Eigentums-Flag heißt zweimal anders]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: beides
migration: nein
vertragsbruch: ja
quelle: B-154 (Nachtlauf 2026-08-12, Entscheidung 5)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-156 · Dasselbe Eigentums-Flag heißt im Vertrag einmal `isMine` und siebenmal `isOwn`

`SubjectResponse` trägt seit [B-13](B-13-fach-kapitel-eigentum.md) ein `IsMine`. Jedes andere
Eigentums-tragende DTO im Vertrag nennt dasselbe Konzept `IsOwn`. Ein Client, der beide liest, muss sich
den Unterschied merken, obwohl es keinen gibt.

## User Story

Als *Frontend-Entwickler* möchte ich das Eigentums-Flag überall gleich benannt finden, damit ich beim
Schreiben einer Bedingung nicht nachsehen muss, welche der beiden Schreibweisen diese Ressource benutzt.

## Ist-Stand am Code

- **Einmal `IsMine`:** `backend/Pugling.Contracts/Creator/CatalogDtos.cs:19` (`SubjectResponse`).
- **Siebenmal `IsOwn`:** `CreatorProfileDtos.cs:10`, `ExerciseAuthoringDtos.cs:27`,
  `ExerciseCatalogDtos.cs:15`, `ExerciseCatalogDtos.cs:28`, `TextbookSeriesDtos.cs:14`,
  `RemarkDtos.cs:100`, `RemarkDtos.cs:126`.
- **B-13 wollte es anders.** Sein Akzeptanzkriterium 5 verlangt `isMine` ausdrücklich „analog
  `TextbookSeriesResponse`" — und genau der heißt `IsOwn`
  (`TextbookSeriesDtos.cs:14`). Der Ausreißer ist also ein Versehen im Vollzug einer Regel, die das
  Gegenteil sagte, kein bewusster Schnitt.
- **Die Kosten sind heute minimal.** Im Frontend liest **kein** Produktionscode `isMine`; die einzigen
  Treffer sind drei Test-Fixtures (`SeriesForm.test.tsx:17-18`, `TextbookForm.test.tsx:19-20`,
  `VaterFachlehrer.test.tsx:12`) plus die generierte `contract.ts:25324`. `Pugling.Client` liest es nicht.
  Ab [B-154](B-154-katalogseite-bietet-fremde-faecher-zum-umbenennen.md) kommt der erste echte Leser hinzu.
- **`IsOwner` ist ein dritter, eigener Begriff und NICHT betroffen:** `ExerciseAuthoringDtos.cs:27` und
  `ExerciseCatalogDtos.cs:15,28` tragen `IsOwn` **und** `IsOwner` nebeneinander — dort heißt `IsOwn`
  „darf schreiben (Owner oder Write-Grant)" und `IsOwner` „ist Owner". Eine Umbenennung darf die zwei
  nicht verschmelzen.

## Die echte Lücke

Kein Verhalten ist falsch — beide Felder liefern, was sie versprechen. Die Lücke ist, dass eine
Namenskonvention an genau einer Stelle bricht, und zwar an der **jüngsten**: das Zeitfenster, in dem die
Umbenennung fast kostenlos ist (ein Leser, drei Fixtures), schließt sich mit jedem weiteren Client, der
`isMine` liest.

## Offene Punkte

1. **Umbenennen oder stehen lassen?** Empfehlung: umbenennen, und zwar bald — die Kosten steigen monoton
   mit der Zahl der Leser, und die Konvention ist 7:1 eindeutig.
2. **Trägt die Umbenennung ein `vertragsbruch: ja`?** Empfehlung: ja, formal — ein Feld ändert seinen
   Namen, `UnmappedMemberHandling.Disallow` lässt Altes nicht durch. Faktisch bricht sie niemanden außer
   den drei Fixtures und B-154s neuem Code, weil es keinen externen Konsumenten gibt. Beides gehört in die
   Schätzung, nicht nur die beruhigende Hälfte.
3. **Reicht die Umbenennung, oder braucht es ein Tor?** Diese Konvention hing bisher an Disziplin, und sie
   ist genau einmal gebrochen worden. Empfehlung: ein `ConventionGuardTests`-Fall, der ein Vertrags-Feld
   namens `IsMine` (oder allgemeiner: ein `bool`-Feld, dessen Name auf ein Eigentumsverhältnis zeigt, außer
   der Positivliste `IsOwn`/`IsOwner`) rot meldet — nach der Regel „mechanische Tore statt Disziplin", und
   erst messen, ob die Regel trennscharf formulierbar ist.
4. **Zieht `Pugling.Client` mit?** Zu prüfen beim Ausformulieren der Schätzung: er liest das Feld heute
   nicht, aber der Routen-Wächter vergleicht gegen das OpenAPI-Dokument.

## Akzeptanzkriterien

1. `SubjectResponse.IsMine` heißt `IsOwn`; die Bedeutung („der aufrufende Creator ist Eigentümer; ein
   ownerloses Fach ist niemandes") bleibt unverändert, XML-Doc nachgezogen.
2. `SubjectsController`s drei Projektionen liefern das Feld unter dem neuen Namen.
3. Die drei Test-Fixtures und der Leser aus B-154 sind nachgezogen; `npm run build` grün.
4. `IsOwner` bleibt, wo es steht, und wird nicht mit `IsOwn` verschmolzen.
5. Das OpenAPI-Dokument wird neu geschrieben (`DocsCaptureTests`), `docs/api-examples` byte-stabil.
6. Falls Punkt 3 der offenen Punkte mit „ja" entschieden wird: ein Wächter, der den Rückfall rot meldet.

## Entscheidungen

Autonom gegrillt im vierten Nachtlauf (`art: Aufräumen`, Freigabe 1).

1. **Umbenennen, jetzt.** Die Konvention steht 8 : 1, und die Kosten wachsen mit jedem Leser; heute sind es ein
   Produktionsleser (`CatalogAdmin.tsx:244`) und Fixtures in vier Testdateien. *Kosten:* ein Vertragsbruch
   (siehe 2) und ein Diff über Backend, Frontend und Doku.
2. **`vertragsbruch: ja`, formal und ehrlich.** `UnmappedMemberHandling.Disallow` betrifft nur Eingaben; das
   Feld ist eine Antwort, ein alter Client liest also still `undefined` statt eines Fehlers – genau darum zieht
   das Frontend im selben Commit mit. Externe Konsumenten gibt es keine. *Kosten:* wer einen alten Stand des
   Frontends gegen den neuen Server fährt, sieht an keinem Fach Knöpfe.
3. **Kein eigenes Tor.** Ein Wächter gegen genau ein Wort (`IsMine`) fängt nur diesen einen Rückfall; die
   eigentliche Fehlerklasse (Synonyme wie `Owned`, `CanEdit`, `IsMine`) ist nicht trennscharf formulierbar,
   ohne legitime Namen wie `IsOwner` mitzutreffen. *Kosten:* ein künftiger Ausreißer hängt wieder an Disziplin
   bzw. am Reviewer. AK 6 entfällt damit.
4. **`Pugling.Client` zieht nicht mit:** er benutzt `SubjectResponse` (`CreatorApi.cs`), liest das Feld aber
   nicht; der Routen-Wächter vergleicht Routen, keine Felder. *Kosten:* keine.
5. **Historische Sätze bleiben historisch.** Kommentare, die beschreiben, was *bis B-170* galt
   (`CatalogAdmin.tsx:188`, `CatalogAdmin.test.tsx:235`, `frontend/CLAUDE.md:112`), nennen weiter `isMine` –
   damals hieß es so. Gegenwartssätze sind umgestellt. *Kosten:* eine Volltextsuche nach `isMine` findet drei
   Treffer, die keine Leser sind.

## Schätzung

- **groesse:** S · **wo:** beides · **migration:** nein · **vertragsbruch:** ja (siehe Entscheidung 2).
- **Angriffsplan:** `CatalogDtos.cs` → Tests (`FachEigentumTests`) → `docs/openapi/v1.json` und
  `docs/api-examples/catalog.md` über die Suite neu → `gen:contract` → Frontend-Leser und Fixtures → Wiki.
- **Testweg:** `FachEigentumTests` (fünf Fälle lesen das Feld), `CatalogAdmin.test.tsx`, `tsc -b`, Rollengang
  Katalogseite im Browser.

## Verlauf

- **2026-08-12** — angelegt beim Grillen von
  [B-154](B-154-katalogseite-bietet-fremde-faecher-zum-umbenennen.md) (Entscheidung 5) im Nachtlauf.
  **Bewusst nicht in B-154 mitgenommen:** dessen Ziel — die Oberfläche verspricht nichts, was der Server
  verweigert — ist ohne die Umbenennung erfüllt; sie hat ein anderes `art` (`Aufräumen`), ein anderes `wo`
  (`beides`) und trägt eine Vertragsfrage, die B-154 nicht braucht.
- **2026-08-12** — direkt auf `ausformuliert` statt auf `idee`: der Ist-Stand entstand beim Recherchieren
  von B-154 und ist am Code belegt (alle acht Fundstellen einzeln nachgesehen), darum wäre
  `unverifiziert: true` eine Untertreibung. Die **fachliche** Frage (umbenennen ja/nein, Tor ja/nein) ist
  offen und gehört ins Grillen.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht – `IsOwn` steht jetzt **8-mal** in `Pugling.Contracts` (neu `CategoryResponse`, die sich auf diese Story beruft), `IsMine` einmal (`CatalogDtos.cs:19`). Es gibt einen Leser im Produktionscode (`CatalogAdmin.tsx:244`) und Fixtures in vier Testdateien. `vertragsbruch: ja` (Umbenennung).
- **2026-10-23** — vierter Nachtlauf, Sprint 8 (NP-4, [Protokoll](../pm-sitzung-2026-10-23-nacht.md)): autonom gegrillt, geschätzt, gebaut. Umgestellt: `CatalogDtos.cs` (Feld, `paramref`, `param`, der Satz an `CategoryResponse`), `FachEigentumTests.cs` (5 × `GetProperty`, 3 Kommentare), `CatalogAdmin.tsx:244` und zwei Gegenwartssätze (`:131`, `:235`), Fixtures in `CatalogAdmin.test.tsx`, `SeriesForm.test.tsx`, `TextbookForm.test.tsx`, `VaterFachlehrer.test.tsx`, zwei Sätze in `CatalogAdmin.test.tsx` (`:14`, `:103`), `wiki/03-uebungstypen.md:32`; `docs/api-examples/catalog.md` und `openapi-examples.generated.json` von der Suite neu geschrieben (je eine Zeile, byte-stabil sonst). Rote Proben (Auflage 5): Draht-Name per `[JsonPropertyName("isMine")]` zurückgesetzt → **5 rot** an `FachEigentumTests.cs:89, :109, :145, :186, :643` (erwartet: die fünf Leser); Frontend-Leser auf das alte Feld zurückgesetzt → **1 rot** an `CatalogAdmin.test.tsx:35` (erwartet: der Eigentümer-Fall). Backend 894/894, Vitest 406/406, `tsc -b` sauber.
- **2026-10-23** — `pugling-reviewer` (Backend, zuerst) und danach `frontend-reviewer` (nacheinander, Freigabe 3): kein Blocker. Gezählt (Behebung ändert Code oder Tests): **3** – (1) vier Inline-`<para>` in Contracts (`CreatorProfileDtos.cs:17`, `TextbookSeriesDtos.cs:20`, `ProfileDtos.cs:26`, `ExerciseGrantDtos.cs:18` im `<param>`) verloren weiter Absatz und Backticks, ohne dass ein Tor es sah → auf leere `///`-Zeile umgestellt; (2) Betonung über einen Absatz würde zu `**a` + Leerzeile + `b**` → Übersetzung nur innerhalb eines Absatzes, sonst bleibt das Tag roh und das Tor meldet es, neuer Fall `Betonung_Ueber_Einen_Absatz_Bleibt_Roh` (Mutante: rot an `OpenApiProseNormalizerTests.cs:55`); (3) die Aussage „Zeilen fremder Eigentümer werden mit umgeschrieben" war ungetestet → Reihe eines zweiten Creators im B-145-Fall (Mutante „nur eigene Reihen": rot an `FachnameFolgtDerFachIdTests.cs:130`). Ohne Zähler: Kommentar zum zweiten Mehrdeutigkeitsfall im Normalisierer; Messung „Transformer raus" am Endstand → die Einrückungs-Zusicherung bleibt rot (**7** Texte an `ContractDocumentTests.cs:213`), trägt also weiter; die vorbestehenden Generator-Eigenheiten → **B-259**; Nit `frontend/CLAUDE.md:112` (historischer Satz) belassen. Nachher: Absatzumbrüche **152**, Backend **895/895**, Vitest **406/406**, `tsc -b` sauber. **Rollengang im Browser** (Freigabe 6, Wegwerf-DB `pugling_walk4.db`, Server nach dem letzten Edit gestartet): Swagger UI zeigt die Summary von `POST /api/v1/auth/login` ohne `<b>`, keine Summary beginnt mit Leerraum, `DELETE …/subjects/{id}` mit Absätzen. Vater-Web `/vater/katalog`: neues Fach „Franz Walk" angelegt → die Zeile bietet „Fach umbenennen"/„Löschen" (`isOwn` auf dem Draht), Seed-Fächer nicht; Reihe und Lehrbuch des Kindes per API an das Fach gehängt, im UI umbenannt in „Französisch Walk" → `/vater/lehrwerke` zeigt die Reihe sofort mit dem neuen Fachnamen, das Lehrbuch des Kindes trägt ihn ebenfalls. **Einschränkung, benannt:** nach dem Wiederverbinden der Extension kamen Klicks und Tastatur nicht mehr an; Eingaben liefen darum über die echten Handler der Seite (nativer Value-Setter + `input`/`change`, `button.click()`), nicht über `computer.type`. Sohn: Regressionszeuge (Startseite geladen, kein Student-Pfad im Diff außer Kommentaren, Suite grün). Abgenommen; Commit: siehe Nachtrag.
