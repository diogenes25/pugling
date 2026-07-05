# PM-Sitzung: Funktions-Sichtung nach dem Lehrplan-Umbau

**Datum:** 2026-07-05  ·  **Moderation:** PM
**Teilnehmer:** Vater (steuert) · Sohn (~11, 5. Klasse, Französisch) · Entwickler
**Ziel:** Nach dem großen „Lehrplan-Umbau" (Plan = Container aus `PlanPosition`s statt plan-weitem
`StudyPlanItem`/`Method`) am **laufenden** Produkt prüfen, ob die App ihren Zweck noch erfüllt:
Kern-Loop, Sohn-Erlebnis, Vater-Steuerung. Reibung fixen, bis beide Rollen am realen Produkt abnehmen.

> Doppelspur-Hinweis (unverändert): die Claude-Skills `vater`/`sohn` sind das dateibasierte Kursformat,
> **nicht** das hier geprüfte Produkt.

---

## Runde 1 — Sichtung & Befund (am echten Stand verankert)

### Statische Ebene — gesund
- **Backend** baut fehlerfrei (0 Warn/0 Err bis auf vorbestehende CS8604 in `VocabAgentApiTests`),
  keine toten Referenzen auf die gelöschten Controller/Entities (`TestsController`,
  `MatchingTestsController`, `ClozeTestsController`, `PracticeSessionsController`, `RatingsController`,
  `RatingEntities`), DI vollständig, EF-Modell in Sync (keine ausstehende Migration).
- **Frontend** baut (tsc + vite), API-Client vollständig auf das Positions-Modell migriert, keine
  Aufrufe gegen gelöschte Endpunkte.
- **Unit/Integration:** 163/163 grün.
- **Fazit:** Die Löschungen spiegeln *entfernte Legacy-Funktionalität*, keine Kompilier-Regressionen.

### Funktionale Ebene — zwei P0-Befunde am laufenden Produkt

