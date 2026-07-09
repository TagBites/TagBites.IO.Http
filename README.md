# TagBites.IO.Http

Read-only HTTP file system support for [TagBites.IO](https://github.com/TagBites/TagBites.IO). Browse and read files published over plain HTTP through the same `FileSystem` API used for local disk and other storages.

The server exposes a small index file (`.dirls` / `.dirrls`) per directory describing its contents; `TagBites.IO.Http` reads that index to list files without needing directory listing support from the web server itself.

## Install

```
dotnet add package TagBites.IO.Http
```

## Usage

```csharp
using TagBites.IO.Http;

var fs = HttpFileSystem.Create("https://example.com/files");

var file = fs.GetFile("/reports/summary.txt");
var content = file.ReadAllText();

foreach (var link in fs.GetDirectory("/reports").GetFiles())
    Console.WriteLine(link.FullName);
```

### Publishing the index files

`HttpFileSystem` itself is read-only - files are actually uploaded to the web server through some other, writable channel (FTP, SFTP, SMB, a CI/CD deploy step, ...), and it's on the write side that the `.dirls`/`.dirrls` index files need to be (re)generated so that `HttpFileSystem` can later list directory contents.

Use `HttpFileSystem.CreateDirectoryInfo` / `CreateRecursiveDirectoryInfo` after an upload, or wrap the writable file system with `HttpFileSystem.CreateBuilder` so the index is kept up to date automatically on every write:

```csharp
using TagBites.IO.Ftp;
using TagBites.IO.Http;

// The same server, reached over FTP for uploading...
var writeFs = HttpFileSystem.CreateBuilder(FtpFileSystem.Create("ftp.example.com", "user", "password"));
writeFs.GetFile("/reports/summary.txt").WriteAllText("...");
// the .dirls index for /reports is regenerated automatically

// ...and over HTTP for reading, from any client.
var readFs = HttpFileSystem.Create("https://example.com/files");
var content = readFs.GetFile("/reports/summary.txt").ReadAllText();
```

## License

See [https://www.tagbites.com/io](https://www.tagbites.com/io) for licensing terms.
