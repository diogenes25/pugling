---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/katalog, rolle/creator]
aliases: [Verlag doppelt nach Umbenennen, DuplicatePublisher greift zu spät]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zum Sprint 1 des Nachtlaufs (2026-08-09), Fund 2
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-136 · Beim Verlag steht dieselbe Dublettenlücke wie bei der Reihe

Die Defektklasse aus [B-133](B-133-zwei-reihen-ein-anzeigename.md), eine Ressource weiter: Der
Verlags-Wächter vergleicht Slug gegen Slug, und der Slug friert beim Umbenennen ein.

## User Story

Als **Creator** möchte ich, dass ein Verlagsname genau einen Verlag meint — sonst stehen im
Auswahlfeld einer Reihe zwei ununterscheidbare „Cornelsen".

## Ist-Stand am Code

- `Controllers/Creator/PublishersController.cs:77-78` — `Create` trifft eine bestehende Zeile **nur**
  über den Slug und gibt sie idempotent zurück.
- `Controllers/Creator/PublishersController.cs:111-113` — `Update` prüft Slug gegen Slug.
- `ApiErrors.DuplicatePublisher` existiert bereits.
- `Data/PuglingDbContext.cs` — `Publisher.Name` trägt seit [B-128](B-128-katalogsuche-case-sensitiv.md)
  die `NOCASE`-Collation. Die ist heute **wirkungslos**: es gibt im Backend keinen einzigen
  Gleichheitsvergleich auf `Publisher.Name` (nachgemessen vom Reviewer). Diese Story macht sie tragend.

Der Ablauf ist derselbe wie bei der Reihe: Verlag „Klett" (Slug `klett`) in „Cornelsen" umbenennen →
`POST {name:"Cornelsen"}` → Slug `cornelsen` ist frei → zweiter Verlag „Cornelsen".

## Die echte Lücke

Nicht „eine vergessene Prüfung", sondern **dieselbe Klasse zum dritten Mal**: B-124 hat sie beim
Umbenennen der Reihe geschlossen, B-133 beim Anzeigenamen der Reihe, und beim Verlag steht sie noch.
Der eigentliche Befund ist, dass „idempotent über den Slug" und „Anzeigename ist eindeutig" zwei
verschiedene Zusicherungen sind, die im Code wie eine aussehen.

## Offene Punkte

1. **Nur die Prüfung, oder gleich das Muster festhalten?** Beim dritten Auftreten derselben Klasse ist
   ein geteilter Helfer oder ein Wächter zu erwägen. Empfehlung: erst die Prüfung bauen (sie ist
   fünf Zeilen, wörtlich wie in `TextbookSeriesController`), und den Wächter erst, wenn eine **vierte**
   slug-idempotente Ressource auftaucht — `InterestTag` ist die Kandidatin, dort ist derselbe Fall
   ungeprüft.
2. **Gilt es auch für `InterestTag`?** Nicht erhoben. Vor dem Bau `InterestTagsController` gegenlesen —
   er ist nach demselben Muster gebaut (slug-idempotent, `DuplicateInterestTag` existiert).

## Akzeptanzkriterien

1. `POST` und `PATCH` auf einen Verlagsnamen, den ein **anderer** Verlag trägt, antworten `409
   duplicate_publisher` — auch wenn die Slugs verschieden sind.
2. Ein idempotenter Slug-Treffer, dessen Anzeigename **nicht** zum geposteten Namen passt, gibt nicht
   still die falsche Zeile heraus (die Spiegelseite, die in B-133 der Reviewer gefunden hat).
3. Die Idempotenz bleibt: derselbe Name liefert denselben Verlag.
4. Je ein Integrationstest, vorher rot.

## Verlauf

- **2026-08-09** — angelegt aus dem `pugling-reviewer`-Befund zu Sprint 1 des Nachtlaufs, Fund 2.
  **Bewusst nicht in B-133 mitgenommen:** dessen Ziel (die Reihe) ist ohne den Verlag erfüllt, und der
  Fehler liegt außerhalb des Diffs — er ist älter als dieser Sprint. Der Ist-Stand stammt aus dem Review
  und ist Zeile für Zeile belegt.
