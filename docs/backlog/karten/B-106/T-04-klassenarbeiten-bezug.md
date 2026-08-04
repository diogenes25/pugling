# T-04 · Bekommt `Klassenarbeit` einen `SeriesUnit`-Bezug?

Status: offen           <!-- offen | beansprucht | entschieden -->
Typ: grilling           <!-- research | prototype | grilling | task -->
Blockiert durch: T-01

## Frage

`Klassenarbeit.SubjectId` ist heute optional (`KlassenarbeitEntities.cs:73`), ohne Chapter- oder
Lehrwerk-Bezug. Nach der Verschmelzung referenzieren die enthaltenen Übungen `SeriesUnit` statt
`Chapter` — soll `Klassenarbeit` diesen Bezug sichtbar/filterbar machen (z. B. „Klassenarbeit zu Unit 3"),
oder bleibt sie bewusst lehrwerk-agnostisch (mehrere Units/Reihen in einer Klausur zulässig, wie heute
mehrere Chapter)?

## Antwort

