---
tags: [typ/story, status/gegrillt, bereich/training, bereich/medien, bereich/belohnung, rolle/student, rolle/supervisor]
aliases: [Hausaufgaben-Foto, Hausaufgabe machen, Foto-Nachweis]
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-19 (Friedrichs Mathe-Hausaufgabe S. 80/82 per Foto nachgeprüft)"
grund: ""
ersetzt_durch: []
wartet_auf: "Ablageweg der Foto-Dateien (Entscheidung mit B-684, steht aus)"
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

**Stand:** Art `Wunsch`. Alle Fragen sind unten stellvertretend entschieden, bis auf den Ablageweg der Fotos (Frage 5,
ausdrücklich zurückgestellt, gemeinsam mit B-684). Berührt Malus-Regeln und Münz-Ökonomie und hängt am Rollenschnitt B-681.

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

1. ~~**Ist „Hausaufgabe machen“ ein neuer `ExerciseType` in einer `PlanPosition` oder eine eigene Sache am Kind?**
   Empfehlung: eigene Ressource am Kind (`supervisor/children/{id}/homework`, `student/homework`). Eine Katalog-Übung
   ist wiederverwendbar und hat eine prüfbare Antwort; eine Hausaufgabe gilt einmal, für ein Kind, mit einer Frist und
   ohne Antwortschlüssel. Die Pflicht-/Malus-Mechanik soll aber **wiederverwendet**, nicht nachgebaut werden –
   **geprüft:** `PositionGoalPenalty` verlangt eine `PlanPositionId` (`Models/PlanPositionEntities.cs:226`); ohne Position
   lässt sich nur das Muster (Ledger-Eintrag, eindeutiger Schlüssel, Concurrency-Marke) übernehmen, nicht die Tabelle.~~ → Entscheidung 1.
2. ~~**Was zählt als „erledigt“ für die Frist: das Hochladen oder die Annahme?** Empfehlung: das Hochladen. Sonst hinge
   der Malus des Kindes an der Reaktionszeit des Vaters.~~ → Entscheidung 2.
3. ~~**Wie viel ist eine Hausaufgabe wert?** Empfehlung: fester Betrag je Hausaufgabe (Familien-Einstellung), nicht je
   Aufgabe frei – der Vater soll beim Abnehmen nicht rechnen müssen.~~ → Entscheidung 3.
4. ~~**Anti-Cheat: altes oder fremdes Foto?** Empfehlung: nur die Kamera-Aufnahme anbieten, Aufnahmezeit mitspeichern,
   den Rest leistet die Abnahme durch den Vater. Keine EXIF-Forensik. Hinweis: `capture="environment"` ist nur ein Hinweis an den Browser; manche Geräte öffnen trotzdem die Galerie – „nur Kamera“ ist so nicht erzwingbar.~~ → Entscheidung 4.
5. ~~**Wo liegen die Fotos, wie lange?** Empfehlung: im Medien-Speicher der App (nicht im Repo), mit Löschung nach einer
   Frist (z. B. Ende des Schulhalbjahres). **Geprüft:** der bestehende Medien-Pfad ist
   öffentlich und Creator-only (siehe Ist-Stand) und taugt nicht; nötig ist ein eigener, nur für berechtigte Supervisor
   lesbarer Ablageweg (gemeinsam mit B-684 zu entscheiden). Das „im Medien-Speicher“ aus der Empfehlung gilt nur für den
   Speicherort, nicht für die öffentliche Auslieferung.~~ → Entscheidung 5, **zurückgestellt** mit B-684.
6. ~~**Teilweise erledigt (wie 12d)?** Empfehlung: kein Teilpunkte-System; der Vater gibt mit „Nacharbeit: 12d fehlt“
   zurück, das Kind lädt ein weiteres Foto nach.~~ → Entscheidung 6.
7. ~~**Gilt das für beide Söhne?** Theodors Schule (AVS) gibt dem Elternkonto Hausaufgaben frei, Prüfungen nicht – für
   die Hausaufgaben selbst reicht das. Beim Ausformulieren bestätigen.~~ → Entscheidung 7.
8. ~~**Höhe des Malus je verpasster Hausaufgabe?** Empfehlung: Familien-Einstellung wie der Betrag der Gutschrift (Punkt 3),
   nicht je Aufgabe frei. *Malus-Regeln – Nutzer entscheidet.*~~ → Entscheidung 8.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

