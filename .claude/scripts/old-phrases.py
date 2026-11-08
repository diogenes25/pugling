"""Findet Zusicherungen auf Meldungstexte, die gerade umgeschrieben wurden.

    python -I .claude/scripts/old-phrases.py [rev]      # Vorgabe: Arbeitsbaum gegen HEAD

Warum überhaupt (Retro-Vorschlag (CA), gelandet 2026-11-08): in Sprint 21 verlor ein umgeschriebener
deutscher Sperrsatz das Wort „bisherigen", und `e2e/reihe-fachwechsel-gesperrt.spec.ts` suchte genau
diese Wendung. Gesucht hatte ich vorher nur in `frontend/src`; gefunden hat es erst der volle
Playwright-Lauf, sieben Minuten und einen Review später. Dieses Skript macht die Suche zu einem Schritt.

Vorgehen:
  1. `git diff <rev>` über die Dateien, in denen Meldungstexte leben (deutsche Tabelle und i18n im
     Frontend, alle `.cs` unter `backend/Pugling.Api`).
  2. Aus jeder entfernten Zeile die String-Literale; daraus alle Fenster aus vier Wörtern.
  3. Ein Fenster, das in keiner hinzugefügten Zeile mehr vorkommt, ist eine **verschwundene Wendung**.
  4. Gesucht wird sie in Tests, E2E-Specs und handgeschriebener Doku. Treffer = eine Zusicherung oder ein
     Satz, der noch den alten Text erwartet. Ausgabe als `Datei:Zeile`, Exit-Code 1 bei Treffern.

Treffer sind Kandidaten, kein Urteil: ein Kommentar, der den alten Satz bewusst zitiert, eine
Negativ-Zusicherung (`not.toContain`, `DoesNotContain`) oder eine zufällig gleiche Allerweltswendung sind
in Ordnung. Erprobt am 2026-11-08: der nachgestellte Fehler aus Sprint 21 („bisherigen" entfernt) ergibt
genau einen Treffer in der E2E; der ganze Sprint 21 gegen `c910cf8` fand einen echten Rest (ein
Test-Summary zitierte einen entfernten englischen Satz) und drei Kandidaten der genannten Art.

Grenze: es findet Wendungen, keine einzelnen Wörter. Eine Zusicherung wie `/exist|Publisher/` auf eine
englische Rohzeile, die jetzt deutsch übersetzt wird, sieht es nicht – die fängt weiter der Lauf.
"""
import os
import re
import subprocess
import sys

SOURCES = ["frontend/src/lib/api.ts", "frontend/src/i18n", "backend/Pugling.Api"]
TARGETS = ["frontend/src", "frontend/e2e", "backend/Pugling.Api.Tests", "docs/REST", "docs/tutorial-creator.md",
           "docs/tutorial-supervisor.md", "docs/tutorial-student.md", "docs/endpunkt-beziehungen.md"]
TARGET_EXT = (".ts", ".tsx", ".cs", ".http", ".md")
WINDOW = 4
LITERAL = re.compile(r'"((?:[^"\\]|\\.)*)"')


def norm(text):
    # Punctuation off on both sides: "betreust." and "betreust (" are the same word, or a re-punctuated sentence looks gone.
    return " ".join(re.sub(r"[^\w\s-]", " ", text).split())


def windows(text):
    words = norm(text).split()
    return {" ".join(words[i:i + WINDOW]) for i in range(len(words) - WINDOW + 1)}


def literals(line):
    return " ".join(m.group(1) for m in LITERAL.finditer(line))


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    rev = sys.argv[1] if len(sys.argv) > 1 else "HEAD"
    diff = subprocess.run(["git", "diff", "-U0", rev, "--", *SOURCES], capture_output=True, text=True,
                          encoding="utf-8", errors="replace").stdout
    # Joined per side before windowing: a message split over "…" + "…" lines would otherwise lose every window that crosses a
    # line break, and a sentence merely re-wrapped would look "gone" (measured on api.ts in the first trial).
    removed_text, added_text = [], []
    for line in diff.splitlines():
        if line.startswith("---") or line.startswith("+++"):
            continue
        if line.startswith("-"):
            removed_text.append(literals(line[1:]))
        elif line.startswith("+"):
            added_text.append(literals(line[1:]))
    gone = sorted(windows(" ".join(removed_text)) - windows(" ".join(added_text)))
    if not gone:
        print("Keine verschwundenen Wendungen in Meldungstexten.")
        return 0

    files = []
    for t in TARGETS:
        if os.path.isfile(t):
            files.append(t)
        for d, _, fs in os.walk(t):
            if any(p in d for p in ("node_modules", f"{os.sep}bin", f"{os.sep}obj")):
                continue
            files += [os.path.join(d, f) for f in fs if f.endswith(TARGET_EXT) and not f.endswith("contract.ts")]

    hits = []
    for f in sorted(set(files)):
        try:
            lines = open(f, encoding="utf-8-sig").read().splitlines()
        except (UnicodeDecodeError, OSError):
            continue
        for n, l in enumerate(lines, 1):
            line_norm = f" {norm(l)} "
            found = [w for w in gone if f" {w} " in line_norm]
            if found:
                hits.append(f"{f.replace(os.sep, '/')}:{n}  „{found[0]}“")
    print(f"{len(gone)} verschwundene Wendung(en) aus Meldungstexten; {len(hits)} Fundstelle(n), die sie noch erwarten:")
    for h in hits:
        print("  " + h)
    return 1 if hits else 0


if __name__ == "__main__":
    sys.exit(main())
