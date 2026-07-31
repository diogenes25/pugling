#!/usr/bin/env bash
# Erzeugt die Index-Tabelle in docs/backlog/README.md aus dem Frontmatter der Story-Dateien.
#
# Warum ein Skript und keine handgepflegte Tabelle: Ein Index, der von Hand nachgezogen wird, driftet
# genau dann, wenn es darauf ankommt – beim Weiterschieben einer Story mitten in einer Sitzung. So wird
# aus dem Drift ein sichtbarer `git diff` statt einer stillen Lüge.
#
# Idempotent: zweimal laufen lassen ändert nichts. Der Wächter dafür ist der leere Diff im zweiten Lauf.
#
# Ausgabe-Regeln, die nicht verhandelbar sind (docs/ wird von markdownlint-cli2 geprüft, .claude/ nicht):
#   - MD060 `leading_and_trailing`: jede Tabellenzeile beginnt und endet mit einem gepaddeten Pipe.
#   - MD055/MD056: gleiche Spaltenzahl in allen Zeilen einer Tabelle.
#   - MD033 ist aus, `<details>` ist also erlaubt und gelebte Praxis in dieser Doku.
set -uo pipefail

root="${CLAUDE_PROJECT_DIR:-$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel 2>/dev/null)}"
[ -n "$root" ] && [ -d "$root/docs/backlog" ] || { echo "docs/backlog fehlt – nichts zu tun." >&2; exit 0; }
cd "$root" || exit 1

readme="docs/backlog/README.md"
[ -f "$readme" ] || { echo "$readme fehlt." >&2; exit 1; }

# Frontmatter-Wert lesen: nur der YAML-Kopf, Anführungszeichen und Kommentare weg.
# Der Bereich wird auf den Kopf begrenzt, damit ein "status:" im Prosa-Text nicht gewinnt.
fm() {
  sed -n '2,/^---$/p' "$1" \
    | grep -m1 "^$2:" \
    | sed -e "s/^$2:[[:space:]]*//" -e 's/[[:space:]]*#.*$//' -e 's/^"\(.*\)"$/\1/' -e "s/^'\(.*\)'$/\1/" \
    | sed -e 's/[[:space:]]*$//'
}

# Rang der Stufe: reifere Stufe zuerst, damit bei gleicher Prio das Weiterfortgeschrittene oben steht.
rank() {
  case "$1" in
    in-arbeit)     echo 1 ;;
    geschaetzt)    echo 2 ;;
    gegrillt)      echo 3 ;;
    ausformuliert) echo 4 ;;
    idee)          echo 5 ;;
    *)             echo 9 ;;
  esac
}

head_row="| Id | Story | Art | Stufe | Prio | Größe | Wo | Kostet |"
sep_row="| --- | --- | --- | --- | --- | --- | --- | --- |"

# Rang der Art bei gleicher Prio. Begründung der Ordnung: ein Defekt wirkt **jetzt**; ein Prüfauftrag ist
# billig und kann Arbeit *streichen*; ein Wunsch ist das Produkt; Aufräumen ändert für niemanden etwas.
artrank() {
  case "$1" in
    Defekt)    echo 1 ;;
    Frage)     echo 2 ;;
    Wunsch)    echo 3 ;;
    Aufräumen) echo 4 ;;
    *)         echo 9 ;;
  esac
}

offen=""
fertig=""
verworfen=""
unbelegt=""
n_offen=0
n_fertig=0
n_verworfen=0

# Trägt die Datei den Abschnitt, den ihre Stufe verlangt? Tolerant gematcht: die dünnen Stories fassen
# "Ist-Stand am Code · Entscheidungen" in EINER Überschrift zusammen und verlinken ein Protokoll.
hat() { grep -qE "^#{2,3} .*$2" "$1"; }

