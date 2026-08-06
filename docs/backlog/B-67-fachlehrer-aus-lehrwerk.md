---
tags: [typ/story, status/in-arbeit, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Fachlehrer vorbelegen, Profil aus Reihe]
status: in-arbeit
prio: P2
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: remark #8
---

# B-67 · Der Fachlehrer fragt nach Fach und Sprachen, die im gewählten Lehrwerk längst stehen

## User Story

Als Vater möchte ich beim Anlegen eines Fachlehrers nach der Wahl des Lehrwerks Fach, Lern- und
Muttersprache **vorbelegt** bekommen, damit ich nicht dreimal eintippe, was das Buch schon weiß.

## Ist-Stand am Code

Im Formular sind es vier voneinander unabhängige Eingaben
([VaterFachlehrer.tsx](../../frontend/src/vater/VaterFachlehrer.tsx)):

- Fach — Pulldown (`:221`)
- Lehrwerk — Pulldown (`:249`)
- Lernsprache — **Freitext** mit Vorgabe `en` (`:258`, Startwert `:140`)
- Muttersprache — **Freitext** mit Vorgabe `de` (`:262`, Startwert `:141`)

Die Daten zum Ableiten liegen vor: `TextbookSeries` trägt `SubjectName`/`SubjectId` und
`SourceLanguage`/`TargetLanguage`
([CurriculumEntities.cs:24-33](../../backend/Pugling.Api/Models/CurriculumEntities.cs)); der Listen-Endpunkt
liefert sie mit ([TextbookSeriesController.cs:47](../../backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs)).

Am Zielobjekt sind die Felder eigenständig: `CreatorProfile.SubjectId` ist nullable,
`SourceLang`/`TargetLang` haben die Vorgaben `en`/`de` (`CurriculumEntities.cs:89,101,103`), `SeriesId` ist
nullable (`:98`).

## Die echte Lücke

Reine Oberflächen-Arbeit — **das Modell muss nicht geändert werden.** Die Werte sind bewusst am Profil
eigenständig, weil ein Profil ohne Reihe (`SeriesId` nullable) sein Fach und seine Sprachen selbst braucht;
und das Matching gewichtet Reihe (8) und Fach (4) getrennt, ein abgeleitetes Fach bliebe also wirksam.

Es fehlt nur: beim Wählen der Reihe die drei Felder vorbelegen — **beschreibbar bleibend**, nicht sperrend.

Berührt [B-63](B-63-lehrwerk-hierarchie.md) (Sprachen als Auswahl statt Freitext), hängt aber nicht davon
ab: das Vorbelegen funktioniert auch mit Freitextfeldern. ~~Verhältnis zu B-63/B-64 klären~~ → siehe
Entscheidung 5: eigenständige, dritte Lücke — keine der beiden löst sie mit.

Nachgeprüft gegen den heutigen Code (2026-08-04, unverändert seit der Ausformulierung, weil B-63/B-64 noch
nicht gebaut sind): alle vier `Datei:Zeile`-Belege oben stimmen weiter exakt
([VaterFachlehrer.tsx:221,249,258,262,140,141](../../frontend/src/vater/VaterFachlehrer.tsx),
[CurriculumEntities.cs:24-33,89,98,101,103](../../backend/Pugling.Api/Models/CurriculumEntities.cs),
[TextbookSeriesController.cs:47](../../backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs)).
Zusätzlich geprüft: `TextbookSeriesResponse` trägt `SubjectId`, `SourceLanguage`, `TargetLanguage` bereits im
Vertrag ([TextbookSeriesDtos.cs:11-13](../../backend/Pugling.Contracts/Creator/TextbookSeriesDtos.cs)), und
`VaterFachlehrer.tsx` lädt die volle Liste schon heute (`const series = useAsync<TextbookSeriesResponse[]>
(() => api.textbookSeries(), [])`, Zeile 32) — die Ableitung braucht **keinen** neuen API-Aufruf, nur eine
`onChange`-Reaktion auf das bestehende Datenobjekt. Die Matching-Gewichte in `CreatorProfileService.cs:18-21`
(Reihe 8 > Fach 4 > Klassenstufe 2 > Schulart 1) bestätigen die Begründung „ein abgeleitetes Fach bliebe
wirksam" unverändert.

## Offene Punkte

1. ~~Nur leere Felder vorbelegen oder auch überschreiben?~~ → siehe Entscheidung 1.
2. ~~Was, wenn die Reihe kein Fach trägt?~~ → siehe Entscheidung 2.
3. ~~Sichtbar machen, dass ein Wert abgeleitet ist?~~ → siehe Entscheidung 3.

## Entscheidungen

