using AtlusFileSystemLibrary.Common.IO;

namespace AtlusFileSystemLibrary
{
    internal class FileSystemUtils
    {
        public static void Save<T>( IFileSystem<T> fs, string path )
        {
            using ( var stream = FileUtils.Create( path, fs.FilePath ) )
            {
                fs.Save( stream );
                fs.Dispose();
            }

            fs.Load( path );
        }
    }
}