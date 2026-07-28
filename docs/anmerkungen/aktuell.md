# Anmerkungen – Export

Stand: 2026-07-28 14:05 UTC · 11 Einträge · Filter: alle

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

## #10 · Ui · erledigt

- **Erfasst:** 2026-07-28 11:56 UTC von Konto 1 (Supervisor)
- **Wo:** `/vater/exercises` (vater)

Die Button in Übersicht sind nicht schön angeordnet. Es ist auch keine "Übersicht"sondern mehr eine Navigation

**Letzte Fehler:**

```json
[{"kind":"http","method":"GET","path":"/api/v1/creator/exercises/24/preview","status":400,"code":"no_checkable_content","at":"2026-07-28T11:16:12.500Z"},{"kind":"http","method":"GET","path":"/api/v1/creator/exercises/24/preview","status":400,"code":"no_checkable_content","at":"2026-07-28T11:16:12.509Z"},{"kind":"http","method":"GET","path":"/api/v1/creator/exercises/25/preview","status":400,"code":"no_checkable_content","at":"2026-07-28T11:16:23.057Z"},{"kind":"http","method":"GET","path":"/api/v1/creator/exercises/25/preview","status":400,"code":"no_checkable_content","at":"2026-07-28T11:16:23.070Z"}]
```

**Antwort** (claude-code, 2026-07-28 12:27 UTC):

KORRIGIERT (die erste Fassung bezog sich auf /vater; gemeint war die Kopfzeilen-Navigation). Beobachtung trifft zu: VaterApp.tsx:32-47 rendert 13 NavLinks als EINE flache Reihe - display:flex, gap:6px, flex-wrap:wrap (index.css:204); jeder Link ist pill-formig (padding 8px/12px, border-radius 10px, :205), der aktive traegt Karten-Hintergrund (:206) - darum lesen sie sich als Knoepfe. Konkret unaufgeraeumt ist: (a) KEINE GRUPPIERUNG - Inhalte (Uebungen, Lehrwerke, Fachlehrer, Vokabeln, Bilder), Steuerung (Neuer Plan, Klassenarbeiten) und Oekonomie (Belohnungen, Shop, Kontostand) stehen gleichrangig nebeneinander; (b) 'Neuer Plan' (:44) ist eine AKTION zwischen Orten und doppelt vorhanden (auch VaterDashboard.tsx:113); (c) uneinheitliche Beschriftung - 10 Eintraege mit Emoji, 3 ohne ('Uebersicht' :33, 'Vokabeln' :38, 'Neuer Plan' :44), und 'Klassenarbeiten' (:43) wie 'Anmerkungen' (:46) tragen dasselbe Symbol; (d) bei dieser Breite bricht die Reihe um, und weil sie im selben flex-Container wie Marke, Profil-Link und Abmelden liegt (:30-51, index.css:202), verschiebt der Umbruch den rechten Block mit. Zum zweiten Satz: 'Uebersicht' fuehrt auf /vater (VaterDashboard.tsx); dort sind Kinder und Plaene ueberwiegend Links in andere Bereiche (:78, :84, :121), echte Uebersichtsdaten traegt nur der Abschnitt 'Heute' (:43-66) - als Landeplatz ist es also tatsaechlich mehr Verteiler als Uebersicht. NEBENBEFUND aus dem Fehler-Mitschnitt dieser Anmerkung (nicht Teil des Textes): 4x 400 no_checkable_content auf creator/exercises/24|25/preview. Beide sind Vokabel-Uebungen 'Einfach Vokabeln' (Autor Vater 6) mit NULL Items (GET .../vocabulary/24/items -> []). Die Fehlerantwort ist korrekt (ExercisePreviewController.cs:42); die Ursache liegt davor: VocabularyExercisesController.ValidateConfigAsync (ExerciseControllers.cs:39-61) prueft die Inhalte von refs/items, verlangt aber NICHT mindestens eines - leere Vokabel-Uebungen sind per API anlegbar. Das UI blockt das (exerciseConfig.tsx:560), die API nicht.

