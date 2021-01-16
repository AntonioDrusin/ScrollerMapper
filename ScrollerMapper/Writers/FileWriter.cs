using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ScrollerMapper.StreamExtensions;

namespace ScrollerMapper
{
    internal class FileWriter : IWriter, IDisposable
    {
        private readonly Options _options;
        private readonly Lazy<IndentedTextWriter> _chipWriter;
        private readonly Lazy<IndentedTextWriter> _constantWriter;
        private BinaryWriter _currentWriter;

        private BinaryWriter _diskChipWriter;
        private BinaryWriter _diskFastWriter;
        private ObjectType _currentObject;

        private IndentedTextWriter ChipWriter => _chipWriter.Value;
        private IndentedTextWriter NormalWriter => _constantWriter.Value;

        private readonly List<string> _offsetDumps = new List<string>();

        public FileWriter(Options options)
        {
            _options = options;

            _chipWriter = CreateCodeFile("_data");
            _constantWriter = CreateCodeFile("");
        }

        private Lazy<IndentedTextWriter> CreateCodeFile(string postFix)
        {
            var fileName = GetFileNameFor(ObjectType.Assembly,
                Path.GetFileNameWithoutExtension(_options.InputFile) + postFix);
            return new Lazy<IndentedTextWriter>(() =>
                new IndentedTextWriter(new StreamWriter(File.Create(fileName)), "\t"));
        }

        public void StartDiskFile(string diskFileName)
        {
            _diskFileName = diskFileName;
            _chipFileName = GetFileNameFor(ObjectType.Chip, diskFileName);
            _fastFileName = GetFileNameFor(ObjectType.Fast, diskFileName);
            _relocFileName = GetFileNameFor(ObjectType.Relocations, diskFileName);

            _diskChipWriter = new BinaryWriter(File.Create(_chipFileName));
            _diskFastWriter = new BinaryWriter(File.Create(_fastFileName));
        }

        public void CompleteDiskFile()
        {
            if (_relocations.Any())
            {
                WriteRelocations();
                _relocations.Clear();
            }

            if (_diskChipWriter != null)
            {
                FlushAndClose(_diskChipWriter);
                _diskChipWriter = null;
            }

            if (_diskFastWriter != null)
            {
                FlushAndClose(_diskFastWriter);
                _diskFastWriter = null;
            }

            _offsets.Clear();

            // Merge the file so it can be loaded at once.
            using (var mergedFile = new BinaryWriter(File.Create(GetFileNameFor(ObjectType.Final, _diskFileName))))
            {
                var chipData = File.ReadAllBytes(_chipFileName);
                mergedFile.Write(Encoding.ASCII.GetBytes("CHIP"));
                mergedFile.Write(Endian.ConvertLong((uint) chipData.Length));
                mergedFile.Write(chipData);
                var fastData = File.ReadAllBytes(_fastFileName);
                mergedFile.Write(Encoding.ASCII.GetBytes("FAST"));
                mergedFile.Write(Endian.ConvertLong((uint)fastData.Length));
                mergedFile.Write(fastData);
                var relocationData = File.ReadAllBytes(_relocFileName);
                mergedFile.Write(Encoding.ASCII.GetBytes("RELO"));
                mergedFile.Write(Endian.ConvertLong((uint)relocationData.Length));
                mergedFile.Write(relocationData);
                mergedFile.Close();
            }
        }

        private void FlushAndClose(BinaryWriter writer)
        {
            writer.Flush();
            writer.Close();
            writer.Dispose();
        }

        private readonly Dictionary<string, uint> _offsets = new Dictionary<string, uint>();
        private bool _seek;
        private string _chipFileName;
        private string _fastFileName;

