---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/ui, rolle/supervisor, rolle/student]
aliases: [Fächer-Verteilung Frontend, Schieberegler Fächer, Regler im Vater-Web]
status: geschaetzt
wartet_auf: "B-644 (Tagesplaner) – nicht bauen, bis B-644 gegrillt ist"
prio: P2
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-616
grund: ""
ersetzt_durch: []
---

# B-619 · Der Vater stellt die Fächer-Verteilung mit Reglern ein, und der Sohn sieht sie

Teil 2 von zwei aus [B-616](B-616-faecher-gewichtung-in-prozent.md) (dort gegrillt, beim Schätzen geteilt). Das Vater-Web bekommt am Kind das
Tagesbudget neuer Wörter und die Fächerliste mit Prozent-Reglern, die zusammen 100 halten; die Sohn-App zeigt
die Verteilung lesend. Voraussetzung: [B-618](B-618-faecher-verteilung-backend.md) ist gebaut, diese Story hängt an dessen API.

## User Story

Als *Vater* möchte ich die Verteilung der Fächer meines Kindes mit Reglern einstellen und sehen, wenn ein Anteil
ins Leere läuft, damit ich die Gewichtung ohne API-Aufrufe pflege. Als *Sohn* möchte ich sehen, wie meine Übung
auf die Fächer verteilt ist, damit ich verstehe, warum ich heute mehr Französisch als Englisch bekomme.

## Ist-Stand am Code

Gemessen am 2026-11-17 auf `main` (Stand `bf5fd841`), nur so weit, wie es die Teilung von B-616 brauchte. Beim
Schätzen am 2026-11-19 nachgemessen: Die Sohn-App hat Routen `progress`, `shop`, `konto`, `ich` u. a.
(`frontend/src/sohn/SohnApp.tsx:169-182`), ein schreibgeschütztes Feld gibt es als `readOnly` in `Lektion.tsx`;
`VaterKind.tsx` ist weiter 560 Zeilen mit den Reitern Profil, Schule, Interessen, Einstellungen, Betreuer
(`:26`).

- Im Frontend gibt es keinen Schieberegler: die Suche nach `type="range"` unter `frontend/src` findet nichts.
- Die Kind-Seite des Vaters ist `frontend/src/vater/VaterKind.tsx` (560 Zeilen); dort wird auch
  `BonusDailyCoinCap` gepflegt, das Vorbild für ein Feld, das nur der Vater setzt.
- Die Startseite des Sohns ist `frontend/src/sohn/SohnHome.tsx`, sein Lernstand `frontend/src/sohn/SohnProgress.tsx`.
- Die API, an der diese Story hängt, existiert noch nicht (B-618).

## Die echte Lücke

Es gibt keine Oberfläche für die Fächer-Verteilung, und kein Regler-Bauteil, auf dem sie aufsetzen könnte.

## Offene Punkte

Keine Produktfragen offen; sie sind in [B-616](B-616-faecher-gewichtung-in-prozent.md) entschieden. Für diese Story gelten daraus:

1. ~~**Wer setzt?**~~ → B-616 Entscheidung 3: nur der Vater, das Kind liest.
2. ~~**Summe?**~~ → B-616 Entscheidung 11: genau 100, die Regler halten die Summe selbst.
3. ~~**Leerer Anteil?**~~ → B-616 Entscheidung 7: verfällt, das Vater-Web warnt.
4. ~~**Positionen außerhalb der Liste?**~~ → B-616 Entscheidung 12: laufen wie heute, das Vater-Web zeigt sie an.

## Entscheidungen

Übernommen aus [B-616](B-616-faecher-gewichtung-in-prozent.md), gegrillt am 2026-11-17 mit dem Vater. Begründung und Kosten stehen dort und
wortgleich in [B-618](B-618-faecher-verteilung-backend.md); hier nur, was die Oberfläche bindet.

1. **Nur der Vater stellt ein, das Kind liest** (B-616 Entscheidung 3). *Begründung:* Das Kind soll ein
   ungeliebtes Fach nicht kleinrechnen. *Kosten:* keine Mitsprache des Kindes.
