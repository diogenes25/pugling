---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/sicherheit]
aliases: [Abbruch in OnTokenValidated, PIN-Guard nach Änderung]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-282 Punkte 1 und 3 (Review zu NP-3, docs/pm-sitzung-2026-10-26-tag.md), abgeteilt in Lauf S, Teil 3
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-372 · Ein Abbruch bei der Kontoprüfung landet als Fehler im Log, und zwei PIN-Prüfungen stehen hinter Änderungen

Abgeteilt aus [B-282](B-282-login-haertung-restpunkte.md), Punkte 1 und 3:

1. **Abbruch in `OnTokenValidated`** (`Auth/TokenSubjectCheck.cs`): die Kontoabfrage nahm `RequestAborted`. Legt der Client während
   der Prüfung auf, wirft sie; `JwtBearerHandler` loggt jede Ausnahme aus dem Hook auf **Error** und wirft weiter. Das läuft gegen die
   Entscheidung, Abbrüche als 499 ohne Fehler-Log zu beenden (`ClientAbortExceptionHandler`, `backend/CLAUDE.md` Punkt 3).
2. **Guard nach der Änderung** (`ChildrenController.Update`, `AuthController.UpdateMe`): die PIN-Prüfung stand hinter Änderungen an
   der getrackten Entität. Harmlos, solange der Ausstieg vor `SaveChanges` liegt, aber gegen „Guard Clauses zuerst"; der Zwilling
   `AdultsController.Update` prüft schon oben.

## User Story

Als **Entwickler**, der das Fehler-Log liest, möchte ich dort keine Abbrüche von Clients finden, und als Leser eines Controllers
möchte ich jede Ablehnung vor der ersten Änderung sehen.

## Ist-Stand am Code (Stand `764f7b5`)

- `TokenSubjectCheck.cs:35`: `AnyAsync(a => a.Id == aid, context.HttpContext.RequestAborted)`.
- `ChildrenController.cs:117`, `AuthController.cs:181`: PIN-Prüfung nach den Feld-Zuweisungen; `AdultsController.cs:85` davor.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **`CancellationToken.None` für die Kontoabfrage**, mit einem Satz Begründung am Code: eine Primärschlüssel-Abfrage ist zu kurz, um
   sich abzubrechen zu lohnen. Das ist eine **neue, verwandte** Ausnahme zur Token-Regel (`backend/CLAUDE.md`): deren Teil 3 nennt nur
   kompensierende Schritte nach dem Commit; hier geht es um einen Lesezugriff in einem Hook, dessen Ausnahme der Handler selbst als
   Fehler loggt. Die Alternative „Abweichung annehmen" verwürfe die bestehende Entscheidung „Abbruch = 499 ohne Fehler-Log".
2. **Jede Ablehnung vor der ersten Zuweisung, ohne die Reihenfolge der Antworten zu ändern.** In `ChildrenController.Update` rückt
   die PIN-Prüfung hinter die Null-Prüfung (davor stand keine andere Ablehnung). In `auth/me` stehen Name, E-Mail-Dublette und PIN
   jetzt als drei Prüfungen vor allen Zuweisungen – **in der alten Reihenfolge**: eine belegte E-Mail mit zu kurzer PIN antwortet weiter
   `409 duplicate_email`, ein leerer Name mit zu kurzer PIN weiter mit dem Namen. (Die erste Fassung zog nur die PIN nach oben und
   machte aus dem 409 still ein 400 – vom Review gefunden.)
3. Punkte 2 und 4 von B-282 (Seed-PIN nach Namen, Zeitseitenkanal) und die Restpunkte zu gemischten Konten bleiben dort: sie
   verlangen eine Abwägung, kein Herstellen.

## Akzeptanzkriterien

1. Eine bereits abgebrochene Anfrage mit gültigem Token läuft durch die Kontoprüfung, ohne zu werfen, und wird nicht abgewiesen.
2. In `ChildrenController.Update` und `AuthController.UpdateMe` steht jede Ablehnung vor der ersten Zuweisung an die Entität.
3. Die Antworten bleiben, was sie waren: belegte E-Mail + zu kurze PIN an `auth/me` → `409 duplicate_email`.

## Schätzung

`XS`. Testweg: `DeletedAccountTokenTests` (AK 1), Code-Lesen (AK 2 – eine reflexive Zusicherung über „Guard vor Zuweisung" wäre
teurer als der Fund), `AccountSelfServiceTests` (AK 3).

## Verlauf

- **2026-11-04** — abgeteilt aus B-282 in Lauf S, Teil 3; ausformuliert, gegrillt (Freigabe 1), geschätzt, gebaut. AK 1 →
  `DeletedAccountTokenTests.Abgebrochene_Anfrage_Wirft_Nicht_Bei_Der_Kontopruefung`. **Rote Probe** vor dem Fix: erwartet 1 failed,
  gemessen **1 failed** (`TaskCanceledException`); danach grün. AK 2: per `grep` – `IsAcceptable` jetzt `ChildrenController.cs:104`
  (direkt hinter `NotFound`) und `AuthController.cs:163`; `PinPolicyTests` danach grün (11/11 zusammen mit den Token-Tests).
- **2026-11-04** — Review `pugling-reviewer`: zwei 🟡. (1) Die erste Fassung zog in `auth/me` nur die PIN-Prüfung nach oben – eine belegte E-Mail mit zu kurzer PIN antwortete dann `400 validation_error` statt `409 duplicate_email`, und kein Test schickte beides. Behoben, indem alle drei Prüfungen in alter Reihenfolge vor die Zuweisungen rücken; neuer Fall `AccountSelfServiceTests.FremdeEMail_MitZuKurzerPin_MeldetWeiterDieEMail`, Mutante „PIN-Prüfung zuerst" (= die erste Fassung) → **1 failed**. Zählt (Code und Test geändert): **Zähler Teil 3: 1.** (2) Die Story behauptete, Teil 3 der Token-Regel nenne genau diese Ausnahme – er nennt nur Schritte nach dem Commit; Entscheidung 1 umformuliert (ohne Zählung, nur Text). 🟢 ohne Zählung: Zeile `:35` statt `:37`. Vom Prüfer nachgerechnet: `UseExceptionHandler` (`Program.cs:577`) liegt vor `UseAuthentication` (`:687`), der 499-Pfad greift weiter; einziger verdrahteter JwtBearer-Hook ist `OnTokenValidated`; die Ownership-Filter laufen außerhalb des Handler-`try` und dürfen `RequestAborted` behalten.
- **2026-11-04** — Abnahme Lauf S, Teil 3: volle Suite **1073/1073**, Build ohne Warnung, E2E-Gesamtlauf **109 passed** (`exit=0`, nach dem letzten Code-Edit). PIN-Prüfung in `auth/me` jetzt `AuthController.cs:170`, nach Name und E-Mail und vor jeder Zuweisung. Rollengang: der Entwickler ist die Rolle (Log, Lesbarkeit); Vater maschinell über `AccountSelfServiceTests` und `PinPolicyTests` im echten HTTP-Pfad (Antworten unverändert); Creator und Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf S](../pm-sitzung-2026-11-04-nacht.md)).
