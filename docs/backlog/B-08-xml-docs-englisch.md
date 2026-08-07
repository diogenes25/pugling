---
tags: [typ/story, status/abgenommen, bereich/doku]
aliases: [XML-Docs auf Englisch, Doku-Übersetzung]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: doku
migration: nein
vertragsbruch: nein
quelle: docs/translate.md
nachgeschaut: "2026-08-07"
---

# B-08 · XML-Doc-Kommentare im Backend auf Englisch übersetzen

**Sammel-Story, keine Kopie.** Glossar (~60 zentral festgelegte Fachbegriffe), Fortschritt je Etappe und die
Fallstricke stehen in [translate.md](../translate.md) — einem *lebenden Nachschlagewerk*, das nur die
steuernde Sitzung pflegt.

## User Story

Als Entwickler möchte ich internationales Englisch in der Code-Dokumentation, damit die Prosa nicht mehr
gegen die englischen Typnamen anläuft (der `Adult`/`Vater`-Fall ist in `CLAUDE.md` selbst dokumentiert).

## Ist-Stand (am 2026-07-30 gegen Branch und Notiz geprüft)

**Weitgehend erledigt, nicht offen** — beim Ernten korrigiert: alle `///`-Kommentare der fünf
Backend-Projekte sind übersetzt (278 Dateien, 4210 Zeilen, 6 Commits) auf Branch
**`docs/xml-docs-englisch`**, abgezweigt von `db-struktur-umbau`@69397fe.

Offen sind genau zwei Dinge:

1. **Etappe 7:** `backend/Pugling.Api/Models/` und `Data/` (398 `<summary>`) blieben deutsch, weil eine
   parallele Sitzung am DB-Layer arbeitete.
2. **Der Merge:** Der Branch hat E0–E6 des DB-Umbaus als Vorfahren und gehört nach `db-struktur-umbau`
   gemergt, **nicht** direkt auf `main`.

Ausdrücklich **nicht** Teil dieser Story: `//`-Inline-Kommentare (~1900 Zeilen) und die Markdown-Doku
(~16k Zeilen) — das wäre ein eigener Plan. Der deutsche Rest an *Laufzeit*-Texten (Ledger-Buchungen,
Content-Platzhalter) hängt an B-30 und ist ein anderer Korpus.

## Entscheidungen

