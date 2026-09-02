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

## Runde 2 — Zweiter Nachtlauf

**Auftrag:** derselbe wie oben, plus eine neunte Auflage des Nutzers: Der Retro-Vorschlag der ersten Nacht
(E2E-Zusicherung über einen verschwindenden Zustand nennt die Reihenfolge) ist **noch nicht entschieden**
und wird **nicht gelandet** — nur in eigener Arbeit befolgt.

**Sprint-Zuschnitt** vom Nutzer nicht widersprochen: die drei Nachschau-Funde zu B-45 zuerst
(B-191, B-192, B-193), danach der Faden „ein Fehlschlag gibt sich nicht mehr als Erfolg aus".

### Ist-Stand: was ich selbst nachgeprüft habe, und was das änderte

Die drei Stories entstanden gestern Nacht aus dem Bericht eines Nachschau-Agenten. Ihre Fundorte habe ich
**jetzt erst selbst am Code gelesen** — und das war nicht überflüssig: Drei Zeilen unter der defekten
Stelle steht die Lösung bereits, und ein Kernstück der Analyse musste geschärft werden.

| Behauptung | Nachgeprüft an | Ergebnis |
|---|---|---|
| `pointsFrom` gibt `current` zurück | `PlanPositions.tsx:184-185` | **hält** — `ex && ex.rewardPoints > 0 ? ex.rewardPoints : current` |
| Der Wert leckt beim Übungswechsel | `:445-452` (`onChange` des Radios) | **hält** — `pointsGoalMet: pointsFrom(ex, cur.pointsGoalMet)` |
| Das UI schickt nie `null` | `:84` (`pointsGoalMet: number`), `:248` | **hält** — der Typ lässt „nicht gesagt" gar nicht zu |
| Die Grenze greift nur bei `null` | `PlanPositionsController.cs:98-99`, `:205` | **hält** |
| `StudyPlanDtos.cs` sagt „`null` = 20" | die `PointsGoalMet`-Zeile, gegen die vier Zeilen darunter | **hält** — der Kontrast steht direkt daneben |

**Zwei Funde, die aus dem Nachlesen selbst kamen:**

1. **B-191 ist ein verfehltes Akzeptanzkriterium, nicht nur ein Nebeneffekt.** B-45s eigenes AK 4 verlangt
   wörtlich: *„belegt das Punkte-Feld … mit dem `RewardPoints`-Wert der gewählten Übung vor (bzw. `20`,
   wenn die Übung `0` trägt)"*. Der Code setzt statt `20` aber `current` — also weder `RewardPoints` noch
   `20`, sondern was gerade dasteht. Das Kriterium war abgenommen, ohne erfüllt zu sein, und der Test
   konnte den Unterschied nicht messen.
2. **Die Antwort auf B-192s offenen Punkt 2 steht bereits im Code.** `PlanPositionsController.cs:85-88`
   entscheidet ausdrücklich: *„The bound applies **only** to inheritance, never to a value the supervisor
   states explicitly — that is their own decision and stays unbounded."* Damit ist der Punkt **nicht offen**
   und muss nicht beim Menschen abgeholt werden. Und er verschiebt die Diagnose: Der Defekt ist nicht die
   fehlende Grenze, sondern dass **die Oberfläche das Schweigen des Vaters in eine Aussage verwandelt**.

**Damit sind B-191 und B-192 ein Fix, nicht zwei** — beide brauchen denselben fehlenden Zustand:
„hat der Vater diese Zahl gesagt, oder kommt sie von woanders?"

### Sprint 2 — Ziel & Umfang

**Sprint-Ziel** (aus dem Sitz des Vaters):
*Die Punktezahl, die im Formular steht, ist die Punktezahl, die mein Kind bekommt — und sie gehört zu der
Übung, die ich gerade gewählt habe.*

**Umfang:** B-191, B-192, B-193 — drei `Defekt`-Stories, alle aus der Nachschau zu B-45, alle über
dieselbe Zahl.

**Draußen geblieben, mit Namen:** B-194 und B-195 (die beiden Nachschau-Funde zu **B-172**). Sie liegen
in derselben Nachschau, aber nicht auf diesem Faden — der eine ist ein `<select>` ohne Rückfall, der
andere ein Ladefenster. Sie in den Sprint zu ziehen hätte ihn zu einer Sammlung gemacht, die Step 6 nicht
mehr widerlegen kann.

### Entwickler-Brief (Sprint 2)

**Der rote Faden:** Eine Zahl, die an drei Stellen etwas anderes bedeutete. Der Server kennt eine Regel
(„was erbt eine Position?"), das Formular kennt eine andere („was zeige ich an?"), und das
Vertragsdokument beschrieb eine dritte. Gebaut wird kein neues Verhalten — es wird **eine** Wahrheit
hergestellt und veröffentlicht.

