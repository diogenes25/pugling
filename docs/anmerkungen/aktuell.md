# Anmerkungen – Export

Stand: 2026-07-27 23:28 UTC · 7 Einträge · Filter: alle

> Erzeugt von `GET api/v1/remarks/export`. **Nicht von Hand bearbeiten** – die Quelle ist
> die Datenbank. Status und Antworten ändert der Skill `anmerkungen` über die API.

## #1 · Question · erledigt

- **Erfasst:** 2026-07-27 21:16 UTC von Konto 11 (Supervisor)
- **Wo:** `/vater/exercises` (vater)

Wie kann ich vorhandene Übungen bearbeiten? Ich möchte z. B. neue Vokabeln hinzufügen bzw. entfernen.

**Antwort** (claude-code, 2026-07-27 22:08 UTC):

Bearbeiten gibt es: Knopf "Bearbeiten" an der Uebungszeile (VaterExercises.tsx:543) oeffnet ExerciseEditModal; bei Vokabeluebungen pflegt der ItemEditor (ExerciseEditModal.tsx:319) die Wortpaare - hinzufuegen per Store-Suche oder freier Eingabe (:413, :458), entfernen ueber api.deleteExerciseItem (:351). KORREKTUR meiner ersten Antwort (im Browser nachgemessen): Der Grund war nicht fremdes Eigentum. Die Liste "Uebungen in diesem Kapitel" erscheint erst, wenn Fach UND Kapitel gewaehlt sind (VaterExercises.tsx:315) - vorher sieht man nur das Anlege-Formular. Und sie zeigt standardmaessig nur EIGENE Uebungen (Checkbox "geteilte Uebungen anderer Vaeter anzeigen", :320-322, Standard aus); Vater 7 besitzt genau eine (Id 27), die mit funktionierendem Bearbeiten-Knopf erscheint. Gebaut: Mit eingeschalteter Checkbox trugen die 10 fremden Uebungen des Kapitels keinen Grund fuer das fehlende Bearbeiten - jetzt steht dort "kein Schreibrecht - nur ausprobieren & zuweisen" (VaterExercises.tsx, ExerciseRow). Verifiziert: Hinweis an allen 10 fremden, Bearbeiten nur an der eigenen.

## #2 · Idea · erledigt

- **Erfasst:** 2026-07-27 21:17 UTC von Konto 11 (Supervisor)
- **Wo:** `/vater/exercises` (vater)

Wie kann ich Vokqbeln bearbeiten? hIER SOLL EIN lINK DER vOKABELN ZUM ENTSPRECHENDEN Seite eingefügt werden

**Antwort** (claude-code, 2026-07-27 22:08 UTC):

Gebaut. Jedes Wortpaar im Bearbeiten-Dialog ist jetzt ein Link auf den Store-Eintrag: ExerciseEditModal.tsx ItemEditor.storeHref() erzeugt /vater/vocab mit search=<Wort> plus Sprachpaar aus der Uebung (src/tgt), ItemRow rendert das Wort als <a target=_blank>. Neues Tab bewusst: der Dialog haelt ungespeicherte Eingaben, ein Wechsel im gleichen Tab wuerde sie verwerfen. Empfangsseite ergaenzt: VaterVocab liest search/src/tgt als STARTWERT (useSearchParams), damit die Zielseite gefiltert und mit richtiger Sprachkombination oeffnet. Verifiziert im Browser: Link auf hello ergibt /vater/vocab mit search=hello, src=en, tgt=de; Zielseite filtert auf 1 Treffer.

## #3 · Idea · erledigt

- **Erfasst:** 2026-07-27 21:19 UTC von Konto 11 (Supervisor)
- **Wo:** `/vater/vocab` (vater)

Die Bilder sollen hier ebenfalls hochgeladen und bearbeitet werden können

**Antwort** (claude-code, 2026-07-27 22:08 UTC):

