---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Inhalt wird lautlos zurueckgedreht, config vom Oeffnen]
status: abgenommen
prio: P1
art: Defekt
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-03 zu B-206 (Nachtlauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-09-04: Vokabel-Pfad gegen docs/api-examples geprueft"
---

# B-210 · „Beschreibung speichern" dreht den gerade gespeicherten Inhalt lautlos zurück

**P1, weil es Arbeit des Creators vernichtet und er es nicht merkt** — kein Fehler, keine Meldung, und
der Bildschirm zeigt weiter die Fassung, die es nicht mehr gibt.

## User Story

Als **Creator** möchte ich, dass ein gespeicherter Inhalt gespeichert bleibt, auch wenn ich danach im
selben Dialog noch den Titel ändere.

## Ist-Stand am Code

`ExerciseEditModal.tsx:203` schickt beim Speichern der Metadaten `config: detail.config` mit — den Stand
**vom Öffnen des Dialogs**. `detail` wird nie neu geladen: `onSaved` ist `existing.reload`
(`VaterExercises.tsx:187`) und frischt nur die **Liste** auf.

Der Weg: Übung öffnen → Inhalt ändern → „Inhalt speichern" (der Server hat jetzt den neuen Inhalt) →
Titel ändern → „Beschreibung speichern" (`:205`). Der PUT ist ein **Vollersatz** und trägt die alte
`config` — der gerade gespeicherte Inhalt ist weg. Der `ContentEditor` zeigt weiter die geänderten
Zeilen, es gibt also nicht einmal einen sichtbaren Widerspruch.

**Älter als B-206, aber von dessen Review-Commit ausdrücklich für behoben erklärt.** Die Review-Runde zu
B-206 hat die *Gegenrichtung* geschlossen (der `ContentEditor` las `initialForm(detail)`) und im Kommentar
`:53` „Ein State, zwei Leser" notiert. Für die **Config** sind es weiter zwei Zustände. `e2e/einkleidung.spec.ts:118→126`
fährt genau die Reihenfolge, die den Fall **nicht** auslöst.

**Nebenbefund aus demselben Umbau:** `ContentEditor.save` (`:321-327`) prüft den Titel **nicht**,
`MetaEditor.submit` (`:197`) schon. Seit dem Hochziehen des `MetaForm` kann „Inhalt speichern" also einen
leergeräumten Titel abschicken und fängt sich einen Server-`400`.

### Reproduziert, bevor gebaut wurde

`e2e/uebung-teilspeichern.spec.ts` Fall 1, gegen den unveränderten Stand gefahren: Nach dem Neuöffnen
stand im Aufgabenfeld `He ___ (go) to school.` — die Fassung von **vor** dem „Inhalt speichern". Die
Notiz beschrieb den Defekt also richtig.

## Offene Punkte

1. ~~Nach jedem Teil-Speichern `detail` neu laden, oder die `config` aus dem lebenden Editor-Zustand
   nehmen?~~ → Entscheidung 1.
2. ~~Den Nebenbefund (Titelprüfung) hier mitnehmen oder trennen?~~ → Entscheidung 2.

## Entscheidungen

1. **Nachladen, nicht merken.** Der Dialog holt seinen `detail` nach jedem erfolgreichen Teil-Speichern
   neu. Die Alternative — sich merken, was zuletzt geschickt wurde — scheitert an einer Tatsache und
   nicht an Geschmack: Der Server **normalisiert** die Config beim Speichern und **formt** sie für die
   Antwort (`ExerciseControllerBase.NormalizeConfig`/`ConfigForResponse`; Birkenbihl vergibt dort
   `sentenceId`/`wordId`, Vokabeln bekommen `_self`-Links). „Was ich geschickt habe" wäre damit eine
   *dritte* Wahrheit, die von der des Servers abweichen darf — dieselbe Fehlerklasse eine Ebene tiefer.
   **Kosten:** ein zusätzlicher GET je Teil-Speichern, und ein Fehlerzweig, den es vorher nicht gab.
2. **Den Nebenbefund mitgenommen.** Dieselbe Datei, dieselbe Ursache (das hochgezogene Formular), und
   getrennt bliebe er liegen. Die Prüfung liegt jetzt als `metaProblem(form)` an **einer** Stelle, die
   beide Knöpfe aufrufen. **Kosten:** keine — es ersetzt eine doppelte Bedingung durch eine geteilte.
3. **Beim Nachladen wird `detail` nicht auf `null` gesetzt.** Das hängte die Editoren aus, und der
   Zeilen-Editor verlöre dabei seinen Zustand: Nach einem Metadaten-Speichern wären ungesicherte
   Inhaltsänderungen weg — ein neuer Datenverlust als Preis für die Behebung eines alten. Der Austausch
   findet an Ort und Stelle statt; das Formular bleibt ebenfalls unangetastet.
4. **Der Riegel liegt im Dialog, nicht im Editor** (aus der Review-Runde, siehe unten). Solange
   nachgeladen wird, nachgeladen *gescheitert* ist oder **irgendein** PUT läuft, sind beide
   Speicher-Knöpfe gesperrt. **Kosten:** der Dialog hält einen Zustand mehr (`schreibt`) und ist der
   einzige Ort, der `api.updateExercise` aufruft.

## Was die Review-Runde gefunden hat

Der `frontend-reviewer` meldete **kein Blocker**, aber sechs Punkte — der erste war ein Loch derselben
Klasse, das der Fix offen gelassen hatte:

1. **Das eigentliche Fenster war nicht der Microtask, sondern der laufende PUT.** Die beiden Editoren
   halten je eine eigene `useAction`-Instanz, und deren Sperre gilt je Instanz. Während „Inhalt
   speichern" unterwegs war, blieb „Beschreibung speichern" **klickbar** — und sein Vollersatz ginge mit
   der `config` von vor dem laufenden Speichern raus. Ohne ETag am Server gewinnt der letzte Schreiber:
   derselbe lautlose Verlust, nur durch zwei ungeduldige Klicks statt durch zwei Schritte. Gelöst über
   einen gemeinsamen `schreibt`-Zustand im Dialog und **einen** Ort, der den PUT abschickt.
2. **`nachladen()` hatte keine Rückmeldung und keine Sperre.** „Erneut laden" war während des eigenen
   Laufs klickbar; zwei überlappende Läufe konnten den Kasten stehen lassen, während die Knöpfe wieder
   aufgingen. Jetzt `disabled` + „Lade…".
3. **Der Fokus verließ im Fehlerfall den Dialog.** Er liegt nach dem Klick auf dem Speichern-Knopf, der
   gerade `disabled` wurde, fällt damit auf `<body>` — und dort greift die Fokus-Falle des Dialogs nicht
   mehr, Tab läuft in die Seite dahinter. Jetzt zieht ihn ein Effekt auf „Erneut laden"; ein Zähler sorgt
   dafür, dass auch ein *wiederholtes* Scheitern mit demselben Text ein Signal gibt (sonst ändert React
   keinen Knoten, und `role="alert"` sagt nichts erneut an).
4. Die Servermeldung lief im Fließtext mit dem nächsten Satz zusammen (`errorMessage` liefert
   „Failed to fetch" ohne Satzzeichen) — jetzt ein eigenes Element.
5. Kopfzeile und `aria-label` des Dialogs lasen `exercise.title` aus der Listenzeile und blieben nach dem
   Umbenennen auf dem alten Namen. Bestand, aber seit es `nachladen()` gibt, liegt die Abhilfe da.
6. `getByLabel` ohne `{ exact: true }` in der neuen Spec (Nit) — nachgezogen.

## Rote Proben (erwartet / gemessen)

| Probe | erwartet | gemessen |
| --- | --- | --- |
| Ausgangsstand ohne Nachladen (Fall 1) | 1 | **1** |
| Ausgangsstand ohne Titelprüfung (Fall 2) | 1 | **1** |
| Knopfsperre bei `veraltet` entfernt | 1 | **1** |
| Fehlerkasten ausgeblendet | 1 | **1** |
| Gemeinsamer Schreib-Riegel entfernt | 1 | **1** |
| Fokusführung **und** geladener Titel entfernt | 2 | **2** |

## Verlauf

- **2026-09-03** — aus der **Nachschau** zu B-206 (Nachtlauf). `entgangen_bei` bleibt **leer**: der Defekt
  ist älter als B-206s Diff, und die Nachschau hat das ausdrücklich geprüft. Was B-206 zuzurechnen wäre,
  ist die *Behauptung* der Review-Runde, das Problem sei damit erledigt.
- **2026-09-04** — **ausformuliert → abgenommen** in einer Sitzung. Gegrillt wurde nicht im Dialog: `art`
  ist `Defekt`, beide offenen Punkte trugen eine Empfehlung, und die erste ließ sich am Server-Code
  *entscheiden* statt abstimmen (siehe Entscheidung 1). Belege: `tsc -b` sauber, `vitest run`
  **357/357** grün, `playwright test` **56/56** grün. `frontend-reviewer` gelaufen, **kein Blocker**;
  alle sechs Funde eingearbeitet und je mit einer Zusicherung belegt. **Rollengang:** die vier Fälle in
  `e2e/uebung-teilspeichern.spec.ts` fahren die Creator-Rolle im echten Browser gegen einen echten
  Server — das ist der Rollengang, und er wiederholt sich, statt einmal stattzufinden. Kein
  Browser-Handgang möglich (keine Chrome-Anbindung in dieser Sitzung), damit greift der dokumentierte
  Ersatz. Die Regel ist als Satz in `frontend/CLAUDE.md` nachgezogen, weil sie bei jedem Dialog mit zwei
  Speicher-Knöpfen wieder entscheidet. Commit `3828bcf`.
- **2026-09-04** — **nachgeschaut** (Retro des Nachtlaufs, Sprint 1). Prüfpunkt: die eine Annahme, die beim
  Bauen nur *hergeleitet* war — dass der `ItemEditor`-Pfad kein Nachladen braucht, weil die Config einer
  Vokabelübung keine Items trägt. Gegen den **erfassten Vertrag** geprüft statt gegen den Controller:
  `docs/api-examples/catalog.md:172-178` zeigt die Antwort einer Vokabelübung mit `"refs": null,
  "items": []`. Ein reiner Einstellungs-PUT schickt damit `hasPayloadItems = false`, und `AfterSaveAsync`
  lässt die Item-Tabelle unangetastet. Die Annahme trägt. **Kein Defekt gefunden**, `entgangen_bei` bleibt
  leer.