**1. Kern-Loop war kaputt: der Selbsteinschätzungs-Test ist unlösbar.**
Der Umbau schickt im neuen `PositionTestsController.ToItem` bei Selbsteinschätzungs-Stufen die Lösung
vorab mit (`Reveal = item.Answer`, [PositionTestsController.cs:49](../backend/Pugling.Api/Controllers/Learn/PositionTestsController.cs#L49)).
In [SohnTest.tsx](../frontend/src/sohn/SohnTest.tsx) hingen die „Gewusst/Nicht gewusst"-Buttons an
`revealed.has(...)`, das nur der „Aufdecken"-Klick setzt — und der „Aufdecken"-Button erscheint nicht
mehr, sobald `it.reveal` gefüllt ist. **Folge:** Der Sohn sieht die Lösung, hat aber **keine
Bewertungs-Buttons** → nur „Abgeben (0/N)" → **jeder Vokabeltest scheitert mit 0 %**. Damit war der
zentrale Kreislauf (üben → Test bestehen → Zielpunkte/Münzen) für den Normalfall tot.

**2. Das Sicherheitsnetz war blind: der E2E-Loop war seit dem Umbau rot — unbemerkt.**
Der Plan-Anlage-Flow wurde grundlegend umgebaut (früher: Assistent mit Vokabel-Auswahl → „Lehrplan
erstellen" inkl. Positionen; jetzt: leerer Container → Positionen referenzieren **Katalog-Übungen**).
Der Playwright-E2E ([full-flow.spec.ts](../frontend/e2e/full-flow.spec.ts)) bildete noch den alten
Assistenten ab und starb bereits beim Vokabel-Anlegen — der eigentliche Loop wurde **gar nicht** mehr
geprüft. Deshalb blieb P0-Befund 1 (und hätte jede weitere Regression) unentdeckt.

### Feedback Sohn (O-Ton)
„Ich hab den Test gemacht und **immer verloren, egal was ich wusste** — es gab gar keinen ‚Gewusst'-Knopf.
So spar ich nie Münzen." (Nach dem Fix: „Jetzt zählt's wieder.") Klein: die Test-Überschrift heißt nur
noch „Test", früher „Tagestest".

### Feedback Vater (O-Ton)
„Wenn mein Sohn keinen Test bestehen kann, ist die App wertlos — das muss laufen." Der Fortschritts-Beleg
(Punkte gesamt, Tagesverlauf) steht. Offen bleibt sein alter Wunsch: der **Lern-Report je Vokabel**
(„was sitzt/sitzt nicht") — dessen Endpunkt (`GET study-plans/{planId}/report`) hat der Umbau **entfernt**;
das neue `overview/progress` liefert nur Tag-für-Tag-Aggregat.

---

## PM-Synthese & Priorisierung (→ Entwickler)

**Beobachtung:** Der Umbau ist strukturell sauber, aber die **funktionale Absicherung ist ihm nicht
gefolgt**: der eine automatische End-to-End-Beleg (E2E) war tot, und dahinter versteckte sich ein
echter Loop-Bruch. Roter Faden dieser Runde: **den Kern-Loop wieder beweisbar machen** — Bug fixen
UND das Sicherheitsnetz auf das neue Modell heben.

| Prio | Item | Größe | Wo | Status |
|------|------|-------|----|--------|
| **P0** | Selbsteinschätzungs-Test unlösbar (Bewertung an sichtbare Lösung koppeln) | S | Frontend | **behoben** |
| **P0** | E2E-Loop auf Positions-Modell umschreiben (Guardrail wiederherstellen) | M | E2E | **behoben** |
| P3 | Rest: ungenutzter Typ `LearningMethod` | XS | Frontend | **behoben** |
| **P1** | Vater-Mastery-Report je Vokabel — Endpunkt beim Umbau entfernt, neu bauen (Backend + UI) | M | Beide | Roadmap |
| P3 | Sohn-Test-Überschrift wieder kindgerecht („Tagestest") | XS | Frontend | Roadmap |
| P3 | E2E deckt Combo ×5 nur bei ≥5 fälligen Karten ab (seeded Übung hat 3) — bewusst reduziert | XS | E2E | Roadmap |
| P2 | Sohn-Abwechslung (Tempo-Modus, Buchstaben-Kästchen, Tipp) | L | Beide | Roadmap |
| P3 | Baseline: PIN-Hash/Rate-Limit, ValueComparer für JSON-Listen | S–M | Backend | Roadmap |
| — | Geräte-Vorbehalt Sound/Haptik (aus Iteration 6) | — | Gerät | offen |

---

## Iteration 7 — umgesetzt (P0 ×2 + Rest)

**Fix 1 — Selbsteinschätzungs-Test ([SohnTest.tsx](../frontend/src/sohn/SohnTest.tsx)):**
Die Bewertungs-Buttons erscheinen jetzt, sobald die Lösung sichtbar ist
(`revealed.has(itemIndex) || it.reveal`) — deckt beide Stufen ab: „Zeigen" (Lösung + Bewertung sofort)
und „Selbstcheck" (erst nach „Aufdecken"). Rein additive, minimale Änderung; keine Backend-/Contract-Änderung.

**Fix 2 — E2E auf Positions-Modell ([full-flow.spec.ts](../frontend/e2e/full-flow.spec.ts)):**
Neuer Flow: Vater legt leeren Plan-Container an → referenziert die system-geseedete Vokabel-Übung
„Begrüßungen" als Position (Tagesziel + Leitner) → Sohn übt alle fälligen Karten → besteht den Test →
Münzen > 0 → Vater sieht „Punkte gesamt > 0" und den als „komplett" markierten Tag. Veraltete
Assertions ersetzt (Batch-Banner „…angelegt", Test-Überschrift, „bestanden %" → „komplett"), Race beim
Kind-Dropdown durch explizites Warten/Wählen entschärft.

**Rest:** ungenutzter Typ `LearningMethod` aus [types.ts](../frontend/src/lib/types.ts) entfernt.

**Verifikation (real):**
- **E2E grün:** `npm run test:e2e` → **1 passed (21.9 s)** — der komplette Vater→Sohn-Loop läuft
  wieder end-to-end durch die echte App (inkl. bestandenem Test = direkter Beleg für Fix 1).
- **Frontend-Build sauber** (tsc + vite; nur bekannte, nicht-fatale vite-plugin-pwa/Rolldown-Warnung).
- **Backend 163/163 grün** (unverändert; Änderung war rein Frontend).
- Änderungsumfang: 3 Dateien (`SohnTest.tsx`, `full-flow.spec.ts`, `types.ts`).

---

## Runde 2 — Abnahme

**Sohn:** Der Test ist wieder bestehbar, Münzen und Feier fließen — am grünen E2E belegt.
→ **Abgenommen** (Loop funktioniert). Offen/nicht-blockierend: kindgerechte Überschrift „Tagestest",
Abwechslung (#2); Klang/Haptik weiter unter Geräte-Vorbehalt.

**Vater:** Der Kern-Loop seines Sohns funktioniert wieder, der Wirkungs-Beleg steht, seine in Sitzung 1
abgenommenen Features (Rewards, Klassenarbeiten, Konto, Plan-Bearbeiten) haben den Umbau überlebt.
→ **Abgenommen** für die Loop-Gesundheit. **Bedingung fürs nächste Mal:** der Mastery-Report je Vokabel
kommt zurück (jetzt nicht nur UI, sondern auch der Endpunkt fehlt) — als nächstes P1.

### ✅ Ergebnis: Kern-Loop wieder beweisbar (E2E grün, 163/163, Build sauber) — beide abgenommen; ein P1 (Mastery-Report) und Politur-Reste bewusst auf die Roadmap.

---

## Offene Roadmap (nach dieser Sitzung, priorisiert)

1. **P1 — Vater-Mastery-Report je Vokabel:** durch den Umbau entfallener Endpunkt neu bauen (aus
   `PositionItemProgress`: Box/Trefferquote/„sitzt") + Vater-UI. Vom Vater erneut eingefordert.
2. **P2 — Sohn-Abwechslung:** Tempo-Modus, echte Buchstaben-Kästchen, Tipp-Knopf (`cards`/`hint`).
3. **P3 — Politur:** Test-Überschrift „Tagestest"; E2E-Combo-×5-Abdeckung wieder herstellen (Übung mit
   ≥5 Inhalten referenzieren); Mehr-Kind-Tagesdashboard.
4. **P3 — Baseline:** PIN-Hash/Rate-Limit, ValueComparer für JSON-Listen.
5. **Geräte-Vorbehalt:** Klang/Haptik am echten Handy gegenhören (aus Iteration 6).

## Konkreter Änderungsstand dieser Sitzung (für Review)
- **Frontend:** `sohn/SohnTest.tsx` (Bewertung an sichtbare Lösung gekoppelt — P0-Bugfix),
  `e2e/full-flow.spec.ts` (Loop auf Positions-Modell umgeschrieben), `lib/types.ts` (`LearningMethod` entfernt).
- **Kein Backend, keine Migration.** Absicherung über grünen E2E + Frontend-Build + Backend-Suite (163/163).

---

## Nachtrag — komplette Roadmap-Abarbeitung (Wellen A–D)

Auf Wunsch anschließend alle offenen Roadmap-Punkte umgesetzt, je Welle verifiziert und committet.

**Welle A (b6194f4):** Sohn-Test-Überschrift zurück auf „Tagestest"; E2E referenziert die 5-Vokabel-Übung
„Vokabeln: En ville" und erzwingt den Combo-Meilenstein ×5 (kein stiller Skip mehr).

**Welle B — Baseline-Härtung (f8ce710):**
- **PIN-Hash** (`PinHasher`, PBKDF2/SHA-256 + Salt) statt Klartext; Login verifiziert per Hash, mit
  Klartext-Fallback für Alt-Konten. Seed und Vater-/Kind-Anlage hashen mit.
- **Login-Rate-Limit** (Policy „login", 10/min pro IP → 429) gegen PIN-Brute-Force; per Konfiguration
  abschaltbar (Tests).
- **JsonValueComparer** an allen JSON-Spalten — schließt den ValueComparer-Fallstrick; reine Metadaten.
- Neue Tests `SecurityHardeningTests` (PinHasher, Rate-Limit, gehashter Seed-Login).

**Welle C — Sohn-Abwechslung (b2c9d58):**
- **Tipp-Knopf** (Hinweis erst auf Wunsch), **LetterBoxes**-Komponente (Buchstaben-Kästchen in Übung +
  Test), **Tempo-Modus** (persistierter Toggle + Countdown-Leiste; Schnell-Bonus zählt serverseitig).
- Klang-/Tippgefühl bleibt unter **Geräte-Vorbehalt** (subjektiv, nicht automatisiert prüfbar).

**Welle D — Mehr-Kind-Tagesdashboard (Commit folgt):**
- Backend-Aggregat `ChildrenDashboardService` + `GET children/daily-overview` (je Kind: Tagesziele
  erfüllt?, Punkte heute, geübt?) auf Basis des Positions-Tages-Rollups.
- „Heute"-Sektion im Vater-Dashboard mit Status-Ampel je Kind. Test `ChildrenDashboardTests`.

**Verifikation Wellen A–D:** Backend **170/170 grün**, Frontend-Build sauber, E2E-Loop grün.

### Offen / bewusst nicht automatisiert
- **Geräte-Vorbehalt Sound/Haptik** (aus Iteration 6) **und** das neue Tempo-/Tippgefühl: einmal am
  echten Handy gegenhören/-fühlen. Kein Maschinentest kann das abnehmen.
