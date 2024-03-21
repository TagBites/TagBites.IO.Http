using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TagBites.IO.Operations;
using TagBites.IO.Streams;
using TagBites.Utils;

namespace TagBites.IO.Http
{
    internal class HttpFileSystemOperations :
        IFileSystemReadOperations,
        IFileSystemAsyncReadOperations,
        IFileSystemDirectReadWriteOperations,
        IFileSystemAsyncDirectReadWriteOperations
    {
        internal const string DefaultDirectoryInfoFileName = ".dirls";
        internal const string DefaultRecursiveDirectoryInfoFileName = ".dirrls";

        private readonly string _address;
        private readonly string _directoryInfoFileName;
        private readonly bool _preventCache;
        private readonly int _timeout;
        private readonly Encoding _encoding;
        private readonly AsyncLock _locker = new();

        public string Kind => "http";
        public string Name => _address;

        public HttpFileSystemOperations(string address, HttpFileSystemOptions options)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));

            _address = address;
            _directoryInfoFileName = options.DirectoryInfoFileName ?? DefaultDirectoryInfoFileName;
            _preventCache = options.PreventCache;
            _timeout = options.Timeout ?? 5000;
            _encoding = options.Encoding ?? Encoding.UTF8;
        }


        public IFileSystemStructureLinkInfo GetLinkInfo(string fullName)
        {
            var parent = PathHelper.GetDirectoryName(fullName);
            if (string.IsNullOrEmpty(parent))
                return new LinkInfo(fullName, true);

            using (_locker.Lock())
            {
                string text;
                try
                {
                    var address = PathHelper.Combine(_address, parent, _directoryInfoFileName) + GetRandomSuffix();
                    using var client = CreateWebClient();
                    text = client.DownloadString(address);
                }
                catch (System.Net.WebException e) when ((e.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(text))
                {
                    var infos = ParseDirectoryInfo(parent, text);
                    var info = infos.FirstOrDefault(x => x.FullName == fullName);
                    if (info == null)
                    {

                    }
                    return info;
                }
            }

            return null;
        }
        public async Task<IFileSystemStructureLinkInfo> GetLinkInfoAsync(string fullName)
        {
            var parent = PathHelper.GetDirectoryName(fullName);
            if (string.IsNullOrEmpty(parent))
                return new LinkInfo(fullName, true);

            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                string text;
                try
                {
                    using var client = CreateHttpClient();
                    text = await client.GetStringAsync(PathHelper.Combine(_address, parent, _directoryInfoFileName) + GetRandomSuffix()).ConfigureAwait(false);
                }
                catch (System.Net.WebException e) when ((e.Response as HttpWebResponse)?.StatusCode ==
                                                        HttpStatusCode.NotFound)
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
            using (_locker.Lock())
            {
                using var client = CreateWebClient();
                using var s = client.OpenRead(PathHelper.Combine(_address, file.FullName) + GetRandomSuffix())!;
                s.CopyTo(stream);
            }
        }
        public async Task ReadFileAsync(FileLink file, Stream stream)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                using var client = CreateHttpClient();
                using var s = await client.GetStreamAsync(PathHelper.Combine(_address, file.FullName) + GetRandomSuffix())!.ConfigureAwait(false);
                await s.CopyToAsync(stream).ConfigureAwait(false);
            }
        }

        public IList<IFileSystemStructureLinkInfo> GetLinks(DirectoryLink directory, FileSystem.ListingOptions options)
        {
            using (_locker.Lock())
            {
                string text;
                try
                {
                    using var client = CreateWebClient();

                    options.RecursiveHandled = options.Recursive && directory.FullName == "/";
                    var infoFileName = options.RecursiveHandled
                        ? DefaultRecursiveDirectoryInfoFileName
                        : DefaultDirectoryInfoFileName;

                    text = client.DownloadString(PathHelper.Combine(_address, directory.FullName, infoFileName) + GetRandomSuffix());
                }
                catch (System.Net.WebException e) when ((e.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                {
                    return Array.Empty<IFileSystemStructureLinkInfo>();
                }

                if (!string.IsNullOrEmpty(text))
                    return ParseDirectoryInfo(directory.FullName, text, options.RecursiveHandled).ToList();
            }

            return Array.Empty<IFileSystemStructureLinkInfo>();
        }
        public async Task<IList<IFileSystemStructureLinkInfo>> GetLinksAsync(DirectoryLink directory, FileSystem.ListingOptions options)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                string text;
                try
                {
                    using var client = CreateHttpClient();

                    options.RecursiveHandled = options.Recursive && directory.FullName == "/";
                    var infoFileName = options.RecursiveHandled
                        ? DefaultRecursiveDirectoryInfoFileName
                        : DefaultDirectoryInfoFileName;

                    text = await client.GetStringAsync(PathHelper.Combine(_address, directory.FullName, infoFileName) + GetRandomSuffix()).ConfigureAwait(false);
                }
                catch (System.Net.WebException e) when ((e.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                {
                    return Array.Empty<IFileSystemStructureLinkInfo>();
                }

                if (!string.IsNullOrEmpty(text))
                    return ParseDirectoryInfo(directory.FullName, text, options.RecursiveHandled).ToList();
            }

            return Array.Empty<IFileSystemStructureLinkInfo>();
        }

        public FileAccess GetSupportedDirectAccess(FileLink file) => FileAccess.Read;
        public Stream OpenFileStream(FileLink file, FileAccess access, bool overwrite)
        {
            if (access != FileAccess.Read)
                throw new NotSupportedException();

            var locker = _locker.Lock();
            try
            {
                using var client = CreateWebClient();
                var stream = client.OpenRead(PathHelper.Combine(_address, file.FullName) + GetRandomSuffix())!;

                // ReSharper disable once AccessToDisposedClosure
                return new NotifyOnCloseStream(stream, locker.Dispose);
            }
            catch
            {
                locker.Dispose();
                throw;
            }
        }
        public async Task<Stream> OpenFileStreamAsync(FileLink file, FileAccess access, bool overwrite)
        {
            if (access != FileAccess.Read)
                throw new NotSupportedException();

            var locker = await _locker.LockAsync().ConfigureAwait(false);
            try
            {
                using var client = CreateHttpClient();
                var stream = await client.GetStreamAsync(PathHelper.Combine(_address, file.FullName) + GetRandomSuffix())!.ConfigureAwait(false);

                // ReSharper disable once AccessToDisposedClosure
                return new NotifyOnCloseStream(stream, locker.Dispose);
            }
            catch
            {
                locker.Dispose();
                throw;
            }
        }

        private static IEnumerable<IFileSystemStructureLinkInfo> ParseDirectoryInfo(string directoryFullName, string directoryInfoContent, bool recursive = false)
        {
            var lines = directoryInfoContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length < 7 || !string.Equals(parts[0], "D", StringComparison.OrdinalIgnoreCase) && !string.Equals(parts[0], "F", StringComparison.OrdinalIgnoreCase))
                    continue;

                var info = new LinkInfo(
                    recursive ? parts[6] : PathHelper.Combine(directoryFullName, parts[6]),
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

        private string GetRandomSuffix()
        {
            return _preventCache
                ? "?r=" + Guid.NewGuid().ToString("N")
                : null;
        }

        private WebClient CreateWebClient()
        {
            return new TimeoutWebClient(_timeout) { Encoding = _encoding };
        }
        private HttpClient CreateHttpClient()
        {
            return new HttpClient { Timeout = TimeSpan.FromMilliseconds(_timeout) };
        }


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
