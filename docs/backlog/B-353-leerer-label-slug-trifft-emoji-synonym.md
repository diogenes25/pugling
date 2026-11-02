---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/vater]
aliases: [leerer Label-Slug, Emoji-Label landet auf fremdem Schlagwort]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu B-351 in Lauf M (docs/pm-sitzung-2026-11-02-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-351]
wartet_auf: ""
nachgeschaut: "2026-11-02"
---

# B-353 · Ein Interesse ohne Buchstaben landet auf einem fremden Schlagwort mit Emoji-Synonym

`InterestSlug.From` gibt für Text ohne Buchstaben oder Ziffern `""` zurück („⚽", „!!!", „–"). Seit
[B-351](B-351-synonym-regel-symmetrisch.md) faltet `InterestTagService.EnsureAsync`
(`backend/Pugling.Api/Services/Shared/InterestTagService.cs:48/51`) auch das Label gegen die Synonyme und vergleicht
`x == labelSlug` – ohne den Rücksprung, den der Slug-Zweig für einen leeren Wert hat. `Clean()` lässt ein Synonym „⚽"
zu, und `SynonymClashAsync` überspringt leere Slugs nur auf der gesuchten Seite.

Fehlerfall des Prüfers: Tag „Fußball" führt das Synonym „⚽"; der Vater setzt das Kind-Interesse
`{slug: "gaming-x", label: "🎮"}`; kein Slug-, kein Label-Treffer, dann `"" == ""` → das Interesse landet auf „Fußball".
Vor `d0afbc0` wurde nur gegen den Slug verglichen, der nie leer ist – die Regression kam mit B-351.

Vorschlag: `labelSlug.Length > 0 && x == labelSlug`, dazu ein Test mit Emoji-Synonym und Emoji-Label.

## User Story

Als **Vater**, der dem Kind ein Interesse mit einem Emoji als Namen gibt, möchte ich, dass es ein eigenes Schlagwort wird,
statt auf einem fremden zu landen, das zufällig ein Emoji als Synonym führt.

## Ist-Stand (Lauf M, am Code belegt, Stand `d0afbc0`)

- `InterestTagService.EnsureAsync` (`backend/Pugling.Api/Services/Shared/InterestTagService.cs:48-51`) vergleicht jedes
  Synonym mit `x == slug || x == labelSlug`; `slug` ist dort nie leer (Rücksprung `:29`), `labelSlug` schon.
- **Gemessen:** Tag mit Synonym „⚽", Kind-Interesse `{slug: "gaming-…", label: "🎮"}` → das Interesse hängt am fremden Tag.

## Die echte Lücke

„Leer" ist kein Wert, der etwas benennt – zwei leere Faltungen sind nicht dasselbe Wort.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Ein leerer Label-Slug vergleicht sich mit keinem Synonym** (`labelSlug.Length > 0 && …`), wie der Slug-Zweig es
   schon tut. *Kosten:* keine; Synonyme ohne Buchstaben bleiben speicherbar, treffen aber nichts – das war schon so.

## Akzeptanzkriterien

1. Ein Interesse mit Emoji-Label landet nicht auf einem Schlagwort mit Emoji-Synonym.

## Schätzung

`XS`, Backend, keine Migration, kein Vertragsbruch. Testweg: `InterestTagLabelTests`.

## Verlauf

- **2026-11-02** — angelegt aus der Nachschau zu B-351 in Lauf M (`pugling-reviewer` über `d0afbc0`, 🟡). Aus Code gelesen.
- **2026-11-02** — Lauf M, Teil 2: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe **1 failed**
  (erwartet 1): `Interesse_Ohne_Buchstaben_Trifft_Kein_Emoji_Synonym` (das Interesse hing am fremden Tag). AK 1 → dieser
  Test; Mutante „ohne Leer-Wächter" → **1 failed**.
- **2026-11-02** — Review `pugling-reviewer`: kein 🔴/🟡 zu B-353; 🟢 Zusicherung nur negativ → positive Zusicherung auf den eigenen Slug ergänzt (ohne Zählung), Mutante „ohne Leer-Wächter" weiter **1 failed**. Geschwisterpfade (`SynonymClashAsync`, `SynonymTakenAsync`, Create/Update, `EnsureManyAsync`) laut Prüfer ohne dasselbe Problem.
- **2026-11-02** — Abnahme Lauf M, Teil 2: volle Suite **1045/1045**, `InterestTagLabelTests` **17/17**. E2E **102 passed** (`exit=0`, nach dem letzten Code-Edit gestartet; die Specs `schlagwort-*` fahren den Schlagwort-Pfad). Rollengang: Vater maschinell über `Interesse_Ohne_Buchstaben_Trifft_Kein_Emoji_Synonym` (Kind-Interesse über die echte Supervisor-Route), Creator über die Schlagwort-Tests; Sohn Regressionszeuge (kein Pfad im Diff). **Abgenommen**, Commit siehe Protokoll ([Lauf M](../pm-sitzung-2026-11-02-nacht.md)).
- **2026-11-02** — Nachschau in Lauf N, Teil 1 (frischer `pugling-reviewer`): **sauber**. Geprüft: Wächter an `InterestTagService.cs:52`, Mutante „ohne Wächter" macht die positive Zusicherung `[gaming]` rot, gleich welcher Tag mit leer faltendem Synonym zuerst steht; Nachbarpfade `SynonymTakenAsync`, `SynonymClashAsync`, PATCH-`carried`, `EnsureManyAsync`, `MediaAssetsController:96` ohne dasselbe Problem. 🟢 (Produktfrage, ohne Story): ein Emoji-Label lässt sich anlegen, aber nicht per `PATCH` setzen (`DeriveRequiredSlug`).
