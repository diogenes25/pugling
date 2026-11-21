---
tags: [typ/story, status/idee, bereich/shop, rolle/supervisor]
aliases: [Angebotstitel leeren meldet Erfolg und lässt ihn stehen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: "Review von B-786 im Nachtlauf A 2026-11-22 (docs/pm-sitzung-2026-11-22.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-787 · Ein geleerter Angebotstitel wird als gespeichert gemeldet und bleibt stehen

Im Bearbeiten einer Angebots-Zeile (`ListingRow`, `frontend/src/vater/VaterShop.tsx`) wird ein leerer Titel zu `null`
(`title: form.title.trim() || null`). `ShopController.UpdateListing` liest `null` als „nicht angegeben“
(`if (dto.Title is not null) listing.Title = …`), und `UpdateShopListingDto` hat keinen `ClearTitle`-Schalter. Leert der
Vater den Titel, meldet die Oberfläche Erfolg, und der alte Titel steht weiter da – die PATCH-Falle aus
`backend/CLAUDE.md` („PATCH-Semantik“). Dasselbe gilt vermutlich für `Description`, die das Vater-Web nicht bearbeitet.

Am Code gelesen (Review-Hinweis 🟢 3 zu B-786), nicht im Browser gesehen. Älter als B-782/B-786.

Mögliche Reparatur (zu entscheiden): `ClearTitle` im Update-DTO samt Fall in `PatchSemanticsTests`, das Vater-Web schickt
ihn bei leerem Feld; oder ein leerer String heißt am Server „leeren“.

## Verlauf

- **2026-11-22** — angelegt aus dem Review von B-786 (`idee`); bewusst nicht in B-786 (anderes Feld, Vertrag betroffen).