**Verlauf** (3):

> **claude-code** · 2026-07-28 12:27 UTC, Konto 1
> 
> Analyse zu 10 korrigiert: erste Fassung bezog sich auf die Seite /vater, gemeint war die Kopfzeilen-Navigation (VaterApp.tsx:32-47). Nachgezogen: fehlende Gruppierung, 'Neuer Plan' als Aktion zwischen Orten, uneinheitliche Emoji-Beschriftung, Umbruch im geteilten Header-Flex.

> **claude-code** · 2026-07-28 12:30 UTC, Konto 1
> 
> Eingeplant in docs/vater-informationsarchitektur-plan.md (10+11+12 sind ein Thema: das Vater-Web hat keine Informationsarchitektur). Etappe 1: Nav in Gruppen Inhalte/Steuern/Belohnen (entlang der drei Ebenen), 'Uebersicht' -> 'Kinder & Plaene', 'Neuer Plan' raus (Aktion, doppelt), Beschriftung einheitlich, nav aus dem geteilten Header-Flex. Keine Dropdowns - flache Reihe mit role=group.

> **claude-code** · 2026-07-28 13:12 UTC, Konto 1
> 
> Gebaut (Etappe 1 des Plans docs/vater-informationsarchitektur-plan.md): Nav in Gruppen Inhalte / Steuern / Belohnen / Entwicklung (neue Komponente NavGroup mit role=group + aria-label, sichtbare Beschriftung aria-hidden, sonst doppelte Ansage); Kopfzeile und Navigation in getrennten Zeilen, damit der Umbruch nicht mehr den rechten Block schiebt; 'Uebersicht' -> 'Kinder & Plaene' (mit Haus-Symbol); 'Neuer Plan' aus der Nav entfernt (Aktion, steht am Abschnitt Lehrplaene); alle Eintraege mit Symbol, Anmerkungen auf ein eigenes Zeichen (vorher zweimal dasselbe wie Klassenarbeiten). Sieben Textstellen mit dem alten Seitennamen mitgezogen (VaterKind, VaterPlanDetail, VaterWizard, VaterClassTests, VaterKonto, VaterRewards, VaterShop). E2E full-flow.spec.ts:39 und bilder.spec.ts:119 auf den Dashboard-Knopf umgestellt. Geprueft: npm run build + npm test gruen, im Dev-Server nachgesehen (12 Nav-Links, Umbruch faellt zwischen die Gruppen, Gruppennamen nicht als eigene Vorlesestelle im Accessibility-Baum). NICHT geprueft: Playwright - Port 5200 war von der echten Instanz belegt, die Suite verlangt eine eigene Wegwerf-DB.

## #11 · Ui · erledigt

- **Erfasst:** 2026-07-28 12:00 UTC von Konto 1 (Supervisor)
- **Wo:** `/vater/exercises` (vater)

"Neues Fach" und "Neues Kapitel" sind prominent angeordnet. Diese FEatures werden aber wahrscheinlich sehr selten genutzt.
Mir sind das zu viele Features (Anlegen, bearbeiten, zuweisen,..) auf dieser Seite. Das ist mir zu unaufgeräumt. Das erstellen ist für mich ein abgeschlossener Bereich und das Bearbeiten und Zuweisen ebenfalls.

**Antwort** (claude-code, 2026-07-28 12:11 UTC):