2. **Die Regler halten zusammen genau 100** (B-616 Entscheidung 11). *Begründung:* Das ganze Budget ist immer
   vergeben, und der Server weist jede andere Summe ab. *Kosten:* Ein Regler bewegt die anderen mit; die Liste
   wird immer als Ganzes gespeichert.
3. **Das Vater-Web warnt bei einem Anteil ohne aktiven Plan** (B-616 Entscheidung 7) **und zeigt Positionen
   außerhalb der Fächerliste an** (B-616 Entscheidung 12). *Begründung:* Beides ändert die Wirkung des Budgets,
   ohne dass der Vater es sonst bemerkt. *Kosten:* zwei Hinweise mehr auf der Kind-Seite.
4. **Der Verlauf der Stände ist in dieser Story nur lesbar, nicht als Diagramm** (B-616 Entscheidung 8: ein
   Stand je Änderung). *Begründung:* Die Stände sind treppenförmig und selten; eine Liste genügt. *Kosten:*
   kein Bild der Verschiebung über die Zeit.

## Akzeptanzkriterien

Final. Die ursprünglichen AK 1 bis 4 (B-616 AK 13 und 14, dazu Folgerungen aus den Entscheidungen 3 und 4) sind beim
Schätzen am 2026-11-19 neu gefasst und auf fünf geteilt: Die alten AK 1 und 2 („rendert die Werte nicht“, „fehlt
oder ist editierbar“) trugen im Ausgangszustand nichts, weil die Fläche dort gar nicht existiert. Je Kriterium
steht jetzt die Änderung, die seinen Test rot macht.

- **AK 1 (Regler halten 100).** Bewegt der Vater einen Regler, bleibt die Summe aller Anteile genau 100; die
  übrigen verschieben sich anteilig, der Rundungsrest geht an das größte Fach (dieselbe Regel wie B-618
  Entscheidung 10). *Rot, wenn die Umrechnung nicht rundet (Summe 99 oder 101) oder ein Regler auf 100 die
  anderen nicht auf 0 zieht.* Geprüft an der reinen Funktion, nicht am DOM.
- **AK 2 (Speichern und Neuladen).** „Speichern“ schickt Budget und Liste als Ganzes; nach `page.reload()` stehen
  die gespeicherten Werte aus dem Server in Reglern und Budgetfeld. Der Knopf ist gesperrt, solange nichts
  geändert ist. *Rot, wenn die Werte nur im Formularzustand liegen (nach dem Neuladen wieder die Vorgabe) oder
  wenn die Liste fachweise statt als Ganzes geschickt wird.*
- **AK 3 (Sohn liest nur).** Die Sohn-App zeigt Fach und Anteil je Zeile als Text. Im DOM dieser Fläche gibt es
  weder einen `slider` noch ein Eingabefeld. Ein Kind ohne Verteilung sieht keinen leeren Kasten. *Rot, wenn die
  Regler-Komponente wiederverwendet wird (ein `slider` taucht auf) oder der Kasten ohne Verteilung erscheint.*
- **AK 4 (Hinweise nur bei Meldung).** Meldet die API für ein Fach einen Anteil ohne aktiven Plan, nennt die
  Kind-Seite des Vaters den Namen dieses Fachs; meldet sie eine Vokabel-Position außerhalb der Liste, nennt sie
  diese Position. Meldet sie nichts, erscheint kein Hinweis. *Rot, wenn der Hinweis fehlt, obwohl die Antwort ihn
  trägt, oder immer steht.*
- **AK 5 (Stände).** Die Kind-Seite listet die datierten Stände, den neuesten zuerst, je mit Datum und den Anteilen
  nach Fach. *Rot, wenn nur der aktuelle Stand erscheint oder die Reihenfolge aufsteigend ist.*

## Schätzung

**Größe: M** – wie der Batch-Pfad im `MediaSelector`: zwei Flächen (Vater-Web, Sohn-App), eine neue reine
Rechnung (Regler-Umrechnung), kein Schema. Näher an L als an S, weil zwei Oberflächen mit Tests und die Sohn-App
mit Übersetzung (`de.ts`/`en.ts`) dazukommen; die Regler-Umrechnung ist die einzige Stelle mit Logik.

