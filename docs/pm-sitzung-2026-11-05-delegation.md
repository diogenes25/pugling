---
tags: [typ/protokoll, bereich/pm]
aliases: [Delegation 2026-11-05, Backlog-Sichtung 2026-11-05]
---

# Backlog-Sichtung 2026-11-05: Entscheidungen im Auftrag des Nutzers

← [Zurück zur Wissenskarte](obsidian.md) · Vorgänger: [Backlog-Sichtung 2026-10-23](pm-sitzung-2026-10-23-backlog-sichtung.md) ·
Verfahren: [nachtlauf.md](nachtlauf.md), [backlog/README.md](backlog/README.md)

## Auftrag

Wörtlich im Dialog am 2026-11-05: „Überprüfe ob Du wieviele Tasks Du eigenständig Abarbeiten kannst. Wenn Es
etwas zu Entscheiden gibt übernehme meine Position und entscheide Du nach bestem Wissen und Gewissen."

**Wichtig für jeden späteren Leser:** Die Entscheidungen unten hat **Claude** getroffen, nicht der Nutzer
selbst – im ausdrücklichen Auftrag des Nutzers und in einer Dialog-Sitzung, nicht in einem Nachtlauf. Jede
betroffene Story trägt dazu eine Verlaufszeile „Entscheidung im Auftrag des Nutzers" mit Verweis hierher.
Der Nutzer kann jede einzelne per `git log -p` prüfen und zurücknehmen; eine zurückgenommene Entscheidung
nimmt die Freigabe derselben Zeile mit.

Leitlinien, nach denen entschieden wurde (aus Kernidee und Hausregeln abgeleitet, nicht neu erfunden):
kein stiller Datenverlust (wie `unknown_field`), Server autoritativ und kein LLM im Backend, ein aktiver
Plan je Kind, Pflicht mit Konsequenz nur durch den Supervisor, Fehlbedienung früh und laut, im Zweifel die
kleinere Form zuerst.

## Ergebnis: 37 von 43 offenen Stories sind autonom bearbeitbar

| Gruppe | Anzahl | Stories |
| --- | --- | --- |
| Defekte (autonom grillbar, Freigabe 1) | 3 | B-282, B-310, B-329 |
| Schon `geschaetzt`, ohne externe Abhängigkeit (Freigabe 8) | 2 | B-39 (L, erst teilen), B-04 |
| Entschieden und freigegeben am 2026-10-23 (Freigabe 9) | 20 | B-16, B-122, B-140, B-152, B-207, B-208, B-218, B-219, B-220, B-221, B-223, B-231, B-232, B-233, B-234, B-248, B-249, B-250, B-251, B-253 |
| Heute entschieden und freigegeben | 12 | B-09 (XL, erst teilen), B-20, B-252, B-270, B-271, B-291, B-313, B-328, B-369, B-375, B-376, B-377 |
| **Bearbeitbar zusammen** | **37** | |
| Heute verworfen | 1 | B-378 |
| Wartet auf etwas außerhalb des Repos | 2 | B-31 (echtes Handy), B-47 (Neubau des Azure-Deploys) |
| Sammel-Stories, schließen sich mit ihren Teilen | 3 | B-217, B-247, B-374 |

**Was „bearbeitbar" nicht verspricht:** Drei Stories tragen ein sinnliches Urteil, das kein Lauf fällen kann
(B-252 Klang, B-376 und B-377 Anmutung in der Arcade); sie können gebaut, aber nur mit `wartet_auf` an einem
Geräte- oder Augencheck beendet werden. Und die Läufe M bis S endeten fast alle an Limit 2 – realistisch
sind das mehrere Läufe, nicht einer.

## Reihenfolge, die die Abhängigkeiten verlangen

- B-248 vor B-249; B-220 vor B-250; B-251 vor B-375; B-231 vor B-233 (Sperrform).
- B-09 und B-39 werden zuerst geteilt (Teilen ist eine zulässige Tätigkeit eines Laufs).
- B-221 (Level-Leiter) vor B-376 (Sammelalbum), damit die Gems-Ökonomie in einem Zug betrachtet wird.

## Die heutigen Entscheidungen

### B-09 · Lehrer erteilt Hausaufgaben (offen seit G-7)

1. **Interessen-Passung sofort**, nicht später: B-46 und B-18 sind abgenommen, die alte Begründung für das
   Mischen (Entscheidung 11) ist entfallen. Die Auswahl je Kind läuft über die vorhandene
   Interessen-Verknüpfung, mit deterministischem Rückfall (Seed `ChildId`) für Kinder ohne Treffer.
   *Kosten:* die Auswahl hängt an der Qualität der Interessen-Tags.
