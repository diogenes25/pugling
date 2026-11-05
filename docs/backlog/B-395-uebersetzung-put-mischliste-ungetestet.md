---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Übersetzung PUT Mischliste ungetestet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
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

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf W, Sprint 9 (Lauf X, Retro Sprint 10; frischer `pugling-reviewer`, 🟡). Entgangen
  bei B-392 (Increment von Lauf W) – kein Limit 2 für Lauf X.
