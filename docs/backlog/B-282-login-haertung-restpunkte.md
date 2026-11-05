---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/sicherheit]
aliases: [NP-3 Restpunkte, Login-Härtung Restpunkte]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer zu NP-3 in Lauf C (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-11-05"
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

## User Story

Als **Familie** möchte ich, dass der Login weder über eine bekannte Seed-PIN noch über die Antwortzeit etwas über fremde
Konten preisgibt.

## Ist-Stand am Code (vor dem Bau, Stand `2361974`)

- `backend/Pugling.Api/Data/Seed.cs:143,159-163`: `SeededChildPins` nach Namen („Sohn" → 1111) für **jedes** Kind ohne Konto.
- `backend/Pugling.Api/Auth/PinHasher.cs:45-52` und `AuthController.cs:58,76,103`: unbekannte Id, fehlendes Konto und leere
  gespeicherte PIN kehrten vor jeder PBKDF2-Arbeit zurück.
- Punkte 1, 3 und 5 sind in B-371/B-372 gebaut; die drei Restpunkte zu gemischten Konten (Verlauf 2026-10-27) setzen ein
  Konto mit Erwachsenen- **und** Student-Profil voraus, das kein Anlege-Pfad baut.

## Entscheidungen

1. **(Im Auftrag des Nutzers)** Punkt 2: die Seed-PIN gilt nur unter dem geseedeten Betreuer – Schlüssel
   (Betreuer-E-Mail, Name), `Seed.SeededChildPin`. Ein selbst angelegtes „Sohn" unter einem anderen Erwachsenen bekommt keine
   PIN (Login deaktiviert, wie jedes Kind ohne gesetzte PIN). Kosten: eine Abfrage mehr beim Start.
2. **(Im Auftrag des Nutzers)** Punkt 4: `PinHasher.Verify` nimmt `string?` und rechnet auf jedem Weg ohne echten Hash eine
   Attrappen-Ableitung; die drei Login-Aktionen prüfen **vor** der Verzweigung. Übrig bleibt der Unterschied der
   DB-Abfragen (eine gegen zwei) – Mikrosekunden gegen ~50 ms PBKDF2, hingenommen.
3. **(Im Auftrag des Nutzers)** Die drei Restpunkte zu gemischten Konten werden **nicht** gebaut: kein Anlege-Pfad erzeugt
   ein solches Konto. Sie bleiben im Verlauf als bekannte Grenze; ein künftiger Pfad, der gemischte Konten baut, muss sie
   mitnehmen.

## Akzeptanzkriterien

1. `Seed.SeededChildPin("Sohn", [fremde E-Mail])` ist `null`, unter `papa@example.com` „1111"; ein Mitbetreuer nimmt die PIN
   nicht weg.
2. `Verify(pin, null)` und `Verify(pin, "")` kosten mindestens 30 % der Zeit einer falschen PIN gegen einen echten Hash.
3. Altklartext und das Abweisen einer leeren gespeicherten PIN verhalten sich wie vorher.

## Schätzung

`S`, `wo: backend`, keine Migration, kein Vertragsbruch. Testweg: `SeedContractTests`, `SecurityHardeningTests`,
`PinPolicyTests` (unverändert grün).

## Verlauf

- **2026-10-26** — angelegt aus dem `pugling-reviewer` zu NP-3 in Lauf C ([Protokoll](../pm-sitzung-2026-10-26-tag.md)),
  Funde 4, 5, 7, 9; Punkt 5 aus dem Delta-Review.
- **2026-10-27** — Restpunkt aus dem Review zu [B-285](B-285-kind-konto-bleibt-nach-vater-loeschung.md): `ChildrenController.Delete` (`:143-147`) hat für ein **gemischtes** Konto (Erwachsenen- und Student-Profil auf einem Login) dieselbe latente Lücke, die B-285 am Vater-Pfad geschlossen hat – kein Anlege-Pfad baut ein solches Konto, darum nur notiert.
- **2026-10-27** — zweiter Restpunkt aus der Nachschau zu B-285: bleibt ein gemischtes Konto wegen eines ko-betreuten Kindes stehen (korrekt), trägt es weiter E-Mail und PIN des gelöschten Erwachsenen – die Adresse bleibt im Unique-Index belegt, und `auth/login` mit alter `accountId` + Erwachsenen-PIN gäbe einen Student-Token des Kindes. Baut heute kein Anlege-Pfad.
- **2026-10-27** — dritter Restpunkt (Review zu B-289): derselbe hypothetische Fall über einen **vorher ausgestellten** Token – überlebt ein gemischtes Konto die Löschung des Erwachsenen, trägt dessen alter Token weiter `aid` (lebend) und `fid` (gelöscht); `TokenSubjectCheck` prüft nur `aid`, ein Schreiber mit `fid` im Fremdschlüssel liefe wieder in die 500 aus B-245. Baut heute kein Anlege-Pfad.
- **2026-11-04** — Punkt 5 abgeteilt nach [B-371](B-371-leere-pin-per-patch-nur-als-angenommen-geprueft.md) und dort in Lauf S, Teil 2 gebaut. Die Punkte 1–4 und die drei Restpunkte vom 2026-10-27 bleiben hier.
- **2026-11-04** — Punkte 1 und 3 abgeteilt nach [B-372](B-372-abbruch-log-und-guard-reihenfolge-im-login-pfad.md) und dort in Lauf S, Teil 3 gebaut. Hier bleiben Punkt 2 (Seed-PIN nach Namen), Punkt 4 (Zeitseitenkanal) und die drei Restpunkte zu gemischten Konten – alle verlangen eine Abwägung.
- **2026-11-05** — Punkte 2 und 4 gebaut in Sprint 2 der Sitzung 2026-11-05 ([Protokoll](../pm-sitzung-2026-11-05.md)). AK 1 → `SeedContractTests.cs:176`, Mutant „Seed-PIN nur nach Namen" → erwartet 1 failed, gemessen 1 von 43. AK 2 → `SecurityHardeningTests.PinHasher_Verify_RechnetAuchOhneGespeichertenHash`, Mutanten „Attrappe weg" und (Review) „Attrappe nicht für Altklartext" → je 1 von 11; dazu (Review) `Login_mit_unbekannter_Id_kostet_so_viel_wie_eine_falsche_PIN` je Login-Route, Mutant „Kind-Login schließt vor `Verify` kurz" → genau der Fall `child` rot (1 von 11). AK 3 → `PinPolicyTests` und `SecurityHardeningTests.PinHasher_Verify_AkzeptiertAltKlartext` unverändert grün. Seed-Wirkung vom Reviewer gegen `Seed.cs:307/1068` und `AccountService.cs:60` gelesen: frische DB bekommt beide PINs, bestehende Konten bleiben unberührt.
- **2026-11-05** — Review `pugling-reviewer`: keine 🔴, vier 🟡 – zwei mit Test-Änderung behoben (Zähler Sprint 2: 2 von 5: Controller-Reihenfolge der Attrappe ungetestet, Altklartext-Pfad ohne Zeitprobe), zwei nur Text (veralteter Kommentar `CreatorAgentTests`, „403" in `docs/REST/Creator.http`) ohne Zählung. Volle Backend-Suite 1076/1076 vor den Review-Fixes, danach die betroffenen Klassen grün (Zahlen im Protokoll). **abgenommen.** Rollengang: der Login aller drei Rollen läuft durch jede Integrations- und E2E-Klasse (Seed-Familie mit 0000/1111); die Zeitmessung selbst ist kein Rollenweg. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `b32460e`.
- **2026-11-05** — **Nachschau** (frischer `pugling-reviewer`, `git show b32460e`): nur drei `Verify`-Aufrufe (`AuthController.cs:59/78/106`), kein Reset-Pfad; Seed auf bestehender DB unberührt (`AccountService.cs:60`); Zeittests mit Faktor ~1,0 gegen 0,3. 🟢: der Kommentar an `Seed.SeededChildPin` behauptete, die E-Mail sei nicht vom Nutzer wählbar – sie ist es per `PATCH auth/me` → abgeschwächt in [B-383](B-383-match-403-behauptungen-nach-b-310.md). 🟢: die Stories nannten den vollen Lauf nach den Review-Fixes nicht – 1079/1079 (1076 + drei Fälle der neuen Theory).
