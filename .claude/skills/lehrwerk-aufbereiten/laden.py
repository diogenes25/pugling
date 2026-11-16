"""Lädt ein aufbereitetes Lehrwerk über die Creator-/Supervisor-API in eine laufende Pugling-Instanz.

Ein Lehrwerk ist ein Ordner unter raw/ (gitignored, Urheberrecht) mit
    buch.json               Reihe, Klasse, Fachlehrer-Profil, Grammatik-Themen, Units, optional Kind
    vokabeln-unit<N>.tsv    Wortliste je Unit (abschnitt, wort, uebersetzung, hinweis, notiz, unsicher)
    uebungen.json           optionale Start-Übungen (eigene Formulierung, nie Aufgaben des Buchs)
Das Format beschreibt FORMAT.md neben diesem Skript.

    python -I -X utf8 laden.py <ordner> --pruefen                  # nur prüfen, ohne API
    python -I -X utf8 laden.py <ordner> --base http://localhost:5200 --account 1 --pin <PIN> [--child 1]

Idempotent: Reihe, Units, Profil und Grammatik-Themen werden gefunden und aktualisiert; eine Übung mit gleichem
Titel in der Unit bleibt stehen (mit --ersetzen wird sie überschrieben). Jede Übung des Ordners – neu, ersetzt
oder stehen geblieben – läuft danach durch den Testmodus mit ihren eigenen Lösungen.
"""
import argparse
import csv
import json
import os
import re
import sys
import unicodedata
import urllib.error
import urllib.request

TSV_COLUMNS = ["abschnitt", "wort", "uebersetzung", "hinweis", "notiz", "unsicher"]
# Typed exercise routes of the Creator API (ExerciseControllers.cs); the loader passes the config through as is.
ROUTES = {"vocabulary", "cloze", "grammar", "matching", "translation", "reading", "list", "birkenbihl"}
# Field limits of the Creator API (SeriesUnit/CreatorProfile); checked offline so a run never stops halfway.
MAX_TOPIC, MAX_TEXT = 200, 2000
# Exercise.BookPages (B-588): free text as printed ("42-43", "WB 12"), at most 20 characters after trimming.
MAX_PAGES = 20
# Config fields per route, mirrored from the config types in Pugling.Contracts: the server rejects an unknown one with
# 400 unknown_field, which would stop a run halfway. Key "" is the top level; any other key is a list of objects, and
# a dotted key ("sentences.decoding") is a list inside the entries of another. LadenFieldListTests reads the JSON
# between the markers and compares it with the config type each route binds, so it cannot drift.
# FIELDS-BEGIN
FIELDS = {
    "vocabulary": {"": ["direction", "sourceLang", "targetLang", "articleRequired", "refs", "items"],
                   "items": ["front", "back", "hint", "vocabularyId", "explanation"], "refs": ["vocabularyId", "key"]},
    "cloze": {"": ["text", "gaps", "wordBank", "cueDistractors"], "gaps": ["index", "answer", "alternatives", "vocabKey", "form", "distractors"]},
    "grammar": {"": ["instruction", "tasks"], "tasks": ["prompt", "answer", "ruleHint"]},
    "matching": {"": ["instruction", "pairs"], "pairs": ["left", "right"]},
    "translation": {"": ["sourceLang", "targetLang", "items"],
                    "items": ["source", "target", "alternatives", "vocabularyId"]},
    "reading": {"": ["text", "questions"], "questions": ["prompt", "choices", "answer"]},
    "list": {"": ["instruction", "ordered", "items"], "items": ["value", "alternatives"]},
    "birkenbihl": {"": ["learningLang", "nativeLang", "nextSentenceId", "nextWordId", "sentences"],
                   "sentences": ["sentenceId", "learningSentence", "naturalTranslation", "decoding"],
                   "sentences.decoding": ["wordId", "learningWord", "gloss", "vocabularyId"]}
}
# FIELDS-END
# Authoring shortcuts build_config turns into contract fields. Only this exact spelling is read there, so only this
# spelling is allowed; "Markup" would pass through unchanged and be rejected by the server. build_config reads
# nothing else: every other field goes to the server as written, so check() and the server are its only readers.
SHORTCUTS = {"cloze": {"markup", "distractors"}}
# What a shortcut decides; written by hand as well, one of the two would silently win. markup decides the bank too:
# none (free typing) or answers plus distractors - a hand-written bank next to generated gaps may miss an answer.
# distractors needs markup (checked on its own), so markup's entry covers it.
SHORTCUT_WRITES = {"markup": {"text", "gaps", "wordbank"}}


