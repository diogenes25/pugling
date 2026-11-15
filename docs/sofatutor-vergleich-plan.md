---
tags: [typ/plan, bereich/training, bereich/auswertung, bereich/klassenarbeit, rolle/supervisor, rolle/student, rolle/creator]
aliases: [sofatutor-Vergleich, sofatutor, Frage an den Betreuer, Einstufungstest]
---

# sofatutor als Vergleichsmaßstab: was wir übernehmen, was nicht

Status: **Plan** (2026-11-15). Die Übernahme läuft als Stories im [Backlog](backlog/README.md); die
Sammel-Story ist [B-585](backlog/B-585-sofatutor-vergleich-sammel-story.md). Dieses Dokument hält den
Vergleich und die Reihenfolge, **nicht** den Zustand der Etappen.

← [Zurück zur Wissenskarte](obsidian.md) · Vorgänger-Vergleiche: [Khan Academy](khan-academy-vergleich-plan.md),
[Knowunity](knowunity-vergleich-plan.md), [Baobab](baobab-vergleich-plan.md),
[Duolingo/Snipwise](duolingo-snipwise-vergleich-plan.md), [RocketTutor](rockettutor-vergleich-plan.md) ·
Fachmodell: [grundprinzip.md](grundprinzip.md)

## Warum dieser Vergleich

