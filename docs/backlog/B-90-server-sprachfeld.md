---
tags: [typ/story, status/idee, bereich/backend]
aliases: [Server-Sprachfeld, Locale, Adult/Child Sprachfeld]
status: idee
prio: P4
art: Wunsch
quelle: B-87 (geteilt)
unverifiziert: true
ersetzt_durch: []
---

# B-90 · Server-Sprachfeld an `Adult`/`Child`

Dritter Teil aus dem geteilten [B-87](B-87-vater-web-franzoesisch-server-sprachfeld.md): ein additives
Vertragsfeld `Locale` an `Adult`/`Child` (Migration + Vertragsbruch), damit der Server pro Konto weiß, in
welcher Sprache er antworten soll. Technisch unabhängig von B-88/B-89 (reine Backend-Änderung), aber
**bedarfsgetrieben statt vorgezogen**: schon B-38 (Entscheidung 3) und B-87 (Ist-Stand) haben festgestellt,
dass heute außer dem in [B-86](B-86-uebungstyp-manifest-anzeigenamen-schluessel.md) separat behandelten
Übungstyp-Anzeigenamen kein Endpunkt sprachabhängigen Text ausliefert — ein vorgezogenes Ausformulieren
ohne echten Konsumenten würde eine Migration rechtfertigen, die noch niemand braucht. Bleibt am
sinnvollsten auf `idee` stehen, bis ein konkreter Bedarf (z. B. lokalisierte Ledger-Texte, siehe
[B-30](B-30-i18n-rest.md)) sie zur Ausformulierung drängt.

## Verlauf

- **2026-08-04** — angelegt beim Teilen von [B-87](B-87-vater-web-franzoesisch-server-sprachfeld.md)
  (Entscheidung 2 dort), bewusst auf `idee` belassen: bedarfsgetrieben, kein Vorgriff ohne Konsument.
