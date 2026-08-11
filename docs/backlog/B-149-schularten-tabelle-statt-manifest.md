---
tags: [typ/story, status/gegrillt, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [SCHOOL_TYPES handgepflegt, Schularten ohne Manifest, Enum-Kopie im Frontend]
status: gegrillt
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer im Nachtlauf Sprint 3 (2026-08-10)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-149 · Die Schularten-Liste ist eine handgepflegte Kopie eines Server-Enums

## User Story

Als **Entwickler** möchte ich, dass eine neue Schulart im Server sie auch im Frontend erscheinen lässt —
ohne dass jemand daran denken muss.

## Ist-Stand am Code

Stand `2dd5a78`, alles nachgemessen.

**Die Liste.** `frontend/src/lib/labels.ts:31` hält `SCHOOL_TYPES` als Literal mit sechs Werten
(`Grundschule, Hauptschule, Realschule, Gymnasium, Gesamtschule, Berufsschule`); das Server-Enum
`SchoolTypes` führt dieselben sechs plus `None`. Der Typ hilft nicht: `SchoolTypes` kommt im generierten
Schema als nackter `string` an, `SCHOOL_TYPES.includes(...)` ist also eine reine **Laufzeit**-Prüfung
ohne Compiler-Netz.

**Das Dokument trägt die Werte NICHT maschinenlesbar — und das ist Absicht.**
`backend/Pugling.Api/Program.cs:313-314` lässt `schema.Enum` für `[Flags]`-Typen bewusst weg, mit
Begründung aus [B-60](B-60-flags-enum-im-dokument.md): Eine Enum-Liste der Einzelnamen wiese genau die
Kombinationen zurück, die der Server täglich sendet. Übrig bleiben die Werte nur in der **Prosa** der
Beschreibung („Comma-separated combination of: None, Grundschule, …", `Program.cs:316`). Damit ist der
billigste der drei Wege aus dem ersten offenen Punkt gestorben: aus dem Dokument ist die Liste nur durch
Parsen eines englischen Satzes zu gewinnen.

**Dreizehn Fundstellen in sieben Dateien, davon vier entscheidungstragend — in drei verschiedenen
Verhaltensweisen.** Das ist
der eigentliche Fund dieser Recherche: Die Liste ist nicht einmal, sondern viermal Schiedsrichter, und
die vier Stellen sind sich uneinig, was mit einem unbekannten Wert geschieht.

| Stelle | Was sie tut | Verhalten bei einer neuen Server-Schulart |
| --- | --- | --- |
| `VaterLehrwerke.tsx:319` (B-143) | gesperrte Option, wenn nicht in der Liste | zeigt den Einzelwert als „Kombination", **unwählbar** |
| `VaterFachlehrer.tsx:327` (B-148) | dieselbe Zeile, seit heute | dasselbe |
| `ExerciseEditModal.tsx:142-143` | zerlegt den String **zuerst**, filtert dann | **verwirft den Wert still** beim Laden (Kombinationen sind hier in Ordnung — das Feld ist eine Checkbox-Gruppe) |
| `VaterKind.tsx:150,186` | normalisiert auf `"None"`, vergleicht gegen den Anzeigewert | Wert überlebt, `None` ist **nicht mehr herstellbar** — und das gilt **heute schon**, weil `Child.SchoolType` dasselbe `[Flags]`-Enum ist (`AdminEntities.cs:71`) und eine Kombination tragen darf |

Die übrigen **neun** sind reine `map`-Aufrufe fürs Pulldown (`ExerciseEditModal.tsx:211`,
`ExerciseFilterBar.tsx:91`, `VaterExerciseCreate.tsx:237`, `VaterFachlehrer.tsx:330`, `VaterKind.tsx:229`,
`VaterLehrwerke.tsx:77,322,658`, `VaterWizard.tsx:255`) — dort fehlt der neue Wert nur zur Auswahl, was
unschön, aber harmlos ist.

**Ein Enum, zwei Rollen** (in der Grill-Runde geschärft). Bei Übung, Reihe und Profil ist `SchoolTypes`
ein **Filter**: Eine Kombination ist sinnvoll, `None` heißt „für alle" (`LearnBaseTypes.cs`, *„no filter
exclusion"*). Beim **Kind** ist es ein **Attribut**: Ein Kind besucht eine Schulart, eine Kombination ist
ein Modellierungsunfall, und `None` heißt „nicht angegeben" (`VaterKind.tsx:228` beschriftet es so). Die
vier Stellen sind sich also nicht aus Schlamperei uneinig — sie beantworten verschiedene Fragen mit
demselben Typ. `labels.ts:30` behauptet dazu global „`None` ist *nicht gesetzt*", was nur fürs Kind gilt.

**Ein Guard-Test hätte kein Vorbild in C# — und das Vorbild, das es gibt, ist teuer.** Kein Test in
`Pugling.Api.Tests` liest heute eine Frontend-Datei (`ContractDocumentTests.cs` erwähnt das Frontend nur
in Kommentaren). Das etablierte Muster „Server-Liste gegen Oberfläche" liegt auf der **E2E**-Seite:
`frontend/e2e/uebungstypen.spec.ts` zählt die Einträge im Typ-Pulldown gegen die Manifest-Liste. Das
nächstliegende C#-Vorbild, `ClientRouteGuardTests.cs`, ist **487 Zeilen** handgeschriebener Parser mit
Rot-Liste (`unreadable`) und einer Datei-Untergrenze (`files.Count >= 11`) — eine Zusicherung mit
Verfallsdatum.

**Ein Nicht-`[Flags]`-Enum landet als echter Union-Typ im generierten Vertrag** —
`ExerciseCheckMode: "None" | "StudyPlanTest" | …` (`contract.ts:22495`). Nur `SchoolTypes` bleibt
`string`. Die Frage ist damit nicht, ob das Dokument die Werte *trägt*, sondern ob es sie **billig tragen
könnte** — das erste war gemessen, das zweite nicht.

## Die echte Lücke

Dieselbe Klasse, gegen die die Manifest-Regel für Übungstypen geschrieben ist
(`frontend/CLAUDE.md`: *„Übungstypen kommen aus dem Server-Manifest"*, weil drei Kopien zwangsläufig
auseinanderliefen). Hier ist es **eine** Kopie — aber sie hat vier Leser, die aus ihr drei verschiedene
Antworten ableiten. Der Schaden ist damit nicht mehr eine fehlende Beschriftung, sondern je nach Stelle
ein gesperrtes Bedienelement, ein still verworfener Wert oder ein unerreichbarer Zustand.

Und die Lücke ist breiter, als der Titel sagt: Selbst wenn die Liste morgen vom Server käme, blieben die
vier Stellen uneinig darüber, **was ein unbekannter Wert bedeutet**. Die Herkunft der Liste und die
Semantik „unbekannter Wert" sind zwei Fragen, und nur die erste steht im Titel.

**Kein `entgangen_bei`:** Die Liste ist alt; B-143 und B-148 haben ihr nur Aufgaben gegeben, für die sie
nicht gedacht war.

## Offene Punkte

1. ~~**Woher kommt die Liste künftig?** … zuerst nachsehen, ob das OpenAPI-Dokument die Einzelwerte
   ausweist — davon hängt ab, ob es geschenkt ist.~~ **Gemessen am 2026-08-11: nein.** `Program.cs:313`
   lässt `schema.Enum` für `[Flags]` bewusst weg (B-60), die Werte stehen nur in der englischen
   Beschreibung. Damit bleiben **zwei** Wege: ein Endpunkt (Muster `ExerciseTypesController`) oder ein
   Tor, das die handgepflegte Liste stehen lässt und bewacht.
   **Empfehlung: das Tor.** Ein Endpunkt kostet Route, DTO, Client-Methode und einen Ladezustand in
   sieben Dateien; er macht eine Liste asynchron, die heute beim Rendern einfach dasteht — für sechs
   Werte, die sich seit dem ersten Commit nicht geändert haben. Das Tor kostet eine Datei und meldet den einen
   Fall, um den es geht.
2. ~~**Wenn Tor: wo?** C#-Test oder E2E.~~ **Beide verworfen** — die Grill-Runde hat einen dritten Weg
   freigelegt, den die Ausformulierung übersehen hatte. → Entscheidung 2.
3. ~~**Gehört die Uneinigkeit der vier Stellen in diese Story?**~~ → Entscheidung 4.
4. ~~**`VaterKind.tsx:150,186` trägt dieselbe Unerreichbarkeit wie die B-148-Regression.**~~ →
   Entscheidung 4; die Ursache liegt tiefer als vermutet (ein Enum, zwei Rollen).

## Entscheidungen

1. **Die Liste bleibt handgepflegt, ein Tor bewacht sie** — keine Ablösung durch eine Server-Quelle.
   **Begründung:** Der Vergleich mit dem Übungstyp-Manifest trägt nicht. Dort waren es **drei** Kopien,
   die schon auseinandergelaufen *waren* (Server zwölf Typen, UI sechs), und zwischen Schlüssel und Route
   war zu übersetzen (Aufsatz → `essays`). Hier ist es **eine** Kopie von sechs Werten, die in der
   gesamten Historie des Enums (vier Commits, alle Umzüge und Übersetzungen) nie gewandert sind — und der
   Enum-Name *ist* die Beschriftung, es gibt nichts zu übersetzen. **Kosten:** Die Kopie bleibt eine
   Kopie. Das Tor meldet, es repariert nicht — jemand muss `labels.ts` von Hand nachziehen.
2. **Das Tor ist der Compiler, nicht ein Test.** Der Schema-Transformer gibt für ein `[Flags]`-Enum
   zusätzlich ein **Geschwister-Schema** mit den Einzelnamen als echtem `enum` heraus; `SchoolTypes`
   selbst bleibt unangetastet `string`. Der Generator macht daraus einen Union-Typ, und `labels.ts`
   leitet die Liste **erschöpfend** daraus ab (`Record<Exclude<…, "None">, …>`).
   **Begründung:** Ein C#-Test, der `labels.ts` parst, erbt die Kostenklasse von `ClientRouteGuardTests`
   (487 Zeilen, Rot-Liste, verrottende Untergrenze); ein E2E liefe nur im langsamen Lauf und bräuchte
   einen Bildschirm, der alle Werte zeigt. Der Compiler schließt zusätzlich die Lücke aus Entscheidung 1:
   „wer nur `npm run build` fährt, merkt nichts" gilt dann nicht mehr. **Kosten:** Das Backend wird
   angefasst, `wo` ist damit `beides` (Backend zuerst). `labels.ts` bekommt eine ungewohntere Form als ein
   schlichtes Array und braucht einen Satz Begründung. Und **eine Unbekannte bleibt**: ob
   `openapi-typescript` ein Schema herausgibt, das keine Operation referenziert. Tut es das nicht, wird
   dieser Weg teurer als hier veranschlagt — das gehört als **erster** Schritt in den Angriffsplan.
3. **`None` ist zweideutig, und das bleibt so — aber es wird benannt.** Bei Übung, Reihe und Profil heißt
   es „für alle" (Filter ohne Einschränkung), beim Kind „nicht angegeben" (Attribut unbekannt).
   **Begründung:** Beide Lesarten sind an ihrer Stelle richtig; `labels.ts:30` behauptet global die
   zweite und ist damit für die Reihe falsch. Der Kommentar wird geschärft, und die Ausnahme steht künftig
   nicht als Prosa, sondern als `Exclude<…, "None">` **im Typ** — an der Stelle, an der sie gilt.
   **Kosten:** Ein Satz mehr Kommentar, und die Zweideutigkeit bleibt bestehen; sie ist ab jetzt nur
   dokumentiert statt unsichtbar. Ob sie *aufzulösen* wäre, geht an Entscheidung 4.
4. **Die Uneinigkeit der vier Stellen wird abgespalten — als `art: Frage`, nicht `Aufräumen`.**
   **Begründung:** Das Ziel dieser Story ist nach Entscheidung 2 erfüllt, ohne dass eine der vier Stellen
   angefasst wird (dieselbe Regel, nach der B-148 tags zuvor B-151 abgespalten hat). `Frage` statt
   `Aufräumen`, weil offen ist, *ob* etwas zu tun ist: Ein denkbares Ergebnis ist „zwei Rollen an einem
   Enum sind hier billiger als zwei Typen — bleibt, mit einer Begründung im Vertragsprojekt", also ein
   `verworfen`. Als `Aufräumen` etikettiert („kein Verhalten ändert sich") wäre dieses Ergebnis gar nicht
   vorgesehen. **Kosten:** Ein `Frage`-Ticket darf ein autonomer Lauf nicht selbst grillen — es bleibt
   liegen, bis jemand darüber spricht. Und B-149 liefert ein Tor aus, das den Build rot macht, während
   drei Stellen weiter uneinig bleiben über einen Wert, der sie über **Deployment-Schieflage** doch
   erreichen kann (neuer Server, alte PWA im Cache). Ein bewusst offener Rest, kein übersehener.

## Akzeptanzkriterien

1. Ergänzt jemand einen Wert in `SchoolTypes`, wird **`tsc -b`** rot und nennt `labels.ts` als Fundort.
2. Entfernt jemand einen Wert aus `SchoolTypes`, ebenso.
3. `SchoolTypes` selbst trägt im Dokument weiterhin **kein** `enum` — B-60 bleibt ungebrochen, belegt
   durch die bestehende Zusicherung dazu.
4. Die `None`-Ausnahme steht als `Exclude<…, "None">` im Typ, nicht als Kommentar daneben.
5. Die Reihenfolge der Liste bleibt Anzeigeentscheidung des Frontends — geprüft wird die **Menge**.
6. Keine der dreizehn Fundstellen ändert ihr Verhalten. Diese Story ist Aufräumen, nicht
   Vereinheitlichung — die geht an die abgespaltene Story.

## Verlauf

- **2026-08-10** — angelegt aus dem Frontend-Review des Nachtlauf-Sprints 3. Nicht im Sprint behoben: der
  Fund betrifft eine Datei, die dieser Sprint gar nicht anfasst, und die Lösung ist eine eigene
  Entwurfsfrage.
- **2026-08-11** — **ausformuliert.** Zwei Messungen haben die Story verändert.
  **Erstens ist der billigste Weg tot:** Das OpenAPI-Dokument trägt die Einzelwerte eines
  `[Flags]`-Enums nicht maschinenlesbar, und zwar mit Vorsatz (`Program.cs:313-314`, Begründung aus
  B-60) — sie stehen nur in der englischen Beschreibung. Aus drei Wegen wurden zwei, und die Empfehlung
  kippt vom Manifest-Endpunkt auf ein **Tor**: Ein Endpunkt kostet Route, DTO, Client-Methode und einen
  `useAsync` in acht Formularen, für sechs Werte, die sich seit dem ersten Commit nicht geändert haben.
  **Zweitens ist der Fund größer als sein Titel:** Die Liste hat **dreizehn** Fundstellen in sieben
  Dateien, **vier** davon entscheidungstragend — und die vier leiten aus ihr **drei verschiedene** Antworten auf „unbekannter
  Wert" ab (gesperrte Option / stiller Verwurf in `ExerciseEditModal.tsx:143` / Unerreichbarkeit in
  `VaterKind.tsx:150,186`). Die Story nannte nur die erste. Das gehört als eigene Story abgespalten
  (offene Punkte 3 und 4) — es ist eine Produktfrage, keine Aufräumarbeit.
  Nebenbei belegt: `VaterKind` trägt dieselbe Unerreichbarkeit, die heute Vormittag in B-148 als
  Regression behoben wurde, und ihr Kommentar begründet nur die andere Hälfte.
- **2026-08-11** — **gegrillt** (Dialog, vier Entscheidungen). Die Runde hat die Empfehlung der
  Ausformulierung zweimal gekippt, beide Male an einem Fakt:
  **Der empfohlene C#-Test ist zu teuer.** Sein Vorbild `ClientRouteGuardTests.cs` ist **487 Zeilen**
  handgeschriebener Parser mit Rot-Liste und einer verrottenden Datei-Untergrenze — genau die
  Kostenklasse, die diese Story vermeiden will. **Und ein dritter Weg war übersehen:** Ein
  Nicht-`[Flags]`-Enum landet im generierten Vertrag als echter Union-Typ (`contract.ts:22495`), nur
  `SchoolTypes` bleibt `string`. Ich hatte gemessen, ob das Dokument die Werte *trägt* — nicht, ob es sie
  billig tragen **könnte**. Damit wird das Tor der **Compiler** statt eines Tests (Entscheidung 2), und
  `wo` wandert von `frontend` auf `beides`.
  Begrifflich geschärft: **`None` heißt zweierlei**, je nach Träger — „für alle" beim Filter (Übung,
  Reihe, Profil), „nicht angegeben" beim Attribut (Kind). `labels.ts:30` behauptet global die zweite
  Lesart. Die Ausnahme steht künftig als `Exclude<…, "None">` im Typ statt als Prosa daneben.
  Abgespalten: [B-152](B-152-schoolTypes-filter-und-attribut.md) (`art: Frage`) — die Uneinigkeit der
  vier Stellen, deren Ursache tiefer liegt als vermutet (ein Enum, zwei Rollen). Zwei Präzisierungen am
  Ist-Stand fielen dabei ab: `ExerciseEditModal` zerlegt zuerst (Kombinationen sind dort in Ordnung), und
  `VaterKind`s Unerreichbarkeit greift **heute schon**, weil ein Kind eine Kombination tragen darf.