Bestaetigt, mit einer Einschraenkung. /vater/exercises traegt vier Anliegen in einer Route: (1) 'Katalog verwalten' (VaterExercises.tsx:195), (2) Lueckentext-Store (:200), (3) das Anlege-Formular mit den Karten 'Fach & Kapitel' (:206), 'Typ & Metadaten' (:241) und 'Inhalt' (:292), (4) die Liste der vorhandenen Uebungen mit Ausprobieren/Bearbeiten/Verwendung/Loeschen (:316-346, ExerciseManageRow ab :502). 'Neues Fach'/'Neues Kapitel' sind zwei von vier Feldern der ERSTEN Karte (:216-236) - im gleichen form-grid und mit gleichem Gewicht wie die zwei Auswahl-Pulldowns, die man jedes Mal braucht. EINSCHRAENKUNG: 'Zuweisen' ist nicht auf dieser Seite. Das passiert im Plan (VaterPlanDetail.tsx / PlanPositions.tsx) bzw. im Assistenten (VaterWizard.tsx); von hier gibt es nur den Lese-Blick 'Verwendung' (VaterExercises.tsx:546, Ausgabe :566-571). Die gewuenschte Trennung ist also 'Erstellen' vs. 'Bearbeiten' - und die beiden stecken heute sogar in EINEM <form> (:202), weil die Bestandsliste innerhalb des Anlege-Formulars gerendert wird (:316).

**Verlauf** (2):

> **claude-code** · 2026-07-28 12:30 UTC, Konto 1
> 
> Eingeplant in docs/vater-informationsarchitektur-plan.md (10+11+12 sind ein Thema: das Vater-Web hat keine Informationsarchitektur). Etappe 3: /vater/exercises wird die Verwaltung, /vater/exercises/neu das Anlegen; 'Neues Fach'/'Neues Kapitel' verlieren die Buehne und verweisen auf den Katalog. Noch nicht angefasst.

> **claude-code** · 2026-07-28 13:47 UTC, Konto 1
> 
> Gebaut (Etappe 3 des Plans docs/vater-informationsarchitektur-plan.md): Anlegen und Verwalten sind getrennt. /vater/exercises verwaltet (Filter, Liste, Ausprobieren/Bearbeiten/Verwendung/Loeschen, Sortierung, Paging), /vater/exercises/neu legt an (VaterExerciseCreate.tsx mit VocabRefPicker). Die 575-Zeilen-Datei mit vier Anliegen in EINEM form ist damit aufgeloest. Die Anlege-Route traegt bewusst keinen Nav-Eintrag - sie ist eine Aktion, erreichbar ueber '+ Neue Uebung' (dieselbe Regel wie bei 'Neuer Plan'). 'Neues Fach' und 'Neues Kapitel' sind aus dem Formular verschwunden; an ihrer Stelle steht ein Satz mit Link auf den Katalog, der das seit Etappe 2 selbst kann. Zwei Dinge kamen beim Schneiden dazu: die Auswahl reist als ?subjectId=&chapterId= in beide Richtungen mit (sonst muesste man Fach und Kapitel nach jedem Wechsel neu einstellen), und Fach/Kapitel sind in der Verwaltung ein FILTER statt Pflicht - die Liste erscheint jetzt, sobald ein Fach gewaehlt ist. Zur Notiz in der Anmerkung: 'Zuweisen' war auf dieser Seite gar nicht vorhanden (das passiert in PlanPositions.tsx bzw. im Assistenten), von hier gab es nur den Lese-Blick 'Verwendung' - die dritte Trennung war schon da. Geprueft: npm run build, npm test und 14/14 Playwright gruen (uebungstypen und vater-von-null fahren den neuen Schnitt vollstaendig durch).

## #12 · Content · erledigt

- **Erfasst:** 2026-07-28 12:03 UTC von Konto 1 (Supervisor)
- **Wo:** `/vater/exercises` (vater)

Was bringt mir "Katalog verwalten"?
Warum ist es in "Übungen" versteckt?

**Antwort** (claude-code, 2026-07-28 12:11 UTC):

