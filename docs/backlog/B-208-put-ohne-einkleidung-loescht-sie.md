---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/katalog, rolle/creator]
aliases: [PUT löscht die Einkleidung, ausgelassenes Feld heißt leer, Vollersatz vs. Autoren-Information]
status: ausformuliert
prio: P2
art: Frage
groesse:
wo: backend
migration: nein
vertragsbruch: offen
quelle: Review-Fund zum Bau von B-206 (pugling-reviewer, 2026-09-03)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-208 · Ein Übungs-PUT ohne `interestTagIds` löscht die Einkleidung lautlos

[B-206](B-206-einkleidung-ist-unsichtbar-und-nicht-korrigierbar.md) hat erklärt, die Einkleidung sei
**Autoren-Information, die nichts sonst festhält** — und das Löschen ihres Stichworts deshalb gesperrt.
Der viel häufigere, leisere Weg blieb offen: Ein `PUT` auf die Übung, der das Feld schlicht **weglässt**,
entfernt die komplette Einkleidung. Antwort `200`, kein Hinweis, keine Rückfrage.

Die Löschsperre ist damit die kleinere Hälfte: Das seltene, laute Ereignis ist gesperrt, das häufige,
leise nicht.

## Prüfauftrag

Soll ein **ausgelassenes** `interestTagIds` „behalten" heißen statt „leeren"? Oder ist der Vollersatz
richtig, und die Zusage aus B-206 ist zu weit formuliert?

Beides ist vertretbar — **darum ist das eine Frage und kein Defekt.** `verworfen` mit Begründung wäre hier
ein Erfolg: dann steht ausdrücklich da, dass PUT ein PUT ist und Aufrufer das Feld mitschicken müssen.

## Ist-Stand am Code

- **Das Feld ist optional und wird zu leer**:
  [ExerciseAuthoringDtos.cs:30](../../backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs#L30)
  (`IReadOnlyList<int>? InterestTagIds = null`) →
  [ExerciseControllerBase.cs:140](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs#L140)
  (`var wanted = (tagIds ?? []).Distinct()…`). „Nicht angegeben" und „leere Liste" sind ununterscheidbar.
- **Bewusst so entschieden** (B-46, „Replacement, not merge" im `<summary>` derselben Methode) — die Frage
  ist also nicht, ob es ein Versehen war, sondern ob die Entscheidung die Zusage von B-206 trägt.
- **Nichts pinnt das Verhalten.** Es gibt keinen Test in eine der beiden Richtungen; die Regel könnte
  kippen, ohne dass ein Tor es merkt.
- **B-206 hat nur den einen Frontend-Aufrufer geflickt**
  ([ExerciseEditModal.tsx](../../frontend/src/vater/ExerciseEditModal.tsx), `payloadFrom`). Der
  `Pugling.Client` nimmt weiter ein beliebiges Payload
  ([CreatorApi.cs](../../backend/Pugling.Client/CreatorApi.cs)), ebenso jeder `.http`-Aufruf und jeder
  KI-Agent.
- **Dieselbe Falle traf schon zweimal andere Felder**: `suggestedBonus` und `executePublic` — beide gelöst,
  indem der *Aufrufer* sie mitschickt, nicht indem der Server „behalten" annimmt. Das ist ein Argument
  **für** den Vollersatz.

## Die echte Lücke

Nicht die Semantik, sondern der **Widerspruch zwischen ihr und der Zusage**: Der `<summary>` am
Stichwort-`DELETE` liest sich seit B-206 so, als sei die Einkleidung geschützt. Beim Bau von B-206 wurde
darum ein Absatz ergänzt, der die zwei Grenzen ausdrücklich benennt — das ist Schadensbegrenzung, keine
Entscheidung. Genau eine der beiden Fassungen muss gelten:

- **Die Semantik gewinnt**: PUT ersetzt, der Aufrufer trägt die Verantwortung. Dann ist hier nichts zu
  bauen, und die Frage wird `verworfen` — aber die Zusagen in B-206 und im Code bleiben so eng formuliert,
  wie sie es jetzt sind.
- **Die Zusage gewinnt**: Ein ausgelassenes Feld heißt „behalten". Dann braucht es einen ausdrücklichen
  Schalter zum Leeren (Muster: `UpdateChildDto.ClearBirthYear`, PATCH-Semantik der `CLAUDE.md`) — und das
  ist ein Vertragsbruch für jeden bestehenden Aufrufer, der heute mit dem Weglassen leert.

## Offene Punkte

1. **Welche der beiden Fassungen gilt?** *Empfehlung:* die Semantik — ein PUT, der je nach Feld mal
   ersetzt und mal behält, ist die schlechtere Überraschung, und die zwei Vorgänger (`suggestedBonus`,
   `executePublic`) sind so gelöst. Kosten: Die Einkleidung bleibt über die API löschbar, ohne dass es
   jemand merkt.
2. **Egal wie entschieden wird: pinnt ein Test das Verhalten?** *Empfehlung:* ja, in beide Richtungen —
   ein PUT **mit** und einer **ohne** `interestTagIds`, je mit der Zusicherung, was danach dransteht. Heute
   ist die Regel unbewacht, und das ist unabhängig von der Antwort falsch.
3. **Braucht der `Pugling.Client` einen Schutz?** Er nimmt ein beliebiges Payload und ist der Weg der
   KI-Agenten. *Empfehlung:* zurückstellen — erst Punkt 1 entscheiden.

## Akzeptanzkriterien (Entwurf)

1. Es steht als nummerierte Entscheidung fest, ob ein ausgelassenes `interestTagIds` „behalten" oder
   „leeren" heißt — mit Begründung und Kosten.
2. Ein Test hält das entschiedene Verhalten in **beide** Richtungen fest.
3. Der `<summary>` am Stichwort-`DELETE` und die Zusage in B-206 sagen danach dasselbe wie der Code.

## Verlauf

- **2026-09-03** — angelegt aus einem Review-Fund beim Bau von B-206. Eigene Story statt Anhang: B-206s
  Ziel (die Einkleidung ist sichtbar, änderbar und ihr Stichwort geschützt) ist ohne sie erfüllt, und die
  Semantik eines PUT ist eine Vertragsentscheidung, die ein Bau-Schritt nicht nebenbei trifft. Der Ist-Stand
  ist am Code belegt; als Sofortmaßnahme trägt der `<summary>` am `DELETE` seit B-206 einen Absatz, der die
  Grenze ausdrücklich benennt, statt mehr zu versprechen, als gilt.
