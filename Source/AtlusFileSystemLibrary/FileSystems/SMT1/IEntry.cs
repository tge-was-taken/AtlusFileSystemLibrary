using System;
using System.IO;

namespace AtlusFileSystemLibrary.FileSystems.SMT1
{
    internal interface IEntry : IDisposable
    {
        int Handle { get; }

        int Length { get; }

        ContentKind ContentKind { get; }

        Stream GetStream();
    }
}