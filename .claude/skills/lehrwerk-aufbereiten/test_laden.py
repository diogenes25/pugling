"""Offline-Tests für laden.py (B-492) – Standardbibliothek, kein Server, keine Fotos.

    python -m unittest discover -s .claude/skills/lehrwerk-aufbereiten -p "test_*.py"

Läuft in CI (.github/workflows/ci.yml). Liest die gemeinsame Fallliste des Antwortvergleichs (B-493,
docs/fallliste-antwortvergleich.json): ändert jemand graded() oder den Server allein, fällt dieser Test oder sein Zwilling.
"""
import importlib.util
import json
import os
import types
import unittest
from unittest import mock

HIER = os.path.dirname(os.path.abspath(__file__))
WURZEL = os.path.abspath(os.path.join(HIER, "..", "..", ".."))

_spec = importlib.util.spec_from_file_location("laden", os.path.join(HIER, "laden.py"))
laden = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(laden)


def faelle():
    with open(os.path.join(WURZEL, "docs", "fallliste-antwortvergleich.json"), encoding="utf-8") as f:
        return json.load(f)["faelle"]


class Antwortvergleich(unittest.TestCase):
    """B-493: graded() sieht zwei Antworten genau dann als gleich, wenn der Server es tut."""

    def test_fallliste(self):
        liste = faelle()
        self.assertGreater(len(liste), 10)
        for fall in liste:
            with self.subTest(warum=fall["warum"]):
                self.assertEqual(laden.graded(fall["a"]) == laden.graded(fall["b"]), fall["gleich"])


class Lueckentext(unittest.TestCase):
    """B-483/B-487/B-491: die Kurzschreibweise [Lösung|Alternative] wird zu Text, Lücken und Wortbank."""

    def test_klammern_werden_luecken(self):
        text, gaps, errors = laden.parse_cloze("We [went|did go] (go) to the [Tower].")
        self.assertEqual(errors, [])
        self.assertEqual(text, "We {{1}} (go) to the {{2}}.")
        self.assertEqual([g["answer"] for g in gaps], ["went", "Tower"])
        self.assertEqual(gaps[0]["alternatives"], ["did go"])

    def test_fehler_werden_genannt(self):
        self.assertTrue(laden.parse_cloze("Kein Platzhalter.")[2])
        self.assertTrue(laden.parse_cloze("Leer [] hier.")[2])
        self.assertTrue(laden.parse_cloze("Selbst {{1}} geschrieben [x].")[2])

    def test_wortbank_ohne_doppelte_kacheln(self):
        # "went" und "Went." sind für den Vergleich dasselbe – eine Kachel, nicht zwei (B-491).
        config = laden.build_config({"typ": "cloze", "config": {"markup": "I [went] home.", "distractors": ["Went.", "go"]}})
        self.assertEqual(sorted(w.lower() for w in config["wordBank"]), ["go", "went"])
        self.assertNotIn("markup", config)


class FakeApi:
    """Records the calls; GET of an exercise detail answers with the page stored on the server."""

    def __init__(self, stored_pages=None):
        self.calls, self.stored_pages = [], stored_pages

    def call(self, method, path, body=None):
        self.calls.append((method, path, body))
        if method == "GET":
            return {"id": 5, "bookPages": self.stored_pages}
        return {"id": 5}

    def written(self):
        return next(body for method, _, body in self.calls if method in ("PUT", "POST"))


