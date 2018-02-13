using System.IO;

namespace AtlusFileSystemLibrary.FileSystems.LB
{
    internal class MemoryEntry : Entry
    {
        private readonly Stream mStream;
        private readonly bool mOwnsStream;

        public MemoryEntry( int handle, Stream stream, bool ownsStream, byte type, short userId, string extension )
            : base( handle, type, false, userId, (int)stream.Length, extension, ( int )stream.Length )
        {
            mStream = stream;
            mOwnsStream = ownsStream;
        }

        public override Stream GetStream( bool decompress = true )
        {
            return mStream;
        }

        public override void Dispose()
        {
            if ( mOwnsStream )
                mStream?.Dispose();
        }
    }
}