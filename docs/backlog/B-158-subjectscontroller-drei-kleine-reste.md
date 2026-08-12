---
tags: [typ/story, status/ausformuliert, bereich/katalog, bereich/backend, rolle/creator]
aliases: [Drei Reste im SubjectsController]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zu B-13 (Nachtlauf 2026-08-12, Funde 4-6)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-158 · Drei kleine Reste im `SubjectsController`

Drei Befunde desselben Reviews an derselben Datei, keiner davon ein Defekt mit heutiger Wirkung, alle drei
von der Sorte „fällt beim nächsten Feld auf". Sie liegen in **einer** Story, weil sie sich in einem Schnitt
beheben lassen und je einzeln nicht die Aufmerksamkeit einer eigenen Datei rechtfertigen.

## User Story

Als *Entwickler* möchte ich, dass die drei Wege durch den `SubjectsController` dieselbe Antwortform und
dieselben Eingabe-Zusicherungen benutzen, damit ein künftiges Feld nicht an genau einer Stelle fehlt.

## Ist-Stand am Code

1. **`Update` baut die Antwort von Hand statt über `Project`** —
   `backend/Pugling.Api/Controllers/Creator/SubjectsController.cs:84-86`. Das ergibt zwei Formen für eine
   Nutzlast plus eine zusätzliche `CountAsync`-Runde. Der Nachbar macht es an derselben Stelle anders:
   `TextbookSeriesController.cs:249` schreibt
   `await Project(db.TextbookSeries.AsNoTracking().Where(…), fid).FirstAsync(ct)`.
   **Wirkung:** ein künftiges Feld in `Project` fehlt in der PATCH-Antwort still.
2. **`Update` hat keine Leer-Prüfung** — `SubjectsController.cs:81`: `PATCH {"name":"   "}` benennt das Fach
   mit `200` auf `""` um. `Create` lehnt genau das ab (`:55`), und `TextbookSeriesController.cs:211-212`
   trägt den Guard. Vorbestehend, nicht von B-13 eingeführt; dessen Owner-Gate hat die Reichweite sogar auf
   den Eigentümer geschrumpft.
3. **Ein Kommentar über `Create` ist strenger als der Code** — `SubjectsController.cs:58-60` sagt „or to
   nobody, which would make it permanently uneditable". Genau das entsteht, wenn `User.CreatorId()` `null`
   ist: `OwnerAdultId = fid` schreibt dann `null` ohne Murren. Praktisch unerreichbar, weil die
   Creator-Rolle ein Adult-Profil impliziert (`backend/Pugling.Api/Auth/TokenService.cs:43-44`), und
   `TextbookSeries` verhält sich genauso — der Kommentar behauptet aber eine Prüfung, die nicht dasteht
   (Fehlerklasse [B-112](B-112-kommentar-begruendet-das-gegenteil.md), nur milder).

## Die echte Lücke

Kein falsches Verhalten, das heute jemand auslöst. Die Lücke ist, dass drei Kleinigkeiten den
`SubjectsController` von dem Muster wegdriften lassen, das `TextbookSeriesController` an genau denselben
Stellen vorgibt — und Punkt 1 ist die Art von Abweichung, die erst auffällt, wenn ein Feld fehlt.

## Offene Punkte

1. **Punkt 2 mit einem Guard oder mit einem Wächter beheben?** Empfehlung: erst den Guard (drei Zeilen,
   Muster liegt vor). Ein reflexiver Wächter „jedes `Update…Dto` mit einem Pflicht-String prüft auf leer"
   wäre die Regel dahinter — erst messen, wie viele Stellen ihn heute reißen würden, wie es
   `CLAUDE.md` verlangt („Neue Regel scharf stellen? Erst messen.").
2. **Punkt 3: Kommentar an den Code angleichen oder Code an den Kommentar?** Empfehlung: den Code, also
   ein `if (fid is null) return this.ProblemWithCode(…)`. Er ist billiger als die Erklärung, warum der
   Zustand unerreichbar ist, und er macht die Zusicherung wahr statt sie zu relativieren. Gegenargument,
   das mit entschieden werden muss: dann weicht `Subject` von `TextbookSeries` ab, und die Abweichung wäre
   neu.
3. **Gehört Punkt 2 überhaupt zu `Aufräumen`?** Ein `PATCH`, der einen leeren Namen speichert, ist der
   Sache nach ein kleiner Defekt. Er steht hier trotzdem als `Aufräumen`, weil kein Nutzer ihn heute
   auslösen kann, ohne es zu wollen. Falls beim Grillen anders entschieden wird, wandert er als eigene
   `Defekt`-Story heraus.

## Akzeptanzkriterien (Entwurf)

1. `Update` liefert seine Antwort über dieselbe `Project`-Projektion wie `List`/`Get`; die zusätzliche
   `CountAsync`-Runde entfällt.
2. `PATCH` mit leerem oder nur aus Leerzeichen bestehendem `name` wird abgelehnt, mit demselben Fehlercode,
   den `Create` dafür benutzt.
3. Punkt 3 ist entschieden und der Zustand danach widerspruchsfrei — entweder der Guard steht, oder der
   Kommentar behauptet ihn nicht mehr.
4. Ein Testfall je Punkt 1 und 2; alles andere bleibt so grün wie vorher.

## Verlauf

- **2026-08-12** — angelegt aus dem `pugling-reviewer`-Befund zum B-13-Review (Funde 4–6, Nachtlauf
  Sprint A). **Bewusst nicht in B-13 mitgenommen:** dessen Ziel ist ohne sie erfüllt, alle drei liegen
  außerhalb seines Diffs (Punkt 2 und 3 sind älter als der Commit), und der Reviewer hat sie ausdrücklich
  als nicht blockierend eingeordnet. Zwei weitere Befunde desselben Reviews sind bewusst **nicht** hier
  gelandet: die ungeschützten Kategorien als [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md)
  (eigene Fehlerklasse, P2) und die Feldnamens-Dublette als
  [B-156](B-156-ismine-heisst-anderswo-isown.md) (Vertragsfrage).
- **2026-08-12** — direkt auf `ausformuliert`: der Ist-Stand kommt aus dem Review und ist mit `Datei:Zeile`
  belegt; nachgesehen wurde jede der drei Stellen einzeln.
