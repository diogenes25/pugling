---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Stufe im Bearbeiten, wartender Aufstieg sichtbar]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Review von B-849 und B-841 im Nachtlauf 2026-11-23-e (docs/pm-sitzung-2026-11-23-e.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-882 · Die Stufe einer bestehenden Position ändern und einen wartenden Aufstieg sehen

## Idee

Zwei Lücken an derselben Stelle des Vater-Webs, beide aus Reviews des Aufbau-Wegs:

1. Das Bearbeiten-Formular einer Position kann die Stufe nicht ändern, obwohl `UpdatePositionDto.currentBuildLevel`
   es kann (B-848: mit Prüfung gegen offene Runden, `build_level_in_play`). Die Norm „jeder Endpunkt braucht ein UI“
   ist damit nur halb erfüllt; heute geht es nur über die API.
2. Seit B-849 kann ein Aufstieg auf den nächsten Lerntag warten (`PlanPosition.BuildLevelEffectiveOn`). Die Zeile zeigt
   die heute gespielte Stufe; dass morgen eine höhere gilt, sieht der Vater nirgends, und `PositionResponse` trägt das
   Datum nicht. Der Vater kann einen wartenden Aufstieg auch nicht „sofort“ setzen: dieselbe Stufe zu senden ist keine
   Änderung.

## Zu entscheiden (im Dialog, `Wunsch`)

- Soll der Vater einen wartenden Aufstieg sehen („ab morgen Stufe 2“), und soll er ihn vorziehen können?
- Darf die Stufe im Bearbeiten-Formular auch auf „ohne Aufbau-Weg“ zurück (heute kennt der Server kein Zurücksetzen auf
  `null`, siehe B-702 Verlauf vom 2026-11-23)?

## Verlauf

- **2026-11-23** — Aus den Reviews von B-849 und B-841 aufgenommen (Nachtlauf E); bewusst nicht im Sprint gebaut, weil
  beide Punkte eine Entscheidung brauchen, die B-704 nicht hergibt.
- **2026-11-24** — Hinweis aus dem Review von B-702 (Sprint 126): `BuildPath.Since` ist seit B-702 im Vertrag (der Tag, ab
  dem die gespielte Stufe gilt; `null`, solange ein Aufstieg wartet). Wer die Zeit auf der Stufe zeigt, entscheidet mit:
  Setzt der Vater bei wartendem Aufstieg die Stufe zurück auf die, die das Kind gerade spielt, beginnt `Since` heute –
  der Beginn der Vorstufe ist beim Aufstieg schon überschrieben worden.
