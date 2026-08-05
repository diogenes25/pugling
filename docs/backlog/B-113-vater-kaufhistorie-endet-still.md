---
tags: [typ/story, status/ausformuliert, bereich/shop, rolle/supervisor]
aliases: [Vater-Kaufhistorie ungeblättert]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: eigener Fund bei der Verifikation von B-110 (Sprint 1 am 2026-08-05)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-113 · Die Kaufhistorie des Vaters endet still bei 100 Zeilen — B-99 eine Ebene höher

B-99 hat den stillen Schnitt in der Kaufhistorie des **Kindes** beseitigt. Dieselbe Liste aus der Sicht
des **Vaters** hat ihn noch: der Server blättert, der Client fragt nie nach einer zweiten Seite, und die
Oberfläche zeigt das Ergebnis als vollständige Liste. Der Vater ist dabei der, der aus dieser Liste
heraus *handelt* — stornieren geht nur an einer Zeile, die er sieht.

## User Story

Als Vater möchte ich jeden Kauf meines Kindes erreichen, auch den hundertersten, damit ich ihn ansehen und
notfalls stornieren kann.

## Ist-Stand am Code

- `backend/Pugling.Api/Controllers/Supervisor/ShopController.cs:337-350` — der Endpunkt ist **geblättert**
  (`skip`/`take`, `ToPagedListAsync` setzt `X-Total-Count`), Vorgabe `PagingExtensions.DefaultTake`
  = **100** (`backend/Pugling.Api/Controllers/PagingExtensions.cs:9`).
- `frontend/src/lib/api.ts:700-704` — `childPurchases(childId, status?)` sendet **kein** `skip`/`take` und
  liest den `X-Total-Count`-Header nicht: der Client bekommt Seite 1 und erfährt nie, dass es mehr gibt.
- `frontend/src/vater/VaterShop.tsx:371` — die Antwort wird als `ShopPurchase[]` geladen und als
  vollständige Liste gezeigt; kein `Pager`, kein „X von Y" (das Muster dafür gibt es im Repo längst:
  `frontend/src/components/ListControls.tsx`, benutzt u. a. in `VaterKonto`).
- Dieselbe Zeile trägt zusätzlich die **veränderliche Sortierung**, die B-110 auf der Kind-Seite entfernt
  hat: `ShopController.cs:347` gruppiert `Status == Owned` nach vorn. Heute unschädlich, weil niemand
  blättert — sie wird zum Fehler in dem Moment, in dem jemand den Pager ergänzt, also genau bei der
  Behebung dieser Story.

## Die echte Lücke

Zwei Dinge, die zusammengehören, aber nicht dasselbe sind:

1. **Der stille Schnitt** (der eigentliche Defekt): ab 101 Käufen fehlen Zeilen ohne jeden Hinweis. Für
   den Vater ist das schlimmer als für das Kind, weil er aus dieser Liste heraus storniert — eine Zeile,
   die er nicht sieht, kann er nicht zurücknehmen. Der Server ist dabei **schon fertig**: er blättert und
   meldet die Gesamtzahl. Es fehlt allein der Client.
2. **Die veränderliche Sortierung** als Falle für die Behebung: wer den Pager einbaut, ohne die
   Sortierung wie in B-110 stabil zu machen, baut den übersprungenen Datensatz mit ein — und zwar in dem
   Pfad, in dem das Stornieren, also genau die Statusänderung, alltäglich ist.

Warum nicht in B-110 mitgemacht: B-110s Ziel und Akzeptanzkriterien betreffen die Sicht des Kindes und
sind ohne diese Ressource vollständig erfüllt; hier sind ein Client-Umbau (Pager, Total, Zustände) und
eine andere Oberfläche betroffen. Dasselbe Muster wie B-97 → B-104 (dieselbe Fehlerklasse eine Ebene
höher wird eine eigene Story, statt die laufende zu dehnen).

## Offene Punkte

1. **Pager oder „Mehr laden"?** Empfehlung: **Pager** (`ListControls`) wie in `VaterKonto` — das
   Vater-Web blättert überall so, und der Sohn-Shop hat „Mehr laden", weil eine Arcade keine Seitenzahlen
   zeigt. Zwei Muster für dieselbe Sache in derselben Oberfläche wären der Fehler.
2. **Nur die Sortierung angleichen oder auch den `status`-Filter der UI anbieten?** Empfehlung: nur die
   Sortierung und der Pager. Der Filter existiert am Endpunkt, aber niemand hat ihn angefragt — er
   gehört in eine eigene Idee, wenn er auffällt.
