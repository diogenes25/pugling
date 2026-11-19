---
tags: [typ/story, status/ausformuliert, bereich/training, bereich/medien, bereich/belohnung, rolle/student, rolle/supervisor]
aliases: [Hausaufgaben-Foto, Hausaufgabe machen, Foto-Nachweis]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-19 (Friedrichs Mathe-Hausaufgabe S. 80/82 per Foto nachgeprüft)"
grund: ""
ersetzt_durch: []
---

# B-687 · Hausaufgabe als Pflicht mit Foto-Nachweis: der Sohn fotografiert sein Heft, der Vater nimmt ab

**Anlass (2026-11-19):** Der Vater sah in WebUntis, dass Friedrich die Mathe-Hausaufgabe zum 18.11. (S. 80 Nr. 1+2,
S. 82 Nr. 12) „nicht erledigt“ hatte. Am nächsten Tag zeigte Friedrich ihm das Heft. Claude prüfte das Foto gegen die
aufbereitete Buchseite (`raw/Math/lambacher-schweizer-9-sh-pugling/md/s080-081.md`, `s082-083.md`): alles richtig,
12d fehlte, das Lösungswort auch. Zwei Erkenntnisse daraus:

1. **Das WebUntis-Signal taugt nicht.** Das Feld `completed` stand bei allen 16 Hausaufgaben seit dem 1.11. auf
   `false`. Es ist ein Häkchen des Schülers, kein Abgabe-Nachweis der Schule.
2. **Das Foto taugt.** Es zeigt, *dass* gearbeitet wurde, und ist gegen die Buchseite nachprüfbar, *ob* richtig.

Die Idee: Eine Schul-Hausaufgabe wird in Pugling zur **Aufgabe „Hausaufgabe machen“ mit Frist**. Das Kind erledigt sie,
indem es **ein oder mehrere Fotos** seines Hefts hochlädt. Der Vater sieht die Fotos und nimmt ab: angenommen →
Münzen, Nacharbeit nötig → zurück ans Kind mit einer Zeile Begründung. Ist bis zur Frist nichts hochgeladen, greift der
**Malus** wie bei jeder verpassten Pflicht. Das ist der „Stick“ aus der CLAUDE.md, diesmal für die Schule statt für die
App-Übungen.

**Vorbehalt:** Art `Wunsch`; der Vorbereitungslauf hat nur **ausformuliert**. Die Fragen unter „Offene Punkte“ sind
**nicht entschieden**, es gibt bewusst keinen Abschnitt `## Entscheidungen`. Berührt Malus-Regeln und Münz-Ökonomie
(Nutzer entscheidet) und hängt am Rollenschnitt B-681.

## User Story

Als **Supervisor** möchte ich, dass mein Kind jede Schul-Hausaufgabe mit einem Foto des Hefts als erledigt meldet, und
dieses Foto abnehmen oder zurückgeben. Dann weiß ich, ob die Hausaufgabe wirklich gemacht ist, und belohne bzw. bestrafe
sie wie die übrigen Pflichten.

Als **Student** möchte ich meine Hausaufgaben in der App sehen und mit einem Handyfoto abhaken, damit ich dafür Münzen
bekomme und keinen Malus kassiere.

## Ist-Stand am Code

Gemessen am 2026-11-19 auf `main` (Stand `47791819`). Pfade relativ zu `backend/Pugling.Api/`, Frontend zu `frontend/`.

- **Hausaufgabe als Fachbegriff: nicht gefunden.** Die Suche nach `homework`/`hausaufgabe` in `Pugling.Api` und
  `Pugling.Contracts` trifft nur Beispielsätze in Kommentaren („homework counts double between …“,
  `Models/PlanPositionEntities.cs:103`, `Services/Shared/ScoringService.cs:43`, `Pugling.Contracts/Common/PlanPositionBaseTypes.cs:51`).
  Keine Entität, kein Endpunkt, kein WebUntis-Code (Suche nach `WebUntis` ohne Treffer; B-654/B-655 sind `geschaetzt`, nicht gebaut).