1. **„Leer" heißt „vom Nutzer nicht angefasst", nicht „Feldwert ist ein leerer String".** Fach startet leer
   (`form.subjectId ?? ""`), aber Lern-/Muttersprache tragen von Anfang an die Vorgaben `en`/`de`
   (`VaterFachlehrer.tsx:140-141`) — ein naiver Leer-Check würde dort nie greifen, weil das Feld nie ein
   leerer String ist. Das Formular bekommt darum ein `touched: Set<"subjectId" | "sourceLang" |
   "targetLang">`, das jeder manuelle `onChange` dieser drei Felder befüllt; die Ableitung beim Wählen der
   Reihe überschreibt nur Felder, die **nicht** in `touched` stehen. Begründung: löst genau die Lücke, die
   die Ausformulierung mit „leere füllen, gefüllte in Ruhe lassen" nur grob benannt hatte, ohne dass ein
   Vater, der bewusst `fr` statt der Vorgabe `en` einträgt und danach die Reihe wechselt, seine Abweichung
   verliert. Kosten: ein zusätzliches `useState<Set<string>>` plus drei `touched.add(...)`-Aufrufe in den
   bestehenden `onChange`-Handlern — kein neuer Zustand außerhalb der Komponente, keine Migration.
2. **Nur setzen, wenn ein Katalog-Fach dranhängt.** Ist `series.subjectId` `null` (die Reihe trägt nur einen
   freien `subjectName`), bleibt das Fach-Pulldown unverändert. Begründung: das Pulldown kennt nur
   Katalog-Fächer (`subjects.map(...)`, `VaterFachlehrer.tsx:224`) — ein Freitextname ließe sich dort gar
   nicht abbilden, ein Rateversuch (z. B. Namensvergleich) wäre zusätzliche, fehleranfällige Logik für einen
   Fall, den die Story nicht verlangt. Kosten: keine — die Bedingung ist ein einzelnes `if (series.subjectId
   != null)` in der Ableitung.
