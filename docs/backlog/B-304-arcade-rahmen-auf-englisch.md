---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [i18n Teil b1, Rahmen der Arcade auf Englisch, Login Konto Shop Skins Englisch]
status: abgenommen
prio: P3
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-295 (geteilt, Lauf F)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: "Mensch: die neuen englischen Texte in src/i18n/en.ts gegenlesen (AK 5)"
---

# B-304 · Der Rahmen der Sohn-Arcade auf Englisch: Login, Konto, Weg, Shop, Skins, Interessen

Erster Teil der geteilten [B-295](B-295-arcade-korpus-auf-englisch.md): alle Bildschirme der Arcade **außer** dem
Spielweg (Üben/Test, der in [B-305](B-305-arcade-spielweg-auf-englisch.md) folgt). Umfang: `SohnLogin`, `SohnKonto`,
`SohnProgress`, `SohnSkins` (mit den Skin-Namen und `blurb`s aus `lib/skins.ts`), `SohnShop` (mit den Arcade-Anteilen
von `lib/shop.ts`), `SohnInterests` (ohne die schon übersetzte Intro-Karte), `MyObjectives`, `BadgesGallery` in
`GamificationPanels` und die `SavedNotice`-Texte ihrer Aufrufer; dazu die Arcade-Anteile von `lib/labels.ts`, die
diese Bildschirme zeigen (`pointKindLabel` im Konto-Verlauf).

## User Story

Als **Kind, das Englisch lernt**, möchte ich, dass auch Konto, Shop und Skins auf Englisch sprechen – sonst ist die
Immersion nach dem ersten Klick vorbei.

## Ist-Stand am Code · Entscheidungen

Wie B-85 (Entscheidungen 1–6 gelten unverändert; sie stehen dort). Für die Ausführung gilt aus B-295 und der
Nachschau zu B-294:

- **Geteilte Helfer bleiben für das Vater-Web deutsch.** `lib/shop.ts` (`unitAmount`, `priceLabel`), `lib/skins.ts`
  und `lib/labels.ts` werden auch vom Vater-Web benutzt, und das ist nicht übersetzt (B-85, Entscheidung 3 und 6).
  Die Arcade bekommt ihre Texte über Schlüssel; die deutschen Helfer bleiben, wie sie sind. Keine globale
  i18next-Abhängigkeit in einem Helfer, den das Vater-Web aufruft – sonst stünde ein Vater nach einem englischen
  Kind am selben Gerät in einem halb englischen Vater-Web.
- **`SohnLogin` liegt außerhalb der `SohnShell`.** Er folgt der gespeicherten Wahl; der `<html lang>`-Effekt
  wandert dafür an die Wurzel `SohnApp`, damit auch der Login sein `lang` trägt. `sprache.spec.ts` (B-299) prüft dann
  nach dem Abmelden einen **englischen** Login mit `lang="en"` statt eines deutschen – der Rückfall auf `de` bleibt
  für das Verlassen der Arcade (Entmontieren von `SohnApp`) und braucht dort einen eigenen Beleg.
- Pluralformen: jeder neue `count`-Schlüssel mit `_one`/`_other` ([B-298](B-298-plural-im-hud-ein-coins.md)).

## Akzeptanzkriterien

1. Jeder UI-Chrome-Text der genannten Bildschirme liegt auf Deutsch und Englisch vor; der Paritäts-Test bleibt grün.
2. Nutzerdaten (Shop-Artikel, Auszeichnungs-/Zieltitel, Buchungsgründe, Plan-/Übungstitel) bleiben unübersetzt.
3. Das Vater-Web bleibt deutsch, auch wenn am selben Gerät `pugling.lang = en` gespeichert ist (Beleg: ein
   Komponenten- oder E2E-Fall mit Englisch gespeichert, der einen geteilten Helfer im Vater-Web liest).
4. Alle Arcade-Specs bleiben grün (Deutsch gepinnt); `sprache.spec.ts` prüft zusätzlich den Shop auf Englisch.
5. Die englischen Texte liest am Ende ein Mensch gegen.

## Schätzung

**M** (untere Hälfte): acht Bildschirme, die Hälfte davon klein; kein Backend. Angriffsplan: Datei für Datei,
Schlüssel nach Bildschirm gruppiert (`login.*`, `account.*`, `progress.*`, `skins.*`, `shop.*`, `interests.*`,
`objectives.*`, `badges.*`), nach jeder Datei Vitest; zuletzt `sprache.spec.ts` erweitern und die Arcade-Specs.

