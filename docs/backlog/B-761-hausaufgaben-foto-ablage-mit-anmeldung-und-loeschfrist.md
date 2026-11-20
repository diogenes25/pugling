---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/belohnung, rolle/student, rolle/supervisor, bereich/medien]
aliases: [Foto-Ablage Hausaufgabe]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: "B-687"
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: "Ablageweg der Foto-Dateien (Entscheidung mit B-684, steht aus)"
---

# B-761 · Hausaufgaben-Foto: authentifizierte Ablage, Upload des Kindes und Löschfrist (Backend)

Der zweite Backend-Teil von [B-687](B-687-hausaufgabe-mit-foto-nachweis.md): das Foto vom Heft eines Kindes, nie öffentlich, nur für das Kind und
berechtigte Supervisor lesbar, mit Löschfrist. **Hängt am Ablageweg**, den B-684 (`idee`) und diese Story gleich lösen sollen.

## User Story

Als **Supervisor** möchte ich, dass das Foto der Hausaufgabe meines Kindes nur ich und mein Kind sehen und dass es nach einer Frist verschwindet,
damit ein Kinderfoto nicht öffentlich im Netz liegt.

## Ist-Stand am Code

Pfade relativ zu `backend/Pugling.Api/`; gemessen am 2026-11-20.

- Mediendateien werden **öffentlich und ohne Anmeldung** ausgeliefert (`Program.cs:598-607`, `UseStaticFiles` mit `RequestPath = media.PublicPath`).
- Upload gibt es nur für den Creator: `MediaAssetsController` mit `[Authorize(Roles = Roles.Creator)]` (`Controllers/Creator/MediaAssetsController.cs:25`),
  `POST creator/media/upload` (`:214`), Größengrenze `MediaOptions.MaxUploadBytes` = 10 MB (`Services/Shared/MediaStorage.cs:19`), dekodieren und neu
  kodieren über `images.Process` (`:244`).
- Ein Student-Upload und eine geschützte Auslieferung: **nicht gefunden**.

## Die echte Lücke

Ein eigener Ablageweg: geschützte Auslieferung, Student-Upload, Löschfrist und die Anbindung an die Abgabe aus B-760. Die Entscheidung dazu fehlt.

## Entscheidungen

*Entscheidungen stammen aus [B-687](B-687-hausaufgabe-mit-foto-nachweis.md): stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt. Die Nummern der Entscheidungen verweisen dorthin.*

1. **Untergrenze (B-687 Entscheidung 5), festgeschrieben:** nie öffentlich; lesen darf nur ein berechtigter Supervisor des Kindes und das Kind selbst;
   dekodieren und neu kodieren wie `images.Process`; Größenlimit mit `400`; eine **Löschfrist**.
2. **Anti-Cheat ist die Abnahme des Vaters** (B-687 Entscheidung 4): der Server speichert die Zeit des Hochladens (`UploadedAt`, Serverzeit), keine
   EXIF-Auswertung.
3. **Ausdrücklich zurückgestellt: Ort der Dateien, Form der Auslieferung und Mechanik und Länge der Löschfrist** – gemeinsam mit
   [B-684](B-684-korrigierte-klausur-einscannen.md) zu entscheiden; B-684 ist `idee`, die Entscheidung steht nirgends. Darum die Marke `wartet_auf`.

## Akzeptanzkriterien

Nummerierung folgt B-687; die Entscheidung über den Ablageweg ist die Voraussetzung.

1. (B-687 AK 1) Ein Kind lädt ein Foto hoch; der Vater sieht es nur nach Anmeldung als berechtigter Supervisor des Kindes. *Rot, wenn das Foto unter einer
   öffentlichen Medien-URL (`Program.cs:598-607`) erreichbar ist oder ein fremder Supervisor es abrufen kann.*
2. (B-687 AK 2, Datei-Hälfte) Ein Student lädt nur für **seine** Hausaufgabe hoch. *Rot, wenn die Eigentumsprüfung am Upload fehlt.*
3. (B-687 AK 3) Ein Upload über dem Limit oder ein Nicht-Bild wird mit `400` abgelehnt. *Rot, wenn die Prüfung aus `MediaAssetsController.cs:224` und `:244` beim
   Nachbau fehlt.*
4. (**neu, aus Entscheidung 1**) Nach Ablauf der Löschfrist ist die Datei weg und die Zeile benennt das („Foto gelöscht“); die Abgabe zählt für den Malus weiter.
   *Rot, wenn die Löschung die Abgabezeile entfernt (dann kostet die Hausaufgabe nachträglich einen Malus) oder die Datei liegen bleibt.*
5. (**neu, aus B-687 Entscheidung 2**) Eine Abgabe **zählt erst mit mindestens einem Foto**, sobald dieser Teil gebaut ist. *Rot, wenn ein leerer Abgabe-Aufruf
   den Malus abwendet.*

## Schätzung

**Größe: M** – wie der Batch-Pfad im `MediaSelector` (B-03): ein neuer, abgesicherter Pfad neben dem vorhandenen, Bildverarbeitung wird wiederverwendet.
Wächst auf L, falls die Löschfrist einen Hintergrunddienst braucht (heute gibt es keinen Zeitgeber).

Nachgesehen am 2026-11-20:

- **`wo: backend`.** Kein Frontend in diesem Teil.
- **`migration: ja`.** Mindestens eine Dateizeile je Abgabe (`HomeworkSubmission` aus B-760 bekommt die Datei-Referenz oder eine Kindtabelle) und ein Löschzeitpunkt.
- **`vertragsbruch: nein`.** Neue Route und neue Antwort.
- **Bau-Sperre:** Der Ablageweg ist zurückgestellt, B-684 ist `idee`. Erst bauen, wenn der Nutzer ihn entschieden hat. Das ist der Grund für `wartet_auf`.
- **Wallet:** berührt das Konto nicht.

### Risiken

1. **Löschfrist ohne Zeitgeber.** Es gibt keinen Hintergrunddienst (`SettleClosedPeriodsAsync` läuft lazy beim Login); eine Frist muss lazy oder mit einem Dienst laufen.
2. **Auslieferung hinter der Anmeldung.** Ein `<img src>` im Browser sendet keinen `Authorization`-Header; die Auslieferung muss eine Form haben, die der Browser
   laden kann (kurzlebiger Link oder Abruf als Blob). Das ist der eigentliche Entscheidungsgrund.
3. **Dateisystem.** Wo die Dateien liegen, muss die Sicherung des Heimnetzes (`scripts/heimnetz.ps1`) kennen, sonst gehen sie beim Umzug verloren oder
   landen im Repo.
4. **Datenschutz:** ein Kinderfoto; Frist und Zugriff gehören in die Doku.

### Angriffsplan

1. Entscheidung über den Ablageweg mit B-684 einholen (kein Code).
2. Speicher und geschützte Auslieferung, Upload mit den Prüfungen aus `MediaAssetsController`, Löschfrist.
3. Anbindung an `HomeworkSubmission` und an die Abrechnung (AK 5).

### Testweg

- Neue Klasse `HausaufgabenFotoTests` (Vorbild `MediaUploadTests`): AK 1 bis 5; zusätzlich ein Test, der die öffentliche Medien-URL abfragt und `404` erwartet.
- `OwnershipMatrixTests` für die neuen Routen.
- Vor der Abnahme: `pugling-reviewer`.

## Verlauf

- 2026-11-20 · aus B-687 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): M (L mit Dienst), backend, Migration ja, kein Vertragsbruch. **Bau-Sperre:** Ablageweg mit B-684 (`idee`) offen, deshalb `wartet_auf`.