- **`wo: frontend`** – die Story hängt an der API aus B-618, schreibt aber selbst nur unter `frontend/`.
- **`migration: nein`** – kein Schema berührt.
- **`vertragsbruch: nein`** – das Frontend liest nur additive Felder; `src/lib/contract.ts` wird aus
  `docs/openapi/v1.json` erzeugt und ist gitignored (`frontend/CLAUDE.md`), also nach B-618 mit
  `npm run gen:contract` nachziehen, nicht von Hand.

### Annahmen (stellvertretend im Vorbereitungslauf 2026-11-19, Sonnet, unbestätigt)

Der Grill von B-616 hat diese drei Punkte nicht entschieden. Sie sind klein und umkehrbar, aber der Vater hat sie
nicht gesehen.

1. **Die Regler rechnen anteilig um.** Zieht der Vater einen Regler, werden die übrigen im Verhältnis ihrer
   bisherigen Werte skaliert. Verworfen: gleichmäßig verteilen, denn ein Fach auf 0 % bekäme dabei ungewollt Anteil
   zurück. Ausgeschlossen ist damit der Fall, dass ein anderer Regler ein vom Vater auf 0 gestelltes Fach wieder
   anhebt. Kosten: Stehen alle übrigen auf 0, bleibt für den Rest kein Träger; dann zieht ein Regler unter 100 den
   Rest auf das größte Fach (eigener Testfall).
2. **Ort im Vater-Web ist der Reiter „Schule“** der Kind-Seite (`CHILD_TABS`, `frontend/src/vater/VaterKind.tsx:26`),
   neben dem Stundenplan: Die Fächerliste beschreibt, was dran ist. Verworfen: „Einstellungen“, wo Bild-Freigabe
   und Bonus-Deckel stehen, also Grenzen und keine Fächer.
3. **Ort in der Sohn-App ist `frontend/src/sohn/SohnProgress.tsx`** (Lernstand), nicht die Startseite. Verworfen:
   `SohnHome.tsx` (494 Zeilen), weil der Tagesplaner B-644 sie ohnehin umbaut. Kosten: Das Kind sieht die
   Verteilung erst beim Blick in den Lernstand, nicht auf dem Weg zur Übung.

### Risiken

1. **Die Story ist angehalten.** B-644 (`ausformuliert`) kann Verteilung und Pflicht in den Tagesplaner verlegen.
   Die Schätzung gilt nur, wenn B-618 wie gegrillt gebaut wird. Nach dem Halt-Vermerk bleiben Stufe und
   Entscheidungen bestehen; die Story wird danach überführt oder verworfen.
2. **Die API aus B-618 ist nicht gebaut**, ihre Feldnamen existieren nicht. AK 4 und AK 5 hängen an zwei noch
   unbenannten Meldungen und der Standfolge. Nach B-618 gegen den echten OpenAPI-Stand nachsehen.
3. **Lücken in B-618, die hier sichtbar werden:** (a) Es steht nirgends, wie der Vater das Tagesbudget wieder
   *entfernt* (leer heißt keine Verteilung, Entscheidung 4); das `clearBonusDailyCoinCap`-Muster fehlt in der
   Story. (b) Die zulässige Spanne des Budgets ist nicht angegeben (`NewItemsPerDay` ist 0 bis 200). Ohne beides
   lässt sich das Feld nicht so validieren wie `BonusSection` (`frontend/src/vater/VaterKind.tsx:348-405`).
4. **Regler und Zugänglichkeit:** `type="range"` gibt es im Frontend noch nirgends. Er braucht `aria-valuetext`
   („Französisch 60 Prozent“), Tastenbedienung und einen Namen je Regler, sonst bricht die A11y-Prüfung
   (`frontend/CLAUDE.md`).
5. **Wechsel des Kindes:** Wie `BonusSection` hält das Formular seine Werte lokal; `key={child.id}` ist Pflicht,
   sonst trägt das nächste Kind die Regler des vorigen.

### Angriffsplan

Voraussetzung: **B-618 gebaut** (API, OpenAPI-Dokument neu erzeugt, `npm run gen:contract`).

1. `frontend/src/lib/api.ts`: Aufrufe für Liste, Budget, Stände und Kind-Lesen nach dem Muster der vorhandenen
   Kind-Aufrufe.
2. Reine Funktion `verteileRegler(anteile, geaendertesFach, neuerWert)` unter `frontend/src/vater/` (Annahme 1) mit
   eigenem Test, bevor eine Komponente existiert.
