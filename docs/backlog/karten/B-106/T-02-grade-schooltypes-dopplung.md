# T-02 · Grade/SchoolTypes-Dopplung zwischen Exercise-Metadaten und Lehrwerk

Status: offen           <!-- offen | beansprucht | entschieden -->
Typ: grilling           <!-- research | prototype | grilling | task -->
Blockiert durch:

## Frage

`Exercise` trägt eigene Klassenstufe/Schulart-Metadaten (siehe „Übungs-Metadaten" in
`backend/Pugling.Api/CLAUDE.md`); `TextbookSeries`/`SeriesUnit` tragen ebenfalls `Grade`
(`CurriculumEntities.cs:57`) und potenziell `SchoolTypes`-Filter (B-63 Entscheidung 7). Sobald jede
Übung über `SeriesUnitId` an eine Unit mit eigenem `Grade` hängt: bleibt `Exercise.Grade` als
unabhängiges Feld bestehen (zwei Wahrheiten, die auseinanderlaufen können), wird es aus der Unit
abgeleitet (keine eigene Spalte mehr), oder ist es ein bewusst redundantes Anzeige-Feld mit
Validierung gegen die Unit? B-63 Entscheidung 8 hat exakt diese Frage bereits als „Umbau am
Übungs-Katalog, nicht am Lehrwerk-Modell" zurückgestellt — hier ist sie fällig.

## Antwort

