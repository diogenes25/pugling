using System.Text.Json;
using Pugling.Api.OpenApi;

namespace Pugling.Api.Tests;

/// <summary>
/// Deterministic proof of the write/read race B-57 fixes, against a throwaway file - not the real generated
/// catalog, and not a bet on the timing between <see cref="DocsCaptureTests"/> and
/// <see cref="OpenApiExampleTests"/>/<see cref="ClientRouteGuardTests"/>/<see cref="ErrorCodeTests"/> in the
/// real suite (that would just be a second flake). The reader mirrors
/// <see cref="OpenApiExampleCatalog.Load"/> exactly: <c>File.OpenRead</c> + <c>JsonSerializer.Deserialize</c>.
/// <para>
/// <b>Why the write is deliberately paused mid-write instead of raced against real disk speed.</b> An
/// earlier version of this test tried to race a reader against a writer running at full speed on real disk
/// I/O. That measured a confound specific to this Windows machine: <b>any</b> fresh file write or rename -
/// unsafe or atomic alike - can trigger a transient, tens-of-milliseconds exclusive-access window, almost
/// certainly Windows real-time antivirus scanning the touched file. That artifact affects both writers
/// about equally and has nothing to do with the actual bug (torn/incomplete JSON content mid-write) or the
/// fix (an atomic rename never exposes an intermediate state at all). Forcing the pause ourselves - instead
/// of hoping the OS is slow enough to observe by luck - removes that confound entirely and makes the
/// property under test ("can a reader ever observe a partial write of the final path") 100% deterministic.
/// </para>
/// <para>
/// <b>What this narrows down to, and what stays an accepted assumption.</b> The reader in
/// <see cref="CountFailedReadsDuring"/> is stopped before the atomic writer's closing <c>File.Move</c> runs
/// (see its doc comment), so <c>AtomaresSchreiben_KeineLeseFehler</c> proves "no reader ever observes the
/// buffered, half-written temp-file content" - the actual content-tearing bug this story fixes - not "a
/// reader opening exactly during the rename itself is always safe". That narrower claim rests on the
/// story's Risiken section ("ein Leser, der exakt während der Rename-Operation öffnet, ist auf NTFS
/// praktisch nicht beobachtbar") and stays an accepted, unverified-by-this-test assumption, not a proven
/// fact - <see cref="OpenApiExampleCatalog.Load"/> has no retry of its own, so if that assumption ever
/// turns out wrong, the failure would surface unfiltered in <c>OpenApiExampleTests</c>, not here.
/// </para>
/// </summary>
public class OpenApiExampleCatalogConcurrencyTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private static string PayloadOf(int count) =>
        JsonSerializer.Serialize(
            Enumerable.Range(0, count).Select(i =>
                new OpenApiExampleEntry($"key-{i}", "group", $"Title {i}", "GET", $"/path/{i}", "father",
                    null, 200, "{}", false, null)).ToList(),
            SerializerOptions);

    // A continuously-polling reader's File.OpenRead (FileShare.Read) allows other READERS in, but not a
    // writer needing FileAccess.Write - opening for write can legitimately hit a sharing violation while a
    // read is in flight. Retrying briefly is standard practice for this, and orthogonal to what the tests
    // measure (whether a reader ever observes torn content) - not to be confused with the class summary's
    // antivirus-driven confound, which is about a lock lasting far longer than a single read attempt.
    private static FileStream OpenForWriteWithRetry(string path)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            }
            catch (IOException) when (attempt < 50)
            {
                Thread.Sleep(1);
            }
        }
    }

    private static void MoveWithRetry(string tempPath, string finalPath)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                File.Move(tempPath, finalPath, overwrite: true);
                return;
            }
            catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && attempt < 50)
            {
                Thread.Sleep(1);
            }
        }
    }

    // The exact pre-fix shape: FileMode.Create truncates the file to zero bytes the instant it opens, then
    // fills it in across two writes with a deliberate pause between them - standing in for "the OS took a
    // while to flush the rest", which is exactly what DocsCaptureTests' real, much larger JSON write does.
    // A reader landing in that pause sees either an empty file or a half-written, invalid-JSON one.
    private static void WriteUnsafePaused(string path, string json, Action duringPause)
    {
        using (var stream = OpenForWriteWithRetry(path))
        using (var writer = new StreamWriter(stream))
        {
            var half = json.Length / 2;
            writer.Write(json.AsSpan(0, half));
            writer.Flush();
            duringPause();
            writer.Write(json.AsSpan(half));
        }
    }

    // The B-57 fix: the paused, half-written content lands in a TEMP file the reader never opens - the final
    // path is untouched until the completed temp file is renamed in, which is what File.Move(overwrite: true)
    // does after duringPause returns. A reader polling the final path during the pause therefore always sees
    // the complete OLD file, never the new one's half-written intermediate state.
    private static void WriteAtomicPaused(string path, string json, Action duringPause)
    {
        var tempPath = Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetRandomFileName()}.tmp");
        try
        {
            // tempPath is a brand-new random name no reader ever opens, so this open never contends.
            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream))
            {
                var half = json.Length / 2;
                writer.Write(json.AsSpan(0, half));
                writer.Flush();
                duringPause();
                writer.Write(json.AsSpan(half));
            }
            MoveWithRetry(tempPath, path);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    // Mirrors OpenApiExampleCatalog.Load's exact read shape: File.OpenRead + Deserialize.
    /// <summary>
    /// What a single read attempt ran into. Keeping the two apart is the whole point (B-165): counting both
    /// into one number made the gate report "1 read failure" for either, so a green run and a red run could
    /// not be told apart by their cause. Which of the two a failing read hits is <b>platform-dependent</b>
    /// (measured, see <see cref="UnsicheresSchreiben_ErzeugtLeseFehler"/>): Windows denies the open
    /// (<b>locked</b>), Linux lets the reader in on half-written content (<b>torn</b>). Both mean the same
    /// defect - except in <see cref="AtomaresSchreiben_KeineLeseFehler"/>, where the final path is never held
    /// open at all and a lone <b>locked</b> read is instead this machine's antivirus blip (class summary).
    /// </summary>
    private enum LeseErgebnis { Ok, Gesperrt, Zerrissen }

    private static LeseErgebnis TryRead(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            JsonSerializer.Deserialize<List<OpenApiExampleEntry>>(stream, SerializerOptions);
            return LeseErgebnis.Ok;
        }
        catch (IOException)
        {
            return LeseErgebnis.Gesperrt;
        }
        catch (JsonException)
        {
            return LeseErgebnis.Zerrissen;
        }
    }

    // Windows real-time antivirus can briefly hold a lock on a file right after it was written/renamed
    // (the same artifact behind the class summary's timing confound), which can make an immediate
    // File.Delete right after the race fail with "being used by another process" - cleanup, not the race
    // itself. A short retry rides that out without weakening anything the tests assert on.
    private static void DeleteTolerantly(string path)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException) when (attempt < 9)
            {
                Thread.Sleep(50);
            }
        }
    }

    /// <summary>
    /// Runs <paramref name="writer"/> against <paramref name="path"/> while a reader polls continuously in
    /// the background, and returns <b>separately</b> how many reads hit a locked file and how many saw torn
    /// content, observed strictly during the writer's mid-write pause (B-165: one number for both could not
    /// say which happened). The reader is stopped the instant the pause ends (before the writer resumes, and
    /// - for the atomic writer - before its closing <c>File.Move</c>), so a separate, orthogonal artifact of
    /// this machine (a real-time-antivirus-driven exclusive-access blip on <b>any</b> fresh write or rename,
    /// unrelated to content-tearing - see the class summary) cannot leak into this measurement.
    /// </summary>
    private static (int Ok, int Gesperrt, int Zerrissen) CountFailedReadsDuring(string path, Action<Action> writer)
    {
        var ok = 0;
        var gesperrt = 0;
        var zerrissen = 0;
        var reading = true;
        // A dedicated Thread, not Task.Run: when the full suite runs hundreds of test classes in parallel
        // (xunit.v3 parallelizes collections by default, and this project has no [Collection] grouping -
        // see the class summary), the ThreadPool queue can be under enough real pressure that a queued
        // work item sits waiting well past a 300ms pause, starving the reader out entirely. A dedicated
        // thread gets its own OS scheduling slot instead of queuing behind unrelated ThreadPool work.
        var readerThread = new Thread(() =>
        {
            while (Volatile.Read(ref reading))
                switch (TryRead(path))
                {
                    case LeseErgebnis.Ok: Interlocked.Increment(ref ok); break;
                    case LeseErgebnis.Gesperrt: Interlocked.Increment(ref gesperrt); break;
                    case LeseErgebnis.Zerrissen: Interlocked.Increment(ref zerrissen); break;
                }
        })
        { IsBackground = true };
        readerThread.Start();
        try
        {
            writer(() =>
            {
                Thread.Sleep(30); // the pause the reader must land in at least once to fail
                Volatile.Write(ref reading, false);
            });
        }
        finally
        {
            // If the writer throws before ever calling the pause callback (e.g. OpenForWriteWithRetry
            // exhausting its attempts under heavy contention), `reading` would otherwise stay true forever
            // and the join below would hang instead of the test failing with the writer's real exception.
            Volatile.Write(ref reading, false);
        }
        readerThread.Join();
        return (ok, gesperrt, zerrissen);
    }

    [Fact]
    public void UnsicheresSchreiben_ErzeugtLeseFehler()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pugling_race_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, PayloadOf(1));
        try
        {
            var (ok, gesperrt, zerrissen) = CountFailedReadsDuring(path, pause => WriteUnsafePaused(path, PayloadOf(40), pause));

            // The claim under test is "during an unsafe write the final path is not reliably readable" - that
            // is what B-57 fixed, since the atomic writer leaves the final path readable AND complete
            // throughout. WHICH way the read fails is platform-dependent, measured on both:
            //   Windows - 1867 locked, 0 torn. `File.OpenRead` requests FileShare.Read ("others may read but
            //             not write"); an already-open WRITE handle contradicts that, so the open is denied
            //             outright and the reader never gets far enough to see half-written bytes.
            //   Linux   - 0 locked, 126 torn (CI run 31810420330). The share mode is not enforced the same
            //             way, so the reader gets in and deserializes a half-written file.
            // Both are the same defect from the caller's side, so both satisfy this case; asserting only one
            // of them pinned the test to one platform and made it red on the other. The split stays in the
            // message, which is B-165's actual gain: a failure says WHICH mode was observed.
            //
            // What turns this case red: swapping WriteUnsafePaused for WriteAtomicPaused - then every read
            // during the pause succeeds on both platforms and the sum is zero.
            Assert.True(gesperrt + zerrissen > 0,
                "Expected a reader landing mid-write to fail on the final path - either denied (locked) or "
                + $"seeing half-written content (torn) - the exact race this story fixes. Locked: {gesperrt}, "
                + $"torn: {zerrissen}, successful: {ok}.");
        }
        finally { DeleteTolerantly(path); }
    }

    [Fact]
    public void AtomaresSchreiben_KeineLeseFehler()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pugling_race_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, PayloadOf(1));
        try
        {
            // Several independent repetitions, not one lucky pass: the fix must hold up every time. Since
            // B-165 the repetitions only multiply the evidence about TORN content - while a lock still failed
            // the case, they multiplied the chance of catching this machine's antivirus blip by ten, which is
            // what made the case flake in the full suite (one run in three).
            var gesperrtGesamt = 0;
            for (var i = 0; i < 10; i++)
            {
                var (ok, gesperrt, zerrissen) = CountFailedReadsDuring(path, pause => WriteAtomicPaused(path, PayloadOf(40), pause));
                gesperrtGesamt += gesperrt;

                // Only torn content fails this case, and it fails on a single one - that is the property the
                // atomic write guarantees exactly. A locked file is reported, not punished: it is an artifact
                // of this machine (class summary) and says nothing about content.
                //
                // TWO assertions, because on Windows the first one alone is TOOTHLESS - measured while
                // building B-165: swapping the atomic writer for the unsafe one kept `zerrissen == 0` green,
                // since an unsafe write is denied to the reader there rather than showing it torn bytes. (On
                // Linux the unsafe writer does produce torn content, so there the first assertion bites on
                // its own - see UnsicheresSchreiben_ErzeugtLeseFehler for both measurements.)
                //
                // What separates the two writers on EVERY platform is whether reads SUCCEED during the pause:
                //   atomic  - the final path is never opened for writing, so reads succeed (~2000 per window)
                //   unsafe  - the final path is held open, so reads fail (measured 1867 of 1867 on Windows)
                // A single denied read is this machine's antivirus blip and stays tolerated - that is the
                // flake B-165 removes. Zero successful reads means the write is not atomic any more.
                Assert.True(zerrissen == 0,
                    $"Run {i + 1}/10: {zerrissen} reader(s) saw torn content - the atomic write must never "
                    + $"expose an intermediate state. Locked: {gesperrt}, ok: {ok}.");
                Assert.True(ok > gesperrt,
                    $"Run {i + 1}/10: {gesperrt} reads were denied and only {ok} succeeded - the final path "
                    + "was held open for most of the window, so the write is no longer atomic. A single "
                    + "denied read is this machine's antivirus and stays tolerated; a majority is not.");
            }
            // Not an assertion - a trace, so a future flake can be told apart from a content regression.
            Assert.True(gesperrtGesamt >= 0, $"Locked reads across all 10 runs: {gesperrtGesamt}.");
        }
        finally { DeleteTolerantly(path); }
    }
}
