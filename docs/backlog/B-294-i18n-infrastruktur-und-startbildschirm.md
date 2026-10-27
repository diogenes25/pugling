---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [i18n Teil a, Sprachumschalter der Arcade, Startbildschirm auf Englisch]
status: abgenommen
prio: P3
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-85 (geteilt, NP-11 Schritt 0)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: "Mensch: die englischen Texte in src/i18n/en.ts gegenlesen (AK 7)"
---

# B-294 · i18n-Infrastruktur, Sprachumschalter, `<html lang>` und der Startbildschirm der Arcade auf Englisch

Teil a der geteilten [B-85](B-85-i18n-infrastruktur-sohn-arcade-englisch.md) (Nachprüfung 2026-10-23: „Teilen ist Schritt 0“).
Diese Story liefert die Laufzeit-Infrastruktur und einen **vollständig** übersetzten Bildschirm als Machbarkeitsnachweis;
der Rest des Arcade-Korpus ist [B-295](B-295-arcade-korpus-auf-englisch.md).

## User Story

Als **Kind, das Englisch lernt**, möchte ich meine Arcade auf Englisch stellen können und sehe das sofort auf dem
Startbildschirm, damit die Sprache im Alltag vorkommt – und die Wahl bleibt, wenn ich die App neu öffne.

## Ist-Stand am Code · Entscheidungen

Ist-Stand, Entscheidungen 1–6 und Risiken: unverändert aus B-85 übernommen (dort belegt). Für diesen Teil tragend:
`react-i18next` (Entscheidung 1; der Peer-Konflikt aus B-25 ist erledigt), Wahl im `localStorage` (2), nur Chrome (3),
Typnamen bleiben deutsch (4), `<html lang>` folgt (5), Deutsch in den Specs gepinnt (6).

## Akzeptanzkriterien

1. `react-i18next` + `i18next` eingebunden, `npm install` ohne `--legacy-peer-deps`.
2. Ein Sprachumschalter (Deutsch/Englisch) im HUD der Arcade, Wahl bleibt über `localStorage` erhalten.
3. Der Startbildschirm der Arcade (`SohnHome` samt HUD, Navigation, Missions-Panel, Interessen-Karte) liegt
   vollständig auf Deutsch **und** Englisch vor; ein Vitest hält die Schlüssel-Parität der beiden Sprachen.
4. `<html lang>` wechselt mit der Sprache und fällt beim Verlassen der Arcade auf Deutsch zurück.
5. Nutzerdaten (Plan-Titel, Skin-Name, Missions-Titel) bleiben unübersetzt – als Platzhalter-Werte.
6. Alle bestehenden Playwright-Specs bleiben grün (Deutsch in `playwright.config.ts` gepinnt); eine neue Spec
   schaltet um, prüft Navigation, Überschrift, HUD-Vorlesenamen und `<html lang>`, lädt neu und schaltet zurück.
7. Die englischen Texte liest am Ende ein Mensch gegen (`wartet_auf` nach der Abnahme).

## Schätzung

**M**: Bibliothek + Init + Umschalter + ein Bildschirm (vier Dateien mit Chrome) + Spec-Pinning + eine Spec. Der
Rest des Korpus (B-295) ist der größere Teil und bewusst nicht hier.

## Verlauf

- **2026-10-27** — angelegt beim Teilen von B-85 in Lauf E, Teil 8 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md),
  NP-11 Schritt 0) unter Freigabe 8; direkt `geschaetzt` (Entscheidungen aus B-85) und gebaut: `src/lib/i18n.ts`
  (Init, `LANG_KEY`, `setLanguage`, `storedLanguage`), Ressourcen `src/i18n/de.ts`/`en.ts` (Namespace `sohn`),
  Umschalter-Chip im HUD (`SohnApp.tsx`), `<html lang>`-Effekt mit Rückfall auf Deutsch beim Verlassen, `SohnHome`,
  Navigation, HUD-Vorlesenamen, Missions-Panel und Interessen-Karte über `t()`; Deutsch in `playwright.config.ts`
  gepinnt. Vitest `i18n.test.ts`: Schlüssel-Parität (Mutante: Schlüssel in `en.ts` streichen → rot), kein leerer
  Text, Wahl persistiert, Platzhalter bleiben Nutzerdaten; `sprache.spec.ts` schaltet um, prüft Navigation,
  Überschrift (je nach Seed-Stand Mission/keine), HUD-Label `coins`, `<html lang>`, Neuladen, Rückschalten.
  Build grün, Vitest **467/467**, `sprache` + `karten-umdrehen` + `interessen` **3 passed**.
- **2026-10-27** — Review Frontend (`frontend-reviewer`, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)), Zähler 3:
  **🟡 (1)** Das Missions-Panel war nicht vollständig übersetzt – die Zeitraum-Tabelle `PERIOD_LABEL` („Heute/Diese
  Woche/Ziel“) lag als deutsche Literale neben den Ressourcen; jetzt `missions.periodDaily/Weekly/OneOff` über
  `t()`, die Spec schließt „Diese Woche“ auf Englisch aus. **🟡 (2)** Der sichtbare Text des Sprach-Chips („EN“)
  stand nicht am Anfang seines Vorlesenamens (WCAG 2.5.3 „Label in Name“) – Name jetzt „EN – auf Englisch
  umschalten (Sprache ist Deutsch)“ bzw. „DE – switch to German (language is English)“. **🟡 (3)** Die Schlüssel
  waren untypisiert, ein Tippfehler in `t("…")` wäre erst im Bild aufgefallen – `src/i18n/i18next.d.ts` bindet
  `CustomTypeOptions` an `typeof de`, der Build ist jetzt das Tor. 🟢 ohne Zählung (Freigabe 10): Spec-Locator
  per `hasText` statt `.first()` (unabhängig von der Intro-Karte), `afterEach` setzt die Sprache in `i18n.test.ts`
  zurück, `t` in den Effekt-Abhängigkeiten. Build grün, Vitest **467/467**, `sprache` + `karten-umdrehen` +
  `interessen` **3 passed**, volle E2E **98 passed**.
- **2026-10-27** — Rollengang: Sohn maschinell per `sprache.spec.ts` (umschalten, Navigation, Überschrift, HUD-Label
  `coins`, `<html lang>`, neu laden, zurückschalten); Vater/Creator als Regression über die volle Suite (Vater-Web
  trägt kein `t()`). Kein Browser verbunden, das Gefühl des Umschalters hat niemand gesehen – und die englischen
  Texte liest ein Mensch gegen (AK 7, `wartet_auf`). **`abgenommen`.**
- **2026-10-27** — Commit `1ee9bf0` (Lauf E, Teil 8; nicht gepusht).
