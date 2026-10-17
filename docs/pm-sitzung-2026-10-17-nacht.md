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
- **Sprint 2 abgenommen** (B-224, B-225, Lehrwerke-Seite): 72/72 Playwright, 381 Vitest, Reviewer ohne
  Fund mit Codewirkung, Browser-Rollengang als Creator. Nachschau von Sprint 1: ein kleiner Defekt (B-227)
  und zwei Nuancen (B-228, B-229), eine Test-Lücke geschlossen. **Der Lauf endet nach Sprint 2** (Limit 2:
  Defekt im Increment eines Sprints dieser Nacht, siehe „Retrospektive Sprint 2").

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
  `plan-kinderliste-faellt-aus`, `fachlehrer-faecher-fallen-aus`); fünf neue Vitest-Fälle in bestehenden
  Dateien (ReferenceSelect 1, Fachlehrer 1, Wizard 1, HistoryTab 2) plus zwei neue Hook-Testdateien (3 + 5
  Fälle). Die „7 rot von 44" der roten Probe zählt die drei `exerciseTypes`-Fälle mit (Nachschau-Korrektur).

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

## Sprint 2 — Ziel & Umfang

**Sprint-Ziel, aus der Creator-Sicht:**

> Der **Creator** liest auf der Lehrwerke-Seite nach jeder Handlung – auch am Verlag – genau **eine**
> Rückmeldung, und ein Knopf, der nichts tun kann, ist gesperrt und sagt am Feld, warum.

**Umfang** (beide aus der Nachschau zu Sprint 2026-09-05, `art: Defekt`, in dieser Sitzung von
`ausformuliert` autonom auf `geschaetzt` gebracht):

| Story | Richtung |
| --- | --- |
| [B-224](backlog/B-224-verlagsbaustein-traegt-zweiten-meldungsplatz.md) | Das Verlags-Vokabular trug den letzten zweiten Meldungsplatz der Seite |
| [B-225](backlog/B-225-reihe-anlegen-schweigt-bei-leerem-namen.md) | „Reihe anlegen" war ohne Namen klickbar und stumm |

**Draußen:** B-226 (`Frage`, gesperrt), B-164 (Nutzer), die übrigen `ausformuliert`-Defekte anderer Flächen
(B-134, B-176, B-194 ist abgenommen, B-201, B-211, B-181, B-190, B-131, B-159, B-175).

**Entwickler-Brief:** `nachObenMelden` zieht nach `src/lib/`; `PublisherAdmin` bekommt `onMeldung`
(Pflicht) und meldet seine zwei Server-Pfade nach oben, der eigene `StatusBanner` entfällt; Öffnen des
Vokabulars und `UnitPanel`-„Bearbeiten" räumen; „Reihe anlegen" `disabled` ohne Namen, Label „(Pflicht)".
Reihenfolge: rote E2E zuerst, dann Code, dann die neun Specs der Seite, Suite, Reviewer, Rollengang.

## Iteration 2 — umgesetzt

- `src/lib/nachObenMelden.ts` (umgezogen, exportiert, Docblock um die Herkunft ergänzt);
  `VaterLehrwerke.tsx` importiert; zwei nun unbenutzte Imports entfernt (tsc).
- `PublisherAdmin.tsx`: `onMeldung` Pflicht, `speichern`/`loeschen` über `nachObenMelden`, kein eigener
  Banner mehr, Aufklappen räumt. `PublisherRow` unverändert.
- `VaterLehrwerke.tsx`: `UnitPanel`-„Bearbeiten" räumt; „Reihe anlegen" gesperrt ohne Namen; Label
  „Reihe (Pflicht)"; Kommentar unter dem Formular sagt jetzt, was der Code tut.
- Zwei E2E: `verlag-meldet-am-platz-der-seite.spec.ts`, `reihe-anlegen-braucht-namen.spec.ts`.

**Rote Proben (erwartet/gemessen), vor dem Fix:** `reihe-anlegen-braucht-namen` 1/1 an Zeile 22
(`toBeDisabled` → `enabled`); `verlag-meldet-am-platz-der-seite` 1/1 an Zeile 44 (Meldung im
Vokabular-Abschnitt: erwartet 0, gemessen 1).

