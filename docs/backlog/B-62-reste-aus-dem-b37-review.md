---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Sohn-Arcade Reste B-37, Doppelte Übungssitzung, Nochmal versuchen ohne Versuch]
status: idee
prio: P3
art: Defekt
quelle: docs/backlog/B-37-uebung-abbruch-unvollendet.md
unverifiziert: true
---

# B-62 · Drei Reste aus dem B-37-Review (Sohn-Arcade)

Der `frontend-reviewer`-Lauf zu [B-37](B-37-uebung-abbruch-unvollendet.md) hat neben den behobenen
Befunden drei Stellen gefunden, die **nicht** von B-37 stammen oder deren Zuschnitt gesprengt hätten. Sie
liegen alle in der Sohn-Arcade und sind hier abgelegt statt als „offen:"-Vermerk.

1. **`SohnPractice.tsx:45-62` startet die Sitzung ohne Ref-Gate.** Der Effekt hat nur ein `alive`-Flag;
   `SohnTest.tsx:69` hat für denselben Fall eine `startedFor`-Ref. Unter StrictMode oder einem Remount
   setzt das zwei `startSession`-POSTs ab, die zweite Sitzung bleibt bei Cursor 0 offen liegen. Das ist
   genau das Muster aus der Memory-Notiz „Effekt-Doppellauf: POST braucht Ref-Gate". **Harmlos für die
   Pflicht** — die Erledigt-Regel fragt `rounds.Any(…)`, eine Sitzung bei Cursor 0 verdirbt also nichts —,
   aber seit B-37 ist die Sitzung die Recheneinheit, und Karteileichen sind von einem Fortsetzungspunkt
   nicht unterscheidbar (E6 hat den Aufräumlauf bewusst abgelehnt).
2. **„Nochmal versuchen" bleibt nach dem letzten Versuch stehen.** `SohnTest.tsx:230` bietet den Neustart
   im Ergebnisbildschirm unverändert an; ist der Tagesdeckel aus B-37/E3 erreicht, führt der Klick in die
   Fehlerbox. Seit dem Review-Nachtrag steht dort wenigstens ein deutscher Satz
   (`api.ts` → `GERMAN_PROBLEM_TEXT.test_attempts_exhausted`), aber der richtige Weg wäre, den Knopf gar
   nicht mehr anzubieten. Dafür müsste der Bildschirm wissen, der wievielte Versuch das war — heute weiß
   er es nicht, und der Vertrag sagt es ihm nicht.
3. **Zwei Lücken im E2E-Durchstich.** `full-flow.spec.ts:79-80` sieht „Runde beenden" nur, klickt ihn nie
   (ein Klick würde den restlichen Durchstich abschneiden) — der Übungs-Ausstieg samt `endSession`-Cleanup
   ist damit nur durch Integrationstests belegt, nicht im Browser. Und `full-flow.spec.ts:117`
   (`getByRole("link", { name: /TEST/ })`) ist ungescopet: sobald ein Plan eine zweite prüfbare Position
   trägt, kippt Playwrights Strict-Mode. Beides wäre in einem eigenen kurzen Spec billiger als im
   Durchstich.

**Ungeprüft** ist der Umfang von Punkt 1: ob es in der Arcade weitere Effekte gibt, die einen POST ohne
Ref-Gate absetzen. Die drei Stellen oben sind mit `Datei:Zeile` belegt.

## Verlauf

- **2026-08-01** — angelegt aus dem `frontend-reviewer`-Lauf zu B-37. Bewusst nicht dort mitgemacht:
  Punkt 1 ist vorbestehend und nicht von B-37 verursacht, Punkt 2 braucht eine Vertrags-Entscheidung
  (der Bildschirm kennt die Versuchsnummer nicht), Punkt 3 ist Testarbeit an einem anderen Spec.
