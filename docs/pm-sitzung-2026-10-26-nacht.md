---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-26, Sechster Nachtlauf]
---

# PM-Sitzung: Nachtlauf 2026-10-26 — Sprint 11 / NP-6 (Lehrwerk und Vokabeln aus einer Hand)

**Datum:** 2026-10-26  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Paket NP-6 aus der [Backlog-Sichtung](pm-sitzung-2026-10-23-backlog-sichtung.md) als Sprint 11 fahren.

Auftrag: der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–9 plus *„Fahre das Paket NP-6 aus
docs/pm-sitzung-2026-10-23-backlog-sichtung.md."* Sprint 11 beginnt mit **Zähler 0**. Ausgangslage: HEAD `15267f2`,
Baum sauber. Nicht angefasst (Abgrenzung im Auftrag): Sprint 10 (B-68, B-263, B-134, B-236), Sprint 7 (B-15, B-17,
B-21), NP-3 und die Nachschau-Defekte B-257, B-260, B-261, B-262.

Browser: die Chrome-Extension war **nicht verbunden** (`tabs_context_mcp`: „Browser extension is not connected“).
Rollengang darum nach Freigabe 6 als E2E-Spec, je Story benannt.

## Sprint 11 — Ziel & Umfang

**Sprint-Ziel:** *Der Vater wählt das Lehrbuch seines Kindes aus dem Katalog statt es frei zu tippen, und der
Creator legt gleichwertige Übersetzungen direkt im Übungs-Editor an.*
**Umfang:** [B-64](backlog/B-64-textbook-vs-series.md) und [B-71](backlog/B-71-inline-vokabelliste-ohne-varianten.md),
beide `geschaetzt`, `Wunsch` → baubar nach Freigabe 8 (B-71 trägt zusätzlich die Freigabe-Zeile vom 2026-08-07).
Die Wortfrage „Lehrwerk/Reihe/Buchreihe“ aus B-123 ist **ausgelassen**: alle Beschriftungen bleiben, wie sie waren
(„Reihe im Katalog“).

**Entwickler-Brief:**

- **Quelle der Wahrheit:** der Vokabel-Store (`Vocabulary.TranslationAlternatives`) für B-71 – keine zweite,
  übungslokale Liste; der Katalog (`TextbookSeries.Publisher`, `SeriesUnit.Grade`) für Verlag/Klasse in B-64, der
  Freitext am `Textbook` bleibt Rückfall.
- **Guards:** B-71 schreibt erst nach allen Prüfungen (409/400/`exercise_in_use` lassen den Store unberührt);
  `null` = unverändert, `[]` = löschen. Rechte: der Item-Endpunkt verlangt Creator + `EnsureCanWrite`, enger als
  der schon offene Store-PATCH.
- **Migration:** keine. Selbst geprüft: beide Stories lesen bzw. schreiben vorhandene Spalten
  (`Vocabulary.TranslationAlternatives` seit B-65, `TextbookSeries.PublisherId`, `SeriesUnit.Grade`); kein Modell-Diff,
  `SchemaGuardTests` grün.
- **Testweg:** Integrationstests je AK, rote Probe je Zweig per Mutante; Komponententests für das Lehrbuch-Formular;
  zwei neue E2E-Specs als Rollengang.

**Prämissen nachgeprüft (Hinweis aus dem Vorlauf):**

- B-71, AK 6 („Abschlusstest liest `TranslationAlternatives`“): im Code belegt
  (`ExerciseContentResolver.cs:99`) und jetzt zusätzlich am laufenden Bewertungsweg gemessen (Integrationstest über
  den Item-Endpunkt, siehe unten).
- B-71, AK 5: **Prämisse teilweise veraltet.** Der Neuanlage-Assistent hat für Vokabeln keine Front/Back-Zeilen mehr
  (`exerciseConfig.tsx` rendert sie für diesen Typ nirgends); das Inline-Anlegen dort ist das Schnell-Anlegen im
  `VocabRefPicker`, das den Store-Endpunkt ruft. Das Feld „Auch richtig“ sitzt darum dort – derselbe Store-Eintrag,
  dieselbe Absicht (Entscheidung 5). Keine neue Entscheidung nötig.
- B-64: Nachprüfung vom 2026-10-23 bestätigt (B-63 gebaut, Ableitung über `Series.Publisher.Name`); die veraltete
  Ist-Stand-Zeile („keine einzige Übung an der Unit“) ist in der Story als überholt markiert.

## Iteration 11 — umgesetzt

**Backend (API-First):**

