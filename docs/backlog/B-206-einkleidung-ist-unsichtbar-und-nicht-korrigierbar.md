---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Einkleidung unsichtbar, stale Interessen-Tag, Sackgasse beim Speichern]
status: ausformuliert
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

1. **Darf ein Stichwort gelöscht werden, das Übungen einkleidet?** Das ist eine Produktentscheidung und
   wurde im Nachtlauf **nicht** getroffen (Freigabe 1 sperrt genau das). Drei Möglichkeiten, jede mit
   Kosten:
   - **Riegel wie bei der Übung** (`409`, „wird von N Übungen getragen"): schützt die Autoren-Information,
     macht aber das Aufräumen der Taxonomie mühsamer — und die Aufräum-Sicht `?unused=true` ist genau
     dafür gebaut.
   - **Löschen erlaubt, aber mit Warnung/Zahl vor dem Bestätigen**: billiger, verlässt sich auf den
     Menschen.
   - **Bleibt bedingungslos**: dann muss die Sackgasse anders zu (siehe Punkt 2) — und die Begründung am
     `DELETE` gehört umgeschrieben, denn „sein Verlust kostet nur Auswahlqualität" ist seit B-46 falsch.
   Empfehlung: **Riegel**, weil die Einkleidung Autoren-Information ist und nicht rekonstruierbar; die
   Aufräum-Sicht zeigt seit dem B-46-Review ohnehin nur wirklich unbenutzte Stichworte.
2. **Reicht eine nur lesbare Anzeige, oder braucht es das Entfernen?** Nur lesbar erfüllt die Zeile der
   User Story und ist billig; erst das Entfernen schließt die Sackgasse auch ohne Punkt 1.
   Empfehlung: **beides**, weil eine Anzeige ohne Korrekturweg denselben Vorwurf einlädt („ich sehe es,
   kann es aber nicht ändern").
3. Gehört ein Editor auch ins Anlege-Formular (`VaterExerciseCreate.tsx`), oder genügt der
   Bearbeiten-Dialog? Empfehlung: erst der Dialog — beim Anlegen gibt es nichts zu verlieren.

## Akzeptanzkriterien

Bewusst als **Ergebnisse** formuliert, nicht als Mechanismen: welcher Weg AK 3 erfüllt, hängt an offenem
Punkt 1 — das Kriterium selbst hängt es nicht.

1. Der Bearbeiten-Dialog einer Übung **zeigt** ihre Einkleidung, und zwar mit den **Namen** der
   Stichworte, nicht mit Ids.
2. Eine Übung ohne Einkleidung sagt das ausdrücklich, statt an dieser Stelle leer zu bleiben — sonst sieht
   „nicht eingekleidet" aus wie „Feld gibt es nicht" (dieselbe Verwechslung wie in B-174).
3. **Ein Speichern scheitert nicht an etwas, das der Vater nicht sehen und nicht ändern kann.** Wird ein
   Stichwort anderswo entfernt, ist der Zustand ohne Schließen und Neuöffnen des Dialogs auflösbar.
4. Keine Fehlermeldung im Dialog nennt eine Größe, die das Formular nicht anzeigt.
5. Regression: die Zusicherung aus B-46, dass `payloadFrom` die Einkleidung **durchreicht**, bleibt grün —
   die Anzeige ersetzt sie nicht, sie ergänzt sie.

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