# --- Reading and offline check ----------------------------------------------------------------------------------

def read_book(folder):
    with open(os.path.join(folder, "buch.json"), encoding="utf-8") as f:
        book = json.load(f)
    words = {}
    for unit in book.get("units", []):
        path = os.path.join(folder, f"vokabeln-unit{unit.get('nr')}.tsv")
        if os.path.exists(path):
            with open(path, encoding="utf-8", newline="") as f:
                reader = csv.DictReader(f, delimiter="\t")
                words[unit["nr"]] = (reader.fieldnames or [], [(n + 2, row) for n, row in enumerate(reader)])
    exercises = []
    path = os.path.join(folder, "uebungen.json")
    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            exercises = json.load(f)
    return book, words, exercises


def parse_cloze(markup):
    """`We [went|did go] (go) to the Tower.` -> text with {{n}} and gaps; mirrors ClozeMarkup in the agent."""
    errors, gaps, out, pos = [], [], [], 0
    if "{{" in markup:
        errors.append("schreibt selbst {{n}}-Platzhalter; Lücken werden als [Lösung] geschrieben")
    for m in re.finditer(r"\[([^\[\]]*)\]", markup):
        out.append(markup[pos:m.start()])
        spellings = [s.strip() for s in m.group(1).split("|") if s.strip()]
        if not spellings:
            errors.append(f"leere Klammer an Stelle {m.start()}")
            spellings = ["?"]
        gaps.append({"index": len(gaps) + 1, "answer": spellings[0], "alternatives": spellings[1:] or None})
        out.append("{{" + str(len(gaps)) + "}}")
        pos = m.end()
    out.append(markup[pos:])
    text = "".join(out)
    if "[" in text or "]" in text:
        errors.append("eckige Klammer ohne Gegenstück")
    if not gaps:
        errors.append("keine einzige [Lücke]")
    return text, gaps, errors


def graded(word):
    """What the server's answer comparison sees of a word (StageMechanics.Normalize): case, a closing . ? ! …, the
    apostrophe spelling, any whitespace and an invisible U+FEFF do not count. Two tiles alike here are one tile to the
    child. The cases are shared with the server and the father's web (docs/fallliste-antwortvergleich.json, B-493)."""
    s = word.replace(chr(0xFEFF), "")
    for c in "’‘ʼ´`":
        s = s.replace(c, "'")
    s = s.strip()
    stripped = s.rstrip()
    while stripped and stripped[-1] in ".?!…":
        stripped = stripped[:-1].rstrip()
    # Split on any whitespace, like the server since B-497: a no-break space copied from a PDF is a space.
    return " ".join((stripped or s).lower().split())


def contains_word(text, word):
    return re.search(r"(?<!\w)" + re.escape(word) + r"(?!\w)", text, re.IGNORECASE) is not None


def unknown_fields(typ, config):
    """Paths of config fields the route does not know. Contract fields match case-insensitively like the server's
    JSON binding; the server's response-only "_self" links count as unknown, since an author never writes them."""
    known = FIELDS.get(typ)
    if known is None:
        return []
    shortcuts = SHORTCUTS.get(typ, set())
    out = []

    def walk(obj, key, where):
        fields = known[key]
        allowed = {f.lower() for f in fields}
        names = fields + (sorted(shortcuts) if key == "" else [])
        out.extend(f"{where}.{k} – {typ} kennt {'dort ' if key else ''}{', '.join(names)}"
                   for k in obj if k.lower() not in allowed and not (key == "" and k in shortcuts))
        # The server binds case-insensitively and keeps one of two spellings; the other one is lost without a word.
        spellings = {}
        for k in obj:
            spellings.setdefault(k.lower(), []).append(k)
        out.extend(f"{where}: {' und '.join(ks)} sind dasselbe Feld – nur eine Schreibweise"
                   for ks in spellings.values() if len(ks) > 1)
        for child in (c for c in known if c and c.rpartition(".")[0] == key):
            name = child.rpartition(".")[2]
            entries = next((v for k, v in obj.items() if k.lower() == name.lower()), None)
            for i, entry in enumerate(entries if isinstance(entries, list) else []):
                if isinstance(entry, dict):
                    walk(entry, child, f"{where}.{name}[{i}]")

    walk(config, "", "config")
    return out


