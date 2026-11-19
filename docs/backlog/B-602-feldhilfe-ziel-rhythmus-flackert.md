---
tags: [typ/story, status/ausformuliert, bereich/qualitaet, rolle/entwickler]
aliases: [Feldhilfe-Spec flackert]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Sprint 82, Verifikation von B-597 (pm-sitzung-2026-11-16-backlog-lauf.md)
grund: ""
ersetzt_durch: []
---

# B-602 · `feldhilfe.spec.ts` flackert beim „Ziel-Rhythmus“

Im Sprint 82 fiel `frontend/e2e/feldhilfe.spec.ts` („Positions-Formular: jedes erklärungsbedürftige Feld hat den
passenden Hinweis“) in einem von drei Einzelläufen rot: Nach dem Klick auf das „ⓘ“ am Feld „Ziel-Rhythmus“ erschien
kein `note` mit „Wochenziel“ einmal pro Woche“ (10 s Wartezeit). Im vollen Lauf fiel einmal stattdessen ein Fall mit dem
Münz-Malus (ebenfalls `getByRole("note")`). Der Diff des Sprints berührt das Positionsformular nicht.

## User Story

Als Entwickler möchte ich, dass die Feldhilfe-Spezifikation nur dann rot wird, wenn ein Hinweis am falschen Feld hängt
oder nicht aufgeht, damit ein Rot dieser Spec nie mehr „einfach nochmal laufen lassen“ heißt.

## Ist-Stand am Code

Die Vermutung der Idee („der Klick kommt, während das Formular noch nachlädt“) stimmt, und der Mechanismus ist
**gewollt**, nicht zufällig:

1. **Das Popover schließt, sobald sich sein Knopf bewegt.** `InfoHint` merkt sich beim Öffnen, wo der Knopf stand
   (`frontend/src/components/InfoHint.tsx:80`), und prüft je Animationsframe, ob er dort noch steht
   (`InfoHint.tsx:38-48`, Schwelle 1 px). Eine Bewegung ruft `close()`. Das ist Absicht (B-273, Akzeptanzkriterium 1:
   „Inhalt, der über dem Knopf aufgeht, schließt ein offenes ⓘ“, `docs/backlog/B-273-feste-feldhilfe-resize-und-layoutverschiebung.md:74`).
2. **Über dem „Ziel-Rhythmus“-Feld lädt Inhalt nach.** Das Feld sitzt in `PositionFields`, und das steht im Planbau
   *unterhalb* der Filterleiste und der Trefferliste der Katalog-Übungen (`frontend/src/vater/PlanPositions.tsx:883`,
   Liste ab `:794`). Die Liste kommt aus einer eigenen Abfrage (`useAsync(... api.searchExercises ...)`,
   `PlanPositions.tsx:642`). Bis sie antwortet, steht dort ein einzeiliges „Lade…“ (`:794`); danach eine Liste mit
   bis zu 240 px Höhe (`:799`). Das Formular selbst erscheint dagegen schon mit dem Bestand der Positionen
   (`PlanPositions.tsx:106`), also früher. Antwortet die Suche *nach* dem Klick auf das „ⓘ“, springt der Knopf nach
   unten, `moved()` greift, das Popover schließt – und `getByRole("note")` findet nichts.
3. **Die Spec wartet nur auf den Knopf, nicht auf die Liste.** Sie legt den Plan an und wartet auf die Überschrift
   (`frontend/e2e/feldhilfe.spec.ts:30`), dann auf die Sichtbarkeit des ersten Knopfs (`:63`) und klickt
   (`:64`). Die Trefferzahl „(N Treffer)“ als Zeichen, dass die Liste steht (`PlanPositions.tsx:756`), kommt in der
   Spec nicht vor. Im zweiten Fall der Datei (Escape, `:71-88`) wartet nicht einmal eine `toBeVisible`-Zusicherung vor
   dem Klick (`:77`); Playwrights Stabilitätsprüfung deckt nur zwei Frames ab.
4. **Dieselbe Lage in zwei weiteren Fällen derselben Datei:** der Gleichlaut-Fall klickt am Planbau
   (`feldhilfe.spec.ts:94-96`) und im Assistenten (`:112-115`) ohne Warten auf die Trefferliste. Die im Backlog
   genannte Zeile `:89` ist durch Commits seit Sprint 82 verrutscht; der Münz-Malus steht heute auf `:47`, `:75`
   und `:94`.
5. **Die Last-Abhängigkeit passt dazu:** Der Playwright-Lauf nutzt einen Worker (`workers: 1`) und eine echte Wegwerf-DB
   (`frontend/playwright.config.ts:46-47`), die Suchantwort kommt also je nach Last früh oder spät. Einzeln und ohne
   Last gewinnt die Liste das Rennen meistens – 2 von 3 Läufen grün entspricht dem.

Nicht nachgestellt: Messen und Starten sind in diesem Lauf nicht erlaubt. Der Mechanismus ist am Code gelesen; dass er
den beobachteten Fall erzeugt, belegt erst die rote Probe unten (Akzeptanzkriterium 3).

## Die echte Lücke

