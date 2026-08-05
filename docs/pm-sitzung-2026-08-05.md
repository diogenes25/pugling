---
tags: [typ/protokoll, bereich/pm]
aliases: [PM-Sitzung 2026-08-05, Sprint 1 Kaufverlauf]
---

# PM-Sitzung: Der Kaufverlauf lügt nicht mehr

**Datum:** 2026-08-05 · **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Erstprobe des am selben Tag geschärften Verfahrens (`pm-loop` → „The Sprint", Step 6, Step 8) an
einem echten, kleinen Sprint — und dabei das Verfahren selbst beobachten.

Diese Sitzung ist **kein** vollständiger `pm-loop`-Durchgang: Rollen-Feedback (Step 2) und Priorisierung
(Step 3) lagen schon vor — die drei Kandidaten kamen aus dem Code-Review der autonomen Bau-Runde vom
2026-08-05. Gefahren wurden Step 3 (Zuschnitt) bis Step 8 (Retrospektive).

## Vorlauf — die drei Kandidaten wurden erst zu Stories

Aus dem Review lagen drei unerfasste Befunde vor. Alle drei sind am Code belegt worden, dann autonom
gegrillt und geschätzt — zulässig, weil alle `art: Defekt` bzw. `Aufräumen` tragen (README → „Der
Backlog-Lauf": bei `Wunsch`/`Frage` wäre hier Halt gewesen).

| Story | Titel | art | Größe | wo |
| --- | --- | --- | --- | --- |
| [B-110](backlog/B-110-kaufverlauf-ueberspringt-zeilen.md) | Der Kaufverlauf überspringt Zeilen und verpasst den eigenen Kauf | Defekt | S | beides |
| [B-111](backlog/B-111-verlauf-luegt-im-fehlerfall.md) | Scheitert das Laden, sagt die App „Noch nichts gekauft" | Defekt | XS | frontend |
| [B-112](backlog/B-112-kommentar-begruendet-das-gegenteil.md) | Ein Kommentar begründet das Gegenteil der Bedingung | Aufräumen | XS | frontend |

Zwei Funde des Ausformulierens, die die Stories gegenüber dem Review verschoben haben:

- **B-110 ist größer als gemeldet.** Der Review nannte Paging und den fehlenden Refresh. Die Recherche fand
  die *Ursache*: der Server sortiert nach `Status` — einem **veränderlichen** Schlüssel —, und über einer
  Ordnung, die sich verschieben kann, ist Offset-Paging grundsätzlich falsch. Zusatzbefund: diese
  Gruppierung war **nie entschieden**, sie kam mit einem Ordner-Umbau (`b253d7a`) an ihren Platz. Damit war
  sie ersetzbar, ohne eine Abwägung zu überstimmen — hätte dort eine Entscheidung gestanden, wäre der
  Zuschnitt ein anderer geworden.
- **B-112 ist kleiner als gemeldet.** Die Bedingung unter dem widersprüchlichen Kommentar ist **richtig**
  und ist die dokumentierte Regel aus `frontend/CLAUDE.md`. Die Story schrumpfte von „Bedingung prüfen" auf
  „Begründung reparieren".

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel:** *Der Sohn sieht in seinem Kaufverlauf jede Zeile — auch die, die er gerade gekauft hat —
und wenn die App den Verlauf nicht laden kann, sagt sie es ihm.*

**Umfang:** B-110, B-111. — **B-112 bewusst außen vor**: sie dient dem Ziel nicht (ein Kommentar in einem
Vater-Dialog), und ein Sprint ist *ein* roter Faden, keine Sammlung des zufällig Gleichzeitigen. Sie bleibt
`geschaetzt` und wartet auf einen Sprint, in den sie gehört.

**Entwickler-Brief:**

- **Quelle der Wahrheit:** die Ordnung der Kaufhistorie gehört dem Server. Der Client darf blättern, aber
  nicht sortieren — also muss die Server-Ordnung *unbeweglich* sein, statt dass der Client Verschiebungen
  ausgleicht.
- **Backend zuerst:** `MeController.ShopViewAsync` sortiert nach `PurchasedAt desc, Id desc`; die
  Gruppierung nach `Status` fällt. Sichtbare Folge (gewollt): ein stornierter Kauf steht chronologisch,
  nicht am Listenende — die Pille „storniert" trägt die Information ohnehin.
- **Dann das Frontend:** `buy()` verwirft den geladenen Verlauf (nicht ergänzen — dem Kauf-Response fehlt
  `X-Total-Count`, „X von Y" wäre danach falsch). `HistoryTab` bekommt den Fehler als **dritten** Zustand.
- **Testweg:** Integrationstest für die Stornierung zwischen zwei Seiten; Komponententests für die drei
  Zustände der Karte; eine **eigene** E2E-Spec — nicht in `full-flow.spec.ts`, weil dessen Shop-Block
  hinter der Klausur-Sequenz liegt und wegen B-109 gar nicht läuft.

## Iteration 1 — umgesetzt

**Backend** (`MeController.cs:413-421`): Sortierung auf `PurchasedAt desc, Id desc`, mit dem *Warum* im
Kommentar (Offset-Paging verlangt eine Ordnung, die sich nicht verschieben kann).

**Frontend** (`SohnShop.tsx`): eigener `historyError`-State neben dem 2-Sekunden-Toast; `buy()` verwirft
Verlauf, Gesamtzahl und `historyLoaded`; `HistoryTab` unterscheidet jetzt „nichts gekauft" / „lädt" /
„gescheitert" und zeigt im dritten Fall Meldung **und** Wiederholen-Knopf. Scheitert eine *spätere* Seite,
bleiben die geladenen Zeilen stehen und der vorhandene „Mehr laden"-Knopf ist der Wiederholen-Weg.

**Rote Proben zuerst — alle drei getroffen:**

| Probe | Erwartung | Gemessen |
| --- | --- | --- |
| `Kaufhistorie_StornoZwischenZweiSeiten_UeberspringtKeineZeile` gegen die alte Sortierung | eine Zeile unerreichbar | `Expected: 4, Actual: 3` |
| Zwei neue Karten-Tests gegen die alte `HistoryTab` | zeigt „Noch nichts gekauft" | 2 failed, Ausgabe enthält wörtlich „Noch nichts gekauft" |
| `e2e/shop-verlauf.spec.ts` gegen die alte `SohnShop` | Kauf fehlt im Verlauf | `toHaveCount(0)` → `Received: 1` |

Der dritte neue Karten-Test („sagt während des Ladens nichts") lief auch **vorher** grün: er beschreibt
Verhalten, das schon stimmte, und ist damit ein Charakterisierungstest, kein Regressionstest. Festgehalten,
weil ein grüner neuer Test sonst wie ein behobener Fehler aussieht.

**Verifikation (gemessen):** Backend `dotnet test -c Release` → **730/730 grün** (729 + 1). Frontend
`npm run build` sauber, `npm test` → **152/152 grün** (149 + 3). `npm run test:e2e` → **26/27**, der
Ausfall ist unverändert `full-flow.spec.ts` (B-109, vorbestehend, per `git stash` schon am 2026-08-05
gegen HEAD bestätigt).

## Runde — Abnahme Sprint 1 (Rollengang)

- **Sohn: signiert, im echten Browser.** Der Weg „Verlauf ansehen → kaufen → Verlauf ansehen" läuft in
  Chromium gegen einen echten Server (`e2e/shop-verlauf.spec.ts`): vorher stand dort dauerhaft „Noch
  nichts gekauft", jetzt steht der eigene Kauf da. Der Kauf selbst, die Feier und der Inventar-Tab
  ebenfalls geprüft.
- **Sohn: eine Teilfläche nur maschinell, nicht im Rollengang** — der Fehlerfall des Ladens. Ihn im
  Browser zu erzeugen hieße, den Server künstlich kaputt zu machen; er ist auf Kartenebene bewiesen
  (`HistoryTab.test.tsx`). Das ist eine argumentierte Ausnahme, keine Lücke: eine Fehleranzeige ist kein
  sinnlich-subjektives Merkmal, sondern eine Bedingung, und die ist gedeckt.
- **Vater: signiert (Regression).** Seine Sicht ist vom Diff nicht berührt (eigener Endpunkt, eigene
  Ordnung); belegt durch die grüne Suite und die neun Vater-Specs im E2E-Lauf. Der Aufbau des Sprints hat
  seine Oberfläche zudem beiläufig benutzt (Verschenken über `/vater/konto`) — sie funktioniert.
- **Creator: signiert (Regression).** Kein Katalog-Pfad angefasst; `uebungstypen.spec.ts`,
  `freigabe.spec.ts`, `lehrwerke.spec.ts` und `vater-von-null.spec.ts` sind grün.
- **Offen und benannt: die beiden Reviewer sind NICHT gelaufen.** In dieser Sitzung gilt die Anweisung,
  keine Agenten ohne ausdrückliche Aufforderung zu starten; `pugling-reviewer`/`frontend-reviewer` sind
  aber Eintrittsbedingung für `abgenommen`. **Folge: B-110 und B-111 bleiben auf `in-arbeit`**, obwohl
  alles andere belegt ist. Das ist die Regel, die greift — nicht ein Versehen.

**Neuer Fund bei der Verifikation:** dieselbe Zeile steht eine Ebene höher noch einmal
(`ShopController.cs:347`), und der Vater-Client blättert diese Liste **überhaupt nicht** — also derselbe
stille Schnitt, den B-99 auf der Kind-Seite beseitigt hat, jetzt bei dem, der aus der Liste heraus
storniert. Als eigene Story aufgenommen: [B-113](backlog/B-113-vater-kaufhistorie-endet-still.md),
`ausformuliert`, bewusst nicht geschätzt (ein offener Punkt könnte sie zur Sammel-Story machen). Nicht in
diesen Sprint gezogen — B-110s Ziel ist ohne sie erfüllt, dasselbe Muster wie B-97 → B-104.

## Retrospektive

**Was die eigenen Tore durchgelassen haben:** nichts aus diesem Sprint — aber der Sprint hat gezeigt,
*warum* die Runde vom Vortag zwei Defekte durchließ, und zwar an einer Stelle, die niemand vermutet hatte.

**Der Befund:** Der Shop-Verlauf des Sohns **war** durch eine E2E gedeckt — seit B-99, in
`full-flow.spec.ts:138-142`. Diese Prüfung läuft seit B-109 **nicht mehr**, weil sie hinter der
Klausur-Sequenz derselben Datei liegt. Mit ihr liegen drei weitere Blöcke still (Fortschritt,
Positions-Report, Lernstand-Drilldown). Ein einzelner roter Schritt hat also **vier fremde Prüfflächen**
mitgenommen.

**Eine Korrektur an der eigenen Analyse, die zum Befund gehört:** Der erste Entwurf dieser Retro
behauptete, das Rot erreiche niemanden, weil „CI kein E2E fährt". Das ist **falsch** und wäre als
Begründung eines neuen Mechanismus in die Doku eingegangen. `.github/workflows/e2e.yml` fährt die Suite an
jedem Pull Request und nachts um 03:00 UTC auf `main` und stellt ein Rot als Issue mit Zustand zu (anlegen,
kommentieren, bei Grün schließen) — gebaut in B-26, genau gegen dieses Versagen. Richtig ist nur der
schmale Satz, aus dem die Fehlannahme entstand: `ci.yml` selbst fährt kein E2E, und das ist dort
begründet. Der Fehler war, aus einer Aussage über *ein* Tor eine Aussage über *die* Zustellung zu machen.

**Damit verschiebt sich der Befund — und wird schärfer:** Das Signal existiert und funktioniert. Es sagt
aber „E2E ist rot" und benennt den gescheiterten Schritt; es sagt **nicht**, dass vier unbeteiligte
Produktflächen dahinter aufgehört haben, geprüft zu werden. Genau das ist passiert: B-109 wurde als
*Symptom* aufgenommen („Klausur hängt bei Frage 3"), mit P3 bewertet — und die Bewertung wäre eine andere
gewesen, hätte jemand gewusst, dass Shop, Fortschritt, Report und Lernstand mit daran hängen. Der Schaden
war nicht unsichtbar, weil niemand hinsah, sondern weil das Signal seinen eigenen Umfang nicht kennt.

**Der Mechanismus (das Pflicht-Ergebnis dieser Retro): eine Fläche pro Spec.** Eine lange Spec vergrößert
mit jedem Schritt den Radius ihres eigenen Rots. Kurze, thematische Specs machen ein Rot präzise: was
scheitert, ist dann auch das, was ungeprüft bleibt. In diesem Sprint schon angewandt
(`e2e/shop-verlauf.spec.ts` als eigene Datei statt als Block in `full-flow.spec.ts`) und als Regel dort
gelandet, wo sie beim Schreiben einer Spec gelesen wird: `frontend/CLAUDE.md`. Dazu der Schadensbefund in
B-109, damit die Story ihren wahren Umfang trägt und neu bewertet werden kann.

**Warum kein Tor, sondern Prosa:** Ein Tor („keine Spec über N Zeilen") wäre mechanisch prüfbar, aber es
misst das Falsche — eine kurze Spec kann drei Flächen abdecken, eine lange eine einzige gründlich. Die
Regel betrifft den *Zuschnitt*, und der ist eine Entscheidung beim Schreiben, keine Zählung danach. Das ist
der in Step 8 vorgesehene, ausdrücklich zu argumentierende Fall „hier hilft kein Tor" — nicht der Rückfall
darauf, weil ein Tor Arbeit wäre.

**Was bewusst ungedeckt bleibt:** dass `full-flow.spec.ts` weiter ein einzelner Ausfallpunkt für seine
verbleibenden Blöcke ist. Das aufzuteilen ist B-109s Entscheidung, nicht die dieses Sprints.
