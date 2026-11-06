---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [series_subject_in_use ohne deutschen Text]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-291]
---

# B-403 · Die Sperre beim Fachwechsel einer Reihe erscheint im deutschen Vater-Web als englischer Satz mit Ref-Nummer

`frontend/src/lib/api.ts` (`GERMAN_PROBLEM_TEXT`) übersetzt Fehlercodes, auf die der Nutzer selbst handeln kann – die Geschwister
`subject_in_use`, `publisher_in_use`, `interest_tag_in_use` stehen dort, gehalten von `errorMessage.test.ts`. Der neue Code
`series_subject_in_use` aus [B-291](B-291-fachwechsel-der-reihe-laesst-themen-stehen.md) (und B-401) fehlt: der Creator liest den
englischen Server-Satz plus `(Ref: …)`. Zu klären beim Ausformulieren: wie die Anzahlen (Übungen/Units) in den deutschen Text kommen.
Die E2E-Spec `reihe-fachwechsel-gesperrt.spec.ts` prüft heute den englischen Wortlaut und muss mitziehen.

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** Ein **fester** deutscher Text, ohne Anzahlen – wie beim Geschwister `exercise_in_use`. Die Anzahlen
   stehen nur im englischen `detail`; ihn zu zerlegen hieße, den Vertrag an eine Formulierung zu binden. Der Satz nennt beide Gründe
   (Zuordnungen beim Wechsel, die Übungen selbst beim Entfernen) und beide Auswege. *Kosten:* der Creator erfährt nicht mehr, **wie
   viele** Übungen/Units betroffen sind.

## Akzeptanzkriterien

| Fall | Beleg | Mutante |
|---|---|---|
| Code übersetzt, beide Gründe genannt | `errorMessage.test.ts` „übersetzt die Sperre beim Fachwechsel einer Reihe …" | Eintrag umbenannt – gemessen: rot (1 von 9) |
| Klickweg zeigt den deutschen Satz | `e2e/reihe-fachwechsel-gesperrt.spec.ts` | Eintrag umbenannt – gemessen: rot an der Text-Zusicherung |

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf Z, Sprint 14 (Retro Sprint 15; frischer `pugling-reviewer`, 🟡). Am Code gelesen.
- **2026-11-05** — gebaut in Lauf AA, Sprint 16. **Rote Probe** gegen den Ausgangszustand: erwartet 1 rot, gemessen 1 rot („expected 'Some
  English sentence. (Ref: trace-12…' to contain 'Grammatik-Themen oder Kategorien'"). E2E-Zusicherung von englischem Wortlaut auf den
  deutschen Satz umgestellt, 1 passed; Mutante (Tabelle) gemessen, zurück → grün. `tsc` sauber.
- **2026-11-05** — Review `frontend-reviewer`: drei 🟡 am Satz – (1) „oder wähle ein anderes Fach" ist nie ein Ausweg aus dieser Sperre,
  (2) die Oberfläche nennt Kategorien „Art" und kann sie im Bearbeiten-Dialog nicht entfernen, nur die Übung löschen, (3) Satzbau. Alle
  drei ändern Code → **Zähler 3 von 5.** Satz neu: nur Auswege, die es gibt (Themen an Unit/Übung herausnehmen, Übung mit Art löschen,
  Fach entfernen nur ohne Übungen). Test zieht mit (`Art dieses Fachs`, `nur löschen`, `keine Übungen trägt`, **nicht** `anderes Fach`) –
  rot gegen den alten Satz gemessen („expected 'Übungen oder Units …' to contain 'Grammatik-Thema'"). 🟢 die E2E-Zusicherung sucht seitenweit
  statt im Formular (vorbestehend), 🟢 der Lösch-Fall hat keine E2E, 🟢 fünfzehn weitere Codes ohne deutschen Text (Liste im Review) –
  ohne Zählung zurückgestellt; der wichtigste Kandidat ist `series_without_subject`.
- **2026-11-05** — Delta-Review `frontend-reviewer`: 🟡 der Satz zum Entfernen nannte nur „keine Übungen", der Server sperrt auch bei
  Unit-Themen → ergänzt. **Zähler 4 von 5.** Zusicherung `keine Übungen trägt und keine ihrer Units` gegen den vorigen Satz gemessen: rot.
  Kein weiteres Delta-Review – bei Zähler 4 ist das nächste Review die Grenze der Nacht; stattdessen beide Varianten selbst gegen
  `SubjectInUseProblemAsync` gelesen (Selbst-Check, so beschriftet): Wechsel zählt Unit-Themen, Übungs-Themen, Übungs-Arten; Entfernen
  zusätzlich jede Übung – der Satz nennt alle vier.
- **2026-11-05** — **abgenommen.** Rollengang Creator als E2E: `e2e/reihe-fachwechsel-gesperrt.spec.ts` zeigt den deutschen Satz am Reihen-Formular, Fach bleibt nach Neuladen. Volle Suite 1115/1115, Playwright 117 passed nach dem letzten Edit; Vater und Sohn Regressionszeugen. Commit: siehe nächster Eintrag.
