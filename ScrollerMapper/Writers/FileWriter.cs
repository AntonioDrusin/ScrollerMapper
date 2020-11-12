using System;
using System.CodeDom.Compiler;
using System.IO;
using ScrollerMapper.StreamExtensions;

namespace ScrollerMapper
{
    internal class FileWriter : IWriter, IDisposable
    {
        private readonly Options _options;
        private readonly Lazy<IndentedTextWriter> _chipWriter;
        private readonly Lazy<IndentedTextWriter> _constantWriter;
        private BinaryWriter _currentWriter;

        private BinaryWriter _diskChipWriter = null;
        private BinaryWriter _diskFastWriter = null;
        private ObjectType _currentObject;

        private IndentedTextWriter ChipWriter => _chipWriter.Value;
        private IndentedTextWriter NormalWriter => _constantWriter.Value;

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
            var chipFileName = GetFileNameFor(ObjectType.Chip, diskFileName);
            var fastFileName = GetFileNameFor(ObjectType.Fast, diskFileName);

            _diskChipWriter = new BinaryWriter(File.Create(chipFileName));
            _diskFastWriter = new BinaryWriter(File.Create(fastFileName));
        }

        public void CompleteDiskFile()
        {
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
        }

        private void FlushAndClose(BinaryWriter writer)
        {
            writer.Flush();
            writer.Close();
            writer.Dispose();
        }

        public void StartObject(ObjectType type, string name)
        {
            _currentObject = type;
            switch (type)
            {
                case ObjectType.Chip:
                    NormalWriter.WriteLine($"{name}{GetLabelPostfix(type)}\tequ\t{_diskChipWriter.BaseStream.Position}");
                    _currentWriter = _diskChipWriter;
                    break;
                case ObjectType.Fast:
                    NormalWriter.WriteLine($"{name}{GetLabelPostfix(type)}\tequ\t{_diskFastWriter.BaseStream.Position}");
                    _currentWriter = _diskFastWriter;
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
                    break;
                case ObjectType.Fast:
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
            _currentWriter = null;

        }

        public void WriteByte(byte data)
        {
            _currentWriter.Write(data);
        }

        public long GetCurrentOffset()
        {
            return _currentWriter.BaseStream.Position;
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


            _currentWriter?.Dispose();
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
                    folder = "disk";
                    break;
                case ObjectType.Fast:
                    extension = "fast";
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
            return type.ToString();
        }
    }
}