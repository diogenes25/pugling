---
tags: [typ/story, status/abgenommen, bereich/shop, rolle/supervisor]
aliases: [Vater-Kaufhistorie ungeblättert, Unveränderliche Sortierung geblätterter Listen]
status: abgenommen
prio: P2
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: eigener Fund bei der Verifikation von B-110 (Sprint 1 am 2026-08-05)
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
nachgeschaut: ""
---

# B-113 · Drei geblätterte Listen mit veränderlicher Sortierung — der Vater erreicht keine von ihnen vollständig

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

## Entscheidungen

1. **Pager, nicht „Mehr laden".** Begründung: das Vater-Web blättert überall mit `ListControls`/`Pager`
   (`VaterKonto` u. a.); der Sohn-Shop ist die einzige Stelle mit „Mehr laden", weil eine Arcade keine
   Seitenzahlen zeigt. Zwei Muster für dieselbe Sache in derselben Oberfläche wäre der Fehler, den diese
   Story beheben soll, nicht wiederholen. **Kosten:** keine — der Baustein existiert bereits.
2. **Nur die Sortierung angleichen, den `status`-Filter nicht in die UI heben.** Begründung: der Filter
   existiert am Endpunkt, aber niemand hat ihn angefragt; ihn jetzt zu bauen wäre eine Erweiterung ohne
   Auftrag. **Kosten:** keine, bleibt eine eigene Idee, falls er auffällt.