Kein Fehler im Produkt, sondern eine **Spec ohne Wartebedingung** für etwas, das der Produktcode absichtlich nicht
toleriert: Die Spec klickt in ein Layout, das sich noch bewegt. Zwei Dinge sind schmaler als die Idee dachte:

- Es ist nicht der erste Klick allein. Jeder Klick auf ein „ⓘ“ vor dem Ende der Suche ist betroffen, in drei der
  drei Fälle der Datei.
- Die Ursache ist deterministisch herstellbar (Suchantwort verzögern), also lässt sich die Reparatur *beweisen*
  statt durch mehrere grüne Läufe zu „belegen“.

Ein Randbefund für den Nutzer, kein Teil dieser Story: Ein Vater, der in diesen Sekunden auf ein „ⓘ“ tippt, sieht
genauso ein sich schließendes Popover. Die Feldhilfe-Position ändert sich nicht, die Liste darüber springt.

## Offene Punkte

1. **Nur die Spec reparieren oder auch die Layoutverschiebung beseitigen?** Empfehlung: **nur die Spec.** Das Schließen
   bei Bewegung ist durch B-273 beschlossen und getestet; die Verschiebung zu beseitigen (Höhe der Trefferliste
   reservieren) wäre ein eigenes Produktvorhaben für Handy-Nutzer und gehört in eine eigene Story.
2. **Worauf wartet die Spec?** Empfehlung: auf „(N Treffer)“ im Beschriftungstext der Katalog-Auswahl
   (`getByText(/\(\d+ Treffer\)/)`), denn der Text erscheint erst, wenn die Zeilen zum Filter gehören
   (`PlanPositions.tsx:756`: sonst „Suche läuft…“). Nicht `waitForTimeout`, nicht ein Wiederholen des Klicks mit
   `toPass` (das verdeckte genau die Bewegung, die B-273 meldet).
3. **Eine Hilfsfunktion für alle drei Fälle?** Empfehlung: ja, in `feldhilfe.spec.ts` selbst (`planbauBereit(page)`),
   weil sie von `planAnlegen` her folgt. Der Assistent im dritten Fall hat eine eigene Trefferliste und bekommt dort
   dieselbe Wartebedingung; ob sein Beschriftungstext ebenfalls „(N Treffer)“ trägt, ist im Code nicht geprüft
   (`frontend/src/vater/VaterWizard.tsx`: keine Fundstelle für „Treffer)“ – nicht gefunden).
4. **Gehört die Probe mit verzögerter Suchantwort in die Spec?** Empfehlung: ja, als eigener Fall nach dem Vorbild
   `planbau-ladefenster.spec.ts:57` (`page.route("**/api/v1/creator/exercises?**", …)`): Er hält die Antwort zurück,
   klickt das „ⓘ“, gibt die Antwort frei und erwartet das geschlossene Popover. Ein Fall, der die verzögerte Lage
   *dauerhaft* herstellt, ist die einzige Möglichkeit, die Reparatur nicht von der Last abhängig zu belegen.

## Entwurf der Akzeptanzkriterien

1. Alle drei Fälle von `feldhilfe.spec.ts`, die ein „ⓘ“ im Planbau anklicken, warten vorher auf „(N Treffer)“.
   *Rot, wenn* die Wartebedingung in einem Fall fehlt: mit der verzögerten Suchantwort aus Kriterium 3 fällt genau
   dieser Fall.
2. Der Assistent im Gleichlaut-Fall (`:112-115`) wartet auf das Äquivalent seiner Trefferliste. *Rot, wenn* es fehlt,
   unter derselben verzögerten Antwort für den Assistenten (`/creator/exercises?**` wird dort ebenfalls abgefragt –
   nicht geprüft, beim Schätzen klären).
3. Ein Fall in der Spec verzögert die Suchantwort per `page.route` um ein Mehrfaches der Frame-Zeit, klickt das
   „ⓘ“ vor der Antwort und erwartet danach das **geschlossene** Popover (`aria-expanded="false"`). *Rot, wenn* jemand
   die Schließen-bei-Bewegung-Regel aus `InfoHint.tsx:45` entfernt (B-273 bleibt geschützt) – und es dokumentiert die
   Ursache des Flackerns als bekannte, gewollte Lage.
4. Die rote Probe der Reparatur: Wartebedingung in einem Fall entfernen **und** Verzögerung aus Kriterium 3 darüber
   stellen → genau dieser Fall wird rot; mit Wartebedingung grün. *Rot, wenn* die Wartebedingung wirkungslos ist
   (z. B. auf einen Text wartet, der vor der Antwort schon steht).
5. Kein Fall der Datei verwendet `waitForTimeout` oder eine Wiederholungsschleife um den Klick. *Rot, wenn* ein
   Wartebefehl diese Form hat; als Durchsicht beim Abnehmen, kein Tor.

## Verlauf

- **2026-11-16** — angelegt bei der Verifikation von B-597 (Sprint 82): 2 von 3 Einzelläufen grün.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Ursache am Code gelesen (`InfoHint.tsx` schließt bei
  Bewegung des Knopfs, B-273; die Trefferliste unter dem Formular lädt nach), Zeilennummern der Idee waren verrutscht;
  nichts gemessen oder gestartet.