# Prüft die Eintrittsbedingung, soweit sie mechanisch prüfbar ist: Abschnitte und Frontmatter-Felder.
# Was NICHT prüfbar ist (ob ein Ist-Stand wirklich belegt, ob eine Entscheidung Kosten nennt), bleibt
# Sache des Skills — aber die dumme Hälfte muss keine Disziplin kosten.
belege() {
  local f="$1" st="$2" fehlt=""
  case "$st" in
    ausformuliert|gegrillt|geschaetzt|in-arbeit|abgenommen)
      hat "$f" "User Story"        || fehlt="$fehlt, Abschnitt „User Story\""
      hat "$f" "Ist-Stand"         || fehlt="$fehlt, Abschnitt „Ist-Stand…\""
      hat "$f" "Akzeptanzkriterien" || fehlt="$fehlt, Abschnitt „Akzeptanzkriterien\""
      ;;
  esac
  case "$st" in
    gegrillt|geschaetzt|in-arbeit|abgenommen)
      hat "$f" "Entscheidungen"    || fehlt="$fehlt, Abschnitt „Entscheidungen\""
      ;;
  esac
  case "$st" in
    geschaetzt|in-arbeit|abgenommen)
      hat "$f" "Sch(ä|ae)tzung"    || fehlt="$fehlt, Abschnitt „Schätzung\""
      for feld in groesse wo migration vertragsbruch; do
        [ -n "$(fm "$f" "$feld")" ] || fehlt="$fehlt, Feld \`$feld\`"
      done
      ;;
  esac
  case "$st" in
    idee) [ "$(fm "$f" unverifiziert)" = "true" ] || fehlt="$fehlt, \`unverifiziert: true\`" ;;
  esac
  case "$st" in
    verworfen) [ -n "$(fm "$f" grund)" ] || fehlt="$fehlt, Feld \`grund\`" ;;
  esac
  hat "$f" "Verlauf" || fehlt="$fehlt, Abschnitt „Verlauf\""
  [ -n "$(fm "$f" quelle)" ] || fehlt="$fehlt, Feld \`quelle\`"

  # `art` ist ab der ersten Stufe Pflicht und geschlossen: ein Tippfehler wäre sonst eine fünfte Art, die
  # niemandem auffällt, und an der Art hängt die Reihenfolge (Defekt vor Wunsch) und die Form der Abnahme.
  case "$(fm "$f" art)" in
    Defekt|Wunsch|Frage|Aufräumen) ;;
    "")  fehlt="$fehlt, Feld \`art\`" ;;
    *)   fehlt="$fehlt, \`art\` ist kein bekannter Wert" ;;
  esac

  printf '%s' "${fehlt#, }"
}

shopt -s nullglob
for f in docs/backlog/B-*.md; do
  base="${f##*/}"
  # Die Id sind die ERSTEN ZWEI Bindestrich-Felder: `${base%%-*}` lieferte nur "B", weil der Slug
  # denselben Trenner benutzt (B-01-bildwahl-einfrieren.md).
  id="$(printf '%s' "$base" | cut -d- -f1,2)"
  status="$(fm "$f" status)";      [ -n "$status" ] || status="?"
  prio="$(fm "$f" prio)";          [ -n "$prio" ] || prio="—"
  groesse="$(fm "$f" groesse)";    [ -n "$groesse" ] || groesse="—"
  wo="$(fm "$f" wo)";              [ -n "$wo" ] || wo="—"
  mig="$(fm "$f" migration)"
  vb="$(fm "$f" vertragsbruch)"

  # Titel = erste H1, ohne den "B-nn · "-Vorspann (die Id steht schon in der eigenen Spalte).
  titel="$(grep -m1 '^# ' "$f" | sed -e 's/^# //' -e 's/^B-[0-9]*[[:space:]]*·[[:space:]]*//' -e 's/|/\\|/g')"
  [ -n "$titel" ] || titel="(ohne Titel)"

  # "offen" ist ein eigener Zustand und darf NICHT wie "nein" aussehen: eine noch nicht gefallene
  # Entscheidung über eine Migration ist die teuerste Unbekannte, die eine Story tragen kann.
  kostet=""
  case "$mig" in
    ja)    kostet="Migration" ;;
    offen) kostet="Migration?" ;;
  esac
  case "$vb" in
    ja)    kostet="${kostet:+$kostet + }Vertrag" ;;
    offen) kostet="${kostet:+$kostet + }Vertrag?" ;;
  esac
  [ -n "$kostet" ] || kostet="—"

  art="$(fm "$f" art)"; [ -n "$art" ] || art="—"
  row="| [$id]($base) | $titel | $art | \`$status\` | $prio | $groesse | $wo | $kostet |"

  luecke="$(belege "$f" "$status")"
  [ -n "$luecke" ] && unbelegt="${unbelegt}| [$id]($base) | \`$status\` | $(printf '%s' "$luecke" | sed 's/|/\\|/g') |"$'\n'

  case "$status" in
    abgenommen)
      fertig="${fertig}${row}"$'\n'
      n_fertig=$((n_fertig + 1))
      ;;
    verworfen)
      grund="$(fm "$f" grund)"; [ -n "$grund" ] || grund="—"
      ersetzt="$(fm "$f" ersetzt_durch)"
      [ -n "$ersetzt" ] && [ "$ersetzt" != "[]" ] && grund="$grund → $ersetzt"
      verworfen="${verworfen}| [$id]($base) | $titel | $(printf '%s' "$grund" | sed 's/|/\\|/g') |"$'\n'
      n_verworfen=$((n_verworfen + 1))
      ;;
    *)
      offen="${offen}$prio$(artrank "$art")$(rank "$status")|$row"$'\n'
      n_offen=$((n_offen + 1))
      ;;
  esac