- B-71: `VocabItemInput`/`VocabItemResponse` additiv um `TranslationAlternatives`; neuer expliziter Schreibpfad
  `VocabularyStoreService.ApplyTranslationAlternatives` (Entscheidung 3), `CleanAlternatives` dorthin verschoben und
  vom Store-Controller mitbenutzt; `AddItem`/`PatchItem` schreiben nach allen Prüfungen, `PatchItem` auf den Eintrag
  **nach** einem Worttausch.
- B-64: `TextbooksController.Project` leitet Verlag (`Series.Publisher.Name`) und Klasse (`CurrentUnit.Grade`) ab,
  Rückfall auf den gespeicherten Freitext, wenn der Katalog keinen Wert hat oder die Verknüpfung fehlt. Vertragsdoku an
  `TextbookResponse` und `UpdateTextbookDto`.

**Frontend:**

- B-64: `TextbookForm` in drei Teile – Gruppe „Aus dem Katalog“ (Reihe, „+ Neue Reihe“ mit Inline-Anlage, Unit)
  zuerst, dann Titel/Fach, dann Gruppe „Ohne Katalog“ (Verlag, Klasse, Kapitel). Verlag/Klasse erscheinen als Text
  „· aus dem Katalog“/„· aus der Unit“, sobald die aktuelle Auswahl einen Katalogwert hat.
- B-71: Wortpaare im Bearbeiten-Dialog mit Spalte „Auch richtig“ (OK speichert, leere Liste löscht), „Wort ergänzen“
  und das Schnell-Anlegen im Assistenten mit Variantenfeld.

**Rote Proben (Auflage 5)** – je Mutante gegen die eigene Fassung, erwartet je mindestens ein roter Fall am benannten Ort:

| Probe | erwartet | gemessen |
| --- | --- | --- |
| B-71 M1: `AddItem` schreibt nicht | AK 1/3 rot | **3** rot (Inline, Fund, Abgelehnt) |
| B-71 M2: `PatchItem` schreibt nicht | AK 2/6 rot | **3** rot (Patch, Worttausch, Test) |
| B-71 M3: `null` heißt leeren | „ohne Feld bleibt“ rot | **2** rot |
| B-71 M4: Patch schreibt aufs alte Wort | Worttausch rot | **1** rot |
| B-71 M5: Antwort ohne Alternativen | AK 4 rot | **1** rot |
| B-71 M6: Schreiben vor der Duplikatprüfung | Abgelehnt rot | **1** rot |
| B-71 M7 (nach Review): Schreiben in `ResolveVocabularyIdAsync` vor dem Save | Abgelehnt rot | **1** rot |
| B-64 M1/M2: Verlag bzw. Klasse nicht abgeleitet | AK 4/5 rot | **2** / **2** rot |
| B-64 M3: Unit ohne Band überschreibt | Katalog-Fall rot | **1** rot |
| B-64 M4: Verlag ohne Rückfall | Entkoppeln rot | **1** rot |
| B-64 M5: Reihe ohne Verlag leert | Katalog-Fall rot | **1** rot |
| B-64 M6 (nach Review): `grade` bei Verknüpfung verworfen | Entkoppeln rot | **1** rot |
| Formular-Tests gegen `ChildMaterialSection.tsx` aus `HEAD` | 4 neue rot, Gegenprobe grün | **4** rot / 11 grün |
| Anzeige liest `form.publisher` bzw. `form.grade` (nach Review) | je 1 rot | **1** / **1** rot |
| Verlagsfeld bei jeder Reihe weg / Klassenfeld bei jeder Unit weg | je 1 rot | **1** / **1** rot |
| B-71-Spec ohne Übernahme der PATCH-Antwort (nach Review) | rot an `:90` | **1 failed**, an `:90` |

`unknown_field` (AK 7) ist eine Absichtserklärung gegen einen künftigen Sonderfall – rot nur, wenn das neue Feld
lax deserialisiert würde; vor dem Fix war der Fall ebenfalls grün.

## Review (nacheinander, je Bereich)

**Zähler Sprint 11 (Freigabe 3): 5 von 5.**

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | roter Test (Suite) | `PatchSemanticsTests` `UpdateTextbookDto`/`clearGrade`: die Fixture verknüpfte eine Unit **mit** Klasse; seit der Ableitung zeigt die Antwort die Katalog-Klasse | Fixture auf eine Unit ohne Klasse, Begründung im Kommentar (Reviewer: keine Abschwächung) | ja |
| 2 | `pugling-reviewer` | B-64: Schreiben von `grade` bei bestehender Verknüpfung ungetestet | Schritt `grade = 8` im Entkoppeln-Fall + Vertragssatz an `UpdateTextbookDto`; Probe M6 | ja |
| 3 | `pugling-reviewer` | B-71: der Abgelehnt-Fall nahm den Id-Weg und fing die naheliegende Regression nicht | zweite Anfrage inline; Probe M7 | ja |
| 4 | `frontend-reviewer` | B-71: nach dem Speichern zieht das Feld nur nach, wenn sich die Server-Liste ändert – räumt der Server nur weg, bleibt „OK“ stehen | Antwort über `runFor` übernehmen; E2E-Schritt, Probe rot an `:90` | ja |
| 5 | `frontend-reviewer` | B-64: Formular-Tests mit Freitext = Katalogwert trugen „aus dem Katalog“ nicht | verschiedene Werte, Reihenwechsel-Fall; zwei Proben | ja |

