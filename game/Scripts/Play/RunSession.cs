using Godot;
using Pugling.Contracts.Student;
using PuglingTower.Core;
using PuglingTower.Net;

namespace PuglingTower.Play;

/// <summary>
/// The level's line to the server for one run: the queue of tower meanings, grading a word, and the heartbeat that
/// counts the play time. The server keeps which word fits which tower; the client never knows.
/// </summary>
public sealed class RunSession(ITowerRunServer server, TowerRunResponse run)
{
    private readonly Queue<TowerTarget> _targets = new(run.Towers);
    private double _sinceBeat;
    private bool _beating;

    /// <summary>The run as the server started it.</summary>
    public TowerRunResponse Run { get; } = run;

    /// <summary>The next meaning a tower asks for, or <c>null</c> when all are handed out.</summary>
    public TowerTarget? NextTarget() => _targets.Count > 0 ? _targets.Dequeue() : null;

    /// <summary>Lets the server grade a word at a tower.</summary>
    public Task<TowerAssignResult> AssignAsync(TowerToken token, TowerTarget target) =>
        server.AssignAsync(Run.RunId, token.TokenId, target.TowerId);

    /// <summary>Counts played time and sends a heartbeat every <see cref="Balance.BeatInterval"/> seconds.</summary>
    /// <param name="delta">Seconds since the last frame (only while the level runs, not paused).</param>
    /// <param name="timeUp">Called when the server says today's play time is used up.</param>
    public void Tick(double delta, Action timeUp)
    {
        _sinceBeat += delta;
        if (_sinceBeat >= Balance.BeatInterval && !_beating) _ = BeatAsync(timeUp);
    }

    private async Task BeatAsync(Action timeUp)
    {
        _beating = true;
        _sinceBeat = 0;
        try
        {
            var beat = await server.HeartbeatAsync(Run.RunId);
            if (beat.TimeUp) timeUp();
        }
        catch (ApiException)
        {
            // A lost beat costs only its seconds; the next one goes out in 15 s.
        }
        finally
        {
            _beating = false;
        }
    }

    /// <summary>
    /// One line in the game log with the local time. project.godot turns on file logging, so the lines survive in
    /// user://logs on the phone - its logcat buffer is overwritten within minutes. Keep the "TURM" prefix: the run
    /// reports are read by it.
    /// </summary>
    public static void Log(string line) => GD.Print($"TURM {Time.GetTimeStringFromSystem()} {line}");
}
