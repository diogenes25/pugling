---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Übungstyp „Gleichung lösen“ (Nachtlauf M)

**Datum:** 2026-11-25  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Paket M des Nachtlaufs – den Übungstyp „Gleichung lösen“ ([B-710](backlog/B-710-gleichung-loesen-typ-und-editor.md),
[B-711](backlog/B-711-loesungsmenge-auswaehlen.md), [B-712](backlog/B-712-sohn-eingabe-loesungsmenge.md)) von `geschaetzt` bis
`abgenommen` bauen.

Autonomer Nachtlauf, vom Nutzer freigegeben; einer von drei parallelen Läufen (M, N, O) im eigenen Worktree
(`nachtlauf/2026-11-25-m`, Basis `6fa43808`). Kollisionsschutz: Backlog-Ids nur B-1000…B-1009, Sprints ab 171, API :5241,
Vite :5281; :5200 und die echte `pugling.db` unberührt.

## Runde 1 — Vorstellung & Feedback

Der Lauf ist auf ein vorgegebenes Paket beschränkt; die drei Stories sind seit 2026-11-19 `geschaetzt`, alle Entscheidungen vom
Nutzer bestätigt („nimm für alle Fragen deine Empfehlung“). Eine neue Rollenbegehung zur Story-Findung entfällt darum; die
Rollen sprechen hier aus den Stories, nicht aus einer erfundenen Sitzung.

- **Creator:** „Ich will nur die Gleichung eintragen – die Lösungsmenge rechne ich nicht von Hand und womöglich falsch.“ (B-710)
- **Vater:** „Friedrich steht vor Kapitel IV (quadratische Gleichungen). Heute kann ich nur eine Zahl je Aufgabe abfragen.“
- **Sohn:** „Ich will nicht wissen müssen, dass man Lösungen mit Semikolon trennt.“ (B-712) – und erst auswählen, dann tippen (B-711).

Abhängigkeiten nachgesehen: [B-639](backlog/B-639-dezimalkomma-im-zahlenvergleich.md) (Zahlen-Parser, hart) ist `abgenommen`;
[B-638](backlog/B-638-formeln-in-aufgabentext-anzeigen.md) (Formel-Darstellung, weich) ist ungebaut – die Karte zeigt den Prompt
`$x^2 - 6x + 8 = 0$` als Quelltext, bleibt aber spielbar (B-710 Entscheidung 5, bewusst so in Kauf genommen).

## PM-Synthese & Priorisierung (→ Entwickler)

| Prio | Story | Größe | Wo | Weg |
| --- | --- | --- | --- | --- |
| P2 | B-710 Typ, Löser, Mengen-Wertung, Vater-Editor | L | beides | Sprint 171 |
| P2 | B-711 Stufe „Lösungsmenge auswählen“ | M | backend | Sprint 171 |
| P2 | B-712 Sohn-Eingabe in getrennten Feldern | M | beides | Sprint 171 |

**Roter Faden:** eine Wertungsregel (die Menge im geteilten `AnswerGrader`), drei Formen, sie zu beantworten.

## Sprint 171 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn löst eine vom Vater nur per Koeffizienten angelegte quadratische Gleichung im Tagestest, indem er die
Lösungen in getrennte Felder einträgt oder „keine Lösung“ antippt – und wird richtig gewertet, in jeder Reihenfolge.
**Umfang:** B-710, B-711, B-712.
**Entwickler-Brief:** Quelle der Wahrheit ist der Löser (`QuadraticEquationSolver`), nie die Config – die Config hat kein
Lösungsfeld (`unknown_field`). **Senke** jeder Wertung ist `AnswerGrader.Matches(string?, ContentItem)`: das Atom trägt
`NumericSet`, alle Spielpfade (Üben, Test, Vorschau, Bonus, Mischrunde, Katalog-Check) werten dort. Die Schreibweise einer
Menge entsteht an genau einer Stelle (`FormatSet`) – Lösung, Auflösung und jede Option der Auswahlstufe. Das Kartenfeld
`setAnswer` entsteht an der einen Projektion `CardFacets`. Reihenfolge: B-710 Backend → Editor → B-711 → B-712 Backend →
Komponente. Keine Migration, Vertrag nur additiv.

## Iteration 171 — umgesetzt

