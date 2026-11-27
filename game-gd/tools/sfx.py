"""Procedural sound effects for the tower game (own work, no licence strings attached)."""
import sys, wave
import numpy as np

SR = 22050
rng = np.random.default_rng(7)
out = sys.argv[1]


def t(sec):
    return np.arange(int(SR * sec)) / SR


def env(n, attack=0.005, decay=0.2):
    x = np.arange(n) / SR
    a = np.clip(x / attack, 0, 1)
    return a * np.exp(-x / decay)


def lowpass(x, a):
    y = np.empty_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc += a * (v - acc)
        y[i] = acc
    return y


def save(name, x, gain=0.9):
    x = x / (np.max(np.abs(x)) + 1e-9) * gain
    data = (x * 32767).astype(np.int16)
    with wave.open(f"{out}/{name}.wav", "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def tone(freq, sec, shape="sine"):
    ph = 2 * np.pi * np.cumsum(np.broadcast_to(freq, t(sec).shape)) / SR
    if shape == "square":
        return np.sign(np.sin(ph)) * 0.6
    if shape == "tri":
        return 2 / np.pi * np.arcsin(np.sin(ph))
    return np.sin(ph)


# Cannon: low pitch-dropping thump + filtered noise burst.
x = t(0.9)
thump = np.sin(2 * np.pi * np.cumsum(55 + 120 * np.exp(-x / 0.05)) / SR) * env(len(x), 0.002, 0.25)
noise = lowpass(rng.standard_normal(len(x)), 0.08) * env(len(x), 0.001, 0.12) * 3
save("cannon", thump * 1.2 + noise)

# Arrow / bolt from a tower: short twang plus airy whoosh.
x = t(0.35)
twang = tone(220 * np.exp(-x / 0.4), 0.35, "tri") * env(len(x), 0.002, 0.06)
whoosh = lowpass(rng.standard_normal(len(x)), 0.25) * np.sin(np.pi * np.clip(x / 0.35, 0, 1)) * 0.5
save("arrow", twang + whoosh, 0.7)

# Fire arrow (strong tower): arrow plus crackle.
x = t(0.5)
crackle = (rng.random(len(x)) > 0.985) * rng.standard_normal(len(x)) * env(len(x), 0.01, 0.25) * 2
roar = lowpass(rng.standard_normal(len(x)), 0.05) * env(len(x), 0.02, 0.2) * 1.5
save("fire_arrow", np.pad(twang + whoosh, (0, len(x) - len(twang))) + crackle + roar, 0.8)

# Hero punch: dull hit with a quick noise transient.
x = t(0.22)
hit = np.sin(2 * np.pi * np.cumsum(90 + 200 * np.exp(-x / 0.02)) / SR) * env(len(x), 0.001, 0.07)
snap = rng.standard_normal(len(x)) * env(len(x), 0.0005, 0.012)
save("punch", hit + snap * 0.6, 0.75)

# Skeleton hit / defeat: bony clatter.
x = t(0.4)
clatter = sum(np.roll(tone(f, 0.4, "square") * env(len(x), 0.001, 0.03), int(SR * d))
              for f, d in [(1400, 0), (1900, 0.04), (1100, 0.09), (1600, 0.15)])
save("bones", clatter, 0.5)

# Pick up a word: two rising notes.
save("pickup", np.concatenate([tone(880, 0.07) * env(int(SR * 0.07), 0.003, 0.05),
                                tone(1320, 0.12) * env(int(SR * 0.12), 0.003, 0.08)]), 0.6)

# Lay a word down: two falling soft notes.
save("drop", np.concatenate([tone(660, 0.07, "tri") * env(int(SR * 0.07), 0.003, 0.05),
                              tone(440, 0.12, "tri") * env(int(SR * 0.12), 0.003, 0.08)]), 0.5)

# Correct: major arpeggio.
notes = [523.25, 659.25, 783.99, 1046.5]
save("correct", np.concatenate([tone(f, 0.11, "tri") * env(int(SR * 0.11), 0.004, 0.09) for f in notes[:-1]]
                               + [tone(notes[-1], 0.35, "tri") * env(int(SR * 0.35), 0.004, 0.2)]), 0.7)

# Wrong: two low descending buzzes.
save("wrong", np.concatenate([tone(196, 0.16, "square") * env(int(SR * 0.16), 0.004, 0.12),
                              tone(147, 0.3, "square") * env(int(SR * 0.3), 0.004, 0.2)]), 0.5)

# Castle hit: low boom with rumble.
x = t(0.7)
save("castle_hit", np.sin(2 * np.pi * np.cumsum(40 + 60 * np.exp(-x / 0.08)) / SR) * env(len(x), 0.002, 0.3)
     + lowpass(rng.standard_normal(len(x)), 0.03) * env(len(x), 0.002, 0.35) * 4, 0.8)

# Victory fanfare.
fan = [(523.25, 0.14), (523.25, 0.14), (523.25, 0.14), (659.25, 0.3), (587.33, 0.14), (659.25, 0.14), (783.99, 0.6)]
save("victory", np.concatenate([(tone(f, d, "square") * 0.4 + tone(f * 2, d, "tri") * 0.3) * env(int(SR * d), 0.005, d * 0.8)
                                for f, d in fan]), 0.7)

# Defeat: slow falling line.
save("defeat", np.concatenate([tone(f, 0.28, "tri") * env(int(SR * 0.28), 0.01, 0.22) for f in [392, 349.23, 311.13, 261.63]]), 0.6)

# UI tap.
save("tap", tone(1000, 0.04) * env(int(SR * 0.04), 0.001, 0.015), 0.4)
print("ok")
