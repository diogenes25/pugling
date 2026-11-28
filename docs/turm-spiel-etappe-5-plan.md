---
tags: [bereich/turmspiel, typ/plan]
---

# Vokabel-Türme, Etappe 5: Rückmeldung des Vaters nach der Handy-Runde

Stand 2026-11-28, **nur geplant, nichts gebaut; Fragen an den Vater entschieden (am Ende)**. Grundlage ist die Rückmeldung des Vaters nach der Runde mit der
Etappe-4-APK („schon ziemlich gut, einige Änderungen“). Umgesetzt wird in einer eigenen Sitzung auf dem Branch
`turmspiel-gdscript` (Stand `ea91c200`). Vorgänger: [turm-spiel-ausbau-plan.md](turm-spiel-ausbau-plan.md),
Technik: [turm-spiel-plan.md](turm-spiel-plan.md).

Sechs Wünsche, nach Aufwand und Abhängigkeit in Schritte geordnet. Schritt 1 und 2 sind unabhängig voneinander und
vom Rest; 3 und 4 berühren beide `words/`, also nacheinander; 5 und 6 berühren beide `level.gd`/`towers.gd`.

| # | Wunsch des Vaters | Kern der Umsetzung | Server? |
|---|---|---|---|
| 1 | Startbildschirm: „Ende“ mit „Willst du aufhören? Ja/Nein“ | Knopf + Rückfrage, auch für die Zurück-Taste | nein |
| 2 | Ritter schlägt mit dem Schwert statt zu schießen | Nahkampf: kurze Reichweite, mehr Schaden, Schlag-Clip | nein |
| 3 | Zu viele unbrauchbare Wörter auf dem Feld | Abwurf an die gefragten Türme koppeln, Deckel fürs Feld | klein (Gruppengröße im Vertrag) |
| 4 | Lager für Wörter, gestaffelte Haltbarkeit | drei Haltbarkeiten: liegen gelassen < abgelegt < eingelagert | nein |
| 5 | Türme sehen mit jeder Stufe besser aus: Pfeil → Stein → Feuerball | Turm-Stufen statt „stark/normal“, Modell und Geschoss je Stufe | nein |
| 6 | Burg ist auch ein Turm, verliert nie ihre Pfeile, steigt langsamer | Burg-Turm mit unbegrenzter Munition und halber Steigung | nein (prüfen) |

---

## Schritt 1 – „Ende“ am Startbildschirm

**Ist:** `screens/home.gd` hat keinen Ende-Knopf. Die Android-Zurück-Taste beendet auf dem Startbildschirm sofort
(`screens/main.gd:86`, `_back = _quit`). Im Level gibt es `ui/pause_menu.gd` mit „Weiter/Aufhören“.

**Soll:**
- Oben rechts neben dem Zahnrad (`home.gd` `_build_top`) ein Knopf „Ende“ (Icon: Tür oder X; ggf. neues
  `Icon.Kind.EXIT` in `ui/icon.gd`).
- Er öffnet eine Rückfrage im Stil des Pause-Menüs: dunkler Schleier, „Willst du aufhören?“, Knöpfe **Ja** (rot)
  und **Nein** (grün). Ja → `get_tree().quit()`, Nein → schließt.
- Die Rückfrage als wiederverwendbares `ui/confirm_dialog.gd` (Titel, Ja-Text, Nein-Text, Signale `confirmed`/
  `cancelled`) – das Pause-Menü bleibt, wie es ist.
- Die Zurück-Taste auf dem Startbildschirm öffnet dieselbe Rückfrage statt sofort zu beenden; ein zweites
  „Zurück“ bei offener Rückfrage schließt sie (wie „Nein“).

**Tests:** `tests/smoke_screens.gd`: Ende-Knopf da → Rückfrage erscheint → „Nein“ lässt den Startbildschirm
stehen; Zurück-Taste öffnet die Rückfrage. `quit()` selbst nicht im Test auslösen (Signal prüfen).

---

## Schritt 2 – Ritter im Nahkampf