**Verifikation:** `tsc -b` sauber · `vitest run` 381/381 · die neun Specs der Lehrwerke-Seite samt der zwei
neuen **14/14** · volle Playwright-Suite **72/72** (3,5 min) · Reviewer und Nachschau: siehe unten.

## Runde 2 — Re-Review / Abnahme

Browser-Rollengang in Chrome (Freigabe 6), wie in Runde 1: Backend mit frischer Wegwerf-DB, `npm run dev`,
Klicks per DOM, Zustand per Skript gelesen.

- **Creator (Lehrwerke-Seite) — signed off.** O-Ton: „Ohne Namen ist „Reihe anlegen" grau, und am Feld steht
  „(Pflicht)" – ich muss nicht raten. Ich habe einen Verlag angelegt, das Vokabular aufgeklappt (die
  Bestätigung von eben war weg, richtig so), ihn umbenannt – und „Gespeichert." stand **einmal**, unten am
  Platz der Seite, nicht im Vokabular. Dann die Reihe angelegt: „Gespeichert." weg, „… steht im Katalog"
  da. Eine Rückmeldung, ein Platz." Gemessen: Knopf gesperrt bei leerem und bei nur-Leerzeichen-Namen,
  bedienbar mit Namen; `.banner.ok` nach dem Umbenennen genau `["Gespeichert."]`, im Vokabular-Abschnitt
  keiner; nach dem Reihe-Anlegen nur noch der Katalog-Satz.
- **Vater, Sohn — Regressionszeugen:** kein Weg ihrer Flächen im Diff; die volle Suite (72/72) und 381 Vitest
  stehen für sie.

**Reviewer (`frontend-reviewer`):** kein Blocker, **kein Fund mit Code- oder Testwirkung** – Zähler dieses
Sprints 0 von 5. Drei Kommentar-Hinweise eingearbeitet: die `toHaveCount(1)`-Zeile der Verlags-Spec war vor
dem Fix auch wahr (jetzt als Schutz beschriftet, die tragende Zeile benannt), die `toHaveValue("")`-Zeile ist
eine Vorbedingung (beschriftet), zwei Formulierungen behaupteten „den letzten `StatusBanner`" statt „den
letzten Platz mit Server-Meldungen" bzw. „keine Eingabeprüfung" statt „keine gemeldete" (berichtigt).

## Retrospektive Sprint 2

