using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using TagBites.IO.Operations;
using TagBites.IO.Streams;

namespace TagBites.IO.Http
{
    internal class HttpFileSystemOperations :
        IFileSystemReadOperations,
        IFileSystemDirectReadWriteOperations,
        IDisposable
    {
        internal const string DefaultDirectoryInfoFileName = ".dirls";

        private readonly string _address;
        private readonly string _directoryInfoFileName;
        private readonly bool _useCache;

        private WebClient Client { get; }

        public HttpFileSystemOperations(string address, HttpFileSystemOptions options)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));

            Client = new TimeoutWebClient(options.Timeout ?? 5000);
            Client.Encoding = options.Encoding ?? Encoding.UTF8;
            _address = address;
            _directoryInfoFileName = options.DirectoryInfoFileName ?? DefaultDirectoryInfoFileName;
            _useCache = options.UseCache;
        }


        public IFileSystemStructureLinkInfo GetLinkInfo(string fullName)
        {
            var parent = PathHelper.GetDirectoryName(fullName);
            if (string.IsNullOrEmpty(parent))
                return new LinkInfo(fullName, true);

            lock (Client)
            {
                string text;
                try
                {
                    text = Client.DownloadString(PathHelper.Combine(_address, parent, _directoryInfoFileName) + GetRandomSuffix());
                }
                catch (System.Net.WebException e) when ((e.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(text))
                    return ParseDirectoryInfo(parent, text).FirstOrDefault(x => x.FullName == fullName);
            }

            return null;
        }

        public void ReadFile(FileLink file, Stream stream)
        {
            lock (Client)
            {
                using var s = Client.OpenRead(PathHelper.Combine(_address, file.FullName) + GetRandomSuffix())!;
                s.CopyTo(stream);
            }
        }
        public IList<IFileSystemStructureLinkInfo> GetLinks(DirectoryLink directory, FileSystem.ListingOptions options)
        {
            lock (Client)
            {
                string text;
                try
                {
                    text = Client.DownloadString(PathHelper.Combine(_address, directory.FullName, _directoryInfoFileName) + GetRandomSuffix());
                }
                catch (System.Net.WebException e) when ((e.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(text))
                    return ParseDirectoryInfo(directory.FullName, text).ToList();
            }

            return null;
        }

        public FileAccess GetSupportedDirectAccess(FileLink file) => FileAccess.Read;
        public Stream OpenFileStream(FileLink file, FileAccess access, bool overwrite)
        {
            if (access != FileAccess.Read)
                throw new NotSupportedException();

            Monitor.Enter(Client);
            try
            {
                var stream = Client.OpenRead(PathHelper.Combine(_address, file.FullName) + GetRandomSuffix())!;

                return new NotifyOnCloseStream(stream, () => Monitor.Exit(Client));
            }
            catch
            {
                Monitor.Exit(Client);
                throw;
            }
        }

        private static IEnumerable<IFileSystemStructureLinkInfo> ParseDirectoryInfo(string directoryFullName, string directoryInfoContent)
        {
            var lines = directoryInfoContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length < 7 || !string.Equals(parts[0], "D", StringComparison.OrdinalIgnoreCase) && !string.Equals(parts[0], "F", StringComparison.OrdinalIgnoreCase))
                    continue;

                var info = new LinkInfo(
                    PathHelper.Combine(directoryFullName, parts[6]),
                    string.Equals(parts[0], "D", StringComparison.OrdinalIgnoreCase));

                if (parts[1] != "-" && DateTime.TryParse(parts[1], out var d1))
                    info.CreationTime = d1;
                if (parts[2] != "-" && DateTime.TryParse(parts[2], out var d2))
                    info.CreationTime = d2;
                if (parts[3] != "-" && long.TryParse(parts[3], out var length))
                    info.Length = length;
                if (parts[4] != "-" && Enum.TryParse<FileHashAlgorithm>(parts[4], out var ha) && ha != FileHashAlgorithm.None)
                    info.Hash = new FileHash(ha, parts[5]);

                yield return info;
            }
        }

        public string CorrectPath(string path) => path;

        private string GetRandomSuffix()
        {
            return _useCache
                ? "?r=" + Guid.NewGuid().ToString("N")
                : null;
        }

        public void Dispose() => Client.Dispose();

        private class LinkInfo : IFileLinkInfo
        {
            public string FullName { get; }
            public bool Exists => true;
            public bool? IsDirectory { get; }

            public DateTime? CreationTime { get; set; }
            public DateTime? LastWriteTime => null;
            public bool IsHidden => false;
            public bool IsReadOnly => false;

            public string ContentPath => FullName;
            public long Length { get; set; }
            public FileHash Hash { get; set; }

            public bool CanRead => true;
            public bool CanWrite => false;

            public LinkInfo(string fullName, bool isDirectory)
            {
                FullName = fullName;
                IsDirectory = isDirectory;
            }
        }
    }
}