        public void StartObject(ObjectType type, string name)
        {
            _currentObject = type;
            switch (type)
            {
                case ObjectType.Chip:
                    _currentWriter = _diskChipWriter;
                    _offsets.Add(name, GetCurrentOffset(type));
                    _offsetDumps.Add($"{_chipFileName} {name} {GetCurrentOffset(type)}");
                    break;
                case ObjectType.Fast:
                    _currentWriter = _diskFastWriter;
                    _offsets.Add(name, GetCurrentOffset(type));
                    _offsetDumps.Add($"{_fastFileName} {name} {GetCurrentOffset(type)}");
                    break;
                default:
                    var fileName = GetFileNameFor(type, name);
                    DataSection(type);
                    ChipWriter.WriteLine($"{name}{GetLabelPostfix(type)}:");
                    ChipWriter.Indent++;
                    ChipWriter.WriteLine($"incbin {Path.GetFileName(fileName)}");

                    _currentWriter = new BinaryWriter(File.Create(fileName));
                    break;
            }
        }

        public void RestartObject(ObjectType type, string name)
        {
            _seek = true;
            _currentObject = type;
            switch (type)
            {
                case ObjectType.Chip:
                    _currentWriter = _diskChipWriter;
                    break;
                case ObjectType.Fast:
                    _currentWriter = _diskFastWriter;

                    break;
                default:
                    throw new NotSupportedException("Only chip and fast are supported with seek");
            }

            var offset = _offsets[name];
            _currentWriter.Seek((int) offset, SeekOrigin.Begin);
        }

        public uint GetOffset(string name)
        {
            return _offsets[name];
        }

        private void DataSection(ObjectType type)
        {
            bool isChip = false;

            switch (type)
            {
                case ObjectType.Palette:
                case ObjectType.TileInfo:
                case ObjectType.Layer:
                case ObjectType.Assembly:
                case ObjectType.Data:
                case ObjectType.SpriteFast:
                    break;
                case ObjectType.Bitmap:
                case ObjectType.Tile:
                case ObjectType.Bob:
                case ObjectType.Sprite:
                case ObjectType.Audio:
                    isChip = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (isChip)
            {
                ChipWriter.WriteLine("\tsection\t\tdatachip,data_c");
            }
            else
            {
                ChipWriter.WriteLine("\tsection\t\tdata");
            }
        }

        public void EndObject()
        {
            switch (_currentObject)
            {
                case ObjectType.Chip:
                case ObjectType.Fast:
                    if (!_seek)
                    {
                        var pos = _currentWriter.BaseStream.Position;
                        if (pos % 2 > 0)
                        {
                            _currentWriter.Write((byte) 0);
                        }
                    }

                    break;
                default:
                    ChipWriter.WriteLine("even");
                    ChipWriter.Indent--;
                    ChipWriter.WriteLine();
                    _currentWriter?.Dispose();
                    ChipWriter.Flush();
                    NormalWriter.Flush();
                    break;
            }

            if (_seek)
            {
                _currentWriter?.Seek(0, SeekOrigin.End);
                _seek = false;
            }

            _currentWriter = null;
        }

        public void WriteByte(byte data)
        {
            _currentWriter.Write(data);
        }

        private readonly List<Relocation> _relocations = new List<Relocation>();
        private string _relocFileName;
        private string _diskFileName;

        public void WriteOffset(ObjectType destinationObjectType, uint destinationOffset)
        {
            if ((destinationOffset & 0x01) != 0)
            {
                throw new ConversionException("Offset for relocation is not even.");
            }

            var sourceOffset = (uint)_currentWriter.BaseStream.Position;

            _relocations.Add(new Relocation
            {
                DestinationType = destinationObjectType,
                SourceOffset = sourceOffset,
                SourceType = _currentObject
            });
            WriteLong(destinationOffset);
        }

        public void WriteRelocations()
        {
            var relocFile = new BinaryWriter(File.Create(_relocFileName));
            WriteRelocationList(relocFile, _relocations.Where(_ => _.SourceType == ObjectType.Chip).ToList());
            WriteRelocationList(relocFile, _relocations.Where(_ => _.SourceType == ObjectType.Fast).ToList());
            if (relocFile.BaseStream.Position > 5632)
            {
                throw new ConversionException("relocations longer than 1 track 5632 bytes");
            }

            relocFile.Close();
        }

        private void WriteRelocationList(BinaryWriter relocFile, List<Relocation> relocations)
        {
            relocFile.Write(Endian.ConvertWord((ushort) relocations.Count));

            foreach (var relocation in relocations)
            {
                uint pointer = (uint) (relocation.DestinationType == ObjectType.Fast ? 1 : 0);
                pointer = pointer | relocation.SourceOffset;

                relocFile.Write(Endian.ConvertLong(pointer));
            }
        }

        public uint GetCurrentOffset(ObjectType objectType)
        {
            switch (objectType)
            {
                case ObjectType.Chip:
                    return (uint) _diskChipWriter.BaseStream.Position;
                case ObjectType.Fast:
                    return (uint) _diskFastWriter.BaseStream.Position;
                default:
                    return (uint) _currentWriter.BaseStream.Position;
            }
        }

        public void WriteWord(ushort data)
        {
            _currentWriter.Write(Endian.ConvertWord(data));
        }

        public void WriteLong(uint data)
        {
            _currentWriter.Write(Endian.ConvertLong(data));
        }

        public void WriteBlob(byte[] data)
        {
            _currentWriter.Write(data);
        }

        public void WriteBlob(byte[] data, int count)
        {
            _currentWriter.Write(data, 0, count);
        }

        public void WriteCode(Code codeType, string code)
        {
            switch (codeType)
            {
                case Code.Data:
                    ChipWriter.WriteLine(code);
                    break;
                case Code.Normal:
                    NormalWriter.WriteLine(code);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(codeType), codeType, null);
            }
        }

