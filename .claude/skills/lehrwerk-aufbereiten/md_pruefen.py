"""Check the page-Markdown of a textbook (MARKDOWN.md) for uniformity and completeness; optionally write _index.md.

    python -I -X utf8 md_pruefen.py raw/<fach>/<werk>-pugling/md --fotos raw/<fach>/<werk> [--index]

Exit code 0 = no FEHLER. Hinweise do not fail the run but must be looked at.
"""
from __future__ import annotations

import argparse
import re
import sys
from collections import Counter
from pathlib import Path

PAGE_TYPES = {
    "umschlag", "titelei", "inhaltsverzeichnis", "einstieg", "lektion", "merkwissen", "uebungen", "test",
    "methoden", "grammatik", "vokabeln", "loesungen", "register", "landeskunde", "anhang", "sonstiges",
}
TAG_PREFIXES = {
    "buch", "fach", "stufe", "verlag", "seitentyp", "schulart", "lernjahr", "kapitel", "thema", "grammatik",
    "kompetenz", "abschnitt",
}
REQUIRED_TAG_PREFIXES = ("buch", "fach", "stufe", "verlag", "seitentyp")
PAGE_KEYS = ("typ", "titel", "buch", "fach", "klasse", "verlag", "seiten", "fotos", "kapitel", "seitentyp",
             "drehung", "lesbarkeit", "unsicher", "tags")
BOOK_KEYS = ("typ", "titel", "buch", "fach", "klasse", "verlag", "fotos-ordner", "tags")
SHARED_KEYS = ("buch", "fach", "klasse", "verlag")
SECTIONS = ("Überblick", "Seitenelemente", "Fakten", "Aufgaben", "Unsicher")
BOOK_SECTIONS = ("Überblick", "Kapitel", "Aufbau", "Tags", "Abdeckung")
# Tags whose values must come from the book's controlled vocabulary in _buch.md ("## Tags").
VOCAB_PREFIXES = ("kapitel", "abschnitt", "thema", "grammatik", "kompetenz")
TASK_HEADER = ["Nr", "Art", "Was verlangt wird", "Stoff", "Niveau/Verweis"]
WORD_HEADER = ["Wort", "Übersetzung", "Hinweis"]
UNSURE_KINDS = ("**unleserlich**", "**nicht im Bild**", "**Druckfehler?**", "**fraglich**")
EMPTY = "– keine –"
SLUG = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
FILE_NAME = re.compile(r"^(?:s\d{3}(?:-\d{3})?|umschlag|titelei|foto-\d{6})(?:-[a-z0-9]+)?\.md$")
PHOTO_EXT = {".jpg", ".jpeg", ".png"}
# Page types whose whole point is factual content: an empty "Fakten" section means the page was skipped.
NEEDS_FACTS = {"inhaltsverzeichnis", "merkwissen", "grammatik", "vokabeln", "register"}
NEEDS_TASKS = {"uebungen", "test"}


def strip_comment(line: str) -> str:
    """Drop a trailing YAML comment that sits outside quotes."""
    quote = None
    for i, ch in enumerate(line):
        if ch in "\"'":
            quote = None if quote == ch else (ch if quote is None else quote)
        elif ch == "#" and quote is None and (i == 0 or line[i - 1] in " \t"):
            return line[:i].rstrip()
    return line.rstrip()


def parse_scalar(raw: str):
    raw = raw.strip()
    if raw.startswith("[") and raw.endswith("]"):
        inner = raw[1:-1].strip()
        return [parse_scalar(p) for p in split_list(inner)] if inner else []
    if len(raw) >= 2 and raw[0] == raw[-1] and raw[0] in "\"'":
        return raw[1:-1]
    if re.fullmatch(r"-?\d+", raw):
        return int(raw)
    return raw


def split_list(inner: str) -> list[str]:
    parts, cur, quote = [], "", None
    for ch in inner:
        if ch in "\"'":
            quote = None if quote == ch else (ch if quote is None else quote)
        if ch == "," and quote is None:
            parts.append(cur)
            cur = ""
        else:
            cur += ch
    parts.append(cur)
    return [p.strip() for p in parts if p.strip()]


