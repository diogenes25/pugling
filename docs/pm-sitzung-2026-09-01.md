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
