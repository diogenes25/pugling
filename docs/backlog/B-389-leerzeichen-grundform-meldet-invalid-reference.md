---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Leerzeichen-baseFormKey ist unbekannte Referenz]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-v
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-389 · Ein `PATCH` im Vokabelspeicher mit `baseFormKey: "  "` meldet `invalid_reference`

`VocabularyStoreController.cs:345-359` prüft `Length == 0`; ein Schlüssel aus Leerzeichen sucht also eine Zeile und findet keine.
Der Anlege-Weg (`:250`, `IsNullOrWhiteSpace`) behandelt denselben Wert als „nicht angegeben". Das Verhalten ist alt, seit B-329
heißt nur der Code anders. Zu entscheiden beim Ausformulieren: „nicht angegeben" wie beim Anlegen oder `validation_error`.

## User Story

Als **Creator** möchte ich, dass ein Grundform-Schlüssel aus Leerzeichen im `PATCH` dasselbe bedeutet wie beim Anlegen – „kein Schlüssel“ –
statt als unbekannte Referenz abgewiesen zu werden.

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** Im `PATCH` gilt Leerraum wie `""`: die Verknüpfung (samt Bezeichnung) wird entfernt. **Grund:** das
   Anlegen behandelt Leerraum als „nicht angegeben“ (`VocabularyStoreController.cs:251`), und im `PATCH` ist `""` der vereinbarte
   Wert für „keine Grundform“; auch `baseFormRelation` macht aus Leerraum schon `null`. Ein `validation_error` wäre die andere
   vertretbare Lesart, hätte aber zwei Bedeutungen für denselben Wert in einem Controller hinterlassen.

## Akzeptanzkriterien

1. `PATCH` mit `baseFormKey: "   "` auf eine verknüpfte Vokabel → `200`, `baseFormId` danach `null`.

## Schätzung

`XS`, `wo: backend`, eine Bedingung.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf U, Sprint 4 (Lauf V, Retro Sprint 5; frischer `pugling-reviewer`, 🟢).
- **2026-11-05** — am Code verifiziert (der Test fiel vor dem Fix mit `400` in `UnknownReferenceCodeTests.cs:273`), ausformuliert,
  gegrillt, geschätzt und gebaut in Lauf X, Sprint 10. AK 1 → `:272-275`; die Verknüpfung ist vorher gesetzt (`:270`), „entfernt“
  unterscheidet sich also vom Ausgangszustand.
- **2026-11-05** — Review `pugling-reviewer`: kein 🔴/🟡; Lesart „Leerraum = entfernen“ bestätigt (der `PATCH` hat für „nicht angegeben“ schon `null`). 🟢 die Entscheidung entfernt auch die Bezeichnung, der Test prüfte sie nicht – jetzt `baseFormRelation` gesetzt (`:268`, vorher geprüft `:272`) und danach `null` (`:279`); rote Probe „Leerraum räumt nur die Id“ → rot nur in `:279`. **Zähler Sprint 10: 1 von 5.** 🟢 DTO-Summary nennt Leerraum (nur Text). 🟢 hingenommen: der Batch-Weg nutzt dieselbe Kernmethode, ohne eigenen Fall; ein Schlüssel mit Rand-Leerzeichen wird weiter nicht getrimmt (Anlegen und Ändern gleich).
- **2026-11-05** — **abgenommen.** Volle Suite 1098/1098 nach dem letzten Edit; Rollengang Creator als E2E: volle Playwright-Suite 115 passed gegen ein frisch gestartetes Backend mit der Änderung (danach nur Test und DTO-Summary). **Grenze:** kein Klickweg – der Vokabelspeicher trimmt die Eingabe vorher (B-389), und der Wort-Endpunkt hat im Dialog keine Id-Eingabe (B-394); belegt per Integrationsfall. Vater und Sohn unberührt. Commit: siehe nächster Eintrag.
