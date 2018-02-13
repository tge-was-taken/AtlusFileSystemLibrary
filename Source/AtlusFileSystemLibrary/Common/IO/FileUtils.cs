using System.IO;

namespace AtlusFileSystemLibrary.Common.IO
{
    public static class FileUtils
    {
        public static FileStream Create( string path )
        {
            var directory = Path.GetDirectoryName( path );
            if ( !string.IsNullOrWhiteSpace(directory) )
                Directory.CreateDirectory( directory );

            return File.Create( path );
        }
    }
}
