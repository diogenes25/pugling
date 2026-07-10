---
name: supervisor
description: >-
  Drive the Pugling REST API from the SUPERVISOR seat (api/v1/supervisor/*) to steer a child —
  study plans, positions with goals/points, learn-goals, the family shop, missions/achievements,
  class-tests — and read the child's progress; AND, in the same pass, smoke-test that the Supervisor
  surface works and (re)write the verified Supervisor tutorial. Use this whenever the user wants to
  exercise/validate the Supervisor API, set up a child's plan against the running app, or refresh
  docs/tutorial-supervisor.md, e.g. "run the supervisor skill", "test the supervisor API",
  "supervisor", "Lehrplan-API prüfen", "Supervisor-Tutorial aktualisieren". This is NOT the file-based
  Lehrplan format (that is `lehrplan-autor`/`lehrplan-lerner`) — it drives the real product API.
---

# supervisor — der Steuernde (technische Rolle „Supervisor")

Du bist der **Supervisor**: aus Katalog-Inhalten machst du **verbindliche Aufgaben** für ein Kind —
Study-Pläne (Container) mit **Positionen** (Ziel/Rhythmus/Punkte), plan-übergreifende **Lernziele**,
den **Familien-Shop**, **Missionen/Auszeichnungen**, **Klassenarbeiten** — und du **kontrollierst** den
Lernstand. Im Produkt ist das der **Vater** (Seed: *Papa*, `fatherId=1`, PIN `0000`), der zugleich Creator ist.

Zwei Ziele in einem Lauf: **(1)** die Supervisor-Endpunkte end-to-end **verifizieren** und **(2)** daraus
**`docs/tutorial-supervisor.md`** schreiben/aktualisieren. Mängel werden **berichtet**, nicht heimlich gefixt.

## Ablauf

Gegen die **Wegwerf-Instanz** (Port 5280, Temp-DB) über `.claude/scripts/tutorial-api.sh`.

1. **Hochfahren**: `build` → `stop` → `serve` (Hintergrund, `run_in_background: true`) → `wait`
   (siehe `creator`-Skill; Windows-Fallstricke stecken im Helfer). Bei Datei-Lock die Dev-Instanz stoppen.
2. **Einloggen**: `source .claude/scripts/tutorial-api.sh; TOK=$(login_father 1 0000)`.
   Kontrolle: `api_get /api/v1/auth/me` → `role:"Supervisor"`, `fatherId:1`.
3. **Steuerungs-Flow durchspielen** (Status + Schlüsselfelder prüfen):
   - `GET /api/v1/supervisor/children` → das betreute Kind (`id:1`, Coins/Gems).
   - `POST /api/v1/supervisor/study-plans` `{"childId":1,"title":"…","durationDays":10}` → **Container**;
     `isPlayable:true`, wenn aktiv **und** heute in Laufzeit. Merke `id`.
   - `POST …/study-plans/{planId}/positions` — Position auf eine **Katalog-Übung** (Seed z. B. `exerciseId:1`
     = „Begrüßungen") mit `cadence`, `useLeitner`, `requireTypedTest`, `goalThreshold`, `pointsGoalMet`,
     `comboThreshold/comboBonusPoints`, `speedThresholdSeconds/speedBonusPoints`, optional
     `stageSchedule:[{"dayNumber":1,"stage":2},…]`. **Wichtig** (im Lauf aufgefallen): ohne `stageSchedule`
     fällt der Abschlusstest des Kindes auf **Stufe 2 (Selbsteinschätzung)** zurück — auch wenn
     `requireTypedTest:true` gesetzt ist. Für einen echten Tipp-Test eine `stageSchedule` mit Stufe 4 setzen.
   - `POST …/children/{childId}/learn-goals` — **plan-übergreifendes** Beherrschungsziel auf Katalog-Scope:
     `{"subjectId":1,"chapterId":null,"exerciseId":null,"metric":"MasteredPercent","targetValue":80,"title":"…"}`.
     Metrik-Enum: `AvgMastery | Coverage | MasteredPercent | MaxWeakItems` (Feld heißt `targetValue`, **nicht**
     `target`; Scope ist `subjectId`/`chapterId`/`exerciseId`, **nicht** scopeType/scopeId).
   - Familien-Shop: `POST /api/v1/supervisor/shop/articles` `{articleNumber,title,unitType,actionType}` →
     dann `POST …/shop/articles/{id}/listings` `{title,coinPrice,gemPrice,unitsPerPurchase,currentStock,maxStock}`.
   - `POST …/children/{childId}/missions` `{"title":"…","metric":"CorrectReviews","target":10,"period":"Daily","rewardPoints":15}`
     (Metrik-Enum `NewWords|CorrectReviews|TestsPassed|MinutesPracticed|DaysComplete|StreakDays`; Periode
     `Daily|Weekly|OneOff`). Analog `…/achievements`.
   - **Kontrolle über die dual-lesbaren Student-Routen** (ein Vater-Token darf mitlesen): `GET
     /api/v1/student/study-plans/{planId}/overview` (Tagesmission/`outstanding`), `…/positions/{positionId}/report`
     (Box/Beherrschung), `GET /api/v1/student/children/{childId}/vocabulary-progress?onlyWeak=true`.
   - `POST /api/v1/supervisor/children/{childId}/points` `{"amount":30,"reason":"…"}` → manuelle Buchung
     (`kind:"Manual"`).
   - Optional: `POST /api/v1/supervisor/class-tests` (Klassenarbeit planen/benoten) und die
     Aktivierungs-Freigabe `…/children/{childId}/shop/activations/{id}/approve` (Gegenstück zum Student-Kauf).
4. **Tutorial** `docs/tutorial-supervisor.md` schreiben — echte Requests + Schlüssel-Response-Felder, volle
   Bodies per Link auf `docs/api-examples/study-plans.md`/`children.md`/`shop.md`. Frontmatter
   `[typ/tutorial, bereich/training, bereich/auswertung, rolle/supervisor]`, relative Links, „Verwandt"-Footer,
   Brücken-Satz *Vater = Creator + Supervisor*. Für Tiefe auf `wiki/04-lernplan-bauen.md`-Themen aufsetzen
   (das wird zum Stub → dieses Tutorial ist die neue kanonische Fassung).
5. **Herunterfahren & Bericht**: `stop`; kurz grün/rot + Mängel (z. B. der `requireTypedTest`/`stageSchedule`-
   Fallstrick), `pugling.db` unberührt.

## Regeln

- **Rolle sauber halten**: primär `api/v1/supervisor/*`; Student-Routen nur **lesend zur Kontrolle**
  (Ownership-Filter erlaubt das dem Vater). Inhalte anlegen ist Creator, Spielen ist Student.
- **Verifizieren, nicht behaupten.** ASCII-Bodies, relative Temp-DB, Hintergrund-Server.
- **Mängel melden, nicht heimlich fixen.**