def check(book, words, exercises):
    """Returns (errors, warnings). Errors stop the load; warnings are for the father's review."""
    err, warn = [], []
    for key in ("reihe", "klasse", "quelle", "profil", "units"):
        if key not in book:
            err.append(f"buch.json: Feld '{key}' fehlt")
    if err:
        return err, warn
    for key in ("name", "verlag", "fach", "schularten", "source", "target"):
        if not book["reihe"].get(key):
            err.append(f"buch.json: reihe.{key} fehlt")
    for key in ("name", "persona", "didactics"):
        value = book["profil"].get(key) or ""
        if not value:
            err.append(f"buch.json: profil.{key} fehlt")
        elif len(value) > MAX_TEXT:
            err.append(f"buch.json: profil.{key} hat {len(value)} Zeichen (höchstens {MAX_TEXT})")

    declared = {g["label"].lower() for g in book.get("grammatik", [])}
    undeclared = set()
    units = {}
    for u in book["units"]:
        name = f"Unit {u.get('nr')}"
        if u.get("nr") in units:
            err.append(f"{name}: Nummer doppelt")
        units[u.get("nr")] = u
        for key in ("label", "topics", "grammar", "wortfelder"):
            if not u.get(key):
                err.append(f"{name}: Feld '{key}' fehlt")
        if len(u.get("grammar", "")) > MAX_TEXT:
            err.append(f"{name}: grammar hat {len(u['grammar'])} Zeichen (höchstens {MAX_TEXT})")
        for t in u.get("topics", []):
            if len(t) > MAX_TOPIC:
                err.append(f"{name}: Thema länger als {MAX_TOPIC} Zeichen: {t[:40]}…")
        undeclared.update(ref for ref in u.get("grammatik", []) if ref.lower() not in declared)

    for nr, (columns, rows) in words.items():
        name = f"vokabeln-unit{nr}.tsv"
        if nr not in units:
            err.append(f"{name}: keine Unit {nr} in buch.json")
        if columns != TSV_COLUMNS:
            err.append(f"{name}: Spalten {columns}, erwartet {TSV_COLUMNS} (Tab-getrennt)")
            continue
        seen = {}
        for line, row in rows:
            word, translation = (row["wort"] or "").strip(), (row["uebersetzung"] or "").strip()
            if not word or not translation or not (row["abschnitt"] or "").strip():
                err.append(f"{name}:{line}: abschnitt, wort und uebersetzung sind Pflicht")
                continue
            if (row["unsicher"] or "").strip():
                err.append(f"{name}:{line}: unsicher gelesen ({word} = {translation}; {row['unsicher']}) – "
                           f"am Foto prüfen und die Spalte leeren")
            if word.lower() in seen:
                err.append(f"{name}:{line}: '{word}' steht schon in Zeile {seen[word.lower()]}")
            seen[word.lower()] = line
            if word.lower() == translation.lower():
                warn.append(f"{name}:{line}: Wort und Übersetzung sind gleich ({word}) – lernt das Kind hier etwas?")
            hint = (row["hinweis"] or "").strip()
            meanings = [m.strip() for m in re.split(r"[,;/()]+", translation)]
            if hint and any(len(m) > 3 and contains_word(hint, m) for m in meanings):
                warn.append(f"{name}:{line}: der Hinweis verrät die Übersetzung ({hint})")
        if len(rows) < 3:
            warn.append(f"{name}: nur {len(rows)} Wörter – der KI-Creator nutzt eine Wortliste erst ab 3")

    titles = set()
    for n, e in enumerate(exercises, 1):
        name = f"uebungen.json #{n} ({e.get('titel', '?')})"
        if e.get("typ") not in ROUTES:
            err.append(f"{name}: typ '{e.get('typ')}' unbekannt ({', '.join(sorted(ROUTES))})")
        if e.get("unit") not in units:
            err.append(f"{name}: Unit {e.get('unit')} gibt es in buch.json nicht")
        if not e.get("titel") or not isinstance(e.get("config"), dict):
            err.append(f"{name}: titel und config sind Pflicht")
            continue
        if e.get("typ") not in ROUTES:
            continue
        if (e["unit"], e["titel"]) in titles:
            err.append(f"{name}: Titel doppelt in der Unit")
        titles.add((e["unit"], e["titel"]))
        err.extend(f"{name}: {p}" for p in unknown_fields(e.get("typ"), e["config"]))
        for shortcut in set(SHORTCUT_WRITES) & SHORTCUTS.get(e["typ"], set()) & set(e["config"]):
            clash = sorted(k for k in e["config"] if k.lower() in SHORTCUT_WRITES[shortcut])
            if clash:
                err.append(f"{name}: {shortcut} erzeugt {', '.join(clash)} selbst – nur eines von beiden schreiben")
        if e["typ"] == "cloze" and "distractors" in e["config"] and "markup" not in e["config"]:
            err.append(f"{name}: distractors gibt es nur zusammen mit markup; ohne markup die wordBank selbst schreiben")
        # build_config is the only other reader of the two shortcuts, so their shape is checked here, not mid-run.
        if e["typ"] == "cloze" and "markup" in e["config"] and not isinstance(e["config"]["markup"], str):
            err.append(f"{name}: markup ist ein Text")
            continue
        # Present but null or empty is not "no bank": null would let the child type freely, [] would show the
        # solutions alone. Leaving the field out is the way to free typing.
        has_distractors = e["typ"] == "cloze" and "distractors" in e["config"]
        distractors = e["config"]["distractors"] if has_distractors else None
        if has_distractors and not (isinstance(distractors, list) and distractors
                                    and all(isinstance(d, str) and d.strip() for d in distractors)):
            err.append(f'{name}: distractors ist eine Liste nicht-leerer Texte, z. B. ["went", "gone"]; '
                       f'ohne Wortbank das Feld weglassen')
            distractors = None
        # The same for a hand-written bank (B-491): the server drops blank entries silently - a bank of nothing else
        # turns the word-bank stage into free typing, a blank among real ones is a slip worth naming.
        bank = e["config"].get("wordBank") if e["typ"] == "cloze" else None
        if e["typ"] == "cloze" and "wordBank" in e["config"] and not (
                isinstance(bank, list) and bank and all(isinstance(w, str) and w.strip() for w in bank)):
            err.append(f'{name}: wordBank ist eine Liste nicht-leerer Texte, z. B. ["went", "gone"]; '
                       f'ohne Wortbank das Feld weglassen')
        elif e["typ"] == "cloze" and bank and isinstance(e["config"].get("gaps"), list):
            # The same question as for distractors on the markup path (B-497): a tile the grader accepts as another
            # spelling of a solution is a second right tile.
            gap_list = [g for g in e["config"]["gaps"] if isinstance(g, dict)]
            answers = {graded(str(g.get("answer") or "")) for g in gap_list}
            spellings = {graded(str(a)) for g in gap_list if isinstance(g.get("alternatives"), list)
                         for a in g["alternatives"]} - answers
            for w in bank:
                if graded(w) in spellings:
                    err.append(f"{name}: die Kachel '{w}' ist eine andere Schreibweise einer Lösung")
        if e["typ"] == "cloze" and "markup" in e["config"]:
            _, gaps, problems = parse_cloze(e["config"]["markup"])
            err.extend(f"{name}: {p}" for p in problems)
            # A distractor the grader accepts for a gap would be a second right tile next to the answer's. One equal
            # to an answer merges with that answer's tile in build_config and is harmless.
            answers = {graded(g["answer"]) for g in gaps}
            alternatives = {graded(a) for g in gaps for a in g["alternatives"] or []} - answers
            for d in distractors or []:
                if graded(d) in alternatives:
                    err.append(f"{name}: der Ablenker '{d}' ist eine andere Schreibweise einer Lösung")
            readable = re.sub(r"\[[^\[\]]*\]", " ", e["config"]["markup"])
            for g in gaps:
                if contains_word(re.sub(r"\([^()]*\)", " ", readable), g["answer"]):
                    warn.append(f"{name}: die Lösung '{g['answer']}' steht schon im Text")
        # The page goes to the server as bookPages; checked here, a 400 would stop the run halfway (B-589).
        if "seiten" in e and not (isinstance(e["seiten"], str) and 0 < len(e["seiten"].strip()) <= MAX_PAGES):
            err.append(f'{name}: seiten ist die Buchseite wie gedruckt, ein Text mit 1 bis {MAX_PAGES} Zeichen, '
                       f'z. B. "42-43"; ohne Seite das Feld weglassen')
        undeclared.update(ref for ref in e.get("grammatik", []) if ref.lower() not in declared)
    if undeclared:
        warn.append(f"nicht in buch.json deklariert, müssen also schon auf dem Server stehen: "
                    f"{', '.join(sorted(undeclared))}")
    return err, warn


