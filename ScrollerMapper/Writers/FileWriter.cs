using System;
using System.CodeDom.Compiler;
using System.IO;
using ScrollerMapper.StreamExtensions;

namespace ScrollerMapper
{
    internal class FileWriter : IWriter, IDisposable
    {
        private readonly BaseOptions _options;
        private readonly Lazy<IndentedTextWriter> _chipWriter;
        private readonly Lazy<IndentedTextWriter> _constantWriter;
        private BinaryWriter _currentWriter;

        private IndentedTextWriter ChipWriter => _chipWriter.Value;
        private IndentedTextWriter ConstantsWriter => _chipWriter.Value;

        public FileWriter(BaseOptions options)
        {
            _options = options;

            _chipWriter = CreateCodeFile("_chip");
            _constantWriter = CreateCodeFile("_def");
        }

        private Lazy<IndentedTextWriter> CreateCodeFile(string postFix)
        {
            var fileName = GetFileNameFor(ObjectType.Assembly, _options.OutputName + postFix);
            return new Lazy<IndentedTextWriter>(() =>
                new IndentedTextWriter(new StreamWriter(File.Create(fileName)), "\t"));
        }

        public void StartObject(ObjectType type, string name)
        {
            var fileName = GetFileNameFor(type, name);
            ChipWriter.WriteLine($"{name}{GetLabelPostfix(type)}:");
            ChipWriter.Indent++;
            ChipWriter.WriteLine($"incbin {Path.GetFileName(fileName)}");

            _currentWriter = new BinaryWriter(File.Create(fileName));
        }

        public void CompleteObject()
        {
            ChipWriter.WriteLine("even");
            ChipWriter.Indent--;
            ChipWriter.WriteLine();
            _currentWriter?.Dispose();
            _currentWriter = null;
            ChipWriter.Flush();
        }

        public void WriteByte(byte data)
        {
            _currentWriter.Write(data);
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

        public void WriteCode(Code codeType, string code)
        {
            switch ( codeType)
            {
                case Code.Chip:
                    ChipWriter.WriteLine(code);
                    break;
                case Code.Def:
                    ConstantsWriter.WriteLine(code);
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

            _currentWriter?.Dispose();
        }

        private string GetFileNameFor(ObjectType type, string name)
        {
            string extension;
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (name == null) name = _options.OutputName;
            return Path.Combine(_options.OutputFolder, $"{name}.{extension}");
        }

        private string GetLabelPostfix(ObjectType type)
        {
            return type.ToString();
        }
    }
}