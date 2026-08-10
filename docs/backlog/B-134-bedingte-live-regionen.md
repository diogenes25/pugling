---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/a11y]
aliases: [Live-Regionen hinter einer Bedingung, aria-live-Sweep, Wächter für Live-Regionen]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-132 (Nachtlauf 2026-08-09, Entscheidung 2)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-134 · Dreizehn Live-Regionen entstehen zusammen mit ihrem Text — und schweigen darum

Abgespalten von [B-132](B-132-hinweis-live-region-haengt-aus.md) (Entscheidung 2). B-132 hat die drei
Hinweise im Fachlehrer-Formular repariert; beim Bauen zeigte die Messung, dass es kein Einzelfall ist.
Der Sweep über zehn Dateien und ein Wächter darüber sind eine andere Arbeit als jener Fix — deshalb eine
eigene Story, wie es `docs/backlog/README.md` für einen Fund beim Bauen verlangt.

## User Story

Als **Nutzer eines Screenreaders** möchte ich Rückmeldungen der App tatsächlich hören — Fehlermeldungen,
Bestätigungen, Toasts —, statt dass sie stumm erscheinen, weil ihre Live-Region im selben Moment
entsteht wie ihr Text.

## Ist-Stand am Code

Die Regel steht im Repo bereits ausformuliert, an der Komponente, die es richtig macht:
`frontend/src/components/StatusBanner.tsx:9-12` — „Die Live-Region steht **immer** im DOM, auch ohne
Meldung: viele Screenreader sagen nur an, was in eine *bereits vorhandene* Region hineinwächst."

Gemessen am 2026-08-09 (`grep` über `frontend/src` nach `aria-live` und `role="status"`, jede Fundstelle
einzeln angesehen — nicht hochgerechnet): **dreizehn Regionen stehen hinter einer Bedingung**, in zehn
Dateien. Die dreizehnte kam am 2026-08-10 dazu, gefunden vom `frontend-reviewer` beim Review von B-123 —
sie hebelt denselben `StatusBanner` aus, dessen Kopfkommentar die Regel begründet.

| Datei:Zeile | Bedingung davor | Was stumm bleibt |
| --- | --- | --- |
| `vater/VaterExerciseCreate.tsx:270` | `{error && …}` | Fehlermeldung beim Anlegen einer Übung |
| `vater/VaterExerciseCreate.tsx:272` | `{okMsg && …}` | Erfolgsmeldung |
| `vater/VaterMedia.tsx:250` | `{msg && …}` | Rückmeldung nach dem Bild-Upload |
| `vater/VaterPlanCreate.tsx:81` | `{error && …}` | Fehler beim Plan-Anlegen |
| `vater/VaterWizard.tsx:397` | `{error && …}` | Fehler im Lehrplan-Assistenten |
| `sohn/SohnPractice.tsx:360` | `{toast && …}` | Toast während des Übens |
| `sohn/SohnShop.tsx:178` | `{msg && …}` | Toast im Shop |
| `vater/VaterVocab.tsx:465` | `{action.message && !action.message.ok && …}` | Fehler an der Vokabelzeile |
| `vater/VaterVocab.tsx:489` | dieselbe Bedingung | Fehler an der zweiten Zeile |
| `vater/VaterLogin.tsx:45` | `{registeredId !== null && …}` | „Konto angelegt, deine Id ist …" |
| `vater/exerciseConfig.tsx:619` | `{choicesMissAnswer(r) && …}` | Validierung „Antwort fehlt in der Auswahl" |
| `components/ListControls.tsx:61` | `if (total <= shown) return null;` | Hinweis auf abgeschnittene Treffer |
| `vater/VaterLehrwerke.tsx:177` | `{action.message && <tr>…}` | Rückmeldung beim Löschen einer Reihe |

Richtig gemacht (Region dauerhaft, Inhalt wechselt): `StatusBanner.tsx:16`, `SohnApp.tsx:94-96`
(Münz-/Gem-/Streak-Chips), `ListControls.tsx:46` (Seitenanzeige) und seit B-132
`VaterFachlehrer.tsx:128-144`.

## Die echte Lücke