- **Die Pflicht-/Malus-Mechanik hängt an der Position, nicht an einer freien Aufgabe:** `PlanPosition.Cadence`
  (`Models/PlanPositionEntities.cs:51`) und `PenaltyCoins` (`:91`); die Abrechnung `PositionProgressService.SettleClosedPeriodsAsync`
  (`Services/Shared/PositionProgressService.cs:754`) iteriert über Positionen, schreibt eine `PositionGoalPenalty`
  (`Models/PlanPositionEntities.cs:223`, Schlüssel `PlanPositionId` + Periode, `:226`, `:231`) und einen negativen
  `ChildPointsEntry` der Art `PointKind.GoalPenalty` (`PositionProgressService.cs:808-818`). Die Zeile `PlanPositionId` ist
  Pflicht – eine Hausaufgabe ohne Position könnte sie **nicht** wiederverwenden. Wiederverwendbar ist das *Muster*: Ledger-Eintrag,
  Idempotenz über einen eindeutigen Schlüssel, `child.ConcurrencyStamp = Guid.NewGuid()` vor dem Speichern (`:827`, Wallet-Invariante).
- **Der Abrechnungs-Auslöser ist träge:** kein Zeitgeber; `SettleClosedPeriodsAsync` läuft beim Kind-Login
  (`Controllers/AuthController.cs:85`, `Controllers/Student/MeController.cs:296`). Eine Hausaufgaben-Frist ohne Login des Kindes
  wird erst beim nächsten Login abgerechnet – dasselbe wie heute bei jeder Pflicht.
- **Belohnungsseite:** `PointKind.Goal` (`Pugling.Contracts/Common/AdminBaseTypes.cs:55`) für erreichte Positionsziele; eine
  Hausaufgabe bräuchte eine eigene Art oder `Mission`/Bonus (`:49`). Nicht gefunden: eine Art „Nachweis angenommen“.
- **Foto-Upload gibt es nur für den Creator:** `MediaAssetsController` ist `[Authorize(Roles = Roles.Creator)]` (`Controllers/Creator/MediaAssetsController.cs:25`),
  `POST creator/media/upload` (`:214`) verlangt Beschreibung (Alt-Text) und liefert ein dekodiertes, neu kodiertes Bild
  (`images.Process`, `:244`; Limit `MediaOptions.MaxUploadBytes` = 10 MB, `Services/Shared/MediaStorage.cs:19`). Ein Student-Upload
  existiert nicht (nicht gefunden).
- **Datenschutz-Befund:** Mediendateien werden **öffentlich und ohne Anmeldung** ausgeliefert
  (`Program.cs:598-607`, `UseStaticFiles` mit `RequestPath = media.PublicPath`). Ein Foto vom Heft eines Kindes darf so
  **nicht** abgelegt werden – das ist die wichtigste Einschränkung für Fragen 5 und B-684.
- **Frontend:** Ein `<input type="file" accept="image/*">` gibt es im Vater-Web (`src/vater/VaterMedia.tsx:245`,
  `src/vater/VocabMediaPanel.tsx:164`); `capture="environment"` und überhaupt ein Datei-Eingabefeld in der Sohn-Arcade: nicht gefunden.
- **Rollen:** Ein Student schreibt über seine eigenen Endpunkte (`student/…`, nur `[Authorize]`, Rollen inline getrennt);
  ein neuer Student-Upload müsste Ownership-sauber an `OwnsChildAsync` (`Auth/AuthAccess.cs:122`) hängen.
- **Stundenplan (anderes Thema):** `TimetableEntry` (`Models/TimetableEntities.cs:7`) trägt Fach und Wochentag, keine Hausaufgabe.

## Die echte Lücke

Nichts davon ist da – das Feature ist **in allen Schichten neu** (Entität, Upload, Abnahme, Malus, Kind- und Vater-UI). Schmaler
als die Skizze: die **Malus-/Münz-Mechanik** ist als Muster vorhanden, aber an die Position gebunden; sie lässt sich nicht
„einfach wiederverwenden“, sondern nur nachbauen oder verallgemeinern (Frage 1). Und der vorhandene Medienpfad taugt
**nicht** für Kinderfotos (öffentlich, Creator-only, Katalog-Asset). Die Story braucht darum einen **eigenen,
authentifizierten Ablageweg** für Fotos – dieselbe Entscheidung wie B-684.

## Bausteine (Skizze aus der Idee)

- **Woher kommt die Hausaufgabe?** Bevorzugt aus WebUntis ([B-654](B-654-webuntis-connector-backend.md), Text +
  Fach + Fälligkeit), sonst vom Vater von Hand angelegt. Ohne B-654 funktioniert die Story auch, nur mit Abtippen.
