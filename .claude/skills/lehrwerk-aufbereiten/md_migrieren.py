"""Bring page-Markdown written under an older MARKDOWN.md up to the current format (mechanical parts only).

    python -I -X utf8 md_migrieren.py raw/<fach>/<werk>-pugling/md

- Task tables with the old four columns get the fifth column "Niveau/Verweis" filled with "–".
- Task numbers like "9A (S. 24)" become "S. 24 · 9A"; single-page files prefix their own page.
- Word lists (header "Wort | Übersetzung | Hinweis"): sub-entries are marked "↳ " (instead of "– " or a hint
  "Unterzeile zu …"), and a phonetic transcription at the end of the word moves to the front of the hint.
- If _buch.md has no "## Tags" section, one is generated from the kapitel/abschnitt/thema/grammatik/kompetenz tags
  the pages already use. That only seeds the vocabulary: near-duplicates must still be merged by hand.
What needs judgement (page of a task on a spread, category of unsure entries) stays for md_pruefen.py to report.
"""
from __future__ import annotations

import re
import sys
from collections import defaultdict
from pathlib import Path

OLD = ["Nr", "Art", "Was verlangt wird", "Stoff"]
WORD_HEADER = ["Wort", "Übersetzung", "Hinweis"]
VOCAB_PREFIXES = ("kapitel", "abschnitt", "thema", "grammatik", "kompetenz")
TRAILING_IPA = re.compile(r"^(.*\S)\s+\[([^\]\s][^\]]*)\]$")
SUB_HINT = re.compile(r"\s*;?\s*Unterzeile zu „[^“]*“\s*;?\s*")


def cells(line: str) -> list[str]:
    return [c.strip() for c in line.strip().strip("|").split("|")]


def row(values: list[str]) -> str:
    return "| " + " | ".join(values) + " |"


def single_page(text: str) -> str | None:
    m = re.search(r'^seiten: "(\d+)"$', text, re.M)
    return m.group(1) if m else None


def migrate_tables(text: str) -> tuple[str, int]:
    page = single_page(text)
    out, section, header, changed = [], None, None, 0
    for line in text.split("\n"):
        if line.startswith("## "):
            section, header = line.strip()[3:], None
        stripped = line.strip()
        if not stripped.startswith("|"):
            header = None
            out.append(line)
            continue
        row_cells = cells(stripped)
        is_rule = set("".join(row_cells)) <= set("-: ")
        if header is None and not is_rule:
            header = row_cells
            if section == "Aufgaben" and header == OLD:
                line, header, changed = row(OLD + ["Niveau/Verweis"]), OLD + ["Niveau/Verweis"], changed + 1
            out.append(line)
            continue
        if is_rule:
            if section == "Aufgaben" and len(row_cells) == 4:
                line = "|---|---|---|---|---|"
            out.append(line)
            continue
        if section == "Aufgaben" and len(row_cells) in (4, 5) and header and header[0] == "Nr":
            if len(row_cells) == 4:
                row_cells.append("–")
            nr = row_cells[0]
            m = re.match(r"^(.*?)\s*\(S\. (\d+)\)$", nr)
            if m:
                nr = f"S. {m.group(2)} · {m.group(1)}"
            elif not nr.startswith("S. ") and page:
                nr = f"S. {page} · {nr}"
            row_cells[0] = nr
            line = row(row_cells)
        elif header == WORD_HEADER and len(row_cells) == 3:
            word, translation, hint = row_cells
            if word.startswith("– "):
                word = "↳ " + word[2:]
            # Headword bold is print layout, not content; one list style across books.
            bold = re.match(r"^(↳ )?\*\*([^*]+)\*\*$", word)
            if bold:
                word = (bold.group(1) or "") + bold.group(2)
            if hint == "–":
                hint = ""
            hint = re.sub(r"^(\[[^\]]+\]) · –$", r"\1", hint)
            if SUB_HINT.search(hint):
                hint = SUB_HINT.sub("; ", hint).strip("; ").strip()
                if not word.startswith("↳ "):
                    word = "↳ " + word
            m = TRAILING_IPA.match(word)
            if m and not m.group(1).endswith("("):
                word, ipa = m.group(1), f"[{m.group(2)}]"
                hint = f"{ipa} · {hint}" if hint else ipa
            new = row([word, translation, hint])
            if new != line.strip():
                changed += 1
                line = new
        out.append(line)
    return "\n".join(out), changed


def used_tags(md_dir: Path) -> dict[str, set[str]]:
    found: dict[str, set[str]] = defaultdict(set)
    for page in md_dir.glob("*.md"):
        if page.name.startswith("_"):
            continue
        head = page.read_text(encoding="utf-8").split("\n---\n", 1)[0]
        for tag in re.findall(r"\b(%s)/([a-z0-9-]+)" % "|".join(VOCAB_PREFIXES), head):
            found[tag[0]].add(f"{tag[0]}/{tag[1]}")
    return found


def main() -> int:
    md_dir = Path(sys.argv[1])
    rows = 0
    for page in sorted(md_dir.glob("*.md")):
        if page.name.startswith("_"):
            continue
        text = "\n".join(page.read_text(encoding="utf-8").splitlines()) + "\n"
        new, n = migrate_tables(text)
        rows += n
        if new != text:
            page.write_text(new, encoding="utf-8")
    book = md_dir / "_buch.md"
    added = False
    if book.exists():
        text = "\n".join(book.read_text(encoding="utf-8").splitlines()) + "\n"
        if not re.search(r"^## Tags\s*$", text, re.M):
            found = used_tags(md_dir)
            block = ["## Tags", "", "Verbindliches Tag-Vokabular (aus den vorhandenen Seiten erzeugt, Dubletten von Hand "
                     "zusammenführen). Neue Tags hier ergänzen, bevor eine Seite sie benutzt.", ""]
            for prefix in VOCAB_PREFIXES:
                if found.get(prefix):
                    block.append(f"- **{prefix}**: " + " ".join(f"#{t}" for t in sorted(found[prefix])))
            block += ["", ""]
            m = re.search(r"^## Abdeckung\s*$", text, re.M)
            text = text[:m.start()] + "\n".join(block) + text[m.start():] if m else text + "\n" + "\n".join(block)
            book.write_text(text, encoding="utf-8")
            added = True
    print(f"{rows} Tabellenzeilen angepasst; Tag-Vokabular in _buch.md {'erzeugt' if added else 'schon vorhanden'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
