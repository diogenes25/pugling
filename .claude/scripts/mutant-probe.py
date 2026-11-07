"""Mutanten-Probe in einem Schritt: anwenden -> bauen -> testen -> zurückspielen -> neu bauen.

    python -I .claude/scripts/mutant-probe.py <mutanten.jsonl>

Warum überhaupt (Retro-Vorschlag (BL), entschieden 2026-11-07): von Hand ging die Probe
viermal still falsch – ein Mutant, der nicht kompiliert, ließ `--no-build` gegen die alte
Assembly laufen (Lauf V); ein per `mv` zurückgespielter Stand hatte die alte Zeit, der
Build war ein No-op und die nächste Messung trug noch die Probe; `\\n`-Muster griffen in
CRLF-Dateien nicht; Tabellenzeilen standen als „gemessen" ohne Lauf (B-399, B-402).
Das Skript macht jede dieser Stellen zur Abbruchbedingung statt zur Disziplin.

Eingabe: JSONL, ein Objekt je Mutante (Zeilen mit # sind Kommentare):
  {"id": "M1", "f": "<pfad>", "l": <zeile 1-basiert>,
   "old": "<alte Zeile, zeichengenau>", "new": "<mutierte Zeile>",
   "build": "<befehl>", "test": "<befehl>",
   "expect": "red" | "green",          # Vorgabe: red
   "fails": ["<Testname-Teil>", ...]}  # optional: diese Namen MÜSSEN in der Ausgabe als
                                       # gefallen stehen – die Probe muss am richtigen Ort fallen
Ein Objekt nur mit "build"/"test" (ohne "f") setzt die Vorgaben für alle folgenden Zeilen.

Ablauf:
  1. Grundlinie: bauen + testen ohne Mutante. Rot -> Abbruch (dann misst keine Probe etwas).
  2. Je Mutante: `old` muss zeichengenau stimmen (sonst Abbruch, nichts geschrieben); Zeile
     ersetzen (CRLF/LF und BOM bleiben); bauen – Build-Fehler heißt `ungemessen
     (kompiliert nicht)`, NIE `rot`; testen; Datei byteweise zurückschreiben, mtime auf jetzt.
  3. Zum Schluss neu bauen und die Grundlinie noch einmal testen – sie muss grün sein,
     sonst ist die Wiederherstellung selbst kaputt und die ganze Tabelle ungültig.

Ausgabe: eine Markdown-Tabelle zum Einfügen in `## Verlauf`, mit dem Stand (Commit +
„geändert", falls der Arbeitsbaum abweicht), an dem gemessen wurde. Eine Zeile `ungemessen`
oder `abweichend` ist kein Beleg (Regel in pm-loop Step 5).

Rot heißt: Testbefehl endet != 0 UND die Ausgabe nennt mindestens einen gefallenen Test
(`Failed:`/`Fehler:` > 0 bei dotnet, `failed` bei vitest/playwright). Ein Exit-Code allein
zählt nicht – Playwright endet auch mit 0, wenn null Tests liefen.
"""
import json
import os
import re
import subprocess
import sys
import time

FAILED_PATTERNS = [
    re.compile(r"(?:Failed|Fehler):\s*([1-9]\d*)"),           # dotnet test
    re.compile(r"Tests?\s+([1-9]\d*)\s+failed", re.I),       # vitest
    re.compile(r"^\s*([1-9]\d*)\s+failed", re.I | re.M),     # playwright
]
PASSED_PATTERNS = [
    re.compile(r"(?:Passed|Bestanden):\s*([1-9]\d*)"),
    re.compile(r"Tests?\s+([1-9]\d*)\s+passed", re.I),
    re.compile(r"^\s*([1-9]\d*)\s+passed", re.I | re.M),
]


def run(cmd):
    if not cmd:
        return 0, ""
    p = subprocess.run(cmd, shell=True, capture_output=True, text=True,
                       encoding="utf-8", errors="replace")
    return p.returncode, p.stdout + p.stderr


def verdict(code, out):
    failed = any(rx.search(out) for rx in FAILED_PATTERNS)
    passed = any(rx.search(out) for rx in PASSED_PATTERNS)
    if code != 0 and failed:
        return "red"
    if code == 0 and passed and not failed:
        return "green"
    return "unklar"  # e.g. zero tests ran, or the runner crashed before reporting


