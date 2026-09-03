---
tags: [typ/story, status/gegrillt, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Einkleidung unsichtbar, stale Interessen-Tag, Sackgasse beim Speichern]
status: gegrillt
prio: P2
art: Defekt
wo: beides
quelle: docs/pm-sitzung-2026-09-02.md (frontend-reviewer zu B-46)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-206 · Die Einkleidung einer Übung ist im Vater-Web unsichtbar — und ein gelöschtes Stichwort macht das Speichern unmöglich

B-46 hat der Übung ein Feld gegeben, das **nur der KI-Creator** setzt und **niemand** ansieht. Das Feld
reist beim Speichern korrekt durch (das war der Frontend-Anteil von B-46), aber es hat keine Anzeige und
keinen Korrekturweg. Daraus folgt eine Sackgasse, deren Teile alle schon existieren.

## User Story

Als *Creator* möchte ich am Bearbeiten-Dialog einer Übung sehen, in welche Interessen sie eingekleidet
ist — und ein falsches Stichwort entfernen können —, damit ich nachvollziehe, für wen die Übung gemacht
ist, und damit ein Speichern nicht an etwas scheitert, das ich nicht sehe.

## Ist-Stand am Code

Belegt vom `frontend-reviewer` im Nachtlauf 2026-09-02, alle Teile am Code nachgewiesen:

1. **Kein Anzeige- und kein Korrekturweg.** `frontend/src/vater/ExerciseEditModal.tsx:117-119` reicht
   `detail.interestTagIds` durch, ohne es zu rendern. Der `MetaEditor` hat kein Feld dafür.
2. **Das Löschen eines Stichworts ist bedingungslos.** `InterestTagsController.cs` (`Delete`) hat keinen
   Verwendungs-Riegel — anders als das Löschen einer Übung, das eine benutzte Übung mit `exercise_in_use`
   ablehnt. Die Links fallen per Cascade (`ExerciseInterestLink`, beide FKs `Cascade`).
3. **Die Sackgasse:** Vater öffnet den Bearbeiten-Dialog einer eingekleideten Übung → anderswo wird das
   Stichwort gelöscht → „Speichern" schickt die veraltete Id mit → `400 validation_error` „Unknown
   interest tag id(s): 7." Der Banner nennt eine Größe, die im Formular **nicht vorkommt**; es gibt kein
   Feld, um sie zu leeren. Der einzige Ausweg ist Dialog schließen und neu öffnen, und darauf kommt
   niemand.
4. **Die Zeile in B-46s User Story verlangt die Anzeige ausdrücklich** („*und am Katalog erkennen können,
   welche Interessen sie einkleiden*"). B-46s Entscheidung 3 schneidet die **Passung** ab (Filter,
   Sortierung, Bewertung → [B-18](B-18-auto-lehrplan-generator.md)), **nicht** die Anzeige. Die
   Akzeptanzkriterien von B-46 verlangen nur die API (AK 6 nennt ausdrücklich die `ExerciseResponse`),
   deshalb ist B-46 vollständig — aber die Lücke bleibt.

## Offene Punkte

1. ~~Darf ein Stichwort gelöscht werden, das Übungen einkleidet?~~ → Entscheidung 1.
2. ~~Reicht eine nur lesbare Anzeige, oder braucht es das Entfernen?~~ → Entscheidung 2.
3. ~~Gehört ein Editor auch ins Anlege-Formular?~~ → Entscheidung 4.

Der Wortlaut der drei Punkte samt der damals abgewogenen Möglichkeiten steht im `## Verlauf`-Eintrag vom
2026-09-02 und in der Grill-Runde unten; er ist hier gekürzt, weil die Abwägungen jetzt als Kosten an den
Entscheidungen hängen.

## Entscheidungen

