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
  und zwei Nuancen (B-228, B-229), eine Test-Lücke geschlossen. **Der erste Lauf endet nach Sprint 2** (Limit 2:
  Defekt im Increment eines Sprints dieser Nacht, siehe „Retrospektive Sprint 2").
- **Zweiter Lauf ab 22:53** („starte den nächsten Nachtlauf"). Vorab: Story-Id B-216 war doppelt → B-230.
- **Sprint 3 abgenommen** (B-227, B-228, B-229 – die Nachschau-Funde zu Sprint 1): 389/389 Vitest, **76/76**
  Playwright, Reviewer im dritten Anlauf ohne Blocker, `web-design-guidelines` ohne Fund, Browser-Rollengang als
  Vater und Sohn. Drei Review-Funde mit Code-/Testwirkung (Grenze fünf), darunter ein echter aus der eigenen
  E2E. Nachschau Sprint 2: kein Defekt, zwei Fragen (B-232, B-233), eine Test-Lücke geschlossen, drei
  Berichtigungen. Retro-Mechanismen nur **vorgeschlagen** (ein Tor, eine Prosa-Regel). Commit `5a91d27` +
  Nachtrag `27cf127`.
- **Sprint 4 abgenommen** (B-175, B-211, B-199 – Behauptungen, die ihr Code aufgehoben hat; kein sichtbares
  Verhalten): tsc, 389 Vitest, zwei Config-Specs, Reviewer ohne Codewirkung (ein falscher Hash berichtigt), Rollen
  als Regressionszeugen. Nachschau Sprint 3: **ein Defekt** (B-235, Unit-Feld im Lehrbuch-Formular, `entgangen_bei:
  B-229`), eine Begründung berichtigt. **Der Lauf endet nach Sprint 4** (Limit 2, siehe „Retrospektive Sprint 4").
  Commit `49b1fbc` + Nachtrag.

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

**Rote Proben (erwartet/gemessen), vor dem Fix:** `reihe-anlegen-braucht-namen` 1/1 an Zeile 22 – heute
Zeile 24, zwei Reviewer-Kommentare kamen nach der Messung dazu (Nachschau 2026-10-18)
(`toBeDisabled` → `enabled`); `verlag-meldet-am-platz-der-seite` 1/1 an Zeile 44 – heute 46 (Meldung im
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

## Zweiter Lauf derselben Nacht (ab 22:53)

Auftrag „starte den nächsten Nachtlauf" nach dem Halt an Limit 2. Ausgangsstand `d93d705` (sieben Commits vor
`origin/main`, ungepusht); kein Backend lief, ein Vite-Dev-Server stand noch auf :5173 (Playwright nutzt ihn
wieder, `reuseExistingServer`). Chrome-Extension in dieser Sitzung verfügbar (Freigabe 6). Kein neuer
`anmerkungen`-Befund (`docs/anmerkungen/aktuell.md` vom 2026-08-14, beide Einträge erledigt).

**Vorab gefunden, nicht gesucht:** Die Story-Id **B-216 war doppelt vergeben** – an „Zwei Wiederhol-Knöpfe
verschwinden …" (seit 2026-09-04, abgenommen, mit Commits und `entgangen_bei`-Verweisen) und an „Der
PIN-Hash liegt doppelt" (Projektanalyse vom 2026-10-17 vormittags, `idee`). Der Index zeigte beide Zeilen als
B-216; der Generator prüft Ids nicht auf Eindeutigkeit (`backlog-index.awk` kennt nur doppelte YAML-Schlüssel
innerhalb einer Datei). Die jüngere Story ist nach **B-230** umnummeriert (Verlauf dort). Ein Tor dafür ist ein
Retro-Kandidat, nicht gelandet (Freigabe 3).

## Sprint 3 — Ziel & Umfang

**Sprint-Ziel, aus zwei Rollensichten:**

> Der **Vater** sieht nach einer gescheiterten Übungssuche beim Drehen am Filter, dass eine *neue* Suche läuft –
> nicht den alten Fehler mit einem Knopf, der zu einer Abfrage gehört, die es nicht mehr gibt; und im
> Plan-Formular heißt sein Kind nur dann „nicht geladen", wenn die Liste wirklich nicht kam. Der **Sohn** behält
> nach einem Fehler beim „Mehr laden" den Fokus bei der Meldung.

**Umfang** (die drei Nachschau-Funde zu Sprint 1, alle `art: Defekt`, in diesem Lauf autonom von
`ausformuliert`/`idee` auf `geschaetzt` gebracht):

| Story | Richtung |
| --- | --- |
| [B-227](backlog/B-227-fehlergedaechtnis-ueberlebt-den-filterwechsel.md) | Das Fehler-Gedächtnis bekommt einen Schlüssel: ein Fehler gilt nur für die Abfrage, in der er auftrat |
| [B-228](backlog/B-228-mehr-laden-verliert-den-fokus.md) | Die gefüllte Verlaufskarte bekommt die Fokusregel der vier anderen Stellen |
| [B-229](backlog/B-229-nicht-geladen-waehrend-des-ladens.md) | `ReferenceSelect` unterscheidet „lädt noch" von „nicht geladen" |

**Draußen, mit Namen:** B-231 (neu, `Frage`: wohin der Fokus ohne Fehlerkasten gehört – aus B-228 E2, gesperrt
unter Freigabe 1), B-226 (`Frage`, zwei Münzstände), B-164 (Nutzer), B-230 (`Aufräumen`, `idee`, Backend –
anderes Thema), die übrigen `ausformuliert`-Defekte anderer Flächen (B-134, B-176, B-201, B-211, B-181, B-190,
B-131, B-159, B-175).

**Warum dieses Thema:** Es ist genau die Arbeit, an der der erste Lauf angehalten hat – Defekte im Increment
derselben Nacht. Weiterzulaufen, ohne sie zu schließen, hieße die Qualitätsschwelle liegen zu lassen, an der
Limit 2 gegriffen hat.

### Entwickler-Brief

- **Ziel:** siehe Sprint-Ziel. Kein Backend berührt, kein Vertrag, keine Migration.
- **B-227:** `useGemerkterFehler(fehler, laedt, schluessel?)` – der gemerkte Fehler trägt seinen Schlüssel,
  Vergleich am Rückgabepunkt (kein Effekt-Reset, sonst ein Render Verzug). `WizardTrefferbereich` bekommt eine
  Pflicht-Prop `schluessel`, gespeist aus `filterKey`; `AddPosition` reicht sein `filterKey`. `useAsync`
  unberührt (B-164).
- **B-228:** Banner der gefüllten `HistoryTab`-Karte mit `tabIndex={-1}`/`ref`; „Mehr laden" fokussiert es
  beim Klick, wenn es steht.
- **B-229:** `ReferenceSelect` mit `laedt?: boolean` → Sonderoption „(lädt…)" und `disabled`;
  `PlanEditForm` reicht `children.showPlaceholder`.
- **Reihenfolge:** rote Vitest-Proben (vier Dateien) → Hook → Trefferbereich/Planbau → Shop-Karte →
  `ReferenceSelect`/Plan-Formular → zwei neue E2E-Specs (rot vor dem Fix gemessen) → `tsc -b`, `vitest run`,
  volle Playwright-Suite → `frontend-reviewer` (+ `web-design-guidelines`, Freigabe 7) → Browser-Rollengang →
  Commit → Nachschau Sprint 2 → Retro.
- **Testweg:** je Story in `## Schätzung`; Fenster-Messungen mit Frist < Verzögerung
  (Muster `planbau-ladefenster.spec.ts`).

## Iteration 3 — umgesetzt

**Frontend (kein Backend berührt, kein Vertragsbruch, keine Migration):**

- `useGemerkterFehler(fehler, laedt, schluessel?)`: der gemerkte Fehler trägt seinen Schlüssel, Vergleich am
  Rückgabepunkt; **die Effekt-Abhängigkeiten bleiben `[fehler, laedt]`** – siehe Fund 1. `WizardTrefferbereich`
  mit Pflicht-Prop `schluessel` aus `filterKey`; `AddPosition` reicht sein `filterKey` (B-227).
- `HistoryTab`, gefüllte Karte: Banner `tabIndex={-1}`/`ref`, „Mehr laden" fokussiert es (B-228).
- `ReferenceSelect` mit `laedt?: boolean` → Sonderoption „(lädt…)" und gesperrtes Feld, solange die Liste zum
  ersten Mal lädt; `PlanEditForm` reicht `children.showPlaceholder` (B-229).
- Acht neue Vitest-Fälle (Hook 4, Trefferbereich 1, `HistoryTab` 1, `ReferenceSelect` 2), drei neue E2E-Specs
  (`filterwechsel-raeumt-den-fehler` mit zwei Fällen, `plan-kinderliste-laedt-noch`, aus der Nachschau
  `verlag-fehler-am-platz-der-seite`), ein Kommentar in `VaterLehrwerke.tsx` berichtigt (Nachschau).

**Rote Proben (erwartet/gemessen), vor dem Fix in einem Lauf:** Vitest **4 rot von 37** – `useGemerkterFehler.test.ts:64`
(`null` erwartet, „Netz weg" gemessen), `ReferenceSelect.test.tsx:124` („(lädt…)" erwartet, „(nicht geladen)"
gemessen), `HistoryTab.test.tsx:117` (Banner erwartet, `<body>` gemessen), `VaterWizard.test.tsx:89` („Suche
läuft…" nicht im DOM); E2E **3 rot von 3** – `filterwechsel` beide Fälle an Zeile 37 (`läuft` binnen 1,2 s nicht
sichtbar), `plan-kinderliste-laedt-noch` Zeile 39 („Sohn (nicht geladen)"). Zweite Hook-Probe nach Fund 1:
`useGemerkterFehler.test.ts:83` **1 rot von 9** („Netz weg" statt `null`).

**Verifikation:** `tsc -b` sauber · `vitest run` **389/389** (47 Dateien; vorher 381) · volle Playwright-Suite
**76/76** (3,9 min; vorher 72) · `frontend-reviewer` im **dritten** Anlauf (zwei Agenten brachen an `ENOTFOUND`
ab; Freigabe 2 wäre der Rückfall gewesen, wurde nicht gebraucht): kein Blocker, kein Should-fix, vier Nits ·
Skill `web-design-guidelines` (Freigabe 7) auf `ReferenceSelect`, `SohnShop`, `VaterPlanDetail`: kein Fund
(Ladetext endet auf „…", Label vorhanden, Fokusring über `.banner[tabindex]:focus-visible`, Knopf nur während der
Anfrage gesperrt).

### Review-Funde (Zähler: 3 von 5)

| # | Quelle | Fund | Behebung |
| --- | --- | --- | --- |
| 1 | eigene E2E (volle Suite 73/75 nach dem ersten Fix) | Vitest grün, Browser rot: der Filterwechsel rendert zuerst mit dem **alten** Fehler und dem **neuen** Schlüssel – `useAsync` räumt `error` erst im Effekt. Der Hook-Effekt hing am Schlüssel und merkte den alten Fehler unter dem neuen. | `schluessel` aus den Effekt-Abhängigkeiten, gemerkt wird nur, wenn `fehler` erscheint; zwei Vitest-Fälle für die echte Render-Sequenz, einer davon rot gemessen |
| 2 | `frontend-reviewer` | `#wiz-type` per `selectOption({ index: 1 })` – Hausregel „nie per Index" | per Label „Vokabelkarten" |
| 3 | `frontend-reviewer` | Banner-Zusicherung in `plan-kinderliste-laedt-noch` mit Standardfrist – gehört nicht zum Fenster, trägt nichts | Frist 1,2 s |

Ohne Zähler (Kommentar/Doku): Hook-Doc behauptete „`ListenAusfall`-Stellen ohne wechselnde Abhängigkeiten" –
die Arten-Liste des Assistenten hängt am Fach, ist aber durch den Schritt-Wechsel abgebaut, bevor es sich ändern
kann (so benannt); B-227 schätzte zwei Vitest-Fälle, es wurden vier.

## Runde 3 — Re-Review / Abnahme

Browser-Rollengang in Chrome (Freigabe 6): Backend mit Wegwerf-DB (Seed Papa #1 / Sohn #1), bestehender Vite-Dev-Server;
Abrufe über einen Fetch-Wrapper **in der Seite** abgeschossen bzw. um 2,5 s verzögert, Zustand per Skript im
Fenster gelesen. Neu gemessen: Eingaben aus der isolierten Welt der Extension (`el.value = …` +
`dispatchEvent`) erreichen Reacts Input-Tracker **nicht** – Text wird mit echten Tastatur-Ereignissen getippt
(`computer.type`), DOM-Klicks und `select`-`change` funktionieren weiter.

- **Vater (Planbau, Plan-Detail) — signed off.** O-Ton: „Die Suche war kaputt, ich stelle das Fach um – und
  jetzt steht da „(Suche läuft…)" statt dem alten Fehler mit einem Knopf, der „Lädt…" sagt und zu nichts mehr
  gehört. Danach „(6 Treffer)". Und im Plan: „Bearbeiten" gedrückt, das Kind-Feld sagt „Sohn (lädt…)" und ist
  kurz grau, dann „Sohn (#1)". Vorher stand da „nicht geladen", und ich habe nach einem Fehler gesucht, den es
  nicht gab." Gemessen: im Fenster kein Banner, kein „Lädt…"-Knopf, Zählzeile „(Suche läuft…)"; Kind-Feld genau
  eine Option „Sohn (lädt…)", `disabled`, kein Banner; danach „Sohn (#1)", bedienbar. Assistent per E2E
  (derselbe Baustein wie im Planbau).
- **Sohn (Shop-Verlauf, gefüllte Karte) — signed off.** Vorbereitung über die echte API: Papa schreibt 1 100
  Münzen gut, legt „Sticker" (1 Münze, Bestand 60) an, der Sohn kauft 21-mal (24 Käufe, Seite 20). O-Ton: „Die
  Liste hat 20 Sachen und unten „Mehr laden (20 von 24)". Das zweite Laden geht schief, da steht „Failed to
  fetch" über der Liste. Ich drück nochmal – und der Fokus sitzt auf der Meldung, der Knopf sagt „Lädt…". Dann
  sind alle 24 da." Gemessen: `document.activeElement` ist das Banner, Knopf „Lädt…" gesperrt, Meldung steht;
  danach 24 Zeilen, kein Banner, kein Knopf. Beobachtung nebenbei: Kopfzeile und Shop-Kopf zeigten hier
  denselben Stand (🪙 979 / 979) – B-226 trat in diesem Durchlauf nicht auf.
- **Creator — Regressionszeuge, mit einem eigenen Gang:** kein Weg seiner Flächen im Diff (nur ein Kommentar in
  `VaterLehrwerke.tsx`); `typ-manifest-faellt-aus`, `uebungstypen` und die neun Lehrwerke-Specs grün. Zusätzlich
  für die Nachschau auf `/vater/lehrwerke` gelaufen: Verlag angelegt, Vokabular geöffnet (räumt die
  Bestätigung – nachgemessen, nachdem ein erster Durchlauf das Vokabular schon offen vorfand), Verlag umbenannt
  → „Gespeichert." am Seitenplatz bei Viewport-y 302, der OK-Knopf bei y 807 (Desktop, 889 px) – beides
  sichtbar, siehe B-232.

## Retrospektive Sprint 3

**Nachschau:** Sprint 2 dieser Nacht (B-224, B-225, Commit `dc2ce84`) durch einen frischen `frontend-reviewer`
gegengelesen, je mit benanntem Prüfpunkt (acht Server-Schreibpfade der Seite auf `nachObenMelden` gezählt,
verbleibende lokale Banner als Eingabeprüfung eingeordnet, `nachObenMelden`-Docblock gegen `useAction`
nachgerechnet, E2E-Zeilen gegen den Ausgangszustand, drei Sperren auf `trim()`, Importer des Umzugs). **Kein
Defekt mit Nutzerwirkung im Sinne einer Reparatur** – aber **ein vermuteter**, den ich im Browser nachgemessen
habe: die Bestätigung einer Verlags-Aktion liegt bei langer Reihen-Tabelle außerhalb des Sichtfelds (bei vier
Reihen 505 px über dem Knopf, sichtbar; bei dreißig nicht mehr). Das ist B-212s ausdrückliche Entscheidung
(„Bestätigung darf unsichtbar sein"), die B-224 um einen fernen Sender erweitert hat, ohne sie neu zu wägen –
darum [B-232](backlog/B-232-bestaetigung-ausserhalb-des-sichtfelds.md) als **`Frage`**, nicht als Defekt mit
`entgangen_bei`, und darum greift Limit 2 hier nicht. **Drei Berichtigungen:** B-224 AK 3 ruhte auf einer
falschen Prämisse (409 beim Umbenennen auf „Klett" ist auslösbar; der rote Pfad des `onMeldung`-Vertrags war
ungetestet → E2E `verlag-fehler-am-platz-der-seite.spec.ts` nachgereicht, 1/1 grün), B-225 Entscheidung 2
begründete den `return` falsch (Enter feuert bei gesperrtem Submit-Knopf nicht; Kommentar nachgezogen), die
Zeilenangaben beider roten Proben waren um zwei verschoben (Reviewer-Kommentare kamen nach der Messung).
**Eine zweite Frage:** zwei Sperr- und zwei Leer-Eingabe-Muster auf einer Seite →
[B-233](backlog/B-233-zwei-sperrmuster-auf-der-lehrwerke-seite.md). `nachgeschaut` auf beiden gesetzt.
**Index: Nachgeschaut 135 von 151 — 35 mit Defekt** (Beginn dieses Laufs: 133 von 148 — 35).

**Was die eigenen Tore dieses Sprints durchgelassen haben:**

1. **Der Vitest modellierte den Übergang atomar, der Browser macht ihn in zwei Schritten.** `rerender({ fehler:
   null, laedt: true, schluessel: "b" })` ändert drei Props in einem Render; in der App ändert der Aufrufer den
   Schlüssel, und `useAsync` räumt `error` erst in seinem Effekt – ein Render mit altem Fehler und neuem Schlüssel
   dazwischen. Der erste Fix war darum in Vitest grün und im Browser falsch; die E2E fand es (Fund 1). Es ist die
   Fortsetzung der gestrigen Lektion („Auslöser eines Zustandsübergangs auflisten") um die Frage, **in welcher
   Reihenfolge** ein Übergang ankommt.
2. **Eine Story-Id war doppelt vergeben, und kein Tor hat es gemeldet.** B-216 stand seit dem 2026-09-04 an einer
   abgenommenen Story und wurde am 2026-10-17 vormittags ein zweites Mal vergeben; der Index zeigte zwei Zeilen
   mit derselben Id. Der Generator prüft Schlüssel nur innerhalb einer Datei.
3. **Zwei Reviewer-Agenten brachen am Netz ab** (`ENOTFOUND`), der dritte Anlauf lief durch. Freigabe 2 (Selbst-Check,
   Story bleibt `in-arbeit`) stand bereit – der Fall zeigt, dass ein Netzfehler keine Aussage über den Code ist
   und ein Wiederholen vor dem Rückfall richtig war.

### Vorgeschlagene Mechanismen — nicht gelandet (Freigabe 3)

**Tor:** `backlog-index.sh` bricht ab (oder meldet in der Mängelliste), wenn zwei Story-Dateien dieselbe Id
tragen. Ort: `.claude/scripts/backlog-index.awk`, eine Zählung je Id über alle Dateien. Kosten: ein paar
Zeilen awk; ein roter Index statt einer stillen Doppelung. Es ist ein **Tor**, also die Sorte Mechanismus, die
der Lauf nicht selbst landen darf – und die, wäre sie gelandet, den Lauf nach Limit 2 beendet hätte.

**Prosa:** Wer einen Hook gegen einen Übergang testet, den ein Effekt eines **anderen** Hooks erzeugt
(`useAsync`: `error = null, loading = true` im Effekt), spielt die Zwischen-Renders nach – zuerst die Props, die
der Aufrufer ändert, dann die, die der Effekt ändert – statt alle in einem `rerender` zu setzen. Ort:
`frontend/CLAUDE.md`, der Absatz zu Komponenten-/Hook-Tests, ein Satz; Kosten: Budget-Skript, eine Zeile
kompensieren. Kein Tor möglich – welche Props ein Effekt setzt, weiß kein Regex. Es schärft den gestrigen
Vorschlag „Auslöser auflisten" um „…und ihre Reihenfolge nachspielen".

**Bewusst ungedeckt:** die Sichtbarkeit einer Meldung im Viewport (B-232) – `toBeVisible` prüft sie nicht, und
ob sie sichtbar sein *soll*, ist eine offene Produktfrage.

**Warum der Lauf weitergeht:** kein Limit greift – der Fund-Zähler steht bei 3 von 5, die Retro hat kein Tor
gelandet (nur vorgeschlagen), und die Nachschau fand im Increment dieser Nacht keinen Defekt, sondern eine
Frage an eine ältere Entscheidung.

## Sprint 4 — Ziel & Umfang

**Sprint-Ziel, aus der Entwickler-Sicht:**

> Der **Entwickler** liest in Kommentar, Test und Workflow nur Begründungen, die noch gelten – keine Zahl, die
> ihr Fix widerrufen hat, keine Freigabe, die ihr eigener Sprint aufgehoben hat, und keine Zusicherung, die am
> Ausgangszustand hängt.

**Umfang** (alle autonom grillbar, in diesem Lauf von `ausformuliert` auf `geschaetzt` gebracht, je XS):

| Story | Art | Richtung |
| --- | --- | --- |
| [B-175](backlog/B-175-zwei-gerottete-zahlen-in-kommentaren.md) | Defekt | Zwei verrottete Zahlen (`playwright.config.ts`, `e2e.yml`) werden gestrichen, nicht korrigiert |
| [B-211](backlog/B-211-interestchips-zusicherung-kann-nicht-fallen.md) | Defekt | Die „sperrt Hinzufügen"-Zusicherung wählt erst, dann sperrt sie – und fällt jetzt |
| [B-199](backlog/B-199-drei-behauptungen-die-ihr-eigener-sprint-aufgehoben-hat.md) | Aufräumen | Fall 1: der `VaterWizard`-Kommentar nennt die Wiederholung; Fall 2 war seit `e9bc135` erledigt; Fall 3 → B-234 (`Frage`) |

**Draußen, mit Namen:** B-201 (Feldhilfe; hängt an der Frage B-186 – die Story empfiehlt selbst „erst B-186
klären"), B-234 (neu, `Frage`), die A11y-Familie B-134/B-176 (anderes Thema), B-181/B-190/B-131 (Backlog- und
Test-Infrastruktur), B-159 (Backend).

**Warum dieses Thema:** Es ist die Fehlerklasse, die diese Nacht zweimal selbst produziert hat (zwei Zeilenangaben
roter Proben, ein Hook-Doc) – Behauptungen, die ihr Code oder ihr Sprint aufgehoben hat. Drei alte Nachschau-Funde
derselben Klasse liegen seit August/September; der Sprint räumt sie in einem Gang.

**Entwickler-Brief:** keine Verhaltensänderung, kein Backend, kein Vertrag. B-175: beide Zahlen streichen,
Verweis auf B-139/B-175. B-211: `fireEvent.change` auf das Pulldown, Zwischenzusicherung „bedienbar", dann
`rerender` mit `disabled`; rote Probe gegen ein vorübergehend mutiertes `ExerciseEditModal.tsx`
(`disabled ||` gestrichen), danach `git checkout`. B-199: Kommentar `VaterWizard.tsx` (Fall 1), Fall 2 belegen,
Fall 3 als B-234. Testweg: `tsc -b`, `vitest run`; keine E2E nötig (kein sichtbares Verhalten), die drei Rollen
sind Regressionszeugen.

## Iteration 4 — umgesetzt

- `frontend/playwright.config.ts`: „drei Nächte" gestrichen, Verweis auf B-139/B-175; `.github/workflows/e2e.yml`:
  „25 Tests" gestrichen, das Limit begründet sich ohne Testzahl (B-175).
- `frontend/src/vater/InterestChips.test.tsx`: der Sperr-Fall wählt erst „Comic", sichert „bedienbar" zu, rendert
  dann mit `disabled` neu (B-211). **Rote Probe (erwartet/gemessen):** `ExerciseEditModal.tsx:764` vorübergehend
  ohne `disabled ||` → `InterestChips.test.tsx:105` **1 rot von 8** (`true` erwartet, `false` gemessen); nach
  `git checkout` 8/8.
- `frontend/src/vater/VaterWizard.tsx`: der Kommentar zu `seitenSchluessel` behauptet nicht mehr, die Suche rufe
  nirgends `reload()` – „Erneut suchen" tut es seit B-162, und genau dort sperrte die Ladezustands-Variante
  fälschlich (B-199, Fall 1). Fall 2 war seit `e9bc135` erledigt (zwei `deferred`, Messung zwischen `reload()`
  und Auflösung) – der zuerst notierte Hash `81eb60c` war falsch, vom Reviewer nachgewiesen (siehe Review-Funde);
  Fall 3 → [B-234](backlog/B-234-dritter-zustand-der-arten-liste.md) (`Frage`).
- Keine roten Proben für B-175 und B-199 – Kommentare, kein Verhalten; so beschriftet (Auflage 5).

**Verifikation:** `tsc -b` sauber · `vitest run` 389/389 · zwei Playwright-Specs (`einkleidung`,
`reihe-anlegen-braucht-namen`) 2/2 als Beleg, dass die geänderte Config lädt und die Einkleidung unverändert
läuft · Reviewer und Nachschau: siehe unten.

### Review-Funde (Zähler: 0 von 5)

`frontend-reviewer`: kein Blocker. **Ein Should-fix ohne Codewirkung:** der in B-199 und hier zuerst genannte
Commit für Fall 2 (`81eb60c`) war falsch – dort stand noch die schwache Fassung; die Zwei-`deferred`-Messung
kam mit `e9bc135`. An drei Stellen berichtigt. Ironie des Sprints, ausdrücklich notiert: eine verrottete Zahl
in einem Sprint gegen verrottete Zahlen – gefunden vom Reviewer, nicht von mir. Drei Nits (Wortwahl im
Kommentar, Zählweise „1 von 8", B-234 ohne `entgangen_bei`; die ersten zwei übernommen, das dritte nicht:
B-234 ist eine `Frage`, das Feld gilt Defekten).

## Runde 4 — Re-Review / Abnahme

Kein sichtbares Verhalten hat sich geändert – zwei Kommentare, ein Workflow-Kommentar, ein Testfall. Ein
Browser-Rollengang hätte nichts zu sehen; stattdessen, ausdrücklich als Ersatz benannt:

- **Entwickler (Sprint-Ziel) — signed off.** O-Ton: „Die drei Sätze, die ich beim nächsten roten Lauf gelesen
  hätte, sagen jetzt, was gilt: keine Nächte-Zahl, keine Testzahl, kein ‚ruft nirgends `reload()`'. Und der
  Sperr-Test fällt, wenn man die Sperre ausbaut – ich habe es gesehen: 1 von 8 rot." Gemessen: `grep "ruft
  nirgends"` leer; Mutante → `:105` rot; `tsc -b` sauber.
- **Creator, Vater, Sohn — Regressionszeugen:** 389/389 Vitest; `einkleidung.spec.ts` (die Fläche des
  geänderten Tests, aus der Creator-Rolle) und `reihe-anlegen-braucht-namen.spec.ts` 2/2 gegen die geänderte
  Playwright-Config; die volle Suite stand vor den Kommentar-Änderungen bei 76/76 (Sprint 3), und keine Änderung
  dieses Sprints berührt Laufzeitcode.

## Retrospektive Sprint 4

**Nachschau:** Sprint 3 dieser Nacht (B-227, B-228, B-229, Commit `5a91d27`) durch einen frischen
`frontend-reviewer` gegen den Stand gelesen, mit eigener Laufzeitprobe (tsc, 389 Vitest, Zählung der 76
E2E-Fälle), je mit benanntem Prüfpunkt (Zustandsmaschine des Hooks in fünf Sequenzen, Schlüssel an allen
Konsumenten mit wechselnden Deps, `ReferenceSelect`-Zweigfolge, Fokus-Reihenfolge, rote Proben gegen den
Ausgangszustand, Kommentare gegen Code, Zählungen des Protokolls). **Ein Defekt mit Nutzerwirkung:** B-229s
Prämisse „die sieben anderen `ReferenceSelect`-Stellen laden auf Seitenebene" war **gezählt, nicht
nachgesehen** – die Unit im Lehrbuch-Formular (`ChildMaterialSection.tsx:192`) lädt im Formular und zeigt beim
Öffnen „(nicht geladen)", exakt das Bild, das B-229 am Kind-Feld behoben hat →
[B-235](backlog/B-235-unit-feld-sagt-nicht-geladen-waehrend-des-ladens.md) (`entgangen_bei: B-229`, `ausformuliert`,
Fix benannt: eine Zeile). Der `frontend-reviewer` der Bau-Runde hatte „sieben andere Aufrufstellen unverändert"
gemeldet – dieselbe Zählung. **Berichtigt:** B-227 Entscheidung 2 begründete den Vergleich am Rückgabepunkt
mit einem Render Verzug, den es so nicht gibt (Docblock nachgezogen, zwei bewusste Grenzen des Hooks benannt);
E2E-Zeile 43 der Kinderliste-Spec als Absichtserklärung beschriftet. Nuancen: Schlüssel-Typ `unknown`,
`VaterWizard.test.tsx` modelliert den Übergang weiter atomar, Trim-Asymmetrie im Assistenten. `nachgeschaut`
auf allen drei gesetzt. **Index: Nachgeschaut 138 von 154 — 36 mit Defekt** (nach Sprint 3: 135 von 151 — 35;
Beginn der Nacht: 126 von 142 — 32).

**Was die Tore von Sprint 3 durchgelassen haben (B-235):** Eine Behauptung über *andere* Stellen („laden auf
Seitenebene") wurde als Begründung formuliert und darum von niemandem geprüft – Reviewer und Autor zählten die
Stellen, sahen aber nicht in jede hinein. Es ist dieselbe Klasse wie der gestrige Retro-Satz („alle Auslöser
auflisten") in einer anderen Dimension: nicht die Übergänge *eines* Zustands, sondern die *Orte* einer Regel.

**Was Sprint 4 selbst durchgelassen hat:** der falsche Commit-Hash (`81eb60c`) – eine verrottete Zahl im Sprint
gegen verrottete Zahlen, vom Reviewer gefunden. Ohne Codewirkung, aber genau die Fehlerklasse.

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

**Wer eine Regel „an Stelle X, weil die anderen Stellen Y erfüllen" baut, listet die anderen Stellen mit
`Datei:Zeile` und der je geprüften Eigenschaft auf – im Ist-Stand der Story, nicht als Zählung.** Ort:
`docs/backlog/README.md` → „Ausformulieren heißt gegen den Code belegen", ein Satz. Begründung: B-229 ist der
dritte gemessene Fall, in dem „die übrigen N Stellen" eine Behauptung war (B-172 fand sieben Stellen, die
„die Regel schon haben" sollten; B-216 fand eine dritte Stelle beim Ausformulieren). Kosten: eine Zeile Prosa im
Backlog-README (nicht resident), je Story einige Zeilen Ist-Stand. Kein Tor möglich – welche Stellen „die
anderen" sind, weiß kein Regex.

### Warum der Lauf hier endet

**Limit 2 des Backlog-Laufs** (`docs/backlog/README.md`, „Der Backlog-Lauf"): Die Nachschau hat im Increment
eines Sprints dieser Nacht einen Defekt gefunden (B-235, aus Sprint 3). Dieselbe Lage wie am Ende des ersten
Laufs (B-227 aus Sprint 1) und wie am 2026-09-02. Der Fund ist klein und sein Fix benannt – aber er steht in
Arbeit, die dieselbe Nacht für fertig erklärt hat, und das ist die Schwelle, an der Weiterlaufen die falsche
Reaktion ist. Sprint 4 wird regulär abgeschlossen (Reviewer, Belege, Commit); danach legt sich der Lauf hin.

## Offene Roadmap · Änderungsstand

Die dauerhafte Liste ist [docs/backlog/](backlog/README.md). Änderungsstand: Sprint 1 `d834a8e` + Nachtrag
`d436a1b`; Sprint 2 samt Nachschau-Nachträgen (B-227/228/229, Test-Lücke, Doku): `dc2ce84`, dazu der Nachtrag mit
diesem Hash; Sprint 3 (zweiter Lauf, B-227/228/229 samt Nachschau-Nachträgen zu B-224/B-225, B-230–B-233):
`5a91d27`, dazu der Nachtrag `27cf127`; Sprint 4 (B-175/211/199 samt Nachschau-Nachträgen zu B-227/228/229,
B-234, B-235): `49b1fbc`, dazu der Nachtrag mit diesem Hash; **nichts gepusht**.

## Für den Morgen

1. **Fünf Retro-Vorschläge dieser Nacht** landen oder verwerfen (Sprint 1: „X statt Y" beidseitig
   zusichern; Sprint 2: Auslöser eines Zustandsübergangs auflisten; Sprint 3: Zwischen-Renders eines
   Effekt-Übergangs nachspielen, und als **Tor** die Id-Eindeutigkeit im Index-Generator; Sprint 4: „die
   anderen Stellen" mit `Datei:Zeile` und geprüfter Eigenschaft belegen statt zählen) – dazu die fünf älteren,
   unverändert offen.
2. **Fünf offene Fragen** (gesperrt für den Lauf, Freigabe 1): B-226 (zwei Münzstände im Shop – in Sprint 3
   nicht reproduziert, 979/979), B-231 (Fokus ohne Fehlerkasten), B-232 (Bestätigung außerhalb des
   Sichtfelds – im Browser gemessen), B-233 (zwei Sperrmuster auf der Lehrwerke-Seite), B-234 (dritter Zustand
   der Arten-Liste, aus B-199).
3. **B-227, B-228, B-229 (Sprint 3) und B-175, B-211, B-199 (Sprint 4) sind abgenommen.** **B-235** (Unit-Feld
   im Lehrbuch-Formular sagt „nicht geladen" beim Laden, `ausformuliert`, `art: Defekt`, Fix benannt: eine
   Zeile) ist der natürliche nächste Sprint – und der Grund, warum der Lauf hier endet. **B-230** ist die
   umnummerierte PIN-Hash-Story (vorher doppelt als B-216).
4. **B-164** bleibt beim Nutzer (Architekturfrage `useAsync`); B-216 hat sie bewusst *nicht* beantwortet, der
   Hook `useGemerkterFehler` trägt jetzt auch einen Schlüssel – zwei Nächte Arbeit an einem Gedächtnis neben
   `useAsync` sind ein Argument, die Frage bald zu entscheiden.
5. `git log` gegen `1ad45c2`: vier Commits des ersten Laufs (Sprint 1 + Nachtrag, Sprint 2 + Nachtrag) plus
   die Commits des zweiten Laufs (Sprint 3 + Nachtrag, ggf. Sprint 4); **nichts ist gepusht.**
