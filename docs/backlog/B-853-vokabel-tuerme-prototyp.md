---
tags: [typ/story, status/idee]
aliases: []
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-822
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-853 · Vokabel-Türme: spielbarer Prototyp mit einer Karte und fünf Türmen

Teil 4 von 4 aus B-822, setzt B-850 und B-851 voraus. Eine Karte, 5 Türme, eine Monsterart,
Zwei-Daumen-Steuerung, Sammeln mit Auswahlleiste, Zuweisen per Ja/Nein, Level-Ende nach 10 richtigen
Zuweisungen; Platzhalter-Grafik. Abnahme: ein Sohn spielt ein Level an der echten Instanz.
Entscheidungen und Akzeptanzkriterien: [B-822](B-822-vokabel-tuerme-natives-spiel.md); Spielidee und
Technik: [turm-spiel-plan.md](../turm-spiel-plan.md).

## Prototyp im Emulator (2026-11-23)

Spielbar im Android-Emulator (AVD `pl-test`, x86_64) gegen eine **Kopie** der echten DB mit Friedrich (Kind 3):
Login → Positionswahl (seine Englisch-Units 1 A/B/C und Französisch) → Level → Ergebnis. Gemessen: Türme zeigen
seine echten Bedeutungen, Monster droppen Tokens, Sammeln über die Auswahlleiste, Zuweisen per Ja/Nein,
`wet → Nässe` vom Server als richtig bewertet, Turm aufmunitioniert mit neuer Bedeutung; Basis fällt → Ergebnis.

- Code: [game/Scripts/](../../game/Scripts/) – `Level.cs` (Karte, Türme, Monster, Zwei-Daumen-Steuerung,
  Platzhalter-Grafik in Code), `GameRoot.cs` (Bildschirmfolge), `Net/PuglingApi.cs`, `Core/TokenPool.cs`.
- **Renderer `gl_compatibility`:** Vulkan präsentiert im Emulator nicht (`VkResult error 5`); für ein 2D-Spiel
  ist OpenGL ES 3 ohnehin die breitere Wahl.
- Zwei Export-Presets: `Android` (arm64, Handy, [handy-installieren.ps1](../../game/tools/handy-installieren.ps1))
  und `Android-Emulator` (x86_64, [emulator-installieren.ps1](../../game/tools/emulator-installieren.ps1)) – ein
  APK mit beiden passt nicht in den AVD-Speicher.

**Offen für die Abnahme:** Friedrich spielt ein Level (auf dem Handy, an der echten Instanz – dafür muss der
Heimnetz-Server neu gebaut werden). Balance ist nur grob gestellt (Start 10 Schuss, Monster 2 HP, Spawn 4,5→2,4 s);
Tokens fallen zufällig, weil der Client die Zuordnung nicht kennt – ob die Trefferchance (~5 von 12) reicht,
zeigt erst der Rollengang. Engine-freie Logik (`TokenPool`) ist noch ohne Unit-Test; `game/` nicht in CI.

## Verlauf

- 2026-11-22 · `idee` angelegt beim Teilen von B-822 (Entscheidung 8).
- 2026-11-23 · Prototyp gebaut und im Emulator gegen eine DB-Kopie mit Friedrich gespielt (vom Nutzer beauftragt). Abnahme durch einen Sohn steht aus; Status bewusst unverändert.
