---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Übersetzung PUT Mischliste ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-x
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-392]
---

# B-395 · Der Klickweg „ein Paar einer Übersetzung ändern" hat keinen Test

B-392 belegt nur einen `PUT` mit **einem** bearbeiteten Paar und neuem Text. Der Editor schickt aber die ganze Liste: unveränderte Paare
mit ihrer Id, das bearbeitete mit `null` (`frontend/src/vater/exerciseConfig.tsx:289-294`). Ungetestet sind darum:

1. **Mischliste:** das unveränderte Paar behält genau seine Id, das bearbeitete bekommt eine (rot bei einer Normalisierung, die alle Ids
   neu vergibt, oder bei einem Fehler in der Index-Zuordnung über `pending` in `TranslationController.NormalizeConfigAsync`).
2. **„Findet es":** ein auf einen vorhandenen Wortlaut geänderter Text bekommt die vorhandene Id, keine Dublette.
3. **B-392 AK 2** („die alte Vokabel ist danach frei") hat keinen Test, der allein fällt – `CatalogReadDeleteTests.cs:203` ist auch
   ohne den Fix grün. Ein Fall, in dem die alte Vokabel vorher gesperrt ist **und** nur durch den neu verlinkten Stand frei wird,
   fehlt; vermutlich genügt Fall 1 mit der Gegenprobe, dass die Vokabel des unveränderten Paars gesperrt **bleibt**.

## Akzeptanzkriterien (je Fall, mit der Mutante, die nur ihn rot macht)

| Fall | Beleg | Mutante |
|---|---|---|
| unverändertes Paar behält seine Id | `CatalogReadDeleteTests.cs:249` | jedes Paar neu auflösen; Ergebnis auf falschem Index |
| bearbeitetes Paar findet den vorhandenen Eintrag | `:250` | Speicher-Suche übersprungen (Dublette) |
| alte Vokabel des bearbeiteten Paars vorher gesperrt (`:242`), danach frei (`:253`); die des unveränderten bleibt gesperrt (`:252`) | `:242`, `:252-253` | **Gegenprobe**, auch ohne B-392 grün |

## Schätzung

`XS`, `wo: backend`, nur ein Test. Kein Laufzeit-Effekt.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf W, Sprint 9 (Lauf X, Retro Sprint 10; frischer `pugling-reviewer`, 🟡). Entgangen
  bei B-392 (Increment von Lauf W) – kein Limit 2 für Lauf X.
- **2026-11-05** — ausformuliert, geschätzt und gebaut in Lauf X, Sprint 11 (Vorschlag (BP) angewandt: je Fall die Mutante, die nur ihn
  rot macht; die Gegenprobe heißt so). Ein Fall statt drei: das Setup ist ein Editor-Speichern. Rote Proben, alle kompiliert: jedes
  Paar neu auflösen → rot `:245` (dazu der B-386-Fall `:160`); Ergebnis auf falschem Index → rot nur `:245`; Speicher-Suche übersprungen
  → rot nur `:246`. Volle Suite 1099/1099. **Zu B-392 AK 2:** einen Test, der allein fällt, gibt es nicht – das AK gilt auch ohne
  den Fix; geblieben ist die Gegenprobe.
- **2026-11-05** — Review `pugling-reviewer`: kein 🔴. 🟡 die Gegenprobe „alte Vokabel danach frei" bewies nichts, solange nicht
  gezeigt war, dass sie vorher gesperrt ist – jetzt `:242`. 🟢 Anzahl der Paare zugesichert (`:248`), 🟢 Mutante „falscher Index" in
  der Summary dem richtigen Fall zugeordnet (nur Text). **Zähler Sprint 11: 2 von 5.** Rote Proben nach der Korrektur wiederholt:
  neu auflösen → rot `:242` (schon der `POST` verliert die Bindung) und `:160`; falscher Index → rot nur `:249`; Suche übersprungen →
  rot nur `:250`.
- **2026-11-05** — **abgenommen.** Volle Suite 1099/1099 nach dem letzten Edit. **Kein Rollengang im Browser:** die Story ändert nur
  einen Test, keinen Laufzeit-Code – ein Gang durch die App prüfte nichts, was dieser Sprint geändert hat (Playwright lief zuletzt in
  Sprint 10 mit 115 passed auf demselben Produktionscode). Commit: siehe nächster Eintrag.
