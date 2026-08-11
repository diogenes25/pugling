---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/katalog, rolle/supervisor, rolle/creator]
aliases: [ChildMaterialSection clearSubject, Lehrbuch verliert Fachnamen, B-143 am Kind, Fachlehrer verliert Fachnamen]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer im Nachtlauf Sprint 3 (2026-08-10), Fund neben dem Diff von B-143
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-148 · Das Lehrbuch-Formular am Kind zerstört den Fachnamen bei jedem Speichern

Dieselbe Fehlerklasse wie [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md), eine Datei weiter
— und **ohne** den Schutz, der dort gratis abfiel. Vom `frontend-reviewer` gefunden, während er B-143
prüfte.

## User Story

Als **Supervisor** möchte ich am Lehrbuch meines Kindes eine Notiz ändern können, ohne dabei die
Fachangabe zu verlieren, die ich nie angefasst habe.

## Ist-Stand am Code

Stand `d36a11a`, alles am Code nachgesehen statt aus dem Review-Befund übernommen.

**Der Defekt, an zwei Stellen statt einer.** Beide Formulare bauen den Lösch-Schalter aus dem
**aktuellen Formularwert** statt aus einem Vergleich gegen den Ladezustand:

- `frontend/src/vater/ChildMaterialSection.tsx:165-175` (Lehrbuch am Kind) —
  `clearSubject: dto.subjectId == null`, dazu `clearGrade`, `clearSeries`, `clearUnit` nach demselben Muster.
- `frontend/src/vater/VaterFachlehrer.tsx:245-251` (Fachlehrer-Profil) — **dieselbe Zeile**, dazu
  `clearSeries`, `clearGradeMin`, `clearGradeMax`. Das beantwortet den offenen Punkt 1: `CreatorProfile`
  trägt es, und zwar identisch.

Beide Fach-`<select>` kennen nur Katalog-Fächer plus „– keine Angabe –"
(`ChildMaterialSection.tsx:196-199`) — sie können den verwaisten Zustand nicht darstellen.

**Die Kette** (für beide gleich):

1. Ein Lehrbuch bzw. Profil, dessen Fach gelöscht wurde, hat `subjectId: null` und
   `subjectName: "Englisch"` — `SetNull` räumt nur die Id
   ([B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md) hat dieses Löschverhalten ausdrücklich als
   richtig bestätigt; der Name ist die gewollte Rückfallebene).
2. Das Formular startet auf `subjectId: ""`, weil es den Zustand nicht darstellen kann.
3. **Jedes** Speichern eines beliebigen anderen Feldes schickt `clearSubject: true`.
4. Der Name ist weg, und daneben steht „Gespeichert.".

**Der Server ist nicht die Ursache und braucht keine Änderung.** `TextbooksController.cs:110-117` wendet
erst den Wert, dann den Schalter an und leitet den Namen anschließend gegen den **Ergebnis**-Zustand ab
(B-142); `CreatorProfilesController.cs:148` ist gleich gebaut. `clearSubject` nimmt Id **und** Name
absichtlich zusammen (`ProfileDtos.cs:52`, `CreatorProfileDtos.cs:46`) — das ist die richtige Semantik
für „Fach entfernen". Falsch ist allein, dass der Client sie ungefragt schickt.

**Warum nur beim Fach.** Die übrigen Schalter derselben Rümpfe (`clearGrade`, `clearUnit`,
`clearGradeMin/Max`) tragen dieselbe Konstruktion, richten aber heute keinen Schaden an: Ihre Werte sind
im Formular vollständig darstellbar, `dto.X == null` heißt dort also tatsächlich „der Nutzer hat geleert".
Das Fach ist der einzige Fall mit einem Zustand, den die Auswahl nicht zeigen kann. `clearSeries` ist
**nicht nachgemessen** — ob eine Reihe fehlen kann, die das Buch referenziert, hängt daran, ob die
Reihenliste je gefiltert ankommt; das gehört in die Umsetzung, nicht in eine Vermutung hier.

## Die echte Lücke

Der Unterschied zu B-143 ist der entscheidende: Dort **schützte der Diff-Vergleich** — `form` blieb gleich
`loaded`, also ging nichts mit, und der Defekt war „man kommt nicht heran". Hier gibt es keinen Vergleich,
sondern eine Ableitung aus dem Momentanwert. Der Defekt ist damit nicht „man kommt nicht heran", sondern
**aktive Zerstörung bei einer unbeteiligten Handlung** — deshalb `P2` statt `P3`.

Und die Lücke ist eine Ebene tiefer, als der Befund sie beschrieb: Es fehlt nicht ein Sonderfall für das
Fach, sondern die **Regel**, dass ein `Clear…`-Schalter aus einem *Vergleich* entsteht. B-143 hat sie in
`seriesPatch.ts` für die Reihe schon gebaut und mit Tests belegt — sie steht dort als lokale Lösung einer
Datei, obwohl inzwischen drei Formulare dieselbe Semantik bedienen.

