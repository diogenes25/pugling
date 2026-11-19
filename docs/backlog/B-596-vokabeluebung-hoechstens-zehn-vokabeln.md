---
tags: [typ/story, status/ausformuliert, bereich/katalog, rolle/creator, lerntechnik/vokabeln]
aliases: [Zehn Vokabeln, Obergrenze Vokabelübung, Untergrenze Vokabelübung, Mini-Übung]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Diskussion Regeln fürs Erstellen von Übungen (Sitzung 2026-11-16, Vater)
grund: ""
ersetzt_durch: []
---

# B-596 · Eine Vokabelübung hat 5 bis 10 Vokabeln

Regel des Vaters (Sitzung 2026-11-16): eine Vokabelübung trägt **mindestens 5 und höchstens 10 Wörter**. Heute gibt es
keine Grenze, weder oben noch unten. Laut Story haben die zehn Buch-Wortlisten mit Richtung `both` in der echten DB im
Schnitt 25 Wörter, die größte 44; diese Zahlen sind in diesem Lauf **nicht** nachgezählt worden (die DB wird nicht angefasst).
Mini-Übungen mit 2–4 Wörtern entstehen vermutlich an Seitengrenzen beim Fotografieren (Übung 38 „Unité 2 · Wörter
Atelier A – Anfang (S. 175)", 4 Wörter).

## User Story

Als **Creator** möchte ich, dass der Server Vokabelübungen außerhalb von 5 bis 10 Wörtern abweist, damit jede Übung
eine Sitzung lang ist und keine Mini-Übung oder Wortlisten-Wand entsteht.
Als **Schüler** möchte ich kurze, überschaubare Wortgruppen üben, damit ich nicht an einer Liste mit 44 Wörtern hängen
bleibe.

## Ist-Stand am Code

**Es gibt keine Zählgrenze, an keinem der Schreibwege:**

- Anlegen/Ändern über die Typ-Route: `VocabularyController.ValidateConfigAsync` prüft Referenzen, Inline-Felder,
  Sprachcodes und Doppelwörter, **nie die Anzahl**
  (`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:38-62`). Der Aufruf geschieht auf POST und PUT
  (`ExerciseControllerBase.cs:427`, `:520`).
- Einzelnes Item: `POST …/vocabulary/{id}/items` (`ExerciseControllers.cs:202`) prüft nur Auflösbarkeit und
  Doppelung, keine Obergrenze.
- Aus Tags: `POST …/refs-from-tags` (`ExerciseControllers.cs:129`) setzt die Items auf **alle** Wörter der Tags – ein
  Weg, der naturgemäß beliebig viele Wörter zieht.
- Fertiger Plan der schwachen Wörter: `WeakWordReviewService.MaxWords = 25`
  (`backend/Pugling.Api/Services/Supervisor/WeakWordReviewService.cs:22`, Prüfung `:38-39`) legt selbst eine
  Vokabelübung mit bis zu 25 Wörtern an. Eine Obergrenze 10 träfe sie (oder sie würde umgangen).
- Bestandsschutz besteht schon in anderer Form: Eine Übung, die ein Plan spielt, darf **nicht kürzer werden und keine
  Wörter umordnen**, nur am Ende wachsen (`ExerciseControllers.cs:111-118` und `ExerciseControllerBase.cs:115-129`,
  `ShiftBlockedProblem` `:135`; Prüfung `ExerciseItemService.WouldShiftAsync`,
  `Services/Creator/ExerciseItemService.cs:90-97`). Das hat Folgen für die Obergrenze, siehe Lücke.
- Zählung: Karten, nicht Wörter, gehen in die Tagesdosis – bei `both` sind es zwei Karten je Wort
  (`ExerciseContentProvider.CardCount`, `Services/Shared/ExerciseContentProvider.cs:109`).

**Die Anlege-Wege teilen laut Story lange Listen selbst auf – das stimmt nicht:**

- `laden.py` legt **eine Übung je Abschnitt der Wortliste** an, ohne jede Aufteilung (`.claude/skills/lehrwerk-aufbereiten/laden.py:443-456`);
  es warnt nur bei **weniger als 3** Wörtern (`:218-219`). Die Länge des Abschnitts entscheidet; das erklärt vermutlich die 25er-Schnitte
  aus der Story.
- KI-Creator: Aufgabenzahl `MinItems = 3`, `MaxItems = 30` (`backend/Pugling.Agent.Creator/Drafting/DraftRules.cs:40-43`),
  gesetzt über `request.ItemCount`; eine Aufteilung einer Unit-Wortliste in mehrere Übungen: nicht gefunden. „Part A (1/3)"
  kommt in `ExerciseStrategy.cs:83` nur als Beispiel für eine Titelkollision vor.
- Vater-Web: die Items laufen über denselben `POST …/items` (`frontend/src/lib/api.ts:518-520`); eine Zählanzeige oder
  Grenze im Editor: nicht gefunden.

**Mini-Übungen:** Eine **Untergrenze** gibt es nirgends in der API. Der Plan prüft nur „nicht leer" (`IsUnfilledAsync`,
`Controllers/Supervisor/PlanPositionsController.cs:36`, `:185`); der KI-Creator nutzt eine Wortliste erst ab 3
(`FixedPairs`, `Drafting/VocabularyStrategy.cs:34`).