**Ausdrücklich zurückgestellt (Frage 5, Ablageweg der Fotos):** Der Ort der Dateien, ihre Auslieferung und die Löschmechanik
werden **gemeinsam mit [B-684](B-684-korrigierte-klausur-einscannen.md)** entschieden; B-684 ist `idee`, die Entscheidung
steht nirgends. Das ist der Grund für `wartet_auf`. Festgeschrieben ist nur die Untergrenze (Entscheidung 5). Alles andere ist
entschieden.

**Querfragen:** Die Rücknahme eines Malus ist **keine** Funktion dieser Story: Die systemische Gegenbuchung ist ein positiver
`GoalPenalty`-Eintrag ([B-627](B-627-berechtigte-meldung-bucht-nach.md) Entscheidung 3), die Gnade des Vaters die vorhandene
`Manual`-Gutschrift (Entscheidung 2 hier). Der Abrechnungs-Auslöser ist, wie bei jeder Pflicht, der Kind-Login
(`AuthController.cs:85`), ebenso bei [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md) Entscheidung 2. **Überschneidung
mit abgenommenen Entscheidungen, nicht geändert:** B-535 (der Malus einer Position beginnt mit dem ersten Spielen des Kindes)
bleibt für Positionen; Entscheidung 3 nimmt den Gedanken nur als „vor der Frist angelegt“ auf.

1. **Eine eigene Ressource am Kind, die das Muster der Pflicht übernimmt, nicht die Tabelle.** Eigene Entität (Hausaufgabe
   mit Fach, Text, Frist als Tag, Abgaben, Zustand) und eigene Routen (`supervisor/children/{id}/homework`, `student/homework`).
   Von der Abrechnung wird das **Muster** übernommen: Ledger-Eintrag, Idempotenz über einen eindeutigen Schlüssel,
   `child.ConcurrencyStamp = Guid.NewGuid()` vor dem Speichern (`PositionProgressService.cs:827`). Die Tabelle
   `PositionGoalPenalty` verlangt eine `PlanPositionId` (`PlanPositionEntities.cs:226`) und taugt nicht. Eine Hausaufgabe
   zählt **nicht** für Serie, Tageskiste, Missionen und die Sperre des Bonus-Trainings. *Begründung:* Eine Katalog-Übung ist
   wiederholbar und hat einen Antwortschlüssel; eine Hausaufgabe gilt einmal, mit Frist, ohne Schlüssel. *Kosten:* eigene
   Entität, **eine Migration**, ein zweiter, kleiner Abrechnungspfad neben `SettleClosedPeriodsAsync`. *Verworfen:* eine
   `PlanPosition` mit neuem `ExerciseType` (Serie und Zeitachse gratis, aber ein Schul-Nachweis würde die Serie an die
   Annahmezeit des Vaters binden). *Schließt aus:* „Hausaufgabe macht den Tag vollständig“; ein Kind, das nur Hausaufgaben
   macht, hat keine Serie.
2. **Erledigt für die Frist heißt: abgegeben. Malus gibt es nur ohne jede Abgabe; die Annahme bringt die Münzen.** Liegt zur
   Frist mindestens ein Foto vor, kostet nichts etwas. Reicht das Kind nach dem Malus ein Foto nach, bleibt der Malus (eine Frist
   ist eine Frist); der Vater erlässt ihn bei Bedarf mit der vorhandenen **`Manual`-Gutschrift** („Münzen schenken“, `POST
   children/{}/points`). *Begründung:* Sonst hinge der Malus an der Reaktionszeit des Vaters. *Kosten:* Ein Kind kann mit einem
   wertlosen Foto den Malus vermeiden; es bekommt dann aber keine Münzen, weil der Vater „Nacharbeit“ setzt. Die Qualität
   hängt an der **zurückgehaltenen Belohnung**, nicht am Malus. *Verworfen:* eine Nachfrist mit Malus nach „Nacharbeit“
   (Strafe für die Antwortzeit des Vaters und für das Kind, das ehrlich nachbessert). *Schließt aus:* Strafe für schlechte
   Abgabe. *Gegen die User Story gerechnet:* „ob die Hausaufgabe wirklich gemacht ist“ weiß der Vater durch seine Sicht auf das
   Foto; das System erzwingt nur die Abgabe.
