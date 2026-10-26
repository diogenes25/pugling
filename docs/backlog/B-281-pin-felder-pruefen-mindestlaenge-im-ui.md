---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Zu kurze PIN zeigt englische Meldung]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Bau von B-28 in Lauf C (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: true
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

## Verlauf

- **2026-10-26** — angelegt beim Bau von B-28 in Lauf C ([Protokoll](../pm-sitzung-2026-10-26-tag.md), NP-3). Aus
  dem Code gelesen (`errorMessage` gibt für einen Code ohne deutsche Fassung `e.message` aus), nicht im Browser
  gesehen.
