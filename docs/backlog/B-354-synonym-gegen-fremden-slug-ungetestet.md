---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Slug-Hälfte der Synonym-Regel ungetestet]
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
---

# B-354 · Dass ein Synonym nicht den Slug eines fremden Schlagworts tragen darf, prüft kein Test

[B-351](B-351-synonym-regel-symmetrisch.md) Entscheidung 1 verbietet ein Synonym, das den Slug **oder** das gefaltete
Label eines fremden Schlagworts trägt. Die Slug-Hälfte (`InterestTagsController.cs:286`, `.Append(t.Slug)`) trägt keine
Zusicherung: die Mutante „Zeile streichen" bleibt grün. AK 1 nutzt bewusst ein umbenanntes Tag und trifft das Synonym nur
über das gefaltete Label; die E2E-Spec `schlagwort-trotz-fremdem-synonym` fährt den B-341-Fall beim Speichern eines Labels.

Fehlerfall ohne die Zeile: Tag A hat den Slug `fussball-1` und heißt inzwischen „Kicker 1"; `POST` eines Tags B mit
`synonyms: ["Fussball 1"]` → `201`. Danach löst `EnsureAsync("Fussball 1")` per Slug zu A auf, während B denselben Text als
Synonym führt.

Dazu (🟢 desselben Prüfers): Entscheidung 2 vergleicht „bereits geführt" über den Slug, der Test schickt aber den
identischen Text (`:175`); die Mutante „`carried` per Rohtext" bleibt grün.

## User Story

Als **Creator** möchte ich, dass die Regel „ein Text benennt nur ein Schlagwort" in jeder ihrer Hälften von einem Test
gehalten wird, damit ein Umbau sie nicht still halbiert.

## Ist-Stand (Lauf M, am Code belegt, Stand `d0afbc0`)

- `SynonymClashAsync` (`InterestTagsController.cs:279-289`) prüft gegen fremde Synonyme, Slugs und gefaltete Labels;
  `PATCH` lässt Synonyme, die das Tag schon führt, per Slug-Vergleich durch (`:175-176`).
- **Gemessen:** Mutante „`.Append(t.Slug)` weg" und Mutante „`carried` per Rohtext" bleiben mit der Suite grün.

## Die echte Lücke

Zwei Teile von B-351 Entscheidung 1 und 2 standen nur im Code.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`, Testlücke)

1. **Je Teil ein Test**, kein Produktcode: ein Synonym mit dem Slug eines umbenannten Tags (in anderer Schreibweise,
   damit nur die Faltung trifft) → `409`; ein geführtes Synonym in anderer Groß/Kleinschreibung erneut gespeichert → `200`.

## Akzeptanzkriterien

1. Die Mutante „Slug fehlt in der Konfliktmenge" macht einen Test rot.
2. Die Mutante „`carried` per Rohtext" macht einen Test rot.

## Schätzung

`XS`, nur Tests. Testweg: `InterestTagLabelTests`.

## Verlauf

- **2026-11-02** — angelegt aus der Nachschau zu B-351 in Lauf M (`pugling-reviewer` über `d0afbc0`, 🟡 + 🟢). Mutanten
  am Code abgeleitet, nicht gelaufen.
- **2026-11-02** — Lauf M, Teil 2: ausformuliert, autonom gegrillt (Freigabe 1), gebaut. Beide neuen Tests vor jeder
  Mutante **grün** (erwartet: grün, es ist eine Testlücke, kein Fehlverhalten). AK 1 → `Synonym_Das_Ein_Fremder_Slug_Ist_Wird_Abgewiesen`,
  Mutante „`.Append(t.Slug)` weg" → **1 failed**; AK 2 → `Gefuehrtes_Synonym_In_Anderer_Schreibweise_Bleibt_Speicherbar`,
  Mutante „`carried` per Rohtext" → **1 failed**.
- **2026-11-02** — Review `pugling-reviewer`: 🟡 AK 2 fing nur die case-sensitive Rohtext-Mutante (`OrdinalIgnoreCase` bliebe grün) → die erneut geschickte Schreibweise weicht jetzt über Groß/Klein hinaus ab (Bindestrich statt Leerzeichen); Mutanten „Rohtext" und „Rohtext ohne Groß/Klein" je **1 failed** (zählt, Zähler Teil 2: 1).
- **2026-11-02** — Abnahme Lauf M, Teil 2: volle Suite **1045/1045**, `InterestTagLabelTests` **17/17**. E2E **102 passed** (`exit=0`, nach dem letzten Code-Edit gestartet; die Specs `schlagwort-*` fahren den Schlagwort-Pfad). Rollengang: Vater maschinell über `Interesse_Ohne_Buchstaben_Trifft_Kein_Emoji_Synonym` (Kind-Interesse über die echte Supervisor-Route), Creator über die Schlagwort-Tests; Sohn Regressionszeuge (kein Pfad im Diff). **Abgenommen**, Commit siehe Protokoll ([Lauf M](../pm-sitzung-2026-11-02-nacht.md)).