**Ist:** `level.gd` `_shoot_hero` feuert alle `HeroLevel.shot_interval` einen blauen Bolzen (`Catalog.HERO_BOLT`)
auf den nächsten Gegner in `Balance.HERO_RANGE` = 5,5. `knight_look.gd` hat keinen Clip für `SHOOT`.

**Soll:** Der Ritter schlägt mit dem Schwert zu – der Spieler steuert damit gezielt, welches Monster fällt
(erwartet: er geht auf die Wortträger).
- Neue Konstanten in `data/balance.gd` statt `HERO_RANGE`/`HERO_SHOT_INTERVAL`: `HERO_MELEE_RANGE` ≈ 1,6,
  `HERO_SWING_INTERVAL` ≈ 0,7 s, `HERO_MELEE_DAMAGE` ≈ 3 (ein Krieger fällt in 1–2 Schlägen, ein Riese nicht).
- Schaden ohne Geschoss: Schlag beginnt (Clip `1H_Melee_Attack_Slice_Diagonal`, abwechselnd `…_Chop`), der Treffer
  wird nach ~0,25 s angerechnet, sofern das Ziel noch lebt und in Reichweite ist. Der Treffer läuft über
  denselben Weg wie ein Projektil-Treffer (Rüstung `EnemyRules.damage_taken`, `Enemies.defeat`, Wortabwurf,
  `score.kill`, `tally.hero_kill`) – dazu die Treffer-Auswertung aus `_move_projectiles` in eine Funktion
  `_on_hit(enemy, damage, shooter, at)` ziehen, die Projektil und Schwert teilen.
- Zielwahl: nächster Gegner in Reichweite; der Ritter dreht sich beim Schlag zum Ziel.
- `HeroLook.Action.SHOOT` → `STRIKE`; `knight_look.gd` spielt den Schlag-Clip. Klang: `punch.wav` oder neuer
  Schwertklang aus `tools/sfx.py`. Funken am Ziel wie beim Projektil-Treffer.
- `HeroLevel`: `shot_interval` → `swing_interval`, `damage_bonus` bleibt (gilt für den Schlag).
- `Catalog.HERO_BOLT` und `data/hero_bolt.tres` entfallen; `RunTally.hero_shots` → `hero_strikes`.

**Leitlinie beachten:** Der Ritter bleibt Plan B und ersetzt die Türme nicht – mehr Schaden, aber nur ein Ziel in
Armlänge, und er muss dafür laufen statt Wörter zu tragen. Gegenprobe in `tests/test_balance.gd`: Ritter allein
hält eine Welle ab Welle 3 nicht.

**Tests:** `tests/test_hero_level.gd` (Intervall/Bonus umbenannt), `tests/smoke_level.gd` (Ritter neben einem
Gegner → Schlag-Ereignis, Gegner verliert Leben; Gegner außerhalb 1,6 → kein Schlag).

---

## Schritt 3 – Mehr brauchbare Wörter auf dem Feld

**Ist und Ursache:** Der Client weiß nicht, welches Wort zu welchem Turm passt (Anti-Cheat). Der Server schickt
die Wörter in Turm-Reihenfolge, nur innerhalb von Sechsergruppen gemischt (`TowerGameService.TokenGroup = 6`), und
der Client lässt zufällig aus den vordersten acht des Pools fallen (`Balance.DROP_WINDOW`). Mit der Zeit laufen die
beiden Reihenfolgen auseinander: richtig zugeordnete Wörter wandern ans Ende des Pools (`TokenPool.consume`),
während die Türme ihre Bedeutungen in der Ziel-Reihenfolge weiterdrehen (`RunSession.next_target`). Dazu fällt
jedes Wort eines Trägers, auch wenn schon zehn unbrauchbare herumliegen – so entstand der „Berg“.

**Soll:**
1. **Abwurf aus den Gruppen der gefragten Türme.** Gruppe eines Wortes = seine Stelle in der Server-Liste / 6,
   Gruppe einer Bedeutung = ihre Stelle in der Ziel-Liste / 6 – beide Listen kommen in derselben Gruppierung vom
   Server, das verrät nichts über die Sechsergruppe hinaus. `TokenPool.draw` bekommt die Menge der Gruppen, die
   die Türme gerade fragen, und zieht bevorzugt daraus; nur wenn dort nichts mehr liegt, aus der Spitze wie heute.
   Erwartung: ≥ 1 von 2 gefallenen Wörtern passt zu einem Turm (heute geschätzt deutlich weniger).