def stand():
    _, head = run("git rev-parse --short HEAD")
    _, dirty = run("git status --porcelain --untracked-files=no")
    return head.strip() + (" + Arbeitsbaum geändert" if dirty.strip() else "")


def load(path):
    defaults, mutants = {}, []
    for n, raw in enumerate(open(path, encoding="utf-8"), 1):
        raw = raw.strip()
        if not raw or raw.startswith("#"):
            continue
        e = json.loads(raw)
        if "f" not in e:
            defaults.update(e)
            continue
        m = {**defaults, **e}
        for k in ("id", "f", "l", "old", "new", "build", "test"):
            if k not in m:
                sys.exit(f"Zeile {n}: Feld '{k}' fehlt")
        mutants.append(m)
    return defaults, mutants


def split_lines(path):
    data = open(path, "rb").read()
    bom = data.startswith(b"\xef\xbb\xbf")
    text = data.decode("utf-8-sig")
    nl = "\r\n" if "\r\n" in text else "\n"
    return data, bom, nl, text.split(nl)


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    sys.stdout.reconfigure(encoding="utf-8")  # the table goes into Markdown; the Windows console default mangles umlauts
    sys.stderr.reconfigure(encoding="utf-8")
    defaults, mutants = load(sys.argv[1])
    if not mutants:
        sys.exit("Keine Mutanten in der Eingabe.")

    # Check every 'old' line up front: a typo must not leave a half-run table behind.
    for m in mutants:
        _, _, _, lines = split_lines(m["f"])
        actual = lines[m["l"] - 1] if 0 < m["l"] <= len(lines) else None
        if actual != m["old"]:
            sys.exit(f"{m['id']}: Zeile {m['f']}:{m['l']} stimmt nicht.\n"
                     f"  erwartet: {m['old']!r}\n  gefunden: {actual!r}")

    baseline_build, baseline_test = mutants[0]["build"], mutants[0]["test"]
    print(f"Grundlinie: {baseline_build}", file=sys.stderr)
    code, out = run(baseline_build)
    if code != 0:
        sys.exit("Grundlinie kompiliert nicht – Abbruch.\n" + out[-3000:])
    if verdict(*run(baseline_test)) != "green":
        sys.exit("Grundlinie ist nicht grün – keine Probe misst dann etwas. Abbruch.")

    measured_at = stand()
    rows = []
    for m in mutants:
        original, bom, nl, lines = split_lines(m["f"])
        lines[m["l"] - 1] = m["new"]
        mutated = nl.join(lines).encode("utf-8")
        if bom:
            mutated = b"\xef\xbb\xbf" + mutated
        expect = m.get("expect", "red")
        print(f"{m['id']}: {m['f']}:{m['l']}", file=sys.stderr)
        try:
            open(m["f"], "wb").write(mutated)
            code, out = run(m["build"])
            if code != 0:
                got, note = "ungemessen", "kompiliert nicht"
            else:
                code, out = run(m["test"])
                got = verdict(code, out)
                note = ""
                missing = [t for t in m.get("fails", []) if t not in out]
                if got == "red" and missing:
                    note = "fällt am falschen Ort, fehlt: " + ", ".join(missing)
                    got = "abweichend"
        finally:
            open(m["f"], "wb").write(original)
            now = time.time()
            os.utime(m["f"], (now, now))  # a restored file with its old mtime makes the next build a no-op
        ok = got == expect
        rows.append((m["id"], f"{m['f']}:{m['l']}", expect,
                     got if ok or got in ("ungemessen", "abweichend", "unklar") else f"{got} (abweichend)",
                     note))

    code, out = run(baseline_build)
    final = verdict(*run(baseline_test)) if code == 0 else "kompiliert nicht"
    print(f"\nGemessen an `{measured_at}`, {time.strftime('%Y-%m-%d %H:%M')}.\n")
    print("| Mutante | Stelle | erwartet | gemessen | Anmerkung |")
    print("|---|---|---|---|---|")
    for r in rows:
        print("| " + " | ".join(r) + " |")
    print(f"\nGrundlinie nach dem Zurückspielen: {final}.")
    bad = [r for r in rows if r[2] != r[3]]
    if final != "green":
        print("UNGÜLTIG: die Wiederherstellung ist nicht grün – Tabelle verwerfen.")
        sys.exit(2)
    if bad:
        print(f"{len(bad)} Zeile(n) ohne Beleg: " + ", ".join(r[0] for r in bad))
        sys.exit(1)


if __name__ == "__main__":
    main()
