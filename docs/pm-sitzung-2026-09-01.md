---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-09-01, Die Oberflaeche verschweigt nichts mehr]
---

# PM-Sitzung 2026-09-01 — Nachtlauf: die Oberfläche verschweigt weder Erfolg noch Fehler

## Auftrag und Freigaben

Unbeaufsichtigter Backlog-Lauf nach `pm-loop` und [docs/backlog/README.md](backlog/README.md).
**Alle acht Freigaben** aus [docs/nachtlauf.md](nachtlauf.md) gelten, vom Nutzer für diesen Lauf erteilt.

**Abweichung zu Freigabe 6, vor dem Start festgestellt:** In dieser Sitzung ist **keine Browser-Verbindung**
verfügbar. Der Rollengang läuft also durchgehend über den dokumentierten Ersatz — bevorzugt eine eigene
Playwright-E2E-Spec (die *ist* der Rollengang, nur wiederholbar), sonst Integrationstest oder Live-Probe.
Jede Story benennt ihren Ersatz im `## Verlauf`.

## Ausgangslage (gemessen, nicht geschätzt)

- Index: **61 offen**, 112 abgenommen, 12 verworfen.
- `geschaetzt`: **27** — zwei davon blockiert (B-07 wartet auf einen Handgriff an der Azure-Instanz,
  B-31 auf ein echtes Handy).
- `ausformuliert`: **15**, ausnahmslos `Defekt`/`Aufräumen` → nach Freigabe 1 vollständig autonom grillbar.
- `idee`: **19**, davon 11 `Defekt`/`Aufräumen` (baubar nach Refinement), 8 `Wunsch`/`Frage` (**gesperrt**).
- `in-arbeit`: **0** — kein Rest aus einem früheren Lauf.
- Nachschau beim Start: **107 von 112**, Arbeitsvorrat 5 (B-165, B-172, B-180, B-182, B-45).
  Davon sind **B-172 und B-45 die Arbeit des vorigen Sprints** — sie sind damit die Pflicht-Nachschau
  der ersten Retrospektive dieses Laufs.

## Was dieser Lauf nicht anfassen darf

- **B-170** (P1, `ausformuliert`) — der verbliebene Kern ist genau die Schema-Entscheidung
  (`ExerciseCategory.CreatedByAdultId` ja/nein), die der Nutzer am 2026-08-14 ausdrücklich auf den Tag
  gelegt hat. `wartet_auf` benennt das. Der nachts baubare Teil ist in B-178 abgeflossen und **abgenommen**.
- **8 `Wunsch`/`Frage` unterhalb `geschaetzt`** — Freigabe 1/8 sperrt sie. Die gebrauchten Entscheidungen
  stehen unten unter „Offene Punkte für den Morgen".

## Anmerkungen des Menschen (pm-loop Step 2 — vor jedem eigenen Urteil)

`docs/anmerkungen/aktuell.md`, Stand 2026-08-14: **zwei Einträge, beide auf `erledigt`** (#1 Fach-Filter
in den Lehrwerken, #2 Band/Buchtyp gehören zum Lehrwerk statt zur Unit — beide mit Antwort und
Verlaufseintrag). **Keine offene Beobachtung**, also nichts, was in eine Rollen-Rückmeldung dieses Laufs
einzufalten wäre. Der Prüfpunkt ist genannt statt behauptet: gezählt wurden die Status-Zeilen der beiden
Überschriften, nicht der Dateikopf.

Kein laufender Server für `scope=all` — der Export ist der dokumentierte Rückfallweg, wenn keiner läuft.

## Die Messung, die den roten Faden trägt

Vor dem Sprint-Zuschnitt gemessen (`useAsync`-Aufrufstellen im gesamten Frontend, je Datei geprüft, ob der
zugehörige `.error` irgendwo in derselben Datei gerendert wird):

**64 von 105 rendern ihren Fehler — 41 nicht.**

