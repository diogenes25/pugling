---
tags: [typ/spezifikation, bereich/frontend, rolle/student]
---

# Funktionsbeschreibung: Sohn-App (Kind, Handy)

← [Zurück zum Wiki-Index](../README.md) · Verwandt: [06 · Anleitung Sohn-App](../wiki/06-sohn-app.md) (API), [05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md)

**Zweck dieses Dokuments:** Vollständige, **design- und technikunabhängige** Beschreibung aller
Funktionen der Sohn-App. Es beschreibt *was* die App kann (Screens, angezeigte Daten, Aktionen,
Zustände, Übergänge) — **nicht** *wie* sie aussieht. Aus dieser Beschreibung kann ohne Kenntnis von
React, der REST-API oder eines Farbschemas eine komplette Oberfläche entworfen werden.

Zielgerät: **Smartphone im Hochformat**, ein Kind (~9–13 Jahre) als alleiniger Nutzer, oft ohne
Aufsicht. Bedienung muss ohne Lesen langer Texte, mit großen Tap-Zielen und sofortiger Rückmeldung
funktionieren.

---

## 1. Wer benutzt die App und wozu

Der **Sohn** (das Kind) lernt. Er legt **keine** Inhalte an und steuert nichts — das macht der Vater
in einer getrennten App. Der Sohn:

1. sieht seine **Tagesmission** (was heute zu tun ist),
2. **übt** und macht **Tests**,
3. verdient dabei zwei Währungen (**Münzen** und **Gems**),
4. gibt sie im **Familien-Shop** (echte Belohnungen) und für **Charaktere/Skins** aus,
5. verfolgt seinen **Fortschritt**, seine **Missionen** und **Auszeichnungen**.

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

Der Katalog kennt **zwölf** Übungstypen. Sie sind aber **nicht gleichwertig ausgebaut**: Im
Lern-Loop der Sohn-App (Üben + Test über eine Position) ist heute nur ein Teil überhaupt spielbar,
und **richtig entwickelt ist allein die Vokabelübung**. Das ist die zentrale Scoping-Aussage für den
UI-Entwurf: **Version 1 der Sohn-App wird um die Vokabelübung herum gebaut.** Die übrigen Typen sind
Roadmap und sollten im Design höchstens als „kommt bald"-Platzhalter auftauchen, nicht als
funktionsfähige Screens.

Die App muss die Typen nicht fest verdrahten: Der Server liefert ein **Typ-Manifest**
(`GET …/learn/exercise-types`) mit Anzeigename, Renderer-Id, Prüfmodus und Fähigkeiten je Typ. Heute
existiert real aber nur der Vokabel-Renderer (`flashcards`).

| Typ (Label) | Was das Kind tut | Prüfmodus | Reifegrad in der Sohn-App |
|---|---|---|---|
| **Vokabeln** | Karteikasten: Wort ↔ Übersetzung über mehrere Stufen (zeigen → selbst einschätzen → Buchstaben → tippen → hören → Auswahl) | im Positions-Loop (Üben/Test) | ✅ **Voll entwickelt** — Store-verknüpft, Audio, Abfragerichtung, alle Antwortmodi, Leitner. **Design-Schwerpunkt.** |
| **Lückentext** | fehlendes Wort in einen Satz einsetzen (tippen oder Wortbank), optional store-verknüpft | im Positions-Loop (Üben/Test) | 🟡 Play-Loop verdrahtet, aber **flach** (kein ausgereifter Renderer) — Roadmap |
| **Zuordnung** | Paare einander zuordnen (links ↔ rechts), optional mit Ablenkern | im Positions-Loop (Üben/Test) | 🟡 Play-Loop verdrahtet, aber **flach** — Roadmap |
| **Rechenaufgaben** | Rechenaufgaben lösen | nur Katalog-Direktcheck | ⛔ **nicht** im Sohn-Positions-Loop — Roadmap |
| **Rechen-Drill** | fortlaufend generierte Rechenaufgaben | nur Katalog (erzeugen + prüfen) | ⛔ **nicht** im Sohn-Positions-Loop — Roadmap |
| **Liste** | Elemente einer Liste nennen | nur Katalog-Check | ⛔ **nicht** im Sohn-Positions-Loop — Roadmap |
| **Leseverständnis** | Text lesen, Fragen beantworten | keine Auto-Prüfung | ⛔ reine Inhaltsübung — Roadmap |
| **Hörverständnis** | Audio hören, Fragen beantworten | keine Auto-Prüfung | ⛔ reine Inhaltsübung — Roadmap |
| **Grammatik** | Grammatik-Aufgaben mit Regelhinweisen | keine Auto-Prüfung | ⛔ Roadmap |
| **Übersetzung** | ganze Sätze übersetzen | keine Auto-Prüfung | ⛔ Roadmap |
| **Aufsatz** | freien Text schreiben | keine Auto-Prüfung (manuell) | ⛔ Roadmap |
| **Birkenbihl** | Wort-für-Wort-Dekodierung lesen | keine Prüfung (Anzeige) | ⛔ reine Inhaltsübung — Roadmap |

