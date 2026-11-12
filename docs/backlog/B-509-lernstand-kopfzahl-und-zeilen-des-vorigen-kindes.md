---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Lernstand stale Kopfzahl, Pager des vorigen Kindes]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 46 in der Retro von Sprint 47)
unverifiziert: false
entgangen_bei: [B-504]
grund: ""
ersetzt_durch: []
---

# B-509 · Lernstand: Kopfzahl, Zeilen und Pager zeigen nach einem Kinderwechsel das vorige Kind

Nachschau von Sprint 46 (frischer `frontend-reviewer` an `3618e63`, gelesen, Vitest 13/13 der beiden Dateien). Ein 🟡,
drei 🟢. B-504 hat den Leerfall an `!words.stale` gebunden; die übrigen Aussagen aus `words.data` nicht – dieselbe Klasse
wie B-499/B-502/B-504, an anderer Stelle derselben Komponente.

1. 🟡 `frontend/src/vater/VaterLernstand.tsx` (`WeakWords`): Überschrift `Wörter (total)`, die Zeilen und
   `<Pager total=…>` lesen `words.data` ohne `stale`. Kind 1 hat 30 Wörter, Wechsel per URL zu Kind 2, dessen Liste
   scheitert: unter „Lernstand · Kind 2" stehen „Wörter (30)", 25 Zeilen von Kind 1 und „1–25 von 30" unter dem
   Fehlerkasten; „Weiter ›" ist bedienbar (`busy={words.loading}` ist falsch) und fragt Kind 2 mit `skip=25` ab – der
   Zustand, den B-504 AK 1 verhindern sollte. Ohne Fehler kurz im Ladefenster. Kein Test fährt „Wechsel, dann Fehler".
2. 🟢 `:98-101`: der Fehlerkasten der Zählung erscheint auch ohne „nur schwache", wo der Leerfall die Zählung nicht
   braucht – zwei Sätze untereinander, die sich zu widersprechen scheinen.
3. 🟢 `:63`: `openWord` überlebt den Kinderwechsel (anders als `skip`); dieselbe `vocabularyId` beim neuen Kind ist
   ungefragt aufgeklappt, im Ladefenster rendert `WordDetail` mit `childId={neu}` und dem alten Wort.
4. 🟢 `frontend/src/vater/VaterShop.test.tsx:31`, `:65`: die Klicks auf „Freigeben" tragen nur, weil `window.confirm`
   in happy-dom `true` liefert; ausdrücklich setzen (`vi.stubGlobal`), sonst fällt der Test bei einem Update mit
   irreführender Meldung.

Bezug: [B-505](B-505-leerfall-an-der-seite-statt-am-bestand.md) (Leerfall an der Seite in acht anderen Listen).

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf 2026-11-12, Nutzerrolle übertragen):

1. **Ein anderes Kind ist eine andere Liste: `WeakWords` bekommt `key={childId}`, der Filter „nur schwache" zieht in
   `VaterLernstand` hoch.** So überlebt der Filter den Kinderwechsel (der Grund, warum B-502/B-504 auf den Schlüssel
   verzichtet haben), und alles andere – Zeilen, Kopfzahl, Pager, `skip`, `openWord` – beginnt beim neuen Kind leer
   (Regel aus `frontend/CLAUDE.md`: Zeilen eines anderen Elternteils werden nicht gerendert, nicht nur gesperrt). Kosten:
   beim Kinderwechsel steht kurz „Lade…" statt der alten Tabelle – richtig, sie gehört einem anderen Kind.
2. **Innerhalb eines Kindes bleibt „sperren, nicht ausblenden" (B-169):** nach einem Filter- oder Seitenwechsel ist der
   Pager gesperrt, solange die Zeilen `stale` sind (`busy={words.loading || words.stale}`) – sonst blättert „Weiter ›"
   nach einem gescheiterten Seitenwechsel von einem Fenster aus, das nicht stimmt; die Kopfzahl zeigt nur eine Zahl, die
   zum eingestellten Filter gehört.
3. Punkt 2: der Fehlerkasten der Zählung erscheint nur unter „nur schwache" – nur dort braucht der Leerfall sie.
4. Punkt 4: `VaterShop.test.tsx` setzt `window.confirm` ausdrücklich (`vi.stubGlobal`) und räumt es ab.

## Akzeptanzkriterien

1. Kind 1 mit Wörtern, Wechsel auf Kind 2, dessen Liste scheitert: keine Zeile, keine Kopfzahl und kein Pager von Kind 1.
2. „nur schwache" ausgeschaltet, Kinderwechsel: der Filter bleibt aus.
3. Ein aufgeklapptes Wort ist nach dem Kinderwechsel zu.
4. Seitenwechsel, der scheitert: „Weiter ›"/„‹ Zurück" gesperrt; die Kopfzahl nach Filterwechsel zeigt keine Zahl des
   vorigen Filters.
5. Ohne „nur schwache" erscheint kein Fehlerkasten der Zählung.

## Schätzung

**Größe: S** — eine Komponente, Tests in `VaterLernstand.test.tsx` (React Testing Library, `api` gemockt).

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 46, Retro Sprint 47). Punkt 1 am Code gelesen: Überschrift, Zeilen und Pager
  ohne `stale`-Prüfung.
- **2026-11-12** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 49).
- **2026-11-13** — gebaut (Sprint 49). `WeakWords` hält den Filter, `WordList key={childId}` (Entscheidung 1 als Hülle
  statt Hochziehen – gleiches Verhalten, Reiterwechsel setzt den Filter wie vorher zurück); Kopfzahl `shownTotal`, Pager
  `busy={loading || stale}`, Zählungs-Kasten nur unter „nur schwache"; `confirm` im Shop-Test gestubbt.
  - AK 1 → `zeigt nach dem Wechsel auf ein Kind, dessen Liste scheitert…`; F1 (ohne Schlüssel) → rot.
  - AK 2 → Bestandstest `beginnt nach einem Kinderwechsel auf Seite 1 und behält den Filter` (grün).
  - AK 3 → `klappt ein offenes Wort beim Kinderwechsel zu`; F1 → rot.
  - AK 4 → `sperrt den Pager…` (F3 → rot), `nennt nach einem gescheiterten Filterwechsel…` (F2 → rot).
  - AK 5 → `zeigt ohne „nur schwache" keinen Kasten der Zählung`; F4 → rot.
  - Review (`frontend-reviewer`): zwei 🟡 außerhalb des Diffs, dieselbe Klasse auf derselben Seite → im Sprint behoben
    (ohne Zählung, außerhalb des Diffs): Kopfzeile nennt den Namen nur ohne `stale` und zeigt einen gescheiterten Abruf
    (H1, H2 → rot), Katalog mit `key={childId}` (H3 → rot). 🟢 Kopfzahl verschwindet auch beim Blättern kurz – gewollt
    nach Entscheidung 2 („nur eine Zahl, die zum eingestellten Filter gehört"), nicht geändert.
  - Verifikation: Vitest 810/810 (78 Dateien), `tsc -b` sauber; Mutanten-Probe nach der letzten Änderung 17/17 rot je am
    genannten Test (Tabelle im Protokoll `docs/pm-sitzung-2026-11-12-tageslauf.md`, Sprint 49). Playwright 140/140 auf
    Ausweichports (5201/5174) – vor den Review-Nachzügen H1–H4, die tragen Komponententests.
  - Rollengang Vater: E2E-Suite gegen echten Server und echtes UI; Creator, Sohn: Regressionszeugen (Suite grün, kein
    Pfad ihrer Ebenen außer `SohnKonto` unverändert). Abgenommen.
