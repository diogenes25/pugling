---
tags: [typ/story, status/idee, bereich/training, rolle/student, lerntechnik/vokabeln, lerntechnik/birkenbihl]
aliases: [Vokabel im Satz, Satzkarte]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-650 · Vokabel im Satz: Karte mit Birkenbihl-Zeile, nur die Zielvokabel ist offen

Heute wird jede Vokabel isoliert abgefragt (laufen ↔ run). Der Vater ist mit der Kombi-Lernkarte
([B-598](B-598-kombi-lernkarte-aufdecken-abschreiben-erinnern.md), [B-643](B-643-lernkarte-leichter-schalten-und-runden-bis-alles-erinnert.md))
zufrieden, und sie soll bleiben. Manche Wörter merkt sich das Kind aber isoliert schlecht. Diese Wörter soll es
zusätzlich in einem kurzen Satz üben, ähnlich wie im Schultest. Die Karte zeigt den Satz in der Lernsprache. Die
Zielvokabel ist darin eine Lücke. Darunter steht die Wort-für-Wort-Übersetzung nach Birkenbihl, in der alle Wörter
außer der Zielvokabel übersetzt sind. Wo es passt, kommt ein Bild dazu. Beispiel für die Zielvokabel *home*:

```text
Tom runs ____ .
Tom rennt heim.
```

Das Kind schreibt *home*. Der Kontext trägt die Bedeutung, und die Birkenbihl-Zeile sorgt dafür, dass der Satz keine
zweite Hürde ist. Gefragt ist also nur das eine Wort.

## User Story

Als **Vater** möchte ich, dass mein Kind schwer zu merkende Vokabeln in einem kurzen Satz mit Birkenbihl-Zeile übt,
in dem nur die Zielvokabel offen ist. **Damit** verankert es das Wort im Zusammenhang, so wie es im Schultest
abgefragt wird, und nicht nur als Wortpaar.

## Fragen fürs Ausformulieren

Ungeprüft, je mit erster Empfehlung. Die Entscheidung trifft erst die Grill-Runde.

1. **Wo im Ablauf?** Eine neue Stufe der Lernkarte, eine eigene Station im Pfad oder ein eigener Übungstyp?
   *Empfehlung:* eine Stufe bzw. Station **nach** dem freien Erinnern. Die Satzkarte vertieft, sie ersetzt die
   Kombi-Lernkarte nicht.
2. **Welche Wörter bekommen sie?** Alle Wörter oder nur die „schweren“?
   *Empfehlung:* zuerst die Wörter, die im Leitner-Verlauf wiederholt falsch waren oder leichter geschaltet wurden.
   So verlangt es der Vater („bei manchen Karten schwer“).
3. **Woher kommen die Sätze?** Aus Beispielsätzen des Lehrwerks, aus Lückentext- und Übersetzungsübungen, die es
   schon gibt, oder vom KI-Creator erzeugt?
   *Empfehlung:* Satz an der Vokabel hinterlegen und vom KI-Creator vorschlagen lassen, mit Durchsicht durch den
   Creator. Der Wortschatz des Satzes sollte aus der Unit kommen. Abgrenzen gegen
   [B-649](B-649-uebersetzungssaetze-im-vokabelspeicher.md): Sätze gehören nicht als Einträge in den Wortspeicher.
4. **Gebeugte Form:** Im Satz steht *runs*, die Vokabel heißt *run*. Was wird erwartet?
   *Empfehlung:* die Form, die der Satz verlangt. Das ist die Schulrealität, braucht aber ein Feld mit der
   erwarteten Lösung je Satz.
5. **Woher kommt die Birkenbihl-Zeile?** Automatische Dekodierung gegen den Vokabelspeicher (siehe
   [B-648](B-648-birkenbihl-jedes-wort-eine-vokabel.md)) oder von Hand?
   *Empfehlung:* automatisch, mit Korrektur durch den Creator. Damit hängt die Story an B-648.
6. **Bild:** [B-03](B-03-lueckensaetze-mit-bild.md) ist verworfen, weil ein Bild auf einer getippten Stufe die
   Lösung verrät (Anti-Cheat). Gilt das hier auch?
   *Empfehlung:* nochmals prüfen. Die Birkenbihl-Zeile nennt die Bedeutung ohnehin, ein Bild verrät also nicht
   mehr als sie. Gefragt ist hier die Schreibweise in der Lernsprache, nicht die Bedeutung. Vorerst ohne Bild
   bauen, wenn das die Story kleiner macht.
7. **Zählt die Satzkarte für die Pflicht und den Leitner-Stand?**
   *Empfehlung:* ja, wie die anderen Stufen. Sonst übt das Kind sie nicht.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Chat mit dem Vater; Beispiel „Tom runs ___ / Tom rennt heim“).
