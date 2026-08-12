---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/frontend, bereich/backend, rolle/creator]
aliases: [Verlagssperre unsichtbar, Klett unloeschbar, ForeignSeriesCount]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: /code-review 2026-08-11 über 1867cfd..HEAD (Funde 1–3)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-127]
wartet_auf: ""
nachgeschaut: 2026-08-12
---

# B-150 · Die Verlags-Löschsperre war für den Vater unsichtbar — der Dialog versprach das Gegenteil

[B-127](B-127-verlag-loeschen-trifft-fremde.md) hat die Sperre eingebaut und ist am 2026-08-10 abgenommen
worden (Commit `f29ee1c`). Was dabei durchkam: die Oberfläche wusste nichts davon. Sie bot das Löschen
weiter an, versprach im Bestätigungsdialog ausdrücklich den Zustand *vor* der Sperre, und der Fehlertext
danach nannte eine Ursache, die im häufigsten Fall gar nicht zutraf.

Eigene Story statt einer Zeile im Verlauf von B-127, weil der Fehler in **schon abgenommener** Arbeit saß
— nur so zählt er in der Messung „was hat die Abnahme durchgelassen".

## User Story

Als **Creator** möchte ich sehen, dass ein Verlag gesperrt ist, *bevor* ich das Löschen bestätige — und
wenn ich doch dagegenlaufe, einen Satz lesen, der die tatsächliche Ursache nennt.

## Ist-Stand am Code

Stand `4da744f` (B-127 war `abgenommen`):

- `frontend/src/vater/PublisherAdmin.tsx:59-62` — der Bestätigungsdialog sagte
  „`N` Reihe(n) verlieren nur die Zuordnung und bleiben nutzbar", der Kommentar darüber
  „keine Sperre nötig". Beides war seit B-127 falsch. Das Gegenstück in `CatalogAdmin.tsx:90` war im
  selben Nachtlauf für B-144 nachgezogen worden, dieses hier nicht.
- `PublisherResponse` trug nur `SeriesCount` — die Zahl im Dialog **zählte fremde Reihen mit** und sagte
  nichts darüber, wem sie gehören. Die Oberfläche konnte die Sperre also gar nicht kennen.
- `PublishersController.cs:205` und `frontend/src/lib/api.ts:99` — „A series of another account points at
  this publisher." / „…eine Reihe eines anderen Kontos." Der Prädikat-Zweig `OwnerAdultId == null` ist
  aber bewusst mit gemeint, und auf einer geseedeten Datenbank ist er der **einzige** auslösende Fall:
  `Seed.cs:1011` hängt die eigentümerlose Reihe „Green Line 1" an den Verlag „Klett".
- Das Admin-Ventil, auf das die Doku von `Delete` verwies, ist über das Produkt nicht erreichbar:
  `Adult.IsAdmin` wird von keinem Endpunkt und keinem DTO geschrieben (nur im Test direkt am `DbContext`,
  `VerlagLoeschenSperreTests.cs:137`).

Der Vater sah also „1 Reihe(n) verlieren nur die Zuordnung", bestätigte, bekam einen 409, und der Satz
darin schickte ihn eine fremde Reihe suchen, die es nicht gab.

## Die echte Lücke

Nicht die Sperre — die ist richtig. „Klett" **soll** unlöschbar sein, weil das Löschen den Verlag einer
Zeile wegnähme, die der ganze geteilte Katalog benutzt. Die Lücke war, dass dieser Dauerzustand nirgends
ausgesprochen war: nicht im Vertrag (kein Feld, an dem die Oberfläche ihn ablesen könnte), nicht im
Dialog, nicht im Fehlertext und nicht in der Doku, die stattdessen ein Ventil versprach, das es über das
Produkt nicht gibt.

B-127 hat die Regel gebaut und ihre **Sichtbarkeit** vergessen. Der `frontend-reviewer` lief in jenem
Sprint und hat es nicht gefunden — er sah den Diff von B-127, und `PublisherAdmin.tsx` stand nicht darin.

## Entscheidungen