**Reihenfolge (Abhängigkeit, API-First):**

1. **B-192 Backend zuerst.** Die Regel bekommt einen Ort (`Services/Shared/GoalPointsPolicy.cs`), und der
   Katalog veröffentlicht ihr Ergebnis als `ExerciseSummary.SuggestedGoalPoints`.
2. **B-193** danach — die Vertragszeile kann erst stimmen, wenn feststeht, was gilt.
3. **B-191 Frontend** zuletzt: Es liest das neue Feld und bekommt den fehlenden Zustand „hat der Vater
   das gesagt?".

**Guards:** Keine Schemaänderung, keine Migration. Ownership und Rollen unberührt — `ExerciseSummary`
kommt aus einer `Roles.Creator`-Action, die Position aus `Roles.Supervisor` mit `PlanOwnershipFilter`.
**`vertragsbruch: ja`**: Ein positioneller Record wächst; `docs/openapi/v1.json` und `src/lib/contract.ts`
werden neu erzeugt.

**Testweg:** drei Ebenen, weil der Defekt über drei lief — Integrationstest (der Server nennt den
wirksamen Wert und legt genau ihn an), Regeltest (das Formular rechnet **nicht** selbst nach) und ein
E2E über **zwei** Übungen (nur die Verkettung kann das Leck messen).

### Iteration 2 — umgesetzt

**Backend:** `GoalPointsPolicy` als gemeinsamer Ort für Grenze (200) und Rückfall (20);
`PlanPositionsController` benutzt sie statt einer eigenen Kopie; `ExerciseCatalogController` veröffentlicht
den wirksamen Wert. Die Projektion rechnet als Ternär **inline** — ein Methodenaufruf wäre im
EF-`.Select(...)` nicht übersetzbar; dupliziert wird damit die *Form* der Regel, nicht ihre *Zahlen*.

**Vertrag:** `ExerciseSummary` um `SuggestedGoalPoints` erweitert, `PointsGoalMet`-Beschreibung korrigiert.
Die neue Beschreibung ist bewusst **einzeilig** — mehrzeilig hätte sie ihre Quelltext-Einrückung ins
OpenAPI-Dokument getragen und damit eine weitere Instanz des bekannten Defekts B-147 erzeugt.

**Frontend:** `pointsFrom(ex, current, getippt)` liest `suggestedGoalPoints`; der Zustand `punkteGetippt`
liegt im **Anlege**-Formular und nicht in `PositionSettings` — er darf `settingsToDto` nicht erreichen.
Die Unterscheidung hängt an dem, was ohnehin verschieden ist: Der Übungswechsel setzt `settings` direkt,
jede Formularänderung läuft durch `PositionFields.onChange`.

#### Rote Probe je Ebene, mit Zahl (Auflage 5)

| Ebene | Erwartet | Gemessen | Was die Zahl sagt |
|---|---|---|---|
| Backend | 2 rot | `Failed: 2, Passed: 0` | Test 2 wörtlich: `Expected 100000, Actual 20` — der Katalog nennt 100000, die angelegte Position trägt 20 |
| Frontend-Regel | 2 rot von 21 | `2 failed \| 19 passed` | `expected 100 to be 20` — der Wert der vorigen Übung, durchgeleckt |
| E2E | 1 rot von 2 | `1 failed \| 1 passed` | `Expected "20", Received "100000"` — der Vater sieht die Zahl, die beim Anlegen eine andere wird |

**Zwei Dinge liefen dabei schief, und beide gehören ins Protokoll:**

1. **Mein erster Frontend-Testfall war grün, ohne etwas zu messen.** Die Verkettung „Übung A, dann Übung
   B" fiel nicht — weil mein Fixture Übung B eine *rohe* 20 gab statt einer *rohen 0*. Mit einer rohen 20
   liefert auch die alte Logik 20, und der Fall wäre vor wie nach der Reparatur grün gewesen. Erst die
   rote Probe hat das entlarvt; ohne sie stünde jetzt ein Test da, der seinen eigenen Namen nicht hält.
2. **Zwei bestehende Tests fielen, weil ihre Regel umgezogen ist.** „Empfehlung 0" und „negative
   Empfehlung" prüften, dass das *Formular* unbrauchbare Werte abfängt — das tut seit B-192 der Server.
   Die Fälle sind **umgeschrieben statt gestrichen**, und die Abdeckung ist **mitgezogen**: Der negative
   Fall war bis dahin *nur* im Frontend gepinnt und steht jetzt im Integrationstest. Was der
   Frontend-Test heute hält, ist die andere Hälfte derselben Entscheidung — dass hier **nicht**
   nachgerechnet wird.

