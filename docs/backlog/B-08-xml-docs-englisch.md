---
tags: [typ/story, status/in-arbeit, bereich/doku]
aliases: [XML-Docs auf Englisch, Doku-Übersetzung]
status: in-arbeit
prio: P3
art: Aufräumen
groesse: S
wo: doku
migration: nein
vertragsbruch: nein
quelle: docs/translate.md
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

1. `Models/` und `Data/` sind übersetzt, mit demselben Glossar.
2. Branch nach `db-struktur-umbau` gemergt.
3. Die Konvention „Doku auf Deutsch" in `CLAUDE.md` ist auf den neuen Stand gezogen.

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
