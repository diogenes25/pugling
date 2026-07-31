using Microsoft.Extensions.FileProviders;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Settings for the image store. <see cref="RootPath"/> deliberately lives <b>next to</b>, not inside,
/// <c>wwwroot</c>: the deploy copies the built frontend there, so a redeploy would delete uploaded
/// images along with it. Uploaded content does not belong in a build artifact directory.
/// </summary>
public class MediaOptions
{
    /// <summary>Storage folder (absolute or relative to the content root). Default: <c>media-uploads</c>.</summary>
    public string RootPath { get; set; } = "media-uploads";

    /// <summary>Public URL prefix under which the folder is served.</summary>
    public string PublicPath { get; set; } = "/media";

    /// <summary>Upper limit per upload in bytes (default 10 MB) – protection against accidentally huge files.</summary>
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
}

/// <summary>
/// Where the image files are written. As an interface, so a blob storage can later step in alongside it
/// without touching the upload logic – the alternative would be <c>File.WriteAllBytes</c> right in the
/// middle of the controller and a rebuild across the whole application as soon as a second host is added.
/// </summary>
public interface IMediaStorage
{
    /// <summary>Writes a file and returns its public URL.</summary>
    /// <param name="relativePath">Path below the root, e.g. <c>run_unicorn/card.webp</c>.</param>
    /// <param name="content">Content of the file.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> SaveAsync(string relativePath, ReadOnlyMemory<byte> content, CancellationToken ct = default);

    /// <summary>Removes all files of an asset (folder). If the folder is missing, nothing happens.</summary>
    Task DeleteFolderAsync(string folder, CancellationToken ct = default);

    /// <summary>
    /// The file provider through which the store can be served statically – or <c>null</c> if it serves
    /// its files itself (blob storage with its own URLs). Deliberately here and not as a path: the
    /// static files middleware needs exactly that. A cast to the concrete class in the startup path
    /// would break every other implementation at startup – and that is exactly why this interface exists.
    /// </summary>
    IFileProvider? CreateContentProvider();
}

/// <summary>
/// Storage on the local file system – the variant for the single-host deploy. The folder is served
/// under <see cref="MediaOptions.PublicPath"/> by a dedicated static files middleware (see Program.cs).
/// </summary>
public class LocalMediaStorage(MediaOptions options, IWebHostEnvironment env, ILogger<LocalMediaStorage> logger)
    : IMediaStorage
{
    /// <summary>Absolute storage folder; relative configuration is relative to the content root.</summary>
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
    /// <remarks>
    /// The folder is created here, not in the constructor: <see cref="PhysicalFileProvider"/> requires
    /// an existing directory, and on first start there is none yet.
    /// </remarks>
    public IFileProvider? CreateContentProvider()
    {
        Directory.CreateDirectory(Root);
        return new PhysicalFileProvider(Root);
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
    /// Resolves a relative path and ensures it stays <b>inside</b> the root. Without this check, a key
    /// like <c>../../appsettings.json</c> would grant write access to half the file system – the key
    /// comes from user input.
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
