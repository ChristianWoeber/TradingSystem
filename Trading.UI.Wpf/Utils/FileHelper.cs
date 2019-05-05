using System.IO;
using System.IO.Compression;

namespace Trading.UI.Wpf.Utils
{
    public static class FileHelper
    {

        public static void CreateZipFile(string path)
        {
            //using (var fileStream = new FileStream(@"C:\temp\temp.zip", FileMode.CreateNew))
            //{
            //    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
            //    {
            //        archive.CreateEntryFromFile(path, Path.GetFileName(path));
            //    }
            //}
        }
    }
}
