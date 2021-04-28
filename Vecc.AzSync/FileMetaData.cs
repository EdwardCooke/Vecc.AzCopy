using System;

namespace Vecc.AzSync
{
    public class FileMetaData
    {
        public string FullFilePath { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
