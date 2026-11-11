---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [Foto zu Wortliste, Vokabelseiten einlesen, Lehrwerk-Import]
status: idee
prio: P1
art: Wunsch
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-470 · Aus Fotos der Vokabelseiten wird eine Wortliste – als wiederholbares Werkzeug statt als Handarbeit unter raw/

**Befund zur Machbarkeit:** Ja, aus Buchfotos entsteht brauchbarer Lernstoff. Aus 53 Fotos von *English G
Access G9, Band 3* wurden Stoffnotizen für fünf Units, 124 Wortpaare der Unit 1 (TSV mit Abschnitt und
Hinweis), ein Fachlehrer-Profil und elf Start-Übungen; jede besteht ihren Selbsttest. Die von Hand
geschriebenen Übungen (question tags, past progressive, unregelmäßige Verben, Wegbeschreibung) treffen den
Stoff der Unit und sind die besten im Bestand.

Der Weg dahin ist aber nicht das Produkt:

- Gelesen hat die Seiten ein großes Modell in einer Sitzung, nicht der Agent. `fill --images` (B-375) schreibt
  nur Stoffnotizen und scheitert mit dem lokalen Modell an dichten Seiten
  ([B-459](B-459-vision-modell-fuer-buchseiten.md)).
- Die Wortpaare, das Ladeskript und die Start-Übungen liegen unter `raw/english/access-pugling/` (gitignored,
  ohne Test). Units 2–5 haben keine Wortliste; der Vater müsste für jede Unit dieselbe Handarbeit wiederholen.
- Wortpaare sind genau der Teil, bei dem ein Lesefehler teuer ist (ein falsches Paar wird wochenlang gelernt)
  und den das Programm nicht aus dem Kopf des Modells nehmen darf (B-460, Entscheidung 2).

Vorschlag zum Ausformulieren: ein Schritt `fill --words --images <ordner>` (oder eine Seite im Vater-Web), der
aus Fotos der Vokabelseiten eine **prüfbare Liste** macht (Wort, Übersetzung, Alternativen, Hinweis, Abschnitt),
sie dem Vater zur Durchsicht zeigt und erst danach über den bestehenden Listen-Import (B-452) als
Vokabelübungen je Abschnitt anlegt. Unsichere Zeilen werden markiert statt geraten. Urheberrecht wie in B-375:
Wortpaare und Fakten ja, Aufgaben und Texte des Buchs nein; Fotos bleiben außerhalb des Repos.

Offen: welches Modell liest (lokal reicht nach B-459 nicht; ein gehostetes Modell heißt, Buchseiten verlassen
den Rechner), und wie Mehrfachbedeutungen ankommen
([B-462](B-462-mehrdeutige-uebersetzung-verlangt-alle-bedeutungen.md)).

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
