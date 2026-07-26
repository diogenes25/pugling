namespace Pugling.Api.Services.Shared;

/// <summary>
/// Einstellungen der Bild-Ablage. <see cref="RootPath"/> liegt bewusst <b>neben</b> und nicht in
/// <c>wwwroot</c>: dorthin kopiert der Deploy das gebaute Frontend, ein Redeploy würde hochgeladene
/// Bilder also mitlöschen. Hochgeladene Inhalte gehören nicht in ein Build-Artefakt-Verzeichnis.
/// </summary>
public class MediaOptions
{
    /// <summary>Ablageordner (absolut oder relativ zum Content-Root). Default: <c>media-uploads</c>.</summary>
    public string RootPath { get; set; } = "media-uploads";

    /// <summary>Öffentliches URL-Präfix, unter dem der Ordner ausgeliefert wird.</summary>
    public string PublicPath { get; set; } = "/media";

    /// <summary>Obergrenze je Upload in Bytes (Default 10 MB) – Schutz vor versehentlichen Riesendateien.</summary>
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
}

/// <summary>
/// Wohin die Bilddateien geschrieben werden. Als Schnittstelle, damit später ein Blob-Storage danebentreten
/// kann, ohne die Upload-Logik anzufassen – die Alternative wäre <c>File.WriteAllBytes</c> mitten im
/// Controller und ein Umbau quer durch die Anwendung, sobald ein zweiter Host dazukommt.
/// </summary>
public interface IMediaStorage
{
    /// <summary>Schreibt eine Datei und liefert ihre öffentliche URL.</summary>
    /// <param name="relativePath">Pfad unterhalb der Wurzel, z. B. <c>run_unicorn/card.webp</c>.</param>
    /// <param name="content">Inhalt der Datei.</param>
    /// <param name="ct">Abbruch-Token.</param>
    Task<string> SaveAsync(string relativePath, ReadOnlyMemory<byte> content, CancellationToken ct = default);

    /// <summary>Entfernt alle Dateien eines Assets (Ordner). Fehlt der Ordner, passiert nichts.</summary>
    Task DeleteFolderAsync(string folder, CancellationToken ct = default);
}

/// <summary>
/// Ablage im lokalen Dateisystem – die Variante für den Single-Host-Deploy. Der Ordner wird per eigener
/// Static-Files-Middleware unter <see cref="MediaOptions.PublicPath"/> ausgeliefert (siehe Program.cs).
/// </summary>
public class LocalMediaStorage(MediaOptions options, IWebHostEnvironment env, ILogger<LocalMediaStorage> logger)
    : IMediaStorage
{
    /// <summary>Absoluter Ablageordner; relative Konfiguration bezieht sich auf den Content-Root.</summary>
    public string Root { get; } = Path.IsPathRooted(options.RootPath)
        ? options.RootPath
        : Path.Combine(env.ContentRootPath, options.RootPath);

    /// <inheritdoc/>
    public async Task<string> SaveAsync(string relativePath, ReadOnlyMemory<byte> content, CancellationToken ct = default)
    {
        var full = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, content, ct);
        return $"{options.PublicPath.TrimEnd('/')}/{relativePath.Replace('\\', '/')}";
    }

    /// <inheritdoc/>
    public Task DeleteFolderAsync(string folder, CancellationToken ct = default)
    {
        var full = Resolve(folder);
        // Best effort: eine verwaiste Datei ist harmlos, ein Serverfehler beim Löschen des Assets nicht.
        try { if (Directory.Exists(full)) Directory.Delete(full, recursive: true); }
        catch (IOException e) { logger.LogWarning(e, "Medien-Ordner {Folder} konnte nicht gelöscht werden", folder); }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Löst einen relativen Pfad auf und stellt sicher, dass er <b>innerhalb</b> der Wurzel bleibt.
    /// Ohne diese Prüfung wäre ein Key wie <c>../../appsettings.json</c> ein Schreibzugriff aufs
    /// halbe Dateisystem – der Key kommt aus einer Nutzereingabe.
    /// </summary>
    private string Resolve(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(Root, relativePath));
        var root = Path.GetFullPath(Root) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Path '{relativePath}' escapes the media root.");
        return full;
    }
}
