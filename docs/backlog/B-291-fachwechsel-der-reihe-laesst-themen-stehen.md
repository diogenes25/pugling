---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/creator]
aliases: [Fachwechsel einer Reihe lässt fremde Themen an Übungen und Units stehen]
status: abgenommen
prio: P3
art: Frage
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf E Teil 4 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-05"
entgangen_bei: []
wartet_auf: ""
---

# B-291 · Wechselt eine Reihe ihr Fach, bleiben die Grammatik-Themen des alten Fachs an Übungen und Units stehen

Die Fachgrenze aus [B-155](B-155-grammatik-themen-als-tags.md) gilt **beim Schreiben** eines Trägers: eine Übung
oder Unit nimmt nur Themen des Fachs ihrer Reihe an. Ein `PATCH` der Reihe, der `SubjectId` ändert oder leert
(`TextbookSeriesController`), lässt jede bestehende Themen-Zuordnung des alten Fachs stehen – und der nächste PUT
einer solchen Übung mit ihren eigenen Ids wird von `GrammarTopicsProblemAsync` abgewiesen: der Creator muss die
Themen entfernen, um überhaupt etwas speichern zu können.

Das ist dieselbe Drift, die `CategoryId` beim Fachwechsel einer Reihe heute schon hat (dort ebenfalls nicht
behandelt). Zwei Wege, beide eine Produktentscheidung: (a) der Fachwechsel wird mit 409 verweigert, solange ein
Träger der Reihe Themen trägt; (b) der Fachwechsel löst die Zuordnungen (und Kategorien) als bekannte Folge, mit
Hinweis im Löschdialog/Formular. Bis dahin ist es eine bekannte Grenze von B-155, dort im Verlauf benannt.

## User Story

Als **Creator** möchte ich, dass ein Fachwechsel meiner Reihe keine Zuordnungen still verwaisen lässt: solange Übungen oder Units
Themen bzw. Kategorien des alten Fachs tragen, wird der Wechsel verweigert, und die Antwort sagt mir, wie viel ich erst lösen muss.

## Entscheidungen

1. (aus dem Verlauf, im Auftrag) **409 verweigern**, Antwort mit Anzahl betroffener Übungen und Units.
2. **(im Lauf ausformuliert)** Gezählt werden Übungen der Reihe mit einem Thema **oder** einer Kategorie des alten Fachs und Units mit
   einem Thema des alten Fachs. Verglichen wird das **resultierende** Fach (`clearSubject` gewinnt über eine Id, wie beim Schreiben),
   vor jedem Schreibzugriff. Eine Reihe ohne Fach hat nichts zu prüfen. Neuer Code `series_subject_in_use` (409).

## Akzeptanzkriterien (je Fall)