3. Vater-Komponente `FaecherVerteilung` im Reiter „Schule“: Budgetfeld (Muster `BonusSection`: `useAction`,
   `StatusBanner`, `disabled={busy}`, `FieldLabel` mit Eintrag in `fieldHelp.ts`), Zeile je Fach mit Regler, Fach
   hinzufügen und entfernen, Hinweise, Standliste. `ListenAusfall` für jede Abfrage.
4. Sohn-Block in `SohnProgress.tsx`: Texte über `t()` mit Einträgen in `src/i18n/de.ts` und `en.ts`.
5. E2E, danach `tsc`, Lint und Vitest über die lokalen Binaries (`./node_modules/.bin/…`, nicht `npx`).

### Testweg

- `faecherVerteilung.test.ts` (neu, Vitest): die reine Funktion – Summe 100 nach jedem Zug, Rundungsrest, Regler
  auf 100, alle übrigen 0 (AK 1). Rote Probe je Fall: Rundung entfernen; anteilige Skalierung durch gleichmäßige
  ersetzen.
- `FaecherVerteilung.test.tsx` (neu, Vitest, Vorbild `VaterKind.test.tsx`): Speichern schickt die Liste als Ganzes
  und ist ohne Änderung gesperrt (AK 2); Hinweise nur bei Meldung, mit Fachname (AK 4); Standliste absteigend (AK 5).
- `SohnProgress.test.tsx` (vorhanden, ergänzen): kein `slider`, keine Eingabefelder, kein Kasten ohne Verteilung
  (AK 3); dazu `i18n.test.ts` (Parität de/en) und `fieldHelp.test.ts`.
- `faecher-verteilung.spec.ts` (neu, E2E, Vorbild `bonus-deckel.spec.ts` und `zwei-faecher.spec.ts`): Vater setzt
  Budget und zwei Regler, speichert, lädt neu (AK 2), meldet sich als Sohn an und sieht die Zeilen ohne Regler
  (AK 3). Die Rechenregeln dahinter prüft B-618 in `FaecherVerteilungTests`.
- Vor der Abnahme: Agent `frontend-reviewer` und der Rollengang als Vater und Sohn. Im `## Verlauf` von B-618
  steht, dass der Gang an der Oberfläche erst mit dieser Story möglich ist.

## Verlauf

- **2026-11-17** — angelegt als Teil 2 der Teilung von B-616 (Schnitt vom Vater gewählt), Stufe `gegrillt`:
  Die Entscheidungen stammen aus der Grill-Runde zu B-616. Entscheidung 4 und die AKs 3 und 4 hat der Assistent
  aus den dortigen Entscheidungen 7, 8 und 12 abgeleitet; vom Vater nicht einzeln bestätigt. Die Schätzung steht
  aus, weil sie die Frontend-Messung und die fertige API aus B-618 braucht.
- **2026-11-17** — **Entscheidung des Vaters (Grill-Runde B-644, Entscheidung 6): angehalten.** Die Story wird nicht gebaut, bis [B-644](B-644-tagesplaner-statt-fester-lauf.md) gegrillt ist: der Tagesplaner verlegt die Pflicht von der Position an die Tagesaufgabe. Stufe und Entscheidungen bleiben bestehen; danach wird sie als Regel des Planers überführt oder verworfen.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): M, frontend, keine Migration, kein Vertragsbruch. AK 1 bis 5 neu gefasst, je mit einer benannten roten Änderung (die alten AK 1 und 2 trugen im Ausgangszustand nichts). Drei Annahmen stellvertretend und unbestätigt (anteilige Regler-Umrechnung, Reiter „Schule“, Sohn-Block im Lernstand). Lücken in B-618: Budget-Entfernen und Budget-Spanne fehlen. `wartet_auf` um die Bestätigung ergänzt; der Halt auf B-644 gilt unverändert.
- **2026-11-19** — Die drei Annahmen vom Nutzer bestätigt: anteilige Regler-Umrechnung, Vater-Ort Reiter „Schule“, Sohn-Block im Lernstand. `wartet_auf: B-644` bleibt, nur die Bestätigung der Sonnet-Annahmen ist erledigt.
