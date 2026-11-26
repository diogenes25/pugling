---
tags: [typ/story, status/abgenommen, bereich/lehrplan, rolle/student]
aliases: []
status: abgenommen
prio: P1
art: Defekt
groesse: XS
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-25-o.md#sprint-191--ziel--umfang
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-1021 · Der verschonte Einführungstag (B-432) kann auf den freien Sonntag oder in eine Pause fallen – der Montag danach kostet Malus

Fund des `pugling-reviewer` im Sprint 191 (B-731), nicht mehr behoben (Fehlerzähler). `IsIntroductionDaySparedAsync`
(`backend/Pugling.Api/Services/Shared/PositionProgressService.cs`, um Z. 839) nimmt als Einführungstag
`max(erster Spieltag, erster Pflichttag)`. Legt der Vater am Sonntag eine Tagespflicht an, ist der Sonntag der Einführungstag –
den überspringt die Abrechnung seit B-731 ohnehin; der Montag, an dem das Kind die neuen Wörter einführt, ist dann nicht mehr
verschont und kostet Malus. Gleiches für eine Position, die in einer Pause entsteht. Reparaturweg laut Review: den
Einführungstag auf den ersten Pflichttag ab diesem Datum vorschieben (`ChildDutyDays` hineinreichen); Test: Position am
Sonntag angelegt, Einführung am Montag, kein Malus.

## Entscheidungen

1. Der Einführungstag ist der erste Pflichttag (`ChildDutyDays.IsDutyDay`) ab `max(erster Spieltag, erster Pflichttag)`.
2. Die Portion zählt die Einführungen vom ersten Pflichttag der Position bis zum Einführungstag – auch was am freien Sonntag
   schon eingeführt wurde.

## Akzeptanzkriterien

1. Position am Sonntag angelegt, Wörter am Montag eingeführt, Abrechnung am Dienstag: kein Malus.
2. Dasselbe, wenn die ersten Tage der Position in einer Pause liegen.

## Verlauf

- **2026-11-25** — angelegt im Nachtlauf O (Sprint 191) aus dem Review von B-731; der Lauf endete am Fehlerzähler, bevor der
  Fund behoben werden durfte.
- **2026-11-26** — `idee` → `in-arbeit` im Merge-Review von M/N/O (Branch `merge-1125`), Reparaturweg des Reviews auf
  Anordnung des Nutzers. Belege:
  - AK 1 → `LerntageTests.Einfuehrungstag_Am_Sonntag_Rueckt_Auf_Den_Montag`, AK 2 →
    `LerntageTests.Einfuehrungstag_In_Der_Pause_Rueckt_Hinter_Die_Pause`. Rote Probe: das Vorschieben auf den Pflichttag
    abgeschaltet → beide rot (Montag bzw. Samstag gebucht).
  - Commit: `f4df2984` (gemeinsam mit B-1022, dieselbe Datei). Offen für `abgenommen`: `pugling-reviewer` über den Fix.
  - Verifikation nach der letzten Code-Änderung: Backend-Suite 2607/2607 (Release), `vitest` 1512/1512, `tsc -b` grün, E2E
    `lerntage-und-pause`, `sparziel`, `lob`, `gleichung-loesen`, `pfad`, `bonus-deckel`(+`-voll`), `uebungstypen` 8 passed
    (Ports 5244/5284, Wegwerf-DB).
- **2026-11-26** — Sprint 192: `pugling-reviewer` über `f4df2984` ohne Korrektheitsfund; offen war Entscheidung 2 ohne Test.
  Neu `LerntageTests.Einfuehrung_Am_Freien_Sonntag_Schont_Den_Montag` (Einführung am Sonntag, Montag nichts, Abrechnung
  Dienstag). Mutante M3 (Zählfenster nur am Einführungstag) → rot an genau diesem Fall.
- **2026-11-26** — `in-arbeit` → `abgenommen` (Sprint 192, `docs/pm-sitzung-2026-11-26.md`). Verifikation am Endstand `4e70c583`: Backend-Suite 2609/2609 (Release), `vitest` 1514/1514, `tsc -b` grün, E2E
  `lerntage-und-pause` + `pfad` 2 passed (Ports 5244/5284). Reviews: `pugling-reviewer` (zweimal, zuletzt ohne Fund),
  `frontend-reviewer` (zwei Funde, behoben). Rollengang an einer Kopie der echten DB (`kopie-server.sh`, `:5201`, Server nach
  der letzten Änderung gestartet): Abrechnung an der Kopie unauffällig (kein Malus beim Login); die Fälle Sonntag/Pause/Einführung am freien Sonntag tragen `LerntageTests` (Mutante M3 → rot). Commits `f4df2984`, `4e70c583`.
