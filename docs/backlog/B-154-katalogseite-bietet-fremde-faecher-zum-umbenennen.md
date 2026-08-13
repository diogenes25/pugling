---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/frontend, rolle/creator]
aliases: [Fach-Eigentum im Vater-Web sichtbar machen]
status: abgenommen
nachgeschaut: 2026-08-13
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/backlog/B-13-fach-kapitel-eigentum.md
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-154 · Die Katalogseite bietet „Umbenennen" und „Löschen" an jedem Fach an — auch an fremden

Seit [B-13](B-13-fach-kapitel-eigentum.md) darf nur der Eigentümer ein Fach umbenennen oder löschen, und ein
Seed-Fach („Englisch", „Mathe", „Erdkunde", „Französisch") gehört niemandem, ist also für **jeden** gesperrt.
`CatalogAdmin.tsx` zeigt die `NameRow` mit „OK" und „Löschen" trotzdem an jedem gewählten Fach, und der
Löschdialog verspricht ausdrücklich, was danach passiert — der Server antwortet `403 not_owner`. Die Antwort
trägt seit B-13 `isMine`, die Oberfläche liest es nicht. Das ist dieselbe Klasse wie
[B-150](B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md) (Verlagssperre unsichtbar, Dialog
versprach das Gegenteil), nur eine Katalogebene höher; B-13 hat sie in Entscheidung 5 bewusst offen gelassen
(`wo: backend`) und die Erfassung als eigene Story vorgesehen, falls sie beim Testen auffällt. Sie fiel beim
Rollengang gegen die laufende App auf.

## User Story

Als *Creator* möchte ich an einem Fach nur die Knöpfe sehen, die ich auch drücken darf, damit ich nicht in
ein `403` laufe, nachdem mir ein Dialog bereits erklärt hat, was mein Löschen anrichten wird.

## Ist-Stand am Code

- **Die Knöpfe hängen an nichts.** `frontend/src/vater/CatalogAdmin.tsx:81-95` rendert die `NameRow` des
  gewählten Fachs bedingungslos; `onSave` ruft `api.updateSubject`, `onDelete` nach einem `confirmAction`
  die `api.deleteSubject` (`frontend/src/lib/api.ts:317-320`). Die `NameRow` selbst kennt nur `busy`, kein
  Recht (`CatalogAdmin.tsx:136-156`).
- **Die Daten liegen schon bereit.** `VaterKatalog.tsx:35` reicht `subjects.data` aus `api.subjects()`
  (`api.ts:315`) durch, also `SubjectResponse[]` — und der trägt seit B-13 `ownerAdultId` **und** `isMine`
  (`backend/Pugling.Contracts/Creator/CatalogDtos.cs:19`, generiert in `frontend/src/lib/contract.ts:25324`).
  Es fehlt kein Feld und kein Aufruf: nur das Lesen fehlt.
- **Kein Produktionscode liest `isMine`.** Die einzigen Treffer im Frontend sind drei Test-Fixtures
  (`SeriesForm.test.tsx:17-18`, `TextbookForm.test.tsx:19-20`, `VaterFachlehrer.test.tsx:12`), die das Feld
  nur nachtragen, damit der Typecheck grün bleibt (Commit `12e559e`).
- **Die Server-Seite ist so scharf wie behauptet.** `SubjectsController.cs:78-79` (`PATCH`) und `:125-126`
  (`DELETE`) gaten über `ClaimsPrincipalExtensions.IsOwnedBy` und liefern sonst
  `this.ProblemWithCode(ApiErrors.NotOwner, …)` — beim `DELETE` **vor** der Verwendungsprüfung.
- **Die „Art" ist nicht betroffen, und das ist wichtig für den Schnitt.** `ExerciseCategory` trägt gar kein
  Eigentümer-Feld (`backend/Pugling.Api/Models/LearnEntities.cs:40-46`), `ExerciseCategoriesController`
  prüft außer `[Authorize(Roles = Roles.Creator)]` nichts (`:20,54,77,101`), und `CategoryResponse` hat
  folgerichtig kein `isMine` (`CatalogDtos.cs:28`). Die Art-Zeilen dürfen also bedingungslos bleiben — nur
  die Fach-Zeile verspricht etwas, das nicht gilt.