| Fall | Erwartet | Beleg | Mutante und was sie rot macht |
|---|---|---|---|
| Übung mit Thema, `subjectId` neu | 409, „1 exercise(s) and 0 unit(s)", Fach bleibt | `ReiheFachwechselTests` `Thema_An_Einer_Uebung…` | Übungs-Themen-Klausel weg → rot dieser + `Nur_Die_Eigenen…` (gemessen, Stand B-402) |
| Unit mit Thema | 409, „0 exercise(s) and 1 unit(s)" | `Thema_An_Einer_Unit…` | Unit-Zählung weg → rot dieser + `Nur_Die_Eigenen…` (gemessen, Stand B-402) |
| Übung mit Kategorie | 409 | `Kategorie_An_Einer_Uebung…` | Kategorie-Klausel weg → rot nur dieser (gemessen, Stand B-402) |
| `clearSubject` | 409 | `Fach_Leeren…` | `ClearSubject` ignoriert → rot dieser + `Fach_Leeren_Mit_Uebungen…` (B-401) (gemessen, Stand B-402) |
| zwei Reihen desselben Fachs, nur A trägt ein Thema | B darf wechseln (200), A: 409 mit „1 exercise(s) and 1 unit(s)" | `Nur_Die_Eigenen_Traeger…` | Reihenfilter Units weg → rot nur dieser; Reihenfilter Übungen weg → rot dieser + beide „Fach leeren"-Fälle (gemessen, Stand B-402) |
| ohne Zuordnungen / gleiches Fach erneut | 200 | `Ohne_Zuordnungen…` | **Gegenprobe**, vorher und nachher grün |
| Klickweg: Bearbeiten-Formular der Reihe | Begründung sichtbar, Fach bleibt | `e2e/reihe-fachwechsel-gesperrt.spec.ts` | Sperre aus – gemessen: rot an der Zusicherung der Begründung (seit B-403 der deutsche Text) |

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 4 (`1c1fe62`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  🟡-Fund des frischen Prüfers; als `Frage` abgelegt (Entscheidung zwischen 409 und Lösen), nicht als Entgleiten.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): Antwort (a): der Fachwechsel wird mit 409 verweigert, solange Träger der Reihe Themen oder Kategorien des alten Fachs tragen; die Antwort nennt die Anzahl der betroffenen Übungen und Units. Kein stiller Verlust von Zuordnungen. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-05** — ausformuliert, geschätzt (S) und gebaut in Lauf Z, Sprint 14. **Rote Probe** gegen den Ausgangszustand: erwartet 4 rot
  + 1 Gegenprobe grün, gemessen 4 rot (`OK` statt `Conflict`) + 1 grün. Mutanten je Fall per Skript mit Build-Prüfung (Tabelle oben).
  Volle Suite 1113/1113.
- **2026-11-05** — Review `pugling-reviewer`: 🟡 kein Test hielt den Reihen-Filter (jeder Test hatte sein eigenes Fach – zwei Reihen
  desselben Fachs hätten sich gegenseitig gesperrt, unbemerkt). **Zähler 1 von 5.** Fall ergänzt, beide Filter-Mutanten gemessen: je nur
  der neue Fall rot. 🟢 kein anderer Weg verschiebt Übungen/Units zwischen Reihen; Fach löschen kaskadiert sauber; 🟢 Wettlauf zwischen
  Zählung und Speichern (paralleler Übungs-PUT fügt ein Thema hinzu) – für einen Ein-Autor-Katalog hingenommen; 🟢 `clearSubject` auf
  einer Reihe mit Übungen ohne Themen geht durch → [B-401](B-401-fach-leeren-laesst-uebungen-ohne-fach.md). Volle Suite 1114/1114.
- **2026-11-05** — **abgenommen.** Rollengang Creator als E2E: `e2e/reihe-fachwechsel-gesperrt.spec.ts` (Aufbau per API, Fachwechsel per
  Klick im Bearbeiten-Formular, Begründung sichtbar, Fach bleibt nach Neuladen) – volle Playwright-Suite **117 passed** nach dem letzten
  Laufzeit-Edit (danach nur ein Testfall). Vater und Sohn Regressionszeugen. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `b6e3724`.
- **2026-11-05** — **Nachschau** (Lauf Z, Retro Sprint 15; frischer `pugling-reviewer`, `git show b6e3724`, ohne Build, am Code
  nachgerechnet): geprüft (1) jede Mutanten-Zeile gegen alle sechs Fälle, (2) ApiErrors-Doku und Entscheidung 2 gegen den Code, (3)
  Fehler-Katalog, Vertrag und die einzige Stelle im Frontend, die eine Reihe patcht, (4) die Berichtigungen aus B-399. **Entgangen:** zwei
  Mutanten-Zeilen gelten seit dem Review-Fall nicht mehr → [B-402](B-402-b291-mutantenzeilen-veraltet.md); der neue Code hat keinen
  deutschen Text im Vater-Web → [B-403](B-403-reihe-fachwechsel-meldung-ohne-deutschen-text.md).
