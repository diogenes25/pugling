---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-17, Sprint Ausfall sichtbar]
---

# Nachtlauf 2026-10-17 — „Ein gescheiterter Abruf steht da, bis ich ihn selbst behoben habe"

**Datum:** 2026-10-17 · **Moderation:** PM · **Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Auftrag:** „Starte einen Nachtlauf" — Auftragstext und Freigaben 1–8 wörtlich aus [nachtlauf.md](nachtlauf.md).
**Ausgangsstand:** `main` bei `1ad45c2` (3 Commits vor `origin/main`, ungepusht). Chrome-Extension angebunden
(Freigabe 6: echter Browser-Rollengang möglich). Kein Server lief.

## Stand — hier einsteigen

Wird je Sprint fortgeschrieben; die letzte Zeile ist der Zustand.

- Sprint 1 angesetzt: vier Frontend-Defekte derselben Familie (B-216, B-215, B-194, B-185), alle unter
  Freigabe 1 autonom gegrillt und geschätzt.
- **Sprint 1 abgenommen** (B-216, B-215, B-194, B-185): 381/381 Vitest, volle Playwright-Suite grün nach einer
  Spec-Korrektur, Reviewer ohne Blocker, Browser-Rollengang in Chrome aus allen drei Rollen. Vier Review-Funde
  mit Code-/Testwirkung (Grenze fünf). Nachschau des Vorsprints: zwei Defekte (B-224, B-225). Ein neuer
  Prüfauftrag aus dem Rollengang (B-226). Commit: siehe „Änderungsstand". Retro-Mechanismen nur
  **vorgeschlagen** (Freigabe 3) – siehe „Für den Morgen".

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel, aus zwei Rollensichten:**

> Der **Vater** sieht jeden gescheiterten Abruf als Meldung, die stehen bleibt, bis er sie selbst behoben hat
> — kein Feld und kein Hinweis behauptet einen Zustand, den der Server nicht geliefert hat. Der **Sohn**
> drückt im Shop „Nochmal versuchen", und der Knopf bleibt unter seinem Finger.

**Umfang** (alle `art: Defekt`, vorher `idee`/`ausformuliert`, in dieser Sitzung auf `geschaetzt` gebracht):

| Story | Richtung derselben Wurzel |
| --- | --- |
| [B-216](backlog/B-216-wiederholknopf-haengt-sich-aus.md) | Der Wiederhol-Knopf verschwindet im Moment des Klicks — an **drei** Stellen, nicht zwei |
| [B-215](backlog/B-215-typ-manifest-ohne-fehlerkanal.md) | Das Typ-Manifest schluckt seinen Ausfall; Anlegen und Bearbeiten sind gesperrt, ohne Grund zu nennen |
| [B-194](backlog/B-194-plandetail-kinderauswahl-ohne-rueckfall.md) | Die Kind-Auswahl im Plan-Detail zeigt bei ausgefallener Liste ein leeres Feld |
| [B-185](backlog/B-185-anlegemodus-behauptet-uebernommenes-fach.md) | Der Hinweis „aus dem Lehrwerk übernommen" steht neben einem Feld, das „fachneutral" zeigt |

**Draußen, mit Namen:** B-164 (Architekturfrage des Nutzers — `useAsync` wird nicht angefasst, siehe B-216
Entscheidung 2), B-183 (Wiederholweg an den 68 Lesefehler-Bannern — Produktbreite, bleibt `idee`), B-134/
B-176/B-201/B-211 (andere Familie: A11y bzw. Texte), B-214 (E2E-Bauart, kein Nutzer-Symptom).

**Warum dieses Thema zuerst:** Es ist die Fortsetzung der Sprints vom 2026-09-04/05 (B-188, B-209, B-212)
und die letzte offene Gruppe der Familie „Ausfall wird als Zustand ausgegeben". Alle vier sind klein, alle
vier haben ein vorgeführtes Muster (`ListenAusfall`, `ReferenceSelect`), und der Rollengang lässt sich als
E2E mit `page.route(…).abort()` fahren.

### Entwickler-Brief

- **Ziel:** siehe Sprint-Ziel. Quelle der Wahrheit bleibt der Server; das Frontend hört nur auf zu lügen.
- **Ein geteiltes Gedächtnis statt vier Kopien:** Die Regel „der letzte Fehler überlebt den Start der
  Wiederholung" wandert aus `ListenAusfall` in einen Hook `useGemerkterFehler(fehler, laedt)` unter
  `src/lib/`. `ListenAusfall` zieht nach `src/components/` und nutzt ihn; die drei B-216-Stellen und das
  Manifest (B-215) nutzen ihn ebenfalls. `useAsync` bleibt unverändert (B-164 gehört dem Nutzer).
