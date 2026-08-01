"""Strenges Zeilen-Ersetzen: die alte Zeile muss ZEICHENGENAU stimmen.

    python .claude/scripts/patch-lines.py <patch.jsonl>

Warum überhaupt: das Schwesterwerkzeug patch-comments.py schützt Code dadurch, dass es
nur Kommentarzeilen anfasst. Manches steht aber *in* einer Code-Zeile – ein Meldungstext
eines Wächters, ein Begründungs-Eintrag in einer Ausnahmeliste. Dort fällt dieser Schutz
weg, also tritt der zweitbeste an seine Stelle: der Aufrufer liefert die alte Zeile mit,
und passt sie nicht auf das Zeichen, wird nichts geschrieben. Ein Suchen-und-Ersetzen
über Textmuster kann das nicht – es trifft irgendwann den Payload-String, den derselbe
Test wieder zurückvergleicht.

Eingabe: JSONL, ein Objekt je Zeile:
  {"f": "<pfad>", "l": <zeile 1-basiert>, "old": "<alte Zeile>", "new": "<neue Zeile>"}

Sicherungen:
  * `old` muss zeichengenau der aktuellen Zeile entsprechen (ohne Zeilenende).
  * Zeilenenden (CRLF/LF) und BOM je Datei bleiben erhalten.
  * Alles oder nichts über den ganzen Lauf.

Die Abgrenzung bleibt Handarbeit und ist die eigentliche Arbeit: eine Assert-Meldung
darf umgestellt werden, ein Payload, den der Test zurückvergleicht, nicht.
"""
import json
import sys
from collections import defaultdict

entries = defaultdict(list)
for raw in open(sys.argv[1], encoding="utf-8"):
    raw = raw.strip()
    if raw and not raw.startswith("#"):
        e = json.loads(raw)
        entries[e["f"]].append(e)

problems, planned = [], {}
for path, items in entries.items():
    data = open(path, "rb").read()
    bom = data.startswith(b"\xef\xbb\xbf")
    text = data.decode("utf-8-sig")
    crlf = "\r\n" in text
    lines = text.replace("\r\n", "\n").split("\n")

    for e in items:
        idx = e["l"] - 1
        if not (0 <= idx < len(lines)):
            problems.append(f"{path}:{e['l']} ausserhalb der Datei")
        elif lines[idx] != e["old"]:
            problems.append(f"{path}:{e['l']} alte Zeile weicht ab\n"
                            f"      erwartet: {e['old']!r}\n"
                            f"      gefunden: {lines[idx]!r}")
        else:
            lines[idx] = e["new"]

    out = "\n".join(lines)
    if crlf:
        out = out.replace("\n", "\r\n")
    planned[path] = (b"\xef\xbb\xbf" if bom else b"") + out.encode("utf-8")

if problems:
    print("ABBRUCH, nichts geschrieben:")
    for p in problems[:30]:
        print("  " + p)
    sys.exit(1)

for path, blob in planned.items():
    open(path, "wb").write(blob)
print(f"OK: {len(planned)} Dateien, {sum(len(v) for v in entries.values())} Zeilen ersetzt")
