---
tags: [typ/spezifikation, bereich/frontend, rolle/student]
---

# Funktionsbeschreibung: Sohn-App (Kind, Handy)

← [Zurück zum Wiki-Index](../README.md) · Verwandt: [Student-Tutorial](tutorial-student.md) (API), [05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md)

**Zweck dieses Dokuments:** Vollständige, **design- und technikunabhängige** Beschreibung aller
Funktionen der Sohn-App. Es beschreibt *was* die App kann (Screens, angezeigte Daten, Aktionen,
Zustände, Übergänge) — **nicht** *wie* sie aussieht. Aus dieser Beschreibung kann ohne Kenntnis von
React, der REST-API oder eines Farbschemas eine komplette Oberfläche entworfen werden.

Zielgerät: **Smartphone im Hochformat**, ein Kind (~9–13 Jahre) als alleiniger Nutzer, oft ohne
Aufsicht. Bedienung muss ohne Lesen langer Texte, mit großen Tap-Zielen und sofortiger Rückmeldung
funktionieren.

---

## 1. Wer benutzt die App und wozu

Der **Sohn** (das Kind) lernt. Er legt **keine** Inhalte an und steuert weder *was* er lernt noch *wie oft*
oder *ab welcher Schwelle* — das macht der Vater in einer getrennten App. Der Sohn:

1. sieht seine **Tagesmission** (was heute zu tun ist),
2. **übt** und macht **Tests**,
3. verdient dabei zwei Währungen (**Münzen** und **Gems**),
4. gibt sie im **Familien-Shop** (echte Belohnungen) und für **Charaktere/Skins** aus,
5. verfolgt seinen **Fortschritt**, seine **großen Ziele** (vom Vater gesetzt), **Missionen** und **Auszeichnungen**,
6. **beschreibt sich selbst** — was er mag und was gar nicht (siehe 4.10).

**Die eine Ausnahme von „steuert nichts"** ist Punkt 6, und sie ist genau deshalb gefahrlos: Interessen
**kleiden den Stoff ein, sie ersetzen ihn nie.** Sie ändern nur, welche Bilder und Beispiele erscheinen —
nicht den Inhalt, nicht die Pflicht, nicht die Punkte. Damit ist es die einzige Stelle, an der das Kind
sich ohne Anti-Cheat-Risiko einbringen kann.

Die App ist bewusst **spielerisch** (Arcade/Duolingo-Anmutung): Streaks, Combos, Konfetti, Sound,
Vibration, Maskottchen. Motivation ist ein Kernfeature, kein Beiwerk.

---

## 2. Grundkonzepte (gelten überall)

Diese Begriffe tauchen auf mehreren Screens auf und müssen konsistent dargestellt werden.

### 2.1 Die zwei Währungen

- **🪙 Münzen (Coins):** verdient durch **Lernleistung** (richtige Antworten, erfüllte Ziele).
  Werden im **Familien-Shop** für echte Belohnungen vom Vater ausgegeben.
- **💎 Gems:** verdient durch **Boni** (Combos, Tempo, Missionen, Auszeichnungen). Werden ausgegeben
  für **kosmetische Charaktere/Skins** und optional als **Gem-Anteil** eines Shop-Artikels.

Beide Salden sind **immer sichtbar** (siehe HUD) und ändern sich sofort nach jeder relevanten Aktion.

### 2.2 Streak 🔥

Anzahl der aufeinanderfolgenden Tage, an denen die Tagesmission erfüllt wurde. Motivations-Anker,
prominent im HUD.

### 2.3 Charakter / Skin

Ein wählbarer Avatar (Maskottchen). Der aktuell ausgerüstete Skin erscheint überall im HUD und als
großes Maskottchen auf Schlüsselscreens. Skins sind rein kosmetisch.

### 2.4 Plan / Position / Übung

- Ein **Plan** (Lehrplan) ist der Container für das, was das Kind gerade lernen soll. Es kann mehrere
  geben, aber **genau einer ist gerade spielbar** (aktiv + heute laufend); die App wählt ihn vor.
- Ein Plan besteht aus mehreren **Positionen**. Jede Position ist eine konkrete **Übung** (z. B. eine
  Vokabelübung, ein Lückentext, eine Rechenaufgabe).
- Jede Position hat einen **Rhythmus**: *Tagesziel* (jeden Tag Pflicht), *Wochenziel* (x-mal pro
  Woche Pflicht) oder *frei* (darf, muss aber nicht).

### 2.5 Server ist die Wahrheit (wichtig fürs UI-Verständnis)

Die App **rechnet nichts selbst aus** und darf nicht so tun, als ob. Ob eine Antwort richtig ist, wie
viele Punkte es gibt, was etwas kostet, wann eine Karte wieder fällig ist, ob ein Ziel erfüllt ist —
das entscheidet immer der Server. Die Oberfläche **zeigt Ergebnisse an** und **schickt Eingaben ab**.
Folge fürs Design: Nach jeder Aktion kommt eine Server-Antwort, die den Bildschirm aktualisiert; es
gibt kurze **Warte-/Ladezustände**, und Salden „springen" auf den echten Wert.

