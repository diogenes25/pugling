---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/katalog, rolle/creator]
aliases: [Fach umbenennen, denormalisierter SubjectName veraltet, Selbstheilung beim PATCH]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer zu B-142 (Nachtlauf 2026-08-10, Fund 7)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-145 · Ein umbenanntes Fach lässt seinen Namen in drei Tabellen stehen

Dieselbe Widersprüchlichkeit wie [B-142](B-142-fachname-driftet-gegen-fach-id.md), nur von der anderen
Seite: dort wanderte die **Id** ohne den Namen, hier ändert sich der **Name** und die Kopien bleiben.

## User Story

Als **Creator** möchte ich ein Fach umbenennen („Franz." → „Französisch") und danach an jeder Lehrwerk-Reihe,
jedem Fachlehrer-Profil und jedem Lehrbuch eines Kindes, das auf dieses Fach zeigt, den neuen Namen sehen –
nicht erst, wenn zufällig jemand die Zeile ein zweites Mal speichert.

## Ist-Stand am Code

- `Controllers/Creator/SubjectsController.cs:78-100` (`Update`) setzt nur `subject.Name` und speichert.
- Drei Spalten tragen eine Kopie des Namens neben der `SubjectId`: `TextbookSeries.SubjectName`
  (`Models/CurriculumEntities.cs:40`), `CreatorProfile.SubjectName` (`:105`), `Textbook.SubjectName`
  (`Models/AdminEntities.cs:162`).
- Seit B-142 leiten alle drei Schreiber den Namen bei **jedem** Schreibzugriff aus der Id ab
  (`SubjectNaming.ResolveNameAsync`, Aufrufe `TextbookSeriesController.cs:173,241`,
  `CreatorProfilesController.cs:101,152`, `TextbooksController.cs:71,117`) – eine betroffene Zeile heilt sich
  also beim nächsten beliebigen `PATCH`. Bis dahin liest jeder die alte Kopie.
- `Data/Seed.cs:207, :616, :1023, :1036-1037` setzt das Paar von Hand; alle Werte stimmen heute (gelesen).
- Uneinheitlicher Fehlercode für dieselbe Regel: `TextbookSeriesController.cs:115` meldet eine unbekannte
  `SubjectId` als `invalid_reference`, `TextbooksController.cs:62,95` als `validation_error`.

## Die echte Lücke

Die Invariante „Name folgt der Id" hält nur, solange sich die Id bewegt. Ändert sich das Ziel der Id, sagt die
Zeile per Id „Französisch" und per Name „Franz." – bis zu einem Schreibzugriff, der mit dem Umbenennen nichts
zu tun hat.

## Offene Punkte

1. ~~Genügt die Selbstheilung, oder zieht das Umbenennen die Kopien aktiv nach?~~ → Entscheidung 1.
2. ~~Ein Wächter für die Hand-Paare in `Seed.cs`?~~ → Entscheidung 2.
3. ~~Den uneinheitlichen Fehlercode hier mitnehmen?~~ → Entscheidung 3 (zurückgestellt, eigene Story).

## Entscheidungen

Autonom gegrillt im vierten Nachtlauf (`art: Defekt`, Freigabe 1); der Code verengt die Antwort.

1. **Aktiv nachziehen**, im selben Request und in **einer** Transaktion mit dem Umbenennen
   (`SubjectNaming.PropagateRenameAsync`, drei `ExecuteUpdateAsync`). Abgeglichen wird **über die Id**, nie über
   den alten Namen – eine Freitext-Zeile mit zufällig gleichem Namen ist ein unkatalogisiertes Werk, keine
   Kopie. Auch Zeilen fremder Eigentümer (das Lehrbuch eines Kindes einer anderen Familie) werden umgeschrieben:
   der Name ist ein zwischengespeicherter Anzeigewert des Katalogs, kein Inhalt der Zeile. *Kosten:* drei
   zusätzliche `UPDATE`s je Umbenennung (selten), und ein Umbenennen verändert Zeilen, die der Creator nicht
   besitzt – nur in dieser einen Spalte.
2. **Kein Wächter für `Seed.cs`** – wie B-142 Entscheidung 3: der Seed ist konsistent und läuft nur auf leeren
   DBs; ein Wächter für eine Datei, die sich selten ändert, kostet mehr als er fängt. *Kosten:* ein künftiger
   Seed-Eintrag mit falschem Paar fällt erst beim Lesen auf.
3. **Fehlercode zurückgestellt** → [B-258](B-258-unbekannte-fach-id-zwei-fehlercodes.md). Er ist eine
   Vertragsfrage (welcher Code gilt, wer zieht mit), keine Folge des Umbenennens. *Kosten:* bleibt vorerst
   uneinheitlich.

## Akzeptanzkriterien

1. Nach `PATCH /api/v1/creator/subjects/{id}` mit neuem Namen liefern Reihe, Fachlehrer-Profil und Lehrbuch,
   die auf dieses Fach zeigen, sofort den neuen `subjectName`.
2. Eine Zeile mit einer **anderen** `SubjectId` und eine Freitext-Zeile **ohne** Id mit dem alten Namen bleiben
   unverändert.
3. Umbenennen und Nachziehen sind eine Transaktion.
4. Ein Integrationstest hält 1 und 2 fest; jede der drei Tabellen hat ihre eigene Zusicherung.

## Schätzung

- **groesse:** S · **wo:** backend · **migration:** nein · **vertragsbruch:** nein.
- **Risiko:** `ExecuteUpdateAsync` umgeht den Change-Tracker – im selben Request liest danach niemand die
  Kopien über getrackte Entitäten (die Antwort projiziert nur das Fach).