3. **Feste Beträge je Kind, getrennt für Gutschrift und Malus, mit Vorgabe aus der Konfiguration; Malus nur für vor der
   Frist angelegte Hausaufgaben.** Zwei nullbare Felder am Kind (Muster `Child.BonusDailyCoinCap`, `AdminEntities.cs:96`:
   leer = Vorgabe aus der Konfiguration), nicht je Aufgabe frei. Eine Familien-Einstellung gibt es als Entität **nicht**
   (nicht gefunden); das Kind trägt die Einstellungen. Gutschrift und Malus sind getrennte Größen, wie `PointsGoalMet` und
   `PenaltyCoins` an der Position (`PlanPositionEntities.cs:84`, `:91`). Der Startwert wird beim Schätzen aus den echten
   Positions-Beträgen der Kinder abgeleitet. Der Malus wird beim Kind-Login gebucht (kein Zeitgeber) für Hausaufgaben, deren
   Frist **vor dem Login** liegt, höchstens 14 Tage zurück (`MaxSettleLookbackDays`, `PositionProgressService.cs:25`), und
   nur wenn die Hausaufgabe **angelegt wurde, bevor ihre Frist verstrich** (ein Nachtrag mit abgelaufener Frist kostet
   nichts, das gilt auch für eine spätere WebUntis-Übernahme, B-654). *Kosten:* zwei Felder am Kind, Migration. Gutschrift als
   neue Art `PointKind.HomeworkCoins` (Münzen, additiv, zählt als verdient); Malus als `PointKind.GoalPenalty` wie jeder Malus.
   *Verworfen:* ein Betrag für beides („wie die Gutschrift“) und freie Beträge je Aufgabe (der Vater soll beim Abnehmen nicht
   rechnen). *Schließt aus:* einen Malus für eine Hausaufgabe, die das Kind nie sehen konnte.
4. **Anti-Cheat ist die Abnahme des Vaters, keine Forensik.** Der Server speichert die Zeit des Hochladens
   (`UploadedAt`, Serverzeit, maßgeblich); das Eingabefeld bittet die Kamera an (`capture="environment"`), erzwingen lässt sich
   das nicht (manche Geräte öffnen die Galerie). Keine EXIF-Auswertung. *Kosten:* Ein altes Foto ist möglich; der Vater sieht
   die Upload-Zeit und vergleicht mit der Aufgabe. *Verworfen:* EXIF- und Bildforensik (Aufwand ohne Verlässlichkeit; der
   Grundsatz „Korrektur beim Vater, KI nur als Vorschlag“ stammt aus B-582). *Schließt aus:* die sichere Verhinderung einer
   Fälschung durch ein unehrliches Kind.
5. **Ablageweg: zurückgestellt (mit B-684); festgeschrieben ist die Untergrenze.** Ein Foto vom Heft eines Kindes wird nie
   öffentlich ausgeliefert: der bestehende Pfad ist öffentlich und nur für den Creator (`Program.cs:598-607`,
   `MediaAssetsController.cs:25`) und kommt **nicht** in Frage. Lesen darf nur ein berechtigter Supervisor des Kindes und das
   Kind selbst; Dekodieren und Neukodieren wie `images.Process` (`MediaAssetsController.cs:244`), Größenlimit mit `400`
   (`MediaOptions.MaxUploadBytes`, 10 MB), und es gibt eine **Löschfrist** (Länge und Mechanik mit B-684). *Kosten:* Die Story
   ist erst baubar, wenn B-684 den Ablageweg entschieden hat (`wartet_auf`). *Verworfen:* den Ablageweg hier allein festlegen
   (B-684 und diese Story sollen Fotos gleich behandeln).
6. **Teilweise erledigt: kein Teilpunkte-System.** Der Vater gibt mit „Nacharbeit: 12d fehlt“ zurück, die Notiz steht an der
   Karte des Kindes, das Kind lädt ein weiteres Foto nach. Die Frist verschiebt sich nicht (Entscheidung 2 macht das
   unnötig: es gibt eine Abgabe, also keinen Malus). *Kosten:* keine Teilbelohnung. *Verworfen:* Punkte je Aufgabe.
7. **Die Funktion gilt je Kind, unabhängig von der Quelle.** Beide Söhne, Anlegen von Hand durch den Supervisor; WebUntis
   (B-654) ist nur eine spätere Quelle derselben Ressource. Dass Theodors Schule dem Elternkonto Hausaufgaben freigibt und
   Prüfungen nicht, ist eine Angabe der Story, am Code nicht prüfbar. *Kosten:* ohne B-654 wird abgetippt.
8. **Die Höhe des Malus ist die zweite Zahl aus Entscheidung 3.** Kein eigener Betrag je Hausaufgabe, keine Staffelung.
   *Kosten:* alle Hausaufgaben eines Kindes sind gleich viel wert.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht. AK 1 bis 3 hängen am Ablageweg (Entscheidung 5, zurückgestellt) und gelten
unabhängig von dessen Form als Untergrenze.

