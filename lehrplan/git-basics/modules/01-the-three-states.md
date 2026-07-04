# Module 01 — The three states: working tree, staging area, repository

## Objective
After this module you should be able to:
- Name Git's three states and say which command moves a file between them.
- Take a brand-new file from untracked to committed using the command line.
- Explain why the staging area exists instead of committing everything at once.

## Learn
Git tracks your files across **three places**:

1. **Working tree** — the actual files on disk you edit. A new file here is *untracked*; Git sees it
   but isn't managing it yet.
2. **Staging area** (also called the *index*) — a holding area for the exact changes you want in your
   next commit. You put changes here with `git add`.
3. **Repository** — the permanent history of commits, stored in `.git/`. You move the staged snapshot
   into history with `git commit`.

The normal flow for a new file `notes.txt`:

```
echo "hello" > notes.txt      # working tree: notes.txt is now UNTRACKED
git add notes.txt             # staging area: change is STAGED
git commit -m "Add notes"     # repository: change is COMMITTED to history
```

`git status` tells you where everything sits: untracked files, staged changes ("Changes to be
committed"), and unstaged changes ("Changes not staged for commit"). Running it constantly is a good
habit.

**Why a staging area at all?** It lets you craft a commit deliberately — you can stage *some* of your
edits and leave others out, so each commit is one coherent change rather than a dump of everything you
touched. That's what makes history readable and reviewable.

## Exercises

### Exercise 01.1  (multiple-choice, 3 pts)
Which command moves a change from the **working tree** into the **staging area**?

- A) `git commit`
- B) `git add`
- C) `git status`
- D) `git push`

Write your chosen letter into the ledger before checking.

### Exercise 01.2  (code, 4 pts)
In a throwaway directory, create a brand-new Git repository, add a single file `hello.txt`
containing the text `hi`, and commit it with the message `First commit`. Your work is "done" when
**all** of these hold, which you can verify yourself:

- `git log --oneline` shows exactly **one** commit whose message is `First commit`.
- `git status` reports a clean working tree (nothing to commit).
- `git show --stat HEAD` shows that `hello.txt` was the file added.

Record the exact sequence of commands you ran in the ledger.

### Exercise 01.3  (explain, 3 pts)
In your own words, explain **why** Git has a separate staging area instead of committing every changed
file automatically. Your answer will be graded against the rubric in the answer key.