2. **Gruppengröße in den Vertrag.** Der Server schickt `tokenGroup` (heute 6) in `TowerRunResponse`, damit der
   Client die Zahl nicht fest verdrahtet. Additiv, ältere Server ohne das Feld → Client fällt auf heutiges
   Verhalten zurück. Server-Test: Wörter einer Gruppe sind genau die Bedeutungen derselben Gruppe.
3. **Deckel für herumliegende Wörter.** Liegen schon `Balance.MAX_LOOSE_WORDS` (Vorschlag 6) nicht eingelagerte
   Wörter auf dem Feld, lässt ein Träger kein Wort fallen, sondern goldene Funken + ein paar Punkte. So bleibt das
   Feld überschaubar, und der Spieler wird nicht mit Wörtern zugeschüttet, die er nicht verwenden kann.
4. Messung vorher/nachher als Regeltest in `tests/test_token_pool.gd`: simulierter Lauf (5 Türme, 40 Wörter,
   fester Seed), Anteil gefallener Wörter, die zu einem gerade fragenden Turm passen.

**Rote Probe:** Ein Test, der mit dem heutigen `draw` (Spitze statt Gruppe) unter der Schwelle bleibt – sonst trägt
er nichts (siehe CLAUDE.md, „Eine Zusicherung, die den Ausgangszustand prüft …“).

---

## Schritt 4 – Lager und gestaffelte Haltbarkeit

**Ist:** Fallen gelassene Wörter verfallen nach 30 s (`Balance.TOKEN_LIFETIME`) und gehen zurück in den Pool;
vom Ritter abgelegte bleiben **für immer** (`WordBook.placed`).

**Soll (Wunsch des Vaters):** drei Klassen, jede hält länger als die vorige.

| Klasse | Wann | Haltbarkeit (Vorschlag) | Kennzeichen |
|---|---|---|---|
| liegen gelassen | ein Träger hat es fallen lassen, niemand hat es angefasst | 30 s (wie heute) | goldener Ring |
| abgelegt | der Ritter hat es aufgehoben und irgendwo abgelegt | 90 s | weißer Ring |
| eingelagert | der Ritter hat es im Lager abgelegt | 5 min (oder bis Laufende, s. Fragen) | steht im Lager, kein Ring |

- **Lager** je Karte: neues Feld `store_spot: Vector2` in `data/level_def.gd` (vier Karten-`.tres` nachtragen), nahe
  der Burg, nicht auf der Straße. `world/map_builder.gd` stellt dort Zelt, Kisten und Säcke aus dem Paket
  (`tent`, `crate_A_big`, `sack`, `barrel`) und einen Bodenring als Grenze. Radius `Balance.STORE_RADIUS` ≈ 2,5.
- Plätze im Lager: feste Raster-Plätze (Vorschlag 8), damit die Schilder sich nicht überdecken; ist das Lager voll,
  wird nur „abgelegt“.
- Ablegen im Lager-Radius → eingelagert, sonst abgelegt. Der Ablegen-Knopf heißt im Lager „Einlagern“
  (`ui/controls.gd`).
- `WordBook` bekommt statt `placed: bool` eine Klasse (`enum Keep { LOOSE, LAID, STORED }`) und die Lebenszeit je
  Klasse aus `Balance`. Blinken in den letzten 5 s gilt für alle Klassen. Verfall → `pool.give_back` wie heute.
- Ein abgelegtes Wort, das wieder aufgehoben wird, startet beim nächsten Ablegen mit voller Zeit seiner neuen Klasse.
- Wörter im Lager zählen **nicht** zum Deckel aus Schritt 3 – das Lager ist gerade der Ort, an dem man sammeln darf.
- Kleine Lager-Anzeige am Rand (Kiste mit Zahl „3/8“), damit der Spieler sieht, was er gesammelt hat.

