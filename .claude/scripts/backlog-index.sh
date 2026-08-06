#!/usr/bin/env bash
# Erzeugt die Index-Tabelle in docs/backlog/README.md aus dem Frontmatter der Story-Dateien.
#
# Warum ein Skript und keine handgepflegte Tabelle: Ein Index, der von Hand nachgezogen wird, driftet
# genau dann, wenn es darauf ankommt – beim Weiterschieben einer Story mitten in einer Sitzung. So wird
# aus dem Drift ein sichtbarer `git diff` statt einer stillen Lüge.
#
# Idempotent: zweimal laufen lassen ändert nichts. Der Wächter dafür ist der leere Diff im zweiten Lauf.
#
# Performance: die Feld-/Abschnitts-Extraktion läuft in EINEM awk-Prozess über ALLE Story-Dateien
# (backlog-index.awk) statt vorher ~15 sed/grep-Pipelines PRO Datei. Auf Git-Bash unter Windows ist
# Prozess-Start teuer (Fork-Emulation); bei über 100 Storys war das der Unterschied zwischen ~15 Sekunden
# und mehreren Minuten (gemessen 2026-08-06, ~8.800 Subshells vorher).
#
# Ausgabe-Regeln, die nicht verhandelbar sind (docs/ wird von markdownlint-cli2 geprüft, .claude/ nicht):
#   - MD060 `leading_and_trailing`: jede Tabellenzeile beginnt und endet mit einem gepaddeten Pipe.
#   - MD055/MD056: gleiche Spaltenzahl in allen Zeilen einer Tabelle.
#   - MD033 ist aus, `<details>` ist also erlaubt und gelebte Praxis in dieser Doku.
set -uo pipefail

root="${CLAUDE_PROJECT_DIR:-$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel 2>/dev/null)}"
[ -n "$root" ] && [ -d "$root/docs/backlog" ] || { echo "docs/backlog fehlt – nichts zu tun." >&2; exit 0; }

awk_script="$(dirname "${BASH_SOURCE[0]}")/backlog-index.awk"
[ -f "$awk_script" ] || { echo "$awk_script fehlt." >&2; exit 1; }

cd "$root" || exit 1

readme="docs/backlog/README.md"
[ -f "$readme" ] || { echo "$readme fehlt." >&2; exit 1; }

# Rang der Stufe: reifere Stufe zuerst, damit bei gleicher Prio das Weiterfortgeschrittene oben steht.
# Setzt $_rank statt es auszugeben: ein Aufruf über $(...) würde für jede offene Story eine Subshell
# forken, und genau das war – neben id_of()/belege() – der verbliebene Zeitfresser nach dem awk-Umbau
# (Kommando-Substitution forkt auch dann, wenn die aufgerufene Funktion selbst keinen externen Befehl
# startet; unter Git-Bash/Windows ist jeder Fork teuer).
rank() {
  case "$1" in
    in-arbeit)     _rank=1 ;;
    geschaetzt)    _rank=2 ;;
    gegrillt)      _rank=3 ;;
    ausformuliert) _rank=4 ;;
    idee)          _rank=5 ;;
    *)             _rank=9 ;;
  esac
}

head_row="| Id | Story | Art | Stufe | Prio | Größe | Wo | Kostet |"
sep_row="| --- | --- | --- | --- | --- | --- | --- | --- |"

# Rang der Art bei gleicher Prio. Begründung der Ordnung: ein Defekt wirkt **jetzt**; ein Prüfauftrag ist
# billig und kann Arbeit *streichen*; ein Wunsch ist das Produkt; Aufräumen ändert für niemanden etwas.
# Setzt $_artrank statt es auszugeben – aus demselben Grund wie rank() oben.
artrank() {
  case "$1" in
    Defekt)    _artrank=1 ;;
    Frage)     _artrank=2 ;;
    Wunsch)    _artrank=3 ;;
    Aufräumen) _artrank=4 ;;
    *)         _artrank=9 ;;
  esac
}