[sofatutor](https://www.sofatutor.com/) ist die große deutsche Abo-Lernplattform der sofatutor GmbH
(Berlin) für Klasse 1 bis 13, dazu ein Vorschul-Ableger. Eigenangaben: über 2,1 Mio. Schüler, über
265.000 Lehrkräfte, über 6.630 Lernvideos und knapp 34.000 Übungen. Käufer sind Eltern (Basis- und
Premium-Abo) und Schulen (Schullizenz auf Anfrage, kostenlos für Startchancen-Schulen).

sofatutor ist ein **Nachschlagewerk mit Übungen**: Das Kind holt sich, was es gerade braucht (Video,
Übung, Arbeitsblatt, Frage an eine Lehrkraft). Pflicht, Termin und Konsequenz kommen nicht vor; die
Eltern sind Käufer und Zuschauer. Damit ist sofatutor das Gegenmodell zu Puglings Kernidee, und der
Vergleich lohnt vor allem dort, wo sofatutor eine **Hilfe** anbietet, die Pugling dem Kind heute nicht
gibt: einen Menschen fragen und den eigenen Stand vorab einstufen lassen.

Quellenstand: Startseite, die Format-Seiten (Übungen, Vokabeltrainer, Arbeitsblätter, Sofaheld,
Hausaufgaben-Chat), weiterführende Schule, Lehrkräfte/Schulen, Startchancen, Datenschutzerklärung und
das Hilfe-Center, abgerufen am 2026-11-15 (Links am Ende). Kein Konto angelegt, die App nicht
durchgespielt. Die Seiten für Lehrkräfte und Schulen zeigen öffentlich nur eine Rollenauswahl; Preise
stehen nicht offen auf der Seite. Was unten nicht als „unabhängig" markiert ist, ist **Anbieterangabe**.

## Was sofatutor anbietet

- **Lernvideos** je Thema mit Geschichten, nach Lehrplänen der Bundesländer. Ein Bezug zu einem
  bestimmten Schulbuch wird nicht genannt.
- **Interaktive Übungen** (Zuordnen, Sortieren, Lücken, Multiple Choice, Bild beschriften, Hören) mit
  Sofort-Feedback, einem **Tipp auf Abruf** und einer **Video-Hilfe**, die an die passende Stelle des
  Videos springt.
- **Vokabeltrainer** (Englisch, Französisch, Spanisch) mit Bildern und **Audio**, verteilter
  Wiederholung („merkt sich deinen Lernstand") und Vokabeln, die man selbst hinzufügt.
- **Arbeitsblätter zum Ausdrucken** mit Lösungsschlüssel, beworben als Vorbereitung auf Klassenarbeiten
  „unter Prüfungsbedingungen"; die Korrektur macht das Kind selbst am Schlüssel.
- **Hausaufgaben-Chat mit Lehrkräften** (Mensch, 17–19 Uhr; außerhalb eine Fragenbox), auch mit **Foto
  der Aufgabe**. Erklärt wird, fertige Lösungen werden nicht versprochen. Nur im Premium-Abo.
- **KI-Chatbot „Sofabuddy"** mit Sprachfunktion und Upload eigener Materialien. Laut Datenschutzerklärung
  läuft der Chatbot über die ChatGPT-API von OpenAI (Irland).
- **Lernplan nach einem Quiz** von etwa zwei Minuten; Tempo und Schwierigkeit sollen sich anpassen. Wie
  genau, steht nirgends.
- **Sofaheld** (Klasse 1–9): Lernwelt mit einem Pinguin; richtige Antworten bringen **Lernpunkte**, die
  neue Pinguine oder ein Minispiel freischalten. Vorgeschlagen wird eine 12-Minuten-Routine (5 Video,
  5 Übung, 2 Frage).
- **Eltern:** bis zu drei Geschwisterprofile in einem Konto; Eltern sehen je Kind Aktivitäten und
  Merklisten. Ein Eltern-Werkzeug mit Zielen, Zeiten oder Konsequenzen wird nicht beschrieben.
- **Lehrkräfte:** Videos und Übungen per **Link ohne Anmeldung** für 14 (Basis) bzw. 60 Tage (Premium)
  freischalten; Lernstandserhebung im Premium-Zugang. Eine Auswertung je Hausaufgabe wird nicht
  beschrieben.
- **Preise:** 30 Tage gratis, dann Abo. Öffentlich nur Drittangaben (etwa 8–15 € im Monat bei 12 bzw.
  24 Monaten Vorkasse).
- **Plattformen und Datenschutz:** Browser und Apps; Hosting bei AWS (Luxemburg), zahlreiche
  US-Dienstleister (Analytics, Support, Mail), Facebook-Conversion-API für Werbung.

**Unabhängige Stimmen** (Bewertungsportale, App-Store): überwiegend gut (etwa 4,5–4,7 von 5 bei
trusted.de, erfahrungen.com, App-Store AT). Gelobt werden Notenverbesserung und dass Eltern den
Fortschritt sehen. Die ernste Kritik betrifft fast nur das **Abo**: auf ErfahrungenScout berichten 61
von 196 Bewertern, dass aus dem Test ungewollt ein Jahres- oder Zweijahresabo wurde. Vereinzelt heißt
es, die Videos seien für Klasse 9/10 zu kindlich – für Friedrich (15) ein Hinweis, kein Beleg.

## Gemeinsamkeiten

| sofatutor | Pugling | Bewertung |
| --- | --- | --- |
| Vokabeltrainer mit verteilter Wiederholung | Leitner je Position, Tagesdosis neuer Wörter in Buchreihenfolge (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:201`), plan-übergreifender Stand je Wort (`Services/Shared/ItemProgressService.cs:60`) | Gleichwertig; Pugling ist mit Pflicht und Dosis strenger. |
| Bild und Audio an der Vokabel | Aussprache-Audio am Store-Wort (`Models/VocabEntities.cs:54`), an der Karte ausgeliefert (`Controllers/Student/PositionPracticeController.cs:157`), Bilder je Kind gewählt, auf getippten Stufen zurückgehalten (`PositionPracticeController.cs:241`) | Gleichwertig; Audio hängt bei uns am Upload. Vorlesen ohne Upload ist [B-252](backlog/B-252-vorlesen-und-sprechen-bei-vokabeln.md). |
| Tipp auf Abruf | `Hint` am Item (`Models/ExerciseItemEntities.cs:34`), `HintsUsed` gezählt (`Models/StudyPlanEntities.cs:199`) | Gleichwertig. |
| Geschwisterprofile, Eltern sehen je Kind | Mehrere Kinder je Supervisor über `SupervisorLink` (`Models/AdminEntities.cs:17`), Lernstand je Kind | Pugling weiter: Eltern steuern, nicht nur schauen. |
| Lernpunkte schalten Pinguine frei | Gems → Skins (`Models/AdminEntities.cs:123`) | Gleichwertig; bestätigt die Zwei-Währungs-Ökonomie. |
| Lehrplan der Bundesländer | Lehrwerk-Reihe und Unit am Kind (`Models/AdminEntities.cs:173`, `:180`) | Pugling näher an der Schule: das eigene Buch statt des Landeslehrplans. |

## Wo sofatutor weiter ist

1. **Ein Mensch beantwortet Fragen zur Aufgabe** – mit Foto, ohne fertige Lösung. Bei Pugling kann das
   Kind niemanden fragen. Das Erfassungs-Widget samt Kontext-Mitschnitt gibt es zwar
   (`Controllers/RemarksController.cs:12`), es ist aber ein Dev-Werkzeug und in der Sohn-App nur im
   Entwicklungsmodus eingebunden (`frontend/src/sohn/SohnApp.tsx:174`). Ein Nachrichten-Modell zwischen
   Kind und Betreuer gibt es nicht (keine Entität in `backend/Pugling.Api/Models/`). → **B-586**
2. **Einstufung vor dem Start.** Ein kurzes Quiz legt den Startpunkt fest. Pugling führt jedes Wort einer
   Position neu ein, in Buchreihenfolge und nach Tagesdosis
   (`Services/Shared/PositionPlayService.cs:201`–`215`, `Models/PlanPositionEntities.cs:40`), auch wenn
   das Kind es längst kann. → **B-587**
3. **Hilfe springt zum Stoff.** Die Video-Hilfe öffnet die passende Stelle. Das Pugling-Gegenstück wäre
   der Sprung von der falschen Antwort zur Stoffnotiz der Unit; das gehört zur Erklärung nach der Antwort
   ([B-583](backlog/B-583-erklaerung-nach-falscher-antwort.md), [B-529](backlog/B-529-erklaerung-nach-der-antwort.md)). Den Bezug selbst – Unit und Buchseite an der Übung, „schon durchgenommen" beim Kind – hat der Nutzer im Gespräch ergänzt. → **B-588**
4. **Papier zur Klassenarbeits-Vorbereitung.** Arbeitsblätter mit Lösungsschlüssel. Pugling hat keine
   Druckausgabe (weder im Backend noch ein `window.print`/`@media print` im Frontend); das ist schon
   [B-582](backlog/B-582-probearbeit-auf-papier-mit-foto-korrektur.md).
5. **Freigabe-Link für Lehrkräfte**, ohne dass Schüler ein Konto brauchen. Anregung für
   [B-09](backlog/B-09-lehrer-hausaufgaben.md).

## Wo Pugling stärker ist

- **Pflicht mit Konsequenz.** Positionen mit Pflichtziel und Münz-Malus
  (`Models/PlanPositionEntities.cs:91`), Klassenarbeit mit Termin und echter Note
  (`Models/KlassenarbeitEntities.cs:79`, `:82`). sofatutor kennt weder Pflicht noch Termin.
- **Belohnung, die die Eltern stiften.** Familien-Shop (`Models/GamificationEntities.cs:175`) statt
  Pinguin-Freischaltung. Für Friedrich der stärkste Hebel ([B-535](backlog/B-535-belohnungen-erreichen-friedrich-nicht.md)).
- **Das eigene Schulbuch** statt eines allgemeinen Landeslehrplans, inklusive Lehrwerk aus Fotos (Skill
  `lehrwerk-aufbereiten`) und Friedrichs Schulformat ([B-524](backlog/B-524-vokabeltest-wie-in-der-schule.md)).
- **Server-Autorität und Anti-Cheat.** Lösungsfelder sind vor dem Studenten gegated; ein KI-Chat beim
  Kind, wie sofatutor ihn hat, wäre zugleich ein Lösungs-Kanal.
- **Daten bleiben im Haus.** Kein LLM im Backend (in `backend/Pugling.Api` kein OpenAI-/Ollama-Bezug),
  keine Werbe- oder Analyse-Dienste, Betrieb im Heimnetz.
- **Kein Abo**, also auch keine Abo-Falle – der Hauptkritikpunkt an sofatutor.

## Bewusst nicht übernommen

- **KI-Chatbot beim Kind** (Sofabuddy). Bricht „kein LLM im Backend" und die Lösungsfeld-Regel und
  schickt Kinderfragen an einen Drittanbieter. Die passende Form ist B-586: Die Antwort gibt ein Mensch,
  der Betreuer. Wer mag, lässt sich dort vom KI-Creator **offline** einen Antwortentwurf machen, den er
  prüft.
- **Lernvideos.** Content-Aufwand, den eine Familie nicht leisten kann; die Stoffnotizen der Unit und die
  Erklärung nach der Antwort (B-583) decken den Vokabel- und Grammatik-Schwerpunkt.
- **Selbstgesteuertes Nachschlagen als Prinzip.** sofatutor lässt das Kind wählen. Bei Pugling entscheidet
  der Vater, was Pflicht ist; Hilfe darf die Pflicht stützen, nicht ersetzen.
- **Selbstkorrektur am Lösungsschlüssel.** Bei Papier korrigiert der Vater bzw. sein Werkzeug (B-582),
  nicht das Kind – sonst ist die Papierprobe Selbstauskunft.
- **Merkliste des Kindes für die Klassenarbeit.** Die Leitner-Stufe und `onlyWeak` wissen schon, was
  wackelt; was das Kind selbst markiert, ist schwächer als die gemessene Schwäche. Der Weg zur Pflicht
  ist [B-584](backlog/B-584-schwache-woerter-werden-wiederholungs-position.md).
- **12-Minuten-Routine und Minispiel.** Die feste Sitzungslänge ist im
  [Duolingo-Vergleich](duolingo-snipwise-vergleich-plan.md) behandelt, das Ausgeben von Punkten im
  [Baobab-Vergleich](baobab-vergleich-plan.md) (Sammelalbum, B-376).
- **Abo- und Werbemodell.**

## Abgrenzung zu den früheren Vergleichen

sofatutor bestätigt vor allem Bekanntes: Vorlesen (Knowunity, B-252), Erklärung beim Fehler (RocketTutor
B-583, Snipwise B-529), Papier als Prüfungsform (RocketTutor B-582), Eltern-Übersicht (B-39),
Lehrer-Freigabe (B-09). Diese Stories bekommen je einen Verlaufseintrag „Prioritäts-Beleg von außen",
keine neue Story. Neu ist nur, was keiner der früheren Anbieter so hatte: ein **Mensch als Ansprechpartner
bei der Aufgabe** und die **Einstufung vor dem Start**. Der Lob-Kanal aus dem Baobab-Vergleich
([B-377](backlog/B-377-lob-kanal-des-vaters.md)) läuft in die andere Richtung (Vater → Kind, Erfolg →
Vater) und ist keine Hilfe bei der Aufgabe.

## Nutzen für Theodor und Friedrich

- **Theodor (12, Englisch/Latein, Access-Buch):** Die Frage an den Vater (B-586) hilft am meisten, wo
  Latein-Grammatik hakt und der Vater nicht danebensitzt. Die Einstufung (B-587) spart ihm bekannte
  Access-Wörter aus früheren Units.
- **Friedrich (15, Französisch > Englisch > Mathe):** Er scheitert an zu schwerem Stoff
  ([B-536](backlog/B-536-aufbau-uebungen-statt-versagen.md)) und braucht Erfolge, die Münzen bringen
  (B-535). B-586 gibt ihm einen Ausweg statt „0 von 8"; B-587 schiebt die Wörter, die er schon kann,
  aus der Einführung, damit die Tagesdosis auf das Neue geht. Vorlesen (B-252) ist für die französische
  Aussprache wichtiger als für Englisch.

## Übernahmeplan

Backend zuerst (API-First); jede Story läuft die normale Kette im Backlog. Die Beschreibung hier ist der
Anlass, nicht der Ist-Stand.

| Etappe | Story | Was | Hängt ab von |
| --- | --- | --- | --- |
| 1 | [B-586](backlog/B-586-kind-fragt-den-betreuer-zur-aufgabe.md) | Kind stellt zur aktuellen Aufgabe eine Frage an seine Betreuer, mit Kontext; Antwort kommt asynchron; nie während der Klausur | berührt [B-377](backlog/B-377-lob-kanal-des-vaters.md) (Kanal Kind ↔ Vater) |
| 2 | [B-587](backlog/B-587-einstufungstest-vor-der-vokabel-position.md) | Einstufungstest vor einer Vokabel-Position: sicher getippte Wörter überspringen die Einführung | [B-04](backlog/B-04-adaptiver-vokabel-pool.md) (Tagesdosis), [B-580](backlog/B-580-plan-aus-unit.md) |
| 3 | [B-588](backlog/B-588-uebung-zeigt-ihren-lehrwerk-bezug.md) | Übung zeigt Unit und Seite im Lehrwerk; das Kind sieht „schon durchgenommen" (Wunsch des Nutzers, Gegenstück zur Video-Hilfe) | Seite aus [B-375](backlog/B-375-heftseite-wird-uebungsentwurf.md); Anti-Cheat: nie während Test/Klausur |
| – | [B-252](backlog/B-252-vorlesen-und-sprechen-bei-vokabeln.md) (bestehend) | Vorlesen an der Vokabelkarte | Verlaufseintrag |
| – | [B-583](backlog/B-583-erklaerung-nach-falscher-antwort.md) (bestehend) | Erklärung nach der Antwort, mit Sprung zur Stoffnotiz | Verlaufseintrag |
| – | [B-582](backlog/B-582-probearbeit-auf-papier-mit-foto-korrektur.md), [B-39](backlog/B-39-supervisor-dashboard.md), [B-09](backlog/B-09-lehrer-hausaufgaben.md) (bestehend) | Papier, Eltern-Übersicht, Lehrer-Freigabe | Verlaufseinträge |

Etappe 1 ist die eigentliche Übernahme: sofatutor verkauft den Menschen als Premium-Merkmal, Pugling hat
ihn schon – den Vater. Es fehlt nur der Kanal.

## Quellen

- [sofatutor.com](https://www.sofatutor.com/) · [Übungen](https://www.sofatutor.com/informationen/uebungen) ·
  [Vokabeltrainer](https://www.sofatutor.com/informationen/vokabeltrainer) ·
  [Arbeitsblätter](https://www.sofatutor.com/informationen/arbeitsblaetter) ·
  [Sofaheld](https://www.sofatutor.com/informationen/sofaheld) ·
  [Hausaufgaben-Chat](https://www.sofatutor.com/informationen/hausaufgaben-chat) ·
  [Weiterführende Schule](https://www.sofatutor.com/informationen/weiterfuehrende-schule)
- [Lehrkräfte und Schulen](https://www.sofatutor.com/lehrkraefte-und-schulen) ·
  [Startchancen](https://www.sofatutor.com/startchancen) ·
  [FAQ](https://www.sofatutor.com/help/faq) · [Datenschutz](https://www.sofatutor.com/legal/datenschutz)
- Hilfe-Center: [Mitgliedschaften](https://sofatutor-com.helpscoutdocs.com/article/90-welche-mitgliedschaften-gibt-es-bei-sofatutor-und-wie-unterscheiden-sich-diese) ·
  [Freischalten für Schüler](https://sofatutor-com.helpscoutdocs.com/article/87-kann-ich-mit-meinem-zugang-fur-lehrkrafte-auch-inhalte-fur-meine-schuler-innen-freischalten-oder-als-hausaufgabe-aufgeben) ·
  [Geschwister](https://sofatutor-com.helpscoutdocs.com/article/508-wie-lernen-geschwister-mit)
- Unabhängig: [ErfahrungenScout](https://erfahrungenscout.de/dienstleistungen/sofatutor-bewertungen) ·
  [erfahrungen.com](https://www.erfahrungen.com/mit/sofatutor/) · [trusted.de](https://trusted.de/sofatutor)
  (beim Abruf 403, Angaben aus dem Suchauszug) · [apptail (App-Store AT)](https://apptail.io/app/sofatutor-JgH/austria) ·
  [mydealz (Preisangabe)](https://www.mydealz.de/deals/sofatutor-lernplattform-30-tage-gratis-80-danach-799mtl-premium-mitgliedschaft-schule-24h-hilfe-von-lehrerinnen-2834219)

---

**Verwandt:** [rockettutor-vergleich-plan.md](rockettutor-vergleich-plan.md) ·
[baobab-vergleich-plan.md](baobab-vergleich-plan.md) · [knowunity-vergleich-plan.md](knowunity-vergleich-plan.md) ·
[khan-academy-vergleich-plan.md](khan-academy-vergleich-plan.md) · [docs/backlog/](backlog/README.md)
