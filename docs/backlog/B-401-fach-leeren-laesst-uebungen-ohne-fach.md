---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Fach leeren trotz Übungen]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-401 · Eine Reihe mit Übungen darf ihr Fach leeren, obwohl eine Reihe ohne Fach keine Übung tragen darf

B-106 T-01 verweigert das **Anlegen** einer Übung in einer Reihe ohne Fach (`series_without_subject`). Ein `PATCH` der Reihe mit
`clearSubject: true` geht aber durch, solange keine Übung Themen oder Kategorien des alten Fachs trägt (seit B-291 geprüft) – danach
stehen Übungen in einer Reihe, die keine tragen dürfte. Zu klären beim Ausformulieren: dieselbe 409 wie B-291 (dann zählt jede Übung),
oder ist eine fachlose Reihe mit Bestand ein zulässiger Zwischenzustand?

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** `clearSubject` auf einer Reihe mit **irgendeiner** Übung ist dieselbe `409 series_subject_in_use` wie
   B-291 – eine fachlose Reihe darf keine Übung tragen (B-106 T-01), also ist jede Übung ein Träger des Fachs. Ein Wechsel auf ein
   **anderes** Fach bleibt nur an Themen/Kategorien gebunden. Die Meldung nennt beim Leeren den anderen Grund. *Kosten:* wer eine Reihe
   fachlos machen will, muss ihre Übungen erst löschen (einen Verschiebe-Weg gibt es nicht) oder auf ein anderes Fach wechseln.
   **Bekannte Grenze:** das Löschen eines Fachs erzeugt denselben Zustand weiter → [B-404](B-404-fach-loeschen-laesst-reihe-mit-uebungen-fachlos.md).

## Akzeptanzkriterien

| Fall | Erwartet | Beleg | Mutante |
|---|---|---|---|
| Reihe mit Übung (ohne Themen), `clearSubject` | 409, „1 exercise(s)", Fach bleibt | `ReiheFachwechselTests.Fach_Leeren_Mit_Uebungen_Wird_Gesperrt` | `clearing`-Klausel → `false` bzw. Argument → `false` – gemessen: je rot nur dieser Fall |
| Reihe ohne Zuordnungen, anderes Fach | 200 | `Ohne_Zuordnungen_Oder_Ohne_Wechsel_Geht_Es_Durch` | **Gegenprobe** |

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-291 (Lauf Z, Sprint 14; `pugling-reviewer`, 🟢). Am Code gelesen, nicht gemessen.
- **2026-11-05** — gegrillt, geschätzt und gebaut in Lauf Z, Sprint 15. **Gemessen** vor dem Fix: `200` statt `409` (Defekt bestätigt).
  Mutanten je neuer Bedingung per Skript mit Build-Prüfung (Tabelle). Volle Suite 1115/1115.
- **2026-11-05** — Review `pugling-reviewer`: 🟡 die Meldung versprach „Move or remove", einen Verschiebe-Weg gibt es nicht → Text
  berichtigt („Remove … or switch to another subject"). **Zähler 1 von 5.** 🟡 Geschwisterweg „Fach löschen" erzeugt denselben Zustand →
  B-404 (Frage), hier als Grenze benannt. 🟢 die Meldungs-Variante war ungeprüft → Zusicherung ergänzt, **gemessen:** Zweige vertauscht →
  rot nur `Fach_Leeren_Mit_Uebungen_Wird_Gesperrt`. B-400 ohne Fund.
- **2026-11-05** — **abgenommen.** Rollengang Creator: Integrationsfall gegen den echten Host; der Anzeigeweg der 409 im Bearbeiten-Formular ist derselbe wie in `e2e/reihe-fachwechsel-gesperrt.spec.ts` (B-291) – volle Playwright-Suite 117 passed nach dem letzten Code-Edit. Vater und Sohn Regressionszeugen. Volle Suite 1115/1115. Commit: siehe nächster Eintrag.
