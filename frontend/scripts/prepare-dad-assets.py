import os
import subprocess
from PIL import Image


def main():
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    src_dir = os.path.join(root, "character", "dad", "images")
    out_dir = os.path.join(root, "public", "character", "dad")
    os.makedirs(out_dir, exist_ok=True)

    print(f"Source dir: {src_dir}")
    print(f"Output dir: {out_dir}")

    # ---------------------------------------------------------------------------
    # Wasserzeichen-Filter: übermalt "Pixelverse"-Schriftzug oben rechts
    # mit der jeweiligen Hintergrundfarbe des Clips, bevor Transparenz erzeugt wird.
    # ---------------------------------------------------------------------------
    del_wm_thumbs    = "drawbox=x=275:y=15:w=155:h=50:color=0xfdfdfd:t=fill"
    del_wm_facepalm  = "drawbox=x=280:y=15:w=150:h=50:color=0xfdfcfd:t=fill"
    # facepalm_2: post-YUV-Grau (168,168,168) = 0xa8a8a8
    del_wm_facepalm2 = "drawbox=x=295:y=5:w=185:h=75:color=0xa8a8a8:t=fill"
    # clock: post-YUV-Schwarz (14,14,14) = 0x0e0e0e
    del_wm_clock     = "drawbox=x=300:y=0:w=180:h=80:color=0x0e0e0e:t=fill"
    del_wm_thumbs_img = "drawbox=x=1200:y=20:w=224:h=60:color=0xfdfdfd:t=fill"

    # ---------------------------------------------------------------------------
    # Palindrom-Loop-Hilfsfunktion:
    # Erstellt einen filter_complex-String, der die gefilterten Frames vorwärts
    # und rückwärts verkettet → nahtloser Ping-Pong-Loop ohne Sprung.
    # Aufbau: [0:v] → vfilter → split → [fwd][tmp] → [tmp]reverse → [rev]
    #         → [fwd][rev]concat=n=2:v=1 → [out]
    # ---------------------------------------------------------------------------
    def pingpong_complex(vfilter: str) -> str:
        return (
            f"[0:v]{vfilter}[proc];"
            f"[proc]split[fwd][tmp];"
            f"[tmp]reverse[rev];"
            f"[fwd][rev]concat=n=2:v=1[out]"
        )

    # ---------------------------------------------------------------------------
    # 1. Daumen hoch 1 – animiertes WebP transparent, Ping-Pong-Loop
    # Vorwärts (Hände hoch) + Rückwärts (Hände runter) = nahtlose Dauerbewegung
    # ---------------------------------------------------------------------------
    fc1 = pingpong_complex(f"{del_wm_thumbs},colorkey=0xfefefe:0.08:0.08,scale=280:-1,fps=15")
    cmd1 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Thumbs_Up_1.mp4"),
        "-filter_complex", fc1, "-map", "[out]",
        "-loop", "0",
        os.path.join(out_dir, "dad_thumbs_up.webp")
    ]
    subprocess.run(cmd1, check=True)
    print("Generated dad_thumbs_up.webp (ping-pong)")

    # ---------------------------------------------------------------------------
    # 2. Daumen hoch 2 – animiertes WebP transparent, Ping-Pong-Loop
    # ---------------------------------------------------------------------------
    fc2 = pingpong_complex(f"{del_wm_thumbs},colorkey=0xfefefe:0.08:0.08,scale=280:-1,fps=15")
    cmd2 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Thumbs_Up_2.mp4"),
        "-filter_complex", fc2, "-map", "[out]",
        "-loop", "0",
        os.path.join(out_dir, "dad_thumbs_up_alt.webp")
    ]
    subprocess.run(cmd2, check=True)
    print("Generated dad_thumbs_up_alt.webp (ping-pong)")

    # ---------------------------------------------------------------------------
    # 3. Facepalm – animiertes WebP transparent, Ping-Pong-Loop
    # ---------------------------------------------------------------------------
    fc3 = pingpong_complex(f"{del_wm_facepalm},colorkey=0xfefefe:0.10:0.08,scale=280:-1,fps=15")
    cmd3 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Dad_does_a_facepalm.mp4"),
        "-filter_complex", fc3, "-map", "[out]",
        "-loop", "0",
        os.path.join(out_dir, "dad_facepalm.webp")
    ]
    subprocess.run(cmd3, check=True)
    print("Generated dad_facepalm.webp (ping-pong)")

    # ---------------------------------------------------------------------------
    # 4. Facepalm 2 – animiertes WebP transparent, Ping-Pong-Loop
    # ---------------------------------------------------------------------------
    fc4 = pingpong_complex(f"{del_wm_facepalm2},colorkey=0xa8a8a8:0.20:0.15,scale=280:-1,fps=15")
    cmd4 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Dad_does_a_facepalm_2.mp4"),
        "-filter_complex", fc4, "-map", "[out]",
        "-loop", "0",
        os.path.join(out_dir, "dad_facepalm_2.webp")
    ]
    subprocess.run(cmd4, check=True)
    print("Generated dad_facepalm_2.webp (ping-pong)")

    # ---------------------------------------------------------------------------
    # 5. Uhr – animiertes WebP transparent, Ping-Pong-Loop
    # ---------------------------------------------------------------------------
    fc5 = pingpong_complex(f"{del_wm_clock},colorkey=0x0e0e0e:0.20:0.15,scale=280:-1,fps=15")
    cmd5 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Dad_clock.mp4"),
        "-filter_complex", fc5, "-map", "[out]",
        "-loop", "0",
        os.path.join(out_dir, "dad_clock.webp")
    ]
    subprocess.run(cmd5, check=True)
    print("Generated dad_clock.webp (ping-pong)")

    # ---------------------------------------------------------------------------
    # 6. MP4 für Web optimiert – Ping-Pong-Loop (kein Alpha, h264 faststart)
    # Browser loopen MP4 per JS/CSS; die Datei enthält vorwärts+rückwärts als
    # eine einzige Sequenz, die mit loop-Attribut nahtlos wiederholt wird.
    # ---------------------------------------------------------------------------
    fc6 = pingpong_complex(del_wm_thumbs)
    cmd6 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Thumbs_Up_1.mp4"),
        "-filter_complex", fc6, "-map", "[out]",
        "-c:v", "libx264", "-crf", "22", "-preset", "medium", "-movflags", "+faststart", "-an",
        os.path.join(out_dir, "dad_thumbs_up.mp4")
    ]
    subprocess.run(cmd6, check=True)
    print("Generated dad_thumbs_up.mp4 (ping-pong)")

    fc7 = pingpong_complex(del_wm_facepalm)
    cmd7 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Dad_does_a_facepalm.mp4"),
        "-filter_complex", fc7, "-map", "[out]",
        "-c:v", "libx264", "-crf", "22", "-preset", "medium", "-movflags", "+faststart", "-an",
        os.path.join(out_dir, "dad_facepalm.mp4")
    ]
    subprocess.run(cmd7, check=True)
    print("Generated dad_facepalm.mp4 (ping-pong)")

    fc8 = pingpong_complex(del_wm_clock)
    cmd8 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Dad_clock.mp4"),
        "-filter_complex", fc8, "-map", "[out]",
        "-c:v", "libx264", "-crf", "22", "-preset", "medium", "-movflags", "+faststart", "-an",
        os.path.join(out_dir, "dad_clock.mp4")
    ]
    subprocess.run(cmd8, check=True)
    print("Generated dad_clock.mp4 (ping-pong)")

    # ---------------------------------------------------------------------------
    # 7. WebM mit Alpha-Kanal – Ping-Pong-Loop (libvpx-vp9, yuva420p)
    # ---------------------------------------------------------------------------
    fc9 = pingpong_complex(f"{del_wm_thumbs},colorkey=0xfefefe:0.08:0.08,scale=360:-1")
    cmd9 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Thumbs_Up_1.mp4"),
        "-filter_complex", fc9, "-map", "[out]",
        "-c:v", "libvpx-vp9", "-pix_fmt", "yuva420p", "-an",
        os.path.join(out_dir, "dad_thumbs_up.webm")
    ]
    subprocess.run(cmd9, check=True)
    print("Generated dad_thumbs_up.webm (ping-pong)")

    fc10 = pingpong_complex(f"{del_wm_facepalm},colorkey=0xfefefe:0.10:0.08,scale=360:-1")
    cmd10 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Dad_does_a_facepalm.mp4"),
        "-filter_complex", fc10, "-map", "[out]",
        "-c:v", "libvpx-vp9", "-pix_fmt", "yuva420p", "-an",
        os.path.join(out_dir, "dad_facepalm.webm")
    ]
    subprocess.run(cmd10, check=True)
    print("Generated dad_facepalm.webm (ping-pong)")

    # ---------------------------------------------------------------------------
    # 8. Standbild Daumen hoch (aus Thumbs up.png) – kein Ping-Pong (Einzelbild)
    # ---------------------------------------------------------------------------
    cmd11 = [
        "ffmpeg", "-y", "-i", os.path.join(src_dir, "Thumbs up.png"),
        "-vf", f"{del_wm_thumbs_img},colorkey=0xfefefe:0.06:0.06,scale=360:-1",
        os.path.join(out_dir, "dad_thumbs_up_still.png")
    ]
    subprocess.run(cmd11, check=True)
    print("Generated dad_thumbs_up_still.png (clean)")

    # ---------------------------------------------------------------------------
    # 9. Standbild Facepalm bei t=0.5s – kein Ping-Pong (Einzelbild)
    # ---------------------------------------------------------------------------
    cmd12 = [
        "ffmpeg", "-y", "-ss", "00:00:00.5", "-i", os.path.join(src_dir, "Dad_does_a_facepalm.mp4"),
        "-vf", f"{del_wm_facepalm},colorkey=0xfefefe:0.10:0.08,scale=360:-1",
        "-vframes", "1",
        os.path.join(out_dir, "dad_facepalm_still.png")
    ]
    subprocess.run(cmd12, check=True)
    print("Generated dad_facepalm_still.png (clean)")

    # ---------------------------------------------------------------------------
    # 10. Avatar: Kopf ausschneiden aus Daumen-hoch-Standbild
    # ---------------------------------------------------------------------------
    still = Image.open(os.path.join(out_dir, "dad_thumbs_up_still.png"))
    w, h = still.size
    head = still.crop((w // 2 - 150, 20, w // 2 + 150, 380))
    head.thumbnail((256, 256), Image.Resampling.LANCZOS)
    head.save(os.path.join(out_dir, "dad_avatar.png"))
    print("Generated dad_avatar.png (clean)")

    print("\nAlle Assets mit Ping-Pong-Loop generiert (vorwärts + rückwärts = nahtloser Dauerlauf)!")


if __name__ == "__main__":
    main()
