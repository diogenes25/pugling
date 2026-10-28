---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Zu kurze PIN zeigt englische Meldung]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Bau von B-28 in Lauf C (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-281 · Eine zu kurze PIN meldet die Oberfläche auf Englisch

Seit [B-28](B-28-login-name-sequenziell.md) lehnt der Server eine nicht-leere PIN unter vier Zeichen mit
`400 validation_error` ab („The PIN must have at least 4 characters; an empty PIN disables the login.“). B-28
Entscheidung 2 wollte bewusst keinen eigenen Fehlercode. `validation_error` hat in `GERMAN_PROBLEM_TEXT`
(`frontend/src/lib/api.ts`) keine deutsche Fassung, weil der Code für viele verschiedene Prüfungen steht. Wer im
Vater-Web eine dreistellige PIN vergibt (Registrierung, Kind-Hub, eigenes Konto, Lehrer-Konto), sieht darum den
englischen Satz.

Zwei Wege: Die PIN-Felder prüfen die Mindestlänge selbst vor dem Absenden (`minLength`/eigene Meldung am Feld, der
Server bleibt die Quelle der Wahrheit), oder ein eigener Code `pin_too_short` mit deutscher Fassung. Der zweite Weg
widerspricht B-28 Entscheidung 2 und gehört darum zum Nutzer.

## User Story

Als Vater, der eine dreistellige PIN vergibt, möchte ich auf Deutsch lesen, dass sie zu kurz ist.

## Ist-Stand am Code

`PinHasher.IsAcceptable` (`backend/Pugling.Api/Auth/PinHasher.cs:31`): leer oder ab vier Zeichen. Fünf Formulare senden
eine PIN: `VaterDashboard` (Kind anlegen), `VaterKind` (Kind-Hub), `VaterLogin` (Registrierung Vater und Lehrer),
`VaterProfil` (eigenes Konto), `VaterWizard` (neues Kind). Keines prüfte die Länge.

## Akzeptanzkriterien

1. Jedes der fünf Formulare meldet eine nicht leere PIN unter vier Zeichen vor dem Absenden auf Deutsch.
2. Leer bleibt erlaubt, wo der Server es erlaubt.
3. Geprüft wird der Wert, der gesendet wird (getrimmt oder nicht, wie das Formular sendet).

## Entscheidungen

1. Der erste Weg der Story: die Felder prüfen selbst (`lib/pin.ts`), der Server bleibt die Quelle der Wahrheit. Der
   zweite (eigener Code `pin_too_short`) widerspräche B-28 Entscheidung 2 und gehört zum Nutzer – nicht genommen.
   Kosten: die Regel steht an zwei Orten (Server und `PIN_MIN_LENGTH`).

## Schätzung

S, `frontend`, keine Migration, kein Vertragsbruch. Testweg: Unit-Test der Regel und ein struktureller Fall über die
fünf Formulare.

## Verlauf

- **2026-10-26** — angelegt beim Bau von B-28 in Lauf C ([Protokoll](../pm-sitzung-2026-10-26-tag.md), NP-3). Aus
  dem Code gelesen (`errorMessage` gibt für einen Code ohne deutsche Fassung `e.message` aus), nicht im Browser
  gesehen.
- **2026-10-28** — in Lauf H, Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) ausformuliert, gegrillt (Weg 1, Freigabe 1), geschätzt (S) und gebaut.
  Rote Probe gegen den Ausgangsstand nicht möglich (neues Modul); Mutanten „`<=` statt `<`“, „leer gilt als zu kurz“,
  „Aufruf in `VaterProfil` entfernt“ → je **1 failed**. **Review-Fund 🟡 (gezählt):** der Kommentar des strukturellen
  Falls behauptete, auch ein künftiges sechstes Formular zu fangen – gestrichen, die Grenze steht jetzt im Kommentar.
  Der Reviewer hat alle PIN-sendenden API-Aufrufe den fünf Formularen zugeordnet und „geprüft = gesendet“ je Formular
  nachgelesen. Fallstrick dabei: eine Mutante an der neuen, ungetrackten `pin.ts` ließ sich nicht per `git checkout`
  zurücknehmen – von Hand wiederhergestellt und nachgeprüft. Vitest **555/555**. Rollengang: die volle E2E-Suite gegen frisch gestartete Server (**99 passed**, `exit=0`, nach der letzten Änderung gestartet); Browser-Extension nicht genutzt. **`abgenommen`.**