## Warum das niemandem aufgefallen ist

Die Verfolgung ist **verwaist**: [B-137](B-137-freitext-fach-unerreichbar.md) hielt unter Punkt 3 fest,
dass `CreatorProfile` und `Textbook` dieselbe Frage stellen, und vermerkte „reist mit B-144". B-144 nennt
sie in der gebauten Fassung in **keinem** Akzeptanzkriterium — beim Grillen wurde die Frage auf das
Löschverhalten verengt, und der Rest fiel zwischen die Stories.

**Kein `entgangen_bei`:** Der Zustand ist älter als B-143/B-144 und wurde von keiner Abnahme
durchgelassen — er wurde in einer Notiz verfolgt und dort vergessen.

## Offene Punkte

1. ~~**Trägt `CreatorProfile` dasselbe?** B-137 nannte beide in einem Atemzug; gemessen ist bisher nur
   `Textbook`.~~ **Beantwortet am 2026-08-11:** ja, `VaterFachlehrer.tsx:245-251`, identische Zeile.
   Die Story deckt beide Formulare ab — sie einzeln zu fahren hieße, dieselbe Regel zweimal zu bauen.
2. **Übernimmt diese Story den Sentinel aus B-143 oder reicht der Diff-Vergleich?** Empfehlung: erst den
   Vergleich (er behebt die Zerstörung), den Sentinel nur, wenn der Zustand am Kind auch *angezeigt*
   werden soll. Die zwei Hälften sind trennbar, und die erste ist die dringende.
3. **Neu: Wird die Regel geteilt oder je Formular wiederholt?** `seriesPatch.ts` löst dasselbe Problem
   seit B-143 für die Reihe, mit eigener Testdatei. **Empfehlung: ein geteilter Helfer**
   („Schalter aus dem Vergleich `loaded` ↔ `form`"), den alle drei Formulare benutzen — drei Kopien
   derselben Semantik sind genau die Konstellation, in der dieses Repo schon zweimal die veraltete Fassung
   hat gewinnen sehen. **Kosten:** eine Umstellung von `seriesPatch.ts` auf den Helfer, also Anfassen von
   Code, der gerade erst abgenommen wurde und grüne Tests hat.
4. **Neu: Soll das verwaiste Fach am Kind sichtbar werden?** Heute zeigt die Auswahl „– keine Angabe –",
   obwohl daneben ein Fachname steht — der Nutzer sieht den Widerspruch, kann ihn aber nicht deuten.
   **Empfehlung: zurückstellen.** Es ist ein eigener Wunsch (Anzeige), nicht Teil der Zerstörung, und
   B-143 hat gezeigt, dass die Sentinel-Hälfte für sich schon eine Story füllt.

## Akzeptanzkriterien (Entwurf, final erst nach dem Grillen)

1. Ein Lehrbuch mit `subjectId: null` und gesetztem `subjectName` behält den Namen, wenn ein **anderes**
   Feld geändert und gespeichert wird.
2. Dasselbe für ein Fachlehrer-Profil im selben Zustand.
3. Wählt der Nutzer ausdrücklich „– keine Angabe –", wird das Fach weiterhin geleert (Id **und** Name) —
   die Behebung darf den Weg nicht zumauern.
4. Ein Regressionstest je Formular, der vor der Behebung rot ist.

## Verlauf

- **2026-08-10** — angelegt aus dem Frontend-Review des Nachtlauf-Sprints 3. **Bewusst nicht im Sprint
  behoben:** der Fund liegt außerhalb seines Diffs, das Sprint-Ziel ist ohne ihn erreicht, und B-143 zu
  erweitern hieße, eine geschätzte Story während des Bauens wachsen zu lassen.
- **2026-08-11** — **ausformuliert.** Gegen den Code belegt statt aus dem Review-Befund abgeschrieben, und
  das hat den Zuschnitt geändert: Der Defekt sitzt an **zwei** Formularen, nicht an einem
  (`VaterFachlehrer.tsx:245-251` trägt dieselbe Zeile — offener Punkt 1 damit beantwortet). Nachgesehen und
  ausdrücklich **nicht** betroffen: der Server. `TextbooksController.cs:110-117` und
  `CreatorProfilesController.cs:148` wenden erst den Wert, dann den Schalter an und leiten den Namen gegen
  den Ergebniszustand ab (B-142) — die `clearSubject`-Semantik „Id und Name zusammen" ist richtig, falsch
  ist nur, dass der Client sie ungefragt schickt. Zwei neue offene Punkte kamen dazu (geteilter Helfer statt
  dritter Kopie; Sichtbarkeit des verwaisten Zustands), einer fiel weg. **Nicht** nachgemessen und als
  solches benannt: ob `clearSeries` denselben nicht darstellbaren Zustand kennt.