def read_note(path: Path) -> tuple[dict, str, list[str]]:
    """Return (frontmatter, body, problems). Only the flat YAML subset that MARKDOWN.md prescribes."""
    # Editors on Windows may save CRLF; the format is line-based, so normalise instead of failing.
    text = "\n".join(path.read_text(encoding="utf-8").splitlines()) + "\n"
    if not text.startswith("---\n"):
        return {}, text, ["keine Frontmatter (Datei muss mit '---' beginnen)"]
    end = text.find("\n---\n", 4)
    if end < 0:
        return {}, text, ["Frontmatter nicht geschlossen"]
    lines, merged = text[4:end].splitlines(), []
    for line in lines:
        line = strip_comment(line)
        if not line.strip():
            continue
        if line[0] in " \t" and merged:
            merged[-1] += " " + line.strip()
        else:
            merged.append(line)
    meta, problems = {}, []
    for line in merged:
        if ":" not in line:
            problems.append(f"Frontmatter-Zeile ohne Doppelpunkt: {line!r}")
            continue
        key, value = line.split(":", 1)
        meta[key.strip()] = parse_scalar(value)
    return meta, text[end + 5:], problems


def sections_of(body: str) -> dict[str, str]:
    found, current = {}, None
    for line in body.splitlines():
        m = re.match(r"^## (.+?)\s*$", line)
        if m:
            current = m.group(1)
            found[current] = ""
        elif current is not None:
            found[current] += line + "\n"
    return found


def section_order(body: str) -> list[str]:
    return [m.group(1) for m in re.finditer(r"^## (.+?)\s*$", body, re.M)]


def is_empty(content: str) -> bool:
    stripped = content.strip()
    return stripped == EMPTY or stripped == ""


def check_tags(meta: dict, where: str, errors: list[str], page_types: list[str] | None) -> None:
    tags = meta.get("tags")
    if not isinstance(tags, list) or not tags:
        errors.append(f"{where}: tags fehlt oder ist leer")
        return
    prefixes = Counter()
    for tag in tags:
        if not isinstance(tag, str) or "/" not in tag:
            errors.append(f"{where}: Tag {tag!r} ohne Präfix")
            continue
        prefix, value = tag.split("/", 1)
        prefixes[prefix] += 1
        if prefix not in TAG_PREFIXES:
            errors.append(f"{where}: Tag-Präfix {prefix!r} nicht erlaubt ({tag})")
        if not SLUG.match(value):
            errors.append(f"{where}: Tag {tag!r} ist kein Slug (klein, ASCII, Bindestriche)")
    if len(tags) != len(set(tags)):
        errors.append(f"{where}: doppelte Tags")
    by_slug: dict[str, set[str]] = {}
    for tag in tags:
        if isinstance(tag, str) and "/" in tag and tag.split("/", 1)[0] in ("thema", "grammatik", "kompetenz"):
            by_slug.setdefault(tag.split("/", 1)[1], set()).add(tag.split("/", 1)[0])
    for slug, prefixes_of in by_slug.items():
        if len(prefixes_of) > 1:
            errors.append(f"{where}: '{slug}' unter mehreren Präfixen {sorted(prefixes_of)} – ein Begriff, ein Tag")
    required = REQUIRED_TAG_PREFIXES if page_types is not None else ("buch", "fach", "stufe", "verlag")
    for prefix in required:
        if not prefixes[prefix]:
            errors.append(f"{where}: Pflicht-Tag {prefix}/… fehlt")
    if page_types is not None:
        tagged = {t.split("/", 1)[1] for t in tags if isinstance(t, str) and t.startswith("seitentyp/")}
        if tagged != set(page_types):
            errors.append(f"{where}: seitentyp-Tags {sorted(tagged)} passen nicht zum Feld {sorted(page_types)}")


def check_book(md_dir: Path, errors: list[str]) -> dict:
    path = md_dir / "_buch.md"
    if not path.exists():
        errors.append("_buch.md fehlt (Steckbrief des Buchs)")
        return {}
    meta, body, problems = read_note(path)
    errors.extend(f"_buch.md: {p}" for p in problems)
    for key in BOOK_KEYS:
        if key not in meta or meta[key] in ("", None, []):
            errors.append(f"_buch.md: Feld {key!r} fehlt")
    if meta.get("typ") != "buch":
        errors.append("_buch.md: typ muss 'buch' sein")
    check_tags(meta, "_buch.md", errors, None)
    secs = sections_of(body)
    for name in BOOK_SECTIONS:
        if name not in secs or is_empty(secs[name]):
            errors.append(f"_buch.md: Abschnitt '## {name}' fehlt oder ist leer")
    vocab = set(re.findall(r"#((?:%s)/[a-z0-9-]+)" % "|".join(VOCAB_PREFIXES), secs.get("Tags", "")))
    for tag in sorted(vocab):
        if not SLUG.match(tag.split("/", 1)[1]):
            errors.append(f"_buch.md: Tag-Vokabular enthält {tag!r} – kein Slug")
    meta["_vocab"] = vocab
    if "lernjahr" in meta and not isinstance(meta["lernjahr"], int):
        errors.append("_buch.md: lernjahr muss eine Zahl sein")
    return meta