Nutzen: 'Katalog verwalten' (CatalogAdmin.tsx) ist die einzige Stelle, an der Fach und Kapitel UMBENANNT und GELOESCHT werden koennen und an der die 'Arten' (Kategorien) gepflegt werden. Anlegen ging schon vorher ueber die '+'-Knoepfe im Formular (VaterExercises.tsx:220 und :234) - korrigieren nicht. Das zaehlt, weil der Katalog GLOBAL ist: Faecher und Kapitel teilen sich alle Vaeter, ein Tippfehler bleibt fuer alle sichtbar (CatalogAdmin.tsx:56-58); darum nennt das Loeschen die Kaskade (:79-96). 'Arten' sind freie Ordnungsbegriffe innerhalb des Fachs und filtern die Uebungssuche beim Planbau (:103-106). Warum unter 'Uebungen': der Katalog IST die Behaelter-Hierarchie der Uebungen (Subject -> Chapter -> Exercise), und der Bereich pflegt genau die zwei Pulldowns dieser Seite - die Begruendung steht als Kommentar in VaterExercises.tsx:191-194. 'Versteckt' trifft aber zu: er liegt eingeklappt hinter einem Ghost-Button (CatalogAdmin.tsx:42-47) und hat keinen eigenen Nav-Eintrag (VaterApp.tsx:32-47).

**Verlauf** (2):

> **claude-code** · 2026-07-28 12:30 UTC, Konto 1
> 
> Eingeplant in docs/vater-informationsarchitektur-plan.md (10+11+12 sind ein Thema: das Vater-Web hat keine Informationsarchitektur). Etappe 2: Katalog bekommt die eigene Route /vater/katalog (samt Nav-Eintrag unter 'Inhalte'), der Einklapper entfaellt; Lueckentexte analog /vater/lueckentexte. Die Frage nach dem Nutzen ist beantwortet, das 'versteckt' wird behoben.

> **claude-code** · 2026-07-28 13:47 UTC, Konto 1
> 
> Gebaut (Etappe 2 des Plans docs/vater-informationsarchitektur-plan.md): Der Katalog hat eine eigene Route /vater/katalog (VaterKatalog.tsx, laedt die Faecher selbst) samt Nav-Eintrag in der Gruppe 'Inhalte'; der Einklapper samt 'Schliessen' ist entfallen - eine eigene Seite muss sich nicht aufklappen. Der Lueckentext-Store analog unter /vater/lueckentexte. Auf der Uebungen-Seite stehen zwei Links dorthin, damit man sie bei fehlendem Kapitel nicht in der Navigation suchen muss. Ueber den Plan hinaus noetig: Fach und Kapitel lassen sich im Katalog jetzt auch ANLEGEN (ein neues Fach wird gleich ausgewaehlt) - sonst waere es eine Seite, auf der man nur umbenennen und loeschen kann, und Etappe 3 haette die Anlege-Felder nicht wegnehmen koennen. Dabei bekam NewName ein fieldId von aussen: mit drei Instanzen war die feste DOM-id dreifach vergeben und jedes label zeigte auf dasselbe Feld; die drei 'Anlegen'-Knoepfe tragen jetzt unterscheidbare zugaengliche Namen. Ein Fund aus dem Test hat die Umsetzung geaendert: der erste Wurf tauschte CatalogAdmin bei jeder Aenderung gegen 'Lade...', weil onCatalogChanged ein reload ausloest und das loading erneut auf true setzt (useAsync.ts:27) - die Fach-Auswahl sprang zurueck und die Erfolgsmeldung erschien nie. Geprueft: npm run build, npm test und 14/14 Playwright gruen.

## #13 · Bug · erledigt

- **Erfasst:** 2026-07-28 13:04 UTC von Konto 1 (Supervisor)
- **Wo:** _(keine Route)_ (?)
- **Folgt aus:** #10

Leere Vokabel-Uebung ist per API anlegbar - Preview/Test scheitern erst spaeter mit no_checkable_content. VocabularyExercisesController.ValidateConfigAsync (ExerciseControllers.cs:39-61) prueft die Inhalte von refs/items, verlangt aber nicht mindestens eines. Belegt an den Uebungen 24 und 25 ('Einfach Vokabeln', Autor Vater 6, GET .../vocabulary/24/items -> []): GET creator/exercises/24/preview antwortet 400 no_checkable_content (ExercisePreviewController.cs:42 - korrekt, aber zu spaet). Das UI blockt den Fall (exerciseConfig.tsx:560), die API nicht; ueber Agent/REST/Edit entstehen so Uebungen, die ein Kind nicht spielen kann. Vorschlag: in ValidateConfigAsync mindestens ein ref/item verlangen (validation_error) und pruefen, ob das Leeren per PUT/Item-DELETE denselben Riegel braucht.

