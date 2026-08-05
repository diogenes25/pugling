---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/student]
aliases: [Geräte-Vorbehalt, Klang und Haptik]
status: geschaetzt
prio: P2
art: Frage
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-07-05.md
wartet_auf: ein echtes Handy — Klang und Haptik sind nicht maschinell zu beurteilen
---

# B-31 · Geräte-Vorbehalt: Klang und Haptik am echten Handy gegenhören

## User Story

Als **Vater** möchte ich wissen, ob der Erfolgs-Ton und die Vibration der Sohn-Arcade auf einem echten
Handy auch wirklich **gut klingen und sich richtig anfühlen** — nicht nur, dass sie technisch auslösen —
damit ich die Story guten Gewissens als abgeschlossen betrachten kann, statt mich auf eine Vermutung zu
verlassen.

## Ist-Stand am Code

Ton und Vibration sind gebaut und zentral verdrahtet — das ist recherchierbar und unstrittig:

- **Ton**: synthetisiert per Web Audio API, keine Asset-Datei
  ([feedback.ts:66-78](../../frontend/src/lib/feedback.ts)) — ein `OscillatorNode` (Dreieckswelle) je Note,
  mit kurzem Attack und exponentiellem Ausklingen. Drei Stufen mit unterschiedlichen Notenfolgen
  ([feedback.ts:53-58](../../frontend/src/lib/feedback.ts)): `small` ein Blip (880 Hz), `medium` zwei Töne,
  `big` ein C-Dur-Arpeggio (C5–E5–G5–C6).
- **Haptik**: `navigator.vibrate(...)` mit stufenabhängigem Muster
  ([feedback.ts:60-64,103-109](../../frontend/src/lib/feedback.ts)) — `small` ein kurzer Puls, `big` eine
  fünfteilige Sequenz. Bleibt aus bei „Bewegung reduzieren" (`prefersReducedMotion()`,
  [feedback.ts:104](../../frontend/src/lib/feedback.ts)).
- **Auslöser**: beide hängen an `playCelebration(tier)`, aufgerufen von `celebrate(...)`
  ([Celebration.tsx:30](../../frontend/src/components/Celebration.tsx)) — zentral für jede Feier, nicht an
  jeder Aufrufstelle einzeln verdrahtet. Aufrufer sind `SohnPractice.tsx:103,105` (Combo-Treffer),
  `SohnTest.tsx:46` (Klausur bestanden/durchgefallen), `SohnShop.tsx:60` (Kauf) und
  `GamificationPanels.tsx:59,111` (Mission/Abzeichen).
- **Stummschaltung**: ein HUD-Schalter (`SohnApp.tsx:58,82-84`) spiegelt `isMuted()`/`setMuted()`
  ([feedback.ts:22-33](../../frontend/src/lib/feedback.ts)) nach `localStorage`; bei `muted` passiert gar
  nichts (`feedback.ts:85`).
- **Absicherung**: fehlt `AudioContext` oder `navigator.vibrate`, oder wirft einer der beiden Aufrufe,
  bleibt es folgenlos (`try`/`catch` um jeden Zweig, [feedback.ts:87-109](../../frontend/src/lib/feedback.ts))
  — bewusst so gebaut, damit der Headless-E2E-Lauf nicht daran zerbricht.

Das ist der vollständige Code-Ist-Stand: **was** auslöst, **wann**, und **wie robust**. Keiner dieser
Punkte beantwortet die eigentliche Frage.

## Die echte Lücke

Ob der synthetische Ton **gut klingt** und die Vibrationsmuster sich **richtig anfühlen**, ist keine
Code-Eigenschaft — es ist eine Wahrnehmung, die nur ein Mensch an einem echten Lautsprecher/Vibrationsmotor
beurteilen kann. Kein Test, kein Automat und keine Code-Recherche kann das entscheiden: ein Unit- oder
E2E-Test kann höchstens belegen, dass `playCelebration` aufgerufen wurde und nicht wirft — nicht, dass das
Ergebnis gut klingt. Diese Lücke bleibt nach diesem Durchgang **unverändert offen**; es wurde in dieser
Runde kein echtes Gerät geprüft, es wurde nur der Code-Ist-Stand recherchiert, der die Prüfaufgabe
präzisiert.

## Entscheidungen

Statt der ursprünglichen offenen Fragen liegt jetzt eine konkrete, noch ausstehende **manuelle
Prüfaufgabe** vor:

- **Wer prüft**: der Vater (Nutzer) selbst — das ist keine Aufgabe, die an einen Agenten oder Reviewer
  delegierbar ist, weil die Wahrnehmung persönlich ist.
- **Womit**: ein echtes Android-Gerät **und** ein echtes iOS-Gerät (Empfehlung), weil `navigator.vibrate`
  auf iOS/Safari nicht unterstützt ist — dort ist also nur der Ton zu prüfen, auf Android beides. Ein
  einzelnes Gerät genügte für den Ton, aber nicht für die Aussage „Haptik fühlt sich richtig an" auf beiden
  Plattformen.
- **Wann**: sobald der Vater Gelegenheit hat, die Sohn-Arcade (`/sohn`) auf einem eigenen oder geliehenen
  Gerät zu öffnen und mindestens einen Treffer, eine Combo und einen Kauf auszulösen (die drei Stufen
  `small`/`medium`/`big` decken das ab).
- **Kein erfundenes Ergebnis**: diese Story trifft **keine** Aussage darüber, ob Ton/Haptik gut sind —
  das kann nur die Prüfung selbst liefern. Das Ergebnis kommt als Verlaufs-Eintrag zurück, wenn der Vater
  geprüft hat.

## Akzeptanzkriterien

- Erfolgs-Ton und Vibration wurden auf einem echten Android-Gerät gehört/gespürt, Ergebnis dokumentiert
  (gut / Mangel benannt).
- Erfolgs-Ton wurde auf einem echten iOS-Gerät gehört, Ergebnis dokumentiert (Vibration entfällt dort
  mangels `navigator.vibrate`-Unterstützung — das ist keine Lücke dieser Story, sondern eine
  Plattformgrenze).
- Bei einem benannten Mangel: die Story geht mit dem Befund zurück in die Kette (`ausformuliert`), statt
  direkt `verworfen` oder `abgenommen` zu werden.
- Ohne Mangel: die Story wird `abgenommen`.

## Schätzung

**XS · frontend · keine Migration · kein Vertragsbruch.**

**Größe XS**: es entsteht **kein** Code — die gesamte Arbeit ist eine reine manuelle Wahrnehmungsprüfung
an echten Geräten, kein Bau, kein Test, keine Zeile Produktionscode.

**Testweg**: **manueller Test am echten Gerät (kein automatisierter Ersatz möglich).** Ein Unit- oder
E2E-Test könnte höchstens den Aufruf von `playCelebration` mocken und dessen Ausführung ohne Exception
belegen — das ist bereits durch die bestehende Suite implizit abgedeckt (kein Wurf in Headless-Umgebungen)
und beantwortet die eigentliche Frage nicht. Die Klang-/Haptik-**Qualität** entzieht sich jeder
Automatisierung.

## Verlauf

- **2026-07-30** — aus der PM-Sitzung vom 2026-07-05 geerntet; steht seit dort unter Geräte-Vorbehalt.
- **2026-08-03** — **ausformuliert** (autonom getroffen, Nutzerauftrag 2026-08-04): der Code-Ist-Stand ist
  jetzt vollständig recherchiert und mit `Datei:Zeile` belegt (`feedback.ts`, `Celebration.tsx`,
  `SohnApp.tsx` und die vier Aufrufstellen der Sohn-Arcade). Die eigentliche Prüfung — klingt/fühlt es sich
  gut an — ist **weiterhin nicht durchgeführt**: das ist keine Code-Eigenschaft und wurde in diesem
  Durchgang bewusst nicht simuliert oder erfunden.
- **2026-08-03** — **gegrillt** (autonom getroffen, Nutzerauftrag 2026-08-04): die ursprünglichen offenen
  Fragen sind durch eine einzige, konkrete Entscheidung ersetzt — wer (Vater), womit (Android **und** iOS,
  wegen der `navigator.vibrate`-Plattformgrenze), wann (nächste Gelegenheit an einem echten Gerät). Kein
  Testergebnis wurde dabei vorweggenommen.
- **2026-08-03** — **geschätzt** (autonom getroffen, Nutzerauftrag 2026-08-04): **XS · frontend · keine
  Migration · kein Vertragsbruch**, Testweg **manueller Test am echten Gerät (kein automatisierter Ersatz
  möglich)**. Die Story bleibt bewusst bei `art: Frage` und wird **nicht** `verworfen` — die Geräteprüfung
  selbst steht nach diesem Durchgang weiterhin aus und ist erst mit einem dokumentierten Ergebnis
  abschließbar.