→ [translate.md](../translate.md): das **Glossar** (~60 Begriffe, u. a. „Vater" → adult/father/supervisor je
Bedeutung, „Etappe" → milestone bei bleibendem Codetyp `KeyResult`) und die Regel, dass **nur die steuernde
Sitzung** es pflegt. Nicht hier wiederholen — ein zweites Glossar wäre eine zweite Terminologie.

## Akzeptanzkriterien

1. ~~`Models/` und `Data/` sind übersetzt, mit demselben Glossar.~~ — erledigt (Etappe 7 am 2026-08-01,
   24 Dateien / 398 `<summary>`); am 2026-08-04 gegengeprüft: **null** deutsche Signalwörter in `///`-Zeilen
   unter `Models/` und `Data/`.
2. ~~Branch nach `db-struktur-umbau` gemergt.~~ — erledigt, Merge-Commit `08c2dcf` („Englische XML-Docs mit
   dem DB-Umbau zusammengefuehrt"); der Branch `docs/xml-docs-englisch` existiert nicht mehr, `main` enthält
   alle Übersetzungs-Commits.
3. ~~Die Konvention „Doku auf Deutsch" in `CLAUDE.md` ist auf den neuen Stand gezogen.~~ — erledigt
   (`CLAUDE.md:94` „Code-Doku auf Englisch – ausnahmslos"); ein Vollscan über alle `CLAUDE.md` findet keine
   zweite, widersprechende Sprachregel mehr.

## Schätzung

**Größe: S** (nicht L — der Großteil ist gebaut) — 398 `<summary>` in zwei Ordnern plus ein Merge.

- **Reihenfolge:** Etappe 7 kollidiert mit **B-07**: E11 (String-Längen), E12 und E13 arbeiten genau an
  `Models/` und `Data/`. Erst die DB-Etappen, dann übersetzen — sonst wird zweimal dieselbe Datei angefasst
  und der Merge teuer.
- **Risiko (aus der Notiz, nicht neu erhoben):** ein rohes `<` in XML-Docs bricht den Build (CS1570, `>`
  ist erlaubt); nach jedem Block prüfen, dass **keine geänderte Zeile ohne `///`** im Diff steht — Agenten
  rutschen sonst in `//`-Kommentare.
- **Testweg:** `dotnet build Pugling.sln` (Warnungen sind Fehler, CS1591 ist scharf) + Sichtprüfung der
  Swagger-Ausgabe.

## Verlauf

- **2026-07-30** — als Sammel-Story geerntet, zunächst falsch als „offen, Größe L" beschrieben.
  Am selben Tag gegen Branch `docs/xml-docs-englisch` und die Memory-Notiz korrigiert: nur Etappe 7
  (`Models/` + `Data/`) und der Merge sind offen — Stufe daher `in-arbeit`, Größe S.
- **2026-08-04** — **abgenommen.** Im Plandokument [translate.md](../translate.md) steht keine offene Etappe
  mehr (0–9 alle „durch", Kopf-Tag `status/abgeschlossen`) — die Bedingung der Sammel-Story ist damit
  erfüllt. Nicht abgeschrieben, sondern nachgemessen:
  - **Testzahl:** `dotnet test Pugling.sln -c Release` → **687/687 grün**, 0 Warnungen (die
    Fortschrittstabelle nennt noch 615 — die Suite ist seit dem 2026-08-01 gewachsen, der Stand dort ist
    nicht falsch, nur älter). Das ist zugleich der im Testweg genannte `dotnet build Pugling.sln`, denn
    `TreatWarningsAsErrors` macht CS1591/CS1570 zum Fehler.
  - **Restefund:** Ein Scan über alle fünf Backend-Projekte auf deutsche Signalwörter (`der|die|das|und|für|
    nicht|wird|eine|ist|Gibt|Liefert|Erstellt|Eindeutige|…`) findet in `///`-Zeilen **6** Treffer, alle
    Fehlalarm (englisches „would **die** with a stack trace" in `DraftRules.cs:34`; fünfmal die deutschen
    Artikel als *Inhalt* der Genus-Doku in `VocabBaseTypes.cs:39-54`), und in `//`-Zeilen **0**.
  - **Merge:** `08c2dcf`; die Übersetzungs-Commits `2250ae7`, `2595d2d`, `f68a048`, `5197833`, `ce5b357`,
    `2ccee38`, `f671ddb`, `109ca72` sind in `main`.
  - **Doku-Tor** (`wo: doku`): `npx markdownlint-cli2` **war zuerst rot** — 6 Befunde in drei *fremden*
    Stories (`B-12`, `B-13`, `B-71`) aus der noch uncommitteten Grooming-Runde: dreimal MD036
    (`**Risiken**` ohne Doppelpunkt liest der Linter als Überschrift) und dreimal MD029, weil B-13s
    Offene-Punkte-Liste die Durchstreichung **vor** die Nummer setzte (`~~1. …~~`) und damit die ersten
    zwei Einträge gar keine Listenelemente waren. Beides in der Form korrigiert, die der Bereich sonst
    verwendet (`**Risiken:**`, `1. ~~…~~`); danach **0 Befunde in 172 Dateien**. In B-08 selbst war kein
    Befund.

  Bewusst **nicht** gefahren: `/smoke-test` bzw. ein E2E. Die Story hat kein Verhalten geändert — geliefert
  wurde Kommentartext, und kein Test liest `///`-Wortlaut ([translate.md](../translate.md), Abschnitt
  „Ausgangsbefund"). Ein grüner Durchstich wäre hier ein Ritual ohne Aussage; die tragende Zusicherung ist
  der Build mit scharfem CS1591/CS1570 plus der Restefund oben.

  Über den Zuschnitt hinaus geliefert (kein Scope-Betrug, aber es gehört ins Protokoll): Etappe 8 hat die
  ~2650 `//`-Kommentare mitgenommen und Etappe 9 die Meldungstexte der Wächter — beides war hier
  ausdrücklich **nicht** Teil der Story. Die Konvention in `CLAUDE.md` deckt das inzwischen ab
  („ausnahmslos"), darum entsteht daraus keine Folge-Story.
- **2026-08-07** — Nachschau (Nachtlauf): Regex-Scan über `Models/*.cs` und `Data/*.cs` auf deutsche
  Signalwörter in `///`-Zeilen wiederholt — 23 Treffer, alle Fehlalarm (englischer Fließtext mit
  zufälligen Substring-Treffern). Kein Fund.