**Antwort** (claude-code, 2026-07-28 14:05 UTC):

KORRIGIERT. Der Befund stimmt (leere Vokabel-Uebungen existieren und scheitern erst spaet), der von mir vorgeschlagene Fix war FALSCH: 'in ValidateConfigAsync mindestens ein ref/item verlangen' laeuft laut ExerciseControllerBase.cs:230 und :302 auf POST UND PUT - und beim PUT traegt die Config berechtigterweise keine Items, weil die Items seit dem Item-Umbau in der Tabelle liegen (leerer Payload = reiner Einstellungs-PUT). Schwerer noch: 'erst leer anlegen, dann fuellen' ist ein GEWOLLTER, getesteter Ablauf - VocabExerciseAuthoringTests.cs:100-106 legt die Uebung mit refs=[] an und fuellt sie danach per refs-from-tags. Eine Schranke am POST haette das zerstoert. Zu den beiden Nachfragen: (1) PUT kann die Item-Menge gar nicht leeren - AfterSaveAsync synchronisiert nur, wenn der Payload Items/Refs traegt (ExerciseControllers.cs:76-88); ein Riegel ist dort unnoetig. (2) Item-DELETE kann leeren, aber nur bei einer Uebung, die in keinem Lehrplan steckt (in-Plan ist ueber ExerciseInPlanAsync schon blockiert, ExerciseControllers.cs:240) - und das Leeren ist dort ein legitimer Zwischenschritt beim Bearbeiten (das einzige Wort austauschen). Ein Riegel kostete dort mehr als er brachte. (3) Die eigentliche Luecke lag woanders: refs-from-tags hat bei Tags ohne Treffer auf NULL Items reconciled und die Uebung damit LAUTLOS geleert - ein Tippfehler im Tag sah wie ein Erfolg aus. Der Schaden entsteht nicht beim Anlegen, sondern beim ZUWEISEN: dort bekommt ein Kind eine Pflicht, die es nicht spielen kann. Dort sitzt der Riegel jetzt.

**Verlauf** (1):

> **claude-code** · 2026-07-28 14:05 UTC, Konto 1
> 
> Gebaut. Drei Aenderungen, alle Backend: (1) Neuer Fehlercode exercise_empty (ApiErrors.cs) - bewusst getrennt von no_checkable_content, weil dort 'nichts zu pruefen' eine Eigenschaft des Typs ist (Aufsatz) und hier ein unfertiger Datenstand. (2) PlanPositionsController lehnt das Zuweisen einer ungefuellten Uebung ab (400 exercise_empty). 'Ungefuellt' ist registry-getrieben definiert: nur Typen mit StoreResolution.ItemTable und null Items - ein Aufsatz hat typbedingt nie Items und bleibt zuweisbar (eigener Regressionstest). (3) refs-from-tags weist einen leeren Treffersatz ab und laesst die Items unberuehrt, statt die Uebung lautlos zu leeren. Dazu: die Vorschau nennt jetzt den Grund (exercise_empty statt des allgemeinen no_checkable_content), damit ein leeres 'Ausprobieren' nicht wie ein Fehler der App aussieht - genau der Fall aus dem Mitschnitt von Anmerkung 10. NICHT gebaut, mit Begruendung in der korrigierten Antwort: keine Schranke am POST (zerstoerte 'erst anlegen, dann fuellen') und keine am Item-DELETE. Geprueft: 464/464 Tests gruen (6 neue in EmptyExerciseGuardTests.cs, darunter der Regressionsschutz fuer Aufsatz und fuer 'erst anlegen, dann fuellen'), dotnet format sauber, und docs/api-examples traegt ein verifiziertes Beispiel (Abdeckung 31/48 Codes).

