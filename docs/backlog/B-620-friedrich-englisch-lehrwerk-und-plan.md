---
tags: [typ/story, status/abgenommen, bereich/lehrplan, bereich/inhalt, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Friedrich Englisch, zweites Fach für Friedrich, Englisch-Plan neben Plan 8]
status: abgenommen
prio: P1
art: Wunsch
groesse: M
wo: doku
migration: nein
vertragsbruch: nein
quelle: B-615
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-620 · Friedrich bekommt Englisch als zweites Fach: Lehrwerk laden und Plan neben Plan 8

Teil (a) aus [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md) (dort gegrillt, beim Schätzen geteilt). Friedrich lernt in Pugling nur
Französisch. Sein Englischbuch wird aus Fotos aufbereitet und geladen, und ein aktiver Englisch-Plan läuft neben
Plan 8, sodass seine Tagesmission zwei Fächer zeigt. Das ist Inhaltsarbeit, kein Code: Die Mechanik für mehrere
Fächer steht seit B-539 und B-606.

## User Story

Als *Vater* möchte ich, dass Friedrich neben Französisch auch Englisch in Pugling übt, damit seine Tagesmission
aus mehr als einem Fach besteht. Als *Sohn* möchte ich eine Tagesmission aus zwei Fächern sehen, damit ich nicht
immer nur dasselbe Fach übe.

## Ist-Stand am Code

Gemessen am 2026-11-17 auf `main` (Stand `74d2f9f5`); die Angaben zur echten DB stammen aus der Messung für
B-615 vom selben Tag (read-only).