- **Erledigen (Sohn-Arcade):** Karte „Hausaufgabe: Mathe – S. 80 Nr. 1+2, S. 82 Nr. 12 · fällig morgen“ → Kamera
  öffnen (`<input type="file" accept="image/*" capture="environment">`) → 1–n Fotos → „Abgeben“.
- **Abnehmen (Vater-Web):** Liste offener Abgaben mit Fotos in groß; Knöpfe *Annehmen* / *Nacharbeit* (mit Notiz).
  Die Notiz sieht das Kind an der Karte („12d fehlt noch“).
- **Belohnung:** Annahme schreibt Münzen gut wie eine erfüllte Pflicht; Frist verpasst ohne Abgabe → Malus wie eine
  verpasste Pflicht. Nacharbeit verschiebt die Frist nicht von selbst.
- **Optional, Creator-seitig: Korrekturvorschlag.** Claude liest das Foto gegen die aufbereitete Buchseite und schlägt
  eine Bewertung vor, genau wie am 2026-11-19 im Chat. Das läuft **nicht im Backend** (kein LLM dort; das lokale
  Vision-Modell liest laut [B-459](B-459-vision-modell-fuer-buchseiten.md) nicht einmal Buchseiten sicher), sondern als
  Skill/Agent, der über die API liest und eine Bemerkung zurückschreibt. Der Vater entscheidet.

## Abgrenzung zu Nachbar-Stories

- [B-654](B-654-webuntis-connector-backend.md)/[B-655](B-655-webuntis-hausaufgaben-vater-web.md)/[B-656](B-656-ki-creator-uebung-aus-hausaufgabe.md):
  Hausaufgabe → *Übung im Katalog* (Stoff üben). Diese Story: Hausaufgabe → *Nachweis, dass die Schulaufgabe gemacht ist*.
  B-651 nannte „Hausaufgaben als Pflichtziel mit Münz-Malus“ ausdrücklich als späteren Schritt – das ist er.
- [B-684](B-684-korrigierte-klausur-einscannen.md): ebenfalls Foto von Papier, aber die *korrigierte Klausur*, und der
  Vater scannt. Hier fotografiert das **Kind**, regelmäßig. Speicherort und Datenschutz der Fotos sollten beide
  Stories gleich lösen.
- [B-582](B-582-probearbeit-auf-papier-mit-foto-korrektur.md) (verworfen): Foto-Korrektur einer Probearbeit; der dort
  formulierte Grundsatz „Korrektur beim Vater, KI nur als Vorschlag“ gilt hier genauso.
- [B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md): passt zum Rollenschnitt – der Vater belohnt und
  bestraft die Schulpflicht, unterrichtet wird woanders.

## Offene Punkte

1. **Ist „Hausaufgabe machen“ ein neuer `ExerciseType` in einer `PlanPosition` oder eine eigene Sache am Kind?**
   Empfehlung: eigene Ressource am Kind (`supervisor/children/{id}/homework`, `student/homework`). Eine Katalog-Übung
   ist wiederverwendbar und hat eine prüfbare Antwort; eine Hausaufgabe gilt einmal, für ein Kind, mit einer Frist und
   ohne Antwortschlüssel. Die Pflicht-/Malus-Mechanik soll aber **wiederverwendet**, nicht nachgebaut werden –
   **geprüft:** `PositionGoalPenalty` verlangt eine `PlanPositionId` (`Models/PlanPositionEntities.cs:226`); ohne Position
   lässt sich nur das Muster (Ledger-Eintrag, eindeutiger Schlüssel, Concurrency-Marke) übernehmen, nicht die Tabelle.
2. **Was zählt als „erledigt“ für die Frist: das Hochladen oder die Annahme?** Empfehlung: das Hochladen. Sonst hinge
   der Malus des Kindes an der Reaktionszeit des Vaters.
3. **Wie viel ist eine Hausaufgabe wert?** Empfehlung: fester Betrag je Hausaufgabe (Familien-Einstellung), nicht je
   Aufgabe frei – der Vater soll beim Abnehmen nicht rechnen müssen.