done
shopt -u nullglob

{
  printf '%s\n' "<!-- backlog-index:start -->"
  printf '%s\n' "<!-- Erzeugt von .claude/scripts/backlog-index.sh — nicht von Hand pflegen. -->"
  printf '\n'

  if [ "$n_offen" -eq 0 ] && [ "$n_fertig" -eq 0 ] && [ "$n_verworfen" -eq 0 ]; then
    printf '%s\n' "*Noch keine Story angelegt.*"
  else
    printf '%s\n\n' "### Offen ($n_offen)"
    if [ "$n_offen" -eq 0 ]; then
      printf '%s\n' "*Keine.*"
    else
      printf '%s\n%s\n' "$head_row" "$sep_row"
      printf '%s' "$offen" | LC_ALL=C sort | cut -d'|' -f2-
    fi

    if [ "$n_fertig" -gt 0 ]; then
      printf '\n<details>\n<summary>Abgenommen (%s)</summary>\n\n' "$n_fertig"
      printf '%s\n%s\n' "$head_row" "$sep_row"
      printf '%s' "$fertig"
      printf '\n</details>\n'
    fi

    if [ -n "$unbelegt" ]; then
      printf '\n%s\n\n' "### ⚠ Stufe behauptet, Datei belegt nicht"
      printf '%s\n' "Diese Stories tragen einen \`status\`, dessen Eintrittsbedingung in der Datei nicht"
      printf '%s\n\n' "vollständig steht. Entweder nachtragen oder die Stufe zurücknehmen."
      printf '%s\n%s\n' "| Id | Stufe | Fehlt |" "| --- | --- | --- |"
      printf '%s' "$unbelegt"
    fi

    if [ "$n_verworfen" -gt 0 ]; then
      printf '\n<details>\n<summary>Verworfen (%s)</summary>\n\n' "$n_verworfen"
      printf '%s\n%s\n' "| Id | Story | Grund |" "| --- | --- | --- |"
      printf '%s' "$verworfen"
      printf '\n</details>\n'
    fi
  fi

  printf '\n%s\n' "<!-- backlog-index:end -->"
} > "$readme.index.tmp"

# Block zwischen den Markern ersetzen. awk statt sed, weil der Block mehrzeilig ist und sed dafür
# Zeilenumbrüche escapen müsste – ein Titel mit Sonderzeichen kippte das.
awk -v blockfile="$readme.index.tmp" '
  /^<!-- backlog-index:start -->$/ { while ((getline line < blockfile) > 0) print line; skip = 1; next }
  /^<!-- backlog-index:end -->$/   { skip = 0; next }
  !skip { print }
' "$readme" > "$readme.tmp"

# Der Marker-Block trägt seinen End-Marker selbst; ohne diese Prüfung fräße ein fehlender
# Start-Marker den halben Index weg, ohne dass es auffällt.
if ! grep -q '^<!-- backlog-index:end -->$' "$readme.tmp"; then
  rm -f "$readme.tmp" "$readme.index.tmp"
  echo "Marker <!-- backlog-index:start/end --> fehlen in $readme – Index nicht geschrieben." >&2
  exit 1
fi

mv "$readme.tmp" "$readme"
rm -f "$readme.index.tmp"
echo "Index geschrieben: $n_offen offen, $n_fertig abgenommen, $n_verworfen verworfen."