1. **Der Vertrag trägt die Sperre, nicht die Oberfläche.** Neu: `PublisherResponse.ForeignSeriesCount` —
   die Teilmenge der Reihen, die dem Aufrufer nicht gehören (fremd **oder** eigentümerlos). Das ist genau
   das Prädikat, an dem `Delete` entscheidet. **Begründung:** die Alternative wäre, dass die Oberfläche
   die Eigentumsregel nachbaut — die zweite Fassung derselben Regel, und nach der Erfahrung dieses Repos
   gewinnt dann die veraltete. **Kosten:** die Projektion braucht die `fid` und muss die Eigentumsprüfung
   inline ausschreiben (EF kann `IsOwnedBy` nicht übersetzen); das Feld ist relativ zum Aufrufer, nicht
   absolut, und wer es als Verlags-Eigenschaft liest, liest den falschen Sitz.
2. **Der Löschen-Knopf ist gesperrt statt anklickbar**, mit der Begründung im `title`.
   **Begründung:** im geseedeten Katalog ist die Sperre ein Dauerzustand — ein Knopf, der nie funktionieren
   kann, gehört nicht angeboten. **Kosten:** ein Admin (den es im UI nicht gibt) sähe den Knopf ebenfalls
   gesperrt; das ist hier folgenlos, wäre aber falsch, sobald es eine Admin-Oberfläche gibt.
3. **Der Meldungstext nennt beide Fälle**, englisch wie deutsch. **Begründung:** der genannte Fall war der
   einzige, der auf einer geseedeten Datenbank *nicht* zutrifft. **Kosten:** der Satz wird länger.
4. **Das Admin-Ventil wird als das benannt, was es ist** — ein Break-Glass-Flag in der Datenbank —, statt
   als Ausweg für den Aufrufer. **Begründung:** die alte Fassung behauptete, die Sperre könne keine Falle
   sein; sie kann es, und zwei Creator am selben Verlag sitzen wirklich fest, bis ein Betreiber eingreift.
   **Kosten:** die Doku gibt eine Einschränkung zu, statt sie wegzuformulieren. Gewollt.

## Akzeptanzkriterien

1. `GET creator/publishers` liefert je Zeile `foreignSeriesCount`; für den Eigentümer der einzigen Reihe
   ist er 0, für ein fremdes Konto am selben Verlag 1 — die Zahl ist relativ zum Aufrufer.
2. Der Löschen-Knopf ist gesperrt, sobald `foreignSeriesCount > 0`, und nennt den Grund.
3. Der Bestätigungsdialog nennt nur noch die **eigenen** Reihen.
4. Die deutsche Fassung von `publisher_in_use` nennt den eigentümerlosen Fall.

## Schätzung

**Größe: S**, `wo: beides` (Backend zuerst — das Feld muss im Vertrag stehen, bevor die Oberfläche es
lesen kann), keine Migration, kein Vertragsbruch (das Feld ist **additiv**). Testweg:
`VerlagLoeschenSperreTests.ForeignSeriesCount_ZeigtDieSperre_VorDemLoeschen` (neu) und
`frontend/src/lib/errorMessage.test.ts` für den zweiten Halbsatz.

## Verlauf

- **2026-08-11** — angelegt **und abgenommen** in einem Zug, nachträglich zur Behebung. Gefunden von
  `/code-review` über `1867cfd..HEAD`, behoben und committet als `d36a11a`, bevor diese Story existierte —
  das war der Fehler: die Funde standen zuerst nur als Verlaufszeile an B-127 und hätten damit in der
  Messung gefehlt (README → „Warum der Defekt eine eigene Story braucht"). Nachgeholt beim nächsten
  `/backlog`-Lauf am selben Tag.
  Belegt: Backend **814/814**, Vitest **204/204**, `tsc -b` sauber, Commit `d36a11a`.
  **Kein Rollengang** an der laufenden App: Alle Löschpfade hängen an `confirmAction`, und ein
  `window.confirm` blockiert die Chrome-Extension — derselbe dokumentierte Ausfall wie bei B-127 und
  B-144 am 2026-08-10. Was ihn hier teilweise ersetzt: der neue Integrationstest fährt die Zahl aus zwei
  Sitzen (eigenes und fremdes Konto) gegen die echte API. Was er **nicht** ersetzt: dass jemand den
  gesperrten Knopf im Browser gesehen hat.
- **2026-08-11** — offen geblieben: Ob eine Admin-Oberfläche je entsteht (Entscheidung 2 wäre dann
  nachzuschärfen), ist bewusst nicht entschieden. `Adult.IsAdmin` bleibt ein Datenbank-Flag.
