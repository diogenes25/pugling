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
