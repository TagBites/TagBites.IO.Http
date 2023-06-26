#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TagBites.IO.Operations;

namespace TagBites.IO.Http;

internal class HttpFileSystemWriteOperations : ProxyFileSystemOperations
{
    private readonly Dictionary<DirectoryLink, IList<FileSystemStructureLink>> _map = new();

    public HttpFileSystemWriteOperations(FileSystem write)
        : base(write)
    { }


    public override IFileLinkInfo WriteFile(FileLink file, Stream stream, bool overwrite)
    {
        var info = base.WriteFile(file, stream, overwrite);
        TryCreateDirectoryInfo(ToInnerLink(file).Parent);
        return info;
    }

    public override async Task<IFileLinkInfo> WriteFileAsync(FileLink file, Stream stream, bool overwrite)
    {
        var info = await base.WriteFileAsync(file, stream, overwrite).ConfigureAwait(false);
        await TryCreateDirectoryInfoAsync(ToInnerLink(file).Parent).ConfigureAwait(false);
        return info;
    }

    public override IFileLinkInfo MoveFile(FileLink source, FileLink destination, bool overwrite)
    {
        var info = base.MoveFile(source, destination, overwrite);
        TryCreateDirectoryInfo(ToInnerLink(source).Parent);
        TryCreateDirectoryInfo(ToInnerLink(destination).Parent);
        return info;
    }
    public override async Task<IFileLinkInfo> MoveFileAsync(FileLink source, FileLink destination, bool overwrite)
    {
        var info = await base.MoveFileAsync(source, destination, overwrite).ConfigureAwait(false);
        await TryCreateDirectoryInfoAsync(ToInnerLink(source).Parent).ConfigureAwait(false);
        await TryCreateDirectoryInfoAsync(ToInnerLink(destination).Parent).ConfigureAwait(false);
        return info;
    }

    public override void DeleteFile(FileLink file)
    {
        base.DeleteFile(file);
        TryCreateDirectoryInfo(ToInnerLink(file).Parent);
    }
    public override async Task DeleteFileAsync(FileLink file)
    {
        await base.DeleteFileAsync(file).ConfigureAwait(false);
        await TryCreateDirectoryInfoAsync(ToInnerLink(file).Parent).ConfigureAwait(false);
    }

    public override IFileSystemStructureLinkInfo CreateDirectory(DirectoryLink directory)
    {
        var info = base.CreateDirectory(directory);
        TryCreateDirectoryInfo(directory.Parent);
        return info;
    }
    public override async Task<IFileSystemStructureLinkInfo> CreateDirectoryAsync(DirectoryLink directory)
    {
        var info = await base.CreateDirectoryAsync(directory).ConfigureAwait(false);
        await TryCreateDirectoryInfoAsync(directory.Parent).ConfigureAwait(false);
        return info;
    }

    public override IFileSystemStructureLinkInfo MoveDirectory(DirectoryLink source, DirectoryLink destination)
    {
        var info = base.MoveDirectory(source, destination);
        TryCreateDirectoryInfo(source.Parent);
        TryCreateDirectoryInfo(destination.Parent);
        return info;
    }
    public override async Task<IFileSystemStructureLinkInfo> MoveDirectoryAsync(DirectoryLink source, DirectoryLink destination)
    {
        var info = await base.MoveDirectoryAsync(source, destination).ConfigureAwait(false);
        await TryCreateDirectoryInfoAsync(source.Parent).ConfigureAwait(false);
        await TryCreateDirectoryInfoAsync(destination.Parent).ConfigureAwait(false);
        return info;
    }

    public override void DeleteDirectory(DirectoryLink directory, bool recursive)
    {
        base.DeleteDirectory(directory, recursive);
        TryCreateDirectoryInfo(directory.Parent);
    }
    public override async Task DeleteDirectoryAsync(DirectoryLink directory, bool recursive)
    {
        await base.DeleteDirectoryAsync(directory, recursive).ConfigureAwait(false);
        await TryCreateDirectoryInfoAsync(directory.Parent).ConfigureAwait(false);
    }

    public override IFileSystemStructureLinkInfo UpdateMetadata(FileSystemStructureLink link, IFileSystemLinkMetadata metadata)
    {
        var info = base.UpdateMetadata(link, metadata);
        TryCreateDirectoryInfo(link.Parent);
        return info;
    }
    public override async Task<IFileSystemStructureLinkInfo> UpdateMetadataAsync(FileSystemStructureLink link, IFileSystemLinkMetadata metadata)
    {
        var info = await base.UpdateMetadataAsync(link, metadata).ConfigureAwait(false);
        await TryCreateDirectoryInfoAsync(link.Parent).ConfigureAwait(false);
        return info;
    }

    private static void TryCreateDirectoryInfo(DirectoryLink? directory)
    {
        if (directory != null)
            HttpFileSystem.CreateDirectoryInfo(directory, recursive: false);
    }
    private static async Task TryCreateDirectoryInfoAsync(DirectoryLink? directory)
    {
        if (directory != null)
            HttpFileSystem.CreateDirectoryInfo(directory, recursive: false);
    }

}