**Tests:** Regeltest der Lebenszeiten je Klasse (verfällt nach 30/90/300 s, nicht vorher); `smoke_level.gd`:
Ablegen im Lager → eingelagert, außerhalb → abgelegt, volles Lager → abgelegt.

---

## Schritt 5 – Sichtbare Turm-Stufen: Pfeil → Stein → Feuerball

**Ist:** Zwei Turmarten wechseln sich ab (`Catalog.TOWER_MIX`: Bolzenturm, Katapult). Ein Turm hat eine Stufe aus der
Kraft seiner Wörter (`core/tower_level.gd`, +1 Schaden je Stufe) und unabhängig davon „stark“ ab 30 Schuss oder fünf
Füllungen (`TowerAmmo.is_strong` → `strong_shot`). Das Modell ändert sich nie.

**Soll:** Ein Turm sieht man an, wie gut er ist, und er schießt mit jeder Rangstufe etwas Besseres.
- **Ränge** (sichtbar) über den bestehenden Stufen (Zahl):

  | Rang | ab Stufe (Vorschlag) | Modell | Geschoss |
  |---|---|---|---|
  | 1 Holzturm | 1 | `building_tower_A_blue` | Pfeile (`bolt.tres`) |
  | 2 Steinturm | 3 | `building_tower_B_blue` | Steine im Bogen (`boulder.tres`) |
  | 3 Katapult | 5 | `building_tower_catapult_blue` | Feuerbälle (`fire_boulder.tres`, ggf. größer/heller) |
  | 4 (optional) Meister | 8 | Katapult + Flagge/Glühring | Feuerbälle mit Flächenschaden |

  Das Katapult bleibt also, wie es der Vater gut fand – als Ziel, auf das ein Turm hinwächst. Alle Türme starten
  gleich (`TOWER_MIX` wird ein einzelner Turm mit Rang-Liste); `TowerKind` bekommt `ranks: Array[TowerRank]`
  (neue Ressource: ab Stufe, Modell, Maßstab, Geschoss, Mündungshöhe, Name für die Rückmeldung, Schussabstand).
- **Was die Stufe treibt – Wörter und Abschüsse.** Der Vater: „wie viel er abschießt und wie viele Vokabeln er
  gekriegt hat“. `TowerLevel` zählt neu auch Abschüsse: Kraft = Wortwert + `kills / Balance.KILLS_PER_POWER`
  (Vorschlag 5 Abschüsse = 1 Kraft). Damit die Leitlinie „Lernen ist der einzige Weg zu Fortschritt“ hält: Abschüsse
  allein tragen höchstens bis Rang 1→2 nicht weiter – jeder Rang verlangt zusätzlich eine Mindestzahl richtiger
  Wörter (Vorschlag Rang 2: 2 Wörter, Rang 3: 5, Rang 4: 10). Entscheidung für den Vater, s. Fragen.
- „Stark“ entfällt (`STRONG_AMMO`, `MASTER_REFILLS`, `strong_shot`, `strong_shot_name`, `became_strong`); der
  Rangaufstieg übernimmt seine Rolle in Rückmeldung (`level/feedback.gd`: „Der Turm wirft jetzt Steine!“) und
  Overlay (`ui/overlay.gd`: Stern → Rang-Symbol).
- **Rangwechsel als Moment:** Modell austauschen (`Towers` tauscht `tower.model`, gleiche Position), Staubwolke +
  Goldfunken (`world/fx.gd`), Klang `level_up.wav`, Banner „Steinturm!“. Die Zwischenstufen innerhalb eines Rangs
  bleiben die kleine Belohnung (+1 Schaden, Ring am Schild).
- Abschüsse zählen dem Turm, dessen Geschoss den letzten Treffer setzt (`Projectile` bekommt `tower: Tower`).

**Tests:** `tests/test_tower_level.gd` (Rang aus Stufe, Mindestwörter sperren den Rang, Abschüsse zählen, ohne
Wörter kein Rang 2), `tests/test_balance.gd` neu gerechnet (Rang 3 schießt langsamer, trifft härter),
`smoke_level.gd` (Rangwechsel tauscht das Modell, Geschoss-Art wechselt). Export-Fallstrick: neue
`@export`-Arrays mit Konstruktor vorbelegen, nicht mit Literal (siehe `tests/test_export_pitfalls.gd`).