1. Ein Kind lädt zu einer Hausaufgabe ein Foto hoch; der Vater sieht es nur nach Anmeldung als berechtigter Supervisor des
   Kindes. *Rot, wenn das Foto unter einer öffentlichen Medien-URL (`Program.cs:598-607`) erreichbar ist oder ein fremder
   Supervisor es abrufen kann.*
2. Ein Student kann nur für **seine** Hausaufgaben hochladen, nie für ein anderes Kind. *Rot, wenn die Ownership-Prüfung
   (`OwnsChildAsync`) fehlt.*
3. Ein Upload über dem Größenlimit oder ein Nicht-Bild wird mit `400` abgelehnt, nicht stumm angenommen. *Rot, wenn die
   Prüfung aus `MediaAssetsController.cs:224` und `:244` beim Nachbau fehlt.*
4. Eine Hausaufgabe **ohne Abgabe** zur Frist kostet beim nächsten Login genau einmal den Malus des Kindes
   (`PointKind.GoalPenalty`, Betrag aus Entscheidung 3); mit mindestens einer Abgabe kostet sie nichts, auch nicht, wenn das
   Kind nachlädt oder der Vater „Nacharbeit“ setzt. *Rot, wenn zweimaliges Abrechnen doppelt bucht, die Wallet-Concurrency-Marke
   nicht bewegt wird oder „Nacharbeit“ einen Malus auslöst.*
5. Eine Hausaufgabe, die nach Ablauf ihrer Frist **angelegt** wurde, und eine, deren Frist länger als 14 Tage zurückliegt,
   kosten nichts. *Rot, wenn die Abrechnung `CreatedAt` oder das Zeitfenster nicht liest.*
6. Eine Abnahme schreibt einmal Münzen gut (`PointKind.HomeworkCoins`, Betrag aus Entscheidung 3); ein zweites „Annehmen“
   nicht. *Rot, wenn der Endpunkt nicht idempotent ist oder die Münzen als „nicht verdient“ aus `WeekCoinsAsync` fallen.*
7. „Nacharbeit“ mit Begründung erscheint an der Karte des Kindes und verschiebt die Frist nicht. *Rot, wenn die Notiz fehlt
   oder die Frist sich verschiebt.*
8. Die Hausaufgabe ändert Serie, Tageskiste, Missionen und die Sperre des Bonus-Trainings nicht (Entscheidung 1). *Rot, wenn
   eine offene Hausaufgabe `GateAsync` sperrt oder `DutyStateAsync` sie als Pflicht liest.*
9. Ein Kind sieht seine Hausaufgabenliste und nicht die der Söhne eines anderen Haushalts. *Rot, wenn die Listen-Abfrage nicht
   je Kind filtert.*
10. Bestand: nach der Migration sind die Beträge am Kind leer (Vorgabe aus der Konfiguration), und an einer Kopie der echten DB
    ändert sich kein Kontostand. *Rot, wenn die Migration etwas vorbelegt.*
11. **Absichtserklärung:** Ein später nachgereichtes Foto nimmt einen schon gebuchten Malus nicht zurück; das geht nur über
    die `Manual`-Gutschrift des Vaters. Der Ausgangszustand (keine Funktion) erfüllt das schon; rot würde nur, wer eine
    automatische Rücknahme einbaut.

## Verlauf

- **2026-11-19** — angelegt aus dem Chat mit dem Vater (Friedrichs Mathe-Hausaufgabe per Foto nachgeprüft); Prio P2
  als Vorschlag, User Story und offene Fragen als Material fürs Ausformulieren.
- **2026-11-19** — `idee` → `ausformuliert` (Vorbereitungslauf, Sonnet): Ist-Stand am Code belegt. Kern: In allen Schichten
  neu; der Malus hängt an `PlanPosition` und ist so nicht wiederverwendbar, der vorhandene Medienpfad ist Creator-only und
  öffentlich (Datenschutz). Sieben offene Punkte mit Empfehlung, **nichts entschieden** (Malus/Münzen, Rollenschnitt B-681).
- **2026-11-20** — gegrillt (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers „Entscheide die offenen Fragen nach deiner Empfehlung“; unbestätigt): eigene Ressource am Kind, keine Wirkung auf Serie und Kiste; Malus nur ohne Abgabe, Gnade über die `Manual`-Gutschrift; Beträge je Kind; Anti-Cheat = Abnahme des Vaters. **Ablageweg der Fotos ausdrücklich zurückgestellt** (gemeinsam mit B-684, `wartet_auf`), nur die Untergrenze steht fest. Akzeptanzkriterien final.
