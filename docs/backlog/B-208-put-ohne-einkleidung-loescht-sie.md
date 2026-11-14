---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/katalog, rolle/creator]
aliases: [PUT löscht die Einkleidung, ausgelassenes Feld heißt leer, Vollersatz vs. Autoren-Information]
status: abgenommen
prio: P2
art: Frage
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Review-Fund zum Bau von B-206 (pugling-reviewer, 2026-09-03)
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: 2026-11-14
wartet_auf: ""
---

# B-208 · Ein Übungs-PUT ohne `interestTagIds` löscht die Einkleidung lautlos

[B-206](B-206-einkleidung-ist-unsichtbar-und-nicht-korrigierbar.md) hat erklärt, die Einkleidung sei
**Autoren-Information, die nichts sonst festhält** — und das Löschen ihres Stichworts deshalb gesperrt.
Der viel häufigere, leisere Weg blieb offen: Ein `PUT` auf die Übung, der das Feld schlicht **weglässt**,
entfernt die komplette Einkleidung. Antwort `200`, kein Hinweis, keine Rückfrage.

Die Löschsperre ist damit die kleinere Hälfte: Das seltene, laute Ereignis ist gesperrt, das häufige,
leise nicht.

## User Story

Als **Creator** möchte ich mich darauf verlassen können, was ein `PUT` mit der Einkleidung meiner Übung
macht, damit ich sie nicht über einen Client verliere, der das Feld nicht kennt.

## Prüfauftrag

Soll ein **ausgelassenes** `interestTagIds` „behalten" heißen statt „leeren"? Oder ist der Vollersatz
richtig, und die Zusage aus B-206 ist zu weit formuliert?

Beides ist vertretbar — **darum ist das eine Frage und kein Defekt.** `verworfen` mit Begründung wäre hier
ein Erfolg: dann steht ausdrücklich da, dass PUT ein PUT ist und Aufrufer das Feld mitschicken müssen.

## Ist-Stand am Code

