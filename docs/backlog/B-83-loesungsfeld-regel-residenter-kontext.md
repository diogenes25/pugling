---
tags: [typ/story, status/idee, bereich/doku, bereich/backend]
aliases: [Lösungsfeld-Regel, Tor folgt dem Geheimnis, Rollenreichweite eines Lese-DTOs]
status: idee
prio: P3
art: Aufräumen
quelle: B-82 (E3′, Bau-Sitzung 2026-08-03)
unverifiziert: true
---

# B-83 · Die Lösungsfeld-Regel steht nur als Kommentar am Wächter

Seit [B-82](B-82-positions-report-gibt-loesungen-preis.md) gilt mechanisch: **gibt eine Action in ihrem
Nutzlast-Graphen ein Feld namens `Answer`/`Solution`/`CorrectAnswer` heraus, muss sie auf eine Rollenmenge
ohne `Student` gegated sein** — `Roles.Creator` genügt dafür ebenso wie `Roles.Supervisor`, denn ein Autor
muss die Lösung seiner eigenen Übung sehen. Der Wächter
(`ConventionGuardTests.Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated`) hält die Regel und begründet
sie ausführlich im Kommentar. **Nur steht sie nirgends, wo man sie vor dem Schreiben eines DTOs liest**: die
Root-`CLAUDE.md` zählt unter „Mechanische Tore statt Disziplin" die Wächter auf, ohne diesen; die
Konventions-Liste sagt nichts über die Rollenreichweite eines Lese-DTOs; und
[docs/codequalitaet-gates-plan.md](../codequalitaet-gates-plan.md) führt das Inventar der Tore, in dem er
fehlt.

Das ist genau die Reihenfolge, die dieses Repo teuer bezahlt hat: Wer ein neues Auswertungs-DTO schreibt,
lernt die Regel erst, wenn das Tor rot wird — und ein rotes Tor ohne vorher gelesene Regel liest sich wie
eine Schikane, nicht wie eine Zusicherung. Vier Türen in dieselbe Kammer ([B-75](B-75-lese-hoerverstehen-ohne-inhalt.md),
[B-77](B-77-liste-menge-als-folge.md), [B-80](B-80-tags-geben-fremde-konfiguration-preis.md),
B-82) sind aufgegangen, ohne dass jemand einen Fehler gemacht hat.

Mitzunehmen ist dabei die **Begründung des Schnitts**, nicht nur die Regel — sie ist der eigentliche Wert und
wurde in der Bau-Sitzung gemessen, nicht vermutet: Der Ordner (`Contracts.Supervisor`) ist ein *Näherungswert*
für „nicht kind-lesbar" und trägt nicht, weil `PlanResponse` und `ObjectiveResponse` **als Typen** dual gelesen
sind. Das Lösungsfeld ist die Sache. Und `Expected` gehört ausdrücklich **nicht** dazu: das ist der Reveal
*nachdem* das Kind geantwortet hat und damit der Zweck der Rückmeldung.

Zu klären beim Ausformulieren: ob die Regel **resident** wird (Root-`CLAUDE.md` — sie ändert bei einem neuen
Lese-DTO eine Entscheidung, was das dortige Kriterium wäre) oder in die verschachtelte
[backend/Pugling.Api/CLAUDE.md](../../backend/Pugling.Api/CLAUDE.md) gehört, und ob E4s Wegfall im
Gates-Inventar nachzuziehen ist.

## Verlauf

- **2026-08-03** — angelegt aus dem Bau von B-82: dort wurde E3 beim Scharfstellen von seiner eigenen
  Kostenmessung umgeworfen und als E3′ neu geschnitten (Tor folgt dem Geheimnis statt dem Ordner, vier
  Ausnahmen statt zehn, E4 dadurch gegenstandslos). Die Regel ist damit mechanisch gesichert, aber nur am
  Wächter dokumentiert — ausdrücklich als eigene Story abgelegt statt stillschweigend in die `CLAUDE.md`
  geschrieben, weil residenter Kontext eine Entscheidung des Nutzers ist. `prio: P3` in Analogie zu
  [B-51](B-51-admin-rolle-dokumentieren.md) vorgeschlagen (Doku-Aufräumen an einer Regel, die im Code schon
  greift) — nicht vom Nutzer bestätigt.