### 2.6 Zwei Lernmodi einer Übung

Jede Übung kann auf zwei Wegen gespielt werden:

- **Üben** (Leitner-Karteikasten): wiederholtes Training. Nur *fällige* Karten zählen für Punkte,
  jede Karte maximal einmal pro Tag wertbar. Falsches senkt, Richtiges hebt die „Box" einer Karte.
- **Test** (Abschlusstest / „Tagestest"): strenge Prüfung. Fragen kommen **einzeln**, **kein Zurück**,
  **kein Zwischenfeedback**. Ergebnis (Prozent, bestanden ja/nein) erst am Ende.

### 2.7 Übungstypen (Katalog) und ihr Reifegrad — WICHTIG fürs Scoping

Der Katalog kennt **zwölf** Übungstypen. Gespielt wird jeder Typ über **denselben generischen
Positions-Loop** (Üben + ggf. Test): Der Server liefert die Karten samt Antwortform, die App zeigt nur an,
was die Karte mitbringt (Satz mit Lücke, Dekodierung, Passage, Audio, Auswahl …). Ob es zur Position einen
**Test** gibt, sagt der Server je Tag (`testable`) — `false` für reine Inhaltsübungen ohne Prüfmodus und
für freie Anzeige-Stufen. **Am weitesten entwickelt ist die Vokabelübung** (Store, Audio,
Abfragerichtung, alle Antwortmodi, Leitner) — sie bleibt der Design-Schwerpunkt.

Die App muss die Typen nicht fest verdrahten: Der Server liefert ein **Typ-Manifest**
(`GET api/v1/creator/exercise-types`, für jeden angemeldeten Nutzer lesbar) mit Anzeigename,
Renderer-Id, Prüfmodus und Fähigkeiten je Typ. Renderer-Ids heute: `flashcards`, `cloze`, `matching`,
`arithmetic`, `list`, `birkenbihl`, `prompts`, `reading`, `listening`, `essay`.

| Typ (Label) | Was das Kind tut | Prüfmodus | Stand in der Sohn-App |
|---|---|---|---|
| **Vokabelkarten** | Karteikasten: Wort ↔ Übersetzung über mehrere Stufen (zeigen → selbst einschätzen → Buchstaben → tippen → hören → Auswahl) | Positions-Test | ✅ **Voll entwickelt** — Store-verknüpft, Audio, Abfragerichtung, alle Antwortmodi, Leitner. **Design-Schwerpunkt.** |
| **Lückentext** | fehlendes Wort in einen Satz einsetzen (tippen oder Wortbank), optional store-verknüpft | Positions-Test | ✅ spielbar (Üben + Test), Satz mit markierter Lücke |
| **Zuordnung** | Paare einander zuordnen (links ↔ rechts), optional mit Ablenkern | Positions-Test | ✅ spielbar (Üben + Test) |
| **Rechenaufgaben** | Rechenaufgaben lösen | Katalog-Check (`CatalogCheck`) | ✅ spielbar und **testbar** im Positions-Loop |
| **Liste** | Elemente einer Liste nennen | Katalog-Check (`CatalogCheck`) | ✅ spielbar und **testbar** im Positions-Loop |
| **Rechen-Drill** | fortlaufend generierte Rechenaufgaben | Katalog (erzeugen + prüfen) | ⛔ liefert keine Karten in den Positions-Loop — nur Katalog-Direktcheck |
| **Leseverständnis** | Text lesen, Fragen beantworten | keiner | 🟡 Durchspielen im generischen Loop (Passage + Fragen), kein Test |
| **Hörverständnis** | Audio hören, Fragen beantworten | keiner | 🟡 Durchspielen im generischen Loop (Audio + Fragen), kein Test |
| **Regelaufgaben** | Grammatik-Aufgaben mit Regelhinweisen | keiner | 🟡 Durchspielen im generischen Loop, kein Test |
| **Übersetzung** | ganze Sätze übersetzen | keiner | 🟡 Durchspielen im generischen Loop, kein Test |
| **Aufsatz** | freien Text schreiben | keiner (manuell) | 🟡 nur Anzeige der Aufgabe, keine Auswertung |
| **Birkenbihl** | Wort-für-Wort-Dekodierung lesen | keiner | 🟡 Durchspielen: Satz mit Dekodierung auf der Kartenvorderseite, kein Test |

**Folge fürs UI:** Ein Kartenbildschirm bedient alle Typen; er rendert die Felder, die eine Karte trägt,
statt nach Typ zu verzweigen. Den **Test-Knopf** zeigt die App nur, wenn der Server `testable` meldet —
sonst heißt die Aktion „Durchspielen".

### 2.8 Antwort-/Stufen-Modi einer Karte (datengetrieben, ein UI bedient alle)

Innerhalb einer spielbaren Übung (heute v. a. der Vokabelübung) kommt jede einzelne Karte bzw.
Testfrage vom Server mit Feldern, aus denen sich der **Antwortmodus** ergibt — meist eine **Leitner-
Stufe** desselben Inhalts, die schrittweise mehr fordert. Das UI muss **dieselben** Interaktionsmuster
für alle diese Modi bereitstellen:

| Kartentyp | Woran erkennbar | Was das Kind tut |
|---|---|---|
| **Aufdecken / Selbsteinschätzung** | Lösung wird mitgeliefert (`reveal`) | Aufgabe lesen → **Aufdecken** → selbst bewerten: „Gewusst" / „Nicht gewusst" |
| **Tippen (Freitext)** | keine Lösung, Antwortlänge unbekannt | Antwort in ein Textfeld eingeben → **Prüfen** |
| **Buchstabenkästchen** | keine Lösung, Antwortlänge bekannt | pro Buchstabe ein Kästchen ausfüllen → **Prüfen** |
| **Buchstaben ordnen** | gemischte Buchstaben mitgeliefert (`letterTiles`) | Kacheln in die richtige Reihenfolge tippen oder klicken → **Prüfen**; ein mehrteiliges oder langes Wort kommt als Buchstabenkästchen |
| **Auswahl (Multiple Choice)** | Auswahlmöglichkeiten mitgeliefert (`choices`) | eine Option antippen (nur **markiert**, änderbar) → **Prüfen** bzw. im Test **Weiter** |
| **Hören** | Audio mitgeliefert (`audioUrl`) | Audio abspielen (statt/zusätzlich zum Text), dann antworten wie oben |

Jede Karte kann zusätzlich einen **optionalen Tipp/Hinweis** (💡) tragen, der erst auf Wunsch
aufgedeckt wird.

**Ein Aufbau für Üben und Test** ([B-526](backlog/B-526-lektionsbildschirm-festes-raster.md)): Kopf mit ✕,
Fortschrittsbalken und Zähler · **Arbeitsanweisung** als Überschrift · die Frage · die Antwortfläche · **ein**
Hauptknopf am unteren Rand. Die Arbeitsanweisung sagt, was zu tun ist („Wähle die richtige Antwort", „Ordne
die Buchstaben", „Schreibe das passende Wort", „Weißt du es?", „Neu – präg es dir ein" …); die App leitet sie
aus den Feldern der Karte ab, nicht aus einer Stufennummer. Ein Wort oder Satz steht in einer **Sprechblase**
am Maskottchen; sobald Lesetext, Bild, Wortkasten oder Dekodierung dabei sind, als volle Karte ohne
Maskottchen.

### 2.9 Feedback- und Motivationssystem (durchgängig)

- **Feiern in drei Stufen** (klein / mittel / groß): Konfetti/Banner, Emoji, optional ein „Fighter"-
  Emoji, jeweils mit **Ton** und **Vibration**.
- **Toasts**: kurze Einblendungen für Punktgewinn, Lösung, Kauf-Bestätigung.
- **Combos**: aufeinanderfolgende richtige Antworten; ab einer Schwelle eigene Feier.
- **Stimmungs-Maskottchen**: schläfrig / fröhlich / begeistert je nach Fortschritt.
- **Barrierefreiheit-Schalter**: „Bewegung reduzieren" (dämpft Animationen) und **Ton an/aus** werden
  respektiert und dauerhaft gemerkt.

---

## 3. Persistenter Rahmen (auf allen eingeloggten Screens)

### 3.1 Kopf-HUD (immer sichtbar)

Zeigt dauerhaft:

- **Mini-Avatar** des ausgerüsteten Charakters
- **🪙 Münzen**-Zähler
- **💎 Gems**-Zähler
- **🔥 Streak**-Zähler
- **Ton-an/aus-Schalter**

### 3.2 Untere Navigation (5 feste Bereiche)

- **🗺️ Pfad** — Tagesmission als Lernpfad (Startseite)
- **🏆 Erfolge** — 7-Tage-Übersicht, Missionen, große Ziele, Auszeichnungen
- **💰 Konto** — Münz-/Gem-Stand und Buchungsverlauf (reine Übersicht, kein Kauf)
- **🛒 Shop** — Familien-Shop (Artikel kaufen, Inventar, Aktivierung) — der einzige Münz-Ausgabeweg
- **🎭 Skins** — Charaktere

Die Screens **Üben** und **Test** haben **keinen** eigenen Navigationseintrag — sie werden vom
Pfad über einen Knoten betreten und über „Zum Pfad" wieder verlassen.

> Hinweis fürs Design: Münzen werden **ausschließlich im Shop** ausgegeben (Familien-Warenkorb mit
> Bestand, Inventar und einem Genehmigungs-Schritt beim Einlösen). Das **Konto** ist nur noch die
> Übersicht über Stände und Buchungsverlauf, kein eigener Kaufweg mehr.

### 3.3 Zentrale Feier-Schicht

Ein einziges, bildschirmfüllendes Overlay für alle Feiern (siehe 2.9), unabhängig vom Screen darunter.

---

## 4. Screens im Detail

Jeder Screen ist beschrieben durch: **Zweck**, **angezeigte Daten**, **Aktionen**, **Zustände** und
**Übergänge**.

### 4.1 Login

**Zweck:** Das Kind meldet sich kindgerecht an (Vollbild, keine untere Navigation).

**Angezeigte Daten:**

- Marken-Element „Pugling" und eine motivierende Überschrift („Bereit für die Runde?").
- Ein großes, animiertes Maskottchen.
- Eine kleine Auswahl von Helden-Avataren als Vorschau.