# --- API ---------------------------------------------------------------------------------------------------------

class Api:
    def __init__(self, base, account, pin):
        self.root, self.token = base.rstrip("/") + "/api/v1", None
        self.token = self.call("POST", "/auth/login", {"accountId": account, "pin": pin})["token"]

    def call(self, method, path, body=None):
        data = None if body is None else json.dumps(body, ensure_ascii=False).encode("utf-8")
        req = urllib.request.Request(self.root + path, data=data, method=method)
        req.add_header("Content-Type", "application/json; charset=utf-8")
        if self.token:
            req.add_header("Authorization", "Bearer " + self.token)
        try:
            with urllib.request.urlopen(req) as r:
                raw = r.read().decode("utf-8")
                return json.loads(raw) if raw else None
        except urllib.error.HTTPError as e:
            raise SystemExit(f"{method} {path} -> {e.code}: {e.read().decode('utf-8')[:600]}")


def slugify(name):
    """Mirrors InterestSlug.From on the server; a differing slug would miss the series and create it again."""
    out = []
    for ch in unicodedata.normalize("NFD", name.lower().replace("ß", "ss")):
        if unicodedata.category(ch) == "Mn":
            continue
        if ch.isalnum():
            out.append(ch)
        elif out and out[-1] != "-":
            out.append("-")
    return "".join(out).strip("-")


