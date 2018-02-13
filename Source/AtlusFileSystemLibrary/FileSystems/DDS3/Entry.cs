using System;
using System.Text;

namespace AtlusFileSystemLibrary.FileSystems.DDS3
{
    internal abstract class Entry : IDisposable
    {
        public DirectoryEntry Parent { get; }

        public FileSystemEntryKind Kind { get; }

        public string Name { get; }

        public uint Offset { get; }

        public string FullName
        {
            get
            {
                var builder = new StringBuilder( 64 );

                var parent = Parent;
                while ( parent != null )
                {
                    builder.Insert( 0, parent.Name + "/" );
                    parent = parent.Parent;
                }

                builder.Append( Name );
                return builder.ToString();
            }
        }

        protected Entry( DirectoryEntry parent, string name, uint offset, FileSystemEntryKind kind)
        {
            Parent = parent;
            Kind = kind;
            Name = name;
            Offset = offset;
        }

        public abstract void Dispose();
    }
}
