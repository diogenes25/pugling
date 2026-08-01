"""Zeilengenaues Ersetzen von //-Kommentaren in C#-Quelldateien.

    python .claude/scripts/patch-comments.py <patch.jsonl>

Warum überhaupt: Kommentare stehen *zwischen* Code. Lässt man ein Modell oder ein
Suchen-und-Ersetzen ganze Dateien umschreiben, ist eine versehentlich mitgeänderte
Zeile im Diff kaum zu sehen und der Build fängt sie nicht – ein getauschter Operand
kompiliert. Dieses Werkzeug dreht die Beweislast um: es fasst **nur** Zeilen an, die
getrimmt mit "//" beginnen, und bricht bei jedem Verstoß ab, ohne zu schreiben.
Entstanden bei der Umstellung der Code-Doku auf Englisch (docs/translate.md,
Etappen 8/9), taugt aber für jeden Kommentar-Umbau in Serie.

Eingabe: JSONL, ein Objekt je Zeile:
  {"f": "<pfad>", "l": <zeile 1-basiert>, "n": "<neue Zeile ohne Zeilenende>"}
  {"f": "<pfad>", "l": <zeile>, "d": true}                 -> Zeile löschen (Kürzen)
  {"f": "<pfad>", "l": <zeile>, "n": "...", "t": true}     -> Kommentar am Zeilenende
  {"f": "<pfad>", "l": <zeile>, "lab": "Neues Label"}      -> Trenner "// ── Label ──"

Sicherungen:
  * Voll-Zeilen-Ersatz: die alte Zeile MUSS getrimmt mit "//" beginnen, die neue auch.
  * Trailing ("t"): der Code-Teil VOR dem ersten "//" muss zeichengenau gleich bleiben.
  * Trenner ("lab"): Strichläufe und Einrückung bleiben, nur das Label wird ersetzt.
  * Zeilenenden (CRLF/LF) und BOM je Datei bleiben erhalten.
  * Alles oder nichts je Lauf; keine Zeile darf doppelt adressiert sein.

Gegenprobe danach (die eigentliche Zusicherung): streicht man aus beiden Ständen alle
Voll-Zeilen-Kommentare und schneidet jede übrige Zeile am ersten "//" ab, muss das
Ergebnis byte-identisch sein.
"""
import json
import re
import sys
from collections import defaultdict

# Trenner-Zeile: fuehrender Strichlauf, Label, optionaler abschliessender Strichlauf.
SEP = re.compile(r"^(\s*//\s*[-─═]{2,}\s*)(.*?)((?:\s*[-─═]{2,})?\s*)$")

patch_file = sys.argv[1]
entries = defaultdict(list)
with open(patch_file, encoding="utf-8") as fh:
    for raw in fh:
        raw = raw.strip()
        if not raw or raw.startswith("#"):
            continue
        e = json.loads(raw)
        entries[e["f"]].append(e)

problems = []
planned = {}
for path, items in entries.items():
    data = open(path, "rb").read()
    bom = data.startswith(b"\xef\xbb\xbf")
    text = data.decode("utf-8-sig")
    crlf = "\r\n" in text
    lines = text.replace("\r\n", "\n").split("\n")

    seen = set()
    for e in items:
        idx = e["l"] - 1
        if idx in seen:
            problems.append(f"{path}:{e['l']} doppelt adressiert")
        seen.add(idx)
        if idx < 0 or idx >= len(lines):
            problems.append(f"{path}:{e['l']} ausserhalb der Datei")
            continue
        old = lines[idx]
        trailing = e.get("t", False)
        if "lab" in e:
            m = SEP.match(old)
            if not m:
                problems.append(f"{path}:{e['l']} keine Trenner-Zeile: {old!r}")
            else:
                # Strichlaeufe und Einrueckung bleiben, nur das Label wird ersetzt.
                e["n"] = m.group(1) + e["lab"] + m.group(3)
        elif trailing:
            if "//" not in old or old.lstrip().startswith("//"):
                problems.append(f"{path}:{e['l']} kein Trailing-Kommentar: {old!r}")
            elif "n" in e:
                if "//" not in e["n"]:
                    problems.append(f"{path}:{e['l']} Ersatz ohne //: {e['n']!r}")
                # Der Code-Teil VOR dem ersten // muss zeichengenau gleich bleiben.
                elif old.split("//", 1)[0] != e["n"].split("//", 1)[0]:
                    problems.append(
                        f"{path}:{e['l']} Code-Teil veraendert:\n"
                        f"      alt: {old.split('//', 1)[0]!r}\n"
                        f"      neu: {e['n'].split('//', 1)[0]!r}")
        else:
            if not old.lstrip().startswith("//"):
                problems.append(f"{path}:{e['l']} keine Kommentarzeile: {old!r}")
            if "n" in e and not e["n"].lstrip().startswith("//") and e["n"].strip():
                problems.append(f"{path}:{e['l']} Ersatz ist keine Kommentarzeile: {e['n']!r}")

    for e in sorted(items, key=lambda x: -x["l"]):
        idx = e["l"] - 1
        if idx < 0 or idx >= len(lines):
            continue
        if e.get("d"):
            del lines[idx]
        else:
            lines[idx] = e["n"]

    out = "\n".join(lines)
    if crlf:
        out = out.replace("\n", "\r\n")
    planned[path] = (b"\xef\xbb\xbf" if bom else b"") + out.encode("utf-8")

if problems:
    print("ABBRUCH, nichts geschrieben:")
    for p in problems[:40]:
        print("  " + p)
    print(f"  ({len(problems)} Befunde)")
    sys.exit(1)

for path, blob in planned.items():
    open(path, "wb").write(blob)
print(f"OK: {len(planned)} Dateien, {sum(len(v) for v in entries.values())} Zeilen angefasst")
