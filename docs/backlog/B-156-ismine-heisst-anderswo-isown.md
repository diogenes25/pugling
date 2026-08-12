---
tags: [typ/story, status/ausformuliert, bereich/katalog, bereich/vertrag, rolle/creator]
aliases: [isMine vs isOwn, Eigentums-Flag heißt zweimal anders]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
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

## Akzeptanzkriterien (Entwurf)

1. `SubjectResponse.IsMine` heißt `IsOwn`; die Bedeutung („der aufrufende Creator ist Eigentümer; ein
   ownerloses Fach ist niemandes") bleibt unverändert, XML-Doc nachgezogen.
2. `SubjectsController`s drei Projektionen liefern das Feld unter dem neuen Namen.
3. Die drei Test-Fixtures und der Leser aus B-154 sind nachgezogen; `npm run build` grün.
4. `IsOwner` bleibt, wo es steht, und wird nicht mit `IsOwn` verschmolzen.
5. Das OpenAPI-Dokument wird neu geschrieben (`DocsCaptureTests`), `docs/api-examples` byte-stabil.
6. Falls Punkt 3 der offenen Punkte mit „ja" entschieden wird: ein Wächter, der den Rückfall rot meldet.

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