**Backend (API-First):**

- `QuadraticEquationConfig { Equations: [ { A, B, C, Tolerance = 0.01 } ] }` in `Pugling.Contracts`; Route
  `…/units/{id}/quadratic-equation` mit `check`; Manifest „Gleichung lösen“, Einheit „Gleichungen“.
- `QuadraticEquationSolver`: exakte Diskriminante in `decimal`, stabile Wurzelformel (Vieta für die kleine Wurzel), sechs
  Nachkommastellen; Anzeige mit den Stellen der Toleranz; LaTeX-Prompt mit `{,}`; Fehlermodelle für die Auswahl.
- Mengen-Atom: `ContentItem.NumericSet` + `NumericAnswer.MatchesSet` (Trenner nur `;`, sortiert paarweise mit Toleranz,
  `keine Lösung` fest, leer nie richtig); `IsMisspelled`/`IsAccentSlip` greifen nicht (`ContentItem.IsNumber`).
- Validierung: `A = 0`, |A| < 0,001, |Koeffizient| > 10⁶, Toleranz < 0, 0 < Toleranz < 10⁻⁶, Toleranz > 1000, zwei durch die
  Rundung verschmolzene Wurzeln, 2·Toleranz ≥ Abstand der Lösungen → `validation_error` (POST und PUT).
- B-711: `QuadraticEquationStage { Direct = 1, Distractors = 2 }`, Vorgabe `Direct`; `Choices` liefert die richtige Menge und
  bis zu drei falsche (Vorzeichen vertauscht, eine Lösung allein, p statt p/2; Auffüller), ohne Doppel und ohne eine, die der
  Grader als richtig nähme; Reihenfolge über FNV-Hash. Der Trenner-Hinweis fällt auf der Auswahlkarte weg (`CardForStage`).
- B-712: `SetAnswer` (additiv) an `PracticeCard`, `TestItem`, `PreviewItem`, `ExtraCard`, gesetzt in `CardFacets` genau dort,
  wo getippt wird und das Atom eine Menge ist.

**Frontend:** Editor-Zeile a/b/c/Toleranz (`inputMode="decimal"`, Komma erlaubt, Lesehilfe „x² − 6x + 8 = 0“), Hin- und Rückweg,
Inhaltsprüfung je Feld; `Mengeneingabe` (zwei Felder, „+“ bis vier, Umschalter „keine Lösung“, Fokus- und Enter-Regeln der
Antwortfläche), Bindung `mengeAntwort`; der Hinweis wird auf `setAnswer`-Karten nicht gezeigt.

**Review:** `pugling-reviewer` – nichts Blockierendes; eingearbeitet: Überlauf bei winzigem A (500 auf jeder Auswahlkarte) →
Untergrenze, Toleranz-Grenzen, verschmolzene Wurzeln, invariante Zahlen in Meldungen, Leer-Wächter in `Choices`, Vorschau-
und Testpfad-Fälle mit `4;2` (vorher prüfte die Vorschau nur `2;4`, das auch ein Textvergleich trifft), `SelfAssessedTest`.
`frontend-reviewer` – nichts Blockierendes; eingearbeitet: Nicht-Zahlen im Editor gingen als `null` an den Server, Fokus aus
dem dritten/vierten Feld fiel beim Kartenwechsel auf `body`, Fokus nach „+“ hing an Microtask-Zeitpunkten. Nicht eingearbeitet
(eigene Story [B-1000](backlog/B-1000-gleichung-vorschau-und-hinweis.md)): Vater-Vorschau mit Feldern, Hinweis am Server.

**Verifikation (nach dem letzten Code-Edit, Commit `a961897b`):** `dotnet test Pugling.sln -c Release` **2542 passed**,
0 failed; `vitest run` **1488 passed** (131 Dateien); `tsc -b` sauber; `dotnet format Pugling.sln` ohne Änderung; Playwright
auf :5241/:5281 **4 passed** (`gleichung-loesen`, `uebungstypen`, `lektion`, `rechnen-mit-komma`). Mutanten-Probe
(`mutant-probe.py`, Debug): Backend 26 Mutanten, im ersten Lauf 16 rot, 2 abweichend (M10 – das Modell „eine Lösung“ trug
zwei Zeilen, der Test verlangte nur „2 oder 4“; M23 – kein Fall, in dem eine anders geschriebene Option in der Toleranz lag),
6 ungemessen (`if (false)` → CS0162), dazu M15 im Nachlauf abweichend (die |A|-Grenze fing `A = 0` mit ab). Tests nachgeschärft
(`Fehlermodelle_Bei_Zwei_Loesungen` pinnt „4“, `Kein_Zweiter_Richtiger_Knopf_In_Der_Toleranz`, `A_Null_Wird_Benannt`) und die
neun Zeilen erneut gemessen: **26 von 26 rot**, Grundlinie danach grün. Frontend 16 Mutanten (Vitest): **16 von 16 rot**.

