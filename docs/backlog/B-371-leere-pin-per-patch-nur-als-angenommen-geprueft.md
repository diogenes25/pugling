---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/test, bereich/sicherheit]
aliases: [leere PIN per PATCH ungeprüft]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-282 Punkt 5 (Delta-Review zu NP-3, docs/pm-sitzung-2026-10-26-tag.md), abgeteilt in Lauf S, Teil 2
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-04"
entgangen_bei: []
wartet_auf: ""
---

# B-371 · Eine leere PIN per PATCH war nur als „angenommen" geprüft

Abgeteilt aus [B-282](B-282-login-haertung-restpunkte.md), Punkt 5. `PinPolicyTests.Aendern_…` schickt auf allen drei Änderungswegen
`pin = ""` und prüft nur `2xx`. Ein Schreibpfad, der die leere PIN annimmt, aber überspringt
(`if (!string.IsNullOrEmpty(dto.Pin)) SetPin(…)`), bliebe grün – und das Konto ließe sich weiter mit der alten PIN öffnen, obwohl
der Vater „Login deaktiviert" gewählt hat. Dazu gehen `adult` und `me` im bestehenden Test auf **denselben** Erwachsenen: ein Pfad
könnte die Arbeit des anderen tragen.

## User Story

Als **Vater**, der den Login seines Kindes (oder seinen eigenen) per leerer PIN abschaltet, möchte ich mich darauf verlassen, dass
die alte PIN danach nicht mehr öffnet.

## Ist-Stand am Code (Stand `cc789ed`)

`ChildrenController.cs:122`, `AdultsController.cs:95`, `AuthController.cs:187`: je `if (dto.Pin is not null) AccountService.SetPin(…)`
– richtig, nur ungetestet in der leeren Richtung.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Ein neuer Fall, je Pfad ein eigenes Subjekt** (Kind, Erwachsener über `supervisor/adults/{id}`, Erwachsener über `auth/me`),
   mit Gegenprobe vor dem PATCH: die alte PIN öffnet zuerst (200), danach nicht mehr (401). Ohne die Gegenprobe könnte der Fall
   auch an einer falschen PIN rot bzw. grün werden.
2. Die übrigen Punkte von B-282 bleiben dort (Abbruch-Log, Seed-PIN nach Namen, Guard-Reihenfolge, Zeitseitenkanal, gemischte Konten).

## Akzeptanzkriterien

1. Je Änderungsweg: nach `PATCH pin = ""` lehnt der Login mit der vorher gültigen PIN ab (401); vorher nahm er sie an (200).

## Schätzung

`XS`, nur Test. Testweg: `PinPolicyTests`.

## Verlauf

- **2026-11-04** — abgeteilt aus B-282 und gebaut in Lauf S, Teil 2. AK 1 → `PinPolicyTests.Leere_Pin_Per_Patch_Sperrt_Den_Alten_Login`;
  Mutante „Pfad überspringt die leere PIN" je Pfad: Kind **1 failed**, `supervisor/adults` **1 failed**, `auth/me` **1 failed**. Im
  Ausgangszustand grün (der Code war richtig, es fehlte die Zusicherung).
- **2026-11-04** — Review `pugling-reviewer`: kein 🔴/🟡. Vom Prüfer nachgerechnet: drei eigene Subjekte, je Login vor dem PATCH 200; Ratenbegrenzer im Test aus (`PuglingWebAppFactory.cs:144`); keine Überschneidung mit `Aendern_…`; zitierte Zeilen `:122`/`:95`/`:187` stimmen.
- **2026-11-04** — Abnahme Lauf S, Teil 2: volle Suite **1071/1071**, Build ohne Warnung, Vitest **638/638**, E2E-Gesamtlauf **109 passed** (`exit=0`, nach dem letzten Code-Edit). Rollengang: Vater maschinell – der Fall schaltet Kind- und eigenen Login per leerer PIN ab und prüft im echten HTTP-Pfad, dass die alte PIN danach 401 bekommt; Creator und Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf S](../pm-sitzung-2026-11-04-nacht.md)).
- **2026-11-04** — Nachschau in Lauf S, Teil 3 (frischer `pugling-reviewer`, `git show 764f7b5`, read-only). Ohne Befund: drei getrennte Subjekte, je 200 vor dem PATCH, Mutante je Pfad rot an seiner 401-Zusicherung; Zeilen `:122`/`:95`/`:187` stimmen; Ratenbegrenzer aus.
