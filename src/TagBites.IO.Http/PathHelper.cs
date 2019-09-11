using System;
using System.IO;

namespace TagBites.IO.Http
{
    internal static class PathHelper
    {
        public static string Combine(string path1, string path2, string path3)
        {
            return Combine(Combine(path1, path2), path3);
        }
        public static string Combine(string path1, string path2)
        {
            if (string.IsNullOrEmpty(path2))
                return path1;

            if (string.IsNullOrEmpty(path1))
                return path2;

            var ch = path1[path1.Length - 1];

            if (ch != Path.DirectorySeparatorChar && ch != Path.AltDirectorySeparatorChar && ch != Path.VolumeSeparatorChar)
                return path1 + "/" + path2;

            return path1 + path2;
        }

        public static string GetDirectoryName(string path)
        {
            if (path == null)
                return null;

            var index = GetLastDirectorySeparator(path);
            if (index == -1)
                return String.Empty;

            return index == 0 && path.Length > 1
                ? path[0].ToString()
                : path.Substring(0, index);
        }
        private static int GetLastDirectorySeparator(string path)
        {
            if (path != null)
            {
                var length = path.Length;
                var index = length;

                while (--index >= 0)
                {
                    var ch = path[index];
                    if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar || ch == Path.VolumeSeparatorChar)
                        return index;
                }
            }

            return -1;
        }
    }
}
