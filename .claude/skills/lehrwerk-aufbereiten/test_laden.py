"""Offline-Tests für laden.py (B-492) – Standardbibliothek, kein Server, keine Fotos.

    python -m unittest discover -s .claude/skills/lehrwerk-aufbereiten -p "test_*.py"

Läuft in CI (.github/workflows/ci.yml). Liest die gemeinsame Fallliste des Antwortvergleichs (B-493,
docs/fallliste-antwortvergleich.json): ändert jemand graded() oder den Server allein, fällt dieser Test oder sein Zwilling.
"""
import importlib.util
import json
import os
import unittest

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


if __name__ == "__main__":
    unittest.main()