- **Zwei Behauptungen im selben Bauteil sind seit B-13 falsch**, und eine davon ist sichtbar:
  - Kommentar `CatalogAdmin.tsx:12-13`: „ein Tippfehler im Fachnamen blieb für alle sichtbar, denn der
    Katalog ist **global**: Fächer teilen sich alle Väter. Genau deshalb warnt das Löschen hier deutlich."
  - **Sichtbarer UI-Text** `CatalogAdmin.tsx:62`: „Fächer und Kapitel sind **gemeinsamer Katalog** – deine
    Änderungen sehen alle Väter." Der Satz nennt zusätzlich „Kapitel", die B-106 entfernt hat.
- **Das Vorbild steht im Haus, zweimal.** `VaterLehrwerke.tsx:183-205` blendet Bearbeiten/Löschen an fremden
  Reihen aus, mit genau dieser Begründung im Kommentar: „Fremde Reihen bleiben lesbar – die Knöpfe fehlen,
  statt später mit 403 zu scheitern." Und `:221-223` sagt dem Nutzer, warum: „Diese Reihe hat jemand anderes
  angelegt – du kannst sie verwenden, aber nicht ändern." `VaterExercises.tsx:329-348` macht es genauso,
  inklusive eines `{!exercise.isOwn && …}`-Zweigs.
- **Keine Testabdeckung für dieses Bauteil.** Es gibt keinen `CatalogAdmin.test.tsx` (`src/vater/` führt
  Komponententests für `SeriesForm`, `TextbookForm`, `VaterFachlehrer`, `VaterLehrwerke`, `ClozeTexts`, aber
  nicht für den Katalog), und keine E2E-Spec fährt `/vater/katalog` (`frontend/e2e/`: die Katalog-Treffer
  in `assistent`/`lehrwerke`/`perspektiven` betreffen die Auswahllisten, nicht diese Verwaltungsseite).

**Nebenfund, bewusst nicht Teil dieser Story** (siehe Entscheidung 5): `IsMine` steht **genau einmal** im
ganzen Vertrag (`CatalogDtos.cs:19`). Die sieben anderen Eigentums-DTOs heißen `IsOwn`
(`CreatorProfileDtos.cs:10`, `ExerciseAuthoringDtos.cs:27`, `ExerciseCatalogDtos.cs:15,28`,
`TextbookSeriesDtos.cs:14`, `RemarkDtos.cs:100,126`) — B-13 hat den Ausreißer eingeführt, obwohl sein
Akzeptanzkriterium 5 „analog `TextbookSeriesResponse`" verlangte, und genau der heißt `isOwn`.

## Die echte Lücke

Nicht ein fehlendes Feld und kein fehlender Endpunkt: beides liegt seit B-13 bereit. Die Lücke ist, dass die
Oberfläche eine Handlung **anbietet und ihre Folgen beschreibt**, die der Server verweigert — der
Löschdialog zählt fünf Zuordnungen auf, die verloren gehen, und danach kommt ein `403`. Dazu behaupten zwei
Texte im selben Bauteil weiter das Gegenteil der geltenden Regel, einer davon auf dem Bildschirm.

## Offene Punkte

1. ~~Knöpfe ausblenden oder nur deaktivieren?~~ → Entscheidung 1.
2. ~~Was steht statt der Knöpfe, oder bleibt die Stelle stumm?~~ → Entscheidung 2.
3. ~~Bleibt das Umbenenn-Feld als schreibgeschützte Anzeige stehen?~~ → Entscheidung 3.
4. ~~Werden der veraltete UI-Text (`:62`) und der Kommentar (`:12-13`) mitkorrigiert?~~ → Entscheidung 4.
5. ~~Wird `isMine` im selben Schnitt auf `isOwn` angeglichen?~~ → Entscheidung 5 (eigene Story).
6. ~~Testweg: Komponententest, E2E oder beides?~~ → Entscheidung 6.

## Entscheidungen

1. **Die Knöpfe verschwinden an fremden und an ownerlosen Fächern, sie werden nicht deaktiviert.**
   Begründung: `VaterLehrwerke.tsx:182` und `VaterExercises.tsx:329-348` machen es beide so, und der
   Kommentar dort nennt den Grund, der hier genauso gilt — ein Knopf, der nur zum Scheitern da ist, ist
   kein Knopf. Ein `disabled`-Knopf wäre zudem die schlechtere A11y-Variante: er bleibt im Fokusbaum und
   trägt keinen Grund. **Kosten:** Die Seite sieht am Seed-Fach „Englisch" jetzt ärmer aus als vorher, und
   niemand kann dort mehr etwas anklicken — das ist aber genau der Zustand, den B-13s Entscheidung 3
   hergestellt hat, nur jetzt sichtbar.