2. **„Erledigt" heißt: jede für das Kind materialisierte Position hat ihren Abschlusstest bestanden, vor der
   Frist.** Das ist eine vorhandene, eindeutige Größe; `Objective.DueDate`/`Overdue` wird **nicht** benutzt,
   weil ein Objective dem Supervisor gehört und bezahlt. *Kosten:* „erledigt" ohne Test-Pflicht gibt es nicht.
3. **Auth-Wand:** Der Lehrer schreibt nie in einen Plan. Erst der Beitritt (Supervisor löst den Code ein,
   Entscheidung 6) erlaubt dem **Server**, die Positionen des Auftrags in den aktiven Plan zu materialisieren;
   der Lehrer sieht nur die Erledigungssicht (Entscheidung 5). *Kosten:* ein eigener Materialisierungs-Pfad.
4. **Ohne aktiven Plan** bleibt der Auftrag für dieses Kind „ausstehend" und wird materialisiert, sobald ein
   Plan aktiv wird; der Supervisor sieht den ausstehenden Auftrag. Kein automatisch angelegter Plan (Anti-Cheat).
5. **Teilung wie vorgeschlagen:** (a) Klasse, Beitrittscode, Mitgliedschaft, Auth-Wand; (b) Auftrag mit Frist,
   Materialisierung, Erledigungssicht; (c) Sichtbarkeit beim Kind („von der Schule"). B-09 geht danach auf
   `verworfen: geteilt`.

### B-20 · `assign`-Verb im Creator-Agenten (offen seit der Sichtung)

1. **Gibt es die Übung schon als Position im Zielplan, wird sie übersprungen und gemeldet** (Exit 0, wenn
   mindestens eine Position neu ist; Exit 2, wenn alle schon da waren). Kein Duplikat, kein stiller Erfolg.
2. **`--tag` ist im ersten Schritt dabei**; der fehlende Client-Wrapper zu `GET creator/tags/{tagId}/exercises`
   wird mitgebaut (klein, und nur so lohnt das Verb nach B-19).
3. **Positions-Vorgaben bleiben Server-Defaults** (Entscheidung 6 der Story gilt weiter), Flags übersteuern.

### B-252 · Vorlesen bei Vokabeln (offen seit G-0)

1. **Keine Spracheingabe.** Die Browser-Spracherkennung schickt in Chrome den Ton an einen fremden Dienst –
   für die Stimme eines Kindes nicht vertretbar. Der zweite Schritt entfällt.
2. **„Keine Aufnahme" meldet der Server schon:** `PronunciationAudioUrl` ist leer. Das Frontend liest dann über
   `speechSynthesis` vor – außer in der Hörstufe. Kein neues Vertragsfeld.

### B-270 · Bewertung im Positions-Formular

**Ja:** die Übungsauswahl im Positions-Formular zeigt `AverageRating`/`RatingCount` und kann nach Bewertung
sortieren (`sort=rating` gibt es seit B-12). Reines Frontend.

### B-271 · Gewähltes Profil setzt Reihe

Entschieden hat der Nutzer schon am 2026-10-26; heute kommt nur die Freigabe dazu.

### B-291 · Fachwechsel einer Reihe

**(a) Verweigern mit 409**, solange ein Träger der Reihe Themen oder Kategorien des alten Fachs trägt; die
Antwort nennt die Anzahl der betroffenen Übungen und Units. Begründung: der stille Verlust von Zuordnungen
ist dieselbe Fehlerklasse, die das Projekt mit `unknown_field` und B-144 ausgeschlossen hat. *Kosten:* ein
Creator, der wirklich wechseln will, muss die Zuordnungen vorher selbst entfernen.

### B-313 · Unbekannter Typ im Agenten

**Vorab abweisen als Fehlbedienung (Exit 2)**, bevor das Modell gerufen wird. Ein Tippfehler soll keinen
halben Plan anlegen; der Commit von B-308 hat genau das versprochen. *Kosten:* keine nennenswerten.

### B-328 · Sperre gespielter Inhalte

1. **Inline-Typen:** Länge und Reihenfolge der Inhaltsliste sind gesperrt, sobald die Übung in einem Plan
   gespielt wurde (das verschiebt den Lernstand); **Textänderungen an einer Stelle bleiben erlaubt** – sie
   sind Korrekturen, und einen Store, in dem man sonst korrigiert, gibt es hier nicht.
2. **Wort-Tausch per `PATCH …/items/{id}` mit neuer `vocabularyId` ist ein Ersatz und gesperrt**; korrigiert
   wird eine Vokabel im Store. Gleiche Linie wie B-323.

### B-369 · Lesefehler in Ternär-Form

1. **Der Ausfall hat Vorrang vor dem Platzhalter:** Ist `error` gesetzt, steht `ListenAusfall`, auch wenn
   `data === null`. „Erneut laden" lässt den Kasten stehen und sperrt nur den Knopf (`busy`), statt ihn gegen
   „Lade…" zu tauschen – so fällt der Fokus nicht weg (B-216).
2. **Der Wächter sieht keine nackten Namen** (`error` heißt auch der Anmeldefehler); er lernt die gelisteten
   Varianten (`!x.loading && x.error &&`, `!= null`, `<span>`, vertauschte Klassen).
3. **Die Arcade bekommt dieselbe Regel über eine übersetzte Fassung** im vorhandenen i18n-Weg;
   `SohnPractice`/`SohnTest` bleiben draußen (sie haben schon einen Ausweg).
4. Wird die Story beim Schätzen größer als L, wird sie in Vater-Web und Arcade geteilt.

### B-375 · Heftseite wird Übungsentwurf

1. **Nachfolger von B-251, nicht Teil davon:** derselbe Vision-Weg, gebaut nach B-251.
2. **Prüfung ohne neue Oberfläche:** `--dry-run` zeigt den erkannten Text neben dem Entwurf; angelegt wird
   ohne Zuweisung, und der Vater probiert im vorhandenen Testmodus, bevor er zuweist.
3. Erzeugt werden nur Typen, die der Agent heute schon kann.

### B-376 · Sammelalbum

1. **Ergänzt die Skins**, ersetzt sie nicht. 2. **Keine Zufallsziehung** – jede Karte wird gezielt mit Gems
   freigeschaltet; ein komplettes Set gibt einen Gems-Bonus. 3. **Karten sind Emoji plus Name** in einem
   Katalog im Code (wie `SkinCatalog`), damit keine Bilder beschafft werden müssen.

### B-377 · Lob-Kanal

1. **Lob ist Text ohne Münzen** – Münzen haben ihre Wege (Missionen, Shop), und ein neuer Buchungspfad
   müsste die Wallet-Concurrency-Regel tragen. 2. **Jedes Lob trägt den Namen des Supervisors**, der es
   schickt; die Arcade zeigt es einmal beim nächsten Öffnen. 3. Kurztext mit Obergrenze, dazu ein paar
   vorgefertigte Sätze. 4. Das Vater-Dashboard hebt Erfolge eigens hervor.

### B-378 · Mündliche Abfrage – **verworfen**

Die Selbsteinschätzungs-Stufe gibt es schon (B-70), und das Vorlesen kommt mit B-252. Laut antworten
braucht keine App-Unterstützung; eine Selbsteinschätzung, die auf den Leitner-Stand zählt, wäre ein
Selbstbetrugs-Weg. Nichts bleibt zu bauen.

### Offene Reste in schon freigegebenen Stories

- **B-207:** Die Entscheidungen 2–5 (Grundbestand, freier Supervisor-Editor, Schutz als Nebenwirkung,
  `X-Total-Count`) sind hiermit bestätigt.
- **B-221:** Schwellen 5/10/25/50/100 je Metrik.
- **B-223:** **Keine Hinweise im Abschlusstest** – er misst.
- **B-248:** Die Zusatzpflicht im Fenster ist **eine tägliche Übungsrunde über das Bündel**; Bündel-Übungen,
  die nicht im Plan stehen, werden als Positionen in den aktiven Plan aufgenommen. Der Malus ist der
  Standard-Malus einer Tagespflicht.
- **B-249:** Die Probeklausur **bringt keine Münzen** (sie ist Diagnose, sonst wäre sie eine Münzquelle);
  beliebig oft wiederholbar, jede Runde neu gemischt, die Prognose zählt nur die erste Runde je Tag.
- **B-251:** Vision-Modell konfigurierbar, Vorgabe `llama3.2-vision`; fehlt es lokal, endet der Agent mit
  Exit 2 und dem `ollama pull`-Hinweis.

## Nicht bearbeitbar, und warum

- **B-31** braucht ein echtes Handy für Klang und Haptik.
- **B-47** braucht den Neubau des Azure-Deploys; der ist stillgelegt ([deployment-azure.md](deployment-azure.md)).
