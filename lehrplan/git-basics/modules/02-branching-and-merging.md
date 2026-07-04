# Module 02 — Branching and merging

## Objective
After this module you should be able to:
- Create a branch, switch to it, and commit on it without touching `main`.
- Merge a branch back into `main` and know what a fast-forward merge is.
- Explain what a merge conflict is and when Git makes you resolve one by hand.

## Learn
A **branch** is just a movable pointer to a commit. `main` is a branch like any other. Working on a
branch lets you develop a change in isolation and merge it back when it's ready.

Core commands:

```
git switch -c feature      # create branch "feature" and switch to it
git switch main            # switch back to main
git merge feature          # merge "feature" into the branch you're currently on
```

(`git checkout -b feature` / `git checkout main` are the older spellings of the same thing.)

**Fast-forward merge:** if `main` hasn't moved since you branched off it, merging `feature` just slides
`main`'s pointer forward to `feature`'s commit — no new merge commit is needed. This happens when the
history is a straight line.

**Merge conflict:** if the *same lines* of a file were changed differently on both branches, Git can't
decide which version wins. It pauses the merge, marks the clashing region in the file with
`<<<<<<<`, `=======`, `>>>>>>>` markers, and waits for you to edit the file to the version you want,
then `git add` it and `git commit` to finish. Conflicts happen on overlapping edits — not merely
because two branches both changed *some* files.

## Exercises

### Exercise 02.1  (multiple-choice, 3 pts)
When does Git produce a **merge conflict** that you must resolve by hand?

- A) Any time you merge two different branches.
- B) When the two branches changed the same lines of the same file in different ways.
- C) Whenever both branches have at least one commit.
- D) Only when you forget to commit before merging.

Write your chosen letter into the ledger before checking.

### Exercise 02.2  (code, 4 pts)
In a throwaway repo that already has one commit on `main` with a file `app.txt`:
create a branch `feature`, add a line to `app.txt` on that branch and commit it, then switch back to
`main` and merge `feature` in. Your work is "done" when:

- `git log --oneline` on `main` shows the commit you made on `feature`.
- The merge was a **fast-forward** (no separate merge commit was created, because `main` didn't move).
- `git branch` shows both `main` and `feature`.

Record the exact command sequence and how you confirmed the fast-forward in the ledger.

### Exercise 02.3  (explain, 3 pts)
In your own words, explain what a **fast-forward merge** is and why it produces no merge commit. Your
answer will be graded against the rubric in the answer key.
