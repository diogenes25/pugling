---
tags: [typ/story, status/abgenommen, bereich/doku, bereich/ki-agent, rolle/creator]
aliases: [Nachschau Sprint 5, Hilfe nennt das gewählte Profil]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (dritter Lauf, Retrospektive Sprint 6 – Nachschau zu Sprint 5)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-237, B-244, B-246]
nachgeschaut: 2026-10-23
wartet_auf: ""
---

# B-256 · Drei Sätze aus Sprint 5, die nach der Abnahme nicht stimmen

Die Nachschau zu Sprint 5 des zweiten Laufs (Commits `4954bd2`, `67ccf42`) fand drei Behauptungen, die der
Code bzw. eine Messung nicht trägt. Alle drei sind Text, einer davon steht in der Hilfe des KI-Creators und
damit vor dem Nutzer.

## User Story

Als Creator, der den KI-Creator ohne `--profile` für ein Kind aufruft, möchte ich in der Hilfe lesen, in welcher
Reihe die Übung entsteht, damit ich nicht die Reihe des gewählten Fachlehrers erwarte und eine andere bekomme.

## Ist-Stand am Code

1. **B-237 – Hilfe des KI-Creators:** `--series` nannte als Standard „Reihe des Profils … das Fach kommt aus dem
   Profil". `BuildRequestAsync` lädt ein Profil aber nur bei ausdrücklichem `--profile`
   (`AgentCommands.cs:318`); das zum Kind gewählte (`BriefingBuilder.ResolveProfileAsync`, `:145`) wird erst
   nach der Ortsauflösung bestimmt und verschiebt sie nicht. Zwei Zeilen höher sagt die Hilfe, `--profile`
   sei „ohne Angabe das bestpassende zum Kind" – ein Leser erwartet dessen Reihe. Dasselbe in
   `README.md:146`.
2. **B-244 – Begründung der Zusicherung:** Entscheidung 2 sagt, der neue Vergleich `201`-Body gegen `GET`-Body
   sei „im Ausgangszustand grün (0 = 0)". **Gemessen falsch:** mit dem alten, von Hand gebauten `Create` ist
   `PublishersTests` **1 rot / 4 grün**, rot an `createdAt` (`…3973509Z` gegen `…3973509` – das frische
   `UtcNow` trägt `Kind=Utc`, der aus SQLite gelesene Wert nicht). Die Zusicherung ist damit stärker als
   behauptet, aber der Test-Kommentar schrieb das Rot dem Feld `foreignSeriesCount` der Mutante zu.
3. **B-246 – Sweep-Zahl:** Verlauf und Commit-Text sagen „4 Treffer, alle in der Ausschlussliste benannt";
   der Sweep auf HEAD ergibt **5** (`uebergabe-vokabeln.md:92` war aus der benannten Spanne `:87-91`
   gerutscht, weil Sprint 5 selbst eine Zeile einfügte). Das Protokoll (`„4 + 1"`) sagt es richtig, die Story
   nicht.

Nebenbefund ohne Story-Anspruch: B-243 Entscheidung 3 nennt den Testfall
`Zeigt_Fremde_Anmerkungen_Nur_Per_Break_Glass`, der heißt `OhneGlobalRead_TraegtDieAdminRolle_WeiterhinAlsBreakGlass`
(`RemarkTests.cs:752`) – in B-243 berichtigt.

## Entscheidungen

1. **Hilfe und README nennen das Profil, das die Reihe tatsächlich setzt:** „Reihe des mit --profile genannten
   Profils … das zum Kind gewählte Profil verschiebt sie nicht". Begründung: der Code verengt die Antwort
   (Defekt, autonom). Ob das gewählte Profil die Reihe künftig setzen *soll*, ist eine Produktfrage und
   gehört nicht hierher. **Kosten:** zwei Zeilen Hilfe mehr.
2. **Der Test-Kommentar sagt, was gemessen ist** (jede Hand-Zeile fällt an `createdAt`), B-244 bekommt die
   Messung als Verlaufszeile statt einer stillen Korrektur der Entscheidung. **Kosten:** keine.
3. **B-246 bekommt die richtige Zahl als Verlaufszeile.** **Kosten:** keine.

## Akzeptanzkriterien

1. `pugling-creator help` und `README.md:146` sagen nicht mehr, ein ohne `--profile` gewähltes Profil setze
   die Reihe oder das Fach.
2. Der Kommentar in `PublishersTests.cs` schreibt das Rot keinem einzelnen Feld mehr zu.
3. B-244, B-246 und B-243 tragen je eine Verlaufszeile mit der Korrektur.

## Schätzung

**Größe: XS** – zwei Hilfezeilen, eine README-Zelle, ein Test-Kommentar, drei Verlaufszeilen. `wo: beides`
(Agent + Doku), keine Migration, kein Vertragsbruch. **Testweg:** `pugling-creator help` lesen, Grep auf den neuen
Halbsatz, Backend-Suite (der Kommentar ändert kein Verhalten).

## Verlauf

- **2026-10-23** — angelegt aus der Nachschau zu Sprint 5 (frischer Agent, read-only, `git show HEAD:`),
  autonom gegrillt (Freigabe 1, `Defekt`) und im selben Sprint behoben. Messung zu Punkt 2 selbst gefahren:
  `Create` vorübergehend auf die Hand-Zeile von `4954bd2^` zurückgesetzt, `dotnet test --filter
  PublishersTests` → **1 rot / 4 grün**, Meldung an `createdAt`; Datei danach byte-gleich zurück (`git diff`
  leer). Liegt außerhalb des Diffs von Sprint 6, darum **kein Zählerfund** (Freigabe 3). Belege für AK 1:
  `grep -c "das zum Kind gewählte" AgentCommands.cs README.md` → je 1. Commit: `39cf33b`.
- **2026-10-23** — Nebenbefund der Messung, bewusst nicht angefasst: eine von Hand gebaute Antwort serialisiert
  `createdAt` mit `Z` (frisches `UtcNow`, `Kind=Utc`), eine Projektion aus SQLite ohne (`Kind=Unspecified`) – derselbe
  Zeitpunkt erscheint im Vertrag in zwei Schreibweisen. Ob das eine eigene Story wird, entscheidet der Nutzer.
- **2026-10-23** — Nachschau (vierter Lauf, frischer Prüfer, read-only über `39cf33b`). Geprüft: Hilfetext `AgentCommands.cs:458-461` und README `:146` gegen `BuildRequestAsync` (`AgentCommands.cs:310-323`), `ResolveLocationAsync` (`:362-367`) und `BriefingBuilder.ResolveProfileAsync` (`:141-158`) – die Reihe setzt nur ein genanntes `--profile`, das Kind-Profil wird erst danach am Fach gesucht; Kommentar `PublishersTests.cs:23-26` gegen `CreatedAt = DateTime.UtcNow` (`CurriculumEntities.cs:20`) und das Zurücklesen in `Create`. **Kein Befund.** Randnotiz älter als der Diff: hat die Reihe des genannten Profils keine Unit, bricht der Agent ab, statt auf „erste Reihe mit Unit" auszuweichen – die Hilfe lässt sich so nicht lesen, der Satz „sonst" meint nur den Fall ohne Profil.
- **2026-10-26** — **Entscheidung des Nutzers** (Morgen-Entscheidung 2026-10-26, Dialog, [Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)): **Ja**, das automatisch zum Kind gewählte Profil soll künftig Reihe und Fach des KI-Creators setzen, `--series` übersteuert weiterhin. Das wird eine eigene Story, [B-271](B-271-gewaehltes-profil-setzt-reihe.md); diese Story bleibt abgenommen.
