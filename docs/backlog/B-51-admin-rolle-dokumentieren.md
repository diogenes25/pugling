---
tags: [typ/story, status/idee, bereich/doku, bereich/auth]
aliases: [Admin-Rolle, Break-Glass, vierter Akteur]
status: idee
prio: P3
art: Aufräumen
quelle: Sitzung 2026-08-01 (Rollen-Durchgang)
unverifiziert: true
---

# B-51 · Die Admin-Rolle kommt in keinem Rollen-Dokument vor

Neben Creator, Supervisor und Student gibt es einen **vierten Akteur**: `Roles.Admin`, der
Plattform-Superuser als Break-Glass. Er wird nicht über die API vergeben, sondern über das Flag
`Adult.IsAdmin` (DB/Seed) und beim Login als Rollen-Claim ausgestellt. Er umgeht die RWX-Prüfung an
Übungen — gedacht etwa, um verwaiste Übungen ohne Owner zu reparieren — und darf alle Anmerkungen lesen
und fremde Kommentare löschen.

**Die drei Dokumente, die Rollen erklären, kennen ihn nicht**: weder [grundprinzip.md](../grundprinzip.md)
noch [rollen-doku.md](../rollen-doku.md) noch [wiki/02 · Authentifizierung](../../wiki/02-authentifizierung.md)
erwähnen ihn. Substanziell beschrieben ist er heute nur als Nebenbemerkung in einem Feature-Plan
([anmerkungen-plan.md](../anmerkungen-plan.md)) — und zwar an der interessantesten Stelle: Dort wurde
`Roles.Admin` als Bedingung **ausdrücklich verworfen**, weil die Rolle „auch die RWX-Rechte umgeht", also
zu breit ist, um als Sichtbarkeitsschalter zu dienen. Dazu der Fallstrick, dass ein frisch gesetztes
`IsAdmin` **erst nach neuer Anmeldung** wirkt, weil Rollen im JWT stecken.

Wer die drei Ebenen liest, weiß von alldem nichts — und wird die Rolle beim nächsten Rechte-Entwurf
entweder übersehen oder falsch einsetzen. Beim Ausformulieren zu klären: Gehört sie in die Rollen-Doku
(sichtbar, mit Warnung vor ihrer Breite) oder ausdrücklich **nicht** (Break-Glass bleibt undokumentiert)?
Soll sie ohne API vergeben bleiben? Und trägt ihre Breite noch, oder gehört sie in engere Rechte
zerlegt — die Anmerkungs-Entscheidung ist ein Indiz, dass sie an mindestens einer Stelle schon zu grob war.

## Verlauf

- **2026-08-01** — angelegt (Quelle: Rollen-Durchgang; die Doku-Lücke ist geprüft, die Frage nach dem
  Zuschnitt der Rolle nicht).
