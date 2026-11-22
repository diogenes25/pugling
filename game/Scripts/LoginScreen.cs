using System.Net.Http.Json;
using Godot;
using HttpClient = System.Net.Http.HttpClient;
using Pugling.Contracts.Auth;

namespace PuglingTower;

/// <summary>
/// B-850 spike: proves that a Godot C# build on Android can log a child in against the Pugling API
/// with the shared contract types, and that TLS works from the embedded .NET runtime.
/// </summary>
public partial class LoginScreen : Control
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private LineEdit _server = null!;
    private LineEdit _childId = null!;
    private LineEdit _pin = null!;
    private Label _status = null!;

    // Mirrors every status line into the log, so `adb logcat -s godot` shows what the phone shows.
    private void Show(string text)
    {
        _status.Text = text;
        GD.Print($"SPIKE {text}");
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        _server = GetNode<LineEdit>("Box/Server");
        _childId = GetNode<LineEdit>("Box/ChildId");
        _pin = GetNode<LineEdit>("Box/Pin");
        _status = GetNode<Label>("Box/Status");
        GetNode<Button>("Box/Login").Pressed += () => _ = LoginAsync();
        GetNode<Button>("Box/Tls").Pressed += () => _ = TlsProbeAsync();
        Show($".NET {System.Environment.Version} · {OS.GetName()}");

        // Headless check without clicking: `-- --autologin=<pin>` logs in, probes TLS, prints and quits.
        var autologin = Array.Find(OS.GetCmdlineUserArgs(), a => a.StartsWith("--autologin=", StringComparison.Ordinal));
        if (autologin is not null) _ = AutoRunAsync(autologin["--autologin=".Length..]);
    }

    private async Task AutoRunAsync(string pin)
    {
        _pin.Text = pin;
        await LoginAsync();
        await TlsProbeAsync();
        GetTree().Quit();
    }

    private async Task LoginAsync()
    {
        try
        {
            Show("Anmelden …");
            var dto = new ChildLoginDto(int.Parse(_childId.Text), _pin.Text);
            using var response = await Http.PostAsJsonAsync($"{_server.Text.TrimEnd('/')}/api/v1/auth/child", dto);
            if (!response.IsSuccessStatusCode)
            {
                Show($"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                return;
            }
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            Show($"Angemeldet als {login!.Name} (Rolle {login.Role}), Token bis {login.ExpiresAt:HH:mm}");
        }
        catch (Exception ex)
        {
            Show($"Fehler: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // TLS from the embedded runtime is the riskier half of the spike: the real instance is only reachable via HTTPS.
    private async Task TlsProbeAsync()
    {
        try
        {
            Show("TLS-Probe …");
            using var response = await Http.GetAsync("https://godotengine.org/");
            Show($"TLS ok: HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            Show($"TLS-Fehler: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