4. **Anti-Cheat: altes oder fremdes Foto?** Empfehlung: nur die Kamera-Aufnahme anbieten, Aufnahmezeit mitspeichern,
   den Rest leistet die Abnahme durch den Vater. Keine EXIF-Forensik. Hinweis: `capture="environment"` ist nur ein Hinweis an den Browser; manche Geräte öffnen trotzdem die Galerie – „nur Kamera“ ist so nicht erzwingbar.
5. **Wo liegen die Fotos, wie lange?** Empfehlung: im Medien-Speicher der App (nicht im Repo), mit Löschung nach einer
   Frist (z. B. Ende des Schulhalbjahres). **Geprüft:** der bestehende Medien-Pfad ist
   öffentlich und Creator-only (siehe Ist-Stand) und taugt nicht; nötig ist ein eigener, nur für berechtigte Supervisor
   lesbarer Ablageweg (gemeinsam mit B-684 zu entscheiden). Das „im Medien-Speicher“ aus der Empfehlung gilt nur für den
   Speicherort, nicht für die öffentliche Auslieferung.
6. **Teilweise erledigt (wie 12d)?** Empfehlung: kein Teilpunkte-System; der Vater gibt mit „Nacharbeit: 12d fehlt“
   zurück, das Kind lädt ein weiteres Foto nach.
7. **Gilt das für beide Söhne?** Theodors Schule (AVS) gibt dem Elternkonto Hausaufgaben frei, Prüfungen nicht – für
   die Hausaufgaben selbst reicht das. Beim Ausformulieren bestätigen.
8. **Höhe des Malus je verpasster Hausaufgabe?** Empfehlung: Familien-Einstellung wie der Betrag der Gutschrift (Punkt 3),
   nicht je Aufgabe frei. *Malus-Regeln – Nutzer entscheidet.*

## Akzeptanzkriterien (Entwurf)

Final erst nach den Punkten 1 bis 5. Je Kriterium die Änderung, die es rot macht:

1. Ein Kind lädt zu einer Hausaufgabe ein Foto hoch; Vater sieht es nur nach Anmeldung als berechtigter Supervisor des
   Kindes. *Rot, wenn* das Foto unter einer öffentlichen Medien-URL (`Program.cs:598-607`) erreichbar ist oder ein
   fremder Supervisor es abrufen kann.
2. Ein Student kann nur für **seine** Hausaufgaben hochladen, nie für ein anderes Kind. *Rot, wenn* die Ownership-Prüfung
   (`OwnsChildAsync`) fehlt.
3. Frist ohne Upload kostet **genau einmal** Münzen, mit Upload nichts, und das Nachladen am selben Tag erhöht den Betrag
   nicht. *Rot, wenn* die Abrechnung bei zweimaligem Aufruf doppelt bucht oder die Wallet-Concurrency-Marke nicht bewegt wird.
4. Eine Abnahme schreibt einmal Münzen gut; ein zweites „Annehmen“ nicht. *Rot, wenn* der Endpunkt nicht idempotent ist.
5. „Nacharbeit“ mit Begründung erscheint an der Karte des Kindes und verschiebt die Frist nicht. *Rot, wenn* die
   Notiz fehlt oder die Frist sich verschiebt.
6. Ein Upload über dem Größenlimit oder ein Nicht-Bild wird mit `400` abgelehnt, nicht stumm angenommen. *Rot, wenn*
   die Prüfung aus `MediaAssetsController.cs:224`/`:244` beim Nachbau fehlt.
7. Ein Kind sieht seine Hausaufgabenliste und die Karten der Söhne eines anderen Haushalts nicht. *Rot, wenn* die
   Listen-Abfrage nicht je Kind filtert.

## Verlauf

- **2026-11-19** — angelegt aus dem Chat mit dem Vater (Friedrichs Mathe-Hausaufgabe per Foto nachgeprüft); Prio P2
  als Vorschlag, User Story und offene Fragen als Material fürs Ausformulieren.
- **2026-11-19** — `idee` → `ausformuliert` (Vorbereitungslauf, Sonnet): Ist-Stand am Code belegt. Kern: In allen Schichten
  neu; der Malus hängt an `PlanPosition` und ist so nicht wiederverwendbar, der vorhandene Medienpfad ist Creator-only und
  öffentlich (Datenschutz). Sieben offene Punkte mit Empfehlung, **nichts entschieden** (Malus/Münzen, Rollenschnitt B-681).
