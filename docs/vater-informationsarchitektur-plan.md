---
tags: [typ/plan, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [Vater-Navigation, Informationsarchitektur, Nav-Umbau, Übungen-Seite aufteilen]
---

# Umbauplan: Informationsarchitektur des Vater-Webs

Status: **Umgesetzt** (Etappen 1–4, 2026-07-28). `npm run build`, `npm test` (9) und alle 14
Playwright-E2E grün. Quelle sind die Anmerkungen **10, 11, 12**
(alle aus `/vater/exercises`, alle vom 2026-07-28) – die belegten Analysen stehen an den Anmerkungen
selbst und im Export unter [anmerkungen/aktuell.md](anmerkungen/aktuell.md).

## Der gemeinsame Befund

Drei Beobachtungen, **ein** Problem: das Vater-Web hat keine Informationsarchitektur. Es ist über
Features gewachsen, und jedes neue Feature bekam entweder einen Nav-Eintrag oder einen Platz auf der
Übungen-Seite. Beides ist inzwischen voll.

| Anmerkung | Beobachtung | Befund |
|---|---|---|
| 10 | „Die Button in Übersicht sind nicht schön angeordnet. Es ist auch keine ‚Übersicht' sondern mehr eine Navigation" | 13 gleichrangige NavLinks in einer flachen, umbrechenden Reihe (`VaterApp.tsx:32-47`, `index.css:204`) |
| 11 | „Zu viele Features auf dieser Seite … Erstellen ist ein abgeschlossener Bereich, Bearbeiten und Zuweisen ebenfalls" | `/vater/exercises` trägt vier Anliegen in **einem** `<form>` (`VaterExercises.tsx:195/200/202/316`) |
| 12 | „Was bringt mir ‚Katalog verwalten'? Warum ist es in ‚Übungen' versteckt?" | Eingeklappt hinter einem Ghost-Button (`CatalogAdmin.tsx:42-47`), kein eigener Nav-Eintrag |

**Der Ordnungsbegriff, der schon da ist:** Die drei Ebenen des Produkts
([grundprinzip.md](grundprinzip.md)) schneiden API *und* Code – nur die Oberfläche nicht. Genau daran
lässt sich die Navigation aufhängen, statt eine neue Taxonomie zu erfinden: **Inhalte** = Creator,
**Steuern** + **Belohnen** = Supervisor.

## Etappe 1 – Kopfzeilen-Navigation gruppieren (Anmerkung 10) — **umgesetzt**

`VaterApp.tsx` (Kopfzeile + neue Komponente `NavGroup`), `index.css:202-212`.

Umgesetzt am 2026-07-28: Gruppen mit `role="group"`/`aria-label` und sichtbarer, `aria-hidden`
gesetzter Beschriftung (sonst nennt ein Screenreader den Gruppennamen zweimal); Kopfzeile und
Navigation in getrennten Zeilen; „Übersicht" → „🏠 Kinder & Pläne"; „Neuer Plan" aus der Navigation
entfernt; alle Einträge mit Symbol, `Anmerkungen` auf 🐞. Die sieben Textstellen, die die Startseite
beim alten Namen nannten, sind mitgezogen (`VaterKind.tsx`, `VaterPlanDetail.tsx`, `VaterWizard.tsx`,
`VaterClassTests.tsx`, `VaterKonto.tsx`, `VaterRewards.tsx`, `VaterShop.tsx`).

Geprüft: `npm run build` (Typecheck) und `npm test` grün; im laufenden Dev-Server nachgesehen –
zwölf Nav-Links, Umbruch fällt zwischen die Gruppen, Gruppennamen erscheinen nicht als eigene
Vorlesestellen im Accessibility-Baum. **Nicht** geprüft: Playwright – Port 5200 war von der echten
Instanz belegt (`playwright.config.ts:46-52` verlangt eine eigene Wegwerf-DB).

**Gruppen** (aus den vorhandenen Routen, nichts Neues):

| Gruppe | Einträge |
|---|---|
| *(Start)* | `/vater` |
| **Inhalte** | Übungen, Vokabeln, Lückentexte¹, Katalog¹, Lehrwerke, Fachlehrer, Bilder |
| **Steuern** | Assistent, Klassenarbeiten |
| **Belohnen** | Belohnungen, Shop, Kontostand |

¹ entstehen erst in Etappe 2 – bis dahin führt die Gruppe sie nicht.

Vier Änderungen, jede mit eigener Begründung:

1. **„Übersicht" umbenennen.** Der Name verspricht, was die Seite nicht hält: echte Übersichtsdaten
   trägt nur der Abschnitt „Heute" (`VaterDashboard.tsx:43-66`), der Rest ist Verlinkung in andere
   Bereiche (`:78`, `:84`, `:121`). Neuer Name **„Kinder & Pläne"** – das ist der Inhalt.
2. **„Neuer Plan" aus der Navigation nehmen** (`:44`). Eine *Aktion* zwischen Orten, und doppelt
   vorhanden: die Seite „Kinder & Pläne" hat den Knopf am Abschnitt, wo er hingehört
   (`VaterDashboard.tsx:113`). Der Assistent bleibt der geführte Weg.
3. **Beschriftung einheitlich.** Heute 10 Einträge mit Emoji, 3 ohne (`:33`, `:38`, `:44`), und
   „Klassenarbeiten" (`:43`) wie „Anmerkungen" (`:46`) tragen dasselbe 📝. Entweder alle oder keins –
   Vorschlag: alle, `Anmerkungen` auf 🐞 (es ist ein Entwicklungswerkzeug).
4. **Nav aus dem geteilten Flex lösen.** `nav` liegt im selben Container wie Marke, Profil-Link und
   Abmelden (`:30-51`, `index.css:202`); bricht die Nav um, verschiebt sie den rechten Block mit.
   Eigene Zeile.

**Darstellung der Gruppen:** flache Reihe mit Gruppen-Trennern (`role="group"` +
`aria-label`), **keine Dropdowns**. Ein Menü kostet Tastatur-, Escape- und `aria-expanded`-Verhalten
und verbirgt Ziele hinter einem Klick – bei 12 Einträgen ist das teurer als es einbringt. Bleibt es
zu breit, kommt danach der Wechsel auf eine linke Spalte (Sidebar), nicht auf Menüs.

## Etappe 2 – Katalog und Lückentexte als eigene Routen (Anmerkung 12) — **umgesetzt**

Beide sind **Bausteine**, keine Teile des Anlegens – wie der Vokabel-Store, der längst seine eigene
Route hat (`/vater/vocab`). Genau das ist die Antwort auf „warum versteckt".

Umgesetzt am 2026-07-28:

- `/vater/katalog` → `VaterKatalog.tsx` umschließt `CatalogAdmin` und lädt die Fächer selbst (vorher
  kamen sie als Prop von der Übungen-Seite). `/vater/lueckentexte` → `VaterLueckentexte.tsx` umschließt
  `ClozeTexts`. Beide in der Nav-Gruppe **Inhalte**.
- Die Einklapper samt „Schließen" sind in **beiden** Komponenten entfallen – die Route *ist* jetzt der
  Auf-/Zu-Zustand.
- **Über den Plan hinaus, aber nötig:** Fach und Kapitel lassen sich im Katalog jetzt auch *anlegen*
  (`NewName`-Zeilen, ein neues Fach wird gleich ausgewählt). Ohne das wäre der Katalog eine Seite, auf
  der man nur umbenennen und löschen kann – und Etappe 3 könnte die Anlege-Felder nicht wegnehmen, weil
  ihr Verweis ins Leere führte. Dabei bekam `NewName` ein `fieldId` von außen: mit drei Instanzen war die
  feste DOM-`id` dreifach vergeben und jedes `label` zeigte auf dasselbe Feld.
- Auf der Übungen-Seite stehen zwei Links dorthin – der Weg von „Kapitel fehlt" zum Katalog darf nicht
  über die Navigation führen.

**Ein Fund aus dem Test, der die Umsetzung geändert hat:** Der erste Wurf von `VaterKatalog` rief
`{subjects.loading ? "Lade…" : <CatalogAdmin/>}`. Weil `onCatalogChanged` ein `reload` auslöst und das
`loading` erneut auf `true` setzt (`useAsync.ts:27`), wurde `CatalogAdmin` bei **jeder** Katalog-Änderung
ausgetauscht und verlor sein `useState`: die Fach-Auswahl sprang auf „– wählen –" und die
Erfolgsmeldung erschien nie. `vater-von-null.spec.ts` hat das sofort aufgedeckt. Der Platzhalter gilt
jetzt nur fürs erste Laden (`subjects.data === null`).

## Etappe 3 – Anlegen und Verwalten trennen (Anmerkung 11) — **umgesetzt**

Der Kern. Vorher steckte die Bestandsliste **innerhalb** des Anlege-`<form>`s, und die Liste lud an
`okMsg` gekoppelt neu – 575 Zeilen für vier Anliegen.

| Route | Inhalt | Datei |
|---|---|---|
| `/vater/exercises` | **Verwalten**: Fach/Kapitel als Filter, Liste mit Ausprobieren / Bearbeiten / Verwendung / Löschen, Sortierung, Paging, geteilte Bibliothek; oben „+ Neue Übung". | `VaterExercises.tsx` (+ `ExerciseManageRow`) |
| `/vater/exercises/neu` | **Anlegen**: Fach & Kapitel (Auswahl), Typ & Metadaten, Inhalts-Editor, „Übung anlegen" + „🧪 Ausprobieren" auf die frisch angelegte Übung. | `VaterExerciseCreate.tsx` (+ `VocabRefPicker`) |

`/vater/exercises` bleibt, was es war – der Ort, an den Gewohnheit und vier E2E zeigen; es wird nur zur
Verwaltung. Die Anlege-Route trägt **keinen** Nav-Eintrag: sie ist eine Aktion, erreichbar über
„+ Neue Übung" (dieselbe Regel wie bei „Neuer Plan" in Etappe 1).