Nicht „Barrierefreiheit wurde vergessen" — das Gegenteil: an allen dreizehn Stellen hat jemand `role`
und `aria-live` bewusst gesetzt. Die Lücke ist, dass **die Attribute allein die Zusicherung nicht
tragen** und die dafür nötige zweite Hälfte (die Region muss die Bedingung überleben) nur an einer
Stelle dokumentiert ist — im Kommentar von `StatusBanner`, den niemand liest, der gerade ein `{error &&
…}` tippt.

Das ist damit die Sorte Regel, die dieses Repo sonst mechanisch hält („mechanische Tore statt
Disziplin"). Ein Sweep ohne Wächter stellt denselben Zustand in drei Monaten wieder her.

## Offene Punkte

1. **Sweep plus Wächter, oder nur der Sweep?** Empfehlung: beides, und der Wächter ist der eigentliche
   Wert — ein Vitest, der über die `.tsx`-Quellen im Frontend läuft und `role="status"`/`aria-live`
   direkt hinter `&&` oder in einem früh-`return null`-Zweig meldet, mit Ausnahmeliste und Begründung
   je Eintrag (Muster: die Ausnahmelisten der `ConventionGuardTests`). Kosten: ein quellentext-lesender
   Test ist ein grober Parser und wird Fehlalarme haben — das Repo hat damit Erfahrung
   (`B-40`, „halbe Parser brauchen eine Rot-Liste").
2. **Ist `role="status"` überall überhaupt richtig?** Mindestens zwei Fälle sind zweifelhaft:
   `ListControls.tsx:61` ist ein statischer Hinweis über den Filterstand, keine Rückmeldung auf eine
   Handlung, und `exerciseConfig.tsx:619` ist eine Validierungsmeldung, für die `role="alert"` die
   passendere Rolle wäre. Empfehlung: beim Sweep je Fundstelle entscheiden statt mechanisch
   umzuformen — sonst wird aus dreizehn richtigen Attributen dreizehn falsche Dauer-Regionen.
3. **Reicht `StatusBanner` als Ersatz für die meisten?** Acht der dreizehn sind schlichte
   Banner/Toasts, für die es die Komponente längst gibt. Empfehlung: dort ersetzen statt reparieren —
   das entfernt die Fehlerquelle, statt sie an dreizehn Stellen einzeln zu umgehen. Kosten: die Optik der
   Toasts (`className="toast"` in der Sohn-Arcade) unterscheidet sich vom Banner; das muss angesehen
   werden, bevor es getauscht wird.

## Akzeptanzkriterien

1. Jede der dreizehn Fundstellen ist entweder auf eine dauerhaft gerenderte Region umgestellt, durch
   `StatusBanner` ersetzt, oder mit begründetem Kommentar als bewusste Ausnahme markiert.
2. Ein Wächter meldet eine neu hinzukommende bedingte Live-Region; seine Ausnahmeliste trägt je Eintrag
   einen Grund.
3. Die Frontend-Suite bleibt grün, die sichtbare Darstellung ändert sich nirgends.

## Verlauf

- **2026-08-09** — angelegt und zugleich ausformuliert im Nachtlauf (Sprint 1, beim Bau von B-132). Der
  Ist-Stand ist gemessen, nicht geschätzt: zwölf Fundstellen einzeln klassifiziert, dazu die vier
  Gegenbeispiele, die es richtig machen. **Bewusst nicht in B-132 mitgenommen:** dessen Ziel — die drei
  Hinweise im Fachlehrer-Formular — ist ohne diesen Sweep erfüllt, und ein Wächter über neun Dateien ist
  eine eigene Arbeit mit eigenen Entscheidungen (Punkt 2 zeigt, dass er nicht mechanisch ist).
- **2026-08-10** — dreizehnte Fundstelle ergänzt (`VaterLehrwerke.tsx:177`, Rückmeldung beim Löschen
  einer Reihe), gefunden vom `frontend-reviewer` beim Review von B-123. Sie ist der lehrreichste der
  Fälle: dort wird ein `StatusBanner` **bedingt** gerendert, also genau die Komponente ausgehebelt,
  deren Kopfkommentar die Regel begründet. Zahlen im Text nachgezogen (zwölf → dreizehn, neun → zehn
  Dateien).
