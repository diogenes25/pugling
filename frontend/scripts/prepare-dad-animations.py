"""Builds the transparent one-shot/loop WebP clips for the extra Learning Dad actions
(punch, boxing-win, angry) from character/dad/animations/*.mp4 into public/character/dad.

Why a separate script from prepare-dad-assets.py: these clips come from another source
folder, are 16:9 (boxing) and need cropping, and the punch clips play ONCE (WebP loop=1)
instead of ping-pong looping.
"""
import os
import subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "character", "dad", "animations")
OUT = os.path.join(ROOT, "public", "character", "dad")


def run(args):
    subprocess.run(["ffmpeg", "-v", "error", "-y", *args], check=True)


def webp(name, src, vf, loop, trim=None, pingpong=False):
    pre = [] if trim is None else ["-ss", str(trim[0]), "-to", str(trim[1])]
    if pingpong:
        fc = f"[0:v]{vf}[p];[p]split[f][t];[t]reverse[r];[f][r]concat=n=2:v=1[out]"
    else:
        fc = f"[0:v]{vf}[out]"
    run([*pre, "-i", os.path.join(SRC, src), "-filter_complex", fc, "-map", "[out]",
         "-loop", str(loop), "-lossless", "0", "-q:v", "70", os.path.join(OUT, f"{name}.webp")])
    # Still = a representative mid frame (also the reduced-motion image).
    run([*pre, "-i", os.path.join(SRC, src), "-vf", rf"{vf},select=eq(n\,{STILL[name]})",
         "-frames:v", "1", os.path.join(OUT, f"{name}_still.png")])


# Frame index (after fps=15) of the still: the moment of impact / peak anger.
STILL = {"dad_punch": 7, "dad_boxing_win": 20, "dad_angry": 8}

key = "colorkey=0xfdfdfd:0.14:0.08"
os.makedirs(OUT, exist_ok=True)

# Jab + cross, 2.5 s, plays once.
webp("dad_punch", "Dad_boxt_JapCross.mp4",
     f"crop=780:700:300:10,{key},scale=320:-1,fps=15", loop=1)
# Shadow-boxing win sequence, first 4.5 s of the 8 s clip (the rest is idle stance), plays once.
webp("dad_boxing_win", "Dad_boxt_win.mp4",
     f"crop=780:700:300:10,{key},scale=320:-1,fps=15", loop=1, trim=(0, 4.5))
# Growl with raised fists only (the clip's face close-up is skipped: it overlaps the
# PixVerse watermark and does not fit a sprite). Ping-pong loop for a seamless rage.
webp("dad_angry", "Dad_Angry_zoom.mp4",
     f"drawbox=x=270:y=0:w=162:h=60:color=0xfdfdfd:t=fill,{key},scale=280:-1,fps=15",
     loop=0, trim=(2.75, 4.4), pingpong=True)
print("done")


# ---------------------------------------------------------------------------
# Grey-panel clips (become_Angry / become_monster / starts_fight)
# 1920x1080 with a ~600 px portrait panel between white pillarbox bars. The panel
# background is a light grey gradient, so a plain colorkey would eat the white socks and
# leave a halo. Instead the backdrop is modelled per pixel from the FIRST frame (calm pose,
# nothing touches the panel edge) and keyed by distance to that model, with a per-frame
# brightness offset taken from the always-empty top rows.
# ---------------------------------------------------------------------------
import shutil
import tempfile

import numpy as np
from PIL import Image
from scipy import ndimage


FLOOR_FROM = 0.80  # fraction of the frame height from which floor clean-up applies
FLOOR_BLUE_REACH = 5  # px a white sole/sock may sit away from shoe blue


def panel_clip(name, src, fps, loop, still_at):
    tmp = tempfile.mkdtemp(prefix="dad_")
    try:
        run(["-i", os.path.join(SRC, src), "-vf", f"crop=590:1080:665:0,scale=295:-1,fps={fps}",
             os.path.join(tmp, "%04d.png")])
        files = sorted(f for f in os.listdir(tmp) if f.endswith(".png"))
        frames = [np.asarray(Image.open(os.path.join(tmp, f)).convert("RGB"), dtype=np.float32) for f in files]
        h, w, _ = frames[0].shape
        # Backdrop model: per row, left strip -> right strip, linearly across the panel.
        first = frames[0]
        left = np.median(first[:, 3:9], axis=1)
        right = np.median(first[:, -9:-3], axis=1)
        t = np.linspace(0, 1, w)[None, :, None]
        bg0 = left[:, None, :] * (1 - t) + right[:, None, :] * t
        top0 = np.median(first[:30], axis=(0, 1))
        out = []
        for fr in frames:
            offset = np.median(fr[:30], axis=(0, 1)) - top0  # the backdrop breathes slightly per frame
            diff = np.abs(fr - (bg0 + offset)).max(axis=2)
            alpha = np.clip((diff - 12) / 14, 0, 1)
            # The floor under the feet is lighter than the backdrop model and survives as a white
            # smudge that also touches the soles. In the bottom part of the frame a bright, near-neutral
            # pixel therefore only stays when it sits right next to the blue of a shoe or sock stripe
            # (white soles, socks); the monster has no blue, so its floor and smoke patches go entirely.
            blue = (fr[:, :, 2] - fr[:, :, 0]) > 40
            # The monster's floor glows warm, so without any shoe blue the test is looser.
            monster = blue[int(FLOOR_FROM * h):].sum() < 50  # the torn shirt is blue too, so only look at the feet
            neutral = (fr.max(axis=2) - fr.min(axis=2)) < (45 if monster else 16)
            bright = fr.mean(axis=2) > (170 if monster else 200)
            near_blue = ndimage.binary_dilation(blue, iterations=FLOOR_BLUE_REACH)
            floor = np.zeros_like(neutral)
            floor[int(FLOOR_FROM * h):] = True
            alpha[floor & neutral & bright & ~near_blue] = 0
            rgba = np.dstack([fr, alpha * 255]).clip(0, 255).astype(np.uint8)
            out.append(Image.fromarray(rgba, "RGBA"))
        out[0].save(os.path.join(OUT, f"{name}.webp"), save_all=True, append_images=out[1:],
                    duration=round(1000 / fps), loop=loop, quality=70, method=4)
        out[min(still_at, len(out) - 1)].save(os.path.join(OUT, f"{name}_still.png"))
        print(name, len(out), "frames")
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


panel_clip("dad_become_angry", "Dad_become_Angry.mp4", 15, 1, 60)
panel_clip("dad_become_monster", "Dad_become_monster.mp4", 12, 1, 90)
panel_clip("dad_starts_fight", "Dad_starts_fight.mp4", 12, 1, 60)

# ---------------------------------------------------------------------------
# White-background clips: boxing sequence and applause.
# ---------------------------------------------------------------------------
STILL.update({"dad_boxing_complete": 20, "dad_applause": 20})
webp("dad_boxing_complete", "Dad_boxt_complete.mp4",
     f"crop=780:700:300:10,{key},scale=320:-1,fps=15", loop=1)
# Applause: the PixVerse watermark wanders across the top right (text and a play icon), so
# the whole corner is painted over; the head never reaches x >= 270. Seamless ping-pong loop.
webp("dad_applause", "Learning_Dad_Applause.mp4",
     f"drawbox=x=270:y=0:w=162:h=80:color=0xfdfdfd:t=fill,{key},scale=280:-1,fps=15",
     loop=0, trim=(0.5, 5.0), pingpong=True)
print("done 2")