**Aktionen:**

- **Helden-Avatar wählen** — rein kosmetische Vorschau, ändert das große Maskottchen.
- **Helden-Nummer eingeben** — eine numerische Kennung (die Kind-ID). Die zuletzt genutzte Nummer wird
  gemerkt und vorausgefüllt.
- **PIN eingeben** — über ein **eigenes Ziffernpad** (0–9, Löschen, Bestätigen); die PIN wird als
  Punkte-Reihe visualisiert (max. 6 Stellen), nie im Klartext.
- **Los / Anmelden**.

**Zustände & Übergänge:**

- Erfolg → **Pfad / Tagesmission**.
- Fehler → freundliche Meldung („Nummer oder PIN falsch"), PIN wird geleert, kein Wechsel.
- Warten → kurzer Ladezustand am Anmelde-Knopf.

### 4.2 Pfad / Tagesmission (Startseite)

**Zweck:** Zeigt, **was heute zu tun ist**, als Weg mit einem markierten nächsten Schritt, und ist Absprung
zu Üben/Test. Wichtigster Screen. Entschieden in
[B-525](backlog/B-525-sohn-startseite-als-schlangenpfad.md).

**Angezeigte Daten:**

- **Missionskopf:** Stimmungs-Maskottchen, „Tagesmission", Titel des aktiven Plans (einmal, ganz oben).
- **Ziel-Status:** entweder „✓ Heute geschafft!" oder ein Fortschritt „🎯 Ziele heute: x / y".
- **Plan-Auswahl:** wenn mehrere Pläne existieren, ein Umschalter (der spielbare ist vorgewählt).
- **Der Pfad:** die Positionen des Plans als **Knoten in Schlangenlinie**, in der Reihenfolge des Plans.
  - **Abschnitts-Banner** mit dem Titel der Lehrwerk-Unit. Aufeinanderfolgende Positionen derselben Unit
    bilden einen Abschnitt; wechselt die Unit, beginnt ein neues Banner.
  - Je Knoten: Typ-Icon, darunter der Übungstitel.
  - **Ring** um den Knoten: Anteil der schon kennengelernten Inhalte. Eine Übung ohne Inhalte (Aufsatz) hat
    keinen Ring. Ein voller Ring heißt „alles gesehen", nicht „fertig".
  - **Häkchen**, wenn das Ziel der laufenden Periode (Tag oder Woche) erfüllt ist. Eine freie Position
    trägt nie eines.
  - **Frist und Malus** an jedem Pflicht-Knoten mit offenem Ziel: bei einem Tagesziel die Stunden bis zum
    Tagesende des Servers („noch 6 Std."), bei einem Wochenziel „bis Sonntag" und am Sonntag die Stunden;
    dahinter, falls gesetzt, der Münz-Malus („sonst −5 Münzen").
  - **Genau ein Knoten ist der nächste** („START", Maskottchen daneben): das erste offene Tagesziel, sonst
    das erste offene Wochenziel. Sind alle Pflichtziele erfüllt, ist keiner hervorgehoben.
- **Kein Knoten ist gesperrt.** Der Pfad zeigt, was dran ist; er erzwingt keine Reihenfolge.

**Aktionen:**

- **Knoten antippen** → ein **Blatt** von unten mit Titel, Rhythmus, Stand (kennengelernt, fällig), Frist
  und Malus.
- **„Los"** (der eine Hauptknopf im Blatt) → Screen *Üben*; an dessen Ende wird der Test angeboten. Ist
  nichts zu üben und heute ein Test möglich, führt „Los" direkt in den *Test*.
- **Der andere Weg** als kleiner zweiter Knopf: „Test", wenn die Position heute testbar ist, bzw. „Üben".
- **Blatt schließen:** ✕, Tipp daneben oder Escape.

**Zustände:**

- **Kein Plan:** schlafendes Maskottchen + Hinweis „Noch keine Mission – dein Vater hat noch keinen
  Lehrplan erstellt" + Abmelden.
- **Alles erledigt:** „✓ Heute geschafft!", kein hervorgehobener Knoten; alle Knoten bleiben spielbar.
- **Laden / Fehler:** neutraler Warte- bzw. Fehlerhinweis mit Wiederholmöglichkeit.

### 4.3 Üben (Leitner-Runde)

**Zweck:** Karteikasten-Training einer Position. Wiederholen, Punkte sammeln, Boxen hochspielen.

**Ablauf/Datenfluss (fürs UI relevant):**

1. Beim Betreten startet eine **Sitzung**; die Kartenreihenfolge steht dann fest.
2. Die App zeigt Karte für Karte (Reihenfolge vom Server; alternativ ein Kartenstapel).
3. Während des Übens läuft im Hintergrund ein **Zeit-Zähler** (aktive Lernsekunden), der periodisch
   an den Server gemeldet wird. Nur *aktive* Zeit zählt, gedeckelt.
4. Beim Verlassen wird die Sitzung **beendet** (dabei prüft der Server Ziele/Missionen erneut).

**Angezeigte Daten pro Karte:**

- Kopf: ✕ (beendet die Runde), Fortschrittsbalken, Zähler „Karte i / n", laufende Combo.
- Die Arbeitsanweisung und die Aufgabe (Text oder Audio-Abspielknopf bei Hör-Karten), siehe 2.8.
- Je nach Antwortmodus (siehe 2.8): Umdreh-Fläche, Textfeld, Buchstabenkästchen oder Auswahl-Optionen.
- Optionaler Tipp (💡), erst auf Antippen sichtbar.
- Optionaler **Tempo-Modus**: eine Countdown-Leiste als Ansporn (schnellere Antwort → Bonus).

**Aktionen:**

- **Aufdecken** und danach selbst bewerten (**Nochmal / Gewusst**) — bei Selbsteinschätzungs-Karten.
- **Antwort eingeben** bzw. eine Option **markieren** und dann **Prüfen** — bei getippten und Auswahl-Karten.
- **Tipp aufdecken**.
- **Tempo-Modus an/aus** (wird gemerkt).
- **Runde beenden** über das ✕ im Kopf.

**Feedback nach jeder Antwort:**

- Richtig: Punkt-Toast („+n 🪙 · Box x"), Combo-Zähler, ggf. Combo-/Tempo-Bonus, kleine Feier;
  Anzeige der nächsten Fälligkeit der Karte.
- Falsch: Lösung wird gezeigt („Lösung: …").
- Nicht wertbar (nicht fällig / heute schon gewertet): still protokolliert, keine Punkte — das UI
  darf hier keinen Punktgewinn vortäuschen.

**Zustände & Übergänge:**

- **Fertig:** „Runde fertig!" mit verdienten Münzen + Kartenzahl; Knöpfe „Weiter zum Test" und
  „Zum Pfad".
- **Nichts fällig:** „Nichts fällig 🎉" mit denselben Weiter-Knöpfen.
- **Laden / Fehler**.

### 4.4 Test (Tagestest / Klausur)

**Zweck:** Ernste Prüfung einer Position. Misst, ob das Ziel erreicht ist.

**Regeln (prägen das UI):**

- Fragen kommen **einzeln nacheinander**.
- **Kein Zurück**, **kein Zwischenfeedback** zur Korrektheit.
- Nach der letzten Antwort schließt der Test **automatisch** ab und zeigt die Auswertung.
- Ein Test lässt sich **nur einmal** einreichen; ein Verbindungsabbruch **setzt denselben Versuch
  fort** (kein Neustart).

**Angezeigte Daten pro Frage:**

- Derselbe Aufbau wie beim Üben (siehe 2.8), mit eigener Kopffarbe und dem Wort „Test" vor der
  Arbeitsanweisung; Zähler „Frage i / n". Eine Stufen-Kennzeichnung gibt es nicht mehr.
- Die Aufgabe (Text oder Audio) + optionaler Tipp.
- Eingabe je nach Antwortmodus (siehe 2.8).

**Aktionen:**

- Antworten (tippen / Option markieren / aufdecken + selbst bewerten) und **Weiter**.
- **Später weiter** über das ✕ im Kopf: der Versuch bleibt stehen und wird fortgesetzt.

**Ergebnis-Screen:**

- Titel „Sieg!" / „Fast!", Stimmungs-Maskottchen.
- **Prozent-Ring**: erreichter Score, richtige / gesamt.
- Bestehensgrenze (Standard 80 %, ggf. abweichend).
- **Item-für-Item-Auswertung**: je Frage ✅/❌, Aufgabe, erwartete Lösung.
- Knöpfe: „Nochmal versuchen" (nur wenn nicht bestanden), „Zum Pfad".
- Bestehen → große Feier; Streak und Salden werden aktualisiert.

### 4.5 Erfolge (Fortschritt)

**Zweck:** Alles, was über Tage und Wochen läuft. Die Positionen selbst stehen nicht mehr hier – sie sind
der Pfad (4.2).

**Angezeigte Daten:**

- „Erfolge" + 🏆 **Gesamtpunkte**.
- Plantitel · „x / y Tage geschafft" · Streak 🔥.
- **Missionen-Panel** (siehe 4.9).
- **Letzte Tage:** eine kleine Kalender-Heatmap der letzten ~7 Tage (teilweise geschafft vs. Tag
  komplett) mit Legende.
- **Meine großen Ziele:** die aktiven Lernziele (Objectives), die der Vater gesetzt hat, je mit
  Etappen-Fortschritt (Key Results) und Währung (🪙 verbindlich / 💎 Dehnungsziel); nur lesend, neu
  erreichte Etappen werden einmal gefeiert (`GET api/v1/student/me/objectives`).
- **Auszeichnungen-Galerie** (siehe 4.9).

**Aktionen:**

- „Zum Pfad" → Startseite.

**Zustände:** Fällt auf einen Hinweis zum Pfad zurück, wenn noch keine Mission gewählt ist.

### 4.6 Konto (Übersicht)

**Zweck:** Münz-/Gem-Stand und Buchungsverlauf ansehen. **Kein Kaufweg** — ausgegeben wird im 🛒 Shop
(Münzen) bzw. 🎭 Skins (Gems).

**Angezeigte Daten:**

- „Mein Konto" + 🪙- und 💎-Stand + Erklärtext (Münzen → Shop, Gems → Skins).
- **Verlauf:** Punkt-Buchungen mit Art-Label (Wiederholung, Ziel erreicht, Combo, Schnell-Bonus, Mission,
  Auszeichnung, Ziel-Etappe, Belohnungsbox, Pflicht-Malus, Skin-Kauf, Shop-Kauf …), Grund und +/- Betrag.

**Aktionen:** keine (reine Lese-Übersicht) — zum Ausgeben in den Shop bzw. zu den Skins wechseln.

**Zustände:** Leerzustand, wenn es noch keine Buchungen gibt; Lade-/Fehlerzustand.

**Aktionen (Ausnahme):** ein dauerhafter Einstieg in die Selbstbeschreibung („Das bin ich", siehe 4.10).
Er liegt hier und nicht in der unteren Navigation: die trägt die fünf Orte, an denen gespielt und
ausgegeben wird — dies ist eine Einstellung.

### 4.7 Familien-Shop

**Zweck:** Der **einzige Münz-Ausgabeweg** und ein Kern-Feature der App: Das Kind kauft aus einem vom Vater
gepflegten **Warenkatalog** Artikel, sammelt sie im eigenen **Inventar** und **beantragt** deren
Einlösung. Es gibt **Bestand**, **gemischte Preise (Münzen + Gems)** und einen **Genehmigungs-Schritt**
durch den Vater.

**Fachliche Bausteine (das Kind muss sie unterscheiden können):**

- **Artikel/Angebot** — ein kaufbarer Eintrag im Katalog. Hat Titel, Beschreibung, eine **Art**
  (`ActionType`, z. B. Verbrauch/Aktion) und eine **Einheit** (`UnitType`, z. B. Minuten, Stück),
  einen **Preis** (Münzen und/oder Gems), **wie viele Einheiten pro Kauf** dazukommen, und einen
  **aktuellen Bestand** (kann ausverkauft sein).
- **Inventar** — was das Kind bereits gekauft, aber noch nicht eingelöst hat: je Artikel eine
  **aggregierte Menge**. (Beispiel: „45 Minuten Bildschirmzeit".)
- **Aktivierungsanfrage** — das Kind bittet, eine **Menge** eines Inventar-Postens einzulösen
  („jetzt 30 Minuten"). Die Menge wird **erst bei Genehmigung** durch den Vater abgezogen.

**Angezeigte Daten (drei Bereiche auf einem Screen oder als Unter-Tabs):**

1. **Kaufen (Katalog):** je Angebot Titel + Beschreibung, Art/Einheit als Label, Preis (🪙 und/oder 💎),
   „+n je Kauf", **Bestand** und ein **Kauf-Knopf**. Der Knopf ist deaktiviert bei fehlender Deckung
   („noch n 🪙" / „noch n 💎") oder wenn ausverkauft.
2. **Mein Inventar:** je besessenem Artikel (Menge > 0) Titel, Art/Einheit und **Menge**, plus einen
   **„Einlösen"-Knopf**.
3. **Meine Anfragen:** eigene Aktivierungsanfragen mit Titel, angefragter Menge, Datum und **Status**
   (Offen / Genehmigt / Abgelehnt), neueste zuerst; optional nach Status filterbar.

**Aktionen:**

- **Artikel kaufen** → Bestätigung (Münzen/Gems sind sofort weg) → Server bucht ab und erhöht das
  Inventar um die Einheiten-pro-Kauf → Toast + Inventar aktualisiert.
- **Einlösen beantragen** → Mengen-Auswahl (wie viele Einheiten, ≤ Bestand) → Bestätigung → erzeugt
  eine **Aktivierungsanfrage** im Status „Offen"; Inventar bleibt bis zur Genehmigung unverändert.
- **Anfrage-Status verfolgen** (Genehmigt/Abgelehnt erscheint, sobald der Vater entscheidet; bei
  Genehmigung sinkt das Inventar entsprechend).

**Zustände & Regeln fürs UI:**

- Leerzustände getrennt je Bereich: kein Katalog, leeres Inventar, keine Anfragen.
- Bei zu wenig Bestand/Deckung/ungültiger Menge blockiert der Server — das UI muss den Knopf
  entsprechend deaktivieren und die Server-Meldung freundlich zeigen (nicht selbst „durchwinken").
- Doppelklick-Schutz: eine laufende Kauf-/Anfrage-Aktion sperrt kurz, bis die Server-Antwort da ist.

### 4.8 Skins / Charaktere

**Zweck:** Avatar-Shop. Gems in Charaktere umsetzen.

**Angezeigte Daten:**

- „Charaktere" + aktueller 💎-Stand + kurzer Erklärtext (Gems verdient man durch Boni).
- Großes Maskottchen des aktuellen Charakters.
- **Katalog** aller Charaktere, je mit Gesicht/Emoji, Name, Kurzbeschreibung und **Status**:
  „ausgerüstet" / „wählen" (besessen) / „💎 n" (gesperrt, Preis).
- Hinweis: **Preise und Besitz kommen vom Server** — die App zeigt nur an.

**Aktionen:**

- **Besessenen Charakter antippen → ausrüsten** (jederzeit umschaltbar; HUD-Avatar ändert sich überall).
- **Gesperrten Charakter antippen → Kauf** mit **Bestätigungsdialog** (Gems sind danach unwiderruflich
  weg); nach Kauf automatisch ausgerüstet.
- **Abmelden**.

**Feedback:** Toast-Rückmeldung; bei zu wenig Gems ist der Kauf blockiert (deutlich erkennbar).

### 4.9 Missionen & Auszeichnungen (eingebettete Panels)

**Missionen-Panel** (unter „Erfolge"):

- Liste der Tages-/Wochen-/Zusatzmissionen, je mit Titel, **Fortschritt (current / target)**,
  Periode (Heute / Diese Woche / Ziel), **Belohnungspunkten**, Fortschrittsbalken, „x / y geschafft".
- Neu erfüllte Missionen werden **genau einmal** gefeiert – beim nächsten Besuch von „Erfolge".

**Auszeichnungen-Galerie** (unter „Erfolge"):

- Raster der Badges: erreichte hervorgehoben, offene gedimmt mit Fortschritt zur Schwelle.
- Neu erreichte Badges → große Feier (einmalig).

### 4.10 Das bin ich (Selbstbeschreibung)

**Zweck:** Das Kind sagt selbst, was es mag und was gar nicht, damit Bilder und Beispiele zu ihm passen.
Ein **geführter Prozess in vier Schritten**, ohne Tippen — bis auf den letzten, freiwilligen Schritt.

**Schritte:**

1. **„Was magst du?"** — Kacheln aller Themen-Schlagworte. Einmal antippen = *mag ich*, nochmal =
   *Lieblingsthema*, nochmal = wieder aus.
2. **„Wie sollen die Bilder aussehen?"** — dieselbe Mechanik für Stil-Schlagworte (Comic, Foto, Pixel-Art).
3. **„Was geht gar nicht?"** — Abneigungen. Sie wiegen schwerer als Vorlieben: ein abstoßendes Motiv
   kehrt den Lerneffekt um, darum schließen sie passende Bilder **hart** aus.
4. **„Fehlt noch was?"** — ein kurzer, optionaler Freitext-Wunsch (max. 60 Zeichen), falls das eigene Ding
   nicht in der Liste stand. Er ist **kein geheimer Kanal**: der Vater sieht ihn am Kindprofil.

**Aktionen:** Weiter · Zurück · Abbrechen · Fertig (speichert alles auf einmal).

**Regeln:**

- **Das Kind wählt aus, es erfindet nicht.** Die Schlagwort-Liste pflegt der Creator; ein Kind kann sie
  nicht erweitern. Ein erfundenes Stichwort träfe nie ein Bild und machte die Auswahl schlechter.
- **Nichts wird unterwegs geschickt.** Verlassen ohne „Fertig" ändert nichts (abbrechbar); ein erneuter
  Besuch zeigt den zuletzt gespeicherten Stand (fortsetzbar).
- **Eine Änderung wirkt auf künftige Bilder, nie rückwirkend.** Ein einmal gewähltes Bild bleibt am Wort
  stehen, bis das Kind bewusst „anderes Bild" drückt — Bildkonstanz *ist* der Merkeffekt.
- **Kein Deckel aufs Ändern.** Es gibt nichts zu gewinnen, also nichts zu missbrauchen.

**Zustände:** Erstbesuch (der Pfad bietet den Prozess von selbst an, solange noch nichts gewählt wurde;
danach nur noch über den Menüpunkt im Konto), leere Schlagwort-Liste („dein Vater legt sie an"),
Lade-/Fehlerzustand. Nach dem Speichern eine Feier und zurück zum Pfad.

---

## 5. Navigationsmodell (Gesamtüberblick)

```text
Login  (nicht angemeldet)
  └─(Login erfolgreich)→ Pfad / Tagesmission
        │  [untere Navigation immer sichtbar: Pfad · Erfolge · Konto · Shop · Skins]
        │
        └─ Knoten antippen → Blatt
               ├─ „Los" → Üben
               │      ├─ Runde fertig → „Weiter zum Test" → Test   |  „Zum Pfad" → Pfad
               │      └─ nichts fällig → „Zum Test" → Test          |  „Zum Pfad" → Pfad
               │  („Los" führt direkt in den Test, wenn nichts zu üben ist)
               └─ „Test" (zweiter Knopf, wenn heute testbar) → Test
                      └─ Ergebnis → „Nochmal versuchen" (nur bei Nichtbestehen) | „Zum Pfad" → Pfad

  Erfolge → „Zum Pfad" → Pfad
  Konto  → Stände + Buchungsverlauf ansehen (kein Kauf)
         └─ „Das bin ich" → Selbstbeschreibung → (Fertig) → Pfad
  Shop   → Artikel kaufen → Inventar → Einlösen beantragen → Anfrage-Status
  Skins  → kaufen / ausrüsten; Abmelden
```

**Typischer Tagesablauf:** Anmelden → der Pfad zeigt den nächsten Knoten → antippen, „Los" → alle fälligen Karten
durchgehen (Combos, kleine Feiern) → „Runde fertig!" → „Weiter zum Test" → Test Frage für Frage →
„Sieg!" → Münzen/Gems im HUD gutgeschrieben, Streak steigt → optional in Konto/Shop/Skins ausgeben.

---

## 6. Querschnittsanforderungen ans UI

- **Ein-Hand-Bedienung, Hochformat:** große Tap-Ziele, Aktionen unten erreichbar.
- **Sofortige Rückmeldung:** jede Aktion erzeugt sichtbares/hörbares/haptisches Feedback; Salden
  aktualisieren sich sichtbar.
- **Ladezustände** nach jeder Server-Aktion; **Fehlerzustände** kindgerecht und mit Wiederholung.
- **Leerzustände** überall sinnvoll (kein Plan, nichts fällig, keine Angebote, leeres Inventar,
  keine Anfragen, keine Missionen).
- **Persistenz über Sitzungen:** zuletzt genutzte Kind-Nummer, gewählter Plan, Tempo-Modus, Ton an/aus,
  bereits gefeierte Missionen/Badges (damit Feiern nicht doppelt auslösen).
- **Barrierefreiheit:** „Bewegung reduzieren" respektieren; Ton abschaltbar; ausreichende Kontraste
  und Schriftgrößen für Kinder.
- **Kein Selbstbetrug ermöglichen:** Das UI zeigt nie Lösungen im Test vorab, täuscht keine Punkte vor
  und lässt keine gesperrten Käufe/Einlösungen „durch" — es spiegelt ausschließlich die Server-Wahrheit.

---

## 7. Vollständige Funktionsliste (Checkliste für den UI-Entwurf)

- [ ] Anmelden mit Nummer + PIN-Ziffernpad, Held-Vorschau, Fehlermeldung
- [ ] Persistentes HUD: Avatar, Münzen, Gems, Streak, Ton-Schalter
- [ ] Untere Navigation: Pfad · Erfolge · Konto · Shop · Skins
- [ ] Pfad: Ziel-Status, Plan-Umschalter, Knoten je Position (Ring, Häkchen, Frist, Malus), Abschnitte je Unit, Blatt mit „Los"
- [ ] Ein Kartenbildschirm für alle Typen (Felder der Karte rendern); Test-Knopf nur bei `testable`, sonst „Durchspielen"
- [ ] Üben: Kartenanzeige aller Antwortmodi (umdrehen/tippen/Buchstaben/Auswahl/hören), Tipp, Tempo-Modus, Antwort-Feedback, Combos, Runden-Abschluss
- [ ] Test: Einzelfragen ohne Zurück, Ergebnis-Ring, Item-Auswertung, Wiederholen bei Nichtbestehen
- [ ] Erfolge: Gesamtpunkte, Tage-Fortschritt, Missionen, 7-Tage-Heatmap, große Ziele, Badges
- [ ] Konto: Münz-/Gem-Stand + Punkt-Verlauf (reine Übersicht, kein Kauf)
- [ ] Familien-Shop: Katalog kaufen (Münz-/Gem-Preise, Bestand), Inventar, Einlösung beantragen, Anfrage-Status
- [ ] Skins: Katalog, ausrüsten, kaufen (mit Bestätigung), Gem-Sperre
- [ ] Das bin ich: vierschrittige Selbstbeschreibung per Kacheln (mag / Lieblingsthema / geht gar nicht) plus optionaler Freitext-Wunsch; Erstbesuch-Angebot auf dem Pfad, dauerhafter Einstieg im Konto
- [ ] Missionen-Panel + Auszeichnungen-Galerie mit einmaligen Feiern
- [ ] Feiern in 3 Stufen mit Ton + Haptik; Toasts; Stimmungs-Maskottchen
- [ ] Lade-, Fehler- und Leerzustände auf jedem Screen