Ein Delta-Review (`frontend-reviewer`) über die Behebungen 4 und 5: **trägt, kein neuer Fehler**.

**Nicht gezählt, mit Grund:**

- `pugling-reviewer` Fund 4 / `frontend-reviewer` 🟡 „Entkoppeln zeigt den Katalogwert im Freitextfeld“: jede Behebung
  verlangt eine Entscheidung, die B-64 nicht hergibt (Antwort um den Freitext erweitern widerspricht Entscheidung 3)
  → **angehalten und notiert** (Freigabe 8), [B-267](backlog/B-267-entkoppeltes-lehrbuch-zeigt-katalogwert-im-freitextfeld.md)
  (`Frage`, `wartet_auf` Nutzer). Kein Datenverlust ohne Zutun; AK 4/5 halten.
- `frontend-reviewer` 🟢 4 (Fokus fällt nach „Reihe anlegen & wählen“ auf `body`) → [B-268](backlog/B-268-fokus-nach-inline-reihe.md).
  **Offen benannt:** der Reviewer stufte es als Hinweis ein, und eine Behebung hätte den Zähler auf 6 gehoben – der
  Lauf hätte dann sofort geendet. Ob das richtig abgewogen war, entscheidet der Nutzer.
- 🟢 3 (`let neu` statt `runFor` in `reiheAnlegen`), 🟢 5 (Klassenfeld flackert kurz beim Öffnen; ein vorher
  getippter Verlag geht nach der Reihenwahl unsichtbar mit – gewollt, Entscheidung 3), 🟢 6 (`.first()` in der
  B-71-Spec, durch Fach/Reihe/Unit eindeutig), 🟢 7 (Muster `busy`/`err` im `VocabRefPicker` vorbestehend):
  Stil- bzw. Hinweise ohne Fehler, nicht geändert.
- `web-design-guidelines` (Freigabe 7) lief nicht – optional, und jeder weitere gezählte Fund hätte den Lauf beendet.

## Verifikation

- `dotnet build Pugling.sln`: 0 Warnungen, 0 Fehler; `dotnet format Pugling.sln`: keine Änderung.
- `dotnet test Pugling.sln -c Release`: **909/909** (900 vor dem Sprint + 7 neue in `VocabItemAlternativesTests` + 2 neue in
  `StudentProfileTests`).
- Frontend: `tsc -b` sauber, Vitest **423/423** (51 Dateien).
- Playwright voll, nach dem letzten Code-Edit: **87 passed** (85 + 2 neue Specs), 8,0 min.
- DocsCaptureTests: `docs/api-examples/catalog.md` trägt eine Zeile mehr (`"translationAlternatives": null` am
  Item); zweiter Lauf ohne weitere Änderung (byte-stabil).

## Runde 11 — Re-Review / Abnahme (Rollengang als E2E, Freigabe 6)

- **Vater (B-64)** – `e2e/lehrbuch-katalog-zuerst.spec.ts`: Kind anlegen; unter „Lehrwerke“ Verlag + Reihe + Unit
  (Klasse 7); am Kind ein Buch ganz ohne Katalog (AK 3); Katalog-Gruppe vor der Freitext-Gruppe (AK 1); verknüpfen –
  Verlag und Klasse erscheinen schon vor dem Speichern als Katalogwert, das Verlagsfeld verschwindet, die Zeile zeigt
  danach den Katalog-Verlag (AK 4); entkoppeln – „Freitext-Verlag“ und Klasse 9 sind wieder da (AK 5); eine fehlende
  Reihe direkt im Formular angelegt, gewählt und am Buch gespeichert (AK 2). **Abgenommen** – mit der offenen
  Entscheidung B-267 und dem Hinweis B-268.
- **Creator (B-71)** – `e2e/auch-richtig-im-uebungseditor.spec.ts`: Fach/Reihe/Unit über die Oberfläche; im
  Assistenten ein Wort mit Variante anlegen und wählen, Übung anlegen; im Bearbeiten-Dialog liest die Zeile die
  Variante vom Store zurück; zweite Variante ergänzt und gespeichert; die anders geschriebene Hauptübersetzung wird
  vom Server weggeräumt und verschwindet aus dem Feld; neues Wort mit Variante ergänzt; nach Neuladen stehen alle drei
  (AK 4/5). AK 6 (Abschlusstest zählt die Variante) hält der Integrationstest. **Abgenommen.**
