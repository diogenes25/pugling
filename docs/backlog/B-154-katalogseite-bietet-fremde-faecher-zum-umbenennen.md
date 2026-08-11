---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [Fach-Eigentum im Vater-Web sichtbar machen]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/backlog/B-13-fach-kapitel-eigentum.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-154 · Die Katalogseite bietet „Umbenennen" und „Löschen" an jedem Fach an — auch an fremden

Seit [B-13](B-13-fach-kapitel-eigentum.md) darf nur der Eigentümer ein Fach umbenennen oder löschen, und ein
Seed-Fach („Englisch", „Mathe", „Erdkunde", „Französisch") gehört niemandem, ist also für **jeden** gesperrt.
`CatalogAdmin.tsx` zeigt die `NameRow` mit „OK" und „Löschen" trotzdem an jedem gewählten Fach, und der
Löschdialog verspricht ausdrücklich, was danach passiert — der Server antwortet `403 not_owner`. Die Antwort
trägt seit B-13 `isMine`, die Oberfläche liest es nicht. Das ist dieselbe Klasse wie
[B-150](B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md) (Verlagssperre unsichtbar, Dialog
versprach das Gegenteil), nur eine Katalogebene höher; B-13 hat sie in Entscheidung 5 bewusst offen gelassen
(`wo: backend`) und die Erfassung als eigene Story vorgesehen, falls sie beim Testen auffällt. Sie fiel beim
Rollengang gegen die laufende App auf.

## Verlauf

- **2026-08-11** — angelegt beim Bau von B-13 (dessen Entscheidung 5 hat den Fall benannt, aber bewusst nicht
  gebaut; das Ziel von B-13 ist ohne diese Story erfüllt).
