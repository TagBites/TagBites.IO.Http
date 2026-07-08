using Xunit;

namespace TagBites.IO.Http.Tests;

public class PathHelperTests
{
    [Theory]
    [InlineData("a", "b", "a/b")]
    [InlineData("a/", "b", "a/b")]
    [InlineData("a", "", "a")]
    [InlineData("", "b", "b")]
    public void Combine_TwoArgs_VariousInputs_CorrectResult(string path1, string path2, string expected)
    {
        Assert.Equal(expected, PathHelper.Combine(path1, path2));
    }

    [Fact]
    public void Combine_ThreeArgs_CombinesAllSegments()
    {
        Assert.Equal("a/b/c", PathHelper.Combine("a", "b", "c"));
    }

    [Fact]
    public void GetDirectoryName_Null_ReturnsNull()
    {
        Assert.Null(PathHelper.GetDirectoryName(null));
    }

    [Fact]
    public void GetDirectoryName_NoSeparator_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, PathHelper.GetDirectoryName("file.txt"));
    }

    [Fact]
    public void GetDirectoryName_NestedPath_ReturnsParent()
    {
        Assert.Equal("a/b", PathHelper.GetDirectoryName("a/b/c"));
    }

    [Fact]
    public void GetDirectoryName_RootLevelFile_ReturnsRootSeparator()
    {
        Assert.Equal("/", PathHelper.GetDirectoryName("/file.txt"));
    }
}