def vocab_notes(unit, rows):
    """Word fields plus the stumbling blocks of the list. The pairs themselves reach the agent through the
    vocabulary exercises (UnitVocabulary); repeating them here without translations would only cost prompt space."""
    head = f"Wortfelder: {unit['wortfelder']}."
    if not rows:
        return head + " Die Wortliste dieser Unit ist noch nicht erfasst (Vokabelseiten fotografieren)."
    pieces, budget = [], MAX_TEXT - len(head) - 20
    for _, w in rows:
        hint = (w["hinweis"] or "").strip()
        piece = f"{w['wort'].strip()} – {hint}"
        if not hint or len(piece) + 2 > budget:
            continue
        pieces.append(piece)
        budget -= len(piece) + 2
    return head + (" Stolpersteine: " + "; ".join(pieces) + "." if pieces else "")


def build_config(e):
    """Turns the cloze shortcuts into contract fields; everything else goes to the server as the author wrote it."""
    config = dict(e["config"])
    if e["typ"] == "cloze" and "markup" in config:
        text, gaps, _ = parse_cloze(config.pop("markup"))
        distractors = config.pop("distractors", None)
        config.update({"text": text, "gaps": gaps})
        if distractors is not None:
            # One tile per spelling the grader tells apart: "went" next to "Went." would show twice.
            bank = {}
            for word in [g["answer"] for g in gaps] + list(distractors):
                bank.setdefault(graded(word), word)
            config["wordBank"] = sorted(bank.values(), key=str.lower)
    return config


def import_pages(e):
    """The book page the folder names for an exercise (field "seiten"), or None when it names none."""
    pages = e.get("seiten")
    return (pages.strip() or None) if isinstance(pages, str) else None


def save_exercise(api, route, old, body, pages):
    """Creates the exercise, or replaces exercise old. PUT is a full replacement on the server: a missing bookPages
    clears the page a creator kept by hand, so without a page of its own the import carries the stored one over
    (B-589). A page from the folder wins over the stored one."""
    if old and pages is None:
        pages = api.call("GET", f"/creator/exercises/{old}").get("bookPages")
    if pages is not None:
        body = {**body, "bookPages": pages}
    return api.call("PUT", f"{route}/{old}", body) if old else api.call("POST", route, body)