| Mutante | Stelle | gemessen |
| --- | --- | --- |
| M1 nicht sortiert · M2 Zusatz erlaubt · M3 Toleranz nur erstes · M4 leer = keine Lösung | `NumericAnswer.cs:43–55` | rot |
| M5 Grader umgangen | `AnswerGrader.cs:20` | rot |
| M6 `IsNumber` ohne Menge | `ExerciseContentProvider.cs:73` | rot |
| M7 Doppelwurzel spaltet · M8 `1x` im Prompt | `QuadraticEquationSolver.cs:26`, `:112` | rot |
| M9 Vorzeichen · M10 eine Lösung · M11 p statt p/2 · M12 Auffüller | `QuadraticEquationSolver.cs:86–92` | rot (M10 im 2. Lauf) |
| M13 kein Hinweis · M21 Hinweis auf der Auswahl | `QuadraticEquationExerciseType.cs:124`, `:64` | rot |
| M14–M20 Validierungsgrenzen | `QuadraticEquationExerciseType.cs:152–165` | rot (M15–M20 im 2./3. Lauf) |
| M22 Optionen auf `Direct` · M23 nur Textvergleich · M24 Vorgabe Auswahl | `QuadraticEquationExerciseType.cs:48`, `:81`, `:90` | rot (M23 im 2. Lauf) |
| M25 `setAnswer` überall · M26 auch auf der Auswahl | `PositionPlayService.cs:766` | rot |
| F1–F3 Bindung · F4 Zweig · F5–F12 Fläche · F13–F16 Editor | `mengeAntwort.ts`, `Lektion.tsx`, `Mengeneingabe.tsx`, `exerciseConfig.tsx` | rot |

## Runde 2 — Re-Review / Abnahme

**Rollengang an einer Kopie der echten DB** (`kopie-server.sh` auf :5241, Release, nach dem letzten Code-Edit gestartet; :5200
unberührt), alle drei Rollen über die API, dazu der UI-Weg Vater → Sohn als Playwright-Spec an einer Wegwerf-DB:

- **Creator — abgenommen.** Im Fach Mathe, Reihe „Lambacher Schweizer 9“, neue Unit „Kapitel IV (Rollengang M)“: ein
  Lösungsfeld in der Config → `unknown_field`; `a = 0` → „A must not be 0 …“; vier Gleichungen angelegt (zwei Lösungen,
  keine, gerundet 2,33/5 mit Toleranz 0,05, Doppellösung). Katalog-Check `4;2`, `keine Lösung`, `2,3;5`, `3;3` → 3 von 4,
  wie gewollt. Manifest: „Gleichung lösen“, Stufen „Lösungsmenge auswählen“/„tippen“, `selfAssessedTest = false`.
  „Ich tippe nur die Koeffizienten, und der Check sagt mir, was das Kind eingeben muss.“
- **Vater — abgenommen.** Plan „Gleichungen Rollengang“ für den Testjungen (Kind 4) im Fach Mathe; Stufe 4 (fremd) → 400,
  Position auf „auswählen“ angelegt, später per PATCH auf „tippen“ gestellt. Im Editor (Spec `gleichung-loesen`) steht
  „x² − 6x + 8 = 0“ neben den Feldern, die gespeicherte Config trägt nur a, b, c, Toleranz.
  „Ich sehe die Gleichung, bevor ich speichere – und rechnen muss ich nichts.“ **Zurückgestellt:** im Testmodus probiert er
  noch das Textfeld statt der Felder aus ([B-1000](backlog/B-1000-gleichung-vorschau-und-hinweis.md)).
