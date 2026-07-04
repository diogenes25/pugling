# PM-Sitzung: Produktvorstellung & Feedback-Iteration

**Datum:** 2026-07-04
**Moderation:** PM
**Teilnehmer:** Vater (steuert Lernerfolg), Sohn (~11, 5. Klasse, Französisch), Entwickler
**Ziel:** Ersten funktionalen Stand vorstellen, Feedback beider Rollen aufnehmen, an den
Entwickler geben und iterieren, **bis beide zufrieden sind**.

Grundlage der Vorstellung ist die **echte App** (React-Frontend `frontend/` gegen die
`api/v1`-Backend-API). Hinweis fürs Protokoll: Die Claude-Skills `vater`/`sohn` sind ein
*paralleles*, dateibasiertes Kurs-Format und **nicht** Teil des vorgestellten Produkts — das
wurde als bekannte konzeptionelle Doppelspur notiert (siehe Backlog B-9).

---

## Runde 1 — Vorstellung & Feedback

### PM-Vorstellung (Kurzfassung, was demonstriert wurde)
- **Vater-Web** (`/vater`): Login, Kinder anlegen (Name/Klasse/PIN) + Punktestand, Vokabel-Store,
  Lehrplan-Assistent (Kind → „Wo hakt es?" → Vokabeln → Feinschliff → erstellen), Plan-Detail mit
  Punkte/Tage/Streak, Tagesverlauf und Leitner-Boxen.
- **Sohn-Arcade-PWA** (`/sohn`): Helden-Login, HUD (Avatar/Münzen/Streak), Tagesmission mit
  Maskottchen-Stimmungen, Üben (Karteikarte + Combo + Feier-Animationen + Münz-Toast), Tagestest
  (Prozent-Ring, ✅/❌-Liste), Trophäenweg (Ligen/Boxen/Kalender/Badges), Skins.
- **Server-autoritatives Scoring** (Anti-Cheat), Missionen/Auszeichnungen im Backend.

### Feedback Vater (O-Ton, gekürzt)
**Gefällt:** Lehrplan-Assistent („ich will steuern, nicht basteln"); server-vergebene Punkte
(fälschungssicher = Grundvoraussetzung); Plan-Detail als Beleg der Wirkung; die Arcade zieht das Kind rein.

**Stört / fehlt:**
- **Klassenarbeiten** nicht anlegbar — „der eigentliche Ernstfall entscheidet die Note".
- Kein **Lern-Report**, welche Vokabel sitzt/nicht sitzt (wird berechnet, aber nicht gezeigt).
- **Belohnungen/Missionen** nicht selbst festlegbar — „mein Motivationshebel, beim Sohn aktuell leer".
- Plan nach Anlage **nicht änderbar/verlängerbar/abschaltbar**; Kind-Stammdaten nicht korrigierbar.
- Keine **Tages-/Mehr-Kind-Übersicht** („wer hat gestern was verpasst?").

**Top-3 (K.-o.-Kriterien):**
1. Klassenarbeiten anlegen, vorbereiten, nachüben.
2. Belohnungen/Missionen selbst festlegen.
3. Pläne nachträglich ändern/verlängern.

### Feedback Sohn (O-Ton, gekürzt)
**Mega:** Helden-Login; **Combo** (⚡, fliegender Ninja ab ×10); knuffiges Maskottchen +
Münzen sammeln; „SIEG!"-Prozent-Ring.

**Nervt / verarscht:**
- **Skin-Kauf ist Fake**: Münzen gehen nicht runter, auf anderem Gerät ist der Skin weg. „Dann spar ich nie wieder."
- Manches Üben zählt **0 Münzen/keine Combo** ohne Erklärung (fühlt sich kaputt an).
- **Kein Sound / kein Vibrieren**; **keine Feier** bei Mission/Badge.
- Teure Skins nur ein großes Emoji, nicht gezeichnet.
- Üben immer gleich → langweilig; kein Tempo-Modus, kein Level-Pfad, keine Buchstaben-Kästchen.

**Top-3 Wünsche:**
1. **Skins richtig kaufen** (Münzen weg, Skin bleibt, geräteübergreifend).
2. **Sound + Haptik + „TADAA"-Moment** bei Combo/Sieg/Badge.
3. **Abwechslung** (Tempo-Modus, Buchstaben-Kästchen mit Tipp-Knopf).

---

## PM-Synthese & Priorisierung (→ Entwickler)

**Beobachtung:** Fast alles, was der Vater vermisst, existiert bereits im Backend, aber ohne
Vater-UI. Fast alles, was den Sohn nervt, ist Frontend-Politur — **außer** dem Skin-Kauf, der ein
echter Produkt-/Korrektheitsfehler ist (der zentrale „Verdienen→Ausgeben"-Kreislauf ist unecht).

**Roter Faden für die Iterationen:** den **Belohnungs-Kreislauf echt machen** —
Vater setzt Belohnung → Sohn verdient Münzen → Sohn gibt Münzen wirklich aus.

| Prio | Item | Größe | Zuordnung |
|------|------|-------|-----------|
| **P0** | Skin-Kauf server-autoritativ (Münzen abbuchen, Besitz/Skin am Kind persistieren) | S–M, API-First | **Iteration 1** |
| **P1** | Missionen/Belohnungen als Vater anlegen (Backend-CRUD existiert → UI + Client) | M, Frontend | **Iteration 2** |
| P2 | Pläne ändern/verlängern/deaktivieren (PATCH/Items existieren → UI + Client) | M, Frontend | Roadmap |
| P2 | Klassenarbeiten-UI (Controller existiert → UI + Client) | L, Frontend | Roadmap |
| P3 | Lern-Report-Ansicht (`/report` existiert → UI) | S–M, Frontend | Roadmap |
| P3 | Sound/Haptik + Mission/Badge-Feier | S, Frontend | Roadmap |
| P3 | Übungs-Abwechslung (Tempo-Modus, LetterBoxes, Tipp-Knopf) | L, Frontend | Roadmap |

**Entwickler-Brief Iteration 1 (P0):** Skin-Kauf serverseitig echt machen.
- Server ist Quelle der Wahrheit für Kosten & Besitz (kein Client-Betrug).
- Kauf = negative Punkte-Buchung (neuer `PointKind`) + Besitz/ausgerüsteter Skin am `Child`.
- Guards: existiert, nicht schon besessen, Deckung; Rollen-/Ownership-sauber (nur der Sohn selbst).
- Frontend zieht Skin-Zustand vom Server; nach Kauf sinkt der Münzstand real.

---

## Iteration 1 — umgesetzt (P0: Skin-Kauf server-autoritativ)

**Backend (API-First):**
- Neuer `PointKind.SkinPurchase`; Skin-Kauf = negative Punkte-Buchung.
- `Child.SelectedSkin` + `Child.OwnedSkins` (JSON) — Besitz/Auswahl persistieren am Kind
  (geräteübergreifend). Migration `SkinOwnership` (Bestandskinder: Starter „pug" vorbelegt).
- `SkinCatalog` als **serverseitige Kosten-Wahrheit** (kein Client-Betrug).
- Endpunkte (Sohn-only): `GET me/skins`, `POST me/skins/{id}/purchase` (Guards: unbekannt→404,
  schon besessen→409, keine Deckung→400; Abbuchung+Freischaltung atomar in einem `SaveChanges`),
  `POST me/skins/{id}/equip`.

**Frontend:** `skins.ts` liefert nur noch den visuellen Katalog; `SohnApp`/`SohnSkins` lesen den
Zustand vom Server, Kauf senkt den Münzstand real, ausgerüsteter Skin gilt geräteübergreifend.

**Verifikation:** neuer Integrationstest `SkinPurchaseTests` (7 Fälle: Start-Skin, Kauf mit/ohne
Deckung inkl. korrekter Abbuchung −2000, schon besessen→409, unbekannt→404, ausrüsten-nicht-besessen→400,
Vater-Zugriff→403). **Gesamte Suite: 87/87 grün.**

---

## Runde 2 — Re-Review

**Sohn:** Wunsch #1 (Skins) erfüllt und getestet („auf Papas Handy eingeloggt und mein Skin war da").
#2 (Sound/Feier) und #3 (Abwechslung) bleiben gewünscht, aber **kein Muss-sofort**.
→ **Für den Moment ZUFRIEDEN**, mit dem Versprechen, dass #2/#3 als Nächstes kommen.

**Vater:** Akzeptiert die Begründung, dass zuerst der Skin-Fehler behoben wurde („einmaliger
Freischuss"). Reihenfolge Missionen → Pläne → Klassenarbeiten okay (Klassenarbeiten als größter
Brocken zuletzt). → **Vorläufig zufrieden**, aber **„wirklich zufrieden" erst, wenn** er alle drei
Dinge selbst in der Oberfläche tun kann (Mission/Belohnung anlegen, laufenden Plan ändern/verlängern,
Klassenarbeit anlegen + nachüben) — **ohne API-Gefummel** — und keine weiteren Sohn-Wünsche vorgezogen
werden, bis seine Drei stehen.

**PM-Entscheidung:** Vaters Bedingung ist konkret und fair. Iterationen 2–4 liefern genau seine
Top-3, in der vereinbarten Reihenfolge, bevor weitere Sohn-Wünsche angefasst werden.

---

## Iteration 2 — umgesetzt (P1: Belohnungen/Missionen als Vater anlegen)

Neuer Vater-Screen **„🏆 Belohnungen"** (`/vater/rewards`): Kind wählen, **Missionen**
(Titel, Ziel-Metrik, Zielwert, Zeitraum täglich/wöchentlich/einmalig, Belohnungsmünzen) und
**Auszeichnungen** (Icon, Titel, Metrik, Schwelle, Belohnung) anlegen, aktiv/inaktiv schalten,
löschen. Bindet an das bereits vorhandene Backend-CRUD (`children/{id}/missions|achievements`) an;
schließt den Kreislauf: Vater setzt Ziel → Sohn verdient Münzen → Sohn gibt sie (echt) aus.
**Verifikation:** `MissionsAdminTests` (Lebenszyklus anlegen→schalten→löschen; Ownership).

## Iteration 3 — umgesetzt (P2: Pläne nachträglich ändern)

Plan-Detailansicht (`/vater/plan/:id`) um **„Bearbeiten"** erweitert: Titel, **Enddatum
(verlängern)**, Neue/Tag, Minuten/Tag, Bestehensgrenze ändern; **Aktivieren/Deaktivieren**;
**Inhalte nachschieben/entfernen**. Bindet an `PATCH /study-plans/{id}` + `…/items` an.
**Verifikation:** `PlanEditTests` (umbenennen/verlängern/deaktivieren; Inhalt hinzufügen+entfernen,
Dublette→400).

## Iteration 4 — umgesetzt (P2: Klassenarbeiten-Bereich)

Neuer Vater-Screen **„📝 Klassenarbeiten"** (`/vater/class-tests`): Kind wählen, Arbeit **planen**
(Titel/Thema/Fach/Termin), **Übungen aus dem Katalog zuweisen** (Suche → zuweisen/entfernen),
**gezielt vorbereiten** (relevante Übungen + Tage bis Termin), **Note nachtragen** (1,0–6,0, setzt
Status „geschrieben"), löschen, sowie ein **„Wiederholen"-Panel** für schwach benotete Arbeiten.
Bindet an den gesamten `class-tests`-Controller an. **Verifikation:** bestehende
`KlassenarbeitenTests` + neuer Test (zuweisen → Note nachtragen → im Vorbereiten & Wiederholen sichtbar).

**Teststand nach Iteration 4:** **92/92 grün**; Frontend-Build (tsc + vite) sauber.

---

## Runde 3 — Abnahme

**Vater (finale Abnahme):** Geht seine drei Bedingungen einzeln durch — Mission/Belohnung anlegen ✅,
laufenden Plan ändern/verlängern (inkl. Inhalte nachschieben) ✅, Klassenarbeit anlegen + nachüben
(inkl. Note nachtragen und Wiederholen) ✅. **„Kein API-Gefummel mehr … Meine Abnahme steht. Ja."**
Gibt die Sohn-Wünsche #2/#3 ausdrücklich für die nächste Runde frei.

**Sohn:** bereits in Runde 2 zufrieden; durch Iterationen 2–4 nichts an seiner Erfahrung verschlechtert,
seine Belohnungs-Panels werden nun (durch die Vater-Missionen) gefüllt.

### ✅ Ergebnis: Beide zufrieden — Iteration abgeschlossen.

- **Sohn:** zufrieden (echter Skin-Kauf); #2/#3 als Nächstes zugesagt.
- **Vater:** wirklich zufrieden (alle drei Top-3-Punkte in der Oberfläche bedienbar), formell abgenommen.
- **Qualität:** 92/92 Integrationstests grün, Frontend-Build (tsc + vite) sauber.

---

## Offene Roadmap (nach dieser Sitzung, priorisiert)

1. **Sohn #2** — Sound/Haptik + „TADAA"-Feier-Moment bei Combo/Sieg/Mission/Badge (vom Vater freigegeben).
2. **Sohn #3** — Übungs-Abwechslung: Tempo-Modus, echte Buchstaben-Kästchen (LetterBoxes), Tipp-Knopf
   (`hint`-Endpunkt existiert schon); zudem Practice-Nutzung des `cards`-Endpunkts (Cloze/Matching üben).
3. **Vater-Komfort** — Mehr-Kind-Tagesdashboard („wer hat heute was geschafft?") und Mastery-Report
   pro Vokabel (`/study-plans/{id}/report` existiert, nur UI fehlt). Vom Vater als nicht-blockierend markiert.
4. **Baseline** (aus Backlog): Login-Härtung (PIN-Hash/Rate-Limit), ValueComparer für JSON-Listen.

## Konkreter Änderungsstand dieser Sitzung (für den Entwickler/Review)

- Backend: `PointKind.SkinPurchase`, `Child.SelectedSkin`/`OwnedSkins` + `SkinCatalog`,
  `MeController` Skin-Endpunkte, Migration `SkinOwnership`.
- Frontend: server-gestützte Skins (`skins.ts`, `SohnApp`, `SohnSkins`); neue Vater-Screens
  `VaterRewards`, `VaterClassTests`; erweitertes `VaterPlanDetail`; API-Client + Types erweitert.
- Tests: `SkinPurchaseTests`, `MissionsAdminTests`, `PlanEditTests`, Klassenarbeiten-Loop-Test.

## Nachtrag — Code-Review-Fixes (gleiche Sitzung)

Nach der Abnahme lief ein High-Effort-Code-Review (8 Finder-Angles). Behobene Korrektheits-Findings:
- **PlanEditForm nullt keine Schwellen mehr:** String-basiertes Formular; leere Zahlenfelder werden
  ausgelassen statt als `0` gespeichert (verhinderte stilles „bestehen ab 0 %").
- **Skin-Kauf nebenläufigkeitssicher:** `Child.ConcurrencyStamp` als EF-Concurrency-Token (Migration
  `ChildConcurrencyStamp`), beim Kauf/Ausrüsten hochgezählt; parallele Zweitbuchung → 409 statt
  Doppel-Abbuchung/Lost-Update. Test `ConcurrencyToken_LaesstZweitenParallelenWriteScheitern`.
- **SohnSkins-Ladefenster:** `ready`-Flag – Kauf/Ausrüsten erst nach geladenem Server-Besitz; kein
  Fehl-Kauf eines besessenen Skins mehr, kein „alles gesperrt"-Flackern.
- **GradeCell:** speichert nur gültige Noten (1,0–6,0), kein `NaN`/Status-Flip; remountet per `key`
  bei externem Notenwechsel (kein veralteter Wert).

Verbleibende Cleanup-/Altitude-Findings (doppelte Skin-Kosten-Quelle, ChildPicker-/RewardManager-
Duplikate, doppelter `me/skins`-Fetch) sind als Refactoring notiert, nicht blockierend. **Teststand: 93/93 grün.**


