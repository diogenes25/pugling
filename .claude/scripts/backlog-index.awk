# Companion to backlog-index.sh: extracts every frontmatter field and section-presence flag that the
# index needs, in ONE process over ALL story files (instead of ~15 sed/grep pipelines per file). On
# Git-Bash/Windows process start is expensive (fork emulation) — with 100+ stories that is the whole
# difference between seconds and minutes.
#
# Deliberately mirrors the old per-field sed pipeline's exact quirks instead of "fixing" them, so the
# generated index stays byte-identical: a multi-line quoted value only yields its first physical line
# (the closing quote never appears on that line, so the quote-strip below simply no-ops); a bare "#"
# inside a value truncates it, same as the old comment-stripping sed.
#
# Output: one record per input file, printed to stdout in argument order, fields separated by \001
# (SOH — see emit() for why not a tab).
function reset() {
  status = prio = groesse = wo = mig = vb = quelle = art = ""
  entgangen = wartet = nachgeschaut = grund = ersetzt = unverifiziert = title = ""
  has_us = has_is = has_ak = has_en = has_sc = has_ve = 0
  in_fm = 0
  fm_done = 0
}

function emit() {
  # \001 (SOH) statt Tab: Tab gilt Bash intern als "IFS-Whitespace" und wird von `read` bei mehreren
  # aufeinanderfolgenden Vorkommen zu EINEM Trenner zusammengefasst – leere Felder (mehrere Storys ohne
  # `groesse`/`wo`/`migration`/`vertragsbruch`, z. B. jede auf `idee`) fielen darüber ersatzlos weg und
  # verschoben jedes folgende Feld. \001 kommt in keinem realistischen Frontmatter-Wert vor und wird von
  # Bash NICHT als Whitespace behandelt.
  printf "%s\001%s\001%s\001%s\001%s\001%s\001%s\001%s\001%s\001%s\001%s\001%s\001%s\001%s\001%s\001%s\001%d\001%d\001%d\001%d\001%d\001%d\n", \
    fname, status, prio, groesse, wo, mig, vb, quelle, art, entgangen, wartet, nachgeschaut, grund, \
    ersetzt, unverifiziert, title, has_us, has_is, has_ak, has_en, has_sc, has_ve
}

BEGIN { reset() }

# A new input file starts: flush the previous record (none yet on the very first file) and reset.
FNR == 1 {
  if (NR > 1) emit()
  reset()
  fname = FILENAME
}

FNR == 1 && $0 == "---" { in_fm = 1; next }
in_fm && !fm_done && $0 == "---" { fm_done = 1; next }

# Frontmatter body: "key: value" lines. Only the FIRST line for a given key is ever seen (a later line
# with the same prefix would mean a duplicate YAML key, which does not occur in this repo).
in_fm && !fm_done {
  idx = index($0, ":")
  if (idx > 0) {
    key = substr($0, 1, idx - 1)
    val = substr($0, idx + 1)
    sub(/^[ \t]+/, "", val)
    hp = index(val, "#")
    if (hp > 0) { val = substr(val, 1, hp - 1); sub(/[ \t]+$/, "", val) }
    if (val ~ /^".*"$/ || val ~ /^'.*'$/) val = substr(val, 2, length(val) - 2)
    sub(/[ \t]+$/, "", val)
    if      (key == "status")        status = val
    else if (key == "prio")          prio = val
    else if (key == "groesse")       groesse = val
    else if (key == "wo")            wo = val
    else if (key == "migration")     mig = val
    else if (key == "vertragsbruch") vb = val
    else if (key == "quelle")        quelle = val
    else if (key == "art")           art = val
    else if (key == "entgangen_bei") entgangen = val
    else if (key == "wartet_auf")    wartet = val
    else if (key == "nachgeschaut")  nachgeschaut = val
    else if (key == "grund")         grund = val
    else if (key == "ersetzt_durch") ersetzt = val
    else if (key == "unverifiziert") unverifiziert = val
  }
  next
}

# Title = first "# " line in the file (frontmatter or body, matching the old grep -m1 over the whole
# file) with the "B-nn · " lead-in stripped and pipes escaped for the Markdown table cell.
title == "" && /^# / {
  t = $0
  sub(/^# /, "", t)
  sub(/^B-[0-9]*[ \t]*·[ \t]*/, "", t)
  gsub(/\|/, "\\|", t)
  title = t
}

/^(##|###) / {
  if ($0 ~ /User Story/)         has_us = 1
  if ($0 ~ /Ist-Stand/)          has_is = 1
  if ($0 ~ /Akzeptanzkriterien/) has_ak = 1
  if ($0 ~ /Entscheidungen/)     has_en = 1
  if ($0 ~ /Sch(ä|ae)tzung/)     has_sc = 1
  if ($0 ~ /Verlauf/)            has_ve = 1
}

END { emit() }