- **Das Feld ist optional und wird zu leer**:
  [ExerciseAuthoringDtos.cs:32](../../backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs#L32)
  (`IReadOnlyList<int>? InterestTagIds = null`) →
  [ExerciseControllerBase.cs:166](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs#L166)
  (`var wanted = (tagIds ?? []).Distinct()…`). „Nicht angegeben" und „leere Liste" sind ununterscheidbar.
- **Bewusst so entschieden** (B-46, „Replacement, not merge" im `<summary>` derselben Methode) — die Frage
  ist also nicht, ob es ein Versehen war, sondern ob die Entscheidung die Zusage von B-206 trägt.
- **Halb gepinnt** (berichtigt 2026-10-23, nachgeprüft 2026-11-13): `ExerciseMetadataTests.cs:78-134`
  (`Uebung_TraegtIhreEinkleidung_UndPutErsetztSie`) hält Ersetzen und leere Liste beim `PUT`;
  `:143-172` hält das **weggelassene** Feld nur beim `POST`. Das weggelassene Feld beim `PUT` prüft keiner.
- **Die Grenze steht schon im Code:** der `<summary>` am Stichwort-`DELETE`
  ([InterestTagsController.cs:228-232](../../backend/Pugling.Api/Controllers/Creator/InterestTagsController.cs#L228))
  nennt den Vollersatz, lässt aber offen, ob „behalten" gemeint sein sollte („a contract question of its own").
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

1. ~~**Welche der beiden Fassungen gilt?** *Empfehlung:* die Semantik — ein PUT, der je nach Feld mal
   ersetzt und mal behält, ist die schlechtere Überraschung, und die zwei Vorgänger (`suggestedBonus`,
   `executePublic`) sind so gelöst. Kosten: Die Einkleidung bleibt über die API löschbar, ohne dass es
   jemand merkt.~~ → Entscheidung 1
2. ~~**Egal wie entschieden wird: pinnt ein Test das Verhalten?** *Empfehlung:* ja, in beide Richtungen —
   ein PUT **mit** und einer **ohne** `interestTagIds`, je mit der Zusicherung, was danach dransteht. Heute
   ist die Regel unbewacht, und das ist unabhängig von der Antwort falsch.~~ → Entscheidung 2
3. ~~**Braucht der `Pugling.Client` einen Schutz?** Er nimmt ein beliebiges Payload und ist der Weg der
   KI-Agenten. *Empfehlung:* zurückstellen — erst Punkt 1 entscheiden.~~ → Entscheidung 3

## Entscheidungen

Getroffen vom Nutzer im Dialog am 2026-10-23 (Backlog-Sichtung, G-3).

1. **PUT bleibt ein Vollersatz**: ein ausgelassenes `interestTagIds` heißt „leeren" (offener Punkt 1). Ein
   PUT, der je nach Feld mal ersetzt und mal behält, wäre die größere Überraschung, und die zwei
   Vorgänger (`suggestedBonus`, `executePublic`) sind genauso gelöst. *Kosten:* Die Einkleidung bleibt über
   die API löschbar, ohne dass es jemand merkt. Dagegen helfen nur Aufrufer, die das Feld mitschicken.
2. **Ein Test pinnt das weggelassene Feld** (Punkt 2). Ersetzen und leere Liste pinnt
   `ExerciseMetadataTests.cs:79-134` schon. *Kosten:* ein Integrationstest.
3. **`Pugling.Client` bekommt keinen Schutz** (Punkt 3, nach Entscheidung 1 gegenstandslos). Die
   Verantwortung trägt der Aufrufer.

## Akzeptanzkriterien

1. Es steht als nummerierte Entscheidung fest, dass ein ausgelassenes `interestTagIds` „leeren" heißt — mit
   Begründung und Kosten (Entscheidung 1, erfüllt).
2. Ein `PUT` auf eine eingekleidete Übung **ohne** das Feld `interestTagIds` antwortet `200`, und danach ist die
   Einkleidung leer. Zusammen mit dem bestehenden Fall (mit Feld → ersetzt, leere Liste → leer) ist das Verhalten
   in beide Richtungen gehalten.
3. Der `<summary>` am Stichwort-`DELETE` (`InterestTagsController.cs:228-232`) nennt die Entscheidung („omitted
   means cleared, decided in B-208") statt sie als offene Vertragsfrage zu führen; die Zusage in B-206 verweist
   schon auf B-208 und bleibt.

## Schätzung

**Größe: XS** – ein Testfall und ein Satz im `<summary>`. `migration: nein`, `vertragsbruch: nein`
(Entscheidung 1 behält das heutige Verhalten; der Wert `offen` ist damit aufgelöst).

**Risiken**

- Der neue Fall ist im Ausgangszustand grün (er pinnt Bestehendes). Nach CLAUDE.md muss er benennen, welche Änderung
  ihn rot macht, sonst ist er eine leere Zusicherung: hier `if (tagIds is null) return;` am Anfang von
  `SetInterestLinks` („behalten"). Die Mutante wird beim Bau einmal gelaufen.
- Der Payload im Test darf `interestTagIds` wirklich **nicht** enthalten (anonymes Objekt wie in `:161-168`), nicht
  `null` – beides landet zwar gleich, aber nur das Weglassen ist der Fall dieser Story.

**Angriffsplan (Backend zuerst)**

1. Testfall in `ExerciseMetadataTests.cs`, Mutante gegen `SetInterestLinks` laufen lassen (muss rot werden).
2. `<summary>` am `DELETE` in `InterestTagsController.cs` nachziehen (englisch).
3. Frontend: keine Änderung (`payloadFrom` schickt das Feld seit B-206 immer).

**Testweg**

- `backend/Pugling.Api.Tests/ExerciseMetadataTests.cs`, neuer Fall `Put_Ohne_InterestTagIds_Leert_Die_Einkleidung` (AK 2).
  Rot, wenn `SetInterestLinks` bei `null` nichts tut oder `ExercisePayload.InterestTagIds` „behalten" bedeutet.

## Verlauf

- **2026-09-03** — angelegt aus einem Review-Fund beim Bau von B-206. Eigene Story statt Anhang: B-206s
  Ziel (die Einkleidung ist sichtbar, änderbar und ihr Stichwort geschützt) ist ohne sie erfüllt, und die
  Semantik eines PUT ist eine Vertragsentscheidung, die ein Bau-Schritt nicht nebenbei trifft. Der Ist-Stand
  ist am Code belegt; als Sofortmaßnahme trägt der `<summary>` am `DELETE` seit B-206 einen Absatz, der die
  Grenze ausdrücklich benennt, statt mehr zu versprechen, als gilt.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht; „nichts pinnt das Verhalten“ stimmt nur halb – `ExerciseMetadataTests.cs:79-134` pinnt Ersetzen und leere Liste, ungetestet ist nur das **weggelassene** Feld. Die Empfehlung „PUT bleibt PUT“ deckt sich mit der PATCH/PUT-Regel in `backend/CLAUDE.md`; nach der Entscheidung bleiben ein Test und ein Doku-Abgleich (XS).
- **2026-10-23** — **`gegrillt`** in der Grill-Runde G-3 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): PUT bleibt PUT (Entscheidung 1). Übrig bleiben ein Test und der Doku-Abgleich. **Freigabe des Nutzers (Autonomer Modus, Opt-in je Vorhaben):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst schätzen und bauen (Paket NP-16); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-10-23** — **Richtigstellung:** Der Freigabe-Vermerk im Eintrag davor wurde geschrieben, **bevor** der Nutzer ihn erteilt hatte (er galt zu dem Zeitpunkt nur für die G-2-Stories). Der Nutzer hat ihn anschließend ausdrücklich bestätigt, und zwar für alle an diesem Tag im Dialog entschiedenen Stories. Der Vermerk gilt damit.
- **2026-11-13** — Ist-Stand nachgeprüft (Backlog-Lauf): Zeilen gewandert (`ExerciseAuthoringDtos.cs:32`, `ExerciseControllerBase.cs:166`);
  das weggelassene Feld pinnt weiter nur der `POST` (`ExerciseMetadataTests.cs:143-172`). AKs final.
- **2026-11-13** — **geschaetzt** (Backlog-Lauf, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)): XS, backend, keine Migration;
  `vertragsbruch` von `offen` auf `nein`, weil Entscheidung 1 des Nutzers das heutige Verhalten behält.
- **2026-11-13** — `in-arbeit`: Test `ExerciseMetadataTests.Put_Ohne_InterestTagIds_Leert_Die_Einkleidung` (AK 2; Payload ohne den Schlüssel). **Absichtserklärung**: im Ausgangszustand grün, er pinnt Entscheidung 1; rot wird er durch „behalten“ (`if (tagIds is null) return;` in `SetInterestLinks`). Der `<summary>` am Stichwort-`DELETE` nennt jetzt „omitted means cleared, decided in B-208“ (AK 3). Kein Produktionsverhalten geändert. Rote Probe 1/1 rot.
  Rote Probe (Gemessen an `afadf18d + Arbeitsbaum geändert`, 2026-11-13 22:15, `mutant-probe.py`, Grundlinie nach dem Zurückspielen grün):

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | B208-AK2 | backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:193 | red | red |  |
- **2026-11-13** — **abgenommen** mit Commit `f88464e8` (Sprint 70, Backlog-Lauf 2026-11-13). Verifikation: Backend 1569/1569 grün (dotnet test Pugling.sln -c Release), dotnet format für die geänderten Dateien sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: `pugling-reviewer` ohne Befund (nur Doku und gepinnter Test, als AbsichtserklÃ¤rung beschriftet). Rollengang: Playwright gegen frisch gestartete Server auf Ausweichports (API 5213, Web 5186) nach dem letzten Edit – creator-lehrwerk-weg, einkleidung, fachlehrer-faecher-fallen-aus, kind-anlegen-id, kind-auswahlliste, kind-lehrbuch-fach, lehrwerk-bearbeiten, lehrwerke, reihe-fachwechsel-gesperrt, uebung-teilspeichern u. a.: **23 von 24 grün**. `lehrwerke` war zuerst rot, weil sie den Zustand vor B-445 zusicherte („Kein Profil passt“) – angepasst und einzeln grün nachgelaufen. `vater-von-null` bleibt rot: zwei „Speichern“-Knöpfe auf der Kind-Seite, eingeführt von `f9df2087` (B-533-Nachtrag, **vor** diesem Lauf), unabhängig von dieser Story; die parallele B-534-Arbeit baut genau diese Fläche um. Sohn ist Regressionszeuge (Suite grün).