def check_word_lists(name: str, facts: str, errors: list[str]) -> None:
    """Word lists share one layout so later tools can parse them without guessing (MARKDOWN.md, Wortlisten)."""
    header = None
    for line in facts.splitlines():
        s = line.strip()
        if not s.startswith("|"):
            header = None
            continue
        row = [c.strip() for c in s.strip("|").split("|")]
        if header is None:
            header = row
            continue
        if header != WORD_HEADER or set("".join(row)) <= set("-: "):
            continue
        if len(row) != 3:
            errors.append(f"{name}: Wortlisten-Zeile mit {len(row)} statt 3 Zellen: {s[:60]}")
            continue
        word, _, hint = row
        if word.startswith(("– ", "- ")):
            errors.append(f"{name}: Untereintrag mit '↳ ' markieren, nicht '– ': {word[:40]}")
        if re.search(r"\s\[[^\]\s][^\]]*\]$", word):
            errors.append(f"{name}: Lautschrift gehört an den Anfang von Hinweis, nicht ins Wort: {word[:40]}")
        if re.match(r"^(↳ )?\*\*[^*]+\*\*$", word):
            errors.append(f"{name}: Stichwort nicht fett setzen (Druckbild, kein Inhalt): {word[:40]}")
        if "Unterzeile zu" in hint:
            errors.append(f"{name}: Untereintrag mit '↳ ' im Wort markieren statt 'Unterzeile zu': {word[:40]}")


