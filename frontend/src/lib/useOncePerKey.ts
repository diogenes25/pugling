import { useEffect, useRef } from "react";

/**
 * Runs `start()` (an async operation) at most once per distinct `key`, even across a React effect firing
 * twice for the same mount (StrictMode's simulated remount in dev, or a genuine remount) - a `startSession`
 * POST fired twice created two `PracticeSession` rows instead of one (B-62).
 * <p>
 * A naive boolean gate ("already started for this key? skip.") is not enough on its own: paired with an
 * `alive` flag that the effect's cleanup flips false, the FIRST invocation's cleanup marks the shared work
 * dead before it resolves, and the SECOND invocation - the one that actually survives - never restarts the
 * work (the gate blocks it) and never sees the result either (its own `alive` flag was never wired up to
 * anything). The screen gets stuck loading forever. This bit during this very fix's own E2E verification.
 * </p>
 * <p>
 * The fix: cache the in-flight/settled <b>promise</b> per key, not just a boolean. Every invocation -
 * including one that does not (re-)start the work - still attaches its own `onDone`/`onError` handler,
 * gated by its own "still mounted" flag returned from its own cleanup. Only the promise creation is
 * deduplicated by key; consuming the result is not.
 * </p>
 */
export function useOncePerKey<T>(
  key: string | null,
  start: () => Promise<T>,
  onDone: (value: T) => void,
  onError: (error: unknown) => void,
) {
  const work = useRef<{ key: string; promise: Promise<T> } | null>(null);
  useEffect(() => {
    // A `null` key (no early-return effect has fired yet, or the caller intentionally pauses) leaves
    // `work` untouched - a later call with the SAME key as before `null` therefore replays the cached
    // promise instead of restarting. Harmless for the current caller (SohnPractice.tsx's key never goes
    // null and then back to the same value), but worth knowing before reusing this for a more volatile key.
    if (key === null) return;
    let alive = true;
    if (work.current?.key !== key) work.current = { key, promise: start() };
    work.current.promise.then(
      (value) => { if (alive) onDone(value); },
      (error) => { if (alive) onError(error); },
    );
    return () => { alive = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);
}
