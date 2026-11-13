---
tags: [typ/story, status/ausformuliert, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Familien-Liga, Wochenvergleich]
status: ausformuliert
prio: P3
art: Frage
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/knowunity-vergleich-plan.md#wo-knowunity-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-253 · Prüfauftrag: ein Wochenvergleich gegen sich selbst oder Geschwister statt einer Liga?

Knowunity motiviert über eine Wochen-Liga mit bis zu 30 Mitschülern und Rängen von Bronze bis Gold. Eine
Liga mit Fremden ist für ein Kind um 11 Jahre ohne Moderation nicht vertretbar und passt nicht zur
Familien-App. Zu prüfen ist eine eingehegte Form: das Kind tritt gegen die eigene Vorwoche an („diese
Woche 340 Münzen, letzte Woche 290"), oder Geschwister unter denselben Supervisoren sehen einen
gemeinsamen Wochenstand. Dieselbe Druck-Frage wie in [B-222](B-222-wochen-streak-als-default.md): erzeugt
der Vergleich Lerneffekt oder nur Druck? Ränge selbst liegen bei
[B-221](B-221-level-leiter-fuer-auszeichnungen.md). Eine belegte Antwort „nicht bauen" ist ein
zulässiges Ergebnis.

## Ist-Stand am Code (2026-11-13)

- Es gibt keinen Wochenvergleich: kein `Week`-Feld in `Pugling.Contracts/Student/`, kein „Vorwoche" in API oder Arcade
  (`grep` über Api, Contracts, `frontend/src`).
- Was es gibt: `GET student/me/points` liefert das Guthaben (`WalletResponse`, `MeController.cs:29`) und
  `GET student/me/points/entries` die Buchungen seitenweise (`:45`, `skip`/`take`) – ohne Zeitraum-Filter. Die Arcade
  müsste die Wochensumme also selbst aus allen Seiten addieren; das gehört nach API-First auf den Server.

## Offene Punkte (die Entscheidung G-6 steht, gefragt ist nur der Schnitt)

1. Was zählt als „verdient": nur Münzen aus dem Lernen (`PointKind` mit Münz-Währung, ohne Shop-Abbuchung, Malus und
   Verschenken) – sonst sinkt die Woche, weil das Kind etwas gekauft hat.
2. Woche: Montag–Sonntag in UTC wie die übrige Tageslogik (Fallstrick „Zeit/UTC" im Root-`CLAUDE.md`).
3. Ort: eine Zeile auf der Startseite der Arcade oder im Fortschritt (`SohnProgress`)?

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 6 des Übernahmeplans).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (G-6): das Kind vergleicht sich **nur mit der eigenen Vorwoche** („diese Woche 340 Münzen, letzte Woche 290“). Es gibt keinen Vergleich mit Geschwistern und keine Ränge. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-21); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): Ist-Stand belegt, drei Schnitt-Fragen; die Entscheidung G-6 des Nutzers bleibt die Grundlage.