def check_page(path: Path, book: dict, errors: list[str], hints: list[str]) -> dict:
    name = path.name
    meta, body, problems = read_note(path)
    errors.extend(f"{name}: {p}" for p in problems)
    if not FILE_NAME.match(name):
        errors.append(f"{name}: Dateiname passt nicht zum Schema (s024-025.md, s147.md, umschlag.md, foto-070459.md)")
    for key in PAGE_KEYS:
        if key not in meta or meta[key] in ("", None):
            errors.append(f"{name}: Feld {key!r} fehlt")
    if meta.get("typ") != "seite":
        errors.append(f"{name}: typ muss 'seite' sein")
    for key in SHARED_KEYS:
        if book and key in meta and str(meta[key]) != str(book.get(key)):
            errors.append(f"{name}: {key}={meta[key]!r} weicht von _buch.md ab ({book.get(key)!r})")
    page_types = meta.get("seitentyp")
    if not isinstance(page_types, list) or not page_types:
        errors.append(f"{name}: seitentyp muss eine nicht leere Liste sein")
        page_types = []
    for t in page_types:
        if t not in PAGE_TYPES:
            errors.append(f"{name}: seitentyp {t!r} unbekannt")
    if meta.get("drehung") not in (0, 90, 180, 270):
        errors.append(f"{name}: drehung muss 0, 90, 180 oder 270 sein")
    if meta.get("lesbarkeit") not in ("gut", "teilweise", "schlecht"):
        errors.append(f"{name}: lesbarkeit muss gut, teilweise oder schlecht sein")
    if not isinstance(meta.get("fotos"), list) or not meta.get("fotos"):
        errors.append(f"{name}: fotos muss eine nicht leere Liste sein")
    check_tags(meta, name, errors, page_types)
    if book and isinstance(book.get("lernjahr"), int):
        if f"lernjahr/{book['lernjahr']}" not in (meta.get("tags") or []):
            errors.append(f"{name}: Tag lernjahr/{book['lernjahr']} fehlt (lernjahr aus _buch.md)")
    vocab = book.get("_vocab") if book else None
    if vocab is not None:
        for tag in meta.get("tags") or []:
            if isinstance(tag, str) and tag.split("/", 1)[0] in VOCAB_PREFIXES and tag not in vocab:
                errors.append(f"{name}: Tag {tag!r} steht nicht im Tag-Vokabular von _buch.md (## Tags)")

    order = section_order(body)
    if [s for s in order if s in SECTIONS] != list(SECTIONS) or len(order) != len(SECTIONS):
        errors.append(f"{name}: Abschnitte {order} – erwartet genau {list(SECTIONS)} in dieser Reihenfolge")
    if not re.search(r"^# \S", body, re.M):
        errors.append(f"{name}: Überschrift '# …' fehlt")
    secs = sections_of(body)
    for sec in ("Überblick", "Seitenelemente"):
        if is_empty(secs.get(sec, "")):
            errors.append(f"{name}: '## {sec}' ist leer")
    for sec in SECTIONS:
        if sec in secs and secs[sec].strip() == "":
            errors.append(f"{name}: '## {sec}' ganz leer – leer heißt '{EMPTY}'")
    if NEEDS_FACTS & set(page_types) and is_empty(secs.get("Fakten", "")):
        errors.append(f"{name}: seitentyp {sorted(NEEDS_FACTS & set(page_types))} ohne Fakten")
    if "vokabeln" in page_types and "|" not in secs.get("Fakten", ""):
        errors.append(f"{name}: Vokabelseite ohne Wort-Tabelle in Fakten")
    if NEEDS_TASKS & set(page_types) and is_empty(secs.get("Aufgaben", "")):
        errors.append(f"{name}: seitentyp {sorted(NEEDS_TASKS & set(page_types))} ohne Aufgaben")

    tasks = secs.get("Aufgaben", "")
    if not is_empty(tasks):
        rows = [l.strip() for l in tasks.splitlines() if l.strip().startswith("|")]
        header = [c.strip() for c in rows[0].strip("|").split("|")] if rows else []
        if header != TASK_HEADER:
            errors.append(f"{name}: Aufgaben-Tabelle braucht genau die Spalten {TASK_HEADER}, hat {header}")
        for row in rows[2:]:
            cells = [c.strip() for c in row.strip("|").split("|")]
            if len(cells) != len(TASK_HEADER):
                errors.append(f"{name}: Aufgabenzeile mit {len(cells)} statt {len(TASK_HEADER)} Zellen: {row[:60]}")
            elif not cells[0].startswith("S. "):
                errors.append(f"{name}: Aufgaben-Nr ohne Seite ('S. 24 · 1'): {cells[0]!r}")

    check_word_lists(name, secs.get("Fakten", ""), errors)

    unsure = secs.get("Unsicher", "")
    entries = [l for l in unsure.splitlines() if l.strip().startswith("- ")]
    if not is_empty(unsure):
        for entry in entries:
            if not entry.strip()[2:].startswith(UNSURE_KINDS):
                errors.append(f"{name}: Unsicher-Eintrag ohne Kategorie {UNSURE_KINDS}: {entry.strip()[:60]}")
    count = 0 if is_empty(unsure) else len(entries)
    if meta.get("unsicher") != count:
        errors.append(f"{name}: unsicher={meta.get('unsicher')!r}, aber {count} Einträge unter '## Unsicher'")
    if re.search(r"\bTODO\b|<titel>|<[a-z-]+>", body):
        errors.append(f"{name}: Platzhalter (TODO/<…>) im Text")
    if meta.get("lesbarkeit") != "gut" and count == 0:
        hints.append(f"{name}: lesbarkeit={meta.get('lesbarkeit')}, aber nichts unsicher – stimmt das?")
    if len(body) < 400:
        hints.append(f"{name}: nur {len(body)} Zeichen – Seite wirklich vollständig erfasst?")
    meta["_name"] = name
    return meta


def page_key(meta: dict) -> tuple:
    m = re.match(r"^(\d+)", str(meta.get("seiten", "")))
    return (0 if meta["_name"] in ("umschlag.md", "titelei.md") else 1, int(m.group(1)) if m else 10_000,
            meta["_name"])


