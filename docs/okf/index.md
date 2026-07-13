---
type: Bundle Index
title: Pugling OKF-Wissensbündel
description: Verzeichnis dauerhafter, konzeptioneller Wissensdokumente (Architektur-Konzepte, Playbooks, How-tos) im Open Knowledge Format.
tags: [okf, index, wissen]
timestamp: 2026-07-13T00:00:00Z
---

# Pugling – OKF-Wissensbündel

Dieses Bündel hält **dauerhaftes, konzeptionelles Wissen** über Pugling, das für Menschen
**und** KI-Agenten über die Zeit nützlich bleiben soll – Architektur-Konzepte, Playbooks,
How-tos. Es ist bewusst getrennt vom API-First-Kern (Code + OpenAPI ist die Quelle der
Wahrheit) und von der tagesaktuellen Doku unter [../](../).

## Konventionen

Jede nicht-reservierte `.md` in diesem Bündel folgt dem **Open Knowledge Format (OKF)**:

- YAML-Frontmatter mit mindestens einem nicht-leeren `type` (beschreibender String, z. B.
  `Architecture Concept`, `Playbook`). Empfohlen: `title`, `description`, `tags`,
  `timestamp` (ISO 8601).
- Body ist reines Markdown; Überschriften wie `# Examples` / `# Citations` sind optional.
- Andere Konzepte über relative Markdown-Pfade verlinken (bleiben im Repo klickbar);
  Quelldateien ebenfalls relativ verlinken.
- Reservierte Dateinamen: **`index.md`** (dieses Verzeichnis-Listing) und **`log.md`**.

Format-Definition (Draft):
<https://github.com/GoogleCloudPlatform/knowledge-catalog/blob/main/okf/SPEC.md>

## Inhalt

_Noch keine Wissensdokumente – dieses Bündel wurde am 2026-07-13 angelegt. Neue Einträge
hier als Liste mit relativem Link und Kurzbeschreibung ergänzen._