3. ~~**Gilt derselbe Fund für weitere Listen?**~~ → **gemessen am 2026-08-05, es ist ein Muster.** Die
   Antwort steht unten; offen bleibt allein die *Entscheidung*, ob diese Story alle drei Stellen trägt
   oder geteilt wird. Empfehlung: **eine Story für alle drei** — es ist eine Regel („der Sortierschlüssel
   eines geblätterten Endpunkts muss unveränderlich sein und einen `Id`-Tiebreaker haben"), und drei
   Stories würden sie dreimal begründen. Größe damit **M**, nicht XS. Folge, die mit entschieden wird:
   **der heutige Titel ist dann zu eng** („Kaufhistorie des Vaters") und wandert mit dem Zuschnitt.

## Die Messung: drei Stellen, nicht eine (2026-08-05)

Gesucht wurde nach `? 0 : 1` in `OrderBy` über dem ganzen Backend. Ergebnis:

| Stelle | Sortiert nach | Geblättert? | Wirkt heute? |
| --- | --- | --- | --- |
| `Controllers/Supervisor/ShopController.cs:347` | `Status == Owned ? 0 : 1` | `skip`/`take` am Endpunkt, **Client blättert nicht** | stiller Schnitt bei 100 |
| `Controllers/Supervisor/ShopController.cs:389` | `Status == Pending ? 0 : 1` (Aktivierungsanfragen) | `skip`/`take`, `ToPagedListAsync` | **ja** — genehmigen/ablehnen ändert genau diesen Schlüssel |
| `Controllers/Student/MeController.cs:374` | `Status == Pending ? 0 : 1` (Anfragen des Kindes) | `skip`/`take`, `ToPagedListAsync` | ja, sobald der Client blättert |

Zwei Verschärfungen gegenüber der ursprünglichen Annahme:

- Bei den **Aktivierungsanfragen** ist die Statusänderung nicht der Ausnahmefall, sondern der
  Regelbetrieb: der Vater genehmigt oder lehnt ab, und genau das verschiebt die Zeile aus der ersten
  Gruppe. Von den drei Stellen ist das die gefährlichste.
- **Beiden Anfrage-Listen fehlt der `Id`-Tiebreaker**: sie enden auf `ThenByDescending(r => r.RequestedAt)`
  ohne weiteres Kriterium. Zwei Anfragen in derselben Sekunde haben damit **keine** definierte Reihenfolge,
  auch ohne jede Statusänderung — zwei Abrufe derselben Seite können unterschiedliche Zeilen liefern. Das
  ist ein eigener, unabhängiger Defekt derselben Familie.

## Akzeptanzkriterien

1. Die Kauf-Liste im Vater-Shop erreicht jede Zeile und zeigt die echte Gesamtzahl.
2. Die Sortierung des Endpunkts ist unabhängig vom `Status` (wie `MeController` seit B-110).
3. Ein Integrationstest belegt, dass eine Stornierung zwischen zwei Seitenabrufen keine Zeile
   überspringt — das Gegenstück zu `Kaufhistorie_StornoZwischenZweiSeiten_UeberspringtKeineZeile`.

## Verlauf

- **2026-08-05** — angelegt und direkt ausformuliert: der Fund entstand **bei der Verifikation von
  B-110** (Sprint 1, `docs/pm-sitzung-2026-08-05.md`), also durch die Frage „wo steht dieselbe Zeile
  noch?". Ist-Stand mit `Datei:Zeile` belegt; bewusst **nicht** in B-110 aufgenommen, Begründung oben.
  Nicht geschätzt: der offene Punkt 3 (ist es ein Muster?) verändert die Größe, und das ist eine Messung,
  keine Schätzung.
- **2026-08-05** — offener Punkt 3 **gemessen**, nicht geschätzt (Abschnitt „Die Messung"): es sind drei
  Stellen, zwei davon werden tatsächlich geblättert, und beiden Anfrage-Listen fehlt zusätzlich der
  `Id`-Tiebreaker. Der Fund kam aus dem **Selbst-Check** zu B-110, nachdem `pugling-reviewer` dreimal an
  einem serverseitigen `529` abgebrochen war — also aus derselben Frage, die dem Reviewer aufgetragen war
  („wo steht diese Zeile noch?"). Die Story bleibt `ausformuliert`: die Zahl liegt vor, die Entscheidung
  über den Zuschnitt (eine Story oder drei) gehört in die Grill-Runde.
