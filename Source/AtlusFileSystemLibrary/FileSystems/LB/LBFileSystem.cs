using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AtlusFileSystemLibrary.Common.IO;

namespace AtlusFileSystemLibrary.FileSystems.LB
{
    public class LBFileSystem : IIndexedFileSystem
    {
        private int mNextHandle;
        private bool mOwnsStream;
        private readonly Dictionary<int, Entry> mEntryMap;
        private Stream mBaseStream;

        public bool IsReadOnly => true;

        public bool HasDirectories => false;

        public bool CanSave => true;

        public LBFileSystem()
        {
            mEntryMap = new Dictionary< int, Entry >();
        }

        public int AllocateHandle()
        {
            return mNextHandle++;
        }

        public void AddDirectory( int handle, ConflictPolicy policy )
        {
            throw new NotSupportedException( "This filesystem does not support directories" );
        }

        public void AddDirectory( int handle, string hostPath, ConflictPolicy policy )
        {
            throw new NotSupportedException( "This filesystem does not support directories" );
        }

        public void AddFile( string hostPath )
        {
            AddFile( AllocateHandle(), hostPath, ConflictPolicy.Ignore );
        }

        public void AddFile( Stream stream, bool ownsStream = true )
        {
            AddFile( AllocateHandle(), stream, ownsStream, ConflictPolicy.Ignore );
        }

        public void AddFile( int handle, string hostPath, ConflictPolicy policy )
        {
            AddFile( handle, File.OpenRead( hostPath ), true, policy );
        }

        public void AddFile( int handle, Stream stream, bool ownsStream, ConflictPolicy policy )
        {
            if (!(stream is FileStream fileStream))
                throw new NotSupportedException( "Can't add files without filename information to this file system" );

            if ( Exists( handle ) )
            {
                switch ( policy.Kind )
                {
                    case ConflictPolicy.PolicyKind.ThrowError:
                        throw new FileExistsException<int>( handle );
                    case ConflictPolicy.PolicyKind.Replace:
                        // Handled later
                        break;
                    case ConflictPolicy.PolicyKind.Ignore:
                        return;
                    default:
                        throw new ArgumentOutOfRangeException( nameof( policy ) );
                }
            }

            var entry = new MemoryEntry( handle, fileStream, true, 1, 0, Path.GetExtension( fileStream.Name )
                                                                                            .ToUpper()
                                                                                            .TrimStart( '.' ) );

            mEntryMap[entry.Handle] = entry;
        }

        public void Delete( int handle )
        {
            var entry = FindEntry( handle );
            mEntryMap.Remove( entry.Handle );
        }

        public IEnumerable< int > EnumerateDirectories( SearchOption option = SearchOption.TopDirectoryOnly )
        {
            throw new NotSupportedException( "This filesystem does not support directories" );
        }

        public IEnumerable< int > EnumerateDirectories( int handle, SearchOption option = SearchOption.TopDirectoryOnly )
        {
            throw new NotSupportedException( "This filesystem does not support directories" );
        }

        public IEnumerable< int > EnumerateFiles( SearchOption option = SearchOption.TopDirectoryOnly )
        {
            return EnumerateFileSystemEntries(option);
        }

        public IEnumerable< int > EnumerateFiles( int handle, SearchOption option = SearchOption.TopDirectoryOnly )
        {
            throw new NotSupportedException( "This filesystem does not support directories" );
        }

        public IEnumerable< int > EnumerateFileSystemEntries( SearchOption option = SearchOption.TopDirectoryOnly )
        {
            return mEntryMap.Keys;
        }

        public IEnumerable< int > EnumerateFileSystemEntries( int handle, SearchOption option = SearchOption.TopDirectoryOnly )
        {
            throw new NotSupportedException( "This filesystem does not support directories" );
        }

        public bool Exists( int handle )
        {
            return mEntryMap.ContainsKey( handle );
        }

