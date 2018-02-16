using System.IO;
using System.Linq;

namespace AtlusFileSystemLibrary.FileSystems.SMT1
{
    internal static class ContentKindDetector
    {
        private static readonly byte[] sTimSignature = { 0x10, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00 };
        private static readonly byte[] sTIMHSignature = { 0x08, 0x00, 0x00, 0x00, 0x80, 0x13, 0x00, 0x00 };
        private static readonly byte[] sSc02Signature = { (byte)'S', (byte)'C', (byte)'0', (byte)'2', 0x18, 0x00, 0x00, 0x00 };

        private static readonly ContentSignature[] sSignatures =
        {
            new ContentSignature( ContentKind.TIM, sTimSignature ),
            new ContentSignature( ContentKind.TIMH, sTIMHSignature ),
            new ContentSignature( ContentKind.SC02, sSc02Signature )
        };

        public static ContentKind Detect( Stream stream )
        {
            var kind = ContentKind.Unknown;

            var bytes = new byte[8];
            stream.Read( bytes, 0, 8 );

            foreach ( var signature in sSignatures )
            {
                if ( bytes.SequenceEqual( signature.Signature ) )
                {
                    kind = signature.Kind;
                    break;
                }
            }

            stream.Position = 0;
            return kind;
        }

        private struct ContentSignature
        {
            public ContentKind Kind { get; }
            public byte[] Signature { get; }

            public ContentSignature( ContentKind kind, byte[] sig )
            {
                Kind = kind;
                Signature = sig;
            }
        }
    }
}