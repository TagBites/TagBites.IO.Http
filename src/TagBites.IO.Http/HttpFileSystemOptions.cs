#nullable enable
using System.Text;

namespace TagBites.IO.Http;

/// <summary>
/// Provides a set of options to http file system.
/// </summary>
public class HttpFileSystemOptions
{
    /// <summary>
    /// The name of the file containing directory names.
    /// </summary>
    public string? DirectoryInfoFileName { get; set; }

    /// <summary>
    /// The encoding applied to the contents of files.
    /// </summary>
    public Encoding? Encoding { get; set; }

    /// <summary>
    /// The length of time, in milliseconds, before the request times out.
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    /// Determines whether the http file system does not use cache
    /// <returns><see langword="true" /> if http file system uses cache; otherwise, <see langword="false" />.</returns>
    /// </summary>
    public bool PreventCache { get; set; } = false;
}
