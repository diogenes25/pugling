---
tags: [typ/story, status/in-arbeit, bereich/training, bereich/katalog, rolle/student]
aliases: [Mehrere Bedeutungen, Komma-Übersetzung]
status: in-arbeit
prio: P1
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-462 · Eine Übersetzung mit mehreren Bedeutungen („sobald, sowie“) gilt getippt nur als Ganzes

Die Wortliste des Lehrbuchs nennt zu vielen Wörtern mehrere Bedeutungen in einem Eintrag. So stehen sie jetzt
im Vokabelspeicher: `as soon as → sobald, sowie`, `(to) reply (to) → antworten (auf); erwidern, entgegnen`,
`I see. → Aha! / Verstehe.`, `Welsh → Walisisch; walisisch`, `the best thing about ... → das Beste an ...`.

Gemessen im Testmodus (`POST creator/exercises/{id}/preview/check`, Übungen 13 und 26):

| Erwartet | Gegeben | Wertung |
|---|---|---|
| `sobald, sowie` | `sobald` | falsch |
| `sobald, sowie` | `sowie` | falsch |
| `antworten (auf); erwidern, entgegnen` | `antworten` | falsch |
| `Aha! / Verstehe.` | `Verstehe` | falsch |
| `Walisisch; walisisch` | `walisisch` | falsch |
| `westlich, West-` | `westlich` | falsch |
| `das Beste an ...` | `das Beste an` | falsch |
| `huge` | `Huge` | richtig |

Sobald eine Position getippt prüft (`RequireTypedTest` oder eine getippte Stufe), ist die Richtung
Englisch → Deutsch für ein Kind am Handy praktisch nicht zu bestehen, und der Stick bucht dafür Münzen ab.
In der Richtung Deutsch → Englisch trifft es Einträge wie `(to) reply (to)` und `the best thing about ...`.

Die Ursache liegt an zwei Stellen. Der Speicher kennt `TranslationAlternatives`, aber weder das Ladeskript
(`raw/english/access-pugling/laden.py`) noch der Listen-Import zerlegt einen Eintrag: `frontend/src/lib/vocabList.ts`
erklärt das Komma ausdrücklich zu keinem Trenner („Abfall, Müll is one translation in the store“). Und der
Vergleich kennt keine Regel für Klammern, Auslassungspunkte und `(to)`.

Zu klären beim Ausformulieren: zerlegt der Import (Komma/Semikolon/Schrägstrich → Hauptbedeutung plus
Alternativen), oder lässt der Vergleich jede einzelne Bedeutung gelten? Was geschieht mit den 124 schon
gespeicherten Einträgen? Verwandt: [B-457](B-457-uebersetzung-scheitert-am-schlusspunkt.md) (Schlusspunkt).

## User Story

Als Sohn möchte ich, dass eine getippte Bedeutung eines Wortlisten-Eintrags als richtig gilt, damit Punkte und
Malus messen, ob ich das Wort kenne – nicht, ob ich den Eintrag samt Komma und Klammern abschreibe.

## Ist-Stand am Code

- `AnswerGrader.Matches` (`backend/Pugling.Api/Services/Shared/AnswerGrader.cs:16`) verglich die Eingabe nur mit
  den ganzen `AcceptedAnswers` (Hauptübersetzung plus `TranslationAlternatives`).
- `ExerciseContentResolver.ResolveVocabularyItemsAsync` (`…/ExerciseContentResolver.cs:99`) reicht die
  Übersetzung so durch, wie sie im Speicher steht; in der Rückrichtung bleibt nur das Wort selbst
  (`ExerciseContentProvider.Swap`).
- `StageMechanics.Normalize` (`…/StageMechanics.cs:35`) kennt Groß/Klein, Leerzeichen und Apostroph, aber keine
  Trenner, Klammern oder Auslassungspunkte.

## Die echte Lücke

Der Speicher hält den Eintrag, wie das Buch ihn druckt. Das ist für die Anzeige richtig (das Kind soll alle
Bedeutungen sehen), für den getippten Vergleich aber falsch.

## Entscheidungen

1. **Der Vergleich lässt jede einzelne Bedeutung gelten; der Import zerlegt nicht.** Begründung: Die 124
   gespeicherten Einträge und jede künftige Liste sind damit ohne Datenpflege geheilt, und die Karte zeigt
   weiter den ganzen Eintrag. Kosten: Die Regel ist eine Heuristik im Code statt einer Angabe des Vaters –
   was sie als Bedeutung erkennt, steht nirgends in den Daten.