        public EntryInfo GetInfo( int handle )
        {
            var entry = FindEntry( handle );
            return new EntryInfo( entry );
        }

        FileSystemEntryInfo<int> IFileSystem<int>.GetInfo( int handle )
        {
            return GetInfo( handle );
        }

        public bool IsDirectory( int handle )
        {
            throw new NotSupportedException( "This filesystem does not support directories" );
        }

        public bool IsFile( int handle )
        {
            return Exists( handle );
        }

        public void Load( string path )
        {
            Load( File.OpenRead( path ), true );
        }

        public void Load( Stream stream, bool ownsStream )
        {
            mBaseStream = stream;
            mOwnsStream = ownsStream;

            using ( var reader = new EndianBinaryReader( stream, Encoding.Default, true, Endianness.LittleEndian ) )
            {
                while ( reader.Position < reader.BaseStreamLength )
                {
                    var type = reader.ReadByte();
                    if ( type == 0xFF ) // end
                        break;

                    var isCompressed = reader.ReadBoolean();
                    var userId = reader.ReadInt16();
                    var length = reader.ReadInt32() - 16;
                    var extension = reader.ReadString( StringBinaryFormat.FixedLength, 4 );
                    var decompressedLength = reader.ReadInt32();
                    var offset = ( int )reader.Position;

                    var entry = new StoredEntry( AllocateHandle(), mBaseStream, offset, type, isCompressed, userId, length, extension, decompressedLength );
                    mEntryMap[entry.Handle] = entry;

                    reader.SeekCurrent( length );
                    reader.Position = AlignmentUtils.Align( reader.Position, 64 );
                }
            }
        }

        public Stream OpenFile( int handle )
        {
            var entry = FindEntry( handle );
            return entry.GetStream();
        }

        public void Save( string outPath )
        {
            using ( var stream = FileUtils.Create( outPath ) )
                Save( stream );
        }

        public Stream Save()
        {
            var stream = new MemoryStream();
            Save( stream );
            return stream;
        }

        public void Save( Stream stream )
        {
            using ( var writer = new EndianBinaryWriter( stream, Encoding.Default, true, Endianness.LittleEndian ) )
            {
                int alignmentBytes;

                foreach ( var entry in mEntryMap.Values.OrderBy(x => x.Handle) )
                {
                    writer.Write( entry.Type );
                    writer.Write( entry.IsCompressed );
                    writer.Write( entry.UserId );
                    writer.Write( entry.Length + 16 );
                    writer.Write( entry.Extension, StringBinaryFormat.FixedLength, 4 );
                    writer.Write( entry.DecompressedLength );
                    entry.GetStream( false ).CopyTo( writer.BaseStream );

                    alignmentBytes = AlignmentUtils.GetAlignedDifference( writer.BaseStreamLength, 64 );
                    for ( int i = 0; i < alignmentBytes; i++ )
                        writer.Write( ( byte ) 0 );
                }

                // Write 'end' entry
                writer.Write( ( byte ) 0xFF );
                writer.Write( ( byte ) 0 );
                writer.Write( ( short ) 0 );
                writer.Write( ( int ) 16 );
                writer.Write( "END0", StringBinaryFormat.FixedLength, 4 );
                writer.Write( ( int ) 0 );

                alignmentBytes = AlignmentUtils.GetAlignedDifference( writer.BaseStreamLength, 64 );
                for ( int i = 0; i < alignmentBytes; i++ )
                    writer.Write( ( byte )0 );
            }
        }

        public void Dispose()
        {
            foreach ( var entry in mEntryMap.Values )
                entry.Dispose();

            if ( mOwnsStream )
                mBaseStream?.Dispose();
        }

        private Entry FindEntry( int handle )
        {
            if ( !mEntryMap.TryGetValue( handle, out var entry ) )
                throw new FileNotFoundException< int >( handle );

            return entry;
        }
    }
}