Gebaut. Das Bilder-Panel je Vokabelzeile kann jetzt auch hochladen: VocabMediaPanel.tsx hat eine Komponente UploadAndLink (Dateifeld accept=image/*, Beschreibung mit dem Wort vorbelegt) und MediaLinkEditor.uploadAndLink() ruft api.uploadMedia und ordnet das Ergebnis in einem Zug zu. Die Beschreibung ist Pflicht, weil der Server sie als Alt-Text nutzt (MediaAssetsController.cs:215-216). Der Upload landet in derselben Bibliothek wie ueber /vater/media - keine zweite Ablage. Gilt fuer beide Ebenen (Vokabel und uebungslokales Item). Verifiziert mit einem echten Upload an der Vokabel goes: Asset angelegt, auf Thumb/Card/Full skaliert, sofort verlinkt; Testbild danach wieder entfernt (Bibliothek und Zuordnung sind wie vorher leer).

## #4 · Ui · erledigt

- **Erfasst:** 2026-07-27 21:20 UTC von Konto 11 (Supervisor)
- **Wo:** `/vater/vocab` (vater)

Der löschen-button ist nicht zu sehen

**Antwort** (claude-code, 2026-07-27 22:08 UTC):

Bestaetigt und behoben - im Browser nachgemessen. Der Knopf war da (VaterVocab.tsx:345), aber die Aktionsspalte stand in EINER Zeile: die Tabelle brauchte 868px Container-Breite, darunter rutschte "Loeschen" als letzter Knopf in den horizontalen Ueberlauf (bei 380px Container 478px ausserhalb) - sichtbar nur fuer den, der seitwaerts scrollt. Fix: die Aktionszelle darf umbrechen (flexWrap) und hat eine Untergrenze (minWidth 230), damit die Spalte nicht auf einen Knopf zusammenschrumpft und jeder Knopf auf eigener Zeile landet; Stil liegt als geteilte Konstante actionCell in VaterVocab.tsx. Ergebnis: min-content faellt auf 772px, obwohl die neue Tags-Spalte (Anmerkung 5) dazugekommen ist; Loeschen ist auf normalen Breiten immer sichtbar. Anlass war uebrigens meine eigene Aenderung: die Tags-Spalte schnitt "Loeschen" bei 1456px ab.

## #5 · Bug · erledigt

- **Erfasst:** 2026-07-27 21:20 UTC von Konto 11 (Supervisor)
- **Wo:** `/vater/vocab` (vater)

Die Tags sollten gesehen werden.

**Antwort** (claude-code, 2026-07-27 22:08 UTC):

Gebaut. Die Store-Tabelle hat jetzt eine Spalte "Tags": die Namen der globalen Tags stehen als Chips in der Zeile (Komponente TagsCell in VaterVocab.tsx), nicht mehr nur die Anzahl am Aufklapp-Knopf. Daten kosteten nichts - v.tags war mit der Liste schon geladen. Geaendert wird weiterhin im Tag-Editor hinter dem Knopf (dort liegen auch die kind-skopierten Tags, die je Zeile einzeln nachgeladen werden muessten). Detail: whiteSpace nowrap je Chip, sonst bricht ein zweiteiliger Name wie "Englisch 101-1000" mitten im Wort um. Die zusaetzliche Spalte hat "Loeschen" abgeschnitten - deshalb gleich mit erledigt, siehe Anmerkung 4.

**Verlauf** (1):

> **claude-code** · 2026-07-27 22:52 UTC, Konto 11
> 
> Gebaut am 2026-07-28: Tag-Spalte in der Store-Zeile (TagsCell in VaterVocab.tsx), Namen als Chips mit nowrap. Die neue Spalte schnitt "Loeschen" ab - deshalb dort gleich flexWrap + minWidth, siehe #4.

## #6 · Bug · erledigt

- **Erfasst:** 2026-07-27 21:21 UTC von Konto 11 (Supervisor)
- **Wo:** `/vater/vocab` (vater)

Der "Grundform-Key" soll generiert werden nicht eingegeben.

**Antwort** (claude-code, 2026-07-27 22:08 UTC):

Gebaut - mit der Klarstellung, dass zwei Keys im Spiel sind. Der EIGENE Key der Vokabel wird schon generiert (VocabKey.Generate, Data/VocabKey.cs:21; VocabularyStoreService.cs:23, VocabularyStoreController.cs:236) und ist kein Eingabefeld. Das Feld "Grundform-Key" war ein Verweis auf eine ANDERE Vokabel (BaseFormKey, z. B. went -> go); generieren geht dort nicht, der Server verlangt einen existierenden Key und lehnt den Selbstverweis ab (VocabularyStoreController.cs:229-231, :312-315). Berechtigt war der Kern: man musste einen Fremdschluessel abtippen. Ersetzt durch BaseFormPicker (VaterVocab.tsx): man tippt das WORT, sucht im Store (Sprachpaar der Vokabel, eigene Id herausgefiltert) und waehlt den Treffer - gespeichert wird der Key. Gesetzte Grundform steht als Key mit Loesen-Knopf. Verifiziert an goes: 7 Treffer fuer go (goes selbst ausgeschlossen), Auswahl setzt en_go_de_gehen, Speichern durchlaeuft ohne Fehler.

## #7 · Ui · erledigt

- **Erfasst:** 2026-07-27 21:22 UTC von Konto 11 (Supervisor)
- **Wo:** `/vater/vocab` (vater)

DAs Widget soll ein{x] haben wenn man doch nichts Anmerken will (Abbruch)

**Antwort** (claude-code, 2026-07-27 21:37 UTC):

Gibt es schon: der Abbruch-Knopf sitzt im Widget-Kopf als "?" mit aria-label=Schliessen (RemarkWidget.tsx:191-192). Zusaetzlich schliesst Escape (:159) und Alt+A togglet (:76-79). Vermutlich uebersehen, weil er als "btn ghost small" direkt neben "Meine" steht. Zu bauen ist nichts; wenn er zu unauffaellig bleibt, waere die Aufgabe reines Styling.