## Die echte Lücke

Die Regel ist nicht „eine Prüfung einbauen", sondern drei Dinge, die gegeneinander laufen:

1. **Oben deckeln und gespielte Übungen nicht kürzen dürfen** vertragen sich nicht: Eine Bestandsübung mit 44 Wörtern in
   einem Plan ist weder verkürzbar (Verschiebe-Sperre) noch änderbar, wenn jede Speicherung die Obergrenze verlangt. Die
   einzig stimmige Regel für Bestand ist „darf nicht wachsen", nicht „muss ≤ 10 sein".
2. **Unten mindestens 5** trifft das Anlegen in Schritten: Das Anlegen mit leeren Refs, dann `/items` oder
   `/refs-from-tags` ist ein ausdrücklich gewollter Weg (`PlanPositionsController.cs:181-184`). Eine Untergrenze auf POST
   würde ihn verbieten; sie kann nur beim **Zuweisen** greifen, wo heute schon „leer" abgewiesen wird.
3. **Die vier Erzeuger** (Typ-Route, Item-Route, Tag-Weg, Weak-Word-Review) müssen dieselbe Regel kennen, sonst ist
   sie über den vierten Weg umgehbar.

Offen bleibt auch, ob die Grenze wirklich im **Server** liegt oder bei den Erzeugern: Der Server-Weg ist die einzige
Stelle, an der die Regel für alle gilt, ist aber auch die einzige, die den Vertrag bricht (neue Fehlerfälle für bestehende
Clients) und die echten Daten der Söhne nicht berühren darf.

## Offene Punkte

1. **Wörter oder Karten?** (aus [B-597](B-597-tippstufen-fragen-die-zielsprache-ab.md), Entscheidung 8): Empfehlung:
   **Wörter**. Das ist, was der Vater sieht und was eine Seite trägt; die Tagesdosis zählt Karten und regelt den Umfang je
   Tag ohnehin. 10 Wörter bei `both` sind 20 Karten, ein einzelnes Üben, kein Problem.
2. **Wo greift die Obergrenze?** Empfehlung: auf **POST und PUT** und auf `POST …/items`, als `validation_error`
   (`ExerciseControllers.cs:38`, `:202`); auf `refs-from-tags` ebenfalls (mehr als 10 Treffer liefern einen Fehler mit
   der Trefferzahl, statt still zu kürzen).
3. **Bestandsübungen über 10:** Empfehlung: **nicht abweisen, nur nicht wachsen lassen**: eine Änderung ist erlaubt, solange
   die neue Wortzahl nicht größer ist als die alte; dasselbe Muster wie die bestehende Kürzungs-Sperre.
   Automatisches Aufteilen kommt nicht in Frage, weil es den Leitner-Stand an den Karten zerreißt (Story-Vorgabe).
4. **Untergrenze 5: wo?** Empfehlung: **beim Zuweisen an einen Plan** (`PlanPositionsController.cs:185`) und beim
   KI-Creator, nicht beim Anlegen; Ausnahme muss möglich sein für die letzte Seite einer Unit (siehe 5).
5. **Rest an der Seitengrenze:** Empfehlung: das Erzeugen entscheidet: `laden.py`/KI-Creator fassen einen zu kleinen
   Rest mit der Nachbarliste derselben Unit zusammen oder halten ihn zurück. Der Server lehnt nicht ab, was ihm eine
   Unit-Wortliste mit 4 Wörtern als Ganzes anbietet, wenn es die ganze Unit ist (sonst wäre eine kleine Unit nicht
   anlegbar).
