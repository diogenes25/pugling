---
tags: [typ/story, status/abgenommen, bereich/shop, rolle/supervisor]
aliases: [Angebotstitel leeren meldet Erfolg und lässt ihn stehen]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Review von B-786 im Nachtlauf A 2026-11-22 (docs/pm-sitzung-2026-11-22.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: "2026-11-22"
---

# B-789 · Ein geleerter Angebotstitel wird als gespeichert gemeldet und bleibt stehen

Im Bearbeiten einer Angebots-Zeile (`ListingRow`, `frontend/src/vater/VaterShop.tsx`) wird ein leerer Titel zu `null`
(`title: form.title.trim() || null`). `ShopController.UpdateListing` liest `null` als „nicht angegeben“
(`if (dto.Title is not null) listing.Title = …`), und `UpdateShopListingDto` hat keinen `ClearTitle`-Schalter. Leert der
Vater den Titel, meldet die Oberfläche Erfolg, und der alte Titel steht weiter da – die PATCH-Falle aus
`backend/CLAUDE.md` („PATCH-Semantik“). Dasselbe gilt vermutlich für `Description`, die das Vater-Web nicht bearbeitet.

Am Code gelesen (Review-Hinweis 🟢 3 zu B-786), nicht im Browser gesehen. Älter als B-782/B-786.

Mögliche Reparatur (zu entscheiden): `ClearTitle` im Update-DTO samt Fall in `PatchSemanticsTests`, das Vater-Web schickt
ihn bei leerem Feld; oder ein leerer String heißt am Server „leeren“.

## Am Code nachgesehen

- Der Server leert schon heute mit einem leeren String: `UpdateListing` setzt `listing.Title = dto.Title.Trim()`, sobald
  `Title` nicht `null` ist (`ShopController.cs:306`), und `UpdateArticle` ebenso die Beschreibung (`:158`). Ein angelegtes
  Angebot ohne Titel trägt ohnehin `""` (`:233`), die Liste zeigt dann „(ohne Titel)“.
- Kaputt ist nur der Client: `ListingRow` schickt `form.title.trim() || null`, `ArticleRow` schickt
  `form.description.trim() || null` (`VaterShop.tsx:140`). Die Artikel-Beschreibung hat damit denselben Fehler – breiter als
  die Notiz.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf A 2026-11-22 (Defekt, autonom grillbar), unbestätigt.*

1. **Das Vater-Web schickt ein geleertes Feld als `""`**, nicht als `null`; der Server bleibt, wie er ist. Verworfen:
   `ClearTitle`/`ClearDescription` im Vertrag – zwei neue Schalter für etwas, das `""` heute schon leistet. Kosten: die
   Regel „leer heißt `""`“ steht nur am Code dieses Controllers; eine Festschreibung (AK 3) hält sie.
2. **Artikel-Beschreibung im selben Zug** (gleicher Fehler, gleiche Datei, gleiche Reparatur).

## Akzeptanzkriterien

1. Angebot bearbeiten, Titel leeren, OK → der PATCH trägt `title: ""`. Rot: `|| null` (Ausgangszustand).
2. Artikel bearbeiten, Beschreibung leeren, OK → der PATCH trägt `description: ""`. Rot: `|| null` (Ausgangszustand).
3. Festschreibung am Server: PATCH `title: ""` leert den Titel eines Angebots, PATCH `description: ""` die Beschreibung eines
   Artikels; `null` lässt beide stehen. Rot: der Controller prüft `IsNullOrWhiteSpace` statt `is not null`.

## Verlauf

- **2026-11-22** — angelegt aus dem Review von B-786 (`idee`); bewusst nicht in B-786 (anderes Feld, Vertrag betroffen).
- **2026-11-22** — umnummeriert von B-787 auf B-789: Lauf B hatte B-787/B-788 auf seinem Branch schon vergeben.
- **2026-11-22** — ausformuliert (Artikel-Beschreibung mit gleichem Fehler gefunden), gegrillt (stellvertretend, Defekt) und geschätzt (S, frontend, kein Vertrag) im Nachtlauf A; in-arbeit in Sprint 114.
  - AK 1 → `VaterShopLeeren.test.tsx` „Angebotstitel geht als leerer Text hinaus“, T1 (`|| null`) → rot; E2E `shop-felder-leeren`, E5 (= T1) → rot.
  - AK 2 → „Artikel-Beschreibung geht als leerer Text hinaus“, T2 → rot.
  - AK 3 → `ShopStockPerChildTests.B789_Leerer_Text_Leert_Titel_Und_Beschreibung_Null_Laesst_Sie_Stehen`, T3 (Titel per `IsNullOrWhiteSpace` übergangen) → rot, T4 (Beschreibung ebenso) → rot. Messung 01:18 nach der letzten Änderung, Grundlinie grün.
- **2026-11-22** — Review `frontend-reviewer`: kein 🟡/🔴, Zähler 0. 🟢: dieselbe Falle in Plan-Beschreibung, Unit-Notizen und Vokabel-Store → [B-791](B-791-geleerte-felder-ausserhalb-des-shops.md); Sohn-Shop zeigt bei leerem Angebotstitel den Artikeltitel (`MeController.cs:414`).
- **2026-11-22** — abgenommen (`28fce14d`, Sprint 114): Rollengang Vater per E2E `shop-felder-leeren` gegen frisch gestarteten Server; Sohn Regressionszeuge (`shop-verlauf`, `full-flow`). Backend 2024/2024 (Release).
- **2026-11-22** — nachgeschaut (Nachtlauf 2026-11-23-a, Retro Sprint 115). Prüfpunkte: (a) beide geänderten Aufrufe in
  `28fce14d` (`VaterShop.tsx` `ArticleRow.save`, `ListingRow` OK) schicken das getrimmte Feld; (b) die übrigen Textfelder der
  Angebotszeile reisen über `...form` mit ihrem geladenen Wert, kein weiteres `|| null` im Update-Pfad des Shops
  (`grep "|| null" VaterShop.tsx`: nur `:63` Anlegen und `:348` Anlegen); (c) `ShopStockPerChildTests.B789_…` prüft beide
  Richtungen (`""` leert, `null` lässt stehen). Kein Befund im Increment; dieselbe Klasse außerhalb des Shops war schon als
  B-791 aus dem Review erfasst (kein Entgangenes).