def load(folder, book, words, exercises, args):
    api = Api(args.base, args.account, args.pin)
    r = book["reihe"]
    subject = next((s for s in api.call("GET", "/creator/subjects") if s["name"] == r["fach"]), None)
    if subject is None:
        raise SystemExit(f"Fach '{r['fach']}' gibt es auf dem Server nicht – Name prüfen oder im Vater-Web anlegen.")
    publisher = api.call("POST", "/creator/publishers", {"name": r["verlag"]})
    series_body = {"name": r["name"], "subjectId": subject["id"], "publisherId": publisher["id"],
                   "schoolTypes": r["schularten"], "sourceLanguage": r["source"], "targetLanguage": r["target"],
                   "notes": r.get("notes")}
    # The series is found by its slug (the server derives "…G9 – Band 3" and "…G9 Band 3" to the same one).
    found = [s for s in api.call("GET", "/creator/textbook-series?take=200") if s["slug"] == slugify(r["name"])]
    series = (api.call("PATCH", f"/creator/textbook-series/{found[0]['id']}", series_body) if found
              else api.call("POST", "/creator/textbook-series", series_body))
    if not series.get("isOwn"):
        raise SystemExit(f"Reihe {series['id']} gehört nicht diesem Konto.")
    sid = series["id"]
    print(f"Reihe {sid}: {series['name']}")

    # Grammar topics: POST is idempotent per subject and label (the server returns the existing one).
    for g in book.get("grammatik", []):
        api.call("POST", "/creator/grammar-topics",
                 {"label": g["label"], "subjectId": subject["id"], "synonyms": g.get("synonyme")})
    # A unit or exercise names a topic by its label or its slug; both resolve to the id.
    topics, slugs = {}, []
    for t in api.call("GET", f"/creator/grammar-topics?subjectId={subject['id']}&take=500"):
        topics[t["slug"]] = topics[t["label"].lower()] = t["id"]
        slugs.append(t["slug"])

    def topic_ids(refs, where):
        missing = [x for x in refs if x.lower() not in topics]
        if missing:
            raise SystemExit(f"{where}: Grammatik-Thema {missing} gibt es im Fach {r['fach']} nicht; "
                             f"vorhanden: {', '.join(sorted(slugs))}")
        return [topics[x.lower()] for x in refs]

    existing = {u["label"]: u for u in api.call("GET", f"/creator/textbook-series/{sid}/units")}
    unit_ids = {}
    for u in book["units"]:
        rows = words.get(u["nr"], ([], []))[1]
        body = {"label": u["label"], "grade": book["klasse"], "orderIndex": u["nr"], "topics": u["topics"],
                "grammar": u["grammar"], "vocabularyNotes": vocab_notes(u, rows),
                "grammarTopicIds": topic_ids(u.get("grammatik", []), u["label"])}
        res = (api.call("PATCH", f"/creator/textbook-series/{sid}/units/{existing[u['label']]['id']}", body)
               if u["label"] in existing else api.call("POST", f"/creator/textbook-series/{sid}/units", body))
        unit_ids[u["nr"]] = res["id"]
        print(f"  Unit {u['nr']} -> {res['id']}: {len(u['topics'])} Themen, Grammatik {len(u['grammar'])} Z., "
              f"{len(rows)} Wörter")

    p = book["profil"]
    profile_body = {"name": p["name"], "subjectId": subject["id"], "schoolTypes": r["schularten"],
                    "gradeMin": book["klasse"], "gradeMax": book["klasse"], "seriesId": sid,
                    "sourceLang": r["source"], "targetLang": r["target"], "persona": p["persona"],
                    "didactics": p["didactics"],
                    "defaultTypes": p.get("defaultTypes", ["Vocabulary", "Cloze", "Grammar", "Translation"]),
                    "active": True}
    mine = [x for x in api.call("GET", "/creator/profiles") if x["name"] == p["name"]]
    profile = (api.call("PATCH", f"/creator/profiles/{mine[0]['id']}", profile_body) if mine
               else api.call("POST", "/creator/profiles", profile_body))
    print(f"Profil {profile['id']}: {profile['name']}")

    # Word lists first: one vocabulary exercise per section, in the order the sections appear in the TSV.
    plan = []
    for u in book["units"]:
        rows = words.get(u["nr"], ([], []))[1]
        sections = list(dict.fromkeys(w["abschnitt"].strip() for _, w in rows))
        short = u.get("kurz") or f"Unit {u['nr']}"
        for i, section in enumerate(sections, 1):
            items = [{"front": w["wort"].strip(), "back": w["uebersetzung"].strip(),
                      "hint": (w["hinweis"] or "").strip() or None}
                     for _, w in rows if w["abschnitt"].strip() == section]
            label = u.get("abschnitte", {}).get(section, section)
            plan.append({"unit": u["nr"], "typ": "vocabulary", "titel": f"{short} · Wörter {label}",
                         "reihenfolge": i, "grammatik": [],
                         "beschreibung": f"Wortliste {label}, {short} ({len(items)} Einträge).",
                         "config": {"direction": "both", "sourceLang": r["source"], "targetLang": r["target"],
                                    "items": items}})
    plan += exercises

    meta = {"gradeMin": book["klasse"], "gradeMax": book["klasse"], "schoolTypes": r["schularten"]}
    have = {}
    for nr, uid in unit_ids.items():
        have[nr] = {e["title"]: e["id"] for e in
                    api.call("GET", f"/creator/exercises?seriesUnitId={uid}&mineOnly=true&take=500")}
    # Every exercise of the folder goes through the self-test, the ones left standing too: after a run that stopped
    # halfway, the next one would otherwise find them "already there" and never test them.
    touched = []
    print("Übungen:")
    for e in plan:
        uid = unit_ids[e["unit"]]
        body = {"title": e["titel"], "orderIndex": e.get("reihenfolge", 0), "rewardPoints": e.get("punkte", 20),
                "config": build_config(e), "description": e.get("beschreibung"),
                "grammarTopicIds": topic_ids(e.get("grammatik", []), e["titel"]),
                "source": f"{book['quelle']}, Unit {e['unit']}", **meta}
        route = f"/creator/textbook-series/{sid}/units/{uid}/{e['typ']}"
        old = have[e["unit"]].get(e["titel"])
        if old and not args.ersetzen:
            print(f"  = {e['titel']} (schon da)")
            touched.append((old, e["titel"]))
            continue
        res = save_exercise(api, route, old, body, import_pages(e))
        touched.append((res["id"], e["titel"]))
        print(f"  {'~' if old else '+'} {e['titel']} -> Übung {res['id']}")

    if args.child:
        child_setup(api, book, subject, sid, unit_ids, args.child)

    print(f"Selbsttest (Testmodus, nebenwirkungsfrei) über alle {len(touched)} Übungen des Ordners:")
    failed = 0
    for eid, title in touched:
        stage = typed_stage(api, eid)
        typed = stage is not None
        # A type without any typed stage (Birkenbihl) grades the self-assessment, not a text; claiming "known" there
        # tests only that the items exist, and the line says so instead of failing every run.
        first = api.call("POST", f"/creator/exercises/{eid}/preview/check", {"answers": [], "stage": stage})
        answers = [{"itemIndex": o["itemIndex"], "givenAnswer": o["expected"], "wasKnown": None if typed else True}
                   for o in first["items"]]
        res = api.call("POST", f"/creator/exercises/{eid}/preview/check", {"answers": answers, "stage": stage})
        # correct/total, not the rounded percentage: 199 of 200 rounds to 100.
        failed += res["correct"] != res["total"]
        note = "" if typed else "  (ohne getippte Stufe – nur Inhalt geprüft)"
        print(f"  {res['scorePercent']:3d} %  {res['correct']}/{res['total']}  {title}{note}")
    if not touched:
        print("  (keine Übung im Ordner – nichts zu prüfen)")
    return failed