class Buchseite(unittest.TestCase):
    """B-589: --ersetzen schickt ein PUT als volle Darstellung – ohne bookPages ginge die gepflegte Seite verloren."""

    ROUTE = "/creator/textbook-series/1/units/2/matching"

    def test_put_ohne_eigene_seite_behaelt_die_gespeicherte(self):
        # Rot, wenn save_exercise die gespeicherte Seite nicht nachliest (GET-Zeile entfernt).
        api = FakeApi(stored_pages="42")
        laden.save_exercise(api, self.ROUTE, 5, {"title": "T"}, laden.import_pages({}))
        self.assertEqual(api.written().get("bookPages"), "42")
        self.assertEqual(api.calls[-1][:2], ("PUT", self.ROUTE + "/5"))

    def test_seite_aus_dem_ordner_gewinnt(self):
        # Rot, wenn die gespeicherte Seite den Importwert überschreibt.
        api = FakeApi(stored_pages="42")
        laden.save_exercise(api, self.ROUTE, 5, {"title": "T"}, laden.import_pages({"seiten": " 7 "}))
        self.assertEqual(api.written().get("bookPages"), "7")

    def test_leere_seite_im_ordner_zaehlt_als_keine(self):
        # Rot ohne strip in import_pages: "  " ginge als Importwert durch, der Server trimmt es zu null (Seite weg).
        api = FakeApi(stored_pages="42")
        laden.save_exercise(api, self.ROUTE, 5, {"title": "T"}, laden.import_pages({"seiten": "  "}))
        self.assertEqual(api.written().get("bookPages"), "42")

    def test_neu_anlegen_fragt_nicht_nach_und_setzt_nur_die_eigene_seite(self):
        # Rot, wenn das Nachlesen auch ohne vorhandene Übung läuft (GET /creator/exercises/None).
        api = FakeApi(stored_pages="42")
        laden.save_exercise(api, self.ROUTE, None, {"title": "T"}, None)
        self.assertEqual([m for m, _, _ in api.calls], ["POST"])
        # Absichtserklärung, kein Mutant: ohne alte und ohne eigene Seite bleibt das Feld weg (Server: null).
        self.assertNotIn("bookPages", api.written())
        api = FakeApi()
        laden.save_exercise(api, self.ROUTE, None, {"title": "T"}, laden.import_pages({"seiten": "WB 12"}))
        self.assertEqual(api.written().get("bookPages"), "WB 12")

    BOOK = {"reihe": {"name": "R", "verlag": "V", "fach": "Englisch", "schularten": ["Gymnasium"],
                      "source": "de", "target": "en"},
            "klasse": 7, "quelle": "Q", "profil": {"name": "P", "persona": "p", "didactics": "d"},
            "units": [{"nr": 1, "label": "Unit 1", "topics": ["t"], "grammar": "g", "wortfelder": "w"}]}

    def test_ersetzen_ueber_load_behaelt_die_gespeicherte_seite(self):
        # Rot, wenn load() am Schreiben vorbei direkt PUT ruft (Ausgangszustand vor B-589): die Seite fehlte im PUT.
        calls = []

        class Server:
            def __init__(self, *_):
                pass

            def call(self, method, path, body=None):
                calls.append((method, path, body))
                if path == "/creator/subjects":
                    return [{"id": 3, "name": "Englisch"}]
                if path.startswith("/creator/textbook-series?"):
                    return []
                if path == "/creator/textbook-series":
                    return {"id": 1, "name": "R", "isOwn": True}
                if path.startswith("/creator/grammar-topics?") or path.endswith("/units") and method == "GET":
                    return []
                if path == "/creator/profiles" and method == "GET":
                    return []
                if path.startswith("/creator/exercises?"):
                    return [{"id": 5, "title": "T"}]
                if path == "/creator/exercises/5":
                    return {"id": 5, "bookPages": "42"}
                if path.endswith("/preview"):
                    return {"typed": True, "stage": "typed"}
                if path.endswith("/preview/check"):
                    return {"items": [], "correct": 1, "total": 1, "scorePercent": 100}
                return {"id": 2, "name": "x"}

        e = {"unit": 1, "typ": "matching", "titel": "T", "config": {"instruction": "i", "pairs": []}}
        args = types.SimpleNamespace(base="http://x", account=1, pin="0000", ersetzen=True, child=None)
        with mock.patch.object(laden, "Api", Server), mock.patch("builtins.print"):
            laden.load("ordner", self.BOOK, {}, [e], args)
        put = [body for method, path, body in calls if method == "PUT"]
        self.assertEqual(len(put), 1)
        self.assertEqual(put[0].get("bookPages"), "42")

    def test_pruefen_lehnt_ungueltige_seite_ab(self):
        # Rot ohne die Prüfung in check(): der Server antwortete mitten im Lauf mit 400 validation_error.
        book = self.BOOK

        def fehler(seiten):
            e = {"unit": 1, "typ": "matching", "titel": "T", "seiten": seiten,
                 "config": {"instruction": "i", "pairs": [{"left": "a", "right": "b"}]}}
            return [x for x in laden.check(book, {}, [e])[0] if "seiten" in x]

        self.assertEqual(fehler("x" * 20), [])
        self.assertTrue(fehler("x" * 21))
        self.assertTrue(fehler(42))
        self.assertTrue(fehler("   "))


if __name__ == "__main__":
    unittest.main()