offen=""
fertig=""
verworfen=""
unbelegt=""
# Die eine Zahl, die etwas über die WIRKUNG des Verfahrens sagt und nicht über seine Einhaltung: Defekte,
# die in bereits abgenommener Arbeit gefunden wurden (Feld `entgangen_bei`). Alles andere im Index zählt
# Regelkonformität; das hier zählt, was die Abnahme durchgelassen hat.
entgangen=""
# Der NENNER zur Zahl oben, und der Grund, warum sie überhaupt etwas bedeutet: `nachgeschaut` hält je
# abgenommener Story fest, ob nach der Abnahme noch einmal jemand hingesehen hat. Ohne dieses Feld ist
# eine leere Entgleitungs-Liste nicht von „nie geprüft" zu unterscheiden — und genau diese Verwechslung
# macht Qualitätszahlen wertlos. Ein Blick, der NICHTS findet, wird darum genauso eingetragen.
nie_geschaut=""
geschaut_ids=""
ziel_ids=""
# `in-arbeit` trug bisher zwei Bedeutungen: „wird gebaut" und „fertig, haengt an einem Schritt ausserhalb
# des Repos". Die zweite ist unsichtbar geparkte Arbeit — vier Stories lagen so da, ohne dass eine Liste
# es gesagt haette. `wartet_auf` trennt das, stufenunabhaengig: es sammelt alles, was ohne Zutun von
# aussen nicht weitergeht (Reviewer, Betreiber-Handgriff, Klang am echten Geraet).
wartet=""
n_wartet=0
n_offen=0
n_fertig=0
n_verworfen=0
n_entgangen=0
n_geschaut=0

# Prüft die Eintrittsbedingung, soweit sie mechanisch prüfbar ist: Abschnitte und Frontmatter-Felder.
# Nimmt die von backlog-index.awk bereits extrahierten Werte/Flags entgegen, statt sie ein zweites Mal
# aus der Datei zu holen — das war vorher der teuerste Teil dieser Funktion. Setzt $_luecke statt es
# auszugeben: über $(...) aufgerufen würde das für JEDE der über hundert Storys forken (siehe rank()).
belege() {
  local st="$1" groesse="$2" wo="$3" mig="$4" vb="$5" quelle="$6" art="$7" unverifiziert="$8" grund="$9"
  local has_us="${10}" has_is="${11}" has_ak="${12}" has_en="${13}" has_sc="${14}" has_ve="${15}"
  local fehlt=""
  case "$st" in
    ausformuliert|gegrillt|geschaetzt|in-arbeit|abgenommen)
      [ "$has_us" = 1 ] || fehlt="$fehlt, Abschnitt „User Story\""
      [ "$has_is" = 1 ] || fehlt="$fehlt, Abschnitt „Ist-Stand…\""
      [ "$has_ak" = 1 ] || fehlt="$fehlt, Abschnitt „Akzeptanzkriterien\""
      ;;
  esac
  case "$st" in
    gegrillt|geschaetzt|in-arbeit|abgenommen)
      [ "$has_en" = 1 ] || fehlt="$fehlt, Abschnitt „Entscheidungen\""
      ;;
  esac
  case "$st" in
    geschaetzt|in-arbeit|abgenommen)
      [ "$has_sc" = 1 ] || fehlt="$fehlt, Abschnitt „Schätzung\""
      [ -n "$groesse" ] || fehlt="$fehlt, Feld \`groesse\`"
      [ -n "$wo" ]      || fehlt="$fehlt, Feld \`wo\`"
      [ -n "$mig" ]     || fehlt="$fehlt, Feld \`migration\`"
      [ -n "$vb" ]      || fehlt="$fehlt, Feld \`vertragsbruch\`"
      ;;
  esac
  case "$st" in
    idee) [ "$unverifiziert" = "true" ] || fehlt="$fehlt, \`unverifiziert: true\`" ;;
  esac
  case "$st" in
    verworfen) [ -n "$grund" ] || fehlt="$fehlt, Feld \`grund\`" ;;
  esac
  [ "$has_ve" = 1 ] || fehlt="$fehlt, Abschnitt „Verlauf\""
  [ -n "$quelle" ] || fehlt="$fehlt, Feld \`quelle\`"

  # `art` ist ab der ersten Stufe Pflicht und geschlossen: ein Tippfehler wäre sonst eine fünfte Art, die
  # niemandem auffällt, und an der Art hängt die Reihenfolge (Defekt vor Wunsch) und die Form der Abnahme.
  case "$art" in
    Defekt|Wunsch|Frage|Aufräumen) ;;
    "")  fehlt="$fehlt, Feld \`art\`" ;;
    *)   fehlt="$fehlt, \`art\` ist kein bekannter Wert" ;;
  esac

  _luecke="${fehlt#, }"
}