def typed_stage(api, eid):
    """The exercise's own preview stage when it is typed, else the first typed stage of its type, else None.

    An exercise the father switched to a self-assessment stage in the Vater-Web still has typed stages; checking it on
    its default stage would only prove that its items exist."""
    data = api.call("GET", f"/creator/exercises/{eid}/preview")
    if data["typed"]:
        return data["stage"]
    return next((o["value"] for o in data["stages"]
                 if api.call("GET", f"/creator/exercises/{eid}/preview?stage={o['value']}")["typed"]), None)


def child_setup(api, book, subject, sid, unit_ids, cid):
    """Grade, school type, textbook and the planned class test of the child follow the book.

    The agent reads the child's FIRST textbook of the subject (ChildFacts.PrimaryTextbook); a second book next to an
    old one would stay invisible. So the existing book of the subject is turned into this one instead of adding one.
    """
    k = book.get("kind", {})
    unit_nr = k.get("unit", book["units"][0]["nr"])
    unit = next(u for u in book["units"] if u["nr"] == unit_nr)
    school = book["reihe"]["schularten"].split(",")[0].strip()
    api.call("PATCH", f"/supervisor/children/{cid}", {"grade": book["klasse"], "schoolType": school})
    books = [b for b in api.call("GET", f"/supervisor/children/{cid}/textbooks") if b.get("subjectId") == subject["id"]]
    body = {"title": book["reihe"]["name"], "subjectId": subject["id"], "seriesId": sid,
            "currentUnitId": unit_ids[unit_nr], "currentChapter": unit["label"]}
    res = (api.call("PATCH", f"/supervisor/children/{cid}/textbooks/{books[0]['id']}", body) if books
           else api.call("POST", f"/supervisor/children/{cid}/textbooks",
                         {**body, "subjectName": None, "grade": None, "publisher": None, "isbn": None}))
    print(f"Kind {cid}: Klasse {book['klasse']}, {school}, Buch {res['id']} -> {res['seriesName']} / "
          f"{res['currentUnitLabel']}")

    test = k.get("klassenarbeit")
    if not test:
        return
    short = unit.get("kurz") or f"Unit {unit_nr}"
    lists = [e["id"] for e in api.call("GET", f"/creator/exercises?seriesUnitId={unit_ids[unit_nr]}"
                                               f"&mineOnly=true&take=500")
             if e["type"] == "Vocabulary" and e["title"].startswith(f"{short} · Wörter ")]
    planned = [t for t in api.call("GET", f"/supervisor/class-tests?childId={cid}")
               if t["subjectId"] == subject["id"] and t["status"] == "Planned"]
    if not planned:
        print("Klassenarbeit: keine geplante im Fach – im Vater-Web mit Datum anlegen, dann erneut laden.")
        return
    api.call("PATCH", f"/supervisor/class-tests/{planned[0]['id']}", {"title": test["titel"], "topic": test["thema"]})
    if lists:
        api.call("POST", f"/supervisor/class-tests/{planned[0]['id']}/exercises", {"exerciseIds": lists})
    print(f"Klassenarbeit {planned[0]['id']}: {test['titel']}, {len(lists)} Wortlisten")