def write_index(md_dir: Path, book: dict, pages: list[dict]) -> None:
    pages = sorted(pages, key=page_key)
    tags = Counter(t for p in pages for t in p.get("tags", []) if isinstance(t, str))
    out = [
        "---",
        "typ: index",
        f"titel: \"{book.get('buch', '?')} – Seitenindex\"",
        f"buch: \"{book.get('buch', '?')}\"",
        f"tags: [{', '.join(t for t in book.get('tags', []))}]",
        "---",
        "",
        f"# {book.get('buch', '?')} – Seitenindex",
        "",
        "Erzeugt von `md_pruefen.py --index` – nicht von Hand ändern. Steckbrief: [[_buch]].",
        "",
        f"{len(pages)} Seitendateien aus {sum(len(p.get('fotos', [])) for p in pages)} Fotos.",
        "",
        "## Seiten",
        "",
        "| Datei | Seiten | Kapitel | Seitentyp | Unsicher |",
        "|---|---|---|---|---|",
    ]
    for p in pages:
        stem = p["_name"][:-3]
        out.append(f"| [[{stem}]] | {p.get('seiten')} | {p.get('kapitel')} | {', '.join(p.get('seitentyp', []))} "
                   f"| {p.get('unsicher')} |")
    by_chapter: dict[str, list[str]] = {}
    for p in pages:
        by_chapter.setdefault(str(p.get("kapitel")), []).append(f"[[{p['_name'][:-3]}]]")
    out += ["", "## Nach Kapitel", ""]
    out += [f"- **{k}**: {', '.join(v)}" for k, v in by_chapter.items()]
    out += ["", "## Tags", ""]
    for prefix in ("kapitel", "seitentyp", "thema", "grammatik", "kompetenz"):
        found = sorted((t, n) for t, n in tags.items() if t.startswith(prefix + "/"))
        if found:
            out.append(f"- **{prefix}**: " + ", ".join(f"#{t} ({n})" for t, n in found))
    (md_dir / "_index.md").write_text("\n".join(out) + "\n", encoding="utf-8")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("md_dir", type=Path)
    ap.add_argument("--fotos", type=Path, required=True, help="folder with the book's photos (not recursive)")
    ap.add_argument("--index", action="store_true", help="write _index.md when there are no errors")
    ap.add_argument("--nur", nargs=2, metavar=("VON", "BIS"), help="partial run: only expect photos whose "
                    "file name sorts between VON and BIS inclusive (e.g. IMG_20261111_135636.jpg "
                    "IMG_20261111_140230.jpg); every existing page file is still checked")
    args = ap.parse_args()

    errors: list[str] = []
    hints: list[str] = []
    if not args.md_dir.is_dir():
        print(f"FEHLER: {args.md_dir} ist kein Ordner")
        return 1
    book = check_book(args.md_dir, errors)
    pages = [check_page(p, book, errors, hints) for p in sorted(args.md_dir.glob("*.md"))
             if not p.name.startswith("_")]

    all_photos = sorted(p.name for p in args.fotos.iterdir() if p.is_file() and p.suffix.lower() in PHOTO_EXT)
    photos = all_photos
    if args.nur:
        lo, hi = args.nur
        photos = [p for p in all_photos if lo <= p <= hi]
        if not photos:
            print(f"FEHLER: --nur {args.nur} trifft kein Foto")
            return 1
    owner: dict[str, list[str]] = {}
    for p in pages:
        for f in p.get("fotos", []) or []:
            owner.setdefault(str(f), []).append(p["_name"])
    for photo in photos:
        if photo not in owner:
            errors.append(f"Foto {photo} ist in keiner Seitendatei erfasst")
        elif len(owner[photo]) > 1:
            errors.append(f"Foto {photo} steht in mehreren Dateien: {owner[photo]}")
    for photo in owner:
        if photo not in all_photos:
            errors.append(f"{owner[photo][0]}: Foto {photo} gibt es im Fotoordner nicht")

    if args.nur:
        # A block agent should see only its own findings: _buch.md, pages of its photos, photos of its range.
        mine = {"_buch.md"} | {p["_name"] for p in pages if set(map(str, p.get("fotos") or [])) & set(photos)}
        errors = [e for e in errors if e.split(":", 1)[0] in mine
                  or (e.startswith("Foto ") and e.split(" ", 2)[1] in photos)]
        hints = [h for h in hints if h.split(":", 1)[0] in mine]
    for e in errors:
        print(f"FEHLER  {e}")
    for h in hints:
        print(f"Hinweis {h}")
    covered = sum(1 for p in photos if p in owner)
    unsure = sum(p.get("unsicher", 0) for p in pages if isinstance(p.get("unsicher"), int))
    print(f"\n{len(pages)} Seitendateien, {covered}/{len(photos)} Fotos erfasst, {unsure} unsichere Stellen, "
          f"{len(errors)} Fehler, {len(hints)} Hinweise")
    if args.index and not errors and not args.nur:
        write_index(args.md_dir, book, pages)
        print(f"_index.md geschrieben ({args.md_dir / '_index.md'})")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
