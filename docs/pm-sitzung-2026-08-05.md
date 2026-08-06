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

**Nachschau:** die sechs Abnahmen der autonomen Runde vom 2026-08-05, die der Code-Review vom Folgetag
**nicht** abgedeckt hatte — B-57, B-61, B-62, B-72, B-84, B-89. Als **Selbst-Check** geführt, weil
`pugling-reviewer` und `frontend-reviewer` je dreimal an einem serverseitigen `529` abgebrochen sind; das
ist der schwächere Beleg und steht so in jeder der sechs Stories. Ergebnis: **ein Befund** in B-89, als
[B-116](backlog/B-116-blaettern-ohne-rueckmeldung.md) aufgenommen (`entgangen_bei: [B-89]`) — der
Rundumschlag hat „dieselbe Abfrage wiederholen" und „andere Abfrage läuft" gleich behandelt und dem
Blättern damit jede Rückmeldung genommen. Fünf ohne Befund, jede mit dem geprüften Punkt im `## Verlauf`
(bei B-62 die nachgerechnete Behauptung „dieselbe Tageszählung wie `Start`" — sie stimmt wortgleich; bei
B-72 der Rundlauf, also genau die Stelle, an der B-66 gescheitert war). `nachgeschaut: 2026-08-05` auf
allen sechs, **auch auf den fünf sauberen** — sonst wäre der Blick nicht von „nie angesehen" zu
unterscheiden.

Damit steht der Nenner der Wirkungs-Zahl bei **14 von 42**, und die Quote bei **4 von 14** geprüften
Stories mit einer Entgleitung. Die Grundlinie 3-von-8 hat sich also *nicht* bestätigt — sie war zu
pessimistisch. Alle vier betroffenen Stories stammen weiter aus derselben Runde ohne Rollengang.

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

## Nachtlauf — Vorlauf

Vom Nutzer beauftragt (`docs/nachtlauf.md`, wörtlicher Auftragstext) mit den drei dort dokumentierten
Freigaben: autonomes Grillen nur für `art: Defekt`/`Aufräumen`, Selbst-Check statt Reviewer nur wenn dieser
unerreichbar ist (Stufe bleibt dann `in-arbeit`), genau **ein** Sprint mit Retro-Vorschlag statt -Landung.

**Vier liegen gebliebene Stories zuerst geprüft:** B-110, B-111, B-114, B-115 standen seit dem Vortag auf
`in-arbeit`/`wartet_auf`, weil `pugling-reviewer`/`frontend-reviewer` an sechs serverseitigen `529`
gescheitert waren. Erneuter Versuch: **alle fünf Reviewer-Läufe (2× je B-110/B-111/B-114, 1× B-115)
liefen jetzt durch**, kein Blocker. B-114 zusätzlich live gegen die laufende API bestätigt (Demo-Kind,
Position 7: `testable` blieb `false`, `goalMet` kippte nach einer vollständigen Übungsrunde auf `true`).
Alle vier auf `abgenommen` gehoben, `wartet_auf` geleert, `nachgeschaut: 2026-08-05` gesetzt (der
frische Reviewer-Blick zählt als der unabhängige Blick nach der Abnahme). Kein Browser-Rollengang möglich
(keine Chrome-Extension in dieser unbeaufsichtigten Sitzung) — je eine Zeile im `## Verlauf` benennt das
und den offenen Handgriff für einen Menschen.

## Sprint 2 — Ziel & Umfang

**Sprint-Ziel:** *Der Vater kann jede geblätterte Liste im Familien-Shop vollständig erreichen, und wenn
er blättert, sagt ihm die Oberfläche, dass die neue Seite noch lädt — statt eine veraltete Seite für die
neue zu halten.*