def main():
    ap = argparse.ArgumentParser(description="Lehrwerk-Ordner prüfen und über die API laden.")
    ap.add_argument("ordner")
    ap.add_argument("--pruefen", action="store_true", help="nur prüfen, keine API")
    ap.add_argument("--base", default="http://localhost:5200")
    ap.add_argument("--account", type=int, default=1)
    ap.add_argument("--pin", default=os.environ.get("PUGLING_PIN"))
    ap.add_argument("--child", type=int, help="Kind auf dieses Buch umstellen (Klasse, Schulart, Buch, Klassenarbeit)")
    ap.add_argument("--ersetzen", action="store_true", help="vorhandene Übungen gleichen Titels überschreiben")
    args = ap.parse_args()

    book, words, exercises = read_book(args.ordner)
    errors, warnings = check(book, words, exercises)
    n_words = sum(len(rows) for _, rows in words.values())
    print(f"{book.get('reihe', {}).get('name', '?')}: {len(book.get('units', []))} Units, {n_words} Wörter in "
          f"{len(words)} Listen, {len(exercises)} Start-Übungen")
    for w in warnings:
        print("  Hinweis:", w)
    for e in errors:
        print("  FEHLER:", e)
    if errors:
        raise SystemExit(f"{len(errors)} Fehler – nichts geladen.")
    if args.pruefen:
        print("Prüfung bestanden." + (f" {len(warnings)} Hinweise ansehen." if warnings else ""))
        return
    if not args.pin:
        raise SystemExit("PIN fehlt: --pin oder Umgebungsvariable PUGLING_PIN (nie in eine Datei schreiben).")
    failed = load(args.ordner, book, words, exercises, args)
    if failed:
        raise SystemExit(f"ACHTUNG: {failed} Übung(en) bestehen ihre eigenen Lösungen nicht.")
    print("Fertig.")


if __name__ == "__main__":
    main()
