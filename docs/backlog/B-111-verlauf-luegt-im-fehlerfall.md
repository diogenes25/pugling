---
tags: [typ/story, status/in-arbeit, bereich/shop, rolle/student]
aliases: [Noch nichts gekauft im Fehlerfall]
status: in-arbeit
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-05 der Commits 4469662…b20600f (Befund 5)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-99]
wartet_auf: frontend-reviewer
---

# B-111 · Scheitert das Laden des Verlaufs, sagt die App „Noch nichts gekauft"

Der Verlauf-Tab des Sohn-Shops kennt nur zwei Zustände: „Zeilen da" und „nichts gekauft". Der dritte —
„das Laden ist gescheitert" — landet in derselben Anzeige wie der zweite. Das Kind bekommt also die
Auskunft, es habe nie etwas gekauft, während sein Geld längst weg ist.

## User Story

Als Sohn möchte ich, dass mir die App sagt, wenn sie meinen Verlauf nicht laden konnte, damit ich nicht
denke, meine Käufe seien verschwunden.

## Ist-Stand am Code

- `frontend/src/sohn/SohnShop.tsx:298-299` — `HistoryTab` zeigt bei
  `!loading && purchases.length === 0` den Text „Noch nichts gekauft. Hol dir im Tab **Kaufen** etwas
  Schönes! 🎁". Nur diese eine Bedingung entscheidet.
- `frontend/src/sohn/SohnShop.tsx:70-72` — scheitert `loadMoreHistory`, wird der Fehler als `flash(...)`
  gezeigt: ein Toast, der nach **2 Sekunden** verschwindet (`SohnShop.tsx:84-87`). `history` bleibt leer,
  `historyLoaded` bleibt `false`.
- Danach steht dauerhaft die Falschaussage im Bild — der Toast ist weg, die Liste behauptet „nichts
  gekauft", und es gibt **keinen** Weg, das Laden erneut auszulösen: der „Mehr laden"-Knopf erscheint erst
  bei `purchases.length < total`, und `total` ist nach dem Fehlschlag `0`
  (`frontend/src/sohn/SohnShop.tsx:311`).
- Der Tab-Wechsel hilft nur zufällig: `openHistoryTab` lädt neu, weil `historyLoaded` `false` blieb — das
  Kind muss aber raten, dass ein zweimaliger Tab-Wechsel etwas ändert.
