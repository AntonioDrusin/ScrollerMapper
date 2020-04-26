using System.IO;
using Newtonsoft.Json;

namespace ScrollerMapper
{
    internal static class FileExtensions {

        public static T ReadJsonFile<T>(this string fileName)
        {
            using (TextReader file = File.OpenText(fileName))
            {
                var reader = new JsonTextReader(file);
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader);
            }
        }

        public static string FromFolderOf(this string fileName, string otherFileName)
        {
            var directoryName = Path.GetDirectoryName(Path.GetFullPath(otherFileName));

            return Path.Combine(directoryName, Path.GetFileName(fileName));
        }
    }
}