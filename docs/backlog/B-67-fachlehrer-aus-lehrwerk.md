---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Fachlehrer vorbelegen, Profil aus Reihe]
status: ausformuliert
prio: P2
art: Wunsch
quelle: remark #8
---

# B-67 · Der Fachlehrer fragt nach Fach und Sprachen, die im gewählten Lehrwerk längst stehen

## User Story

Als Vater möchte ich beim Anlegen eines Fachlehrers nach der Wahl des Lehrwerks Fach, Lern- und
Muttersprache **vorbelegt** bekommen, damit ich nicht dreimal eintippe, was das Buch schon weiß.

## Ist-Stand am Code

Im Formular sind es vier voneinander unabhängige Eingaben
([VaterFachlehrer.tsx](../../frontend/src/vater/VaterFachlehrer.tsx)):

- Fach — Pulldown (`:221`)
- Lehrwerk — Pulldown (`:249`)
- Lernsprache — **Freitext** mit Vorgabe `en` (`:258`, Startwert `:140`)
- Muttersprache — **Freitext** mit Vorgabe `de` (`:262`, Startwert `:141`)

Die Daten zum Ableiten liegen vor: `TextbookSeries` trägt `SubjectName`/`SubjectId` und
`SourceLanguage`/`TargetLanguage`
([CurriculumEntities.cs:24-33](../../backend/Pugling.Api/Models/CurriculumEntities.cs)); der Listen-Endpunkt
liefert sie mit ([TextbookSeriesController.cs:47](../../backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs)).

Am Zielobjekt sind die Felder eigenständig: `CreatorProfile.SubjectId` ist nullable,
`SourceLang`/`TargetLang` haben die Vorgaben `en`/`de` (`CurriculumEntities.cs:89,101,103`), `SeriesId` ist
nullable (`:98`).

## Die echte Lücke

Reine Oberflächen-Arbeit — **das Modell muss nicht geändert werden.** Die Werte sind bewusst am Profil
eigenständig, weil ein Profil ohne Reihe (`SeriesId` nullable) sein Fach und seine Sprachen selbst braucht;
und das Matching gewichtet Reihe (8) und Fach (4) getrennt, ein abgeleitetes Fach bliebe also wirksam.

Es fehlt nur: beim Wählen der Reihe die drei Felder vorbelegen — **beschreibbar bleibend**, nicht sperrend.

Berührt [B-63](B-63-lehrwerk-hierarchie.md) (Sprachen als Auswahl statt Freitext), hängt aber nicht davon
ab: das Vorbelegen funktioniert auch mit Freitextfeldern.

## Offene Punkte

1. **Nur leere Felder vorbelegen oder auch überschreiben?** *Empfehlung: leere füllen, gefüllte in Ruhe
   lassen* — sonst verliert ein Wechsel der Reihe beim Bearbeiten eine bewusste Abweichung.
2. **Was, wenn die Reihe kein Fach trägt** (`SubjectId` nullable, `SubjectName` frei)? *Empfehlung: nur
   setzen, wenn ein Katalog-Fach dranhängt* — einen Freitextnamen auf ein Pulldown zu raten geht schief.
3. **Sichtbar machen, dass ein Wert abgeleitet ist?** *Empfehlung: ein kurzer Hinweis („aus dem Lehrwerk
   übernommen") statt einer Sperre* — dieselbe Haltung wie bei der Slug-Meldung im Lehrwerk-Formular
   ([VaterLehrwerke.tsx:328-331](../../frontend/src/vater/VaterLehrwerke.tsx)).

## Akzeptanzkriterien

1. Wählt man im Fachlehrer-Formular ein Lehrwerk, füllen sich leere Felder für Fach, Lern- und
   Muttersprache aus der Reihe.
2. Bereits gefüllte Felder bleiben unverändert.
3. Alle drei Felder bleiben von Hand änderbar; ein Profil ohne Lehrwerk verhält sich wie bisher.
4. Trägt die Reihe kein Katalog-Fach, bleibt das Fach-Pulldown leer statt geraten.
5. Ein Komponententest deckt „Reihe wählen → Felder gefüllt" und „gefülltes Feld bleibt" ab.

## Verlauf

- **2026-08-02** — angelegt aus Anmerkung #8; Ist-Stand am Code belegt, Befund:
  [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#e--fachlehrer-ableitung-aus-dem-lehrwerk-8).