- **Angriffsplan:** Test rot → Helfer in `SubjectNaming` → Aufruf in `SubjectsController.Update` nur bei
  tatsächlich geändertem Namen.
- **Testweg:** `FachnameFolgtDerFachIdTests.Umbenanntes_Fach_Zieht_Den_Namen_In_Alle_Drei_Kopien_Nach`, Rollengang
  Katalogseite (umbenennen, Reihe ansehen).

## Verlauf

- **2026-08-10** — angelegt aus dem `pugling-reviewer`-Befund zu B-142 (Fund 7), Nachtlauf Sprint 2.
  **Bewusst nicht in B-142 mitgenommen:** dessen Ziel — die Zeile widerspricht sich nicht mehr selbst,
  wenn die Id wandert — ist ohne diese Story erfüllt, und der Fehler liegt außerhalb des Diffs (er ist
  älter). `unverifiziert: true`, weil die Belege aus dem Review stammen und nicht aus eigener Recherche.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (`SubjectsController.cs:86-94` setzt nur `Name`), Selbstheilung beim nächsten PATCH bestätigt (`TextbookSeriesController.cs:240-241`). Uneinheitliche Fehlercodes weiter da (`invalid_reference` vs. `validation_error`). Der Code verengt die Antwort (`SubjectNaming`, drei `UPDATE`s) – autonom grillbar, S.
- **2026-10-23** — vierter Nachtlauf, Sprint 8 (NP-4, [Protokoll](../pm-sitzung-2026-10-23-nacht.md)): Nachprüfungs-Zeile gelesen und gegen den Code bestätigt (Zeilen inzwischen `:78-100`). Ausformuliert (Belege selbst gelesen, `unverifiziert` entfällt), autonom gegrillt (`Defekt`), geschätzt, gebaut. Rote Probe (Auflage 5): neuer Fall gegen den Ausgangsstand – erwartet rot an der ersten Kopie, gemessen **rot an `FachnameFolgtDerFachIdTests.cs:120`** (`Expected: "Französisch (Gymnasium) 8"`, `Actual: "Englisch 1"`). Mutanten: ohne Reihen → `:120`, ohne Profile → `:121`, ohne Lehrbücher → `:122`, ohne Id-Filter → `:123` (Unbeteiligter), Freitext mitgezogen → `:124` (Zwilling), Bedingung `renamed` umgedreht → `:120`; jede an der erwarteten Zeile. Backend **894/894**.
- **2026-10-23** — `pugling-reviewer` (Backend, zuerst) und danach `frontend-reviewer` (nacheinander, Freigabe 3): kein Blocker. Gezählt (Behebung ändert Code oder Tests): **3** – (1) vier Inline-`<para>` in Contracts (`CreatorProfileDtos.cs:17`, `TextbookSeriesDtos.cs:20`, `ProfileDtos.cs:26`, `ExerciseGrantDtos.cs:18` im `<param>`) verloren weiter Absatz und Backticks, ohne dass ein Tor es sah → auf leere `///`-Zeile umgestellt; (2) Betonung über einen Absatz würde zu `**a` + Leerzeile + `b**` → Übersetzung nur innerhalb eines Absatzes, sonst bleibt das Tag roh und das Tor meldet es, neuer Fall `Betonung_Ueber_Einen_Absatz_Bleibt_Roh` (Mutante: rot an `OpenApiProseNormalizerTests.cs:55`); (3) die Aussage „Zeilen fremder Eigentümer werden mit umgeschrieben" war ungetestet → Reihe eines zweiten Creators im B-145-Fall (Mutante „nur eigene Reihen": rot an `FachnameFolgtDerFachIdTests.cs:130`). Ohne Zähler: Kommentar zum zweiten Mehrdeutigkeitsfall im Normalisierer; Messung „Transformer raus" am Endstand → die Einrückungs-Zusicherung bleibt rot (**7** Texte an `ContractDocumentTests.cs:213`), trägt also weiter; die vorbestehenden Generator-Eigenheiten → **B-259**; Nit `frontend/CLAUDE.md:112` (historischer Satz) belassen. Nachher: Absatzumbrüche **152**, Backend **895/895**, Vitest **406/406**, `tsc -b` sauber. **Rollengang im Browser** (Freigabe 6, Wegwerf-DB `pugling_walk4.db`, Server nach dem letzten Edit gestartet): Swagger UI zeigt die Summary von `POST /api/v1/auth/login` ohne `<b>`, keine Summary beginnt mit Leerraum, `DELETE …/subjects/{id}` mit Absätzen. Vater-Web `/vater/katalog`: neues Fach „Franz Walk" angelegt → die Zeile bietet „Fach umbenennen"/„Löschen" (`isOwn` auf dem Draht), Seed-Fächer nicht; Reihe und Lehrbuch des Kindes per API an das Fach gehängt, im UI umbenannt in „Französisch Walk" → `/vater/lehrwerke` zeigt die Reihe sofort mit dem neuen Fachnamen, das Lehrbuch des Kindes trägt ihn ebenfalls. **Einschränkung, benannt:** nach dem Wiederverbinden der Extension kamen Klicks und Tastatur nicht mehr an; Eingaben liefen darum über die echten Handler der Seite (nativer Value-Setter + `input`/`change`, `button.click()`), nicht über `computer.type`. Sohn: Regressionszeuge (Startseite geladen, kein Student-Pfad im Diff außer Kommentaren, Suite grün). Abgenommen; Commit: siehe Nachtrag.