2. **Nur Vokabel-Atome** (`ContentItem.MeaningsSeparable`, gesetzt allein im Vokabel-Pfad des Resolvers).
   Begründung: In einem Satz (Übersetzung, Lückentext) gehört das Komma zur einen Antwort. Kosten: Eine
   Zuordnungs-Übung mit Listeneinträgen bleibt exakt.
3. **Trenner sind `;`, ` / ` (mit Leerzeichen) und `,` – nur außerhalb von Klammern; Klammerteile dürfen
   stehen oder fehlen, ein `/` in der Klammer zählt Wahlmöglichkeiten auf (`stay (at/with)`); `...` und das
   Schlusszeichen fallen weg.** Am Komma wird **nicht** getrennt, wenn der Abschnitt wie ein Satz endet
   („Ja, bitte.“) oder Auslassungspunkte trägt („Es war schade, dass ...“ – sonst gälte „dass“). Ein Punkt
   nach `sb`, `sth`, `jdm`, `jdn`, `etw`, `usw` ist eine Abkürzung, kein Satzende. Kosten: Eine Wendung mit
   Komma **ohne** Schlusszeichen („Nein, danke“) gilt schon mit einem Teil als richtig; die Abkürzungsliste
   ist Produktwissen im Code und wächst mit dem nächsten Lehrwerk.
4. **Beide Richtungen.** `(to) reply (to)` ist in der Rückrichtung genauso ein Listeneintrag.
5. **Die Auswahlstufe bietet eine Einzelbedeutung nicht als falsche Option an** (`DistractorPool`), sonst
   stünden zwei richtige Knöpfe nebeneinander.

Bewusst offen: Eine Übung, die den Artikel trainiert (`articleRequired`), ließe bei einem Eintrag der
Wörterbuchform „Haus, das“ das Wort ohne Artikel gelten. Im Speicher kommt die Form nicht vor.

Nicht Teil dieser Story: Buchstabenkästchen bilden weiter die Hauptübersetzung ab (dort tippt das Kind den
ganzen Eintrag, die Trennzeichen sind vorgegeben).

## Akzeptanzkriterien

- Jede Zeile der Messtabelle oben, die „falsch“ lautete, wird richtig gewertet – im echten Test des Kindes.
- In der Rückrichtung zählt `reply` für `(to) reply (to)`.
- Ein Atom ohne Kennzeichen (Übersetzung, Lückentext, Zuordnung) wertet unverändert exakt.
- Die Artikel-Nachsicht (B-421) gilt je Bedeutung.

## Schätzung

S, nur Backend, keine Migration, kein Vertragsbruch. Testweg: Einheitentests am Vergleich, ein
Integrationstest durch den Positions-Test, Messung im Testmodus an der Kopie der Access-Datenbank.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-11** — ausformuliert, entschieden (Defekt, Auftrag des Nutzers „Ja, B-462“) und gebaut:
  `AnswerVariants`, `ContentItem.MeaningsSeparable`/`GradedAnswers()`, Vergleich und Auswahlstufe.
  Rote Probe: ohne das Kennzeichen im Resolver fallen genau die drei Fälle von
  `Test_EineBedeutungDesEintrags_WirdAlsRichtigGewertet`. Suite 1262/1262 grün (Release). Messung an der
  laufenden API (Kopie der Access-Datenbank, `preview/check`, Übungen 13 und 26): alle sieben zuvor falschen
  Zeilen richtig; Übersetzungs-Übung 23 unverändert.
- **2026-11-11** — `pugling-reviewer`: ein blockierender Fund (ein Eintrag mit Auslassungspunkten wurde am
  Komma zerlegt, „dass“ galt für „Es war schade, dass ...“) und acht Hinweise. Eingearbeitet: Trennung nur
  außerhalb von Klammern, keine Komma-Trennung bei Auslassungspunkten, Abkürzungspunkt, `/` in der Klammer,
  Auswahlstufe in beiden Richtungen getestet, leere Zusicherungen entfernt bzw. als Absichtserklärung
  beschriftet, die Wand zur Satz-Übersetzung als Integrationstest über `preview/check`. Suite 1273/1273 grün
  (Release); Messung an der laufenden API mit dem endgültigen Stand wiederholt (neun von neun richtig).
