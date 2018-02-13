using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AtlusFileSystemLibrary.Common.IO;

namespace AtlusFileSystemLibrary.Compressions
{
    public class LBCompression : ICompression
    {
        public bool CanCompress => false;

        public bool CanDecompress => true;

        public Stream Compress( Stream input )
        {
            throw new NotImplementedException();
        }

        public Stream Decompress( Stream input, int decompressedSize = -1 )
        {
            // Based off of firefly's 'unlb'
            var decompressed = new List<byte>( decompressedSize );

            using ( var reader = new EndianBinaryReader( input, Encoding.Default, true, Endianness.LittleEndian ) )
            {
                while ( decompressed.Count < decompressedSize )
                {
                    var op = reader.ReadByte();

                    var count = op & 0x1F;
                    if ( count == 0 )
                        count = reader.ReadUInt16();

                    var opcode = ( Opcode )( ( op >> 4 ) & 0xE );
                    switch ( opcode )
                    {
                        case Opcode.CopyBytes:
                            for ( int i = 0; i < count; i++ )
                                decompressed.Add( reader.ReadByte() );
                            break;
                        case Opcode.RepeatZero:
                            for ( int i = 0; i < count; i++ )
                                decompressed.Add( 0 );
                            break;
                        case Opcode.RepeatByte:
                            {
                                var b = reader.ReadByte();
                                for ( int i = 0; i < count; i++ )
                                    decompressed.Add( b );
                            }
                            break;
                        case Opcode.CopyBytesFromOffset:
                            {
                                var offset = reader.ReadByte();
                                for ( int i = 0; i < count; i++ )
                                    decompressed.Add( decompressed[( decompressed.Count - 1 ) + -offset + 1] );
                            }
                            break;
                        case Opcode.CopyBytesFromLargeOffset:
                            {
                                var offset = reader.ReadUInt16();
                                for ( int i = 0; i < count; i++ )
                                    decompressed.Add( decompressed[( decompressed.Count - 1 ) + -offset + 1] );
                            }
                            break;
                        case Opcode.CopyBytesZeroInterleaved:
                            for ( int i = 0; i < count; i++ )
                            {
                                decompressed.Add( reader.ReadByte() );
                                decompressed.Add( 0 );
                            }
                            break;

                        default:
                            throw new InvalidDataException();
                    }
                }
            }

            return new MemoryStream( decompressed.ToArray() );
        }

        private enum Opcode
        {
            CopyBytes = 0x00,
            RepeatZero = 0x02,
            RepeatByte = 0x04,
            CopyBytesFromOffset = 0x06,
            CopyBytesFromLargeOffset = 0x08,
            CopyBytesZeroInterleaved = 0x0A,
        }
    }
}
