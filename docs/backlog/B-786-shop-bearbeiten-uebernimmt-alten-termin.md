---
tags: [typ/story, status/geschaetzt, bereich/shop, rolle/supervisor]
aliases: [Angebot bearbeiten übernimmt alten oder verworfenen Termin]
status: geschaetzt
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
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

## User Story

Als Vater möchte ich, dass „OK“ im Bearbeiten eines Angebots nur den Termin schickt, den ich in diesem Formular sehe und
gewählt habe – nicht einen alten oder einen, den ich mit „Abbrechen“ verworfen habe.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf A 2026-11-22 (Defekt, autonom grillbar), unbestätigt.*

1. **„Bearbeiten“ belegt das Formular jedes Mal frisch aus dem Angebot** (Titel, Preise, Menge, Auffüll-Plan, Zielgruppe).
   „Abbrechen“ verwirft damit wirklich. Verworfen: „Abbrechen“ setzt zurück – gleichwertig, aber zwei Stellen statt einer.
   Kosten: keine sichtbare.
2. **Das Datumsfeld ist leer vorbelegt, wenn das Angebot nicht „einmalig“ ist.** Wer auf „einmalig“ umstellt, muss ein Datum
   wählen; „OK“ bleibt bis dahin gesperrt (`planIncomplete`). Verworfen: den Server den Rest-Termin beim Wechsel weg von
   „einmalig“ löschen lassen – das wäre eine Vertragsänderung für einen Fall, den nur dieses Formular falsch macht. Kosten:
   ein Vater, der den alten Termin wirklich wieder will, tippt ihn neu.
3. **Die Artikel-Zeile bleibt außen vor**: sie trägt keinen Termin, ihr „Abbrechen“ hat keine Folge am Server.

## Akzeptanzkriterien

1. Ein „wöchentlich“-Angebot mit Rest-Termin: „Bearbeiten“, Auffüllen auf „einmalig“ → das Datumsfeld ist leer, „OK“
   gesperrt. Rot macht es: `refillDate` aus `listing.refillAtUtc` unabhängig von der Art (Ausgangszustand).
2. Ein „einmalig“-Angebot: „Bearbeiten“, Datum ändern, „Abbrechen“, „Bearbeiten“ → das Feld zeigt wieder den gespeicherten
   Termin; Preis ändern, „OK“ → der PATCH trägt den geladenen Termin. Rot macht es: der Plan wird beim Öffnen nicht neu belegt.
3. Wie 2, aber der Preis wird vor „Abbrechen“ geändert → nach dem erneuten Öffnen steht der gespeicherte Preis. Rot macht es:
   das Formular (`form`) wird beim Öffnen nicht neu belegt.
4. Ein „einmalig“-Angebot öffnet mit seinem Datum vorbelegt (B-782 AK 1 bleibt grün). Rot macht es: das Datum wird immer leer
   vorbelegt.

## Schätzung

**Größe: S** – eine Belegungsfunktion und ihr Aufruf beim Öffnen in `ListingRow`, vier Vitest-Fälle neben B-782.
`wo: frontend`, `migration: nein`, `vertragsbruch: nein`.

### Angriffsplan

1. `editState(listing)` liefert Formular, Plan und Zielgruppe; die `useState`-Initialisierer und „Bearbeiten“ nutzen sie.
2. `refillDate` nur bei `refillKind === "Once"` aus dem Termin.
3. Vitest in `VaterShopEinmalTermin.test.tsx` (AK 1–4); E2E-Rollengang als Erweiterung von `shop-einmal-termin-bearbeiten`.

## Verlauf

- **2026-11-22** — angelegt aus dem Review von B-782 (`idee`); bewusst nicht in B-782, weil dessen Sprint-Ziel ohne
  diese Wege erfüllt ist.
- **2026-11-22** — ausformuliert, gegrillt (stellvertretend, Defekt, autonom) und geschätzt im Nachtlauf A: S, frontend.
