# Answer key — Module 02

## Exercise 02.1  (multiple-choice, 3 pts)

**Correct answer: B) When the two branches changed the same lines of the same file in different
ways.**
Award 3 pts only for B. Anything else: 0 pts. (Merging different branches or having commits on both
is normal and usually conflict-free; conflicts require *overlapping* edits.)

## Exercise 02.2  (code, 4 pts)

Reference command sequence (starting from a repo with one commit on `main` containing `app.txt`):

```
git switch -c feature
echo "second line" >> app.txt
git commit -am "Extend app.txt on feature"
git switch main
git merge feature
```

Verify:

- `git log --oneline` on `main` now includes `Extend app.txt on feature`.
- The merge printed `Fast-forward` (and `git log --oneline` shows a straight line with no separate
  "Merge branch..." commit).
- `git branch` lists both `feature` and `main`.

Scoring (must have actually run it):

- 2 pts — the feature commit is present on `main` after the merge.
- 1 pt — the merge was a fast-forward (confirmed by the `Fast-forward` output or the absence of a
  merge commit).
- 1 pt — both branches exist (`git branch` shows `main` and `feature`).

## Exercise 02.3  (explain, 3 pts)

Rubric — award per criterion, max 3:

- 1 pt — states a fast-forward just moves the branch pointer forward to the other branch's commit.
- 1 pt — explains it happens because the base branch had no new commits since branching (history is
  linear / hasn't diverged).
- 1 pt — concludes that because nothing needs to be reconciled, no merge commit is created.
