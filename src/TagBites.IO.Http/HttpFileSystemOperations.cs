using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using TagBites.IO.Operations;

namespace TagBites.IO.Http
{
    internal class HttpFileSystemOperations : IFileSystemReadOperations
    {
        internal const string DefaultDirectoryInfoFileName = ".dirls";

        private readonly string _address;
        private readonly string _directoryInfoFileName;

        private WebClient Client { get; }

        public HttpFileSystemOperations(string address, string directoryInfoFileName = null, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));

            Client = new TimeoutWebClient(5000);
            Client.Encoding = encoding ?? Encoding.UTF8;
            _address = address;
            _directoryInfoFileName = directoryInfoFileName ?? DefaultDirectoryInfoFileName;
        }


        public IFileSystemStructureLinkInfo GetLinkInfo(string fullName)
        {
            var parent = PathHelper.GetDirectoryName(fullName);
            if (string.IsNullOrEmpty(parent))
                return new LinkInfo(fullName) { IsDirectory = true };

            lock (Client)
            {
                string text;
                try
                {
                    text = Client.DownloadString(PathHelper.Combine(_address, parent, _directoryInfoFileName));
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
        public string CorrectPath(string path) => path;

        public Stream ReadFile(FileLink file)
        {
            lock (Client)
                return Client.OpenRead(PathHelper.Combine(_address, file.FullName));
        }
        public IList<IFileSystemStructureLinkInfo> GetLinks(DirectoryLink directory, FileSystem.ListingOptions options)
        {
            lock (Client)
            {
                string text;
                try
                {
                    text = Client.DownloadString(PathHelper.Combine(_address, directory.FullName, _directoryInfoFileName));
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

        private IEnumerable<IFileSystemStructureLinkInfo> ParseDirectoryInfo(string directoryFullName, string directoryInfoContent)
        {
            var lines = directoryInfoContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length < 7 || !string.Equals(parts[0], "D", StringComparison.OrdinalIgnoreCase) && !string.Equals(parts[0], "F", StringComparison.OrdinalIgnoreCase))
                    continue;

                var info = new LinkInfo(PathHelper.Combine(directoryFullName, parts[6]));
                info.IsDirectory = string.Equals(parts[0], "D", StringComparison.OrdinalIgnoreCase);

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

        private class LinkInfo : IFileLinkInfo
        {
            public string FullName { get; }
            public bool Exists => true;
            public bool IsDirectory { get; set; }

            public DateTime? CreationTime { get; set; }
            public DateTime? LastWriteTime { get; set; }
            public bool IsHidden => false;
            public bool IsReadOnly => false;

            public string ContentPath => FullName;
            public long Length { get; set; }
            public FileHash Hash { get; set; }

            public bool CanRead => true;
            public bool CanWrite => false;

            public LinkInfo(string fullName)
            {
                FullName = fullName;
            }
        }
    }
}