## Verlauf

- **2026-10-27** — angelegt beim Teilen von [B-295](B-295-arcade-korpus-auf-englisch.md) in Lauf F
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)): B-295 erwies sich beim Lesen als Bündel zweier getrennter
  Flächen (Rahmen-Bildschirme gegen Spielweg mit den Übungskomponenten), zusammen ~2000 Zeilen in 12+ Dateien.
  Entscheidungen aus B-85/B-295 übernommen; die zwei Ausführungs-Punkte oben (geteilte Helfer, Login-`lang`) folgen
  aus Entscheidung 3/5 von B-85 und den Hinweisen in B-295, sie entscheiden keine Produktfrage neu.
- **2026-10-27** — gebaut in Lauf G, Teil 1 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)). Acht Bildschirme +
  `SohnHome` (Skin-Name) + `Mascot` (Vorlesename) auf Schlüssel; `<html lang>`-Effekt an der `SohnApp`-Wurzel;
  Skin-Texte über `lib/skinText.ts`. **Abweichung vom Ist-Stand der Story:** `lib/skins.ts` liest das Vater-Web gar
  nicht – die Skin-Texte kommen trotzdem aus den Ressourcen, `lib/skins.ts` bleibt der visuelle Katalog mit deutschem
  Rückfall. Deutsch ändert sich sichtbar nur in den Pluralformen („1 Ziel“, „1 Tag“ statt „1 Ziele“, „1 Tage“).
- **2026-10-27** — Belege. `arcadeSprache.test.tsx` (am Ende **21 Fälle**): rote Probe gegen den Ausgangsstand nicht
  messbar (die Datei importiert neue Module), stattdessen je Fall eine Mutante – **18 Mutanten, je 1 failed** (eine
  davon, `UNIT_LABEL` Gramm abweichend, **2 failed** = beide Zählformen), die unveränderte Kontrolle **13 passed**.
  Lücke dabei: „Menge im Shop ohne `count`“ blieb zuerst **12/12 grün**, Fall für `amount` ergänzt → **1 failed**.
  `sprache.spec.ts` um Shop (englische Reiter, Menge „10 min“ am Seed-Angebot `TV-001`), englischen Login mit
  `lang="en"` und das deutsche Vater-Web (Mengen-Zelle „10 Min“ aus `unitAmount`, AK 3) erweitert; vier
  E2E-Mutanten je **1 failed** an der richtigen Zeile. Vitest **494/494**, volle E2E **98 passed** (`exit=0`).
- **2026-10-27** — Review (`frontend-reviewer`): kein Blocker. 🟡 (zählt) **AK 2 nur als Kommentar** – „Kinoabend“
  konnte nie rot werden, weil `t` bei fehlendem Schlüssel den Schlüssel zurückgibt → Titel ist jetzt ein vorhandener
  Schlüssel (`shop.cancelled`), Mutante „Titel durch `t`“ → **1 failed**. 🟡 (zählt) `lang="de"` nach `vaterLogin` ist
  nach `page.goto` immer wahr → gestrichen (der echte Beleg ist der Vitest-Fall beim Entmontieren). 🟢 ohne Zählung:
  deutsche Schluss-Anführungszeichen wieder wie vorher (ASCII), `skinText` von `sohn/` nach `lib/` (sonst hinge
  `components/` an `sohn/`), Kommentar nennt die richtige Testdatei, Einheiten-Abgleich über alle fünf Einheiten × {1, 2}
  gegen `unitAmount`, `|Lade Shop` aus der Negativ-Regex, vier englische Formulierungen geglättet;
  `errorMessage` bleibt deutsch → [B-314](B-314-fehlermeldungen-der-arcade-bleiben-deutsch.md). **Zähler: 2.**
- **2026-10-27** — Rollengang: der Sohn-Gang ist `sprache.spec.ts` im echten Browser gegen frisch gestartete Server
  (umschalten, neu laden, Shop, Skins, abmelden, englischer Login, Vater-Shop, wieder anmelden); Vater und Creator
  Regressionszeugen – volle E2E grün, der Vater-Pfad im Diff nur als Leser der unveränderten Helfer (AK 3 belegt).
  Chrome-Extension nicht genutzt; die Spec ist der Gang. **`abgenommen`**, `wartet_auf` das menschliche Gegenlesen
  (AK 5). Zum Gegenlesen besonders: `progress.title` „Trophy path“, `pointKind.*`, `interests.*Hint`.