Drei Dinge, die beim Schneiden dazukamen:

- **Die Auswahl reist mit.** Beide Richtungen reichen `?subjectId=&chapterId=` durch, sonst müsste der
  Vater Fach und Kapitel nach jedem Wechsel neu einstellen. Die E2E prüfen genau das (`toHaveURL`).
- **Fach/Kapitel sind in der Verwaltung ein Filter, keine Pflicht.** Die Liste erscheint jetzt, sobald ein
  Fach gewählt ist (Kapitel = „– alle –"). Vorher blieb die Seite leer, bis auch ein Kapitel stand – das
  war die Logik des *Anlegens*, die dem Suchen im Weg stand.
- **„Neues Fach" / „Neues Kapitel" sind weg**, ersetzt durch einen Satz mit Link auf den Katalog. Sie
  waren zwei von vier Feldern der ersten Karte – gleiches Gewicht wie die Pulldowns, die man jedes Mal
  braucht. Möglich wurde das erst dadurch, dass der Katalog seit Etappe 2 selbst anlegen kann.

**Zur Notiz in Anmerkung 11:** „Zuweisen" war auf dieser Seite gar nicht vorhanden – das passiert im
Plan (`PlanPositions.tsx`) bzw. im Assistenten (`VaterWizard.tsx`); von hier gibt es nur den Lese-Blick
„Verwendung". Die dritte Trennung war also schon da; die ersten zwei fehlten.

## Etappe 4 – Tests nachziehen

Läuft **mit jeder Etappe** mit, statt am Ende: ein Umbau, dessen Specs erst später nachgezogen werden,
läuft eine Etappe lang ohne Netz.

Erledigt (Etappen 1 und 2), alle 14 E2E grün:

| Datei | Stelle | Was daraus wurde |
|---|---|---|
| `e2e/full-flow.spec.ts` | `:39` | `link "Neuer Plan"` mit `exact:true` war der Nav-Eintrag → jetzt der Knopf am Abschnitt „Lehrpläne" (`/Neuer Plan/`) |
| `e2e/bilder.spec.ts` | `:119` | dito, dazu ein `goto("/vater")` – der Klick kam von der Kind-Seite |
| `e2e/vater-von-null.spec.ts` | `:173-181` Katalog, `:206-207` Lückentexte | `goto` auf die neuen Routen; die Gegenprobe „Pulldown kennt den neuen Namen" wandert zurück auf `/vater/exercises` |
| `e2e/anmerkungen.spec.ts`, `uebungstypen.spec.ts` | `goto("/vater/exercises")` | unverändert tragfähig |

Erledigt (Etappe 3):

| Datei | Was daraus wurde |
|---|---|
| `e2e/uebungstypen.spec.ts` | Fach/Kapitel im Katalog, die sechs Typen auf `/vater/exercises/neu`, Liste und Bearbeiten in der Verwaltung; der Manifest-Vergleich steht jetzt **vor** dem Wechsel, weil das Typ-Pulldown zur Anlege-Seite gehört |
| `e2e/vater-von-null.spec.ts` | derselbe Schnitt im „von Null"-Flow, plus die Gegenprobe, dass „Übungen verwalten" die Auswahl als Query mitnimmt |

Abschluss je Etappe: `npm run build` (Typecheck), `npm test`, `npx playwright test`.

## Reihenfolge und Risiko

Etappen 1 → 2 → 3 → 4, jede für sich lauffähig. Etappe 1 ist rein additiv (Labels und CSS), Etappe 2
verschiebt zwei fertige Komponenten in eigene Routen, erst Etappe 3 zerlegt eine 575-Zeilen-Datei.
Wer abbrechen muss, hat nach 1 und 2 schon den größeren Teil des Nutzens.

Kein Backend-Anteil: alle drei Anmerkungen sind reine Oberfläche.

## Nebenbefund (nicht Teil dieses Plans)

Aus dem Fehler-Mitschnitt von Anmerkung 10: 4× `400 no_checkable_content` auf
`creator/exercises/24|25/preview`. Beide sind Vokabel-Übungen „Einfach Vokabeln" (Autor Vater 6) mit
**null Items**. Die Fehlerantwort ist korrekt (`ExercisePreviewController.cs:42`) – die Ursache liegt
davor: `VocabularyExercisesController.ValidateConfigAsync`
([ExerciseControllers.cs:39-61](../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs))
prüft die *Inhalte* von `refs`/`items`, verlangt aber **nicht mindestens eines**. Leere
Vokabel-Übungen sind per API anlegbar; das UI blockt sie (`exerciseConfig.tsx:560`), die API nicht.
Eigener, kleiner Fix – gehört nicht in diesen Umbau.