---

## Schritt 6 – Die Burg als Turm

**Ist:** Die Burg ist nur Ziel (`Balance.CASTLE_LIVES`), sie schießt nicht.

**Soll:** Die Burg ist ein Turm, der nie ohne Pfeile dasteht, aber langsamer aufsteigt.
- Ein zusätzlicher `Tower` an `def.castle()` mit eigener Art `data/castle_tower.tres`: Pfeile, Reichweite ≈ 6,
  gemächlicher Takt. `TowerAmmo` bekommt `unlimited: bool` – die Burg verbraucht nichts, eine falsche Zuordnung
  kostet sie nichts an Munition (sie könnte stattdessen Wortwert verlieren – s. Fragen).
- Die Burg fragt wie jeder Turm nach einer Bedeutung, und ein richtiges Wort lässt sie aufsteigen – mit halbem
  Tempo (`TowerLevel` mit Faktor: `needed(level) × Balance.CASTLE_LEVEL_FACTOR`, Vorschlag 2). Sie bekommt eigene
  Ränge (Pfeile → Feuerpfeile → doppelte Feuerpfeile), ihr Modell bleibt die Burg.
- **Server prüfen:** der Server kennt `TowerSlots = 5`. Die Zuordnung läuft über das versiegelte Item der
  Bedeutung, nicht über einen Platz – ein sechster fragender Turm sollte ohne Server-Änderung gehen. Vor dem Bau
  am Code nachweisen (`TowerGameService.AssignAsync`), sonst Slots auf 6.
- **Balance-Gefahr:** unbegrenzte Munition darf die Leitlinie nicht aushebeln – ohne Wörter muss ein Lauf weiterhin
  kippen. Gegenprobe in `tests/test_balance.gd`: Burg + Ritter ohne Wörter verlieren spätestens in Welle 3;
  Burg-Schaden 1, Takt ≈ 1,2 s als Startwert.

**Tests:** Burg schießt ohne Munitionsabzug, falsche Zuordnung leert sie nicht, Aufstieg braucht doppelte Kraft,
Balance-Gegenprobe wie oben.

---

## Abschluss jeder Umsetzung

- `tests/run_tests.gd` (Regeltests) und die Smoke-Läufe headless grün; je neuer Regel eine rote Probe am richtigen
  Ort (`tests/rote_probe.sh`).
- Server-Teil (nur Schritt 3, ggf. 6): Integrationstest in `Pugling.Api.Tests`, volle Suite im Hintergrund
  (~15 min, kein zweiter `dotnet test` daneben).
- Vor der Handy-Runde `tests/smoke_export.sh` (spielt das exportierte Paket), `.godot/exported` vorher löschen.
- Handy-Runde gegen die Wegwerf-API; Befund und Zahlen hier nachtragen.
- [turm-spiel-ausbau-plan.md](turm-spiel-ausbau-plan.md) auf Etappe 5 verweisen lassen.

## Entscheidungen des Vaters (2026-11-28)

1. **Abschüsse und Stufe:** Abschüsse zählen mit, aber jeder Rang verlangt zusätzlich Mindest-Wörter
   (Rang 2: 2, Rang 3: 5, Rang 4: 10). Ohne Lernen kein Katapult.
2. **Lager:** eingelagert 5 min, abgelegt 90 s, liegen gelassen 30 s; das Lager hat 8 Plätze.
3. **Burg und falsche Wörter:** Ein falsches Wort an der Burg zieht ein Stück Fortschritt zur nächsten Stufe ab
   (nie unter die erreichte Stufe). Vorschlag für die Größe: der Wortwert der Bedeutung.
4. **Deckel fürs Feld:** Ab 6 losen Wörtern lässt ein Träger goldene Funken und ein paar Punkte fallen, kein Wort.
5. **Ritter:** nicht eigens gefragt – es gilt der Vorschlag: er schlägt den nächsten Gegner in Reichweite, der
   Spieler steuert durch Hinlaufen.