**Umfang:** B-113, B-116. Beide kamen als `ausformuliert` (Defekt) aus dem Vortag; da `art: Defekt` gilt,
autonom gegrillt und geschätzt (README → „Der Backlog-Lauf"). Beim Grillen von B-113 ein Fund, der die
eigene Vorannahme korrigiert: die beiden Aktivierungs-Anfrage-Listen sind — entgegen der ursprünglichen
Vermutung im Ist-Stand — **nicht** bereits geblättert dargestellt; nur die Kaufhistorie bekommt darum
einen echten Pager, die beiden Anfrage-Listen nur die Sortierungs-Korrektur (Begründung: kostenlos zu
messende Zeilenzahl heute, das belegte Risiko liegt bei der Kaufhistorie, aus der der Vater storniert).

**Entwickler-Brief:**

- **Quelle der Wahrheit:** die Ordnung einer server-paginierten Liste gehört dem Server und muss
  unveränderlich sein — dieselbe Regel wie B-110, jetzt an drei weiteren Stellen (Kaufhistorie
  Vater-Sicht, zwei Aktivierungs-Anfrage-Listen) durchgezogen, plus ein fehlender `Id`-Tiebreaker an
  beiden Anfrage-Listen.
- **Der Ladezustand beim Blättern gehört in den `Pager`**, nicht in jede der sieben Listen einzeln: er ist
  der einzige Ort, der sowohl die geklickte Seite als auch deren Ankunft kennt.
- **Backend zuerst:** `ShopController.cs`/`MeController.cs` — Sortierung entschärfen, Tiebreaker ergänzen.
- **Dann Frontend:** `ListControls.tsx` `Pager` bekommt `busy`; sieben Aufrufer verdrahten es; `api.ts`
  `childPurchases` wird paginiert; `VaterShop.tsx` bekommt den Pager für die Kaufhistorie.
- **Testweg:** je ein Integrationstest für die Storno-/Genehmigungs-Race (Gegenstück zu B-110s Test);
  ein Komponententest für den `busy`-Zustand des `Pager`.

## Iteration 2 — umgesetzt

**Backend:** `ShopController.cs:347` (Kaufhistorie) auf `PurchasedAt desc, Id desc` ohne
`Owned`-vor-`Cancelled`-Gruppierung; `ShopController.cs:389` und `MeController.cs:374` (beide
Aktivierungs-Anfrage-Listen) auf `RequestedAt desc, Id desc` mit ergänztem `Id`-Tiebreaker. **Rote Probe
zuerst:** zwei neue Integrationstests (`ShopFlowTests.cs`) scheiterten gegen den Vorzustand exakt wie
B-110s Vorbild (`Expected: 4, Actual: 3`), grün nach dem Fix.

**Frontend:** `ListControls.tsx` `Pager` bekommt `busy?: boolean` — ein `useRef` friert die zuletzt
gezeigte Spanne ein, solange `busy` gilt; alle sieben Aufrufer (`ClozeTexts`, `VaterAnmerkungen`,
`VaterClassTests`, `VaterExercises`, `VaterKonto`, `VaterLernstand`, `VaterVocab`) verdrahtet.
`api.ts` `childPurchases` von einem Status-Parameter auf `httpPaged` mit `skip`/`take` umgestellt
(Muster wie `classTests`/`childPoints`); `VaterShop.tsx`s `ChildShopView` hält `purchaseSkip` und rendert
den `Pager` unter der Kauf-Tabelle.

**Verifikation (gemessen):** Backend `dotnet test -c Release` → **732/732 grün** (730 + 2 neue).
Frontend `npm run build` sauber, `npm test` → **153/153 grün** (152 + 1 neuer). `dotnet format
--verify-no-changes` clean. `pugling-reviewer` und `frontend-reviewer` liefen beide erfolgreich, kein
Blocker. **Live gegen die laufende API geprüft** (Demo-Vater/Demo-Kind, vier eingefügte Käufe): Seite 1
und Seite 2 lieferten korrekt `X-Total-Count: 4` und die erwarteten Ids ohne Überlappung. Der geplante
Storno-Nachweis auf demselben Weg scheiterte an einem `409 concurrency_conflict` — Artefakt der rohen
SQL-Einfügung der Testzeilen (kein EF-Pfad), kein Produktdefekt; die Zeilen wurden entfernt. Derselbe
Nachweis steht bereits belegt in den beiden neuen Integrationstests, die über den echten EF-Pfad laufen.

## Runde — Abnahme Sprint 2 (Rollengang)

- **Vater: signiert, mit benanntem Rest.** Die Ordnungs-/Tiebreaker-Korrektur ist durch die zwei neuen
  Integrationstests (roter Vorzustand → grün) und die Live-Probe gegen die laufende API belegt
  (`X-Total-Count`, keine Lücke über zwei Seiten). Der `busy`-Ladezustand des `Pager` ist durch den neuen
  Komponententest und den Reviewer belegt. **Kein Browser-Rollengang möglich** (keine Chrome-Extension in
  dieser unbeaufsichtigten Sitzung) — benannt, nicht verschwiegen: ein Mensch sollte einmal im Vater-Web
  die Kaufhistorie blättern (dabei einen Kauf stornieren) und auf einer mehrseitigen Liste zügig „Weiter"
  klicken, um beides selbst zu sehen.
- **Sohn: Regression, kein eigener Pfad berührt.** Backend-Suite grün (732/732), Frontend-Suite grün
  (153/153), eigene Specs unberührt. Eine Nebenwirkung ausdrücklich benannt: `MeController.cs`s
  `MyActivations` (Sohns eigene Sicht auf seine Aktivierungsanfragen) trägt dieselbe
  Sortierungs-Korrektur — die Reihenfolge dort ändert sich ebenfalls von „offen zuerst" auf „neueste
  zuerst", was für den Sohn nur eine andere Anordnung derselben, ohnehin nach Status eingefärbten Zeilen
  bedeutet. Keine Sohn-Spec prüft diese Reihenfolge; nichts davon ist rot geworden.
- **Creator: Regression, kein Pfad berührt.** Kein Katalog-Code angefasst; die Creator-E2E-Specs sind Teil
  der grünen Gesamtsuite.

## Retrospektive — Sprint 2

**Nachschau:** B-110, B-111, B-114, B-115 (Vorlauf dieser Sitzung) — je ein frischer Reviewer-Lauf ohne
Kenntnis des vorigen Selbst-Checks, plus bei B-114 eine Live-Probe gegen die echte API. Ergebnis: **kein
neuer Fund** über das bereits Dokumentierte hinaus. `nachgeschaut: 2026-08-05` auf allen vier gesetzt.
Index-Stand: **Nachgeschaut 18 von 48** (B-113/B-116 sind zu frisch für diese Runde und zählen erst im
nächsten Nachtlauf/PM-Zyklus in den Nenner).

**Was dieser Sprint über die eigenen Tore gelernt hat:** `pugling-reviewer` hat für B-113 ausdrücklich
berichtet, dass sein eigener `dotnet build`/`dotnet test`-Lauf am selben Datei-Lock scheiterte, der auch
meinen ersten Build-Versuch traf (ein laufender `dotnet run`-Dev-Server sperrt die Debug-Ausgabe —
CLAUDE.md → „Arbeitsweise" nennt das für den Test-Gate-Hook, aber ein Reviewer-Agent, der von Null einen
Build/Test-Lauf startet, weiß das nicht von sich aus). Der Agent hat das transparent benannt und ist auf
Diff-Lektüre zurückgefallen — das ist der **richtige** Umgang mit einem gescheiterten Verifikationsschritt
(benennen statt verschweigen), aber es hätte auch anders laufen können: eine Formulierung, die den
fehlgeschlagenen Build nur als Nebensatz in einem sonst souverän klingenden Bericht erwähnt, ist leicht zu
überlesen, wenn niemand den vollen Text liest. In dieser Sitzung war das folgenlos, weil die eigene
Verifikation (`-c Release`) bereits vorlag — aber ein Reviewer-Lauf, dessen Build/Test-Schritt lautlos
ausfällt, ist strukturell derselbe Fall wie der Selbst-Check-statt-Reviewer aus dem Vorlauf: ein
schwächerer Beleg, der aussieht wie der volle.

**Vorschlag für einen Mechanismus (nicht gelandet — Freigabe 3 dieses Nachtlaufs):** die
Agenten-Definitionen von `pugling-reviewer`/`frontend-reviewer` (`.claude/agents/*.md`, falls dort
Build/Test-Befehle stehen) um die Anweisung ergänzen, `dotnet build`/`dotnet test` **immer mit
`-c Release`** zu fahren — dieselbe Regel, die der Test-Gate-Hook schon befolgt, nur bisher nicht dem
Reviewer mitgegeben. Das ist eine Entscheidung (welcher Text wo steht), kein mechanisches Tor: eine
Build-Konfiguration lässt sich nicht durch einen Guard-Test erzwingen, den ein Subagent vor seinem eigenen
Lauf liest. Wo die Zeile landet und ob sie reicht, entscheidet der Nutzer am Morgen.

**Was bewusst ungedeckt bleibt:** ob ein Reviewer-Lauf seinen eigenen Verifikationsstatus (verifiziert vs.
nur gelesen) strukturiert statt in Prosa berichten sollte — das wäre der schärfere, aber auch teurere
Fix (Schema-Änderung an der Agentenausgabe) und ist diesem Nachtlauf zu groß.

## Ende des Nachtlaufs

Genau **ein** Sprint (Freigabe 3) — der Lauf endet hier. Offene `Wunsch`/`Frage`-Punkte wurden keine
angetroffen: B-113 und B-116 waren beide `art: Defekt` und damit autonom grillbar; kein Halt war nötig.
Nichts ist gepusht. Commits: `ff6b1a3` (B-113 Backend), `07eddc6` (B-116), `9f475ea` (B-113 Frontend);
die Backlog-Dateien selbst sind noch unkommittiert.

**Ehrlich benannt:** `.claude/scripts/backlog-index.sh` lief einmal erfolgreich direkt nach den vier
Reviewer-Nachträgen (B-110/B-111/B-114/B-115 stehen im Index als `abgenommen`), ließ sich danach in
dieser Sitzung aber nicht erneut ausführen — das Werkzeug selbst meldete wiederholt eine temporäre
Nichtverfügbarkeit, unabhängig vom Befehl. Der Index im Repo spiegelt darum **B-113/B-116 und die
`nachgeschaut`-Setzungen dieser Retro noch nicht**. Das ist ohnehin der erste Schritt der Morgen-Prüfung
(unten) — hier nur vorab benannt, damit es niemanden überrascht, falls „60 offen" im Index noch die alte
Zahl zeigt.

Die fünf Prüfpunkte für den Morgen stehen in [docs/nachtlauf.md](nachtlauf.md#am-morgen-fünf-zeilen-prüfen).

## Nachtlauf 2 — Vorlauf

Vom Nutzer am Abend desselben Tages neu beauftragt, nach demselben Auftragstext wie oben, aber mit einer
geänderten Freigabe 3 ([docs/nachtlauf.md](nachtlauf.md), heute aktualisiert): **mehrere thematisch
verwandte Sprints** statt genau einem, Retro bleibt je Sprint auf „nur vorschlagen" beschränkt, ein
Defekt im eigenen Increment beendet die ganze Nacht statt nur den Sprint, ausgesprochen große Stories
werden geteilt statt gebaut. Neu dazu: die Chrome-Extension ist in dieser Sitzung erreichbar (geprüft per
`tabs_context_mcp`, ein Tab verbunden) — ein echter Browser-Rollengang ist damit möglich, wo er sich
lohnt; und der Skill `web-design-guidelines` steht für UI-Stories zusätzlich zur Verfügung.

**Buildbares Material zu Beginn:** 14 `Aufräumen`-Stories bereits `geschaetzt` (B-25/27/32/44/47/49/51/
55/58/59/74/83/88/112), 4 `Aufräumen` `ausformuliert` (B-95/100/101/102), 2 `Aufräumen` + 1 `Defekt`
`idee` (B-107/108/109). `Wunsch`/`Frage` (33 Stories) bleiben gesperrt; B-07/B-31 warten weiter extern.

Vier thematische Sprints gebildet (nach Themengebiet, nicht nach Zeitbudget):

| Sprint | Thema | Stories |
| --- | --- | --- |
| 1 | Rollen-/Bezeichner-Konsistenz | B-44, B-32, B-51, B-83, B-112 |
| 2 | Testsuite-Qualität & Determinismus | B-88, B-27, B-55, B-58 |
| 3 | CI/Deploy-Tooling | B-25, B-47 |
| 4 | Typsicherheit stärken | B-59, B-74, B-49 |

## Nachtlauf-2-Sprint 1 — Ziel & Umfang

**Sprint-Ziel:** *Wer die Rollen-Dokumentation oder den Auth-Code liest, findet dort, was der Code
längst tut — Creator/Supervisor/Student statt Vater/Kind als Ebenen-Namen, keinen `Father`-Bezeichner
mehr für eine `Adult`-Zeile, die vierte `Admin`-Rolle sichtbar, und die Lösungsfeld-Regel dort, wo man
sie vor dem Schreiben eines DTOs liest.*

**Umfang:** B-44 (grundprinzip.md), B-32 (Father→Adult-Bezeichner), B-51 (Admin-Rolle dokumentieren),
B-83 (Lösungsfeld-Regel in CLAUDE.md), B-112 (Kommentar-Korrektur). Alle fünf `art: Aufräumen`, alle
bereits `geschaetzt` bzw. (B-112) mit vollständiger Schätzung.

**Entwickler-Brief:**

- Kein Verhalten ändert sich — die Abnahme ist „alles so grün wie vorher" plus die inhaltliche Richtigkeit
  der neuen Doku-Zeilen gegen den tatsächlichen Code.
- Backend zuerst (B-32, einziger Code-Eingriff dieses Sprints): drei Bezeichner umbenennen, dann die
  Doku-Stories (B-44, B-51, B-83) und der eine Frontend-Kommentar (B-112).
- Testweg: `dotnet build`/`dotnet test Pugling.sln -c Release`/`dotnet format --verify-no-changes` fürs
  Backend, `npm run build`/`npm test -- --run` fürs Frontend, `markdownlint-cli2` für alle geänderten
  `.md`-Dateien.

## Nachtlauf-2-Iteration 1 — umgesetzt

**Backend** (B-32): `AuthAccess.cs` (`authorFatherId` → `authorAdultId`), `ShopService.cs`/
`ShopController.cs` (`ListingsForFatherAsync` → `ListingsForSupervisorAsync`), `TestApi.cs` + 70 weitere
Testdateien (`FatherAsync` → `AdultAsync`, wortgrenzen-scharf per Regex, `NewFatherAsync`-Wrapper
unberührt).

**Doku** (B-44, B-51, B-83): `docs/grundprinzip.md` auf Supervisor/Student umgestellt (Alias, Tabelle,
zwei H2-Titel, Fließtext, Fußnote zu B-46) mit den zwei bewussten Ausnahmen aus B-44s Entscheidung 5;
neuer Abschnitt „Admin — Plattform-Superuser" in `wiki/02-authentifizierung.md` plus Verweiszeile in
`docs/rollen-doku.md`; neue Konventionszeile zur Lösungsfeld-Regel in der Root-`CLAUDE.md`.

**Frontend** (B-112): Kommentar in `ExerciseEditModal.tsx:353` auf Englisch umgeschrieben, benennt beide
Gründe der Bedingung.

**Verifikation (gemessen):** Backend `dotnet build` sauber, `dotnet test Pugling.sln -c Release` →
**734/734 grün** (unverändert), `dotnet format --verify-no-changes` clean. Frontend `npm run build`
sauber, `npm test -- --run` → **153/153 grün** (unverändert). `markdownlint-cli2` gegen alle vier
geänderten `.md`-Dateien → **0 Issues**.

**Ehrlich benannter Nebenfund (kein Fehler, keine eigene Story):** B-32s AK4-Grep-Probe
(`grep -rn "FatherAsync" backend/`) zeigt weiterhin drei Treffer in `RemarkTests.cs`/`OwnershipTests.cs`/
`ExerciseGrantsTests.cs` — das sind unabhängige, lokale private Test-Helfer
(`RegisterFatherAsync`/`FreshFatherAsync`/`RegisterAdminFatherAsync`), die der Ist-Stand von B-32 nicht
erfasst hatte. Außerhalb des Story-Umfangs (nur `TestApi.FatherAsync`), bewusst nicht mitgezogen.
`CLAUDE.md`s eigenes Kontext-Budget war schon **vor** B-83 um 1546 B über der 19000-B-Grenze; B-83s neue
Zeile (+685 B) vergrößert das bestehende, nicht von diesem Sprint verursachte Defizit, kompensiert es
aber nicht (Warn-Tor, kein Blocker).

## Runde — Abnahme Nachtlauf-2-Sprint-1 (Rollengang)

- **Vater/Creator/Sohn: Regression, kein sichtbarer Pfad geändert.** Alle fünf Stories sind
  `art: Aufräumen` ohne beabsichtigte Verhaltensänderung; belegt durch die grüne Gesamtsuite (734/734
  Backend, 153/153 Frontend) und beide Reviewer-Läufe ohne Blocker.
- **Browser-Rollengang versucht, nicht gelungen — ehrlich benannt statt verschwiegen.** Die
  Chrome-Extension war verbunden (Freigabe 6), `/vater/exercises` und `/vater` luden sichtbar korrekt
  (Seiteninhalt per `get_page_text` gelesen, Demo-Vater #3 eingeloggt), aber das native `<select>` „Fach"
  ließ sich über die verfügbaren Automatisierungs-Aktionen (Klick auf Option, Pfeiltaste+Enter) nicht
  befüllen — kein Absturz, sondern eine Automatisierungsgrenze dieses spezifischen Steuerelements.
  Da B-112s Änderung ohnehin nur einen Kommentar betrifft (keine Zeile, die zur Laufzeit ein anderes
  Verhalten zeigt), ist der dokumentierte Ersatz aus `docs/nachtlauf.md` („Wenn kein Browser da ist")
  hier ausreichend: die grüne Suite plus der `frontend-reviewer`, der den Code um die Stelle gelesen hat.
- **Kein Rollengang, der einen Defekt hätte verstecken können**, weil keine der fünf Stories einen
  Pfad berührt, den ein Rollengang prüfen würde (keine neue Bedingung, kein neuer Endpunkt, keine
  geänderte Rückgabe) — der einzige Fund dieses Sprints (siehe Retro) kam ohnehin aus dem
  `frontend-reviewer`, nicht aus einem fehlenden Rollengang.

## Retrospektive — Nachtlauf 2, Sprint 1

**Nachschau:** Vor diesem Sprint stand der Index bei „Nachgeschaut 18 von 49" (Stand nach der Erprobung
vom selben Tag). Dieser Nachtlauf beginnt eine neue Runde und schuldet keine Nachschau der *vorigen*
Runde erneut — B-113/B-116/B-117 (aus der Erprobung) sind bereits durch deren eigene Retro geprüft.
Für **diesen** Sprint gilt die Nachschau-Pflicht dem *nächsten* Zyklus; hier stattdessen die einzige
Handlung, die jetzt fällig ist: der Fund aus Step 5 selbst, siehe unten.

**Was dieser Sprint über die eigenen Tore gelernt hat:** Der `frontend-reviewer` fand in B-112 einen
**echten Defekt im eigenen Increment** — der von mir geschriebene Kommentar hatte die beiden Hälften der
Begründung vertauscht (siehe B-112s `## Verlauf`). Kein Test hätte das gefangen (es ist eine reine
Prosa-Aussage in einem Kommentar), nur ein Reviewer, der den referenzierten Code (`useAsync.ts`)
tatsächlich nachrechnet statt den Kommentar für bare Münze zu nehmen. Das ist genau die Fehlerklasse,
vor der die Nachtlauf-Auflagen 4/5 warnen: eine Behauptung, die plausibel klingt und falsch ist.

**Der Mechanismus:** Kein neuer Gate — das bestehende Tor (`frontend-reviewer` vor jeder Abnahme, Step 5)
hat **funktioniert**, genau wie vorgesehen. Es braucht keine Verschärfung; es hat den Fund gemacht, für
den es da ist, bevor irgendetwas als `abgenommen` markiert wurde.

**Warum die Nacht hier endet (Freigabe 3):** Ein Review-Fund im eigenen Increment dieses Sprints ist
laut Auftrag der Grund, den gesamten Lauf zu beenden, nicht nur den Sprint — unabhängig davon, dass der
Fund vor der Abnahme kam und behoben wurde. Die Qualitätsschwelle ist einmal gerutscht; Weiterlaufen in
Sprint 2–4 wäre die falsche Reaktion. Die drei noch nicht begonnenen Sprints (Testsuite-Qualität,
CI/Deploy-Tooling, Typsicherheit) bleiben `geschaetzt` und liegen für die nächste Sitzung bereit.

## Ende des Nachtlaufs 2

Ein Sprint gebaut und abgenommen (B-44, B-32, B-51, B-83, B-112), drei weitere geplante Sprints
(Testsuite-Qualität: B-88/27/55/58; CI/Deploy-Tooling: B-25/47; Typsicherheit: B-59/74/49) nicht
angefasst — Freigabe 3 (Review-Fund im eigenen Increment) beendet die Nacht nach Sprint 1. Kein
`Wunsch`/`Frage`-Punkt wurde angetroffen, da alle fünf Stories `art: Aufräumen` waren. Nichts ist
gepusht.

## Nachtlauf 2 — Regeländerung und Fortsetzung

Der Nutzer hat nach dem Abbruch die Freigabe 3 selbst geändert (Commit `bb8dcb5`,
[docs/nachtlauf.md](nachtlauf.md)): ein einzelner, **behobener** Review-Fund im eigenen Increment
beendet die Nacht nicht mehr automatisch. Neu gilt ein Fünf-Fehlversuche-Zähler je Sprint — jeder Fund
wird sofort analysiert und entweder selbständig behoben oder als `art: Defekt` im selben Sprint
bearbeitet; erst mehr als fünf solcher Funde in einem Sprint gelten als Endlosschleife und beenden den
gesamten Lauf. Begründung des Nutzers: die alte Fassung bestrafte einen **funktionierenden** Reviewer
(B-112s Fund war klein und sofort korrigiert) wie einen Fehlschlag. Mit der neuen Regel wurde der Lauf
ausdrücklich fortgesetzt — Sprint 2 beginnt hier.

## Nachtlauf-2-Sprint 2 — Ziel & Umfang

**Sprint-Ziel:** *Die Testsuite selbst wird verlässlicher: Punkte-Zeitfenster hängen an einer steuerbaren
Uhr statt der Wanduhr, fünf bisher ungeprüfte Zahlen-Grenzen des `ScoringService` sind billig statt teuer
gepinnt, kein Testlauf hinterlässt mehr unbegrenzt wachsende Wegwerf-Dateien, und der Lehrplan-Assistent
hat seinen ersten echten Durchstich.*

**Umfang:** B-88 (TimeProvider statt Wanduhr), B-27 (ScoringService-Grenzfälle), B-55 (Wegwerf-Dateien),
B-58 (Assistent-E2E). Alle vier bereits `geschaetzt`, alle `art: Aufräumen`.

**Entwickler-Brief:** Backend zuerst (B-88, B-27, B-55-Backend-Teil), dann Frontend (B-55-Frontend-Teil,
B-58). Kein Verhalten ändert sich für Vater/Sohn/Creator — die Abnahme ist „alles so grün wie vorher"
plus, für B-58, ein neu geprüfter Weg. Testweg: `dotnet test Pugling.sln -c Release`,
`dotnet format --verify-no-changes`, `npm run build`, `npm run test:e2e`.

## Nachtlauf-2-Iteration 2 — umgesetzt

**Backend:** `PositionPracticeController.cs:413` auf `time.GetLocalNow().DateTime` (B-88);
`PositionTimeSlotTests.cs` auf ein schmales 13–15-Uhr-Fenster mit eingefrorener `TestClock` umgebaut plus
Gegenfall (B-88); neue Host-freie `ScoringServiceBoundaryTests.cs` mit fünf Grenzfällen (B-27);
`QueryPlanSmokeTests.cs` in `try`/`finally` mit `ClearPool` vor `Delete` (B-55); neues
`backend/Pugling.Api.Tests/CLAUDE.md` (B-55).

**Frontend:** `e2e/temp-paths.ts`, `e2e/global-setup.ts`, `e2e/global-teardown.ts`, verdrahtet in
`playwright.config.ts` (B-55); neuer Spec `e2e/assistent.spec.ts` (B-58).

**Verifikation (gemessen):** `dotnet test Pugling.sln -c Release` → **746/746 grün** (unverändert +12 neue
Fälle). `dotnet format --verify-no-changes` clean. `npm run build` clean. `npm run test:e2e` →
**27/28 grün** (einziger Ausfall: der vorbestehende, dokumentierte B-109-Flake in `full-flow.spec.ts`).

**Fünf rote Proben, alle mit Zahl belegt (Auflage 5):** B-88s Zeitfenster-Test (Erwartet 20/Gemessen 10,
Wanduhr außerhalb des Fensters), B-27s fünf Grenzfälle (je Erwartet/Gemessen einzeln in B-27s `## Verlauf`
dokumentiert), B-58s Vertausch-Gegenprobe (Erwartet 95%/−7 vs. Gemessen 7%/−95). Alle Produktionscode-
Mutationen danach `git diff`-bestätigt zurückgenommen (byte-identisch).

**Zwei Review-Funde in diesem Sprint (Zähler: 2 von 5, kein Abbruch):**

1. `pugling-reviewer` (kein Blocker): zwei Politur-Hinweise in `QueryPlanSmokeTests.cs` (die
   `ClearPool`-Hilfsverbindung selbst nicht disposed; `File.Delete` ohne eigenes `try`/`catch` hätte einen
   echten Testfehler maskieren können). Beide sofort behoben.
2. `frontend-reviewer` (kein Blocker): die erste Kommentar-Fassung in `global-setup.ts`/
   `global-teardown.ts` schrieb die `EBUSY`-Ursache fälschlich einem Virenscanner zu — tatsächlich ist es
   Playwrights eigene Task-Reihenfolge (Setup nach dem eigenen `webServer`-Start, Teardown vor dessen
   Stop). Kommentare korrigiert, Sweep um einen expliziten Ausschluss der eigenen Pfade gehärtet.

## Runde — Abnahme Nachtlauf-2-Sprint-2 (Rollengang)

- **Sohn: Regression.** Kein eigener Pfad berührt; grüne Gesamtsuite ist der Beleg.
- **Vater: signiert, mit echtem Rollengang für B-58.** `e2e/assistent.spec.ts` fährt den kompletten
  Assistenten-Weg im echten Browser gegen den echten Server (Chrome-Extension verbunden, Freigabe 6)
  — genau der Weg, den ein Vater ginge. Für B-88/B-27/B-55 gilt Regression: kein sichtbarer Pfad
  geändert, belegt durch die grüne Suite und beide Reviewer.
- **Creator: Regression.** Kein Katalog-Pfad berührt.

## Retrospektive — Nachtlauf 2, Sprint 2

**Nachschau:** Sprint 1 dieser Nacht (B-44/32/51/83/112) wird hiermit nachgesehen — `git log` und die
`## Verlauf`-Einträge der fünf Stories erneut gelesen: keine der fünf Änderungen zeigt einen Rest-Fehler,
der beim Bauen nicht schon gefunden wurde (B-112s eigener Fund ist bereits dort dokumentiert und
behoben). `nachgeschaut: 2026-08-06` würde auf allen fünf gesetzt, sobald der Index das nächste Mal läuft.

**Was dieser Sprint über die eigenen Tore gelernt hat:** Die neue Fünf-Fehlversuche-Regel hat sich sofort
bewährt — zwei echte, aber kleine Funde kamen durch, wurden behoben, und die Nacht lief weiter, statt beim
ersten (harmlosen) Fund zu enden. Der `frontend-reviewer`-Fund zeigt zugleich eine eigene Lektion: eine
Kommentar-Begründung, die plausibel klingt (Virenscanner), aber nie gegen die tatsächliche Bibliotheks-
Implementierung nachgerechnet wurde, ist dieselbe Fehlerklasse wie B-112 aus Sprint 1 — eine Behauptung,
die niemand verifiziert hat, bevor sie als Kommentar stehen blieb.

**Kein neuer Mechanismus.** Beide Funde wurden durch die bestehenden Reviewer gefangen, bevor irgendetwas
`abgenommen` wurde — die Tore haben funktioniert. Kein Vorschlag für diesen Sprint.

## Ende von Nachtlauf-2-Sprint-2

Vier Stories gebaut und abgenommen. Zwei Review-Funde, beide behoben, Zähler bei 2 von 5 — kein
Abbruchgrund. Weiter mit Sprint 3 (CI/Deploy-Tooling: B-25, B-47).

## Nachtlauf-2-Sprint 3 — Ziel & Umfang

**Sprint-Ziel:** *Ein frischer Checkout oder CI-Runner installiert die Frontend-Abhängigkeiten ohne einen
vergessenen Sonderschalter.*

**Umfang:** **nur B-25** — B-47 (Deploy-Artefakt-Smoke) bleibt vor dem Bauen erneut geprüft `geschaetzt`:
seine eigene Entscheidung 1 verlangt, dass der `workflow_run`-Block in `deploy-azure.yml` wieder scharf
ist, bevor gebaut wird. Nachgesehen: der Block ist weiterhin auskommentiert
(`.github/workflows/deploy-azure.yml:28-31`, wartet auf die Azure-Reaktivierung aus B-33/B-07). Kein
Verstoß gegen die Eintrittsbedingung — B-47 wartet auf ein externes Ereignis, ist also kein Fall für
diesen Sprint.

**Entwickler-Brief:** `vite-plugin-pwa` auf `^1.3.0` anheben (löst den Peer-Konflikt mit `vite@8`), das
Flag `--legacy-peer-deps` aus den drei Workflow-Dateien und der zugehörigen Doku entfernen. Kein
Backend-Anteil.

## Nachtlauf-2-Iteration 3 — umgesetzt

`frontend/package.json`: `vite-plugin-pwa` `^0.21.1` → `^1.3.0`, Lockfile neu erzeugt. `--legacy-peer-deps`
entfernt aus `ci.yml`, `deploy-azure.yml`, `e2e.yml`; `frontend/CLAUDE.md`, `frontend/vitest.config.ts`
und `docs/deployment-azure.md` (Fallstrick 1, direkt von einem entfernten Workflow-Kommentar verwiesen)
nennen den Konflikt nicht mehr als aktuellen Zustand.

**Verifikation (gemessen):** `npm install` ohne Flag → clean, kein `ERESOLVE`. `npm run build` →
`PWA v1.3.0`; die vite-plugin-pwa-Bundle-Warnung, die diese Nacht bei **jedem** Build erschien, ist als
Nebeneffekt verschwunden. `npm test -- --run` → **153/153 grün**. `frontend-reviewer` lief gegen den
Diff, kein Blocker.

## Runde — Abnahme Nachtlauf-2-Sprint-3 (Rollengang)

Reines Tooling ohne Laufzeit-Effekt auf eine der drei Rollen — kein Rollengang nötig, Regression über
die grüne Suite und den Reviewer belegt.

## Retrospektive — Nachtlauf 2, Sprint 3

**Nachschau:** Sprint 2 (B-88/27/55/58) nachgesehen — keine der vier Stories zeigt einen Rest-Fehler
über die bereits im Sprint dokumentierten und behobenen zwei Funde hinaus.

**Kein neuer Mechanismus.** Reine Abhängigkeitspflege, kein Prozess-Fund in diesem Sprint.

## Ende von Nachtlauf-2-Sprint-3

Eine Story gebaut und abgenommen, eine bewusst nicht angefasst (wartet extern). Kein Review-Fund —
Zähler bleibt bei 2 von 5. Weiter mit Sprint 4 (Typsicherheit: B-59, B-74, B-49).

## Nachtlauf-2-Sprint 4 — Ziel & Umfang

**Sprint-Ziel:** *Drei bislang lose Typisierungslücken werden vom Compiler statt von Testabdeckung allein
gehalten: der Vertrag sagt selbst, welche Status-/Scope-Werte möglich sind, der Übungs-Editor schlägt bei
einem vertauschten Feld sofort fehl, und die Sohn-Arcade teilt sich mit dem Vater-Web dieselbe
Wiedereintritts-Sperre.*

**Umfang:** B-59 (Status-Strings → Enums, Vertragsbruch), B-74 (Editor-Zeilen typisieren), B-49
(Sohn-App auf `useAction`). Backend zuerst (B-59), dann Frontend (B-59-Rest, B-74, B-49).

**Entwickler-Brief:** B-59 ändert das Wire-Format dreier Felder (Kleinschreibung → PascalCase), aber keine
sichtbare Bedeutung – die deutschen Anzeigetexte bleiben identisch. B-74 und B-49 ändern kein Verhalten,
nur die interne Absicherung. Testweg: rote Proben für jede der drei Stories einzeln (Backend-Assertions,
`tsc`-Mutation, unverändertes `next()`-Verhalten), dann die volle Suite.

## Nachtlauf-2-Iteration 4 — umgesetzt

**B-59 (Backend):** drei neue Enums (`GoalStatus`, `KeyResultScope`, `BatchItemStatus`),
`ObjectiveEvaluationService.StatusOf`/`ObjectiveService.KrScope` geben sie direkt zurück,
`VocabularyStoreController` schreibt Enum-Werte. Zehn Testassertions auf PascalCase gehoben (eine mehr als
geschätzt, beim tatsächlichen roten Lauf gefunden). **B-59 (Frontend):** `contract.ts` neu erzeugt,
`GoalStatus`-Hand-Typ entfernt, `VaterZiele.tsx`/`MyObjectives.tsx`/`VaterVocab.tsx` auf PascalCase
gehoben; ein bislang verstecktes E2E-Risiko (`vater-von-null.spec.ts` prüfte wörtlich „subject") gefunden
und behoben, bevor es rot geworden wäre.

**B-74:** ~20 Zeilen-/Extra-Schnittstellen für `exerciseConfig.tsx`, `satisfies`/Rückgabetyp-Annotationen
in allen vier Kernfunktionen, öffentliche Signaturen unverändert.

**B-49:** alle vier Sohn-Schreibstellen auf `useAction`; `judge`/`reshuffleImage` teilen eine Instanz.
Nebenbefund: drei Knöpfe ohne jedes `disabled={busy}` gefunden und nachgezogen. Zwei neue
Doppelklick-Zusicherungen (`shop-verlauf.spec.ts`, `full-flow.spec.ts`).

**Verifikation (gemessen):** `dotnet test Pugling.sln -c Release` → **746/746 grün**. `npm run build`
clean. `npm test -- --run` → **153/153 grün**. `npm run test:e2e` → **27/28 grün** (einziger Ausfall:
der vorbestehende B-109-Flake, an derselben Stelle wie vor dieser Nacht).

**Drei rote Proben, alle mit Zahl belegt:** B-59s sechs rot gewordenen Testmethoden (Erwartet/Gemessen je
Zeile in B-59s `## Verlauf`), B-74s `tsc`-Mutation (`TS2551` auf den erfundenen Feldnamen), B-49s
gegengeprüftes `next()`-Verhalten (Code gelesen, nicht nur behauptet).

## Runde — Abnahme Nachtlauf-2-Sprint-4 (Rollengang)

- **Sohn: Regression, mit zwei neuen E2E-Nachweisen.** `shop-verlauf.spec.ts` und der erste Durchlauf von
  `full-flow.spec.ts` (bis zum vorbestehenden B-109-Hänger) fahren echte Doppelklicks gegen den echten
  Server und zählen die abgeschickten POSTs – das ist der Rollengang für B-49s Kern (die Sperre wirkt
  tatsächlich, nicht nur im Code gelesen). B-59/B-74 ändern für den Sohn nichts Sichtbares (dieselben
  deutschen Texte), belegt durch die grüne Gesamtsuite.
- **Vater: Regression.** `VaterZiele.tsx` zeigt weiterhin dieselben Status-Pillen, `uebungstypen.spec.ts`
  bleibt grün (deckt alle elf Typen inkl. `exerciseConfig.tsx`).
- **Creator: Regression.** Kein Katalog-Endpunkt geändert; `BatchItemResult`s neues Enum ändert nur die
  Wire-Form, nicht die Bedeutung.

## Retrospektive — Nachtlauf 2, Sprint 4

**Nachschau:** Sprint 3 (B-25) nachgesehen — keine Entgleitung über das bereits Dokumentierte hinaus.

**Was dieser Sprint über die eigenen Tore gelernt hat:** Zwei Funde in dieser Nacht (B-59s zusätzliche
VocabAgentApiTests-Zeile, B-49s drei ungesperrten Knöpfe) kamen nicht aus der Story-Recherche, sondern aus
dem tatsächlichen Ausführen der roten Probe bzw. dem genauen Lesen des Codes beim Bauen — derselbe Punkt,
den `docs/backlog/README.md` schon mehrfach macht: „Ausformulieren heißt gegen den Code belegen", nicht
gegen die Notiz. Beide wurden sofort behoben, nicht als Fund für eine Folge-Story liegen gelassen, weil sie
im **eigenen** Increment lagen.

**Kein neuer Mechanismus.** Die bestehenden Tore (rote Probe vor jedem Fix, `tsc` als Wächter für B-74,
volle Suite nach jeder Story) haben gehalten.

## Ende von Nachtlauf-2-Sprint-4

Drei Stories gebaut und abgenommen, zwei kleine Nebenfunde sofort behoben (kein neuer Review-Fund).

## Ende des Nachtlaufs 2

Alle vier geplanten Sprints sind durch, kein Abbruchgrund ist eingetreten. Der Fünf-Fehlversuche-Zähler
lief in keinem Sprint über 2 von 5 – die neue Regel (Review-Fund sofort analysieren, beheben oder als
`Defekt` im selben Sprint bearbeiten) griff genau zweimal (Sprint 1: B-112-Kommentar, Sprint 2: EBUSY-
Kommentar + `assistent.spec.ts`-Klick-Bug) und beide Male sofort per Selbstheilung, ohne dass eine
separate Defekt-Story nötig wurde. Sprint 4 kam mit zwei eigenen Nebenfunden (nicht vom Reviewer, sondern
beim Bauen selbst entdeckt) ebenfalls sauber durch.

**Bilanz der Nacht (9 Stories abgenommen):**

| Sprint | Thema | Stories | Rollengang |
|---|---|---|---|
| 1 | Rollen-/Bezeichner-Konsistenz | B-44, B-32, B-51, B-83, B-112 | Regression (Suite + `frontend-reviewer`) |
| 2 | Testsuite-Qualität & Determinismus | B-88, B-27, B-55, B-58 | `assistent.spec.ts` (echter Browser) |
| 3 | CI/Deploy-Tooling | B-25 (B-47 bewusst nicht angefasst, wartet extern) | Regression |
| 4 | Typsicherheit stärken | B-59, B-74, B-49 | `shop-verlauf.spec.ts` + `full-flow.spec.ts` (echter Browser) |

**Was die Nacht über den eigenen Prozess gelernt hat:** die Fünf-Fehlversuche-Regel hat genau das getan,
wofür sie gedacht war – ein Review-Fund ist ein normaler Arbeitsschritt innerhalb eines Sprints, kein
Abbruchgrund, solange er sich nicht zur Endlosschleife häuft. Der ursprüngliche „ein Fund beendet die
Nacht"-Reflex aus der Erprobungsphase hätte die drei produktiven Sprints 2–4 verhindert.

**Stand danach:** Backlog-Index zeigt 62 abgenommene Stories (59 vor der Nacht + B-59/B-74/B-49; B-25 und
die fünf Sprint-1-Stories waren zum Zeitpunkt des letzten Index-Laufs vor Sprint 4 bereits gezählt). Alles
liegt lokal auf `main`, nichts wurde gepusht – das bleibt beim Nutzer. `docs/nachtlauf.md` trägt die
überarbeitete Freigabe 3 (mehrere Sprints, Fünf-Fehlversuche-Zähler) für den nächsten Lauf.