        public void Dispose()
        {
            foreach (var line in _offsetDumps)
            {
                Console.WriteLine(line);
            }


            if (_chipWriter.IsValueCreated)
            {
                _chipWriter.Value.Close();
                _chipWriter.Value.Dispose();
            }

            if (_constantWriter.IsValueCreated)
            {
                _constantWriter.Value.Close();
                _constantWriter.Value.Dispose();
            }

            _diskChipWriter?.Dispose();
            _diskFastWriter?.Dispose();
        }

        private string GetFileNameFor(ObjectType type, string name)
        {
            string extension;
            string folder = "";
            switch (type)
            {
                case ObjectType.Assembly:
                    extension = "s";
                    break;
                case ObjectType.Palette:
                    extension = "PAL";
                    break;
                case ObjectType.TileInfo:
                    extension = "INFO";
                    break;
                case ObjectType.Bitmap:
                    extension = "BMP";
                    break;
                case ObjectType.Layer:
                    extension = "LAYER";
                    break;
                case ObjectType.Tile:
                    extension = "TILE";
                    break;
                case ObjectType.Bob:
                    extension = "BOB";
                    break;
                case ObjectType.Sprite:
                case ObjectType.SpriteFast:
                    extension = "SPRITE";
                    break;
                case ObjectType.Audio:
                    extension = "AUD";
                    break;
                case ObjectType.Data:
                    extension = "DAT";
                    break;
                case ObjectType.Chip:
                    extension = "chip";
                    break;
                case ObjectType.Fast:
                    extension = "fast";
                    break;
                case ObjectType.Relocations:
                    extension = "rel";
                    break;
                case ObjectType.Final:
                    extension = "data";
                    folder = "disk";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (name == null) name = Path.GetFileNameWithoutExtension(_options.InputFile);
            return Path.Combine(Path.Combine(_options.OutputFolder, folder), $"{name}.{extension}");
        }

        private string GetLabelPostfix(ObjectType type)
        {
            switch (type)
            {
                case ObjectType.SpriteFast:
                    return "Sprite";
            }

            return type.ToString();
        }
    }

    internal class Relocation
    {
        public ObjectType DestinationType { get; set; }
        public uint SourceOffset { get; set; }
        public ObjectType SourceType { get; set; }
    }
}