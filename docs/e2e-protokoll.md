# E2E-Integrations-Protokoll: Vater legt an → Sohn arbeitet ab

Ziel: Der Vater erstellt über die **Web-Oberfläche** einen Lehrplan; der Sohn arbeitet ihn über die
**Handy-App** ab. Der Ablauf wird via Playwright durch beide echten UIs getrieben (Backend auf
frischer Wegwerf-DB, echte `pugling.db` bleibt unangetastet). Jede Runde: ausführen → beobachten →
Ursache fixen → wiederholen, bis der Durchstich fehlerfrei ist.

- **Test:** [frontend/e2e/full-flow.spec.ts](../frontend/e2e/full-flow.spec.ts)
- **Konfig:** [frontend/playwright.config.ts](../frontend/playwright.config.ts) — startet Backend (`:5200`,
  `ConnectionStrings__Default` → Temp-DB) und Vite (`:5173`).
- **Ausführen:** `cd frontend && npm run test:e2e`

## Umfang dieses Durchstichs (Vertikale: Vokabeln)

Vater: Login → Vokabeln anlegen → Leitner-Vokabelplan erstellen → Fortschritt sehen.
Sohn: Login → Tagesmission → Leitner-Übung → Tagestest (SelfAssess) → Sieg/Punkte → Wallet/Trophäenweg.

## Runden

<!-- Neue Einträge oben anfügen -->

### Runde 6 — Combo/Sonderpunkte im Backend konfigurierbar (+ Bugfix)
Combo-Schwelle und Bonuspunkte sind jetzt **Plan-Einstellungen** (`StudyPlan.ComboThreshold` /
`ComboBonusPoints`, per EF-Migration `ComboSettings`, Default 5/5 auch für Bestandspläne). Der Vater
setzt sie im Plan-Formular; 0 schaltet den Bonus ab. Der Review-Endpunkt nutzt die Plan-Werte statt
einer Code-Konstante. Neue Integrationstests (`ComboTests`) für „Bonus bei Schwelle" und „aus bei 0".
**Dabei echten Bug gefunden:** Die Combo zählte sich selbst mit (EF-Relationship-Fixup reihte das noch
ungespeicherte Review in `session.Reviews` ein → off-by-one). Fix: Vorstreak **vor** dem `Add` zählen.
57/57 Unit-Tests + E2E grün.

### Runde 5 — Motivations-Animationen (Duolingo-Stil) + Combo-Bonus
Neu: server-autoritative **Combo** (Treffer in Folge, im `PracticeSessionsController` gezählt → cheat-sicher)
mit eskalierendem **Combo-Bonus** (alle 5 → +5/+10/+15 …), zurückgegeben im `ReviewOutcome`. Frontend:
`Celebration`-System in drei Stufen — kleiner Emoji-Pop bei jedem Treffer, mittleres Konfetti-Banner ab
Combo ×5, großer fliegender Kämpfer ab ×10; Test-Sieg feiert groß. Nur transform/opacity,
`prefers-reduced-motion` blendet die Ebene aus. Integrationstests weiter 55/55 grün.
E2E um 5 Vokabeln erweitert und um Assertions für Combo-Banner (×5) + Sieg ergänzt.
Zwei kleine Test-Fixes nötig (Strict-Mode: „COMBO ×5" und „SIEG!" existieren jetzt je zweimal —
inline-Zähler/Ergebnis-Titel **und** Feier-Banner; Locator auf `.cel-title`/`.vtitle` geschärft).
Ergebnis: 3× stabil grün.

### Runde 4 — GRÜN ✅ (3× stabil auf frischer DB)
Assertion auf die Auswertung geschärft (`Punkte gesamt > 0` + Tagesverlauf zeigt „bestanden N%").
Ergebnis: kompletter Durchstich läuft deterministisch durch — Vater legt via Web an, Sohn arbeitet
via App ab (Login → Mission → Leitner-Übung → Tagestest → Sieg), Punkte fließen in die Wallet und in
die Vater-Auswertung. 3 aufeinanderfolgende Läufe grün.

### Runde 3 — Assertion zu unspezifisch
`locator('.table')` traf 2 Tabellen (Inhalte + Tagesverlauf) → strict-mode-Verletzung. Der Flow selbst
war bereits vollständig grün (Test bestanden 100 %, Punkte verbucht). Fix: gezielt auf die
Tagesverlauf-Zeile und die „Punkte gesamt"-Kachel prüfen.

### Runde 2 — **Produktfehler gefunden:** Üben sperrte den Tagestest aus
Nach der Leitner-Übung (alle Karten „gewusst") stieg die Box jeder Karte und `DueOn` rutschte in die
Zukunft. Der Tagestest zog aus demselben „heute fällig"-Pool (`ScheduleService.SelectAsync`) und brach
mit „Für heute stehen keine Inhalte an." ab → der Sohn konnte nach dem Üben keinen Test mehr machen.

**Erkenntnis:** Der Tagestest ist eine *Standortbestimmung*, kein Spaced-Repetition-Trigger. Er darf
nicht am Fälligkeits-Pool hängen.
**Fix (Backend, [TestsController.cs](../backend/Pugling.Api/Controllers/Learn/TestsController.cs)):**
Ist der fällige Pool leer, fällt der Test auf den bereits eingeführten Stoff zurück (sonst alle
Plan-Inhalte). Die Übung bleibt streng Leitner-getrieben. Damit funktioniert jede Reihenfolge
(erst üben, dann testen – oder umgekehrt).

### Runde 1 — Konfig-Fehler (ESM)
`__dirname` existiert im ESM-Projekt nicht → `playwright.config.ts` konnte nicht geladen werden.
Fix: Pfad aus `import.meta.url` ableiten.

## Offene Punkte / Notizen fürs nächste Verfahren

- **Tagesziel-Minuten im E2E:** Der Heartbeat meldet echte Sekunden; in einem schnellen UI-Durchlauf
  wird `DailyMinutesRequired` selten erreicht, daher wird „Tag komplett" nicht immer erzielt. Die
  Punkte-Pfade (Leitner-Übung + Test bestanden) sind aber verifiziert. Für einen dedizierten
  Minuten-/„Tag komplett"-Test entweder die Anforderung klein setzen oder Heartbeats gezielt treiben.
- **Goldene Stunde (2×):** Bewusst noch nicht in der Sohn-App verdrahtet — es fehlt ein
  sohn-lesbarer Endpunkt für die `TimeSlotRule`-Fenster. Kleiner Backend-Zusatz (Read-only) offen.
- **Skins persistent:** Freischaltung/Auswahl liegen aktuell im `localStorage` (pro Kind). Für
  geräteübergreifende Speicherung ein Feld am `Child` (`SelectedSkin`, `UnlockedSkins`) ergänzen.
- **Nächste Vertikalen:** Cloze- und Matching-Tests, Klassenarbeiten-Boss, Tags — analog anschließen.