# Ein Backslash zum Pipe-Escapen ohne `sed`: unter Git-Bash/Windows kostet das EXEC eines externen
# Binaries – nicht das reine Fork einer Subshell – den Großteil der Laufzeit (mutmaßlich Antiviren-
# Realtime-Scan je neu gestartetem Prozess-Image). `${var//pattern/repl}` bleibt in derselben Bash.
bs='\'

shopt -s nullglob
files=(docs/backlog/B-*.md)
shopt -u nullglob

while IFS=$'\x01' read -r fpath status prio groesse wo mig vb quelle art entgangen_bei wartet_auf \
  nachgeschaut grund ersetzt unverifiziert titel has_us has_is has_ak has_en has_sc has_ve; do
  [ -n "$fpath" ] || continue
  base="${fpath##*/}"
  # Id = die ERSTEN ZWEI Bindestrich-Felder (${base%%-*} lieferte nur "B", weil der Slug denselben
  # Trenner benutzt, B-01-bildwahl-einfrieren.md) — inline statt als Funktion, um keine Subshell zu forken.
  rest="${base#*-}"
  id="B-${rest%%-*}"
  [ -n "$status" ] || status="?"
  [ -n "$prio" ] || prio="—"
  [ -n "$groesse" ] || groesse="—"
  [ -n "$wo" ] || wo="—"
  [ -n "$art" ] || art="—"
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

  row="| [$id]($base) | $titel | $art | \`$status\` | $prio | $groesse | $wo | $kostet |"

  belege "$status" "$groesse" "$wo" "$mig" "$vb" "$quelle" "$art" "$unverifiziert" "$grund" \
    "$has_us" "$has_is" "$has_ak" "$has_en" "$has_sc" "$has_ve"
  [ -n "$_luecke" ] && unbelegt="${unbelegt}| [$id]($base) | \`$status\` | ${_luecke//|/${bs}|} |"$'\n'

  # Entgleitung: dieser Defekt steckte in Arbeit, die schon `abgenommen` war. Nur bei `art: Defekt`
  # gezählt – ein Wunsch, der später auffällt, ist kein Qualitätsverlust, sondern ein Wunsch.
  if [ -n "$entgangen_bei" ] && [ "$entgangen_bei" != "[]" ] && [ "$art" = "Defekt" ]; then
    entgangen="${entgangen}| [$id]($base) | $titel | ${entgangen_bei//|/${bs}|} | \`$status\` |"$'\n'
    n_entgangen=$((n_entgangen + 1))
    # Ziel-Ids für die Trefferquote merken: aus "[B-99, B-66]" wird " B-99 B-66 ".
    ziel="${entgangen_bei//[][]/}"
    ziel_ids="$ziel_ids ${ziel//,/ }"
  fi

  if [ -n "$wartet_auf" ] && [ "$wartet_auf" != '""' ] && [ "$status" != "abgenommen" ] && [ "$status" != "verworfen" ]; then
    wartet="${wartet}| [$id]($base) | $titel | \`$status\` | ${wartet_auf//|/${bs}|} |"$'\n'
    n_wartet=$((n_wartet + 1))
  fi

  case "$status" in
    abgenommen)
      fertig="${fertig}${row}"$'\n'
      n_fertig=$((n_fertig + 1))
      if [ -n "$nachgeschaut" ] && [ "$nachgeschaut" != '""' ]; then
        n_geschaut=$((n_geschaut + 1))
        geschaut_ids="$geschaut_ids $id"
      else
        nie_geschaut="${nie_geschaut}| [$id]($base) | $titel |"$'\n'
      fi
      ;;
    verworfen)
      [ -n "$grund" ] || grund="—"
      if [ -n "$ersetzt" ] && [ "$ersetzt" != "[]" ]; then
        grund="$grund → $ersetzt"
      fi
      verworfen="${verworfen}| [$id]($base) | $titel | ${grund//|/${bs}|} |"$'\n'
      n_verworfen=$((n_verworfen + 1))
      ;;
    *)
      artrank "$art"
      rank "$status"
      offen="${offen}$prio$_artrank$_rank|$row"$'\n'
      n_offen=$((n_offen + 1))
      ;;
  esac
