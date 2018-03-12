using System;
using System.IO;

namespace SS.CloudStorage.Core
{
    public class Utils
    {
        public static string GetRelativePath(string filePath, string directoryPath)
        {
            var pathUri = new Uri(filePath);
            // Folders must end in a slash
            if (!directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                directoryPath += Path.DirectorySeparatorChar;
            }
            var folderUri = new Uri(directoryPath);
            return
                Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri)
                    .ToString()
                    .Replace(Path.DirectorySeparatorChar, '/'));
        }
    }
}