**Nebenbefund, nicht mitgebaut:** B-193s Akzeptanzkriterium 3 verlangte eine Zählung statt einer
Vermutung. Gezählt: **24** Vorgabewert-Aussagen in `StudyPlanDtos.cs`. Die **acht mit einer konkreten
Zahl** habe ich einzeln gegen den Code geprüft (`MaxBox ?? 5`, `PenaltyCoins ?? 0`, `NewContentPoints ??
sb ?? 10`, `ComboThreshold ?? sb ?? 5`, `ComboBonusPoints ?? sb ?? 5`, `SpeedThresholdSeconds ?? sb ?? 0`,
`SpeedBonusPoints ?? sb ?? 0`, `GoalThreshold ?? DefaultPassPercent = 80` in
`PositionProgressService.cs:17`). **Nur `PointsGoalMet` war falsch.** Die übrigen 16 tragen keine Zahl und
können nicht numerisch driften.

**Verifikation:** `dotnet build Pugling.sln` grün · `dotnet test -c Release` **838/838** ·
`npx tsc -b` sauber · `npm run test` **327/327** · voller Playwright-Lauf **43/43**.

### Review (Step 5, Sprint 2) — beide Reviewer, ein Blocker

**Backend (`pugling-reviewer`): kein Blocker** — und ein Fund, der meine Begründung **gemessen widerlegt
hat.** Ich hatte die Regel als Ternär in die EF-Projektion geschrieben und das damit begründet, dass ein
Methodenaufruf dort nicht übersetzbar sei. Der Reviewer hat das auf einer Kopie des Baums nachgemessen:
EF Core 10 erlaubt den Aufruf in der **finalen** Projektion. Damit war die Duplikation, die B-192 gerade
abschaffen wollte, von der Reparatur selbst wieder eingeführt worden — mit einer falschen Begründung
daneben, die sich festgesetzt hätte. Behoben: der Code ruft `GoalPointsPolicy.Effective` jetzt direkt.

Dazu eine **Test-Blindstelle, per Defekt-Injektion belegt**: Der Abgleich-Test fuhr nur den Rückfall-Zweig
(beide Seiten 20 — zugleich der Entity-Vorgabewert), eine Fassung ohne jede Vererbung blieb also grün.
Mit dem ergänzten plausiblen Paar fällt sie; selbst nachgeprüft (`Expected 35, Actual 20`).

**Frontend (`frontend-reviewer`): ein Blocker, und er trifft den Kern.** `punkteGetippt` war in **keinem**
Testlauf jemals `true` — kein Test tippte je in das Punkte-Feld. Man konnte das Flag durch das Literal
`false` ersetzen, und 327/327 plus beide E2E blieben grün. Der Zustand, um den die ganze Story geht, lag
außerhalb jeder Zusicherung. Die rote Probe ist nachgeholt: `Expected "77", Received "60"`.

Derselbe Reviewer fand den zweiten: Das Flag überlebte das **Hinzufügen** einer Position — ab da belegte
keine weitere Übung mehr vor, Position 2..n trugen die Zahl von Position 1. Das war ein Rückschritt
gegenüber dem Stand *vor* dieser Story und widersprach der ausgelieferten Feldhilfe.

### Runde 2 — Rollengang / Abnahme (Step 6)

Maschinell geführt, voller Playwright-Lauf: **44/44 grün.**

**Vater/Supervisor** (`/vater/plan/…`) — abgenommen:

> „Die Zahl im Feld ist jetzt die Zahl, die mein Kind bekommt. Vorher stand da 100000 und angelegt wurden
> 20 — ich hätte das nie gemerkt, weil beides plausibel aussieht. Und wenn ich selbst etwas eintippe,
> überlebt es den Übungswechsel; sobald die Position steht, fängt die nächste wieder bei der Empfehlung
> an."
> Belegt durch `punkte-empfehlung.spec.ts` (drei Fälle) und `PlanPositionCrudTests` (zwei neue).

**Creator** — **Regressionszeuge, kein zweites Urteil.** Seine Fläche ändert sich in einem Punkt: Die
Katalog-Trefferzeile trägt ein Feld mehr. Kein Creator-Weg im Diff, seine Specs
(`creator-lehrwerk-weg`, `uebungstypen`, `freigabe`, `lehrwerke`) im selben Lauf grün.

**Sohn/Student** — **Regressionszeuge.** Kein Pfad der Arcade liegt im Diff; `full-flow`,
`uebung-abbruch`, `shop-verlauf` grün. Mehr wird nicht behauptet.