2. **Statt der Knöpfe steht ein Satz, der den Grund nennt** — ein `<p className="muted">` in der Form von
   `VaterLehrwerke.tsx:221-223`, mit den zwei Fällen unterschieden: einem fremden Fach („hat jemand anderes
   angelegt") und einem ownerlosen Seed-Fach („gehört zum Grundbestand"). Begründung: Ein stummes Fehlen
   liest sich als Fehler der Seite; B-150 ist genau daran gescheitert, dass die Sperre unsichtbar war.
   **Kosten:** zwei Textbausteine mehr, und die Unterscheidung braucht `ownerAdultId` zusätzlich zu
   `isMine` — beide liegen in derselben Antwort, kostet also keinen Aufruf.
3. **Die ganze `NameRow` entfällt an einem nicht eigenen Fach, es bleibt keine schreibgeschützte Anzeige.**
   Begründung: Der Fachname steht zwei Zeilen darüber schon in der Auswahlliste (`CatalogAdmin.tsx:70`) —
   ein zweites, nicht editierbares Feld mit demselben Wort wäre Dopplung, und ein `readOnly`-Eingabefeld
   ist die Bauform, die am stärksten „hier darf man tippen" suggeriert. **Kosten:** Die Zeile „Fach
   umbenennen" fehlt dann ganz; wer den Namen genau lesen will, liest ihn in der Auswahl.
