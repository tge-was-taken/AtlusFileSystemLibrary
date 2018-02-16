using System;
using System.Collections.Generic;
using System.IO;
using AtlusFileSystemLibrary;
using AtlusFileSystemLibrary.Common.IO;
using AtlusFileSystemLibrary.FileSystems.PAK;

namespace PAKPack
{
    internal static class Program
    {
        public static IReadOnlyDictionary< string, ICommand > Commands { get; } = new Dictionary<string, ICommand>
        {
            { "pack", new PackCommand() },
            { "unpack", new UnpackCommand() },
            { "replace", new ReplaceCommand() }
        };

        public static IReadOnlyDictionary< string, FormatVersion > FormatsByName { get; } = new Dictionary< string, FormatVersion >
        {
            { "v1", FormatVersion.Version1 },
            { "v2", FormatVersion.Version2 },
            { "v3", FormatVersion.Version3 }
        };

        public static IReadOnlyDictionary<FormatVersion, string> NameToFormat { get; } = new Dictionary<FormatVersion, string>
        {
            { FormatVersion.Version1, "v1"},
            { FormatVersion.Version2, "v2" },
            { FormatVersion.Version3, "v3" }
        };

        private static void Main( string[] args )
        {
            if ( args.Length == 0 )
            {
                Console.WriteLine( "PAKPack 1.0 - A PAK pack/unpacker made by TGE (2018)\n" +
                                   "\n" +
                                   "Usage:\n" +
                                   "  PAKPack <command>\n" +
                                   "\n" +
                                   "Commands:\n" +
                                   "\n" +
                                   "    pack        Packs the given input into a PAK file and outputs it to the specified output path.\n" +
                                   "        Usage:\n" +
                                   "            pack <input directory path> <format> [output file path]\n" +
                                   "\n" +
                                   "    unpack      Unpacks the given input PAK file and outputs it to the specified output directory.\n" +
                                   "        Usage:\n" +
                                   "            unpack <input file path> [output directory path]\n" +
                                   "\n" +
                                   "    replace     Replaces the specified file(s) with the contents of the specified input\n" +
                                   "        Usage:\n" +
                                   "            replace <input pak file path> <file name to replace> <file path> [output file path]\n" +
                                   "            replace <input pak file path> <path to file directory> [output file path]\n" +
                                   "\n" );
                return;
            }

            if ( !Commands.TryGetValue( args[ 0 ], out var command ) )
            {
                Console.WriteLine( "Invalid command specified." );
                return;
            }

            if ( command.Execute( args ) )
            {
                Console.WriteLine( "Command executed successfully." );
            }
            else
            {
                Console.WriteLine( "Command failed." );
            }
        }
    }

    internal interface ICommand
    {
        bool Execute( string[] args );
    }

    internal class PackCommand : ICommand
    {
        public bool Execute( string[] args )
        {
            if ( args.Length < 3 )
            {
                Console.WriteLine( "Expected at least 2 arguments" );
                return false;
            }

            var inputPath = args[ 1 ];
            if ( !Directory.Exists( inputPath ) )
            {
                Console.WriteLine( "Input directory doesn't exist" );
                return false;
            }

            var formatName = args[ 2 ];
            if ( !Program.FormatsByName.TryGetValue( formatName, out var format ) )
            {
                Console.WriteLine( "Invalid format specified." );
                return false;
            }

            var outputPath = Path.ChangeExtension( inputPath, "pak" );
            if ( args.Length > 3 )
                outputPath = args[ 3 ];

            using ( var pak = new PAKFileSystem( format ) )
            {
                foreach ( string file in Directory.EnumerateFiles( inputPath, "*.*", SearchOption.AllDirectories ) )
                {
                    Console.WriteLine( $"Adding {file}" );

                    pak.AddFile( file.Substring( inputPath.Length )
                                     .Trim( Path.DirectorySeparatorChar )
                                     .Replace( "\\", "/" ),
                                 file, ConflictPolicy.Ignore );
                }

                Console.WriteLine( $"Saving..." );
                pak.Save( outputPath );
            }

            return true;
        }
    }

    internal class UnpackCommand : ICommand
    {
        public bool Execute( string[] args )
        {
            if ( args.Length < 2 )
            {
                Console.WriteLine( "Expected at least 1 argument." );
                return false;
            }

            var inputPath = args[1];
            if ( !File.Exists( inputPath ) )
            {
                Console.WriteLine( "Input file doesn't exist." );
                return false;
            }

            var outputPath = Path.ChangeExtension( inputPath, null );
            if ( args.Length > 2 )
                outputPath = args[2];

            Directory.CreateDirectory( outputPath );

            if ( !PAKFileSystem.TryOpen( inputPath, out var pak ) )
            {
                Console.WriteLine( "Invalid PAK file." );
                return false;
            }

            using ( pak )
            {
                Console.WriteLine( $"PAK format version: {Program.NameToFormat[pak.Version]}" );

                foreach ( string file in pak.EnumerateFiles() )
                {
                    using ( var stream = FileUtils.Create( outputPath + Path.DirectorySeparatorChar + file ) )
                    using ( var inputStream = pak.OpenFile( file ) )
                    {
                        Console.WriteLine( $"Extracting {file}" );
                        inputStream.CopyTo( stream );
                    }
                }
            }

            return true;
        }
    }

    internal class ReplaceCommand : ICommand
    {
        public bool Execute( string[] args )
        {
            if ( args.Length < 3 )
            {
                Console.WriteLine( "Expected at least 2 arguments." );
                return false;
            }

            var inputPath = args[1];
            if ( !File.Exists( inputPath ) )
            {
                Console.WriteLine( "Input file doesn't exist." );
                return false;
            }

            if ( !PAKFileSystem.TryOpen( inputPath, out var pak ) )
            {
                Console.WriteLine( "Invalid PAK file." );
                return false;
            }

            string outputPath = Path.GetRandomFileName();
            bool replaceInput = true;

            if ( Directory.Exists( args[ 2 ] ) )
            {
                var directoryPath = args[ 2 ];

                if ( args.Length > 3 )
                {
                    outputPath = args[3];
                    replaceInput = false;
                }

                using ( pak )
                {
                    foreach ( string file in Directory.EnumerateFiles( directoryPath, "*.*", SearchOption.AllDirectories ) )
                    {
                        Console.WriteLine( $"Adding/Replacing {file}" );
                        pak.AddFile( file.Substring( directoryPath.Length )
                                         .Trim( Path.DirectorySeparatorChar )
                                         .Replace( "\\", "/" ),
                                     file, ConflictPolicy.Replace );
                    }

                    Console.WriteLine( "Saving..." );
                    pak.Save( outputPath );
                }
            }
            else
            {
                if ( args.Length > 4 )
                {
                    outputPath = args[4];
                    replaceInput = false;
                }

                using ( pak )
                {
                    var entryName = args[2];

                    if ( !pak.Exists( entryName ) )
                    {
                        Console.WriteLine( "Specified entry doesn't exist." );
                        return false;
                    }

                    var filePath = args[3];
                    if ( !File.Exists( filePath ) )
                    {
                        Console.WriteLine( "Specified replacement file doesn't exist." );
                        return false;
                    }

                    Console.WriteLine( $"Adding/Replacing {entryName}" );
                    pak.AddFile( entryName, filePath, ConflictPolicy.Replace );

                    Console.WriteLine( "Saving..." );
                    pak.Save( outputPath );
                }
            }

            if ( replaceInput )
            {
                File.Copy( outputPath, inputPath, true );
                File.Delete( outputPath );
            }

            return true;
        }
    }
}