Das beantwortet den offenen Punkt 2 von [B-174](backlog/B-174-arten-liste-verschweigt-ihren-fehler.md)
(„nur hier oder als Muster?") und den offenen Punkt 2 von
[B-162](backlog/B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) mit einer Zahl statt einer
Vermutung: Es **ist** ein Muster, und es ist mit 41 Stellen zu breit für einen Sprint. Der Sprint nimmt
darum die Stellen, an denen das Schweigen **tragend** ist (der Hauptinhalt einer Fläche), und legt den
Rest als eigene Story mit der gemessenen Zahl ab.

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel** (aus dem Sitz des Creators/Vaters, damit Step 6 es widerlegen kann):
*Nach jedem Speichern und jedem gescheiterten Laden sehe ich, was wirklich passiert ist — kein Erfolg
verschwindet ungesehen, kein Fehler tarnt sich als „leer" oder „lädt noch".*

**Umfang:** B-162, B-173, B-174, B-184, B-151 — fünf Stories, alle `art: Defekt`, alle `wo: frontend`,
alle mit derselben Wurzel: **ein Zustand trug zwei Bedeutungen, und die Oberfläche gab die falsche aus.**

**Draußen geblieben, mit Namen:**

- **B-183** („Ein gescheitertes Lesen ist ohne F5 nicht zu wiederholen") — thematisch der nächste Nachbar
  und ausdrücklich **gesperrt**: `art: Wunsch` auf `idee` (Freigabe 1/8). B-162 baut *einen* Knopf an
  *einer* Stelle, weil dort der Ausweg nachweislich versperrt ist; die allgemeine Infrastruktur bleibt
  eine Entscheidung des Nutzers. Siehe „Offene Punkte für den Morgen".
- **B-164** („frischer Zustand mit alten Daten") — dieselbe Datei wie B-174, aber eine andere Aussage mit
  eigener gemessener Breite. B-174s Aufnahme empfahl, beide zusammen zu bauen; das ist **nicht** übernommen
  worden, weil es B-174 an eine fremde Fälligkeit gebunden hätte.

### Refinement — der größere Teil des Sprints, getrennt ausgewiesen

Keine der fünf Stories war zu Beginn `geschaetzt`: vier standen auf `idee`, eine auf `ausformuliert`.
Die Kette `ausformuliert → gegrillt → geschaetzt` wurde für alle fünf gefahren (Freigabe 1 — `Defekt` ist
autonom grillbar), mit Ist-Ständen **am Code** statt aus den Aufnahmen. Das hat drei Behauptungen der
Aufnahmen **widerlegt**:

| Story | Was die Aufnahme behauptete | Was am Code steht |
|---|---|---|
| B-184 | Offen sei, ob die `useRef`-Begründung der vier Stellen noch gilt | **Gegenstandslos** — die zwei Bedingungen sind orthogonal, der Fix ist rein additiv |
| B-151 | Das Formular verliere seine Meldung | Nur **einer von drei** Meldewegen verliert sie; und „offen lassen" ist durch zwei Code-Kommentare ausdrücklich verboten |
| B-162 | Fundorte `:434`, `:479`, `:489` | Seit B-161/B-169 verrottet — tatsächlich `:561-563`, `:546`, `:561`. Der Befund selbst hielt |

## Entwickler-Brief (eine Ebene über den fünf Angriffsplänen)

**Der rote Faden:** Jede der fünf Stellen unterscheidet einen Zustand nicht, den das System **bereits
kennt**. `useAsync` weiß, ob ein Abruf gescheitert ist (`error`); die Seite weiß, ob ein Filter steht; die
Meldung weiß, dass sie ein Erfolg ist. Gebaut wird also nichts Neues — es wird **gelesen**, was schon da
ist.

**Reihenfolge (Abhängigkeit):** B-184 zuerst, weil `showPlaceholder` das Primitiv ist, das B-162 danach
benutzt. Dann B-174 (kleinste Fläche), dann B-162 (Baustein-Schnitt), dann B-151/B-173 (gemeinsamer
Meldungsplatz).

**Guards:** Keine Server-Berührung, keine Migration, kein Vertragsbruch — die fünf Stories fassen
ausschließlich Frontend-Zustände an.

**Testweg, und eine Korrektur an den eigenen Schätzungen:** B-174 und B-184 hatten geschätzt, ein E2E
scheide aus, weil ein scheiternder Abruf einen abgeschalteten Server bräuchte. Das ist **falsch** — und
das Repo wusste es schon: `optionsliste-faellt-aus.spec.ts` (aus B-172) hält im eigenen Kopfkommentar
fest, dass `page.route(…).abort()` genau eine Zeile kostet und „damit der teuerste Beleg der billigste
geworden ist". Beide Stories haben darum zusätzlich einen echten Browser-Beleg bekommen.

## Iteration 1 — umgesetzt

Kein Backend. Frontend, in der Reihenfolge des Briefs; Commit `e479d83`.

**Rote Probe je Story, mit Zahl (Auflage 5):**

| Story | Erwartet | Gemessen | Wogegen |
|---|---|---|---|
| B-184 | 1 von 3 rot | `1 failed \| 2 passed` | Feld zunächst mit der **heutigen**, falschen Bedingung implementiert — die Zusicherung fällt an einer Verhaltensaussage, nicht an einem fehlenden Symbol |
| B-174 | 2 von 3 neuen rot | `2 failed \| 17 passed` | Der dritte Fall prüft Abwesenheit und ist heute korrekt grün |
| B-162 | 3 von 5 rot | `3 failed \| 2 passed` | Baustein vorübergehend auf sein Vorher-Verhalten zurückgedreht; zwei Fälle sind Gegenproben |
| B-173 + B-151 | 4 E2E rot | `4 failed` | Quelländerungen gestasht, Specs gegen `HEAD` gefahren |
| B-162 (E2E) | 1 von 2 rot | `1 failed \| 1 passed` | Zweiter Lauf nötig: das Routen-Muster des ersten war falsch, der Fall fiel aus dem **falschen** Grund |

**Eine rote Probe hat dabei ihren eigenen Fehler aufgedeckt.** Die erste Fassung der beiden
Bestätigungs-E2E prüfte `Gespeichert.` **vor** dem Verschwinden der Zeile — `toBeVisible()` pollt und traf
das Banner in dem Frame, in dem es noch stand. Die Zusicherung hätte **vor wie nach** der Reparatur
gehalten: genau die Fehlerklasse, vor der `CLAUDE.md` warnt („Eine Zusicherung, die den Ausgangszustand
prüft, kann nie fehlschlagen"). Erst mit umgedrehter Reihenfolge — erst auf das Verschwinden warten, dann
die Meldung fordern — fiel sie.

**Verifikation:** `npx tsc -b` sauber · `npm run test` **326/326** in 38 Dateien · voller Playwright-Lauf
**42/42**.

## Review (Step 5) — zwei Runden `frontend-reviewer`

**Runde 1 — ein Blocker, und er war berechtigt.** Mein erster Entwurf spiegelte *jede* Meldung des
Formulars an den Seitenplatz. Damit wanderten auch „Der Name der Reihe fehlt." und „Nichts geändert."
ans Seitenende — Meldungen, die die Zeile nie verlassen. Ein Speichern mit leerem Namen sah am Formular
aus wie „nichts passiert". Der Reviewer hat das benannt, ich habe es nicht gesehen.

**Der vorgeschlagene Fix war ebenfalls falsch, und das hat eine Messung gezeigt, keine Überlegung.**
„Die Meldung lokal *und* oben zeigen" ließ „Gespeichert." doppelt auf einem Bildschirm stehen; daran
brachen **vier bestehende E2E** (`strict mode violation … resolved to 2 elements`, dreimal in
`lehrwerk-bearbeiten.spec.ts`, einmal in `optionsliste-faellt-aus.spec.ts`).

**Die tragende Fassung ist die dritte**, und der Reviewer hat sie in der Nachprüfung schärfer formuliert
als ich beim Bauen: **Die Meldung wandert genau dann, wenn ihr Träger sterben kann** — und nur der
Erfolgspfad ruft `onSaved()`/`onDone()`. Damit ist `useReportedMessage` wieder entfallen; es gibt **einen**
Mechanismus, wie Entscheidung 1 beider Stories es verlangt hatte.

**Runde 2 — kein Blocker mehr**, drei kleinere Funde, alle eingearbeitet (Commit `6688ff5`): eine übrig
gebliebene Zusicherung, die den Ausgangszustand prüft; ein nicht geräumter Meldungsplatz beim Aufklappen
einer Reihe; `{ exact: true }` an beiden „Gespeichert."-Zusicherungen.

## Runde 1 — Rollengang / Abnahme (Step 6)

Geführt als **maschineller Rollengang**: der volle Playwright-Lauf fährt echten Chromium gegen echten
Server mit Wegwerf-DB. Das *ist* der Rollengang, nur wiederholbar statt einmalig — und ausdrücklich kein
Ersatzbeleg. Freigabe 6 greift nicht (keine Browser-Verbindung in dieser Sitzung), der dokumentierte
E2E-Weg ist der stärkere.

**Ergebnis: 42/42 grün**, darunter vier neue Fälle, die vor der Reparatur nachweislich rot waren.

**Creator** (`/vater/katalog`, `/vater/lehrwerke`) — abgenommen:

> „Wenn die Arten nicht laden, sehe ich das jetzt. Vorher habe ich auf eine stumme Liste geschaut und
> hätte eine Art zum zweiten Mal angelegt — im geteilten Katalog, wo ich sie in einem Grundbestands-Fach
> nie wieder losgeworden wäre. Und wenn ich einer Reihe den Verlag wegnehme, während ich nach genau dem
> Verlag filtere, verschwindet sie zwar — aber ich sehe, dass es geklappt hat, statt zu glauben, ich
> hätte sie gelöscht."
> Belegt durch `ausfall-wird-genannt.spec.ts` (B-174) und `bestaetigung-ueberlebt.spec.ts` (B-173).

**Vater/Supervisor** (`/vater/wizard`, `/vater/kind/:id`) — abgenommen:

> „Der Assistent behauptet nicht mehr, mein Katalog sei leer, wenn in Wahrheit die Suche gescheitert ist —
> und, wichtiger: ich komme aus dem Zustand wieder raus. Vorher half nur F5, und das kostet den ganzen
> Assistenten. Beim Lehrbuch am Kind sehe ich endlich, dass gespeichert wurde."
> Belegt durch `ausfall-wird-genannt.spec.ts` (B-162, inkl. dem Knopf, der wirklich neu sucht) und
> `bestaetigung-ueberlebt.spec.ts` (B-151).

**Sohn/Student** — **Regressionszeuge, kein zweites Urteil.** Kein Pfad der Sohn-Arcade liegt im Diff
(die fünf Stories fassen ausschließlich `src/vater/` und `src/lib/` an); seine Specs (`full-flow`,
`uebung-abbruch`, `uebungstypen`, `shop-verlauf`, `perspektiven`) sind im selben Lauf grün. Mehr wird hier
nicht behauptet — O-Ton für eine Rolle zu erfinden, die die Änderung nie berührt hat, ist genau der
hohle Stempel, den Step 6 verhindern soll.

### Was ein Mensch noch ansehen muss (dritter Ausgang, nicht verschwiegen)

**Die Platzierung des Meldungsplatzes ist nicht maschinell beurteilbar.** Der Reviewer hat es benannt und
ausdrücklich *nicht* blockiert: Bleibt die Zeile stehen (der Normalfall in einer ungefilterten Liste),
liegt „Gespeichert." jetzt unter der ganzen Tabelle statt unter dem Knopf — bei zehn Reihen mit
aufgeklapptem Formular plausibel außerhalb des Sichtfelds. Ein E2E kann belegen, dass die Meldung **im
DOM** ist; ob sie **gesehen** wird, kann es nicht.

**Benannter Check für dich:** `/vater/lehrwerke` mit mehreren Reihen öffnen, eine mittlere bearbeiten und
speichern — ist die Bestätigung dort, wo du hinsiehst? Wenn nein, ist der billigste Weg der Vorschlag des
Reviewers: die reisende Meldung **benennen** (`Gespeichert: „Access 6"`). Das beantwortet zugleich die
Frage „welche der zehn Reihen?", wenn die Zeile verschwunden ist.

## Retrospektive (Sprint 1)

**Nachschau:** B-45 und B-172 (die Arbeit des vorigen Sprints, beide am 2026-09-01 abgenommen und danach
nie wieder angesehen). Beide gegen den **Code** nachgerechnet, nicht gegen die Testfarbe — Akzeptanzkriterium
für Akzeptanzkriterium, dazu gezielt die Fehlerfamilie „eine Bedingung, die zwei Situationen zusammenzieht".
`nachgeschaut: 2026-09-01` an beiden gesetzt. Index danach: **Nachgeschaut 109 von 113.**

**Fünf Defekte gefunden, vier davon in abgenommener Arbeit:**

| Neu | Was | `entgangen_bei` |
|---|---|---|
| [B-191](backlog/B-191-punktevorbelegung-traegt-den-wert-der-vorigen-uebung.md) | `pointsFrom(ex, current)` — `current` heißt „selbst getippt" **und** „von der vorher angeklickten Übung geerbt". Übung A (100) → Übung B (0) → Position B trägt 100 Münzen, die niemand gesetzt hat | B-45 |
| [B-192](backlog/B-192-obergrenze-schuetzt-nur-den-api-pfad.md) | Die Obergrenze (0, 200] greift nur bei `pointsGoalMet = null`; das Vater-Web schickt **immer** eine Zahl — der Vererbungszweig wird über die Oberfläche nie genommen | B-45 |
| [B-193](backlog/B-193-vertragsdoku-nennt-die-alte-null-bedeutung.md) | `StudyPlanDtos.cs:99` sagt in Swagger weiter „`null` = 20"; die sechs Geschwister darunter sagen es richtig | B-45 |
| [B-195](backlog/B-195-unit-liste-bleibt-waehrend-des-reihenwechsels-waehlbar.md) | `units.error ? [] : …` deckt den Fehlerfall, nicht das Ladefenster — während des Reihenwechsels bleiben die Units der alten Reihe **wählbar** | B-172 |
| [B-194](backlog/B-194-plandetail-kinderauswahl-ohne-rueckfall.md) | Der achte `<select>`-Fall hat die Regel von B-172 nicht geerbt | — (außerhalb des damaligen Umfangs) |

**Die Zahl steigt, und das ist die ehrliche Lesart:** Drei Entgleitungen aus **einer** abgenommenen Story
(B-45) sind viel. Bemerkenswert ist dabei nicht die Menge, sondern dass B-191 **exakt** das Fehlerprofil
trägt, das der Bereich seit dem 2026-08-05 dokumentiert — *eine Bedingung, die zwei Situationen
zusammenzieht*. Das Profil ist benannt, in einer `CLAUDE.md`-Regel verankert, von zwei Reviewern gesucht
worden — und ist trotzdem wieder durchgekommen.

### Was die Tore dieses Sprints durchgelassen haben

Ein Fund kam **von außerhalb der Schleife** und ist damit per Definition ein Retro-Eingang: Der
`frontend-reviewer` musste mir sagen, dass mein Meldungs-Umbau funktionierende Pfade zerstört. Kein Tor
hätte das gefangen — bis der **rote Test** kam: Erst die vier gebrochenen E2E haben entschieden, dass auch
der *vorgeschlagene* Fix falsch war. Die Messung hat zwei Meinungen geschlagen.

Der wertvollste Selbstfund ist unangenehmer: **Meine eigene rote Probe war zuerst zahnlos.** Die beiden
Bestätigungs-E2E prüften „Gespeichert." *vor* dem Verschwinden ihres Trägers; `toBeVisible()` pollt und
traf das Banner im letzten Frame. Sie hätten **vor wie nach** der Reparatur gehalten — also genau das, was
`CLAUDE.md` seit vier Messungen verbietet. Die rote Probe je Datei fängt das nicht: sie fragt, ob
*irgendein* Fall fällt, und drei andere fielen ja.

### Der vorgeschlagene Mechanismus — nur Vorschlag, nicht gelandet

Freigabe 3 verbietet diesem Lauf das Landen. Der Vorschlag steht darum hier und wartet auf deine
Entscheidung:

> **Eine E2E-Zusicherung über einen Zustand, der verschwinden kann, nennt die Reihenfolge.** Wo ein Test
> belegt, dass etwas ein Verschwinden **überlebt**, muss zuerst auf das Verschwinden gewartet werden
> (`toHaveCount(0)`) und **danach** die Überlebende gefordert. Andersherum pollt Playwright den Frame, in
> dem beide noch stehen, und die Zusicherung kann nicht fallen.
>
> **Ort:** `frontend/CLAUDE.md`, direkt bei „Eine Fläche je E2E-Spec". **Nicht** als Tor — die Regel ist
> syntaktisch nicht trennscharf von einer legitimen Reihenfolge, und ein Wächter, der raten muss,
> produziert Ausnahmelisten statt Sicherheit.
> **Kosten:** ~330 Bytes in einer Datei mit Kontextbudget. Zu bezahlen im selben Durchgang — dieser Lauf
> hat dort bereits 315 Bytes freigeräumt und die Datei netto **16 Bytes kleiner** hinterlassen als vorher,
> es gibt also Luft, aber sie ist nicht geschenkt.

**Ein zweiter Kandidat, ausdrücklich nicht empfohlen:** ein Wächter über `getByText` ohne `{ exact: true }`.
Er hätte B-189 gefangen — aber Teilstring ist meistens gewollt, und die Regel wäre nicht trennscharf. Der
Bereich hat mit „feste Untergrenzen verrotten" schon einmal bezahlt, was dabei herauskommt.

### Was bewusst ungedeckt bleibt

Dass eine Meldung **gesehen** wird, kann keine Maschine prüfen — nur, dass sie im DOM steht. Der dritte
Ausgang von Step 6 ist dafür da, und er ist mit einem benannten Check besetzt (siehe oben).

## Der Lauf endet hier — und zwar an einer Regel, nicht an einem Urteil

Freigabe 3 zieht eine Grenze: *„Ein Zähler je Sprint zählt jeden so gefundenen Fehler, dessen Behebung
Code oder Tests ändert; überschreitet er **5**, ist das keine Korrektur mehr, sondern eine Endlosschleife —
dann endet der GESAMTE Lauf sofort."*

**Gezählt, ehrlich statt günstig: neun.** Alle im Increment dieses Sprints, alle mit einer Änderung an Code
oder Tests:

| # | Fund | Quelle | Behebung fasste an |
|---|---|---|---|
| 1 | Meldung wanderte auch dort nach oben, wo die Zeile bleibt — Eingabefehler landeten am Seitenende | Reviewer R1 | Code |
| 2 | Der Übergang nach `null` wurde nicht gemeldet, der Seitenplatz veraltete | Reviewer R1 | Code |
| 3 | `CatalogAdmin.test.tsx` prüfte `role="alert"`, das es nirgends gab | Reviewer R1 | Test |
| 4 | Die E2E-Retry-Zusicherung war schon erfüllt, wenn eine Abfrage nur *startete* | Reviewer R1 | Test |
| 5 | Beide neuen Fehler-Banner waren für Screenreader stumm (SC 4.1.3) | Reviewer R1 | Code |
| 6 | Der Fix zu #1 zeigte die Meldung doppelt und brach **vier bestehende E2E** | roter Test | Code |
| 7 | Eine übrig gebliebene Zusicherung prüfte den Ausgangszustand | Reviewer R2 | Test |
| 8 | Der Seitenplatz wurde beim Aufklappen einer Reihe nicht geräumt | Reviewer R2 | Code |
| 9 | `getByText("Gespeichert.")` ohne `exact` — dieselbe Falle wie B-189 | Reviewer R2 | Test |

**Nicht gezählt, mit Grund:** die vier Funde außerhalb des Sprint-Diffs (B-187, B-188, B-189, B-190) und
die fünf der Nachschau (B-191…B-195) — Freigabe 3 nimmt „außerhalb des Diffs dieses Sprints" ausdrücklich
aus. Ebenso die Dokumentation der Identitäts-Anforderung an `useReportedMessage`: reiner Kommentar.

**Man könnte das kleiner rechnen** — #6 ist die Folge von #1, #2 fiel mit der Vereinheitlichung ohnehin
weg, und #3/#7/#9 sind Schwächen in *neuen* Tests statt Produktdefekte. Selbst bei der günstigsten Zählung
bleiben es sechs. Die Grenze ist mechanisch, und ich lege sie nicht zu meinen Gunsten aus: **die Nacht
endet nach Sprint 1.** Sprint 2 (B-187, B-188, B-190 — die direkten Nachkommen dieses Fadens) ist geplant,
zugeschnitten und **nicht** begonnen.

Zur Einordnung, ohne die Grenze wegzureden: Die Schwere fiel deutlich (R1 ein Blocker, R2 keiner), der
Sprint ist grün abgeschlossen, alles ist committet. Ob neun Funde „nicht konvergiert" heißen oder
„ein gründlicher Reviewer an einer heiklen Umstellung", ist deine Entscheidung am Morgen — die Regel sagt
nur, dass ich sie nicht allein treffe.

## Offene Punkte für den Morgen — die Entscheidungen, die ich gebraucht hätte

Freigabe 1/8 sperrt `Wunsch` und `Frage` unterhalb von `geschaetzt`. Notiert, **nicht** entschieden:

| Story | Die Entscheidung, die fehlt |
|---|---|
| [B-183](backlog/B-183-gescheitertes-lesen-ohne-erneuten-versuch.md) (`Wunsch`) | Der thematisch nächste Nachbar dieses Sprints. B-162 hat **einen** „Erneut suchen"-Knopf an **einer** Stelle gebaut, weil dort der Ausweg nachweislich versperrt war. Soll daraus eine Infrastruktur über alle Lesewege werden — und wenn ja, an `useAsync` oder je Fläche? Das ist Produktrichtung, nicht Reparatur. |
| [B-186](backlog/B-186-assistent-und-planseite-setzen-zielpunkte-verschieden.md) (`Frage`) | **Hängt jetzt an B-191/B-192.** Die Nachschau hat gezeigt, dass die Plan-Seite die serverseitige Vererbung nie auslöst — die Frage „setzen beide Wege die Zielpunkte gleich?" hat damit eine belegte Antwort: nein. Lohnt der Prüfauftrag noch, oder geht er in B-192 auf? |
| [B-152](backlog/B-152-schoolTypes-filter-und-attribut.md) (`Frage`) | `SchoolTypes` ist Filter und Attribut zugleich — vier Stellen leiten daraus drei Antworten ab. Ein Prüfauftrag, der in `verworfen` enden kann; das ist eine Wertentscheidung. |
| [B-122](backlog/B-122-top-level-listen-bekommen-paging.md) (`Wunsch`) | Sieben Top-Level-Sammlungen ohne `skip`/`take`. Neue Fähigkeit. |
| [B-140](backlog/B-140-freigabe-kennzeichen-in-der-uebungsauswahl.md) (`Wunsch`) | Kennzeichen für zurückgezogene Übungen in der Auswahl. |
| [B-90](backlog/B-90-server-sprachfeld.md) · [B-91](backlog/B-91-vater-web-extraktion-englisch.md) · [B-92](backlog/B-92-franzoesisch-zweite-zielsprache.md) (`Wunsch`) | Das Mehrsprachigkeits-Programm. Reihenfolge und Umfang sind Produktrichtung. |

**Dazu der eine P1, den keine Nacht anfassen darf:**
[B-170](backlog/B-170-selbst-angelegte-art-im-grundbestand-ist-unloeschbar.md) — der verbliebene Kern ist
die Schema-Entscheidung `ExerciseCategory.CreatedByAdultId` ja/nein. Du hast sie am 2026-08-14
ausdrücklich auf den Tag gelegt (die Kette wird dabei neu gefaltet, und der Snapshot-Diff *ist* die
Abnahme). Der nachts baubare Teil ist als B-178 abgeflossen und abgenommen.

## Offene Roadmap

Die dauerhafte Liste ist [docs/backlog/](backlog/README.md) — hier steht nur die **Begründung** der
aktuellen Reihenfolge, nicht die Reihenfolge selbst.

**Der nächste Sprint liegt fertig zugeschnitten da** und heißt *„ein Fehlschlag gibt sich nicht mehr als
Erfolg aus"* — er führt den Faden dieser Nacht von der Oberfläche ins Werkzeug:
[B-188](backlog/B-188-wizard-verschweigt-drei-weitere-ausfaelle.md) (P2, der `children`-Fall kostet ein
doppelt angelegtes Kind), [B-187](backlog/B-187-unitform-verliert-seine-bestaetigung.md) (der dritte Fall
von B-151) und [B-190](backlog/B-190-backlog-index-schreibt-leeren-index.md) (P2 — der Index-Generator
meldet „geschrieben", nachdem `awk` `fatal` gesagt hat, und löscht dabei die Übersicht über 64 offene
Stories; in dieser Nacht selbst ausgelöst).

**Danach** drängen sich die drei Nachschau-Funde zu B-45 auf — B-191 vor allem, weil er das Fehlerprofil
trägt, das der Bereich seit einem Monat namentlich sucht und trotzdem wieder durchgelassen hat.

## Konkreter Änderungsstand (für Review)

| Commit | Inhalt |
|---|---|
| `91785c5` | Fünf Stories durch `ausformuliert`/`gegrillt`/`geschaetzt`, Ist-Stände am Code belegt |
| `e479d83` | Sprint-Increment: B-162, B-173, B-174, B-184, B-151, B-189 |
| `6688ff5` | Nachprüfung des Reviewers eingearbeitet |

**Nichts ist gepusht** — das bleibt bei dir.
