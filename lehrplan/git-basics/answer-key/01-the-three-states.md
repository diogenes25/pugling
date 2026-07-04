# Answer key — Module 01

## Exercise 01.1  (multiple-choice, 3 pts)
**Correct answer: B) `git add`.**
Award 3 pts only for B. Anything else: 0 pts. (`commit` moves staged → repository; `status` moves
nothing; `push` sends commits to a remote.)

## Exercise 01.2  (code, 4 pts)
Reference command sequence:

```
git init
echo hi > hello.txt
git add hello.txt
git commit -m "First commit"
```

Verify by running:
- `git log --oneline` → exactly one line, message `First commit`.
- `git status` → "nothing to commit, working tree clean".
- `git show --stat HEAD` → lists `hello.txt` as added, 1 file changed.

Scoring (must have actually run it, not imagined output):
- 2 pts — the single commit exists with the exact message `First commit`.
- 1 pt — working tree is clean (`git status` confirms).
- 1 pt — `hello.txt` is the committed file with content `hi`.
Partial credit is fine; award each sub-point independently.

## Exercise 01.3  (explain, 3 pts)
Rubric — award per criterion, max 3:
- 1 pt — states the staging area lets you choose *which* changes go into the next commit (not
  necessarily all of them).
- 1 pt — connects this to making each commit a single coherent/logical change.
- 1 pt — notes a concrete benefit: readable history, easier review, or the ability to stage part of
  your edits.
