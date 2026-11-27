---
tags: [typ/story, status/idee, bereich/turmspiel, rolle/student]
aliases: [Animierter Dad im Turmspiel]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: "game"
migration: ""
vertragsbruch: ""
quelle: "Rückmeldung des Vaters zum Turmspiel-Prototyp, 2026-11-27 (docs/turm-spiel-plan.md → Learning Dad, Sound, Katapult)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-1035 · Turmspiel: ein eigener, komplett animierter Learning Dad als Held

Der Vater möchte im Turmspiel später einen **eigenen, voll animierten Learning Dad** als Spielfigur statt des
KayKit-Ritters.

**Was schon ausprobiert und verworfen ist:** Am 2026-11-27 lief Learning Dad als flache Figur in der 3D-Welt,
die immer zur Kamera schaut. Die Bilder kamen aus den App-Clips (Boxschlag, Daumen hoch, Facepalm, Box-Sieg,
wütend). Das Ergebnis gefiel weniger als der Ritter, deshalb wurde es wieder ausgebaut:

- Es gab keinen Laufzyklus; die Figur hüpfte in Boxhaltung.
- Aus der Draufsicht wirkte sie flach.

**Gebraucht wird darum ein echtes 3D-Modell** mit Skelett und Clips:

- Bewegungen: Stehen, Laufen, Aufheben, Geben, Schlagen/Schießen, Jubel, Enttäuschung, Niederlage
- Stil passend zum KayKit-Look (Low-Poly)
- Format: glTF/glb

**Andockstelle:** `IHeroLook` (`game/Scripts/Heroes/`). Eine neue Klasse nach dem Muster von `KnightLook`
genügt; Position, Tragen und Kamera bleiben in `Hero`.

**Offen:**

- Woher kommt das Modell (selbst modellieren, beauftragen, KI-Generator mit Auto-Rigging)?
- Unter welcher Lizenz steht es?
- Soll es dem App-Dad (blaues Poloshirt, blonde Haare) ähneln?

## Verlauf

- **2026-11-27** — Idee aufgenommen, nachdem der Billboard-Dad im Spiel gegen den Ritter verlor (Commit siehe `git log -- game/Scripts/Heroes`).