**Folge fürs UI:** Die Positions-Kacheln auf der Basis dürfen zwar unterschiedliche Typ-Icons zeigen,
aber ein spielbares **Üben/Test-Erlebnis** muss nur die **Vokabelübung** vollständig liefern. Für
nicht-spielbare oder unreife Typen zeigt die App einen freundlichen Hinweis („Diese Übung kannst du
bald spielen") statt einer kaputten Spielansicht.

### 2.8 Antwort-/Stufen-Modi einer Karte (datengetrieben, ein UI bedient alle)

Innerhalb einer spielbaren Übung (heute v. a. der Vokabelübung) kommt jede einzelne Karte bzw.
Testfrage vom Server mit Feldern, aus denen sich der **Antwortmodus** ergibt — meist eine **Leitner-
Stufe** desselben Inhalts, die schrittweise mehr fordert. Das UI muss **dieselben** Interaktionsmuster
für alle diese Modi bereitstellen:

| Kartentyp | Woran erkennbar | Was das Kind tut |
|---|---|---|
| **Umdrehen / Selbsteinschätzung** | Lösung wird mitgeliefert (`reveal`) | Aufgabe lesen → **Umdrehen** → selbst bewerten: „Gewusst" / „Nicht gewusst" |
| **Tippen (Freitext)** | keine Lösung, Antwortlänge unbekannt | Antwort in ein Textfeld eingeben → **Prüfen** |
| **Buchstabenkästchen** | keine Lösung, Antwortlänge bekannt | pro Buchstabe ein Kästchen ausfüllen → **Prüfen** |
| **Auswahl (Multiple Choice)** | Auswahlmöglichkeiten mitgeliefert (`choices`) | eine Option antippen → **Prüfen** |
| **Hören** | Audio mitgeliefert (`audioUrl`) | Audio abspielen (statt/zusätzlich zum Text), dann antworten wie oben |

Jede Karte kann zusätzlich einen **optionalen Tipp/Hinweis** (💡) tragen, der erst auf Wunsch
aufgedeckt wird. Jede Karte/Frage trägt eine **Stufen-Kennzeichnung** (z. B. „Zeigen", „Selbstcheck",
„Buchstaben", „Tippen", „Hören", „Auswahl") — als kleines Label anzeigen.

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

- **🏠 Basis** — Tagesmission (Startseite)
- **🗺️ Weg** — Fortschritt / Trophäenweg
- **💰 Konto** — Münz-/Gem-Stand und Buchungsverlauf (reine Übersicht, kein Kauf)
- **🛒 Shop** — Familien-Shop (Artikel kaufen, Inventar, Aktivierung) — der einzige Münz-Ausgabeweg
- **🎭 Skins** — Charaktere

Die Screens **Üben** und **Test** haben **keinen** eigenen Navigationseintrag — sie werden aus der
Basis über eine Übungskachel betreten und über „Zur Basis" wieder verlassen.

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

- Erfolg → **Basis / Tagesmission**.
- Fehler → freundliche Meldung („Nummer oder PIN falsch"), PIN wird geleert, kein Wechsel.
- Warten → kurzer Ladezustand am Anmelde-Knopf.

### 4.2 Basis / Tagesmission (Startseite)

**Zweck:** Zeigt, **was heute zu tun ist**, und ist Absprung zu Üben/Test. Wichtigster Screen.

**Angezeigte Daten:**

- **Missionskopf:** Stimmungs-Maskottchen, „Tagesmission", Titel des aktiven Plans.
- **Ziel-Status:** entweder „✓ Heute geschafft!" oder ein Fortschritt „🎯 Ziele heute: x / y".
- **Plan-Auswahl:** wenn mehrere Pläne existieren, ein Umschalter (der spielbare ist vorgewählt).
- **Positions-Kacheln** (eine je Übung des Plans), je mit:
  - Typ-Icon der Übung (Vokabeln, Lückentext, Zuordnen, Rechnen, Hören, Lesen …),
  - Übungstitel,
  - Rhythmus-Kennzeichnung (Tagesziel / Wochenziel / frei),
  - Status (✓ erledigt / offen), bei Leitner ggf. „(n fällig)".
- **Missionen-Panel** (siehe 4.9) unten eingebettet.

**Aktionen (je Kachel):**

- **Üben** (bzw. „Durchspielen" bei freien Positionen) → Screen *Üben*.
- **Test** (falls die Position testbar ist) → Screen *Test*.

**Zustände:**

- **Kein Plan:** schlafendes Maskottchen + Hinweis „Noch keine Mission – dein Vater hat noch keinen
  Lehrplan erstellt" + Abmelden.
- **Alles erledigt:** klar sichtbarer Erledigt-Zustand, Feier möglich.
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

- Fortschritt „Karte i / n".
- Die Aufgabe (Text oder Audio-Abspielknopf bei Hör-Karten).
- Je nach Antwortmodus (siehe 2.8): Umdreh-Fläche, Textfeld, Buchstabenkästchen oder Auswahl-Optionen.
- Optionaler Tipp (💡), erst auf Antippen sichtbar.
- Optionaler **Tempo-Modus**: eine Countdown-Leiste als Ansporn (schnellere Antwort → Bonus).

**Aktionen:**

- **Umdrehen** und danach selbst bewerten (**Gewusst / Nicht gewusst**) — bei Selbsteinschätzungs-Karten.
- **Antwort eingeben/auswählen** und **Prüfen** — bei getippten/Auswahl-Karten.
- **Tipp aufdecken**.
- **Tempo-Modus an/aus** (wird gemerkt).
- **Runde beenden / Zur Basis**.

**Feedback nach jeder Antwort:**

- Richtig: Punkt-Toast („+n 🪙 · Box x"), Combo-Zähler, ggf. Combo-/Tempo-Bonus, kleine Feier;
  Anzeige der nächsten Fälligkeit der Karte.
- Falsch: Lösung wird gezeigt („Lösung: …").
- Nicht wertbar (nicht fällig / heute schon gewertet): still protokolliert, keine Punkte — das UI
  darf hier keinen Punktgewinn vortäuschen.

**Zustände & Übergänge:**

- **Fertig:** „Runde fertig!" mit verdienten Münzen + Kartenzahl; Knöpfe „Weiter zum Test" und
  „Zur Basis".
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

- „Tagestest", Fortschritt „Frage i / n", Stufen-Label.
- Die Aufgabe (Text oder Audio) + optionaler Tipp.
- Eingabe je nach Antwortmodus (siehe 2.8).

**Aktionen:**

- Antworten (tippen / auswählen / aufdecken + selbst bewerten) und **Weiter**.

**Ergebnis-Screen:**

- Titel „Sieg!" / „Fast!", Stimmungs-Maskottchen.
- **Prozent-Ring**: erreichter Score, richtige / gesamt.
- Bestehensgrenze (Standard 80 %, ggf. abweichend).
- **Item-für-Item-Auswertung**: je Frage ✅/❌, Aufgabe, erwartete Lösung.
- Knöpfe: „Nochmal versuchen" (nur wenn nicht bestanden), „Zur Basis".
- Bestehen → große Feier; Streak und Salden werden aktualisiert.

### 4.5 Weg / Trophäenweg (Fortschritt)

**Zweck:** Motivierender Überblick über den Fortschritt im aktiven Plan.

**Angezeigte Daten:**

- „Trophäenweg" + 🏆 **Gesamtpunkte**.
- Plantitel · „x / y Tage geschafft" · Streak 🔥.
- **Meine Übungen:** je Position ein Status (✅ erfüllt / ⬜ offen / • frei) mit Rhythmus-Label.
- **Letzte Tage:** eine kleine Kalender-Heatmap der letzten ~7 Tage (teilweise geschafft vs. Tag
  komplett) mit Legende.
- **Auszeichnungen-Galerie** (siehe 4.9).

**Aktionen:**

- „Zur Tagesmission" → Basis.

**Zustände:** Fällt auf einen Basis-Hinweis zurück, wenn noch keine Mission gewählt ist.

### 4.6 Konto (Übersicht)

**Zweck:** Münz-/Gem-Stand und Buchungsverlauf ansehen. **Kein Kaufweg** — ausgegeben wird im 🛒 Shop
(Münzen) bzw. 🎭 Skins (Gems).

**Angezeigte Daten:**

- „Mein Konto" + 🪙- und 💎-Stand + Erklärtext (Münzen → Shop, Gems → Skins).
- **Verlauf:** Punkt-Buchungen mit Art-Label (Basis, Test, Combo, Tempo, Mission, Auszeichnung,
  Skin-Kauf, Shop-Kauf …), Grund und +/- Betrag.

**Aktionen:** keine (reine Lese-Übersicht) — zum Ausgeben in den Shop bzw. zu den Skins wechseln.

**Zustände:** Leerzustand, wenn es noch keine Buchungen gibt; Lade-/Fehlerzustand.

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

**Missionen-Panel** (auf der Basis):

- Liste der Tages-/Wochen-/Zusatzmissionen, je mit Titel, **Fortschritt (current / target)**,
  Periode (Heute / Diese Woche / Ziel), **Belohnungspunkten**, Fortschrittsbalken, „x / y geschafft".
- Neu erfüllte Missionen werden **genau einmal** gefeiert.

**Auszeichnungen-Galerie** (auf dem Weg):

- Raster der Badges: erreichte hervorgehoben, offene gedimmt mit Fortschritt zur Schwelle.
- Neu erreichte Badges → große Feier (einmalig).

---

## 5. Navigationsmodell (Gesamtüberblick)

```text
Login  (nicht angemeldet)
  └─(Login erfolgreich)→ Basis / Tagesmission
        │  [untere Navigation immer sichtbar: Basis · Weg · Konto · Shop · Skins]
        │
        ├─ Übungskachel „Üben/Durchspielen" → Üben
        │      ├─ Runde fertig → „Weiter zum Test" → Test   |  „Zur Basis" → Basis
        │      └─ nichts fällig → „Zum Test" → Test          |  „Zur Basis" → Basis
        │
        └─ Übungskachel „Test" → Test
               └─ Ergebnis → „Nochmal versuchen" (nur bei Nichtbestehen) | „Zur Basis" → Basis

  Weg    → „Zur Tagesmission" → Basis
  Konto  → Stände + Buchungsverlauf ansehen (kein Kauf)
  Shop   → Artikel kaufen → Inventar → Einlösen beantragen → Anfrage-Status
  Skins  → kaufen / ausrüsten; Abmelden
```

**Typischer Tagesablauf:** Anmelden → Basis zeigt Tagesmission → „Üben" → alle fälligen Karten
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
- [ ] Untere Navigation: Basis · Weg · Konto · Shop · Skins
- [ ] Tagesmission: Ziel-Status, Positionskacheln, Plan-Umschalter, Missionen-Panel
- [ ] **Scoping v1: Üben/Test nur für Vokabeln voll bauen**; andere Typen als „kommt bald"-Hinweis statt kaputter Spielansicht
- [ ] Üben: Kartenanzeige aller Antwortmodi (umdrehen/tippen/Buchstaben/Auswahl/hören), Tipp, Tempo-Modus, Antwort-Feedback, Combos, Runden-Abschluss
- [ ] Test: Einzelfragen ohne Zurück, Ergebnis-Ring, Item-Auswertung, Wiederholen bei Nichtbestehen
- [ ] Weg/Trophäenweg: Gesamtpunkte, Tage-Fortschritt, Übungsstatus, 7-Tage-Heatmap, Badges
- [ ] Konto: Münz-/Gem-Stand + Punkt-Verlauf (reine Übersicht, kein Kauf)
- [ ] Familien-Shop: Katalog kaufen (Münz-/Gem-Preise, Bestand), Inventar, Einlösung beantragen, Anfrage-Status
- [ ] Skins: Katalog, ausrüsten, kaufen (mit Bestätigung), Gem-Sperre
- [ ] Missionen-Panel + Auszeichnungen-Galerie mit einmaligen Feiern
- [ ] Feiern in 3 Stufen mit Ton + Haptik; Toasts; Stimmungs-Maskottchen
- [ ] Lade-, Fehler- und Leerzustände auf jedem Screen
