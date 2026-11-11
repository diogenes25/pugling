"""B-472: one-off rewrite of stored progress to the new card numbering of "both".

Until B-472 a vocabulary exercise asked "both" ways played one card per word, every odd one swapped. Since B-472 it
plays two cards per word: card 2i forwards, card 2i+1 backwards. Stored card numbers would point at other cards
afterwards, so this script rewrites them once:

    old card i  ->  2i      (i even: was asked forwards)
    old card i  ->  2i + 1  (i odd: was asked the other way round)

Only positions on exercises with "both" are touched. Rewritten are PositionItemProgress.ItemIndex,
TestItemResult.ItemIndex, the frozen orders PracticeSessions.Order and TestAttempts.Order, and every setting that counts
cards (PlanPositions.NewItemsPerDay and .ItemCount, Exercises.DefaultItemCount): doubled, so that they mean the same
number of words as before.

Not idempotent - nothing in the data tells whether it already ran. Hence it
- only runs while the server is stopped (port 5200 free),
- writes a backup next to the database first,
- leaves a marker <db>.b472-done and refuses a second run.

Usage (from the repository root):
    python -I scripts/b472-beide-richtungen.py backend/Pugling.Api/pugling.db --dry-run
    python -I scripts/b472-beide-richtungen.py backend/Pugling.Api/pugling.db
"""

import argparse
import json
import shutil
import socket
import sqlite3
import sys
from datetime import datetime
from pathlib import Path


def new_index(old: int) -> int:
    # An odd card was the word asked the other way round - it becomes that word's backward card.
    return 2 * old if old % 2 == 0 else 2 * old + 1


def port_in_use(port: int) -> bool:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.settimeout(0.5)
        return s.connect_ex(("127.0.0.1", port)) == 0


def remap_order(raw: str | None) -> str | None:
    if raw is None:
        return None
    return json.dumps([new_index(i) for i in json.loads(raw)], separators=(",", ":"))


def rewrite(con: sqlite3.Connection) -> dict[str, int]:
    counts = {"exercises": 0, "positions": 0, "progress": 0, "results": 0, "sessions": 0, "attempts": 0}
    positions = con.execute(
        """SELECT p.Id FROM PlanPositions p JOIN Exercises e ON e.Id = p.ExerciseId
           WHERE e.Type = 'Vocabulary' AND json_extract(e.ConfigJson, '$.direction') = 'both'""").fetchall()
    for (position_id,) in positions:
        counts["positions"] += 1

        # Two passes through negative values: (PlanPositionId, ItemIndex) is unique, and a single pass could
        # collide with a row not yet rewritten.
        rows = con.execute("SELECT Id, ItemIndex FROM PositionItemProgress WHERE PlanPositionId = ?",
                           (position_id,)).fetchall()
        for row_id, old in rows:
            con.execute("UPDATE PositionItemProgress SET ItemIndex = ? WHERE Id = ?", (-1 - new_index(old), row_id))
        con.execute("UPDATE PositionItemProgress SET ItemIndex = -1 - ItemIndex WHERE PlanPositionId = ? AND ItemIndex < 0",
                    (position_id,))
        counts["progress"] += len(rows)

        results = con.execute(
            """SELECT r.Id, r.ItemIndex FROM TestItemResult r JOIN TestAttempts t ON t.Id = r.TestAttemptId
               WHERE t.PlanPositionId = ? AND r.ItemIndex IS NOT NULL""", (position_id,)).fetchall()
        for row_id, old in results:
            con.execute("UPDATE TestItemResult SET ItemIndex = ? WHERE Id = ?", (new_index(old), row_id))
        counts["results"] += len(results)

        for table, key in (("PracticeSessions", "sessions"), ("TestAttempts", "attempts")):
            rows = con.execute(f'SELECT Id, "Order" FROM {table} WHERE PlanPositionId = ?', (position_id,)).fetchall()
            for row_id, order in rows:
                con.execute(f'UPDATE {table} SET "Order" = ? WHERE Id = ?', (remap_order(order), row_id))
            counts[key] += len(rows)

        # Both settings count cards; doubled, they keep meaning the same number of words.
        con.execute("""UPDATE PlanPositions SET NewItemsPerDay = NewItemsPerDay * 2,
                              ItemCount = CASE WHEN ItemCount IS NULL THEN NULL ELSE ItemCount * 2 END
                       WHERE Id = ?""", (position_id,))

    # The exercise's own default cap counts cards too; once per exercise, not per position. A wrong mention in a set-graded
    # test has no card number (ItemIndex NULL) and is left alone above.
    counts["exercises"] = con.execute(
        """UPDATE Exercises SET DefaultItemCount = DefaultItemCount * 2
           WHERE Type = 'Vocabulary' AND json_extract(ConfigJson, '$.direction') = 'both' AND DefaultItemCount IS NOT NULL""").rowcount
    return counts


def main() -> int:
    parser = argparse.ArgumentParser(description="B-472: Lernstand auf die neue Zählung von „beide“ umschreiben.")
    parser.add_argument("database", type=Path)
    parser.add_argument("--dry-run", action="store_true", help="nur zählen, nichts schreiben")
    parser.add_argument("--port", type=int, default=5200, help="Port des Servers, der angehalten sein muss")
    args = parser.parse_args()

    db = args.database.resolve()
    marker = db.with_name(db.name + ".b472-done")
    if not db.exists():
        print(f"Datenbank {db} nicht gefunden.", file=sys.stderr)
        return 2
    if marker.exists():
        print(f"Schon umgeschrieben (Marke {marker}). Ein zweiter Lauf würde den Stand verfälschen.", file=sys.stderr)
        return 2
    if port_in_use(args.port):
        print(f"Port {args.port} ist belegt – erst den Server anhalten.", file=sys.stderr)
        return 2

    if not args.dry_run:
        backup = db.with_name(f"{db.name}.bak-vor-b472-{datetime.now():%Y%m%d-%H%M%S}")
        shutil.copy2(db, backup)
        # A -wal left by an unclean stop holds the latest answers; without it the backup would miss them.
        for suffix in ("-wal", "-shm"):
            if (side := db.with_name(db.name + suffix)).exists():
                shutil.copy2(side, backup.with_name(backup.name + suffix))
        print(f"Sicherung: {backup}")

    con = sqlite3.connect(db)
    try:
        with con:  # one transaction: all or nothing
            counts = rewrite(con)
            if args.dry_run:
                raise _DryRun(counts)
    except _DryRun as dry:
        counts = dry.counts
    finally:
        con.close()

    print(("Trockenlauf – nichts geschrieben. " if args.dry_run else "Umgeschrieben: ")
          + f"{counts['positions']} Position(en), {counts['exercises']} Standard-Begrenzung(en), {counts['progress']} Karteikasten-Zeilen, "
          + f"{counts['results']} Testergebnisse, {counts['sessions']} Übungsrunden, {counts['attempts']} Tests.")
    if not args.dry_run:
        marker.write_text(f"B-472 umgeschrieben am {datetime.now():%Y-%m-%d %H:%M}\n", encoding="utf-8")
    return 0


class _DryRun(Exception):
    """Raised inside the transaction so that the context manager rolls it back."""

    def __init__(self, counts: dict[str, int]):
        super().__init__("dry run")
        self.counts = counts


if __name__ == "__main__":
    sys.exit(main())