3. **Ein kurzer Hinweistext statt einer Sperre, im bestehenden `muted`-Stil.** Nach einer Ableitung zeigt das
   Formular neben den betroffenen Feldern einen `<span className="muted">`-Hinweis „aus dem Lehrwerk
   übernommen" (dasselbe Muster wie die bereits vorhandenen `muted`-Zusatzzeilen in derselben Datei,
   `VaterFachlehrer.tsx:64,68`) — **nicht** die `InfoHint`/`FieldLabel`-Popover-Konvention aus
   `frontend/CLAUDE.md`, weil die für die *dauerhafte* Erklärung eines Feldbegriffs gedacht ist
   (`lib/fieldHelp.ts`), nicht für einen einmaligen Ableitungs-Moment an einem konkreten Wert. Begründung:
   dieselbe Haltung wie die Erfolgsmeldung beim Anlegen einer Reihe
   ([VaterLehrwerke.tsx:328-331](../../frontend/src/vater/VaterLehrwerke.tsx), „`runFor`, weil die Meldung
   den Namen **des Servers** nennt") — informieren, nicht blockieren. Kosten: ein Stück UI-Text, gebunden an
   dieselbe `touched`-Markierung aus Entscheidung 1 (Hinweis verschwindet, sobald der Nutzer das Feld selbst
   ändert).
4. **Sprachcode-Rohwerte sind heute Freitext — bewusst kein Blocker.** Weil `TextbookSeries.SourceLanguage`/
   `.TargetLanguage` vor [B-63](B-63-lehrwerk-hierarchie.md) noch `string?`-Freitext sind
   (`CurriculumEntities.cs:31,33`, unverändert nachgeprüft), kann eine Reihe theoretisch einen Wert tragen,
   der nicht in der `LANGUAGES`-Liste steht. Da die Zielfelder in `CreatorProfile` selbst Freitext bleiben
   (Akzeptanzkriterium 3), ist das kein Korrektheitsproblem, nur ein Qualitätsdetail: nach B-63s
   Entscheidung 5 (dieselbe `LANGUAGES`-Liste im Lehrwerk-Formular) sind die abgeleiteten Werte zuverlässiger
   sauber. Begründung: kein Grund, auf B-63 zu warten (siehe Entscheidung 5) — nur ein Hinweis, den Testfall
   „Reihe mit unüblichem Sprachcode" nicht überzubewerten. Kosten: keine.
5. **Verhältnis zu B-63/B-64: eigenständige dritte Lücke, keine Überlappung im Baubestand.** B-63 baut die
   *innere Struktur* des geteilten Katalogs um (`Publisher`→Entität, `Topics`→Liste, `BookType`, Filter) und
   ersetzt in seiner Entscheidung 5 die zwei Freitext-`<input>` für Sprachen **im Lehrwerk-Formular**
   (`VaterLehrwerke.tsx:374-379`) durch `<select>` aus `LANGUAGES` — eine andere Datei, ein anderes Formular
   als diese Story (`VaterFachlehrer.tsx`). B-64 klärt die Brücke `Textbook` (Freitext am Kind) ↔
   `TextbookSeries` (Katalog) in `ChildMaterialSection.tsx`/`TextbooksController.cs` — wieder andere Dateien,
   andere Frage („welchen Weg nehme ich beim Anlegen des Kind-Buchs", nicht „was befüllt sich beim Anlegen
   eines Fachlehrers"). Keine der beiden Stories berührt `VaterFachlehrer.tsx`, `CreatorProfile` oder
   `CreatorProfileService`. **Keine harte Abhängigkeit**: alle drei von B-67 gelesenen Felder
   (`TextbookSeries.SubjectId`, `.SourceLanguage`, `.TargetLanguage`) bleiben durch B-63 im Typ unverändert
   (`string`/`int?`, kein Vertragsbruch an dieser Stelle) — B-67 ist vor, während oder nach B-63/B-64 baubar.
   Einzige weiche Beziehung: siehe Entscheidung 4 (Datenqualität der Sprachwerte steigt leicht nach B-63,
   ändert aber weder Umfang noch Größe dieser Story). Begründung: verhindert, dass B-67 fälschlich als
   Teilmenge oder Dopplung von B-63/B-64 gilt und deshalb unnötig zurückgestellt wird. Kosten: keine.

## Akzeptanzkriterien

1. Wählt man im Fachlehrer-Formular ein Lehrwerk, füllen sich die Felder Fach, Lern- und Muttersprache aus
   der Reihe — sofern der Nutzer sie seit dem Öffnen des Formulars noch nicht selbst geändert hat
   (Entscheidung 1).
2. Ein bereits vom Nutzer geändertes Feld bleibt beim (erneuten) Wählen einer Reihe unverändert.
3. Alle drei Felder bleiben von Hand änderbar; ein Profil ohne Lehrwerk verhält sich wie bisher.
4. Trägt die Reihe kein Katalog-Fach (`subjectId` ist `null`), bleibt das Fach-Pulldown unverändert statt
   geraten (Entscheidung 2).
5. Nach einer Ableitung zeigt das Formular neben den betroffenen Feldern den Hinweis „aus dem Lehrwerk
   übernommen"; der Hinweis verschwindet, sobald der Nutzer das Feld selbst ändert (Entscheidung 3).
6. Ein Komponententest deckt mindestens drei Fälle ab: „Reihe wählen → leere Felder gefüllt", „vom Nutzer
   geändertes Feld bleibt trotz Reihenwahl unverändert" und „Reihe ohne Katalog-Fach lässt das Fach-Pulldown
   in Ruhe".

## Schätzung

**Größe: S** — vergleichbar mit B-01 (`childId` aus dem Test-Pfad ziehen): eine lokalisierte Änderung in
**einer** Komponente (`VaterFachlehrer.tsx`), keine neue Route, kein neuer Endpunkt, keine neuen
Vertragsfelder (`TextbookSeriesResponse` trägt `subjectId`/`sourceLanguage`/`targetLanguage` bereits, die
Reihenliste ist im Formular schon geladen). Etwas mehr als eine reine Ein-Punkt-Änderung, weil drei Felder,
ein `touched`-Zustand und ein Hinweistext zusammenkommen — dafür bleibt es unter M, weil keine neue
Interaktionsebene (Batch, Liste, neuer Screen) entsteht wie beim `MediaSelector`-Anker.

- **`wo: frontend`** — reine Oberflächen-Arbeit, kein Backend-Anteil (die Ist-Stand-Prüfung bestätigt: alle
  gebrauchten Felder sind bereits im Vertrag und werden bereits geladen).
- **`migration: nein`** — kein Schema betroffen, `CreatorProfile`/`TextbookSeries` ändern sich nicht.
- **`vertragsbruch: nein`** — keine neue/geänderte DTO-Form; die Ableitung liest ausschließlich bereits
  vorhandene Felder der bereits geladenen `TextbookSeriesResponse[]`.
- **Risiken:**
  - Die `touched`-Logik aus Entscheidung 1 ist der einzige neue Zustand dieser Story — ohne den zugehörigen
    Testfall (Akzeptanzkriterium 2/6) bliebe ein Regressionsrisiko unsichtbar: ein Refactoring des Formulars
    könnte den Leer-Check versehentlich wieder auf einen naiven String-Vergleich zurückstellen.
  - Sprachcode-Rohwerte aus der Reihe sind vor B-63 ungeprüfter Freitext (Entscheidung 4) — kein
    Korrektheitsrisiko für diese Story, aber ein Testfall mit „unüblichem" Sprachcode sollte trotzdem einmal
    grün laufen, damit die Übernahme nicht an einer stillen Annahme über das Format hängt.
  - Keine reale Abhängigkeit zu B-63/B-64 (Entscheidung 5) — das einzige Risiko wäre, B-67 fälschlich hinter
    einer der beiden Stories einzureihen und damit ohne Grund zu verzögern.
- **Angriffsplan** (reines Frontend, keine Backend-Reihenfolge nötig):
  1. `touched`-Zustand (`Set<"subjectId" | "sourceLang" | "targetLang">`) in `ProfileForm` ergänzen, in den
     bestehenden `onChange`-Handlern der drei Felder befüllen.
  2. `onChange` des Lehrwerk-`<select>` (`VaterFachlehrer.tsx:250`) erweitert: die gewählte Reihe aus
     `series` nachschlagen, für jedes nicht-`touched`-Feld den Wert übernehmen (Fach nur bei gesetztem
     `subjectId`, Entscheidung 2).
  3. Hinweistext-Rendering: `muted`-Zeile neben Fach-/Sprachfeldern, sichtbar nur solange das jeweilige Feld
     zuletzt abgeleitet und noch nicht `touched` ist.
  4. Neue Datei `VaterFachlehrer.test.tsx` (Muster: `ClozeTexts.test.tsx` im selben Ordner) mit den drei
     Fällen aus Akzeptanzkriterium 6.
- **Testweg:**
  - Komponententest `frontend/src/vater/VaterFachlehrer.test.tsx` (neu, React Testing Library, Muster
    `ClozeTexts.test.tsx`): rendert `ProfileForm` mit einer Test-`series`-Liste, wählt per `fireEvent` eine
    Reihe, prüft Feldwerte und Hinweistext für alle drei Fälle aus Akzeptanzkriterium 6.
  - Kein Backend-Test nötig (kein Endpunkt, kein Contract geändert); `npm test` deckt die neue Datei ab.
  - Kein E2E-Fall zwingend erforderlich (reine Formular-Vorbelegung ohne neuen Netzwerkpfad), aber falls ein
    bestehender Fachlehrer-E2E-Rundgang existiert, dort einen Blick auf das gefüllte Fach-Feld ergänzen –
    kein neuer Spec nötig.

## Verlauf

- **2026-08-02** — angelegt aus Anmerkung #8; Ist-Stand am Code belegt, Befund:
  [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#e--fachlehrer-ableitung-aus-dem-lehrwerk-8).
- **2026-08-04** — gegrillt: alle drei offenen Punkte in fünf nummerierte Entscheidungen überführt
  (Leer-Semantik über einen `touched`-Zustand statt naivem String-Vergleich, Fach nur bei Katalog-Bezug,
  Hinweistext im bestehenden `muted`-Stil statt Sperre, Sprachcode-Rohwerte bewusst kein Blocker, Verhältnis
  zu B-63/B-64 als eigenständige dritte Lücke ohne Überlappung geklärt); jeder `Datei:Zeile`-Beleg des
  Ist-Stands gegen den heutigen Code nachgeprüft, dabei zusätzlich verifiziert, dass `TextbookSeriesResponse`
  die benötigten Felder bereits trägt und `VaterFachlehrer.tsx` die Reihenliste bereits lädt (autonom
  getroffen, Nutzerauftrag).
- **2026-08-04** — geschätzt: Größe S, `wo: frontend`, `migration: nein`, `vertragsbruch: nein`; Risiken,
  Angriffsplan und Testweg ergänzt (autonom getroffen, Nutzerauftrag).
- **2026-08-06** — in Arbeit: `touched`/`derived`-Zustand und `deriveFromSeries` in
  [VaterFachlehrer.tsx](../../frontend/src/vater/VaterFachlehrer.tsx) ergänzt (`ProfileForm` exportiert),
  Hinweistext „aus dem Lehrwerk übernommen" im bestehenden `muted`-Stil ergänzt. Neuer Komponententest
  [VaterFachlehrer.test.tsx](../../frontend/src/vater/VaterFachlehrer.test.tsx) deckt die drei Fälle aus
  Akzeptanzkriterium 6 ab (Reihe wählen → leere Felder gefüllt; berührtes Feld bleibt unverändert; Reihe
  ohne Katalog-Fach lässt das Fach-Pulldown in Ruhe) — 3 von 3 grün. Gesamte Suite weiter grün
  (`npm test`: 156/156, 24 Dateien), `npm run build` (`tsc -b && vite build`) fehlerfrei. Kein Backend-
  Anteil, kein neuer Endpunkt, kein Contract geändert. `frontend-reviewer` und Rollengang stehen noch aus.
