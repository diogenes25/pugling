---
tags: [typ/story, status/idee, bereich/shop, rolle/supervisor]
aliases: [Angebot bearbeiten übernimmt alten oder verworfenen Termin]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Review von B-782 im Nachtlauf A 2026-11-22 (docs/pm-sitzung-2026-11-22.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-786 · Das Bearbeiten eines Angebots schickt einen Termin, den der Vater nie gewählt hat

Zwei Wege im Bearbeiten-Formular einer Angebots-Zeile (`ListingRow`, `frontend/src/vater/VaterShop.tsx`) schicken einen
„einmalig“-Termin, den der Vater in diesem Formular nicht gewählt hat. Beide sind älter als B-782; B-782 hat sie weder
verursacht noch verschlimmert.

1. **Alter Termin beim Wechsel auf „einmalig“.** Der Server hält `RefillAtUtc` auch an einem nicht-einmaligen Angebot fest
   (`ShopController.UpdateListing`, `dto.RefillAtUtc ?? listing.RefillAtUtc`) und liefert ihn aus. Ein „wöchentlich“-Angebot,
   das früher einmal „einmalig“ war, belegt das Datumsfeld darum mit dem alten Termin vor. Stellt der Vater auf „einmalig“
   und bestätigt, ohne das Datum anzufassen, geht der alte Termin hinaus. Das erfüllt die Pflicht aus B-776 („Termin beim
   Wechsel verlangt“), obwohl kein Datum gewählt wurde, und liegt er in der Vergangenheit, füllt jedes Kind sofort auf.
2. **„Abbrechen“ setzt das Formular nicht zurück.** `plan`/`form` sind `useState` der Zeile; „Abbrechen“ schließt nur. Beim
   nächsten Öffnen steht das verworfene Datum im Feld; ändert der Vater dann nur den Preis, geht es als neuer Termin hinaus,
   und jeder Bestand wird geleert.

Am Code gelesen (Review-Hinweise 🟢 1 und 2 zu B-782), nicht im Browser gesehen.

Mögliche Reparatur (zu entscheiden): das Formular beim Öffnen jedes Mal aus `listing` neu belegen; `refillDate` leer
vorbelegen, wenn das Angebot nicht „einmalig“ ist.

## Verlauf

- **2026-11-22** — angelegt aus dem Review von B-782 (`idee`); bewusst nicht in B-782, weil dessen Sprint-Ziel ohne
  diese Wege erfüllt ist.
