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
   "env": {"NAME": "wert"},            # optional: Umgebung für build und test. Unter Windows läuft der Befehl
                                       # in cmd.exe, das `NAME=wert befehl` nicht kennt (E2E auf Ausweichports)
   "expect": "red" | "green",          # Vorgabe: red
   "fails": ["<Testname-Teil>", ...]}  # optional: diese Namen MÜSSEN in der Ausgabe als
                                       # gefallen stehen – die Probe muss am richtigen Ort fallen.
                                       # Verglichen wird als Teilstring der ganzen Ausgabe: auch die
                                       # Meldung einer Zusicherung zählt, wenn der Runner sie nur beim
                                       # Fehlschlag druckt (Playwright/Vitest `expect(…, "…")`, xUnit
                                       # `Assert.True(x, warum)`); nie ein Text aus Logzeilen (B-494)
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
(`Failed:`/`Fehler:` > 0 bei dotnet, `failed` bei vitest/playwright, `FAILED (failures=N)` bei
Pythons unittest). Ein Exit-Code allein
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
    re.compile(r"^FAILED \((?:failures|errors)=([1-9]\d*)", re.M),  # python unittest
]
PASSED_PATTERNS = [
    re.compile(r"(?:Passed|Bestanden):\s*([1-9]\d*)"),
    re.compile(r"Tests?\s+([1-9]\d*)\s+passed", re.I),
    re.compile(r"^\s*([1-9]\d*)\s+passed", re.I | re.M),
    re.compile(r"^Ran ([1-9]\d*) tests? in [^\n]*\n+OK\b", re.M),  # python unittest
]


def run(cmd, env=None):
    if not cmd:
        return 0, ""
    p = subprocess.run(cmd, shell=True, capture_output=True, text=True,
                       encoding="utf-8", errors="replace",
                       # The probe's own commands run past its lock (playwright.config.ts checks this flag).
                       env={**os.environ, "PUGLING_MUTANT_PROBE": "1", **(env or {})})
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
    """Lines without their ending, plus each line's own ending: the format hook writes touched lines as CRLF into
    an LF file, so one ending per file guessed from the first CRLF splits a mixed file wrongly (measured on B-409)."""
    data = open(path, "rb").read()
    bom = data.startswith(b"\xef\xbb\xbf")
    # Only at "\n" - str.splitlines would also split at U+2028 and friends inside string literals.
    raw = re.findall(r"[^\n]*\n|[^\n]+$", data.decode("utf-8-sig"))
    lines = [r.rstrip("\r\n") for r in raw]
    endings = [r[len(l):] for r, l in zip(raw, lines)]
    return data, bom, endings, lines


def join_lines(lines, endings):
    return "".join(l + e for l, e in zip(lines, endings))


# While a probe runs, the checkout holds mutated sources. Every other build in the same checkout - the test gate,
# a second probe, the E2E server's `dotnet run` - would measure a mutant and report it as a defect of the real code
# (Sprint 82: the gate reported a mutant twice, an E2E server was built with one). Those readers check this file.
LOCK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".mutant-probe.lock")
# A lock older than this is left over from a killed run, not a running one: the longest probe run measured took ~40 min.
LOCK_STALE_SECONDS = 4 * 3600


def lock_held():
    try:
        return time.time() - os.path.getmtime(LOCK) < LOCK_STALE_SECONDS
    except OSError:
        return False


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    sys.stdout.reconfigure(encoding="utf-8")  # the table goes into Markdown; the Windows console default mangles umlauts
    sys.stderr.reconfigure(encoding="utf-8")
    if lock_held():
        sys.exit(f"Eine andere Probe läuft (Sperre {os.path.normpath(LOCK)}) – zwei Proben mutieren dieselben Quellen. "
                 "Abbruch; ist sie verwaist (Lauf abgeschossen), die Datei löschen.")
    open(LOCK, "w", encoding="utf-8").write(f"{os.getpid()} {sys.argv[1]}\n")
    try:
        probe()
    finally:
        os.remove(LOCK)


def probe():
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
        # A mutant equal to its line changes nothing: it would read as "green (abweichend)" and look like a test gap
        # (Sprint 86: a failed string replace built exactly that, and the run cost a full build and test for nothing).
        if m["new"] == m["old"]:
            sys.exit(f"{m['id']}: 'new' ist gleich 'old' – die Mutante ändert nichts ({m['f']}:{m['l']}).")

    # One baseline per distinct (build, test) pair: a list mixing dotnet, vitest and playwright needs each one green.
    baselines = list(dict.fromkeys((m["build"], m["test"], json.dumps(m.get("env") or {}, sort_keys=True))
                                   for m in mutants))
    for build, test, env in baselines:
        env = json.loads(env)
        print(f"Grundlinie: {test}", file=sys.stderr)
        code, out = run(build, env)
        if code != 0:
            sys.exit("Grundlinie kompiliert nicht – Abbruch.\n" + out[-3000:])
        if verdict(*run(test, env)) != "green":
            sys.exit(f"Grundlinie ist nicht grün ({test}) – keine Probe misst dann etwas. Abbruch.")

    measured_at = stand()
    rows = []
    for m in mutants:
        original, bom, endings, lines = split_lines(m["f"])
        lines[m["l"] - 1] = m["new"]
        mutated = join_lines(lines, endings).encode("utf-8")
        if bom:
            mutated = b"\xef\xbb\xbf" + mutated
        expect = m.get("expect", "red")
        print(f"{m['id']}: {m['f']}:{m['l']}", file=sys.stderr)
        try:
            open(m["f"], "wb").write(mutated)
            code, out = run(m["build"], m.get("env"))
            if code != 0:
                got, note = "ungemessen", "kompiliert nicht"
            else:
                code, out = run(m["test"], m.get("env"))
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

    final = "green"
    for build, test, env in baselines:
        env = json.loads(env)
        code, out = run(build, env)
        got = verdict(*run(test, env)) if code == 0 else "kompiliert nicht"
        if got != "green":
            final = f"{got} ({test})"
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