4. **Ja, beide Texte werden im selben Schnitt korrigiert** — der sichtbare Absatz `:62` und der
   Datei-Kommentar `:12-13`. Begründung: Der sichtbare Satz ist Teil desselben Defekts (er behauptet
   „deine Änderungen sehen alle Väter", während der Server sie ablehnt), und der Kommentar begründet die
   Warnung mit einer Regel, die nicht mehr gilt — das ist die Fehlerklasse aus
   [B-112](B-112-kommentar-begruendet-das-gegenteil.md), im selben Bauteil, das diese Story ohnehin anfasst.
   **Kosten:** keine; es sind zwei Textänderungen ohne Verhaltensfolge. Der Nebensatz über „Kapitel" fällt
   dabei mit weg (B-106 hat sie entfernt).
5. **`isMine` → `isOwn` wird hier NICHT angeglichen, sondern als eigene Story erfasst.** Begründung: Das
   Ziel dieser Story — die Oberfläche verspricht nichts, was der Server verweigert — ist ohne die
   Umbenennung erfüllt, und damit greift die Regel des Bereichs („Ein Fund beim Bauen wird eine eigene
   Story, nicht ein Anhang an die laufende"). Sie ist außerdem `art: Aufräumen` und nicht `Defekt`, hat
   einen anderen `wo` (`beides`) und trägt eine Vertragsfrage, die diese Story nicht braucht.
   **Kosten:** Der Ausreißer bleibt bis dahin stehen, und diese Story liest ihn unter seinem schiefen Namen
   — ein Nachziehen kostet später eine Zeile mehr in genau dem Code, den sie jetzt schreibt. Das ist der
   billigere Fehler: der Nachtlauf darf `Aufräumen` autonom grillen, die Story ist also nicht blockiert.
   → **[B-156](B-156-ismine-heisst-anderswo-isown.md)**
6. **Komponententest (`CatalogAdmin.test.tsx`, neu), keine eigene E2E-Spec.** Begründung: Der Defekt ist
   eine reine Render-Entscheidung aus einem Feld der Antwort — genau die Ebene, für die
   `frontend/CLAUDE.md` den Komponententest vorsieht („Bausteine und Regeln hier, Wege durch die App bei
   Playwright"), und `VaterLehrwerke.test.tsx` ist das direkte Vorbild für dieselbe Sache eine Ebene höher.
   Eine E2E müsste zwei Creator-Konten anlegen, um „fremdes Fach" überhaupt herzustellen; der ownerlose
   Seed-Fall ist im Browser billiger, aber allein kein Weg durch die App. **Kosten:** Der Rollengang für
   diese Story ist damit **kein** wiederholbarer Browser-Weg, sondern eine einmalige Live-Probe (siehe
   Testweg) — nach `docs/nachtlauf.md` Freigabe 6 zulässig, aber der schwächere Beleg. Ausdrücklich
   benannt statt verschwiegen.

## Akzeptanzkriterien

1. An einem Fach mit `isMine: true` ist die Zeile „Fach umbenennen" mit „OK" und „Löschen" unverändert da
   und funktioniert wie bisher.
2. An einem Fach mit `isMine: false` **und** `ownerAdultId !== null` fehlen Feld und beide Knöpfe; an ihrer
   Stelle steht ein Satz, der sagt, dass ein anderer Creator das Fach angelegt hat.
3. An einem Fach mit `ownerAdultId === null` (Seed-Grundbestand) fehlen Feld und Knöpfe ebenfalls; der Satz
   nennt diesen Fall eigens und behauptet nicht, ein anderer Creator sei der Eigentümer.
4. Die Art-Zeilen (`NameRow` je `CategoryResponse`) sind unverändert bedingungslos — kein Recht wird dort
   erfunden, das der Server nicht kennt.
5. Der sichtbare Absatz über den „gemeinsamen Katalog" sagt, was gilt: lesen alle, ändern nur der
   Eigentümer — und nennt keine „Kapitel".
6. Der Datei-Kommentar begründet die Löschwarnung nicht mehr mit der Globalität des Katalogs.
7. `CatalogAdmin.test.tsx` belegt die Fälle 1–4 (eigenes, fremdes, ownerloses Fach; Art bleibt bedienbar).
8. `npm run build` (Typecheck) und `npm test` bleiben grün; keine Server-Datei wird angefasst.

## Schätzung

**Größe: S** — ein Bauteil, eine Bedingung, zwei Textbausteine und ein neuer Komponententest. Näher an
[B-150](B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md) (dieselbe Klasse, `S`) als an B-143;
kein neues Feld, kein neuer Aufruf, kein Server-Anteil. Der Anker `XS` passt nicht, weil ein Test für ein
bisher untestetes Bauteil dazukommt (Fixtures für drei Fach-Zustände).

- **`migration: nein`** — keine Schemaänderung; die Story fasst ausschließlich `frontend/src/vater/` an.
- **`vertragsbruch: nein`** — der Vertrag bleibt unangetastet. Sie *liest* nur ein Feld, das seit B-13
  existiert. Die Namensangleichung `isMine` → `isOwn` ist ausdrücklich ausgelagert (Entscheidung 5).

**Risiken:**

- **Der ownerlose Fall ist der wahrscheinlichste Alltag, nicht der Randfall.** Alle vier Seed-Fächer haben
  keinen Owner (`Data/Seed.cs`, B-13 hat sie nicht nachträglich zugewiesen). Wer die Seite nach dieser
  Story zum ersten Mal öffnet, sieht also mit hoher Wahrscheinlichkeit **gar keine** Bearbeitungszeile —
  richtig, aber überraschend. Entscheidung 2 ist genau deshalb nicht verhandelbar: ohne den Satz liest es
  sich wie eine kaputte Seite. Beim Rollengang gezielt in dieser Reihenfolge ansehen.
- `VaterKatalog.tsx` reicht `subjects` schon durch; es besteht die Versuchung, `isMine` dort zu filtern
  statt in `CatalogAdmin` zu lesen. Das wäre falsch: die Auswahlliste **muss** fremde Fächer weiter zeigen
  (lesen darf jeder, und die Arten darunter sind für alle bedienbar).

**Angriffsplan** (kein Backend-Anteil, daher nur Frontend):

1. `CatalogAdmin.tsx`: `SubjectResponse` liefert `isMine`/`ownerAdultId` schon — die `NameRow` des Fachs in
   `{subject.isMine && (…)}` fassen (Muster `VaterLehrwerke.tsx:183`).
2. Den `{!subject.isMine && (…)}`-Zweig mit dem erklärenden `<p className="muted">` ergänzen, die zwei
   Fälle über `subject.ownerAdultId === null` unterscheiden.
3. Den sichtbaren Absatz `:61-63` neu formulieren (lesen alle / ändern der Eigentümer, ohne „Kapitel").
4. Den Datei-Kommentar `:9-24` an der Stelle korrigieren, die die Globalität als Grund nennt.
5. `CatalogAdmin.test.tsx` neu: drei Fach-Fixtures (eigen / fremd / ownerlos), je eine Zusicherung auf
   Vorhandensein bzw. Fehlen von „OK"/„Löschen" und auf den erklärenden Satz; dazu ein Fall, der belegt,
   dass die Art-Zeile in allen drei Zuständen ihre Knöpfe behält. Vorbild `VaterLehrwerke.test.tsx`.
6. `npm run build` und `npm test` laufen lassen; `frontend-reviewer` über den Diff.

**Testweg**: `CatalogAdmin.test.tsx` (neu, React Testing Library, Vorbild `VaterLehrwerke.test.tsx`) für die
Akzeptanzkriterien 1–4 und 7; `npm run build` als Typecheck für 8. **Rollengang**: Live-Probe gegen die
laufende App auf `/vater/katalog` — Seed-Fach „Englisch" zeigt keine Bearbeitungszeile, aber den Satz; ein
selbst angelegtes Fach zeigt sie. Nach `docs/nachtlauf.md` Freigabe 6 im Browser, wenn die Chrome-Verbindung
steht; sonst der benannte Ersatz. Keine E2E-Spec (Entscheidung 6, mit Kosten benannt).

## Verlauf

- **2026-08-11** — angelegt beim Bau von B-13 (dessen Entscheidung 5 hat den Fall benannt, aber bewusst nicht
  gebaut; das Ziel von B-13 ist ohne diese Story erfüllt).
- **2026-08-12** — ausformuliert (Nachtlauf, Sprint A): Ist-Stand am Code belegt. Zwei Dinge kamen dabei
  heraus, die die Idee nicht hatte: die **„Art" ist nicht betroffen** (`ExerciseCategory` trägt gar kein
  Eigentümer-Feld, `LearnEntities.cs:40-46`) — der Schnitt ist also kleiner als „die Katalogseite"; und im
  selben Bauteil stehen **zwei seit B-13 falsche Behauptungen**, eine davon als sichtbarer UI-Text
  (`CatalogAdmin.tsx:62`). `unverifiziert` entfernt.
- **2026-08-12** — gegrillt (autonom, `art: Defekt`, Freigabe 1 des Nachtlaufs): sechs Entscheidungen mit
  Begründung und Kosten. Der Nebenfund `isMine` vs. `isOwn` (ein Ausreißer gegen sieben) wurde **nicht**
  mitgeschluckt, sondern nach der Regel des Bereichs als [B-156](B-156-ismine-heisst-anderswo-isown.md)
  ausgelagert.
- **2026-08-12** — geschätzt (Nachtlauf): `S`, `wo: frontend`, `migration: nein`, `vertragsbruch: nein`.
  Testweg Komponententest statt E2E, mit den Kosten dieser Wahl (kein wiederholbarer Browser-Weg) benannt.
- **2026-08-12** — gebaut (Nachtlauf, Sprint A). Die Fach-Zeile liegt jetzt in einem **exportierten**
  Baustein `SubjectRow`; er wurde exportiert, weil `CatalogAdmin` beim Fachwechsel die Arten nachlädt und
  damit am Netz hängt — kein Test dieses Frontends mockt `../lib/api`, und das bleibt so (Vorbild:
  `VaterLehrwerke.test.tsx` rendert das exportierte `UnitForm`). Damit prüft der Test die **Bindung** an
  `isMine`, nicht eine Hilfsfunktion daneben. Dazu die zwei Textkorrekturen (Entscheidung 4).
  **Rote Probe:** mit entfernter Bedingung (`if (true || subject.isMine)`) fallen **3 von 5** Fällen — genau
  die drei Nicht-Eigentümer-Fälle; die beiden anderen (eigenes Fach, `NameRow`) bleiben grün, wie sie sollen.
- **2026-08-12** — **Rollengang im echten Browser** (Freigabe 6, Chrome-Verbindung stand): eigener Server
  auf `:5200` mit Wegwerf-DB, **nach** der letzten Änderung gestartet (die Regel aus dem 2026-08-10er Lauf),
  Frontend auf `:5173`. Die drei Zustände wurden über die **echte API** hergestellt, nicht per SQL:
  eigenes Fach (`owner=1`), fremdes Fach eines zweiten registrierten Creators (`owner=4`), Seed-Fach
  „Englisch" (`owner=null`). Gesehen: am Seed-Fach **kein** Feld und **keine** Knöpfe, dafür „gehört zum
  Grundbestand"; am fremden Fach „hat jemand anderes angelegt"; am eigenen Fach „FACH UMBENENNEN" samt
  „Löschen". Die Art-Zeilen behalten in allen Fällen ihr „Löschen" (AK 4).
  **Ehrliche Grenze des Gangs:** Der OK-Knopf ließ sich nicht klicken — die CDP-Tastenanschläge erreichten
  das Eingabefeld in dieser Sitzung nach drei Versuchen nicht (Werkzeug-Artefakt, kein Produktbefund: das
  Feld blieb unverändert, der Fokusring stand auf dem Auswahlfeld). Der Schreibweg ist stattdessen
  **serverseitig** belegt (`PATCH` eigenes Fach → `200`, fremdes → `403 not_owner`, Seed → `403 not_owner`)
  **und** seit dem Reviewer-Fund im Komponententest (siehe unten).
- **2026-08-12** — **Fund im eigenen Rollengang, sofort behoben** (Freigabe 3): eine Zeile **über** der
  Karte versprach weiter „Hier legst du sie an, benennst sie um und löschst sie"
  (`VaterKatalog.tsx:22-25`) — für die vier ownerlosen Seed-Fächer gilt beides nicht. Der Bildschirm
  widersprach sich damit zwei Absätze weit. Das ist die Fehlerklasse aus Entscheidung 4, nur eine Datei
  weiter; darum im selben Schnitt behoben statt ausgelagert (das Ziel der Story ist mit dem Satz nicht
  erfüllt). Der `frontend-reviewer` hat denselben Fund unabhängig gemeldet, mit fast demselben Wortlaut.
- **2026-08-12** — `geschaetzt → abgenommen`. **`frontend-reviewer`: kein abnahmeblockierender Fund**,
  AK 1–8 erfüllt. Er hat außerdem zwei Dinge bestätigt, die ich sonst nur behauptet hätte: `== null` ist
  richtig **und** besser als das `=== null` meines Angriffsplans (der Vertrag gibt `ownerAdultId` optional),
  und die fehlende Live-Region am erklärenden Satz ist **richtig** — sie entsteht mit ihrem Text in einem
  per `key` neu montierten Teilbaum und wäre nach der Messung aus B-132 garantiert stumm.
  **Zwei nicht blockierende Funde mitgenommen:** (1) `expect(okKnopf()).toBeNull()` konnte **nie**
  fehlschlagen — „OK" erscheint nur bei `dirty`, beim Mount also niemals. Jetzt tragend gemacht: tippen →
  OK erscheint → klicken → `onSave` bekommt den neuen Namen. Damit pinnt der Test genau den Weg, den der
  Browser mir verweigert hat. (2) Die Begründung in meinem neuen `key`-Kommentar hielt nicht (`SubjectRow`
  hält selbst keinen State) — korrigiert, weil ein Kommentar mit nicht tragender Begründung genau die
  Klasse ist, die diese Story oben repariert.
  **Belege:** `npx vitest run` **256/256 grün** (35 Dateien, davon 5 neu), `npm run build` (tsc -b + vite)
  grün, `markdownlint-cli2` 0 Issues über 253 Dateien. Backend unberührt (keine Server-Datei angefasst),
  Suite dort weiter 821/821.
  **Ein Folgefund als eigene Story:** [B-159](B-159-reihe-ohne-owner-behauptet-fremden-ersteller.md) — bei
  der Lehrwerk-Reihe fehlt dieselbe Unterscheidung eine Ebene höher (ownerlose Reihe behauptet einen
  fremden Ersteller). Selbst am Code nachgesehen, nicht aus dem Review übernommen.
- 2026-08-13 · **Nachschau: ein Fund**, und er ist erst *nach* dieser Story falsch geworden: der Vorspann-Satz „umbenennen und löschen kannst du, was du selbst angelegt hast" gilt seit B-157 fuer Arten nicht mehr und widerspricht der Karte zwei Absätze darunter → [B-170](B-170-selbst-angelegte-art-im-grundbestand-ist-unlöschbar.md). Ein zweiter Fund (`categories.error` wird nirgends gezeigt) ging als [B-174](B-174-arten-liste-verschweigt-ihren-fehler.md) heraus; seine Alt-Daten-Hälfte liegt schon als [B-164](B-164-useasync-paart-frischen-zustand-mit-alten-daten.md) vor und wurde **nicht** doppelt angelegt. Sauber befunden: die Richtung des Gates ist fail-closed, `isMine: true` bei `ownerAdultId == null` kann serverseitig nicht entstehen, und keiner der fuenf Fälle prueft den Ausgangszustand.
