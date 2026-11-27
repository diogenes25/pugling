using System.Net.Http.Headers;
using System.Net.Http.Json;
using Pugling.Contracts.Auth;
using Pugling.Contracts.Student;
using HttpClient = System.Net.Http.HttpClient;

namespace PuglingTower.Net;

/// <summary>
/// The game's thin HTTP layer against the Pugling API: child login and the tower-game endpoints (B-851). Uses the
/// shared contract types; every failure surfaces as an <see cref="ApiException"/> with a German message for the screen.
/// </summary>
public sealed class PuglingApi : ITowerRunServer, IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private string _root = "";

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();

    /// <summary>The logged-in child's id, set by <see cref="LoginAsync"/>.</summary>
    public int ChildId { get; private set; }

    /// <summary>The logged-in child's display name.</summary>
    public string Name { get; private set; } = "";

    /// <summary>Logs the child in with its PIN and keeps the bearer token for all further calls.</summary>
    public async Task LoginAsync(string server, int childId, string pin)
    {
        _root = server.TrimEnd('/') + "/api/v1/";
        var login = await SendAsync<LoginResponse>(HttpMethod.Post, "auth/child", new ChildLoginDto(childId, pin));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        ChildId = childId;
        Name = login.Name;
    }

    /// <summary>The vocabulary positions a run can be built from.</summary>
    public Task<TowerGameOverview> OverviewAsync() =>
        SendAsync<TowerGameOverview>(HttpMethod.Get, $"student/children/{ChildId}/tower-game", null);

    /// <summary>Starts a run from one plan position.</summary>
    public Task<TowerRunResponse> StartRunAsync(int planPositionId) =>
        SendAsync<TowerRunResponse>(HttpMethod.Post, $"student/children/{ChildId}/tower-game/runs",
            new StartTowerRunDto(planPositionId));

    /// <summary>Lets the server grade a token-to-tower assignment.</summary>
    public Task<TowerAssignResult> AssignAsync(int runId, string tokenId, string towerId) =>
        SendAsync<TowerAssignResult>(HttpMethod.Post, $"student/children/{ChildId}/tower-game/assign",
            new AssignTowerTokenDto(runId, tokenId, towerId));

    /// <summary>Reports that the level is still in the foreground; the server counts the play time.</summary>
    public Task<TowerHeartbeatResult> HeartbeatAsync(int runId) =>
        SendAsync<TowerHeartbeatResult>(HttpMethod.Post, $"student/children/{ChildId}/tower-game/runs/{runId}/heartbeat", null);

    /// <summary>Ends the level; the server decides by its own count whether it is won and pays the coins.</summary>
    public Task<TowerRunEndResult> EndAsync(int runId) =>
        SendAsync<TowerRunEndResult>(HttpMethod.Post, $"student/children/{ChildId}/tower-game/runs/{runId}/end", null);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body)
    {
        using var request = new HttpRequestMessage(method, _root + path);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType());
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex)
        {
            throw new ApiException($"Server nicht erreichbar ({ex.GetType().Name}).");
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // The ProblemDetails body carries a stable code; only the 409 cases need telling apart here.
                var problem = await response.Content.ReadAsStringAsync();
                throw new ApiException((int)response.StatusCode switch
                {
                    401 => "PIN falsch oder Anmeldung abgelaufen.",
                    404 => "Nicht gefunden – ist der Plan aktiv?",
                    409 when problem.Contains("tower_game_time_limit") => "Deine Spielzeit für heute ist um. Morgen geht's weiter!",
                    409 when problem.Contains("tower_game_run_closed") => "Das Level ist vorbei.",
                    409 => "Gerade kam etwas dazwischen – bitte nochmal.",
                    429 => "Zu viele Versuche, kurz warten.",
                    var code => $"Fehler vom Server (HTTP {code}).",
                }, problem.Contains("tower_game_run_closed"));
            }
            // A 200 that is not the expected JSON (wrong host, a router page) must reach the screen like any failure.
            try
            {
                return await response.Content.ReadFromJsonAsync<T>()
                    ?? throw new ApiException("Unerwartete Antwort vom Server.");
            }
            catch (Exception ex) when (ex is not ApiException)
            {
                throw new ApiException($"Unerwartete Antwort vom Server ({ex.GetType().Name}).");
            }
        }
    }
}

/// <summary>
/// The two calls a running level makes. <see cref="PuglingApi"/> is the real one; a stand-in lets a level run without a
/// server (offline practice, a smoke run).
/// </summary>
public interface ITowerRunServer
{
    /// <summary>Lets the server grade a token-to-tower assignment.</summary>
    Task<TowerAssignResult> AssignAsync(int runId, string tokenId, string towerId);

    /// <summary>Reports that the level is still in the foreground; the server counts the play time.</summary>
    Task<TowerHeartbeatResult> HeartbeatAsync(int runId);
}

/// <summary>A failed API call with a message ready for the screen; <see cref="RunClosed"/> means the level is over.</summary>
public sealed class ApiException(string message, bool runClosed = false) : Exception(message)
{
    /// <summary>The server took no more assignments for this run (ended, or the grace time after the limit is over).</summary>
    public bool RunClosed { get; } = runClosed;
}