**Was ein Mensch noch ansehen muss:** nichts Sinnlich-Visuelles in diesem Sprint. Die einzige offene
Frage ist eine **Produktentscheidung**, keine Wahrnehmung: ob eine ausdrücklich getippte Zahl über 200
weiter unbegrenzt bleiben soll (der Code entscheidet das heute mit Begründung; dieser Lauf hat es nicht
angetastet).

## Retrospektive (Sprint 2)

**Nachschau:** die fünf Stories aus Sprint 1 (B-162, B-173, B-174, B-184, B-151) — gegen den **Code** und
Akzeptanzkriterium für Akzeptanzkriterium nachgerechnet, nicht gegen die Testfarbe. `nachgeschaut:
2026-09-02` an allen fünf. Index danach: **114 von 121.**

**Sechs Defekte gefunden, alle in Arbeit, die zwölf Stunden vorher abgenommen wurde:**

| Neu | Was | Gewicht |
|---|---|---|
| [B-196](backlog/B-196-loeschen-meldet-nichts-und-raeumt-nichts.md) | **B-173s Akzeptanzkriterium 2 ist nicht gebaut.** Das Löschen einer Reihe meldet nichts — und lässt die Bestätigung der vorigen Handlung stehen, wo sie als Löschbestätigung gelesen wird | **schwer** |
| [B-197](backlog/B-197-meldungsplatz-wird-beim-absenden-nicht-geraeumt.md) | Der Meldungsplatz wird beim **Absenden** nicht geräumt: grünes „Gespeichert." steht über rotem Fehler — **die Reparatur hat eine neue Instanz genau des Defekts erzeugt, gegen den sie gebaut wurde** | **schwer** |
| [B-198](backlog/B-198-teilstring-falle-im-selben-commit-neu-gebaut.md) | Die Teilstring-Falle aus B-189 wurde **im selben Commit** drei Dateien weiter neu gebaut — und die Nachbesserung zog zwei Zeilen nach und übersah die dritte | mittel |
| [B-199](backlog/B-199-drei-behauptungen-die-ihr-eigener-sprint-aufgehoben-hat.md) | Drei Behauptungen, deren Prämisse derselbe Sprint aufgehoben hat — darunter ein Kommentar, der eine **Freigabe** ausspricht, die B-162 zurückgenommen hat | mittel |
| (in B-147 nachgetragen) | Einrückung und Absatztrennung im Vertragsdokument sind **dasselbe** — man kann nur eines von beiden haben, gemessen | klein |

**Die Zahl steigt auf 27 von 114, und die ehrliche Lesart ist unangenehm:** Zwei der sechs Funde sind
keine übersehenen Randfälle, sondern **Selbstwidersprüche des Sprints** — ein nicht gebautes
Akzeptanzkriterium und eine Reparatur, die ihren eigenen Fehler neu erzeugt. Beide sind durch Review,
838/838, 327/327 und einen 43/43-Rollengang gekommen.

### Was die Tore durchgelassen haben

**Der Rollengang hat B-196 nicht gefangen, weil keine Spec eine Reihe löscht.** Das ist kein Zufall: Ein
maschineller Rollengang fährt die Wege, die jemand geschrieben hat — und geschrieben wird, was man im
Blick hatte. Er ist damit stark gegen Regression und **blind gegen ein Akzeptanzkriterium, an das beim
Schreiben der Spec niemand dachte**. Genau das unterscheidet ihn von einem Menschen, der die Seite ansieht.

**Und der zweite Befund ist schärfer:** B-173s AK 2 stand in der Story, wurde beim Bauen nicht umgesetzt,
und die Abnahme hat es nicht bemerkt — obwohl `abgenommen` eine belegte Verifikation verlangt. Belegt
wurde, dass *die Suite* grün ist, nicht dass *jedes Kriterium* erfüllt ist. Dieselbe Lücke hat B-174s
Entscheidung 3 stillschweigend fallen lassen.

### Der vorgeschlagene Mechanismus — nur Vorschlag, nicht gelandet

Freigabe 3 verbietet diesem Lauf das Landen. Der Vorschlag:

> **Jedes Akzeptanzkriterium bekommt vor `abgenommen` einen benannten Beleg** — den Namen des Tests, der
> Spec oder des Rollengang-Schritts, der es prüft. Wo keiner existiert, steht *„nicht geprüft, weil …"*.
> Eine Liste von Kriterien und daneben eine Testzahl ist **kein** Beleg: Sie sagt, dass die Suite grün
> ist, nicht dass sie dieses Kriterium anfasst. Genau daran sind heute B-173 AK 2 und B-174 Entscheidung 3
> vorbeigekommen.
>
> **Ort:** `docs/backlog/README.md`, Eintrittsbedingung von `abgenommen`. **Nicht als Tor** — ein
> Akzeptanzkriterium ist Freitext, und es maschinell einem Test zuzuordnen hieße, es zu erraten. Was
> mechanisch prüfbar wäre, ist nur die *Anwesenheit* der Zuordnung, und das ist ein Formular, das man
> ausfüllt statt es zu meinen.
> **Kosten:** Die Abnahme wird länger, und der Verfahrenstext des Backlog-README wächst — er steht bereits
> 114 Bytes über Budget. Zu bezahlen im selben Durchgang; der erste Kandidat wäre die Erzählung der
> 14er-Runde vom 2026-08-05, deren Begründung schon im Protokoll steht.

**Ein zweiter Kandidat, ausdrücklich verworfen:** ein Wächter über „boolescher Zustand, den kein Test je
auf `true` bringt" — er hätte den Blocker dieses Sprints gefangen. Verworfen, weil er nicht trennscharf
ist: Ein Flag, das nur in einem seltenen Produktpfad wahr wird, ist legitim, und der Wächter bräuchte
sofort eine Ausnahmeliste. Der Bereich hat mit „feste Untergrenzen verrotten" schon bezahlt, was dabei
herauskommt.

### Was bewusst ungedeckt bleibt

Dass ein maschineller Rollengang nur die Wege fährt, die jemand geschrieben hat. Dagegen hilft kein
Mechanismus — nur ein Mensch, der die Seite ansieht, oder ein Kriterium, das seinen Beleg benennen muss
(siehe Vorschlag).

## Der zweite Lauf endet hier — aus zwei Gründen, und der zweite wiegt schwerer

**Grund 1, der Fund-Zähler.** Freigabe 3 setzt die Grenze bei fünf Funden im eigenen Increment, deren
Behebung Code oder Tests ändert. Gezählt für Sprint 2:

| # | Fund | Quelle | Behebung fasste an |
|---|---|---|---|
| 1 | `punkteGetippt` war in keinem Lauf je `true` — das Flag durch `false` zu ersetzen ließ alles grün | Reviewer FE | Test |
| 2 | Das Flag überlebte das Hinzufügen; Position 2..n verloren ihre Vorbelegung | Reviewer FE | Code |
| 3 | Die EF-Begründung war falsch (gemessen) — die Regel stand unnötig zweimal da | Reviewer BE | Code |
| 4 | Der neue `<para>` trägt Einrückung bzw. verliert die Satzgrenze im Vertragsdokument | Reviewer BE | Doku am Vertrag |
| 5 | Test-Blindstelle: eine Fassung ohne jede Vererbung blieb grün | Reviewer BE | Test |
| (6) | `take=200` + `.Single` verwettet eine unausgesprochene Obergrenze | Reviewer BE | Test — **Härtung, kein Defekt** |

Fünf Defekte, dazu eine Härtung. Bei strenger Zählung sechs. Ich gebe beide Lesarten an, statt mir die
günstigere auszusuchen — aber sie entscheidet hier ohnehin nichts, denn:

**Grund 2, und er ist der eigentliche: Sprint 1s Increment hat sich als defekthaltig erwiesen.** Genau
das ist die zweite Grenze aus [docs/backlog/README.md](backlog/README.md) („Der Backlog-Lauf", Limit 2) —
und sie hängt an keiner Zählung. Die Nachschau hat gefunden:

- **B-196** — ein Akzeptanzkriterium, das in der Story steht, nicht gebaut wurde und trotzdem
  `abgenommen` bekam.
- **B-197** — eine Reparatur, die eine **neue Instanz genau des Defekts** erzeugt, gegen den sie gebaut
  wurde: grünes „Gespeichert." über rotem Fehler.

Beides ist durch zwei Reviewer, 838/838, 327/327 und einen 43/43-Rollengang gekommen. Weiterzulaufen wäre
die falsche Reaktion — die Qualitätsschwelle ist gerutscht, und das Verfahren sagt genau dafür: anhalten,
damit ein Mensch es sieht.

**Nicht angefangen** ist darum der geplante dritte Sprint (B-188, B-187, B-190). Er bleibt zugeschnitten.

## Offene Punkte für den Morgen

**Zwei Retro-Vorschläge warten jetzt auf dich** — der von letzter Nacht ist noch unentschieden, und dieser
Lauf hat ihn absprachegemäß nicht gelandet:

1. **Aus Nacht 1:** Eine E2E-Zusicherung über einen Zustand, der verschwinden kann, nennt die Reihenfolge
   (erst auf das Verschwinden warten, dann die Überlebende fordern). Ort: `frontend/CLAUDE.md`.
2. **Aus Nacht 2:** Jedes Akzeptanzkriterium bekommt vor `abgenommen` einen **benannten Beleg**. Ort:
   `docs/backlog/README.md`. Dieser hätte B-196 und B-174s stillschweigend entfallene Entscheidung 3
   gefangen — beide Male ist eine Testzahl als Beleg für ein Kriterium durchgegangen, das sie nicht anfasst.

**Gesperrt geblieben (`Wunsch`/`Frage` unterhalb `geschaetzt`)** — unverändert die Liste aus Nacht 1,
dazu neu:

- **B-186** hat durch diesen Lauf eine belegte Antwort bekommen: Der Assistent (`VaterWizard`) schickt
  immer seine Intensitäts-Zahl und erbt darum **nie** — B-45 und B-192 wirken dort nicht. Die Frage ist
  jetzt nicht mehr „setzen beide Wege gleich?", sondern „soll der Assistent überhaupt erben?". Das ist
  Produktrichtung.
- **B-199 Punkt 3** (B-174s Entscheidung 3 nachbauen oder zurücknehmen) ist eine Entscheidung über einen
  sichtbaren Zustand — sie gehört in den Dialog.

## Offene Roadmap (Stand nach Nacht 2)

Die dauerhafte Liste ist [docs/backlog/](backlog/README.md); hier steht nur die Begründung der Reihenfolge.

**Das Dringlichste sind jetzt B-196 und B-197** — nicht wegen ihrer Größe, sondern weil sie in Arbeit
sitzen, die als fertig gilt, und weil B-197 die Fläche betrifft, auf der gerade zwei Meldungen
gleichzeitig etwas Widersprüchliches sagen können. Danach der zugeschnittene dritte Sprint
(B-188, B-187, B-190).

## Konkreter Änderungsstand (beide Nächte)

| Commit | Inhalt |
|---|---|
| `91785c5` · `e479d83` · `6688ff5` · `58cca00` | Nacht 1: Refinement, Sprint-1-Increment, Reviewer-Nachprüfung, ein Kommentar |
| `26c387c` | Nacht 1: Abnahme + Nachschau zu B-45/B-172 |
| `c3cdec3` | Nacht 2: Sprint-2-Increment |
| (dieser) | Nacht 2: Reviewer-Funde, Abnahme, Nachschau zu Sprint 1 |

**Nichts ist gepusht** — das bleibt bei dir.

## Runde 3 — Dritter Sprint (nach ausdrücklicher Fortsetzung durch den Nutzer)

Der zweite Lauf endete an Limit 2 („Sprint 1s Increment hat sich als defekthaltig erwiesen"). Genau dafür
ist die Grenze da: Ein Mensch sieht es und entscheidet. Der Nutzer hat fortgesetzt — dieser Sprint schließt
darum zuerst die Selbstwidersprüche, statt Neues zu bauen.

### Sprint 3 — Ziel & Umfang

**Sprint-Ziel** (aus dem Sitz des Creators/Vaters):
*Was am Meldungsplatz steht, gehört zu der Handlung, die ich gerade getan habe — und wenn eine Handlung
nichts meldet, steht dort auch nichts Altes.*

**Umfang:** B-197, B-196, B-198.

**Draußen geblieben, mit Namen:** **B-199** (drei Behauptungen, deren Prämisse ihr eigener Sprint
aufgehoben hat). Sie ist Sprint-1-Schuld wie die drei anderen, dient dem Ziel aber nicht — sie handelt von
veralteten Begründungen, nicht vom Meldungsplatz. Mitzunehmen hätte den Sprint zu einer Sammlung gemacht,
die Step 6 nicht widerlegen kann.

### Iteration 3 — umgesetzt

`onOeffnen` und `onMessage` sind **ein** Prop `onMeldung(m | null)`; geräumt wird beim Öffnen, **zu Beginn
jeder Handlung** und beim Erfolg. Das Löschen einer Reihe meldet und nennt den Namen — die Zeile, auf die
sich die Meldung bezieht, ist ja gerade verschwunden.

| Rote Probe | Erwartet | Gemessen |
|---|---|---|
| B-197 (E2E) | rot | `Expected: 0, Received: 1` — das grüne „Gespeichert." stand nachweislich neben der roten Fehlermeldung |
| B-196 (E2E, **gezielt**) | rot | `getByText('Reihe „…" gelöscht.')` — element not found |

**Warum B-196 eine eigene Gegenprobe brauchte:** Im gemeinsamen Fall bricht der Lauf schon an B-197s
Zusicherung ab. „Der Lauf war rot" wäre für diese Story kein Beleg gewesen — nur dafür, dass *irgendetwas*
fiel. Das ist dieselbe Unterscheidung, die `CLAUDE.md` für die rote Probe je Datei macht.

**Ein Testfehler von mir gehört ins Protokoll:** Schritt 3 des neuen E2E füllte den bereits gespeicherten
Namen zurück — es gab nichts zu speichern, und das Formular meldete „Nichts geändert.". Kein Codefehler,
ein Denkfehler im Testaufbau; Schritt 1 und 2 waren durchgelaufen.

### Review (Step 5) — ein Blocker, und er ist der lehrreichste der drei Nächte

**Der Ist-Stand von B-197 nennt drei kleinere Ausprägungen der Lücke. Ich habe zwei gebaut und die dritte
kommentarlos weggelassen.** Die Unit-Ziehharmonika — und das Unit-Formular sitzt als aufgeklappte Zeile
*in* derselben Tabelle, der Meldungsplatz direkt darunter: rot und grün wären benachbart gewesen, nicht
seitenweit getrennt.

Im selben Sprint macht **B-196 es richtig vor**: Dort steht der Ausschluss von `ChildMaterialSection` als
Entscheidung 3, mit Begründung und Kosten. Der Unterschied zwischen *entschieden* und *vergessen* ist
genau das, was dieser Sprint reparieren sollte — und er ist mir in derselben Stunde unterlaufen.

**Zwei weitere Funde widerlegen Behauptungen, die ich selbst aufgeschrieben hatte:**

| Meine Behauptung | Was der Reviewer belegt hat |
|---|---|
| „Ein Prop mit `null` als Wert kann man nicht halb verdrahten" (B-197, Entscheidung 2) | Ein `?`-Prop kann man **ganz weglassen** — und genau so wäre die nächste Stelle entstanden. `onMeldung` ist jetzt **Pflicht**, wo nichts auf seine Abwesenheit verzweigt; Preis **gezahlt statt aufgeschrieben**: 18 Aufrufstellen in Komponententests |
| „es sind **zwei** stumme Erfolgspfade" (B-196, Entscheidung 3, als gemessen ausgewiesen) | Es sind **vier**. Die Entscheidung trägt weiter, die Zahl trug nicht — korrigiert, die drei übrigen als [B-200](backlog/B-200-drei-stumme-erfolgspfade.md) |
| B-198s Beleg-Tabelle versprach eine Zählung im `## Verlauf` | Die stand dort nicht. Jetzt: **118** `getByText`-Aufrufe, davon **29** ohne `exact`; sieben echte Fälle behoben |

**Und ein Gewinn, den niemand gesucht hat** (Fund des Reviewers, SC 4.1.3): Vor der Änderung setzte ein
zweites erfolgreiches Speichern denselben String „Gespeichert." — keine DOM-Änderung in der
`aria-live`-Region, also **keine Ansage**. Das Räumen beim Absenden leert die Region zwischendurch, und
die Wiederholung wird angesagt.

### Runde 3 — Rollengang / Abnahme (Step 6)

Maschinell, voller Playwright-Lauf: **45/45 grün.**

**Creator** (`/vater/lehrwerke`) — abgenommen:

> „Wenn ich eine Reihe lösche, sagt mir das jetzt jemand — und zwar mit Namen, was wichtig ist, weil die
> Zeile ja weg ist. Vorher stand da noch die Bestätigung von vorhin, und ich habe sie für die
> Löschbestätigung gehalten. Und wenn mein zweiter Speicherversuch scheitert, steht oben nicht mehr
> gleichzeitig, dass alles geklappt hat."
> Belegt durch `bestaetigung-ueberlebt.spec.ts`, Fall „der Meldungsplatz gehoert zur letzten Handlung".

**Vater/Supervisor** und **Sohn/Student** — **Regressionszeugen.** Kein Pfad im Diff berührt ihre Flächen
(die Änderung liegt in `src/vater/` an drei Lehrwerks-/Material-Seiten); ihre Specs sind im selben Lauf
grün. Mehr wird nicht behauptet.

**Was ein Mensch noch ansehen muss:** nichts Sinnlich-Visuelles. Der einzige offene Punkt ist eine
**Entscheidung**, keine Wahrnehmung — wie viele Meldungsorte eine Seite haben darf (`NewSeries` und
`PublisherAdmin` tragen eigene Banner neben dem Seitenplatz; nach „Reihe löschen → Reihe anlegen" stehen
beide Meldungen nebeneinander). Das ist im `## Verlauf` von B-197 als bewusst offen benannt.

## Retrospektive (Sprint 3)

**Nachschau:** die drei Stories aus Sprint 2 (B-191, B-192, B-193) — Akzeptanzkriterium für
Akzeptanzkriterium gegen den Code, dazu die vier riskantesten Flächen einzeln durchgerechnet (der
`punkteGetippt`-Wächter auf Falsch-Positiv *und* Falsch-Negativ, die Fehlerpfade des Resets, die
Client-Evaluation in der EF-Projektion gegen Sortierung/Paging/`X-Total-Count`, und ob `ExerciseDetail`
den Wert braucht). `nachgeschaut: 2026-09-02` an allen dreien. Index: **117 von 124.**

**Zum ersten Mal in drei Nächten: kein funktionaler Defekt in der abgenommenen Arbeit.** Die Vererbung,
der Zustand und die Vertragszeile halten, was ihre Kriterien verlangen — inklusive der beiden
Reviewer-Nachbesserungen.

**Zwei Textwidersprüche sind trotzdem durchgekommen, und einer davon ist bekannt:**

| Fund | Was |
|---|---|
| B-191, AK 2 | „Eine selbst getippte Zahl überlebt einen Übungswechsel unverändert." Seit dem Reset gilt das nur noch **innerhalb** einer Position. Der Code ist richtig, der E2E-Titel trug die Korrektur sogar — nur das Kriterium wurde nie nachgezogen. **Korrigiert**, kein `entgangen_bei` (kein Produktdefekt, eine falsche Zeile) |
| [B-201](backlog/B-201-feldhilfe-argumentiert-in-beide-richtungen.md) | Die Feldhilfe sagt „änderst du sie, bleibt die Änderung" — die Hälfte, die der Reset unwahr gemacht hat. Dazu wird derselbe Text im Assistenten gerendert, wo seine erste Hälfte **nie** zutraf |

### Was die Tore durchgelassen haben

**Dieselbe Klasse wie in Nacht 2, nur in die andere Richtung.** Dort stand ein Akzeptanzkriterium in der
Story und war nicht gebaut (B-173, AK 2). Hier war es gebaut und die Story sagte etwas anderes (B-191,
AK 2). Beide Male hat die Abnahme **Story und Code nicht abgeglichen** — belegt wurde, dass die Suite grün
ist, nicht dass sie dieses Kriterium anfasst.

Das ist die zweite unabhängige Bestätigung für den Retro-Vorschlag aus Nacht 2. Er hätte **beide** Fälle
gefangen: Wer den Beleg für AK 2 benennen muss, liest den E2E-Titel („bis die Position steht") und merkt,
dass er etwas Engeres beweist als das Kriterium behauptet.

### Der vorgeschlagene Mechanismus — **derselbe wie in Nacht 2, nicht ein dritter**

Freigabe 3 verbietet das Landen. Statt einen neuen Mechanismus zu erfinden, ist die ehrliche Auskunft
dieser Retro: **Der Vorschlag aus Nacht 2 ist der richtige, und er hat jetzt zwei unabhängige Belege.**

> Jedes Akzeptanzkriterium bekommt vor `abgenommen` einen **benannten Beleg** — Testname, Spec-Fall oder
> Rollengang-Schritt. Wo keiner existiert, steht *„nicht geprüft, weil …"*.
>
> **Belege:** B-173 AK 2 (in der Story, nie gebaut, trotzdem abgenommen) und B-191 AK 2 (gebaut, aber die
> Story sagte etwas anderes). Zwei Sprints, zwei Richtungen, dieselbe Lücke.
> **Ort:** `docs/backlog/README.md`, Eintrittsbedingung von `abgenommen`. **Kein Tor** — ein
> Akzeptanzkriterium ist Freitext.
> **Kosten:** Die Abnahme wird länger; der Verfahrenstext wächst und steht schon über Budget.

**In der Praxis habe ich ihn in diesem Sprint befolgt**, ohne ihn zu landen: B-196, B-197 und B-198 tragen
je eine Beleg-Tabelle, und B-196s AK 3 steht ausdrücklich als *„nicht geprüft, weil …"* da statt als
behaupteter Beleg. Das ist der Testlauf des Vorschlags gegen sich selbst — und er hat sofort etwas
geleistet: Die fehlende Zählung in B-198 fiel dem Reviewer nur auf, **weil** die Beleg-Tabelle sie
versprach.

### Was bewusst ungedeckt bleibt

Dass ein maschineller Rollengang nur die Wege fährt, die jemand geschrieben hat. Dieser Sprint ist selbst
der Beweis: B-196 existiert, weil keine Spec je eine Reihe gelöscht hat.
