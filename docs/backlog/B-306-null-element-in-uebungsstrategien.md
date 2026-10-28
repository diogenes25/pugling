---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [null in wordBank, null in Alternatives, null-Lücke Cloze]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Review zu B-302/B-303 in Lauf F, Teil 3 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-22]
wartet_auf: ""
nachgeschaut: "2026-10-28"
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
- **2026-10-27** — in Lauf F, Teil 4 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)) als `Defekt` unter Freigabe 1
  ausformuliert, geschätzt (XS) und gebaut. **Gemessen:** drei Tests (Wortbank-/Alternativen-`null` im Lückentext,
  `null`-Lücke, `null`-Alternative in der Übersetzung) → rote Probe **3 failed**, je `NullReferenceException` nach
  bestandener Prüfung. Fix: `Clean` und die Übersetzungs-Alternativen `v?.Trim() ?? ""`; eine `null`-Lücke ist eine
  eigene Regelverletzung mit Listenposition („Die Lücke an Position 3 ist leer (null) …“), damit die Reparaturrunde
  greift. Mutante „Regel aus“ → **1 failed**. Review (`pugling-reviewer`): Vokabel/Grammatik haben keinen Rest
  (`NotBlank(item?.…)`), `ExpectedAnswers` läuft erst nach bestandener Prüfung. 🟢 genommen: die Zusicherung prüfte
  nur „leer“ (steht in jeder `NotBlank`-Meldung) → jetzt „Position 3“ und „(null)“; der Testkommentar sagt, dass eine
  `null` für `Clean` genügt. `CreatorAgentTests` **62/62**, Suite **984/984 `exit=0`**. Rollengang: Agent ohne
  Oberfläche, Tests sind der Weg. **`abgenommen`.**
- **2026-10-28** — nachgeschaut (Lauf H, Teil 1, frischer `pugling-reviewer` am Commit `d8e4a0a`, Dateien im Stand des
  Commits): alle vier Strategien in `Validate`, `ExpectedAnswers` und `ToPayloadAsync` auf Dereferenzierung eines
  `null`-Elements nach bestandener Prüfung durchgesehen – kein Rest; Mutanten der drei Fälle hergeleitet (`Clean` zurück
  auf `v.Trim()`, Zeilen 45-46 entfernt, `a.Trim()`), je rot; die Behauptung „trägt auch `w?.Trim()` in Zeile 91“
  nachgerechnet (die `null` steht in der Bank vor „stable“). **Entgangen:** [B-318](B-318-null-element-ohne-fall-in-drei-strategien.md)
  (🟢, kein Fall für ein `null`-Element in Vokabel/Übersetzung/Grammatik), im Sprint behoben.