- **Der bestehende Test schreibt die Verwechslung fest**: `frontend/src/sohn/HistoryTab.test.tsx:19-22`
  („zeigt einen Hinweis statt einer leeren Liste, solange noch nichts geladen ist") erwartet genau diesen
  Text für `purchases=[]`, `loading=false` — also für den Zustand, der auch „Fehler" bedeutet.

## Die echte Lücke

`purchases.length === 0 && !loading` trägt **drei** Bedeutungen: noch nicht geladen, nichts gekauft,
Laden gescheitert. Die Karte kennt aber nur eine Anzeige dafür, und sie wählt die
selbstbewussteste — eine Aussage über die Vergangenheit des Kindes, die sie nicht belegen kann.

Das ist dieselbe Fehlerklasse wie B-84 („die API-Beispiele behaupten Unerreichbarkeit, wo nur nichts
mitgeschnitten wurde"): kein Wissen wird als negatives Wissen ausgegeben.

## Offene Punkte

1. ~~Fehler in der Karte oder im Rahmen anzeigen?~~ → entschieden, siehe 1.
2. ~~Den Toast behalten?~~ → entschieden, siehe 2.

## Entscheidungen

1. **Die Karte bekommt den Fehler als eigenen Zustand** (`error`-Prop) und zeigt ihn mit einem
   Wiederholen-Knopf, statt dass der Rahmen ihn behandelt. Begründung: `HistoryTab` ist die einzige
   Stelle, die weiß, ob sie Zeilen hat — nur dort ist entscheidbar, ob „leer" eine Aussage oder eine
   Unbekannte ist. **Kosten:** eine Prop und ein Zweig mehr in einer Karte, die vorher nur Daten
   darstellte; und der bestehende Test aus `HistoryTab.test.tsx:19-22` muss seine Erwartung schärfen
   (er prüft künftig „leer **ohne** Fehler").
2. **Der Toast bleibt zusätzlich** — er ist die sofortige Rückmeldung auf den Klick, und der
   Karten-Zustand ist der dauerhafte Befund. Zwei Wege für dieselbe Information sind hier kein Lärm,
   weil sie verschiedene Fragen beantworten („ist mein Klick angekommen?" und „was ist der Stand?").
   **Kosten:** bei einem Fehler erscheint kurz beides.
3. **„Noch nichts gekauft" bleibt für den echten Leerfall** — die Formulierung ist richtig und
   einladend, sie war nur an die falsche Bedingung geknüpft.

## Akzeptanzkriterien

1. Scheitert das Laden der ersten Seite, zeigt der Verlauf-Tab eine Fehlermeldung **und** einen
   Wiederholen-Knopf — nicht „Noch nichts gekauft".
2. Der Wiederholen-Knopf löst denselben Ladeweg aus wie das erste Öffnen des Tabs.
3. Hat das Kind wirklich nichts gekauft (Server antwortet mit leerer Liste), steht weiter „Noch nichts
   gekauft".
4. Während des Ladens steht keine der beiden Aussagen im Bild.

## Schätzung

**Größe: XS** — eine Prop, ein Zweig, ein Knopf; der bestehende Test wird geschärft.

**Risiken:** keine über die Testanpassung hinaus. Kein Server-Anteil, kein Vertrag.

**Angriffsplan** (kein Backend-Anteil): `HistoryTab` um `error`/`onRetry` erweitern → `SohnShop` hält den
Verlauf-Fehler als eigenen State (nicht im geteilten `msg`) → `HistoryTab.test.tsx` schärfen und um den
Fehlerfall ergänzen.

**Testweg:** `frontend/src/sohn/HistoryTab.test.tsx` — die Karte ist ein Baustein mit reinen Props, also
genau der Fall, den ein Komponententest tragen soll (kein gefälschtes `fetch` nötig): ein Fall für
„Fehler statt Leermeldung", einer für „Wiederholen meldet nach oben", und der bestehende Leerfall-Test
mit geschärfter Bedingung.

## Verlauf

- **2026-08-05** — angelegt aus dem Code-Review der autonomen Bau-Runde (Befund 5), direkt mit belegtem
  Ist-Stand.
- **2026-08-05** — im Autonomen Modus ausformuliert, gegrillt und geschätzt (`art: Defekt`, damit autonom
  grillbar). Der Fund der Recherche, der über den Review-Befund hinausgeht: der bestehende Test
  `HistoryTab.test.tsx:19-22` **schreibt die Verwechslung fest** — der Fix macht ihn also
  notwendigerweise rot, und das ist kein Regressionsverdacht, sondern der Zweck.
- **2026-08-05** — in **Sprint 1** gebaut, Protokoll `docs/pm-sitzung-2026-08-05.md`. Rote Probe: die zwei
  neuen Karten-Fälle scheiterten gegen die alte `HistoryTab`, und die Fehlerausgabe enthielt wörtlich
  „Noch nichts gekauft" — die Lüge also im Protokoll. Umgesetzt: `historyError` als eigener State neben
  dem Toast, drei Zustände in der Karte, Wiederholen-Knopf; bei einem Fehler auf einer *späteren* Seite
  bleiben die geladenen Zeilen stehen und „Mehr laden" ist selbst der Wiederholen-Weg. Der bestehende
  Leerfall-Test ist wie vorgesehen geschärft („leer **ohne** Fehler"). Verifikation: **152/152** Frontend,
  Build sauber. Ehrlich vermerkt: der dritte neue Fall („sagt während des Ladens nichts") war schon vorher
  grün — ein Charakterisierungstest, kein Regressionstest. Commit: `dfebc44`.
- **2026-08-05** — bleibt bewusst auf `in-arbeit`: `frontend-reviewer` ist nicht gelaufen (Anweisung
  dieser Sitzung, keine Agenten unaufgefordert zu starten). Der **Rollengang** deckt diese Story nur
  teilweise: der Fehlerfall ist auf Kartenebene bewiesen, nicht im Browser — ihn dort zu erzeugen hieße,
  den Server künstlich kaputt zu machen. Argumentierte Ausnahme, keine stille Lücke.
- **2026-08-05** — **Selbst-Check statt Reviewer-Lauf** (`frontend-reviewer` dreimal an `529 Overloaded`
  abgebrochen, Freigabe lag vor). Schwächerer Beleg, ersetzt den Reviewer nicht — Stufe bleibt
  `in-arbeit`. Er hat aber **zwei echte Mängel in der eigenen Arbeit** gefunden, beide sofort behoben:
  - **Ein Rennen zwischen Kauf und laufendem Nachladen.** Eine Seitenanfrage, die vor dem Verwerfen
    losgeschickt wurde, hängte ihr Ergebnis danach trotzdem ein: sie trägt den Offset des alten Stands,
    also landete Seite 2 im frisch geleerten Zustand — die Liste begann bei Zeile 21, `historyLoaded`
    stand auf `true`, und der eben getätigte Kauf fehlte. Also genau die Lüge, gegen die B-110 angetreten
    ist, nur über einen anderen Weg. Behoben mit einem Generationszähler (`historyGeneration`), der eine
    veraltete Antwort verwirft.
  - **Der dauerhafte Fehler-Banner war stumm.** Der 2-Sekunden-Toast trägt `role="status"`, der Zustand,
    der *bleibt*, trug nichts — ein Screenreader bekam die flüchtige Meldung und nicht den Befund. Beide
    Banner tragen jetzt `role="alert"`.
  - **Eine Korrektur an der eigenen Begründung** (Entscheidung 2 in B-110): „kein `X-Total-Count`" ist nur
    die halbe Wahrheit — der Client kennt die alte Gesamtzahl und könnte den neuen Kauf vorne anhängen und
    hochzählen. Der tragende Grund ist ein anderer: eine Gesamtzahl clientseitig fortzuschreiben führt
    Server-Zustand doppelt und driftet, sobald sich sonst etwas geändert hat (etwa ein Storno). **Preis
    dieser Entscheidung, ehrlich benannt:** wer viele Seiten nachgeladen hatte, verliert sie beim Kauf und
    muss erneut blättern.
  - Verifikation nach den Korrekturen: **152/152** Frontend, Build sauber, `e2e/shop-verlauf.spec.ts`
    grün.
- **2026-08-05** — `entgangen_bei: [B-99]` gesetzt: `HistoryTab` samt der Zwei-Zustands-Anzeige entstand in
  B-99, der Defekt saß beim Fund also in abgenommener Arbeit. Zählt in die Wirkungs-Zahl.