1. **Ein Stichwort, das Übungen einkleidet, lässt sich nicht löschen — `DELETE` antwortet 409.**
   Analog zu `exercise_in_use` ([ApiErrors.cs:135](../../backend/Pugling.Api/Errors/ApiErrors.cs#L135),
   genutzt in [ExerciseControllers.cs:270](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs#L270)).
   *Begründung:* Die Einkleidung ist **Autoren-Information** und nicht rekonstruierbar — anders als
   Bild-Verknüpfungen und Kind-Interessen, deren Verlust nur Auswahlqualität kostet. Genau diese
   Asymmetrie steht seit B-46 als offene Frage im Kommentar am `DELETE` von
   [InterestTagsController.cs](../../backend/Pugling.Api/Controllers/Creator/InterestTagsController.cs);
   sie wird hiermit beantwortet, und der Kommentar wird entsprechend umgeschrieben.
   *Kosten:* ein neuer `ApiErrors`-Code (additiv), und Aufräumen braucht zwei Schritte — erst aus den
   Übungen nehmen, dann löschen. Tragbar, weil die Aufräum-Sicht `?unused=true` seit dem B-46-Review
   ohnehin nur Stichworte anbietet, an denen **gar nichts** hängt; ein Riegel trifft dort also nie.
   *Abhängigkeit:* Der Riegel wäre für sich genommen eine neue Sackgasse — er ist nur zusammen mit
   Entscheidung 2 tragfähig, die den Ausweg baut.

2. **Der Bearbeiten-Dialog zeigt die Einkleidung als Chips mit Namen und lässt sie ändern — entfernen
   *und* hinzufügen.** Gefüllt aus der bestehenden Taxonomie über das vorhandene
   `api.interestTags(search)`; Muster ist der Interessen-Editor in
   [VaterKind.tsx](../../frontend/src/vater/VaterKind.tsx).
   *Begründung:* Nur-Entfernen wäre eine Einbahnstraße — ein versehentlich entferntes Stichwort ließe sich
   nur noch über den KI-Creator zurückholen, und mit dem Riegel aus Entscheidung 1 muss man zum Aufräumen
   gezielt strippen können, also auch korrigieren können.
   *Kosten:* ein Auswahlfeld mehr im Dialog, und „welches Stichwort passt hier" wird zur Handarbeit des
   Vaters. Das ist **keine** Wiederaufnahme der Passungs-Frage, die B-46s Entscheidung 3 an
   [B-18](B-18-auto-lehrplan-generator.md) abgegeben hat: dort geht es um Filtern, Sortieren und Bewerten
   durch die Maschine, hier um Autorenschaft von Hand.

3. **Der Riegel wird dort sichtbar, wo gelöscht wird — die Schlagwort-Verwaltung nennt die Übungen.**
   [InterestTagAdmin.tsx](../../frontend/src/vater/InterestTagAdmin.tsx) trägt heute die Spalten „Bilder"
   und „Kinder" und fragt vor dem Löschen mit genau diesen zwei Zahlen; `InterestTagResponse` liefert seit
   B-46 aber **drei**. Die dritte Spalte und die dritte Zahl in der Rückfrage kommen dazu, und ein
   abgewiesenes Löschen nennt seinen Grund.
   *Begründung:* Ohne das verspräche der Bestätigungsdialog ab Entscheidung 1 eine Handlung, die der Server
   verweigert — dieselbe Klasse wie
   [B-150](B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md) und
   [B-160](B-160-gesperrter-knopf-nennt-den-grund-nie.md). Es ist damit **Folge** des Riegels, kein
   Nachbarfund, und gehört in diese Story statt in eine eigene.
   *Kosten:* eine Spalte und ein Satz mehr in einer Fläche, die diese Story sonst nicht anfassen würde.

4. **Das Anlege-Formular (`/vater/exercises/neu`) bekommt vorerst keinen Einkleidungs-Editor —
   zurückgestellt.**
   *Begründung:* Der Defekt ist eine Sackgasse **bestehender** Daten; beim Anlegen gibt es nichts zu
   verlieren. Das Anlege-Formular ist zudem das größte im Vater-Web, und die Einkleidung setzt heute der
   KI-Creator.
   *Kosten:* eine Asymmetrie, die man erklären muss („warum steht das hier nicht?"), und ein zweiter
   Handgriff für den, der sofort einkleiden will — der Dialog ist einen Klick entfernt. Nachrüsten bleibt
   eine kleine eigene Story, falls das Feedback es verlangt.

## Akzeptanzkriterien

Bewusst als **Ergebnisse** formuliert, nicht als Mechanismen. AK 3 hing beim Ausformulieren am offenen
Punkt 1; seit Entscheidung 1 und 2 steht der Weg dorthin fest, das Kriterium selbst blieb unverändert.

1. Der Bearbeiten-Dialog einer Übung **zeigt** ihre Einkleidung, und zwar mit den **Namen** der
   Stichworte, nicht mit Ids.
2. Eine Übung ohne Einkleidung sagt das ausdrücklich, statt an dieser Stelle leer zu bleiben — sonst sieht
   „nicht eingekleidet" aus wie „Feld gibt es nicht" (dieselbe Verwechslung wie in B-174).
3. **Ein Speichern scheitert nicht an etwas, das der Vater nicht sehen und nicht ändern kann.** Wird ein
   Stichwort anderswo entfernt, ist der Zustand ohne Schließen und Neuöffnen des Dialogs auflösbar.
4. Keine Fehlermeldung im Dialog nennt eine Größe, die das Formular nicht anzeigt.
5. Regression: die Zusicherung aus B-46, dass `payloadFrom` die Einkleidung **durchreicht**, bleibt grün —
   die Anzeige ersetzt sie nicht, sie ergänzt sie.
6. Ein Löschversuch an einem Stichwort, das Übungen einkleidet, wird abgewiesen **und sagt warum**; die
   Schlagwort-Verwaltung zeigt die Übungszahl schon vorher, statt sie erst im Fehlerfall zu nennen
   (Entscheidung 3).

## Anhaltspunkte für die Umsetzung

Der Lesepfad steht: `api.interestTags()` und der Editor-Aufbau liegen als Muster in
`frontend/src/vater/VaterKind.tsx` (Interessen des Kindes, `useAsync` + `useAction`), die
Chip-Darstellung samt Entfernen in der Vokabel-Tag-Fläche. Der Server braucht **nichts** Neues für die
Anzeige — `ExerciseDetail.interestTagIds` und `GET creator/interest-tags` genügen.

## Verlauf

- **2026-09-02** — angelegt aus dem `frontend-reviewer`-Befund zu B-46 (Nachtlauf, Sprint 1). Nicht in
  jenen Sprint gezogen: die Anzeige ist ein eigenes Vorhaben, und der Riegel am Löschen ist eine
  Produktentscheidung, die ein unbeaufsichtigter Lauf nicht treffen darf. Der Ist-Stand ist vollständig am
  Code belegt und stammt nicht aus einer Vermutung — die Stufe `idee` ist damit fast schon
  `ausformuliert`, es fehlen nur die Entscheidungen zu den drei offenen Punkten.
- **2026-09-03** — auf `ausformuliert` gehoben, und zwar weil der **Index-Wächter** die Datei mit
  `idee` + `unverifiziert: false` als widersprüchlich gemeldet hat. Die beiden Auswege waren, das Flag auf
  `true` zu setzen (das wäre falsch: der Ist-Stand steht mit `Datei:Zeile` da und ist vom
  `frontend-reviewer` am Code gemessen) oder die Stufe zu nehmen, die der Inhalt schon trägt. Also die
  Stufe. Was noch fehlt, ist ausschließlich die Grill-Runde über die drei offenen Punkte — und deren
  erster ist eine Produktentscheidung, die ein unbeaufsichtigter Lauf nicht treffen darf.
- **2026-09-03** — gegrillt, im Dialog mit dem Nutzer (drei Fragen, drei Antworten). Alle drei offenen
  Punkte sind nummerierte Entscheidungen; dazu eine vierte, die sich aus der ersten **ergibt** statt
  gefragt zu werden. Zwei Befunde aus der Vorab-Recherche haben die Runde verschoben und stehen darum hier:
  Die „Warnung mit Zahl", die der Ist-Stand als Alternative anbietet, **existiert bereits**
  ([InterestTagAdmin.tsx:69](../../frontend/src/vater/InterestTagAdmin.tsx#L69)) — sie nennt nur zwei der
  drei Zahlen, obwohl `InterestTagResponse` seit B-46 alle drei liefert. Und der Riegel aus Entscheidung 1
  macht genau diesen Bestätigungsdialog zu einem, der etwas verspricht, was der Server verweigert; daraus
  wurde Entscheidung 3, ohne dass sie als offener Punkt notiert war. Die Akzeptanzkriterien sind final
  (AK 6 kam hinzu).
