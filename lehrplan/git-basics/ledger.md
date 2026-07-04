# Ledger — git-basics

Append-only record of attempts and self-grades. The father audits this.

## [2026-07-04 01:12] Module 01 — Exercise 01.1 (multiple-choice, 3 pts) — attempt 1
**My answer (committed before checking):** B — `git add`.

## [2026-07-04 01:12] Module 01 — Exercise 01.2 (code, 4 pts) — attempt 1
**My answer (committed before checking):** ran in a throwaway dir:
```
git init
echo hi > hello.txt
git add hello.txt
git commit -m "First commit"
```
Verified: `git log --oneline` = one commit "First commit"; `git status` clean; `git show --stat HEAD` lists hello.txt added; content is `hi`.

## [2026-07-04 01:12] Module 01 — Exercise 01.3 (explain, 3 pts) — attempt 1
**My answer (committed before checking):** The staging area lets you pick exactly which changes go
into the next commit instead of committing every edit automatically. That way each commit is one
coherent, logical change rather than a dump of everything you touched — which keeps history readable
and easy to review, and lets you stage only part of your edits (e.g. commit a bugfix while leaving an
unrelated experiment unstaged).

**After checking the key (01.1):** key says B. Match. **Score: 3 / 3.**
**After checking the key (01.2):** single commit "First commit" (2), clean tree (1), hello.txt=hi (1). All passed. **Score: 4 / 4.**
**After checking the key (01.3):** covered all three rubric criteria — which-changes, coherent commit, readable-history/reviewability benefit. **Score: 3 / 3.**
### Module 01 total: 10 / 10 — PASSED (bar 7/10).

## [2026-07-04 01:12] Module 02 — Exercise 02.1 (multiple-choice, 3 pts) — attempt 1
**My answer (committed before checking):** B — conflict happens when both branches changed the same lines of the same file differently.

## [2026-07-04 01:12] Module 02 — Exercise 02.2 (code, 4 pts) — attempt 1
**My answer (committed before checking):** ran in a throwaway repo (main had one commit with app.txt):
```
git switch -c feature
echo "second line" >> app.txt
git commit -am "Extend app.txt on feature"
git switch main
git merge feature
```
Merge output printed `Fast-forward`. `git log --oneline` on main shows the feature commit atop the initial one (straight line, no merge commit). `git branch` shows both `feature` and `main`.

## [2026-07-04 01:12] Module 02 — Exercise 02.3 (explain, 3 pts) — attempt 1
**My answer (committed before checking):** A fast-forward merge happens when the branch you merge
into (main) hasn't gained any commits since you branched off it, so history is still a straight line.
Git doesn't need to reconcile two diverging lines of work — it just slides main's pointer forward to
the feature branch's latest commit. Because nothing has to be combined, no new merge commit is created.

**After checking the key (02.1):** key says B. Match. **Score: 3 / 3.**
**After checking the key (02.2):** feature commit on main (2), Fast-forward confirmed (1), both branches exist (1). **Score: 4 / 4.**
**After checking the key (02.3):** covered pointer-moves-forward, base-had-no-new-commits/linear, and no-merge-commit. **Score: 3 / 3.**
### Module 02 total: 10 / 10 — PASSED (bar 7/10).

## COURSE RESULT: 20 / 20 (100%) — PASSED (overall bar 16/20). Completed in one attempt per module, honestly graded.