**Nachschau:** Sprint 1 dieser Nacht (B-216, B-215, B-194, B-185, Commit `d834a8e`) durch einen frischen
`frontend-reviewer`-Agenten gegen den Stand `d834a8e` gelesen, mit eigener Laufzeitprobe (tsc, 381 Vitest,
die fünf neuen Specs 7/7 auf einer isolierten Kopie). **Ein Defekt mit Nutzerwirkung:** das
Fehler-Gedächtnis überlebt nicht nur die Wiederholung, sondern auch den Start einer *anderen* Suche
(Filterwechsel nach einem Fehler zeigt den alten Text mit „Lädt…") →
[B-227](backlog/B-227-fehlergedaechtnis-ueberlebt-den-filterwechsel.md) (`entgangen_bei: B-216`).
**Zwei Konsistenz-Nuancen:** die gefüllte Verlaufskarte ohne Fokusregel →
[B-228](backlog/B-228-mehr-laden-verliert-den-fokus.md); „(nicht geladen)" auch während des Ladens →
[B-229](backlog/B-229-nicht-geladen-waehrend-des-ladens.md) (`entgangen_bei: B-194`). **Eine
Test-Lücke** (B-215 AK 4 nie zugesichert) sofort geschlossen, **zwei Doku-Fehler** (B-216 AK 3 widersprach
Entscheidung 5; Zählung der Vitest-Fälle im Protokoll) berichtigt. `nachgeschaut` auf allen vier gesetzt.
**Index: Nachgeschaut 133 von 148 — 35 mit Defekt** (Abend: 126 von 142 — 32).

**Was die Tore von Sprint 1 durchgelassen haben (B-227):** Der Hook wurde aus *einem* Übergang abgeleitet
(„`reload()` setzt `error = null, loading = true`") und gegen genau diesen getestet. Dass `useAsync`
denselben Übergang auch bei einem Wechsel der Abhängigkeiten erzeugt, steht in `useAsync.ts:45-48` und in
`frontend/CLAUDE.md` („die andere Hälfte derselben Falle") – gelesen, aber beim Ableiten der Regel nicht
mitgezählt. Reviewer und E2E prüften die Wiederholung, nicht den Filterwechsel; die Nachschau fand es durch
die Frage „welche anderen Auslöser erzeugen denselben Zustand?".

**Was Sprint 2 selbst durchgelassen hat:** nichts mit Codewirkung (Reviewer 0, Suite grün, Rollengang
deckungsgleich).

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

**Wer eine Regel aus einem Zustandsübergang ableitet, listet vor dem Bauen alle Auslöser dieses Übergangs
auf und entscheidet je Auslöser.** Das ist die Schärfung des unentschiedenen Retro-Vorschlags 4 vom
2026-09-03 („beim Übertragen eines Musters alle Stellen des Vorbilds auflisten") um den Fall, dass das
„Vorbild" ein Hook-Zustand ist: `(error = null, loading = true)` hat in `useAsync` drei Auslöser (Erstlauf,
`reload`, Dep-Wechsel), B-216 hat einen bedacht. Ort: `frontend/CLAUDE.md` neben der „Wiederkehrenden Falle
beim Platzhalter", als ein Satz; Kosten: Budget-Skript, eine Zeile kompensieren. Kein Tor möglich – welche
Auslöser ein Übergang hat, weiß kein Regex.

### Warum der Lauf hier endet

Nicht am Fund-Zähler (Sprint 1: 4 von 5, Sprint 2: 0), sondern an **Limit 2 des Backlog-Laufs**
(`docs/backlog/README.md`, „Der Backlog-Lauf"): Die Nachschau hat im Increment eines Sprints dieser Nacht
einen Defekt gefunden (B-227). Die Nachtlauf-Fassung verfeinert dieses Limit für Review-Funde *während* eines
Sprints (Fünfer-Zähler); für einen Nachschau-Fund im abgenommenen Increment gilt die allgemeine Regel, und
dieselbe Lage hat am 2026-09-02 die Nacht 2 beendet. Weiterlaufen wäre die falsche Reaktion – B-227 ist klein,
aber es ist ein Defekt in Arbeit, die dieselbe Nacht für fertig erklärt hat.

## Offene Roadmap · Änderungsstand

Die dauerhafte Liste ist [docs/backlog/](backlog/README.md). Änderungsstand: Sprint 1 `d834a8e` + Nachtrag
`d436a1b`; Sprint 2 samt Nachschau-Nachträgen (B-227/228/229, Test-Lücke, Doku) als ein Commit, Hash im
Nachtrag der Stories B-224/B-225; **nichts gepusht**.

## Für den Morgen

1. **Zwei Retro-Vorschläge dieser Nacht** landen oder verwerfen (Sprint 1: „X statt Y" beidseitig
   zusichern; Sprint 2: Auslöser eines Zustandsübergangs auflisten) – dazu die fünf älteren, unverändert offen.
2. **B-227** (Fehler-Gedächtnis überlebt den Filterwechsel) ist `ausformuliert`, `art: Defekt`, mit
   Empfehlung (Schlüssel-Parameter am Hook) – autonom grillbar in einem Folgelauf. **B-228** und **B-229**
   sind `idee`.
3. **B-226** ist eine `Frage` (zwei Münzstände im Shop) – gesperrt für den Lauf, Entscheidung beim Nutzer:
   erst klären, ob die Differenz fachlich ist.
4. **B-164** bleibt beim Nutzer (Architekturfrage `useAsync`); B-216 hat sie bewusst *nicht* beantwortet, der
   neue Hook `useGemerkterFehler` ist der Kandidat, falls „Feld im Hook" gewinnt – und B-227 ist ein Argument
   dafür, die Frage bald zu entscheiden.
5. `git log` gegen `1ad45c2`: vier Commits dieser Nacht (Sprint 1 + Nachtrag, Sprint 2 + Nachtrag);
   **nichts ist gepusht.**