6. **Weak-Word-Review (bis 25):** Empfehlung: die Grenze von 25 auf 10 senken **oder** die Regel für „Wiederholung" ausnehmen.
   Das ist eine Produktentscheidung (eine Wiederholung ist ein Tagespflicht-Päckchen, keine Lerneinheit).
7. **Bestehende Daten bei Friedrich (Child 3, Plan 8 bei null angefangen am 2026-11-16):** Neu aufteilen ist dort
   „billig", aber **nicht** in dieser Story: der Umbau echter Pläne geschieht über ein Skript an einer DB-Kopie, nicht
   über die API-Regel.
8. **Wo steht die Regel für den Creator?** Empfehlung: im Vertrag als Konstante im `VocabularyConfig`-Kommentar
   (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:23`) und im Typ-Manifest, damit das Vater-Web die Grenze
   anzeigen kann und `laden.py` sie nicht doppelt hartkodiert.
9. **Vertragsbruch?** Ein neuer Fehlerfall ändert keine Felder; ob das als Bruch zählt, entscheidet erst `geschaetzt`.
   Hinweis: `laden.py` und der KI-Creator rufen die API auf und scheitern bei Listen über 10; beide müssen **vor** der
   Server-Regel nachziehen.
10. **Berührt die Leitbild-Stories** (B-680): Wenn der Lehrer-Agent täglich je Fach Pakete schneidet, ist die
    Wortzahl je Paket seine Sache, nicht die des Katalogs. Empfehlung: die Regel als **Katalog-Obergrenze** formulieren,
    nicht als pädagogische Größe.

## Entscheidungen

Noch keine – Stufe `ausformuliert`.

## Akzeptanzkriterien (Entwurf)

Je Kriterium: welche Änderung macht es rot.

1. `POST …/vocabulary` mit 11 Wörtern liefert `validation_error`, mit 10 `201`. Rot, wenn die Grenze fehlt oder bei `> 11`
   liegt.
2. `PUT` eines Bestands mit 44 Wörtern in einem Plan, **unverändert in der Länge**, bleibt möglich; mit 45 Wörtern nicht.
   Rot, wenn die Regel alle Speicherungen abweist (Bestandsschutz) oder das Wachstum zulässt.
3. `POST …/items` auf eine Übung mit 10 Items liefert `validation_error`; mit 9 geht es. Rot, wenn der Item-Weg die
   Grenze nicht kennt.
4. `refs-from-tags` mit 11 Treffern liefert einen Fehler, der die Anzahl nennt, und lässt die Items unverändert. Rot,
   wenn gekürzt oder gespeichert wird.
5. Das Zuweisen einer Vokabelübung mit 4 Wörtern an einen Plan liefert `validation_error`, mit 5 geht es. Rot, wenn die
   Untergrenze beim Anlegen statt beim Zuweisen prüft (dann scheitert der Weg „anlegen, dann füllen").
6. Zwei Wörter sind bei `both` vier Karten, aber **zwei** Wörter für die Grenze. Rot, wenn `CardCount` statt der
   Wortzahl zählt.
7. `laden.py` meldet einen Abschnitt über 10 Wörtern **vor** dem ersten Aufruf mit Abschnittsname und Zahl. Rot, wenn der
   Fehler erst als 400 des Servers kommt.
8. Der KI-Creator legt aus einer Unit-Wortliste mit 25 Wörtern keine Übung mit 25 Wörtern an. Rot, wenn `Normalize`
   die Liste unverändert durchreicht.

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Regeln fürs Erstellen von Übungen.
- **2026-11-16** — offene Frage „Wörter oder Karten?“ aus dem Grillen von B-597 übernommen.
- **2026-11-16** — um die Untergrenze 5 erweitert (Vater im Chat), Titel auf „5 bis 10“ gezogen.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): keine Zählgrenze an vier Schreibwegen; die Behauptung „`laden.py` und
  KI-Creator teilen selbst auf“ ist nicht belegbar; die Obergrenze kollidiert mit der Verschiebe-Sperre für gespielte
  Übungen; zehn offene Punkte mit Empfehlung.