- **Manifest mit Fehlerkanal:** `useExerciseTypes` liefert immer ein Lookup mit `loading`, `error`,
  `reload`; die leere Liste bleibt („lesbar statt kaputt"). Angezeigt wird der Ausfall nur dort, wo er eine
  Handlung sperrt: Übungsliste (Bearbeiten/Löschen) und Übung anlegen (Anlegen).
- **Kind-Auswahl:** `ReferenceSelect` bekommt eine optionale Leer-Option (`emptyLabel` optional); das
  Plan-Detail nutzt es mit dem geladenen Kind und rendert `children.error`.
- **Abgeleitetes Fach:** Im Anlege-Modus speist die gewählte Reihe die Sonderoption, wenn die Fächerliste
  den abgeleiteten Wert nicht kennt — Anzeige folgt dem Zustand, der Hinweis wird wahr.
- **Reihenfolge:** Hook + `ListenAusfall`-Umzug → B-216 → B-215 → B-194 → B-185 → Vitest → E2E-Specs →
  `tsc -b`, `vitest run`, `playwright test` → `frontend-reviewer` (+ `web-design-guidelines`, Freigabe 7) →
  Browser-Rollengang → Commit.
- **Testweg:** je Story benannt in `## Schätzung`; E2E eine Fläche je Spec.

## Iteration 1 — umgesetzt

**Frontend (kein Backend berührt, kein Vertragsbruch, keine Migration):**

- Neuer Hook `src/lib/useGemerkterFehler.ts`: der letzte Fehler überlebt den Start der Wiederholung.
  `ListenAusfall` zieht nach `src/components/` und nutzt ihn; Trefferbereich des Assistenten, Übungssuche im
  Planbau und die Verlaufskarte des Sohn-Shops ebenfalls (B-216 – **drei** Stellen, die Story nannte zwei).
- `useExerciseTypes` liefert immer ein Lookup mit `loading`/`error`/`reload`; sieben Konsumenten ohne `?.`;
  `ListenAusfall was="Die Übungstypen"` über der Übungsliste und dem Anlege-Formular (B-215).
- `ReferenceSelect`: `emptyLabel` optional (Pflichtfeld ohne Leer-Option); Plan-Detail nutzt es für das Kind
  und rendert `children.error` (B-194).
- `ProfileForm`: im Anlege-Modus speist die gewählte Reihe die Sonderoption, wenn die Fächerliste den
  abgeleiteten Wert nicht kennt (B-185).
- Fokus: ein gesperrter Knopf verliert in Chromium den Fokus (im E2E gemessen: `toBeFocused` meldete
  `inactive`). Darum wandert er beim Klick auf den Kasten (`tabIndex={-1}`), mit Fokusring
  `.banner[tabindex]:focus-visible`.
- Fünf neue E2E-Specs (`wiederholknopf-bleibt`, `shop-verlauf-wiederholen`, `typ-manifest-faellt-aus`,
  `plan-kinderliste-faellt-aus`, `fachlehrer-faecher-fallen-aus`), sieben neue Vitest-Fälle plus zwei
  Hook-Testdateien.

**Verifikation:** `tsc -b` sauber · `vitest run` **381/381** (47 Dateien) · `playwright test` **69/70** im
ersten vollen Lauf; der rote `lehrwerke.spec.ts` war eine Nebenwirkung der neuen Fachlehrer-Spec in der
geteilten DB, nach der Korrektur die fünf betroffenen Specs gemeinsam **5/5** grün, alle übrigen 65 waren im
vollen Lauf grün.

**Rote Proben (erwartet/gemessen), vor dem Fix in einem Lauf gemessen:** 7 rot von 44 (ReferenceSelect 1/1,
VaterFachlehrer 1/1, VaterWizard 1/1, HistoryTab 1/1, exerciseTypes 3/3) plus ein Importfehler für den noch
nicht existierenden Hook. Jede fällt an der Zeile, die ihren Fall trägt (Tabellen bzw. Verlauf in den Stories).

### Review-Funde (Zähler: 4 von 5)

| # | Quelle | Fund | Behebung |
| --- | --- | --- | --- |
| 1 | eigene E2E | gesperrter Knopf verliert den Fokus (Chromium) | Fokus auf den Kasten, AK 1 umformuliert, Entscheidung 5 in B-216 |
| 2 | `web-design-guidelines` | fokussierter Kasten ohne projektweiten Fokusring | CSS-Regel in `index.css` |
| 3 | `frontend-reviewer` | Leer-Satz und Zählzeile im Trefferbereich hingen an `fehler`, nicht am Gedächtnis – im Fenster standen Banner **und** Leer-Satz | `gescheitert` an beiden Stellen, Vitest-Zusicherung ergänzt; Zwilling im Planbau gleich |
| 4 | volle Suite | `lehrwerke.spec.ts` rot: das von der neuen Spec angelegte aktive Englisch-Profil passt zu jedem Kind | Profil wird stillgelegt angelegt |

Ohne Zähler (Kommentar/Doku bzw. außerhalb des Diffs): B-215-Risikosatz zu `reload()` berichtigt, zwei
Kommentare zum Schlüssel-Rückfall, PATCH-Zusicherung in `plan-kinderliste-faellt-aus` als Absichtserklärung
beschriftet, Banner der gefüllten Verlaufskarte (`SohnShop.tsx`, „Mehr laden") ebenfalls ans Gedächtnis
gehängt und mit einem Vitest-Fall belegt.

## Runde 1 — Re-Review / Abnahme

Echter Browser-Rollengang in Chrome (Freigabe 6): Backend mit Wegwerf-DB (`ConnectionStrings__Default` auf
eine Temp-Datei, Seed Papa #1 / Sohn #1), `npm run dev`; Abrufe über einen Fetch-Wrapper **in der Seite**
abgeschossen bzw. um 2,5 s verzögert, Navigation clientseitig, damit der Wrapper überlebt. Klicks per DOM
(`button.click()`), weil die Mauskoordinaten der Extension bei DPR 1,5 neben dem Knopf landeten – der erste
Mausklick traf nichts, der DOM-Klick ist derselbe Ereignisweg.

- **Creator (Übungsliste, Fachlehrer) — signed off.** O-Ton: „Vorher stand bei ausgefallenem Manifest nur
  „Typ hier nicht bearbeitbar" oder gar nichts. Jetzt steht da, *was* nicht geladen wurde, und „Erneut laden"
  holt die Namen wirklich zurück – ich hab es gesehen: „· Vocabulary" wurde zu „· Vokabelkarten". Beim
  Fachlehrer zeigt das Fach endlich das, was die Reihe mir gibt, auch wenn die Fächerliste klemmt." Gemessen:
  Banner, „Lädt…" gesperrt, Fokusring auf dem Kasten, danach kein Banner; Fach-Feld „Englisch (nicht geladen)"
  mit Wert 1 und drei wahren Hinweisen.
- **Vater (Planbau, Plan-Detail) — signed off.** O-Ton: „Der Knopf bleibt da, wenn ich draufdrücke, und sagt
  „Lädt…". Vorher war er einfach weg. Und im Plan sehe ich jetzt „Sohn (nicht geladen)" statt eines leeren
  Felds, und darüber steht, dass die Kinderliste nicht kam." Gemessen: Zählzeile „(Suche gescheitert)" ohne
  Leer-Satz im Fenster, danach „(12 Treffer)"; Kind-Feld mit genau einer Option, Banner „Kinder: …".
  Der Assistent wurde nicht von Hand gefahren – `wiederholknopf-bleibt.spec.ts` fährt ihn im echten Browser,
  und es ist derselbe Baustein wie im Planbau-Zwilling.
- **Sohn (Shop-Verlauf) — signed off.** O-Ton: „Ich drück „Nochmal versuchen" und der Knopf bleibt, mit
  „Lädt…". Dann steht da „Noch nichts gekauft". Vorher war nach dem Drücken einfach alles leer – als hätte ich
  nichts gemacht." Gemessen: Meldung bleibt, Knopf „Lädt…" gesperrt, Fokus auf der Meldung, danach Leer-Satz.
  **Beobachtung außerhalb des Umfangs:** Kopfzeile „🪙 60", Shop-Kopf „🪙 50" im selben Bild →
  [B-226](backlog/B-226-zwei-muenzstaende-im-shop-bildschirm.md).
- **Regressionszeugen:** die ganze Playwright-Suite und 381 Vitest; kein Weg des Durchstichs
  (`full-flow.spec.ts`) berührt.

## Retrospektive

**Nachschau:** Sprint 2026-09-05 (B-187, B-200, B-212, Commit `3d763fe`) durch einen frischen
`frontend-reviewer`-Agenten gegengelesen, je mit benanntem Prüfpunkt (sieben Schreibpfade der Datei auf ihren
Meldungsplatz gezählt, `scrollIntoView`-Bedingung, Stories gegen Code, Zusicherungen gegen den
Ausgangszustand, Kommentare). **Zwei Funde:** B-212 verspricht die *Seite*, gebaut ist die *Datei* –
`PublisherAdmin.tsx:80` trägt einen eigenen `StatusBanner` →
[B-224](backlog/B-224-verlagsbaustein-traegt-zweiten-meldungsplatz.md) (`entgangen_bei: B-212`); ein im
Sprint neu geschriebener Kommentar behauptete eine Knopf-Sperre, die nur der Verlags-Knopf hat, darunter ein
stummer Pfad → [B-225](backlog/B-225-reihe-anlegen-schweigt-bei-leerem-namen.md). Dazu drei falsche
Zeilenangaben der roten Proben (korrigiert) und zwei veraltete Kommentare (nachgezogen). `nachgeschaut` auf
allen drei gesetzt. **Index: Nachgeschaut 129 von 146 — 33 mit Defekt** (vorher 126 von 142 — 32).

**Was die eigenen Tore dieses Sprints durchgelassen haben:**

1. **Die Vitest-Zusicherung prüfte die Anwesenheit des Neuen, nicht die Abwesenheit des Alten.** „Banner
   steht im Fenster" war grün, während daneben weiter der Leer-Satz stand (Fund 3). Gefunden vom Reviewer,
   nicht von einem Test. Es ist exakt die Fehlerklasse von B-162 („zwei Ursachen in einem Bildschirm"), eine
   Runde später, in dem Test, der B-162s Regel bewachen sollte.
2. **Ein Zustand, den der Browser erzeugt, war in happy-dom unsichtbar.** Dass ein `disabled`-Knopf den Fokus
   verliert, misst kein Komponententest; die E2E fand es (Fund 1). Das Tor hat funktioniert – aber erst auf
   der teuersten Stufe.
3. **Eine neue Spec veränderte die geteilte DB so, dass eine fremde Spec fiel** (Fund 4). Die Suite fing es;
   die Regel „Specs hinterlassen nichts, was ein Matching ändert" steht nirgends.

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

**Jede rote Probe zu einem „X statt Y"-Defekt sichert beide Hälften im selben Zustand zu: Y ist weg, X ist
da.** Ort: `frontend/CLAUDE.md`, der Absatz zu Zusicherungen, oder `docs/backlog/README.md` bei den roten
Proben (Auflage 5 verlangt die Zahl, nicht die Form). Begründung: dreimal gemessen (B-162 selbst, B-188s
Kinder-Fall, heute Fund 3) – die eine Hälfte allein ist grün, sobald irgendetwas erscheint. Kosten: eine
Zeile Prosa, die bezahlt werden muss (Budget-Skript), und je Probe eine Zusicherung mehr. Kein Tor: Welche
Zusicherung „die andere Hälfte" ist, kann kein Regex erkennen.

**Benannt, nicht vorgeschlagen:** eine E2E-Hausregel „Specs legen Fachlehrer-Profile stillgelegt an oder
räumen sie weg" – zu speziell für eine Dauerregel; der Kommentar in der Spec trägt sie vorerst.

**Bewusst ungedeckt:** Fund 1 (Fokusverlust) ist nur im echten Browser messbar – das ist der Platz der E2E,
und sie hat ihn gefunden.

## Offene Roadmap · Änderungsstand

Die dauerhafte Liste ist [docs/backlog/](backlog/README.md). Änderungsstand dieses Sprints: ein Commit mit Code,
Specs, Stories und Protokoll: `d834a8e`, dazu dieser Nachtrag, **nicht gepusht**.

## Für den Morgen

1. **Retro-Vorschlag oben** landen oder verwerfen (dazu die fünf älteren, unverändert offen).
2. **B-224** (Verlags-Baustein, zweiter Meldungsplatz) und **B-225** (stummer „Reihe anlegen") sind
   `ausformuliert` und `art: Defekt` – ein Folgelauf darf sie autonom grillen; die Empfehlungen stehen drin.
3. **B-226** ist eine `Frage` (zwei Münzstände im Shop) – gesperrt für den Lauf, Entscheidung beim Nutzer:
   erst klären, ob die Differenz fachlich ist.
4. **B-164** bleibt beim Nutzer (Architekturfrage `useAsync`); B-216 hat sie bewusst *nicht* beantwortet, der
   neue Hook `useGemerkterFehler` ist der Kandidat, falls „Feld im Hook" gewinnt.
5. `git log` gegen `1ad45c2`; **nichts ist gepusht.**
