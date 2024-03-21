using System;
using System.Collections.Generic;
using System.Text;

namespace TagBites.IO.Http
{
    /// <summary>
    /// Exposes static methods for creating Http file system.
    /// </summary>
    public static class HttpFileSystem
    {
        /// <summary>
        /// Creates a Http file system.
        /// </summary>
        /// <param name="address">The Http address.</param>
        /// <param name="directoryInfoFileName">The name of the file containing directory names.</param>
        /// <param name="encoding">The encoding applied to the contents of files.</param>
        /// <param name="timeout">The length of time, in milliseconds, before the request times out.</param>
        /// <returns>A Http file system contains the procedures that are used to perform file and directory operations.</returns>
        public static FileSystem Create(string address, string directoryInfoFileName = null, Encoding encoding = null, int? timeout = null)
        {
            var options = new HttpFileSystemOptions
            {
                DirectoryInfoFileName = directoryInfoFileName,
                Encoding = encoding,
                Timeout = timeout
            };

            return new FileSystem(new HttpFileSystemOperations(address, options));
        }

        /// <summary>
        /// Creates a Http file system.
        /// </summary>
        /// <param name="address">The Http address.</param>
        /// <param name="options"></param>
        /// <returns>A Http file system contains the procedures that are used to perform file and directory operations.</returns>
        public static FileSystem Create(string address, HttpFileSystemOptions options)
        {
            return new FileSystem(new HttpFileSystemOperations(address, options));
        }

        /// <summary>
        /// Creates a Http file system with write file system.
        /// </summary>
        /// <param name="writeFileSystem">A file system with writing methods.</param>
        /// <returns></returns>
        public static FileSystem CreateBuilder(FileSystem writeFileSystem)
        {
            writeFileSystem = new FileSystem(new HttpFileSystemWriteOperations(writeFileSystem));
            return writeFileSystem;
        }

        /// <summary>
        /// Creates a file with information about files in directory.
        /// File line format: D/F   Length   Created    Modified    Hash Algorithm    Hash    Name
        /// </summary>
        /// <param name="directory">The link to the directory.</param>
        /// <param name="directoryInfoFileName">The name of file with directory information.</param>
        /// <param name="recursive">Recursive.</param>
        public static void CreateDirectoryInfo(DirectoryLink directory, string directoryInfoFileName = null, bool recursive = true)
        {
            const string empty = "-";
            directoryInfoFileName ??= HttpFileSystemOperations.DefaultDirectoryInfoFileName;

            var stack = new Stack<DirectoryLink>();
            stack.Push(directory);

            while (stack.Count > 0)
            {
                directory = stack.Pop();

                var sb = new StringBuilder();


                foreach (var link in directory.GetLinks())
                {
                    if (link.Name == directoryInfoFileName)
                        continue;
                    if (link.Type == FileSystemLinkType.Directory && recursive)
                        stack.Push((DirectoryLink)link);

                    var file = link as IFileResourceLink;
                    var hash = file?.Hash ?? FileHash.Empty;

                    sb.AppendFormat("{0}\t{1:u}\t{2:u}\t{3}\t{4}\t{5}\t{6}\n",
                        link.Type == FileSystemLinkType.Directory ? 'D' : 'F',
                        link.CreationTime ?? DateTime.MinValue,
                        link.ModifyTime ?? DateTime.MinValue,
                        file?.Length.ToString() ?? empty,
                        hash.IsEmpty ? "-" : hash.Algorithm.ToString(),
                        hash.IsEmpty ? "-" : hash.Value,
                        link.Name);
                }

                directory.GetFile(directoryInfoFileName).WriteAllText(sb.ToString());
            }
        }
        /// <summary>
        /// Creates a file with information about recursive files in directory.
        /// </summary>
        /// <param name="directory">The link to the directory.</param>
        /// <param name="ignoredPaths">The set of ignored paths.</param>
        /// <param name="directoryInfoFileName">The name of file with directory information.</param>
        public static void CreateRecursiveDirectoryInfo(DirectoryLink directory, IEnumerable<string>? ignoredPaths = null, string? directoryInfoFileName = null)
        {
            const string empty = "-";
            directoryInfoFileName ??= HttpFileSystemOperations.DefaultRecursiveDirectoryInfoFileName;
            var ignoredPathsSet = ignoredPaths != null ? new HashSet<string>(ignoredPaths) : null;

            var stack = new Stack<DirectoryLink>();
            stack.Push(directory);

            var sb = new StringBuilder();
            while (stack.Count > 0)
            {
                var currentDirectory = stack.Pop();
                foreach (var link in currentDirectory.GetLinks())
                {
                    if (link.Name == directoryInfoFileName || link.Name == HttpFileSystemOperations.DefaultDirectoryInfoFileName)
                        continue;

                    if (ignoredPathsSet?.Contains(link.FullName) == true)
                        continue;

                    if (link.Type == FileSystemLinkType.Directory)
                        stack.Push((DirectoryLink)link);

                    var file = link as IFileResourceLink;
                    var hash = file?.Hash ?? FileHash.Empty;

                    sb.AppendFormat("{0}\t{1:u}\t{2:u}\t{3}\t{4}\t{5}\t{6}\n",
                        link.Type == FileSystemLinkType.Directory ? 'D' : 'F',
                        link.CreationTime ?? DateTime.MinValue,
                        link.ModifyTime ?? DateTime.MinValue,
                        file?.Length.ToString() ?? empty,
                        hash.IsEmpty ? "-" : hash.Algorithm.ToString(),
                        hash.IsEmpty ? "-" : hash.Value,
                        link.FullName);
                }
            }

            directory.GetFile(directoryInfoFileName).WriteAllText(sb.ToString());
        }
    }


}