- **Sohn** – Regressionszeuge: sein Tier ist unverändert; die Bewertung liest dieselbe Liste wie seit B-65 (neuer
  Integrationstest über den Item-Weg grün), seine Specs im vollen Playwright-Lauf grün.

## Retrospektive

**Nachschau (erste Pflichthandlung):** Index vor dem Lauf *„Nachgeschaut: 169 von 182“*; die abgenommenen, nie
nachgeschauten Stories waren 13 (B-19, B-46, B-160, B-165, B-167, B-170, B-180, B-182, B-188, B-189, B-195, B-202,
B-209) – alle aus früheren Läufen. **Sprint 10 (B-68, B-263, B-134, B-236) ist nicht abgenommen und gehört
ausdrücklich nicht dazu.** Geprüft von drei frischen Prüfern (nur lesend), je Story mit benanntem Prüfpunkt im
`## Verlauf`, `nachgeschaut: 2026-10-26` gesetzt. Ergebnis: **10 ohne Befund, 3 mit Befund** →
[B-264](backlog/B-264-interessen-ohne-kind-kleiden-nicht-ein.md) (aus B-46, am Code belegt),
[B-265](backlog/B-265-sperr-zaehler-zusicherung-kann-nie-fallen.md) (aus B-165, am Code belegt),
[B-266](backlog/B-266-wartepunkt-netzwerk-statt-app-zustand.md) (aus B-180, nur strukturell, `unverifiziert`). Index
danach *„Nachgeschaut: 182 von 184 — in 50 davon ein durchgekommener Defekt“*; die zwei übrigen sind B-64 und
B-71 aus diesem Sprint. Fünf Hinweise ohne Zählwert stehen in den Verlauf-Zeilen (B-167, B-170, B-182, B-188, B-202).

**Was die eigenen Tore durchgelassen haben:** Bis zum Review lagen in den neuen Tests zwei Zusicherungen, die die
behauptete Eigenschaft nicht trugen (Fund 3: der Abgelehnt-Fall über den Id-Weg; Fund 5: Freitext gleich Katalogwert).
Beide hatten eine rote Probe – aber gegen **meine** Mutante, nicht gegen die naheliegendste. Genau das sagt die
Hausregel (rote Probe je Datei fragt nicht, ob jeder Fall etwas trägt); die Mutanten-Tabelle hat es diesmal nicht
verhindert, weil ich die Mutanten aus meinem eigenen Bau abgeleitet habe.

**Vorschlag (nur vorgeschlagen, Freigabe 3):** in den Testweg-Abschnitt von `geschaetzt` bzw. in pm-loop Step 5 ein
Satz: *„Je Testfall, der eine Abgrenzung behauptet (‚nicht X‘, ‚aus dem Katalog statt aus dem Freitext‘), müssen sich
die beiden Werte im Testaufbau unterscheiden.“* Prosa, kein Tor – ein Tor müsste Testdaten semantisch lesen; begründet
ungedeckt bleibt darum die Wahl der Mutanten selbst.

**Zweite Beobachtung (kein Vorschlag):** Der Zähler stand nach einem Review-Durchgang bei 5. Mit einer reinen
Hinweis-Einstufung (🟢) eines Reviewers hängt jetzt an einer Zeile Text, ob ein a11y-Mangel behoben oder zurückgestellt
wird (B-268). Das gehört in die Morgen-Entscheidung, nicht in einen weiteren Mechanismus.

## Ende

Endgrund: **Paket abgearbeitet** (NP-6 war der einzige Auftrag); zusätzlich griffe Limit 2 (das eigene Increment trug
Funde aus dem Review). Zähler Sprint 11: **5** (nicht überschritten).

**Offene Entscheidungen für den Nutzer:**

1. [B-267](backlog/B-267-entkoppeltes-lehrbuch-zeigt-katalogwert-im-freitextfeld.md): soll `TextbookResponse` den
   gespeicherten Freitext getrennt mitliefern (a), das Formular „angezeigt = gespeichert“ erzwingen (b) oder das Feld
   leer zeigen (c)?
2. [B-268](backlog/B-268-fokus-nach-inline-reihe.md): war es richtig, den Fokus-Hinweis zurückzustellen statt ihn zu
   beheben und den Lauf an Freigabe 3 enden zu lassen?
3. Der Retro-Vorschlag oben (Abgrenzungs-Testdaten müssen sich unterscheiden).
4. Unverändert offen aus früheren Läufen: Sprint 10 (Zähler überschritten), Sprint 7, NP-3.

**Commit:** `3094e34` (nicht gepusht).
