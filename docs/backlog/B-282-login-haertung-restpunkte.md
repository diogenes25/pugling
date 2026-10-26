---
tags: [typ/story, status/idee, bereich/backend, bereich/sicherheit]
aliases: [NP-3 Restpunkte, Login-Härtung Restpunkte]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zu NP-3 in Lauf C (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-282 · Restpunkte der Login-Härtung (NP-3)

Der `pugling-reviewer` hat zu NP-3 ([B-245](B-245-token-mit-fid-auf-geloeschten-adult.md),
[B-28](B-28-login-name-sequenziell.md), [B-230](B-230-pin-hash-doppelt-gehalten.md)) neun Punkte genannt. Die vier
mit dem größten Gewicht für die Tests sind behoben. Diese vier bleiben, ausdrücklich zurückgestellt:

1. **Abbruch in `OnTokenValidated`** (`backend/Pugling.Api/Auth/TokenSubjectCheck.cs`): Die Abfrage nimmt
   `RequestAborted`. Wirft sie bei einem Client-Abbruch, loggt `JwtBearerHandler` das auf **Error** und wirft weiter.
   Das läuft gegen die Entscheidung, Abbrüche als 499 ohne Fehler-Log zu beenden (`Program.cs`). Vorschlag: die kurze
   PK-Abfrage mit `CancellationToken.None` und einem Satz Begründung (`backend/CLAUDE.md`, „CancellationToken in drei
   Teilen“), oder die Abweichung bewusst annehmen.
2. **Seed-PIN eines Kindes nach Namen** (`backend/Pugling.Api/Data/Seed.cs`, `SeededChildPins`): `ChildrenController.Create`
   speichert Kind und Konto in zwei Schritten. Stirbt der Prozess dazwischen, bekäme ein selbst angelegtes Kind namens
   „Sohn“ beim nächsten Start mit `Seed:Enabled` die bekannte PIN 1111. Unwahrscheinlich. Vorschlag: die Tabelle nur
   auf die geseedeten Kinder anwenden (über den betreuenden Erwachsenen) oder die Seed-Konten in den Seed-Routinen
   selbst anlegen.
3. **Guard nach der Änderung** (`ChildrenController.Update`, `AuthController.UpdateMe`): Die PIN-Prüfung steht hinter
   Änderungen an der getrackten Entität. Harmlos, solange der frühe Ausstieg vor `SaveChanges` liegt, aber gegen „Guard
   Clauses zuerst“. Vorschlag: direkt hinter die Null-Prüfung ziehen, wie in `AdultsController`.
4. **Zeitseitenkanal** (nur Hinweis, außerhalb von B-28 AK 5): Unbekannte Id, fehlendes Konto und jetzt auch eine
   leere gespeicherte PIN antworten ohne PBKDF2-Arbeit. Die Antwortzeit unterscheidet „deaktiviert/unbekannt“ von
   „falsche PIN“. Vorschlag, falls relevant: ein Schein-`Verify` gegen einen festen Hash auf allen frühen Ausgängen.
5. **Leere PIN per PATCH nur als „angenommen“ geprüft** (`backend/Pugling.Api.Tests/PinPolicyTests.cs`, aus dem
   Delta-Review): Ein Schreibpfad, der eine leere PIN mit 2xx annimmt, aber ignoriert (`if (!string.IsNullOrEmpty(dto.Pin)) SetPin(…)`),
   bliebe grün. Vorschlag: nach jedem leeren PATCH den Login mit der alten PIN auf 401 prüfen (für `adult` und `me` je ein
   eigener Erwachsener).

**In Lauf C bewusst nicht behoben:** Jede Behebung hätte gezählt, und der Zähler stand nach den vier wichtigeren
Behebungen bei 4.

## Verlauf

- **2026-10-26** — angelegt aus dem `pugling-reviewer` zu NP-3 in Lauf C ([Protokoll](../pm-sitzung-2026-10-26-tag.md)),
  Funde 4, 5, 7, 9; Punkt 5 aus dem Delta-Review.
