using System.Reflection;
using TagBites.IO.Operations;
using Xunit;

namespace TagBites.IO.Http.Tests;

public class HttpFileSystemOperationsParseDirectoryInfoTests
{
    [Fact]
    public void ParseDirectoryInfo_FileLine_SetsCreationAndLastWriteTimeFromDistinctColumns()
    {
        const string content = "F\t2024-01-01 10:00:00Z\t2024-02-02 11:00:00Z\t123\tMD5\tabcdef\tfile.txt\n";
        var expectedCreationTime = DateTime.Parse("2024-01-01 10:00:00Z");
        var expectedLastWriteTime = DateTime.Parse("2024-02-02 11:00:00Z");

        var info = ParseDirectoryInfo("/dir", content).Cast<IFileLinkInfo>().Single();

        Assert.Equal(expectedCreationTime, info.CreationTime);
        Assert.Equal(expectedLastWriteTime, info.LastWriteTime);
        Assert.NotEqual(info.CreationTime, info.LastWriteTime);
    }

    private static IEnumerable<IFileSystemStructureLinkInfo> ParseDirectoryInfo(string directoryFullName, string directoryInfoContent, bool recursive = false)
    {
        var method = typeof(HttpFileSystemOperations).GetMethod("ParseDirectoryInfo", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (IEnumerable<IFileSystemStructureLinkInfo>)method.Invoke(null, [directoryFullName, directoryInfoContent, recursive])!;
    }
}