- **Mehrere aktive Pläne je Kind gehen:** Aktivieren schaltet nur Pläne desselben Fachs ab
  (`backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:159` `DeactivateSameSubjectPlansAsync`),
  und die Tagessicht vereinigt alle aktiven Pläne
  (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:295` `ComputeChildDayAsync`).
- **Friedrich (Child 3)** hat einen aktiven Plan (Plan 8, Französisch) und ein Lehrwerk am Kind (Découvertes
  Série bleue 2). Für Englisch hat er weder Lehrwerk noch Übungen noch Plan.
- **Aufbereitete Bücher unter `raw/`:** `raw/english/access-pugling` (55 Seiten-Markdown, Access 3, Theodors
  Buch), `raw/france/decouvertes-serie-bleue-2-pugling` (58), `raw/latin/viva-1-pugling` (49),
  `raw/Math/lambacher-schweizer-9-sh-pugling` (19). **Ein Englischbuch für Friedrich ist nicht dabei.**
- Der Weg vom Foto in die App steht als Skill `lehrwerk-aufbereiten` (Seiten-Markdown, `md_pruefen.py`,
  Stoffnotizen, Wortlisten, Start-Übungen, `laden.py`, Kind umstellen); den Plan erzeugt `ki-creator plan`
  bzw. „Plan aus Unit“ ([B-580](B-580-plan-aus-unit.md), `geschaetzt`, noch nicht gebaut).

## Die echte Lücke

Es fehlen die Fotos von Friedrichs Englischbuch. Alles danach ist ein bekannter Weg, der für zwei Bücher schon
gegangen wurde.

Beobachtung beim Schätzen, keine Entscheidung: Für Theodors Latein liegt das Buch (Viva 1) schon als
Seiten-Markdown vor. Latein für Theodor wäre also ohne neue Fotos machbar, ist nach B-615 Entscheidung 2 aber
erst nach Friedrichs Englisch dran.

## Offene Punkte

Keine. Entschieden in [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md).

## Entscheidungen

Übernommen aus [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md) (gegrillt am 2026-11-17 mit dem Vater), wörtlich.

1. (B-615 Entscheidung 1) **Dreiteilung.** (a) Inhalt und Plan für ein weiteres Fach, (b) Stärke je Fach, (c) Pflicht nach Stundenplan.
   Sie werden getrennt gebaut. *Begründung:* (a) bringt den Fächermix sofort und ohne Code, weil die Mechanik seit
   B-539/B-606 steht. *Kosten:* drei Stories, deren Wechselwirkung (Dosis × Taktung × Obergrenze) beim Schätzen von
   (b) und (c) abgestimmt werden muss. Die Teil-Stories legt der Schritt `geschaetzt` an, nicht diese Bearbeitung.
2. (B-615 Entscheidung 2) **Als Nächstes Friedrich Englisch.** *Begründung:* seine zweite Baustelle, und die Sprach-Übungstypen passen.
   Weg: Englischbuch fotografieren, `lehrwerk-aufbereiten`, dann ein Englisch-Plan neben Plan 8. *Kosten:* der
   Vater fotografiert. Latein für Theodor und Mathe für Friedrich folgen später (Mathe fehlen passende Übungstypen).

## Akzeptanzkriterien

Final, übernommen aus B-615.

- **AK 1.** (B-615 AK 1) **Absichtserklärung, kein Regressionstest** (es gibt keinen Code, den eine Änderung rot machen könnte):
  Friedrichs Englischbuch liegt als Reihe mit Units und Wortlisten in der echten DB, das Lehrwerk hängt am Kind,
  und ein aktiver Englisch-Plan läuft neben Plan 8. Abnahme durch einmaliges Nachsehen: `student/today` für
  Child 3 zeigt Pflichten aus beiden Fächern.

## Schätzung

**Größe: M** — ein ganzes Buch durch den Weg von `lehrwerk-aufbereiten` zu führen ist mehr als ein Werkzeug
(S, B-470) und weniger als ein Umbau: Fotos lesen und je Foto eine Markdown-Datei schreiben, prüfen, Stoffnotizen
und Wortlisten je Unit, laden, Kind umstellen, Plan anlegen. Kein Code.

- **`migration: nein`**, **`vertragsbruch: nein`** — es wird nur Inhalt über die bestehende API geladen.
- **`wo: doku`** — kein Backend- und kein Frontend-Code; die Seiten-Markdown liegen unter `raw/` (gitignored,
  Copyright).

### Risiken

1. **Wartet auf die Fotos.** Ohne sie beginnt nichts; Reflexe und Anschnitte kosten eine zweite Fotorunde (so
   bei Access und Découvertes, siehe `raw/fotoliste.md`).
2. **Es wird in die echte DB geladen.** Vorher sichern (das Startskript `scripts/heimnetz.ps1` sichert beim
   Start); `laden.py` zuerst gegen eine Kopie, dann gegen die laufende Instanz.
3. **Zwei Fächer heißt doppelte Pflicht.** Ohne Obergrenze (B-621) und ohne Verteilung (B-618) addieren sich die
   Pflichten aus Plan 8 und dem Englisch-Plan. Der Englisch-Plan startet deshalb klein; wie klein, legt der Vater
   beim Anlegen fest.
4. **Der Stoff kann zu schwer sein.** Bei Französisch war er es (B-536). Englisch ist Friedrichs stärkeres Fach,
   das Risiko ist kleiner, aber nicht null.

### Angriffsplan

1. Der Vater fotografiert das Buch (Doppelseite je Foto, Fußzeile im Bild) nach `raw/english/<buch>`.
2. `lehrwerk-aufbereiten`: Seiten-Markdown, `md_pruefen.py` grün, Stoffnotizen und Wortlisten je Unit.
3. `laden.py` gegen eine Kopie der DB, dann gegen die echte Instanz; Lehrwerk an Child 3 hängen.
4. Englisch-Plan für die aktuelle Unit anlegen (`ki-creator plan` oder von Hand) und aktivieren; Plan 8 bleibt
   aktiv, weil er ein anderes Fach trägt.

### Testweg

- `md_pruefen.py` und der Prüflauf von `laden.py` (beide aus dem Skill `lehrwerk-aufbereiten`).
- Abnahme durch einmaliges Nachsehen an der echten Instanz: `student/today` für Child 3 zeigt Pflichten aus
  beiden Fächern (AK 1, als Absichtserklärung beschriftet).
- Rollengang: Friedrich spielt eine Runde Englisch in der Sohn-App.

## Verlauf

- **2026-11-17** — angelegt als Teil (a) der Teilung von B-615. Geschätzt: M, `doku`, keine Migration, kein
  Vertragsbruch; wartet auf die Fotos des Buchs.
- **2026-11-17** — Fotos liegen vor: `raw/english/access/Access 5` (90 Fotos, English G Access G9 Band 5,
  Klasse 9), aufbereitet unter `raw/english/access-5-pugling` (90/90 Seiten, `md_pruefen.py` 0 Fehler;
  `laden.py --pruefen` bestanden, 4 Units, 732 Wörter, 12 Start-Übungen, keine `unsicher`-Zeile in den TSVs).
  Probelauf gegen eine Wegwerf-DB auf `:5280`: 27 Übungen, jede 100 % im Selbsttest, zweiter Lauf nur „schon da".
  In der echten DB fehlt die Reihe noch (Kopie vom selben Tag: Child 3 hat nur Découvertes; Klasse 9/Gymnasium
  passt zum Buch). Offen: Laden in die echte Instanz nach Freigabe, dann der Englisch-Plan neben Plan 8.
- **2026-11-17** — `in-arbeit`: Vater gibt frei (Unit 1, 5 neue Wörter/Tag). Echte Instanz, je mit Sicherung
  (`pugling.db.bak-vor-access5-20261117`, `…-vor-friedrich-englisch-20261117`), PIN vom Vater im Terminal getippt:
  `laden.py --child 3` legt Reihe 8 (Units 24–27) und Übungen 78–104 an, Selbsttest 27/27 mit 100 %, Child 3 hat
  Buch 4 (Access G9 Band 5, Unit 1). **Plan 12** „Englisch: Unit 1 Wörter mit Lernkarte“ (17.11.–28.12., Fach 1,
  Unit 24, aktiv): Pflicht täglich Positionen 93–95 = Übungen 78/79/80 mit 2/1/2 neuen Wörtern, Stufe 4 getippt,
  80 %, 20/2; Kür 96–98 = Übungen 93–95. Plan 8 (Französisch) bleibt aktiv – in der DB nachgesehen. Offen für
  `abgenommen`: AK 1 durch einen Blick auf `student/today` von Child 3 (dafür fehlt mir ein Token; der Vater sieht
  es in der Sohn-App oder der Vorschau).
- **2026-11-17** — `abgenommen`. AK 1 (Absichtserklärung, kein Regressionstest): **Rollengang durch den Vater** –
  Friedrichs Startseite zeigt Pflichten aus Englisch und Französisch. Belegt: Selbsttest 27/27 Übungen mit 100 %
  gegen die echte Instanz; in der DB nachgesehen: Plan 12 und Plan 8 aktiv, je eigenes Fach; Buch 4 an Child 3.
  Kein Code, darum weder Testlauf noch Reviewer (`wo: doku`); Markdown-Lint grün. Commits `82712e6e`, `fe979722`.