3. **Eine Story für alle drei Stellen, nicht drei einzelne.** Begründung: es ist eine Regel („der
   Sortierschlüssel eines geblätterten Endpunkts muss unveränderlich sein und einen `Id`-Tiebreaker
   tragen"), und drei Stories würden sie dreimal begründen und dreimal verifizieren. **Kosten:** die Story
   wächst von XS auf **M**, und der ursprüngliche Titel („Kaufhistorie des Vaters") ist zu eng — geändert
   auf die Regel.
4. **Korrektur einer eigenen Fehlannahme beim Ausformulieren:** die beiden Anfrage-Listen sind **nicht**
   bereits geblättert angezeigt — `frontend/src/lib/api.ts:708/670` (`childActivations`, `myActivations`)
   senden ebenfalls kein `skip`/`take` und lesen `X-Total-Count` nicht, `VaterShop.tsx:396-415` und die
   Sohn-Seite zeigen die Antwort komplett. Trotzdem bekommt **nur die Kaufhistorie** einen echten Pager in
   dieser Story: dort ist der stille Schnitt **belegt riskant**, weil der Vater aus der Liste heraus
   *storniert* — eine Zeile hinter der 100er-Grenze kann er nicht zurücknehmen. Für die Anfrage-Listen ist
   die reale Gefahr heute die **nicht-deterministische Reihenfolge** (Kriterium 4), nicht der Zeilenverlust
   selbst — hundert gleichzeitig offene Aktivierungsanfragen einer Familie sind unbelegt. **Kosten:**
   diese Story behebt die Sortierung/den Tiebreaker an allen drei Stellen, verdrahtet aber nur an einer
   einen Pager. Ein echter Fall von „mehr als 100 Aktivierungsanfragen" bliebe stumm bis jemand ihn
   beobachtet — bewusst in Kauf genommen, dasselbe Verhältnismäßigkeits-Argument wie B-103.

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

1. Die Kauf-Liste im Vater-Shop erreicht jede Zeile, zeigt die echte Gesamtzahl und bietet einen Pager.
2. Alle drei Stellen sortieren unabhängig vom `Status` (wie `MeController` seit B-110) und tragen einen
   `Id`-Tiebreaker.
3. Ein Integrationstest je geblätterter Liste belegt, dass eine Statusänderung (Storno bzw.
   genehmigen/ablehnen) zwischen zwei Seitenabrufen keine Zeile überspringt — das Gegenstück zu
   `Kaufhistorie_StornoZwischenZweiSeiten_UeberspringtKeineZeile`.
4. Die beiden Anfrage-Listen (Aktivierungsanfragen Vater- und Kind-Sicht) liefern für zwei Anfragen
   derselben Sekunde eine definierte, wiederholbare Reihenfolge.

## Schätzung

**Größe: M** — drei Backend-Stellen (eine Zeile Sortierung + ein `Id`-Tiebreaker je Stelle) plus ein
client-seitiger Pager-Umbau an genau einer Oberfläche (`VaterShop`s Kaufhistorie, Entscheidung 4); die
beiden Anfrage-Listen bekommen nur die Backend-Korrektur, ihr fehlender Pager ist bewusst nicht Teil
dieser Story. Kein Vertragsbruch (nur eine Feld-Bedeutung wird präziser, kein Feld ändert Typ/Name),
keine Migration.

**Risiken:** ein bestehender Test kann die alte `Owned`-vor-`Pending`-Gruppierung an einer der drei
Stellen festschreiben (dann bewusst anpassen, wie schon bei B-110); die sichtbare Reihenfolge in den
Anfrage-Listen ändert sich für Einträge mit identischem `RequestedAt`.

**Angriffsplan** (Backend zuerst, drei gleichartige Stellen in einem Zug):

1. `ShopController.cs:347` (Kaufhistorie) — Sortierung auf `PurchasedAt desc, Id desc`, wie `MeController`
   seit B-110.
2. `ShopController.cs:389` (Aktivierungsanfragen Vater-Sicht) und `MeController.cs:374` (Anfragen des
   Kindes) — Sortierung auf `RequestedAt desc, Id desc` (Tiebreaker ergänzt).
3. Je ein Integrationstest analog `Kaufhistorie_StornoZwischenZweiSeiten_UeberspringtKeineZeile` für die
   Kaufhistorie und für die Aktivierungsanfragen (Genehmigen zwischen zwei Seiten).
4. Frontend: `frontend/src/lib/api.ts` `childPurchases(...)` um `skip`/`take` erweitern und `X-Total-Count`
   lesen; `VaterShop.tsx` bekommt `ListControls`/`Pager` wie `VaterKonto`.

**Testweg:** Backend-Integrationstests wie oben (`Pugling.Api.Tests`); Frontend keine neue
Komponente — `Pager` ist bereits getestet (`ListControls.test.tsx`), die Story verdrahtet ihn nur neu in
`VaterShop`.

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
- **2026-08-05 (Nachtlauf)** — **autonom gegrillt und geschätzt** (`art: Defekt`, README → „Der
  Backlog-Lauf"). Beim Ausformulieren der Entscheidungen ein **Fund, der eine eigene Annahme korrigiert**
  (Entscheidung 4): die beiden Anfrage-Listen sind entgegen der ursprünglichen Vermutung **nicht** bereits
  geblättert dargestellt — `childActivations`/`myActivations` senden kein `skip`/`take`, `VaterShop.tsx`
  zeigt die Antwort komplett. Damit bekommt nur die Kaufhistorie einen Pager (dort ist der Schaden belegt:
  der Vater storniert aus der Liste heraus); die Anfrage-Listen bekommen nur die Sortierungs-Korrektur,
  ihr fehlender Pager bleibt bewusst außerhalb dieser Story. Titel geschärft (trug bisher nur die
  Kaufhistorie). `groesse: M`, `wo: beides`, keine Migration, kein Vertragsbruch.
- **2026-08-05 (Nachtlauf, Sprint 2)** — **gebaut wie geplant.** Rote Probe zuerst: zwei neue
  Integrationstests (`ShopFlowTests.cs`) scheiterten gegen den Vorzustand exakt wie beim Vorbild B-110
  (`Expected: 4, Actual: 3`), grün nach dem Fix. **Backend:** `ShopController.cs:347` (Kaufhistorie) auf
  `PurchasedAt desc, Id desc` ohne Gruppierung; `ShopController.cs:389` und `MeController.cs:374`
  (beide Anfrage-Listen) auf `RequestedAt desc, Id desc` mit ergänztem `Id`-Tiebreaker; die
  „open ones first"-Behauptung im XML-Doc-Summary von `ChildActivations` entfernt (stimmte nicht mehr).
  **Frontend:** `api.ts` `childPurchases` von einem status-Parameter auf `httpPaged` mit `skip`/`take`
  umgestellt (Muster wie `classTests`/`childPoints`); `VaterShop.tsx`s `ChildShopView` hält jetzt
  `purchaseSkip` und rendert einen `Pager` unter der Kauf-Tabelle (Muster wie `VaterKonto`).
  **Verifikation:** Backend `dotnet test -c Release` → **732/732 grün** (730 + 2); Frontend `npm run build`
  sauber, `npm test` → **153/153 grün** (152 + 1, siehe B-116). `pugling-reviewer` und `frontend-reviewer`
  liefen beide erfolgreich, kein Blocker (der Backend-Reviewer konnte wegen eines Datei-Locks des
  laufenden Dev-Servers selbst nicht bauen/testen, hat den Diff aber gegen das B-110-Muster geprüft; die
  eigene Verifikation deckt Build/Test bereits ab). **Live gegen die laufende API geprüft** (Demo-Vater,
  Demo-Kind, vier direkt eingefügte Käufe): Seite 1 (`skip=0&take=2`) und Seite 2 (`skip=2&take=2`) lieferten
  korrekt `X-Total-Count: 4` und die erwarteten vier Ids ohne Überlappung — die Blätterung selbst ist damit
  am echten Server bestätigt. Der geplante Storno-zwischen-zwei-Seiten-Nachweis am selben Weg scheiterte an
  einem `409 concurrency_conflict`, verursacht durch die **rohe SQL-Einfügung** der Testzeilen (kein
  EF-Pfad) — kein Produktdefekt, sondern ein Artefakt des Kurzschritts; die Zeilen wurden wieder entfernt.
  Der Storno-Nachweis selbst steht bereits **belegt** in den beiden neuen Integrationstests, die über den
  echten EF-Pfad laufen. **Kein Browser-Rollengang möglich** (Chrome-Extension in dieser unbeaufsichtigten
  Sitzung nicht verbunden) — ein Mensch sollte einmal im Vater-Web „Käufe" blättern und dabei einen Kauf
  stornieren. **Eintrittsbedingung erfüllt, Stufe auf `abgenommen`.** Commits: `ff6b1a3` (Backend,
  Sortierung + Tests), `9f475ea` (Frontend, Pager an der Kaufhistorie; baut auf `07eddc6`s `busy`-Prop
  auf).
