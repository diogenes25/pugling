---
tags: [typ/story, status/idee, bereich/agent, rolle/creator]
aliases: [null in wordBank, null in Alternatives, null-Lücke Cloze]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Review zu B-302/B-303 in Lauf F, Teil 3 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-306 · Ein `null`-Element aus dem JSON-Modus besteht die Prüfung der Übungsstrategien und stürzt danach roh ab

Dieselbe Klasse wie [B-303](B-303-fill-null-thema-im-json-modus.md), aber im `create`-Pfad des Agenten: die Regeln
(`Validate`) lesen die Listen null-sicher, der Schritt danach (`ToPayloadAsync` bzw. `Clean`) nicht. Ein Entwurf
besteht also die Prüfung und endet erst beim Anlegen (nur außerhalb von `--dry-run`) mit einer
`NullReferenceException`. Gelesen vom Prüfer, nicht gemessen (`unverifiziert`):

- `Drafting/ClozeStrategy.cs:116` – `Clean` trimmt `WordBank` und `Gap.Alternatives` ohne Null-Schutz; `Validate`
  liest die Wortbank null-sicher (Zeile 87), `"wordBank":["a",null]` kommt also durch.
- `Drafting/TranslationStrategy.cs:70` – `Alternatives?.Select(a => a.Trim())`, `Validate` sieht `Alternatives` nicht an.
- `ClozeStrategy`, Randfall: Lücken `[g1, null]` bei `ItemCount = 2` bestehen Zähl- und Platzhalterprüfung (null wird
  übersprungen) und fallen dann bei `g.Answer.Trim()` (Zeilen 96/105).

**Behebung (Vorschlag):** `v?.Trim() ?? ""` in `Clean` und bei `Alternatives` (Muster `DraftRules.cs:73`), leere
Einträge weglassen; eine `null`-Lücke als Regelverletzung (dann greift die Reparaturrunde). Je ein Test mit einem
`FakeChatClient`-JSON mit `null`-Element und `DryRun: false`.

## Verlauf

- **2026-10-27** — angelegt aus dem Review zu B-302/B-303 (Lauf F, Teil 3, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 🟡 außerhalb des Diffs: eigene Story, weil das Sprint-Ziel (der `fill`-Pfad) ohne sie erfüllt ist. Ob es ein
  Entgleiten ist, entscheidet erst die Messung – darum `entgangen_bei` noch leer.