- **Sohn — geliefert, Geräte-Check offen.** Auswahlstufe: vier Karten mit je vier Knöpfen in Antwort-Schreibweise, kein
  Trenner-Hinweis, alle richtig gewählt → richtig. Test auf „tippen“: jede Karte trägt `setAnswer`, `4;2`/`keine Lösung`/
  `2,3;5`/`3;3` → 3 von 4. In der Arcade (Spec, Handy-Viewport 393×830): zwei Felder „Lösung 1/2“, „4“ und „2“ getippt,
  Enter; „keine Lösung“ angetippt, Felder leer und gesperrt; Ergebnis 2/2. **Offen für den Menschen:** (1) die Formel steht
  als `$x^2 - 6x + 8 = 0$` auf der Karte, bis [B-638](backlog/B-638-formeln-in-aufgabentext-anzeigen.md) gebaut ist;
  (2) am echten Handy prüfen, dass `inputMode="decimal"` die Zahlentastatur mit Komma und Minus zeigt und sie nach Enter
  offen bleibt.
- **Entwickler-Regressionszeugen:** volle Suite grün (2542), Vitest grün (1488), `lektion`- und `rechnen-mit-komma`-Spec
  grün (Antwortfläche und Zahlen-Atom unverändert).

## Retrospektive

**Nachschau:** Sprint 165 (B-990, `fc3c37f4`): die Kürzel-Regel sitzt in `AnswerGrader.Comparable`, also an der Senke für
Üben, Test und Vorschau; `MatchesGap` faltet die Kürzel nicht, hat aber keinen Aufrufer (toter Code, kein Pfad). Kein
Befund, `nachgeschaut: 2026-11-25` gesetzt. Index: **582 von 623** (vor dieser Abnahme).

**Was die eigenen Tore durchgelassen haben:** die zwei stärksten Funde kamen vom Review, nicht von Test oder Mutante – ein
Überlauf bei winzigem `A` (jede Auswahlkarte wäre ein 500 geworden) und ein Fokusverlust aus dem dritten Feld. Beide lagen
außerhalb der Fälle, die ich gebaut hatte; Mutanten messen nur, ob die geschriebenen Fälle tragen. Die Probe fand drei Tests,
die weniger trugen als ihr Kommentar (M10, M23, M15) – die Regel aus `CLAUDE.md`, hier vom Werkzeug statt vom Reviewer
gefunden. Sechs Mutanten liefen ins Leere, weil `if (false)` unter `TreatWarningsAsErrors` nicht kompiliert (CS0162).

**Der eine Mechanismus:** `mutant-probe.py` nennt bei einem Build-Fehler mit CS0162 Grund und Ausweg in der Tabelle
(„Bedingung unmöglich machen statt `false`“) – vorher stand dort nur „kompiliert nicht“, der Grund lag im verschluckten
Build-Log. Kein neues Tor, darum kein Halt.

**Bewusst ungedeckt:** (1) Der Hinweis wird im Frontend auf jeder `setAnswer`-Karte ausgeblendet, nicht nur der
Trenner-Hinweis; kein Test fängt einen künftigen echten Hinweis dort ab (B-1000). (2) Die Lösungsfeld-Regel
(`ConventionGuardTests`) kann die Config dieses Typs nicht prüfen, weil sie hinter `Roles.Creator` liegt – AK 9 tragen
`unknown_field` und der Kartentest. (3) Gleichungen mit |A| < 0,001 oder Wurzeln enger als 10⁻⁶ sind ausgeschlossen statt
unterstützt.

## Offene Roadmap (priorisiert)

Die Liste, die gilt, ist `docs/backlog/`. Für diesen Typ:

1. [B-638](backlog/B-638-formeln-in-aufgabentext-anzeigen.md) – die Formel als Formel zeigen; erst dann ist die Sohn-Karte
   vollständig.
2. [B-1000](backlog/B-1000-gleichung-vorschau-und-hinweis.md) – Vater-Testmodus mit Feldern, Hinweis am Server.
3. Die zurückgestellten Stufen „Anzahl bestimmen“/„Verfahren wählen“ (B-710, offene Punkte) – erst, wenn Friedrich den Typ
   gespielt hat.

**Änderungsstand:** `a961897b` (Code, alle drei Stories), danach je Story ein Abnahme-Commit und der Retro-Commit.