done < <([ "${#files[@]}" -gt 0 ] && awk -f "$awk_script" "${files[@]}")

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

    # Die Quote läuft über die NACHGESCHAUTEN, nie über alle abgenommenen: eine Entgleitung ist nur dort
    # sichtbar, wo hinterher jemand hingesehen hat. „4 von 42" läse sich wie eine Fehlerrate und wäre eine
    # Lüge über die nie geprüften.
    n_ziele=0
    for z in $(printf '%s' "$ziel_ids" | tr ' ' '\n' | sort -u); do
      [ -n "$z" ] || continue
      case " $geschaut_ids " in *" $z "*) n_ziele=$((n_ziele + 1)) ;; esac
    done
    n_nie=$((n_fertig - n_geschaut))

    if [ -n "$wartet" ]; then
      printf '\n%s\n\n' "### Wartet auf Zutun von außen ($n_wartet)"
      printf '%s\n' "Diese Stories kommen **im Repo nicht weiter** — es fehlt ein Schritt, den nur ein Mensch"
      printf '%s\n\n' "oder ein Werkzeug außerhalb tun kann. Nicht „in Arbeit\" im Sinne von „wird gerade gebaut\"."
      printf '%s\n%s\n' "| Id | Story | Stufe | Wartet auf |" "| --- | --- | --- | --- |"
      printf '%s' "$wartet"
    fi

    printf '\n%s\n\n' "### Nach der Abnahme entgangen ($n_entgangen)"
    printf '%s\n\n' "**Nachgeschaut: $n_geschaut von $n_fertig abgenommenen** — und in $n_ziele davon steckte ein Defekt, der bei der Abnahme durchgekommen war. Der Nenner ist die Zahl der *geprüften*, nicht der abgenommenen Stories; die übrigen $n_nie sind **unbeobachtet**, nicht sauber."
    if [ "$n_entgangen" -eq 0 ]; then
      printf '%s\n' "*Keine Entgleitung erfasst.*"
    else
      printf '%s\n%s\n' "| Defekt | Titel | Entgangen bei | Stufe |" "| --- | --- | --- | --- |"
      printf '%s' "$entgangen"
    fi

    if [ -n "$nie_geschaut" ]; then
      printf '\n<details>\n<summary>Nie nachgeschaut (%s) — Arbeitsvorrat der Nachschau</summary>\n\n' "$n_nie"
      printf '%s\n' "Abgenommen, aber nach der Abnahme nie wieder angesehen. Wer hier einen Blick tut, setzt"
      printf '%s\n\n' "danach \`nachgeschaut: <Datum>\` — **auch wenn er nichts gefunden hat**, sonst zählt der Blick nicht."
      printf '%s\n%s\n' "| Id | Story |" "| --- | --- |"
      printf '%s' "$nie_geschaut"
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
